using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1ConfSrc2 : GnneAction
{
	public int Funct5 { get; }

	public Gpr LeftRepeats { get; }

	public Ssr Shape { get; }

	public int Sid { get; }

	public ACT1_SOURCE_TYPE SourceType { get; }

	public GnneActionMfuAct1ConfSrc2()
		: base(GnneActionName.MfuAct1ConfSrc2)
	{
		Funct5 = -1;
		LeftRepeats = new Gpr(-1, -1, needRenewal: false);
		Shape = new Ssr(-1, -1L, needRenewal: false);
		Sid = -1;
		SourceType = ACT1_SOURCE_TYPE.l2;
	}

	public GnneActionMfuAct1ConfSrc2(int funct5, Gpr leftRepeats, Ssr shape, int sid, ACT1_SOURCE_TYPE sourceType)
		: base(GnneActionName.MfuAct1ConfSrc2)
	{
		Funct5 = funct5;
		LeftRepeats = leftRepeats;
		Shape = shape;
		Sid = sid;
		SourceType = sourceType;
	}

	public static bool operator ==(GnneActionMfuAct1ConfSrc2 lhs, GnneActionMfuAct1ConfSrc2 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.LeftRepeats.Value == rhs.LeftRepeats.Value && lhs.Shape.Value == rhs.Shape.Value && lhs.Sid == rhs.Sid)
		{
			return lhs.SourceType == rhs.SourceType;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1ConfSrc2 lhs, GnneActionMfuAct1ConfSrc2 rhs)
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
