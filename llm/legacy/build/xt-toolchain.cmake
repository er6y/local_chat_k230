# xt-toolchain.cmake - Xuantie GCC 14.1.1 cross toolchain for K230 (C908, rv64gcv)
# CMAKE_SYSTEM_PROCESSOR must be set here (not on the cmake command line) so that
# project() keeps it and ggml's ggml_get_system_arch() picks arch/riscv.
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR riscv64)
set(CMAKE_C_COMPILER /root/xuantie/bin/riscv64-unknown-linux-gnu-gcc)
set(CMAKE_CXX_COMPILER /root/xuantie/bin/riscv64-unknown-linux-gnu-g++)
