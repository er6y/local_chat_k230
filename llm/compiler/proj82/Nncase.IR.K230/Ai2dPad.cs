using System;
using Nncase.PatternMatch;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class Ai2dPad : Op, IEquatable<Ai2dPad?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(Ai2dPad), 0, "input", GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo Padding = new ParameterInfo(typeof(Ai2dPad), 1, "padding", TypePatternUtility.HasRank(2) & TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo Value = new ParameterInfo(typeof(Ai2dPad), 2, "value", TypePatternUtility.IsScalar());

	public static readonly ParameterInfo InDeqBias = new ParameterInfo(typeof(Ai2dPad), 3, "in_deq_bias", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo OutQuantParam = new ParameterInfo(typeof(Ai2dPad), 4, "out_quant_param");

	public PadMode Mode { get; }

	public PrimType OutputType { get; }

	public Ai2dPad(PadMode mode, PrimType outputType)
	{
		Mode = mode;
		OutputType = outputType;
	}

	public Ai2dPad With(PadMode? mode = null, PrimType? outputType = null)
	{
		return new Ai2dPad(mode ?? Mode, outputType ?? OutputType);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as Ai2dPad);
	}

	public bool Equals(Ai2dPad? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && Mode.Equals(other.Mode))
		{
			return OutputType.Equals(other.OutputType);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Mode, OutputType));
	}
}
