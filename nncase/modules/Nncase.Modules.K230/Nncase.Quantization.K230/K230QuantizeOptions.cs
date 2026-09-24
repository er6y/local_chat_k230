namespace Nncase.Quantization.K230;

public sealed class K230QuantizeOptions : QuantizeOptions
{
	private PrimType _quantType;

	private PrimType _wQuantType;

	public K230QuantizeOptions(PrimType quantType, PrimType wQuantType)
	{
		_quantType = quantType;
		_wQuantType = wQuantType;
	}
}
