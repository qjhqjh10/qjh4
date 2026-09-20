# 原版「场上卡」的 3D 卡体（`3DBody`）—— 规格与接线

> 2026-09-19 建 · **2026-09-20 更新：已经接上了**（`CardView.BuildBody3D` + `Shaders/Card3D.shader`）。
> 起因：用户问「场上用平面立绘顶替原版的 3D 卡模型，原版是什么样子的？」
> 结论 = **资源全在本地、也已经导进工程了，缺的不是资产是接线**。
> 关联：`资料/战场还原度_差距清单_0917.md`（战场四大类差距）· `项目任务.md` 待办第 12 行。
>
> ✅ **2026-09-20 全部闭合**：① 3D 卡体接线（`CardView.BuildBody3D` + `Shaders/Card3D.shader` +
> `工具/import_original_3dcard.py`）② **场卡搬进真 3D**（直立站 `MinionArea` 线、y=0、缩放/槽位/朝向逐值照原版；
> 判据 = `Board/ArenaSlots.cs`；**落点判定与拖拽也按透视投影算**）③ **取景**（`BattleScene.BoardFramer` =
> 原版运行时的 `CameraVerticalFramer`；**原来那个静态 `lensShift` 从来没生效过**）——
> 见 §四 / §四之三 / §四之五。
> ⚠️ 唯一还差的是 `MatCap` **本地只解出 tier1 一张**
> （原版按 `CardTier` 取数组，其余档没有 ⇒ 四档先用同一张）。

---

## 一句话

原版场上的卡**不是一块立绘板，是一张「贴了立绘的厚 3D 卡」** ——
薄板 + **滚圆的底边**，靠 **matcap 假光照**出立体感，材质是 **Unlit**、**不吃实时灯光**。

---

## 一、它是什么（实测数值）

| 项 | 值 | 出处 |
|---|---|---|
| 网格 | **`Card 3D WH40k`** · 881 顶点 / 2499 索引 / **833 三角面** / 1 个 submesh | `bundle_battleprefabs_vfxandmisc_assets_all/Mesh/Card 3D WH40k.obj` |
| 姊妹网格 | `Card 3D Subdivided`（1641 顶点）—— 同一外形同一 bbox，**均匀细分，供顶点级形变动画用** | 同上 |
| 包围盒（卡单位） | X **−1.0482…1.0419**（宽 **2.0901**）· Y 0.0124…2.9729（高 **2.9604**）· Z ±0.9423（厚 **1.8846**） | 实读 mesh |
| **几何形状** | **薄板 + 滚圆的底边**：y>0.55 的主体里 \|z\| 只到 0.09（板厚 ≈0.13）；最低的 0.25 个单位（y 0…0.25，占 **345/881 顶点**）向外卷到 z=±0.942，且 \|z\| 随 \|x\| 增大而减小 ⇒ **底边是一段滚圆弧面** | 实读 mesh |
| ⚠️ 不是 | **不是平板 quad，也不「浮雕」**（角色并未在 z 上凸起） | 实读 mesh |
| 顶点通道 | position / normal / color / **UV0 / UV1 / UV2**（**无 tangent**） | 同上 |
| ⚠️ UV0 | 是**图集布局**（正面大 quad u 0.8956–0.9410 / v 0.8763–0.9063；背面 0.9518–0.9921；侧边跨满整图）⇒ **直接拿整张卡面 sprite 采样会取到错误子矩形** | 同上 |
| 🔴 **UV1 = 立绘 UV**（2026-09-19 查实） | 正面四角 `(0.1818,0.0017)/(0.8203,0.0017)/(0.1833,0.9978)/(0.8188,0.9978)`；**背面 0.0152–0.1276、滚边 0.836–0.989 故意落在 0.18–0.82 之外** ⇒ 被 mask 挡住、回落到 `_BaseMap` 底材 | `Mesh/Card 3D WH40k.obj` + PS 反汇编 |
| **UV2 = 计数器开关**（4 分量、整网格只有两个值） | `(0,0,0,1)`×770 / `(1,1,1,1)`×111；那 111 个正是**正面下部计数器面板**。shader 只用 `.x`：`lerp(matcap着色, _BaseMap×UV2.x×_CountersIntensity, UV2.x)` | 同上（ch6：off44 dim4 float32） |
| **UV0 岛布局**（512² `WF 3D Card_Card 3D_BaseColor.png`） | 正面大 quad `u .8956–.9410 v .8763–.9063`（纯黑，被立绘盖住）· 背面 `.9518–.9921`（深灰）· 左右侧条 `.667–.761 / .761–.841`（竖条金属）· **底部滚边** `u 0–.667 v 0–.710`（黑团+灰环）· **正面下部计数器面板** `u .030–.676 v .640–.996`（三彩钮：绿 u.245–.494 / 紫 u.030–.234 / 右侧红钮） | mesh UV 岛（连通分量）+ 像素内容核 |
| 🔴 **2026-09-19 更正** | 本文原写「正面 u 0.066…0.665 / v 0.625…0.989」—— **那是正面下部那些面板（计数器等）的 UV，不是正面大 quad**（大 quad 是 `0.8956–0.9410 × 0.8763–0.9063`）。两者别混 | 同上 |
| 预制体 `Card 3D` | `localRotation` = 绕 Y 转 **180°**；`localScale` = **0.88586**（均匀）；父节点 = `3DBody` ⇒ 世界尺寸 ≈ **1.852 × 2.622** | `GameObject/Card 3D.json` · `Transform_-5606686786952979520.json` |
| 材质 `Card 3d Lvl1` | Shader Graph「Universal Unlit」（Opaque / Unlit）· 14 个属性：`_BaseMap`（图集）· **`_CardImage`（每张卡的立绘，运行时灌）** · `_MatCap` + `_MatCap_Intensity` + `_MatCapPower`（**按稀有度换**）· `_CountersIntensity` · `_USE_BLEND` · 两个 SG 变量贴图 | `Material_791121889026168482.json` · `Shader_-7938055025392973240.json` |
| ⇒ 立体感的来源 | **matcap 假光照 + 底边滚圆**（材质 Unlit） | 同上 |

