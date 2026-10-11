# B · 详情窗放大卡 —— 文字块 / 底板 / 底板不可见

> 执行代理 `B-详情窗` · 2026-10-19 · 白名单 = `Core/CardView.cs`(CRLF) · `Core/CardWinBox.cs` · `Shell/CardDetailPopup.cs`(LF)
> 红线遵守：未跑 Unity · 未动 git · 未改 `Editor/` · 未改正本 · 未越白名单。

---

## 结论（一句话）

**② 已改** —— 但改的是**底板**（下移 `0.08` 卡单位），**不是**文字（主对话简报里给的「把六条文字 y01 抬到 −0.6945」这条**会引入可见回归**，理由见「§偏离说明」）。
**③ 没查清** —— 但已把它从「原因不明」收窄到「**层确实建出来了、也确实 enabled、网格与材质都对，就是不出像素**」，并**排除**了 z/渲染队列、贴图缺失、`ApplyTint` 刷白、网格错、GO 被关这五条；还缺一个**运行时探针**（`Editor/` 侧，不在我白名单）。

---

## 改动清单（文件:行号 —— 改了什么 —— 判据出处）

| 文件:行号 | 改了什么 | 判据出处 |
|---|---|---|
| `Core/CardView.cs:404` | `TextBgAt.y` **0.67111 → 0.69512**（底板中心 −0.5700 → **−0.6500** 卡单位） | `assets_full/bundle_scenes_scenes_battlearena1/RectTransform_213.json`（GO `TextBackground Big UI`）`m_AnchoredPosition = (−0.001, **−0.65000**)、`m_SizeDelta = (1.7766, 1.55)` |
| `Core/CardView.cs:405` | `TextBgSmallAt.y` **0.80621 → 0.78217**（−1.0200 → **−0.9400**） | 同上，`RectTransform_470.json`（GO `TextBackground Small UI`）`m_AnchoredPosition = (0, **−1.02000**)` |
| `Core/CardView.cs:388-403`（新增 16 行注释） | 记下这次更正 + 父链读数 + 「文字那条路为什么不取」+ 「整卡 0.08 仍开着」 | 本报告 §判据 |

**没有改的**：六条文字 `y01`（`UnitNameAt / UnitDescAt / TacticNameAt / TacticDescAt / UnitArmyAt / UnitRaceAt / TacticArmyAt`）· 五个派生常量（`UnitDescTop01` 0.6180 · `UnitDescBottom01` 0.8466 · `TacticDescTop01` 0.6691 · `TacticDescBottom01` 0.9061 · `RaceDescBottom01` 0.834）· `TextBgZ` · 任何贴图 / 任何 `Editor/` 文件。
⇒ **`RaceDescBottom01` 不需要重测**（它约束的是**文字块**的位置与高度，本轮文字**一动没动**）。

**波及的宿主**（`CardView` 是卡面全局共用件）：战场手牌 / 墓地 / 手牌→场上（场上整张不画）· 卡组编辑卡池 · 收藏窗 · 卡片详情窗 · `CardBaseDemo`。
⚠️ 但这次只挪**底板**，而底板**当前在实拍里根本看不见**（③）⇒ **本轮的可见效果 = 0，直到 ③ 修好**。

---

## 判据（每条：资产路径 / 文件:行号 / 实测读数）

### ① 父链（这是 ②/③ 两件共同的地基）

出处：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`
读法：`GameObject/<名>_<id>.json` → `m_Component[].m_PathID` → `RectTransform_<pid>.json` / `Transform_<pid>.json`（**RT 的 PathID 不在文件名里就是在 `m_GameObject` 上，这里用文件名尾号**）。

