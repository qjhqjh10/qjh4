# B145-E3 · `BattleDriver.cs`：A-09 / A-12 / A-13（+ A-10 定案并落地 / A-11 判据不成立）

会话：第一百三十一会话 · 执行代理 `B145-E3` · 2026-11-05
白名单：`CardPresentation/Battle/BattleDriver.cs`（改）· `CardPresentation/Editor/B145_E3_Assert.cs`（新建）
`typecheck`：**运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_e3 bash 工具/typecheck.sh`）

---

## 一、判据（`.c` / 资产 + 摘录）

### A-09 名牌文字色 = 纯白

| 项 | 值 | 出处 |
|---|---|---|
| 我方名牌文字 | `m_fontColor = (1,1,1,1)` | `Battle Arena 1.unity` 的 `PlayerNameText`（MB3747） |
| 敌方名牌文字 | `m_fontColor = (1,1,1,1)` | 同场景 `EnemyNameText`（MB3834） |
| 材质 | `_FaceColor = (1,1,1,1)` | 同上两颗的 TMP 材质 |

⇒ 原版**没有任何调色**，就是纯白。我们改前用的是 `dim = (0.86, 0.88, 0.93)`（蓝灰），**同一份 `dim`
还喂着手牌行 `HandLabel`(`:29139` 旧) / 牌堆 `pileLabel`(`:29333` 旧) / `foePileLabel`(`:29336` 旧) 三处**
⇒ 只能新增一档给名牌，**⛔ 不能把 `dim` 改白**（施工单那一条已照办）。

### A-12 伤害压到 0 也弹飘字

判据 = `decomp_full/CardScript._ReceiveDamage_d__381__MoveNext.c`：

- `:54` —— `*(undefined1 *)(param_1 + 0x35) = 1;`（协程开场，**默认就弹**）
- `:186` —— `*(undefined1 *)(param_1 + 0x35) = 0;`（**唯一清 0 的地方**，在
  `if (HasCurrentTrait(0xf0=240 dodge) == 0 && HasCurrentTrait(0x136=310 invulnerable) == 0 && HasCurrentTrait(0x50=80 shield) == 0)`
  的 **else** 支里）
- `:313-318` —— `if (*(char *)(param_1 + 0x35) != '\0') { BattleCardUI__DamageReceived(ui, *(int*)(damageInfo + 0x18) /* = amount */, 0); }`

⇒ **`amount` 完全不参与这个判断**：0 也会调 `DamageReceived(0)` ⇒ 屏幕上弹 `0`。
我们改前的 `if (e.Amount != 0)` 是**自加的闸**（注释写「免得屏幕上冒出 `-0`」，而 `-0` 根本不会出现 ——
`PopNumber` 用的是 `amount.ToString()`，见 `PopNumber.cs` 的 `B140_WP` 那一行）。

### A-13 开攻击选择器前的两道闸

判据 = `decomp_full/BattleManager__CanAttackCard.c:17-43`（逐句）：

| 位置 | 原文 | 含义 |
|---|---|---|
| `:24` | `HasCurrentTrait(param_2, 0x96 /*=150*/) == '\0'` | 攻方**没有** `cantAttack` |
| `:35` | `HasCurrentTrait(param_2, 0x370 /*=880*/) == '\0'` | 攻方**没有** `nonCombatant` |
| `:37-40` | `*(int *)(param_2 + 0x228) == 2 \|\| CardScript__get_IsAttacking()` | `cardState ∈ {2} ∪ IsAttacking` |

而 `decomp_full/CardScript__get_IsAttacking.c` 全文：
```c
if (*(int *)(param_1 + 0x228) == 3) return true;
return *(int *)(param_1 + 0x228) == 0x11;
```
⇒ **`cardState ∈ {2, 3, 17}`**（**不含 15 `inPlayTransforming`**，与 `RuleCore.CanAttackNow` 的 `B97_W1` 同口径）。

⚠️ 这两道闸在 `CanAttackCard` 里，而技能那颗钮走 `CanUseActiveAbility`（**另一个函数**）
⇒ 被禁攻的单位仍能开**技能**钮；我们只把 `melee/ranged` 置 false。

---

## 二、改动（文件:行号 → 改前 / 改后；行数变化）

### 行数变化总账：`BattleDriver.cs` **42,592 → 42,663 = +71 行**

| 位置 | 变化 | 说明 |
|---|---|---|
| `OpenCommand`（A-13） | **+3** | 2 行注释 + 1 行代码（`:15305`） |
| 类尾（A-09/A-12 的三个成员 + doc） | **+68** | 插在 `BattleDriver` 类**最后一个成员之后、类结束 `}` 之前**（新 `:42520-42578`；类结束由旧 `:42509` → 新 `:42580`） |
| A-09 的两处实参 / `dim` 行（A-09） | **0** | 全在同一物理行内替换 |
| A-12 的 `if` 行 + 它上面那段注释（A-12） | **0** | 同上 |
| A-10 的 `string an = …` 行 + doc（A-10） | **0** | 同上 |

⚠️ **因类尾追加了 68 行，`ChoicePanelPool` 那一族（旧 `:42544-42591`）整体 +71**
（`public static class ChoicePanelPool` 现读 `:42615`）。**这一片若有账落点，需要 `check_ledger_points --fix`。**

### 逐条

**A-09**（3 处就地改，0 行变化）

| 现读行 | 改前 | 改后 |
|---|---|---|
| `:28857` | `var dim = new Color(0.86f, 0.88f, 0.93f);` | `var dim = DimInk;` ＋ 行尾注释（值搬到类尾，**数值一个字没改**） |
| `:28882` | `_enemyText = Hud(…, 3, **dim**, …)` | `_enemyText = Hud(…, 3, **PlateInk**, …)` ＋ `✅ 已复刻` 行尾标记 |
| `:28895` | `_myText = Hud(…, 3, **dim**, …)` | `_myText = Hud(…, 3, **PlateInk**, …)` |

**A-12**（2 处就地改，0 行变化）

| 现读行 | 改前 | 改后 |
|---|---|---|
| `:25532`（原 `:25520` 那段注释） | `// 伤害 0 = 被挡下（原版也发事件）—— 那一条不飘字，免得屏幕上冒出「-0」` | 就地订正为原版判据（`+0x35` 默认 1、`:313` 不看 amount） |
| `:25542` | `if (e.Amount != 0)` | `if (ShowHitDamagePop(e.Amount))` ＋ `✅ 已复刻` 行尾标记 |

