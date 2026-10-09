# X2 · 诊断「SDF 比卡背大」那条红（只读诊断，一个字没改）

> 只读诊断代理 X2 · 2026-10-19 · 症状：`BattleScene.Run` = **2428 通过 / 1 失败**（基线 2402/0），
> 唯一那条 = `Editor/BattleScene.cs:1306` 的 `★ SDF 比卡背大 **1.34376 × 1.21548**（原版两个 sizeDelta 之比）—— 实得 1.36319 × 1.28100`（`d:/4/_tmp_view/battle.log:3946`）。
> ⛔ 没跑 Unity、没碰 git、没改任何别的文件。

---

## ① 断言的落点 + 算式逐项（它比的是「框」还是「画心」）

**落点** `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs:1305-1307`（`Check` 在 `:1306`）：

```csharp
float rw = dsdf.WorldW / pile.WorldW, rh = dsdf.WorldH / pile.WorldH;      // :1305
Check(Mathf.Abs(rw - 2.9212f / 2.1739f) < 0.02f && Mathf.Abs(rh - 3.8122f / 3.1364f) < 0.02f,
      $"★ SDF 比卡背大 **1.34376 × 1.21548**（原版两个 sizeDelta 之比）—— 实得 {rw:F5} × {rh:F5}");   // :1306-1307
```

- **分子 `dsdf`** = `drv.MyDeckSdfQuad`（`:1296`，`BattleDriver.cs:12791`）。它由 `MakeDeckSdf` 建：
  `HudImageTex(... worldHeight: Px(DeckSdfPx) ...)`（`BattleDriver.cs:13066`，`DeckSdfPx = DeckCardPx × 3.8122/3.1364`，`:1254`）
  ＋ `q.SetAspect(DeckSdfRectAspect)`（`:13073`，`= 2.9212f/3.8122f`，`:1265`）。
- **分母 `pile`** = `drv.MyPileQuad`（`:1295`，`BattleDriver.cs:12795`）。B2 之后它由 `FitDeckPile`（`:13041-13053`）摆：
  `SetWorldHeight(Px(h))` ＋ `SetAspect(w/h)`，其中 `h = fh × sh`、`sh = (1020 − padB − padT)/1020`
  ⇒ **写进去的是「画心」**（`:13049-13050`）。
- **`WorldW` 不是渲染测量**：`ImageQuad.cs:107-108` → `WorldH = _worldH`；`WorldW = _worldH * _aspect`
  ⇒ 两个数都是**我们自己塞进去的几何**（B2 §4·3-4 / B6 §5·1-1 已记这条明账）。
- ⇒ **量纲混用**：`rw/rh` = **SDF 的「框」÷ 卡背的「画心」**。
  · SDF 那一侧 padding 各轴和 = 0（见 ⑤）⇒ 它的「框」== 「画心」；
  · 卡背那一侧 padding ≠ 0、B2 起 quad 里存的就是「画心」。
  · 而断言的期望值 `2.9212/2.1739`、`3.8122/3.1364` 是**两个节点的 sizeDelta 之比（框之比）**。

---

## ② 期望值 `1.34376 × 1.21548` 的原版实读核对（**没抄错，它就是框之比**）

`D:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/RectTransform/`（本会话现读）：

| 节点 | 资产 | `m_SizeDelta` | `anchoredPosition` | `pivot` |
|---|---|---|---|---|
| `Cardback Shadow SDF` | `RectTransform_-3818142791924758008.json` | **2.9212000370025635 × 3.8122000694274902** | (0,0) | (.5,.5) |
| （同上，第二份 prefab 副本） | `RectTransform_4167557891041249678.json` | 同值 | (0,0) | (.5,.5) |
| `Cardback` | `RectTransform_-4585763702919738994.json` | **2.1738998889923096 × 3.136399984359741** | (0,0) | (.5,.5) |
| （同上，第二份副本） | `RectTransform_-616306181444777464.json` | 同值 | (0,0) | (.5,.5) |

- 比值：`2.9212/2.1739 = 1.34376` · `3.8122/3.1364 = 1.21547` ⇒ **与断言里的数逐位一致**。
- 两节点 `anchoredPosition` 都是 (0,0)、`pivot` 都是 (.5,.5) ⇒ **同心** ✓（断言上方 `:1291-1294` 的注释属实）。
- 父链现读（本会话走 `m_Father`）：两节点的同一个父 = **`Cardback Container_-24362778020185179080`**（`sizeDelta (1,1)`），
  再上是 **`2DCard_6015224010382035342`**（`2.0927×3.3313` = 卡身 = `CardView.Width/Height`）。
