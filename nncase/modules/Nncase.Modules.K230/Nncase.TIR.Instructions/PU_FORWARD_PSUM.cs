using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_FORWARD_PSUM : InstructionOp, ISerializeInst, IEquatable<PU_FORWARD_PSUM?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_FORWARD_PSUM), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_FORWARD_PSUM), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_FORWARD_PSUM), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr { get; }

	public GP_REGISTER rlen { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_forward_psum, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr, 5);
		bitWriter.Write(rlen, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_FORWARD_PSUM(GP_REGISTER raddr, GP_REGISTER rlen)
	{
		this.raddr = raddr;
		this.rlen = rlen;
	}

	public PU_FORWARD_PSUM With(GP_REGISTER? raddr = null, GP_REGISTER? rlen = null)
	{
		return new PU_FORWARD_PSUM(raddr ?? this.raddr, rlen ?? this.rlen);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_FORWARD_PSUM);
	}

	public bool Equals(PU_FORWARD_PSUM? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr.Equals(other.raddr))
		{
			return rlen.Equals(other.rlen);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr, rlen));
	}
}
