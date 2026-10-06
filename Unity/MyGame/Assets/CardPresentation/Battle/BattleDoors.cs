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

        // ---- 分层：面板里三层的 z（相机在 z=-20 朝 +z 看，**z 越小越近**）----
        //   压暗层 **z = 0**（`EndPanel.Build` 里建 dim 时给的就是 `Vector3.zero`，别去动它）
        //   → 视频 **-0.55**（盖在压暗上）
        //   → 文字与骷髅 **-0.60**（再盖在视频上）
        // ⚠️ 原来文字/骷髅和压暗是**同一个 z（0）**、靠渲染顺序压；加上视频之后必须显式拉开，
        //    否则视频会盖住文字。踩过一次：视频层一度和压暗同层，整块面板都看不见。
        /// <summary>整屏压暗那一层。**比视频和内容都靠后**，但必须比任何一张卡靠前 ——
        /// 给 0 的话手牌（z 0~0.24）会从面板底下亮着透出来。</summary>
        public const float ZDim = -0.50f;
        public const float ZVideo = -0.55f;
        public const float ZContent = -0.60f;

        VideoPlayer _player;
        RenderTexture _rt;
        ImageQuad _screen;
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

        /// <summary>显示区尺寸（照 dump 里 `Video Image` 的 1920×1080）。</summary>
        public const float ScreenWidthPx = 1920f;
        public const float ScreenHeightPx = 1080f;

        /// <summary>🔴 **原版 `BattleDoors` 节点自己的 `RectTransform` 尺寸**（2026-10-11 · A218 现读）：
        /// `bundle_scenes_scenes_battlearena1` 的 `GameObject «BattleDoors»`（go_pid 60）→ `RectTransform`
        /// `anchor (0.5,0.5) 重合` · `pivot (0.5,0.5)` · **`m_SizeDelta = (1920.1199, 1118.9810)`** ·
        /// `m_AnchoredPosition = (−0.064, −5.276)` · **`m_LocalScale = 1.0665`**（⇒ 视觉 2048×1193，
        /// 比屏大一圈 —— 开门视频要盖过屏幕边缘，**原版如此**）。
        /// ⚠️ 我们这一层不渲染任何东西（真正的显示是 `_screen` 那颗 quad）⇒ 矩形**照抄原版那一对数值**，
        /// 缩放不复刻（我们的节点 `localScale` 恒 1）。</summary>
        const float DoorsBoxW = 1920.1199f, DoorsBoxH = 1118.9810f;

        public static BattleDoors Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`
            //    （= 原版 `BattleDoors` 自己的 rect，见 `DoorsBoxW/H` 的注释）。
            //    改坏法：删掉 `SetPxSize` 那句 ⇒ `Editor/BattleScene.cs` §A218「`BattleDoors` 的 rect = 原版矩形」红。
            var go = new GameObject("BattleDoors", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, DoorsBoxW, DoorsBoxH);
            var d = go.AddComponent<BattleDoors>();
            d.Build();
            d.Hide();
            return d;
        }

        void Build()
        {
            // 🔴 **2026-10-11（A218）**：本节点是**原版那一颗 `Video Image`**（照 `Create` 上面那段注释：
            //    原版 `Video Image` 的 `Image` 上传着视频；`VideoPlayer` 组件的宿主**就是它** ——
            //    `bundle_scenes_scenes_battlearena1` 的 `VideoPlayer/VideoPlayer_3593` → 宿主 «Video Image» 实读）。
            //    ⇒ 矩形照它：**1920×1080**（RT · `sizeDelta (1920,1080)` · `anchor (0.5,0.5) 重合`）。
            //    ⚠️ 我们把它拆成两半：**组件宿主**（本节点）与**显示 quad**（`_screen`，1920×1080 那颗）；
            //    原版是同一颗节点。改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218 那条红。
            var go = new GameObject("video_player", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            MenuDraw.SetPxSize(go.transform, ScreenWidthPx, ScreenHeightPx);
            _player = go.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = false;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.None;   // 这三条片子**没有音轨**（实测 audioTrackCount=0）
            _player.skipOnDrop = true;
            _player.gameObject.SetActive(false);

            // 🆕 开门音效那条路（**与视频无关的另一支** —— 原版那三条 .mp4 没有音轨）
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

            string name;
            if (!ClipName.TryGetValue(r, out name)) return 0f;

            var clip = Resources.Load<VideoClip>(ResourceRoot + name);
            if (clip == null) return 0f;      // 资产不在（Art/ 被删了）——不是错误，退回「无视频」这条路

            _rt = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
            _rt.Create();

            _player.gameObject.SetActive(true);
            _player.clip = clip;
            _player.targetTexture = _rt;

            _screen = ImageQuad.Create(transform, _rt,
                                       EndPanel.Pos(ScreenWidthPx * 0.5f - 6f, 540f - 64f, ZVideo),
                                       ScreenHeightPx / EndPanel.PxPerUnit,
                                       new Vector2(0.5f, 0.5f), "video_screen");
            // RT 是左右拼的 3840×1080，显示区是 1920×1080 —— 宽高比必须盖掉，
            // 否则 `ImageQuad` 会按 RT 的 3.56 比例把画面拉成两倍宽。
            if (_screen != null) _screen.SetAspect(ScreenWidthPx / ScreenHeightPx);

            var sh = Shader.Find(ShaderName);
            if (sh == null)
            {
                Debug.LogWarning("BattleDoors：找不到 shader `" + ShaderName +
                                 "`，视频会按原样（左右两幅并排）显示 —— 打包前记得把它注册进 Always Included");
            }
            else
            {
                _mat = new Material(sh);
                if (_screen != null) _screen.SetMaterial(_mat);
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
            if (_screen != null) { DestroyImmediate(_screen.gameObject); _screen = null; }
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

        /// <summary>视频那层显不显示（自检用）。</summary>
        public bool ScreenVisible
        {
            get { return _screen != null && _screen.gameObject.activeSelf; }
        }

        /// <summary>显示区尺寸（自检用）：世界宽 × 世界高。</summary>
        public Vector2 ScreenWorldSize
        {
            get { return _screen == null ? Vector2.zero : new Vector2(_screen.WorldW, _screen.WorldH); }
        }

        /// <summary>视频层的贴图（自检用，读像素）。</summary>
        public Texture ScreenTexture { get { return _screen == null ? null : _screen.Texture; } }

        /// <summary>解码器已经解到第几帧（自检用）。**-1 = 一帧都没出**。
        /// ⚠️ 它和 `Playing` 是两回事：`Playing` 是**我们的时序**（按片长算），
        ///    这个是**解码器**的真实进度 —— 批处理里只有把主线程还给编辑器环路它才动。</summary>
        public int DecodedFrames { get { return _player == null ? -1 : (int)_player.frame; } }
    }
}
