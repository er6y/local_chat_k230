namespace Nncase.Passes.Rules.K230;

public struct Ai2dConfig
{
	public uint SrcCh0Ptr;

	public uint SrcCh1Ptr;

	public uint SrcCh2Ptr;

	public uint SrcCh3Ptr;

	public uint DstCh0Ptr;

	public uint DstCh1Ptr;

	public uint DstCh2Ptr;

	public uint DstCh3Ptr;

	public uint SrcCh0WidthLayout;

	public uint SrcCh1WidthLayout;

	public uint SrcCh2WidthLayout;

	public uint SrcCh3WidthLayout;

	public uint DstCh0WidthLayout;

	public uint DstCh1WidthLayout;

	public uint DstCh2WidthLayout;

	public uint DstCh3WidthLayout;

	public uint M0;

	public uint M1;

	public uint M3;

	public uint M4;

	public uint Reserved0;

	public uint Reserved1;

	public uint Channel;

	public uint DstChannel;

	public uint CordRound;

	public uint Interpolation;

	public uint PadMod;

	public uint Shift;

	public uint BoundInd;

	public uint SrcFormat;

	public uint DstFormat;

	public uint BoundVal;

	public uint BoundSmooth;

	public uint Reserved2;

	public uint Yuv2rgbCoef0;

	public uint Yuv2rgbCoef1;

	public uint Reserved3;

	public uint Yuv2RgbCoef2;

	public uint Yuv2RgbCoef3;

	public uint Reserved4;

	public uint Yuv2RgbCoef4;

	public uint Yuv2RgbCoef5;

	public uint Reserved5;

	public uint Yuv2RgbCoef6;

	public uint Yuv2RgbCoef7;

	public uint Reserved6;

	public uint Yuv2RgbCoef10;

	public uint Yuv2RgbCoef11;

	public uint ConstPadCh0;

	public uint ConstPadCh1;

	public uint ConstPadCh2;

	public uint ConstPadCh3;

	public uint CrcInd;

	public uint DstInd;

	public uint CmdId;

	public uint Sign;

	public uint Reserved7;

	public uint Yuv2RgbCoef8;

	public uint Yuv2RgbCoef9;

	public uint Reserved8;

	public uint Reserved9;

	public uint PadT;

	public uint PadB;

	public uint PadL;

	public uint PadR;

	public uint SrcWidthShape;

	public uint SrcHeightShape;

	public uint DstWidthShape;

	public uint DstHeightShape;

	public uint IntrMask;

	public uint CscEn;

	public uint M2;

	public uint M5;

	public uint SrcX;

	public uint SrcY;

	public uint DstX;

	public uint DstY;

	public uint Reserved10;

	public uint Ai2dCalcEnable;

