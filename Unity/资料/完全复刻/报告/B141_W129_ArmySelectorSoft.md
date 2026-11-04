# B141_W129 · `Army Selector` 视口软边**逐窗传参**（`B141-ARMY-SELECTOR-SOFT`）

> 执行写手 `B141-W56` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 判据 = `判据/B141_W129_对账_UI动画.md` §3.1（`4-16` 同族口径）＋ `报告/B141_W129_GlowClip3.md` §三（W27 顺手发现）。
> 原版判据全部**逐 prefab 现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab` 的 `RectMask2D`，
> 沿 `m_GameObject → m_Father` 父链逐层解出节点路径 —— 不是按名字撞）。
> ⛔ 本笔未跑 Unity、未动 git、未改 `d:/2`、未改账主表/活状态表/CLAUDE.md/所有权表。

## 一句话

`ShowEventArmiesPanel.Build` 里写死的 `m_Softness (0,0)` 换成了**形参 `Vector2Int? softness`**，能量窗显式传
**`(25,0)`**（原版 `EnergySinglePlayerOnlyEventWindow.prefab:6898`）；**没传就出声**（⛔ 不静默）。

---

## 一、原版逐窗值（prefab 现读 · 2026-11-04）

> 九份 prefab 里凡带 `Army Selector` 的 `RectMask2D` 全部扫过；**五处「活动/排行」实体窗的 `m_Padding` 全是 `(0,0,0,0)`**
> ⇒ 本笔**只动 `m_Softness`**（将来若哪族 pad 非 0，得再把 pad 一并形参化）。

| # | prefab | 节点路径（父链实读） | `m_Softness` | `m_Padding` | prefab 行 | 我们落点 | 改前 | 改后 |
|---|---|---|---|---|---|---|---|---|
| 1 | `EnergySinglePlayerOnlyEventWindow` | `Army Selector Panel/Army Selector/Viewport` | **(25,0)** | (0,0,0,0) | `:6892-6898`（GO `1477046003934300`） | `Shell/ShowEventArmiesPanel.cs:110`（唯一宿主 = 能量窗） | `(0,0)` ❌ | **`(25,0)`** ✅ |
| 2 | `SkirmishModeEventWindow` | `Ranked Army Selector/Army Selector/Viewport` | (0,52) | (0,0,0,0) | `:4505-4512` | `Shell/LiveOpsEventWindow.cs:989`（`VpSoft`） | (0,52) ✅ | 不动 |
| 3 | `RankedEventWindowV2` | `Ranked Army Selector/Army Selector/Viewport` | (0,52) | (0,0,0,0) | `:4895-4902` | 同上（三族同值，`LiveOpsEventWindow.cs:177`） | (0,52) ✅ | 不动 |
| 4 | `Campaign Army Selector` / `Campaign Tab` | `(Campaign Army Selector/)Viewport` | (0,0) | (0,0,0,0) | — | `Shell/CampaignTab.cs:299` | (0,0) ✅ | 不动 |
| 5 | `RankedSkirmishLeaderboardPopup` · `RankedClassicLeaderboardPopup Variant` · `DraftLeaderboardPopup` | `Ranking Display/Content/Army Selector/Viewport` | (42,0) | (0,0,0,0) | — | `Shell/LeaderboardWindow.cs:159`（`ArmyClipSoft`）+ `:450` | (42,0) ✅ | 不动 |
| 6 | `Daily Reward Popup` | `Army Selector/Viewport` | (42,0) | (0,0,0,0) | — | `Shell/DailyRewardPopup.cs:1400` | (42,0) ✅（W6 已修） | 不动 |
| 7 | `Forge Army Selector` / `Forge Tab` | `Forge Army Selector/Viewport` | (42,0) | (0,0,0,0) | — | `Shell/ForgeTab.cs:246`（`SelSoft`）+ `:478` | (42,0) ✅ | 不动 |
| 8 | `Practice Mode Menu` | `Deck Selector/Army Selector/Viewport` | **(0,50)** | (0,0,0,0) | — | `Shell/PracticeModePopup.cs:468`（`ArmyClipSoft`） | (0,50) ✅ | 不动 |
| 9 | `Army Selector`（独立母版）· `Ranked Army Selector`（独立母版） | `Viewport/Army Selector` | (42,0) / (0,52) | (0,0,0,0) | — | 由 6/2 覆盖（母版不单独建） | —— | 不动 |

⇒ 结论（铁律 5·c「一个值 ≠ 全部情况」）：**这一颗掩码在原版有 5 种取值**（0,0 / 0,50 / 0,52 / 25,0 / 42,0），
共享件写死任何一个都是错。**唯一真缺口 = #1（能量窗，经共享件）**；#2–#8 早在各自窗里按原版值接线，本笔只给断言钉住。

---

## 二、调用点清单（现读，全仓）

| 调用点 | 形态 | 软边从哪来 | 现值 |
|---|---|---|---|
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:948` | `ShowEventArmiesPanel.Build(root, QArt, QArt1, QText, new Vector2Int(25, 0))` | **本笔新增第 5 实参** | (25,0) |
| `Editor/ShellScene.cs:11282` | `ShowEventArmiesPanel.Build(pRoot)`（自检宿主建一颗裸面板） | **没传** ⇒ 走出声那一支、按 (0,0) 建 | (0,0) ⚠️ 见 §五·1 |

