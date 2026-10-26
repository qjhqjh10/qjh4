# B8_N · 埋伏数值位印 `?` + 卡面侧（P0 真缺陷）

> 执行代理 **N**（代号 `B8_N`）· 2026-10-26
> 白名单 = `CardPresentation/Core/CardView.cs` · `CardPresentation/Core/CardFaceRules.cs` · 账补丁 `账/_parts/B8_N.tsv` · 本报告
> 红线执行：**没跑 Unity / 批处理** · **没动 git** · **没改主账**（只写 `_parts/B8_N.tsv`）· **没改正本** · **没越白名单** · **没派子代理**
> `typecheck`（`TMPDIR=/tmp/wf_b8n`）：**运行时 0 / 编辑器 0**（改动后跑）
> 行尾：`CardView.cs` **CRLF 0 / LF 14390 / 裸 CR 0**（二进制读核过，没翻）

---

## ① 结论（数字）

| 项 | 数 |
|---|---|
| **P0 真缺陷修掉** | **1**（埋伏/面朝下的敌方单位，数值位由「整层关掉 ⇒ 空白」改成「印 `?`」= 原版 `CardTextCountersController.HideStat`） |
| 新增 / 改动的 `.cs` | **1 个文件**（`Core/CardView.cs`）· `+216 / −10` 行（净 **+206**；其中 **≈200 行是类尾新增成员 + 判据注释**） |
| `CardFaceRules.cs` | **未改**（任务 2 = 只写报告，见 §⑥·①） |
| 账补丁 | `账/_parts/B8_N.tsv` **5 行**（11 列 · LF · 无跨补丁重复键） |
| **C → A** | **2**（`CardTextCountersController.HideStat()` · `ResetHide()`） |
| **A 就地订正（铁律 5）** | **2**（`BattleCardUI.ToggleStats(bool,bool)` · `BasicCardUI.ToggleStats(bool)` —— 原写「我们的显隐判据只有 `StatShownFor` … 一致」，**漏了「印 `?`」与「护甲整格关」两件**） |
| **B → C（补读后发现真差异）** | **1**（`CardScript.UpdateAttackText()`：敌方卡的攻击类型高亮，原版**从数值推**，我们靠 `LastAttackType` ⇒ 敌方没打过之前一格都不亮） |
| **沿用未升档** | `CardView` 那 34 条 B 行**一条都不够 G0–G8**（卡在 G3 = 表现层没有离线探针读数口；且口径正本 §六 明写不覆盖 `UpdateFigures` / `UpdateHealthText` 这类纯表现层方法）—— 见 §③ |

**一句话**：把「原版印 `?`、我们关层变空白」这条 **P0** 按判据改成了印 `?`（场上另建四件 TMP、2D 面那四件 TMP 直接写 `?`），顺手把两处**写错等价的 A 行**按铁律 5 订正；补读 `UpdateAttackText` 时查出一条**真差异**（敌方攻击类型高亮）并落成 C 行。

---

## ② 判据（逐条，`文件:行号`）

### 甲、`HideStat` 到底做什么（两份反编译交叉）

| 份 | 出处 | 读到什么 |
|---|---|---|
| 🥈 | `cpp2il/out_cpp2il_dev_ilspy/CardTextCountersController.cs:698-721` | `public void HideStat()`：`hiddenStat = true; currentAmount = -1;` → `while (…) { textsToSet[i].textToSet.text = "?"; }` —— **一个 GameObject 都不关** |
| 🥇 | `decomp_full/CardTextCountersController__HideStat.c:13-16` + `:26-33` | `*(undefined1 *)(param_1 + 0x65) = 1;`（hiddenStat）· `*(undefined4 *)(param_1 + 0x60) = 0xffffffff;`（currentAmount）· 循环里逐个调 TMP 的 `text` setter，实参 `DAT_18423be78` = 静态串 `"?"` |
| 🥈 | 同文件 `:726-729`（`ResetHide`） | 只有 `hiddenStat = false;` |
| 🥈 | 同文件 `:209-212`（7 参 `SetText` 头部） | `if (hiddenStat) return;` ⇒ **藏住期间任何刷新都改不动文本** = 「闸」 |

