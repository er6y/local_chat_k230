import io

p = r'D:\work\git_dev\k230_prj\k230_llm\llamacpp\ggml\src\ggml-cpu\arch\riscv\quants.c'
src = io.open(p, encoding='utf-8').read()

start_marker = "// Column-batched Q8_0 dot (decode GEMV): ncol consecutive weight rows\n"
end_marker = "#if defined(__riscv_v)\nstatic NOINLINE void ggml_vec_dot_q1_0_q8_0_vl256"

i0 = src.index(start_marker)
i1 = src.index(end_marker, i0)

out = src[:i0] + "#if defined(__riscv_v)\nstatic NOINLINE void ggml_vec_dot_q1_0_q8_0_vl256"
out += src[i1 + len(end_marker):]
io.open(p, 'w', encoding='utf-8', newline='\n').write(out)
print("cols function removed, %d bytes" % (len(src) - len(out)))
