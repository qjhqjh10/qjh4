# B103_CLOSE · 第一百零三会话 · 旧任务核销核查（只读代理）

> 派活：调度台（第一百零三会话收尾 · 交接 §三「任务 3：旧任务核销」）。
> 本代理**只读**：没有改任何 `.cs` / 账 / 正本 / `d:/2`；没跑 Unity / typecheck / git。
> 唯一写盘 = 本文件。
> 改动清单来源 = `资料/完全复刻/调度台日志.md` **§31·7**（B 清零 11 条 + 本会话修掉的 10 类真缺陷）＋ `报告/B103_*.md` 逐份现读。

---

## 〇、方法与被核对象

| 步 | 做了什么 |
|---|---|
| 1 | 读 `调度台日志.md` §31·7（`:11088-11113`）取「本会话改了什么」的权威清单 |
| 2 | 对 `资料/任务线/*/任务总表.md` 的 **§一** 用 `awk` 切片后按关键词 grep（类名 / 方法名 / 功能词共 4 批），不回读整份（战斗线 §一 单份 **115 KB**） |
| 3 | 命中项**逐条回源现读代码**（`RuleEngine/Core/*` · `CardPresentation/**`）＋ 本会话报告，判 ✅ / ⚠️ / ❌ |
| 4 | 另扫 `资料/完全复刻/待接线与排队项.md` 与 `判据/B103_本会话待办与裁定.md`（调度台自己的待办正本） |

### 🔴 两条先例的现核（⛔ 不算新发现，只作确认 + 定位订正）

| 先例 | 现核结论 | 证据 |
|---|---|---|
| `ca 85` 无变身机制（未建）」已过期 | **确认过期**。**但位置订正**：那句话在 **`判据/B91_P2_chosenAbility逐码判据.tsv:21`**（`.md` 里没有这一行；`.md:45` 那行是 `ca 90`，早已标「已实现」） | 闸 `RuleEngine/Core/RuleCore.cs:15479` / `:17364`（`… \|\| ca == 85`）· 分派 `Core/EffectResolver.cs:18530`（`if (ca == 85) return RunTransformUnitRow(...)`）· 执行体 `Core/TransformUnit_B94B12.cs`（`BattleManager.AddTransformCardWithBuff` 全链）。`报告/B103_W5.md` §⑤-1 已记 |
| `Shell/DeckRuntime.cs:2751`「`CollectionFilterController<T>.Toggle` 无产物」已过期 | **确认已订正**（2026-10-30 `B103_R2` 现核，就地写在注释里）。**但路径订正**：真身 = **`Assets/CardPresentation/Deck/DeckRuntime.cs:2751`**（不是 `Shell/`；全仓只有一个 `DeckRuntime.cs`） | 现读该行：「🔴 2026-10-30 订正（铁律 5，`B103_R2` 现核）：方法体读得到 —— `decomp_generic_trial/out/CollectionDisplay_object___ToggleFilters__15eec60.c` + `out_cpp2il_dev_ilspy/CollectionFilterController.cs:159-207`」 |

---

## 一、✅ 可核销（11 条）

### A. 任务线 §一（2 条）

