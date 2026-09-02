# K230 语音助手 — 工作区规则

> 深度背景看 `docs/`（硬件手册 + 交接书 + 任务书），本文只放每个会话都要遵守的操作红线。

## 红线（违反会挂板/丢数据）

1. **串口只开 COM4 @115200**（FTDI VID_0403&PID_6001）。COM16 等其他口是非 K230 设备，**严禁打开**。root 登录：输 `root` 回车，再回车一次空密码。
2. 板上程序必须经 `llm/board/safe_run.sh` 包裹（WDT 喂狗护航，挂死自动复位）。
3. 板上写文件后必须 `sync` 再 `reboot`，否则 ext4 journal 回滚丢文件（已两次丢 .so）。
4. CMA 池脏了（CmaFree 远低于 ~1000MB 干净值）先 reboot 拿干净池再测。
5. 板子内核级挂死（串口无输出、adb 假在线）→ 物理拔电，没有别的办法。
6. WSL 只做编译；adb/scp/git/文件操作在 Windows 侧做。

## 三条通道（按优先级）

| 通道 | 地址 | 用途 |
|---|---|---|
| 串口 TTL | COM4 @115200 | 控制 + 救援（永生通道） |
| WiFi ssh | `ssh root@192.168.1.252`（免密） | 文件传输 + 快捷命令（高负载会断，~20s 自愈） |
| USB adb | VID_1209 | 备份（dwc2 驱动 bug，高负载即死，别依赖） |

## 测试纪律

- 启动模式：板上 `nohup setsid sh /mnt/data/kpu_llm/run_xxx.sh > /mnt/data/launcher.log 2>&1 < /dev/null &`，然后轮询 tail 日志；不要前台挂长任务。
- 输出必须落盘 `/mnt/data/*.log`。
- KPU 点亮必要条件：环境带 `LD_BIND_NOW=1`。
- selftest 判据：l0_* probe cos>0.98。

## Windows/PowerShell 纪律

- PS 5.1 不支持 `&&`；**严禁内联含 `$()`/`$var` 的命令**——写 .ps1/.sh 落盘执行。
- 板上脚本必须 LF 行尾（`sed -i 's/\r$//'`）。
- GitHub 直连被墙：用 `https://gh-proxy.com/https://github.com/...` 通道。

## Git 纪律

- `.gguf`/`.kmodel`/`.so`/编译产物不进 git（.gitignore 已挡）。
- 模型分发走 GitHub Release + SHA256 清单。
- vendor runtime（Canaan nncase 库）**禁止上传公开仓库**（商业授权），只放获取脚本。
- llamacpp submodule pin 在 tag（如 k230-v0.1），升级要 deliberate bump + 板上回归。

## 编译（唯一正确路径）

见 `llm/build/README.md`。核心三点：
1. WSL 里 `bash /mnt/d/work/git_dev/local_chat_k230/llm/build/build_kpu_llama.sh`（或改码后 `deploy_test.sh` 一键编译）。
2. cmake 必须带 `KPU_NNCASE_DIR` 环境变量，否则**静默编出无 KPU 版本**（.so 变 ~800KB 就是中招）。
3. 部署要覆盖板上三个 .so 名（.so/.so.0/.so.0.22.0）。

老工作区 `D:\work\git_dev\k230_prj\k230_llm\llamacpp` 是 fork 的 git 仓库（snapshot 保命分支 + k230 分支）；
主仓 `D:\work\git_dev\local_chat_k230` 的 `llm/llamacpp` 是 submodule，pin 在 `k230-v0.1`。
编译用主仓路径，git 操作用 fork 路径。
