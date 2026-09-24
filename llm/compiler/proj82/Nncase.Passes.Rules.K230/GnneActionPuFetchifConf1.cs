using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuFetchifConf1 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public int StrideW { get; }

	public int StrideH { get; }

	public Ssr StrideS { get; }

	public GnneActionPuFetchifConf1()
		: base(GnneActionName.PuFetchifConf1)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		StrideW = -1;
		StrideH = -1;
		StrideS = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuFetchifConf1(int tcuId, int puId, int funct4, int strideW, int strideH, Ssr strideS)
		: base(GnneActionName.PuFetchifConf1)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		StrideW = strideW;
		StrideH = strideH;
		StrideS = strideS;
	}

	public static bool operator ==(GnneActionPuFetchifConf1 lhs, GnneActionPuFetchifConf1 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.StrideW == rhs.StrideW && lhs.StrideH == rhs.StrideH)
		{
			return lhs.StrideS.Value == rhs.StrideS.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuFetchifConf1 lhs, GnneActionPuFetchifConf1 rhs)
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