## 二、代码怎么用它（切换时机）

- **切换总闸** `BattleCardUI__ShowBoardObjects.c:13-23`：`Card2DController.SetCardVisibility(false)` →
  `body3D != null` 时 `ToggleBody3D(option)` → `Card2DController.Toggle(option ^ 1)`
  （这就是已知的 **inPlay 关掉整张 `2DCard`**）⇒ **3D 体与 2D 卡互斥**。
- `BattleCardUI__ToggleBody3D.c:15-31`：`body3D.SetActive(optionBody)`；传了 `optionEffectAnchor` 就覆盖它；
  **若 `cardScript.HasCurrentTrait(0x4b0)` 为真则强制 false**；最后开关 `effectsAnchor.gameObject`。
- `BattleCardUI__ChangeCardToMinion.c:7-12`：**单位上场即显示 3D 体**（`CardType ∈ {0,10}`）。
- **立绘怎么灌** `BattleCardUI__SetCardImageTo3DBase.c:22-49`：取 `rawCard.cardSprite.texture` 写进
  `minion3DRenderer.material` 的 **`_CardImage`**；带 0x4b0 则改灌 `CardTrait.GetTrait(0x4b0).CustomObject`；
  收尾 `CardMaterialHelper.GetCardMatCapByCardTier` → **`_MatCap`**。
- `CardMaterialHelper__GetCardMatCapByCardTier.c:10-23`：matcap 数组按 **`RawCardScript.CardTier`** 索引（越界钳末位）。
- **残骸体**（第 8 行阵营资源那条会用到）：`RemnantBody__Initialize.c:15-19` +
  `RemnantBody__BodyVisibilityToggle.c:12-18` —— 走同一个 `ConfigureMaterial`，可见性转调 `ToggleBody3D`。
- 关键字段偏移（`dump.cs`）：`body3D` 0x158 · `body3DRangeAnchor`/`MeleeAnchor`/`HealthAnchor` 0x160/0x168/0x170 ·
  **`minion3DRenderer` 0x180**（上面所有 SetTexture 的目标）· `effectsAnchor` 0x1A0 · `originalMaterial` 0x2C8 ·
  `RemnantBody3D` 0x2E0。

## 三、资产在哪 · shader 规格（✅ **2026-09-20 已全部接上**；`Card3D.shader` 注释里引的「§三 缺口 1」= 本节）

**资产在本地、而且已经导进工程了** —— 缺的不是资产：

| 已有（工程内） | 路径 |
|---|---|
| 真 Unity Mesh | `Assets/WarpforgeVFX/Meshes/Card 3D WH40k.asset` · `Card 3D Subdivided.asset` |
| 材质 | `Assets/WarpforgeVFX/Materials/Card 3d Lvl1.mat` |
| 贴图 | `Assets/WarpforgeVFX/Textures/WF 3D Card_Card 3D_BaseColor.png` · `Card base Matcap.png` · `MatCap Card Level 1.png` |

**现状（2026-09-20）**：落地 shader = `Assets/CardPresentation/Shaders/Card3D.shader`（照原版 PS 逐行转写：
`_CardImage` 走 UV1 + 写死 mask、`_MatCap` 走视图空间法线）。场上卡体由 `CardView.BuildBody3D` **程序化建**
（`body3D` 子节点 · `localRotation` = yaw 180° · `localScale` = **0.88586** · `MeshFilter` → `Card 3D WH40k` ·
`MeshRenderer` → `CardPresentation/Card3D`；原版那些兄弟节点 `TraitIcons` / `HealthText` 由我们自己的徽标/数值层顶上）。
`CardFace.Board` 时把原来那块平面立绘（`AddLayer("art", …)`）换成 3D 卡体，**取不到网格/shader 时退回 2D 立绘并打 warning**（不静默）。
🔴 **尺寸照原版用 `localScale 0.88586`（世界 1.852×2.622）** —— 理由是**徽标位置**（`Core/Badges.cs`）当初就是按
**这个 bbox** 换算的，两边必须同一个尺子；原版场上那张本来就比手牌那张小（手牌用 `2DCard` 的 2.0927×3.3313）。
**不再另设「按宽/按高对齐」。**
⚠️ 工程里那份 `Card 3d Lvl1.mat` 是被降级的 `URP/Unlit`（`_CardImage`/`_MatCap` 全丢），**别用它**。

