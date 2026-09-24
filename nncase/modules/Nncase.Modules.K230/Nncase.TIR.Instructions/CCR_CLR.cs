using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class CCR_CLR : InstructionOp, ISerializeInst, IEquatable<CCR_CLR?>
{
	public static readonly ParameterInfo ccr = new ParameterInfo(typeof(CCR_CLR), 0, "ccr", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(CCR_CLR), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ccr_clr, 7);
		bitWriter.Write(((TensorConst)call[ccr]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public new CCR_CLR With()
	{
		return new CCR_CLR();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as CCR_CLR);
	}

	public bool Equals(CCR_CLR? other)
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
