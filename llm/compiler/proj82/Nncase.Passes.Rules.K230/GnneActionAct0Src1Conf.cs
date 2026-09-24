using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionAct0Src1Conf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct3 { get; }

	public ACT0_CHANNEL Channel { get; }

	public Ssr Shape { get; }

	public int RshiftBits { get; }

	public GnneActionAct0Src1Conf()
		: base(GnneActionName.Act0Src1Conf)
	{
		TcuId = -1;
		PuId = -1;
		Funct3 = -1;
		Channel = ACT0_CHANNEL.pu;
		Shape = new Ssr(-1, -1L, needRenewal: false);
		RshiftBits = -1;
	}

	public GnneActionAct0Src1Conf(int tcuId, int puId, int funct3, ACT0_CHANNEL channel, Ssr shape, int rshiftBits)
		: base(GnneActionName.Act0Src1Conf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct3 = funct3;
		Channel = channel;
		Shape = shape;
		RshiftBits = rshiftBits;
	}

	public static bool operator ==(GnneActionAct0Src1Conf lhs, GnneActionAct0Src1Conf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct3 == rhs.Funct3 && lhs.Channel == rhs.Channel && lhs.Shape.Value == rhs.Shape.Value)
		{
			return lhs.RshiftBits == rhs.RshiftBits;
		}
		return false;
	}

	public static bool operator !=(GnneActionAct0Src1Conf lhs, GnneActionAct0Src1Conf rhs)
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