| # | 台账位置 | 原文摘要 | 判定 | 证据（现读） |
|---|---|---|---|---|
| A1 | `任务线/战斗线/任务总表.md:315` | 🆕 **`GetCardEffectsToShow` 的「项源」不统一**（`✗` 要做 · 被三个前提挡着） | ✅ **已解决** | ① 前提①「四个文件空出来」已满足（W3/W5 本轮写过 `EffectResolver`/`Enchantment`/`CardDef` 侧）；② 现读已是**一条只读投影**：`RuleEngine/Core/EffectsToShow.cs:201 Of()`（7 源）→ 消费 `Battle/CardDisplayWindow.cs:2222 RowsOfEffects` → `BattleDriver.cs:13212`；③ **序**已按原版 `+0x108` 的 `List.Add` 序落地（`CardInstance.cs:536 NextEffectSeq()` 唯一发号口 · `UnitState.cs:3700` 转发 · `EffectsToShow.cs:238/292/354/378/413/427` 六处读点 · `EffectResolver.cs:11697/11960/12014/12188/12191/12202/12310`）；④ `RemoveTrait` 无序 HashSet 已改有序簿记（`UnitState.cs:4257-4282`）。账 `特效动画:521` 已 B→A（§31·7）。判据 → `报告/B103_W3.md` §①②，`报告/B101_R7_效果行消费端.md`。⚠️ 唯一残余 = **S7 `ruleprobe effects` 探针**（见 §三-1，归调度台，不是这条任务的内容） |
| A2 | `任务线/界面线/任务总表.md:281` | 🆕 **Deck 编辑窗「层与订阅一起晚」**（`✗` 要做 · 修法要改建期挂载） | ✅ **可核销**（🔴 **原前提也需订正**） | 现读：① **订阅在开窗那一拍无条件挂** —— `Deck/DeckRuntime.cs:9625-9631 WildcardDisplayEnable()` → `HookWildcardUpdate()`；② **库存信号到了就播扫光**（且顺手补了四格数字）—— `:9136-9141 OnWildcardsUpdated()`（`WildcardShineAfterTab(2)` + `RefreshHeader()`，`B103_R1`/AUD03 #8 复核）；③ 🔴 **「开窗就停在 Cards 页时原版会重播」这句前提被推翻**：`DeckRuntime.cs:9602-9618` 逐条判据（`WildcardDisplay__OnEnable.c`：Register 无条件、`Play` 受 `currentArmy(+0x50)!=0` 闸；窗每次新实例 ⇒ 开窗那拍闸永远关着）+ `:9630` 注释「由此**没有**『开窗那一拍重播』这一支」⇒ **原版开窗本来就不播**。⚠️ 该行索引里的 `Shell/DeckRuntime.cs` 路径是错的（真身 `Deck/DeckRuntime.cs`） |

### B. `待接线与排队项.md`（1 条）

| # | 位置 | 原文摘要 | 判定 | 证据 |
|---|---|---|---|---|
| B1 | `待接线与排队项.md:55`（🟠 P1「`GetCardEffectsToShow` 项源统一」） | 「真残差只剩 5 条：S3-c / S3-d / S3-f / `RemoveTrait` 无序 / S7」 | ✅ **4/5 已清**（只剩 S7） | S3-c `BattleContext.cs:827/837 NextRowGroup()` + `EffectResolver.cs:11697…12310` 透传；S3-d `Enchantment.cs:119/283`；S3-f `CardInstance.cs:211` + `EffectResolver.cs:5659/5871`；RemoveTrait 有序簿记 `UnitState.cs:869/1151/2629/4257-4282`。全部逐条在 `报告/B103_W3.md` §① 表 + §② 落点表（含 `TMPDIR=… typecheck` 0/0 与账闸 332/332、1122/1122） |

### C. 调度台待办正本里已闭环的（7 条 —— 供 §31·7 / 交接勾销）

> 这些不是「任务线 §一」，是 `判据/B103_本会话待办与裁定.md` 与 `交接_第一百零三会话.md` 里挂着的活；本轮已做完，下一会话别重派。

| # | 位置 | 活 | 判定 | 证据 |
|---|---|---|---|---|
| C1 | `判据/B103_本会话待办与裁定.md` §二 排队「AUD05-A/B/D 三份补丁」 | 收藏窗 Cards/Styles 页 + 卡背页接**小屏 4 列**档；`DeckRuntime:2751` 过期注释 | ✅ 已解决 | `Shell/CollectionWindow.cs:211`（Cards ×1.5 ⇒ 4 列）· `:2168`（Styles ×1.5 ⇒ 4 列）· `:960/974`（Deck ×1.25 ⇒ 5 列）· `:1793`（Cardback ×1.2 ⇒ 5 列）· `:220/6746 UiSetSmallScreenUIForTest`；格子内容跟随缩放 `MenuDraw.DeckCell`（`B103_F6` §1·1）+ 卡背光晕 `CosmoSdfRect`（`:1926` / `:6750`）；2751 见 §〇 |
| C2 | 同上 §二 新发现的账「同族的另 4 个包要按新尺子重新定级」 | `wf_menus_extra 29` · `wf_prefabs_extra 31` · `wf_battleshared_extra 13` · `wf_mainmenu_extra 58` | ✅ 已解决 | `报告/B103_W7b.md` §一：**29/31/13/58 → 0**（pid 级静态；材质→shader 悬空 6/7/0/3 → 0/0/0/0）；`wf_prefabs_extra` 容器登记根 = `报告/B103_W7c.md` |
| C3 | 同上「`GetCardEffectsToShow` 的 S3-c 生产端」 | `BattleContext.NextRowGroup` + `EffectResolver` 5 处 | ✅ 已解决 | `报告/B103_W3.md` §4·1 + `BattleContext.cs:827/837` |
| C4 | 同上「计数器搬家（`CardInstance` 一份）」 | `S3-d`/`S3-f` 前置 | ✅ 已解决 | `Core/CardInstance.cs:536 NextEffectSeq()`（🔶 已打自设计标）；`UnitState.cs:3700` 转发；`grep EffectSeqOf` = 0（B103_W3 §①） |
| C5 | 同上「`DisplayEnemyCardInCenter` 的阻塞建模（`CardFeel` → `EventTiming` → `BattleDriver`）」 | 建 1.078 s 阻塞 | ✅ 已解决 | `Core/CardFeel.cs:4240-4246 CardCenterBlockingSeconds(0.3f)` = **1.078**（⛔ 别写 `return 1.078f`）· `Core/EventTiming.cs:501` 把 `EvtKind.SabotageCreated` 搬进 `TimedKinds` · `Battle/CardDisplayWindow.cs` + `BattleDriver.cs:9916/10003/10139`（三段等待 0.1+0.198+0.78） |
| C6 | 同上 §二 调度台自己做「`B98K2_Assert.cs` D1/D2 翻面」 | 断言钉死的是「掉血**之后**判 FTUE 无敌」的错口径 | ✅ 已解决 | §31·7 第 2 条：`RuleCore.cs:9010` 已改「掉血前」，D1/D2 重排成「判别档 + 正例」 |
| C7 | 同上 §二 排队「AUD07 自检宿主 108 处」 | 本地化宽化 | ✅ 已解决 | §31·7 第 9 条：宽化 26 运行时 + **103 宿主** + 5 镜像；未覆盖部分仅 AUD03 `#94` 的 4 窗（见 §三-6） |

