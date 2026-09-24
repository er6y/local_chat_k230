using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmStoreOfConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public Ssr StrideD { get; }

	public DataType Datatype { get; }

	public GnneActionDmStoreOfConf()
		: base(GnneActionName.DmStoreOfConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		Datatype = DataTypes.UInt8;
	}

	public GnneActionDmStoreOfConf(int tcuId, int puId, int funct4, Ssr strideD, DataType datatype)
		: base(GnneActionName.DmStoreOfConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		StrideD = strideD;
		Datatype = datatype;
	}

	public static bool operator ==(GnneActionDmStoreOfConf lhs, GnneActionDmStoreOfConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.StrideD.Value == rhs.StrideD.Value)
		{
			return lhs.Datatype == rhs.Datatype;
		}
		return false;
	}

	public static bool operator !=(GnneActionDmStoreOfConf lhs, GnneActionDmStoreOfConf rhs)
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