`grep -rn "ShowEventArmiesPanel.Build" --include=*.cs d:/4` 全仓只有这 **2 处**（另有 `_tmp_view/scene_bak/` 一份历史备份，不参与编译）。
⇒ **Skirmish / Ranked / Campaign / Leaderboard 四族「逐一接线」的落点是它们各自的窗**（§一 #2–#5），
它们**本来就不经过 `ShowEventArmiesPanel`**（原版那条类只有一个调用点 = 能量窗 `Open()`；
排行/竞技那一族走的是 `ArmySelectorRanked`）—— 所以**不是漏接线，也不是该把四族硬塞进本件**。

---

## 三、改了什么（行数变化）

| 文件 | HEAD 行数 | 现读行数 | 变化 | 说明 |
|---|---|---|---|---|
| `Shell/ShowEventArmiesPanel.cs` | 311 | 349 | **+38，全部加在类尾（`EnsureScroll` 之后）** | 形参 + `ApplySoftness` + 三个自检读口 + 判据段（含逐窗值表与 `✅ 已复刻` 标记） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | 1747 | 1747 | **0** | `:948` 同一物理行加第 5 实参 + `:940` 那条注释就地补值（为 0 行数变化特意压成一行） |
| `Editor/B141_W56_Assert.cs` | —— | 249 | 新建 | 断言本体 |

- 行号守恒验证：`ShowEventArmiesPanel.cs` 的 `:97`（`Build`）/`:136`（`Initialize`）/`:169`（`Reset`）**与改前逐字同号**
  （账 `日常_奖励.tsv:700/701` 与 `:716` 引用的就是这三处）· `:110`（`ViewportClip.Hang`）同号；
  `EnergySinglePlayerOnlyEventWindow.cs` 的 `:948` / `:1418` / `:1501` / `:1538` 同号。
- 闸：`python -I -X utf8 工具/check_ledger_points.py` 全量跑过一遍，**输出里没有一条提到
  `ShowEventArmiesPanel` 或 `EnergySinglePlayerOnlyEventWindow`** ⇒ 这两份文件没有新增漂移/失败。

### 代码形状（判据 → `ShowEventArmiesPanel.cs` 类尾 §软边）

```csharp
public static ShowEventArmiesPanel Build(Transform parent, int qArt = 3111, int qArt1 = 3112, int qText = 3114, Vector2Int? softness = null)
...
var vp = ViewportClip.Hang(sel, "Viewport", new PxRect(...), Vector4.zero, p._softness);   // ⛔ 不再有 Vector2Int.zero 字面量
```

- `softness` 保持**可选**只为不弄坏白名单外的宿主（`Editor/ShellScene.cs`）。**缺省不是「一个原版值」，是「你漏了」**：
  走兜底时 `Debug.LogWarning("[Event] … **没传 softness** ⇒ 视口按 (0,0) 硬边建 …")` 并置 `SoftnessGiven = false`。
- 自检读口：`panel.Softness`（`Vector2Int`）· `panel.SoftnessGiven`（`bool`）· `panel.Viewport`（`ViewportClip`）。

---

## 四、断言（`Editor/B141_W56_Assert.cs`，**25 条**，含前提断言）

| 段 | 条数 | 断什么 | 🧨 怎么才能红 |
|---|---|---|---|
| A（行为，建一扇能量窗） | 10 | `ArmiesPanel` 建出来 · `SoftnessGiven` = true · `Softness` = **字面量 (25,0)** · `ViewportClip.softness` = **(25,0)** · `padding` = (0,0,0,0) · `State.RenderClip` 非空 | ① 把 `Build` 里 `p._softness` 改回 `Vector2Int.zero` ⇒ A3 红（形参被无视）；② 删掉能量窗第 5 实参 ⇒ A1/A2 红（缺省被静默用） |
| B（共享件源码） | 5 | `Hang(sel,"Viewport"…)` 那一行必带 `p._softness` 且**不含** `Vector2Int.zero` · 形参声明恰 1 处 · 「没传 softness」出声恰 1 处 | 写回常量 ⇒ B1 红（与 A3 **不共因**：一条看运行结果、一条看建造口） |
| C（调用点） | 4 | 能量窗 `ShowEventArmiesPanel.Build(` 恰 1 处 · 那一行带 `new Vector2Int(25, 0)` · ≥5 个实参 | 删实参 / 改值 ⇒ 红 |
| D（另四族，**不走本件**） | 6 | `LiveOpsEventWindow.VpSoft` = (0,52) · `LeaderboardWindow.ArmyClipSoft` = (42,0) · `PracticeModePopup.ArmyClipSoft` = (0,50)（三条**直接读常量**，不建窗）· CampaignTab 那颗 `Hang(sel,"Viewport"…)` 仍是 `Vector2Int.zero` · `ForgeTab.SelSoft` 的 `new Vector2Int(42, 0)` 恰 1 处 | 谁把别的窗的值「顺手抄成 25/52」⇒ 红 |

