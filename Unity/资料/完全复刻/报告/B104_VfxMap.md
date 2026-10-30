# B104_VfxMap · 特效触发层 4 条「未接」+ AN1 R3/R12（执行报告）

> 代理：`B104_VfxMap`（执行型，2026-10-31）· 白名单：`CardPresentation/Battle/VfxMap.cs`（**实际路径 = `CardPresentation/Core/VfxMap.cs`**，见 §八-1）·
> `工具/gen_card_vfx_by_card.py` + 产物 · 新建 `CardPresentation/Editor/B104_VfxChain_Assert.cs` · 本报告。
> ⛔ 未跑 Unity · 未动 git · 未改 `账/**` / `判据/B9_*.tsv` / `BattleDriver.cs` / `EventTiming.cs` / `CardFeel.cs` / `Editor/*Scene.cs`。

## 一、结论（一句话）

**6 条里只有 1 条真缺口（PreloadTraitEffect），已实现 + 8 条断言；其余 5 条回源核完 = 早已解决、只是台账 `判据/B9_特效触发链.tsv` 没更新。**

- 真做：`VfxMap.PreloadTraitEffect / PreloadTraitEffectsOfCard(s)`（原版 `BattleManager.PreloadTraitEffect` 的等价物）。
- 已解决、账没更新：`CancelAnimsFromUnit`（原版**死代码**）· `RemoveDestroyableAnims`（2026-10-26 已接线）· `Ambush End(1)`（原版**不可达**）· AN1·R3（数据侧 `B99_A2` 2026-10-30 已补）· AN1·R12（时长表已覆盖）。
- ⚠️ **生产调用点那一行不在我的白名单**（`BattleDriver.Begin`）⇒ 见 §四 的可粘贴补丁，交调度台。

## 二、逐条：现状 / 结论 / 判据 / 落点

