// hwprobe_probe.c - ask K230 kernel what riscv_hwprobe(2) reports for IMA_EXT_0
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <sys/syscall.h>
#include <stdint.h>

struct hp { int64_t key; uint64_t value; };

int main() {
    struct hp pair = {4, 0};  // key 4 = RISCV_HWPROBE_KEY_IMA_EXT_0
    errno = 0;
    long r = syscall(258, &pair, (size_t)1, (size_t)0, (void*)NULL, 0u);
    int saved = errno;
    printf("syscall ret=%ld errno=%d (%s)\n", r, saved, strerror(saved));
    printf("pair.key=%lld (expect 4, -1 means unknown key)\n", (long long)pair.key);
    printf("pair.value=0x%llx\n", (unsigned long long)pair.value);
    printf("IMA_V(bit2)=%d  Zvfh(bit30)=%d  Zvfhmin(bit31)=%d\n",
           (int)!!(pair.value & (1ULL << 2)),
           (int)!!(pair.value & (1ULL << 30)),
           (int)!!(pair.value & (1ULL << 31)));
    return 0;
}
