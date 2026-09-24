#!/bin/bash
cd /ux/work/yilei.wang/k230
export PYTHONPATH=/ux/work/yilei.wang/k230/env/lib
export DOTNET_ROOT=/ux/work/yilei.wang/k230/dotnet-sdk
export PATH=/ux/work/yilei.wang/k230/dotnet-sdk:$PATH
export NNCASE_PLUGIN_PATH=/ux/work/yilei.wang/k230/rebuild283_srv
export DOTNET_gcServer=0
P=/ux/work/yilei.wang/k230/conda/bin/python
$P s3c_compile.py
