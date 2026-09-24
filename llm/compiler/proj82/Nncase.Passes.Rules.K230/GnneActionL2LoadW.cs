namespace Nncase.Passes.Rules.K230;

public class GnneActionL2LoadW : GnneAction
{
	public Gpr Basement { get; }

	public Gpr AddrD { get; }

	public Gpr AddrS { get; }

	public Gpr ValidCNum { get; }

	public GnneActionL2LoadW(Gpr basement, Gpr addrD, Gpr addrS, Gpr validCNum)
		: base(GnneActionName.L2LoadW)
	{
		Basement = basement;
		AddrD = addrD;
		AddrS = addrS;
		ValidCNum = validCNum;
	}
}
