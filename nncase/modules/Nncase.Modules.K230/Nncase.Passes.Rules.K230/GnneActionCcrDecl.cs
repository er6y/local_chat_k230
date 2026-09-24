namespace Nncase.Passes.Rules.K230;

public class GnneActionCcrDecl : GnneAction
{
	public Gpr Num { get; }

	public GnneActionCcrDecl(Gpr num)
		: base(GnneActionName.CcrDecl)
	{
		Num = num;
	}
}
