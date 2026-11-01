# B113W4 · `B-148` 收尾：退役 `CardDisplayWindowPartial_B111F1E`

- 会话/波次：第一百一十三会话 · 第 1 波 · 写手 `W4`
- 日期：2026-11-01
- 范围：`A/CardPresentation/Battle/CardDisplayWindow.cs`（原地 1 行）· **删除** `A/CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs`(+`.meta`) · `A/CardPresentation/Editor/B111_F1E_Assert.cs`
- 本波未跑 Unity（纪律）；只跑了秒级 typecheck + 账闸（只读）

---

## 一、改动（文件:行号 + 摘录）

### 1. `A/CardPresentation/Battle/CardDisplayWindow.cs:1508`（`BuildFan()` 末尾那一拍）

原：

```csharp
PlayOpenAnimation(); EnsureLevelUpFx();          // 开窗那一拍（原版窗根那颗 `Animation`，见 `CardFan` 里那一块判据）
```

现：

```csharp
PlayOpenAnimation();          // 开窗那一拍（原版窗根那颗 `Animation`，见 `CardFan` 里那一块判据）· 🔴 2026-11-01（`B-148`）：原来这里还顺带调了一次「确保升级动画组件」的口，那个部分类（`CardDisplayWindowPartial_B111F1E.cs`）**已整体退役删除**—— 战斗窗不建光效层、`_fx` 恒 null，那一跳只会每次开窗挂一个**永不播**的 `CardDisplayLevelUp` 并打一条「接不上」告警；升级播放留在 `Shell/CardDetailPopup.PlayUpgradeAnimation`
```

- **同一物理行原地改**（`git diff --numstat` = **1/1**）⇒ 本文件 2686 行、行号前后完全一致（`b.count(b'\n')` 头尾都是 2686，LF，裸 CR 0）。
  这一点是硬要求：该文件下游还有账落点 `:1604 / :1711 / :1905 / :2131`（`战斗界面_HUD.tsv` / `其他_基础设施.tsv`）。
- 退役注释里**故意不写** `EnsureLevelUpFx` / `LevelUpFx` 这两个标识符 —— 新断言按**文本计数**判「旧的没了」，注释里带上会把它自己搞成假绿/假红。

### 2. 删除 `A/CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs` + `.cs.meta`

- 删前全仓 `grep`：`Assets/**.cs` 里唯一引用方是 `Editor/B111_F1E_Assert.cs`（本次已同步改写），**没有第二个消费者**。
- 判据文件 / 账 / 交接里的引用属**文档**，按铁律「证据允许过期」不跟改（其中一处需调度台订正，见 §五）。
- 该 partial 里的东西一件不留：`LevelUpFx` / `LevelUpInheritChild` / `LevelUpInheritTarget` / `_warnedNoLevelUpFxTargetB111F1E` / `EnsureLevelUpFx()` / `BindLevelUpInheritTargets()` / `FindFxNode()` —— 全部只在该文件里出现过（删前已逐个 grep）。

### 3. `A/CardPresentation/Editor/B111_F1E_Assert.cs`（断言新口径）

| | 原（`B111_F1E`） | 新（`B-148` 退役后） |
|---|---|---|
| 文件头 | 「判据详见 `Battle/CardDisplayWindowPartial_B111F1E.cs` 文件头」 | 改写成**现行口径**：方案 A 生产链在 `CardDetailPopup`（`:1240` 挂光效层 / `PlayUpgradeAnimation`），partial 已整体删除及其原因 |
| 常量 | `WinPath` / `PartPath` / `FxCpPath` | `WinPath` / **`OldPartPath`（已退役，断言其不存在）** / **`PopupPath`（新）** / `FxCpPath` |
| B1 | `Count(win,"EnsureLevelUpFx();")==1` + `"PlayOpenAnimation(); EnsureLevelUpFx();"==1` | **「旧的没了」**：① `!File.Exists(OldPartPath)`；② `Count(win,"EnsureLevelUpFx")==0`；③ `Count(win,"LevelUpFx")==0 && Count(win,"BindLevelUpInheritTargets")==0` |
| B2–B5 | 读 partial 源码（`SlotViews[0]` / `fx.inheritChild=` / `FindFxNode(_fx,…)` / `root.name == name`） | 随 partial 一并删除 |
| B2（新） | —— | **「新的在」**：`Count(PopupPath,"LevelUpEffect.DoAnim();")==1` · `"RebuildKeepingState(); PlayUpgradeAnimation(after);"==1` · `"void PlayUpgradeAnimation("==1` · `"CardDisplayEffects.BuildUnder2DCard(SlotViews[0].FaceRoot)"==1` |
| B6 | 未动（`!win.Contains("CardDisplayEffects.Build(_cards")`） | 不变 |
| A 段 | 运行时真建光效层 / 真建 `CardView` | 不变（仍在测判据本身的两个取值口：`Level Up Effect` 精确名 + `2DCard/Front`） |