> ✅ **原版 shader 已逐行反汇编**（`Shader_-7938055025392973240.json` 的
> `compressedBlob` → LZ4 → DXBC → `d3dcompiler_47.D3DDisassemble`）—— **原版属性表**
> （落地版 `Card3D.shader` **属性同名、值照抄**）：
> `_BaseMap`(2D，采样用 **UV0 原样**、不加 tiling/offset) · `_CardImage`(2D，用 **UV1** 采样，RGB 作正面输出) ·
> `_MatCap`（采样法相同）· `_MatCap_Intensity` = **1.69** · `_MatCapPower` = **1.24**（`cap = pow(cap, _MatCapPower)`）·
> `_CountersIntensity` = **1.12**（配 UV2.x）· `_USE_BLEND`（5 个 pass 的编译产物里**没引用**）可不管 ·
> 两个 `_var3DCardColor_..._Texture2D` 在 23 KB 字节码明文里**没有** ⇒ 死属性。
> ⚠️ 原版的 ISGN 里**没有 COLOR** ⇒ **不乘顶点色**，否则滚边顶点色 (0,0,0) 会**全黑**。
> 我们那边的 `_Color` 是**自己加的**（整卡着色 / 淡出用，原版没有）；`_ExtraAmbientColor` / `_FogContribution` /
> `_APPLYAMBIENTCOLOR_ON` / `_CastShadows` / `IN.color 相乘` 是旧版残留，**一律不要**。
> ⚠️ **没有 Shader Graph 源资产、节点图查不到** ⇒ 只能照字节码转写。
>
> **Fragment 骨架**（照 PS 逐行对译）：
> ```
> base = _BaseMap(UV0);
> art  = _CardImage(UV1) * _BaseMap(UV1).a;          // 立绘那块用 _BaseMap 的 alpha 当遮罩
> cap  = pow(_MatCap(viewNormal.xy*0.5+0.5) * _MatCap_Intensity, _MatCapPower) * <cb0[56].xyz>;
> col  = lerp(cap * base.rgb, base.rgb * UV2.x * _CountersIntensity, UV2.x);
> m    = (UV1.x > 0.18 && UV1.x <= 0.82 && UV1.y > 0) ? 1 : 0;   // ← 编译期字面量 l(0.82,1,0.18,0)
> o.rgb = saturate(col * (1 - m) + art.rgb * m);  o.a = 1;
> ```
> **顶点结构要补** `float2 uv1 : TEXCOORD1; float4 uv2 : TEXCOORD2;`（现 `Varyings` 没有）。
> ⚠️ mesh 正面 UV1 是**镜像**的（x=+ → u 小），原版靠预制体 **yaw 180°** 翻回来 —— 我们不加那个旋转就得**翻 U**。
> ⚠️ **唯一查不到的一处**：`mul r0.xyz, r0.xyzx, cb0[56].xyzx`（matcap 之后乘的那个全局向量）——
> DXBC 被剥了 RDEF、`$Globals` 里只列 `_GlobalMipBias` / `unity_AmbientSky`。
> 要么拿 `unity_AmbientSky` 顶、要么先当 1（只影响 matcap 的亮度色调）。**别再花时间找它的名字。**
>
> **`_CardImage` 贴的是「纯立绘」**（不是合成卡面）：`BattleCardUI__SetCardImageTo3DBase.c:22,31,33` 取
> `rawCard.cardSprite` 的 `.texture`，`:49` `SetTexture(_CardImage, 它)` ⇒ 灌进去的是**未裁的 1024² 立绘整图**。
> 三处数值互咬死这一点：mesh 正面 UV1 = 0.1818–0.8203 ／ shader 写死的 mask = 0.18–0.82 ／
> 立绘 alpha 内容 = u 0.179–0.819。

✅ 场上卡**已搬进真 3D**（见 §四之三：直立站 `MinionArea` 线、y=0、按**透视** `BoardCamera` 画）。

### 一条别踩的

- **「28 个网格」不是卡体**：`Assets/WarpforgeArena1/Scenes/battlearena1.unity` 里那 28 个网格
  **全是场景道具**（Barrel / Fences / Cannon / Vehicle / Platform…），**不含 `Card 3D`**。别拿它当卡体来源。
  （场景里唯一用到 `Card 3D WH40k` 的是 **`Cache Stealth`** —— 定案见 §四末：那是**素材预热缓存**，
  `m_IsActive:false`、挂 `Cache [No delete]` 下、坐标 (99.53,…) 是 prefab 自带偏移；**既不是卡体来源、也不是交接网格**。）

