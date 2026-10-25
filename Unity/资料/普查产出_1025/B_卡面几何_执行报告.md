# B · 卡面几何 / 卡牌线 4 件活 · 执行报告

> 执行代理 **B** · 2026-10-25 · 白名单：`Core/{CardView,CardArt,CardText,CardIcons,Badges,CardFeel}.cs`
> ＋ `Editor/{CardFaceProbe,CardBaseDemo,CardFaceAssert*}.cs`。
> **没跑 Unity**（红线①）· **没动 git**（②）· **没改两张正本**（③）· 改 `.cs` 全走 Edit/Write（④）·
> 类型检查 `TMPDIR=/tmp/wf_b1* bash 工具/typecheck.sh`（⑤）。
> 判据第一权威 = `d:/2/tools/decomp_full/` ＋ 解包资源字段（⑥）；本轮**独立现读**了 prefab 的
> `RectTransform/*.json`（不是转抄文档）。

---

## 一、B1 `M2` 整张卡面低 0.08 —— **已改（含立绘中心 0.456→0.536 那一笔）**

### 1·1 判据（本轮亲自现读，不是转抄）

`d:/2/新解包资源/assets_full/` 逐 `RectTransform/*.json` 扫 `m_AnchoredPosition` / `m_SizeDelta`：

| 对象 | 文件 | `m_AnchoredPosition` | `m_SizeDelta` |
|---|---|---|---|
| `Front`（战斗卡前缀） | `bundle_battleprefabs_vfxandmisc_assets_all/RectTransform/RectTransform_-8828616577395090496.json` | **(0, +0.0800)** | 2.0927 × 3.4913 |
| `Front`（菜单/HUD 族） | `bundle_menus_assets_all/RectTransform/RectTransform_-1999584626134795229.json` 等 **41 份** | **(0, +0.0800)**（无例外） | 同上 |
| `2DCard` | `…battleprefabs…/RectTransform_-8834377289179948096.json` | (0, 0) | **2.0927 × 3.3313** |
| `CardFrame` | `…battleprefabs…/RectTransform_4740678016171744192.json` | (0, **−0.0300**) | **2.2452 × 3.2572** |
| `CardImage`（战斗族） | `…battleprefabs…/RectTransform_2364194910465924032.json` | (0, **−0.0650**) | 2.7484 × 2.7484 |

⇒ 原版是**三级**：`2DCard(0,0) → Front(+0.08) → {CardImage / CardFrame / 文字族}`，
而 `CardImage(−0.044·菜单族 / −0.065·战斗族)`、`CardFrame(−0.03)` 那些 y **全是 `Front` 局部值**。
与 `普查产出_1024d/X1_三处判据补查.md` §二 完全一致（该文说 152 实例；本轮 menus 包只扫到 41 份 ——
差额在 13 个 arena 场景 104 份，**不影响结论**，`+0.08` 无例外）。

我们原来把这些层直接挂在 `2DCard`（= 拿 `Front` 局部值当 `2DCard` 局部值用）
⇒ **整张卡面（框 / 文字 / 立绘）比原版低 0.08 卡单位 = 2.4% 卡高**（详情窗尺度 ≈ 20 px）。
「战术立绘中心 0.456 vs 实拍 0.57」那条就是它的一条推论（0.456 + 0.08 = 0.536，落在 0.57±0.06 内）
⇒ **两件一起做、只改这一处**。

### 1·2 改动（`Core/CardView.cs`，共 12 处）