```
2DCard                 RT210    apos=(0, 0)        size=(2.0927, 3.3313)
 └ Front               RT126    apos=(0, +0.08000) size=(2.0927, 3.4913)     ← 底部与 2DCard 底边重合、顶端多出 0.16
    ├ Textbackgrounds  TF1074   lpos=(0, 0, 0)                               ← **纯 Transform**
    │   ├ TextBackground Big UI    RT213  apos=(−0.00100, −0.65000) size=(1.7766, 1.5500)
    │   └ TextBackground Small UI  RT470  apos=( 0.00000, −1.02000) size=(1.8635, 0.9621)
    ├ CardFrame        RT941   apos=(0, −0.03000) size=(2.2452, 3.2572)
    └ Card Info        TF865    lpos=(0, 0, 0)                               ← **纯 Transform**
        ├ Cost Container   RT273  apos=(0.78800,  0.66600)
        ├ CardImage        RT1131 apos=(0, −0.04400)  size=(2.7484, 2.7484)   （在 Front 下，与 Card Info 平级）
        ├ Rarity           RT591  apos=(0, −1.45800)
        └ Name and description  RT596 apos=(0, −0.77450) size=(1.3000, 0.6800)
            ├ MinionOrWarlord Container TF962（纯 Transform，lpos 0,0,0）
            │    NameTextUnit RT41 apos=(0,0.50100) · DescTextUnit RT1173 apos=(0.01640,0.00080)
            │    ArmyTextUnit RT301 apos=(0,0.31000) · RaceText RT649 apos=(0,−0.45200)
            └ Tactic Container TF1175（纯 Transform）
                 NameTextTactic RT374 apos=(0,0.31350) · DescTextTactic RT1043 apos=(0,−0.18360)
                 ArmyTextTactc RT439 apos=(0,0.13000)
```

**两条路（底板 / 文字）的父级结构逐字相同** —— 中间都隔着**一个零偏移的纯 `Transform`**：

| 路 | 相对卡中心的 y |
|---|---|
| 底板 Big | `Front(+0.08)` + `Textbackgrounds(0)` + `(−0.65)` = **−0.57** |
| 文字块 | `Front(+0.08)` + `Card Info(0)` + `(−0.7745)` = **−0.6945** |

⇒ 要么两条都加 `+0.08`、要么两条都不加。**本文件里其他层（`CardImage` −0.044 · `CardFrame` −0.03 · `Cost Container` +0.666 / 0.788 等）取的全是【不加】的原始值 ⇒ 底板才是那个异类。** ← 这是选底板而不是选文字的**唯一**理由，展开见 §偏离说明。

### ② 运行期实况（证明 `Front` 的 `+0.08` 不是模板态残留）

`d:/4/Unity/资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`：

| 行 | 节点 | `pos` | `rect` |
|---|---|---|---|
| :166 | `…/CardUI (1)/2DCard` | `0.0,0.0` | `2.1,3.3` |
| :168 | `…/2DCard/Front` | **`0.0,0.1`** | `2.1,3.5` |
| :174 | `…/2DCard/Front/CardFrame` | `0.0,-0.0` | `2.2,3.3` |
| :193 | `…/Front/Card Info/Name and description` | `0.0,-0.8` | `1.3,0.7` |

⇒ `Front` 在**运行时**仍然是 `+0.08`（`0.1` 是这一列 1 位小数的写法），`CardFrame` 仍是 `−0.03` ⇒ **`+0.08` 是真的，没有被运行时覆盖。**

### ③ 我们代码里三条路的现状（本文件，改前）

| 元素 | 常量 | 我们用的局部值 | 含 `+0.08` 吗 |
|---|---|---|---|
| 立绘 | `ArtUnitY = −0.044` | −0.044 | ❌ |
| 卡框 | `FrameUnitY = −0.03` | −0.03 | ❌ |
| 软光/影 | `ShadowY = −0.0126` | −0.0126 | ❌ |
| 费用/攻/远/甲/血 | `CostAt / MeleeAt / …` | 原始容器值 | ❌ |
| 六条文字 | `UnitNameAt …` | −0.7745+子偏移 | ❌ |
| **文字底板** | `TextBgAt`（改前） | `0.08 − 0.65 = −0.57` | ✅ **只有它** |

⇒ 底板比文字**高 `0.08` 卡单位**。在详情窗那个尺度（卡单位 = 250 px，见 `RectTransform_…` 的 `m_LocalScale = 250`）**= 20 px** —— 与简报里的数字一致。

---

## 状态 → 参数 表（改前 / 改后，逐值）

**底板（本轮唯一改动的对象）** —— 两档由 `hasDesc = !string.IsNullOrEmpty(d.keywords)` 决定（`CardView.cs:1665`），**不是** unit/tactic 分档：

