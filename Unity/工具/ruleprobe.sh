#!/bin/bash
# 引擎离线对拍（⚡ 快速内环 / 旁证）—— 正式验收仍然是 `RuleEngineTest.Run`
# 用法：bash d:/4/Unity/工具/ruleprobe.sh          （跑 check，打一行摘要）
#       bash d:/4/Unity/工具/ruleprobe.sh b19test  （跑 B19 那 45 条断言）
# 判据：**只看那行 `=== 全池 … ===` 摘要**（解析差异 0 行 = 绿）。
#   ⚠️ 本探针的退出码现在是可信的（收编时把 `Main` 改成了 int + `return Fail`），
#   但按本工程规矩仍以【摘要行】为准 —— 照 `typecheck.sh:68-70` 与 `_run_8_checks.sh:13-22`
#   那两条血案（`$?` 被 `$(date)` 吃掉 ⇒ 编译错误也报 0 ⇒ 假绿）。
# 为什么快：`RuleEngine/Core/` 只碰 `UnityEngine.Debug`/`Random` ⇒ net8 直接编，**不跑 Unity**（实测重编 ~1.5 秒，
#   而 Unity 批处理 1–9 分钟、且同一工程只能跑一个实例）。原理与坑 → `资料/引擎离线对拍_探针.md`
set -u
cd /d/4/Unity/工具/ruleprobe || exit 1

MODE="${1:-check}"

dotnet build -v:q --nologo 2>&1 | grep -E 'error|已成功生成' || true
DLL="bin/Debug/net8.0/wfprobe.dll"
if [ ! -f "$DLL" ]; then echo "!! 编译失败：$DLL 不存在"; exit 1; fi

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
