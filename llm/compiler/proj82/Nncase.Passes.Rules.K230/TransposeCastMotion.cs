using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class TransposeCastMotion : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsCast("cast", "castCall", (Cast _) => true, Nncase.PatternMatch.F.Tensors.IsTranspose("tp", "tpCall", (Transpose _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasDataType(DataTypes.Boolean)
	}, Nncase.PatternMatch.Utility.IsTensorConst("perm")));


	private Expr? GetReplace(Expr input, Expr perm, Cast cast, Call castCall)
	{
		return Nncase.IR.F.Tensors.Transpose(Nncase.IR.F.Tensors.Cast(input, castCall.CheckedDataType, cast.CastMode), perm);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr input = (Expr)__result["input"];
		Expr perm = (Expr)__result["perm"];
		Cast cast = (Cast)__result["cast"];
		Call castCall = (Call)__result["castCall"];
		return GetReplace(input, perm, cast, castCall);
	}
}
