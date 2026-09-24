using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nncase.IR;
using Nncase.Runtime;

namespace Nncase.CodeGen.K230;

public record GModelRTFunction(string name, Delegate handle) : IRTFunction
{
	public string Name
	{
		get
		{
			return name;
		}
		set
		{
		}
	}

	public Delegate Handle
	{
		get
		{
			return handle;
		}
		set
		{
		}
	}

	public IReadOnlyList<IRType> ParameterTypes { get; }

	public IRType ReturnType { get; }

	public ValueTask InitializeAsync()
	{
		throw new NotImplementedException();
	}

	public ValueTask UninitializeAsync()
	{
		throw new NotImplementedException();
	}

	public ValueTask InvokeAsync(IReadOnlyList<IValue> parameters, IValue ret)
	{
		throw new NotImplementedException();
	}
}
