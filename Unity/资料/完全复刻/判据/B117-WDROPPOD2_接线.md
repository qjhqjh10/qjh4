# B117-WDROPPOD2 · `B117-47` 的 4 条 `RuleCore.cs` 接线补丁（已落地）

> 执行代理 `B117-WDROPPOD2` · 2026-11-02（第一百一十七会话 · 引擎线）
> 上游：`判据/B117-WDROPPOD_dropPod支.md` §三（补丁正本）· 活状态表 **`B117-47`**
> 结论一句话：**4 条补丁全部落地，全部零行数、行号零漂移；`typecheck` 0/0；两条探针读数与补丁前逐句一致（唯一差异面仍是 `UM64`，与 `B117-10` 相关、非本批）。**

---

## 一、四条各自贴在哪（现读行号 + 锚点原文）

⚠️ 简报给的行号**实测未漂**（`WREPLAY2` / `WMULL` 的改动不在 3714–3816 这几段）。全部按**引号原文**定位后才落的笔。

### 补丁 1 — `RuleEngine/Core/RuleCore.cs:3714`（签名，同一行改）

锚点原文：`        public static void ActivateTraitsOnSummonOrEnchantment(UnitState u)`

现读：

```csharp
        public static void ActivateTraitsOnSummonOrEnchantment(UnitState u, bool onSummon = true)
```

### 补丁 2 — `RuleEngine/Core/RuleCore.cs:3725`（`ferocity` if 的收尾 `}`，行尾追加）

锚点原文（`ferocity`/`oath` 那个 if 的最后三行）：

```csharp
                u.CannotAttackThisTurn = true;
            }
        }
```

现读 `:3725`：**代码在注释之前**（⛔ 不是落进 `//` 里 —— 见 `CLAUDE.md` 那条「同一物理行追加」的坑）：

```csharp
            } if (onSummon) u.ApplyDropPodOnSummon();   // 🔴 2026-11-02（`B117_WDROPPOD` · 活状态表 `B117-47`）：…
```

### 补丁 3 — `RuleEngine/Core/RuleCore.cs:3816`（`ApplyDeployTurnState` 里那句）

锚点原文：`            RecomputeActAndAttack(u);`（该串在 `RuleCore.cs` 里出现 **4 次** ⇒ 用上一行注释尾 `⇒ 所以本入口的行为逐字不变。` 做唯一化，⛔ 没按行号盲贴）

现读：

```csharp
            RecomputeActAndAttack(u); u.ApplyDropPodOnSummon();   // 🔴 2026-11-02（`B117_WDROPPOD`）：…
```

### 补丁 4 — `RuleEngine/Core/Enchantment.cs:1016`（同一行改；与补丁 1 同批落地）

锚点原文：`            RuleCore.ActivateTraitsOnSummonOrEnchantment(u);`

现读：

```csharp
            RuleCore.ActivateTraitsOnSummonOrEnchantment(u, onSummon: false);   // 原版 `CardScript__AddEnchantment.c:91` 传的是 0 ⇒ dropPod 支**不进**
```

---

## 二、零行数 / 行尾守恒（自检读数）

| 检查 | 读数 |
|---|---|
| `RuleCore.cs` 行数 | **18,406**（改前改后相同；`git diff --numstat` = **26 / 26** 增删相等，那 26 行里含别的写手的既有未提交改动） |
| `Enchantment.cs` 行数 | **2,118**（`git diff --numstat` = **1 / 1**，正是补丁 4） |
| 行尾 | 两个文件均 **纯 LF**：`crlf = 0` · `bare_cr = 0`（`io.open(p,'rb')` 读） |
| 四条现读行号 | **3714 / 3725 / 3816 / 1016** —— 与补丁正本给的行号**逐个相同**（零漂移，账落点不受影响） |

---

## 三、验证读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_wdroppod2 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh check` | **rc=1 · 哨兵失败 1 条**；`解析差异 1 行` = **`UM64｜Angels of Death`** ⇒ 与补丁前**同一张、同一条**，本批零引擎影响 |
| `bash 工具/ruleprobe.sh play check` | **rc=1**；①-a / ①-b 各「**1 张卡变了**」= **`UM64`**（`playRc 18 → 0`），①-b 备注「电池没起步（主档同样打不出去 `rc=ErrNoTargetAvailable`）」⇒ **预期差异面仍只有 `UM64`**，与 `B117-10`（`B117-WGB`）同源 |
| 重钉基线 | ⛔ **没有**（简报要求只报读数） |

⚠️ 两条 `rc=1` 都不是本批引入：本批的 dropPod 支**池内 0 张卡带该 trait**（`cards_engine.json` 1127 张现扫）⇒ 结构上不可能改任何一张卡的 play 结果。

