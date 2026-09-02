#!/bin/bash
OUT=/tmp/nncase_headers
BASE=https://raw.githubusercontent.com/kendryte/nncase/master/src/Native/include/nncase
mkdir -p $OUT/nncase/kernels/stackvm
curl -sL -o $OUT/nncase/functional/ops.platform.h $BASE/functional/ops.platform.h
curl -sL -o $OUT/nncase/kernels/stackvm/resize_image.h $BASE/kernels/stackvm/resize_image.h
[ -s $OUT/nncase/functional/ops.platform.h ] && echo "OK  ops.platform.h" || echo "FAIL ops.platform.h"
[ -s $OUT/nncase/kernels/stackvm/resize_image.h ] && echo "OK  resize_image.h" || echo "FAIL resize_image.h"

echo "=== Final missing check ==="
grep -rh '#include <nncase/' $OUT/nncase/ 2>/dev/null | sort -u | while read line; do
    f=$(echo "$line" | sed 's/#include <//;s/>//')
    if [ ! -f $OUT/$f ]; then
        echo "MISSING: $f"
    fi
done
echo "=== Done ==="
