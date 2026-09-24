using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEFusion : Op, IEquatable<GNNEFusion?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEFusion), 0, "input", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public new GNNEFusion With()
	{
		return new GNNEFusion();
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEFusion);
	}

	public bool Equals(GNNEFusion? other)
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
