using System;

namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1ConfSrc1 : GnneAction
{
	public int Funct5 { get; }

	public Gpr Slice { get; }

	public Gpr RightRepeats { get; }

	public Gpr SliceRepeats { get; }

	public int Sid { get; }

	public int SliceLoc { get; }

	public GnneActionMfuAct1ConfSrc1()
		: base(GnneActionName.MfuAct1ConfSrc1)
	{
		Funct5 = -1;
		Slice = new Gpr(-1, -1, needRenewal: false);
		RightRepeats = new Gpr(-1, -1, needRenewal: false);
		SliceRepeats = new Gpr(-1, -1, needRenewal: false);
		Sid = -1;
		SliceLoc = -1;
	}

	public GnneActionMfuAct1ConfSrc1(int funct5, Gpr slice, Gpr rightRepeats, Gpr sliceRepeats, int sid, int sliceLoc)
		: base(GnneActionName.MfuAct1ConfSrc1)
	{
		Funct5 = funct5;
		Slice = slice;
		RightRepeats = rightRepeats;
		SliceRepeats = sliceRepeats;
		Sid = sid;
		SliceLoc = sliceLoc;
	}

	public static bool operator ==(GnneActionMfuAct1ConfSrc1 lhs, GnneActionMfuAct1ConfSrc1 rhs)
	{
		if (lhs.Funct5 == rhs.Funct5 && lhs.Slice.Value == rhs.Slice.Value && lhs.RightRepeats.Value == rhs.RightRepeats.Value && lhs.SliceRepeats.Value == rhs.SliceRepeats.Value && lhs.Sid == rhs.Sid)
		{
			return lhs.SliceLoc == rhs.SliceLoc;
		}
		return false;
	}

	public static bool operator !=(GnneActionMfuAct1ConfSrc1 lhs, GnneActionMfuAct1ConfSrc1 rhs)
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
