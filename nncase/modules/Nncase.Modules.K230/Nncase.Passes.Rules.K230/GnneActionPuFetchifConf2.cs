using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuFetchifConf2 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Gic { get; }

	public Gpr GicLas { get; }

	public GnneActionPuFetchifConf2()
		: base(GnneActionName.PuFetchifConf2)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Gic = new Gpr(-1, -1, needRenewal: false);
		GicLas = new Gpr(-1, -1, needRenewal: false);
	}

	public GnneActionPuFetchifConf2(int tcuId, int puId, int funct4, Gpr gic, Gpr gicLast)
		: base(GnneActionName.PuFetchifConf2)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Gic = gic;
		GicLas = gicLast;
	}

	public static bool operator ==(GnneActionPuFetchifConf2 lhs, GnneActionPuFetchifConf2 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4)
		{
			return lhs.Gic.Value == rhs.Gic.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuFetchifConf2 lhs, GnneActionPuFetchifConf2 rhs)
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
