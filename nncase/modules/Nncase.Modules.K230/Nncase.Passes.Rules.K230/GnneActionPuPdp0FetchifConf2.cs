using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0FetchifConf2 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Gic { get; }

	public Gpr GicLast { get; }

	public GnneActionPuPdp0FetchifConf2()
		: base(GnneActionName.PuPdp0FetchifConf2)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Gic = new Gpr(-1, -1, needRenewal: false);
		GicLast = new Gpr(-1, -1, needRenewal: false);
	}

	public GnneActionPuPdp0FetchifConf2(int tcuId, int puId, int funct4, Gpr gic, Gpr gicLast)
		: base(GnneActionName.PuPdp0FetchifConf2)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Gic = gic;
		GicLast = gicLast;
	}

	public static bool operator ==(GnneActionPuPdp0FetchifConf2 lhs, GnneActionPuPdp0FetchifConf2 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.Gic.Value == rhs.Gic.Value)
		{
			return lhs.GicLast.Value == rhs.GicLast.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0FetchifConf2 lhs, GnneActionPuPdp0FetchifConf2 rhs)
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