| 状态 | 常量 | 改前 y01 | 改后 y01 | 世界 y（卡单位） | 尺寸（卡单位） |
|---|---|---|---|---|---|
| `d.keywords` 非空 → `TextBackground Big UI` | `TextBgAt` | 0.67111 | **0.69512** | −0.5700 → **−0.6500** | 1.7766 × 1.5500 |
| `d.keywords` 为空 → `TextBackground Small UI` | `TextBgSmallAt` | 0.80621 | **0.78217** | −1.0200 → **−0.9400** | 1.8635 × 0.9621 |

（两档 x 不变：Big `0.49952`、Small `0.50000`。）

**六条文字（本轮未动，逐值列出以便对账）**：

| 状态 | 常量 | y01 | 世界 y（卡单位） |
|---|---|---|---|
| 单位卡 · 卡名 | `UnitNameAt` | 0.5821 | −0.2735 |
| 单位卡 · 效果 | `UnitDescAt` | 0.7323 | −0.7739 |
| 单位卡 · 阵营行 | `UnitArmyAt` | 0.6394 | −0.4644 |
| 单位卡 · 兵种行 | `UnitRaceAt` | 0.8682 | −1.2263 |
| 战术卡 · 卡名 | `TacticNameAt` | 0.6384 | −0.4610 |
| 战术卡 · 效果 | `TacticDescAt` | 0.7876 | −0.9581 |
| 战术卡 · 阵营行 | `TacticArmyAt` | 0.6935 | −0.6445 |

**层间关系（改前 → 改后，与原版字段比）**：

| 关系 | 改前 | 改后 | 原版字段 | 改后对不对 |
|---|---|---|---|---|
| 底板 ↔ 单位卡卡名 | 0.2965 | **0.3765** | 0.3765 | ✅ |
| 底板 ↔ 战术卡卡名 | 0.1090 | **0.1890** | 0.1890 | ✅ |
| 底板 ↔ 卡框中心 | 0.5400 | **0.6200** | 0.6200 | ✅ |
| 卡名 ↔ 卡框中心 | 0.4310 | 0.4310（未动） | 0.4310 | ✅（本来就对） |

⚠️ **只改文字那条路（简报给的方案）会得到**：底板↔卡名 ✅ / **卡名↔卡框 0.3510 ❌（原版 0.4310）** / **底板↔卡框 0.5400 ❌（原版 0.6200）** —— 三个关系错两个。

**派生常量（本轮不受影响，故不重测）**：`UnitDescTop01` 0.6180 · `UnitDescBottom01` 0.8466 · `TacticDescTop01` 0.6691 · `TacticDescBottom01` 0.9061 · `RaceDescBottom01` 0.834。

---

## 偏离说明（🔴 请裁 —— 我没有照简报的字面改）

简报写的是「**六条文字 y01 应为 −0.6945**（父链上算，对）⇒ 把文字抬 `0.08`」。父链的**推导本身没错**，但**结论只能二选一**，而两个方向在**渲染上不等价**：

- **方向的真相**：`Front +0.08` 对**整个 `Front` 子树**都成立 ⇒ 原版整张卡面都在「World 1（= `2DCard` 中心 + 0.08 + 局部值）」，而**我们整张卡面都在 World 2（只用局部值）**。
- **两个世界的差别**：World 1 与 World 2 之间是**整张卡面的整体平移**，而卡面自己**就是**卡的外形（卡框 = 外轮廓）⇒ 相对 `2DCard` 的那 `0.08` 平移**在画面上看不见**（没有任何参照物画出来）。
- **所以**："把文字抬到 World 1" **不等于** "把卡面换成 World 1" —— 它只把**文字单独**挪到 World 1，于是文字相对**卡框/立绘/宝石**（还留在 World 2）就错开 `0.08`，那是**看得见的 20 px 错位**。
- **而底板**：它是唯一一个已经在 World 1 的层 ⇒ **把它拉回 World 2** 才是"最小且不回归"的修法：三个层间关系全部对齐原版，文字与卡框的关系（本来就对）一点不动。
- **另外**：World 1 那条路要正确，得**把全部层（立绘/框/影/宝石/数值/文字/底板）一起 +0.08**，那超出我的白名单（且它在画面上同样不可见）。已把这一条写成注释留在 `CardView.cs:399-403`。

⛔ **如果调度台坚持要 World 1**：把 `CardView.cs:404-405` 的 y 改回 `0.67111 / 0.80621`，再把**七条文字 y01** 各减 `0.0240146`（`0.08/3.3313`），并把五个派生常量一起平移 —— **但那样必须同时把立绘/卡框/宝石也 `+0.08`**，否则是拿一个新错位换掉旧错位。

