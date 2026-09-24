using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class ConvTransposeFusion : GNNESingleInputFusion<GNNEConv2DTranspose>
{
	public override string Name { get; } = "TileConv2dTransposeCase";

}
