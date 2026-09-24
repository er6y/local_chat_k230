using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Nncase.Simulator;

public sealed class GModelEngine
{
	private string _exe;

	private string _workingDirectory;

	private string _logs;

	public GModelEngine(string exe = "cmodel", string working_directory = "")
	{
		_exe = exe;
		_workingDirectory = working_directory;
		_logs = string.Empty;
	}

	public void Run(string arguments)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		StringWriter errWriter = new StringWriter(stringBuilder);
		try
		{
			StringWriter logWriter = new StringWriter(stringBuilder2);
			try
			{
				using Process process = new Process();
				process.StartInfo.FileName = _exe;
				if (_workingDirectory != string.Empty)
				{
					process.StartInfo.WorkingDirectory = _workingDirectory;
				}
				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
				{
					errWriter.WriteLine(e.Data);
				};
				process.OutputDataReceived += delegate(object _, DataReceivedEventArgs msg)
				{
					logWriter.Write(msg);
				};
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.StandardInput.WriteLine("Start!");
				process.StandardInput.Close();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					throw new InvalidOperationException(stringBuilder.ToString());
				}
				_logs = stringBuilder2.ToString();
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
}
