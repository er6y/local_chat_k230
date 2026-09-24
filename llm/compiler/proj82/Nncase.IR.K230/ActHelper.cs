using System;

namespace Nncase.IR.K230;

public static class ActHelper
{
	public static Shape GetAct16SegementsShape => new Shape(1, 1, 1, 49);

	public static float[,] NestSelect(float[,] arr, Func<float, float> f)
	{
		float[,] array = new float[arr.GetLength(0), arr.GetLength(1)];
		for (int i = 0; i < arr.GetLength(0); i++)
		{
			for (int j = 0; j < arr.GetLength(1); j++)
			{
				array[i, j] = f(arr[i, j]);
			}
		}
		return array;
	}
}
