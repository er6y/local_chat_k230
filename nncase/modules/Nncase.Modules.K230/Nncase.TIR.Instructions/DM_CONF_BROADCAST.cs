using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_CONF_BROADCAST : InstructionOp, ISerializeInst, IEquatable<DM_CONF_BROADCAST?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_CONF_BROADCAST), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo broadcast_if = new ParameterInfo(typeof(DM_CONF_BROADCAST), 1, "broadcast_if", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo broadcast_w = new ParameterInfo(typeof(DM_CONF_BROADCAST), 2, "broadcast_w", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo psum_cascade = new ParameterInfo(typeof(DM_CONF_BROADCAST), 3, "psum_cascade", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_CONF_BROADCAST), 4, "reserved0", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf_broadcast, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[broadcast_if]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[broadcast_w]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[psum_cascade]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 3);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new DM_CONF_BROADCAST With()
	{
		return new DM_CONF_BROADCAST();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_CONF_BROADCAST);
	}

	public bool Equals(DM_CONF_BROADCAST? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null)
		{
			return Equals((InstructionOp?)other);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore());
	}
}