**A-13**（+3 行）

| 现读行 | 内容 |
|---|---|
| `:15302-15304` | 3 行注释（判据逐条：`:24` / `:35` / `:37-40` + `get_IsAttacking`） |
| `:15305` | `if ((melee \|\| ranged) && !AttackSelectorAllowed(u, Ctx)) { melee = false; ranged = false; }` |

⚠️ 被挡掉之后如果**也没有技能**可放，会落到下面已有的 `if (!melee && !ranged && !skill) { return; }`（**静默**）——
与原版同一层（`CanAttackCard` 是纯谓词、不出声）一致。

**A-10**（判据定案后一并落地，0 行变化）

| 现读行 | 改前 | 改后 |
|---|---|---|
| `:10775-10780`（doc） | 「名字用 `CardText.Name(CardDef)` = 面板上真显示的那一个……原版取的也是本地化后的卡名」 | 就地订正：原版**固定 `"English"`** |
| `:10781` | `string an = CardText.Name(a), bn = CardText.Name(b);` | `string an = a.Name, bn = b.Name;` ＋ `✅ 已复刻` 行尾标记 |

**新增成员**（`BattleDriver` 类尾，`:42520-42578`）

- `public static readonly Color PlateInk = new Color(1f, 1f, 1f);`
- `public static readonly Color DimInk = new Color(0.86f, 0.88f, 0.93f);`（原局部 `dim` 的值，供自检读同一份）
- `public static bool ShowHitDamagePop(int amount) { return amount >= 0; }`（`PlayHitFeel` 的唯一决策口）
- `public static bool AttackSelectorAllowed(UnitState u, BattleContext ctx)`（`OpenCommand` 的两道闸）

