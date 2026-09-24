using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class TanhToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; }

	public Expr? GetReplace(Unary u, Call call, Expr input)
	{
		int num = 1;
		ActParam16 actParam = new ActParam16(num);
		double[] array = new double[15]
		{
			-5.0, -4.28515625, -3.572265625, -2.857421875, -2.142578125, -1.4287109375, -0.71435546875, 0.0, 0.71435546875, 1.4287109375,
			2.142578125, 2.857421875, 3.572265625, 4.28515625, 5.0
		};
		double[] array2 = new double[32]
		{
			0.0,
			-1.0,
			0.0004031658172607422,
			-511.0 / 512.0,
			0.0016813278198242188,
			-127.0 / 128.0,
			0.006992340087890625,
			-0.9736328125,
			0.02880859375,
			-0.9111328125,
			0.11407470703125,
			-373.0 / 512.0,
			0.38916015625,
			-0.33544921875,
			0.85888671875,
			0.0,
			0.85888671875,
			0.0,
			0.38916015625,
			0.33544921875,
			0.11407470703125,
			373.0 / 512.0,
			0.02880859375,
			0.9111328125,
			0.006992340087890625,
			0.9736328125,
			0.0016813278198242188,
			127.0 / 128.0,
			0.0004031658172607422,
			511.0 / 512.0,
			0.0,
			1.0
		};
		for (int i = 0; i < 15; i++)
		{
			actParam.Xs[i, 0] = (float)array[i];
		}
		for (int j = 0; j < 16; j++)
		{
			actParam.Ks[j, 0] = (float)array2[j * 2];
			actParam.Bs[j, 0] = (float)array2[j * 2 + 1];
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 49 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, true, GnneActivationType.Add, actParam, call.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Unary u = (Unary)__result["u"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(u, call, input);
	}

	public TanhToFakeActivation()
	{
		Func<Unary, bool> condition = (Unary u) => u.UnaryOp == UnaryOp.Tanh;
		Pattern = Nncase.PatternMatch.F.Math.IsUnary("u", "call", condition, Nncase.PatternMatch.Utility.IsWildcard("input"))with
		{
			TypePattern = (TypePatternUtility.HasDataType(DataTypes.Float32) & TypePatternUtility.HasFixedShape())
		};
	}
}
