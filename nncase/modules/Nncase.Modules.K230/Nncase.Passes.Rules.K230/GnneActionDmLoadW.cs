using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadW : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public Gpr AddrS { get; }

	public Gpr AddrBw { get; }

	public Ssr Iochannels { get; }

	public DM_LOAD_W_DEST DestType { get; }

	public GnneActionDmLoadW(int tcuId, int puId, Gpr addrS, Gpr addrBw, Ssr iochannels, DM_LOAD_W_DEST destType, SegmentND sliceInfo)
		: base(GnneActionName.DmLoadW)
	{
		TcuId = tcuId;
		PuId = puId;
		AddrS = addrS;
		AddrBw = addrBw;
		Iochannels = iochannels;
		DestType = destType;
	}
}
