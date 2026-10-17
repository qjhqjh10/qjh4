// GameWindow.cs —— 从 `Shell/WindowsManager.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
// 🔴 本类是**所有窗口的基类**（全仓 39 处 `AddComponent`）—— 命名空间顶层、全名不变 ⇒ 引用处**一行都不用改**。
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;      // `WFSoundPlayer`（特效/音效那条路唯一的播放器，`Assets/WarpforgeVFX/Runtime/`）

namespace CardPresentation
{
    // 🔴 **2026-10-11 就地订正（`A1551` · 铁律 5）：这里原来挂着一句 `[ExecuteAlways]` 及它的注释 ——
    //   那个属性是【锚点类 `WindowHolder`】的，不是 `GameWindow` 的。** 2026-10-11 把 `WindowHolder`
    //   拆去独立文件时**【类搬走了、类前面那个属性没搬】**（`git diff a04b76b HEAD -- WindowsManager.cs`
    //   = 0 增 / 13 删）⇒ 属性落到 `GameWindow` 上，而 **`SearchingOpponentWindow : GameWindow` 继承了它**
    //   ⇒ **编辑模式也派 `OnEnable`** ⇒ `NetSelfTest` 的 `A932⑥` 翻红（`OnHint` 订阅者 0→1）。
    //   🔑 **判别式**：形状完全相同的**孪生断言 `N⑤` 仍然绿** —— 因为 `SearchingMatchPopup : MonoBehaviour`
    //   （**不是**本类子类）⇒ 一对孪生只翻一条 =「属性只落在 `GameWindow` 上」的签名。
    //   ✅ **属性已搬回 `Shell/WindowHolder.cs`**（它才是"挂在锚点节点上、`OnEnable` 自注册"的那个类）。
    //   ⛔ **别再加回本类** —— 加了等于让**整族窗口在编辑模式**跑 `Awake`/`OnEnable`/`OnDisable`/`Update`。
    //   ⚠️ **教训**：拆类的脚本**必须把「类前面挂着的属性」一起搬**（那批拆的 8 个类里只有它带属性）。

    /// <summary>所有菜单窗口的基类。子类重写 <see cref="Open"/> 做自己的铺数据/播动画。</summary>
    public class GameWindow : MonoBehaviour
    {
        /// <summary>🔴 **A672 哨兵 —— 这不是原版的枚举值，是「还没赋过值」的标记**（与下面 `UnsetPlacement` 同形）。
        /// <para>原版 `type`（字段 **0x20**）在 prefab 里是**必填**的 —— `WindowsManager__OpenWindowCO.c`
        /// 按它决定「把场上其余**全部**藏起来」（`==0`）还是「把上一个 `ToBackground()`」（`==1`）。
        /// 我们逐窗在各自的 `Create()` 里赋（现读：生产建窗点 **35** 个全赋了 · 夹具 `Editor/ShellScene.cs`
        /// 另 **17** 处也全赋了 —— 合计 **52** 处，一处不漏）。**默认值原来是 `Fullscreen`(0)**
        /// ⇒ 将来哪扇新窗忘了赋，会**静默**走全屏支（开它就把别的窗全藏起来），而且**没有任何断言会红** ——
        /// 正是本工程红线「不许静默失败」要挡的那种。</para>
        /// <para>⇒ 默认值改成这个**不可能被误当成合法档位**的哨兵，**两层出声**（照 A166 那两层）：
        /// ① `AttachToAnchor`（建窗那一刻、带上窗口名）；② `OpenWindow`（有人绕过 `AttachToAnchor` 时 ——
        /// 与 `GetWindowAnchor` 那条「走到这里 = 有人绕过了它」同形）。**两层都照旧默认值 `Fullscreen` 兜底**
        /// （画面不变），⛔ 不是照「不等于 `Fullscreen` ⇒ 当弹窗」那条**隐含判据**走（那会把「忘赋」变成另一种
        /// 静默坏法）。</para>
        /// ⛔ **别把它加进 `WindowType` 枚举**（枚举值照原版：`Fullscreen=0 / Popup=1`）。</summary>
        public static readonly WindowType UnsetType = (WindowType)(-1);

        public WindowType type = UnsetType;

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

        /// <summary>🔴 **原版 `GameWindow.updateNavPanel`（字段偏移 `0x3A`，`d:/2/tools/il2cpp_out/dump.cs:108812`）**。
        /// <para>**语义（唯一读点 = `GameWindow__ESCPressed.c` 的第三道门槛，逐条读出来的）**：
        /// 按 ESC 关掉这一扇窗时，把**左竖导航回点到 `Main`（第 0 颗）** ——
        /// 原版那一句是 `NavigationPanelController.SetMainToggle(this)`（`__ESCPressed.c` 末段）。
        /// 观感 = 「关掉子界面、回到主菜单 ⇒ 左栏的灯回到最上面那颗」。</para>
        /// <para>⚠️ **全库只有两个读点、都在 ESC 那一支**（`GameWindow__ESCPressed` /
        /// `SearchingMatchWindowDemo__ESCPressed`）—— **没有任何「开窗时读它」的地方**。
        /// ⇒ 铁律 5 就地订正：`Shell/SocialWindow.cs` 里那句「原版 `updateNavPanel = 1` ⇒ **开窗时**
        /// 把左竖导航的 SOCIAL 键点亮」是**错口径** —— `SOCIAL` 那颗亮起来来自**导航钮自己的 `ToggleGroup`**
        /// （点它那一下，`m_AllowSwitchOff = 0` 单选），与原版 `SetToggle` 无关、更与 `updateNavPanel` 无关
        /// （全库**没有** `SetToggle(nav, 4)` 这个调用点）。</para>
        /// <para>**逐窗值**（照各自 prefab 实读；本仓把这一格**赋在开窗入口**，理由见
        /// `MainMenuRuntime.MarkUpdatesNavPanel`）：`1` = **Collection / Inbox / Shop / Social**，其余 `0`。
        /// 缺省 `false` 与「其余全是 0」一致。</para></summary>
        public bool updateNavPanel;
        /// <summary>小屏 UI 下的额外放大倍数（原版 `extraScaleSmallScreen`）。**逐窗实测、照各自的值填**
        /// （1.0 有 93 个；1.07 练习窗 · 1.075 玩家档案 · 1.15 决斗 · 1.2 卡包信息 …）。
        /// 🔴 **`1.0` 的含义是「不覆盖」**：此时 prefab 里烤着的 `menuScale` 原样生效 ——
        /// 窗口根上带成品的 3 扇（`TrophyInfoPopup` / `AllianceMemberOptionsPopup` / `GenericOptionsPanel`）
        /// 就是 `extra = 1.0` 而烤的是 **1.35**。判据 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头。</summary>
        public float extraScaleSmallScreen = 1f;
        /// <summary>开窗音效（原版 `TryOpen` 里播，走 FX 组）。没有就不播。</summary>
        public AudioClip openSound;

