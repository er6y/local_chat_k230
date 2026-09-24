using System;

namespace Nncase;

public sealed record QuantizeParamType : ValueType
{
	public override Type CLRType => typeof(QuantizeParam);

	public unsafe override int SizeInBytes => sizeof(QuantizeParam);

	public override Guid Uuid { get; } = new Guid("82108194-d1e0-4c5c-a41e-eca61b1a595b");


	public override string ToString()
	{
		return "QParam";
	}
}
