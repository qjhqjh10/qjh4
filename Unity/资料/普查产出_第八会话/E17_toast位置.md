# E17 —— toast 通道落点改成【菜单/外壳那一档】（`A1050` 后续）

日期 2026-10-09 · 执行代理 E17 · 白名单两件（`Battle/ErrorMessageBanner.cs` / `Shell/MessageToast.cs`）

---

## ① 结论

1. ✅ **做完了**：给 `Battle/ErrorMessageBanner.cs` 加了**场景档** `LayoutPreset`（`BattlePreset` / `MenuPreset`），
   `Shell/MessageToast` **显式传 `MenuPreset`**；`BattleDriver.BuildErrorBanner` **不传** ⇒ 战斗档。
   **战斗侧行为逐位不变**（不传档那条重载就是「加档之前那一份」，数值我按 float32 逐位核过，见 ②·4）。
2. 🔴 **但「两场景只差根的一个 `ap`」这条前提是【错的】**（我现读订正；上一轮 E7 报告 `:110` 也是这么写的）：
   两场景**总共差 6 项**，其中**项高**那一项**直接决定位置**。逐条实读见 ②·2。
   ⇒ 菜单那一档的条心**不是 296.64 px，而是 `311.6400146484375` px**（差 15 px = 项高差的一半）。
3. ✅ 现在外壳侧那一颗：**条心自重而下 `311.6400146484375` px**（= 原版主菜单那一档）；
   战斗侧仍是 `510.23974609375` px（与 `BattleScene.cs:11795` 那条断言逐位相同）。
4. ⚠️ 量出来的差：`510.23974609375 − 311.6400146484375 = 198.5997314453125` px
   = **根那一档 213.5997314453125** 减去 **项高那一档 15**（`80/2 − 50/2`）。
   任务书里写的「213.6 px」只算了前一半。
5. ⚠️ ⑤ 那一格（主菜单那颗 `m_IsActive = false`）**仍然没查清** —— 多了一条旁证、仍推不出解释（见 ④·1）。

---

## ② 证据

### ②·1 那条不变量怎么来的（任务要求「先读通」，逐句拆）

`Battle/ErrorMessageBanner.cs`（改后行号）：

| 步 | 代码 | 干了什么 |
|---|---|---|
| 1 | `Create` → `go.transform.localPosition = LayoutSpace.FromPixel(RootCx, preset.RootTopPx + RootH*0.5f)` | 把**绝对屏坐标**（设计 px，左上原点）当 `localPosition` 写 ⇒ 只有当「父链 = 原点 + 单位缩放」时它才真是绝对屏坐标 |
| 2 | `Build()` → `container.localPosition = FromPixel(容器中心绝对) − transform.localPosition` | 减掉根的 `localPosition` ⇒ 得**相对根的**偏移（绝对值做差） |
| 3 | `RelayoutActive()` → `it.root.localPosition = _container.InverseTransformPoint(FromPixel(960, centerY))` | 🔴 **把那个世界点反解回子节点本地坐标** ⇒ `_container.TransformPoint(local)` **恒等于** `FromPixel(960, centerY)` |

⇒ **「从外面挪不动」的根因在第 3 步**：条目的**世界**位置由这一句**钉死**在绝对屏坐标上，
与宿主（`MessageToast` 那个 GO / 它上面的 `Safe area Only Horizontal`）的位置、缩放**无关**。
（容器与根那两级在宿主有位移时**会**跟着挪，但承载可见物的是条目 ⇒ 视觉位置钉死。上一轮验过这条我复核了，
矩阵推法：`root.pos = P.pos + P.scale·L_root`、`container.pos = root.pos + root.scale·(A − L_root)`，
两项抵消后 = `P.pos + A`（容器会跟宿主走）；但条目走 `InverseTransformPoint` ⇒ 精确回那个世界点。）

**`−213.6` 从哪来**：`RectTransform_3373.json` 的 `m_AnchoredPosition.y = −213.5997314453125`
+ `m_AnchorMin/Max = (0.5, 1)` + `m_Pivot = (0.5, 1)` ⇒ **根的上缘在屏顶下 `213.5997314453125` px**。
它一路往下的传导（都是实读值的加减）：

