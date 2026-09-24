using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0FetchifConf1 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public int StrideW { get; }

	public int StrideH { get; }

	public GnneActionPuPdp0FetchifConf1()
		: base(GnneActionName.PuPdp0FetchifConf1)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		StrideW = -1;
		StrideH = -1;
	}

	public GnneActionPuPdp0FetchifConf1(int tcuId, int puId, int funct4, int strideW, int strideH)
		: base(GnneActionName.PuPdp0FetchifConf1)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		StrideW = strideW;
		StrideH = strideH;
	}

	public static bool operator ==(GnneActionPuPdp0FetchifConf1 lhs, GnneActionPuPdp0FetchifConf1 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.StrideW == rhs.StrideW)
		{
			return lhs.StrideH == rhs.StrideH;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0FetchifConf1 lhs, GnneActionPuPdp0FetchifConf1 rhs)
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
