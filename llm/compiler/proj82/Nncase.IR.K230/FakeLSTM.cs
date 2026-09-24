using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeLSTM : Op, IEquatable<FakeLSTM?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakeLSTM), 0, "input", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo WXc = new ParameterInfo(typeof(FakeLSTM), 1, "w_xc", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo ActXc = new ParameterInfo(typeof(FakeLSTM), 2, "act_xc", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo WRc = new ParameterInfo(typeof(FakeLSTM), 3, "w_rc", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo ActRc = new ParameterInfo(typeof(FakeLSTM), 4, "act_rc_0", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo InitialH = new ParameterInfo(typeof(FakeLSTM), 5, "initial_h", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo InitialC = new ParameterInfo(typeof(FakeLSTM), 6, "initial_c", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo SegFittingParamFt = new ParameterInfo(typeof(FakeLSTM), 7, "seg_fitting_param_ft", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo SegFittingParamGt = new ParameterInfo(typeof(FakeLSTM), 8, "seg_fitting_param_gt", TypePatternUtility.HasDataType(DataTypes.Float32));

	public static readonly ParameterInfo HasStatic = new ParameterInfo(typeof(FakeLSTM), 9, "has_static", TypePatternUtility.IsBool());

	public static readonly ParameterInfo OutputSize = new ParameterInfo(typeof(FakeLSTM), 10, "output_size", TypePatternUtility.IsIntegral());

	public LSTMDirection Direction { get; }

	public ActParam2 ActParamXc { get; }

	public ActParam2 ActParamRc { get; }

	public FakeLSTM(LSTMDirection direction, ActParam2 actParamXc, ActParam2 actParamRc)
	{
		Direction = direction;
		ActParamXc = actParamXc;
		ActParamRc = actParamRc;
	}

	public FakeLSTM With(LSTMDirection? direction = null, ActParam2? actParamXc = null, ActParam2? actParamRc = null)
	{
		return new FakeLSTM(direction ?? Direction, actParamXc ?? ActParamXc, actParamRc ?? ActParamRc);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeLSTM);
	}

	public bool Equals(FakeLSTM? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && Direction.Equals(other.Direction) && ActParamXc.Equals(other.ActParamXc))
		{
			return ActParamRc.Equals(other.ActParamRc);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Direction, ActParamXc, ActParamRc));
	}
}
