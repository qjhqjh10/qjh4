# B121-CMT · 战斗代码「功能级注释」现状与卡牌/关键词代码索引普查

> 2026-11-03 · 只读普查（未改任何 `.cs` / 未跑 Unity / 未动 git / 未写活状态表与账）
> 派了 3 个只读子代理（一层扇出）：**A 引擎块**（`RuleEngine/{Core,Data}`）· **B 战斗界面块**（`CardPresentation/Battle`）· **C 表现/输入/联机块**（`Hand`/`Core`/`Board`/`Effects`/`Net`/回放/音效）。
> 子报告：`报告/B121_CMT_子报告_引擎.md` · `_战斗界面.md` · `_表现与联机.md`。主报告只做汇总与裁定。

---

## 〇、一句话结论

**注释不是「缺」，是「缺一个维度」。** 战斗域的注释密度已经 **50.7%**（221 个 `.cs` / 231,469 行 / 117,331 注释行），文件头几乎 100% 有「我是什么 + 原版判据在哪」；但 **19,079 个注释块里只有 3,614 个（18.9%）能看出「这一段实现的是哪个游戏功能 / 哪个关键词 / 哪个阵营机制」** —— 其余写的是**出处、踩坑、日期与账号**。用户举的「这段是修女阵营祈祷功能的实现」，**在关键词定义处答得出、在实现处答不出**：`CardDef.cs:7661` 写着「**祈祷**（修女会）」，而真正实现它的 `RuleCore.cs:12275` / `Enchantment.cs:755` **一个「修女会」都没有**。

**索引侧**：功能点索引能答「**功能** → 代码」（180 条，方法级 `Class.Method:行`），卡牌总索引能答「**卡** → 数据/特效/动画/语音 + 通用管线入口」（1,129 张），但**都不能答「某张卡 / 某个关键词的**实现**在哪个方法**」—— 卡牌总索引的 `code_entry` 列 **1,129 张卡只有 1 个不同取值**（全是同一条通用管线），per-card 特判只覆盖 **36 张（3.2%）**。

**结论**：本波该做的是「**给已有注释补一句功能标题**」+「**补一张关键词/效果词 → 结算方法的索引**」，**不是重写注释**。两者都能做到**零行数变化**（见 §四、§五）。

---

## 一、现状量化

### 1·1 扫描范围

| 目录 | 文件 | 行数 | 注释行 | 密度 |
|---|---|---|---|---|
| `RuleEngine/Core` | 94 | 105,073 | 58,709 | 55.9% |
| `RuleEngine/Data` | 24 | 14,873 | 5,845 | 39.3% |
| `CardPresentation/Battle` | 79 | 95,899 | 45,828 | 47.8% |
| `CardPresentation/Hand` | 2 | 3,649 | 1,980 | 54.3% |
| `CardPresentation/Board` | 3 | 1,403 | 823 | 58.7% |
| `CardPresentation/Effects` | 8 | 1,893 | 568 | 30.0% |
| `CardPresentation/Net` | 11 | 8,679 | 3,578 | 41.2% |
| **战斗核心合计** | **221** | **231,469** | **117,331** | **50.7%** |
| （参考）`CardPresentation/Core` 全场 | 78 | 55,035 | 26,888 | 48.9% |
| （参考）`CardPresentation/Shell` | 286 | 194,518 | 84,308 | 43.3% |

> 口径：行首 `//` 或 `*` 记注释行（不含 `[Tooltip("…")]` 里的判据 —— 实测 `HandLayout.cs:156-209` / `BoardLayout.cs` 的判据在特性里，按 `//` 数会**低估**）。

### 1·2 「功能级注释块」计数（本报告的核心量化）

判据：一个**连续注释块**里出现任意一个「关键词中文名 / 英文关键词常量 / 阵营名 / 机制名」（伏击·嗜血·祈祷·职责·狂暴·议程·誓约·虫群·残骸·路标石·灵魂石·信仰·修女·灵族·兽人·光环·主动技能·召唤·变身·部署·换牌·回放·触发·关键词 + Ambush/BloodThirst/Pray/Duty/Ferocity/Agenda/Oath/Swarm/Remnant/Waystone/SpiritStone/Sororitas/…）。

