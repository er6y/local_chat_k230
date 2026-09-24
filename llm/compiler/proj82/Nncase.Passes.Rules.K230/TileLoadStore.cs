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
public class TileLoadStore : RewriteRule<Pattern>
{
	private static int _count = -1;

	private CcrHandler _ccrHandler = new CcrHandler();

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private DataType? _inputType;

	private DataType? _outputType;

	private Call? _lif;

	private Call? _sof;

	private GNNEShape? _inputShape;

	private GNNEShape? _outputShape;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNELoadStoreFusion();


	private PrimFunction GetReplace(Call ldCall, Call stCall)
	{
		_count++;
		int[] array = ldCall[GNNELoad.Input].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ldCall[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(stCall.CheckedDataType, stCall.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		InitParameters(ldCall, stCall);
		TiledGlb glb = SearchGlbParameters();
		List<GnneAction> actions = BuildSchedule(glb, buffer, buffer2);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileLoadStore_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private void InitParameters(Call ld, Call st)
	{
		_lif = ld;
		_sof = st;
		_inputType = _lif.CheckedDataType;
		_outputType = _sof.CheckedDataType;
		_inputShape = new GNNEShape(_lif.CheckedShape[0].FixedValue, _lif.CheckedShape[1].FixedValue, _lif.CheckedShape[2].FixedValue, _lif.CheckedShape[3].FixedValue);
		_outputShape = new GNNEShape(_sof.CheckedShape[0].FixedValue, _sof.CheckedShape[1].FixedValue, _sof.CheckedShape[2].FixedValue, _sof.CheckedShape[3].FixedValue);
	}

	private TileLoadStoreGlb SearchGlbParameters()
	{
		int i = 1;
		int j = 1;
		int k = 1;
		int num = _outputShape[3];
		int w = _inputShape[3];
		int nPingPongSplit = GNNEEnv.NPingPongSplit;
		AllocateResult allocateResult = HandleAllocate(i, j, k, w, k, num, nPingPongSplit);
		if (!allocateResult.IsOk)
		{
			nPingPongSplit = 1;
		}
		for (; k < _inputShape[2] && k < 65535; k++)
		{
			int num2 = k + 1;
			allocateResult = HandleAllocate(i, j, num2, w, num2, num, nPingPongSplit);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; j < _inputShape[1] && j < 65535; j++)
		{
			allocateResult = HandleAllocate(i, j + 1, k, w, k, num, nPingPongSplit);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; i < _inputShape[0] && i < 65535; i++)
		{
			allocateResult = HandleAllocate(i + 1, j, k, w, k, num, nPingPongSplit);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		allocateResult = HandleAllocate(i, j, k, w, k, num, nPingPongSplit, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLoadStore.cs", 123);
		GNNEShape gNNEShape = new GNNEShape(i, j, k, num);
		return new TileLoadStoreGlb(allocateResult.GlbMap, allocateResult.Items, gNNEShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private AllocateResult HandleAllocate(int n, int c, int h, int w, int e, int f, int nPingPongSplit, bool isFinal = false)
	{
		List<BoxOnGlb> list = new List<BoxOnGlb>();
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		int num = (int)Math.Ceiling(1.0 * (double)c / (double)nPingPongSplit);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4] { n, num, h, w }, _inputType, 0);
		int value = tensorOnGlb.AllocatedBytes * nPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4] { n, num, h, w }, _inputType, 0);
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
		AllocateResult allocateResult = SpaceSearcher.TryAllocate(new BoxPacker(16)
		{
			Boxes = list
		});
		if (allocateResult.IsOk)
		{
			tensorOnGlb.Mmu = allocateResult.Items[ItemName.Ifmap];
			tensorOnGlb2.Mmu = allocateResult.Items[ItemName.Ifmap];
		}
		dictionary.Add(ItemName.Ifmap, tensorOnGlb);
		dictionary.Add(ItemName.Ofmap, tensorOnGlb2);
		return new AllocateResult
		{
			IsOk = allocateResult.IsOk,
			Items = allocateResult.Items,
			GlbMap = dictionary
		};
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf)
	{
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		gnneActionUpdater.UpdateMmuConf();
		int[] strides = ((TensorConst)_sof[GNNEStore.Strides]).Value.ToArray<int>();
		Func<int, int, int, Segment1D> func = delegate(int start, int end, int dim)
		{
			int num = (int)Math.Ceiling(1.0 * (double)start / (double)strides[dim]);
			return new Segment1D(new System.Range(end: (int)Math.Ceiling(1.0 * (double)end / (double)strides[dim]), start: num), new Padding(0, 0));
		};
		foreach (Segment1D item in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[0], _inputShape[0]))
		{
			Segment1D segment1D = func(item.Start, item.End, 0);
			foreach (Segment1D item2 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[1], _inputShape[1]))
			{
				Segment1D segment1D2 = func(item2.Start, item2.End, 1);
				foreach (Segment1D item3 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[2], _inputShape[2]))
				{
					Segment1D segment1D3 = func(item3.Start, item3.End, 2);
					foreach (Segment1D item4 in TileUtilities.GetSegmentStartEndLength(0, glb.LastOutShape[3], _inputShape[3]))
					{
						Segment1D segment1D4 = func(item4.Start, item4.End, 3);
						SegmentND segmentND = new SegmentND(segment1D, segment1D2, segment1D3, segment1D4);
						SegmentND segmentND2 = new SegmentND(segment1D, item2, item3, item4);
						int chunkSize = (int)Math.Ceiling(1.0 * (double)item2.Length / (double)glb.NPingPongSplit);
						List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(item2.Start, chunkSize, item2.End);
						for (int i = 0; i < segmentStartEndLength.Count; i++)
						{
							Segment1D segment1D5 = segmentStartEndLength[i];
							Segment1D segment1D6 = func(segment1D5.Start, segment1D5.End, 1);
							List<CcrSet> ccrsToSet = new List<CcrSet>
							{
								new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, i)), 1)
							};
							SegmentND tensor = new SegmentND(segmentND2[0], segment1D5, segmentND2[2], segmentND2[3]);
							gnneActionUpdater.UpdateLoadIf(tensor, _lif, i, ddrIf, 0, null, ItemName.Ifmap, ccrsToSet);
							List<CcrClr> ccrsToClr = new List<CcrClr>
							{
								new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, i)))
							};
							SegmentND ofmap = new SegmentND(segmentND[0], segment1D6, segmentND[2], segmentND[3]);
							SegmentND slice = new SegmentND(segmentND[0], segment1D6, segmentND[2], segmentND[3]);
							slice[0] = new Segment1D((slice[0].Start * strides[0])..slice[0].End, new Padding(0, 0));
							slice[1] = new Segment1D((slice[1].Start * strides[1])..slice[1].End, new Padding(0, 0));
							slice[2] = new Segment1D((slice[2].Start * strides[2])..slice[2].End, new Padding(0, 0));
							slice[3] = new Segment1D((slice[3].Start * strides[3])..slice[3].End, new Padding(0, 0));
							int sliceOffsetInTensor = TileUtilities.GetSliceOffsetInTensor(in tensor, in slice);
							gnneActionUpdater.UpdateStoreT(ofmap, _sof, i, ddrOf, sliceOffsetInTensor, null, null, ccrsToClr);
						}
					}
				}
			}
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLoadStore.cs", 249);
		return list;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call ldCall = (Call)__result["ldCall"];
		Call stCall = (Call)__result["stCall"];
		return GetReplace(ldCall, stCall);
	}
}
