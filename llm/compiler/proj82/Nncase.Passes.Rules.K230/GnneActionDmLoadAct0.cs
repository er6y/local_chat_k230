using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmLoadAct0 : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public Gpr AddrS { get; }

	public Gpr Len { get; }

	public ACT0_CHANNEL DestChannel { get; }

	public bool IsByChannel { get; }

	public GnneActionDmLoadAct0(int tcuId, int puId, Gpr addrS, Gpr len, ACT0_CHANNEL destChannel, bool isByChannel)
		: base(GnneActionName.DmLoadAct0)
	{
		TcuId = tcuId;
		PuId = puId;
		AddrS = addrS;
		Len = len;
		DestChannel = destChannel;
		IsByChannel = isByChannel;
	}
}