## 四、场上卡在 3D 里的**姿态与站位**（2026-09-20 查实）

> 起因：第 12 行要把场上的卡搬进透视那层。原计划写的是「把 2D 槽位投到**地板平面**上」——
> **查完发现那条思路是错的：原版场上的卡是直立站着的，不是躺在地板上。**
> ⚠️ 这一节是「先查后做」救回来的一例：`BoardCamera` **没有俯仰**（rot 是单位四元数），
> 真照原计划把卡躺平，画面上会**侧看成一条边**。

| 项 | 结论 | 出处 |
|---|---|---|
| **姿态** | **直立**（竖着站、底边压在地面上），**不是躺平** | 进场旋转被显式归零：`MinionManager__MoveMinionToConversionPoint.c:58-59`（`DOLocalRotate` 到零向量）· `:72`（`set_localRotation` 是同一常量，判为 `Quaternion.identity`）· `CardScript__UpdateMinionInPlayPosition.c:118`（每次落位先重设世界旋转、再只动 localPosition） |
| 为什么是直立 | 网格**原点在卡的底边**（`Card 3D WH40k` 实测 Y 0.0124…2.9729）⇒ 局部 Y 就是长轴，站立时底边正好贴地 | 本文 §一 · `Mesh/Card 3D WH40k.obj` |
| 预制体链上的旋转 | **只有 yaw 180° 一个**（在 `Card 3D` 子节点上，用来把镜像的 UV1 翻正） | `Transform_-5606686786952979520.json` |
| 动画也不翻转 | `Card Hand To Board` 的 `m_RotationCurves` / `m_EulerCurves` = **0 条**，root delta pose = `(0,0,0,1)` | `[PF]AnimationClip/AnimationClip_-3614292623624764332.json` |
| **站位** | **就是 `MinionArea` 那条线，y = 0（不抬起）**。`MinionManager` 脚本**就挂在场景的 `MinionArea` 节点上** | `[A1]GameObject/MinionArea.json` + `Transform_1309.json`（玩家，local (0,0,−6.655)）/ `_1314.json`（敌，local (0,0,1.043)）；父链 → `BattleBoardElements`(100,0,0) |
| **槽位公式** | 玩家行落点 = **(100 ± (0.82k + 0.09), 0, −6.655)**；敌行同式、`z = 1.043` | `MinionManager__FillMinionPositions.c:33-35,55-56`（**y 被显式写成 0**）；数值 `MinionSeparation 0.82` · `minionExtraDistanceFromHero 0.09` · `ExtraYOnBoard 0` · `slotsPerSide 4` = `[A1]MonoBehaviour_4372.json`（玩家）/ `_4373.json`（敌） |
| 朝向 | 卡根 world rotation = identity ⇒ 卡面朝 **−Z**，正对 `BoardCamera`（它在 z=−13.572 朝 +Z 看） | 同上 |
| 地面高度 | `Floor plane` 在 y ≈ 0.10 | `[A1]Transform/… Floor plane` |

**手牌 → 棋盘：是「飞过去」的两段补间 + 落地，不是直接出现**：

| 段 | 做什么 | 时长 |
|---|---|---|
| ① | `PlayMinionFromHand`：取 `MinionManager` 世界位置 → `CamerasConversionHelper.ConvertPositionAndScaleUIToBoard` 把手牌 UI 位换算进棋盘空间 → `SetParent(MinionManager)` | `MinionManager__PlayMinionFromHand.c:14-22` · `CardScript__SetCardPosAndScaleToBoard.c:22-46` |
| ② | `DOLocalMove` 飞到**槽位 + up × `minionConversionHeight`**（玩家 **1.0** / 敌 **2.1**），同时 `DOLocalRotate` 归零 | **0.30 s**（`minionToConversionPointTime`）—— `MinionManager__MoveMinionToConversionPoint.c:90-96` |
| ③ | 落到 y=0（`UpdateMinionInPlayPosition(slot, timeToLand, tween:true)`）→ `LandOnGround`（扬尘粒子 + 相机抖动 + 落地音） | **0.2 s**（`timeToLand`）· 落地前 **0.05 s**（`timeBeforeLand`）—— `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:135-151,186-205` |
| ⚠️ | `Card Hand To Board` clip 实长 **0.9167 s**（55 帧 @60fps）、唯一事件在 **t=0.55 s**；它在 0.167 s 打开 `Board Elements`（3D 体）、0.70 s 关掉 `2DCard`。**它没有位移/旋转主曲线** ⇒ **别拿它当位移曲线**，只当 2D→3D 交接与特效时间轴 | `AnimationClip_-3614292623624764332.json` · `CardScript__GetHandToBoardAnimEventTime.c:17-29` |

