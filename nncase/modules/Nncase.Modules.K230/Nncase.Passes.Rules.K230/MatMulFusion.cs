using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class MatMulFusion : GNNEDoubleInputFusion<GNNEMatMul>
{
	public override string Name { get; } = "TileMatMulCase";

}
