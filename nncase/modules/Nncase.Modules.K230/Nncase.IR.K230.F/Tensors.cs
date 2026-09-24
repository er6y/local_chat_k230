using System.Collections.Generic;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230.F;

public static class Tensors
{
	public static Call FakeDynamicGNNEMatMul(Expr inputA, Expr inputB, Expr act, bool dynamicChannel)
	{
		return new Call(new FakeDynamicGNNEMatMul(dynamicChannel), inputA, inputB, act);
	}

	public static Call FakeMatMul(Expr inputA, Expr inputB, Expr act, ActParam2 actParam2)
	{
		return new Call(new FakeMatMul(actParam2), inputA, inputB, act);
	}

	public static Call FakeActivation(Expr inputa, Expr inputb, Expr act, Expr outChannels, Expr inAShiftBits, Expr inBShiftBits, Expr outShiftBits, Expr is16Segments, GnneActivationType type, ActParamBase actParam, int[] outputShape)
	{
		return new Call(new FakeActivation(type, actParam, new IRArray<int>((IEnumerable<int>)outputShape)), inputa, inputb, act, outChannels, inAShiftBits, inBShiftBits, outShiftBits, is16Segments);
	}

	public static Call FakeConv2D(Expr input, Expr weights, Expr act, Expr padding, Expr stride, Expr dilation, Expr groups, Expr padValue, ActParam2 actParam)
	{
		return new Call(new FakeConv2D(actParam), input, weights, act, padding, stride, dilation, groups, padValue);
	}

	public static Call FakeConv2DTranspose(Expr input, Expr weights, Expr act, Expr outputShape, Expr padding, Expr outputPadding, Expr stride, Expr dilation, Expr groups, Expr padValue, ActParam2 actParam)
	{
		return new Call(new FakeConv2DTranspose(actParam), input, weights, act, outputShape, padding, outputPadding, stride, dilation, groups, padValue);
	}

	public static Call FakePdp(ReduceOp reduceOp, Expr input, Expr padValue, Expr filter, Expr stride, Expr padding, Expr countIncludePad)
	{
		return new Call(new FakePdp(reduceOp), input, padValue, filter, stride, padding, countIncludePad);
	}

	public static Call FakeAi2dPad(Expr input, Expr padding, Expr value, PadMode mode)
	{
		return new Call(new FakeAi2dPad(mode), input, padding, value);
	}

	public static Call FakeLSTM(Expr input, Expr wXc, Expr actXc, Expr wRc, Expr actRc, Expr initialH, Expr initialC, Expr segFittingParamFt, Expr segFittingParamGt, Expr hasStatic, LSTMDirection lstmDirection, Expr outputSize, ActParam2 actParamXc, ActParam2 actParamRc)
	{
		return new Call(new FakeLSTM(lstmDirection, actParamXc, actParamRc), input, wXc, actXc, wRc, actRc, initialH, initialC, segFittingParamFt, segFittingParamGt, hasStatic, outputSize);
	}

	public static Call FakeAi2dResize(MFU_CROP_RESIZE resizeMethod, bool halfPixelCenters, bool alignCornersm, Expr input, Expr newSize)
	{
		return new Call(new FakeAi2dResize(resizeMethod, halfPixelCenters, alignCornersm), input, newSize);
	}

	public static Call GNNETranspose(Expr input, MFU_TRANS_PERMUTE perm)
	{
		return new Call(new GNNETranspose(perm), input);
	}

	public static Call GNNEPdp1(PrimType destType, MFU_PDP_OP reduceOp, Expr input, Expr filter, Expr stride, Expr padding, Expr quantParams, Expr depuantParams, Expr value, Expr shiftBits, Expr countIncludePad)
	{
		return new Call(new GNNEPdp1(reduceOp, destType), input, filter, stride, padding, quantParams, depuantParams, value, shiftBits, countIncludePad);
	}

	public static Call GNNEPdp0Reduce(PU_PDP0_MODE reduceOp, PrimType outputType, ActParam2 actParam, Expr input, Expr filter, Expr stride, Expr padding, Expr depuantParams, Expr value, Expr shiftBits, Expr countIncludePad, Expr act)
	{
		return new Call(new GNNEPdp0Reduce(reduceOp, outputType, actParam), input, filter, stride, padding, depuantParams, value, shiftBits, countIncludePad, act);
	}

	public static Call GNNEStore(DataType destType, Expr input)
	{
		return GNNEStore((PrimType)destType, input, new long[4] { 1L, 1L, 1L, 1L });
	}

	public static Call GNNEStore(DataType destType, Expr input, Expr stride)
	{
		return new Call(new GNNEStore((PrimType)destType), input, stride);
	}

	public static Call GNNEPad(Expr input, Expr pads, Expr value)
	{
		return new Call(new GNNEPad(PadMode.Constant), input, pads, value);
	}

	public static Call GNNELoad(PrimType destType, Expr input)
	{
		return new Call(new GNNELoad(destType), input);
	}

	public static Call GNNEStoreQuant(DataType destType, Expr input)
	{
		return new Call(new GNNEStore((PrimType)destType), input, new long[4] { 1L, 1L, 1L, 1L });
	}

	public static Call GNNELoadW(PrimType destType, Expr input)
	{
		return new Call(new GNNELoadW(destType), input);
	}

	public static Call GNNELoadIFDeq(DataType destType, Expr input)
	{
		return new Call(new GNNELoad((PrimType)destType), input);
	}

	public static Call GNNEActivation(Expr inputa, Expr inputb, Expr act, Expr inAShiftBits, Expr inBShiftBits, Expr outShiftBits, Expr deqAParams, Expr deqBParams, Expr outChannels, GnneActivationType type, Expr is16Segments, DataType destType, ActParamBase actParam, int[] outputShape)
	{
		return new Call(new GNNEActivation(type, (PrimType)destType, actParam, outputShape), inputa, inputb, act, inAShiftBits, inBShiftBits, outShiftBits, deqAParams, deqBParams, outChannels, is16Segments);
	}

