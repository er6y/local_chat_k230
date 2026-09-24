namespace Nncase.Passes.Rules.K230;

public class GnneActionIntr : GnneAction
{
	public Gpr IntrNum { get; }

	public GnneActionIntr(Gpr intrNum)
		: base(GnneActionName.Intr)
	{
		IntrNum = intrNum;
	}
}
