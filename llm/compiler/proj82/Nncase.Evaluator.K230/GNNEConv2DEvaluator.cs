using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEConv2DEvaluator : IEvaluator<GNNEConv2D>, IEvaluator, ITypeInferencer<GNNEConv2D>, ITypeInferencer, ICostEvaluator<GNNEConv2D>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEConv2D target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEConv2D conv)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(conv, GNNEConv2D.Input);
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(conv, GNNEConv2D.Weights);
		byte[] weightsBias = context.GetArgumentValueAsArray<byte>(conv, GNNEConv2D.WeightsBias);
		Half[] argumentValueAsArray = context.GetArgumentValueAsArray<Half>(conv, GNNEConv2D.Act);
		byte deqBias = context.GetArgumentValueAsScalar<byte>(conv, GNNEConv2D.DeqBias);
		long argumentValueAsScalar = context.GetArgumentValueAsScalar<long>(conv, GNNEConv2D.ShiftBits);
		long[] argumentValueAsArray2 = context.GetArgumentValueAsArray<long>(conv, GNNEConv2D.Padding);
		long[] argumentValueAsArray3 = context.GetArgumentValueAsArray<long>(conv, GNNEConv2D.Stride);
		long[] argumentValueAsArray4 = context.GetArgumentValueAsArray<long>(conv, GNNEConv2D.Dilation);
		long argumentValueAsScalar2 = context.GetArgumentValueAsScalar<long>(conv, GNNEConv2D.Groups);
		long num = argumentValueAsTensor.Shape[1].FixedValue / argumentValueAsScalar2 * argumentValueAsTensor2.Shape[2].FixedValue * argumentValueAsTensor2.Shape[3].FixedValue * argumentValueAsTensor2.ElementType.SizeInBytes;
		byte[] array = new byte[argumentValueAsTensor2.Shape[0].FixedValue * num];
		for (int j = 0; j < argumentValueAsTensor2.Shape[0].FixedValue; j++)
		{
			Array.Copy(argumentValueAsTensor2.BytesBuffer.ToArray(), (long)j * (long)argumentValueAsTensor2.BytesBuffer.Length / argumentValueAsTensor2.Shape[0].FixedValue, array, j * num, num);
		}
		Dimension[] array2 = argumentValueAsTensor2.Shape.ToArray();
		array2[1] = (int)(argumentValueAsTensor.Shape[1].FixedValue / argumentValueAsScalar2);
		Tensor tensor = Tensor.FromBytes(new TensorType(argumentValueAsTensor2.ElementType, array2), array);
		float[] inputDeq = argumentValueAsTensor.ToArray<float>();
		inputDeq.Select((float _, int i) => inputDeq[i] -= (int)deqBias).AsParallel().ToArray();
		float[] weightsDeq = tensor.ToArray<float>();
		int qArgPerChannel = tensor.Dimensions[1] * tensor.Dimensions[2] * tensor.Dimensions[3];
		weightsDeq.Select((float _, int i) => weightsDeq[i] -= (int)weightsBias[i / qArgPerChannel]).AsParallel().ToArray();
		Tensor tensor2 = OrtKI.Conv(OrtKISharp.Tensor.MakeTensor(inputDeq, ((IEnumerable<int>)argumentValueAsTensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), OrtKISharp.Tensor.MakeTensor(weightsDeq, ((IEnumerable<int>)tensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), K230Kernels.Proc(tensor.Dimensions[0]), "NOTSET", argumentValueAsArray4, argumentValueAsScalar2, new long[2]
		{
			tensor.Dimensions[2],
			tensor.Dimensions[3]
		}, new long[4]
		{
			argumentValueAsArray2[0],
			argumentValueAsArray2[2],
			argumentValueAsArray2[1],
			argumentValueAsArray2[3]
		}, argumentValueAsArray3).ToTensor();
		float[] array3 = tensor2.ToArray<float>();
		float[] array4 = new float[K230Kernels.ComputeSize(tensor2.Shape)];
		int num2 = tensor2.Dimensions[2] * tensor2.Dimensions[3];
		for (int k = 0; k < array3.Length; k++)
		{
			int channel = k / num2;
			array4[k] = K230Kernels.ApplyAct0(array3[k], argumentValueAsArray.ToArray(), channel, (sbyte)argumentValueAsScalar);
		}
		float[] array5 = array4.Select((float x) => (float)System.Math.Round(x)).ToArray();
		Half[] array6 = array4.Select((float x) => (Half)x).ToArray();
		Tensor<float> tensor3 = Tensor.From(array5, tensor2.Shape);
		Tensor<Half> tensor4 = Tensor.From(array6, tensor2.Shape);
		if (conv.DestType == DataTypes.UInt8)
		{
			return Value.FromTensor(tensor3.Cast<byte>(CastMode.KDefault));
		}
		if (conv.DestType == DataTypes.Int8)
		{
			return Value.FromTensor(tensor3.Cast<sbyte>(CastMode.KDefault));
		}
		if (conv.DestType == DataTypes.Int16)
		{
			return Value.FromTensor(tensor3.Cast<short>(CastMode.KDefault));
		}
		return Value.FromTensor(tensor4.Cast<Half>(CastMode.KDefault));
	}

	private static IRType Conv2DTypeFp16(TensorType input, TensorType weights, Expr stride, Expr padding, Expr dilation, Expr groups, DataType target)
	{
		List<Dimension> list = input.Shape.ToList();
		list[1] = weights.Shape[0];
		if (stride is TensorConst tensorConst && padding is TensorConst tensorConst2 && dilation is TensorConst tensorConst3 && groups is TensorConst tensorConst4 && input.Shape[2].IsFixed && input.Shape[3].IsFixed && weights.Shape[2].IsFixed && weights.Shape[3].IsFixed)
		{
			Tensor<int> tensor = tensorConst.Value.Cast<int>();
			Tensor<int> tensor2 = tensorConst2.Value.Cast<int>();
			Tensor<int> tensor3 = tensorConst3.Value.Cast<int>();
			int num = tensorConst4.Value.ToScalar<int>();
			if (input.Shape[1].FixedValue < num || input.Shape[1].FixedValue % num != 0)
			{
				return new InvalidType($"The Input Channel / Groups Error ({input.Shape[1].FixedValue}/{num})");
			}
			list[2] = TypePatternUtility.GetWindowedOutputSize(input.Shape[2].FixedValue + tensor2[new int[2]] + tensor2[new int[2] { 0, 1 }], weights.Shape[2].FixedValue, tensor[new int[1]], tensor3[new int[1]], same: false);
			list[3] = TypePatternUtility.GetWindowedOutputSize(input.Shape[3].FixedValue + tensor2[new int[2] { 1, 0 }] + tensor2[new int[2] { 1, 1 }], weights.Shape[3].FixedValue, tensor[new int[1] { 1 }], tensor3[new int[1] { 1 }], same: false);
		}
		else
		{
			Dimension value = (list[3] = Dimension.Unknown);
			list[2] = value;
		}
		return new TensorType(target, new Shape(list));
	}

	private IRType Visit(ITypeInferenceContext context, GNNEConv2D target, TensorType input, TensorType weights)
	{
		int num = ((TensorConst)context.GetArgument(target, GNNEConv2D.Groups)).Value.ToScalar<int>();
		TensorType tensorType = weights;
		if (input.Shape[1] / num != weights.Shape[1])
		{
			tensorType = weights with
			{
				Shape = new Shape(weights.Shape[0].FixedValue, input.Shape[1].FixedValue / num, weights.Shape[2].FixedValue, weights.Shape[3].FixedValue)
			};
		}
		if (input.DType != DataTypes.Int8 && input.DType != DataTypes.UInt8 && input.DType != DataTypes.Int16)
		{
			return new InvalidType("Unsupported input_type, should be one of [int8, uint8, int16]");
		}
		if (tensorType.DType != DataTypes.Int8 && tensorType.DType != DataTypes.UInt8 && tensorType.DType != DataTypes.Int16)
		{
			return new InvalidType("Unsupported w_type, should be one of [int8, uint8, int16]");
		}
		if (input.DType == DataTypes.Int16 && tensorType.DType == DataTypes.Int16)
		{
			return new InvalidType("int16 for both of input_type and w_type is not supported");
		}
		_ = input.DType;
		PrimType destType = target.DestType;
		Expr[] arguments = context.GetArguments(target, GNNEConv2D.Stride, GNNEConv2D.Padding, GNNEConv2D.Dilation, GNNEConv2D.Groups);
		if (destType == DataTypes.Int8 || destType == DataTypes.UInt8 || destType == DataTypes.Int16)
		{
			IRType iRType = TypeInference.Conv2DType(input, tensorType, arguments[0], arguments[1], arguments[2], arguments[3]);
			if (iRType is TensorType tensorType2)
			{
				return tensorType2 with
				{
					DType = destType
				};
			}
			return iRType;
		}
		if (destType == DataTypes.Float16)
		{
			return Conv2DTypeFp16(input, tensorType, arguments[0], arguments[1], arguments[2], arguments[3], destType);
		}
		return new InvalidType("Conv2d output type should be one of [int8, int16, float16, uint8]");
	}

	public IRType Visit(ITypeInferenceContext context, GNNEConv2D target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEConv2D.Input);
		TensorType weights = context.CheckArgumentType<TensorType>(target, GNNEConv2D.Weights);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Input);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Weights);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.WeightsBias);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.WeightsBiasQint8);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Act);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.ActQint8);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.DeqBias);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.ShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.ShiftBitsQint8);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Qint8Qp);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Padding);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Stride);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Dilation);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Groups);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.Is16Quant);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.PadValue);
		context.CheckArgumentType<IRType>(target, GNNEConv2D.WeightsQInt8);
		return Visit(context, target, input, weights);
	}
}
