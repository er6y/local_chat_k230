using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nncase.IR;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

// T3-A v3.2: LOCALIZED subroutine sharing.
//
// Measured facts driving this design (2026-09-22 board + sim):
//  - conditional branches (BEQ/BNE) are UNUSABLE on the closed .a runtime
//    (stream output goes zero) - official streams never use them either;
//    counted loops are therefore impossible. Official models are STATIC
//    UNROLL + JAL ladder + JALR shared subroutines with SHORT jumps (-64B).
//  - instruction issue ~14ns; our v3.1 subroutine version was 10x SLOWER
//    because its bodies were parked up to ~150KB away (~1us per round
//    trip); v3.2 flushes a fresh copy of the bodies every ~ChunkBudget
//    bytes so every call->body distance stays short.
//  - pseudo-instructions serialize with VALUE-DEPENDENT length (TrueSizeOf).
//
// Order-preserving: const-run slots replaced by JAL calls into shared
// bodies; guard JAL (rd=rlink) keeps linear pc out of body blocks.
public static class GnneLoopify
{
	private const int MinPeriods = 16;
	private const int MaxPeriod = 4096;
	private const int MinRunLen = 2;
	private const long ChunkBudget = 3072;
	private static readonly HashSet<string> TwoByte = new()
	{
		"AI2D_COMPUTE", "CCR_CLR", "CCR_DECL", "CCR_SET", "DM_CONF_BROADCAST",
		"END", "FENCE", "FENCE_I", "INTR", "MMU_SETID", "PU_COMPUTE", "PU_PDP0_COMPUTE"
	};

	private sealed class InstInfo
	{
		public Call Call = null!;
		public string TypeName = "";
		public string Sig = "";
		public string Key = "";
		public int Size = 4;
	}

	private sealed class Seg
	{
		public int P;
		public int Start;
		public int K;
		public int End { get { return Start + K * P; } }
		public int Rlink = -1;
		public int[] RunOfSlot = new int[0];
		public List<(int start, int len)> Runs = new();
		public HashSet<int> BodySlots = new();
		public HashSet<int> Clobbers = new();
	}

	private static void Log(string s)
	{
		System.IO.File.AppendAllText("/root/kmini/loopify.log", s + "|");
	}

	private static List<Nncase.IR.ParameterInfo> ParamsOf(Type t)
	{
		return t.GetFields(BindingFlags.Static | BindingFlags.Public)
			.Where(f => f.FieldType == typeof(Nncase.IR.ParameterInfo))
			.Select(f => (Nncase.IR.ParameterInfo)f.GetValue(null)!)
			.OrderBy(p => p.Index).ToList();
	}

	private static int TrueSizeOf(Call c)
	{
		var tt = c.Target.GetType();
		string nm = tt.Name;
		if (nm == "LoadImm")
		{
			var rd = (int)(GP_REGISTER)tt.GetProperty("Rd")!.GetValue(c.Target)!;
			uint v = 0;
			var ps = ParamsOf(tt);
			if (ps.Count > 0 && c[ps[0]] is TensorConst tc)
			{
				try { v = unchecked((uint)tc.Value.ToScalar<int>()); } catch { }
			}
			if (rd == 0 || rd == 32)
			{
				return 0;
			}
			return (v < 2048 && v != 0) ? 4 : 8;
		}
		if (nm == "LoadDDrAddr")
		{
			var rt = (int)(GP_REGISTER)tt.GetProperty("rd_target")!.GetValue(c.Target)!;
			int off = 0;
			var ps = ParamsOf(tt);
			if (ps.Count > 1 && c[ps[1]] is TensorConst tc2)
			{
				try { off = tc2.Value.ToScalar<int>(); } catch { }
			}
			int inner = (rt == 0 || rt == 32) ? 0 : ((unchecked((uint)off) < 2048 && off != 0) ? 4 : 8);
			return 4 + inner + 4;
		}
		return TwoByte.Contains(nm) ? 2 : 4;
	}

