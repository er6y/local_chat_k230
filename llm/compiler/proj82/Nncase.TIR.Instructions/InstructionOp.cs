using System;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public class InstructionOp : Op, IEquatable<InstructionOp?>
{
	public override bool CanFoldConstCall => false;

	public new InstructionOp With()
	{
		return new InstructionOp();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as InstructionOp);
	}

	public bool Equals(InstructionOp? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null)
		{
			return base.Equals((object?)other);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore());
	}
}