| 位置 | 改了什么 |
|---|---|
| 新常量 `FrontAposY = 0.08f` | 判据**只此一处**；建节点 / `PlaceRim` / `HitAt` / `StatWorld` 共用 |
| 新属性 `FaceContentRoot` | = `2DCard/Front`（卡面各层真正的父节点）；未建时退回 `FaceRoot` |
| `Build`（`_tiltRoot` 建好之后） | 建出 `Front` 节点 + `localPosition=(0,+0.08,0)`；**不嵌在 `if (_tiltRoot == null)` 里**（`Build` 重入幂等，两个出生入口共用） |
| `AddLayer` | 父节点 `FaceRoot → FaceContentRoot`；新增可选形参 `parent`（唯一调用点 = 徽标底板，见下） |
| 文字层 `Fill` / 角标数字 `TmpFont.NewText` | 父节点 → `FaceContentRoot` |
| `ApplyBoardSortingOrders` | 碰到 `Front` 要**再下钻一层**（否则整棵卡面树按名字 `Front` 取号恒 −1 ⇒ 场上层号一条都不设） |
| `ApplyClip` | 换算标架 `FaceRoot → FaceContentRoot`（被裁网格都在 `Front` 局部系） |
| `HitAt` / `StatWorld` / `StatLayerWorldPos` | 非场上那支**补回 `+0.08`**（它们拿的是卡根/`3DBody` 坐标，而数值图标现在在 `Front` 下） |
| 新 `PlaceInfoLayer()` | `_info` 那张整卡 quad：**场上** `y = −0.08`（它烘的是 `3DBody` 空间的四个数）、其它 `y = 0`；z 仍按形态。两个入口（`Build` / `SetFace`）都走它 |
| 新 `PlaceArmourIcon()` | 护甲盾：场上那块网格（`armourIconBoard`）烘的是 `BoardArmourShieldAt`（`3DBody` 空间）⇒ 场上 `y = −0.08` |
| `PlaceRim` | 状态环**不挂 `Front`**（原版 `MinionLight` 在卡体上），非场上那支把 `+0.08` 显式写进 `localPosition.y` |
| **不搬**：四个数值层 `stat0..3` / 徽标 `badgePlate/badgeIcon/badgeCross/badgeCounter` | 它们的坐标是 **`3DBody` 空间**（`BoardMeleeAt` / `Badges.SlotAt` 一族）⇒ 留在 `2DCard`（`FaceRoot`），否则整排数值/徽标上浮 0.08。⛔ 别「看到卡面层就搬进 Front」 |

顺手就地订正 3 处**过期注释**（铁律 5）：`FrameCostAt01` 那段「卡根空间 = Front 局部」、
`TextBgAt` 上面那句「❗ 仍开着：整张卡面低 0.08」、`RarityGem` 那句「`FrameUnitY = −0.03` 已含整卡面低 0.08」。

### 1·3 断言（`Editor/CardFaceProbe.cs`，挂在 `CheckLayers` ⇒ `CardBaseDemo.Run` 会跑）

- **M2 前提**：`Front` 节点存在且 ≠ `FaceLayerRoot`；效果文字层存在。
- **M2 ①**：`Front.parent == FaceLayerRoot`（挂在 `2DCard` 下）。
- **M2 ②**：`Front.localPosition.y == +0.08 ±1e-4`、x = 0。
- **M2 ③ ★ 灭自证（结构上不可能同时满足）**：把 `Front.y` 临时归零，量**文字层的世界 y 位移** ——
  必须 = `0.08 × 卡的世界缩放`。**位移 0 = 文字还挂在 `2DCard` 上 ⇒ 整脸偏低没修掉 ⇒ 红**。
  🧨 改坏法：`Fill` 里 `TmpFont.NewText(FaceContentRoot, …)` 改回 `FaceRoot` ⇒ 必红。
- 旧断言 A1659 的两条「文字/卡名挂 `FaceLayerRoot`」改成「挂 `FaceContentRoot`」
  （判别力不变：`Front` 是 `2DCard` 的直接子件，下面那条「倾摆 10° 仍同面」照旧成立）。
- `Editor/CardBaseDemo.cs`：`LayerCenterY` / `TmpInkCenterY` 的量尺**从卡根改成 `FaceContentRoot`** ——
  文件里所有期望 y（`BigY −0.65` / `SmallY −1.02` / 卡名墨心那一族）**本来就是 `Front` 局部值**，
  不换标架会整体多 0.08、全部假红。

### 1·4 需要主对话跑什么

1. **`CardBaseDemo.Run`**（会重写 `Scenes/CardBase.unity` ⇒ 先留副本、跑完 `git diff --numstat`）。
   日志找 `M2 ①②③` 与 `A1659`（父件那条现在应写「挂 `2DCard/Front` 下」）。
