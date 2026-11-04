# B141 · W129 · 5-07 —— 余下 30 颗 `m_SelectedSprite` 接线报告（`B141-W70`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W70`。
> 输入判据 = `报告/B141_W129_SelectedArt.md` §三（`B141-W44` 的「该派给谁」表）· `报告/B141_W129_ButtonFx.md` §5-07（W34 表）
> ＋ **本写手对原版 prefab 的现读复核**（`d:/2/tools/assetripper_out/ExportedProject/Assets/`：`Social Submenu Variant` ·
> `DeckSelectionContinueButton` · `Practice Mode Menu` · `Generic Multi Card Display Combat` · `Draft Mode Ongoing State` ·
> `RankedEventWindow` · `Player Profile Window`）。
> 机制（`_selectedTex` / `Bind(..., selectedArt)` / `SelectedTexForTest` / `MissingSelectedArt`）由 `B141-W34` 做在
> `Shell/WindowButton.cs` —— **本写手一个字节没动那个文件**。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表 / CLAUDE.md。
> 写入 = 5 个白名单 `.cs` ＋ 新建 `Editor/B141_W70_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

W44 接掉 47/77；**本批把这 30 颗的每一颗都落到了「去向」上**，其中**新接线 11 颗**（2 个代码站点）、
**等价 7 颗**（原版那几张图与我们画的**同一张**，不需要动代码）、**树上没有对应节点 6 颗**、**机制阻塞 6 颗**（已上报，见 §四·1）。

| 口径 | 数 |
|---|---:|
| 本批负责的 prefab 实例 | **30** |
| **✅ 本批新接线**（`selectedArt:` 真加上） | **11** = 成就页 4 ＋ 下拉项 6 ＋ `Achievement Type Toggle.prefab` 模板 1（同一站点） |
| **✅ 等价**（现状已画同一张图 / 原版死值） | **7** = 社交 4 颗 `trans=0` 页签 ＋ `CircleButton` 3 颗（选中图 = 常态图） |
| **⛔ 我们树上没有对应节点** | **6** = 成就页第 5 颗（`Secret`）1 ＋ `CircleButton` 2（Ranked / 轮抽走 `To Battle`）＋ `Debug Battle Button` 3（原版强制隐藏） |
| **🔴 机制阻塞（`BindNine` 压扁九宫格）** | **6** = 社交 6 个下拉框的合上面 |
| 求和自洽 | 11 + 7 + 6 + 6 = **30** ✔（＋ W44 的 47 = 77，与总表一致） |
| `typecheck` | `TMPDIR=/tmp/wf_b141_w70 bash 工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0**（含断言文件） |
| 断言 | **31 条调用点**（`Editor/B141_W70_Assert.cs`，`Run()` 零前提；B4 是 4 张图循环 ⇒ **运行时 34 条**） |
| ⚠️ 账会漂 | 5 个文件都被 `账/*.tsv` 按行号引用 ⇒ 请在「没有 `.cs` 写手」时跑 `check_ledger_points --all-A --fix` |

---

## 一、逐族落点（30 颗，按 W44 表逐条）

| 族（颗数） | 原版选中图 | 本批判的什么 | 结果 |
|---|---|---|---:|
| 成就页 `Achievement Type Toggle (0..4)`（5） | `40K_settings_button_hover` | 运行时按枚举重建 **4** 颗（W66 已核），逐颗补 `selectedArt: ArtTabSel` | ✅ **4 接线** ＋ 1 无节点 |
| `Achievement Type Toggle.prefab`（模板 1） | 同上 | 与上面同一条判据、同一站点（`BuildTabs` 就是它的实例化口） | ✅ **1 接线**（计在上一条的站点里） |
| 社交 `Generic Tab UI Button {Info,Trophies,Search,Create}`（4） | `40K_tab_button_overwindow` | 原版 **`m_Transition = 0`（死值）**，四态一像素都不变；我们 `ArtTabOff` 就是这张 | ✅ **4 等价**（⛔ 故意不接，理由见 §二·3） |
| 社交 `{Languages,Privacy}Dropdown` 合上面（6） | `40K_dropdown_field_opened` | 底图是**九宫格**；`BindNine` 会压扁中段（§四·1） | 🔴 **6 阻塞** |
| 社交下拉项 `Item`（6） | `40K_dropdown_item_selected` | `BindRow` 第五格补 `selectedArt: ItemSelArt`（单 quad，安全） | ✅ **6 接线** |
| `CircleButton` 一族（5） | `40k_UI_bt_play` | 逐颗现读：**选中图 = 它自己的常态图**（同一张）⇒ Selected 态画常态图 | ✅ **3 等价**（Practice / MultiCard / 模板实例）· ⛔ **2 无节点**（Ranked 走 `To Battle`、轮抽走 `UI_Button_Mulligan`） |
| `Debug Battle Button`（3） | `40k_UI_bt_play` | 原版 `OnEnable` 强制 `SetActive(false)`（正式包也不显示）⇒ 我们按纪律不建 | ⛔ **3 无节点** |

