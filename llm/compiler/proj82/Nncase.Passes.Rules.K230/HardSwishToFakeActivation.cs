using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class HardSwishToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsHardSwish("hardSwish", "call", (HardSwish _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	});


	private Expr? GetReplace(Call call, Expr input)
	{
		int num = 1;
		ActParam16 actParam = new ActParam16(num);
		float[] array = new float[15]
		{
			-3f, -2.57f, -2.14f, -1.71f, -1.29f, -0.86f, -0.43f, 0f, 0.43f, 0.86f,
			1.29f, 1.71f, 2.14f, 2.57f, 3f
		};
		double[] array2 = new double[16]
		{
			0.0, -0.428333333333333, -0.285, -0.141666666666666, 2.611728587599745E-16, 0.141666666666667, 0.285, 0.428333333333333, 0.571666666666667, 0.715,
			0.858333333333334, 0.999999999999999, 1.141666666666667, 1.284999999999999, 1.428333333333333, 1.0
		};
		double[] array3 = new double[16]
		{
			0.0, -1.290016666666665, -0.92165, -0.614916666666666, -0.372433333333333, -0.189916666666667, -0.06665, -0.005016666666667, -0.005016666666667, -0.06665,
			-0.189916666666667, -0.372433333333331, -0.614916666666667, -0.921649999999997, -1.290016666666666, 0.0
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
