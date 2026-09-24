using System;

namespace Nncase;

public sealed record CropBBoxType : ValueType
{
	public override Type CLRType => typeof(CropBBox);

	public unsafe override int SizeInBytes => sizeof(CropBBox);

	public override Guid Uuid { get; } = new Guid("f6e1ce8b-e25b-4840-a206-5f3dc094aa8e");


	public override string ToString()
	{
		return "CropBBox";
	}
}