| # | 条目 | 台账判定 | 回源现核 | 结论 | 判据（第一权威） |
|---|---|---|---|---|---|
| ① | `BattleManager.CancelAnimsFromUnit` | 未接 | **原版本身是死代码**：四路查证 0 调用点（`out_cpp2il_dev_ilspy/BattleManager.cs:34080` 仅定义 · `out_il2csharp/Assembly-CSharp/BattleManager.cs:14916` 仅定义 · `decomp_full` 全库只命中它自己那份 `.c` · `assets_full` 全部 `MonoBehaviour/*.json` 0 命中）。我们的等价物 = `BattleDriver.CancelPendingAnimsOf(side,slot)`（逐条对 `CancelAnimFromUnitFromQueue`：队尾往前扫 / 只认 `playAnim` / 命中 `targetCard`），**已接**在 `BattleDriver.cs:23017`（`EvtKind.EnchantmentRemoved`） | **不实现 = 原版形状**（铁律 11 例外①）。台账应改「原版无」 | `decomp_full/BattleManager__CancelAnimsFromUnit.c` 全文（四句转调）· `BattleManager__CancelAnimFromUnitFromQueue.c:25-81` · 我们那段结论的原文 = `BattleDriver.cs:34762-34784`（`B95_WBD`/`V3`） |
| ② | `BattleManager.RemoveDestroyableAnims` | 未接 | **早已接线**：`BattleDriver.cs:23019`（`EvtKind.EnchantmentRemoved` → 闸 `e.Amount ∈ {15,16}` → `RemoveDestroyableAnims`）→ `BattleDriver.cs:34534` → `Core/TraitFrames.cs:1338 RemoveDestroyable` → `:1346 DestroyableOne` → `AnimFXController.DoDestroy()` | **已接**（台账过期）。⚠️ 台账那句「EC54/play 的 `Blind_continuous_effect` 留下一件」是**原版同款**（`WarpforgeEffectLibrary.cs` 的 `AutoLifetime` doc，2026-10-26 `V3` 现读推翻），⛔ 别另造清理点 | `decomp_full/BattleManager__RemoveDestroyableAnims.c` 全文（闸 `(buffType-15)>1` · 两本字典 · 逐条 `DoDestroy` + `Dictionary.Remove`） |
| ③ | `BattleManager.PreloadTraitEffect` | 未接 | **真没接**（全仓 `Preload` 只命中音频导入/AssetBundle 那些无关项） | ✅ **本批实现**（机制）；生产调用点 = 补丁 §四 | `decomp_full/BattleManager__PreloadTraitEffect.c` 全文 · `AnimInfo__PreloadAssets.c`（`Load()`→`Instantiate`→`Destroy`）· 唯一调用点 `BattleCardManager__PreloadCard.c:49` ← `BattleCacheInitializer._Initialize_d__0__MoveNext.c:97/158` |
| ④ | `CardTrait(Ambush End(1))` 的播放口 | 未接 | **原版不可达**：`trait_effect_bindings.json` 现读 `ambush` **恰好 1 行、`triggerType = 1 (End)`、prefab `AmbushEffect`**；消费端只认 `DisplayStatusAnim`=0/2、`DisplayTriggerAnim`=3/6 ⇒ 没有任何代码消费 End(1)。我们 `VfxMap.IsKnownGapEvent` 留空 + 出声一次 | **不实现 = 原版形状**（铁律 11 例外①）。台账应改「原版无」 | `VfxMap.cs:308-325`（已有长注释，逐条列三份产物 grep 0 命中）· 本次再核 `CardPresentation/Resources/trait_effect_bindings.json` 的 `ambush` 行 |
| R3 | AN1·R3：`ability` 桶 `tg=-1` 不在 `CardAnimOrder` ⇒ 单点播（原 59 行 / 31 卡） | 未修 | **已修（数据侧）**：`card_anim_order.json` 现读 **93 卡 / 158 拍**；`cards_engine.animTimes` 里带 `tg=-1` 的也是 **93 卡**，**逐卡 0 缺**（id 与卡名两条键都查过）。修法不是我原想的「`TryGetBeats` 现组拍」，而是 `B99_A2`（2026-10-30）**把 31 卡 / 59 拍并进生成器**（`工具/gen_card_anim_order.py` 文件头已写死这条口径） | **已解决**（台账/任务线过期）。断言 `P2` 钉住 | `工具/gen_card_anim_order.py:11-19`（增量源 = 当前 build `cards_raw.json` 顶层 `activeAbility`）· 数据现核（本报告 §七） |
| R12 | AN1·R12：7 张卡能力拍 1.0 s 兜底 + 播未归口件 | 未修 | **已解决**：`ability_anim_durations.json` 现读 `uncovered` = **3 张**（`SOR30` / `SOR65` / `SW9`），逐张理由 = **原版 `activeAbility` 一条非空 CardAnim 都没有**（`m_AssetGUID` 全空）⇒ 原版这条路上本来不放动画（例外①）。原 7 张里 `SOR66`(2.0) / `SOR70`(0.2) / `SOR34`(0.2) 已有真值；`ASH74` / `GOF77` / `SOR23` 没有 `tg=-1` 行、也不在 `wantedCards(86)` 里（= 它们不发 Ability 事件 ⇒ 兜底值根本不被消费）。两链交叉校验 93/93 一致 | **已解决**（台账过期）。断言 `P3` 钉住 | `数据/游戏数据/ability_anim_durations.json` 的 `_headline` / `stats` / `uncovered`（生成器 `工具/gen_ability_anim_durations.py`）· 我另核了 `cards[].active` 逐卡 |

## 三、改动清单

