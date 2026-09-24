using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_CONF_DEQ : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_CONF_DEQ?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_CONF_DEQ), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_CONF_DEQ), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_CONF_DEQ), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rbx { get; }

	public QUANT_TYPE quant_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rbx, 5);
		bitWriter.Write(quant_type, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 8);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_CONF_DEQ(PU_PDP0_CONF_FUNCTION funct4, GP_REGISTER rbx, QUANT_TYPE quantType)
	{
		this.funct4 = funct4;
		this.rbx = rbx;
		quant_type = quantType;
	}

	public PU_PDP0_CONF_DEQ With(PU_PDP0_CONF_FUNCTION? funct4 = null, GP_REGISTER? rbx = null, QUANT_TYPE? quantType = null)
	{
		return new PU_PDP0_CONF_DEQ(funct4 ?? this.funct4, rbx ?? this.rbx, quantType ?? quant_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_CONF_DEQ);
	}

	public bool Equals(PU_PDP0_CONF_DEQ? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rbx.Equals(other.rbx))
		{
			return quant_type.Equals(other.quant_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rbx, quant_type));
	}
}
