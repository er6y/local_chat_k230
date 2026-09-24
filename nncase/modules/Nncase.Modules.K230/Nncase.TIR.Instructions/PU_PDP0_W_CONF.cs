using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_W_CONF : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_W_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_W_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_W_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo kernel_h = new ParameterInfo(typeof(PU_PDP0_W_CONF), 2, "kernel_h", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo kernel_w = new ParameterInfo(typeof(PU_PDP0_W_CONF), 3, "kernel_w", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_W_CONF), 4, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[kernel_h]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[kernel_w]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_W_CONF(PU_PDP0_CONF_FUNCTION funct4)
	{
		this.funct4 = funct4;
	}

	public PU_PDP0_W_CONF With(PU_PDP0_CONF_FUNCTION? funct4 = null)
	{
		return new PU_PDP0_W_CONF(funct4 ?? this.funct4);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_W_CONF);
	}

	public bool Equals(PU_PDP0_W_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return funct4.Equals(other.funct4);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4));
	}
}
