namespace Nncase.Passes.Rules.K230;

public class GnneActionPackShapeReg : GnneAction
{
	public Gpr N { get; }

	public Gpr C { get; }

	public Gpr H { get; }

	public Gpr W { get; }

	public Ssr Ss { get; }

	public GnneActionPackShapeReg(Gpr n, Gpr c, Gpr h, Gpr w, Ssr ss)
		: base(GnneActionName.PackShapeReg)
	{
		N = n;
		C = c;
		H = h;
		W = w;
		Ss = ss;
	}
}
