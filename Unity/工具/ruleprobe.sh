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
if [ "$MODE" = "check" ]; then
  dotnet bin/Debug/net8.0/wfprobe.dll check "基线/out_baseline.txt"
else
  dotnet bin/Debug/net8.0/wfprobe.dll "$MODE"
fi
echo "=== $(date +%H:%M:%S) 结束 ==="
