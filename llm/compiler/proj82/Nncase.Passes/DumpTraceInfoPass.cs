using System.IO;
using System.Threading.Tasks;
using Nncase.Diagnostics;
using Nncase.IR;

namespace Nncase.Passes;

internal sealed class DumpTraceInfoPass : ModulePass
{
	protected override Task<IRModule> RunCoreAsync(IRModule module, RunPassContext options)
	{
		if (DumpScope.Current.IsEnabled(DumpFlags.PassIR))
		{
			TraceInfoVisitor traceInfoVisitor = new TraceInfoVisitor();
			traceInfoVisitor.Visit(module.Entry);
			using (StreamWriter writer = new StreamWriter(DumpScope.Current.OpenFile("trace_info.py")))
			{
				traceInfoVisitor.DumpTraceInfo(writer);
			}
			using (StreamWriter writer2 = new StreamWriter(DumpScope.Current.OpenFile("draw_trace.py")))
			{
				traceInfoVisitor.DumpDrawTrace(writer2);
			}
			using (StreamWriter writer3 = new StreamWriter(DumpScope.Current.OpenFile("draw_roofline.py")))
			{
				traceInfoVisitor.DumpDrawRoofLine(writer3);
			}
			using StreamWriter writer4 = new StreamWriter(DumpScope.Current.OpenFile("estimate_fps.py"));
			traceInfoVisitor.DumpEstimateFps(writer4);
		}
		return Task.FromResult(module);
	}

	protected override Task OnPassStartAsync(IRModule input, RunPassContext context)
	{
		return Task.CompletedTask;
	}

	protected override Task OnPassEndAsync(IRModule post, RunPassContext context)
	{
		return Task.CompletedTask;
	}
}
