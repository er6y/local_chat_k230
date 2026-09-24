using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0ConfDeq : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Bx { get; }

	public DataType QuantType { get; }

	public GnneActionPuPdp0ConfDeq()
		: base(GnneActionName.PuPdp0ConfDeq)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Bx = new Gpr(-1, -1, needRenewal: false);
		QuantType = DataTypes.UInt8;
	}

	public GnneActionPuPdp0ConfDeq(int tcuId, int puId, int funct4, Gpr bx, DataType quantType)
		: base(GnneActionName.PuPdp0ConfDeq)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Bx = bx;
		QuantType = quantType;
	}

	public static bool operator ==(GnneActionPuPdp0ConfDeq lhs, GnneActionPuPdp0ConfDeq rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.Bx.Value == rhs.Bx.Value)
		{
			return lhs.QuantType == rhs.QuantType;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuPdp0ConfDeq lhs, GnneActionPuPdp0ConfDeq rhs)
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