```
根上缘            = −ap.y                       = 213.5997314453125        （菜单档 = 0）
容器上缘          = 根上缘 + (RootH − ContH)     = 213.5997314453125 + 70.3500061035156 = 283.94973754882813
单条条心(项高 h)  = 容器上缘 + (ContH − h) + h/2 = 容器上缘 + ContH − h/2
                  h = 80（战斗）⇒ 283.94973754882813 + 266.2900085449219 − 40 = 510.23974609375
                  h = 50（菜单）⇒   0.0           + 266.2900085449219 − 25 = 311.6400146484375  ← 外壳这一档
```

### ②·2 两场景**到底**差哪几项（全部自己复读 `assets_full` 的原始 json）

| 项 | 战斗 `bundle_scenes_scenes_battlearena1` | 主菜单 `bundle_scenes_scenes_mainmenuwarpforge` | 出处（PathID） |
|---|---|---|---|
| 根 `m_AnchoredPosition` | `(0, **−213.5997314453125**)` | `(**−0.00010299999848939478**, **0.0**)` | `RectTransform_3373` / `RectTransform_1206` |
| 根 `m_SizeDelta` | `1920.699951171875 × 336.6400146484375` | **同** | 同上 · `m_AnchorMin/Max = (0.5,1)` · `m_Pivot (0.5,1)` 两档同 |
| 容器 `RectTransform` | `anchor (0,0)-(1,0)` · `ap (0,133.14500427246094)` · `sd (0,266.2900085449219)` · `pivot (0.5,0.5)` | **逐位同** | `RectTransform_3154` / `RectTransform_1205` |
| 容器 `VerticalLayoutGroup` | `m_ChildAlignment 7` · `m_Spacing 0` · `m_ChildControlWidth/Height 0` · `padding 0` | **逐位同** | `MonoBehaviour_4106` / `MonoBehaviour_2086` |
| **item `m_SizeDelta`** | **`1439.1600341796875 × 80`** | **`1310 × 50`** | `RectTransform_2823` / `RectTransform_1208` |
| item 存档 `ap` | `(960.3499755859375, −241.29000854492188)` · `anchor (0,1)` | **逐位同** | 同上 |
| **文字 `m_fontSize`** | **`60`**（`m_fontSizeBase` 同 · `enableAutoSizing 0` · `H 2 / V 512`） | **`36`** | `MonoBehaviour_3740` / `MonoBehaviour_1740`（都挂 `m_GameObject = 文字 GO`） |
| **文字 `m_SizeDelta.y`**（= 原版单行高） | **`56.849998474121094`** | **`34.11000061035156`** | `RectTransform_2942` / `RectTransform_1207` |
| **`Background` `m_SizeDelta`** | **`870.97998046875 × 60.849998474121094`** | **`618.5899658203125 × 38.11000061035156`** | `RectTransform_2989` / `RectTransform_1204` |
| 四段时长 | `0.3 / 0.15 / 2.0 / 0.25` | **逐位同** | `MonoBehaviour_4710` / `MonoBehaviour_2087` |
| 根上另两颗 | `Canvas_2577`（`m_RenderMode 2` · `m_OverrideSorting 1` · `m_SortingOrder 1` · `m_SortingLayerID 1054366423`）· `MonoBehaviour_5029`（`errorMessageItemRef → 4710` · `messageColor (1,1,1,1)` · `errorColor (0.8,0,0,1)`） | `Canvas_1076` **逐位同** · `MonoBehaviour_2356`（`errorMessageItemRef → 2087`）**其余逐位同** | 两包 `Canvas/` `MonoBehaviour/` |
| item 子件结构 | `RT 2823` + `CanvasGroup_3577` + `MB 4710` | `RT 1208` + `CanvasGroup_1626` + `MB 2087`（**同形**） | `GameObject/Error Mensage_Ref.json` |

> ⚠️ **导出树里 GO 是「按名字存文件」的** —— 同名 GO 会撞车（两个包里 `GameObject/Container.json` 是**两个不同节点**：
> 主菜单那个真容器被导成了 `Container_132.json`）。**我这次是按「组件 PathID」建索引**找 GO，别按文件名找。

🔴 **item `sd` 的高为什么决定位置**：容器那个 `VerticalLayoutGroup` 是 **`m_ChildControlHeight = 0`**
⇒ uGUI 取**子件自己的 `sizeDelta`** 当它这一轴的长度（`LayoutGroup.GetChildSizes`：
`if (!controlSize) { min = child.sizeDelta[axis]; preferred = min; flexible = 0; }`），
而 `SetChildAlongAxisWithScale` 竖轴那一支写的是 `anchoredPosition.y = −pos − size·pivot.y`
⇒ **项高就是堆叠步长**，条心 = `容器上缘 + ContH − 项高/2`。