        /// <summary>关窗音效（原版 `GameWindow.closeSound`，字段偏移 **0x30**）。没有就按下一格兜底。</summary>
        public AudioClip closeSound;

        /// <summary>🆕 **2026-10-17（音效线 `W_G`）**：原版 `GameWindow.useDefaultCloseSoundIfNull`
        /// —— 字段偏移 **0x38**，**ctor 里就置 1**（`GameWindow__.ctor.c`：`*(param_1 + 0x38) = 1`
        /// 紧挨着 `extraScaleSmallScreen = 1.0f` 那一句；字段序旁证 → 本文件 `ESCPressed` 的注释）。
        /// <para>语义（逐句读 `GameWindow__Close.c`）：**`closeSound == null` 且本开关为真** ⇒
        /// 播 `SoundManager.defaultCloseSound`；两者都不成立时**什么都不播**（原版那一刻
        /// `SoundManager.Play2D(null, …)` 只在 `showNullAudioPlaysLog` 打开时才打日志，而那一位实测是 **0**）。
        /// ⇒ 默认值必须是 **true**（不是 C# 的 `false` 默认）—— 否则**全库 141 扇窗一起静音**。</para>
        /// <para>默认 cue 的判据链（**2026-10-17 解开**，此前 `资料/普查产出_1017/UI/U1` §五·5 记的是「没解出」）：
        /// `SoundManager` MB（`bundle_staticgeneralassets_assets_all` pid `-5308416599568047990`）
        /// 的 `defaultCloseSound` = PPtr `m_FileID 5 / m_PathID 8084683923307806969`
        /// （`m_FileID` 是 **1-based externals** ⇒ externals[4] = `CAB-62d1945b…` = `soundcollection`）
        /// → cue **`ButtonClick Back`** → `clipList[0]` = pid `-3585420588411809655` → AudioClip
        /// **`UIButton Back`**。cue 参数（`min/maxPitch = 1` · `min/maxVolume = 1` · `timeToPlayAgain = 0.1`）
        /// 已进 `Resources/animfx_sounds.json`（`工具/import_original_sfx.py` 的 `EXTRA_CUES`）。</para>
        public bool useDefaultCloseSoundIfNull = true;

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
        /// （`Shell/ForgeTab.cs` 的 `AddHit` 转调那一处 · `Shell/CampaignTab.cs` 的 `AddHit` 转调那一处）⇒ 那两页现在也吃 `Clip` 与 `ClipPad`；
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
        /// + `align`（**0 = 居中（`Label` 默认）· 1 = 左 · 2 = 右**）。
        /// <para>🔴 **2026-10-13（A435 阶段 2 · 丁 / B1）**：裁切状态**不再只吃本窗字段** ——
        /// 两处守卫改成走 `ViewportClip.Resolve`（本窗那两份只是**形参**：**非空 = 旧路赢、逐位不变**；
        /// 本窗没设 `Clip` 时才由**父链上最近的 `ViewportClip` 节点**接管）。
        /// 形状/判据逐条同 `MenuWindowBase.Text`（A435① 的成品，两处逐字同形）。
        /// 🔴 **这一处不接的后果 = 全壳所有窗的文字都吃不到节点态** —— `GameWindow` 是共同基类
        /// （`MenuWindowBase` 族与 `LiveOpsEventWindow` 族全部从它继承）⇒ 这是「A435 阶段 2 算不算做完」的一环。</para>
        /// <para>🔴 **2026-10-13 订正（铁律 5）**：本行原来还缀着「**；原版 TMP 的 `m_HorizontalAlignment`**」——
        /// **那句是错的、而且就是 A635 那个缺陷的源头**：**两套枚举不是同一套**，只有 `1` 同义 ——
        /// **本仓** `0 = 居中 / 1 = 左 / 2 = 右`，**原版 TMP** `Left = 1 / Center = 2 / Right = 4 / Flush = 0x10(16)`
        /// （`TMPro_TextAlignmentOptions`）。⇒ **⛔ 别照原版字段值往这里填**（照填会把 `Center` 写成右对齐）。
        /// 原文已删；要查原版对齐，去 prefab 读 **TMP 组件自己的 `m_HorizontalAlignment`**。</para>
        /// <para>🔴 **对齐必须在 `ClipText` 之前** —— `ClipText` 夹的是**世界坐标**的顶点，先裁再挪会把裁好的块
        /// 挪出框（**2026-10-13 订正（铁律 5）**：这里原来举的例子写「`CampaignRewardWindow` 那颗 `Warning`
        /// **原版就是 `Right`**」—— **原版实读是 `Center/Middle`**，A635 已把实现改成 `Center`
        /// （删掉那个 `2`）并把 `Point Count` 补成 `Left`；⚠️ **「对齐必须在 `ClipText` 之前」这条结论不变**，
        /// 变的只是那个例子里的对齐值）。</para>
        /// <para>🆕 **2026-10-11（A241）**：本方法原来在 <b>三</b> 个文件里各有一份**逐字相同**的副本
        /// （`Shell/DailyStreakPopup.cs` · `Shell/CampaignRewardWindow.cs` · `Shell/InboxWindow.cs`）——
        /// 三扇都是 `GameWindow` 直系、**够不到** `MenuWindowBase` 家族的 `MainMenuSubmenuWindow.Text`
        /// （那一份签名/语义**不同** —— 走 `TextBox` + `wrap`/`alignLeft` 两个 bool、且求交走 `ClipRect`
        /// —— 是**有意留在那一支**的，⛔ 别顺手并进来），而共同基类 `GameWindow` 当年不在那几件的白名单里
        /// ⇒ 各自就地抄了一份。现在**只留这一份**
        /// （判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §六·6 · `资料/待办判据_1008.md` §A241）。</para>
        /// <para>🔴 **2026-10-16 就地订正（铁律 5 · A837）**：原文写「`DailyStreakPopup` 那一份**没有**它（它一个调用点都不传）」——
        /// **已过期**：当天 W13（A679）给它的奖格 `Collect Text` 补了 **命名实参 `align: 1`**（原版 `m_HorizontalAlignment = 1 = Left`，
        /// 出处 `资料/普查产出_1016/A679_align全表.md` §三·1）⇒ 它**现在有一个调用点传 align**。
        /// 其余结论不变（`align` 仍是那三份的**超集**；缺省 0 = 那一档什么都不做）。
        /// ⛔ 以下为原文（留痕）：⚠️ **两处如实标注**：① `align` 是那三份的**超集** —— `DailyStreakPopup` 那一份**没有**它
        /// （它一个调用点都不传）⇒ 上移后**行为逐字等价**（`align` 缺省 0 = 那一档什么都不做）；
        /// ② 可见性从三份的 `private` 变成 `protected`（三份的调用点全在各自类里 ⇒ 调用点一个没改）。
        /// ⚠️ 祖先那一层还有别的 `Text` 重载（`MenuWindowBase.Text` / `SocialPage.Text` / `PlayerProfileWindow.Text`…）
        /// —— 参数表各不相同 ⇒ 是**重载**不是覆盖，各自照旧解析到自己那一份。</para>
        /// <para>🆕 **2026-10-12（A446）补 `autoMaxPx` / `autoBasePx` 两个形参并透传**：底下的
        /// <see cref="MenuDraw.Text"/> **早就有了这两个口**（A333 / A336② 开的），本方法却一直没转发
        /// ⇒ 走本方法建的 `GameWindow` 直系窗（`CampaignRewardWindow` / `DailyStreakPopup` / `InboxWindow`…）
        /// **拿不到「原版 `m_fontSizeMax` / `m_fontSizeBase`」那两档**（自适应上限恒 = `fontPx`、base 恒 = 调用方那档）。
        /// 语义与缺省值**逐字同 <see cref="MenuDraw.Text"/>**：**都 `&lt;= 0` ⇒ 旧行为**（上限 = `fontPx`、
        /// base = 调用方那一档）⇒ 既有调用点**一个都不用改**。
        /// 用法（判据 = 原版 prefab 逐颗实读，别按窗统一）→ `Shell/CampaignRewardWindow.cs` 的
        /// `WarnAutoMax/WarnAutoBase` 那几条常量。</para>
        /// <para>🔴 **为什么这两个形参挂在【最后】**（⛔ 别顺手挪到 `autoMinPx` 旁边去对齐 `MenuDraw.Text` 的次序）：
        /// `align` 是位置实参（`CampaignRewardWindow` 那颗 `Warning` 就是 `…, WarnAutoMin, 2)`），
        /// 而 `autoMaxPx` / `autoBasePx` 是 **float** —— `int` 字面量 `2` **能隐式转 float**，
        /// 一旦把新形参插在 `align` 前面，`…, 18f, 2)` 会**静默绑到 `autoMaxPx = 2f`、`align` 退回 0**
        /// （不报错、只是对齐与上限同时错）。挂末尾 ⇒ 老调用点**逐位不变**；
        /// 新调用点用**命名实参**（`autoMaxPx: …`）最稳。</para></summary>
        protected Label Text(Transform parent, PxRect r, string s, Color color, string name, float fontPx, int q,
                             float wrapPx = 0f, float autoMinPx = 0f, int align = 0,
                             float autoMaxPx = 0f, float autoBasePx = 0f)
        {
            // 🔴 **2026-10-13（A435 阶段 2 · 丁 / B1）：取状态走【一处】共用解析**（`ViewportClip.Resolve`）——
            //    本方法原来两处都直接读 `RenderClip` / `ClipSoftness` 两个**本窗字段**，于是本窗没设
            //    `Clip` 时：① 下面那句 `Visible` 拿到 `null` ⇒ 一律判「可见」；
            //    ② 末句 `if (RenderClip.HasValue)` ⇒ **根本不调 `ClipText`**。
            //    ⇒ **父链上有没有 `ViewportClip` 节点，对这段文字完全不起作用**（正是 H10 §五·1 记的那个缺口）。
            //    现在两处都吃**解析后**的那一份（`_st`）：形参非空 = 旧路赢、**逐位等于原来的 `RenderClip` /
            //    `ClipSoftness`**（`PaddedClip(·, Vector4.zero)` 首句早退）⇒ 迁移前（无节点 + 本窗设了 `Clip`）
            //    **行为逐位不变**；本窗没设 `Clip` 而父链上有节点时，才由**节点**接管。
            //    ⚠️ 解析起点 = `parent`（本方法建的标签/图都挂在它下面）—— 与 `MenuDraw.Text` / `ClipText`
            //    内部那一份取法同一口径（节点在父链上同样命中）。
            // 🔴 **动过这段就别忘下面那句 `ClipText`**：两处必须用**同一份** `_st` —— 否则会出现
            //    「按节点判了可见、却按本窗字段（= 不裁）建出来」这种半拉子状态（静默、且只在挂节点时现形）。
            var _st = ViewportClip.Resolve(parent, RenderClip, ClipSoftness, default(Vector4));
            if (!MenuDraw.Visible(r, _st.RenderClip)) return null;
            // A446：后两个口**透传**（`<= 0` ⇒ `MenuDraw.Text` 内部退回旧行为，见那边的注释）
            var lb = MenuDraw.Text(parent, r, s, color, name, fontPx, q, wrapPx, autoMinPx, autoMaxPx, autoBasePx);
            if (lb == null) return null;
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
            // 🔴 A435 阶段 2 · 丁（B1）：形参 = **调用方原样那一份**（`RenderClip` / `ClipSoftness`），**不是**
            //    `_st` 里那两份 —— 形参非空时两条路逐位相同，而形参为 `null` 时它让 `ClipText` / `ClippedTextGuard`
            //    **跟着父链重解析**（守卫要在节点挪动之后重裁，快照会拿旧框裁，见 `ClippedTextGuard` 的类注释）。
            if (_st.RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
            return lb;
        }

        public WindowState CurrentState { get; private set; }
        public object Data { get; private set; }
        public WindowsManager Manager { get; internal set; }

        /// <summary>有没有**显式赋过** `placement`（默认值是哨兵 `UnsetPlacement`，见那边的注释）。</summary>
        public bool HasPlacement { get { return (int)placement >= 0; } }

        /// <summary>🆕 **A672**：有没有**显式赋过** `type`（默认值是哨兵 `UnsetType`，见那边的注释）。
        /// 形状与 <see cref="HasPlacement"/> **逐字同构** —— 两个字段都是「哨兵 = 还没赋过」（⛔ 别发明第三种）。
        /// ⚠️ **它只判「赋没赋过」，不判「值合不合法」**（正数里塞个野值照样算赋过）——
        /// 与 `HasPlacement` 的边界一致（那边把 `None`(0) 也算「赋过」、由 `GetWindowAnchor` 兜底报错）。</summary>
        public bool HasType { get { return (int)type >= 0; } }

        /// <summary>原版 `GameWindow.TryOpen(object data, GameWindowOptions options)` —— **生产路径就是这一条**
        /// （`WindowsManager.OpenWindow` → 它；原版另一条**无参**重载 = <see cref="TryOpen()"/>）。逐句对位见下面每一支的注释。
        ///
        /// <para>🔴 **2026-10-12（A217②）就地订正（铁律 5）：本方法原来【恒】`Open()`** ——
        /// 那是「同窗再开一次会**重建内容**」；**原版不这样**。判据（第一权威 = 反编译，两个重载本地都没有 `.c`，
        /// 用 VA 反汇编读出）：
        /// <list type="bullet">
        /// <item>`GameWindow.TryOpen(data, options)` = **VA `0x180836120`**（`dump.cs` RVA `0x836120`；
        ///   `file off = RVA − 0x1600`，公式 → `资料/普查产出_1012/V8_判据补查.md` §顺手发现 2）逐句 =
        ///   **① 先 `SetupData`（虚表 `0x188`）→ ② 读 `CurrentState`（`+0x68`）→ ③ 按值分三支**。</item>
        /// <item>**`Closed`(0) ⇒ 才** 播 `openSound`(`+0x28`) → `SetActive(true)` → `state=1` →
        ///   `call [rax+0x178]`（= 虚方法 `Open()`）→ 触发 `OnOpen`(`+0x58`) 委托。</item>
        /// <item>**`Background`(2) ⇒ 只** 把 `state` 写回 `Open` + `call [rax+0x1a8]`（提回前台）。
        ///   （`state == 1` 那一档 `cmovne` 直接把标志压掉 ⇒ 落进下面 `Open` 支。）</item>
        /// <item>**`Open`(1) ⇒ 一个字段都不写就 `return`** —— **不刷内容**。</item>
        /// <item>旁证（🔴 **2026-10-12（A447）亲读指令流订正**）：`WindowsManager__ShowPreviousWindow`
        ///   （VA `0x180876560`）末段 = `set_CurrentWindow(currentWindow)` + **无参** `GameWindow.TryOpen()`
        ///   （`mov rcx,[rdi+0x58]` → `xor edx,edx` → `call 0x180835fd0`，**一个实参都不喂**）。
        ///   ⚠️ 这里原来把它写成 `GameWindow__TryOpen(win, 0)` —— **调的目标地址是 `0x180835fd0`（无参那条）**，
        ///   那个 `0` 是 il2cpp 调用约定的**尾槽**（Ghidra 在本工程的打印里一律印成 `0`；同函数里就有例子：
        ///   `WindowsManager__set_CurrentWindow(this, value, 0)`，而那个 setter 只有一个实参）
        ///   ⇒ 这一跳**一个 `data` 都没喂**。⛔ 别读成「传了 `null`」（那会走带参那条、清空 `Data`）。</item>
        /// </list>
        /// ⇒ 原版「同窗再开」**只重喂 `data`、不重建内容** —— 刷内容**只可能**来自各窗**自己 override 的
        /// `TryOpen(data, options)`（全库 7 个：`DeckEditingWindow` / `DeckSelectionPopup` / `PurchasePremiumWindow` /
        /// `RankedEventWindow{,V2}` / `SinglePlayerOnlyEnergyWindow` / `SkirmishEventWindow`，形状一律
        /// **先调基类、再补自己的刷新**）**或 `Open()`（全库 57 个，只在 `Closed⇒Open` 那一拍被基类调）**。</para>
        ///
        /// <para>🔴 **关掉就是销毁**（`WindowsManager__CloseWindowCO.c`：`Close` → 摘 `openWindows` →
        /// `automaticallyLoadedWindows` 命中的 **`Object.Destroy(win.gameObject)`** + 释放 + 删缓存条目）
        /// ⇒ **「复用不重建」只在「窗还开着」时成立**；关过之后再点，原版走**新建**（我们这一侧同义：
        /// `OpenByRef` 的 `StillOpen` 判假 ⇒ 新建一扇，新扇 `state == Closed` ⇒ 照常重建内容）。
        /// ⚠️ 我们关窗只 `SetActive(false)`、不销毁（老账 A123），所以「关过之后再开」在本仓可能是
        /// **同一个实例**重走本方法 —— 那时 `CurrentState` 是 `Closed`、照样走重建支，与原版可观测行为一致。</para>
        ///
        /// <para>⚠️ **两处如实标注**（铁律 3）：
        /// ① `SetupData` 在**所有**支里都跑（V8 读出的 ① 在分档**之前**）—— 它是**纯数据写入**
        ///    （`GameWindow__SetupData.c` 只写 `+0x48 windowOptions` / `+0x40 data`，**一个 UI 字段都不碰**；
        ///    我们这一份 = 基类 `Data = data`，三个覆写 `CampaignRewardWindow` / `RewardWindow` /
        ///    `MissionRerollPopup` 也只是记 `_ctx` / `Index`）⇒ 放哪一支都一样、**不会重建内容**。
        /// ② 原版 `Open` 支末尾那句 `ToFocus()`（虚表 Slot 7）本仓**没有对应物**（全库零处；
        ///    原版 `GameWindow__ToFocus.c` 本身是空桩）—— 如实记，⛔ 不编一个出来。
        /// ③ 原版 `OnOpen`（`+0x58`）委托本仓没有（全仓 `OnOpen` 只命中 `TryOpenTab` 那一族）⇒ 不实现。</para>
        ///
        /// <para>🔴 **2026-10-12（A447）本方法拆成【两条重载】，照原版那两个重载**：
        /// 带参这条（本方法）= 原版 `TryOpen(data, options)`（VA `0x180836120`，**先 `SetupData`**）；
        /// 无参那条 <see cref="TryOpen()"/> = 原版 `TryOpen()`（VA **`0x180835fd0`**，**一次都不调 `SetupData`**）
        /// —— 两条的差别**只有这一句**（三支本体逐句相同）。⛔ 别再拿「传 `null`」当无参那条的替身：
        /// 传 `null` 走的是**本方法**、`SetupData(null)` 会把 `Data`（与三个覆写里的 `_ctx` / `Index`）**清空**。
        /// ⇒ **调用方要按「原版那一跳调的是哪条重载」来选**（`ShowPreviousWindow` ⑥ 已改调无参那条）。</para>
        ///
        /// <para>🔴 **`Open` 支随手加一条出声**（红线「不许静默失败」）：原版这一支是**静默早退**；
        /// 但「`CurrentState` 说开着、物体却不活」在**本仓**是一种**只可能来自绕过 `Close()`/`Hide()`**
        /// 的坏局面（A217① 的出声判据就是为它设的，见 `Close()`）⇒ 走到这一支还撞上它**必须出声**，
        /// 否则本方法会把「早退」变成**静默留脏**。正常路径**零命中**（两个合法写法都同时写 state 与激活态）。</para></summary>
        public bool TryOpen(object data)
        {
            SetupData(data);        // ① 原版第一句（`GameWindow__SetupData.c`：只写那两个数据字段，不碰 UI）
            return OpenByState();   // ②③④ = 两条重载共用的分档本体
        }

        /// <summary>原版那条【**无参**】重载 `GameWindow.TryOpen()` = **VA `0x180835fd0`** ——
        /// 与带参那条**逐句同构**，**唯一差别 = 不调 `SetupData`** ⇒ `Data` / 覆写里的 `_ctx` / `Index`
        /// **原样保留**（⛔ 不是「传 `null` 的同一个东西」——那条会把它们清空，见 <see cref="TryOpen(object)"/>）。
        ///
        /// <para>🔴 **判据 = 本件（A447）亲读指令流**（`工具/disasm_va.py`，⛔ 不是照报告抄）：
        /// `0x180835FD0` 在 il2cpp 类初始化守卫之后**头一条就是** `mov esi, dword ptr [rbx + 0x68]`
        /// （读 `CurrentState`）——**它之前没有** `mov rax,[rcx]` + `call [rax+0x188]`；
        /// 而带参那条 `0x180836120` 序言之后**头三句**正是
        /// `mov rax,[rcx]` / `mov r9,[rax+0x190]` / `call [rax+0x188]`（= `SetupData`，槽位自洽链 →
        /// `资料/普查产出_1012/H14_打架定案与标称字号.md` §一·③）。两条的三支本体
        /// （`0x180835F01…0x180836112` ↔ `0x1808361EB…`）逐句相同（`Closed ⇒ 播音 + SetActive(true)
        /// + state=1 + call [rax+0x178](Open()) + OnOpen`；`Background ⇒ state=1 + call [rax+0x1a8]`；
        /// `Open ⇒ ret`）。</para>
        ///
        /// <para>**谁用它**：`WindowsManager.ShowPreviousWindow` ⑥（A447 起）—— 原版那一跳的指令流
        /// （`WindowsManager__ShowPreviousWindow` @ `0x180876560`，`0x1808767C1`–`0x1808767D0`）=
        /// `mov rcx, qword ptr [rdi + 0x58]`（= 字段 `currentWindow`，`dump.cs` 字段表 `0x58`）
        /// → `xor edx, edx` → **`call 0x180835fd0`** ⇒ 调的**就是这一条、且一个实参都不喂**。</para></summary>
        public bool TryOpen()
        {
            return OpenByState();
        }

        /// <summary>②③④ 三支本体（**两条 `TryOpen` 重载共用一份**）。
        /// ⚠️ **如实标注（铁律 3）**：原版那是**两个各自编译出来的方法体**（内容逐句相同），
        /// 本仓**合成一个私有帮手** —— 这是**我们挑的**写法（少一份会漂的第二份实现），语义逐句等价。
        /// ⛔ 别把 `SetupData` 再挪进来（它是带参那条独有的第一句）。</summary>
        bool OpenByState()
        {
            var state = CurrentState;

            if (state == WindowState.Closed)
            {
                // ② 原版 `state == 0` 那一支：才做「播音 → 激活 → 进 Open 态 → Open()」
                if (openSound != null) WFSoundPlayer.Play(openSound, true, 1f, 1f);  // = 原版 `SoundManager.Play2D(..., MixerType.FX)`
                gameObject.SetActive(true);                                          // 建场景时窗口是关着的（照原版）
                CurrentState = WindowState.Open;
                Open();                    // ← 🔴 **内容只在这一支里重建**（A217② 的核心）
                ApplySmallScreenScale();   // 🆕 A165 —— 原版这一段写在 `GameWindow.Open()` 里，见方法注释
                return true;
            }

            if (state == WindowState.Background)
            {
                // ③ 原版：`state` 写回 `Open` + 提回前台（`call [rax+0x1a8]`）。**不播开窗音、不 `Open()`**
                Debug.Log($"[Win] `{name}`：`TryOpen` 命中 **`Background`** 支 —— 只提回前台，**不重建内容**"
                          + "（照原版 `GameWindow.TryOpen`：`Background ⇒ state=Open + 前台动作`，不调 `Open()`）");
                gameObject.SetActive(true);          // ← 我们的「前台动作」那半边（原版那一刻那句的等价物）
                CurrentState = WindowState.Open;
                return true;
            }

            // ④ 原版 `state == 1` 那一支：**一个字段都不写就 return**（同窗再开不刷内容）
            if (!gameObject.activeSelf)
                Debug.LogWarning($"[Win] `{name}`：`TryOpen` 早退（`CurrentState = Open`，照原版）—— "
                    + "**但它的物体是 `inactive`**，这个组合只可能来自「绕过 `Close()`/`Hide()` 直接改激活态」"
                    + "（那条路没有记账）⇒ 照原版这一支**什么都没做**，⛔ 别把它当成「已经开好了」"
                    + "（判据与同族那三条 → `Close()` 的守卫与 `Editor/ShellScene.cs` 的 A217 那一节）");
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
        /// ⇒ 写进 `Open()` 等于对绝大多数窗**不生效**（静默）。`TryOpen`（**两条重载都走 `OpenByState`**）
        /// 是本工程唯一的开窗入口
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
        /// 这正是原版 `HideAllWindows` 判据的形状（predicate `b__49_0` = `存活 &amp;&amp; *(int*)(w+0x68) != 0`
        /// ⇒ **只藏 state≠Closed 的**），也是「全屏窗开时把弹窗一起藏掉、上面那扇关掉之后它们还能回来」
        /// 那条链的前提（回来 = `ShowPreviousWindow` 认**列表尾** → `UnHide()`（列表尾是弹窗那一支）
        /// / **`TryOpen()`（无参那条）**（列表尾那一支；A217② 起就是**完整** `TryOpen`；
        /// ⚠️ **A447 起调的是无参重载** —— 原版那一跳调的就是它、**不重喂 `data`**，见那边的注释）。</para>
        /// <para>⚠️ **2026-10-12（A217②）就地订正（铁律 5）**：本行原来写 `UnHide`/`ReopenFromBackground` ——
        /// 后者**在生产路径上已被 `TryOpen` 取代**（它只是 `TryOpen` 非 `Closed` 支的替身，
        /// A217② 把那一支并回 `TryOpen` 之后就不再需要；方法还留着，只为 `Editor/ShellScene.cs` 的既有调用点）。</para>
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
            // ==================== 🆕 2026-10-17（W_G）：关窗音 ====================
            // 判据 = `GameWindow__Close.c` 逐句：① 两个 null 判据 ② `!gameObject.activeSelf ⇒ return`
            //   ③ **播关窗音**（`closeSound` 为空且 `useDefaultCloseSoundIfNull` ⇒ `SoundManager.defaultCloseSound`）
            //   ④ 才把活交给 `WindowsManager.CloseWindow`（= 我们的「置灰 + 摘表」那一对）。
            // ⇒ **放在守卫之后、`CurrentState = Closed` 之前**：原版那一刻窗还活着（activeSelf 为真）。
            // ⚠️ `Hide()` 那条路**不播**（原版 `GameWindow__Hide.c` 里没有这一句）—— ⛔ 别把它挪进 `Hide()`。
            PlayCloseSound();
            CurrentState = WindowState.Closed;
            Data = null;
            // 🔴 **2026-10-12（`A1709`）就地改次序（铁律 5）**：本行原来排在 `NotifyClosed` **之后**
            //   （旧形状 = `if (Manager != null) Manager.NotifyClosed(this);` 在前、`gameObject.SetActive(false);` 在后）。
            //   **原版次序正好相反**，判据 = `d:/2/tools/decomp_full/`：
            //     · `GameWindow__Close.c`（Slot 8，VA `0x180835930`）自己**只做两件事** —— 首句守卫
            //       （`!gameObject.activeSelf ⇒ return`）+ 播关窗音，然后把活交给 `WindowsManager.CloseWindow`；
            //     · `WindowsManager__CloseWindowCO.c`：**先** `(**(code **)(*param_2 + 0x1c8))(…)`（= 虚表
            //       **Slot 9 `Hide()`**，偏移 `0x1c8` 由 `WindowsManager__HideAllWindows.c:63` 同一处调用互证），
            //       而 `Hide()` 的逐句（`GameWindow__Hide.c`）=「守卫 → `state = Closed` → `SetActive(false)`」；
            //       **之后**才 `List.Remove(param_2)`、再 `ShowPreviousWindow()`。
            //   ⇒ 原版 = 「物体置灰」排在「摘表 / `ShowPreviousWindow`」**之前**，我们排在**之后**。
            //
            //   🔴 **不改会怎样（本笔要修的那条可见缺陷）**：`NotifyClosed` 那一跳会把**常驻主菜单重新开回来** ——
            //   关掉列表里**最后一扇**时 `ShowPreviousWindow` 走「列表空了」那一支（本类 `:1209-1222`
            //   = 原版 `CurrentWindow = baseMenu` + `OpenWindow(baseMenu)`）；而当**被关的正是 `baseMenu` 自己**时
            //   （`CloseAllWindows()` 的下降循环里它在下标 0、**最后**才被关到），那一支把 `this` 重新
            //   `SetActive(true)` + `state = Open`；紧跟着的老尾句又把它摁灰 ⇒ 造出 `(state = Open, activeSelf = false)`
            //   —— 正是 `:543-548` 那条守卫**自己点名**的非法组合（它写「只可能来自绕过 `Close()`/`Hide()`」）。
            //   ⚠️ 这一句 `SetActive(false)` **照原版必须有**（= `Hide()` 的尾句），错的只是它的**位置**。
            //
            //   🔴 **同族那一档 —— `A1714`，2026-10-14 已补上**（原文留痕，铁律 5）：
            //     这里原来写「同族偏离（本笔【没】顺手改，如实标注）：原版 `CloseWindowCO` 里还有一档我们**没有**的判据
            //     —— **「关的这扇就是 `baseMenu`」（`param_2 == *(manager + 0x48)`）⇒ 只 `Hide()` 就 `return`**……
            //     本笔用**次序**那半边把脏态消掉；『补不补那一支』是**另一笔**，⛔ 别在这儿顺手加」。
            //     **现在加了** —— 判据**逐句重读** `d:/2/tools/decomp_full/WindowsManager__CloseWindowCO.c`：
            //       `cVar1 = UnityEngine_Object__op_Equality(param_2, *(param_1 + 0x48))`（`0x48` = `baseMenu` 字段）；
            //       **为真那一支**（那份 `.c` 末尾的 `else if (param_2 != 0)`）= `(**(code **)(*param_2 + 0x1c8))(…)`
            //       （= 虚表 **Slot 9 `Hide()`**，`0x1c8` 与 `HideAllWindows.c:63` 互证）+ **`return`** ——
            //       ⇒ **既不 `List.Remove`、也不 `ShowPreviousWindow`、也不 `Destroy`**；
            //       **为假那一支**才是我们照抄的那条（`Hide()` → `Remove` → `Destroy` + 缓存释放 → `wasTop ⇒ ShowPreviousWindow`）。
            //   🔴 **为什么它非做不可**（= `A1709` 缺的那一半）：原版 `CloseAllWindows` 收尾那句是**无条件 `List.Clear()`**，
            //     它敢清表的前提**就是这一支** —— 关它自己时**不会**被 `ShowPreviousWindow` 重新开回来。
            //     我们缺这一支 ⇒ 关它自己会触发「列表空了 ⇒ `CurrentWindow = baseMenu` + `OpenWindow(baseMenu)`」
            //     ⇒ 它又变成 `Open` + active、还被 `Add` 回表 ⇒ 那次 `Clear` 就把它摘成孤儿（= A1709 的根因）。
            //   ✅ **本支 + `CloseAllWindows()` 第 ③ 步（只摘 `Closed` 的，见那个方法）合起来的净效果 = 原版**：
            //     `baseMenu` 被**藏**（`state = Closed` + `SetActive(false)`）**且不在表里** —— 与原版
            //     「`Hide()` + `List.Clear()`」逐位同效（⛔ 那一圈的 `Closed` 过滤句因此**不用动**）。
            //   ⚠️ **只做原版那一件**：这一支**不碰 `Data`**（原版那一刻只调了 `Hide()`；清 `Data` 是**我们**在
            //     另一支里加的，⛔ 别顺手也搬到这一支来 —— 两处形状不同是**原版如此**）。
            //   ⚠️ **`BaseMenu` 只有壳那条路会设**（`MainMenuRuntime.RegisterMainMenuWindow` → `SetBaseMenu`）
            //     ⇒ 独立场景里它恒 null ⇒ 本支**零命中**（与加它之前逐位一致）。
            // ==================== 🆕 A1714：被关的这扇 == `baseMenu` ⇒ 只 `Hide()` 就 `return` ====================
            //   （判据见上；⚠️ 用 `==` 而不是 `ReferenceEquals` —— 原版那一跳走的正是 `Object.op_Equality`。）
            if (Manager != null && Manager.BaseMenu == this) { Hide(); return; }
            // ==========================================================================================================
            gameObject.SetActive(false);                      // = 原版 `CloseWindowCO` 里那一跳 `Hide()` 的尾句（**排在摘表之前**）
            if (Manager != null) Manager.NotifyClosed(this);  // = 原版 `List.Remove` + `ShowPreviousWindow`（排在置灰之后）
        }

        /// <summary>🆕 **2026-10-17（音效线 `W_G`）**：原版 `GameWindow.Close` 里那一句关窗音
        /// （`GameWindow__Close.c` 的 `SoundManager__Play2D(…, uVar2, 0)`，`uVar2` 的取法见下面注释）。
        /// <para>逐句：`if (closeSound == null &amp;&amp; useDefaultCloseSoundIfNull) cue = SoundManager.Instance.defaultCloseSound;`
        /// （反编译里那段 `op_Implicit` + `*(param_1 + 0x38)` + `lVar3 + 0x38` 三句就是它），
        /// 然后**无条件** `SoundManager.Play2D(cue, MixerType.FX)` —— cue 为空时原版是静默的
        /// （`showNullAudioPlaysLog = 0`，实读 `bundle_staticgeneralassets_assets_all` 的 SoundManager MB）。
        /// ⇒ 这里同样：**两个来源都空 ⇒ 不播、也不出声**（那是原版的正常路径，不是缺陷）。</para>
        /// <para>⚠️ 我们的 `closeSound` 是 `AudioClip`（与 `openSound` 同一个口径 —— 我们把原版单 clip 的
        /// `AudioCue` 直接摊成 clip）；**默认**那条走 `WindowSounds` 的 cue（它带 `timeToPlayAgain = 0.1` 的节流）。</para></summary>
        void PlayCloseSound()
        {
            if (closeSound != null)
            {
                WFSoundPlayer.Play(closeSound, true, 1f, 1f);   // = 原版 `Play2D(..., MixerType.FX)`
                return;
            }
            if (!useDefaultCloseSoundIfNull) return;            // 原版：两个来源都空 ⇒ 静默
            WindowSounds.PlayFx(WindowSounds.DefaultCloseCue, $"窗 `{name}` 的 Close()");
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
        /// `gameObject.SetActive(true)` + `CurrentState = Open`（两行，无其它副作用）。
        ///
        /// <para>🔴 **2026-10-12（A217②）就地改口径（铁律 5）**：本方法原来是「原版 `TryOpen` 非 `Closed` 支」
        /// 的**替身**（单列出来是因为当时我们的 `TryOpen` **恒调 `Open()`**、并进去会把「同窗再开重建内容」
        /// 那个既有偏离固定下来 —— 见 `资料/普查产出_1010/乙_A177_A217_A228_A241.md` §⑤·1）。
        /// A217② 已把 `GameWindow.TryOpen` 按原版**按 `CurrentState` 分三档**
        /// （`Closed ⇒ 重建内容` / `Background ⇒ 只提前台` / `Open ⇒ 什么都不做`）
        /// ⇒ **本方法现在是 `TryOpen` 那两档的严格子集**，生产路径**已不再调用它**
        /// （`ShowPreviousWindow` ⑥ 调 **完整** `prev.TryOpen()`（**无参**那条，VA `0x180835fd0`）
        /// = 原版那一跳 —— ⚠️ A447 订正：原来写的是 `prev.TryOpen(null)`，那会**清空 `Data`**）。
        /// ⛔ **别再把它当成「回来」的生产入口** —— 它**不处理 `Closed`**（那一档原版要 `SetActive(true)` +
        /// `state=Open` + **`Open()` 重建内容**），漏掉它 = 藏过的窗回来时是空的（`Editor/ShellScene.cs` 那条
        /// 同名断言将来要一起换）。</para>
        /// <para>⚠️ **保留它只为既有调用点**：`Editor/ShellScene.cs:1861-1862`（自检直调；A217 那一节）。
        /// （同族先例：`WindowsManager.ShowPreviousWindow` 在 `Warpforge_code` 里也是 `private` ——
        /// 自检**不**直调生产方法、走生产那条路更有鉴别力。）</para>
        /// <para>⚠️ 原版 `TryOpen` 那一支后面还有一句 `ToFocus()`（虚表 Slot 7）—— 本仓**没有这个虚方法**
        /// （全库零处），而原版 `GameWindow__ToFocus.c` 本身是空的（`decomp_full` 已知的错桩之一）
        /// ⇒ 无对应物，如实记。</para></summary>
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
        ///   → `call 0x182feb6f0`（= `Behaviour.get_enabled`）→ `test al,al; je &lt;return>`
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
        /// `RewardsWindow`(0) / `DailyStreakPopup`(0) / `BoosterPackOpenWindow`(0) / **`PopUpGameWindow`(0)**
        /// （= 2026-10-12 A416 收编之后 `ShowPopUp` 开的那扇，原版 prefab 的 `closeOnESC = 0`）都属这一档，
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
            // ③ 原版**第三道门槛**：`updateNavPanel`（0x3A）—— 见上面那一格的 doc。
            //    原版这一句是 `NavigationPanelController.SetMainToggle(this)`（`__ESCPressed.c` 末段）。
            //    ⚠️ 只在**这一支**（ESC 真的关掉了这扇窗）里调 —— 原版就是这一处，⛔ 别顺手挪进 `Close()`：
            //    那样连「窗自己的返回钮 / 代码关窗」也会改灯，而原版那些路**不动灯**。
            //    `SetNavToggle` 在没有主菜单实例（卡组编辑 / 对战 / 壳）时**什么都不做**，是正常路径。
            if (updateNavPanel) MainMenuRuntime.SetNavToggle(MainMenuRuntime.NavigationPanelToggleType.Main);
            return true;
        }
    }
}