2. **`CardFaceProbe.Run`**（独立探针，不是 12 条自检之一）—— 至少把 `CheckLayers` 那一段跑出来。
3. **并排比 PnP**（铁律 10 第 6 条，本代理做不了）：抽 `Howling Banshee Exarch` 或
   `Warpforge_20_Deathwing-Terminator.png`，与 `d:/2/Warpforge部队卡片/…` 并排 ——
   **本次是整体上移 2.4%**，看卡面内容在框里的位置是不是更像原版。
4. 若 `BattleScene.Run` / `DeckScene.Run` / `CollectionScene.Run` / `ShellScene.Run` 冒出
   **「某层世界 y 差 0.08 / 0.0266 px 级」**的红：**那不是新缺陷**，是那些断言拿卡根当标架算的
   （我改不到 —— `Battle/*`、`Shell/*`、`Deck/*`、别的 `Editor/*.cs` 都在黑名单）。判据同 `LayerCenterY` 那条。

### 1·5 没覆盖到的风险（如实记）

1. **`Editor/CardFactionProbe.cs:452` `OurFrameRect`**：它遍历 `FaceLayerRoot` 的**直接子件**找 `"frame"`
   —— 现在 `frame` 是孙件 ⇒ 它 `return false`、**静默走兜底** `FrameQuadCrop()`。该文件不在我的白名单
   ⇒ **需主对话改一行**（`view.FaceContentRoot` 替换 `view.FaceLayerRoot`）。兜底有实现、不会崩。
2. **`ApplyClip` 的两个例外**：`rim` 与四个数值层不挂 `Front` ⇒ 它们被视口裁切时差 `+0.08`（2.4% 卡高）。
   这两层平时 `enabled=false` / 只在场上出现，且裁切本身就是「转过时取外接矩形」的近似档 ⇒ 按近似处理
   （已在代码里写明）。
3. **`InkBoxCardLocal`（`CardBaseDemo` 的 `AssertFaceTextFit` 专用）仍量卡根空间** —— 它是「文字有没有
   越出卡体」的绝对框，文字整体上移 0.08 后**上沿余量少 0.08、下沿多 0.08**。判据本身没变（卡体矩形没动），
   但**边界样本可能翻红**；真红了先看是不是这个原因。

---

## 二、B2 卡体比例 0.628 vs 0.689 —— **判据不成立，没有改任何常数**

### 2·1 复量（同一份 prefab 现读，两个数都是原版的）

| 量 | 原版（本轮现读） | 我们 | 结论 |
|---|---|---|---|
| `2DCard`（卡根 RT） | **2.0927 × 3.3313** = 0.6282 | `CardView.Width/Height`、`Quad()`、`CardUnitW/H` **逐位相同** | 一致 |
| `CardFrame`（可见外圈） | **2.2452 × 3.2572 @ y −0.03** = 0.6893 | `FrameUnitW/H/Y` **逐位相同**，`FrameQuad()` 按**固定矩形**画（`m_PreserveAspect=0`），`FrameMesh` 顶点 = `±1.1226 / ±1.6286 + (−0.03)` | 一致 |

⇒ **我们画出来的卡框本来就是 2.2452 × 3.2572（比例 0.6893 = 原版）**，`0.628` 是同一份 prefab 的
`2DCard` RT（原版也是它）。所谓「铺卡本体而不是可见 `CardFrame`」在**画什么**这一层**不存在** ——
`CardView.cs:11106-11131 FrameQuad` 从 2026-09-27 起就是固定矩形（当时的理由与 `m_PreserveAspect=0` 一致）。

**「瘦高 9%」是怎么来的**：`0.689/0.628 = 1.097` 是**两个不同盒子**的比值（可见框 vs 卡本体 RT），
不是同一张卡的两个版本。`普查产出_1024c/E6_收藏卡池格子几何.md` §三·3 / §六·4 拿它当「我们比原版瘦高」，
是把「原版的可见框」和「我们的（= 原版同一个）卡本体」对打了。

