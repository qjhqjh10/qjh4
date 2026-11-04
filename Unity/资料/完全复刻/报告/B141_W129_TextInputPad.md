# B141_W129 · W25 — `/Text Area` 族按 `m_Padding = (−8,−5,−8,−5)` 建视口裁切（条目 `4-23`）

> 执行写手 `B141-W25` · 第一百二十八会话 · 波次 `W129` · 2026-11-04
> 判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` **W11 包** · 审计 = `报告/B138_审计汇总_问题清单.md` §4 `4-23`
> 白名单外一个字节没动（⛔ `MenuDraw` / `ViewportClip` / 账 / 活状态表 / 所有权表）

---

## §1 原版判据：38 处 / 19 个 prefab，逐值同一

**扫描口径**（可复跑）：在 `d:/2/tools/assetripper_out/ExportedProject/Assets/**/*.prefab` 里逐份解析 YAML，
按 `--- !u!(114) &id` 找 `MonoBehaviour`，筛 `m_Padding: {x: -8, y: -5, z: -8, w: -5}`，
再经 `m_GameObject → RectTransform → m_Father` 还原节点路径。
（脚本：`d:/4/_tmp_view/w25_scan_pad.py` —— 浏览器临时件，非工程资产。）

**结果**：

| 项 | 值 |
|---|---|
| `m_Padding = (−8,−5,−8,−5)` 的出现次数 | **38** |
| 落在名为 `Text Area` 的节点上的 | **38 / 38**（叶子名统计：`{'Text Area': 38}`） |
| `m_Softness` | **`(0,0)` × 38 / 38** |
| 分布的 prefab 数 | **19** |

> ⇒ 这一族**只有一套参数**（不是「多套配置」，铁律 5·c 已核）。
> `m_Padding` 的语义 = UGUI (x=Left, y=Bottom, z=Right, w=Top)，**正值缩小、负值扩大**
> （判据 = `Culling/Clipping.cs:26-30`；本仓同一份算式 = `MenuDraw.PaddedRect`）⇒ `(−8,−5,−8,−5)` = 四外各扩一点。

**逐 prefab 计数**：

| prefab | 处数 | prefab | 处数 |
|---|---|---|---|
| `Social Submenu Variant` | 6 | `Rewards Base Submenu Variant` | 2 |
| `Main Menu Settings Window` | 4 | `Forge Tab` | 2 |
| `Give Feedback Popup menu` | 4 | `Deck Editing Menu` | 2 |
| `Collection Menu Variant` | 3 | `Debug Play Window` | 2 |
| `ChooseNameWindow` | 3 | 其余 10 份各 1（`Referral Popup` / `Raid Progress Tab` / `Player Profile Window` / `GenericPromptWindow` / `GameObject/Debug Reward Window` / `Friends Menu Demo` / `Expansion Pass Tab` / `Deck Selection Popup with Tabs` / `Debug Reactivate Event Window` / `ChatPanel`） | 10 |
| | | **合计** | **38** |

**单点实证**（`GenericPromptWindow.prefab` —— 本类直接对位的那一份）：

- `:1337` `m_Padding: {x: -8, y: -5, z: -8, w: -5}` · `:1338` `m_Softness: {x: 0, y: 0}`
- 该 `MonoBehaviour`（`&114719342369648492`）的 `m_Script` = `{fileID: -146154839, guid: d3e719b59ab71ba3f6b398058c866280}`
- 它的 `m_GameObject` = `1587765826080250` → `:187` `m_Name: Text Area`
- 节点的 `RectTransform`（`&224632746491512912`）：`AnchorMin(0,0) / AnchorMax(1,1)` · `sizeDelta (−20,−13)` · `pivot (0.5,0.5)`

---

## §2 我们改了什么（只动一个文件）

**改前**（`Shell/TextInputField.cs:157`，审计原话「只建节点」）：

```csharp
var areaNode = MenuDraw.Node(go, "Text Area", area);          // 只有节点、没有掩码
```

**改后**（`Shell/TextInputField.cs:167`）：

```csharp
var areaNode = ViewportClip.Hang(go, AreaNodeName, area, AreaPad, AreaSoftness).transform;
...
MenuDraw.ClipText(f._ph,   null, Vector2.zero);   // :182  占位字吃同一颗掩码
MenuDraw.ClipText(f._text, null, Vector2.zero);   // :196  正文同一颗
```

类尾新增三个常量（`✅ 已复刻（2026-11-04 · B141_W25 · …）` 标记在同段注释里）：

```csharp
public const string AreaNodeName = "Text Area";
public static readonly Vector4 AreaPad = new Vector4(-8f, -5f, -8f, -5f);
public static readonly Vector2Int AreaSoftness = Vector2Int.zero;
```

**为什么用 `ViewportClip` 而不是就地传一个 `PxRect`**：
本仓「每个视口一个掩码」的唯一等价物就是 `ViewportClip`（节点自己带框、`padding` / `softness` 两个字段逐字对原版
`RectMask2D`，见 `Shell/ViewportClip.cs` 文件头）。`ClipText(..., null, ...)` 沿父链解析 ⇒ 守卫
（`ClippedTextGuard`）每次重排重新解析、**不冻快照**（`MenuDraw.ClipText:1263-1276` 那条 A484 纪律）。

**行数变化**：`Shell/TextInputField.cs` **388 → 422 行（+34；`git diff --numstat` = `35 / 1`）**（纯 LF、无裸 CR）。
⚠️ 该文件**不被账按行号引用**（`资料/完全复刻/账/*.tsv` 里 `TextInputField` 的命中全在**别的文件**的落点上：
`AllianceSearchTab.cs:443/:997` · `AllianceMemberTab.cs:1509`）⇒ 无账漂移风险。

---

## §3 覆盖数：原版 38 处 vs 我们实际覆盖

| 口径 | 数 | 说明 |
|---|---|---|
| 原版 `/Text Area` 挂 `(-8,-5,-8,-5)` 的节点 | **38**（19 prefab） | §1 |
| **本类（`TextInputField.Create`）能覆盖的** | **3 个调用点 ⇒ 4 个实例** | 见下 |
| 本类改后**每实例都建视口掩码** | **4 / 4** | 断言 A1–A11 逐值钉住 |

3 个生产调用点（`grep -rn "TextInputField.Create("`，现读）：

| 调用点 | 实例数 | 对应原版 prefab 节点 |
|---|---|---|
| `Shell/PromptPopup.cs:458` | 1 | `GenericPromptWindow/Window/inputs/template/Text Area`（**本类对位那一份**） |
| `Shell/AllianceSearchTab.cs:997`（`Field(...)`，建盟 **Name / Desc** 各一格） | 2 | `Social Submenu Variant/…/Create Alliance View/…/Name Input/Text Area` + `…/Desc Input/Text Area` |
| `Shell/AllianceMemberTab.cs:1513`（已入盟改盟描述框） | 1 | `Social Submenu Variant/…/GeneralDetails/Content/Description input text/Text Area` |

> ⚠️ **38 ≠ 4**：其余 34 处**不由本类建**（本类只服务 `GenericPromptWindow` 那一族的版面：
> 900×90 行 + `40K_dropdown_bg` 底 + 40/45px 字）。⛔ 别把「38 处全建了」写成已完成。

---

## §4 ❓ 剩余站点（⛔ 白名单外，交调度台分派）

这些**同样是 `Text Area` 节点、同样没有按 pad 裁**，但**不在本包白名单**（各自属于 W1 / W14 / 其他文件）：

| 文件:行号 | 现读 | 原版节点 | 判定 |
|---|---|---|---|
| `Shell/SettingsWindow.cs:6757` | `MenuDraw.Node(f.Root, "Text Area", area)` —— 裸节点 | `Main Menu Settings Window/…/InputEmail`、`InputPassword`（账号页 + 登录窗共 4 处） | 🟠 未裁（`SettingsWindow` 自己那套 `MenuInputField`） |
| `Shell/ProfileTab.cs:1330` | `Node(fld, "Text Area", …)`，且 `:1329` 注释**已过期**（「我们没有掩码体系」—— 现在有） | `ChooseNameWindow/Choose Name Input Field/Text Area` · `Player Profile Window/…` | 🟠 未裁 + 注释待订正（铁律 5） |
| `Shell/GiveFeedbackPopup.cs:512` / `:696` / `:706` | `MenuDraw.Node(..., "Text Area", …)` × 3 | `Give Feedback Popup menu/…/Input field/Text Area` + `…/S2Q4` `S4Q2_Extra` `S4Q3_Extra` 三格各自的 `Text Area/Text Area`（共 4 处） | 🟠 未裁 |
| `Shell/ReferralPopupWindow.cs:499` | 裸节点，但 `:506` 显式 `MenuDraw.ClipText(_inputText, TextClipRect(), …)`（`TextClipRect()` = `PaddedRect(TextAreaR, (-8,-5,-8,-5))`） | `Referral Popup/window/content/Input View/input/Text Area` | 🟢 **已按 pad 裁**（显式形参那一路，不是节点）；⚠️ **`Placeholder` 那一颗没裁** |
| `Shell/ChatPanel.cs`（`Bind` 路，`:179/:277` 建的两颗字） | 走 `ChatPanel` 自己的 `ClipText` 包装（`:776`），框 = `ChatTab/Viewport` | `ChatPanel/…/InputField (TMP)/Text Area` | 🟡 被**页视口**裁，不是 `Text Area` 的 pad（差异小，但要改属 ChatPanel 的账） |
| `Shell/AllianceSearchTab.cs:443` / `Shell/FriendsTab.cs:593` | `TextInputField.Bind`（宿主自己画两颗字，本类**不建视觉件**） | `Social Submenu Variant/…/Search Field/Text Area` · `Friends Menu Demo/…/Player Input field/Text Area` | 🟡 同上（被外层 `Viewport` 裁，不是 `Text Area` pad） |

⇒ **建议调度台**：`SettingsWindow` / `ProfileTab` / `GiveFeedbackPopup` 三处（共 **8 个节点**）各按本包同一写法补
（`ViewportClip.Hang(...)` + `ClipText(..., null, ...)`），`ProfileTab.cs:1329` 那句过期注释同批订正。

---

## §5 断言

**新建** `CardPresentation/Editor/B141_W25_Assert.cs`（`class B141W25_Assert`，**全局命名空间**）。

| 段 | 条数 | 内容 |
|---|---|---|
| A（夹具实建） | **26**（含 8 条前提） | 自建临时根节点 → `TextInputField.Create` → 查 `template/Text Area`；pad **逐分量对字面量** (−8,−5,−8,−5)；softness (0,0)；`HasBaseRect` + `BaseRect` = (110,106.5)-(990,183.5)；`State.RenderClip` = **(102,101.5)-(998,188.5)**；外扩量 W+16 / H+10（结构性判别式）；两颗 `Label` 各挂 `ClippedTextGuard` |
| B（源码结构） | **7** | `B0` 读得到源码 · `ViewportClip.Hang(go, AreaNodeName` 恰 1 · `MenuDraw.Node(go, "Text Area"` 恰 **0** · 两句 `ClipText` 各 1 · pad 字面量 1 · 节点名常量 1 |
| **合计** | **33** | 前提条不成立时**当场红**（⛔ 不是 `if (前提) Check(结论)` 那种静默跳过；前提红时后面的条会提前 return、实际条数随之变少） |

**灭自证**（🧨 每条怎么才能红）：

- pad 改回 `Vector4.zero`（或改成正值）⇒ A2–A5 **四红** + A8a–A8d **四红** + A9 两红 + B5 红；
- 退回 `MenuDraw.Node`（只建节点）⇒ A1/A8/A10/A11 红 + B1（0 处）/B2（1 处）红；
- 删掉 `MenuDraw.ClipText` 任一句 ⇒ A10 或 A11 红 + B3/B4 红；
- **两条判据不共因**：A2–A5 是「值对不对」、A9 是「框有没有真的外扩」、B1/B2 是「写法换没换」——
  只改其中一处都拦得住。

**期望值来源**：全部是**prefab 字面量**（`-8/-5`）＋ **算术可复算的框**（880×77 ± pad），
⛔ 不从被测实现自己的常量反算（否则「常量与断言一起改」就能蒙过）。

**接线（本包不动宿主）**：宿主 = `CardPresentation/Editor/MainMenuScene.cs`（同族 `B102ALLY_Assert` 在 `:18538`，
`TextInputField.Create` 的站点都在 SocialWindow 那条链上）。在**合计行之前、同一物理行、该行第一个 `//` 之前**追加：

```csharp
B141W25_Assert.Run(); _sink.Pass += B141W25_Assert.Pass; _sink.Fail += B141W25_Assert.Fail;
```

---

## §6 验证

- `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w25 bash 工具/typecheck.sh`
- **最终结果：`运行时错误数: 0` · `编辑器错误数: 0`**（本包两个文件都编过）。
- 过程留痕：前三轮里有一次运行时是 **1 错**，停在 `Assets/CardPresentation/Shell/MissionsTab.cs(2849,1) CS1022`
  （**不是本包的文件**，W14 写手在飞、文件尾未闭合），那一轮的编辑器 `CS0006`（找不到 `WFCheck.dll`）
  是**级联假错**；对方收笔后复跑即 0/0。
- 源码闸自校验（断言 B 段那 6 个字面量，`python` 现数）：`1 / 0 / 1 / 1 / 1 / 1` —— 与断言期望一致。
- 文件行尾：`TextInputField.cs` = 纯 LF、`\r` 无裸 CR（`crlf 0 · lf 422 · lone_cr 0`）。
- 报告自身表格列数：`工具/check_md_tables.py` 按**每张表分段**跑 ⇒ 6 张表 `bad rows: 0`。
- ⚠️ **没有跑 Unity**（红线）：断言未实际执行，接线由调度台完成。

---

## §7 ❓ 未查清 / 待办

1. **38 处里我们只覆盖了 4 个实例**（§3、§4）—— 其余 34 处分散在 6 个别的文件，**不在本包白名单**。
2. `ReferralPopupWindow` 那条只裁了 `Text`、**`Placeholder` 没裁**（同一个 `RectMask2D` 在原版两颗都管）。
3. `ChatPanel` / `AllianceSearchTab(搜索框)` / `FriendsTab` 走 `Bind` 路，裁的是**外层页视口**而不是
   `Text Area` 的 `(-8,-5,-8,-5)` —— 视觉差异小（pad 只有 5–8px），但**不是原版那条掩码**，已列入 §4。
4. 本包**没有跑 Unity**（红线）：断言只做了类型检查，**没有实际运行**；接线由调度台在 `MainMenuScene` 合计行前完成。