---

## 二、⚠️ 已过期需订正（9 条，⛔ 条条有现读反证）

| # | 位置 | 原文（过期） | 现读事实 | 证据 |
|---|---|---|---|---|
| D1 | `判据/B91_P2_chosenAbility逐码判据.tsv:20`（ca **80**）与 `:21`（ca **85**） | 「无变身机制；（未建）」 | **两支都建了并已接线** | 闸 `RuleCore.cs:17364`（`ca == 80` / `ca == 85`）· 分派 `EffectResolver.cs:18533`（`if (ca == 80) return RunTransformSelfRow(...)`）/ `:18530`（`ca == 85`）· 执行体 `Core/RuleCorePartial_B103W5.cs:626-701`（`RunTransformSelfRow`）/ `Core/TransformUnit_B94B12.cs`（85） |
| D2 | 同 tsv `:31`（**160**）与 `:33`（**165**） | 「无 discard 动词；（未建）」 | **执行器 + 分派都在**，缺的只有计划表闸 | `EffectResolver.cs:18531`（`ca == 160` / `ca == 165`）；`报告/B103_W5.md` §⑤-2 已记 |
| D3 | `判据/B92_战斗判据_B9.tsv:20-21` | `[C-真没建] 无变身机制`（80/85） | 同 D1 | 同上 |
| D4 | `判据/B102_R4_C类大工程两条.md` 行 `:80`（80）`:81`（85）`:91`（160）`:92`（162）`:93`（165）`:117`（310）`:120`（330）`:123`（350） | 我们现状列 = 「无（未建）」 | **8 支全部已建**：160/165（`EffectResolver.cs:18531`）· 162/310（动作 56/57 + 执行体）· 330（`RunDrawUntilEqualRow`）· 350（`RunRemoveFromCemeteryRow`）· 80/85（见 D1） | 动作类型：`Core/ActionPump.cs:2461 ActionTypeDisappearFromHand = 56` / `:2464 = 57`（入队 `:2490/:2590`，出队 `:2467/:2598`）；执行体 `Core/RuleCorePartial_B103W5c.cs`（162/310）；闸/分派见 D1 |
| D5 | 同上 `:127`（380）`:146`（480）与 `:156` 批表（批0「只差接线」） | 「只差接线」 | **已接线** | `RuleCore.cs:17364` 含 `ca == 380` / `ca == 480`；`EffectResolver.cs:18533` 两个 `if`；`RunSetReferenceCardRow` `:20633` → `SetReferenceCards` `:1366` |
| D6 | `任务线/引擎线/任务总表.md:65`（E5-4 `referencedCards`） | 「缺三件：`cardAbilities[].abilityLogic` **没有 `ca` 列**、410 没有入队点、`AM54` 是战术卡」 | **① 已过期**：`cards_engine.json` **v45 / count 1129**，`activeAbilityLogic` 共 **133 行、133 行全带 `ca`**（现读 python 扫）；且「唯一写入点 = 380」的执行器与分派现已接（D5）。② 410 入队点 / `ta=200/201` 的运行时卡实例仍是缺口（见 §三-4） | python 现扫 `MyGame/Assets/RuleEngine/Resources/cards_engine.json`；`EffectResolver.cs:1366/20633` |
| D7 | `任务线/引擎线/任务总表.md:124`（D10） | 「`activeAbility` 执行链是**独立缺口**」（整条） | **已大幅推进，不再成立**：`B103_W5` §6·5 记「86 支已实现 **66**」；此后 W5b/W5c 与后续批又把 80/200/345/135/340/162/310/160/165 落进闸与分派（**23 个休眠码现读都在**）⇒ **未结约 6 支**（见 §三-3）。⛔ 别再按「整条缺口」派活 | `RuleCore.cs:17364` 与 `EffectResolver.cs:18530-18533` 现读含 `85/160/165/220/280/290/330/375/135/340/200/345/80/162/310/350/360/370/380/390/400/480`；`报告/B103_W5.md` §1·2 / §6·5 |
| D8 | `任务线/特效线/任务总表.md:27` | 「⛔ 仍未做：A2 剩余导出（… **卡包外观 17（商店挂起）** …）」 | **卡包外观 17 早在 2026-10-23 就已达成**（17/17 根名命中、容器 38 条，在 `wf_boosters_extra.bundle`） | `资料/普查产出_1023b/特效线_A2_执行报告.md` §〇 第 3、5 行（「17 卡包外观 **已在** `wf_boosters_extra.bundle`」/「导出备用已达成」）。⚠️ 本行其余（`Tutorial Pointer Menu` / `Card Display Open` / 战场线）仍未做 |
| D9 | `待接线与排队项.md:132`（与 `:171`） | 「投影 7 源 + `Seq` 在 `UnitState.cs`、消费在 `CardDisplayWindow.cs` **均未接** ⇒ 规则核无生产消费者」 | **过期**（同文件 `:55` 已由 `B101_R7` 就地订正为「全线接通」）；这两处是 2026-10-28 的旧行，未跟着改 | `EffectsToShow.cs:201 Of()` → `CardDisplayWindow.cs:2222/1295/2568` → `BattleDriver.cs:13212`（现读）；见 B1 |

