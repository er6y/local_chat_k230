using Nncase.IR;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ToFakeAi2dPad : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsPad("pad", "call", (Pad p) => p.PadMode != PadMode.Constant, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsWildcard("padding"), Nncase.PatternMatch.Utility.IsWildcard("value"));


	private Expr? GetReplace(Pad pad, Expr input, Expr padding, float value)
	{
		return Nncase.IR.K230.F.Tensors.FakeAi2dPad(input, padding, value, pad.PadMode);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Pad pad = (Pad)__result["pad"];
		Expr input = (Expr)__result["input"];
		Expr padding = (Expr)__result["padding"];
		float value = ((TensorConst)__result["value"]).Value.ToScalar<float>();
		return GetReplace(pad, input, padding, value);
	}
}
