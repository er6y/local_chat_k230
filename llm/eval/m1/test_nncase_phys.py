#!/usr/bin/python3
"""test_nncase_phys.py - check physical addresses of nncase tensors"""
import numpy as np
import nncaseruntime as nn
import os, mmap, struct

KMODEL = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.kmodel"
SCALE = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.scale"

def virt_to_phys(vaddr):
    """Convert virtual address to physical via /proc/self/pagemap"""
    pagesize = os.sysconf(os.sysconf_names['SC_PAGE_SIZE'])
    offset = (vaddr // pagesize) * 8
    try:
        with open('/proc/self/pagemap', 'rb') as f:
            f.seek(offset)
            data = f.read(8)
            if len(data) == 8:
                entry = struct.unpack('Q', data)[0]
                if entry & (1 << 63):  # present
                    return (entry & ((1 << 55) - 1)) * pagesize + (vaddr % pagesize)
    except:
        pass
    return 0

def main():
    # Load kmodel as bytes (Python heap)
    kmodel_bytes = open(KMODEL, "rb").read()
    kmodel_addr = id(kmodel_bytes) + 16  # approximate buffer address
    print(f"kmodel bytes object: id={hex(id(kmodel_bytes))} len={len(kmodel_bytes)}")
    # Get actual buffer address from memoryview
    mv = memoryview(kmodel_bytes)
    print(f"memoryview addr: {hex(id(mv))}")

    interp = nn.Interpreter()
    interp.load_model(kmodel_bytes)
    in_shape = [int(v) for v in interp.get_input_shape(0)]
    out_shape = [int(v) for v in interp.get_output_shape(0)]
    K = in_shape[1]
    OUT = out_shape[1]
    S = in_shape[2]
    MT = S * S
    print(f"in={in_shape} out={out_shape}")

    np.random.seed(42)
    x = np.random.randn(4, K).astype(np.float32)
    sc = np.fromfile(SCALE, dtype=np.float32)
    inp = np.zeros((1, K, MT), dtype=np.float32)
    inp[0, :, :4] = x.T
    inp[0, :, :4] /= sc.reshape(K, 1)
    inp = inp.reshape(1, K, S, S)

    # Check input tensor physical address
    inp_addr = inp.ctypes.data
    inp_phys = virt_to_phys(inp_addr)
    print(f"input numpy virt={hex(inp_addr)} phys={hex(inp_phys)}")

    t = nn.RuntimeTensor.from_numpy(inp)
    interp.set_input_tensor(0, t)
    interp.run()
    out = interp.get_output_tensor(0).to_numpy()

    out_addr = out.ctypes.data
    out_phys = virt_to_phys(out_addr)
    print(f"output numpy virt={hex(out_addr)} phys={hex(out_phys)}")

    y = out.reshape(OUT, MT)[:, :4].T.copy()
    print(f"y[0:2,0:4]: {y[:2, :4]}")
    print(f"y nonzero: {np.count_nonzero(y)} / {y.size}")

if __name__ == "__main__":
    main()
