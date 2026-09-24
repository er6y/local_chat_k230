using System;

namespace Nncase.Passes;

public static class PassesExtensions
{
	public static ValueRange<T> AsValueRange<T>(this Tensor<T> ts) where T : unmanaged, IEquatable<T>, IComparable<T>
	{
		if (ts.Dimensions.Length == 1 && ts.Dimensions[0] == 2)
		{
			return new ValueRange<T>(ts[new int[1]], ts[new int[1] { 1 }]);
		}
		throw new NotSupportedException("The Tensor Can't View As ValueRange!");
	}
}