---

## 三、断言（`CardPresentation/Editor/B145_E3_Assert.cs`，新建，**待调度台接线**）

`public static class B145_E3_Assert { public static void Run(); Pass; Fail; RunCount; }` —— 全局命名空间、类名 == 文件名。

| 组 | 条数 | 钉的是什么 | 🧨 怎么才能红 |
|---|---|---|---|
| A（A-09） | 10 | `PlateInk` = (1,1,1,1)；`DimInk` = (0.86,0.88,0.93) 一个字没动；两档**不是同一个颜色**；两档 alpha 都 > 0.5 | 把 `PlateInk` 改成 `DimInk`（或两处都改白）⇒ A2/A3 红；把 `DimInk` 也改白 ⇒ A2 红 |
| B（A-12） | 3 | `ShowHitDamagePop(0) == true`（★关键）· `(7) == true` · `(-1) == false`（幸存者广播标记） | 改回 `amount != 0` ⇒ B1/B3 红；写成 `amount > 0` ⇒ B1 红；写成恒 `true` ⇒ B3 红 |
| C（A-13） | 13 | 正常单位放行 · `cantAttack(150)` 拦住 · `nonCombatant(880)` 拦住 · `cardState 17` 放行 · **`cardState 15` 拦住** · `cardState 3` 放行 · `null` 不放行 | 删任一 trait 项 ⇒ C2/C3 红；改用 `IsInPlayStateOf`（含 15）⇒ C5 红；删掉 state 那格 ⇒ C4/C5 红 |

共 **26 条**（A 10 + B 3 + C 13）。C 组的夹具与 `RuleEngine/Editor/B144_R2_AI_Assert.cs` 同形（`CardDef` 5 参 ctor + `RuleCore.NewBattle` + `ctx.NewInstance`），**不建窗口、不跑表现层**。

**建议宿主**：`Editor/BattleScene.cs` 的聚合行（**同一物理行、第一个 `//` 之前**）追加
`B145_E3_Assert.Run(); _pass += B145_E3_Assert.Pass; _fail += B145_E3_Assert.Fail;`
（`Editor/*Scene.cs` 不在本写手白名单，⛔ 没动。）

⚠️ **B 组断言的边界（如实说明）**：它只钉住**决策口** `ShowHitDamagePop`。若有人把旧闸直接加回 `PlayHitFeel`
的调用点（绕开决策口），B 组不会红 —— 那一档只能靠宿主跑 `BattleScene.Run` 端到端验。

---

## 四、A-10 / A-11 判据定案

### A-10 —— **定案：原版 = 费用升序 → 固定英文名（`ToLower`）→ `uniqueId`；两处旧说法都不完整**

真身 = `BattleManager.SortByCostAndName`（`GetChoiceOptions.c:110-113` 那次 `Sort` 传进来的
`DAT_18423e6f8`）。判据两份互证：

`cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:9071-9101`：
```csharp
private int SortByCostAndName(RawCardScript a, RawCardScript b)
{
    if (a.manaCost > b.manaCost) return 1;
    if (a.manaCost < b.manaCost) return 4294967295;          // -1
    string text1 = a.GetLocalizedCardName("English");
    string text2 = b.GetLocalizedCardName("English");
    int num1 = (text1.ToLower()).CompareTo(text2.ToLower());
    if (num1 != 0) { … 再把两个 ToLower 的名字 CompareTo 一次 … }
    else           { obj9 = a.uniqueId; obj10 = b.uniqueId; }
    return obj9.CompareTo(obj10);
}
```
`decomp_full/BattleManager__SortByCostAndName.c` 逐句同构（`+0x78` = `manaCost`、`DAT_184299380` = `"English"`、`+0x38` = `uniqueId`）。