- ⚠️ **顺带订正一句注释**（与本次红无关）：链条上（`Cardback` / `Cardback Container` / `2DCard`）`m_LocalScale` **全是 1.0**
  —— `BattleDriver.cs:1247/13029` 那句「@容器 scale 100 ⇒ 217.39×313.64 px」在这层 prefab 里**不成立**，
  ×100 来自更上游的场景侧缩放。比值与缩放无关，所以不影响本判定。

---

## ③ 判定：**(α) 断言期望值/量法过期**（不是实现错、不是别的回归 γ）

**实得 = 期望 ÷ (sw, sh)，一个因子都不多。** 逐位现算（判据见下）：

| 量 | 值 | 来源 |
|---|---|---|
| 本次牌堆卡背 | **`Cardback_BL_Campaign_Free`** | `battle.log:3958` 现读：SDF 贴图 = `Cardback_BL_Campaign_Free_sdf` |
| 它的 `m_Rect` | 707 × 1020 | `bundle_cosmeticscardbacksimages_assets_all/Sprite/Cardback_BL_Campaign_Free_Main.json` |
| 它的 `textureRect` | 158, **23.0761**, **696.9239 × 967.8478** | 同上（`m_RD.textureRect`；`textureRectOffset = (0, 21.0761)`） |
| `Resources/Cardbacks.json` 该条 | `padL 0 · padR 10.0761 · padB 21.0761 · padT 31.0761` | 与上面逐项吻合 |
| ⇒ `sw = 696.9239/707` | **0.985748** | |
| ⇒ `sh = 967.8478/1020` | **0.948870** | |

- `1.34376 / 0.985748 = ` **1.36319** ←→ 日志 **1.36319** ✓
- `1.21547 / 0.948870 = ` **1.28097** ←→ 日志 **1.28100** ✓（差在第 5 位 = 取整）
- **旧口径复核**（把卡背那块当「框」）：`1.34376 × 1.21550` —— 与断言的期望值吻合，
  y 上那 0.00002 来自我们 `DeckSdfPx = 314 × 3.8122/3.1364 = 381.66` 而原版节点是 **381.22**（0.12%，`DeckCardPx` 由 313.64 取整成 314）。
  ⇒ 基线绿是理所当然，现在红也只有一个成因 ⇒ **排除 γ**。

**关键判别（题目要的那一刀）：原版里「框之比」≠「画心之比」。**

| | x | y |
|---|---|---|
| 框之比（两个节点 `sizeDelta`） | 1.34376 | 1.21547 |
| 画心之比（原版实况会画出来的） | `292.12 / (217.39 × 0.985748)` = **1.36319** | `381.22 / (313.64 × 0.948870)` = **1.28096** |

⇒ **不是同一个数**，根因：**SDF 那批 sprite padding 各轴和 = 0、卡背那批 ≠ 0**（233/233 现读见 ⑤）。
⇒ 断言拿「框之比」去比「框 ÷ 画心」—— **B2 之前碰巧成立（那时卡背那块也是框），B2 之后结构性不成立**。

**⚠️ α 的前提（必须一起说清）**：这依赖 C1/B2 的「第二段（按 padding 内缩）」为真 ——
C1 §五·1 自己写明那一环是**推的**（`Sprites.DataUtility.GetOuterUV/GetPadding` 是 native）。
本会话把能读的都读了，**三条独立证据都指向「内缩」**：

1. **`Image.cs:830-848`（现读）**：`GetDrawingDimensions` 里
   `v = (padding.x/spriteW, padding.y/spriteH, (spriteW−padding.z)/spriteW, (spriteH−padding.w)/spriteH)`，
   再 `r.x + r.width*v.x …` ⇒ **画出来的 quad = 节点框按 padding 比例内缩**（源代码，不是转述）。
2. **那批 sprite 的 `m_Rect` 不可能是紧框**：Main **233/233 恒 707×1020**、SDF **233/233 恒 100×130.5**，
   而 `textureRect` 逐张不同（`sw ∈ [0.8612,1]`、`sh ∈ [0.9048,1]`）
   ⇒ `m_Rect` 是「声明画布」、`textureRect` 是真实像素 ⇒ padding 是**正的内缩量**
   （若反号，quad 会画到节点**外面**一圈，与 `textureRectOffset` 的定义和物理都矛盾）。
