using System;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public class GNNELoadEvaluator : IEvaluator<GNNELoad>, IEvaluator, ITypeInferencer<GNNELoad>, ITypeInferencer, ICostEvaluator<GNNELoad>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNELoad target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(GNNELoad target, Tensor input)
	{
		DataType elementType = input.ElementType;
		PrimType destType = target.DestType;
		(DataType, PrimType) tuple = (elementType, destType);
		if (tuple.Item1 == DataTypes.Float32 && tuple.Item2 == DataTypes.Float16)
		{
			return Value.FromTensor(input.Cast<Half>());
		}
		(DataType, PrimType) tuple2 = tuple;
		if (tuple2.Item1 == tuple2.Item2)
		{
			return Value.FromTensor(input);
		}
		(DataType, PrimType) tuple3 = tuple;
		throw new NotSupportedException("GNNELoadVector Error With " + tuple3.Item1.GetDisplayName() + " => " + tuple3.Item2.GetDisplayName());
	}

	public IRType Visit(GNNELoad target, TensorType input)
	{
		DataType dType = input.DType;
		PrimType destType = target.DestType;
		(DataType, PrimType) tuple = (dType, destType);
		if (tuple.Item1 == DataTypes.Float32 && tuple.Item2 != DataTypes.Float16)
		{
			return new InvalidType("when load input type is float, output type should be float16");
		}
		(DataType, PrimType) tuple2 = tuple;
		if (tuple2.Item1 != tuple2.Item2 && tuple2.Item1 != DataTypes.Float32)
		{
			return new InvalidType("load input type and output type should be same");
		}
		(DataType, PrimType) tuple3 = tuple;
		if (tuple3.Item2 != DataTypes.Int8 && tuple3.Item2 != DataTypes.Int16 && tuple3.Item2 != DataTypes.Float16 && tuple3.Item2 != DataTypes.Float32 && tuple3.Item2 != DataTypes.UInt8)
		{
			return new InvalidType("load output type should be one of [int8, int16, float16, uint8]");
		}
		(DataType, PrimType) tuple4 = tuple;
		if (tuple4.Item1 == DataTypes.Int8 || tuple4.Item1 == DataTypes.Int16 || tuple4.Item1 == DataTypes.Float16 || tuple4.Item1 == DataTypes.Float32 || tuple4.Item1 == DataTypes.UInt8)
		{
			return input with
			{
				DType = tuple4.Item2
			};
		}
		return new InvalidType("Not Support Load (Input: " + dType.GetDisplayName() + " or (Output: " + destType.GetDisplayName());
	}

	public IValue Visit(IEvaluateContext context, GNNELoad target)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(target, GNNELoad.Input);
		return Visit(target, argumentValueAsTensor);
	}

	public IRType Visit(ITypeInferenceContext context, GNNELoad target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNELoad.Input);
		context.CheckArgumentType<IRType>(target, GNNELoad.Input);
		return Visit(target, input);
	}
}