| 说法 | 判定 |
|---|---|
| `BattleDriver.cs:10757-10765` 的 doc（旧）「`GetLocalizedCardName().ToLower()` + 当前文化 + `uniqueId`」 | 🟠 **少了「`manaCost` 升序」这一整键**，且没写明语言参数是 `"English"` |
| `B144_W3_B129` 二·#19「固定英文名小写 + `uniqueId`」 | ✅ 对（但同样漏了 `manaCost` 这一键 —— 我们的代码**本来就有**它，所以两说都不影响落地） |
| 落地前的 `CardText.Name(a)` | ❌ **错**（那是本地化显示名）⇒ 中文档下顺序与原版不同 |

**已改**：`string an = a.Name, bn = b.Name;`（`CardDef.Name` = `cards_engine.json` 的 `name` = 英文名）。
`.ToLower()` / `String.CompareTo` 的「当前文化」口径**原文没错、照旧**（`String.ToLower()` / `String.CompareTo(string)` 都是文化敏感）。

### A-11 —— **判据不成立，不改**

施工单写「原版两条路都过 `GetChoiceOptions.c:46-70` 抽 → `:110` 排」。**回 `.c` 复核后不成立**：

1. `GetChoiceOptions` **全库只有两个调用方**（`grep -rln` 结果 = 3 个文件：定义 1 + 调用 2）——
   `BattleManager__ChoiceOfCardPlayer.c:27` 与 `BattleManager__ChoiceOfCardEnemy.c:31`，
   两者都是 **「挑一张牌」**，不是 `chooseone` / `chooseeffect` 那种**卡面载荷**。
   ⇒ 我方 `chooseone` / `chooseeffect` 两组候选**根本不经过那个函数**（`BattleDriver.cs:11289` 那条旧注释**是对的**）。
2. **敌方（bot）那一路根本不排**：`ChoiceOfCardEnemy.c:14-24`
   ```
   bVar5 = !networked && (card+0x40 != 0) && matchType != 0xa0(160 Replay)
   lVar3 = GetChoiceOptions(param_1, param_2, param_3, bVar5, 0);
   ```
   而 `GetChoiceOptions.c:110-113` 是 `if (options.Count <= iVar1 && param_4 == '\0') Sort(...)`
   ⇒ **`param_4 == true`（bot 且非回放）时【不排】**。
   我方引擎 `EffectResolver.BuildChoiceOptions`（`:19904-19960`）也确实**不排** ⇒ 与 bot 那一路**一致**；
   玩家那一路由面板 `ShowAsk` 排（`:11311` 附近 `cands.Sort(CompareChoiceOptions)`），也一致。

⇒ **A-11 不需要任何改动**；施工单里「两条路都过排序」这句**应就地订正**（调度台改 `B145_施工单汇总_离线.md` 时可引用本报告）。

---

## 五、typecheck 结果

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（`cd D:/4/Unity/MyGame && TMPDIR=/tmp/wf_e3 bash D:/4/Unity/工具/typecheck.sh`）

---

## 六、卡住 / 顺手发现

### 🔴 1（**必须处理**）A-10 落地后，`Editor/BattleScene.cs` 有一条旧断言会变红

`CardPresentation/Editor/BattleScene.cs:30084-30088`：

```csharp
string n0 = (driver.ChooseOptionName(i - 1) ?? "").ToLower();
string n1 = (driver.ChooseOptionName(i) ?? "").ToLower();
if (string.Compare(n0, n1, System.StringComparison.CurrentCulture) > 0) nameAsc = false;
…
Check(nameAsc, "★（a2）同费用的相邻两张按**本地化卡名**升序（原版第 2 键 = `GetLocalizedCardName().ToLower()`）");
```

