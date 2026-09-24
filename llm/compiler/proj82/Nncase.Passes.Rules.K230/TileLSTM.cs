using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nncase.IR;
using Nncase.IR.Buffers;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileLSTM : RewriteRule<Pattern>
{
	private static int _count = -1;

	private readonly List<WeightGroupHandler> _weightGroups = new List<WeightGroupHandler>(2);

	private CcrHandler _ccrHandler = new CcrHandler();

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private GNNEShape? _inputShape;

	private GNNEShape? _wXcShape;

	private GNNEShape? _wXcShapeReshape;

	private GNNEShape? _actXcShape;

	private GNNEShape? _wRcShape;

	private GNNEShape? _wRcShapeReshape;

	private GNNEShape? _actRcShape;

	private GNNEShape? _initHShape;

	private GNNEShape? _initCShape;

	private GNNEShape? _outputShape;

	private GNNEShape? _outputHShape;

	private GNNEShape? _outputCShape;

	private Call? _lIf;

	private Call? _lWXc;

	private Call? _lActXc;

	private Call? _lWRc;

	private Call? _lActRc0;

	private Call? _lActRc1;

	private Call? _lInitH;

	private Call? _lInitC;

	private Call? _lSegFt;

	private Call? _lSegGt;

	private Call? _of;

	private Call? _ofH;

	private Call? _ofC;

	private Call? _lwXcQarg;

	private Call? _lwRcQarg;

	private Call? _lwBinAct;

	private Call? _lwBinQAct;

	private DataType? _inputType;

	private DataType? _wXcType;

	private DataType? _actXcType;

	private DataType? _wRcType;

	private DataType? _actRcType;

	private DataType? _initHType;

	private DataType? _initCType;

	private DataType? _outputType;

	private DataType? _outputHType;

	private DataType? _outputCType;

	private Call? _oldLstm;

	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsFusion("k230", new GNNELSTMFusion().Pattern);


	private PrimFunction GetReplace(Expr output, Call midCall, IReadOnlyList<Expr> midCallParams)
	{
		_count++;
		Call call = (Call)midCallParams[0];
		Call call2 = (Call)((Nncase.IR.Tuple)output)[0];
		int num = ((TensorConst)midCall[GNNELSTM.OutputSize]).Value.ToScalar<int>();
		Call call3 = ((num >= 2) ? ((Call)((Nncase.IR.Tuple)output)[1]) : null);
		Call call4 = ((num >= 3) ? ((Call)((Nncase.IR.Tuple)output)[2]) : null);
		Nncase.TIR.Buffer buffer = null;
		Nncase.TIR.Buffer buffer2 = null;
		List<Nncase.TIR.Buffer> list = new List<Nncase.TIR.Buffer>();
		InitParameters(midCall, call, call2, call3, call4);
		TileLSTMGlb glb = SearchGlbParameters();
		int[] array = _oldLstm[GNNELSTM.Input].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(call[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer3, "var ddrIf");
		list.Add(buffer3);
		T.AttachBuffer((TensorConst)_lWXc[GNNELoadW.Input], out Nncase.TIR.Buffer buffer4, "var ddrLWXC");
		T.AttachBuffer((TensorConst)_lWRc[GNNELoadW.Input], out Nncase.TIR.Buffer buffer5, "var ddrLWRC");
		T.AttachBuffer((TensorConst)_lwXcQarg[GNNELoadW.Input], out Nncase.TIR.Buffer buffer6, "var ddrLWXCQARG");
		T.AttachBuffer((TensorConst)_lwRcQarg[GNNELoadW.Input], out Nncase.TIR.Buffer buffer7, "var ddrLWRCQARG");
		T.AttachBuffer((TensorConst)_lActXc[GNNELoadW.Input], out Nncase.TIR.Buffer buffer8, "var ddrLACTXC");
		T.AttachBuffer((TensorConst)_lActRc0[GNNELoadW.Input], out Nncase.TIR.Buffer buffer9, "var ddrLACTRC0");
		T.AttachBuffer((TensorConst)_lActRc1[GNNELoadW.Input], out Nncase.TIR.Buffer buffer10, "var ddrLACTRC1");
		T.AttachBuffer((TensorConst)_lSegFt[GNNELoadW.Input], out Nncase.TIR.Buffer buffer11, "var ddrLSEGFT");
		T.AttachBuffer((TensorConst)_lSegGt[GNNELoadW.Input], out Nncase.TIR.Buffer buffer12, "var ddrLSEGGT");
		T.AttachBuffer((TensorConst)_lwBinAct[GNNELoadW.Input], out Nncase.TIR.Buffer buffer13, "var ddrLWBINACT");
		T.AttachBuffer((TensorConst)_lwBinQAct[GNNELoadW.Input], out Nncase.TIR.Buffer buffer14, "var ddrLWBINQACT");
		Nncase.TIR.Buffer buffer15;
		if (_lInitH[GNNELoad.Input] is TensorConst)
		{
			T.AttachBuffer((TensorConst)_lInitH[GNNELoad.Input], out buffer15, "ddrLInitH");
		}
		else
		{
			T.CreateBuffer(new TensorType(_lInitH[GNNELoad.Input].CheckedDataType, _lInitH[GNNELoad.Input].CheckedShape), MemoryLocation.Input, out buffer15, "ddrLInitH");
			list.Add(buffer15);
		}
		Nncase.TIR.Buffer buffer16;
		if (_lInitC[GNNELoad.Input] is TensorConst)
		{
			T.AttachBuffer((TensorConst)_lInitC[GNNELoad.Input], out buffer16, "ddrLInitC");
		}
		else
		{
			T.CreateBuffer(new TensorType(_lInitC[GNNELoad.Input].CheckedDataType, _lInitC[GNNELoad.Input].CheckedShape), MemoryLocation.Input, out buffer16, "ddrLInitC");
			list.Add(buffer16);
		}
		T.CreateBuffer(new TensorType(call2.CheckedDataType, call2.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer17, "var ddrOf");
		list.Add(buffer17);
		if ((object)call3 != null)
		{
			T.CreateBuffer(new TensorType(call3.CheckedDataType, call3.CheckedShape), MemoryLocation.Output, out buffer, "ddrOfH");
			list.Add(buffer);
		}
		if ((object)call4 != null)
		{
			T.CreateBuffer(new TensorType(call4.CheckedDataType, call4.CheckedShape), MemoryLocation.Output, out buffer2, "ddrOfC");
			list.Add(buffer2);
		}
		List<GnneAction> actions = BuildSchedule(glb, midCall, call, call2, call3, call4, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, buffer10, buffer11, buffer12, buffer13, buffer14, buffer15, buffer16, buffer17, buffer, buffer2);
		Span<byte> bytesBuffer = ((TensorConst)((Call)buffer4.MemSpan.Start)[DDrOf.Input]).Value.BytesBuffer;
		ArrangeWeights(_wXcType, _wXcShape, bytesBuffer, _weightGroups[0], _wXcShapeReshape);
		Span<byte> bytesBuffer2 = ((TensorConst)((Call)buffer5.MemSpan.Start)[DDrOf.Input]).Value.BytesBuffer;
		ArrangeWeights(_wRcType, _wRcShape, bytesBuffer2, _weightGroups[1], _wRcShapeReshape);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileLSTM_{_count}", K230RtModule.Kind, list.ToArray()).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private void ArrangeWeights(DataType weightsType, GNNEShape weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup, GNNEShape weightsReshape)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		List<SegmentND> list = weightGroup.WeightGroupSlice();
		byte[] array = new byte[oldWeights.Length];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < weightsShape[1]; i++)
		{
			int num3 = 0;
			foreach (SegmentND item in list)
			{
				TileUtilities.Assert(num3 == weightGroup.WeightGroupOffset(item) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLSTM.cs", 165);
				for (int j = 0; j < bytesPerElement; j++)
				{
					for (int k = 0; k < item[0].Length; k++)
					{
						for (int l = 0; l < item[2].Length; l++)
						{
							for (int m = 0; m < item[3].Length; m++)
							{
								for (int n = 0; n < item[1].Length; n++)
								{
									int num4 = (num + (((k + item[0].Start) * weightsReshape[1] + n + item[1].Start) * weightsReshape[2] + l + item[2].Start) * weightsReshape[3] + m + item[3].Start) * bytesPerElement;
									array[num2++] = oldWeights[num4 + j];
									num3++;
								}
							}
						}
					}
				}
			}
			num += num3 / bytesPerElement;
		}
		array.CopyTo(oldWeights);
	}

	private TileLSTMGlb SearchGlbParameters()
	{
		int nPingPongSplit = 1;
		AllocateResult allocateResult = HandleAllocate(nPingPongSplit, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLSTM.cs", 201);
		GNNEShape gNNEShape = new GNNEShape(1, 1, 1, _outputShape[3]);
		return new TileLSTMGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, nPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, Call call, Call ld, Call st, Call stH, Call stC, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrLWXC, Nncase.TIR.Buffer ddrLWRC, Nncase.TIR.Buffer ddrLWXCQARG, Nncase.TIR.Buffer ddrLWRCQARG, Nncase.TIR.Buffer ddrLACTXC, Nncase.TIR.Buffer ddrLACTRC0, Nncase.TIR.Buffer ddrLACTRC1, Nncase.TIR.Buffer ddrLSEGFT, Nncase.TIR.Buffer ddrLSEGGT, Nncase.TIR.Buffer ddrLWBINACT, Nncase.TIR.Buffer ddrLWBINQACT, Nncase.TIR.Buffer ddrLInitH, Nncase.TIR.Buffer ddrLInitC, Nncase.TIR.Buffer ddrOf, Nncase.TIR.Buffer ddrOfH, Nncase.TIR.Buffer ddrOfC)
	{
		List<GnneAction> list = new List<GnneAction>();
		_gpr = new GprHandler(GNNEEnv.GprNum);
		_ssr = new SsrHandler(GNNEEnv.SsrNum);
		_ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, _ccrHandler, _gpr, _ssr);
		gnneActionUpdater.UpdateMmuConf();
		int nPingPongSplit = glb.NPingPongSplit;
		int[] array = new int[1];
		int[] array2 = new int[1];
		SegmentND segmentND = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(.._inputShape[3], new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
		SegmentND segmentND2 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..(_outputShape[3] * 4), new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
		SegmentND weight = new SegmentND(segmentND2[1], segmentND[1], new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
		BuildL1Schedule(isInitH: false, gnneActionUpdater, glb, segmentND, weight, segmentND2, 0, 8, 0, weightGroupOnly: true, _weightGroups[0], array, ddrIf, ddrLWXC);
		SegmentND ori = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
		TileUtilities.GlbTensorIndexShift(in ori, in ori);
		weight = new SegmentND(segmentND2[1], ori[1], new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
		BuildL1Schedule(isInitH: true, gnneActionUpdater, glb, ori, weight, segmentND2, 0, 9, 0, weightGroupOnly: true, _weightGroups[1], array2, ddrIf, ddrLWRC);
		if (_wXcType == DataTypes.UInt8 || _wXcType == DataTypes.Int16)
		{
			for (int i = 0; i < _outputShape[1]; i++)
			{
				gnneActionUpdater.UpdateLoadWQarg(_lwXcQarg, _weightGroups[0], ddrLWXCQARG, _outputShape[3] * 4 * i, null, null, ItemName.LstmWXcQarg, _outputShape[3] * 4 * i);
			}
		}
		if (_wRcType == DataTypes.UInt8 || _wRcType == DataTypes.Int16)
		{
			for (int j = 0; j < _outputShape[1]; j++)
			{
				gnneActionUpdater.UpdateLoadWQarg(_lwRcQarg, _weightGroups[1], ddrLWRCQARG, _outputShape[3] * 4 * j, null, null, ItemName.LstmWRcQarg, _outputShape[3] * 4 * j);
			}
		}
		gnneActionUpdater.UpdateLoadAct(_lActXc, ddrLACTXC, ItemName.LstmActXc);
		gnneActionUpdater.UpdateLoadAct(_lActRc0, ddrLACTRC0, ItemName.LstmActRc);
		gnneActionUpdater.UpdateLoadAct(_lActRc1, ddrLACTRC1, ItemName.LstmActRc, glb.GlbMap[ItemName.LstmActRc].AllocatedBytes);
		gnneActionUpdater.UpdateLoadAct(_lSegFt, ddrLSEGFT, ItemName.LstmSegFittingParamFt);
		gnneActionUpdater.UpdateLoadAct(_lSegGt, ddrLSEGGT, ItemName.LstmSegFittingParamGt);
		gnneActionUpdater.UpdateLoadAct(_lwBinAct, ddrLWBINACT, ItemName.LstmBinAct);
		gnneActionUpdater.UpdateLoadAct(_lwBinQAct, ddrLWBINQACT, ItemName.LstmBinQAct);
		int num = 0;
		List<int> list2 = new List<int>();
		for (int k = 0; k < _inputShape[1]; k++)
		{
			list2.Add(k);
		}
		if (((GNNELSTM)_oldLstm.Target).Direction == LSTMDirection.Reverse)
		{
			list2.Reverse();
		}
		for (int l = 0; l < _outputShape[1]; l++)
		{
			if (l == 1)
			{
				list2.Reverse();
			}
			for (int m = 0; m < _inputShape[2]; m++)
			{
				for (int n = 0; n != list2.Count; n++)
				{
					int num2 = list2[n];
					int l2 = ((n != list2.Count - 1) ? list2[n + 1] : 0);
					if (num2 == list2[0])
					{
						SegmentND segmentND3 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(num2..(num2 + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)), new Segment1D(.._inputShape[3], new Padding(0, 0)));
						List<CcrSet> ccrsToSet = new List<CcrSet>
						{
							new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, num)), array[0])
						};
						List<int> stridesD = new List<int>
						{
							segmentND3[1].Length,
							segmentND3[2].Length,
							segmentND3[3].Length
						};
						List<CcrClr> ccrsToClr = new List<CcrClr>();
						if (num2 != list2[0])
						{
							_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.IfmapFake, num));
						}
						gnneActionUpdater.UpdateLoadIf(segmentND3, _lIf, num, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet, ccrsToClr);
					}
					SegmentND segmentND4 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)));
					SegmentND segmentND5 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)));
					List<CcrSet> list3 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmOfH)), 0)
					};
					List<CcrSet> list4 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmOfC)), 0)
					};
					if (num2 == list2[0])
					{
						list3[0].Value = array2[0];
						list4[0].Value = 1;
						List<int> stridesD2 = new List<int>
						{
							segmentND4[1].Length,
							segmentND4[2].Length,
							segmentND4[3].Length
						};
						gnneActionUpdater.UpdateLoadIf(segmentND4, _lInitH, 0, ddrLInitH, 0, stridesD2, ItemName.LstmOfH, list3);
						stridesD2 = new List<int>
						{
							segmentND5[1].Length,
							segmentND5[2].Length,
							segmentND5[3].Length
						};
						gnneActionUpdater.UpdateLoadIf(segmentND5, _lInitC, 0, ddrLInitC, 0, stridesD2, ItemName.LstmOfC, list4);
					}
					SegmentND segmentND6 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(.._inputShape[3], new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
					SegmentND segmentND7 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..(_outputShape[3] * 4), new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
					SegmentND weight2 = new SegmentND(segmentND7[1], segmentND6[1], new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
					BuildL1Schedule(isInitH: false, gnneActionUpdater, glb, segmentND6, weight2, segmentND7, num, 8, 0, weightGroupOnly: false, _weightGroups[0], array, ddrIf, ddrLWXC, num2 == list2[0], l, num2 == list2[list2.Count - 1], l2, m);
					SegmentND ori2 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
					SegmentND ori3 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)));
					SegmentND segmentND8 = TileUtilities.GlbTensorIndexShift(in ori2, in ori2);
					SegmentND segmentND9 = TileUtilities.GlbTensorIndexShift(in ori3, in ori3);
					weight2 = new SegmentND(segmentND7[1], ori2[1], new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
					BuildL1Schedule(isInitH: true, gnneActionUpdater, glb, ori2, weight2, segmentND7, 0, 9, 0, weightGroupOnly: false, _weightGroups[1], array2, ddrIf, ddrLWRC, num2 == list2[0], l);
					List<CcrSet> ccrsToSet2 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 10)), 4)
					};
					List<CcrClr> list5 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 8)))
					};
					list5.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 9))));
					int offsetS = 0;
					int allocatedBytes = glb.GlbMap[ItemName.Ofmap].AllocatedBytes;
					int offsetD = 0;
					int offsetAct = 0;
					SegmentND segmentND10 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND7[1].Start..segmentND7[1].End, new Padding(0, 0)));
					List<int> src1Stride = new List<int>
					{
						segmentND10[1].Length,
						segmentND10[2].Length,
						segmentND10[3].Length
					};
					List<int> src2Stride = new List<int>
					{
						segmentND10[1].Length,
						segmentND10[2].Length,
						segmentND10[3].Length
					};
					List<int> dstStride = new List<int>
					{
						segmentND10[1].Length,
						segmentND10[2].Length,
						segmentND10[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND10, segmentND10, segmentND10, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: false, 0, ccrsToSet2, list5, offsetS, allocatedBytes, offsetD, offsetAct, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmBinAct, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride, src2Stride, dstStride, isByChannel: false);
					List<CcrSet> ccrsToSet3 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 6)), 1)
					};
					List<CcrClr> ccrsToClr2 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 10)))
					};
					SegmentND segmentND11 = new SegmentND(segmentND7);
					segmentND11[1] = new Segment1D((_outputShape[3] * 2)..(_outputShape[3] * 3), new Padding(0, 0));
					Segment1D segment1D = new Segment1D(..0, new Padding(0, 0));
					SegmentND segmentND12 = new SegmentND(segment1D, segment1D, segment1D, segment1D);
					SegmentND segmentND13 = new SegmentND(ori2);
					SegmentND segmentND14 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND11[1].Start..segmentND11[1].End, new Padding(0, 0)));
					SegmentND segmentND15 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND13[1].Start..segmentND13[1].End, new Padding(0, 0)));
					int addr = new TensorOnGlb(new int[4]
					{
						segmentND7[0].Length,
						segmentND7[1].Length,
						segmentND7[2].Length,
						segmentND7[3].Length
					}, DataTypes.Float16, 0).GetAddr(segmentND11[0].Start, segmentND11[1].Start, segmentND11[2].Start, segmentND11[3].Start, 0, 1);
					int offsetS2 = 0;
					int offsetD2 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 2;
					int offsetAct2 = 0;
					List<int> src1Stride2 = new List<int>
					{
						segmentND14[1].Length,
						segmentND14[2].Length,
						segmentND14[3].Length
					};
					List<int> src2Stride2 = new List<int>
					{
						segmentND12[1].Length,
						segmentND12[2].Length,
						segmentND12[3].Length
					};
					List<int> dstStride2 = new List<int>
					{
						segmentND15[1].Length,
						segmentND15[2].Length,
						segmentND15[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND14, segmentND12, segmentND15, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: true, 0, ccrsToSet3, ccrsToClr2, addr, offsetS2, offsetD2, offsetAct2, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmSegFittingParamFt, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride2, src2Stride2, dstStride2);
					List<CcrClr> list6 = new List<CcrClr>
					{
						new CcrClr(list4[0].Ccr)
					};
					list6.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 6))));
					List<CcrSet> ccrsToSet4 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 2)), 1)
					};
					int offsetS3 = 0;
					int offsetS4 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 2;
					int offsetD3 = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 2;
					int offsetAct3 = 0;
					SegmentND segmentND16 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND9[1].Start..segmentND9[1].End, new Padding(0, 0)));
					List<int> src1Stride3 = new List<int>
					{
						segmentND16[1].Length,
						segmentND16[2].Length,
						segmentND16[3].Length
					};
					List<int> src2Stride3 = new List<int>
					{
						segmentND16[1].Length,
						segmentND16[2].Length,
						segmentND16[3].Length
					};
					List<int> dstStride3 = new List<int>
					{
						segmentND16[1].Length,
						segmentND16[2].Length,
						segmentND16[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND16, segmentND16, segmentND16, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, _initCType, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: false, 0, ccrsToSet4, list6, offsetS3, offsetS4, offsetD3, offsetAct3, ItemName.Ofmap, ItemName.LstmOfC, ItemName.LstmBinAct, MFU_ACT1_FUNCTION.mul, ItemName.Ofmap, src1Stride3, src2Stride3, dstStride3, isByChannel: false);
					List<CcrSet> ccrsToSet5 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 4)), 1)
					};
					List<CcrClr> ccrsToClr3 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 10)))
					};
					SegmentND segmentND17 = new SegmentND(segmentND7);
					_ = _outputShape[3];
					segmentND17[1] = new Segment1D(0.._outputShape[3], new Padding(0, 0));
					Segment1D segment1D2 = new Segment1D(..0, new Padding(0, 0));
					SegmentND segmentND18 = new SegmentND(segment1D2, segment1D2, segment1D2, segment1D2);
					SegmentND segmentND19 = new SegmentND(ori2);
					SegmentND segmentND20 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND17[1].Start..segmentND17[1].End, new Padding(0, 0)));
					SegmentND segmentND21 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND19[1].Start..segmentND19[1].End, new Padding(0, 0)));
					int addr2 = new TensorOnGlb(new int[4]
					{
						segmentND7[0].Length,
						segmentND7[1].Length,
						segmentND7[2].Length,
						segmentND7[3].Length
					}, DataTypes.Float16, 0).GetAddr(segmentND17[0].Start, segmentND17[1].Start, segmentND17[2].Start, segmentND17[3].Start, 0, 1);
					int offsetS5 = 0;
					int allocatedBytes2 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes;
					_ = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4;
					int offsetD4 = allocatedBytes2 + 0;
					int offsetAct4 = 0;
					List<int> src1Stride4 = new List<int>
					{
						segmentND20[1].Length,
						segmentND20[2].Length,
						segmentND20[3].Length
					};
					List<int> src2Stride4 = new List<int>
					{
						segmentND18[1].Length,
						segmentND18[2].Length,
						segmentND18[3].Length
					};
					List<int> dstStride4 = new List<int>
					{
						segmentND21[1].Length,
						segmentND21[2].Length,
						segmentND21[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND20, segmentND18, segmentND21, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: true, 0, ccrsToSet5, ccrsToClr3, addr2, offsetS5, offsetD4, offsetAct4, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmSegFittingParamFt, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride4, src2Stride4, dstStride4);
					List<CcrSet> ccrsToSet6 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 7)), 1)
					};
					List<CcrClr> ccrsToClr4 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 10)))
					};
					SegmentND segmentND22 = new SegmentND(segmentND7);
					segmentND22[1] = new Segment1D((_outputShape[3] * 3)..(_outputShape[3] * 4), new Padding(0, 0));
					Segment1D segment1D3 = new Segment1D(..0, new Padding(0, 0));
					SegmentND segmentND23 = new SegmentND(segment1D3, segment1D3, segment1D3, segment1D3);
					SegmentND segmentND24 = new SegmentND(ori2);
					SegmentND segmentND25 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND22[1].Start..segmentND22[1].End, new Padding(0, 0)));
					SegmentND segmentND26 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND24[1].Start..segmentND24[1].End, new Padding(0, 0)));
					int addr3 = new TensorOnGlb(new int[4]
					{
						segmentND7[0].Length,
						segmentND7[1].Length,
						segmentND7[2].Length,
						segmentND7[3].Length
					}, DataTypes.Float16, 0).GetAddr(segmentND22[0].Start, segmentND22[1].Start, segmentND22[2].Start, segmentND22[3].Start, 0, 1);
					int offsetS6 = 0;
					int offsetD5 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 3;
					int offsetAct5 = 0;
					List<int> src1Stride5 = new List<int>
					{
						segmentND25[1].Length,
						segmentND25[2].Length,
						segmentND25[3].Length
					};
					List<int> src2Stride5 = new List<int>
					{
						segmentND23[1].Length,
						segmentND23[2].Length,
						segmentND23[3].Length
					};
					List<int> dstStride5 = new List<int>
					{
						segmentND26[1].Length,
						segmentND26[2].Length,
						segmentND26[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND25, segmentND23, segmentND26, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: true, 0, ccrsToSet6, ccrsToClr4, addr3, offsetS6, offsetD5, offsetAct5, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmSegFittingParamGt, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride5, src2Stride5, dstStride5);
					List<CcrClr> list7 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 4)))
					};
					list7.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 7))));
					List<CcrSet> ccrsToSet7 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 0)), 1)
					};
					int allocatedBytes3 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes;
					_ = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4;
					int offsetS7 = allocatedBytes3 + 0;
					int offsetS8 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 3;
					_ = glb.GlbMap[ItemName.Ofmap].AllocatedBytes;
					int offsetD6 = 0;
					int offsetAct6 = 0;
					SegmentND segmentND27 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND9[1].Start..segmentND9[1].End, new Padding(0, 0)));
					List<int> src1Stride6 = new List<int>
					{
						segmentND27[1].Length,
						segmentND27[2].Length,
						segmentND27[3].Length
					};
					List<int> src2Stride6 = new List<int>
					{
						segmentND27[1].Length,
						segmentND27[2].Length,
						segmentND27[3].Length
					};
					List<int> dstStride6 = new List<int>
					{
						segmentND27[1].Length,
						segmentND27[2].Length,
						segmentND27[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND27, segmentND27, segmentND27, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: false, 0, ccrsToSet7, list7, offsetS7, offsetS8, offsetD6, offsetAct6, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmBinAct, MFU_ACT1_FUNCTION.mul, ItemName.Ofmap, src1Stride6, src2Stride6, dstStride6, isByChannel: false);
					List<CcrClr> list8 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 2)))
					};
					list8.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 0))));
					List<CcrSet> list9 = new List<CcrSet>();
					list9.Add(new CcrSet(list4[0].Ccr, (num2 == list2[list2.Count - 1] && (object)_ofC == null) ? 1 : 2));
					List<CcrSet> ccrsToSet8 = list9;
					int offsetS9 = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 2;
					_ = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4;
					int offsetS10 = 0;
					int offsetD7 = 0;
					int offsetAct7 = 0;
					SegmentND segmentND28 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND9[1].Start..segmentND9[1].End, new Padding(0, 0)));
					List<int> src1Stride7 = new List<int>
					{
						segmentND28[1].Length,
						segmentND28[2].Length,
						segmentND28[3].Length
					};
					List<int> src2Stride7 = new List<int>
					{
						segmentND28[1].Length,
						segmentND28[2].Length,
						segmentND28[3].Length
					};
					List<int> dstStride7 = new List<int>
					{
						segmentND28[1].Length,
						segmentND28[2].Length,
						segmentND28[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND28, segmentND28, segmentND28, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: false, 0, ccrsToSet8, list8, offsetS9, offsetS10, offsetD7, offsetAct7, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmBinAct, MFU_ACT1_FUNCTION.add, ItemName.LstmOfC, src1Stride7, src2Stride7, dstStride7, isByChannel: false);
					List<CcrSet> ccrsToSet9 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 5)), 1)
					};
					List<CcrClr> ccrsToClr5 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 10)))
					};
					SegmentND segmentND29 = new SegmentND(segmentND7);
					segmentND29[1] = new Segment1D(_outputShape[3]..(_outputShape[3] * 2), new Padding(0, 0));
					Segment1D segment1D4 = new Segment1D(..0, new Padding(0, 0));
					SegmentND segmentND30 = new SegmentND(segment1D4, segment1D4, segment1D4, segment1D4);
					SegmentND segmentND31 = new SegmentND(ori2);
					SegmentND segmentND32 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND29[1].Start..segmentND29[1].End, new Padding(0, 0)));
					SegmentND segmentND33 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND31[1].Start..segmentND31[1].End, new Padding(0, 0)));
					int addr4 = new TensorOnGlb(new int[4]
					{
						segmentND7[0].Length,
						segmentND7[1].Length,
						segmentND7[2].Length,
						segmentND7[3].Length
					}, DataTypes.Float16, 0).GetAddr(segmentND29[0].Start, segmentND29[1].Start, segmentND29[2].Start, segmentND29[3].Start, 0, 1);
					int offsetS11 = 0;
					int offsetD8 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4;
					int offsetAct8 = 0;
					List<int> src1Stride8 = new List<int>
					{
						segmentND32[1].Length,
						segmentND32[2].Length,
						segmentND32[3].Length
					};
					List<int> src2Stride8 = new List<int>
					{
						segmentND30[1].Length,
						segmentND30[2].Length,
						segmentND30[3].Length
					};
					List<int> dstStride8 = new List<int>
					{
						segmentND33[1].Length,
						segmentND33[2].Length,
						segmentND33[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND32, segmentND30, segmentND33, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: true, 0, ccrsToSet9, ccrsToClr5, addr4, offsetS11, offsetD8, offsetAct8, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmSegFittingParamFt, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride8, src2Stride8, dstStride8);
					List<CcrSet> ccrsToSet10 = new List<CcrSet>
					{
						new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 3)), 1)
					};
					List<CcrClr> ccrsToClr6 = new List<CcrClr>
					{
						new CcrClr(list4[0].Ccr)
					};
					SegmentND segmentND34 = new SegmentND(segmentND8);
					SegmentND segmentND35 = new SegmentND(segmentND8);
					Segment1D segment1D5 = new Segment1D(..0, new Padding(0, 0));
					SegmentND segmentND36 = new SegmentND(segment1D5, segment1D5, segment1D5, segment1D5);
					SegmentND segmentND37 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND34[1].Start..segmentND34[1].End, new Padding(0, 0)));
					SegmentND segmentND38 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND35[1].Start..segmentND35[1].End, new Padding(0, 0)));
					int offsetS12 = 0;
					int offsetS13 = 0;
					int offsetD9 = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 3;
					int offsetAct9 = 0;
					List<int> src1Stride9 = new List<int>
					{
						segmentND37[1].Length,
						segmentND37[2].Length,
						segmentND37[3].Length
					};
					List<int> src2Stride9 = new List<int>
					{
						segmentND36[1].Length,
						segmentND36[2].Length,
						segmentND36[3].Length
					};
					List<int> dstStride9 = new List<int>
					{
						segmentND38[1].Length,
						segmentND38[2].Length,
						segmentND38[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND37, segmentND36, segmentND38, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, DataTypes.Float16, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: true, 0, ccrsToSet10, ccrsToClr6, offsetS12, offsetS13, offsetD9, offsetAct9, ItemName.Ofmap, ItemName.LstmOfC, ItemName.LstmSegFittingParamGt, MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride9, src2Stride9, dstStride9);
					int value = array2[0] + 1;
					if (num2 == list2[list2.Count - 1])
					{
						value = (((object)_ofH == null) ? 1 : 2);
					}
					List<CcrClr> list10 = new List<CcrClr>
					{
						new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 5)))
					};
					list10.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, 3))));
					List<CcrSet> ccrsToSet11 = new List<CcrSet>
					{
						new CcrSet(list3[0].Ccr, value)
					};
					int offsetS14 = glb.GlbMap[ItemName.Ofmap].AllocatedBytes + glb.GlbMap[ItemName.Ofmap].GlbNByte / 4;
					int offsetS15 = glb.GlbMap[ItemName.Ofmap].GlbNByte / 4 * 3;
					int offsetD10 = 0;
					int offsetAct10 = 0;
					SegmentND segmentND39 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)), new Segment1D(segmentND8[1].Start..segmentND8[1].End, new Padding(0, 0)));
					List<int> src1Stride10 = new List<int>
					{
						segmentND39[1].Length,
						segmentND39[2].Length,
						segmentND39[3].Length
					};
					List<int> src2Stride10 = new List<int>
					{
						segmentND39[1].Length,
						segmentND39[2].Length,
						segmentND39[3].Length
					};
					List<int> dstStride10 = new List<int>
					{
						segmentND39[1].Length,
						segmentND39[2].Length,
						segmentND39[3].Length
					};
					gnneActionUpdater.UpdateMfuAct1(segmentND39, segmentND39, segmentND39, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, DataTypes.Float16, DataTypes.Float16, _outputType, new DeQuantizeParam(0, 1f), new DeQuantizeParam(0, 1f), 0, 0, 0, is16Segments: false, 0, ccrsToSet11, list10, offsetS14, offsetS15, offsetD10, offsetAct10, ItemName.Ofmap, ItemName.Ofmap, ItemName.LstmBinQAct, MFU_ACT1_FUNCTION.mul, ItemName.LstmOfH, src1Stride10, src2Stride10, dstStride10, isByChannel: false);
					SegmentND ofmap = new SegmentND(new Segment1D(num2..(num2 + 1), new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)));
					List<CcrClr> ccrsToClr7 = new List<CcrClr>
					{
						new CcrClr(list3[0].Ccr)
					};
					gnneActionUpdater.UpdateStoreT(ofmap, _of, 0, ddrOf, 0, null, null, ccrsToClr7, ItemName.LstmOfH, new List<int>
					{
						1,
						1,
						_outputShape[3]
					});
					if (num2 == list2[list2.Count - 1])
					{
						SegmentND ofmap2 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(m..(m + 1), new Padding(0, 0)), new Segment1D(.._outputShape[3], new Padding(0, 0)));
						if ((object)_ofH != null)
						{
							List<CcrClr> ccrsToClr8 = new List<CcrClr>
							{
								new CcrClr(list3[0].Ccr)
							};
							gnneActionUpdater.UpdateStoreT(ofmap2, _ofH, 0, ddrOfH, 0, null, null, ccrsToClr8, ItemName.LstmOfH, new List<int>
							{
								1,
								1,
								_outputShape[3]
							});
						}
						if ((object)_ofC != null)
						{
							List<CcrClr> ccrsToClr9 = new List<CcrClr>
							{
								new CcrClr(list4[0].Ccr)
							};
							gnneActionUpdater.UpdateStoreT(ofmap2, _ofC, 0, ddrOfC, 0, null, null, ccrsToClr9, ItemName.LstmOfC, new List<int>
							{
								1,
								1,
								_outputShape[3]
							});
						}
					}
					num = (num + 1) % nPingPongSplit;
				}
			}
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLSTM.cs", 665);
		return list;
	}

	private void BuildL1Schedule(bool isInitH, GnneActionUpdater actionUpdater, TiledGlb glb, SegmentND ifmap, SegmentND weight, SegmentND psum, int iPp, int ofPp, int wPp, bool weightGroupOnly, WeightGroupHandler weightGroup, int[] ifLives, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrW, bool isFirstL = false, int d = 0, bool isLastL = false, int l = 0, int b = 0)
	{
		int l1Pp = 0;
		bool restored = ReshapeIfmapWeightsOfmap(isInitH, ref ifmap, ref psum, ref weight);
		List<int> list = L1Search(weight, psum, (!isInitH) ? _inputType : (isFirstL ? _initHType : _outputType));
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
		foreach (Segment1D item in list2)
		{
			foreach (Segment1D item2 in segmentStartEndLength3)
			{
				foreach (Segment1D item3 in segmentStartEndLength)
				{
					Padding p = new Padding(0, 0);
					Segment1D segment1D = item3;
					Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(segment1D.Start, segment1D.Length, ifmap[2].Length, weight[2].Length, 1, 1, in p);
					foreach (Segment1D item4 in segmentStartEndLength2)
					{
						Padding p2 = new Padding(0, 0);
						Segment1D segment1D2 = item4;
						Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(segment1D2.Start, segment1D2.Length, ifmap[3].Length, weight[3].Length, 1, 1, in p2);
						new SegmentND(item2, item, item3, item4);
						SegmentND segmentND = new SegmentND(item2, item, segment1D, segment1D2);
						bool flag = true;
						foreach (Segment1D item5 in list3)
						{
							Segment1D segment1D3 = item5;
							foreach (Segment1D item6 in segmentStartEndLength4)
							{
								foreach (Segment1D item7 in segmentStartEndLength5)
								{
									Segment1D segment1D4 = item5;
									List<Segment1D> segmentStartEndLength6 = TileUtilities.GetSegmentStartEndLength(item6.Start, chunkSize, item6.End);
									List<Segment1D> segmentStartEndLength7 = TileUtilities.GetSegmentStartEndLength(item7.Start, chunkSize2, item7.End);
									Segment1D segment1D5 = item2;
									Segment1D segment1D6 = item;
									SegmentND segments = new SegmentND(segment1D5, segment1D4, inputRowSegment, inputRowSegment2);
									new SegmentND(item, segment1D3, item6, item7);
									bool flag2 = false;
									SegmentND segmentND2 = new SegmentND(segments);
									if (segmentND2[0].Length > 0 && segmentND2[1].Length > 0 && segmentND2[2].Length > 0 && segmentND2[3].Length > 0)
									{
										flag2 = true;
										if (!weightGroupOnly)
										{
											int num = 0;
											List<CcrSet> list4 = new List<CcrSet>();
											if ((item == list2[list2.Count - 1] && TileUtilities.IsLastSlice(segmentND2, ifmap)) || (item == list2[0] && TileUtilities.IsFirstSlice(segmentND2, ifmap)))
											{
												num = 1;
												if (item == list2[list2.Count - 1] && TileUtilities.IsLastSlice(segmentND2, ifmap) && !isLastL && !isInitH)
												{
													list4.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.IfmapFake, iPp)), 1));
												}
											}
											int ccrItem = _ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, iPp));
											if (isInitH)
											{
												ccrItem = _ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmOfH));
											}
											List<CcrClr> list5 = new List<CcrClr>();
											if (num > 0)
											{
												list5.Add(new CcrClr(ccrItem));
											}
											int length = ifmap[1].Length;
											int length2 = ifmap[2].Length;
											int length3 = ifmap[3].Length;
											actionUpdater.UpdateG2LIf(segmentND2, ifmap, (isInitH && isFirstL) ? _lInitH : _lIf, iPp, list4, list5, 0, (!isInitH) ? _inputType : (isFirstL ? _initHType : _outputType), isInitH ? ItemName.LstmOfH : ItemName.Ifmap, restored, length, length2, length3);
											if (item == list2[list2.Count - 1] && TileUtilities.IsLastSlice(segmentND2, ifmap) && !isLastL && !isInitH)
											{
												SegmentND segmentND3 = new SegmentND(new Segment1D(..1, new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)), new Segment1D(b..(b + 1), new Padding(0, 0)), new Segment1D(.._inputShape[3], new Padding(0, 0)));
												List<CcrSet> ccrsToSet = new List<CcrSet>
												{
													new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap, iPp)), ifLives[0])
												};
												List<int> stridesD = new List<int>
												{
													segmentND3[1].Length,
													segmentND3[2].Length,
													segmentND3[3].Length
												};
												List<CcrClr> ccrsToClr = new List<CcrClr>
												{
													new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.IfmapFake, iPp)))
												};
												actionUpdater.UpdateLoadIf(segmentND3, _lIf, iPp, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet, ccrsToClr);
											}
										}
										else
										{
											ifLives[0]++;
										}
									}
									foreach (Segment1D item8 in segmentStartEndLength6)
									{
										foreach (Segment1D item9 in segmentStartEndLength7)
										{
											SegmentND segmentND4 = new SegmentND(segment1D6, segment1D3, item8, item9);
											SegmentND slice = new SegmentND(segments);
											int num2 = ((!isInitH) ? ((!(_wXcType == DataTypes.Int16)) ? 1 : 2) : ((!(_wRcType == DataTypes.Int16)) ? 1 : 2));
											int num3 = ((!isInitH) ? ((!(_inputType == DataTypes.Int16)) ? 1 : 2) : ((!isFirstL) ? ((!(_outputType == DataTypes.Int16)) ? 1 : 2) : ((!(_initHType == DataTypes.Int16)) ? 1 : 2)));
											for (int i = 0; i < num2; i++)
											{
												for (int num4 = 0; num4 < num3; num4++)
												{
													if (!weightGroupOnly)
													{
														List<CcrSet> list6 = new List<CcrSet>();
														List<CcrClr> list7 = new List<CcrClr>();
														if (b == 0 && isFirstL && num4 == 0)
														{
															if (!isInitH)
															{
																list6.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmWXc)), 1));
																list7.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmWXc))));
																actionUpdater.UpdateLoadW(segmentND4, _lWXc, weightGroup, 0, ddrW, list6, null, glb.GlbMap[ItemName.LstmWXc].AllocatedBytes * d, h2c: false, weightGroup.CurrentOffset() * d * TileUtilities.GetBytesPerElement(_wXcType), ItemName.LstmWXc, i);
															}
															else
															{
																list6.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmWRc)), 1));
																list7.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.LstmWRc))));
																actionUpdater.UpdateLoadW(segmentND4, _lWRc, weightGroup, 0, ddrW, list6, null, glb.GlbMap[ItemName.LstmWRc].AllocatedBytes * d, h2c: false, weightGroup.CurrentOffset() * d * TileUtilities.GetBytesPerElement(_wRcType), ItemName.LstmWRc, i);
															}
														}
														int offsetWS = (isInitH ? (glb.GlbMap[ItemName.LstmWRc].AllocatedBytes * d) : (glb.GlbMap[ItemName.LstmWXc].AllocatedBytes * d));
														actionUpdater.UpdateG2RW(segmentND4, weightGroup, weight[0].Length, isInitH ? _lWRc : _lWXc, wPp, i, null, list7, offsetWS, _outputShape[3] * 4 * d, isInitH ? ItemName.LstmWRc : ItemName.LstmWXc, isInitH ? ItemName.LstmWRcQarg : ItemName.LstmWXcQarg);
													}
													else
													{
														weightGroup.UpdateWeightGroup(segmentND4);
													}
													if (!weightGroupOnly)
													{
														int bias = ((TensorConst)_oldLstm[GNNELSTM.IfDeqBias]).Value.ToScalar<int>();
														if (isInitH)
														{
															bias = (isFirstL ? ((TensorConst)_oldLstm[GNNELSTM.HDeqBias0]).Value.ToScalar<int>() : ((TensorConst)_oldLstm[GNNELSTM.HDeqBias1]).Value.ToScalar<int>());
														}
														actionUpdater.UpdateL2RIf(slice, segmentND2, 1, 1, weight[1].Length, (isInitH && isFirstL) ? _lInitH : _lIf, 0, num4, bias);
													}
													bool releaseIf = false;
													int num5;
													if (item9 == segmentStartEndLength7[segmentStartEndLength7.Count - 1])
													{
														if (item8 == segmentStartEndLength6[segmentStartEndLength6.Count - 1] && i == num2 - 1)
														{
															num5 = ((num4 == num3 - 1) ? 1 : 0);
															goto IL_0a4f;
														}
													}
													num5 = 0;
													goto IL_0a4f;
													IL_0a4f:
													if (((uint)num5 & (flag2 ? 1u : 0u)) != 0)
													{
														releaseIf = true;
														flag2 = false;
													}
													bool loopStart = false;
													if (flag && i == 0 && num4 == 0)
													{
														loopStart = true;
														flag = false;
													}
													bool flag3 = segment1D3.End == weight[1].Length && item9.End == weight[3].End && item8.End == weight[2].End && i == num2 - 1 && num4 == num3 - 1;
													DataType @float = DataTypes.Float16;
													ACT0_OUTPUT_DEST aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.dm;
													if (!weightGroupOnly)
													{
														int shift = ((TensorConst)_oldLstm[GNNELSTM.XcShiftBits]).Value.ToScalar<int>();
														if (isInitH)
														{
															shift = ((!isFirstL) ? ((TensorConst)_oldLstm[GNNELSTM.RcShiftBits1]).Value.ToScalar<int>() : ((TensorConst)_oldLstm[GNNELSTM.RcShiftBits0]).Value.ToScalar<int>());
														}
														int num6 = 0;
														if (flag3 && item.End == weight[0].End && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
														{
															num6 = 1;
														}
														List<CcrSet> list8 = new List<CcrSet>();
														if (num6 > 0)
														{
															list8.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, ofPp)), 1));
														}
														List<CcrClr> ccrsToClr2 = new List<CcrClr>();
														int num7 = 0;
														if (!isInitH)
														{
															num7 = glb.GlbMap[ItemName.LstmActXc].GlbNByte / 2 * d;
														}
														if (isInitH)
														{
															num7 = glb.GlbMap[ItemName.LstmActRc].GlbNByte / 2 * d;
															if (!isFirstL)
															{
																num7 += glb.GlbMap[ItemName.LstmActRc].AllocatedBytes;
															}
														}
														actionUpdater.UpdateR2LPsum(shift, segmentND, segmentND, psum, 0, l1Pp, aCT0_OUTPUT_DEST, releaseIf, Math.Max(i, num4), TcuComputeMode.NormalConv2d, loopStart, flag3, 1, 1, weight[0].Length, _inputType, isInitH ? _wRcType : _wXcType, @float, isInitH ? _lActRc0.CheckedDataType : _lActXc.CheckedDataType, list8, ccrsToClr2, num7, glb.GlbMap[ItemName.Ofmap].AllocatedBytes * (isInitH ? 1 : 0), null, isInitH ? ItemName.LstmActRc : ItemName.LstmActXc);
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
		}
		ifLives[0] = Math.Min(ifLives[0], 2);
	}

	private List<List<Segment1D>> GetL1McSeg(SegmentND ifmap, SegmentND psum, int mInloop, int cInloop)
	{
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[1].Start, mInloop, psum[1].End);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start, cInloop, ifmap[1].End);
		return new List<List<Segment1D>> { segmentStartEndLength, segmentStartEndLength2 };
	}

	private List<int> L1Search(SegmentND weight, SegmentND psum, DataType type)
	{
		int item = Math.Min(GNNEEnv.PuHeight, weight[1].Length);
		int item2 = Math.Min(GNNEEnv.PuWidth, weight[0].Length);
		int length = weight[2].Length;
		int length2 = weight[3].Length;
		int num = 1;
		int num2 = 1;
		int length3 = weight[2].Length;
		int length4 = weight[3].Length;
		int val = 1;
		int val2 = 1;
		int psumPingPangSplit = 1;
		int bytesPerElement = TileUtilities.GetBytesPerElement(type);
		if (!HandleL1Allocate(length, length2, Math.Max(num, val), Math.Max(num2, val2), psumPingPangSplit, bytesPerElement))
		{
			throw new NotSupportedException("exceeds L1 size");
		}
		return new List<int> { item, item2, num, num2, length3, length4 };
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

	private bool ReshapeIfmapWeightsOfmap(bool isInitH, ref SegmentND ifmap, ref SegmentND ofmap, ref SegmentND weights)
	{
		if (ifmap[2].Length == 1 && ifmap[3].Length == 1 && weights[2].Length == 1 && weights[3].Length == 1 && (weights[1].Length % 24 == 0 || weights[1].Length % 20 == 0 || weights[1].Length % 16 == 0))
		{
			int num = ((weights[1].Length % 24 == 0) ? 24 : ((weights[1].Length % 20 == 0) ? 20 : 16));
			int num2 = weights[1].Length / num;
			uint num3 = 4u;
			if (num2 <= num3)
			{
				num3 = (uint)num2;
			}
			else
			{
				while (num3 != 0 && num2 % num3 != 0L)
				{
					num3--;
				}
			}
			long num4 = num2 / num3;
			uint num5 = 31u;
			if (num4 <= num5)
			{
				num5 = (uint)num4;
			}
			else
			{
				while (num5 != 0 && num4 % num5 != 0L)
				{
					num5--;
				}
			}
			num = (int)((int)num4 / num5 * num);
			ifmap = new SegmentND(ifmap[0], new Segment1D(..num, new Padding(0, 0)), new Segment1D(..(int)num5, new Padding(0, 0)), new Segment1D(..(int)num3, new Padding(0, 0)));
			weights = new SegmentND(weights[0], new Segment1D(..num, new Padding(0, 0)), new Segment1D(..(int)num5, new Padding(0, 0)), new Segment1D(..(int)num3, new Padding(0, 0)));
			ofmap = new SegmentND(ofmap[0], ofmap[1], ofmap[2], ofmap[3]);
			if (!isInitH)
			{
				_wXcShapeReshape = new GNNEShape(weights[0].Length, num, (int)num5, (int)num3);
			}
			else
			{
				_wRcShapeReshape = new GNNEShape(weights[0].Length, num, (int)num5, (int)num3);
			}
			return true;
		}
		if (!isInitH)
		{
			_wXcShapeReshape = new GNNEShape(weights[0].Length, weights[1].Length, weights[2].Length, weights[3].Length);
		}
		else
		{
			_wRcShapeReshape = new GNNEShape(weights[0].Length, weights[1].Length, weights[2].Length, weights[3].Length);
		}
		return false;
	}

	private GNNEShape ComputeRS(GNNEShape inShape, GNNEShape wShape)
	{
		if (inShape[2] == 1 && inShape[3] == 1 && wShape[2] == 1 && wShape[3] == 1 && (wShape[1] % 24 == 0 || wShape[1] % 20 == 0 || wShape[1] % 16 == 0))
		{
			int num = ((wShape[1] % 24 == 0) ? 24 : ((wShape[1] % 20 == 0) ? 20 : 16));
			int num2 = wShape[1] / num;
			int num3 = 4;
			if (num2 <= num3)
			{
				num3 = num2;
			}
			else
			{
				while (num3 > 0 && num2 % num3 != 0)
				{
					num3--;
				}
			}
			int num4 = num2 / num3;
			int num5 = 31;
			if (num4 <= num5)
			{
				num5 = num4;
			}
			else
			{
				while (num5 > 0 && num4 % num5 != 0)
				{
					num5--;
				}
			}
			num = num4 / num5 * num;
			return new GNNEShape(wShape[0], num, num5, num3);
		}
		return wShape;
	}

	private AllocateResult HandleAllocate(int nPingPongSplit, bool isFinal = false)
	{
		List<BoxOnGlb> list = new List<BoxOnGlb>();
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		GNNEShape inShape = new GNNEShape(1, _inputShape[3], 1, 1);
		GNNEShape wShape = new GNNEShape(_wXcShape[2], _wXcShape[3], 1, 1);
		GNNEShape wShape2 = new GNNEShape(_wRcShape[2], _wRcShape[3], 1, 1);
		wShape = ComputeRS(inShape, wShape);
		wShape2 = ComputeRS(inShape, wShape2);
		int[] obj = new int[4] { 1, 0, 1, 1 };
		obj[1] = _inputShape[3];
		TensorOnGlb tensorOnGlb = new TensorOnGlb(obj, _inputType, 0);
		int value = tensorOnGlb.GlbNByte * nPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4]
		{
			wShape[2],
			wShape[3],
			wShape[1],
			wShape[0]
		}, _wXcType, 0);
		int num = SpaceSearcher.GetWeightSize(wShape[2], wShape[3], wShape[1], wShape[0], TileUtilities.GetBytesPerElement(_wXcType)) * _wXcShape[1];
		tensorOnGlb2.AllocatedBytes = num / _wXcShape[1];
		num = TileUtilities.GetAlignedNum(num, GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb3 = new TensorOnGlb(new int[4]
		{
			1,
			1,
			_actXcShape[2],
			_actXcShape[3]
		}, _actXcType, 0);
		int value2 = tensorOnGlb3.GlbNByte * _actXcShape[1];
		value2 = TileUtilities.GetAlignedNum(value2, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb4 = new TensorOnGlb(new int[4]
		{
			wShape2[2],
			wShape2[3],
			wShape2[1],
			wShape2[0]
		}, _wRcType, 0);
		int num2 = SpaceSearcher.GetWeightSize(wShape2[2], wShape2[3], wShape2[1], wShape2[0], TileUtilities.GetBytesPerElement(_wRcType)) * _wRcShape[1];
		tensorOnGlb4.AllocatedBytes = num2 / _wRcShape[1];
		num2 = TileUtilities.GetAlignedNum(num2, GNNEEnv.WBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb5 = new TensorOnGlb(new int[4]
		{
			1,
			1,
			_actRcShape[2],
			_actRcShape[3]
		}, _actRcType, 0);
		int value3 = tensorOnGlb5.GlbNByte * _actRcShape[1];
		value3 = (tensorOnGlb5.AllocatedBytes = TileUtilities.GetAlignedNum(value3, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth));
		value3 *= 2;
		int[] obj2 = new int[4] { 1, 0, 1, 1 };
		obj2[1] = _initHShape[3];
		TensorOnGlb tensorOnGlb6 = new TensorOnGlb(obj2, _outputType, 0);
		int glbNByte = tensorOnGlb6.GlbNByte;
		glbNByte = TileUtilities.GetAlignedNum(glbNByte, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		int[] obj3 = new int[4] { 1, 0, 1, 1 };
		obj3[1] = _initCShape[3];
		TensorOnGlb tensorOnGlb7 = new TensorOnGlb(obj3, _outputCType, 0);
		int glbNByte2 = tensorOnGlb7.GlbNByte;
		glbNByte2 = TileUtilities.GetAlignedNum(glbNByte2, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		int[] obj4 = new int[4] { 1, 0, 1, 1 };
		obj4[1] = _outputShape[3] * 4;
		TensorOnGlb tensorOnGlb8 = new TensorOnGlb(obj4, DataTypes.Float16, 0);
		int glbNByte3 = tensorOnGlb8.GlbNByte;
		glbNByte3 = (tensorOnGlb8.AllocatedBytes = TileUtilities.GetAlignedNum(glbNByte3, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth));
		glbNByte3 *= 2;
		TensorOnGlb tensorOnGlb9 = new TensorOnGlb(new int[]{1,0,1,1}, DataTypes.Float16, 0);
		int glbNByte4 = tensorOnGlb9.GlbNByte;
		glbNByte4 = TileUtilities.GetAlignedNum(glbNByte4, 8 * GNNEEnv.GlbBankWidth);
		TensorOnGlb value4 = new TensorOnGlb(new int[]{1,0,1,1}, DataTypes.Float16, 0);
		int[] obj5 = new int[4] { 1, 1, 0, 7 };
		obj5[2] = _outputShape[3];
		TensorOnGlb tensorOnGlb10 = new TensorOnGlb(obj5, DataTypes.BFloat16, 0);
		int glbNByte5 = tensorOnGlb10.GlbNByte;
		glbNByte5 = TileUtilities.GetAlignedNum(glbNByte5, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		int[] obj6 = new int[4] { 1, 1, 0, 7 };
		obj6[2] = _outputShape[3];
		TensorOnGlb tensorOnGlb11 = new TensorOnGlb(obj6, DataTypes.Float16, 0);
		int glbNByte6 = tensorOnGlb11.GlbNByte;
		glbNByte6 = TileUtilities.GetAlignedNum(glbNByte6, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
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
			value / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.WBankWidth,
			num / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmWXc));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.ActBankWidth,
			value2 / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmActXc));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.WBankWidth,
			num2 / GNNEEnv.WBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmWRc));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.ActBankWidth,
			value3 / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmActRc));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmOfH));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte2 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmOfC));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte3 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte4 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmSegFittingParamFt));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			glbNByte4 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmSegFittingParamGt));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			glbNByte5 / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmBinAct));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			glbNByte6 / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.LstmBinQAct));
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.LstmWXc, tensorOnGlb2);
		dictionary.Add(ItemName.LstmActXc, tensorOnGlb3);
		dictionary.Add(ItemName.LstmWRc, tensorOnGlb4);
		dictionary.Add(ItemName.LstmActRc, tensorOnGlb5);
		dictionary.Add(ItemName.LstmOfH, tensorOnGlb6);
		dictionary.Add(ItemName.LstmOfC, tensorOnGlb7);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb8);
		dictionary.Add(ItemName.LstmSegFittingParamFt, tensorOnGlb9);
		dictionary.Add(ItemName.LstmSegFittingParamGt, value4);
		dictionary.Add(ItemName.LstmBinAct, tensorOnGlb10);
		dictionary.Add(ItemName.LstmBinQAct, tensorOnGlb11);
		if (_wXcType == DataTypes.UInt8 || _wXcType == DataTypes.Int16)
		{
			TensorOnGlb value5 = new TensorOnGlb(new int[]{1,0,1,1}, _wXcType, 0);
			int wQargSize = SpaceSearcher.GetWQargSize(_wXcShape[1] * _wXcShape[2], TileUtilities.GetBytesPerElement(_lwXcQarg[GNNELoadW.Input].CheckedDataType));
			wQargSize = TileUtilities.GetAlignedNum(wQargSize, GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.WQargBankWidth,
				wQargSize / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.LstmWXcQarg));
			dictionary.Add(ItemName.LstmWXcQarg, value5);
		}
		if (_wRcType == DataTypes.UInt8 || _wRcType == DataTypes.Int16)
		{
			TensorOnGlb value6 = new TensorOnGlb(new int[]{1,0,1,1}, _wRcType, 0);
			int wQargSize2 = SpaceSearcher.GetWQargSize(_wRcShape[1] * _wRcShape[2], TileUtilities.GetBytesPerElement(_lwRcQarg[GNNELoadW.Input].CheckedDataType));
			wQargSize2 = TileUtilities.GetAlignedNum(wQargSize2, GNNEEnv.WQargBankWidth * GNNEEnv.GlbBankWidth);
			list.Add(new BoxOnGlb(new int[2]
			{
				GNNEEnv.WQargBankWidth,
				wQargSize2 / GNNEEnv.WQargBankWidth / GNNEEnv.GlbBankWidth
			}, ItemName.LstmWRcQarg));
			dictionary.Add(ItemName.LstmWRcQarg, value6);
		}
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(new BoxPacker(16)
		{
			Boxes = list
		});
		if (allocateResult.IsOk)
		{
			foreach (KeyValuePair<ItemName, TensorOnGlb> item in dictionary)
			{
				if (allocateResult.Items.ContainsKey(item.Key))
				{
					item.Value.Mmu = allocateResult.Items[item.Key];
				}
			}
		}
		return new AllocateResult
		{
			IsOk = allocateResult.IsOk,
			Items = allocateResult.Items,
			GlbMap = dictionary
		};
	}

	private void InitParameters(Call lstmNode, Call ld, Call st, Call stH, Call stC)
	{
		_oldLstm = lstmNode;
		_of = st;
		_ofH = stH;
		_ofC = stC;
		_inputShape = new GNNEShape(_oldLstm[GNNELSTM.Input].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.Input].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.Input].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.Input].CheckedShape[3].FixedValue);
		_wXcShape = new GNNEShape(_oldLstm[GNNELSTM.WXc].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[3].FixedValue);
		_wXcShapeReshape = new GNNEShape(_oldLstm[GNNELSTM.WXc].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.WXc].CheckedShape[3].FixedValue);
		_actXcShape = new GNNEShape(_oldLstm[GNNELSTM.ActXc].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.ActXc].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.ActXc].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.ActXc].CheckedShape[3].FixedValue);
		_wRcShape = new GNNEShape(_oldLstm[GNNELSTM.WRc].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[3].FixedValue);
		_wRcShapeReshape = new GNNEShape(_oldLstm[GNNELSTM.WRc].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.WRc].CheckedShape[3].FixedValue);
		_actRcShape = new GNNEShape(_oldLstm[GNNELSTM.ActRc0].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.ActRc0].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.ActRc0].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.ActRc0].CheckedShape[3].FixedValue);
		_initHShape = new GNNEShape(_oldLstm[GNNELSTM.InitialH].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.InitialH].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.InitialH].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.InitialH].CheckedShape[3].FixedValue);
		_initCShape = new GNNEShape(_oldLstm[GNNELSTM.InitialC].CheckedShape[0].FixedValue, _oldLstm[GNNELSTM.InitialC].CheckedShape[1].FixedValue, _oldLstm[GNNELSTM.InitialC].CheckedShape[2].FixedValue, _oldLstm[GNNELSTM.InitialC].CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(st.CheckedShape[0].FixedValue, st.CheckedShape[1].FixedValue, st.CheckedShape[2].FixedValue, st.CheckedShape[3].FixedValue);
		_outputHShape = (((object)_ofH != null) ? new GNNEShape(stH.CheckedShape[0].FixedValue, stH.CheckedShape[1].FixedValue, stH.CheckedShape[2].FixedValue, stH.CheckedShape[3].FixedValue) : new GNNEShape(_initHShape.N, _initHShape.C, _initHShape.H, _initHShape.W));
		_outputCShape = (((object)_ofC != null) ? new GNNEShape(stC.CheckedShape[0].FixedValue, stC.CheckedShape[1].FixedValue, stC.CheckedShape[2].FixedValue, stC.CheckedShape[3].FixedValue) : new GNNEShape(_initCShape.N, _initCShape.C, _initCShape.H, _initCShape.W));
		_lIf = ld;
		_lWXc = (Call)_oldLstm[GNNELSTM.WXc];
		_lActXc = (Call)_oldLstm[GNNELSTM.ActXc];
		_lWRc = (Call)_oldLstm[GNNELSTM.WRc];
		_lActRc0 = (Call)_oldLstm[GNNELSTM.ActRc0];
		_lActRc1 = (Call)_oldLstm[GNNELSTM.ActRc1];
		_lInitH = (Call)_oldLstm[GNNELSTM.InitialH];
		_lInitC = (Call)_oldLstm[GNNELSTM.InitialC];
		_lSegFt = (Call)_oldLstm[GNNELSTM.SegFittingParamFt];
		_lSegGt = (Call)_oldLstm[GNNELSTM.SegFittingParamGt];
		_lwXcQarg = (Call)_oldLstm[GNNELSTM.WXcQarg];
		_lwRcQarg = (Call)_oldLstm[GNNELSTM.WRcQarg];
		_lwBinAct = (Call)_oldLstm[GNNELSTM.ActBin];
		_lwBinQAct = (Call)_oldLstm[GNNELSTM.ActBinQ];
		_of = st;
		_ofH = stH;
		_ofC = stC;
		_inputType = _oldLstm[GNNELSTM.Input].CheckedDataType;
		_wXcType = _oldLstm[GNNELSTM.WXc].CheckedDataType;
		_actXcType = _oldLstm[GNNELSTM.ActXc].CheckedDataType;
		_wRcType = _oldLstm[GNNELSTM.WRc].CheckedDataType;
		_actRcType = _oldLstm[GNNELSTM.ActRc0].CheckedDataType;
		_initHType = _oldLstm[GNNELSTM.InitialH].CheckedDataType;
		_initCType = _oldLstm[GNNELSTM.InitialC].CheckedDataType;
		_outputType = ((TensorType)((TupleType)_oldLstm.CheckedType)[0]).DType;
		_outputHType = (((object)_ofH != null) ? _ofH[GNNEStore.Input].CheckedDataType : _outputType);
		_outputCType = (((object)_ofC != null) ? _ofC[GNNEStore.Input].CheckedDataType : DataTypes.Float16);
		_weightGroups.Add(new WeightGroupHandler(_wXcType, _wXcType));
		_weightGroups.Add(new WeightGroupHandler(_wRcType, _wRcType));
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr output = (Expr)__result["output"];
		Call midCall = (Call)__result["midCall"];
		IReadOnlyList<Expr> midCallParams = (IReadOnlyList<Expr>)__result["midCallParams"];
		return GetReplace(output, midCall, midCallParams);
	}
}
