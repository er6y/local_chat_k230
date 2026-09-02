#!/bin/sh
# run_t_vendor.sh - Run t_vendor with safe_run wrapper, output to log
export LD_LIBRARY_PATH=/mnt/data/kpu_llm
export LD_BIND_NOW=1
cd /mnt/data/kpu_llm

echo "=== t_vendor test $(date) ==="
./safe_run.sh ./t_vendor /mnt/data/kpu_qwen/s4sq_q8/l0_q.kmodel /mnt/data/kpu_llm/vendor_test_input.bin /mnt/data/kpu_llm/vendor_test_ref_output.bin
echo "=== exit code: $? ==="
echo "=== DONE $(date) ==="
