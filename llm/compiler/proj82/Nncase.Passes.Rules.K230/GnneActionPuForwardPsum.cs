namespace Nncase.Passes.Rules.K230;

public class GnneActionPuForwardPsum : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public Gpr Addr { get; }

	public Gpr Len { get; }

	public GnneActionPuForwardPsum(int tcuId, int puId, Gpr addr, Gpr len)
		: base(GnneActionName.PuForwardPsum)
	{
		TcuId = tcuId;
		PuId = puId;
		Addr = addr;
		Len = len;
	}
}
