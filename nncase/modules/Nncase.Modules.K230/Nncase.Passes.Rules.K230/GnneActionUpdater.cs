using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.Tensors;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionUpdater
{
	private readonly TiledGlb _glb;

	private readonly CcrHandler _ccrHandler;

	private readonly GprHandler _gprHandler;

	private readonly SsrHandler _ssRegHandler;

	private readonly ConfActions _latestConfActions = new ConfActions();

	public List<GnneAction> Actions { get; }

	public GnneActionUpdater(List<GnneAction> actions, TiledGlb glb, CcrHandler ccrHandler, GprHandler gpr, SsrHandler ssr)
	{
		Actions = actions;
		_glb = glb;
		_ccrHandler = ccrHandler;
		_gprHandler = gpr;
		_ssRegHandler = ssr;
	}

	public long PackStride(int n, int c, int h)
	{
		return n * 4294967296L + (long)c * 65536L + h;
	}

	public long PackShape(int n, int c, int h, int w)
	{
		return n * 281474976710656L + c * 4294967296L + (long)h * 65536L + w;
	}

	public ConfActions LatestConfActions()
	{
		return _latestConfActions;
	}

	public void UpdateEnd()
	{
		Actions.Add(new GnneActionEnd());
	}

	public void UpdateFence()
	{
		Actions.Add(new GnneActionFence());
	}

	public void UpdateIntr(int intrNum)
	{
		Gpr intrNum2 = _gprHandler.SetGprItem(intrNum);
		Actions.Add(new GnneActionIntr(intrNum2));
	}

	public void UpdateCcr(List<CcrSet> ccrsToSet, List<CcrClr> ccrsToClr)
	{
		if (ccrsToSet == null)
		{
			ccrsToSet = new List<CcrSet>();
		}
		if (ccrsToClr == null)
		{
			ccrsToClr = new List<CcrClr>();
		}
		int num = ccrsToSet.Count + ccrsToClr.Count;
		if (num <= 0)
		{
			return;
		}
		Gpr num2 = _gprHandler.SetGprItem(num);
		Actions.Add(new GnneActionCcrDecl(num2));
		foreach (CcrClr item in ccrsToClr)
		{
			Actions.Add(new GnneActionCcrClr(item.Ccr));
		}
		foreach (CcrSet item2 in ccrsToSet)
		{
			TileUtilities.Assert(item2.Value > 0, "cs.Value > 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/ActionUpdater.cs", 85);
			Actions.Add(new GnneActionCcrSet(item2.Ccr, item2.Value));
		}
		_ccrHandler.SetItems(ccrsToSet);
		_ccrHandler.ClearItems(ccrsToClr);
	}

	public void UpdateMmuConf()
	{
		foreach (KeyValuePair<ItemName, MmuItem> item in _glb.Items)
		{
			Gpr start = _gprHandler.SetGprItem(item.Value.StartDepth);
			Gpr depth = _gprHandler.SetGprItem(item.Value.Depth);
			using (IEnumerator<KeyValuePair<ItemName, MmuItem>> enumerator2 = (from _003C_003Eh__TransparentIdentifier0 in _glb.Items.Where<KeyValuePair<ItemName, MmuItem>>(delegate(KeyValuePair<ItemName, MmuItem> itemOther)
				{
					KeyValuePair<ItemName, MmuItem> keyValuePair2 = itemOther;
					return keyValuePair2.Key != item.Key;
				}).Select(delegate(KeyValuePair<ItemName, MmuItem> itemOther)
				{
					int startDepth = item.Value.StartDepth;
					int x1R2 = item.Value.StartDepth + item.Value.Depth;
					KeyValuePair<ItemName, MmuItem> keyValuePair = itemOther;
					int startDepth2 = keyValuePair.Value.StartDepth;
					keyValuePair = itemOther;
					int startDepth3 = keyValuePair.Value.StartDepth;
					keyValuePair = itemOther;
					return new
					{
						itemOther = itemOther,
						lap = Overlap(startDepth, x1R2, startDepth2, startDepth3 + keyValuePair.Value.Depth)
					};
				})
				where _003C_003Eh__TransparentIdentifier0.lap > 0
				select _003C_003Eh__TransparentIdentifier0.itemOther).GetEnumerator())
			{
				if (enumerator2.MoveNext())
				{
					KeyValuePair<ItemName, MmuItem> current = enumerator2.Current;
					Console.WriteLine("[tiling_check]: mmu overlap happen!");
					Console.WriteLine($"mmu_a: {item.Key}, mmu_b: {current.Key}");
					Console.WriteLine($"start_a: {item.Value.StartDepth}, depth_a: {item.Value.Depth}; start_b: {current.Value.StartDepth}, depth_b: {current.Value.Depth}");
					throw new NotSupportedException("mmu config conflicts!");
				}
			}
			Actions.Add(new Gnne_action_mmu_conf(item.Value, start, depth));
		}
		static int Overlap(int x1L, int x1R, int x2L, int x2R)
		{
			int num = ((x1L > x2L) ? x1L : x2L);
			return ((x1R < x2R) ? x1R : x2R) - num;
		}
	}

	public void UpdateLoadWQarg(Call lwQarg, WeightGroupHandler weightGroup, Nncase.TIR.Buffer ddrCon, int offsetD = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, ItemName name = ItemName.WQarg, int offsetS = 0)
	{
		DataType checkedDataType = lwQarg[GNNELoadW.Input].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		int num = 0;
		foreach (Segment1D item in weightGroup.QargGroupSlice())
		{
			num++;
			int num2 = item.Start * bytesPerElement + offsetS;
			int length = item.Length;
			int addr = weightGroup.QargAlignedOffset(item) * bytesPerElement + offsetD;
			Gpr lenCompressed = new Gpr(-1, length, needRenewal: false);
			Gpr lenDecompressed = new Gpr(-1, length, needRenewal: false);
			GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, checkedDataType, checkedDataType);
			if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
			{
				Gpr lenCompressed2 = _gprHandler.SetGprItem(length);
				lenDecompressed = _gprHandler.SetGprItem(length);
				gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, checkedDataType, checkedDataType);
				_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
				Actions.Add(gnneActionL2LoadWConf);
			}
			Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num2);
			Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, addr));
			Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
			Gpr basement = _gprHandler.SetGprItem1(gprItem);
			int value = System.Math.Min(GNNEEnv.WAlignNum - 1, length - 1);
			Gpr validCNum = _gprHandler.SetGprItem(value);
			if (num == weightGroup.QargGroupSlice().Count && GNNEEnv.UseCcr)
			{
				UpdateCcr(ccrsToSet, ccrsToClr);
			}
			Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		}
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateLoadWQargMatmul(Call call, WeightGroupHandler weightGroup, Nncase.TIR.Buffer ddrCon, int n, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, ItemName name = ItemName.WQarg)
	{
		DataType checkedDataType = call[GNNELoadW.Input].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		int num = 0;
		foreach (Segment1D item in weightGroup.QargGroupSlice())
		{
			num++;
			int num2 = item.Start * bytesPerElement + n * weightGroup.CurrentQargOffset();
			int length = item.Length;
			int addr = weightGroup.QargAlignedOffset(item) * bytesPerElement + n * weightGroup.CurrentQargAlignedOffset();
			Gpr lenCompressed = new Gpr(-1, length, needRenewal: false);
			Gpr lenDecompressed = new Gpr(-1, length, needRenewal: false);
			GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, checkedDataType, checkedDataType);
			if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
			{
				Gpr lenCompressed2 = _gprHandler.SetGprItem(length);
				lenDecompressed = _gprHandler.SetGprItem(length);
				gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, checkedDataType, checkedDataType);
				_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
				Actions.Add(gnneActionL2LoadWConf);
			}
			Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num2);
			Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, addr));
			Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
			Gpr basement = _gprHandler.SetGprItem1(gprItem);
			int value = System.Math.Min(GNNEEnv.WAlignNum - 1, length - 1);
			Gpr validCNum = _gprHandler.SetGprItem(value);
			if (num == weightGroup.QargGroupSlice().Count && GNNEEnv.UseCcr)
			{
				UpdateCcr(ccrsToSet, ccrsToClr);
			}
			Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		}
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateLoadDwQarg(Call call, WeightGroupHandler weightGroup, Nncase.TIR.Buffer ddrCon, int offsetD = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null)
	{
		DataType checkedDataType = call[GNNEPdp0DW.WeightsBias].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		int num = 0;
		foreach (Segment1D item in weightGroup.DwQargGroupSlice())
		{
			num++;
			int num2 = item.Start * bytesPerElement;
			int length = item.Length;
			int addr = weightGroup.DwQargAlignedOffset(item) * bytesPerElement + offsetD;
			Gpr lenCompressed = new Gpr(-1, length, needRenewal: false);
			Gpr lenDecompressed = new Gpr(-1, length, needRenewal: false);
			GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, checkedDataType, checkedDataType);
			if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
			{
				Gpr lenCompressed2 = _gprHandler.SetGprItem(length);
				lenDecompressed = _gprHandler.SetGprItem(length);
				gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, checkedDataType, checkedDataType);
				_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
				Actions.Add(gnneActionL2LoadWConf);
			}
			Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num2);
			Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.DwQarg].Mmu.Id, addr));
			Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
			Gpr basement = _gprHandler.SetGprItem1(gprItem);
			int value = System.Math.Min(GNNEEnv.WAlignNum - 1, length - 1);
			Gpr validCNum = _gprHandler.SetGprItem(value);
			if (num == weightGroup.DwQargGroupSlice().Count && GNNEEnv.UseCcr)
			{
				UpdateCcr(ccrsToSet, ccrsToClr);
			}
			Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		}
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdatePreloadW(SegmentND weight, Call call, WeightGroupHandler weightGroup, int wPp, Nncase.TIR.Buffer ddrCon, int offsetD = 0, ItemName weightItem = ItemName.WeightPreload, bool h2c = false, int pos = 0)
	{
		DataType checkedDataType = call[GNNELoadW.Input].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		foreach (SegmentND item in weightGroup.WeightGroupSlice())
		{
			if (item[0].Start >= weight[0].Start && item[1].Start >= weight[1].Start && item[2].Start >= weight[2].Start && item[3].Start >= weight[3].Start && item[0].End <= weight[0].End && item[1].End <= weight[1].End && item[2].End <= weight[2].End && item[3].End <= weight[3].End)
			{
				int num = weightGroup.WeightGroupOffset(item) * bytesPerElement + pos * weightGroup.GetShapeSize(item);
				int shape_size = item.Shape_size;
				int num2 = weightGroup.WeightGroupAlignedOffset(item) * bytesPerElement + pos * weightGroup.GetAlignedShapeSize(item);
				num2 += wPp * _glb.GlbMap[weightItem].AllocatedBytes + offsetD;
				Gpr lenCompressed = new Gpr(-1, shape_size, needRenewal: false);
				Gpr lenDecompressed = new Gpr(-1, shape_size, needRenewal: false);
				GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType);
				if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
				{
					Gpr lenCompressed2 = _gprHandler.SetGprItem(shape_size);
					lenDecompressed = _gprHandler.SetGprItem(shape_size);
					gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType);
					_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
					Actions.Add(gnneActionL2LoadWConf);
				}
				Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num);
				Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[weightItem].Mmu.Id, num2));
				Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
				Gpr basement = _gprHandler.SetGprItem1(gprItem);
				int value = item[1].Length - 1;
				if (h2c)
				{
					value = item[1].Length * item[2].Length - 1;
				}
				Gpr validCNum = _gprHandler.SetGprItem(value);
				Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
			}
		}
		Actions.Add(new GnneActionFence());
	}

	public void UpdateLoadW(SegmentND wg, Call call, WeightGroupHandler weightGroup, int wPp, Nncase.TIR.Buffer ddrCon, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetD = 0, bool h2c = false, int offsetS = 0, ItemName name = ItemName.Weight, int pos = 0)
	{
		DataType checkedDataType = call[GNNELoadW.Input].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		int num = weightGroup.WeightGroupOffset(wg) * bytesPerElement + offsetS + pos * weightGroup.GetShapeSize(wg);
		int shape_size = wg.Shape_size;
		int num2 = weightGroup.WeightGroupAlignedOffset(wg) * bytesPerElement + pos * weightGroup.GetAlignedShapeSize(wg);
		num2 += wPp * _glb.GlbMap[name].AllocatedBytes + offsetD;
		Gpr lenCompressed = new Gpr(-1, shape_size, needRenewal: false);
		Gpr lenDecompressed = new Gpr(-1, shape_size, needRenewal: false);
		GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType);
		if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
		{
			Gpr lenCompressed2 = _gprHandler.SetGprItem(shape_size);
			lenDecompressed = _gprHandler.SetGprItem(shape_size);
			gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType, (checkedDataType == DataTypes.Int16) ? DataTypes.UInt8 : checkedDataType);
			_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
			Actions.Add(gnneActionL2LoadWConf);
		}
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		int value = wg[1].Length - 1;
		if (h2c)
		{
			value = wg[1].Length * wg[2].Length - 1;
		}
		Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num);
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, num2));
		Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
		Gpr basement = _gprHandler.SetGprItem1(gprItem);
		Gpr validCNum = _gprHandler.SetGprItem(value);
		Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateLoadDw(Call dw, WeightGroupHandler weightGroup, Nncase.TIR.Buffer ddrCon, int offsetD = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null)
	{
		DataType checkedDataType = ((Call)dw[GNNEPdp0DW.Weights])[GNNELoadW.Input].CheckedDataType;
		int bytesPerElement = TileUtilities.GetBytesPerElement(checkedDataType);
		Dimension dimension = dw[GNNEPdp0DW.Weights].CheckedShape[2];
		Dimension dimension2 = dw[GNNEPdp0DW.Weights].CheckedShape[3];
		foreach (SegmentND item in weightGroup.WeightGroupSlice())
		{
			int start = item[0].Start;
			int end = item[0].End;
			Segment1D[] obj = new Segment1D[4]
			{
				..1,
				start..end,
				null,
				null
			};
			Index? index = dimension.Value;
			obj[2] = (index.HasValue ? ((Segment1D)(..index.GetValueOrDefault())) : null);
			index = dimension2.Value;
			obj[3] = (index.HasValue ? ((Segment1D)(..index.GetValueOrDefault())) : null);
			SegmentND tensor4d = new SegmentND(obj);
			weightGroup.UpdateDwWeightGroup(tensor4d);
		}
		int num = weightGroup.DwGroupSlice().Count;
		foreach (SegmentND item2 in weightGroup.DwGroupSlice())
		{
			int num2 = weightGroup.DwGroupOffset(item2) * bytesPerElement;
			int addr = weightGroup.DwGroupAlignedOffset(item2) * bytesPerElement + offsetD;
			num--;
			int start2 = item2[1].Start;
			int alignedNum = TileUtilities.GetAlignedNum(item2[1].End, GNNEEnv.PuWidth);
			Segment1D[] obj2 = new Segment1D[4]
			{
				..1,
				start2..alignedNum,
				null,
				null
			};
			Index? index = dimension.Value;
			obj2[2] = (index.HasValue ? ((Segment1D)(..index.GetValueOrDefault())) : null);
			index = dimension2.Value;
			obj2[3] = (index.HasValue ? ((Segment1D)(..index.GetValueOrDefault())) : null);
			SegmentND segmentND = new SegmentND(obj2);
			int shape_size = segmentND.Shape_size;
			Gpr lenCompressed = new Gpr(-1, shape_size, needRenewal: false);
			Gpr lenDecompressed = new Gpr(-1, shape_size, needRenewal: false);
			GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, checkedDataType, checkedDataType);
			if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
			{
				Gpr lenCompressed2 = _gprHandler.SetGprItem(shape_size);
				lenDecompressed = _gprHandler.SetGprItem(shape_size);
				gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, checkedDataType, checkedDataType);
				_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
				Actions.Add(gnneActionL2LoadWConf);
			}
			Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + (ulong)num2);
			Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.DwWeight].Mmu.Id, addr));
			Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
			Gpr basement = _gprHandler.SetGprItem1(gprItem);
			int value = System.Math.Min(segmentND[1].Length - 1, GNNEEnv.PuHeight - 1);
			Gpr validCNum = _gprHandler.SetGprItem(value);
			if (GNNEEnv.UseCcr && num == 0)
			{
				UpdateCcr(ccrsToSet, ccrsToClr);
			}
			Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		}
	}

	public void UpdateLoadAct(Call ld, Nncase.TIR.Buffer ddrCon, ItemName name = ItemName.Act, int offsetD = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null)
	{
		DataType checkedDataType = ld[GNNELoadW.Input].CheckedDataType;
		int size = ld[GNNELoadW.Input].CheckedShape.Size;
		Gpr lenCompressed = new Gpr(-1, size, needRenewal: false);
		Gpr lenDecompressed = new Gpr(-1, size, needRenewal: false);
		GnneActionL2LoadWConf gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed, lenDecompressed, checkedDataType, checkedDataType);
		if ((object)_latestConfActions.L2LoadWConf == null || _latestConfActions.L2LoadWConf != gnneActionL2LoadWConf)
		{
			Gpr lenCompressed2 = _gprHandler.SetGprItem(size);
			lenDecompressed = _gprHandler.SetGprItem(size);
			gnneActionL2LoadWConf = new GnneActionL2LoadWConf(lenCompressed2, lenDecompressed, checkedDataType, checkedDataType);
			_latestConfActions.L2LoadWConf = gnneActionL2LoadWConf;
			Actions.Add(gnneActionL2LoadWConf);
		}
		Gpr addrS = _gprHandler.SetGprItem1(Nncase.IR.F.Buffer.DDrOf(ddrCon.MemSpan) + 0uL);
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, offsetD));
		Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrCon);
		Gpr basement = _gprHandler.SetGprItem1(gprItem);
		Gpr validCNum = _gprHandler.SetGprItem(GNNEEnv.WAlignNum - 1);
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionL2LoadW(basement, addrD, addrS, validCNum));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateLoadIf(SegmentND ifmap, Call call, int iPp, Nncase.TIR.Buffer ddrIf, int offsetD = 0, List<int> stridesD = null, ItemName name = ItemName.Ifmap, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, bool h2c = false, int bias = 0, List<int> stridesS = null, GNNEShape shape = null, int[] layout = null)
	{
		Shape checkedShape = call[GNNELoad.Input].CheckedShape;
		DataType checkedDataType = call[GNNELoad.Input].CheckedDataType;
		DataType checkedDataType2 = call.CheckedDataType;
		if (stridesS == null)
		{
			stridesS = new int[3]
			{
				checkedShape[1].Value.Value,
				checkedShape[2].Value.Value,
				checkedShape[3].Value.Value
			}.ToList();
		}
		if (stridesD == null)
		{
			stridesD = new int[3]
			{
				ifmap[1].Length,
				ifmap[2].Length,
				ifmap[3].Length
			}.ToList();
		}
		if (shape == null)
		{
			shape = new GNNEShape(ifmap[0].Length, ifmap[1].Length, ifmap[2].Length, ifmap[3].Length);
		}
		Ssr ssr;
		Gpr n2;
		Gpr c2;
		Gpr h2;
		Gpr w;
		Ssr ssr2;
		Gpr addrD;
		if (h2c)
		{
			if (ifmap.PadH.Before > 0)
			{
				int addr = iPp * _glb.GlbMap[name].AllocatedBytes + offsetD + _glb.GlbMap[name].GetAddr(0, 0, 0, 0);
				addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, addr));
				Gpr value = _gprHandler.SetGprItem(bias);
				Gpr n = _gprHandler.SetGprItem(stridesD[0]);
				Gpr c = _gprHandler.SetGprItem(stridesD[1]);
				Gpr h = _gprHandler.SetGprItem(stridesD[2]);
				ssr = _ssRegHandler.SetSsrItem(PackStride(stridesD[0], stridesD[1], stridesD[2]));
				Actions.Add(new GnneActionPackStrideReg(n, c, h, ssr));
				n2 = _gprHandler.SetGprItem(ifmap[0].Length);
				c2 = _gprHandler.SetGprItem(ifmap[1].Length);
				h2 = _gprHandler.SetGprItem(ifmap.PadH.Before);
				w = _gprHandler.SetGprItem(ifmap[3].Length);
				ssr2 = _ssRegHandler.SetSsrItem(PackShape(ifmap[0].Length, ifmap[1].Length, ifmap.PadH.Before, ifmap[3].Length));
				Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, ssr2));
				Actions.Add(new GnneActionMfuMemset(addrD, value, ssr, ssr2, checkedDataType2));
			}
			if (ifmap.PadH.After > 0)
			{
				int addr2 = iPp * _glb.GlbMap[name].AllocatedBytes + offsetD + _glb.GlbMap[name].GetAddr(0, 0, ifmap[2].Length + ifmap.PadH.Before, 0);
				addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, addr2));
				Gpr value2 = _gprHandler.SetGprItem(bias);
				Gpr n3 = _gprHandler.SetGprItem(stridesD[0]);
				Gpr c3 = _gprHandler.SetGprItem(stridesD[1]);
				Gpr h3 = _gprHandler.SetGprItem(stridesD[2]);
				ssr = _ssRegHandler.SetSsrItem(PackStride(stridesD[0], stridesD[1], stridesD[2]));
				Actions.Add(new GnneActionPackStrideReg(n3, c3, h3, ssr));
				n2 = _gprHandler.SetGprItem(ifmap[0].Length);
				c2 = _gprHandler.SetGprItem(ifmap[1].Length);
				h2 = _gprHandler.SetGprItem(ifmap.PadH.After);
				w = _gprHandler.SetGprItem(ifmap[3].Length);
				ssr2 = _ssRegHandler.SetSsrItem(PackShape(ifmap[0].Length, ifmap[1].Length, ifmap.PadH.After, ifmap[3].Length));
				Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, ssr2));
				Actions.Add(new GnneActionMfuMemset(addrD, value2, ssr, ssr2, checkedDataType2));
			}
		}
		bool flag = true;
		if (call[GNNELoad.Input] is Call call2)
		{
			Expr target = call2.Target;
			if (!(target is Slice))
			{
				if (target is Reshape && ((Call)call[GNNELoad.Input])[Reshape.Input] is Call call3 && call3.Target is Slice)
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			ReshapeSpecialScenario(ref stridesS, ref shape, ref stridesD);
		}
		Ssr strideS = new Ssr(-1, PackStride(stridesS[0], stridesS[1], stridesS[2]), needRenewal: false);
		ssr = new Ssr(-1, PackStride(stridesD[0], stridesD[1], stridesD[2]), needRenewal: false);
		Gnne_action_l2_load_conf gnne_action_l2_load_conf = new Gnne_action_l2_load_conf(ssr, strideS, checkedDataType2, checkedDataType);
		if (flag)
		{
			Gpr n4 = _gprHandler.SetGprItem(stridesS[0]);
			Gpr c4 = _gprHandler.SetGprItem(stridesS[1]);
			Gpr h4 = _gprHandler.SetGprItem(stridesS[2]);
			strideS = _ssRegHandler.SetSsrItem(PackStride(stridesS[0], stridesS[1], stridesS[2]));
			Actions.Add(new GnneActionPackStrideReg(n4, c4, h4, strideS, (GNNELoad)call.Target));
		}
		else
		{
			Gpr n5 = _gprHandler.SetGprItem1(stridesS[0]);
			Gpr c5 = _gprHandler.SetGprItem1(stridesS[1]);
			Gpr h5 = _gprHandler.SetGprItem1(stridesS[2]);
			strideS = _ssRegHandler.SetSsrItem(PackStride(stridesS[0], stridesS[1], stridesS[2]), forceNew: true);
			Actions.Add(new GnneActionPackStrideReg(n5, c5, h5, strideS, (GNNELoad)call.Target));
		}
		Gpr n6 = _gprHandler.SetGprItem(stridesD[0]);
		Gpr c6 = _gprHandler.SetGprItem(stridesD[1]);
		Gpr h6 = _gprHandler.SetGprItem(stridesD[2]);
		ssr = _ssRegHandler.SetSsrItem(PackStride(stridesD[0], stridesD[1], stridesD[2]), forceNew: true);
		Actions.Add(new GnneActionPackStrideReg(n6, c6, h6, ssr));
		gnne_action_l2_load_conf = new Gnne_action_l2_load_conf(ssr, strideS, checkedDataType2, checkedDataType);
		_latestConfActions.L2LoadConf = gnne_action_l2_load_conf;
		Actions.Add(gnne_action_l2_load_conf);
		n2 = _gprHandler.SetGprItem(shape[0]);
		c2 = _gprHandler.SetGprItem(shape[1]);
		h2 = _gprHandler.SetGprItem(shape[2]);
		w = _gprHandler.SetGprItem(shape[3]);
		ssr2 = _ssRegHandler.SetSsrItem(PackShape(shape[0], shape[1], shape[2], shape[3]));
		Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, ssr2));
		int addr3 = iPp * _glb.GlbMap[name].AllocatedBytes + offsetD;
		if (h2c)
		{
			addr3 = iPp * _glb.GlbMap[name].AllocatedBytes + offsetD + ifmap[2].Padding.Before * _glb.GlbMap[name].Stride[2];
		}
		int id = _glb.GlbMap[name].Mmu.Id;
		if (name == ItemName.LstmOfH || name == ItemName.LstmOfC)
		{
			addr3 = offsetD;
			id = _glb.GlbMap[name].Mmu.Id;
		}
		Gpr addrS = _gprHandler.SetGprItem1(ddrIf.MemSpan);
		addrD = _gprHandler.SetGprItem(ToGlbAddr(id, addr3));
		Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrIf);
		Gpr basement = _gprHandler.SetGprItem1(gprItem);
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionL2Load(basement, addrD, addrS, ssr2, call, ifmap, ddrIf, layout));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateG2LIf(SegmentND slice, SegmentND ifmap, Call call, int iPp, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetS = 0, DataType type = null, ItemName name = ItemName.Ifmap, bool restored = false, int strideNReshape = 0, int strideCReshape = 0, int strideHReshape = 0, bool h2c = false, int r = 0, int uh = 1, bool slice_from = true)
	{
		DataType l2Datatype = (((object)call != null) ? call[GNNELoad.Input].CheckedDataType : (((object)type == null) ? DataTypes.UInt8 : type));
		if (restored)
		{
			Ssr strideS = new Ssr(-1, PackStride(strideNReshape, strideCReshape, strideHReshape), needRenewal: false);
			GnneActionDmLoadL1Conf gnneActionDmLoadL1Conf = new GnneActionDmLoadL1Conf(0, 0, 0, strideS, l2Datatype, L1_TYPE.if_);
			if ((object)_latestConfActions.DmLoadL1Conf == null || _latestConfActions.DmLoadL1Conf != gnneActionDmLoadL1Conf)
			{
				Gpr n = _gprHandler.SetGprItem(strideNReshape);
				Gpr c = _gprHandler.SetGprItem(strideCReshape);
				Gpr h = _gprHandler.SetGprItem(strideHReshape);
				strideS = _ssRegHandler.SetSsrItem(PackStride(strideNReshape, strideCReshape, strideHReshape));
				Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
				gnneActionDmLoadL1Conf = new GnneActionDmLoadL1Conf(0, 0, 0, strideS, l2Datatype, L1_TYPE.if_);
				_latestConfActions.DmLoadL1Conf = gnneActionDmLoadL1Conf;
				Actions.Add(gnneActionDmLoadL1Conf);
			}
		}
		else
		{
			Ssr strideS2 = new Ssr(-1, PackStride(_glb.GlbMap[name].Dimensions[1], _glb.GlbMap[name].Dimensions[2], _glb.GlbMap[name].Dimensions[3]), needRenewal: false);
			GnneActionDmLoadL1Conf gnneActionDmLoadL1Conf2 = new GnneActionDmLoadL1Conf(0, 0, 0, strideS2, l2Datatype, L1_TYPE.if_);
			if ((object)_latestConfActions.DmLoadL1Conf == null || _latestConfActions.DmLoadL1Conf != gnneActionDmLoadL1Conf2)
			{
				Gpr n2 = _gprHandler.SetGprItem(_glb.GlbMap[name].Dimensions[1]);
				Gpr c2 = _gprHandler.SetGprItem(_glb.GlbMap[name].Dimensions[2]);
				Gpr h2 = _gprHandler.SetGprItem(_glb.GlbMap[name].Dimensions[3]);
				strideS2 = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[name].Dimensions[1], _glb.GlbMap[name].Dimensions[2], _glb.GlbMap[name].Dimensions[3]));
				Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideS2));
				gnneActionDmLoadL1Conf2 = new GnneActionDmLoadL1Conf(0, 0, 0, strideS2, l2Datatype, L1_TYPE.if_);
				_latestConfActions.DmLoadL1Conf = gnneActionDmLoadL1Conf2;
				Actions.Add(gnneActionDmLoadL1Conf2);
			}
		}
		int num = slice[2].Length;
		if (h2c)
		{
			num = (int)(System.Math.Floor((double)(num - r + slice.PadH.Before + slice.PadH.After) * 1.0 / (double)uh) + 1.0);
		}
		Gpr n3 = _gprHandler.SetGprItem(slice[0].Length);
		Gpr c3 = _gprHandler.SetGprItem(slice[1].Length);
		Gpr h3 = _gprHandler.SetGprItem(num);
		Gpr w = _gprHandler.SetGprItem(slice[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(slice[0].Length, slice[1].Length, num, slice[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w, ssr));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		int num3;
		if (restored)
		{
			int num2 = (slice[0].Start - ifmap[0].Start) % ifmap[0].Length;
			num3 = offsetS + iPp * _glb.GlbMap[name].AllocatedBytes + num2 * _glb.GlbMap[name].Stride[0] + slice[2].Start * strideHReshape;
		}
		else
		{
			int dim = (slice[0].Start - ifmap[0].Start) % ifmap[0].Length;
			int dim2 = (slice[1].Start - ifmap[1].Start) % ifmap[1].Length;
			int dim3 = (slice[2].Start - ifmap[2].Start) % ifmap[2].Length;
			if (h2c)
			{
				dim3 = slice[2].Start - slice.PadH.Before - (ifmap[2].Start - ifmap.PadH.Before);
			}
			int dim4 = (slice[3].Start - ifmap[3].Start) % ifmap[3].Length;
			num3 = offsetS + iPp * _glb.GlbMap[name].AllocatedBytes;
			if (slice_from)
			{
				num3 += _glb.GlbMap[name].GetAddr(dim, dim2, dim3, dim4);
			}
		}
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, num3));
		int value = (h2c ? ((uh << 16) + r) : 0);
		Gpr htocWindow = _gprHandler.SetGprItem(value);
		Actions.Add(new GnneActionDmLoadL1(0, 0, addrS, ssr, htocWindow, L1_TYPE.if_, slice));
	}

	public void UpdateG2RW(SegmentND slice, WeightGroupHandler weightGroup, int ocPerGroup, Call call, int wPp, int pos, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetWS = 0, int offsetQargS = 0, ItemName wName = ItemName.Weight, ItemName wQargName = ItemName.WQarg, bool h2c = false)
	{
		DataType checkedDataType = call[GNNELoadW.Input].CheckedDataType;
		PrimType primType = DataTypes.Int8;
		if (checkedDataType == DataTypes.UInt8 || (checkedDataType == DataTypes.Int16 && pos == 0))
		{
			primType = DataTypes.UInt8;
		}
		int num = slice[2].Length;
		int length = slice[3].Length;
		int num2 = System.Math.Max(slice[1].Length, GNNEEnv.WAlignNum) * num * length;
		if (h2c)
		{
			num = 1;
			num2 = System.Math.Max(slice[1].Length, GNNEEnv.WAlignNum) * length;
		}
		TileUtilities.Assert(num <= 31 && length <= 31, "kernelH <= 31 && kernelW <= 31", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/ActionUpdater.cs", 746);
		Gpr strideOc = new Gpr(-1, num2, needRenewal: false);
		GnneActionDmLoadWConf gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, num, length, strideOc);
		if ((object)_latestConfActions.DmLoadWConf == null || _latestConfActions.DmLoadWConf != gnneActionDmLoadWConf)
		{
			strideOc = _gprHandler.SetGprItem(num2);
			gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, num, length, strideOc);
			_latestConfActions.DmLoadWConf = gnneActionDmLoadWConf;
			Actions.Add(gnneActionDmLoadWConf);
		}
		int num3 = System.Math.Max(slice[0].Length / ocPerGroup, 1);
		int num4 = System.Math.Min(slice[0].Length, ocPerGroup);
		Gpr groups = new Gpr(-1, num3, needRenewal: false);
		Gpr goc = new Gpr(-1, num4, needRenewal: false);
		GnneActionDmLoadWConf2 gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
		if ((object)_latestConfActions.DmLoadWConf2 == null || _latestConfActions.DmLoadWConf2 != gnneActionDmLoadWConf2)
		{
			groups = _gprHandler.SetGprItem(num3);
			goc = _gprHandler.SetGprItem(num4);
			gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
			_latestConfActions.DmLoadWConf2 = gnneActionDmLoadWConf2;
			Actions.Add(gnneActionDmLoadWConf2);
		}
		GnneActionPuWConf gnneActionPuWConf = new GnneActionPuWConf(0, 0, 5, num, length);
		if ((object)_latestConfActions.PuWConf == null || _latestConfActions.PuWConf != gnneActionPuWConf)
		{
			_latestConfActions.PuWConf = gnneActionPuWConf;
			Actions.Add(gnneActionPuWConf);
		}
		GnneActionDmLoadWConfDeq gnneActionDmLoadWConfDeq = new GnneActionDmLoadWConfDeq(0, 0, 2, primType);
		if ((object)_latestConfActions.DmLoadWConfDeq == null || _latestConfActions.DmLoadWConfDeq != gnneActionDmLoadWConfDeq)
		{
			_latestConfActions.DmLoadWConfDeq = gnneActionDmLoadWConfDeq;
			Actions.Add(gnneActionDmLoadWConfDeq);
		}
		int num5 = wPp * _glb.GlbMap[wName].AllocatedBytes + offsetWS;
		num5 += pos * weightGroup.GetAlignedShapeSize(slice);
		num5 += weightGroup.WeightGroupAlignedOffset(slice) * TileUtilities.GetBytesPerElement(checkedDataType);
		int addr = weightGroup.QargAlignedOffset(slice[0]) + offsetQargS;
		int length2 = slice[0].Length;
		int num6 = slice[1].Length;
		if (h2c)
		{
			num6 = slice[1].Length * slice[2].Length;
		}
		Gpr n = _gprHandler.SetGprItem(0);
		Gpr c = _gprHandler.SetGprItem(num6);
		Gpr h = _gprHandler.SetGprItem(length2);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackStride(0, num6, length2));
		Actions.Add(new GnneActionPackStrideReg(n, c, h, ssr));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[wName].Mmu.Id, num5));
		int mmuItem = ((!(primType == DataTypes.Int8)) ? _glb.GlbMap[wQargName].Mmu.Id : 0);
		Gpr addrBw = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr));
		Actions.Add(new GnneActionDmLoadW(0, 0, addrS, addrBw, ssr, DM_LOAD_W_DEST.pu, slice));
	}

	public void UpdateG2RWMatmul(SegmentND slice, SegmentND tensor, WeightGroupHandler weightGroup, int ocPerGroup, Call call, int wPp, int pos, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int n = 0, ItemName wName = ItemName.Weight, ItemName wQargName = ItemName.WQarg)
	{
		DataType checkedDataType = call[GNNELoad.Input].CheckedDataType;
		PrimType primType = DataTypes.Int8;
		if (checkedDataType == DataTypes.UInt8 || (checkedDataType == DataTypes.Int16 && pos == 0))
		{
			primType = DataTypes.UInt8;
		}
		int length = slice[2].Length;
		int length2 = slice[3].Length;
		int num = TileUtilities.GetAlignedNum(tensor[1].Length, GNNEEnv.WAlignNum) * length * length2;
		TileUtilities.Assert(length == 1 && length2 == 1, "kernelH == 1 && kernelW == 1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/ActionUpdater.cs", 834);
		Gpr strideOc = new Gpr(-1, num, needRenewal: false);
		GnneActionDmLoadWConf gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, length, length2, strideOc);
		if ((object)_latestConfActions.DmLoadWConf == null || _latestConfActions.DmLoadWConf != gnneActionDmLoadWConf)
		{
			strideOc = _gprHandler.SetGprItem(num);
			gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, length, length2, strideOc);
			_latestConfActions.DmLoadWConf = gnneActionDmLoadWConf;
			Actions.Add(gnneActionDmLoadWConf);
		}
		int num2 = System.Math.Max(slice[0].Length / ocPerGroup, 1);
		int num3 = System.Math.Min(slice[0].Length, ocPerGroup);
		Gpr groups = new Gpr(-1, num2, needRenewal: false);
		Gpr goc = new Gpr(-1, num3, needRenewal: false);
		GnneActionDmLoadWConf2 gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
		if ((object)_latestConfActions.DmLoadWConf2 == null || _latestConfActions.DmLoadWConf2 != gnneActionDmLoadWConf2)
		{
			groups = _gprHandler.SetGprItem(num2);
			goc = _gprHandler.SetGprItem(num3);
			gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
			_latestConfActions.DmLoadWConf2 = gnneActionDmLoadWConf2;
			Actions.Add(gnneActionDmLoadWConf2);
		}
		GnneActionPuWConf gnneActionPuWConf = new GnneActionPuWConf(0, 0, 5, length, length2);
		if ((object)_latestConfActions.PuWConf == null || _latestConfActions.PuWConf != gnneActionPuWConf)
		{
			_latestConfActions.PuWConf = gnneActionPuWConf;
			Actions.Add(gnneActionPuWConf);
		}
		GnneActionDmLoadWConfDeq gnneActionDmLoadWConfDeq = new GnneActionDmLoadWConfDeq(0, 0, 2, primType);
		if ((object)_latestConfActions.DmLoadWConfDeq == null || _latestConfActions.DmLoadWConfDeq != gnneActionDmLoadWConfDeq)
		{
			_latestConfActions.DmLoadWConfDeq = gnneActionDmLoadWConfDeq;
			Actions.Add(gnneActionDmLoadWConfDeq);
		}
		int num4 = wPp * _glb.GlbMap[wName].AllocatedBytes + n * tensor[0].Length * num * TileUtilities.GetBytesPerElement(primType);
		num4 += pos * _glb.GlbMap[wName].AllocatedBytes / 2;
		num4 += ((slice[0].Start - tensor[0].Start) * num + (slice[1].Start - tensor[1].Start)) * TileUtilities.GetBytesPerElement(primType);
		int addr = weightGroup.QargAlignedOffset(slice[0]) + n * weightGroup.CurrentQargAlignedOffset();
		int length3 = slice[0].Length;
		int length4 = slice[1].Length;
		Gpr n2 = _gprHandler.SetGprItem(0);
		Gpr c = _gprHandler.SetGprItem(length4);
		Gpr h = _gprHandler.SetGprItem(length3);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackStride(0, length4, length3));
		Actions.Add(new GnneActionPackStrideReg(n2, c, h, ssr));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[wName].Mmu.Id, num4));
		int mmuItem = ((!(primType == DataTypes.Int8)) ? _glb.GlbMap[wQargName].Mmu.Id : 0);
		Gpr addrBw = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr));
		Actions.Add(new GnneActionDmLoadW(0, 0, addrS, addrBw, ssr, DM_LOAD_W_DEST.pu, slice));
	}

	public void UpdateG2RDw(Call call, SegmentND slice, WeightGroupHandler weightGroup, int ocPerGroup, int offsetWS = 0, int offsetQargS = 0, List<CcrClr> dwCcrsToClr = null)
	{
		DataType checkedDataType = call[GNNEPdp0DW.Input].CheckedDataType;
		int length = slice[2].Length;
		int length2 = slice[3].Length;
		int num = GNNEEnv.PuWidth * length * length2;
		Gpr strideOc = new Gpr(-1, num, needRenewal: false);
		GnneActionDmLoadWConf gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, length, length2, strideOc);
		if ((object)_latestConfActions.DmLoadWConf == null || _latestConfActions.DmLoadWConf != gnneActionDmLoadWConf)
		{
			strideOc = _gprHandler.SetGprItem(num);
			gnneActionDmLoadWConf = new GnneActionDmLoadWConf(0, 0, 1, length, length2, strideOc);
			_latestConfActions.DmLoadWConf = gnneActionDmLoadWConf;
			Actions.Add(gnneActionDmLoadWConf);
		}
		int num2 = System.Math.Max(slice[0].Length / ocPerGroup, 1);
		int num3 = System.Math.Min(slice[0].Length, ocPerGroup);
		Gpr groups = new Gpr(-1, num2, needRenewal: false);
		Gpr goc = new Gpr(-1, num3, needRenewal: false);
		GnneActionDmLoadWConf2 gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
		if ((object)_latestConfActions.DmLoadWConf2 == null || _latestConfActions.DmLoadWConf2 != gnneActionDmLoadWConf2)
		{
			groups = _gprHandler.SetGprItem(num2);
			goc = _gprHandler.SetGprItem(num3);
			gnneActionDmLoadWConf2 = new GnneActionDmLoadWConf2(0, 0, 5, groups, goc);
			_latestConfActions.DmLoadWConf2 = gnneActionDmLoadWConf2;
			Actions.Add(gnneActionDmLoadWConf2);
		}
		GnneActionPuPdp0WConf gnneActionPuPdp0WConf = new GnneActionPuPdp0WConf(0, 0, 6, length, length2);
		if ((object)_latestConfActions.Pdp0WConf == null || _latestConfActions.Pdp0WConf != gnneActionPuPdp0WConf)
		{
			_latestConfActions.Pdp0WConf = gnneActionPuPdp0WConf;
			Actions.Add(gnneActionPuPdp0WConf);
		}
		GnneActionDmLoadWConfDeq gnneActionDmLoadWConfDeq = new GnneActionDmLoadWConfDeq(0, 0, 2, checkedDataType);
		if ((object)_latestConfActions.DmLoadWConfDeq == null || _latestConfActions.DmLoadWConfDeq != gnneActionDmLoadWConfDeq)
		{
			_latestConfActions.DmLoadWConfDeq = gnneActionDmLoadWConfDeq;
			Actions.Add(gnneActionDmLoadWConfDeq);
		}
		int addr = weightGroup.DwGroupAlignedOffset(slice) * TileUtilities.GetBytesPerElement(checkedDataType) + offsetWS;
		int addr2 = weightGroup.DwQargAlignedOffset(slice[1]) + offsetQargS;
		int num4 = 1;
		int num5 = slice[0].Length * slice[1].Length;
		Gpr n = _gprHandler.SetGprItem(0);
		Gpr c = _gprHandler.SetGprItem(num5);
		Gpr h = _gprHandler.SetGprItem(num4);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackStride(0, num5, num4));
		Actions.Add(new GnneActionPackStrideReg(n, c, h, ssr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.DwWeight].Mmu.Id, addr));
		int mmuItem = ((!(checkedDataType == DataTypes.Int8)) ? _glb.GlbMap[ItemName.DwQarg].Mmu.Id : 0);
		Gpr addrBw = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr2));
		if (GNNEEnv.UseCcr)
		{
			List<CcrSet> ccrsToSet = new List<CcrSet>();
			UpdateCcr(ccrsToSet, dwCcrsToClr);
		}
		Actions.Add(new GnneActionDmLoadW(0, 0, addrS, addrBw, ssr, DM_LOAD_W_DEST.pdp0, slice));
	}

	public void UpdateL2RIf(SegmentND slice, SegmentND ifmap, int strideH, int strideW, int icPerGroup, Call call, int padValue, int pos, int bias, DataType type = null, bool h2c = false, int r = 0)
	{
		DataType dataType = (((object)call != null) ? call[GNNELoad.Input].CheckedDataType : (((object)type == null) ? DataTypes.UInt8 : type));
		PrimType quantType = DataTypes.Int8;
		if (dataType == DataTypes.UInt8 || (dataType == DataTypes.Int16 && pos == 0))
		{
			quantType = DataTypes.UInt8;
		}
		int num = 24;
		int num2 = ifmap[2].Length;
		int length = ifmap[3].Length;
		int strideH2 = strideH;
		if (h2c)
		{
			strideH2 = 1;
			num2 = (int)(System.Math.Floor((double)(slice[2].Length - r + slice.PadH.Before + slice.PadH.After) * 1.0 / (double)strideH) + 1.0);
		}
		Ssr strideS = new Ssr(-1, PackStride(num, num2, length), needRenewal: false);
		GnneActionPuFetchifConf1 gnneActionPuFetchifConf = new GnneActionPuFetchifConf1(0, 0, 0, strideW, strideH2, strideS);
		if ((object)_latestConfActions.FetchifConf1 == null || _latestConfActions.FetchifConf1 != gnneActionPuFetchifConf)
		{
			Gpr n = _gprHandler.SetGprItem(num);
			Gpr c = _gprHandler.SetGprItem(num2);
			Gpr h = _gprHandler.SetGprItem(length);
			strideS = _ssRegHandler.SetSsrItem(PackStride(num, num2, length));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
			gnneActionPuFetchifConf = new GnneActionPuFetchifConf1(0, 0, 0, strideW, strideH2, strideS);
			_latestConfActions.FetchifConf1 = gnneActionPuFetchifConf;
			Actions.Add(gnneActionPuFetchifConf);
		}
		int num3 = System.Math.Max(ifmap[1].Length / icPerGroup, 1);
		int num4 = System.Math.Min(slice[1].Length, icPerGroup);
		if (h2c)
		{
			num4 *= r;
		}
		Gpr gic = new Gpr(-1, num4, needRenewal: false);
		GnneActionPuFetchifConf2 gnneActionPuFetchifConf2 = new GnneActionPuFetchifConf2(0, 0, 1, gic, new Gpr(-1, -1, needRenewal: false));
		if ((object)_latestConfActions.FetchifConf2 == null || _latestConfActions.FetchifConf2 != gnneActionPuFetchifConf2)
		{
			gic = _gprHandler.SetGprItem(num4);
			gnneActionPuFetchifConf2 = new GnneActionPuFetchifConf2(0, 0, 1, gic, new Gpr(-1, -1, needRenewal: false));
			_latestConfActions.FetchifConf2 = gnneActionPuFetchifConf2;
			Actions.Add(gnneActionPuFetchifConf2);
		}
		int num5 = slice[1].Length;
		int num6 = slice[2].Length + slice.PadH.Sum();
		int num7 = slice[3].Length + slice.PadW.Sum();
		if (h2c)
		{
			num6 = num2;
			num5 *= r;
		}
		int num8 = slice[2].Start - ifmap[2].Start;
		int num9 = slice[3].Start - ifmap[3].Start;
		int num10 = ifmap[3].Length * num8 + num9;
		if (pos == 1)
		{
			num10 += GNNEEnv.IfL1Size / GNNEEnv.PuHeight / 2;
		}
		Ssr shape = new Ssr(-1, PackShape(slice[0].Length, num5, num6, num7), needRenewal: false);
		Gpr addrS = new Gpr(-1, num10, needRenewal: false);
		Gpr groups = new Gpr(-1, num3, needRenewal: false);
		GnneActionPuFetchifConf3 gnneActionPuFetchifConf3 = new GnneActionPuFetchifConf3(0, 0, 2, addrS, groups, shape);
		if ((object)_latestConfActions.FetchifConf3 == null || _latestConfActions.FetchifConf3 != gnneActionPuFetchifConf3)
		{
			Gpr n2 = _gprHandler.SetGprItem(slice[0].Length);
			Gpr c2 = _gprHandler.SetGprItem(num5);
			Gpr h2 = _gprHandler.SetGprItem(num6);
			Gpr w = _gprHandler.SetGprItem(num7);
			shape = _ssRegHandler.SetSsrItem(PackShape(slice[0].Length, num5, num6, num7));
			Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, shape));
			addrS = _gprHandler.SetGprItem(num10);
			groups = _gprHandler.SetGprItem(num3);
			gnneActionPuFetchifConf3 = new GnneActionPuFetchifConf3(0, 0, 2, addrS, groups, shape);
			_latestConfActions.FetchifConf3 = gnneActionPuFetchifConf3;
			Actions.Add(gnneActionPuFetchifConf3);
		}
		Ssr ssr = new Ssr(-1, -1L, needRenewal: false);
		if (slice[2].Length == 0 || slice[3].Length == 0)
		{
			Gpr padValue2 = new Gpr(-1, padValue, needRenewal: false);
			ssr = new Ssr(-1, PackShape(num6, 0, num7, 0), needRenewal: false);
			GnneActionPuFetchifConf4 gnneActionPuFetchifConf4 = new GnneActionPuFetchifConf4(0, 0, 3, padValue2, ssr);
			if ((object)_latestConfActions.FetchifConf4 == null || _latestConfActions.FetchifConf4 != gnneActionPuFetchifConf4)
			{
				padValue2 = _gprHandler.SetGprItem(padValue);
				Gpr n3 = _gprHandler.SetGprItem(num6);
				Gpr c3 = _gprHandler.SetGprItem(0);
				Gpr h3 = _gprHandler.SetGprItem(num7);
				Gpr w2 = _gprHandler.SetGprItem(0);
				ssr = _ssRegHandler.SetSsrItem(PackShape(num6, 0, num7, 0));
				Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w2, ssr));
				gnneActionPuFetchifConf4 = new GnneActionPuFetchifConf4(0, 0, 3, padValue2, ssr);
				_latestConfActions.FetchifConf4 = gnneActionPuFetchifConf4;
				Actions.Add(gnneActionPuFetchifConf4);
			}
		}
		else if (h2c)
		{
			Gpr padValue3 = new Gpr(-1, padValue, needRenewal: false);
			ssr = new Ssr(-1, PackShape(0, 0, slice.PadW.Before, slice.PadW.After), needRenewal: false);
			GnneActionPuFetchifConf4 gnneActionPuFetchifConf5 = new GnneActionPuFetchifConf4(0, 0, 3, padValue3, ssr);
			if ((object)_latestConfActions.FetchifConf4 == null || _latestConfActions.FetchifConf4 != gnneActionPuFetchifConf5)
			{
				padValue3 = _gprHandler.SetGprItem(padValue);
				Gpr n4 = _gprHandler.SetGprItem(0);
				Gpr c4 = _gprHandler.SetGprItem(0);
				Gpr h4 = _gprHandler.SetGprItem(slice.PadW.Before);
				Gpr w3 = _gprHandler.SetGprItem(slice.PadW.After);
				ssr = _ssRegHandler.SetSsrItem(PackShape(0, 0, slice.PadW.Before, slice.PadW.After));
				Actions.Add(new GnneActionPackShapeReg(n4, c4, h4, w3, ssr));
				gnneActionPuFetchifConf5 = new GnneActionPuFetchifConf4(0, 0, 3, padValue3, ssr);
				_latestConfActions.FetchifConf4 = gnneActionPuFetchifConf5;
				Actions.Add(gnneActionPuFetchifConf5);
			}
		}
		else
		{
			Gpr padValue4 = new Gpr(-1, padValue, needRenewal: false);
			ssr = new Ssr(-1, PackShape(slice.PadH.Before, slice.PadH.After, slice.PadW.Before, slice.PadW.After), needRenewal: false);
			GnneActionPuFetchifConf4 gnneActionPuFetchifConf6 = new GnneActionPuFetchifConf4(0, 0, 3, padValue4, ssr);
			if ((object)_latestConfActions.FetchifConf4 == null || _latestConfActions.FetchifConf4 != gnneActionPuFetchifConf6)
			{
				padValue4 = _gprHandler.SetGprItem(padValue);
				Gpr n5 = _gprHandler.SetGprItem(slice.PadH.Before);
				Gpr c5 = _gprHandler.SetGprItem(slice.PadH.After);
				Gpr h5 = _gprHandler.SetGprItem(slice.PadW.Before);
				Gpr w4 = _gprHandler.SetGprItem(slice.PadW.After);
				ssr = _ssRegHandler.SetSsrItem(PackShape(slice.PadH.Before, slice.PadH.After, slice.PadW.Before, slice.PadW.After));
				Actions.Add(new GnneActionPackShapeReg(n5, c5, h5, w4, ssr));
				gnneActionPuFetchifConf6 = new GnneActionPuFetchifConf4(0, 0, 3, padValue4, ssr);
				_latestConfActions.FetchifConf4 = gnneActionPuFetchifConf6;
				Actions.Add(gnneActionPuFetchifConf6);
			}
		}
		int num11 = 31;
		Gpr ic = new Gpr(-1, num11, needRenewal: false);
		Gpr bx = new Gpr(-1, bias, needRenewal: false);
		GnneActionPuFetchifConfDeq gnneActionPuFetchifConfDeq = new GnneActionPuFetchifConfDeq(0, 0, 4, ic, bx, quantType);
		if ((object)_latestConfActions.FetchifConfDeq == null || _latestConfActions.FetchifConfDeq != gnneActionPuFetchifConfDeq)
		{
			ic = _gprHandler.SetGprItem(num11);
			bx = _gprHandler.SetGprItem(bias);
			gnneActionPuFetchifConfDeq = new GnneActionPuFetchifConfDeq(0, 0, 4, ic, bx, quantType);
			_latestConfActions.FetchifConfDeq = gnneActionPuFetchifConfDeq;
			Actions.Add(gnneActionPuFetchifConfDeq);
		}
	}

	public void UpdateR2LPsum(int shift, SegmentND r2LPsum, SegmentND l2GOf, SegmentND storeOf, int ofPp, int l1Pp, ACT0_OUTPUT_DEST destTarget, bool releaseIf, int pos, TcuComputeMode tcuMode, bool loopStart, bool loopEnd, int strideH, int strideW, int ocPerGroup, DataType ifType, DataType wType, DataType ofType, DataType actType, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetAct = 0, int offsetL2GOf = 0, List<CcrClr> actCcrToClr = null, ItemName act0Name = ItemName.Act, bool l2g_of_slice_from = true)
	{
		if (loopEnd)
		{
			UpdateDmLoadAct0(l2GOf, tcuMode, isByChannel: true, ACT0_CHANNEL.pu, actType, act0Name, offsetAct, actCcrToClr);
			if (destTarget == ACT0_OUTPUT_DEST.dm)
			{
				UpdateDmStoreOf(l2GOf, storeOf, ofPp, ofType, ACT0_CHANNEL.pu, ccrsToSet, ccrsToClr, offsetL2GOf, l2g_of_slice_from);
			}
			List<CcrSet> list = new List<CcrSet>();
			if (destTarget == ACT0_OUTPUT_DEST.psum)
			{
				list.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Psum, l1Pp)), 1));
			}
			UpdateAct0Compute(l2GOf, shift, l1Pp, ifType, wType, ofType, destTarget, ACT0_CHANNEL.pu, isByChannel: true, list);
		}
		int num = System.Math.Min(r2LPsum[1].Length, ocPerGroup);
		Gpr gpr = new Gpr(-1, -1, needRenewal: false);
		Ssr ssr = new Ssr(-1, -1L, needRenewal: false);
		if (tcuMode == TcuComputeMode.TransposeConv2d)
		{
			Gpr goc = new Gpr(-1, num, needRenewal: false);
			GnneActionPuOfConf1 gnneActionPuOfConf = new GnneActionPuOfConf1(strideD: new Ssr(-1, PackStride(l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length * strideH), needRenewal: false), tcuId: 0, puId: 0, funct4: 6, goc: goc, gocLast: new Gpr(-1, -1, needRenewal: false));
			if ((object)_latestConfActions.PuOfConf1 == null || _latestConfActions.PuOfConf1 != gnneActionPuOfConf)
			{
				Gpr n = _gprHandler.SetGprItem(l2GOf[1].Length);
				Gpr c = _gprHandler.SetGprItem(l2GOf[2].Length);
				gpr = _gprHandler.SetGprItem(l2GOf[3].Length * strideH);
				ssr = _ssRegHandler.SetSsrItem(PackStride(l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length * strideH));
				Actions.Add(new GnneActionPackStrideReg(n, c, gpr, ssr));
				goc = _gprHandler.SetGprItem(num);
				gnneActionPuOfConf = new GnneActionPuOfConf1(0, 0, 6, goc, new Gpr(-1, -1, needRenewal: false), ssr);
				_latestConfActions.PuOfConf1 = gnneActionPuOfConf;
				Actions.Add(gnneActionPuOfConf);
			}
		}
		else
		{
			Gpr goc2 = new Gpr(-1, num, needRenewal: false);
			GnneActionPuOfConf1 gnneActionPuOfConf2 = new GnneActionPuOfConf1(strideD: new Ssr(-1, PackStride(r2LPsum[1].Length, r2LPsum[2].Length, r2LPsum[3].Length), needRenewal: false), tcuId: 0, puId: 0, funct4: 6, goc: goc2, gocLast: new Gpr(-1, -1, needRenewal: false));
			if ((object)_latestConfActions.PuOfConf1 == null || _latestConfActions.PuOfConf1 != gnneActionPuOfConf2)
			{
				Gpr n2 = _gprHandler.SetGprItem(r2LPsum[1].Length);
				Gpr c2 = _gprHandler.SetGprItem(r2LPsum[2].Length);
				gpr = _gprHandler.SetGprItem(r2LPsum[3].Length);
				ssr = _ssRegHandler.SetSsrItem(PackStride(r2LPsum[1].Length, r2LPsum[2].Length, r2LPsum[3].Length));
				Actions.Add(new GnneActionPackStrideReg(n2, c2, gpr, ssr));
				goc2 = _gprHandler.SetGprItem(num);
				gnneActionPuOfConf2 = new GnneActionPuOfConf1(0, 0, 6, goc2, new Gpr(-1, -1, needRenewal: false), ssr);
				_latestConfActions.PuOfConf1 = gnneActionPuOfConf2;
				Actions.Add(gnneActionPuOfConf2);
			}
		}
		int num2 = 0;
		int num3 = 0;
		if (tcuMode == TcuComputeMode.TransposeConv2d)
		{
			num2 = r2LPsum[2].Start - l2GOf[2].Start;
			num3 = r2LPsum[3].Start - l2GOf[3].Start;
		}
		int num4 = (num2 * l2GOf[3].Length + num3 + l1Pp * GNNEEnv.PsumL1ElePerChan / 2) * 4;
		Gpr gpr2 = new Gpr(-1, -1, needRenewal: false);
		Gpr gpr3 = new Gpr(-1, -1, needRenewal: false);
		Ssr ssr2 = new Ssr(-1, -1L, needRenewal: false);
		if (tcuMode == TcuComputeMode.TransposeConv2d)
		{
			Gpr addrD = new Gpr(-1, num4, needRenewal: false);
			ssr2 = new Ssr(-1, PackShape(r2LPsum[0].Length, r2LPsum[1].Length, (int)System.Math.Ceiling(1.0 * (double)r2LPsum[2].Length / (double)strideH), (int)System.Math.Ceiling(1.0 * (double)r2LPsum[3].Length / (double)strideW)), needRenewal: false);
			GnneActionPuOfConf2 gnneActionPuOfConf3 = new GnneActionPuOfConf2(0, 0, 7, addrD, ssr2);
			if ((object)_latestConfActions.PuOfConf2 == null || _latestConfActions.PuOfConf2 != gnneActionPuOfConf3)
			{
				Gpr n3 = _gprHandler.SetGprItem(r2LPsum[0].Length);
				Gpr c3 = _gprHandler.SetGprItem(r2LPsum[1].Length);
				gpr2 = _gprHandler.SetGprItem((int)System.Math.Ceiling(1.0 * (double)r2LPsum[2].Length / (double)strideH));
				gpr3 = _gprHandler.SetGprItem((int)System.Math.Ceiling(1.0 * (double)r2LPsum[3].Length / (double)strideW));
				ssr2 = _ssRegHandler.SetSsrItem(PackShape(r2LPsum[0].Length, r2LPsum[1].Length, (int)System.Math.Ceiling(1.0 * (double)r2LPsum[2].Length / (double)strideH), (int)System.Math.Ceiling(1.0 * (double)r2LPsum[3].Length / (double)strideW)));
				Actions.Add(new GnneActionPackShapeReg(n3, c3, gpr2, gpr3, ssr2));
				addrD = _gprHandler.SetGprItem(num4);
				gnneActionPuOfConf3 = new GnneActionPuOfConf2(0, 0, 7, addrD, ssr2);
				_latestConfActions.PuOfConf2 = gnneActionPuOfConf3;
				Actions.Add(gnneActionPuOfConf3);
			}
		}
		else
		{
			Gpr addrD2 = new Gpr(-1, num4, needRenewal: false);
			ssr2 = new Ssr(-1, PackShape(r2LPsum[0].Length, r2LPsum[1].Length, r2LPsum[2].Length, r2LPsum[3].Length), needRenewal: false);
			GnneActionPuOfConf2 gnneActionPuOfConf4 = new GnneActionPuOfConf2(0, 0, 7, addrD2, ssr2);
			if ((object)_latestConfActions.PuOfConf2 == null || _latestConfActions.PuOfConf2 != gnneActionPuOfConf4)
			{
				Gpr n4 = _gprHandler.SetGprItem(r2LPsum[0].Length);
				Gpr c4 = _gprHandler.SetGprItem(r2LPsum[1].Length);
				gpr2 = _gprHandler.SetGprItem(r2LPsum[2].Length);
				gpr3 = _gprHandler.SetGprItem(r2LPsum[3].Length);
				ssr2 = _ssRegHandler.SetSsrItem(PackShape(r2LPsum[0].Length, r2LPsum[1].Length, r2LPsum[2].Length, r2LPsum[3].Length));
				Actions.Add(new GnneActionPackShapeReg(n4, c4, gpr2, gpr3, ssr2));
				addrD2 = _gprHandler.SetGprItem(num4);
				gnneActionPuOfConf4 = new GnneActionPuOfConf2(0, 0, 7, addrD2, ssr2);
				_latestConfActions.PuOfConf2 = gnneActionPuOfConf4;
				Actions.Add(gnneActionPuOfConf4);
			}
		}
		bool loadPsum = !loopStart;
		bool clrPsum = loopStart && tcuMode == TcuComputeMode.TransposeConv2d;
		PU_COMPUTE_MODE pU_COMPUTE_MODE = ((tcuMode == TcuComputeMode.TransposeConv2d) ? PU_COMPUTE_MODE.pu_mode_deconv : PU_COMPUTE_MODE.pu_mode_normal);
		PU_OUTPUT_DEST destTarget2 = ((loopEnd && pU_COMPUTE_MODE != PU_COMPUTE_MODE.pu_mode_deconv) ? PU_OUTPUT_DEST.act0 : PU_OUTPUT_DEST.psum);
		GnneActionPuComputeConf gnneActionPuComputeConf = new GnneActionPuComputeConf(0, 0, 8, loadPsum, clrPsum, destTarget2, releaseIf, pU_COMPUTE_MODE);
		if ((object)_latestConfActions.PuComputeConf == null || _latestConfActions.PuComputeConf != gnneActionPuComputeConf)
		{
			_latestConfActions.PuComputeConf = gnneActionPuComputeConf;
			Actions.Add(gnneActionPuComputeConf);
		}
		PU_OF_SHIFT_MODE mode = PU_OF_SHIFT_MODE.none;
		if (ifType == DataTypes.Int16 || wType == DataTypes.Int16)
		{
			mode = ((pos != 0) ? PU_OF_SHIFT_MODE.left_shift_4 : PU_OF_SHIFT_MODE.right_shift_4);
		}
		Actions.Add(new GnneActionPuCompute(0, mode));
		if (tcuMode == TcuComputeMode.TransposeConv2d && loopEnd)
		{
			int value = 0;
			int value2 = l2GOf[2].Length * l2GOf[3].Length;
			Gpr addr = _gprHandler.SetGprItem(value);
			Gpr len = _gprHandler.SetGprItem(value2);
			Actions.Add(new GnneActionPuForwardPsum(0, 0, addr, len));
		}
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateL2RPsum(Call dw, Call pool, Call act1, SegmentND l2RIf2, SegmentND ifmap2, SegmentND l2RDw, SegmentND l2RPsum, SegmentND l2GOf, SegmentND storeOf, int ofPp, int l1Pp, ACT0_OUTPUT_DEST destTarget, TcuComputeMode tcuMode, int ocPerGroup, WeightGroupHandler weightGroup, List<CcrClr> l2RIf2CcrsToClr = null, List<CcrSet> ofCcrsToSet = null, List<CcrClr> ofCcrsToClr = null, int offsetSSrc2 = 0, int offsetAct = 0, int offsetAct1 = 0, int offsetOfmap = 0, int offsetSDw = 0, int offsetSDwQarg = 0, ItemName src2ItemName = ItemName.Ifmap2, List<CcrClr> dwCcrsToClr = null, List<CcrClr> act1CcrsToClr = null, bool swapAB = false)
	{
		if (act1 != null)
		{
			DataType checkedDataType = act1[GNNEActivation.InputA].CheckedDataType;
			DataType quantType = checkedDataType;
			if (act1[GNNEActivation.InputB] != None.Default)
			{
				quantType = act1[GNNEActivation.InputB].CheckedDataType;
			}
			DataType checkedDataType2 = act1.CheckedDataType;
			DeQuantizeParam deqParams = ((TensorConst)act1[GNNEActivation.DeqAParams]).Value.ToArray<DeQuantizeParam>()[0];
			DeQuantizeParam deqParams2 = ((TensorConst)act1[GNNEActivation.DeqBParams]).Value.ToArray<DeQuantizeParam>()[0];
			int rshiftBits = ((TensorConst)act1[GNNEActivation.InAShiftBits]).Value.ToScalar<int>();
			int rshiftBits2 = ((TensorConst)act1[GNNEActivation.InBShiftBits]).Value.ToScalar<int>();
			int rshiftBitsD = ((TensorConst)act1[GNNEActivation.OutShiftBits]).Value.ToScalar<int>();
			if ((act1[GNNEActivation.InputA] is Call call && call.Target is GNNELoad) || swapAB)
			{
				checkedDataType = act1[GNNEActivation.InputB].CheckedDataType;
				quantType = act1[GNNEActivation.InputA].CheckedDataType;
				deqParams = ((TensorConst)act1[GNNEActivation.DeqBParams]).Value.ToArray<DeQuantizeParam>()[0];
				deqParams2 = ((TensorConst)act1[GNNEActivation.DeqAParams]).Value.ToArray<DeQuantizeParam>()[0];
				rshiftBits = ((TensorConst)act1[GNNEActivation.InBShiftBits]).Value.ToScalar<int>();
				rshiftBits2 = ((TensorConst)act1[GNNEActivation.InAShiftBits]).Value.ToScalar<int>();
			}
			UpdateMfuAct1InFuse(l2RPsum, l2RIf2, ifmap2, l2GOf, storeOf, ACT1_SOURCE_TYPE.psum, ACT1_SOURCE_TYPE.l2, checkedDataType, quantType, checkedDataType2, deqParams, deqParams2, rshiftBits, rshiftBits2, rshiftBitsD, ((TensorConst)act1[GNNEActivation.Is16Segments]).Value.ToScalar<bool>(), ofPp, l1Pp, l2RIf2CcrsToClr, ofCcrsToSet, ofCcrsToClr, offsetSSrc2, offsetAct1, offsetOfmap, src2ItemName, act1CcrsToClr, (((GNNEActivation)act1.Target).Type == GnneActivationType.Mul) ? MFU_ACT1_FUNCTION.mul : MFU_ACT1_FUNCTION.add);
		}
		else
		{
			PU_PDP0_MODE mode = PU_PDP0_MODE.dw;
			DataType actType = DataTypes.UInt8;
			DataType dataType = DataTypes.UInt8;
			DataType wType = DataTypes.UInt8;
			DataType ofType = DataTypes.UInt8;
			int shift = 0;
			int kernelH = 1;
			int kernelW = 1;
			int strideH = 1;
			int strideW = 1;
			int num = 0;
			int num2 = 0;
			ItemName name = ItemName.DwAct1;
			if (dw != null)
			{
				actType = ((Call)dw[GNNEPdp0DW.Act])[GNNELoadW.Input].CheckedDataType;
				dataType = dw[GNNEPdp0DW.Input].CheckedDataType;
				wType = ((Call)dw[GNNEPdp0DW.Weights])[GNNELoadW.Input].CheckedDataType;
				ofType = dw.CheckedDataType;
				shift = ((TensorConst)dw[GNNEPdp0DW.ShiftBits]).Value.ToScalar<int>();
				kernelH = dw[GNNEPdp0DW.Weights].CheckedShape[2].FixedValue;
				kernelW = dw[GNNEPdp0DW.Weights].CheckedShape[3].FixedValue;
				num = ((TensorConst)dw[GNNEPdp0DW.PadValue]).Value.ToScalar<int>();
				num2 = ((TensorConst)dw[GNNEPdp0DW.DeqBias]).Value.ToScalar<int>();
				strideH = ((TensorConst)dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[0];
				strideW = ((TensorConst)dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[1];
				name = ItemName.DwAct1;
			}
			else if (pool != null)
			{
				actType = ((Call)pool[GNNEPdp0Reduce.Act])[GNNELoadW.Input].CheckedDataType;
				dataType = pool[GNNEPdp0Reduce.Input].CheckedDataType;
				ofType = pool.CheckedDataType;
				shift = ((TensorConst)pool[GNNEPdp0Reduce.ShiftBits]).Value.ToScalar<int>();
				kernelH = ((TensorConst)pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[0];
				kernelW = ((TensorConst)pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[1];
				mode = ((GNNEPdp0Reduce)pool.Target).ReduceOp;
				num = ((TensorConst)pool[GNNEPdp0Reduce.Value]).Value.ToScalar<int>();
				num2 = ((TensorConst)pool[GNNEPdp0Reduce.DepuantParams]).Value.ToScalar<DeQuantizeParam>().ZeroPoint;
				strideH = ((TensorConst)pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[0];
				strideW = ((TensorConst)pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[1];
				name = ItemName.PdpAct1;
			}
			int length = l2RPsum[1].Length;
			int length2 = l2RPsum[2].Length;
			int length3 = l2RPsum[3].Length;
			int length4 = l2GOf[1].Length;
			int length5 = l2GOf[2].Length;
			int length6 = l2GOf[3].Length;
			Padding padding = l2RPsum[2].Padding;
			Padding padding2 = l2RPsum[3].Padding;
			int num3 = num;
			if (dataType == DataTypes.UInt8)
			{
				num3 = num - num2;
			}
			if (pool != null)
			{
				GnneActionPuPdp0WConf gnneActionPuPdp0WConf = new GnneActionPuPdp0WConf(0, 0, 6, kernelH, kernelW);
				if ((object)_latestConfActions.Pdp0WConf == null || _latestConfActions.Pdp0WConf != gnneActionPuPdp0WConf)
				{
					_latestConfActions.Pdp0WConf = gnneActionPuPdp0WConf;
					Actions.Add(gnneActionPuPdp0WConf);
				}
			}
			UpdateDmLoadAct0(l2GOf, tcuMode, isByChannel: true, ACT0_CHANNEL.pdp0, actType, name, offsetAct, act1CcrsToClr);
			UpdateDmStoreOf(l2GOf, storeOf, ofPp, ofType, ACT0_CHANNEL.pdp0, ofCcrsToSet, ofCcrsToClr, offsetOfmap);
			UpdateAct0Compute(l2GOf, shift, l1Pp, dataType, wType, ofType, destTarget, ACT0_CHANNEL.pdp0, isByChannel: true);
			GnneActionPuPdp0ModeConf gnneActionPuPdp0ModeConf = new GnneActionPuPdp0ModeConf(0, 0, 0, mode);
			if ((object)_latestConfActions.Pdp0ModeConf == null || _latestConfActions.Pdp0ModeConf != gnneActionPuPdp0ModeConf)
			{
				_latestConfActions.Pdp0ModeConf = gnneActionPuPdp0ModeConf;
				Actions.Add(gnneActionPuPdp0ModeConf);
			}
			GnneActionPuPdp0FetchifConf1 gnneActionPuPdp0FetchifConf = new GnneActionPuPdp0FetchifConf1(0, 0, 1, strideW, strideH);
			if ((object)_latestConfActions.Pdp0FetchifConf1 == null || _latestConfActions.Pdp0FetchifConf1 != gnneActionPuPdp0FetchifConf)
			{
				_latestConfActions.Pdp0FetchifConf1 = gnneActionPuPdp0FetchifConf;
				Actions.Add(gnneActionPuPdp0FetchifConf);
			}
			Gpr gic = new Gpr(-1, length, needRenewal: false);
			GnneActionPuPdp0FetchifConf2 gnneActionPuPdp0FetchifConf2 = new GnneActionPuPdp0FetchifConf2(0, 0, 2, gic, new Gpr(-1, -1, needRenewal: false));
			if ((object)_latestConfActions.Pdp0FetchifConf2 == null || _latestConfActions.Pdp0FetchifConf2 != gnneActionPuPdp0FetchifConf2)
			{
				gic = _gprHandler.SetGprItem(length);
				gnneActionPuPdp0FetchifConf2 = new GnneActionPuPdp0FetchifConf2(0, 0, 2, gic, new Gpr(-1, -1, needRenewal: false));
				_latestConfActions.Pdp0FetchifConf2 = gnneActionPuPdp0FetchifConf2;
				Actions.Add(gnneActionPuPdp0FetchifConf2);
			}
			Ssr shape = new Ssr(-1, PackShape(1, length, length2 + padding.Sum(), length3 + padding2.Sum()), needRenewal: false);
			GnneActionPuPdp0FetchifConf3 gnneActionPuPdp0FetchifConf3 = new GnneActionPuPdp0FetchifConf3(0, 0, 3, shape);
			if ((object)_latestConfActions.Pdp0FetchifConf3 == null || _latestConfActions.Pdp0FetchifConf3 != gnneActionPuPdp0FetchifConf3)
			{
				Gpr n = _gprHandler.SetGprItem(1);
				Gpr c = _gprHandler.SetGprItem(length);
				Gpr h = _gprHandler.SetGprItem(length2 + padding.Sum());
				Gpr w = _gprHandler.SetGprItem(length3 + padding2.Sum());
				shape = _ssRegHandler.SetSsrItem(PackShape(1, length, length2 + padding.Sum(), length3 + padding2.Sum()));
				Actions.Add(new GnneActionPackShapeReg(n, c, h, w, shape));
				gnneActionPuPdp0FetchifConf3 = new GnneActionPuPdp0FetchifConf3(0, 0, 3, shape);
				_latestConfActions.Pdp0FetchifConf3 = gnneActionPuPdp0FetchifConf3;
				Actions.Add(gnneActionPuPdp0FetchifConf3);
			}
			Gpr padValue = new Gpr(-1, num3, needRenewal: false);
			Ssr pad = new Ssr(-1, PackShape(padding.Before, padding.After, padding2.Before, padding2.After), needRenewal: false);
			GnneActionPuPdp0FetchifConf4 gnneActionPuPdp0FetchifConf4 = new GnneActionPuPdp0FetchifConf4(0, 0, 4, padValue, pad);
			if ((object)_latestConfActions.Pdp0FetchifConf4 == null || _latestConfActions.Pdp0FetchifConf4 != gnneActionPuPdp0FetchifConf4)
			{
				padValue = _gprHandler.SetGprItem(num3);
				Gpr n2 = _gprHandler.SetGprItem(padding.Before);
				Gpr c2 = _gprHandler.SetGprItem(padding.After);
				Gpr h2 = _gprHandler.SetGprItem(padding2.Before);
				Gpr w2 = _gprHandler.SetGprItem(padding2.After);
				pad = _ssRegHandler.SetSsrItem(PackShape(padding.Before, padding.After, padding2.Before, padding2.After));
				Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w2, pad));
				gnneActionPuPdp0FetchifConf4 = new GnneActionPuPdp0FetchifConf4(0, 0, 4, padValue, pad);
				_latestConfActions.Pdp0FetchifConf4 = gnneActionPuPdp0FetchifConf4;
				Actions.Add(gnneActionPuPdp0FetchifConf4);
			}
			Gpr bx = new Gpr(-1, num2, needRenewal: false);
			GnneActionPuPdp0ConfDeq gnneActionPuPdp0ConfDeq = new GnneActionPuPdp0ConfDeq(0, 0, 5, bx, dataType);
			if ((object)_latestConfActions.Pdp0ConfDeq == null || _latestConfActions.Pdp0ConfDeq != gnneActionPuPdp0ConfDeq)
			{
				bx = _gprHandler.SetGprItem(num2);
				gnneActionPuPdp0ConfDeq = new GnneActionPuPdp0ConfDeq(0, 0, 5, bx, dataType);
				_latestConfActions.Pdp0ConfDeq = gnneActionPuPdp0ConfDeq;
				Actions.Add(gnneActionPuPdp0ConfDeq);
			}
			Ssr strideD = new Ssr(-1, PackStride(1, length4, length5), needRenewal: false);
			Ssr shapeD = new Ssr(-1, PackShape(1, length4, length5, length6), needRenewal: false);
			GnneActionPuPdp0OfConf gnneActionPuPdp0OfConf = new GnneActionPuPdp0OfConf(0, 0, 7, strideD, shapeD);
			if ((object)_latestConfActions.Pdp0OfConf == null || _latestConfActions.Pdp0OfConf != gnneActionPuPdp0OfConf)
			{
				Gpr n3 = _gprHandler.SetGprItem(1);
				Gpr c3 = _gprHandler.SetGprItem(length4);
				Gpr h3 = _gprHandler.SetGprItem(length5);
				Gpr w3 = _gprHandler.SetGprItem(length6);
				strideD = _ssRegHandler.SetSsrItem(PackStride(1, length4, length5));
				Actions.Add(new GnneActionPackStrideReg(n3, c3, h3, strideD));
				shapeD = _ssRegHandler.SetSsrItem(PackShape(1, length4, length5, length6));
				Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w3, shapeD));
				gnneActionPuPdp0OfConf = new GnneActionPuPdp0OfConf(0, 0, 7, strideD, shapeD);
				_latestConfActions.Pdp0OfConf = gnneActionPuPdp0OfConf;
				Actions.Add(gnneActionPuPdp0OfConf);
			}
			if (dw != null)
			{
				UpdateG2RDw(dw, l2RDw, weightGroup, ocPerGroup, offsetSDw, offsetSDwQarg, dwCcrsToClr);
			}
			if (GNNEEnv.UseCcr)
			{
				List<CcrSet> ccrsToSet = new List<CcrSet>();
				List<CcrClr> list = new List<CcrClr>();
				list?.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Psum, l1Pp))));
				UpdateCcr(ccrsToSet, list);
			}
			int value = l1Pp * GNNEEnv.PsumL1ElePerChan / 2 * 4;
			Gpr addrS = _gprHandler.SetGprItem(value);
			Actions.Add(new GnneActionPuPdp0Compute(0, addrS));
		}
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateDmLoadAct0(SegmentND l2GOf, TcuComputeMode tcuMode, bool isByChannel, ACT0_CHANNEL destChannel, DataType actType, ItemName name = ItemName.Act, int offsetS = 0, List<CcrClr> act1CcrsToClr = null)
	{
		int addr = 0;
		int value = GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(actType);
		if (isByChannel)
		{
			addr = l2GOf[1].Start * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(actType) + offsetS;
			value = l2GOf[1].Length * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(actType);
		}
		else if (tcuMode == TcuComputeMode.MatMul)
		{
			addr = l2GOf[0].Start * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(actType) + offsetS;
			value = l2GOf[0].Length * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(actType);
		}
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[name].Mmu.Id, addr));
		Gpr len = _gprHandler.SetGprItem(value);
		if (GNNEEnv.UseCcr)
		{
			List<CcrSet> ccrsToSet = new List<CcrSet>();
			UpdateCcr(ccrsToSet, act1CcrsToClr);
		}
		Actions.Add(new GnneActionDmLoadAct0(0, 0, addrS, len, destChannel, isByChannel));
	}

	public void UpdateDmStoreOf(SegmentND l2GOf, SegmentND storeOf, int ofPp, DataType ofType, ACT0_CHANNEL channel = ACT0_CHANNEL.pu, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrs_to_clr = null, int offsetD = 0, bool slice_from = true)
	{
		Ssr strideD = new Ssr(-1, PackStride(_glb.GlbMap[ItemName.Ofmap].Dimensions[1], _glb.GlbMap[ItemName.Ofmap].Dimensions[2], _glb.GlbMap[ItemName.Ofmap].Dimensions[3]), needRenewal: false);
		GnneActionDmStoreOfConf gnneActionDmStoreOfConf = new GnneActionDmStoreOfConf(0, 0, 4, strideD, ofType);
		if ((object)_latestConfActions.DmStoreOfConf == null || _latestConfActions.DmStoreOfConf != gnneActionDmStoreOfConf)
		{
			Gpr n = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[1]);
			Gpr c = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[2]);
			Gpr h = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[3]);
			strideD = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[ItemName.Ofmap].Dimensions[1], _glb.GlbMap[ItemName.Ofmap].Dimensions[2], _glb.GlbMap[ItemName.Ofmap].Dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, strideD));
			gnneActionDmStoreOfConf = new GnneActionDmStoreOfConf(0, 0, 4, strideD, ofType);
			_latestConfActions.DmStoreOfConf = gnneActionDmStoreOfConf;
			Actions.Add(gnneActionDmStoreOfConf);
		}
		Gpr n2 = _gprHandler.SetGprItem(l2GOf[0].Length);
		Gpr c2 = _gprHandler.SetGprItem(l2GOf[1].Length);
		Gpr h2 = _gprHandler.SetGprItem(l2GOf[2].Length);
		Gpr w = _gprHandler.SetGprItem(l2GOf[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(l2GOf[0].Length, l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, ssr));
		int dim = (l2GOf[0].Start - storeOf[0].Start) % storeOf[0].Length;
		int dim2 = (l2GOf[1].Start - storeOf[1].Start) % storeOf[1].Length;
		int dim3 = (l2GOf[2].Start - storeOf[2].Start) % storeOf[2].Length;
		int dim4 = (l2GOf[3].Start - storeOf[3].Start) % storeOf[3].Length;
		int num = offsetD + ofPp * _glb.GlbMap[ItemName.Ofmap].AllocatedBytes;
		if (slice_from)
		{
			num += _glb.GlbMap[ItemName.Ofmap].GetAddr(dim, dim2, dim3, dim4);
		}
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.Ofmap].Mmu.Id, num));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrs_to_clr);
		}
		int num2 = (l2GOf[0].Length - 1) * _glb.GlbMap[ItemName.Ofmap].Stride[0] + (l2GOf[1].Length - 1) * _glb.GlbMap[ItemName.Ofmap].Stride[1] + (l2GOf[2].Length - 1) * _glb.GlbMap[ItemName.Ofmap].Stride[2] + (l2GOf[3].Length - 1) * _glb.GlbMap[ItemName.Ofmap].Stride[3];
		if (num + num2 > _glb.GlbMap[ItemName.Ofmap].Mmu.Depth * 32)
		{
			Console.WriteLine($"dim0: {l2GOf[0].Length}, dim1: {l2GOf[1].Length}, dim2: {l2GOf[2].Length}, dim3: {l2GOf[3].Length}");
			Console.WriteLine($"stride_n: {_glb.GlbMap[ItemName.Ofmap].Dimensions[1]}, stride_c: {_glb.GlbMap[ItemName.Ofmap].Dimensions[2]}, stride_h: {_glb.GlbMap[ItemName.Ofmap].Dimensions[3]}");
			Console.WriteLine($"mmu_size: {_glb.GlbMap[ItemName.Ofmap].Mmu.Depth * 32}");
			Console.WriteLine($"start_addr: {offsetD},  ofmap: {ofPp}, suboffset: {_glb.GlbMap[ItemName.Ofmap].GetAddr(dim, dim2, dim3, dim4)}");
			throw new NotSupportedException("dm store exceed mmu size!");
		}
		Actions.Add(new GnneActionDmStoreOf(0, 0, addrD, ssr, channel));
	}

	public void UpdateAct0Compute(SegmentND l2GOf, int shift, int l1Pp, DataType ifType, DataType wType, DataType ofType, ACT0_OUTPUT_DEST destTarget, ACT0_CHANNEL channel, bool isByChannel, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null)
	{
		int rshiftBits = shift;
		if (channel == ACT0_CHANNEL.pu && (ifType == DataTypes.Int16 || wType == DataTypes.Int16))
		{
			rshiftBits = shift - 4;
		}
		Ssr shape = new Ssr(-1, PackShape(l2GOf[0].Length, l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length), needRenewal: false);
		GnneActionAct0Src1Conf gnneActionAct0Src1Conf = new GnneActionAct0Src1Conf(0, 0, 0, channel, shape, rshiftBits);
		if ((object)_latestConfActions.Act0Src1Conf == null || _latestConfActions.Act0Src1Conf != gnneActionAct0Src1Conf)
		{
			Gpr n = _gprHandler.SetGprItem(l2GOf[0].Length);
			Gpr c = _gprHandler.SetGprItem(l2GOf[1].Length);
			Gpr h = _gprHandler.SetGprItem(l2GOf[2].Length);
			Gpr w = _gprHandler.SetGprItem(l2GOf[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(l2GOf[0].Length, l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n, c, h, w, shape));
			gnneActionAct0Src1Conf = new GnneActionAct0Src1Conf(0, 0, 0, channel, shape, rshiftBits);
			_latestConfActions.Act0Src1Conf = gnneActionAct0Src1Conf;
			Actions.Add(gnneActionAct0Src1Conf);
		}
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		int value = l1Pp * GNNEEnv.PsumL1ElePerChan / 2 * 4;
		Gpr addrD = _gprHandler.SetGprItem(value);
		Actions.Add(new GnneActionAct0Compute(0, addrD, channel, destTarget, ofType, isByChannel));
	}

	public void UpdateStoreT(SegmentND ofmap, Call store, int ofPp, Nncase.TIR.Buffer ddrOf, int offset = 0, SegmentND ofmapStDdr = null, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, ItemName name = ItemName.Ofmap, List<int> stridesS = null, List<int> stridesD = null, GNNEShape shape = null, int[] layout = null)
	{
		Shape checkedShape = store[GNNEStore.Input].CheckedShape;
		DataType checkedDataType = store[GNNEStore.Input].CheckedDataType;
		DataType checkedDataType2 = store.CheckedDataType;
		if ((object)ofmapStDdr == null)
		{
			ofmapStDdr = ofmap;
		}
		if (stridesS == null)
		{
			stridesS = new int[3]
			{
				_glb.GlbMap[name].Dimensions[1],
				_glb.GlbMap[name].Dimensions[2],
				_glb.GlbMap[name].Dimensions[3]
			}.ToList();
		}
		if (stridesD == null)
		{
			stridesD = new int[3]
			{
				checkedShape[1].Value.Value,
				checkedShape[2].Value.Value,
				checkedShape[3].Value.Value
			}.ToList();
		}
		if (shape == null)
		{
			shape = new GNNEShape(ofmapStDdr[0].Length, ofmapStDdr[1].Length, ofmapStDdr[2].Length, ofmapStDdr[3].Length);
		}
		if (true)
		{
			ReshapeSpecialScenario(ref stridesD, ref shape, ref stridesS);
		}
		Ssr strideS = new Ssr(-1, PackStride(stridesS[0], stridesS[1], stridesS[2]), needRenewal: false);
		Ssr strideD = new Ssr(-1, PackStride(stridesD[0], stridesD[1], stridesD[2]), needRenewal: false);
		GnneActionL2StoreConf gnneActionL2StoreConf = new GnneActionL2StoreConf(strideD, strideS, checkedDataType, checkedDataType2);
		Gpr n = _gprHandler.SetGprItem(stridesS[0]);
		Gpr c = _gprHandler.SetGprItem(stridesS[1]);
		Gpr h = _gprHandler.SetGprItem(stridesS[2]);
		strideS = _ssRegHandler.SetSsrItem(PackStride(stridesS[0], stridesS[1], stridesS[2]));
		Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
		Gpr n2 = _gprHandler.SetGprItem(stridesD[0]);
		Gpr c2 = _gprHandler.SetGprItem(stridesD[1]);
		Gpr h2 = _gprHandler.SetGprItem(stridesD[2]);
		strideD = _ssRegHandler.SetSsrItem(PackStride(stridesD[0], stridesD[1], stridesD[2]));
		Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideD, (GNNEStore)store.Target));
		gnneActionL2StoreConf = new GnneActionL2StoreConf(strideD, strideS, checkedDataType, checkedDataType2);
		_latestConfActions.L2StoreConf = gnneActionL2StoreConf;
		Actions.Add(gnneActionL2StoreConf);
		Gpr n3 = _gprHandler.SetGprItem(shape[0]);
		Gpr c3 = _gprHandler.SetGprItem(shape[1]);
		Gpr h3 = _gprHandler.SetGprItem(shape[2]);
		Gpr w = _gprHandler.SetGprItem(shape[3]);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(shape[0], shape[1], shape[2], shape[3]));
		Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w, ssr));
		TileUtilities.Assert(shape[3] <= stridesS[2] || shape[2] == 1, "shape[3] <= stridesS[2] || shape[2] == 1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/ActionUpdater.cs", 1745);
		int id = _glb.GlbMap[name].Mmu.Id;
		int addr = ofPp * _glb.GlbMap[name].AllocatedBytes + offset;
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(id, addr));
		Gpr addrD = _gprHandler.SetGprItem1(ddrOf.MemSpan.Start);
		Expr gprItem = Nncase.IR.F.Buffer.BufferIndexOf(ddrOf);
		Gpr basement = _gprHandler.SetGprItem1(gprItem);
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionL2Store(basement, addrS, addrD, ssr, store, ofmapStDdr, ddrOf, store.CheckedShape.ToValueArray()));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateMfuAct1(SegmentND ifmap1, SegmentND ifmap2, SegmentND ofmap, ACT1_SOURCE_TYPE sourceType1, ACT1_SOURCE_TYPE sourceType2, DataType quantType1, DataType quantType2, DataType quantTypeD, DeQuantizeParam deqParams1 = default(DeQuantizeParam), DeQuantizeParam deqParams2 = default(DeQuantizeParam), int rshiftBits1 = 0, int rshiftBits2 = 0, int rshiftBitsD = 0, bool is16Segments = false, int iPp = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetS1 = 0, int offsetS2 = 0, int offsetD = 0, int offsetAct1 = 0, ItemName src2ItemName = ItemName.Ifmap2, ItemName src1ItemName = ItemName.Ifmap, ItemName act1Name = ItemName.MfuAct1, MFU_ACT1_FUNCTION act1Mode = MFU_ACT1_FUNCTION.add, ItemName dstItemName = ItemName.Ofmap, List<int> src1Stride = null, List<int> src2Stride = null, List<int> dstStride = null, bool isByChannel = true)
	{
		if (src1Stride == null)
		{
			src1Stride = new List<int>(3)
			{
				_glb.GlbMap[src1ItemName].Dimensions[1],
				_glb.GlbMap[src1ItemName].Dimensions[2],
				_glb.GlbMap[src1ItemName].Dimensions[3]
			};
		}
		if (src2Stride == null && _glb.GlbMap.ContainsKey(src2ItemName))
		{
			src2Stride = new List<int>(3)
			{
				_glb.GlbMap[src2ItemName].Dimensions[1],
				_glb.GlbMap[src2ItemName].Dimensions[2],
				_glb.GlbMap[src2ItemName].Dimensions[3]
			};
		}
		else if (src2Stride == null)
		{
			src2Stride = new int[3].ToList();
		}
		if (dstStride == null)
		{
			dstStride = new List<int>(3)
			{
				_glb.GlbMap[dstItemName].Dimensions[1],
				_glb.GlbMap[dstItemName].Dimensions[2],
				_glb.GlbMap[dstItemName].Dimensions[3]
			};
		}
		Ssr strideS = new Ssr(-1, PackStride(src1Stride[0], src1Stride[1], src1Stride[2]), needRenewal: false);
		Ssr strideS2 = new Ssr(-1, PackStride(src2Stride[0], src2Stride[1], src2Stride[2]), needRenewal: false);
		Ssr strideD = new Ssr(-1, PackStride(dstStride[0], dstStride[1], dstStride[2]), needRenewal: false);
		GnneActionMfuAct1ConfStride gnneActionMfuAct1ConfStride = new GnneActionMfuAct1ConfStride(8, strideS, strideS2, strideD);
		if ((object)_latestConfActions.Act1ConfStride == null || _latestConfActions.Act1ConfStride != gnneActionMfuAct1ConfStride)
		{
			Gpr n = _gprHandler.SetGprItem(src1Stride[0]);
			Gpr c = _gprHandler.SetGprItem(src1Stride[1]);
			Gpr h = _gprHandler.SetGprItem(src1Stride[2]);
			strideS = _ssRegHandler.SetSsrItem(PackStride(src1Stride[0], src1Stride[1], src1Stride[2]));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
			Gpr n2 = _gprHandler.SetGprItem(src2Stride[0]);
			Gpr c2 = _gprHandler.SetGprItem(src2Stride[1]);
			Gpr h2 = _gprHandler.SetGprItem(src2Stride[2]);
			strideS2 = _ssRegHandler.SetSsrItem(PackStride(src2Stride[0], src2Stride[1], src2Stride[2]));
			Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideS2));
			Gpr n3 = _gprHandler.SetGprItem(dstStride[0]);
			Gpr c3 = _gprHandler.SetGprItem(dstStride[1]);
			Gpr h3 = _gprHandler.SetGprItem(dstStride[2]);
			strideD = _ssRegHandler.SetSsrItem(PackStride(dstStride[0], dstStride[1], dstStride[2]));
			Actions.Add(new GnneActionPackStrideReg(n3, c3, h3, strideD));
			gnneActionMfuAct1ConfStride = new GnneActionMfuAct1ConfStride(8, strideS, strideS2, strideD);
			_latestConfActions.Act1ConfStride = gnneActionMfuAct1ConfStride;
			Actions.Add(gnneActionMfuAct1ConfStride);
		}
		int[] srcShape = new int[4]
		{
			ifmap1[0].Length,
			ifmap1[1].Length,
			ifmap1[2].Length,
			ifmap1[3].Length
		};
		int[] array = new int[4]
		{
			ifmap2[0].Length,
			ifmap2[1].Length,
			ifmap2[2].Length,
			ifmap2[3].Length
		};
		int[] destShape = new int[4]
		{
			ofmap[0].Length,
			ofmap[1].Length,
			ofmap[2].Length,
			ofmap[3].Length
		};
		TileUtilities.MfuRepeat mfuRepeat = TileUtilities.GetMfuRepeat(srcShape, destShape);
		TileUtilities.MfuRepeat mfuRepeat2 = TileUtilities.GetMfuRepeat(array, destShape);
		int sliceLoc = 1;
		Gpr slice = new Gpr(-1, mfuRepeat.SliceLen, needRenewal: false);
		Gpr rightRepeats = new Gpr(-1, mfuRepeat.ScalarRepeat, needRenewal: false);
		Gpr sliceRepeats = new Gpr(-1, mfuRepeat.SliceRepeat, needRenewal: false);
		GnneActionMfuAct1ConfSrc1 gnneActionMfuAct1ConfSrc = new GnneActionMfuAct1ConfSrc1(9, slice, rightRepeats, sliceRepeats, 0, sliceLoc);
		if ((object)_latestConfActions.Act1ConfSrc1 == null || _latestConfActions.Act1ConfSrc1 != gnneActionMfuAct1ConfSrc)
		{
			slice = _gprHandler.SetGprItem(mfuRepeat.SliceLen);
			rightRepeats = _gprHandler.SetGprItem(mfuRepeat.ScalarRepeat);
			sliceRepeats = _gprHandler.SetGprItem(mfuRepeat.SliceRepeat);
			gnneActionMfuAct1ConfSrc = new GnneActionMfuAct1ConfSrc1(9, slice, rightRepeats, sliceRepeats, 0, sliceLoc);
			_latestConfActions.Act1ConfSrc1 = gnneActionMfuAct1ConfSrc;
			Actions.Add(gnneActionMfuAct1ConfSrc);
		}
		Ssr shape = new Ssr(-1, PackShape(ifmap1[0].Length, ifmap1[1].Length, ifmap1[2].Length, ifmap1[3].Length), needRenewal: false);
		int leftRepeats = mfuRepeat.LeftRepeats;
		Gpr leftRepeats2 = new Gpr(-1, leftRepeats, needRenewal: false);
		GnneActionMfuAct1ConfSrc2 gnneActionMfuAct1ConfSrc2 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats2, shape, 0, sourceType1);
		if ((object)_latestConfActions.Act1ConfSrc2 == null || _latestConfActions.Act1ConfSrc2 != gnneActionMfuAct1ConfSrc2)
		{
			Gpr n4 = _gprHandler.SetGprItem(ifmap1[0].Length);
			Gpr c4 = _gprHandler.SetGprItem(ifmap1[1].Length);
			Gpr h4 = _gprHandler.SetGprItem(ifmap1[2].Length);
			Gpr w = _gprHandler.SetGprItem(ifmap1[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(ifmap1[0].Length, ifmap1[1].Length, ifmap1[2].Length, ifmap1[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n4, c4, h4, w, shape));
			leftRepeats2 = _gprHandler.SetGprItem(leftRepeats);
			gnneActionMfuAct1ConfSrc2 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats2, shape, 0, sourceType1);
			_latestConfActions.Act1ConfSrc2 = gnneActionMfuAct1ConfSrc2;
			Actions.Add(gnneActionMfuAct1ConfSrc2);
		}
		int sliceLoc2 = 1;
		Gpr slice2 = new Gpr(-1, mfuRepeat2.SliceLen, needRenewal: false);
		Gpr rightRepeats2 = new Gpr(-1, mfuRepeat2.ScalarRepeat, needRenewal: false);
		Gpr sliceRepeats2 = new Gpr(-1, mfuRepeat2.SliceRepeat, needRenewal: false);
		GnneActionMfuAct1ConfSrc1 gnneActionMfuAct1ConfSrc3 = new GnneActionMfuAct1ConfSrc1(9, slice2, rightRepeats2, sliceRepeats2, 1, sliceLoc2);
		if ((object)_latestConfActions.Act1ConfSrc1 == null || _latestConfActions.Act1ConfSrc1 != gnneActionMfuAct1ConfSrc3)
		{
			slice2 = _gprHandler.SetGprItem(mfuRepeat2.SliceLen);
			rightRepeats2 = _gprHandler.SetGprItem(mfuRepeat2.ScalarRepeat);
			sliceRepeats2 = _gprHandler.SetGprItem(mfuRepeat2.SliceRepeat);
			gnneActionMfuAct1ConfSrc3 = new GnneActionMfuAct1ConfSrc1(9, slice2, rightRepeats2, sliceRepeats2, 1, sliceLoc2);
			_latestConfActions.Act1ConfSrc1 = gnneActionMfuAct1ConfSrc3;
			Actions.Add(gnneActionMfuAct1ConfSrc3);
		}
		shape = new Ssr(-1, PackShape(ifmap2[0].Length, ifmap2[1].Length, ifmap2[2].Length, ifmap2[3].Length), needRenewal: false);
		int leftRepeats3 = mfuRepeat2.LeftRepeats;
		Gpr leftRepeats4 = new Gpr(-1, leftRepeats3, needRenewal: false);
		GnneActionMfuAct1ConfSrc2 gnneActionMfuAct1ConfSrc4 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats4, shape, 1, sourceType2);
		if ((object)_latestConfActions.Act1ConfSrc2 == null || _latestConfActions.Act1ConfSrc2 != gnneActionMfuAct1ConfSrc4)
		{
			Gpr n5 = _gprHandler.SetGprItem(ifmap2[0].Length);
			Gpr c5 = _gprHandler.SetGprItem(ifmap2[1].Length);
			Gpr h5 = _gprHandler.SetGprItem(ifmap2[2].Length);
			Gpr w2 = _gprHandler.SetGprItem(ifmap2[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(ifmap2[0].Length, ifmap2[1].Length, ifmap2[2].Length, ifmap2[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n5, c5, h5, w2, shape));
			leftRepeats4 = _gprHandler.SetGprItem(leftRepeats3);
			gnneActionMfuAct1ConfSrc4 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats4, shape, 1, sourceType2);
			_latestConfActions.Act1ConfSrc2 = gnneActionMfuAct1ConfSrc4;
			Actions.Add(gnneActionMfuAct1ConfSrc4);
		}
		shape = new Ssr(-1, PackShape(ofmap[0].Length, ofmap[1].Length, ofmap[2].Length, ofmap[3].Length), needRenewal: false);
		int shape_size = ofmap.Shape_size;
		Gpr len = new Gpr(-1, shape_size, needRenewal: false);
		GnneActionMfuAct1ConfDest gnneActionMfuAct1ConfDest = new GnneActionMfuAct1ConfDest(11, len, shape);
		if ((object)_latestConfActions.Act1ConfDest == null || _latestConfActions.Act1ConfDest != gnneActionMfuAct1ConfDest)
		{
			Gpr n6 = _gprHandler.SetGprItem(ofmap[0].Length);
			Gpr c6 = _gprHandler.SetGprItem(ofmap[1].Length);
			Gpr h6 = _gprHandler.SetGprItem(ofmap[2].Length);
			Gpr w3 = _gprHandler.SetGprItem(ofmap[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(ofmap[0].Length, ofmap[1].Length, ofmap[2].Length, ofmap[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n6, c6, h6, w3, shape));
			len = _gprHandler.SetGprItem(shape_size);
			gnneActionMfuAct1ConfDest = new GnneActionMfuAct1ConfDest(11, len, shape);
			_latestConfActions.Act1ConfDest = gnneActionMfuAct1ConfDest;
			Actions.Add(gnneActionMfuAct1ConfDest);
		}
		int num = BitConverter.ToInt16(BitConverter.GetBytes((Half)deqParams1.Scale), 0);
		int zeroPoint = deqParams1.ZeroPoint;
		Gpr scale = new Gpr(-1, num, needRenewal: false);
		Gpr bias = new Gpr(-1, zeroPoint, needRenewal: false);
		GnneActionMfuAct1ConfDeq gnneActionMfuAct1ConfDeq = new GnneActionMfuAct1ConfDeq(12, scale, bias, quantType1, 0, rshiftBits1);
		if ((object)_latestConfActions.Act1ConfDeq == null || _latestConfActions.Act1ConfDeq != gnneActionMfuAct1ConfDeq)
		{
			scale = _gprHandler.SetGprItem(num);
			bias = _gprHandler.SetGprItem(zeroPoint);
			gnneActionMfuAct1ConfDeq = new GnneActionMfuAct1ConfDeq(12, scale, bias, quantType1, 0, rshiftBits1);
			_latestConfActions.Act1ConfDeq = gnneActionMfuAct1ConfDeq;
			Actions.Add(gnneActionMfuAct1ConfDeq);
		}
		int num2 = BitConverter.ToInt16(BitConverter.GetBytes((Half)deqParams2.Scale), 0);
		int zeroPoint2 = deqParams2.ZeroPoint;
		Gpr scale2 = new Gpr(-1, num2, needRenewal: false);
		Gpr bias2 = new Gpr(-1, zeroPoint2, needRenewal: false);
		GnneActionMfuAct1ConfDeq gnneActionMfuAct1ConfDeq2 = new GnneActionMfuAct1ConfDeq(12, scale2, bias2, quantType2, 1, rshiftBits2);
		if ((object)_latestConfActions.Act1ConfDeq == null || _latestConfActions.Act1ConfDeq != gnneActionMfuAct1ConfDeq2)
		{
			scale2 = _gprHandler.SetGprItem(num2);
			bias2 = _gprHandler.SetGprItem(zeroPoint2);
			gnneActionMfuAct1ConfDeq2 = new GnneActionMfuAct1ConfDeq(12, scale2, bias2, quantType2, 1, rshiftBits2);
			_latestConfActions.Act1ConfDeq = gnneActionMfuAct1ConfDeq2;
			Actions.Add(gnneActionMfuAct1ConfDeq2);
		}
		GnneActionMfuAct1ConfQuant gnneActionMfuAct1ConfQuant = new GnneActionMfuAct1ConfQuant(13, quantTypeD, rshiftBitsD);
		_latestConfActions.Act1ConfQuant = gnneActionMfuAct1ConfQuant;
		Actions.Add(gnneActionMfuAct1ConfQuant);
		GnneActionMfuAct1Conf gnneActionMfuAct1Conf = new GnneActionMfuAct1Conf(14, act1Mode, !is16Segments && isByChannel, is16Segments);
		if ((object)_latestConfActions.Act1Conf == null || _latestConfActions.Act1Conf != gnneActionMfuAct1Conf)
		{
			_latestConfActions.Act1Conf = gnneActionMfuAct1Conf;
			Actions.Add(gnneActionMfuAct1Conf);
		}
		int addr = iPp * _glb.GlbMap[dstItemName].AllocatedBytes + offsetD;
		int addr2 = iPp * _glb.GlbMap[src1ItemName].AllocatedBytes + offsetS1;
		int id = _glb.GlbMap[src1ItemName].Mmu.Id;
		if (src1ItemName != ItemName.Ifmap)
		{
			addr2 = offsetS1;
			id = _glb.GlbMap[src1ItemName].Mmu.Id;
		}
		int addr3 = 0;
		int mmuItem = 0;
		if (array.Any((int x) => x != 0))
		{
			addr3 = offsetS2 + iPp * _glb.GlbMap[src2ItemName].AllocatedBytes;
			mmuItem = _glb.GlbMap[src2ItemName].Mmu.Id;
		}
		int addr4 = offsetAct1;
		int id2 = _glb.GlbMap[dstItemName].Mmu.Id;
		if (dstItemName != ItemName.Ofmap)
		{
			addr = offsetD;
			id2 = _glb.GlbMap[dstItemName].Mmu.Id;
		}
		if (!is16Segments)
		{
			addr4 = ofmap[1].Start * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(DataTypes.Float16) + offsetAct1;
		}
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(id2, addr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(id, addr2));
		Gpr addrS2 = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr3));
		Gpr addrArg = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[act1Name].Mmu.Id, addr4));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionMfuAct1Compute(addrD, addrS, addrS2, addrArg));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateMfuAct1InFuse(SegmentND l2RPsum, SegmentND l2RIf2, SegmentND ifmap2, SegmentND l2GOf, SegmentND ofmap, ACT1_SOURCE_TYPE sourceType1, ACT1_SOURCE_TYPE sourceType2, DataType quantType1, DataType quantType2, DataType quantTypeD, DeQuantizeParam deqParams1, DeQuantizeParam deqParams2, int rshiftBits1, int rshiftBits2, int rshiftBitsD, bool is16Segments, int ofPp, int l1Pp, List<CcrClr> l2RIf2CcrsToClr = null, List<CcrSet> ofCcrsToSet = null, List<CcrClr> ofCcrsToClr = null, int offsetSSrc2 = 0, int offsetAct1 = 0, int offsetOfmap = 0, ItemName src2ItemName = ItemName.Ifmap2, List<CcrClr> act1CcrsToClr = null, MFU_ACT1_FUNCTION actType = MFU_ACT1_FUNCTION.add)
	{
		Ssr ssr = new Ssr(-1, PackStride(_glb.GlbMap[ItemName.Ifmap2].Dimensions[1], _glb.GlbMap[ItemName.Ifmap2].Dimensions[2], _glb.GlbMap[ItemName.Ifmap2].Dimensions[3]), needRenewal: false);
		Ssr strideD = new Ssr(-1, PackStride(_glb.GlbMap[ItemName.Ofmap].Dimensions[1], _glb.GlbMap[ItemName.Ofmap].Dimensions[2], _glb.GlbMap[ItemName.Ofmap].Dimensions[3]), needRenewal: false);
		GnneActionMfuAct1ConfStride gnneActionMfuAct1ConfStride = new GnneActionMfuAct1ConfStride(8, ssr, ssr, strideD);
		if ((object)_latestConfActions.Act1ConfStride == null || _latestConfActions.Act1ConfStride != gnneActionMfuAct1ConfStride)
		{
			Gpr n = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ifmap2].Dimensions[1]);
			Gpr c = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ifmap2].Dimensions[2]);
			Gpr h = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ifmap2].Dimensions[3]);
			ssr = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[ItemName.Ifmap2].Dimensions[1], _glb.GlbMap[ItemName.Ifmap2].Dimensions[2], _glb.GlbMap[ItemName.Ifmap2].Dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, ssr));
			Gpr n2 = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[1]);
			Gpr c2 = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[2]);
			Gpr h2 = _gprHandler.SetGprItem(_glb.GlbMap[ItemName.Ofmap].Dimensions[3]);
			strideD = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[ItemName.Ofmap].Dimensions[1], _glb.GlbMap[ItemName.Ofmap].Dimensions[2], _glb.GlbMap[ItemName.Ofmap].Dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideD));
			gnneActionMfuAct1ConfStride = new GnneActionMfuAct1ConfStride(8, ssr, ssr, strideD);
			_latestConfActions.Act1ConfStride = gnneActionMfuAct1ConfStride;
			Actions.Add(gnneActionMfuAct1ConfStride);
		}
		int[] srcShape = new int[4]
		{
			l2RPsum[0].Length,
			l2RPsum[1].Length,
			l2RPsum[2].Length,
			l2RPsum[3].Length
		};
		int[] array = new int[4]
		{
			l2RIf2[0].Length,
			l2RIf2[1].Length,
			l2RIf2[2].Length,
			l2RIf2[3].Length
		};
		int[] destShape = new int[4]
		{
			l2GOf[0].Length,
			l2GOf[1].Length,
			l2GOf[2].Length,
			l2GOf[3].Length
		};
		TileUtilities.MfuRepeat mfuRepeat = TileUtilities.GetMfuRepeat(srcShape, destShape);
		TileUtilities.MfuRepeat mfuRepeat2 = TileUtilities.GetMfuRepeat(array, destShape);
		int sliceLoc = 0;
		Gpr slice = new Gpr(-1, mfuRepeat.SliceLen, needRenewal: false);
		Gpr rightRepeats = new Gpr(-1, mfuRepeat.ScalarRepeat, needRenewal: false);
		Gpr sliceRepeats = new Gpr(-1, mfuRepeat.SliceRepeat, needRenewal: false);
		GnneActionMfuAct1ConfSrc1 gnneActionMfuAct1ConfSrc = new GnneActionMfuAct1ConfSrc1(9, slice, rightRepeats, sliceRepeats, 0, sliceLoc);
		if ((object)_latestConfActions.Act1ConfSrc1 == null || _latestConfActions.Act1ConfSrc1 != gnneActionMfuAct1ConfSrc)
		{
			slice = _gprHandler.SetGprItem(mfuRepeat.SliceLen);
			rightRepeats = _gprHandler.SetGprItem(mfuRepeat.ScalarRepeat);
			sliceRepeats = _gprHandler.SetGprItem(mfuRepeat.SliceRepeat);
			gnneActionMfuAct1ConfSrc = new GnneActionMfuAct1ConfSrc1(9, slice, rightRepeats, sliceRepeats, 0, sliceLoc);
			_latestConfActions.Act1ConfSrc1 = gnneActionMfuAct1ConfSrc;
			Actions.Add(gnneActionMfuAct1ConfSrc);
		}
		Ssr shape = new Ssr(-1, PackShape(l2RPsum[0].Length, l2RPsum[1].Length, l2RPsum[2].Length, l2RPsum[3].Length), needRenewal: false);
		int leftRepeats = mfuRepeat.LeftRepeats;
		Gpr leftRepeats2 = new Gpr(-1, leftRepeats, needRenewal: false);
		GnneActionMfuAct1ConfSrc2 gnneActionMfuAct1ConfSrc2 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats2, shape, 0, sourceType1);
		if ((object)_latestConfActions.Act1ConfSrc2 == null || _latestConfActions.Act1ConfSrc2 != gnneActionMfuAct1ConfSrc2)
		{
			Gpr n3 = _gprHandler.SetGprItem(l2RPsum[0].Length);
			Gpr c3 = _gprHandler.SetGprItem(l2RPsum[1].Length);
			Gpr h3 = _gprHandler.SetGprItem(l2RPsum[2].Length);
			Gpr w = _gprHandler.SetGprItem(l2RPsum[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(l2RPsum[0].Length, l2RPsum[1].Length, l2RPsum[2].Length, l2RPsum[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w, shape));
			leftRepeats2 = _gprHandler.SetGprItem(leftRepeats);
			gnneActionMfuAct1ConfSrc2 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats2, shape, 0, sourceType1);
			_latestConfActions.Act1ConfSrc2 = gnneActionMfuAct1ConfSrc2;
			Actions.Add(gnneActionMfuAct1ConfSrc2);
		}
		int sliceLoc2 = 1;
		Gpr slice2 = new Gpr(-1, mfuRepeat2.SliceLen, needRenewal: false);
		Gpr rightRepeats2 = new Gpr(-1, mfuRepeat2.ScalarRepeat, needRenewal: false);
		Gpr sliceRepeats2 = new Gpr(-1, mfuRepeat2.SliceRepeat, needRenewal: false);
		GnneActionMfuAct1ConfSrc1 gnneActionMfuAct1ConfSrc3 = new GnneActionMfuAct1ConfSrc1(9, slice2, rightRepeats2, sliceRepeats2, 1, sliceLoc2);
		if ((object)_latestConfActions.Act1ConfSrc1 == null || _latestConfActions.Act1ConfSrc1 != gnneActionMfuAct1ConfSrc3)
		{
			slice2 = _gprHandler.SetGprItem(mfuRepeat2.SliceLen);
			rightRepeats2 = _gprHandler.SetGprItem(mfuRepeat2.ScalarRepeat);
			sliceRepeats2 = _gprHandler.SetGprItem(mfuRepeat2.SliceRepeat);
			gnneActionMfuAct1ConfSrc3 = new GnneActionMfuAct1ConfSrc1(9, slice2, rightRepeats2, sliceRepeats2, 1, sliceLoc2);
			_latestConfActions.Act1ConfSrc1 = gnneActionMfuAct1ConfSrc3;
			Actions.Add(gnneActionMfuAct1ConfSrc3);
		}
		shape = new Ssr(-1, PackShape(l2RIf2[0].Length, l2RIf2[1].Length, l2RIf2[2].Length, l2RIf2[3].Length), needRenewal: false);
		int leftRepeats3 = mfuRepeat2.LeftRepeats;
		Gpr leftRepeats4 = new Gpr(-1, leftRepeats3, needRenewal: false);
		GnneActionMfuAct1ConfSrc2 gnneActionMfuAct1ConfSrc4 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats4, shape, 1, sourceType2);
		if ((object)_latestConfActions.Act1ConfSrc2 == null || _latestConfActions.Act1ConfSrc2 != gnneActionMfuAct1ConfSrc4)
		{
			Gpr n4 = _gprHandler.SetGprItem(l2RIf2[0].Length);
			Gpr c4 = _gprHandler.SetGprItem(l2RIf2[1].Length);
			Gpr h4 = _gprHandler.SetGprItem(l2RIf2[2].Length);
			Gpr w2 = _gprHandler.SetGprItem(l2RIf2[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(l2RIf2[0].Length, l2RIf2[1].Length, l2RIf2[2].Length, l2RIf2[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n4, c4, h4, w2, shape));
			leftRepeats4 = _gprHandler.SetGprItem(leftRepeats3);
			gnneActionMfuAct1ConfSrc4 = new GnneActionMfuAct1ConfSrc2(10, leftRepeats4, shape, 1, sourceType2);
			_latestConfActions.Act1ConfSrc2 = gnneActionMfuAct1ConfSrc4;
			Actions.Add(gnneActionMfuAct1ConfSrc4);
		}
		shape = new Ssr(-1, PackShape(l2GOf[0].Length, l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length), needRenewal: false);
		int shape_size = l2GOf.Shape_size;
		Gpr len = new Gpr(-1, shape_size, needRenewal: false);
		GnneActionMfuAct1ConfDest gnneActionMfuAct1ConfDest = new GnneActionMfuAct1ConfDest(11, len, shape);
		if ((object)_latestConfActions.Act1ConfDest == null || _latestConfActions.Act1ConfDest != gnneActionMfuAct1ConfDest)
		{
			Gpr n5 = _gprHandler.SetGprItem(l2GOf[0].Length);
			Gpr c5 = _gprHandler.SetGprItem(l2GOf[1].Length);
			Gpr h5 = _gprHandler.SetGprItem(l2GOf[2].Length);
			Gpr w3 = _gprHandler.SetGprItem(l2GOf[3].Length);
			shape = _ssRegHandler.SetSsrItem(PackShape(l2GOf[0].Length, l2GOf[1].Length, l2GOf[2].Length, l2GOf[3].Length));
			Actions.Add(new GnneActionPackShapeReg(n5, c5, h5, w3, shape));
			len = _gprHandler.SetGprItem(shape_size);
			gnneActionMfuAct1ConfDest = new GnneActionMfuAct1ConfDest(11, len, shape);
			_latestConfActions.Act1ConfDest = gnneActionMfuAct1ConfDest;
			Actions.Add(gnneActionMfuAct1ConfDest);
		}
		int num = BitConverter.ToInt16(BitConverter.GetBytes((Half)deqParams1.Scale), 0);
		int zeroPoint = deqParams1.ZeroPoint;
		Gpr scale = new Gpr(-1, num, needRenewal: false);
		Gpr bias = new Gpr(-1, zeroPoint, needRenewal: false);
		GnneActionMfuAct1ConfDeq gnneActionMfuAct1ConfDeq = new GnneActionMfuAct1ConfDeq(12, scale, bias, quantType1, 0, rshiftBits1);
		if ((object)_latestConfActions.Act1ConfDeq == null || _latestConfActions.Act1ConfDeq != gnneActionMfuAct1ConfDeq)
		{
			scale = _gprHandler.SetGprItem(num);
			bias = _gprHandler.SetGprItem(zeroPoint);
			gnneActionMfuAct1ConfDeq = new GnneActionMfuAct1ConfDeq(12, scale, bias, quantType1, 0, rshiftBits1);
			_latestConfActions.Act1ConfDeq = gnneActionMfuAct1ConfDeq;
			Actions.Add(gnneActionMfuAct1ConfDeq);
		}
		int num2 = BitConverter.ToInt16(BitConverter.GetBytes((Half)deqParams2.Scale), 0);
		int zeroPoint2 = deqParams2.ZeroPoint;
		Gpr scale2 = new Gpr(-1, num2, needRenewal: false);
		Gpr bias2 = new Gpr(-1, zeroPoint2, needRenewal: false);
		GnneActionMfuAct1ConfDeq gnneActionMfuAct1ConfDeq2 = new GnneActionMfuAct1ConfDeq(12, scale2, bias2, quantType2, 1, rshiftBits2);
		if ((object)_latestConfActions.Act1ConfDeq == null || _latestConfActions.Act1ConfDeq != gnneActionMfuAct1ConfDeq2)
		{
			scale2 = _gprHandler.SetGprItem(num2);
			bias2 = _gprHandler.SetGprItem(zeroPoint2);
			gnneActionMfuAct1ConfDeq2 = new GnneActionMfuAct1ConfDeq(12, scale2, bias2, quantType2, 1, rshiftBits2);
			_latestConfActions.Act1ConfDeq = gnneActionMfuAct1ConfDeq2;
			Actions.Add(gnneActionMfuAct1ConfDeq2);
		}
		GnneActionMfuAct1ConfQuant gnneActionMfuAct1ConfQuant = new GnneActionMfuAct1ConfQuant(13, quantTypeD, rshiftBitsD);
		if ((object)_latestConfActions.Act1ConfQuant == null || _latestConfActions.Act1ConfQuant != gnneActionMfuAct1ConfQuant)
		{
			_latestConfActions.Act1ConfQuant = gnneActionMfuAct1ConfQuant;
			Actions.Add(gnneActionMfuAct1ConfQuant);
		}
		GnneActionMfuAct1Conf gnneActionMfuAct1Conf = new GnneActionMfuAct1Conf(14, actType, !is16Segments, is16Segments);
		if ((object)_latestConfActions.Act1Conf == null || _latestConfActions.Act1Conf != gnneActionMfuAct1Conf)
		{
			_latestConfActions.Act1Conf = gnneActionMfuAct1Conf;
			Actions.Add(gnneActionMfuAct1Conf);
		}
		if (GNNEEnv.UseCcr)
		{
			if (array.Any((int x) => x != 0))
			{
				ofCcrsToClr.AddRange(l2RIf2CcrsToClr);
			}
			CcrClr item = new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Psum, l1Pp)));
			ofCcrsToClr.Add(item);
			if (act1CcrsToClr != null)
			{
				ofCcrsToClr.AddRange(act1CcrsToClr);
			}
			UpdateCcr(ofCcrsToSet, ofCcrsToClr);
		}
		int dim = (l2GOf[0].Start - ofmap[0].Start) % ofmap[0].Length;
		int dim2 = (l2GOf[1].Start - ofmap[1].Start) % ofmap[1].Length;
		int dim3 = (l2GOf[2].Start - ofmap[2].Start) % ofmap[2].Length;
		int dim4 = (l2GOf[3].Start - ofmap[3].Start) % ofmap[3].Length;
		int addr = offsetOfmap + ofPp * _glb.GlbMap[ItemName.Ofmap].AllocatedBytes + _glb.GlbMap[ItemName.Ofmap].GetAddr(dim, dim2, dim3, dim4);
		int addr2 = l1Pp * GNNEEnv.PsumL1ElePerChan / 2 * 4;
		int addr3 = 0;
		int mmuItem = 0;
		int mmuItem2 = 0;
		if (array.Any((int x) => x != 0))
		{
			dim = (l2RIf2[0].Start - ifmap2[0].Start) % ifmap2[0].Length;
			dim2 = (l2RIf2[1].Start - ifmap2[1].Start) % ifmap2[1].Length;
			dim3 = (l2RIf2[2].Start - ifmap2[2].Start) % ifmap2[2].Length;
			dim4 = (l2RIf2[3].Start - ifmap2[3].Start) % ifmap2[3].Length;
			addr3 = offsetSSrc2 + ofPp * _glb.GlbMap[ItemName.Ifmap2].AllocatedBytes + _glb.GlbMap[ItemName.Ifmap2].GetAddr(dim, dim2, dim3, dim4);
			mmuItem2 = _glb.GlbMap[src2ItemName].Mmu.Id;
		}
		int addr4 = (is16Segments ? offsetAct1 : (l2GOf[1].Start * GNNEEnv.ActNumPerChan * TileUtilities.GetBytesPerElement(DataTypes.Float16) + offsetAct1));
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.Ofmap].Mmu.Id, addr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr2));
		Gpr addrS2 = _gprHandler.SetGprItem(ToGlbAddr(mmuItem2, addr3));
		Gpr addrArg = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.MfuAct1].Mmu.Id, addr4));
		Actions.Add(new GnneActionMfuAct1Compute(addrD, addrS, addrS2, addrArg));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateMfuTranspose(SegmentND ifmap, SegmentND ofmap, DataType l2Datatype, MFU_TRANS_PERMUTE perm, int iPp, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetS = 0, int offsetD = 0, ItemName ifName = ItemName.Ifmap, ItemName ofName = ItemName.Ofmap, int of_pp = -1)
	{
		Ssr strideS = new Ssr(-1, PackStride(_glb.GlbMap[ifName].Dimensions[1], _glb.GlbMap[ifName].Dimensions[2], _glb.GlbMap[ifName].Dimensions[3]), needRenewal: false);
		Ssr strideD = new Ssr(-1, PackStride(_glb.GlbMap[ofName].Dimensions[1], _glb.GlbMap[ofName].Dimensions[2], _glb.GlbMap[ofName].Dimensions[3]), needRenewal: false);
		GnneActionMfuTransposeConf gnneActionMfuTransposeConf = new GnneActionMfuTransposeConf(0, strideD, strideS, l2Datatype, perm);
		if ((object)_latestConfActions.TransposeConf == null || _latestConfActions.TransposeConf != gnneActionMfuTransposeConf)
		{
			Gpr n = _gprHandler.SetGprItem(_glb.GlbMap[ifName].Dimensions[1]);
			Gpr c = _gprHandler.SetGprItem(_glb.GlbMap[ifName].Dimensions[2]);
			Gpr h = _gprHandler.SetGprItem(_glb.GlbMap[ifName].Dimensions[3]);
			strideS = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[ifName].Dimensions[1], _glb.GlbMap[ifName].Dimensions[2], _glb.GlbMap[ifName].Dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
			Gpr n2 = _gprHandler.SetGprItem(_glb.GlbMap[ofName].Dimensions[1]);
			Gpr c2 = _gprHandler.SetGprItem(_glb.GlbMap[ofName].Dimensions[2]);
			Gpr h2 = _gprHandler.SetGprItem(_glb.GlbMap[ofName].Dimensions[3]);
			strideD = _ssRegHandler.SetSsrItem(PackStride(_glb.GlbMap[ofName].Dimensions[1], _glb.GlbMap[ofName].Dimensions[2], _glb.GlbMap[ofName].Dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideD));
			gnneActionMfuTransposeConf = new GnneActionMfuTransposeConf(0, strideD, strideS, l2Datatype, perm);
			_latestConfActions.TransposeConf = gnneActionMfuTransposeConf;
			Actions.Add(gnneActionMfuTransposeConf);
		}
		Gpr n3 = _gprHandler.SetGprItem(ifmap[0].Length);
		Gpr c3 = _gprHandler.SetGprItem(ifmap[1].Length);
		Gpr h3 = _gprHandler.SetGprItem(ifmap[2].Length);
		Gpr w = _gprHandler.SetGprItem(ifmap[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(ifmap[0].Length, ifmap[1].Length, ifmap[2].Length, ifmap[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w, ssr));
		if (of_pp == -1)
		{
			of_pp = iPp;
		}
		int addr = of_pp * _glb.GlbMap[ofName].AllocatedBytes + offsetD;
		int addr2 = iPp * _glb.GlbMap[ifName].AllocatedBytes + offsetS;
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ofName].Mmu.Id, addr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ifName].Mmu.Id, addr2));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionMfuTranspose(addrD, addrS, ssr));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateMfuPdp1(Call pdp, SegmentND ifmap, SegmentND ofmap, int iPp, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetS = 0, int offsetD = 0)
	{
		int strideH = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int strideW = ((TensorConst)pdp[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int num = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)pdp[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
		DataType checkedDataType = pdp[GNNEPdp1.Input].CheckedDataType;
		DataType checkedDataType2 = pdp.CheckedDataType;
		PDP_FUNCTION funct = PdpFunc(((GNNEPdp1)pdp.Target).ReduceOp);
		ReadOnlySpan<int> dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
		int n = dimensions[1];
		dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
		int c = dimensions[2];
		dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
		Ssr strideS = new Ssr(-1, PackStride(n, c, dimensions[3]), needRenewal: false);
		dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
		int n2 = dimensions[1];
		dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
		int c2 = dimensions[2];
		dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
		Ssr strideD = new Ssr(-1, PackStride(n2, c2, dimensions[3]), needRenewal: false);
		dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
		_ = ref dimensions[3];
		GnneActionMfuPdp1Conf1 gnneActionMfuPdp1Conf = new GnneActionMfuPdp1Conf1(1, strideW, strideH, strideD, strideS, funct);
		if ((object)_latestConfActions.Pdp1Conf1 == null || _latestConfActions.Pdp1Conf1 != gnneActionMfuPdp1Conf)
		{
			GprHandler gprHandler = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			Gpr n3 = gprHandler.SetGprItem(dimensions[1]);
			GprHandler gprHandler2 = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			Gpr c3 = gprHandler2.SetGprItem(dimensions[2]);
			GprHandler gprHandler3 = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			Gpr h = gprHandler3.SetGprItem(dimensions[3]);
			SsrHandler ssRegHandler = _ssRegHandler;
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			int n4 = dimensions[1];
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			int c4 = dimensions[2];
			dimensions = _glb.GlbMap[ItemName.Ifmap].Dimensions;
			strideS = ssRegHandler.SetSsrItem(PackStride(n4, c4, dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n3, c3, h, strideS));
			GprHandler gprHandler4 = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			Gpr n5 = gprHandler4.SetGprItem(dimensions[1]);
			GprHandler gprHandler5 = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			Gpr c5 = gprHandler5.SetGprItem(dimensions[2]);
			GprHandler gprHandler6 = _gprHandler;
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			Gpr h2 = gprHandler6.SetGprItem(dimensions[3]);
			SsrHandler ssRegHandler2 = _ssRegHandler;
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			int n6 = dimensions[1];
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			int c6 = dimensions[2];
			dimensions = _glb.GlbMap[ItemName.Ofmap].Dimensions;
			strideD = ssRegHandler2.SetSsrItem(PackStride(n6, c6, dimensions[3]));
			Actions.Add(new GnneActionPackStrideReg(n5, c5, h2, strideD));
			gnneActionMfuPdp1Conf = new GnneActionMfuPdp1Conf1(1, strideW, strideH, strideD, strideS, funct);
			_latestConfActions.Pdp1Conf1 = gnneActionMfuPdp1Conf;
			Actions.Add(gnneActionMfuPdp1Conf);
		}
		int length = ofmap[3].Length;
		int length2 = ofmap[2].Length;
		int num3 = System.Math.Min(GNNEEnv.MfuPuHeight, ifmap[1].Length * num) / num;
		int num4 = num * num3;
		int num5 = num3;
		if (ifmap[1].Length % num3 != 0)
		{
			num5 = ifmap[1].Length % num3;
		}
		int num6 = num * num5;
		int num7 = BitConverter.ToInt16(BitConverter.GetBytes((Half)((TensorConst)pdp[GNNEPdp1.Value]).Value.ToScalar<float>()), 0);
		Gpr countW = new Gpr(-1, length, needRenewal: false);
		Gpr countH = new Gpr(-1, length2, needRenewal: false);
		Gpr peH = new Gpr(-1, num4, needRenewal: false);
		Gpr peLastH = new Gpr(-1, num6, needRenewal: false);
		GnneActionMfuPdp1Conf2 gnneActionMfuPdp1Conf2 = new GnneActionMfuPdp1Conf2(2, countW, countH, peH, peLastH);
		if ((object)_latestConfActions.Pdp1Conf2 == null || _latestConfActions.Pdp1Conf2 != gnneActionMfuPdp1Conf2)
		{
			countW = _gprHandler.SetGprItem(length);
			countH = _gprHandler.SetGprItem(length2);
			peH = _gprHandler.SetGprItem(num4);
			peLastH = _gprHandler.SetGprItem(num6);
			gnneActionMfuPdp1Conf2 = new GnneActionMfuPdp1Conf2(2, countW, countH, peH, peLastH);
			_latestConfActions.Pdp1Conf2 = gnneActionMfuPdp1Conf2;
			Actions.Add(gnneActionMfuPdp1Conf2);
		}
		Ssr pad = new Ssr(-1, PackShape(ifmap.PadH.Before, ifmap.PadH.After, ifmap.PadW.Before, ifmap.PadW.After), needRenewal: false);
		Gpr peChannels = new Gpr(-1, num3, needRenewal: false);
		Gpr peLastChannels = new Gpr(-1, num5, needRenewal: false);
		Gpr padValue = new Gpr(-1, num7, needRenewal: false);
		GnneActionMfuPdp1Conf3 gnneActionMfuPdp1Conf3 = new GnneActionMfuPdp1Conf3(3, peChannels, peLastChannels, padValue, pad);
		if ((object)_latestConfActions.Pdp1Conf3 == null || _latestConfActions.Pdp1Conf3 != gnneActionMfuPdp1Conf3)
		{
			Gpr n7 = _gprHandler.SetGprItem(ifmap.PadH.Before);
			Gpr c7 = _gprHandler.SetGprItem(ifmap.PadH.After);
			Gpr h3 = _gprHandler.SetGprItem(ifmap.PadW.Before);
			Gpr w = _gprHandler.SetGprItem(ifmap.PadW.After);
			pad = _ssRegHandler.SetSsrItem(PackShape(ifmap.PadH.Before, ifmap.PadH.After, ifmap.PadW.Before, ifmap.PadW.After));
			Actions.Add(new GnneActionPackShapeReg(n7, c7, h3, w, pad));
			peChannels = _gprHandler.SetGprItem(num3);
			peLastChannels = _gprHandler.SetGprItem(num5);
			padValue = _gprHandler.SetGprItem(num7);
			gnneActionMfuPdp1Conf3 = new GnneActionMfuPdp1Conf3(3, peChannels, peLastChannels, padValue, pad);
			_latestConfActions.Pdp1Conf3 = gnneActionMfuPdp1Conf3;
			Actions.Add(gnneActionMfuPdp1Conf3);
		}
		Gpr windowW = new Gpr(-1, num2, needRenewal: false);
		Gpr windowH = new Gpr(-1, num, needRenewal: false);
		Half value = (Half)1.0;
		Gpr scale = new Gpr(-1, BitConverter.ToInt16(BitConverter.GetBytes(value), 0), needRenewal: false);
		bool enableH2C = false;
		bool enableBw = false;
		GnneActionMfuPdp1Conf4 gnneActionMfuPdp1Conf4 = new GnneActionMfuPdp1Conf4(4, windowW, windowH, scale, enableH2C, enableBw);
		if ((object)_latestConfActions.Pdp1Conf4 == null || _latestConfActions.Pdp1Conf4 != gnneActionMfuPdp1Conf4)
		{
			windowW = _gprHandler.SetGprItem(num2);
			windowH = _gprHandler.SetGprItem(num);
			scale = _gprHandler.SetGprItem(BitConverter.ToInt16(BitConverter.GetBytes(value), 0));
			gnneActionMfuPdp1Conf4 = new GnneActionMfuPdp1Conf4(4, windowW, windowH, scale, enableH2C, enableBw);
			_latestConfActions.Pdp1Conf4 = gnneActionMfuPdp1Conf4;
			Actions.Add(gnneActionMfuPdp1Conf4);
		}
		DeQuantizeParam deQuantizeParam = ((TensorConst)pdp[GNNEPdp1.DequantParams]).Value.ToScalar<DeQuantizeParam>();
		int num8 = BitConverter.ToInt16(BitConverter.GetBytes((Half)deQuantizeParam.Scale), 0);
		int zeroPoint = deQuantizeParam.ZeroPoint;
		scale = new Gpr(-1, num8, needRenewal: false);
		Gpr bias = new Gpr(-1, zeroPoint, needRenewal: false);
		GnneActionMfuPdp1ConfDeq gnneActionMfuPdp1ConfDeq = new GnneActionMfuPdp1ConfDeq(6, scale, bias, checkedDataType, ((TensorConst)pdp[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
		if ((object)_latestConfActions.Pdp1ConfDeq == null || _latestConfActions.Pdp1ConfDeq != gnneActionMfuPdp1ConfDeq)
		{
			scale = _gprHandler.SetGprItem(num8);
			bias = _gprHandler.SetGprItem(zeroPoint);
			gnneActionMfuPdp1ConfDeq = new GnneActionMfuPdp1ConfDeq(6, scale, bias, checkedDataType, ((TensorConst)pdp[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
			_latestConfActions.Pdp1ConfDeq = gnneActionMfuPdp1ConfDeq;
			Actions.Add(gnneActionMfuPdp1ConfDeq);
		}
		QuantizeParam quantizeParam = ((TensorConst)pdp[GNNEPdp1.QuantParams]).Value.ToScalar<QuantizeParam>();
		int num9 = BitConverter.ToInt16(BitConverter.GetBytes((Half)quantizeParam.Scale), 0);
		int num10 = BitConverter.ToInt16(BitConverter.GetBytes((Half)quantizeParam.ZeroPoint), 0);
		scale = new Gpr(-1, num9, needRenewal: false);
		Gpr bias2 = new Gpr(-1, num10, needRenewal: false);
		GnneActionMfuPdp1ConfQuant gnneActionMfuPdp1ConfQuant = new GnneActionMfuPdp1ConfQuant(7, scale, bias2, checkedDataType2, ((TensorConst)pdp[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
		if ((object)_latestConfActions.Pdp1ConfQuant == null || _latestConfActions.Pdp1ConfQuant != gnneActionMfuPdp1ConfQuant)
		{
			scale = _gprHandler.SetGprItem(num9);
			bias2 = _gprHandler.SetGprItem(num10);
			gnneActionMfuPdp1ConfQuant = new GnneActionMfuPdp1ConfQuant(7, scale, bias2, checkedDataType2, ((TensorConst)pdp[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
			_latestConfActions.Pdp1ConfQuant = gnneActionMfuPdp1ConfQuant;
			Actions.Add(gnneActionMfuPdp1ConfQuant);
		}
		Gpr n8 = _gprHandler.SetGprItem(ifmap[0].Length);
		Gpr c8 = _gprHandler.SetGprItem(ifmap[1].Length);
		Gpr h4 = _gprHandler.SetGprItem(ifmap[2].Length);
		Gpr w2 = _gprHandler.SetGprItem(ifmap[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(ifmap[0].Length, ifmap[1].Length, ifmap[2].Length, ifmap[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n8, c8, h4, w2, ssr));
		int addr = iPp * _glb.GlbMap[ItemName.Ofmap].AllocatedBytes + offsetD;
		int addr2 = iPp * _glb.GlbMap[ItemName.Ifmap].AllocatedBytes + offsetS;
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.Ofmap].Mmu.Id, addr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.Ifmap].Mmu.Id, addr2));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionMfuPdp1Compute(addrD, addrS, ssr));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
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

	public void UpdateMfuGlobalPdp1(Call call, DataType inputType, DataType outputType, PDP_FUNCTION pdpOp, SegmentND ifmap, SegmentND ofmap, int iPp, Half sumScale, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null, int offsetS = 0, int offsetD = 0, ItemName ifName = ItemName.Ifmap, List<int> ifStride = null, List<int> ofStride = null)
	{
		int strideH = ((TensorConst)call[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
		int strideW = ((TensorConst)call[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
		int length = ifmap[2].Length;
		int length2 = ifmap[3].Length;
		if (ifStride == null)
		{
			ifStride = new int[3]
			{
				_glb.GlbMap[ifName].Dimensions[1],
				_glb.GlbMap[ifName].Dimensions[2],
				_glb.GlbMap[ifName].Dimensions[3]
			}.ToList();
		}
		Ssr strideS = new Ssr(-1, PackStride(ifStride[0], ifStride[1], ifStride[2]), needRenewal: false);
		if (ofStride == null)
		{
			ofStride = new int[3]
			{
				_glb.GlbMap[ItemName.Ofmap].Dimensions[1],
				_glb.GlbMap[ItemName.Ofmap].Dimensions[2],
				_glb.GlbMap[ItemName.Ofmap].Dimensions[3]
			}.ToList();
			if (inputType == DataTypes.Float16 && (outputType == DataTypes.UInt8 || outputType == DataTypes.Int8))
			{
				ofStride[2] *= 2;
			}
		}
		Ssr strideD = new Ssr(-1, PackStride(ofStride[0], ofStride[1], ofStride[2]), needRenewal: false);
		GnneActionMfuPdp1Conf1 gnneActionMfuPdp1Conf = new GnneActionMfuPdp1Conf1(1, strideW, strideH, strideD, strideS, pdpOp);
		if ((object)_latestConfActions.Pdp1Conf1 == null || _latestConfActions.Pdp1Conf1 != gnneActionMfuPdp1Conf)
		{
			Gpr n = _gprHandler.SetGprItem(ifStride[0]);
			Gpr c = _gprHandler.SetGprItem(ifStride[1]);
			Gpr h = _gprHandler.SetGprItem(ifStride[2]);
			strideS = _ssRegHandler.SetSsrItem(PackStride(ifStride[0], ifStride[1], ifStride[2]));
			Actions.Add(new GnneActionPackStrideReg(n, c, h, strideS));
			Gpr n2 = _gprHandler.SetGprItem(ofStride[0]);
			Gpr c2 = _gprHandler.SetGprItem(ofStride[1]);
			Gpr h2 = _gprHandler.SetGprItem(ofStride[2]);
			strideD = _ssRegHandler.SetSsrItem(PackStride(ofStride[0], ofStride[1], ofStride[2]));
			Actions.Add(new GnneActionPackStrideReg(n2, c2, h2, strideD));
			gnneActionMfuPdp1Conf = new GnneActionMfuPdp1Conf1(1, strideW, strideH, strideD, strideS, pdpOp);
			_latestConfActions.Pdp1Conf1 = gnneActionMfuPdp1Conf;
			Actions.Add(gnneActionMfuPdp1Conf);
		}
		int length3 = ofmap[3].Length;
		int length4 = ofmap[2].Length;
		int num = System.Math.Min(GNNEEnv.MfuPuHeight, ifmap[1].Length * length) / length;
		int num2 = length * num;
		int num3 = num;
		if (ifmap[1].Length % num != 0)
		{
			num3 = ifmap[1].Length % num;
		}
		int num4 = length * num3;
		int num5 = BitConverter.ToInt16(BitConverter.GetBytes(((TensorConst)call[GNNEPdp1.Value]).Value.ToScalar<Half>()), 0);
		Gpr countW = new Gpr(-1, length3, needRenewal: false);
		Gpr countH = new Gpr(-1, length4, needRenewal: false);
		Gpr peH = new Gpr(-1, num2, needRenewal: false);
		Gpr peLastH = new Gpr(-1, num4, needRenewal: false);
		GnneActionMfuPdp1Conf2 gnneActionMfuPdp1Conf2 = new GnneActionMfuPdp1Conf2(2, countW, countH, peH, peLastH);
		if ((object)_latestConfActions.Pdp1Conf2 == null || _latestConfActions.Pdp1Conf2 != gnneActionMfuPdp1Conf2)
		{
			countW = _gprHandler.SetGprItem(length3);
			countH = _gprHandler.SetGprItem(length4);
			peH = _gprHandler.SetGprItem(num2);
			peLastH = _gprHandler.SetGprItem(num4);
			gnneActionMfuPdp1Conf2 = new GnneActionMfuPdp1Conf2(2, countW, countH, peH, peLastH);
			_latestConfActions.Pdp1Conf2 = gnneActionMfuPdp1Conf2;
			Actions.Add(gnneActionMfuPdp1Conf2);
		}
		Ssr pad = new Ssr(-1, PackShape(ifmap.PadH.Before, ifmap.PadH.After, ifmap.PadW.Before, ifmap.PadW.After), needRenewal: false);
		Gpr peChannels = new Gpr(-1, num, needRenewal: false);
		Gpr peLastChannels = new Gpr(-1, num3, needRenewal: false);
		Gpr padValue = new Gpr(-1, num5, needRenewal: false);
		GnneActionMfuPdp1Conf3 gnneActionMfuPdp1Conf3 = new GnneActionMfuPdp1Conf3(3, peChannels, peLastChannels, padValue, pad);
		if ((object)_latestConfActions.Pdp1Conf3 == null || _latestConfActions.Pdp1Conf3 != gnneActionMfuPdp1Conf3)
		{
			Gpr n3 = _gprHandler.SetGprItem(ifmap.PadH.Before);
			Gpr c3 = _gprHandler.SetGprItem(ifmap.PadH.After);
			Gpr h3 = _gprHandler.SetGprItem(ifmap.PadW.Before);
			Gpr w = _gprHandler.SetGprItem(ifmap.PadW.After);
			pad = _ssRegHandler.SetSsrItem(PackShape(ifmap.PadH.Before, ifmap.PadH.After, ifmap.PadW.Before, ifmap.PadW.After));
			Actions.Add(new GnneActionPackShapeReg(n3, c3, h3, w, pad));
			peChannels = _gprHandler.SetGprItem(num);
			peLastChannels = _gprHandler.SetGprItem(num3);
			padValue = _gprHandler.SetGprItem(num5);
			gnneActionMfuPdp1Conf3 = new GnneActionMfuPdp1Conf3(3, peChannels, peLastChannels, padValue, pad);
			_latestConfActions.Pdp1Conf3 = gnneActionMfuPdp1Conf3;
			Actions.Add(gnneActionMfuPdp1Conf3);
		}
		Gpr windowW = new Gpr(-1, length2, needRenewal: false);
		Gpr windowH = new Gpr(-1, length, needRenewal: false);
		int num6 = BitConverter.ToInt16(BitConverter.GetBytes(sumScale), 0);
		Gpr scale = new Gpr(-1, num6, needRenewal: false);
		bool enableH2C = false;
		bool enableBw = false;
		GnneActionMfuPdp1Conf4 gnneActionMfuPdp1Conf4 = new GnneActionMfuPdp1Conf4(4, windowW, windowH, scale, enableH2C, enableBw);
		if ((object)_latestConfActions.Pdp1Conf4 == null || _latestConfActions.Pdp1Conf4 != gnneActionMfuPdp1Conf4)
		{
			windowW = _gprHandler.SetGprItem(length2);
			windowH = _gprHandler.SetGprItem(length);
			scale = _gprHandler.SetGprItem(num6);
			gnneActionMfuPdp1Conf4 = new GnneActionMfuPdp1Conf4(4, windowW, windowH, scale, enableH2C, enableBw);
			_latestConfActions.Pdp1Conf4 = gnneActionMfuPdp1Conf4;
			Actions.Add(gnneActionMfuPdp1Conf4);
		}
		DeQuantizeParam deQuantizeParam = ((TensorConst)call[GNNEPdp1.DequantParams]).Value.ToScalar<DeQuantizeParam>();
		int num7 = BitConverter.ToInt16(BitConverter.GetBytes((Half)deQuantizeParam.Scale), 0);
		int zeroPoint = deQuantizeParam.ZeroPoint;
		scale = new Gpr(-1, num7, needRenewal: false);
		Gpr bias = new Gpr(-1, zeroPoint, needRenewal: false);
		GnneActionMfuPdp1ConfDeq gnneActionMfuPdp1ConfDeq = new GnneActionMfuPdp1ConfDeq(6, scale, bias, inputType, ((TensorConst)call[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
		if ((object)_latestConfActions.Pdp1ConfDeq == null || _latestConfActions.Pdp1ConfDeq != gnneActionMfuPdp1ConfDeq)
		{
			scale = _gprHandler.SetGprItem(num7);
			bias = _gprHandler.SetGprItem(zeroPoint);
			gnneActionMfuPdp1ConfDeq = new GnneActionMfuPdp1ConfDeq(6, scale, bias, inputType, ((TensorConst)call[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
			_latestConfActions.Pdp1ConfDeq = gnneActionMfuPdp1ConfDeq;
			Actions.Add(gnneActionMfuPdp1ConfDeq);
		}
		QuantizeParam quantizeParam = ((TensorConst)call[GNNEPdp1.QuantParams]).Value.ToScalar<QuantizeParam>();
		int num8 = BitConverter.ToInt16(BitConverter.GetBytes((Half)quantizeParam.Scale), 0);
		int num9 = BitConverter.ToInt16(BitConverter.GetBytes((Half)quantizeParam.ZeroPoint), 0);
		scale = new Gpr(-1, num8, needRenewal: false);
		Gpr bias2 = new Gpr(-1, num9, needRenewal: false);
		GnneActionMfuPdp1ConfQuant gnneActionMfuPdp1ConfQuant = new GnneActionMfuPdp1ConfQuant(7, scale, bias2, outputType, ((TensorConst)call[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
		if ((object)_latestConfActions.Pdp1ConfQuant == null || _latestConfActions.Pdp1ConfQuant != gnneActionMfuPdp1ConfQuant)
		{
			scale = _gprHandler.SetGprItem(num8);
			bias2 = _gprHandler.SetGprItem(num9);
			gnneActionMfuPdp1ConfQuant = new GnneActionMfuPdp1ConfQuant(7, scale, bias2, outputType, ((TensorConst)call[GNNEPdp1.ShiftBits]).Value.ToScalar<int>());
			_latestConfActions.Pdp1ConfQuant = gnneActionMfuPdp1ConfQuant;
			Actions.Add(gnneActionMfuPdp1ConfQuant);
		}
		Gpr n4 = _gprHandler.SetGprItem(ifmap[0].Length);
		Gpr c4 = _gprHandler.SetGprItem(ifmap[1].Length);
		Gpr h4 = _gprHandler.SetGprItem(ifmap[2].Length);
		Gpr w2 = _gprHandler.SetGprItem(ifmap[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackShape(ifmap[0].Length, ifmap[1].Length, ifmap[2].Length, ifmap[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n4, c4, h4, w2, ssr));
		int addr = iPp * _glb.GlbMap[ItemName.Ofmap].AllocatedBytes + offsetD;
		int addr2 = iPp * _glb.GlbMap[ifName].AllocatedBytes + offsetS;
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ItemName.Ofmap].Mmu.Id, addr));
		Gpr addrS = _gprHandler.SetGprItem(ToGlbAddr(_glb.GlbMap[ifName].Mmu.Id, addr2));
		if (GNNEEnv.UseCcr)
		{
			UpdateCcr(ccrsToSet, ccrsToClr);
		}
		Actions.Add(new GnneActionMfuPdp1Compute(addrD, addrS, ssr));
		if (!GNNEEnv.UseCcr)
		{
			Actions.Add(new GnneActionFence());
		}
	}

	public void UpdateMfuMemset(SegmentND slice, SegmentND tensor, int value, DataType l2Datatype, int mmuItem, int iPp, int offsetD = 0, List<CcrSet> ccrsToSet = null, List<CcrClr> ccrsToClr = null)
	{
		Gpr n = _gprHandler.SetGprItem(tensor[1].Length);
		Gpr c = _gprHandler.SetGprItem(tensor[2].Length);
		Gpr h = _gprHandler.SetGprItem(tensor[3].Length);
		Ssr ssr = _ssRegHandler.SetSsrItem(PackStride(tensor[1].Length, tensor[2].Length, tensor[3].Length));
		Actions.Add(new GnneActionPackStrideReg(n, c, h, ssr));
		Gpr n2 = _gprHandler.SetGprItem(slice[0].Length);
		Gpr c2 = _gprHandler.SetGprItem(slice[1].Length);
		Gpr h2 = _gprHandler.SetGprItem(slice[2].Length);
		Gpr w = _gprHandler.SetGprItem(slice[3].Length);
		Ssr ssr2 = _ssRegHandler.SetSsrItem(PackShape(slice[0].Length, slice[1].Length, slice[2].Length, slice[3].Length));
		Actions.Add(new GnneActionPackShapeReg(n2, c2, h2, w, ssr2));
		Gpr value2 = _gprHandler.SetGprItem(value);
		int addr = iPp * _glb.GlbMap[ItemName.Ofmap].AllocatedBytes + offsetD;
		Gpr addrD = _gprHandler.SetGprItem(ToGlbAddr(mmuItem, addr));
		Actions.Add(new GnneActionMfuMemset(addrD, value2, ssr, ssr2, l2Datatype));
		Actions.Add(new GnneActionFence());
	}

	public void UpdateAi2dResize(Ai2dConfig config, List<CcrSet> ccrs_to_set = null, List<CcrClr> ccrs_to_clr = null, bool write_all = true)
	{
		for (int i = ((!write_all) ? 28 : 0); i < 36; i++)
		{
			uint addrValue = config.GetAddrValue((uint)i);
			Gpr gpr = _gprHandler.SetGprItem((int)addrValue);
			if (i < 8)
			{
				Actions.Add(new GnneActionExtraw(i * 4, gpr, gpr.Index, 0));
			}
			else
			{
				Actions.Add(new GnneActionExtrw(i * 4, gpr, gpr.Index, 0));
			}
		}
		if (config.IntrMask == 0)
		{
			if (GNNEEnv.UseCcr)
			{
				UpdateCcr(ccrs_to_set, ccrs_to_clr);
			}
			Actions.Add(new GnneActionAi2dCompute());
			if (!GNNEEnv.UseCcr)
			{
				Actions.Add(new GnneActionFence());
			}
		}
	}

	public void ResetActions()
	{
		Actions.Clear();
	}

	public void ReshapeSpecialScenario(ref List<int> strideDdr, ref GNNEShape shape, ref List<int> strideGlb)
	{
		if (strideDdr[0] == shape[1] && strideDdr[1] == shape[2] && strideDdr[2] == shape[3] && strideGlb[0] == shape[1] && strideGlb[1] == shape[2] && strideGlb[2] == shape[3] && strideDdr[0] * strideDdr[1] * strideDdr[2] < 65536 && strideGlb[0] * strideGlb[1] * strideGlb[2] < 65536 && shape[0] * shape[1] * shape[2] * shape[3] < 65536)
		{
			strideDdr = new int[3]
			{
				1,
				1,
				strideDdr[0] * strideDdr[1] * strideDdr[2]
			}.ToList();
			shape = new GNNEShape(1, 1, 1, shape[0] * shape[1] * shape[2] * shape[3]);
			strideGlb = new int[3]
			{
				1,
				1,
				strideGlb[0] * strideGlb[1] * strideGlb[2]
			}.ToList();
		}
		else if (strideDdr[1] == shape[2] && strideDdr[2] == shape[3] && strideGlb[1] == shape[2] && strideGlb[2] == shape[3] && strideDdr[0] * strideDdr[1] * strideDdr[2] < 65536 && strideGlb[0] * strideGlb[1] * strideGlb[2] < 65536 && shape[1] * shape[2] * shape[3] < 65536)
		{
			strideDdr = new int[3]
			{
				1,
				1,
				strideDdr[0] * strideDdr[1] * strideDdr[2]
			}.ToList();
			shape = new GNNEShape(shape[0], 1, 1, shape[1] * shape[2] * shape[3]);
			strideGlb = new int[3]
			{
				1,
				1,
				strideGlb[0] * strideGlb[1] * strideGlb[2]
			}.ToList();
		}
		else if (strideDdr[2] == shape[3] && strideGlb[2] == shape[3] && strideDdr[1] * strideDdr[2] < 65536 && strideGlb[1] * strideGlb[2] < 65536 && shape[2] * shape[3] < 65536)
		{
			strideDdr = new int[3]
			{
				strideDdr[0],
				1,
				strideDdr[1] * strideDdr[2]
			}.ToList();
			shape = new GNNEShape(shape[0], shape[1], 1, shape[2] * shape[3]);
			strideGlb = new int[3]
			{
				strideGlb[0],
				1,
				strideGlb[1] * strideGlb[2]
			}.ToList();
		}
	}

	private int ToGlbAddr(int mmuItem, int addr)
	{
		return (mmuItem << 28) + addr;
	}
}