	public static Call SimpleSingleInputGNNEAct(Expr input, ActParamBase actParam, int channels, QuantParam qp, DataType destType, ActParam2 actParam2, int[] outputShape)
	{
		return GNNEActivation(input, None.Default, GNNELoadW(DataTypes.Float16, actParam.ToAct1Data()), 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
		{
			new DeQuantizeParam(qp.ZeroPoint, qp.Scale)
		}), Tensor.FromArray(new DeQuantizeParam[1]
		{
			new DeQuantizeParam(qp.ZeroPoint, qp.Scale)
		}), channels, GnneActivationType.Uninitialized, false, destType, actParam2, outputShape);
	}

	public static Call GNNEConv2D(PrimType destType, Expr input, Expr weights, Expr weightsQInt8, Expr weightsBias, Expr weightsBiasQint8, Expr act, Expr actQint8, Expr deqBias, Expr shiftBits, Expr shiftBitsQint8, Expr qint8Qp, Expr padding, Expr stride, Expr dilation, Expr groups, Expr is16Quant, Expr padValue, ActParam2 actParam, ActParam2 actParamQInt8)
	{
		return new Call(new GNNEConv2D(actParam, actParamQInt8, destType), input, weights, weightsBias, weightsBiasQint8, act, actQint8, deqBias, shiftBits, shiftBitsQint8, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, weightsQInt8);
	}

	public static Call GNNEPdp0DW(PrimType destType, Expr input, Expr weights, Expr weightsQInt8, Expr weightsBias, Expr weightsBiasQint8, Expr act, Expr actQint8, Expr deqBias, Expr shiftBits, Expr shiftBitsQint8, Expr qint8Qp, Expr padding, Expr stride, Expr dilation, Expr groups, Expr is16Quant, Expr padValue, ActParam2 actParam, ActParam2 actParamQInt8)
	{
		return new Call(new GNNEPdp0DW(actParam, actParamQInt8, destType), input, weights, weightsBias, weightsBiasQint8, act, actQint8, deqBias, shiftBits, shiftBitsQint8, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, weightsQInt8);
	}

	public static Call GNNEConv2DTranspose(PrimType destType, Expr input, Expr weights, Expr weightsQInt8, Expr weightsBias, Expr weightsBiasQint8, Expr act, Expr actQint8, Expr deqBias, Expr shiftBits, Expr shiftBitsQint8, Expr qint8Qp, Expr padding, Expr stride, Expr dilation, Expr groups, Expr is16Quant, Expr padValue, ActParam2 actParam, ActParam2 actParamQInt8, Expr outputPadding, Expr outputShape)
	{
		return new Call(new GNNEConv2DTranspose(actParam, actParamQInt8, destType), input, weights, weightsBias, weightsBiasQint8, act, actQint8, deqBias, shiftBits, shiftBitsQint8, qint8Qp, padding, stride, dilation, groups, is16Quant, padValue, weightsQInt8, outputPadding, outputShape);
	}

	public static Call Ai2dPad(Expr input, Expr padding, Expr value, Expr inDeqBias, Expr outQuantParam, PrimType outputType, PadMode mode)
	{
		return new Call(new Ai2dPad(mode, outputType), input, padding, value, inDeqBias, outQuantParam);
	}

	public static Call Ai2dResize(PrimType destType, MFU_CROP_RESIZE resizeMethod, bool alignCorners, bool halfPixelCenters, Expr input, Expr newSize, Expr inDeqBias, Expr outQuantParam)
	{
		return new Call(new Ai2dResize(alignCorners, resizeMethod, halfPixelCenters, destType), input, newSize, inDeqBias, outQuantParam);
	}

	public static Call GNNELSTM(PrimType destTypeO, PrimType destTypeOH, Expr input, Expr wXc, Expr actXc, Expr wRc, Expr actRc0, Expr actRc1, Expr initialH, Expr initialC, Expr segFittingParamFt, Expr segFittingParamGt, Expr wXcQarg, Expr wRcQarg, Expr actBin, Expr actBinQ, ActParam2 activationParamXc, ActParam2 activationParamRc0, ActParam2 activationParamRc1, Expr ifDeqBias, Expr xcShiftBits, Expr hDeqBias0, Expr hDeqBias1, Expr cShiftBits, Expr rcShiftBits0, Expr rcShiftBits1, Expr outHShiftBits, Expr outCShiftBits, ActParam2 activationParamBin, ActParam2 activationParamBinQ, LSTMDirection direction, Expr hasStatic, Expr outputSize)
	{
		return new Call(new GNNELSTM(direction, activationParamBin, activationParamBinQ, activationParamXc, activationParamRc0, activationParamRc1, destTypeO, destTypeOH), input, wXc, actXc, wRc, actRc0, actRc1, initialH, initialC, segFittingParamFt, segFittingParamGt, wXcQarg, wRcQarg, actBin, actBinQ, ifDeqBias, xcShiftBits, hDeqBias0, hDeqBias1, cShiftBits, rcShiftBits0, rcShiftBits1, outHShiftBits, outCShiftBits, hasStatic, outputSize);
	}

	public static Call GNNEMatMul(ActParam2 actParam, PrimType destType, Expr inputA, Expr inputB, Expr act, Expr inputABias, Expr inAShiftBits, Expr inBShiftBits, Expr shiftBits, Expr deqBBias)
	{
		return new Call(new GNNEMatMul(actParam, destType), inputA, inputB, act, inputABias, inAShiftBits, inBShiftBits, shiftBits, deqBBias);
	}
}