---

## 二、代码改动（2 个站点 + 3 处等价性就地落痕）

### 1. `Shell/AchievementsMenu.cs` —— 成就页 4 颗（+7 行）

- 新常量 `ArtTabSel = "40K_settings_button_hover"`（与 `ArtTabOn` **同图但不同义**：`onSprite` vs `m_SelectedSprite`）。
  签名区注释写明「5 颗逐颗现读 = `…_hover`，`m_Transition = 2` 真值」⇒ 后人不至于把 `ArtTabOn` 顶替成 5-07 的落点。
- `BuildTabs` 的 `for (i …)` 里，在已有的 `tabWb.allowClickWhenAlreadySelected = true;` **同一行后面**补
  `tabWb.Bind(_tabBg[i], ArtTab, ArtTabHover, null, ArtTabSel);`
  —— `Bind` **幂等**（与 `MenuDraw.Hit` 那次前四格逐位相同，只多第五格），既有站点的画法零变化。
- 「5 颗 prefab / 4 颗运行时」：prefab 烘焙 5 颗、**5 颗的 Label 文本在 prefab 里全是 `Secret` 占位**；运行时按
  `Enum.GetValues(AchievementType)` 重建（**枚举只有 4 个值**，W66 已核 `Everguild.LiveOps/Achievement.cs:5-10`）
  ⇒ 我们建 4 颗。**第 5 颗没有运行时对应物**（⛔ 这不是漏做，见 §三·2）。

### 2. `Shell/AllianceMemberTab.cs` —— 下拉项 6 颗（+24 行）

- 新常量 `public const string ItemSelArt = "40K_dropdown_item_selected";`（带完整判据：6 个 `Item` 模板逐颗
  = `guid 9c5732fe…` → 切片名）。
- `ItemArtName(i)` 的「选中行」那一格改成用 `ItemSelArt`（**一处判据**，⛔ 不再有第二份字面量）。
- `BindRow(i)` 的 `wb.Bind(...)` **第五格**补 `ItemSelArt`（单 quad ⇒ `SetAspect` 语义正确，安全）。
- 社交页签那条常量上方加了 5 行的判据块（`trans=0` 死值 / 为什么**不**接）—— 见 §二·3。

### 3. `Shell/AllianceSearchTab.cs`（+14 行）· `Shell/PracticeModePopup.cs`（±0）· `Battle/MultiCardDisplay.cs`（+7 行）

- `AllianceSearchTab` 的 `ArtTabOn/ArtTabOff` 上方同样加了判据块（Search/Create 两颗与成员页同族）。
- `PracticeModePopup` 的 `CircleButton`（`Img(root, "40k_UI_bt_play", …)`）**同行行尾**加
  `✅ 已复刻（… 5-07，等价）`：原版那颗 `m_SelectedSprite` 与它自己的 `Image.m_Sprite` **是同一张**
  ⇒ Selected 态画常态图，本件已经画的就是它 ⇒ **不需要另绑**（⛔ 别为了「有个 `selectedArt:`」硬造一个 WindowButton）。
- `MultiCardDisplay`：① 把那句**过期**的「本地缺 `40k_UI_bt_play_hover/_pressed` 两张」就地订正
  （两张 **2026-10-31 已导入**，guid `64917033…` / `d3346f16…` 逐一对上）；② 补 5-07 等价判据。

**为什么「等价」不写成代码**：`selectedArt` 的意义是「`BtnState.Selected` 那一档换图」。
那 7 颗的原版选中图**就等于它当前画的那张**（CircleButton）或**原版自己根本不吃这一档**（`trans=0` 的社交页签，
`EverguildToggle.DoStateTransition` 在 `None` 档直接返回）⇒ 再挂一次 `Bind` 只会在**别处**（悬停/按下）引入变化，
**那是 5-17 的活、而且是新偏离**。所以本批对它们只落**判据 + 断言**，不落无效接线。

