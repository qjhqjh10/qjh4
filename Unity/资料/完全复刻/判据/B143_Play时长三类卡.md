# `EvtKind.Play` 的时长：三类卡（单位 / 战术 / 防御）逐类核对（只读）

> 执行写手 `B143-W1` · 2026-11-05 · 上游 = `判据/B143_零时长假设横切.md` §3·2 那条 🟠
> ⛔ 本文件**不改任何代码**（`BattleDriver` 归 W6）。✅ 行号 = 现读。

---

## 〇 · 先纠我自己上一轮的一句话（铁律 5）

上一轮我写「战术卡打出当刻**没有任何占时的起手演出**」——**不准确**。现读：战术卡打出当刻**有**演出，
走的是 **`play` 桶**（`VfxMap.PlayCard` → `PlayPlacedPieces`），只是**它的时长不计进时间线**。
真正的差异不是「有没有起手」，而是「**起手演多久算不算数**」。

---

## 1 · 我们这一侧（现读，逐类）

| 环节 | 现读 |
|---|---|
| 发出点 | 单位 = `RuleCore.cs:4092`（`PlayCard`）；**战术/防御 = `EffectResolver.cs:2712`**（`PlayTacticCore`，`Slot = targetSlot`，无目标则 `-1`） |
| 事件时长 | `EventTiming.DurationOf(Play)` = **0**（`Play` 在 `ZeroDurationKinds` 里，`EventTiming.cs:~441`） |
| **有没有演出** | ✅ **有**：`BattleDriver.cs:22001` `case EvtKind.Play: return VfxMap.PlayCard;`；`:23538` 那一支把 `Play` / `Trigger` 两桶送进 `PlayPlacedPieces`（逐拍参数：`startPos/endPos/pointTowards/move/ease` 全消费） |
| **起播偏移** | `PlayBucketOffsetOf(e)`（`BattleDriver.cs:23944-23966`）：`Slot < 0` ⇒ **0** · 单位 ⇒ **1.30**（`EventTiming.PlayBucketHandPlayedHold`，`:310`）· 战术 `targeted` 且**敌方**手牌 ⇒ **0.5**（`EnemyTargetingHold`，回放跳过档 ÷3）· 其余战术 ⇒ **0** |
| **它占不占时间线** | ❌ **不占**：`Play` 的 `DurationOf` 是 0，折叠件自己的时长**没有**记进 `t` |

⇒ 一句话：**三类卡的 `play` 桶都真的会播；差别只在「起播偏移」与「结束后时间线等不等它」**。
后者今天对所有卡都是「不等」。

---

## 2 · 原版这一侧（判据逐条）

> 正本 = `BattleDriver.cs:23930-23966` 那段 doc 已经抄过的 `.c`（本文件只做**交叉核对**并把三类分开）。

| 类 | 原版时间线 | 判据（现读引用） |
|---|---|---|
| **单位（手牌打出）** | 登场链 **全部 `yield` 完** → 才 `BroadcastCardPlayed`（= `play` 桶起播）⇒ 累计 **1.30 s** = `MinionPlayedIntoField` 0.45 + `minionToConversionPointTime` 0.3 + 另一段 0.55（分段见 `PlayBucketHandPlayedHold` 的 doc：`delay 0.1 + timeBeforeLand 0.05 + timeToLand 0.2 + attackStepTime 0.1`） | `_ResolvePlayCardFromHand_d__447` **state 0xb / `:1426`**（`BroadcastCardPlayed` 的唯一调用点） |
| **战术 / 防御（无目标或本机打出）** | `_d__447:641` **直落广播块**（中间**没有** `yield`）⇒ 偏移 **0** | 同上 `:641`（`goto LAB_1809bdcfc`） |
| **战术（`targeted` · **敌方**手牌）** | 取牌 + `PayCardCost` 之后先 `EnemyTargetingAnim`，`WaitForSeconds(GetTargettingTime() = 0.5)`，**然后**才 `TargetedSpellPlayed` ⇒ 偏移 **+0.5** | `_d__447:477-491` |
| **防御卡属于哪类** | 原版 `cardType == 20 (Tactic)` 同一档（我们的 `tactic` / `defence` 都是 20） | `EffectResolver.cs` 的 `PlayTactic` 头注释 + `RawCardScript__OnCardPlayedWithTarget.c:73` 那两条 `cardType` 判断 |
| **`play` 桶的内容来源** | `abilityLogic.cardAnimations[]`（= **trigger-0 那一条 ability** 逐拍），经 `AbilityLogic.PlayAnim` 播 | `PlayAbility.c:273` → `PlayAnim.c:45-60` |

---

## 3 · 逐类判定

| 类 | 起播偏移：我们 vs 原版 | 时长：我们 vs 原版 | 判定 |
|---|---|---|---|
| 单位（手牌） | **1.30 vs 1.30** ✅ | 0 vs（登场链的 1.30 **已算在 `Deploy` 里**）✅ | **✅ 一致** |
| 战术（本机 / 无目标） | **0 vs 0** ✅ | 0 vs 0（广播块本身不 `yield`）✅ | **✅ 一致** |
| 战术（targeted · 敌方手牌） | **0.5 vs 0.5** ✅（`AU1` 已接，回放 ÷3） | 同上 ✅ | **✅ 一致** |
| 防御卡 | 同战术 ✅ | 同战术 ✅ | **✅ 一致** |

