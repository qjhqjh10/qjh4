# W · `DeckRuntime.cs` CS1570 收尾 + 两条新账判据查证（2026-10-18 第三会话）

写手：执行代理。允许文件 = `Deck/DeckRuntime.cs` · `Shell/MenuScroll.cs`（判据支持才改）。
**实际改动只有 `DeckRuntime.cs`（6 行注释）**；`MenuScroll.cs` **未动**（判据不支持改）。

## ① CS1570：`Assets/CardPresentation/Deck/DeckRuntime.cs` —— **14 → 0**
- ⚠️【按文件汇总】那格原来报 **27** = **14 条 CS1570 + 13 条 CS1573** 的和（CS1573「缺 `<param>`」本轮不碰）。
- 命令 `WF_DOC=1 TMPDIR=/tmp/wf_dr bash d:/4/Unity/工具/typecheck.sh`，**只看「按文件汇总 · 全集」那一节**；为拿全量另对 `wf_csc_doc.rsp` 重跑 csc 并按文件过滤（⛔ 没拿 `head -20` 当账单）。
- **CS1591 指纹（证明编译器真跑了）**：运行时 **4399** · 编辑器 **580**（改前改后同值）；`运行时错误数: 0` · `编辑器错误数: 0`。

**逐根因行（5 处，不是上一批记的 8 —— 那 8 个里 3 个是连带的报错行）**

| 行 | 病灶 | 改法 | 原报条数 |
|---|---|---|---|
| 1821 | `iVar6 < iVar7`（裸 `<`） | `&lt;` | 2 |
| 1827 | —— **纯连带**（「`>` 结束标记 `iVar7`」） | **不改** | 1 |
| 2154 | `全部 < 0.83828` | `&lt;` | 2 |
| 2406 | `deg &gt; 60 && deg &lt; 120` —— `>`/`<` 早转好了，**漏的是裸 `&&`** | `&amp;&amp;` | 2 |
| 2448 | `IDropHandler<CosmeticItem>`（裸 `<`/`>` ⇒ 被当成未闭合元素） | `&lt;` `&gt;` | 3 |
| 4587 | `IDropHandler<…>`（同上） | `&lt;` `&gt;` | 3 |

**🔴 就地订正简报的一条前提（铁律 5：留更正痕迹）** —— 简报写「这一族**全是裸 `<X>` 的连带，一件都不是真漏闭合** ⇒ 一处都不用补标签」：**前半句对，后半句对第 2450 行不成立**。
- 转义 2448 之后 **2450 那两条仍在、且换了对象**：「结束标记 `summary` 与开始标记 **`para`** 不匹配」+「元素 `summary` 需要结束标记」。
- 真因：**2447 的 `<para>` 从未闭合**（2448~2450 之间没有 `</para>`，2450 直接 `</summary>`）——2448 那颗未闭合的 `<CosmeticItem>` 原来**把 `para` 挡在最里面**（解析器只报最内层）⇒ 上一轮才读成「无需补标签」。
- 处置：**只此一处** —— `DeckRuntime.cs:2450` 行尾 `</summary>` 前补 `</para>`。依据：同文件同族一律 `<para>`…`</para>`（`1815/1827` · `1829/1831` · `2152/2155`）⇒ 是漏写；**零代码语义变化**。若调度台不认，回退这一处 = 只剩这 2 条，其余 12 条已清。
- 教训（建议进坑表）：**「上一批说过不用补标签」不能当后续豁免** —— 前一条裸标签会**遮住**后面那条真漏闭合，**转义一层要再跑一次**才看得见下一层。

**行尾**：`DeckRuntime.cs` 改前改后**纯 CRLF**（`b.count(b'\r\n')` = `b.count(b'\n')` = **6340**，`file` 报 CRLF）。`git diff --numstat` = **96/18**，其中绝大部分是**本会话之前**就有的未提交改动，本轮只 ±6。⛔ 未用 `sed -i`。
✅ `Shell/MenuScroll.cs` 那 1/1 是**别人的旧改动**（mtime 07:15，内容是「别抄行号」那句注释），**不是本轮**。

## ②a 卡背【格】命中区 —— **判据：原版 = 整格 250×405 ⇒ 【不改】**
包 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（按 `m_Script` / pid 反查，JSON 逐字段亲读）。
⚠️ `Image` 在该包以 **`MonoBehaviour`** 落盘（`m_Script.m_PathID = 350208831926335389`）—— **字段齐全，不是「没导出」**。

树（根 `Collection Cosmetic` pid `-586895015824482954`）：

