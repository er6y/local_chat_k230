using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1ConfStride : GnneAction
{
	public int Funct5 { get; }

	public Ssr StrideS1 { get; }

	public Ssr StrideS2 { get; }

	public Ssr StrideD1 { get; }

	public GnneActionMfuAct1ConfStride()
		: base(GnneActionName.MfuAct1ConfStride)
	{
		Funct5 = -1;
		StrideS1 = new Ssr(-1, -1L, needRenewal: false);
		StrideS2 = new Ssr(-1, -1L, needRenewal: false);
		StrideD1 = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionMfuAct1ConfStride(int funct5, Ssr strideS1, Ssr strideS2, Ssr strideD1)
		: base(GnneActionName.MfuAct1ConfStride)
	{
		Funct5 = funct5;
		StrideS1 = strideS1;
		StrideS2 = strideS2;
		StrideD1 = strideD1;
	}

	public static bool operator ==(GnneActionMfuAct1ConfStride lhs, GnneActionMfuAct1ConfStride rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.StrideS1.Value == rhs.StrideS1.Value && lhs.StrideS2.Value == rhs.StrideS2.Value)
		{
			return lhs.StrideD1.Value == rhs.StrideD1.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1ConfStride lhs, GnneActionMfuAct1ConfStride rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object? obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj == null)
		{
			return false;
		}
		throw new NotImplementedException();
	}
}
