using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileMatMul : RewriteRule<Pattern>
{
	private static int _count = -1;

	private readonly List<Tuple<SegmentND, TensorStat>>[] _ofmapRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2LIfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2RWRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _l2GOfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly Dictionary<Var, Nncase.TIR.Buffer> _ifBufferMap = new Dictionary<Var, Nncase.TIR.Buffer>(ReferenceEqualityComparer.Instance);

	private CcrHandler _ccrHandler = new CcrHandler();

	private WeightGroupHandler _weightGroup;

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private DataType? _inputType;

	private DataType? _outputType;

	private DataType? _weightType;

	private Call? _mm;

	private Call? _lif;

	private Call? _lw;

	private Call? _lact;

	private Call? _lwQarg;

	private Call? _sof;

	private GNNEShape? _inputShape;

	private GNNEShape? _outputShape;

	private GNNEShape? _weightsShape;

	private int _strideH;

	private int _strideW;

	private int _dilationH;

	private int _dilationW;

	private bool _broadcastW;

	private bool _broadcastIf;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNEMatMulFusion();


	private PrimFunction GetReplace(Call call, Call ld, Call st)
	{
		_count++;
		InitParameters(call, ld, st);
		TileMatMulGlb glb = SearchGlbParameters();
		int[] array = ((Call)_mm[GNNEMatMul.InputB])[GNNELoad.Input].CheckedShape.ToValueArray();
		List<Nncase.TIR.Buffer> list = new List<Nncase.TIR.Buffer>();
		Nncase.TIR.Buffer buffer;
		if (((Call)_mm[GNNEMatMul.InputB])[GNNELoad.Input] is TensorConst)
		{
			T.AttachBuffer((TensorConst)((Call)call[GNNEMatMul.InputB])[GNNELoad.Input], out buffer, "ddrIf");
		}
		else
		{
			T.CreateBuffer(new TensorType(((Call)_mm[GNNEMatMul.InputB])[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out buffer, "ddrIf");
			list.Add(buffer);
			_ifBufferMap.Add((Var)((Call)_mm[GNNEMatMul.InputB])[GNNELoad.Input], buffer);
		}
		int[] array2 = ((Call)_mm[GNNEMatMul.InputA])[GNNELoad.Input].CheckedShape.ToValueArray();
		Nncase.TIR.Buffer buffer2;
		if (((Call)_mm[GNNEMatMul.InputA])[GNNELoad.Input] is TensorConst)
		{
			T.AttachBuffer((TensorConst)((Call)call[GNNEMatMul.InputA])[GNNELoad.Input], out buffer2, "ddrW");
		}
		else
		{
			T.CreateBuffer(new TensorType(((Call)_mm[GNNEMatMul.InputA])[GNNELoad.Input].CheckedDataType, array2), MemoryLocation.Input, out buffer2, "ddrW");
			list.Add(buffer2);
			_ifBufferMap.Add((Var)((Call)_mm[GNNEMatMul.InputA])[GNNELoad.Input], buffer2);
		}
		if (list.Count == 2)
		{
			list.Reverse();
		}
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer3, "var ddrOf");
		list.Add(buffer3);
		T.AttachBuffer((TensorConst)((Call)call[GNNEMatMul.InputABias])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer4, "var ddrWQarg");
		T.AttachBuffer((TensorConst)((Call)call[GNNEMatMul.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer5, "var ddrAct");
		List<GnneAction> list2 = new List<GnneAction>();
		ItemRecStatusInit();
		BuildSchedule(glb, call, ld, st, buffer, buffer3, buffer2, buffer4, buffer5, weightGroupOnly: true, _weightGroup);
		list2 = BuildSchedule(glb, call, ld, st, buffer, buffer3, buffer2, buffer4, buffer5, weightGroupOnly: false, _weightGroup);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileMatMul_{_count}", K230RtModule.Kind, list.ToArray()).Body(actionToInstruct.Instructions(list2), I.END(GP_REGISTER.x0, 0)).Build();
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
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _outputShape[1]);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _outputShape[2]);
		List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(0, glb.GlbMap[ItemName.Ifmap].Dimensions[1], _weightsShape[3]);
		if (!weightGroupOnly)
		{
			gnneActionUpdater.UpdateLoadAct(_lact, ddrAct);
			if (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16)
			{
				for (int i = 0; i < (_broadcastW ? 1 : _outputShape[1]); i++)
				{
					gnneActionUpdater.UpdateLoadWQargMatmul(_lwQarg, weightGroup, ddrWQarg, i);
				}
			}
		}
		Segment1D segment1D = new Segment1D(..0, new Padding(0, 0));
		Segment1D segment1D2 = new Segment1D(..1, new Padding(0, 0));
		List<SegmentND> list2 = new List<SegmentND>();
		list2.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list2.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list3 = list2;
		list2 = new List<SegmentND>();
		list2.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list2.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list4 = list2;
		Segment1D segment1D3 = segment1D2;
		Segment1D segment1D4 = segment1D3;
		foreach (Segment1D item2 in segmentStartEndLength3)
		{
			foreach (Segment1D item3 in segmentStartEndLength)
			{
				foreach (Segment1D item4 in segmentStartEndLength2)
				{
					SegmentND psum = new SegmentND(item3, item2, segment1D3, item4);
					SegmentND segmentND = new SegmentND(segment1D2, item3, item2, item4);
					foreach (Segment1D item5 in segmentStartEndLength4)
					{
						Segment1D segment1D5 = item4;
						SegmentND weight = new SegmentND(item2, item5, segment1D2, segment1D2);
						SegmentND segmentND2 = new SegmentND(segment1D2, _broadcastW ? segment1D2 : item3, item2, item5);
						if (list4[0] == segmentND2)
						{
							num3 = 0;
						}
						else if (list4[1] == segmentND2)
						{
							num3 = 1;
						}
						else
						{
							num3 = (num3 + 1) % 2;
							if (!weightGroupOnly && _g2RWRec[num3].Count > 0 && _g2RWRec[num3][0].Item1 == segmentND2)
							{
								TensorStat item = _g2RWRec[num3][0].Item2;
								int value = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
								List<CcrSet> ccrsToSet = new List<CcrSet>
								{
									new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, num3)), value)
								};
								List<int> stridesD = new List<int>
								{
									segmentND2[1].Length,
									item2.Length,
									TileUtilities.GetAlignedNum(item5.Length, GNNEEnv.WAlignNum)
								};
								gnneActionUpdater.UpdateLoadIf(segmentND2, _lw, num3, ddrW, 0, stridesD, ItemName.Weight, ccrsToSet);
							}
							list4[num3] = segmentND2;
						}
						SegmentND ifmap = new SegmentND(_broadcastIf ? segment1D2 : item3, item5, segment1D4, segment1D5);
						SegmentND segmentND3 = new SegmentND(segment1D2, _broadcastIf ? segment1D2 : item3, item5, segment1D5);
						if (list3[0] == segmentND3)
						{
							num2 = 0;
						}
						else if (list3[1] == segmentND3)
						{
							num2 = 1;
						}
						else
						{
							num2 = (num2 + 1) % 2;
							if (!weightGroupOnly && _g2LIfRec[num2].Count > 0 && _g2LIfRec[num2][0].Item1 == segmentND3)
							{
								TensorStat item = _g2LIfRec[num2][0].Item2;
								int value2 = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
								List<CcrSet> ccrsToSet2 = new List<CcrSet>
								{
									new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, num2)), value2)
								};
								List<int> stridesD2 = new List<int>
								{
									glb.GlbMap[ItemName.Ifmap].Dimensions[0],
									glb.GlbMap[ItemName.Ifmap].Dimensions[1],
									glb.GlbMap[ItemName.Ifmap].Dimensions[3]
								};
								gnneActionUpdater.UpdateLoadIf(segmentND3, _lif, num2, ddrIf, 0, stridesD2, ItemName.Ifmap, ccrsToSet2);
							}
							list3[num2] = segmentND3;
						}
						BuildL1Schedule(gnneActionUpdater, glb, ifmap, weight, psum, num2, num, num3, weightGroupOnly, weightGroup);
					}
					if (!weightGroupOnly)
					{
						bool num4 = !_ofmapRec[num][0].Item2.IsLastSlice;
						_ofmapRec[num].RemoveAt(0);
						List<CcrSet> list5 = new List<CcrSet>();
						if ((num4 ? 1 : 0) > (false ? 1 : 0))
						{
							list5.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, num)), 1));
						}
						List<CcrClr> ccrsToClr = new List<CcrClr>
						{
							new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, num)))
						};
						List<int> stridesS = new List<int>
						{
							glb.GlbMap[ItemName.Ofmap].Dimensions[0],
							glb.GlbMap[ItemName.Ofmap].Dimensions[1],
							glb.GlbMap[ItemName.Ofmap].Dimensions[3]
						};
						gnneActionUpdater.UpdateStoreT(segmentND, _sof, num, ddrOf, 0, null, list5, ccrsToClr, ItemName.Ofmap, stridesS);
					}
					else
					{
						_ofmapRec[num].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
					}
					num = (num + 1) % nPingPongSplit;
				}
			}
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileMatMul.cs", 309);
		return list;
	}

	private List<int> L1Search(TiledGlb glb, SegmentND weight, SegmentND psum)
	{
		int item = Math.Min(GNNEEnv.PuHeight, _weightsShape[3]);
		int item2 = Math.Min(GNNEEnv.PuWidth, _weightsShape[2]);
		int h = 1;
		int w = 1;
		int e = 1;
		int f = 1;
		int item3 = 1;
		int item4 = 1;
		int psumPingPangSplit = 1;
		int ifBytesPerElementGlb = TileUtilities.GetBytesPerElement(_inputType);
		if (!HandleL1Allocate(h, w, e, f, psumPingPangSplit, ifBytesPerElementGlb))
		{
			throw new NotSupportedException("exceeds L1 size");
		}
		while (f < psum[3].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1Size / GNNEEnv.PuHeight / 2 && IncreaseFBy1())
		{
		}
		return new List<int> { item, item2, e, f, item3, item4 };
		bool IncreaseFBy1()
		{
			int num = f + 1;
			int num2 = num;
			bool num3 = HandleL1Allocate(h, num2, e, num, psumPingPangSplit, ifBytesPerElementGlb);
			if (num3)
			{
				f = num;
				w = num2;
			}
			return num3;
		}
	}

	private bool HandleL1Allocate(int h, int w, int e, int f, int psumPingPangSplit, int ifBytesPerElementGlb)
	{
		if (e * f > GNNEEnv.PsumL1ElePerChan / psumPingPangSplit)
		{
			return false;
		}
		if (GNNEEnv.PuHeight * h * w * ifBytesPerElementGlb > GNNEEnv.IfL1Size)
		{
			return false;
		}
		return true;
	}

	private void BuildL1Schedule(GnneActionUpdater actionUpdater, TiledGlb glb, SegmentND ifmap, SegmentND weight, SegmentND psum, int iPp, int ofPp, int wPp, bool weightGroupOnly, WeightGroupHandler weightGroup)
	{
		int l1Pp = 0;
		List<int> list = L1Search(glb, weight, psum);
		TileUtilities.GetSegmentStartEndLength(psum[2].Start, list[2], psum[2].End);
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[3].Start, list[3], psum[3].End);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(psum[0].Start, 1, psum[0].End);
		TileUtilities.GetSegmentStartEndLength(weight[2].Start, list[4], weight[2].End);
		TileUtilities.GetSegmentStartEndLength(weight[3].Start, list[5], weight[3].End);
		List<List<Segment1D>> l1McSeg = GetL1McSeg(ifmap, psum, list[1], list[0]);
		List<Segment1D> list2 = l1McSeg[0];
		List<Segment1D> list3 = l1McSeg[1];
		Segment1D segment1D = new Segment1D(..1, new Padding(0, 0));
		SegmentND item = new SegmentND(segment1D, _broadcastW ? segment1D : psum[0], weight[0], weight[1]);
		SegmentND item2 = new SegmentND(segment1D, _broadcastIf ? segment1D : psum[0], weight[1], ifmap[3]);
		SegmentND item3 = new SegmentND(segment1D, psum[0], psum[1], psum[3]);
		foreach (Segment1D item4 in list2)
		{
			foreach (Segment1D item5 in segmentStartEndLength2)
			{
				Segment1D segment1D2 = segment1D;
				Segment1D segment1D3 = segment1D;
				foreach (Segment1D item6 in segmentStartEndLength)
				{
					Segment1D segment1D4 = item6;
					new SegmentND(item5, item4, segment1D2, item6);
					SegmentND segmentND = new SegmentND(item5, item4, segment1D3, segment1D4);
					bool flag = true;
					foreach (Segment1D item7 in list3)
					{
						Segment1D current4;
						Segment1D segment1D5 = (current4 = item7);
						Segment1D segment1D6 = segment1D;
						Segment1D segment1D7 = segment1D;
						Segment1D segment1D8 = segment1D5;
						Segment1D segment1D9 = item5;
						Segment1D segment1D10 = item4;
						SegmentND x = new SegmentND(_broadcastIf ? segment1D : segment1D9, segment1D8, segment1D3, segment1D4);
						SegmentND w = new SegmentND(item4, current4, segment1D6, segment1D7);
						bool flag2 = false;
						SegmentND segmentND2 = TileUtilities.ShiftInputTensor(in x, in w, 1, 1, _strideH, _strideW, _dilationH, _dilationW);
						if (segmentND2[0].Length > 0 && segmentND2[1].Length > 0 && segmentND2[2].Length > 0 && segmentND2[3].Length > 0)
						{
							flag2 = true;
							if (!weightGroupOnly)
							{
								int num = _g2LIfRec[iPp][0].Item2.Stat_cnt();
								_g2LIfRec[iPp].RemoveAt(0);
								List<CcrClr> list4 = new List<CcrClr>();
								if (num > 0)
								{
									list4.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, iPp))));
								}
								actionUpdater.UpdateG2LIf(segmentND2, ifmap, _lif, iPp, null, list4, 0, _inputType, ItemName.Ifmap, restored: false, 0, 0, 0, h2c: false, 1, _strideH);
							}
							else
							{
								_g2LIfRec[iPp].Add(new Tuple<SegmentND, TensorStat>(item2, new TensorStat(isFirstSlice: false, isLastSlice: false)));
							}
						}
						Segment1D segment1D11 = segment1D6;
						Segment1D segment1D12 = segment1D7;
						SegmentND w2 = new SegmentND(segment1D10, current4, segment1D11, segment1D12);
						SegmentND segmentND3 = TileUtilities.ShiftInputTensor(in x, in w2, 1, 1, _strideH, _strideW, _dilationH, _dilationW);
						int num2 = ((!(_weightType == DataTypes.Int16)) ? 1 : 2);
						int num3 = ((!(_inputType == DataTypes.Int16)) ? 1 : 2);
						for (int i = 0; i < num2; i++)
						{
							for (int j = 0; j < num3; j++)
							{
								if (!weightGroupOnly)
								{
									int num4 = _g2RWRec[wPp][0].Item2.Stat_cnt();
									_g2RWRec[wPp].RemoveAt(0);
									List<CcrClr> list5 = new List<CcrClr>();
									if (num4 > 0)
									{
										list5.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, wPp))));
									}
									actionUpdater.UpdateG2RWMatmul(w2, weight, weightGroup, _weightsShape[2], _lw, wPp, i, null, list5, (!_broadcastW) ? (item5.Start - segmentStartEndLength2[0].Start) : 0);
								}
								else
								{
									if (item5.Start == 0)
									{
										weightGroup.UpdateWeightGroup(w2);
									}
									_g2RWRec[wPp].Add(new Tuple<SegmentND, TensorStat>(item, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
								if (!weightGroupOnly)
								{
									actionUpdater.UpdateL2RIf(segmentND3, segmentND2, _strideH, _strideW, _weightsShape[3], _lif, 0, j, ((TensorConst)_mm[GNNEMatMul.DeqBBias]).Value.ToArray<byte>()[segmentND3[0].Start], _inputType, h2c: false, 1);
								}
								bool releaseIf = false;
								if (i == num2 - 1 && j == num3 - 1 && flag2)
								{
									releaseIf = true;
									flag2 = false;
								}
								bool loopStart = false;
								if (flag && i == 0 && j == 0)
								{
									loopStart = true;
									flag = false;
								}
								bool flag3 = current4.End == _weightsShape[3] && segment1D12.End == weight[3].End && segment1D11.End == weight[2].End && i == num2 - 1 && j == num3 - 1;
								DataType outputType = _outputType;
								ACT0_OUTPUT_DEST aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.dm;
								if (!weightGroupOnly)
								{
									int shift = ((TensorConst)_mm[GNNEMatMul.ShiftBits]).Value.ToScalar<int>();
									int num5 = 0;
									int num6 = 0;
									if (flag3)
									{
										Tuple<SegmentND, TensorStat> tuple = _l2GOfRec[ofPp][0];
										_l2GOfRec[ofPp].RemoveAt(0);
										if (tuple.Item2.IsFirstSlice && !_ofmapRec[ofPp][0].Item2.IsFirstSlice)
										{
											num6 = 1;
										}
										if (tuple.Item2.IsLastSlice)
										{
											num5 = 1;
										}
									}
									List<CcrSet> list6 = new List<CcrSet>();
									if (num5 > 0)
									{
										list6.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, ofPp)), 1));
									}
									List<CcrClr> list7 = new List<CcrClr>();
									if (num6 > 0)
									{
										list7.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, ofPp))));
									}
									int offsetAct = item5.Start * glb.GlbMap[ItemName.Act].GlbNByte / _outputShape[1];
									aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.dm;
									actionUpdater.UpdateR2LPsum(shift, segmentND, segmentND, psum, ofPp, l1Pp, aCT0_OUTPUT_DEST, releaseIf, Math.Max(i, j), TcuComputeMode.MatMul, loopStart, flag3, _strideH, _strideW, _weightsShape[2], _inputType, _weightType, outputType, _lact.CheckedDataType, list6, list7, offsetAct);
								}
								else if (flag3 && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
								{
									_l2GOfRec[ofPp].Add(new Tuple<SegmentND, TensorStat>(item3, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
							}
						}
					}
				}
			}
		}
	}

	private List<List<Segment1D>> GetL1McSeg(SegmentND ifmap, SegmentND psum, int mInloop, int cInloop)
	{
		List<Segment1D> list = new List<Segment1D>();
		List<Segment1D> list2 = new List<Segment1D>();
		list = TileUtilities.GetSegmentStartEndLength(psum[1].Start, mInloop, psum[1].End);
		list2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start, cInloop, ifmap[1].End);
		return new List<List<Segment1D>> { list, list2 };
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
		g2LIfRec = _ofmapRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list4 in g2LIfRec)
		{
			if (list4.Count > 0)
			{
				list4[0].Item2.IsFirstSlice = true;
				list4[list4.Count - 1].Item2.IsLastSlice = true;
			}
		}
	}

	private void ItemRecStatusInit()
	{
		for (int i = 0; i < _ofmapRec.Length; i++)
		{
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
		}
	}

	private void InitParameters(Call convNode, Call ld, Call st)
	{
		_mm = convNode;
		_lif = (Call)_mm[GNNEMatMul.InputB];
		_lw = (Call)_mm[GNNEMatMul.InputA];
		_lact = (Call)_mm[GNNEMatMul.Act];
		_lwQarg = (Call)_mm[GNNEMatMul.InputABias];
		_sof = st;
		_inputShape = new GNNEShape(_lif.CheckedShape[0].FixedValue, _lif.CheckedShape[1].FixedValue, _lif.CheckedShape[2].FixedValue, _lif.CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(_mm.CheckedShape[0].FixedValue, _mm.CheckedShape[1].FixedValue, _mm.CheckedShape[2].FixedValue, _mm.CheckedShape[3].FixedValue);
		_weightsShape = new GNNEShape(_lw.CheckedShape[0].FixedValue, _lw.CheckedShape[1].FixedValue, _lw.CheckedShape[2].FixedValue, _lw.CheckedShape[3].FixedValue);
		_strideH = 1;
		_strideW = 1;
		_dilationH = 1;
		_dilationW = 1;
		_inputType = _lif.CheckedDataType;
		_weightType = _lw.CheckedDataType;
		_weightGroup = new WeightGroupHandler(_weightType, _weightType);
		_outputType = _mm.CheckedDataType;
		_broadcastW = _weightsShape[1] == 1;
		_broadcastIf = _inputShape[1] == 1;
	}

	private TileMatMulGlb SearchGlbParameters()
	{
		int i = 1;
		int j = 1;
		int r = 1;
		int s = 1;
		int num = 1;
		int num2 = Math.Min(GNNEEnv.PuWidth, _outputShape[3]);
		int h = 1;
		int w = num2;
		int k = 1;
		AllocateResult allocateResult;
		for (; j < _weightsShape[3]; j++)
		{
			allocateResult = HandleAllocate(i, j + 1, h, w, r, s, k, num, num2);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; k < GNNEEnv.PuWidth && k < _weightsShape[2]; k++)
		{
			allocateResult = HandleAllocate(i, j, h, w, r, s, k + 1, num, num2);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		while (num2 < _outputShape[3])
		{
			int num3 = num2 + 1;
			int num4 = num3;
			allocateResult = HandleAllocate(i, j, h, num4, r, s, k, num, num3);
			if (!allocateResult.IsOk)
			{
				break;
			}
			num2 = num3;
			w = num4;
		}
		for (; k < _weightsShape[2]; k++)
		{
			allocateResult = HandleAllocate(i, j, h, w, r, s, k + 1, num, num2);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; i < _outputShape[1]; i++)
		{
			allocateResult = HandleAllocate(i + 1, j, h, w, r, s, k, num, num2);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		if (k % GNNEEnv.PuWidth != 0 && k > GNNEEnv.PuWidth)
		{
			k = k / GNNEEnv.PuWidth * GNNEEnv.PuWidth;
		}
		allocateResult = HandleAllocate(i, j, h, w, r, s, k, num, num2, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileMatMul.cs", 820);
	GNNEShape gNNEShape = new GNNEShape(i, k, num, num2);
		System.IO.File.AppendAllText("/tmp/k230_srv/tiledbg.log", "TILEDBG MatMul MatMul glb=(i=" + i + ",k=" + k + ",num=" + num + ",num2=" + num2 + ") j=" + j);
		return new TileMatMulGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
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
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4]
		{
			_broadcastIf ? 1 : n,
			c,
			h,
			w
		}, _inputType, 0);
		int num2 = tensorOnGlb.AllocatedBytes * num;
		list2[0] += num2;
		num2 = TileUtilities.GetAlignedNum(num2, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		list2[1] += num2;
		int num3 = (_broadcastW ? m : (m * n));
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { num3, c, r, s }, _weightType, 0);
		int num4 = SpaceSearcher.GetWeightSize(r, s, c, num3, TileUtilities.GetBytesPerElement(_weightType)) * GNNEEnv.NPingPongSplit;
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
			_outputShape[1],
			_outputShape[2],
			GNNEEnv.ActNumPerChan
		}, _lact.CheckedDataType, 0);
		int actSize = SpaceSearcher.GetActSize(_outputShape[1] * _outputShape[2], GNNEEnv.ActNumPerChan, TileUtilities.GetBytesPerElement(_lact.CheckedDataType));
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
			TensorOnGlb value2 = new TensorOnGlb(new int[4]
			{
				1,
				1,
				_broadcastW ? 1 : _outputShape[1],
				_weightsShape[2]
			}, _lwQarg.CheckedDataType, 0);
			int num6 = (_broadcastW ? 1 : _outputShape[1]) * SpaceSearcher.GetWQargSize(_weightsShape[2], TileUtilities.GetBytesPerElement(_lwQarg.CheckedDataType));
			list2[0] += num6;
			num6 = TileUtilities.GetAlignedNum(num6, GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += num6;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.WQargBankWidth,
				num6 / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
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
