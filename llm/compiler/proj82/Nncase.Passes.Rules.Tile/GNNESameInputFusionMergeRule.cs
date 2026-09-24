using Nncase.Passes.Mutators;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNESameInputFusionMergeRule : SameInputFusionMergeRule
{
	public override string ModuleKind => "k230";
}
