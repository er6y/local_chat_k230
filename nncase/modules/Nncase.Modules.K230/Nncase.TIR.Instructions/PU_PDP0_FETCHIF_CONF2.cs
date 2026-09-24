using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_FETCHIF_CONF2 : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_FETCHIF_CONF2?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF2), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF2), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_FETCHIF_CONF2), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_PDP0_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rgic { get; }

	public GP_REGISTER rgic_last { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rgic, 5);
		bitWriter.Write(rgic_last, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_FETCHIF_CONF2(PU_PDP0_CONF_FUNCTION funct4, GP_REGISTER rgic, GP_REGISTER rgicLast)
	{
		this.funct4 = funct4;
		this.rgic = rgic;
		rgic_last = rgicLast;
	}

	public PU_PDP0_FETCHIF_CONF2 With(PU_PDP0_CONF_FUNCTION? funct4 = null, GP_REGISTER? rgic = null, GP_REGISTER? rgicLast = null)
	{
		return new PU_PDP0_FETCHIF_CONF2(funct4 ?? this.funct4, rgic ?? this.rgic, rgicLast ?? rgic_last);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_FETCHIF_CONF2);
	}

	public bool Equals(PU_PDP0_FETCHIF_CONF2? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rgic.Equals(other.rgic))
		{
			return rgic_last.Equals(other.rgic_last);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rgic, rgic_last));
	}
}