---

## PnP 静态量测表（量了哪几张卡、量到多少、与我们的值差多少）

**量法**：`900×1200` PnP PNG → 取 `alpha>250` 的连通外框当「卡面框」→ 在**去掉左右各 18% 的中间列带**里按颜色找行：白字（`min>195 且 max−min<28`）= 卡名/关键词，橙字（`≈(250,152,90)`）= 阵营行/兵种行 → 行中心 `t = (y − boxTop) / boxH`。
采样 5 张（`python -I -X utf8`，脚本在 `D:/tmp/wb_b/pnp4.py`，可复跑）：

| PnP 卡 | 类型 | 卡面框 px | 卡名 t | 阵营行 t | 兵种/类型行 t |
|---|---|---|---|---|---|
| `Aeldari/3部队/Warpforge_10_Veteran-Guardian.png` | 单位 | x[144,756] y[106,1112] h=1006 | 0.5900 | 0.6382 | 0.8862 |
| `Necron/3部队/Warpforge_07_Immortals-Phalanx.png` | 单位 | x[99,779] y[112,1143] h=1031 | 0.5664 | 0.6217 | 0.8817 |
| `Necron/3部队/1_Warpforge_30_Lychguard.png` | 单位 | x[146,754] y[158,1112] h=954 | 0.5671 | 0.6221 | 0.8826 |
| `Necron/4计策/Warpforge_10_Dimensional-Corridor.png` | 战术 | x[122,780] y[112,1144] h=1032 | 0.6202 | 0.6819 | —（战术卡不印） |
| **我们**（`_tmp_view/cardface/Howling_Banshee_Exarch.png`，700×1115 **正好 = 卡本体**） | 单位 | — | **0.5821** | **0.6394** | **0.8682** |
| **我们**（战术，取常量） | 战术 | — | **0.6384** | **0.6935** | — |

**差多少（单位 = 卡高的比例）**：单位卡卡名 PnP 0.5664–0.5900（**跨卡离散 0.024！**）vs 我们 0.5821 ⇒ 我们的值**落在 PnP 自己的离散带里**；阵营行 PnP 0.6217–0.6382 vs 我们 0.6394 ⇒ 差 0.001–0.018。

🔴 **结论：PnP 测不出 `0.024` 这么大的差异** —— 它**自己跨卡的离散度就是 0.024**，而本次要判的量正是 0.024。加上：
- 卡面框定位器我实测出来的宽高比在 **0.608–0.660** 之间跳（真实的卡框矩形比例是 **0.6893**），说明「`alpha>250` 的外框」里**混进了印刷投影**，`boxH` 本身有 ±3%（≈ ±0.1 卡单位）的系统误差；
- **铁律 10 第 3 条**：PnP 是**印刷品、非官方**，**版式不是尺子**。

⇒ **PnP 这一轮只能当旁证、不能当判据**；也因此 `RaceDescBottom01 = 0.834`（注释写「量出来的」）的**版式依据比注释说的弱**（见 §顺手发现 3）。

---

## 层 × 场景 × 出现条件 表（③ 用）

| 层 | 谁建（`CardView`） | 出现条件 | 我们 z | 原版 `Front` 子序 | 实拍可见？ |
|---|---|---|---|---|---|
| `shadow` | `Build` | 取到 SDF 材质 + 图 | **+0.06** | 1 | ✅ 可见 |
| `art` | `Build` | `!board` | **+0.03** | 2 (`CardImage`) | ✅ 可见 |
| `artFront` | `Build` | `!board && cut && UseFrontLayer` | **−0.008** | 原版**没有**这一层 | ❌ 出厂不建 |
| `frame` | `Build` | `!board` | **0** | 4 (`CardFrame`) | ✅ 可见 |
| **`textBg`** | `Build` | `!board` **且** `CardArt.DeckUi("Card_Text_smooth_background") != null` | **−0.012** | 3 (`Textbackgrounds`) | 🔴 **看不见** |
| `costBg` | `Build` | `!board` 且图在 | **−0.015** | `Card Info/Cost Container/Cost Background` | ✅ 可见 |
| `armourIcon` | `Build` | `d.isUnit && armor>0` | **−0.015** | `Card Info/Armour Container/Image` | ✅ 可见 |
| `info` | `Build` | 总是 | **−0.02** | `Card Info` 各 Text | ✅ 可见 |
| `gem` | `Build` | `!board` 且图在 | **−0.01** | `Card Info/Rarity` | ✅ 可见 |
| `title/keywords/army/race/createdBy` | `BuildTextLayers` | `TmpFont.Available` + 各档 | **−0.03** | `Card Info/Name and description/*` | ✅ 可见 |
| `textBgSmall` | 同上（同一层，换 mesh） | `d.keywords` **为空** | **−0.012** | 3 | 🔴 **看不见** |

