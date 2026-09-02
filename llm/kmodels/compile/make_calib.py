#!/usr/bin/env python3
# make_calib.py - fold dumped real activations into PTQ calibration tensors
# reads /tmp/kpu_poc/dumps/d_{stem}_{1|2}_x.f32  (raw f32, [M,IN] row-major)
# writes /tmp/kpu_poc/calib{S}/{stem}_{i}.npy  ([1,IN,S,S], rows folded, rest zero)
# usage: make_calib.py [S]   (default 4; also build 16 in one pass)
import os, glob, sys, numpy as np

DUMPS = "/tmp/kpu_poc/dumps"
IN_MAP = {"q": 1024, "k": 1024, "v": 1024, "o": 2048, "gate": 1024, "up": 1024, "down": 3072}

def build(S):
    CAL = f"/tmp/kpu_poc/calib{S}"
    os.makedirs(CAL, exist_ok=True)
    n = 0
    for f in sorted(glob.glob(f"{DUMPS}/d_*_x.f32")):
        base = os.path.basename(f)[:-6]          # d_l0_v_1
        parts = base.split("_")
        seq = parts[-1]
        stem = "_".join(parts[1:-1])             # l0_v
        suffix = stem.split("_", 1)[1]
        IN = IN_MAP[suffix]
        a = np.fromfile(f, dtype=np.float32)
        if a.size % IN:  # shape mismatch -> skip rather than corrupt
            continue
        M = a.size // IN
        x = a.reshape(M, IN)
        t = np.zeros((1, IN, S, S), np.float32)
        for m in range(min(M, S * S)):
            t[0, :, m // S, m % S] = x[m]
        np.save(f"{CAL}/{stem}_{seq}.npy", t)
        n += 1
    print(f"calib{S}: {n} tensors from real activations")

if __name__ == "__main__":
    for s in ([int(sys.argv[1])] if len(sys.argv) > 1 else [4, 16]):
        build(s)
