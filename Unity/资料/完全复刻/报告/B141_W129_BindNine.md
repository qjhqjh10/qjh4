# B141 · W129 · `B141-BINDNINE`（P1）—— `BindNine` 压扁九宫格的修复 + 社交 6 个下拉框 `selectedArt` 接线

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 **`B141-W81`**。
> 输入判据 = `报告/B141_W129_SelectedArtRest.md` §四·1（W70 上报的机制缺陷）· `报告/B141_W129_SelectedArt.md`（W44 已接的
> `DeckSelectionPopup` 2 颗）＋ **本写手对 UGUI 九宫语义与 `ImageQuad` 实现的现读**（见 §一）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表 / CLAUDE.md。
> 写入 = `Shell/WindowButton.cs` · `Shell/AllianceMemberTab.cs` · `Editor/B141_W70_Assert.cs`（只改 A11 那一处过期断言）·
> 新建 `Editor/B141_W81_Assert.cs` · 本文件。

---

## 〇、一句话结论

`WindowButton.BindNine` 换图时把**左下角块**的宽高比套到全部 9 块上，中段世界宽被压成角块宽（社交下拉框 **1.2037 → 0.5556**，
边框中间断开）。已改成「九宫格子块**只换图、比例一块都不动**」，**单 quad 站点那套「换图后拉回绑定时的比」原样保留**；
修好后把 W70 暂缓的 **社交 6 个下拉框**（`AllianceDropdownBox.Create` 一处站点 = 6 个 prefab 实例）接上 `selectedArt`。

| 口径 | 数 |
|---|---:|
| 修复的机制缺陷 | **1** 个（`SetOn` 对 9 块统一 `SetAspect`） |
| 受影响的生产站点（修好后一起变正常） | **14** 处（＋1 处自检探针；第 14 处那个站点覆盖 6 个下拉框实例） |
| 本次新接线 | **6 颗**（社交 `LanguagesDropdown` ×3 + `Privacy Dropdown` ×3 — 一处站点全覆盖） |
| `typecheck` | `TMPDIR=/tmp/wf_b141_w81 bash 工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0**（3 轮，末轮含断言文件） |
| 断言 | **25 条调用点**（`Editor/B141_W81_Assert.cs`；无循环 ⇒ 运行时也是 25 条） |

---

## 一、判据：原版九宫到底怎么摆

### 1. 原版规则（第一权威 = UGUI 源码，⛔ 不是截图）

`Image.Type = Sliced` 换 sprite 时，uGUI 用**新 sprite 的 `m_Border`** 重算网格：**角块按 border 像素不缩放**，
只有中段/边段被拉伸（`Image.GenerateSlicedSprite`）。也就是说：**换图不改变任何一块的宽高比**，「比例」是切分几何的事，
和纹理是什么无关。

旁证（本工程自己的九宫实现，同一口径）：`ImageQuad.CreateNineSlice`（`Battle/ImageQuad.cs:564-637`）逐块
`q.SetAspect(w / h)` —— **每一块带着它自己那一格的比例**；`MenuDraw.PlaceCell` 在重切/裁切时同样按「那一格」写回。

### 2. 我们的实现错在哪

- `BindNine`（`Shell/WindowButton.cs:566`）`Bind(qs[0], …)` ⇒ `_targetAspect = qs[0].WorldW / WorldH`
  = **`qs[0]` 是左下角块**（`CreateNineSlice` 建件顺序 i=0,j=0 的第一块，`GetComponentsInChildren` 深度优先拿到它）。
- `SwapTo → SetOn`（`:452-464`）原来对 `target` 和 `_nine` 的**每一块**都 `SetAspect(_targetAspect)`
  ⇒ 中段/边块被改成角块的比，而 `ImageQuad.WorldW = WorldH × _aspect` ⇒ **中段被压扁**。

### 3. 实算（社交下拉框 · 本机现读）

框 `250 × 59.4` px · border `(60,35,60,35)` px · `PixelsPerUnit = 108`（`LayoutSpace.Px = px ÷ 108`）：

| 量 | 正确值 | 旧代码（被压成） |
|---|---:|---:|
| 中段宽 `wm = (250−60−60) ÷ 108` | **1.2037** 世界单位 | **0.5556** |
| 角块宽 `60 ÷ 108` | 0.5556 | 0.5556 |
| 角块高（竖向被 `scY` 夹过：`59.4 < 35+35`） | 0.2751 | 0.2751 |
| 角块比例（= 旧代码套给 9 块的那个 `_targetAspect`） | 2.0202 | 2.0202 |
| 竖中段高 `59.4 − 0.2751×2×108` | ≈ 0 ⇒ 那两块不画（6 块） | 同 |

⇒ 旧代码把中段宽从 **1.2037** 变成 **0.5556**（正好等于角块宽），**边框中间那一条断开**。

---

## 二、改前 / 改后（`Shell/WindowButton.cs`）

### 1. `SetOn`（`:456-464`）—— 唯一的行为改动点

```csharp
// 改前
q.SetTexture(tex);
// …（单 quad 的「拉回绑定时的比」理由）
if (_targetAspect > 0f) q.SetAspect(_targetAspect);

