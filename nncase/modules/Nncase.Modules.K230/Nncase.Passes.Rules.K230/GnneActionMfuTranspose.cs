namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuTranspose : GnneAction
{
	public Gpr AddrD { get; }

	public Gpr AddrS { get; }

	public Ssr Shape { get; }

	public GnneActionMfuTranspose(Gpr addrD, Gpr addrS, Ssr shape)
		: base(GnneActionName.MfuTranspose)
	{
		AddrD = addrD;
		AddrS = addrS;
		Shape = shape;
	}
}
