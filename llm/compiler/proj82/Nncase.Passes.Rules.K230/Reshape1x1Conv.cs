using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class Reshape1x1Conv : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsCallWildcard("fusionCall", FusionPattern.IsGNNEFusion<GNNEConv2D>());


	private Expr? GetReplace(Call call, Call ld, Call st, Call fusionCall, IReadOnlyList<Expr> fusionCallParams)
	{
		Expr input = call[GNNEConv2D.Input];
		Expr expr = call[GNNEConv2D.Weights];
		Expr weightsBias = call[GNNEConv2D.WeightsBias];
		Expr weightsBiasQint = call[GNNEConv2D.WeightsBiasQint8];
		Expr act = call[GNNEConv2D.Act];
		Expr actQint = call[GNNEConv2D.ActQint8];
		Expr deqBias = call[GNNEConv2D.DeqBias];
		Expr shiftBits = call[GNNEConv2D.ShiftBits];
		Expr shiftBitsQint = call[GNNEConv2D.ShiftBitsQint8];
		Expr qint8Qp = call[GNNEConv2D.Qint8Qp];
		Expr expr2 = call[GNNEConv2D.Padding];
		Expr expr3 = call[GNNEConv2D.Stride];
		Expr dilation = call[GNNEConv2D.Dilation];
		Expr expr4 = call[GNNEConv2D.Groups];
		Expr is16Quant = call[GNNEConv2D.Is16Quant];
		Expr padValue = call[GNNEConv2D.PadValue];
		Expr weightsQInt = call[GNNEConv2D.WeightsQInt8];
		GNNEConv2D gNNEConv2D = (GNNEConv2D)call.Target;
		Fusion fusion = (Fusion)fusionCall.Target;
		if ((input.CheckedShape[0].FixedValue == 1 || input.CheckedShape[2].FixedValue != 1 || input.CheckedShape[3].FixedValue != 1) && ((TensorConst)expr4).Value.ToScalar<int>() == 1 && ((TensorConst)expr3).Value.ToArray<int>().All((int x) => x == 1) && expr.CheckedShape.ToValueArray()[2..].All((int x) => x == 1) && ((TensorConst)expr2).Value.ToArray<int>().All((int x) => x == 0) && (input.CheckedDataType == DataTypes.UInt8 || input.CheckedDataType == DataTypes.Int8) && ((input.CheckedShape[1].FixedValue == expr.CheckedShape[1].FixedValue && new int[3] { 24, 20, 16 }.Any((int x) => input.CheckedShape[1].Value % x == 0 && input.CheckedShape[1].Value != x)) || input.CheckedShape[1].FixedValue != expr.CheckedShape[1].FixedValue) && input.CheckedShape[2].FixedValue * input.CheckedShape[3].FixedValue < 65536)
		{
			int[] obj = new int[4] { 24, 20, 16, 0 };
			obj[3] = input.CheckedShape[1].FixedValue;
			int num = obj.FirstOrDefault((int d) => input.CheckedShape[1].FixedValue % d == 0);
			int num2 = ((!(expr.CheckedDataType == DataTypes.Int16)) ? 1 : 2);
			int fixedValue = (expr.CheckedShape[0] * input.CheckedShape[1] * expr.CheckedShape[2] * expr.CheckedShape[3] * num2).FixedValue;
			byte[] array = ((TensorConst)((Call)expr)[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
			byte[] array2 = new byte[fixedValue];
			for (int i = 0; i < fixedValue; i++)
			{
				array2[i] = array[i / (input.CheckedShape[1].FixedValue * num2) * expr.CheckedShape[1].FixedValue * num2 + i % (input.CheckedShape[1].FixedValue * num2)];
			}
			Shape shape = new Shape(expr.CheckedShape[0], num, input.CheckedShape[1] / num, 1);
			Call weights = Nncase.IR.K230.F.Tensors.GNNELoadW((PrimType)expr.CheckedDataType, Tensor.FromBytes(new TensorType(expr.CheckedDataType, shape), array2));
			Shape shape2 = new Shape(input.CheckedShape[0], num, input.CheckedShape[1] / num, input.CheckedShape[2] * input.CheckedShape[3]);
			Call call2 = Nncase.IR.F.Tensors.Reshape(fusionCallParams[0], shape2);
			call2.CheckedType = new TensorType(input.CheckedDataType, shape2);
			Var var = new Var("input", new TensorType(call2.CheckedDataType, call2.CheckedShape));
			Call input2 = Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)ld.CheckedDataType, var);
			Call input3 = Nncase.IR.K230.F.Tensors.GNNEConv2D(gNNEConv2D.DestType, input2, weights, weightsQInt, weightsBias, weightsBiasQint, act, actQint, deqBias, shiftBits, shiftBitsQint, qint8Qp, expr2, expr3, dilation, expr4, is16Quant, padValue, gNNEConv2D.ActParam, gNNEConv2D.ActParamQInt8);
			Call body = Nncase.IR.K230.F.Tensors.GNNEStore(st.CheckedDataType, input3);
			return Nncase.IR.F.Tensors.Reshape(new Call(new Fusion(fusion.Name, fusion.ModuleKind, body, var), call2), st.CheckedShape);
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Call ld = (Call)__result["ld"];
		Call st = (Call)__result["st"];
		Call fusionCall = (Call)__result["fusionCall"];
		IReadOnlyList<Expr> fusionCallParams = (IReadOnlyList<Expr>)__result["fusionCallParams"];
		return GetReplace(call, ld, st, fusionCall, fusionCallParams);
	}
}
