// WindowsManager.cs — 窗口系统：**所有菜单页都从这一条路开**（阶段二「游戏外壳」）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_Shell_原版规格.md` §三「开窗口调用链」。类名 / 枚举值 / 流程**照原版**：
//   · `WindowsPlacement { None=0, Canvas=5, World=10, Popup=15 }`
//     实证：主菜单的 `1 - Below Upper Bar Holder{10}` · `2 - Canvas Holder Above upper bar{5}` · `3 - PopUp Holder{15}`
//   · `WindowType { Fullscreen=0, Popup=1 }` —— `WindowsManager__OpenWindowCO.c` 里 `window.type==0` 关当前主窗、
//     `==1` 把上一个 `ToBackground()`
//   · `WindowHolder.OnEnable` → `WindowsManager.RegisterAnchor(placement, transform)`（`WindowHolder__OnEnable.c`）；
//     `GetWindowAnchor` 取不到时原版走 `CustomDebug.LogError` ⇒ 我们照做（项目红线：不许静默失败）
//   · `GameWindow.TryOpen`：`SetupData` → **`SoundManager.Play2D(openSound, MixerType.FX)`** → `SetActive(true)`
//     → `CurrentState=Open` → `Open()`（`GameWindow__TryOpen.c`）
//   · `GameWindow.Open()`：**小屏 UI 开关开着**且 `extraScaleSmallScreen != 1` 时
//     `TransformScalerBySmallScreenUI.SetScale(extraScaleSmallScreen)`（`GameWindow__Open.c`）
//     —— 🔴 **2026-10-06 就地订正（铁律 5）**：这里原来写「**窗口宽度 < 阈值**时 …」+「实证值：普通窗 1.0 ·
//     商店/活动类 1.2」—— **两句都不对**。判据：`GameWindow__Open.c` 里**没有任何宽度判定**
//     （第一层判据是静态 bool `GameStaticData.smallScreenUI`，第二层是 `Mathf.Approximately(extra,1)`）；
//     `extra` 实测 1.0 有 93 个、其余 48 个是 1.05/1.07/1.075/1.1/1.12/1.15/1.2/1.35 **逐窗实数**。
//     全量复核 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §②（正本 §三 第 5 条也已就地订正）。
//
// 🔴 **不是照抄的部分（原版查不到，如实标 —— 铁律 3）**：
//   ① **弹窗 prefab 与「类 → prefab」字典是自建的**：全 `assets_full` grep
//      `popupWindowOneButton` / `temporaryWindowDictionary` **零命中**（正本 §五 第 2 条）。
//      ⇒ `ShowPopUp` 我们只保留**签名与行为**（文案 + 1~2 个按钮 + 结果回调）。
//      🔴 **2026-09-23 更正（铁律 5）**：上面这条「原版 popup prefab 本地没有」**已经不成立** ——
//      `GenericPromptWindow` 在 `bundle_menus_assets_all` 里参数齐全（`资料/日常_原版规格.md` §七），
//      界面已改由 `PromptPopup` **照原版**搭。
//   ② 🔴 **2026-10-06 就地订正（铁律 5 · A154/A165 全量复核）**：这里原来写「**「宽度 < 阈值」的那个阈值查不到**
//      ⇒ 只在 `extraScaleSmallScreen != 1f` 时才放大；普通窗实测就是 1.0 ⇒ 默认空转（不是没实现，是没东西可放大）」
//      —— **阈值不存在**，原版从来不量宽度；真正缺的那一层是**开关**（`GameStaticData.smallScreenUI`，
//      出厂 0）+ **缩放器组件**。**2026-10-06（A165）两样都补上了**：开关 → `Shell/TransformScalerBySmallScreenUI.cs`
//      的 `SmallScreenUI`，缩放器 → 同文件的 `TransformScalerBySmallScreenUI`，挂在 `GameWindow.TryOpen` 上。
//      ⇒ 「默认空转」这个说法**现在不成立**了：普通窗（`extra = 1.0` 且 prefab 没烤 `menuScale`）在小屏开关开着时
//      也**不放大**（原版就是这么设计的），但三条 `extra = 1.0 / 烤 1.35` 的窗会按 **1.35** 放大。
//   ③ `WindowsManager` 实例的序列化值全丢 ⇒ `anchors` 表靠场景里的 `WindowHolder` 注册（照原版机制），
//      **不要**在代码里写死三个锚点的引用。
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;      // `WFSoundPlayer`（特效/音效那条路唯一的播放器，`Assets/WarpforgeVFX/Runtime/`）

namespace CardPresentation
{
    /// <summary>窗口锚点位置。**值照原版**（主菜单三个 Holder = 10 / 5 / 15）。</summary>
    public enum WindowsPlacement { None = 0, Canvas = 5, World = 10, Popup = 15 }

    /// <summary>窗口类型。**值照原版**（`OpenWindowCO` 按它决定关不关当前主窗 / 要不要把上一个压到背景）。</summary>
    public enum WindowType { Fullscreen = 0, Popup = 1 }

    /// <summary>窗口状态。**值照原版**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowState.cs`：
    /// `Closed = 0 / Open = 1 / Background = 2`）。
    /// <para>🔴 **2026-10-07（A77㉑②）就地订正（铁律 5）**：本枚举原来中间多插了一枚 `Opening`
    /// （`Closed = 0, Opening, Open, Background`）⇒ **`Open` 从 1 变成 2**。
    /// 原版 `GameWindow.IsOpen()` 的判据是**字面量** `*(int*)(this + 0x68) == 1`
    /// （`d:/2/tools/decomp_full/GameWindow__IsOpen.c`；`CurrentState` 就在 0x68 —— 字段偏移见
    /// `d:/2/tools/il2cpp_out/dump.cs` 的 `GameWindow` 一节）⇒ 枚举一旦错位，**照常量补那道门槛就会判错**
    /// （`Opening` 会被当成 `Open`）。</para>
    /// <para>`Opening` 全仓**从未被使用过**（`grep -rn "Opening" --include=*.cs` 只命中 `Booster Opening` 之类
    /// 的动画名）⇒ 删掉它是**零行为变化**，且本文件下面新加的 `IsOpen()` 才与那个 `1` 对得上。</para></summary>
    public enum WindowState { Closed = 0, Open = 1, Background = 2 }

    /// <summary>挂在场景里的锚点节点上（主菜单三个）。`OnEnable` 自注册 —— 原版就是这个机制。
    /// ⚠️ `[ExecuteAlways]` 是**我们加的**：建场景是在**编辑模式**下跑的，而普通 MonoBehaviour 的
    /// `OnEnable` 在编辑模式下不触发 ⇒ 不注册就一个锚点都没有（2026-09-22 第一版自检 8 条红里有 5 条是这个根因）。</summary>
    [ExecuteAlways]
    public class WindowHolder : MonoBehaviour
    {
        public WindowsPlacement placement = WindowsPlacement.None;

        void OnEnable() { RegisterNow(); }
        void OnDisable() { WindowsManager.UnregisterAnchor(placement, transform); }

        /// <summary>🔴 **必须由建场景的代码在「赋完 placement 之后」显式调一次** ——
        /// `AddComponent` 的那一刻 `OnEnable` 就跑掉了，而 `placement` 是**下一行**才赋值的
        /// ⇒ 光靠 `OnEnable` 会拿 `None` 去注册（2026-09-22 第一版就这么白跑一轮，日志里那条
        /// 「placement = None」的报错就是它）。同一个节点重复注册是幂等的（`RegisterAnchor` 会直接返回）。</summary>
        public void RegisterNow() { WindowsManager.RegisterAnchor(placement, transform); }
    }

    /// <summary>所有菜单窗口的基类。子类重写 <see cref="Open"/> 做自己的铺数据/播动画。</summary>
    public class GameWindow : MonoBehaviour
    {
        public WindowType type = WindowType.Fullscreen;

        /// <summary>🔴 **A166 哨兵 —— 这不是原版的枚举值，是「还没赋过值」的标记**。
        /// <para>原版 `windowsPlacement` 在 prefab 里是**必填**的（全库 141 个带该字段的实例**逐个都有值**）；
        /// 我们是逐窗在各自的 `Create()` 里赋（26 扇有对照的窗全赋了）。**默认值原来是 `Popup`(15)** ⇒
        /// 将来哪扇新窗忘了赋，会**静默**挂到弹窗那一档 —— 这正是本工程红线「不许静默失败」要挡的那种。</para>
        /// <para>⇒ 默认值改成这个**不可能被误当成合法档位**的哨兵，由 `AttachToAnchor` **出声**（A166；
        /// 出处 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §④-5）。</para>
        /// ⛔ **别把它加进 `WindowsPlacement` 枚举**（枚举值照原版：`None=0 / Canvas=5 / World=10 / Popup=15`）。</summary>
        public static readonly WindowsPlacement UnsetPlacement = (WindowsPlacement)(-1);

        public WindowsPlacement placement = UnsetPlacement;
        /// <summary>ESC 能不能关。**逐窗不同，照各自实证值填**（正本 §三 第 10 条列了 7 个实例）。</summary>
        public bool closeOnEsc = true;
        /// <summary>小屏 UI 下的额外放大倍数（原版 `extraScaleSmallScreen`）。**逐窗实测、照各自的值填**
        /// （1.0 有 93 个；1.07 练习窗 · 1.075 玩家档案 · 1.15 决斗 · 1.2 卡包信息 …）。
        /// 🔴 **`1.0` 的含义是「不覆盖」**：此时 prefab 里烤着的 `menuScale` 原样生效 ——
        /// 窗口根上带成品的 3 扇（`TrophyInfoPopup` / `AllianceMemberOptionsPopup` / `GenericOptionsPanel`）
        /// 就是 `extra = 1.0` 而烤的是 **1.35**。判据 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头。</summary>
        public float extraScaleSmallScreen = 1f;
        /// <summary>开窗音效（原版 `TryOpen` 里播，走 FX 组）。没有就不播。</summary>
        public AudioClip openSound;

