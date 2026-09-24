import numpy as np, glob, os

# 1) where do the 6 bad stems' NaN scales come from: calib data itself?
for tag in ['l10_o', 'l11_o', 'l16_o', 'l0_o', 'l0_q']:
    fs = sorted(glob.glob(f'/tmp/kpu_poc/calib4/{tag}_*.npy'))
    tot_nan = tot_ch = 0
    nonfinite_files = 0
    maxv = 0.0
    for p in fs:
        a = np.load(p)
        m = np.abs(a[0]).max(axis=(1, 2))
        bad = int((~np.isfinite(m)).sum())
        tot_nan += bad
        tot_ch += m.size
        if bad:
            nonfinite_files += 1
        else:
            maxv = max(maxv, float(m.max()))
    print(f"{tag}: files={len(fs)} nonfinite_files={nonfinite_files} "
          f"nonfinite_ch={tot_nan}/{tot_ch} finite_max={maxv:.4f}")

# 2) inspect the actual bad scale files
print("--- scale files ---")
for tag in ['l10_o', 'l0_o']:
    s = np.fromfile(f'/tmp/kpu_poc/qwen_s4sq/{tag}.scale', dtype=np.float32)
    print(f"{tag}: len={s.size} finite={int(np.isfinite(s).sum())} "
          f"min={s.min()} max={s.max()} head={s[:4]}")
