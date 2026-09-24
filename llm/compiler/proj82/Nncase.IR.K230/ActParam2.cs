using System;

namespace Nncase.IR.K230;

public class ActParam2 : ActParamBase
{
	public override int N => 2;

	public override Shape Shape => new Shape(1, 1, base.Channels, base.DataSizePerChannel);

	public ActParam2(ActParam2 other)
		: base(other)
	{
	}

	public ActParam2(int channel, QuantParam quantizeParam = default(QuantParam), bool isDeq = false)
		: base(channel, quantizeParam, isDeq)
	{
		InitValues();
	}

	public static ActParam2 GetDefaultConvActParam(int oc, TensorConst bias)
	{
		ActParam2 actParam = new ActParam2(oc, new QuantParam(0, 1f));
		float[] array = bias.Value.ToArray<float>();
		for (int i = 0; i < actParam.Bs.GetLength(0); i++)
		{
			for (int j = 0; j < actParam.Bs.GetLength(1); j++)
			{
				actParam.Bs[i, j] += array[j];
			}
		}
		return actParam;
	}

	public static ActParam2 GetDefaultConvActParam(Expr weights, TensorConst bias)
	{
		return GetDefaultConvActParam(weights.CheckedShape[0].FixedValue, bias);
	}

	public static ActParam2 GetFakeConvActParam(TensorConst weights, TensorConst bias, bool splitWeightsToAct, float[] newWeights)
	{
		int fixedValue = weights.CheckedShape[0].FixedValue;
		ActParam2 defaultConvActParam = GetDefaultConvActParam(fixedValue, bias);
		if (splitWeightsToAct)
		{
			float[] array = new float[weights.CheckedShape[0].FixedValue];
			ReadOnlySpan<float> readOnlySpan = weights.Value.ToArray<float>();
			float num = 0f;
			int num2 = weights.CheckedShape[1].FixedValue * weights.CheckedShape[2].FixedValue * weights.CheckedShape[3].FixedValue;
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				if (System.Math.Abs(readOnlySpan[i]) > num)
				{
					num = System.Math.Abs(readOnlySpan[i]);
				}
				if ((i + 1) % num2 == 0)
				{
					if (num < 1f)
					{
						num = 1f;
					}
					array[i / num2] = num;
					num = 0f;
				}
			}
			for (int j = 0; j < fixedValue; j++)
			{
				defaultConvActParam.Ks[0, j] *= array[j];
				defaultConvActParam.Ks[1, j] *= array[j];
			}
			for (int k = 0; k < readOnlySpan.Length; k++)
			{
				newWeights[k] /= array[k / num2];
			}
		}
		return defaultConvActParam;
	}

	public static ActParam2 GetFakeConvTransposeActParam(Expr weights, TensorConst bias)
	{
		return GetDefaultConvActParam(weights.CheckedShape[0].FixedValue, bias);
	}

	public override int FusedShiftBits()
	{
		sbyte shiftBits = ShiftBitsHelper.GetShiftBits(this);
		FusedShiftBits(shiftBits);
		return shiftBits;
	}

	public void ForEachChannel(Action<ActParam2, int> f)
	{
		for (int i = 0; i < base.Channels; i++)
		{
			f(this, i);
		}
	}
}
