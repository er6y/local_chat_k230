using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_MODE_CONF : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_MODE_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_MODE_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_MODE_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_MODE_CONF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public PU_PDP0_MODE mode { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(mode, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_MODE_CONF(PU_PDP0_CONF_FUNCTION funct4, PU_PDP0_MODE mode)
	{
		this.funct4 = funct4;
		this.mode = mode;
	}

	public PU_PDP0_MODE_CONF With(PU_PDP0_CONF_FUNCTION? funct4 = null, PU_PDP0_MODE? mode = null)
	{
		return new PU_PDP0_MODE_CONF(funct4 ?? this.funct4, mode ?? this.mode);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_MODE_CONF);
	}

	public bool Equals(PU_PDP0_MODE_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4))
		{
			return mode.Equals(other.mode);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, mode));
	}
}