| 文件 | 行 | 内容 |
|---|---|---|
| `CardPresentation/Core/VfxMap.cs` | **3964-4103**（+140 行，**追加在类尾**；`git diff --numstat` = `140 0`） | `PreloadTraitEffect(trait, army=-1)` · `PreloadTraitEffectsOfCard(CardDef, army=-1)` · `PreloadTraitEffectsOfCards(IEnumerable<CardDef>)` · `WarmEffectPrefab`（`Instantiate`→立刻销毁；编辑器走 `DestroyImmediate`）· 计数/去重/自检读口 `PreloadedTraitPrefabCount` / `TraitPrefabsInstantiatedTotal` / `TraitPreloadFailures` / `PreloadedTraitPrefabsForTest()` / `ResetTraitPreloadForTest()` |
| `CardPresentation/Editor/B104_VfxChain_Assert.cs` | 新建，**380 行**（+ 同名 `.meta`，格式/长度与 `B103W1_Assert.cs.meta` 同款：59 B、2 行） | 类 `B104_VfxChain_Assert`（全局命名空间、类名=文件名）· `Run()` 幂等无副作用（只 `Instantiate`+立刻销毁、清自检计数器）· `Passed`/`Failed` |
| `CardPresentation/Battle/BattleDriver.cs` | **5433-5438**（+6 行：4 行注释 + 2 行调用，**只此一处**，⛔ 未动该文件其它内容） | `Begin` 里 `BuildOpeningDeckData(...)` 之后调 `VfxMap.PreloadTraitEffectsOfCards(myCards/foeCards)`（= §四） |
| `判据/B9_特效触发链.tsv` | **14 / 16 / 45 行**（3 行就地订正；8 列保持、LF 未翻） | `CancelAnimsFromUnit` → `原版无` · `RemoveDestroyableAnims` → `已接` · `Ambush End(1)` → `原版无`；每行备注写「2026-10-31 订正（铁律 5，`B104_VfxMap`）」+ 判据文件:行。⛔ 其它 42 行未动；重数 = **已接 29 / 未接 1 / 判不了 3 / 原版无 12**（45 条数据行） |
| `工具/gen_card_vfx_by_card.py` + 产物 | —— | **未改**（R3/R12 的缺口都不在它这条数据链上；见 §二 R3/R12） |

### 断言 **33 条**（`B104_VfxChain_Assert.Run()`；⛔ 前提与结论各一条、都无条件执行，不写 `if (前提) Check(结论)`）

- **P0 前提 2 条**：卡池载得到；`trait_effect_bindings.json` 有行（⛔ 载不到时下面的条目是「没验到」不是「通过」）。
- **P1 预热 14 条**（本批唯一代码改动的断言）：`P1-0/0b` 前提（有带真件、且那些件在效果库里都取得到的样本行）· `P1-1` 解析 ≥1 件 · `P1-2` 去重数 == 解析数 · `P1-3` **真实例化过**（不是只记账）· `P1-4a/b` **幂等**（第二次不再实例化）· `P1-5` **逐件对账**（预热集合 == 该 trait 全部行的默认件去重）· `P1-6` 反例（不存在的 trait ⇒ 0 件零副作用）· `P1-7/7b` 卡级正例（真卡 `CardTraits` → ≥1 件）· `P1-8a/b` 卡级反例（`null` / 无 trait ⇒ 0）。
- **P2 R3 回归 3 条**：`P2-0` 前提（带 `tg=-1` 的卡 > 0）· `P2-1` **每张都 `TryGetBeats ≥ 1`**（= AN1 R3 的原始验收标准）· `P2-2` 反例（不存在的卡 = 0，证明 P2-1 不是恒真）。
- **P3 R12 回归 4 条**：`P3-0` 前提 · `P3-1` 同一批卡 `AbilityAnimDurations.TryGet` 必须全部命中 · `P3-2/3` 反例（无 `tg=-1` 行且卡名全池唯一的卡必须查不到）。
- **P4 Ambush 5 条**：恰好 1 行 / `TriggerType==1` / 不是 5 档 / `IsKnownGapEvent` 真 / `Resolve` 回 null。
- **P5 已核销两条的结构闸 5 条**：`CancelPendingAnimsOf` · `RemoveDestroyableAnims` · `TraitFrames.RemoveDestroyable` · `AnimFXController.DoDestroy` · `EvtKind.EnchantmentRemoved` 五个符号都还在。

