using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class PReluToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsPRelu("prelu", "preluCall", (PRelu _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("slope"));


	public Expr? GetReplace(PRelu prelu, Call preluCall, TensorConst slope, Expr input)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(preluCall.CheckedShape.ToValueArray(), 0, array, array.Length - preluCall.CheckedShape.Rank, preluCall.CheckedShape.Rank);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num, new QuantParam(0, 1f));
		float[] array2 = slope.Value.ToArray<float>();
		for (int i = 0; i < actParam.Ks.GetLength(1); i++)
		{
			if (array2.Length == 1)
			{
				actParam.Ks[0, i] = array2[0];
			}
			else if (array2.Length == num)
			{
				actParam.Ks[0, i] = array2[i];
			}
			else
			{
				actParam.Ks[0, i] = array2[i * array2.Length / num];
			}
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, preluCall.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		PRelu prelu = (PRelu)__result["prelu"];
		Call preluCall = (Call)__result["preluCall"];
		TensorConst slope = (TensorConst)__result["slope"];
		Expr input = (Expr)__result["input"];
		return GetReplace(prelu, preluCall, slope, input);
	}
}
