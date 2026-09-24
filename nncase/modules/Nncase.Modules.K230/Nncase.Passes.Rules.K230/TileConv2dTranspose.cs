using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileConv2dTranspose : RewriteRule<Pattern>
{
	private struct ImmParam
	{
		public int PadTop;

		public int PadBottom;

		public int PadLeft;

		public int PadRight;

		public int PaddedH;

		public int ImmE;

		public int EStart;

		public int EEnd;

		public int PaddedW;

		public int ImmF;

		public int FStart;

		public int FEnd;
	}

	private static int _count = -1;

	private readonly List<Tuple<SegmentND, TensorStat>>[] _weightRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _ofmapRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2LIfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2RWRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _l2GOfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2RWSliceRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private CcrHandler _ccrHandler = new CcrHandler();

	private WeightGroupHandler? _weightGroup;

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private GlbSearchStrategy _strategy;

	private DataType? _inputType;

	private DataType? _outputType;

	private DataType? _weightType;

	private Call? _conv;

	private int _icPerGroup;

	private int _ocPerGroup;

	private int _groupPerPass;

	private Call? _lif;

	private Call? _lw;

	private Call? _lact;

	private Call? _lwQarg;

	private Call? _sof;

	private GNNEShape? _inputShape;

	private GNNEShape? _outputShape;

	private GNNEShape? _weightsShape;

	private Padding? _paddingH;

	private Padding? _paddingW;

	private int _outputPaddingH;

	private int _outputPaddingW;

	private int _strideH;

	private int _strideW;

	private int _dilationH;

	private int _dilationW;

	private int _groups;

	private int _kernelH;

	private int _kernelW;

	private ImmParam _immParam;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNEConv2DTransposeFusion();


	private PrimFunction GetReplace(Call call, Call ld, Call st)
	{
		_count++;
		InitParameters(call, ld, st);
		TileConv2dTransposeGlb glb = SearchGlbParameters();
		int[] array = ld[GNNELoad.Input].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		byte[] array2 = ((TensorConst)((Call)call[GNNEConv2DTranspose.Weights])[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
		byte[] array3 = new byte[array2.Length];
		Array.Copy(array2, array3, array3.Length);
		T.AttachBuffer(Const.FromTensor(Tensor.FromBytes(DataTypes.UInt8, array3.ToArray(), new int[1] { array3.Length })), out Nncase.TIR.Buffer buffer3, "var ddrW");
		T.AttachBuffer((TensorConst)((Call)call[GNNEConv2DTranspose.WeightsBias])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer4, "var ddrWQarg");
		T.AttachBuffer((TensorConst)((Call)call[GNNEConv2DTranspose.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer5, "var ddrAct");
		ItemRecStatusInit();
		BuildSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, weightGroupOnly: true, _weightGroup);
		List<GnneAction> actions = BuildSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, weightGroupOnly: false, _weightGroup);
		Span<byte> bytesBuffer = buffer3.Const().Value.BytesBuffer;
		ArrangeWeights(_weightType, _weightsShape, bytesBuffer, _weightGroup);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileConv2dTranspose_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, Call conv, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf, Nncase.TIR.Buffer ddrW, Nncase.TIR.Buffer ddrWQarg, Nncase.TIR.Buffer ddrAct, bool weightGroupOnly, WeightGroupHandler weightGroup)
	{
		List<GnneAction> list = new List<GnneAction>();
		if (!weightGroupOnly)
		{
			_gpr = new GprHandler(GNNEEnv.GprNum);
			_ssr = new SsrHandler(GNNEEnv.SsrNum);
			_ccrHandler = new CcrHandler();
		}
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, _ccrHandler, _gpr, _ssr);
		if (!weightGroupOnly)
		{
			UpdateCcrRecStat();
		}
		if (!weightGroupOnly)
		{
			gnneActionUpdater.UpdateMmuConf();
		}
		int num = 0;
		int num2 = -1;
		int num3 = -1;
		int nPingPongSplit = GNNEEnv.NPingPongSplit;
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _outputShape[0]);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _immParam.ImmE);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _immParam.ImmF);
		List<Segment1D> list2 = new List<Segment1D>();
		List<Segment1D> list3 = new List<Segment1D>();
		if (glb.LastOutShape[1] >= _ocPerGroup)
		{
			list2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _outputShape[1]);
			list3 = TileUtilities.GetSegmentStartEndLength(0, glb.GlbMap[ItemName.Ifmap].Dimensions[1], _inputShape[1]);
		}
		else
		{
			for (int i = 0; i < _groups; i++)
			{
				List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(i * _ocPerGroup, glb.LastOutShape[1], (i + 1) * _ocPerGroup);
				List<Segment1D> segmentStartEndLength5 = TileUtilities.GetSegmentStartEndLength(i * _icPerGroup, glb.GlbMap[ItemName.Ifmap].Dimensions[1], (i + 1) * _icPerGroup);
				list2.AddRange(segmentStartEndLength4);
				list3.AddRange(segmentStartEndLength5);
			}
		}
		if (!weightGroupOnly && (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16))
		{
			gnneActionUpdater.UpdateLoadWQarg(_lwQarg, weightGroup, ddrWQarg);
		}
		if (!weightGroupOnly)
		{
			gnneActionUpdater.UpdateLoadAct(_lact, ddrAct);
		}
		Segment1D segment1D = new Segment1D(..0, new Padding(0, 0));
		List<SegmentND> list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list5 = list4;
		list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list6 = list4;
		int l1Pp = 0;
		foreach (Segment1D item2 in segmentStartEndLength)
		{
			foreach (Segment1D item3 in segmentStartEndLength2)
			{
				Segment1D vnocInputRowSegment = TileUtilities.GetVnocInputRowSegment(item3.Start, item3.Length, _immParam.PadTop, _immParam.PadBottom, _inputShape[2], _weightsShape[2], _strideH);
				foreach (Segment1D item4 in segmentStartEndLength3)
				{
					Segment1D vnocInputColSegment = TileUtilities.GetVnocInputColSegment(item4.Start, item4.Length, _immParam.PadLeft, _immParam.PadRight, _inputShape[3], _weightsShape[3], _strideW);
					foreach (Segment1D item5 in list2)
					{
						SegmentND tensor = new SegmentND(item2, item5, item3, item4);
						int num4 = item5.Start / _ocPerGroup * _icPerGroup;
						int num5 = (item5.End - 1) / _ocPerGroup * _icPerGroup + _icPerGroup;
						foreach (Segment1D item6 in list3)
						{
							if (item6.Start < num4 || item6.End > num5)
							{
								continue;
							}
							int num6 = item6.Start % _icPerGroup;
							int num7 = Math.Min(num6 + item6.Length, _icPerGroup);
							SegmentND segmentND = new SegmentND(item5, new Segment1D(num6..num7, new Padding(0, 0)), new Segment1D(.._weightsShape[2], new Padding(0, 0)), new Segment1D(.._weightsShape[3], new Padding(0, 0)));
							if (list6[0] == segmentND)
							{
								num3 = 0;
							}
							else if (list6[1] == segmentND)
							{
								num3 = 1;
							}
							else
							{
								num3 = (num3 + 1) % 2;
								weightGroup.Current_aligned_offset_init();
								if (weightGroupOnly)
								{
									_weightRec[num3].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
								list6[num3] = segmentND;
							}
							SegmentND segmentND2 = new SegmentND(item2, item6, vnocInputRowSegment, vnocInputColSegment);
							if (list5[0] == segmentND2)
							{
								num2 = 0;
							}
							else if (list5[1] == segmentND2)
							{
								num2 = 1;
							}
							else
							{
								num2 = (num2 + 1) % 2;
								if (!weightGroupOnly && _g2LIfRec[num2].Count > 0 && _g2LIfRec[num2][0].Item1 == segmentND2)
								{
									TensorStat item = _g2LIfRec[num2][0].Item2;
									int value = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
									List<CcrSet> ccrsToSet = new List<CcrSet>
									{
										new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, num2)), value)
									};
									List<int> stridesD = new List<int>
									{
										glb.GlbMap[ItemName.Ifmap].Dimensions[1],
										glb.GlbMap[ItemName.Ifmap].Dimensions[2],
										glb.GlbMap[ItemName.Ifmap].Dimensions[3]
									};
									gnneActionUpdater.UpdateLoadIf(segmentND2, _lif, num2, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet);
								}
								list5[num2] = segmentND2;
							}
							BuildL1Schedule(gnneActionUpdater, glb, segmentND2, segmentND, tensor, num2, num, num3, weightGroupOnly, weightGroup, l1Pp, ddrW);
						}
						if (!weightGroupOnly)
						{
							SegmentND slice = new SegmentND(tensor[0], tensor[1], tensor[2], new Segment1D(Math.Max(tensor[3].Start, _immParam.FStart)..Math.Min(tensor[3].End, _immParam.FEnd), new Padding(0, 0)));
							if (tensor[2].Start == 0)
							{
								int eStart = _immParam.EStart;
								int num8 = _immParam.EStart + (slice[2].End - _immParam.EStart);
								slice[2] = new Segment1D(eStart..num8, new Padding(0, 0));
							}
							if (slice[2].End == _immParam.ImmE)
							{
								int num9 = _immParam.EEnd - (_immParam.EEnd - slice[2].Start);
								int eEnd = _immParam.EEnd;
								slice[2] = new Segment1D(num9..eEnd, new Padding(0, 0));
							}
							SegmentND ofmapStDdr = new SegmentND(slice[0], slice[1], new Segment1D((slice[2].Start - _immParam.EStart)..(slice[2].End - _immParam.EStart), new Padding(0, 0)), new Segment1D((slice[3].Start - _immParam.FStart)..(slice[3].End - _immParam.FStart), new Padding(0, 0)));
							int offset = TileUtilities.GetSliceOffsetInTensor(in tensor, in slice) * glb.GlbMap[ItemName.Ofmap].DType.SizeInBytes;
							bool num10 = !_ofmapRec[num][0].Item2.IsLastSlice;
							_ofmapRec[num].RemoveAt(0);
							List<CcrSet> list7 = new List<CcrSet>();
							if ((num10 ? 1 : 0) > (false ? 1 : 0))
							{
								list7.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, num)), 1));
							}
							List<CcrClr> ccrsToClr = new List<CcrClr>
							{
								new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, num)))
							};
							gnneActionUpdater.UpdateStoreT(tensor, _sof, num, ddrOf, offset, ofmapStDdr, list7, ccrsToClr);
						}
						else
						{
							_ofmapRec[num].Add(new Tuple<SegmentND, TensorStat>(tensor, new TensorStat(isFirstSlice: false, isLastSlice: false)));
						}
						num = (num + 1) % nPingPongSplit;
					}
				}
			}
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2dTranspose.cs", 291);
		return list;
	}

	private List<List<Segment1D>> GetL1McSeg(SegmentND ifmap, SegmentND psum, int mInloop, int cInloop)
	{
		int num = Math.Max(ifmap[1].Length / _icPerGroup, 1);
		List<Segment1D> list = new List<Segment1D>();
		List<Segment1D> list2 = new List<Segment1D>();
		if (num >= 2 && _groupPerPass < 2)
		{
			for (int i = 0; i < num; i++)
			{
				List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[1].Start + i * _ocPerGroup, mInloop, psum[1].Start + (i + 1) * _ocPerGroup);
				List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start + i * _icPerGroup, cInloop, ifmap[1].Start + (i + 1) * _icPerGroup);
				list.AddRange(segmentStartEndLength);
				list2.AddRange(segmentStartEndLength2);
			}
		}
		else
		{
			list = TileUtilities.GetSegmentStartEndLength(psum[1].Start, mInloop, psum[1].End);
			list2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start, cInloop, ifmap[1].End);
		}
		return new List<List<Segment1D>> { list, list2 };
	}

	private bool HandleL1Allocate(int h, int w, int e, int f, int psumPingPangSplit, int ifBytesPerElementGlb)
	{
		if (e * f > GNNEEnv.PsumL1ElePerChan / psumPingPangSplit)
		{
			return false;
		}
		return GNNEEnv.PuHeight * h * w * ifBytesPerElementGlb <= GNNEEnv.IfL1Size;
	}

	private List<int> L1Search(TiledGlb glb, SegmentND weight, SegmentND psum)
	{
		int item = Math.Min(GNNEEnv.PuHeight, _groupPerPass * _icPerGroup);
		int item2 = Math.Min(GNNEEnv.PuWidth, _groupPerPass * _ocPerGroup);
		int num = 1;
		int num2 = 1;
		int num3 = 1;
		int num4 = 1;
		num = GlbInputHeight(num3);
		num2 = GlbInputWidth(num4);
		int bytesPerElement = TileUtilities.GetBytesPerElement(_inputType);
		if (!HandleL1Allocate(num, num2, num3, num4, 1, bytesPerElement))
		{
			throw new NotSupportedException("exceeds L1 size");
		}
		while (num4 < psum[3].Length && num3 < psum[2].Length)
		{
			if (num4 <= num3)
			{
				int num5 = num4 + 1;
				int num6 = GlbInputWidth(num5);
				if (!HandleL1Allocate(num, num6, num3, num5, 1, bytesPerElement))
				{
					break;
				}
				num4 = num5;
				num2 = num6;
			}
			else
			{
				int num7 = num3 + 1;
				int num8 = GlbInputHeight(num7);
				if (!HandleL1Allocate(num8, num2, num7, num4, 1, bytesPerElement))
				{
					break;
				}
				num3 = num7;
				num = num8;
			}
		}
		while (num3 < psum[2].Length)
		{
			int num9 = num3 + 1;
			int num10 = GlbInputHeight(num9);
			if (!HandleL1Allocate(num10, num2, num9, num4, 1, bytesPerElement))
			{
				break;
			}
			num3 = num9;
			num = num10;
		}
		while (num4 < psum[3].Length)
		{
			int num11 = num4 + 1;
			int num12 = GlbInputWidth(num11);
			if (!HandleL1Allocate(num, num12, num3, num11, 1, bytesPerElement))
			{
				break;
			}
			num4 = num11;
			num2 = num12;
		}
		return new List<int> { item, item2, num3, num4 };
		int GlbInputHeight(int e)
		{
			return Math.Min((int)Math.Ceiling((decimal)(1f * (float)(e + _weightsShape[2] - _strideH - 1) / (float)_strideH)) + 1, _immParam.PaddedH - _immParam.PadTop - _immParam.PadBottom);
		}
		int GlbInputWidth(int f)
		{
			return Math.Min((int)Math.Ceiling((decimal)(1f * (float)(f + _weightsShape[3] - _strideW - 1) / (float)_strideW)) + 1, _immParam.PaddedW - _immParam.PadLeft - _immParam.PadRight);
		}
	}

	private void BuildL1Schedule(GnneActionUpdater actionUpdater, TiledGlb glb, SegmentND ifmap, SegmentND weight, SegmentND psum, int iPp, int ofPp, int wPp, bool weightGroupOnly, WeightGroupHandler weightGroup, int l1Pp, Nncase.TIR.Buffer ddrW)
	{
		List<int> list = L1Search(glb, weight, psum);
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[2].Start, list[2], psum[2].End);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(psum[3].Start, list[3], psum[3].End);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(psum[0].Start, 1, psum[0].End);
		List<List<Segment1D>> l1McSeg = GetL1McSeg(ifmap, psum, list[1], list[0]);
		List<Segment1D> list2 = l1McSeg[0];
		List<Segment1D> list3 = l1McSeg[1];
		int chunkSize = 1;
		int chunkSize2 = 1;
		foreach (Segment1D item in list2)
		{
			foreach (Segment1D item2 in segmentStartEndLength3)
			{
				foreach (Segment1D item3 in segmentStartEndLength)
				{
					Segment1D vnocInputRowSegment = TileUtilities.GetVnocInputRowSegment(item3.Start, item3.Length, _immParam.PadTop, _immParam.PadBottom, _inputShape[2], _weightsShape[2], _strideH);
					foreach (Segment1D item4 in segmentStartEndLength2)
					{
						Segment1D vnocInputColSegment = TileUtilities.GetVnocInputColSegment(item4.Start, item4.Length, _immParam.PadLeft, _immParam.PadRight, _inputShape[3], _weightsShape[3], _strideW);
						SegmentND segmentND = new SegmentND(item2, item, item3, item4);
						int num = item.Start / _ocPerGroup * _icPerGroup;
						int num2 = (item.End - 1) / _ocPerGroup * _icPerGroup + _icPerGroup;
						bool flag = true;
						foreach (Segment1D item5 in list3)
						{
							if (item5.Start < num || item5.End > num2)
							{
								continue;
							}
							int num3 = item5.Start % _icPerGroup;
							Segment1D segment1D = new Segment1D(new System.Range(end: Math.Min(num3 + item5.Length, _icPerGroup), start: num3), new Padding(0, 0));
							Segment1D segment1D2 = item5;
							List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(0, chunkSize, _weightsShape[2]);
							List<Segment1D> segmentStartEndLength5 = TileUtilities.GetSegmentStartEndLength(0, chunkSize2, _weightsShape[3]);
							Segment1D segment1D3 = item2;
							Segment1D segment1D4 = item;
							bool flag2 = false;
							SegmentND segmentND2 = new SegmentND(segment1D3, segment1D2, vnocInputRowSegment, vnocInputColSegment);
							if (segmentND2[0].Length > 0 && segmentND2[1].Length > 0 && segmentND2[2].Length > 0 && segmentND2[3].Length > 0)
							{
								flag2 = true;
								if (!weightGroupOnly)
								{
									int num4 = _g2LIfRec[iPp][0].Item2.Stat_cnt();
									_g2LIfRec[iPp].RemoveAt(0);
									List<CcrClr> list4 = new List<CcrClr>();
									if (num4 > 0)
									{
										list4.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, iPp))));
									}
									actionUpdater.UpdateG2LIf(segmentND2, ifmap, _lif, iPp, null, list4);
								}
								else
								{
									_g2LIfRec[iPp].Add(new Tuple<SegmentND, TensorStat>(ifmap, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
							}
							foreach (Segment1D item6 in segmentStartEndLength4)
							{
								foreach (Segment1D item7 in segmentStartEndLength5)
								{
									SegmentND segmentND3 = new SegmentND(segment1D4, segment1D, item6, item7);
									Segment1D vnocInputRowSegmentGivenRi = TileUtilities.GetVnocInputRowSegmentGivenRi(segmentND[2].Start, segmentND[2].Length, _immParam.PadTop, _immParam.PadBottom, _inputShape[2], segmentND3[2].Start, _strideH);
									Segment1D vnocInputColumnSegmentGivenSi = TileUtilities.GetVnocInputColumnSegmentGivenSi(segmentND[3].Start, segmentND[3].Length, _immParam.PadLeft, _immParam.PadRight, _inputShape[3], segmentND3[3].Start, _strideW);
									SegmentND slice = new SegmentND(segment1D3, segment1D2, vnocInputRowSegmentGivenRi, vnocInputColumnSegmentGivenSi);
									Segment1D vnocOutputRowSegmentGivenRi = TileUtilities.GetVnocOutputRowSegmentGivenRi(vnocInputRowSegmentGivenRi.Start, vnocInputRowSegmentGivenRi.Length, segmentND3[2].Start, _strideH, vnocInputRowSegmentGivenRi.Padding.Before, vnocInputRowSegmentGivenRi.Padding.After, _inputShape[2]);
									Segment1D vnocOutputRowSegmentGivenRi2 = TileUtilities.GetVnocOutputRowSegmentGivenRi(vnocInputColumnSegmentGivenSi.Start, vnocInputColumnSegmentGivenSi.Length, segmentND3[3].Start, _strideW, vnocInputColumnSegmentGivenSi.Padding.Before, vnocInputColumnSegmentGivenSi.Padding.After, _inputShape[3]);
									SegmentND r2LPsum = new SegmentND(segment1D3, segment1D4, vnocOutputRowSegmentGivenRi, vnocOutputRowSegmentGivenRi2);
									if (vnocOutputRowSegmentGivenRi.Length == 0 || vnocOutputRowSegmentGivenRi2.Length == 0)
									{
										continue;
									}
									int num5 = ((!(_weightType == DataTypes.Int16)) ? 1 : 2);
									int num6 = ((!(_inputType == DataTypes.Int16)) ? 1 : 2);
									for (int i = 0; i < num5; i++)
									{
										for (int num7 = 0; num7 < num6; num7++)
										{
											if (!weightGroupOnly)
											{
												Tuple<SegmentND, TensorStat> tuple = _weightRec[wPp][0];
												Tuple<SegmentND, TensorStat> tuple2 = _g2RWRec[wPp][0];
												_g2RWRec[wPp].RemoveAt(0);
												if (tuple2.Item2.IsLastSlice)
												{
													_weightRec[wPp].RemoveAt(0);
												}
												Tuple<SegmentND, TensorStat> tuple3 = _g2RWSliceRec[wPp][0];
												_g2RWSliceRec[wPp].RemoveAt(0);
												TileUtilities.Assert(segmentND3 == tuple3.Item1 && tuple2.Item1 == tuple.Item1, "l2RW == g2RWSliceRecStat.Item1 && g2RWRecStat.Item1 == weightRecStat.Item1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2dTranspose.cs", 530);
												if (tuple3.Item2.IsFirstSlice)
												{
													int num8 = ((!tuple.Item2.IsFirstSlice && tuple2.Item2.IsFirstSlice) ? 1 : 0);
													List<CcrClr> list5 = new List<CcrClr>();
													if (num8 > 0)
													{
														list5.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WeightFake, wPp))));
													}
													List<CcrSet> ccrsToSet = new List<CcrSet>
													{
														new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, (wPp << 1) + (tuple3.Item2.SliceIdx & 1))), 1)
													};
													actionUpdater.UpdateLoadW(segmentND3, _lw, weightGroup, wPp, ddrW, ccrsToSet, list5, 0, h2c: false, 0, ItemName.Weight, i);
												}
												int num9 = 0;
												if (tuple2.Item2.IsLastSlice && _weightRec[wPp].Count > 0)
												{
													num9 = 1;
												}
												List<CcrSet> list6 = new List<CcrSet>();
												if (num9 > 0)
												{
													list6.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WeightFake, wPp)), 1));
												}
												List<CcrClr> list7 = new List<CcrClr>();
												if (tuple3.Item2.IsFirstSlice)
												{
													list7.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, (wPp << 1) + (tuple3.Item2.SliceIdx & 1)))));
												}
												actionUpdater.UpdateG2RW(segmentND3, weightGroup, _ocPerGroup, _lw, wPp, i, list6, list7);
											}
											else
											{
												weightGroup.UpdateWeightGroup(segmentND3);
												_g2RWRec[wPp].Add(new Tuple<SegmentND, TensorStat>(weight, new TensorStat(isFirstSlice: false, isLastSlice: false)));
												_g2RWSliceRec[wPp].Add(new Tuple<SegmentND, TensorStat>(segmentND3, new TensorStat(isFirstSlice: false, isLastSlice: false, -1)));
											}
											if (!weightGroupOnly)
											{
												actionUpdater.UpdateL2RIf(slice, segmentND2, _strideH, _strideW, _icPerGroup, _lif, 0, num7, ((TensorConst)_conv[GNNEConv2DTranspose.DeqBias]).Value.ToScalar<int>());
											}
											bool releaseIf = false;
											int num10;
											if (item7 == segmentStartEndLength5[segmentStartEndLength5.Count - 1])
											{
												if (item6 == segmentStartEndLength4[segmentStartEndLength4.Count - 1] && i == num5 - 1)
												{
													num10 = ((num7 == num6 - 1) ? 1 : 0);
													goto IL_08dd;
												}
											}
											num10 = 0;
											goto IL_08dd;
											IL_08dd:
											if (((uint)num10 & (flag2 ? 1u : 0u)) != 0)
											{
												releaseIf = true;
												flag2 = false;
											}
											bool loopStart = false;
											if (flag && i == 0 && num7 == 0)
											{
												loopStart = true;
												flag = false;
											}
											bool flag3 = segment1D.End == _weightsShape[1] && item7.End == weight[3].End && item6.End == weight[2].End && i == num5 - 1 && num7 == num6 - 1;
											if (!weightGroupOnly)
											{
												int num11 = 0;
												int num12 = 0;
												if (flag3)
												{
													Tuple<SegmentND, TensorStat> tuple4 = _l2GOfRec[ofPp][0];
													_l2GOfRec[ofPp].RemoveAt(0);
													if (tuple4.Item2.IsFirstSlice && !_ofmapRec[ofPp][0].Item2.IsFirstSlice)
													{
														num12 = 1;
													}
													if (tuple4.Item2.IsLastSlice)
													{
														num11 = 1;
													}
												}
												List<CcrSet> list8 = new List<CcrSet>();
												if (num11 > 0)
												{
													list8.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, ofPp)), 1));
												}
												List<CcrClr> list9 = new List<CcrClr>();
												if (num12 > 0)
												{
													list9.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, ofPp))));
												}
												int shift = ((TensorConst)_conv[GNNEConv2DTranspose.ShiftBits]).Value.ToScalar<int>();
												ACT0_OUTPUT_DEST destTarget = ACT0_OUTPUT_DEST.dm;
												actionUpdater.UpdateR2LPsum(shift, r2LPsum, segmentND, psum, ofPp, l1Pp, destTarget, releaseIf, Math.Max(i, num7), TcuComputeMode.TransposeConv2d, loopStart, flag3, _strideH, _strideW, _ocPerGroup, _inputType, _weightType, _outputType, _lact.CheckedDataType, list8, list9);
											}
											else if (flag3)
											{
												_l2GOfRec[ofPp].Add(new Tuple<SegmentND, TensorStat>(psum, new TensorStat(isFirstSlice: false, isLastSlice: false)));
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void ArrangeWeights(DataType weightsType, GNNEShape weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		List<SegmentND> list = weightGroup.WeightGroupSlice();
		byte[] array = new byte[oldWeights.Length];
		int num = 0;
		int num2 = 0;
		foreach (SegmentND item in list)
		{
			TileUtilities.Assert(num == weightGroup.WeightGroupOffset(item) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2dTranspose.cs", 652);
			for (int i = 0; i < bytesPerElement; i++)
			{
				for (int j = 0; j < item[0].Length; j++)
				{
					for (int k = 0; k < item[2].Length; k++)
					{
						for (int l = 0; l < item[3].Length; l++)
						{
							for (int m = 0; m < item[1].Length; m++)
							{
								int num3 = ((((j + item[0].Start) * weightsShape[1] + m + item[1].Start) * weightsShape[2] + k + item[2].Start) * weightsShape[3] + l + item[3].Start) * bytesPerElement;
								array[num2++] = oldWeights[num3 + i];
								num++;
							}
						}
					}
				}
			}
		}
		array.CopyTo(oldWeights);
	}

	private void UpdateCcrRecStat()
	{
		List<Tuple<SegmentND, TensorStat>>[] g2LIfRec = _g2LIfRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list in g2LIfRec)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (j == 0)
				{
					list[j].Item2.IsFirstSlice = true;
				}
				if (j == list.Count - 1)
				{
					list[j].Item2.IsLastSlice = true;
				}
				if (j < list.Count - 1 && list[j].Item1 != list[j + 1].Item1)
				{
					list[j].Item2.IsLastSlice = true;
					list[j + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		g2LIfRec = _g2RWRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list2 in g2LIfRec)
		{
			for (int k = 0; k < list2.Count; k++)
			{
				if (k == 0)
				{
					list2[k].Item2.IsFirstSlice = true;
				}
				if (k == list2.Count - 1)
				{
					list2[k].Item2.IsLastSlice = true;
				}
				if (k < list2.Count - 1 && list2[k].Item1 != list2[k + 1].Item1)
				{
					list2[k].Item2.IsLastSlice = true;
					list2[k + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		g2LIfRec = _l2GOfRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list3 in g2LIfRec)
		{
			for (int l = 0; l < list3.Count; l++)
			{
				if (l == 0)
				{
					list3[l].Item2.IsFirstSlice = true;
				}
				if (l == list3.Count - 1)
				{
					list3[l].Item2.IsLastSlice = true;
				}
				if (l < list3.Count - 1 && list3[l].Item1 != list3[l + 1].Item1)
				{
					list3[l].Item2.IsLastSlice = true;
					list3[l + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		g2LIfRec = _weightRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list4 in g2LIfRec)
		{
			if (list4.Count > 0)
			{
				list4[0].Item2.IsFirstSlice = true;
				list4[list4.Count - 1].Item2.IsLastSlice = true;
			}
		}
		g2LIfRec = _ofmapRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list5 in g2LIfRec)
		{
			if (list5.Count > 0)
			{
				list5[0].Item2.IsFirstSlice = true;
				list5[list5.Count - 1].Item2.IsLastSlice = true;
			}
		}
		for (int m = 0; m < _g2RWSliceRec.Length; m++)
		{
			List<SegmentND> list6 = new List<SegmentND>();
			List<SegmentND> wSlices = new List<SegmentND>();
			List<Tuple<SegmentND, TensorStat>> list7 = new List<Tuple<SegmentND, TensorStat>>();
			for (int n = 0; n < _g2RWRec[m].Count; n++)
			{
				if (_g2RWRec[m][n].Item2.IsFirstSlice)
				{
					list6.Clear();
					wSlices.Clear();
					list7.Clear();
				}
				if (!list6.Contains(_g2RWSliceRec[m][n].Item1))
				{
					list6.Add(_g2RWSliceRec[m][n].Item1);
				}
				wSlices.Add(_g2RWSliceRec[m][n].Item1);
				list7.Add(_g2RWSliceRec[m][n]);
				if (!_g2RWRec[m][n].Item2.IsLastSlice)
				{
					continue;
				}
				foreach (SegmentND slice in list6)
				{
					list7[wSlices.FindIndex((SegmentND x) => x == slice)].Item2.IsFirstSlice = true;
					list7[wSlices.FindLastIndex((SegmentND rWSliceIdx) => rWSliceIdx == slice)].Item2.IsLastSlice = true;
				}
				int cnt;
				for (cnt = 0; cnt < wSlices.Count; cnt++)
				{
					list7[cnt].Item2.SliceIdx = list6.FindIndex((SegmentND x) => x == wSlices[cnt]);
					_g2RWSliceRec[m][n - wSlices.Count + 1 + cnt] = list7[cnt];
				}
			}
			if (!(_weightType == DataTypes.Int16))
			{
				continue;
			}
			for (int num = 1; num < _g2RWSliceRec[m].Count; num++)
			{
				if (_g2RWSliceRec[m][_g2RWSliceRec[m].Count - num - 1].Item2.IsFirstSlice)
				{
					_g2RWSliceRec[m][_g2RWSliceRec[m].Count - num].Item2.IsFirstSlice = true;
				}
				if (_g2RWSliceRec[m][num].Item2.IsLastSlice)
				{
					_g2RWSliceRec[m][num - 1].Item2.IsLastSlice = true;
				}
			}
			for (int num2 = 0; num2 < _g2RWSliceRec[m].Count; num2++)
			{
				_g2RWSliceRec[m][num2].Item2.SliceIdx = _g2RWSliceRec[m][num2].Item2.SliceIdx * 2 + (num2 & 1);
			}
		}
	}

	private void ItemRecStatusInit()
	{
		for (int i = 0; i < _weightRec.Length; i++)
		{
			if (_weightRec[i] != null)
			{
				_weightRec[i].Clear();
			}
			else
			{
				_weightRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
			if (_ofmapRec[i] != null)
			{
				_ofmapRec[i].Clear();
			}
			else
			{
				_ofmapRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
			if (_g2LIfRec[i] != null)
			{
				_g2LIfRec[i].Clear();
			}
			else
			{
				_g2LIfRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
			if (_g2RWRec[i] != null)
			{
				_g2RWRec[i].Clear();
			}
			else
			{
				_g2RWRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
			if (_l2GOfRec[i] != null)
			{
				_l2GOfRec[i].Clear();
			}
			else
			{
				_l2GOfRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
			if (_g2RWSliceRec[i] != null)
			{
				_g2RWSliceRec[i].Clear();
			}
			else
			{
				_g2RWSliceRec[i] = new List<Tuple<SegmentND, TensorStat>>();
			}
		}
	}

	private TileConv2dTransposeGlb SearchGlbParameters()
	{
		int i = 1;
		int j = 1;
		int num = _weightsShape[2];
		int num2 = _weightsShape[3];
		int num3 = 1;
		int num4 = Math.Min(GNNEEnv.PuWidth, _immParam.ImmF);
		if (num4 < GNNEEnv.PuWidth)
		{
			num3 = Math.Min((int)Math.Ceiling(1.0 * (double)GNNEEnv.PuWidth / (double)num4), _immParam.ImmE);
		}
		int num5 = GetGlbInputHeight(num3);
		int num6 = GlbInputWidth(num4);
		int k = 1;
		AllocateResult allocateResult;
		for (; j < _icPerGroup; j++)
		{
			allocateResult = HandleAllocate(i, j + 1, num5, num6, num, num2, k, num3, num4);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; k < GNNEEnv.PuWidth && k < _ocPerGroup; k++)
		{
			allocateResult = HandleAllocate(i, j, num5, num6, num, num2, k + 1, num3, num4);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		if (_strategy == GlbSearchStrategy.IfFirst)
		{
			if (_groupPerPass == 1)
			{
				while (k < _outputShape[1] && (num != _weightsShape[2] || num2 != _weightsShape[3] || j < _weightsShape[1] || k < _outputShape[1] / 2))
				{
					int num7 = k + 1;
					int num8 = j;
					if (k >= _ocPerGroup)
					{
						num7 = k + _ocPerGroup;
						num8 = j + _icPerGroup;
					}
					allocateResult = HandleAllocate(i, num8, num5, num6, num, num2, num7, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num7;
					j = num8;
				}
			}
			else
			{
				while (k < _outputShape[1] && (num != _weightsShape[2] || num2 != _weightsShape[3] || j < _weightsShape[1] || k < _weightsShape[0] / 2))
				{
					int num9 = Math.Min(k + _groupPerPass * _ocPerGroup, _outputShape[1]);
					int num10 = Math.Min(j + _groupPerPass * _icPerGroup, _inputShape[1]);
					allocateResult = HandleAllocate(i, num10, num5, num6, num, num2, num9, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num9;
					j = num10;
				}
			}
			while (num3 < _immParam.ImmE)
			{
				int num11 = num3 + 1;
				int num12 = GetGlbInputHeight(num11);
				allocateResult = HandleAllocate(i, j, num12, num6, num, num2, k, num11, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num3 = num11;
				num5 = num12;
			}
			while (num4 < _immParam.ImmF)
			{
				int num13 = num4 + 1;
				int num14 = GlbInputWidth(num13);
				allocateResult = HandleAllocate(i, j, num5, num14, num, num2, k, num3, num13);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num4 = num13;
				num6 = num14;
			}
			for (; i < _outputShape[0]; i++)
			{
				allocateResult = HandleAllocate(i + 1, j, num5, num6, num, num2, k, num3, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
			}
		}
		else if (_strategy == GlbSearchStrategy.WFirst)
		{
			while (num3 < _immParam.ImmE)
			{
				int num15 = num3 + 1;
				int num16 = GetGlbInputHeight(num15);
				allocateResult = HandleAllocate(i, j, num16, num6, num, num2, k, num15, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num3 = num15;
				num5 = num16;
			}
			while (num4 < _immParam.ImmF)
			{
				int num17 = num4 + 1;
				int num18 = GlbInputWidth(num17);
				allocateResult = HandleAllocate(i, j, num5, num18, num, num2, k, num3, num17);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num4 = num17;
				num6 = num18;
			}
			for (; i < _outputShape[0] && (j != _inputShape[1] || num5 != _immParam.ImmE || num6 != _immParam.ImmF || i < _outputShape[0] / 2); i++)
			{
				allocateResult = HandleAllocate(i + 1, j, num5, num6, num, num2, k, num3, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
			}
			if (_groupPerPass == 1)
			{
				while (k < _outputShape[1])
				{
					int num19 = k + 1;
					int num20 = j;
					if (k >= _ocPerGroup)
					{
						num19 = k + _ocPerGroup;
						num20 = j + _icPerGroup;
					}
					allocateResult = HandleAllocate(i, num20, num5, num6, num, num2, num19, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num19;
					j = num20;
				}
			}
			else
			{
				while (k < _outputShape[1])
				{
					int num21 = Math.Min(k + _groupPerPass * _ocPerGroup, _outputShape[1]);
					int num22 = Math.Min(j + _groupPerPass * _icPerGroup, _inputShape[1]);
					allocateResult = HandleAllocate(i, num22, num5, num6, num, num2, num21, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num21;
					j = num22;
				}
			}
		}
		if (k % GNNEEnv.PuWidth != 0 && k > GNNEEnv.PuWidth && k <= _ocPerGroup)
		{
			k = k / GNNEEnv.PuWidth * GNNEEnv.PuWidth;
		}
		allocateResult = HandleAllocate(i, j, num5, num6, num, num2, k, num3, num4, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2dTranspose.cs", 1146);
		GNNEShape gNNEShape = new GNNEShape(i, k, num3, num4);
		return new TileConv2dTransposeGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
		int GetGlbInputHeight(int e)
		{
			return Math.Min((int)Math.Ceiling((decimal)(1f * (float)(e + _weightsShape[2] - _strideH - 1) / (float)_strideH)) + 1, _immParam.PaddedH - _immParam.PadTop - _immParam.PadBottom);
		}
		int GlbInputWidth(int f)
		{
			return Math.Min((int)Math.Ceiling((decimal)(1f * (float)(f + _weightsShape[3] - _strideW - 1) / (float)_strideW)) + 1, _immParam.PaddedW - _immParam.PadLeft - _immParam.PadRight);
		}
	}

	private AllocateResult HandleAllocate(int n, int c, int h, int w, int r, int s, int m, int e, int f, bool isFinal = false)
	{
		List<float> glbUsage = new List<float> { 0f, 0f };
		AllocateResult boxes = GetBoxes(n, c, h, w, r, s, m, e, f, glbUsage);
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(new BoxPacker(16)
		{
			Boxes = boxes.Boxes
		});
		allocateResult.GlbMap = boxes.GlbMap;
		if (allocateResult.IsOk)
		{
			foreach (KeyValuePair<ItemName, TensorOnGlb> item in boxes.GlbMap)
			{
				if (allocateResult.Items.ContainsKey(item.Key))
				{
					item.Value.Mmu = allocateResult.Items[item.Key];
				}
			}
		}
		return allocateResult;
	}

	private void InitParameters(Call convNode, Call ld, Call st)
	{
		_conv = convNode;
		_lif = ld;
		_lw = (Call)_conv[GNNEConv2DTranspose.Weights];
		_lact = (Call)_conv[GNNEConv2DTranspose.Act];
		_lwQarg = (Call)_conv[GNNEConv2DTranspose.WeightsBias];
		_sof = st;
		_inputShape = new GNNEShape(_lif.CheckedShape[0].FixedValue, _lif.CheckedShape[1].FixedValue, _lif.CheckedShape[2].FixedValue, _lif.CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(_conv.CheckedShape[0].FixedValue, _conv.CheckedShape[1].FixedValue, _conv.CheckedShape[2].FixedValue, _conv.CheckedShape[3].FixedValue);
		_groups = ((TensorConst)_conv[GNNEConv2DTranspose.Groups]).Value.ToScalar<int>();
		_weightsShape = new GNNEShape(_lw.CheckedShape[0].FixedValue * _groups, _lw.CheckedShape[1].FixedValue / _groups, _lw.CheckedShape[2].FixedValue, _lw.CheckedShape[3].FixedValue);
		int[] array = ((TensorConst)_conv[GNNEConv2DTranspose.Padding]).Value.ToArray<int>();
		_paddingH = new Padding(array[0], array[1]);
		_paddingW = new Padding(array[2], array[3]);
		_outputPaddingH = ((TensorConst)_conv[GNNEConv2DTranspose.OutputPadding]).Value.ToArray<int>()[0];
		_outputPaddingW = ((TensorConst)_conv[GNNEConv2DTranspose.OutputPadding]).Value.ToArray<int>()[1];
		_strideH = ((TensorConst)_conv[GNNEConv2DTranspose.Stride]).Value.ToArray<int>()[0];
		_strideW = ((TensorConst)_conv[GNNEConv2DTranspose.Stride]).Value.ToArray<int>()[1];
		_dilationH = ((TensorConst)_conv[GNNEConv2DTranspose.Dilation]).Value.ToArray<int>()[0];
		_dilationW = ((TensorConst)_conv[GNNEConv2DTranspose.Dilation]).Value.ToArray<int>()[1];
		_kernelH = _lw.CheckedShape[2].FixedValue;
		_kernelW = _lw.CheckedShape[3].FixedValue;
		_icPerGroup = _inputShape[1] / _groups;
		_ocPerGroup = _outputShape[1] / _groups;
		_groupPerPass = Math.Min(Math.Max(Math.Min(GNNEEnv.PuHeight / _icPerGroup, GNNEEnv.PuWidth / _ocPerGroup), 1), _groups);
		_strategy = DetermineSearchStrategy();
		_inputType = _lif.CheckedDataType;
		_weightType = _lw.CheckedDataType;
		_weightGroup = new WeightGroupHandler(_weightType, _weightType);
		_outputType = _conv.CheckedDataType;
		ref int paddedH = ref _immParam.PaddedH;
		ref int immE = ref _immParam.ImmE;
		(int, int) convTPadHImmE = TileUtilities.GetConvTPadHImmE(_inputShape[2], _strideH, _kernelH, _outputShape[2]);
		paddedH = convTPadHImmE.Item1;
		immE = convTPadHImmE.Item2;
		paddedH = ref _immParam.PaddedW;
		ref int immF = ref _immParam.ImmF;
		convTPadHImmE = TileUtilities.GetConvTPadWImmF(_inputShape[3], _strideW, _kernelW, _outputShape[3]);
		paddedH = convTPadHImmE.Item1;
		immF = convTPadHImmE.Item2;
		Padding padding = TileUtilities.CalPadTopBottom(_immParam.PaddedH, _inputShape[2]);
		_immParam.PadTop = padding.Before;
		_immParam.PadBottom = padding.After;
		if (_paddingH.Before > 0)
		{
			_immParam.EStart = _paddingH.Before;
			_immParam.EEnd = _immParam.EStart + _outputShape[2];
		}
		Padding padding2 = TileUtilities.CalPadLeftRight(_immParam.PaddedW, _inputShape[3]);
		_immParam.PadLeft = padding2.Before;
		_immParam.PadRight = padding2.After;
		if (_paddingW.Before > 0)
		{
			_immParam.FStart = _paddingW.Before;
			_immParam.FEnd = _immParam.FStart + _outputShape[3];
		}
		paddedH = ref _immParam.EStart;
		ref int eEnd = ref _immParam.EEnd;
		convTPadHImmE = TileUtilities.GetConvTOutputStartEnd(_inputShape[2], _immParam.PaddedH, _immParam.ImmE, _outputShape[2], _paddingH, _outputPaddingH);
		paddedH = convTPadHImmE.Item1;
		eEnd = convTPadHImmE.Item2;
		paddedH = ref _immParam.FStart;
		ref int fEnd = ref _immParam.FEnd;
		(paddedH, fEnd) = TileUtilities.GetConvTOutputStartEnd(_inputShape[3], _immParam.PaddedW, _immParam.ImmF, _outputShape[3], _paddingW, _outputPaddingW);
	}

	private GlbSearchStrategy DetermineSearchStrategy()
	{
		if (_inputShape?.Size > _weightsShape?.Size)
		{
			return GlbSearchStrategy.IfFirst;
		}
		return GlbSearchStrategy.WFirst;
	}

	private AllocateResult GetBoxes(int n, int c, int h, int w, int r, int s, int m, int e, int f, List<float> glbUsage)
	{
		List<BoxOnGlb> list = new List<BoxOnGlb>();
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		List<float> list2 = new List<float> { 0f, 0f };
		int num = GNNEEnv.NPingPongSplit;
		if (n == _inputShape[0] && c == _inputShape[1] && h == _inputShape[2] && w == _inputShape[3])
		{
			num = 1;
		}
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, c, h, w }, _inputType, 0);
		int num2 = tensorOnGlb.AllocatedBytes * num;
		list2[0] += num2;
		num2 = TileUtilities.GetAlignedNum(num2, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		list2[1] += num2;
		int num3 = c;
		if (num3 > _icPerGroup)
		{
			num3 = _icPerGroup;
		}
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { m, num3, r, s }, _weightType, 0);
		int num4 = SpaceSearcher.GetWeightSize(r, s, num3, m, TileUtilities.GetBytesPerElement(_weightType)) * GNNEEnv.NPingPongSplit;
		tensorOnGlb2.AllocatedBytes = num4 / GNNEEnv.NPingPongSplit;
		list2[0] += num4;
		num4 = TileUtilities.GetAlignedNum(num4, GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
		list2[1] += num4;
		TensorOnGlb tensorOnGlb3 = new TensorOnGlb(new int[4] { n, m, e, f }, _outputType, 0);
		int num5 = tensorOnGlb3.AllocatedBytes * GNNEEnv.NPingPongSplit;
		list2[0] += num5;
		num5 = TileUtilities.GetAlignedNum(num5, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		list2[1] += num5;
		TensorOnGlb value = new TensorOnGlb(new int[4]
		{
			1,
			1,
			_outputShape[1],
			GNNEEnv.ActNumPerChan
		}, _lact.CheckedDataType, 0);
		int actSize = SpaceSearcher.GetActSize(_outputShape[1], GNNEEnv.ActNumPerChan, TileUtilities.GetBytesPerElement(_lact.CheckedDataType));
		list2[0] += actSize;
		actSize = TileUtilities.GetAlignedNum(actSize, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		list2[1] += actSize;
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			num2 / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.WBankWidth,
			num4 / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Weight));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			num5 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.ActBankWidth,
			actSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Act));
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Weight, tensorOnGlb2);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb3);
		dictionary.Add(ItemName.Act, value);
		if (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16)
		{
			int[] obj = new int[4] { 1, 1, 1, 0 };
			obj[3] = _outputShape[1];
			TensorOnGlb value2 = new TensorOnGlb(obj, _lwQarg.CheckedDataType, 0);
			int wQargSize = SpaceSearcher.GetWQargSize(_outputShape[1], TileUtilities.GetBytesPerElement(_lwQarg.CheckedDataType));
			if (_inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1)
			{
				wQargSize = SpaceSearcher.GetWQargSize(_outputShape[1] * GNNEEnv.PuWidth, TileUtilities.GetBytesPerElement(_lwQarg.CheckedDataType));
			}
			list2[0] += wQargSize;
			wQargSize = TileUtilities.GetAlignedNum(wQargSize, GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += wQargSize;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.WQargBankWidth,
				wQargSize / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.WQarg));
			dictionary.Add(ItemName.WQarg, value2);
		}
		glbUsage[0] = list2[0] / (float)GNNEEnv.GlbSize;
		glbUsage[1] = list2[1] / (float)GNNEEnv.GlbSize;
		return new AllocateResult
		{
			Boxes = list,
			GlbMap = dictionary
		};
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, ld, st);
	}
}