✅ **`Cache Stealth` 定案**（原来在本文与差距清单里都列为「存疑项」）：它是**场景里的素材预热缓存** ——
`m_IsActive: false`，层级 `Cache Stealth` ⊂ **`Cache [No delete]`** ⊂ `BattlePrefab`、与 `BattleCacheManager` 同级；
网格 = `Card 3D WH40k`、材质 = **`Card 3d Stealth`**（外部 material −4903742837201913907）。
它的坐标 `(99.53, 0.05, −1.28)` 与卡 prefab 根节点**自带的自定义本地偏移完全相同** ⇒
**那个 `99.53` 是 prefab 自带偏移、不是棋盘上的某个特殊格位，别当锚点用**；
它**既不是场上卡、也不是手牌→战场的交接网格**。
### 四之二、3DBody 空间与「数值 / 徽标」的**真实锚点**（2026-09-20 逐份 JSON 实读）

🔴 **`3DBody` 空间的 y=0 就是卡底边，而且链上没有任何补偿**
（`CardPrefab` → `Board Elements` → `3DBody` 三者 `localScale` **全是 1**，`localPosition` 也全是 0）
⇒ **3DBody 空间 = 卡自己的坐标空间**。我们那边把「卡中心」当原点（卡本体 2.0927 × 3.3313），
所以换算只有一步：**`ourY = 3DBody.y − 3.3313/2`**。

> ⚠️ **不要乘 0.88586**。那个数是 **`Card 3D`（网格自己那个节点）** 的 `localScale`；
> 数值/徽标节点和 `Card 3D` 是**平级**的兄弟，活在**未缩放**的 3DBody 空间里。
> （曾经的换算 `ourY = (origY/2.96 − 0.5) × 3.3313` **两处都错** —— 既乘了 1.125 又按 3.3313 拉伸。）

**`3DBody` 的 10 个直接子节点**（相对 3DBody 的 localPosition，全部实读）：

| 节点 | pos | 是什么 |
|---|---|---|
| `Card 3D` | (0, 0, 0) · scale **0.88586** · yaw 180° | 卡体网格 `Card 3D WH40k`（= `BattleCardUI.minion3DRenderer`） |
| `TraitIcons` | (0.296, 1.533, −0.014) | 7 个徽标容器的父节点（0.296 用来抵消容器内部枢轴不对称：−0.859+0.296=−0.563、0.267+0.296=+0.563） |
| `Base Attack Counters` | (−0.528, 0.656, −0.049) · scale 0.13924 | 攻数值挂点（内含 Melee/Range 两个子容器，scale 7.181786 —— **0.13924×7.181786 = 1.0000**，两层精确抵消） |
| `Base Health Counters` | (0.499, 0.354, −0.207) · scale 0.13924 | 血数值挂点 |
| `Armour Container` | (0.663, 0.771, −0.053) | 甲数值挂点 |
| `DamageCounter` | (0, 1.708, 0) | 飘伤害数字 |
| `Minion Death Icon` | (0, 1.710, −0.128) | 死亡图标 |
| `MinionLight` | (−0.001, 1.326, 0) | SpriteRenderer 光 |
| `SwarmIcon` | (0, 3.329, −0.05) | 虫群图标（**超出卡顶 y=2.634**） |
| `CanActParticles` | (0, 0.030, 0.006) | 可行动粒子 |

**🔴 攻/血/甲在原版是「四个独立的世界空间 TMP 文本节点」，不是贴在网格的计数器面板上**
（网格确实自带三个彩钮，但数字是另外的节点画上去的）：

| 文本节点 | 3DBody 坐标 | 我们的卡单位（`y − 1.66565`） | 字段（`dump.cs`） |
|---|---|---|---|
| `Melee AttackText` | (−0.680, 0.740, −0.077) | (−0.680, **−0.926**) | `body3DMeleeAnchor` `:26179` |
| `Range Attack Text` | (−0.434, 0.406, −0.071) | (−0.434, **−1.260**) | `body3DRangeAnchor` `:26178` |
| `HealthText` | (0.566, 0.357, −0.080) | (0.566, **−1.309**) | `body3DHealthAnchor` `:26180` |
| `Armour Text` | (0.678, 0.772, −0.063) | (0.678, **−0.894**) | —（**没有** anchor 字段） |

⇒ 落点 = `CardView` 的 `BoardMeleeAt / BoardRangedAt / BoardHealthAt / BoardArmourAt`。
⚠️ **我们自己量的网格计数器面板**（取 UV2.x==1 那 111 个顶点）给出 melee x≈−0.668 · ranged≈−0.423 ·
health≈+0.555 —— 与上表**互相印证**，两条独立路径同结论。
⚠️ **「谁读那三个 anchor 字段」查不到**（`decomp_full/` 全目录 grep 无命中，只有字段声明）；
唯一确认的用途是 `HighlightAttackType` 里当 `textCounter` 把数字放大 1.35×。

**7 个徽标容器**（`TraitIcons` 的子节点，容器 scale **0.750**）：

| 容器 | 容器局部 x | 容器局部 y | 合成到 3DBody |
|---|---|---|---|
| `TraitIconContainer 1/2/3`（左） | −0.859 | 0.826 / 0.401 / −0.039 | (**−0.563**, 2.359 / 1.934 / 1.494) |
| `TraitIconContainer Right 1/2/3/4`（右） | +0.267 | 0.826 / 0.401 / −0.039 / −0.479 | (**+0.563**, 2.359 / 1.934 / 1.494 / 1.054) |

