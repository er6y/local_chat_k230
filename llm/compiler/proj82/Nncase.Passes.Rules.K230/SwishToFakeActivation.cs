using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class SwishToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsSwish("Swish", "call", (Swish _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	});


	private Expr? GetReplace(Call call, Expr input)
	{
		int num = 1;
		ActParam16 actParam = new ActParam16(num);
		float[] array = new float[16]
		{
			-6.8f, -4f, -2.2f, -1.6f, -1.2f, -0.8f, -0.4f, 0f, 0.4f, 0.8f,
			1.2f, 1.6f, 2.2f, 4f, 6.8f, 0f
		};
		double[] array2 = new double[16]
		{
			0.0, -0.021684465098961, -0.083146935094056, -0.083239373463281, -0.02318165804185, 0.073618083374296, 0.218122950353851, 0.401072247994053, 0.598927752005947, 0.781877049646149,
			0.926381916625704, 1.023181658041854, 1.083239373463282, 1.083146935094056, 1.021684465098961, 1.0
		};
		double[] array3 = new double[16]
		{
			0.0, -0.146472330255445, -0.395702832349094, -0.404303627166028, -0.308112929911211, -0.193207045153763, -0.078813366995298, -0.006394948413948, -0.006394948413948, -0.078813366995297,
			-0.193207045153763, -0.308112929911212, -0.404303627166028, -0.395702832349094, -0.146472330255446, 0.0
		};
		for (int i = 0; i < 15; i++)
		{
			actParam.Xs[i, 0] = array[i];
		}
		for (int j = 0; j < 16; j++)
		{
			actParam.Bs[j, 0] = (float)array3[j];
			actParam.Ks[j, 0] = (float)array2[j];
		}
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 49 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, true, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray()).InheritMetaData(call);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(call, input);
	}
}