### 另两条「待办本身该撤销 / 行号已漂」（同属 ⚠️，但不算台账原文错）

| # | 位置 | 事 | 结论 |
|---|---|---|---|
| D10 | `判据/B103_本会话待办与裁定.md` §二 排队「**AUD05-C 视口宽收口**（`1589.8`/`1589.7`/`1589.78` → 原版 **1589.75**）」 | 该前提被现读**推翻** | `B103_F5` 判据：Cards 与 Styles 在 prefab 里是**同一个** `sizeDelta.x = −163.05`（视口同 1589.95），我方 1589.7/1589.79 的 0.09 差是**两次实测的舍入差**（`Shell/CollectionWindow.cs:2168` 就地订正注释）；`Editor/B103F5_Assert.cs:72-73` 已按 1589.7/1589.79 钉死 ⇒ **该待办应撤销**（继续「收口到 1589.75」会打红 3 条断言且无判据支撑） |
| D11 | `任务线/引擎线/任务总表.md:223`（`RuleCore.cs:3522-3524` 的 doc 过期） | **仍未做**，且**行号已漂** | 现读 `RuleCore.cs:3532-3534` 仍写着「光环那条路（`AddAuraKeyword` / `ClearAuraGrants`）**没有接**」；替换文本在 `报告/B9_W18AR_光环侧翼疲劳.md` §5·3，尚未落 |

---

## 三、❌ 确认仍未做（8 条）

