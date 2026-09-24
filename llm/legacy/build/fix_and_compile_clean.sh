#!/bin/bash
# fix_and_compile_clean.sh - point the SQ compilers at the CLEAN calibration
# and rebuild both kmodel sets
pkill -f 'mk_sq4c.py' 2>/dev/null
pkill -f 'mk_sq16c.py' 2>/dev/null
sleep 1
cd /tmp/kpu_poc
sed -i 's#calib{S}/{tag}#calib{S}_clean/{tag}#g' mk_sq4c.py mk_sq16c.py
echo "subs count: $(grep -c 'calib{S}_clean' mk_sq4c.py) / $(grep -c 'calib{S}_clean' mk_sq16c.py)"
rm -rf qwen_s4sq_clean qwen_s16sq_clean
nohup bash -c '
  cd /tmp/kpu_poc
  ~/venv_nncase29/bin/python mk_sq4c.py all --jobs 4 > /tmp/mk4c.log 2>&1
  ~/venv_nncase29/bin/python mk_sq16c.py all --jobs 4 > /tmp/mk16c.log 2>&1
  echo ALL_CLEAN_COMPILE_DONE >> /tmp/mk16c.log
' > /dev/null 2>&1 &
echo STARTED
