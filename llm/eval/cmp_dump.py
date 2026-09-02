#!/usr/bin/env python3
"""cmp_dump.py - compare two .f32 dumps, compute cosine similarity."""
import sys, struct, math

def load_f32(path):
    with open(path, "rb") as f:
        data = f.read()
    return list(struct.unpack(f"<{len(data)//4}f", data))

def cosine(a, b):
    dot = sum(x*y for x,y in zip(a,b))
    na = math.sqrt(sum(x*x for x in a))
    nb = math.sqrt(sum(y*y for y in b))
    return dot / (na * nb + 1e-30)

def max_abs_err(a, b):
    return max(abs(x-y) for x,y in zip(a,b))

if len(sys.argv) != 3:
    print("usage: cmp_dump.py <a.f32> <b.f32>")
    sys.exit(1)

a = load_f32(sys.argv[1])
b = load_f32(sys.argv[2])
n = min(len(a), len(b))
cos = cosine(a[:n], b[:n])
mae = max_abs_err(a[:n], b[:n])
mism = sum(1 for i in range(n) if abs(a[i]-b[i]) > 0.01 * max(abs(a[i]), 1e-6))
print(f"n={n} cos={cos:.6f} max_abs_err={mae:.6f} mismatches={mism}/{n}")
print(f"cos>0.98: {'PASS' if cos > 0.98 else 'FAIL'}")