⇒ **结论：`✅ 我们与原版一致（都不为 `Play` 这一拍额外等待）`。上一轮那条 🟠「一旦加起手就静默吞时长」的前提不成立 —— 起手早就有了、且偏移与原版逐档对齐。**

---

## 4 · 剩下的**真差异**（不是「没有载体」，是「折叠件不算时长」）

原版 `play` 桶那一件 = `abilityLogic.cardAnimations[]`，由 `AbilityLogic.PlayAnim` 播 ——
**`PlayAnim` 是逐拍带 `waitAnimToApplyEffect` 的**（`AbilityLogic__PlayAnim.c:274`，同 `CardAnimBeat.Waits`）。
我们把它**折叠成单件**（`card_vfx_by_card.json` 的 per-card picks），**拍序、每拍时长、起→终锚点全丢**，
而且**一点时长都不推进时间线**。

- **今天的影响**：单位卡无感（它演在 `Deploy` 那 1.30 s 的窗口里）；**战术卡**这一件与紧接着的正文事件**同刻起播**，
  看起来像「特效和结算一起发生」——这正是 `ANIM-TRIGGER` 那条账要收的东西。
- **归属**：`play` 桶**不在** `B143_AnimTrigger接线补丁.md` 的范围（那个补丁只接 `EvtKind.Trigger`，且明确「`Play` 单独立项」）。
  所以这条**不是新账**，是那条线上「`Play` 那一半」的现状描述。
- **量化**：`play` 桶现读 **796 条源全是 `abilityLogic.cardAnimations[]`**（`BattleDriver.cs:23940` 的注释），
  即「726 卡 / 1,178 拍」那张新表里属于 `ThisCardPlayed(579 行)` 的那一大块 —— 接上它才谈得上「`Play` 也逐拍」。

---

## 5 · 顺手发现（⛔ 都没改）

1. `PlayBucketOffsetOf` 的 doc 里有一句「`teleport` 的卡 ≈ 1.25（`J1` 标「待核」）⇒ 今天按 1.30 排（差 0.05，如实记）」
   —— 这是**已知的 0.05 差**，与本条同族（都是 `Play` 的偏移档），登记在这里免得再被当新账查一遍。
2. `EvtKind.Play` 在 `DurationOf` 里是 0，而 `play` 桶**有**真实时长 ⇒ 若将来要让 `Play` 承载时长，
   正确落点是 `DurationOf(BattleEvent)` 按卡分档（`e.CardId` → 该卡 `ThisCardPlayed` 首拍之和），
   ⛔ **不能**把 `Play` 塞进 `TimedKinds` 一刀切（那会把单位卡也推后 1.30 的两倍）。
3. 单位卡的 `play` 桶偏移 1.30 与 `DeployHoldOf` 是**两件事**（`:23942` 的注释已经写了「⛔ 别把它并进 `DeployHoldOf`」）——
   这句话很关键：谁要「统一时长」时会第一个撞上它。

---

## 6 · 2026-11-05 追加：**`Play` 那一半的数据/生成器/读取口已做出来**（未接 `BattleDriver`）

| 件 | 现读 |
|---|---|
| 生成器 | **新建** `工具/gen_card_anim_order_play.py`（`--write` / `--check` / `--dry` / `--diff-old`；**不带参 = 打印用法 + rc 2**；复用 `gen_card_anim_order.slim_beat`，⛔ 没抄第二份） |
| 新表 | **新建** `MyGame/Assets/CardPresentation/Resources/card_anim_order_play.json`（`--check` 幂等 = True，333 KB） |
| 读取口 | `CardAnimOrder.TryGetPlayBeats(卡名或id, List<CardAnimBeat>)` + `PlayRowCount` + `PlayCount` + `PlayLoaded` + `PlayFallbackHits/PlayLastFallback` + `ResetPlayForTest`（**只加新口**，`TryGetBeats` / `TryGetTriggerBeats` 一行未动） |
| 断言 | `CardPresentation/Editor/B143_R1_Assert.cs` 新增 **H 组 25 条**（总数 / 口径自洽 / 身子重算 / `(ai,li)` 升序 / 单行与多行抽样 / 未知卡 fallback 计数 / **跨表同源**） |

### 6·1 新表口径（现读，`--write` 的 `stats`）

```
cards=475  rows=579  steps_raw=703  steps=697  blank_guid=6
pure_wait=9  movers=138  offset_beats=12  cards_no_rows=359  not_in_pool=94
```
- `rows=579` 与触发表的 `this_card_played_rows=579` **完全对上**（同源，同一条 filter 的两种切法）；
- `steps_raw − steps = 6 = blank_guid`（空 `ca` 拍按原版 `PlayAnim:62-64` 丢掉）。

