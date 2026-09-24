namespace Nncase.Passes.Rules.K230;

public class GnneAction
{
	public GnneActionName Name { get; }

	public GnneAction(GnneActionName name)
	{
		Name = name;
	}

	public int ToGlbAddr(int mmuItem, int addr)
	{
		return (mmuItem << 28) + addr;
	}
}
