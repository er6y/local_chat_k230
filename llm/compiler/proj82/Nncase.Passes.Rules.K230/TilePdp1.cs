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
public class TilePdp1 : RewriteRule<Pattern>
{
	private static int _count = -1;

	public override Pattern Pattern { get; } = FusionPattern.IsGNNEPDP1Fusion();


	private PrimFunction GetReplace(Call call, GNNEPdp1 callOp, Call ld, Call st)
	{
		_count++;
		TilePDP1Glb glb = SearchGlbParameters(ld, st, call);
		int[] array = ld.Arguments[0].CheckedShape.ToValueArray();
		T.CreateBuffer(new TensorType(ld[GNNELoad.Input].CheckedDataType, array), MemoryLocation.Input, out Nncase.TIR.Buffer buffer, "var ddrIf");
		T.CreateBuffer(new TensorType(st.CheckedDataType, st.CheckedShape), MemoryLocation.Output, out Nncase.TIR.Buffer buffer2, "var ddrOf");
		List<GnneAction> actions = ((!IsGlobalPdp(ld, call)) ? BuildSchedule(glb, call, ld, st, buffer, buffer2) : BuildScheduleGlobalPdp(glb, callOp, call, ld, st, buffer, buffer2));
		ActionToInstruct actionToInstruct = new ActionToInstruct();
		return T.PrimFunc($"TilePDP1_{_count}", K230RtModule.Kind, buffer, buffer2).Body(actionToInstruct.Instructions(actions), I.END(GP_REGISTER.x0, 0)).Build();
	}

