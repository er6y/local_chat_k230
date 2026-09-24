using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldLoadStore : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEStore((GNNEStore _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad((GNNELoad _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")), Nncase.PatternMatch.Utility.IsTensorConst());


	public Expr? GetReplace(Expr input)
	{
		return input;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr input = (Expr)__result["input"];
		return GetReplace(input);
	}
}