	public Ai2dConfig()
	{
		SrcCh0Ptr = 32u;
		SrcCh1Ptr = 32u;
		SrcCh2Ptr = 32u;
		SrcCh3Ptr = 32u;
		DstCh0Ptr = 32u;
		DstCh1Ptr = 32u;
		DstCh2Ptr = 32u;
		DstCh3Ptr = 32u;
		SrcCh0WidthLayout = 16u;
		SrcCh1WidthLayout = 16u;
		SrcCh2WidthLayout = 16u;
		SrcCh3WidthLayout = 16u;
		DstCh0WidthLayout = 16u;
		DstCh1WidthLayout = 16u;
		DstCh2WidthLayout = 16u;
		DstCh3WidthLayout = 16u;
		M0 = 32u;
		M1 = 32u;
		M3 = 32u;
		M4 = 32u;
		Reserved0 = 32u;
		Reserved1 = 32u;
		Channel = 3u;
		DstChannel = 3u;
		CordRound = 2u;
		Interpolation = 2u;
		PadMod = 2u;
		Shift = 8u;
		BoundInd = 4u;
		SrcFormat = 4u;
		DstFormat = 4u;
		BoundVal = 16u;
		BoundSmooth = 1u;
		Reserved2 = 15u;
		Yuv2rgbCoef0 = 12u;
		Yuv2rgbCoef1 = 12u;
		Reserved3 = 8u;
		Yuv2RgbCoef2 = 12u;
		Yuv2RgbCoef3 = 12u;
		Reserved4 = 8u;
		Yuv2RgbCoef4 = 12u;
		Yuv2RgbCoef5 = 12u;
		Reserved5 = 8u;
		Yuv2RgbCoef6 = 12u;
		Yuv2RgbCoef7 = 12u;
		Reserved6 = 8u;
		Yuv2RgbCoef10 = 12u;
		Yuv2RgbCoef11 = 12u;
		ConstPadCh0 = 8u;
		ConstPadCh1 = 8u;
		ConstPadCh2 = 8u;
		ConstPadCh3 = 8u;
		CrcInd = 1u;
		DstInd = 1u;
		CmdId = 1u;
		Sign = 1u;
		Reserved7 = 4u;
		Yuv2RgbCoef8 = 12u;
		Yuv2RgbCoef9 = 12u;
		Reserved8 = 8u;
		Reserved9 = 32u;
		PadT = 16u;
		PadB = 16u;
		PadL = 16u;
		PadR = 16u;
		SrcWidthShape = 16u;
		SrcHeightShape = 16u;
		DstWidthShape = 16u;
		DstHeightShape = 14u;
		IntrMask = 1u;
		CscEn = 1u;
		M2 = 32u;
		M5 = 32u;
		SrcX = 16u;
		SrcY = 16u;
		DstX = 16u;
		DstY = 13u;
		Reserved10 = 2u;
		Ai2dCalcEnable = 1u;
		SrcCh0Ptr = 0u;
		SrcCh1Ptr = 0u;
		SrcCh2Ptr = 0u;
		SrcCh3Ptr = 0u;
		DstCh0Ptr = 0u;
		DstCh1Ptr = 0u;
		DstCh2Ptr = 0u;
		DstCh3Ptr = 0u;
		SrcCh0WidthLayout = 0u;
		SrcCh1WidthLayout = 0u;
		SrcCh2WidthLayout = 0u;
		SrcCh3WidthLayout = 0u;
		DstCh0WidthLayout = 0u;
		DstCh1WidthLayout = 0u;
		DstCh2WidthLayout = 0u;
		DstCh3WidthLayout = 0u;
		FP32 fP = default(FP32);
		fP.U = 0u;
		fP.F = 1024f;
		M0 = fP.U;
		M1 = 0u;
		M3 = 0u;
		M4 = fP.U;
		Reserved0 = 0u;
		Reserved1 = 0u;
		Channel = 0u;
		DstChannel = 0u;
		CordRound = 0u;
		Interpolation = 0u;
		PadMod = 0u;
		Shift = 0u;
		BoundInd = 0u;
		SrcFormat = 0u;
		DstFormat = 0u;
		BoundVal = 0u;
		BoundSmooth = 0u;
		Reserved2 = 0u;
		Yuv2rgbCoef0 = 256u;
		Yuv2rgbCoef1 = 0u;
		Reserved3 = 0u;
		Yuv2RgbCoef2 = 292u;
		Yuv2RgbCoef3 = 3950u;
		Reserved4 = 0u;
		Yuv2RgbCoef4 = 256u;
		Yuv2RgbCoef5 = 3995u;
		Reserved5 = 0u;
		Yuv2RgbCoef6 = 3947u;
		Yuv2RgbCoef7 = 125u;
		Reserved6 = 0u;
		Yuv2RgbCoef10 = 0u;
		Yuv2RgbCoef11 = 3836u;
		ConstPadCh0 = 0u;
		ConstPadCh1 = 0u;
		ConstPadCh2 = 0u;
		ConstPadCh3 = 0u;
		CrcInd = 0u;
		DstInd = 0u;
		CmdId = 0u;
		Sign = 0u;
		Reserved7 = 0u;
		Yuv2RgbCoef8 = 256u;
		Yuv2RgbCoef9 = 520u;
		Reserved8 = 0u;
		Reserved9 = 0u;
		PadT = 0u;
		PadB = 0u;
		PadL = 0u;
		PadR = 0u;
		SrcWidthShape = 0u;
		SrcHeightShape = 0u;
		DstWidthShape = 0u;
		DstHeightShape = 0u;
		IntrMask = 1u;
		CscEn = 0u;
		M2 = 0u;
		M5 = 0u;
		SrcX = 0u;
		SrcY = 0u;
		DstX = 0u;
		DstY = 0u;
		Reserved10 = 0u;
		Ai2dCalcEnable = 1u;
	}

