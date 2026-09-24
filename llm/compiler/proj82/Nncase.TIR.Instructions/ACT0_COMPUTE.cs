using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class ACT0_COMPUTE : InstructionOp, ISerializeInst, IEquatable<ACT0_COMPUTE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(ACT0_COMPUTE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(ACT0_COMPUTE), 1, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo is_by_channel = new ParameterInfo(typeof(ACT0_COMPUTE), 2, "is_by_channel", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(ACT0_COMPUTE), 3, "reserved1", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public ACT0_CHANNEL channel { get; }

	public ACT0_OUTPUT_DEST target { get; }

	public ACT_OUTPUT_TYPE dest_datatype { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.act0_compute, 7);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(channel, 1);
		bitWriter.Write(target, 2);
		bitWriter.Write(dest_datatype, 2);
		bitWriter.Write(((TensorConst)call[is_by_channel]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 6);
		bitWriter.Flush();
		writer.Write(array);
	}

	public ACT0_COMPUTE(GP_REGISTER raddrD, ACT0_CHANNEL channel, ACT0_OUTPUT_DEST target, ACT_OUTPUT_TYPE destDatatype)
	{
		raddr_d = raddrD;
		this.channel = channel;
		this.target = target;
		dest_datatype = destDatatype;
	}

	public ACT0_COMPUTE With(GP_REGISTER? raddrD = null, ACT0_CHANNEL? channel = null, ACT0_OUTPUT_DEST? target = null, ACT_OUTPUT_TYPE? destDatatype = null)
	{
		return new ACT0_COMPUTE(raddrD ?? raddr_d, channel ?? this.channel, target ?? this.target, destDatatype ?? dest_datatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ACT0_COMPUTE);
	}

	public bool Equals(ACT0_COMPUTE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && channel.Equals(other.channel) && target.Equals(other.target))
		{
			return dest_datatype.Equals(other.dest_datatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, channel, target, dest_datatype));
	}
}
