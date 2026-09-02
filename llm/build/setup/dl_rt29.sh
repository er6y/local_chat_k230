#!/bin/bash
# dl_rt29.sh - fetch v2.9 runtime linux tgz through a github mirror
set -e
URL=https://github.com/kendryte/nncase/releases/download/v2.9.0/nncase_k230_v2.9.0_runtime_linux.tgz
for M in "https://ghproxy.net/" "https://gh-proxy.com/" "https://mirror.ghproxy.com/" ""; do
    echo "TRY ${M:-direct}"
    if curl -sL --max-time 120 "${M}${URL}" -o /tmp/rt29.tgz; then
        if gzip -t /tmp/rt29.tgz 2>/dev/null; then
            echo "OK via ${M:-direct}"
            break
        fi
    fi
done
ls -la /tmp/rt29.tgz
gzip -t /tmp/rt29.tgz && echo GZIP_OK
