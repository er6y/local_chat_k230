namespace Nncase.Passes.Rules.K230;

public class MmuItem
{
	private readonly int _id;

	private readonly int _startBank;

	private readonly int _width;

	private readonly int _startDepth;

	private readonly int _depth;

	public int Id => _id;

	public int StartBank => _startBank;

	public int Width => _width;

	public int StartDepth => _startDepth;

	public int Depth => _depth;

	public MmuItem()
	{
		_id = 0;
		_startBank = 0;
		_width = 0;
		_startDepth = 0;
		_depth = 0;
	}

	public MmuItem(int id, int startBank, int width, int startDepth, int depth)
	{
		_id = id;
		_startBank = startBank;
		_width = width;
		_startDepth = startDepth;
		_depth = depth;
	}
}
