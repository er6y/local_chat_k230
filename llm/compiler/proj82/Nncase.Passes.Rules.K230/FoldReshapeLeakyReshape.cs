using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Math;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldReshapeLeakyReshape : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsReshape("bc2", (Reshape _) => true, Nncase.PatternMatch.F.Math.IsBinary("bin2", BinaryOp.Max, Nncase.PatternMatch.F.Math.IsBinary("bin1", BinaryOp.Mul, Nncase.PatternMatch.F.Tensors.IsReshape("bc1", (Reshape _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("rhs")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	})));


	public Expr? GetReplace(Reshape bc2, Binary bin2, Binary bin1, Reshape bc1, Expr input, Expr rhs)
	{
		Call rhs2 = Nncase.IR.F.Math.Binary(bin1.BinaryOp, input, rhs);
		return Nncase.IR.F.Math.Binary(bin2.BinaryOp, bin1, rhs2);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Reshape bc = (Reshape)__result["bc2"];
		Binary bin = (Binary)__result["bin2"];
		Binary bin2 = (Binary)__result["bin1"];
		Reshape bc2 = (Reshape)__result["bc1"];
		Expr input = (Expr)__result["input"];
		Expr rhs = (Expr)__result["rhs"];
		return GetReplace(bc, bin, bin2, bc2, input, rhs);
	}
}
