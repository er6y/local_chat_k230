using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_FETCHIF_CONF3 : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_FETCHIF_CONF3?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF3), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF3), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF3), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF3), 3, "reserved1", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_FETCHIF_CONF3(PU_PDP0_CONF_FUNCTION funct4, SHAPE_REGISTER rshape)
	{
		this.funct4 = funct4;
		this.rshape = rshape;
	}

	public PU_PDP0_FETCHIF_CONF3 With(PU_PDP0_CONF_FUNCTION? funct4 = null, SHAPE_REGISTER? rshape = null)
	{
		return new PU_PDP0_FETCHIF_CONF3(funct4 ?? this.funct4, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_FETCHIF_CONF3);
	}

	public bool Equals(PU_PDP0_FETCHIF_CONF3? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4))
		{
			return rshape.Equals(other.rshape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rshape));
	}
}
