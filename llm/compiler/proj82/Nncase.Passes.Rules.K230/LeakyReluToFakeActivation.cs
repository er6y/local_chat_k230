using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class LeakyReluToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsLeakyRelu("leaky", "leakyCall", (LeakyRelu _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("alpha"));


	public Expr? GetReplace(LeakyRelu leaky, Call leakyCall, TensorConst alpha, Expr input)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(leakyCall.CheckedShape.ToValueArray(), 0, array, array.Length - leakyCall.CheckedShape.Rank, leakyCall.CheckedShape.Rank);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num, new QuantParam(0, 1f));
		float num2 = alpha.Value.ToScalar<float>();
		for (int i = 0; i < actParam.Ks.GetLength(1); i++)
		{
			actParam.Ks[0, i] = num2;
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, leakyCall.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		LeakyRelu leaky = (LeakyRelu)__result["leaky"];
		Call leakyCall = (Call)__result["leakyCall"];
		TensorConst alpha = (TensorConst)__result["alpha"];
		Expr input = (Expr)__result["input"];
		return GetReplace(leaky, leakyCall, alpha, input);
	}
}
