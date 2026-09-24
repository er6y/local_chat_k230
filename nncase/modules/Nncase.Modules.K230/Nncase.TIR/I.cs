using Nncase.IR;
using Nncase.TIR.Instructions;

namespace Nncase.TIR;

public static class I
{
	public static Call LoadImm(GP_REGISTER rd, Expr value)
	{
		return new Call(new LoadImm(rd), value);
	}

	public static Call LoadDdrAddr(GP_REGISTER rd_basement, GP_REGISTER rd_target, Expr basement, Expr offset)
	{
		return new Call(new LoadDDrAddr(rd_target, rd_basement), basement, offset);
	}

	public static Sequential Push(GP_REGISTER rs)
	{
		return T.Sequential(ADDI(R.rsp, R.rsp, -4), SW(R.rsp, rs, 0));
	}

	public static Call Mov(GP_REGISTER rd, GP_REGISTER rs)
	{
		return ADD(rd, R.r0, rs);
	}

	public static Call SUBI(GP_REGISTER rd, Expr imm)
	{
		return ADDI(rd, rd, -imm);
	}

	public static Call LUI(GP_REGISTER rd, Expr imm)
	{
		return new Call(new LUI(rd), imm);
	}

	public static Call AUIPC(GP_REGISTER rd, Expr imm)
	{
		return new Call(new AUIPC(rd), imm);
	}

	public static Call ADDI(GP_REGISTER rd, GP_REGISTER rs, Expr imm, ARITHMETIC_IMM_FUNCTION funct5 = ARITHMETIC_IMM_FUNCTION.addi)
	{
		return new Call(new ADDI(rd, rs, funct5), imm);
	}