| 目录 | 注释块 | 含功能/关键词/阵营名 | 占比 |
|---|---|---|---|
| `RuleEngine/Core` | 7,522 | 2,260 | **30.0%** |
| `RuleEngine/Data` | 1,098 | 121 | 11.0% |
| `CardPresentation/Battle` | 9,075 | 1,119 | 12.3% |
| `CardPresentation/Hand` | 311 | 30 | 9.6% |
| `CardPresentation/Board` | 135 | 12 | 8.9% |
| `CardPresentation/Effects` | 171 | 4 | 2.3% |
| `CardPresentation/Net` | 767 | 68 | 8.9% |
| **合计** | **19,079** | **3,614** | **18.9%** |

按行看：**117,331 注释行里只有 7,685 行（6.5%）** 提到过上述任一词。
⇒ **「注释多」与「看得出这是什么功能」是两件事**，这正是用户感觉「没有相关注释」的原因。

### 1·3 正例（好注释长什么样，5 个）

| # | 位置 | 好在哪里 |
|---|---|---|
| 1 | `RuleEngine/Core/RuleCore.cs:12520` | `/// **这一格是不是一颗「可以收集的路标石」**（灵族的灵魂石）。**只判不执行** —— 表现层拿它决定…` —— **就是用户要的形状**：功能是什么 + 谁在用 |
| 2 | `RuleEngine/Core/CardDef.cs:7661` | `/// <summary>**祈祷**（修女会）：`缓慢非战斗行动`（规则书 `:198`）。` —— **功能名 + 阵营 + 出处**，一条 `///` 说全 |
| 3 | `RuleEngine/Core/CardDef.cs:6955-6971`（SummonRift） | 语义 + 原版 trait id + 落点 + **可达性**（「全池今天 0 张卡带它 ⇒ 零行为变化」）四样齐 |
| 4 | `CardPresentation/Battle/BattleDriver.cs:41693` `ChoicePanelPool` 类级 doc（约 60 行） | 先说**解决什么** → 三条闸 → 逐族核对 → 「已知残余（只报不改）」，是全块样板 |
| 5 | `CardPresentation/Hand/HandLayout.cs:10-14` | 文件头一次说死「这是谁的移植 + 权威实例在哪」：`原版 CardsHorizontalLayout` + `MB_5271.json` 路径 |

补充正例：`RuleEngine/Core/CounterAttack.cs:1-19`（文件头当功能地图）· `Core/Enchantment.cs:682-690`（⑥ 祈祷附魔段）· `Core/RuleCore.cs:11995-12003`（信仰 vs 誓约「不是同一档」）· `BattleDriver.cs:150-186`（三阵营资源，带机器码级判据）· `BattleDriver.cs:201-208`（联机块头）。

### 1·4 反例（最该补「一句话功能标题」的，12 个）