### 6·2 与旧 `play` 桶的差集（`--diff-old` 现算；⚠️ **差集是「卡集合」，不是「796 条 picks」**）

> 施工单里的 **796** 是 `play` 桶的**源行数**（`BattleDriver.cs:23940` 注释：`play` 桶 796/796 条源全是
> `abilityLogic.cardAnimations[]`），那是**行**的口径；旧表本身没存行号索引 ⇒ 可比的只有**卡集合**。

| 口径 | 数 |
|---|---:|
| 新表卡数（`trigger == 0` 且有可播拍） | **475** |
| 旧 `play` 桶卡数（`card_vfx_by_card.json` 的 `byCard[id].play` 非空） | **534** |
| 交集 | **473** |
| **新表有、旧桶没有** | **2**：`TL64` · `UM39` |
| **旧桶有、新表没有** | **61** |
| ‣ 其中「**不在全量件 `card_anim_order.json` 里**」（= 全量件覆盖缺口，**不是口径差**） | **57** |
| ‣ 在全量件里、但**没有 `trigger == 0` 的行** | **4**：`ASH52` · `GOF26` · `SW29` · `UM89` |

- **57 条的性质**：全量件只覆盖 726/1129 张池内卡（它的 `cardAbilities` 那一半），剩下 403 张只有 activeAbility 或根本没有行
  ⇒ 这 57 张的旧 `play` pick 是 `card_vfx_by_card.py` 从**卡 SO 直读**来的，`card_anim_order.json` 里查不到 ⇒ **新表天然覆盖不到**（要补得先补全量件，那是另一条账）。
- **4 条的性质**：它们的 `cardAbilities` 行在，但都不是 `trigger == 0`（旧桶把「第一条 ability 的第一件动画」当 play，不看 trigger）⇒ **旧桶口径更宽**。
- **2 条的性质**：`TL64` / `UM39` 有 `trigger == 0` 的卡Anim 行，但旧桶把它们归到了 `trigger`/`_assetRefs` 档 ⇒ **旧桶漏了**（`UM39` 的 pick 落在 `byCard.UM39.trigger` 里）。

⇒ 一句话：**两张表不是同一集合，差 63 张卡，原因分三类（57 全量件缺口 / 4 旧桶不看 trigger / 2 旧桶归档错）**。
新表**只装 `trigger == 0`**，比旧桶**窄**（这是口径选择，不是回归）；接消费端时**必须按新表**（旧桶那 61 张会**少播**，
但它们今天也是「折叠单件」，两边的差异只在「拍序/时长」这一层）。

### 6·3 断言 H 组（25 条，宿主未接）

| # | 钉什么 | 灭自证 |
|---|---|---|
| H0–H2 | 表载得到 / 解得开 / `PlayLoaded` | 删表 ⇒ 红 |
| H3a–f | `cards 475` · `rows 579` · `steps_raw 703` · `steps 697` · `blank 6` · `raw−steps==blank` | 删一卡/一行/一拍 ⇒ 至少一条红 |
| H4a–c | **从身子重算** == `stats`（只改头不改身子 ⇒ 红） | — |
| H4d | 全表 `rows` 按 `(ai,li)` **升序** | 重排 ⇒ 红 |
| H5 | `CardAnimOrder.PlayCount == 475` | — |
| H6 / H6b–e | `EC29`：1 行 1 拍、`ca=bc43540f…`、`vfx=EC Heldrake Rally`、`tm=2.75` | 改生成器/手改表 ⇒ 红 |
| H7 / H7b | `SAU46` 出牌表 **3 行** / 3 拍 | 少一行 ⇒ 红 |
| H8 / H8b | 未知卡 ⇒ 0 拍 **且** `PlayFallbackHits +1` | 挡「静默返回 0」 |
| H9 / H9b | `EC29` 在**触发表**与**出牌表**里是**同一个 `ca`** | 两表分裂 ⇒ 红 |

### 6·4 读数

| 命令 | 结果 |
|---|---|
| `python 工具/gen_card_anim_order_play.py --check` | **幂等 = True** |
| `TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `bash 工具/ruleprobe.sh play check` | ⚠️ **指纹变了**：`2f6946d6e5feae8b09983d3ab826cc9a` ≠ 基线 `2f640db5…`（rc=1），但 **①-a 主档 1142 张逐字节一致、①-b 电池也一致** ⇒ **不是结算变化**。<br>归因：`RuleEngine/Resources/cards_engine.json` 的 mtime = **01:29**（另一路写手刚重新生成卡池），且同一分钟 `RuleCore.cs` / `ActionPump.cs` 也在写。<br>**与本批无关**：我改的 3 个文件（`CardAnimOrder.cs` = `CardPresentation/`、`card_anim_order_play.json` = 纯资源、`B143_R1_Assert.cs` = `Editor/`）**都不在 `ruleprobe.csproj` 的编译集内**（它只编 `RuleEngine/Core/*.cs` + 列出的 `Data/*.cs`，见 `csproj:9/144`）。⛔ 未重钉基线。 |
