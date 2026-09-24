using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_PDP1_CONF3 : InstructionOp, ISerializeInst, IEquatable<MFU_PDP1_CONF3?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_PDP1_CONF3), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rpe_channels { get; }

	public GP_REGISTER rpe_last_channels { get; }

	public GP_REGISTER rpad_value { get; }

	public SHAPE_REGISTER sspad { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rpe_channels, 5);
		bitWriter.Write(rpe_last_channels, 5);
		bitWriter.Write(rpad_value, 5);
		bitWriter.Write(sspad, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_PDP1_CONF3(MFU_CONF_FUNCTION funct5, GP_REGISTER rpeChannels, GP_REGISTER rpeLastChannels, GP_REGISTER rpadValue, SHAPE_REGISTER sspad)
	{
		this.funct5 = funct5;
		rpe_channels = rpeChannels;
		rpe_last_channels = rpeLastChannels;
		rpad_value = rpadValue;
		this.sspad = sspad;
	}

	public MFU_PDP1_CONF3 With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rpeChannels = null, GP_REGISTER? rpeLastChannels = null, GP_REGISTER? rpadValue = null, SHAPE_REGISTER? sspad = null)
	{
		return new MFU_PDP1_CONF3(funct5 ?? this.funct5, rpeChannels ?? rpe_channels, rpeLastChannels ?? rpe_last_channels, rpadValue ?? rpad_value, sspad ?? this.sspad);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_PDP1_CONF3);
	}

	public bool Equals(MFU_PDP1_CONF3? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rpe_channels.Equals(other.rpe_channels) && rpe_last_channels.Equals(other.rpe_last_channels) && rpad_value.Equals(other.rpad_value))
		{
			return sspad.Equals(other.sspad);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rpe_channels, rpe_last_channels, rpad_value, sspad));
	}
}
