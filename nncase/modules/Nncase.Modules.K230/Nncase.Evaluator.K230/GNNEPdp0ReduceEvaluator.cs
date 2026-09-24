using System;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEPdp0ReduceEvaluator : IEvaluator<GNNEPdp0Reduce>, IEvaluator, ITypeInferencer<GNNEPdp0Reduce>, ITypeInferencer, ICostEvaluator<GNNEPdp0Reduce>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEPdp0Reduce target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEPdp0Reduce p)
	{
		Tensor<Half> argumentValueAsTensor = context.GetArgumentValueAsTensor<Half>(p, GNNEPdp0Reduce.Input);
		int[] argumentValueAsArray = context.GetArgumentValueAsArray<int>(p, GNNEPdp0Reduce.Filter);
		int[] argumentValueAsArray2 = context.GetArgumentValueAsArray<int>(p, GNNEPdp0Reduce.Stride);
		Tensor<int> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<int>(p, GNNEPdp0Reduce.Padding);
		bool[] argumentValueAsArray3 = context.GetArgumentValueAsArray<bool>(p, GNNEPdp0Reduce.CountIncludePad);
		IValue argumentValue = context.GetArgumentValue(p, GNNEPdp0Reduce.DepuantParams);
		Tensor<Half> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<Half>(p, GNNEPdp0Reduce.Act);
		int argumentValueAsScalar = context.GetArgumentValueAsScalar<int>(p, GNNEPdp0Reduce.ShiftBits);
		Tensor<float> padValue = context.GetArgumentValue(p, GNNEPdp0Reduce.Value).AsTensor().Cast<float>();
		Tensor<float> tensor = new Tensor<float>(context.CurrentCall.CheckedShape.ToValueArray());
		(Func<float, float, float>, Func<float, int, float>) tuple = p.ReduceOp switch
		{
			PU_PDP0_MODE.average => ((float a, float b) => a + b, (float v, int k) => v / (float)k), 
			PU_PDP0_MODE.min => ((float a, float b) => System.Math.Min(a, b), (float v, int _) => v), 
			PU_PDP0_MODE.max => ((float a, float b) => System.Math.Max(a, b), (float v, int _) => v), 
			PU_PDP0_MODE.sum => ((float a, float b) => a + b, (float v, int _) => v), 
			_ => throw new ArgumentOutOfRangeException("context"), 
		};
		Pdp0Impl(argumentValueAsTensor, argumentValueAsTensor3.Buffer.Span, tensor, argumentValueAsArray[0], argumentValueAsArray[1], argumentValueAsArray2[0], argumentValueAsArray2[1], (Before: argumentValueAsTensor2[new int[2]], After: argumentValueAsTensor2[new int[2] { 0, 1 }]), (Before: argumentValueAsTensor2[new int[2] { 1, 0 }], After: argumentValueAsTensor2[new int[2] { 1, 1 }]), checked((sbyte)argumentValueAsScalar), tuple.Item1, tuple.Item2, argumentValueAsArray3[0], (argumentValue is NoneValue) ? new DeQuantizeParam(0, 1f) : argumentValue.AsTensor().ToScalar<DeQuantizeParam>(), padValue);
		if (p.DestType == DataTypes.Int8)
		{
			return Value.FromTensor(tensor.Cast<sbyte>(CastMode.KDefault));
		}
		if (p.DestType == DataTypes.UInt8)
		{
			return Value.FromTensor(tensor.Cast<byte>(CastMode.KDefault));
		}
		if (p.DestType == DataTypes.Int16)
		{
			return Value.FromTensor(tensor.Cast<short>(CastMode.KDefault));
		}
		return Value.FromTensor(tensor.Cast<Half>(CastMode.KDefault));
	}

	private void Pdp0Impl(Tensor<Half> input, ReadOnlySpan<Half> act, Tensor<float> output, int filterH, int filterW, int strideH, int strideW, (int Before, int After) paddingH, (int Before, int After) paddingW, sbyte shiftBits, Func<float, float, float> binaryOp, Func<float, int, float> windowOp, bool countIncludePad, DeQuantizeParam deQuantizeParam, Tensor<float> padValue)
	{
		int[] array = input.Shape.ToValueArray();
		int windowedOutputSize = TypePatternUtility.GetWindowedOutputSize(array[2] + paddingH.Before + paddingH.After, filterH, strideH, 1, same: false);
		int windowedOutputSize2 = TypePatternUtility.GetWindowedOutputSize(array[3] + paddingW.Before + paddingW.After, filterW, strideW, 1, same: false);
		for (int i = 0; i < array[0]; i++)
		{
			for (int j = 0; j < array[1]; j++)
			{
				for (int k = 0; k < windowedOutputSize; k++)
				{
					for (int l = 0; l < windowedOutputSize2; l++)
					{
						int num = k * strideH - paddingH.Before;
						int num2 = l * strideW - paddingW.Before;
						int num3 = System.Math.Max(0, -num);
						int num4 = System.Math.Min(filterH, array[2] - num);
						int num5 = System.Math.Max(0, -num2);
						int num6 = System.Math.Min(filterW, array[3] - num2);
						float num7 = (float)input[new int[4]
						{
							i,
							j,
							num + num3,
							num2 + num5
						}];
						int num8 = 0;
						for (int m = num3; m < num4; m++)
						{
							for (int n = num5; n < num6; n++)
							{
								int num9 = num + m;
								int num10 = num2 + n;
								float num11 = (float)input[new int[4] { i, j, num9, num10 }];
								if (m != num3 || n != num5)
								{
									num7 = binaryOp(num7 - (float)deQuantizeParam.ZeroPoint, num11 - (float)deQuantizeParam.ZeroPoint);
								}
								num8++;
							}
						}
						if (countIncludePad)
						{
							for (int num12 = 0; num12 < filterH * filterW - num8; num12++)
							{
								num7 = binaryOp(num7, padValue.GetValue(0));
							}
							num8 = filterH * filterW;
						}
						Half half = (Half)num8;
						float value = windowOp(num7 * deQuantizeParam.Scale, (int)half);
						value = K230Kernels.ApplyAct0(value, act, j, shiftBits);
						output[new int[4] { i, j, k, l }] = value;
					}
				}
			}
		}
	}

	private IRType Visit(ITypeInferenceContext context, GNNEPdp0Reduce target, TensorType input)
	{
		Expr[] arguments = context.GetArguments(target, GNNEPdp0Reduce.Filter, GNNEPdp0Reduce.Stride, GNNEPdp0Reduce.Padding);
		IRType iRType = TypeInference.ReduceWindow2DType(input, arguments[0], arguments[1], arguments[2], false);
		return new TensorType(target.DestType, ((TensorType)iRType).Shape);
	}

	public IRType Visit(ITypeInferenceContext context, GNNEPdp0Reduce target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEPdp0Reduce.Input);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Input);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Filter);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Stride);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Padding);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.DepuantParams);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Value);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.ShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.CountIncludePad);
		context.CheckArgumentType<IRType>(target, GNNEPdp0Reduce.Act);
		return Visit(context, target, input);
	}
}
