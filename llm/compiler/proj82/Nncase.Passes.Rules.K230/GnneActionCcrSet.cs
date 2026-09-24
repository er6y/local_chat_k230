namespace Nncase.Passes.Rules.K230;

public class GnneActionCcrSet : GnneAction
{
	public int Ccr { get; }

	public int Value { get; }

	public GnneActionCcrSet(int ccr, int value)
		: base(GnneActionName.CcrSet)
	{
		Ccr = ccr;
		Value = value;
	}
}
