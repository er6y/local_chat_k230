using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.NN;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReshapePadReshapeMotion : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsReshape("bc2", "call", (Reshape _) => true, Nncase.PatternMatch.F.NN.IsPad("p", (Pad _) => true, Nncase.PatternMatch.F.Tensors.IsReshape("bc1", (Reshape _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("pads"), Nncase.PatternMatch.Utility.IsTensorConst("value")));


	public Expr? GetReplace(Reshape bc2, Call call, Pad p, Reshape bc1, TensorConst pads, TensorConst value, TensorConst input)
	{
		if (call.CheckedShape.Count != 4 || p.CheckedShape.Count != 3 || call.CheckedShape[1] != p.CheckedShape[0] || call.CheckedShape[2] != p.CheckedShape[1] || call.CheckedShape[3] != p.CheckedShape[2])
		{
			return null;
		}
		Shape checkedShape = call.CheckedShape;
		Call input2 = Nncase.IR.F.Tensors.Reshape(bc1, new Dimension[4]
		{
			1,
			checkedShape[0],
			checkedShape[1],
			checkedShape[2]
		});
		Tensor<int> tensor = pads.Value.Cast<int>();
		int[][] array = new int[4][];
		for (int i = 0; i < 4; i++)
		{
			if (i != 0)
			{
				for (int j = 0; j < tensor.Length; j++)
				{
					array[i][j] = tensor[new int[2]
					{
						i - 1,
						j
					}];
				}
			}
		}
		return Nncase.IR.F.NN.Pad(input2, array, p.PadMode, value);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Reshape bc = (Reshape)__result["bc2"];
		Call call = (Call)__result["call"];
		Pad p = (Pad)__result["p"];
		Reshape bc2 = (Reshape)__result["bc1"];
		TensorConst pads = (TensorConst)__result["pads"];
		TensorConst value = (TensorConst)__result["value"];
		TensorConst input = (TensorConst)__result["input"];
		return GetReplace(bc, call, p, bc2, pads, value, input);
	}
}
