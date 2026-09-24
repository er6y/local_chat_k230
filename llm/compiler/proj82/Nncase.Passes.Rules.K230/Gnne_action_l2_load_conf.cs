using System;

namespace Nncase.Passes.Rules.K230;

public class Gnne_action_l2_load_conf : GnneAction
{
	public Ssr StrideD { get; }

	public Ssr StrideS { get; }

	public DataType L2Datatype { get; }

	public DataType DdrDatatype { get; }

	public Gnne_action_l2_load_conf()
		: base(GnneActionName.L2LoadConf)
	{
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		StrideS = new Ssr(-1, -1L, needRenewal: false);
		L2Datatype = DataTypes.UInt8;
		DdrDatatype = DataTypes.UInt8;
	}

	public Gnne_action_l2_load_conf(Ssr strideD, Ssr strideS, DataType l2Datatype, DataType ddrDatatype)
		: base(GnneActionName.L2LoadConf)
	{
		StrideD = strideD;
		StrideS = strideS;
		L2Datatype = l2Datatype;
		DdrDatatype = ddrDatatype;
	}

	public static bool operator ==(Gnne_action_l2_load_conf lhs, Gnne_action_l2_load_conf rhs)
	{
		if (lhs.StrideD.Value == rhs.StrideD.Value && lhs.StrideS.Value == rhs.StrideS.Value && lhs.L2Datatype == rhs.L2Datatype)
		{
			return lhs.DdrDatatype == rhs.DdrDatatype;
		}
		return false;
	}

	public static bool operator !=(Gnne_action_l2_load_conf lhs, Gnne_action_l2_load_conf rhs)
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
