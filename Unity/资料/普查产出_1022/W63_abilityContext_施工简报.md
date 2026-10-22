# W63 · `abilityContext` 进卡表 施工简报（只读代理产出，2026-10-22）

> 结论：判据已闭合、可施工。分块方案见 §5；**唯一真串行点 = `ActionPump` 的行签名先定死**。
> 本文是 `T03·c` / `T03·d` 里「出队按施放者复核做不到逐卡精确」的唯一收口路径。

## 1. 它是什么、几种取值、谁要读

`abilityContext` = 原版 `CardAbility.abilityContext`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs:10`，字段偏移 **+0x14**，枚举 `AbilityContext.cs:1-9`）。
语义 = **「这条 ability 的载体（thisCard）此刻必须待在哪个区域」**，只在 `CardAbility.CanTriggerAbility` 里经 `IsRightContext` 判一次（`decomp_full/CardAbility__IsRightContext.c`）：

| 值 | 名 | 判据（反编译逐句） |
|---|---|---|
| 0 | Play | `IsInPlay && !EnoughPendingDamageToDie && !isRemnant(+0x65)`（`:18-32`） |
| 10 | Hand | `IsInHand`（`:34-38`） |
| 20 | Deck | `cardState & ~0x10 == 0` ⇒ inDeck(0)/inDeckDrawing(16)（`:41-47`） |
| 30 | Cemetery | `cardState == 6`（`:51-55`） |
| 40 | InPlayOrDying | `IsInPlayOrDying` = 状态 {2,3,15,17,**5 待死**}（`:52-55`） |
| 50 | InPlayOrCementery | `IsInCemetery \|\| IsInPlay`（`:66-72`）—— 🔴 **不含状态 5（待死）**，与 40 不等价 |

🔴 三条口径：

1. **不是所有触发都读它**：`CanTriggerAbility.c:69-95` 对 trigger ∈ **{0 ThisCardPlayed · 80 ThisUnitDeath · 145 Codex · 220 Sacrifice · 500 TriggerEnchantment}** 整段跳过；600/605 在余额判定前显式检查（`:85-124`）；其余走 `:126` 兜底。⇒ 数据全量落，消费端照这张 skip 表。
2. **它不是「出队」时读的**：`IsRightContext` 全库只有 `CanTriggerAbility` 一个调用者，由 `RawCardScript__TriggerAbilities.c:108,207` 在**广播那一刻**同步调 ⇒ 原版闸在**入队前**。出队侧另有「施放者还在不在」= `BattleAction.checkIfCasterStillInPlay`(+0xD0) ＋ `AbilityLogic.checkIfCasterInPlay`(+0x74，1,873 条全 0 休眠)。⇒ 现有 `QueuedAction.RequireActorInPlay` 是**原版入队前那道闸的代理**，不是 `checkIfCasterInPlay` 的对应物。
3. 旧报告 `W61b_引擎_T03c审查修复.md` §四·2 把 `spirit/faith` 写成「50 ⇒ 判不出」——**不成立**：600/605 走 `:85-124` 真检查；「50 = 待死也照跑」也不成立（状态 5 不满足 50）。

## 2. 数据侧：位置、形状、现读分布

- `资料/普查产出_1018/allcards_1350/cards_raw.json` → `cards[].cardAbilities[].abilityContext`；`cards_engine.json` 现 v13、**无此列**。
- 现读（1351 张 / 1,496 条，**0 条缺键**）：`{0:1317, 50:86, 40:75, 10:17, 30:1}`。
- 限定我们池（1127 张，全部有真值）：非 0 = **173 条 / 157 张**（`{50:85, 40:71, 10:16, 30:1}`）。
  ⚠️ **2026-10-22 就地订正（铁律 5）**：原写「159 条 / 148 张（`{50:71,…}`）」**不成立** —— 任何口径都复现不出。生成器实测：原始非 0 = 173/157；**合并 3 条同 `(trigger,context)` 重复行后 = 170 条**（`SOR56` / `Catechism of Death` / `UM116`，不合并会被消费端当「同 key 重复」整组丢弃）。`20 Deck` 在池内 = **0 条**（值域里有、数据里没有）。
- ⚠️ **以下为全 1351 张口径（不是池内）**：按 trigger 的非 0 分布：`50 UnitAttack {0:5,40:9}` · `90 OtherUnitDeath {0:18,50:11,40:1,10:2}` · `145 Codex {0:34,50:3}` · `301 Regiment {40:10,50:3}` · `410 FollowUpResolution {50:17,0:12,30:1,40:2}` · `580 Strike {0:29,40:5}` · `600 UseSpiritStone {0:16,50:13}` · `605 UseFaith {0:14,50:15}` · `670 Ambush {40:11,0:1}` · `715 Ecstasy {40:8,0:2}` · `81/130` 全 40 · `0 ThisCardPlayed {0:745,50:22,40:1,10:4}`。
- 冲突：同卡同 trigger 两个 context = **11 张，全在 trigger 0**（都是战术卡）；同卡同关键词冲突 = 0。

## 3. 接线链（照 `triggerPriority` 模板）

| 跳 | `triggerPriority` 现有实现 | `abilityContext` 要加什么 |
|---|---|---|
| 生成器 | `gen_cards_engine.py:1187-1214/1217-1265/2772-2774/3239/3265-3271/4100-4119/4145-4151` | `ability_contexts_of()`，落 `entry["abilityContext"]`，**version 改 14**，读数出声 |
| 卡表 | `Resources/cards_engine.json`（单行） | 每卡多一列 |
| DTO | `Data/CardDatabase.cs:33/264/292-315` | `AbilityContextDto[] abilityContext`（**`-1` 哨兵**，别用 0） |
| version 守卫 | `CardDatabase.cs:441-446`（`<13` 出声） | `<14` 出声 ⇒ **回退「未知」而不是 Play(0)** |
| 映射 | `CardDatabase.cs:617-652/490-525` | `MapAbilityContext`：缺键/非法值/重复 ⇒ 出声+丢弃 |
| CardDef | `CardDef.cs:590/1167-1177` | 可选形参 + `AbilityContextOfTrigger(name)` + `AbilityContextOfKeyword(kw)`；`-1` = 未知 |
| 第二解析路 | `工具/ruleprobe/Program.cs:540-593/594-607/627-639/528` | 同步补，否则探针恒 `-1` |

JSON 形状（JsonUtility 不支持字典）：

```json
"abilityContext": [{"trigger":50,"name":"UnitAttack","context":0},
                   {"trigger":580,"name":"Strike","keyword":"strike","context":40}]
