using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1ConfDest : GnneAction
{
	private Ssr _shape;

	public int Funct5 { get; }

	public Gpr Len { get; }

	public Ssr Shape => _shape;

	public GnneActionMfuAct1ConfDest()
		: base(GnneActionName.MfuAct1ConfDest)
	{
		Funct5 = -1;
		Len = new Gpr(-1, -1, needRenewal: false);
		_shape = new Ssr(-1, -1L, needRenewal: false);
	}

	public GnneActionMfuAct1ConfDest(int funct5, Gpr len, Ssr shape)
		: base(GnneActionName.MfuAct1ConfDest)
	{
		Funct5 = funct5;
		Len = len;
		_shape = shape;
	}

	public static bool operator ==(GnneActionMfuAct1ConfDest lhs, GnneActionMfuAct1ConfDest rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs._shape.Value == rhs.Shape.Value)
		{
			return lhs.Len.Value == rhs.Len.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1ConfDest lhs, GnneActionMfuAct1ConfDest rhs)
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
