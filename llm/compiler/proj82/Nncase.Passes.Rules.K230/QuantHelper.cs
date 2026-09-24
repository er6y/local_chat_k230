using System;

namespace Nncase.Passes.Rules.K230;

public static class QuantHelper
{
	public static QuantizeParam GetQuantParam(QuantParam qp)
	{
		return new QuantizeParam(qp.ZeroPoint, 1f / qp.Scale);
	}

	public static DeQuantizeParam GetDeqParam(QuantParam qp)
	{
		return new DeQuantizeParam(qp.ZeroPoint, qp.Scale);
	}

	public static QuantizeParam GetQuantParamFromDeqParam(DeQuantizeParam qP)
	{
		return new QuantizeParam(qP.ZeroPoint, 1f / qP.Scale);
	}

	public static DeQuantizeParam GetDeqParamFromQuantParam(QuantizeParam qP)
	{
		return new DeQuantizeParam(qP.ZeroPoint, 1f / qP.Scale);
	}

	public static T Quantize<T>(float data, QuantizeParam qp)
	{
		return (T)Convert.ChangeType(Math.Clamp((int)Math.Round(data * qp.Scale + (float)qp.ZeroPoint), Convert.ToInt32(typeof(T).GetField("MinValue").GetValue(null)), Convert.ToInt32(typeof(T).GetField("MaxValue").GetValue(null))), typeof(T));
	}

	public static T Quantize1<T>(float data, QuantParam qp)
	{
		return Quantize<T>(data, GetQuantParam(qp));
	}
}
