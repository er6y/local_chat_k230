using Nncase.IR;
using Nncase.IR.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class StackToConcat : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsStack("stack", "stackCall", Nncase.PatternMatch.Utility.IsTuple("tuple"), Nncase.PatternMatch.Utility.IsTensorConst("axis"));


	private Expr? GetReplace(Expr stack, Call stackCall, Expr tuple, int axis)
	{
		if (axis != 0 || stackCall.CheckedShape.Rank <= 2)
		{
			return null;
		}
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.F.Tensors.Concat(tuple, axis), stackCall.CheckedShape);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr stack = (Expr)__result["stack"];
		Call stackCall = (Call)__result["stackCall"];
		Expr tuple = (Expr)__result["tuple"];
		int axis = ((TensorConst)__result["axis"]).Value.ToScalar<int>();
		return GetReplace(stack, stackCall, tuple, axis);
	}
}