🔴 **硬旁证（不靠算法推，看存档值）**：菜单那条 item 的**存档** `ap.y = −241.29000854492188`
**正是 h = 50、N = 1 时布局算法会写出来的那个值**：`−(266.2900085449219 − 50) − 50×0.5 = −241.290009`。
若 h = 80，它该是 `−226.29`。而且**菜单那份 item 的 GO 是 `m_IsActive = true`**（战斗那份模板是 `false`）
⇒ 菜单场景里**布局是真跑过的**，那个存档值就是它的输出。⇒ 菜单档项高**确实**是 50。

### ②·3 两档各是多少（改后）

| | 根上缘 | 容器上缘 | 项高 | 单条条心 | 字号 | 单行高 | 参考底条 |
|---|---|---|---|---|---|---|---|
| **战斗档** `BattlePreset` | `213.5997314453125` | `283.94973754882813` | `80` | **`510.23974609375`** | `60` | `56.849998474121094` | `870.97998046875 × 60.849998474121094` |
| **菜单档** `MenuPreset` | `0` | `70.3500061035156` | `50` | **`311.6400146484375`** | `36` | `34.11000061035156` | `618.5899658203125 × 38.11000061035156` |

> ⛔ 两档的**存档 `ap.y`** 都是 `−241.29000854492188`（→ 存档位 = 上缘 + 241.29 = `525.23974609375` / `311.6400146…`：
> 菜单那档**恰好**与布局结果同值，因为它的项高就是 50）—— ⛔ **别把存档值当条心**（战斗那档差 15 px）。

### ②·4 「战斗档逐位不变」的自证（float32 实算，不是眼估）

```
ContTopY        = RootTop + (RootH − ContH)          = 213.5997314453125 + 70.3500061035156   = 283.9497375488281  ← 旧常量 283.94973754882813f 的同一个 float
单条条心         = ContTopY + (ContH − 80) + 40                                                        = 510.23974609375   ← 与旧实现/BattleScene 断言逐位相同
模板位 y         = ContTopY − ItemApY = 283.9497375488281 + 241.29000854492188                          = 525.23974609375
容器中心 y       = ContTopY + ContH/2                                                                   = 417.09474182128906
根中心 y         = 213.5997314453125 + 336.6400146484375/2                                              = 381.91973876953125
```
（同一算式我用 `numpy.float32` 逐项算过，偏差 0。）

**外加一次「照代码读数值 vs 照资产读数值」的对账**（脚本对 14 项逐项比，容差 1e-9）：**14/14 OK、`bad = 0`**，
对账项 = 两档的 `RootTopPx` / `ItemWPx` / `ItemHPx` / `RefBarWPx` / `RefBarHPx` / `FontPx` / `RefLineHeightPx`
（分别对 `RectTransform_{3373,1206,2823,1208,2989,1204,2942,1207}.json` 的 `m_AnchoredPosition.y` / `m_SizeDelta`
与两处 TMP `m_fontSize`）；同时算出两档条心 = `510.23974609375` / `311.6400146484375`。
（本批是纯读脚本、不落盘 ⇒ 没有 `_tmp` 残留。）

---

## ③ 改动清单

### ③·1 `Unity/MyGame/Assets/CardPresentation/Battle/ErrorMessageBanner.cs`（+193 / −53 · `git diff --numstat` 原文：`193	53	…/ErrorMessageBanner.cs`）

