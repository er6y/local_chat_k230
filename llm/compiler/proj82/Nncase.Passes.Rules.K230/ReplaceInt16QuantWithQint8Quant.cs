using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ReplaceInt16QuantWithQint8Quant : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEConv2D("conv_dw", (GNNEConv2D _) => true, Nncase.PatternMatch.F.K230.IsGNNEConv2D("conv", (GNNEConv2D _) => true, Nncase.PatternMatch.F.K230.IsGNNEStore("st", (GNNEStore _) => true, Nncase.PatternMatch.F.Math.IsQuantize("q", (Quantize _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad("ld", (GNNELoad _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input"))))), Nncase.PatternMatch.F.K230.IsGNNELoadW("lw", (GNNELoadW _) => true, Nncase.PatternMatch.Utility.IsTensorConst("old_w")), Nncase.PatternMatch.F.K230.IsGNNELoadW("lwb", (GNNELoadW _) => true, Nncase.PatternMatch.Utility.IsTensorConst("old_wb")), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_weightsBiasQint8"), Nncase.PatternMatch.F.K230.IsGNNELoadW("lact", (GNNELoadW _) => true, Nncase.PatternMatch.Utility.IsTensorConst("old_act")), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_actQint8"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_deqBias"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_shiftBits"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_shiftBitsQint8"), Nncase.PatternMatch.F.Math.IsQuantParamOf("conv_dw_qint8Qp", (QuantParamOf _) => true), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_padding"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_stride"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_dilation"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_groups"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_is16Quant"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_padValue"), Nncase.PatternMatch.Utility.IsTensorConst("conv_dw_weightsQInt8"));


	public Expr? GetReplace(GNNEConv2D conv_dw, GNNEConv2D conv, GNNEStore st, Quantize q, GNNELoad ld, TensorConst input, GNNELoadW lw, TensorConst old_w, GNNELoadW lwb, TensorConst old_wb, GNNELoadW lact, TensorConst old_act, TensorConst conv_dw_weightsBiasQint8, TensorConst conv_dw_actQint8, TensorConst conv_dw_deqBias, TensorConst conv_dw_shiftBits, TensorConst conv_dw_shiftBitsQint8, QuantizeParam conv_dw_qint8Qp, TensorConst conv_dw_padding, TensorConst conv_dw_stride, TensorConst conv_dw_dilation, TensorConst conv_dw_groups, TensorConst conv_dw_is16Quant, TensorConst conv_dw_padValue, TensorConst conv_dw_weightsQInt8)
	{
		if (conv.CheckedDataType == DataTypes.Int16)
		{
			Call input2 = Nncase.IR.F.Math.Quantize(ld, new QuantParam(conv_dw_qint8Qp.ZeroPoint, conv_dw_qint8Qp.Scale), DataTypes.UInt8);
			Call input3 = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.UInt8, input2);
			Call act = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, conv_dw.ActParamQInt8.GetAct0Data);
			Tensor<byte> tensor = Tensor.From(old_wb.Value.ToArray<byte>(), conv_dw_weightsBiasQint8.Value.Shape);
			Call weightsBias = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor);
			Tensor<byte> tensor2 = Tensor.From(old_w.Value.ToArray<byte>(), conv_dw_weightsQInt8.Value.Shape);
			Call weights = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor2);
			return Nncase.IR.K230.F.Tensors.GNNEConv2D(DataTypes.UInt8, input3, weights, conv_dw_weightsQInt8, weightsBias, conv_dw_weightsBiasQint8, act, conv_dw_actQint8, conv_dw_deqBias, conv_dw_shiftBits, conv_dw_shiftBitsQint8, conv_dw_qint8Qp.ZeroPoint, conv_dw_padding, conv_dw_stride, conv_dw_dilation, conv_dw_groups, conv_dw_is16Quant, conv_dw_padValue, conv_dw.ActParam, conv_dw.ActParamQInt8);
		}
		if (old_w.CheckedDataType == DataTypes.Int16)
		{
			Call act2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, conv_dw.ActParamQInt8.GetAct0Data);
			Tensor<byte> tensor3 = Tensor.From(old_w.Value.ToArray<byte>(), conv_dw_weightsQInt8.Value.Shape);
			Call weights2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor3);
			Call input4 = Nncase.IR.F.Math.Quantize(q, new QuantParam(conv_dw_qint8Qp.ZeroPoint, conv_dw_qint8Qp.Scale), DataTypes.UInt8);
			Call input5 = Nncase.IR.K230.F.Tensors.GNNELoad(DataTypes.UInt8, input4);
			Tensor<byte> tensor4 = Tensor.From(old_wb.Value.ToArray<byte>(), conv_dw_weightsBiasQint8.Value.Shape);
			Call weightsBias2 = Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.UInt8, tensor4);
			return Nncase.IR.K230.F.Tensors.GNNEConv2D(DataTypes.UInt8, input5, weights2, conv_dw_weightsQInt8, weightsBias2, conv_dw_weightsBiasQint8, act2, conv_dw_actQint8, conv_dw_deqBias, conv_dw_shiftBits, conv_dw_shiftBitsQint8, conv_dw_qint8Qp.ZeroPoint, conv_dw_padding, conv_dw_stride, conv_dw_dilation, conv_dw_groups, conv_dw_is16Quant, conv_dw_padValue, conv_dw.ActParam, conv_dw.ActParamQInt8);
		}
		return null;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEConv2D conv_dw = (GNNEConv2D)__result["conv_dw"];
		GNNEConv2D conv = (GNNEConv2D)__result["conv"];
		GNNEStore st = (GNNEStore)__result["st"];
		Quantize q = (Quantize)__result["q"];
		GNNELoad ld = (GNNELoad)__result["ld"];
		TensorConst input = (TensorConst)__result["input"];
		GNNELoadW lw = (GNNELoadW)__result["lw"];
		TensorConst old_w = (TensorConst)__result["old_w"];
		GNNELoadW lwb = (GNNELoadW)__result["lwb"];
		TensorConst old_wb = (TensorConst)__result["old_wb"];
		GNNELoadW lact = (GNNELoadW)__result["lact"];
		TensorConst old_act = (TensorConst)__result["old_act"];
		TensorConst conv_dw_weightsBiasQint = (TensorConst)__result["conv_dw_weightsBiasQint8"];
		TensorConst conv_dw_actQint = (TensorConst)__result["conv_dw_actQint8"];
		TensorConst conv_dw_deqBias = (TensorConst)__result["conv_dw_deqBias"];
		TensorConst conv_dw_shiftBits = (TensorConst)__result["conv_dw_shiftBits"];
		TensorConst conv_dw_shiftBitsQint = (TensorConst)__result["conv_dw_shiftBitsQint8"];
		QuantizeParam conv_dw_qint8Qp = ((TensorConst)__result["conv_dw_qint8Qp"]).Value.ToScalar<QuantizeParam>();
		TensorConst conv_dw_padding = (TensorConst)__result["conv_dw_padding"];
		TensorConst conv_dw_stride = (TensorConst)__result["conv_dw_stride"];
		TensorConst conv_dw_dilation = (TensorConst)__result["conv_dw_dilation"];
		TensorConst conv_dw_groups = (TensorConst)__result["conv_dw_groups"];
		TensorConst conv_dw_is16Quant = (TensorConst)__result["conv_dw_is16Quant"];
		TensorConst conv_dw_padValue = (TensorConst)__result["conv_dw_padValue"];
		TensorConst conv_dw_weightsQInt = (TensorConst)__result["conv_dw_weightsQInt8"];
		return GetReplace(conv_dw, conv, st, q, ld, input, lw, old_w, lwb, old_wb, lact, old_act, conv_dw_weightsBiasQint, conv_dw_actQint, conv_dw_deqBias, conv_dw_shiftBits, conv_dw_shiftBitsQint, conv_dw_qint8Qp, conv_dw_padding, conv_dw_stride, conv_dw_dilation, conv_dw_groups, conv_dw_is16Quant, conv_dw_padValue, conv_dw_weightsQInt);
	}
}
