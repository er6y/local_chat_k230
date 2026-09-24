using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNEStore : Op, IEquatable<GNNEStore?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNEStore), 0, "input", TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(DataTypes.Float16) | TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Float32) | TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo Strides = new ParameterInfo(typeof(GNNEStore), 1, "strides", TypePatternUtility.HasRank(1) & TypePatternUtility.IsIntegral() & TypePatternUtility.HasDataType(DataTypes.Int64));

	public PrimType DestType { get; }

	public GNNEStore(PrimType destType)
	{
		DestType = destType;
	}

	public GNNEStore With(PrimType? destType = null)
	{
		return new GNNEStore(destType ?? DestType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNEStore);
	}

	public bool Equals(GNNEStore? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other))
		{
			return DestType.Equals(other.DestType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(DestType));
	}
}