### 乙、两个调用入口（判据不同，这是本实现里 `hideArmourCell` 的来源）

| 入口 | 出处 | 动作 |
|---|---|---|
| **场上**（`CardScript.SetAmbush(true)` → `BattleCardUI.ToggleStats(false,false)`） | `decomp_full/CardScript__SetAmbush.c:21-41` · `decomp_full/BattleCardUI__ToggleStats.c:13-63` · `cpp2il/.../BattleCardUI.cs:3065-3127` | 五个 counter（**血 / 甲 / 近战 / 远程 / 费用**）各 `HideStat()`；**另加** `armourFrame.SetActive(false)`。`armourFrame` = 护甲**词条框**（`dump.cs:26176` `public GameObject armourFrame; // 0x150`），**不是** `Armour Container/Image`（我们那面盾 `_armourIcon`）⇒ **盾牌不藏** |
| **2D 展示窗**（`CardDisplayWindow.ShowBattleCard` 的伏击支） | `cpp2il/.../CardDisplayWindow.cs:657-708`（`:674` 是三句 `ToggleStats(false)` 之一）→ `cpp2il/.../BasicCardUI.cs:383-432` | 同样五个 `HideStat()`，但**护甲那件多一次** `armourCountersController.Toggle(false)`（`BasicCardUI.cs:394`）= **整格关掉、连 `?` 都不印** |

### 丙、原版场上那四件 TMP 的逐值（prefab 实读，2026-10-26）

`bundle_battleprefabs_vfxandmisc_assets_all/`：

| 节点 | RectTransform `m_LocalScale` | TMP `m_fontSize` | 材质 |
|---|---|---|---|
| `Melee AttackText` | `0.4828722` | **7** | `Material_-1760077249967186717` |
| `Range Attack Text` | `0.4828722` | **7** | 同上 |
| `Armour Text_3697587013347089344` | `0.4828722` | **7** | 同上 |
| `HealthText` | `3.4678855`（父 0.13924 ⇒ 乘积 0.48286） | **8** | 同上 |

四件公共项：`m_VerticalAlignment = 512`(Middle) · `m_HorizontalAlignment = 2`(Center) · `m_fontColor` 白 ·
`m_fontStyle 0` · `m_enableAutoSizing = 1`（min 2 / 2.7，max 7 / 8）。
材质 `Material_-1760077249967186717`（= `Pragati-Regular White_thick Offset for 3D`）实读：
`_OutlineWidth 0.12` · `_FaceDilate 0.12` · `_OutlineColor (0,0,0,1)` · `_FaceColor` 白 · `_GradientScale 21`。
⇒ **有效世界字号 3.3801 / 3.8629**（= 7 / 8 × 0.48287）。

`?` 字形**在**：`Resources/Fonts/Pragati-Regular SDF.asset` 的 `m_CharacterTable` 250 条里含 `m_Unicode: 63`
（原版 `bundle_fonts_assets_all/MonoBehaviour/Pragati-Regular SDF.json` 同样是 250 条含 63）⇒ TMP 印得出真字形，不用自造。

### 丁、我们改了哪几处（行号）

| 位置 | 改了什么 |
|---|---|
| `CardView.cs:7210-7211` | 类文档两行就地改写：**徽标不归 `ApplyAmbush` 管**（按 `Badges.cs:12-24` 的 `W10` 订正）—— 这正是 `Badges.cs:18-23` 指名要下一个改 `CardView` 的代理顺手改的那句 |
| `CardView.cs:7235-7237` | `ApplyAmbush` 的 hide 分支：删掉 `for(…) SetActive(false)` 与 `Show(_armourIcon,false)`，改成 `SetStatsHidden(true)`；`_armourIcon` **不再藏** |
| `CardView.cs:7242` | 恢复分支补 `SetStatsHidden(false)`（= 原版 `ResetHide`） |
| `CardView.cs:7980-7984` | `ApplyTint`：四颗 `?` 走 TMP 顶点色（状态色 × 淡出 × 灰化 × 涨落色，同一族口径；循环在 `:7983-7984`） |
| `CardView.cs:13118-13121` | `SyncStatLayers`：`bool show = board && !_statsHidden && StatShownFor(…)`（`:13121`）—— 这是原版 `SetText` 头部 `if (hiddenStat) return;` 的等价物 |
| `CardView.cs:13219-13225` | `SyncFaceStatTexts`：护甲整格关那一支（`bool on` `:13221`）+ 文本写成 `?`（`string txt` `:13225`） |
| `CardView.cs:14183-14379` | **类尾新增**：判据全集注释 + `BoardStatTextScale` / 描边三常量 / `HiddenStatText` / `_ambushMark[]` / `_statsHidden` / `SetStatsHidden()` / `StatsHiddenFor()` / `EnsureAmbushMark()` / `AmbushMarkFontSize()` / `ApplyAmbushMarkOutline()` / `PlaceAmbushMark()` / 三个自检读口 |