---

## ③ 根因：**没查清**，但已排除五条、收窄到「层活着但不出像素」

### 实测（不是推断）

在 `d:/4/_tmp_view/cardface/Howling_Banshee_Exarch.png`（700×1115 = **卡本体 1:1**，`334.7 px/卡单位`）上按改前/改后两个候选位置找**硬边**：

| 位置 | 期望上沿 py | 期望下沿 py | 实测 |
|---|---|---|---|
| 改前（中心 −0.5700） | 489 | 1008 | 无任何台阶（跨边界的行均值 106.8→114.5→118.0，是**变亮**） |
| 改后（中心 −0.6500） | 516 | 1035 | 同上 |
| 左沿 px 53（改前）/ 75（改后） | — | — | 列均值 89.5→93.0→98.8→100.2，**无下降** |

⇒ 底板**一个像素都没画**。
再扫**全部 130 张** `_tmp_view/cardface/*.png`（脚本 `D:/tmp/wb_b/scanall.py`，判据 = 边界处内外行均值比 < 0.78）：**0 命中** ⇒ **系统性的、每一张卡都不画**（不是某张卡的数据问题）。

### 已排除（每条都带读数）

| 候选 | 怎么排掉的 |
|---|---|
| **贴图缺失** | `Resources/Art/ui_deck/Card_Text_smooth_background.png` 在（32×32 RGBA，RGB 全白、内部 alpha **255**，中心不是透明）；`.meta` 的 guid `109a3d81…` 与场景里材质 `_MainTex` 的 guid **逐字相同**；`_tmp_view/*.log` 里**没有** `[CardView] 文字底板图 … 取不到` 那条 warning（有就说明没建层） |
| **层被关掉** | `Scenes/CardBase.unity` / `DeckEditor.unity`（自检宿主 `SaveScene` 存下来的**运行时真实状态**）里，全部 `textBg` GO 的 `m_IsActive = **1**`（CardBase 15/15、DeckEditor 24/24）、`MeshRenderer.m_Enabled = **1**`、`m_LocalPosition.z = **−0.012**` |
| **`ApplyTint` 把它刷成不透明白** | 代码 `CardView.cs:3217-3218` 已经在整卡着色的 foreach **之后**单独刷回 `(0,0,0,TextBgAlpha*_alpha)`；场景里该材质 `m_Colors: _Color = {r:0,g:0,b:0,a:**0.647**}` 逐字吻合 |
| **网格错 / 位置错** | 场景里 `SpriteQuad_textBg` 的 `m_SubMeshes.localAABB` = `center (−0.0010045, **−0.57001865**, 0)`、`extent (0.8883, 0.775)` = **正是该画的矩形**；`m_Channels` 里 position(dim3)+uv0(offset 12, dim2) 都在 |
| **z / 渲染队列** | `costBg`(z **−0.015**) 与 `_info`(z **−0.02**) **都在底板附近、都在立绘之上、且实拍都可见**（费用蓝色六边形 + 五个数字都画出来了）⇒ 这个 z 带**整体是通的**，不是排序问题 |

### 还差什么（**卡住点**：需要 `Editor/` 侧，我的白名单里没有）

一个运行时探针就够，读四样：

1. `_textBg.isVisible`（`Renderer.isVisible`，**渲染后才可信**）· `forceRenderingOff` · `enabled`
2. `_textBg.sharedMesh != null && vertexCount == 4`（防静态缓存网格被 `UnloadUnusedAssets` 回收）
3. `_textBg.sharedMaterial.color / renderQueue / shader.name`（在**运行时**读，不是读存档场景）
4. **只渲这一层**（把 `art`/`frame` 强制关掉渲一帧）—— 排除「被立绘盖住」这**唯一**还没排除掉的可能（理论上不成立：立绘在 z=+0.03，比底板远，且 `costBg` 在两者之间照样可见，但这是**唯一**没直接证伪的一条）

