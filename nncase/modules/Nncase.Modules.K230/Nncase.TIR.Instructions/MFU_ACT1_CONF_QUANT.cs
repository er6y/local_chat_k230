using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_QUANT : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_QUANT?>
{
	public static readonly ParameterInfo rshift_bits = new ParameterInfo(typeof(MFU_ACT1_CONF_QUANT), 0, "rshift_bits", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_QUANT), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public QUANT_TYPE quant_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(quant_type, 2);
		bitWriter.Write(((TensorConst)call[rshift_bits]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 13);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_QUANT(MFU_CONF_FUNCTION funct5, QUANT_TYPE quantType)
	{
		this.funct5 = funct5;
		quant_type = quantType;
	}

	public MFU_ACT1_CONF_QUANT With(MFU_CONF_FUNCTION? funct5 = null, QUANT_TYPE? quantType = null)
	{
		return new MFU_ACT1_CONF_QUANT(funct5 ?? this.funct5, quantType ?? quant_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_QUANT);
	}

	public bool Equals(MFU_ACT1_CONF_QUANT? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5))
		{
			return quant_type.Equals(other.quant_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, quant_type));
	}
}
