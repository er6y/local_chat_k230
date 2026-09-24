using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class TransposeFusion : GNNESingleInputFusion<GNNETranspose>
{
	public override string Name { get; } = "TileTransposeCase";

}
