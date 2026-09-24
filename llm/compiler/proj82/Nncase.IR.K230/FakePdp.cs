using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakePdp : Op, IEquatable<FakePdp?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakePdp), 0, "input", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo PadValue = new ParameterInfo(typeof(FakePdp), 1, "pad_value", TypePatternUtility.IsScalar());

	public static readonly ParameterInfo Filter = new ParameterInfo(typeof(FakePdp), 2, "filter", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(FakePdp), 3, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(FakePdp), 4, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo CountIncludePad = new ParameterInfo(typeof(FakePdp), 5, "count_include_pad", TypePatternUtility.IsBool());

	public ReduceOp ReduceOp { get; }

	public FakePdp(ReduceOp reduceOp)
	{
		ReduceOp = reduceOp;
	}

	public FakePdp With(ReduceOp? reduceOp = null)
	{
		return new FakePdp(reduceOp ?? ReduceOp);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakePdp);
	}

	public bool Equals(FakePdp? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return ReduceOp.Equals(other.ReduceOp);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ReduceOp));
	}
}
