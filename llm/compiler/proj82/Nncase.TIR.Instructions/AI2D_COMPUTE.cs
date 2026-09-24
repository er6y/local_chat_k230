using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class AI2D_COMPUTE : InstructionOp, ISerializeInst, IEquatable<AI2D_COMPUTE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(AI2D_COMPUTE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ai2d_compute, 7);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new AI2D_COMPUTE With()
	{
		return new AI2D_COMPUTE();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as AI2D_COMPUTE);
	}

	public bool Equals(AI2D_COMPUTE? other)
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
