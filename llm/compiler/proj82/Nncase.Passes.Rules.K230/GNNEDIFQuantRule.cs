using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230.F;
using Nncase.Quantization;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

public abstract class GNNEDIFQuantRule : QuantRule
{
	public Expr InputA => (Expr)base.MatchResult["inputA"];

	public Expr InputB => (Expr)base.MatchResult["inputB"];

	public Expr Output => (Expr)base.MatchResult["output"];

	public ValueRange<float> InputARange { get; private set; }

	public ValueRange<float> InputBRange { get; private set; }

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

	public ValueRange<float> GetValueRange(string name)
	{
		if (base.MatchResult.GetValueOrDefault(name) is TensorConst tensorConst)
		{
			float[] array = tensorConst.Value.ToArray<float>();
			return new ValueRange<float>(array[0], array[1]);
		}
		return new ValueRange<float>(0f, 0f);
	}

	public override void Init()
	{
		InputARange = GetValueRange("inputARange");
		InputBRange = GetValueRange("inputBRange");
	}

	public Expr Store(Expr callResult)
	{
		return Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Int8, callResult);
	}

	public Expr Load(Expr input, string name)
	{
		if (input.CheckedType == null)
		{
			input.InferenceType();
		}
		if (base.ModelQuantMode == ModelQuantMode.UsePTQ)
		{
			QuantMode quantMode = ((!(base.QuantType == DataTypes.UInt8)) ? QuantMode.SignedSymmetricMode : QuantMode.UnsignedMode);
			QuantParam quantParam = QuantUtility.GetQuantParam((name == "inputA") ? InputARange : InputBRange, QuantBits, quantMode);
			Call input2 = Math.Quantize(input, quantParam, base.QuantType);
			return Nncase.IR.K230.F.Tensors.GNNELoadIFDeq(base.QuantType, input2);
		}
		return Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)base.QuantType, input);
	}
}
