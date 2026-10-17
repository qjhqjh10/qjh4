// CardDisplayWindow.cs — 卡牌放大展示窗（原版 `CardDisplayWindow`）· **战斗里的那处摆放**
//
// 原版出处：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` 第一节 + 运行时 dump
//   `BattlePrefab/.../FrontCanvas/Safe area FrontCanvas/Card Display Window`（**activeSelf=False**，平时关着）
//   + 场景 JSON `bundle_scenes_scenes_battlearena1/`（2026-09-28 逐节点复读）。
//   · 遮罩 `Menu Dark Background`  4574.6×2572.4，`color = (0,0,0,0.773)`
//   · `Card Display`               752×868，屏幕正中（x[584,1336] y[106,974]）
//   · `Card Display/Cards/CardUI (0..4)` —— **5 个卡槽**（脚本字段 `cardUIs[5]`）
//   · `Card Display/LowerSection`  y 921.34~1058.34：`FlavourTextBG`(1320×178) → `LoreText`
//     (1250×93.47 · fs32 · 居中) + `Voice Over Button`(88.655² @1654.7→1743.4)
//     —— **就这两件，没有第三件**（`Show Card Text` 那颗钮是**菜单版**才有的，见下面 A860）
//
// 🔴 **2026-10-17（A860）：战斗版比菜单版少两样，逐字段核过 —— 别照菜单版补**。
//   判据（13 个竞技场**逐份实读**，`bundle_scenes_scenes_battlearena{1,2,3,aeldari,astramilitarum,
//   blacklegion,darkangels,emperorschildren,genestealers,leviathan,sororitas,spacewolves,tauviorla}`）：
//   · **`options`（`CardDisplayOptions`）= `{m_FileID:0, m_PathID:0}` = null —— 13/13 全都是 0**；
//     `informationPanel` 同样 13/13 为 0（菜单版那颗是 `2331` / `2206`）。
//     `ShowCard.c:153-167` 的 `else` 支（`showOptions == true` 才走）**那句守卫就是**
//     `if (options == null || card.field_0x4c != 0) goto LAB_1807fbe68`（`:159`）⇒ **战斗侧那块面板永远不会出现**
//     （`ShowBattleCard` 传进来的 `showOptions = DisplayCardEffects(...)` 于战斗侧是**空转**）。
//     节点树也一致：本窗 `Card Display` 的孩子只有 4 个（`LowerSection` / `Cards` / `TutorialObjs` /
//     `EffectList`），**没有** `CardDisplayOptions` 的宿主，也没有 `Panel`（三块面板）、
//     `Wildcard Segment`、`Card Counter` —— 那四件挂在**菜单版**的 `CardDisplayer Menu For Menu` 上。
//   · **`showCardTextButton`（眼睛钮）= `{m_PathID:0}` = null —— 13/13 全都是 0**。
//     `ShowCard.c:85-96` / `CardSwapFinished.c:20-45` / `Open.c:72-83` 三处**都拿 `!= null` 把着**，
//     `Open` 那个 `AddListener(→ ToggleCardState)` 也不接 ⇒ **战斗侧点不到、也画不出这颗钮**。
//     节点面复核：全 `assets_full` 里 `Show Card Text` 这个 GameObject **只有 1 份**
//     （`bundle_scenes_scenes_mainmenuwarpforge`）；13 个竞技场 `GameObject/` 目录里
//     `*Text*` 名字全表**零命中**。`LowerSection` 的 `RectTransform` 也实读到**只有 2 个孩子**
//     （`FlavourTextBG` 1320×178 + `Voice Over Button` 88.655² @ x 734.33）—— 菜单那份是 **3 个**。
//     ⚠️ **别拿图标认钮**：眼睛那张图 `-7255197835733746773` 在竞技场里**是有的**，但它挂在
//     `ChooseCardMenu` 的开关钮上（`MonoBehaviour_4813` → `m_TargetGraphic` = `_4921`，一个 `Image`）
//     —— 同 `40K_melee_glow` 那次的教训（铁律 3）。
//   ⇒ 本窗**不建** `Show Card Text`。2026-10-17 那一轮先删了钮、留了三个桩给白名单外的调用点；
//     **2026-10-17（B8）三个桩（`HitEye` / `ToggleLore` / `LoreVisible`）连同最后一个调用点
//     （`Battle/BattleDriver.cs` 的 `HandleDisplayWindowClick`）已一并删干净** —— 全仓零残留。
//     📌 留下的知识（那颗钮**菜单版**才有的真语义）：`CardDisplayWindow.ToggleCardState` 翻的是
//     `showCardInfo` 并逐格调 `CardTextsController.ChangeState / ChangeToInitialState`，
//     ⚠️ **不是**「切整块 `FlavourTextBG`」；战斗版这一块「露不露」现在只看**有没有字**（见 `RefreshLore`）。
//     ⛔ **别把它建回来** —— `Editor/BattleScene.cs` 有断言钉着（节点名 / 那一格不吃点击）。
//
// 🔴 **2026-09-28 两条订正**（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十）：
//   ① **卡体 743 px 是错的** —— 那是**另一棵树**的尺寸：多卡展示窗
//      `Generic Multi Card Display Combat/Viewport/Content/CardUI Reference` 的 `m_LocalScale = 223.14`
//      （`RectTransform_3421.json`）。**本窗** 5 个槽的 `m_LocalScale` **全是 250**
//      （`RectTransform_2874/2772/2908/3170/2663.json`）、`Cards`→`Card Display`→`Safe area FrontCanvas`
//      一路父级 scale 都是 1 ⇒ **本窗卡体 = 523.175×832.825 px**，**与菜单版同一个尺寸**。
//   ② **原版是同一个脚本、两处摆放**（正本 §〇·2）⇒ 两边共用 `CardFan`（扇形位姿/分层/着色）
//      与 `CardWinBox`（矩形），**判据只此一份**，不再各写一套。
//
// **怎么打开**（原版行为，两条链 —— 2026-09-28 补上棋盘那条）：
//   · **手牌**：反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— 轻点卡牌就开关。
//     我们照这个做：手牌**按下→松开且几乎没移动**（`CardInteraction.TapThreshold`）就开关一次。
//   · **棋盘上的单位**（我方与对手**都给开**）：`CardCollider3D.OnPointerClick` →
//     `CardScript.OnTouchUpAsButton` 的 **C 段**（state 2/3/0x11）→ `BattleManager.DisplayCard`
//     → 本窗的 `ShowBattleCard`（**那条链里就调 `DisplayCardEffects`** ⇒ 有 buff 就出那块 `EffectList`）。
//     ⚠️ 棋盘上**拖拽**是另一件事（弹攻击三选一），见 `BattleDriver.BoardPress` 的注释。
//     判据全文 → `资料/待办判据_战场与战斗视图.md` §8b。
// **怎么关**（🆕 **2026-09-29 三条都接上了**；原来只做「再轻点同一张牌」，另外两条记的是「我们没接」）：
//   ① **再轻点同一张牌**（手牌 / 棋盘单位都走这一条）。
//   ② **点遮罩空白**（原版 `BackgroundCloseButton.OnPointerClick` → `CardDisplayWindow.OnBackgroundClick`
//      → `Close`，链有直证）—— 落点 = `BattleDriver.HandleDisplayWindowClick` 的最后那一支。
//   ③ **指针移开**（原版 `CardCollider.OnPointerExit` → `CardScript.OnTouchExit`，唯一守卫是
//      `displayingCardFlag`）—— 落点 = `BattleDriver.Update` 里那条每帧判据。
//      🔴 **这一条我们按实情收了口径**：守卫多加了「窗的地界」（`ContainsPointer`），
//      否则窗里的语音/眼睛钮永远点不到。**待实机核**（触屏是不是「抬手即关」产物里证实不了 ——
//      `StandaloneInputModule` 没反编译）。判据全文 → `资料/待办判据_战场与战斗视图.md` §8b。
//
// ═══════════════════════════════════════════════════════════════════════════════════════
// 🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：本窗已从【世界空间 quad】迁到【uGUI 子画布】**
//   判据与 8 步配方 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑨/⑩；
//   宿主映射 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`CardDisplayWindow` 那一行）：
//   原版 = **`Card Display Window`** @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，
//   **`OverHand` 层 / order 0 / 兄弟序 6/14**（`RT_3223` · `RT_3055.m_Children`）。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, "Card Display Window Canvas",
//     HudCanvas.SortingLayer.OverHand, 0)`（子画布**不带** `CanvasScaler` —— 全场景只有根上那一个）。
//   · **单位**：uGUI 档 **1 单位 = 1 设计 px**（推导 → `HudCanvas` 文件头）⇒ 本文件里 `CardWinBox.*`
//     那些 px 常量**原样**进 `sizeDelta` / `MakeText` 的 px 形参；⛔ **别再用 `MenuDraw.Rect` /
//     `MenuDraw.Text` / `MenuDraw.SetPxSize` / `Label.Create`** —— 那一族的单位是**世界单位**（= px ÷ 108），
//     迁过来的件用它会把整扇窗缩成 **1/108**，而画面只是「小得看不见」、**一个字都不报**。
//   · **坐标**：图/字一律**中心锚 + 父件中心为原点**（y 向上）；「整屏绝对 px → 父件局部」只有 `Rel(...)`
//     这一个换算口（⛔ 别在两处各推一次）。
//   · **层级**：不再靠 z / `renderQueue`，改由 (排序层 `OverHand`, `sortingOrder` 0, **兄弟序**) 决定；
//     兄弟序的排法见 `HudCanvas.Add`（**按 z 降序**）⇒ 下面 `Z*` 那一族常量**语义照搬**（值照旧能读）。
//   🔴 **没迁的两样（有意，⛔ 不是漏迁）**：
//     ① **卡体那一族**（`CardView` —— 5 个位姿槽 + 相关卡）**留在世界空间**
//        （`T09` §五·补⑧：手牌与场上卡那一批走第 5 步的 `Physics2D` 链路；`CardView` 也不在本件白名单）。
//        它的父链 = **本窗的世界壳**（`WorldShellName`）`→ Card Display → Cards`，原点/矩形/缩放
//        与迁之前**逐位相同** ⇒ `CardFan` 的位姿换算、`MenuDraw.Local` 那个共用换算口、以及自检里
//        `LayoutSpace.ToPixel(卡的世界点)` 那一族**一个字都不用改**（不改这一条就会踩
//        `BattleLogPanel` 报告 §6·3 那个坑：`CardView.SetPose` 收的是**世界单位**）。
//     ② 🔴 **`Menu Dark Background`（整屏压暗遮罩）也留在世界空间** —— 它是**全窗唯一必须画在卡体
//        【下面】**的一件，而卡体还在 `Default` 排序层、子画布在 `OverHand`（更高）⇒
//        **画布上的任何件都画在它【上面】**（判据 → `T09` §五·补⑤ ⑤ + `T09b` §A·0·3）。
//        挪上去 ⇒ 遮罩盖住卡体 ⇒ **本窗打开时只剩压暗（窗就没用了）**。
//        ⚠️ **这一条与同批的 `Battle/MultiCardDisplay.cs` 是同一口径**（那一扇的 `mcd_mask`
//           也是整屏 `Menu Dark Background`、也同样【有意不迁】，它文件头写着「在卡片迁上画布之前，
//           『必须画在卡片后面』的整屏件一个都不能上画布」）；⛔ 别单独把它挪上去。
//        ⛔ **与简报里那句「所有图与字都在子画布上」的差**：如实记在报告 §1/§2 —— 除这一件，
//           本窗的图与字**全部**在子画布上（**7 件图 = 语音钮 1 + 风味底图 1 + 效果底板 5** ·
//           **13 颗字 = 提示行 1 + `AffectedBy` 1 + `EnchanterText` 5 + `EffectText` 5 + `LoreText` 1**；
//           另有 4 个**不画东西**的分组节点）。要翻这一件**只需 3 行**（`Img(_root, …)` + `ZMask` 走兄弟序），
//           但那样本窗会当场失去用途 ⇒ 交调度台裁。
//        ✅ **代价对冲**：卡体在 `Default`/queue 3009+、遮罩在 `Default`/queue 3000 ⇒
//           **两个机制（排序层 / `renderQueue`）都指向「卡在遮罩之上」**，与今天逐位相同。
//     ⚠️ **（推论）因此有两个同名节点**（世界壳下有 `Card Display`/`Cards`，画布上也有一个 `Card Display`
//        装着 `LowerSection`/`FlavourTextBG`）—— 原版那一级**同时**装着 `Cards` 与 `LowerSection`，
//        而 `Cards` 不能上画布 ⇒ 只能拆开，如实记（判据见报告 §3 父链对照）。
//     ✅ **好消息（逐值算过）：画布上的件与卡体【一个都不重叠】** ⇒ 「画布在卡体之上」这条**在本窗看不见**：
//        · 卡体那一叠的**外接盒**（含转角；逐槽算过，极值都在槽 0 上）= x ∈ [698.4, 1221.6] ·
//          y ∈ [63.6, 896.4]（`CardFan`：中心 (960,480) + `523.175×832.825`，其余槽 x 全为负偏移、
//          尺寸只减不增 ⇒ 都落在里面）；
//        · 画布上的件：`EffectList` x ≥ **1358.1**（离卡体右缘 **136.5 px**）· 风味条 y ≥ **900.84**
//          （离卡体下缘 **4.4 px**）· 提示行 y = 1064 · 语音钮 x ∈ [1650.0, 1738.7]。
//        ⇒ **没有一处需要「谁压谁」的裁决** ✓（`EffectList` 在原版里本来就排在 `Cards` 之后 ⇒ 即使将来
//          卡体变大压过来，也是「画布在上」才对）。
//   · **命中那一路（`HitSlot`/`HitVoice`/`ContainsPointer`）当时一个字没改**：它们比的是
//     `LayoutSpace.ToPixel(世界点)` 与**设计 px** 常量 —— 与「那件建在哪一层」无关。
//     🔴 **2026-10-17（`T09` 卡体批派生件 `EW12`）起 `HitSlot` 改了**：它现在**转发**到
//     `CardView.ContainsUiCollider`（标架 = 卡自己 ⇒ 命中区**跟着卡的扇形角与倾摆转**；
//     矩形 = 原版 `2DCard/UI Collider` 的 `1.8927×2.8913 @ y −0.02`；⛔ 别再退回轴对齐框，
//     ⛔ 也别改用 `BoxCollider2D` 主面 —— 那是手牌/棋盘的送达件，见 `HitSlot` 的 doc）。
//     `HitVoice` / `HitBtn`（语音那颗钮）**仍按 px 判** —— 它比的是**不转的**那颗钮，本来就该按 px。
//     ⚠️ 那一族（含 `HitSlot` 的降级支）另有 `A990②` 的账：拿 `ToPixel` 读回来的 px 去比**字面设计 px**
//     只在 16:9 对；非 16:9 下画布会缩放、而世界那份不缩 ⇒ 两者分家（`T09` 第 4/5 步的账）。
// ═══════════════════════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using DG.Tweening;          // `OnComplete` 是它的扩展方法（换位收尾那一步）
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public class CardDisplayWindow : MonoBehaviour
    {
        /// <summary>整块窗放到 HUD **前面**（相机看 +Z，z 越小越近）。
        /// 比结算面板（−0.5）还靠前 —— 展示窗是「盖在所有东西上面」的那一层。
        /// 🔴 **2026-10-16（`T09` 批 3）**：uGUI 档里这两个数**不再进 `localPosition`** —— 它们只用来
        /// **登记兄弟序**（`HudCanvas.Add`：z 大的排在前面 = 画在**底下**，与旧那套同向）。</summary>
        const float ZMask = -2.0f, ZContent = -2.1f;
        /// <summary>卡格之间的 z 阶梯（**只是兜底**：真正决定前后的是渲染队列，见 `CardFan.QueueFor`）。
        /// ⚠️ 它只作用在**卡体那一叠**（世界空间、没迁）⇒ 本文件里**照旧有效**。</summary>
        const float ZSlotStep = 0.02f;
        /// <summary>遮罩 / 文字 / 按钮那一档队列（**低于卡格的 `CardFan.QCardBase`** ⇒ 卡永远画在它们之上）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：本件整层迁到子画布之后**只剩来历** —— 画布上的件没有独立队列
        /// （谁压谁由 (排序层, `sortingOrder`, 兄弟序) 定，见 `HudCanvas` 文件头），本文件**已经不再调**
        /// `SetRenderQueue`。⚠️ **值不变、也别删**：`Editor/BattleScene.cs` 的 `A1672` 那一段引着
        /// 「`CardDisplayWindow.QChrome 3000`」这个名字（它说的是「卡体那一族 ≤ 3000」那条假设）。</summary>
        const int QChrome = 3000;
        /// <summary>🔴 **画布上那几层【分组节点】登记的兄弟序档**（`Card Display` / `LowerSection` /
        /// `FlavourTextBG` / `EffectList`）。
        /// <para>旧那套它们一律是 `z = 0`（`MenuDraw.Node` 不写 z）—— 那时容器**不渲染**、z 无所谓。
        /// uGUI 档**整棵子树跟着容器排**（兄弟序是整个树的深度优先次序）⇒ 容器那一档决定的是
        /// 「它**这一整棵**」与**同一个父件下**的别的件谁先谁后。本窗 `_root` 下只有三样
        /// （`Card Display` 容器 / `EffectList` 容器 / 提示行 `cdw_hint`）⇒ 取 `ZContent` 让两个容器
        /// 与各自的**内容同档**；提示行再小 0.02 ⇒ 排最后 = 画在最上（它与风味底图那条带重叠）。</para>
        /// <para>⚠️ 整屏遮罩**不在画布上**（有意，见文件头）⇒ 它不参与这一档的账。</para></summary>
        const float ZGroup = ZContent;

        // ---- 🔴 **2026-10-16（`T09` 批 3）**：本窗各容器的【绝对中心】px（左上原点 y 向下）------
        //   ⚠️ **建件与读回共用这一份**（⛔ 别在两处各推一次）：它就是「整屏绝对矩形 → 父件局部坐标」
        //   那条换算（`Rel`）的 `cx0/cy0`。
        static readonly Vector2 ScreenC = new Vector2(960f, 540f);
        /// <summary>`Card Display`（画布上那一级，装 `LowerSection`）的中心。</summary>
        static readonly Vector2 CardDisplayC =
            new Vector2((CardWinBox.CardL + CardWinBox.CardR) * 0.5f, (CardWinBox.CardT + CardWinBox.CardB) * 0.5f);
        /// <summary>`LowerSection` 的中心。</summary>
        static readonly Vector2 LowerC =
            new Vector2((CardWinBox.LowerL + CardWinBox.LowerR) * 0.5f, (CardWinBox.LowerT + CardWinBox.LowerB) * 0.5f);
        /// <summary>`FlavourTextBG` 的中心（= `LoreBgT/LoreBgB` 那条带的中点）。</summary>
        static readonly Vector2 LoreBgC =
            new Vector2((CardWinBox.LowerL + CardWinBox.LowerR) * 0.5f, (CardWinBox.LoreBgT + CardWinBox.LoreBgB) * 0.5f);
        /// <summary>`EffectList`（`Elements` 那一块）的中心 —— 5 个槽的外接矩形中点。</summary>
        static readonly Vector2 FxC =
            new Vector2((CardWinBox.EffL + CardWinBox.EffR) * 0.5f,
                        (CardWinBox.EffRowCy[0] - CardWinBox.EffRowH * 0.5f
                       + CardWinBox.EffRowCy[CardWinBox.EffSlots - 1] + CardWinBox.EffRowH * 0.5f) * 0.5f);

        /// <summary>本窗那块 **uGUI 子画布**的名字（= 原版宿主名 + ` Canvas`，见 `T09` §五·补⑩ 第 1 步）。
        /// 🔴 **只此一份** —— `EnsureSubCanvas` 按名字幂等（自检/探针会把本窗建好几遍）。
        /// 判据 = `T09c` §一：`Card Display Window` @ `…/Safe area FrontCanvas`，`OverHand` / order **0**。</summary>
        const string SubCanvasName = "Card Display Window Canvas";
        /// <summary>🔴 **世界空间那层壳**的名字 —— 它装**两样**：整屏压暗（`_mask`）与卡体那一族
        /// （`Card Display` / `Cards`，见文件头「没迁的两样」）。⛔ **它不是原版件**（原版这一整棵
        /// 都在 `Card Display Window` 那一级，而那一级现在在画布上）。</summary>
        const string WorldShellName = "Card Display Window (world)";

        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：`_root` = 画布上那个**铺满子画布**的节点
        /// （= 本组件自己那个节点）；`_cardsShell` = **世界空间那层壳**（卡体那一族的父节点）；
        /// `_cards` / `_lower` / `_bg` 与迁之前同名同义（只有 `_cards` 那一支改挂世界壳下）。</summary>
        Transform _root, _cards, _lower, _bg, _cardsShell;
        /// <summary>本窗那块子画布（`OverHand` / order 0）—— 自检口。</summary>
        Canvas _subCanvas;
        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：`_mask`（整屏压暗）**有意留在世界空间**
        /// ⇒ 字段类型**保持 `ImageQuad`**（它没迁，类型上就不该装成 `HudPiece` —— 同
        /// `MultiCardDisplay._mask` 那条）。理由与代价 → 文件头「没迁的两样 ②」。
        /// ⚠️ 它是本窗**唯一**一件世界空间的图/字；`_voiceBtn` 已迁（类型换成 `HudPiece`）。</summary>
        ImageQuad _mask;
        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：类型 `ImageQuad` → `HudPiece`（现在建在子画布上）。</summary>
        HudPiece _voiceBtn;
        Label _lore, _hint;
        AudioSource _voiceAudio;

        // 「谁给我加的 buff」那一竖列（原版 `Card Display/EffectList`）—— **建一次**，之后只切 activeSelf + 改字
        Transform _fxRoot;
        Label _fxTitle;
        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：类型从 `ImageQuad[]` 换成 `HudPiece[]`（同 `_voiceBtn`）。</summary>
        HudPiece[] _fxBg;
        Label[] _fxWho, _fxWhat;
        Vector3 _fxTitleBase;
        Vector3[] _fxWhoBase, _fxWhatBase;

        /// <summary>每个**位姿槽**上的卡（下标 0 = 前台）。判据与真值 → `CardFan`。</summary>
        public readonly CardView[] SlotViews = new CardView[CardFan.Slots];
        readonly CardDef[] _slotDefs = new CardDef[CardFan.Slots];
        bool _swapping;
        /// <summary>开窗那一拍还在播没有 —— 我们这套的 `displayAnimation.isPlaying`（闸③，见 `SwapToFront`）。</summary>
        bool _opening;

        /// <summary>正在展示的那张卡（`CardData` 是 struct，不能拿 null 当「没有」⇒ 单独一个 bool）</summary>
        CardData _shown;
        bool _hasShown;

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }

        /// <summary>「谁给我加的 buff」现在露着几行（原版最多 5 槽；自检用）</summary>
        public int EffectRowCount { get; private set; }
        /// <summary>第 `i` 行「**谁给的**」（自检用）</summary>
        public string EffectWho(int i) { return (_fxWho != null && i >= 0 && i < _fxWho.Length && _fxWho[i] != null) ? _fxWho[i].Text : null; }
        /// <summary>第 `i` 行「**给了什么**」（自检用）</summary>
        public string EffectWhat(int i) { return (_fxWhat != null && i >= 0 && i < _fxWhat.Length && _fxWhat[i] != null) ? _fxWhat[i].Text : null; }

        /// <summary>第 `i` 行的**文字在底板前面吗**（z 更小 = 离相机更近；自检用）。
        /// 🔴 这条是 2026-09-28 踩出来的：`PlaceLeft` 拿「`SetZ` **之前**」的 `basePos` 重设位置，
        ///    每次改字都把 z 拨回 ≈0 ⇒ 被 −2.10 的底板压住 —— **图上只表现为「字暗一点」**
        ///    （量像素才看得出：白字峰值 190 而不是 255），**当时 940+ 条断言全绿**。
        ///    ⇒ 给它一条断言，别再靠眼睛。
        /// <para>🔴🔴 **2026-10-16（`T09` 批 3）就地换口（铁律 5）**：原来比的是
        /// **`transform.position.z`** —— 那是**世界空间那套**的判据；上画布之后每个件的
        /// `localPosition.z` 都是 0 ⇒ 它会恒判「一样远」＝**无信息量**。现在比 **`HudCanvas.OrderZOf`**
        /// （画布上的排位 z）；`HudCanvas.Add` 按 z **降序**排兄弟序 ⇒ **值小的仍然在前面**，
        /// 与本条要断的语义**逐字同向**。</para></summary>
        public bool EffectTextInFrontOfBg(int i)
        {
            if (_fxWho == null || i < 0 || i >= _fxWho.Length || _fxWho[i] == null) return false;
            float tz = HudCanvas.OrderZOf((RectTransform)_fxWho[i].transform);
            float bz = (_fxBg != null && i < _fxBg.Length && _fxBg[i] != null)
                     ? HudCanvas.OrderZOf(_fxBg[i].Rect) : float.MaxValue;
            return tz < bz;
        }

        /// <summary>第 `i` 行「谁给的」那行字的**左缘**（屏幕 px · 自检用）。
        /// 🔴 左对齐是原版口径（`m_HorizontalAlignment = 1`），而「居中」这个 bug **肉眼才能发现** ——
        /// 所以给它一条断言：**短句与长句的左缘必须落在同一个 x**（都贴框左缘）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）**：uGUI 档里 `Label.WorldW` **就是设计 px**
        /// （`T09` §七·8 —— 它读 `textBounds`，而 `TMP_UGUI` 的单位就是画布单位）⇒ 不再走
        /// `LayoutSpace.PxX` 那一跳；`localPosition.x` 是「相对 `EffectList` 中心」的 px
        /// ⇒ 加回 `FxC.x` 才是**整屏绝对 px**（与下面 `wantLeft` 同一个坐标系）。</para></summary>
        public float EffectWhoLeftPx(int i) { return LeftPx(_fxWho, i); }
        /// <summary>第 `i` 行「给了什么」那行字的左缘（屏幕 px）。</summary>
        public float EffectWhatLeftPx(int i) { return LeftPx(_fxWhat, i); }
        static float LeftPx(Label[] a, int i)
        {
            if (a == null || i < 0 || i >= a.Length || a[i] == null) return float.NaN;
            var lb = a[i];
            float wPx = lb.WorldW;                                        // uGUI 档：就是 px（见上）
            return lb.transform.localPosition.x + FxC.x - wPx * 0.5f;     // 块左缘 = 中心 − 半个块宽
        }
        /// <summary>位姿槽上有几张卡（= 1 主卡 + 相关卡数，最多 `CardFan.Slots`；自检用）</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）—— 原版 `cardInFront`。</summary>
        public CardView Card { get { return SlotViews[0]; } }
        /// <summary>前台那张的卡表项（没拿到 `CardDef` 时是 null）。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>开窗那一拍（`CardFan.OpenTime`，原版 legacy clip `Card Display Open`）还在播没有
        /// —— 我们这套的 `displayAnimation.isPlaying`（原版 `ChangeCardPosition` 的**闸③**）。</summary>
        public bool IsOpening { get { return _opening; } }
        /// <summary>位姿槽 `i` 上的卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < CardFan.Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < CardFan.Slots) ? _slotDefs[i] : null; }

        /// <summary>窗口上那行字（自检用）</summary>
        public string ShownTitle { get; private set; }
        /// <summary>窗口下面那段正文（自检用）</summary>
        public string ShownBody { get; private set; }
        /// <summary>语音按钮上一次播的是哪个文件（**没播成是 null**；自检用）</summary>
        public string LastVoiceFile { get; private set; }

        /// <summary>只有**前台那张**吃 tooltip（原版 `InitializeCardForDisplay` 的
        /// `Card2DController.ToggleTooltips(index == 0)`）—— 外面问这一条，不要自己数槽位。</summary>
        public static bool SlotHasTooltips(int i) { return i == 0; }

        public static CardDisplayWindow Create(Transform parent)
        {
            // ---- ① 世界空间那层壳（**整屏压暗 + 卡体那一族**，见文件头「没迁的两样」）------------
            //  判据 = 它与迁之前那个根**逐位同构**：整屏 `RectTransform` + `localPosition = 0`（世界原点）
            //  ⇒ 卡体的世界坐标、`MenuDraw.Local` / `CardFan` 的换算、以及那张整屏遮罩的 z/队列
            //  **一个字不用改**。
            var shell = new GameObject(WorldShellName, typeof(RectTransform));
            shell.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(shell.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);

            // ---- ② 子画布（原版宿主 = `Card Display Window` @ `…/Safe area FrontCanvas`）----------
            //  `OverHand` / order 0 / 兄弟序 6/14（判据 → `T09c` §一 那一行）。
            //  ⚠️ `EnsureSubCanvas` **按名字幂等**（探针/自检会把本窗建好几遍）⇒ 直接调它、别自己再包一层。
            HudCanvas.Ensure(LayoutSpace.Cam);          // 幂等（HUD 图族会先建；这里兜底）
            var cvRt = HudCanvas.EnsureSubCanvas(null, SubCanvasName, HudCanvas.SortingLayer.OverHand, 0);
            if (cvRt == null)
            {
                // ⛔ **不许静默退回世界空间 quad** —— 半迁移态（图在画布、字在世界）在 16:9 下看不出来，
                //    别的宽高比下会分家（`T09` §五·补⑦ 那条「只换半边比不换更坏」）。
                UnityEngine.Object.DestroyImmediate(shell);
                Debug.LogError("[展示窗] `OverHand` 子画布建不起来（根 canvas 没立？）⇒ **本窗这次不建**");
                return null;
            }

            // ---- ③ 窗本体：**挂在子画布下、铺满它** ----------------------------------------------
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名 prefab `Card Display Window` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读；它平时 `activeSelf=False`）⇒ 整屏矩形。
            // 🔴 **2026-10-16（`T09` 批 3）**：那一对锚点/`sizeDelta` **原样保留**（`FillParent` 写的正是
            //    `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`），只是量纲从「世界单位」变成「设计 px」；
            //    ⚠️ 它现在的 `rect` = **子画布的 rect**（`Expand` 档下屏比画布高时比 1080 大，见 `T09` §七·8）
            //    ⇒ `Editor/BattleScene.cs` §A218 那条的期望值要换成「画布矩形」（见报告 §4）。
            //    ⛔ **别写死 1920×1080**：`Expand` 档下写死的矩形盖不住两边。
            //    改坏法：删掉 `FillParent` ⇒ 根 `rect` 归零 ⇒ A218 那条红。
            var go = new GameObject("Card Display Window", typeof(RectTransform));
            go.transform.SetParent(cvRt, false);
            HudCanvas.FillParent((RectTransform)go.transform);
            var w = go.AddComponent<CardDisplayWindow>();
            w._cardsShell = shell.transform;
            w._subCanvas = cvRt.GetComponent<Canvas>();
            w.Build();
            w.Hide();
            return w;
        }

        /// <summary>本窗那块 **uGUI 子画布**（`OverHand` / order 0）—— 自检口：断它的 `overrideSorting` /
        /// `sortingLayerID` / **不带 `CanvasScaler`**（原版全场景只有一个、在根上）。
        /// ⚠️ 它是**本片真正的自证** —— 迁过去的件自己的断言只能证明「常量进得去」（`T09` §五·补⑥）。</summary>
        public Canvas SubCanvas { get { return _subCanvas; } }

        /// <summary>自检口：**装卡体那一族的世界壳**（`Card Display` / `Cards` 在它下面）——
        /// ⛔ 它不是原版件（见文件头），断言找卡体请走 `SlotView(i)` / 这个口，别拿 `transform` 去找。</summary>
        public Transform CardsShell { get { return _cardsShell; } }

        /// <summary>自检口：**窗本体在画布上的那个节点**（= `transform` 自己）——
        /// 只是给断言一个读起来明确的别名（同 `BattleLogPanel.PanelRect` 的形状）。</summary>
        public RectTransform WindowRect { get { return (RectTransform)transform; } }

        // ============================================================ 🔴 uGUI 档的两个换算/建件口
        //
        //  判据与逐项映射 → `T09` §五·补④ 那张表 + §五·补⑩ 的 8 步配方。本窗只用到这两条：
        //   · 位置：旧 `MenuDraw.Local` / `EndPanel.Pos`（世界单位）→ `Rel`（设计 px、中心锚）
        //   · 建件：旧 `MenuDraw.Rect` / `MenuDraw.Text` → `Img` / `Text`（**全部收 px**）

        /// <summary>设计 px（**左上原点、y 向下**、整屏绝对）→ 某个容器内的局部坐标
        /// （**中心原点、y 向上**）。`cx0/cy0` = 那个容器的**绝对中心**（px）。
        /// <para>与 `HudCanvas.PosPx` 是**同一条式子**（那个是「父件 = 整屏」的特例，见 `ScreenC`）——
        /// 本窗的容器（`_lower` / `_bg` / `_fxRoot`）都不是整屏 ⇒ **一律走这一个**。</para></summary>
        static Vector3 Rel(float xPx, float yPxFromTop, float cx0, float cy0)
        {
            return new Vector3(xPx - cx0, cy0 - yPxFromTop, 0f);
        }

        /// <summary>建一张 uGUI 图（`Image`、中心锚 + 中心枢轴、**按矩形拉满** = 原版 `m_PreserveAspect = 0`）。
        /// `pos` = **相对父件中心**的 px（y 向上）；`wPx/hPx` = 尺寸（px）；`z` = 兄弟序那一档。
        /// ⚠️ 贴图取不到 ⇒ 出声 + 返回 `null`（⛔ 不摆白块、也⛔ 不退回去建世界空间 quad）。</summary>
        static HudPiece Img(Transform parent, Texture2D tex, string name, Vector3 pos, float wPx, float hPx,
                            float z, Color? tint)
        {
            if (parent == null || tex == null)
            {
                Debug.LogWarning($"[展示窗] `{name}` 的**贴图取不到** ⇒ 这一件不建（⛔ 不摆白块）");
                return null;
            }
            // 锚点/枢轴在中心 ⇒ `anchoredPosition`（= `localPosition`）就是「相对父中心的偏移」
            var piece = HudCanvas.MakeImageInParent(parent, tex, name, 0f, 0f, wPx, hPx, z, tint ?? Color.white);
            if (piece != null) piece.Rect.localPosition = pos;
            return piece;
        }

        /// <summary>建一颗 uGUI 字（`TMP_UGUI`，经 `HudCanvas.MakeText` → `Label.CreateUgui`）。
        /// `pos` = **相对父件中心**的 px（y 向上）；`fontPx` = 原版那一颗的 `m_fontSize`（**画布 px**，
        /// `&lt;= 0` ⇒ 不设、按 `scale` 那一档）；`boxW/boxH` = 那一颗的**框**（px，`boxW > 0` 才开折行 + 自适应）。
        /// <para>🔴 **这些数全是 px** —— 旧那套 `MenuDraw.Text` 收的是同一个数、内部 `÷108` 才写进 TMP；
        /// 这里**直接吃 px**（⛔ 别顺手补一个 `LayoutSpace.Px(...)`：那会让字**小 108 倍**、**不报错**）。
        /// 与 `MenuDraw.TextCore` 的**唯一差别**就是这三处单位 + 不设渲染队列（画布上没有队列）。</para></summary>
        static Label Text(Transform parent, string name, string text, Vector3 pos, int scale,
                          float fontPx, float boxW, float boxH, float autoMin, float autoMax,
                          float z, Color color, Vector2 anchor)
        {
            if (parent == null) return null;
            var lb = HudCanvas.MakeText(parent, text, pos.x, pos.y, scale, color, anchor, z, name);
            if (lb == null)
            {
                // `Label.CreateUgui` 已经出过声（拿不到 TMP 字体资产时 uGUI 档**不许**退化成点阵）
                Debug.LogWarning($"[展示窗] `{name}` 这颗字建不出来 ⇒ 这一件不建（不静默）");
                return null;
            }
            if (fontPx > 0f) lb.SetGlyphHeight(fontPx);        // ⛔ 别除 108（uGUI 档 1 单位 = 1 px）
            if (boxW > 0f)
            {
                lb.SetWrapWidth(boxW);
                if (autoMin > 0f && fontPx > autoMin)
                    // 上限取**原版 `m_fontSizeMax`**（`autoMaxPx <= 0` 才退回 `fontPx` = 旧行为，同 `MenuDraw.TextCore`）
                    lb.SetAutoFitBox(boxW, boxH, autoMin, autoMax > 0f ? autoMax : fontPx);
            }
            return lb;
        }

        /// <summary>建一个**纯分组节点**（只有 `RectTransform`、不画东西）：矩形按**整屏绝对设计 px** 给
        /// （左上原点 y 向下），内部换算成「相对父件中心、y 向上」的局部坐标。
        /// <para>🔴 uGUI 档的尺寸**直接吃 px** ⇒ ⛔ 别用 `MenuDraw.Node`（那个走 `LayoutSpace.Px` = ÷108）。</para></summary>
        static Transform Node(Transform parent, string name, float x1, float y1, float x2, float y2,
                              Vector2 parentCenter, float z)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            HudCanvas.SetPxSize(rt, x2 - x1, y2 - y1);        // 中心锚 + 居中 pivot + px
            rt.localPosition = Rel((x1 + x2) * 0.5f, (y1 + y2) * 0.5f, parentCenter.x, parentCenter.y);
            HudCanvas.Add(rt, z);
            return rt;
        }

        void Build()
        {
            _root = transform;

            // 遮罩：**盖满整个可见区**（原版那张是 4574.6×2572.36，两倍屏还多，就是「铺满带余量」）。
            // ⚠️ 遮罩与提示行**建一次就留着**，开关只切 activeSelf —— 每次都重建的话，
            //    `Hide` 里把它们销毁了、`Show` 又不会重建，第二次打开就只剩一张光卡（踩过）。
            // 🔴🔴 **2026-10-16（`T09` 批 3）：这一件【有意不迁】—— 仍在世界空间，⛔ 别顺手挪上画布。**
            //    它是全窗**唯一必须画在卡体【下面】**的一件，而卡体还在 `Default` 排序层、子画布在
            //    `OverHand`（更高）⇒ 画布上的任何件都画在它**上面**（`T09` §五·补⑤ ⑤ · `T09b` §A·0·3）
            //    ⇒ 挪上去就是「遮罩盖住卡体 = 本窗打开时只剩压暗」。
            //    判据与代价逐条 → 文件头「没迁的两样 ②」（同批 `Battle/MultiCardDisplay.cs` 同一条口径）。
            //    ⚠️ 它挂**世界壳**（⛔ 不是 `_root` —— 那个在画布上、局部单位是设计 px）。
            //    矩形/队列/z **逐值照旧**：`MenuDraw.Rect`（世界空间 quad + 队列 `QChrome`）+ `SetZ(ZMask)`。
            if (_cardsShell != null)
            {
                _mask = MenuDraw.Rect(_cardsShell, CardArt.Solid(),
                                      new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB),
                                      "Menu Dark Background", QChrome, CardWinBox.ShadeColor);
                SetZ(_mask, ZMask);
            }
            else Debug.LogError("[展示窗] 世界壳没建出来 ⇒ **整屏压暗建不了**（本窗打开时不会压暗）");

            // 🔴 **2026-10-18（W12 · W6 顺手核）：「这行是我们自加的」成立，判据补齐** ——
            //    原版 `Card Display Window` 子树里**没有**「再点一下关闭」这样的节点
            //    （2026-10-18 逐节点走了一遍 `battlearena1` 那棵树；`Battle/` 那 93 条代码字面量里
            //     也没有对应的键，见 `d:/2/tools/il2cpp_out/stringliteral.json`）。
            //    ⇒ 铁律 11 例外①（原版没有）⇒ **保留中文、不改**，只把判据落在这里。
            //    ⚠️ 代价如实记：**英文档下这行会露中文**（没有原版键可接）。
            // 🔴 **2026-10-16（`T09` 批 3）**：`Label.Create(_root, …, EndPanel.Pos(960,1064,z), 2, …)` →
            //    `Text(...)`（**px**；位置 = `HudCanvas.PosPx(960,1064)`，即父中心下 524 px）。
            //    `scale: 2` 照旧 —— 两个空间里「一个 scale 档」都是 **7 px 的大写高**（`Label.CapUnitOfScale`）。
            //    ⚠️ 它比内容那一族**再靠前 0.02**：位置 (960,1064) 正落在风味底图那条带里
            //    （`LoreBgT..LoreBgB` = 900.84–1078.84），而 uGUI 档里**同 z 的先后由创建顺序定**
            //    （旧那套同 z 时先后本来就是未定义的）⇒ 给它一个明确的档，免得提示行被底图吃掉。
            //    📌 **这一格是有意收口、不是原版值**（原版这一行是我们自加的，见上一段）。
            _hint = Text(_root, "cdw_hint", "再点一下关闭", HudCanvas.PosPx(960f, 1064f), 2,
                         0f, 0f, 0f, 0f, 0f, ZContent - 0.02f,
                         new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f));

            // 结构照原版那棵树：`Card Display` → { `Cards`(5 个卡槽), `LowerSection` → `FlavourTextBG` → `LoreText` }
            // 🔴 **2026-10-16（`T09` 批 3）**：`Cards` **不在这棵树里**了 —— 它装的是**卡体**（世界空间那一族，
            //    见文件头「没迁的那一件」）⇒ 它挂在**世界壳**下（下面那一段），矩形/原点与迁之前逐位相同。
            //    画布上这一级**仍叫 `Card Display`**（原版那一级同时装着 `Cards` 与 `LowerSection` ⇒
            //    拆成两个同名节点是这次迁移的必然结果，如实记）。
            var cardDisplay = Node(_root, "Card Display", CardWinBox.CardL, CardWinBox.CardT,
                                   CardWinBox.CardR, CardWinBox.CardB, ScreenC, ZGroup);
            _lower = Node(cardDisplay, "LowerSection", CardWinBox.LowerL, CardWinBox.LowerT,
                          CardWinBox.LowerR, CardWinBox.LowerB, CardDisplayC, ZGroup);
            _bg = Node(_lower, "FlavourTextBG", CardWinBox.LowerL, CardWinBox.LoreBgT,
                       CardWinBox.LowerR, CardWinBox.LoreBgB, LowerC, ZGroup);

            // ---- 世界空间那一层：`Card Display` → `Cards`（**卡体那一族**，没迁）------------------
            //  ⚠️ 它下面那一级与世界壳**同名**（原版就叫 `Card Display`）—— 两处在不同的父件下，不冲突。
            //  矩形逐值照旧（`CardWinBox` 那一组），父件是「整屏 + 原点 + 无缩放」的世界壳
            //  ⇒ 卡体的世界坐标、`MenuDraw.Local` 的换算**与迁之前逐位相同**（这是本件敢不碰卡体代码的理由）。
            if (_cardsShell != null)
            {
                var cardsHolder = MenuDraw.Node(_cardsShell, "Card Display",
                                                new PxRect(CardWinBox.CardL, CardWinBox.CardT, CardWinBox.CardR, CardWinBox.CardB));
                _cards = MenuDraw.Node(cardsHolder, "Cards",
                                       new PxRect(CardWinBox.CardsCx - CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy - CardWinBox.CardsS * 0.5f,
                                                  CardWinBox.CardsCx + CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy + CardWinBox.CardsS * 0.5f));
            }
            else Debug.LogError("[展示窗] 世界壳（装卡体那一层）没建出来 ⇒ **卡体建不了**（本窗只剩字）");

            // 一颗圆钮：语音（图在工程里才建）。**只这一颗** —— 眼睛钮（`Show Card Text`）战斗版原版就没有
            // （`showCardTextButton = {m_PathID:0}`，13/13；节点也零命中），2026-10-17 删掉，见文件头 A860。
            // 🔴 **2026-10-16（`T09` 批 3）**：`MenuDraw.Rect(..., keepAspect: true)` → 自己按贴图比例内接
            //    （`ImageQuad.FitHeight` 的算式**一个字没动**，只是它现在给出的是 **px**），再交给 `Img`。
            //    落位 = 相对 `LowerSection` 中心 **(734.33, 0)** —— 与旧那套逐值相同，也正是原版那个
            //    `m_AnchoredPosition`（`Voice Over Button` 相对 `LowerSection` 中心）。
            //    ⚠️ 本工程那张图是 **128×128**（比例 1）⇒ 内接后就是那个 88.655² 的框本身。
            var voiceTex = CardArt.Ui("40k_UI_bt_voicelines");
            if (voiceTex != null)
            {
                float vAspect = voiceTex.width / (float)voiceTex.height;
                float vhPx = ImageQuad.FitHeight(CardWinBox.BtnS, CardWinBox.BtnS, vAspect);
                _voiceBtn = Img(_lower, voiceTex, "Voice Over Button",
                                Rel(CardWinBox.VoiceCx, CardWinBox.BtnCy, LowerC.x, LowerC.y),
                                vhPx * vAspect, vhPx, ZContent, null);
            }
            else
                Debug.Log("[展示窗] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮不建**（不摆白方块）");

            _voiceAudio = gameObject.AddComponent<AudioSource>();
            _voiceAudio.playOnAwake = false;
            _voiceAudio.spatialBlend = 0f;          // 2D —— 原版这个 source 也挂在 UI 上
            _voiceAudio.volume = 0.85f;             // ⚠️ 这个系数是**我们自己定的**（原版逐 source 的音量没查到）
            // 🆕 2026-09-19：挂到 `Voices` 通道 ⇒ 受设置面板那根「语音」滑块控制
            _voiceAudio.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;

            SetChrome(false);
            BuildEffectList();
        }

        // ============================================================ 「谁给我加的 buff」那一竖列
        //
        // 原版：`Card Display/EffectList` —— **只有这扇窗有**（卡的 prefab 上没有这棵树），出厂关着，
        // 整组**当且仅当**「≥1 条 effect 的文字非空」时 `SetActive(true)`（`DisplayCardEffects`，唯一写者）。
        // ⚠️ **建一次就留着、开关只切 activeSelf** —— 同遮罩那条教训：`Hide` 里销毁了、`Show` 又不会重建，
        //    第二次打开就只剩一张光卡（那次踩过）。
        void BuildEffectList()
        {
            _fxRoot = Node(_root, "EffectList",
                           CardWinBox.EffL, CardWinBox.EffRowCy[0] - CardWinBox.EffRowH * 0.5f,
                           CardWinBox.EffR, CardWinBox.EffRowCy[CardWinBox.EffSlots - 1] + CardWinBox.EffRowH * 0.5f,
                           ScreenC, ZGroup);

            // 🔴 **2026-10-16（`T09` 批 3）**：下面每一件的坐标都改成「**相对 `_fxRoot` 中心**的 px」
            //    （旧那套走 `MenuDraw.*` 的「整屏绝对矩形 → 父件局部」= `MenuDraw.Local`，两者同值）
            //    ⇒ 换算只有 `Rel(...)` 一处，`FxC` 是那个中心（**建件与读回共用**）。
            _fxTitle = Text(_fxRoot, "AffectedBy", TitleText,
                            Rel(CardWinBox.EffTitleCx, CardWinBox.EffTitleCy, FxC.x, FxC.y), 5,
                            CardWinBox.EffTitlePx, CardWinBox.EffTitleW, CardWinBox.EffTitleH, 19.3f, FxTitleAutoMax,
                            ZContent - 0.02f, Color.white, new Vector2(0.5f, 0.5f));
            if (_fxTitle != null)
            {
                _fxTitle.SetAlignLeft();
                // ⚠️ 旧那套这里还有一句 `SetZ`，而 `basePos` **必须在那之后取**（取早了会把 z 拨回去，
                //    见 `PlaceLeft` 的注释）。uGUI 档的 z 是**兄弟序**、不在 `localPosition` 里
                //    ⇒ 那个陷阱没了；`basePos` 就是建件时写下的位置。
                _fxTitleBase = _fxTitle.transform.localPosition;
                PlaceLeft(_fxTitle, _fxTitleBase, CardWinBox.EffTitleW);
            }

            _fxBg = new HudPiece[CardWinBox.EffSlots];
            _fxWho = new Label[CardWinBox.EffSlots];
            _fxWhat = new Label[CardWinBox.EffSlots];
            _fxWhoBase = new Vector3[CardWinBox.EffSlots];
            _fxWhatBase = new Vector3[CardWinBox.EffSlots];
            var bgTex = CardArt.Ui("40K_display");
            if (bgTex == null)
                Debug.LogWarning("[展示窗] `40K_display` 不在 `Resources/Art/ui/` ⇒ 效果清单**只画字、不画底板**（不摆白方块）");
            for (int i = 0; i < CardWinBox.EffSlots; i++)
            {
                float cy = CardWinBox.EffRowCy[i];
                if (bgTex != null)
                {
                    _fxBg[i] = Img(_fxRoot, bgTex, "EffectBg",
                                   Rel(CardWinBox.EffCx, cy, FxC.x, FxC.y),
                                   CardWinBox.EffBgW, CardWinBox.EffBgH, ZContent, null);
                }
                float wcy = cy + CardWinBox.EffWhoDy, tcy = cy + CardWinBox.EffWhatDy;
                // ⚠️ 文字比底板**再近 0.02** —— 旧那套是「同一个渲染队列里靠到相机的距离排序」；uGUI 档里
                //    它变成**兄弟序的一档**（`HudCanvas.Add` 按 z 降序 ⇒ 值小的排后面 = 画在上面），同向 ✓。
                _fxWho[i] = Text(_fxRoot, "EnchanterText", "",
                                 Rel(CardWinBox.EffCx, wcy, FxC.x, FxC.y), 5,
                                 CardWinBox.EffTextPx, CardWinBox.EffWhoW, CardWinBox.EffWhoH, 10f, FxTextAutoMax,
                                 ZContent - 0.02f, Color.white, new Vector2(0.5f, 0.5f));
                if (_fxWho[i] != null)
                {
                    _fxWho[i].SetAlignLeft();
                    // 🆕 **2026-10-16（A712 阶段 2 的尾巴 · W23 给的修法 · 主对话落）**：
                    //   原版这一颗的 `m_VerticalAlignment = 1024`（= `Bottom`），框 467.8×38.1（单行）。
                    //   ⚠️ **如实标注**：本件 `Text(...)` **传了 `boxW`（= `EffWhoW`，折行开着）** ⇒
                    //   长名字会折行；折行时 `Bottom` 的目标按**块高**折到末行（`Label.VOffsetBlockWorld` 管这条，
                    //   W25 2026-10-16 补），但**「我们折行后的形状」与「原版那一颗单行」是不是同一个形状没逐字核过** ——
                    //   原版那格的框宽 467.8 与我们的框宽也**没逐字比对**。照原版落 `Bottom`，形状差异如实记。
                    //   🔴 **2026-10-16（`T09` 批 3）**：框高这一格旧那套是 `LayoutSpace.Px(EffWhoH)`（世界单位），
                    //   uGUI 档**直接给 px**（`SetVAlign` 收的是「本标签所在空间的单位」）。
                    _fxWho[i].SetVAlign(Label.VAlign.Bottom, CardWinBox.EffWhoH);
                    _fxWhoBase[i] = _fxWho[i].transform.localPosition;
                }
                _fxWhat[i] = Text(_fxRoot, "EffectText", "",
                                  Rel(CardWinBox.EffCx, tcy, FxC.x, FxC.y), 5,
                                  CardWinBox.EffTextPx, CardWinBox.EffWhatW, CardWinBox.EffWhatH, 10f, FxTextAutoMax,
                                  ZContent - 0.02f, Color.white, new Vector2(0.5f, 0.5f));
                if (_fxWhat[i] != null)
                {
                    _fxWhat[i].SetAlignLeft();
                    _fxWhatBase[i] = _fxWhat[i].transform.localPosition;
                }
            }
            SetEffectRows(null);              // 出厂关着（原版 `m_IsActive = false`）
        }

        /// <summary>🔴 **A406（2026-10-12）：本窗四处 TMP 的 `m_fontSizeMax` / `m_fontSizeBase` 逐颗实读** ——
        /// 判据 = 节点本体（`bundle_scenes_scenes_battlearena1` 的 `GameObject/{AffectedBy,EnchanterText,
        /// EffectText,LoreText}.json` → 它 `m_Component` 里那颗 `TextMeshProUGUI`）：
        /// ```text
        /// AffectedBy    fs=0.4   min=0.2  max=0.4  base=36.0  ← 局部单位；×0.8854167×108.79123 = 画布 px
        /// EnchanterText fs=0.3   min=0.1  max=0.3  base=36.0
        /// EffectText    fs=0.3   min=0.1  max=0.3  base=36.0
        /// LoreText      fs=32.0  min=18.0 max=32.0 base=36.0  ← 已是画布 px（父链 scale = 1）
        /// ```
        /// ⇒ 上限（画布 px）= `AffectedBy` **38.5**（= `CardWinBox.EffTitlePx`）·
        ///   两个 `Effect*` **32.6**（= `CardWinBox.EffTextPx`）· `LoreText` **32.0**
        ///   —— ⚠️ **`LoreText` 是这一批唯一一处「上限 ≠ 标称」**：
        ///   `CardWinBox.LorePx = 35` 是**菜单版** `Card Detail Popup` 那颗的值（那边 `auto[10~35]`），
        ///   而**战斗这一份原版是 `auto[18~32]`** —— 两处摆放、两颗 TMP 的字段**真的不同**（铁律 5·c）
        ///   ⇒ 战斗窗三格都用战斗版：标称 `CardWinBox.LorePxBattle`(32) · 下限 **18** · 上限 `LoreAutoMax`(32)
        ///     （**2026-10-15 · `A2111` 表三·3/·4** 补齐后两格；之前只有上限是战斗版的）。
        /// <para>⚠️ **`m_fontSizeBase` 这一格【不传】是有判据的，不是没读到**：原版四颗的 base 都是 **36.0**
        /// —— 那正是 TMP 的**序列化默认值**（`TMP_Text.cs:473`，作者的 `fontSize` setter 在
        /// `m_enableAutoSizing=1` 时**不回写 base**）⇒ 运行期它进的是
        /// `m_fontSize = Mathf.Clamp(base, min, max)`（`TextMeshPro.cs:2149`），**四颗全被夹到 `max`**。
        /// 而本口 `basePx &lt;= 0` 的缺省语义 = 「base 取调用方那一档（`cur`）」——
        /// 本窗这四颗的 `cur` 分别是 38.5 / 32.6 / 32.6 / 32，**四颗全都 ≥ 各自的 max 或恰在 max 上**
        /// （`LoreText` 那颗是 **32 = max**）⇒ **与「原版把 36 夹到 max」逐位同效**。
        /// （换算：若照字面传 `basePx = 36`，在**我们**这套 px 口径里 36 落在 `[19.3, 38.5]` **区间内部**，
        ///  那反而与「原版被夹到 max」**不同** —— 单位不同的两个数不能直接对填。）</para></summary>
        const float FxTitleAutoMax = 38.5f, FxTextAutoMax = 32.6f, LoreAutoMax = 32f;

        /// <summary>标题那句 = **原版词条** `Battle/HUD/AffectedBy`（`Loc.T`）。
        /// 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：这里原来写
        /// 「**这是我们写的** —— 原版那条词条在**远端 I2 语言表**里，本地只有一条英文样例 `'Affected by:'`」
        /// —— **「这是我们写的」不成立**：键和英文原文**都在本地**（那颗 `Localize` 就在
        /// `bundle_scenes_scenes_battlearena1/MonoBehaviour_4578.json`，`mTerm = Battle/HUD/AffectedBy`，
        /// 同族 TMP 逐字 `Affected by:`；判据全文 → `Core/Loc.cs` 那一块）。
        /// **错因**：「本地只有一条英文样例」被读成了「拿不到词条」—— 其实那条样例**就是**原版文案。</summary>
        public static string TitleText { get { return Loc.T(TitleTerm); } }
        /// <summary>标题词条键 —— **只此一份**。</summary>
        public const string TitleTerm = "Battle/HUD/AffectedBy";

        /// <summary>一行效果：**谁给的** + **给了什么**。</summary>
        public struct EffectRow
        {
            public string Who, What;
            public EffectRow(string who, string what) { Who = who; What = what; }
        }

        /// <summary>把引擎的限时增益（`UnitState.TempBuff`）翻成两行文字。
        /// <para>🔴 **2026-10-18（第十五轮 · `G5`）就地订正**：这里原来写「原版那几句模板在服务端
        /// （`GameStaticData.GetEffectDesc`，本地只有一条样例）⇒ **这里的措辞是我们写的**」——
        /// **「措辞是我们写的」这句在【中文】上仍然成立，但「原版模板在服务端」这个理由不完整**：
        /// 模板的**键**在本地（`Battle/Effect/Change*OneTurn`，见 `Core/Loc.cs` 那一块），
        /// 只有**值**在远端 I2 表。本批把消费面也钉死了 —— 原版是
        /// `CardEffectItem.LoadEffect` → `GameStaticData.GetEffectDesc`（`decomp_full` 两个方法体亲读），
        /// 而那正对应我们这一块（展示窗的「受到以下影响：」两行）。</para>
        /// <para>⇒ 现在**属性那几行走原版键**（`Battle/Effect/Change{MeleeAttack,RangedAttack,Health,Armour}OneTurn`，
        /// 占位符 `{0}` 填**带符号的数值**）；**关键词那几行照旧走 `CardText`**（关键词有它自己那一族词条，
        /// ⛔ 不是 `Battle/Effect/*`）。认不出来的名字**原样打出来**（不静默、不自造）。</para>
        /// <para>⚠️ 中文列 **原来** = 改之前 `AttrZh` 的原话（`"{0} 近战"` + `+2` ⇒ 逐字等于原来的 `"+2 近战"`）
        /// ⇒ 当时「**中文档零变化**」。🔴 **2026-10-14（第二十七会话）就地订正**：这两条（`Battle/Effect/`
        /// `ChangeMeleeAttackOneTurn` 等）的中文列**已换成官方 zh-CN 表**的值
        /// （`获得 {0} 近战攻击一回合`）⇒ 这一族的中文档**不再零变化**（英文档照旧）。</para>
        /// 「谁给的」用 `SourceCard`（施加它的**真卡名**，不是那个恒为「战术卡」的 `Src` —— 两者**别混**）。</summary>
        public static List<EffectRow> RowsOf(IReadOnlyList<UnitState.TempBuff> buffs)
        {
            var rows = new List<EffectRow>();
            if (buffs == null) return rows;
            for (int i = 0; i < buffs.Count; i++)
            {
                var b = buffs[i];
                if (b == null) continue;
                string what;
                // 🔴 **2026-10-19（A1316②）就地更正**：这一行原来走 `CardText.KeywordZh`（**恒中文列**）
                //   ⇒ **英文档下这一行仍是中文**（同一个根：拿「恒中文」的取值口当显示口）。
                //   改成 `CardText.KeywordDisplay`（**跟语档**：中文档 = 中文列「先锋」、英文档 = 英文列
                //   `Vanguard`）—— 中文档**逐字不变**（`Loc` 的 `Card_Trait/*` 两列就是这两串）。
                //   同族的标题那一处（`Core/Tooltip.cs` 的 `TipText.Trait`）2026-10-10（A1084）已经这么改了
                //   ⇒ 本行是**同一族漏掉的那一处**。
                //   ⚠️ 表索引（`TipText.Trait` 查规则书那张按中文名索引的表）**仍然要用 `KeywordZh`** ——
                //   只有「显示口」才换（⛔ 别把两件事合成一件）。
                if (b.IsKeyword) what = CardText.KeywordDisplay(b.Name);   // 关键词：跟语档；⛔ 不是 `Battle/Effect/*`
                else what = AttrText(b.Name, b.Value);
                if (string.IsNullOrEmpty(what)) what = b.Name;      // 认不出来就**原样打出来**（不静默、不自造）
                string who = b.SourceCard;
                if (string.IsNullOrEmpty(who)) who = b.Src;         // 兜底；两个都空才留白（上面那半句还在）
                rows.Add(new EffectRow(who, what));
            }
            return rows;
        }

        /// <summary>属性名 → 原版词条键（**只此一份**）。`null` = 认不出来（调用方原样打名字）。
        /// <para>🔴 键名的判据 = `GameStaticData.GetEffectDesc` 的 `case` 分派（`decomp_full` 亲读，
        /// 那 25 条键逐条列在 `Core/Loc.cs`）：`"attack"` 那一档在 `case 1` 里按
        /// `*(char*)(struct+0x32..0x35)`（哪几个属性非零）**多选一**——
        /// 单属性那四条是 `ChangeMeleeAttackOneTurn` / `ChangeRangedAttackOneTurn` /
        /// `ChangeHealthOneTurn` / `ChangeArmourOneTurn`（我们这边一条 `TempBuff` 只带一个属性 ⇒ 正好对上那些单属性键）。</para>
        /// <para>⚠️ **`…OneTurn` 而不是无后缀那四条**：`TempBuff` 是**限时**增益（到回合边界必撤，
        /// 见 `UnitState.RevertBuffs`），而原版无后缀的 `ChangeX` 是**永久**改属性 ⇒ 别拿错档。</para></summary>
        static string AttrTerm(string name)
        {
            switch (name)
            {
                case "attack": return "Battle/Effect/ChangeMeleeAttackOneTurn";
                case "ranged": return "Battle/Effect/ChangeRangedAttackOneTurn";
                case "health": return "Battle/Effect/ChangeHealthOneTurn";
                case "armour": return "Battle/Effect/ChangeArmourOneTurn";
            }
            return null;
        }

        /// <summary>属性那半句 = 原版词条 + 把 `{0}` 换成**带符号的数值**（正值带 `+`）。
        /// 属性名认不出 ⇒ 返回 `null`（调用方原样打名字）。
        /// <para>⚠️ **数值为 0 时只印属性名、不印 `{0}`**：原版 `GetEffectDesc` 在同类字段全 0 时
        /// 直接回**空串**（`case 1` 那条 `return DAT_1842b80e0`），我们**故意比原版多说一个词**
        /// （印属性名比印空白对玩家有用）—— 如实标，⛔ 别当成原版行为。</para></summary>
        static string AttrText(string name, int value)
        {
            string term = AttrTerm(name);
            if (term == null) return null;
            string s = Loc.T(term);
            if (s.IndexOf("{0}", System.StringComparison.Ordinal) < 0) return s;   // 表里那条没带占位符 ⇒ 原样
            if (value == 0) return s.Replace("{0} ", "").Replace(" {0}", "").Replace("{0}", "").Trim();
            return s.Replace("{0}", (value > 0 ? "+" : "") + value);
        }

        /// <summary>摆/收这块（`rows` 为空 = 整组不出现 —— 原版就是「有 buff 才露」）。
        /// 🔴 **先把根亮起来再改字**：TMP 在**未激活**的对象里量不出尺寸（`SetAutoFitBox` 那条踩过）。</summary>
        void SetEffectRows(IReadOnlyList<EffectRow> rows)
        {
            if (_fxRoot == null) return;
            int n = rows == null ? 0 : Mathf.Min(rows.Count, CardWinBox.EffSlots);
            if (rows != null && rows.Count > CardWinBox.EffSlots)
                Debug.LogWarning($"[展示窗] 效果清单有 {rows.Count} 条，原版只有 {CardWinBox.EffSlots} 个槽 ⇒ **只画前 {CardWinBox.EffSlots} 条**（不静默）");
            EffectRowCount = n;
            _fxRoot.gameObject.SetActive(n > 0);
            if (_fxTitle != null) _fxTitle.gameObject.SetActive(n > 0);
            for (int i = 0; i < CardWinBox.EffSlots; i++)
            {
                bool on = i < n;
                if (_fxBg[i] != null) _fxBg[i].gameObject.SetActive(on);
                if (_fxWho[i] != null)
                {
                    _fxWho[i].gameObject.SetActive(on);
                    if (on) { _fxWho[i].SetText(rows[i].Who ?? ""); PlaceLeft(_fxWho[i], _fxWhoBase[i], CardWinBox.EffWhoW); }
                }
                if (_fxWhat[i] != null)
                {
                    _fxWhat[i].gameObject.SetActive(on);
                    if (on) { _fxWhat[i].SetText(rows[i].What ?? ""); PlaceLeft(_fxWhat[i], _fxWhatBase[i], CardWinBox.EffWhatW); }
                }
            }
        }

        /// <summary>把一行字摆成**左对齐**（原版 `m_HorizontalAlignment = 1`；`AffectedBy` 也是 Left）。
        /// ⚠️ **不能只调 `Label.SetAlignLeft()`** —— 那只设 TMP 的 `alignment`，而 `Label.RefreshBounds()`
        ///    每次都把**字形块整体居中到锚点**上 ⇒ 短句看上去还是居中（2026-09-28 实拍抓到：
        ///    「先锋」两个字正正地飘在底板中间）。这里按「**块左缘贴框左缘**」再推一把 ——
        ///    多行折行时 `WorldW` ≈ 框宽 ⇒ 位移 ≈ 0，正好就是原版的行为。
        /// 🔴 **`basePos` 必须是「`SetZ` 之后」的 `localPosition`** —— 本函数是**整份**重设位置（含 z），
        ///    传进一个 z 还没推过的 `basePos`，就会把字从 −2.12 拨回 ≈0 ⇒ **被 −2.10 的底板压住**。
        ///    症状极隐蔽：图上只是「**字暗一点**」（白字峰值 190 而不是 255），**940+ 条断言全绿**。</summary>
        /// 🔴 **`basePos` 的语义（`T09` 批 3 后）**：uGUI 档的 z 是**兄弟序**、不在 `localPosition` 里
        ///    ⇒ 旧那套「`basePos` 取早了会把 z 拨回去、字被底板压住」那个陷阱**没有了**
        ///    （踩过的症状：图上只是「字暗一点」、白字峰值 190 而不是 255、940+ 条断言全绿）。
        ///    现在传进来的就是建件时写下的那个 `localPosition`。</summary>
        static void PlaceLeft(Label lb, Vector3 basePos, float boxWpx)
        {
            if (lb == null) return;
            // 🔴 **2026-10-16（`T09` 批 3）**：uGUI 档里 `Label.WorldW` **就是设计 px**（见 `LeftPx`）
            //    ⇒ 旧那一对换算（`LayoutSpace.PxX(世界长)` 与 `LayoutSpace.Px(dx)`）**两处都删掉**。
            //    ⛔ 留一个就会把整行字推出框外（差 108 倍、**不报错**）。
            float wPx = lb.WorldW;
            float dx = Mathf.Max(0f, boxWpx - wPx) * 0.5f;
            lb.transform.localPosition = basePos + new Vector3(-dx, 0f, 0f);
        }

        /// <summary>把一件**世界空间 quad** 推到指定的 z（`MenuDraw.Rect` 建出来的东西 z 一律是 0）。
        /// 🔴 相机看 +Z、**z 越小越靠前** ⇒ 不推的话它会停在 z≈0（比 `ZMask = −2.0` 更靠后）。
        /// ⚠️ **本窗只剩一件走这条**：整屏压暗（`_mask`，它有意留在世界空间，见文件头）；
        ///    画布上的件**不许**用它（那些件的 z 是**兄弟序**、走 `SetZ(HudPiece, ·)`）。</summary>
        static void SetZ(ImageQuad q, float z)
        {
            if (q == null) return;
            var p = q.transform.localPosition; q.transform.localPosition = new Vector3(p.x, p.y, z);
        }

        /// <summary>把一个已建好的画布件挪到另一档**兄弟序**（= 旧那套的 `localPosition.z`，
        /// 语义同向：值大的在后面）。
        /// 🔴 **必须走 `HudCanvas.Reorder`** —— 建件口**自己已经登记过一次**，再用 `Add` 会在那一组的账里
        ///    留**两条**同一个件的记录（`HudCanvas.Group.Add` **不去重**，`T09` §七·11）。</summary>
        static void SetZ(HudPiece p, float z)
        {
            if (p == null) return;
            HudCanvas.Reorder(p.Rect, z);
        }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_voiceBtn != null) _voiceBtn.gameObject.SetActive(on);
        }

        // ============================================================ 命中（战斗侧不走 PointerLayer）

        /// <summary>点在**哪一格**上（−1 = 没点到任何卡）。**前台优先**：
        /// 前面的卡盖住后面的，所以从槽 0 往后找、先命中的就是它。
        ///
        /// <para>🔴 **2026-10-17（`T09` 卡体批派生件 `EW12`）：判据【转发】到卡体自己那一份** ——
        /// 标架 = 内层 `2DCard`（⇒ 命中区跟着卡的**扇形角与倾摆**转）。
        /// 🔴 **2026-10-17（`BSC62` 派生件 `W16`）就地换矩形**：本窗的矩形 = 原版 `2DCard` 的子件
        /// **`UI Collider`** `1.8927 × 2.8913 @ y −0.02`（卡单位）⇒ 调
        /// <see cref="CardView.ContainsUiCollider"/>（**唯一一份数**在 `Core/CardView.cs` 的
        /// 「第二套矩形」那一节）。⛔ **不是** `BoxCollider2D` 主面 `1.97 × 3.02 @ (0,0)` ——
        /// 那一套是原版**手牌/棋盘**的送达路径（`CardCollider` 实现 8 个 pointer/drag 接口），
        /// 展示窗若跟着它用，命中区会比原版**大 4.1% 宽 / 4.5% 高**（日志四边
        /// `102.5 / 857.5 / 713.75 / 1206.25` = `1.97×3.02×250` 逐位吻合那次就是这个成因）。
        /// ⛔ **也别退回「拿设计 px 比一个轴对齐矩形」** —— 那个框**不跟卡转**。
        /// 🔴 **`BSC62` 现核的送达路径（就地订正 `R5`/`EW12` 那句）**：本窗的点击在**原版由
        /// `2DCard/UI Collider` 送达** —— 该节点挂 `UIGenericEventCatcher`，`BasicCardUI.uiEventCatcher`
        /// （`+0xd0`）正指向它（`BasicCardUI__get_UIEventCatcher.c:5`），而
        /// `CardDisplayWindow.AddCardsListeners.c:25/30` 往它的 `OnClick` 上 **`AddListener`**；
        /// 该节点 `Image.raycastTarget = 1`，`UICollider.OnEnable` 把 `BoxCollider2D.size` 设成 `RT.rect`。
        /// ⚠️ **`R5` 原写「`UI Collider` 三个 UnityEvent 全空 ⇒ 它不是送达路径」—— 那条漏了
        /// 【运行时 `AddListener`】**，已推翻（判定 → `资料/普查产出_1017/D_新红11条诊断.md` A1）。
        /// 旧写法漏掉两样：
        /// ① **扇形角**（槽 1–8 = `CardFan._rot` 的 `2.510°…32.4°`）⇒ 轴对齐框与画出来的卡**对不上**：
        ///    最坏处 = θ 32.4°（槽 8）时**两块命中区的最大距离（Hausdorff）= 0.6273 卡单位**
        ///    （槽 8 自己那档 **86.4 px**、按前台档 156.8 px ≈ 卡身高度的 **18.8%**）·
        ///    **两块命中区的对称差 = 命中区面积的 38.2%**（两边各 19.1%）；槽 1（2.510°）
        ///    是 0.0624 卡单位 ≈ **14.5 px** / 4.6% —— 各档都随 θ 单调涨（逐档表 → `EW12` 报告 §④）。
        ///    🔴 **复算订正（本件现算）**：`EW9` 报告 §③③ 引的 `(w/2)·sinθ + (h/2)(cosθ−1) = 0.3014`
        ///    ① 用的是**卡身** `2.0927×3.3313`，而这里比的是**当时的命中矩形** `1.8927×2.8913`
        ///    （批② 起已换成 `1.97×3.02`；下面这组数按批① 旧矩形复算，**未按新矩形重算**）；
        ///    ② 而且**只是【纵向】**那一半（横向 = `(h/2)·sinθ + (w/2)(cosθ−1)` **更大**）。
        ///    按命中矩形的两个分量复算：纵向 **0.2820** · 横向 **0.6273**（= Hausdorff）⇒ 按大的记；
        /// ② **倾摆**（`AutoCardRotation` 的 ±10°，只写在 `_tiltRoot` 上）。
        /// 转发之后两样一起对上 —— 扇形角写在**卡根**、倾摆写在 `_tiltRoot`，后者是前者的子件。
        /// ⚠️ 连带把**开窗那一拍**也对上了：`PlayOpenAnimation` 补的是**卡自己的 `transform`**
        /// ⇒ 命中区跟着卡走（旧写法钉在扇形终态的位置上）。</para>
        ///
        /// <para>⚠️ **卡面没建出来那一格**（`SlotViews[i] == null`；`BuildFan` 已经报过一次）走
        /// <see cref="HitSlotNoView"/>：照旧拿 `CardFan` 的矩形判、并**出声一次**。
        /// 口径同壳侧 `CardDetailPopup.BuildCardHit` 的 `A157` ——「这一格**点不点得到**」与
        /// 「这一格的卡面**画不画得出来**」是两件事（否则一次渲染失败 = 点卡片变成关窗）。</para></summary>
        public int HitSlot(Vector3 world)
        {
            if (!Visible) return -1;
            for (int i = 0; i < SlotCount && i < CardFan.Slots; i++)
            {
                var v = SlotViews[i];
                if (v != null)
                {
                    // 🔴 判据只此一份 = `CardView.ContainsUiCollider`（矩形 = 原版 `2DCard` 的子件
                    //    `UI Collider`：`1.8927×2.8913 @ y −0.02`；标架 = 内层 `2DCard` ⇒ 跟卡转）。
                    //    见 `Core/CardView.cs` 的「第二套矩形」那一节。⛔ 不是 `BoxCollider2D` 主面
                    //    （那是手牌/棋盘的送达件）、⛔ 也不是 `CardFan` 那套轴对齐比例。
                    if (v.ContainsUiCollider(world)) return i;
                    continue;
                }
                if (!_hitNoViewSaid)
                {
                    _hitNoViewSaid = true;
                    Debug.LogWarning("[展示窗] 第 " + i + " 格的 `CardView` 没建出来（`BuildFan` 已经报过一次）⇒ "
                                   + "这一格的命中退回**轴对齐**的 `CardFan` 矩形（**不跟卡的转角/倾摆转**）"
                                   + "—— 「点不点得到」与「画不画得出来」是两件事（A157），所以这一下照旧吃掉");
                }
                if (HitSlotNoView(i, world)) return i;
            }
            return -1;
        }

        bool _hitNoViewSaid;

        /// <summary><see cref="HitSlot"/> 的**降级支**：那一格的 `CardView` 没建出来时，拿 `CardFan` 的
        /// 点击区矩形（判据只此一份 → `HitW`/`HitH`/`HitCy`）在**设计 px** 里判。
        /// 🔴 **它不跟卡的转角/倾摆转** —— 只走「卡没建出来」这条降级路（正常路径见 `HitSlot`）。
        /// ⚠️ 换算用 `LayoutSpace.ToDesignPixel`（⛔ **不是 `ToPixel`**）：这里比的是 `CardFan.Cx`/`HitCy`
        ///    这些**字面设计 px**，而只有 `ToDesignPixel` 才是 `MenuDraw.Local` / `FromPixel`（建件那条路）
        ///    的逆 —— `ToPixel` 的 x 写死 108，非 16:9 下差 `VisibleWidth / DesignWidth` 倍
        ///    （判据 → `LayoutSpace.ToPixel` 那一段：凡「**读回来的 px 要去比一个【字面设计 px】**」一律用它）。</summary>
        bool HitSlotNoView(int i, Vector3 world)
        {
            var px = LayoutSpace.ToDesignPixel(world);
            float hw = CardFan.HitW(i) * 0.5f, hh = CardFan.HitH(i) * 0.5f;
            return Mathf.Abs(px.x - CardFan.Cx(i)) <= hw && Mathf.Abs(px.y - CardFan.HitCy(i)) <= hh;
        }

        /// <summary>指针还停在**窗的地界**里吗（关窗那条「指针移开」用的守卫）。
        ///
        /// 🔴 **如实标注：这是「我们按实情收的口径」，不是原版字面。**
        ///   原版那条是 `CardCollider.OnPointerExit` → `CardScript.OnTouchExit`，
        ///   **唯一守卫是 `displayingCardFlag`**（= 指针离开**那张卡**就关）。
        ///   照字面做的话，窗里那颗钮（语音）**永远点不到** —— 它在卡外的 `LowerSection` 上，
        ///   指针一离开卡就先关窗了。⇒ 我们把「地界」定成 **5 个卡格 ∪ 语音钮**：指针离开这整块才关。
        ///   ⚠️ **2026-10-17（B8）**：原来这里还有第三个落点（眼睛钮）。那颗钮战斗版原版就没有
        ///   （`showCardTextButton = {m_PathID:0}`，13/13）⇒ 连同它的桩一起删了，「地界」只剩这两块。
        ///   ⚠️ **待实机核**：原版在**触屏**上是不是「抬手即关」，产物里证实不了
        ///   （`StandaloneInputModule` 没反编译）。判据 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        public bool ContainsPointer(Vector3 world)
        {
            if (!Visible) return false;
            if (HitSlot(world) >= 0) return true;
            return HitVoice(world);
        }

        /// <summary>语音按钮被点到了没有（px 判定，和别处同一套换算）。</summary>
        public bool HitVoice(Vector3 world)
        {
            if (_voiceBtn == null || !_voiceBtn.gameObject.activeSelf) return false;
            return HitBtn(world, CardWinBox.VoiceCx);
        }

        static bool HitBtn(Vector3 world, float cx)
        {
            // 🔴 **2026-10-17（`T09` 批 4 · W11）就地换口（铁律 5）**：`ToPixel` → **`ToDesignPixel`**。
            //   这里比的是 `CardWinBox.VoiceCx` / `CardWinBox.BtnCy`（**字面设计 px**）—— 与同文件
            //   `HitSlotNoView`（比 `CardFan.Cx` / `HitCy`）是**同一条判据**：只有 `ToDesignPixel` 才是
            //   `FromPixel`（建件那条路）的逆；`ToPixel` 的 x 斜率**写死 108**，非 16:9 下差
            //   `VisibleWidth / DesignWidth` 倍（4:3 ⇒ 0.75）⇒ 语音钮的可点区在别的宽高比下会漂。
            //   判据 → `LayoutSpace.ToPixel` 那段 doc（「凡读回来的 px 要去比一个【字面设计 px】一律用
            //   `ToDesignPixel`」）。
            var px = LayoutSpace.ToDesignPixel(world);
            float h = CardWinBox.BtnS * 0.5f;
            return Mathf.Abs(px.x - cx) <= h && Mathf.Abs(px.y - CardWinBox.BtnCy) <= h;
        }

        /// <summary>
        /// 点语音按钮 → 播**正在展示的那张卡**的单位语音（原版 `voiceOverButton` + `voiceOverAudioSource`）。
        /// 判据（哪条语音）复用 `VoiceLines.TryPick` —— **只此一处**，不另写一张后缀表。
        /// ⚠️ 这张卡没有语音时**说出来**（红线：不许静默失败），返回 false。
        /// </summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            if (!_hasShown) return false;      // 窗里没有卡（`CardData` 是 struct，用它判「有没有」）
            // ⚠️ **`_shown.id` 是「名字」不是卡 id**（`ToCardData` 的注释：`id` 兼着显示名/配对的活）——
            //    语音表按**引擎卡 id** 索引 ⇒ 要用 `artId`（原版卡 = 卡 id；自造卡 = 卡名，自然查不到）。
            //    判据只此一份：`CardButtons.VoiceKey`。
            string key = CardButtons.VoiceKey(_shown);
            if (string.IsNullOrEmpty(key)) return false;
            if (!VoiceLines.Has(key))
            {
                Debug.Log($"[展示窗] 「{_shown.title}」没有单位语音（`VoiceLines.Has(\"{key}\")` 为假）—— 语音按钮这次没声音");
                return false;
            }
            string file, text;
            // `rng` 传 null = 取第一条（定死、可复现）；这个按钮是「听一下」，不需要随机
            // ⚠️ 用 `ForVoiceOver`（**随便哪条都行**）—— 借 `ForDeploy` 会挂空：有的卡只有 attack/death
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null || _voiceAudio == null) return false;
            _voiceAudio.PlayOneShot(clip);
            LastVoiceFile = file;
            return true;
        }

        // ============================================================ 开 / 关

        /// <summary>开/关一次。已开着就关掉（原版那个方法名就是 `Toggle...`）。</summary>
        public void Toggle(CardData d, CardDef def) { if (Visible) Hide(); else Show(d, def); }

        /// <summary>开/关（**棋盘单位的轻点**走这条 —— 带「谁给我加的 buff」那几行）。
        /// `effects` 由调用方从引擎状态算（`RowsOf`），空/null ⇒ 那块整组不出现（原版就是「有 buff 才露」）。</summary>
        public void Toggle(CardData d, CardDef def, IReadOnlyList<EffectRow> effects)
        {
            if (Visible) Hide();
            else Show(d, def, effects);
        }

        /// <summary>开窗（不带卡表项 —— 只显示主卡、没有相关卡，并**出声**）。</summary>
        public void Show(CardData d) { Show(d, null, null); }

        /// <summary>开窗。`def` 是这张卡的**引擎卡表项** —— **相关卡要它才算得出来**（没有就只画主卡）。</summary>
        public void Show(CardData d, CardDef def) { Show(d, def, null); }

        /// <summary>开窗 + 那块「谁给我加的 buff」（原版 `ShowBattleCard` → `DisplayCardEffects` 一条链）。</summary>
        public void Show(CardData d, CardDef def, IReadOnlyList<EffectRow> effects)
        {
            Hide();                       // 先把上一次那一叠拆干净（换阵营要换卡框贴图，原地改不换图）

            ShownTitle = d.title;
            ShownBody = d.keywords;
            _shown = d; _hasShown = true;   // 语音按钮要按它查单位语音
            LastVoiceFile = null;
            // ⚠️ 这里原来还有一句 `LoreVisible = true;`（原版每次 `ShowCard` 都把 lore 复位）——
            //    那颗眼睛钮连同这个开关已删（判据见文件头）⇒ 「露不露」现在**只有一处判据**：
            //    `RefreshLore` 里按**有没有字**开关（原版 `SetCardLore.c:24-27`）。

            BuildFan(d, def);
            RefreshLore();
            RefreshButtons();

            SetChrome(true);
            Visible = true;
            gameObject.SetActive(true);
            SetEffectRows(effects);      // ⚠️ 放在最后 —— 它要先把那块亮起来再改字（TMP 未激活量不出尺寸）
        }

        public void Hide()
        {
            Visible = false;
            ShownTitle = null;
            ShownBody = null;
            _hasShown = false;
            LastVoiceFile = null;
            _swapping = false;
            _opening = false;            // 关窗 = 那 5 条补间随之死掉（`SetLink`），闸③ 也一并清
            SlotCount = 0;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }
            if (_cards != null) MenuDraw.ClearChildren(_cards);
            if (_bg != null) MenuDraw.ClearChildren(_bg);
            _lore = null;
            SetChrome(false);
            SetEffectRows(null);         // 那块也跟着收（原版 `DisplayCardEffects` 每次重算）
        }

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心偏上 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildFan(CardData d, CardDef def)
        {
            var cards = new List<CardData>();
            var defs = new List<CardDef>();
            cards.Add(d); defs.Add(def);
            if (def != null)
            {
                // 相关卡：判据只此一份（`RelatedCards.Find`），与原版那 4 个 `relatedCard1..4` 字段的关系 → 那个文件头
                foreach (var r in RelatedCards.Find(def, CardFan.Slots - 1))
                {
                    cards.Add(BattleDriver.ToCardData(r, r.Faction));
                    defs.Add(r);
                }
            }
            else
            {
                Debug.LogWarning("[展示窗] 没拿到卡表项（`CardDef`）⇒ **只显示主卡、没有相关卡**"
                               + "（相关卡是按卡表算的；不静默）");
            }

            SlotCount = Mathf.Min(cards.Count, CardFan.Slots);
            _swapping = false;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }

            for (int i = 0; i < SlotCount; i++)
            {
                var cd = cards[i];
                // 🔴 **`CardUse.ZoomWindow`（`A2110` + `A2111`）**：放大窗那一族在原版**是另一种值** ——
                //    `NameTextUnit` 字号 **16**（别处全 19）、阴影 alpha **0.8**（材质默认 0.604）。
                //    扫 13 arena × 8 实例 = 104 个 `NameTextUnit`：`fs=16` 恰好 65 个 = 每场这 5 张
                //    （判据 → `资料/普查产出_1015战斗/C1_逐格对账_卡面放大窗与卡面渲染.md` 表三·1/·2）。
                //    ⛔ **别图省事去掉这个形参** —— 去掉就是把放大窗改回「大 18.75% + 影子偏淡」。
                var v = CardView.Create(_cards, cd, CardFan.SlotName(i), use: CardUse.ZoomWindow);
                if (v == null)
                {
                    Debug.LogWarning("[展示窗] 第 " + i + " 格（" + cd.title + "）的 `CardView` 建不出来 —— 那一格没画，出声");
                    continue;
                }
                v.gameObject.SetActive(true);
                // 🔴 **在这里摆的是 `Card Display Open` 那条 clip 的 t=0**（5 个槽一律 `(0,60)` / 0° / scale 210），
                //    **不是终态** —— 终态由下面的 `PlayOpenAnimation` 在 `CardFan.OpenTime`(0.3167s) 里补过去
                //    （原版那颗 `Animation.m_PlayAutomatically = true`：窗口一亮就播）。
                //    ⛔ **别改回「直接摆终态」**：`Editor/BattleScene.cs` 那一段「开窗起点 / 半程」会红
                //    （那两条就是这条动画的**灭自证** —— 只断终态的话「摆终态」也满足）。
                v.SetPose(OpenStartLocal(i), CardFan.OpenStartRot,
                          CardFan.OpenStartHpx / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);   // 展示窗里的卡不吃「可出牌/不可出牌」那套状态色
                CardFan.ApplySlotTint(v, i == 0);            // 前台白 / 相关卡压暗（原版 `cardInBackGroundColorTint`）
                SlotViews[i] = v; _slotDefs[i] = defs[i];
            }
            CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
            PlayOpenAnimation();          // 开窗那一拍（原版窗根那颗 `Animation`，见 `CardFan` 里那一块判据）
            Debug.Log($"[展示窗] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 位姿：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（`CardFan` 文件头）");
        }

        /// <summary>第 `i` 格的**局部坐标**（相对 `_cards`；z 按槽位阶梯，只作兜底，真正排序看渲染队列）。
        /// ⚠️ 走 `MenuDraw.Local` **而不是**直接拿 `EndPanel.Pos` —— 那样只有在「窗根正好在世界原点」时才对。</summary>
        Vector3 SlotLocal(int i)
        {
            float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
            var l = MenuDraw.Local(_cards, CardFan.Cx(i) - w * 0.5f, CardFan.Cy(i) - h * 0.5f,
                                            CardFan.Cx(i) + w * 0.5f, CardFan.Cy(i) + h * 0.5f);
            return new Vector3(l.x, l.y, ZContent - i * ZSlotStep);
        }

        /// <summary>第 `i` 格的**开窗起点**（局部坐标）—— 原版 clip `Card Display Open` 的 t=0：
        /// 5 个槽**一律**在屏心偏上 `(960,480)`、卡体 scale 210。
        /// ⚠️ 原版那 10 条位移曲线只动 `m_AnchoredPosition.x/y`、**不动 z** ⇒ 起点**沿用该格终态的 z**
        /// （`SlotLocal` 那个 `ZContent − i×ZSlotStep` 阶梯 —— 它只是兜底，真正排序看渲染队列）。</summary>
        Vector3 OpenStartLocal(int i)
        {
            var l = MenuDraw.Local(_cards,
                                   CardFan.OpenStartCx - CardFan.OpenStartWpx * 0.5f,
                                   CardFan.OpenStartCy - CardFan.OpenStartHpx * 0.5f,
                                   CardFan.OpenStartCx + CardFan.OpenStartWpx * 0.5f,
                                   CardFan.OpenStartCy + CardFan.OpenStartHpx * 0.5f);
            return new Vector3(l.x, l.y, SlotLocal(i).z);
        }

        /// <summary>开窗那一拍：把 5 个槽从**起点**（原版 clip t=0：`(0,60)` · 0° · scale 210）
        /// 补间到**扇形终态**（`CardFan` 的 `_x/_y/_rot/_scl`），时长 `CardFan.OpenTime`。
        ///
        /// <para>🔴 **判据逐值来自那条 legacy clip 的原文**（出处与逐值 → `Core/CardFan.cs` 的
        /// 「开窗那一拍」那一块）。**缓动不能交给 DOTween 的枚举** —— 原版是**平滑步 `t²(3−2t)`**，
        /// 而 `Ease.InOutQuad`/`InOutSine` 都不是它 ⇒ 一律 `Ease.Linear` 建 tween、
        /// 在 setter 里走 `CardFan.OpenEase`。</para>
        ///
        /// <para>🔴 **为什么是代码补间、不是那颗 `Animation` 组件**：原版窗根挂着 legacy clip
        /// `m_PlayAutomatically = true`；而本工程 **legacy `AnimationClip` 从 bundle 搬进工程会变空壳**
        /// （`资料/已知的坑.md`「bundle 资产搬进工程的两条死路」）⇒ 只能把曲线**读出来写等价补间**。
        /// ⚠️ **批处理下没有帧循环** ⇒ 一律经 `CardTween`（`UpdateType.Manual` 时由 `CardTween.Advance` 推进）。
        /// ⚠️ 原版**只有开窗、没有关窗动画**（`CardDisplayWindow__Close.c` 里没有第二条 clip）—— ⛔ 别自己补。</para>
        ///
        /// <para>⚠️ **每帧走 `SetPose`、不裸用 `DOLocalMove`/`DOScale`**：`CardView` 的缩放要经 `ApplyScale`
        /// （高亮那层在 `_baseScale` 上乘系数）⇒ 直接写 `transform.localScale` 会让 `_baseScale`
        /// **永远停在起点值**上（之后任何一次 `SetPose`/高亮刷新都会把卡弹回 210 那一档）。</para>
        ///
        /// <para>⚠️ **闭包**：`v` 与那几个起终点都声明在循环体内（每次迭代一个新变量），
        /// 循环变量 `i` **不进 lambda** —— 否则 5 条 tween 会全用最后一格的值（经典踩法）。</para></summary>
        void PlayOpenAnimation()
        {
            _opening = false;
            DG.Tweening.Tween last = null;
            float denom = CardView.Height * 108f;
            for (int i = 0; i < SlotCount && i < CardFan.Slots; i++)
            {
                var v = SlotViews[i];
                if (v == null) continue;                     // 建不出来那一格（`BuildFan` 已经出声告警过）
                Vector3 startPos = OpenStartLocal(i), endPos = SlotLocal(i);
                float startRot = CardFan.OpenStartRot, endRot = CardFan.Rot(i);
                float startScale = CardFan.OpenStartHpx / denom, endScale = CardFan.Hpx(i) / denom;
                // 起步先把起点写上（同 `CardFan.TweenTint` 那条：不写就会从『当前位姿』起步，连开两次会跳）
                v.SetPose(startPos, startRot, startScale);
                float e = 0f;
                var tw = DG.Tweening.DOTween.To(() => e, x =>
                {
                    e = x;
                    float k = CardFan.OpenEase(x);           // 🔴 原版那条曲线在这儿
                    v.SetPose(Vector3.Lerp(startPos, endPos, k),
                              Mathf.Lerp(startRot, endRot, k),
                              Mathf.Lerp(startScale, endScale, k));
                }, 1f, CardFan.OpenTime);
                last = CardTween.Use(tw, DG.Tweening.Ease.Linear, v);
                _opening = true;
            }
            // 闸门（= 原版 `displayAnimation.isPlaying`）：**最后一条播完**才算开窗动画结束。
            if (last != null) last.OnComplete(() => { _opening = false; });
            Debug.Log($"[展示窗] 开窗动画：{SlotCount} 格从 (0,60)/0°/scale210 补到扇形终态，"
                    + $"{CardFan.OpenTime:F4}s = 原版 clip `Card Display Open`（60fps · 19 帧 · 平滑步）");
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ **开窗动画还在播**。
        /// 🔴 **2026-10-16 就地订正（铁律 5）**：闸③ 这条注释原来写「**本工程不适用** —— 原版摆位那条
        /// legacy clip 我们没有」—— **前半句现在不成立了**：开窗动画已按 clip 原文补上
        /// （`PlayOpenAnimation` / `CardFan.OpenTime`），所以闸③ **照原版接**。
        /// 判据（`CardDisplayWindow__ChangeCardPosition.c:53-57`，亲读）：
        /// ```c
        /// if (*(longlong *)(param_1 + 0xb8) != 0) {            // +0xB8 = displayAnimation
        ///   if (UnityEngine_Animation__get_isPlaying(*(longlong *)(param_1 + 0xb8),0) != '\0') return;
        /// ```
        /// ⚠️ 这条闸**不是可有可无**：开窗补间与换位补间写的是**同一个 transform**，
        /// 开着时不挡 ⇒ 两条 tween 互相覆盖、卡会停在**谁也没算过的位姿**上（静默）。
        /// 收尾照原版 `CardSwapFinished`：开闸 · 换前台指针 · **重设 lore / 语音钮** ·
        /// **换色补间**（去前台变白、让位的压暗到 `cardInBackGroundColorTint`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_opening)
            {
                Debug.Log($"[展示窗] 换位被挡：开窗动画还在播（原版闸③ `displayAnimation.isPlaying`，"
                        + $"还有 {CardFan.OpenTime:F4}s 那条没走完）");
                return;
            }
            if (_swapping)
            {
                Debug.Log("[展示窗] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= CardFan.Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.eulerAngles.z, rb = b.transform.eulerAngles.z;
            float sa = a.transform.localScale.x, sb = b.transform.localScale.x;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            CardTween.ToPose(b.transform, pa, ra, sa, CardFan.SwapTime, DG.Tweening.Ease.OutQuad).OnComplete(() =>
            {
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **层级按新的槽位重排**（原版那一步是 `SetSiblingIndex` 两两互换 —— 目的就是
                //    「现在站在前台的那张画在最上面」；我们这套的前后由**渲染队列**决定，见 `CardFan`）。
                CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：重设 lore / 语音 / 眼睛钮 —— 认的都是**新前台**那张
                if (SlotViews[0] != null)
                {
                    _shown = SlotViews[0].Data;
                    ShownTitle = _shown.title;
                    ShownBody = _shown.keywords;
                }
                RefreshLore();
                RefreshButtons();
                Debug.Log("[展示窗] 换位完成：前台现在是「" + (SlotViews[0] != null ? ShownTitle : "?")
                        + "」（收尾重设了 lore / 语音 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, CardFan.SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换：去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`
            // （原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, CardFan.SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, CardFan.SwapTime);
            Debug.Log($"[展示窗] 换位：第 {idx} 格 ⇄ 前台（{CardFan.SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        // ============================================================ 下缘那条（文字 + 两颗钮）

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 文案
        /// （I2 词条在远端，本地拿不到 —— 正本 §十·2 已结案）⇒ 画**效果文字**，并**如实标注**。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。</summary>
        void RefreshLore()
        {
            if (_bg == null) return;
            MenuDraw.ClearChildren(_bg);
            _lore = null;
            // ⚠️ **先亮起来再建字** —— TMP 在**未激活**的对象里量不出尺寸（`textBounds` 是垃圾，
            //    实测能到 4.29e9 ⇒ 面板宽度顶到上限、一个字看不见）。最后再按开关收起/露出。
            _bg.gameObject.SetActive(true);

            // ① 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张。
            //    ⚠️ 原版 `FlavourTextBG` 的 `m_PreserveAspect = 0`（实读）⇒ **拉伸**铺满，别开 keepAspect。
            // 🔴 **2026-10-16（`T09` 批 3）**：`MenuDraw.Rect` → `Img`（uGUI 一张 `Image`、`PA = 0` 拉满）。
            //    矩形就是 `_bg` 自己那一块（`LowerL/LoreBgT → LowerR/LoreBgB`）⇒ **相对父中心 = (0,0)**，
            //    尺寸 = 那两个差 —— 与旧那套逐值相同。
            string fac = _hasShown ? _shown.faction : null;
            var tex = CardArt.FlavorBg(fac);
            if (tex != null)
                Img(_bg, tex, "Image", Vector3.zero,
                    CardWinBox.LowerR - CardWinBox.LowerL, CardWinBox.LoreBgB - CardWinBox.LoreBgT,
                    ZContent, null);
            else
                Debug.LogWarning($"[展示窗] 阵营「{fac}」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");

            // ② 那行字
            // 🔴 **没字 ⇒ 整块不亮**（原版 `SetCardLore.c:24-27`：`SetActive(loreObjectBG, !IsNullOrEmpty(flavourText))`）。
            //    ⚠️ **2026-10-17 就地更正**：这里原来是 `SetActive(LoreVisible)`，而那个开关恒 true
            //    ⇒ **没字也亮一块空的风味底板**（它当时倒是能被眼睛钮手动切掉，可战斗版原版根本没有那颗钮
            //    ⇒ 实际就是「永远露着」）⇒ 照原版收口（`Editor/BattleScene.cs` 有一条断言钉着）。
            //    🆕 **2026-10-17（B8）**：那个开关本身已删 —— 现在这一块的显隐**只有这一处判据**。
            string body = ShownBody ?? "";
            if (string.IsNullOrEmpty(body)) { _bg.gameObject.SetActive(false); return; }
            // 🔴 **2026-10-16（`T09` 批 3）**：`MenuDraw.Text(...)` → `Text(...)`（uGUI `TMP_UGUI`）。
            //    那一颗四格（标称 / 下限 / 上限 / 框）**逐值照旧**，只是单位从「世界单位」换成 px：
            //    `boxW` = `r.W` = `LoreR − LoreL`（1250）· `boxH` = `r.H`（93.47）· 下限 18 · 上限 32。
            // 原版战斗版 `LoreText`：**fs32** · `auto[18,32]` · 基准 36 · 限宽换行 · **居中** · 白
            // 🔴 **2026-09-28：不是右对齐** —— `m_HorizontalAlignment = 2`（Center，位标志 Left=1/Center=2/Right=4），
            //    实拍也一致（正本 §十·5 第 4 条）。两颗建字口都默认居中 ⇒ **不要** `AlignRight`。
            // 🔴 **A406（2026-10-12）就地订正「fs35 auto 10–35」那两个数**：那是**菜单版**
            //   （`Card Detail Popup`）那颗的值；**战斗这一份**（`bundle_scenes_scenes_battlearena1` 的
            //   `GameObject/LoreText.json`，父链 = `LowerSection/FlavourTextBG`）原版是
            //   **`字号=32.0 auto[18.0~32.0] 基准=36.0`**。
            // 🔴 **2026-10-15（`A2111` 表三·3/·4）把剩下那两格也补齐**：之前只改到上限（`LoreAutoMax=32`），
            //   **标称字号仍传菜单版的 `LorePx`(35)**、**下限仍传 `10f`**（原版战斗版是 **18**）。
            //   现在三格都是战斗版的：标称 `LorePxBattle`(32) · 下限 **18** · 上限 `LoreAutoMax`(32)。
            _lore = Text(_bg, "LoreText", body,
                         Rel((CardWinBox.LoreL + CardWinBox.LoreR) * 0.5f,
                             (CardWinBox.LoreT + CardWinBox.LoreB) * 0.5f, LoreBgC.x, LoreBgC.y), 5,
                         CardWinBox.LorePxBattle, CardWinBox.LoreR - CardWinBox.LoreL,
                         CardWinBox.LoreB - CardWinBox.LoreT, 18f, LoreAutoMax,
                         ZContent, Color.white, new Vector2(0.5f, 0.5f));
            if (_lore == null)
                Debug.LogWarning("[展示窗] 效果文字条建不出来（`Text` 返回 null）—— 不静默");
            _bg.gameObject.SetActive(true);    // 走到这里 body 必非空 ⇒ 照原版「有字才亮」（见上面 ② 那条判据）
        }

        /// <summary>遮罩用的纯色贴图（`ImageQuad` 得有一张图才肯建）。宽高比按传入的来。
        /// ⚠️ **`MultiCardDisplay` 也用这一个**（两张遮罩是同一个作用，别写第二份）。
        /// 🔴 2026-09-28 本文件重写时**一度把它弄丢**（`MultiCardDisplay` 三处调用立刻编译不过）——
        ///    别再删：它不是本窗私有的。</summary>
        static Texture2D _solid;
        static float _solidAspect;
        internal static Texture2D SolidTexFor(float aspect)
        {
            if (_solid != null && Mathf.Abs(_solidAspect - aspect) < 0.01f) return _solid;
            int h = 32;
            int w = Mathf.Max(4, Mathf.RoundToInt(h * aspect));
            _solid = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            _solid.SetPixels(px);
            _solid.Apply();
            _solidAspect = aspect;
            return _solid;
        }

        /// <summary>语音钮**按卡决定建不建**（原版 `CardDisplayWindow.SetCardVoiceOver`。
        /// ⚠️ **本窗只有这一颗** —— 眼睛钮战斗版原版就没有（见文件头 A860），别再照菜单版补一颗。
        /// 判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物）。</summary>
        void RefreshButtons()
        {
            bool voice = _hasShown && CardButtons.HasVoice(_shown);
            if (_voiceBtn != null)
            {
                _voiceBtn.gameObject.SetActive(voice);
                // ⚠️ 旧那套这句是「建件时它 z = 0（`MenuDraw.Rect` 不写 z）⇒ 露出来时要把它推到 `ZContent`」；
                //    uGUI 档里它**建件时就已经登记在 `ZContent` 那一档**了 ⇒ 这句成了**幂等的重排**
                //    （`SetZ` → `HudCanvas.Reorder`，不会在账里留第二条 —— 见那个助手）。
                if (voice) SetZ(_voiceBtn, ZContent);
            }
        }
    }
}
