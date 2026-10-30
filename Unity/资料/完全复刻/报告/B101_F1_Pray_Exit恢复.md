# B101_F1 · 恢复 `Pray_Exit`（撤销 `B100_F11` 的 `strip_enchantment_unit_anims`）

> 判据 = `资料/完全复刻/报告/B101_R4_演出与预览判据.md` **§6·8**（该文件已就地作废自己的 §一 / §1·3 / §1·4 / §1·5）。
> 一句话：**原版会播 `Pray_Exit`**（`AbilityLogic.PlayAbility` → `PlayAnim` → `BattleManager.AddCardAnim`，演在**载体单位**身上），
> `B100_F11` 为了让我们自己那条计数断言过闸，把这条**原版真实数据**从产物里删掉了 ⇒ 本件撤销。
> ⚠️ 「今天仍播不出来」这一点**仍然成立**（`Enchantments.Matching` 认不出这条 ability；`VfxMap` 按**被打的卡**查表，附魔卡不受伤）
> ⇒ 本件目标是**恢复数据 + 修正尺子口径**，不是「让它今天就能播」。

---

## 一、白名单内的改动

### 1·1 `工具/gen_card_vfx_by_card.py`（`git diff --numstat` = **25+ / 83−**）

五处，全部是「把 F11 的那一层拆掉」，**没有新增任何过滤**：

| # | 位置（本次改完后的行号） | 改前 | 改后 |
|---|---|---|---|
| ① | `:1873-1898` | F11 的论证段（「附魔不是单位 ⇒ `hit` 桶不收附魔卡」）+ `def strip_enchantment_unit_anims(…)`（整函数，约 46 行） | 一段「**撤销**」的判据段（`B101_R4` §6·8 的三条证据 + 「今天仍播不出来」+「⛔ 别搬去 `EnchantmentRemoved`」），**函数整个删掉** |
| ② | `:2202-2206` | `en_st = strip_enchantment_unit_anims(by_card, cards, not_played)` + `[3·e/4]` 打印 + 逐条 `⛔ 不播` 打印（10 行） | 两行注释：**不做任何卡型过滤** |
| ③ | `:2450-2451` | `"enchantmentNotUnit": en_st,`（含 3 行注释） | 一行注释说明「该格已随 strip 撤销，⛔ 别再加回来」 |
| ④ | `:2639-2640` | `_note` 里「`notPlayed[].outcome` 多了第三种 `enchantment-not-unit`」 | 改成「**已撤销** ⇒ 现在只剩 `duplicate` / `unbacked` 两种」 |

原址留的标记行（简报要求的那一行，逐字）：

```python
#  🔴 2026-10-30 B101_F1：撤销 B100_F11 的 strip —— 判据 @B101_R4 §6·8（原版走 AbilityLogic.cardAnimations）
```

**为什么选「整函数删掉」而不是「只把 `Pray Enchantment` 排掉」**：R4 §6·8 否掉的是**整套前提**（附魔那条 `cardAnimations`
走的是**独立通道**，与「单位技能动画」不是一回事），不是「这一张卡特殊」。留一个「只跳过某张卡」的白名单
== 把「这张卡例外」写进生成器，下个会话会拿它当判据。

> ⚠️ 生成器的另一个 `strip_*`（`strip_unbacked_inference`，`conf = inferred-*` / `unknown` 那一档）**没动** —— 那是另一条口径，与本件无关。

### 1·2 `CardPresentation/Core/VfxMap.cs`（`git diff --numstat` = **2+ / 2−**，**零行数变化**）

简报写的是 `VfxMap.cs:3521`（那是 F11 报告当时的行号，现读漂到 **:3530**）。按铁律 5「同一句话被复制到别处也要一起改」，
**两处同文注释都改了**（`:3409`（`SelfCheckRoutedTriggers` 里那份副本）与 `:3530`（`SelfCheckUnitSkillAnims` 第 ① 支））：

```csharp
// 改前（两处逐字相同）
if (string.Equals(card.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;   // 🔴 2026-10-25：附魔不是单位（它的 trigger 60 是附魔能力，不是单位技能动画）⇒ 不进 unit_*_anim 桶、本自检也不要求搬

// 改后
if (string.Equals(card.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;   // 🔴 2026-10-30 B101_F1：口径应为「按**单位卡**筛」（原版 `AddCardAnim` 的 `actingCard` 是载体单位，判据 `B101_R4` §6·8）—— ⚠️ 现行为仍是「跳过附魔」、与口径不符：待调度台删本行（数据侧已恢复 `Pray_Exit`）
```

