using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_STORE_OF : InstructionOp, ISerializeInst, IEquatable<DM_STORE_OF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_STORE_OF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_STORE_OF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_STORE_OF), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public SHAPE_REGISTER rshape { get; }

	public ACT0_CHANNEL src_channel { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_store_of, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(src_channel, 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_STORE_OF(GP_REGISTER raddrD, SHAPE_REGISTER rshape, ACT0_CHANNEL srcChannel)
	{
		raddr_d = raddrD;
		this.rshape = rshape;
		src_channel = srcChannel;
	}

	public DM_STORE_OF With(GP_REGISTER? raddrD = null, SHAPE_REGISTER? rshape = null, ACT0_CHANNEL? srcChannel = null)
	{
		return new DM_STORE_OF(raddrD ?? raddr_d, rshape ?? this.rshape, srcChannel ?? src_channel);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_STORE_OF);
	}

	public bool Equals(DM_STORE_OF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && rshape.Equals(other.rshape))
		{
			return src_channel.Equals(other.src_channel);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, rshape, src_channel));
	}
}
