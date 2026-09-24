using System;

namespace Nncase;

public struct CropBBox : IEquatable<CropBBox>
{
	public int X0;

	public int Y0;

	public int X1;

	public int Y1;

	public CropBBox(int x0, int y0, int x1, int y1)
	{
		X0 = x0;
		Y0 = y0;
		X1 = x1;
		Y1 = y1;
	}

	public bool Equals(CropBBox other)
	{
		if (X0 == other.X0 && Y0 == other.Y0 && X1 == other.X1)
		{
			return Y1 == other.Y1;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is CropBBox)
		{
			return Equals((CropBBox)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X0, Y0, X1, Y1);
	}
}
