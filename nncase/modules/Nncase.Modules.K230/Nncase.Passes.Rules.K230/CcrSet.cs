namespace Nncase.Passes.Rules.K230;

public class CcrSet
{
	public int Ccr { get; set; }

	public int Value { get; set; }

	public CcrSet(int ccr, int value)
	{
		Ccr = ccr;
		Value = value;
	}
}