🔴 **容器 ≠ 图标**（2026-09-20 修的第二处）：每个容器下还有两个子节点 ——
`Container`（**图标本体**，x = **∓0.287**）与 `IconBackground`（**底板**，x = **∓0.300**、y = **−0.020**）。
这两个数活在**容器自己的空间**里 ⇒ 乘容器 scale **0.750** 才是卡单位：
**图标比容器再往卡外 0.21525 · 底板再往外 0.225 并下移 0.015**。
（我们原来把图标**直接画在容器位置上** ⇒ 7 个徽标整体**偏内 0.215 卡单位 ≈ 卡宽的 10%**；
而底板原来被设成「摆在图标正后方」，理由是「我们是平面 2D 卡」—— 那条前提随 3D 卡体一起作废了。）
⇒ 落点 = `Badges.IconOutward` / `PlateOutward` / `PlateDy` + `CardView.BadgeIconAt01`。

### 四之三、场上的卡**落在哪、多大**（2026-09-20 实读；代码判据 = `Board/ArenaSlots.cs`）

| 项 | 玩家侧 | 敌方侧 | 出处 |
|---|---|---|---|
| `MinionArea` 的 z | **−6.655** | **+1.043** | `MonoBehaviour_4372/4373.json`（父链 `BattleBoardElements`(100,0,0)，**链上 scale 全 1**） |
| 槽位 x | **±(n·0.82 + 0.09)**，n=1..4 | **±(n·1.53 + 0.09)** | `MinionManager__FillMinionPositions.c:33-35,55-56`（**y 被显式写成 0**） |
| 卡根缩放（部队） | **0.36** | **0.69** | `BattleManager__GetUnitSizeInPlay.c:21-29` = `MinionManager.desiredScale`；按 `EntityScript.isPlayer` 取哪一份 |
| 卡根缩放（督军） | **0.4** | **0.77** | 同上（`heroScale`） |
| 卡根旋转 | identity | identity | `MoveMinionToConversionPoint.c:56-59,66-72` · `UpdateMinionInPlayPosition.c:105-118` |
| 卡根位置 | **就是槽位坐标**（`ExtraYOnBoard = 0`，无抬升） | 同 | `MoveMinionToConversionPoint.c:41-47,102-105` |

- **9 个格的对应**：原版 `MinionManager` 只填 **8 个小兵槽**（两列各 4），**中央是督军位**（
  `GetIndexFromSlot` / `OnDrawGizmosSelected` 把 hero 画在 MinionManager 原点）⇒ 与我们
  `BoardSpec.Size = 9 / WarlordSlot = 4` 一致。
- **我们与它的唯一约定差**：原版**卡根就落在地面上**（`3DBody` 的 y=0 = 卡底），
  而我们的卡根是**卡中心** ⇒ 落点要加 `CardUnitH/2 × scale` 补偿（`ArenaSlots.RootPosition`）。
  补偿后**身体的世界跨度与原版一致**（原版 0.004…0.948，我们同样 0…0.948）。
- 🔴 **「MB 4373 是旧预设」那条作废**：4372/4373 = **玩家 / 敌方两份**（和 `CardsHorizontalLayout`
  MB 5271/4053 那次是同一个误判）。屏幕上一验自洽：玩家步距 0.82 × 182.14 = 149.3 px、
  敌 1.53 × 86.2 = 131.9 px；玩家卡宽 2.0927 × 0.36 × 182.14 = 137.2 px、敌同式 = 124.5 px。

### 四之四、`708 / 466` 那两行 —— **不是原版值**（本节原来的「已查清」结论已作废）

⚠️ 本节原来那套「相机 / 场地 / 模型三者一致 ⇒ 卡可以落在 79%」的推导**是错的** ——
前提「我们的相机 = 原版相机」不成立。**否定结论、真值与根因只在 §四之五**，本节不留第二份。
唯一留下的是两条**没随它一起作废的东西**：

- **仍未重算**：**手牌行** —— `HandLayout.DefaultBaselineY = 0.1204` 是**我们挑的**
  （原版 `HandAnchor` 链是 **961/1080 ⇒ 0.110**，见 `HandLayout.cs:54-60`）；与取景是两件事。
- **待核（不影响落点）**：「槽局部 y offset (MB4372 −0.6 / MB4373 +5.0) 归属待核」（`审查更正清单_0827.md:95`）——
  `MinionManager__FillMinionPositions.c:33-35,55-56` 把 y **显式写成 0** ⇒ 落点不受它影响。

### 四之五、🔴 根因：`lensShift` **一直没生效**（我们的 bug），原版还会**运行时重算**它

> **一句坑**：`Camera.lensShift` **只在 `usePhysicalProperties = true`（物理相机模式）下有效** ——
> 我们原来**只设了 `lensShift`、从没开物理相机**，那个 `−0.205` 被 Unity **静默忽略**，3D 战场整个**低 ≈150 px**；
> 而两条断言只比 `fieldOfView == 46.397` 与 `lensShift == −0.205`（**两条都过**）
> ⇒ 又一次「**断言钉住了错的东西**」。

