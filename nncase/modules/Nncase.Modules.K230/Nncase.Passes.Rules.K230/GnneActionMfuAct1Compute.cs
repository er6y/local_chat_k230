namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuAct1Compute : GnneAction
{
	public Gpr AddrD1 { get; }

	public Gpr AddrS1 { get; }

	public Gpr AddrS2 { get; }

	public Gpr AddrArg { get; }

	public GnneActionMfuAct1Compute(Gpr addrD1, Gpr addrS1, Gpr addrS2, Gpr addrArg)
		: base(GnneActionName.MfuAct1Compute)
	{
		AddrD1 = addrD1;
		AddrS1 = addrS1;
		AddrS2 = addrS2;
		AddrArg = addrArg;
	}
}
