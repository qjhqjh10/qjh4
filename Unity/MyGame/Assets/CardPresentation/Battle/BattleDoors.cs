// BattleDoors.cs — 结算的「开门」：播原版 Victory / Defeat / Draw 视频
// （对应原版 `EndBattleDoors` + 运行时 dump 里 `BattleHud/Canvas/BattleDoors` 那棵子树）
//
// ---- 它为什么是「播视频」而不是动画（证据链，2026-09-12）----
//   - 反编译 `EndBattleDoors.SetupDoor`（`decomp_out/EndBattleDoors__SetupDoor.c`，193 行）：
//     建 RenderTexture → 贴给 `Video Image` 的 RawImage → `VideoPlayer.targetTexture` →
//     选片 → **返回 `VideoClip.length()`**，调用方拿它 `WaitForSeconds`。
//   - 那个类里**没有 Animation / Animator / DOTween**，字段桩只有
//     `VideoPlayer` + `RawImage` + 三条 `AssetReferenceTyped<VideoClip>`。
//   - 运行时 dump 里 `BattleDoors` 子树是单链：`BattleDoors`(activeSelf=False) → `Canvas`
//     → `EndBattlePanel` → `Background` / **`Video Image`** / `AllRewardsHolder`。没有别的节点。
//   - 98 个唯一 AnimationClip 里结算相关的 0 个；解包里也没有 `EndBattleDoors` 的预制体或 clip。
//
// ---- 原件在哪 ----
//   `素材/Warpforge原版/视频/videos/{Victory,Defeat,Draw} Video.mp4`
//   （实为 matroska/webm + VP8，3840×1080 / yuv420p）。拷进
//   `Assets/CardPresentation/Resources/Art/videos/{victory,defeat,draw}.webm`
//   —— `Art/` 整个目录是 gitignore 的，**原版资产不进仓库**。
//   目录删掉 → `Resources.Load` 返回 null → 本组件 `HasVideo=false`，
//   结算界面照常显示（只是没有开门视频），**不静默卡住**。
//
// ---- ⚠️ 和原版不一样的一处（实测，不是猜）----
//   原版 `VideoClip` 元数据是 `Width:1920 Height:1080 m_HasSplitAlpha:true` ——
//   那是**原版导入时已经拆好的产物**（bundle 里存的就已经是 3840×1080 的左右拼）。
//   我们把同一份字节丢进自己的工程，Unity 报的是 **3840×1080**（`VideoProbe.Run` 实测），
//   **Unity 不会替我们拆左右半**。所以：
//     · RT 必须是 **3840×1080**（照 clip 实际尺寸，否则视频会被压扁进 RT）；
//     · 用自建 shader `CardPresentation/Video Split Alpha` 在显示时合成
//       （左半取色、右半取明度当 alpha）；
//     · 显示区还是原版的 **1920×1080**（dump 里 `Video Image` 的尺寸），所以宽高比要盖过。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CardPresentation
{
    public class BattleDoors : MonoBehaviour
    {
        public enum Result { Victory = 10, Defeat = 20, Draw = 30 }   // 值照原版枚举

        /// <summary>视频放原版资产目录下（gitignore）。删掉这个目录 = 没有开门视频，界面照常。</summary>
        const string ResourceRoot = "Art/videos/";

        static readonly Dictionary<Result, string> ClipName = new Dictionary<Result, string>
        {
            { Result.Victory, "victory" },
            { Result.Defeat,  "defeat"  },
            { Result.Draw,    "draw"    },
        };

        /// <summary>🆕 **开门音效**（2026-09-27 接）。原版每一档都配一支 `AudioCue`，走
        /// `SoundManager.Play2D(cue, MixerType.Jingles /*=3*/)`（`EndBattleDoors__SetupDoor.c:113`；
        /// 三支 cue 的字段偏移 = `victoryCue` 0xB0 / `defeatAudioCue` 0xB8 / `drawAudioCue` 0xC0）。
        /// 🔴 **这三个不在那三条 .mp4 里** —— 原版那三条片子**没有音轨**（实测 `audioTrackCount=0`），
        /// 音效是**另播**的；我们原来**一支都没接**（`audioOutputMode = None` ⇒ 静默）。
        /// 资产：`素材/Warpforge原版/音频/音效库/AudioClip/{MatchVictory,MatchDefeat,MatchDraw}.ogg`
        /// ⇒ 已拷进 `Resources/Art/audio/sfx/`（`Art/` 整个目录 gitignore，**原版资产不进仓库**）。
        /// ⚠️ 删掉那三支 ⇒ `Resources.Load` 返回 null ⇒ **静默跳过音效**（画面照常，与删视频同一条退化路）。</summary>
        static readonly Dictionary<Result, string> CueName = new Dictionary<Result, string>
        {
            { Result.Victory, "MatchVictory" },
            { Result.Defeat,  "MatchDefeat"  },
            { Result.Draw,    "MatchDraw"    },
        };
        const string CueRoot = "Art/audio/sfx/";

        /// <summary>🆕 原版 `SetupDoor` **返回的是 `clip.length + 一个常量`**（不是光片长）——
        /// 那个常量是 `EndBattleDoors` ctor 里的 `commonAnimationScaleTime`
        /// （`EndBattleDoors__.ctor.c:5`，`0x3e19999a` = **0.15f**）。
        /// ⚠️ 这个值同时也是 `ShowRewards` 里奖励入场 `SetDelay` 用的那个常量
        /// （`EndBattleDoors__SetupDoor.c:194`）⇒ **别把它当成「随便多等一会儿」**。
        /// 📌 判据：2026-09-27 那轮 `SetupDoor` 反编译复核（10/0x14/0x1e = `BattleWinner` 枚举那次）。</summary>
        public const float CommonAnimationScaleTime = 0.15f;

        /// <summary>片长（秒）—— 原版 `SetupDoor` 就是把 `VideoClip.length` 返回给调用方等的。
        /// 这三个数是**用 `VideoProbe.Run` 从这份资产里实测出来的**，写在这里是给自检当期望值用。</summary>
        public static readonly Dictionary<Result, float> ExpectedLength = new Dictionary<Result, float>
        {
            { Result.Victory, 3.71f },
            { Result.Defeat,  6.79f },
            { Result.Draw,    3.40f },
        };

        const string ShaderName = "CardPresentation/Video Split Alpha";

        // ---- 分层：面板里三层的 z ----
        //   🔴 **2026-10-16（`T09` 批 2）：这一对（结算面板 + 开门）已迁到 uGUI 子画布上**
        //     ⇒ 下面这三个 z 的**含义变了、值一个没动**：
        //       · **旧（世界空间 quad）**：世界 z，**z 越小越靠近相机**（画在越前面）；
        //       · **新（uGUI）**：`HudCanvas.Add` 的**兄弟序键**，**z 大的排前面 = 画在底下**。
        //     ⇒ **语义同向（大的都在后）、值直接照抄**（口径 → `T09` §五·补⑩ 三）。
        //   三层：压暗 **-0.50**（`ZDim`，最靠后 = 画在最底下）
        //     → 视频 **-0.55**（`ZVideo`，盖在压暗上）→ 文字与骷髅 **-0.60**（`ZContent`，再盖在视频上）。
        // ⚠️ **踩过的坑（仍然成立）**：文字/骷髅原来和压暗**同一个 z（0）**、靠渲染顺序压；加上视频之后
        //    必须**显式拉开**，否则视频会盖住文字（视频层一度和压暗同层 ⇒ 整块面板都看不见）。
        //    uGUI 档同理：三层各占一个不同的 z，兄弟序才是确定的。
        // ⚠️ 原来这里写「压暗层 **z = 0**（`EndPanel.Build` 里建 dim 时给的就是 `Vector3.zero`，别去动它）」
        //    —— **那句已过期**（`EndPanel` 的 dim 早已改到 `ZDim = -0.50`）⇒ 就地订正（铁律 5）。
        /// <summary>整屏压暗那一层。**比视频和内容都靠后**，但必须比任何一张卡靠前 ——
        /// 给 0 的话手牌（z 0~0.24）会从面板底下亮着透出来。</summary>
        public const float ZDim = -0.50f;
        public const float ZVideo = -0.55f;
        public const float ZContent = -0.60f;

        VideoPlayer _player;
        RenderTexture _rt;
        /// <summary>🔴 **2026-10-24（`B-26`）**：原版 `BattleDoors/Canvas`（GO `Canvas_1091`）那一级
        /// —— 本件 + `EndPanel` 共用的**子画布**（由 <see cref="CreateEnvelope"/> 建在
        /// `BattleDoors` 节点**下面**，照原版的树）。</summary>
        RectTransform _canvas;
        /// <summary>共用的子画布（原版 `BattleDoors/Canvas`）。`null` = 没建起来（已出声）。</summary>
        public RectTransform CanvasTransform { get { return _canvas; } }
        /// <summary>显示层。🔴 **2026-10-16（`T09` 批 2）起是 uGUI 的 `RawImage`**（不再是世界空间 quad）——
        /// 两条判据：① **原版那一件本身就是 `RawImage`**（见文件头「它为什么是播视频」那段）；
        /// ② **`Image` 这条路 API 上根本走不通**：`Image` 要的是 `Sprite`，而 `Sprite.Create` 只吃
        /// **`Texture2D`** —— 我们的图源是 `RenderTexture`（⛔ 不是「没试」，是不能）。
        /// ⚠️ 节点挂在原版 `Video Image` 下面（`B-26` 起；它自己就是原版那一颗的「显示」半边）
        /// ⇒ 跟着父链的 `localScale 1.06647`（`A2105②`：画出来 2048×1152）。</summary>
        RectTransform _screenRt;
        RawImage _screen;
        Material _mat;
        /// <summary>开门音效那个 `AudioSource`（🆕 2026-09-27）。</summary>
        AudioSource _sfx;

        float _length, _t;

        /// <summary>🔴 **结算后出口的那道闸** = 原版 `BattleManager.matchFinishedAndWaitingToLeave`
        /// （`+0x510`，`dump.cs:30916`）。它**不是**「结算面板显示着」—— 原版是
        /// **开门视频播完（+0.15s）之后**才置位（`BattleManager._CloseBattleDoors_d__393__MoveNext.c:72`），
        /// 置位之前点/按都不理（`BattleManager__Update.c:105` 那句 `if (0x510 == 0) return;`）。
        /// ⚠️ **显式字段、不拿 `_t >= _length` 现推**：`Stop()` **故意不清 `_t`**（留给自检对账，
        ///   见 `Stop` 的注释）⇒ 上一局播完的 `_t` 会留在片长上，第二局 `SetupDoor` 之后、
        ///   `Play()` 之前那一瞬按算式判会**误判成「已播完」**（= 闸门提前开）。</summary>
        bool _finished;

        /// <summary>这一段有没有视频（没有就跳过、直接显示面板内容）。</summary>
        public bool HasVideo { get; private set; }

        /// <summary>视频正播着（自检用）。</summary>
        public bool Playing { get; private set; }

        /// <summary>这次播的是哪条（`victory`/`defeat`/`draw`；没视频时是空串）。</summary>
        public string Clip { get; private set; }

        /// <summary>源片长（秒），0 = 没视频。原版 `SetupDoor` 的返回值语义。</summary>
        public float Length { get { return _length; } }

        /// <summary>播到第几秒了（自检用）。</summary>
        public float Elapsed { get { return _t; } }

        /// <summary>🔴 **出口闸门开了没有**（= 原版 `BattleManager.matchFinishedAndWaitingToLeave`）。
        /// **置位时刻照原版**：`Advance` 把时间推到 `_length`（= 片长 + 0.15f，见 `CommonAnimationScaleTime`）
        /// 的那一下 —— 对应原版 `_CloseBattleDoors_d__393__MoveNext.c:51-56` 的 `WaitForSeconds(片长+0.15)`
        /// 走完、`:72` 那一句置位。
        /// ⚠️ **没有开门视频**（`Resources/Art/videos/` 被删那一档）⇒ **当场为真** —— 原版那条
        ///    `WaitForSeconds(0)` 也是立刻过；不让玩家在一屏没有视频的结算界面上**卡死**。
        /// 📌 读者只有一个：`EndPanel.ExitReady` → `BattleDriver.HandleEndBattleExit()`（点/ESC 走人）。</summary>
        public bool Finished { get { return _finished; } }

        /// <summary>🔴 **这一轮开门视频的【最后一帧】还留着当底图吗**（自检用）。
        /// 默认 `false`（原版行为：`Advance` 推够片长后 `Pause()`，**画面停在最后一帧**）；
        /// `DropFrame()` 之后为 `true`（那一帧**不再留**，视频层也不画了）。
        /// 📌 判据：`BattleDoors.Advance` 的收尾是 `_player.Pause()` —— 解码器停在最后一帧、
        ///   `_screen` 那颗 quad 也还 active ⇒ **结算面板底下永远垫着那帧画面**。
        ///   原版那一支（`_CloseBattleDoors` 协程）之后会走 `BattleHud.ToggleWithAnimation` 等收尾，
        ///   我们这边没有对应的收帧动作 ⇒ 「跳过演出」这一档要**显式**把帧丢掉（`Z1′`）。</summary>
        public bool FrameDropped { get; private set; }

        /// <summary>显示区尺寸（照 dump 里 `Video Image` 的 1920×1080）。</summary>
        public const float ScreenWidthPx = 1920f;
        public const float ScreenHeightPx = 1080f;

        /// <summary>🔴 **原版 `BattleDoors` 节点自己的 `RectTransform` 尺寸**（2026-10-11 · A218 现读）：
        /// `bundle_scenes_scenes_battlearena1` 的 `GameObject «BattleDoors»`（go_pid 60）→ `RectTransform`
        /// `anchor (0.5,0.5) 重合` · `pivot (0.5,0.5)` · **`m_SizeDelta = (1920.1199, 1118.9810)`** ·
        /// **`m_AnchoredPosition = (−0.064, −5.276)`** · **`m_LocalScale = 1.0665`**（⇒ 视觉 2048×1193，
        /// 比屏大一圈 —— 开门视频要盖过屏幕边缘，**原版如此**）。
        /// ⚠️ 我们这一层不渲染任何东西（真正的显示是 `_screen` 那颗 quad）⇒ 矩形**照抄原版那一对数值**。
        /// <para>🔴 **2026-10-24（`A28`）就地订正（铁律 5）**：本段原文最后一句写着「缩放**不复刻**」
        /// —— **已过期**（`A2105②` 已把 `localScale` 补上，见 `DoorsScale`）；而 **`m_AnchoredPosition`
        /// 那一对当时确实没抄**（`CreateEnvelope` 只写了 `sizeDelta` ⇒ 落在 (0,0)，整块面板/视频偏高 5.28 px）。
        /// 现在补上 → **`DoorsApX/Y`**（同一份 `RT` = `RectTransform_2717.json`，逐位照抄）。</para></summary>
        const float DoorsBoxW = 1920.1199f, DoorsBoxH = 1118.9810f;

        /// <summary>🔴 **2026-10-24（`A28`）：`BattleDoors` 自己的 `m_AnchoredPosition`**（原版
        /// `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2717.json`，m_GameObject = GO 60
        /// = «BattleDoors»；`m_AnchorMin/Max = m_Pivot = (0.5, 0.5)`）。
        /// 📌 同一对值在 `2D层_battlearena2全树.md:1195` 那棵树里表现为「整块比屏心低 5.28 px」
        /// （`[-960,526 1920x1119]` 的中心 y = 1085.28 vs 画布中心 1080）。
        /// 🧨 漏抄（= 我们改之前的 (0,0)）⇒ `Editor/BattleScene.cs` 的 `A28·Doors` 那条红。</summary>
        const float DoorsApX = -0.0640869140625f, DoorsApY = -5.276123046875f;

        /// <summary>🔴 **2026-10-15（账 `A2105②`）**：原版 `BattleDoors` 自己的 `m_LocalScale`（实测 lossyScale
        /// **1.06647**；序列化字段读出来是 **1.0665**，两者差 0.003%、可忽略）。
        /// ⚠️ **我们原来【不复刻】它**（旧注释写着「我们的节点 `localScale` 恒 1」）—— 那是**判据就在父链上没照**，
        /// 后果 = 整个结算转场小 6.65%（开门视频该 2048×1152、骷髅底板该 691.2×55.9）。
        /// 判据 → `资料/普查产出_1015战斗/C2_逐格对账_弹窗与结算转场.md`。</summary>
        const float DoorsScale = 1.06647f;

        /// <summary>🔴 **2026-10-16（`T09` 批 2）：这一对（结算面板 + 开门）【共用】的那块子画布的名字** ——
        /// 原版宿主 = `BattleDoors/Canvas`（GO `Canvas_1091`；判据 → `T09c` §一）。
        /// ⚠️ 名字**不叫** `Canvas`（那是原版 GO 自己的名字，落在我们这边太容易撞）—— 取原版的**路径名**。</summary>
        public const string CanvasName = "BattleDoors Canvas";

        /// <summary>🔴 **2026-10-16（`T09` 批 2）：拿这一对共用的子画布**（没有就建）。
        /// <para>**原版宿主** = `BattleDoors/Canvas`：**`PopUps` 层 / `sortingOrder 0`**、
        /// `overrideSorting = true`、父链 `BattleHud/Canvas/BattleDoors`（判据 → `T09c` §一 那两行）。</para>
        /// <para>🔑 **为什么必须是【子】画布、且必须挂在根 canvas 下**：子画布**继承根 canvas 的 `CanvasScaler`**
        /// （原版全场景就一个、在根上）⇒ 非 16:9 下整块面板才跟着缩放。⛔ 挂在 `hudRoot` 下**拿不到缩放**
        /// （`scaleFactor` 恒 1）⇒ 今天那个「4:3 下整体偏大 1.333 倍」的毛病**原样留着**，而且
        /// **16:9 下逐位相同、看不出来**（口径 → `T09` §五·补⑨ 一·2）。</para>
        /// <para>🔴 **2026-10-16（修波 `FD` · 账 `F10`）就地订正（铁律 5）**：本段原来写的是
        /// **「⛔ 幂等：`HudCanvas.EnsureSubCanvas` 每调一次就新建一个同名对象 ⇒ 这里先按名字找」** ——
        /// **那句【已过期】**：`EnsureSubCanvas` 早已改成**按名字幂等**（复用路径还会把层/order 对齐到本次传的
        /// 值 + 重排次，见它的 doc）。⇒ 下面 `PairCanvas` 里那个「先查再建」的包装**已删**，直接调它即可
        /// （原来那句理由「`EndPanel` 与 `BattleDoors` 两边都会调它」**由 `EnsureSubCanvas` 自己保证**）。</para>
        /// <para>拿不到根画布（`LayoutSpace.Cam` 为空）⇒ 出声并返回 `null`，由调用点决定**不建那一件**
        /// （⛔ 绝不退回世界空间 quad —— 半迁移态比不迁移更坏，且 16:9 下看不出来）。</para></summary>
        public static RectTransform PairCanvas()
        {
            HudCanvas.Ensure(LayoutSpace.Cam);        // 幂等（根 canvas 要 `LayoutSpace.Apply` 之后才立得起来）
            if (HudCanvas.RootRect == null)
            {
                Debug.LogError("[BattleDoors] 根 canvas 没建起来（`LayoutSpace.Cam` 为空？）" +
                               "⇒ 结算面板与开门**这一对都建不出来**");
                return null;
            }
            // 🔴 **2026-10-24（`B-26`）**：这块画布现在**挂在 `BattleDoors` 节点下面**（照原版的树
            //   `BattleHud/Canvas/BattleDoors/Canvas`）⇒ 先按名字找（`SubCanvas` 已含嵌套），
            //   找不到才建**外骨架**（外骨架会把它建在自己的 `CanvasTransform` 上）。
            //   ⛔ 别再像改之前那样直接在根 canvas 下平铺一块同名的 —— 那会与 `BattleDoors` 分家。
            var found = HudCanvas.SubCanvas(CanvasName);
            if (found != null) return (RectTransform)found.transform;
            var d = CreateEnvelope();
            return d != null ? d.CanvasTransform : null;
        }

        /// <summary>
        /// 🔴🔴 **2026-10-24（`B-26`）**：建**外骨架** —— 原版那棵树是
        /// **`BattleHud/Canvas/BattleDoors/Canvas/EndBattlePanel`**
        /// （出处 `资料/说明书/01_战斗_对战/2D层_battlearena2全树.md:1195-1200`）。
        /// <para>本函数只建上面那**两级**：`BattleDoors`（RT · `m_SizeDelta (1920.1199, 1118.981)` ·
        /// `localScale 1.06647`）+ 它下面的 `Canvas`（我们沿用自己的名字 `BattleDoors Canvas`）。
        /// 返回的组件的 `gameObject` **就是原版那颗 `BattleDoors`**（`EndBattleDoors` 脚本也挂在它身上）。</para>
        /// <para>⛔ **视频那两颗节点不在这里建**：原版 `Video Image` 是 `EndBattlePanel` 的**子件**
        /// ⇒ 由 <see cref="BuildVideo"/> 按「面板根」建（`EndPanel.Build` 调它）。
        /// 改坏法：把面板再挂回根 canvas（= 我们改之前那棵 `EndPanel → BattleDoors` 的倒置树）
        /// ⇒ `Editor/BattleScene.cs` 的 `B-26` 那条「父子链」断言红。</para>
        /// </summary>
        public static BattleDoors CreateEnvelope()
        {
            HudCanvas.Ensure(LayoutSpace.Cam);              // 幂等（根 canvas 要 `LayoutSpace.Apply` 之后才立得起来）
            if (HudCanvas.RootRect == null)
            {
                Debug.LogError("[BattleDoors] 根 canvas 没建起来（`LayoutSpace.Cam` 为空？）"
                             + "⇒ 结算面板与开门**这一对都建不出来**");
                return null;
            }
            var go = new GameObject("BattleDoors", typeof(RectTransform));
            go.transform.SetParent(HudCanvas.RootRect, false);
            // 根矩形 = **原版 `BattleDoors` 自己的 `m_SizeDelta`**（见 `DoorsBoxW/H` 的注释）。
            var goRt = (RectTransform)go.transform;
            HudCanvas.SetPxSize(goRt, DoorsBoxW, DoorsBoxH);
            // 🔴 **2026-10-24（`A28`）**：`m_AnchoredPosition = (−0.064, −5.276)` —— 与 `sizeDelta` **同一份 RT**
            //   （`RectTransform_2717.json`）。`SetPxSize` 只写 `sizeDelta`（锚点/枢轴居中），**不碰位置**
            //   ⇒ 不补这一句就落在 (0,0)：整块面板 / 视频 / 骷髅**比原版高 5.28 px**（16:9 下也看得出来，
            //   只是没有对照物时不容易发现）。判据见 `DoorsApX/Y`。
            goRt.anchoredPosition = new Vector2(DoorsApX, DoorsApY);
            // 🔴 **2026-10-15（账 `A2105②`）**：原版这一颗的 `m_LocalScale = 1.0665`（实测 lossyScale 1.06647）。
            //   加在这一颗上、**不是**逐件改尺寸：原版就是「父件带缩放、子件照自己的 sizeDelta」，
            //   子件（`EndBattlePanel` / `Video Image` / 骷髅底板）跟着一起放大才对。
            go.transform.localScale = Vector3.one * DoorsScale;
            var d = go.AddComponent<BattleDoors>();
            // 原版 `BattleDoors/Canvas`（GO `Canvas_1091`）= 这一对共用的**子画布**（PopUps / order 0）。
            d._canvas = HudCanvas.EnsureSubCanvas((Transform)go.transform, CanvasName,
                                                  HudCanvas.SortingLayer.PopUps, 0);
            return d;
        }

        /// <summary>
        /// 建一颗**独立的** `BattleDoors`（外骨架 + 视频节点），给探针用（先例 `A218`）。
        /// <paramref name="panelRoot"/> = 原版 `EndBattlePanel` 那一级
        /// （`Video Image` 挂在它下面）；传 `null` ⇒ 退到共用的子画布下（探针自己没建面板时）。
        /// ⚠️ 生产路**不**走这里 —— `EndPanel.Create` 自己调 <see cref="CreateEnvelope"/> 再
        /// 用 `CanvasTransform` 建面板，然后 `EndPanel.Build` 调 <see cref="BuildVideo(_root)"/>。
        /// </summary>
        public static BattleDoors Create(Transform panelRoot)
        {
            var d = CreateEnvelope();
            if (d == null) return null;
            // 🔴 **2026-10-24（`B-26`）**：探针常把「裸节点」当面板根传进来（不在任何画布上）——
            //   那时退到**共用的子画布**下（与改之前的选宿主口径同义）。两个理由：
            //   ① uGUI 件不在画布父链上**根本不渲染**；② 自检那把「画布子件按**设计 px** 读 `rect`」
            //      的尺子只在画布父链下成立（否则 `sizeDelta` 会被当成世界单位、差 108 倍）。
            var host = panelRoot != null && panelRoot.GetComponentInParent<Canvas>() != null
                     ? panelRoot : (Transform)d._canvas;
            d.BuildVideo(host);
            d.Hide();
            return d;
        }

        /// <summary>🔴 **2026-10-24（`B-26`）**：建视频那两颗节点 —— 原版
        /// **`EndBattlePanel/Video Image`**（`[-966,481 1920x1080]`，`VideoPlayer` 的宿主）。
        /// <para>⚠️ **如实记一处「一拆二」**：原版 `Video Image` 一颗节点上同时挂着 `VideoPlayer` 与
        /// `RawImage`；我们拆成 **`Video Image`（`VideoPlayer` 宿主）+ 它的子件 `video_screen`（`RawImage`）**
        /// —— 因为 `DropFrame()` 只关显示层、不关解码器那一颗（合成一颗会让「丢帧」顺手把解码器停掉）。
        /// 两层的位置/尺寸**逐值照旧**（`Video Image` 1920×1080 居中；`video_screen` 中心偏移 (−6, +64)）。</para>
        /// </summary>
        public void BuildVideo(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                Debug.LogError("[BattleDoors] `BuildVideo` 没有面板根 ⇒ `Video Image` 建不出来（结算时没有开门视频）");
                return;
            }
            // 原版 `Video Image`：`VideoPlayer` 组件的宿主就是它（`bundle_scenes_scenes_battlearena1` 的
            // `VideoPlayer_3593` → GO «Video Image» 实读）。矩形 = **1920×1080**（`RT sizeDelta`、中心锚）。
            var go = new GameObject("Video Image", typeof(RectTransform));
            go.transform.SetParent(panelRoot, false);
            HudCanvas.SetPxSize((RectTransform)go.transform, ScreenWidthPx, ScreenHeightPx);
            // 兄弟序：压暗(Background, −0.50) → 视频(−0.55) → 奖励/内容(−0.60) ⇒ 画出来
            // 「压暗在底、视频居中、内容在最上」（与改之前那三层同值，见 `ZDim/ZVideo/ZContent`）。
            HudCanvas.Add((RectTransform)go.transform, ZVideo);
            _player = go.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = false;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.None;   // 这三条片子**没有音轨**（实测 audioTrackCount=0）
            _player.skipOnDrop = true;
            _player.gameObject.SetActive(false);

            // 🆕 开门音效那条路（**与视频无关的另一支** —— 原版那三条 .mp4 没有音轨）。
            //    宿主挂在 `BattleDoors` 外节点上（它是「一次开门」的那颗脚本节点，和原版同构）。
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.loop = false;
            _sfx.spatialBlend = 0f;                                       // 原版就是 `Play2D`
            _sfx.outputAudioMixerGroup = WarpforgeAudio.JinglesGroup;      // 原版 `MixerType.Jingles`（=3）
        }

        /// <summary>
        /// 按结果准备开门视频 + 开门音效。**对应原版 `EndBattleDoors.SetupDoor`** —— 包括「返回片长」这个约定。
        /// </summary>
        /// <returns>片长（秒）+ <see cref="CommonAnimationScaleTime"/>（**原版就是这么返回的**）。
        /// **0 表示没有视频**（资产不在，或还没挑到片），调用方据此决定等不等。</returns>
        public float SetupDoor(Result r)
        {
            Stop();
            HasVideo = false;
            Clip = null;
            _length = 0f;
            // 🔴 闸门**默认开着**，下面真取到片再关回去 —— 这样所有「早退 / 取不到片」的分支
            //    （`ClipName` 里没有这一档 / `Resources.Load` 拿不到资产 / `Art/` 被删）
            //    都自动落在「没片可等 ⇒ 当场可走」这一档（原版 `WaitForSeconds(0)`）。
            _finished = true;
            FrameDropped = false;      // 新的一轮开门 ⇒ 上一轮「丢掉的那一帧」这件事作废

            string name;
            if (!ClipName.TryGetValue(r, out name)) return 0f;

            var clip = Resources.Load<VideoClip>(ResourceRoot + name);
            if (clip == null) return 0f;      // 资产不在（Art/ 被删了）——不是错误，退回「无视频」这条路

            _rt = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
            _rt.Create();

            _player.gameObject.SetActive(true);
            _player.clip = clip;
            _player.targetTexture = _rt;

            // 🔴 **2026-10-16（`T09` 批 2）**：显示层从「世界空间 quad」改成 uGUI 的 **`RawImage`**
            //    （为什么不是 `Image` → 见 `_screen` 的 doc）。
            //    位置/尺寸**逐值照旧**：旧那行 `ImageQuad.Create(transform, _rt, EndPanel.Pos(1920/2−6, 540−64, …), 1080/108, …)`
            //    = 「中心相对**父中心** (−6, +64) px ＋ `sizeDelta (1920, 1080)` px」——
            //    `EndPanel.Pos` 的式子就是 `(px−960)/108, (540−py)/108`，而 uGUI 的锚点也在父中心、单位就是 px
            //    ⇒ **同一对数**（推导 → `T09` §五·补⑩ 三 那张换算表）。
            //    ⚠️ 旧那句 `SetAspect(1920/1080)` **不需要了**：旧 quad 是「按 **RT** 的比例（3840/1080）定宽」才要盖掉，
            //       uGUI 的 `sizeDelta` 本来就是**显式写死**的 1920×1080。父件那一级 `localScale 1.06647`
            //       ⇒ 画出来仍是**原版的 2048×1152**（`A2105②`）。
            const float cxPx = ScreenWidthPx * 0.5f - 6f;    // 954（dump：`Video Image` @ x = −6）
            const float cyPx = 540f - 64f;                   // 476（dump：@ y = +64）
            var sgo = new GameObject("video_screen", typeof(RectTransform));
            // 🔴 **2026-10-24（`B-26`）**：挂到原版 `Video Image` 那一颗下面（它 = `VideoPlayer` 宿主）。
            //   位置是「相对**父中心**的 (−6, +64) px」，父件本身就是中心锚、1920×1080 ⇒ 与挂在
            //   `EndBattlePanel` 上**逐位同址**（父件的中心就是面板中心），画面不动。
            sgo.transform.SetParent(_player != null ? _player.transform : transform, false);
            _screenRt = (RectTransform)sgo.transform;
            // 🔴 **2026-10-16（修波 `FD` · `G5` 报的）：原来这里内联了这个式子的【中心锚特化形】**
            //   `new Vector2(cxPx − 960f, 540f − cyPx)`（`0.5 × 1920` 与 `1080 × 0.5` 都**精确可表示**
            //   ⇒ 与 `HudCanvas.AnchorApPx(…, 0.5, 0.5)` **逐位等价**，本波已复算，见报告 §④）。
            //   ⇒ 改走那个口（铁律：两处写同一条规则 = 迟早不一致）。
            // ⚠️ `HudCanvas.SetAnchorPx` **只管锚点与位置、⛔ 不碰 `pivot`**（见它的 doc）⇒ 枢轴仍在这里
            //   显式写；`anchorMin/anchorMax` 由它一并设成 `(0.5, 0.5)`，与改写前同一对值。
            _screenRt.pivot = new Vector2(0.5f, 0.5f);
            HudCanvas.SetAnchorPx(_screenRt, 0.5f, 0.5f, cxPx, cyPx);   // = (−6, +64)
            _screenRt.sizeDelta = new Vector2(ScreenWidthPx, ScreenHeightPx);
            _screen = sgo.AddComponent<RawImage>();
            _screen.texture = _rt;
            _screen.color = Color.white;
            // 🔴 **2026-10-20（W57 · 战斗侧 `T09` 步骤 A3-d）：`raycastTarget` 由 `false` 改成 `true`。**
            //   判据（**原版 prefab 现读**，`bundle_scenes_scenes_battlearena1` 的 MB 4961 与
            //   `…battlearena2` 的 MB 5177 两份同值）：`EndBattlePanel/**Video Image**` 那颗 `RawImage`：
            //     `m_RaycastTarget = 1` · `m_RaycastPadding = (0,0,0,0)` · 无 `Button` / `EventTrigger`。
            //   （原版那颗是默认值没关 —— uGUI 新建 Graphic 时 `raycastTarget` 默认就是 true。）
            //   ⚠️ **不挂点击处理器**：原版这一层只是吃射线（模态闸的一部分，压在压暗层之上）；
            //   「点任意处离开」走的是 `BattleManager.Update` 的裸输入，**不经过 uGUI**（见 `EndPanel.Build` 压暗那段）。
            //   ⚠️ 对时序/回调**零影响**：`Playing` / `_finished` / `Advance` / `DropFrame` 全不读它。
            //   ⚠️ 这张图只在 `SetupDoor` 取到片时才建 ⇒ **没有视频那一档只有压暗层在挡**（原版同样没有这一层）。
            _screen.raycastTarget = true;
            HudCanvas.Add(_screenRt, ZVideo);        // 兄弟序按 z 排（见文件头「分层」那段）

            var sh = Shader.Find(ShaderName);
            if (sh == null)
            {
                Debug.LogWarning("BattleDoors：找不到 shader `" + ShaderName +
                                 "`，视频会按原样（左右两幅并排）显示 —— 打包前记得把它注册进 Always Included");
            }
            else
            {
                _mat = new Material(sh);
                if (_screen != null) _screen.material = _mat;   // uGUI 侧 = `Image.material` 的对应物（`RawImage` 同）
            }

            HasVideo = true;
            Clip = name;
            _length = (float)clip.length + CommonAnimationScaleTime;
            _finished = false;                 // 有片 ⇒ 闸门关着，等 `Advance` 推够 `_length`（见 `Finished`）
            PlayCue(r);
            return _length;
        }

        /// <summary>开播。</summary>
        public void Play()
        {
            if (!HasVideo || _player == null) return;
            _t = 0f;
            _finished = false;      // 重播 = 闸门重新关上（再等这一遍片长；照原版「一次开门 = 一次等待」）
            Playing = true;
            // 🆕 上一轮若 `DropFrame()` 过，这里把显示层挂回来（否则重播看得见声音、看不见画面）
            if (_screen != null && !_screen.gameObject.activeSelf) _screen.gameObject.SetActive(true);
            FrameDropped = false;
            _player.gameObject.SetActive(true);
            _player.Play();
        }

        /// <summary>停下并丢掉这轮的状态（重开一局要调）。
        /// ⚠️ **不清 `_t`** —— 播完之后 `Elapsed` 要留在片长上给自检对账；
        ///    `Play()` 自己会把它归零，所以留着不会串到下一局。</summary>
        /// <summary>这一档的开门音效**加载得到吗**（自检用：缺了会出声警告，但不该悄悄少一件）。</summary>
        public static bool HasCue(Result r)
        {
            string cue;
            if (!CueName.TryGetValue(r, out cue)) return false;
            return Resources.Load<AudioClip>(CueRoot + cue) != null;
        }

        /// <summary>放这一档的开门音效（原版 `SoundManager.Play2D(cue, MixerType.Jingles)`）。
        /// ⚠️ **资产不在就只出一行警告、画面照常**（与视频同一条退化路：删掉 `Resources/Art/` 之后
        /// 结算界面照常显示，只是没声、没片）。</summary>
        void PlayCue(Result r)
        {
            if (_sfx == null) return;
            string cue;
            if (!CueName.TryGetValue(r, out cue)) return;
            var clip = Resources.Load<AudioClip>(CueRoot + cue);
            if (clip == null)
            {
                Debug.LogWarning("BattleDoors：没有开门音效 `" + CueRoot + cue +
                                 "`（原版每档一支、走 Jingles 组）—— 视频照播，只是**没声**");
                return;
            }
            _sfx.PlayOneShot(clip);
        }

        public void Stop()
        {
            Playing = false;
            _finished = false;                      // 闸门跟着这次开门一起作废（下一次 `SetupDoor` 重新定）
            if (_sfx != null) _sfx.Stop();          // 🆕 音效也停（开第二局时别把上一局的 jingle 拖着）
            if (_player != null)
            {
                _player.Stop();
                _player.targetTexture = null;
                _player.clip = null;
                _player.gameObject.SetActive(false);
            }
            if (_screen != null) { DestroyImmediate(_screen.gameObject); _screen = null; _screenRt = null; }
            if (_rt != null) { _rt.Release(); DestroyImmediate(_rt); _rt = null; }
            if (_mat != null) { DestroyImmediate(_mat); _mat = null; }
            HasVideo = false;
        }

        public void Hide()
        {
            Stop();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 推时间。**时长按 `_length` 自己算，不看 `VideoPlayer.isPlaying`** ——
        /// 批处理里没有帧循环，解码器出不出帧跟这段时序逻辑是两件事（`BattleScene.Step` 手动推，
        /// 真机由 `BattleDriver.Update` 每帧推）。这样时序在批处理里也是可断言的。
        /// </summary>
        /// <returns>这一推**刚好播完**返回 true（用来触发「揭晓奖励」那一步）。</returns>
        public void Advance(float dt)
        {
            if (!Playing) return;
            _t += dt;
            if (_t >= _length)
            {
                _t = _length;
                Playing = false;
                if (_player != null) _player.Pause();
                // 🔴 **原版就在这一刻开闸**：`WaitForSeconds(SetupDoor 的返回值)` 走完
                //    → `BattleManager._CloseBattleDoors_d__393__MoveNext.c:72` `matchFinishedAndWaitingToLeave = true`。
                _finished = true;
            }
        }

        /// <summary>🆕 2026-10-18（`A940` 尾账 `Z1′`）：**把最后一帧丢干净** —— 视频层不再画、解码器停掉。
        ///
        /// 🔴 **为什么需要它**：「跳过正常结算演出」那一档（`skipNormalBattleEnd*`）我们的做法是
        ///   把时间**一帧推完**（`Advance(_length)`，见 `EndPanel.Show` 的 `skipSequence`），
        ///   而 `Advance` 的收尾是 `_player.Pause()` ⇒ **解码器停在最后一帧、`_screen` 还 active**
        ///   ⇒ 玩家「跳过」之后，结算面板底下**仍然垫着那段视频的最后一帧**。
        ///   这一条只做一件事：**把那一帧扔掉**（⛔ 不碰 `_finished` / 不碰闸门 —— 「跳过之后能不能走」
        ///   由 `Advance` 那一步决定，两者是两件事，别在这里顺手开闸）。
        ///
        /// ⚠️ **幂等**；⚠️ **没有视频**（`HasVideo == false`）时什么都不做（也就没有「最后一帧」可丢）。
        /// ⚠️ 下一轮 `SetupDoor()` 会把 <see cref="FrameDropped"/> 清回 false 并把显示层重新挂出来。
        /// </summary>
        public void DropFrame()
        {
            if (!HasVideo) return;
            if (_player != null) _player.Stop();      // 解码器停掉（不再 hold 最后一帧）
            if (_screen != null) _screen.gameObject.SetActive(false);
            FrameDropped = true;
        }

        /// <summary>视频那层显不显示（自检用）。</summary>
        public bool ScreenVisible
        {
            get { return _screen != null && _screen.gameObject.activeSelf; }
        }

        /// <summary>显示区尺寸（自检用）：世界宽 × 世界高。
        /// 🔴 **2026-10-16（`T09` 批 2）**：uGUI 档用 `GetWorldCorners()` **直接量世界尺寸**
        /// （⛔ 不是 `sizeDelta × 某个系数` —— 那个换算**随分辨率变**，写死系数就是重犯「两套尺」那个错；
        /// 同 `HudPiece.WorldW` 文件头那段）。量出来的仍**是世界单位** ⇒ 读者一个字不用改。</summary>
        public Vector2 ScreenWorldSize
        {
            get
            {
                if (_screenRt == null) return Vector2.zero;
                var c = new Vector3[4];
                _screenRt.GetWorldCorners(c);
                return new Vector2(Mathf.Abs(c[3].x - c[0].x), Mathf.Abs(c[1].y - c[0].y));
            }
        }

        /// <summary>视频层的贴图（自检用，读像素）。</summary>
        public Texture ScreenTexture { get { return _screen == null ? null : _screen.texture; } }

        /// <summary>解码器已经解到第几帧（自检用）。**-1 = 一帧都没出**。
        /// ⚠️ 它和 `Playing` 是两回事：`Playing` 是**我们的时序**（按片长算），
        ///    这个是**解码器**的真实进度 —— 批处理里只有把主线程还给编辑器环路它才动。</summary>
        public int DecodedFrames { get { return _player == null ? -1 : (int)_player.frame; } }

        // ── 🆕 2026-10-20（W57 · `T09` 步骤 A3-d）：**Editor-only 只读探针** ────────────────────
        //  ⚠️ 只读、且**播放器构建里不存在**（`#if UNITY_EDITOR`）。断言写在 `Editor/BattleScene.cs`
        //     （本波口径 = **只做不验** ⇒ 本件不写断言）。
        //  🔑 这一层**没有按钮**（原版 `Video Image` 上零 `Button` / 零 `EventTrigger`）⇒ 探针读的是
        //     「它吃不吃射线」这一件事：原版 `m_RaycastTarget = 1`（判据见 `SetupDoor` 里那一段）。
        //  ⚠️ 显示层**只在 `SetupDoor` 取到片时才建**、`Stop()` 里会销毁 ⇒ 没视频那一档这里全是「没有」。
#if UNITY_EDITOR
        /// <summary>视频层那颗 `RawImage`（`null` = 这一轮没建：没视频 / 已 `Stop()`）。Editor-only 只读探针。</summary>
        public RawImage ScreenGraphicForTest { get { return _screen; } }

        /// <summary>视频层吃不吃射线（原版 `Video Image.m_RaycastTarget = 1`）。Editor-only 只读探针。</summary>
        public bool ScreenRaycastTargetForTest { get { return _screen != null && _screen.raycastTarget; } }

        /// <summary>视频层的 `raycastPadding`（**L,B,R,T**；原版 `(0,0,0,0)`）。Editor-only 只读探针；
        /// 没有那件时返回 **四个 `NaN`**（⛔ 不拿 0 冒充「查过」）。</summary>
        public Vector4 ScreenRaycastPaddingForTest
        {
            get
            {
                return _screen != null ? _screen.raycastPadding
                                       : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            }
        }
#endif
    }
}
