using System;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.Quantization;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

public abstract class GNNEQuantRule : QuantRule
{
	private bool _isInt16Quant;

	public Expr Input => (Expr)base.MatchResult["input"];

	public Expr Output => (Expr)base.MatchResult["output"];

	public ValueRange<float> InputRange { get; private set; }

	public ValueRange<float> OutputRange { get; private set; }

	public int QuantBits
	{
		get
		{
			if (!(base.QuantType == DataTypes.Int16))
			{
				return 8;
			}
			return 12;
		}
	}

	public int WQuantBits
	{
		get
		{
			if (!(base.WQuantType == DataTypes.Int16))
			{
				return 8;
			}
			return 12;
		}
	}

	public ValueRange<float> GetValueRange(string name)
	{
		float[] array = ((TensorConst)base.MatchResult[name]).Value.ToArray<float>();
		return new ValueRange<float>(array[0], array[1]);
	}

	public override void Init()
	{
		InputRange = GetValueRange("inputRange");
		OutputRange = GetValueRange("outputRange");
	}

	public Expr Store(Expr callResult)
	{
		return Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Int8, callResult);
	}

	public Expr Load(Expr input)
	{
		if (input.CheckedType == null)
		{
			input.InferenceType();
		}
		_isInt16Quant = false;
		PrimType destType = (_isInt16Quant ? DataTypes.Int16 : DataTypes.Int8);
		if (base.ModelQuantMode == ModelQuantMode.UsePTQ)
		{
			QuantMode quantMode = ((!(base.QuantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			QuantParam quantParam = QuantUtility.GetQuantParam(InputRange, QuantBits, quantMode);
			Call input2 = Nncase.IR.F.Math.Quantize(input, quantParam, base.QuantType);
			GNNETypePatternUtility.GNNEGetDeqParams(input.CheckedShape[1].FixedValue, quantParam.Scale, quantParam.ZeroPoint);
			return Nncase.IR.K230.F.Tensors.GNNELoadIFDeq(destType, input2);
		}
		return Nncase.IR.K230.F.Tensors.GNNELoad(destType, input);
	}

	public Expr LoadW(Expr weights, ValueRange<float> weightsRange)
	{
		if (base.ModelQuantMode == ModelQuantMode.UsePTQ)
		{
			QuantMode quantMode = ((!(base.WQuantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			QuantParam quantParam = QuantUtility.GetQuantParam(weightsRange, WQuantBits, quantMode);
			Call input = Nncase.IR.F.Math.Quantize(weights, quantParam, base.WQuantType);
			return Nncase.IR.K230.F.Tensors.GNNELoadIFDeq(base.WQuantType, input);
		}
		return Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Int8, weights);
	}

	public Func<Expr, Expr> WithLoadStore(Func<Expr, Expr> inputCtor)
	{
		return Utility.Apply(WithLoadStoreImpl, inputCtor);
		Func<Expr, Expr> WithLoadStoreImpl(Func<Expr, Expr> inCtor)
		{
			Func<Expr, Expr> inCtor2 = inCtor;
			return (Expr input) => Store(inCtor2(Load(input)));
		}
	}

	public Func<Expr, Expr> WithFullLower(Func<Expr, Expr> inputCtor)
	{
		return GetReplaceHelper.WithTmpGNNEShape(WithLoadStore(inputCtor), Output.CheckedShape.ToValueArray());
	}
}