```

`keyword` 只在 `ABILITY_TRIGGER_KEYWORD` 有映射时出现。⛔ 只按 keyword 落会漏掉 trigger 50（`UnitAttack`）⇒ **恰恰修不了点名的 5 张卡**；关键词路只覆盖 173 行里的 53 行（池内原始非 0 173；合并后 170）。

## 4. 消费点

| # | 位置 | 现在 | 要改成 |
|---|---|---|---|
| C1 | `Core/ActionPump.cs:207-214/229-240/336` · `Core/ActionQueue.cs:79-88` | 按关键词族猜；`UnitInPlay` 只实现 Play(0) | `IsRightContext`：0=在场且活着；40=在棋盘（含待死）；50=在场或 `Discard`；10/20/30 按 `Hand`/`Deck`/`Discard` 查 `Instance`；`-1` 回退旧表 |
| C2 | `Core/EffectResolver.cs:7368-7375/5869-5874/7237` | 预调无条件放开、棋盘循环无条件挡 | 按 `TriggerNameOfWhenKind(kind)` 查 context；**这条修 UM83/GSC12/DA22 多响与 SAU31/UM70 少响** |
| C3 | `EffectResolver.cs:6371-6376/6545-6548` | 一律 `require:false` | 按 600/605 的 context（0 该判在场） |
| C4 | `RuleCore.cs:5748/8447/8541/8624/8809/9398/10467` | 手写 `require:false` | 由数据取 40（行为不变、不再硬编码） |
| C5 | `RuleCore.cs:10370-10381`（唯一关键词入队口） | `ActorMustBeInPlayForTrigger(keyword)` | 换成 `AbilityContextOfKeyword` + skip 表（codex/backlash/sacrifice 不判） |
| C6 | `EffectResolver.cs:6222-6227/8859-8880` | `require:true` | 数据确认后可数据驱动 |
| C7 | `EffectResolver.cs:6206-6207/7267-7277` | 无 `UnitState` 施放者 | context 10/20/30 的卡级监听器今天表达不出 |

## 5. 分块施工计划（文件互不相交）

| 块 | 文件（独占） | 内容 | 依赖 |
|---|---|---|---|
| **A** | `工具/gen_cards_engine.py` | version 14 · `ability_contexts_of()` · 落列 · 读数/出声 | 契约 |
| **B1** | `RuleEngine/Core/CardDef.cs` | `AbilityContextSpec` + 访问器 + 常量 | 契约 |
| **B2** | `RuleEngine/Data/CardDatabase.cs` | DTO/哨兵/version<14/映射/传参 | B1 共契约、文件不交 |
| **C** | `Core/ActionPump.cs` + `Core/ActionQueue.cs` | `IsRightContext` + 泵点读 context；旧表降级为 `-1` | B1 |
| **D** | `Core/RuleCore.cs` | C5 + C4 | B1、C |
| **E** | `Core/EffectResolver.cs` | C2 + C3 + C6 | B1、C |
| **F** | `工具/ruleprobe/Program.cs` | 读列 + 传参 + 出声 | 契约 |
| **G** | 引擎测试文件 | 「context 0 挡 / 40 放 / 50 看墓地」断言；C1 改签名要同步 T27 | C、D、E |

A/F 与 C# 侧完全可并行；D/E 文件不同可并行；**唯一真串行点 = C 的行签名先定死**。
生成器跑完核 `--check`；B1-B2/D/E 属引擎共用件，同步点要跑 `RuleEngineTest.Run`。

## 6. 风险 / 兼容

1. `ruleprobe` 是第二解析路：不传 ⇒ 探针恒 `-1`，context 相关读数被静默钉死成旧口径。
2. 旧表（version<14）缺列**不能**读成 Play(0)（会把死亡族/墓地族掐死）⇒ `-1 = 未知` + 出声。
3. 幂等：新列必须按键排序（`sorted` by trigger）才字节稳定；`--check` 会自动报过期。
4. **version 14 撞名**：`CardDatabase.cs:654` 的 `MapSkipDraws` doc 已写「v14（2026-10-21）」但当时没 bump ⇒ 本次要就地订正。
5. **墓地监听者根本没被扫**（`EffectResolver.SnapshotBoardListeners:7286-7315` 只扫棋盘，`:7301` 无条件跳过待死）⇒ context 50 的 11 条 `OtherUnitDeath` 即使接列也不会响；独立缺口。
6. 棋盘循环的待死闸不许一起放开：`:7301` 对应原版 `IsInPlay`，只有**预调**两跳可按 context 40 放开。
7. **C1 语义风险（需裁）**：原版在入队前判、我们出队判 ⇒「入队时活着、被后面动作打死」原版照跑、我们取消（更严）。改入队时判会让 T27 ②③ 红。
8. 冲突卡（11 张战术卡 trigger 0）：按「不同 context ⇒ 整组丢弃 + 出声」，消费端未知回退 = 宽松方向。

## 7. 没查清

1. `checkIfCasterStillInPlay` 的确切读取点未定位（结构体拷贝掩盖；只在字符串表见到 " no longer has caster in play, so is ignored"）。
2. 原版入队前判 vs 我们出队判是否可观察等价 —— 需裁。
3. 11 张战术卡的第二条 ability（context 50）是不是 410 的后半段 —— 没核。
4. 监听 kind → trigger 只覆盖 12 个；`Kills / GainSpirit / GainFaith / GainQuest / GetsHuntMark / GetsShield / GetsMarkerlight / GetsStun / LosesStealth / BecomesRemnant / CreatesSecret / CreatesSabotage` 没有对应 trigger 名 ⇒ 只能读到「未知」。
5. `+0x65 = isRemnant` 与我们的「变残骸换 `UnitState`」是否等价 —— 未逐行核。
6. context 10/20/30 落在卡级监听器时引擎没有 `UnitState` 载体，需另设入口。
