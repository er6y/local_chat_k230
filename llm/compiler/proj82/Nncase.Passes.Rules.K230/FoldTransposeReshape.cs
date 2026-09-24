using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldTransposeReshape : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsTranspose("tp", "call", (Transpose _) => true, Nncase.PatternMatch.F.Tensors.IsReshape("bc", (Reshape _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("perm"));


	private Expr? GetReplace(Transpose tp, Call call, Reshape bc, TensorConst input, TensorConst perm)
	{
		if (perm != new int[4] { 0, 2, 3, 1 } || bc.CheckedShape[0] != input.CheckedShape[0] || bc.CheckedShape[1] != input.CheckedShape[2] || bc.CheckedShape[2] != input.CheckedShape[1] || bc.CheckedShape[3] != input.CheckedShape[3])
		{
			return null;
		}
		return Nncase.IR.F.Tensors.Transpose(input, new int[4] { 0, 3, 2, 1 });
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Transpose tp = (Transpose)__result["tp"];
		Call call = (Call)__result["call"];
		Reshape bc = (Reshape)__result["bc"];
		TensorConst input = (TensorConst)__result["input"];
		TensorConst perm = (TensorConst)__result["perm"];
		return GetReplace(tp, call, bc, input, perm);
	}
}