**为什么不自证**：B1 读的是**另一个文件**（`CardDisplayWindow.cs` + 文件系统里那个 partial **不存在**），B2 读的是**第三个文件**（`CardDetailPopup.cs`）——
「两边一起改回去」（把 partial 加回来 + 把 `EnsureLevelUpFx` 加回去）会让 B1 红；把驱动从 `CardDetailPopup` 删掉会让 B2 红。两条互不覆盖。

---

## 二、删除清单

| 文件 | 行数 |
|---|---|
| `A/CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs` | 104（整文件） |
| `A/CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs.meta` | 2（guid `757e7dfcc3ecef942b315c60461dbd2e`） |

---

## 三、改动清单（文件 + 净行数）

| 文件 | 净行数 | 说明 |
|---|---|---|
| `A/CardPresentation/Battle/CardDisplayWindow.cs` | **0**（1/1 原地改） | 删一次调用 + 退役说明 |
| `A/CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs`(+`.meta`) | **−106** | 整份删除 |
| `A/CardPresentation/Editor/B111_F1E_Assert.cs` | **+1**（201→202 行；50 增 49 删） | B 段改口径 |

---

## 四、验证

1. 秒级类型检查：`TMPDIR=/tmp/wf_w4 bash d:/4/Unity/工具/typecheck.sh`
   → `运行时错误数: 0` · `编辑器错误数: 0`（**0/0**）。
   删文件后没有任何「找不到类型/成员」的报错 ⇒ 反证 **无第二消费者**。
2. 行尾与行数守恒（二进制读）：
   - `CardDisplayWindow.cs`：crlf 0 / lf 2686 / 裸 CR 0；HEAD 也是 2686 ⇒ **行号零漂移**。
   - `B111_F1E_Assert.cs`：crlf 0 / lf 202 / 裸 CR 0。
3. 账闸 `python -I -X utf8 Unity/工具/check_ledger_points.py`（**只读、未 `--fix`**）：
   - 输出里 **`CardDisplayWindow.cs` 出现 0 次** ⇒ 我这一改没有制造任何漂移。
   - 全表读数 `A 5841 · 通过 5539 · 漂移 289 · 失败 13` —— 漂移/失败**全部集中在 `BattleDriver.cs` / `RuleEngine/` / 别的代理正在写的文件**上（不是本白名单内的文件）。按 CLAUDE.md 铁律 13·3 第 2 条，那是**并发写手的在飞改动**，不是本条的回归。（上一同步点读数是 `5841/0/0`，A 条数一致。）
4. 全仓 `grep CardDisplayWindowPartial_B111F1E`（`Assets/**.cs`）：只剩**注释**与断言里的**路径常量**（`OldPartPath`）⇒ **0 个活引用**。
5. ⛔ 未跑 Unity / 未跑 `BattleScene.Run`（本波纪律）—— `B111_F1E_Assert.Run` 的真实读数要等调度台同步点。

---

## 五、顺手发现（⛔ 本代理未改，需调度台处置）

