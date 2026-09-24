using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_FETCHIF_CONF4 : InstructionOp, ISerializeInst, IEquatable<PU_FETCHIF_CONF4?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_FETCHIF_CONF4), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_FETCHIF_CONF4), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_FETCHIF_CONF4), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(PU_FETCHIF_CONF4), 3, "reserved1", TypePatternUtility.IsIntegralScalar());

	public PU_CONF_FUNCTION funct4 { get; }

	public GP_REGISTER rpad_value { get; }

	public SHAPE_REGISTER sspad { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(rpad_value, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(sspad, 3);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_FETCHIF_CONF4(PU_CONF_FUNCTION funct4, GP_REGISTER rpadValue, SHAPE_REGISTER sspad)
	{
		this.funct4 = funct4;
		rpad_value = rpadValue;
		this.sspad = sspad;
	}

	public PU_FETCHIF_CONF4 With(PU_CONF_FUNCTION? funct4 = null, GP_REGISTER? rpadValue = null, SHAPE_REGISTER? sspad = null)
	{
		return new PU_FETCHIF_CONF4(funct4 ?? this.funct4, rpadValue ?? rpad_value, sspad ?? this.sspad);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_FETCHIF_CONF4);
	}

	public bool Equals(PU_FETCHIF_CONF4? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && rpad_value.Equals(other.rpad_value))
		{
			return sspad.Equals(other.sspad);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, rpad_value, sspad));
	}
}
