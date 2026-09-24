using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class FENCE : InstructionOp, ISerializeInst, IEquatable<FENCE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(FENCE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.fence, 7);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new FENCE With()
	{
		return new FENCE();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FENCE);
	}

	public bool Equals(FENCE? other)
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