	public uint GetAddrValue(uint idx)
	{
		return idx switch
		{
			0u => SrcCh0Ptr, 
			1u => SrcCh1Ptr, 
			2u => SrcCh2Ptr, 
			3u => SrcCh3Ptr, 
			4u => DstCh0Ptr, 
			5u => DstCh1Ptr, 
			6u => DstCh2Ptr, 
			7u => DstCh3Ptr, 
			8u => (SrcCh1WidthLayout << 16) + SrcCh0WidthLayout, 
			9u => (SrcCh3WidthLayout << 16) + SrcCh2WidthLayout, 
			10u => (DstCh1WidthLayout << 16) + DstCh0WidthLayout, 
			11u => (DstCh3WidthLayout << 16) + DstCh2WidthLayout, 
			12u => M0, 
			13u => M1, 
			14u => M3, 
			15u => M4, 
			16u => Reserved0, 
			17u => Reserved1, 
			18u => (DstFormat << 28) | (SrcFormat << 24) | (BoundInd << 20) | (Shift << 12) | (PadMod << 10) | (Interpolation << 8) | (CordRound << 6) | (DstChannel << 3) | Channel, 
			19u => (Reserved2 << 17) | (BoundSmooth << 16) | BoundVal, 
			20u => (Reserved3 << 24) | (Yuv2rgbCoef1 << 12) | Yuv2rgbCoef0, 
			21u => (Reserved4 << 24) | (Yuv2RgbCoef3 << 12) | Yuv2RgbCoef2, 
			22u => (Reserved5 << 24) | (Yuv2RgbCoef5 << 12) | Yuv2RgbCoef4, 
			23u => (Reserved6 << 24) | (Yuv2RgbCoef7 << 12) | Yuv2RgbCoef6, 
			24u => (ConstPadCh0 << 24) | (Yuv2RgbCoef11 << 12) | Yuv2RgbCoef10, 
			25u => (Reserved7 << 28) | (Sign << 27) | (CmdId << 26) | (DstInd << 25) | (CrcInd << 24) | (ConstPadCh3 << 16) | (ConstPadCh2 << 8) | ConstPadCh1, 
			26u => (Reserved8 << 24) | (Yuv2RgbCoef9 << 12) | Yuv2RgbCoef8, 
			27u => Reserved9, 
			28u => (PadB << 16) | PadT, 
			29u => (PadR << 16) | PadL, 
			30u => (SrcHeightShape << 16) | SrcWidthShape, 
			31u => (CscEn << 31) | (IntrMask << 30) | (DstHeightShape << 16) | DstWidthShape, 
			32u => M2, 
			33u => M5, 
			34u => (SrcY << 16) | SrcX, 
			35u => (Ai2dCalcEnable << 31) | (Reserved10 << 29) | (DstY << 16) | DstX, 
			_ => 0u, 
		};
	}