| # | 位置 | 活 | 为什么仍开（现读） |
|---|---|---|---|
| E1 | `判据/B103_本会话待办与裁定.md` §二 排队「**S7 `ruleprobe effects` 探针**」 | 建 `工具/ruleprobe/EffectsProbe.cs` + `Program.cs` 加 `case "effects"` | `ls 工具/ruleprobe/` **无 `EffectsProbe.cs`**；`Program.cs` 里**无 `"effects"` 分支**（现读 grep 0 命中）。归调度台（`工具/ruleprobe/**` 独占） |
| E2 | `任务线/引擎线/任务总表.md:233` | `ChooseValueOptions` 的 26 档分派 | 本轮改动不涉及；无证据表明落过 |
| E3 | `任务线/引擎线/任务总表.md:124`（同上 D7 的**残量**） | ca 剩余支 | **闸/分派里没有**：`ca == 71` · `240` · `170` · `175` · `190` · `210`（`grep "ca == 71|240|170|175|190|210" RuleCore.cs EffectResolver.cs` = **0 命中**）。其中 170/175/190/210/240 的执行器与动词都在，缺 `EffectText` 生产者 + `handmove` 动词表放开（`CardDef.cs:3518` 自承「要 `play check` A/B 才敢加」）；71 判「大概率已等价」但未夹具核（`报告/B103_W5.md` §6·4 / §7·1） |
| E4 | `任务线/引擎线/任务总表.md:117`（C3②） | `ta=200/201` 需要**运行时卡实例 `(CardDef, owner, zone)`** 的小重构 | 380 写入点已接（D5），但 200/201 的「手牌卡进引用表」结构未做；`UnitState.ReferencedCards` 只装 `UnitState`（`EffectResolver.cs:1496` 如实标注） |
| E5 | `判据/B103_本会话待办与裁定.md` §三 AUD03 `#94` 遗留 | 4 个窗（`RewardWindow` / `RewardsWindow` / `DailyRewardPopup` / `InboxWindow`）逐颗绑文本 | 报告 `B103_A03.md` 明写「逐颗绑定只做了 2 个窗（其余是剩余项）」；交接 §一「AUD03 16 条全做（`#94` 还剩 4 窗绑文本）」 |
| E6 | `任务线/卡牌线/任务总表.md:147` | `effectText` 链喂文本（38 行 / 31 值 / 31 卡） | 本轮无证据；仍在 §一（B101_K2 未动它） |
| E7 | `任务线/工具文档线/任务总表.md:53` | 批次 6（旧任务回核）的结论搬进各线 | **本报告只覆盖 B103 这一轮**；`判据/B9_旧任务回核_结论.tsv`（115 行）的其余部分仍未搬 |
| E8 | `任务线/自检断言线/任务总表.md:61` | `Editor/RewardsScene.cs:2546` 的 `StartsWith` 断言无鉴别力 | 本轮 `B103_F3` 修的是 RewardsScene 的 18 条红（`dr` 泄漏/销毁引用），**不是**这条断言口径；仍开 |

---

## 四、给调度台的操作建议（逐条：改哪一行成什么）

1. **可直接改写状态列**
   - `战斗线:315` → 状态 `✗ 要做` 改 `✅ 已核销 2026-10-30（B103_W3）`，并把「三个前提」那段改成「已满足；残余仅 S7 探针（归工具线）」。
   - `界面线:281` → 状态改 `✅ 已核销 2026-10-30（B101_K2/A03 #8 复核 + B99_W1/W5 判据）`，并**就地订正**「原版会重播」这句（原版开窗那拍永不播，判据 `DeckRuntime.cs:9602-9618`）。
   - `待接线与排队项:55` → 残差「5 条」改「**只剩 S7 探针**」。
2. **就地订正过期结论**（保留订正痕迹，铁律 5）：D1–D9 逐条。
3. **撤销** D10（AUD05-C 视口宽收口）——判据已被推翻。
4. **搬 §二**：A1/A2/B1 与 C1–C7 逐条搬入各线 §二（留痕），§一 只留指针。
5. **不要重派** C1–C7、A1、A2、B1 的任何一条（下一会话会看到本文件）。

---

## 五、覆盖边界（⛔ 别把「没列出」读成「都做完了」）

1. **§一 没逐行读**：战斗线 §一单份 115 KB，本代理按**关键词 grep**（4 批：会话改动名 / 功能词 / 现象词 / 数值码）命中后回源；**没有命中关键词的条目可能被漏**。
2. **只读 §一 + 待接线 + B103 待办正本**；其余章节（§二 已完成、附·一/二/三）**未读**，所以本报告不评「§二 里是否有重复」。
3. **不评「本轮没碰的线」**：卡牌ID逆推线（F1 等主线）、战场线（用户 2026-10-29 暂停）、联机线（T07 纯文档）、活动线 / 音效线 §一（表格为空、只剩已闭环叙述）—— 本轮改动与它们无可证明的交集。
4. **「已解决」一律有现读 `文件:行号`**；「查不到」的地方本文件明写查不到（如 71 的等价性、345 的判据）。
5. **行号会漂**：本文件写的 `RuleCore.cs:17364` / `EffectResolver.cs:18533` 等是**今晚现读**的值；下会话用之前请按符号名重核（铁律 19·4）。
