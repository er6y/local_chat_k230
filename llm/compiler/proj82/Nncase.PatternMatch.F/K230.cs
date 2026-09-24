using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.TIR.Instructions;

namespace Nncase.PatternMatch.F;

public static class K230
{
	public static CallPattern IsFakeActivation(GnneActivationType Type, ActParamBase ActParam, IRArray<int> OutputShape, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>((FakeActivation x) => x.Type == Type && x.ActParam == ActParam && x.OutputShape == OutputShape, null), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeActivation(Func<FakeActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>(condition, null), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeActivation(string? target_name, GnneActivationType Type, ActParamBase ActParam, IRArray<int> OutputShape, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>((FakeActivation x) => x.Type == Type && x.ActParam == ActParam && x.OutputShape == OutputShape, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeActivation(string? target_name, Func<FakeActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeActivation(string? target_name, string? call_name, GnneActivationType Type, ActParamBase ActParam, IRArray<int> OutputShape, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>((FakeActivation x) => x.Type == Type && x.ActParam == ActParam && x.OutputShape == OutputShape, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeActivation(string? target_name, string? call_name, Func<FakeActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? outchannels = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<FakeActivation>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeAi2dPad(PadMode Mode, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>((FakeAi2dPad x) => x.Mode == Mode, null), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dPad(Func<FakeAi2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>(condition, null), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dPad(string? target_name, PadMode Mode, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>((FakeAi2dPad x) => x.Mode == Mode, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dPad(string? target_name, Func<FakeAi2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dPad(string? target_name, string? call_name, PadMode Mode, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>((FakeAi2dPad x) => x.Mode == Mode, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeAi2dPad(string? target_name, string? call_name, Func<FakeAi2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dPad>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeAi2dResize(MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, bool AlignCorners, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>((FakeAi2dResize x) => x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.AlignCorners == AlignCorners, null), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dResize(Func<FakeAi2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>(condition, null), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dResize(string? target_name, MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, bool AlignCorners, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>((FakeAi2dResize x) => x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.AlignCorners == AlignCorners, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dResize(string? target_name, Func<FakeAi2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>(condition, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeAi2dResize(string? target_name, string? call_name, MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, bool AlignCorners, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>((FakeAi2dResize x) => x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.AlignCorners == AlignCorners, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeAi2dResize(string? target_name, string? call_name, Func<FakeAi2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null)
	{
		return new CallPattern(new OpPattern<FakeAi2dResize>(condition, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeConv2D(ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>((FakeConv2D x) => x.ActParam == ActParam, null), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2D(Func<FakeConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>(condition, null), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2D(string? target_name, ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>((FakeConv2D x) => x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2D(string? target_name, Func<FakeConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2D(string? target_name, string? call_name, ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>((FakeConv2D x) => x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeConv2D(string? target_name, string? call_name, Func<FakeConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2D>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeConv2DTranspose(ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>((FakeConv2DTranspose x) => x.ActParam == ActParam, null), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2DTranspose(Func<FakeConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>(condition, null), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2DTranspose(string? target_name, ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>((FakeConv2DTranspose x) => x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2DTranspose(string? target_name, Func<FakeConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>(condition, target_name), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeConv2DTranspose(string? target_name, string? call_name, ActParam2 ActParam, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>((FakeConv2DTranspose x) => x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeConv2DTranspose(string? target_name, string? call_name, Func<FakeConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? act = null, Pattern? outputshape = null, Pattern? padding = null, Pattern? outputpadding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<FakeConv2DTranspose>(condition, target_name), new VArgsPattern(new Pattern[10]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(bool DynamicChannel, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>((FakeDynamicGNNEMatMul x) => x.DynamicChannel == DynamicChannel, null), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(Func<FakeDynamicGNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>(condition, null), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(string? target_name, bool DynamicChannel, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>((FakeDynamicGNNEMatMul x) => x.DynamicChannel == DynamicChannel, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(string? target_name, Func<FakeDynamicGNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(string? target_name, string? call_name, bool DynamicChannel, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>((FakeDynamicGNNEMatMul x) => x.DynamicChannel == DynamicChannel, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeDynamicGNNEMatMul(string? target_name, string? call_name, Func<FakeDynamicGNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeDynamicGNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeLSTM(LSTMDirection Direction, ActParam2 ActParamXc, ActParam2 ActParamRc, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>((FakeLSTM x) => x.Direction == Direction && x.ActParamXc == ActParamXc && x.ActParamRc == ActParamRc, null), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeLSTM(Func<FakeLSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>(condition, null), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeLSTM(string? target_name, LSTMDirection Direction, ActParam2 ActParamXc, ActParam2 ActParamRc, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>((FakeLSTM x) => x.Direction == Direction && x.ActParamXc == ActParamXc && x.ActParamRc == ActParamRc, target_name), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeLSTM(string? target_name, Func<FakeLSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>(condition, target_name), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeLSTM(string? target_name, string? call_name, LSTMDirection Direction, ActParam2 ActParamXc, ActParam2 ActParamRc, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>((FakeLSTM x) => x.Direction == Direction && x.ActParamXc == ActParamXc && x.ActParamRc == ActParamRc, target_name), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeLSTM(string? target_name, string? call_name, Func<FakeLSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<FakeLSTM>(condition, target_name), new VArgsPattern(new Pattern[11]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeMatMul(ActParam2 ActParam2, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>((FakeMatMul x) => x.ActParam2 == ActParam2, null), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeMatMul(Func<FakeMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>(condition, null), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeMatMul(string? target_name, ActParam2 ActParam2, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>((FakeMatMul x) => x.ActParam2 == ActParam2, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeMatMul(string? target_name, Func<FakeMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakeMatMul(string? target_name, string? call_name, ActParam2 ActParam2, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>((FakeMatMul x) => x.ActParam2 == ActParam2, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakeMatMul(string? target_name, string? call_name, Func<FakeMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<FakeMatMul>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakePdp(ReduceOp ReduceOp, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>((FakePdp x) => x.ReduceOp == ReduceOp, null), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakePdp(Func<FakePdp, bool> condition, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>(condition, null), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakePdp(string? target_name, ReduceOp ReduceOp, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>((FakePdp x) => x.ReduceOp == ReduceOp, target_name), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakePdp(string? target_name, Func<FakePdp, bool> condition, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>(condition, target_name), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsFakePdp(string? target_name, string? call_name, ReduceOp ReduceOp, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>((FakePdp x) => x.ReduceOp == ReduceOp, target_name), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsFakePdp(string? target_name, string? call_name, Func<FakePdp, bool> condition, Pattern? input = null, Pattern? padvalue = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<FakePdp>(condition, target_name), new VArgsPattern(new Pattern[6]
		{
			input ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsAi2dPad(PadMode Mode, PrimType OutputType, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>((Ai2dPad x) => x.Mode == Mode && x.OutputType == OutputType, null), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dPad(Func<Ai2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>(condition, null), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dPad(string? target_name, PadMode Mode, PrimType OutputType, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>((Ai2dPad x) => x.Mode == Mode && x.OutputType == OutputType, target_name), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dPad(string? target_name, Func<Ai2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>(condition, target_name), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dPad(string? target_name, string? call_name, PadMode Mode, PrimType OutputType, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>((Ai2dPad x) => x.Mode == Mode && x.OutputType == OutputType, target_name), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsAi2dPad(string? target_name, string? call_name, Func<Ai2dPad, bool> condition, Pattern? input = null, Pattern? padding = null, Pattern? value = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dPad>(condition, target_name), new VArgsPattern(new Pattern[5]
		{
			input ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsAi2dResize(bool AlignCorners, MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, PrimType OutputDatatype, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>((Ai2dResize x) => x.AlignCorners == AlignCorners && x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.OutputDatatype == OutputDatatype, null), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dResize(Func<Ai2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>(condition, null), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dResize(string? target_name, bool AlignCorners, MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, PrimType OutputDatatype, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>((Ai2dResize x) => x.AlignCorners == AlignCorners && x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.OutputDatatype == OutputDatatype, target_name), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dResize(string? target_name, Func<Ai2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>(condition, target_name), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsAi2dResize(string? target_name, string? call_name, bool AlignCorners, MFU_CROP_RESIZE ResizeMethod, bool HalfPixelCenters, PrimType OutputDatatype, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>((Ai2dResize x) => x.AlignCorners == AlignCorners && x.ResizeMethod == ResizeMethod && x.HalfPixelCenters == HalfPixelCenters && x.OutputDatatype == OutputDatatype, target_name), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsAi2dResize(string? target_name, string? call_name, Func<Ai2dResize, bool> condition, Pattern? input = null, Pattern? newsize = null, Pattern? indeqbias = null, Pattern? outquantparam = null)
	{
		return new CallPattern(new OpPattern<Ai2dResize>(condition, target_name), new VArgsPattern(new Pattern[4]
		{
			input ?? Utility.IsWildcard(),
			newsize ?? Utility.IsWildcard(),
			indeqbias ?? Utility.IsWildcard(),
			outquantparam ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsDynamicGNNEMatMul(PrimType OutputDType, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>((DynamicGNNEMatMul x) => x.OutputDType == OutputDType, null), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsDynamicGNNEMatMul(Func<DynamicGNNEMatMul, bool> condition, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>(condition, null), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsDynamicGNNEMatMul(string? target_name, PrimType OutputDType, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>((DynamicGNNEMatMul x) => x.OutputDType == OutputDType, target_name), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsDynamicGNNEMatMul(string? target_name, Func<DynamicGNNEMatMul, bool> condition, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsDynamicGNNEMatMul(string? target_name, string? call_name, PrimType OutputDType, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>((DynamicGNNEMatMul x) => x.OutputDType == OutputDType, target_name), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsDynamicGNNEMatMul(string? target_name, string? call_name, Func<DynamicGNNEMatMul, bool> condition, Pattern? text = null, Pattern? inputa = null, Pattern? inputb = null, Pattern? inputabias = null, Pattern? inputbbias = null, Pattern? act = null, Pattern? shiftbits = null, Pattern? dynamicchannel = null)
	{
		return new CallPattern(new OpPattern<DynamicGNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			text ?? Utility.IsWildcard(),
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inputbbias ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			dynamicchannel ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEActivation(GnneActivationType Type, PrimType OutputDType, ActParamBase ActParam, IRArray<int> OutputShape, IRArray<bool> InputFromL1, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>((GNNEActivation x) => x.Type == Type && x.OutputDType == OutputDType && x.ActParam == ActParam && x.OutputShape == OutputShape && x.InputFromL1 == InputFromL1, null), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEActivation(Func<GNNEActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>(condition, null), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEActivation(string? target_name, GnneActivationType Type, PrimType OutputDType, ActParamBase ActParam, IRArray<int> OutputShape, IRArray<bool> InputFromL1, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>((GNNEActivation x) => x.Type == Type && x.OutputDType == OutputDType && x.ActParam == ActParam && x.OutputShape == OutputShape && x.InputFromL1 == InputFromL1, target_name), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEActivation(string? target_name, Func<GNNEActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>(condition, target_name), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEActivation(string? target_name, string? call_name, GnneActivationType Type, PrimType OutputDType, ActParamBase ActParam, IRArray<int> OutputShape, IRArray<bool> InputFromL1, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>((GNNEActivation x) => x.Type == Type && x.OutputDType == OutputDType && x.ActParam == ActParam && x.OutputShape == OutputShape && x.InputFromL1 == InputFromL1, target_name), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEActivation(string? target_name, string? call_name, Func<GNNEActivation, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? outshiftbits = null, Pattern? deqaparams = null, Pattern? deqbparams = null, Pattern? outchannels = null, Pattern? is16segments = null)
	{
		return new CallPattern(new OpPattern<GNNEActivation>(condition, target_name), new VArgsPattern(new Pattern[10]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			outshiftbits ?? Utility.IsWildcard(),
			deqaparams ?? Utility.IsWildcard(),
			deqbparams ?? Utility.IsWildcard(),
			outchannels ?? Utility.IsWildcard(),
			is16segments ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEConv2D(ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>((GNNEConv2D x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, null), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2D(Func<GNNEConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>(condition, null), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2D(string? target_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>((GNNEConv2D x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2D(string? target_name, Func<GNNEConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>(condition, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2D(string? target_name, string? call_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>((GNNEConv2D x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEConv2D(string? target_name, string? call_name, Func<GNNEConv2D, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2D>(condition, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEConv2DTranspose(ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>((GNNEConv2DTranspose x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, null), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2DTranspose(Func<GNNEConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>(condition, null), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2DTranspose(string? target_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>((GNNEConv2DTranspose x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2DTranspose(string? target_name, Func<GNNEConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>(condition, target_name), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEConv2DTranspose(string? target_name, string? call_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>((GNNEConv2DTranspose x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEConv2DTranspose(string? target_name, string? call_name, Func<GNNEConv2DTranspose, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null, Pattern? outputpadding = null, Pattern? outputshape = null)
	{
		return new CallPattern(new OpPattern<GNNEConv2DTranspose>(condition, target_name), new VArgsPattern(new Pattern[19]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard(),
			outputpadding ?? Utility.IsWildcard(),
			outputshape ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEFusion(Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>((GNNEFusion x) => true, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNEFusion(Func<GNNEFusion, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>(condition, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNEFusion(string? target_name, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>((GNNEFusion x) => true, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNEFusion(string? target_name, Func<GNNEFusion, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNEFusion(string? target_name, string? call_name, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>((GNNEFusion x) => true, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNEFusion(string? target_name, string? call_name, Func<GNNEFusion, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNEFusion>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNELoad(PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>((GNNELoad x) => x.DestType == DestType, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoad(Func<GNNELoad, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>(condition, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoad(string? target_name, PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>((GNNELoad x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoad(string? target_name, Func<GNNELoad, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoad(string? target_name, string? call_name, PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>((GNNELoad x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNELoad(string? target_name, string? call_name, Func<GNNELoad, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoad>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNELoadW(PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>((GNNELoadW x) => x.DestType == DestType, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoadW(Func<GNNELoadW, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>(condition, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoadW(string? target_name, PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>((GNNELoadW x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoadW(string? target_name, Func<GNNELoadW, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNELoadW(string? target_name, string? call_name, PrimType DestType, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>((GNNELoadW x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNELoadW(string? target_name, string? call_name, Func<GNNELoadW, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNELoadW>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNELSTM(LSTMDirection Direction, ActParam2 ActivationParamBin, ActParam2 ActivationParamBinQ, ActParam2 ActivationParamXc, ActParam2 ActivationParamRc0, ActParam2 ActivationParamRc1, PrimType DestTypeO, PrimType DestTypeOH, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>((GNNELSTM x) => x.Direction == Direction && x.ActivationParamBin == ActivationParamBin && x.ActivationParamBinQ == ActivationParamBinQ && x.ActivationParamXc == ActivationParamXc && x.ActivationParamRc0 == ActivationParamRc0 && x.ActivationParamRc1 == ActivationParamRc1 && x.DestTypeO == DestTypeO && x.DestTypeOH == DestTypeOH, null), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNELSTM(Func<GNNELSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>(condition, null), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNELSTM(string? target_name, LSTMDirection Direction, ActParam2 ActivationParamBin, ActParam2 ActivationParamBinQ, ActParam2 ActivationParamXc, ActParam2 ActivationParamRc0, ActParam2 ActivationParamRc1, PrimType DestTypeO, PrimType DestTypeOH, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>((GNNELSTM x) => x.Direction == Direction && x.ActivationParamBin == ActivationParamBin && x.ActivationParamBinQ == ActivationParamBinQ && x.ActivationParamXc == ActivationParamXc && x.ActivationParamRc0 == ActivationParamRc0 && x.ActivationParamRc1 == ActivationParamRc1 && x.DestTypeO == DestTypeO && x.DestTypeOH == DestTypeOH, target_name), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNELSTM(string? target_name, Func<GNNELSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>(condition, target_name), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNELSTM(string? target_name, string? call_name, LSTMDirection Direction, ActParam2 ActivationParamBin, ActParam2 ActivationParamBinQ, ActParam2 ActivationParamXc, ActParam2 ActivationParamRc0, ActParam2 ActivationParamRc1, PrimType DestTypeO, PrimType DestTypeOH, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>((GNNELSTM x) => x.Direction == Direction && x.ActivationParamBin == ActivationParamBin && x.ActivationParamBinQ == ActivationParamBinQ && x.ActivationParamXc == ActivationParamXc && x.ActivationParamRc0 == ActivationParamRc0 && x.ActivationParamRc1 == ActivationParamRc1 && x.DestTypeO == DestTypeO && x.DestTypeOH == DestTypeOH, target_name), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNELSTM(string? target_name, string? call_name, Func<GNNELSTM, bool> condition, Pattern? input = null, Pattern? wxc = null, Pattern? actxc = null, Pattern? wrc = null, Pattern? actrc0 = null, Pattern? actrc1 = null, Pattern? initialh = null, Pattern? initialc = null, Pattern? segfittingparamft = null, Pattern? segfittingparamgt = null, Pattern? wxcqarg = null, Pattern? wrcqarg = null, Pattern? actbin = null, Pattern? actbinq = null, Pattern? ifdeqbias = null, Pattern? xcshiftbits = null, Pattern? hdeqbias0 = null, Pattern? hdeqbias1 = null, Pattern? cshiftbits = null, Pattern? rcshiftbits0 = null, Pattern? rcshiftbits1 = null, Pattern? outhshiftbits = null, Pattern? outcshiftbits = null, Pattern? hasstatic = null, Pattern? outputsize = null)
	{
		return new CallPattern(new OpPattern<GNNELSTM>(condition, target_name), new VArgsPattern(new Pattern[25]
		{
			input ?? Utility.IsWildcard(),
			wxc ?? Utility.IsWildcard(),
			actxc ?? Utility.IsWildcard(),
			wrc ?? Utility.IsWildcard(),
			actrc0 ?? Utility.IsWildcard(),
			actrc1 ?? Utility.IsWildcard(),
			initialh ?? Utility.IsWildcard(),
			initialc ?? Utility.IsWildcard(),
			segfittingparamft ?? Utility.IsWildcard(),
			segfittingparamgt ?? Utility.IsWildcard(),
			wxcqarg ?? Utility.IsWildcard(),
			wrcqarg ?? Utility.IsWildcard(),
			actbin ?? Utility.IsWildcard(),
			actbinq ?? Utility.IsWildcard(),
			ifdeqbias ?? Utility.IsWildcard(),
			xcshiftbits ?? Utility.IsWildcard(),
			hdeqbias0 ?? Utility.IsWildcard(),
			hdeqbias1 ?? Utility.IsWildcard(),
			cshiftbits ?? Utility.IsWildcard(),
			rcshiftbits0 ?? Utility.IsWildcard(),
			rcshiftbits1 ?? Utility.IsWildcard(),
			outhshiftbits ?? Utility.IsWildcard(),
			outcshiftbits ?? Utility.IsWildcard(),
			hasstatic ?? Utility.IsWildcard(),
			outputsize ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEMatMul(ActParam2 ActParam, PrimType OutputDType, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>((GNNEMatMul x) => x.ActParam == ActParam && x.OutputDType == OutputDType, null), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEMatMul(Func<GNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>(condition, null), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEMatMul(string? target_name, ActParam2 ActParam, PrimType OutputDType, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>((GNNEMatMul x) => x.ActParam == ActParam && x.OutputDType == OutputDType, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEMatMul(string? target_name, Func<GNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEMatMul(string? target_name, string? call_name, ActParam2 ActParam, PrimType OutputDType, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>((GNNEMatMul x) => x.ActParam == ActParam && x.OutputDType == OutputDType, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEMatMul(string? target_name, string? call_name, Func<GNNEMatMul, bool> condition, Pattern? inputa = null, Pattern? inputb = null, Pattern? act = null, Pattern? inputabias = null, Pattern? inashiftbits = null, Pattern? inbshiftbits = null, Pattern? shiftbits = null, Pattern? deqbbias = null)
	{
		return new CallPattern(new OpPattern<GNNEMatMul>(condition, target_name), new VArgsPattern(new Pattern[8]
		{
			inputa ?? Utility.IsWildcard(),
			inputb ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			inputabias ?? Utility.IsWildcard(),
			inashiftbits ?? Utility.IsWildcard(),
			inbshiftbits ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			deqbbias ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPad(PadMode Mode, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>((GNNEPad x) => x.Mode == Mode, null), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPad(Func<GNNEPad, bool> condition, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>(condition, null), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPad(string? target_name, PadMode Mode, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>((GNNEPad x) => x.Mode == Mode, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPad(string? target_name, Func<GNNEPad, bool> condition, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPad(string? target_name, string? call_name, PadMode Mode, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>((GNNEPad x) => x.Mode == Mode, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPad(string? target_name, string? call_name, Func<GNNEPad, bool> condition, Pattern? input = null, Pattern? pads = null, Pattern? value = null)
	{
		return new CallPattern(new OpPattern<GNNEPad>(condition, target_name), new VArgsPattern(new Pattern[3]
		{
			input ?? Utility.IsWildcard(),
			pads ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp0DW(ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>((GNNEPdp0DW x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, null), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0DW(Func<GNNEPdp0DW, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>(condition, null), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0DW(string? target_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>((GNNEPdp0DW x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0DW(string? target_name, Func<GNNEPdp0DW, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>(condition, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0DW(string? target_name, string? call_name, ActParam2 ActParam, ActParam2 ActParamQInt8, PrimType DestType, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>((GNNEPdp0DW x) => x.ActParam == ActParam && x.ActParamQInt8 == ActParamQInt8 && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp0DW(string? target_name, string? call_name, Func<GNNEPdp0DW, bool> condition, Pattern? input = null, Pattern? weights = null, Pattern? weightsbias = null, Pattern? weightsbiasqint8 = null, Pattern? act = null, Pattern? actqint8 = null, Pattern? deqbias = null, Pattern? shiftbits = null, Pattern? shiftbitsqint8 = null, Pattern? qint8qp = null, Pattern? padding = null, Pattern? stride = null, Pattern? dilation = null, Pattern? groups = null, Pattern? is16quant = null, Pattern? padvalue = null, Pattern? weightsqint8 = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0DW>(condition, target_name), new VArgsPattern(new Pattern[17]
		{
			input ?? Utility.IsWildcard(),
			weights ?? Utility.IsWildcard(),
			weightsbias ?? Utility.IsWildcard(),
			weightsbiasqint8 ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard(),
			actqint8 ?? Utility.IsWildcard(),
			deqbias ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			shiftbitsqint8 ?? Utility.IsWildcard(),
			qint8qp ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			dilation ?? Utility.IsWildcard(),
			groups ?? Utility.IsWildcard(),
			is16quant ?? Utility.IsWildcard(),
			padvalue ?? Utility.IsWildcard(),
			weightsqint8 ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp0Reduce(PU_PDP0_MODE ReduceOp, PrimType DestType, ActParam2 ActParam, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>((GNNEPdp0Reduce x) => x.ReduceOp == ReduceOp && x.DestType == DestType && x.ActParam == ActParam, null), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0Reduce(Func<GNNEPdp0Reduce, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>(condition, null), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0Reduce(string? target_name, PU_PDP0_MODE ReduceOp, PrimType DestType, ActParam2 ActParam, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>((GNNEPdp0Reduce x) => x.ReduceOp == ReduceOp && x.DestType == DestType && x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0Reduce(string? target_name, Func<GNNEPdp0Reduce, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>(condition, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp0Reduce(string? target_name, string? call_name, PU_PDP0_MODE ReduceOp, PrimType DestType, ActParam2 ActParam, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>((GNNEPdp0Reduce x) => x.ReduceOp == ReduceOp && x.DestType == DestType && x.ActParam == ActParam, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp0Reduce(string? target_name, string? call_name, Func<GNNEPdp0Reduce, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? depuantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null, Pattern? act = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp0Reduce>(condition, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			depuantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard(),
			act ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp1(MFU_PDP_OP ReduceOp, PrimType DestType, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>((GNNEPdp1 x) => x.ReduceOp == ReduceOp && x.DestType == DestType, null), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp1(Func<GNNEPdp1, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>(condition, null), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp1(string? target_name, MFU_PDP_OP ReduceOp, PrimType DestType, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>((GNNEPdp1 x) => x.ReduceOp == ReduceOp && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp1(string? target_name, Func<GNNEPdp1, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>(condition, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEPdp1(string? target_name, string? call_name, MFU_PDP_OP ReduceOp, PrimType DestType, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>((GNNEPdp1 x) => x.ReduceOp == ReduceOp && x.DestType == DestType, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEPdp1(string? target_name, string? call_name, Func<GNNEPdp1, bool> condition, Pattern? input = null, Pattern? filter = null, Pattern? stride = null, Pattern? padding = null, Pattern? quantparams = null, Pattern? dequantparams = null, Pattern? value = null, Pattern? shiftbits = null, Pattern? countincludepad = null)
	{
		return new CallPattern(new OpPattern<GNNEPdp1>(condition, target_name), new VArgsPattern(new Pattern[9]
		{
			input ?? Utility.IsWildcard(),
			filter ?? Utility.IsWildcard(),
			stride ?? Utility.IsWildcard(),
			padding ?? Utility.IsWildcard(),
			quantparams ?? Utility.IsWildcard(),
			dequantparams ?? Utility.IsWildcard(),
			value ?? Utility.IsWildcard(),
			shiftbits ?? Utility.IsWildcard(),
			countincludepad ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEStore(PrimType DestType, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>((GNNEStore x) => x.DestType == DestType, null), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEStore(Func<GNNEStore, bool> condition, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>(condition, null), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEStore(string? target_name, PrimType DestType, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>((GNNEStore x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEStore(string? target_name, Func<GNNEStore, bool> condition, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>(condition, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), null);
	}

	public static CallPattern IsGNNEStore(string? target_name, string? call_name, PrimType DestType, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>((GNNEStore x) => x.DestType == DestType, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNEStore(string? target_name, string? call_name, Func<GNNEStore, bool> condition, Pattern? input = null, Pattern? strides = null)
	{
		return new CallPattern(new OpPattern<GNNEStore>(condition, target_name), new VArgsPattern(new Pattern[2]
		{
			input ?? Utility.IsWildcard(),
			strides ?? Utility.IsWildcard()
		}, null), call_name);
	}

	public static CallPattern IsGNNETranspose(MFU_TRANS_PERMUTE Perm, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>((GNNETranspose x) => x.Perm == Perm, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNETranspose(Func<GNNETranspose, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>(condition, null), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNETranspose(string? target_name, MFU_TRANS_PERMUTE Perm, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>((GNNETranspose x) => x.Perm == Perm, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNETranspose(string? target_name, Func<GNNETranspose, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), null);
	}

	public static CallPattern IsGNNETranspose(string? target_name, string? call_name, MFU_TRANS_PERMUTE Perm, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>((GNNETranspose x) => x.Perm == Perm, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}

	public static CallPattern IsGNNETranspose(string? target_name, string? call_name, Func<GNNETranspose, bool> condition, Pattern? input = null)
	{
		return new CallPattern(new OpPattern<GNNETranspose>(condition, target_name), new VArgsPattern(new Pattern[1] { input ?? Utility.IsWildcard() }, null), call_name);
	}
}