**载体差异（🔶 标了）**：场上那四个数是**烘出来的字形网格**（`PragatiDigits.Data.cs` 的表只有 `0–9`，**没有 `?`**），
所以这一档另建四件 TMP 印 `?`（字体/字号/对齐/描边全照原版那四件）。要改回网格，得先给字形表补 `?` 并重烘三档
（`工具/gen_pragati_digits.py` + 生成物 `Core/PragatiDigits.Data.cs` —— **都不在本批白名单**）。

---

## ③ 卡面侧那 34 条 B 行：为什么一条都没升 A

`python -I -X utf8 工具/ledger_slice.py --file CardView --status B --limit 0` 现读 **137 行**，其中「我们的落点」真指到
`CardView.cs:<行号>` 的 **34 条**。逐条按 `判据/战斗逻辑驱动_B2A_口径.md` 的 G0–G8 对拍，**全部卡在同一格**：

| 格 | 结论 |
|---|---|
| G0 / G1 / G2 / G6 / G8 | 多数已 ✓（前几轮已填） |
| **G3（可观察量，两侧数值）** | **✗ 全体**。这张表 §三 的观测量全在**引擎侧**（`ruleprobe play` 的 dump：血量/手牌/墓地/`ActiveEffects`）。`CardView` 是 Unity 侧视图，`ruleprobe`（纯 dotnet）**读不到**它的任何状态；唯一的读法是 `Editor/*.cs` 自检宿主 —— 而**子代理不许跑 Unity 批处理** |
| **§六 的排除** | 口径正本 §六 明写：**不覆盖**「纯表现层」方法（`UpdateFigures` / `UpdateHealthText` / `CleanStatusAnims`）—— 而那正是这 34 条里的主力（`SyncFaceStatTexts` 一族） |
| **方案 §4.1** | 反方向也堵：`CardView.cs:13199` / `:13232` / `:13104` 这几处**一个 `文件:行` 覆盖多个原版方法** ⇒ 按 §4.1 只能记 `B` |

⇒ 本轮**不虚增定案率**，一条没升。要真升，得先有一个「UI 侧观测量」的口径（谁的断言、读哪个属性）——
这件事归调度台定，不属于执行代理自裁。

**与「数值 / 关键词 / 徽章」最相关那 7 行的逐条结论**（都亲读了判据方法体）：

