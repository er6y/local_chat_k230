using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class ActionToInstruct
{
	private List<Call> _instSeq;

	public List<Call> InstSeq
	{
		get
		{
			return _instSeq;
		}
		set
		{
			_instSeq = value;
		}
	}

	public ActionToInstruct()
	{
		InstSeq = new List<Call>();
	}

	public Sequential Instructions(List<GnneAction> actions)
	{
		// actions = GnnePeephole.Run(actions); // T2-1 disabled: full-model sim FAIL cos 0.9396
		Visit(actions);
		// InstSeq = GnneLoopify.Run(InstSeq); // DISABLED 2026-09-22
		// InstSeq = GnnePeephole.RunInst(InstSeq); // T2-2 disabled: same verdict
		return Enumerable.ToArray(InstSeq).ToSequential();
	}

	public void Visit(List<GnneAction> actions)
	{
		foreach (GnneAction action in actions)
		{
			switch (action.Name)
			{
			case GnneActionName.WriteGpr:
				Visit((GnneActionWriteGpr)action);
				break;
			case GnneActionName.PackStrideReg:
				Visit((GnneActionPackStrideReg)action);
				break;
			case GnneActionName.PackShapeReg:
				Visit((GnneActionPackShapeReg)action);
				break;
			case GnneActionName.MmuConf:
				Visit((Gnne_action_mmu_conf)action);
				break;
			case GnneActionName.Intr:
				Visit((GnneActionIntr)action);
				break;
			case GnneActionName.Fence:
				Visit((GnneActionFence)action);
				break;
			case GnneActionName.End:
				Visit((GnneActionEnd)action);
				break;
			case GnneActionName.L2LoadConf:
				Visit((Gnne_action_l2_load_conf)action);
				break;
			case GnneActionName.L2Load:
				Visit((GnneActionL2Load)action);
				break;
			case GnneActionName.L2LoadWConf:
				Visit((GnneActionL2LoadWConf)action);
				break;
			case GnneActionName.L2LoadW:
				Visit((GnneActionL2LoadW)action);
				break;
			case GnneActionName.L2StoreConf:
				Visit((GnneActionL2StoreConf)action);
				break;
			case GnneActionName.L2Store:
				Visit((GnneActionL2Store)action);
				break;
			case GnneActionName.DmLoadL1Conf:
				Visit((GnneActionDmLoadL1Conf)action);
				break;
			case GnneActionName.DmLoadL1:
				Visit((GnneActionDmLoadL1)action);
				break;
			case GnneActionName.DmLoadWConf:
				Visit((GnneActionDmLoadWConf)action);
				break;
			case GnneActionName.DmLoadWConf2:
				Visit((GnneActionDmLoadWConf2)action);
				break;
			case GnneActionName.DmLoadWConfDeq:
				Visit((GnneActionDmLoadWConfDeq)action);
				break;
			case GnneActionName.DmLoadW:
				Visit((GnneActionDmLoadW)action);
				break;
			case GnneActionName.PuFetchifConf1:
				Visit((GnneActionPuFetchifConf1)action);
				break;
			case GnneActionName.PuFetchifConf2:
				Visit((GnneActionPuFetchifConf2)action);
				break;
			case GnneActionName.PuFetchifConf3:
				Visit((GnneActionPuFetchifConf3)action);
				break;
			case GnneActionName.PuFetchifConf4:
				Visit((GnneActionPuFetchifConf4)action);
				break;
			case GnneActionName.PuFetchifConfDeq:
				Visit((GnneActionPuFetchifConfDeq)action);
				break;
			case GnneActionName.PuWConf:
				Visit((GnneActionPuWConf)action);
				break;
			case GnneActionName.PuOfConf1:
				Visit((GnneActionPuOfConf1)action);
				break;
			case GnneActionName.PuOfConf2:
				Visit((GnneActionPuOfConf2)action);
				break;
			case GnneActionName.PuComputeConf:
				Visit((GnneActionPuComputeConf)action);
				break;
			case GnneActionName.PuCompute:
				Visit((GnneActionPuCompute)action);
				break;
			case GnneActionName.PuForwardPsum:
				Visit((GnneActionPuForwardPsum)action);
				break;
			case GnneActionName.DmLoadAct0:
				Visit((GnneActionDmLoadAct0)action);
				break;
			case GnneActionName.DmStoreOf:
				Visit((GnneActionDmStoreOf)action);
				break;
			case GnneActionName.DmStoreOfConf:
				Visit((GnneActionDmStoreOfConf)action);
				break;
			case GnneActionName.Act0Src1Conf:
				Visit((GnneActionAct0Src1Conf)action);
				break;
			case GnneActionName.Act0Compute:
				Visit((GnneActionAct0Compute)action);
				break;
			case GnneActionName.MfuAct1ConfStride:
				Visit((GnneActionMfuAct1ConfStride)action);
				break;
			case GnneActionName.MfuAct1ConfSrc1:
				Visit((GnneActionMfuAct1ConfSrc1)action);
				break;
			case GnneActionName.MfuAct1ConfSrc2:
				Visit((GnneActionMfuAct1ConfSrc2)action);
				break;
			case GnneActionName.MfuAct1ConfDest:
				Visit((GnneActionMfuAct1ConfDest)action);
				break;
			case GnneActionName.MfuAct1ConfDeq:
				Visit((GnneActionMfuAct1ConfDeq)action);
				break;
			case GnneActionName.MfuAct1ConfQuant:
				Visit((GnneActionMfuAct1ConfQuant)action);
				break;
			case GnneActionName.MfuAct1Conf:
				Visit((GnneActionMfuAct1Conf)action);
				break;
			case GnneActionName.MfuAct1Compute:
				Visit((GnneActionMfuAct1Compute)action);
				break;
			case GnneActionName.MfuTransposeConf:
				Visit((GnneActionMfuTransposeConf)action);
				break;
			case GnneActionName.MfuTranspose:
				Visit((GnneActionMfuTranspose)action);
				break;
			case GnneActionName.MfuPdp1Conf1:
				Visit((GnneActionMfuPdp1Conf1)action);
				break;
			case GnneActionName.MfuPdp1Conf2:
				Visit((GnneActionMfuPdp1Conf2)action);
				break;
			case GnneActionName.MfuPdp1Conf3:
				Visit((GnneActionMfuPdp1Conf3)action);
				break;
			case GnneActionName.MfuPdp1Conf4:
				Visit((GnneActionMfuPdp1Conf4)action);
				break;
			case GnneActionName.MfuPdp1ConfQuant:
				Visit((GnneActionMfuPdp1ConfQuant)action);
				break;
			case GnneActionName.MfuPdp1ConfDeq:
				Visit((GnneActionMfuPdp1ConfDeq)action);
				break;
			case GnneActionName.MfuPdp1Compute:
				Visit((GnneActionMfuPdp1Compute)action);
				break;
			case GnneActionName.MfuMemset:
				Visit((GnneActionMfuMemset)action);
				break;
			case GnneActionName.PuPdp0ModeConf:
				Visit((GnneActionPuPdp0ModeConf)action);
				break;
			case GnneActionName.PuPdp0FetchifConf1:
				Visit((GnneActionPuPdp0FetchifConf1)action);
				break;
			case GnneActionName.PuPdp0FetchifConf2:
				Visit((GnneActionPuPdp0FetchifConf2)action);
				break;
			case GnneActionName.PuPdp0FetchifConf3:
				Visit((GnneActionPuPdp0FetchifConf3)action);
				break;
			case GnneActionName.PuPdp0FetchifConf4:
				Visit((GnneActionPuPdp0FetchifConf4)action);
				break;
			case GnneActionName.PuPdp0ConfDeq:
				Visit((GnneActionPuPdp0ConfDeq)action);
				break;
			case GnneActionName.PuPdp0WConf:
				Visit((GnneActionPuPdp0WConf)action);
				break;
			case GnneActionName.PuPdp0OfConf:
				Visit((GnneActionPuPdp0OfConf)action);
				break;
			case GnneActionName.PuPdp0Compute:
				Visit((GnneActionPuPdp0Compute)action);
				break;
			case GnneActionName.CcrDecl:
				Visit((GnneActionCcrDecl)action);
				break;
			case GnneActionName.CcrSet:
				Visit((GnneActionCcrSet)action);
				break;
			case GnneActionName.CcrClr:
				Visit((GnneActionCcrClr)action);
				break;
			case GnneActionName.Extrw:
				Visit((GnneActionExtrw)action);
				break;
			case GnneActionName.Extraw:
				Visit((GnneActionExtraw)action);
				break;
			case GnneActionName.Ai2dCompute:
				Visit((GnneActionAi2dCompute)action);
				break;
			default:
				throw new NotImplementedException("unsupported gnne action");
			}
		}
	}

	private Expr ToDdrAddrOffset(Nncase.TIR.Buffer alloc, ulong offset, bool inputIsSlice = false, int nextConcatStart = -1)
	{
		switch (alloc.MemSpan.Location)
		{
		case MemoryLocation.Input:
			if (inputIsSlice)
			{
				return Nncase.IR.F.Buffer.DDrOf(alloc.MemSpan) + offset;
			}
			return offset;
		case MemoryLocation.Output:
			if (nextConcatStart >= 0)
			{
				return Nncase.IR.F.Buffer.DDrOf(alloc.MemSpan) - nextConcatStart + offset;
			}
			return offset;
		default:
			return Nncase.IR.F.Buffer.DDrOf(alloc.MemSpan) + offset;
		}
	}

	private Expr ToDdrAddrOffset(Nncase.TIR.Buffer alloc, SegmentND sliceInfo, bool inputIsSlice = false, int nextConcatStart = -1, int[] layout = null)
	{
		int sliceOffsetInTensor;
		if (layout == null)
		{
			SegmentND tensor = new SegmentND(0..alloc.FixedDimensions(0), 0..alloc.FixedDimensions(1), 0..alloc.FixedDimensions(2), 0..alloc.FixedDimensions(3));
			sliceOffsetInTensor = TileUtilities.GetSliceOffsetInTensor(in tensor, in sliceInfo);
		}
		else
		{
			SegmentND tensor2 = new SegmentND(0..layout[0], 0..layout[1], 0..layout[2], 0..layout[3]);
			sliceOffsetInTensor = TileUtilities.GetSliceOffsetInTensor(in tensor2, in sliceInfo);
		}
		ulong num = (ulong)sliceOffsetInTensor;
		return ToDdrAddrOffset(alloc, (ulong)TileUtilities.GetBytesPerElement(alloc.ElemType) * num);
	}

	private QUANT_TYPE ToQuantType(DataType type)
	{
		if (type == DataTypes.UInt8)
		{
			return QUANT_TYPE.u8;
		}
		if (type == DataTypes.Int8)
		{
			return QUANT_TYPE.i8;
		}
		if (type == DataTypes.Int16)
		{
			return QUANT_TYPE.i16;
		}
		return QUANT_TYPE.disable;
	}

	private DDR_DATATYPE ToDdrType(DataType type)
	{
		if (type == DataTypes.Int8 || type == DataTypes.UInt8)
		{
			return DDR_DATATYPE.i8;
		}
		if (type == DataTypes.Float16)
		{
			return DDR_DATATYPE.fp16;
		}
		if (type == DataTypes.Float32)
		{
			return DDR_DATATYPE.fp32;
		}
		if (type == DataTypes.Int16)
		{
			return DDR_DATATYPE.i16;
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	private L2_DATATYPE ToL2Type(DataType type)
	{
		if (type == DataTypes.Int8 || type == DataTypes.UInt8)
		{
			return L2_DATATYPE.i8;
		}
		if (type == DataTypes.Float16)
		{
			return L2_DATATYPE.fp16;
		}
		if (type == DataTypes.Int16)
		{
			return L2_DATATYPE.i16;
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	private ACT_OUTPUT_TYPE ToActOutputType(DataType type)
	{
		if (type == DataTypes.Int8)
		{
			return ACT_OUTPUT_TYPE.i8;
		}
		if (type == DataTypes.UInt8)
		{
			return ACT_OUTPUT_TYPE.u8;
		}
		if (type == DataTypes.Float16)
		{
			return ACT_OUTPUT_TYPE.fp16;
		}
		if (type == DataTypes.Int16)
		{
			return ACT_OUTPUT_TYPE.i16;
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	private void Visit(GnneActionWriteGpr action)
	{
		InstSeq.Add(I.LoadImm((GP_REGISTER)action.GprIndex, action.Imm));
	}

	private void Visit(GnneActionPackStrideReg action)
	{
		Expr value = action.N.Value;
		Expr value2 = action.C.Value;
		Expr value3 = action.H.Value;
		if (action.N.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.N.Index, value));
		}
		if (action.C.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.C.Index, value2));
		}
		if (action.H.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.H.Index, value3));
		}
		InstSeq.Add(I.SS_PACK_STRIDE((GP_REGISTER)action.N.Index, (GP_REGISTER)action.C.Index, (GP_REGISTER)action.H.Index, (SHAPE_REGISTER)action.Ss.Index));
	}

	private void Visit(GnneActionPackShapeReg action)
	{
		if (action.N.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.N.Index, action.N.Value));
		}
		if (action.C.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.C.Index, action.C.Value));
		}
		if (action.H.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.H.Index, action.H.Value));
		}
		if (action.W.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.W.Index, action.W.Value));
		}
		InstSeq.Add(I.SS_PACK_SHAPE((GP_REGISTER)action.N.Index, (GP_REGISTER)action.C.Index, (GP_REGISTER)action.H.Index, (GP_REGISTER)action.W.Index, (SHAPE_REGISTER)action.Ss.Index));
	}

	private void Visit(Gnne_action_mmu_conf action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Start.Index, action.Start.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Depth.Index, action.Depth.Value);
		Call item3 = I.MMU_CONF((GP_REGISTER)action.Start.Index, (GP_REGISTER)action.Depth.Index, action.Item.Id);
		if (action.Start.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Depth.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionIntr action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.IntrNum.Index, action.IntrNum.Value);
		Call item2 = I.INTR((GP_REGISTER)action.IntrNum.Index, action.IntrNum.Value);
		if (action.IntrNum.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionFence action)
	{
		InstSeq.Add(I.FENCE());
	}

	private void Visit(GnneActionEnd action)
	{
		InstSeq.Add(I.END(GP_REGISTER.x0));
	}

	private void Visit(Gnne_action_l2_load_conf action)
	{
		Call item = I.L2_LOAD_CONF((SHAPE_REGISTER)action.StrideD.Index, (SHAPE_REGISTER)action.StrideS.Index, ToL2Type(action.L2Datatype), ToDdrType(action.DdrDatatype));
		InstSeq.Add(item);
	}

	private void Visit(GnneActionL2Load action)
	{
		Expr offset = ToDdrAddrOffset(action.Buffer, action.SliceInfo, inputIsSlice: false, -1, action.Layout);
		InstSeq.Add(I.LoadDdrAddr((GP_REGISTER)action.Basement.Index, (GP_REGISTER)action.AddrS.Index, action.Basement.Value, offset));
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value));
		}
		InstSeq.Add(I.L2_LOAD((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.AddrS.Index, (SHAPE_REGISTER)action.Shape.Index));
	}

	private void Visit(GnneActionL2LoadWConf action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.LenCompressed.Index, action.LenCompressed.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.LenDecompressed.Index, action.LenDecompressed.Value);
		Call item3 = I.L2_LOAD_W_CONF((GP_REGISTER)action.LenCompressed.Index, (GP_REGISTER)action.LenDecompressed.Index, ToL2Type(action.L2Datatype), ToDdrType(action.DdrDatatype), action.EnableDecompress);
		if (action.LenCompressed.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.LenDecompressed.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionL2LoadW action)
	{
		InstSeq.Add(I.LoadDdrAddr((GP_REGISTER)action.Basement.Index, (GP_REGISTER)action.AddrS.Index, action.Basement.Value, action.AddrS.Value));
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value));
		}
		if (action.ValidCNum.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.ValidCNum.Index, action.ValidCNum.Value));
		}
		InstSeq.Add(I.L2_LOAD_W((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.AddrS.Index, (GP_REGISTER)action.ValidCNum.Index));
	}

	private void Visit(GnneActionL2StoreConf action)
	{
		Call item = I.L2_STORE_CONF((SHAPE_REGISTER)action.StrideD.Index, (SHAPE_REGISTER)action.StrideS.Index, ToL2Type(action.L2Datatype), ToDdrType(action.DdrDatatype));
		InstSeq.Add(item);
	}

	private void Visit(GnneActionL2Store action)
	{
		Expr offset = ToDdrAddrOffset(action.Buffer, action.SliceInfo, inputIsSlice: false, -1, action.Layout);
		InstSeq.Add(I.LoadDdrAddr((GP_REGISTER)action.Basement.Index, (GP_REGISTER)action.AddrD.Index, action.Basement.Value, offset));
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value));
		}
		InstSeq.Add(I.L2_STORE((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.AddrS.Index, (SHAPE_REGISTER)action.Shape.Index));
	}

	private void Visit(GnneActionDmLoadL1Conf action)
	{
		Call item = I.DM_LOAD_L1_CONF(action.TcuId, action.PuId, (SHAPE_REGISTER)action.StrideS.Index, ToL2Type(action.L2Datatype), action.L1Type);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionDmLoadL1 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.HtocWindow.Index, action.HtocWindow.Value);
		Call item3 = I.DM_LOAD_L1(action.TcuId, action.PuId, (GP_REGISTER)action.AddrS.Index, (GP_REGISTER)action.HtocWindow.Index, (SHAPE_REGISTER)action.Shape.Index, action.L1Type);
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.HtocWindow.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionDmLoadWConf action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.StrideOc.Index, action.StrideOc.Value);
		Call item2 = I.DM_LOAD_W_CONF(action.TcuId, action.PuId, action.KernelH, action.KernelW, (GP_REGISTER)action.StrideOc.Index);
		if (action.StrideOc.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionDmLoadWConf2 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Groups.Index, action.Groups.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Goc.Index, action.Goc.Value);
		Call item3 = I.DM_LOAD_W_CONF2(action.TcuId, action.PuId, (GP_REGISTER)action.Groups.Index, (GP_REGISTER)action.Goc.Index);
		if (action.Groups.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Goc.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionDmLoadWConfDeq action)
	{
		Call item = I.DM_LOAD_W_CONF_DEQ(action.TcuId, action.PuId, ToQuantType(action.QuantType));
		InstSeq.Add(item);
	}

	private void Visit(GnneActionDmLoadW action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.AddrBw.Index, action.AddrBw.Value);
		Call item3 = I.DM_LOAD_W(action.TcuId, action.PuId, (GP_REGISTER)action.AddrS.Index, (GP_REGISTER)action.AddrBw.Index, (SHAPE_REGISTER)action.Iochannels.Index, action.DestType);
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.AddrBw.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionPuFetchifConf1 action)
	{
		Call item = I.PU_FETCHIF_CONF1(action.TcuId, action.PuId, action.StrideW, action.StrideH, (SHAPE_REGISTER)action.StrideS.Index);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionPuFetchifConf2 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Gic.Index, action.Gic.Value);
		Call item2 = I.PU_FETCHIF_CONF2(action.TcuId, action.PuId, (GP_REGISTER)action.Gic.Index, GP_REGISTER.x0);
		if (action.Gic.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionPuFetchifConf3 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Groups.Index, action.Groups.Value);
		Call item3 = I.PU_FETCHIF_CONF3(action.TcuId, action.PuId, (GP_REGISTER)action.AddrS.Index, (GP_REGISTER)action.Groups.Index, (SHAPE_REGISTER)action.Shape.Index);
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Groups.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionPuFetchifConf4 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.PadValue.Index, action.PadValue.Value);
		Call item2 = I.PU_FETCHIF_CONF4(action.TcuId, action.PuId, (GP_REGISTER)action.PadValue.Index, (SHAPE_REGISTER)action.Pad.Index);
		if (action.PadValue.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionPuFetchifConfDeq action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Ic.Index, action.Ic.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Bx.Index, action.Bx.Value);
		Call item3 = I.PU_FETCHIF_CONF_DEQ(action.TcuId, action.PuId, (GP_REGISTER)action.Ic.Index, (GP_REGISTER)action.Bx.Index, ToQuantType(action.QuantType));
		if (action.Ic.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Bx.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionPuWConf action)
	{
		Call item = I.PU_W_CONF(action.TcuId, action.PuId, action.KernelH, action.KernelW);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionPuOfConf1 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Goc.Index, action.Goc.Value);
		Call item2 = I.PU_OF_CONF1(action.TcuId, action.PuId, (GP_REGISTER)action.Goc.Index, GP_REGISTER.x0, (SHAPE_REGISTER)action.StrideD.Index);
		if (action.Goc.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionPuOfConf2 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value);
		Call item2 = I.PU_OF_CONF2(action.TcuId, action.PuId, (GP_REGISTER)action.AddrD.Index, (SHAPE_REGISTER)action.ShapeD.Index);
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionPuComputeConf action)
	{
		Call item = I.PU_COMPUTE_CONF(action.TcuId, action.PuId, action.LoadPsum, action.ClrPsum, action.DestTarget, action.ReleaseIf, action.Mode);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionPuForwardPsum action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Addr.Index, action.Addr.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Len.Index, action.Len.Value);
		Call item3 = I.PU_FORWARD_PSUM(action.TcuId, action.PuId, (GP_REGISTER)action.Addr.Index, (GP_REGISTER)action.Len.Index);
		if (action.Addr.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Len.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionPuCompute action)
	{
		Call item = I.PU_COMPUTE(action.TcuId, action.Mode);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionDmLoadAct0 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Len.Index, action.Len.Value);
		Call item3 = I.DM_LOAD_ACT0(action.TcuId, action.PuId, (GP_REGISTER)action.AddrS.Index, (GP_REGISTER)action.Len.Index, action.DestChannel, action.IsByChannel);
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Len.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionDmStoreOf action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value);
		Call item2 = I.DM_STORE_OF(action.TcuId, action.PuId, (GP_REGISTER)action.AddrD.Index, (SHAPE_REGISTER)action.Shape.Index, action.SrcChannel);
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionDmStoreOfConf action)
	{
		Call item = I.DM_STORE_OF_CONF(action.TcuId, action.PuId, (SHAPE_REGISTER)action.StrideD.Index, ToL2Type(action.Datatype));
		InstSeq.Add(item);
	}

	private void Visit(GnneActionAct0Src1Conf action)
	{
		Call item = I.ACT0_SRC1_CONF(action.TcuId, action.PuId, action.Channel, (SHAPE_REGISTER)action.Shape.Index, ACT0_CONF_FUNCTION.act_src1_conf, action.RshiftBits);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionAct0Compute action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value);
		Call item2 = I.ACT0_COMPUTE((GP_REGISTER)action.AddrD.Index, action.TcuId, action.Channel, action.Target, ToActOutputType(action.DestDatatype), action.IsByChannel);
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionMfuAct1ConfStride action)
	{
		Call item = I.MFU_ACT1_CONF_STRIDE((SHAPE_REGISTER)action.StrideS1.Index, (SHAPE_REGISTER)action.StrideS2.Index, (SHAPE_REGISTER)action.StrideD1.Index);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionMfuAct1ConfSrc1 action)
	{
		if (action.Slice.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Slice.Index, action.Slice.Value));
		}
		if (action.RightRepeats.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.RightRepeats.Index, action.RightRepeats.Value));
		}
		if (action.SliceRepeats.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.SliceRepeats.Index, action.SliceRepeats.Value));
		}
		InstSeq.Add(I.MFU_ACT1_CONF_SRC1((GP_REGISTER)action.Slice.Index, (GP_REGISTER)action.RightRepeats.Index, (GP_REGISTER)action.SliceRepeats.Index, action.Sid, (SLICE_LOCATION)action.SliceLoc));
	}

	private void Visit(GnneActionMfuAct1ConfSrc2 action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.LeftRepeats.Index, action.LeftRepeats.Value);
		Call item2 = I.MFU_ACT1_CONF_SRC2((GP_REGISTER)action.LeftRepeats.Index, (SHAPE_REGISTER)action.Shape.Index, action.Sid, action.SourceType);
		if (action.LeftRepeats.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionMfuAct1ConfDest action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Len.Index, action.Len.Value);
		Call item2 = I.MFU_ACT1_CONF_DEST((GP_REGISTER)action.Len.Index, (SHAPE_REGISTER)action.Shape.Index);
		if (action.Len.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		InstSeq.Add(item2);
	}

	private void Visit(GnneActionMfuAct1ConfDeq action)
	{
		Call item = I.LoadImm((GP_REGISTER)action.Scale.Index, action.Scale.Value);
		Call item2 = I.LoadImm((GP_REGISTER)action.Bias.Index, action.Bias.Value);
		Call item3 = I.MFU_ACT1_CONF_DEQ((GP_REGISTER)action.Scale.Index, (GP_REGISTER)action.Bias.Index, ToQuantType(action.QuantType), action.Sid, MFU_CONF_FUNCTION.act1_conf_deq, action.RshiftBits);
		if (action.Scale.NeedRenewal)
		{
			InstSeq.Add(item);
		}
		if (action.Bias.NeedRenewal)
		{
			InstSeq.Add(item2);
		}
		InstSeq.Add(item3);
	}

	private void Visit(GnneActionMfuAct1ConfQuant action)
	{
		Call item = I.MFU_ACT1_CONF_QUANT(ToQuantType(action.QuantType), MFU_CONF_FUNCTION.act1_conf_quant, action.RshiftBits);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionMfuAct1Conf action)
	{
		Call item = I.MFU_ACT1_CONF(action.Funct4, action.IsByChannel, action.Is16Segments);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionMfuAct1Compute action)
	{
		if (action.AddrD1.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD1.Index, action.AddrD1.Value));
		}
		if (action.AddrS1.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS1.Index, action.AddrS1.Value));
		}
		if (action.AddrS2.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS2.Index, action.AddrS2.Value));
		}
		if (action.AddrArg.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrArg.Index, action.AddrArg.Value));
		}
		InstSeq.Add(I.MFU_ACT1_COMPUTE((GP_REGISTER)action.AddrD1.Index, (GP_REGISTER)action.AddrS1.Index, (GP_REGISTER)action.AddrS2.Index, (GP_REGISTER)action.AddrArg.Index));
	}

	private void Visit(GnneActionMfuTransposeConf action)
	{
		Call item = I.MFU_TRANSPOSE_CONF((SHAPE_REGISTER)action.StrideD.Index, (SHAPE_REGISTER)action.StrideS.Index, ToL2Type(action.L2Datatype), action.Permute);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionMfuTranspose action)
	{
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value));
		}
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value));
		}
		InstSeq.Add(I.MFU_TRANSPOSE((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.AddrS.Index, (SHAPE_REGISTER)action.Shape.Index));
	}

	private void Visit(GnneActionMfuPdp1Conf1 action)
	{
		Call item = I.MFU_PDP1_CONF1(action.StrideW, action.StrideH, (SHAPE_REGISTER)action.StrideS.Index, action.Funct2, (SHAPE_REGISTER)action.StrideD.Index);
		InstSeq.Add(item);
	}

	private void Visit(GnneActionMfuPdp1Conf2 action)
	{
		if (action.CountW.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.CountW.Index, action.CountW.Value));
		}
		if (action.CountH.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.CountH.Index, action.CountH.Value));
		}
		if (action.PeH.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PeH.Index, action.PeH.Value));
		}
		if (action.PeLastH.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PeLastH.Index, action.PeLastH.Value));
		}
		InstSeq.Add(I.MFU_PDP1_CONF2((GP_REGISTER)action.CountW.Index, (GP_REGISTER)action.CountH.Index, (GP_REGISTER)action.PeH.Index, (GP_REGISTER)action.PeLastH.Index));
	}

	private void Visit(GnneActionMfuPdp1Conf3 action)
	{
		if (action.PeChannels.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PeChannels.Index, action.PeChannels.Value));
		}
		if (action.PeLastChannels.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PeLastChannels.Index, action.PeLastChannels.Value));
		}
		if (action.PadValue.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PadValue.Index, action.PadValue.Value));
		}
		InstSeq.Add(I.MFU_PDP1_CONF3((GP_REGISTER)action.PeChannels.Index, (GP_REGISTER)action.PeLastChannels.Index, (GP_REGISTER)action.PadValue.Index, (SHAPE_REGISTER)action.Pad.Index));
	}

	private void Visit(GnneActionMfuPdp1Conf4 action)
	{
		if (action.WindowW.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.WindowW.Index, action.WindowW.Value));
		}
		if (action.WindowH.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.WindowH.Index, action.WindowH.Value));
		}
		if (action.Scale.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Scale.Index, action.Scale.Value));
		}
		InstSeq.Add(I.MFU_PDP1_CONF4((GP_REGISTER)action.WindowW.Index, (GP_REGISTER)action.WindowH.Index, (GP_REGISTER)action.Scale.Index, action.EnableH2C, action.EnableBw));
	}

	private void Visit(GnneActionMfuPdp1ConfQuant action)
	{
		if (action.Scale.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Scale.Index, action.Scale.Value));
		}
		if (action.Bias.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Bias.Index, action.Bias.Value));
		}
		InstSeq.Add(I.MFU_PDP1_CONF_QUANT((GP_REGISTER)action.Scale.Index, (GP_REGISTER)action.Bias.Index, ToQuantType(action.QuantType), MFU_CONF_FUNCTION.pdp1_conf_quant, action.RshiftBits));
	}

	private void Visit(GnneActionMfuPdp1ConfDeq action)
	{
		if (action.Scale.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Scale.Index, action.Scale.Value));
		}
		if (action.Bias.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Bias.Index, action.Bias.Value));
		}
		InstSeq.Add(I.MFU_PDP1_CONF_DEQ((GP_REGISTER)action.Scale.Index, (GP_REGISTER)action.Bias.Index, ToQuantType(action.QuantType), MFU_CONF_FUNCTION.pdp1_conf_deq, action.RshiftBits));
	}

	private void Visit(GnneActionMfuPdp1Compute action)
	{
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value));
		}
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value));
		}
		InstSeq.Add(I.MFU_PDP1_COMPUTE((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.AddrS.Index, (SHAPE_REGISTER)action.Shape.Index));
	}

	private void Visit(GnneActionMfuMemset action)
	{
		if (action.AddrD.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrD.Index, action.AddrD.Value));
		}
		if (action.Value.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Value.Index, action.Value.Value));
		}
		InstSeq.Add(I.MFU_MEMSET((GP_REGISTER)action.AddrD.Index, (GP_REGISTER)action.Value.Index, (SHAPE_REGISTER)action.Stride.Index, (SHAPE_REGISTER)action.Shape.Index, ToL2Type(action.L2Datatype)));
	}

	private void Visit(GnneActionPuPdp0WConf action)
	{
		InstSeq.Add(I.PU_PDP0_W_CONF(action.TcuId, action.PuId, action.KernelH, action.KernelW));
	}

	private void Visit(GnneActionPuPdp0ModeConf action)
	{
		InstSeq.Add(I.PU_PDP0_MODE_CONF(action.TcuId, action.PuId, action.Mode));
	}

	private void Visit(GnneActionPuPdp0FetchifConf1 action)
	{
		InstSeq.Add(I.PU_PDP0_FETCHIF_CONF1(action.TcuId, action.PuId, action.StrideW, action.StrideH));
	}

	private void Visit(GnneActionPuPdp0FetchifConf2 action)
	{
		if (action.Gic.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Gic.Index, action.Gic.Value));
		}
		if (action.GicLast.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.GicLast.Index, action.GicLast.Value));
		}
		InstSeq.Add(I.PU_PDP0_FETCHIF_CONF2(action.TcuId, action.PuId, (GP_REGISTER)action.Gic.Index, (GP_REGISTER)action.GicLast.Index));
	}

	private void Visit(GnneActionPuPdp0FetchifConf3 action)
	{
		InstSeq.Add(I.PU_PDP0_FETCHIF_CONF3(action.TcuId, action.PuId, (SHAPE_REGISTER)action.Shape.Index));
	}

	private void Visit(GnneActionPuPdp0FetchifConf4 action)
	{
		if (action.PadValue.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.PadValue.Index, action.PadValue.Value));
		}
		InstSeq.Add(I.PU_PDP0_FETCHIF_CONF4(action.TcuId, action.PuId, (GP_REGISTER)action.PadValue.Index, (SHAPE_REGISTER)action.Pad.Index));
	}

	private void Visit(GnneActionPuPdp0ConfDeq action)
	{
		if (action.Bx.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Bx.Index, action.Bx.Value));
		}
		InstSeq.Add(I.PU_PDP0_CONF_DEQ(action.TcuId, action.PuId, (GP_REGISTER)action.Bx.Index, ToQuantType(action.QuantType)));
	}

	private void Visit(GnneActionPuPdp0OfConf action)
	{
		InstSeq.Add(I.PU_PDP0_OF_CONF(action.TcuId, action.PuId, (SHAPE_REGISTER)action.StrideD.Index, (SHAPE_REGISTER)action.ShapeD.Index));
	}

	private void Visit(GnneActionPuPdp0Compute action)
	{
		if (action.AddrS.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.AddrS.Index, action.AddrS.Value));
		}
		InstSeq.Add(I.PU_PDP0_COMPUTE(action.TcuId, (GP_REGISTER)action.AddrS.Index));
	}

	private void Visit(GnneActionCcrDecl action)
	{
		if (action.Num.NeedRenewal)
		{
			InstSeq.Add(I.LoadImm((GP_REGISTER)action.Num.Index, action.Num.Value));
		}
		InstSeq.Add(I.CCR_DECL((GP_REGISTER)action.Num.Index));
	}

	private void Visit(GnneActionCcrSet action)
	{
		InstSeq.Add(I.CCR_SET(action.Ccr, action.Value));
	}

	private void Visit(GnneActionCcrClr action)
	{
		InstSeq.Add(I.CCR_CLR(action.Ccr));
	}

	private void Visit(GnneActionExtrw action)
	{
		InstSeq.Add(I.LoadImm((GP_REGISTER)action.Rs, action.S_value.Value));
		InstSeq.Add(I.EXTRW(action.Extrd, (GP_REGISTER)action.Rs, action.Imm));
	}

	private void Visit(GnneActionExtraw action)
	{
		InstSeq.Add(I.LoadImm((GP_REGISTER)action.Rs, action.Value.Value));
		InstSeq.Add(I.EXTRAW(action.Extrd, (GP_REGISTER)action.Rs, action.Imm));
	}

	private void Visit(GnneActionAi2dCompute action)
	{
		Call item = I.AI2D_COMPUTE();
		InstSeq.Add(item);
	}
}