// 改后
if (InNine(q)) { q.SetTexture(tex, keepAspect: true); return; }   // 九宫格子块：只换图、比例一块都不动
q.SetTexture(tex);
// …（单 quad 的那一段原样保留）
if (_targetAspect > 0f) q.SetAspect(_targetAspect);
```

- 九宫格子块走 `ImageQuad.SetTexture(t, keepAspect: true)`（A292 早就有的口，语义就是「只换图、`_aspect`/`WorldW`/`WorldH` 一个字节都不碰」）
  ⇒ 与 UGUI「换 sprite 不改角块尺寸」逐条对齐，而且**不用再各自记一份比例**。
- **为什么不用「每块存一份 `_aspect`」那个方案**：`MenuDraw.ClipNineChildren` / `ApplySoftEdges` 会在**绑完之后**按裁切
  重新 `PlaceCell`（改比例/位置），存下来的绑定时刻快照会过期；`keepAspect` 是「按此刻的几何」，天然不会过期。

### 2. `InNine`（新增，**追加在类尾** ⇒ 不挪既有成员的行号）

按 `_nine` 数组逐项认（不是「只认 `_nine[0]`」那种脆判据）；`_nine == null`（单 quad 站点）恒 false。
类尾那一段注释写清了**原版规则 · 实算 · 为什么不用快照方案**。

### 3. `BindNine` 的 doc 就地订正

原来只写「9 块都要换」，现在补上「**但比例用每块自己那一格**，⛔ 不许套 `qs[0]` 的比」——
防止下个会话看到 `_targetAspect` 又把它套回 9 块。

---

## 三、影响面：修好后一起变正常的 14 个生产站点

（`grep -rn "\.BindNine(" --include=*.cs` 现读 = **14 生产 + 1 探针**；⚠️ `Editor/ShellScene.cs:4605` 注释里那句「全库 10 处」**已过期**，
那文件不在本写手白名单 ⇒ 只记不改，见 §七·3。）

| # | 站点 | 换的图（常态 → 悬停/按下） | 之前的表现 |
|---:|---|---|---|
| 1 | `Shell/AllianceSearchTab.cs:885` | `40K_button` → `40K_button_hover` | 悬停时中段压成角块宽 |
| 2 | `Shell/AllianceSearchTab.cs:972` | `40K_button` → `40K_button_hover` | 同上 |
| 3 | `Shell/AllianceMemberOptionsPopup.cs:450` | `ArtBtn` → `ArtBtnHover` | 同上 |
| 4 | `Shell/GenericOptionsPanel.cs:500` | 同上 | 同上 |
| 5 | `Shell/ChatPanel.cs:392` | `UI_Button_Mulligan` → `…_Hover` | 同上 |
| 6 | `Shell/FriendsTab.cs:213` | `UI_Button_Organe_Square_Normal` → `…_Hover` | 同上 |
| 7 | `Shell/FriendsTab.cs:224` | 同上 | 同上 |
| 8 | `Shell/DeckSelectionPopup.cs:774` | `40K_tab_button[ _overwindow]`（W44 接的选中图） | **悬停即破形**（换的是同一张图，仍会走 `SetAspect`） |
| 9 | `Shell/GiveFeedbackPopup.cs:503` | `ArtButton` → `40K_button_hover` | 同 #1 |
| 10 | `Shell/ImportDeckPopup.cs:260` | `40K_button` → `…_hover` | 同上 |
| 11 | `Shell/LeaderboardWindow.cs:756` | `ArtSeasonBtn` | 同上 |
| 12 | `Shell/OfferContainer.cs:1685` | `WebShopArt` → `_square_hover` | 同上 |
| 13 | `Shell/PurchasePremiumWindow.cs:838` | `ArtBtn` → `ArtBtnHover` | 同上 |
| 14 | **`Shell/AllianceMemberTab.cs:2453`（本次新接；一个站点覆盖 6 个下拉框实例）** | `40K_dropdown_field_closed` → `…_opened` | ——（新接线） |
| — | `Editor/ShellScene.cs:4608`（自检探针） | `40K_button` | 那个探针 9 块比例本来就是 1.0 ⇒ 修前修后**逐值不变**（既有断言不受影响） |

> 其中 **#1-#14 的「悬停图与常态图**像素尺寸相同**」已逐对现读核过**（489×107 / 410×124 / 145×124 / 727×102 …），
> 所以 uv 切分不变、只换纹理 —— `keepAspect` 不会让 uv 与几何错配。唯一一对**尺寸不同**的是
> `40K_tab_button`(489×79) ↔ `40K_tab_button_overwindow`(489×97)（只服务 `DeckSelectionPopup` 的 `Selected` 档 = 键盘/手柄），见 §七·1。

---

## 四、社交 6 个下拉框的接线（`Shell/AllianceMemberTab.cs`）

### 1. 判据（原版 `Social Submenu Variant.prefab` 逐颗现读，2026-11-04）

`LanguagesDropdown` ×3 + `Privacy Dropdown` ×3（父链：`Create Alliance View ▸ Select Language / Select Privacy` ×1 组、
`Alliances Tab ▸ … ▸ GeneralDetails ▸ Config fields` ×2 组 = NotMember / Member 两个变体）——**6 颗逐值相同**：

| 字段 | 值 |
|---|---|
| `m_Transition` | **2**（SpriteSwap） |
| `m_HighlightedSprite` | `40K_dropdown_field_opened`（guid `6c66073c…`） |
| `m_PressedSprite` | 同上 |
| `m_SelectedSprite` | 同上 |
| 常态图（`m_TargetGraphic.m_Sprite`） | `40K_dropdown_field_closed` |

### 2. 落点（一处站点覆盖 6 个实例）

`AllianceDropdownBox.Create`（`:2447-2456`）原来 `MenuDraw.Hit(...)` 的返回值被丢掉（= 命中区上没有换图）；现在：

```csharp
var fieldHit = MenuDraw.Hit(f, "Hit", fieldR, QItemBg, box.Toggle);
if (fieldHit != null && bgClosed != null)
{
    var fieldWb = fieldHit.GetComponent<WindowButton>();
    if (fieldWb != null)
        fieldWb.BindNine(bgClosed, ArtFieldClosed, ArtFieldOpened, ArtFieldOpened, ArtFieldOpened);
    else Debug.LogWarning("[Social] 下拉框 `" + name + "` 的命中区上没有 `WindowButton` ⇒ 悬停/按下**不换图**");
}
```

- 两个图名收成常量 `ArtFieldClosed` / `ArtFieldOpened`（**类尾**，零行号漂移），`Create` 里原来那两处裸字面量也改成用它们
  ⇒ **一处判据**（⛔ 不再有第二份字面量）。
- 展开态仍由 `SetOpenVisual` 切成独立的 `#Bg Opened` 九宫（同一张图）⇒ 与 `Bind` 那条路**等价**，没去动它。
- W70 留在 `ItemSelArt` doc 与 `Create` 末尾的「未接 / 机制阻塞」说明**已就地改成事实**（铁律 5）；`Editor/B141_W70_Assert.cs`
  的 A11 原来断的是「不接的理由写在代码里」，现已改成断「**那条过期理由不许再出现**」（否则代码里会留着一条与事实相反的说明）。

