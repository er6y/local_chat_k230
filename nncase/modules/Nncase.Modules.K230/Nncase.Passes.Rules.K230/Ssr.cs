namespace Nncase.Passes.Rules.K230;

public struct Ssr
{
	public int Index { get; set; }

	public long Value { get; set; }

	public bool NeedRenewal { get; set; }

	public Ssr(int index, long value, bool needRenewal)
	{
		Index = index;
		Value = value;
		NeedRenewal = needRenewal;
	}
}
