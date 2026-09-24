using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class ACT0_SRC1_CONF : InstructionOp, ISerializeInst, IEquatable<ACT0_SRC1_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(ACT0_SRC1_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(ACT0_SRC1_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo rshift_bits = new ParameterInfo(typeof(ACT0_SRC1_CONF), 2, "rshift_bits", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(ACT0_SRC1_CONF), 3, "reserved0", TypePatternUtility.IsIntegralScalar());

	public ACT0_CONF_FUNCTION funct3 { get; }

	public ACT0_CHANNEL channel { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.act0_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct3, 3);
		bitWriter.Write(channel, 1);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[rshift_bits]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 7);
		bitWriter.Flush();
		writer.Write(array);
	}

	public ACT0_SRC1_CONF(ACT0_CONF_FUNCTION funct3, ACT0_CHANNEL channel, SHAPE_REGISTER rshape)
	{
		this.funct3 = funct3;
		this.channel = channel;
		this.rshape = rshape;
	}

	public ACT0_SRC1_CONF With(ACT0_CONF_FUNCTION? funct3 = null, ACT0_CHANNEL? channel = null, SHAPE_REGISTER? rshape = null)
	{
		return new ACT0_SRC1_CONF(funct3 ?? this.funct3, channel ?? this.channel, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ACT0_SRC1_CONF);
	}

	public bool Equals(ACT0_SRC1_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct3.Equals(other.funct3) && channel.Equals(other.channel))
		{
			return rshape.Equals(other.rshape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct3, channel, rshape));
	}
}
