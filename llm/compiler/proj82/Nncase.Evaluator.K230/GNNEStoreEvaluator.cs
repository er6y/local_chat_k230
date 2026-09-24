using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public class GNNEStoreEvaluator : IEvaluator<GNNEStore>, IEvaluator, ITypeInferencer<GNNEStore>, ITypeInferencer, ICostEvaluator<GNNEStore>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEStore target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	private IValue Visit(GNNEStore target, Tensor input, Tensor strides)
	{
		DataType elementType = input.ElementType;
		PrimType destType = target.DestType;
		(DataType, PrimType) tuple = (elementType, destType);
		Tensor tensor;
		if (tuple.Item1 == DataTypes.Float16 && tuple.Item2 == DataTypes.Float32)
		{
			tensor = input.Cast<float>();
		}
		else
		{
			(DataType, PrimType) tuple2 = tuple;
			if (!(tuple2.Item1 == tuple2.Item2))
			{
				throw new ArgumentOutOfRangeException($"{elementType} => {destType}");
			}
			tensor = input;
		}
		Tensor tensor2 = tensor;
		return OrtKI.Slice(starts: Tensor.From(tensor2.Shape.Select((Dimension _) => 0L).ToArray()).ToOrtTensor(), ends: Tensor.From(((IEnumerable<Dimension>)tensor2.Shape).Select((Func<Dimension, long>)((Dimension i) => i.FixedValue)).ToArray()).ToOrtTensor(), axes: Tensor.From(((IEnumerable<Dimension>)tensor2.Shape).Select((Func<Dimension, int, long>)((Dimension _, int i) => i)).ToArray()).ToOrtTensor(), data: tensor2.ToOrtTensor(), steps: strides.ToOrtTensor()).ToValue();
	}

	private IRType Visit(ITypeInferenceContext context, GNNEStore target, TensorType input)
	{
		Tensor<int> tensor = ((TensorConst)context.GetArgument(target, GNNEStore.Strides)).Value.Cast<int>();
		if (tensor.Any((int s) => s != 1))
		{
			return new InvalidType("Not Support Stride != 1, Please Fix it.");
		}
		if (tensor.Length != input.Shape.Rank)
		{
			return new InvalidType($"Stride Length {tensor.Length} != Input Rank {input.Shape.Rank}");
		}
		DataType dType = input.DType;
		PrimType destType = target.DestType;
		(DataType, PrimType) tuple = (dType, destType);
		if (tuple.Item1 != DataTypes.Float16 && tuple.Item2 == DataTypes.Float32)
		{
			return new InvalidType("when store output is float, input should be float16");
		}
		(DataType, PrimType) tuple2 = tuple;
		if (tuple2.Item1 != tuple2.Item2 && tuple2.Item2 != DataTypes.Float32)
		{
			return new InvalidType("store input type and output type should be same");
		}
		(DataType, PrimType) tuple3 = tuple;
		if (tuple3.Item1 != DataTypes.Int8 && tuple3.Item1 != DataTypes.UInt8 && tuple3.Item1 != DataTypes.Int16 && tuple3.Item1 != DataTypes.Float16 && tuple3.Item1 != DataTypes.Float32)
		{
			return new InvalidType("store input type should be one of [int8, uint8, int16, float16]");
		}
		(DataType, PrimType) tuple4 = tuple;
		if (tuple4.Item1 == DataTypes.Int8 || tuple4.Item1 == DataTypes.UInt8 || tuple4.Item1 == DataTypes.Int16 || tuple4.Item1 == DataTypes.Float16 || tuple4.Item1 == DataTypes.Float32)
		{
			return input with
			{
				DType = tuple4.Item2
			};
		}
		return new InvalidType("Not Support Load (Input: " + dType.GetDisplayName() + " or (Output: " + destType.GetDisplayName());
	}

	public IValue Visit(IEvaluateContext context, GNNEStore target)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(target, GNNEStore.Input);
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(target, GNNEStore.Strides);
		return Visit(target, argumentValueAsTensor, argumentValueAsTensor2);
	}

	public IRType Visit(ITypeInferenceContext context, GNNEStore target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEStore.Input);
		context.CheckArgumentType<IRType>(target, GNNEStore.Input);
		context.CheckArgumentType<IRType>(target, GNNEStore.Strides);
		return Visit(context, target, input);
	}
}