| 处 | 改动 |
|---|---|
| 文件头 **⑪（新增）** | 「两场景不是逐字段相同」那张 13 行表 + 旁证 + 菜单档条心 `311.6400146484375` + 为什么这算「位置」的一半 + 「做法 = 开档位」 |
| 文件头 A · `ItemCenterPxOf` doc · `TemplateBarSizePx` doc | 把写死的战斗档数值改成「本档」口径（含菜单那一档的对值） |
| 几何常量区 | 删 `RootCy` / `ContTopY` / `ItemW` / `ItemH` / `TextFontPx` / `OrigLineHeightPx` / `RefBarW` / `RefBarH` 八个常量（**值全进了 `BattlePreset`**）；新增 `BattleRootTopPx = 213.5997314453125f` 与 `ContTopFromRootTop = RootH − ContH`（两档相同的推导量） |
| **新增 `public struct LayoutPreset`** | 六项：`RootTopPx` · `ItemWPx/ItemHPx` · `RefBarWPx/RefBarHPx` · `FontPx` · `RefLineHeightPx`（每项都带两档实读值 + 出处 PathID） |
| **新增 `BattlePreset` / `MenuPreset`** | `public static readonly`；`BattlePreset` 的六个数**就是**原来那八个常量里的六个（另两个 `RootCx`/`ItemApX` 两档相同 ⇒ 留常量） |
| `_preset` 字段 | `LayoutPreset _preset = BattlePreset;`（⇒ 老路径默认值 = 加档之前的行为） |
| `Create` | 拆成两个重载：`Create(Transform)`（**= 战斗档**，`BattleDriver` 那条路一字未改）与 `Create(Transform, LayoutPreset)`；后者在 `Build()` **之前**写 `b._preset` |
| `Build` / `BuildTemplateChildren` / `BuildItemNode` / `ShowItem` / `RelayoutActive` | 六处读常量改为读 `_preset.*`（位置、项高、字号、单行高、参考底条、兜底尺寸） |
| **新增 4 个只读自检口** | `Preset` · `RootTopPx` · `ContainerTopPx` · `ItemHeightPx` · **`SingleItemCenterYpx`**（= `RootTopPx + RootH − ItemHPx/2`，**算式只留这一份**） |

### ③·2 `Unity/MyGame/Assets/CardPresentation/Shell/MessageToast.cs`（**200 → 233 行**；⚠️ 未跟踪文件、无 git 基线，我逐行读过改后全篇）

| 处 | 改动 |
|---|---|
| 文件头 ④ | 🔴 **就地订正**（铁律 5）：删掉「子树逐字段相同、只有根 `ap` 不同」，换成 ②·2 那张表的 6 项差异 + 硬旁证；并写明「上一版的 296.64 是按项高 80 算的、已作废」 |
| 文件头 ⑤ | 保留「没查清」，**补一条新旁证**（item 子件 `m_IsActive = true` + 存档 `ap.y` = h=50 的布局输出 ⇒ 编辑器里布局跑过），并写明**它只解释了一半** |
| 文件头「已知偏离」整节 | 删（已消掉），换成「✅ 那 213.6 px 已消掉」+ 档的定义指向 `ErrorMessageBanner.cs` 文件头 ⑪ |
| `Ensure` | `ErrorMessageBanner.Create(t.transform)` → **`Create(t.transform, ErrorMessageBanner.MenuPreset)`**（带注释写清「不传 = 战斗档、会低 198.6 px」） |
| 新增自检口 | `Preset`（转发 `_banner.Preset`）· `SingleItemCenterYpx`（转发；没建件 ⇒ `NaN`） |
| ⛔ 未碰 | `DeckInfoPopup.cs`（另一个写手已交件）· 其余一切黑名单文件 |

### ③·3 类型检查 / 行尾 / 未跑的东西

