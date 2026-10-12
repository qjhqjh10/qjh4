# DIAG 徽标底板图 —— 原版是哪张、我们用的是哪张

> 只读调查（2026-10-12）。⛔ 未跑 Unity、未动 git、未改任何生产文件。
> 放大图落在 scratch `d:/4/_tmp_view/diag22_badge/`（原版 sprite 重建图 + 原版实拍裁切），非生产文件。

## 一、结论

1. **原版这层 `IconBackground` 引用的底板 sprite = `Base3d Trait Background`（PathID `2729514481590049735`），本地有**，而且**我们工程现在画的就是它** —— `Resources/Art/ui/Base3d_Trait_Background.png` 与从原版 bundle 里读出来的那份 **md5 `75a79401371f` 逐字节相同**。
2. 🔴 **`DIAG_战斗HUD两处.md:24`（`A1662` 那半句）和 `Core/Badges.cs:42-43` 的「原版 sprite 在解包里找不到本体、我们画的是替身」【不成立】，按铁律 5 要就地更正。** 错因：按**对象名** `IconBackground` 去找 **Sprite**（全库没有这个名的 sprite）⇒ 判成「没有」。**名字对不上 ≠ 资源不存在。**
3. **真差异不在图，在「怎么画 / 什么颜色 / 锚在哪」**：原版是 `SpriteRenderer` **Sliced 九宫格 + 深色 `m_Color=(0.3230,0.3431,0.3585)` + sprite 的 pivot 在【左边缘】（向右/向内铺）**；我们是**整张贴图按自身比例内接成一个【居中】quad、不上色（白）** ⇒ 用户看到的「**白色的斜方块**」就是这块板：**亮约 3 倍 + 整体往外挪了半个板宽、且宽了/窄了对不上**。
4. 同族第二条也错：`Badges.cs:134` 写「`Trait Not Active` 那张贴图解包全库找不到」—— **找得到**，= **`40k_Combat_Icon_Cross`**（PathID `4627540309930320646`），本地三处都有，我们 `Resources/Art/ui/` 里那份同样与原版逐字节相同。

## 二、原版侧证据

**① 那 7 个 `IconBackground` 是什么** —— 全在 `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`，组件表一律 `Transform + SpriteRenderer + MonoBehaviour`（**没有 `Image`，是 3D 预制体里的 SpriteRenderer，不是 UI**）。
父链（逐级解 `Transform.m_Father` 实读）：**`IconBackground ← TraitIconContainer{1,2,3} / TraitIconContainer Right{1..4} ← TraitIcons ← 3DBody ← Board Elements ← CardPrefab`**（7 份 = 左 3 + 右 4，与 `boardTraitIcons[7]` 一一对应）。MonoBehaviour 是 `NestedFadeGroup.NestedFadeGroupSpriteRenderer`（字段只有 `alpha=1.0 / exclude=0`，**不改颜色**，只是淡入淡出用）。

**② 引用链**：7 份 `SpriteRenderer.m_Sprite` **逐份同值** = `m_FileID 3, m_PathID 2729514481590049735`；`m_Materials` = `Sprites-Default`（`bundle_Warpforge_unitybuiltinassets/Material/Material_-7504259843138580660.json`，`_Color = 白`）。
**PathID `2729514481590049735` → `Base3d Trait Background`，三条独立佐证**：
- ① 直接用 UnityPy 打开真 bundle `…/aa/StandaloneWindows64/atlasindividual_assets_battleatlasui.bundle` 逐个 `Sprite` 读 `m_Name` ⇒ `pathID=2729514481590049735 name='Base3d Trait Background'`（包内 60 个 sprite，同时坐实 `4627540309930320646='40k_Combat_Icon_Cross'`）；
- ② `d:/2/Warpforge_tools/data/ui_extract/battle_sprite_pathid.json`（101 条，UnityPy 生成）同值；
- ③ 旧解包 / 我们自己导入的原版树里那份文件名**直接带 PathID**：`…/battleatlasui/Sprite/Base3d Trait Background_2729514481590049735.json`。

**③ sprite 本体与尺寸**（`bundle_atlasindividual_assets_battleatlasui/Sprite/Base3d Trait Background.json` 实读）：`m_Rect/textureRect = 105×214`、`m_PixelsToUnits = 200`、**`m_Pivot = (0, 0.5)`（左中！）**、`m_Offset = (−52.5, 0)`、**`m_Border = (0, 0, 96, 0)`**、`m_AtlasTags = ["BattleAtlasUI"]`。贴图不透明板区实测 **texture x 0..62 / y 47..164**（其余是透明边 + 右下那圈**柔和投影**）。

