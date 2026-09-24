# server: build s3c bisect variants by freezing host inputs into constants
import onnx
from onnx import helper, TensorProto as T
import numpy as np

m = onnx.load('s3c.onnx')
g = m.graph
rng = np.random.default_rng(3)


def freeze(g, name, arr, dtype):
    # remove graph input `name`, add initializer, splice consumers
    gi = [i for i in g.input if i.name == name]
    assert len(gi) == 1, name
    g.input.remove(gi[0])
    init = helper.make_tensor(name + '_c', dtype, arr.shape, arr.flatten().tolist())
    g.initializer.append(init)
    for n in g.node:
        n.input[:] = [name + '_c' if x == name else x for x in n.input]


def build(tag, freeze_pos=False, freeze_kv=False):
    mm = onnx.ModelProto()
    mm.CopyFrom(m)
    gg = mm.graph
    if freeze_pos:
        pos = np.array([[21]], np.int64)
        freeze(gg, 'position_ids', pos, T.INT64)
    if freeze_kv:
        kt = (rng.standard_normal((48, 64, 32)) * 0.2).astype(np.float32)
        v = (rng.standard_normal((48, 32, 64)) * 0.2).astype(np.float32)
        freeze(gg, 'kt_all', kt, T.FLOAT)
        freeze(gg, 'v_all', v, T.FLOAT)
    onnx.save(mm, 's3c_%s.onnx' % tag)
    print('s3c_%s.onnx saved, inputs=%d' % (tag, len(gg.input)))


build('nopos', freeze_pos=True)
build('nokv', freeze_kv=True)
