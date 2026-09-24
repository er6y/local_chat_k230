using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

public static class CodeGenUtil
{
	public static List<int> ToStackArgs(IEnumerable<Nncase.TIR.Buffer> parameters, IReadOnlyDictionary<Nncase.TIR.Buffer, (uint Start, uint Seg_offset, uint Size)>? allocation = null, IReadOnlyDictionary<Nncase.TIR.Buffer, int>? constBufferMap = null)
	{
		List<int> list = new List<int>();
		foreach (var item2 in parameters.Reverse().Select((Nncase.TIR.Buffer p, int i) => (p: p, i: i)))
		{
			Nncase.TIR.Buffer item = item2.p;
			List<int> list2 = new List<int>();
			if (item.CheckedShape.IsScalar)
			{
				if (constBufferMap != null)
				{
					if (!constBufferMap.ContainsKey(item))
					{
						throw new InvalidDataException("The K230 Dynamic function Only accept Int32 Params");
					}
					list2.Add(constBufferMap[item]);
				}
				else
				{
					list2.Add(0);
				}
			}
			else
			{
				foreach (Dimension item3 in item.CheckedShape)
				{
					list2.Add(item3.FixedValue);
				}
				list2.Add((int)(allocation?[item].Start ?? 0));
				list2.Add(item.ElemType.ToNncaseTypeCode());
			}
			list2.Reverse();
			list.AddRange(list2);
		}
		return list;
	}

	public static CallableType ToSignature(Tensor[] inputs, Tensor[] outputs)
	{
		Func<Tensor, TensorType> selector = (Tensor t) => (t.Rank == 0) ? TensorType.Scalar(t.ElementType) : new TensorType(t.ElementType, t.Shape);
		return new CallableType(new TupleType(outputs.Select(selector)), new IRArray<IRType>((IEnumerable<IRType>)inputs.Select(selector)));
	}

	public static IEnumerable<Nncase.TIR.Buffer> ToPrimFuncParameters(CallableType signature)
	{
		Nncase.TIR.Buffer buffer2;
		Nncase.TIR.Buffer buffer;
		return (from tt in signature.Parameters.OfType<TensorType>()
			select T.CreateBuffer(tt, MemoryLocation.Input, out buffer2, "var _")).Concat(from tt in ((TupleType)signature.ReturnType).OfType<TensorType>()
			select T.CreateBuffer(tt, MemoryLocation.Output, out buffer, "var _"));
	}

	public static int InstLength(string inst_name)
	{
		return inst_name switch
		{
			"LUI" => 4, 
			"AUIPC" => 4, 
			"ADDI" => 4, 
			"ADD" => 4, 
			"SUB" => 4, 
			"MUL" => 4, 
			"DIV" => 4, 
			"DIVU" => 4, 
			"REM" => 4, 
			"REMU" => 4, 
			"LW" => 4, 
			"LH" => 4, 
			"LHU" => 4, 
			"LB" => 4, 
			"LBU" => 4, 
			"SW" => 4, 
			"SH" => 4, 
			"SB" => 4, 
			"BEQ" => 4, 
			"BNE" => 4, 
			"BLT" => 4, 
			"BLTU" => 4, 
			"BGE" => 4, 
			"BGEU" => 4, 
			"JAL" => 4, 
			"JALR" => 4, 
			"INTR" => 2, 
			"END" => 2, 
			"FENCE" => 2, 
			"FENCE_I" => 2, 
			"EXTRW" => 4, 
			"CCR_DECL" => 2, 
			"CCR_SET" => 2, 
			"CCR_CLR" => 2, 
			"MMU_CONF" => 4, 
			"MMU_SETID" => 2, 
			"SS_PACK_SHAPE" => 4, 
			"SS_PACK_STRIDE" => 4, 
			"L2_LOAD_CONF" => 4, 
			"L2_LOAD_W_CONF" => 4, 
			"L2_LOAD_W" => 4, 
			"L2_STORE_CONF" => 4, 
			"L2_LOAD" => 4, 
			"L2_STORE" => 4, 
			"DM_CONF_BROADCAST" => 2, 
			"DM_LOAD_L1_CONF" => 4, 
			"DM_LOAD_W_CONF" => 4, 
			"DM_LOAD_W_CONF2" => 4, 
			"DM_LOAD_W_CONF_DEQ" => 4, 
			"DM_STORE_OF_CONF" => 4, 
			"DM_LOAD_L1" => 4, 
			"DM_LOAD_W" => 4, 
			"DM_LOAD_ACT0" => 4, 
			"DM_STORE_OF" => 4, 
			"PU_FETCHIF_CONF1" => 4, 
			"PU_FETCHIF_CONF2" => 4, 
			"PU_FETCHIF_CONF3" => 4, 
			"PU_FETCHIF_CONF4" => 4, 
			"PU_FETCHIF_CONF_DEQ" => 4, 
			"PU_W_CONF" => 4, 
			"PU_OF_CONF1" => 4, 
			"PU_OF_CONF2" => 4, 
			"PU_COMPUTE_CONF" => 4, 
			"PU_COMPUTE" => 2, 
			"PU_FORWARD_PSUM" => 4, 
			"PU_PDP0_MODE_CONF" => 4, 
			"PU_PDP0_FETCHIF_CONF1" => 4, 
			"PU_PDP0_FETCHIF_CONF2" => 4, 
			"PU_PDP0_FETCHIF_CONF3" => 4, 
			"PU_PDP0_FETCHIF_CONF4" => 4, 
			"PU_PDP0_CONF_DEQ" => 4, 
			"PU_PDP0_W_CONF" => 4, 
			"PU_PDP0_OF_CONF" => 4, 
			"PU_PDP0_COMPUTE" => 2, 
			"ACT0_SRC1_CONF" => 4, 
			"ACT0_COMPUTE" => 4, 
			"MFU_MEMCPY" => 4, 
			"MFU_MEMSET" => 4, 
			"MFU_TRANSPOSE_CONF" => 4, 
			"MFU_TRANSPOSE" => 4, 
			"MFU_PDP1_CONF1" => 4, 
			"MFU_PDP1_CONF2" => 4, 
			"MFU_PDP1_CONF3" => 4, 
			"MFU_PDP1_CONF4" => 4, 
			"MFU_PDP1_CONF_DEQ" => 4, 
			"MFU_PDP1_CONF_QUANT" => 4, 
			"MFU_PDP1_COMPUTE" => 4, 
			"MFU_ACT1_CONF_STRIDE" => 4, 
			"MFU_ACT1_CONF_SRC1" => 4, 
			"MFU_ACT1_CONF_SRC2" => 4, 
			"MFU_ACT1_CONF_DEST" => 4, 
			"MFU_ACT1_CONF_DEQ" => 4, 
			"MFU_ACT1_CONF_QUANT" => 4, 
			"MFU_ACT1_CONF" => 4, 
			"MFU_ACT1_COMPUTE" => 4, 
			"AI2D_COMPUTE" => 2, 
			_ => throw new NotSupportedException(), 
		};
	}
}
