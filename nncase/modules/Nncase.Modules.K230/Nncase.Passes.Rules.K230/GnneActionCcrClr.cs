namespace Nncase.Passes.Rules.K230;

public class GnneActionCcrClr : GnneAction
{
	public int Ccr { get; }

	public GnneActionCcrClr(int ccr)
		: base(GnneActionName.CcrClr)
	{
		Ccr = ccr;
	}
}
