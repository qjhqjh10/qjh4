# B140_WP 执行报告 —— `ENG-07` tsv 判定列过期 + `B139-D3` 伤害飘字去 `Abs`

> 执行写手 `B140_WP` · 2026-11-04 · 第 127 会话 `B140`。依据 `判据/B140_参数复核_G1P1_战斗引擎.md` §三·1 / §三·3（行号现读）。
> ⛔ 未跑 Unity · 未动 git · 未改 `CLAUDE.md` / `活状态表.md` / 账主表 / `RuleEngine/Core/{RuleCore,EffectText,EffectResolver}.cs` / `CardView.cs`。
> 只写三处：`资源/B115_ENG07_有键无实现.tsv` · `Battle/PopNumber.cs` · 新建 `Editor/B140_WP_Assert.cs`（+ 本报告）。

## 一、`ENG-07` tsv：判定列 4 行过期 → 已就地订正（只改这份 tsv，未动账主表）

| 键 | 改前 | 改后（判定 / 我们的实现·现读落点） | 原版判据 |
|---|---|---|---|
| `psyker` | 真缺口 / 无 | `等价已实现` / `RuleCore.cs:2311 ManaBuffProcess.AddPsychicReactor(ctx, ctx.Active)`（支路 `Core/ManaBuffDeadBranch_B116W41.cs`） | `CardScript__OnTurnStart.c:177` 造 `ManaBuff{psychicMana=1}`，`BattleManager__ProcessManaBuff.c:27-49` **一次都不读**该 flag ⇒ 净效果恒零 |
| `reactor` | 真缺口 / 无 | `等价已实现` / 同上 `RuleCore.cs:2311`（同一句，一改两键） | 同上（`plasmaMana=1` 同样不被读） |
| `transient` | 真缺口 / 无 | `等价已实现` / `RuleCore.cs:2311` **同一物理行内的 transient 支**（逐单位 `Has/HasDefaultTrait("transient")` 且 `!HasRemovedTrait` ⇒ `Health=0` + `CleanupDeaths(Ability)`） | `CardScript__OnTurnStart.c:257`（`DestroyUnit(…, priority 1, deathType=ability(20))`，**无侧别守卫** ⇒ 双方都扫） |
| `massive` | 真缺口 / 无 | `等价已实现` / `Core/CardView.cs:15815-15848`（`SetArtMirrorX` :15835）+ 接线 `BattleDriver.cs:26905 ApplyMassiveMirror`（定义 :40231） | `CardScript__CardSetup.c:198-210`（`ShowDefaultTrait(0x24e=590) ∧ isPlayer==0` ⇒ `localScale=(-1,1,1)`，**水平镜像**） |

- `psyker`/`reactor` 的「死支照抄，池内 0 卡」写在「证据」列（判定列保持纯值，便于计数）。
- 池内 0 卡已现核：`cards_engine.json`（`count=1142`）的 `keywords ∪ cardTraits ∪ enchantTrait` 命中 `psyker/reactor/transient/massive` = **0/0/0/0**
  （同脚本对 `oathDouble` 命中 1 张 `Chaplain Cassius` ⇒ 探针有效，非假阴性）；`activeAbilityPsychicCost` / `PlasmaCost` 非零卡 = **0 / 0**。
- `jam` 仍 `真缺口`（原版 23 处读点 vs 我们 7 处）—— 本包未动。

**② 44 → 42 的「被合并 2 键」= ❓查不到（不存在可复现清单）**：`AUD7:128` 逐一点名 = **42**，与本表 44 组 42 行逐键集合相同（对称差 = 空）；
`AUD7:117` 逐一点名 = 22，与 22 组相同。`B115_S7 §2` 已把 44 的两个候选来源（`penumbra` / `ability`）逐条否定 ⇒ 可枚举合计 **22+42=64**，`44` 无可复现清单（疑为历史笔误）。
已在 tsv **表尾**加一行 `#` 开头的单列说明（不是数据行）。

**回读校验（裸 `split("\t")`）**：总行 65 → **66**（表头 1 + 数据 64 + 说明 1）；数据行 **64 未变**（22 组 22 + 44 组 42，逐键未变）；
列数数据行全 **7 列**（`Counter({7: 64})`，说明行 1 列）；判定计数 42/17/5 → **46 / 17 / 1（`jam`）**；行尾 LF、无裸 CR（`CRLF 0 / LF 66 / bareCR 0`）。
⚠️ 过程：第一次改 `notCollectable` 行时吃掉了行尾空第 7 格的 trailing `\t` ⇒ 回读发现 1 行 6 列，已用二进制脚本补回。

## 二、`B139-D3` 收紧：伤害飘字去 `Abs`（`PopNumber.cs`）

