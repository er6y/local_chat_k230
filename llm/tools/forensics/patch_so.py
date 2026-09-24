import struct

p = 'D:/work/git_dev/k230_prj/tmp/nncase_rt.so'
data = bytearray(open(p, 'rb').read())
end = '<'
e_phoff, = struct.unpack_from(end + 'Q', data, 0x20)
e_phentsize, e_phnum = struct.unpack_from(end + 'HH', data, 0x36)
target = 0x3fae00  # kd_mpi_sys_mmz_flush_cache vaddr
off = None
for i in range(e_phnum):
    po = e_phoff + i * e_phentsize
    p_type, p_flags, p_offset, p_vaddr, p_paddr, p_filesz, p_memsz, p_align = struct.unpack_from(end + 'IIQQQQQQ', data, po)
    if p_type == 1 and p_vaddr <= target < p_vaddr + p_filesz:
        off = p_offset + (target - p_vaddr)
        print('LOAD seg: vaddr=0x%x filesz=0x%x -> fileoff=0x%x' % (p_vaddr, p_filesz, off))
        break
assert off is not None
print('current bytes:', data[off:off + 16].hex())
# riscv64 ret = 0x00008067 little-endian
data[off:off + 4] = bytes([0x67, 0x80, 0x00, 0x00])
out = 'D:/work/git_dev/k230_prj/tmp/nncase_rt_noflush.so'
open(out, 'wb').write(bytes(data))
print('patched ->', out)
print('new bytes:', data[off:off + 16].hex())
