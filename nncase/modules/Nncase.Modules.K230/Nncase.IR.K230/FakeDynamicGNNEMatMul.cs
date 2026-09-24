using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeDynamicGNNEMatMul : Op, IEquatable<FakeDynamicGNNEMatMul?>
{
	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(FakeDynamicGNNEMatMul), 0, "input_a", TypePatternUtility.HasRank((int r) => r >= 2 && r <= 4, "r >= 2 && r <= 4") & TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(FakeDynamicGNNEMatMul), 1, "input_b", TypePatternUtility.HasRank((int r) => r >= 2 && r <= 4, "r >= 2 && r <= 4") & TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(FakeDynamicGNNEMatMul), 2, "act", TypePatternUtility.HasRank((int r) => r >= 1 && r <= 4, "r >= 1 && r <= 4") & TypePatternUtility.HasDataType(DataTypes.Float32));

	public bool DynamicChannel { get; }

	public FakeDynamicGNNEMatMul(bool dynamicChannel)
	{
		DynamicChannel = dynamicChannel;
	}

	public FakeDynamicGNNEMatMul With(bool? dynamicChannel = null)
	{
		return new FakeDynamicGNNEMatMul(dynamicChannel ?? DynamicChannel);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeDynamicGNNEMatMul);
	}

	public bool Equals(FakeDynamicGNNEMatMul? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return DynamicChannel.Equals(other.DynamicChannel);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(DynamicChannel));
	}
}
