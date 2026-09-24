using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeConv2DTranspose : Op, IEquatable<FakeConv2DTranspose?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakeConv2DTranspose), 0, "input", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo Weights = new ParameterInfo(typeof(FakeConv2DTranspose), 1, "weights", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(FakeConv2DTranspose), 2, "act", GNNETypePatternUtility.ValidFakeAct());

	public static readonly ParameterInfo OutputShape = new ParameterInfo(typeof(FakeConv2DTranspose), 3, "output_shape", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(FakeConv2DTranspose), 4, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo OutputPadding = new ParameterInfo(typeof(FakeConv2DTranspose), 5, "output_padding");

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(FakeConv2DTranspose), 6, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Dilation = new ParameterInfo(typeof(FakeConv2DTranspose), 7, "dilation", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Groups = new ParameterInfo(typeof(FakeConv2DTranspose), 8, "groups", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(FakeConv2DTranspose), 9, "value", TypePatternUtility.IsScalar());

	public ActParam2 ActParam { get; }

	public FakeConv2DTranspose(ActParam2 actParam)
	{
		ActParam = actParam;
	}

	public FakeConv2DTranspose With(ActParam2? actParam = null)
	{
		return new FakeConv2DTranspose(actParam ?? ActParam);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeConv2DTranspose);
	}

	public bool Equals(FakeConv2DTranspose? other)
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
