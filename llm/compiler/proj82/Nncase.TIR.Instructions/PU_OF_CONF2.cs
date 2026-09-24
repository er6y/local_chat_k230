using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_OF_CONF2 : InstructionOp, ISerializeInst, IEquatable<PU_OF_CONF2?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_OF_CONF2), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_OF_CONF2), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_OF_CONF2), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(PU_OF_CONF2), 3, "reserved1", TypePatternUtility.IsIntegralScalar());

	public PU_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER raddr_d { get; }

	public SHAPE_REGISTER rshape_d { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(rshape_d, 3);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_OF_CONF2(PU_CONF_FUNCTION funct4, GP_REGISTER raddrD, SHAPE_REGISTER rshapeD)
	{
		this.funct4 = funct4;
		raddr_d = raddrD;
		rshape_d = rshapeD;
	}

	public PU_OF_CONF2 With(PU_CONF_FUNCTION? funct4 = null, GP_REGISTER? raddrD = null, SHAPE_REGISTER? rshapeD = null)
	{
		return new PU_OF_CONF2(funct4 ?? this.funct4, raddrD ?? raddr_d, rshapeD ?? rshape_d);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_OF_CONF2);
	}

	public bool Equals(PU_OF_CONF2? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && raddr_d.Equals(other.raddr_d))
		{
			return rshape_d.Equals(other.rshape_d);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, raddr_d, rshape_d));
	}
}
