# B121-WE · `BattleDriver.cs` 注释 + 驱动侧三条 + 死代码 C08

> 执行写手 `B121-WE` · 2026-11-03 · 独占 `CardPresentation/Battle/BattleDriver.cs`
> 判据优先级：`d:/2/tools/decomp_full/`（一手）> `B121_CMT_…` §三/§五（本波任务书）> 既有判据（**用前已现读代码**）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改账主表 / `活状态表` / 正本 / 别人的文件。
> **行数守恒：改动前 41,742 行 → 改动后 41,742 行**（`git diff --numstat` = 91/91）。

---

## §A 注释（用户 2026-11-03「做好战斗部分代码注释」）

> 全部采用**零行数**手法：① 就地改写已有注释行（A 法）② 方法体 `{` 行尾追加（B 法，`{` 行不是声明行、不是账落点）。
> 共改 **66 行**（57 条段标题 + 8 处方法体 `{` 行尾 + 1 处 XML doc 首行）。

### P0（1 处）

| 段/方法 | 注释要点 | 判据 | 零行数 |
|---|---|---|---|
| `SetFactionResourceVisible:11770`（`{` 行 = `:11771`） | 【功能】**阵营资源显隐闸**：修女会「信仰」4 件（图标/数字 × 我·敌）+ 灵族「灵魂石」6 件（图标/宝石/数字 × 我·敌）一次性开关；⛔ **不含任务点**；调用点只有 `BuildHud`（开局全藏）与 `UpdateHud`（每帧按阵营判，0 值也显示 `0`） | 原版 `PlayerManager.ResetMana` 三条 getter（`cmp [rcx+0x2c], 30/80/110`）→ `ManaTypeHolder.Toggle`；`ManaTypeHolder__Toggle.c:17` = `SetActive`（机器码级出处见本文件 `ShowsQuestPoints` 的 doc） | ✅（`{` 行尾） |

### P1（9 处主链入口）

| 方法 | 功能句要点 | 判据 | 零行数 |
|---|---|---|---|
| `ShowAsk:11236` | 效果要玩家选牌 / 三选一 / 选效果时弹面板摆候选（三支 verb）；面板池与结算池**同刻同序**（→ `ChoicePanelPool`） | `BattleManager__ChoiceOfCardPlayer.c` · `__GetChoiceOptions.c:36` | ✅ |
| `DrivePlayerTurn:14925` | 玩家回合主循环（拖牌 / 点选 / 攻击 / 技能 / 结束回合），含回放与联机两条分支闸 | `BattleManager.Update` 的 status 状态机 | ✅ |
| `DriveAiTurn:19043` | AI 回合驱动（`SimpleAI` 一步一延时，含 `GetEndTurnDelay` 收手延迟）；联机局不跑 | `SimpleAI.PlayTurn` · `AIBotsConfig.GetEndTurnDelay` | ✅ |
| `PlaySignals:19516` | 引擎事件流 → 演出时间线（批内阻塞窗 + 血账 `_pendingHp` + 逐事件排片） | `BattleManager.WaitingForBlockingAction` 那一族 | ✅ |
| `SyncBoard:26597` | 引擎棋盘 → 场上卡视图同步（含「插入推人」/「离场补位」后的视图回收）；349 行原本 0 段标 | `MinionManager` 两条 `List` · `__ReassembleMinionsWhilePlayingUnit.c`（`BattleManager__Update.c:575`） | ✅ |
| `BuildHud:28646` | **战斗 HUD 主链**（场上卡 / 手牌 / 牌库 / 坟场 / 能量 / 计时 / 按钮），只能建一次；非主路径 → `BuildHudExtras` | `FP-HUD-002` + 本文件各件注释 | ✅ |
| `BuildHudExtras:29975` | HUD **非主路径**那批件（头衔条 / 头像 / 联盟钮 / 任务点 / 能量累积灯 / 加时 splash）；603 行原本无方法头 | 各件注释里逐条带的 `menu_dump` 框与 prefab 字段 | ✅ |
| `UpdateHud:32063` | 每帧把 HUD 的字 / 灯 / 条刷成引擎真值；只读 `Ctx`、只写视图件 | 同上 | ✅ |
| `Begin:4924` | 开局主链：建档 → 摆视角 → 发牌 → 换牌入口 → 首个回合；教程 / 联机 / 回放三种开局都从这里进 | `BeginFromPendingCore`（`:4090`） | ✅（改写 `:4899` 的 `/// 开局。`） |

### 段标题「功能名前置」（57 条，覆盖战斗主链）

改法：`//  🆕 日期（代理·账）：**功能名**` ⇒ `//  功能域 · 功能名 —— 🆕 日期（代理·账）`。**只改字、不改行**。
覆盖：手牌（抽牌终点锚）· 出牌（两段式目标 / 目标型卡 / 进攻防御卡 / 防御池闸 / reveal / 中段交接 / 中央展示 / 满手牌）·
攻击（反击演出）· 回合（时钟 / 切后台补倒计时）· 技能（面板事件 / 一次技能 N 拍 / 位移泵）· 教程（开局链 / 表现层 /
执行器接线 / 可跳过动作 / 动作音效 / 五档聊天）· HUD（Canvas 三件 / 钮命中矩形 / 按钮反馈 / 能量累积六步 / 主链标题）·
回放（局内出口 / 第三条记账口）· 联机（对面表现三口 + 等对手收尾 / 完成收尾）· 结算（出口 / 交排位数据层）·
演出（流水线 / 阻塞闸）· 表现（开局运镜 / 扬尘 / 飞行单位 / 卡体动画层）· 聊天（AI 发言 / 督军消息）。

