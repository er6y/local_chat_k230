using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1Conf : GnneAction
{
	public int Funct5 { get; }

	public MFU_ACT1_FUNCTION Funct4 { get; }

	public bool IsByChannel { get; }

	public bool Is16Segments { get; }

	public GnneActionMfuAct1Conf()
		: base(GnneActionName.MfuAct1Conf)
	{
		Funct5 = -1;
		Funct4 = MFU_ACT1_FUNCTION.add;
		IsByChannel = false;
		Is16Segments = false;
	}

	public GnneActionMfuAct1Conf(int funct5, MFU_ACT1_FUNCTION funct4, bool is_by_channel, bool is_16_segments)
		: base(GnneActionName.MfuAct1Conf)
	{
		Funct5 = funct5;
		Funct4 = funct4;
		IsByChannel = is_by_channel;
		Is16Segments = is_16_segments;
	}

	public static bool operator ==(GnneActionMfuAct1Conf lhs, GnneActionMfuAct1Conf rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.Funct4 == rhs.Funct4 && lhs.IsByChannel == rhs.IsByChannel)
		{
			return lhs.Is16Segments == rhs.Is16Segments;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1Conf lhs, GnneActionMfuAct1Conf rhs)
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
