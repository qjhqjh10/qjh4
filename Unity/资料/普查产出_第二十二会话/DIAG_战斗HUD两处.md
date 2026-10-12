# DIAG 战斗 HUD 两处多余件

> 只读诊断（2026-10-12 · 对应 `资料/任务_卡组卡牌战斗.md` 的 `A1661` / `A1662`）。⛔ 未跑 Unity、未动 git、未改任何生产文件。
> 为看细节我裁了几张放大图到 `d:/4/_tmp_view/diag22/`（scratch，非生产文件）。

## 一、战将卡左上角那颗图标：是什么 / 谁建的 / 原版有没有（判定：**本就有**）

**是什么** = 场上卡的 **trait 徽标第 0 位**（原版 `BattleCardUI.boardTraitIcons[7]` 的替身）。三个层同一位：
`badgePlate0`（底板）+ `badgeIcon0`（图标）+ 有角标时才用的 `badgeCounter0`。第 0 位 = **左列第一格 = 卡的左上角**。

**谁建的（文件:行号）**
- `MyGame/Assets/CardPresentation/Core/CardView.cs:1286 SetBadges(List<Badge>)` → `:1394 BuildBadgeSlot(i)`（底板图 `Art/ui/Base3d_Trait_Background`，图标图 `Art/traits/<关键词>.png`；层挂在卡面根下，`BadgePlateZ=-0.095 / BadgeIconZ=-0.10`）。
- 数据来自 `Battle/BattleDriver.cs:11710`（`ToCardData` 里 `Badges.For(u.Keywords, fb.body, u.Card.NumericKeywords, KeywordActive)`）。
- 几何 = `Core/Badges.cs:108-120 SlotAt`（左列 x=−0.563 · 右列 +0.563，y 逐行 0.826/0.401/−0.039/−0.479），加 `IconOutward / PlateOutward`（`Badges.cs:169-171`）。**这些数字都逐值来自原版 `TraitIconContainer*` 的 JSON**，不是我们编的。

