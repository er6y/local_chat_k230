using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionDmStoreOf : GnneAction
{
	public int TcuId { get; }

	public int PuId { get; }

	public Gpr AddrD { get; }

	public Ssr Shape { get; }

	public ACT0_CHANNEL SrcChannel { get; }

	public GnneActionDmStoreOf(int tcuId, int puId, Gpr addrD, Ssr shape, ACT0_CHANNEL srcChannel)
		: base(GnneActionName.DmStoreOf)
	{
		TcuId = tcuId;
		PuId = puId;
		AddrD = addrD;
		Shape = shape;
		SrcChannel = srcChannel;
	}
}
