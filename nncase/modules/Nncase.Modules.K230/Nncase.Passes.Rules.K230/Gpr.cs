using Nncase.IR;

namespace Nncase.Passes.Rules.K230;

public class Gpr
{
	public int Index { get; set; }

	public Expr Value { get; set; }

	public bool NeedRenewal { get; set; }

	public Gpr(int index, Expr value, bool needRenewal)
	{
		Index = index;
		Value = value;
		NeedRenewal = needRenewal;
	}
}
