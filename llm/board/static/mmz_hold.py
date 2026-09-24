# board: resident mmz dummy allocator (shifts .a workspace allocations)
import ctypes
import sys
import time

SO = '/usr/lib/python3.13/site-packages/nncaseruntime/_nncaseruntime_k230.cpython-313-riscv64-linux-gnu.so'
lib = ctypes.CDLL(SO)
D = int(sys.argv[1]) if len(sys.argv) > 1 else 0
r = lib.kd_mpi_mmz_init()
print('mmz_init rc=%d' % r, flush=True)
phy = ctypes.c_ulong(0)
virt = ctypes.c_void_p()
rc = lib.kd_mpi_sys_mmz_alloc(ctypes.byref(phy), ctypes.byref(virt), None, None, D * 1024 * 1024)
print('alloc rc=%d phys=0x%x size=%dMB' % (rc, phy.value, D), flush=True)
if rc != 0:
    sys.exit(1)
time.sleep(300)
print('HOLD_END', flush=True)