        // ============================================================ 裁切状态：`Clip` / `ClipSoftness` / `ClipPad`
        //
        // 🆕 **2026-10-07（A78②）「三兄弟」从 `MenuWindowBase` 挪到这里** —— 按判据原文：
        //   「`MenuDraw.Hit` / `DeckCell` 的 `hitPad` 参数**只有 `MenuWindowBase` 家族能喂到**……
        //    这正是「下一个缺口」的形状**……将来真要给那一族加，该做的是**把状态挪到 `GameWindow`**，
        //    ⛔ 不是每扇窗各抄一段」（出处 → `资料/普查产出_1004/W6审查_共用件.md` · `资料/待办判据_1006.md` §A78②）。
        //   ⇒ 现在这三份状态 + 两份转发（`RenderClip` / `AddHit`）**只声明一次**，全 `GameWindow` 族都继承得到。
        // 🔴 **零行为变化**：原来那一族（`MainMenuSubmenuWindow` 及奖励/商店/社交/收藏/档案窗）的读法**一个字没改**
        //   （`_win.ClipPad` / `_win.ClipSoftness` / `Clip` / `RenderClip` 全靠继承解析到同一份）——
        //   改的只是**声明处**上移了一层。既有断言逐条覆盖它：`Editor/ShellScene.cs` 的
        //   `RenderClip = Clip 按 ClipPad 内缩`（左沿 1010）· `Editor/RewardsScene.cs:1511,1513,1520,2817`（锻造/战役两条轨道）。
        // ⚠️ **这管两件事，别只当它是渲染状态**（判据 → `MenuDraw.PaddedClip` 上面那一段·UGUI `Culling/Clipping.cs:26-30`）：
        //   · **渲染那一片** = `RenderClip`（= `Clip` 按 `ClipPad` **内缩**）→ 喂 `Rect` / `Nine` / `Text` / `TextBox`；
        //   · **命中区那一片** = **裸 `Clip` + `ClipPad`** → `AddHit` 转发给 `MenuDraw.Hit`，由它**把 `ClipPad` 缩在
        //     `clip` 上**（= 原版的 `R ∩ (V−pad)`）。
        //     🔴 **2026-10-08（A188）就地订正（铁律 5）**：本行原文写「→ 缩**命中区自己的矩形**」—— **错模型**。
        //     判据（两道射线关：图形自己的 rect+`m_RaycastPadding` ∧ mask 自己的 rect+`m_Padding`）与代价
        //     （锻造轨道那一幕实测：原版命中宽 200.762 / 旧写法 190.762）→ `MenuDraw.Hit` 的 `maskPad` 注释。
        //     ⚠️ 此后**两条路缩的是同一个框**（`V − pad`），差别只有「缩完拿去干嘛」：一条给渲染、一条给命中。
        //
        // 🔴 **原版把这类状态放在哪一层**（第一权威 = 反编译 / 解包资源）：**不是「窗口的字段」**，而是
        //   **每个视口节点自己挂的 `RectMask2D` 组件**（`m_Padding` / `m_Softness` / `m_Enabled` 三样都在组件上，
        //   逐处实读的出处 → 下面 `ClipSoftness` / `ClipPad` 两段的表）。⇒ 我们的模型里与之等价的那一份状态
        //   **必须能挂在「任意一扇窗」上**（任何一扇窗里都可能开出一个视口）—— 这正是它属于 `GameWindow`、
        //   而不是某个子家族的理由。
        // ⚠️ **还没查清的那半**（⛔ 别猜，如实记）：原版那个组件是**逐节点**的（同一扇窗里几个视口可以各自带着
        //   不同的 pad / softness），而我们是**逐窗一份**（纪律：谁设谁还原 —— `ForgeTab` / `CampaignTab` 的
        //   「成对拿捏」写法）。今天生产上非零的只有锻造轨道一处 ⇒ 够用；**将来同一扇窗里两个视口的 pad 不同时，
        //   这一套表达不了**（那时该做的是让状态可入栈，⛔ 不是再抄字段）。

        /// <summary>**裁切边界**（画布像素 · 左上原点）。非空时 `Rect` 把越界部分**截掉**、
        /// 并把 uv 跟着截（`ImageQuad.SetUvRect`）—— 这就是原版 `RectMask2D` 的等效物。
        /// 谁用它：滚动区在画内容**之前**设一次、画完清掉（`ForgeTab.BuildRewardCells` 那种）。
        /// 🆕 **2026-10-03：横纵两轴都裁了** —— 原来只裁 x，纵向滚动区（商店 `Packs Scroll View`）
        ///    接上来时才暴露出「纵向裁不住」。
        /// 🆕 **2026-10-03：九宫格与点击区也吃 `Clip`** —— `Nine()` 建完**逐子块**截到框内、根节点位置不动；
        ///    `AddHit()`（→ `MenuDraw.Hit`）：**视口外 → 连节点一起不建**；压在视口边上 → 命中区**截到视口内**
        ///    （判据 = 原版 `RectMask2D` 的**射线那一面**，出处见 `MenuDraw.ClipRect` 的注释）。
        /// ✅ **2026-10-04：文字也吃 `Clip`**（`MenuDraw.ClipText`：TMP 逐字夹顶点 + 改 uv）。
        /// 🔴 **2026-10-07（A140②）**：喂给渲染那些口子的**不是本字段本身**，而是 `RenderClip`
        ///   （= 本字段按 `ClipPad` 内缩）—— 原版 `RectMask2D` 的渲染那一面**也读 `m_Padding`**。
        /// 🔴 **2026-10-07（A78②）：声明处从 `MainMenuSubmenuWindow` 上移到本类**（判据见上面那一段）。</summary>
        public PxRect? Clip;

        /// <summary>**软边**（原版 `RectMask2D.m_Softness`，**画布像素**：`x` 管左右两条边、
        /// `y` 管上下两条边；`(0,0)` = 硬边 = 没有软边那套行为）。**与 `Clip` 配对使用**：
        /// `Clip` 画内容前设一次、画完清掉，本字段同理（谁设 `Clip` 谁负责把软边一起设对）。
        /// 🔴 **2026-10-07（A78②）：声明处从 `MainMenuSubmenuWindow` 上移到本类**（逐字表照旧，见下）。
        ///
        /// 🔴 **原版真值（逐处实读 `assets_full` 的 `RectMask2D` JSON · 字段名 `m_Softness`）**，本壳用到的几处：
        ///   · 商店三页 `Viewport`（`Card Shop Tab` / `Item Shop Tab` / `Daily Shop Tab` /
        ///     `Item Shop Tab No Automatic Ordering` / `Card Shop VIP Tab Variant` /
        ///     `Shop Menu Variant/Content Area/Tabs/Shop Tab` 的 `Packs Scroll View/Viewport`）= **(0,25)**
        ///   · 锻造厂阵营条（`Forge Tab/Forge Army Selector/Viewport` 与
        ///     `Rewards Base Submenu Variant/…/Forge Tab/Forge Army Selector/Viewport`）= **(42,0)**
        ///   · 练习窗阵营条（`Practice Mode Menu/Deck Selector/Army Selector/Viewport`）= **(0,50)**；
        ///     同一窗的卡组列表（`…/Deck Buttons/Decks Scroll view/Viewport`）= **(0,23)**
        ///   · 玩家档案的 `Avatar Tab` / `Title Tab` 两个 `Item Display Panel/Scroll Rect` = **(0,50)**；
        ///     `Trophies Tab/Scroll/Viewport` 与 `Ranking Tab/AllFactions/scroll rect/viewport` = **(0,0)**
        ///   · 聊天（`Chat Tab/Viewport`，在 `bundle_mainmenualwaysloaded_assets_all`）= **(0,22)**
        ///     ✅ **2026-10-05（A78①）：已接线，但不走本字段** —— `ChatPanel` 不是 `MenuWindowBase` 族，
        ///     它是把软边**逐件传给 `MenuDraw`** 的（`ChatTab.VpSoft` → `ChatMessageRow.Build(clipSoft)`）；
        ///     断言 → `Editor/ShellScene.cs` ⑤·f。本行留着只为「逐处真值表」这一件事，⛔ 别照它去 `ChatPanel` 里设字段。
        ///     🔴 **2026-10-07（A78②）订正**：本字段**现在**在 `GameWindow` 上 ⇒ `ChatPanel` 也够得着了；
        ///     ⛔ 但**别顺手改它**（A78① 的裁定是「本窗不靠字段」，逐件传那条路已经是既有的、成立的做法）。
        ///   · 奖励窗 `Reward Window/Content/Scroll View/Viewport` = **(200,0)** ·
        ///     战役奖励窗 `Campaign Reward Window/Content/Scroll View/Viewport` = **(200,0)** ·
        ///     每日连击窗 `Daily Streak Popup/…/Rewards Scroll View/Viewport` = **(89,0)** ·
        ///     每日奖励窗 `Daily Reward Popup/Tracks/Rewards Scroll View/Viewport` = **(0,0)**
        ///   · 锻造奖励轨道（`…/Forge Tab/Rewards Scroll View/Viewport`）与战役轨道（`…/Campaign Track/Viewport`）
        ///     = **(0,0)**（硬边。⚠️ **两处的 `m_Padding` 不一样**：锻造轨道 = **(10,0,0,0)**、战役轨道 = **(0,0,0,0)**
        ///     —— 逐处实读 `d:/4/_tmp_view/q1_rm2d.txt:105,189`（锻造那两条路径）与 `:297-298`（战役））
        ///     🔴 **2026-10-05 订正（铁律 5）**：这一行原来写「`m_Padding` 是 (10,0,0,0) —— **我们没建模 padding**，
        ///     见报告」—— **两处都错**：① 那个非零值**只有锻造轨道有**（把一处推广到两处 = 铁律 5·c「一个值 ≠ 全部情况」）；
        ///     ② padding **已经建模且已经接线**（`ClipPad` + `MenuDraw.PaddedHitRect`），见下面那一节。
        ///   · 收藏窗各页 `Scroll View/Viewport`、选卡组窗 `Deck Scroll View/Viewport` = **(0,0)**
        ///   · 排行榜四棵的 `Content/Scroll View/Viewport` = (0,0)，而 `Ranking Display/Content/Army Selector/Viewport`
        ///     = **(42,0)**
        ///   ⚠️ **全库数量 = 222**（2026-10-05 独立复算 · 两法逐包同值 · 每包都数过）：
        ///     `bundle_menus_assets_all` **150** + `bundle_mainmenualwaysloaded_assets_all` **1**
        ///     + `bundle_generalgamewindows_assets_all` **5** + `bundle_scenes_scenes_mainmenuwarpforge` **1**
        ///     + 13 个 `bundle_scenes_scenes_battlearena*`（`1/2/3` 与 11 个阵营包）各 **5** = **65**；其余包 **0**。
        ///     **复现**（在 `d:/2/新解包资源/assets_full/` 下跑，把包名替进去；例：`bundle_menus_assets_all`）：
        ///     · 判据 A：`grep -rl 536591447201701790 bundle_menus_assets_all/MonoBehaviour | wc -l`（= 150）
        ///     · 判据 B：`grep -rl m_Softness bundle_menus_assets_all/MonoBehaviour | wc -l`（= 150）
        ///     🔴 **判据 A 认的是 `m_Script` 的 PathID，不是 guid** —— `RectMask2D` 的实例里写着
        ///     `m_Script: {m_FileID: 1, m_PathID: 536591447201701790}`（`m_FileID = 1` → `bundle_Waprforge_monoscripts`）；
        ///     拿工程本地 `com.unity.ugui` 那个 guid（`3312d7739989d2b4e91e6319e9a96d76`）去 grep 解包目录
        ///     **命中 0**（2026-10-05 实测）。
        ///     ⚠️ **必须限定到 `MonoBehaviour/`**：每个包的 `AssetBundle/AssetBundle_1.json`（包清单）里也含这个
        ///     PathID ⇒ 对整包 grep 会逐包多算 1（数出 151 / 2 / 6 / 6）。
        ///     🔴 **更正痕迹（铁律 5）**：
        ///     ① **2026-10-05 二次订正**：下面这次「改成 156」**订过头了** —— 全量表
        ///        `d:/4/_tmp_view/q1_rm2d.txt` **只扫了 3 个菜单族包**（它自己的三个表头就是 150 + 1 + 5 = **156**），
        ///        **222 才是全库数**；「战场场景 65」不是「查不到」，是**那张表从来没扫过** `battlearena*`。
        ///     ② **2026-10-05 一次订正（错，已推翻）**：曾按 `资料/普查产出_1004/W6审查_共用件.md` §F3
        ///        把 222 判成「没有出处」并改成 156；错因 = **把菜单族那三包当成了全库**（`bundle_scenes_scenes_battlearena*`
        ///        从未被 grep）。同一句错也复制在 `MenuDraw.cs` 的 `m_Padding` 段，**那边同步订正**。
        ///     ③ **更早那版**：「全库 222 个（菜单 150 / 通用弹窗 5 / 战场 65 / 主菜单 1）」—— **222 对，分项漏一项**：
        ///        那个「主菜单 1」指的是 `bundle_scenes_scenes_mainmenuwarpforge`，**漏的是**
        ///        `bundle_mainmenualwaysloaded_assets_all` 那 1 个（列出来的四项只有 **221**）。
        /// 🔴 **机制与代价** → `MenuDraw.ApplySoftEdges` 的注释（几何等效：按渐隐带内沿切开 + 逐顶点 alpha 斜坡）。</summary>
        public Vector2 ClipSoftness;

