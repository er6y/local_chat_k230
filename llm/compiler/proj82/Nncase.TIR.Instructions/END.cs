using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class END : InstructionOp, ISerializeInst, IEquatable<END?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(END), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rs { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.end, 7);
		bitWriter.Write(rs, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public END(GP_REGISTER rs)
	{
		this.rs = rs;
	}

	public END With(GP_REGISTER? rs = null)
	{
		return new END(rs ?? this.rs);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as END);
	}

	public bool Equals(END? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return rs.Equals(other.rs);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rs));
	}
}
