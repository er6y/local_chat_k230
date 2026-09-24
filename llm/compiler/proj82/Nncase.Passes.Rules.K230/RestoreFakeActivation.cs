using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class RestoreFakeActivation : GNNEDIFQuantRule
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeActivation("fakeActivation", "call", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsTensorConst("inputARange")), Nncase.PatternMatch.Utility.IsRangeOfMarker(Nncase.PatternMatch.Utility.IsWildcard("inputB"), Nncase.PatternMatch.Utility.IsTensorConst("inputBRange")), Nncase.PatternMatch.Utility.IsWildcard("act"), Nncase.PatternMatch.Utility.IsWildcard("outChannels"), Nncase.PatternMatch.Utility.IsWildcard("inAShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("inBShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("outShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("is16Segment"));


	public Expr? GetReplace(FakeActivation fakeActivation, Call call, Expr inputA, Tensor<float> inputARange, Expr inputB, Tensor<float> inputBRange, Expr act, Expr outChannels, Expr inAShiftbits, Expr inBShiftbits, Expr outShiftbits, Expr is16Segment)
	{
		return Nncase.IR.F.Math.Binary((fakeActivation.Type != 0) ? BinaryOp.Mul : BinaryOp.Add, inputA, inputB);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeActivation fakeActivation = (FakeActivation)__result["fakeActivation"];
		Call call = (Call)__result["call"];
		Expr inputA = (Expr)__result["inputA"];
		Tensor<float> inputARange = ((TensorConst)__result["inputARange"]).Value.Cast<float>();
		Expr inputB = (Expr)__result["inputB"];
		Tensor<float> inputBRange = ((TensorConst)__result["inputBRange"]).Value.Cast<float>();
		Expr act = (Expr)__result["act"];
		Expr outChannels = (Expr)__result["outChannels"];
		Expr inAShiftbits = (Expr)__result["inAShiftbits"];
		Expr inBShiftbits = (Expr)__result["inBShiftbits"];
		Expr outShiftbits = (Expr)__result["outShiftbits"];
		Expr is16Segment = (Expr)__result["is16Segment"];
		base.Option = __context;
		base.MatchResult = __result;
		Init();
		return GetReplace(fakeActivation, call, inputA, inputARange, inputB, inputBRange, act, outChannels, inAShiftbits, inBShiftbits, outShiftbits, is16Segment);
	}
}