| 项 | 读数 |
|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_e17 bash d:/4/Unity/工具/typecheck.sh` ⇒ **`运行时错误数: 0` / `编辑器错误数: 0`** |
| 行尾 | 两个文件改前改后都是**纯 LF**（二进制数：`ErrorMessageBanner.cs` CRLF 0 / LF 817 · `MessageToast.cs` CRLF 0 / LF 233）；⛔ 没用过 `sed -i` |
| Unity 自检 | **一条没跑**（红线：本代理不许跑 Unity） |
| git | 只跑了 `git diff --numstat`（读）· ⛔ 没做任何 git 写操作 |

**建议主对话跑哪几条**：本条**只动了一个自检宿主**（`BattleScene.cs` 那个宿主里有一整节 `ErrorMenus` 断言 ——
`:11695-11830`）⇒ 按铁律 12 第 1 条**只需 `BattleScene.Run`**；
⚠️ 另外**外壳侧的宿主现在还不在任何自检里**（`Editor/*.cs` 全库 0 处引用 `MessageToast`，我 grep 过）
⇒ 那条通道**今天没有任何断言覆盖**（见 ④·3）。

---

## ④ 没查清的部分（⛔ 不猜）

1. **主菜单那颗 `m_IsActive = false`**（任务附带的只读题）。**仍然没查清**，但多了一条旁证：
   · 菜单那份 **item 子件的 `m_IsActive` 是 `true`**（战斗那份是 `false`），而它的**存档 `ap.y` = h = 50 时布局的输出**；
   · 布局**只对激活的子树跑**（`LayoutGroup.cs`：`if (rect == null || !rect.gameObject.activeInHierarchy) continue;`）
     ⇒ 「根**曾经**是活的、后来被关掉（而布局写下的值留在节点上）」**与资源自洽**；
   · ⛔ 但它**说不出运行时是谁把它打开的**。本地**没有主菜单的实况 dump**（`Unity参照管线_0825/data/` 里只有 `runtime_ui_dump_Battle_Arena_1` 一族）
     而 14 个脚本读 `Instance` + `ShareDeck.c:21` 对 null 抛异常 ⇒ 运行时它必须活着。
     **结论照旧：本地资源与代码推不出这一格的解释**。
2. **菜单档的「单行高 / 字号」是否要用在我们的 toast 上** —— 我按**完全复刻**口径做了（字号 36 · 单行高 34.11 · 参考底条 618.59×38.11）。
   ⚠️ 若调度台认为「外壳的字号该跟我们其它界面一致」，那就只把 `FontPx`/`RefLineHeightPx`/`RefBarW/H` 四项改回战斗档的值即可
   （**位置那一半不受影响**：条心只由 `RootTopPx` + `ItemHPx` 决定）—— **一个字段的事**，我把它做成参数就是为了留这条路。
3. **外壳侧条心 `311.6400146484375` 目前没有任何自检覆盖**（`Editor/*.cs` 零引用 `MessageToast`）。
   要钉的话，最省的一条：`ShellScene.Run`（或 `MainMenuScene.Run`）里
   `var t = MessageToast.Ensure(); MessageToast.Show("x", false, "y");` 后断
   `Mathf.Abs(t.Banner.ItemCenterPxOf(0).y - t.SingleItemCenterYpx) < 0.25f`
   **且** `Mathf.Abs(t.Preset.ItemHPx - 50f) < 1e-4f`（第二条是「灭自证」用的：只断条心的话，
   把 `MenuPreset` 整个换成 `BattlePreset` 也会因为两边一起变而绿）。
   ⚠️ 这条要**新开自检宿主**，`Editor/*.cs` 不在我的白名单 ⇒ **归调度台**。

---

## ⑤ 顺手发现的东西

1. 🔴 **上一轮那条「两场景子树逐字段相同」是过度概括** —— `资料/普查产出_第八会话/E7_弹窗星号与toast.md:110`
   与 `:111`（含 `296.63974609375` 那个数）**现在都不对**。该文件不在本批白名单，**请调度台按铁律 5 订正**
   （订正文案我已写进 `MessageToast.cs` 文件头 ④，可整段搬）。
   ⇒ 教训同族：**「比了三处都一样」≠「逐字段相同」** —— 它只比了根 / 容器 / 时序 MB，**没比 item 与文字那两颗**。
2. 🔴 **导出树按 GO 名存文件 ⇒ 同名 GO 撞车**（主菜单的真容器被导成 `Container_132.json`，
   而 `GameObject/Container.json` 是**另一个节点**，6 个组件、看着还挺像）。
   **新版判据**：按**组件 PathID**（`m_Component[].m_PathID`）建索引反查 GO，⛔ 别按文件名找。
3. ⚠️ **我们这条通道（战斗侧也一样）把条目钉死在绝对屏坐标 ⇒ 不走安全区**。
   原版那颗挂在 **`Safe area Only Horizontal`** 下面、`anchor (0.5,1)` ⇒ 位置**跟安全区节点走**；
   我们这句 `InverseTransformPoint(FromPixel(…))` 把世界位置钉死 ⇒ 宿主（含安全区节点的 `localScale/localPosition`，
   `ShellParts.cs` 里 `UISafeArea` 会写这两个值）挪不动它。
   异形屏 / 极端宽高比下这会与原版有差（本地无实况、没量过差多少）⇒ **记一笔，归调度台**，不是我这一批的活。
4. ⚠️ 两档的**根 `ap.x` 差 0.00010299999848939478**（战斗 `0` / 菜单 `−0.00010`）—— 我**没给它开档位**
   （0.0001 px 无意义，如实写进文件头 ⑪ 那张表）。
5. ✅ 顺带核过：`ErrorMessageBanner` 的脚本 guid（`cb428214e55f49deb4bfbfd145f66ed7`）**在 `Assets/**` 的 `.unity` / `.prefab` 里 0 处引用**
   ⇒ 它**永远不会被序列化进场景** ⇒ 新加的 `_preset` 字段不存在「老场景反序列化成 0 ⇒ 位置飞掉」那种风险
   （若将来有人把它存进场景，`_preset` 就会吃序列化值 —— 现在这条路是断的，如实记）。
