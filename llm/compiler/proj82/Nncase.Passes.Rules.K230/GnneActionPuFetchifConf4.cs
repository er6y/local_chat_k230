using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuFetchifConf4 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr PadValue { get; }

	public Ssr Pad { get; }

	public GnneActionPuFetchifConf4()
		: base(GnneActionName.PuFetchifConf4)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		PadValue = new Gpr(-1, -1, needRenewal: false);
		Pad = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionPuFetchifConf4(int tcuId, int puId, int funct4, Gpr padValue, Ssr pad)
		: base(GnneActionName.PuFetchifConf4)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		PadValue = padValue;
		Pad = pad;
	}

	public static bool operator ==(GnneActionPuFetchifConf4 lhs, GnneActionPuFetchifConf4 rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.PadValue.Value == rhs.PadValue.Value)
		{
			return lhs.Pad.Value == rhs.Pad.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuFetchifConf4 lhs, GnneActionPuFetchifConf4 rhs)
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