**原版那个位置有没有东西：有（三条证据）**
1. 代码侧：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleCardUI.cs:226` 的 `boardTraitIcons[7]`；筛选器 = `d:/2/tools/decomp_full/CardTraitCollection__GetTraitInPlayListForIcons.c`（202 行全的），调用点 `BattleCardUI__UpdateTraitIcons.c:59`。
2. **原版实拍**：`d:/4/Unity/资料/原版参照图/用户实拍_1017/战斗截图参考.png` —— 两张**场上战将卡**的**左上角各有一枚徽标**（我方 = 绿底白徽、敌方 = 暗红底金徽）。放大图 `_tmp_view/diag22/ref_my_card.png` / `ref_foe_card.png`。**位置、以及「这个位置本来就有东西」与原版一致。**
3. 我们的场卡也照这一套画（`_tmp_view/battle/09_原版卡_开局.png` 放大 `diag22/badge12x.png`：卡的左上角一枚「底板 + 图标」的徽标）。

**判定 =「本就有」，⛔ 不是「我们多加的件」**。若用户仍要求它不出现，那是**口径变更**（原版有），要用户拍板 —— 不能拿「原版没有」当理由删。
⚠️ **但有两处与原版【已知】不同（账已存在，`A1362`）**：
- ⓐ **画哪些词条**：原版要 `HasIcon && IsInPlayIcon && TraitIcon != null`（+ `CardTraitData._hide == false`）；我们只按「查得到图标就显示」⇒ **有可能画出原版不会画的那一枚**。两个开关的**逐 trait 取值本地取不到**（`CardTrait` 资产全库 0 实例，实据见 `资料/普查产出_第二十会话/C2_卡面渲染与图标.md:100`）。
- ⓑ **底板那张图是替身**：原版的 `IconBackground` sprite（PathID `2729514481590049735`）本地没有，我们画的是 `Base3d_Trait_Background.png`（奶油色团）。

## 二、绿色进度条：是什么 / 谁建的 / 何时显示 / 原版对应物（判定：**原版有这一层，不该删；要查的是形态与位置**）

**是什么** = **「未行动」绿光那一层**（原版 `3DBody/CanActParticles` + 子节点 `RotatingRing`）：
- `Core/CardView.cs:2943 BuildCanAct()`：
  · `CanActParticles` = Unity 内置 `Quad` 网格（`m_Mesh` pathID 10210）+ 贴图 `Circle_Hoop` + 加性混合；
  · 子节点 `RotatingRing` = 原版网格 `FxObject_cylinder_short`（**压扁的圆柱**，scale z=0.05929）+ 贴图 `Spark UI`，材质名 **`Sparks UI Additive Scroll`** ——「Scroll」= **UV 一直在滚** ⇒ 这正是用户说的「**好像在加载什么东西**」。

**颜色 = 决定性证据**：全工程 `float (0.45084,0.94510,0.25098)` 只在 `Core/CardView.cs:3041` 出现一次（`RotatingRing.startColor`）= **RGB(115,241,64)**；2026-10-10 玩家实拍 `_tmp_view/player/V1_Battle.png` 里那条绿条实测 **RGB(112,240,64)**（y 714–716 · x 1214–1261）。同族另一件 `CanActParticles.startColor`（`:2969`）= RGB(77,241,23)，色相差得开。

**什么时候显示 / 值由谁驱动**
- 开关：`CardView.SetCanAct(bool)`（`Core/CardView.cs:2848`），**唯一调用点** = `Battle/BattleDriver.cs:11262`（`SyncBoard` 里 `v.SetCanAct(RuleCore.CanActNow(...))`）⇒ 判据 = **「这个单位这一回合还能行动吗」**，不是进度、不是加载。对局里大部分时间都有单位能行动 ⇒ 用户看着像「常驻」。
- 形态由原版 ParticleSystem 字段驱动：`maxParticles=2` · `startSize=1.9` · `loop=false` 但 **`ringBufferMode=Loop(0.1~0.9)`** —— 那条注释自己写着「**这才是「常亮」的真因**」（`CardView.cs:2973-2976`）。

**原版有没有：有**
- 我们这一层就是**照原版 prefab 逐字段复刻**的（`bundle_battleprefabs_vfxandmisc_assets_all/ParticleSystem/ParticleSystem_-467847035904746560.json` 与 `…_-2548938496250242112.json`），自检 12 条逐值核对全绿（断言文字见 `_tmp_view/s21_final_BattleScene.Run.log:40826-41006`）。
- **原版实拍里它就在**：`战斗截图参考.png` 我方战将**卡的底座下缘那条贴地的亮绿弧线**（放大 `diag22/ref_base.png` 亲读）—— 就是同一件东西。⇒ **「有一条绿条」本身不是多余的件**。

**⚠️ 但位置对不上（这才是真正要改的）**
- 原版那条**贴着卡的底部**；2026-10-10 玩家实拍里我们那条落在**卡的正中**：实测卡 y 500→950（`diag22/v1card.png` 带标尺），绿条 y≈730 = **正卡心**；而按代码它应在**卡根 y=−1.63548**（`From3DBody(0,0.030169,0.006053)` = 卡底），自检实读也是 `(0.00000,−1.63548,0.00605)`。
- ⇒ 当时那个构建里它不是画在卡底，**或者它根本不是 CanAct 那两件**（我没能在最新构建里复现，见五）。

**排除掉的误读（省下一位的时间）**
- ⛔ 不是「加载条」：壳的 `Loading text` / `Progress text`（`Shell/ShellRuntime.cs:247-248`）是**两段文字带，没有进度条**。
- ⛔ 不是资源/倒计时条：HUD 里没有这类常驻条（`TurnClock` 是文字）。
- ⛔ 不是「设置窗那三根绿滑条」：`_tmp_view/battle/18_设置面板.png` 里那三条**薄荷绿长条 + 右端圆钮** = `Battle/WfSlider.cs` 的音量滑块（3 根，开着设置面板才有）—— 用户描述的是「常驻、一条」，且要开面板才见，排除。

## 三、证据

| 项 | 出处 |
|---|---|
| 徽标建的代码 | `MyGame/Assets/CardPresentation/Core/CardView.cs:1286` / `:1394` / `:1403-1431` |
| 徽标喂数（关键词） | `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:11710` |
| 徽标几何（原版 JSON 逐值） | `MyGame/Assets/CardPresentation/Core/Badges.cs:103-120, 169-171` |
| **原版同位置有徽标（实拍）** | `资料/原版参照图/用户实拍_1017/战斗截图参考.png`（裁切 `_tmp_view/diag22/ref_my_card.png` / `ref_foe_card.png`） |
| 原版徽标组件/筛选器 | `BattleCardUI.cs:226` · `decomp_full/CardTraitCollection__GetTraitInPlayListForIcons.c` · `BattleCardUI__UpdateTraitIcons.c:59` |
| 绿光建的代码 | `MyGame/Assets/CardPresentation/Core/CardView.cs:2943`（父层 `:3023` RotatingRing） |
| 绿光开关与唯一调用点 | `Core/CardView.cs:2848` ← `Battle/BattleDriver.cs:11262`（`RuleCore.CanActNow`） |
| **绿光颜色唯一出处** | `Core/CardView.cs:3041` = (0.45084,0.94510,0.25098) |
| 玩家实拍里的绿条 | `_tmp_view/player/V1_Battle.png`（2560×1440）y 714–716 · x 1214–1261 · RGB(112,240,64)；同图放大 `_tmp_view/player/crops/after_board.png` |
| **原版同效果（实拍）** | `资料/原版参照图/用户实拍_1017/战斗截图参考.png` 卡底绿弧（裁切 `_tmp_view/diag22/ref_base.png`） |
| 现有自检读数 | `_tmp_view/s21_final_BattleScene.Run.log:40826-41006`（12 条 `✓`，含位置/旋转/缩放/材质/吐粒子） |

## 四、改动清单（逐项 · 先查后改，⛔ 别先动代码）

| # | 文件:行号 | 改什么 |
|---|---|---|
| 1 | `Battle/BattleDriver.cs:11710` 或 `Core/Badges.cs:317 For(...)` | **先判**：要不要按原版 `HasIcon && IsInPlayIcon` 收窄（`A1362`）。**今天判不了逐 trait 取值**（资产本地没有）⇒ 要去查「能不能从别处补齐这份开关表」；补不到就**保持现状 + 在卡面/README 写清这是替身** |
| 2 | `Core/CardView.cs:1396`（`CardArt.Ui("Base3d_Trait_Background")`） | 底板图是替身。**要做**：找/解出原版那枚 `IconBackground` sprite（PathID 2729514481590049735）；找不到则**如实标注**，⛔ 别换成别的图冒充 |
| 3 | 徽标第 0 位具体是哪一枚图 | **先量**：真机/自检读 `CardView.BadgeTexture(0).name`。我的外观比对给出 `talent`，但 `UM_Roboute_Guilliman` 的 `keywords` 只有 `["Codex"]` ⇒ **矛盾未解**（我的比对法在已知样本上会认错，不可当结论） |
| 4 | `Core/CardView.cs:2943 BuildCanAct()` 的落点 | **先量**：真机进战斗，量那条绿光的**世界坐标/在卡上的高度**，与原版实拍（卡底）比。2026-10-10 实拍显示它当时落在**卡心**（应贴卡底）⇒ 若今天仍如此，改落点；若已贴卡底，**只差与原版并排比一次** |
| 5 | `Core/CardView.cs:3023-3064 RotatingRing` | 它那圈 UV 滚动（`Sparks UI Additive Scroll`）是「像加载条」的直接来源。**照原版核一遍几何**（尤其 scale z=0.05929 与 radius），确认画出来的是**贴地的一圈弧**而不是**一条直条** |
| 6 | `Editor/BattleScene.cs:10163` 那一族断言 | 现在只断「建出来没 / 挂对没 / 亮灭对不对」，**没有一条断「画在哪」**⇒ 补一条「绿光中心 = 卡根 y≈−1.6355 ± 容差」的断言（本仓血规：断言要盯原版参数，别盯我们的常量） |

## 五、没查清的部分（⛔ 不猜）

1. **那条绿条的最终归属没坐实**。颜色只能指到「CanAct 绿光」这一族（`RotatingRing` 最像）；但**落点对不上**（实拍照在卡心、代码说在卡底）。我试着在最新构建里复现：`_tmp_view/play17/*.png`（2026-10-11 player）**全图零处**匹配那个绿（含松阈值扫描）—— 可能那几帧绿光是灭的，也可能它只在 10-10 那版构建里出现。⇒ **需要真机**：`-wfscene Battle` 进战斗、让某个单位保持可行动、截图 + `工具/wf_click.py`，再看 `_tmp_view/click_log.txt`/对象树。
2. **徽标第 0 位画的是哪一枚图**（见四·#3）。做法：跑一次 `BattleScene.Run` 并在自检里加一句打印 `probe.BadgeTexture(i).name`（现在是断言里只比了 `armour` 那一处），或真机 dump。
3. **徽标的落点/尺寸是否与原版差一截**：09 那张图放大后看，我们的底板有一角**压出卡框左边之外**，而原版实拍里徽标是**在卡框内**的。我量不准（卡在 3D 透视里、我的像素估边误差大）⇒ **没查清**，要与原版并排比一次才能定。
4. `A1362` 缺的那份「逐 trait 的 `_hasIcon`/`_isInPlayIcon`」今天仍取不到（`CardTrait` 资产全库 0 实例）⇒ 上面四·#1 被它卡住，属「判据不可及」，不是「没找」。
