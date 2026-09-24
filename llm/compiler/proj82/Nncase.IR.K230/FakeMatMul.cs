using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeMatMul : Op, IEquatable<FakeMatMul?>
{
	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(FakeMatMul), 0, "input_a", TypePatternUtility.HasRank(4) & TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(FakeMatMul), 1, "input_b", TypePatternUtility.HasRank(4) & TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(FakeMatMul), 2, "act");

	public ActParam2 ActParam2 { get; }

	public FakeMatMul(ActParam2 actParam2)
	{
		ActParam2 = actParam2;
	}

	public FakeMatMul With(ActParam2? actParam2 = null)
	{
		return new FakeMatMul(actParam2 ?? ActParam2);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeMatMul);
	}

	public bool Equals(FakeMatMul? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return ActParam2.Equals(other.ActParam2);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ActParam2));
	}
}
