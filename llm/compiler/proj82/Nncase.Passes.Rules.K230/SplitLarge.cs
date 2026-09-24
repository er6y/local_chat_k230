using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

internal static class SplitLarge
{
	public static Expr? Split(Call call, Expr input, int[] splitShape, Func<(int Dim, int Axis), bool> shouldSplit, Func<int, int> getRound, Func<int, int> getConcatAxis, Func<(int Count, int ChunkSize, int CurrentSize, int Index, int Axis), (int NewBegin, int NewEnd, Padding Pads)> computeNewInputSize, Func<Expr, Padding, int, Expr> callMaker)
	{
		Expr input2 = input;
		Func<(int Count, int ChunkSize, int CurrentSize, int Index, int Axis), (int NewBegin, int NewEnd, Padding Pads)> computeNewInputSize2 = computeNewInputSize;
		Func<Expr, Padding, int, Expr> callMaker2 = callMaker;
		Call call2 = call;
		(int, int)[] array = (from pair in splitShape.Select((int dim, int axis) => (dim: dim, axis: axis))
			where pair.dim > 65535
			select pair).ToArray();
		if (array.Length != 1)
		{
			return null;
		}
		if (!shouldSplit(array[0]))
		{
			return null;
		}
		int axis2 = array[0].Item2;
		if (call2.CheckedShape.Rank != 4)
		{
			return null;
		}
		int num = splitShape[axis2];
		(int, int) tuple = ComputeChunkSize(num);
		int j = tuple.Item1;
		int item = tuple.Item2;
		int num2 = getRound(axis2);
		int w = item / num2 * num2;
		int element = num - w * (j - 1);
		Call call3 = Tensors.Concat(new Nncase.IR.Tuple((from s in Enumerable.Range(0, j - 1)
			select w).Append(element).Select(delegate(int sliceW, int i)
		{
			int[] inShape = input2.CheckedShape.ToValueArray();
			(int, int, Padding) tuple2 = computeNewInputSize2((j, w, sliceW, i, axis2));
			int newW = tuple2.Item1;
			int newWEnd = tuple2.Item2;
			Padding item2 = tuple2.Item3;
			int[] array2 = (from iv in Enumerable.Range(0, 4)
				select (iv == axis2) ? newW : 0).ToArray();
			int[] array3 = (from iv in Enumerable.Range(0, 4)
				select (iv != axis2) ? inShape[iv] : newWEnd).ToArray();
			Expr expr = Tensors.Slice(input2, array2, array3, 4);
			if (input2 is Marker marker)
			{
				expr = marker.With(null, expr, null, null, null);
			}
			Expr expr2 = callMaker2(expr, item2, axis2);
			if (call2.Users.Count != 0 && call2.Users.First() is Marker marker2)
			{
				expr2 = marker2.With(null, expr2, null, null, null);
			}
			return expr2;
		}).ToArray()), getConcatAxis(axis2));
		if (!call3.CheckedShape.ToValueArray().SequenceEqual(call2.CheckedShape.ToValueArray()))
		{
			throw new InvalidOperationException("SplitLargeCall result shape is not same as origin call shape");
		}
		return call3;
	}

	private static (int N, int ChunkSize) ComputeChunkSize(int dim)
	{
		int num = 1;
		int i;
		for (i = 2; !(System.Math.Ceiling((float)dim / (float)i) < 65535.0); i++)
		{
		}
		num = (int)System.Math.Ceiling((float)dim / (float)i);
		return (N: i, ChunkSize: num);
	}
}
