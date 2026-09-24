using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1Conf1 : GnneAction
{
	public int Funct5 { get; }

	public int StrideW { get; }

	public int StrideH { get; }

	public PDP_FUNCTION Funct2 { get; }

	public Ssr StrideD { get; }

	public Ssr StrideS { get; }

	public GnneActionMfuPdp1Conf1()
		: base(GnneActionName.MfuPdp1Conf1)
	{
		Funct5 = -1;
		StrideW = -1;
		StrideH = -1;
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		StrideS = new Ssr(-1, -1L, needRenewal: false);
		Funct2 = PDP_FUNCTION.max;
	}

	public GnneActionMfuPdp1Conf1(int funct5, int strideW, int strideH, Ssr strideD, Ssr strideS, PDP_FUNCTION funct2)
		: base(GnneActionName.MfuPdp1Conf1)
	{
		Funct5 = funct5;
		StrideW = strideW;
		StrideH = strideH;
		StrideD = strideD;
		StrideS = strideS;
		Funct2 = funct2;
	}

	public static bool operator ==(GnneActionMfuPdp1Conf1 lhs, GnneActionMfuPdp1Conf1 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.StrideW == rhs.StrideW && lhs.StrideH == rhs.StrideH && lhs.StrideD.Value == rhs.StrideD.Value && lhs.StrideS.Value == rhs.StrideS.Value)
		{
			return lhs.Funct2 == rhs.Funct2;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuPdp1Conf1 lhs, GnneActionMfuPdp1Conf1 rhs)
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
