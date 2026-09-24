using System;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEActivationEvaluator : IEvaluator<GNNEActivation>, IEvaluator, ITypeInferencer<GNNEActivation>, ITypeInferencer, ICostEvaluator<GNNEActivation>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEActivation target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEActivation a)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(a, GNNEActivation.InputA);
		IValue argumentValue = context.GetArgumentValue(a, GNNEActivation.InputB);
		bool flag = argumentValue is NoneValue;
		int[] array = argumentValueAsTensor.Shape.ToValueArray();
		if (!flag)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = System.Math.Max(array[i], argumentValue.AsTensor().Shape.ToValueArray()[i]);
			}
		}
		Tensor argumentValueAsTensor2 = context.GetArgumentValueAsTensor(a, GNNEActivation.Act);
		bool[] array2 = context.GetArgumentValueAsTensor(a, GNNEActivation.Is16Segments).ToArray<bool>();
		int[] array3 = context.GetArgumentValueAsTensor(a, GNNEActivation.OutChannels).ToArray<int>();
		IValue argumentValue2 = context.GetArgumentValue(a, GNNEActivation.DeqAParams);
		IValue argumentValue3 = context.GetArgumentValue(a, GNNEActivation.DeqBParams);
		return Value.FromConst(K230Kernels.Gnne_activation(outputShape: context.CurrentCall.CheckedShape, is16Segments: array2[0], inputa: argumentValueAsTensor, inputb: flag ? new Tensor<Half>(0) : argumentValue.AsTensor(), has_uninitailized_input: flag, actData: argumentValueAsTensor2.ToArray<float>(), outChannels: array3[0], deQuantizeParamA: (argumentValue2 is NoneValue) ? new DeQuantizeParam(0, 1f) : ((DeQuantizeParam)argumentValue2.AsTensor()[new int[1]]), deQuantizeParamB: (argumentValue3 is NoneValue) ? new DeQuantizeParam(0, 1f) : ((DeQuantizeParam)argumentValue3.AsTensor()[new int[1]]), actType: a.Type, outputType: new TensorType(a.OutputDType, array)));
	}

	private IRType Visit(GNNEActivation target, TensorType inputA, IRType inputB)
	{
		TensorType result = new TensorType(target.OutputDType, target.OutputShape.ToArray());
		if (!(inputB is NoneType))
		{
			int[] array = inputA.Shape.ToValueArray();
			TensorType tensorType = (TensorType)inputB;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = System.Math.Max(array[i], tensorType.Shape.ToValueArray()[i]);
			}
			result = new TensorType(target.OutputDType, array);
		}
		if (inputB is NoneType)
		{
			(TensorType, IRType) tuple = (inputA, inputB);
			if (tuple.Item1.DType == DataTypes.UInt8 || tuple.Item1.DType == DataTypes.Int8 || tuple.Item1.DType == DataTypes.Float16 || tuple.Item1.DType == DataTypes.Int16)
			{
				return result;
			}
			return new InvalidType("No support the act's datatype");
		}
		(TensorType, TensorType) tuple2 = (inputA, inputB as TensorType);
		if ((tuple2.Item1.DType == DataTypes.UInt8 || tuple2.Item1.DType == DataTypes.Int8 || tuple2.Item1.DType == DataTypes.Float16 || tuple2.Item1.DType == DataTypes.Int16) && (inputB is NoneType || tuple2.Item2.DType == DataTypes.UInt8 || tuple2.Item2.DType == DataTypes.Int8 || tuple2.Item2.DType == DataTypes.Float16))
		{
			return result;
		}
		return new InvalidType("No support the act's datatype");
	}

	public IRType Visit(ITypeInferenceContext context, GNNEActivation target)
	{
		TensorType inputA = context.CheckArgumentType<TensorType>(target, GNNEActivation.InputA);
		IRType inputB = context.CheckArgumentType<IRType>(target, GNNEActivation.InputB);
		context.CheckArgumentType<IRType>(target, GNNEActivation.InputA);
		context.CheckArgumentType<IRType>(target, GNNEActivation.InputB);
		context.CheckArgumentType<IRType>(target, GNNEActivation.Act);
		context.CheckArgumentType<IRType>(target, GNNEActivation.InAShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEActivation.InBShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEActivation.OutShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEActivation.DeqAParams);
		context.CheckArgumentType<IRType>(target, GNNEActivation.DeqBParams);
		context.CheckArgumentType<IRType>(target, GNNEActivation.OutChannels);
		context.CheckArgumentType<IRType>(target, GNNEActivation.Is16Segments);
		return Visit(target, inputA, inputB);
	}
}
