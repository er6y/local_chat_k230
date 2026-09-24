using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1Conf2 : GnneAction
{
	public int Funct5 { get; }

	public Gpr CountW { get; }

	public Gpr CountH { get; }

	public Gpr PeH { get; }

	public Gpr PeLastH { get; }

	public GnneActionMfuPdp1Conf2()
		: base(GnneActionName.MfuPdp1Conf2)
	{
		Funct5 = -1;
		CountW = new Gpr(-1, -1, needRenewal: false);
		CountH = new Gpr(-1, -1, needRenewal: false);
		PeH = new Gpr(-1, -1, needRenewal: false);
		PeLastH = new Gpr(-1, -1, needRenewal: false);
	}

	public GnneActionMfuPdp1Conf2(int funct5, Gpr countW, Gpr countH, Gpr peH, Gpr peLastH)
		: base(GnneActionName.MfuPdp1Conf2)
	{
		Funct5 = funct5;
		CountW = countW;
		CountH = countH;
		PeH = peH;
		PeLastH = peLastH;
	}

	public static bool operator ==(GnneActionMfuPdp1Conf2 lhs, GnneActionMfuPdp1Conf2 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.CountW.Value == rhs.CountW.Value && lhs.CountH.Value == rhs.CountH.Value && lhs.PeH.Value == rhs.PeH.Value)
		{
			return lhs.PeLastH.Value == rhs.PeLastH.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuPdp1Conf2 lhs, GnneActionMfuPdp1Conf2 rhs)
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