| # | 文件:行 | 这段其实是 | 为什么算反例 |
|---|---|---|---|
| 1 | `CardPresentation/Battle/BattleDriver.cs:11770` `SetFactionResourceVisible` | **两组资源条的显隐闸：修女信仰 4 件 + 灵族灵魂石 6 件 ×（我·敌）**。⚠️ **2026-11-03 `B121-WE` 订正：原文「三阵营含任务点」不成立** —— 任务点走 `QuestPointsVisible:11696`（账里分开落：`战斗界面_HUD.tsv:541/543/550` vs `:542`） | **全块唯一一个「机制级却零注释、零文档」的函数**（11 行全裸） |
| 2 | `RuleEngine/Core/RuleCore.cs:12275` `keyword == Pray` 支 | **修女会「祈祷」的落地点**（置 `Prayed` + 挂 `Pray Enchantment` + 走唯一触发出口） | 内容很厚，但**没有一处写「修女会」** |
| 3 | `RuleEngine/Core/RuleCore.cs:12028` `CanUseFaithAbility` / `:12060` `UseFaithAbilityCore` | **修女会「信仰」能力**（`N ☀:` 阈值前缀、**不扣费**） | 同上：实现处无阵营名，只能去 `CardDef.cs:3783` 反查 |
| 4 | `RuleEngine/Core/Enchantment.cs:755` `AttachPray` | **修女会「祈祷附魔」**（原版隐藏卡 `Pray Enchantment`） | 段头写了机制，方法头没写「这是哪个阵营的」 |
| 5 | `RuleEngine/Core/EffectResolver.cs:3091` `DoDeal` | 卡面动词 **`Deal N damage`** 的伤害总入口 | 上方只有 banner「各个动词的结算」，**方法无 `///`** |
| 6 | `RuleEngine/Core/EffectResolver.cs:3722/:3748/:4332/:4373` `DoHeal`/`DoDraw`/`DoDestroy`/`DoStun` | **治疗 / 抽牌 / 直接置死 / 眩晕** 四个动词 | 同上，四个都无方法头 |
| 7 | `RuleEngine/Core/RuleCore.cs:9187` `Hurt` | **全引擎唯一的伤害入口**（死亡 / 广播 / 触发三拍） | 无 `///`、无首句 —— 新人找不到「伤害从哪进」 |
| 8 | `RuleEngine/Core/UnitState.cs:1104` + `CardDef.cs:6146` `KwValue` | **关键词数值的唯一读口**（`Armour 2` 的 2 从哪来） | 两处都零注释 |
| 9 | `CardPresentation/Battle/BattleDriver.cs:26597` `SyncBoard` | 把引擎棋盘同步成场上卡视图 | **349 行、0 个段标、无方法头** |
| 10 | `CardPresentation/Battle/BattleDriver.cs:29975` `BuildHudExtras` | HUD 非主路径那批件（头衔条/头像/联盟钮/任务点/能量累积灯/加时） | **603 行无方法头**；`BuildHud:28646`（1,329 行）段标题只有「HUD」两个字 |
| 11 | `CardPresentation/Battle/BattleDriver.cs:14925` `DrivePlayerTurn` / `:19043` `DriveAiTurn` / `:19516` `PlaySignals` / `:23631` `PlayAbilityBeat` / `:11236` `ShowAsk` | **玩家回合主循环 / AI 回合 / 引擎信号→演出时间线 / 技能演出拍 / 选牌弹窗** | 五条主链入口全无方法头（198–270 行不等，`DrivePlayerTurn` 还 0 段标） |
| 12 | `CardPresentation/Net/NetTransport.cs:742/:766/:775/:813` | **重连时「把新连接收作待替换候选」的整条生命周期**（写候选 → 取帧 → 提升 → 作废） | 四个公开方法零注释，是本块最集中的一簇「成族无说明」 |

> 补充：`BattleDriver.cs` 的段标题大多以「🆕 日期（执行代理 XX · 账 XXX）」开头，功能名被挤到括号后半句（例 `:7089-7092`）。⚠️ **2026-11-03 `B121-WE` 现读订正：`// ===` 开头块标题 335 个、紧跟「🆕 日期」标题的 106 个、`//  🆕 20` 注释行 132 条**（原文「112 个里 96 个」已过期）。这是「注释密度高、可读性低」的直接成因。

---

## 二、索引能力评估

### 2·1 现有三套索引各自答什么

