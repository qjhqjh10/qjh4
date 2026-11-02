# B117-WDROPPOD · `ActivateTraitsOnSummonOrEnchantment` 的 `0xe6`（dropPod）支

> 执行代理 `B117_WDROPPOD` · 2026-11-02（第一百一十七会话 · 引擎线）· 活状态表 **`B117-47`**
> 产出：新建 `RuleEngine/Core/UnitStatePartial_B117DROPPOD.cs` ＋ 新建 `RuleEngine/Editor/RuleEngineTest_B117DROPPOD.cs` ＋ 本文件
> 结论一句话：**语义已查清、执行体已落地（休眠，池内 0 张）；真正的接线在 `RuleCore.cs`（归 `WREPLAY2`）⇒ 交回 3 处零行数补丁，见 §三。**

---

## 一、原版语义（逐句，判据 = `d:/2/tools/decomp_full/CardScript__ActivateTraitsOnSummonOrEnchantment.c` 全文 145 行）

方法签名：`ActivateTraitsOnSummonOrEnchantment(this /*param_1*/, char onSummon /*param_2*/, undefined1 playSound /*param_3*/)`。

| 行 | 原文（编译后形状） | 语义 |
|---|---|---|
| `:42-43` | `iVar1 = *(int *)(param_1 + 0x228); if (iVar1==2 \|\| iVar1==0xf \|\| iVar1==3 \|\| iVar1==0x11)` | 外层闸 = **`cardState(+0x228) ∈ {2 在场, 15 变身中, 3 攻击中, 17 瞄准攻击中}`**（⛔ 不是 cardType） |
| `:48-58` | 取 `CurrentTraits(+0x128)` 的枚举器逐条 `MoveNext` | 遍历**当前 trait 表** |
| `:67-69` | `BattleCardUI.DisplayStatusAnim(card.battleCardUI(+0x290), traitId, playSound, 0, index, …)` | 每条 trait 刷一次状态图标动画（**表现层**，我们无对应层） |
| `:70-74` | `iVar1 = *(int *)(lVar6 + 0x18); cVar7 = 0; if (iVar1 == 0xe6) cVar7 = param_2;` | **本条的判定**：只有「这一条 trait 是 `dropPod(0xe6)`」**且**「`onSummon != 0`」才把 `cVar7` 置真 |
| `:75-101` | `if (cVar7 == 0) { … }` —— `0x28`/`0x1cc` ⇒ `+0x230 = 1`；`0x4f1`/`0x4fb` ⇒ `+0x230 = 1` **且** `+0x23c = 0` | 旧支（`fast`/`flank`/`ferocity`/`oath`）—— 与既有实现同形，**不含 `0xe6`** |
| `:102-135` | `else { … }` = **本批补的新支** | 见下 |

**新支（`:102-135`）逐句：**

1. `:106` `uVar2 = *(undefined4 *)(*(longlong *)(DAT_18427be00 + 0xb8) + 0x1fc);`
   = 读 **`GameStaticData.dropPodMaxHealth`**（`+0x1FC`，值 **2**）。
2. `:116-120` 把这 20 字节写进 `param_1 + 0xcc / +0xd0 / +0xd4 / +0xd8 / +0xdc`
   = **`EntityScript.currentDropPodHealth`**（`EntityScript__get_currentDropPodHealth.c:8-12` 读的正是 `+0xcc` 那一组）。
3. `:123` 读 **`GameStaticData.dropPodAttack`**（`+0x1F8`，值 **0**）。
4. `:129-133` 写 `param_1 + 0xe0 … +0xf0` = **`currentDropPodAttack`**。
5. `:134` `CardScript__UpdateFigures(param_1, 0)` —— 刷显示数值（**表现层**，我们无对应层）。