**④ 渲染参数**（7 份 `SpriteRenderer` 逐份同值）：**`m_DrawMode = 1 (Sliced)`**、`m_Size = (0.85, 0.874)`、**`m_Color = (0.32298, 0.34305, 0.35849, 1.0)`（深灰）**、`m_SortingOrder = 0`；`Transform.localScale = (±0.8656, 1.0692, 1.0)`（**右列 x 为负 = 镜像**）。

**⑤ 位置**（`Transform` 逐份实读，容器 `localScale = 0.7502`）：

| | TraitIcons | 容器 x | 容器 y（4 行 = .826/.401/−.039/−.479） | IconBackground pos | 图标 `Container` pos |
|---|---|---|---|---|---|
| 左 1/2/3 | (0.296, **1.533**, −0.014) | **−0.8590** | 前 3 行 | (−0.3000, −0.0200, 0) | (−0.2870, 0, 0) |
| 右 1..4 | 同上 | **+0.2670** | 全 4 行 | (+0.3040, −0.0200, 0)，`scale.x = **−0.8656**` | (+0.2880, 0, 0) |

⇒ **底板与图标基本同心**（差 0.013/0.02 容器单位），**两列都往【卡内】铺**（左列靠 pivot 在左边缘向右铺，右列靠 `scale.x = −0.8656` 镜像）；**板子整块在卡框内**：图标左边 = −1.006、板子可见左边 = −0.788，卡半宽 = 1.0463。**带计数器**：容器下是**两支并排**子树 `Without counter` / `With counter`（`TraitIcon` + `Trait Not Active` 各一份），另有 6 个 `TraitCounter` TMP（`m_fontSize=5.5 / Bold / (1, 0.9729, 0.9104)`，我们已照抄）；`Trait Not Active` 的图 = `40k_Combat_Icon_Cross`、`localScale 0.49`（我们**没画**）。

## 三、本地 PNG 检索（目录 / 词 / 命中）

| 搜过的目录 | 搜过的词 | 结果 |
|---|---|---|
| `d:/2/新解包资源/assets_full/`（24.7 万文件） | `Base3d` `Trait` `Background`（`*/Sprite/` `*/Texture2D/` 文件名）+ PathID | ✅ `bundle_atlasindividual_assets_battleatlasui/Sprite/Base3d Trait Background.json`；图集 `Texture2D/sactx-0-2048x2048-BC7-BattleAtlasUI-06d3a381.png` |
| 同上（真 bundle，UnityPy 直读） | PathID `2729514481590049735` | ✅ 读出 105×214 RGBA，另存 `_tmp_view/diag22_badge/Base3dTraitBackground_full.png` |
| `d:/2/Warpforge_tools/data/ui_extract/`（5557 张） | `*Trait*` `*Base3d*` | ✅ `scenes_scenes_battlearena1_sprites/Sprite/Base3d Trait Background.png` |
| `d:/4/Unity/素材/Warpforge原版/` | `Base3d` `Trait` | ✅ `UI图集/图集/battleatlasui/sliced/Base3d_Trait_Background.png` + `…/Sprite/Base3d Trait Background_2729514481590049735.json` |
| `d:/2/解包整理/`（旧解包，只作旁证） | 同上 | ✅ `03_界面UI/图集/battleatlasui/Sprite/Base3d Trait Background_2729514481590049735.json` |
| **我们工程** | `Base3d_Trait_Background` | ✅ `MyGame/Assets/CardPresentation/Resources/Art/ui/Base3d_Trait_Background.png`（**与原版 md5 同**）、`…/Art/原版/battleatlasui/Base3d_Trait_Background.png` |

## 四、我们侧（图从哪来 + 与原版的形状/颜色差）

- 取图口：`Core/CardView.cs:1526` `CardArt.Ui("Base3d_Trait_Background")`（`Core/CardArt.cs:424` → `Resources.Load("Art/ui/…")`）⇒ **就是原版那张**，**不是替身**。
- 画法：`CardView.cs:1526-1527` `AddLayer("badgePlate"+i, …, FitQuad("badgePlate"+i, plateAt, Badges.PlateW=0.552, Badges.PlateH=0.701, plate))`；`FitQuad`（`:1696`）**按贴图自身比例内接**、`SpriteQuad`（`:4599`）**UV 拉满 0..1 全贴图、quad 以 `at01` 为【中心】**；材质 = `Sprites/Default`（`BaseMaterial()`），**不上色**（`_tint` 默认白，`ApplyTint` 只把整卡色刷上去）。
- **三处硬差异（都能量）**：
  1. **颜色**：原版 `m_Color=(0.3230,0.3431,0.3585)` ⇒ 奶油色被乘 **≈0.33（约暗 3 倍）**；我们是原色（≈`215,191,159`）⇒ **这就是「白色的方块」的来源**。
  2. **锚点**：原版 sprite **pivot 在左边缘**，板子从 `plateAt.x` **往卡内**铺；我们把 quad **居中**在 `plateAt.x` ⇒ 可见板**整体往外挪 0.172 卡单位**（左列：我们可见板左边 `−0.9601`，原版 `−0.7881`；卡左边 `−1.0463`）。
  3. **形状/尺寸**：原版 Sliced（右边界 96/105）⇒ 可见板 **0.3312 宽**（左列 x ∈ [−0.7881, −0.4569]）× 0.383 高；我们内接后可见板 **0.2064 宽**（x ∈ [−0.9601, −0.7537]）—— **只有原版的 0.62 倍宽**，且把原图**透明边 + 右下投影**一起画了进去。
