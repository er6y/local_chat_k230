# board: time kd_mpi_sys_mmz_flush_cache for various sizes
import ctypes
import time

SO = '/usr/lib/python3.13/site-packages/nncaseruntime/_nncaseruntime_k230.cpython-313-riscv64-linux-gnu.so'
lib = ctypes.CDLL(SO)
lib.kd_mpi_mmz_init()
phy = ctypes.c_ulong(0)
virt = ctypes.c_void_p()
SZ = 64 * 1024 * 1024
rc = lib.kd_mpi_sys_mmz_alloc(ctypes.byref(phy), ctypes.byref(virt), None, None, SZ)
print('alloc rc=%d phys=0x%x virt=0x%x' % (rc, phy.value, virt.value or 0))
if rc != 0:
    raise SystemExit(1)
fn = lib.kd_mpi_sys_mmz_flush_cache
fn.argtypes = [ctypes.c_ulong, ctypes.c_void_p, ctypes.c_uint]
for mb in (1, 2, 4, 8, 16, 32, 48, 64):
    n = mb * 1024 * 1024
    ts = []
    for _ in range(5):
        t0 = time.time()
        fn(phy.value, virt.value, n)
        ts.append((time.time() - t0) * 1000)
    print('flush %2dMB: %s ms' % (mb, [round(t, 3) for t in ts]))
