using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class FoldMaxAndFakeActivation : RewriteRule<Pattern>, IRewriteRule
{
	public override Pattern Pattern { get; } = Math.IsBinary("binary", "binaryCall", (Binary _) => true, Nncase.PatternMatch.F.K230.IsFakeActivation("fakeActivation", "call", (FakeActivation _) => true, Nncase.PatternMatch.Utility.IsWildcard("inputA"), Nncase.PatternMatch.Utility.IsWildcard("inputB"), Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsTensorConst("outChannels"), Nncase.PatternMatch.Utility.IsWildcard("inAShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("inBShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("outShiftbits"), Nncase.PatternMatch.Utility.IsWildcard("is16Segment")), Nncase.PatternMatch.Utility.IsTensorConst());


	public Expr? GetReplace(FakeActivation fakeActivation, Call call, Call binaryCall, Expr inputA, Expr inputB, TensorConst act, TensorConst outChannels, Expr inAShiftbits, Expr inBShiftbits, Expr outShiftbits, Expr is16Segment, Expr binary, RunPassContext context)
	{
		if (context.Driver is DataflowPass && context.GetAnalysis<IExprUserAnalysisResult>()[binary].Count() > 1)
		{
			return null;
		}
		int num = outChannels.Value.ToScalar<int>();
		ActParam2 actParam = new ActParam2(num);
		for (int i = 0; i < num; i++)
		{
			if (act.Value.Cast<int>()[new int[3] { 1, 0, i }] == 1)
			{
				if (act.Value.Cast<int>()[new int[3] { 2, 0, i }] < 0)
				{
					actParam.Ks[0, i] = 1f;
					actParam.Ks[1, i] = 1f;
					actParam.Bs[0, i] = 0f;
					actParam.Bs[1, i] = 0f;
				}
			}
			else
			{
				actParam.Xs[0, i] = actParam.Bs[0, i] / (1f - actParam.Ks[0, i]);
				if (actParam.Ks[0, i] > 1f)
				{
					actParam.Ks[0, i] = 1f;
					actParam.Bs[0, i] = 0f;
				}
				else
				{
					actParam.Ks[1, i] = 1f;
					actParam.Bs[1, i] = 0f;
				}
			}
		}
		act = new TensorConst(new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 }));
		return Nncase.IR.K230.F.Tensors.FakeActivation(inputA, inputB, act, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray()).InheritMetaData(binaryCall);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		FakeActivation fakeActivation = (FakeActivation)__result["fakeActivation"];
		Call call = (Call)__result["call"];
		Call binaryCall = (Call)__result["binaryCall"];
		Expr inputA = (Expr)__result["inputA"];
		Expr inputB = (Expr)__result["inputB"];
		TensorConst act = (TensorConst)__result["act"];
		TensorConst outChannels = (TensorConst)__result["outChannels"];
		Expr inAShiftbits = (Expr)__result["inAShiftbits"];
		Expr inBShiftbits = (Expr)__result["inBShiftbits"];
		Expr outShiftbits = (Expr)__result["outShiftbits"];
		Expr is16Segment = (Expr)__result["is16Segment"];
		Expr binary = (Expr)__result["binary"];
		return GetReplace(fakeActivation, call, binaryCall, inputA, inputB, act, outChannels, inAShiftbits, inBShiftbits, outShiftbits, is16Segment, binary, __context);
	}
}
