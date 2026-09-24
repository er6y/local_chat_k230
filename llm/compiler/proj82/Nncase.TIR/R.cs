using System;
using Nncase.TIR.Instructions;

namespace Nncase.TIR;

public static class R
{
	public static readonly GP_REGISTER r0 = GP_REGISTER.x0;

	public static readonly GP_REGISTER rax = GP_REGISTER.x1;

	public static readonly GP_REGISTER rbx = GP_REGISTER.x2;

	public static readonly GP_REGISTER rdi = GP_REGISTER.x3;

	public static readonly GP_REGISTER rsi = GP_REGISTER.x4;

	public static readonly GP_REGISTER rdx = GP_REGISTER.x5;

	public static readonly GP_REGISTER rcx = GP_REGISTER.x6;

	public static readonly GP_REGISTER r7 = GP_REGISTER.x7;

	public static readonly GP_REGISTER r8 = GP_REGISTER.x8;

	public static readonly GP_REGISTER r9 = GP_REGISTER.x9;

	public static readonly GP_REGISTER r10 = GP_REGISTER.x10;

	public static readonly GP_REGISTER r11 = GP_REGISTER.x11;

	public static readonly GP_REGISTER r12 = GP_REGISTER.x12;

	public static readonly GP_REGISTER r13 = GP_REGISTER.x13;

	public static readonly GP_REGISTER rsp = GP_REGISTER.x14;

	public static readonly GP_REGISTER rbp = GP_REGISTER.x15;

	public static readonly GP_REGISTER rhp = GP_REGISTER.x16;

	public static readonly GP_REGISTER r17 = GP_REGISTER.x17;

	public static readonly GP_REGISTER r18 = GP_REGISTER.x18;

	public static readonly GP_REGISTER r19 = GP_REGISTER.x19;

	public static readonly GP_REGISTER r20 = GP_REGISTER.x20;

	public static readonly GP_REGISTER r21 = GP_REGISTER.x21;

	public static readonly GP_REGISTER r22 = GP_REGISTER.x22;

	public static readonly GP_REGISTER r23 = GP_REGISTER.x23;

	public static readonly GP_REGISTER r24 = GP_REGISTER.x24;

	public static readonly GP_REGISTER r25 = GP_REGISTER.x25;

	public static readonly GP_REGISTER r26 = GP_REGISTER.x26;

	public static readonly GP_REGISTER r27 = GP_REGISTER.x27;

	public static readonly GP_REGISTER r28 = GP_REGISTER.x28;

	public static readonly GP_REGISTER r29 = GP_REGISTER.x29;

	public static readonly GP_REGISTER r30 = GP_REGISTER.x30;

	public static readonly GP_REGISTER r31 = GP_REGISTER.x31;

	public static string Parse(GP_REGISTER reg)
	{
		return reg switch
		{
			GP_REGISTER.x0 => "R.r0", 
			GP_REGISTER.x1 => "R.rax", 
			GP_REGISTER.x2 => "R.rbx", 
			GP_REGISTER.x3 => "R.rdi", 
			GP_REGISTER.x4 => "R.rsi", 
			GP_REGISTER.x5 => "R.rdx", 
			GP_REGISTER.x6 => "R.rcx", 
			GP_REGISTER.x7 => "R.r7", 
			GP_REGISTER.x8 => "R.r8", 
			GP_REGISTER.x9 => "R.r9", 
			GP_REGISTER.x10 => "R.r10", 
			GP_REGISTER.x11 => "R.r11", 
			GP_REGISTER.x12 => "R.r12", 
			GP_REGISTER.x13 => "R.r13", 
			GP_REGISTER.x14 => "R.rsp", 
			GP_REGISTER.x15 => "R.rbp", 
			GP_REGISTER.x16 => "R.rhp", 
			GP_REGISTER.x17 => "R.r17", 
			GP_REGISTER.x18 => "R.r18", 
			GP_REGISTER.x19 => "R.r19", 
			GP_REGISTER.x20 => "R.r20", 
			GP_REGISTER.x21 => "R.r21", 
			GP_REGISTER.x22 => "R.r22", 
			GP_REGISTER.x23 => "R.r23", 
			GP_REGISTER.x24 => "R.r24", 
			GP_REGISTER.x25 => "R.r25", 
			GP_REGISTER.x26 => "R.r26", 
			GP_REGISTER.x27 => "R.r27", 
			GP_REGISTER.x28 => "R.r28", 
			GP_REGISTER.x29 => "R.r29", 
			GP_REGISTER.x30 => "R.r30", 
			GP_REGISTER.x31 => "R.r31", 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
