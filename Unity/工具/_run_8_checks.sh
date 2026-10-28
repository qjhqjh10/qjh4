#!/bin/bash
# 自检串行跑（Unity 同工程只能一个实例）
# 2026-09-26：老八条 + 🆕 联机三条（NetSelfTest / NetBattleTest / SettingsScene）
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
unset ELECTRON_RUN_AS_NODE
PROJ="D:\4\Unity\MyGame"
run () {  # $1=入口  $2=日志名  $3=筛法
  echo "=== $(date +%H:%M:%S) 开始 $1 ==="
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod "$1" -logFile "d:/4/_tmp_view/$2" >/dev/null 2>&1
  # 🔴 2026-09-28 修：原来这里写 `echo "... (退出码 $?)"` —— `$?` 前面先跑了 `$(date)`，
  #    所以它取到的是 **date 的退出码**，**恒为 0**（编译错误时也报 0 ⇒ 假绿）。
  #    必须**先存下来**再打印；并且顺带查「编译没过」那一行（Unity 编译失败时进程照样退 0/1）。
  local rc=$?
  local cc=""
  grep -q "Scripts have compiler errors" "d:/4/_tmp_view/$2" && cc="  🔴 **编译错误（这一条根本没跑）**"
  # 🔴 2026-10-03（**A61**）：**这里是全工程唯一盯「退出码」的地方** ——
  #    进程内的断言**抓不到「退出期」的段错误**（断言跑完了才崩），所以退出码只能在这儿看。
  #    139 = 128 + 11 = SIGSEGV。`BattleScene.Run` 那条是**已知的间歇段错误**
  #    （断言全绿、崩在打印「合计」之后；2026-10-01 一天 7 次里 6 次崩；2026-10-02 两跑都是 0）。
  #    ⚠️ **判据看日志末的「断言合计」，别只看退出码** —— 但也**别放过它**，它是真崩。
  local seg=""
  [ "$rc" = "139" ] && seg="  🔴 **段错误退出（139）** —— 已知间歇：**看「断言合计」判绿红，别拿退出码当判据、也别当没看见**"
  echo "    $(date +%H:%M:%S) 结束 $1 (退出码 $rc)$cc$seg"
}
run RuleEngineTest.Run  ruleengine.log
run BattleScene.Run     battle.log
run DeckScene.Run       deck.log
run ShellScene.Run      shell.log
run MainMenuScene.Run   menu.log
run RewardsScene.Run    rewards.log
run ShopScene.Run       shop.log
run CollectionScene.Run collection.log
# ---- 🆕 2026-09-26：联机三条（碰了 `Net/` 或 `BattleDriver` 的联机那一段就必须跑）----
run NetSelfTest.Run     net.log          # 传输 / 握手 / 心跳 / 掉线重连
run NetBattleTest.Run   netbattle.log    # 两个裸 context 真打一局 + 换牌定序 + 掐断重连重放
run SettingsScene.Run   settings.log     # 设置窗三页 + 联机页
# ---- 🆕 2026-10-17：卡面基座（⑨ 账上那条「`CardBaseDemo.Run` 不在本脚本里」的收口）----
#   它是**卡面 / 版面那两族**的必跑项（四档分辨率 + 悬停/让位/拖拽/落位/状态色），
#   原来只靠人手单发 ⇒ 常年漏跑。现在并入全套（🔴 仍然**串行**，同工程只能一个 Unity 实例）。
#   ⚠️ 它的另一半职责是**出截图**（`d:/4/_tmp_view/cardbase/`）—— 涉及版面时断言绿了**也要看一眼图**。
run CardBaseDemo.Run    cardbase.log     # 四档分辨率渲染 + 交互闭环（51 条断言）
# ---- 🆕 2026-10-24（`F2` · R1 P2-1）：AnimFX 模块层 ----
#   `AnimFXCheck` 这轮加了 **20+ 条**新断言（A8 +8 / B4 +8 / A4 的 EC57 组 / ⑤b–⑤g 语音段），
#   而它**不在本脚本里、全仓也没有第二个 `-executeMethod` 指向它** ⇒ 那些断言**永远不会执行**。
#   它比上面最轻的那几条还轻（纯数据 + 装配断言，不建场景、不出图）⇒ 并入全套。
#   ⚠️ 仍然**串行**（同一工程只能一个 Unity 实例）；⛔ 别改其它条目的顺序/参数。
run AnimFXCheck.Run     animfx.log       # AnimFX 模块层：模块覆盖 / 装配 / 引用解析 / 语音
# ---- 🆕 2026-10-25（第八十会话 · RU 核销审查查出）----
#   `Editor/CardFaceAssertE5.cs:74 → :528` **真调** `SoundCueAssertT.Run()`（10+1 条），
#   而它**不在本脚本里、全仓也没有第二个 `-executeMethod` 指向它** ⇒ 那些断言**只在手动单发时才跑**；
#   且 `Editor/SoundCueAssertT.cs:133/193` 自称「真正的非自指保障 = 它在批跑清单里」**这句现在是假的**。
#   判据出处：`资料/完全复刻/报告/RU_任务核销_第2轮.md` §④·1。⛔ 仍然**串行**。
run CardFaceAssertE5.Run cardface_e5.log  # 卡面音效 cue 断言（含 SoundCueAssertT 的 10+1 条）
# ---- 🆕 2026-10-27（第八十八会话 · `B88_E8b` 建、调度台并入）----
#   `Editor/AudioSnapshotCheck.cs` 断「混音快照名条数 = 枚举数 / 逐档 GetSnapshot 取得到 / SnapshotMisses == 0」。
#   加它的理由同上面两条：**现有宿主没有一个碰过 `WarpforgeAudio`**（`AudioSetup.PlayCheck` 在 batchmode 下直接拒跑）
#   ⇒ 不加进本清单，那几条断言**永远不会执行**。⛔ 仍然**串行**。
run AudioSnapshotCheck.Run audiosnap.log # 混音快照：条数 / 逐档取得到 / 0 miss
# ---- 🆕 2026-10-28（第八十九会话 · `B89-E9` 建、调度台并入）----
#   `Editor/LoadableRefCheck.cs` 断 `LoadableReference<T>` 的 12 条原版语义（`Equals`/`GetHashCode`/
#   两个 `implicit`/序列化三钩子），含**「`Equals(null)` 原版抛 NRE」**那条灭自证断言。
#   加它的理由同上面几条：**没有第二个 `-executeMethod` 指向它** ⇒ 不加进本清单，36 条断言永远不会执行。
#   ⚠️ 该宿主**只跑纯函数，不建场景、不出图**。⛔ 仍然**串行**。
run LoadableRefCheck.Run loadable_ref.log # LoadableReference<T>：12 条语义 / 36 断言
# ---- 🆕 2026-10-28（第八十九会话 · `B89-E10` 建、调度台并入）----
#   `Editor/PlayerDataMathCheck.cs` 断 `Core/PlayerDataMath.cs` 的六条纯函数（`GetMD5Hash` 空串返 `""`
#   不是 null · `AreDecksEqual` 的**不对称** · `GetXPFromMatch` 的分支表（含 `Campaign` 返 130 那条原版 bug））。
#   P0 相关：`GetXPFromMatch` 是「赛后 XP 一分不发」的那条判据本体。判绿红看日志里的 `PDMATH 合计`。
#   ⚠️ 纯函数，不建场景、不出图。⛔ 仍然**串行**。
run PlayerDataMathCheck.Run pdmath.log # 玩家数据纯函数：MD5 / 卡组相等 / XP 分支表
# ---- 🆕 2026-10-28（第八十九会话 · `B89-E11` 建、调度台并入）----
#   `Editor/GameModesCheck.cs` 断 `Shell/GameModesHandler.cs`（`LiveOpsHandler` 的 GameModes 子类）：
#   `DeserializeSharedData` 三条回落（`Classic→Ranked→new`，`FastMode` 无 Ranked 级回落，第三条取整份）
#   · `SetResultCount` 三支 +1 且 `default` 必须排在 `GetValuesForMode` **之前**（否则未知 winner 会往战绩表塞空记录，静默）
#   · `CombatFinished` 末尾清 `CurrentPlayingEvent`（原版行为，同一局只分派一次）。
#   ⚠️ 纯逻辑，不建场景、不出图。⛔ 仍然**串行**。
run GameModesCheck.Run gamemodes.log # GameModes：共享数据回落 / 战绩计数 / 分派一次性
# ---- 🆕 2026-10-28（第八十九会话 · `B89-E12` 建、调度台并入）----
#   `Editor/GameStaticDataCheck.cs` 断 `Shell/GameStaticData.cs`（13 格默认值 + `CheckIfNewCards`/`FoundNewCard`）
#   与 `Core/Tooltip.GetTraitDesc` 的 `isAlt` 支（3 条 `CardTraitDescription/*Alt` 键已在官方表里）。
#   ⚠️ `LoadSharedDefaultValues` 的 13 个默认值全部现读 `dump.cs:119411-119520`；`hiFPS = true`、
#   `smallScreenUI`/`useCombatAutoZoom` 是**同一个值**、`graphicsQuality` 默认 **4（Ultra）**（`.c` 全文 = `return 4`）。
#   ⚠️ 纯数据 + 纯函数，不建场景、不出图。⛔ 仍然**串行**。
run GameStaticDataCheck.Run gsd.log # GameStaticData 13 格默认值 / 新卡判定 / 词条 Alt 文案
# ---- 🆕 2026-10-28（第八十九会话 · `B89-E6b` 建、调度台并入）----
#   `RuleEngine/Editor/EnchantmentTemplateCheck.cs` 断卡表 v37 的两列（`whileInPlayAnim` / `permanentBuffs`）
#   与 `AttachAbilityDamage` 的 null vs [] 两档（26 条，含 4 条灭自证 + 全池下限）。
#   🔴 **`HasAbilityDamage` 全仓 0 消费者**、`DoDeal` 90 档读的是 `AbilityDamageAt`（对 null 与 [] 同回 null）
#   ⇒ 这两列**今天不改任何卡的结算**；加它的理由是「数据侧先落地、消费端后接」时别丢断言。
#   ⚠️ 纯数据，不建场景、不出图。⛔ 仍然**串行**。
run EnchantmentTemplateCheck.Run enchant_tpl.log # 卡表 v37 两列 / abilityDamage 的 null 与空数组两档
echo "=== $(date +%H:%M:%S) 全部结束（老八条 + 联机三条 + 卡面基座 + AnimFX + 卡面音效 + 音频快照 + LoadableRef + PlayerDataMath + GameModes + GameStaticData + EnchantmentTemplate = 20 条）==="
# 🔴 **2026-10-18 就地订正（铁律 5）**：这一句原来写「= **13** 条」—— **错了**，实读脚本的 `run` 调用**只有 12 个**
#   （`grep -c '^run '` 会数出 13，因为它**把上面那句函数定义 `run () {` 也算进去了**；
#    判据 = 本脚本的 12 个入口 + 本脚本跑出来的日志里**只有 12 行「开始」**）。
#   错因溯源：`2cfb357` 曾把它改成 12、`ff6a612` 又改回 13 ⇒ **13 是 2026-10-18 引入的一处回归**，
#   而全仓的文档跟着抄了三份（11 / 12 / 13）⇒ **改这里就是改源头**（⛔ 别再改回去）。
# 🔴 **2026-10-24 再订正（`F2` · R1 P2-1）**：上面那段**仍然成立**（它记录的是 2026-10-18 那次回归）。
#   **现在**（本行起）= **14 个 `run` 调用** —— 2026-10-24 新增 `AnimFXCheck.Run`（12→13），
#   2026-10-25 新增 `CardFaceAssertE5.Run`（13→**14**，见上面那条 RU）。
#   2026-10-27（第八十八会话）再增 `AudioSnapshotCheck.Run`（14→**15**，见 `B88_E8b` 那条注释）。
#   2026-10-28（第八十九会话）再增 `LoadableRefCheck.Run`（15→**16**，见 `B89-E9` 那条注释）。
#   2026-10-28（第八十九会话 · 同日第二批）再增 `PlayerDataMathCheck.Run`（16→**17**，见 `B89-E10` 那条注释）。
#   2026-10-28（第八十九会话 · 同日第三批）再增 `GameModesCheck.Run`（17→**18**，见 `B89-E11` 那条注释）。
#   2026-10-28（第八十九会话 · 同日第四批）再增 `GameStaticDataCheck.Run`（18→**19**，见 `B89-E12` 那条注释）。
#   2026-10-28（第八十九会话 · 同日第五批）再增 `EnchantmentTemplateCheck.Run`（19→**20**，见 `B89-E6b` 那条注释）。
#   判据照旧：`grep -c '^run '` 会得 **21**（含函数定义 `run () {`），减去那一行 = **20**；也可以数日志里的「开始」行。⛔ 以后加/删条目时**只改这一处与 `echo` 那一句**。
