using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class CCR_SET : InstructionOp, ISerializeInst, IEquatable<CCR_SET?>
{
	public static readonly ParameterInfo ccr = new ParameterInfo(typeof(CCR_SET), 0, "ccr", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo value = new ParameterInfo(typeof(CCR_SET), 1, "value", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ccr_set, 7);
		bitWriter.Write(((TensorConst)call[ccr]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[value]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new CCR_SET With()
	{
		return new CCR_SET();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as CCR_SET);
	}

	public bool Equals(CCR_SET? other)
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