**量化（现读）**：`//  🆕 20` 开头的注释行 **132 → 81**；「功能域 · 」开头的段标题 **0 → 55**；
`Update` 的段标题从「每帧」补成「每帧主泵（`Update`）：时钟 / 输入沿 / AI 回合 / 演出流水线 / HUD 刷新」。

---

## §B 驱动侧三条（`活状态表` B120-B02 / B03 / B11）

> 🔴 **现读结论：三条的驱动侧【在 2026-11-03 开工时已经落地】**（第一百二十会话 `B120-W2b` 做的）。
> 本波**没有重复改**（重复改只会制造两份口径），改为**逐条回源核对 + 用断言钉死防回退**（`Editor/B121_WE_Assert.cs` B 段）。

| 条目 | 现读证据（`BattleDriver.cs`） | 原版判据 | 断言 |
|---|---|---|---|
| **B02** 技能面板可用层 | `:15757` `_uSlot = Array.IndexOf(Ctx.Players[_me].Board, u)` → `RuleCore.CanStartActiveAbility(Ctx,_me,_uSlot) == RuleCodes.OK`；`:15782` `canUse: _canUse` 传给 `SkillPanel.Show` | `BattleManager__CanUseActiveAbility.c`（`+0x40` 比对 + `+0x244/+0x246` 可用位）；我们的等价物 `RuleCore.cs:15488` | B02（含灭自证：⛔ 退回 `ShownTargets > 0`） |
| **B03** 教程高亮 relation | `:18026` `int rel = tp != null ? tp.positionRelation : 0; float dx = TutorialTipOffset(pref);`（与同段 `SetTip` **同源**）· `:18027` `ov.SetHighlight(true, c, sz, (TutRelation)rel, dx)` | `AiScripted__GetTipPosition.c` 末段三条 relation 偏移；高亮与提示过**同一个** `GetTipPosition(..., positionRelation, …)` | B03（三参旧形态 ⇒ 红） |
| **B11** 设置窗 Skip tutorial | `:18243` `SkipTutorialFromSettings()` → `:18247` `ov.TrySkipFromSettings()`（组件侧 `TutorialOverlay.cs:1795` **不查** `SkipAllowed`） | `BattleSettingsWindow__SkipTutorialButtonOnClick.c` 全文两句（`CloseWindow` + `ClickSkip`）；`minTimeBeforeSkip` 是 `TutorialTipScript` 的字段 | B11 + **灭自证**：该链 ⛔ 不再出现 `ov.TrySkip()` |

---

## §C 死代码 C08

**删**：`readonly List<Tween> _attackTweens`（`:40608`）· `void ApplyReplaySkipToLiveAttackTweens(bool skip)`（`:40625`）·
以及 `NoteAttackTween` 体内那两行登记表维护。**零调用点**依据：`SetReplayPaused` 的唯一调用 `B120-W2b` 已停用（`:8457` 注释），
提速改由 `CardFeel` 的 `stepScale` 形参**按段**做（原版 `get_AttackStepTime` 在**建补间那一刻**求值）。

**保留了 `NoteAttackTween`（纯转发 `return t;`）**：三处调用点（`AttackRecover` ×2 / `MeleeAttack` ×1）与
`Editor/B117_WBD_Assert.cs:327-329` 的源级断言仍指着它 ⇒ 删它要连改 4 处、收益为零。

**🔴 为什么删掉的位置改写成「同长度墓碑注释」而不是直接删行**：本文件账引用 **400+ 处**，`:40606` 之后还压着
`RecodeKindSkipTutorial` / `SkipTutorialFromSettingsForTest` / `ChoicePanelPool` 等落点（`账` 里 `>40600` 的引用 **11 条 / 8 个不同行号**）。
直接删 19 行 ⇒ 这些行号整体 −19 ⇒ **3 条进「失败」（次要落点落到空行）+ 1 条漂移**（实测过，见 §验证「反面记录」）。
改成**同长度**墓碑（19 行注释 + 9 行纯转发 = 原 28 行）⇒ **零漂移**，而且把「为什么删 / 判据 / ⛔ 别加回来」写在原地。

