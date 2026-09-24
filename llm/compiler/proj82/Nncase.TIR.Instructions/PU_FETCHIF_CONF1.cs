using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_FETCHIF_CONF1 : InstructionOp, ISerializeInst, IEquatable<PU_FETCHIF_CONF1?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_FETCHIF_CONF1), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_FETCHIF_CONF1), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo stride_w = new ParameterInfo(typeof(PU_FETCHIF_CONF1), 2, "stride_w", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo stride_h = new ParameterInfo(typeof(PU_FETCHIF_CONF1), 3, "stride_h", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_FETCHIF_CONF1), 4, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_CONF_FUNCTION funct4 { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[stride_w]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[stride_h]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_FETCHIF_CONF1(PU_CONF_FUNCTION funct4, SHAPE_REGISTER rstrideS)
	{
		this.funct4 = funct4;
		rstride_s = rstrideS;
	}

	public PU_FETCHIF_CONF1 With(PU_CONF_FUNCTION? funct4 = null, SHAPE_REGISTER? rstrideS = null)
	{
		return new PU_FETCHIF_CONF1(funct4 ?? this.funct4, rstrideS ?? rstride_s);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_FETCHIF_CONF1);
	}

	public bool Equals(PU_FETCHIF_CONF1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4))
		{
			return rstride_s.Equals(other.rstride_s);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rstride_s));
	}
}
