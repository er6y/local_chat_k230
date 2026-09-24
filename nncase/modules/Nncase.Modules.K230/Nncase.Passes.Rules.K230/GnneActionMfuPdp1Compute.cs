namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuPdp1Compute : GnneAction
{
	public Gpr AddrD { get; }

	public Gpr AddrS { get; }

	public Ssr Shape { get; }

	public GnneActionMfuPdp1Compute(Gpr addrD, Gpr addrS, Ssr shape)
		: base(GnneActionName.MfuPdp1Compute)
	{
		AddrD = addrD;
		AddrS = addrS;
		Shape = shape;
	}
}
