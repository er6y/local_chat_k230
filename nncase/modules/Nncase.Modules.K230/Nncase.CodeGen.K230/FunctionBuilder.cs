using System;
using System.Collections.Generic;
using System.IO;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.CodeGen.K230;

internal class FunctionBuilder : IDisposable
{
	private struct MemoryRange
	{
		public uint Start;

		public uint Size;
	}

	private struct DescHeader
	{
		public uint InputPoolSize;

		public uint OutputPoolSize;

		public uint Inputs;

		public uint Outputs;
	}

	private readonly uint _id;

	private readonly SectionManager _textSectionManager;

	private readonly BinaryWriter _textWriter;

	private readonly BinaryWriter _rdataWriter;

	public FunctionBuilder(uint id, BinaryWriter rdataWriter)
	{
		_id = id;
		_textSectionManager = new SectionManager();
		_textWriter = _textSectionManager.GetWriter(".text");
		_rdataWriter = rdataWriter;
	}

	public unsafe LinkableFunction Build(PrimFunction function)
	{
		new InstSerializeVisitor(_textWriter).Visit(function.Body);
		SectionManager sectionManager = new SectionManager();
		using (BinaryWriter writer = sectionManager.GetWriter(".desc"))
		{
			DescHeader descHeader = default(DescHeader);
			descHeader.InputPoolSize = 0u;
			descHeader.OutputPoolSize = 0u;
			descHeader.Inputs = 0u;
			descHeader.Outputs = 0u;
			DescHeader value = descHeader;
			long pos = writer.Position();
			writer.Skip((ulong)sizeof(DescHeader));
			ArrayExtensions.SpanValueEnumerable<Nncase.TIR.Buffer> spanValueEnumerable = function.Parameters.AsValueEnumerable();
			ArrayExtensions.SpanWhereEnumerable<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> spanWhereEnumerable = spanValueEnumerable.Where((Nncase.TIR.Buffer buf) => buf.MemSpan.Location == MemoryLocation.Input);
			WhereEnumerator<Nncase.TIR.Buffer, FunctionWrapper<Nncase.TIR.Buffer, bool>> enumerator = spanWhereEnumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Nncase.TIR.Buffer current = enumerator.Current;
				value.Inputs++;
				MemoryRange memoryRange = default(MemoryRange);
				MemoryRange value2;
				checked
				{
					memoryRange.Start = (uint)current.Start();
					memoryRange.Size = (uint)current.Size();
					value2 = memoryRange;
					writer.Write(ref value2);
				}
				value.InputPoolSize = Math.Max(value.InputPoolSize, value2.Start + value2.Size);
			}
			spanValueEnumerable = function.Parameters.AsValueEnumerable();
			spanWhereEnumerable = spanValueEnumerable.Where((Nncase.TIR.Buffer buf) => buf.MemSpan.Location == MemoryLocation.Output);
			enumerator = spanWhereEnumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Nncase.TIR.Buffer current2 = enumerator.Current;
				value.Outputs++;
				MemoryRange memoryRange = default(MemoryRange);
				MemoryRange value3;
				checked
				{
					memoryRange.Start = (uint)current2.Start();
					memoryRange.Size = (uint)current2.Size();
					value3 = memoryRange;
					writer.Write(ref value3);
				}
				value.OutputPoolSize = Math.Max(value.OutputPoolSize, value3.Start + value3.Size);
			}
			writer.Position(pos);
			writer.Write(ref value);
		}
		foreach (KeyValuePair<Const, ValueRange<long>> rdata in function.SchedResult.Rdatas)
		{
			rdata.Deconstruct(out var key, out var value4);
			Const @const = key;
			ValueRange<long> valueRange = value4;
			Span<byte> bytesBuffer = ((TensorConst)@const).Value.BytesBuffer;
			if ((uint)bytesBuffer.Length != valueRange.Max - valueRange.Min)
			{
				throw new InvalidDataException("The Buffer Szie Not Equal!");
			}
			_rdataWriter.Position((uint)valueRange.Min);
			_rdataWriter.Write(bytesBuffer);
		}
		return new LinkableFunction(_id, function, _textSectionManager.GetContent(".text"), sectionManager.GetContent(".desc"));
	}

	public void Dispose()
	{
	}
}