| 账行 | 方法 | 判据（现读） | 结论 |
|---|---|---|---|
| `战斗逻辑驱动:1108` | `UpdateAttackText` | `CardScript__UpdateAttackText.c:1-67` 全文 | **改 C/P1**（真差异：敌方攻击类型高亮，见 §⑤·1） |
| `战斗逻辑驱动:1109` | `UpdateCostText` | `CardScript__UpdateCostText.c:1-22` 全文（= 一句 7 参 `SetText(currentCost, currentCost, rawCard.manaCost, 1,1,0,1,0)`） | **保持 B**：值/显隐/颜色都对上了（落点 `SyncFaceCostText` `CardView.cs:13315-13328`）；**没核的** = 原版 `doAnimation=1` 的 `DOPunchScale` 我们在 2D 面那几件 TMP 上到底做没做（`BumpStat` 只服务场上网格层）⇒ 有疑点，不能升 |
| `战斗逻辑驱动:1110` | `UpdateFigures` | `CardScript__UpdateFigures.c:1-26` 全文（5 串 + `IsUnit` → `CheckIfDead`） | **保持 B**：我们**没有单一总入口**（三处刷新口），「五件一起刷」没有判据口 ⇒ 缺总入口这件事本身还在 |
| `战斗逻辑驱动:1111` | `UpdateHealthText(bool)` | 142 行，**本轮没读** | **保持 B**（如实记：没读，不是「读了没差异」） |
| `战斗逻辑驱动:1115` | `UpdateTraitIcons` | `BattleCardUI__UpdateTraitIcons.c` 132 行（前几轮已读，见 `Badges.cs` 头部） | **保持 B**：等价链已足（先全关再逐个开 + 7 位上限）；G5「哪 4 类时机**不**调它」没逐条对 |
| `战斗逻辑驱动:1020` | `SetEnemySecret` | `CardScript__SetEnemySecret.c:5-6` + `BattleCardUI__SetEnemySecret.c:26-40` | **保持 B**：我们无「秘密面」那一档（面朝下统一走卡背/伏击图）；`:40` 之后仍没读完 |
| `战斗界面_HUD:58` | `SetEmptyRarityStars` | 见 `B8_U` 订正（`:514-517`） | **不重复写账**（`B8_U.tsv` 已改，见 §⑥·④） |

**已做的替代动作**：补读了 `UpdateAttackText`（见 §⑤），把它的 `AttackTypeChanged` 语义取实并改成 C（真差异），
比原来那句「切换近战/远程显示哪一格（未取）」可执行。

---

## ④ 账漂移预期（给调度台）

`CardView.cs` 只增不删的那些行会让**按行号写的 A 行**漂移。精确分段（现在 → 账里）：

| 现在行号 | 偏移 | 触发点 |
|---|---|---|
| ≤ 7979 | **0** | —— |
| 7980 – 13118 | **+5** | `ApplyTint` 那 5 行 |
| 13119 – 13219 | **+6** | `SyncStatLayers` 注释 +1 |
| 13220 – 14181 | **+8** | `SyncFaceStatTexts` +2 |
| ≥ 14182 | **+198** | 类尾新增成员块（`14183–14379`；原来 `#if UNITY_EDITOR` 那 8 行从 14183 挪到 14381） |

`ApplyAmbush`（7212-7251）**净 0 行**（刻意配平）⇒ `:7212` / `:7216-7251` 那几条账**不用动**。
现读 `check_ledger_points.py`：A 3023 · 通过 2895（告警 89）· **漂移 120** · 失败 8 ——
其中提到 `CardView.cs` 的漂移 **52 条**（含本轮引入的 + 之前就有的）。
⛔ 本轮**没有** `--fix`（有别的代理在写 `.cs`）。请按铁律在「没有 `.cs` 写手」时跑一次
`check_ledger_points.py --all-A --fix`。

---

## ⑤ 顺手发现（不在本批任务里，但查实了）

1. **敌方卡的攻击类型高亮，我们是空的**（→ 已落账 `战斗逻辑驱动.tsv:1108` 改 C/P1）
   `CardScript__UpdateAttackText.c:20-33`：`isPlayer == 0` 那一支把 `type = (melee < range) ? Ranged(2) : Melee(1)`
   写进 `+0x120`，变了才调 `AttackTypeChanged` → 逐件 `HighlightIfNeeded`。
   我们：`CardView.SetOwnAttackType(CardView.cs:9067)` 只吃调用方给的值，而 `BattleDriver` 喂的是
   `UnitState.LastAttackType`（`BattleDriver.cs:15004` 等）⇒ 敌方没打过之前是 `None` ⇒ **两格都不放大、底光都不亮**。
   修点在 `BattleDriver`（喂之前先按 `(melee<range)` 推），**不在我的白名单**。
2. **原版「费用」也是五件 counter 之一**（`BattleCardUI.ToggleStats` 对 `costCountersController` 也调 `HideStat`）。
   我们场上**没有费用层**（`ShowsCost && !board` 才建 `_costBg`，`_info` 场上也不烘费用）⇒ 这一件**无可见差异**，
   但它是「原版那五件 vs 我们四件」的口径差，记在这里以免将来有人以为漏了一件。
