using System;
using System.Linq;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

public static class GNNETypePatternUtility
{
	public static TypePattern ValidDType()
	{
		return TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Float16) | TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Float32) | TypePatternUtility.HasDataType(DataTypes.Int16);
	}

	public static TypePattern ValidTCUInput()
	{
		return TypePatternUtility.HasRank(4) & TypePatternUtility.HasDataType(DataTypes.Float16);
	}

	public static TypePattern ValidPSum()
	{
		return TypePatternUtility.IsIRType();
	}

	public static TypePattern ValidAct()
	{
		return TypePatternUtility.HasDataType(DataTypes.Float16);
	}

	public static TypePattern ValidBias()
	{
		return TypePatternUtility.HasRank(2) & (TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Float16));
	}

	public static TypePattern ValidWeights()
	{
		return TypePatternUtility.HasRank(4) & (TypePatternUtility.HasDataType(DataTypes.Float16) | TypePatternUtility.HasDataType(DataTypes.Float32) | TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Int16) | TypePatternUtility.HasDataType(DataTypes.UInt8));
	}

	public static TypePattern ValidMFUInput()
	{
		return TypePatternUtility.HasRank(4) & (TypePatternUtility.HasDataType(DataTypes.UInt8) | TypePatternUtility.HasDataType(DataTypes.Int8) | TypePatternUtility.HasDataType(DataTypes.Float16));
	}

	public static TypePattern ValidFakeInput()
	{
		return TypePatternUtility.HasRank(4) & TypePatternUtility.HasDataType(DataTypes.Float32);
	}

	public static TypePattern ValidFakeMFUConstant()
	{
		return TypePatternUtility.HasShape(new Shape(4)) & TypePatternUtility.HasDataType(DataTypes.Float32);
	}

	public static TypePattern ValidFakeFusedClamp()
	{
		return TypePatternUtility.HasShape(new Shape(2)) & TypePatternUtility.HasDataType(DataTypes.Float32);
	}

	public static TypePattern ValidFusedClamp()
	{
		return TypePatternUtility.HasRank(2) & TypePatternUtility.HasDataType(DataTypes.Float16);
	}

	public static TypePattern ValidFakePSum()
	{
		return TypePatternUtility.IsIRType();
	}

	public static TypePattern ValidFakeAct()
	{
		return TypePatternUtility.HasRank(2) & TypePatternUtility.HasDataType(DataTypes.Float32);
	}

	public static DeQuantizeParam GetGNNEDeqParams(float scale, int zeroPoint)
	{
		return new DeQuantizeParam(zeroPoint, scale);
	}

	public static DeQuantizeParam[] GNNEGetDeqParams(int c, float scale, int zeroPoint)
	{
		return Enumerable.Repeat(GetGNNEDeqParams(scale, zeroPoint), c).ToArray();
	}

	public static DeQuantizeParam[] GNNEGetDeqParams(int c, QuantParam quantParam)
	{
		return GNNEGetDeqParams(c, quantParam.Scale, quantParam.ZeroPoint);
	}

	public static QuantizeParam GetGNNEQuantParams(float scale, int zeroPoint)
	{
		return new QuantizeParam(zeroPoint, 1f / scale);
	}

	public static QuantizeParam[] GNNEGetQuantParams(int c, float scale, int zeroPoint)
	{
		return Enumerable.Repeat(GetGNNEQuantParams(scale, zeroPoint), c).ToArray();
	}

	public static int Get_bytes_per_element(DataType type)
	{
		if (type == DataTypes.Int8 || type == DataTypes.UInt8)
		{
			return 1;
		}
		if (type == DataTypes.Int16 || type == DataTypes.Float16)
		{
			return 2;
		}
		if (type == DataTypes.Float32 || type == DataTypes.UInt32 || type == DataTypes.Int32)
		{
			return 4;
		}
		throw new ArgumentOutOfRangeException(type.GetDisplayName());
	}

	public static void CheckIsValidTransposeType(DataType inputType)
	{
		if (Get_bytes_per_element(inputType) > 2)
		{
			throw new NotSupportedException("Unsupported transpose type");
		}
	}

	public static long AlignDiv(long numerator, long denorminator)
	{
		long num = numerator / denorminator;
		if (numerator % denorminator == 0L)
		{
			return num;
		}
		return num + 1;
	}

	public static QuantizeParam[] GNNEGetQuantParams(int c, QuantParam quantParam)
	{
		return Enumerable.Repeat(GetGNNEQuantParams(quantParam.Scale, quantParam.ZeroPoint), c).ToArray();
	}

	public static bool IsDepthWise(int ic, int oc, int groups)
	{
		if (ic == oc && oc == groups)
		{
			return groups != 1;
		}
		return false;
	}

	public static bool IsDepthWise(Expr input, Expr weights, int groups, bool isConv2DTranspose = false)
	{
		if (!isConv2DTranspose)
		{
			return IsDepthWise(input.CheckedShape[1].FixedValue, weights.CheckedShape[0].FixedValue, groups);
		}
		return IsDepthWise(input.CheckedShape[1].FixedValue, weights.CheckedShape[1].FixedValue, groups);
	}

	internal static long[] ApplyPerm(MFU_TRANS_PERMUTE perm)
	{
		return ApplyPerm1(perm, new long[4] { 0L, 1L, 2L, 3L });
	}

	internal static T[] ApplyPerm1<T>(MFU_TRANS_PERMUTE perm, T[] v)
	{
		if (v.Length != 4)
		{
			throw new InvalidOperationException($"applyPerm need Rank4, but get {v.Length}");
		}
		T val = v[0];
		T val2 = v[1];
		T val3 = v[2];
		T val4 = v[3];
		return perm switch
		{
			MFU_TRANS_PERMUTE.NCHW => new T[4] { val, val2, val3, val4 }, 
			MFU_TRANS_PERMUTE.NCWH => new T[4] { val, val2, val4, val3 }, 
			MFU_TRANS_PERMUTE.NHCW => new T[4] { val, val3, val2, val4 }, 
			MFU_TRANS_PERMUTE.NHWC => new T[4] { val, val3, val4, val2 }, 
			MFU_TRANS_PERMUTE.NWCH => new T[4] { val, val4, val2, val3 }, 
			MFU_TRANS_PERMUTE.NWHC => new T[4] { val, val4, val3, val2 }, 
			MFU_TRANS_PERMUTE.CNHW => new T[4] { val2, val, val3, val4 }, 
			MFU_TRANS_PERMUTE.CNWH => new T[4] { val2, val, val4, val3 }, 
			MFU_TRANS_PERMUTE.CHNW => new T[4] { val2, val3, val, val4 }, 
			MFU_TRANS_PERMUTE.CHWN => new T[4] { val2, val3, val4, val }, 
			MFU_TRANS_PERMUTE.CWNH => new T[4] { val2, val4, val, val3 }, 
			MFU_TRANS_PERMUTE.CWHN => new T[4] { val2, val4, val3, val }, 
			MFU_TRANS_PERMUTE.HNCW => new T[4] { val3, val, val2, val4 }, 
			MFU_TRANS_PERMUTE.HNWC => new T[4] { val3, val, val4, val2 }, 
			MFU_TRANS_PERMUTE.HCNW => new T[4] { val3, val2, val, val4 }, 
			MFU_TRANS_PERMUTE.HCWN => new T[4] { val3, val2, val4, val }, 
			MFU_TRANS_PERMUTE.HWNC => new T[4] { val3, val4, val, val2 }, 
			MFU_TRANS_PERMUTE.HWCN => new T[4] { val3, val4, val2, val }, 
			MFU_TRANS_PERMUTE.WNCH => new T[4] { val4, val, val2, val3 }, 
			MFU_TRANS_PERMUTE.WNHC => new T[4] { val4, val, val3, val2 }, 
			MFU_TRANS_PERMUTE.WCNH => new T[4] { val4, val2, val, val3 }, 
			MFU_TRANS_PERMUTE.WCHN => new T[4] { val4, val2, val3, val }, 
			MFU_TRANS_PERMUTE.WHNC => new T[4] { val4, val3, val, val2 }, 
			MFU_TRANS_PERMUTE.WHCN => new T[4] { val4, val3, val2, val }, 
			_ => throw new ArgumentOutOfRangeException("perm", perm, null), 
		};
	}

	internal static MFU_TRANS_PERMUTE ToMFUPerm(int[] perm)
	{
		if (perm.Length != 4)
		{
			throw new InvalidOperationException($"applyPerm need Rank4, but get {perm.Length}");
		}
		if (Enum.TryParse<MFU_TRANS_PERMUTE>(new string(perm.Select(ToPerm).ToArray()), out var result))
		{
			return result;
		}
		throw new InvalidOperationException("InvalidPerm");
		static char ToPerm(int v)
		{
			return v switch
			{
				0 => 'N', 
				1 => 'C', 
				2 => 'H', 
				3 => 'W', 
				_ => throw new InvalidOperationException($"perm value should < 4, but get {v}"), 
			};
		}
	}

	internal static int[] GetGNNEShape(Expr input)
	{
		return GetGNNEShape(input.CheckedShape.ToValueArray());
	}

	internal static int[] GetGNNEShape(int[] dims)
	{
		if (dims.Length > 4)
		{
			throw new InvalidOperationException("dims Length should <= 4");
		}
		return Enumerable.Repeat(1, 4 - dims.Length).Concat(dims).ToArray();
	}
}
