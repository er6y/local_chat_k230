using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using OrtKISharp;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public sealed class GNNEMatMulEvaluator : IEvaluator<GNNEMatMul>, IEvaluator, ITypeInferencer<GNNEMatMul>, ITypeInferencer, ICostEvaluator<GNNEMatMul>, ICostEvaluator
{
	public IValue Visit(IEvaluateContext context, GNNEMatMul matMul)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(matMul, GNNEMatMul.InputA);
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(matMul, GNNEMatMul.InputB);
		byte[] inputABias = context.GetArgumentValueAsTensor<byte>(matMul, GNNEMatMul.InputABias).ToArray();
		byte[] deqBBias = context.GetArgumentValueAsTensor<byte>(matMul, GNNEMatMul.DeqBBias).ToArray();
		Tensor<Half> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<Half>(matMul, GNNEMatMul.Act);
		Tensor argumentValueAsTensor4 = context.GetArgumentValueAsTensor(matMul, GNNEMatMul.ShiftBits);
		int fixedValue = argumentValueAsTensor.Shape[1].FixedValue;
		int fixedValue2 = argumentValueAsTensor2.Shape[1].FixedValue;
		int aRows = argumentValueAsTensor.Shape[2].FixedValue;
		int aCols = argumentValueAsTensor.Shape[3].FixedValue;
		int bCols = argumentValueAsTensor2.Shape[3].FixedValue;
		int[] array = argumentValueAsTensor.Shape.ToValueArray();
		int num = array.Length - 1;
		Shape shape = argumentValueAsTensor2.Shape;
		array[num] = shape[shape.Count - 1].FixedValue;
		TensorType tensorType = new TensorType(matMul.OutputDType, array);
		float[] inputADeq = argumentValueAsTensor.ToArray<float>();
		float[] inputBDeq = argumentValueAsTensor2.ToArray<float>();
		if (fixedValue == fixedValue2 || (fixedValue < fixedValue2 && fixedValue == 1))
		{
			inputADeq.Select((float _, int i) => inputADeq[i] -= (int)inputABias[i * aRows / (aRows * aCols * bCols)]).ToArray();
			inputBDeq.Select((float _, int i) => inputBDeq[i] -= (int)deqBBias[i / (aRows * aCols * bCols)]).ToArray();
		}
		else
		{
			if (fixedValue <= fixedValue2 || fixedValue2 != 1)
			{
				throw new InvalidOleVariantTypeException("Invalid matmul");
			}
			inputADeq.Select((float _, int i) => inputADeq[i] -= (int)inputABias.ToArray()[i / aRows]).ToArray();
			inputBDeq.Select((float _, int i) => inputBDeq[i] -= (int)deqBBias[0]).ToArray();
		}
		Tensor tensor = OrtKI.MatMul(OrtKISharp.Tensor.MakeTensor(inputADeq, ((IEnumerable<int>)argumentValueAsTensor.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray()), OrtKISharp.Tensor.MakeTensor(inputBDeq, ((IEnumerable<int>)argumentValueAsTensor2.Dimensions.ToArray()).Select((Func<int, long>)((int i) => i)).ToArray())).ToTensor();
		float[] array2 = tensor.ToArray<float>();
		float[] array3 = new float[K230Kernels.ComputeSize(tensor.Shape)];
		for (int j = 0; j < array2.Length; j++)
		{
			array3[j] = K230Kernels.ApplyAct0(array2[j], argumentValueAsTensor3.ToArray<Half>(), j / tensor.Shape[3].FixedValue, argumentValueAsTensor4.ToArray<sbyte>()[0]);
		}
		float[] array4 = array3.Select((float x) => (float)System.Math.Round(x)).ToArray();
		Half[] array5 = array3.Select((float x) => (Half)x).ToArray();
		Tensor<float> tensor2 = Tensor.From(array4, tensor.Shape);
		Tensor<Half> tensor3 = Tensor.From(array5, tensor.Shape);
		if (tensorType == DataTypes.UInt8)
		{
			return Value.FromTensor(tensor2.Cast<byte>(CastMode.KDefault));
		}
		if (tensorType == DataTypes.Int8)
		{
			return Value.FromTensor(tensor2.Cast<sbyte>(CastMode.KDefault));
		}
		if (tensorType == DataTypes.Int16)
		{
			return Value.FromTensor(tensor2.Cast<short>(CastMode.KDefault));
		}
		return Value.FromTensor(tensor3.Cast<Half>(CastMode.KDefault));
	}

	public Cost Visit(ICostEvaluateContext context, GNNEMatMul target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	private IRType Visit(TensorType inputA, TensorType inputB, GNNEMatMul target)
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
		Shape shape3 = new Shape(from t in inputA.Shape.Zip(inputB.Shape)
			select System.Math.Max(t.First.FixedValue, t.Second.FixedValue));
		return new TensorType(target.OutputDType, shape3.ToArray()[..(shape3.Count - 2)].Concat(second).ToArray());
	}

	public IRType Visit(ITypeInferenceContext context, GNNEMatMul target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, GNNEMatMul.InputA);
		TensorType inputB = context.CheckArgumentType<TensorType>(target, GNNEMatMul.InputB);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.InputA);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.InputB);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.Act);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.InputABias);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.InAShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.InBShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.ShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEMatMul.DeqBBias);
		return Visit(inputA, inputB, target);
	}
}
