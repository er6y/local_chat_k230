using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

internal static class FuseQuantHelper
{
	internal static Pattern MakePattern<T>(string callName, string opName) where T : Op
	{
		return Math.IsQuantize("quant", "call", (Quantize _) => true, Nncase.PatternMatch.Utility.IsCallWildcard("st", Nncase.PatternMatch.Utility.IsOp<GNNEStore>("stOp"), Nncase.PatternMatch.Utility.IsCallWildcard(callName, Nncase.PatternMatch.Utility.IsOp<T>(opName), Nncase.PatternMatch.Utility.IsWildcard("input")), Nncase.PatternMatch.Utility.IsWildcard("stStrides")), Nncase.PatternMatch.Utility.IsWildcard("qp"));
	}

	internal static ActParamBase FuseActAndQuant(Quantize quant, QuantParam qp, ActParam2 actParam)
	{
		ActParam2 actParam2 = new ActParam2(actParam);
		actParam2.FusedQuantParam(qp);
		ValueRange<float> fusedClamp = GetFusedClamp(actParam2, quant.TargetType);
		actParam2.SetFusedClamp(fusedClamp);
		return actParam2;
	}

	internal static ActParamBase FuseActAndQuant(Quantize quant, QuantParam qp, ActParam16 actParam)
	{
		ActParam16 actParam2 = new ActParam16(actParam);
		actParam2.FusedQuantParam(qp);
		ValueRange<float> fusedClamp = GetFusedClamp(actParam2, quant.TargetType);
		actParam2.SetFusedClamp(fusedClamp);
		return actParam2;
	}

	internal static ValueRange<float> GetFusedClamp(ActParamBase act_param, DataType quantType)
	{
		ValueRange<float> full = ValueRange<float>.Full;
		if (quantType == DataTypes.UInt8)
		{
			for (int i = 0; i < act_param.FusedClamp.Length; i++)
			{
				full.Max = ((act_param.FusedClamp[i].Max > 255f) ? 255f : act_param.FusedClamp[i].Max);
				full.Min = ((act_param.FusedClamp[i].Min < 0f) ? 0f : act_param.FusedClamp[i].Min);
			}
		}
		else if (quantType == DataTypes.Int8)
		{
			for (int j = 0; j < act_param.FusedClamp.Length; j++)
			{
				full.Max = ((act_param.FusedClamp[j].Max > 127f) ? 127f : act_param.FusedClamp[j].Max);
				full.Min = ((act_param.FusedClamp[j].Min < -127f) ? (-127f) : act_param.FusedClamp[j].Min);
			}
		}
		else if (quantType == DataTypes.Int16)
		{
			for (int k = 0; k < act_param.FusedClamp.Length; k++)
			{
				full.Max = ((act_param.FusedClamp[k].Max > 2047f) ? 2047f : act_param.FusedClamp[k].Max);
				full.Min = ((act_param.FusedClamp[k].Min < -2047f) ? (-2047f) : act_param.FusedClamp[k].Min);
			}
		}
		return full;
	}
}
