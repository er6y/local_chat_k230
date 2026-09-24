using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeActivation : Op, IEquatable<FakeActivation?>
{
	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(FakeActivation), 0, "input_a", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(FakeActivation), 1, "input_b", TypePatternUtility.IsNoneType() | TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(FakeActivation), 2, "act", GNNETypePatternUtility.ValidFakeAct());

	public static readonly ParameterInfo OutChannels = new ParameterInfo(typeof(FakeActivation), 3, "out_channels", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo InAShiftBits = new ParameterInfo(typeof(FakeActivation), 4, "in_a_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo InBShiftBits = new ParameterInfo(typeof(FakeActivation), 5, "in_b_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo OutShiftBits = new ParameterInfo(typeof(FakeActivation), 6, "out_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo Is16Segments = new ParameterInfo(typeof(FakeActivation), 7, "is_16_segments", TypePatternUtility.IsBool());

	public GnneActivationType Type { get; }

	public ActParamBase ActParam { get; }

	public IRArray<int> OutputShape { get; }

	public FakeActivation(GnneActivationType type, ActParamBase actParam, IRArray<int> outputShape)
	{
		Type = type;
		ActParam = actParam;
		OutputShape = outputShape;
	}

	public FakeActivation With(GnneActivationType? type = null, ActParamBase? actParam = null, IRArray<int>? outputShape = null)
	{
		return new FakeActivation(type ?? Type, actParam ?? ActParam, outputShape ?? OutputShape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeActivation);
	}

	public bool Equals(FakeActivation? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && Type.Equals(other.Type) && ActParam.Equals(other.ActParam))
		{
			return OutputShape.Equals(other.OutputShape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Type, ActParam, OutputShape));
	}
}