```diff
-            string txt = heal ? ("+" + Mathf.Abs(amount)) : Mathf.Abs(amount).ToString();
+            string txt = heal ? ("+" + Mathf.Abs(amount)) : amount.ToString();
```

（第 **236** 行；治疗档 / 字号 / 框 / 0.35 下限一位没动）

| | 原版值 | 我们（改前） | 我们（改后） |
|---|---|---|---|
| 伤害 `-3` | `-3` | `3`（`Abs`） | **`-3`** |
| 伤害 `5` | `5` | `5` | `5`（不变） |
| 治疗 `5` / 治疗 `-5` | `+5` / `+5` | `+5` / `+5` | 不变 |

- 判据亲读：`decomp_full/CardDamageCounterController__DamageReceived.c:10-11` —— `uVar2 = System_Int32__ToString(local_res10, 0);` 随后**直接**当 `SetText` 实参，全函数无 `Abs`、无符号前缀。
- **行数守恒 697 → 697**（LF、无裸 CR）⇒ 账里 `PopNumber.cs:216/228/236/247/313/486/548` 落点不漂。两处就地改写（非新增行）：`:214` summary 改口径「原版逐字 = `Int32.ToString()`（负数印 `-N`）」；`:235` 行尾追加 `✅ 已复刻（2026-11-04 · B140_WP · 判据… · 断言宿 Editor/B140_WP_Assert.cs）`。

**断言 `Assets/CardPresentation/Editor/B140_WP_Assert.cs`**（新建 · 全局命名空间 · 类名==文件名 · `public static void Run()`，可直接 `-executeMethod B140_WP_Assert.Run`；先断前提再断结论）：

| 条 | 内容 | 灭自证（怎么才红） |
|---|---|---|
| `P0` | `TmpFont.Available`（不可用 ⇒ 整包等于没跑，当场红，不静默跳过） | 字体缺 ⇒ 红 |
| `A`/`A2` | 伤害 `-3` ⇒ `"-3"`，且 **≠ `Mathf.Abs(-3).ToString()`**（`"3"`） | 把 `Abs` 加回伤害分支 ⇒ **A 红** |
| `B` | 伤害 `5` ⇒ `"5"`（无 `+` 无 `-`） | 加任何前缀 ⇒ 红 |
| `C`/`C2` | 治疗 `5` ⇒ `"+5"`；治疗 `-5` **仍** `"+5"` | 删 `"+"` / 顺手把治疗也改 `amount.ToString()` ⇒ 红 |
| `D` | 源码结构性：定位 `string txt = heal`（现读 `:236`），切三元两半 ⇒ 伤害半 **不含 `Abs`** 且含 `ToString`；治疗半保留 `"+" + Mathf.Abs` | 伤害半出现 `Abs` ⇒ `D3` 红（挡「运行时在别处再 Abs 一次」） |

`D` 段切分已在本地用同算式离线复算：`healBranch = ("+" + Mathf.Abs(amount))` · `dmgBranch = amount.ToString();` ⇒ `D3/D4/D5` 全绿。
⚠️ **本件未在 Unity 里实跑**（红线：不跑 Unity）—— `A/B/C` 是运行时断言，需调度台在同步点跑一次 `-executeMethod B140_WP_Assert.Run` 才算数。

## 三、`typecheck`（`TMPDIR=/tmp/wf_b140wp bash d:/4/Unity/工具/typecheck.sh`）

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⇒ **0 / 0**。

## 四、账 / 跨包发现（本包**未动**，交调度台分流）

1. **账里已有同键、本包未改**（红线）：`账/战斗界面_HUD.tsv:742`（`CardDamageCounterController.DamageReceived`）「差异」列现写「改后：纯整数」，
   本包又收紧成「无 `Abs`」⇒ **建议调度台在该行备注补一句**（账主表只有调度台能写）。`:111` 的落点 `PopNumber.cs:216` 与本包无关、行号未漂。
2. **`活状态表.md:169` 的 `B139-D3` 是另一件事**（断言样本 `-1`/`-123` 失效 → 已核销），不是本条「去 `Abs`」；本包不新增账行。
3. ⚠️ **`Editor/BattleScene.cs:23897` 语义已过期**（不在白名单，未改）：`Check(!LastPop.Text.StartsWith("-") && !StartsWith("+"), "…纯整数、无符号")`。
   实战伤害量为正 ⇒ **今天仍绿、无回归**，但「无符号」口径已与原版不符；同族 `:23435` 注释「包 E 已改成无符号整数」也要改口径。建议并入下一包 `BattleScene.cs`（判据已定，无需再查）。
4. 生产端唯一调用点 `BattleDriver.cs:25500` 传 `e.Amount`（伤害事件量为正）⇒ 本次改动**零行为变化**，只堵负数域与原版的偏离。
