# B121-DATA 子报告 · 战斗界面 / 表现 / 音效 / 配置侧

> 只读普查代理 `B121-C`（2026-11-03）。范围 = `CardPresentation/{Battle,Core,Hand,Board}` 的表现层 · `CardPresentation/Resources/*` 配置表 ·
> 音效/语音/BGM · 动画数据 · HUD 配置 · 战役脚本。**未改任何源码/数据/场景，未跑 Unity，未动 git。**
> 问题口径 = 「**代码已经写了，但没有对应的数据/资源/配置让它生效**」。

---

## 一、总表

| 编号 | 功能 | 类别 | 代码落点(文件:行) | 缺什么数据 | 当前行为 | 原版判据(有/无 + 出处) | 补数据来源 | 优先级 |
|---|---|---|---|---|---|---|---|---|
| **VIS-01** | 进攻环境卡（39 张）的 ability 执行链 | **A** | `RuleEngine/Data/CardDatabase.cs:4077-4104`（DTO）· `Battle/CampaignScriptRunner` 无关 · `Battle/BattleDriver.cs:37468-37478`（`ApplyEnvWarlordAbilityOnce`） | **缺「DTO → ability 树」的映射器**：41 行 logic 已在 `offensive_env_cards.json`（chosenAbility 30×34 / 40×3 / 60×3 / 0×1，buffType 12×39 / 15×2，enchantmentId 41/41 命中 37 个模板），37 个模板已在 `enchantment_templates.json` | 表读完、`TriggerOps("thiscardplayed")` 恒 null（desc 是触发句、keyword 空）⇒ 督军那一跳空转；换环境照做、**环境效果不落地** | **有**：`AbilityLogic__PlayAbility` → `CardScript__AddEffect` case 12/15（附魔挂载） | 数据已在本地；补 `Core/Enchantment.cs` 的 chosenAbility 30/40/60 挂载口（B93_E16 §4 有逐处清单） | **P0** |
| **VIS-02** | 逐卡「召唤」clip 时长（多张部署间隔） | **A** | `Core/EventTiming.cs:933-943`（`DeployHoldOf`） | `card → summon CardAnim / AnimInfo.GetAnimDuration` 的逐卡映射 | 恒 **1.0 s**（来自 `Summon Troop Tween.duration`，而该资产 `waitAnimation=0` 非阻塞） | **有**：原版无间隔字段，相邻召唤 = 上一张整条协程 = 0.45 s + 按卡 summon clip（`_ResolveSummonUnit_d__510:100-150,266-301`） | `cards_raw.json` 的 activeAbility/ability cardAnimations + `cardanim_clips.json`（`vd+ts+tm+te`）；缺的是「哪条 CardAnim 属于 summon」的归类 | P1 |
| **VIS-03** | 替代能力（`alternativeAbility`）的动画时长与分支 | **A** | `Core/AbilityAnimDurations.cs:62-72`（注释态）· `Core/EventTiming.cs:119` `AbilityUsesAlternative` | **`cards_engine.json` 缺两列**：`normalAbilityBoardCriteria` / `hasAlternativeAbility` | `AbilityUsesAlternative` 挂钩拿不到判据 ⇒ 恒走普通支；SW21 的 0.2 s 替代分支切不到 | **有**：`cards_raw.json` 的 `full_raw.normalAbilityBoardCriteria`（1351 张全带结构体，非默认 2 张） | `工具/gen_cards_engine.py` 补两列（卡牌线文件） | P1 |
| **VIS-04** | 原版音效 cue 覆盖 + 双向对账 | **A / D** | `WarpforgeVFX/Runtime/WFSoundBank.cs:43-50`（三表合一） | **610 个 cue 资产里 73 个不在任何表**（现读，见 §二·1）；缺「cue → 调用方」双向对账 | 这 73 条不响；调用方未知 ⇒ 无法判断是原版就没用、还是我们漏接 | 资产在 `bundle_soundcollection_assets_all/MonoBehaviour`（现数 610）；**调用方未对账** | 从 `decomp_full` 找 `AssetReference` 持有者；9 条 `Trait *` 与 7 个有 prefab 的 trait 同名（见 §二·2） | P1 |
| VIS-05 | 单位「攻击/阵亡」聊天气泡文本 | B | `Battle/BattleDriver.cs:7580-7600` | —（原版本就空） | 只放音、不弹泡（2026-10-24 E4 已照原版改） | 无：`voice_lines.json` 现读 attack 0/56 · death 0/56 带 text | — | — |
| **VIS-06** | 战将变身对白（B120-E03c） | **B** | `Battle/BattleDriver.cs:35422-35437` · `Battle/ChatPopupPanel.cs:1455-1478` | 上游数据 `transformedWarlordId` **1351 张 SO 全为 0**（本次亲核，见 §二·3） | 方法体 + 夹具齐；**零生产调用者** | 字段存在但全空（原版休眠） | 若将来 SO 填值，链路即通 | P2 |
| **VIS-07** | 结算战报三格（Troops / Tactics / DamageDone） | **B / D** | `Battle/EndPanel.cs:640-660` | **宿主 prefab 本地无实例**（`BattleEndPlayerData`） | 我方副标题是自加；三格未做 | **有**：`BattleEndPlayerData__Setup.c` 方法体齐（拼法 + `": "` + 顺序） | 复核：AssetRipper 导出工程 1389 prefab / 17 场景对脚本 guid `8e6048…` **0 引用**；`decomp_full` 里 `Setup` 外部调用点 0 | P2 |
| **VIS-08** | 战役脚本（`CampaignScriptRunner`） | **B** | `Battle/CampaignScriptRunner.cs:1-45` | `WarlordPathData` 四张表 ⊂ `TimedCampaignObject`（服务端 LiveOps 下发） | 执行器 + 数据结构齐；`NullCampaignScriptHost` 出声兜底 | 原版数据在远端，本地不存在（已按 3 个词二进制搜 `D:/2/关服前存档`，0 命中） | 需远端；离线不可得 | P2 |
| VIS-09 | 战斗 13 个 cue 字段 | B | `Resources/battle_sounds.json` 的 `gaps[]` | 原版那一格是空 PPtr / 悬挂 GUID（含 `changeToActiveAbilityCUE`） | 我们不响 | 原版也不响（`CardScript__ChangeAttackType.c:53` 调用点在、数据空） | — | — |
| VIS-10 | 7 条 trait 特效 | B | `Resources/trait_effect_bindings.json` 的 `unresolved` 7 | `TraitEffect.effectAnim` 空引用（原版如此） | 不播特效 | 原版空（`combo` / `daemonicAllegiance` / `firstStrike` / `large` / `mekaniak` / `piercing` / `resolution`） | — | — |
| VIS-11 | 单位聊天引用字段 | B | `Battle/UnitChatPanel.cs:1644-1645` | `chatRef` / `introRef` **0/1351 非空**（本次亲核）；开场白真源 = `customIntroChatData`（57 张 → `card_chat_intro.json`） | 现读恒等于卡 id | 原版两字段也全空 | — | — |
| **VIS-12** | 卡体动画「一帧裕量」 | **C 🔶** | `Battle/Card3DAnimationController.cs:395-418` | 无数据缺；**判据冲突待裁定** | `fps<=0 ⇒ 60`（裕量 1/60 s） | 原版 = `Application.targetFrameRate`（出厂 `FPSLimit 2 ⇒ -1`）⇒ `t < 1` 即收尾 | 反证：`cardanim_clips.json` 1067 条里 881 条（82.6%）≤1.0 s ⇒ 照抄会让它们当帧收尾 | **P1（需调度台裁）** |
| VIS-13 | 活动级结算两跳的事件实例 | C 🔶 | `Battle/BattleDriver.cs:39740-39775` | 远端 LiveOps 事件实例 | 取不到 ⇒ 出声 + `CombatFinished` 早退 | 服务器（关服） | 🔶 调度台 2026-10-31 裁定 | P3 |
| VIS-14 | 语音总开关 | C 🔶 | `Battle/BattleDriver.cs:35410-35418` | `UnityRemoteConfigData`（本地无该资产） | 离线等价 `true` | 远端 RemoteConfig | 🔶 调度台 2026-10-27 裁定 | P3 |
| VIS-15 | `misc_clips.json` 11 条 legacy clip | 反向缺口 | `Resources/misc_clips.json` | **数据在、运行期无消费者** | 不播 | 原版多为孤儿（全库 0 处 pathID 引用） | 需要时接 | P2 |
| VIS-16 | 等待横幅的 z | C 🔶 | B120_Q10 | 原版无 per-node 深度字段 | `Z=-0.45` 是**我们的映射值**，应标 🔶 | 无判据 | 标 `🔶 自己设计` 即可 | P2 |