| 节点 | RectTransform | Image 关键字段 |
|---|---|---|
| `Collection Cosmetic`（根） | 250×405 · anchor(0,1)-(0,1) · sizeDelta(250,405) · pivot(0,1) | **无 Image**（CanvasRenderer + MB `3975876605969042202` = `CollectionCosmetic`） |
| └ `Cardback Container` | 拉伸满格 · anchor(0,0)-(1,1) · sizeDelta(0,0) | **无 Image** |
| 　├ `Cardback Shadow SDF` | anchor(**−0.17,−0.185**)-(**1.18,1.175**) · sizeDelta(0,0) ⇒ 337.5×550.8（格坐标 −42.5,−70.875） | **`m_RaycastTarget: 0`** · `m_PreserveAspect: 1` · `m_RaycastPadding (0,0,0,0)` · `m_Type: 0` · `m_UseSpriteMesh: 1` |
| 　└ **`Cardback`** | **拉伸满格 = 250×405** · anchor(0,0)-(1,1) · sizeDelta(0,0) · pivot(.5,.5) | **`m_RaycastTarget: 1`** ← ★ 唯一 raycast 目标 · `m_PreserveAspect: 1` · `m_RaycastPadding (0,0,0,0)` · `m_PixelsPerUnitMultiplier: 1.0` · `m_Sprite: null`（运行时 `Config` 塞） |

**运行时会不会改矩形？不会 —— 三条证据**：① 三节点上**没有** `AspectRatioFitter` / `ContentSizeFitter` / `LayoutElement`，`Cardback` 的 sizeDelta 恒 0、anchor 恒 (0,0)-(1,1) ⇒ 恒 = 父格 250×405；② `decomp_full/CollectionCosmetic__Config.c` 全文**只** `Image.set_sprite` ×2 + 一次材质色调用、**不碰任何 rect**（`__HoverHighlight.c` 只改材质色，`__.ctor.c` 只取 shader property id）；③ `CollectionCard__OnRectTransformDimensionsChange.c` **只属 `CollectionCard`（卡牌格）**，`CollectionCosmetic` 没有这一支。

**🔴 为什么 `m_PreserveAspect = 1` 管不着命中（本地 UGUI 源码逐行读过）**
- 命中先由 `GraphicRaycaster.cs:327` 判 `RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, …, graphic.raycastPadding)` —— 吃**整个 RectTransform 矩形** + `m_RaycastPadding`（原版全 0），**与内接后的网格无关**。
- 再走 `Image.IsRaycastLocationValid`（`com.unity.ugui@27635d171b1a/…/Image.cs:1899`）：**首句 `if (alphaHitTestMinimumThreshold <= 0) return true;`** —— 那个 `preserveAspect` 分支（`:1916-1917`）**只在 alpha 阈值 > 0 时才走**。
- 而 `m_AlphaHitTestMinimumThreshold` 在 `Image.cs:597-598` 写着 `// Not serialized until we support read-enabled sprites better.` + `private float … = 0;` ⇒ **不序列化 ⇒ 装载后恒 0**（旁证：`assets_full` 的 `bundle_menus_assets_all` 与 `battlearena1` 两包 `grep` 该字段 = **0 命中**）⇒ **那一支是死代码**，`m_PreserveAspect` 对命中区**零影响**。

**结论：不改。** 原版命中区 = `Cardback` 的 Image 矩形 = **整格 250×405**（padding 0）∩ `RectMask2D` 视口；我方 = `AddHit` 建在 `cell` 上、矩形 = 整格 `r`（`MenuDraw.Hit` 自带「∩ clip、整块在外不建」）⇒ **逐条等价**。改成「按贴图比例内接后的更小矩形」**才是偏离**。附带确认：上一批改的**渲染**那一半（`keepAspect: true`）**只动网格**、与命中无关 ⇒ 两半各自正确。

**顺手订正简报一处坐标（只报告）**：简报说「`DeckRuntime` 的卡背格是 `AddHit(cell, "Hit", r, …)`」—— **`DeckRuntime.cs` 里 `AddHit` 零命中**，真身在 **`Shell/CollectionWindow.cs:1169`**（`RebuildCosmoCells`）；`DeckRuntime` 那份卡背页走**自算命中** `CosmeticNameAt`（`DeckRuntime.cs:2227-2237`，整格网格算术）⇒ 结论一致。

