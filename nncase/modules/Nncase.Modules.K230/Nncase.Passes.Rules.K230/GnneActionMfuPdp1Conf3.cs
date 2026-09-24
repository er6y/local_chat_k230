using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1Conf3 : GnneAction
{
	public int Funct5 { get; }

	public Gpr PeChannels { get; }

	public Gpr PeLastChannels { get; }

	public Gpr PadValue { get; }

	public Ssr Pad { get; }

	public GnneActionMfuPdp1Conf3()
		: base(GnneActionName.MfuPdp1Conf3)
	{
		Funct5 = -1;
		PeChannels = new Gpr(-1, -1, needRenewal: false);
		PeLastChannels = new Gpr(-1, -1, needRenewal: false);
		PadValue = new Gpr(-1, -1, needRenewal: false);
		Pad = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionMfuPdp1Conf3(int funct5, Gpr peChannels, Gpr peLastChannels, Gpr padValue, Ssr pad)
		: base(GnneActionName.MfuPdp1Conf3)
	{
		Funct5 = funct5;
		PeChannels = peChannels;
		PeLastChannels = peLastChannels;
		PadValue = padValue;
		Pad = pad;
	}

	public static bool operator ==(GnneActionMfuPdp1Conf3 lhs, GnneActionMfuPdp1Conf3 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.PeChannels.Value == rhs.PeChannels.Value && lhs.PeLastChannels.Value == rhs.PeLastChannels.Value && lhs.PadValue.Value == rhs.PadValue.Value)
		{
			return lhs.Pad.Value == rhs.Pad.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuPdp1Conf3 lhs, GnneActionMfuPdp1Conf3 rhs)
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
