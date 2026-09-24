using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class ConvFusion : GNNESingleInputFusion<GNNEConv2D>
{
	public override string Name { get; } = "TileConv2dCase";

}
