# 原版「场上卡」的 3D 卡体（`3DBody`）—— 规格与接线差

> 2026-09-19 建 · 用途：**接原版场上那张 3D 卡**这件事的唯一输入。
> 起因：用户问「场上用平面立绘顶替原版的 3D 卡模型，原版是什么样子的？」
> 结论 = **资源全在本地、也已经导进工程了，缺的不是资产是接线** —— 差三步 + 两个必须先定的坑。
> 关联：`资料/战场还原度_差距清单_0917.md`（战场四大类差距）· `项目任务.md` 待办第 12 行。

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
| ⚠️ UV0 | 是**图集布局**：正面 u 0.066…0.665 / v 0.625…0.989；背面 u 0.742…1.0；侧边跨满整图 ⇒ **直接拿整张卡面 sprite 采样会取到错误子矩形** | 同上 |
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

## 三、我们要接的话差什么

**资产在本地、而且已经导进工程了** —— 缺的不是资产：

| 已有（工程内） | 路径 |
|---|---|
| 真 Unity Mesh | `Assets/WarpforgeVFX/Meshes/Card 3D WH40k.asset` · `Card 3D Subdivided.asset` |
| 材质 | `Assets/WarpforgeVFX/Materials/Card 3d Lvl1.mat` |
| 贴图 | `Assets/WarpforgeVFX/Textures/WF 3D Card_Card 3D_BaseColor.png` · `Card base Matcap.png` · `MatCap Card Level 1.png` |

**缺口 1（最大）：材质被降级了。** 工程里 `Card 3d Lvl1.mat` 的 shader 是 **URP/Unlit**，
属性只剩 `_BaseMap` / `_MainTex`；原版 Shader Graph 的 `_CardImage` / `_MatCap` / `_MatCap_Intensity` /
`_MatCapPower` / `_CountersIntensity` / `_USE_BLEND` **全丢** ⇒ **现在这张 mesh 根本贴不上立绘**。
原 shader 的编译产物在（属性表可读），但**没有 Shader Graph 源资产、节点图查不到**。
工程现成的 `Assets/WarpforgeVFX/Shaders/WFMatcap.shader` 只有 `_MainTex` + `_MatCap`、**没有 `_CardImage`**
⇒ **要自己写一个 matcap + 卡面贴图的 shader**。

**缺口 2：没有预制体。** 工程里找不到 `3DBody` / `Card 3D` 的 prefab（`find Assets -iname "*.prefab"` + grep 为空）。
原版那棵树要手工重建：`3DBody`(scale 1) → `Card 3D`(scale 0.88586, yaw 180°, MeshFilter→`Card 3D WH40k`,
MeshRenderer→`Card 3d Lvl1`) + `TraitIcons`/`TraitIconContainer`/`HealthText` 等兄弟节点。

**缺口 3：代码没接线。** 我们现在走**程序化建面**：`CardView.cs:737` `mf.sharedMesh = Quad()`；
`:760-765` 的 `CardFace.Board` 分支只 `AddLayer("art", …)` 一块平面立绘。要接得把 MeshFilter 换成
`Card 3D WH40k` + 换材质 + 在 `CardView.cs:126-132` 的两档 `CardFace` 里**加第三档**。

### 两个必须先定的坑

1. **UV0 是图集 UV**（正面只占 u 0.066…0.665 / v 0.625…0.989），正版靠 Shader Graph 内部把它重映射到
   `_CardImage` —— **那套重映射规则本地查不到**，得自己反推。
2. **尺寸对不上**：mesh 宽 2.090 与我们的卡本体 2.0927 **一致**，但**高 2.960 vs 我们 3.3313 不一致**
   （原版 `Card 3D` 另有 0.88586 缩放）⇒ 接入前要决定**按宽对齐还是按高对齐**。

### 两条别踩的

- **「28 个网格」不是卡体**：`Assets/WarpforgeArena1/Scenes/BattleArena1.unity` 里那 28 个网格
  **全是场景道具**（Barrel / Fences / Cannon / Vehicle / Platform…），**不含 `Card 3D`**；
  场景里唯一用到 `Card 3D WH40k` 的是 **`Cache Stealth`** 节点。别拿它当卡体来源。
- ⚠️ **`Cache Stealth`（3D 卡网格）用途未确认** —— `战场还原度_差距清单_0917.md` 把它列为存疑项。

## 四、出处

- 反编译：`D:/2/tools/decomp_full/` 的 `BattleCardUI__{ToggleBody3D,ShowBoardObjects,ChangeCardToMinion,SetCardImageTo3DBase,SetCardMaterial}.c` ·
  `CardMaterialHelper__{ConfigureMaterial,GetCardMatCapByCardTier}.c` · `RemnantBody__{Initialize,BodyVisibilityToggle}.c` ·
  `CardScript__{DoBodyAnimation,SetAmbush}.c` · `Card3DAnimationController__Toggle.c`
- 字段偏移：`D:/2/tools/il2cpp_out/dump.cs:26177`(body3D) · `:26187`(minion3DRenderer) · `:26239`(originalMaterial) · `:26348`(ToggleBody3D)
- 资产：`D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`（`Mesh/` `Material/` `GameObject/` `Transform/` `MeshFilter/` `MeshRenderer/` `Shader/`）
- 场景：`Assets/WarpforgeArena1/Scenes/BattleArena1.unity` · `资料/说明书/02_战场_场景/battlearena1.md:283,427`
- 我们这侧：`Assets/CardPresentation/Core/CardView.cs:119-131,737,760-765` ·
  `Assets/CardPresentation/Core/Badges.cs:47-51`（**已按实测 bbox 换算徽标位**，与本次实读一致）
- **查不到**：Shader Graph 节点图 / `_CardImage` 的 UV 重映射规则 · mesh pathID `−6960435115557275368`
  具体对应 `WH40k` 还是 `Subdivided`（导出目录没留 pathID↔文件名索引，5 个 MeshFilter 共用它）。
