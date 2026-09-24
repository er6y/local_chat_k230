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
public class TilePad : RewriteRule<Pattern>
{
	private static int _count = -1;

	private CcrHandler? _ccrHandler = new CcrHandler();

	private GprHandler? _gpr = new GprHandler();

	private SsrHandler? _ssr = new SsrHandler();

	private DataType? _inputType;

	private DataType? _outputType;

	private Call? _pad;

	private Call? _lif;

	private Call? _sof;

	private GNNEShape? _inputShape;

	private GNNEShape? _outputShape;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNEPadFusion();


	private PrimFunction GetReplace(Call call, Call ld, Call st)
	{
		_count++;
		int[] array = ld[GNNELoad.Input].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		InitParameters(call, ld, st);
		TiledGlb glb = SearchGlbParameters();
		List<GnneAction> actions = BuildSchedule(call, glb, buffer, buffer2);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TilePad_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private void InitParameters(Call padNode, Call ld, Call st)
	{
		_pad = padNode;
		_lif = ld;
		_sof = st;
		_inputType = _lif.CheckedDataType;
		_outputType = _pad.CheckedDataType;
		_inputShape = new GNNEShape(_lif.CheckedShape[0].FixedValue, _lif.CheckedShape[1].FixedValue, _lif.CheckedShape[2].FixedValue, _lif.CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(_pad.CheckedShape[0].FixedValue, _pad.CheckedShape[1].FixedValue, _pad.CheckedShape[2].FixedValue, _pad.CheckedShape[3].FixedValue);
	}

	private TileConv2DGlb SearchGlbParameters()
	{
		int n = 1;
		if (GNNEEnv.BatchInference)
		{
			n = Math.Min(4, _inputShape[0]);
		}
		int i = 1;
		int j = 1;
		int num = _outputShape[3];
		AllocateResult allocateResult;
		for (; j < _outputShape[2]; j++)
		{
			allocateResult = HandleAllocate(n, i, j + 1, num);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; i < _outputShape[1]; i++)
		{
			allocateResult = HandleAllocate(n, i + 1, j, num);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		allocateResult = HandleAllocate(n, i, j, num, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePad.cs", 103);
		GNNEShape gNNEShape = new GNNEShape(n, i, j, num);
		return new TileConv2DGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private AllocateResult HandleAllocate(int n, int m, int e, int f, bool isFinal = false)
	{
		List<BoxOnGlb> list = new List<BoxOnGlb>();
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		TileUtilities.GetBytesPerElement(_inputType);
		int num = (int)Math.Ceiling(1.0 * (double)m / (double)GNNEEnv.NPingPongSplit);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, num, e, f }, _inputType, 0);
		int allocatedBytes = tensorOnGlb.AllocatedBytes;
		allocatedBytes = (tensorOnGlb.AllocatedBytes = TileUtilities.GetAlignedNum(allocatedBytes, GNNEEnv.OfmapBankWidth * GNNEEnv.GlbBankWidth));
		allocatedBytes *= GNNEEnv.NPingPongSplit;
		TensorOnGlb tensorOnGlb2 = tensorOnGlb;
		int basementSize = SpaceSearcher.GetBasementSize();
		basementSize = TileUtilities.GetAlignedNum(basementSize, GNNEEnv.GlbWidth * GNNEEnv.GlbBankWidth);
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.GlbWidth,
			basementSize / GNNEEnv.GlbWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Basement));
		list.Add(new BoxOnGlb(new int[2]
		{
			GNNEEnv.OfmapBankWidth,
			allocatedBytes / GNNEEnv.OfmapBankWidth / GNNEEnv.GlbBankWidth
		}, ItemName.Ofmap));
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(new BoxPacker(16)
		{
			Boxes = list
		});
		if (allocateResult.IsOk)
		{
			tensorOnGlb2.Mmu = allocateResult.Items[ItemName.Ofmap];
			tensorOnGlb.Mmu = allocateResult.Items[ItemName.Ofmap];
		}
		dictionary.Add(ItemName.Ifmap, tensorOnGlb2);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb);
		return new AllocateResult
		{
			IsOk = allocateResult.IsOk,
			Items = allocateResult.Items,
			GlbMap = dictionary
		};
	}