**多实例（铁律 4：先解父链）**：同包内另有 `GameObject/Collection Cosmetic.json`（`active=false`）—— **不是**这颗：**另一颗脚本**（`-3422054156208822308`）· 无 `Cardback` 子件 · 带 `CanvasGroup` · 父 RT `-3820370437395452124` = `Content Area > Cosmetic Display > Cosmetic Drag Controller`（`DeckRuntime.cs:2243` 注释已把该 pid 记成这个节点）⇒ 判为**拖拽幽灵模板**。**两实例各自都对。**

**没查清 / 停手**：① 原版卡背格的**点击**语义（点一下开什么窗）本轮**没判** —— 基类 `CollectionItem<T>` 在 `decomp_full/` 里**没有方法体文件**（68 个 `Collection*.c` 中 `CollectionItem*` 零命中）；**不影响 ②a**（本账只判命中区几何）。② 未真反编译 `Image__IsRaycastLocationValid` 去看那个分支的 VA —— 用的是「源码声明不序列化」+「两包零命中」两条证据，要更强判据可补那一刀。
③ 查过哪些包 / 哪些词：包 = `bundle_menus_assets_all`（`battlearena1` 只作 alpha 字段的反证）；词 = `CollectionCosmetic` · `CosmeticItem` · `CosmoCell` · `Collection Cosmetic` · `m_AlphaHitTestMinimumThreshold` · `CollectionItem`。

## ②b `MenuScroll.Intersects` 吃未内接的外框 —— **判据：不可观测 / 且改法本身是回归 ⇒ 【不改】**
**调用方清单（`grep 'Intersects' Assets/` 全量）**：生产代码 = `Shell/CollectionWindow.cs` 的 `:928`（卡牌格）· **`:1093`（卡背格 ← 本账）** · `:1603` · `:1930` · `:2052` · `:2185` · `:3010`，另有 `Shell/CampaignTab.cs` / `Shell/BattleLogTab.cs` 一族的 `_scroll.Intersects` ⇒ 与「卡背内接」相关的**只有 `:1093` 一处**。
钉住它的自检 = `Editor/ShellScene.cs:101-102` · `:2885-2889`（★两条求交入口逐格同答，19 处构建循环 vs `MenuDraw.Visible`）· `:5037-5052` · `:5276-5291`；`Editor/CollectionScene.cs:7362-7386`；`Editor/ShopScene.cs:4716-4736`；`Editor/RewardsScene.cs:4827`。
契约 = `MenuScroll.cs:66-67`「**整块在视口外就不建**」；`:404-410` 实现 = 一条**通用**「矩形 ∩ 裁切框」（`ClipNode` 优先、回落 `Viewport`）。

**可观测性：否**（三档都查）—— ① **画面**：内接由 `MenuDraw.Rect` **自己**做（`MenuDraw.cs:1356-1361` 先按贴图比例内缩，**再** `:1363-1376` 求交，整块在外 `:1370` **返回 null**）⇒ 落在空白带那格**画不出任何 quad**，**逐像素相同**；② **命中**：那格的 `AddHit` **本来就该建**（原版 raycast 吃整格，见 ②a）⇒ 用内接框去剔格子**反而丢掉原版有的命中区**；③ **自检 / 性能**：**没有任何断言**数「视口内交集几个卡背格」——最近的 `CollectionScene.cs:4358`（`Count > 0`）与 `:4707`（`Count <= wantFac` **上界**）**都是不等式**，多建少建都不红；多出的节点上界 = 空白带那点 = **每列至多 1 行**（≈6~7 个空 GameObject、各 0 quad）。

**结论：不改（`MenuScroll.cs` 零改动）。** 真要「对齐」，改的**不是 `Intersects`**（纯矩形谓词，拿不到贴图比例），而是 `:1093` 调用点传内接框 —— **那会同时踢掉原版确实存在的命中区（②a）= 回归不是修复**。契约 + 19 处调用方 + 一站自检都钉在这一条上，波及面远大于收益。**只报告。**

## 总表
| 账 | 结论 | 动作 |
|---|---|---|
| ① CS1570 | 14 → **0**（5 个根因行 + 1 处真漏闭合 `</para>`） | ✅ 改 `DeckRuntime.cs` 6 行（纯注释） |
| ②a 卡背格命中区 | 原版 raycast = `Cardback` 那颗 Image · 矩形 = **整格 250×405**（`RaycastTarget=1` · padding 0 · `PreserveAspect` 只管网格） | ⛔ **不改**（我方逐条等价） |
| ②b `Intersects` | 外框不可观测（画面逐像素同 / 命中本就该整格 / 无断言盯）· 改法本身是回归 | ⛔ **不改**（零改动） |