> **灭自证**：见断言文件头那张表（改空转 / 删去重 / 丢阵营参数 / `AllRowsFor` 只给一本字典 / 退回 62 卡 / 删 `tg=-1` 行 / 给 ambush 补 FromCode(5) / 删接线符号 ⇒ 各自点名哪一条红）。

## 四、✅ 生产调用点（**2026-10-31 续做已落**，所有权更新后 `BattleDriver.cs` 这一处归本代理）

原版调用链 = `BattleCacheInitializer.Initialize`（开局前）→ 双方牌库**每一张卡** → `PreloadCard` → 逐条 `CardTraits` → `PreloadTraitEffect`。
我们的对应点 = `BattleDriver.Begin` 里 `myCards` / `foeCards` 已解析好、`Ctx = RuleCore.NewBattle(...)` **之前**。

**已插入**（`BattleDriver.cs:5432` 的 `BuildOpeningDeckData(myDeck, foeDeck, myCards, foeCards);` **紧接其后、`WarnMissingPresentationData();` 之前**，共 6 行 = 4 行注释 + 2 行调用；⛔ 未动该文件其它任何内容）：

```csharp
            BuildOpeningDeckData(myDeck, foeDeck, myCards, foeCards);
            // 🆕 2026-10-31（`B104_VfxMap`）：原版 `BattleCacheInitializer._Initialize` →
            //   `BattleCardManager.PreloadCard` → `BattleManager.PreloadTraitEffect`。
            //   判据 = `decomp_full/BattleManager__PreloadTraitEffect.c` + `AnimInfo__PreloadAssets.c`
            //   （`Load()` → `Instantiate` → `Destroy`）；机制实现见 `VfxMap.PreloadTraitEffectsOfCard`。
            VfxMap.PreloadTraitEffectsOfCards(myCards);
            VfxMap.PreloadTraitEffectsOfCards(foeCards);
            WarnMissingPresentationData();
```

- ⛔ 若担心开局多花几十毫秒，**照原版做**（原版就在开局前做同一件事）；本机影响面本来就 ≈0（效果库是 SO 直接引用）。

## 五、typecheck

