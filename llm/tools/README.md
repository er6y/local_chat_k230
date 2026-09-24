# tools/ — GNNE ISA 逆向 & kmodel 格式工具

## ISA（编码器/解码器双验证过：mini .script 重编码 md5 与 kmodel .text 逐字节一致）

- `isa_spec.json` — 97 指令字段布局 + 26 枚举（从插件 IL 抽取）
- `rt_isa.py` — .script 文本 → 机器码（汇编器）
- `rt_decode.py <kmodel> <fn_idx>` — kmodel 函数 .text → 指令文本（解码器，
  板上也有 /root/rt_decode.py + /root/isa_spec.json）
- 坑：**板上别解码 >1MB 大函数**（160 万指令 → 内存爆板）；大解码去服务器

## kmodel v7 格式

- `kmodel_parse4.py` — 权威解析器（**模块跳转 `pos = mstart + msize`；段记录跨度 = ssize；
  函数记录跨度 = fsize**——旧版两个坑都栽在这）
- `kmodel_ledger.py` / `kmodel_topfn.py` — 段账本（.text/.rdata 分账）+ 大函数榜

## forensics/（一次性取证工具，历史案件留档）

- `patch_so.py` — .so 函数补丁器（vaddr→文件偏移；注意改返回值语义会 abort）
- `elfmmz.py` — ELF dynsym/RELA 解析（PLT 依赖检查）
- `textcmp.py` / `period.py` — 官方 vs 我们 .text 密度对比（inst_opt 破案用）
