// SaveSandbox.cs — 批处理（`-batchmode` / 自检）下的**存档写口沙箱**（🆕 2026-11-04 · `B140_V4b`）
//
// 为什么有这个文件：
//   · `SaveRoot.Path` 从 2026-11-03（`B128`）起 = 玩家的真存档根 `C:\warpforgesave`。
//   · 但**自检宿主是 `-batchmode -executeMethod`** —— 它们照样会调各存档类的 `Save()` / `SaveNow()`，
//     于是「跑一次自检」= 改写玩家真档（B139-V4b 实测 5 处：卡牌等级 / 排位 / 录像 / 联机设置 / 玩家档案）。
//   · `RuleEngine/Data/DeckStore.cs` 的 `BatchSafePath()` 已经有这道闸（`B139_V4`）；本文件是它的
//     **共享版**，给 `CardPresentation` 这一侧的 5 个写口用（⛔ 别再去 DeckStore 里复制第二份）。
//
// 三档行为（`Dir()`）：
//   ① 非批处理（正式运行）           ⇒ 原样返回 `SaveRoot.Path/<sub>` —— **逐字节不变**；
//   ② 批处理 **且** 没人注入 `OverridePath` ⇒ `Application.temporaryCachePath/wfcheck_save/<进程 tag>/<sub>`；
//   ③ 批处理 **且** 注入了 `OverridePath`    ⇒ `OverridePath/<sub>`（自检自己的临时档，优先级最高）。
//   另有 `DisableForTest` 一档，**只给断言「灭自证」用**：打开它 ⇒ 恒回 `SaveRoot.Path`
//   （断言据此证明「A 组那条判据不是同义反复」）。⛔ 生产路径永远不设它。
//
// 🔴 建目录失败 ⇒ `LogError` **出声**、`Dir()` 回 `null`（调用方据此**跳过本次保存**）
//   —— ⛔ **绝不因此回落到玩家真存档根**（那正是这条闸要防的事）。
//
// 🔶 自己设计（非原版）：**批处理存档沙箱** · 判据 = 原版没有自检宿主（存档在服务端 `PlayFab` / `PlayerDataManager`）
//   ⇒「自检别写玩家真档」这条判据在原版里根本不存在、本地也拿不到，只能我们定。
//   调度台 2026-11-04（`B139_V4` 的形状 + `B140_V4b` 的 5 处落点）。
using System;
using System.IO;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>批处理存档沙箱（见文件头）。**唯一**给 `CardPresentation` 侧存档类拼根目录的口。</summary>
    public static class SaveSandbox
    {
        /// <summary>沙箱目录名（`Application.temporaryCachePath` 下的一层）。与 `DeckStore.BatchSandboxDirName` 同名。</summary>
        public const string DirName = "wfcheck_save";

        /// <summary>批处理判据口。⚠️ **只给自检用** —— 宿主机在交互式编辑器里跑时靠它把闸强制打开/关掉；
        /// 正式代码恒 `null` ⇒ 用 `Application.isBatchMode`。</summary>
        public static Func<bool> BatchModeProbe;

        /// <summary>自检注入的根（`null` / 空 ⇒ 走 ①② 那两档）。⚠️ 用完必须还原成 `null`。</summary>
        public static string OverridePath;

        /// <summary>🔴 **只给断言「灭自证」用**：`true` ⇒ 恒回 `SaveRoot.Path`（模拟「这道闸被人删了」）。
        /// ⛔ 生产路径永远不设它；断言跑完必须还原成 `false`。</summary>
        public static bool DisableForTest;

        /// <summary>一次进程一个沙箱（上一次自检留下的档**不会污染**这一次）。</summary>
        static readonly string _tag = Guid.NewGuid().ToString("N").Substring(0, 8);
        public static string ProcessTag { get { return _tag; } }

        /// <summary>这一趟算不算批处理（`BatchModeProbe` 优先，正式代码恒 `null`）。</summary>
        public static bool BatchNow
        {
            get { return BatchModeProbe != null ? BatchModeProbe() : Application.isBatchMode; }
        }

        static string _root;          // 沙箱根（`null` = 建不出来 / 还没算过）
        static bool _resolved;        // 算过没有（无论成败，只算一次）
        static bool _said;            // 「实际落点 / 建不出目录」只出声一次（⛔ 不刷屏）
        /// <summary>沙箱根建不出来时的原因（成功时 `null`）。</summary>
        public static string LastError { get; private set; }

        /// <summary>存档根（或它下面的一个相对路径）。
        /// <para>三档见文件头；🔴 **返回 `null` = 批处理沙箱建不出来** ⇒ 调用方必须**跳过本次保存**（已 `LogError`）。</para>
        /// <para>⚠️ 非批处理这一档**只拼字符串、不建目录** —— 与原来各处 `Path.Combine(SaveRoot.Path, …)` 逐字节同。</para></summary>
        public static string Dir(string sub = null)
        {
            // ① 灭自证档（只给断言用）
            if (DisableForTest) return Combine(SaveRoot.Path, sub);
            // ② 自检自己注入的临时根（优先级最高，既有自检全靠它）
            if (!string.IsNullOrEmpty(OverridePath)) return Combine(OverridePath, sub);
            // ③ 非批处理：与加这道闸之前**完全一样**
            if (!BatchNow) return Combine(SaveRoot.Path, sub);
            // ④ 批处理：沙箱
            string root = SandboxRoot();
            return root == null ? null : Combine(root, sub);
        }

        static string Combine(string root, string sub)
        {
            if (string.IsNullOrEmpty(sub)) return root;
            return System.IO.Path.Combine(root, sub);
        }

        /// <summary>沙箱根：`temporaryCachePath/wfcheck_save/<进程 tag>`（建一次、缓存）。</summary>
        static string SandboxRoot()
        {
            if (_resolved) return _root;
            _resolved = true;
            var dir = System.IO.Path.Combine(Application.temporaryCachePath, DirName, _tag);
            try
            {
                Directory.CreateDirectory(dir);
                _root = dir;
                if (!_said)
                {
                    _said = true;
                    Debug.Log("[SaveSandbox] 批处理（自检）⇒ 存档写口落点 = 沙箱「" + dir
                            + "」；玩家真存档根**一个字节都不碰**（要落真根：显式设各存档类自己的 Override，或起一个非批处理进程）");
                }
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                if (!_said)
                {
                    _said = true;
                    Debug.LogError("[SaveSandbox] 批处理沙箱目录建不出来（" + dir + "）：" + LastError
                                 + " ⇒ 本进程这些存档**写会失败**（⛔ 但绝不回落到玩家真存档根）");
                }
            }
            return _root;
        }
    }
}
