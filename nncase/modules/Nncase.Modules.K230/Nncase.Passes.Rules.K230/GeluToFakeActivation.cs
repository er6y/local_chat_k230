using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class GeluToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = NN.IsGelu("gelu", "call", (Gelu _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("alpha"));


	public Expr? GetReplace(Gelu gelu, Call call, Expr input, Tensor<float> alpha)
	{
		if (System.Math.Abs((double)alpha[new int[1]] - 0.5773502588272095) < 1E-06)
		{
			int num = 1;
			float[] array = new float[15]
			{
				-5.7f, -4f, -3.2f, -1.75f, -1.5f, -1f, -0.75f, -0.41f, 0f, 0.45f,
				0.65f, 1.15f, 2f, 3.8f, 5f
			};
			double[] array2 = new double[32]
			{
				-0.002292839261282875, -0.014662115375225326, -0.012083319172583762, -0.06762279719310094, -0.04355115475127236, -0.1968532719071754, -0.07038844813525835, -0.2835633561715456, -0.04080581730250121, -0.22941641246785216,
				-0.004866290168452858, -0.16218870718999323, 0.06978228128368957, -0.09344377557716932, 0.1355952296136711, -0.04373907923809428, 0.2293206404535466, -0.004927545580047488, 0.34145955677305617, -0.0030660283609182937,
				0.42404171597102036, -0.03458965365914235, 0.5019084167113341, -0.08683216895716561, 0.6057840597436063, -0.20679235778811544, 0.6424345064997299, -0.2705260541574357, 0.5986371536428863, -0.10914053277360192,
				0.5819093157146709, -0.027774986180709504
			};
			ActParam16 actParam = new ActParam16(num, new QuantParam(0, 1f));
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
			return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, true, GnneActivationType.Add, actParam, call.CheckedShape.ToValueArray());
		}
		if ((double)System.Math.Abs(alpha[new int[1]] - 1f) < 1E-06)
		{
			int num2 = 1;
			float[] array3 = new float[15]
			{
				-3.273f, -2.24f, -1.381f, -0.789f, -0.508f, -0.306f, -0.172f, 0f, 0.172f, 0.306f,
				0.508f, 0.789f, 1.381f, 2.24f, 3.273f
			};
			float[] array4 = new float[16]
			{
				0f, -0.024013134f, -0.10315602f, -0.09541199f, 0.05015764f, 0.1931251f, 0.31305435f, 0.43165174f, 0.5683482f, 0.6869456f,
				0.80687493f, 0.94984233f, 1.095412f, 1.103156f, 1.0240132f, 1f
			};
			float[] array5 = new float[16]
			{
				0f, -0.07650964f, -0.25319445f, -0.25159308f, -0.13332045f, -0.05944821f, -0.021542944f, -0.0019543152f, -0.0019543152f, -0.021542944f,
				-0.05944821f, -0.13332045f, -0.25159308f, -0.25319445f, -0.07650964f, 0f
			};
			ActParam16 actParam2 = new ActParam16(num2, new QuantParam(0, 1f));
			for (int k = 0; k < 15; k++)
			{
				actParam2.Xs[k, 0] = array3[k];
			}
			for (int l = 0; l < 16; l++)
			{
				actParam2.Ks[l, 0] = array4[l];
				actParam2.Bs[l, 0] = array5[l];
			}
			Tensor<float> tensor2 = new Tensor<float>(actParam2.GetAct1Data, new int[2] { num2, 49 });
			return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor2, num2, 0, 0, 0, true, GnneActivationType.Add, actParam2, call.CheckedShape.ToValueArray());
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Gelu gelu = (Gelu)__result["gelu"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		Tensor<float> alpha = ((TensorConst)__result["alpha"]).Value.Cast<float>();
		return GetReplace(gelu, call, input, alpha);
	}
}
