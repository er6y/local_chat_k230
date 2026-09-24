namespace Nncase.Passes.Rules.K230;

public class GnneActionExtraw : GnneAction
{
	public int Extrd { get; }

	public Gpr Value { get; }

	public int Rs { get; }

	public int Imm { get; }

	public GnneActionExtraw(int extrd, Gpr value, int rs, int imm)
		: base(GnneActionName.Extraw)
	{
		Extrd = extrd;
		Value = value;
		Rs = rs;
		Imm = imm;
	}
}
