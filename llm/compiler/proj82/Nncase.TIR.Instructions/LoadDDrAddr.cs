using System;
using System.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class LoadDDrAddr : InstructionOp, ISerializeInst, IEquatable<LoadDDrAddr?>
{
	public static readonly ParameterInfo Basement = new ParameterInfo(typeof(LoadDDrAddr), 0, "basement", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo Offset = new ParameterInfo(typeof(LoadDDrAddr), 1, "offset", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd_target { get; }

	public GP_REGISTER rd_basement { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		Call call2 = I.LW(rd_basement, GP_REGISTER.x0, ((TensorConst)call[Basement]).Value.ToScalar<int>() * 4);
		Call call3 = I.LoadImm(rd_target, ((TensorConst)call[Offset]).Value.ToScalar<int>());
		Call call4 = I.ADD(rd_target, rd_basement, rd_target);
		((LW)call2.Target).Serialize(writer, call2);
		((LoadImm)call3.Target).Serialize(writer, call3);
		((ADD)call4.Target).Serialize(writer, call4);
	}

	public LoadDDrAddr(GP_REGISTER rdTarget, GP_REGISTER rdBasement)
	{
		rd_target = rdTarget;
		rd_basement = rdBasement;
	}

	public LoadDDrAddr With(GP_REGISTER? rdTarget = null, GP_REGISTER? rdBasement = null)
	{
		return new LoadDDrAddr(rdTarget ?? rd_target, rdBasement ?? rd_basement);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as LoadDDrAddr);
	}

	public bool Equals(LoadDDrAddr? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rd_target.Equals(other.rd_target))
		{
			return rd_basement.Equals(other.rd_basement);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd_target, rd_basement));
	}
}
