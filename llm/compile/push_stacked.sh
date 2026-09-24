#!/bin/bash
# chunked push llm_kv6_stacked.kmodel -> board, per-chunk md5, assemble+verify
set -e
SRC=/ux/work/yilei.wang/k230/llm_kv6_stacked.kmodel
BOARD="ssh -o BatchMode=yes -o StrictHostKeyChecking=no root@172.16.72.255"
RDIR=/mnt/data/static/parts
DST=/mnt/data/static/llm_kv6_stacked.kmodel
W=/tmp/k230_srv/push_stacks
mkdir -p $W $RDIR_LOCAL_UNUSED 2>/dev/null || true
mkdir -p $W
cd $W
rm -f chunk_* src.md5
split -b 32M -d $SRC chunk_
md5sum $SRC | awk "{print \$1}" > src.md5
N=$(ls chunk_* | wc -l)
echo "CHUNKS=$N"
$BOARD "mkdir -p $RDIR && rm -f $RDIR/chunk_*"
i=0
for c in chunk_*; do
  i=$((i+1))
  ok=0
  for attempt in 1 2 3; do
    if scp -q -o BatchMode=yes "$c" "root@172.16.72.255:$RDIR/"; then
      ok=1; break
    fi
    echo "RETRY $c attempt=$attempt"; sleep 5
  done
  [ $ok -eq 1 ] || { echo "FAIL $c"; exit 1; }
  [ $((i % 6)) -eq 0 ] && echo "pushed $i/$N"
done
echo "assembling..."
$BOARD "cat $RDIR/chunk_* > $DST && sync && md5sum $DST"
echo -n "server: "; md5sum $SRC
$BOARD "rm -f $RDIR/chunk_*"
echo "PUSH_DONE"
