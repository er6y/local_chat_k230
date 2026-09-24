using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEActivation : Op, IEquatable<GNNEActivation?>
{
	public static readonly ParameterInfo InputA = new ParameterInfo(typeof(GNNEActivation), 0, "input_a", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo InputB = new ParameterInfo(typeof(GNNEActivation), 1, "input_b", (TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType()) | TypePatternUtility.IsNoneType());

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(GNNEActivation), 2, "act", GNNETypePatternUtility.ValidAct());

	public static readonly ParameterInfo InAShiftBits = new ParameterInfo(typeof(GNNEActivation), 3, "in_a_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo InBShiftBits = new ParameterInfo(typeof(GNNEActivation), 4, "in_b_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo OutShiftBits = new ParameterInfo(typeof(GNNEActivation), 5, "out_shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo DeqAParams = new ParameterInfo(typeof(GNNEActivation), 6, "deq_a_params", (TypePatternUtility.IsTensor() & TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam)) | TypePatternUtility.IsNoneType());

	public static readonly ParameterInfo DeqBParams = new ParameterInfo(typeof(GNNEActivation), 7, "deq_b_params", (TypePatternUtility.IsTensor() & TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam)) | TypePatternUtility.IsNoneType());

	public static readonly ParameterInfo OutChannels = new ParameterInfo(typeof(GNNEActivation), 8, "out_channels", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo Is16Segments = new ParameterInfo(typeof(GNNEActivation), 9, "is_16_segments", TypePatternUtility.IsBool());

	public GnneActivationType Type { get; }

	public PrimType OutputDType { get; }

	public ActParamBase ActParam { get; }

	public IRArray<int> OutputShape { get; }

	public IRArray<bool> InputFromL1 { get; }

	public GNNEActivation(GnneActivationType type, PrimType outputDType, ActParamBase actParam, int[] outputShape)
		: this(type, outputDType, actParam, outputShape, new bool[2])
	{
	}

	public GNNEActivation(GnneActivationType type, PrimType outputDType, ActParamBase actParam, IRArray<int> outputShape, IRArray<bool> inputFromL1)
	{
		Type = type;
		OutputDType = outputDType;
		ActParam = actParam;
		OutputShape = outputShape;
		InputFromL1 = inputFromL1;
	}

	public GNNEActivation With(GnneActivationType? type = null, PrimType? outputDType = null, ActParamBase? actParam = null, IRArray<int>? outputShape = null, IRArray<bool>? inputFromL1 = null)
	{
		return new GNNEActivation(type ?? Type, outputDType ?? OutputDType, actParam ?? ActParam, outputShape ?? OutputShape, inputFromL1 ?? InputFromL1);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEActivation);
	}

	public bool Equals(GNNEActivation? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && Type.Equals(other.Type) && OutputDType.Equals(other.OutputDType) && ActParam.Equals(other.ActParam) && OutputShape.Equals(other.OutputShape))
		{
			return InputFromL1.Equals(other.InputFromL1);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Type, OutputDType, ActParam, OutputShape, InputFromL1));
	}
}