---

## 四、W1 / W1′ / W3 / W3′ 四条「接线闸」现在应该能过（给调度台）

**文件**：`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_B117DROPPOD.cs` · `B117DROPPOD_Assert.SectionW_Wiring()`（`:163-213`）

| 断言 | 位置 | 现在为什么能过 |
|---|---|---|
| **W1**（池子 0 → 2） | `:172-173` | 补丁 3 把 `u.ApplyDropPodOnSummon()` 接进了 `ApplyDeployTurnState`，而 W1 正是调 `RuleCore.ApplyDeployTurnState(u)`（`:171`） |
| **W1′**（舱体攻击 7 → 0） | `:174` | 同上（`ApplyDropPodOnSummon` 同时写两格） |
| **W3**（`onSummon:true` 池子 0 → 2） | `:191-192` | 补丁 1 加默认参数、补丁 2 在方法尾接 `if (onSummon) u.ApplyDropPodOnSummon();`；W3 调的是 `RuleCore.ActivateTraitsOnSummonOrEnchantment(u)`（`:190`，吃默认 `true`） |
| **W3′**（舱体攻击 7 → 0） | `:193` | 同上 |

**另外五条现在也绿**（本批没碰）：`W2` / `W2′`（不带 `dropPod` ⇒ 池子 0 / 攻击 7 不动，`:182-183`）· `W4` / `W4′` / `W4″`（`fast` 抬 `Exhausted`、`ferocity` 抬 `Exhausted` + 压 `CannotAttackThisTurn`，`:201-207`）—— 补丁 2 那一句**追加在 `ferocity` if 之后**，没改那三句本身。

### 🔴 但宿主还没接线 ⇒ 本文件现在【一条都不会跑】

`B117DROPPOD_Assert` 在 `RuleEngineTest.cs` 里**零引用**（现 grep 全库只有它自己那份定义与文件头注释）。
请 `WWIREA` 在 `RuleEngineTest.cs` **任一条既有 `Guard(...)` 的同一物理行**追加（零行数）：

```csharp
Guard("B117DROPPOD_Assert", B117DROPPOD_Assert.Run); _pass += B117DROPPOD_Assert.Pass; _fail += B117DROPPOD_Assert.Fail;
```

⚠️ 同一个坑已在 `CLAUDE.md` 记过：**追加到某一行时，先看该行第一个 `//` 在哪** —— 落进 `//` 之后会「看着接上了、`typecheck` 绿、断言一条不跑」。

---

## 五、顺手发现（只报不改）

1. **`W5`（附魔路负例）现在可以写了** —— 补丁 1 已把 `(UnitState u, bool onSummon = true)` 这个重载加进来，
   `RuleCore.ActivateTraitsOnSummonOrEnchantment(u, onSummon: false)` 现在**编得过**。
   建议在 `SectionW_Wiring()` 里补一条：「附魔路（`onSummon:false`）池子**仍是** 0 / 攻击**仍是** 7」——
   那正是补丁 4 的验收闸。⚠️ 该文件不归本代理，交给调度台排写手。
2. **`RuleCore.cs:3711` 的 doc 已经过期**（本批没动它 —— 一动就不是零行数了）：
   现读「**调用点**：`TutorialScript.PlaceTroops`（唯一）」，而实际至少还有 `RuleCore.cs:655`（督军开局）
   也在调它。建议该文件的写手顺手把那半句改成「`TutorialScript.PlaceTroops` / `RuleCore.cs:655`（督军开局）」
   —— 纯注释订正，零行数。
3. `B117-WDROPPOD_dropPod支.md` §二 说「`SetupDropPodStats()` 用 `Has` 而原版用 `HasCurrentTrait`」
   （`UnitState.cs:3907`）—— 本代理**没动** `UnitState.cs`，那条仍开着，需要 `UnitState.cs` 的写手做一行替换。

---

## 六、改动的文件（全部在简报白名单内）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/RuleCore.cs` | 3 处零行数（`:3714` 签名 · `:3725` 行尾追加 · `:3816` 行尾追加） |
| `RuleEngine/Core/Enchantment.cs` | 1 处零行数（`:1016` 传 `onSummon: false`） |
| `判据/B117-WDROPPOD2_接线.md` | 本文件 |

⛔ 没动 `Battle/BattleDriver.cs` · `Core/CardView.cs` · `Editor/*Scene.cs` · `UnitState.cs` · 账主表 · git · `d:/2`。
✅ `RuleCore.cs` 里别的写手的两段（448-660 / 2377-2401）**没碰**。
