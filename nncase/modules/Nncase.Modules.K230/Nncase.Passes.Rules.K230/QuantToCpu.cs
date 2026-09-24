using System;
using Nncase.IR;
using Nncase.IR.Math;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules.K230;

// Rewrite small standalone Quantize nodes into primitive CPU (stackvm) ops
// (mul + round + clamp + add + cast) so the K230 fusion never turns them into
// single-op ACT1 regions. Decode-sized tensors pay a large fixed per-region
// cost on the board (~2.4ms); the same work costs ~10us as stackvm ops.
// Big tensors (prefill) are left on the hardware path via the size guard.
[RuleGenerator]
public sealed class QuantToCpu : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Math.IsQuantize("quant", "call", (Quantize _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("qp"));

	private Expr? GetReplace(Call call, Quantize quant, Expr input, QuantParam qp)
	{
		if (qp.Scale <= 0f)
		{
			return null;
		}
		long numel = 1;
		int[] dims = call.CheckedShape.ToValueArray();
		int[] array = new int[dims.Length];
		for (int i = 0; i < dims.Length; i++)
		{
			array[i] = dims[i];
			numel *= dims[i];
			if (numel <= 0)
			{
				return null;
			}
		}
		// quantize input is always floating (PTQ); f32 = 4 bytes
		long bytes = numel * 4;
		if (bytes > 1048576)
		{
			return null;
		}
		float invS = 1f / qp.Scale;
		Expr scaled = Nncase.IR.F.Math.Binary(BinaryOp.Mul, input, Tensor.FromScalar(invS));
		Expr rounded = Nncase.IR.F.Math.Unary(UnaryOp.Round, scaled);
		float lo;
		float hi;
		if (quant.TargetType == DataTypes.UInt8)
		{
			lo = 0f - qp.ZeroPoint;
			hi = 255f - qp.ZeroPoint;
		}
		else if (quant.TargetType == DataTypes.Int8)
		{
			lo = -128f - qp.ZeroPoint;
			hi = 127f - qp.ZeroPoint;
		}
		else if (quant.TargetType == DataTypes.Int16)
		{
			lo = -2047f - qp.ZeroPoint;
			hi = 2047f - qp.ZeroPoint;
		}
		else
		{
			return null;
		}
		Expr clamped = Nncase.IR.F.Math.Binary(BinaryOp.Max, rounded, Tensor.FromScalar(lo));
		clamped = Nncase.IR.F.Math.Binary(BinaryOp.Min, clamped, Tensor.FromScalar(hi));
		Expr shifted = Nncase.IR.F.Math.Binary(BinaryOp.Add, clamped, Tensor.FromScalar((float)qp.ZeroPoint));
		return Nncase.IR.F.Tensors.Cast(shifted, quant.TargetType);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Quantize quant = (Quantize)__result["quant"];
		Expr input = (Expr)__result["input"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		return GetReplace(call, quant, input, qp);
	}
}
