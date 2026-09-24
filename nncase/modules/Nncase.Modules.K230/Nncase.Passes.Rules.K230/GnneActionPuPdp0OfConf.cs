using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0OfConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Ssr StrideD { get; }

	public Ssr ShapeD { get; }

	public GnneActionPuPdp0OfConf()
		: base(GnneActionName.PuPdp0OfConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		ShapeD = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuPdp0OfConf(int tcuId, int puId, int funct4, Ssr strideD, Ssr shapeD)
		: base(GnneActionName.PuPdp0OfConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		StrideD = strideD;
		ShapeD = shapeD;
	}

	public static bool operator ==(GnneActionPuPdp0OfConf lhs, GnneActionPuPdp0OfConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.StrideD.Value == rhs.StrideD.Value)
		{
			return lhs.ShapeD.Value == rhs.ShapeD.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0OfConf lhs, GnneActionPuPdp0OfConf rhs)
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
