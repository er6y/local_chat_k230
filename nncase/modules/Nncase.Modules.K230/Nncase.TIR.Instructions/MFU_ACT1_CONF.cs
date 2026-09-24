using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF?>
{
	public static readonly ParameterInfo is_by_channel = new ParameterInfo(typeof(MFU_ACT1_CONF), 0, "is_by_channel", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo is_16_segments = new ParameterInfo(typeof(MFU_ACT1_CONF), 1, "is_16_segments", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public MFU_ACT1_FUNCTION funct4 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[is_by_channel]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[is_16_segments]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 14);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF(MFU_CONF_FUNCTION funct5, MFU_ACT1_FUNCTION funct4)
	{
		this.funct5 = funct5;
		this.funct4 = funct4;
	}

	public MFU_ACT1_CONF With(MFU_CONF_FUNCTION? funct5 = null, MFU_ACT1_FUNCTION? funct4 = null)
	{
		return new MFU_ACT1_CONF(funct5 ?? this.funct5, funct4 ?? this.funct4);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF);
	}

	public bool Equals(MFU_ACT1_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5))
		{
			return funct4.Equals(other.funct4);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, funct4));
	}
}
