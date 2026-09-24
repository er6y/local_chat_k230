namespace Nncase.Passes.Rules.K230;

public class GnneActionBasementG2R : GnneAction
{
	private int _rbasement;

	public int Rbasement => _rbasement;

	public GnneActionBasementG2R(int rbasement)
		: base(GnneActionName.BasementG2R)
	{
		_rbasement = rbasement;
	}
}
