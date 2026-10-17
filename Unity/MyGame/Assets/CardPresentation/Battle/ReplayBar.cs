// ReplayBar.cs — 原版 `ReplayButtons`（战斗界面左上角那 4 枚回放按钮）
//
// 出处（**坐标悬案 2026-09-17 已复核，以坑 38 为准**）：
//   `资料/索引与盘点/解包资源使用地图.md` 的坑 35 说它在屏外（`LeftArea` 相对 [-550,1117]、
//   观战才滑入）—— **那条是错的**：`ReplayButtons`(GO 143 / RT 3355) 的 `m_Father` 是
//   `Safe area BackCanvas`(RT 3498)，和 `LeftArea`(RT 2849) **平级**（两者都挂在 3498 下），
//   根本不在 LeftArea 里。按原始 JSON 手算 + 复跑 `chain_rect.py` 都是**屏内顶部**：
//
//     ReplayButtons 容器   x[410.2, 703.8]  y[37.3, 94.7]   293.60×57.41
//        Holder（中间层）   x[416.3, 674.8]  y[36.3, 95.7]   258.54×59.43（比容器还高 1 px，原版如此）
//        ├ Replay          x[419.6, 499.4]  y[41.7, 90.3]   79.80×48.57   图 `40K_replay_bt_restart`
//        ├ Play            x[498.7, 578.5]  y[41.7, 90.3]   79.80×48.57   图 `40K_replay_bt_play`
//        ├ Pause           x[498.7, 578.5]  y[41.7, 90.3]   79.80×48.57   图 `40K_replay_bt_pause`
//        └ StepPlay        x[577.9, 657.7]  y[41.7, 90.3]   79.80×48.57   图 `40K_replay_bt_next`
//     · 容器内相对 x = **9.42 / 88.47 / 88.47 / 167.67**，相对容器顶 **4.4**
//     · `Play` 与 `Pause` **同座标互斥**（同屏只看得见 3 枚）；场景默认态 = Play 可见、Pause 隐藏
//     · 四张图实测 **95×59（restart/next）/ 95×58（play/pause）**，`m_PreserveAspect=0` ⇒ **Simple 拉伸**
//       （所以我们用 `SetAspect(79.80/48.57)` 拉成原版那个矩形，不按贴图比例画）
//
// ✅ **2026-09-27：当年那两处「查不到」现在都查到了**（判据 → `资料/普查产出_0927/回放_界面真值.md`
//    §A/§B/§C 与 `回放_入口与数据链.md`）。原文照留痕，下面是订正后的口径：
//
//   ⚠️ **原来写的**：「① 什么时候显示 —— 原版哪个模式才出现（观战？回放？）**查不到**；
//      ② 这四个按钮接什么 —— 原版语义查不到 ⇒ 我们接的是**本局的时间控制**。」
//
//   ① **显示时机（已查到）**：`ReplayHud.Setup()` **只做一件事** =
//      `objHolder.SetActive(BattleManager.matchType == 0xA0)`（`0xA0 = 160 = MatchType.Replay`）。
//      ⇒ **只有「回放局」才出现**，普通对局里**整条是关的**。
//      🔴 **这意味着我们原来一直显示它 = 与原版不符** ⇒ 现在照原版做：`Setup(false)`，普通对局**不显示**。
//      ⚠️ **开关做在 `Holder` 这一层**（原版字段 `objHolder`，偏移 **0x48**），**不是根节点**。
//   ② **四颗钮的原版语义（已查到）**：`Replay → ClickRestartReplay`（从头重放）·
//      `Play → ClickPlayReplay`（继续）· `Pause → ClickPauseReplay`（暂停）· `StepPlay → ClickNextStepReplay`（单步）。
//      它们**全都是「回放的播放控制」**，不是「本局的时间控制」。
//      ⚠️ **我们那套时间控制没有删**（暂停/继续/单步/重开一局仍是活的）—— 只是**从这 4 颗钮上摘下来了**，
//      挪到**键盘**（`Space` 暂停/继续 · `.` 单步 · `R` 重开一局，见 `BattleDriver.Update`）。
//      理由：界面要**照原版**（原版普通局里根本没有这条），而那几个功能是我们自己的、留着当调试手段；
//      键盘不留痕，不会让画面对不上原版。
//   ③ 顺带订正一条旧注释：原文写「原版预制体的静态态正好相反（Play 可见/Pause 隐藏）——那是进战场前的样子」。
//      **真因是「场景序列化 ≠ 运行期」**：`ReplayHud.Initialize()` 跑完就是 **关 `Play`、开 `Pause`**
//      ⇒ 我们的 `SetPlaying(true)`（亮 Pause）**与原版一致**，那一条**不是「我们挑的」**。
//
// ---- 原来那段「我们挑的」留痕（已作废，别照它做） ----
// 🔴 **两处是「我们挑的」，别当成复刻**（原版查不到，见 `资料/战斗UI_原版对账表.md` §三·〇 :129）：
// ---- 原来那段「我们挑的」留痕（**已作废，别照它做**） ----
//   ① **什么时候显示** —— 原版哪个模式才出现（观战？回放？）**查不到**；
//   ② **这四个按钮接什么** —— 原版语义查不到。我们接的是**本局的时间控制**：
//        Replay = 重开一局（`BattleDriver.Restart`）—— 🔴 **2026-10-16 就地订正（W22 · 铁律 5）**：
//        本行原文写「和结算面板那句『按 R 再来一局』同一个入口」，**已过期** ——
//        结算面板那句提示**已按原版删掉**（原版那屏零文字零按钮），而 `R` 是**我们自己的调试键**
//        （原版**没有**「再来一局」；唯一的重开键在**回放模式**，见 `资料/普查产出_1016/判据_结算后出口.md` §2.6）。
//        结算后的**真出口**现在是「点屏幕任意处 / ESC ⇒ 回主菜单」（`BattleDriver.LeaveBattle`）。
//        Play / Pause = 暂停 / 继续（冻结事件时间线、回合计时与 AI 步进）
//        StepPlay = **单步**推进一条排在最前的事件
//   ⛔ **这两条现在都不成立了**（上面①②已经答了）—— 保留只为「当年为什么这么写」。
//      ⚠️ 但那四个**功能**没删（挪到键盘了），别以为它们没了。
//
// 🔴 **2026-10-17（`BSC62` · `W15`）：整条已从世界空间 `ImageQuad` 迁到 uGUI 根画布**
//   —— 判据 = 原版它是**画布件**（父链 `Replay <- Holder <- ReplayButtons <- Safe area BackCanvas <- Canvas`，
//   `battlearena2` 现读）。**原版 layer / order（`R5` 留的那条「没查清」—— 本次现核）**：
//   `BackCanvas` 的 `Canvas`（`Canvas_2768`）与根 `Canvas`（`Canvas_2765`）都是
//   `m_SortingLayerID = 0`(**Default**) · `m_SortingOrder = 0` · `m_OverrideSorting = false`
//   ⇒ 这一条**不另立排序组**，就画在根画布这一组里；层内先后由**兄弟序**定。
//   我们的等价物 = **根画布 + `HudCanvas.Add(z = 0.3)`**（与 HUD 图族同一档）。
//   ⚠️ 四枚钮的 `m_AnchoredPosition` **取原版序列化值**（`-86 / -6.9488 / -6.9488 / +72.2512`），
//   ⛔ 别再按 `BarX + SlotDx + BtnW/2` 反算（那会差 0.03 px，且把原版的两套坐标混成一套）。
using UnityEngine;