**同步要改的断言（不在我白名单 ⇒ 交补丁）**：`Assets/CardPresentation/Editor/B117_WBD_Assert.cs:330-333`
原文：
```csharp
        Check(src.IndexOf("ApplyReplaySkipToLiveAttackTweens(on);", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）`SetReplayPaused` 会把**在飞**的攻击补间一起提速 / 复原"
            + "（原版暂停之后，正在 `WaitForSeconds(get_AttackStepTime())` 的协程立刻变快）");
```
换成（**现在是红的** —— 墓碑里没有 `(on);` 这个形态，所以这条断言已失效，必须改）：
```csharp
        Check(src.IndexOf("void ApplyReplaySkipToLiveAttackTweens", StringComparison.Ordinal) < 0
           && src.IndexOf("readonly List<Tween> _attackTweens", StringComparison.Ordinal) < 0
           && src.IndexOf("_attackTweens.Add(", StringComparison.Ordinal) < 0,
              "★ ⑤（源级·灭自证）给在飞攻击补间提速那条路**已删**（2026-11-03 `B121-WE` · 死代码 `C08`）"
            + "：原版 `get_AttackStepTime` 在**建补间那一刻**求值，事后改 `timeScale` = 双重缩放"
            + "｜🧨 把它加回来 ⇒ 本条红");
```
`:327-329` **不用改**（`NoteAttackTween(...)` 两处调用仍在）。

---

## §验证

| 项 | 结果 |
|---|---|
| 行数守恒 | ✅ 41,742 → 41,742（`git diff --numstat` = **91 / 91**）；CRLF 41,741 / LF 41,741 / 裸 CR **0** |
| `TMPDIR=/tmp/wf_we bash 工具/typecheck.sh` | ✅ **0 / 0**（收工前最后一次跑；中途跑过一次 1 运行时 + 5 编辑器错，全部落在同批别人的在飞文件 —— `RuleEngine/Data/CardDatabase.cs:5140` · `Editor/B121_C3_Assert.cs:90` · `Editor/B121_WB_Assert.cs:208-216` —— 那些写手收工后已归零） |
| `check_ledger_points.py` | ✅ **漂移 0 / 失败 0**（合计 A 5876 条 · 通过 5876）。中途跑过一次 37 漂移 + 7 失败，**全部落在 `CardPresentation/Shell/CollectionWindow.cs`**（同批别的写手的在飞状态，收工后已归零）；**`BattleDriver.cs` 相关从头到尾 0 条** |
| 断言 | 新建 `Assets/CardPresentation/Editor/B121_WE_Assert.cs`（**21 条**，全**源级**、不需要 driver）—— ⚠️ **接线由调度台做**：`B121_WE_Assert.Run(); pass += …Pass; fail += …Fail;` |

**反面记录（踩过，别再犯）**：第一版是**直接删 19 行**，`check_ledger_points` 立刻从 `漂移 0 / 失败 0` 变成
`漂移 1 / 失败 3`（失败 = 次要落点 `:41018` / `:41363` 变成空行）。⇒ 本文件**任何**增删行都要先问「下游还有账落点吗」。

---

## §顺手发现（只报，未改）

1. 🔴 **`B121_CMT` §1·4 #1 的描述要就地订正**：它把 `SetFactionResourceVisible` 写成「三阵营资源条（修女信仰 / 灵族灵魂石 / **任务点**）」。
   **实测它只覆盖信仰 + 灵魂石两组（10 件）**，任务点那组走 `QuestPointsVisible`（`:11696`）/ `ShowsQuestPoints`（`:187`），
   账里也是分开落的（`战斗界面_HUD.tsv:541/543/550` vs `:542`）。本文件的 P0 注释**按实测写**（并写明「不含任务点」）。
   该报告不在我白名单 ⇒ 请调度台订正那一格（同一句话若被复制到别处，一并改）。
2. 🔴 **`B121_CMT` §一 的「112 个 `// ====` 段标题、96 个以 🆕 开头」与现读不符**：
   现读 `// ===` 开头的块标题 **335** 个；`// ===` 后**紧跟**「🆕 日期」标题的 **106** 个；`//  🆕 20` 开头的**注释行** **132** 条。
   ⇒ 引用这些数时请按本行口径（或现读）。
3. **任务书里「类尾 `:40205`」是段标题、不是类尾**：`:40205` 是 `// ====`；`BattleDriver` 类的真类尾在 **`:41658`**
   （`LightPlayableHandNow` 之后；同文件 `:41639-41641` 那段自我提醒写着「一律追加在类尾」），
   文件最末 `}` 属于 `ChoicePanelPool`（`:41693` 起）。
   本波**没有新增成员**，所以没受影响；将来加成员请落到 `:41658` 之前。
4. **`B117_WBD_Assert.cs:330` 现在就是红的**（不是我改坏的 —— 是 `B120-W2b` 停用 `SetReplayPaused` 那条调用时就红了：
   那句源码文本 `ApplyReplaySkipToLiveAttackTweens(on);` 当时已被注释掉）。本波把方法删掉后**更不可能变绿** ⇒ 按 §C 的补丁替换。
5. `BattleDriver.cs` 里 **4 条账行落在 `:11770`**（`战斗界面_HUD.tsv:541/543/550` + `ManaTypeHolder.Toggle` 那条）——
   本波注释按「`{` 行尾追加」写，`:11770` **一字未动** ⇒ 那 4 条零漂移（这也是不把注释插在方法签名上方的原因，铁律 §五 D）。
6. `B121_WB_Assert.cs` / `B121_C3_Assert.cs` 的 5 条编译错是**同批别的写手**的在飞状态，我没有碰。