🔴 **代码行为一个字没动**（简报第三条第 4 句）。文案与行为**现在是不符的** —— 按简报要求**不擅自改**，改动清单见 §四·2。

---

## 二、产物变化（逐条，可复跑）

两份产物都由**同一个脚本**产出（⛔ 没手改）。`git diff --numstat`：

| 产物 | numstat |
|---|---|
| `Unity/数据/游戏数据/card_vfx_by_card.json`（全量） | **31 / 42** |
| `Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json`（运行时） | **1 / 1** |

（正好是 `B100_F11` 报告里 `42 / 31` 与 `1 / 1` 的**反向** ⇒ 这一层确实被逐字撤回到 F11 之前。）

**卡-动画条目级 diff（拿 `git show HEAD:` 那一版与现盘逐条比，脚本见下）：**

```
removed 0
added   1
  + ('9218576fd3e26554db5b4c239a36dbc7', 'hit', 'Pray_Exit', 'Pray_Exit')      # Pray Enchantment · Sororitas
```

⇒ **变化面 = 1 条**（远低于简报「> 60 条先停手」那条线）。**没有任何别的卡/事件被增删。**
配套的 `notPlayed[]`：**−1 条**（`9218576f… / hit / Pray_Exit`，`outcome = enchantment-not-unit` 那条），无新增。

**计数类字段（全量件）：**

| 字段 | HEAD | 现在 | 为什么 |
|---|---|---|---|
| `stats.cardsCoveredByEvent.hit` | 1 | **2** | 多回这张卡 |
| `stats.entriesByConf.authoritative` | 4584 | **4585** | 同上（`conf = authoritative`） |
| `stats.triggerBuckets.rows.mapped` | 854 | **855** | 这条 row 归口是 `mapped`（trigger 60 → `hit`） |
| `stats.triggerBuckets.cards.mapped` | 585 | **586** | 同上 |
| `stats.notPlayed.length` | 23 | **22** | 撤销那一条 |
| `stats.enchantmentNotUnit` | 有（1 条 dropped） | **整格消失** | 随 strip 撤销（`⛔` 见 §一·①③） |
| `generatedAt` | — | 变了 | 每次跑都变，正常 |

**运行时件：** `entryCount` 2674 → **2675** · `notPlayed.length` 23 → **22** · `cardCount` 1100（不变）·
`cards[65].events` 1 → **2**（`hit/Pray_Exit` 回到 `events[0]`，`trigger/Pray_Success` 仍在）。

条目本体（全量件，逐字）：

```json
"hit": [{"conf": "authoritative", "src": "cardSO.cardAbilities[0].abilityLogic[0].cardAnimations[0]",
         "soField": "cardAbilities[0].abilityLogic[0].cardAnimations[0]",
         "guid": "72426af784f94aa40abc1fcc52e2b7b4", "cardanim": "Pray_Exit", "prefab": "Pray_Exit", …}]
```

⇒ 这正是 R4 §6·8 说的那条链：`cardAbilities[0]`（trigger 60 = `UnitDamaged`，`chosenAbility = removeThisEnchantment`）的
`abilityLogic[0].cardAnimations[0]`，原版由 `AbilityLogic.PlayAnim` 播。

**复跑核对用的脚本**（本次实跑；两份都按「(卡 id, 事件, prefab, cardanim) 四元组取集合再比差」）：

```python
a, b = entries(git_show_HEAD_full_json), entries(新盘全量 json)
print(sorted(b - a))   # → 只有 ('9218576fd3e26554db5b4c239a36dbc7', 'hit', 'Pray_Exit', 'Pray_Exit')
print(sorted(a - b))   # → []
```

---

