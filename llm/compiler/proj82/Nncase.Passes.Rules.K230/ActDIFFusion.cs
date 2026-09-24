using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class ActDIFFusion : GNNEDoubleInputFusion<GNNEActivation>
{
	public override string Name { get; } = "TileAct1Case";

}