```text
TMPDIR=/tmp/wf_vfxmap bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

- 写实现后 / 改断言后 / 再改断言后各一次 = **三次都是 0 / 0**。
- 🔴 **2026-10-31 续做（插 `BattleDriver` 那 6 行之后）那一跑**：运行时 **1 条错**，位置 = `Assets/CardPresentation/Shell/CampaignData.cs(688,13): CS0103 名称"Debug"不存在` —— **不是我的文件**（另一个代理正在写它：该文件 `git status` 是 `M`，缺 `using UnityEngine;` 的半成品状态），错误**只集中在那儿**、我插的两行没有任何诊断 ⇒ 按本仓既有判据「错误全部集中在不是你负责的文件上 ⇒ 不是你的问题」。
- ✅ **隔离证明**（⛔ 没碰工程里那个文件）：把 `CampaignData.cs` **拷到临时目录**、只在副本里补一行 `using UnityEngine;`、把 rsp 里那一条换成副本路径后直编运行时程序集 ⇒ **0 条 error**。⇒ 唯一那条错是别人的半成品，**我的改动编得过**。
- ⚠️ **没有跑任何 Unity 宿主**（本批明令不跑）⇒ 断言**一次都没执行过**，这是事实，别把 typecheck 绿读成「断言过了」。

## 六、没做的与原因

1. **R3/R12 的进一步代码改动**：回源核完 = **已经解决**（数据侧），再改就是把活的改坏。R3 落点 `CardAnimOrder.cs` / `BattleDriver.cs` 也都不是我白名单（`⛔ 不碰 BattleDriver.cs`），而我核出现状**不需要改**。
2. **`Ambush End(1)` 的播放口**：判据为空（原版三份产物 0 消费点）⇒ 铁律 11 例外①，**不做**；保留 `IsKnownGapEvent` 出声。
3. **`CancelAnimsFromUnit` 的四条道**：原版死代码（四路 0 调用点）⇒ 铁律 11 例外①，**不做**；我们只保留「按卡撤 `playAnim`」那一条。
4. **`RemoveDestroyableAnims`**：已接线，**不动**。
5. **`PreloadTraitEffect` 的生产调用点**：✅ **2026-10-31 续做已落**（所有权更新后 `BattleDriver.cs` 那一处归本代理）—— 见 §四。整条链现在是**机制 + 调用点都在**，但**仍未跑过**（本批不跑 Unity）。
6. **数据重生成**：**没有重生成任何 JSON**（改的是代码 + 新断言文件）⇒ 无需 `--check` 幂等；R3/R12 的读数全部来自现读盘上文件。

## 七、账的覆盖边界

- 本报告覆盖的是**特效触发链**（`判据/B9_特效触发链.tsv`，**45 条数据行**（46 行含表头）里的 4 条「未接」）**+ AN1 的 R3/R12 两笔**，**不是特效层全部**。
- 「读过 / 核过」≠「跑过」：本批**零 Unity 执行**，所有断言（**33 条**）到交回为止**一次都没跑**。
- 结论的强度分三档，逐条已在 §二 标明来源：① 反编译原文（`decomp_full` / `cpp2il`）—— 最强；② 数据文件现读（`card_anim_order.json` / `ability_anim_durations.json` / `trait_effect_bindings.json`）—— 强，但只代表**盘上这一版数据**；③ 别人的结论（`V3` 的四路查证、`B99_A2` 的生成口径）—— 我只做了抽样复核，没有重跑那四路 grep。
- 未覆盖：`判据/B9` 其余 **41 行**（订正后 = 已接 29 / 未接 1 / 判不了 3 / 原版无 12，我逐行读的只有「未接」那 4 条）本批**没有重新核**；「判不了」那 3 条由只读代理 `B104_VFX判据` 负责。

## 八、顺手发现（⛔ 我没改，交调度台分流）

1. **🔴 文件所有权表里我的路径是错的**：`文件所有权_当前.md:27` 写 `CardPresentation/Battle/VfxMap.cs` —— **全仓没有这个文件**，真身是 `CardPresentation/Core/VfxMap.cs`（本报告全部改动落在 Core 那份）。`B104_CardFeel` 那行的排除清单里也写着 `VfxMap.cs`，两条都建议改成 `Core/VfxMap.cs`，否则下个代理会「按路径找不到文件」。
2. **台账三处过期 —— ✅ 2026-10-31 续做已就地订正**（`判据/B9_特效触发链.tsv` 第 14/16/45 行，所有权更新后归本代理）：`CancelAnimsFromUnit` → «原版无（死代码）»；`RemoveDestroyableAnims` → «已接»（并把「有可见后果」那半句按 `V3` 的现读改写）；`Ambush End(1)` → «原版无（不可达）»。⛔ 其它行一个字没动。
3. **`判据/B9` 的计数**：订正后重数 = **已接 29 / 未接 1 / 判不了 3 / 原版无 12**（45 条数据行 = 46 行含表头；按第 7 列逐行数）。⚠️ **TSV 本身没有「计数行」**（只有表头 + 45 条数据）⇒ 无处可「跟」，所以上面那组数只写进本报告；**旧数字仍留在** `任务线/特效线/任务总表.md:52/174` 与 `完全复刻/判据/B104_本会话待办与裁定.md:88`（都不是我的文件，未动）。
4. **卡池张数又涨了**：`RuleEngine/Resources/cards_engine.json` 现读 `cards` **1129** 张（2026-10-15 的交接写 1127、2026-10-30 的 `card_anim_order` 统计里用 93 卡）—— 与本次结论不冲突（R3 的 93 卡是按 `tg=-1` 现算的），但**引用张数的一律现读**。
5. **`TraitFrames.AllRowsFor` 是「整列」读口**（2026-10-29 `F5` 落地，`TraitFrames.cs:1360-1380`）—— 本批的预热正是靠它才和原版 `GetTraitEffects` 同形；以后谁把它改成「三本字典拼起来去重」，预热会**静默少件**（`P1-5` 会红）。