---

## 二、重点条目详情

### 1 · VIS-04 音效 cue 覆盖率的**现读更正**（铁律 5）
- **旧数（过期）**：`B120_F_音效语音BGM.md:27/103` 写「610 资产 / 表内 508 / ≈96 悬空」；`B120_W5_执行.md:144` 写「508/610」。
- **现读（本次算）**：`animfx_sounds.json` 513 条（其中 **508** 个名字对得上资产）＋ `battle_sounds.json` 27 条（**24** 个 `srcCue` 全是资产名）＋ `remnant_sounds.json` 6 条 ⇒ **覆盖 537 / 610，缺 73**。
  旧数漏算 `battle_sounds` 那 24 条 ⇒ 「≈96」偏大。
- **缺的 73 条里有 5 条在代码里出现过**，但**没有一条是当 cue 用**：`Add card to deck`（`RewardWindow.SoundClipOnAppear`，走 `WFSoundBank.Clip(clip名)` 直取，文件在）、`OvertimeStart`（`BattleDriver.cs:30705` 同样直取，文件在）、`Victory`/`Defeat`（`Loc.cs` 词条键）、`Blackout`（`WindowsManager.cs:1875` 的 GameObject 名）。
- ⚠️ 顺带核实：三张表的 clip 引用 **643 条 / 0 条缺文件**；`voice_lines.json` **1897 行 / 0 条缺文件**；13 场 BGM 的 `tracks[].clip` **全部有文件**。

