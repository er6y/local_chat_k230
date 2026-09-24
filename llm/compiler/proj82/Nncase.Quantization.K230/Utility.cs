namespace Nncase.Quantization.K230;

public static class Utility
{
	public static QuantizeParam GetQuantParam(QuantParam qP)
	{
		return new QuantizeParam(qP.ZeroPoint, 1f / qP.Scale);
	}

	public static DeQuantizeParam GetDeqParam(QuantParam qP)
	{
		return new DeQuantizeParam(qP.ZeroPoint, qP.Scale);
	}

	public static QuantizeParam GetQuantParamFromDeqParam(DeQuantizeParam qP)
	{
		return new QuantizeParam(qP.ZeroPoint, 1f / qP.Scale);
	}

	public static DeQuantizeParam GetDeqParamFromQuantParam(QuantizeParam qP)
	{
		return new DeQuantizeParam(qP.ZeroPoint, 1f / qP.Scale);
	}
}
