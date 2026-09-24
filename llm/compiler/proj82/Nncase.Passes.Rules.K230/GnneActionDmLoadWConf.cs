using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadWConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public int KernelH { get; }

	public int KernelW { get; }

	public Gpr StrideOc { get; }

	public GnneActionDmLoadWConf()
		: base(GnneActionName.DmLoadWConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		KernelH = -1;
		KernelW = -1;
		StrideOc = new Gpr(-1, -1, needRenewal: false);
	}

	public GnneActionDmLoadWConf(int tcuId, int puId, int funct4, int kernelH, int kernelW, Gpr strideOc)
		: base(GnneActionName.DmLoadWConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		KernelH = kernelH;
		KernelW = kernelW;
		StrideOc = strideOc;
	}

	public static bool operator ==(GnneActionDmLoadWConf lhs, GnneActionDmLoadWConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.KernelH == rhs.KernelH && lhs.KernelW == rhs.KernelW)
		{
			return lhs.StrideOc.Value == rhs.StrideOc.Value;
		}
		return false;
	}

	public static bool operator !=(GnneActionDmLoadWConf lhs, GnneActionDmLoadWConf rhs)
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
