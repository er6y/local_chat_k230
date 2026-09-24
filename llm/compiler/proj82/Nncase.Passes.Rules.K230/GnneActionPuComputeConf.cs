using System;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuComputeConf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public int Funct4 { get; }

	public bool LoadPsum { get; }

	public bool ClrPsum { get; }

	public PU_OUTPUT_DEST DestTarget { get; }

	public bool ReleaseIf { get; }

	public PU_COMPUTE_MODE Mode { get; }

	public GnneActionPuComputeConf()
		: base(GnneActionName.PuComputeConf)
	{
		TcuId = -1;
		PuId = -1;
		Funct4 = -1;
		LoadPsum = false;
		ClrPsum = false;
		DestTarget = PU_OUTPUT_DEST.psum;
		ReleaseIf = false;
		Mode = PU_COMPUTE_MODE.pu_mode_normal;
	}

	public GnneActionPuComputeConf(int tcuId, int puId, int funct4, bool loadPsum, bool clrPsum, PU_OUTPUT_DEST destTarget, bool releaseIf, PU_COMPUTE_MODE mode)
		: base(GnneActionName.PuComputeConf)
	{
		TcuId = tcuId;
		PuId = puId;
		Funct4 = funct4;
		LoadPsum = loadPsum;
		ClrPsum = clrPsum;
		DestTarget = destTarget;
		ReleaseIf = releaseIf;
		Mode = mode;
	}

	public static bool operator ==(GnneActionPuComputeConf lhs, GnneActionPuComputeConf rhs)
	{
		if (lhs.TcuId == rhs.TcuId && lhs.PuId == rhs.PuId && lhs.Funct4 == rhs.Funct4 && lhs.LoadPsum == rhs.LoadPsum && lhs.ClrPsum == rhs.ClrPsum && lhs.DestTarget == rhs.DestTarget && lhs.ReleaseIf == rhs.ReleaseIf)
		{
			return lhs.Mode == rhs.Mode;
		}
		return false;
	}

	public static bool operator !=(GnneActionPuComputeConf lhs, GnneActionPuComputeConf rhs)
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
