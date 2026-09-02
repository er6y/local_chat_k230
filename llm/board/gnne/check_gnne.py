import mmap, struct, os
fd = os.open('/dev/mem', os.O_RDWR | os.O_SYNC)
m = mmap.mmap(fd, 512, mmap.MAP_SHARED, mmap.PROT_READ | mmap.PROT_WRITE, offset=0x80400000)
vals = struct.unpack('8Q', m[:64])
print('gnne regs[0:8]:', [hex(v) for v in vals])
vals2 = struct.unpack('8Q', m[64:128])
print('gnne regs[8:16]:', [hex(v) for v in vals2])
m.close()
os.close(fd)
