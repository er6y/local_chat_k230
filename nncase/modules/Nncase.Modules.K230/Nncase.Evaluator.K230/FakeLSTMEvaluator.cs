#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class FakeLSTMEvaluator : IEvaluator<FakeLSTM>, IEvaluator, ITypeInferencer<FakeLSTM>, ITypeInferencer, ICostEvaluator<FakeLSTM>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, FakeLSTM target)
	{
		TensorType argumentType = context.GetArgumentType<TensorType>(target, FakeLSTM.Input);
		TensorType argumentType2 = context.GetArgumentType<TensorType>(target, FakeLSTM.WXc);
		TensorType argumentType3 = context.GetArgumentType<TensorType>(target, FakeLSTM.WRc);
		TupleType returnType = context.GetReturnType<TupleType>();
		return new Cost
		{
			[CostFactorNames.MemoryLoad] = CostUtility.GetMemoryAccess(argumentType) + CostUtility.GetMemoryAccess(argumentType2) + CostUtility.GetMemoryAccess(argumentType3),
			[CostFactorNames.MemoryStore] = returnType.Select((IRType t) => (t is TensorType type) ? CostUtility.GetMemoryAccess(type) : UInt128.One).Sum()
		};
	}

	public IValue Visit(IEvaluateContext context, FakeLSTM l)
	{
		Tensor tensor = context.GetArgumentValueAsTensor(l, FakeLSTM.Input);
		Tensor<float> argumentValueAsTensor = context.GetArgumentValueAsTensor<float>(l, FakeLSTM.WXc);
		Tensor<Half> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<Half>(l, FakeLSTM.ActXc);
		Tensor<float> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<float>(l, FakeLSTM.WRc);
		Tensor<Half> argumentValueAsTensor4 = context.GetArgumentValueAsTensor<Half>(l, FakeLSTM.ActRc);
		Tensor tensor2 = context.GetArgumentValueAsTensor(l, FakeLSTM.InitialH);
		Tensor<float> argumentValueAsTensor5 = context.GetArgumentValueAsTensor<float>(l, FakeLSTM.InitialC);
		Tensor<Half> argumentValueAsTensor6 = context.GetArgumentValueAsTensor<Half>(l, FakeLSTM.SegFittingParamFt);
		Tensor<Half> argumentValueAsTensor7 = context.GetArgumentValueAsTensor<Half>(l, FakeLSTM.SegFittingParamGt);
		int num = context.GetArgumentValueAsTensor<int>(l, FakeLSTM.OutputSize).ToArray()[0];
		Tensor<float> output = new Tensor<float>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		Tensor<float> outputH = new Tensor<float>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		Tensor<float> outputC = new Tensor<float>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		if (num >= 2)
		{
			outputH = new Tensor<float>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
		}
		if (num >= 3)
		{
			outputC = new Tensor<float>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
		}
		if (context.CurrentCall.EnodeBestQuantConfigWithCosine != null)
		{
			MarkerPattern markerPattern = Utility.IsRangeOfMarker(Utility.IsWildcard(), Utility.IsWildcard());
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[0]))
			{
				MixQuantInfo? mixQuantInfo = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo;
				if (mixQuantInfo != null && mixQuantInfo.HasBindedMixQuantInfo)
				{
					List<QuantParam> list = ((Marker)context.CurrentCall.Arguments[0]).MixQuantInfo?.QuantParameter;
					Trace.Assert(list.Count == 1);
					float[] array = tensor.ToArray<float>();
					for (int i = 0; i < array.Length; i++)
					{
						double num2 = (double)array[i] / (double)list[0].Scale + (double)list[0].ZeroPoint;
						if (list[0].Scale != 1f || list[0].ZeroPoint != 0)
						{
							num2 = System.Math.Round(num2);
						}
						double num3 = (num2 - (double)list[0].ZeroPoint) * (double)list[0].Scale;
						array[i] = (float)num3;
					}
					tensor = Value.FromTensor(Tensor.From(array, tensor.Shape)).AsTensor();
				}
			}
			if (markerPattern.MatchLeaf(context.CurrentCall.Arguments[5]))
			{
				MixQuantInfo? mixQuantInfo2 = ((Marker)context.CurrentCall.Arguments[5]).MixQuantInfo;
				if (mixQuantInfo2 != null && mixQuantInfo2.HasBindedMixQuantInfo && ((Tensor<float>)tensor2).ToScalar() == 0f)
				{
					List<QuantParam> list2 = ((Marker)context.CurrentCall.Arguments[5]).MixQuantInfo?.QuantParameter;
					Trace.Assert(list2.Count == 1);
					float[] array2 = tensor2.ToArray<float>();
					for (int j = 0; j < array2.Length; j++)
					{
						double num4 = (double)array2[j] / (double)list2[0].Scale + (double)list2[0].ZeroPoint;
						if (list2[0].Scale != 1f || list2[0].ZeroPoint != 0)
						{
							num4 = System.Math.Round(num4);
						}
						double num5 = (num4 - (double)list2[0].ZeroPoint) * (double)list2[0].Scale;
						array2[j] = (float)num5;
					}
					tensor2 = Value.FromTensor(Tensor.From(array2, tensor2.Shape)).AsTensor();
				}
			}
		}
		Tensor[] tensors = K230Kernels.FakeGnneLstm((Tensor<float>)tensor, argumentValueAsTensor, argumentValueAsTensor2, argumentValueAsTensor3, argumentValueAsTensor4, (Tensor<float>)tensor2, argumentValueAsTensor5, argumentValueAsTensor6, argumentValueAsTensor7, output, outputH, outputC, l.Direction, num).ToArray();
		return Value.FromTensors(tensors);
	}

	private IRType Visit(ITypeInferenceContext context, FakeLSTM target, TensorType input, TensorType initialH, TensorType initialC)
	{
		int numDirections = ((target.Direction != LSTMDirection.Bidirectional) ? 1 : 2);
		int seqLenIndex = 1;
		if (context.GetArgument(target, FakeLSTM.OutputSize) is TensorConst tensorConst)
		{
			TensorType tensorType = InferYType(context, target, input, seqLenIndex, numDirections);
			IRType[] subArray = (new TensorType[3] { tensorType, initialH, initialC })[..tensorConst.Value.ToScalar<int>()];
			return new TupleType(subArray);
		}
		return new InvalidType("LSTM OutputSize Must be known");
	}

	private TensorType InferYType(ITypeInferenceContext context, FakeLSTM target, TensorType x, int seqLenIndex, int numDirections)
	{
		List<Dimension> list = x.Shape.ToList();
		list.Insert(seqLenIndex + 1, numDirections);
		int fixedValue = context.GetArgument(target, FakeLSTM.WRc).CheckedShape[3].FixedValue;
		list.RemoveAt(0);
		list[list.Count - 1] = fixedValue;
		return x with
		{
			Shape = list.ToArray()
		};
	}

	public IRType Visit(ITypeInferenceContext context, FakeLSTM target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, FakeLSTM.Input);
		TensorType initialH = context.CheckArgumentType<TensorType>(target, FakeLSTM.InitialH);
		TensorType initialC = context.CheckArgumentType<TensorType>(target, FakeLSTM.InitialC);
		context.CheckArgumentType<IRType>(target, FakeLSTM.Input);
		context.CheckArgumentType<IRType>(target, FakeLSTM.WXc);
		context.CheckArgumentType<IRType>(target, FakeLSTM.ActXc);
		context.CheckArgumentType<IRType>(target, FakeLSTM.WRc);
		context.CheckArgumentType<IRType>(target, FakeLSTM.ActRc);
		context.CheckArgumentType<IRType>(target, FakeLSTM.InitialH);
		context.CheckArgumentType<IRType>(target, FakeLSTM.InitialC);
		context.CheckArgumentType<IRType>(target, FakeLSTM.SegFittingParamFt);
		context.CheckArgumentType<IRType>(target, FakeLSTM.SegFittingParamGt);
		context.CheckArgumentType<IRType>(target, FakeLSTM.HasStatic);
		context.CheckArgumentType<IRType>(target, FakeLSTM.OutputSize);
		return Visit(context, target, input, initialH, initialC);
	}
}