### 2·2 真实差异在哪（如果格位真的对不上）

格位 fit 的代码**全部**用 `高度 ÷ (CardView.Height × 108)` 反解缩放
（`CollectionWindow.cs:1466` · `DeckRuntime.cs:841` · `DraftModeWindow.cs:1080` · `WhereToGetCardsPopup.cs:571` …），
也就是「让**卡本体高** = 目标高」。原版格位是不是也这么反解 —— **`E6` §五·1 自己写着「没查清」**
（`_mobileSizeScale` 那一路没反到，实拍与原版公式差 8% 也没物证）。
⇒ **现在没有可拿的判据去改**（铁律 6：⛔ 不许按推算改常数；铁律 5·c：一个值 ≠ 全部情况）。

### 2·3 建议

- 这一行**不该按「改卡体宽高比」做** —— 那会把 `Width/Height` 改成 2.2452/3.2572，**牵动 20+ 个调用点**
  的缩放反解，且方向是「卡更大」（与 E6 实拍里我们本来就已经大 6~9% 的方向相反）。
- 要动就动**格位 fit 的目标尺寸**（Shell/Deck 侧，本代理白名单外），而且**先补判据**：
  原版卡池/卡组格的缩放反解式（`RecyclableScrollRect` 那一路的私有方法）。
- 本条已在报告里给出「判据不成立」的证据链，**建议主对话把卡的 §一 那一行改成
  「口径订正：可见卡体比例本来就是 0.6893；缺口在格位 fit 的基准（判据未闭合）」**，而不是留着「✗ 未做」。

---

## 三、B3 插图上下白边残余 —— **已复量到逐张宽度；修复点在 `Shaders/Card3D.shader`（我的白名单外）**

### 3·1 复量（本轮新测，1168 张 `Resources/Art/cards/art_*.png` 全量）

判据与 `S1` 同族：一「行」≥90% 的像素是**近白**（三通道 ≥250 且通道差 ≤8）。
（`pure`（==255）下只有 4 张有上白边 —— `S1` 的「上 151 / 下 7」是**近白**那一档，本轮近白读数 **127 / 5**，
差异来自阈值，量级一致。）

| 边 | 有白条的张数 | 宽度分布（px） | 现行 shader 裁掉多少 | 结论 |
|---|---|---|---|---|
| 左 | 658 | 2:4 · 3:6 · 4:45 · 5:210 · **6:388** · 7:2 · 8:3 | **9.67 px**（`uMin 0.014412`） | 全盖住 |
| 右 | 660 | 2:3 · 3:44 · 4:221 · **5:387** · 6:2 · 7:3 | **7.02 px**（`uMax 0.989541`） | 7 px 那 2 张**只剩 0.02 px 余量** |
| 上 | 127 | 1:6 · **2:121** | **2.25 px**（`vMax 0.997800`） | 理论盖住，余量 0.25 px |
| 下 | 5 | **1:4** · 2:1（`art_sau19`） | **1.74 px**（`vMin 0.001700`） | **`art_sau19` 露 ~0.26 px** |

⇒ **「残余」不是裁得不够宽，是余量几乎为 0** ⇒ 一走 mip（贴图有 mipmap、卡在屏上只有 150 px 宽）
白边就被摊回来 —— 这正是 `S1 §2·10` 说的「一直闪」。**覆盖面：修前 5 张有实测露白 / 其余靠 mip 漏。**

### 3·2 为什么 2D 卡面不用改（几何反证，不是「看不出来」）

立绘板 = `ArtExtent` 的方形 2.7484 × `ArtCoverMargin 1.04`，671×1024 的图 ⇒ **w 1.8724 / h 2.8583**，
板缘在 |x| = **0.936**、y ∈ [−1.473, +1.385]；卡框矩形 ±1.1226 / ±1.6286 再减贴图留白
⇒ 金属外沿 ≈ ±1.04、上下 ≈ ±1.55。**四条板边全部落在卡框金属底下** ⇒ 2D 面天然看不到那圈白。
（`W14 §2·4` 的结论「2D 看不出来」这次补上了算式。）

