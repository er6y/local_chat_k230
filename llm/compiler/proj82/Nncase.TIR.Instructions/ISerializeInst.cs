using System.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public interface ISerializeInst
{
	void Serialize(BinaryWriter writer, Call call);
}
