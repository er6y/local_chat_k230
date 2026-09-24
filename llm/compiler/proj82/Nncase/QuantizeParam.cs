using System;

namespace Nncase;

public struct QuantizeParam : IEquatable<QuantizeParam>
{
	public int ZeroPoint;

	public float Scale;

	public QuantizeParam(int zeroPoint, float scale)
	{
		ZeroPoint = zeroPoint;
		Scale = scale;
	}

	public QuantizeParam(QuantizeParam other)
	{
		ZeroPoint = other.ZeroPoint;
		Scale = other.Scale;
	}

	public bool Equals(QuantizeParam other)
	{
		if (Scale.Equals(other.Scale))
		{
			return ZeroPoint == other.ZeroPoint;
		}
		return false;
	}

	public override string ToString()
	{
		return $"<{ZeroPoint}, {Scale}>";
	}

	public override bool Equals(object? obj)
	{
		if (obj is QuantizeParam other)
		{
			return Equals(other);
		}
		return false;
	}
}