        /// <summary>**原版 `RectMask2D.m_Padding`** —— 与 `Clip` 配对。
        /// 🔴 **它两副面孔都改**（**2026-10-07 就地订正，铁律 5**）：本节原来写「**只改「点不点得到」，
        /// 不改「画到哪儿」**（判据 = 本地 UGUI `RectMask2D.cs:178-185`：那个字段全文件只用在
        /// `IsRaycastLocationValid` 一处，渲染那一面压根不读它）」—— **两句都错**，错因 =
        /// **只 grep 了 `RectMask2D.cs`**，而渲染那一面的裁剪算式在**另一个文件**
        /// `Runtime/UGUI/UI/Core/Culling/Clipping.cs:26-30`（`xMin + offset.x` / `xMax − offset.z` / …，
        /// 由 `RectMask2D.PerformClipping()` 调，见 `MenuDraw.PaddedClip` / `PaddedHitRect` 上面那段完整判据）。
        /// ⇒ 本壳**两份都过 padding，且两份缩的都是「mask 自己那个框」**：**渲染**那一份 = `RenderClip`
        /// （下面那个属性，喂 `Rect`/`Nine`/`Text`）· **命中区**那一份 = `AddHit` 把裸 `Clip` + `ClipPad`
        /// 转发给 `MenuDraw.Hit`，由它**把 `ClipPad` 缩在 `clip` 上**（**2026-10-08（A188）订正**：
        /// 原文写「缩命中区自己的矩形」= 错模型；实测代价 = 锻造轨道那一幕**原版 200.762 / 旧写法 190.762**）。
        /// 形状 = UGUI 的 `(x=Left, y=Bottom, z=Right, w=Top)`（画布像素）；**正值缩小、负值扩大**
        /// —— 符号**已坐实**（`[TODO-verify]` 已摘，判据同上）。
        /// 🔴 **2026-10-07（A78②）：声明处从 `MainMenuSubmenuWindow` 上移到本类**（其余逐字未改）。
        ///
        /// **怎么用**：谁设 `Clip` 谁顺手把它设对（与 `ClipSoftness` 同一条纪律）——
        /// 渲染那边**不用各页操心**（`RenderClip` 在转发时自己算），命中区那边由 `AddHit` 转发。
        /// ✅ **2026-10-05（A48 接线批）订正**：这里原来写「`ForgeTab` / `CampaignTab` 那两份自己的 `AddHit`
        /// 副本**还没转发**」—— **两份现在都已转调本方法**（`ForgeTab.cs` · `CampaignTab.cs` 各只剩一个
        /// 转发的同名薄包装），生产赋值两处：`ForgeTab.TrackPad` = **(10,0,0,0)**（原版实读的那两处锻造路径）·
        /// `CampaignTab` = `Vector4.zero`（战役轨道实读就是零，显式写出来是「本来就是 0」不是漏配）。
        /// 🔴 **今天全工程只有锻造轨道一处非零**；非该族的窗（`GameWindow` 族那几个 `RectMask2D`）实读**全是 (0,0)**。
        /// ✅ **2026-10-08（A188）本条已收口（原文是「⚠️ 还没查清」）**：`ClipPad` 非零而 `Clip` 为 null 时，
        /// 命中区**不再**被缩掉 —— pad 现在缩的是 `clip`，**没有 mask 就没有 pad**，
        /// 与原版「padding 长在 mask 组件上」的模型一致（`PaddedClip(null, pad)` 第一句就返 null，
        /// 见 `MenuDraw.Hit` 的 `maskPad` 注释）。今天生产上本来也没有这种站点（非零那处一定与 `Clip` 成对）。</summary>
        public Vector4 ClipPad;

        /// <summary>🔴 **渲染那一份裁切** = `Clip` 按原版 `RectMask2D.m_Padding`（`ClipPad`）**内缩**
        /// （**正 = 缩小**；`ClipPad` 全 0 时**与 `Clip` 逐字段相同**）。**喂给所有渲染/建节点的口子**：
        /// `Rect` / `Nine` / `Text` / `TextBox`（`MenuWindowBase` 那几个包装）+ `DrawRect` / `DrawNine`（本类）。
        /// 判据 + 退化那一支的处置 → `MenuDraw.PaddedClip` 的注释（UGUI `Clipping.FindCullAndClipWorldRect`）。
        /// ⛔ **命中区【不要】用这个属性**（保持「裸 `Clip` + `ClipPad`」那一份转发 —— 全族唯一入口 = `AddHit`）：
        /// `MenuDraw.Hit` 自己会拿 `PaddedClip(clip, maskPad)` 把 pad **缩在 `clip` 上**
        /// （**2026-10-08（A188）订正**：原文写「由 `MenuDraw.Hit` → `PaddedHitRect` 缩**命中区自己的矩形**」=
        /// 旧模型；两条路现在缩的是**同一个框**，只是拿去做的事不同）。见 `ClipPad` 的注释。
        /// ⚠️ **它只覆盖本类与 `MenuWindowBase` 那几个包装**：谁把裸 `Clip` 直接传给
        /// `MenuDraw.Rect/Nine/Tiled/ClipText`（绕开包装），谁就绕过了 padding —— 现读全工程**没有这种站点**
        /// （唯一非零 pad 的 `ForgeTab` 全部走 `_win.Rect` / `_win.TextBox` / `_win.Nine`；`CampaignTab` 那处 pad 本来就是 0）。
        /// 🔴 **2026-10-07（A78②）**：本属性原来在 `MainMenuSubmenuWindow` 上 —— 那时只有那一族够得着
        /// `Clip`/`ClipPad`，等于把「同一个视口的三件套」按家族劈成了两半；现在声明在本类，全族共享同一份。</summary>
        public PxRect? RenderClip { get { return MenuDraw.PaddedClip(Clip, ClipPad); } }

        /// <summary>**渲染转发**（`GameWindow` 族用；`MenuWindowBase` 那几个吃**图名**的包装是另一层）：
        /// 把本窗的裁切状态喂给 `MenuDraw.Rect` —— 裁切框走 `RenderClip`（含 `ClipPad`）、软边走 `ClipSoftness`。
        /// 🆕 **2026-10-07（A78②）**：这一份是给**任意一扇 `GameWindow` 族的窗**用的 —— 在那之前
        /// 「非零 `m_Padding` / 非零 `m_Softness`」这两样**只有 `MenuWindowBase` 家族喂得到**
        /// （字段长在那个子类上）⇒ 别的窗要用就只能自己抄一份字段 + 一段转发。
        /// ⛔ **别再在某一扇窗里抄一遍这两个转发**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// ⚠️ `clip` 那份**由本窗的 `Clip` 字段给**（不是一个形参）：本族的视口归各窗自己管，
        /// 设 `Clip` / `ClipSoftness` / `ClipPad` 就跟 `MenuWindowBase` 家族完全同一套纪律。</summary>
        public ImageQuad DrawRect(Transform parent, Texture2D tex, PxRect r, string name, int q,
                                  Color? tint = null, bool keepAspect = false)
        {
            return MenuDraw.Rect(parent, tex, r, name, q, tint, keepAspect, RenderClip, ClipSoftness);
        }

        /// <summary>同上，`MenuDraw.Nine`（原版 `Image.Type = Sliced` 的九宫格）那一份 —— 参数表照
        /// `MenuDraw.Nine` 去掉 `clip` / `clipSoftness`（那两样走本窗的状态）。🆕 2026-10-07（A78②）。</summary>
        public GameObject DrawNine(Transform parent, Texture2D tex, PxRect r, Vector4 border,
                                   float texW, float texH, int q, Color? tint = null, bool fillCenter = true,
                                   string name = "Nine", Vector4? borderOutPx = null)
        {
            return MenuDraw.Nine(parent, tex, r, border, texW, texH, q, tint, fillCenter, name,
                                 borderOutPx, RenderClip, ClipSoftness);
        }

        /// <summary>🔴 **命中区转发**（全工程唯一一份规则：**裸 `Clip` + `ClipPad`** → `MenuDraw.Hit`）——
        /// 一个**透明点击区**（整块矩形）+ `WindowButton`，返回那个节点。
        /// 原版这一层就是按钮自己的 `RectTransform`；我们这套没有 uGUI 事件 ⇒ 单独一个透明 quad 当命中区
        /// —— **`PointerLayer` 扫的就是它**（`GetComponentInChildren&lt;ImageQuad&gt;()` 拿矩形）。
        /// 🔴 2026-09-24：这段原来只有 `MainMenuSubmenuWindow.AddHit` 一份，新的活动窗/搜索弹窗也要
        /// ⇒ 收口到 `MenuDraw.Hit`，那边**转调**。
        /// 🔴 **2026-10-07（A78②）：本方法从 `MainMenuSubmenuWindow` 上移到本类**（签名逐字未动 ⇒
        /// 那一族所有调用点解析到同一份实现、行为一字不变）。**为什么必须上移**：它转发的那两样
        /// （`Clip` / `ClipPad`）本来就是全族的状态，方法留在家族里 = 「每扇窗各抄一段」那个缺口的形状。
        /// 🔴 **`Clip` 生效时**：视口外的点击区**不建**（返回 null）、压在边上的**截到视口内**
        /// —— 判据 = 原版 `RectMask2D` 的射线那一面（`MenuDraw.ClipRect` 的注释里有出处）。
        /// 🆕 **`ClipPad` 也转发** —— 转发的是**裸 `Clip` + `ClipPad`**，由 `MenuDraw.Hit` **把 `ClipPad` 缩在 `clip` 上**
        /// （判据与符号约定 → `MenuDraw.PaddedClip` 上面那一段）。🔴 **2026-10-08（A188）就地订正（铁律 5）**：
        /// 本行原文写「由 `MenuDraw.Hit` 缩**命中区自己的矩形**（`PaddedHitRect`）」= **错模型**
        /// —— pad 缩的是 **mask 自己那个框**，命中区 = `R ∩ (V−pad)`（判据 = 两道射线关，见 `Hit` 的 `maskPad` 注释）。
        /// 🔴 **2026-10-07 就地订正（铁律 5）**：本节原文写「padding **只改射线那一面**⇒ 渲染那一份
        /// （`Rect`/`Nine`/`Text`）**照旧不吃它**」—— **渲染那一面也吃**（UGUI `Clipping.FindCullAndClipWorldRect`），
        /// 且**吃法与本条相同**：两处缩的都是 `V − pad`（渲染那一份取 `RenderClip`、命中那一份由 `MenuDraw.Hit` 现算）。
        /// ✅ **2026-10-05（A48 接线批）**：`ForgeTab` / `CampaignTab` 两份自己的 `AddHit` 副本**都已改成转调这里**
        /// （`ForgeTab.cs:791-793` · `CampaignTab.cs:763-765`）⇒ 那两页现在也吃 `Clip` 与 `ClipPad`；
        /// 端到端断言 → `Editor/RewardsScene.cs` 锻造轨道那一段（居中那三条 + **跨边那一条**，后者是唯一能
        /// 分辨「pad 缩 `clip`」与「pad 被丢掉」的）与战役轨道那一条（`m_Padding = 0` ⇒ 命中区不许被缩）。
        /// ⚠️ **原文里那句「`RewardsScene.cs:1511,1513`（命中宽 190.762 = 原版 200.762 − pad.L 10、左边缘 +10）」
        /// 已过期**（那是旧模型的值；现期望 = **200.762 / 左沿 = 按钮左沿**）。
        /// ⚠️ **`onClick` 传 null 是合法的**（几处自检就是这么用的：只量命中区、不接行为）。</summary>
        public Transform AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                                ImageQuad target = null, string art = null,
                                string hoverArt = null, string pressedArt = null)
        {
            return MenuDraw.Hit(parent, name, r, q, onClick, target, art, hoverArt, pressedArt, Clip, ClipPad);
        }

