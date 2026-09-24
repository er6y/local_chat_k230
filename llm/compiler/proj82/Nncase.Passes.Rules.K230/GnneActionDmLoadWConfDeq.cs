using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadWConfDeq : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public DataType QuantType { get; }

	public GnneActionDmLoadWConfDeq()
		: base(GnneActionName.DmLoadWConfDeq)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		QuantType = DataTypes.UInt8;
	}

	public GnneActionDmLoadWConfDeq(int tcuId, int puId, int funct4, DataType quantType)
		: base(GnneActionName.DmLoadWConfDeq)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		QuantType = quantType;
	}

	public static bool operator ==(GnneActionDmLoadWConfDeq lhs, GnneActionDmLoadWConfDeq rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4)
		{
			return lhs.QuantType == rhs.QuantType;
		}
		return false;
	}

	public static bool operator !=(GnneActionDmLoadWConfDeq lhs, GnneActionDmLoadWConfDeq rhs)
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
