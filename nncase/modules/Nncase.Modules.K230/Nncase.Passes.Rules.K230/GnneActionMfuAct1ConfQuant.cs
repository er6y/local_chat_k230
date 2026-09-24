using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1ConfQuant : GnneAction
{
	public int Funct5 { get; }

	public DataType QuantType { get; }

	public int RshiftBits { get; }

	public GnneActionMfuAct1ConfQuant()
		: base(GnneActionName.MfuAct1ConfQuant)
	{
		Funct5 = -1;
		QuantType = DataTypes.UInt8;
		RshiftBits = -1;
	}

	public GnneActionMfuAct1ConfQuant(int funct5, DataType quant_type, int rshift_bits)
		: base(GnneActionName.MfuAct1ConfQuant)
	{
		Funct5 = funct5;
		QuantType = quant_type;
		RshiftBits = rshift_bits;
	}

	public static bool operator ==(GnneActionMfuAct1ConfQuant lhs, GnneActionMfuAct1ConfQuant rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.QuantType == rhs.QuantType)
		{
			return lhs.RshiftBits == rhs.RshiftBits;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1ConfQuant lhs, GnneActionMfuAct1ConfQuant rhs)
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
