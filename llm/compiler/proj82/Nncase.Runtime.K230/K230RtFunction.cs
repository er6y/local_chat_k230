using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nncase.IR;

namespace Nncase.Runtime.K230;

public class K230RtFunction : IRTFunction
{
	public IReadOnlyList<IRType> ParameterTypes
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IRType ReturnType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ValueTask InitializeAsync()
	{
		throw new NotImplementedException();
	}

	public ValueTask InvokeAsync(IReadOnlyList<IValue> parameters, IValue ret)
	{
		throw new NotImplementedException();
	}

	public ValueTask UninitializeAsync()
	{
		throw new NotImplementedException();
	}
}
