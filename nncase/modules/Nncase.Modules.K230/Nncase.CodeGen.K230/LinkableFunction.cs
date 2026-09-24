using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

internal sealed class LinkableFunction : ILinkableFunction
{
	private readonly Stream _desc;

	public uint Id { get; }

	public BaseFunction SourceFunction { get; }

	public Stream Text { get; }

	public IEnumerable<FunctionRef> FunctionRefs => Enumerable.Empty<FunctionRef>();

	public IReadOnlyList<ILinkedSection> Sections { get; }

	public LinkableFunction(uint id, PrimFunction sourceFunction, Stream text, Stream desc)
	{
		Id = id;
		SourceFunction = sourceFunction;
		Text = text;
		_desc = desc;
		Sections = new LinkedSection[1]
		{
			new LinkedSection(_desc, ".desc", 0u, 8u, (uint)_desc.Length)
		};
	}

	public void DeCompile(TextWriter asmWriter, TextWriter bwWriter)
	{
		K230DeSerializerVisitor k230DeSerializerVisitor = new K230DeSerializerVisitor();
		using MemoryStream memoryStream = new MemoryStream();
		Text.CopyTo(memoryStream);
		k230DeSerializerVisitor.DeSerialize(asmWriter, bwWriter, memoryStream.ToArray(), SourceFunction.Name);
	}
}
