using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionAct0Compute : GnneAction
{
	public int TcuId { get; }

	public Gpr AddrD { get; }

	public ACT0_CHANNEL Channel { get; }

	public ACT0_OUTPUT_DEST Target { get; }

	public DataType DestDatatype { get; }

	public bool IsByChannel { get; }

	public GnneActionAct0Compute(int tcuId, Gpr addrD, ACT0_CHANNEL channel, ACT0_OUTPUT_DEST target, DataType destDatatype, bool isByChannel)
		: base(GnneActionName.Act0Compute)
	{
		TcuId = tcuId;
		AddrD = addrD;
		Channel = channel;
		Target = target;
		DestDatatype = destDatatype;
		IsByChannel = isByChannel;
	}
}
