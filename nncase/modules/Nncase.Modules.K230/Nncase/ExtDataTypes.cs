namespace Nncase;

public static class ExtDataTypes
{
	public static readonly ValueType QuantParam = new QuantizeParamType();

	public static readonly ValueType DeQuantParam = new DeQuantizeParamType();

	public static readonly ValueType CropBBox = new CropBBoxType();
}
