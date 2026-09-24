using System;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEPdp1 : Op, IEquatable<GNNEPdp1?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEPdp1), 0, "input", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo Filter = new ParameterInfo(typeof(GNNEPdp1), 1, "filter", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(GNNEPdp1), 2, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(GNNEPdp1), 3, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo QuantParams = new ParameterInfo(typeof(GNNEPdp1), 4, "quant_params", TypePatternUtility.IsNoneType() | (TypePatternUtility.IsTensor() & TypePatternUtility.HasDataType(ExtDataTypes.QuantParam)));

	public static readonly ParameterInfo DequantParams = new ParameterInfo(typeof(GNNEPdp1), 5, "dequantize_param", TypePatternUtility.IsNoneType() | (TypePatternUtility.IsTensor() & TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam)));

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(GNNEPdp1), 6, "value", TypePatternUtility.IsScalar() & TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo ShiftBits = new ParameterInfo(typeof(GNNEPdp1), 7, "shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo CountIncludePad = new ParameterInfo(typeof(GNNEPdp1), 8, "count_include_pad", TypePatternUtility.IsBool());

	public MFU_PDP_OP ReduceOp { get; }

	public PrimType DestType { get; }

	public GNNEPdp1(MFU_PDP_OP reduceOp, PrimType destType)
	{
		ReduceOp = reduceOp;
		DestType = destType;
	}

	public GNNEPdp1 With(MFU_PDP_OP? reduceOp = null, PrimType? destType = null)
	{
		return new GNNEPdp1(reduceOp ?? ReduceOp, destType ?? DestType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEPdp1);
	}

	public bool Equals(GNNEPdp1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && ReduceOp.Equals(other.ReduceOp))
		{
			return DestType.Equals(other.DestType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ReduceOp, DestType));
	}
}
