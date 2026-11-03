// SaveRoot.cs — **存档根目录的唯一解析口**（🆕 2026-11-03 · `B128`「存档独立化」）
//
// 为什么有这个文件：
//   · 原来 14 处存档各自算 `Application.persistentDataPath/<子目录>`（= `%USERPROFILE%\AppData\LocalLow\<公司>\<产品>\`），
//     **离开这台开发机就找不到自己的档**（换电脑 / 打包给别人跑 = 从零开始，而且没人说得清档在哪）。
//   · 用户 2026-11-03 定：存档根 = **`C:\warpforgesave`**（全小写）⇒ 游戏搬到别的电脑也能直接玩。
//   · 这是**独立的新游戏、与原版彻底分开** ⇒ 成就 / 等级 / 卡牌进度**全部从零开始**，
//     ⛔ **不迁移** `%LocalLow%` 那份旧数据（那份**永不删除**，本文件只做「以后写哪儿」）。
//
// 解析顺序（`Path`）：
//   ① `Override`（**只给自检** —— 注入了就用它，不碰玩家真档）；
//   ② `C:\warpforgesave`（能建目录、且能写一个探针文件再删掉）；
//   ③ 回落 `%USERPROFILE%\warpforgesave` —— 🔴 **回落时 `Debug.LogError` 大声报「C 盘不可写，已回落到 X」**（⛔ 不许静默失败）。
//
// 用法：
//   · 各存档类**只**用 `SaveRoot.Path` 拼根目录（⛔ 别再直接写 `Application.persistentDataPath`）；
//   · 启动时（`ShellRuntime.Awake`）调一次 `SaveRoot.Ensure()` —— 建目录 + 探针验可写 + 把**实际解析到的路径**打进日志（每进程只打一次）。
//
// 🔶 自己设计（非原版）：**存档根本身**。原版存档在服务端（`PlayerDataManager` / PlayFab），
//    服务器已关服 ⇒ 「档放哪」这条判据**在原版里根本不存在、本地永远拿不到**，必须我们定。
//    用户 2026-11-03 裁定：`C:\warpforgesave`（全小写）。
using System;
using System.IO;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>存档根目录的**唯一**解析口。见文件头。</summary>
    public static class SaveRoot
    {
        /// <summary>首选存档根（用户 2026-11-03 定：**全小写**）。</summary>
        public const string PrimaryRoot = @"C:\warpforgesave";

        /// <summary>回落目录名（拼在 `%USERPROFILE%` 下）。</summary>
        public const string FallbackDirName = "warpforgesave";

        /// <summary>可写探针文件名（建完立刻删，不留在盘上）。</summary>
        const string ProbeName = ".wf_saveroot_probe";

        /// <summary>自检注入口：`null` / 空 = 走真实解析（①→②→③）。⚠️ 自检用完必须还原成 `null`。</summary>
        public static string Override;

        static string _resolved;          // 解析结果（`null` = 还没解析过）
        static bool _logged;              // 「实际路径」只打一次
        static bool _fallbackSaid;        // 「C 盘不可写」只报一次（⛔ 不刷屏）
        static bool _allFailedSaid;       // 「首选与回落都写不进去」只报一次

        /// <summary>最近一次探针失败的原因（成功时 `null`）。日志之外还能被自检读。</summary>
        public static string LastError { get; private set; }

        /// <summary>存档根目录。解析顺序 = ① `Override` ② `C:\warpforgesave` ③ `%USERPROFILE%\warpforgesave`。
        /// <para>⚠️ 全都写不进去时**仍返回首选路径**（`C:\warpforgesave`）—— 让各存档类自己的写盘失败链去大声报错，
        /// ⛔ 不在这里返回 null 把 NRE 甩给 14 个消费方。</para></summary>
        public static string Path
        {
            get
            {
                if (!string.IsNullOrEmpty(Override)) return Override;
                if (_resolved == null) Resolve();
                return _resolved ?? PrimaryRoot;
            }
        }

        /// <summary>🔴 **启动时调一次**：解析根目录 → 建目录（不存在就建）→ 写探针再删掉验可写 → 把**实际路径**打进日志。
        /// <para>幂等：重复调用只会再验一次可写（目录已在就只写一个探针），日志每进程只打一次。</para>
        /// <para>返回 `false` = 首选与回落**都不可写**（已 `LogError`）。</para></summary>
        public static bool Ensure()
        {
            string root;
            if (!string.IsNullOrEmpty(Override)) root = Override;
            else { if (_resolved == null) Resolve(); root = _resolved; }

            if (string.IsNullOrEmpty(root))
            {
                // 走到这里 = Resolve 的最后一档也失败了（Resolve 内部已报过错）。
                return false;
            }

            if (!TryPrepare(root))
            {
                Debug.LogError("[SaveRoot] 存档根「" + root + "」**不可写**（" + (LastError ?? "未知原因")
                             + "）⇒ 本进程的存档会写失败（各存档类会各自报错）。");
                return false;
            }

            LogOnce(root);
            return true;
        }

        // ──────────────────────────────────────────────────────────────

        /// <summary>真实解析（不碰 `Override`）：② `C:\warpforgesave` → ③ `%USERPROFILE%\warpforgesave`。</summary>
        static void Resolve()
        {
            if (TryPrepare(PrimaryRoot))
            {
                _resolved = PrimaryRoot;
                return;
            }

            string user = null;
            try { user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
            catch (Exception e) { LastError = e.GetType().Name + ": " + e.Message; }
            string fallback = string.IsNullOrEmpty(user) ? null : System.IO.Path.Combine(user, FallbackDirName);

            if (!_fallbackSaid)
            {
                _fallbackSaid = true;
                Debug.LogError("[SaveRoot] **C 盘不可写**：" + PrimaryRoot + " 建不出 / 写不进（" + (LastError ?? "未知原因")
                             + "）⇒ 已回落到 " + (fallback ?? "<拿不到 %USERPROFILE%>"));
            }

            if (fallback != null && TryPrepare(fallback))
            {
                _resolved = fallback;
                return;
            }

            if (!_allFailedSaid)
            {
                _allFailedSaid = true;
                Debug.LogError("[SaveRoot] 首选「" + PrimaryRoot + "」与回落「" + (fallback ?? "<null>")
                             + "」**都写不进去** ⇒ 存档无法落盘（后续每条写盘都会各自报错）。");
            }
        }

        /// <summary>建目录 + 写一个探针文件再删掉（用来验「真的能写」，光 `Directory.Exists` 不算）。</summary>
        static bool TryPrepare(string dir)
        {
            if (string.IsNullOrEmpty(dir)) { LastError = "路径为空"; return false; }
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string probe = System.IO.Path.Combine(dir, ProbeName);
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        /// <summary>「存档根 = X」每进程只打一次（`Ensure()` 每次启动进游戏都要跑到，⛔ 不刷屏）。</summary>
        static void LogOnce(string dir)
        {
            if (_logged) return;
            _logged = true;
            Debug.Log("[SaveRoot] 存档根 = " + dir
                    + "（" + (string.IsNullOrEmpty(Override) ? "解析结果" : "⚠️ 自检注入 Override") + "）");
        }

        // ── 类尾追加区（新成员一律加在这里，⛔ 别插进上面既有成员之间 —— 账按 `文件:行号` 引用）──
    }
}
