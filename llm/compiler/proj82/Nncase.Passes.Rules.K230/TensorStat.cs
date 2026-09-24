namespace Nncase.Passes.Rules.K230;

public class TensorStat
{
	public bool IsFirstSlice { get; set; }

	public bool IsLastSlice { get; set; }

	public int SliceIdx { get; set; }

	public TensorStat(bool isFirstSlice, bool isLastSlice, int sliceIdx = 0)
	{
		IsFirstSlice = isFirstSlice;
		IsLastSlice = isLastSlice;
		SliceIdx = sliceIdx;
	}

	public int Stat_cnt()
	{
		int num = 0;
		if (IsFirstSlice)
		{
			num++;
		}
		if (IsLastSlice)
		{
			num++;
		}
		if (IsFirstSlice && IsLastSlice)
		{
			num = 1;
		}
		return num;
	}
}