        /// <summary>摆一段字，**吃本窗的裁切**（`RenderClip` / `ClipSoftness`）—— 原版 `RectMask2D` 对文字一视同仁：
        /// ① **整块**在视口外 ⇒ **不建**（`MenuDraw.Visible`）；② 压在视口边上 ⇒ **裁**
        /// （`MenuDraw.ClipText`：逐字夹顶点 + 按同一仿射改 uv）。参数表 = `MenuDraw.Text` + 本窗那一套裁切
        /// + `align`（**0 = 居中（`Label` 默认）· 1 = 左 · 2 = 右**；原版 TMP 的 `m_HorizontalAlignment`）。
        /// <para>🔴 **对齐必须在 `ClipText` 之前** —— `ClipText` 夹的是**世界坐标**的顶点，先裁再挪会把裁好的块
        /// 挪出框（`CampaignRewardWindow` 那颗 `Warning` 原版就是 `Right`，原来写成「建完再 `MenuDraw.AlignRight`」
        /// ⇒ 已改）。</para>
        /// <para>🆕 **2026-10-11（A241）**：本方法原来在 <b>三</b> 个文件里各有一份**逐字相同**的副本
        /// （`Shell/DailyStreakPopup.cs` · `Shell/CampaignRewardWindow.cs` · `Shell/InboxWindow.cs`）——
        /// 三扇都是 `GameWindow` 直系、**够不到** `MenuWindowBase` 家族的 `MainMenuSubmenuWindow.Text`
        /// （那一份签名/语义**不同** —— 走 `TextBox` + `wrap`/`alignLeft` 两个 bool、且求交走 `ClipRect`
        /// —— 是**有意留在那一支**的，⛔ 别顺手并进来），而共同基类 `GameWindow` 当年不在那几件的白名单里
        /// ⇒ 各自就地抄了一份。现在**只留这一份**
        /// （判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §六·6 · `资料/待办判据_1008.md` §A241）。</para>
        /// <para>⚠️ **两处如实标注**：① `align` 是那三份的**超集** —— `DailyStreakPopup` 那一份**没有**它
        /// （它一个调用点都不传）⇒ 上移后**行为逐字等价**（`align` 缺省 0 = 那一档什么都不做）；
        /// ② 可见性从三份的 `private` 变成 `protected`（三份的调用点全在各自类里 ⇒ 调用点一个没改）。
        /// ⚠️ 祖先那一层还有别的 `Text` 重载（`MenuWindowBase.Text` / `SocialPage.Text` / `PlayerProfileWindow.Text`…）
        /// —— 参数表各不相同 ⇒ 是**重载**不是覆盖，各自照旧解析到自己那一份。</para></summary>
        protected Label Text(Transform parent, PxRect r, string s, Color color, string name, float fontPx, int q,
                             float wrapPx = 0f, float autoMinPx = 0f, int align = 0)
        {
            if (!MenuDraw.Visible(r, RenderClip)) return null;
            var lb = MenuDraw.Text(parent, r, s, color, name, fontPx, q, wrapPx, autoMinPx);
            if (lb == null) return null;
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
            if (RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
            return lb;
        }

        public WindowState CurrentState { get; private set; }
        public object Data { get; private set; }
        public WindowsManager Manager { get; internal set; }

        /// <summary>有没有**显式赋过** `placement`（默认值是哨兵 `UnsetPlacement`，见那边的注释）。</summary>
        public bool HasPlacement { get { return (int)placement >= 0; } }

        /// <summary>原版叫 `TryOpen`：只有它做「播音 → 激活 → 进 Open 态」这一串。</summary>
        public bool TryOpen(object data)
        {
            SetupData(data);
            if (openSound != null) WFSoundPlayer.Play(openSound, true, 1f, 1f);   // = 原版 `SoundManager.Play2D(..., MixerType.FX)`
            gameObject.SetActive(true);                                           // 建场景时窗口是关着的（照原版）
            CurrentState = WindowState.Open;
            Open();
            ApplySmallScreenScale();   // 🆕 A165 —— 原版这一段写在 `GameWindow.Open()` 里，见方法注释
            return true;
        }

        /// <summary>🆕 **A165**：原版 `GameWindow.Open()` 的**那一段**（逐句 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头）：
        /// <code>
        /// if (GameStaticData.smallScreenUI) {
        ///     if (!Mathf.Approximately(extraScaleSmallScreen, 1f)) {
        ///         (GetComponent&lt;TransformScalerBySmallScreenUI&gt;() ?? gameObject.AddComponent&lt;TransformScalerBySmallScreenUI&gt;())
        ///             .SetScale(extraScaleSmallScreen);
        ///     }
        /// }
        /// </code>
        /// <para>🔴 **为什么挂在 `TryOpen` 而不是 `Open()`**：原版 `Open()` 是虚方法、且**基类那一份**才做这件事；
        /// 我们的 `Open()` 被各扇窗覆写（`SettingsWindow.Open` / `TrophyInfoPopup.Open` / …），**没有一处调 base**
        /// ⇒ 写进 `Open()` 等于对绝大多数窗**不生效**（静默）。`TryOpen` 是本工程**唯一的开窗入口**
        /// （`WindowsManager.OpenWindow` → 它），语义等价。⚠️ 不是原版机制的替代品：prefab 里**烤着**组件的窗
        /// （我们这边是 `TrophyInfoPopup`）靠自己的 `OnEnable`/`LateUpdate` 起作用，这里只是**补上 AddComponent 与覆盖**这一支。</para>
        /// <para>⚠️ **开关关着时连 `AddComponent` 都不做**（照原版）；但**已经存在**的组件要补一次 `Initialize()` ——
        /// 我们的组件是代码建的、而且窗 GO 常常一直 active（`OnEnable` 不会再跑）⇒ 不补这一次，
        /// 开关被改过之后重开的窗会**停在旧状态**（静默）。</para></summary>
        void ApplySmallScreenScale()
        {
            var sc = GetComponent<TransformScalerBySmallScreenUI>();
            if (sc == null)
            {
                if (!SmallScreenUI.Enabled) return;    // 原版第一层判据（与窗口宽/屏宽无关）
                sc = gameObject.AddComponent<TransformScalerBySmallScreenUI>();
            }
            if (SmallScreenUI.Enabled && !Mathf.Approximately(extraScaleSmallScreen, 1f))
                sc.SetScale(extraScaleSmallScreen);    // = 原版那一支：**覆盖** prefab 烤的 menuScale
            else
                sc.Initialize();                       // 否则按「不覆盖」语义重算 enabled（prefab 烤的值原样生效）
        }

        protected virtual void SetupData(object data) { Data = data; }

        /// <summary>子类在这里铺自己的内容。基类什么都不做（原版 `Open()` 也是虚方法）。</summary>
        public virtual void Open() { }

        /// <summary>被更高优先级的窗压到背景（原版 `ToBackground()`）。</summary>
        public virtual void ToBackground() { CurrentState = WindowState.Background; }

        /// <summary>🆕 **A217③**：原版 `GameWindow.Hide()` —— **虚表 Slot 9 = `0x1c8`**
        /// （`d:/2/tools/il2cpp_out/dump.cs` 的 `GameWindow` 一节：`Slot: 9`、VA `0x180835B60`，与该 .c 文件对得上），
        /// 逐句 = `GameWindow__Hide.c`：
        /// <list type="number">
        /// <item>`Component.get_gameObject(this) == null` ⇒ return（那两个 null 判据是 IL2CPP 的样板）；</item>
        /// <item>**`!gameObject.activeSelf` ⇒ return**（首句守卫）；</item>
        /// <item>`CurrentState = Closed`（`*(undefined4 *)(param_1 + 0x68) = 0`）；</item>
        /// <item>`gameObject.SetActive(false)`；</item>
        /// <item>末尾 `OnCloseWindow`（字段 `0x60`）非空就 Invoke（顺手把它换成一份缓存委托）。</item>
        /// </list>
        /// <para>🔴 **它【不动】`WindowsManager` 的任何字段/表**（原版就是不动的 —— 它**不调** `CloseWindow`）：
        /// 藏起来的窗**仍留在 `openWindows` 里**、只是 `state == Closed` + 物体 inactive。
        /// 这正是原版 `HideAllWindows` 判据的形状（predicate `b__49_0` = `存活 && *(int*)(w+0x68) != 0`
        /// ⇒ **只藏 state≠Closed 的**），也是「全屏窗开时把弹窗一起藏掉、上面那扇关掉之后它们还能回来」
        /// 那条链的前提（回来 = `ShowPreviousWindow` 认**列表尾** → `UnHide`/`ReopenFromBackground`）。</para>
        /// <para>⚠️ **两处如实标注**：① 第 5 步那个 `OnCloseWindow` 事件**我们没有**（全仓 `OnCloseWindow`
        /// 零命中）⇒ 不实现；② 与我们的 `Close()` 是**两条路**（原版 `Close` → `manager.CloseWindow` →
        /// `CloseWindowCO` → **先调 Slot 9 `Hide()`** 再摘表/销毁；我们 `Close()` 自己做了 state/物体那两步、
        /// `NotifyClosed` 做记账）—— 两条路在「state=Closed + 物体 inactive」上等价，差别只在**记不记账**。</para></summary>
        public virtual void Hide()
        {
            if (gameObject == null) return;
            if (!gameObject.activeSelf) return;
            CurrentState = WindowState.Closed;
            gameObject.SetActive(false);
        }

        public virtual void Close()
        {
            // 🆕 **A217①**：原版 `GameWindow__Close.c` 的**第三句**（前两句是两个 null 判据）：
            //   `if (!gameObject.activeSelf) return;` —— 我们原来缺这一句。
            // 🔴 **为什么补了它还要出声**：没有守卫时，「本来就没开」的窗调 `Close()` 会**走完整条记账**
            //   （摘表 + 可能触发 `ShowPreviousWindow` + 清 `Data`）—— 那是一层**兜底**；加了守卫，
            //   兜底就没了 ⇒ 万一有人**绕过 `Close()`/`Hide()`** 直接把物体置灰了（那条路没有记账），
            //   跳过就等于**静默留脏** ⇒ 违「不许静默失败」那条铁律 ⇒ **出声**。
            //   🔴 **出声的判据必须比「不活 + 在表里」更窄**（这是 A217① 与 ③ 的交叉点，2026-10-11 落地时定的）：
            //   A217③ 引入 `Hide()` 之后，「物体不活、**但仍留在 `openWindows` 里**」是**合法状态**
            //   （`HideAllWindows` 藏起来的窗就是这样，原版也一样 —— `Hide()` 不摘表）⇒ 那一档**不该出声**
            //   （`CloseAllWindows()` 里逐扇 `Close()` 撞上的就是它，原版那一刻也是**静默早退**）。
            //   ⇒ 出声的条件 = **`CurrentState` 还不是 `Closed`**（= 有人绕过两条路改了激活态）。
            // ⚠️ **今天生产路径零个站**会在「本来就没开」时调 `Close()`：全壳唯一把窗物体置 inactive 的
            //   地方就是 `Close()`（尾句）与 `Hide()`，两者都**同时**把 `CurrentState` 写成 `Closed`
            //   ⇒ 加它**零可观测差异**（那条出声也一声不响）。断言 → `Editor/ShellScene.cs` 的 A217 那一节
            //   （⑤ 那三条：脏现场出声 / 合法藏起来的**不**出声）。
            // ⚠️ 四处 `Close` 覆写（`SearchingOpponentWindow.ReleaseHold` / `LiveOpsEventWindow` 取消搜索 /
            //   `MissionRerollPopup` 换回页签 / `SettingsWindow.ApplyIfDirty`）都跑在 `base.Close()`**之前**
            //   ⇒ 守卫加在基类里**不会**掐掉它们（它们那些副作用照旧跑，只是基类那两步不跑）。
            if (!gameObject.activeSelf)
            {
                if (CurrentState != WindowState.Closed && Manager != null && Manager.openWindows.Contains(this))
                    Debug.LogWarning($"[Win] `{name}`：`Close()` 打在一扇**已经不活**（`activeSelf == false`）"
                        + "**却既不在 `Closed` 态、又还挂在 `openWindows` 里**的窗上 —— 照原版 `GameWindow.Close` "
                        + "首句**直接 return**，所以这一下**没有摘表**、`CurrentState`/`Data` 也**原样**。"
                        + "这个组合只可能来自「绕过 `Close()`/`Hide()` 直接改物体的激活态」（那条路没有记账）"
                        + "⇒ **别把它当成「已经关掉了」**。"
                        + "（⚠️ `Hide()` 藏起来的窗**不算**这一档：它的 state 是 `Closed`、按原版就该留在表里。）");
                return;
            }
            CurrentState = WindowState.Closed;
            Data = null;
            if (Manager != null) Manager.NotifyClosed(this);
            gameObject.SetActive(false);
        }

        /// <summary>🆕 A77㉑①：**这一扇窗现在是不是「开着」**（原版 `GameWindow.IsOpen()`，虚表 **Slot 12 / 0x1f8**）。
        /// 原版逐句 = `return *(int *)(this + 0x68) == 1;`（`d:/2/tools/decomp_full/GameWindow__IsOpen.c`）——
        /// `0x68` = `CurrentState`、那个 `1` = `WindowState.Open`
        /// （枚举值已按原版对齐，见 `WindowState` 那段订正）。
        /// <para>**谁用它**：原版 `WindowsManager.Update` 的 ESC 那一段 —— 先问本方法，为 `false` **直接 return**、
        /// 为 `true` 才调 `ESCPressed()`（`d:/2/tools/decomp_full/WindowsManager__Update.c`：虚表 `0x1f8` 那一跳
        /// 返回 0 就 `return`，否则调 `0x1e8`）。我们的等价物 = `PointerLayer.KeyCancel` 的第一跳。</para>
        /// <para>⚠️ **别说它「恒真」**：窗可以自己 `ToBackground()`（原版 `OpenWindowCO` 对底窗那一句就是这么干的）
        /// ⇒ 那时它**仍是** `currentWindow` 却不在 `Open` 态，原版那一刻按 ESC **什么都不做**。</para></summary>
        public virtual bool IsOpen() { return CurrentState == WindowState.Open; }

        /// <summary>🆕 A77㉑①：**把一扇已经在场的窗带回 `Open` 态**（= 顶窗关掉之后，原来被压到背景的那一扇）。
        /// 原版这一跳散在两个方法里，逐句：
        /// <list type="bullet">
        /// <item>`GameWindow.TryOpen(data, options)` 的**第一段**（反汇编 VA `0x180836120`，逐条读出来）：
        ///   `SetupData`(虚表 0x188) → `esi = [rbx + 0x68]`（`CurrentState`）→ `test esi,esi; je <完整开启那一支>`
        ///   ⇒ 已经开着 / 在背景时只做 `if (CurrentState != Open) { CurrentState = Open; ToFocus(); }` 然后返回 ——
        ///   **不播开窗音、不 `SetActive`、不重建内容**。</item>
        /// <item>`GameWindow.UnHide()`（VA `0x180836280`）：`ToBackground(); gameObject.SetActive(true);`
        ///   —— 含义是「**重新显示出来、但置于背景**」（`ShowPreviousWindow` 在列表尾是弹窗时用它）。</item>
        /// </list>
        /// <para>🔴 **为什么单列一个方法、不并进我们的 `TryOpen`**：我们仓里 `TryOpen` 今天另外承担着
        /// 「同一扇窗再开一次会**重建内容**」的用法（`Shell/CardDetailPopup.cs:243` 的 `ShowCard` →
        /// `Manager.OpenWindow(this)` → `TryOpen` → `Open()` → `Build()`）—— 把原版那一段并进去，
        /// 「换一张卡」会变成**不换**（静默）。那是我们的既有偏离（**已记账**，不归 A77㉑ 这一件），
        /// 所以原版那支单独成方法，`ShowPreviousWindow` 只用这一份。</para>
        /// <para>⚠️ 原版那支后面还有一句 `ToFocus()`（虚表 Slot 7）—— 本仓**没有这个虚方法**（全库零处），
        /// 而原版 `GameWindow__ToFocus.c` 本身是空的（`decomp_full` 已知的错桩之一）⇒ 无对应物，如实记。</para></summary>
        public void ReopenFromBackground()
        {
            gameObject.SetActive(true);            // = 原版 `UnHide()` 的第二句（顶窗被 `Hide()` 过时要能回来）
            CurrentState = WindowState.Open;       // = 原版 `TryOpen` 非 `Closed` 支那唯一一句写入
        }

        /// <summary>🆕 A77㉑①：原版 `GameWindow.UnHide()` **逐句照搬** ——
        /// `ToBackground(); gameObject.SetActive(true);`（VA `0x180836280`：先虚调 Slot 10 `ToBackground`、
        /// 再 `SetActive(true)`；两者之间没有任何条件）。
        /// <para>谁用它：`WindowsManager.ShowPreviousWindow` —— 列表尾是弹窗时，把「最后一个非弹窗窗」
        /// 重新显示出来（原版 `WindowsManager__ShowPreviousWindow.c` 里那次 `GameWindow__UnHide` 调用）。</para></summary>
        public void UnHide()
        {
            ToBackground();                        // 原版 Slot 10；本仓 = `CurrentState = Background`
            gameObject.SetActive(true);
        }

        /// <summary>🆕 A49：**ESC 打在这一扇窗自己身上**（原版 `GameWindow.ESCPressed()` 是个虚方法 ——
        /// `MainMenuWindow` 覆写成「再叫出退出游戏的弹窗」（`MainMenuWindow__ESCPressed.c`）、
        /// `DeckEditingWindow` 覆写成「先存卡组」（`DeckEditingWindow__ESCPressed.c`））。
        ///
        /// 🔴 **门槛是两道、按序**（反汇编 VA `0x180835ab0` 逐条读出来；`d:/2/tools/decomp_full/GameWindow__ESCPressed.c`）：
        /// <list type="number">
        /// <item>**`EventSystemController.Instance.eventSystem.enabled`** ——
        ///   `call 0x181258290`（`SingletonBehaviour&lt;EventSystemController&gt;.get_Instance`，类指针 = `DAT_1842bdc50`）
        ///   → `mov rcx,[rax + 0x20]`（= `EventSystemController.eventSystem`，`dump.cs` 实读）
        ///   → `call 0x182feb6f0`（= `Behaviour.get_enabled`）→ `test al,al; je <return>`
        ///   ⇒ **输入系统没启用 ⇒ ESC 不关窗**。
        ///   🔑 **认那个类的两处交叉验证**（别的 .c 里把同一个槽当 `this` 传给 `EventSystemController` 的方法）：
        ///   `CombatCameraZoom__HandleManualControl.c` → `IsPointerOverUIObject` ·
        ///   `EverguildTooltipManager__Update.c` → `GetPointerEventData`
        ///   ⇒ ⛔ **别信「0x20 上挂着 Behaviour」那种形状**（`AlliancesManager` 在 0x20 上也挂了个 `MonoBehaviour`）。</item>
        /// <item>**`closeOnESC`**（0x39）—— `cmp byte ptr [rbx + 0x39],0; je &lt;return&gt;`
        ///   （`GameWindow__ToggleESC.c` 写的就是 `param_1 + 0x39`；字段序旁证：`type` 在 0x20、
        ///   `useDefaultCloseSoundIfNull`(默认 true) 在 0x38、`closeOnESC` 在 0x39、`updateNavPanel` 在 0x3a、
        ///   `extraScaleSmallScreen`(默认 1.0f) 在 0x3c、`CurrentState` 在 0x68）。</item>
        /// </list>
        /// 两道都过 ⇒ `Close()`（虚表 0x1b8）+ `updateNavPanel`(0x3a) 那一支。
        /// ⇒ **`closeOnEsc == false` 的窗按 ESC 什么都不做** —— `MissionRerollPopup`(0) / `PromptPopup`(0) /
        /// `RewardsWindow`(0) / `DailyStreakPopup`(0) / `BoosterPackOpenWindow`(0) 都属这一档，
        /// **那不是缺陷，是原版的值**。
        /// <para>🔴 **第一道门槛在我们这边的等价物**：原版读的是 UGUI `EventSystem.enabled`，而本仓
        /// **没有 UGUI `EventSystem`**（唯一那条输入路 = `PointerLayer`，见 `Shell/PointerLayer.cs` 文件头）
        /// ⇒ 用 `PointerLayer.InputEnabled` 当它（**我们挑的等价物**，那一位的注释里有完整判据与来源）。</para>
        ///
        /// ⚠️ 原版返回 `void`；这里返回「**有没有真的关掉**」给输入层记账（唯一调用点 = `PointerLayer.KeyCancel`）
        /// —— **这一条是我们加的**（如实标注，铁律 3）。</summary>
        public virtual bool ESCPressed()
        {
            // ① 原版第一道门槛：输入系统开着没有（= `EventSystemController.Instance.eventSystem.enabled`）
            if (!PointerLayer.InputEnabled)
            {
                // 出声（红线：不许静默失败）—— 「按了没反应」要么是缺陷、要么是原版这个值，必须能从日志里分辨
                Debug.Log($"[Win] `{name}`：ESC 不关窗 —— **输入层当前是停用的**" +
                          "（原版 `GameWindow.ESCPressed` 的第一道门槛 = `EventSystemController.Instance.eventSystem.enabled`）");
                return false;
            }
            // ② 原版第二道门槛：`closeOnESC`（字面读出来的 0x39，见上面那段）
            if (!closeOnEsc)
            {
                // 出声（红线：不许静默失败）—— 「按了没反应」要么是缺陷、要么是原版这个值，必须能从日志里分辨
                Debug.Log($"[Win] `{name}` 的 `closeOnEsc = false`（原版值）⇒ **ESC 不关这扇窗**" +
                          "（照 `GameWindow__ESCPressed` 的第二道判定）");
                return false;
            }
            Close();
            return true;
        }
    }