---

## 五、断言（`Editor/B141_W81_Assert.cs`，25 条）

| 组 | 条数 | 盯什么 |
|---|---:|---|
| **A** 源码 / 接线 | 8 | 九宫格分支走 `keepAspect: true` · **单 quad 那句 `SetAspect(_targetAspect)` 还在** · `InNine` 按数组逐项认 · `BindNine` doc 写明规则 · 6 颗 `BindNine` 实参（`_closed` 常态 + `_opened` 三档） · 两个常量 = 原版图名 · `AllianceMemberTab` 里 `.BindNine(` 只有 1 处 · 两扇社交窗都经 `AllianceDropdownBox.Create`（6 实例全覆盖） |
| **B** 运行时探针（真实图 727×102 + 真实 border + 真实框 250×59.4） | 17 | 6 块（浮点残差下 9 块）· 换图**前**中段 = 1.2037 · 角块比例 = 2.02 · **悬停后中段宽不变** · **中段 ≠ 角块宽** · 角块宽不变 · **6 块纹理全换成 `_opened`** · 松开后仍 1.2037 且纹理回 `_closed` · **单 quad 比例 2.5 悬停后仍是 2.5** |
| **合计** | **25** | ——（无循环） |

**挂法**（同一物理行追加；⛔ 别插进 `//` 之后）：

