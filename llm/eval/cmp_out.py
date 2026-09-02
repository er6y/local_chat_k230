#!/usr/bin/env python3
# cmp_out.py - compare kpu dumped output vs torch reference
# usage: cmp_out.py <tag> <OUT> <S>
import sys
import numpy as np

tag, OUT, S = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
y_ref = np.load(f"/tmp/kpu_poc/qwen/{tag}.probe_y.npy")  # [1,OUT,S,S] f32
y_kpu = np.fromfile(f"/tmp/kpu_poc/qwen/{tag}.kmodel.out", dtype=np.float32)
assert y_kpu.size == y_ref.size, (y_kpu.size, y_ref.size)
y_kpu = y_kpu.reshape(y_ref.shape)
a, b = y_ref.ravel(), y_kpu.ravel()
cos = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12))
rel = float(np.linalg.norm(a - b) / (np.linalg.norm(a) + 1e-12))
print(f"{tag}: cos_sim={cos:.6f} rel_l2={rel:.6f} max_abs={np.abs(a-b).max():.5f} ref_absmax={np.abs(a).max():.5f}")
print("ref  first8:", np.round(a[:8], 4))
print("kpu  first8:", np.round(b[:8], 4))
