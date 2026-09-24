using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEMatMul : Op, IEquatable<GNNEMatMul?>
{
	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(GNNEMatMul), 0, "input_a", TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Int16));

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(GNNEMatMul), 1, "input_b", TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Int16));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(GNNEMatMul), 2, "act", GNNETypePatternUtility.ValidAct());

	public static readonly ParameterInfo InputABias = new ParameterInfo(typeof(GNNEMatMul), 3, "input_a_bias", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo InAShiftBits = new ParameterInfo(typeof(GNNEMatMul), 4, "in_a_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32));

	public static readonly ParameterInfo InBShiftBits = new ParameterInfo(typeof(GNNEMatMul), 5, "in_b_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32));

	public static readonly ParameterInfo ShiftBits = new ParameterInfo(typeof(GNNEMatMul), 6, "shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo DeqBBias = new ParameterInfo(typeof(GNNEMatMul), 7, "deq_b_bias", TypePatternUtility.IsIntegral());

	public ActParam2 ActParam { get; }

	public PrimType OutputDType { get; }

	public GNNEMatMul(ActParam2 actParam, PrimType outputDType)
	{
		ActParam = actParam;
		OutputDType = outputDType;
	}

	public GNNEMatMul With(ActParam2? actParam = null, PrimType? outputDType = null)
	{
		return new GNNEMatMul(actParam ?? ActParam, outputDType ?? OutputDType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEMatMul);
	}

	public bool Equals(GNNEMatMul? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && ActParam.Equals(other.ActParam))
		{
			return OutputDType.Equals(other.OutputDType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ActParam, OutputDType));
	}
}