**原版本来就是物理相机**（`07_场景/battlearena1/Camera/Camera_1461.json`）：
`m_FocalLength 28.0` · `m_SensorSize (41.5, 24.0)` · `m_GateFitMode 2`（Horizontal）·
`m_LensShift.y −0.205`。⚠️ `m_GateFitMode = Horizontal` 意味着 16:9 下 **vFOV ≈ 45.26°**，
不是按**竖直** fit 算的 46.397°。

**而且原版运行时还会重算它** —— `BattleCameraSreenSize` → `CameraVerticalFramer.CalculateFraming`
（`D:/2/tools/decomp_full/CameraVerticalFramer__CalculateFraming.c`，265 行；组件实例 =
`07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4697.json`，它的 `boardCamera` 指向 pathID 1461
= 战场相机）。**这条链是必要知识，抄在这儿免得下次重查**：

```
maxUIViewportPosition = WorldToViewportPoint(enemyCardAreaSizeHelper.GetWorldCorners()[0])
maxUIViewportPosition.y = verticalPaddingModifierByAspectRatio.Evaluate(camera.aspect)
                        * verticalPaddingByZoom.Evaluate(zoom)
                        + maxUIViewportPosition.y
playerCardAreaSizeHelper.sizeDelta.y = 原值 × playerHand.CurrentSizeMultiplier
minUIViewportPosition = WorldToViewportPoint(playerCardAreaSizeHelper.GetWorldCorners()[3])
垂直视口高 = Mathf.Min(|两者世界 y 之差|, maxVerticalSizeInViewPort)      // 实例值 = 7.0
新 sensorSize.x = (sensorSize.x − cameraSizeXTable.Evaluate(垂直视口高)) * zoom
                + cameraSizeXTable.Evaluate(垂直视口高)
新 lensShift.y  = viewShiftModifier.Evaluate((maxY − minY) * k + minY)
```
> ✅ **2026-09-20 全接上了** —— 上面那几个未知数**全部读掉**，公式已落成 C#（`BattleScene.BoardFramer`）：
> · **`k` = `DAT_1834b2bb4` = 0.5**（`GameAssembly.dll` 直读：VA→RVA→`.rdata`）⇒ 曲线输入取 **`[minY, maxY]` 的中点**
> · **`DAT_1834b2bb8` = 1.0**（`zoom` 上界，也是 `CombatCameraZoom.GetMaxZoomLevel` 的默认返回）⇒ `verticalPaddingByZoom(1.0) = −0.171`
> · **角点**：max 读 **corner[0] = 左下**、min 读 **corner[2] = 右上**（读的是 `+0x20` / `+0x38`，数组从 `+0x20` 起、每点 12 字节）
> · **两个 helper 的几何**（运行时 UI dump `runtime_ui_dump_drive_0912.tsv:340,348`）：
>   `PlayerCardAreaSizeHelper To Use` 高 **249.9**、pivot **(0.5,0)** ⇒ **上沿** = 249.9/1080 = **0.2314**；
>   `EnemyCardAreaSizeHelper Data` 高 **96.7**、pivot **(0.5,1)** ⇒ **下沿** = (1080−96.7)/1080 = **0.9105**。
>   （父节点 `BottomAnchor`/`UpperAnchor` 的 anchor 分别贴在 canvas 的**底 / 顶** ⇒ 直接就是 viewport y。）
> ⇒ 16:9 下 **`lensShift.y = −0.2103`**，实测落点：

| | 接上算法之后 | （旧的「`battle.gd` 旧校准」值） |
|---|---|---|
| 我方行卡心 | **58.90% 距顶 = 636 px** | ~~708 px~~ |
| 敌方行卡心 | **39.37% = 425 px** | ~~466 px~~ |
| 我方卡**底边** | **67.1% = 725 px** | （旧值会让卡底落到 794 px） |
| 手牌上沿 | **75.8% = 819 px** | 同 |

🔑 **一条自洽性旁证（这条最有说服力）**：算法给出的取景下，我方卡底边（725 px）**压在手牌上沿（819 px）之上，留 94 px** ✓
—— 而**旧的 708/466 会让卡底到 794 px、离手牌只剩 25 px**，那本身就**不像一个成品游戏**。
⇒ **`708 / 466` 是 `battle.gd` 时代由旧投影算出来的**（`战斗重建_0827/审查更正清单_0827.md:95`
自己标着「由投影/旧校准；无独立 JSON 结束值·待核」）。**别再拿它当靶** —— 拿它当靶会逼着原版算法跑偏
（我中途就按它标定过一版 `−0.1381`，方向是错的）。

⚠️ **仍未算的一段**：`cameraSizeXTable`（按垂直视口高改 `sensorSize.x`）—— 在 `zoom = 1` 时
`(sx − c)·zoom + c = sx` **恒等于不变**，所以 16:9 下不生效，本次没接。**窄屏（zoom<1）时才要它。**