### 3·3 建议 patch（**我没改**：`Shaders/Card3D.shader` 不在白名单）

`Shaders/Card3D.shader`（`ArtCropUvMin` / `ArtCropUvMax` 两个 `float2`）：

| 常量 | 现值 | 建议 | 依据 |
|---|---|---|---|
| `ArtCropUvMin.y` | `0.001700` | **`0.004883`**（下裁 5 px） | 实测最大下白条 2 px + mip 余量 |
| `ArtCropUvMax.y` | `0.997800` | **`0.995117`**（上裁 5 px） | 实测最大上白条 2 px + mip 余量 |
| `ArtCropUvMax.x` | `0.989541` | **`0.985075`**（右裁 10 px） | 实测最大右白条 7 px（2 张） |
| `ArtCropUvMin.x` | `0.014412` | 可不动（9.67 px 已够） | 实测最大左白条 6 px |

⚠️ 代价：立绘相对卡框**再缩 0.3~0.5%**（`Card3D` 的 artUv 是整块重映射 ⇒ 画面轻微内缩，不位移）。
改完 **必须**重跑 `CardBaseDemo.Run` 的 `AssertBoardArtNoWhiteEdge`（① 常量普查会用 `textureRect`
重算这四个数，**它会因为容差 1 px 而红** ⇒ 要同步调那条的期望算式）+ 真机看一眼真卡。
⚠️ 另一个可选收法（更彻底）：`工具/import_original_art.py` 按实测宽度裁 PNG —— 但 `W14 §2·4` 的
「同一批图也喂 2D 卡面」这条理由仍然成立，且 496 张是 1024²、裁法要另定 ⇒ **不建议**。

---

## 四、B4 `W62` `CalibScale` 复量 —— **现读数 = 1.70（不是台账写的 1.319）；判定：一个常数都不改**

### 4·1 复量（离线现读，硬证据）

`Assets/CardPresentation/Resources/Fonts/Warpforge Trait TextSprites.asset`（YAML 现读）：

| 项 | 值 | 出处 |
|---|---|---|
| `questPoints1` 的字符 → `m_GlyphIndex` | **64** | 该文件 `m_CharacterTable` / `Atlas_trait_icon_questPoints1` |
| glyph 64 的 `m_Scale`（**盘上现值**） | **1.70** | 该文件 `m_GlyphTable` 第 64 项 |
| 原版同一条（`d:/4/Unity/数据/游戏数据/trait_textsprites.json`） | **1.70** | `glyphs[index==64].scale` |
| glyph 0（`Atlas_trait_icon_sabotage`） | 盘上 **1.65** / 原版 **1.65** | 同上 |

⇒ **当前资产 = 原版字形表 × `CalibScale 1.0`，逐位相同**（`Editor/IconSetup.cs:160` 现读 `const float CalibScale = 1.0f`）。

### 4·2 台账那句「现 glyph scale = 1.319」是**两代之前的读数**（铁律 5 要订正）

`_tmp_view/t68_sync/IconSizeProbe.log`（**2026-10-23 12:06**）第 424 行 `questPoints1 glyph scale = 1.319`。
反解：**1.70 × 0.776 = 1.3192** —— 那是 `CalibScale = 0.776`（`IconSetup.cs` doc 里记的**更早一版**）时的产物。
之后 0.776 → 0.842（PnP 尺子）→ **1.0**（2026-10-24 数字客户端裁定）⇒ 现读数应为 **1.70**（+28.9%）。

### 4·3 判定：**不改任何常数**，理由有三条硬证据

1. **4.0% 那个口径已经被判「不可比」**：`Editor/IconSetup.cs:90-99` 原文 ——
   「『11.4% 卡高』只在**名义字号**下成立，而卡面永远先过 `FitToBox` ⇒ 与成品图的
   『图标 ÷ 卡高 4.0%（48/1200）』**本来就不可比**」；铁律 10 第 3 条：PnP 是**印刷品**，
   量它可以、拿它当**数字版版式规格**不行。⇒ 拿 4.0% 反推 `CalibScale` 正是这条禁止的做法。
