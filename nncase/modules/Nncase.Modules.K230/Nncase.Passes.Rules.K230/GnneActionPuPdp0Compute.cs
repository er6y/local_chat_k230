namespace Nncase.Passes.Rules.K230;

public class GnneActionPuPdp0Compute : GnneAction
{
	public int TcuId { get; }

	public Gpr AddrS { get; }

	public GnneActionPuPdp0Compute(int tcuId, Gpr addrS)
		: base(GnneActionName.PuPdp0Compute)
	{
		TcuId = tcuId;
		AddrS = addrS;
	}
}