## 三、验收

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b101_f1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅ |
| `python -I -X utf8 工具/check_ledger_points.py --all-A` | **漂移 0 · 失败 0**（A 5729 / 通过 5729）✅ |
| 行尾 | 两个源文件都是 **CRLF 0 / 裸 CR 0**（`VfxMap.cs` 3962 行 · 改前改后**行数相同**；`.py` 2778 → 2720）。⚠️ `VfxMap.cs` 的改动是**同位置换注释**，没有插行 ⇒ 账的行号不漂移 |
| 产物 diff 可解释 | ✅ 只有 1 条卡-动画条目（见 §二） |
| 产物 = 生成器产出（幂等） | ✅ `--check --generated-at "2026-10-30 12:28:58"` ⇒ 两份**逐字节一致**（3,268,584 B / 420,489 B），rc=0 |

> ⚠️ **账闸的 before 不是 0/0**：本件**开工时**跑了一遍基线 = **漂移 55 · 失败 8**（落点集中在
> `个人资料_成就_排行.tsv` / `其他_基础设施.tsv` / `商店.tsv` / `日常_奖励.tsv`，都是 `Shell/*.cs` 的行号对不上）。
> 本件**收工时**再跑 = **0 / 0** —— 那不是本件修的（本件只碰了 `.py` + 一行 C# 注释），而是**期间别的代理把那些行对齐了**。
> ⇒ 结论只说一件事：**本件没有引入新的漂移**（`VfxMap.cs` 行数守恒 · `.py` 不是账的落点目标）。

---

## 四、给调度台的宿主改动清单（⛔ 本件没动这些文件）

> 数据侧已经回到「合法数据」，**下面四处不改就会红**。顺序建议：4·2（VfxMap 行为）→ 4·1（BattleScene 数值）→ 4·3（撤 F11 断言）→ 4·4（注释）。

### 4·1 `CardPresentation/Editor/BattleScene.cs` —— 现读 `:13428-13468`（简报写的 `13423-13430` 是漂移前的号）

**(a) `:13433-13434`：两行**

```csharp
// 改前
                            // 🔴 2026-10-25：**附魔不是单位** —— 它的 trigger 60 是附魔能力（如 `Pray_Exit`），不算单位技能动画。
                            if (string.Equals(cd.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;
// 改后（整两行删掉）
```

> 为什么必须删：不删的话 `udRows` 停在 1、而**运行期桶是 2 条** ⇒ 下面 (b)(c) 两条会比出一个说不清的差额；
> 删掉后 `udRows = 2`（`BL20` + `Pray Enchantment`），与桶一致，而且**尺子口径**才等于 R4 §6·8 的「按单位卡筛」。

**(b) `:13446-13448`：**

```csharp
// 改前
                    Check(udRows == 1,
                          $"★ W63/技能动画：原版 `UnitDamaged(60)` 在卡池里 **1 行**（`BL20`，实得 {udRows}）"
                        + "—— 🔴 2026-10-23 Trait 权威化后另外 3 条已剔掉（2 条「删重复」· 1 条「无依据不播」、进 `notPlayed` 出声），见下一条的 1");
// 改后
                    Check(udRows == 2,
                          $"★ W63/技能动画：原版 `UnitDamaged(60)` 在卡池里 **2 行**（`BL20` + `Pray Enchantment`，实得 {udRows}）"
                        + "—— 🔴 2026-10-23 Trait 权威化后另外 3 条已剔掉（2 条「删重复」· 1 条「无依据不播」、进 `notPlayed` 出声）；"
                        + "🧨 2026-10-30 `B101_F1`：附魔那条**是合法原版数据**（`B101_R4` §6·8：走 `AbilityLogic.cardAnimations`、演在载体单位身上），⛔ 别再按卡型排掉");
```

**(c) `:13449-13455`：**

```csharp
// 改前
                    Check(VfxMap.CardTableUnitAttackEntries == 12 && VfxMap.CardTableUnitDamagedEntries == 1
                          && VfxMap.CardTableUnitAnimEntries == 13,
                          $"★ W63/技能动画：运行期两桶 = **12 + 1 = 13 条**（实得 {VfxMap.CardTableUnitAttackEntries} + "
// 改后
                    Check(VfxMap.CardTableUnitAttackEntries == 12 && VfxMap.CardTableUnitDamagedEntries == 2
                          && VfxMap.CardTableUnitAnimEntries == 14,
                          $"★ W63/技能动画：运行期两桶 = **12 + 2 = 14 条**（实得 {VfxMap.CardTableUnitAttackEntries} + "
```

