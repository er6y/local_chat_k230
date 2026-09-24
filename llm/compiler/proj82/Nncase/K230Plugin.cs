using DryIoc;
using Nncase.Hosting;

namespace Nncase;

public sealed class K230Plugin : IPlugin, IApplicationPart
{
	public void ConfigureServices(IRegistrator registrator)
	{
		registrator.AddK230();
	}
}
