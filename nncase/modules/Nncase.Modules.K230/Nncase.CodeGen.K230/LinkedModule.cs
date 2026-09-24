using System.Collections.Generic;
using System.IO;
using Nncase.Runtime.K230;

namespace Nncase.CodeGen.K230;

internal sealed class LinkedModule : ILinkedModule
{
	public string ModuleKind => K230RtModule.Kind;

	public uint Version => K230RtModule.Version;

	public IReadOnlyList<ILinkedFunction> Functions { get; }

	public IReadOnlyList<ILinkedSection> Sections { get; }

	public LinkedModule(IReadOnlyList<ILinkedFunction> functions, Stream text, Stream rdata)
	{
		Functions = functions;
		Sections = new LinkedSection[2]
		{
			new LinkedSection(text, ".text", 0u, 8u, (uint)text.Length),
			new LinkedSection(rdata, ".rdata", 0u, 8u, (uint)rdata.Length)
		};
	}
}
