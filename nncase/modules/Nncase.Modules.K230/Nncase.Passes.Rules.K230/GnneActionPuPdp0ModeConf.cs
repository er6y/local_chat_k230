using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0ModeConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public PU_PDP0_MODE Mode { get; }

	public GnneActionPuPdp0ModeConf()
		: base(GnneActionName.PuPdp0ModeConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Mode = PU_PDP0_MODE.dw;
	}

	public GnneActionPuPdp0ModeConf(int tcuId, int puId, int funct4, PU_PDP0_MODE mode)
		: base(GnneActionName.PuPdp0ModeConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Mode = mode;
	}

	public static bool operator ==(GnneActionPuPdp0ModeConf lhs, GnneActionPuPdp0ModeConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4)
		{
			return lhs.Mode == rhs.Mode;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0ModeConf lhs, GnneActionPuPdp0ModeConf rhs)
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
