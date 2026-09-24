using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

public sealed class PadFusion : GNNESingleInputFusion<GNNEPad>
{
	public override string Name { get; } = "TilePadCase";

}
