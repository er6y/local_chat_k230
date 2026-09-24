using Nncase.Evaluator;
using Nncase.IR;

namespace Nncase.Passes;

internal sealed class MetricEvaluateContext : IMetricEvaluateContext
{
	public Call CurrentCall { get; }

	public MetricEvaluateContext(Call call)
	{
		CurrentCall = call;
	}

	public T GetArgument<T>(Op op, ParameterInfo parameter) where T : Expr
	{
		return (T)CurrentCall[parameter];
	}

	public T GetArgumentType<T>(Op op, ParameterInfo parameter) where T : IRType
	{
		return (T)CurrentCall[parameter].CheckedType;
	}

	public T GetReturnType<T>() where T : IRType
	{
		return (T)CurrentCall.CheckedType;
	}
}
