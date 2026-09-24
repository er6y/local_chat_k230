using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class Pdp1Fusion : GNNESingleInputFusion<GNNEPdp1>
{
	public override string Name { get; } = "TilePdp1Case";

}