    /// <summary>
    /// 窗口管理器单例。**名字照原版（`WindowsManager`，注意有个 s；全工程没有 `WindowManager` 这个类）**。
    /// 挂在 `Shell` 场景里的常驻对象上。
    /// </summary>
    public class WindowsManager : MonoBehaviour
    {
        public static WindowsManager Instance { get; private set; }

        /// <summary>锚点表。原版是 `static Dictionary<WindowsPlacement, Transform> anchors`。</summary>
        static readonly Dictionary<WindowsPlacement, Transform> _anchors =
            new Dictionary<WindowsPlacement, Transform>();

        public readonly List<GameWindow> openWindows = new List<GameWindow>();
        public GameWindow currentWindow;
        public GameWindow popUpWindow;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // 指针层与窗口管理器**同生共死**：任何开窗路径都会经过这里
            //（真鼠标的点击/滚轮全走它；自检也能直调 `PointerLayer.ClickAt/WheelAt` 验「这一处吃不吃得到输入」）。
            // ⚠️ 放在 `Awake` 而不是 `EnsureHost`：自检是**直接 `AddComponent<WindowsManager>()`** 建的
            //    （不走 `EnsureHost`）—— 2026-09-23 就因为放在 `EnsureHost` 里，自检报「`PointerLayer` 不在场景里」。
            PointerLayer.Ensure(transform.parent);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// **确保场景里有 `WindowsManager` 与三个锚点**（幂等）。原版这三个 Holder 在主菜单场景里
        /// （`1 - Below Upper Bar Holder{10}` / `2 - Canvas Holder Above upper bar{5}` / `3 - PopUp Holder{15}`，
        /// **缺一不可**；正本 §三 第 7 条）。
        /// 🔴 **判据只留这一份**：壳（`ShellRuntime`）与「单独打开某个界面场景按 Play」都走它 ——
        /// 两处各建一次 = 迟早不一致（CLAUDE.md §三）。名字与 placement 都照原版。
        /// </summary>
        public static WindowsManager EnsureHost(Transform root = null)
        {
            // 指针层与窗口管理器**同生共死**：任何开窗路径都会经过这里
            //（真鼠标的点击/滚轮全走它；自检也能直调 `PointerLayer.ClickAt/WheelAt` 验「这一处吃不吃得到输入」）
            PointerLayer.Ensure(root);

            if (Instance != null) return Instance;

            var holderRoot = new GameObject("Window Anchors").transform;
            if (root != null) holderRoot.SetParent(root, false);
            MakeHolder(holderRoot, "1 - Below Upper Bar Holder", WindowsPlacement.World);
            MakeHolder(holderRoot, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);
            MakeHolder(holderRoot, "3 - PopUp Holder", WindowsPlacement.Popup);

            var go = new GameObject("WindowsManager");
            if (root != null) go.transform.SetParent(root, false);
            var wm = go.AddComponent<WindowsManager>();
            // 🔴 **2026-09-24 修**：这里必须**显式登记 `Instance`** —— 它原来只在 `Awake()` 里赋，
            //    而**编辑模式（自检）不跑 `Awake`** ⇒ 建完 `Instance` 还是 null：
            //      ① 任何用 `WindowsManager.Instance` 的代码**静默失败**（实测：模式卡的 `OpenMode` 报「没有 WindowsManager」）；
            //      ② 再调一次 `EnsureHost` 会**又建一台 + 又一套锚点**（`Instance` 仍是 null ⇒ 判不出「已经有了」）。
            //    同族先例就在本文件：`WindowHolder` 正是因此才有 `RegisterNow()`（见下面 `MakeHolder` 的注释）。
            Instance = wm;
            Debug.Log("[Win] 场景里没有 `WindowsManager` ⇒ 现建了一台 + 三个锚点（单独打开界面场景时走这条路）");
            return wm;
        }

