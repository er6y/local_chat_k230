using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1Conf4 : GnneAction
{
	public int Funct5 { get; }

	public Gpr WindowW { get; }

	public Gpr WindowH { get; }

	public Gpr Scale { get; }

	public bool EnableH2C { get; }

	public bool EnableBw { get; }

	public GnneActionMfuPdp1Conf4()
		: base(GnneActionName.MfuPdp1Conf4)
	{
		Funct5 = -1;
		WindowW = new Gpr(-1, -1, needRenewal: false);
		WindowH = new Gpr(-1, -1, needRenewal: false);
		Scale = new Gpr(-1, -1, needRenewal: false);
		EnableH2C = false;
		EnableBw = false;
	}

	public GnneActionMfuPdp1Conf4(int funct5, Gpr windowW, Gpr windowH, Gpr scale, bool enableH2C, bool enableBw)
		: base(GnneActionName.MfuPdp1Conf4)
	{
		Funct5 = funct5;
		WindowW = windowW;
		WindowH = windowH;
		Scale = scale;
		EnableH2C = enableH2C;
		EnableBw = enableBw;
	}

	public static bool operator ==(GnneActionMfuPdp1Conf4 lhs, GnneActionMfuPdp1Conf4 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.WindowW.Value == rhs.WindowW.Value && lhs.WindowH.Value == rhs.WindowH.Value && lhs.Scale.Value == rhs.Scale.Value && lhs.EnableH2C == rhs.EnableH2C)
		{
			return lhs.EnableBw == rhs.EnableBw;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuPdp1Conf4 lhs, GnneActionMfuPdp1Conf4 rhs)
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
