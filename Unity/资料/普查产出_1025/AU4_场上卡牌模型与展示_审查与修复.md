# AU4 · 场上卡牌模型与展示 —— 审查与修复

> 日期：2026-10-25 · 角色：**审查+修复**代理 `AU4` · 用户口径：**真机验收先暂停，先解决代码层面与原版的差异**
> 范围（一片）：场上/手牌**卡牌模型本身** —— 3D 卡体尺寸与倾摆 · 投影（BlobShadow）· 旋转与朝向 ·
> 扇形排布与重叠 · 放大/详情类**展示窗**（中央展示卡、多卡展示、卡牌详情窗的战斗那扇、飞行卡）。
> 白名单：`Battle/{CardDisplayWindow,FlyingCardController,MultiCardDisplay,CombatAutoZoom,CombatCameraZoom,BattleBackdrop}.cs`
> · `Core/{CardPreview,CardFan,CardEffects,AutoCardRotation,BlobShadow}.cs`（`CardFeel.cs` 只读；`CardView.cs` / `BattleDriver.cs` / `VfxMap.cs` 只读）。
> 行尾：两个被改文件 `CardDisplayWindow.cs` **CRLF 未翻**（1797/1797）· `MultiCardDisplay.cs` **LF 未翻**（0/1265），
> `git diff --numstat` = **62/2** 与 **91/17**（不是整篇重写）。
> 类型检查：`TMPDIR=/tmp/wf_au4 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 一、差异总表（逐项：原版值 + 出处 → 我们 → 差异）

### 1·1 卡体 / 倾摆 / 投影

| # | 项 | 原版（出处） | 我们（file:line） | 差异 |
|---|---|---|---|---|
| 1 | `AutoCardRotation` 的**两级合成** | 外层 `CardUI Reference` 摆扇形角 → 内层 `2DCard` 挂 `AutoCardRotation`（写**自己的** `localRotation`）。出处 = `资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` A0 全树 + `AutoCardRotation__Update.c` | 卡根 = 外层（`SetPose` 写 `Quaternion.Euler(0,0,rotZ)`，`Core/CardView.cs:8371`）· `_tiltRoot` = `2DCard`（`CardView.cs:3946-3951`，组件在它身上） | **无差异**。两级**相乘**是 3D 正确语义；倾摆写**绝对** `localRotation`，所以两层**不能合**（合了会把扇形角 Lerp 回 0）—— 注释已在两侧写明 |
| 2 | 倾摆的**轴 / 顺序 / 数值** | `Update` 里 `GetRotation(dx,maxX,Vector3.down) * GetRotation(dy,maxY,Vector3.right)`；`GetRotation` = `AngleAxis(Clamp(t*max,±max),axis) * identity`。`.cctor` = `EVAL 2f`；`.ctor` = `2 / 15 / 15`；**prefab 值 153/153 = `10 / 10 / 10`** | `Core/AutoCardRotation.cs:144-150`（先 x 后 y、轴 down/right）· `:50` Eval=2 · `:54,57,60` 10/10/10 | **无差异**（逐句复核 `AutoCardRotation__{Update,GetRotation,OnEnable,cctor,ctor}.c`） |
| 3 | 倾摆的**闸门** | `if (cacheTransform.hasChanged) timer = EVAL; else if (timer <= 0) return;` → `Step` → `hasChanged = false` | `Core/AutoCardRotation.cs:104-110` | **无差异** |
| 4 | 倾摆**什么时候真会动** | 只在 `2DCard` **自己的** local TRS 被写时（父件动不置子件的 `hasChanged`）。原版拖拽写的是**卡根**（`CardScript__StartMovingFromHand.c` 的 `DOScale(Component.transform(cardScript))` + `BattleManager.StartMovingCardInHand`） | 我们拖拽也写卡根（`Hand/CardInteraction.cs:620-626`）⇒ 手牌拖拽时倾摆同样**不触发** | **无差异**（两侧同构）。⚠️ 结论：`±10°` 倾摆在手牌/展示窗里**基本是静默的**，这是**原版行为**，⛔ 别为「看得见倾斜」去改闸门 |
| 5 | 场上卡倾摆 | 场上走 3D 体、`2DCard` 整个 `SetActive(false)`（`BattleCardUI__ShowBoardObjects`）⇒ 倾摆不跑 | `CardView.SetFace`：`_tilt.enabled = !board` + **显式复位** `_tiltRoot.localRotation = identity`（`CardView.cs:2145-2146`，`新账-67` 那条） | **无差异**（「停用 ≠ 复位」那一课已修：否则回到手牌时会带着旧的 ±10° 与其包围盒） |
| 6 | 软阴影 `BlobShadow` 的**参数** | `MonoBehaviour_2235663227690458048.json` 现读：`floorY 0` · `maxYForAlpha0 0.15` · `yOffsetForSnap 0.01` · `scaleMultiplierByHeight 1.1` · `m_Color (0,0,0,0.36078429)` · `localScale 2.0567584` · `localRotation (0,0.70710784,−0.70710576,0)` | `Core/BlobShadow.cs:51-54`（四个数）· `:103-109`（rotation/scale/color） | **无差异**（逐值核 JSON；`.ctor` 的 1.0/0.01/1.2 是**出厂默认**、被 prefab 覆盖 —— 取 prefab 值是对的） |
| 7 | 软阴影**行为** | `Update`：`t = clamp01((target.y − yCenterOffset − floorY)/maxY)` → 颜色 Lerp 到 alpha 0、`localScale` Lerp×1.1、**世界位置** `(target.x, floorY+yOffset, target.z)`、`enabled = a != 0` | `Core/BlobShadow.cs:140-165`（`Sync`）· `:191-203`（`Tick` 早退闸） | **无差异**（含 `Start` 里 `targetYCenterOffset = target.y − self.y`；我们同点 ⇒ 0，与文件头一致）。⚠️ 唯一偏离：`hasChanged` 换成「记住上一帧 `target.position`」—— **判据充分**（原版 target 就是被移动的那个节点；我们 target 是卡体子件、父件动不置它的 flag，照抄会冻住影子），已在注释里写清 |
| 8 | 影子的**位置量纲** | 原版 `3DBody` 的 y=0 **就是卡底边**，影子贴 y≈0.01 | 我们 `RootPosition` = 地面点 + `Height/2 × scale`（`Board/ArenaSlots.cs` 的 `RootPosition`）⇒ `body3D` 世界 y = **0** ⇒ `t = 0` ⇒ alpha **0.361**（不是 0、不隐身） | **无差异**（本条特意复算过：若卡根 y 写成 0，影子会恒 `t=5.9` ⇒ **全程隐身**，是个会静默失效的坑） |
| 9 | 卡底影子的**数量/挂载** | `CardPrefab/Board Elements/3DBody/Card 3D/<软阴影>` —— 只有场上 3D 体 | 只在 `CardView.BuildBody3D` 建（`CardView.cs:6648`）⇒ 展示窗/手牌卡**没有**影子 | **无差异** |

### 1·2 展示窗（扇形的真值全部**逐值复算**过）

| # | 项 | 原版（出处） | 我们 | 差异 |
|---|---|---|---|---|
| 10 | 扇形**终态**（1 主卡 + 4 相关卡） | `AnimationClip_1560242982351263260.json`（`Card Display Open`，legacy·60fps·`t∈[0,0.3166667]`·19 帧·events 空·`m_WrapMode 1`）末键现读：x `0/−121/−232/−332/−432` · y `60/53/49/45/27` · z 四元数 `0 → 2·atan2` = `0/2.5104/5.8904/12.7491/19.1991°` · scale `250/232.9537/221.5926/204.5463/181.8148` | `Core/CardFan.cs:37-42`（x/y/rot/scl）· `:45` Slots=9（槽 5–8 **外推**，用户 2026-09-26 授权） | **槽 0–4 逐值无差异**（本次把 5 条 rotation curve 的末键四元数当场换成度数复算，含槽 3 = **12.749°**，文件里记的 12.750 是四舍五入）· 槽 5–8 是我们外推的（已标） |
| 11 | 开窗动画**起点 / 缓动** | clip 的 `m_ScaleCurves` / `m_FloatCurves` 首键现读：5 槽一律 `scale 210` · `anchoredPosition (0,60)`；**两端 inSlope/outSlope 全 0** ⇒ 赫米特退化成**平滑步 `t²(3−2t)`**；`m_WrapMode 1(Clamp)` | `CardFan.cs:77-97`（`OpenTime/OpenStartScale/OpenStartAnchor/OpenEase`）· 消费点 `CardDisplayWindow.cs:1502-1532`（`Ease.Linear` + 自己算平滑步，⛔ 不是 InOutQuad） | **无差异**（本次逐键复核首末键与切线） |
| 12 | 换位：**闸门 / 时长 / 做哪些补间** | `ChangeCardPosition.c`：①`swappingCards` ②点的是前台自己 ⇒ return ③`displayAnimation.isPlaying` ⇒ return；两条 `DOLocalMove + DOLocalRotateQuaternion + DOScale`（时长 `relatedCardSwapTime`）+ **`SetSiblingIndex` 互换** + 两条换色 tween（去前台=`(1,1,1,1)`、让位=`cardInBackGroundColorTint (0.65,0.65,0.65,1)`） | `CardDisplayWindow.cs:1521-1592`；`relatedCardSwapTime = 0.25` 现读 MB_4750 ✓（`CardFan.cs:49`）· 层级用 `CardFan.ApplyQueues`（队列替 `SetSiblingIndex`）· `TweenTint` 0.25s | **无差异**（含 `ArtOverdraw` 两轴 —— 原版 `SetImageOverDraw(cardInFront,0)` + `(被点那张, 它的 useOverDraw)`，`CardOverdrawHandle` 的 `useOverDraw && index==0` 语义 = 我们的两轴相与） |
| 13 | 换位**收尾** | `CardSwapFinished.c`：`swappingCards=0` · `cardInFront = 新前台` · `SetCardLore` · `SetCardVoiceOver` · 再按 tutorialObj 开关 | `CardDisplayWindow.cs:1575-1586` | **无差异**（tutorialObj 我们没有教学模式，如实留白） |
| 14 | 5 个槽的**缩放/命中矩形** | `m_LocalScale 250`（`RT_2874/2772/2908/3170/2663`）⇒ 卡体 `523.175×832.825`；`2DCard/UI Collider` = `1.8927×2.8913 @ y −0.02`（`RT_2801`/`RT_1220`） | `CardFan.cs:112-116`（FrontW/H）· `:152-165`（`HitRatioX 0.90443` / `HitRatioY 0.86792` / `HitCyDropPx 5`）· 命中转发 `CardView.ContainsUiCollider` | **无差异**（两轴比例**不同**这条已收口，⛔ 别退回一个比例双轴同用） |
| 15 | 扇形**分层** | 原版靠 `SetSiblingIndex` 两两互换（目的：前台画在最上） | `CardFan.cs:190-201`：`QueueFor = baseQ + max(0,count−1−i)`，`QCardBase = 3009`，9 槽 ⇒ 3009–3017（`QEffect 3020` 之上留空） | **无差异**（换位后必须再 `ApplyQueues` —— 已做） |
| 16 | **开窗 / 关窗音** | 本窗 MB_4750（战斗）与 MB_2033（菜单）逐字段现读：`openSound = PPtr(fileID 12, 4006604330728631164)` · `closeSound = PPtr(fileID 12, 3524674544779823761)` · `useDefaultCloseSoundIfNull = 1`（只因 closeSound 非空才轮不到默认音）· 消费点 `GameWindow__TryOpen.c`（`CurrentState==0` 才 `Play2D(cue, FX)`）/ `GameWindow__Close.c`（`!activeSelf` 直接 return） | **本次补上**（见 §二） | ❗**原来是缺的**（两个窗级声音一条都没播） |
| 17 | **换位音** `soundOnCardSwap` | MB_4750 的 `soundOnCardSwap = PPtr(fileID 12, 4812904303840730115)`；消费点 `ChangeCardPosition.c` 在两条补间**之前** `Play2D(window+0xd8, FX)` | 未接（`CardDisplayWindow.cs` 里已落注释） | ❗**缺口**，且**卡在数据侧**：见 §三·1 |
| 18 | `tryShowResources` / `ToFocus` 的资源条浮层 | `ToFocus`（抬 `ResourcesBarController` 的 sorting）= `if (options != null)` 才跑；本窗 `options = {m_PathID:0}` 13/13 空；`tryShowResources = 0`（战斗）⇒ **战斗侧两条都不发生** | 我们本来就没做 | **无差异**（不用补 —— 这条查清后**销账**） |
| 19 | 多卡展示窗**卡片尺寸/间距** | `CardUI Reference` `m_LocalScale 223.14` × `Viewport.m_LocalScale 0.7` ⇒ 卡高 `3.3313×156.198 = 520.3`；`HorizontalOrVerticalLayoutGroup.m_Spacing 500 × 0.7 = 350` | `MultiCardDisplay.cs:280`（`CardHeightPx 520.3`）· `:299`（`SpacingPx 350`） | **无差异** |
| 20 | 多卡展示窗**卡心竖位** | `Viewport`（`RT_3176`）= `anchor(0,0)-(1,1)` + `sizeDelta(0,0)` ⇒ rect **= 整条带** y[131,949]（`m_LocalScale 0.7` 只缩大小、不动中心）· `Content`（`RT_3174`）= stretch + `sizeDelta.y 0` ⇒ 高 = 视口高、**中心同 540** · 布局组 `MonoBehaviour_4822`：`m_ChildAlignment 4 (MiddleCenter)` + `ChildControlHeight 0` ⇒ 卡**垂直居中** | 旧：`(131 + 63.204 + 949)/2 = **571.60**`（把标题让出去了） | ❗**差 31.6 px**（卡高 520.3 ⇒ 6% 卡高）。**本次修**（见 §二·1）· 自洽旁证：若 `Viewport` 真有遮罩，真实竖向范围 = `818.04×0.7 = 572.63` ⇒ y[253.7,826.3]，卡在 540 时上下各留 26.3（对称）、在 571.6 时**下沿被裁 5.5 px** |
| 21 | 多卡展示窗**初始横向位置** | `Content` 的 `m_AnchorMin/Max = (0,0)/(1,1)` + `m_Pivot (0.5,0.5)` + **`m_AnchoredPosition = (−0.0009766, 0)`**（`RT_3174` 现读）⇒ 宽被 `ContentSizeFitter(HorizontalFit=PreferredSize)` 撑开后**仍居中** ⇒ UGUI `horizontalNormalizedPosition = (W−V)/2 ÷ (W−V) = **0.5**`（= 看正中间那几张） | 旧：`_scrollPx = 0`（= 最左） | ❗**装不下时看的是头几张**（原版是中间）。**本次修**（见 §二·2）· ⚠️ 会翻一条既有断言，见 §三·2 |
| 22 | 多卡展示窗**滚动参数** | `MB_4823`：`m_Horizontal 1` · `m_Vertical 0` · `m_MovementType 1(Clamped)` · `m_Elasticity 0.1` · `m_Inertia 1` · `m_DecelerationRate 0.135` · `m_ScrollSensitivity 1.0` · 无滚动条 | `MultiCardDisplay.cs:311`（`0.135`）+ `TickScroll`（惯性式 `Pow(rate, dt*60)`） | **无差异**；如实标的偏离：① 越界回弹（`Elasticity 0.1`）没做、我们硬夹 ② 拖拽阈值 6 px 是我们加的 |
| 23 | 多卡展示窗**拖拽量纲** | —（原版是 uGUI `ScrollRect`，拖拽天然在**画布 px** 里） | 旧：`world.x * EndPanel.PxPerUnit`（= 写死 108/世界单位） | ❗**非 16:9 下滚速不同步**（4:3 ⇒ 0.75×，21:9 ⇒ 1.3125×）。**本次修**（见 §二·3） |
| 24 | 多卡展示窗**关窗音** | `MB_4182`：`openSound = 空`（⇒ **开窗无声，⛔ 别加**）· `closeSound = 空` · **`useDefaultCloseSoundIfNull = 1`** ⇒ 走 `SoundManager.defaultCloseSound` = cue `ButtonClick Back`（`WindowSounds.DefaultCloseCue`） | **本次补上** | ❗**原来是缺的** |
| 25 | 多卡展示窗 `closeOnESC / enableCardTooltips` | `MB_4182` 两者都 = **1** | 未做（都要动 `BattleDriver`） | ❗**缺口**（见 §三·3/·4） |
| 26 | 飞行卡（`FlyingCardController`）**参数与曲线** | `MonoBehaviour_2087276408211151808.json` 现读：`initialHeightOffset 0.4` · `flyingHeight 0.1` · `animationSpeed 0.35` · `yMovementCurve` 4 键 `(0, 0.000229, s 5.8569) / (0.25, 1, 0) / (0.75, −1, 0.02292) / (1, 0, 5.5617)`、`pre 2(Loop) / post 1(Once)` | `Battle/FlyingCardController.cs:73-80` + `:258-291`（曲线逐值） | **无差异**（另核 `.ctor` 的 1.0/1.0/1.0 被 prefab 覆盖 ⇒ 取 prefab 值） |
| 27 | 飞行卡**状态机 / 到达判据 / 时长** | `Toggle.c`：`time<0` ⇒ 警告+归零；`==0` ⇒ 直接 `Flying/Landed`；`>0` ⇒ 过渡态（生产实参 `DAT_1834b3158 = 0.4`）；`GoToTargetPosition`：`t = clamp01(time/desired)`、`localPosition = lerp(from,target,t)`、**按写之前那个位置**判 `dist² <= 1e-4`（`DAT_1834b2f48`）、到达时再精确写 target；`ChangeState` 的两条早退 + `Landed` 时回 `originalLocalPosition` + 回调 | `FlyingCardController.cs:120-249` | **无差异**（逐句复核 `__{Toggle,ChangeState,Update,InTransitionToFlyState,InTransitionToLandState,FlyingState,GoToTargetPosition,GetFlyPosition,Awake,ctor,IsFlying}.c`）；我们改的两处（普通类 + `Flying` 态只写 y）已在文件头如实标 |
| 28 | 自动取景 / 手动镜头三件 | `unitsZoomCurve` 5 键 13/13 场逐字节同 · `maxZoomForSmallScreenDevicesByAspectRatio` 5 键 · 取景三条曲线逐键逐切线 · `zoomSpeed 12.5` · `dragSensitivity 0.01` · `snapBackSpeed 5` · `invertMouseScrollWheel true` · `worldBounds (89.95,−8,20.115,15.75)`（我们整体平移 −100 ⇒ x −10.05）· `SmoothDamp(smoothTime = dt×zoomSpeed, maxSpeed +∞)` | `CombatAutoZoom.cs:404,481-525,587-703` · `CombatCameraZoom.cs:165,180,185,189,198,757-854` | **无差异**（本次只抽检：`OffZoomLevel 1.0`、三条曲线的键/切线、`worldBounds` 平移那一步 —— 均对得上；深层数学前几轮已逐句核过） |

---

## 二、已修（5 条，全部 `typecheck` 0/0）

| # | 文件:行 | 改了什么 | 判据 |
|---|---|---|---|
| 1 | `Battle/MultiCardDisplay.cs:809` | 一排卡的**卡心 y**：`(BandTopPx+HeaderH+BandBottomPx)/2 = 571.60` → **`(BandTopPx+BandBottomPx)/2 = 540`** | `RT_3176`（Viewport stretch）+ `RT_3174`（Content stretch + `sizeDelta.y 0`）+ `MB_4822`（`ChildAlignment 4 MiddleCenter`）现读；见 §一·20 |
| 2 | `Battle/MultiCardDisplay.cs:794` | **初始横向偏移** `0` → `MaxScroll × 0.5`（装得下时 `Max=0` ⇒ 仍居中，行为不变） | `RT_3174` 的 `m_Pivot (0.5,0.5)` + `m_AnchoredPosition ≈ 0` + `ContentSizeFitter(HorizontalFit=PreferredSize)` ⇒ UGUI 初始 `horizontalNormalizedPosition = 0.5`；见 §一·21 |
| 3 | `Battle/MultiCardDisplay.cs:944` | `TickScroll` 的「世界→设计 px」x 斜率：`EndPanel.PxPerUnit`(108) → **`LayoutSpace.PxPerWorldX`**（= `DesignPxW ÷ VisibleWidth`） | `LayoutSpace.ToPixel` / `ToDesignPixel` 那两段 doc（`ToPixel` 的 x 写死 108、**只在 16:9 是那个意思**）；同文件 `HitContinue` 早前已按这条订正 |
| 4 | `Battle/MultiCardDisplay.cs:1078-1092` | `HitContinue` 的**兜底支**（「条不在画布上」）同上换口径：`world.x×108 + 960 / 540 − world.y×108` → **`LayoutSpace.ToDesignPixel(world)`** | 同 #3；顺手订正了那里「老口径，一字未改」的注释（铁律 5） |
| 5 | `Battle/CardDisplayWindow.cs:1223 / 1307` + `Battle/MultiCardDisplay.cs:863` | **窗级声音三处**：放大窗 **开窗音 `CardExpand`**（只在「关→开」那一拍，= 原版 `GameWindow__TryOpen` 的 `CurrentState==0` 支）· 放大窗 **关窗音 `Close Card Display`**（只在 `Visible` 时，= 原版 `!activeSelf ⇒ return`）· 多卡展示窗 **关窗音 = 默认音 `ButtonClick Back`**（原版 `closeSound` 空 + `useDefaultCloseSoundIfNull=1`）。两窗 `Show()` 内部那次清理都改走 `HideCore(false)`（原版开窗那拍**不播**关窗音） | MB_4750 / MB_2033 / MB_4182 逐字段现读（见 §一·16/·24）+ `GameWindow__TryOpen.c` / `__Close.c` + `WindowSounds` 文件头那张表（同一 pid 已在音效线直读为 `CardExpand` / `Close Card Display`） |

**文档订正（铁律 5，同属本次）**
- `MultiCardDisplay` 文件头 ①：原写「卡间距 **24 px** —— 解包里查不到（我们挑的）」→ **作废**，间距已是原版真值 350（`500×0.7`）。
- 文件头那段「🔴 **我们这一档没做滚动**（装不下就等比缩）⇒ 没有可迁的拖拽口」→ **已过期**（2026-10-24 `W64·B-25` 已做横向滚动），改为如实标两条仍存的偏离（无回弹 / 6 px 阈值）。
- `ViewportClipPx` 增补【判据不足】说明：`Viewport`（GO 769 / `RT_3176`）的 `m_Component` 只有 2 条（`RectTransform` + 一条**没导出 MonoBehaviour** 的内置组件）⇒ 解包里**找不到 `RectMask2D`/`Mask` 实例**；两种读法的可见结果相同 ⇒ **值不动**，只标清。
- `CardDisplayWindow.SlotHasTooltips` 增补「**本口目前零调用**」+ 为什么（下面 §三·4）。

---

## 三、判据不足 / 缺口（已落注释，需别的线接手）

### 3·1 换位音 `soundOnCardSwap` —— **卡在数据侧**（音效线 `工具/import_original_sfx.py`）
- 判据：MB_4750 `soundOnCardSwap = PPtr(fileID 12, **4812904303840730115**)`；
  `ChangeCardPosition.c` 在两条补间**之前** `SoundManager.Play2D(window+0xd8, MixerType.FX(0))`。
- 该 pid 在解包里**有**：`bundle_soundcollection_assets_all/MonoBehaviour/CardMove_4812904303840730115.json`，
  `m_Name = "CardMove"`，**`clipList` 4 条**（`-6740773956303463874` / `5303275677218709805` /
  `-1522524771764259241` / `8024429088123589697`），`minPitch=maxPitch=1` · `vol 1` · `timeToPlayAgain 0.1`。
- **挡点**：`Resources/animfx_sounds.json`（460 条）里**没有** `CardMove` 这个 cue（只有 `CardStartDrag`，
  它的 clipList 恰好只含 4 条里的第 1 条）⇒ `WFSoundBank.TryGetCue` 现在拿不到（会 `BadCues++` + 警告）。
- ⇒ 补齐 = 生成器把这条 4-clip cue 收进表；**不是 `CardDisplayWindow.cs` 能单方面接的**（已在该处注释写清）。

### 3·2 🔴 **我改的 §二·2 会翻一条既有断言**（`Editor/BattleScene.cs` 不归我）
- 现断言：`Editor/BattleScene.cs:13498` `Check(sx0 == 0f, "（前提）B-25⑤：开窗时横向偏移 = 0")`。
- 按 §一·21 的判据，原版初位是 **`MaxScroll × 0.5`**（= 归一化 0.5）。
- 建议改成：`Check(Mathf.Abs(mcd.ScrollPxForTest - mcd.MaxScrollPxForTest * 0.5f) < 1f, "（前提）B-25⑤：开窗初位 = ScrollRect 初始 normalizedPosition 0.5（内容居中）")`
  —— 注意同一段后面「往右拖到头 ⇒ 夹回 0」那一条（`:13526`）**仍然成立**（它会把偏移推到 0）。
- ⚠️ 同段 `:13459` 的注释「③ 滚动上限 = max(0, 自然宽 − 视口宽 1920)」不用改。

### 3·3 `closeOnESC = 1`（两扇窗都有）—— 要动 `BattleDriver`
- 放大窗 MB_4750 `closeOnESC = 1` · 多卡展示窗 MB_4182 `closeOnESC = 1`；原版消费者 =
  `GameWindow.ESCPressed` / `GameWindow__ToggleESC`（继承来的）。
- 我们：`BattleDriver` 只在结算屏处理 ESC ⇒ 这两扇窗开着按 ESC 不关。
- 另注（来自 `资料/真Play待验清单.md` / 战斗线 §一）：**本机 ESC 全族目前不可判**（`wf_click key esc`
  与 PowerShell `SendKeys` 两条路都无效、鼠标正常）⇒ 补了也只能静态验，真机要人按键盘。

### 3·4 `enableCardTooltips = 1`（多卡展示窗）/ `ToggleTooltips(index==0)`（放大窗）—— 要动 `BattleDriver`
- 原版：放大窗只**前台那张**开 tooltip（`InitializeCardForDisplay.c` 的 `ToggleTooltips(param_3 == 0)`）；
  多卡展示窗 `enableCardTooltips = 1`（`MB_4182`）。
- 我们：tooltip 泵是 `BattleDriver.TickTooltipAt`，**只遍历棋盘/手牌那批卡的命中区** ⇒ 两扇窗里的卡
  **悬停不出关键词/数值 tooltip**；`CardDisplayWindow.SlotHasTooltips(i)` 是**零调用**的死口（已标注）。
- 补法：驱动层把「放大窗前台那一格」与「多卡展示窗里每个可见卡格」加进候选（命中的是
  `CardView.TraitAnchor*` / `StatAt` 那一套，判据已有）。

### 3·5 `Viewport` 那条第 3 个组件（`pathID 2291`）是什么 —— **坐实不了**
- `GameObject/Viewport_769.json` 的 `m_Component` = `[RectTransform 3176, 2291]`，而 **没有
  `MonoBehaviour_2291.json`** ⇒ 它是一条**内置组件**（`CanvasRenderer` 最可能），**不是** `Mask`/`RectMask2D`
  （那两个在别的窗里都导出成了 MonoBehaviour，见 `LanguagesDropdown/Viewport` 的 `MB 4401+4599`）。
- ⇒ 「多卡展示窗视口裁切」这一层**只有间接判据**（原版是 `ScrollRect` + 视口节点 + `Clamped`）。
  两种读法（真不裁 / 裁 572.63 高）**可见结果相同**（卡心 540、卡高 520.3 ⇒ 永远在范围内）
  ⇒ 我们保持现状并在 `ViewportClipPx` 的 doc 里标清，⛔ 别照读法② 顺手改成 572.63。

### 3·6 `voiceOverAudioSource = {m_PathID:0}`（原版两扇窗都是空）
- 我们给窗加了一颗 `AudioSource`（`CardDisplayWindow.cs:679` 一带），`volume 0.85` 是**我们自己挑的**
  （原版逐 source 的音量没查到）；但音频走 `Voices` 混音组 ⇒ 受设置里「语音」滑块控制。
- 另：原版「语音钮显不显示」还串了两条 **Remote Config** 开关（`SetCardVoiceOver.c` 的
  `+0x10` / `+0x11`，分别管 cardType 0 / 10）；我们按 `CardButtons.HasVoice` 判（无服务器 ⇒ 取不到那两条）。
- 这两条**不影响画面**，如实记在这里，⛔ 不算缺陷（`GetAvailableSounds().Count > 0` 与 `HasVoice` 的差在有卡的池内为 0）。

---

## 四、只能真机看的（本环境不可判，单列）

| # | 事项 | 怎么验 |
|---|---|---|
| R1 | **多卡展示窗卡心下移 31.6 px 的观感**（540 vs 571.6） | 起 player → 点我方牌堆（我们挑的入口 `BattleDriver.ShowMyDeck`）摊开一次，看整排是否**在窗口带里竖直居中**、上下留白是否对称 |
| R2 | **装不下时初始看中间**（先滚到中间那几张） | 同一入口，牌库 ≥ 5 张时看第一帧是不是**中间几张**在正中（原版是） |
| R3 | **两扇窗的开/关窗音**（`CardExpand` / `Close Card Display` / `ButtonClick Back`） | 点开/关一次放大窗与多卡窗，听「开一响、关一响、同级音量」；⚠️ 连点过快会被 `timeToPlayAgain 0.1` 节流（原版行为，不是 bug） |
| R4 | **ESC 关窗**（§三·3） | ⚠️ 本机 ESC 疑似不可用（两条独立路都无效）⇒ 需真人键盘复核；判「逻辑坏没坏」不能只靠这一条 |
| R5 | 换位音（§三·1 补完之后） | 放大窗里点后面那张卡换位，听是否有一条短促的「move」音 |
| R6 | 扇形重叠/队列的观感（9 槽时最外那两张是否压边/出屏） | 找一张相关卡 ≥ 6 的卡开窗（槽 5–8 是我们外推的），看有没有压屏边 |

---

## 五、顺手发现（不属本片，交调度台分流）

1. **`资料/任务线/战斗线/任务/T04_卡面放大窗残余.md` 已过期**（本片的账）：文件头写「🔴 未完成」，
   表格里 **#1 卡背翻面** 与 **#2 开窗动画** 都标 `❌` —— 但两者**都已做完**（`CardDisplayWindow.SetFrontVisible`
   2026-10-19 · `PlayOpenAnimation` 2026-10-16，判据与断言都在）。⇒ 建议主对话按铁律 5 就地订正该表（我不改台账）。
2. **`CardFan.Slots = 9` vs 原版 1+4**：我们多出的 4 槽是 2026-09-26 用户授权的冗余，**不是缺陷**；
   但 `QEffect = 3020` 与 9 槽队列（3009–3017）之间的空号只剩 2 个 ⇒ 将来再加层要重新排队列段。
3. **`T04` 表 #5「数值变化飘字」**（`CardTextCountersController` ×5）仍未做 —— 属**卡面**（`CardView`，`B2` 的地盘），
   本片不碰；`CardOverdrawHandle` 那半（#顺手一条）**本次已核清语义 = 我们的两轴相与**，可以销账。