        static void MakeHolder(Transform parent, string name, WindowsPlacement p)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var h = t.gameObject.AddComponent<WindowHolder>();
            h.placement = p;
            // ⚠️ 先赋字段**再**注册（`OnEnable` 在 `AddComponent` 那一刻就跑过了，那时 placement 还是 None）
            h.RegisterNow();
        }

        // ---------------------------------------------------------- 锚点

        public static void RegisterAnchor(WindowsPlacement p, Transform t)
        {
            if (p == WindowsPlacement.None)
            {
                Debug.LogError($"[Win] `WindowHolder` 挂在 `{t.name}` 上，但 placement = None —— " +
                               "原版这个字段是必填的（None 只用于「不挂 Holder」的窗口，如 MainMenuWindow）");
                return;
            }
            if (_anchors.TryGetValue(p, out var cur))
            {
                if (cur == t) return;                       // 同一节点重复注册（建场景 + 运行时 OnEnable 各一次）—— 正常
                // 上一次建场景留下的**死引用**（对象已销毁）⇒ 直接顶掉，这不是冲突
                if (cur != null)
                    Debug.LogError($"[Win] 锚点 {p} 被两个**活着的**节点抢：`{cur.name}` 与 `{t.name}` —— 原版是静态表，后注册的会顶掉前一个");
            }
            _anchors[p] = t;
        }

        public static void UnregisterAnchor(WindowsPlacement p, Transform t)
        {
            if (_anchors.TryGetValue(p, out var cur) && cur == t) _anchors.Remove(p);
        }

        /// <summary>取锚点。**取不到要报出来**（照原版 `CustomDebug.LogError`）—— 静默返回 null 会让窗口建到场景根上。</summary>
        public static Transform GetWindowAnchor(WindowsPlacement p)
        {
            if ((int)p < 0)
            {
                // A166：哨兵只该在 `AttachToAnchor` 里被拦下并出声（那里会带上窗口名）。走到这里 = 有人绕过了它。
                Debug.LogError("[Win] `GetWindowAnchor(哨兵)` —— 这是 `GameWindow.placement` **没显式赋值**时那个默认值，" +
                               "不是任何一档锚点。原版这个字段必填（141/141 都有值）。");
                return null;
            }
            if (_anchors.TryGetValue(p, out var t) && t != null) return t;
            Debug.LogError($"[Win] 找不到 {p} 的锚点 —— 场景里缺对应的 `WindowHolder`。" +
                           "主菜单那一层需要 10 / 5 / 15 三个都齐（正本 §三 第 7 条）");
            return null;
        }

        public static bool HasAnchor(WindowsPlacement p) => _anchors.TryGetValue(p, out var t) && t != null;

        public static void ClearAnchorsForTest() { _anchors.Clear(); }

        // ---------------------------------------------------------- 开 / 关

        /// <summary>照原版 `OpenWindowCO` 的判定顺序。
        /// <para>🔴 **2026-10-11（A217③）就地改了两处**（判据 = `d:/2/tools/decomp_full/WindowsManager__OpenWindowCO.c`
        /// 逐句 + 调度台裁定 `资料/普查产出_1010/调度台_口径裁定_1011.md` §A217）：
        /// <list type="number">
        /// <item>**全屏支**：原来是 `currentWindow.Close()`（只收掉当前主窗）⇒ 照原版改成
        ///   `if (openWindows.Count > 0) HideAllWindows();` —— 原版这一句**连弹窗一起藏**
        ///   （`HideAllWindows` 的 predicate 只排除 `state == Closed` 的窗）。
        ///   ⚠️ 这是**可观测的行为变化**：原来「开全屏窗 ⇒ 底下那扇被 `Close()` 掉、出表」，
        ///   现在「被藏起来、**仍在表里**」⇒ 关掉上面那扇时 `ShowPreviousWindow` 会把它**带回来**
        ///   （原版就是这么退回去的）。</item>
        /// <item>**弹窗支之外统一写 `currentWindow`**：原版 `set_CurrentWindow(param_1, param_2)` 写在
        ///   if/else **之后**（`WindowsManager__OpenWindowCO.c:50`，两支都过）⇒ 弹窗也写
        ///   ⇒ **`currentWindow` 就等于「最上面那扇」**（与 `TopWindow` 那段的旁证一致：
        ///   `HidePopUp` 第一件事就是比 `currentWindow == popUpWindow`）。
        ///   ⚠️ 这条修掉一个真缺陷：原来「全屏窗盖在弹窗上」时 `currentWindow` 仍是**被盖住的那扇**，
        ///   而 ESC / 键盘选中走的是 `TopWindow`（= 旧式 `popUpWindow ?? currentWindow`）⇒ **打错窗**。</item>
        /// </list>
        /// ⚠️ **一处如实标注**：弹窗支那个 `currentWindow != win` 条件**是我们加的**（原版没有）——
        ///   后果只是「同一扇弹窗再开一次时不会被自己 `ToBackground()` 一下」；因为我们 `TryOpen` 恒会
        ///   `Open()`（那条偏离另记），原版那一压随后也会被盖回 `Open` ⇒ **零可观测差异**
        ///   （本仓 `ToBackground` 没有覆写，见 `grep "override void ToBackground"` 零命中）。
        /// </para></summary>
        public void OpenWindow(GameWindow win, object data = null, bool closeAll = false)
        {
            if (win == null) { Debug.LogError("[Win] OpenWindow(null)"); return; }

            if (closeAll) CloseAllWindows();

            if (win.type == WindowType.Fullscreen)
            {
                // 原版：全屏窗开时**场上一扇都不留**（`if (openWindows.Count > 0) HideAllWindows()`）
                if (openWindows.Count > 0) HideAllWindows();
                currentWindow = win;
            }
            else
            {
                // 原版：弹窗开时把上一个压到背景
                if (currentWindow != null && currentWindow != win) currentWindow.ToBackground();
                popUpWindow = win;
                // = 原版那一句写在 if/else **之外**的 `set_CurrentWindow(win)`（见上面第 2 条）
                currentWindow = win;
            }

            win.Manager = this;
            if (!openWindows.Contains(win)) openWindows.Add(win);
            win.TryOpen(data);
            // 🆕 A49：新开的窗成了**最上面那一扇** ⇒ 把「选中」挪到它里面第一颗。
            // 🔴 **2026-10-07（A77㉑⑥）就地订正（铁律 5）**：这里原来写「照 `StandaloneInputModule.ActivateModule`」——
            //    **两句都不对**（判据 = 本机 UGUI 源码
            //    `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs`）：
            //    ① `ActivateModule`（`:269`）**只在输入模块被激活时跑一次**（由 `EventSystem.cs:542` 在
            //       `ShouldActivateModule()` 由假转真那一下调）—— **不是每次开窗**；
            //    ② 它取的是 `eventSystem.currentSelectedGameObject`（非空时）**否则** `firstSelectedGameObject`
            //       （`:280-288`）—— 两者**都不是**「窗里层级序第一颗按钮」。
            //    ⇒ 「开窗后默认选中谁」这件事，原版的**时刻与对象都与我们这一次调用不同**；本行是**我们挑的**
            //    （原版那台 `EventSystem` 的实例在 `assets_full` 里根本不存在 ⇒ `firstSelectedGameObject`
            //    取不到判据，见 `PointerLayer` 的「键盘导航」一节 ①）。⛔ **别把它写成「原版就是这么做的」。**
            PointerLayer.SelectFirstIn(win.gameObject);
        }

