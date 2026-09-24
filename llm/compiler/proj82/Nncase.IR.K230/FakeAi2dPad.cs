using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeAi2dPad : Op, IEquatable<FakeAi2dPad?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakeAi2dPad), 0, "input", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(FakeAi2dPad), 1, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(FakeAi2dPad), 2, "value", TypePatternUtility.IsScalar());

	public PadMode Mode { get; }

	public FakeAi2dPad(PadMode mode)
	{
		Mode = mode;
	}

	public FakeAi2dPad With(PadMode? mode = null)
	{
		return new FakeAi2dPad(mode ?? Mode);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeAi2dPad);
	}

	public bool Equals(FakeAi2dPad? other)
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