⚠️ **本件尚未接线**（白名单外宿主）：`CardPresentation/Editor/ShellScene.cs` 合计行之前、同一物理行**第一个 `//` 之前**追加
```csharp
B141W56_Assert.Run(); _sink.Pass += B141W56_Assert.Pass; _sink.Fail += B141W56_Assert.Fail;
```
（计数器名以宿主当时那一对为准。⚠️ 本仓已 6 次踩「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）

---

## 五、顺手发现（**未改**，需调度台分流）

1. **`Editor/ShellScene.cs:11282` 现在会走「没传 softness」那一支**（有出声、不是静默）：它是自检宿主，建一颗裸面板验
   `Initialize/Reset`。建议补第 5 实参 `new Vector2Int(25, 0)`（= 它模拟的那扇能量窗的原版值），否则 `ShellScene.Run` 日志里会多一条
   `[Event] … 没传 softness …` 警告。**该文件不在本写手白名单**（热点宿主，另有写手）。
2. **`Editor/B141_W27_Assert.cs:352` 那句日志已过期**（铁律 5：就地改）：它写着「由共享件 `Shell/ShowEventArmiesPanel.cs:110` 建
   **（现在传的是 (0,0)）**」——现在传的是 `(25,0)`。行号 `:110` 仍然对（本笔零位移），只有括号里那半句要改。**不在我白名单**。
   同理 `B141_W27_Assert.cs:33` 的卷首注释也写着「不在本批白名单 ‑ 只记不修」，应改成「已由 B141-W56 修」。
3. **类级横切补全（铁律 21）**：W27 报告只列了 5 处 `Army Selector` 视口，实际扫出 **9 份 prefab / 5 种取值**（见 §一）。
   多出来的两处 **`ForgeTab` = (42,0)** 与 **`PracticeModePopup` = (0,50)** **都已经是对的**（不是新缺陷），本笔把它们钉进断言 D 段。
   ⇒ 结论：这一类参数缺陷**没有第二个未修点**。
4. **`m_Padding` 这一族还没形参化**：五处实体窗全是 `(0,0,0,0)`，所以现在不需要；但 `ViewportClip.Hang` 的 pad 形参仍由 `Build`
   写死 `Vector4.zero` —— 若将来哪一族 pad 非 0（同族先例：`DraftModeWindow` 牌表 pad `(0,0,0,−10)`），要一起形参化（见类尾注释）。

---

## 六、❓ / 没做的

- **没跑 Unity**（红线）⇒ 断言**没有实跑读数**；`typecheck` 只证明编得过（`运行时 0 / 编辑器 0`）。
- 四族（Skirmish/Campaign/Leaderboard/Forge/Practice）**只做常量与源码断言，没有建窗做行为断言**：那要各建一扇窗
  （`LiveOpsEventWindow` / `CampaignTab` / `LeaderboardWindow` / `ForgeTab` / `PracticeModePopup`），成本远超本条，
  且它们的值**早在别的波次落地并有自己的断言**（`RewardsScene` / `MainMenuScene` 里那些 softness 改坏法）。
- `ShowEventArmiesPanel` 的软边**是否真的把顶点色斜坡打出来**（渲染面）没有直接断言：`softness` 只经 `MenuDraw.ApplySoftEdges`
  落到顶点色，间接断在 `State.RenderClip` + `softness` 值上；"并排比"要 Unity 与截图 ⇒ 留给真机验收。

## 七、覆盖边界

- 本笔**只覆盖**「`Army Selector` 视口这一颗 `RectMask2D` 的 `m_Softness`」，**不覆盖**同 prefab 里其它掩码
  （例：能量窗的 `Image Mask` = `(375,0)`，W27 已做）。
- 判据源 = AssetRipper 导出工程 `.prefab` 的**实例覆盖值**；独立 prefab（`Campaign Army Selector.prefab` 等）与实例覆盖
  在这一格上逐位相同（都扫过），没有「母版 vs 实例」的分歧。

