using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEPdp0DWEvaluator : IEvaluator<GNNEPdp0DW>, IEvaluator, ITypeInferencer<GNNEPdp0DW>, ITypeInferencer, ICostEvaluator<GNNEPdp0DW>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEPdp0DW target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEPdp0DW p)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(p, GNNEPdp0DW.Input);
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(p, GNNEPdp0DW.Weights);
		byte[] weightsBias = context.GetArgumentValueAsArray<byte>(p, GNNEPdp0DW.WeightsBias);
		Half[] argumentValueAsArray = context.GetArgumentValueAsArray<Half>(p, GNNEPdp0DW.Act);
		byte deqBias = context.GetArgumentValueAsScalar<byte>(p, GNNEPdp0DW.DeqBias);
		long argumentValueAsScalar = context.GetArgumentValueAsScalar<long>(p, GNNEPdp0DW.ShiftBits);
		long[] array = context.GetArgumentValueAsArray<long>(p, GNNEPdp0DW.Padding).ToArray();
		long[] argumentValueAsArray2 = context.GetArgumentValueAsArray<long>(p, GNNEPdp0DW.Stride);
		long[] argumentValueAsArray3 = context.GetArgumentValueAsArray<long>(p, GNNEPdp0DW.Dilation);
		long argumentValueAsScalar2 = context.GetArgumentValueAsScalar<long>(p, GNNEPdp0DW.Groups);
		float[] inputDeq = argumentValueAsTensor.ToArray<float>();
		inputDeq.Select((float _, int i) => inputDeq[i] -= (int)deqBias).AsParallel().ToArray();
		float[] weightsDeq = argumentValueAsTensor2.ToArray<float>();
		int qArgPerChannel = argumentValueAsTensor2.Dimensions[1] * argumentValueAsTensor2.Dimensions[2] * argumentValueAsTensor2.Dimensions[3];
		weightsDeq.Select((float _, int i) => weightsDeq[i] -= (int)weightsBias[i / qArgPerChannel]).AsParallel().ToArray();
		Tensor tensor = OrtKI.Conv(OrtKISharp.Tensor.MakeTensor(inputDeq, ((IEnumerable<int>)argumentValueAsTensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), OrtKISharp.Tensor.MakeTensor(weightsDeq, ((IEnumerable<int>)argumentValueAsTensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), K230Kernels.Proc(argumentValueAsTensor2.Dimensions[0]), "NOTSET", argumentValueAsArray3, argumentValueAsScalar2, new long[2]
		{
			argumentValueAsTensor2.Dimensions[2],
			argumentValueAsTensor2.Dimensions[3]
		}, new long[4]
		{
			array[0],
			array[2],
			array[1],
			array[3]
		}, argumentValueAsArray2).ToTensor();
		float[] array2 = tensor.ToArray<float>();
		float[] array3 = new float[K230Kernels.ComputeSize(tensor.Shape)];
		int num = tensor.Dimensions[2] * tensor.Dimensions[3];
		for (int j = 0; j < array2.Length; j++)
		{
			int channel = j / num;
			array3[j] = K230Kernels.ApplyAct0(array2[j], argumentValueAsArray.ToArray(), channel, (sbyte)argumentValueAsScalar);
		}
		float[] array4 = array3.Select((float x) => (float)System.Math.Round(x)).ToArray();
		Half[] array5 = array3.Select((float x) => (Half)x).ToArray();
		Tensor<float> tensor2 = Tensor.From(array4, tensor.Shape);
		Tensor<Half> tensor3 = Tensor.From(array5, tensor.Shape);
		if (p.DestType == DataTypes.UInt8)
		{
			return Value.FromTensor(tensor2.Cast<byte>(CastMode.KDefault));
		}
		if (p.DestType == DataTypes.Int8)
		{
			return Value.FromTensor(tensor2.Cast<sbyte>(CastMode.KDefault));
		}
		if (p.DestType == DataTypes.Int16)
		{
			return Value.FromTensor(tensor2.Cast<short>(CastMode.KDefault));
		}
		return Value.FromTensor(tensor3.Cast<Half>(CastMode.KDefault));
	}

	private IRType Visit(ITypeInferenceContext context, GNNEPdp0DW target, TensorType input, TensorType weights)
	{
		if (input.DType != DataTypes.Int8 && input.DType != DataTypes.UInt8 && input.DType != DataTypes.Int16)
		{
			return new InvalidType("Unsupported input_type, should be one of [int8, uint8, int16]");
		}
		if (weights.DType != DataTypes.Int8 && weights.DType != DataTypes.UInt8 && weights.DType != DataTypes.Int16)
		{
			return new InvalidType("Unsupported w_type, should be one of [int8, uint8, int16]");
		}
		if (input.DType == DataTypes.Int16 && weights.DType == DataTypes.Int16)
		{
			return new InvalidType("int16 for both of input_type and w_type is not supported");
		}
		PrimType destType = target.DestType;
		Expr[] arguments = context.GetArguments(target, GNNEPdp0DW.Stride, GNNEPdp0DW.Padding, GNNEPdp0DW.Dilation, GNNEPdp0DW.Groups);
		if (input.Shape.IsUnranked)
		{
			return input with
			{
				Shape = Shape.Unknown(4)
			};
		}
		List<Dimension> list = input.Shape.ToList();
		if (arguments[0] is TensorConst tensorConst && arguments[1] is TensorConst tensorConst2 && arguments[2] is TensorConst tensorConst3 && arguments[3] is TensorConst tensorConst4 && input.Shape[2].IsFixed && input.Shape[3].IsFixed && weights.Shape[2].IsFixed && weights.Shape[3].IsFixed)
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
		return new TensorType(destType, new Shape(list));
	}

	public IRType Visit(ITypeInferenceContext context, GNNEPdp0DW target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEPdp0DW.Input);
		TensorType weights = context.CheckArgumentType<TensorType>(target, GNNEPdp0DW.Weights);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Input);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Weights);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.WeightsBias);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.WeightsBiasQint8);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Act);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.ActQint8);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.DeqBias);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.ShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.ShiftBitsQint8);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Qint8Qp);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Padding);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Stride);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Dilation);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Groups);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.Is16Quant);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.PadValue);
		context.CheckArgumentType<IRType>(target, GNNEPdp0DW.WeightsQInt8);
		return Visit(context, target, input, weights);
	}
}
