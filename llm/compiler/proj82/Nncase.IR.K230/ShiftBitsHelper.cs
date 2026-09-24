using System;

namespace Nncase.IR.K230;

public static class ShiftBitsHelper
{
	private static sbyte _mAXSHIFTBITS = 30;

	private static sbyte _minAct1ShiftBits;

	public static sbyte Maxshiftbits
	{
		get
		{
			return _mAXSHIFTBITS;
		}
		set
		{
			_mAXSHIFTBITS = value;
		}
	}

	public static sbyte MinAct1ShiftBits1
	{
		get
		{
			return _minAct1ShiftBits;
		}
		set
		{
			_minAct1ShiftBits = value;
		}
	}

	public static sbyte ComputeShiftBits(float scale)
	{
		if ((double)System.Math.Abs(scale) <= 1E-30)
		{
			return _mAXSHIFTBITS;
		}
		return (sbyte)System.Math.Log2(65500f / System.Math.Abs(scale));
	}

	public static sbyte GetShiftBits(ActParam2 actParam)
	{
		sbyte shiftBits = _mAXSHIFTBITS;
		ActHelper.NestSelect(actParam.Ks, delegate(float k)
		{
			sbyte val = ComputeShiftBits(k);
			shiftBits = System.Math.Min(shiftBits, val);
			return shiftBits;
		});
		return shiftBits;
	}

	public static sbyte ComputeAct1ShiftBits(float scale)
	{
		if (System.Math.Abs(scale) > 65500f)
		{
			return (sbyte)(System.Math.Abs(scale) / 65500f);
		}
		return _minAct1ShiftBits;
	}

	public static sbyte GetAct1ShiftBits(ActParamBase actParam)
	{
		sbyte shiftBits = _minAct1ShiftBits;
		ActHelper.NestSelect(actParam.Ks, delegate(float k)
		{
			sbyte val = ComputeAct1ShiftBits(k);
			shiftBits = System.Math.Max(shiftBits, val);
			return shiftBits;
		});
		return shiftBits;
	}
}
