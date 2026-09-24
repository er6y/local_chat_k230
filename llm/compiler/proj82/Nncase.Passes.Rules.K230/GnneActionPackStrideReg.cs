using Nncase.IR;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPackStrideReg : GnneAction
{
	private Gpr _n;

	private Gpr _c;

	private Gpr _h;

	private Ssr _ss;

	private Op _conn;

	public Gpr N => _n;

	public Gpr C => _c;

	public Gpr H => _h;

	public Ssr Ss => _ss;

	public Op Conn => _conn;

	public GnneActionPackStrideReg(Gpr n, Gpr c, Gpr h, Ssr ss, Op conn = null)
		: base(GnneActionName.PackStrideReg)
	{
		_n = n;
		_c = c;
		_h = h;
		_ss = ss;
		_conn = conn;
	}
}