namespace CardPresentation
{
    public class ReplayBar : MonoBehaviour
    {
        /// <summary>点到了哪一枚</summary>
        public enum Btn { None, Replay, Play, Pause, Step }

        // ---- 原版实测值（1920×1080 设计 px；**全部逐值来自 battlearena2 现读**）----
        /// <summary>一枚按钮的矩形（原版 4 颗都是 `m_SizeDelta (79.8, 48.57)` · `pivot (0.5,0.5)`）——
        /// 原版 `m_PreserveAspect = 0` ⇒ **Simple 拉伸到该矩形**（所以不按贴图 95×59 的比例画）。</summary>
        const float BtnW = 79.80f, BtnH = 48.57f;

        /// <summary>每枚钮**相对 `Holder` 中心**的 `m_AnchoredPosition.x`（原版逐值：
        /// `Replay -86.0` · `Play / Pause -6.9487762451171875`（同座标互斥）· `StepPlay +72.251220703125`）。
        /// ⚠️ 与 `BarX + SlotDx + BtnW/2` 算出来的「**近似**值」差 0.03 px 以内 —— 这里取**原版序列化值**。</summary>
        static readonly float[] SlotApX = { -86.0f, -6.9487762451171875f, 72.251220703125f };

        /// <summary>`ReplayButtons`（根）的原版锚点/偏移/尺寸：`anchor (0.5,1)` · `pivot (0.5,0.5)` ·
        /// `m_AnchoredPosition (-403, -66)` · `m_SizeDelta (293.6000061035156, 57.40599822998047)`。
        /// ⇒ 绝对 rect `x[410.2,703.8] y[37.3,94.7]`（与文件头一致）。</summary>
        const float RootApX = -403f, RootApY = -66f;

