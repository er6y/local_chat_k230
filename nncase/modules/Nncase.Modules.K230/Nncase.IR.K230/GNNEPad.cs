using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEPad : Op, IEquatable<GNNEPad?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEPad), 0, "input", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo Pads = new ParameterInfo(typeof(GNNEPad), 1, "pads", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(GNNEPad), 2, "value", TypePatternUtility.IsScalar());

	public PadMode Mode { get; }

	public GNNEPad(PadMode mode)
	{
		Mode = mode;
	}

	public GNNEPad With(PadMode? mode = null)
	{
		return new GNNEPad(mode ?? Mode);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEPad);
	}

	public bool Equals(GNNEPad? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return Mode.Equals(other.Mode);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Mode));
	}
}
