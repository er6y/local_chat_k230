using System;
using System.Linq;
using System.Runtime.InteropServices;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;

namespace Nncase.Evaluator.K230;

[EvaluatorGenerator]
[TypeInferGenerator]
public sealed class DynamicGNNEMatMulEvaluator : IEvaluator<DynamicGNNEMatMul>, IEvaluator, ITypeInferencer<DynamicGNNEMatMul>, ITypeInferencer, ICostEvaluator<DynamicGNNEMatMul>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, DynamicGNNEMatMul target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, DynamicGNNEMatMul.InputA);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, DynamicGNNEMatMul.InputB);
		TensorType argumentType3 = context.GetArgumentType<TensorType>(target, DynamicGNNEMatMul.Act);
		TensorType argumentType4 = context.GetArgumentType<TensorType>(target, DynamicGNNEMatMul.InputABias);
		TensorType argumentType5 = context.GetArgumentType<TensorType>(target, DynamicGNNEMatMul.InputBBias);
		TensorType returnType = context.GetReturnType<TensorType>();
		Shape shape = argumentType.Shape;
		int num;
		if (!shape[shape.Count - 1].IsFixed)
		{
			num = 1;
		}
		else
		{
			Shape shape2 = argumentType.Shape;
			num = shape2[shape2.Count - 1].FixedValue;
		}
		int num2 = num;
		float num3 = 768f;
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2) + CostUtility.GetMemoryAccess(argumentType3) + CostUtility.GetMemoryAccess(argumentType4) + CostUtility.GetMemoryAccess(argumentType5),
			[CostFactorNames.MemoryStore] = CostUtility.GetMemoryAccess(returnType),
			[CostFactorNames.CPUCycles] = CostUtility.GetCPUCycles(returnType, (float)num2 / num3)
		};
	}

	private static Tensor GnneMatmulV2<TIA, TIB>(ReadOnlySpan<TIA> inputA, ReadOnlySpan<TIB> inputB, TensorType outputType, ReadOnlySpan<Half> act, ReadOnlySpan<byte> inABias, int aBatch0, int aBatch1, int aRows, int aCols, int bBatch0, int bBatch1, int bCols, byte deqBBias, int inAShiftBits, int inBShiftBits, sbyte shiftBits, bool dynamicChannel) where TIA : unmanaged where TIB : unmanaged
	{
		DataType dType = outputType.DType;
		if (dType == DataTypes.UInt8)
		{
			Tensor<byte> tensor = new Tensor<byte>(outputType.Shape.ToValueArray());
			K230Kernels.DynamicGnneMatmul(inputA, inputB, tensor.Buffer.Span, act, inABias, aBatch0, aBatch1, aRows, aCols, bBatch0, bBatch1, bCols, deqBBias, inAShiftBits, inBShiftBits, shiftBits, dynamicChannel);
			return tensor;
		}
		if (dType == DataTypes.Int8)
		{
			Tensor<sbyte> tensor2 = new Tensor<sbyte>(outputType.Shape.ToValueArray());
			K230Kernels.DynamicGnneMatmul(inputA, inputB, tensor2.Buffer.Span, act, inABias, aBatch0, aBatch1, aRows, aCols, bBatch0, bBatch1, bCols, deqBBias, inAShiftBits, inBShiftBits, shiftBits, dynamicChannel);
			return tensor2;
		}
		if (dType == DataTypes.Int16)
		{
			Tensor<short> tensor3 = new Tensor<short>(outputType.Shape.ToValueArray());
			K230Kernels.DynamicGnneMatmul(inputA, inputB, tensor3.Buffer.Span, act, inABias, aBatch0, aBatch1, aRows, aCols, bBatch0, bBatch1, bCols, deqBBias, inAShiftBits, inBShiftBits, shiftBits, dynamicChannel);
			return tensor3;
		}
		if (dType == DataTypes.Float16)
		{
			Tensor<Half> tensor4 = new Tensor<Half>(outputType.Shape.ToValueArray());
			K230Kernels.DynamicGnneMatmul(inputA, inputB, tensor4.Buffer.Span, act, inABias, aBatch0, aBatch1, aRows, aCols, bBatch0, bBatch1, bCols, deqBBias, inAShiftBits, inBShiftBits, shiftBits, dynamicChannel);
			return tensor4;
		}
		if (dType == DataTypes.Float32)
		{
			Tensor<float> tensor5 = new Tensor<float>(outputType.Shape.ToValueArray());
			K230Kernels.DynamicGnneMatmul(inputA, inputB, tensor5.Buffer.Span, act, inABias, aBatch0, aBatch1, aRows, aCols, bBatch0, bBatch1, bCols, deqBBias, inAShiftBits, inBShiftBits, shiftBits, dynamicChannel);
			return tensor5;
		}
		throw new ArgumentOutOfRangeException("inputA");
	}

	private IValue Visit(Tensor inputA, Tensor inputB, Tensor<Half> act, Tensor<byte> inputABias, byte inputBBias, DynamicGNNEMatMul target, int shiftBits, int dynamicChannel)
	{
		int[] array = inputA.Shape.ToValueArray();
		int num = array.Length - 1;
		Shape shape = inputB.Shape;
		array[num] = shape[shape.Count - 1].FixedValue;
		TensorType outputType = new TensorType(target.OutputDType, array);
		int fixedValue = inputA.Shape[0].FixedValue;
		int fixedValue2 = inputA.Shape[1].FixedValue;
		int fixedValue3 = inputA.Shape[2].FixedValue;
		int fixedValue4 = inputA.Shape[3].FixedValue;
		int fixedValue5 = inputB.Shape[0].FixedValue;
		int fixedValue6 = inputB.Shape[1].FixedValue;
		int fixedValue7 = inputB.Shape[3].FixedValue;
		(DataType, DataType) tuple = (inputA.ElementType, inputB.ElementType);
		checked
		{
			Memory<Half> buffer;
			if (tuple.Item1 == DataTypes.UInt8 && tuple.Item2 == DataTypes.UInt8)
			{
				ReadOnlySpan<byte> inputA2 = inputA.BytesBuffer;
				ReadOnlySpan<byte> inputB2 = inputB.BytesBuffer;
				buffer = act.Buffer;
				return Value.FromTensor(GnneMatmulV2(inputA2, inputB2, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1));
			}
			(DataType, DataType) tuple2 = tuple;
			if (tuple2.Item1 == DataTypes.UInt8 && tuple2.Item2 == DataTypes.Int8)
			{
				ReadOnlySpan<byte> inputA3 = inputA.BytesBuffer;
				ReadOnlySpan<sbyte> inputB3 = MemoryMarshal.Cast<byte, sbyte>(inputB.BytesBuffer);
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA3, inputB3, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple3 = tuple;
			if (tuple3.Item1 == DataTypes.UInt8 && tuple3.Item2 == DataTypes.Int16)
			{
				ReadOnlySpan<byte> inputA4 = inputA.BytesBuffer;
				ReadOnlySpan<short> inputB4 = MemoryMarshal.Cast<byte, short>(inputB.BytesBuffer);
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA4, inputB4, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple4 = tuple;
			if (tuple4.Item1 == DataTypes.Int8 && tuple4.Item2 == DataTypes.UInt8)
			{
				ReadOnlySpan<sbyte> inputA5 = MemoryMarshal.Cast<byte, sbyte>(inputA.BytesBuffer);
				ReadOnlySpan<byte> inputB5 = inputB.BytesBuffer;
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA5, inputB5, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple5 = tuple;
			if (tuple5.Item1 == DataTypes.Int8 && tuple5.Item2 == DataTypes.Int8)
			{
				ReadOnlySpan<sbyte> inputA6 = MemoryMarshal.Cast<byte, sbyte>(inputA.BytesBuffer);
				ReadOnlySpan<sbyte> inputB6 = MemoryMarshal.Cast<byte, sbyte>(inputB.BytesBuffer);
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA6, inputB6, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple6 = tuple;
			if (tuple6.Item1 == DataTypes.Int8 && tuple6.Item2 == DataTypes.Int16)
			{
				ReadOnlySpan<sbyte> inputA7 = MemoryMarshal.Cast<byte, sbyte>(inputA.BytesBuffer);
				ReadOnlySpan<short> inputB7 = MemoryMarshal.Cast<byte, short>(inputB.BytesBuffer);
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA7, inputB7, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple7 = tuple;
			if (tuple7.Item1 == DataTypes.Int16 && tuple7.Item2 == DataTypes.UInt8)
			{
				ReadOnlySpan<short> inputA8 = MemoryMarshal.Cast<byte, short>(inputA.BytesBuffer);
				ReadOnlySpan<byte> inputB8 = inputB.BytesBuffer;
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA8, inputB8, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			(DataType, DataType) tuple8 = tuple;
			if (tuple8.Item1 == DataTypes.Int16 && tuple8.Item2 == DataTypes.Int8)
			{
				ReadOnlySpan<short> inputA9 = MemoryMarshal.Cast<byte, short>(inputA.BytesBuffer);
				ReadOnlySpan<sbyte> inputB9 = MemoryMarshal.Cast<byte, sbyte>(inputB.BytesBuffer);
				buffer = act.Buffer;
				return (TensorValue)GnneMatmulV2(inputA9, inputB9, outputType, buffer.Span, inputABias.Buffer.Span, fixedValue, fixedValue2, fixedValue3, fixedValue4, fixedValue5, fixedValue6, fixedValue7, inputBBias, 0, 0, (sbyte)shiftBits, dynamicChannel == 1);
			}
			throw new ArgumentOutOfRangeException($"Invalid Input A {inputA.ElementType} or Input B {inputB.ElementType}");
		}
	}

	private IRType Visit(TensorType inputA, TensorType inputB, DynamicGNNEMatMul target)
	{
		DataType dType = inputA.DType;
		DataType dType2 = inputB.DType;
		if (dType != DataTypes.Int8 && dType != DataTypes.UInt8 && dType != DataTypes.Int16)
		{
			return new InvalidType("Unsupported input_a_type, should be one of [int8, uint8, int16]");
		}
		if (dType2 != DataTypes.Int8 && dType2 != DataTypes.UInt8 && dType2 != DataTypes.Int16)
		{
			return new InvalidType("Unsupported input_b_type, should be one of [int8, uint8, int16]");
		}
		if (dType == DataTypes.Int16 && dType2 == DataTypes.Int16)
		{
			return new InvalidType("int16 for both of input_a_type and input_b_type is not supported");
		}
		if (target.OutputDType != DataTypes.Float16 && target.OutputDType != DataTypes.Float32)
		{
			return new InvalidType("Invalid Ouput Datatype");
		}
		Dimension[] array = new Dimension[2];
		Shape shape = inputA.Shape;
		array[0] = shape[shape.Count - 2];
		Shape shape2 = inputB.Shape;
		array[1] = shape2[shape2.Count - 1];
		Dimension[] second = array;
		Shape shape3 = ((inputA.Shape.Rank > inputB.Shape.Rank) ? inputA.Shape : inputB.Shape);
		return new TensorType(target.OutputDType, shape3.ToArray()[..(shape3.Count - 2)].Concat(second).ToArray());
	}

	public IValue Visit(IEvaluateContext context, DynamicGNNEMatMul target)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(target, DynamicGNNEMatMul.InputA);
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(target, DynamicGNNEMatMul.InputB);
		Tensor<Half> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<Half>(target, DynamicGNNEMatMul.Act);
		Tensor<byte> argumentValueAsTensor4 = context.GetArgumentValueAsTensor<byte>(target, DynamicGNNEMatMul.InputABias);
		byte argumentValueAsScalar = context.GetArgumentValueAsScalar<byte>(target, DynamicGNNEMatMul.InputBBias);
		int argumentValueAsScalar2 = context.GetArgumentValueAsScalar<int>(target, DynamicGNNEMatMul.ShiftBits);
		int argumentValueAsScalar3 = context.GetArgumentValueAsScalar<int>(target, DynamicGNNEMatMul.DynamicChannel);
		return Visit(argumentValueAsTensor, argumentValueAsTensor2, argumentValueAsTensor3, argumentValueAsTensor4, argumentValueAsScalar, target, argumentValueAsScalar2, argumentValueAsScalar3);
	}

	public IRType Visit(ITypeInferenceContext context, DynamicGNNEMatMul target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, DynamicGNNEMatMul.InputA);
		TensorType inputB = context.CheckArgumentType<TensorType>(target, DynamicGNNEMatMul.InputB);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.Text);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.InputA);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.InputB);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.InputABias);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.InputBBias);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.Act);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.ShiftBits);
		context.CheckArgumentType<IRType>(target, DynamicGNNEMatMul.DynamicChannel);
		return Visit(inputA, inputB, target);
	}
}
