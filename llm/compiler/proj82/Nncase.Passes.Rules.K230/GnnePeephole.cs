using System.Collections.Generic;
using Nncase.IR;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

// T2 optimization passes over the emitted action/instruction stream.
// Straight-line only (current schedules emit no branches).
public static class GnnePeephole
{
	private static int? Eval(Expr e)
	{
		if (e is TensorConst tc)
		{
			try
			{
				return tc.Value.ToScalar<int>();
			}
			catch
			{
				return null;
			}
		}
		return null;
	}

	// action level: drop re-issued SS shape/stride packs with unchanged constant values
	public static List<GnneAction> Run(List<GnneAction> actions)
	{
		var shapeState = new (int?, int?, int?, int?)?[8];
		var strideState = new (int?, int?, int?)?[8];
		var outList = new List<GnneAction>(actions.Count);
		int dropped = 0;
		foreach (var a in actions)
		{
			switch (a.Name)
			{
			case GnneActionName.PackShapeReg:
			{
				var p = (GnneActionPackShapeReg)a;
				var t = (Eval(p.N.Value), Eval(p.C.Value), Eval(p.H.Value), Eval(p.W.Value));
				if (t.Item1.HasValue && t.Item2.HasValue && t.Item3.HasValue && t.Item4.HasValue && shapeState[p.Ss.Index] == t)
				{
					dropped++;
					break;
				}
				shapeState[p.Ss.Index] = t;
				outList.Add(a);
				break;
			}
			case GnneActionName.PackStrideReg:
			{
				var s = (GnneActionPackStrideReg)a;
				var t = (Eval(s.N.Value), Eval(s.C.Value), Eval(s.H.Value));
				if (t.Item1.HasValue && t.Item2.HasValue && t.Item3.HasValue && strideState[s.Ss.Index] == t)
				{
					dropped++;
					break;
				}
				strideState[s.Ss.Index] = t;
				outList.Add(a);
				break;
			}
			default:
				outList.Add(a);
				break;
			}
		}
		System.IO.File.AppendAllText("/root/kmini/peephole.log", "PEEPHOLE dropped=" + dropped + " of " + actions.Count + "|");
		return outList;
	}

	// instruction level: drop same-value LoadImm; rewrite arithmetic progressions as ADDI self-increment
	public static List<Call> RunInst(List<Call> insts)
	{
		var regVal = new Dictionary<int, int?>();
		var chain = new Dictionary<int, long[]>();
		var outList = new List<Call>(insts.Count);
		int dropped = 0;
		int inc = 0;
		foreach (var c in insts)
		{
			var t = c.Target;
			if (t is LoadImm li)
			{
				int rd = (int)li.Rd;
				if (rd >= 32)
				{
					outList.Add(c);
					continue;
				}
				int? v = null;
				try
				{
					if (c[LoadImm.Value] is TensorConst tc)
					{
						v = tc.Value.ToScalar<int>();
					}
				}
				catch
				{
				}
				if (!v.HasValue)
				{
					regVal[rd] = null;
					chain.Remove(rd);
					outList.Add(c);
					continue;
				}
				if (regVal.TryGetValue(rd, out var cur) && cur.HasValue && cur.Value == v.Value)
				{
					dropped++;
					continue;
				}
				if (chain.TryGetValue(rd, out var ch) && ch[2] >= 1 && v.Value - ch[0] == ch[1] && ch[1] >= -2048 && ch[1] <= 2047 && ch[1] != 0)
				{
					outList.Add(I.ADDI((GP_REGISTER)rd, (GP_REGISTER)rd, (Expr)unchecked((int)ch[1])));
					inc++;
					chain[rd][0] = v.Value;
					regVal[rd] = v;
					continue;
				}
				if (chain.TryGetValue(rd, out var ch2) && ch2[2] == 0 && v.Value != ch2[0])
				{
					chain[rd] = new long[3] { v.Value, v.Value - ch2[0], 1 };
					regVal[rd] = v;
					outList.Add(c);
					continue;
				}
				chain[rd] = new long[3] { v.Value, 0, 0 };
				regVal[rd] = v;
				outList.Add(c);
				continue;
			}
			if (t is LoadDDrAddr dd)
			{
				int rb = (int)dd.rd_basement;
				int rt = (int)dd.rd_target;
				if (rb < 32)
				{
					regVal[rb] = null;
					chain.Remove(rb);
				}
				if (rt < 32)
				{
					regVal[rt] = null;
					chain.Remove(rt);
				}
				outList.Add(c);
				continue;
			}
			if (t is LW lw)
			{
				int rd2 = (int)lw.rd;
				if (rd2 < 32)
				{
					regVal[rd2] = null;
					chain.Remove(rd2);
				}
				outList.Add(c);
				continue;
			}
			outList.Add(c);
		}
		System.IO.File.AppendAllText("/root/kmini/peephole.log", "INST dedup=" + dropped + " inc=" + inc + " of " + insts.Count + "|");
		return outList;
	}
}
