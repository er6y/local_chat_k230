using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileConv2D : RewriteRule<Pattern>
{
	private static int _count = -1;

	private readonly List<Tuple<SegmentND, TensorStat>>[] _weightRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _ofmapRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2LIfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2RWRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _l2GOfRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _l2RIf2Rec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly List<Tuple<SegmentND, TensorStat>>[] _g2RWSliceRec = new List<Tuple<SegmentND, TensorStat>>[2];

	private readonly Dictionary<Var, Nncase.TIR.Buffer> _ifBufferMap = new Dictionary<Var, Nncase.TIR.Buffer>(ReferenceEqualityComparer.Instance);

	private readonly List<WeightGroupHandler> _weightGroups = new List<WeightGroupHandler>(2);

	private CcrHandler _ccrHandler = new CcrHandler();

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private GlbSearchStrategy _strategy;

	private DataType? _inputType;

	private DataType? _outputType;

	private DataType? _weightType;

	private Call? _conv;

	private Call? _pool;

	private Call? _dw;

	private Call? _act1;

	private int _icPerGroup;

	private int _ocPerGroup;

	private int _groupPerPass;

	private Call? _lif;

	private Call? _lif2;

	private Call? _lw;

	private Call? _lact;

	private Call? _lwQarg;

	private Call? _sof;

	private bool _weightPreloadEn;

	private bool _h2C;

	private GNNEShape? _inputShape;

	private GNNEShape? _outputShape;

	private GNNEShape? _convOutputShape;

	private GNNEShape? _weightsShape;

	private Padding? _paddingH;

	private Padding? _paddingW;

	private int _strideH;

	private int _strideW;

	private int _dilationH;

	private int _dilationW;

	private int _groups;

	private int _fusedKernelH;

	private int _fusedKernelW;

	private Padding? _fusedPaddingH;

	private Padding? _fusedPaddingW;

	private int _fusedStrideH;

	private int _fusedStrideW;

	private int _fusedDilationH;

	private int _fusedDilationW;

	private Tuple<int, int>? _weightSplitPattern;

	public override OrPattern Pattern { get; } = Nncase.PatternMatch.Utility.IsAlt(FusionPattern.IsL1Pdp0DwFusion(), FusionPattern.IsL1Pdp0ReduceFusion(), FusionPattern.IsL1Act1Fusion(), FusionPattern.IsGNNEConvFusion());


	private PrimFunction GetReplace(Call call, Call ld, Call st, Fusion fusion)
	{
		_count++;
		InitParameters(call, ld, st);
		TileConv2DGlb glb = SearchGlbParameters();
		int[] array = ld.Arguments[0].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		List<Nncase.TIR.Buffer> list = new List<Nncase.TIR.Buffer> { buffer };
		_ifBufferMap.Add((Var)ld[GNNELoad.Input], buffer);
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		byte[] array2 = ((TensorConst)((Call)call[GNNEConv2D.Weights])[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
		byte[] array3 = new byte[array2.Length];
		Array.Copy(array2, array3, array3.Length);
		T.AttachBuffer(Const.FromTensor(Tensor.FromBytes(DataTypes.UInt8, array3.ToArray(), new int[1] { array3.Length })), out Nncase.TIR.Buffer buffer3, "var ddrW");
		T.AttachBuffer((TensorConst)((Call)call[GNNEConv2D.WeightsBias])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer4, "var ddrWQarg");
		T.AttachBuffer((TensorConst)((Call)call[GNNEConv2D.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer5, "var ddrAct");
		Nncase.TIR.Buffer buffer6 = null;
		Nncase.TIR.Buffer buffer7 = null;
		Nncase.TIR.Buffer buffer8 = null;
		Nncase.TIR.Buffer buffer9 = null;
		Nncase.TIR.Buffer buffer10 = null;
		Nncase.TIR.Buffer buffer11 = null;
		if ((object)_dw != null)
		{
			byte[] array4 = ((TensorConst)((Call)_dw[GNNEPdp0DW.Weights])[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
			byte[] array5 = new byte[array4.Length];
			Array.Copy(array4, array5, array5.Length);
			T.AttachBuffer(Const.FromTensor(Tensor.FromBytes(DataTypes.UInt8, array5.ToArray(), new int[1] { array5.Length })), out buffer6, "ddrDW");
			T.AttachBuffer((TensorConst)((Call)_dw[GNNEPdp0DW.WeightsBias])[GNNELoadW.Input], out buffer7, "ddrDWQarg");
			T.AttachBuffer((TensorConst)((Call)_dw[GNNEPdp0DW.Act])[GNNELoadW.Input], out buffer8, "ddrDWAct");
		}
		if ((object)_pool != null)
		{
			T.AttachBuffer((TensorConst)((Call)_pool[GNNEPdp0Reduce.Act])[GNNELoadW.Input], out buffer10, "ddrPdpAct");
		}
		if ((object)_act1 != null)
		{
			T.AttachBuffer((TensorConst)((Call)_act1[GNNEActivation.Act])[GNNELoadW.Input], out buffer9, "ddrAct1");
			if ((object)_lif2 != null)
			{
				if (!(_lif2[GNNELoad.Input] is TensorConst))
				{
					buffer11 = buffer;
					if (_lif2[GNNELoad.Input] != _lif[GNNELoad.Input])
					{
						T.CreateBuffer(new TensorType(_lif2[GNNELoad.Input].CheckedDataType, _lif2.CheckedShape), MemoryLocation.Input, out buffer11, "ddrIf2");
						list.Add(buffer11);
						_ifBufferMap.Add((Var)_lif2[GNNELoad.Input], buffer11);
					}
				}
				else
				{
					T.AttachBuffer((TensorConst)_lif2[GNNELoad.Input], out buffer11, "ddrIf2");
				}
			}
		}
		list = (from v in fusion.Parameters.AsValueEnumerable()
			select _ifBufferMap[v]).ToList();
		list.Add(buffer2);
		ItemRecStatusInit();
		BuildIfFirstSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, buffer10, buffer11, weightGroupOnly: true, _weightGroups[0]);
		List<GnneAction> list2 = BuildIfFirstSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, buffer10, buffer11, weightGroupOnly: false, _weightGroups[0]);
		ItemRecStatusInit();
		BuildWFirstSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, buffer10, buffer11, weightGroupOnly: true, _weightGroups[1]);
		List<GnneAction> list3 = BuildWFirstSchedule(glb, call, ld, st, buffer, buffer2, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, buffer10, buffer11, weightGroupOnly: false, _weightGroups[1]);
		ScheduleStrategy scheduleStrategy = DetermineScheduleStrategy(list2, list3);
		List<GnneAction> actions = ((scheduleStrategy == ScheduleStrategy.IfFirstSchedule) ? list2 : list3);
		Span<byte> bytesBuffer = buffer3.Const().Value.BytesBuffer;
		ArrangeWeights(_weightType, _weightsShape, bytesBuffer, _weightGroups[(int)scheduleStrategy]);
		if ((object)_dw != null)
		{
			ArrangeDwWeights(_dw[GNNEPdp0DW.Weights].CheckedDataType, _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray(), buffer6.Const().Value.BytesBuffer, _weightGroups[(int)scheduleStrategy]);
		}
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileConv2d_{_count}", K230RtModule.Kind, list.ToArray()).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private List<GnneAction> BuildIfFirstSchedule(TiledGlb glb, Call convCall, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf, Nncase.TIR.Buffer ddrW, Nncase.TIR.Buffer ddrWQarg, Nncase.TIR.Buffer ddrAct, Nncase.TIR.Buffer ddrDW, Nncase.TIR.Buffer ddrDWQarg, Nncase.TIR.Buffer ddrDWAct, Nncase.TIR.Buffer ddrAct1, Nncase.TIR.Buffer ddrPdpAct, Nncase.TIR.Buffer ddrIf2, bool weightGroupOnly, WeightGroupHandler weightGroup)
	{
		List<GnneAction> list = new List<GnneAction>();
		_gpr = new GprHandler(GNNEEnv.GprNum);
		_ssr = new SsrHandler(GNNEEnv.SsrNum);
		_ccrHandler = new CcrHandler();
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
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _outputShape[2]);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]);
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
		if (!weightGroupOnly)
		{
			if (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16)
			{
				gnneActionUpdater.UpdateLoadWQarg(_lwQarg, weightGroup, ddrWQarg);
			}
			gnneActionUpdater.UpdateLoadAct(_lact, ddrAct);
			if ((object)_dw != null)
			{
				gnneActionUpdater.UpdateLoadDw(_dw, weightGroup, ddrDW);
				if (_dw[GNNEPdp0DW.Weights].CheckedDataType == DataTypes.UInt8)
				{
					gnneActionUpdater.UpdateLoadDwQarg(_dw, weightGroup, ddrDWQarg);
				}
			}
			if ((object)_act1 != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_act1[GNNEActivation.Act], ddrAct1, ItemName.MfuAct1);
			}
			if ((object)_dw != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_dw[GNNEPdp0DW.Act], ddrDWAct, ItemName.DwAct1);
			}
			else if ((object)_pool != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_pool[GNNEPdp0Reduce.Act], ddrPdpAct, ItemName.PdpAct1);
			}
		}
		Segment1D segment1D = new Segment1D(0..0, new Padding(0, 0));
		List<SegmentND> list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list5 = list4;
		list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list6 = list4;
		int l1Pp = 0;
		_weightSplitPattern = new Tuple<int, int>(0, 0);
		foreach (Segment1D item2 in segmentStartEndLength)
		{
			foreach (Segment1D item3 in segmentStartEndLength2)
			{
				Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(item3.Start, item3.Length, _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
				Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(inputRowSegment.Start, inputRowSegment.Length, _inputShape[2], _weightsShape[2], _strideH, _dilationH, in _paddingH);
				foreach (Segment1D item4 in segmentStartEndLength3)
				{
					Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(item4.Start, item4.Length, _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
					Segment1D inputColumnSegment2 = TileUtilities.GetInputColumnSegment(inputColumnSegment.Start, inputColumnSegment.Length, _inputShape[3], _weightsShape[3], _strideW, _dilationW, in _paddingW);
					foreach (Segment1D item5 in list2)
					{
						SegmentND segmentND = new SegmentND(item2, item5, item3, item4);
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
							SegmentND segmentND2 = new SegmentND(item5, new Segment1D(num6..num7, new Padding(0, 0)), new Segment1D(0.._weightsShape[2], new Padding(0, 0)), new Segment1D(0.._weightsShape[3], new Padding(0, 0)));
							if (list6[0] == segmentND2)
							{
								num3 = 0;
							}
							else if (list6[1] == segmentND2)
							{
								num3 = 1;
							}
							else
							{
								num3 = (num3 + 1) % 2;
								weightGroup.Current_aligned_offset_init();
								if (weightGroupOnly)
								{
									_weightRec[num3].Add(new Tuple<SegmentND, TensorStat>(segmentND2, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
								list6[num3] = segmentND2;
							}
							SegmentND segmentND3 = new SegmentND(item2, item6, inputRowSegment2, inputColumnSegment2);
							if (list5[0] == segmentND3)
							{
								num2 = 0;
							}
							else if (list5[1] == segmentND3)
							{
								num2 = 1;
							}
							else
							{
								num2 = (num2 + 1) % 2;
								if (!weightGroupOnly && _g2LIfRec[num2].Count > 0 && _g2LIfRec[num2][0].Item1 == segmentND3)
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
									gnneActionUpdater.UpdateLoadIf(segmentND3, _lif, num2, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet, null, _h2C, ((TensorConst)convCall[GNNEConv2D.DeqBias]).Value.ToScalar<int>());
								}
								list5[num2] = segmentND3;
							}
							SegmentND segmentND4 = segmentND;
							if (!weightGroupOnly)
							{
								bool flag = _inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1;
								if ((object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default && (item6.End == _inputShape[1] || flag))
								{
									TensorStat item = _l2RIf2Rec[num][0].Item2;
									int value2 = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
									List<CcrSet> ccrsToSet2 = new List<CcrSet>
									{
										new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2, num)), value2)
									};
									List<int> stridesD2 = new List<int>
									{
										glb.GlbMap[ItemName.Ifmap2].Dimensions[1],
										glb.GlbMap[ItemName.Ifmap2].Dimensions[2],
										glb.GlbMap[ItemName.Ifmap2].Dimensions[3]
									};
									gnneActionUpdater.UpdateLoadIf(segmentND4, _lif2, num, ddrIf2, 0, stridesD2, ItemName.Ifmap2, ccrsToSet2);
								}
							}
							BuildL1Schedule(gnneActionUpdater, glb, segmentND3, segmentND2, segmentND, num2, num, num3, segmentND4, weightGroupOnly, weightGroup, ref l1Pp, ddrW);
						}
						if (!weightGroupOnly)
						{
							bool num8 = !_ofmapRec[num][0].Item2.IsLastSlice;
							_ofmapRec[num].RemoveAt(0);
							List<CcrSet> list7 = new List<CcrSet>();
							if ((num8 ? 1 : 0) > (false ? 1 : 0))
							{
								list7.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, num)), 1));
							}
							List<CcrClr> ccrsToClr = new List<CcrClr>
							{
								new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, num)))
							};
							gnneActionUpdater.UpdateStoreT(segmentND, _sof, num, ddrOf, 0, null, list7, ccrsToClr);
						}
						else
						{
							_ofmapRec[num].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
						}
						num = (num + 1) % nPingPongSplit;
					}
				}
			}
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 416);
		return list;
	}

	private List<GnneAction> BuildWFirstSchedule(TiledGlb glb, Call convCall, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf, Nncase.TIR.Buffer ddrW, Nncase.TIR.Buffer ddrWQarg, Nncase.TIR.Buffer ddrAct, Nncase.TIR.Buffer ddrDW, Nncase.TIR.Buffer ddrDWQarg, Nncase.TIR.Buffer ddrDWAct, Nncase.TIR.Buffer ddrAct1, Nncase.TIR.Buffer ddrPdpAct, Nncase.TIR.Buffer ddrIf2, bool weightGroupOnly, WeightGroupHandler weightGroup)
	{
		List<GnneAction> list = new List<GnneAction>();
		_gpr = new GprHandler(GNNEEnv.GprNum);
		_ssr = new SsrHandler(GNNEEnv.SsrNum);
		_ccrHandler = new CcrHandler();
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
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _outputShape[2]);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]);
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
		if (!weightGroupOnly)
		{
			if (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16)
			{
				gnneActionUpdater.UpdateLoadWQarg(_lwQarg, weightGroup, ddrWQarg);
			}
			gnneActionUpdater.UpdateLoadAct(_lact, ddrAct);
			if ((object)_dw != null)
			{
				gnneActionUpdater.UpdateLoadDw(_dw, weightGroup, ddrDW);
				if (_dw[GNNEPdp0DW.Weights].CheckedDataType == DataTypes.UInt8)
				{
					gnneActionUpdater.UpdateLoadDwQarg(_dw, weightGroup, ddrDWQarg);
				}
			}
			if ((object)_act1 != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_act1[GNNEActivation.Act], ddrAct1, ItemName.MfuAct1);
			}
			if ((object)_dw != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_dw[GNNEPdp0DW.Act], ddrDWAct, ItemName.DwAct1);
			}
			else if ((object)_pool != null)
			{
				gnneActionUpdater.UpdateLoadAct((Call)_pool[GNNEPdp0Reduce.Act], ddrPdpAct, ItemName.PdpAct1);
			}
		}
		Segment1D segment1D = new Segment1D(0..0, new Padding(0, 0));
		List<SegmentND> list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list5 = list4;
		list4 = new List<SegmentND>();
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		list4.Add(new SegmentND(segment1D, segment1D, segment1D, segment1D));
		List<SegmentND> list6 = list4;
		int l1Pp = 0;
		_weightSplitPattern = new Tuple<int, int>(0, 0);
		foreach (Segment1D item2 in list2)
		{
			int num4 = item2.Start / _ocPerGroup * _icPerGroup;
			int num5 = (item2.End - 1) / _ocPerGroup * _icPerGroup + _icPerGroup;
			foreach (Segment1D item3 in segmentStartEndLength)
			{
				foreach (Segment1D item4 in segmentStartEndLength2)
				{
					Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(item4.Start, item4.Length, _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
					Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(inputRowSegment.Start, inputRowSegment.Length, _inputShape[2], _weightsShape[2], _strideH, _dilationH, in _paddingH);
					foreach (Segment1D item5 in segmentStartEndLength3)
					{
						Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(item5.Start, item5.Length, _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
						Segment1D inputColumnSegment2 = TileUtilities.GetInputColumnSegment(inputColumnSegment.Start, inputColumnSegment.Length, _inputShape[3], _weightsShape[3], _strideW, _dilationW, in _paddingW);
						SegmentND segmentND = new SegmentND(item3, item2, item4, item5);
						foreach (Segment1D item6 in list3)
						{
							if (item6.Start < num4 || item6.End > num5)
							{
								continue;
							}
							int num6 = item6.Start % _icPerGroup;
							int num7 = Math.Min(num6 + item6.Length, _icPerGroup);
							SegmentND segmentND2 = new SegmentND(item2, new Segment1D(num6..num7, new Padding(0, 0)), new Segment1D(0.._weightsShape[2], new Padding(0, 0)), new Segment1D(0.._weightsShape[3], new Padding(0, 0)));
							if (list6[0] == segmentND2)
							{
								num3 = 0;
							}
							else if (list6[1] == segmentND2)
							{
								num3 = 1;
							}
							else
							{
								num3 = (num3 + 1) % 2;
								weightGroup.Current_aligned_offset_init();
								if (weightGroupOnly)
								{
									_weightRec[num3].Add(new Tuple<SegmentND, TensorStat>(segmentND2, new TensorStat(isFirstSlice: false, isLastSlice: false)));
								}
								list6[num3] = segmentND2;
							}
							SegmentND segmentND3 = new SegmentND(item3, item6, inputRowSegment2, inputColumnSegment2);
							if (list5[0] == segmentND3)
							{
								num2 = 0;
							}
							else if (list5[1] == segmentND3)
							{
								num2 = 1;
							}
							else
							{
								num2 = (num2 + 1) % 2;
								if (!weightGroupOnly && _g2LIfRec[num2].Count > 0 && _g2LIfRec[num2][0].Item1 == segmentND3)
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
									gnneActionUpdater.UpdateLoadIf(segmentND3, _lif, num2, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet, null, _h2C, ((TensorConst)convCall[GNNEConv2D.DeqBias]).Value.ToScalar<int>());
								}
								list5[num2] = segmentND3;
							}
							SegmentND segmentND4 = segmentND;
							if (!weightGroupOnly)
							{
								bool flag = _inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1;
								if ((object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default && (item6.End == _inputShape[1] || flag))
								{
									TensorStat item = _l2RIf2Rec[num][0].Item2;
									int value2 = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
									List<CcrSet> ccrsToSet2 = new List<CcrSet>
									{
										new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2, num)), value2)
									};
									List<int> stridesD2 = new List<int>
									{
										glb.GlbMap[ItemName.Ifmap2].Dimensions[1],
										glb.GlbMap[ItemName.Ifmap2].Dimensions[2],
										glb.GlbMap[ItemName.Ifmap2].Dimensions[3]
									};
									gnneActionUpdater.UpdateLoadIf(segmentND4, _lif2, num, ddrIf2, 0, stridesD2, ItemName.Ifmap2, ccrsToSet2);
								}
							}
							BuildL1Schedule(gnneActionUpdater, glb, segmentND3, segmentND2, segmentND, num2, num, num3, segmentND4, weightGroupOnly, weightGroup, ref l1Pp, ddrW);
						}
						if (!weightGroupOnly)
						{
							bool num8 = !_ofmapRec[num][0].Item2.IsLastSlice;
							_ofmapRec[num].RemoveAt(0);
							List<CcrSet> list7 = new List<CcrSet>();
							if ((num8 ? 1 : 0) > (false ? 1 : 0))
							{
								list7.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, num)), 1));
							}
							List<CcrClr> ccrsToClr = new List<CcrClr>
							{
								new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, num)))
							};
							gnneActionUpdater.UpdateStoreT(segmentND, _sof, num, ddrOf, 0, null, list7, ccrsToClr);
						}
						else
						{
							_ofmapRec[num].Add(new Tuple<SegmentND, TensorStat>(segmentND, new TensorStat(isFirstSlice: false, isLastSlice: false)));
						}
						num = (num + 1) % nPingPongSplit;
					}
				}
			}
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 654);
		return list;
	}

	private TileConv2DGlb SearchGlbParameters()
	{
		int i = 1;
		int j = 1;
		int num = _weightsShape[2];
		int num2 = _weightsShape[3];
		int num3 = 1;
		int num4 = Math.Min(GNNEEnv.PuWidth, _outputShape[3]);
		if (num4 < GNNEEnv.PuWidth)
		{
			num3 = Math.Min((int)Math.Ceiling(1.0 * (double)GNNEEnv.PuWidth / (double)num4), _outputShape[2]);
		}
		int num5 = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num3, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH), _inputShape[2], num, _convOutputShape[2], _strideH, _dilationH, in _paddingH);
		int num6 = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num4, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW), _inputShape[3], num2, _convOutputShape[3], _strideW, _dilationW, in _paddingW);
		int k = 1;
		AllocateResult allocateResult;
		for (; j < _groupPerPass * _icPerGroup; j++)
		{
			allocateResult = HandleAllocate(i, j + 1, num5, num6, num, num2, k, num3, num4);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; k < GNNEEnv.PuWidth && k < _groupPerPass * _ocPerGroup; k++)
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
			while (num3 < _outputShape[2])
			{
				int num11 = num3 + 1;
				int inputHeight = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num11, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH), _inputShape[2], num, _convOutputShape[2], _strideH, _dilationH, in _paddingH);
				allocateResult = HandleAllocate(i, j, inputHeight, num6, num, num2, k, num11, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num3 = num11;
				num5 = inputHeight;
			}
			while (num4 < _outputShape[3])
			{
				int num12 = num4 + 1;
				int inputHeight2 = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num12, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW), _inputShape[3], num2, _convOutputShape[3], _strideW, _dilationW, in _paddingW);
				allocateResult = HandleAllocate(i, j, num5, inputHeight2, num, num2, k, num3, num12);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num4 = num12;
				num6 = inputHeight2;
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
			while (num3 < _outputShape[2])
			{
				int num13 = num3 + 1;
				int inputHeight3 = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num13, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH), _inputShape[2], num, _convOutputShape[2], _strideH, _dilationH, in _paddingH);
				allocateResult = HandleAllocate(i, j, inputHeight3, num6, num, num2, k, num13, num4);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num3 = num13;
				num5 = inputHeight3;
			}
			while (num4 < _outputShape[3])
			{
				int num14 = num4 + 1;
				int inputHeight4 = SpaceSearcher.GetInputHeight(SpaceSearcher.GetInputHeight(num14, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW), _inputShape[3], num2, _convOutputShape[3], _strideW, _dilationW, in _paddingW);
				allocateResult = HandleAllocate(i, j, num5, inputHeight4, num, num2, k, num3, num14);
				if (!allocateResult.IsOk)
				{
					break;
				}
				num4 = num14;
				num6 = inputHeight4;
			}
			for (; i < _outputShape[0] && (j != _inputShape[1] || num5 != _inputShape[2] || num6 != _inputShape[3] || i < _outputShape[0] / 2); i++)
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
					int num15 = k + 1;
					int num16 = j;
					if (k >= _ocPerGroup)
					{
						num15 = k + _ocPerGroup;
						num16 = j + _icPerGroup;
					}
					allocateResult = HandleAllocate(i, num16, num5, num6, num, num2, num15, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num15;
					j = num16;
				}
			}
			else
			{
				while (k < _outputShape[1])
				{
					int num17 = Math.Min(k + _groupPerPass * _ocPerGroup, _outputShape[1]);
					int num18 = Math.Min(j + _groupPerPass * _icPerGroup, _inputShape[1]);
					allocateResult = HandleAllocate(i, num18, num5, num6, num, num2, num17, num3, num4);
					if (!allocateResult.IsOk)
					{
						break;
					}
					k = num17;
					j = num18;
				}
			}
		}
		if (k % GNNEEnv.PuWidth != 0 && k > GNNEEnv.PuWidth && k <= _ocPerGroup)
		{
			k = k / GNNEEnv.PuWidth * GNNEEnv.PuWidth;
		}
		allocateResult = HandleAllocate(i, j, num5, num6, num, num2, k, num3, num4, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 921);
		GNNEShape gNNEShape = new GNNEShape(i, k, num3, num4);
		System.IO.File.AppendAllText("/tmp/k230_srv/tiledbg.log", "TILEDBG Conv2D in=[" + string.Join(",", _inputShape) + "] out=[" + string.Join(",", _outputShape) + "] w=[" + string.Join(",", _weightsShape) + "] glbTile=(i=" + i + ",k=" + k + ",h=" + num3 + ",w=" + num4 + ") j=" + j + " groupPerPass=" + _groupPerPass + " ocPerGroup=" + _ocPerGroup + " icPerGroup=" + _icPerGroup + "|");
				return new TileConv2DGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private AllocateResult HandleAllocate(int n, int c, int h, int w, int r, int s, int m, int e, int f, bool isFinal = false)
	{
		List<float> glbUsage = new List<float> { 0f, 0f };
		AllocateResult boxes = GetBoxes(n, c, h, w, r, s, m, e, f, glbUsage);
		BoxPacker bp = new BoxPacker(16)
		{
			Boxes = boxes.Boxes
		};
		AllocateResult allocation = SpaceSearcher.TryAllocate(bp);
		allocation.GlbMap = boxes.GlbMap;
		if (!allocation.IsOk)
		{
			return allocation;
		}
		foreach (KeyValuePair<ItemName, TensorOnGlb> item in boxes.GlbMap.Where<KeyValuePair<ItemName, TensorOnGlb>>((KeyValuePair<ItemName, TensorOnGlb> map) => allocation.Items.ContainsKey(map.Key)))
		{
			item.Value.Mmu = allocation.Items[item.Key];
		}
		return allocation;
	}

	private List<int> L1Search(TiledGlb glb, SegmentND weight, SegmentND psum)
	{
		SegmentND weight2 = weight;
		SegmentND psum2 = psum;
		TiledGlb glb2 = glb;
		int item = Math.Min(GNNEEnv.PuHeight, _groupPerPass * _icPerGroup);
		int item2 = Math.Min(GNNEEnv.PuWidth, _groupPerPass * _ocPerGroup);
		int h = 1;
		int w = 1;
		int e = 1;
		int f = ((!StripeSearch()) ? 1 : psum2[3].Length);
		int fStep = ((!StripeSearch()) ? 1 : psum2[3].Length);
		int r = ((_weightSplitPattern.Item1 == 0 || _dilationH > 3) ? 1 : _weightSplitPattern.Item1);
		int s = ((_weightSplitPattern.Item2 == 0 || _dilationW > 3) ? 1 : _weightSplitPattern.Item2);
		int hConvOut = SpaceSearcher.GetInputHeight(e, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
		int e2 = hConvOut;
		int inH = _inputShape[2] + _paddingH.Sum();
		int r2 = r;
		int outH = _convOutputShape[2];
		int strideH = _strideH;
		int dilationH = _dilationH;
		Padding p = new Padding(0, 0);
		h = SpaceSearcher.GetInputHeight(e2, inH, r2, outH, strideH, dilationH, in p);
		int wConvOut = SpaceSearcher.GetInputHeight(f, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
		int e3 = wConvOut;
		int inH2 = _inputShape[3] + _paddingW.Sum();
		int r3 = s;
		int outH2 = _convOutputShape[3];
		int strideW = _strideW;
		int dilationW = _dilationW;
		p = new Padding(0, 0);
		w = SpaceSearcher.GetInputHeight(e3, inH2, r3, outH2, strideW, dilationW, in p);
		int psumPingPangSplit = 1;
		{
			System.IO.File.AppendAllText("/tmp/k230_srv/tiledbg.log", "TILEDBG pp forced 1 (was " + (((object)_pool != null || (object)_dw != null || (object)_act1 != null) ? 2 : 1) + ")|");
		}
		int ifBytesPerElementGlb = TileUtilities.GetBytesPerElement(_inputType);
		if (!HandleL1Allocate(h, w, Math.Max(e, hConvOut), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb))
		{
			throw new NotSupportedException("exceeds L1 size");
		}
		while (f < psum2[3].Length && e < psum2[2].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1Size / GNNEEnv.PuHeight / 2)
		{
			if (f <= e)
			{
				if (!IncreaseFBy1())
				{
					break;
				}
			}
			else if (!IncreaseEBy1())
			{
				break;
			}
		}
		while (f < psum2[3].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1SizePerChan / 2 && IncreaseFBy1())
		{
		}
		while (e < psum2[2].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1SizePerChan / 2 && IncreaseEBy1())
		{
		}
		if (_weightSplitPattern.Equals(new Tuple<int, int>(0, 0)))
		{
			while (s < weight2[3].Length && r < weight2[2].Length && s < 8 && r < 31 && _dilationW <= 3 && _dilationH <= 3)
			{
				if (s <= r)
				{
					if (!IncreaseSBy1())
					{
						break;
					}
				}
				else if (!IncreaseRBy1())
				{
					break;
				}
			}
			while (s < weight2[3].Length && s < 8 && _dilationW <= 3 && IncreaseSBy1())
			{
			}
			while (r < weight2[2].Length && r < 31 && _dilationH <= 3 && IncreaseRBy1())
			{
			}
		}
		while (f < psum2[3].Length && e < psum2[2].Length)
		{
			if (f <= e)
			{
				if (!IncreaseFBy1())
				{
					break;
				}
			}
			else if (!IncreaseEBy1())
			{
				break;
			}
		}
		while (f < psum2[3].Length && IncreaseFBy1())
		{
		}
		while (e < psum2[2].Length && IncreaseEBy1())
		{
		}
		if (_dilationW > 3)
		{
			r = 1;
		}
		if (_dilationH > 3)
		{
			s = 1;
		}
		_weightSplitPattern = new Tuple<int, int>(r, s);
		System.IO.File.AppendAllText("/tmp/k230_srv/tiledbg.log", "TILEDBG L1Res h=" + h + " w=" + w + " e=" + e + " f=" + f + " item=" + item + " item2=" + item2 + " psumH=" + psum2[2].Length + " psumW=" + psum2[3].Length + " stripe=" + StripeSearch() + "|");
		return new List<int> { item, item2, e, f, r, s };
		bool IncreaseEBy1()
		{
			int num5 = e + 1;
			int inputHeight5 = SpaceSearcher.GetInputHeight(num5, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			int inH5 = _inputShape[2] + _paddingH.Sum();
			int r4 = r;
			int outH5 = _convOutputShape[2];
			int strideH3 = _strideH;
			int dilationH3 = _dilationH;
			Padding p4 = new Padding(0, 0);
			int inputHeight6 = SpaceSearcher.GetInputHeight(inputHeight5, inH5, r4, outH5, strideH3, dilationH3, in p4);
			bool flag = HandleL1Allocate(inputHeight6, w, Math.Max(num5, inputHeight5), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (!flag)
			{
				return flag;
			}
			e = num5;
			h = inputHeight6;
			hConvOut = inputHeight5;
			return flag;
		}
		bool IncreaseFBy1()
		{
			int num6 = f + fStep;
			int inputHeight7 = SpaceSearcher.GetInputHeight(num6, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
			int inH6 = _inputShape[3] + _paddingW.Sum();
			int r5 = s;
			int outH6 = _convOutputShape[3];
			int strideW3 = _strideW;
			int dilationW3 = _dilationW;
			Padding p5 = new Padding(0, 0);
			int inputHeight8 = SpaceSearcher.GetInputHeight(inputHeight7, inH6, r5, outH6, strideW3, dilationW3, in p5);
			bool flag2 = HandleL1Allocate(h, inputHeight8, Math.Max(e, hConvOut), Math.Max(num6, inputHeight7), psumPingPangSplit, ifBytesPerElementGlb);
			if (!flag2)
			{
				return flag2;
			}
			f = num6;
			w = inputHeight8;
			wConvOut = inputHeight7;
			return flag2;
		}
		bool IncreaseRBy1()
		{
			int num = r + 1;
			int inputHeight = SpaceSearcher.GetInputHeight(e, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			int inH3 = _inputShape[2] + _paddingH.Sum();
			int outH3 = _convOutputShape[2];
			int strideH2 = _strideH;
			int dilationH2 = _dilationH;
			Padding p2 = new Padding(0, 0);
			int inputHeight2 = SpaceSearcher.GetInputHeight(inputHeight, inH3, num, outH3, strideH2, dilationH2, in p2);
			bool num2 = HandleL1Allocate(inputHeight2, w, Math.Max(e, inputHeight), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (num2)
			{
				r = num;
				h = inputHeight2;
				hConvOut = inputHeight;
			}
			return num2;
		}
		bool IncreaseSBy1()
		{
			int num3 = s + 1;
			int inputHeight3 = SpaceSearcher.GetInputHeight(f, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
			int inH4 = _inputShape[3] + _paddingW.Sum();
			int outH4 = _convOutputShape[3];
			int strideW2 = _strideW;
			int dilationW2 = _dilationW;
			Padding p3 = new Padding(0, 0);
			int inputHeight4 = SpaceSearcher.GetInputHeight(inputHeight3, inH4, num3, outH4, strideW2, dilationW2, in p3);
			bool num4 = HandleL1Allocate(h, inputHeight4, Math.Max(e, hConvOut), Math.Max(f, inputHeight3), psumPingPangSplit, ifBytesPerElementGlb);
			if (num4)
			{
				s = num3;
				w = inputHeight4;
				wConvOut = inputHeight3;
			}
			return num4;
		}
		bool StripeSearch()
		{
			if (weight2[3].Length == 1 && weight2[2].Length == psum2[2].Length && _strideH == 1 && _strideW == 1 && ((((object)_pool != null || (object)_dw != null || (object)_act1 != null) && psum2[3].Length < 512) || psum2[3].Length < 1024))
			{
				return !(glb2.GlbMap[ItemName.Ifmap].DType == DataTypes.Int16);
			}
			return false;
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

	private void BuildL1Schedule(GnneActionUpdater actionUpdater, TiledGlb glb, SegmentND ifmap, SegmentND weight, SegmentND psum, int iPp, int ofPp, int wPp, SegmentND ifmap2, bool weightGroupOnly, WeightGroupHandler weightGroup, ref int l1Pp, Nncase.TIR.Buffer ddrW)
	{
		List<int> list = L1Search(glb, weight, psum);
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[2].Start, list[2], psum[2].End);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(psum[3].Start, list[3], psum[3].End);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(psum[0].Start, 1, psum[0].End);
		List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(weight[2].Start, list[4], weight[2].End);
		List<Segment1D> segmentStartEndLength5 = TileUtilities.GetSegmentStartEndLength(weight[3].Start, list[5], weight[3].End);
		List<List<Segment1D>> l1McSeg = GetL1McSeg(ifmap, psum, list[1], list[0]);
		List<Segment1D> list2 = l1McSeg[0];
		List<Segment1D> list3 = l1McSeg[1];
		int chunkSize = list[4];
		int chunkSize2 = Math.Min(GNNEEnv.PuKernelSpad / 2, list[5]);
		if (_dilationH > 1)
		{
			chunkSize = 1;
		}
		if (_dilationW > 1)
		{
			chunkSize2 = 1;
		}
		foreach (Segment1D item in list2)
		{
			foreach (Segment1D item2 in segmentStartEndLength3)
			{
				foreach (Segment1D item3 in segmentStartEndLength)
				{
					Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(item3.Start, item3.Length, _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
					Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(inputRowSegment.Start, inputRowSegment.Length, _inputShape[2], _weightsShape[2], _strideH, _dilationH, in _paddingH);
					foreach (Segment1D item4 in segmentStartEndLength2)
					{
						Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(item4.Start, item4.Length, _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
						Segment1D inputColumnSegment2 = TileUtilities.GetInputColumnSegment(inputColumnSegment.Start, inputColumnSegment.Length, _inputShape[3], _weightsShape[3], _strideW, _dilationW, in _paddingW);
						SegmentND l2GOf = new SegmentND(item2, item, item3, item4);
						SegmentND segmentND = new SegmentND(item2, item, inputRowSegment, inputColumnSegment);
						int num = item.Start / _ocPerGroup * _icPerGroup;
						int num2 = (item.End - 1) / _ocPerGroup * _icPerGroup + _icPerGroup;
						Segment1D segment1D = new Segment1D(0..0, new Padding(0, 0));
						bool flag = _inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1;
						bool flag2 = list3[0].Start == 0 || flag;
						foreach (Segment1D item5 in list3)
						{
							if (item5.Start < num || item5.End > num2)
							{
								continue;
							}
							int num3 = item5.Start % _icPerGroup;
							segment1D = new Segment1D(new System.Range(end: Math.Min(num3 + item5.Length, _icPerGroup), start: num3), new Padding(0, 0));
							foreach (Segment1D item6 in segmentStartEndLength4)
							{
								foreach (Segment1D item7 in segmentStartEndLength5)
								{
									Segment1D segment1D2 = item5;
									List<Segment1D> segmentStartEndLength6 = TileUtilities.GetSegmentStartEndLength(item6.Start, chunkSize, item6.End);
									List<Segment1D> segmentStartEndLength7 = TileUtilities.GetSegmentStartEndLength(item7.Start, chunkSize2, item7.End);
									Segment1D segment1D3 = item2;
									Segment1D segment1D4 = item;
									SegmentND x = new SegmentND(segment1D3, segment1D2, inputRowSegment2, inputColumnSegment2);
									SegmentND w = new SegmentND(item, segment1D, item6, item7);
									bool flag3 = false;
									SegmentND segmentND2 = TileUtilities.ShiftInputTensor(in x, in w, _weightsShape[2], _weightsShape[3], _strideH, _strideW, _dilationH, _dilationW);
									if (segmentND2[0].Length > 0 && segmentND2[1].Length > 0 && segmentND2[2].Length > 0 && segmentND2[3].Length > 0)
									{
										flag3 = true;
										if (!weightGroupOnly)
										{
											int num4 = _g2LIfRec[iPp][0].Item2.Stat_cnt();
											_g2LIfRec[iPp].RemoveAt(0);
											List<CcrClr> list4 = new List<CcrClr>();
											if (num4 > 0)
											{
												list4.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, iPp))));
											}
											actionUpdater.UpdateG2LIf(segmentND2, ifmap, _lif, iPp, null, list4, 0, DataTypes.UInt8, ItemName.Ifmap, restored: false, 0, 0, 0, _h2C, _weightsShape[2], _strideH);
										}
										else
										{
											_g2LIfRec[iPp].Add(new Tuple<SegmentND, TensorStat>(ifmap, new TensorStat(isFirstSlice: false, isLastSlice: false)));
										}
									}
									foreach (Segment1D item8 in segmentStartEndLength6)
									{
										foreach (Segment1D item9 in segmentStartEndLength7)
										{
											SegmentND w2 = new SegmentND(segment1D4, segment1D, item8, item9);
											SegmentND slice = TileUtilities.ShiftInputTensor(in x, in w2, _weightsShape[2], _weightsShape[3], _strideH, _strideW, _dilationH, _dilationW);
											int num5 = ((!(_weightType == DataTypes.Int16)) ? 1 : 2);
											int num6 = ((!(_inputType == DataTypes.Int16)) ? 1 : 2);
											for (int i = 0; i < num5; i++)
											{
												for (int num7 = 0; num7 < num6; num7++)
												{
													if (!weightGroupOnly)
													{
														_weightPreloadEn = false;
														if (!_weightPreloadEn)
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
															TileUtilities.Assert(w2 == tuple3.Item1 && tuple2.Item1 == tuple.Item1, "l2RW == g2RWSliceRecStat.Item1 && g2RWRecStat.Item1 == weightRecStat.Item1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 1334);
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
																actionUpdater.UpdateLoadW(w2, _lw, weightGroup, wPp, ddrW, ccrsToSet, list5, 0, _h2C, 0, ItemName.Weight, i);
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
															actionUpdater.UpdateG2RW(w2, weightGroup, _ocPerGroup, _lw, wPp, i, list6, list7, 0, 0, ItemName.Weight, ItemName.WQarg, _h2C);
														}
														else
														{
															actionUpdater.UpdateG2RW(w2, weightGroup, _ocPerGroup, _lw, wPp, i, null, null, 0, 0, ItemName.Weight, ItemName.WQarg, _h2C);
														}
													}
													else
													{
														weightGroup.UpdateWeightGroup(w2);
														if (!_weightPreloadEn)
														{
															_g2RWRec[wPp].Add(new Tuple<SegmentND, TensorStat>(weight, new TensorStat(isFirstSlice: false, isLastSlice: false)));
															_g2RWSliceRec[wPp].Add(new Tuple<SegmentND, TensorStat>(w2, new TensorStat(isFirstSlice: false, isLastSlice: false, -1)));
														}
													}
													if (!weightGroupOnly)
													{
														actionUpdater.UpdateL2RIf(slice, segmentND2, _strideH, _strideW, _icPerGroup, _lif, 0, num7, ((TensorConst)_conv[GNNEConv2D.DeqBias]).Value.ToScalar<int>(), DataTypes.UInt8, _h2C, _weightsShape[2]);
													}
													bool releaseIf = false;
													int num10;
													if (item9 == segmentStartEndLength7[segmentStartEndLength7.Count - 1])
													{
														if (item8 == segmentStartEndLength6[segmentStartEndLength6.Count - 1] && i == num5 - 1)
														{
															num10 = ((num7 == num6 - 1) ? 1 : 0);
															goto IL_09f4;
														}
													}
													num10 = 0;
													goto IL_09f4;
													IL_09f4:
													if (((uint)num10 & (flag3 ? 1u : 0u)) != 0)
													{
														releaseIf = true;
														flag3 = false;
													}
													bool loopStart = false;
													if (flag2 && i == 0 && num7 == 0)
													{
														loopStart = true;
														flag2 = false;
													}
													bool flag4 = segment1D.End == _weightsShape[1] && item9.End == weight[3].End && item8.End == weight[2].End && i == num5 - 1 && num7 == num6 - 1;
													DataType ofType = _outputType;
													ACT0_OUTPUT_DEST aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.dm;
													if ((object)_pool != null || (object)_dw != null || (object)_act1 != null)
													{
														aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.psum;
														ofType = _conv.CheckedDataType;
													}
													if (!weightGroupOnly)
													{
														int shift = ((TensorConst)_conv[GNNEConv2D.ShiftBits]).Value.ToScalar<int>();
														int num11 = 0;
														int num12 = 0;
														if (flag4 && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
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
														actionUpdater.UpdateR2LPsum(shift, segmentND, segmentND, psum, ofPp, l1Pp, aCT0_OUTPUT_DEST, releaseIf, Math.Max(i, num7), TcuComputeMode.NormalConv2d, loopStart, flag4, _strideH, _strideW, _ocPerGroup, _inputType, _weightType, ofType, _lact.CheckedDataType, list8, list9);
													}
													else if (flag4 && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
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
						if (!weightGroupOnly)
						{
							if (((object)_pool == null && (object)_dw == null && (object)_act1 == null) || segment1D.End != _weightsShape[1])
							{
								continue;
							}
							SegmentND l2RDw = new SegmentND();
							Segment1D segment1D5 = new Segment1D(0..0, new Padding(0, 0));
							SegmentND l2RIf = new SegmentND(segment1D5, segment1D5, segment1D5, segment1D5);
							List<CcrClr> list10 = new List<CcrClr>();
							if ((object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default)
							{
								l2RIf = segmentND;
								int num13 = _l2RIf2Rec[ofPp][0].Item2.Stat_cnt();
								_l2RIf2Rec[ofPp].RemoveAt(0);
								for (int j = 0; j < num13; j++)
								{
									list10.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2, ofPp))));
								}
							}
							if ((object)_dw != null)
							{
								int[] subArray = _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray()[2..];
								l2RDw = new SegmentND(new Segment1D(0..1, new Padding(0, 0)), item, new Segment1D(0..subArray[0], new Padding(0, 0)), new Segment1D(0..subArray[1], new Padding(0, 0)));
							}
							int num14 = 0;
							int num15 = 0;
							Tuple<SegmentND, TensorStat> tuple5 = _l2GOfRec[ofPp][0];
							_l2GOfRec[ofPp].RemoveAt(0);
							if (tuple5.Item2.IsFirstSlice && !_ofmapRec[ofPp][0].Item2.IsFirstSlice)
							{
								num15 = 1;
							}
							if (tuple5.Item2.IsLastSlice)
							{
								num14 = 1;
							}
							List<CcrSet> list11 = new List<CcrSet>();
							if (num14 > 0)
							{
								list11.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, ofPp)), 1));
							}
							List<CcrClr> list12 = new List<CcrClr>();
							if (num15 > 0)
							{
								list12.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, ofPp))));
							}
							actionUpdater.UpdateL2RPsum(_dw, _pool, _act1, l2RIf, ifmap2, l2RDw, segmentND, l2GOf, psum, ofPp, l1Pp, ACT0_OUTPUT_DEST.dm, TcuComputeMode.NormalConv2d, _ocPerGroup, weightGroup, list10, list11, list12);
							l1Pp = (l1Pp + 1) % 2;
						}
						else
						{
							if ((object)_act1 != null && segment1D.End == _weightsShape[1] && _act1[GNNEActivation.InputB] != None.Default)
							{
								_l2RIf2Rec[ofPp].Add(new Tuple<SegmentND, TensorStat>(ifmap2, new TensorStat(isFirstSlice: false, isLastSlice: false)));
							}
							if (((object)_pool != null || (object)_dw != null || (object)_act1 != null) && segment1D.End == _weightsShape[1])
							{
								_l2GOfRec[ofPp].Add(new Tuple<SegmentND, TensorStat>(psum, new TensorStat(isFirstSlice: false, isLastSlice: false)));
							}
						}
					}
				}
			}
		}
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
		if (_h2C)
		{
			h += _paddingH.Sum();
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
			obj[3] = _convOutputShape[1];
			TensorOnGlb value2 = new TensorOnGlb(obj, _lwQarg.CheckedDataType, 0);
			int wQargSize = SpaceSearcher.GetWQargSize(_convOutputShape[1], TileUtilities.GetBytesPerElement(_lwQarg.CheckedDataType));
			if (_inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1)
			{
				wQargSize = SpaceSearcher.GetWQargSize((int)Math.Ceiling(1.0 * (double)_convOutputShape[1] / (double)GNNEEnv.PuHeight) * GNNEEnv.PuWidth, TileUtilities.GetBytesPerElement(_lwQarg.CheckedDataType));
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
		if ((object)_dw != null)
		{
			int fusedKernelH = _fusedKernelH;
			int fusedKernelW = _fusedKernelW;
			int num6 = _outputShape[1];
			DataType checkedDataType = _dw[GNNEPdp0DW.Weights].CheckedDataType;
			TensorOnGlb value3 = new TensorOnGlb(new int[4] { num6, 1, fusedKernelH, fusedKernelW }, checkedDataType, 0);
			int weightSize = SpaceSearcher.GetWeightSize(fusedKernelH, fusedKernelW, TileUtilities.GetAlignedNum(num6, GNNEEnv.PuWidth), 1, TileUtilities.GetBytesPerElement(checkedDataType));
			list2[0] += weightSize;
			weightSize = TileUtilities.GetAlignedNum(weightSize, GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += weightSize;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.WBankWidth,
				weightSize / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.DwWeight));
			dictionary.Add(ItemName.DwWeight, value3);
			TensorOnGlb value4 = new TensorOnGlb(new int[4]
			{
				1,
				1,
				num6,
				GNNEEnv.ActNumPerChan
			}, _dw[GNNEPdp0DW.Act].CheckedDataType, 0);
			int actSize2 = SpaceSearcher.GetActSize(num6, GNNEEnv.ActNumPerChan, TileUtilities.GetBytesPerElement(_dw[GNNEPdp0DW.Act].CheckedDataType));
			list2[0] += actSize2;
			actSize2 = TileUtilities.GetAlignedNum(actSize2, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += actSize2;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.ActBankWidth,
				actSize2 / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.DwAct1));
			dictionary.Add(ItemName.DwAct1, value4);
			if (checkedDataType == DataTypes.UInt8 || checkedDataType == DataTypes.Int16)
			{
				int[] obj2 = new int[4] { 1, 1, 1, 0 };
				obj2[3] = num6;
				TensorOnGlb value5 = new TensorOnGlb(obj2, checkedDataType, 0);
				int wQargSize2 = SpaceSearcher.GetWQargSize(num6, TileUtilities.GetBytesPerElement(checkedDataType));
				list2[0] += wQargSize2;
				wQargSize2 = TileUtilities.GetAlignedNum(wQargSize2, GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
				list2[1] += wQargSize2;
				list.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.WQargBankWidth,
					wQargSize2 / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
				}, ItemName.DwQarg));
				dictionary.Add(ItemName.DwQarg, value5);
			}
		}
		TensorOnGlb tensorOnGlb4 = new TensorOnGlb(new int[]{1,1,1,0}, DataTypes.Float16, 0);
		if ((object)_act1 != null)
		{
			bool num7 = ((TensorConst)_act1[GNNEActivation.Is16Segments]).Value.ToScalar<bool>();
			int num8 = (num7 ? 1 : _outputShape[1]);
			int num9 = (num7 ? 49 : GNNEEnv.ActNumPerChan);
			TensorOnGlb value6 = new TensorOnGlb(new int[4] { 1, 1, num8, num9 }, _act1[GNNEActivation.Act].CheckedDataType, 0);
			int actSize3 = SpaceSearcher.GetActSize(num8, num9, TileUtilities.GetBytesPerElement(_act1[GNNEActivation.Act].CheckedDataType));
			list2[0] += actSize3;
			actSize3 = TileUtilities.GetAlignedNum(actSize3, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += actSize3;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.ActBankWidth,
				actSize3 / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.MfuAct1));
			dictionary.Add(ItemName.MfuAct1, value6);
			if (_act1[GNNEActivation.InputB] != None.Default)
			{
				Call call = (Call)_act1[GNNEActivation.InputA];
				Call call2 = (Call)_act1[GNNEActivation.InputB];
				DataType dataType = DataTypes.Float16;
				if ((object)call != null && call.Target is GNNELoad)
				{
					dataType = call.CheckedDataType;
				}
				else if ((object)call2 != null && call2.Target is GNNELoad)
				{
					dataType = call2.CheckedDataType;
				}
				int num10 = e;
				int num11 = f;
				int num12 = 32 / TileUtilities.GetBytesPerElement(dataType);
				if (e * f % num12 != 0)
				{
					int num13 = e + num12 - 1;
					int num14 = f + num12 - 1;
					long num15 = 4294836225L;
					for (int i = e; i < num13; i++)
					{
						for (int j = f; j < num14; j++)
						{
							int num16 = i * j;
							if (num16 % num12 == 0 && num16 < num15)
							{
								num15 = num16;
								num10 = i;
								num11 = j;
							}
						}
					}
				}
				tensorOnGlb4 = new TensorOnGlb(new int[4] { n, m, num10, num11 }, dataType, 0);
				int num17 = tensorOnGlb4.AllocatedBytes * GNNEEnv.NPingPongSplit;
				list2[0] += num17;
				num17 = TileUtilities.GetAlignedNum(num17, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
				list2[1] += num17;
				list.Add(new BoxOnGlb(new int[2]
				{
					GNNEEnv.IfmapBankWidth,
					num17 / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
				}, ItemName.Ifmap2));
			}
		}
		dictionary.Add(ItemName.Ifmap2, tensorOnGlb4);
		if ((object)_pool != null)
		{
			TensorOnGlb value7 = new TensorOnGlb(new int[4]
			{
				1,
				1,
				_outputShape[1],
				GNNEEnv.ActNumPerChan
			}, _pool[GNNEPdp0Reduce.Act].CheckedDataType, 0);
			int actSize4 = SpaceSearcher.GetActSize(_outputShape[1], GNNEEnv.ActNumPerChan, TileUtilities.GetBytesPerElement(_pool[GNNEPdp0Reduce.Act].CheckedDataType));
			list2[0] += actSize4;
			actSize4 = TileUtilities.GetAlignedNum(actSize4, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
			list2[1] += actSize4;
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.ActBankWidth,
				actSize4 / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.PdpAct1));
			dictionary.Add(ItemName.PdpAct1, value7);
		}
		glbUsage[0] = list2[0] / (float)GNNEEnv.GlbSize;
		glbUsage[1] = list2[1] / (float)GNNEEnv.GlbSize;
		return new AllocateResult
		{
			Boxes = list,
			GlbMap = dictionary
		};
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
			if (_l2RIf2Rec[i] != null)
			{
				_l2RIf2Rec[i].Clear();
			}
			else
			{
				_l2RIf2Rec[i] = new List<Tuple<SegmentND, TensorStat>>();
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

	private void InitParameters(Call convNode, Call ld, Call st)
	{
		_weightSplitPattern = new Tuple<int, int>(0, 0);
		_conv = convNode;
		_lif = ld;
		_lw = (Call)_conv[GNNEConv2D.Weights];
		_lact = (Call)_conv[GNNEConv2D.Act];
		_lwQarg = (Call)_conv[GNNEConv2D.WeightsBias];
		_sof = st;
		_inputShape = new GNNEShape(_lif.CheckedShape[0].FixedValue, _lif.CheckedShape[1].FixedValue, _lif.CheckedShape[2].FixedValue, _lif.CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(_conv.CheckedShape[0].FixedValue, _conv.CheckedShape[1].FixedValue, _conv.CheckedShape[2].FixedValue, _conv.CheckedShape[3].FixedValue);
		_convOutputShape = _outputShape;
		_weightsShape = new GNNEShape(_lw.CheckedShape[0].FixedValue, _lw.CheckedShape[1].FixedValue, _lw.CheckedShape[2].FixedValue, _lw.CheckedShape[3].FixedValue);
		int[] array = ((TensorConst)_conv[GNNEConv2D.Padding]).Value.ToArray<int>();
		_paddingH = new Padding(array[0], array[1]);
		_paddingW = new Padding(array[2], array[3]);
		_strideH = ((TensorConst)_conv[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
		_strideW = ((TensorConst)_conv[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
		_dilationH = ((TensorConst)_conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
		_dilationW = ((TensorConst)_conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
		_groups = ((TensorConst)_conv[GNNEConv2D.Groups]).Value.ToScalar<int>();
		_icPerGroup = _inputShape[1] / _groups;
		_ocPerGroup = _outputShape[1] / _groups;
		_groupPerPass = Math.Min(Math.Max(Math.Min(GNNEEnv.PuHeight / _icPerGroup, GNNEEnv.PuWidth / _ocPerGroup), 1), _groups);
		_pool = ((st[GNNEStore.Input] is Call call && call.Target is GNNEPdp0Reduce) ? ((Call)st[GNNEStore.Input]) : null);
		_dw = ((st[GNNEStore.Input] is Call call2 && call2.Target is GNNEPdp0DW) ? ((Call)st[GNNEStore.Input]) : null);
		_act1 = ((st[GNNEStore.Input] is Call call3 && call3.Target is GNNEActivation) ? ((Call)st[GNNEStore.Input]) : null);
		if ((object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default)
		{
			if (_act1[GNNEActivation.InputA] is Call call4 && call4.Target is GNNELoad)
			{
				_lif2 = _act1[GNNEActivation.InputA] as Call;
			}
			else
			{
				_lif2 = _act1[GNNEActivation.InputB] as Call;
			}
		}
		else
		{
			_lif2 = null;
		}
		_strategy = DetermineSearchStrategy();
		_inputType = _lif.CheckedDataType;
		_weightType = _lw.CheckedDataType;
		_weightGroups.Add(new WeightGroupHandler(_weightType, _weightType));
		_weightGroups.Add(new WeightGroupHandler(_weightType, _weightType));
		_outputType = _conv.CheckedDataType;
		if ((object)_pool != null)
		{
			_outputType = _pool.CheckedDataType;
		}
		if ((object)_dw != null)
		{
			_outputType = _dw.CheckedDataType;
		}
		if ((object)_act1 != null)
		{
			_outputType = _act1.CheckedDataType;
		}
		_fusedKernelH = 1;
		_fusedKernelW = 1;
		_fusedPaddingH = new Padding(0, 0);
		_fusedPaddingW = new Padding(0, 0);
		_fusedStrideH = 1;
		_fusedStrideW = 1;
		_fusedDilationH = 1;
		_fusedDilationW = 1;
		if ((object)_pool != null)
		{
			_outputShape = new GNNEShape(_pool.CheckedShape[0].FixedValue, _pool.CheckedShape[1].FixedValue, _pool.CheckedShape[2].FixedValue, _pool.CheckedShape[3].FixedValue);
			_fusedKernelH = ((TensorConst)_pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[0];
			_fusedKernelW = ((TensorConst)_pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[1];
			array = ((TensorConst)_pool[GNNEPdp0Reduce.Padding]).Value.ToArray<int>();
			_fusedPaddingH = new Padding(array[0], array[1]);
			_fusedPaddingW = new Padding(array[2], array[3]);
			_fusedStrideH = ((TensorConst)_pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[0];
			_fusedStrideW = ((TensorConst)_pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[1];
		}
		else if ((object)_dw != null)
		{
			_outputShape = new GNNEShape(_dw.CheckedShape[0].FixedValue, _dw.CheckedShape[1].FixedValue, _dw.CheckedShape[2].FixedValue, _dw.CheckedShape[3].FixedValue);
			_fusedKernelH = _dw[GNNEPdp0DW.Weights].CheckedShape[2].FixedValue;
			_fusedKernelW = _dw[GNNEPdp0DW.Weights].CheckedShape[3].FixedValue;
			array = ((TensorConst)_dw[GNNEPdp0DW.Padding]).Value.ToArray<int>();
			_fusedPaddingH = new Padding(array[0], array[1]);
			_fusedPaddingW = new Padding(array[2], array[3]);
			_fusedStrideH = ((TensorConst)_dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[0];
			_fusedStrideW = ((TensorConst)_dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[1];
			_fusedDilationH = ((TensorConst)_dw[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[0];
			_fusedDilationW = ((TensorConst)_dw[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[1];
		}
		else if ((object)_act1 != null)
		{
			_outputShape = new GNNEShape(_act1.CheckedShape[0].FixedValue, _act1.CheckedShape[1].FixedValue, _act1.CheckedShape[2].FixedValue, _act1.CheckedShape[3].FixedValue);
			TileUtilities.Assert(_outputShape.Dims.SequenceEqual(_convOutputShape.Dims), "_outputShape.Dims.SequenceEqual(_convOutputShape.Dims)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 1942);
		}
		_convOutputShape[2] = TileUtilities.GetInputRowSegment(0, _outputShape[2], _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH).Length;
		_convOutputShape[3] = TileUtilities.GetInputColumnSegment(0, _outputShape[3], _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW).Length;
		bool flag = _inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1;
		_h2C = _weightsShape[1] * _weightsShape[2] <= GNNEEnv.PuHeight && _weightsShape[2] != 1 && _inputShape[2] > 200 && _inputShape[3] > 200 && !flag && _inputType != DataTypes.Int16;
	}

	private GlbSearchStrategy DetermineSearchStrategy()
	{
		if (_inputShape.Size > _weightsShape.Size)
		{
			return GlbSearchStrategy.IfFirst;
		}
		return GlbSearchStrategy.WFirst;
	}

	private ScheduleStrategy DetermineScheduleStrategy(List<GnneAction> actionsIfFirst, List<GnneAction> actionsWFirst)
	{
		DdrBandwidth[] array = new DdrBandwidth[2]
		{
			new DdrBandwidth(),
			new DdrBandwidth()
		};
		foreach (GnneAction item in actionsIfFirst)
		{
			array[0].CalcBw(item);
		}
		foreach (GnneAction item2 in actionsWFirst)
		{
			array[1].CalcBw(item2);
		}
		long num = array[0].TotalBw[0];
		long num2 = array[1].TotalBw[0];
		if (num >= num2)
		{
			return ScheduleStrategy.WFirstSchedule;
		}
		return ScheduleStrategy.IfFirstSchedule;
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
				if (l < list3.Count - 1 && !(list3[l].Item1 == list3[l + 1].Item1))
				{
					list3[l].Item2.IsLastSlice = true;
					list3[l + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		g2LIfRec = _l2RIf2Rec;
		foreach (List<Tuple<SegmentND, TensorStat>> list4 in g2LIfRec)
		{
			for (int m = 0; m < list4.Count; m++)
			{
				if (m == 0)
				{
					list4[m].Item2.IsFirstSlice = true;
				}
				if (m == list4.Count - 1)
				{
					list4[m].Item2.IsLastSlice = true;
				}
				if (m < list4.Count - 1 && list4[m].Item1 != list4[m + 1].Item1)
				{
					list4[m].Item2.IsLastSlice = true;
					list4[m + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		g2LIfRec = _weightRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list5 in g2LIfRec)
		{
			if (list5.Count > 0)
			{
				list5[0].Item2.IsFirstSlice = true;
				list5[list5.Count - 1].Item2.IsLastSlice = true;
			}
		}
		g2LIfRec = _ofmapRec;
		foreach (List<Tuple<SegmentND, TensorStat>> list6 in g2LIfRec)
		{
			if (list6.Count > 0)
			{
				list6[0].Item2.IsFirstSlice = true;
				list6[list6.Count - 1].Item2.IsLastSlice = true;
			}
		}
		for (int n = 0; n < _g2RWSliceRec.Length; n++)
		{
			List<SegmentND> list7 = new List<SegmentND>();
			List<SegmentND> wSlices = new List<SegmentND>();
			List<Tuple<SegmentND, TensorStat>> list8 = new List<Tuple<SegmentND, TensorStat>>();
			for (int num = 0; num < _g2RWRec[n].Count; num++)
			{
				if (_g2RWRec[n][num].Item2.IsFirstSlice)
				{
					list7.Clear();
					wSlices.Clear();
					list8.Clear();
				}
				if (!list7.Contains(_g2RWSliceRec[n][num].Item1))
				{
					list7.Add(_g2RWSliceRec[n][num].Item1);
				}
				wSlices.Add(_g2RWSliceRec[n][num].Item1);
				list8.Add(_g2RWSliceRec[n][num]);
				if (!_g2RWRec[n][num].Item2.IsLastSlice)
				{
					continue;
				}
				foreach (SegmentND slice in list7)
				{
					list8[wSlices.FindIndex((SegmentND x) => x == slice)].Item2.IsFirstSlice = true;
					list8[wSlices.FindLastIndex((SegmentND g2RwSliceIdx) => g2RwSliceIdx == slice)].Item2.IsLastSlice = true;
				}
				int cnt;
				for (cnt = 0; cnt < wSlices.Count; cnt++)
				{
					list8[cnt].Item2.SliceIdx = list7.FindIndex((SegmentND x) => x == wSlices[cnt]);
					_g2RWSliceRec[n][num - wSlices.Count + 1 + cnt] = list8[cnt];
				}
			}
			if (!(_weightType == DataTypes.Int16))
			{
				continue;
			}
			for (int num2 = 1; num2 < _g2RWSliceRec[n].Count; num2++)
			{
				if (_g2RWSliceRec[n][_g2RWSliceRec[n].Count - num2 - 1].Item2.IsFirstSlice)
				{
					_g2RWSliceRec[n][_g2RWSliceRec[n].Count - num2].Item2.IsFirstSlice = true;
				}
				if (_g2RWSliceRec[n][num2].Item2.IsLastSlice)
				{
					_g2RWSliceRec[n][num2 - 1].Item2.IsLastSlice = true;
				}
			}
			for (int num3 = 0; num3 < _g2RWSliceRec[n].Count; num3++)
			{
				_g2RWSliceRec[n][num3].Item2.SliceIdx = _g2RWSliceRec[n][num3].Item2.SliceIdx * 2 + (num3 & 1);
			}
		}
	}

	private void ArrangeWeights(DataType weightsType, GNNEShape weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		List<SegmentND> list = weightGroup.WeightGroupSlice();
		byte[] array = new byte[oldWeights.Length];
		if (_h2C)
		{
			int num = 0;
			int num2 = 0;
			foreach (SegmentND item in list)
			{
				TileUtilities.Assert(num == weightGroup.WeightGroupOffset(item) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 2178);
				for (int i = 0; i < bytesPerElement; i++)
				{
					for (int j = 0; j < item[0].Length; j++)
					{
						for (int k = 0; k < item[3].Length; k++)
						{
							for (int l = 0; l < item[2].Length; l++)
							{
								for (int m = 0; m < item[1].Length; m++)
								{
									int num3 = ((((j + item[0].Start) * weightsShape[1] + m + item[1].Start) * weightsShape[2] + l + item[2].Start) * weightsShape[3] + k + item[3].Start) * bytesPerElement;
									array[num2++] = oldWeights[num3 + i];
									num++;
								}
							}
						}
					}
				}
			}
		}
		else
		{
			int num4 = 0;
			int num5 = 0;
			foreach (SegmentND item2 in list)
			{
				TileUtilities.Assert(num4 == weightGroup.WeightGroupOffset(item2) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileConv2d.cs", 2205);
				for (int n = 0; n < bytesPerElement; n++)
				{
					for (int num6 = 0; num6 < item2[0].Length; num6++)
					{
						for (int num7 = 0; num7 < item2[2].Length; num7++)
						{
							for (int num8 = 0; num8 < item2[3].Length; num8++)
							{
								for (int num9 = 0; num9 < item2[1].Length; num9++)
								{
									int num10 = ((((num6 + item2[0].Start) * weightsShape[1] + num9 + item2[1].Start) * weightsShape[2] + num7 + item2[2].Start) * weightsShape[3] + num8 + item2[3].Start) * bytesPerElement;
									array[num5++] = oldWeights[num10 + n];
									num4++;
								}
							}
						}
					}
				}
			}
		}
		array.CopyTo(oldWeights);
	}

	private void ArrangeDwWeights(DataType weightsType, int[] weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		int num = 0;
		byte[] array = new byte[oldWeights.Length];
		ref int reference = ref weightsShape[0];
		ref int reference2 = ref weightsShape[1];
		int num2 = weightsShape[1];
		int num3 = weightsShape[0];
		reference = num2;
		reference2 = num3;
		foreach (SegmentND item in from wg in weightGroup.DwGroupSlice()
			let cs = wg[1].Start
			let ce = wg[1].End
			let dwShape = _dw?[GNNEPdp0DW.Weights].CheckedShape.ToValueArray()
			select new SegmentND(..1, cs..ce, ..dwShape[2], ..dwShape[3]))
		{
			for (int i = 0; i < item[0].Length; i++)
			{
				for (int j = 0; j < item[2].Length; j++)
				{
					for (int k = 0; k < item[3].Length; k++)
					{
						for (int l = 0; l < item[1].Length; l++)
						{
							int num4 = ((((i + item[0].Start) * weightsShape[1] + l + item[1].Start) * weightsShape[2] + j + item[2].Start) * weightsShape[3] + k + item[3].Start) * bytesPerElement;
							for (int m = 0; m < bytesPerElement; m++)
							{
								array[num * GNNEEnv.PuWidth + l] = oldWeights[num4 + m];
							}
						}
						num++;
					}
				}
			}
		}
		array.CopyTo(oldWeights);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		Fusion fusion = (Fusion)__result["fusion"];
		return GetReplace(call, ld, st, fusion);
	}
}
