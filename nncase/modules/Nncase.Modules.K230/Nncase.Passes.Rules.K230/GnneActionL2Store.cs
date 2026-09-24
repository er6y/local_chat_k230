using Nncase.IR;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

public class GnneActionL2Store : GnneAction
{
	public Gpr Basement { get; }

	public Gpr AddrS { get; }

	public Gpr AddrD { get; }

	public Ssr Shape { get; }

	public Call Output { get; }

	public SegmentND SliceInfo { get; }

	public Buffer Buffer { get; }

	public int[] Layout { get; }

	public GnneActionL2Store(Gpr basement, Gpr addrS, Gpr addrD, Ssr shape, Call output, SegmentND sliceInfo, Buffer buffer, int[] layout = null)
		: base(GnneActionName.L2Store)
	{
		Basement = basement;
		AddrS = addrS;
		AddrD = addrD;
		Shape = shape;
		Output = output;
		SliceInfo = sliceInfo;
		Buffer = buffer;
		Layout = layout;
	}
}