`ChooseOptionName` 返回的是**面板上显示的本地化标题**（`CardData.title` = `CardText.Name(c)`，中文档下是中文）；
而排序键现在是**英文名**（`CardData.id` = `CardDef.Name`，见 `BattleDriver.cs:28372` 的 `id = c.Name`）。
这一节的夹具是 `Pilfered Supplies` 的 4 张，费用 2 的三张英文顺序是
`Cult Propaganda` < `Jammed Communications` < `Poisoned Supplies`，而它们的中文标题顺序**不是**这个序
⇒ 中文档下 `nameAsc` 很可能为 false ⇒ **这条会红**。

**建议补丁（`BattleScene.cs` 不在我的白名单，未改）**：把这两行换成**同一个键**，并订正文案——

```csharp
                    string n0 = (driver.ChooseOptionId(i - 1) ?? "").ToLower();   // 原版第 2 键 = GetLocalizedCardName("English")
                    string n1 = (driver.ChooseOptionId(i) ?? "").ToLower();
                    if (string.Compare(n0, n1, System.StringComparison.CurrentCulture) > 0) nameAsc = false;
                }
                Check(nameAsc,
                      "★（a2）同费用的相邻两张按**英文卡名**升序（原版第 2 键 = `GetLocalizedCardName(\"English\").ToLower()`）");
```

（`ChooseOptionId(i)` = `_chooseViews[i].Data.id` = `CardDef.Name` = 英文名 —— 与 `CompareChoiceOptions` **同一个键**。）

### 2（A-12 的**已知残留**，原版有、我们缺）

原版 `_ReceiveDamage…MoveNext.c:186` 还会把 `+0x35` 清 0 的三档：`dodge(0xf0=240)` / `invulnerable(0x136=310)` /
`shield(0x50=80)` —— **被挡下时屏幕不冒数字**。

我们**判不出来**：`RuleEngine/Core/RuleCore.cs:8650-8685` 在 `EmitHit(ctx, u, 0)` **之前**就把 shield/dodge 摘掉了
（`u.RemoveAll(KeywordTable.Shield)` / `Dodge`），而 `Hit` 事件只带 `(player, slot, amount)`
⇒ 事件到表现层时那三条 trait 已经不在，`u.Has(...)` 恒假。

池内现读：`shield` **13 张** · `dodge` / `invulnerable` **0 张**。
⇒ 这 13 张若真吃到 0 伤害，我们会**多一次「0」飘字**（原版静默）。
**要做只能给 `Hit` 事件加一个「被完全挡下」的位**（`RuleEngine` 侧改动，不在本批白名单）⇒ 已记进本节，交调度台排期。

### 3（`decomp_full` 又两例「文件名 ≠ 内容」）

- `BattleManager.__c__DisplayClass486_0___GetChoiceOptions_b__0.c` 的内容是
  `AiScripted_<>c__DisplayClass33_0__<ExecuteAction>b__0`（比 `+0x38` 的两个字符串）；
- `BattleManager.__c__DisplayClass486_0__.ctor.c` 的内容是 `<>f__AnonymousType31___ctor` → `NetworkingPeer__OnMessage`。

⇒ **这两份都不能当 `GetChoiceOptions` 比较器的判据**（`A1962` 那条坑的重复命中）。
真身是**具名方法** `SortByCostAndName`，只能从 `cpp2il/out_il2csharp` / `decomp_full/BattleManager__SortByCostAndName.c` 读。
（`BattleDriver.cs:10742` 的旧 doc 已经记过这条坑，本次**复核仍然成立**。）

### 4（`dim` 仍被三处使用，没动）

`BuildHud` 里 `var dim = DimInk;` 之后仍有 `HandLabel`（旧 `:29139`）/ `pileLabel`（旧 `:29333`）/
`foePileLabel`（旧 `:29336`）三处读它 ⇒ 施工单要求的「只换名牌两个实参」已严格照办（A 组断言 A2 钉住）。
