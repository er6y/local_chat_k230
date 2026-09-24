using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nncase.Passes.Rules;

public sealed record ActivationPartialParameter<T>(int Start, int SizePreParam, Tensor<T> Parent) where T : unmanaged, IEquatable<T>
{
	private T this[params int[] indices]
	{
		get
		{
			return this[indices.AsSpan()];
		}
		set
		{
			this[indices.AsSpan()] = value;
		}
	}

	private T this[ReadOnlySpan<int> indices]
	{
		get
		{
			if (indices[indices.Length - 1] - Start >= SizePreParam)
			{
				throw new NotSupportedException();
			}
			return Parent[indices];
		}
		set
		{
			if (indices[indices.Length - 1] - Start >= SizePreParam)
			{
				throw new NotSupportedException();
			}
			Parent[indices] = value;
		}
	}

	public void Fill(T v)
	{
		ForEach((IReadOnlyList<int> _, T _) => v);
	}

	public void ForEach(Func<IReadOnlyList<int>, T, T> callable)
	{
		Func<IReadOnlyList<int>, T, T> callable2 = callable;
		(from d in Parent.Dimensions.ToArray().SkipLast(1)
			select Enumerable.Range(0, d)).Append(Enumerable.Range(0, SizePreParam)).CartesianProduct().AsParallel()
			.ForAll(delegate(IEnumerable<int> em)
			{
				int[] array = em.ToArray();
				array[^1] += Start;
				T arg = this[array];
				array[^1] -= Start;
				T value = callable2(array, arg);
				array[^1] += Start;
				this[array] = value;
			});
	}

	[CompilerGenerated]
	private bool PrintMembers(StringBuilder builder)
	{
		RuntimeHelpers.EnsureSufficientExecutionStack();
		builder.Append("Start = ");
		builder.Append(Start.ToString());
		builder.Append(", SizePreParam = ");
		builder.Append(SizePreParam.ToString());
		builder.Append(", Parent = ");
		builder.Append(Parent);
		return true;
	}
}
