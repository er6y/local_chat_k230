using DryIoc;
using Nncase.Hosting;
using Nncase.Targets;

namespace Nncase;

internal sealed class K230CoreModule : IApplicationPart
{
	public void ConfigureServices(IRegistrator registrator)
	{
		registrator.Register<ITarget, K230Target>(Reuse.Singleton);
		registrator.Register<ValueType, QuantizeParamType>(Reuse.Singleton);
		registrator.Register<ValueType, DeQuantizeParamType>(Reuse.Singleton);
		registrator.Register<ValueType, CropBBoxType>(Reuse.Singleton);
	}
}
