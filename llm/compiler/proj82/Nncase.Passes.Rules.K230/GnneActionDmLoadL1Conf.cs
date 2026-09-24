using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadL1Conf : GnneAction
{
	public Ssr StrideS { get; }

	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public DataType L2Datatype { get; }

	public L1_TYPE L1Type { get; }

	public GnneActionDmLoadL1Conf()
		: base(GnneActionName.DmLoadL1Conf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		StrideS = new Ssr(-1, -1L, needRenewal: false);
		L2Datatype = DataTypes.UInt8;
		L1Type = L1_TYPE.if_;
	}

	public GnneActionDmLoadL1Conf(int tcuId, int puId, int funct4, Ssr strideS, DataType l2Datatype, L1_TYPE l1Type)
		: base(GnneActionName.DmLoadL1Conf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		StrideS = strideS;
		L2Datatype = l2Datatype;
		L1Type = l1Type;
	}

	public static bool operator ==(GnneActionDmLoadL1Conf lhs, GnneActionDmLoadL1Conf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.StrideS.Value == rhs.StrideS.Value && lhs.L2Datatype == rhs.L2Datatype)
		{
			return lhs.L1Type == rhs.L1Type;
		}
		return false;
	}

	public static bool operator !=(GnneActionDmLoadL1Conf lhs, GnneActionDmLoadL1Conf rhs)
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
