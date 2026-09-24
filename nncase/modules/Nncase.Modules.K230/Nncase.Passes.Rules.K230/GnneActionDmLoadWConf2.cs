using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadWConf2 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Groups { get; }

	public Gpr Goc { get; }

	public GnneActionDmLoadWConf2()
		: base(GnneActionName.DmLoadWConf2)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Groups = new Gpr(-1, -1, needRenewal: false);
		Goc = new Gpr(-1, -1, needRenewal: false);
	}

	public GnneActionDmLoadWConf2(int tcuId, int puId, int funct4, Gpr groups, Gpr goc)
		: base(GnneActionName.DmLoadWConf2)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Groups = groups;
		Goc = goc;
	}

	public static bool operator ==(GnneActionDmLoadWConf2 lhs, GnneActionDmLoadWConf2 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.Groups.Value == rhs.Groups.Value)
		{
			return lhs.Goc.Value == rhs.Goc.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionDmLoadWConf2 lhs, GnneActionDmLoadWConf2 rhs)
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
