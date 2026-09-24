using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeConv2D : Op, IEquatable<FakeConv2D?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakeConv2D), 0, "input", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo Weights = new ParameterInfo(typeof(FakeConv2D), 1, "weights", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(FakeConv2D), 2, "act", GNNETypePatternUtility.ValidFakeAct());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(FakeConv2D), 3, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(FakeConv2D), 4, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Dilation = new ParameterInfo(typeof(FakeConv2D), 5, "dilation", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Groups = new ParameterInfo(typeof(FakeConv2D), 6, "groups", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(FakeConv2D), 7, "pad_value", TypePatternUtility.IsScalar());

	public ActParam2 ActParam { get; }

	public FakeConv2D(ActParam2 actParam)
	{
		ActParam = actParam;
	}

	public FakeConv2D With(ActParam2? actParam = null)
	{
		return new FakeConv2D(actParam ?? ActParam);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeConv2D);
	}

	public bool Equals(FakeConv2D? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return ActParam.Equals(other.ActParam);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ActParam));
	}
}
