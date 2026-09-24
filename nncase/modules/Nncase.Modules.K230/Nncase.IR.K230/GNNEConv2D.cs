using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEConv2D : Op, IEquatable<GNNEConv2D?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEConv2D), 0, "input", TypePatternUtility.HasRank(4) & (TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(DataTypes.UInt8)));

	public static readonly ParameterInfo Weights = new ParameterInfo(typeof(GNNEConv2D), 1, "weights", TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo WeightsBias = new ParameterInfo(typeof(GNNEConv2D), 2, "weights_bias", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo WeightsBiasQint8 = new ParameterInfo(typeof(GNNEConv2D), 3, "weights_bias_qint8", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(GNNEConv2D), 4, "act", GNNETypePatternUtility.ValidAct());

	public static readonly ParameterInfo ActQint8 = new ParameterInfo(typeof(GNNEConv2D), 5, "act_qint8", GNNETypePatternUtility.ValidAct());

	public static readonly ParameterInfo DeqBias = new ParameterInfo(typeof(GNNEConv2D), 6, "deq_bias", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo ShiftBits = new ParameterInfo(typeof(GNNEConv2D), 7, "shift_bits", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo ShiftBitsQint8 = new ParameterInfo(typeof(GNNEConv2D), 8, "shift_bits_qint8", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Qint8Qp = new ParameterInfo(typeof(GNNEConv2D), 9, "qint8_qp", TypePatternUtility.IsScalar() & TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam));

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(GNNEConv2D), 10, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(GNNEConv2D), 11, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Dilation = new ParameterInfo(typeof(GNNEConv2D), 12, "dilation", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Groups = new ParameterInfo(typeof(GNNEConv2D), 13, "groups", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Is16Quant = new ParameterInfo(typeof(GNNEConv2D), 14, "is_16_quant", TypePatternUtility.IsBool());

	public static readonly ParameterInfo PadValue = new ParameterInfo(typeof(GNNEConv2D), 15, "pad_value", TypePatternUtility.IsScalar());

	public static readonly ParameterInfo WeightsQInt8 = new ParameterInfo(typeof(GNNEConv2D), 16, "weights_qint8", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public ActParam2 ActParam { get; }

	public ActParam2 ActParamQInt8 { get; }

	public PrimType DestType { get; }

	public GNNEConv2D(ActParam2 actParam, ActParam2 actParamQInt8, PrimType destType)
	{
		ActParam = actParam;
		ActParamQInt8 = actParamQInt8;
		DestType = destType;
	}

	public GNNEConv2D With(ActParam2? actParam = null, ActParam2? actParamQInt8 = null, PrimType? destType = null)
	{
		return new GNNEConv2D(actParam ?? ActParam, actParamQInt8 ?? ActParamQInt8, destType ?? DestType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEConv2D);
	}

	public bool Equals(GNNEConv2D? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && ActParam.Equals(other.ActParam) && ActParamQInt8.Equals(other.ActParamQInt8))
		{
			return DestType.Equals(other.DestType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ActParam, ActParamQInt8, DestType));
	}
}