	private static InstInfo Analyze(Call c, int seq)
	{
		var info = new InstInfo();
		info.Call = c;
		var tt = c.Target.GetType();
		info.TypeName = tt.Name;
		info.Size = TwoByte.Contains(info.TypeName) ? 2 : 4;
		var sig = new StringBuilder(info.TypeName);
		var key = new StringBuilder(info.TypeName);
		foreach (var p in tt.GetProperties())
		{
			if (p.PropertyType == typeof(GP_REGISTER))
			{
				int r = (int)(GP_REGISTER)p.GetValue(c.Target)!;
				bool isDef = p.Name.ToLowerInvariant().Contains("rd");
				sig.Append('|').Append(p.Name).Append(isDef ? "=d" : "=u");
				key.Append('|').Append(p.Name).Append('=').Append(r);
			}
			else if (p.PropertyType.IsEnum || p.PropertyType == typeof(int))
			{
				key.Append('|').Append(p.Name).Append('=').Append(p.GetValue(c.Target));
			}
		}
		foreach (var pi in ParamsOf(tt))
		{
			var e = c[pi];
			if (e is TensorConst tc)
			{
				try { key.Append("|c").Append(tc.Value.ToScalar<int>()); }
				catch { key.Append("|cx"); }
			}
			else if (e is Call)
			{
				key.Append("|e").Append(seq);
			}
			else
			{
				key.Append("|n");
			}
		}
		if (info.TypeName == "LoadImm" || info.TypeName == "LoadDDrAddr")
		{
			info.Size = TrueSizeOf(c);
		}
		info.Sig = sig.ToString();
		info.Key = key.ToString();
		return info;
	}

	private static List<(string name, int reg, bool isDef)> GetRegs(InstInfo info)
	{
		var res = new List<(string name, int reg, bool isDef)>();
		foreach (var p in info.Call.Target.GetType().GetProperties())
		{
			if (p.PropertyType == typeof(GP_REGISTER))
			{
				int r = (int)(GP_REGISTER)p.GetValue(info.Call.Target)!;
				res.Add((p.Name, r, p.Name.ToLowerInvariant().Contains("rd")));
			}
		}
		return res;
	}

	public static List<Call> Run(List<Call> insts)
	{
		try
		{
			var res = RunInner(insts);
			Log("LOOPIFY done");
			return res;
		}
		catch (Exception e)
		{
			Log("LOOPIFY EXC " + e.GetType().Name + " " + e.Message.Replace('\n', ' '));
			return insts;
		}
	}

