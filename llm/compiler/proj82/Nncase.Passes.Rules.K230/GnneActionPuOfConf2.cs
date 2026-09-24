using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuOfConf2 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr AddrD { get; }

	public Ssr ShapeD { get; }

	public GnneActionPuOfConf2()
		: base(GnneActionName.PuOfConf2)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		AddrD = new Gpr(-1, -1, needRenewal: false);
		ShapeD = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuOfConf2(int tcuId, int puId, int funct4, Gpr addrD, Ssr shapeD)
		: base(GnneActionName.PuOfConf2)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		AddrD = addrD;
		ShapeD = shapeD;
	}

	public static bool operator ==(GnneActionPuOfConf2 lhs, GnneActionPuOfConf2 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.AddrD.Value == rhs.AddrD.Value)
		{
			return lhs.ShapeD.Value == rhs.ShapeD.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuOfConf2 lhs, GnneActionPuOfConf2 rhs)
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
