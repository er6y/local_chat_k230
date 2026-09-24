namespace Nncase.Passes.Rules.K230;

public class GnneActionExtrw : GnneAction
{
	public int Extrd { get; }

	public Gpr S_value { get; }

	public int Rs { get; }

	public int Imm { get; }

	public GnneActionExtrw(int extrd, Gpr value, int rs, int imm)
		: base(GnneActionName.Extrw)
	{
		Extrd = extrd;
		S_value = value;
		Rs = rs;
		Imm = imm;
	}
}
