using System;
using NetFabric.Hyperlinq;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

public static class GModelBuilderExtensions
{
	public static int ToCModelTypeCode(this DataType dtype)
	{
		if (dtype == DataTypes.UInt8)
		{
			return 0;
		}
		if (dtype == DataTypes.Int8)
		{
			return 1;
		}
		if (dtype == DataTypes.Int16)
		{
			return 2;
		}
		if (dtype == DataTypes.Float16)
		{
			return 3;
		}
		if (dtype == DataTypes.Float32)
		{
			return 4;
		}
		throw new NotSupportedException();
	}

	public static int ToNncaseTypeCode(this DataType dtype)
	{
		if (dtype == DataTypes.Int8)
		{
			return 0;
		}
		if (dtype == DataTypes.Int16)
		{
			return 1;
		}
		if (dtype == DataTypes.Int32)
		{
			return 2;
		}
		if (dtype == DataTypes.Int64)
		{
			return 3;
		}
		if (dtype == DataTypes.UInt8)
		{
			return 4;
		}
		if (dtype == DataTypes.UInt16)
		{
			return 5;
		}
		if (dtype == DataTypes.UInt32)
		{
			return 6;
		}
		if (dtype == DataTypes.UInt64)
		{
			return 7;
		}
		if (dtype == DataTypes.Float16)
		{
			return 8;
		}
		if (dtype == DataTypes.Float32)
		{
			return 9;
		}
		if (dtype == DataTypes.Float64)
		{
			return 10;
		}
		if (dtype == DataTypes.BFloat16)
		{
			return 11;
		}
		throw new NotSupportedException();
	}

	public static ArrayExtensions.SpanWhereEnumerable<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> InputOf(this ReadOnlySpan<Nncase.TIR.Buffer> arr)
	{
		return from b in arr.AsValueEnumerable()
			where b.MemSpan.Location == MemoryLocation.Input
			select b;
	}

	public static ArrayExtensions.SpanWhereEnumerable<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> InputBufferOf(this ReadOnlySpan<Nncase.TIR.Buffer> arr)
	{
		return from b in arr.AsValueEnumerable()
			where b.MemSpan.Location == MemoryLocation.Input && b.Dimensions.Length != 0
			select b;
	}

	public static ArrayExtensions.SpanWhereEnumerable<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> InputScalarOf(this ReadOnlySpan<Nncase.TIR.Buffer> arr)
	{
		return from b in arr.AsValueEnumerable()
			where b.MemSpan.Location == MemoryLocation.Input && b.Dimensions.Length == 0
			select b;
	}

	public static ArrayExtensions.SpanWhereEnumerable<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> OutputOf(this ReadOnlySpan<Nncase.TIR.Buffer> arr)
	{
		return from b in arr.AsValueEnumerable()
			where b.MemSpan.Location == MemoryLocation.Output
			select b;
	}
}