- 说明：`Badges.SlotAt`/`IconOutward`/`PlateOutward`（`Core/Badges.cs:103-120,169-171`）**与原版逐值吻合**（本次现核：容器 ±0.859/±0.267、scale 0.7502、`IconBackground ±0.300/−0.020` 全对）—— **几何常量没错，错在「quad 居中 + 整图内接 + 不上色」这一步**。

## 五、修法建议（逐条给落点）

1. **`Core/CardView.cs:1526-1527`（主修）**：把 `FitQuad` 换成 **Sliced 九宫格 mesh** —— 按 sprite 的 `m_Border=(0,0,96,0)` 切成两片：左片 UV `x∈[0,9/105]` 拉伸到 **0.0473 宽**、右片 UV `x∈[9/105,1]` 保持 **0.5047 宽**，总 **0.552 × 0.701**（= `PlateW/PlateH`），**左边缘对齐 `plateAt.x`**（左列向右、右列镜像 —— 原版右列镜像靠 `IconBackground.localScale.x = −0.8656`）。
2. **同上，材质上色**：给底板那份材质设 `_Color = (0.3230, 0.3431, 0.3585)`（原版 `SpriteRenderer.m_Color`）；注意别被 `ApplyTint()`（`CardView.cs:3356` 那个 foreach）刷回白 —— 参照 `_textBg` 的做法（`:3376`）在循环之后**单独刷回去**。
3. **`Core/Badges.cs:42-43` 就地更正**（铁律 5）：删掉「找不到本体 / 按自身比例 fit 进格子」，改成「sprite = `Base3d Trait Background`（PathID 2729514481590049735），本地有，我们用的就是它；原版是 Sliced + `m_Color` 深色 + pivot 在左边缘」。
4. **`Core/Badges.cs:134` 就地更正**：`Trait Not Active` 的图 = `40k_Combat_Icon_Cross`（PathID 4627540309930320646），本地有 —— 要不要画它，**另立一条账**（现在是「不画」，理由不再是「找不到」）。
5. **给 `A1662` 换判据**：`DIAG_战斗HUD两处.md:24` 与 `:73` 那两行按本文更正（该文件是别的会话的正本，我这次只读，**留给调度台合并**）。
6. **补一条断言**（本仓血规：断言盯原版参数）：断**画出来的可见板**「左边 = `plateAt.x`（容差 0.01）、宽 ≈ 0.331 卡单位、色 ≈ (0.3230,0.3431,0.3585)」，⛔ 别只断「层建起来了」。落点 `Editor/BattleScene.cs` 现有徽标断言族。

## 六、没查清的部分（不猜）

1. **用户实拍里「只有我方战将有、敌方那一张干净」**：我方这次只核到「底板只在**这一位**有可画的图标时才建」（`CardView.cs:1394-1405`：底板或图标缺一就不建）⇒ 敌将那张**很可能只是没有带图标的 keyword**；但**没在真机上核过**（本轮不跑 Unity）⇒「敌方是不是也画了、只是颜色/位置看着不同」**还没查清**。
2. **原版运行时到底把底板刷成什么颜色**：预制体给的是深灰 `(0.3230,0.3431,0.3585)`，而原版实拍（`资料/原版参照图/用户实拍_1017/战斗截图参考.png` 我方战将左上角）那枚是**深绿**圆盘 + 白徽 —— **那枚绿多半是 trait 图标自己带的彩色圆盘**（我们的 `Art/traits/codex.png` 就是「蓝盘 + 白徽」那一类），但**底板运行时是否还被覆写成阵营色，没有找到赋值点** ⇒ 未查清。
3. **底板的可见形状在真机上到底偏成什么样**：我只能从 `SpriteRenderer`/`Sprite` 字段算出**卡单位**的矩形，**没有真机并排比**（用户在 12× 放大图里看到的白板是透视投影后的样子）。⇒ 修完请按铁律 10 第 6 条**并排渲一次**再定。