        /// <summary>🆕 A49：**最上面那一扇窗**。
        /// 🔴 原版 `WindowsManager.Update` 的 ESC 就是打给 `currentWindow`（字段 0x58，
        /// 实证 = `WindowsManager__get_CurrentWindow.c` 读的就是 0x58），而原版 `OpenWindowCO`
        /// **对弹窗也会 `set_CurrentWindow(win)`** —— 那一句写在 type 分支**之外**
        /// （`WindowsManager__OpenWindowCO.c:50`），旁证：`HidePopUp` 第一件事就是比
        /// `currentWindow == popUpWindow`（`WindowsManager__HidePopUp.c` 读 0x58 / 0x60）
        /// ⇒ **原版的 `currentWindow` 就等于「最上面那扇」**。
        /// <para>🔴 **2026-10-11（A217③）就地订正（铁律 5）**：本属性原来是
        /// `popUpWindow ?? currentWindow`（那段注释还写着「⛔ 不要为了这一条去改 `OpenWindow` 的语义」）——
        /// **A217③ 把 `OpenWindow` 按原版改了**（两支都写 `currentWindow`）⇒ 那个折中**不再需要**，
        /// 本属性直接返回 `currentWindow`（= 原版 `get_CurrentWindow` 的读法）。
        /// 🔴 **不改会怎样**：全屏窗开时 `HideAllWindows()` 会把弹窗藏起来，而 `popUpWindow` 字段
        /// **仍指着那扇藏起来的弹窗**（原版这一支也不清 0x60）⇒ 老写法会让「最上面那扇」**指向一扇藏起来的窗**
        /// ⇒ ESC 与键盘默认选中都会打到它身上（A217③ 修的就是这一处）。
        /// ⛔ 别再退回 `??` 那一版。</para>
        /// <para>`popUpWindow` 字段**留着**：它是 `ShowPreviousWindow` / `NotifyClosed` / `Dump()` 那几处的
        /// 记账（原版 0x60 也是这个角色），只是**不再**参与「谁是顶窗」的判定。
        /// 没有窗 ⇒ null（原版那一刻也是「什么都不做」）。</para></summary>
        public GameWindow TopWindow
        {
            get
            {
                return currentWindow;      // Unity 的假 null（已销毁）也会走这里 ⇒ 调用点照旧自己判
            }
        }

        /// <summary>🆕 **A217③**：原版 `WindowsManager.HideAllWindows()`
        /// （`WindowsManager__HideAllWindows.c` + predicate `WindowsManager.__c__.<HideAllWindows>b__49_0.c` 逐句）——
        /// 把 `openWindows` 里**活着且 `state != Closed`** 的每一扇都 `Hide()` 掉
        /// （= `CurrentState = Closed` + 物体 `SetActive(false)`，见 `GameWindow.Hide`）。
        /// <list type="bullet">
        /// <item>predicate 逐句 = `w != null（Unity 假 null 也算 null）` **且** `*(int*)(w + 0x68) != 0`
        ///   ⇒ **只藏「还开着」的**（已经 `Closed` 的不再动）。</item>
        /// <item>🔴 **不摘表、不动 `currentWindow` / `popUpWindow`**（原版就不动）⇒ 藏起来的窗**仍留在
        ///   `openWindows` 里**，等上面那扇关掉时由 `ShowPreviousWindow` 认**列表尾**把它们带回来
        ///   （`UnHide()` / `ReopenFromBackground()`）。</item>
        /// <item>谁调它：`OpenWindow` 的**全屏支**唯一一处（= 原版那一句）。</item>
        /// </list>
        /// ⚠️ 与原版的差别如实记：原版关窗会 `Destroy` + 释放 addressable，我们只 `SetActive(false)`（老账，A123 记过）。</summary>
        public void HideAllWindows()
        {
            for (int i = openWindows.Count - 1; i >= 0; i--)
            {
                var w = openWindows[i];
                if (w == null) continue;                               // = predicate 的 Unity 假 null 那一半
                if (w.CurrentState == WindowState.Closed) continue;     // = predicate 的 `*(int*)(w+0x68) != 0`
                w.Hide();
            }
        }

        /// <summary>把窗口挂到它自己的锚点上（建场景时调一次；窗口是自己建的，锚点由 `WindowHolder` 给）。
        /// <para>🆕 **A166**：`placement` 还是哨兵（= 这扇窗**忘了显式赋值**）时**出声**
        /// （红线：不许静默失败），并照**老默认值 `Popup`** 兜底建出来 —— 画面不变、但日志里明明白白。</para></summary>
        public static void AttachToAnchor(GameWindow win)
        {
            var p = win.placement;
            if (!win.HasPlacement)
            {
                Debug.LogError($"[Win] `{win.name}` 的 `placement` **没有显式赋值**（还是哨兵 {(int)GameWindow.UnsetPlacement}）—— " +
                               "原版这个字段是**必填**的（全库 141 个实例逐个有值），我们逐窗在各自的 `Create()` 里赋。" +
                               "⚠️ 现在照**旧默认值 `Popup`(15)** 兜底建出来（画面不变），但请去那扇窗的 `Create()` 里" +
                               "照原版那颗 MB 补上（`5=Canvas` / `10=World` / `15=Popup`）。见 A166 / `GameWindow.UnsetPlacement`。");
                p = WindowsPlacement.Popup;
            }
            var anchor = GetWindowAnchor(p);
            if (anchor == null) return;
            win.transform.SetParent(anchor, false);
            win.transform.localPosition = Vector3.zero;
            win.transform.localRotation = Quaternion.identity;
            win.transform.localScale = Vector3.one;
        }

        internal void NotifyClosed(GameWindow win)
        {
            // 🆕 A77㉑①：原版 `CloseWindowCO` 里那一跳的判据是「关掉的**是不是 `CurrentWindow`**」
            //   —— 而原版 `CurrentWindow` 就等于「最上面那扇」（弹窗也写它，见本类 `TopWindow` 那段的旁证）
            //   ⇒ 我们这一侧取 `TopWindow` **必须**在摘除**之前**算（摘完 `currentWindow`/`popUpWindow`
            //   都已经清空，那一刻恒为 null）。
            bool wasTop = win == TopWindow;
            openWindows.Remove(win);
            if (currentWindow == win) currentWindow = null;
            if (popUpWindow == win) popUpWindow = null;
            if (wasTop) ShowPreviousWindow();      // = 原版 `WindowsManager__CloseWindowCO.c` 的 `ShowPreviousWindow()`
        }