1. 🟠 **`_fx` / `EffectsRoot` 现在是零生产、零消费的死格**：
   `CardDisplayWindow.cs:1948 Transform _fx;`（**全文件 0 个赋值点**，`_fx` 恒 null）与 `:1950 public Transform EffectsRoot`（**全仓 0 个读者**，删前就已是 0）。
   退役 partial 之后连最后一个「想读它」的地方也没了。
   ⚠️ **不能随手删**：这两格在 `:1948-1950`，其下游还有账落点 `:2131 / :1905 / :1711`（`战斗界面_HUD.tsv:306/:307/:304` 等）⇒ 删 3 行会连带漂 3+ 条，必须在「没有 `.cs` 写手 + 补丁已合并」的同步点用 `--fix` 一起做。
   `:646` 那句「这一层**不建**（`_fx` 恒 null）。唯一合法建点 = `Shell/CardDetailPopup` 的主卡 `2DCard` 下」**现读仍然成立**，无需订正。
2. 🟠 **账 `菜单_外壳.tsv:526` 的备注（第 8 列）已过期**：仍写着「**b) ✅ 已落地（`B111_F1E`）** —— … `CardPresentation/Battle/CardDisplayWindowPartial_B111F1E.cs:64 BindLevelUpInheritTargets` + 调用点 `CardPresentation/Battle/CardDisplayWindow.cs:1508`」。
   该文件今天已删除、`:1508` 也不再调它 ⇒ 正确形态应是「**b) 已退役（`B-148`，2026-11-01）**：那一跳随 partial 一并删除，方案 A 生产链在 `Shell/CardDetailPopup`」。
   ✅ **不影响账闸**：那一段在**备注列**（col 8），不在落点列（col 7）；落点列现读 = `CardView.cs:2144` / `CardDisplayLevelUpPartial_B111F1C.cs:144` / `CardDetailPopup.cs:3009` / `CardDetailPopupPartial_B111F1D.cs:53`，**本来就没有这个 partial** ⇒ 无悬挂落点。**但正文已与事实相反（铁律 5）**，需调度台就地订正（账只有调度台能写）。
3. ✅ **顺手答掉判据文件里那条「没查」**（`判据/B113W_对账_B_引擎数据.md` §尚需确认 2：`Core/CardDisplayLevelUpPartial_B111F1C` 是否还有第二消费者）：
   **有，而且是活的** —— `Shell/CardDetailPopup.cs:3018` 调 `BindLevelUpInheritTargets()`（`Shell/CardDetailPopupPartial_B111F1D.cs:53`），后者 `:66-67` 给 `fx.inheritChild` / `fx.inheritTarget` 赋值 ⇒ `CardDisplayLevelUp.ApplyInherit` 仍有生产调用路径。
   ⇒ **`Core/CardDisplayLevelUpPartial_B111F1C.cs` 不是死代码**，退役展示窗那一份**不影响**它。
4. ℹ️ `Editor/B111_F1D_Assert.cs`（别人的文件）**未受影响**：它只读 `Shell/CardDetailPopup.cs` 与 `Shell/CardDetailPopupPartial_B111F1D.cs`，与本条无关。
5. ℹ️ 文档里仍有大量「`CardDisplayWindow.cs:1508` 调 `EnsureLevelUpFx()`」的陈述（`判据/B111W_对账_*.md` · `判据/B112W_C_结构方案_148_149_152.md` · `活状态表 B-70`）。按铁律「证据允许过期」，本代理**不跟改判据文件**；**活状态表 `B-148` 行需要调度台核销**（现读仍是「🟠部分完成 … partial 未退役」）。

---

## 六、尚需确认

1. `B111_F1E_Assert` 的新读数（应为 **18 条全绿** = A 段 8 条〔`Build` 前提 + 节点精确名 + `… Down` 子件 4 条，`CardView.Create` 前提 + `Front` 名 + 父件 `2DCard` 3 条，另 1 条 `CountPrefix` 佐证〕+ B 段 10 条〔2 条读文件前提 + B1 3 条 + B2 4 条 + B6 1 条〕）要等调度台跑 `BattleScene.Run`（它挂在 `Editor/BattleScene.cs:46896` 聚合行上）。本波未跑 Unity，**没有实跑证据**。
2. `_fx` / `EffectsRoot` 的删除时机（见 §五.1）—— 建议留到下一次重同步点。
