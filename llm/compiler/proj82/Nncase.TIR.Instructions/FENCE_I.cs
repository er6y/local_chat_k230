using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class FENCE_I : InstructionOp, ISerializeInst, IEquatable<FENCE_I?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(FENCE_I), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.fence_i, 7);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new FENCE_I With()
	{
		return new FENCE_I();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FENCE_I);
	}

	public bool Equals(FENCE_I? other)
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
