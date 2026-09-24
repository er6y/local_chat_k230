#!/usr/bin/env python3
"""oc_k230v2.py - SAFE overclock sequence. The v1 attempt died at PLL3 powerdown
because the AI domain was still clocked from PLL3 (ai_clk_sel=pll3_div2) - the
clock vanished and the interconnect locked up. Correct order:
  1. ai_clk -> pll1_div2 (1188MHz, keeps AI domain alive)   [CMU+0x08 bit2=0]
  2. SHRM  -> pll0_div2  (800MHz, SRAM off PLL3)            [CMU+0x5c bit14=0]
  3. reprogram PLL3 to 1.8GHz (nobody is using it now)
  4. ai_clk -> pll3_div2 (900MHz)                           [bit2=1 + upd]
  5. CPU1  -> pll3_oclk (1.8GHz)                            [sel=01 + upd]
Each CMU write follows the W1T bit31 update rule. Watchdog reboots the board
if anything still goes wrong."""
import ctypes, os, sys, time

libc = ctypes.CDLL("libc.so.6", use_errno=True)
libc.mmap.restype = ctypes.c_void_p
libc.mmap.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int,
                      ctypes.c_int, ctypes.c_int, ctypes.c_long]
fd = os.open("/dev/mem", os.O_RDWR | os.O_SYNC)

def mapreg(phys, size=0x1000):
    a = libc.mmap(None, size, 0x3, 0x1, fd, phys)
    assert a and a != 0xFFFFFFFFFFFFFFFF, hex(phys)
    return a

CMU, BOOT = mapreg(0x91100000), mapreg(0x91102000)
def rd32(b, o): return ctypes.c_uint32.from_address(b + o).value
def wr32(b, o, v): ctypes.c_uint32.from_address(b + o).value = v
def wr_upd(b, o, v): wr32(b, o, v); wr32(b, o, v | (1 << 31))

def freq_mhz():
    v = rd32(BOOT, 0x30)
    return 24.0 * ((v & 0x1FFF) + 1) / (((v >> 16) & 0x3F) + 1) / (((v >> 24) & 0xF) + 1)

def busy():
    t0 = time.time()
    x = 0
    for i in range(2_000_000):
        x = (x * 33 + 1) & 0xFFFFFFFF
    return (time.time() - t0) * 1000.0

revert = "--revert" in sys.argv
target_fb = 399 if revert else 449

print(f"start: PLL3={freq_mhz():.0f}MHz ai=0x{rd32(CMU,8):08x} cpu1=0x{rd32(CMU,4):08x} shrm=0x{rd32(CMU,0x5c):08x}", flush=True)
ms0 = busy()
print(f"busy(2M)={ms0:.0f}ms", flush=True)

# 1. AI domain off PLL3 -> pll1_div2
ai = rd32(CMU, 8)
wr_upd(CMU, 8, ai & ~(1 << 2))
print(f"ai -> pll1_div2: 0x{rd32(CMU,8):08x}", flush=True)
# 2. SHRM off PLL3 -> pll0_div2
s = rd32(CMU, 0x5c)
if s & (1 << 14):
    wr_upd(CMU, 0x5c, s & ~(1 << 14))
print(f"shrm -> pll0_div2: 0x{rd32(CMU,0x5c):08x}", flush=True)
time.sleep(0.05)

# 3. reprogram PLL3 (unused now)
new = (rd32(BOOT, 0x30) & ~0x1FFF) | (target_fb & 0x1FFF)
wr32(BOOT, 0x38, rd32(BOOT, 0x38) | (1 << 16) | (1 << 0))   # pwrdwn
time.sleep(0.01)
wr32(BOOT, 0x30, new)
time.sleep(0.01)
wr32(BOOT, 0x38, rd32(BOOT, 0x38) | (1 << 17) | (1 << 1))   # init
lock = False
for i in range(3000):
    if rd32(BOOT, 0x3c) & 1:
        lock = True; break
    time.sleep(0.001)
print(f"PLL3 -> {freq_mhz():.0f}MHz lock={lock}", flush=True)

# 4. AI domain back on PLL3/2 (900MHz when OC, 800 when revert)
wr_upd(CMU, 8, rd32(CMU, 8) | (1 << 2))
print(f"ai -> pll3_div2: 0x{rd32(CMU,8):08x}", flush=True)
# 5. CPU1 -> pll3_oclk (1.8GHz) or back to pll0_oclk
sel = 0b10 if revert else 0b01
wr_upd(CMU, 4, (rd32(CMU, 4) & ~0x6) | (sel << 1))
print(f"cpu1 -> {'pll3_oclk' if sel==1 else 'pll0_oclk'}: 0x{rd32(CMU,4):08x}", flush=True)

ms1 = busy()
print(f"busy(2M)={ms1:.0f}ms speedup x{ms0/ms1:.3f}", flush=True)
os.close(fd)
