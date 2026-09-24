using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuOfConf1 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Goc { get; }

	public Gpr GocLast { get; }

	public Ssr StrideD { get; }

	public GnneActionPuOfConf1()
		: base(GnneActionName.PuOfConf1)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Goc = new Gpr(-1, -1, needRenewal: false);
		GocLast = new Gpr(-1, -1, needRenewal: false);
		StrideD = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuOfConf1(int tcuId, int puId, int funct4, Gpr goc, Gpr gocLast, Ssr strideD)
		: base(GnneActionName.PuOfConf1)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Goc = goc;
		GocLast = gocLast;
		StrideD = strideD;
	}

	public static bool operator ==(GnneActionPuOfConf1 lhs, GnneActionPuOfConf1 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.Goc.Value == rhs.Goc.Value)
		{
			return lhs.StrideD.Value == rhs.StrideD.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuOfConf1 lhs, GnneActionPuOfConf1 rhs)
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
