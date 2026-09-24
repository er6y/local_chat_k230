using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuTransposeConf : GnneAction
{
	public int Funct5 { get; }

	public DataType L2Datatype { get; }

	public MFU_TRANS_PERMUTE Permute { get; }

	public Ssr StrideD { get; }

	public Ssr StrideS { get; }

	public GnneActionMfuTransposeConf()
		: base(GnneActionName.MfuTransposeConf)
	{
		Funct5 = -1;
		StrideD = new Ssr(-1, -1L, needRenewal: false);
		StrideS = new Ssr(-1, -1L, needRenewal: false);
		L2Datatype = DataTypes.UInt8;
		Permute = MFU_TRANS_PERMUTE.CHNW;
	}

	public GnneActionMfuTransposeConf(int funct5, Ssr strideD, Ssr strideS, DataType l2Datatype, MFU_TRANS_PERMUTE permute)
		: base(GnneActionName.MfuTransposeConf)
	{
		Funct5 = funct5;
		StrideD = strideD;
		StrideS = strideS;
		L2Datatype = l2Datatype;
		Permute = permute;
	}

	public static bool operator ==(GnneActionMfuTransposeConf lhs, GnneActionMfuTransposeConf rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.StrideD.Value == rhs.StrideD.Value && lhs.StrideS.Value == rhs.StrideS.Value && lhs.L2Datatype == rhs.L2Datatype)
		{
			return lhs.Permute == rhs.Permute;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuTransposeConf lhs, GnneActionMfuTransposeConf rhs)
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