	private static List<Call> RunInner(List<Call> insts)
	{
		string jn = Environment.GetEnvironmentVariable("NNCASE_LOOPIFY_MICROJALN");
		if (!string.IsNullOrEmpty(jn))
		{
			// N harmless JAL +4 (jump-to-next) probes placed right after
			// CCR_CLR (synced) or at arbitrary positions; rd = register not
			// read within the next 256 instructions. Board timing of the two
			// variants separates jump cost from pipeline-drain cost.
			int nn2 = int.Parse(jn);
			bool syncOnly = nn2 >= 10000;
			int count = nn2 % 10000;   // 2001 = sync variant, 2000 = any
			var probeInfos = new List<InstInfo>(insts.Count);
			for (int i = 0; i < insts.Count; i++)
			{
				probeInfos.Add(Analyze(insts[i], i));
			}
			// proper liveness: reg is clobber-safe before inst i iff not live-in at i
			var liveIn = new int[insts.Count + 1];
			int live = 0;
			liveIn[insts.Count] = 0;
			for (int i = insts.Count - 1; i >= 0; i--)
			{
				foreach (var pr in GetRegs(probeInfos[i]))
				{
					if (pr.isDef) live &= ~(1 << pr.reg);
					else live |= (1 << pr.reg);
				}
				liveIn[i] = live;
			}
			var sites = new List<int>();
			for (int i = 0; i < insts.Count && sites.Count < count; i++)
			{
				bool isSync = probeInfos[i].TypeName == "CCR_CLR";
				if (syncOnly ? isSync : (i % 97 == 0))
				{
					sites.Add(i + 1);
				}
			}
			var siteSet = new HashSet<int>(sites.Where(x => x < insts.Count));
			var lst = new List<Call>(insts.Count + siteSet.Count);
			int placed = 0;
			var pool = new HashSet<int>();
			for (int i = 0; i < insts.Count; i++)
			{
				if (siteSet.Contains(i))
				{
					int dead = ~liveIn[i] & unchecked((int)0xFFFFFFFE);   // exclude x0
					int rd = -1;
					for (int cand = 31; cand >= 5 && rd < 0; cand--)
					{
						if (!pool.Contains(cand) && (dead & (1 << cand)) != 0) rd = cand;
					}
					if (rd >= 0)
					{
						bool withRet = Environment.GetEnvironmentVariable("NNCASE_LOOPIFY_MICROJALRET") == "1";
						lst.Add(I.JAL((GP_REGISTER)rd, (Expr)(withRet ? 8 : 4)));
						if (withRet)
						{
							lst.Add(I.JALR((GP_REGISTER)rd, (GP_REGISTER)rd, (Expr)0, (Expr)0));
						}
						pool.Add(rd);
						placed++;
						if (pool.Count > 20) pool.Clear();
					}
				}
				lst.Add(insts[i]);
			}
			Log("MICROJALN placed=" + placed + " of " + count + (syncOnly ? " sync" : " any"));
			return lst;
		}
		var infos = new List<InstInfo>(insts.Count);
		for (int i = 0; i < insts.Count; i++)
		{
			infos.Add(Analyze(insts[i], i));
		}
		int n = infos.Count;
		var sigs = new string[n];
		for (int i = 0; i < n; i++)
		{
			sigs[i] = infos[i].Sig;
		}
		int w0 = 1500;
		if (n < w0 + 16 * 8)
		{
			return insts;
		}
		var segs = new List<Seg>();
		int pos = w0;
		while (pos + 14 * 8 <= n)
		{
			int bestP = 0, bestA = -1, bestK = 0;
			for (int p = 4; p <= MaxPeriod; p++)
			{
				if (pos + 14 * p > n)
				{
					break;
				}
				for (int a = pos; a < pos + 2 * p; a++)
				{
					if (a + 13 * p > n)
					{
						break;
					}
					bool ok = true;
					for (int i = a; i < a + 13 * p; i++)
					{
						if (sigs[i] != sigs[i + p])
						{
							ok = false;
							break;
						}
					}
					if (!ok)
					{
						continue;
					}
					int k = 13;
					while (a + (k + 1) * p <= n)
					{
						int pb = a + k * p;
						bool ok2 = true;
						for (int j = 0; j < p; j++)
						{
							if (sigs[pb + j] != sigs[a + j])
							{
								ok2 = false;
								break;
							}
						}
						if (!ok2)
						{
							break;
						}
						k++;
					}
					if ((long)k * p > (long)bestK * bestP || ((long)k * p == (long)bestK * bestP && p > bestP))
					{
						bestK = k;
						bestP = p;
						bestA = a;
					}
				}
			}
			if (bestP == 0 || bestK < MinPeriods)
			{
				break;
			}
			var seg = new Seg { P = bestP, Start = bestA, K = bestK, RunOfSlot = new int[bestP] };
			var constSlot = new bool[bestP];
			int nConst = 0;
			for (int j = 0; j < bestP; j++)
			{
				seg.RunOfSlot[j] = -1;
				string k0 = infos[bestA + j].Key;
				bool same = true;
				for (int m = 1; m < bestK; m++)
				{
					if (infos[bestA + m * bestP + j].Key != k0)
					{
						same = false;
						break;
					}
				}
				constSlot[j] = same;
				if (same) nConst++;
			}
			int j0 = 0;
			while (j0 < bestP)
			{
				if (!constSlot[j0])
				{
					j0++;
					continue;
				}
				int j1 = j0;
				while (j1 < bestP && constSlot[j1])
				{
					j1++;
				}
				if (j1 - j0 >= MinRunLen)
				{
					for (int j = j0; j < j1; j++)
					{
						seg.RunOfSlot[j] = seg.Runs.Count;
					}
					seg.Runs.Add((j0, j1 - j0));
				}
				j0 = j1;
			}
			Log("SEG p=" + bestP + " a=" + bestA + " K=" + bestK + " nConst=" + nConst + "/" + bestP + " runs=" + seg.Runs.Count);
			if (seg.Runs.Count > 0)
			{
				segs.Add(seg);
			}
			pos = bestA + bestK * bestP;
		}
		if (segs.Count == 0)
		{
			Log("FN n=" + n + " result=nosegs");
			return insts;
		}
		// clobber set: run starts + ALL period starts (guards may appear at
		// any chunk boundary; being conservative only reduces eligible regs)
		foreach (var seg in segs)
		{
			for (int m = 0; m < seg.K; m++)
			{
				int pb = seg.Start + m * seg.P;
				seg.Clobbers.Add(pb);
				foreach (var rr in seg.Runs)
				{
					for (int j = rr.start; j < rr.start + rr.len; j++)
					{
						seg.BodySlots.Add(pb + j);
					}
					seg.Clobbers.Add(pb + rr.start);
				}
			}
		}
		foreach (var seg in segs)
		{
			for (int cand = 31; cand >= 1 && seg.Rlink < 0; cand--)
			{
				long lastDef = -1, lastClob = -1;
				bool bad = false;
				for (int i = seg.Start; i < n && !bad; i++)
				{
					if (seg.Clobbers.Contains(i))
					{
						lastClob = i;
					}
					bool inBody = seg.BodySlots.Contains(i);
					foreach (var pr in GetRegs(infos[i]))
					{
						if (pr.reg != cand)
						{
							continue;
						}
						if (!pr.isDef)
						{
							if (lastClob > lastDef)
							{
								bad = true;
								break;
							}
						}
						else if (inBody)
						{
							bad = true;
							break;
						}
						else
						{
							lastDef = i;
						}
					}
				}
				if (!bad)
				{
					seg.Rlink = cand;
				}
			}
		}
		var okSegs = segs.Where(g => g.Rlink >= 0).ToList();
		Log("FN n=" + n + " segs=" + segs.Count + " ok=" + okSegs.Count +
			" rlinks=" + string.Join(",", segs.Select(g => g.Rlink).ToArray()));
		segs = okSegs;
		if (segs.Count == 0)
		{
			return insts;
		}
		var outList = new List<Call>(insts.Count);
		long bytes = 0;
		for (int i = 0; i < w0; i++)
		{
			outList.Add(infos[i].Call);
			bytes += infos[i].Size;
		}
		int prevEnd = w0;
		foreach (var seg in segs)
		{
			int spanEnd = seg.End;
			for (int i = prevEnd; i < seg.Start; i++)
			{
				outList.Add(infos[i].Call);
				bytes += infos[i].Size;
			}
			var chunkJals = new List<(int listPos, long bytePos, int runIdx)>();
			long chunkBytes0 = bytes;
			for (int m = 0; m < seg.K; m++)
			{
				int pbase = seg.Start + m * seg.P;
				for (int j = 0; j < seg.P; j++)
				{
					int rid = seg.RunOfSlot[j];
					if (rid >= 0)
					{
						if (j == seg.Runs[rid].start)
						{
							chunkJals.Add((outList.Count, bytes, rid));
							outList.Add(I.JAL((GP_REGISTER)seg.Rlink, (Expr)0));
							bytes += 4;
						}
						continue;
					}
					outList.Add(infos[pbase + j].Call);
					bytes += infos[pbase + j].Size;
				}
				if (bytes - chunkBytes0 >= ChunkBudget || m == seg.K - 1)
				{
					FlushChunk(outList, chunkJals, seg, infos, ref bytes);
					chunkJals = new List<(int listPos, long bytePos, int runIdx)>();
					chunkBytes0 = bytes;
				}
			}
			prevEnd = spanEnd;
		}
		for (int i = prevEnd; i < n; i++)
		{
			outList.Add(infos[i].Call);
			bytes += infos[i].Size;
		}
		long recount = 0;
		foreach (var c in outList)
		{
			recount += TrueSizeOf(c);
		}
		Log("FN n=" + n + " streamBytes=" + bytes + " recount=" + recount + " listN=" + outList.Count + " result=OK");
		return outList;
	}