| 索引 | 产物 | 规模（现读） | 粒度 | **能不能答用户的问** |
|---|---|---|---|---|
| **功能点索引** `工具/gen_feature_index.py` | `判据/功能点索引_<域>_part.tsv` × 20 份 | **180 条功能点**（战斗相关 **66**：逻辑驱动 28 · HUD 16 · 特效 10 · 卡面 5 · 联机 7） | **方法级** `Class.Method:行`（如 `FP-ENG-002 攻击与伤害结算 → RuleCore.cs:6269 DeclareAttack`） | ✅「**某个功能**的代码在哪」 · ❌「某张卡 / 某个关键词的实现在哪」（关键词只被笼统归进 **FP-ENG-001 关键词与词条**一条，落点是 `UnitState.ApplyTraitEffect` 等 6 处） |
| **卡牌总索引** `工具/gen_card_dossier.py` | `判据/卡牌总索引.tsv` | **1,129 张卡 × 25 列** | 卡级（身份/数值/关键词/desc/abilities/vfx/anim/voice/**code_entry/code_special**） | 🟠 **半答**：`code_entry` **1,129 张卡只有 1 个不同取值**（全是同一条通用管线 `CardDef.cs:48 → CardDef.AddTriggerOp:4994 → EffectText:1284 → EffectText.Parse:1463 → EffectResolver.EffectDispatch…`）⇒ 它答的是「**所有卡都走这条路**」；`code_special` 只有 **36 张（3.2%）**有真正的 per-card 字面量特判 |
| **词条权威表** `判据/CardTrait词条权威表.md` | 74 条 CardTrait | 词条级 | `词条 → 原版 SO 字段 + 我们工程里出现过的文件名` | 🟠 **半答**：只列**文件名**，不判语义、不给行号（文件自己写明「不判断语义是否一致」） |

### 2·2 缺口（按可补性排序）

1. 🔴 **没有「关键词 → 实现方法」的索引**（用户的「祈祷」正是这一格）。
   - 现状：`CardDef.cs` 的 `KeywordTable` **64 个常量、64/64 有 `///`，但只有 44/64 写了实现落点、18/64 写了阵营名**。
   - 唯一像索引的是源码里的表：`ActionPump.TriggerNameOfKeyword:1081`（关键词 → 原版 trigger id，`Pray→655 Duty→662 Ambush→670 Ferocity→725`…），但**没有任何文档指向它**。
2. 🔴 **没有「卡面动词 / buffType → 结算方法」的索引**。
   - 源码里已有现成表：`EffectResolver.cs:1061` `EffectDispatch`（**54 个 handler**，`deal→DoDeal` `heal→DoHeal` `jam→DoJam`…，注释写明「只此一处、`ImplementedEffectVerbs` 从这张表派生」）—— **它本身就是最好的索引，但没有导出成文档**。⚠️ **2026-11-03 `B121-WF` 订正：原文写 28，现读 54**（照 28 对账会漏 26 个动词）。
   - buffType（1 addStats / 2 changeCost / 3 addTrait / 7 skipCardDraw / 8 markOfChaos / 12 enchantment / 15 singleUse / 16 choice / 40 replaceAbility / 70 …）散在 `CardDef.cs` 各处 `///` 里，**没有一张总表**。
3. 🟠 **「卡 → 效果实现」只能答到「通用管线」**：卡牌总索引的 `abilities` 列已把效果树写成人话（`trigger UseSpiritStone(600) ⇒ buffOthers · buffType addStats(1) melee+1…`），**结合 `EffectDispatch` 就能反查到方法**，但两者没有被连起来（需要一次 join）。
4. 🟠 **功能点索引没有反向指针**：`FP-ENG-001` 不含 `Pray`/`Ambush` 等词，搜「祈祷」在 180 条功能点里 **0 命中**。

---

## 三、补注释清单（战斗主链优先级 · 全部零行数）

> 「插入方式」一律满足 **`b.count(b'\n')` 前后不变**。P0 = 用户点名/主链入口；P1 = 主链但已有部分注释；P2 = 边角。

| # | 文件 | 类/方法 | 建议注释原文（一句话功能） | 判据出处 | 插入方式（零行数） | 优先级 |
|---|---|---|---|---|---|---|
| 1 | `RuleEngine/Core/RuleCore.cs:12275` | `ApplyKeyword` 的 Pray 支 | 【功能】修女会「祈祷」：置 `Prayed` + 挂 `Pray Enchantment` + 走唯一触发出口 | `CardDef.cs:7661` · 原版 `trigger=655` | 就地改写紧邻的 `// 🆕 2026-10-25（X13）` 行，前置「【功能】修女会·祈祷 ——」 | P0 |
| 2 | `RuleEngine/Core/RuleCore.cs:12028` | `CanUseFaithAbility` | 【功能】修女会「信仰」能力使用闸（`N ☀:` 阈值、**不扣费**） | `CardDef.cs:3783` · `EffectText.cs:8496` | 改写 `///` 摘要**同行** | P0 |
| 3 | `RuleEngine/Core/RuleCore.cs:12060` | `UseFaithAbilityCore` | 【功能】修女会「信仰」能力结算 | 同上 · `EffectResolver.cs:791-922` | 其上方已有注释 → 就地改写 | P0 |
| 4 | `RuleEngine/Core/Enchantment.cs:755` | `AttachPray` | 【功能】修女会「祈祷附魔」—— 原版隐藏卡 `Pray Enchantment` | `Enchantment.cs:682-706` | 改写方法上方已有 `//` 首行 | P0 |
| 5 | `RuleEngine/Core/EffectResolver.cs:3091` | `DoDeal` | 【功能】卡面动词 `Deal N damage` 的伤害总入口 | 原版 `AbilityLogic__PlayAbility` | 并入 `:3088` banner 行（同块） | P0 |
| 6 | `EffectResolver.cs:3722/:3748/:4332/:4373` | `DoHeal`/`DoDraw`/`DoDestroy`/`DoStun` | 【功能】治疗 / 抽牌 / 直接置死 / 眩晕 | 同上 | 各自**方法体 `{` 行尾**追加 | P0 |
| 7 | `RuleEngine/Core/RuleCore.cs:9187` | `Hurt` | 【功能】全引擎唯一伤害入口（伤害→死亡→广播→触发三拍） | 原版 `BattleManager__ApplyDamage` | 方法体首行行尾追加 | P0 |
| 8 | `RuleEngine/Core/UnitState.cs:1104` + `CardDef.cs:6146` | `KwValue` ×2 | 【功能】关键词数值唯一读口（`Armour 2` 的 2） | 原版 `GetCurrentTraitValueWithModifiers` | 行尾追加（两处都已有行内注释，接在其后） | P0 |
| 9 | `CardPresentation/Battle/BattleDriver.cs:11770` | `SetFactionResourceVisible` | 【功能】两组资源条显隐闸（修女信仰 / 灵族灵魂石；⚠️ 2026-11-03 `B121-WE` 订正：不含任务点，任务点走 `QuestPointsVisible:11696`） | `BattleDriver.cs:150-186` | 方法体 `{` 行尾追加 | P0 |
| 10 | `BattleDriver.cs:26597` | `SyncBoard` | 【功能】引擎棋盘 → 场上卡视图同步（含插入推人后的视图回收） | `MinionManager` 两组 List | 方法体 `{` 行尾追加 | P0 |
| 11 | `BattleDriver.cs:29975` | `BuildHudExtras` | 【功能】HUD 非主路径件：头衔条 / 头像 / 联盟钮 / 任务点 / 能量累积灯 / 加时 | `menu_dump` / prefab | 方法体 `{` 行尾追加 | P0 |
| 12 | `BattleDriver.cs:32063` | `UpdateHud` | 【功能】每帧把 HUD 的字/灯/条刷成引擎真值 | 同上 | 方法体 `{` 行尾追加 | P0 |
| 13 | `BattleDriver.cs:14925` | `DrivePlayerTurn` | 【功能】玩家回合主循环（拖牌 / 点选 / 攻击 / 技能 / 结束回合） | `BattleManager` 回合协程 | 方法体 `{` 行尾追加 | P0 |
| 14 | `BattleDriver.cs:19043` | `DriveAiTurn` | 【功能】AI 回合驱动（`SimpleAI` 一步一延时） | `SimpleAI.PlayTurn` | 方法体 `{` 行尾追加 | P0 |
| 15 | `BattleDriver.cs:19516` | `PlaySignals` | 【功能】引擎事件流 → 演出时间线（阻塞窗 / 血账） | `BattleDriver` 信号泵 | 方法体 `{` 行尾追加 | P0 |
| 16 | `BattleDriver.cs:11236` | `ShowAsk` | 【功能】效果要求玩家选牌/选选项时弹面板 | 原版 `ChooseCardMenu` | 方法体 `{` 行尾追加 | P1 |
| 17 | `BattleDriver.cs:28646` | `BuildHud` | 把 `:28642-28644` 的段标题「HUD」改成「【功能】战斗 HUD 主链：场上卡 / 手牌 / 牌库 / 坟场 / 能量 / 计时 / 按钮」 | `FP-HUD-002` | 就地改写该段标题行 | P1 |
| 18 | `BattleDriver.cs:4924` | `Begin` | 【功能】开局主链：建档 → 摆视角 → 发牌 → 换牌入口 → 首个回合 | `BeginFromPendingCore:4090` | 在已有 XML doc **首行**补一句功能（同行） | P1 |
| 19 | `BattleDriver.cs` 段标题 ×96 | `// ==== 🆕 日期（代理·账）====` | 同物理行重排为「**功能名 —— 日期（代理·账）**」 | 各段自身 | **只改字、不改行**（批量可脚本化，但要逐行核） | P1 |
| 20 | `RuleEngine/Core/Aura.cs` | `Recompose` / `RefreshMeleeEqualsHealth` | 【功能】光环重算（先全清再加回 —— **幂等 ≠ 无副作用**） | `CLAUDE.md` 2026-10-25 E1 条 | 改写已有注释首行 | P1 |
| 21 | `RuleEngine/Core/ActionPump.cs:1081` | `TriggerNameOfKeyword` | 【功能】关键词 → 原版 trigger id 映射表（`Pray 655 / Duty 662 / Ambush 670 / Ferocity 725`） | 原版 `AbilityTrigger` 枚举 | 改写其上方已有 `///` 首行 | P1 |
| 22 | `RuleEngine/Core/EffectResolver.cs:1061` | `EffectDispatch` | 【功能】卡面动词 → 结算函数（**54** 个 handler，`ImplementedEffectVerbs` 由此派生；2026-11-03 `B121-WF` 订正原文 28） | 已在 doc 里 | 已有 → **不必改**，只需在报告/索引里指过来 | P1 |
| 23 | `RuleEngine/Core/RuleCore.cs:6269` | `DeclareAttack` | 【功能】攻击宣言（合法性 / 反击 / 飞行 / 远程） | `FP-ENG-002` | 改写已有注释首行 | P1 |
| 24 | `RuleEngine/Core/EffectResolver.cs:3697` 附近 | `DoDeploy` / `DoDeployOnce` / `DoBecome` | 【功能】免费部署 / 变身（`Stratagems … become …`） | `CreatePool.cs` · 原版 `AddStealMinion` | 方法体 `{` 行尾追加 | P1 |
| 25 | `CardPresentation/Battle/ReplayStore.cs:1-6` | 文件头 | 已有（「原版在服务器 ⇒ 用户拍板自己做，**这是加功能不是复刻**」） | — | **正例，不改** | — |
| 26 | `CardPresentation/Net/NetTransport.cs:742/766/775/813` | 候选连接替换四方法 | 【功能】重连「待替换候选」生命周期 ①写候选 ②取帧 ③提升 ④作废 | 正本 `联机P2P_设计与交接.md` | 各自行尾追加 `// 候选连接替换族（n/4）：…` | P1 |
| 27 | `RuleEngine/Core/CardDef.cs:6915` | `Vanguard` | 【功能】先导 = 前线单位（**唯一没有独立功能句的关键词**） | 规则书 / `RuleCore.IsValidTarget:5572` | 改写该常量已有 `///` 同行 | P1 |
| 28 | `RuleEngine/Core/CardDef.cs` `KeywordTable` 64 常量 | — | 把 **44/64 → 64/64** 补「实现落点」、**18/64 → 64/64** 补「阵营归属」 | 各词已查清（见子报告 §二） | 逐条**就地改写**既有 `///` 行（⛔ 不加行） | P1 |
| 29 | `CardPresentation/Core/CardView.cs:4251` | `Build` | 【功能】按层建整卡的视觉树（16k 行文件的核心） | `CardView.cs:1-20` 层结构图 | 改写 `:4245` 已有注释行 | P2 |
| 30 | `CardPresentation/Effects/*` | 8 个文件（密度 30%） | 文件头已有 → 只补方法头 | 原版 `UIEffectToggler*` | 方法体 `{` 行尾 | P2 |

> **不在本清单里但更急的**：`CardPresentation/Battle/ScenarioBlendables.cs`(38.3%) · `EnvironmentApplier.cs`(27.9%) · `BattleHudAnim.cs`(18.4%) 三个低密度文件**未逐方法判读**（不在点名 Top 15 内），下一波若要全量补注释需先补这一层判读。

---

## 四、统一的注释模板

### 4·1 文件头模板（`原版有对应`）

```csharp
// <文件名> — <一句话：这个文件负责哪个游戏功能>
//
// 对应原版：<原版类名>（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/<类>.cs`）
//   · <主方法> → `<类>__<方法>.c:行`（`d:/2/tools/decomp_full/`）
//   · 关键字段/常量：<资产路径 + 字段名>（解包 `d:/2/新解包资源/assets_full/…`）
//
// 本文件负责：① … ② … ③ …
// ⛔ 不负责：<最容易搞混的相邻职责，点名文件>
// 账/判据：`判据/<文档>.md` §N
```

### 4·2 文件头模板（`自己设计 / 无原版可对`）

```csharp
// <文件名> — <一句话功能>
// 🔶 自己设计（非原版）：<是什么> · <为什么必须自定：判据在哪 / 为什么拿不到> · <谁定的 + 日期>
//   （典型：服务器已关服 ⇒ 判据在远端；或原版无此功能 ⇒ 我们补的）
```

### 4·3 方法头模板（挂在已有注释行上，**不新增行**）

```csharp
/// 【功能】<这个方法是哪个游戏功能的哪一步>（<关键词/阵营/机制名>）
/// 判据：`<文件>.c:<行>` · <规则书 `:行`> / <资产字段>
/// ⛔ 别改成 <错误做法> —— <原因>（踩过的坑写在这）
```

一行版（空间紧、方法体首行）：

```csharp
{   // 【功能】<一句话>（<关键词/阵营>） · 判据 <文件:行>
```

### 4·4 「自己设计」标记（固定串，⛔ 别用别的写法）

```csharp
// 🔶 自己设计（非原版）：<是什么> · <为什么必须自己定> · <谁定的 + 日期>
```
> 现存 **7 处仍是占位符 `TODO(谁定的)`**（违反 CLAUDE.md §18 三样俱全）：`AchievementTally.cs:134` · `CardDef.cs:910/7365/7468/7597` · `ConditionEval.cs:302` · `StarterCards.cs:26` · `Hand/HandLayout.cs:299` · `Core/CardText.cs:76/82/84/809` · `Net/NetProtocol.cs:166` · `Net/NetSession.cs:24`。

---

## 五、插入位置规则（账不漂移的硬约束）

| 做法 | 什么时候用 | 风险 |
|---|---|---|
| **A. 就地改写已有注释行**（首选） | 方法/段上方**已有** `//` 或 `///` | 零风险，行数不变 |
| **B. 方法体 `{` 那一行行尾追加** | 完全裸的方法（如 `SetFactionResourceVisible`） | `{` 行不是声明行、不是账落点；⚠️ **必须写在第一个 `//` 之前**（否则被注释吞掉，CLAUDE.md 2026-10-30 那条） |
| **C. 追加到类尾** | 要写「整类索引表」时 | ⚠️ **先确认文件最末 `}` 关的是哪个类** —— `BattleDriver.cs` 有两个顶层类（`BattleDriver` 止于 `:41692`、`ChoicePanelPool` 起于 `:41693`） |
| **⛔ D. 方法签名上方新增 `///` 行** | **禁用** | 插一行 ⇒ 下游全部行号 +1 ⇒ `check_ledger_points` 成片漂移 |
| **⛔ E. 声明行（`class X` / `const string X`）行尾加注释** | **禁用** | 会让 A 档落点直接判**失败**（不是漂移）—— 名字不再是行尾 token |

**改完必跑**（主对话在同步点统一跑，代理不许跑 Unity）：
1. `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py`（漂移必须 0）
2. **行数守恒自检**：`io.open(p,'rb').read().count(b'\n')` 前后相同
3. `bash d:/4/Unity/工具/typecheck.sh`（秒级，挡编不过）

---

## 六、建议的后续动作（本波只查不改，供调度台派活）

1. **P0 批（10 处）**：§三 的 #1–#9 + #27 —— 一次性把「阵营机制 + 五个动词 + 伤害入口 + 关键词数值读口」补上。**一个写手独占 `RuleCore.cs` / `EffectResolver.cs` / `CardDef.cs`**（三份都是热点、非 partial）。
2. **P1 批（BattleDriver）**：§三 #10–#19 —— 由**一个写手独占 `BattleDriver.cs`**（41,741 行、非 partial、账引用 400+ 处），按「方法体 `{` 行尾」逐条补；段标题重排是纯文字替换，可脚本化但要逐行核。
3. **索引批（新增文档，不动 `.cs`）**：新建 `判据/关键词与效果词_代码落点索引.tsv`，由现成数据源 join：
   · 关键词 64 条 ← `CardDef.cs KeywordTable` 的 `///`（现读 44/64 有落点）
   · 动词 28 条 ← `EffectResolver.cs:1061 EffectDispatch`
   · 关键词↔trigger ← `ActionPump.cs:1081 TriggerNameOfKeyword`
   · 卡 → 效果树 ← `判据/卡牌总索引.tsv` 的 `abilities` 列
   ⇒ 这一张表能一次把「祈祷 → `RuleCore:12275` → `Enchantment.AttachPray:755` → trigger 655」串起来，正是用户要的「卡牌总索引告诉我代码在哪」。
4. **`gen_card_dossier.py` 加一列**：`code_entry` 现在是**全池同一个值**（1 个去重），建议改成「**该卡的每个 ability → 命中的 `EffectDispatch` handler 行号**」，这样「卡 → 代码」才真正成立。

---

## §七、顺手发现（本波查出、未改）

1. **`code_entry` 列是「假索引」**：1,129 张卡只有 1 个不同取值 —— 它答的是「所有卡走同一条管线」，读者容易误以为「这是这张卡的代码」。（`判据/卡牌总索引.tsv` 第 21 列）
2. **`TODO/FIXME` 在战斗界面块只有 1 处**（`BattleDriver.cs:34882`）⇒ **后续别按 TODO 找缺口**，未做的都写成散文注释了。
3. **7 处 `TODO(谁定的)` 占位符**（见 §四·4）—— 违反 CLAUDE.md §18 的「三样俱全」。
4. **`RuleCore.cs:16` 的「v1 只实现 5 个关键词」仍留在文件头**，紧接三行才写「已过期」—— 新读者扫前 20 行会先读到错口径。建议把订正句前置到同一行（零行数）。
5. **`CardDef.cs:8336` 别名表**有 `new[] { "remnant", "remnant" }`（规范名映射到自己），未判缺陷，仅记录。
6. **`Net/*` 的「原版引用率低」是特性不是缺陷**（原版走 Photon，我们自建 P2P）⇒ 做全仓「注释引用率」排名时要把 `Net/*` 单列，否则会被它拉低。
7. **只数 `//` 会低估参数判据**：`Hand/HandLayout.cs:156-209`、`Board/BoardLayout.cs` 把原版字段名/单位写进了 `[Tooltip("…")]` —— 以后注释度量要把特性字符串算进去。
8. **`CardPresentation/Battle/` 实际 79 个 `.cs`**（任务书写「30+」）—— 文件数被低估两倍多，行数对得上。
9. **未覆盖**：`ScenarioBlendables.cs`(38.3%) · `EnvironmentApplier.cs`(27.9%) · `BattleHudAnim.cs`(18.4%) 三个低密度文件只做了计数，未逐方法判读；`RuleEngine/Editor/*`（含 39,763 行的 `RuleEngineTest.cs`）不在本次范围。