---

## 三、差额 30 − 11 = 19 的逐条解释（⛔ 不是「不做」）

1. **等价 7**（社交 4 页签 ＋ `CircleButton` 3）：见 §二·3 末段。
   - 社交 4 颗：选中图 = `40K_tab_button_overwindow` = 我们的 `ArtTabOff`；原版 `trans=0` ⇒ **四态一像素都不变**。
     🔴 若硬接 `selectedArt`，`on` 态页签被键盘选中时就会从 `40K_tab_button` 画成 `_overwindow` —— **反而造出一个原版没有的变化**。
   - `CircleButton` 3 颗：prefab 现读，`CircleButton` 自己的 `Image.m_Sprite` 与 `m_SelectedSprite` **都是 `7c987b77…`**。
2. **无节点 6**：
   - 成就页第 5 颗：prefab 烘焙 5 颗、Label 全是 `Secret` 占位；运行时按 4 值枚举重建 ⇒ 我们建 4 颗。
   - `CircleButton` 2 颗：`RankedEventWindow ▸ Ranked Play Mode Button`（prefab 现读 `trans=2 sel=7c987b77`）在我们树里的等价物是
     `To Battle Button`（`LiveOpsEventWindow.BuildToBattle`，贴图 `UI_Button_Mulligan`）；轮抽 `Draft Mode Ongoing State ▸ Battle Button Button`
     同理。**两颗都不是同一个节点**（W44 §三 的 ❓1 就是这条）⇒ 要不要改成原版结构（`CircleButton` + 文字）**要调度台拍板**，
     本写手**不擅自改形态**（4 扇模式窗会连着动）。
   - `Debug Battle Button` 3 颗：原版 `OnEnable` 强制隐藏、正式包也不显示 ⇒ 不建。
3. **机制阻塞 6**：见 §四·1。

---

## 四、顺手发现（只记 / 已就近落痕，⛔ 未越界修改）

1. 🔴🔴 **`Shell/WindowButton.cs` 的 `BindNine` 会把九宫格压扁**（`B141-W44` 的 DeckSelectionPopup 那 2 颗也受影响）。
   `BindNine` → `Bind(qs[0], …)` 记下 `_targetAspect = qs[0].WorldW/WorldH`（**角块**的比），而 `SwapTo → SetOn`
   对**9 块**都 `SetAspect(_targetAspect)`；`ImageQuad.WorldW = _worldH * _aspect` ⇒ 中段/边块被改成角的宽高比。
   实算（社交下拉框 250×59.4、border `(60,35,60,35)`）：中段本该 **1.204** 世界单位宽，会被压成 **0.556** ⇒ 边框中间断开。
   🔴 所以本批**没有**给 6 个社交下拉框接 `selectedArt`（理由已写进 `AllianceMemberTab.Create` 的注释）。
   ✅ 正确修法（不在本写手白名单）：`_nine` 每块各存一份 aspect，或 `SetOn` 对九宫格子件**不**改 aspect
   （只换纹理）；修好后这 6 颗一行就能接上。**这条也会让 W44 已接的 `DeckSelectionPopup` 两颗在悬停时变形 —— 建议优先修。**
2. ⚠️ `Battle/MultiCardDisplay.cs` 旧注释「本地缺 `40k_UI_bt_play_hover/_pressed` 两张」**已过期**（两张 2026-10-31 已在
   `Resources/Art/ui/`）—— 已就地订正（铁律 5）。
3. ⚠️ 成就页 **prefab 5 颗 / 运行时 4 颗** 的差异不是本批引入的（W66 已核并断言 4）；本报告只把「第 5 颗去向」写清。
4. 5-17 边界（⛔ 本批没动）：`CircleButton` 的真实悬停/按下图 = `40k_UI_bt_play_hover` / `…_pressed`（`MultiCardDisplay` 已接、
   `PracticeModePopup` 没接）；成就页那 4 颗我们传的**悬停**图 = `_selected`（W60 已核）。这些属 5-17。

---

## 五、断言（`Editor/B141_W70_Assert.cs`，31 条调用点 / 运行时 34 条）

