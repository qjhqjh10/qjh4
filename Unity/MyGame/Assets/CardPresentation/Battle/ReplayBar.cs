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
using UnityEngine;

namespace CardPresentation
{
    public class ReplayBar : MonoBehaviour
    {
        /// <summary>点到了哪一枚</summary>
        public enum Btn { None, Replay, Play, Pause, Step }

        // ---- 原版实测值（1920×1080，**y 从上往下**）----
        const float RefW = 1920f, RefH = 1080f;
        const float BarX = 410.2f, BarY = 37.3f;
        const float BtnW = 79.80f, BtnH = 48.57f, BtnTop = 4.4f;
        const float BtnAspect = BtnW / BtnH;                 // 1.6430（原版是拉伸，不是按贴图比例）
        static readonly float[] SlotDx = { 9.42f, 88.47f, 167.67f };   // 第 2 槽是 Play/Pause 共用

        /// <summary>`Holder`（原版字段 `objHolder`，偏移 0x48）的中心：绝对 rect
        /// `416.3,36.3 → 674.8,95.7` ⇒ **(545.55, 66.00)**。⚠️ **比外面那条容器还高 1 px**（原版如此，别「修」）。</summary>
        const float HolderCx = 545.55f, HolderCy = 66.00f;

        /// <summary>🔴 **两个容器的矩形尺寸（px）** —— 2026-10-11（A218）为写 `sizeDelta` 立的常量，
        /// 值逐条来自原版实读（`bundle_scenes_scenes_battlearena1` 与运行时 dump 两处一致）：
        /// · 根 `ReplayButtons`：绝对 rect **x[410.2, 703.8] y[37.3, 94.7]** ⇒ **293.60 × 57.41**
        ///   （`m_SizeDelta = (293.6, 57.406)` · `anchor (0.5,1) 重合` · `ap (−403, −66)`）；
        /// · `Holder`：绝对 rect **x[416.3, 674.8] y[36.3, 95.7]** ⇒ **258.54 × 59.43**
        ///   （`m_SizeDelta = (258.5, 59.4)`，和上面那句「比外面那条还高 1 px」是同一件事）。
        /// ⛔ 别拿 `BtnW` 去凑：容器宽 293.60 ≠ 4 个按钮的宽（原版它俩本来就不同源）。</summary>
        const float BarW = 293.60f, BarH = 57.41f, HolderW = 258.54f, HolderH = 59.43f;

        Transform _holder;

        /// <summary>HUD 图统一的 z（`BattleDriver.HudImageZ`）—— 排在 HUD 那一层</summary>
        const float Z = 0.3f;

        ImageQuad _replay, _play, _pause, _step;

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
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`
            //    （= 原版容器 `ReplayButtons` 的矩形 **293.60 × 57.41**，见 `BarW/BarH` 的注释）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「`ReplayBar` 根 = 293.6×57.41」红。
            var go = new GameObject("ReplayBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, BarW, BarH);
            var b = go.AddComponent<ReplayBar>();
            b.Build();
            return b;
        }

        void Build()
        {
            // 🔴 **`Holder` 这一层是照原版加的**（原版 `objHolder`，字段偏移 0x48）——
            //    `ReplayHud.Setup()` 的显隐开关**做在这一层**，不是根节点。
            //    ⚠️ 原版这个容器 `x[416.3,674.8] y[36.3,95.7]`，**比外面那条还高 1 px**（原版如此）。
            var holderGo = new GameObject("Holder", typeof(RectTransform));
            holderGo.transform.SetParent(transform, false);
            MenuDraw.SetPxSize(holderGo.transform, HolderW, HolderH);
            holderGo.transform.localPosition = Spot(HolderCx, HolderCy);
            _holder = holderGo.transform;

            _replay = Make("Replay",   SlotDx[0], "40K_replay_bt_restart");
            _play   = Make("Play",     SlotDx[1], "40K_replay_bt_play");
            _pause  = Make("Pause",    SlotDx[1], "40K_replay_bt_pause");
            _step   = Make("StepPlay", SlotDx[2], "40K_replay_bt_next");
            SetPlaying(true);      // 开局就在播 ⇒ 亮的是「暂停」（**与原版 `Initialize()` 一致**，见文件头 ③）
        }

        ImageQuad Make(string goName, float relX, string art)
        {
            var tex = CardArt.Ui(art);
            if (tex == null)
            {
                // 图缺了要**说出来**（红线）—— 但别让整个 HUD 建不起来
                Debug.LogWarning($"[ReplayBar] 取不到图 `{art}`（回放条这一枚会是空的）");
                return null;
            }
            // 原版容器左上角 + 容器内相对位；按钮**贴容器顶**（相对顶 4.4 px）而不是垂直居中
            float xPx = BarX + relX + BtnW * 0.5f;
            float yPx = BarY + BtnTop + BtnH * 0.5f;
            var q = ImageQuad.Create(_holder, tex,
                                     Spot(xPx, yPx) - (_holder != null ? _holder.position : Vector3.zero),
                                     BtnH / 108f, new Vector2(0.5f, 0.5f), "replay_" + goName);
            if (q != null) q.SetAspect(BtnAspect);       // 原版 `m_PreserveAspect = 0` ⇒ 拉成原版矩形
            return q;
        }

        static Vector3 Spot(float xPx, float yPx)
        {
            // 原版 y 从上往下；`LayoutSpace.ToWorld` 是 (0,0)=左下 ⇒ 翻一下
            return LayoutSpace.ToWorld(xPx / RefW, 1f - yPx / RefH);
        }

        /// <summary>分辨率变了要重算（和 HUD 其它件一样，`BattleDriver.ReanchorHud` 里调）。</summary>
        public void RefreshLayout()
        {
            Place(_replay, SlotDx[0]);
            Place(_play, SlotDx[1]);
            Place(_pause, SlotDx[1]);
            Place(_step, SlotDx[2]);
        }

        void Place(ImageQuad q, float relX)
        {
            if (q == null) return;
            var p = Spot(BarX + relX + BtnW * 0.5f, BarY + BtnTop + BtnH * 0.5f);
            q.transform.localPosition = new Vector3(p.x, p.y, Z) - (_holder != null ? _holder.position : Vector3.zero);
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
