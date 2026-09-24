using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0FetchifConf3 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Ssr Shape { get; }

	public GnneActionPuPdp0FetchifConf3()
		: base(GnneActionName.PuPdp0FetchifConf3)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Shape = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuPdp0FetchifConf3(int tcuId, int puId, int funct4, Ssr shape)
		: base(GnneActionName.PuPdp0FetchifConf3)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Shape = shape;
	}

	public static bool operator ==(GnneActionPuPdp0FetchifConf3 lhs, GnneActionPuPdp0FetchifConf3 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4)
		{
			return lhs.Shape.Value == rhs.Shape.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0FetchifConf3 lhs, GnneActionPuPdp0FetchifConf3 rhs)
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