---

## 八、W62 收尾：三处尾巴 + 宿主接线（2026-11-04 · 写手 `B141-W62`）

### 8·1 改了什么（逐处，全部零行数变化）

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| ① | `Editor/ShellScene.cs:11282` | `ShowEventArmiesPanel.Build(pRoot);`（4 参 ⇒ 走 `ApplySoftness` 的「没传 softness」出声支、视口 `(0,0)` 硬边） | `ShowEventArmiesPanel.Build(pRoot, softness: new Vector2Int(25, 0));` + 行尾 `// ✅ 已复刻（2026-11-04 · B141 · EnergySinglePlayerOnlyEventWindow.prefab:6898 m_Softness (25,0)）` |
| ② | `Editor/B141_W27_Assert.cs:33`（卷首 ③） | 「共享件 `ShowEventArmiesPanel.cs:110` 建、**不在本批白名单** ⇒ 本写手只记不修」 | 「建（**2026-11-04 订正：已由同波 `B141-W56`/`W62` 落成传参 `(25,0)`**；原写「不在本批白名单 ⇒ 只记不修」已过期）」 |
| ③ | `Editor/B141_W27_Assert.cs:352`（D 段日志） | 「建（现在传的是 (0,0)）」·「⇒ ⛔ 不在本批白名单，只记不修」 | 「建（(25,0)，2026-11-04 W56）」·「⇒ ⛔ 本写手当时不改（2026-11-04 W56 已落成传参）」 |
| ④ | `Editor/B141_W56_Assert.cs` | 卷首写死「`ShellScene.cs:11282` 仍调 4 参 ⇒ 会走出声那一支，期望值仍是 (0,0)」；`Run()` 里 A/B/C/D 四段 | 该句改成「已补第 5 实参…该说法已过期」；**新增 §E `E_HostCallSite()`**（源码闸：`ShellScene.cs` 里 `ShowEventArmiesPanel.Build(` 恰 1 处，且那一行必须带 `new Vector2Int(25, 0)`、不得回潮成 `Build(pRoot);`） |
| ⑤ | `Editor/ShellScene.cs` 聚合行（11301，同一物理行） | `…B118W24_Assert.Run(...); Debug.Log(P + $"=== 合计：…` | `…B118W24_Assert.Run(...); **B141W56_Assert.Run(); _sink.Pass += B141W56_Assert.Pass; _sink.Fail += B141W56_Assert.Fail;** Debug.Log(P + $"=== 合计：…` |

### 8·2 为什么 ① 是「补第 5 实参」而不是「留着让缺省生效」

- 本处是 ShellScene 自检宿主里的 ⑦ 活动阵营图标行（合成 `pRoot`，只验 `Count` / 徽记名），**不是原版任何一扇窗的调用点**；
  它传 25 只为**消掉共享件那条出声支**（缺省 `(0,0)` 会 `Debug.LogWarning`，见 `ShowEventArmiesPanel.cs:323-326`）。
- 值取 `(25,0)` = 共享件**唯一实体宿主**（能量窗）的原版值 `EnergySinglePlayerOnlyEventWindow.prefab:6898`，与派活单一致。

### 8·3 接线位置（防止「代码落进行尾注释」）

- 插在 `B118W24_Assert.Run(...)` 之后、`Debug.Log(P + $"=== 合计：…` **之前**、该行**第一个 `//`（index 9014）之前**（实插 index 8856）。
  ⇒ 断言计入合计，且不在注释里。整行长度 9995 → 10087，**行数不变**。

### 8·4 验了什么 / 没验什么

- ✅ `TMPDIR=/tmp/wf_b141_w62 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ✅ 灭自证（内存里模拟，只读不写）：把 `ShellScene.cs:11282` 的 `, softness: new Vector2Int(25, 0)` 删掉 ⇒
  `E2a`（那行含 `new Vector2Int(25, 0)`）与 `E2b`（那行不含 `Build(pRoot);`）**两条同时判红**；同时 `Build` 走 `ApplySoftness`
  的「没传 softness」出声支（`ShowEventArmiesPanel.cs:324`）。
- ⛔ 未跑 Unity（红线）⇒ `B141W56_Assert` 的 **A 段行为断言**与总量**没有实跑读数**；下次 `ShellScene.Run` 时看它的
  `[B141W56] 合计：✓ N · ✗ M` 那一行。
- 行数守恒（二进制计数，LF / 无裸 CR）：`ShellScene.cs` 11655 → **11655**；`B141_W27_Assert.cs` 编辑处 2 行 → 2 行；
  `B141_W56_Assert.cs` 按新增 §E 增长（非账落点文件）。
