using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class HardSigmoidToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsHardSigmoid("hardSigmoid", "call", (HardSigmoid _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsWildcard("alpha"), Nncase.PatternMatch.Utility.IsWildcard("beta"));


	private Expr? GetReplace(Call call, Expr input, float alpha, float beta)
	{
		int[] array = input.CheckedShape.ToValueArray();
		int[] array2 = new int[4] { 1, 1, 1, 1 };
		Array.Copy(array, 0, array2, array2.Length - array.Length, array.Length);
		int num = array2[1];
		ActParam2 actParam = new ActParam2(num);
		actParam.ForEachChannel(delegate(ActParam2 param, int i)
		{
			param.Ks[0, i] = alpha;
			param.Bs[0, i] = beta;
			param.Xs[0, i] = 0f;
			param.Ks[1, i] = alpha;
			param.Bs[1, i] = beta;
		});
		actParam.SetFusedClamp(new ValueRange<float>(0f, 1f));
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		float alpha = ((TensorConst)__result["alpha"]).Value.ToScalar<float>();
		float beta = ((TensorConst)__result["beta"]).Value.ToScalar<float>();
		return GetReplace(call, input, alpha, beta);
	}
}
