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
public class TileTranspose : RewriteRule<Pattern>
{
	private static int _count = -1;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNETransposeFusion();


	private PrimFunction GetReplace(Call call, GNNETranspose callOp, Call ld, Call st)
	{
		_count++;
		MFU_TRANS_PERMUTE perm = callOp.Perm;
		TileTransposeGlb glb = SearchGlbParameters(ld, st, perm);
		int[] array = ld.Arguments[0].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Buffer buffer2, "var ddrOf");
		List<GnneAction> actions = BuildSchedule(glb, callOp, ld, st, buffer, buffer2);
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TileTranspose_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, GNNETranspose transpose, Call ld, Call st, Buffer ddrIf, Buffer ddrOf)
	{
		MFU_TRANS_PERMUTE perm = transpose.Perm;
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		gnneActionUpdater.UpdateMmuConf();
		int[] lastOutShape = glb.LastOutShape;
		Shape inputShape = ld.CheckedShape;
		IEnumerable<SegmentND> enumerable = from glbInputBatch in TileUtilities.SegmentBy(0, lastOutShape[0], inputShape[0].FixedValue)
			from glbInputChannel in TileUtilities.SegmentBy(0, lastOutShape[1], inputShape[1].FixedValue)
			from glbInputRow in TileUtilities.SegmentBy(0, lastOutShape[2], inputShape[2].FixedValue)
			from glbInputColumn in TileUtilities.SegmentBy(0, lastOutShape[3], inputShape[3].FixedValue)
			select new SegmentND(glbInputBatch, glbInputChannel, glbInputRow, glbInputColumn);
		int num = 0;
		foreach (SegmentND item in enumerable)
		{
			SegmentND segmentND = new SegmentND(GNNETypePatternUtility.ApplyPerm1(perm, item.ToArray()));
			List<CcrSet> list2 = new List<CcrSet>();
			list2.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num)), 1));
			SegmentND ifmap = item;
			List<int> stridesD = new int[3]
			{
				glb.GlbMap[ItemName.Ifmap].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap].Dimensions[3]
			}.ToList();
			gnneActionUpdater.UpdateLoadIf(ifmap, ld, num, ddrIf, 0, stridesD, ItemName.Ifmap, list2);
			List<CcrSet> ccrsToSet = new List<CcrSet>
			{
				new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)), 1)
			};
			List<CcrClr> ccrsToClr = new List<CcrClr>
			{
				new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num)))
			};
			SegmentND ofmap = segmentND;
			gnneActionUpdater.UpdateMfuTranspose(ifmap, ofmap, ld.CheckedDataType, perm, num, ccrsToSet, ccrsToClr);
			List<CcrClr> ccrsToClr2 = new List<CcrClr>
			{
				new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)))
			};
			gnneActionUpdater.UpdateStoreT(ofmap, st, num, ddrOf, 0, null, null, ccrsToClr2);
			num = (num + 1) % 2;
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileTranspose.cs", 86);
		return list;
	}

	private TileTransposeGlb SearchGlbParameters(Call ld, Call st, MFU_TRANS_PERMUTE perm)
	{
		Call ld2 = ld;
		Call st2 = st;
		GNNEShape shapeB = new GNNEShape(ld2.Arguments[0].CheckedShape.ToValueArray());
		GNNEShape lastOutputShape = new GNNEShape(1, 1, 1, 1);
		for (int num = 3; num >= 0; num--)
		{
			int i1 = num;
			lastOutputShape[num] = TileUtilities.SearchAxis(lastOutputShape, shapeB, num, (int newV) => HandleAllocate(ld2, st2, perm, lastOutputShape.WithIndex(i1, newV)));
		}
		AllocateResult allocateResult = HandleAllocate(ld2, st2, perm, lastOutputShape, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileTranspose.cs", 105);
		return new TileTransposeGlb(allocateResult.GlbMap, allocateResult.Items, lastOutputShape.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
	}

	private AllocateResult HandleAllocate(Call ld, Call st, MFU_TRANS_PERMUTE perm, GNNEShape shape, bool isFinal = false)
	{
		BoxPacker boxPacker = new BoxPacker(16);
		Dictionary<ItemName, TensorOnGlb> dictionary = new Dictionary<ItemName, TensorOnGlb>();
		GNNEShape gNNEShape = new GNNEShape(GNNETypePatternUtility.ApplyPerm1(perm, shape.Dims));
		int alignmentFactor = ((TileUtilities.GetBytesPerElement(ld.CheckedDataType) == 1) ? 32 : 16);
		int alignedNum = TileUtilities.GetAlignedNum(shape[3], alignmentFactor);
		TensorOnGlb tensorOnGlb = new TensorOnGlb(new int[4]
		{
			shape[0],
			shape[1],
			shape[2],
			alignedNum
		}, ld.CheckedDataType, 0);
		int value = tensorOnGlb.GlbNByte * GNNEEnv.NPingPongSplit;
		value = TileUtilities.GetAlignedNum(value, GNNEEnv.IfmapBankWidth * GNNEEnv.GlbBankWidth);
		int alignedNum2 = TileUtilities.GetAlignedNum(gNNEShape[3], alignmentFactor);
		TensorOnGlb tensorOnGlb2 = new TensorOnGlb(new int[4]
		{
			gNNEShape[0],
			gNNEShape[1],
			gNNEShape[2],
			alignedNum2
		}, st.CheckedDataType, 0);
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

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		GNNETranspose callOp = (GNNETranspose)__result["callOp"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, callOp, ld, st);
	}
}
