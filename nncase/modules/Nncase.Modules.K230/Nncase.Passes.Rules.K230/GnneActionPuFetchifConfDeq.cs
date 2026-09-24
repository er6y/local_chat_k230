using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuFetchifConfDeq : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Gpr Ic { get; }

	public Gpr Bx { get; }

	public DataType QuantType { get; }

	public GnneActionPuFetchifConfDeq()
		: base(GnneActionName.PuFetchifConfDeq)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		Ic = new Gpr(-1, -1, needRenewal: false);
		Bx = new Gpr(-1, -1, needRenewal: false);
		QuantType = DataTypes.UInt8;
	}

	public GnneActionPuFetchifConfDeq(int tcuId, int puId, int funct4, Gpr ic, Gpr bx, DataType quantType)
		: base(GnneActionName.PuFetchifConfDeq)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		Ic = ic;
		Bx = bx;
		QuantType = quantType;
	}

	public static bool operator ==(GnneActionPuFetchifConfDeq lhs, GnneActionPuFetchifConfDeq rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.Ic.Value == rhs.Ic.Value && lhs.Bx.Value == rhs.Bx.Value)
		{
			return lhs.QuantType == rhs.QuantType;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuFetchifConfDeq lhs, GnneActionPuFetchifConfDeq rhs)
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
