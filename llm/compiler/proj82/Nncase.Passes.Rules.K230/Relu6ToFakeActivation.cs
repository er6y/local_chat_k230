using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class Relu6ToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsRelu6("relu6", "relu6Call", (Relu6 _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"));


	public Expr? GetReplace(Relu6 relu6, Call relu6Call, Expr input)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(relu6Call.CheckedShape.ToValueArray(), 0, array, array.Length - relu6Call.CheckedShape.Rank, relu6Call.CheckedShape.Rank);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num, new QuantParam(0, 1f));
		for (int i = 0; i < actParam.Ks.GetLength(1); i++)
		{
			actParam.Ks[0, i] = 0f;
			actParam.Bs[0, i] = 0f;
			actParam.FusedClamp[i].Max = 6f;
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, relu6Call.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Relu6 relu = (Relu6)__result["relu6"];
		Call relu6Call = (Call)__result["relu6Call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(relu, relu6Call, input);
	}
}
