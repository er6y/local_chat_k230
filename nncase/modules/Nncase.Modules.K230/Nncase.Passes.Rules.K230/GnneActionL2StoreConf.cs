using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionL2StoreConf : GnneAction
{
	public Ssr StrideD { get; }

	public Ssr StrideS { get; }

	public DataType L2Datatype { get; }

	public DataType DdrDatatype { get; }

	public GnneActionL2StoreConf()
		: base(GnneActionName.L2StoreConf)
	{
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		StrideS = new Ssr(-1, -1L, needRenewal: false);
		L2Datatype = DataTypes.UInt8;
		DdrDatatype = DataTypes.UInt8;
	}

	public GnneActionL2StoreConf(Ssr strideD, Ssr strideS, DataType l2Datatype, DataType ddrDatatype)
		: base(GnneActionName.L2StoreConf)
	{
		StrideD = strideD;
		StrideS = strideS;
		L2Datatype = l2Datatype;
		DdrDatatype = ddrDatatype;
	}

	public static bool operator ==(GnneActionL2StoreConf lhs, GnneActionL2StoreConf rhs)
	{
		if (lhs.StrideD.Value == rhs.StrideD.Value && lhs.StrideS.Value == rhs.StrideS.Value && lhs.L2Datatype == rhs.L2Datatype)
		{
			return lhs.DdrDatatype == rhs.DdrDatatype;
		}
		return false;
	}

	public static bool operator !=(GnneActionL2StoreConf lhs, GnneActionL2StoreConf rhs)
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
