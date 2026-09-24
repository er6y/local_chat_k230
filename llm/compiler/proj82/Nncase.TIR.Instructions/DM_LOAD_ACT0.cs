using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_ACT0 : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_ACT0?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_ACT0), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_ACT0), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo is_by_channel = new ParameterInfo(typeof(DM_LOAD_ACT0), 2, "is_by_channel", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_ACT0), 3, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_s { get; }

	public GP_REGISTER rlen { get; }

	public ACT0_CHANNEL dest_channel { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_load_act0, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rlen, 5);
		bitWriter.Write(dest_channel, 1);
		bitWriter.Write(((TensorConst)call[is_by_channel]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 7);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_ACT0(GP_REGISTER raddrS, GP_REGISTER rlen, ACT0_CHANNEL destChannel)
	{
		raddr_s = raddrS;
		this.rlen = rlen;
		dest_channel = destChannel;
	}

	public DM_LOAD_ACT0 With(GP_REGISTER? raddrS = null, GP_REGISTER? rlen = null, ACT0_CHANNEL? destChannel = null)
	{
		return new DM_LOAD_ACT0(raddrS ?? raddr_s, rlen ?? this.rlen, destChannel ?? dest_channel);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_ACT0);
	}

	public bool Equals(DM_LOAD_ACT0? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_s.Equals(other.raddr_s) && rlen.Equals(other.rlen))
		{
			return dest_channel.Equals(other.dest_channel);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_s, rlen, dest_channel));
	}
}