3. **`armourFrame` 这个框我们没有绑定**：原版 `ToggleStats(false)` 会关它（护甲**词条框**）。
   我们的对应物在 `Core/TraitFrames.cs` 的框绑定表里，而 `armour` **没有绑**（`TraitFrames` 现读只有 stealth / codex / swarm / oath 四条）
   ⇒ 今天该框根本不存在 ⇒ 无可见差异。**不是**「我们关错了东西」。

---

## ⑥ 给调度台的请求

### ① 任务 2：`CardFaceRules.ShouldShowTargetsAvailable` 的消费点（**只写报告，我没动那个文件**）

- **文件/方法**：`CardPresentation/Battle/BattleDriver.cs` → `void ShowSkillPanel(UnitState u, int targets)`（现读声明在 **`:15065`**；调用点 `:15049`）
- **现在那两行**（现读 `:15065-15070`）：
  ```csharp
  void ShowSkillPanel(UnitState u, int targets)
  {
      if (skillPanel == null || u == null) return;
      _skillPanelTargets = targets;
      skillPanel.Show(CardDisplayName(u.Card, u.Name), u.Ability, targets);
  }
  ```
- **改成**（只在最后那一句上加形参；`SkillPanel.Show` 已有 `bool targetsAny = false`，`Battle/SkillPanel.cs:1535-1537`）：
  ```csharp
  skillPanel.Show(CardDisplayName(u.Card, u.Name), u.Ability, targets,
                  targetsAny: !CardFaceRules.ShouldShowTargetsAvailable(u.Card, false));
  ```
  逐字说明：**形参语义是反的** —— `targetsAny == true` ⇒ `SetTargetsSuffix` **整段不拼**（`SkillPanel.cs:2088-2093`），
  而原版那道闸是「**有** `targetsAvailableCounter` 且 `targetsAffected != any` 才拼」⇒ 取反。
  `u.Card` 是 `CardDef`（`RuleEngine/Core/UnitState.cs:40`）⇒ 直接喂 `CardFaceRules` 的形参，不需要转换。
- **顺带**：`SetTargetsAvailable(n, targetsAny)`（`SkillPanel.cs:1572`）今天**只有 `Editor/BattleScene.cs` 三个自检调用点**
  （`:11858` `:11865` `:11895`），生产侧没有第二处 ⇒ 只改 `ShowSkillPanel` 一处就够。
- `CardFaceRules` 在命名空间 `CardPresentation`，`BattleDriver` 同命名空间 ⇒ 不需要 `using`。

### ② 任务 1 的可观察量（要跑哪条自检）

- **宿主**：`Editor/BattleScene.cs` 的 **1d·⑥ 伏击「面朝下」** 那一块（现读 `:2912-2938`，现有三条断言**仍然成立**，
  因为四个网格层在埋伏时**依旧是 `activeSelf == false`**）。
- **要新增一条（我给的草稿，⛔ 我没写进 `BattleScene.cs` —— 不在白名单，也不该自己写自己跑自证）**：
  ```csharp
  Check(foe.AmbushMarkText(CardView.StatMelee) == "?" && foe.StatsHiddenFor(CardView.StatMelee),
        "★ 埋伏 ⇒ 数值位印 `?`（原版 HideStat），⛔ 不是关层变空白");
  // 灭自证：只关层不印 ? 的那一版，上面这条的 AmbushMarkText 会是 "" ⇒ 红；而下面这条仍绿
  Check(foe.StatShown(CardView.StatMelee) == -1, "★ 四个数值层读 activeSelf ⇒ 都关着（既有断言，保留）");
  ```
- **两侧数值**：原版侧期望 = 字面量 `"?"`（`CardTextCountersController.cs:719` 写死）；
  我们侧读数口 = `CardView.AmbushMarkText(s)` / `AmbushMarkShown(s)` / `StatsHiddenFor(s)`（新增，见 §②·丁）。
- **顺带一条**（若同一批跑）：`own.SetAmbush(true, hideStats:false)` ⇒ `AmbushMarkShown(StatMelee) == false`（我方看得见自己的数）。

### ③ 要转派的（都不在我的白名单）