        /// <summary>🆕 A77㉑①：**把「上一扇」带回来** —— 逐句对位原版
        /// `WindowsManager.ShowPreviousWindow`（`d:/2/tools/decomp_full/WindowsManager__ShowPreviousWindow.c`）。
        /// 原版那一串（每一步都标出我们的对应物，**不同的地方单独说**）：
        /// <list type="number">
        /// <item>`openWindows.RemoveAll(w =&gt; w == null)` —— predicate `b__51_0` 就是 Unity 的假 null 判据
        ///   （`WindowsManager.__c___ShowPreviousWindow_b__51_0.c` 体里只有一句 `Object.op_Equality`）。</item>
        /// <item>列表**空了** ⇒ `CurrentWindow = baseMenu`（字段 0x48）+ `if (baseMenu != null) OpenWindow(baseMenu)`。
        ///   🔴 **我们这一支什么都不做** —— 详见下面那段「我们没有 baseMenu」。</item>
        /// <item>取**列表尾**那一扇（原版 `List.get_Item(Count - 1)`）。</item>
        /// <item>它是弹窗（`type == Popup`，字段 **0x20**）⇒ 在列表里 `LastOrDefault(type != Popup)`
        ///   （predicate `b__51_1` = `*(int*)(w + 0x20) != 1`）并 `UnHide()` 它 —— 把它**重新显示出来、置于背景**。</item>
        /// <item>`CurrentWindow = 列表尾那一扇`；`列表尾.TryOpen()`（非 `Closed` 支 ⇒ 只回 `Open` + `ToFocus`）。</item>
        /// </list>
        /// <para>🔴 **我们没有常驻 `baseMenu`（第 2 步）**：本仓的主菜单**不是** `WindowsManager` 的窗
        /// （`Shell/MainMenuRuntime.cs` 自己那棵树、不经过窗口表）⇒ 没有可以「开回来」的对象，
        /// 这一步**什么都不做**（主菜单本来就一直在），只把两个字段清干净。**已记账**（如实标注，铁律 3）。</para>
        /// <para>🔴 **为什么要它**（㉑ 的裁定：三件必须一起做）：原版 `CloseWindowCO` 关掉**最上面那扇**时会调本方法
        /// ⇒ 关掉弹窗之后，底下那扇从 `Background` **回到 `Open`**。我们原来没有这一跳 ⇒ 底窗**停在 `Background`**；
        /// 又因为 `KeyCancel` 那时也没问 `IsOpen()`，ESC **照样能关它** —— 两处偏离**凑巧抵消**、看着像对的
        /// （这正是那条旧断言 `…它被压到背景态` 在替我们背书的由来）。
        /// ⇒ 本方法 **与 `PointerLayer.KeyCancel` 里那道 `IsOpen()` 门槛必须成对**，⛔ 不许只留一件。</para>
        /// <para>⚠️ **可见性照原版 = `private`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:305`
        /// 那一行就是 `private void ShowPreviousWindow()`）—— 自检**不直调它**、走生产那条路
        /// （`KeyCancel` → `Close` → `NotifyClosed` → 这里），比直调更有鉴别力。</para></summary>
        void ShowPreviousWindow()
        {
            // ① 死条目（原版 predicate = Unity 假 null；`openWindows` 里理论上不留死引用，但照原版清一遍不花钱）
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] == null) openWindows.RemoveAt(i);

            if (openWindows.Count == 0)
            {
                // ② 原版：`CurrentWindow = baseMenu` + `OpenWindow(baseMenu)`。我们没有 baseMenu（见上面那段）
                //    ⇒ 无可开；只把记账清干净（`NotifyClosed` 通常已经清过，这里兜一次）。
                currentWindow = null;
                popUpWindow = null;
                return;
            }

            var prev = openWindows[openWindows.Count - 1];                   // ③ 列表尾

            GameWindow lastMain = null;                                      // ④ 最后一个「非弹窗」
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] != null && openWindows[i].type != WindowType.Popup) { lastMain = openWindows[i]; break; }

            if (prev.type == WindowType.Popup && lastMain != null) lastMain.UnHide();

            // ⑤ 顶窗记账：**照原版** `CurrentWindow = 列表尾那一扇`（原版第 4 步 —— 不论它是不是弹窗）。
            //    🔴 **2026-10-11（A217③）就地订正（铁律 5）**：这里原来写
            //      `if (prev.type == Popup) { popUpWindow = prev; currentWindow = lastMain; }`
            //      —— 那个「列表尾是弹窗 ⇒ `currentWindow` 让给主窗」的分格，**只在旧模型下自洽**
            //      （旧模型里 `currentWindow` = 主窗、`TopWindow = popUpWindow ?? currentWindow`）。
            //      A217③ 之后 `currentWindow` 的语义 = **最上面那扇**（`OpenWindow` 两支都写它）
            //      ⇒ 再把 `currentWindow` 让给 `lastMain`，`TopWindow` 就会指向**被压在下面那扇**
            //      （ESC / 键盘默认选中直接打错窗 —— 与本条要修的是同一个缺陷，只是换了个入口）。
            //    `popUpWindow` 仍按我们这一份记账填（原版只有 `currentWindow` 一个字段）。
            currentWindow = prev;
            popUpWindow = prev.type == WindowType.Popup ? prev : null;

            prev.ReopenFromBackground();                                     // ⑥ = 原版 `TryOpen()` 的非 `Closed` 支
            // ⚠️ **如实标注一处未做的偏离**（不归本件）：原版这里调的是**完整** `TryOpen()` ——
            //    若列表尾此刻是 `Closed`（A217③ 之后这**很常见**：`HideAllWindows` 藏起来的窗就是 Closed），
            //    原版会走「`SetActive(true)` + `CurrentState = Open` + **`Open()`（重建内容）**」那一支；
            //    我们只做前两步（`ReopenFromBackground` = 非 Closed 支）。内容**不会重建**（我们的 `Hide()`
            //    也不销毁任何东西 ⇒ 内容还在），但与原版「重走一遍 `Open()`」仍有差 —— 它与 A217② 那个
            //    「同窗再开要不要重建」是**同一类**问题（原版不重建时那些入口靠什么刷新内容，**没人查过**）
            //    ⇒ 归到 A217② 那条待查证里，⛔ 别在这里顺手改。
        }

        public void CloseAllWindows()
        {
            for (int i = openWindows.Count - 1; i >= 0; i--)
                if (openWindows[i] != null) openWindows[i].Close();
            openWindows.Clear();
            currentWindow = null;
            popUpWindow = null;
        }

        // ---------------------------------------------------------- 开窗入口：**按引用复用**（A123 落地 · A177 收编）

        // 🔴 **原版判据（三层，全部本地可复现）** → `资料/普查产出_1007/波6_A123_入口复用.md` ·
        //   `资料/普查产出_1007/波6判据核查.md` §2 · `资料/普查产出_1006/甲5_A97_A99_A104_A105.md` §二 A104：
        //   ① 缓存字段 = **`WindowsManager.automaticallyLoadedWindows`**
        //      （`dump.cs`：`BiDirectionalDictionary<ComponentReference<GameWindow>, GameWindow>` @ **0x68**，
        //      **键 = prefab 引用**）；② `WindowsManager.OpenWindow` **第一件事就是查它**（`TryGetValue`
        //      在 `Instantiate` **之前**，命中 `jne` 直接跳复用块，实测 VA `0x180875990`）
        //      ⇒ **同一扇窗点两次只有一个实例**；③ 🔴 **命中只在「窗还开着」时发生**：
        //      `WindowsManager__CloseWindowCO.c` 关窗时会 `Object.Destroy(gameObject)` + `ComponentReference.Release`
        //      + **把缓存条目删掉** ⇒ 关过之后再点，**原版走的是新建**。
        //
        // 🔴 **2026-10-11（A177）**：这一份**从 `Shell/MainMenuRuntime.cs` 搬到了这里** —— 那个缓存字段
        //   **本来就是 `WindowsManager` 的**（证据见上面 ①），A123 当年因为「`WindowsManager` 是共用件、
        //   本批不动」把它留在了入口那一侧；A177 要收编的正是「**没走这条机制**的那几条入口」。
        //   ⚠️ **今天仓里仍是两份**（`MainMenuRuntime._openByRef` 那 8 条主菜单入口还在用它自己那一份）——
        //   「一个文件同一时刻只有一个写手」那条红线不许本件动那个文件 ⇒ 合并成**一份**要等调度台把那边
        //   8 条改成转调这里（改法：`MainMenuRuntime.OpenByRef(...)` 的整个方法体换成一行
        //   `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);` 并删掉它自己那份 `_openByRef` /
        //   `StillOpen`；**已写进报告**）。⛔ 别在任何入口那一侧再抄第三份。
        //
        // **我们的等价物**（与原版逐条对位）：
        //   · 缓存 = `_openByRef`，键 = **prefab 根名**（与各自 `Create()` 里 `new GameObject(...)` 用的那个名字
        //     同源 ⇒ 真·「按引用」；⚠️ 改名要两处一起改，几条共用名收在下面那几个 `PrefabRef*` 常量里）；
        //   · 「还开着吗」= `WindowsManager.openWindows` 里还有没有它 —— `GameWindow.Close()` →
        //     `NotifyClosed` 会把它摘掉 ⇒ **与判据③同义**；
        //   · 关窗那一刻我们**拿不到回调**（`GameWindow.Close()` 里没有事件）⇒ 条目**惰性**删（下一次开窗时）。
        //     对外可观测的行为与判据③一致：**关过之后这条引用永不复用**。
        //   · **`static`**：原版那份缓存在 **manager** 上（不随主菜单那一层重建而丢），而窗是挂在锚点上的、
        //     主菜单重建也不消失 ⇒ 放成实例字段会在「主菜单重来一次、旧窗还开着」时**又建一扇**（正是要修的）。
        //   ⛔ **别用「实例还在就复用」**（`XxxWindow.Instance == null` 那种写法）—— 我们的 `Close()` 只
        //     `SetActive(false)`、**不销毁**（原版是 `Object.Destroy`）⇒ 「实例还在」在关窗之后**恒为真**、
        //     等于永不新建。断言 → `Editor/MainMenuScene.cs` 的 A123 那一节（最锋利的那条就盯它）。
        static readonly Dictionary<string, GameWindow> _openByRef = new Dictionary<string, GameWindow>();

        /// <summary>几条「prefab 根名」的**唯一出处**（= 各自 `Create()` 里 `new GameObject(...)` 用的那个名字）。
        /// ⚠️ `MainMenuRuntime` 那边另有一份同名的 `Ref*` 常量（**合并时以这里为准**）。
        /// ⚠️ 排行榜那一条对**两棵** prefab（遭遇榜 / 经典榜）⇒ 键由 `LeaderboardWindow.NameOf(kind)` 给，
        /// 那是「kind → prefab 根名」的唯一来源（`Create` 建 GO 用的也是它），别在这再抄一份。</summary>
        public const string PrefabRefChat = "ChatPanel";
        public const string PrefabRefProfile = "Player Profile Window";
        public const string PrefabRefBattleLog = "Battle Log Popup";

        /// <summary>🆕 **A177**：按 **prefab 引用**开窗 —— **原版 `automaticallyLoadedWindows` 命中就复用**的等价物。
        /// 还开着 ⇒ 复用同一扇（同一个实例再走一遍开窗流程，不新建）；关过 / 没建过 ⇒ 新建。两条路都出声（红线）。</summary>
        /// <param name="prefabRef">prefab 根名（见上面那几个 `PrefabRef*`）。看下面「键为什么可能带后缀」那条。</param>
        /// <param name="closeAll">照该入口原版 `OpenWindowButton.closeOtherMenus` 传
        /// （`OpenSocial` / `OpenRewards` / `OpenCollection` / `OpenShop` 四条是 1 ⇒ `true`）。
        /// ⚠️ 这一支会先 `CloseAllWindows()`：若复用的那扇自己也在开着，它会先被关掉再立刻重开
        /// （**同一个实例**、`Open()` 重建内容）⇒ 结束时仍只有一扇，与原版「复用后照样重走一遍开窗流程」一致。</param>
        public static T OpenByRef<T>(string prefabRef, System.Func<WindowsManager, T> create, bool closeAll = false)
            where T : GameWindow
        {
            var wm = EnsureHost();
            T win = null;
            GameWindow cached;
            if (_openByRef.TryGetValue(prefabRef, out cached))
            {
                if (StillOpen(wm, cached))
                {
                    // = 原版 `TryGetValue` 命中那一跳（A104 §二 第 2 跳）
                    win = cached as T;
                    Debug.Log($"[Win] `{prefabRef}` 已经开着了 ⇒ **复用同一扇**（照原版 `automaticallyLoadedWindows` 命中复用），不新建");
                }
                else
                {
                    // = 原版 `CloseWindowCO` 把缓存条目删掉那一跳（我们惰性做，见上面那段）
                    _openByRef.Remove(prefabRef);
                }
            }
            if (win == null)
            {
                win = create(wm);
                _openByRef[prefabRef] = win;
                Debug.Log($"[Win] `{prefabRef}` 不在（没建过 / 已经关掉）⇒ **新建一扇**（照原版 `Instantiate` 那一支）");
            }
            wm.OpenWindow(win, null, closeAll);   // 复用那一支照原版**照样再走一遍**开窗流程（A104 §二 第 5 跳）
            return win;
        }

        /// <summary>「这扇窗还开着吗」—— 判据只有 `WindowsManager.openWindows` 一处
        /// （`GameWindow.Close()` → `NotifyClosed` 会把它从那里摘掉）。
        /// ⚠️ 先过一遍 Unity 的 `== null`：场景卸载 / 对象被销毁时那是**假 null**（那种也要从缓存里剔掉）。</summary>
        public static bool StillOpen(WindowsManager wm, GameWindow win)
        {
            if (win == null) return false;
            if (wm == null) return false;
            return wm.openWindows.Contains(win);
        }

        /// <summary>自检用：清空那一份「按引用复用」的缓存（别让它跨段影响后面的断言）。</summary>
        public static void ClearReuseCacheForTest() { _openByRef.Clear(); }

        // ---------------------------------------------------------- 弹窗

        /// <summary>
        /// 通用弹窗（原版 `ShowPopUp(text, localizeTexts, closeOnEsc, …)` 三个重载）。
        /// 界面 = **`PromptPopup`**（照原版 `GenericPromptWindow` prefab，出处 `资料/日常_原版规格.md` §七）。
        /// ⚠️ 原版那两个 `popupWindowOneButton/TwoButtons` 仍然本地没有（正本 §五 第 2 条），
        /// 但**同族的 `GenericPromptWindow` 有** ⇒ 用它当宿主；「只有 Ok」那一档是我们挑的（见 `PromptPopup` 文件头）。
        /// </summary>
        public void ShowPopUp(string text, string okText = null, System.Action onOk = null,
                              string cancelText = null, System.Action onCancel = null)
        {
            var win = PromptPopup.Create(this, text, okText, onOk, cancelText, onCancel);
            OpenWindow(win);
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"锚点 {_anchors.Count} 个：");
            foreach (var kv in _anchors) sb.Append($" {kv.Key}→{kv.Value.name}");
            sb.Append($" · 开着的窗 {openWindows.Count} 个");
            if (currentWindow != null) sb.Append($" · 当前主窗 {currentWindow.name}");
            if (popUpWindow != null) sb.Append($" · 弹窗 {popUpWindow.name}");
            return sb.ToString();
        }
    }
}
