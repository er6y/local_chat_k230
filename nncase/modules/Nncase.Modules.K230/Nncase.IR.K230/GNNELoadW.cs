using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNELoadW : Op, IEquatable<GNNELoadW?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNELoadW), 0, "input", TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Float16) | TypePatternUtility.HasDataType(DataTypes.Float32) | TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(ExtDataTypes.DeQuantParam));

	public PrimType DestType { get; }

	public GNNELoadW(PrimType destType)
	{
		DestType = destType;
	}

	public GNNELoadW With(PrimType? destType = null)
	{
		return new GNNELoadW(destType ?? DestType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNELoadW);
	}

	public bool Equals(GNNELoadW? other)
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
