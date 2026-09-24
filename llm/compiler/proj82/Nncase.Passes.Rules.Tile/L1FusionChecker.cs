using System;
using Nncase.IR;
using Nncase.Passes.Rules.K230;
using Nncase.PatternMatch;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

internal sealed class L1FusionChecker : IFusionChecker
{
	private Fusion _fusion;

	private RunPassContext _passOptions;

	public bool Check(Fusion fusion, RunPassContext passOptions)
	{
		_fusion = fusion;
		return new FusionConvertVisitor(passOptions, new TileOptions(Array.Empty<int>())).TryL1Fuse(fusion);
	}

	public PrimFunction Convert()
	{
		IRewriteRule rewriteRule = new TileConv2D();
		CompilerServices.TryMatchRoot(_fusion, rewriteRule.Pattern, out IMatchResult result);
		return (PrimFunction)rewriteRule.GetReplace(result, _passOptions);
	}
}