常量真值出处：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:2404-2405`（`dropPodAttack = 0; dropPodMaxHealth = 2;`），
字段偏移 `+0x1F8/+0x1FC` 见 `dump.cs:119543-119544`。

**它【不】做的事（⛔ 别顺手补）：**
- **不摘** `dropPod` 这个 trait（舱体还在身上，开舱才摘）；
- **不动** `canAct(+0x230)` / `canAttack(+0x23C)`；
- **不发** `Landing(0x1b8)` —— 开舱仍然只走 `RuleCore.OpenDropPod`（`BeginTurn` / `DeclareAttack` 两个调用点）。

**与「旧支」的三个差别（这是任务问的那三问）：**

| 问 | 旧支（fast/flank/ferocity/oath） | 新支（dropPod） |
|---|---|---|
| **什么时候触发** | `onSummon` **不限**（召唤与附魔两条路都跑） | **只在 `onSummon != 0`**（附魔路 `param_2 == 0` ⇒ 什么都不做） |
| **对什么对象** | 看**当前 trait 表**里有没有那四个词 | 看当前 trait 表里有没有 `0xe6`（同一次遍历，逐条判） |
| **做什么** | 写两个**布尔位**（能不能动 / 能不能攻击） | 写两个**整数格**（独立血池 / 舱体攻击），来源是**全局常量**（2 / 0），与卡面数值无关 |

---

## 二、我们补在哪、怎么补

**执行体 = 新建 `Unity/MyGame/Assets/RuleEngine/Core/UnitStatePartial_B117DROPPOD.cs`**
（`partial class UnitState`；**不碰 `UnitState.cs`** —— 那个文件有 43 条按行号引用的账落点）：

```csharp
public bool ApplyDropPodOnSummon()
{
    if (!HasCurrentTrait(KeywordTable.DropPod)) return false;
    CurrentDropPodHealth = DropPodMaxHealthDefault;   // 2（原版 .c:106 → :116-120）
    CurrentDropPodAttack = DropPodAttackDefault;      // 0（原版 .c:123 → :129-133）
    return true;
}
```

- 判据用 **`HasCurrentTrait`**（原版遍历的就是 `CurrentTraits(+0x128)`），⛔ 不是 `Has`。
- 两格字段**早就存在**（`UnitState.CurrentDropPodHealth/Attack`，`B97_W22` 落的），常量也早就在
  （`DropPodMaxHealthDefault = 2` / `DropPodAttackDefault = 0`，出处同上）—— **本批补的是「召唤那一刻刷一次」这个动作本身**。
- 返回值 `bool` = 有没有命中这一支（给断言与将来诊断用；原版无返回值）。

**与另外两个写点的分工（原版本来就有三处，⛔ 别合并）：**

| 写点 | 原版位置 | 我们的对应物 | 闸 |
|---|---|---|---|
| 建卡 | `CardSetup.c:158-200` | `UnitState.SetupDropPodStats()`（ctor 里调） | **`cardType == 10`（督军）** ∧ `HasCurrentTrait(0xe6)` |
| 回手/重置 | `ResetToValuesInHand.c:110-133` | `UnitState.cs:2675` | **无条件** |
| **召唤**（本批） | `ActivateTraitsOnSummonOrEnchantment.c:102-135` | **本文件新增** | `cardState ∈ {2,3,15,17}` ∧ `HasCurrentTrait(0xe6)` ∧ **`onSummon`** |

🔴 关键推论（断言 D3 钉住）：**普通单位（非督军）在构造时池子是 0/0**（`CardSetup` 那一格有 `cardType==10` 闸）
⇒ **本支才是它们的唯一初始化口**。所以这不是「重复赋值」，是原版设计。

**接线（本代理没做，见 §三）**：调用点必须在 `RuleCore.cs`，而该文件本波由 `WREPLAY2` 占用。
⛔ **没接线 ⇒ 本文件永远不被生产代码调用** ⇒ 断言 **W 段会一直红**（那就是它存在的意义）。

---

## 三、接线补丁（交回调度台转 `WREPLAY2`）—— 🔴 全部**零行数**

> ⚠️ `RuleCore.cs` 有 400+ 条按行号引用的账落点 ⇒ **⛔ 一行都不许新增**，三处全部**就地追加到已有物理行**。

### 补丁 1/3 —— `RuleCore.cs:3714`（签名，同一行改）

```diff
-        public static void ActivateTraitsOnSummonOrEnchantment(UnitState u)
+        public static void ActivateTraitsOnSummonOrEnchantment(UnitState u, bool onSummon = true)
```

### 补丁 2/3 —— `RuleCore.cs:3725`（方法体末尾，追加到那条 `}` 行上）

现状（注意上下文：这是**那个方法**的最后两行）：

```csharp
                u.CannotAttackThisTurn = true;
            }
        }
