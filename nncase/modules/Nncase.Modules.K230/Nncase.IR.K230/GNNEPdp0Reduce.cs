using System;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEPdp0Reduce : Op, IEquatable<GNNEPdp0Reduce?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEPdp0Reduce), 0, "input", GNNETypePatternUtility.ValidMFUInput());

	public static readonly ParameterInfo Filter = new ParameterInfo(typeof(GNNEPdp0Reduce), 1, "filter", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Stride = new ParameterInfo(typeof(GNNEPdp0Reduce), 2, "stride", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(GNNEPdp0Reduce), 3, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo DepuantParams = new ParameterInfo(typeof(GNNEPdp0Reduce), 4, "dequantize_param", TypePatternUtility.IsNoneType() | (TypePatternUtility.IsTensor() & TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam)));

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(GNNEPdp0Reduce), 5, "value", TypePatternUtility.IsScalar() & TypePatternUtility.HasDataType(DataTypes.Int32));

	public static readonly ParameterInfo ShiftBits = new ParameterInfo(typeof(GNNEPdp0Reduce), 6, "shift_bits", TypePatternUtility.HasDataType(DataTypes.Int32) & TypePatternUtility.IsScalar());

	public static readonly ParameterInfo CountIncludePad = new ParameterInfo(typeof(GNNEPdp0Reduce), 7, "count_include_pad", TypePatternUtility.IsBool());

	public static readonly ParameterInfo Act = new ParameterInfo(typeof(GNNEPdp0Reduce), 8, "act", GNNETypePatternUtility.ValidAct());

	public PU_PDP0_MODE ReduceOp { get; }

	public PrimType DestType { get; }

	public ActParam2 ActParam { get; }

	public GNNEPdp0Reduce(PU_PDP0_MODE reduceOp, PrimType destType, ActParam2 actParam)
	{
		ReduceOp = reduceOp;
		DestType = destType;
		ActParam = actParam;
	}

	public GNNEPdp0Reduce With(PU_PDP0_MODE? reduceOp = null, PrimType? destType = null, ActParam2? actParam = null)
	{
		return new GNNEPdp0Reduce(reduceOp ?? ReduceOp, destType ?? DestType, actParam ?? ActParam);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEPdp0Reduce);
	}

	public bool Equals(GNNEPdp0Reduce? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && ReduceOp.Equals(other.ReduceOp) && DestType.Equals(other.DestType))
		{
			return ActParam.Equals(other.ActParam);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ReduceOp, DestType, ActParam));
	}
}
