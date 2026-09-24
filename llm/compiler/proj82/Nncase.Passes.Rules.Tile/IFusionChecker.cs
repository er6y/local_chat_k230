using Nncase.IR;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

internal interface IFusionChecker
{
	bool Check(Fusion fusion, RunPassContext passOptions);

	PrimFunction Convert();
}
