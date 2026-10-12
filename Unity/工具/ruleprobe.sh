#!/bin/bash
# 引擎离线对拍（⚡ 快速内环 / 旁证）—— 正式验收仍然是 `RuleEngineTest.Run`
# 用法：bash d:/4/Unity/工具/ruleprobe.sh          （跑 check，打一行摘要）
#       bash d:/4/Unity/工具/ruleprobe.sh b19test  （跑 B19 那 45 条断言）
#       🆕 `play` 那一路（`A1431`，**结算层**——「这张卡打出去数字对不对」）：
#         … play <id|卡名>                          单卡读数（≈0.3 秒）
#         … play all [dump <out.tsv>] [pf <out.txt>] 全池逐卡真打（≈5 秒）；`pf` 写**结算指纹**基线
#         … play check [<pf.txt>]                   结算层对拍（逐卡差异 ＋ 哨兵）⇒ 差异/哨兵 ⇒ rc=1
#       ⚠️ 演示/故障注入用 `WFPROBE_POOL=<另一份池.json>`（**会出声**，见 `Program.PoolOverride`）。
# 判据：**只看那行 `=== 全池 … ===` 摘要**（解析差异 0 行 = 绿）。
#   ⚠️ **2026-10-12（`A1382`）补**：上面那句**只说了一半** —— `check` 除「与基线比」之外还跑一组
#   **与基线无关的哨兵**（`=== 哨兵合计：失败 N 条 ===`），**那 N 条也计入 rc**
#   ⇒ `解析差异 0 行` 而 `哨兵合计 > 0` = **仍然红**（`rc=1`）。⛔ 别只看前一行。
#   🔴 其中 ⑦ `[输出指纹]`（`A1382`）**既不读卡池、也不读基线文件** ⇒ `scan` 重生成基线**抹不掉它**
#   ⇒ 「任何真回归 ＋ 重生成基线 ⇒ 静默变绿」这条洞由它兜住（判据 → `Program.PinnedParseMd5`）。
#   ⚠️ 本探针的退出码现在是可信的（收编时把 `Main` 改成了 int + `return Fail`），
#   但按本工程规矩仍以【摘要行】为准 —— 照 `typecheck.sh:68-70` 与 `_run_8_checks.sh:13-22`
#   那两条血案（`$?` 被 `$(date)` 吃掉 ⇒ 编译错误也报 0 ⇒ 假绿）。
# 🔴 2026-10-11 修（台账 `A1466`）：**构建失败一律出声并停** —— 见下面那段长注释。
#   本头的用法里没有「用旧 dll 跑」这一档，别再把它加回来。
# 为什么快：`RuleEngine/Core/` 只碰 `UnityEngine.Debug`/`Random` ⇒ net8 直接编，**不跑 Unity**（实测重编 ~1.5 秒，
#   而 Unity 批处理 1–9 分钟、且同一工程只能跑一个实例）。原理与坑 → `资料/引擎离线对拍_探针.md`
set -u
cd /d/4/Unity/工具/ruleprobe || exit 1

MODE="${1:-check}"

