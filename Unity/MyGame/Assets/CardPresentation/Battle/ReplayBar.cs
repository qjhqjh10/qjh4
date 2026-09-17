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
// 🔴 **两处是「我们挑的」，别当成复刻**（原版查不到，见 `资料/战斗UI_原版对账表.md` §三·〇 :129）：
//   ① **什么时候显示** —— 原版哪个模式才出现（观战？回放？）**查不到**；
//   ② **这四个按钮接什么** —— 原版语义查不到。我们接的是**本局的时间控制**：
//        Replay = 重开一局（`BattleDriver.Restart`，和结算面板那句「按 R 再来一局」同一个入口）
//        Play / Pause = 暂停 / 继续（冻结事件时间线、回合计时与 AI 步进）
//        StepPlay = **单步**推进一条排在最前的事件
//      ——之所以不摆 4 个点了没反应的图：本工程的红线是「不许静默失败」，
//      摆一排假按钮比不摆更糟。
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
            var go = new GameObject("ReplayBar");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<ReplayBar>();
            b.Build();
            return b;
        }

        void Build()
        {
            _replay = Make("Replay",   SlotDx[0], "40K_replay_bt_restart");
            _play   = Make("Play",     SlotDx[1], "40K_replay_bt_play");
            _pause  = Make("Pause",    SlotDx[1], "40K_replay_bt_pause");
            _step   = Make("StepPlay", SlotDx[2], "40K_replay_bt_next");
            SetPlaying(true);      // 开局就在播 ⇒ 亮的是「暂停」（见 `SetPlaying` 的注释）
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
            var q = ImageQuad.Create(transform, tex, Spot(xPx, yPx), BtnH / 108f,
                                     new Vector2(0.5f, 0.5f), "replay_" + goName);
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
            q.transform.localPosition = new Vector3(p.x, p.y, Z);
        }

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
        /// ⚠️ **原版预制体的静态态正好相反**（`Play` 可见 / `Pause` 隐藏）—— 那是**进战场前、
        /// 回放还没开始**的样子。我们这一条是**我们挑的**（原版那组件的语义查不到，见文件头）。
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
