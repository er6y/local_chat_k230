using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNELoad : Op, IEquatable<GNNELoad?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNELoad), 0, "input", TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(DataTypes.Float16) | TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Float32) | TypePatternUtility.HasDataType(DataTypes.UInt8));

	public PrimType DestType { get; }

	public GNNELoad(PrimType destType)
	{
		DestType = destType;
	}

	public GNNELoad With(PrimType? destType = null)
	{
		return new GNNELoad(destType ?? DestType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNELoad);
	}

	public bool Equals(GNNELoad? other)
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
