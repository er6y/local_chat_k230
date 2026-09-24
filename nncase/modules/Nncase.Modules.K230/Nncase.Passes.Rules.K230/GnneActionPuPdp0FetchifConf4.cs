using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0FetchifConf4 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr PadValue { get; }

	public Ssr Pad { get; }

	public GnneActionPuPdp0FetchifConf4()
		: base(GnneActionName.PuPdp0FetchifConf4)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		PadValue = new Gpr(-1, -1, needRenewal: false);
		Pad = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuPdp0FetchifConf4(int tcuId, int puId, int funct4, Gpr padValue, Ssr pad)
		: base(GnneActionName.PuPdp0FetchifConf4)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		PadValue = padValue;
		Pad = pad;
	}

	public static bool operator ==(GnneActionPuPdp0FetchifConf4 lhs, GnneActionPuPdp0FetchifConf4 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.PadValue.Value == rhs.PadValue.Value)
		{
			return lhs.Pad.Value == rhs.Pad.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0FetchifConf4 lhs, GnneActionPuPdp0FetchifConf4 rhs)
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
