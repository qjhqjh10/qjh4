// BoosterPackOpenWindow.cs — §三 第 29 条 **A7**：商店的「Booster Pack Open Window」（**开包窗**）
//
// ============================ 出处（唯一正本） ============================
// **几何/结构**：`资料/阶段二_商店_原版规格.md` **§五·三**（+ 本节下面那张「层 × 场景 × 出现条件」表）。
//   复核命令：`python 工具/menu_dump.py bundle_menus_assets_all "Booster Pack Open Window" --depth 9 --relative`
//             （绝对矩形 1920×1080 · 左上原点 · y 向下）
// **窗口字段**：MB `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_9012570135841684515.json`
//   —— 原样抄进 `Create()`：`type = 0 (Fullscreen)` · `windowsPlacement = 5 (Canvas)` ·
//   `closeOnESC = 0` · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` ·
//   `timeToShowHelpText = 10.0` · `useQuickOpenProtection = 1` · `closeAnimation = "Booster Window Close"`。
//   ⚠️ **这一扇是 `Canvas(5)` 不是弹窗 `Popup(15)`** —— 与 `BoosterInfoPopup` 的 `World(10)` 也不同，三扇各自实测、别互推。
// **行为**：**原版反编译方法体**（第一权威，`d:/2/tools/decomp_full/`）：
//   `BoosterPackOpenWindow__{Initialize,OnEnable,BoosterPackOpen,CardOpened,ToggleTextHelper,Update,CloseButtonClick}.c` ·
//   `CardInBoosterPack__{Initialize,ChangeState,UiColliderOnClick,TryHighlightCardback}.c` ·
//   `CardInBoosterPack._WaitForSpinAnimationEnd_d__26__MoveNext.c` · `BasicCardUI__{SetRawCardData,ToggleNewCardBadge,ToggleCarUpgradeIfNeeded}.c` ·
//   `BoosterPackBackground__Initialize.c`。逐条落地见下面每个方法的注释。
//
// ============================ 🔴 5 张卡的位姿：**从原版 clip 里读出来的** ============================
// `Booster Window Open`（0.617 s，`menus_assets_all` 的 `AnimationClip_5911697182262325120`）**末帧**：
//   · 5 张 `Booster Animation Parent/Cards/CardInBoosterPack UI n` 的 **localScale = 151**
//     （`m_ScaleCurves`，t=0.217 时是 157.5815、t=0.617 收到 151）；
//   · 它们的 **x = −730 / −360 / 0 / +360 / +730**、**y = 0**、**z = 129**（`m_FloatCurves`，t=0.617 那批单键）。
// `Booster Window Close` 里同样 5 条 scale=151（起手就是停住的位姿）⇒ **两处互证**。
// 换算到画布 px：`Booster Animation Parent` 的 `m_LocalScale = **0.876259982585907**`（RT 实测）
//   ⇒ 卡心 x = 960 + {−730,−360,0,360,730} × 0.87626 = **320.33 / 644.55 / 960.00 / 1275.45 / 1599.67**，
//     卡心 y = **540**（`Booster Animation Parent` 矩形 910,490→1010,590 的中心）。
//   ⇒ 卡单位 → px 的系数 `CardK = 151 × 0.87626 = **132.3153**`。
// 🔴 **原版那 5 张卡的 `m_SizeDelta` 是 2.5437×3.3686**（模板位、**动画驱动**）；卡本体是它子节点 `2DCard` 的
//   **2.0927×3.3313** —— 与 `CardView.Width/Height` **逐位相同**（那也正是工程里 `CardView` 的尺寸来源）。
//   ⇒ 画出来 **276.90 × 440.77 px**。**不要**拿 2.5437 去算（那是容器，不是卡）。
//
// ============================ 层 × 场景 × 出现条件（**每一格都有出处**） ============================
// | # | 层（原版节点名） | 矩形（根 1920×1080 · 左上原点） | 什么时候出现 |
// |---|---|---|---|
// | ① | `Booster pack Background` | −100,−100 → 2020,1180（**2120×1280**，四周出血 100） | 恒。`Image.m_Sprite = **0**`（运行期赋图）⇒ 原版画出来就是**一块纯色**（`m_Color` = 白）。有传奇 ⇒ `BoosterPackBackground.Initialize(true)` 把颜色换成 `thereIsALegendaryCardbackgroundColor` = **(1.51358,0.45948,0,1)** |
// | ② | `Booster Animation Parent` | 910,490 → 1010,590（100×100） | 恒（`scl 0.87626` · `z −202`） |
// | ③ | `Cards` | 同 ② | 恒（自有 `Canvas`：`m_OverrideSorting = 1` · `m_SortingOrder = 2` ⇒ **画在背景之上**） |
// | ④ | `CardInBoosterPack UI 1..5` | 动画末帧见上 | 恒 |
// | ④a | `Cardback Shadow SDF` | 2.92×3.81 卡单位 | `ChangeState(1)` ⇒ **翻之前** |
// | ④b | `Cardback` | 2.17×3.14 卡单位 | 同上（`ToggleCardBack(true)`） |
// | ④c | `2DCard`（卡面） | 2.0927×3.3313 卡单位 | **翻开之后** |
// | ④d | `Card Ready for level up` | 1.78×2.74 @ (0,+0.02)（父 = `CardUI`） | 出厂 **INACT**；翻开后 = `BasicCardUI.ToggleCarUpgradeIfNeeded` → `CardServices.CheckForCardUpgrade(card)` |
// | ④e | `New Card Badge`（+ 子 `Text`） | 1.21×0.39 @ (0.32,1.14)、锚 (0,0.5) | 出厂 active **但 `SetRawCardData` 一进来就关**；翻开后 = `ToggleNewCardBadge(GetOwnedCount(card) **== 1**)` |
// | ④f | `Ban Icon`（+ 子 `Banned Text`） | 锚 (.1,.5)-(.9,.5)、`sd (0,2.23)`、`preserveAspect` | **恒不显示**：`BasicCardUI.SetRawCardData` 末尾 `SetActive(bannedIcon, **false**)`，而翻牌链**从不** `ToggleBanned` |
// | ⑤ | `Tap to discover` | 1156.67,977 → 1865,1080 | **开窗先关**；停手 **10 s** 后淡入（`Update`） |
// | ⑥ | `Tap to close` | 同 ⑤ | **5 张全翻开**后出现（`CardOpened` 里 `cardsLeftToOpen` 归零 ⇒ `SetActive(true)` + `DOScale ×1.1` 0.4 s OutBack） |
// | ⑥b | `Tap to close/Collider` | −534.74,−204.55 → 3318.35,2027.98（**盖满整屏**） | 跟 ⑥ 一起 ⇒ **点哪儿都关** |
//
// ============================ 🔴 我们挑的 / 缺口（逐条出声，红线：不许静默失败） ============================
//   · **卡包内容是我们编的**：原版是服务端 drop table（`Booster Pack Ultramarines` SO 的 `dropTableItems` 指着 `DT Ultramarines All R2+` / `DT Ultramarines All`；原写「两张**没导出来**的掉落表」）
//     ⚠️ 2026-10-28 订正（B93_Q8 现核）：**不成立** —— 34 份 `DT *` + 11 个 Booster 容器都在 `assets_full`（`bundle_menus_assets_all` / `bundle_cosmeticsso_assets_all` / `bundle_duplicateassetisolationso_assets_all`），之前因**文件名带空格**漏检；判别字段 = `cantBeRandomlyCreated`（过滤已接进 `RollPack`）。
//     我们按**同一份 SO 的出厂文案**定规则：「Contains 5 cards for the Sautekh army.」
//     + 「At least one of the cards is guaranteed to be Rare or better」
//     ⇒ **5 张 + 至少 1 张 rare/epic/legendary**，阵营按商品主图对（见 `ArmyOfArt`）。
//     🔴 **哪 5 张是我们挑的**（用 `System.Random(seed)` 定种子可复现；原版 `Initialize` 里那句 `ListExtensions.Shuffle` 是**无种子**的）。
//   · ✅ **翻牌动画已做（2026-10-17 · 界面线 T19）** —— 🔴 **就地订正（铁律 5）**：本行原来写
//     「**翻牌动画没做**：原版是 `Content` 上播 `Booster Opening - Card *` …… 我们直接换层」—— **不成立**。
//     现在按原版三份 clip 的**关键帧**用代码补间（`Shell/BoosterPackAnim.cs`）：翻牌旋转（含 ±40° 反向蓄力）、
//     `0.95` 压扁、`Content` 起手抖（±0.025 卡单位）、`EnableCardBackAnimEvent` 那一拍换层 + `localScale.x → -1`、
//     收尾 `ResetContentAnimation` 回 `Card Idle`（8 s 循环）。
//     ⚠️ **等价物**：原版是 legacy `Animation` 组件播 clip，我们**不能**直接播那 7 份 `.anim`
//     （路径/缩放/锚点都按原版 prefab 烤死，与我们的绝对 px 坐标系对不上）⇒ 采样表 + 代码补间，逐条标注在
//     `BoosterPackAnim.cs` 文件头。**没有 0.2 s 的 `CrossFade` 交叉淡入**（我们只播一条、没有混合层）。
//   · ✅ **稀有度相机抖已做（等价物）**：`contentByRarities[i].shakes`（rarity 2 延迟 0.65 幅度 5 ·
//     rarity 3 延迟 1.0 幅度 40 · rarity 4 两条：延迟 0 频率 0.25 + 延迟 1.15 幅度 20 方向 z 10 衰减 0.5）。
//     菜单层**没有相机** ⇒ 抖的是**卡容器**（`Booster Animation Parent`）。⚠️ preset SO 本地没有
//     （`bundle_menus_assets_all/MonoBehaviour/` 搜 `"amplitude"` 只命中 5 份 `CardInBoosterPack`）
//     ⇒ `sustain/attack/decay` 的**缺省值拿不到**；时长 0.5 s / 8 Hz / 包络是**我们挑的**。
//     传奇第一条的幅度取 preset 缺省值 ⇒ **不抖**（不猜）。缺口与判据 → `BoosterPackAnim.cs` 文件头。
//   · ✅ **音效已接（2026-10-17 · 音效线 `W_G`）** —— 🔴 **就地订正（铁律 5）**：本行原来写
//     「`clickSound` / `newCardSound` / `openSound` / `closeSound` / 两段音频 cue **全在远端音频表里，本地没有** ⇒ 不播」
//     —— **不成立**。那几条 cue 全在 `bundle_soundcollection_assets_all` 里，逐个解出来了：
//     `CardClick 2`→`UI Click Card 2` · `New Card`→`New Card` · `Booster Pack Window Close`→`Cardshuffle out` ·
//     `Booster Pack Open Card Rarity 1..4`→`Cardflip_{Common,Rare,Epic,Legendary}` ·
//     `Booster Open *`→`Open_Cardpack_*`。表与播放口 → `Shell/WindowSounds.cs`（唯一一张表）。
//     落点：**拆包声**在 `Build()`（原版在 `BoosterPackVisualItem.Initialize`，那件东西我们没建 ⇒ 落点是我们挑的）·
//     **点卡声 + 稀有度声**在 `Reveal()`（原版 `UiColliderOnClick` + `ChangeState(3)`）·
//     **`newCardSound`** 跟 `IsNewCard`（= 角标那条，**当前恒假**，见下）·
//     **关闭声**在 `CloseFromTap()`（原版 `CloseButtonClick`，之后基类 `Close()` 还会再响一声默认关窗音）。
//     ⚠️ `BoosterPackVisualItem.openBoosterPack` 是我们**用 `Army` 反查**的（原版是那条 prefab 上的字段）；
//     映射表与判据 → `WindowSounds.BoosterOpenCue`。
//   · ✅ **`useQuickOpenProtection` 已做（2026-10-17 · T19）** —— 🔴 **就地订正（铁律 5）**：本行原来写
//     「没做 —— 时间源在批处理里不可靠」—— **不成立**。按 `BoosterPackOpenWindow__CalculateReOpenProtectionDelay.c`
//     逐句复刻（60 s 窗口 · 连开超过 4 次后每次 +0.15 s · 上限 2.0 · `<= 0.15` 按 0）：
//     `BoosterPackAnim.StepProtection`，时间源可注入（`Now`，批处理里换掉即可）。
//     它触发的就是 `CardInBoosterPack.ChangeState(4)` 的**卡抖动**（抖完才翻）。
//   · **快速开包 = 只有内部 API**（`QuickOpenAll()`）：原版 prefab 的整棵树**没有任何 skip / open-all 件**
//     （`menu_dump --depth 6` 实读；MB 字段里也只有 `useQuickOpenProtection`）⇒ **入口判据未定**，⛔ 不造按钮。
//   · **翻开之后再点卡 → 卡详情窗（原版 `CardInBoosterPack.CardClicked`）没接**：它在
//     `_WaitForSpinAnimationEnd` 收尾时给 `uiCollider.onClick` **再加**一个 `CardClicked` 监听，
//     `CardDisplayWindow.ShowCard(card, …, true, …)`。我们的卡翻开后把命中区关掉了（`SlotHits` 失效）——
//     ⇒ 这一条**没复刻**（缺口），要接得先决定用哪扇详情窗（`Shell/CardDetailPopup` 还是战斗那扇）。
//   · ✅ **`OpenCardbacks` 那条 clip 不接是【对的】** —— 已查清（2026-10-27 `B9_W34FEED` 订正「用途未查清」）= `DeckGeneralInfoDemo.cardbacksAnimation`（0.25 s · Once · 卡背两层入场），消费者 = `Shell/PracticeModePopup.BuildCardbacks`（⛔ 与开包窗无关）。
//   · ✅ **背景 `Booster Window - Background Shake On Open`（0.9167 s）已接（2026-10-27 `B9_W20WIRE`）** —— 🔴 就地订正（铁律 5）：
//     原写「只动 `floatProperty1/2`、原版 shader 逻辑本地没有 ⇒ 没有等价物可依」**不成立**（`B8_AS` 已解出 `Shader Graphs/Nebula` 的 `_Distort_*`）。
//   · ✅ **垂直对齐已落（2026-10-16 · A712 阶段 2）**：`Tap to discover` / `Tap to close` 两条 TMP 原版是
//     `m_VerticalAlignment = **8192 (Capline)**` + `m_HorizontalAlignment = 4 (Right)`
//     ⇒ 两条都调 `MenuDraw.SetVAlign(…, Label.VAlign.Capline, HintR)`（横向那半走 `AlignRight`，本来就有）。
//     🔴 **就地订正（铁律 5）**：这段原来写着「我们统一按**框内居中**画（全工程口径）。差约四分之一行高，
//     **没实拍可对** ⇒ 标成近似」—— 那在 `Battle/Label.cs` 有 `SetVAlign` 之前是对的，**现在过期了**
//     （阶段 1 于 2026-10-15 落了逐档算式）⇒ 本笔落成真调用，**不再标「近似」**。
//     **字号 49.82 · 颜色 (0.5566,0.5566,0.5566,1) 是原版值**。
//   · **`New Card Badge` / `Banned Text` 的文案**：原版 prefab 里是**俄语占位**（`Новинка!` / `Запрещено`），
//     运行期由远端 I2 词条覆盖。按 `Daily Shop Tab` 的先例（葡语 `Atualiza em:` → 英文 `Refreshes in:`）
//     我们用英文并**记在这里**（`NewBadgeText` / `BannedText`）。
//   · **`Card Ready for level up` 的判据是我们的替身**：原版 `CardServices.CheckForCardUpgrade` 是服务端数据
//     ⇒ 用工程既有的 `CardProgress.CanUpgrade`（与「卡片详情窗」同一份）。⚠️ 我们的拥有数口径是**「给足」**
//     ⇒ **单看拥有数这一条，它本来是恒真的**（5 张卡都亮那对箭头；原版由服务端决定）。**如实标**。
//     🔴 **2026-10-12 就地订正（铁律 5）**：本行原来只说到「**恒真**」就完了 —— **已经变了**：
//     用户当天裁定「**默认最高级**」⇒ `CardProgress.Level` 的缺省 **1 → `MaxLevel`**，
//     而 `CanUpgrade` 的第一句就是 `l < MaxLevel` ⇒ **本作所有卡都判成满级、这一条现在【恒假】**
//     （那对箭头**本版永远不亮**）。⇒ 「恒真 → 恒假」是**同一裁定的必然后果，不是新缺陷**
//     （「默认最高级」的答案本来就是「没有卡能升级」）。⚠️ 层照建（`UpBadges[i]` 仍在），
//     只 `SetActive(false)` —— 将来口径若回到「可升级」，这一条自己就活了，⛔ 别删层。
//   · **`New Card Badge` 恒不显示**：判据 `GetOwnedCount(card) == 1`（这是你**第一张**）。我们的
//     `CardProgress.Owned` 默认就给足（≥ 卡组上限 + 9）⇒ 这一条**恒假**。层照建、如实说，**不是忘了做**。
//   · **粒子的位置/缩放是按推导来的**：原版把 `contentByRarities[i].particles` 实例化成 **卡的子物体**
//     （继承卡节点 `151 × 0.87626` 那个缩放）⇒ 我们给一个同比例的 `VfxAnchor`（`CardK / 108`）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `BoosterPackOpenWindow`（商店买完卡包弹的那扇**开包窗**）。</summary>
    public class BoosterPackOpenWindow : GameWindow
    {
        // 队列档：**在全屏/弹窗那一整段（3100–3169）之上、`Tooltip`（4077–4079）之下**。
        // 🔴 为什么落在这里：原版这一扇是 `windowsPlacement = 5 (Canvas)` = 挂在
        //    `2 - Canvas Holder Above upper bar` 上（**在顶栏之上**）。
        //    🔴 **2026-10-11（A307 现读订正，铁律 5）**：这里原写「我们的壳把顶栏定死在 `QBar* = 3600~3604`
        //    （2026-09-28 那条口径：**全工程只有顶栏排在所有窗口之上**）⇒ 本窗**不越顶栏**，落在 3169–3197。
        //    这是一处**口子上的偏离**：原版它压在顶栏上、我们压在顶栏下」—— 🔴 **那段已被同日的 A283 抹平**：
        //    顶栏整条降到 **`2994–2998`**（用户 2026-10-11 裁定「照原版」，判据 → `MainMenuRuntime.QBarPanel` 那段）
        //    —— ⚠️ **2026-10-18 订正**：这个号段**已过期**（2026-10-17 F5 把下沿从 `2994` 放宽到 `2986`
        //    ⇒ **现带子 = `2986–2998`**，**唯一出处 = `Shell/TopBar.cs` 那张表**）；**下面那条结论不受影响**
        //    （本窗 3170–3197 仍在顶栏之上 —— 放宽的是**下沿**，顶栏最高那一号 `QBarOverlay 2998` 没动）。
        //    ⇒ 本窗 **3170–3197** 现在**在顶栏【之上】**，与原版（挂 `2 - Canvas Holder Above upper bar`）**一致**
        //    ⇒ ✅ **已对齐、不再是偏离**（⛔ 别再把这条当待办挂着）。
        //    ⚠️ 顺带订正同段两个旧号：`Tooltip` 早已不是 **3199**（2026-10-04 搬到 `3605–3607`；
        //       2026-10-20 W58 再搬到 `4077–4079` —— 前一号段是历史留痕，⛔ 别当现状）、
        //       这段带子的下界也从 **3169** 变 **3170**（`QCloseSurface = QBase − 1` 那号 2026-10-04 A47 就删了）。
        public const int QBase = 3170;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿这两个档核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        public const int QShade = QBase;          // 3170 整屏背景（纯色）—— **「点哪儿都关」那一层也用它**
        // 🔴 **2026-10-04（A47 接线批）删掉了 `QCloseSurface = QBase − 1`（3169）**：「内容档 − 1」这个写法
        //   按规矩是错的（压暗层的命中区必须落在**压暗层自己那一档**）⇒ 整屏那块改走 `MenuDraw.ShadeHit`、
        //   直接用 `QShade`（仍**严格低于** `QCard` ⇒ 卡照样能点）。见 `Build()` 里那一段。
        public const int QCard = QBase + 1;       // 3171 起，**每张卡 5 档**（见 CardStride）
        const int CardStride = 5;                 //   +0 卡影SDF · +1 卡背 · +2 卡面 · +3 角标 · +4 命中区
        const int QHint = QCard + 5 * CardStride; // 3196 两段提示字
        const int QHintHit = QHint + 1;           // 3197 `Tap to close` 那个节点自己的命中区

        // ============================================================ 真值（MB / RT / clip）

        /// <summary>`Booster pack Background`（四周出血 100px）。</summary>
        static readonly PxRect BgR = new PxRect(-100f, -100f, 2020f, 1180f);
        /// <summary>常态背景色 = 那张 `Image` 的 `m_Color`（实读 `(1,1,1,1)`）。</summary>
        static readonly Color BgNormal = new Color(1f, 1f, 1f, 1f);
        /// <summary>有传奇时的背景色 = `BoosterPackBackground.thereIsALegendaryCardbackgroundColor`。
        /// ⚠️ 红分量 **&gt;1**（原版就是 HDR 值，不是抄错）。</summary>
        static readonly Color BgLegendary = new Color(1.513579f, 0.459480f, 0f, 1f);

        /// <summary>`Booster Animation Parent`（100×100，中心 = 屏心）。</summary>
        static readonly PxRect AnchorR = new PxRect(910f, 490f, 1010f, 590f);
        /// <summary>`m_LocalScale` 实测。</summary>
        const float AnchorScale = 0.876259982585907f;

        /// <summary>`Booster Window Open` 末帧：5 张卡的 x（父级局部单位）。</summary>
        static readonly float[] CardX = { -730f, -360f, 0f, 360f, 730f };
        /// <summary>`Booster Window Open` 末帧：5 张卡的统一 `localScale`。</summary>
        const float CardScale = 151f;
        /// <summary>卡单位 → 画布 px（= `CardScale × AnchorScale`）。**别在别处再乘**。</summary>
        public static float CardK { get { return CardScale * AnchorScale; } }     // 132.3153

        /// <summary>卡本体（`2DCard`）—— 与 `CardView.Width/Height` 逐位相同。</summary>
        const float CardW = 2.0927f, CardH = 3.3313f;
        /// <summary>`CardUI` 那个容器（**不是卡本体**，别拿它算卡面）。</summary>
        const float ContainerW = 2.5437f, ContainerH = 3.3686f;
        /// <summary>`Cardback` / `Cardback Shadow SDF` 的尺寸（卡单位）。</summary>
        const float BackW = 2.17f, BackH = 3.14f, BackSdfW = 2.92f, BackSdfH = 3.81f;
        /// <summary>`Card Ready for level up`：1.78×2.74 @ (0, +0.02)（父 = `CardUI`）。</summary>
        const float UpW = 1.78f, UpH = 2.74f, UpDy = 0.02f;
        /// <summary>`New Card Badge`：1.21×0.39 · 锚 (0,0.5) · `anchoredPosition (0.32, 1.14)`（父 = `CardUI`）。</summary>
        const float BadgeW = 1.21f, BadgeH = 0.39f, BadgeAx = 0.32f, BadgeAy = 1.14f;
        /// <summary>`Ban Icon`：锚 (.1,.5)-(.9,.5) ⇒ 宽 = 父宽 × 0.8；`sd (0, 2.23)`；`preserveAspect`。</summary>
        const float BanW = ContainerW * 0.8f, BanH = 2.23f;

        /// <summary>两段提示字的矩形与字号/颜色（原版 TMP 实测）。</summary>
        static readonly PxRect HintR = new PxRect(1156.67f, 977f, 1865f, 1080f);
        const float HintPx = 49.81999969f;
        static readonly Color HintColor = new Color(0.5566f, 0.5566f, 0.5566f, 1f);
        /// <summary>`Tap to discover/Text` 的**出厂原文**（原版 prefab 那颗 TMP 的 `m_text`）。
        /// 🔴 **2026-10-17（`A1756`）起上屏走词条** `MenuShop/BoosterPackOpening/TapToDiscover`
        /// （官方两列 = `Tap on the cards to discover` / `点击卡片进行探索`）。
        /// ⚠️ 这里是**属性**不是 `const`：自检（`Editor/ShopScene.cs`）读到的也要是**当前语档**的值
        /// （`const` 只能写死英文，中文档下那条断言会假红）。</summary>
        public static string DiscoverText { get { return Loc.T("MenuShop/BoosterPackOpening/TapToDiscover"); } }
        /// <summary>`Tap to close` 的**出厂原文**；词条 = `MenuShop/BoosterPackOpening/TapToClose`
        /// （官方两列 = `Tap to close` / `点击关闭`），规格同 <see cref="DiscoverText"/>。</summary>
        public static string CloseText { get { return Loc.T("MenuShop/BoosterPackOpening/TapToClose"); } }
        /// <summary>`Tap to close/Collider`：盖满整屏那块（`NonDrawingGraphic`）的矩形。</summary>
        static readonly PxRect CloseSurfaceR = new PxRect(-534.74f, -204.55f, 3318.35f, 2027.98f);

        /// <summary>`timeToShowHelpText`（MB 原文 **10.0**）：静止这么久 ⇒ `Tap to discover` 淡入。</summary>
        public const float HelpDelay = 10f;
        /// <summary>悬停放大系数（`CardInBoosterPack.TryHighlightCardback` 里的 `x1.1` 常量，
        /// 从 `GameAssembly.dll` 的 `_DAT_1834b31f8` 读出 = **1.1**）。</summary>
        public const float HoverScale = 1.1f;
        /// <summary>悬停补间时长（同族常量 `_DAT_1834b2dc8` = **0.3 s**）。</summary>
        public const float HoverTime = 0.3f;

        // ---- 角标文案（**原版出厂是俄语占位**，见文件头）----
        /// <summary>`New Card Badge/Text` 的出厂原文 = `Новинка!`（俄语「新品」）。</summary>
        public const string NewBadgeFactoryText = "Новинка!";
        /// <summary>我们显示的（英文）。</summary>
        public const string NewBadgeText = "NEW";
        /// <summary>`Ban Icon/Banned Text` 的出厂原文 = `Запрещено`（俄语「禁止」）。</summary>
        public const string BannedFactoryText = "Запрещено";
        /// <summary>我们显示的（英文；**这一层恒不显示**，见文件头）。</summary>
        public const string BannedText = "BANNED";

        // ============================================================ 状态

        public int Page { get; private set; }
        public int Index { get; private set; }
        /// <summary>这一包装的是哪个阵营（空 = 全卡池；判据 → `ArmyOfArt`）。</summary>
        public string Army { get; private set; }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static BoosterPackOpenWindow LastOpened { get; private set; }

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        /// <summary>这一包的 5 张卡（**我们摇的**，见文件头）。</summary>
        public CardDef[] Cards { get; private set; }
        /// <summary>哪几张已经翻开。</summary>
        public readonly bool[] Opened = new bool[5];
        /// <summary>还没翻开的张数（原版 `cardsLeftToOpen`，`CardOpened` 里自减）。</summary>
        public int CardsLeft { get; private set; }
        /// <summary>开窗到现在过了多少秒（原版 `Update` 里那个累加器）。</summary>
        public float TimeAlive { get; private set; }
        /// <summary>提示文字现在藏着吗（原版 `_helperTextHidden`）。</summary>
        public bool HelperHidden { get; private set; }

        /// <summary>自检用：每一格的节点 / 卡背 / 卡面 / 三个角标 / 命中区。</summary>
        public readonly Transform[] SlotNodes = new Transform[5];
        public readonly GameObject[] BackNodes = new GameObject[5];
        public readonly GameObject[] FaceNodes = new GameObject[5];
        public readonly GameObject[] UpBadges = new GameObject[5];
        public readonly GameObject[] NewBadges = new GameObject[5];
        public readonly GameObject[] BanIcons = new GameObject[5];
        public readonly WindowButton[] SlotHits = new WindowButton[5];
        /// <summary>每格的粒子挂点（带 `CardK / 108` 的缩放，见 `BuildSlot` ④g）。</summary>
        public readonly Transform[] VfxAnchors = new Transform[5];
        /// <summary>自检用：翻牌时真正播出去的那 4 个粒子 prefab 名（按稀有度）。</summary>
        public readonly List<string> PlayedFx = new List<string>();
        /// <summary>自检用：`Tap to discover` / `Tap to close` 两个节点。</summary>
        public Transform DiscoverNode { get; private set; }
        public Transform CloseNode { get; private set; }
        /// <summary>自检用：整屏那块「点哪儿都关」的命中区。</summary>
        public WindowButton CloseSurfaceHit { get; private set; }
        /// <summary>自检用：背景那张 quad（量颜色用）。</summary>
        public ImageQuad BgQuad { get; private set; }

        // ============================================================ 🆕 T19：动画状态机（翻牌 / 抖动 / 快速开包）
        //
        // 判据与逐条出处 → `Shell/BoosterPackAnim.cs` 文件头（clip 采样表 + 反编译方法体）。
        // 原版那一套是 **legacy `Animation` 组件**播 clip；我们**不能**直接播那 7 份 `.anim`
        //   （路径/缩放/锚点都按原版 prefab 烤死：`Cards` 节点带 0.8763、卡节点 scale 151、
        //   而我们的坐标系是绝对 px、scale 1）⇒ **按同一份关键帧用代码补间**（等价物，逐条标注）。
        //
        // 状态号**照抄原版 `CardInBoosterPack.state`**（`CardInBoosterPack__ChangeState.c`）：
        //   0 = 无 · 1 = 卡背态（交互关）· 2 = 可交互 · 3 = 翻牌中 · 4 = 防连开抖动 ·
        // 🔶 自己设计（非原版）：自加状态号 `5 = 翻完了` · 原版翻完停在 3；5 只给我们自检读，⛔ 不是原版状态号（原版判据拿不到） · TODO(谁定的)｜原注：5 = **我们自己加的「翻完了」**（原版翻完之后 `state` **停在 3** —— `_WaitForSpinAnimationEnd`
        //       只把交互重新打开、不再改 state，再点走的是它后来加上的 `CardClicked` 监听）
        //       ⇒ 5 只表示「这一张已经收尾」（自检好读），⛔ 别把它当原版状态号。
        public enum CardState { None = 0, Back = 1, Ready = 2, Opening = 3, Shaking = 4, Open = 5 }

        /// <summary>每张卡的当前状态（原版那套号 + 我们加的 5）。</summary>
        public readonly int[] CardStates = new int[5];
        /// <summary>每张卡的 `Content` 节点（原版 `CardInBoosterPack > Content` —— 翻的就是它）。</summary>
        public readonly Transform[] ContentNodes = new Transform[5];
        /// <summary>开包 clip（`Booster Window Open`）播完了吗 —— 播完之前**点卡无效**（原版 state 1）。</summary>
        public bool WindowOpenDone { get; private set; }
        /// <summary>这一包的防连开延迟（`CalculateReOpenProtectionDelay` 的返回值；0 = 不抖）。
        /// 🆕 这一格**此前一直是缺口**（文件头旧注释写「时间源在批处理里不可靠」）——
        /// 现在按原版公式实现了，时间源可注入（<see cref="Now"/>）。</summary>
        public float ProtectionDelay { get; private set; }
        /// <summary>快速开包被调用过几次（内部 API —— 原版**没有**这个入口，见 `QuickOpenAll`）。</summary>
        public int QuickOpens { get; private set; }

        /// <summary>防连开的时间源（**可注入**：批处理里 `Time.unscaledTime` 可能不动）。
        /// ⚠️ 静态字段 —— 自检用完记得 `ResetReOpenProtectionForTest()`。</summary>
        public static System.Func<float> Now = () => Time.unscaledTime;
        static float _protLastOpen = float.NegativeInfinity;
        static float _protDelayState;
        static int _protCount;

        /// <summary>清掉防连开的跨窗口状态（自检用 —— 静态字段会跨 `Build()` 活）。</summary>
        public static void ResetReOpenProtectionForTest()
        {
            _protLastOpen = float.NegativeInfinity;
            _protDelayState = 0f;
            _protCount = 0;
        }

        // 每个窗口自己的时钟与逐卡状态
        float _openT;
        readonly float[] _flipT = new float[5];
        readonly int[] _flipKind = new int[5];
        readonly bool[] _swapped = new bool[5];
        readonly float[] _idleT = new float[5];
        readonly float[] _shakeT = new float[5];
        readonly float[] _shakeDur = new float[5];
        readonly Vector3[] _slotBase = new Vector3[5];
        Transform _anchorNode;
        Vector3 _anchorBase;

        // 稀有度相机抖（原版 `OverwriteCameraShakePreset.Play()`）—— 菜单层没有相机 ⇒ 抖**卡容器**（等价物）
        struct ShakeBurst { public float At, Amp, Decay, Z; }
        readonly List<ShakeBurst> _bursts = new List<ShakeBurst>();
        float _fxClock;

        // ============================================================ `Tap to close` 的闪烁（A426）
        //
        // 判据（原版）：`Booster Pack Open Window / Tap to close` 的组件表 = **`TextMeshProUGUI,Localize,
        // UIGenericEventCatcher,BlinkGraphic`**（`python 工具/menu_dump.py bundle_menus_assets_all
        // "Booster Pack Open Window" --depth 4` 实读）—— 那颗 `BlinkGraphic` 挂在该节点**自己**身上，
        // `graphic` 指向**同一个 GameObject 上那颗 `Graphic`**（= 那段字本身；36/36 个实例都是这个形状）。
        // ⚠️ `Tap to discover`（同一 rect 的兄弟件）**没有**这颗件 —— 别顺手给它也接一颗。
        // `(blinkSpeed, colorVariation) = (1.0, 0.5)` = 原版 `.ctor` 的两个立即数（36 个实例没人覆盖过）。
        // 公共件 = `Shell/BlinkGraphic.cs`（A315 建）—— ⛔ 别在这里再写一份公式。

        /// <summary>`Tap to close` 那段字上的 `BlinkGraphic`。`null` = 还没建过 ⇒ **没接**。**自检读它**。</summary>
        public BlinkGraphic Blink { get { return _blink; } }
        BlinkGraphic _blink;

        /// <summary>自检用：`Tap to close` 那颗字（闪烁写的就是它的 `color`）。</summary>
        public Label CloseLabel { get { return _closeLabel; } }
        Label _closeLabel;

        /// <summary>推一拍闪烁（由 `Update` 与 `AddTime` 同一拍调；批处理下没有帧循环 ⇒ 自检**直调**）。
        /// 🔴 **本窗就这一条时钟** —— ⛔ 别在别处再推一次（两处推 = 走两倍速）。
        /// <para>⚠️ **`Tap to close` 关着时不推**：原版那颗件挂在该节点自己身上，5 张没翻完时它不激活
        /// ⇒ `Update()` 根本不跑、**时钟冻住**，全翻开那一拍才从第 0 拍开始闪。我们挂窗根
        /// （理由见 `BlinkGraphic.cs` 文件头 ①）⇒ 这里**把这个可见后果收窄回原语义**。</para></summary>
        public void Tick(float dt)
        {
            if (_blink == null) return;
            if (_closeLabel != null && !_closeLabel.gameObject.activeInHierarchy) return;   // = 原版关着那一支
            _blink.Tick(dt);
        }

        /// <summary>接线（`Build()` 里调一次 ⇒ 重开一包 = 时钟归零）。
        /// `lb == null` ⇒ **解绑 + 静音**：那段字没建出来时上面刚报过一条，这里再报只是刷屏
        /// （⛔ 不是静默失败 —— 那种情况本来就没有东西可闪）。</summary>
        void BindBlink(Label lb)
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            _blink.LogTag = "[BoosterPack]";
            _blink.blinkSpeed = BlinkGraphic.DefaultSpeed;          // 36/36 实例都是这一对
            _blink.colorVariation = BlinkGraphic.DefaultVariation;
            if (lb == null) { _blink.Silent = true; _blink.Bind(null, null, null); return; }
            var l = lb;                                             // ⛔ 别直接捕形参（两个委托要活到下一次 `Build()`）
            _blink.Silent = false;
            _blink.Bind(l.gameObject, () => l.color, c => l.SetColor(c));
            _blink.Restart();                                       // = 原版 `Start()`：取原色 + 时钟归零、**当帧不写色**
        }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        // ============================================================ 开

        public static BoosterPackOpenWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Booster Pack Open Window");
            var win = go.AddComponent<BoosterPackOpenWindow>();
            win.type = WindowType.Fullscreen;                  // 实证 type = 0
            win.placement = WindowsPlacement.Canvas;           // 实证 windowsPlacement = 5（**不是 10 / 15**）
            win.closeOnEsc = false;                            // 实证 closeOnESC = **0**
            win.extraScaleSmallScreen = 1f;                    // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            // 🆕 **2026-10-25（执行代理 `X18`）**：成就 `Ach_15`（`ContainersOpen`）——
            //   判据 §B-3 #7「开包窗 `Open()` +1」。**一扇窗 = 开了一个包**：
            //   唯一生产入口 `ShopTabPage.OpenBoosterPack` 每次 `Create` 一扇新窗再 `OpenWindow`
            //   （⇒ 每次购买各进一次这里）；`Show()` 只换内容、不再 `Open`（⛔ 别挪到 `Show`，
            //   那样一包换页/重建会重复计数）。
            //   ⚠️ 原版 `containerType = 1`（只数卡包）—— 本窗**只用来开卡包**（文件头），
            //   所以「进了这扇窗」就等于「开了一个卡包」，不再多一道容器类型判据。
            //   ⚠️ 对局外动作 ⇒ `GainAchievementOutsideMatch`（没有模式闸；`tut=1` 与它无关）。
            ProfileData.GainAchievementOutsideMatch(ProfileData.IdAchContainersOpen, 1);
            Build();
        }

        /// <summary>开哪一件（照 `BoosterInfoPopup.Show` 那条先例：先 `OpenWindow` 再喂数据）。</summary>
        public void Show(int page, int index)
        {
            Page = page; Index = index;
            Build();
        }

        /// <summary>🆕 **2026-10-17（音效线 `W_G`）**：`Tap to close` / 整屏 `Collider` 那一下。
        /// <para>判据 = `BoosterPackOpenWindow__CloseButtonClick.c`：**先**播 `soundOnClose`
        /// （字段 `+0x80`，cue `Booster Pack Window Close` → clip `Cardshuffle out`，`MixerType.FX`），
        /// **再**起 `WaitAnimationForClose` 协程；协程末尾才调虚表 `0x1b8`（= `GameWindow.Close()`）
        /// ⇒ 关窗那一刻**还会再响一声默认关窗音**（`ButtonClick Back`）。两声是**原版就有的**，⛔ 别去重。</para>
        /// <para>⚠️ 我们不做 `Booster Window Close` 那段动画（文件头已标注）⇒ 两声响在同一拍。</para></summary>
        public void CloseFromTap()
        {
            WindowSounds.PlayFx(WindowSounds.BoosterCloseCue, "开包窗 `Tap to close`");
            Close();
        }

        // ============================================================ 建

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            PlayedFx.Clear();
            TimeAlive = 0f;
            HelperHidden = true;                 // `OnEnable`：一进来把 `Tap to discover` 关掉
            for (int i = 0; i < 5; i++) { Opened[i] = false; SlotNodes[i] = null; BackNodes[i] = null;
                                          FaceNodes[i] = null; UpBadges[i] = null; NewBadges[i] = null;
                                          BanIcons[i] = null; SlotHits[i] = null; VfxAnchors[i] = null; }
            DiscoverNode = null; CloseNode = null; CloseSurfaceHit = null; BgQuad = null;
            _closeLabel = null;                  // 🆕 A426：上一轮那颗字已经被上面清掉了 ⇒ 别留悬引用
            // 🆕 T19：动画状态机归零（重开一包 = 时钟从 0 开始）
            WindowOpenDone = false;
            ProtectionDelay = 0f;
            QuickOpens = 0;
            _openT = 0f; _bgShakeT = 0f;
            _fxClock = 0f;
            _bursts.Clear();
            _anchorNode = null;
            for (int i = 0; i < 5; i++)
            {
                CardStates[i] = (int)CardState.None;
                ContentNodes[i] = null;
                _flipT[i] = 0f; _flipKind[i] = 0; _swapped[i] = false;
                _idleT[i] = 0f; _shakeT[i] = 0f; _shakeDur[i] = 0f;
            }

            var offers = ShopData.Offers(Page);
            if (offers == null || Index < 0 || Index >= offers.Length)
            {
                Debug.LogWarning("[BoosterPack] 商品下标越界（page=" + Page + " idx=" + Index + "）⇒ 不铺内容");
                BindBlink(null);                 // 🆕 A426：这一轮**连字都没建** ⇒ 解绑 + 静音（上面刚报过一条）
                return;
            }
            var o = offers[Index];
            Army = ArmyOfArt(o.Art);
            Cards = RollPack(Army, unchecked(Page * 1000 + Index));
            CardsLeft = Cards.Length;

            // 🆕 **T19：防连开延迟**（原版 `BoosterPackOpenWindow.Initialize` 里那句
            //   `CardInBoosterPack.Initialize(..., fVar9)`，fVar9 就是 `CalculateReOpenProtectionDelay()`
            //   的返回值 —— 它**在 Initialize 里**算、`lastOpenTime` 也在那里更新）。
            //   公式与常量 → `BoosterPackAnim.StepProtection`；时间源可注入（`Now`）。
            {
                float now = Now();
                ProtectionDelay = BoosterPackAnim.StepProtection(now - _protLastOpen, ref _protDelayState, ref _protCount);
                _protLastOpen = now;
            }

            // 🆕 **2026-10-17（音效线 `W_G`）**：拆包那一声（原版 `BoosterPackVisualItem.openBoosterPack`，
            //   `BoosterPackVisualItem__Initialize.c` 末段 `SoundManager.Play2D(cue, **3 = Jingles**)`）。
            // 🔴 **落点是我们挑的**：原版这一声在「卡包实物被点开、卡片开始飞出」那一刻（`BoosterPackVisualItem`），
            //   而 `BoosterPackVisualItem`（卡包实物那件东西）我们**没建**（开包窗是直接铺 5 张卡的）⇒ 唯一对应的
            //   可听时刻就是本窗铺内容这一拍 ⇒ 落在 `Build()` 末尾之前（`Army` 已定、还没铺卡）。
            //   如实标注，⛔ 别把它当成原版的同一跳。
            //   阵营 → cue 的映射判据 → `WindowSounds.BoosterOpenCue`。
            WindowSounds.PlayJingles(WindowSounds.BoosterOpenCue(Army), "开包窗铺内容（拆包声）");

            // ① 背景：**纯色块**（原版 `backgroundImage` 的 `m_Sprite = 0`）——
            //    有传奇就换成 `thereIsALegendaryCardbackgroundColor`。
            var hasLegendary = false;
            for (int i = 0; i < Cards.Length; i++) if (RarityInt(Cards[i]) == 4) hasLegendary = true;
            BgQuad = MenuDraw.Rect(transform, CardArt.Solid(), BgR, "Booster pack Background", QShade,
                                   hasLegendary ? BgLegendary : BgNormal);
            if (BgQuad == null)
                Debug.LogError("[BoosterPack] 背景没建出来（`CardArt.Solid()` 取不到？）—— 这一扇会没有底色");

            // ② `Booster Animation Parent`（`m_LocalScale = 0.87626`）+ ③ `Cards`
            // 🔴 **不要在节点上真的设那个 0.87626** —— 我们的摆放全走「绝对像素矩形」，
            //    而 `MenuDraw.Local` 是「世界坐标 − 父节点位置」（**没除父级缩放**）⇒ 父级一带缩放，
            //    子件的 localPosition 就不再等于那个差值，整排卡会一起错位。
            //    ⇒ 0.87626 **已经折进 `CardK`**（`CardX[i] * AnchorScale` 那一处），节点保持 scale 1。
            AttachBgDistort(); var anchor = MenuDraw.Node(transform, "Booster Animation Parent", AnchorR);
            var cards = MenuDraw.Node(anchor, "Cards", AnchorR);
            _anchorNode = anchor;
            _anchorBase = anchor != null ? anchor.localPosition : Vector3.zero;

            for (int i = 0; i < 5; i++) BuildSlot(cards, i);

            // ⑤ `Tap to discover`（原版**开窗是关着的**）
            DiscoverNode = MenuDraw.Node(transform, "Tap to discover", HintR);
            var dl = MenuDraw.Text(DiscoverNode, HintR, DiscoverText, HintColor, "Text", HintPx, QHint, face: TmpFont.Face.LatinCardBody);
            if (dl != null) MenuDraw.AlignRight(dl, HintR);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版 `m_VerticalAlignment = 8192 (Capline)`
            //   （判据 = 文件头那两条读数；横向 `m_HorizontalAlignment = 4 (Right)` 已经在上一句里）。
            MenuDraw.SetVAlign(dl, Label.VAlign.Capline, HintR);
            DiscoverNode.gameObject.SetActive(false);

            // ⑥ `Tap to close`（+ 子 `Collider` 盖满整屏）—— 同样先关着
            CloseNode = MenuDraw.Node(transform, "Tap to close", HintR);
            var cl = MenuDraw.Text(CloseNode, HintR, CloseText, HintColor, "Text", HintPx, QHint, face: TmpFont.Face.LatinCardBody);
            if (cl != null) MenuDraw.AlignRight(cl, HintR);
            // 🆕 **2026-10-16（A712 阶段 2）**：同上（两条同档 `Capline`）。⚠️ `BindBlink(cl)` 排在后面 ——
            //   `SetVAlign` 会 `RefreshBounds()` 重排一次（**不改颜色**），`BlinkGraphic.Restart()` 读的是出厂色
            //   ⇒ 先后无所谓；这里照「先摆好、再接件」排。
            MenuDraw.SetVAlign(cl, Label.VAlign.Capline, HintR);
            _closeLabel = cl;
            // 🆕 A426：这段字在原版**是会呼吸的**（`Tap to close` 节点上挂着 `BlinkGraphic`）
            //   —— 此前我们一直画成静止的。接线放在这里：新建完那颗字、**在它被关掉之前**
            //   （`Restart()` 在这里读到的就是出厂色 `HintColor`）。
            BindBlink(cl);
            // 整屏那块（原版 `Collider` 的 `NonDrawingGraphic`）：**必须是带 `ImageQuad` 的**，
            // 否则 `PointerLayer` 收不到（`Shell/MenuDraw.DeckCell` 那颗裸节点就是栽在这上面，已记账）。
            // 🔴 **2026-10-04（A47 接线批）**：这一段原来是**手写** `ImageQuad.Create` + `WindowButton`
            //   （不经过 `MenuDraw.Hit`），档用的是**当时那个** `QCloseSurface`（= `QBase − 1` = 3169）。
            //   ⚠️ **2026-10-12（A408 现读订正，铁律 5）**：**那一号 2026-10-04 同日（A47）就删了**、
            //   本文件里**已无此名**（同口径见 `QShade` / `QCard` 声明上方那一整段）——
            //   **本行只是复述改前**，⛔ 别照它去找那个常量。
            //   按规矩收口到公共件 `MenuDraw.ShadeHit`，档改成**压暗层自己那一档** `QShade`(3170)
            //   —— 仍**严格低于**卡命中区的最低档 `QCard`(3171) ⇒ 卡照样能点（规矩 → `MenuDraw.ShadeHit`）。
            //   ⚠️ 摆法随之改变（**这是 `MenuDraw.Hit` 的既有摆法，别改**）：节点摆在父原点、
            //      quad 摆在矩形中心 —— 自检里原先那条量**节点位置**的断言因此改成量 quad（见 `ShopScene`）。
            var surf = MenuDraw.ShadeHit(CloseNode, CloseSurfaceR, QShade, QCard, CloseFromTap, "Collider");
            if (surf == null)
                Debug.LogWarning("[BoosterPack] 整屏那块 `Collider` 的命中区**没建起来**（`CardArt.Solid()` 取不到？）"
                                 + " —— 「点哪儿都关」这一路会失效（红线：不许静默失败）");
            else
                CloseSurfaceHit = surf.GetComponent<WindowButton>();
            CloseNode.gameObject.SetActive(false);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[BoosterPack] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            Debug.Log("[BoosterPack] 开了「" + o.Name + "」的开包窗：5 张卡 · 阵营 `"
                      + (string.IsNullOrEmpty(Army) ? "<全卡池>" : Army) + "` · 有传奇 " + hasLegendary
                      + " · 背景色 " + (hasLegendary ? "thereIsALegendaryCardbackgroundColor" : "常态 (1,1,1,1)"));
            HintGaps();
        }

        /// <summary>一格 `CardInBoosterPack UI n`。
        /// 🔴 层序照原版 `CardUI` 的**兄弟序**：`2DCard`（卡背) → `Card Ready for level up` → `New Card Badge` → `Ban Icon`。</summary>
        void BuildSlot(Transform parent, int i)
        {
            float cx = 960f + CardX[i] * AnchorScale;
            float cy = 540f;
            float w = CardW * CardK, h = CardH * CardK;                 // 卡本体 276.90 × 440.77
            float bx = ContainerW * CardK * 0.5f;                        // 容器半宽（角标锚点用）

            var slot = MenuDraw.Node(parent, "CardInBoosterPack UI " + (i + 1),
                                     new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f));
            SlotNodes[i] = slot;
            _slotBase[i] = slot != null ? slot.localPosition : Vector3.zero;
            int q0 = QCard + i * CardStride;

            // 🆕 **T19：`Content`**（原版 `CardInBoosterPack > Content`，矩形 = 容器 2.5437×3.3686 卡单位）。
            //   🔴 **翻牌翻的就是这一层**：三条 `Card Open *` clip 的 `m_RotationCurves` / `m_ScaleCurves` /
            //   `m_AnchoredPosition` 全部挂在 `path = Content` 上（`CardInBoosterPack.cardAnimation` 字段实读）。
            //   ⚠️ 粒子挂点（`VfxAnchors`）**留在 slot 上**：原版 `Instantiate(particles, card.transform)`
            //   挂在卡节点、**不在** Content 里 ⇒ 粒子不跟着翻。
            var content = MenuDraw.Node(slot, "Content",
                                        new PxRect(cx - ContainerW * CardK * 0.5f, cy - ContainerH * CardK * 0.5f,
                                                   cx + ContainerW * CardK * 0.5f, cy + ContainerH * CardK * 0.5f));
            ContentNodes[i] = content;
            CardStates[i] = (int)CardState.Back;          // 原版 `Initialize` 末尾 `ChangeState(1)`

            // ④a/④b 卡背（原版 `2DCard/Cardback Container` 的两件；`ChangeState(1)` 时开着）
            var backGo = new GameObject("Cardback Container");
            backGo.transform.SetParent(content, false);
            BackNodes[i] = backGo;
            var sdf = MenuDraw.Rect(backGo.transform, CardArt.CardBackSdf(Army),
                                    new PxRect(cx - BackSdfW * CardK * 0.5f, cy - BackSdfH * CardK * 0.5f,
                                               cx + BackSdfW * CardK * 0.5f, cy + BackSdfH * CardK * 0.5f),
                                    "Cardback Shadow SDF", q0);
            if (sdf == null) Debug.Log("[BoosterPack] 第 " + (i + 1) + " 格的 `Cardback Shadow SDF` 没画" +
                                       "（`CardArt.CardBackSdf(" + (Army ?? "null") + ")` 取不到 —— 那份 `_SDF` 掩码图没导）");
            else SdfMat(sdf, q0);   // 换原版 SDF shader（`_Outline` 悬停高亮要用它，见 `OutlineCardback`）
            var back = MenuDraw.Rect(backGo.transform, CardArt.CardBack(Army),
                                     new PxRect(cx - BackW * CardK * 0.5f, cy - BackH * CardK * 0.5f,
                                                cx + BackW * CardK * 0.5f, cy + BackH * CardK * 0.5f),
                                     "Cardback", q0 + 1);
            if (back == null)
                Debug.LogWarning("[BoosterPack] ⚠️ 第 " + (i + 1) + " 格的**卡背图取不到**（`CardArt.CardBack("
                                 + (Army ?? "null") + ")`）⇒ 那一格翻之前是**空的**。阵营 `" + (Army ?? "<空>")
                                 + "` 的默认卡背在 `Resources/Cardbacks.json` 里对不上，或图没跑 `import_original_art.py`");

            // ④c 卡面：`2DCard`（= `CardView`）。**先关着**，翻开才开（`ChangeState(1)` 是 ToggleCardBack(true)）。
            var faceGo = new GameObject("2DCard");
            faceGo.transform.SetParent(content, false);
            FaceNodes[i] = faceGo;
            if (Cards != null && i < Cards.Length)
            {
                var cd = BattleDriver.ToCardData(Cards[i], Cards[i].Faction);
                var v = CardView.Create(faceGo.transform, cd, "CardUI");
                if (v != null)
                {
                    v.gameObject.SetActive(true);
                    v.SetPose(MenuDraw.Local(faceGo.transform, cx - w * 0.5f, cy - h * 0.5f,
                                             cx + w * 0.5f, cy + h * 0.5f),
                              0f, h / (CardView.Height * 108f));
                    v.SetData(cd);
                    v.SetFace(CardFace.Full);
                    // 🔴 队列要**覆盖 CardView 内部那些写死的 3000**（它照原版材质来的）
                    //    —— 收口到 `CardFan.SetCardQueue`（和卡片详情窗同一份，别各写一遍）。
                    CardFan.SetCardQueue(v, q0 + 2);
                }
                else Debug.LogError("[BoosterPack] 第 " + (i + 1) + " 格的 `CardView` 建不出来");
            }
            faceGo.SetActive(false);

            // ④d `Card Ready for level up`（1.78×2.74 @ (0,+0.02)，父 = `CardUI`）
            float uw = UpW * CardK, uh = UpH * CardK, uy = cy - UpDy * CardK;
            var upR = new PxRect(cx - uw * 0.5f, uy - uh * 0.5f, cx + uw * 0.5f, uy + uh * 0.5f);
            var up = MenuDraw.Node(content, "Card Ready for level up", upR);
            // ⚠️ 图名用**导入后的文件名**（`CardArt.MenuUi` 不做「空格 → 下划线」转换）
            MenuDraw.Rect(up, Art("Card_Ready_For_Level_Up"), upR, "Image", q0 + 3);
            UpBadges[i] = up.gameObject;
            up.gameObject.SetActive(false);

            // ④e `New Card Badge`：锚 (0,.5)、pos (0.32,1.14) ⇒ 中心 = (容器左边 + 0.32, 容器中线 + 1.14)
            float bcx = cx - bx + BadgeAx * CardK, bcy = cy - BadgeAy * CardK;
            float bw = BadgeW * CardK, bh = BadgeH * CardK;
            var badgeR = new PxRect(bcx - bw * 0.5f, bcy - bh * 0.5f, bcx + bw * 0.5f, bcy + bh * 0.5f);
            var badge = MenuDraw.Node(content, "New Card Badge", badgeR);
            // `WF_Special offer_Value` 324×87 · 九宫 162,0,162,0 · Sliced · 色 (0.547,0.0876,0.0542,1)
            var badgeTex = Art("WF_Special_offer_Value");
            if (badgeTex != null)
            {
                // ⚠️ `MenuDraw.Nine` 的 `borderOutPx` 传**画布 px**（`ImageQuad.PixelsPerUnit` 已经是 108，别自己乘除）
                var nb = MenuDraw.Nine(badge, badgeTex, badgeR, new Vector4(162f, 0f, 162f, 0f),
                                       badgeTex.width, badgeTex.height, q0 + 3,
                                       new Color(0.547f, 0.0876f, 0.0542f, 1f));
                if (nb == null) Debug.LogWarning("[BoosterPack] `New Card Badge` 的九宫格没建出来");
            }
            // 子 `Text`：`sizeDelta 115.15×21.43` × **`localScale 0.01`**、`m_fontSize 27.7`（同样是缩放前的字号）
            // ⇒ 画出来的字高 = `27.7 × 0.01 × CardK` ≈ **36.65 px**（**别直接把 27.7 当画布 px 传**）
            float btw = 1.1515f * CardK, bth = 0.2143f * CardK;
            var btR = new PxRect(bcx - btw * 0.5f, bcy - bth * 0.5f, bcx + btw * 0.5f, bcy + bth * 0.5f);
            var bt = MenuDraw.Text(badge, btR, NewBadgeText, new Color(1f, 0.78f, 0f, 1f), "Text",
                                   27.7f * 0.01f * CardK, q0 + 3, face: TmpFont.Face.LatinCardBody);
            if (bt != null) MenuDraw.AlignLeft(bt, btR);          // 原版 `m_HorizontalAlignment = 1 (Left)`
            NewBadges[i] = badge.gameObject;
            badge.gameObject.SetActive(false);   // `SetRawCardData` 一进来就关（见文件头那张表）

            // ④f `Ban Icon`（**恒不显示**：`SetRawCardData` 末尾 SetActive(false)，翻牌链从不 ToggleBanned）
            //     锚 (.1,.5)-(.9,.5) ⇒ 宽 = 容器宽 × 0.8；`preserveAspect` ⇒ 实际画成正方形。
            //     `m_fontSize` 实读 **0.25**（`auto[0.25~72]`），节点**没有**缩放 ⇒ 画布 px = 0.25 × CardK ≈ 33.08。
            float banW = BanW * CardK, banSide = Mathf.Min(banW, BanH * CardK);
            var banR = new PxRect(cx - banSide * 0.5f, cy - banSide * 0.5f,
                                  cx + banSide * 0.5f, cy + banSide * 0.5f);
            var ban = MenuDraw.Node(content, "Ban Icon", banR);
            MenuDraw.Rect(ban, Art("40k_Cross_icon_cross_big_Banned_card"), banR, "Image", q0 + 3);
            var bt2 = MenuDraw.Text(ban, banR, BannedText, Color.white, "Banned Text", 0.25f * CardK, q0 + 3, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版这一颗的 `m_fontSizeBase` **原文**。
            //    判据（原版实读）：`/Booster Pack Open Window/…/Ban Icon/Banned Text`
            //    `m_fontSize 0.25` · `auto[0.25~72]` · **`m_fontSizeBase 36.0`**（逐站表 §二·3 #12；
            //    同节 #4 那颗 `Campaign Tab` 的 `Quantity` `base 61.23` 就是「原版也有真设过的」对照）。
            //    ⚠️ 量纲：本窗的 px 带 `CardK` 缩放（卡节点 `×0.87626`，见 本文件 的注释）
            //    ⇒ 这一格与上面 `min/max` 一样**也要乘 `CardK`**（否则差 1/0.876 ≈ 1.14 倍）。
            if (bt2 != null) bt2.SetAutoFitBox(LayoutSpace.Px(banSide), LayoutSpace.Px(banR.H),
                                               0.25f * CardK, 72f * CardK, 36f * CardK);
            BanIcons[i] = ban.gameObject;
            ban.gameObject.SetActive(false);

            // ④g 粒子挂点：原版把 `contentByRarities[i].particles` 实例化成**卡的子物体**，
            //     于是继承卡节点 `151 × 0.87626` 那个缩放 ⇒ 这里给一个同比例的挂点
            //     （`CardK / 108`：我们的世界是 108 px/单位，而卡那边是 `CardK` px/单位）。
            var vfx = new GameObject("EffectAnchor");
            vfx.transform.SetParent(slot, false);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localScale = Vector3.one * (CardK / (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight));
            VfxAnchors[i] = vfx.transform;

            // 命中区：**必须有 `ImageQuad`**（`PointerLayer.CollectHits` 取的是「按钮下第一个 ImageQuad」）
            // ⚠️ **两轴各一个比例**（原版那两个 `m_SizeDelta` 分量是绝对卡单位 ⇒ x 比 ≠ y 比，见 `HitRatioX/Y`）
            float hitW = w * HitRatioX, hitH = h * HitRatioY;
            var hit = MenuDraw.Hit(slot, "Hit",
                                   new PxRect(cx - hitW * 0.5f, cy - hitH * 0.5f, cx + hitW * 0.5f, cy + hitH * 0.5f),
                                   q0 + 4, null);
            if (hit != null)
            {
                int captured = i;
                var wb = hit.GetComponent<WindowButton>();
                SlotHits[i] = wb;
                if (wb != null)
                {
                    // 🆕 2026-10-17（W_G）：原版这颗 `2DCard/UI Collider` 是 `UIGenericEventCatcher`
                    //   （`CardInBoosterPack.uiCollider`）—— **它自己不播默认按钮音**；
                    //   那一下的声是 `CardInBoosterPack.clickSound`（cue `CardClick 2`，在 `Reveal` 里播）。
                    //   ⇒ 关掉通用按钮音，免得「一声变两声」。
                    wb.playClickSound = false;
                    // 🔴 **2026-10-17（T19 收尾）**：翻牌前后是**两个 handler**（原版同形）——
                    //   `UiColliderOnClick`（state 2 才生效）→ `CardInBoosterPack.CardClicked`（翻完才挂上）。
                    //   ⇒ 这里走同一个派发口，按当前状态分流。
                    wb.onClick = () => OnSlotClick(captured);
                    wb.onEnter = () => { if (!Opened[captured]) Hover(captured, true); };
                    wb.onExit = () => { if (!Opened[captured]) Hover(captured, false); };
                }
            }
        }

        /// <summary>卡的点击接收器比卡面**小一圈** —— 原版 `2DCard/UI Collider` 是拉伸锚 + `sd (−0.2,−0.44)`。
        ///
        /// <para>🔴 **判据 = 解包原件字段**（第一权威 · 2026-10-10 现读现核）：本窗名下共 **5 颗** `UI Collider`
        /// （五张卡各一颗），**5 颗逐字段相同**，且与 A156 核过的战斗/菜单侧那颗**逐位一致**：
        /// · `bundle_menus_assets_all/RectTransform/RectTransform_-8719435385506612189.json`
        ///   （其余四颗 pid：`6429976486133054499` / `876504314555284515` / `-2166550367152489437` / `1600754383137618979`）
        ///   `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)`（**拉伸锚**）· `m_Pivot (0.5,0.5)`
        ///   · `m_AnchoredPosition (0, −0.02)` · `m_SizeDelta (−0.2, −0.44)`
        /// · 父件 `2DCard`（`RectTransform_1205956342134619171.json`）：**`m_SizeDelta = 2.0927 × 3.3313`**（卡单位）。
        /// ⇒ 点击区 = **(2.0927−0.2) × (3.3313−0.44)** = **1.8927 × 2.8913** 卡单位 —— 原版那两个分量是
        ///   **绝对卡单位、不是百分比** ⇒ **x 比 ≠ y 比**：`1.8927/2.0927` / `2.8913/3.3313`。</para>
        ///
        /// <para>🔴 **2026-10-10（A183）就地订正**：这里原来只有一只 `const float HitRatio = 0.8679f`
        /// （那正是 **y** 那个比）**双轴同用** ⇒ x 两沿各多内缩 `276.8961 × (0.90443 − 0.8679) / 2` ≈ **5.06 px**
        /// （卡体 276.8961 宽）。⛔ **别再退回「一个比例双轴同用」**（同族先例 → `Core/CardFan.cs`）。</para>
        ///
        /// <para>⚠️ **没查清的（如实留白）**：原版那颗还有 `m_AnchoredPosition (0, −0.02)` 的**卡单位中心下移**
        /// （战斗侧折算 = 5px @ scale 250）—— **本窗折算成几 px 静态读不出**：本窗卡的渲染尺度是
        /// `Card2DController.cardScales` / `bigSizeMultiplier` 在**运行期**喂的（prefab 里只有 2.09×3.33 的设计值）
        /// ⇒ 见 `资料/普查产出_1009/查证V1_原版prefab四件.md` §五·1。**故此处仍按卡心居中，那个偏置没动**。</para>
        ///
        /// <para>⛔ 别与 `Tap to close/Collider`（`NonDrawingGraphic`，3853.09 × 2232.53 的**全窗吸收层**）混为一谈。</para></summary>
        const float HitRatioX = 0.90443f;   // (2.0927 − 0.2) / 2.0927 = 1.8927 / 2.0927
        const float HitRatioY = 0.86792f;   // (3.3313 − 0.44) / 3.3313 = 2.8913 / 3.3313

        // ============================================================ 交互

        /// <summary>卡上那一下点击的**分派**（🔴 2026-10-17 T19 收尾）：
        /// · `state == 2 Ready` ⇒ 走 <see cref="Reveal"/>（原版 `UiColliderOnClick`，翻牌）；
        /// · `state == 5 Open` ⇒ 走 <see cref="OpenCardDetail"/>（原版 `CardClicked`，翻开的卡再点开详情窗）；
        /// · 其余状态（1 开包中 / 3 翻牌中 / 4 抖动）⇒ **什么都不做**（原版那两个 handler 各自有状态守卫）。</summary>
        public void OnSlotClick(int i)
        {
            if (i < 0 || i >= 5) return;
            if (CardStates[i] == (int)CardState.Ready) { Reveal(i); return; }
            if (CardStates[i] == (int)CardState.Open) { OpenCardDetail(i); return; }
        }

        /// <summary>翻开的卡再点 ⇒ 开卡片详情窗。判据 = 原版 `CardInBoosterPack.CardClicked.c`：
        /// `CardDisplayWindow.Instance.ShowCard(card, …, true, …)` —— 菜单场景里的
        /// `CardDisplayWindow` 就是我们的 **`Shell/CardDetailPopup`**（见它的文件头：两扇同源、卡体同尺寸）。
        /// ⚠️ 原版还会在详情窗关闭时做 `ToggleCarUpgradeIfNeeded` / `ToggleNewCardBadge` 收尾
        /// （`CardDisplayWindowOnCloseWindow.c`）；那两件由 `CardDetailPopup` 自己的升级/新卡逻辑承担，
        /// 本窗**不再写第二份**。</summary>
        void OpenCardDetail(int i)
        {
            if (Cards == null || i < 0 || i >= Cards.Length) return;
            if (Manager == null)
            {
                Debug.LogWarning("[BoosterPack] 没有 `WindowsManager` ⇒ 开不了卡片详情窗（点的是第 " + (i + 1) + " 张）");
                return;
            }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(Cards[i]);                 // 同 `CollectionWindow.OpenCardDetail` 那条正路
            Debug.Log("[BoosterPack] 翻开后点第 " + (i + 1) + " 张 ⇒ 开卡片详情窗：" + Cards[i].Name);
        }

        /// <summary>点一张卡（原版 `CardInBoosterPack.UiColliderOnClick`）：
        /// **只有 `state == 2`（可交互）才生效**；先播 `clickSound`；有防连开延迟 ⇒ 先抖（state 4）再翻，
        /// 否则直接进入翻牌（state 3）。丑条判据 → `BoosterPackAnim.cs` 文件头。
        /// ⚠️ 开包 clip（0.6167 s）播完之前所有卡都是 state 1 ⇒ **点了没反应**（原版就是这样）。</summary>
        public void Reveal(int i)
        {
            if (i < 0 || i >= 5) return;
            if (CardStates[i] != (int)CardState.Ready) return;      // 原版：`state != 2` 直接 return
            if (Cards == null || i >= Cards.Length || CardsLeft <= 0) return;

            // 原版 `UiColliderOnClick` 第一句 = `clickSound.Play2D(MixerType.FX)`（cue `CardClick 2`）。
            // 稀有度音 + 粒子在 `ChangeState(3)` 里（有防连开时那是**抖完之后**才响）。
            WindowSounds.PlayFx(WindowSounds.BoosterCardClickCue, "开包窗点开第 " + (i + 1) + " 张");

            if (ProtectionDelay > 0f)
            {
                // 原版 `ChangeState(4)`：`DOShakePosition(duration = +0x98, ...)`，`OnComplete → ChangeState(3)`
                CardStates[i] = (int)CardState.Shaking;
                _shakeT[i] = 0f;
                _shakeDur[i] = ProtectionDelay;
                Debug.Log("[BoosterPack] 第 " + (i + 1) + " 张：防连开延迟 " + ProtectionDelay.ToString("F2")
                          + " s ⇒ 先抖（原版 `ChangeState(4)`）");
                return;
            }
            BeginFlip(i);
        }

        /// <summary>`ChangeState(3)`：开始翻这张（原版先 `window.CardOpened(this)` 记数、再播 clip/音/粒子）。
        /// <para>⚠️ `Opened[i]` 在**这一刻**就置真（原版 `cardsLeftToOpen` 在 `CardOpened` 里自减，
        /// 不等动画播完）—— 所以「`Tap to close` 出现」也发生在最后一张**开始翻**的那一拍。</para></summary>
        void BeginFlip(int i)
        {
            var kind = BoosterPackAnim.KindForRarity(RarityInt(Cards[i]));
            CardStates[i] = (int)CardState.Opening;
            _flipT[i] = 0f;
            _flipKind[i] = (int)kind;
            _swapped[i] = false;
            Opened[i] = true;
            // 原版 `ChangeState(3)` 里那句 `DOScale(记下的 scale, 0.2 s)` —— 把悬停的 1.1 倍收回去
            // （悬停时点卡，命中区随即关掉 ⇒ `onExit` 不一定会来，必须显式复位）。
            Hover(i, false);
            // 🔴 **2026-10-17（T19 收尾）**：命中区**不再关掉** —— 原版翻完之后这颗
            //   `2DCard/UI Collider` 会重新挂上 `CardClicked`（翻开的卡可以再点开详情窗）。
            //   翻牌进行中（state 3/4）由 `OnSlotClick` 直接忽略，等价于原来的「点不到」。
            // （原来这里有一句 `SlotHits[i].gameObject.SetActive(false)`，已删）

            // `CardOpened`（原版 `ChangeState(3)` **第一句**）：`Tap to discover` 淡出（我们直接关）+ 计数自减
            if (DiscoverNode != null) DiscoverNode.gameObject.SetActive(false);
            CardsLeft--;
            if (CardsLeft <= 0 && CloseNode != null)
            {
                CloseNode.gameObject.SetActive(true);      // 原版还会 DOScale ×1.1 / 0.4s OutBack（我们直接设值）
                Debug.Log("[BoosterPack] 5 张全翻开 ⇒ `Tap to close` 出现（原版 `CardOpened` 里 `cardsLeftToOpen` 归零那条）");
            }

            // 原版 `ChangeState(3)`：按稀有度取 `audioCue` → `Play2D(cue, 3 = Jingles)`
            WindowSounds.PlayJingles(WindowSounds.BoosterRarityCue(RarityInt(Cards[i])),
                                     "开包窗第 " + (i + 1) + " 张的稀有度音");
            // 原版 `Instantiate(contentByRarities[rarity].particles, card.transform)`
            PlayCardFx(i);
            StartShakes(RarityInt(Cards[i]));

            Debug.Log("[BoosterPack] 翻开第 " + (i + 1) + " 张：" + Cards[i].Name
                      + "（稀有度档 " + RarityInt(Cards[i]) + " · clip `" + kind + "` "
                      + BoosterPackAnim.FlipDuration(kind).ToString("F3") + " s · 还剩 " + CardsLeft + " 张）");
        }

        /// <summary>`EnableCardBackAnimEvent`：翻到 90° ⇒ 关卡背、露卡面（clip 的 `localScale.x → -1` 同一拍）。</summary>
        void SwapToFace(int i)
        {
            _swapped[i] = true;
            if (BackNodes[i] != null) BackNodes[i].SetActive(false);
            if (FaceNodes[i] != null) FaceNodes[i].SetActive(true);
        }

        /// <summary>`FinishedOpeningAnimEvent` + `ResetContentAnimation`：旋转/缩放/位移归零、补角标、
        /// 之后回 `Card Idle` 待机（原版 `CrossFade` 回当前 clip，那条 clip 是 8 s 循环的 `Card Idle`）。</summary>
        void FinishFlip(int i)
        {
            CardStates[i] = (int)CardState.Open;
            _idleT[i] = 0f;
            ResetContent(i);
            ApplyCardBadges(i);
            // 🆕 ③ `newCardSound`（原版 `_WaitForSpinAnimationEnd`：**拥有数 == 1** 才播 ——
            //   「这是你第一张」）。⚠️ 我们的 `CardProgress.Owned` 默认给足 ⇒ **这条恒假**
            //   （与 `New Card Badge 恒不显示` 是同一裁定，见文件头）。接线照判据留着 —— 口径一变它就自己响。
            if (Cards != null && i < Cards.Length && IsNewCard(Cards[i]))
                WindowSounds.PlayFx(WindowSounds.BoosterNewCardCue, "开包窗第 " + (i + 1) + " 张（第一张）");
        }

        /// <summary>把 `Content` 的旋转/缩放/位移归零（原版 `CardInBoosterPack.ResetContentAnimation`
        /// 是把 `contentAnimation` 那两跳写回「出厂 scale / rotation」——出厂都是恒等）。</summary>
        void ResetContent(int i)
        {
            var c = ContentNodes[i];
            if (c == null) return;
            c.localRotation = Quaternion.identity;
            c.localScale = Vector3.one;
            c.localPosition = Vector3.zero;
        }

        /// <summary>稀有度相机抖 —— 原版 `contentByRarities[rarity].shakes` 里每条 `OverwriteCameraShakePreset.Play()`。
        /// ⚠️ **等价物**：菜单层没有相机 ⇒ 抖**卡容器**（`Booster Animation Parent`）；包络/时长是我们挑的，
        /// 依据/缺口 → `BoosterPackAnim.cs` 文件头。</summary>
        void StartShakes(int rarity)
        {
            var list = BoosterPackAnim.ShakesFor(rarity);
            for (int k = 0; k < list.Length; k++)
            {
                if (list[k].Amplitude <= 0f) continue;       // 幅度取 preset 缺省值的那条：本地查不到 ⇒ 不猜
                _bursts.Add(new ShakeBurst { At = _fxClock + list[k].Delay, Amp = list[k].Amplitude, Decay = list[k].DecayTime, Z = 0f });
            }
        }

        /// <summary>推进动画时钟（由 `Update` 每帧调；**批处理没有帧循环 ⇒ 自检直调**，
        /// 与 `AddTime` / `Tick` 同一条口径）。
        /// 🔴 **本窗就这一条动画时钟** —— ⛔ 别在别处再推一次（两处推 = 走两倍速）。</summary>
        public void Advance(float dt)
        {
            if (dt <= 0f || Cards == null) return;
            _fxClock += dt; StepBgShake(dt);

            // ---- ① 开包 clip（`Booster Window Open`，0.6167 s）：卡的缩放 157.58151 → 151 ----
            if (!WindowOpenDone)
            {
                _openT += dt;
                float s = BoosterPackAnim.WindowOpenScale(_openT);
                for (int i = 0; i < 5; i++) if (SlotNodes[i] != null) SlotNodes[i].localScale = Vector3.one * s;
                if (_openT >= BoosterPackAnim.WindowOpenDuration)
                {
                    WindowOpenDone = true;
                    // 原版 `AnimationEndEvent`：5 张卡 `ChangeState(2)`
                    for (int i = 0; i < 5; i++)
                    {
                        if (SlotNodes[i] != null) SlotNodes[i].localScale = Vector3.one;
                        if (CardStates[i] == (int)CardState.Back) CardStates[i] = (int)CardState.Ready;
                    }
                    Debug.Log("[BoosterPack] 开包 clip 播完（" + BoosterPackAnim.WindowOpenDuration.ToString("F3")
                              + " s）⇒ 5 张卡可点（原版 `AnimationEndEvent` 把每张 `ChangeState(2)`）");
                }
            }

            // ---- ② 逐卡：抖动 / 翻牌 / 待机 ----
            for (int i = 0; i < 5; i++) StepCard(i, dt);

            // ---- ③ 稀有度相机抖（等价物：抖卡容器）----
            StepBursts();
        }

        void StepCard(int i, float dt)
        {
            var c = ContentNodes[i];
            if (c == null) return;
            int st = CardStates[i];

            if (st == (int)CardState.Shaking)
            {
                _shakeT[i] += dt;
                float d = Mathf.Max(1e-4f, _shakeDur[i]);
                float amp = BoosterPackAnim.ProtectionShakePx(_shakeDur[i]);     // px（我们的量纲，见 BoosterPackAnim）
                float u = Mathf.Clamp01(_shakeT[i] / d);
                float ph = _shakeT[i] / d * Mathf.PI * 2f * BoosterPackAnim.ProtectionVibrato;
                float ox = Mathf.Sin(ph) * amp;
                float oy = Mathf.Cos(ph * 1.37f + 0.6f) * amp * 0.5f;
                if (SlotNodes[i] != null)
                    SlotNodes[i].localPosition = _slotBase[i] + new Vector3(LayoutSpace.Px(ox), LayoutSpace.Px(oy), 0f);
                if (u >= 1f)
                {
                    if (SlotNodes[i] != null) SlotNodes[i].localPosition = _slotBase[i];
                    BeginFlip(i);                                                // 原版 `OnComplete → ChangeState(3)`
                }
                return;
            }

            if (st == (int)CardState.Opening)
            {
                _flipT[i] += dt;
                var kind = (BoosterFlipKind)_flipKind[i];
                float dur = BoosterPackAnim.FlipDuration(kind);
                float t = Mathf.Min(_flipT[i], dur);
                float yaw = BoosterPackAnim.FlipYaw(kind, t);
                float squash = BoosterPackAnim.FlipSquash(kind, t);
                float mx = BoosterPackAnim.FlipMirrorX(kind, t);
                float jx, jy;
                BoosterPackAnim.FlipJitter(kind, t, out jx, out jy);
                c.localRotation = Quaternion.Euler(0f, yaw, 0f);
                c.localScale = new Vector3(mx * squash, squash, squash);        // 镜像纠正（末帧 x = -1）
                c.localPosition = new Vector3(LayoutSpace.Px(jx * CardK), LayoutSpace.Px(jy * CardK), 0f);
                if (!_swapped[i] && _flipT[i] >= BoosterPackAnim.FlipRevealTime(kind)) SwapToFace(i);
                if (_flipT[i] >= dur) FinishFlip(i);
                return;
            }

            if (st == (int)CardState.Open)
            {
                // 翻开之后回 `Card Idle`（8 s 循环的 ±5° 摆动）
                _idleT[i] += dt;
                c.localRotation = Quaternion.Euler(0f, BoosterPackAnim.IdleYaw(_idleT[i]), 0f);
            }
        }

        void StepBursts()
        {
            if (_anchorNode == null) return;
            if (_bursts.Count == 0) return;
            float ox = 0f, oy = 0f;
            bool any = false;
            for (int k = 0; k < _bursts.Count; k++)
            {
                var b = _bursts[k];
                float local = _fxClock - b.At;
                if (local < 0f || local > BoosterPackAnim.CameraShakeDuration) continue;
                any = true;
                float u = local / BoosterPackAnim.CameraShakeDuration;
                float env = b.Decay > 0f ? Mathf.Clamp01(1f - u / b.Decay) : 1f;
                float w = Mathf.Sin(local * Mathf.PI * 2f * BoosterPackAnim.CameraShakeHz);
                ox += b.Amp * env * w;
                oy += b.Amp * env * Mathf.Cos(local * Mathf.PI * 2f * BoosterPackAnim.CameraShakeHz * 1.3f) * 0.35f;
            }
            _anchorNode.localPosition = any
                ? _anchorBase + new Vector3(LayoutSpace.Px(ox), LayoutSpace.Px(oy), 0f)
                : _anchorBase;
        }

        /// <summary>**快速开包（内部 API）**：跳过所有动画、把这一包一次性全翻开。
        /// <para>🔴 **入口判据未定 —— 原版 prefab 里【没有】这个按钮**：`Booster Pack Open Window` 的整棵树
        /// （`python 工具/menu_dump.py bundle_menus_assets_all "Booster Pack Open Window" --depth 6`）只有
        /// `Booster pack Background` / `Booster Animation Parent` / `Cards` / 5×`CardInBoosterPack` /
        /// `Tap to discover` / `Tap to close` / `Collider` —— **没有任何 skip / open-all 件**；
        /// MB 的字段表里也只有 `useQuickOpenProtection`（那是**防连开**、不是快速开包）。
        /// ⇒ 本方法**只做内部 API**（自检与将来的入口用），⛔ 不造按钮。</para>
        /// <para>语义（我们定的）：已翻的不动；没翻的直接置 `Open`（关卡背、开卡面、补角标、内容归零）；
        /// **不补播**点卡音/稀有度音/粒子（跳过动画的同一条语义），也不闪 `Tap to close` 的缩放。</para></summary>
        public void QuickOpenAll()
        {
            if (Cards == null) return;
            QuickOpens++;
            if (!WindowOpenDone)
            {
                WindowOpenDone = true;
                _openT = BoosterPackAnim.WindowOpenDuration;
                for (int i = 0; i < 5; i++)
                    if (SlotNodes[i] != null) { SlotNodes[i].localScale = Vector3.one; SlotNodes[i].localPosition = _slotBase[i]; }
            }
            for (int i = 0; i < 5; i++)
            {
                if (i >= Cards.Length) break;
                if (CardStates[i] == (int)CardState.Open) continue;
                if (!Opened[i])
                {
                    Opened[i] = true;
                    if (CardsLeft > 0) CardsLeft--;
                }
                if (SlotNodes[i] != null) SlotNodes[i].localPosition = _slotBase[i];
                // 🔴 2026-10-17（T19 收尾）：命中区**保持开着** —— 翻开的卡可以再点开详情窗（原版 `CardClicked`）。
                // （原来这里有一句 `SlotHits[i].gameObject.SetActive(false);`，已删）
                SwapToFace(i);
                ResetContent(i);
                ApplyCardBadges(i);
                CardStates[i] = (int)CardState.Open;
                _idleT[i] = 0f;
            }
            if (DiscoverNode != null) DiscoverNode.gameObject.SetActive(false);
            if (CloseNode != null) CloseNode.gameObject.SetActive(true);
            Debug.Log("[BoosterPack] 快速开包（内部 API）：5 张直接全翻（原版无此入口 —— 见 `QuickOpenAll` 注释）");
        }

        /// <summary>翻开之后那两个角标（判据逐条见文件头那张表）。</summary>
        void ApplyCardBadges(int i)
        {
            if (Cards == null || i >= Cards.Length) return;
            var c = Cards[i];
            bool canUp = ReadyForLevelUp(c);
            if (UpBadges[i] != null) UpBadges[i].SetActive(canUp);
            bool isNew = IsNewCard(c);
            if (NewBadges[i] != null) NewBadges[i].SetActive(isNew);
        }

        /// <summary>`BasicCardUI.ToggleCarUpgradeIfNeeded` 的判据是 `CardServices.CheckForCardUpgrade(card)`
        /// —— **服务端数据，本地没有** ⇒ 用工程既有的 `CardProgress.CanUpgrade`（与卡片详情窗同一份）。
        /// ⚠️ 拥有数那一半是「给足」⇒ 单看它**恒真**；
        /// 🔴 **但 2026-10-12 起整条【恒假】**：用户裁定「默认最高级」⇒ `CardProgress.Level` 缺省 = `MaxLevel`
        /// ⇒ `CanUpgrade` 的第一句 `l &lt; MaxLevel` 就返回 false（**这不是新缺陷，是那一裁定的必然后果**）。
        /// ⚠️ **改这一条之前先读文件头那一格**（含「层别删」那条）。</summary>
        public static bool ReadyForLevelUp(CardDef c)
        {
            return c != null && CardProgress.CanUpgrade(c.Id, c.Rarity);
        }

        /// <summary>`BasicCardUI.ToggleNewCardBadge(GetOwnedCount(card) == 1)` —— 这是你**第一张**。
        /// ⚠️ 我们的 `CardProgress.Owned` 默认就给足（≥ 卡组上限 + 9）⇒ 这一条**恒假**（见文件头）。</summary>
        public static bool IsNewCard(CardDef c)
        {
            return c != null && CardProgress.Owned(c.Id, c.Rarity) == 1;
        }

        /// <summary>`CardInBoosterPack.TryHighlightCardback(bool)`：悬停 ⇒ 整卡 ×**1.1**，补间 **0.3 s**。
        /// ⚠️ 批处理里没有帧循环（DOTween 补间跑不完）⇒ **直接设值**，把原版的补间时长记在这。
        /// ⚠️ 缩的是**卡那一层**（`2DCard` 及三个角标 + 卡背），**不缩命中区**（缩了指针一抖就掉出去）。</summary>
        public void Hover(int i, bool on)
        {
            if (i < 0 || i >= 5) return;
            float s = on ? HoverScale : 1f;
            var one = Vector3.one * s;
            ScaleCardLayers(BackNodes[i] != null ? BackNodes[i].transform : null, one);
            ScaleCardLayers(FaceNodes[i] != null ? FaceNodes[i].transform : null, one);
            ScaleCardLayers(UpBadges[i] != null ? UpBadges[i].transform : null, one);
            ScaleCardLayers(NewBadges[i] != null ? NewBadges[i].transform : null, one);
            ScaleCardLayers(BanIcons[i] != null ? BanIcons[i].transform : null, one); OutlineCardback(i, on);
        }

        /// <summary>把一层卡件补到 <paramref name="to"/>（原版 `TryHighlightCardback` 的
        /// `DOTween.Kill(gameObject)` + `DOScale(transform, …, **0.3 s**)`；常量 = <see cref="HoverTime"/>，
        /// `_DAT_1834b2dc8` 实读 0.3 · 倍率 `_DAT_1834b31f8` 实读 **1.1** ⇒ 与 <see cref="HoverScale"/> 一致）。
        /// <para>⚠️ 批处理 / 非播放态**直接设值**（DOTween 补间跑不完，同 `DraftModeWindow` 那五条动画的口径）。</para>
        /// <para>🔴 原版打的是**整个 `CardInBoosterPack` 根**；我们逐层打（⛔ 不缩命中区 —— 缩了指针一抖就掉出去，
        /// 这条差异见 <see cref="Hover"/> 的注释，不是这一笔改出来的）。</para></summary>
        static void ScaleCardLayers(Transform t, Vector3 to)
        {
            if (t == null) return;
            if (!Application.isPlaying) { t.localScale = to; return; }
            var go = t.gameObject;
            DG.Tweening.DOTween.Kill(go);
            DG.Tweening.ShortcutExtensions.DOScale(t, to, HoverTime);
        }

        /// <summary>播这一档的粒子（原版 `ChangeState(3)` 里 `Instantiate(contentByRarities[i].particles, card.transform)`）。
        /// 🔴 **只出声、不静默**：效果库没生成 / 里没有这一条时打警告并**记下来**（自检也看这个列表）。</summary>
        void PlayCardFx(int i)
        {
            if (Cards == null || i >= Cards.Length) return;
            string fx = CardFxFor(RarityInt(Cards[i]));
            var player = WarpforgeVFX.WarpforgeEffectPlayer.Play(fx, VfxAnchors[i], Vector3.zero, 1f, -1f);
            if (player == null)
            {
                Debug.LogWarning("[BoosterPack] 第 " + (i + 1) + " 张的翻牌粒子 `" + fx + "` **没播出来**"
                                 + " —— 要么效果库没生成（`-executeMethod EffectLibraryBuilder.Run`），"
                                 + "要么 `BoosterPackExporter.Run` 还没跑过。这一格会**只有卡、没有粒子**。");
                return;
            }
            PlayedFx.Add(fx);
        }

        /// <summary>4 个开卡包粒子 prefab 的名字。**判据 = `CardInBoosterPack.contentByRarities[]` 逐条实读**
        /// （`0/1 → Rarity 1` · `2 → Rarity 2` · `3 → Rarity 3` · `4/5 → Rarity 4`），名字本身出自
        /// `boosterpacks_assets_all` 里那 4 个 GameObject。
        /// 🔴 **表只此一份**：`WarpforgeArena1/Editor/BoosterPackExporter.cs` 的根表**转调这里**
        /// （编辑期程序集引用运行期程序集是允许的、反过来不行 ⇒ 表必须放在这一侧）。
        /// ⚠️ `工具/extract_missing_shaders.py` 的 `BOOSTER_GROUPS.roots` 里也有同 4 个名字 —— 那是 python，
        ///    只能各写一份（重打包要用），两处的注释互相指认。</summary>
        public static readonly string[] CardFxNames =
        {
            "Boosterpack Open Card Rarity 1",
            "Boosterpack Open Card Rarity 2",
            "Boosterpack Open Card Rarity 3",
            "Boosterpack Open Card Rarity 4",
        };

        /// <summary>稀有度档 → 粒子 prefab 名（分组见 `CardFxNames`）。</summary>
        public static string CardFxFor(int rarity)
        {
            if (rarity <= 1) return CardFxNames[0];
            if (rarity == 2) return CardFxNames[1];
            if (rarity == 3) return CardFxNames[2];
            return CardFxNames[3];
        }

        // ============================================================ 时间

        /// <summary>原版 `BoosterPackOpenWindow.Update`：累加 `timeAlive`；到 `timeToShowHelpText`（**10 s**）
        /// 且提示还藏着 ⇒ 把 `Tap to discover` 打开并淡入（我们直接开）。
        /// ⚠️ 批处理里没有帧循环 ⇒ 自检**直调本方法**（和 `WindowButton.Enter/Exit` 同一条口径）。</summary>
        public void AddTime(float dt)
        {
            if (dt <= 0f) return;
            if (TimeAlive >= HelpDelay) return;
            TimeAlive += dt;
            if (TimeAlive < HelpDelay || !HelperHidden) return;
            HelperHidden = false;
            if (DiscoverNode != null) DiscoverNode.gameObject.SetActive(true);
            Debug.Log("[BoosterPack] 静止 " + HelpDelay.ToString("F0") + " s ⇒ `Tap to discover` 出现"
                      + "（原版 `Update` 里 `timeToShowHelpText` 那条）");
        }

        /// <summary>三条时钟各走各的（⛔ 别合并）：`AddTime` = `timeToShowHelpText` 的累加器、
        /// `Tick` = `Tap to close` 的闪烁、`Advance` = 开包/翻牌/抖动。</summary>
        void Update() { AddTime(Time.deltaTime); Tick(Time.deltaTime); Advance(Time.deltaTime); }

        // ============================================================ 数据：这一包是哪 5 张

        /// <summary>商品主图 → 阵营。**判据 = 原版 SO 自己的字段**：`Booster Pack Ultramarines.json` 的
        /// `containerPreviewImage.m_SubObjectName = "40K_shop_offer_booster_UM"` ⇒ **那张图就是那个阵营的**。
        /// ⚠️ 这份映射**是我们把两张表对起来的**（我们的商品表里没有阵营字段）——
        ///    名字对不上时不算错、但要**出声**（`ArmyOfArt` 返回 null ⇒ 全卡池）。
        /// </summary>
        public const string ArtUM = "40K_shop_offer_booster_UM";
        public const string ArtSautekh = "40K_shop_offer_booster_Sautekh";
        public const string ArtSpaceWolves = "40K_shop_offer_booster_Space_Wolves";
        public const string ArtLeviathan = "40K_shop_offer_booster_leviathan";
        public const string ArtGeneric = "40K_shop_offer_booster_generic";

        public static string ArmyOfArt(string art)
        {
            switch (art)
            {
                case ArtUM: return "Ultramarines";
                case ArtSautekh: return "Sautekh";
                case ArtSpaceWolves: return "SpaceWolves";
                case ArtLeviathan: return "Leviathan";
                default: return null;                    // 含 `_generic`：**没有阵营** ⇒ 全卡池
            }
        }

        /// <summary>稀有度串 → 原版那个整数档（`contentByRarities[].rarity` / `card.rarity`）。
        /// 判据：`contentByRarities` 的分组是 `0/1 · 2 · 3 · 4/5`，
        /// 而 `BoosterPackOpenWindow.Initialize` 判「包里有传奇」用的是 `card.rarity == **4**`
        /// ⇒ **4 = legendary**；`CardRarityColorsSO.colors[]` 里 rarity 4 是橙色、3 紫、2 绿，逐档对上。</summary>
        public static int RarityInt(CardDef c)
        {
            if (c == null) return 0;
            switch (c.Rarity)
            {
                case "rare": return 2;
                case "epic": return 3;
                case "legendary": return 4;
                default: return 0;                       // common / special（special 是**卡框档位**、不是稀有度）
            }
        }

        /// <summary>摇这一包。规则来自**原版自己的出厂文案**（`Booster Pack Ultramarines` SO 的
        /// `itemDescriptionKey = Item_Description/BoosterPackExplanationUltramarines`，文案原文见
        /// `BoosterInfoPopup.DescSample`）：
        ///   · 「Contains **5** cards for the … army.」（与 prefab 里正好 **5** 个 `CardInBoosterPack` 互证）
        ///   · 「At least one of the cards is guaranteed to be **Rare or better**」
        /// 🔴 **哪 5 张是我们摇的**（原版走服务端 drop table，那两张 `DT *` 本地没导出来）。
        /// ⚠️ 用 `System.Random(seed)` **可复现**（原版 `Initialize` 里那句 `ListExtensions.Shuffle` 是无种子的）。
        /// 阵营池不够 5 张 / 没有 rare+ 时**出声**并退让，不静默。</summary>
        public static CardDef[] RollPack(string army, int seed)
        {
            var pool = CardDatabase.Load();
            var list = string.IsNullOrEmpty(army) ? pool : CardDatabase.OfFaction(pool, army);
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("[BoosterPack] ⚠️ 阵营 `" + army + "` 在卡池里一张都没有 ⇒ 退回全卡池");
                list = pool;
            }
            // 🔴 **2026-10-28（B93_Q8）：原版卡包池的判据 = `cantBeRandomlyCreated == 0`** —— 见
            //   `资料/完全复刻/判据/B93_Q8_开包过滤判定.md`。硬证据：原版 drop table 的 **329 条白名单**里
            //   `show=0` / `inv=10 / 5` / `cbrc=1` 一条都没有；而 4 个本地有掉落表的阵营，卡池 − 白名单
            //   **逐张 == 卡池里 `cbrc=1` 的那批**（22/18/12/10）。
            //   ⚠️ **别改成按 `showInCollectionOptions` / `inventoryOptions` 筛**：那会漏掉 39 张 `show=5` 的战场环境卡
            //   （`defence` · `inv=5` · `cbrc=1`），它们在原版 4 个阵营的白名单里同样 0 命中。
            {
                var filtered = new List<CardDef>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && !list[i].CantBeRandomlyCreated) filtered.Add(list[i]);
                if (filtered.Count == 0)
                    Debug.LogWarning("[BoosterPack] ⚠️ 阵营 `" + army + "` 剔掉 `cantBeRandomlyCreated` 之后一张不剩 ⇒ "
                                     + "本包退回未过滤（会开出原版不会出的卡，如实出声）");
                else list = filtered;
            }
            var rare = new List<CardDef>();
            var rest = new List<CardDef>();
            for (int i = 0; i < list.Count; i++)
                (RarityInt(list[i]) >= 2 ? rare : rest).Add(list[i]);
            if (rare.Count == 0)
                Debug.LogWarning("[BoosterPack] ⚠️ 阵营 `" + army + "` 里**没有 rare 及以上的卡** ⇒ "
                                 + "「至少一张 Rare or better」这条**这一包满足不了**（如实说，不换别的卡凑）");

            var rnd = new System.Random(seed);
            var pick = new List<CardDef>();
            if (rare.Count > 0) pick.Add(rare[rnd.Next(rare.Count)]);
            // 剩下 4 张从「除了已选那张以外的全部」里抽
            var others = new List<CardDef>();
            // 🔴 2026-10-28（B93_Q8 顺手发现 #1）：原来无条件读 `pick[0]` —— `rare.Count == 0` 时 `pick` 为空
            //   ⇒ `ArgumentOutOfRangeException`（而上面那条告警恰好写着"这种情况会发生"）。
            if (pick.Count > 0) { for (int i = 0; i < list.Count; i++) if (list[i] != pick[0]) others.Add(list[i]); }
            else others.AddRange(list);
            for (int k = 0; pick.Count < 5 && others.Count > 0; k++)
            {
                int j = rnd.Next(others.Count);
                pick.Add(others[j]);
                others.RemoveAt(j);
            }
            while (pick.Count < 5) pick.Add(list[rnd.Next(list.Count)]);   // 池子太小才会走到这（上面已出声）
            // 原版 `Initialize` 里那句 `Shuffle` ⇒ 顺序也打乱
            for (int i = pick.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var t = pick[i]; pick[i] = pick[j]; pick[j] = t;
            }
            return pick.ToArray();
        }

        /// <summary>把「原版有、我们没做 / 只能做等价物」的几处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[BoosterPack] 如实标注：① **卡包内容是我们摇的**（原版走服务端 drop table，"
                      + "`DT Ultramarines All(R2+)` —— ⚠️ 2026-10-28 订正（B93_Q8 现核）：原写「本地没导出来」**不成立**，34 份 `DT *` + 11 个 Booster 容器都在 `assets_full`（以前因**文件名带空格**漏检）；判别字段 = `cantBeRandomlyCreated`）；"
                      + "② **翻牌动画是代码补间**（判据 = 原版 7 条 clip 的关键帧，采样表在 `BoosterPackAnim`）"
                      + " —— 原版是 legacy `Animation` 播 clip，我们**不能**直接播（坐标系/缩放/锚点都对不上）；"
                      + "③ **相机抖是等价物**（菜单层没有相机 ⇒ 抖卡容器；包络/时长我们挑的；"
                      + "传奇第一条的幅度取 preset 缺省值、本地查不到 ⇒ 不抖）；"
                      + "④ **防连开（60 s 内连开）已实现**（`BoosterPackAnim.StepProtection`）；"
                      + "⑤ **快速开包只有内部 API `QuickOpenAll()`** —— 原版 prefab 里没有这个入口；"
                      + "⑥ `OpenCardbacks` clip 不接（已查清 = `DeckGeneralInfoDemo.cardbacksAnimation`，0.25 s · Once · 消费者 `PracticeModePopup.BuildCardbacks`，与开包窗无关）；"
                      + "⑦ 背景那条 `Booster Window - Background Shake On Open` **已接**（`_Distort_Scale` / `_Distort_Strength`；"
                      + "`Shader Graphs/Nebula` 已解出，但我们的图仍用 `Sprites/Default` ⇒ 今天无观感，两属性静默 no-op）。"
                      + "✅ 音效已接（2026-10-17 `W_G`；cue 本地都有）。详见 `Shell/BoosterPackOpenWindow.cs` 文件头"
                      + " 与 `Shell/BoosterPackAnim.cs` 文件头");
        }

        public string Dump()
        {
            int open = 0;
            for (int i = 0; i < 5; i++) if (CardStates[i] == (int)CardState.Open) open++;
            return "BoosterPack[" + Page + "/" + Index + "] 阵营 " + (string.IsNullOrEmpty(Army) ? "<全池>" : Army)
                   + " · 5 张 · 已翻 " + (5 - CardsLeft) + " · 收尾 " + open + "/5"
                   + " · 开包 clip " + (WindowOpenDone ? "播完" : (_openT.ToString("F3") + "s"))
                   + " · 防连开 " + ProtectionDelay.ToString("F2") + "s"
                   + " · 快速开包 " + QuickOpens + " 次"
                   + " · 粒子 " + PlayedFx.Count + " 次"
                   + " · 取不到的图 " + MissingArt.Count + " 张";
        }

        // ═══════════ 2026-10-27（B9_W20WIRE）：背景 `AnimateUIMaterialProperty` 接线 ═══════════
        // 判据：原版 `Booster pack Background` 上挂 `AnimateUIMaterialProperty`
        //   （`floatProperty1 = _Distort_Scale 0` · `floatProperty2 = _Distort_Strength 0`，两个属性都在
        //    `Shader Graphs/Nebula` 的 `m_Floats` 里 —— `B8_AS_特效侧施工单.md` §3·3 表 #2）。
        // 驱动 = `Booster Window - Background Shake On Open`（0.9167 s · `m_Legacy: 1` · 60 fps）的两条
        //   `floatProperty1.value` / `floatProperty2.value` 曲线，键值**现读**自
        //   `WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim`（逐键含 in/outSlope，见 `BgCurve1/2`）。
        // 🔴 为什么不是「挂 `Animation` 组件播那条 clip」：那份 `.anim` **不在任何 `Resources/` 下** ⇒ 运行期
        //   `Resources.Load<AnimationClip>` 取不到；而 legacy clip 里的 MonoBehaviour 曲线又只有 `Animation`
        //   组件推得动（本工程的 `AnimationClip` 运行期重建口 `BuildClip` 只绑 Transform/GameObject 两族）。
        //   ⇒ 走本工程既有做法（同 `BoosterPackAnim` 的「采样表 + 代码补间」）逐帧写 `floatPropertyX.value`。
        WarpforgeVFX.AnimateUIMaterialProperty _bgAnim;
        float _bgShakeT;
        static AnimationCurve _bgC1, _bgC2;

        /// <summary>把背景那张图挂上 `AnimateUIMaterialProperty`（两槽 = 原版那两个属性）。</summary>
        void AttachBgDistort()
        {
            if (BgQuad == null) return;
            var mr = BgQuad.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                Debug.LogWarning("[BoosterPack] 背景 quad 上没有 `MeshRenderer` ⇒ `AnimateUIMaterialProperty` **挂不上**"
                                 + "（`_Distort_*` 那条曲线不会走；⛔ 不静默）");
                return;
            }
            _bgAnim = WarpforgeVFX.AnimateUIMaterialProperty.Attach(mr,
                WarpforgeVFX.AnimateUIMaterialProperty.Slot("_Distort_Scale", 0f),
                WarpforgeVFX.AnimateUIMaterialProperty.Slot("_Distort_Strength", 0f));
        }

        /// <summary>`floatProperty1.value`（`_Distort_Scale`）的那条曲线 —— 键与切线逐字现读自那份 `.anim`。
        /// 🔴 切线必须逐键写回（端点相同、中段斜率不同 ⇒ 只比端点抓不到）。</summary>
        static AnimationCurve BgCurve1()
        {
            if (_bgC1 != null) return _bgC1;
            _bgC1 = new AnimationCurve(
                new Keyframe(0f, -2f, 0f, 0f),
                new Keyframe(0.5833333f, 0.2f, 0.8727274f, 0.8727274f),
                new Keyframe(0.9166667f, 0.4f, 0f, 0f));
            return _bgC1;
        }

        /// <summary>`floatProperty2.value`（`_Distort_Strength`）的那条曲线（同上，逐键现读）。</summary>
        static AnimationCurve BgCurve2()
        {
            if (_bgC2 != null) return _bgC2;
            _bgC2 = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.2166667f, 0.35f, 0f, 0f),
                new Keyframe(0.5833333f, 0.35f, 0f, 0f),
                new Keyframe(0.9166667f, 0f, 0f, 0f));
            return _bgC2;
        }

        /// <summary>逐帧把两条曲线的当前值写回 `AnimateUIMaterialProperty` 的两个槽。
        /// 播一次、**停在末键**（= legacy clip `m_WrapMode 0` / `m_StopTime 1` 播完后的行为）。</summary>
        void StepBgShake(float dt)
        {
            if (_bgAnim == null) return;                 // 没接上 / 背景被重建过（Unity 的伪 null）
            const float len = 0.9166667f;                // clip 末键时刻（60 fps × 55 帧）
            if (_bgShakeT < len) _bgShakeT = Mathf.Min(len, _bgShakeT + dt);
            var s1 = _bgAnim.floatProperty1;
            s1.value = BgCurve1().Evaluate(_bgShakeT);
            _bgAnim.floatProperty1 = s1;
            var s2 = _bgAnim.floatProperty2;
            s2.value = BgCurve2().Evaluate(_bgShakeT);
            _bgAnim.floatProperty2 = s2;
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        //  🆕 **2026-10-28（B88_B2 · 账 `锻造:40` 的卡背那半）：`Cardback Shadow SDF` 走原版 SDF shader**
        //
        //  判据（第一权威 = `d:/2/tools/decomp_full/CardInBoosterPack__TryHighlightCardback.c` ＋
        //        `Card2DController__ToggleHighlight.c`，2026-10-28 实读）：
        //   · `TryHighlightCardback(option)` 在 `currentState == ReadyToOpen` 时调
        //     **`card2DController.ToggleHighlight(option, isCardback: true, instant: false)`**；
        //   · `Card2DController.ToggleHighlight` 里 `isCardback == true` 走的是 **`cardBackShadow.material`**
        //     + `cardBackHighlightAndShadowColorChanger`（= 那颗 **`Cardback Shadow SDF`**，⛔ 不是 `Cardback` 本体），
        //     把 `cardHighlightMaterialID`（= `Shader.PropertyToID("_Outline")`，`Card2DController.cs:185`）那个
        //     颜色的 **alpha 写成 `option ? 1 : 0`**，再 `DoColorChange(…, 0.2f, instant)`。
        //   ⇒ 我们要有 `_Outline` 可写，这一层就必须是**原版 SDF shader**（`CardView.CardbackSdfMaterialBase`
        //     逐值抄过 `Card Backs SDF`），⛔ 不是 `Sprites/Default`。
        //   ⚠️ 做法照 `Delegate`/`CollectionWindow` 那一族：**每格一份 clone**（共享会让五格抢同一张掩码）。
        // ══════════════════════════════════════════════════════════════════════════════════

        /// <summary>把一格的 `Cardback Shadow SDF` quad 换成原版 SDF 材质（`_Outline.a` 平时 0）。
        /// ⚠️ 队列要在 `SetMaterial` **之后**再设一次 —— `ImageQuad.SetMaterial` 会把旧材质的 renderQueue
        /// 搬过来（同 `CollectionWindow` 那条注释记的坑）。</summary>
        static void SdfMat(ImageQuad q, int q0)
        {
            if (q == null) return;
            var baseMat = CardView.CardbackSdfMaterialBase();
            if (baseMat == null)
            {
                // `CardbackSdfMaterialBase` 自己已经报过「解析不到原版 shader」的警告 —— 这里只说明后果
                Debug.LogWarning("[BoosterPack] 卡背 SDF 材质取不到 ⇒ 悬停时**不会有**那圈描边"
                                 + "（原版 `ToggleHighlight(isCardback:true)` 打的就是这颗材质）。");
                return;
            }
            var m = new Material(baseMat);       // ⚠️ 每格一份：共享会让五格共用最后一张掩码
            m.mainTexture = q.Texture;           // SDF 掩码本身
            q.SetMaterial(m);
            q.SetRenderQueue(q0);                // `SetMaterial` 搬过来的是旧材质那份队列 ⇒ 显式设回本层
        }

        /// <summary>`CardInBoosterPack.TryHighlightCardback` 的**卡背描边那半**（缩放那半见 <see cref="Hover"/>）：
        /// 把该格 `Cardback Shadow SDF` 材质的 `_Outline.a` 补到 **1（悬停）/ 0（离开）**，时长
        /// **0.2 s**（= 原版 `Card2DController.HIGHLIGHT_COLOR_CHANGE_TIME` ＝ `CardHighlight.OutlineAnimTime`）。
        /// ⚠️ 批处理 / 非播放态**直接设值**（DOTween 补间跑不完 —— 同 <see cref="ScaleCardLayers"/> 的口径）。
        /// ⚠️ 目标节点**按名字在 `Cardback Container` 下找**（`transform.Find` 只找直接子件，而它正是直接子件）
        /// —— 这样**不必新增字段/数组**，也就不会挪动本文件里那些 `文件:行号` 账落点。</summary>
        void OutlineCardback(int i, bool on)
        {
            if (i < 0 || i >= 5 || BackNodes[i] == null) return;
            var t = BackNodes[i].transform.Find("Cardback Shadow SDF");
            if (t == null) return;                                   // 那一格没建 SDF（已出声）
            var mr = t.GetComponent<MeshRenderer>();
            var m = mr != null ? mr.sharedMaterial : null;
            if (m == null || !m.HasProperty("_Outline")) return;      // 材质没换成 SDF（`SdfMat` 已出声）

            var cur = m.GetColor("_Outline");
            float to = on ? 1f : 0f;
            if (!Application.isPlaying) { cur.a = to; m.SetColor("_Outline", cur); return; }

            DG.Tweening.DOTween.Kill(m);                              // 同一颗材质只留一条补间（快进快出不会打架）
            float from = cur.a;
            var tw = DG.Tweening.DOTween.To(() => from, v =>
            {
                if (m == null) return;
                var c = m.GetColor("_Outline"); c.a = v; m.SetColor("_Outline", c);
            }, to, CardHighlight.OutlineAnimTime);
            DG.Tweening.TweenSettingsExtensions.SetTarget(tw, m);
            DG.Tweening.TweenSettingsExtensions.SetEase(tw, DG.Tweening.Ease.Linear);
        }
    }
}
