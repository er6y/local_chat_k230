using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nncase.CodeGen.K230;

internal sealed class LinkableModule : ILinkableModule
{
	private const int _textAlignment = 8;

	private readonly Stream _rdata;

	private readonly IReadOnlyList<LinkableFunction> _functions;

	public LinkableModule(Stream rdata, IReadOnlyList<LinkableFunction> functions)
	{
		_rdata = rdata;
		_functions = functions;
	}

	public ILinkedModule Link(ILinkContext linkContext)
	{
		List<LinkedFunction> list = new List<LinkedFunction>();
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
		{
			foreach (LinkableFunction function in _functions)
			{
				binaryWriter.Flush();
				binaryWriter.AlignPosition(8L);
				long textBegin = binaryWriter.Position();
				function.Text.Position = 0L;
				function.Text.CopyTo(binaryWriter.BaseStream);
				list.Add(new LinkedFunction(function.Id, function.SourceFunction, (ulong)textBegin, (ulong)function.Text.Length, function.Sections));
			}
		}
		return new LinkedModule(list, memoryStream, _rdata);
	}
}
