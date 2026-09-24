using System;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class Ai2dResize : Op, IEquatable<Ai2dResize?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(Ai2dResize), 0, "input", GNNETypePatternUtility.ValidDType());

	public static readonly ParameterInfo NewSize = new ParameterInfo(typeof(Ai2dResize), 1, "new_size", TypePatternUtility.HasDataType(DataTypes.Int32));

	public static readonly ParameterInfo InDeqBias = new ParameterInfo(typeof(Ai2dResize), 2, "in_deq_bias", TypePatternUtility.IsIntegral());

	public static readonly ParameterInfo OutQuantParam = new ParameterInfo(typeof(Ai2dResize), 3, "out_quant_param");

	public bool AlignCorners { get; }

	public MFU_CROP_RESIZE ResizeMethod { get; }

	public bool HalfPixelCenters { get; }

	public PrimType OutputDatatype { get; }

	public Ai2dResize(bool alignCorners, MFU_CROP_RESIZE resizeMethod, bool halfPixelCenters, PrimType outputDatatype)
	{
		AlignCorners = alignCorners;
		ResizeMethod = resizeMethod;
		HalfPixelCenters = halfPixelCenters;
		OutputDatatype = outputDatatype;
	}

	public Ai2dResize With(bool? alignCorners = null, MFU_CROP_RESIZE? resizeMethod = null, bool? halfPixelCenters = null, PrimType? outputDatatype = null)
	{
		return new Ai2dResize(alignCorners ?? AlignCorners, resizeMethod ?? ResizeMethod, halfPixelCenters ?? HalfPixelCenters, outputDatatype ?? OutputDatatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as Ai2dResize);
	}

	public bool Equals(Ai2dResize? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && AlignCorners.Equals(other.AlignCorners) && ResizeMethod.Equals(other.ResizeMethod) && HalfPixelCenters.Equals(other.HalfPixelCenters))
		{
			return OutputDatatype.Equals(other.OutputDatatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(AlignCorners, ResizeMethod, HalfPixelCenters, OutputDatatype));
	}
}
