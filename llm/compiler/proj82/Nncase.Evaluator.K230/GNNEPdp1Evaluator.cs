using System;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.TIR.Instructions;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNEPdp1Evaluator : IEvaluator<GNNEPdp1>, IEvaluator, ITypeInferencer<GNNEPdp1>, ITypeInferencer, ICostEvaluator<GNNEPdp1>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNEPdp1 target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNEPdp1 r)
	{
		Tensor argumentValueAsTensor = context.GetArgumentValueAsTensor(r, GNNEPdp1.Input);
		int[] argumentValueAsArray = context.GetArgumentValueAsArray<int>(r, GNNEPdp1.Filter);
		int[] argumentValueAsArray2 = context.GetArgumentValueAsArray<int>(r, GNNEPdp1.Stride);
		Tensor<int> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<int>(r, GNNEPdp1.Padding);
		bool[] argumentValueAsArray3 = context.GetArgumentValueAsArray<bool>(r, GNNEPdp1.CountIncludePad);
		IValue argumentValue = context.GetArgumentValue(r, GNNEPdp1.QuantParams);
		IValue argumentValue2 = context.GetArgumentValue(r, GNNEPdp1.DequantParams);
		Tensor<Half> padValue = context.GetArgumentValue(r, GNNEPdp1.Value).AsTensor().Cast<Half>();
		Tensor<float> tensor = new Tensor<float>(context.CurrentCall.CheckedShape.ToValueArray());
		MFU_PDP_OP reduceOp = r.ReduceOp;
		if (reduceOp <= MFU_PDP_OP.SUM)
		{
			(Func<float, float, float>, Func<float, int, float>) tuple;
			(Func<float, float, float>, Func<float, int, float>) tuple2;
			switch (reduceOp)
			{
			case MFU_PDP_OP.AVERAGE:
				tuple = ((float a, float b) => a + b, (float v, int k) => v / (float)k);
				goto IL_01eb;
			case MFU_PDP_OP.MIN:
				tuple = ((float a, float b) => System.Math.Min(a, b), (float v, int _) => v);
				goto IL_01eb;
			case MFU_PDP_OP.MAX:
				tuple = ((float a, float b) => System.Math.Max(a, b), (float v, int _) => v);
				goto IL_01eb;
			case MFU_PDP_OP.SUM:
				{
					tuple = ((float a, float b) => a + b, (float v, int _) => v);
					goto IL_01eb;
				}
				IL_01eb:
				tuple2 = tuple;
				Pdp1Impl(argumentValueAsTensor, tensor, r.DestType, argumentValueAsArray[0], argumentValueAsArray[1], argumentValueAsArray2[0], argumentValueAsArray2[1], (Before: argumentValueAsTensor2[new int[2]], After: argumentValueAsTensor2[new int[2] { 0, 1 }]), (Before: argumentValueAsTensor2[new int[2] { 1, 0 }], After: argumentValueAsTensor2[new int[2] { 1, 1 }]), tuple2.Item1, tuple2.Item2, argumentValueAsArray3[0], (argumentValue is NoneValue) ? null : argumentValue.AsTensor(), (argumentValue2 is NoneValue) ? null : argumentValue2.AsTensor(), padValue);
				if (r.DestType == DataTypes.Int8)
				{
					return Value.FromTensor(tensor.Cast<sbyte>(CastMode.KDefault));
				}
				if (r.DestType == DataTypes.UInt8)
				{
					return Value.FromTensor(tensor.Cast<byte>(CastMode.KDefault));
				}
				if (r.DestType == DataTypes.Int16)
				{
					return Value.FromTensor(tensor.Cast<short>(CastMode.KDefault));
				}
				return Value.FromTensor(tensor.Cast<Half>(CastMode.KDefault));
			}
		}
		throw new NotSupportedException();
	}

	private void Pdp1Impl(Tensor input, Tensor output, DataType destType, int filterH, int filterW, int strideH, int strideW, (int Before, int After) paddingH, (int Before, int After) paddingW, Func<float, float, float> binaryOp, Func<float, int, float> windowOp, bool countIncludePad, Tensor quantizeParam = null, Tensor deQuantizeParam = null, Tensor<Half> padValue = null)
	{
		int[] array = input.Shape.ToValueArray();
		DataType elementType = input.ElementType;
		input = input.Cast<float>();
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
						if (elementType != DataTypes.Float16 && deQuantizeParam != null)
						{
							num7 = (num7 - (float)deQuantizeParam.ToArray<DeQuantizeParam>()[0].ZeroPoint) * deQuantizeParam.ToArray<DeQuantizeParam>()[0].Scale;
						}
						int num8 = 0;
						for (int m = num3; m < num4; m++)
						{
							for (int n = num5; n < num6; n++)
							{
								int num9 = num + m;
								int num10 = num2 + n;
								float num11 = (float)input[new int[4] { i, j, num9, num10 }];
								if (elementType != DataTypes.Float16 && deQuantizeParam != null)
								{
									num11 = (num11 - (float)deQuantizeParam.ToArray<DeQuantizeParam>()[0].ZeroPoint) * deQuantizeParam.ToArray<DeQuantizeParam>()[0].Scale;
								}
								if (m != num3 || n != num5)
								{
									num7 = binaryOp(num7, num11);
								}
								num8++;
							}
						}
						if (countIncludePad)
						{
							for (int num12 = 0; num12 < filterH * filterW - num8; num12++)
							{
								num7 = binaryOp(num7, (float)padValue.GetValue(0));
							}
							num8 = filterH * filterW;
						}
						int arg = num8;
						float num13 = windowOp(num7, arg);
						if (destType == DataTypes.Float16)
						{
							output[new int[4] { i, j, k, l }] = num13;
						}
						else if (destType == DataTypes.Int8)
						{
							output[new int[4] { i, j, k, l }] = System.Math.Clamp((sbyte)System.Math.Round(num13 * quantizeParam.ToArray<QuantizeParam>()[0].Scale + (float)quantizeParam.ToArray<QuantizeParam>()[0].ZeroPoint), (sbyte)-127, sbyte.MaxValue);
						}
						else if (destType == DataTypes.Int16)
						{
							output[new int[4] { i, j, k, l }] = System.Math.Clamp((short)System.Math.Round(num13 * quantizeParam.ToArray<QuantizeParam>()[0].Scale + (float)quantizeParam.ToArray<QuantizeParam>()[0].ZeroPoint), (short)(-32767), short.MaxValue);
						}
						else if (destType == DataTypes.UInt8)
						{
							output[new int[4] { i, j, k, l }] = System.Math.Clamp((byte)System.Math.Round(num13 * quantizeParam.ToArray<QuantizeParam>()[0].Scale + (float)quantizeParam.ToArray<QuantizeParam>()[0].ZeroPoint), (byte)0, byte.MaxValue);
						}
					}
				}
			}
		}
	}

	private IRType Visit(ITypeInferenceContext context, GNNEPdp1 target, TensorType input)
	{
		Expr[] arguments = context.GetArguments(target, GNNEPdp1.Filter, GNNEPdp1.Stride, GNNEPdp1.Padding);
		IRType iRType = TypeInference.ReduceWindow2DType(input, arguments[0], arguments[1], arguments[2], false);
		return new TensorType(target.DestType, ((TensorType)iRType).Shape);
	}

	public IRType Visit(ITypeInferenceContext context, GNNEPdp1 target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNEPdp1.Input);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.Input);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.Filter);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.Stride);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.Padding);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.QuantParams);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.DequantParams);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.Value);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.ShiftBits);
		context.CheckArgumentType<IRType>(target, GNNEPdp1.CountIncludePad);
		return Visit(context, target, input);
	}
}
