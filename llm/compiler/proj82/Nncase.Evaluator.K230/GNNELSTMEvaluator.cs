using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.CostModel;
using Nncase.IR;
using Nncase.IR.K230;

namespace Nncase.Evaluator.K230;

[TypeInferGenerator]
public class GNNELSTMEvaluator : IEvaluator<GNNELSTM>, IEvaluator, ITypeInferencer<GNNELSTM>, ITypeInferencer, ICostEvaluator<GNNELSTM>, ICostEvaluator
{
	public Cost Visit(ICostEvaluateContext context, GNNELSTM target)
	{
		return new Cost { [CostFactorNames.CPUCycles] = (byte)1 };
	}

	public IValue Visit(IEvaluateContext context, GNNELSTM l)
	{
		Tensor<float> argumentValueAsTensor = context.GetArgumentValueAsTensor<float>(l, GNNELSTM.Input);
		Tensor<float> argumentValueAsTensor2 = context.GetArgumentValueAsTensor<float>(l, GNNELSTM.WXc);
		Tensor<Half> argumentValueAsTensor3 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.ActXc);
		Tensor<float> argumentValueAsTensor4 = context.GetArgumentValueAsTensor<float>(l, GNNELSTM.WRc);
		Tensor<Half> argumentValueAsTensor5 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.ActRc0);
		Tensor<Half> argumentValueAsTensor6 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.ActRc1);
		Tensor<float> argumentValueAsTensor7 = context.GetArgumentValueAsTensor<float>(l, GNNELSTM.InitialH);
		Tensor<float> argumentValueAsTensor8 = context.GetArgumentValueAsTensor<float>(l, GNNELSTM.InitialC);
		Tensor<Half> argumentValueAsTensor9 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.SegFittingParamFt);
		Tensor<Half> argumentValueAsTensor10 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.SegFittingParamGt);
		Tensor<Half> argumentValueAsTensor11 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.WXcQarg);
		Tensor<Half> argumentValueAsTensor12 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.WRcQarg);
		Tensor<Half> argumentValueAsTensor13 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.ActBin);
		Tensor<Half> argumentValueAsTensor14 = context.GetArgumentValueAsTensor<Half>(l, GNNELSTM.ActBinQ);
		Tensor<int> argumentValueAsTensor15 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.IfDeqBias);
		Tensor<int> argumentValueAsTensor16 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.XcShiftBits);
		Tensor<int> argumentValueAsTensor17 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.HDeqBias0);
		Tensor<int> argumentValueAsTensor18 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.HDeqBias1);
		Tensor<int> argumentValueAsTensor19 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.RcShiftBits0);
		Tensor<int> argumentValueAsTensor20 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.RcShiftBits1);
		Tensor<int> argumentValueAsTensor21 = context.GetArgumentValueAsTensor<int>(l, GNNELSTM.OutputSize);
		Tensor outputC = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		if (argumentValueAsTensor21.ToArray()[0] >= 2)
		{
			outputC = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 3)
		{
			outputC = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
		}
		Tensor output = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		if (l.DestTypeO == DataTypes.UInt8)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				output = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				output = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				output = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (l.DestTypeO == DataTypes.Int8)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				output = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				output = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				output = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (l.DestTypeO == DataTypes.Int16)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				output = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				output = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				output = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 1)
		{
			output = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 2)
		{
			output = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 3)
		{
			output = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
		}
		Tensor outputH = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		if (l.DestTypeOH == DataTypes.UInt8)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				outputH = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				outputH = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				outputH = new Tensor<byte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (l.DestTypeOH == DataTypes.Int8)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				outputH = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				outputH = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				outputH = new Tensor<sbyte>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (l.DestTypeOH == DataTypes.Int16)
		{
			if (argumentValueAsTensor21.ToArray()[0] >= 1)
			{
				outputH = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 2)
			{
				outputH = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
			}
			else if (argumentValueAsTensor21.ToArray()[0] >= 3)
			{
				outputH = new Tensor<short>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
			}
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 1)
		{
			outputH = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[0]).Shape.ToValueArray());
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 2)
		{
			outputH = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[1]).Shape.ToValueArray());
		}
		else if (argumentValueAsTensor21.ToArray()[0] >= 3)
		{
			outputH = new Tensor<Half>(((TensorType)((TupleType)context.CurrentCall.CheckedType)[2]).Shape.ToValueArray());
		}
		return Value.FromTensors(K230Kernels.GnneLstmImpl(l.DestTypeO, l.DestTypeOH, argumentValueAsTensor, argumentValueAsTensor2, argumentValueAsTensor3, argumentValueAsTensor4, argumentValueAsTensor5, argumentValueAsTensor6, argumentValueAsTensor7, argumentValueAsTensor8, argumentValueAsTensor9, argumentValueAsTensor10, output, outputH, outputC, l.Direction, argumentValueAsTensor11, argumentValueAsTensor12, argumentValueAsTensor13, argumentValueAsTensor14, argumentValueAsTensor15[new int[1]], argumentValueAsTensor17[new int[1]], argumentValueAsTensor18[new int[1]], argumentValueAsTensor16[new int[1]], argumentValueAsTensor19[new int[1]], argumentValueAsTensor20[new int[1]], argumentValueAsTensor21.ToArray()[0]).ToArray());
	}

	private IRType Visit(ITypeInferenceContext context, GNNELSTM target, TensorType input, TensorType initialH, TensorType initialC)
	{
		int numDirections = ((target.Direction != LSTMDirection.Bidirectional) ? 1 : 2);
		int seqLenIndex = 1;
		if (context.GetArgument(target, GNNELSTM.OutputSize) is TensorConst tensorConst)
		{
			InferYType(context, target, input, seqLenIndex, numDirections);
			Shape shape = new Dimension[4]
			{
				input.Shape[1],
				initialH.Shape[1],
				initialH.Shape[2],
				initialH.Shape[3]
			};
			IRType[] subArray = (new TensorType[3]
			{
				new TensorType(target.DestTypeO, shape),
				new TensorType(target.DestTypeOH, new Dimension[4]
				{
					1,
					initialH.Shape[1],
					initialH.Shape[2],
					initialH.Shape[3]
				}),
				new TensorType(DataTypes.Float16, new Dimension[4]
				{
					1,
					initialC.Shape[1],
					initialC.Shape[2],
					initialC.Shape[3]
				})
			})[..tensorConst.Value.ToScalar<int>()];
			return new TupleType(subArray);
		}
		return new InvalidType("LSTM OutputSize Must be known");
	}

	private TensorType InferYType(ITypeInferenceContext context, GNNELSTM target, TensorType x, int seqLenIndex, int numDirections)
	{
		List<Dimension> list = x.Shape.ToList();
		list.Insert(seqLenIndex + 1, numDirections);
		int fixedValue = context.GetArgument(target, GNNELSTM.WRc).CheckedShape[3].FixedValue;
		list.RemoveAt(0);
		list[list.Count - 1] = fixedValue;
		return x with
		{
			Shape = list.ToArray()
		};
	}

	public IRType Visit(ITypeInferenceContext context, GNNELSTM target)
	{
		TensorType input = context.CheckArgumentType<TensorType>(target, GNNELSTM.Input);
		TensorType initialH = context.CheckArgumentType<TensorType>(target, GNNELSTM.InitialH);
		TensorType initialC = context.CheckArgumentType<TensorType>(target, GNNELSTM.InitialC);
		context.CheckArgumentType<IRType>(target, GNNELSTM.Input);
		context.CheckArgumentType<IRType>(target, GNNELSTM.WXc);
		context.CheckArgumentType<IRType>(target, GNNELSTM.ActXc);
		context.CheckArgumentType<IRType>(target, GNNELSTM.WRc);
		context.CheckArgumentType<IRType>(target, GNNELSTM.ActRc0);
		context.CheckArgumentType<IRType>(target, GNNELSTM.ActRc1);
		context.CheckArgumentType<IRType>(target, GNNELSTM.InitialH);
		context.CheckArgumentType<IRType>(target, GNNELSTM.InitialC);
		context.CheckArgumentType<IRType>(target, GNNELSTM.SegFittingParamFt);
		context.CheckArgumentType<IRType>(target, GNNELSTM.SegFittingParamGt);
		context.CheckArgumentType<IRType>(target, GNNELSTM.WXcQarg);
		context.CheckArgumentType<IRType>(target, GNNELSTM.WRcQarg);
		context.CheckArgumentType<IRType>(target, GNNELSTM.ActBin);
		context.CheckArgumentType<IRType>(target, GNNELSTM.ActBinQ);
		context.CheckArgumentType<IRType>(target, GNNELSTM.IfDeqBias);
		context.CheckArgumentType<IRType>(target, GNNELSTM.XcShiftBits);
		context.CheckArgumentType<IRType>(target, GNNELSTM.HDeqBias0);
		context.CheckArgumentType<IRType>(target, GNNELSTM.HDeqBias1);
		context.CheckArgumentType<IRType>(target, GNNELSTM.CShiftBits);
		context.CheckArgumentType<IRType>(target, GNNELSTM.RcShiftBits0);
		context.CheckArgumentType<IRType>(target, GNNELSTM.RcShiftBits1);
		context.CheckArgumentType<IRType>(target, GNNELSTM.OutHShiftBits);
		context.CheckArgumentType<IRType>(target, GNNELSTM.OutCShiftBits);
		context.CheckArgumentType<IRType>(target, GNNELSTM.HasStatic);
		context.CheckArgumentType<IRType>(target, GNNELSTM.OutputSize);
		return Visit(context, target, input, initialH, initialC);
	}
}