2. **数字客户端那把尺子（实拍 `获得 ①`）反推 C = 0.985**（与 1.0 差 1.5%），且**原版字形表里
   `Atlas_SpiritStone_1` 的 `m_Scale` 就是 1.65**（`数据/游戏数据/trait_textsprites.json` 现读）⇒
   **原版根本不做标定**，`CalibScale = 1.0` = 照原版原样（本轮现读盘上 1.65/1.70 再证一次）。
3. **`IconSizeProbe` 量的是「名义字号下的墨高」**（`fontSize 2.355229` 直出），不是卡面实际渲出来的尺寸
   ⇒ 拿它除卡高得到的百分比**不构成**「卡面图标偏大」的证据。

⇒ **动作 = 0 个常数改动**；唯一该做的是把台账/总表那句「现 glyph scale 1.319」订正成
「**盘上 1.70 = 原版 × `CalibScale 1.0`**；1.319 是 `CalibScale 0.776` 时代的旧读数」，
并注明「4.0% 口径已作废（`IconSetup.cs:90-99`）」。要我量「图标 ÷ 卡高」的可比版本，
只能走**渲图**（`CardBaseDemo` 那条渲图断言 / `CardFaceProbe` 渲 1920×1080 后数像素），不是 `IconSizeProbe`。

---

## 五、顺手发现（未改，交主对话分派）

1. 🔴 **496/1168 张立绘是 1024²（哈希命名 = 异画），而 `Card3D.shader` 的 UV 裁剪常量是按 671×1024 的
   atlas-crop 推的**（`u ∈ [0.0144, 0.9895]`）。对这些方形异画，同一个 lerp 会采到整图的
   14.8 ~ 1013 px（≈整张），**与原版「图集窗口」不是同一块**。异画卡上场时立绘映射可能偏。
   判据：`art_<md5>.png` 全部 1024×1024（496 张）· 其余 671×1024（672 张）。**没验画面，先记账。**
2. `Editor/CardFactionProbe.cs:452` 的 `OurFrameRect` 会因 `Front` 那一级**静默走兜底**（见 §1·5·1）。
3. `CardBaseDemo.LayerSizeCardUnits` 仍以卡根为标架 —— **尺寸与平移无关 ⇒ 不用改**（只是记一笔，
   免得下一个人看到两个函数标架不同以为漏了）。

## 六、没查清 / 需要判据的

1. **B2 的真正缺口**：原版卡池 / 卡组格的**缩放反解式**（`RecyclableScrollRect` 私有方法那段没反到）
   ⇒ 没有它，任何「按可见卡体重算比例」都会是推算（⛔ 按铁律 6 不改）。
2. **B3 的 mip 阈值**：5 px 内缩是「实测白条 + 余量」的经验值；真正的判据应该是「与原版采样窗口逐像素
   等价」—— 但原版的 atlas 邻域像素是**别的 sprite**，我们无法复现 ⇒ 只能自定一个够大的余量。
   建议由 `AssertBoardArtNoWhiteEdge` 的渲图那条在真机截图里定阈值（现在那条**从未跑过**）。
3. **`_BaseMap.a` 那圈压暗**（`W14 §2·6·2`）仍未闭合 —— 不在本轮 4 件活里。

---

## 七、本轮改动清单（`git diff --numstat` 已核，无行尾翻转）

```
Unity/MyGame/Assets/CardPresentation/Core/CardView.cs         +142 / -25
Unity/MyGame/Assets/CardPresentation/Editor/CardFaceProbe.cs   +48 / -4   (M2 断言 4 条 + A1659 两条改父件)
Unity/MyGame/Assets/CardPresentation/Editor/CardBaseDemo.cs    +10 / -5   (LayerCenterY / TmpInkCenterY 换标架)
```

**类型检查**：`TMPDIR=/tmp/wf_b1h bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
（中途几次红全是别的代理正在改的 `Shell/DraftModeWindow.cs` / `Editor/ProfileAssertU2b.cs` /
`Editor/BattleScene.cs`，与本批文件无关，按铁律 13·3 未去动）。
