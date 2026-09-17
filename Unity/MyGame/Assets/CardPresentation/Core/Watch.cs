// Watch.cs — player 跑批用的**看门狗 + 面包屑**（编辑器里一律不启动）
//
// 🔴 **为什么不能直接用 `Debug.Log` 做这件事**：卡死的现场可能是 **Unity 的日志系统本身停住**
//    （日志由另一个线程写盘；主线程在 `Debug.Log` 上被缓冲/锁挡住，或日志线程被 I/O 挡住）。
//    那时日志文件上「根本不出帧」和「日志被缓冲吃了尾巴」**长得一模一样** ——
//    本工程已经为这个歧义白查过一轮（`资料/特效还原_进度与交接.md` §七 那条）。
//    ⇒ 看门狗**从后台线程直接 `File.AppendAllText` 写自己的文件**，绕开 Unity 的日志。
//
// 它回答三个问题（一次跑批就能全答）：
//   ① **进程还活着吗** —— 看门狗文件还在长 = 活着（哪怕 Unity 日志已经死了）
//   ② **主线程还在出帧吗** —— `frame=` 那一列在不在涨
//   ③ **卡在哪一步** —— `phase=` 那一列是**主线程最后写下的面包屑**
//
// 判据（与 CPU 采样合起来看）：
//   frame 不涨 + cpu 涨   ⇒ 主线程**死循环**（在哪一步看 phase/MARK）
//   frame 不涨 + cpu 不涨 ⇒ 主线程**阻塞/等待**
//   frame 在涨、Unity 日志不涨 ⇒ **是日志死了，不是游戏死了**（换尺子，别追代码）
//
// 用法：命令行给 `-wfwatch`（PlayBoot 会拉起它）。不给就完全不启动，零开销。
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

public static class Watch
{
    static string _path;
    static volatile bool _on;

    // —— 主线程每帧写、后台线程读（都是 volatile，读到旧值没关系，看的是趋势）——
    static volatile int _frame;
    static volatile float _t;          // Time.realtimeSinceStartup（主线程写，后台线程不能碰 Unity API）
    static volatile string _phase = "";
    static int _phaseAtFrame = -1;

    static readonly ConcurrentQueue<string> _marks = new ConcurrentQueue<string>();
    static System.Diagnostics.Process _proc;   // 缓存：每 tick 新建一个 Process 太浪费

    /// <summary>主线程「多少秒没出帧」就判长停顿。**0 = 不判**（默认，诊断模式要留着现场）。
    /// ⚠️ 为什么默认关：诊断时我们**要**那个卡住的进程活着（好采样/好留证）；
    ///    而跑长批时我们要它**自己退**（否则一次卡住白等 20 分钟，本工程吃过）。
    /// 实测参考值：单实例正常跑，最长的一次停顿是**场景加载那 9~10 s**；
    /// 并发跑 4 个实例时会涨到 118~125 s（那是并发特有的，别当成产品行为）。</summary>
    static int _stallQuitSec;

    /// <summary>启动看门狗。**在编辑器里直接返回**（纪律：绝不影响四条自检）。</summary>
    public static void Start(string path, int stallQuitSec = 0)
    {
        if (Application.isEditor || _on) return;
        _on = true;
        _path = path;
        _stallQuitSec = stallQuitSec;
        Line($"=== Watch 启动 {DateTime.Now:HH:mm:ss.fff}  pid={System.Diagnostics.Process.GetCurrentProcess().Id}" +
             $"  stallQuit={stallQuitSec}s ===");
        _proc = System.Diagnostics.Process.GetCurrentProcess();

        var th = new Thread(Loop) { IsBackground = true, Name = "~Watch" };
        th.Start();
    }

    /// <summary>主线程每帧调一次（放在已有 MonoBehaviour 的 `Update` 里）。</summary>
    public static void Tick()
    {
        if (!_on) return;
        _frame++;
        _t = Time.realtimeSinceStartup;
    }

    /// <summary>面包屑：主线程进到某一步就写一个。**不改变逻辑**，只在 `-wfwatch` 下记事。</summary>
    public static void Mark(string phase)
    {
        if (!_on) return;
        _phase = phase;
        _phaseAtFrame = _frame;
        _marks.Enqueue($"MARK f={_frame} t={Time.realtimeSinceStartup:F2} {phase}");
    }

    /// <summary>焦点/暂停事件（`PlayerBoot.OnApplicationFocus/Pause` 调）—— 用来**证伪**
    /// 「失去焦点被暂停」那一族猜测（本机实测 runInBackground 是开的，但仍留证据）。</summary>
    public static void Event(string what)
    {
        if (!_on) return;
        _marks.Enqueue($"EVENT f={_frame} t={Time.realtimeSinceStartup:F2} {what}");
    }

    static void Loop()
    {
        int lastFrame = -1;
        float lastT = -1f;
        while (true)
        {
            Thread.Sleep(1000);
            try
            {
                string s;
                while (_marks.TryDequeue(out s)) Line("  " + s);

                int f = _frame;
                float t = _t;
                double cpu = _proc.TotalProcessorTime.TotalSeconds;
                bool moving = f != lastFrame || t != lastT;
                // `stuck=` 是从**上一条状态行**到现在「没动过」的累计秒数 —— 它就是「卡了多久」
                string st = moving ? "" : $"  STUCK={_stuck}s";
                if (!moving) _stuck++;
                else _stuck = 0;
                Line($"[{DateTime.Now:HH:mm:ss.fff}] frame={f} t={t:F2} cpu={cpu:F1} " +
                     $"moving={(moving ? 1 : 0)} phase=[{_phase}]{st}");
                lastFrame = f; lastT = t;

                // 长停顿保险丝（默认关）—— 把「静默挂住」变成「有退出码、有阶段」。
                if (_stallQuitSec > 0 && _stuck >= _stallQuitSec)
                {
                    Line($"!!! 主线程 {_stuck}s 没出帧（阈值 {_stallQuitSec}s）· 最后 phase=[{_phase}] · " +
                         $"frame={f} t={t:F2} · **看门狗主动结束进程**（别让它静默挂住）");
                    try { _proc.Kill(); } catch { }
                    Thread.Sleep(5000);     // 等 Kill 生效；没生效也不许把看门狗自己结束掉
                }
            }
            catch (Exception e) { try { Line("  !! Watch 自身出错：" + e.Message); } catch { } }
        }
    }

    static int _stuck;

    static void Line(string s)
    {
        try { File.AppendAllText(_path, s + "\r\n"); } catch { /* 看门狗自己不许把游戏搞崩 */ }
    }
}
