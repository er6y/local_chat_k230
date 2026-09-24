using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class GnneActionPuCompute : GnneAction
{
	public int TcuId { get; }

	public PU_OF_SHIFT_MODE Mode { get; }

	public GnneActionPuCompute(int tcuId, PU_OF_SHIFT_MODE mode)
		: base(GnneActionName.PuCompute)
	{
		TcuId = tcuId;
		Mode = mode;
	}
}