| 组 | 条数 | 盯什么 |
|---|---:|---|
| **A** 逐站接线 | 17 | 成就页常量/逐颗 `Bind`/运行时 4 颗/常态图仍在 · 下拉项常量/第五格/共用常量 · 社交两文件（`ArtTabOff` 对、**且没有** `selectedArt: ArtTabOff`）· `CircleButton` 两张图在树里 · Ranked/轮抽**确实没有** `40k_UI_bt_play` · `Debug Battle Button` 不建 · 下拉框「不接的理由写在代码里」 |
| **B** 灭自证/口径/探针 | 14（B4 循环 4 次 ⇒ 运行时 17） | `TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex` · 成就第五格≠常态图 · 下拉项第五格≠常态图 · 4 张选中图都取得到 · 探针三条（真图 ⇒ 非空 / 缺省 ⇒ null / 错名字 ⇒ 记进 `MissingSelectedArt`）· 探针清账 · **`MissingSelectedArt` 终态为空** · 30 颗四种去向求和 = 30 |
| **合计** | **31**（运行时 34） | —— |

**挂法**（同一物理行追加；⛔ 别插进 `//` 之后）：

```csharp
B141_W70_Assert.Run(); pass += B141_W70_Assert.Pass; fail += B141_W70_Assert.Fail;
```

判绿红按行首 `B141W70 ✓` / `B141W70 ✗` 数（⛔ 别 `grep -c ✗`）。

灭自证（🧨 什么坏法会让哪条红）：

| 改坏法 | 变红的断言 |
|---|---|
| 删掉 `tabWb.Bind(…, ArtTabSel)` 那一句 | **A2** |
| `ArtTabSel` 改成常态图那串 / `BindRow` 第五格改回 `null` | **A1 / A5 / B2 / B3** |
| `ItemArtName` 改回字面量（两处判据） | **A6** |
| 给 `trans=0` 的社交页签硬接 `selectedArt: ArtTabOff` | **A7 / A7b / A7c** |
| `WindowButton.TexOf(Selected)` 改回无条件 `_normalTex` | **B1** |
| 从 `Resources/Art/` 删掉任一选中图 | **B4**（且运行时 `MissingSelectedArt` 非空） |
| `Bind` 里删 `_selectedTex` 赋值 / 删 `MissingSelectedArt.Add` | **B5b / B7b** |

---

## 六、行数变化（相对本写手开工那一刻，非 `HEAD`）

| 文件 | 开工 | 现在 | Δ |
|---|---:|---:|---:|
| `Shell/AchievementsMenu.cs` | 636 | 643 | **+7** |
| `Shell/AllianceMemberTab.cs` | 2848 | 2872 | **+24** |
| `Shell/AllianceSearchTab.cs` | 1141 | 1155 | **+14** |
| `Shell/PracticeModePopup.cs` | 3890 | 3890 | **±0** |
| `Battle/MultiCardDisplay.cs` | 1331 | 1338 | **+7** |
| `Editor/B141_W70_Assert.cs` | —— | 新建 | +242 |

> 行尾：5 个文件改完逐字节复核，**全是 LF、裸 CR = 0**（`crlf 0 / bareCR 0`），没有翻行尾。
> ⚠️ 账会漂 ⇒ 请调度台在「没有 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`。
> 新增符号（便于按符号名重定位）：`AchievementsMenu.ArtTabSel` · `AchievementsMenu.BuildTabs`（`tabWb.Bind(...)` 第二句）
> · `AllianceDropdownBox.ItemSelArt` · `AllianceDropdownBox.BindRow` · `AllianceDropdownBox.ItemArtName` · `B141_W70_Assert.*`。

---

## 七、给调度台的移交项

1. **🔴 P1 · 修 `WindowButton.BindNine`**（§四·1）：它压扁九宫格中段，**W44 已接的 `DeckSelectionPopup` 两颗在悬停/选中时会变形**。
   修好后社交 6 个下拉框的 `selectedArt` 一行即可接上（`Create` 里已经有 `bgClosed` 引用，注释里留了位置）。
2. **❓ 轮抽 / 排位的战斗钤要不要改成原版结构**（`CircleButton` + 文字，现在 `UI_Button_Mulligan` + 文字）——
   这是形态决定（4 扇模式窗），**要拍板**，本批按「无对应节点」记。
3. **成就页 5/4**：若要复刻第 5 颗，得先查清原版运行时到底建 4 颗还是 5 颗（本地只能看到 prefab 烘焙 5 颗 + 枚举 4 个值）。
4. **5-17 边界**：`PracticeModePopup` 那颗 `CircleButton` 的真实悬停/按下图（`…_bt_play_hover/_pressed`，本地已有）本批**没接**。
