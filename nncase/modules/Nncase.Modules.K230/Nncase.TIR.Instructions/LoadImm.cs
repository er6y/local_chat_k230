using System;
using System.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class LoadImm : InstructionOp, ISerializeInst, IEquatable<LoadImm?>
{
	public static readonly ParameterInfo Value = new ParameterInfo(typeof(LoadImm), 0, "value", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER Rd { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		uint num = ((TensorConst)call[Value]).Value.ToScalar<uint>();
		if (Rd != 0 && Rd != GP_REGISTER.invalid)
		{
			if (num < 2048 && num != 0)
			{
				Call call2 = I.ADDI(Rd, R.r0, num & 0x7FFu);
				((ADDI)call2.Target).Serialize(writer, call2);
				return;
			}
			uint num2 = ((num >> 12) + ((num >> 11) & 1)) & 0xFFFFFu;
			uint num3 = num & 0xFFFu;
			Call call3 = I.LUI(Rd, num2);
			Call call4 = I.ADDI(Rd, Rd, num3);
			((LUI)call3.Target).Serialize(writer, call3);
			((ADDI)call4.Target).Serialize(writer, call4);
		}
	}

	public LoadImm(GP_REGISTER rd)
	{
		Rd = rd;
	}

	public LoadImm With(GP_REGISTER? rd = null)
	{
		return new LoadImm(rd ?? Rd);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as LoadImm);
	}

	public bool Equals(LoadImm? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return Rd.Equals(other.Rd);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(Rd));
	}
}
