using System;

namespace Nncase;

public struct DeQuantizeParam : IEquatable<DeQuantizeParam>
{
	public int ZeroPoint;

	public float Scale;

	public DeQuantizeParam(int zeroPoint, float scale)
	{
		ZeroPoint = zeroPoint;
		Scale = scale;
	}

	public bool Equals(DeQuantizeParam other)
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
		if (obj is DeQuantizeParam other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ZeroPoint, Scale);
	}
}