	private static void FlushChunk(List<Call> outList, List<(int listPos, long bytePos, int runIdx)> chunkJals,
		Seg seg, List<InstInfo> infos, ref long bytes)
	{
		if (chunkJals.Count == 0)
		{
			return;
		}
		int rlink = seg.Rlink;
		long guardByte = bytes;
		int guardPos = outList.Count;
		outList.Add(I.JAL((GP_REGISTER)rlink, (Expr)0));
		bytes += 4;
		var used = new List<int>();
		var seen = new HashSet<int>();
		foreach (var site in chunkJals)
		{
			if (seen.Add(site.runIdx))
			{
				used.Add(site.runIdx);
			}
		}
		var bodyPos = new Dictionary<int, long>();
		foreach (var rid in used)
		{
			bodyPos[rid] = bytes;
			var rr = seg.Runs[rid];
			for (int j = rr.start; j < rr.start + rr.len; j++)
			{
				var a = infos[seg.Start + j];
				var ops = ParamsOf(a.Call.Target.GetType()).Select(pi2 => a.Call[pi2]).ToArray();
				outList.Add(new Call(a.Call.Target, ops));
				bytes += a.Size;
			}
			outList.Add(I.JALR((GP_REGISTER)rlink, (GP_REGISTER)rlink, (Expr)0, (Expr)0));
			bytes += 4;
		}
		outList[guardPos] = I.JAL((GP_REGISTER)rlink, (Expr)unchecked((int)(bytes - guardByte)));
		foreach (var site in chunkJals)
		{
			long off = bodyPos[site.runIdx] - site.bytePos;
			outList[site.listPos] = I.JAL((GP_REGISTER)rlink, (Expr)unchecked((int)off));
		}
	}
}