	public string ToString(uint idx)
	{
		switch (idx)
		{
		case 0u:
			return "src_ch0_ptr: " + SrcCh0Ptr;
		case 1u:
			return "src_ch1_ptr: " + SrcCh1Ptr;
		case 2u:
			return "src_ch2_ptr: " + SrcCh2Ptr;
		case 3u:
			return "src_ch3_ptr: " + SrcCh3Ptr;
		case 4u:
			return "dst_ch0_ptr: " + DstCh0Ptr;
		case 5u:
			return "dst_ch1_ptr: " + DstCh1Ptr;
		case 6u:
			return "dst_ch2_ptr: " + DstCh2Ptr;
		case 7u:
			return "dst_ch3_ptr: " + DstCh3Ptr;
		case 8u:
			return "src_ch1_width_layout: " + SrcCh1WidthLayout + "\nsrc_ch0_width_layout: " + SrcCh0WidthLayout;
		case 9u:
			return "src_ch3_width_layout: " + SrcCh3WidthLayout + "\nsrc_ch2_width_layout: " + SrcCh2WidthLayout;
		case 10u:
			return "dst_ch1_width_layout: " + DstCh1WidthLayout + "\ndst_ch0_width_layout: " + DstCh0WidthLayout;
		case 11u:
			return "dst_ch3_width_layout: " + DstCh3WidthLayout + "\ndst_ch2_width_layout: " + DstCh2WidthLayout;
		case 12u:
		{
			FP32 fP6 = default(FP32);
			fP6.F = 0f;
			fP6.U = M0;
			return "M0: " + fP6.F;
		}
		case 13u:
		{
			FP32 fP5 = default(FP32);
			fP5.F = 0f;
			fP5.U = M1;
			return "M1: " + fP5.F;
		}
		case 14u:
		{
			FP32 fP4 = default(FP32);
			fP4.F = 0f;
			fP4.U = M3;
			return "M3: " + fP4.F;
		}
		case 15u:
		{
			FP32 fP3 = default(FP32);
			fP3.F = 0f;
			fP3.U = M4;
			return "M4: " + fP3.F;
		}
		case 16u:
			return string.Empty;
		case 17u:
			return string.Empty;
		case 18u:
			return "dst_format: " + DstFormat + "\nSrcFormat: " + SrcFormat + "\nBoundInd: " + BoundInd + "\nshift: " + Shift + "\npad_mod: " + PadMod + "\ninterpolation: " + Interpolation + "\ncord_round: " + CordRound + "\ndst_channel: " + DstChannel + "\nchannel: " + Channel;
		case 19u:
			return "bound_smooth: " + BoundSmooth + "\nbound_val: " + BoundVal;
		case 20u:
			return "yuv2rgb_coef1: " + Yuv2rgbCoef1 + "\nyuv2rgb_coef0: " + Yuv2rgbCoef0;
		case 21u:
			return "yuv2rgb_coef3: " + Yuv2RgbCoef3 + "\nyuv2rgb_coef2: " + Yuv2RgbCoef2;
		case 22u:
			return "yuv2rgb_coef5: " + Yuv2RgbCoef5 + "\nyuv2rgb_coef4: " + Yuv2RgbCoef4;
		case 23u:
			return "yuv2rgb_coef7: " + Yuv2RgbCoef7 + "\nyuv2rgb_coef6: " + Yuv2RgbCoef6;
		case 24u:
			return "const_pad_ch0: " + ConstPadCh0 + "\nyuv2rgb_coef11: " + Yuv2RgbCoef11 + "\nyuv2rgb_coef10: " + Yuv2RgbCoef10;
		case 25u:
			return "sign: " + Sign + "\ncmd_id: " + CmdId + "\ndst_ind: " + DstInd + "\nsrc_ind: " + CrcInd + "\nconst_pad_ch3: " + ConstPadCh3 + "\nconst_pad_ch2: " + ConstPadCh2 + "\nconst_pad_ch1: " + ConstPadCh1;
		case 26u:
			return "yuv2rgb_coef9: " + Yuv2RgbCoef9 + "\nyuv2rgb_coef8: " + Yuv2RgbCoef8;
		case 27u:
			return string.Empty;
		case 28u:
			return "pad_b: " + PadB + "\npad_t: " + PadT;
		case 29u:
			return "pad_r: " + PadR + "\npad_l: " + PadL;
		case 30u:
			return "src_height_shape: " + SrcHeightShape + "\nsrc_width_shape: " + SrcWidthShape;
		case 31u:
			return "csc_en: " + CscEn + "\nintr_mask: " + IntrMask + "\ndst_height_shape: " + DstHeightShape + "\ndst_width_shape: " + DstWidthShape;
		case 32u:
		{
			FP32 fP2 = default(FP32);
			fP2.F = 0f;
			fP2.U = M2;
			return "M2: " + fP2.F;
		}
		case 33u:
		{
			FP32 fP = default(FP32);
			fP.F = 0f;
			fP.U = M5;
			return "M5: " + fP.F;
		}
		case 34u:
			return "SrcY: " + SrcY + "\nSrcX: " + SrcX;
		case 35u:
			return "Ai2dCalcEnable: " + Ai2dCalcEnable + "\nDstY: " + DstY + "\nDstX: " + DstX;
		default:
			return string.Empty;
		}
	}

	public float U32ToFloat(uint u)
	{
		FP32 fP = default(FP32);
		fP.F = 0f;
		fP.U = u;
		return fP.F;
	}

	public float OriginM(uint u)
	{
		return U32ToFloat(u) / 1024f;
	}
}
