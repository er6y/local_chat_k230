using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class Pdp0ReduceFusion : GNNESingleInputFusion<GNNEPdp0Reduce>
{
	public override string Name { get; } = "TilePdp0ReduceCase";

}