**实例里那三条曲线（`MonoBehaviour_4697.json`，实读）**：

| 曲线 | 关键帧 |
|---|---|
| **`viewShiftModifier`**（**输出就是 `lensShift.y`**） | t 0.345→−0.0965 · 0.3887→−0.1523 · 0.417→−0.1642 · **0.5→−0.2201** · 0.571→−0.2598 · 0.6514→−0.3330 · 0.7067→−0.4216 —— **全负** |
| `verticalPaddingByZoom` | 0.003→−0.032 · 1.0→−0.171 |
| `verticalPaddingModifierByAspectRatio` | 1.333 / 1.6 / 1.77 → **1.0**（≥2.333 才降到 0.65） |
| `cameraSizeXTable` | 4.93→41.08 · 5.59→35.68 · … 11.45→17.47（输入是**世界单位的垂直视口高**，钳 7.0） |

🔑 **一致性旁证**：我们需要把画面**上移**（我方行 79% → 实测 58.9%，见上面那张落点表），
而 `viewShiftModifier` **输出全负** ✓ —— 方向对得上；需要的值也落在曲线量程内。

**当前状态（2026-09-20）**：`BattleScene.BuildBoardCamera` 已开物理相机（focal 28 / sensor 41.5×24 /
gateFit Horizontal），`lensShift.y` 由 **`BoardFramer.LensShiftY(camera.aspect)`** 现算（**就是原版算法**、
**不再是 16:9 下的定值** ⇒ 宽高比变了自动重算）—— 逐值与实测落点见本节「✅ 2026-09-20 全接上了」那张表。
⚠️ **坑**：中途曾按旧表标定过一版 `−0.1381` —— **方向是错的、已废**，别再照着旧表标。

⚠️ **三个操作坑（本轮全踩了）**：
① **改完相机必须重跑 `BattleScene.BuildAndSaveScene`** —— 自检读的是磁盘上那个 `Battle.unity`，
　 不重建的话跑的**还是旧相机**（诊断读到 `lens=(0,-0.29)`、断言却读到 `−0.205`）。
② **改判据要 `grep` 全部调用点，别按变量名认** —— 改拖拽落点时漏了 `DriveTacticAndCheck` 那一处
　（它变量名是 `dropSlot` 不是 `freeSlot`），镜头一改那 4 条战术卡用例立刻红。
③ **判据要在判据成立的条件下量** —— 「行位对不对」必须在 **aspect 已知**的时刻量：
　`gateFit = Horizontal` 下竖直取景**跟宽高比走**，在 `Shot` 之前量会得到另一个数
　（同一个点：62.1% vs 66.08%）。而且要用 **`WorldToViewportPoint`**（0..1），
　别用 `WorldToScreenPoint` + `pixelHeight`（批处理下 `pixelHeight` 会变）。

> 📁 `[PF]` = `D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/` ·
> `[A1]` = `.../bundle_scenes_scenes_battlearena1/`





---

## 五、出处

- 反编译：`D:/2/tools/decomp_full/` 的 `BattleCardUI__{ToggleBody3D,ShowBoardObjects,ChangeCardToMinion,SetCardImageTo3DBase,SetCardMaterial}.c` ·
  `CardMaterialHelper__{ConfigureMaterial,GetCardMatCapByCardTier}.c` · `RemnantBody__{Initialize,BodyVisibilityToggle}.c` ·
  `CardScript__{DoBodyAnimation,SetAmbush}.c` · `Card3DAnimationController__Toggle.c`
- 字段偏移：`D:/2/tools/il2cpp_out/dump.cs:26177`(body3D) · `:26187`(minion3DRenderer) · `:26239`(originalMaterial) · `:26348`(ToggleBody3D)
- 资产：`D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`（`Mesh/` `Material/` `GameObject/` `Transform/` `MeshFilter/` `MeshRenderer/` `Shader/`）
- 场景：`Assets/WarpforgeArena1/Scenes/battlearena1.unity` · `资料/说明书/02_战场_场景/battlearena1.md:283,427`
- 我们这侧：`Assets/CardPresentation/Core/CardView.cs:119-131,737,760-765` ·
  `Assets/CardPresentation/Core/Badges.cs:47-51`（**已按实测 bbox 换算徽标位**，与本次实读一致）
- **查不到**：Shader Graph 节点图 · matcap 之后乘的那个全局向量（`cb0[56].xyz`，DXBC 剥了 RDEF）。
  ✅ **2026-09-20 定案：mesh pathID `−6960435115557275368` = `Card 3D WH40k`**（不是 `Subdivided`）——
  实据 = 场景 `Cache Stealth` 的 `MeshFilter_1521.json` 引它，而那是个卡 mesh 实例；`Subdivided` 无引用点。
  ~~`_CardImage` 的 UV 重映射规则~~ —— ✅ **2026-09-20 更正：根本没有「重映射」这回事**（立绘直接吃 mesh 的 UV1 + 一个写死的 mask）。
