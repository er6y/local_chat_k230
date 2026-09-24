using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class GNNELSTM : Op, IEquatable<GNNELSTM?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(GNNELSTM), 0, "input", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo WXc = new ParameterInfo(typeof(GNNELSTM), 1, "w_xc", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo ActXc = new ParameterInfo(typeof(GNNELSTM), 2, "act_xc");

	public static readonly ParameterInfo WRc = new ParameterInfo(typeof(GNNELSTM), 3, "w_rc", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo ActRc0 = new ParameterInfo(typeof(GNNELSTM), 4, "act_rc_0");

	public static readonly ParameterInfo ActRc1 = new ParameterInfo(typeof(GNNELSTM), 5, "act_rc_1");

	public static readonly ParameterInfo InitialH = new ParameterInfo(typeof(GNNELSTM), 6, "initial_h", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo InitialC = new ParameterInfo(typeof(GNNELSTM), 7, "initial_c", TypePatternUtility.HasRank(4) & GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo SegFittingParamFt = new ParameterInfo(typeof(GNNELSTM), 8, "seg_fitting_param_ft");

	public static readonly ParameterInfo SegFittingParamGt = new ParameterInfo(typeof(GNNELSTM), 9, "seg_fitting_param_gt");

	public static readonly ParameterInfo WXcQarg = new ParameterInfo(typeof(GNNELSTM), 10, "w_xc_qarg", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo WRcQarg = new ParameterInfo(typeof(GNNELSTM), 11, "w_rc_qarg", TypePatternUtility.HasDataType(DataTypes.UInt8));

	public static readonly ParameterInfo ActBin = new ParameterInfo(typeof(GNNELSTM), 12, "act_bin");

	public static readonly ParameterInfo ActBinQ = new ParameterInfo(typeof(GNNELSTM), 13, "act_bin_q");

	public static readonly ParameterInfo IfDeqBias = new ParameterInfo(typeof(GNNELSTM), 14, "if_deq_bias");

	public static readonly ParameterInfo XcShiftBits = new ParameterInfo(typeof(GNNELSTM), 15, "xc_shift_bits", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo HDeqBias0 = new ParameterInfo(typeof(GNNELSTM), 16, "h_deq_bias_0");

	public static readonly ParameterInfo HDeqBias1 = new ParameterInfo(typeof(GNNELSTM), 17, "h_deq_bias_1");

	public static readonly ParameterInfo CShiftBits = new ParameterInfo(typeof(GNNELSTM), 18, "c_shift_bits", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo RcShiftBits0 = new ParameterInfo(typeof(GNNELSTM), 19, "rc_shift_bits_0", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo RcShiftBits1 = new ParameterInfo(typeof(GNNELSTM), 20, "rc_shift_bits_1", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo OutHShiftBits = new ParameterInfo(typeof(GNNELSTM), 21, "out_h_shift_bits", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo OutCShiftBits = new ParameterInfo(typeof(GNNELSTM), 22, "out_c_shift_bits", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo HasStatic = new ParameterInfo(typeof(GNNELSTM), 23, "has_static", TypePatternUtility.IsBool());

	public static readonly ParameterInfo OutputSize = new ParameterInfo(typeof(GNNELSTM), 24, "output_size");

	public LSTMDirection Direction { get; }

	public ActParam2 ActivationParamBin { get; }

	public ActParam2 ActivationParamBinQ { get; }

	public ActParam2 ActivationParamXc { get; }

	public ActParam2 ActivationParamRc0 { get; }

	public ActParam2 ActivationParamRc1 { get; }

	public PrimType DestTypeO { get; }

	public PrimType DestTypeOH { get; }

	public GNNELSTM(LSTMDirection direction, ActParam2 activationParamBin, ActParam2 activationParamBinQ, ActParam2 activationParamXc, ActParam2 activationParamRc0, ActParam2 activationParamRc1, PrimType destTypeO, PrimType destTypeOH)
	{
		Direction = direction;
		ActivationParamBin = activationParamBin;
		ActivationParamBinQ = activationParamBinQ;
		ActivationParamXc = activationParamXc;
		ActivationParamRc0 = activationParamRc0;
		ActivationParamRc1 = activationParamRc1;
		DestTypeO = destTypeO;
		DestTypeOH = destTypeOH;
	}

	public GNNELSTM With(LSTMDirection? direction = null, ActParam2? activationParamBin = null, ActParam2? activationParamBinQ = null, ActParam2? activationParamXc = null, ActParam2? activationParamRc0 = null, ActParam2? activationParamRc1 = null, PrimType? destTypeO = null, PrimType? destTypeOH = null)
	{
		return new GNNELSTM(direction ?? Direction, activationParamBin ?? ActivationParamBin, activationParamBinQ ?? ActivationParamBinQ, activationParamXc ?? ActivationParamXc, activationParamRc0 ?? ActivationParamRc0, activationParamRc1 ?? ActivationParamRc1, destTypeO ?? DestTypeO, destTypeOH ?? DestTypeOH);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GNNELSTM);
	}

	public bool Equals(GNNELSTM? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && Direction.Equals(other.Direction) && ActivationParamBin.Equals(other.ActivationParamBin) && ActivationParamBinQ.Equals(other.ActivationParamBinQ) && ActivationParamXc.Equals(other.ActivationParamXc) && ActivationParamRc0.Equals(other.ActivationParamRc0) && ActivationParamRc1.Equals(other.ActivationParamRc1) && DestTypeO.Equals(other.DestTypeO))
		{
			return DestTypeOH.Equals(other.DestTypeOH);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Direction, ActivationParamBin, ActivationParamBinQ, ActivationParamXc, ActivationParamRc0, ActivationParamRc1, DestTypeO), HashCode.Combine(DestTypeOH));
	}
}
