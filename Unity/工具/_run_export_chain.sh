#!/bin/bash
# 2026-11-01（第一百一十三会话 · B-144③）：**资源导出链的【唯一正确顺序】**，写死成脚本。
#
# 为什么要有它（判据 → `资料/完全复刻/判据/B113W_对账_D_资源导出.md` 的 B-144 行）：
#   这条顺序以前**只活在两处注释里**（`EffectExporter.cs:22` · `BoosterPackExporter.cs:29-30`），
#   靠人记。2026-11-01 已经实际破过一次：booster 那 4 件 prefab 停在 01:08、而 `Prefabs/` 最早 03:26
#   ⇒ 4 条悬空引用 + 一整轮诊断。
#
# 顺序为什么是这个：
#   ① `EffectExporter.Run`      —— 全量重导：**它会先 `ClearGenerated()` 把 Mat/Tex/Mesh/Prefab 四个目录删光**。
#                                 ⚠️ 所以它必须**排第一**，后面每一步都在重建它删掉的东西。
#   ② `EffectExporter.RunListed` —— 补 `Run()` 的「效果根过滤器」挡掉的那几件（`ListedPrefabs` 名单）。
#                                 它也走同一个 prefab 保存口，所以同样会写 `Prefabs/`。
#   ③ `BoosterPackExporter.Run` —— **必跑、不能省**：卡包开包窗那 4 件 + `WarpforgeBooster/Prefabs/*.prefab`
#                                 引用的就是 ① 删掉又重建的那批特效 prefab。**漏了它 = 一批悬空 guid。**
#   ④ `EffectLibraryBuilder.Run` —— 把导出集落进 `WarpforgeEffectLibrary` 那个 .asset（运行期按名字取）。
#                                 它读的是 **①+②+③ 之后的**产物，所以必须在它们后面。
#   ⑤ `ArenaBuilder.ApplyAllTextureImportsFromCLI` —— **可选**（要传 `--optional` 才跑）。
#                                 只影响战场那棵树（`WarpforgeArena1/arenas/**`）的贴图导入项，与上面四步无依赖；
#                                 日常重导特效不需要它，改过 `ArenaBuilder` 的导入表时才跑。
#
# 用法：
#   bash d:/4/Unity/工具/_run_export_chain.sh              # 跑 ①～④
#   bash d:/4/Unity/工具/_run_export_chain.sh --optional   # 跑 ①～⑤
#
# ⚠️ 同一工程同一时刻**只能有一个 Unity 实例**（见 `CLAUDE.md` §1·b）⇒ 本脚本**必须独占**跑，
#    跑之前确认没有别的 `-executeMethod` 在跑。
# ⚠️ 三步里 `Run` 是**全量重导**（几十分钟量级），别在没备份的情况下随便跑。
set -e

UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
LOG=d:/4/_tmp_view/export_chain_$(date +%Y%m%d_%H%M%S)
mkdir -p "$LOG"

# ⚠️ 本机带了 `ELECTRON_RUN_AS_NODE`，启动 Unity 前必须 unset，否则静默失败（`CLAUDE.md` §二）
unset ELECTRON_RUN_AS_NODE

echo "日志目录：$LOG"
echo "开始：$(date +%H:%M:%S)"

# $1 = `-executeMethod` 入口名   $2 = 日志文件名（放在 $LOG 下）
step () {
  echo "=== $(date +%H:%M:%S) 开始 $1 ==="
  local rc=0
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod "$1" -logFile "$LOG/$2" >/dev/null 2>&1 || rc=$?
  local cc=""
  grep -q "Scripts have compiler errors" "$LOG/$2" 2>/dev/null && cc="  🔴 编译错误（这一步根本没跑）" || true
  echo "    $(date +%H:%M:%S) 结束 $1 —— 退出码 $rc$cc"
  if [ "$rc" -ne 0 ] || [ -n "$cc" ]; then
    echo "🔴 第 $1 步没跑成 ⇒ 后面的顺序已经没有意义，停在这里（⛔ 别忽略继续往下）。"
    exit 1
  fi
}

# ① 全量重导（**先删后建**，必须第一）
step EffectExporter.Run              1_effect_run.log
# ② 补 `ListedPrefabs` 那几件
step EffectExporter.RunListed        2_effect_listed.log
# ③ **必跑**：卡包 prefab 引用的就是 ① 重导出来的特效 prefab
step BoosterPackExporter.Run         3_booster.log
# ④ 效果库落盘
step EffectLibraryBuilder.Run        4_effect_library.log

# ⑤ 可选：战场贴图导入项（与上面四步无依赖）
if [ "${1:-}" = "--optional" ]; then
  step ArenaBuilder.ApplyAllTextureImportsFromCLI  5_arena_teximport.log
else
  echo "（跳过 ⑤ ArenaBuilder.ApplyAllTextureImportsFromCLI；要跑加 --optional）"
fi

echo "=== $(date +%H:%M:%S) 全部结束 ==="
echo "日志：$LOG"
echo "📌 收尾核对（判据在脚本头）："
echo "   · ① 的日志末尾应有「B-144 收尾自检：booster 那 N 件 prefab 的悬空引用 = 0」（≠0 会打成 LogError）"
echo "   · ③ 的日志里 4 件开卡包 prefab 应全部 OK"
echo "   · 要独立复核悬空引用：python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py --fields m_Script,m_Sprite,m_fontAsset,m_sharedMaterial,m_Mesh,m_Texture,m_Asset <prefab...>"