---

## 没查清的部分（「没查清」+ 还差什么，⛔ 不许填空）

1. **③ 的根因 —— 没查清。** 已知的全部事实如上：**层活着、开着、网格/材质/颜色都对、贴图在、不出像素**。上面 §还差什么 那四样读数拿到之前，**我不给它编一个原因**。
2. **原版运行时底板的 `m_Color` 与「卡框会不会被它压暗」—— 没查清**（原有注释 `CardView.cs:400-402` 也自陈没查清）。我们 `TextBgZ = −0.012` 是**画在卡框【之前】**（z 更小 = 更近），而原版子序是 `CardImage → Textbackgrounds → CardFrame`（底板在卡框**之下**）。这一条要渲并排图才能定（铁律 10 第 6 条），我跑不了 Unity。
3. **原版 `TextBackgrounds` 的 9-slice 几何 —— 没查清**：我们按**整块 32×32 拉伸**画，原版是 `m_Type=1`(Sliced) + `border (7,0,7,9)` + `m_PixelsPerUnitMultiplier = 14.7`。底板的**圆角/淡出宽度**因此对不上（见 §顺手发现 4）。

---

## 我建议加的断言（逐条；你自己不写）

> 背景：本工程 **16k+ 断言全绿**，而底板这块**一个像素都没画** —— 因为**唯一**盯着它的断言只看了 `activeSelf`：
> `CardView.cs:1017  public bool TextBgVisible { get { return _textBg != null && _textBg.gameObject.activeSelf; } }`
> 场景里 `m_IsActive` 是 1 ⇒ 这条断言**恒绿**。这是 ③ 能躲过全量自检的**直接原因**。

1. **（灭自证 · 盯原版字段）底板中心必须 = `−0.65` 卡单位**（有描述）/ `−1.02`（无描述）——
   即 `Mathf.Abs(plateCenterLocalY − (−0.65f)) < 1e-3f`，注释里写明「−0.65 是 `RectTransform_213` 的 `m_AnchoredPosition.y`（`Front` 树下的**原始值**）」。
   ⚠️ 只断「底板比文字低多少」是**不够的**（两边一起改回去照样绿）⇒ 必须**同时**断这个**绝对值**。
2. **（关系 · 与上一条结构上不可能同时满足）`底板中心 = 单位卡名行中心 − 0.3765`**、`= 战术卡名行中心 − 0.1890`（卡单位）。
   这两条把「底板↔文字」和「底板↔卡中心」钉在一起 —— 单改任何一边都过不去。
3. **（一个值 ≠ 全部情况 · 铁律 5·c）两档都断**：`d.keywords` 非空 → Big（1.7766×1.55 @ −0.65）；**为空 → Small（1.8635×0.9621 @ −0.94）**。现在**一条都没有**。
4. **（防③复发）别只断 `activeSelf`** —— 至少加：
   - `_textBg.sharedMesh != null && _textBg.sharedMesh.vertexCount == 4`
   - `_textBg.sharedMaterial != null && _textBg.sharedMaterial.color == (0,0,0,0.647)`
   - **一条渲图像素判据**：在 `CardFaceProbe` 的渲图里取「底板矩形内 / 紧邻其外」两块的行均值，断言**比值 < 0.8**（今天实测 = **0.78** 上下、无台阶 ⇒ 这条会**当场红**）。这是唯一能抓住"活着但不出像素"的形状。
5. **（同类风险）**`SpriteQuad` / `GemMesh` 那几个**静态缓存网格**在生成后加一条 `vertexCount == 4` 的断言（防 `UnloadUnusedAssets` 把 `sharedMesh` 回收成 null —— 现在没有任何断言盯这个）。

---

## 顺手发现（看见了但没改）

