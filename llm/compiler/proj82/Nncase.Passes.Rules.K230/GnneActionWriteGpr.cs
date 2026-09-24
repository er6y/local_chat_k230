namespace Nncase.Passes.Rules.K230;

public class GnneActionWriteGpr : GnneAction
{
	private int _gprIndex;

	public int GprIndex => _gprIndex;

	public int Imm { get; }

	public GnneActionWriteGpr(int gprIndex, int imm)
		: base(GnneActionName.WriteGpr)
	{
		_gprIndex = gprIndex;
		Imm = imm;
	}
}
