using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_OF_CONF1 : InstructionOp, ISerializeInst, IEquatable<PU_OF_CONF1?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_OF_CONF1), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_OF_CONF1), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_OF_CONF1), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rgoc { get; }

	public GP_REGISTER rgoc_last { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rgoc, 5);
		bitWriter.Write(rgoc_last, 5);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_OF_CONF1(PU_CONF_FUNCTION funct4, GP_REGISTER rgoc, GP_REGISTER rgocLast, SHAPE_REGISTER rstrideD)
	{
		this.funct4 = funct4;
		this.rgoc = rgoc;
		rgoc_last = rgocLast;
		rstride_d = rstrideD;
	}

	public PU_OF_CONF1 With(PU_CONF_FUNCTION? funct4 = null, GP_REGISTER? rgoc = null, GP_REGISTER? rgocLast = null, SHAPE_REGISTER? rstrideD = null)
	{
		return new PU_OF_CONF1(funct4 ?? this.funct4, rgoc ?? this.rgoc, rgocLast ?? rgoc_last, rstrideD ?? rstride_d);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_OF_CONF1);
	}

	public bool Equals(PU_OF_CONF1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rgoc.Equals(other.rgoc) && rgoc_last.Equals(other.rgoc_last))
		{
			return rstride_d.Equals(other.rstride_d);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rgoc, rgoc_last, rstride_d));
	}
}
