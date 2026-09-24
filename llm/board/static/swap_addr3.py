import struct

KM = '/mnt/data/kmini/s3c.kmodel'
data = bytearray(open(KM, 'rb').read())
foff = 0x86c0 + 0x45cd8
fn = data[foff:foff + 586]
w1 = int.from_bytes(fn[0x4c:0x50], 'little')
w2 = int.from_bytes(fn[0x50:0x54], 'little')
print('before: LUI w=%08x (imm=%d)  ADDI w=%08x (imm=%d)' % (w1, w1 >> 12, w2, w2 >> 20))
assert w1 >> 12 == 484 and (w2 >> 20) == 1043
nw1 = (w1 & 0xFFF) | (228 << 12)
nw2 = (w2 & 0xFFFFF) | (3100 << 20)
data[foff + 0x4c:foff + 0x50] = struct.pack('<I', nw1)
data[foff + 0x50:foff + 0x54] = struct.pack('<I', nw2)
print('after:  LUI w=%08x (imm=%d)  ADDI w=%08x (imm=%d)' % (nw1, nw1 >> 12, nw2, nw2 >> 20))
open('/mnt/data/kmini/s3c_swap.kmodel', 'wb').write(bytes(data))
print('written s3c_swap.kmodel')
