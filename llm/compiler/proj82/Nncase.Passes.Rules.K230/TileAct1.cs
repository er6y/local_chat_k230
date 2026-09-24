using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TileAct1 : RewriteRule<Pattern>
{
	private static int _count = -1;

	private DataType? _inputAType;

	private DataType? _inputBType;

	private DataType? _outputType;

	private Call? _act1;

	private Call? _lif1;

	private Call? _lif2;

	private Call? _lact;

	private Call? _sof;

	private GNNEShape? _inputAShape;

	private GNNEShape? _inputBShape;

	private GNNEShape? _outputShape;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNEActivationFusion();


	private List<GnneAction> BuildSchedule(TiledGlb glb, Call act, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf, Nncase.TIR.Buffer ddrAct, Nncase.TIR.Buffer ddrIf2)
	{
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		gnneActionUpdater.UpdateMmuConf();
		gnneActionUpdater.UpdateLoadAct(_lact, ddrAct, ItemName.MfuAct1);
		if (act[GNNEActivation.InputB] != None.Default)
		{
			GNNEShape gNNEShape = new GNNEShape(0, 0, 0, 0);
			for (int i = 0; i < 4; i++)
			{
				gNNEShape[i] = Math.Min(_inputAShape[i], _inputBShape[i]);
			}
			GNNEShape gNNEShape2 = new GNNEShape(0, 0, 0, 0);
			GNNEShape gNNEShape3 = new GNNEShape(0, 0, 0, 0);
			GNNEShape gNNEShape4 = new GNNEShape(0, 0, 0, 0);
			for (int j = 0; j < 4; j++)
			{
				gNNEShape2[j] = ((gNNEShape[j] == 1) ? 1 : (_inputAShape[j] / gNNEShape[j]));
				gNNEShape3[j] = ((gNNEShape[j] == 1) ? 1 : (_inputBShape[j] / gNNEShape[j]));
				gNNEShape4[j] = ((gNNEShape[j] == 1) ? 1 : (_outputShape[j] / gNNEShape[j]));
			}
			foreach (Segment1D item in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _outputShape[0]))
			{
				Segment1D segment1D = item / (gNNEShape4[0] / gNNEShape2[0]);
				Segment1D segment1D2 = item / (gNNEShape4[0] / gNNEShape3[0]);
				if (_inputAShape[0] == 1)
				{
					segment1D = new Segment1D(..1, new Padding(0, 0));
				}
				if (_inputBShape[0] == 1)
				{
					segment1D2 = new Segment1D(..1, new Padding(0, 0));
				}
				foreach (Segment1D item2 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _outputShape[1]))
				{
					Segment1D segment1D3 = item2 / (gNNEShape4[1] / gNNEShape2[1]);
					Segment1D segment1D4 = item2 / (gNNEShape4[1] / gNNEShape3[1]);
					if (_inputAShape[1] == 1)
					{
						segment1D3 = new Segment1D(..1, Padding.Zero());
					}
					if (_inputBShape[1] == 1)
					{
						segment1D4 = new Segment1D(..1, new Padding(0, 0));
					}
					foreach (Segment1D item3 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _outputShape[2]))
					{
						Segment1D segment1D5 = item3 / (gNNEShape4[2] / gNNEShape2[2]);
						Segment1D segment1D6 = item3 / (gNNEShape4[2] / gNNEShape3[2]);
						if (_inputAShape[2] == 1)
						{
							segment1D5 = new Segment1D(..1, Padding.Zero());
						}
						if (_inputBShape[2] == 1)
						{
							segment1D6 = new Segment1D(..1, new Padding(0, 0));
						}
						foreach (Segment1D item4 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]))
						{
							Segment1D segment1D7 = item4 / (gNNEShape4[3] / gNNEShape2[3]);
							Segment1D segment1D8 = item4 / (gNNEShape4[3] / gNNEShape3[3]);
							if (_inputAShape[3] == 1)
							{
								segment1D7 = new Segment1D(..1, Padding.Zero());
							}
							if (_inputBShape[3] == 1)
							{
								segment1D8 = new Segment1D(..1, Padding.Zero());
							}
							SegmentND segmentND = new SegmentND(item.Start..item.End, item2, item3, item4);
							SegmentND segmentND2 = new SegmentND(segment1D, segment1D3, segment1D5, segment1D7);
							SegmentND segmentND3 = new SegmentND(segment1D2, segment1D4, segment1D6, segment1D8);
							int num = Math.Min(segment1D3.Length, segment1D4.Length);
							int num2 = (int)Math.Ceiling(1f * (float)num / (float)glb.NPingPongSplit);
							List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(segment1D3.Start, num2 * segment1D3.Length / num, segment1D3.End);
							List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(segment1D4.Start, num2 * segment1D4.Length / num, segment1D4.End);
							List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(item2.Start, num2 * item2.Length / num, item2.End);
							for (int k = 0; k < segmentStartEndLength3.Count; k++)
							{
								Segment1D segment1D9 = segmentStartEndLength[k];
								Segment1D segment1D10 = segmentStartEndLength2[k];
								Segment1D segment1D11 = segmentStartEndLength3[k];
								SegmentND segmentND4 = new SegmentND(segmentND2[0], segment1D9, segmentND2[2], segmentND2[3]);
								SegmentND segmentND5 = new SegmentND(segmentND3[0], segment1D10, segmentND3[2], segmentND3[3]);
								SegmentND ofmap = new SegmentND(segmentND[0], segment1D11, segmentND[2], segmentND[3]);
								List<CcrSet> ccrsToSet = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, k)), 1)
								};
								gnneActionUpdater.UpdateLoadIf(segmentND4, _lif1, k, ddrIf, 0, null, ItemName.Ifmap, ccrsToSet);
								List<CcrSet> ccrsToSet2 = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap2, k)), 1)
								};
								gnneActionUpdater.UpdateLoadIf(segmentND5, _lif2, k, ddrIf2, 0, null, ItemName.Ifmap2, ccrsToSet2);
								List<CcrSet> ccrsToSet3 = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, k)), 1)
								};
								List<CcrClr> list2 = new List<CcrClr>
								{
									new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, k)))
								};
								list2.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap2, k))));
								List<int> src1Stride = new List<int>
								{
									segmentND4[1].Length,
									segmentND4[2].Length,
									segmentND4[3].Length
								};
								List<int> src2Stride = new List<int>
								{
									segmentND5[1].Length,
									segmentND5[2].Length,
									segmentND5[3].Length
								};
								gnneActionUpdater.UpdateMfuAct1(segmentND4, segmentND5, ofmap, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, _inputAType, _inputBType, _outputType, (_inputAType == DataTypes.Float16) ? new DeQuantizeParam(0, 1f) : ((TensorConst)act[GNNEActivation.DeqAParams]).Value.ToScalar<DeQuantizeParam>(), (_inputBType == DataTypes.Float16) ? new DeQuantizeParam(0, 1f) : ((TensorConst)act[GNNEActivation.DeqBParams]).Value.ToScalar<DeQuantizeParam>(), ((TensorConst)act[GNNEActivation.InAShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.InBShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.OutShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.Is16Segments]).Value.ToScalar<bool>(), k, ccrsToSet3, list2, 0, 0, 0, 0, ItemName.Ifmap2, ItemName.Ifmap, ItemName.MfuAct1, (((GNNEActivation)act.Target).Type == GnneActivationType.Mul) ? MFU_ACT1_FUNCTION.mul : MFU_ACT1_FUNCTION.add, ItemName.Ofmap, src1Stride, src2Stride);
								List<CcrClr> ccrsToClr = new List<CcrClr>
								{
									new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, k)))
								};
								gnneActionUpdater.UpdateStoreT(ofmap, _sof, k, ddrOf, 0, null, null, ccrsToClr);
							}
						}
					}
				}
			}
		}
		else
		{
			GNNEShape gNNEShape5 = new GNNEShape(0, 0, 0, 0);
			GNNEShape gNNEShape6 = new GNNEShape(0, 0, 0, 0);
			for (int l = 0; l < 4; l++)
			{
				gNNEShape5[l] = 1;
				gNNEShape6[l] = ((_inputAShape[l] == 1) ? 1 : (_outputShape[l] / _inputAShape[l]));
			}
			foreach (Segment1D item5 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _outputShape[0]))
			{
				Segment1D segment1D12 = item5 / (gNNEShape6[0] / gNNEShape5[0]);
				if (_inputAShape[0] == 1)
				{
					segment1D12 = new Segment1D(0..1, Padding.Zero());
				}
				foreach (Segment1D item6 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _outputShape[1]))
				{
					Segment1D segment1D13 = item6 / (gNNEShape6[1] / gNNEShape5[1]);
					if (_inputAShape[1] == 1)
					{
						segment1D13 = new Segment1D(0..1, Padding.Zero());
					}
					foreach (Segment1D item7 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _outputShape[2]))
					{
						Segment1D segment1D14 = item7 / (gNNEShape6[2] / gNNEShape5[2]);
						if (_inputAShape[2] == 1)
						{
							segment1D14 = new Segment1D(0..1, Padding.Zero());
						}
						foreach (Segment1D item8 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]))
						{
							Segment1D segment1D15 = item8 / (gNNEShape6[3] / gNNEShape5[3]);
							if (_inputAShape[3] == 1)
							{
								segment1D15 = new Segment1D(0..1, Padding.Zero());
							}
							SegmentND segmentND6 = new SegmentND(item5, item6, item7, item8);
							SegmentND segmentND7 = new SegmentND(segment1D12, segment1D13, segment1D14, segment1D15);
							int chunkSize = (int)Math.Ceiling(1.0 * (double)segment1D13.Length / (double)glb.NPingPongSplit);
							List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(segment1D13.Start, chunkSize, segment1D13.End);
							for (int m = 0; m < segmentStartEndLength4.Count; m++)
							{
								Segment1D segment1D16 = segmentStartEndLength4[m];
								SegmentND segmentND8 = new SegmentND(segmentND7[0], segment1D16, segmentND7[2], segmentND7[3]);
								Segment1D segment1D17 = new Segment1D(0..0, Padding.Zero());
								SegmentND ifmap = new SegmentND(segment1D17, segment1D17, segment1D17, segment1D17);
								SegmentND ofmap2 = new SegmentND(segmentND6[0], segment1D16, segmentND6[2], segmentND6[3]);
								List<CcrSet> ccrsToSet4 = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, m)), 1)
								};
								List<int> stridesD = new List<int>
								{
									glb.GlbMap[ItemName.Ifmap].Dimensions[1],
									glb.GlbMap[ItemName.Ifmap].Dimensions[2],
									glb.GlbMap[ItemName.Ifmap].Dimensions[3]
								};
								gnneActionUpdater.UpdateLoadIf(segmentND8, _lif1, m, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet4);
								List<CcrSet> ccrsToSet5 = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, m)), 1)
								};
								List<CcrClr> ccrsToClr2 = new List<CcrClr>
								{
									new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, m)))
								};
								gnneActionUpdater.UpdateMfuAct1(segmentND8, ifmap, ofmap2, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, _inputAType, _inputAType, _outputType, (_inputAType == DataTypes.Float16) ? new DeQuantizeParam(0, 1f) : ((TensorConst)act[GNNEActivation.DeqAParams]).Value.ToScalar<DeQuantizeParam>(), (_inputBType == DataTypes.Float16) ? new DeQuantizeParam(0, 1f) : ((TensorConst)act[GNNEActivation.DeqBParams]).Value.ToScalar<DeQuantizeParam>(), ((TensorConst)act[GNNEActivation.InAShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.InBShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.OutShiftBits]).Value.ToScalar<int>(), ((TensorConst)act[GNNEActivation.Is16Segments]).Value.ToScalar<bool>(), m, ccrsToSet5, ccrsToClr2, 0, 0, 0, 0, ItemName.Ifmap2, ItemName.Ifmap, ItemName.MfuAct1, (((GNNEActivation)act.Target).Type == GnneActivationType.Mul) ? MFU_ACT1_FUNCTION.mul : MFU_ACT1_FUNCTION.add);
								List<CcrClr> ccrsToClr3 = new List<CcrClr>
								{
									new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, m)))
								};
								gnneActionUpdater.UpdateStoreT(ofmap2, _sof, m, ddrOf, 0, null, null, ccrsToClr3);
							}
						}
					}
				}
			}
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAct1.cs", 249);
		return list;
	}

	private TileAct1Glb SearchGlbParameters(Call ld, Call st, Call act)
	{
		GNNEShape scaleA;
		GNNEShape scaleB;
		GNNEShape scaleOut;
		GNNEShape o;
		GNNEShape a;
		GNNEShape b;
		if (act[GNNEActivation.InputB] != None.Default)
		{
			GNNEShape gNNEShape = new GNNEShape(0, 0, 0, 0);
			for (int i = 0; i < 4; i++)
			{
				gNNEShape[i] = Math.Min(_inputAShape[i], _inputBShape[i]);
			}
			scaleA = new GNNEShape(0, 0, 0, 0);
			scaleB = new GNNEShape(0, 0, 0, 0);
			scaleOut = new GNNEShape(0, 0, 0, 0);
			for (int j = 0; j < 4; j++)
			{
				scaleA[j] = ((gNNEShape[j] == 1) ? 1 : (_inputAShape[j] / gNNEShape[j]));
				scaleB[j] = ((gNNEShape[j] == 1) ? 1 : (_inputBShape[j] / gNNEShape[j]));
				scaleOut[j] = ((gNNEShape[j] == 1) ? 1 : (_outputShape[j] / gNNEShape[j]));
			}
			int num = GNNEEnv.NPingPongSplit;
			int num2 = scaleOut[0];
			int num3 = Math.Min(scaleOut[1] * num, _outputShape[1]);
			if (num3 == 1)
			{
				num = 1;
			}
			int num4 = scaleOut[2];
			int num5 = scaleOut[3];
			o = new GNNEShape(num2, num3, num4, num5);
			a = new GNNEShape(0, 0, 0, 0);
			b = new GNNEShape(0, 0, 0, 0);
			CalcInputShape();
			AllocateResult allocateResult;
			while (o[3] < _outputShape[3])
			{
				o[3] = Math.Min(o[3] + scaleOut[3], _outputShape[3]);
				CalcInputShape();
				allocateResult = HandleAllocate(o, a, b, num);
				if (allocateResult.IsOk)
				{
					num5 = o[3];
					continue;
				}
				o[3] = num5;
				break;
			}
			while (o[2] < _outputShape[2])
			{
				o[2] = Math.Min(o[2] + scaleOut[2] * num, _outputShape[2]);
				CalcInputShape();
				allocateResult = HandleAllocate(o, a, b, num);
				if (allocateResult.IsOk)
				{
					num4 = o[2];
					continue;
				}
				o[2] = num4;
				break;
			}
			while (o[1] < _outputShape[1])
			{
				o[1] = Math.Min(o[1] + scaleOut[1], _outputShape[1]);
				CalcInputShape();
				allocateResult = HandleAllocate(o, a, b, num);
				if (allocateResult.IsOk)
				{
					num3 = o[1];
					continue;
				}
				o[1] = num3;
				break;
			}
			while (o[0] < _outputShape[0])
			{
				o[0] = Math.Min(o[0] + scaleOut[0], _outputShape[0]);
				CalcInputShape();
				allocateResult = HandleAllocate(o, a, b, num);
				if (allocateResult.IsOk)
				{
					num2 = o[0];
					continue;
				}
				o[0] = num2;
				break;
			}
			CalcInputShape();
			allocateResult = HandleAllocate(o, a, b, num);
			TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAct1.cs", 368);
			GNNEShape gNNEShape2 = new GNNEShape(num2, num3, num4, num5);
			return new TileAct1Glb(allocateResult.GlbMap, allocateResult.Items, gNNEShape2.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
		}
		int k = 1;
		int l = 1;
		int num6 = 1;
		int w = _inputAShape[3];
		int num7 = _outputShape[2] / _inputAShape[2];
		int num8 = _outputShape[3];
		int nPingPongSplit = GNNEEnv.NPingPongSplit;
		AllocateResult allocateResult2 = HandleAllocate(k, l, num6, w, num7, num8, nPingPongSplit);
		if (!allocateResult2.IsOk)
		{
			nPingPongSplit = 1;
		}
		for (; l < _inputAShape[1]; l++)
		{
			allocateResult2 = HandleAllocate(k, l + 1, num6, w, num7, num8, nPingPongSplit);
			if (!allocateResult2.IsOk)
			{
				break;
			}
		}
		while (num6 < _inputAShape[2])
		{
			int num9 = num6 + 1;
			int e = num9 * (_outputShape[2] / _inputAShape[2]);
			allocateResult2 = HandleAllocate(k, l, num9, w, e, num8, nPingPongSplit);
			if (!allocateResult2.IsOk)
			{
				break;
			}
			num6++;
			num7 = num6 * (_outputShape[2] / _inputAShape[2]);
		}
		for (; k < _inputAShape[0]; k++)
		{
			allocateResult2 = HandleAllocate(k + 1, l, num6, w, num7, num8, nPingPongSplit);
			if (!allocateResult2.IsOk)
			{
				break;
			}
		}
		allocateResult2 = HandleAllocate(k, l, num6, w, num7, num8, nPingPongSplit, isFinal: true);
		TileUtilities.Assert(allocateResult2.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileAct1.cs", 437);
		GNNEShape gNNEShape3 = new GNNEShape(k, l, num7, num8);
		return new TileAct1Glb(allocateResult2.GlbMap, allocateResult2.Items, gNNEShape3.Dims, GNNEEnv.NPingPongSplit, allocateResult2.GlbMap[ItemName.Ifmap], allocateResult2.GlbMap[ItemName.Ofmap]);
		void CalcInputShape()
		{
			for (int m = 0; m < 4; m++)
			{
				a[m] = Math.Min(o[m] / scaleOut[m] * scaleA[m], _inputAShape[m]);
				b[m] = Math.Min(o[m] / scaleOut[m] * scaleB[m], _inputBShape[m]);
			}
		}
	}

	private AllocateResult HandleAllocate(int n, int c, int h, int w, int e, int f, int nPingPongSplit, bool isFinal = false)
	{
		BoxPacker boxPacker = new BoxPacker(16);
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		int num = (int)Math.Ceiling(1f * (float)c / (float)nPingPongSplit);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, num, h, w }, _inputAType, 0);
		int value = tensorOnGlb.AllocatedBytes * nPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { n, num, e, f }, _outputType, 0);
		int value2 = tensorOnGlb2.AllocatedBytes * nPingPongSplit;
		value2 = TileUtilities.GetAlignedNum(value2, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		bool num2 = ((TensorConst)_act1[GNNEActivation.Is16Segments]).Value.ToScalar<bool>();
		int c2 = (num2 ? 1 : _inputAShape[1]);
		int nActParam = (num2 ? 49 : GNNEEnv.ActNumPerChan);
		int actSize = SpaceSearcher.GetActSize(c2, nActParam, TileUtilities.GetBytesPerElement(_lact.CheckedDataType));
		actSize = TileUtilities.GetAlignedNum(actSize, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			value / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			value2 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.ActBankWidth,
			actSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.MfuAct1));
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(boxPacker);
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb2);
		dictionary.Add(ItemName.MfuAct1, new TensorOnGlb(new int[]{0,0,0,0}, _lact.CheckedDataType, 0));
		if (allocateResult.IsOk)
		{
			foreach (KeyValuePair<ItemName, TensorOnGlb> item in dictionary)
			{
				item.Value.Mmu = allocateResult.Items[item.Key];
			}
		}
		return new AllocateResult
		{
			IsOk = allocateResult.IsOk,
			Items = allocateResult.Items,
			GlbMap = dictionary
		};
	}

	private AllocateResult HandleAllocate(GNNEShape o, GNNEShape a, GNNEShape b, int nPingPongSplit, bool isFinal = false)
	{
		BoxPacker boxPacker = new BoxPacker(16);
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		int num = Math.Min(a[1], b[1]);
		int num2 = (int)Math.Ceiling(1f * (float)num / (float)nPingPongSplit);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4]
		{
			a[0],
			a[1] / num * num2,
			a[2],
			a[3]
		}, _inputAType, 0);
		int value = tensorOnGlb.AllocatedBytes * nPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4]
		{
			b[0],
			b[1] / num * num2,
			b[2],
			b[3]
		}, _inputBType, 0);
		int value2 = tensorOnGlb2.AllocatedBytes * nPingPongSplit;
		value2 = TileUtilities.GetAlignedNum(value2, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb3 = new TensorOnGlb(new int[4]
		{
			o[0],
			o[1] / num * num2,
			o[2],
			o[3]
		}, _outputType, 0);
		int value3 = tensorOnGlb3.AllocatedBytes * nPingPongSplit;
		value3 = TileUtilities.GetAlignedNum(value3, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		bool num3 = ((TensorConst)_act1[GNNEActivation.Is16Segments]).Value.ToScalar<bool>();
		int c = (num3 ? 1 : _inputAShape[1]);
		int nActParam = (num3 ? 49 : GNNEEnv.ActNumPerChan);
		int actSize = SpaceSearcher.GetActSize(c, nActParam, TileUtilities.GetBytesPerElement(_lact.CheckedDataType));
		actSize = TileUtilities.GetAlignedNum(actSize, GNNEEnv.ActBankWidth * GNNEEnv.GlbBankWidth);
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			value / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			value2 / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap2));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			value3 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.ActBankWidth,
			actSize / GNNEEnv.ActBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.MfuAct1));
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(boxPacker);
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Ifmap2, tensorOnGlb2);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb3);
		dictionary.Add(ItemName.MfuAct1, new TensorOnGlb(new int[] { 1, 1, 1, 1 }, _lact.CheckedDataType, 0));
		if (allocateResult.IsOk)
		{
			foreach (KeyValuePair<ItemName, TensorOnGlb> item in dictionary)
			{
				item.Value.Mmu = allocateResult.Items[item.Key];
			}
		}
		return new AllocateResult
		{
			IsOk = allocateResult.IsOk,
			Items = allocateResult.Items,
			GlbMap = dictionary
		};
	}

	private AllocateResult HandleAllocate(int n, int c, int h, int w, int e, int f, DataType inType, DataType outType, bool isFinal = false)
	{
		BoxPacker boxPacker = new BoxPacker(16);
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(inType) == 1) ? 32 : 16);
		int alignedNum = TileUtilities.GetAlignedNum(w, alignmentFactor);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, c, h, alignedNum }, inType, 0);
		int value = tensorOnGlb.GlbNByte * GNNEEnv.NPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		alignmentFactor = ((TileUtilities.GetBytesPerElement(outType) == 1) ? 32 : 16);
		int alignedNum2 = TileUtilities.GetAlignedNum(f, alignmentFactor);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { n, c, e, alignedNum2 }, outType, 0);
		int value2 = tensorOnGlb2.GlbNByte * GNNEEnv.NPingPongSplit;
		value2 = TileUtilities.GetAlignedNum(value2, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth);
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.IfmapBankWidth,
			value / GNNEEnv.IfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ifmap));
		boxPacker.Boxes.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			value2 / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(boxPacker);
		if (allocateResult.IsOk)
		{
			tensorOnGlb.Mmu = allocateResult.Items[ItemName.Ifmap];
			tensorOnGlb2.Mmu = allocateResult.Items[ItemName.Ofmap];
		}
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb2);
		allocateResult.GlbMap = dictionary;
		return allocateResult;
	}

	private void InitParameters(Call act, Call ld, Call st)
	{
		_act1 = act;
		_inputAType = _act1[GNNEActivation.InputA].CheckedDataType;
		_inputBType = ((_act1[GNNEActivation.InputB] != None.Default) ? _act1[GNNEActivation.InputB].CheckedDataType : DataTypes.Float16);
		_outputType = act.CheckedDataType;
		_lif1 = ld;
		if (_act1[GNNEActivation.InputB] != None.Default)
		{
			_lif2 = (Call)_act1[GNNEActivation.InputB];
		}
		_lact = (Call)_act1[GNNEActivation.Act];
		_sof = st;
		_inputAShape = new GNNEShape(_lif1.CheckedShape[0].FixedValue, _lif1.CheckedShape[1].FixedValue, _lif1.CheckedShape[2].FixedValue, _lif1.CheckedShape[3].FixedValue);
		_inputBShape = ((_act1[GNNEActivation.InputB] != None.Default) ? new GNNEShape(_lif2.CheckedShape[0].FixedValue, _lif2.CheckedShape[1].FixedValue, _lif2.CheckedShape[2].FixedValue, _lif2.CheckedShape[3].FixedValue) : _inputAShape);
		_outputShape = new GNNEShape(st.CheckedShape[0].FixedValue, st.CheckedShape[1].FixedValue, st.CheckedShape[2].FixedValue, st.CheckedShape[3].FixedValue);
	}

	private PrimFunction GetReplace(Call call, GNNEActivation callOp, Call ld, Call st)
	{
		_count++;
		InitParameters(call, ld, st);
		TileAct1Glb glb = SearchGlbParameters(ld, st, call);
		int[] array = ld.Arguments[0].CheckedShape.ToValueArray();
		int[] array2 = array;
		if (call[GNNEActivation.InputB] != None.Default)
		{
			array2 = call[GNNEActivation.InputB].CheckedShape.ToValueArray();
		}
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		Nncase.TIR.Buffer buffer2 = buffer;
		List<Nncase.TIR.Buffer> list = new List<Nncase.TIR.Buffer> { buffer };
		if (call[GNNEActivation.InputB] != None.Default)
		{
			if (!(((Call)call[GNNEActivation.InputB])[GNNELoad.Input] is TensorConst))
			{
				T.CreateBuffer(new TensorType(((Call)call[GNNEActivation.InputB])[GNNELoad.Input].CheckedDataType, array2), MemoryLocation.Input, out buffer2, "ddrIf2");
				list.Add(buffer2);
			}
			else
			{
				T.AttachBuffer((TensorConst)((Call)call[GNNEActivation.InputB])[GNNELoad.Input], out buffer2, "ddrIf2");
			}
		}
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer3, "var ddrOf");
		list.Add(buffer3);
		T.AttachBuffer((TensorConst)((Call)call[GNNEActivation.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer4, "var ddrAct");
		List<GnneAction> actions = BuildSchedule(glb, call, ld, st, buffer, buffer3, buffer4, buffer2);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileAct1_{_count}", K230RtModule.Kind, list.ToArray()).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		GNNEActivation callOp = (GNNEActivation)__result["callOp"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, callOp, ld, st);
	}
}
