// ClickLog.cs — **真实鼠标点击的记录器**（2026-09-24 用户点名要的）
//
// 为什么要有它：自检跑在 `-batchmode`（无头）里 —— **没有帧循环、没有鼠标**，
// 它只能「直调鼠标回调调的那个函数」。所以「**真点一下会怎样**」这件事，
// 自检**永远验不到**：命中矩形对不对、有没有被别的层压住、有没有按钮根本没绑动作、
// 点下去到底该发生什么 —— 这些只有**真 Play + 真鼠标**才现形。
// ⇒ 这个类把每一次真实点击落成一行，格式固定、人（和 AI）都能一眼判读。
//
// 一行 = 一次点击，含三样（用户 2026-09-24 原话：「我点击了什么东西，应该有什么作用」）：
//   ① **点了哪儿**：屏幕像素 + 画布像素（1920×1080 口径）
//   ② **命中了什么**：命中的节点名 + 它的父链；**以及同一点上被压在下面的候选**（按渲染队列/z 排）
//      —— 这一条专治本工程反复踩的「同一个渲染队列谁盖谁不可控」那一族坑
//   ③ **实际触发了什么**：点击那一帧里**所有 `Debug.Log`**（同帧捕获）——
//      我们的每个动作几乎都留了日志（`[Collection] 筛选栏 打开` / `[Deck] 卡背已换成 X` …）
//      ⇒ 有日志 = 这一下干了什么；**命中区在、但一条日志都没有** = 最可疑的那种（会显式标出来）
//
// 落盘位置：`d:/4/_tmp_view/click_log.txt`（app 追加，不覆盖；`OverridePath` 可改）
//   ⚠️ 路径是**从 `Application.dataPath` 推**的（`Assets/../../../_tmp_view`），
//      推不到（例如打成了构建版、盘符变了）就退回 `persistentDataPath`，**并在日志头写明实际路径**。
//
// 怎么用（用户）：打开 `Assets/CardPresentation/Scenes/{Battle,DeckEditor}.unity` 或从 Shell 进菜单 → **按 Play** →
//   随便点。每点一下写一行。⚠️ **必须 Play**：`Update` 只在 Play 里跑，没有帧循环就没有记录。
// 怎么用（我）：`tail -f d:/4/_tmp_view/click_log.txt` 盯着它 —— 用户一停手这边就有新行。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>真实点击的记录器。**只在 Play 里有输出**（`Update` 驱动的调用点都在 Play 里）。</summary>
    public static class ClickLog
    {
        /// <summary>总开关。默认开 —— 这是学习/复刻工程，日志只有几行/次点击。</summary>
        public static bool Enabled = true;
        /// <summary>改落盘位置（自检用；不设就按下面 `DefaultDir` 推）</summary>
        public static string OverridePath;
        /// <summary>自检用：读回上一次写出去的正文（免得自检去碰文件系统）</summary>
        public static string LastBlock { get; private set; }

        static string _path;
        static bool _hooked;
        static int _seq;

        // ---- 同帧日志捕获 ----
        static bool _capturing;
        static readonly List<string> _captured = new List<string>();
        static readonly object _lock = new object();

        static readonly List<string> _hits = new List<string>();   // 「命中」「候选」两类都塞这儿
        static string _scene, _source, _screen, _canvas;
        static float _t0;

        /// <summary>落盘目录：优先 `Assets/../../../_tmp_view`（= `d:/4/_tmp_view`），不行就 `persistentDataPath`。</summary>
        static string DefaultDir
        {
            get
            {
                try
                {
                    var up3 = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
                    var dir = Path.Combine(up3, "_tmp_view");
                    if (Directory.Exists(dir)) return dir;       // ⚠️ 不自己建目录（免得到处留垃圾）
                    if (Directory.Exists(up3) && Directory.Exists(Path.Combine(up3, "MyGame")))
                        return dir;                              // 工程根认得出来就允许建
                }
                catch { /* 推不出来就走下面 */ }
                return Application.persistentDataPath;
            }
        }

        static string Path_
        {
            get
            {
                if (OverridePath != null) return OverridePath;
                if (_path == null) _path = Path.Combine(DefaultDir, "click_log.txt");
                return _path;
            }
        }

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (!_capturing) return;
            lock (_lock)
            {
                if (_captured.Count < 24) _captured.Add("[" + type + "] " + msg);
            }
        }

        /// <summary>
        /// 开始记录一次点击。**每个场景在自己的派发口各调一次**（同一次点击只许调一次）。
        /// </summary>
        /// <param name="scene">场景名（`Battle` / `DeckEditor` / `Shell` …）</param>
        /// <param name="source">哪条派发链（`PointerLayer` / `DeckRuntime.HandlePointer` / `BattleDriver`）</param>
        /// <param name="canvasPx">画布像素（可空；给了就一起记）</param>
        public static void Begin(string scene, string source, Vector2? canvasPx = null)
        {
            if (!Enabled) return;
            Hook();
            EnsureDriver();
            _capturing = true;
            lock (_lock) _captured.Clear();
            _hits.Clear();
            _scene = scene; _source = source; _t0 = Time.realtimeSinceStartup;
            var m = UnityEngine.InputSystem.Mouse.current;
            _screen = m != null ? Fmt(m.position.ReadValue()) : "?（批处理/无鼠标）";
            _canvas = canvasPx.HasValue ? Fmt(canvasPx.Value) : "?（这条派发链没给）";
        }

        /// <summary>点位格式化 —— 不用 `Vector2.ToString()`（它自带括号，会写出一堆 `((1.0, 2.0))`）。</summary>
        static string Fmt(Vector2 v) { return "(" + v.x.ToString("F1") + ", " + v.y.ToString("F1") + ")"; }

        /// <summary>记一条「命中了谁」。可以调多次（第一次 = 真正吃到的那件）。</summary>
        public static void Hit(string what, string detail = null)
        {
            if (!_capturing) return;
            _hits.Add("  " + what + (string.IsNullOrEmpty(detail) ? "" : "   " + detail));
        }

        /// <summary>记「候选」那一组（同一点上的其它件，含被压住的）。</summary>
        public static void Candidates(IEnumerable<string> lines)
        {
            if (!_capturing || lines == null) return;
            _hits.Add("  同时压在这点上的（按渲染队列↓ / z↑ 排，**第一个才是真吃到的**）：");
            foreach (var l in lines) _hits.Add("    " + l);
        }

        /// <summary>结束并落盘。⚠️ **调用方不用手动调它** —— `Begin` 会顺手挂一个帧末驱动
        /// （`LateUpdate` → `Flush`），这样各场景只管在派发口 `Begin` 就行，出口再多也不会漏收尾。</summary>
        public static void Flush()
        {
            if (_capturing) End();
        }

        static void EnsureDriver()
        {
            if (_driver != null) return;
            // ⚠️ **只在 Play 里挂** —— 自检跑在编辑模式，`DontDestroyOnLoad` 在那里会刷警告，
            //    而编辑模式下 `LateUpdate` 本来也不会跑（那个驱动是给真 Play 收尾用的）。
            if (!Application.isPlaying) return;
            var go = new GameObject("~ClickLogDriver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<Driver>();
        }

        static Driver _driver;

        /// <summary>帧末收尾用的小驱动（隐藏、不销毁、不画任何东西）。</summary>
        class Driver : MonoBehaviour
        {
            void LateUpdate() { ClickLog.Flush(); }
        }

        /// <summary>结束并落盘。</summary>
        public static void End()
        {
            if (!_capturing) return;
            _capturing = false;

            var sb = new System.Text.StringBuilder();
            sb.Append("──── 点击 #").Append(++_seq).Append(" ── ")
              .Append(DateTime.Now.ToString("MM-dd HH:mm:ss.fff")).Append('\n');
            sb.Append("  场景 ").Append(_scene).Append(" · 来源 ").Append(_source)
              .Append(" · 屏幕 ").Append(_screen).Append(" · 画布 ").Append(_canvas).Append('\n');
            if (_hits.Count == 0) sb.Append("  ⚠ **这一点上没有命中任何可点的件**（空点）\n");
            else foreach (var h in _hits) sb.Append(h).Append('\n');

            lock (_lock)
            {
                if (_captured.Count == 0)
                    sb.Append("  ⚠ **这一下一条日志都没有** —— 要么它本来就不出声，要么它根本没接上（重点看这种）\n");
                else
                {
                    sb.Append("  这一下实际触发的日志：\n");
                    foreach (var c in _captured) sb.Append("    ").Append(c).Append('\n');
                }
            }
            sb.Append('\n');

            LastBlock = sb.ToString();
            try
            {
                var dir = System.IO.Path.GetDirectoryName(Path_);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(Path_, LastBlock, new System.Text.UTF8Encoding(false));   // 不写 BOM
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ClickLog] 写不出去（" + Path_ + "）：" + e.Message + "\n" + LastBlock);
                Enabled = false;      // 别每点一下就刷一条警告
            }
        }

        /// <summary>自检用：不落盘、只把它会写成什么吐出来（`End` 的干跑）。</summary>
        public static string DryRun() { var s = LastBlock; return s; }
    }
}
