using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class GNNEMac : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEFusion("gnne_fusion", "call", (GNNEFusion _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input"));


	public Expr? GetReplace(GNNEFusion gnne_fusion, Call call, Expr input)
	{
		return input;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEFusion gnne_fusion = (GNNEFusion)__result["gnne_fusion"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(gnne_fusion, call, input);
	}
}
