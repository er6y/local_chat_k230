using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.Runtime.K230;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

public sealed class ModuleBuilder : IModuleBuilder, IDisposable
{
	private readonly SectionManager _sectionManager;

	private readonly BinaryWriter _rdataWriter;

	public CompileOptions CompileOptions { get; }

	public string ModuleKind => K230RtModule.Kind;

	public ModuleBuilder(CompileOptions options)
	{
		_sectionManager = new SectionManager();
		_rdataWriter = _sectionManager.GetWriter(".rdata");
		CompileOptions = options;
	}

	public ILinkableModule Build(IReadOnlyList<BaseFunction> functions)
	{
		LinkableFunction[] array = functions.OfType<PrimFunction>().Select((PrimFunction f, int i) => new FunctionBuilder((uint)i, _rdataWriter).Build(f)).ToArray();
		_rdataWriter.Flush();
		if (CompileOptions.DumpFlags.HasFlag(DumpFlags.CodeGen))
		{
			string dump_root = Path.Join(CompileOptions.DumpDir, ModuleKind);
			foreach (LinkableFunction item in array.OfType<LinkableFunction>())
			{
				DumpAsm(dump_root, item);
			}
		}
		return new LinkableModule(_sectionManager.GetContent(".rdata"), array);
	}

	public void Dispose()
	{
		((IDisposable)_sectionManager.GetContent(".rdata")).Dispose();
	}

	private void DumpAsm(string dump_root, LinkableFunction function)
	{
		string text = Path.Join(dump_root, function.SourceFunction.Name);
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		using FileStream stream = File.OpenWrite(Path.Join(text, "compile.text.asm"));
		using FileStream stream2 = File.OpenWrite(Path.Join(text, "ddr_bandwidth.csv"));
		using StreamWriter asmWriter = new StreamWriter(stream);
		using StreamWriter bwWriter = new StreamWriter(stream2);
		function.DeCompile(asmWriter, bwWriter);
	}
}
