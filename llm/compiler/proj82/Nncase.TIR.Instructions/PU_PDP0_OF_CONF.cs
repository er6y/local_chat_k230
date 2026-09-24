using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_OF_CONF : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_OF_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_OF_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_OF_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_OF_CONF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public SHAPE_REGISTER rshape_d { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(rshape_d, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_OF_CONF(PU_PDP0_CONF_FUNCTION funct4, SHAPE_REGISTER rstrideD, SHAPE_REGISTER rshapeD)
	{
		this.funct4 = funct4;
		rstride_d = rstrideD;
		rshape_d = rshapeD;
	}

	public PU_PDP0_OF_CONF With(PU_PDP0_CONF_FUNCTION? funct4 = null, SHAPE_REGISTER? rstrideD = null, SHAPE_REGISTER? rshapeD = null)
	{
		return new PU_PDP0_OF_CONF(funct4 ?? this.funct4, rstrideD ?? rstride_d, rshapeD ?? rshape_d);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_OF_CONF);
	}

	public bool Equals(PU_PDP0_OF_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rstride_d.Equals(other.rstride_d))
		{
			return rshape_d.Equals(other.rshape_d);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rstride_d, rshape_d));
	}
}