        /// <summary>`Holder`（原版字段 `objHolder`，偏移 0x48）：`anchor/pivot (0.5,0.5)` ·
        /// `m_AnchoredPosition (-11.47700023651123, 0)` · `m_SizeDelta (258.5379943847656, 59.428001403808594)`
        /// ⇒ 绝对 rect `x[416.3,674.8] y[36.3,95.7]`（**比外面那条容器高 1 px，原版如此，别「修」**）。</summary>
        const float HolderApX = -11.47700023651123f;
        const float BarW = 293.6000061035156f, BarH = 57.40599822998047f;
        const float HolderW = 258.5379943847656f, HolderH = 59.428001403808594f;

        Transform _holder;

        /// <summary>uGUI 档的**兄弟序深度**（`HudCanvas.Add` 的 z；语义同旧世界 z：大的在底下）。
        /// 原版这一条挂在 `Safe area BackCanvas` 下、和 HUD 图族同一档 ⇒ 沿用 `HudImageZ` 的 0.3。</summary>
        const float Z = 0.3f;

        HudPiece _replay, _play, _pause, _step;

        /// <summary>自检用：4 枚图都取到了没有</summary>
        public bool HasArt
        {
            get
            {
                return _replay != null && _replay.Texture != null
                    && _play != null && _play.Texture != null
                    && _pause != null && _pause.Texture != null
                    && _step != null && _step.Texture != null;
            }
        }

        /// <summary>自检用：现在画面上是哪几枚（Play/Pause 互斥，所以永远 3 枚）</summary>
        public int VisibleCount
        {
            get
            {
                int n = 0;
                if (_replay != null && _replay.gameObject.activeSelf) n++;
                if (_play != null && _play.gameObject.activeSelf) n++;
                if (_pause != null && _pause.gameObject.activeSelf) n++;
                if (_step != null && _step.gameObject.activeSelf) n++;
                return n;
            }
        }

        /// <summary>自检用：暂停钮现在亮着没有（= 现在正在播，图标表示「点了会暂停」）</summary>
        public bool PauseShown { get { return _pause != null && _pause.gameObject.activeSelf; } }
        /// <summary>自检用：播放钮现在亮着没有（= 现在停着，图标表示「点了会继续」）</summary>
        public bool PlayShown { get { return _play != null && _play.gameObject.activeSelf; } }

