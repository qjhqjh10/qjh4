# B143 · `B141-ROWABLE-WHITELIST` 施工前核对（只读 · 2026-11-05 · 执行写手 `B143-W4`）

> 用途：同步点跟 `play check` 重钉一起做这条时，**照本文件直接下手**。
> ⛔ 本文件是核对单：**没改 `RuleCore.cs`、没跑 `play check`、没动账**。
> 数据口径：`RuleEngine/Resources/cards_engine.json`（`count` 现读 = **1,142**，mtime 2026-11-04 22:29）。
> 代码口径：`RuleEngine/Core/RuleCore.cs` 现读 **18,866 行**（LF 18,866 · CRLF 0 · 裸 CR 0）。

---

## 一、现读行号 + 原文（**行号未漂**，仍是 E56 写的那两处）

| 站点 | 现读行号 | 当前行原文 | E56 写的 | 是否漂 |
|---|---|---|---|---|
| `PublishRowAbilityTargetTable` 白名单 | **`RuleCore.cs:18581`** | `            if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) return;` | `:18581` | **否**（逐字同） |
| 紧邻的 `MaxTargets` 闸（**保持不动**） | `:18582` | `            if (ta != 0 && set.MaxTargets >= 1) return;` | — | — |
| `TryRowOwnAbilityTargets` 白名单 | **`RuleCore.cs:18778`** | `            if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) return false;` | `:18778` | **否**（逐字同） |
| 紧邻的 `MaxTargets` 闸（**保持不动**） | `:18779` | `            if (ta != 0 && set.MaxTargets >= 1) return false;` | — | — |

**可直接粘贴的补丁（两行、零行数变化）**：

```csharp
// :18581（PublishRowAbilityTargetTable）
            if (ta != 0 && ta != 10 && ta != 20 && ta != 30 && ta != 110 && ta != 140 && ta != 150) return;

// :18778（TryRowOwnAbilityTargets）
            if (ta != 0 && ta != 10 && ta != 20 && ta != 30 && ta != 110 && ta != 140 && ta != 150) return false;
```

⚠️ 两处**必须同批**；`MaxTargets` 两道闸**不动**；改完 `RuleCore.cs` 仍必须是 **18,866 行**。

### 一·1 🔴 第三处「同形白名单」——E56 没点名，**建议本次不放宽**

| 站点 | 现读行号 | 原文 |
|---|---|---|
| `TriggerAbilityTargetCriteriaByNumber`（触发 / 战术正文路） | **`RuleCore.cs:18452`** | `                if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) continue;   // ① + ②（见类尾）…` |

- 它和那两处**长得一样、消费者不同**：这三处的分工是 `:18452` = 触发/战术正文（`AttachTriggerAbilityTargets`），`:18581` = 主动能力**效果层表**（每行重挂），`:18778` = 主动能力**事件表**（每行并进 `BattleEvent.AbilityTargetList`）。前两处的唯一调用点是 `RuleCore.cs:15632` 的主动能力行循环。
- 该文件 `:18412-18424` 已写明**为什么触发路故意不收 20/30**：① 单目标行由事件的 `TargetPlayer/TargetSlot` 已表达；② `ta=30` + 随机 `targetSelectMethod` 会**消费 `ctx.GetRandomInt`**，而触发/战术正文走文本 op 管线、不吃这张表 ⇒ 补抽 = 凭空多烧一个随机数。
- ⇒ **本次只改 `:18581` / `:18778`**；`:18452` 保持。⚠️ 顺带：`:18759` 的 doc 写「白名单只此一处」**不准确**（实际 3 处），建议改那一句时一并订正（本次没改）。

---

## 二、影响面复算（现读数据，与 E56 完全一致）

口径：逐卡遍历 `activeAbilityTargets[]`（每项 `{ab, lg, criteria{targetsAffected, maxTargets, …}}`）；
「可发布」= `ta ∈ 白名单` **且** 通过第二道闸（`ta == 0 || maxTargets < 1`）。

| 口径 | 行数 | 卡数 | 按 `ta` 分布（行） |
|---|---:|---:|---|
| **当前可发布**（`{0,10,110,140,150}`） | **47** | **43** | `0`:17 · `10`:26 · `110`:3 · `150`:1 |
| **本次新增**（`20/30` 且 `maxTargets<1`） | **118** | **83** | `20`:49 · `30`:69 |
| 改后合计 | **165** | **118** | `0`:17 · `10`:26 · `20`:49 · `30`:69 · `110`:3 · `150`:1 |

- **被 `MaxTargets ≥ 1` 挡掉的只有 2 行**（与 E56 一致）：`DA13 Librarian(ab -1, lg 1)` · `SW9 Fenrisian Wolf(ab -1, lg 0)`。
- 新增行的**每卡分布**：`1 行/卡` × **52 张** · `2 行/卡` × **27 张** · `3 行/卡` × **4 张**（52+27+4 = 83）。
- 随机流：`ta=20 self` / `ta=30 target` 建池**不消费随机数**（`AbilityLogic__GetTargets.c:184-207`）。⚠️ 但 `ta=30` 且 `chosen == null` 时 `AbilityTargetListFromCriteria` 建出**空表** ⇒ `PublishRowAbilityTargetTable` 的 `units.Count == 0` 直接 return、`TryRowOwnAbilityTargets` 返 `false` ⇒ **不写表 / 不并事件**（与改前同向，属天然降险）。

