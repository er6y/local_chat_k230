using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class BatchNormToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsBatchNormalization("bn", "bnCall", (BatchNormalization _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasRank(4)
	}, Nncase.PatternMatch.Utility.IsTensorConst("scale"), Nncase.PatternMatch.Utility.IsTensorConst("bias"), Nncase.PatternMatch.Utility.IsTensorConst("mean"), Nncase.PatternMatch.Utility.IsTensorConst("var"), Nncase.PatternMatch.Utility.IsTensorConst("eps"));


	public Expr? GetReplace(BatchNormalization bn, Call bnCall, Expr input, float[] scale, float[] bias, float[] mean, float[] var, float eps)
	{
		int fixedValue = bnCall.CheckedShape[1].FixedValue;
		ActParam2 actParam = new ActParam2(fixedValue, new QuantParam(0, 1f));
		for (int i = 0; i < actParam.Ks.GetLength(1); i++)
		{
			actParam.Ks[0, i] = (float)((double)scale[i] / System.Math.Sqrt(var[i] + eps));
			actParam.Bs[0, i] = (float)((double)bias[i] - (double)(scale[i] * mean[i]) / System.Math.Sqrt(var[i] + eps));
			actParam.Ks[1, i] = (float)((double)scale[i] / System.Math.Sqrt(var[i] + eps));
			actParam.Bs[1, i] = (float)((double)bias[i] - (double)(scale[i] * mean[i]) / System.Math.Sqrt(var[i] + eps));
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { fixedValue, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, fixedValue, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, bnCall.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		BatchNormalization bn = (BatchNormalization)__result["bn"];
		Call bnCall = (Call)__result["bnCall"];
		Expr input = (Expr)__result["input"];
		float[] scale = ((TensorConst)__result["scale"]).Value.ToArray<float>();
		float[] bias = ((TensorConst)__result["bias"]).Value.ToArray<float>();
		float[] mean = ((TensorConst)__result["mean"]).Value.ToArray<float>();
		float[] var = ((TensorConst)__result["var"]).Value.ToArray<float>();
		float eps = ((TensorConst)__result["eps"]).Value.ToScalar<float>();
		return GetReplace(bn, bnCall, input, scale, bias, mean, var, eps);
	}
}
