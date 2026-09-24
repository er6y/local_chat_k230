using Nncase.Passes.Mutators;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNEMultiInputFusionMergeRule : MultiInputFusionMergeRule
{
	public override string ModuleKind => "k230";
}
