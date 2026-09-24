using System;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNETranspose : Op, IEquatable<GNNETranspose?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNETranspose), 0, "input", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public MFU_TRANS_PERMUTE Perm { get; }

	public GNNETranspose(MFU_TRANS_PERMUTE perm)
	{
		Perm = perm;
	}

	public GNNETranspose With(MFU_TRANS_PERMUTE? perm = null)
	{
		return new GNNETranspose(perm ?? Perm);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNETranspose);
	}

	public bool Equals(GNNETranspose? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return Perm.Equals(other.Perm);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Perm));
	}
}
