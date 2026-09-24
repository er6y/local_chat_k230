using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuWConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public int KernelH { get; }

	public int KernelW { get; }

	public GnneActionPuWConf()
		: base(GnneActionName.PuWConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		KernelH = -1;
		KernelW = -1;
	}

	public GnneActionPuWConf(int tcuId, int puId, int funct4, int kernelH, int kernelW)
		: base(GnneActionName.PuWConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		KernelH = kernelH;
		KernelW = kernelW;
	}

	public static bool operator ==(GnneActionPuWConf lhs, GnneActionPuWConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.KernelH == rhs.KernelH)
		{
			return lhs.KernelW == rhs.KernelW;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuWConf lhs, GnneActionPuWConf rhs)
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