---

## 三、必须同批改的断言（`B140_WA_Assert` C4）

现读 `RuleEngine/Editor/B140_WA_Assert.cs:202-205`：

```csharp
// :202
RuleCore.PublishRowAbilityTargetTableForTest(ctx2, p2, carrier, chosenNull, new[] { 30, 20, 1, CardDef.ActiveAbilityObject });
// :203-205
CheckTrue(ctx2.AbilityTargetTable != null && ctx2.AbilityTargetTable.Count == expectFriendlyMinions,
          "C4 ★ 行 `ta=20 self` **不顶掉**上一张表（白名单 = `{0,10,110,140,150}` ∧ 不限张数；"
        + "去掉白名单 ⇒ 这里变 1 ⇒ 红）");
```

- **现在期望**：`Count == expectFriendlyMinions`（= **3**，即上一行 `ta=10` 留下的表）。
- **fixture**：`AbilityRowCard` 的 `lg=1` 行 = `TargetsAffected = 20, MaxTargets = 0`（`:244-248`）；`ta=20 self` 建池 = `[source]`（`RuleCore.cs:17975-17977`）。
- **按补丁改后**：`ta=20` 进白名单且 `maxTargets=0` ⇒ **会**重挂本行表 ⇒ `Count == 1`（施放者本人），表主人仍是 `carrier`。
- ⇒ **改后**（建议替换 `:203-205`，行数不变）：

```csharp
CheckTrue(ctx2.AbilityTargetTable != null && ctx2.AbilityTargetTable.Count == 1,
          "C4 ★ 行 `ta=20 self` **会**顶掉上一张表（白名单 = `{0,10,20,30,110,140,150}` ∧ 不限张数）⇒ "
        + "表 = 这一行自己的目标（self = 施放者本人 **1** 张）；🧨 把 20/30 从白名单删掉 ⇒ 回到 3 ⇒ 红");
```
（可选加强：再断 `ctx2.AbilityTargetTable[0].CardId == carrier.Card.Name` —— 证「是自己的目标、不是沿用上一张」。）

- `B140_WA_Assert` 的**行号不被账引用**（账 `战斗逻辑驱动.tsv:80` 只在备注里提文件名）⇒ 改这一段不影响账。
- ⚠️ 其他组不受影响：C3 用 `ta=10`；C5/C6 走触发路（`:18452` 没改）；A/B/D 组不碰这两处。

---

## 四、受影响最大的 5 张（按「本次新增的可发布行数」，同数按 id 序）

| # | 卡 id | 卡名 | 新增行 | 组成（ta, maxTargets） | 该卡 `activeAbilityTargets` 总行数 |
|---:|---|---|---:|---|---:|
| 1 | `SOR5` | Battle Sister | **3** | 20/0 ×3 | 3 |
| 2 | `SOR25` | Imagifier | **3** | 30/0 ×2 + 20/0 ×1 | 3 |
| 3 | `DA20` | Master Lazarus | **3** | 30/0 ×2 + 20/0 ×1 | 3 |
| 4 | `DA18` | Inner Circle Companion | **3** | 20/0 ×3 | 3 |
| 5 | `UM96` | Chapter Champion | **2** | 20/0 ×2 | 2 |

> 同为 2 行的还有 `UM95 Attack Bike` · `SOR72 Castigator` · `SOR71 Adelaide the Serene` 等 27 张，未单列。
> 「受影响」= 这些行从「不发布」变成「每行各挂自己的表 / 并进事件表」；其中 **3 行的 4 张**是变化面最大的（同一张卡三个行各自重挂）。

---

## 五、还差什么 / 施工顺序与风险

1. **顺序（硬）**：① 改 `:18581` + `:18778`（两处原子）→ ② 同批改 `B140_WA_Assert` C4（§三）→ ③ `typecheck` 0/0 → ④ 账闸 `check_ledger_points.py`（两处零行数变化 ⇒ 应 **0 漂移**）→ ⑤ 在**没有引擎写手在飞**时 `play check` 并**重钉基线**。
2. **`play check` 必须留到最后**：现在有引擎写手在飞，跑了也不算数（E56 §五那次 14 张差异就是别人在飞造成的）。重钉时按 `报告/B142_*.md` 逐条认领差异，⛔ 别当成本条的回归。
3. **影响面 83 张 > 简报的「30 张停手线」**：E56 正是按这条闸停的手。同步点要做 = **显式接受这个面**，并留一句「为什么这次可以越线」（判据已定、两处原子、随机流零消费）。
4. **第三处白名单 `:18452` 本次不动**（§一·1）。若将来要把触发路也放宽，**先解决随机消费**（`ta=30` + `targetSelectMethod` 随机档），否则会改全池随机序列。
5. **`ta=30` 的 `chosen == null` 面**：产品路径里 `chosen` 通常是玩家点的目标；夹具 / AI 某些分支可能为 null ⇒ 那一行**空表 → no-op**（不是错，是原版建池为空）。首跑时别把「表没变」误判成「没生效」，先看 `chosen`。
6. **同族但不在本条的**：`B141_W77_Assert` 的 GroupC（死窗口三面，E56 §6.1）与本条无关，⛔ 别混进同一次改动。
7. **行号纪律**：`RuleCore.cs` 有 500+ 处账按行号引用；两处只能**就地替换**（各 1 行 → 各 1 行），⛔ 别插行。改完现读仍是 **18,866 行**。