	public static Call ADD(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.add, Expr? reserved0 = null)
	{
		return new Call(new ADD(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call SUB(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.sub, Expr? reserved0 = null)
	{
		return new Call(new SUB(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call MUL(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.mul, Expr? reserved0 = null)
	{
		return new Call(new MUL(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call DIV(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.div, Expr? reserved0 = null)
	{
		return new Call(new DIV(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call DIVU(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.divu, Expr? reserved0 = null)
	{
		return new Call(new DIVU(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call REM(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.rem, Expr? reserved0 = null)
	{
		return new Call(new REM(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call REMU(GP_REGISTER rd, GP_REGISTER rs1, GP_REGISTER rs2, ARITHMETIC_FUNCTION funct5 = ARITHMETIC_FUNCTION.remu, Expr? reserved0 = null)
	{
		return new Call(new REMU(rd, rs1, funct5, rs2), reserved0 ?? ((Expr)0));
	}

	public static Call LW(GP_REGISTER rd, GP_REGISTER rs, Expr offset, LOAD_FUNCTION funct3 = LOAD_FUNCTION.lw)
	{
		return new Call(new LW(rd, rs, funct3), offset);
	}

	public static Call LH(GP_REGISTER rd, GP_REGISTER rs, Expr offset, LOAD_FUNCTION funct3 = LOAD_FUNCTION.lh)
	{
		return new Call(new LH(rd, rs, funct3), offset);
	}

	public static Call LHU(GP_REGISTER rd, GP_REGISTER rs, Expr offset, LOAD_FUNCTION funct3 = LOAD_FUNCTION.lhu)
	{
		return new Call(new LHU(rd, rs, funct3), offset);
	}

	public static Call LB(GP_REGISTER rd, GP_REGISTER rs, Expr offset, LOAD_FUNCTION funct3 = LOAD_FUNCTION.lb)
	{
		return new Call(new LB(rd, rs, funct3), offset);
	}

	public static Call LBU(GP_REGISTER rd, GP_REGISTER rs, Expr offset, LOAD_FUNCTION funct3 = LOAD_FUNCTION.lbu)
	{
		return new Call(new LBU(rd, rs, funct3), offset);
	}

	public static Call SW(GP_REGISTER rd, GP_REGISTER rs, Expr offset, STORE_FUNCTION funct3 = STORE_FUNCTION.sw)
	{
		return new Call(new SW(rd, rs, funct3), offset);
	}

	public static Call SH(GP_REGISTER rd, GP_REGISTER rs, Expr offset, STORE_FUNCTION funct3 = STORE_FUNCTION.sh)
	{
		return new Call(new SH(rd, rs, funct3), offset);
	}

	public static Call SB(GP_REGISTER rd, GP_REGISTER rs, Expr offset, STORE_FUNCTION funct3 = STORE_FUNCTION.sb)
	{
		return new Call(new SB(rd, rs, funct3), offset);
	}

	public static Call BEQ(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.beq)
	{
		return new Call(new BEQ(rs1, rs2, funct3), offset);
	}

	public static Call BNE(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.bne)
	{
		return new Call(new BNE(rs1, rs2, funct3), offset);
	}

	public static Call BLT(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.blt)
	{
		return new Call(new BLT(rs1, rs2, funct3), offset);
	}

	public static Call BLTU(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.bltu)
	{
		return new Call(new BLTU(rs1, rs2, funct3), offset);
	}

	public static Call BGE(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.bge)
	{
		return new Call(new BGE(rs1, rs2, funct3), offset);
	}

	public static Call BGEU(GP_REGISTER rs1, GP_REGISTER rs2, Expr offset, BRANCH_FUNCTION funct3 = BRANCH_FUNCTION.bgeu)
	{
		return new Call(new BGEU(rs1, rs2, funct3), offset);
	}

	public static Call JAL(GP_REGISTER rd, Expr offset)
	{
		return new Call(new JAL(rd), offset);
	}

	public static Call JALR(GP_REGISTER rd, GP_REGISTER rs, Expr offset, Expr? reserved0 = null)
	{
		return new Call(new JALR(rd, rs), reserved0 ?? ((Expr)0), offset);
	}

	public static Call INTR(GP_REGISTER rs, Expr? reserved0 = null)
	{
		return new Call(new INTR(rs), reserved0 ?? ((Expr)0));
	}

	public static Call END(GP_REGISTER rs, Expr? reserved0 = null)
	{
		return new Call(new END(rs), reserved0 ?? ((Expr)0));
	}

	public static Call FENCE(Expr? reserved0 = null)
	{
		return new Call(new FENCE(), reserved0 ?? ((Expr)0));
	}

	public static Call FENCE_I(Expr? reserved0 = null)
	{
		return new Call(new FENCE_I(), reserved0 ?? ((Expr)0));
	}

	public static Call EXTRW(Expr extrd, GP_REGISTER rs, Expr imm)
	{
		return new Call(new EXTRW(rs), extrd, imm);
	}

	public static Call EXTRAW(Expr extrd, GP_REGISTER rs, Expr imm)
	{
		return new Call(new EXTRAW(rs), extrd, imm);
	}

	public static Call CCR_DECL(GP_REGISTER rnum, Expr? reserved0 = null)
	{
		return new Call(new CCR_DECL(rnum), reserved0 ?? ((Expr)0));
	}

	public static Call CCR_SET(Expr ccr, Expr value)
	{
		return new Call(new CCR_SET(), ccr, value);
	}

	public static Call CCR_CLR(Expr ccr, Expr? reserved0 = null)
	{
		return new Call(new CCR_CLR(), ccr, reserved0 ?? ((Expr)0));
	}

	public static Call MMU_CONF(GP_REGISTER rstart, GP_REGISTER rdepth, Expr mmu_id, Expr? reserved0 = null)
	{
		return new Call(new MMU_CONF(rstart, rdepth), mmu_id, reserved0 ?? ((Expr)0));
	}

	public static Call MMU_SETID(GP_REGISTER rd, Expr mmu_id)
	{
		return new Call(new MMU_SETID(rd), mmu_id);
	}

	public static Call SS_PACK_SHAPE(GP_REGISTER rn, GP_REGISTER rc, GP_REGISTER rh, GP_REGISTER rw, SHAPE_REGISTER rss, Expr? reserved0 = null)
	{
		return new Call(new SS_PACK_SHAPE(rn, rc, rh, rw, rss), reserved0 ?? ((Expr)0));
	}

	public static Call SS_PACK_STRIDE(GP_REGISTER rn, GP_REGISTER rc, GP_REGISTER rh, SHAPE_REGISTER rss, Expr? reserved0 = null, Expr? reserved1 = null)
	{
		return new Call(new SS_PACK_STRIDE(rn, rc, rh, rss), reserved0 ?? ((Expr)0), reserved1 ?? ((Expr)0));
	}

	public static Call L2_LOAD_CONF(SHAPE_REGISTER rstride_d, SHAPE_REGISTER rstride_s, L2_DATATYPE l2_datatype, DDR_DATATYPE ddr_datatype, Expr? reserved0 = null)
	{
		return new Call(new L2_LOAD_CONF(rstride_d, rstride_s, l2_datatype, ddr_datatype), reserved0 ?? ((Expr)0));
	}

	public static Call L2_LOAD_W_CONF(GP_REGISTER rlen_compressed, GP_REGISTER rlen_decompressed, L2_DATATYPE l2_datatype, DDR_DATATYPE ddr_datatype, Expr enable_decompress, Expr? reserved0 = null)
	{
		return new Call(new L2_LOAD_W_CONF(rlen_compressed, rlen_decompressed, l2_datatype, ddr_datatype), enable_decompress, reserved0 ?? ((Expr)0));
	}

	public static Call L2_LOAD_W(GP_REGISTER raddr_d, GP_REGISTER raddr_s, GP_REGISTER rvalid_c_num, Expr? reserved0 = null)
	{
		return new Call(new L2_LOAD_W(raddr_d, raddr_s, rvalid_c_num), reserved0 ?? ((Expr)0));
	}

	public static Call L2_STORE_CONF(SHAPE_REGISTER rstride_d, SHAPE_REGISTER rstride_s, L2_DATATYPE l2_datatype, DDR_DATATYPE ddr_datatype, Expr? reserved0 = null)
	{
		return new Call(new L2_STORE_CONF(rstride_d, rstride_s, l2_datatype, ddr_datatype), reserved0 ?? ((Expr)0));
	}

	public static Call L2_LOAD(GP_REGISTER raddr_d, GP_REGISTER raddr_s, SHAPE_REGISTER rshape, Expr? reserved0 = null)
	{
		return new Call(new L2_LOAD(raddr_d, raddr_s, rshape), reserved0 ?? ((Expr)0));
	}

	public static Call L2_STORE(GP_REGISTER raddr_d, GP_REGISTER raddr_s, SHAPE_REGISTER rshape, Expr? reserved0 = null)
	{
		return new Call(new L2_STORE(raddr_d, raddr_s, rshape), reserved0 ?? ((Expr)0));
	}

	public static Call DM_CONF_BROADCAST(Expr tcu_id, Expr broadcast_if, Expr broadcast_w, Expr psum_cascade, Expr? reserved0 = null)
	{
		return new Call(new DM_CONF_BROADCAST(), tcu_id, broadcast_if, broadcast_w, psum_cascade, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_L1_CONF(Expr tcu_id, Expr pu_id, SHAPE_REGISTER rstride_s, L2_DATATYPE datatype, L1_TYPE l1_type, DM_CONF_FUNCTION funct4 = DM_CONF_FUNCTION.load_l1_conf, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_L1_CONF(funct4, rstride_s, datatype, l1_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_W_CONF(Expr tcu_id, Expr pu_id, Expr kernel_h, Expr kernel_w, GP_REGISTER rstride_oc, DM_CONF_FUNCTION funct4 = DM_CONF_FUNCTION.load_w_conf)
	{
		return new Call(new DM_LOAD_W_CONF(funct4, rstride_oc), tcu_id, pu_id, kernel_h, kernel_w);
	}

	public static Call DM_LOAD_W_CONF2(Expr tcu_id, Expr pu_id, GP_REGISTER rgroups, GP_REGISTER rgoc, DM_CONF_FUNCTION funct4 = DM_CONF_FUNCTION.load_w_conf2, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_W_CONF2(funct4, rgroups, rgoc), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_W_CONF_DEQ(Expr tcu_id, Expr pu_id, QUANT_TYPE quant_type, DM_CONF_FUNCTION funct4 = DM_CONF_FUNCTION.load_w_conf_deq, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_W_CONF_DEQ(funct4, quant_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_STORE_OF_CONF(Expr tcu_id, Expr pu_id, SHAPE_REGISTER rstride_d, L2_DATATYPE datatype, DM_CONF_FUNCTION funct4 = DM_CONF_FUNCTION.store_of_conf, Expr? reserved0 = null)
	{
		return new Call(new DM_STORE_OF_CONF(funct4, rstride_d, datatype), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_L1(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_s, GP_REGISTER rhtoc_window, SHAPE_REGISTER rshape, L1_TYPE l1_type, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_L1(raddr_s, rhtoc_window, rshape, l1_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_W(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_s, GP_REGISTER raddr_bw, SHAPE_REGISTER r_iochannels, DM_LOAD_W_DEST dest_type, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_W(raddr_s, raddr_bw, r_iochannels, dest_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call DM_LOAD_ACT0(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_s, GP_REGISTER rlen, ACT0_CHANNEL dest_channel, Expr is_by_channel, Expr? reserved0 = null)
	{
		return new Call(new DM_LOAD_ACT0(raddr_s, rlen, dest_channel), tcu_id, pu_id, is_by_channel, reserved0 ?? ((Expr)0));
	}

	public static Call DM_STORE_OF(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_d, SHAPE_REGISTER rshape, ACT0_CHANNEL src_channel, Expr? reserved0 = null)
	{
		return new Call(new DM_STORE_OF(raddr_d, rshape, src_channel), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_FETCHIF_CONF1(Expr tcu_id, Expr pu_id, Expr stride_w, Expr stride_h, SHAPE_REGISTER rstride_s, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_fetchif_conf1, Expr? reserved0 = null)
	{
		return new Call(new PU_FETCHIF_CONF1(funct4, rstride_s), tcu_id, pu_id, stride_w, stride_h, reserved0 ?? ((Expr)0));
	}

	public static Call PU_FETCHIF_CONF2(Expr tcu_id, Expr pu_id, GP_REGISTER rgic, GP_REGISTER rgic_last, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_fetchif_conf2, Expr? reserved0 = null)
	{
		return new Call(new PU_FETCHIF_CONF2(funct4, rgic, rgic_last), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_FETCHIF_CONF3(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_s, GP_REGISTER rgroups, SHAPE_REGISTER rshape, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_fetchif_conf3, Expr? reserved0 = null)
	{
		return new Call(new PU_FETCHIF_CONF3(funct4, raddr_s, rgroups, rshape), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_FETCHIF_CONF4(Expr tcu_id, Expr pu_id, GP_REGISTER rpad_value, SHAPE_REGISTER sspad, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_fetchif_conf4, Expr? reserved0 = null, Expr? reserved1 = null)
	{
		return new Call(new PU_FETCHIF_CONF4(funct4, rpad_value, sspad), tcu_id, pu_id, reserved0 ?? ((Expr)0), reserved1 ?? ((Expr)0));
	}

	public static Call PU_FETCHIF_CONF_DEQ(Expr tcu_id, Expr pu_id, GP_REGISTER ric, GP_REGISTER rbx, QUANT_TYPE quant_type, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_fetchif_conf_deq, Expr? reserved0 = null)
	{
		return new Call(new PU_FETCHIF_CONF_DEQ(funct4, ric, rbx, quant_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_W_CONF(Expr tcu_id, Expr pu_id, Expr kernel_h, Expr kernel_w, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_w_conf, Expr? reserved0 = null)
	{
		return new Call(new PU_W_CONF(funct4), tcu_id, pu_id, kernel_h, kernel_w, reserved0 ?? ((Expr)0));
	}

	public static Call PU_OF_CONF1(Expr tcu_id, Expr pu_id, GP_REGISTER rgoc, GP_REGISTER rgoc_last, SHAPE_REGISTER rstride_d, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_of_conf1, Expr? reserved0 = null)
	{
		return new Call(new PU_OF_CONF1(funct4, rgoc, rgoc_last, rstride_d), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_OF_CONF2(Expr tcu_id, Expr pu_id, GP_REGISTER raddr_d, SHAPE_REGISTER rshape_d, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_of_conf2, Expr? reserved0 = null, Expr? reserved1 = null)
	{
		return new Call(new PU_OF_CONF2(funct4, raddr_d, rshape_d), tcu_id, pu_id, reserved0 ?? ((Expr)0), reserved1 ?? ((Expr)0));
	}

	public static Call PU_COMPUTE_CONF(Expr tcu_id, Expr pu_id, Expr load_psum, Expr clr_psum, PU_OUTPUT_DEST dest_target, Expr release_if, PU_COMPUTE_MODE mode, PU_CONF_FUNCTION funct4 = PU_CONF_FUNCTION.pu_compute_conf, Expr? reserved0 = null)
	{
		return new Call(new PU_COMPUTE_CONF(funct4, dest_target, mode), tcu_id, pu_id, load_psum, clr_psum, release_if, reserved0 ?? ((Expr)0));
	}

	public static Call PU_COMPUTE(Expr tcu_id, PU_OF_SHIFT_MODE of_shift_mode, Expr? reserved0 = null)
	{
		return new Call(new PU_COMPUTE(of_shift_mode), tcu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_FORWARD_PSUM(Expr tcu_id, Expr pu_id, GP_REGISTER raddr, GP_REGISTER rlen, Expr? reserved0 = null)
	{
		return new Call(new PU_FORWARD_PSUM(raddr, rlen), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_MODE_CONF(Expr tcu_id, Expr pu_id, PU_PDP0_MODE mode, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_mode_conf, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_MODE_CONF(funct4, mode), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_FETCHIF_CONF1(Expr tcu_id, Expr pu_id, Expr stride_w, Expr stride_h, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf1, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_FETCHIF_CONF1(funct4), tcu_id, pu_id, stride_w, stride_h, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_FETCHIF_CONF2(Expr tcu_id, Expr pu_id, GP_REGISTER rgic, GP_REGISTER rgic_last, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf2, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_FETCHIF_CONF2(funct4, rgic, rgic_last), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_FETCHIF_CONF3(Expr tcu_id, Expr pu_id, SHAPE_REGISTER rshape, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf3, Expr? reserved0 = null, Expr? reserved1 = null)
	{
		return new Call(new PU_PDP0_FETCHIF_CONF3(funct4, rshape), tcu_id, pu_id, reserved0 ?? ((Expr)0), reserved1 ?? ((Expr)0));
	}

	public static Call PU_PDP0_FETCHIF_CONF4(Expr tcu_id, Expr pu_id, GP_REGISTER rpad_value, SHAPE_REGISTER sspad, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf4, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_FETCHIF_CONF4(funct4, rpad_value, sspad), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_CONF_DEQ(Expr tcu_id, Expr pu_id, GP_REGISTER rbx, QUANT_TYPE quant_type, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf_deq, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_CONF_DEQ(funct4, rbx, quant_type), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_W_CONF(Expr tcu_id, Expr pu_id, Expr kernel_h, Expr kernel_w, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_w_conf, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_W_CONF(funct4), tcu_id, pu_id, kernel_h, kernel_w, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_OF_CONF(Expr tcu_id, Expr pu_id, SHAPE_REGISTER rstride_d, SHAPE_REGISTER rshape_d, PU_PDP0_CONF_FUNCTION funct4 = PU_PDP0_CONF_FUNCTION.pdp0_of_conf, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_OF_CONF(funct4, rstride_d, rshape_d), tcu_id, pu_id, reserved0 ?? ((Expr)0));
	}

	public static Call PU_PDP0_COMPUTE(Expr tcu_id, GP_REGISTER raddr_s, Expr? reserved0 = null)
	{
		return new Call(new PU_PDP0_COMPUTE(raddr_s), tcu_id, reserved0 ?? ((Expr)0));
	}

	public static Call ACT0_SRC1_CONF(Expr tcu_id, Expr pu_id, ACT0_CHANNEL channel, SHAPE_REGISTER rshape, ACT0_CONF_FUNCTION funct3 = ACT0_CONF_FUNCTION.act_src1_conf, Expr? rshift_bits = null, Expr? reserved0 = null)
	{
		return new Call(new ACT0_SRC1_CONF(funct3, channel, rshape), tcu_id, pu_id, rshift_bits ?? ((Expr)0), reserved0 ?? ((Expr)0));
	}

	public static Call ACT0_COMPUTE(GP_REGISTER raddr_d, Expr tcu_id, ACT0_CHANNEL channel, ACT0_OUTPUT_DEST target, ACT_OUTPUT_TYPE dest_datatype, Expr is_by_channel, Expr? reserved0 = null, Expr? reserved1 = null)
	{
		return new Call(new ACT0_COMPUTE(raddr_d, channel, target, dest_datatype), reserved0 ?? ((Expr)0), tcu_id, is_by_channel, reserved1 ?? ((Expr)0));
	}

	public static Call MFU_MEMCPY(GP_REGISTER raddr_d, GP_REGISTER raddr_s, SHAPE_REGISTER rstride_d, SHAPE_REGISTER rstride_s, SHAPE_REGISTER rshape, Expr? reserved0 = null)
	{
		return new Call(new MFU_MEMCPY(raddr_d, raddr_s, rstride_d, rstride_s, rshape), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_MEMSET(GP_REGISTER raddr_d, GP_REGISTER rv, SHAPE_REGISTER rstride, SHAPE_REGISTER rshape, L2_DATATYPE l2_datatype, Expr? reserved0 = null)
	{
		return new Call(new MFU_MEMSET(raddr_d, rv, rstride, rshape, l2_datatype), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_TRANSPOSE_CONF(SHAPE_REGISTER rstride_d, SHAPE_REGISTER rstride_s, L2_DATATYPE l2_datatype, MFU_TRANS_PERMUTE permute, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.transpose_conf, Expr? reserved0 = null)
	{
		return new Call(new MFU_TRANSPOSE_CONF(funct5, rstride_d, rstride_s, l2_datatype, permute), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_TRANSPOSE(GP_REGISTER raddr_d, GP_REGISTER raddr_s, SHAPE_REGISTER rshape, Expr? reserved0 = null)
	{
		return new Call(new MFU_TRANSPOSE(raddr_d, raddr_s, rshape), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_CONF1(Expr stride_w, Expr stride_h, SHAPE_REGISTER rstride_s, PDP_FUNCTION funct2, SHAPE_REGISTER rstride_d, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf1, Expr? reserved0 = null)
	{
		return new Call(new MFU_PDP1_CONF1(funct5, rstride_s, funct2, rstride_d), stride_w, stride_h, reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_CONF2(GP_REGISTER rcount_w, GP_REGISTER rcount_h, GP_REGISTER rpe_h, GP_REGISTER rpe_last_h, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf2)
	{
		return new Call(new MFU_PDP1_CONF2(funct5, rcount_w, rcount_h, rpe_h, rpe_last_h));
	}

	public static Call MFU_PDP1_CONF3(GP_REGISTER rpe_channels, GP_REGISTER rpe_last_channels, GP_REGISTER rpad_value, SHAPE_REGISTER sspad, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf3, Expr? reserved0 = null)
	{
		return new Call(new MFU_PDP1_CONF3(funct5, rpe_channels, rpe_last_channels, rpad_value, sspad), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_CONF4(GP_REGISTER rwindow_w, GP_REGISTER rwindow_h, GP_REGISTER rscale, Expr enable_h2c, Expr enable_bw, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf4, Expr? reserved0 = null)
	{
		return new Call(new MFU_PDP1_CONF4(funct5, rwindow_w, rwindow_h, rscale), enable_h2c, enable_bw, reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_CONF_DEQ(GP_REGISTER rscale, GP_REGISTER rbias, QUANT_TYPE quant_type, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf_deq, Expr? rshift_bits = null, Expr? reserved0 = null)
	{
		return new Call(new MFU_PDP1_CONF_DEQ(funct5, rscale, rbias, quant_type), rshift_bits ?? ((Expr)0), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_CONF_QUANT(GP_REGISTER rscale, GP_REGISTER rbias, QUANT_TYPE quant_type, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.pdp1_conf_quant, Expr? rshift_bits = null, Expr? reserved0 = null)
	{
		return new Call(new MFU_PDP1_CONF_QUANT(funct5, rscale, rbias, quant_type), rshift_bits ?? ((Expr)0), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_PDP1_COMPUTE(GP_REGISTER raddr_d, GP_REGISTER raddr_s, SHAPE_REGISTER rshape, Expr? reserved1 = null)
	{
		return new Call(new MFU_PDP1_COMPUTE(raddr_d, raddr_s, rshape), reserved1 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_STRIDE(SHAPE_REGISTER rstride_s1, SHAPE_REGISTER rstride_s2, SHAPE_REGISTER rstride_d1, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_stride, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_STRIDE(funct5, rstride_s1, rstride_s2, rstride_d1), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_SRC1(GP_REGISTER rslice, GP_REGISTER rright_repeats, GP_REGISTER rslice_repeats, Expr sid, SLICE_LOCATION slice_loc, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_src1, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_SRC1(funct5, rslice, rright_repeats, rslice_repeats, slice_loc), sid, reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_SRC2(GP_REGISTER rleft_repeats, SHAPE_REGISTER rshape, Expr sid, ACT1_SOURCE_TYPE source_type, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_src2, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_SRC2(funct5, rleft_repeats, rshape, source_type), sid, reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_DEST(GP_REGISTER rlen, SHAPE_REGISTER rshape, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_dest, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_DEST(funct5, rlen, rshape), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_DEQ(GP_REGISTER rscale, GP_REGISTER rbias, QUANT_TYPE quant_type, Expr sid, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_deq, Expr? rshift_bits = null, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_DEQ(funct5, rscale, rbias, quant_type), sid, rshift_bits ?? ((Expr)0), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF_QUANT(QUANT_TYPE quant_type, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf_quant, Expr? rshift_bits = null, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF_QUANT(funct5, quant_type), rshift_bits ?? ((Expr)0), reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_CONF(MFU_ACT1_FUNCTION funct4, Expr is_by_channel, Expr is_16_segments, MFU_CONF_FUNCTION funct5 = MFU_CONF_FUNCTION.act1_conf, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_CONF(funct5, funct4), is_by_channel, is_16_segments, reserved0 ?? ((Expr)0));
	}

	public static Call MFU_ACT1_COMPUTE(GP_REGISTER raddr_d1, GP_REGISTER raddr_s1, GP_REGISTER raddr_s2, GP_REGISTER raddr_arg, Expr? reserved0 = null)
	{
		return new Call(new MFU_ACT1_COMPUTE(raddr_d1, raddr_s1, raddr_s2, raddr_arg), reserved0 ?? ((Expr)0));
	}

	public static Call AI2D_COMPUTE(Expr? reserved0 = null)
	{
		return new Call(new AI2D_COMPUTE(), reserved0 ?? ((Expr)0));
	}
}