```csharp
B141_W81_Assert.Run(); pass += B141_W81_Assert.Pass; fail += B141_W81_Assert.Fail;
```

判绿红按行首 `B141W81 ✓` / `B141W81 ✗` 数（⛔ 别 `grep -c ✗`）。

**灭自证（🧨 什么坏法会让哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| `SetOn` 删掉 `InNine` 分支（回到 9 块统一 `SetAspect`） | **A1 / B4 / B5**（B4 变 0.5556、B5 变成「等于角块宽」） |
| 把九宫格子块改成「什么都不做」 | **B6**（纹理没换） |
| 顺手把单 quad 那句 `SetAspect` 也删掉 | **A2 / B8c** |
| 删掉 `Create` 里的 `BindNine` | **A5 / A7** |
| `ArtFieldOpened` 误写成 `_closed`（三档任一） | **A5** |
| `InNine` 改成「只认 `_nine[0]`」 | **B6**（其余块不换） |

---

## 六、行数变化（相对本写手开工那一刻，非 `HEAD`）

| 文件 | 开工 | 现在 | Δ |
|---|---:|---:|---:|
| `Shell/WindowButton.cs` | 1315 | 1337 | **+22**（`InNine` + 类尾判据块；`SetOn` / `BindNine` 是**同物理行就地改写**） |
| `Shell/AllianceMemberTab.cs` | 2872 | 2885 | **+13**（注释块 + 接线 6 行 + 类尾常量 3 行） |
| `Editor/B141_W70_Assert.cs` | 242 | 244 | **+2**（只改 A11；⚠️ 之后 `B141-W85` 又在**同一文件**加了 §C 组，本写手的 Δ 与它的不重叠） |
| `Editor/B141_W81_Assert.cs` | —— | 新建 | +228 |

> 行尾：三份改过的文件逐字节复核 = **纯 LF、裸 CR 0**（`crlf 0 / bareCR 0`），没有翻行尾。
> ⚠️ **账会漂**：`Shell/WindowButton.cs` / `Shell/AllianceMemberTab.cs` 都被 `账/*.tsv` 按 `文件:行号` 引用
> ⇒ 请调度台在「没有 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`。
> 新增符号（便于按符号名重定位）：`WindowButton.InNine` · `AllianceDropdownBox.ArtFieldClosed` · `AllianceDropdownBox.ArtFieldOpened` · `B141_W81_Assert.*`。

---

## 七、❓ / 给调度台的移交项

1. **❓ `DeckSelectionPopup` 的 `Selected` 档换的是另一张尺寸不同的图**（`40K_tab_button` 489×79 ↔ `…_overwindow` 489×97，
   border 也不同：`TabBorderOn/Off`）。原版 uGUI 会按**新 sprite 的 border** 重算网格；我们的 `SetTexture` 不动 `UvRect`
   ⇒ 那一档（**只有键盘/手柄选中走得到**，指针下不可达）的 uv 切分会沿用旧图的分界。
   ⚠️ 这是**修前修后行为相同**的既有限制（不在本次范围内），要做得先让 `WindowButton` 记住 9 宫的 border。
   两块图不同尺寸 ⇒ 无法用 `keepAspect` 自动救。
2. **⚠️ 本批**没跑 Unity**：24 条断言与「中段 1.2037」那组数**已按源码 + 真实图尺寸实算过**，但**断言本身尚未被执行**
   —— 请在同步点挂上并跑对应宿主（`ShellScene.Run` 覆盖「外壳/社交」，或由调度台指定）。
3. **⚠️ 过期数字（只记不改，文件不在白名单）**：`Editor/ShellScene.cs:4605` 的注释写「全库 **10** 处 `BindNine`」——
   2026-11-04 现读 = **15 处生产站点 + 1 处自检探针**。下次有该文件写手时顺手订正。
4. **⚠️ 并发写手**：本批动 `Shell/AllianceMemberTab.cs` 期间，`B141-W85` 在同一文件的 `BuildList`（`:2526`，列表底板染色）
   与 `Editor/B141_W70_Assert.cs`（§C 组）**同时落盘**。两处改动**区域不重叠**、均已核对在盘上；
   但 `B141_W44_Assert.cs:180` 断的 `DeckSelectionPopup` 那串源码**未受影响**（本批没动那文件）。
5. **✅ 复查入口**：`grep -rn "\.BindNine(" --include=*.cs Unity/MyGame/Assets`（应 = **15 处**：14 生产 + 1 探针）
   · `grep -n "InNine\|keepAspect: true" Shell/WindowButton.cs` · 本报告 §一 那张实算表。
