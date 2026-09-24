using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1ConfDeq : GnneAction
{
	public int Funct5 { get; }

	public Gpr Scale { get; }

	public Gpr Bias { get; }

	public DataType QuantType { get; }

	public int RshiftBits { get; }

	public GnneActionMfuPdp1ConfDeq()
		: base(GnneActionName.MfuPdp1ConfDeq)
	{
		Funct5 = -1;
		Scale = new Gpr(-1, 1, needRenewal: false);
		Bias = new Gpr(-1, -1, needRenewal: false);
		QuantType = DataTypes.UInt8;
		RshiftBits = -1;
	}

	public GnneActionMfuPdp1ConfDeq(int funct5, Gpr scale, Gpr bias, DataType quantType, int rshiftBits)
		: base(GnneActionName.MfuPdp1ConfDeq)
	{
		Funct5 = funct5;
		Scale = scale;
		Bias = bias;
		QuantType = quantType;
		RshiftBits = rshiftBits;
	}

	public static bool operator ==(GnneActionMfuPdp1ConfDeq lhs, GnneActionMfuPdp1ConfDeq rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.Scale.Value == rhs.Scale.Value && lhs.Bias.Value == rhs.Bias.Value && lhs.QuantType == rhs.QuantType)
		{
			return lhs.RshiftBits == rhs.RshiftBits;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuPdp1ConfDeq lhs, GnneActionMfuPdp1ConfDeq rhs)
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