	private List<GnneAction> BuildSchedule(TiledGlb glb, Call pdp, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf)
	{
		int[] subArray = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[..2];
		int[] subArray2 = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[2..4];
		int u = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int u2 = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int r = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int s = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		gnneActionUpdater.UpdateMmuConf();
		GNNEShape gNNEShape = new GNNEShape(ld.Arguments[0].CheckedShape.ToValueArray());
		int[] lastOutShape = glb.LastOutShape;
		Shape outShape = pdp.CheckedShape;
		IEnumerable<SegmentND> enumerable = from glbOutputBatch in TileUtilities.SegmentBy(0, lastOutShape[0], outShape[0].FixedValue)
			from glbOutputChannel in TileUtilities.SegmentBy(0, lastOutShape[1], outShape[1].FixedValue)
			from glbOutputRow in TileUtilities.SegmentBy(0, lastOutShape[2], outShape[2].FixedValue)
			from glbOutputColumn in TileUtilities.SegmentBy(0, lastOutShape[3], outShape[3].FixedValue)
			select new SegmentND(glbOutputBatch, glbOutputChannel, glbOutputRow, glbOutputColumn);
		int num = 0;
		foreach (SegmentND item in enumerable)
		{
			int start = item[2].Start;
			int length = item[2].Length;
			int h = gNNEShape[2];
			Padding p = new Padding(subArray[0], subArray[1]);
			Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(start, length, h, r, u, 1, in p);
			int start2 = item[3].Start;
			int length2 = item[3].Length;
			int w = gNNEShape[3];
			p = new Padding(subArray2[0], subArray2[1]);
			Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(start2, length2, w, s, u2, 1, in p);
			SegmentND segmentND = new SegmentND(item[0], item[1], inputRowSegment, inputColumnSegment);
			List<CcrSet> ccrsToSet = new List<CcrSet>
			{
				new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num)), 1)
			};
			SegmentND ifmap = segmentND;
			List<int> stridesD = new int[3]
			{
				glb.GlbMap[ItemName.Ifmap].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap].Dimensions[3]
			}.ToList();
			gnneActionUpdater.UpdateLoadIf(ifmap, ld, num, ddrIf, 0, stridesD, ItemName.Ifmap, ccrsToSet);
			List<CcrSet> ccrsToSet2 = new List<CcrSet>
			{
				new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)), 1)
			};
			List<CcrClr> ccrsToClr = new List<CcrClr>
			{
				new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num)))
			};
			SegmentND ofmap = item;
			gnneActionUpdater.UpdateMfuPdp1(pdp, ifmap, ofmap, num, ccrsToSet2, ccrsToClr);
			List<CcrClr> ccrsToClr2 = new List<CcrClr>
			{
				new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num)))
			};
			gnneActionUpdater.UpdateStoreT(ofmap, st, num, ddrOf, 0, null, null, ccrsToClr2);
			num = (num + 1) % 2;
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePdp1.cs", 104);
		return list;
	}

	private List<GnneAction> BuildScheduleGlobalPdp(TiledGlb glb, GNNEPdp1 globalPdp, Call pdp, Call ld, Call st, Nncase.TIR.Buffer ddrIf, Nncase.TIR.Buffer ddrOf)
	{
		int[] subArray = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[..2];
		int[] subArray2 = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[2..4];
		int u = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int u2 = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int num = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		List<GnneAction> list = new List<GnneAction>();
		GprHandler gpr = new GprHandler(GNNEEnv.GprNum);
		SsrHandler ssr = new SsrHandler(GNNEEnv.SsrNum);
		CcrHandler ccrHandler = new CcrHandler();
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(list, glb, ccrHandler, gpr, ssr);
		gnneActionUpdater.UpdateMmuConf();
		int s = 0;
		int r = 0;
		SplitGlobalPdp1(num2, num, ref r, ref s);
		GNNEShape gNNEShape = new GNNEShape(ld.Arguments[0].CheckedShape.ToValueArray());
		int[] lastOutShape = glb.LastOutShape;
		Shape outShape = pdp.CheckedShape;
		SegmentND[] array = (from glbOutputBatch in TileUtilities.SegmentBy(0, lastOutShape[0], outShape[0].FixedValue)
			from glbOutputChannel in TileUtilities.SegmentBy(0, lastOutShape[1], outShape[1].FixedValue)
			from glbOutputRow in TileUtilities.SegmentBy(0, lastOutShape[2], outShape[2].FixedValue)
			from glbOutputColumn in TileUtilities.SegmentBy(0, lastOutShape[3], outShape[3].FixedValue)
			select new SegmentND(glbOutputBatch, glbOutputChannel, glbOutputRow, glbOutputColumn)).ToArray();
		List<SegmentND> list2 = new List<SegmentND>();
		SegmentND[] array2 = array;
		foreach (SegmentND segmentND in array2)
		{
			int start = segmentND[2].Start;
			int length = segmentND[2].Length;
			int h = gNNEShape[2];
			Padding p = new Padding(subArray[0], subArray[1]);
			Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(start, length, h, num, u, 1, in p);
			int start2 = segmentND[3].Start;
			int length2 = segmentND[3].Length;
			int w = gNNEShape[3];
			p = new Padding(subArray2[0], subArray2[1]);
			Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(start2, length2, w, num2, u2, 1, in p);
			SegmentND item = new SegmentND(segmentND[0], segmentND[1], inputRowSegment, inputColumnSegment);
			list2.Add(item);
		}
		int num3 = 0;
		bool[] array3 = new bool[2] { true, true };
		bool flag = false;
		bool flag2 = false;
		for (int j = 0; j < array.Length; j++)
		{
			SegmentND segmentND2 = array.ToList()[j];
			SegmentND segmentND3 = list2[j];
			if (array.Length == 1)
			{
				flag = true;
			}
			else
			{
				if ((j == array.Length - 1 || j == array.Length - 2) && (j & 1) == 0)
				{
					flag = true;
				}
				if ((j == array.Length - 1 || j == array.Length - 2) && (j & 1) == 1)
				{
					flag2 = true;
				}
			}
			if (j == array.Length - 2)
			{
				if (list2[list2.Count - 1][1].Length == 1)
				{
					flag2 = true;
				}
			}
			List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, r, gNNEShape[2]);
			List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, s, gNNEShape[3]);
			List<CcrSet> list3 = new List<CcrSet>();
			list3.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num3)), (segmentStartEndLength.Count * segmentStartEndLength2.Count == 1) ? 1 : 2));
			SegmentND segmentND4 = new SegmentND(segmentND3);
			int alignedNum = TileUtilities.GetAlignedNum(segmentND3[3].Length, (TileUtilities.GetBytesPerElement(ld.CheckedDataType) == 1) ? 32 : 16);
			SegmentND tensor = new SegmentND(segmentND3[0], segmentND3[1], segmentND3[2], new Segment1D(..alignedNum, new Padding(0, 0)));
			List<int> stridesD = new int[3]
			{
				glb.GlbMap[ItemName.Ifmap].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap].Dimensions[3]
			}.ToList();
			gnneActionUpdater.UpdateLoadIf(segmentND4, ld, num3, ddrIf, 0, stridesD, ItemName.Ifmap, list3);
			alignedNum = TileUtilities.GetAlignedNum(segmentStartEndLength2.Count, 16);
			SegmentND segmentND5 = new SegmentND(segmentND2[0], segmentND2[1], new Segment1D(..segmentStartEndLength.Count, new Padding(0, 0)), new Segment1D(..segmentStartEndLength2.Count, new Padding(0, 0)));
			SegmentND tensor2 = new SegmentND(segmentND2[0], segmentND2[1], segmentND5[2], new Segment1D(..alignedNum, new Padding(0, 0)));
			PDP_FUNCTION pdpOp = PdpFunc((globalPdp.ReduceOp == MFU_PDP_OP.AVERAGE) ? MFU_PDP_OP.SUM : globalPdp.ReduceOp);
			Half sumScale = (Half)(1.0 / (double)gNNEShape[2]);
			Half sumScale2 = (Half)(1.0 / (double)gNNEShape[3]);
			for (int k = 0; k < segmentStartEndLength.Count; k++)
			{
				for (int l = 0; l < segmentStartEndLength2.Count; l++)
				{
					int num4 = ((k == 0 && l == 0) ? 1 : 0);
					List<CcrClr> list4 = new List<CcrClr>();
					if (num4 > 0)
					{
						list4.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num3))));
					}
					if (k == 0 && l == 0 && !array3[num3])
					{
						list4.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.OfmapFake, num3))));
					}
					SegmentND slice = new SegmentND(segmentND4[0], segmentND4[1], segmentStartEndLength[k], segmentStartEndLength2[l]);
					SegmentND slice2 = new SegmentND(segmentND5[0], segmentND5[1], new Segment1D(k..(k + 1), new Padding(0, 0)), new Segment1D(l..(l + 1), new Padding(0, 0)));
					int offsetS = TileUtilities.GetSliceOffsetInTensor(in tensor, in slice) * TileUtilities.GetBytesPerElement(ld.CheckedDataType);
					int offsetD = TileUtilities.GetSliceOffsetInTensor(in tensor2, in slice2) * TileUtilities.GetBytesPerElement(DataTypes.Float16);
					gnneActionUpdater.UpdateMfuGlobalPdp1(pdp, ld.CheckedDataType, DataTypes.Float16, pdpOp, slice, slice2, num3, sumScale, null, list4, offsetS, offsetD);
				}
			}
			SegmentND ifmap = segmentND5;
			SegmentND ofmap = new SegmentND(segmentND5[0], segmentND5[1], new Segment1D(..1, new Padding(0, 0)), new Segment1D(..1, new Padding(0, 0)));
			List<CcrSet> list5 = new List<CcrSet>();
			list5.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num3)), 1));
			List<CcrClr> list6 = new List<CcrClr>();
			list6.Add(new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ifmap, num3))));
			int offsetS2 = 0;
			int offsetD2 = 0;
			gnneActionUpdater.UpdateMfuGlobalPdp1(pdp, DataTypes.Float16, pdp.CheckedDataType, pdpOp, ifmap, ofmap, num3, sumScale2, list5, list6, offsetS2, offsetD2, ItemName.Ofmap);
			List<CcrClr> ccrsToClr = new List<CcrClr>
			{
				new CcrClr(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.Ofmap, num3)))
			};
			bool num5 = ((num3 != 0) ? (!flag2) : (!flag));
			List<CcrSet> list7 = new List<CcrSet>();
			if ((num5 ? 1 : 0) > (false ? 1 : 0))
			{
				list7.Add(new CcrSet(ccrHandler.GetCcrItem(ccrHandler.GetName(ItemName.OfmapFake, num3)), 1));
			}
			List<int> stridesS = new int[3]
			{
				glb.GlbMap[ItemName.Ofmap].Dimensions[1],
				glb.GlbMap[ItemName.Ofmap].Dimensions[2],
				glb.GlbMap[ItemName.Ofmap].Dimensions[3] * ((pdp.CheckedDataType == DataTypes.Float16 || pdp.CheckedDataType == DataTypes.Int16) ? 1 : 2)
			}.ToList();
			gnneActionUpdater.UpdateStoreT(ofmap, st, num3, ddrOf, 0, null, list7, ccrsToClr, ItemName.Ofmap, stridesS);
			array3[num3] = false;
			num3 = (num3 + 1) % 2;
		}
		TileUtilities.Assert(ccrHandler.CcrSanityCheck(), "ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePdp1.cs", 270);
		return list;
		static PDP_FUNCTION PdpFunc(MFU_PDP_OP op)
		{
			if (op <= MFU_PDP_OP.SUM)
			{
				switch (op)
				{
				case MFU_PDP_OP.MIN:
					return PDP_FUNCTION.min;
				case MFU_PDP_OP.MAX:
					return PDP_FUNCTION.max;
				case MFU_PDP_OP.AVERAGE:
					return PDP_FUNCTION.average;
				case MFU_PDP_OP.SUM:
					return PDP_FUNCTION.sum;
				}
			}
			return PDP_FUNCTION.min;
		}
	}

	private TilePDP1Glb SearchGlbParameters(Call ld, Call st, Call pdp)
	{
		int[] subArray = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[..2];
		int[] subArray2 = ((TensorConst)pdp[GNNEPdp1.Padding]).Value.ToArray<int>()[2..4];
		int uh = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int uh2 = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int num = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		GNNEShape gNNEShape = new GNNEShape(ld.Arguments[0].CheckedShape.ToValueArray());
		GNNEShape gNNEShape2 = new GNNEShape(pdp.CheckedShape.ToValueArray());
		bool flag = IsGlobalPdp(ld, pdp);
		int i = 1;
		int j = 1;
		int num3 = 1;
		int num4 = gNNEShape2[3];
		int e = num3;
		int inH = gNNEShape[2];
		int outH = gNNEShape2[2];
		Padding p = new Padding(subArray[0], subArray[1]);
		int h = SpaceSearcher.GetInputHeight(e, inH, num, outH, uh, 1, in p);
		int e2 = num4;
		int inH2 = gNNEShape[3];
		int outH2 = gNNEShape2[3];
		p = new Padding(subArray2[0], subArray2[1]);
		int inputHeight = SpaceSearcher.GetInputHeight(e2, inH2, num2, outH2, uh2, 1, in p);
		DataType checkedDataType = ld.CheckedDataType;
		DataType outType = (flag ? DataTypes.Float16 : pdp.CheckedDataType);
		if (flag)
		{
			int s = 0;
			int r = 0;
			SplitGlobalPdp1(num2, num, ref r, ref s);
			num3 = (int)Math.Ceiling(1.0 * (double)num / (double)r);
			num4 = (int)Math.Ceiling(1.0 * (double)num2 / (double)s);
			TileUtilities.Assert(num4 <= 64 && num3 <= 16 && num3 * num4 <= 256, "ow <= 64 && oh <= 16 && oh * ow <= 256", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePdp1.cs", 308);
		}
		AllocateResult allocateResult = new AllocateResult();
		while (num3 < gNNEShape2[2])
		{
			int num5 = num3 + 1;
			int inH3 = gNNEShape[2];
			int outH3 = gNNEShape2[2];
			p = new Padding(subArray[0], subArray[1]);
			int inputHeight2 = SpaceSearcher.GetInputHeight(num5, inH3, num, outH3, uh, 1, in p);
			allocateResult = HandleAllocate(i, j, inputHeight2, inputHeight, num3, num4, checkedDataType, outType);
			if (!allocateResult.IsOk)
			{
				break;
			}
			num3 = num5;
			h = inputHeight2;
		}
		for (; j < gNNEShape[1]; j++)
		{
			allocateResult = HandleAllocate(i, j + 1, h, inputHeight, num3, num4, checkedDataType, outType);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		for (; i < gNNEShape[0]; i++)
		{
			allocateResult = HandleAllocate(i + 1, j, h, inputHeight, num3, num4, checkedDataType, outType);
			if (!allocateResult.IsOk)
			{
				break;
			}
		}
		allocateResult = HandleAllocate(i, j, h, inputHeight, num3, num4, checkedDataType, outType, isFinal: true);
		TileUtilities.Assert(allocateResult.IsOk, "allocation.IsOk", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TilePdp1.cs", 358);
		if (flag)
		{
			num3 = 1;
			num4 = 1;
		}
		GNNEShape gNNEShape3 = new GNNEShape(i, j, num3, num4);
		return new TilePDP1Glb(allocateResult.GlbMap, allocateResult.Items, gNNEShape3.Dims, GNNEEnv.NPingPongSplit, allocateResult.GlbMap[ItemName.Ifmap], allocateResult.GlbMap[ItemName.Ofmap]);
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

	private void SplitGlobalPdp1(int kernelW, int kernelH, ref int r, ref int s)
	{
		r = ((kernelH > 16) ? 16 : kernelH);
		s = Math.Min(Math.Min(256 / r, kernelW), 64);
	}

	private bool IsGlobalPdp(Call ld, Call pdp)
	{
		int num = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		GNNEShape gNNEShape = new GNNEShape(ld.Arguments[0].CheckedShape.ToValueArray());
		if (gNNEShape[2] == num && gNNEShape[3] == num2)
		{
			if (gNNEShape[2] <= 16 && gNNEShape[3] <= 64)
			{
				return gNNEShape[2] * gNNEShape[3] > 256;
			}
			return true;
		}
		return false;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		GNNEPdp1 callOp = (GNNEPdp1)__result["callOp"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		return GetReplace(call, callOp, ld, st);
	}
}