	private List<GnneAction> BuildSchedule(Call pad, TiledGlb glb, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf)
	{
		int[] array = ((TensorConst)pad[GNNEPad.Pads]).Value.ToArray<int>();
		Padding p = new Padding(array[0], array[1]);
		Padding p2 = new Padding(array[2], array[3]);
		Padding p3 = new Padding(array[4], array[5]);
		Padding p4 = new Padding(array[6], array[7]);
		DataType checkedDataType = pad.CheckedDataType;
		DataType dataType = checkedDataType;
		if ((object)dataType != null)
		{
			short num;
			if (dataType == DataTypes.UInt8)
			{
				num = ((TensorConst)pad[GNNEPad.Value]).Value.ToScalar<byte>();
			}
			else if (dataType == DataTypes.Int8)
			{
				num = ((TensorConst)pad[GNNEPad.Value]).Value.ToScalar<sbyte>();
			}
			else if (dataType == DataTypes.Int16)
			{
				num = ((TensorConst)pad[GNNEPad.Value]).Value.ToScalar<short>();
			}
			else
			{
				if (!(dataType == DataTypes.Float16))
				{
					goto IL_0121;
				}
				num = BitConverter.ToInt16(BitConverter.GetBytes(((TensorConst)pad[GNNEPad.Value]).Value.ToScalar<Half>()), 0);
			}
			int value = num;
			List<GnneAction> list = new List<GnneAction>();
			GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
			SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
			CcrHandler ccrHandler = new CcrHandler();
			GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
			gnneActionUpdater.UpdateMmuConf();
			foreach (Segment1D item in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _outputShape[0]))
			{
				Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(item.Start, item.Length, _inputShape[0], 1, 1, 1, in p);
				foreach (Segment1D item2 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _outputShape[1]))
				{
					Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(item2.Start, item2.Length, _inputShape[1], 1, 1, 1, in p2);
					foreach (Segment1D item3 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _outputShape[2]))
					{
						Segment1D inputRowSegment3 = TileUtilities.GetInputRowSegment(item3.Start, item3.Length, _inputShape[2], 1, 1, 1, in p3);
						foreach (Segment1D item4 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _outputShape[3]))
						{
							Segment1D inputRowSegment4 = TileUtilities.GetInputRowSegment(item4.Start, item4.Length, _inputShape[3], 1, 1, 1, in p4);
							SegmentND segmentND = new SegmentND(item, item2, item3, item4);
							SegmentND segmentND2 = new SegmentND(inputRowSegment, inputRowSegment2, inputRowSegment3, inputRowSegment4);
							int chunkSize = (int)Math.Ceiling((float)item2.Length / (float)GNNEEnv.NPingPongSplit);
							List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(item2.Start, chunkSize, item2.End);
							for (int i = 0; i < segmentStartEndLength.Count; i++)
							{
								Segment1D segment1D = segmentStartEndLength[i];
								Segment1D inputRowSegment5 = TileUtilities.GetInputRowSegment(segment1D.Start, segment1D.Length, _inputShape[1], 1, 1, 1, in p2);
								SegmentND ifmap = new SegmentND(segmentND2[0], inputRowSegment5, segmentND2[2], segmentND2[3]);
								SegmentND segmentND3 = new SegmentND(segmentND[0], segment1D, segmentND[2], segmentND[3]);
								gnneActionUpdater.UpdateMfuMemset(segmentND3, segmentND3, value, _inputType, glb.GlbMap[ItemName.Ofmap].Mmu.Id, i);
								List<CcrSet> ccrsToSet = new List<CcrSet>
								{
									new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, i)), 1)
								};
								int num2 = inputRowSegment.Padding.Before * segmentND3[1].Length * segmentND3[2].Length * segmentND3[3].Length + inputRowSegment5.Padding.Before * segmentND3[2].Length * segmentND3[3].Length + inputRowSegment3.Padding.Before * segmentND3[3].Length + inputRowSegment4.Padding.Before;
								num2 *= TileUtilities.GetBytesPerElement(_inputType);
								gnneActionUpdater.UpdateLoadIf(ifmap, _lif, i, ddrIf, num2, new List<int>
								{
									segmentND[1].Length,
									segmentND[2].Length,
									segmentND[3].Length
								}, ItemName.Ofmap, ccrsToSet);
								List<CcrClr> ccrsToClr = new List<CcrClr>
								{
									new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, i)))
								};
								gnneActionUpdater.UpdateStoreT(segmentND3, _sof, i, ddrOf, 0, null, null, ccrsToClr);
							}
						}
					}
				}
			}
			TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePad.cs", 242);
			return list;
		}
		goto IL_0121;
		IL_0121:
		throw new ArgumentOutOfRangeException(checkedDataType.GetDisplayName());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, ld, st);
	}
}