# 🔴 **2026-10-11 修（台账 `A1466`）**：这一段原来【只判「dll 在不在」】——
#   `dotnet build` 的退出码被 `2>&1 | grep … || true` **吃掉了**（同族血案：`typecheck.sh:68-70`
#   的 `$?` 被 `$(date)` 吃掉、`_run_8_checks.sh:13-22`）。而**构建会被别人半成品的源码打断**
#   （实测一次 `CS1525`，来源 = 当时另一个写手正在写的 `工具/ruleprobe/WillRun.cs`）
#   ⇒ 构建**失败**时旧 dll 还在（`bin/` 没被清），脚本**照样拿旧 dll 跑**，
#   打出「**解析差异 0 行**」这种**看起来全绿**的读数。
#   🔴 探针是**所有引擎改动的验收工具**（`A1369`/`A1435`/`A1439`/`A1447`/`A1452` … 全靠它出读数）
#   ⇒ 这种假绿**比「没有读数」更糟**：读数会被当成证据往台账上写。
#   ⇒ 现在**拿 `dotnet build` 的退出码**：非 0 ⇒ **出声并停**，⛔ 一步都不往下走。
#   ⚠️ 日志用 `mktemp` 每次一份独立文件（⛔ 别用固定名 —— 两个探针同时跑会互相盖，
#      同族取舍见 `Program.cs` 里 `CheckMain` 那条「临时 dump 路径固定」的已知限制）。
BUILD_LOG="$(mktemp 2>/dev/null || true)"
if [ -z "$BUILD_LOG" ]; then BUILD_LOG="./_wfprobe_build.log"; fi   # `mktemp` 失败（`TMPDIR` 不存在等）⇒ 退回本地固定名
dotnet build -v:q --nologo > "$BUILD_LOG" 2>&1
build_rc=$?                               # ← 必须【紧跟】那条命令取（别让别的命令把 `$?` 吃掉）
grep -E 'error|已成功生成' "$BUILD_LOG" || true     # 照旧把 error 行/成功行回显给人看
if [ "$build_rc" -ne 0 ]; then
  # ⚠️ 下面这些行**一律用单引号**（或把反引号转义）—— 本工程的名词大量用 `反引号`，
  #   而双引号里它会变成**命令替换**（实测：`echo "…见 \`A1466\`…"` ⇒ `A1466: command not found`，
  #   消息被吃成半句）。**这是「不许静默失败」在脚本里最容易被忽略的一处。**
  echo "!! 构建失败（dotnet build rc=$build_rc）—— **停下**：⛔ 不用旧 dll 接着跑（那会给出假绿读数）。"
  echo '   台账出处 = `资料/任务_卡组卡牌战斗.md` 的 `A1466`。'
  echo "   构建输出（末 40 行）："
  tail -n 40 "$BUILD_LOG"
  echo '   ⚠️ 先看那些 error 落在【哪个文件】：若是**别人正在写的**源码（本工程多写手并行），那不是你的问题；'
  echo '      但它一律算「本次读数不可信」—— 等构建通过再跑。'
  rm -f "$BUILD_LOG"
  exit 1
fi
rm -f "$BUILD_LOG"
DLL="bin/Debug/net8.0/wfprobe.dll"
if [ ! -f "$DLL" ]; then echo "!! 构建报成功、但 $DLL 不存在（bin/ 被清过？）"; exit 1; fi

echo "=== $(date +%H:%M:%S) ruleprobe $MODE ==="
# 🔴 **2026-10-18 修（同族血案第二例）**：原来正文跑完直接 `echo` 收尾 ⇒ **末行那句 `echo` 把探针的退出码吃掉了**
#    （实测 `bash 工具/ruleprobe.sh; echo $?` 恒 `0`），而本头第 6 行还写着「退出码现在是可信的」
#    —— **两头打架**。（同族：`typecheck.sh` 的 `$?` 被 `$(date)` 吃掉 ⇒ 编译错误也报 0。）
#    做法照 `_run_8_checks.sh` 的形状：**先把 `rc` 存下来，再打印，最后 `exit $rc`**。
if [ "$MODE" = "check" ]; then
  # ⚠️ 第二参数（基线路径）原来被写死 ⇒ 传了也没用；现在接上，缺省仍是那一条。
  dotnet bin/Debug/net8.0/wfprobe.dll check "${2:-基线/out_baseline.txt}"
else
  # 🔴 2026-10-18 修：原来只传 `"$MODE"`（= `$1`）⇒ 后面的参数**全被吞掉**，
  #    于是 README/本头的用法 `… seg "<文本>"` 必崩（`Program.cs:47` 取 `args[i+1]` ⇒ IndexOutOfRange）。
  #    转发**全部**参数（`"$@"`），模式仍是 `$1`。
  dotnet bin/Debug/net8.0/wfprobe.dll "$@"
fi
rc=$?                                   # ← 必须在 `echo` **之前**取（见上面那段）
echo "=== $(date +%H:%M:%S) 结束（rc=$rc）==="
# ⚠️ 判据仍是【摘要行】（本头第 5 行），退出码只是**辅助** —— 但它不该再恒 0。
exit $rc