（该 `Check` 后面那三句说明文字**不用改**：它们讲的是「12 不是 13 · `SAU31` 的 `Wait_1s`」那件事，与附魔无关。）

**(d) `:13467-13468`：**

```csharp
// 改前
                        Check(udCards.Length == 1 && udHit == 1,
                              $"★ W63/技能动画：1 张 `UnitDamaged` 卡（`BL20`；2026-10-23 权威化后只剩这一条）**按卡 id 命中**（{udHit}/{udCards.Length}）");
// 改后
                        Check(udCards.Length == 2 && udHit == 2,
                              $"★ W63/技能动画：2 张 `UnitDamaged` 卡（`BL20` · `Pray Enchantment`）**按卡 id 命中**（{udHit}/{udCards.Length}）");
```

> ⚠️ **两处不变**：`:13443-13445` 的 `uaRows == 13` 与 `:13464-13466` 的 `uaCards.Length == 11`（`UnitAttack` 那条线未受影响）。

### 4·2 `CardPresentation/Core/VfxMap.cs:3530`（**行为**，本件只改了文案）

```csharp
// 改前（现读）
                    if (string.Equals(card.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;   // 🔴 2026-10-30 B101_F1：…（本件改过的文案）
// 改后
                    （整行删掉）
```

**为什么必须删**：`SelfCheckUnitSkillAnims` 第 ④ 支是 `CardTableUnitDamagedEntries(2) == expectedRows(带 skip 时 = 1)` ⇒ **必红**；
删掉后 `expectedRows = 2`（`BL20` + `Pray Enchantment` 各 1 行，都有 prefab）= 桶里 2 条 ⇒ 绿。
第 ② 支会把这张附魔卡纳入逐卡比对：`want = ["Pray_Exit"]`、`got = ResolveAllTimed(UnitDamaged, …, perCardOnly: true)`
（静态推演同名 1 件）；第 ③ 支问的是**旧桶 `hit` 还剩不剩**，这条已被 `LoadTableFromJson` 搬走 ⇒ 0。**⚠️ 这三句是静态推演，没跑 Unity 验**（见 §六）。

**`:3409` 那一处（`SelfCheckRoutedTriggers`）建议**：**保持行为不变**（`UnitDamaged` 不在 `MustRouteTriggerNames` 里 ⇒
它对这张卡是空转，删不删都不影响结果），只改文案即可（本件已改）。

### 4·3 `CardPresentation/Editor/B100F11_Assert.cs` —— **② 整节要撤**（本件没动，它在宿主侧）

| 位置 | 现状 | 为什么红 / 建议 |
|---|---|---|
| `:96` | `SectionB();` | 建议删掉这一行 |
| `:179-307` | `static void SectionB()` 整个方法 | 建议整段删（它的前提「附魔不是单位」已被 `B101_R4` §6·8 推翻）；其中 **B1** 会随 §4·2 一起转绿，但 **B2 / B4 / B5 / B5-b 是「钉着 F11 结论」的断言**，数据恢复后必然红 |
| 逐条对照 | B2：`CardTableUnitDamagedEntries == rowsWithoutEnch`（2 vs 1）· B4：桶里**没有**附魔卡 · B5：`hit` 桶里**没有**附魔行 · B5-b：`notPlayed[]` 里**有** `enchantment-not-unit` | 三条都是「F11 结论」本身，撤销后语义反转 |
| 文件头 `:21-30` | 那段 ② 的论证（「判据已经写死在 C# 里…」） | 按铁律 5 就地订正成「**已作废**，见 `B101_R4` §6·8」 |
| **保留** | `SectionA()`（`DiscardReveal` / `StealReveal` 的时长登记） | 与附魔无关，**仍然有效、⛔ 别一起删** |

### 4·4 `CardPresentation/Editor/BattleVfxAssertE3.cs`（可选，只影响文案）

- `:110-118` 的 `D3` 只断 `VfxMap.SelfCheckUnitSkillAnims().Length == 0` ⇒ **不用改数值**，随 §4·2 自动转绿。
- `:103` 的注释「那 13 行（`UnitAttack` 12 + `UnitDamaged` 1）」⇒ 改成 **14 行（12 + 2）**（`Pray Enchantment` 那条）。
- ⚠️ `:114` 那句日志里的 `CardTableUnitDamagedEntries` 是**读出来的**，不写死 ⇒ 不用改。