| 转给 | 活 | 判据 |
|---|---|---|
| `CardDisplayWindow.cs` 的写手 | 伏击支（`:674` 那三句之后）加一句 `cardViewOfMainCard.SetStatsHidden(true, hideArmourCell: true)` | `BasicCardUI.cs:383-432`（护甲整格关）；写口已在 `CardView.cs:14251-14278`。⚠️ 需要先确认展示窗里主卡那个 `CardView` 实例怎么拿（`CardDisplayWindow.cs:1870` 那一带） |
| `BattleDriver.cs` 的写手 | 敌方单位：喂 `SetOwnAttackType` 之前先按 `(melee < range) ? Ranged : Melee` 推（原版 `UpdateAttackText` 那一支只对 `isPlayer==0`） | `CardScript__UpdateAttackText.c:20-33` + `BasicCardUI__AttackTypeChanged.c:1-34` |
| `BattleDriver.cs` 的写手 | `ShowSkillPanel` 那一句（见本节 ①） | `CardFaceRules.ShouldShowTargetsAvailable` |
| 生成器 | 若要「场上也走字形网格印 `?`」：给 `工具/gen_pragati_digits.py` 的表补 `?`（三档 thin/thick/thick3d）并重生成 `Core/PragatiDigits.Data.cs`；然后 `EnsureAmbushMark` 可删 | 现状 = 表里只有 `0–9`（30 条 = 10 字 × 3 档） |

### ④ 账

- `账/_parts/B8_N.tsv`（5 行）：`HideStat` C→A · `ResetHide` C→A · `BattleCardUI.ToggleStats` A（订正）·
  `BasicCardUI.ToggleStats` A（订正）· `UpdateAttackText` B→C/P1。
- ⚠️ **原本我也改了一行 `BasicCardUI.SetEmptyRarityStars()`，但 `B8_U.tsv` 也改同一键且内容更全**
  （它还订正了判据出处 `:583-586 → :514-517`）⇒ **我把那行从自己的补丁里删了**，让 `B8_U` 赢。
  **它剩的那一半（展示窗「藏数值」）的写口就是本批的 `SetStatsHidden`** —— 合入后那条与 `战斗界面_HUD.tsv:307` 应可一起核销。

---

## ⑦ 没查清的部分（如实记）

1. **描边厚度观感未验**：原版那四件走 `_GradientScale 21` 的材质；我们走字体资产自带那份实例材质
   （`_GradientScale` 未必是 21）。`_OutlineWidth 0.12` 在两种 gradient scale 下不是同一个像素厚薄 ⇒
   **本机不跑 Unity、没并排看过**。要验：渲一张埋伏单位，和原版那张 `?` 并排。
2. **`?` 的字号只看首行/单字宽度内的情形**：原版 `m_enableAutoSizing = 1`（我们关掉 autoSizing 并直接给
   max 值 3.3801/3.8629）。单个 `?` 在 0.9×0.9（Health 1.0×1.0 × scale）的框里装得下 ⇒ 两端结果应当相同，
   但**这是推理、不是实测**（原版 autoSize 在装不下时的行为我们照不到，因为我们根本没有装不下的情形）。
3. **原版 `+0x120`（攻击类型）对我方卡是谁写的**：`UpdateAttackText` 只在 `isPlayer == 0` 时写它；
   我方那张的写点本轮**没查**（§⑤·1 只查了敌方那一半）。
4. **`armourFrame` 绑定的 `TraitFrames` 那一跳**：我说「今天该框不存在」是读 `TraitFrames` 的绑定表得出的，
   **没有**去核「原版这场战斗里这个框会不会真的出现」（那要看 `CardTrait` 的 armour 定义 + 框 prefab）—— 判据不在我的域。
5. **`SetStatsHidden` 在 `CardFace.Full`（展示窗）那一档没跑过**：逻辑上走 `SyncFaceStatTexts` 那条 TMP 路，
   但那四件 TMP 只在 `TmpFont.Available` 时建；取不到字体资产时展示窗这一档**什么都不发生**（不静默：`EnsureAmbushMark`
   会 warning，但那条 warning 只在 `_faceMode == Board` 之后才可能触发 ⇒ **展示窗那一档确实可能静默**）。
   ⚠️ 这条留给接 `CardDisplayWindow` 的代理：它应当先判 `TmpFont.Available` 并出声。