3. **`Editor/UGUI/UI/SpriteDrawUtility.cs:89-152`（编辑器预览同构）**：`outer = sprite.rect` 用**定外接框**；
   `padding = GetPadding(sprite)` 之后 `paddedTexArea = outerRect 按 padding 内缩`；再用 `uv = GetOuterUV(sprite)`
   把贴图**铺满那块内缩区**（`DrawTextureWithTexCoords(paddedTexArea, tex, uv)`，`:157`）
   ⇒ 「画心 = 框 × texRect/m_Rect」是 **1:1**、**不是再平方**，且「定框用 `m_Rect`、画心用 `textureRect`」两段都对。
   补一条同链的旁证：`Image.cs:1168-1174`（Sliced）里几何列的起点写的就是 `padding.x/padding.y`
   （= 内缩量），而它对应 `s_UVScratch[0] = outer` ⇒ `outer` 就是**像素区（textureRect）**的 UV。

⇒ **若这三条哪天被真 Play 推翻**（画满框那一档），要改的是 `Core/CardbackFace.cs` 的第二段 +
回退 `CollectionScene`/`DeckScene` 那一批新期望值（**那时本断言的常量反而会重新成立**）；
**但两条路都不会得到「本断言照原样就绿」的第三种答案** —— 现在这个 red 不是「实现对了、断言没错」。

---

## ④ 修法方向（最小；⛔ 我没改）

**改断言，不改实现**（实现那一半按 ③ 的证据是对的）。**而且不能只换常量 —— 那个期望值会随「这次用哪张卡背」变。**

**判据（常量写死结构性不成立）**，现算同一张表里的四个阵营默认卡背（`Cardbacks.json` 的 `defaults`）：

| 默认卡背 | `textureRect` | 两段式下的正期望 `rh` |
|---|---|---|
| `Cardback_TL_Campaign_Free`（Leviathan） | 707.0 × 1019.9 | **1.21553**（= 旧常量） |
| `Cardback_BL_Campaign_Free`（本局） | 696.9 × 967.8 | **1.28097** |
| `Cardback_UM_Campaign_Free`（Ultramarines） | 707.0 × 939.8 | **1.31913** |
| `Cardback_GOF_Campaign_Free`（Goff） | 645.9 × 929.9 | **1.33324** |

⇒ 换一个阵营打这一局，正确的期望值就从 1.21553 漂到 1.33324。**期望值必须由「牌堆那张贴图自己的数据」现算。**

**推荐写法（最小改动，保留原版那两个常量当骨架）**：

```csharp
// 两段式：牌堆卡背那块 quad 里存的是【画心】= 框 × (texRect/m_Rect)
// ⇒ 期望比值 = 原版框之比 ÷ (texW/707, texH/1020)。
// texW/texH 用【贴图自己】顶（我们的 PNG 就是 textureRect 裁片，误差 ≤0.5px ⇒ 比值上 ≤0.001）
// —— 独立于 CardbackTable/CardbackFace.Fit，⛔ 不问被测实现（同 B2/B6 的做法）
float kx = pile.Texture.width / 707f, ky = pile.Texture.height / 1020f;
float ew = (2.9212f / 2.1739f) / kx, eh = (3.8122f / 3.1364f) / ky;
Check(Mathf.Abs(rw - ew) < 0.02f && Mathf.Abs(rh - eh) < 0.02f,
      $"★ SDF(框) 比卡背(画心) 大 **{ew:F5} × {eh:F5}**"
    + $"（原版框之比 1.34376 × 1.21548 ÷ texRect/m_Rect）—— 实得 {rw:F5} × {rh:F5}");
```