---

## 五、判别式（怎么做会让「新尺子」红）

| # | 做法 | 哪条红 | 说明 |
|---|---|---|---|
| 1 | 把 `strip_enchantment_unit_anims` 加回来（或按 `type == "enchantment"` 再过滤一次） | 数据：`byCard[…].hit` 又没了 · 断言：`BattleScene` 的 `CardTableUnitDamagedEntries == 2` 变 1 ⇒ 红 | 这是**本件撤销的那一层**，回来就红 |
| 2 | 只删 `VfxMap` 的 skip、不改 `BattleScene` 的数值 | `BattleScene` `udRows == 2` 绿、但 `CardTableUnitDamagedEntries == 1` 红（实得 2） | 两处口径**必须同批改** |
| 3 | 只改 `BattleScene` 的数值、不删 `VfxMap` 的 skip | `VfxMap.SelfCheckUnitSkillAnims()` 第 ④ 支红（`2 != 1`）⇒ `BattleVfxAssertE3.D3` 与 `B100F11_Assert.B1` 一起红 | 同理 |
| 4 | 把这条**搬去** `EvtKind.EnchantmentRemoved` 那一拍（而不是留在 `hit`） | 数据：`byCard[…].hit` 空、多一个 `enchantment_removed` 桶 ⇒ 上面三条计数全变；且**今天没有任何「该播哪件」的原版判据** | ⛔ 拿猜测顶替判据 |
| 5 | 把尺子改成「按 `type == "unit"` 筛」而不是删 skip | `expectedRows` 变 1（附魔被排掉）⇒ 又回到 §4·2 那条红 | 「按单位卡筛」指的是**这条动画演的是单位**，不是「只数 `type == unit` 的卡」 |
| 6 | 产物手工改（不走生成器） | `--check` 两份都会报不同 | `⛔ 不手改产物` |

---

## 六、没查清的部分 / 如实记

1. 🔴 **VfxMap 去掉 skip 之后的三支后果是静态推演，没跑 Unity 验**（本件禁令：⛔ 不跑 Unity）。
   推演依据：`AnimTimes` 现读 `UnitDamaged` 共 2 行（`BL20 Buff_DiabolicStrength` · `Pray Enchantment Pray_Exit`，都有 `prefab`），
   `want = ["Pray_Exit"]` 与桶里那件同名 ⇒ ②支应过；`hit` 桶被搬空 ⇒ ③支应过。**如果调度台跑 `BattleScene.Run` 时 ②/③ 报出别的名字，
   那份报错就是真判据**（别按本段的推演去改数据）。
2. ⚠️ **`Enchantments.Matching` 认不出这条 ability** —— 这是 R4 §6·8 里的判断（`desc` 为空、能力在 `chosenAbility`），
   **本件没有独立复核**（不在白名单、也不需要：本件只恢复数据）。⇒ 「今天播不出来」这句是**转述 R4**，不是本件结论。
3. ⚠️ **`B100_F11` 报告 §五·4 提议的「搬去 `EnchantmentRemoved` 那一拍」仍然是「没有判据」** —— 本件沿用了 R4 §6·8 的处置
   （留 `hit` 桶 = 合法数据、空转），**没有**去查 `ResolveRemoveEffect` 那条链；如果调度台要把那一拍真做出来，那是**另一条活**。
4. 📌 **顺手发现（只报不改）**：
   - `VfxMap.cs:1261` 附近那句汇总日志把 `notPlayed` 一律叫「⛔ 无权威依据**不播** N 条」——
     本件撤销后 `outcome` 只剩 `duplicate` / `unbacked` 两种，那句措辞**又变回成立的**了（`B100_F11` 报告 §五·2 提的那个问题自动消失）。**未改**。
   - `VfxMap.cs:3409` 与 `:3530` 是**同一句话的两份副本**（同一段 `continue`）—— `B100_F11` 报告只提了后者。
     本件把两处文案一起改了（铁律 5），但**行为都没动**。
   - `BattleScene.cs:13446-13448` 的旧文案里「另外 3 条已剔掉」与本件无关（那是 2026-10-23 Trait 权威化那一批），
     §4·1(b) 的改后文案**保留了它**，只补了 `B101_F1` 那半句。
