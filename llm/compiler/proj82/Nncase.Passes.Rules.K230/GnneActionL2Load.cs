using Nncase.IR;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

public class GnneActionL2Load : GnneAction
{
	private Ssr _shape;

	public Gpr Basement { get; }

	public Gpr AddrD { get; }

	public Gpr AddrS { get; }

	public Ssr Shape => _shape;

	public Call Input { get; }

	public SegmentND SliceInfo { get; }

	public Buffer Buffer { get; }

	public int[] Layout { get; }

	public GnneActionL2Load(Gpr basement, Gpr addrD, Gpr addrS, Ssr shape, Call input, SegmentND sliceInfo, Buffer buffer, int[] layout = null)
		: base(GnneActionName.L2Load)
	{
		Basement = basement;
		AddrD = addrD;
		AddrS = addrS;
		_shape = shape;
		Input = input;
		SliceInfo = sliceInfo;
		Buffer = buffer;
		Layout = layout;
	}
}