### 2 · VIS-04 后半：73 条里的 `Trait *` 一族（A 候选，未定）
`Trait Blast` · `Trait Camouflage` · `Trait Concussive` · `Trait Front Line` · `Trait Rally` · `Trait Relentless` · `Trait Resolution` · `Trait Stealth` · `Trait Tide` 共 9 条；
其中 **7 条**对应的 trait 在 `trait_effect_bindings.json` 里**有 prefab**（`camouflage` / `concussive` / `rally` / `relentless` / `resolution` / `stealth` / `tide`）。
⇒ 「有视觉特效、没有音效表项」的嫌疑最大。**但**：`CardTrait` 上**没有**音效字段（`dump.cs:21327-21356` 全字段已读），`TraitEffect` ctor 也没有 ⇒ 这些 cue 的持有者另有其人。
**下一步（谁会做）**：对 `StealthEffect` / `RallyEffect` / `TideEffect`（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`）逐颗读组件、按 `m_Script` PathID 反查 `AudioCue` 字段；命中才补表（⛔ 别凭名字补）。

### 3 · VIS-06 的一处**措辞订正**
`B120-W30` 写「两列数据（`transformedWarlordId`/`deckCriteria`）在 1,351 张 SO 里**全空**」。
本次亲核 `cards_raw.json`：`transformedWarlordId` **0/1351 非空**（对）；但 `deckCriteria` **1351/1351 都有结构体**（`triggerIf: 1` + 全 0 计数），**不是空**。
结论不变（没有新督军 id ⇒ 变身永不发生），但写「全空」会误导下个会话去「补 deckCriteria」——**要改的是 id，不是 criteria**。

### 4 · VIS-01 的准确说法
不是「没数据」：`offensive_env_cards.json` 39 张 / 22 列 / 41 行 logic / 41 个 `enchantmentId` 命中 37 个模板（现读）。
而是「**数据齐、消费端缺一格**」：`CardDatabase.cs:4094-4095` 自己写着「模板已在 `enchantment_templates.json`，缺的只是**谁来挂它**」。
⇒ 这一条应当按「有代码、有数据、缺映射」记，而不是「缺数据」——补的时候别去重生成表。

---

## 三、已核实【不是缺口】的疑似项（免得下个会话重复查）

| 疑似 | 现读结论 |
|---|---|
| 结算评分行的 `sealIconSprite = Rank Skull_0` 本地没有（`EndPanel.cs:1189` / `:1315` 注释） | **已解决**：`Resources/Art/ui_menu/Rank_Skull_0.png` 已在（`B120_W9` 导入），`EndPanel.cs:1419` 已走 `CardArt.MenuUi("Rank_Skull_0")` ⇒ **只是注释过期** |
| `AttackSelector.ShowJustOne` 调用方没传 `army` ⇒ 技能钮占位图（`AttackSelector.cs:1225`） | **已解决**：`BattleDriver.cs:15362` 已传 `ArmyIcons.Id(u.Card.Faction)` ⇒ **注释过期** |
| `card_icon_plan.json` 只有 876 张（池 1129） | **不是缺**：只收含方括号 token 的卡；876 之外的 253 张 desc 里 `[` 出现 **0** 次 |
| `RetaliationHoldOf` 441 组回落到 0.75 | **已解决**：改走 `CardAnimClips.RangedChainDuration`（`EventTiming.cs:139-147`） |
| `card_vfx_by_card` 的 `cardsUncovered` | **0**（1129/1129 覆盖） |
| 音效表命名的 `timeToPlayAgain` 缺格 | 三张表**逐条都带**（现读 missing = 0），`WFSoundCue` 默认值已改 0.1 |

---

## 四、判定与还差什么

- **A（真缺口，要做）**：VIS-01（P0）· VIS-02 · VIS-03 · VIS-04。
- **B（原版也休眠，记录即可）**：VIS-05 · VIS-06 · VIS-07 · VIS-08 · VIS-09 · VIS-10 · VIS-11。
- **C（我们自设计，需裁定或补标）**：VIS-12（**要裁**，影响 82.6% 卡体动画）· VIS-13 · VIS-14 · VIS-16 · VIS-15（反向缺口）。
- **D（判据查不到，已写清搜过什么）**：VIS-04 后半（73 条 cue 的调用方）· VIS-07（宿主 prefab）。

**查过但没找到的目录/关键词（供复核）**：
`d:/2/新解包资源/assets_full`（全树 `troopsText` / `WarlordPath1` / `preMulliganScriptedActions` / `TimedCampaign`）·
`d:/2/tools/assetripper_out/ExportedProject`（`BattleEndPlayerData` 脚本 guid 反查 prefab/scene）·
`d:/2/tools/decomp_full`（`BattleEndPlayerData__Setup.c` 外部调用点）·
`D:/2/关服前存档/安卓版本warpforge，包含远端资料/`（战役脚本三词二进制搜）·
`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（`transformedWarlordId` / `chatRef` / `introRef` / `normalAbilityBoardCriteria`）。

---

## 五、主代理 `B121-DATA` 回源订正（2026-11-03，铁律 5）

1. **VIS-02 的「`DeployHoldOf` 恒 1.0 s」不成立**：现读 `CardPresentation/Core/EventTiming.cs:959-969`，
   `DeploySource.Summon` ⇒ `DeploySummonHold` = **0.45**（`:935`，判据是原版那条协程真寿命），不是 1.0。
   「逐卡 summon clip 时长」这一层是否还缺，需要单独核（主清单把它降为 P2 / 待复核）。
2. **VIS-03 不是缺口**：`normalAbilityBoardCriteria` 与 `hasAlternativeAbility` 是**稀疏列**（只落非默认值）。
   现读池内 `hasAlternativeAbility=1` 只有 `SW21` 一张、`normalAbilityBoardCriteria` 非默认 2 张 —— 与 SO 真值完全一致，
   缺的那 1,128 张是「默认 0」，不是「没落列」。
3. **VIS-01 的结论仍成立，但原因要换**：不是「缺 DTO → ability 树的映射器」——
   `CardDatabase.cs:3729`（`B115_S5`，2026-11-01）已把模板 `abilities` 灌进 `CardDef`，`RunEnchantmentLogicRows` 也已存在。
   真实断点是**执行器只覆盖 `ca 30/40/50/340`**（`EnchantmentExecutor_B115.cs:77-109`）：37 个环境模板 / 71 行 logic 里
   **44 行 `ca` 没有执行器**（`ca 0` 10、`10` 20、`20` 2、`60` 6、`130` 4、`236` 2）。补的时候**别重生成表**，补执行器分支。
4. **VIS-06 的措辞订正（已在主清单采纳）**：`transformedWarlordId` 0/1351 非空（对）；`deckCriteria` 1351/1351
   都有结构体、只是计数全 0 ⇒ 要补的是 **id**，不是 criteria。
5. **VIS-07 补一条检索边界**：`BattleEndPlayerData` 的宿主 prefab 在 `assets_full` 与 AssetRipper 导出工程双向 0 引用，
   这是本轮**确认过的「本地不可见」**（不是「没找」）；离线无法继续。
