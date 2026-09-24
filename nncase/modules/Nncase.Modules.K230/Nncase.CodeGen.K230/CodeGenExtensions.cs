using System.Linq;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

public static class CodeGenExtensions
{
	public static int Size(this Buffer buffer)
	{
		return ((TensorConst)buffer.MemSpan.Size).Value.ToScalar<int>();
	}

	public static ulong Start(this Buffer buffer)
	{
		return ((TensorConst)buffer.MemSpan.Start).Value.ToScalar<ulong>();
	}

	public static int[] Shape(this Buffer buffer)
	{
		return (from d in buffer.Dimensions.ToArray()
			select ((TensorConst)d).Value.ToScalar<int>()).ToArray();
	}
}
