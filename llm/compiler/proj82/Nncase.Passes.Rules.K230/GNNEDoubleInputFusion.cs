using Nncase.IR;
using Nncase.IR.K230;
using Nncase.Passes.Rules.Neutral;

namespace Nncase.Passes.Rules.K230;

public class GNNEDoubleInputFusion<T> : DoubleInputFusion<T, GNNELoad, GNNEStore> where T : Op
{
	public override string ModuleKind { get; } = "k230";

}
