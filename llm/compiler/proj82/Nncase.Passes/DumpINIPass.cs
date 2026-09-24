using System.Threading.Tasks;
using Nncase.Diagnostics;
using Nncase.IR;

namespace Nncase.Passes;

internal sealed class DumpINIPass : ModulePass
{
	protected override Task<IRModule> RunCoreAsync(IRModule module, RunPassContext options)
	{
		if (DumpScope.Current.IsEnabled(DumpFlags.PassIR))
		{
			new INIVisitor(DumpScope.Current.Directory + "/fake_model").Visit(module.Entry);
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