1. **简报与代码有一处对不上**：简报写「`UnitDescBottom01 = 0.834` 是对着 PnP 量出来的」，而 `CardView.cs` 里 **0.834 是 `RaceDescBottom01`**（:275），**`UnitDescBottom01` = 0.8466**（:263）。两条都断言过，别把 0.834 当 Unit 的。
2. **🔴 整张卡面比原版低 `0.08` 卡单位（相对 `2DCard`）** —— 原版是 `Front +0.08` + 局部值（`§判据 ①/②`），我们**除底板外全部只用局部值**。这一条**在画面上看不见**（卡面自己就是卡的外形，没有参照物），但它是**真的**、且**逐层都在**：`ArtUnitY` / `FrameUnitY` / `ShadowY` / `CostAt` / 四个数值 / 六条文字。要"完全复刻"就得**整棵 `Front` 子树一起 `+0.08`**（一次性、不动任何层间关系）。**本轮没做**（白名单 + 简报明写「不许顺手改别的层」）。已记在 `CardView.cs:399-403`。
3. **`RaceDescBottom01 = 0.834` 的"原版"依据比注释弱**：它比的是「我们渲图 vs PnP 印品」，而 PnP **自己的跨卡离散 ≥ 0.024 卡高**、定位器还有 ±3% 系统误差（§PnP 表）⇒ 它其实应当**标成「我们挑的（照 PnP 旁证）」**，不是"原版值"。**⛔ 不是叫改回去**，只是陈述依据强度。
4. **底板的 9-slice 没做**：贴图实测**不是圆角矩形**，是**竖直 alpha 渐变**（第 0 列 4→18→62→105→166→215→239，中间 255，末两行 250/239；**同一行内左右几乎恒定**）；原版按 `Sliced` + `border(7,0,7,9)` + `ppuMul 14.7` 画。我们整块拉伸 ⇒ 上下淡出被按整卡高拉长、左右那 7px 的圆角被拉成整屏宽。
5. **`CardView.cs:379-381` 的注释还在说「我们的 z 取 **+0.015**」，而实现是 `TextBgZ = −0.012`** —— 旧注释残留（下面几行 :393-404 才解释了为什么改成 −0.012）。同一段里两套说法，容易误导。
6. **`TextBgVisible`（`CardView.cs:1017`）只在 `Editor/BattleScene.cs:10027` 被用**，断的是「**场上**不画底板」。**手牌/卡池/收藏/详情窗没有任何一条断言碰过这层**（见 §建议断言 4）。

---

## 类型检查读数（运行时 / 编辑器）

```
$ TMPDIR=/tmp/wf_b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（改完 `.cs` 立刻跑，两个读数都是 **0**。⚠️ 按铁律 12，**本轮没跑任何 Unity 自检** —— 待办没做完不跑；`CardView.cs` 是卡面共用件，同步点该覆盖的是 `CardBaseDemo.Run` + `BattleScene.Run` + `DeckScene.Run` + `CardFaceProbe` 并排图。）

**行尾核对**：`CardView.cs` 改前 CRLF 5412/5412，改后 **5428/5428**（没翻）；`git diff --numstat` = **18 / 2**（不是整篇重写）。

---

## ① 底部横栏 = lore —— **确认主对话的裁定**（未改代码）

**一句话：确认。** 我按简报给的路径现读了两个方法体，与裁定逐字一致，并补两条**新读数**（都是「顺手发现」，没动代码）：

- `d:/2/tools/decomp_full/RawCardScript__GetLocalizedFlavourText.c`（27 行）：取卡自己的 `+0x178`（空则退 `+0x38`）→ `System_String__Concat(prefix, 值)` → **`I2_Loc_LocalizationManager__GetTranslation(...)`** ⇒ 真值在 **I2 词条表**里，**本地没有**（远端 CCD）⇒ 属铁律 11 例外①。裁定成立。
- `d:/2/tools/decomp_full/CardDisplayWindow__SetCardLore.c`（46 行）：拿到译文后 `UnityEngine_GameObject__SetActive(FlavourTextBG, **!IsNullOrEmpty(译文)**)` —— **译文为空时它会【把整条横栏关掉】**，而且横栏的**底图**是 `FlavourTextSO__GetClanFlavorBackground(...)`（按阵营取的一张背景图）。
  ⇒ 🆕 **两条值得记的**：① **「lore 拿不到」在原版里对应的是「整条横栏不显示」，不是「横栏里画别的东西」** —— 我们目前「拿不到就画效果文字 + `Debug.Log`」是**我们自己的中间处置**，若要走「原版行为」那一档，应当是**把横栏隐藏**；② 那条横栏的**底图**来自 `FlavourTextSO`（**本地有类 `FlavourTextSO.cs`**，资产未定位）⇒ **底图可能拿得到**，缺的只有文字。**⛔ 本轮没改**（简报明令不许动这段）。
