using Nncase.Quantization.K230;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

internal static class NonConstQuantManager
{
	public static QuantizeParam GetInAQuantParam(DataType quantType, ValueRange<float> inARange)
	{
		return Nncase.Quantization.K230.Utility.GetQuantParamFromDeqParam(GetInADeqQuantParam(quantType, inARange));
	}

	public static DeQuantizeParam GetInADeqQuantParam(DataType quantType, ValueRange<float> inARange)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		return Nncase.Quantization.K230.Utility.GetDeqParam(QuantUtility.GetQuantParam(inARange, 8, quantMode));
	}

	public static QuantizeParam GetInBQuantParam(DataType quantType, ValueRange<float> inBRange)
	{
		return Nncase.Quantization.K230.Utility.GetQuantParamFromDeqParam(GetInBDeqQuantParam(quantType, inBRange));
	}

	public static DeQuantizeParam GetInBDeqQuantParam(DataType quantType, ValueRange<float> inBRange)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		return Nncase.Quantization.K230.Utility.GetDeqParam(QuantUtility.GetQuantParam(inBRange, 8, quantMode));
	}

	public static QuantizeParam GetOfQuantParam(DataType quantType, ValueRange<float> outRange)
	{
		return Nncase.Quantization.K230.Utility.GetQuantParamFromDeqParam(GetOfDeqQuantParam(quantType, outRange));
	}

	public static DeQuantizeParam GetOfDeqQuantParam(DataType quantType, ValueRange<float> outRange)
	{
		QuantMode quantMode = ((!(quantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
		int bits = ((quantType == DataTypes.Int16) ? 12 : 8);
		return Nncase.Quantization.K230.Utility.GetDeqParam(QuantUtility.GetQuantParam(outRange, bits, quantMode));
	}
}
