using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class SigmoidToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsSigmoid("Sigmoid", "call", (Sigmoid _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
	});


	private Expr? GetReplace(Call call, Expr input)
	{
		int num = 1;
		ActParam16 actParam = new ActParam16(num);
		float[] array = new float[15]
		{
			-7f, -4.5f, -3.5f, -2.7f, -2.1f, -1.6f, -1f, 0f, 1f, 1.6f,
			2.1f, 2.7f, 3.5f, 4.5f, 7f
		};
		double[] array2 = new double[32]
		{
			0.0005523135475095087, 0.004717946782565874, 0.003582545941984816, 0.024643784893781717, 0.017628076952972527, 0.08920121475552378, 0.04080084671519202, 0.17057512199171598, 0.07504241042393778, 0.2641958959068108,
			0.11550039574401527, 0.35039917518496155, 0.16589656476120918, 0.4312276071962273, 0.23123362875892262, 0.4955178069668943, 0.23398496370676491, 0.5031840614393268, 0.17066333504274322, 0.5625762717242175,
			0.1197647477013204, 0.6417049229131112, 0.07822321089963047, 0.7281582013897308, 0.04270052410967129, 0.8235195920244173, 0.01849619693381177, 0.9073131477622514, 0.0037644096557241102, 0.9742926548134362,
			0.000567715948649905, 0.9951637114353361
		};
		for (int i = 0; i < 15; i++)
		{
			actParam.Xs[i, 0] = array[i];
		}
		for (int j = 0; j < 16; j++)
		{
			actParam.Ks[j, 0] = (float)array2[j * 2];
			actParam.Bs[j, 0] = (float)array2[j * 2 + 1];
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 49 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, true, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(call, input);
	}
}
