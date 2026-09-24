using System;

namespace Nncase;

public sealed record DeQuantizeParamType : ValueType
{
	public override Type CLRType => typeof(DeQuantizeParam);

	public unsafe override int SizeInBytes => sizeof(DeQuantizeParam);

	public override Guid Uuid { get; } = new Guid("30985b8f-294f-4678-b81f-88dc2241fbae");


	public override string ToString()
	{
		return "DeQParam";
	}
}
