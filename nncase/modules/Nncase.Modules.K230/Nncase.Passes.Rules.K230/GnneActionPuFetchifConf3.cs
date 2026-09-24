using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuFetchifConf3 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr AddrS { get; }

	public Gpr Groups { get; }

	public Ssr Shape { get; }

	public GnneActionPuFetchifConf3()
		: base(GnneActionName.PuFetchifConf3)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		AddrS = new Gpr(-1, -1, needRenewal: false);
		Groups = new Gpr(-1, -1, needRenewal: false);
		Shape = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuFetchifConf3(int tcuId, int puId, int funct4, Gpr addrS, Gpr groups, Ssr shape)
		: base(GnneActionName.PuFetchifConf3)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		AddrS = addrS;
		Groups = groups;
		Shape = shape;
	}

	public static bool operator ==(GnneActionPuFetchifConf3 lhs, GnneActionPuFetchifConf3 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.AddrS.Value == rhs.AddrS.Value && lhs.Groups.Value == rhs.Groups.Value)
		{
			return lhs.Shape.Value == rhs.Shape.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuFetchifConf3 lhs, GnneActionPuFetchifConf3 rhs)
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
