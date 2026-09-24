using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nncase.IR;
using Nncase.Runtime;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

public sealed class CSourceGModelBuilder : IRTModule
{
	public GModelBuilder GModelBuilder;

	private PrimFunction? _entry;

	public ModuleType ModuleType => ModuleType.Create("k230");

	public byte[] Source
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool IsSerialized { get; private set; }

	public IReadOnlyList<IRTFunction> Functions
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string CSourceFilePath { get; private set; }

	public string AsmFilePath { get; private set; }

	public string AsmOutFilePath { get; private set; }

	public CallableType? Signature { get; set; }

	public PrimFunction Entry => _entry;

	public string Backend { get; set; }

	public ValueTask InitializeAsync()
	{
		throw new NotImplementedException();
	}

	public ValueTask UninitializeAsync()
	{
		throw new NotImplementedException();
	}

	private static string _save_csoure_context(Stream src)
	{
		string tempFileName = Nncase.CodeGen.CodeGenUtil.GetTempFileName(".c");
		using FileStream destination = File.Open(tempFileName, FileMode.Create, FileAccess.Write);
		src.CopyTo(destination);
		return tempFileName;
	}

	public CSourceGModelBuilder(Stream s)
		: this(_save_csoure_context(s))
	{
	}

	public CSourceGModelBuilder(string c_source_path, CallableType? signature = null)
	{
		CSourceFilePath = c_source_path;
		AsmFilePath = Nncase.CodeGen.CodeGenUtil.GetTempFileName(".s");
		AsmOutFilePath = AsmFilePath + ".o";
		IsSerialized = false;
		GModelBuilder = null;
		Signature = signature;
		Backend = "cmodel";
	}

	public string Dump(string name, string dumpDirPath)
	{
		string dumpDirPath2 = dumpDirPath;
		if (!Directory.Exists(dumpDirPath2))
		{
			Directory.CreateDirectory(dumpDirPath2);
		}
		if (!IsSerialized)
		{
			AsmFilePath = Path.Join(dumpDirPath2, name + Path.GetExtension(AsmFilePath));
			AsmOutFilePath = Path.Join(dumpDirPath2, name + Path.GetExtension(AsmOutFilePath));
			Serialize();
		}
		if (Path.GetDirectoryName(AsmFilePath) != dumpDirPath2)
		{
			Func<string, string, string, string> func = delegate(string _name, string indent, string old_path)
			{
				string text = Path.Join(dumpDirPath2, indent, _name + Path.GetExtension(old_path));
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Copy(old_path, text);
				return text;
			};
			AsmFilePath = func(name, "", AsmFilePath);
			AsmOutFilePath = func(name, "", AsmOutFilePath);
		}
		return GModelBuilder.Dump(name, dumpDirPath2);
	}

	private string getFullExePath(string exe_name)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("PATH");
		if (environmentVariable != null)
		{
			string[] array = environmentVariable.Split(OperatingSystem.IsWindows() ? ';' : ':');
			for (int i = 0; i < array.Length; i++)
			{
				string text = Path.Combine(array[i], exe_name);
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		return Path.Join(Path.GetDirectoryName(GetType().Assembly.Location), exe_name);
	}

	private void compile()
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder sb = new StringBuilder();
		StringWriter errWriter = new StringWriter(stringBuilder);
		try
		{
			StringWriter logWriter = new StringWriter(sb);
			try
			{
				using Process process = new Process();
				process.StartInfo.FileName = getFullExePath("chibicc");
				process.StartInfo.Arguments = $"{CSourceFilePath} -cc1 -cc1-input {CSourceFilePath} -cc1-output {AsmFilePath}";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
				{
					errWriter.WriteLine(e.Data);
				};
				process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs msg)
				{
					logWriter.Write(msg);
				};
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					throw new InvalidOperationException(stringBuilder.ToString());
				}
			}
			finally
			{
				if (logWriter != null)
				{
					((IDisposable)logWriter).Dispose();
				}
			}
		}
		finally
		{
			if (errWriter != null)
			{
				((IDisposable)errWriter).Dispose();
			}
		}
	}

	private void assemble()
	{
		string asmFilePath = AsmFilePath;
		string asmOutFilePath = AsmOutFilePath;
		Assembler assembler = new Assembler(asmFilePath);
		assembler.Assemble();
		assembler.Dump(asmOutFilePath);
	}

	public void Serialize()
	{
		if (!IsSerialized)
		{
			compile();
			assemble();
			AssemblyParser assemblyParser = new AssemblyParser(AsmOutFilePath, Signature);
			GModelBuilder = new GModelBuilder(null, assemblyParser);
			GModelBuilder.Serialize();
			IsSerialized = true;
		}
	}

	public Tensor[] Invoke(params Tensor[] inputs)
	{
		if (!IsSerialized)
		{
			Serialize();
		}
		GModelBuilder.Backend = Backend;
		return GModelBuilder.Invoke(inputs);
	}
}
