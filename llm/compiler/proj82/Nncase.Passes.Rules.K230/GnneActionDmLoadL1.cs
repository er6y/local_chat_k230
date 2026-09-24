using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadL1 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public Gpr AddrS { get; }

	public Ssr Shape { get; }

	public Gpr HtocWindow { get; }

	public L1_TYPE L1Type { get; }

	public GnneActionDmLoadL1(int tcuId, int puId, Gpr addrS, Ssr shape, Gpr htocWindow, L1_TYPE l1Type, SegmentND sliceInfo)
		: base(GnneActionName.DmLoadL1)
	{
		TcuId = tcuId;
		PuId = puId;
		AddrS = addrS;
		Shape = shape;
		HtocWindow = htocWindow;
		L1Type = l1Type;
	}
}