```

改成（只动中间那行，行数不变）：

```csharp
                u.CannotAttackThisTurn = true;
            } if (onSummon) u.ApplyDropPodOnSummon();   // 🔴 2026-11-02（`B117_WDROPPOD` · 活状态表 `B117-47`）：原版 `.c:70-78` 的 `0xe6`（dropPod）支 —— 遍历当前 trait 表时，**只有**「`trait == dropPod` ∧ `onSummon != 0`」才走 `.c:102-135`（`+0xcc/+0xe0` ← `GameStaticData` 的 2 / 0）。附魔那条路传 0 ⇒ **不进**（见 `Enchantment.cs` 同口径注）。⚠️ 零行数：本文件 400+ 处账落点按行号引。
        }
```

### 补丁 3/3 —— `RuleCore.cs` `ApplyDeployTurnState` 里那一行（现读 `:3816`）

```diff
-            RecomputeActAndAttack(u);
+            RecomputeActAndAttack(u); u.ApplyDropPodOnSummon();   // 🔴 2026-11-02（`B117_WDROPPOD`）：原版 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:69-71`（写 `+0x58`）→ `UpdateSummonSicknessAnims().ActivateMinion()`（= 上面那一跳 `RecomputeActAndAttack`）→ **`ActivateTraitsOnSummonOrEnchantment(card, 1, 1, 0)`**（= 本句，`onSummon = 1`）。⚠️ 零行数。
```

### 补丁 4/4 —— `Enchantment.cs:1016`（同一行改；与补丁 1 **必须同批落地**，否则编不过）

```diff
-            RuleCore.ActivateTraitsOnSummonOrEnchantment(u);
+            RuleCore.ActivateTraitsOnSummonOrEnchantment(u, onSummon: false);   // 原版 `CardScript__AddEnchantment.c:91` 传的是 0 ⇒ dropPod 支**不进**
```

> ✅ **调用链自证**：
> `PlayCard` / `DeployFree` / `DoReanimate` / `TransformUnit` 四条部署入口 **全部**经过 `ApplyDeployTurnState`
> （`RuleCore.cs:4159` · `:4971` · `EffectResolver.cs:15869` · `TransformUnit_B95A3.cs:164`）⇒ 补丁 3 一处覆盖四条；
> 起始兵 `TutorialScript.PlaceTroops`（`:1568`）与督军开局 `RuleCore.cs:655` 走的是
> `ActivateTraitsOnSummonOrEnchantment`（补丁 1+2 覆盖）；附魔路 `Enchantment.cs:1016` 由补丁 4 显式传 `false`。
> ⇒ 原版那 5 个 `onSummon:1` 调用点 + 1 个 `onSummon:0` 调用点**全部对上**。

---

## 四、验证读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_wdroppod2 bash Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（首个 TMPDIR 那次报的 2 条 `BattleSfx.cs` 是 `B117-WSOUNDCOOL` 当时写一半的中间态，与本批无关；它 6 秒后自己改完） |
| `bash Unity/工具/ruleprobe.sh check` | **rc=1，哨兵失败 1 条**；🔴 **改前/改后逐字节相同**（把新文件临时移出再跑一次，`diff` 只有时间戳一行）⇒ **本批零引擎影响** |
| `bash Unity/工具/ruleprobe.sh play check` | **rc=1**（①-a / ①-b 各「1 张卡变了」= **`UM64`**，`playRc 18→0`）；🔴 **改前/改后逐字节相同**（同上做法）⇒ **那 1 条不是本批造成的**，是 `B117-WGB` 正在做的 `B117-10` UM64 目标格 |
| 全池带 `dropPod` 的卡 | **0 张**（`cards_engine.json` 1127 张；与 `KeywordTable.DropPod` doc 的「`SW23 Fenrisian Drop Pod` 只是卡名带这个词」一致）⇒ **本支休眠，零可观测** |

> ⚠️ 两条 rc=1 **都不是本批引入**，也**没有重钉任何基线**（简报要求：只报读数、⛔ 不重钉）。

**断言：28 条**（`Editor/RuleEngineTest_B117DROPPOD.cs`）

| 段 | 条数 | 现在的状态 |
|---|---|---|
| **D 段**（单位级：`ApplyDropPodOnSummon` 语义 / 灭自证 / 与 `CardSetup` 的闸差） | **19** | ✅ 现在就该全绿 |
| **W 段**（接线：`ApplyDeployTurnState` / `ActivateTraitsOnSummonOrEnchantment` / 别碰坏 fast-ferocity） | **9** | 🔴 其中 **4 条**（W1 / W1′ / W3 / W3′）**在补丁落地前必红**；另 5 条绿 |

`W4` 三条是**回归****断言**：同一次改动会碰 `ActivateTraitsOnSummonOrEnchantment`，
所以顺手钉住 `fast` / `ferocity` 那三句没被改坏。

⚠️ **宿主接线未做**：`RuleEngineTest.cs` 本波归 `B117-WRED-RE` ⇒ 请在任一条既有 `Guard(...)` 的
**同一物理行**追加（零行数）：

```csharp
Guard("B117DROPPOD_Assert", B117DROPPOD_Assert.Run); _pass += B117DROPPOD_Assert.Pass; _fail += B117DROPPOD_Assert.Fail;
```

---

## 五、顺手发现（只报不改）

1. 🔴 **`UnitState.SetupDropPodStats()` 用的是 `Has`，原版用的是 `HasCurrentTrait`**
   （`UnitState.cs:3907` vs `CardSetup.c:159` 的 `EntityScript__HasCurrentTrait`）。
   两词差 `cardTraits` 列 / 残骸档 / jam 三处 —— 与 `B117-24` 那一族**同源**。
   今天 0 张卡带 `dropPod` ⇒ **零可观测**；改法 = 那一行 `Has(` → `HasCurrentTrait(`（零行数）。
   ⛔ 本代理**没动** `UnitState.cs`（不归本批），交给调度台派写手。
2. **活状态表 `B117-47` 的判据指针写的是 `B117_逐卡核验_D_Sautekh_BL_AM.md` §五·4 —— 那一节里没有 dropPod**
   （该文件 §五 只有 5 条顺手发现，逐条是卡名两源 / 卡号写错 / trigger 180 / S3 口径 / SAU33 236）。
   本条的真正判据 = 反编译 `.c` 本身（本文件 §一 已逐句固化）⇒ 建议把那格 `证据锚` 改成指向本文件 §一。
3. **简报写的文件名少一个前缀**：真身是 `CardScript__ActivateTraitsOnSummonOrEnchantment.c`（不是 `ActivateTraitsOnSummonOrEnchantment.c`）。

---

## 六、还欠什么

1. **表现层两跳没做**（原版同一支里）：`BattleCardUI.DisplayStatusAnim(…, 0xe6, …)` 与 `CardScript.UpdateFigures`。
   我们的规则层没有对应层 —— 与 `Enchantment.cs` 那条「`DisplayStatusAnim` 如实留缺口」同口径，**如实记着**。
2. **`onSummon: false` 的负例断言（W5）没写**：它要调 `ActivateTraitsOnSummonOrEnchantment(u, onSummon: false)`，
   而该重载由补丁 1 一起加进来 ⇒ **现在写会编不过**（typecheck 必须 0/0）。补丁落地后请补一条：
   「附魔路（`onSummon:false`）**不许**把池子刷成 2/0」—— 那是补丁 4 的验收闸。
3. **真正的端到端验证做不了**：池内 0 张 `dropPod` 卡 ⇒ 就算接线全上，也**没有一局能触发**。
   本批的正确性由 D/W 两段断言 + 逐句判据承担，不是靠真机。
