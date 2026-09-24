using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class ResizeFusion : GNNESingleInputFusion<Ai2dResize>
{
	public override string Name { get; } = "TileResizeCase";

}