        public static ReplayBar Create(Transform parent)
        {
            // 🔴 **2026-10-17（`BSC62` · `W15`）：根节点已从世界空间 quad 迁到 uGUI 根画布**。
            //    原版父链 `ReplayButtons <- Safe area BackCanvas <- Canvas`；**两块祖先 canvas 的
            //    `m_SortingLayerID = 0`(Default) · `m_SortingOrder = 0` · `m_OverrideSorting = false`**
            //    （`battlearena2` 的 `Canvas_2768` / `Canvas_2765` 现读）⇒ 它不另立排序组，
            //    就画在**根画布这一组**里（与 HUD 图族同一层）。⚠️ `parent`（原来是 `hudRoot`）
            //    **不再使用** —— uGUI 件一律挂 `HudCanvas.RootRect`，⛔ 别挂 `hudRoot`（那是反向补震用的）。
            //    改坏法：根节点不再是 `RectTransform`、或矩形不是原版 293.6×57.406
            //    ⇒ `Editor/BattleScene.cs` §A218「`ReplayBar` 根 = 293.6×57.41」红。
            var canvas = HudCanvas.Ensure(LayoutSpace.Cam);
            var go = new GameObject("ReplayBar", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (canvas != null)
            {
                rt.SetParent(canvas, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);   // 原版 `m_AnchorMin/Max (0.5,1)`
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(RootApX, RootApY);  // 原版 `m_AnchoredPosition (-403,-66)`
                rt.sizeDelta = new Vector2(BarW, BarH);               // 原版 `m_SizeDelta`
                HudCanvas.Add(rt, Z);
            }
            else
            {
                // ⛔ 不许静默退回世界空间 quad（半迁移态在 16:9 下看不出来）—— 出声。
                Debug.LogError("[ReplayBar] uGUI 根画布建不起来（`LayoutSpace.Cam` 为空？）⇒ 回放条**不显示**");
                rt.SetParent(parent, false);
            }
            var b = go.AddComponent<ReplayBar>();
            b.Build();
            return b;
        }

        void Build()
        {
            // 🔴 **`Holder` 这一层是照原版加的**（原版 `objHolder`，字段偏移 0x48）——
            //    `ReplayHud.Setup()` 的显隐开关**做在这一层**，不是根节点。
            //    ⚠️ 原版这个容器 `x[416.3,674.8] y[36.3,95.7]`，**比外面那条还高 1 px**（原版如此）；
            //    锚/枢轴 = 中心、`ap = (-11.477, 0)`、`sd = (258.538, 59.428)`（`RectTransform_3133` 现读）。
            var holderGo = new GameObject("Holder", typeof(RectTransform));
            var hrt = (RectTransform)holderGo.transform;
            hrt.SetParent(transform, false);
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(HolderApX, 0f);
            hrt.sizeDelta = new Vector2(HolderW, HolderH);
            HudCanvas.Add(hrt, Z);          // Holder 只分组；先后仍由它自己那一档的兄弟序
            _holder = hrt;

            _replay = Make("Replay",   SlotApX[0], "40K_replay_bt_restart");
            _play   = Make("Play",     SlotApX[1], "40K_replay_bt_play");
            _pause  = Make("Pause",    SlotApX[1], "40K_replay_bt_pause");
            _step   = Make("StepPlay", SlotApX[2], "40K_replay_bt_next");
            SetPlaying(true);      // 开局就在播 ⇒ 亮的是「暂停」（**与原版 `Initialize()` 一致**，见文件头 ③）
        }

        HudPiece Make(string goName, float apX, string art)
        {
            var tex = CardArt.Ui(art);
            if (tex == null)
            {
                // 图缺了要**说出来**（红线）—— 但别让整个 HUD 建不起来
                Debug.LogWarning($"[ReplayBar] 取不到图 `{art}`（回放条这一枚会是空的）");
                return null;
            }
            // 原版锚/枢轴都是中心，`m_AnchoredPosition = (SlotApX, 0)`；尺寸直接是原版
            // `m_SizeDelta (79.8, 48.57)` + `PA = 0` ⇒ **拉满该矩形**（⛔ 不按贴图 95×59 的比例）。
            var p = HudCanvas.MakeImageInParent(_holder, tex, goName, apX, 0f, BtnW, BtnH, Z, Color.white);
            if (p == null)
            {
                Debug.LogWarning($"[ReplayBar] `{goName}` 建不出来（`HudCanvas.MakeImageInParent` 回了 null）");
            }
            return p;
        }

        /// <summary>分辨率变了要重算 —— 🔴 **uGUI 档不需要**：位置/尺寸由 `RectTransform` 的锚点
        /// 自己跟画布缩放走（原版也是这么做的）。保留本口是因为 `BattleDriver.ReanchorHud` 还在调它，
        /// ⛔ 别在这里再加「重贴世界坐标」那一套（那就是半迁移态）。</summary>
        public void RefreshLayout()
        {
        }

        // ============================================================ 原版那两层（2026-09-27 补）

        /// <summary>原版 `ReplayHud.Setup()` —— **只做一件事**：
        /// `objHolder.SetActive(BattleManager.matchType == 0xA0)`（`0xA0 = 160 = MatchType.Replay`）。
        /// 🔴 我们**从不进回放局** ⇒ 传 `false` 时**整条不显示**（原版普通对局就是这样）。
        /// ⚠️ 开关在 `Holder` 上、**不在根节点**（照原版）。</summary>
        public void Setup(bool isReplayMatch)
        {
            if (_holder != null) _holder.gameObject.SetActive(isReplayMatch);
        }

        /// <summary>自检读口：这一层现在显示着吗（= 原版 `objHolder.activeSelf`）。</summary>
        public bool HolderVisible { get { return _holder != null && _holder.gameObject.activeSelf; } }
        /// <summary>自检读口：`Holder` 那个容器节点（原版 `objHolder`）。</summary>
        public Transform Holder { get { return _holder; } }

        /// <summary>这一下点在回放条的哪一枚上（没点中返回 <see cref="Btn.None"/>）。</summary>
        public Btn Hit(Vector3 world)
        {
            if (_replay != null && _replay.gameObject.activeSelf && _replay.Contains(world)) return Btn.Replay;
            // Play / Pause **同座标**（原版就是互斥的两枚图）⇒ 命中判据用**同一个矩形**，
            // 报哪一枚看当前亮着哪一枚。⚠️ 不能只判「亮着的那一枚」——那样停着的时候
            // 点它永远返回 None，**就再也播不起来了**（2026-09-17 自检当场抓到）。
            if (_play != null && _play.Contains(world)) return _play.gameObject.activeSelf ? Btn.Play : Btn.Pause;
            if (_step != null && _step.gameObject.activeSelf && _step.Contains(world)) return Btn.Step;
            return Btn.None;
        }

        /// <summary>
        /// 切「在播 / 已停」。
        ///
        /// 🔴 **图标表示「点了会发生什么」，不是「现在是什么状态」** —— 在播时亮 **Pause**、
        /// 停住时亮 **Play**（播放器的通行做法，也是唯一不会「点了没反应」的排法）。
        /// ✅ **2026-09-27 订正**：原来这里写「原版预制体的静态态正好相反 —— 那是**我们挑的**」。
        ///    真因是「**场景序列化 ≠ 运行期**」：`ReplayHud.Initialize()` 跑完就是 **关 `Play`、开 `Pause`**
        ///    ⇒ 我们这条 **与原版一致**，**不是我们挑的**（判据 → `资料/普查产出_0927/回放_界面真值.md` §C）。
        ///    场景里存的那份（`Play=T/Pause=F`）是**进战场前**的样子，照它建就错。
        /// </summary>
        public void SetPlaying(bool playing)
        {
            if (_play != null) _play.gameObject.SetActive(!playing);    // 在播 → 亮「暂停」
            if (_pause != null) _pause.gameObject.SetActive(playing);   // 停住 → 亮「播放」
        }

        // ---- 自检用：四枚各自的**世界坐标**（断言它们等于原版矩形；见 `BattleScene`）----
        public Vector3 ReplayWorldPos { get { return _replay != null ? _replay.transform.position : Vector3.zero; } }
        public Vector3 PlayWorldPos   { get { return _play   != null ? _play.transform.position   : Vector3.zero; } }
        public Vector3 PauseWorldPos  { get { return _pause  != null ? _pause.transform.position  : Vector3.zero; } }
        public Vector3 StepWorldPos   { get { return _step   != null ? _step.transform.position   : Vector3.zero; } }
        /// <summary>自检用：一枚按钮的世界尺寸（宽×高）—— 断言 79.80×48.57 px</summary>
        public Vector2 BtnWorldSize
        {
            get
            {
                if (_play == null) return Vector2.zero;
                return new Vector2(_play.WorldW, _play.WorldH);
            }
        }
    }
}
