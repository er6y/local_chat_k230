using System.Runtime.InteropServices;

namespace Nncase.Passes.Rules.K230;

[StructLayout(LayoutKind.Explicit)]
public struct FP32
{
	[FieldOffset(0)]
	public uint U;

	[FieldOffset(0)]
	public float F;
}