- `ImageQuad.Texture` 已在用（`:1297` 那句 `pile.Texture == null`），取 `width/height` 不用新开口（`ImageQuad.cs:138`）。
- **容差**：现算的实得 1.36319/1.28097 与 PNG 取整带来的抖动量级 ~1e-3、`DeckSdfPx` 那 0.12% ≈ 1.5e-3 ⇒ **0.02 仍够**。
- **顺带提醒**：这次 **x 轴偏差 = 0.01943，本来就贴着 0.02 的边**（y 轴 0.06552 才把它顶红）—— 改的时候别把容差当「离得远」。
- **另一种同样可行的改法**（可作第二条断言、不是替代）：让 `BattleDriver` 把**框**也留一个口
  （例如 `FitDeckPile` 记下 `fw/fh`，或加 `MyPileFrameW/H`），断言改比「框 ÷ 框」= 1.34376 × 1.21548。
  好处是**两个量纲各断一条**（框的关系 / 两段式那一半），红的时候一眼看得出坏的是哪一半；
  代价是要动 `BattleDriver.cs`（比改断言大）。
- ⛔ **不要**把期望值改成一个新常量（上表已证它会随阵营漂）。

---

## ⑤ `B2` 说「SDF 那层不用动」—— **对**（我现读复核过）

- **233/233** 张 `_SDF` 的 `m_Rect = (899.5, 447, **100 × 130.5**)`，与自己的 `textureRect` **逐项最大只差 0.5px**
  （样本：`Cardback_BL_Campaign_Free_SDF.json` → `m_Rect (899.5,447,100,130.5)` / `textureRect (900,447.25,100,130.5)`，`textureRectOffset (0.5,0.25)`）。
- 按 B2 的算式：`sw = (100 − 0.5 − (−0.5))/100 = ` **1.000000**、`sh = (130.5 − 0.25 − (−0.25))/130.5 = ` **1.000000**
  ⇒ **尺寸一个像素都不差**，只有 `dx = (0.5−(−0.5))/2 × k`，`dyUp = (0.25−(−0.25))/2 × k`，
  `k = 框宽/100 ≈ 2.92` ⇒ **横 1.46px / 纵 0.73px**（B2 写的「1.7px」同量级）。
  ⇒ 「按原样」与「严格套公式」差的是一个**亚像素偏置**，而 SDF 是卡背底下的灰掩码 ⇒ **不动是对的**。
- ⚠️ 措辞上精确一点（B2 的注释说「padding 恒 0」）：严格说不是 padding 逐边为 0，而是**两轴的 padding 代数和为 0**
  （逐边是 ±0.5px 的图集取整）。结论不变。
- 顺带解释了断言为什么是**混量纲**的：SDF 那块 quad 恒等于**节点框**（`DeckSdfPx` + `DeckSdfRectAspect`），
  而卡背那块 B2 起是**画心**。

---

## ⑥ 判不了的 / 缺什么 · 顺带核到的

**判不了的：**

1. 「画心 = 按 padding 内缩那一块」仍是**推**的（`Sprites.DataUtility.GetOuterUV/GetPadding` 是 native，本机读不到实现）。
   **唯一能钉死的**：真 Play 进原版看牌堆卡背、量**画心**宽高比（**已在 B2 §五 的待验清单里**）——
   预测 `Cardback_UM_Warlord_Tigurius` 应为 **0.7451**（= 707/948.85）；若量出 **0.6931**（`m_Rect` 比例）⇒ 第二段反了。
2. `ImageQuad.WorldW/WorldH` **不是渲染测量**（`= _worldH * _aspect`，`ImageQuad.cs:107-108`）——
   本断言比的是「我们塞进去的几何」，不是 mesh/renderer 实测。这条明账本轮**没动**（B2 §4·3-4 / B6 §5·1-1 都记着）。
3. 本代理**没跑 Unity / 没跑原版实况**，所有读数都是静态现读 + 算式复核（算式与日志 5 位对上）。
4. 牌堆卡背的**框**现在没有对外口（`FitDeckPile` 只把画心写进 quad）⇒ 想按「框之比」断就得先在 `BattleDriver` 上开口（见 ④ 第二种改法）。

**顺带核到的（都在本件范围之外，仅提示）：**

- 收口那 12 条里**另一条红是 `RuleEngineTest.Run`（4259/4261，2 条：近战翻倍 3→6 / 远程也翻倍 2→4）**，与卡背无关。
- **`CollectionScene.Run` = 1466/1466 ✅ · `DeckScene.Run` = 1374/1374 ✅**（`_tmp_view/collection.log:43382` / `deck.log:25992`）
  —— 即 B2/B6 那两批按两段式算出来的期望值是**跑绿**的 ⇒ 本批里 BattleScene 这条正是 B2 §3·4 预测的那一处
  「还可能引用旧值的断言」，**只剩它一处没同步**。
