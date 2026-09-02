#!/usr/bin/env python3
# analyze_timing3.py - per-token breakdown of cpu_gemm_timing.log
# log lines: "<prev> -> <cur> : <us>"  (delta from entering mul_mat N to N+1)
import re, sys, statistics

path = sys.argv[1] if len(sys.argv) > 1 else r"D:\work\git_dev\k230_prj\k230_llm\.tools\timing_def.log"
pat = re.compile(r"^(.*?)\s*->\s*(.*?)\s*:\s*([\d.]+)\s*us")
rows = []
for line in open(path, encoding="utf-8", errors="replace"):
    m = pat.match(line.strip())
    if m:
        rows.append((m.group(1), m.group(2), float(m.group(3))))
print(f"entries: {len(rows)}  (= {len(rows)/196:.2f} tokens x 196 gemms)")

def tok_of(name):
    m = re.search(r"-(\d+)$", name)
    return int(m.group(1)) if m else None

# group into tokens: a token starts at Qcur-0
tokens = []  # list of (list of (prev,cur,us))
cur = []
for prev, c, us in rows:
    if re.match(r"^Qcur-0\b", c) and cur:
        tokens.append(cur); cur = []
    cur.append((prev, c, us))
if cur: tokens.append(cur)
print(f"complete tokens: {len(tokens)}")

tot = [sum(r[2] for r in t) for t in tokens]
print(f"\nper-token SUM of logged deltas (us): min={min(tot):.0f} med={statistics.median(tot):.0f} max={max(tot):.0f}")
print(f"  -> {statistics.median(tot)/1e6:.3f} s/token median = {1e6/statistics.median(tot):.2f} t/s (GEMM-entry to GEMM-entry)")

# categorize deltas
import collections
cat = collections.Counter(); cat_us = collections.Counter()
for t in tokens:
    for prev, c, us in t:
        # classify by the PREVIOUS node (the work that just finished + gap to next)
        key = None
        if prev.startswith("ffn_out-27"): key = "tail27->Qcur0 (logits+rebuild+embed)"
        elif prev.startswith("ffn_out-"): key = "ffn_out->Qcur (attn norm+rope+qkv gap)"
        elif prev.startswith("ffn_up-"):  key = "up->down (silu-mul gap)"
        elif prev.startswith("ffn_gate-"):key = "gate->up"
        elif prev.startswith("Kcur-"):    key = "K->node (attn core: k/v rope? softmax+V*O)"
        elif prev.startswith("Vcur-"):    key = "V->K"
        elif prev.startswith("Qcur-"):    key = "Q->V"
        else: key = "other(" + prev + ")"
        cat[key] += 1; cat_us[key] += us
print("\nper-token category totals (median token):")
# use median token by total
mid = sorted(range(len(tokens)), key=lambda i: tot[i])[len(tokens)//2]
agg = collections.Counter()
for prev, c, us in tokens[mid]:
    key = None
    if prev.startswith("ffn_out-27"): key = "tail27->Qcur0"
    elif prev.startswith("ffn_out-"): key = "ffn_out->Qcur"
    elif prev.startswith("ffn_up-"):  key = "up->down"
    elif prev.startswith("ffn_gate-"):key = "gate->up"
    elif prev.startswith("Kcur-"):    key = "K->node"
    elif prev.startswith("Vcur-"):    key = "V->K"
    elif prev.startswith("Qcur-"):    key = "Q->V"
    else: key = "other:" + prev
    agg[key] += us
s = sum(agg.values())
for k, v in agg.most_common():
    print(f"  {k:28s} {v/28 if k not in ('tail27->Qcur0',) else v:9.0f} us x{agg[k] if k!='tail27->Qcur0' else 1}  = {v:8.0f} us  ({100*v/s:.1f}%)")
print(f"  TOTAL {s:.0f} us")

# the 7 GEMM shapes: which GEMM line dominates (time from its entry to next entry)
shape = collections.Counter(); shapen = collections.Counter()
for t in tokens:
    for prev, c, us in t:
        m = re.match(r"^(Qcur|Vcur|Kcur|ffn_gate|ffn_up|ffn_out|node)-?\d*", prev)
        k = m.group(1) if m else prev
        shape[k] += us; shapen[k] += 1
print("\nby GEMM (avg us per occurrence, includes following gap):")
for k in shape:
    print(f"  {k:10s} n={shapen[k]:4d}  avg={shape[k]/shapen[k]:7.0f} us")
