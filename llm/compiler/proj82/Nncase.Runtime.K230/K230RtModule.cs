using System.Collections.Generic;

namespace Nncase.Runtime.K230;

public class K230RtModule : RTModule
{
	public static readonly string Kind = "k230";

	public static readonly uint Version = 1u;

	public K230RtModule(IReadOnlyList<IRTFunction> functions)
		: base(functions)
	{
	}
}
