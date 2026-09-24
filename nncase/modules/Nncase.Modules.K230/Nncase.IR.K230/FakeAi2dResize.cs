using System;
using Nncase.PatternMatch;
using Nncase.TIR.Instructions;

namespace Nncase.IR.K230;

[PatternFunctionalGenerator]
public sealed class FakeAi2dResize : Op, IEquatable<FakeAi2dResize?>
{
	public static readonly ParameterInfo Input = new ParameterInfo(typeof(FakeAi2dResize), 0, "input", GNNETypePatternUtility.ValidFakeInput());

	public static readonly ParameterInfo NewSize = new ParameterInfo(typeof(FakeAi2dResize), 1, "new_size", TypePatternUtility.HasDataType(DataTypes.Int32));

	public MFU_CROP_RESIZE ResizeMethod { get; }

	public bool HalfPixelCenters { get; }

	public bool AlignCorners { get; }

	public FakeAi2dResize(MFU_CROP_RESIZE resizeMethod, bool halfPixelCenters, bool alignCorners)
	{
		ResizeMethod = resizeMethod;
		HalfPixelCenters = halfPixelCenters;
		AlignCorners = alignCorners;
	}

	public FakeAi2dResize With(MFU_CROP_RESIZE? resizeMethod = null, bool? halfPixelCenters = null, bool? alignCorners = null)
	{
		return new FakeAi2dResize(resizeMethod ?? ResizeMethod, halfPixelCenters ?? HalfPixelCenters, alignCorners ?? AlignCorners);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as FakeAi2dResize);
	}

	public bool Equals(FakeAi2dResize? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && base.Equals((object?)other) && ResizeMethod.Equals(other.ResizeMethod) && HalfPixelCenters.Equals(other.HalfPixelCenters))
		{
			return AlignCorners.Equals(other.AlignCorners);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(ResizeMethod, HalfPixelCenters, AlignCorners));
	}
}
