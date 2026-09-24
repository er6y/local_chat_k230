using System.Linq;
using Nncase.IR;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToFakeDynamicMatmul : IRewriteRule
{
	public IPattern Pattern { get; } = Math.IsMatMul(null, "oldMatmul", Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsWildcard("inputB"))with
	{
		TypePattern = TypePatternUtility.HasShape((Shape shape) => !shape.IsFixed, "HasDynamicShape")
	};


	private bool AnyUnknow(Shape shape, int start, int end)
	{
		Shape shape2 = shape;
		return Enumerable.Range(0, (end < 0) ? (shape2.Count + end) : end).Any((int i) => shape2[i].IsUnknown);
	}

	private Expr? GetReplace(Call oldMatmul, Expr inputA, Expr inputB)
	{
		Shape checkedShape = inputA.CheckedShape;
		_ = inputB.CheckedShape;
		Expr act = new ActivationParameter<float>((from d in checkedShape.SkipLast(1)
			select (!d.IsFixed) ? 1 : d.FixedValue).ToArray(), ValueRange<float>.Full).ToAct0Data<float>();
		Shape checkedShape2 = inputA.CheckedShape;
		return Nncase.IR.K230.F.Tensors.FakeDynamicGNNEMatMul(inputA, inputB, act, checkedShape2[checkedShape2.Count - 2].IsUnknown);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call oldMatmul = (Call)__result["oldMatmul"];
		Expr inputA = (Expr)__result["inputA"];
		Expr inputB = (Expr)__result["inputB"];
		return GetReplace(oldMatmul, inputA, inputB);
	}
}
