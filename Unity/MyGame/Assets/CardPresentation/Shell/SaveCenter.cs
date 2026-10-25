// SaveCenter.cs — 中央存档（完全复刻 · Step 1「建中央层」）
//
// ⛔ **本文件是纯新增，不接入任何现有子系统**（那是 Step 4，要双写期 + 全字段 dump 相等）。
//    它现在只是「一个可被调用的统一读写口」。现有 10 份旧档一个字节都不会因为它而改变。
//
// 判据与出处
//   · 原版是**两层**：① 云端中央存档 = PlayFab user data，按 `PlayerDataKey` 分 **51** 个 key
//     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayerDataKey.cs:1-55`）；② 本地设置档 = 一个
//     `PlayerDecks{dataVersion}.dat`（**只存设置+凭据、不存进度**）。
//   · 原版读：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/OnlineData.cs:145-193`
//     —— 缺 key ⇒ `Log("No player data found for key X")` + 返回 null；**坏 JSON 没有 try/catch**（异常冒泡）。
//     ⇒ 我们**不照抄这个缺陷**：坏 JSON 走「隔离成 `.bak` + 出声」（方案 §Step3 已写；原版无此兜底）。
//   · 原版写：`PlayerDataManager.cs:11893 UpdateOnlineUserData` —— 未登录 ⇒ `LogWarning` + **静默丢弃**。
//     ⇒ 我们**不照抄**：本地没有云端副本，写失败必须出声（红线：不许静默失败）。
//   · 落盘先例（照抄形状）：`Shell/DailyData.cs:1934-1936`（先写 `.tmp` 再换名）。
//   · 施工方案：`资料/完全复刻/判据/存档体系_原版语义与施工方案.md` §七 Step 1 / Step 3。
//
// 落盘形态（**Step 0 定的 `_v` 约定**）
//   根目录：`Application.persistentDataPath/WarpforgeSave/`
//   每个 key 一个文件：`<key>.json`
//   内容：**顶层第一个键**是 `"_v"`（int，从 1 开始），其余字段 = 该 key 的 payload，**同一层**（不套壳）：
//        {"_v":1,"rows":[...],"skullsCount":0}     ← DailyCounters 这种
//        {"_v":1}                                  ← 空 payload
//   🔴 **未来版本拒绝覆盖**：盘上 `_v` > 本代码 `CurrentVersion(key)` ⇒ 读返回 null、写/删一律拒绝，
//      并且**出声**。理由：原版 `ClearLocalPlayerData()` 敢 `File.Delete`（`:10157-10172`）是因为
//      云端有权威副本；**我们没有** ⇒ 降级运行绝不能毁掉更新版本写下的档。
//
// 写盘策略（G3 / G4）
//   · **一律 tmp + rename 原子写**：写 `<key>.json.tmp` → `FileStream.Flush()` → `File.Replace`（目标存在时，
//     NTFS 上原子）→ 失败退回「删旧 + 换名」（不是原子，会出声说明走了兜底）→ 两者都失败 ⇒ `LogError` + 返回 false。
//   · ⛔ 不删档：`Delete()` 也是**改名到 `.del-<时间戳>`**（可回滚）；坏档隔离到 `.bak`（同样不销毁）。
//   · 落盘门（照 `DailyData.StoreOn` 的既有纪律）：① `OverrideRoot` 设了 ⇒ 允许（自检临时档）；
//     ② 否则只有真在一局里（`isPlaying && !isBatchMode`）才允许 ⇒ **批处理自检一个字节都不碰玩家真档**。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>中央存档的统一读写口（原版 `PlayerDataManager` 的本地等价物）。
    /// <para>接口：<see cref="Get{T}"/> / <see cref="GetRaw"/> / <see cref="Set{T}"/> / <see cref="SetRaw"/> /
    /// <see cref="Exists"/> / <see cref="Delete"/> / <see cref="Flush"/> / <see cref="RegisterMigration"/>。
    /// ⛔ Step 1 阶段**没有任何子系统调用它**（Step 4 才切）。</para></summary>
    public static class SaveCenter
    {
        /// <summary>`persistentDataPath` 下的根目录名（与 `WarpforgeDaily` / `WarpforgeDraft` 并列）。</summary>
        public const string RootDirName = "WarpforgeSave";

        /// <summary>payload 顶层的版本键（**必须是第一个键** —— 读的时候按这个位置剥信封）。</summary>
        public const string VersionField = "_v";

        /// <summary>每个 key 的起始 schema 版本（Step 0：从 1 开始）。</summary>
        public const int DefaultVersion = 1;

        /// <summary>自检用的**根目录改写口**（`null` = 用玩家真目录）。⚠️ 自检一律要设它，
        /// 与 `DailyData.OverrideDir` / `ReplayStore.OverrideDir` 同一套路。</summary>
        public static string OverrideRoot;

        static readonly object _gate = new object();
        static readonly HashSet<string> _said = new HashSet<string>();       // 「同一句话只说一次」的记账
        static readonly HashSet<string> _future = new HashSet<string>();     // 已确认是「未来版本」的 key
        static readonly Dictionary<string, int> _current = new Dictionary<string, int>();  // key → 本代码的 schema 版本
        static readonly Dictionary<string, Dictionary<int, Func<string, string>>> _migrations =
            new Dictionary<string, Dictionary<int, Func<string, string>>>();  // key → (fromVersion → 迁移函数)

        /// <summary>最近一次写盘成功没有（`Flush()` 读它；**初值 true** = 本进程还没写过，没有失败可言）。
        /// ⚠️ 写盘是**立即**的 —— `Flush()` 不负责「把缓冲写下去」，它只报「上一次写是不是真的成了」。</summary>
        public static bool LastWriteOk { get; private set; } = true;

        /// <summary>最近一次写盘失败的原文（成功时 `null`）。日志之外还能被自检读。</summary>
        public static string LastWriteError { get; private set; }

        /// <summary>存档根目录（`persistentDataPath/WarpforgeSave`，或自检注入的目录）。</summary>
        public static string RootDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideRoot)
                     ? Path.Combine(Application.persistentDataPath, RootDirName)
                     : OverrideRoot;
            }
        }

        /// <summary>这一趟允许**写**盘吗（读不受限）。判据与 `DailyData.StoreOn` 逐条同义：
        /// ① 设了 `OverrideRoot` ⇒ 允许；② 否则只有真在一局里才允许 ⇒ 批处理自检绝不碰玩家真档。</summary>
        public static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideRoot) || (Application.isPlaying && !Application.isBatchMode); }
        }

        /// <summary>某个 key 的存档文件完整路径（日志 / 自检用）。</summary>
        public static string PathOf(string key)
        {
            return Path.Combine(RootDir, key + ".json");
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  版本 / 迁移
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>本代码对某个 key 认识的 schema 版本（默认 <see cref="DefaultVersion"/> = 1）。</summary>
        public static int CurrentVersion(string key)
        {
            int v;
            return _current.TryGetValue(key, out v) ? v : DefaultVersion;
        }

        /// <summary>自检专用：把某个 key 的「本代码版本」调高（用来验「旧档会被迁移 / 未来档会被拒绝」）。
        /// ⛔ 正式代码别调它。</summary>
        public static void SetCurrentVersionForTest(string key, int version)
        {
            _current[key] = version < 1 ? 1 : version;
        }

        /// <summary>注册一条**逐 key 的补丁**：盘上 `_v == fromVersion` 时，用 `migrate` 把 payload 升到下一版。
        /// <para>形态照原版 `CheckForTutoriaDoneAndDataFix()`（`PlayerDataManager.cs:11229`）：
        /// **「探测旧字段 → 补新字段 → 不认识的忽略」**，⛔ 不要造「版本 → 大迁移脚本」那一套
        /// （原版全库 `Migrat*|Upgrade*|Convert*` 在 `PlayerDataManager` 内 0 命中）。</para>
        /// <para>⚠️ `migrate` 的入参与出参都是**去掉 `_v` 的 payload JSON**；抛异常 ⇒ 本次读返回 null 并出声
        /// （不落盘、不销毁盘上那份）。</para></summary>
        public static bool RegisterMigration(string key, int fromVersion, Func<string, string> migrate)
        {
            string why;
            if (!ValidateKey(key, "RegisterMigration", out why)) return false;
            if (migrate == null) { LogError("[SaveCenter] RegisterMigration('" + key + "')：迁移函数是 null ⇒ 拒绝"); return false; }
            if (fromVersion < 1) { LogError("[SaveCenter] RegisterMigration('" + key + "')：fromVersion 必须 ≥ 1（给的是 " + fromVersion + "）"); return false; }
            Dictionary<int, Func<string, string>> table;
            if (!_migrations.TryGetValue(key, out table))
            {
                table = new Dictionary<int, Func<string, string>>();
                _migrations[key] = table;
            }
            if (table.ContainsKey(fromVersion))
            {
                LogError("[SaveCenter] RegisterMigration('" + key + "', " + fromVersion + ")：**已经注册过**同一起点 ⇒ 拒绝覆盖（幂等/可复现）");
                return false;
            }
            table[fromVersion] = migrate;
            Debug.Log("[SaveCenter] 已注册迁移：'" + key + "' v" + fromVersion + " → v" + (fromVersion + 1));
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  读
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>读一个 key 并反序列化成 <typeparamref name="T"/>。
        /// <para>返回 `null` 的四种情况（**每一种都出声**，⛔ 不静默当默认值）：缺 key / 读盘失败 / 坏 JSON（已隔离）/
        /// 未来版本（已拒绝）。</para>
        /// <para>⚠️ `JsonUtility` 的固有陷阱：**payload 能解析成 JSON、但字段对不上 T** 时会静默给默认值 ——
        /// 这是 G2（没有版本/迁移）的残留，靠 `_v` + `RegisterMigration` 把关，本层查不出来。</para></summary>
        public static T Get<T>(string key) where T : class
        {
            string raw = GetRaw(key);
            if (raw == null) return null;
            try
            {
                T v = JsonUtility.FromJson<T>(raw);
                if (v == null)
                {
                    LogError("[SaveCenter] key '" + key + "' 的 payload 解不成 " + typeof(T).Name
                           + "（内容非空但结果是 null）⇒ 返回 null。文件**没动**。");
                }
                return v;
            }
            catch (Exception e)
            {
                LogError("[SaveCenter] key '" + key + "' 的 payload 反序列化失败：" + e.Message
                       + " ⇒ 返回 null。文件**没动**（要修档请手工改 `" + PathOf(key) + "`）。");
                return null;
            }
        }

        /// <summary>读一个 key 的**payload JSON（已剥掉 `_v` 信封）**。缺 key / 坏档 / 未来版本 ⇒ `null` + 出声。
        /// <para>用途：① 迁移的输入 ② 自检做「导入前后逐字段相等」的对比（不用反序列化就能逐字比）。</para></summary>
        public static string GetRaw(string key)
        {
            string why;
            if (!ValidateKey(key, "Get", out why)) return null;

            string path = PathOf(key);
            if (!File.Exists(path))
            {
                SpeakOnce("missing|" + key, false,
                    "[SaveCenter] 缺 key '" + key + "'（找过 `" + path + "`）⇒ 返回 null。"
                  + "⛔ 不静默当默认值：调用方要自己区分「第一次跑」与「档丢了」。"
                  + "（原版同款出声：`OnlineData.cs:172` 附近的 `No player data found for key X`）");
                return null;
            }

            string text;
            try { text = File.ReadAllText(path, Encoding.UTF8); }
            catch (Exception e)
            {
                LogError("[SaveCenter] 读 key '" + key + "' 失败：" + e.Message + "（`" + path + "`）⇒ 返回 null");
                return null;
            }

            string problem;
            int version = ProbeVersion(text, out problem);
            if (problem != null)
            {
                Quarantine(key, problem);
                return null;
            }

            string payload;
            if (version <= 0)
            {
                // 没有信封 ⇒ 整份内容就是 payload（手写档 / 从旧客户端手工搬过来的）。⛔ 别拿去剥信封，那会把它当坏档。
                SpeakOnce("noversion|" + key, false,
                    "[SaveCenter] key '" + key + "' 的档**没有 `_v`**（`" + path + "`）⇒ 按 v" + DefaultVersion
                  + " 处理（整份内容当 payload）。要么是手工放进来的，要么是坏档 —— 请确认。");
                version = DefaultVersion;
                payload = text.Trim();
            }
            else
            {
                if (version > CurrentVersion(key))
                {
                    LockFuture(key, version);
                    return null;
                }
                payload = StripEnvelope(text, out problem);
                if (payload == null)
                {
                    Quarantine(key, "信封结构不认识：" + problem);
                    return null;
                }
            }

            payload = ApplyMigrations(key, payload, version);
            return payload;
        }

        /// <summary>这个 key 盘上有没有档（**纯事实**：未来版本导致的「拒绝读」也算存在）。</summary>
        public static bool Exists(string key)
        {
            string why;
            if (!ValidateKey(key, "Exists", out why)) return false;
            return File.Exists(PathOf(key));
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  写
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>把 <paramref name="value"/> 序列化成 payload 并原子落盘。返回是否**真的写成功**。
        /// <para>⛔ `value == null` 会被拒绝（要删档走 <see cref="Delete"/>，免得「写 null」= 静默清空）。</para></summary>
        public static bool Set<T>(string key, T value) where T : class
        {
            string why;
            if (!ValidateKey(key, "Set", out why)) return false;
            if (value == null)
            {
                LogError("[SaveCenter] Set('" + key + "') 的值是 **null** ⇒ 拒绝（不想覆盖就把调用删掉；要删档走 Delete()）");
                return false;
            }
            string payload;
            try { payload = JsonUtility.ToJson(value); }
            catch (Exception e)
            {
                LogError("[SaveCenter] Set('" + key + "') 序列化失败：" + e.Message + " ⇒ 没写盘");
                return false;
            }
            return SetRaw(key, payload);
        }

        /// <summary>写一份**已经拼好的 payload JSON**（Step 2 的旧档导入走这里；也用于「按原样转存」）。
        /// <para>要求：payload 必须是 **JSON 对象**（首尾 `{` `}`）—— 原版每个 key 存的都是一份 JSON 对象。</para></summary>
        public static bool SetRaw(string key, string payloadJson)
        {
            string why;
            if (!ValidateKey(key, "SetRaw", out why)) return false;

            if (!StoreOn)
            {
                SpeakOnce("storeoff", false,
                    "[SaveCenter] 落盘**关着**（没设 `OverrideRoot`，且这一趟不在真的一局里）⇒ 本次写 '" + key
                  + "' 被丢弃、**一个字节都没碰玩家真档**。⚠️ 自检要走盘：先设 `SaveCenter.OverrideRoot = <临时目录>`。");
                LastWriteOk = false;
                LastWriteError = "StoreOn = false（批处理 / 不在 Play 且没设 OverrideRoot）";
                return false;
            }

            if (!GuardNotFuture(key)) return false;

            if (payloadJson == null)
            {
                LogError("[SaveCenter] SetRaw('" + key + "')：payload 是 null ⇒ 拒绝");
                LastWriteOk = false; LastWriteError = "payload 是 null";
                return false;
            }

            string payload = payloadJson.Trim();
            if (payload.Length < 2 || payload[0] != '{' || payload[payload.Length - 1] != '}')
            {
                LogError("[SaveCenter] SetRaw('" + key + "')：payload 不是 JSON 对象（开头/结尾不是 { }）："
                       + Head(payload, 60) + " ⇒ 拒绝");
                LastWriteOk = false; LastWriteError = "payload 不是 JSON 对象";
                return false;
            }

            // 盘上那份要是坏的：先隔离再写（坏档不能拦着恢复，但**也不许销毁**）
            string bad;
            int diskVersion;
            if (TryProbeOnDisk(key, out diskVersion, out bad) && bad != null)
            {
                Debug.LogWarning("[SaveCenter] 写 '" + key + "' 之前发现盘上那份读不出版本（" + bad + "）⇒ 先隔离成 `.bak` 再写");
                Quarantine(key, bad);
            }

            int version = CurrentVersion(key);
            string json = WrapEnvelope(version, payload);
            string error;
            bool ok = AtomicWrite(PathOf(key), json, out error);
            LastWriteOk = ok;
            LastWriteError = ok ? null : error;
            if (ok)
            {
                Debug.Log("[SaveCenter] 写 '" + key + "' 成功：v" + version + " · " + Utf8Bytes(json) + " B · `" + PathOf(key) + "`");
            }
            else
            {
                Debug.LogError("[SaveCenter] 写 '" + key + "' **失败**：" + error + "（`" + PathOf(key) + "`）⇒ "
                             + "这一份**没落盘**（红线：不许静默失败 —— 玩家看到「存住了」其实是假的）");
            }
            return ok;
        }

        /// <summary>删一个 key。⚠️ **不真删**：改名到 `<key>.json.del-<时间戳>`（可回滚）。
        /// <para>原版 `ClearLocalPlayerData()`（`PlayerDataManager.cs:10157-10172`）是真的 `File.Delete` ——
        /// 它敢删是因为云端有权威副本；我们没有。</para></summary>
        public static bool Delete(string key)
        {
            string why;
            if (!ValidateKey(key, "Delete", out why)) return false;
            if (!StoreOn)
            {
                SpeakOnce("storeoff|del", false,
                    "[SaveCenter] 落盘**关着** ⇒ Delete('" + key + "') 不动盘上任何东西。");
                return false;
            }
            if (!GuardNotFuture(key)) return false;

            string path = PathOf(key);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SaveCenter] Delete('" + key + "')：本来就没有这份档（`" + path + "`）⇒ 什么都不做");
                return false;
            }
            string del = path + ".del-" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            if (File.Exists(del)) del += "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            try
            {
                File.Move(path, del);
                Debug.Log("[SaveCenter] 已删 key '" + key + "'（**没真删**：改名到 `" + del + "`，可回滚）");
                return true;
            }
            catch (Exception e)
            {
                LogError("[SaveCenter] Delete('" + key + "') 失败：" + e.Message + "（`" + path + "`）");
                return false;
            }
        }

        /// <summary>把「上一次写盘」的结果交出来（本层每次 `Set*` **立即**落盘，没有缓冲）。
        /// 返回 `false` 时会再出声一次 —— 调用方（将来 Step 4 的切换点）可以用它决定要不要重试/提示玩家。</summary>
        public static bool Flush()
        {
            if (!LastWriteOk)
            {
                Debug.LogError("[SaveCenter] Flush()：上一次写盘**没成功**（" + LastWriteError + "）⇒ 那几笔数据只在内存里。");
            }
            return LastWriteOk;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  诊断（Step 4 切换时拿它做「切换前/后逐字段相等」的基线）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>列出根目录里**每一个** `<key>.json`（版本 / 字节数 / 坏档原因），再列出键表里**没有文件**的 key。
        /// <para>⚠️ 只列主文件：`.tmp` / `.bak` / `.del-*` 都不算（它们是隔离/回滚件，不是当前档）。</para></summary>
        public static string DumpAll()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[SaveCenter] 根目录：`").Append(RootDir).AppendLine("`");
            int files = 0;
            if (!Directory.Exists(RootDir))
            {
                sb.AppendLine("  （目录不存在 ⇒ 一个 key 都没有）");
            }
            else
            {
                string[] all = Directory.GetFiles(RootDir);
                Array.Sort(all, StringComparer.Ordinal);          // 顺序稳定（自检可逐字比）
                for (int i = 0; i < all.Length; i++)
                {
                    string f = all[i];
                    if (!f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;   // 滤掉 .tmp/.bak/.del-*
                    files++;
                    string text = "";
                    try { text = File.ReadAllText(f, Encoding.UTF8); } catch { }
                    string problem;
                    int v = ProbeVersion(text, out problem);
                    FileInfo fi = new FileInfo(f);
                    sb.Append("  ").Append(Path.GetFileNameWithoutExtension(f))
                      .Append(" · v").Append(v >= 0 ? v.ToString() : "?")
                      .Append(" · ").Append(fi.Length).Append(" B")
                      .Append(problem == null ? "" : " · 🔴 " + problem)
                      .AppendLine();
                }
            }
            sb.Append("  文件数 = ").Append(files).Append(" / 键表 ").Append(SaveKeys.Count).AppendLine(" 条");

            StringBuilder missing = new StringBuilder();
            int nMissing = 0;
            for (int i = 0; i < SaveKeys.All.Length; i++)
            {
                if (File.Exists(PathOf(SaveKeys.All[i]))) continue;
                nMissing++;
                if (missing.Length > 0) missing.Append(" · ");
                missing.Append(SaveKeys.All[i]);
            }
            sb.Append("  键表里**没有文件**的 ").Append(nMissing).Append(" 条：").Append(nMissing == 0 ? "（无）" : missing.ToString());
            return sb.ToString();
        }

        /// <summary>自检专用：把「只说一次」的记账 / 未来版本锁 / 版本表 / 迁移表 / 写盘结果全清掉。
        /// ⚠️ **不碰盘**（要清盘自己删目录）。</summary>
        public static void ResetForTest()
        {
            lock (_gate)
            {
                _said.Clear();
                _future.Clear();
                _current.Clear();
                _migrations.Clear();
                LastWriteOk = true;
                LastWriteError = null;
            }
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  内部：校验 / 信封 / 版本探测 / 原子写 / 隔离
        // ════════════════════════════════════════════════════════════════════════════════════

        static bool ValidateKey(string key, string who, out string problem)
        {
            problem = null;
            if (string.IsNullOrEmpty(key))
            {
                problem = "key 是空的";
                LogError("[SaveCenter] " + who + "：key 是空的 ⇒ 拒绝");
                return false;
            }
            if (!SaveKeys.IsKnown(key))
            {
                problem = "key 不在键表里";
                LogError("[SaveCenter] " + who + "：key '" + key + "' **不在键表**（`Shell/SaveKeys.cs`）⇒ 拒绝。"
                       + "要加新 key 就先把它写进键表（原版没有的用 `_ours_` 前缀）—— ⛔ 别让拼错的 key 静默建出孤儿文件。");
                return false;
            }
            if (!SaveKeys.IsAsciiKey(key))
            {
                problem = "key 含非法字符";
                LogError("[SaveCenter] " + who + "：key '" + key + "' 含非法字符（只允许字母/数字/`_`/`.`）⇒ 拒绝");
                return false;
            }
            return true;
        }

        /// <summary>payload 是不是**合法 JSON 对象**（Step 2 导入前也要用它把坏旧档挡在外面）。</summary>
        public static bool IsWellFormedJsonObject(string text, out string problem)
        {
            problem = null;
            if (text == null) { problem = "内容是 null"; return false; }
            string t = text.Trim();
            if (t.Length < 2) { problem = "内容太短（" + t.Length + " 字符）"; return false; }
            if (t[0] != '{' || t[t.Length - 1] != '}') { problem = "不是 JSON 对象（首/尾不是 { }）"; return false; }
            int v = ProbeVersion(t, out problem);      // ProbeVersion 内部就是 JsonUtility 解析一次
            if (problem != null) return false;
            if (v < 0) { problem = "版本字段是负数"; return false; }
            return true;
        }

        /// <summary>读 `_v`。`problem != null` ⇒ 这份内容**不是合法 JSON**（`JsonUtility` 抛了）。
        /// <para>⚠️ 返回 `0` 但 `problem == null` = 合法 JSON 但**没有 `_v`**（老档 / 手写档）。</para></summary>
        static int ProbeVersion(string text, out string problem)
        {
            problem = null;
            if (string.IsNullOrEmpty(text)) { problem = "文件是空的"; return 0; }
            if (text.Trim().Length == 0) { problem = "文件只有空白"; return 0; }
            SaveVersionProbe probe;
            try { probe = JsonUtility.FromJson<SaveVersionProbe>(text); }
            catch (Exception e) { problem = "不是合法 JSON：" + e.Message; return 0; }
            if (probe == null) { problem = "JSON 解析结果是 null"; return 0; }
            return probe._v;
        }

        /// <summary>**不产生副作用**地探一下盘上那份的版本。返回 false = 没有文件。
        /// `problem != null` = 有文件但读不出来（坏档）—— 写路径据此决定「先隔离再写」。</summary>
        static bool TryProbeOnDisk(string key, out int version, out string problem)
        {
            version = 0; problem = null;
            string path = PathOf(key);
            if (!File.Exists(path)) return false;
            string text;
            try { text = File.ReadAllText(path, Encoding.UTF8); }
            catch (Exception e) { problem = "读不了：" + e.Message; return true; }
            version = ProbeVersion(text, out problem);
            return true;
        }

        /// <summary>写/删之前的**未来版本闸门**：盘上 `_v` 比本代码高 ⇒ 一律拒绝并锁住这个 key。</summary>
        static bool GuardNotFuture(string key)
        {
            if (_future.Contains(key))
            {
                LogError("[SaveCenter] key '" + key + "' 是**未来版本**的档（已锁）⇒ 本轮拒绝写/删（降级运行不能毁新档）");
                LastWriteOk = false; LastWriteError = "未来版本锁：" + key;
                return false;
            }
            int v; string problem;
            if (!TryProbeOnDisk(key, out v, out problem)) return true;   // 没有文件 ⇒ 随便写
            if (problem != null) return true;                            // 坏档 ⇒ 交给写路径「先隔离再写」
            if (v > CurrentVersion(key)) { LockFuture(key, v); LastWriteOk = false; LastWriteError = "未来版本锁：" + key; return false; }
            return true;
        }

        static void LockFuture(string key, int diskVersion)
        {
            _future.Add(key);
            LogError("[SaveCenter] key '" + key + "' 的档是**未来版本**（盘上 v" + diskVersion
                   + " > 本代码 v" + CurrentVersion(key) + "）⇒ **拒绝读/写/删**，文件原地不动。"
                   + "理由：原版删档是因为云端有权威副本（`PlayerDataManager.cs:10157-10172`），**我们没有** ⇒ "
                   + "降级运行绝不能覆盖新版写下的档。换回新版客户端再打开。");
        }

        static string WrapEnvelope(int version, string payloadObjectJson)
        {
            // payloadObjectJson 已 trim、首尾一定是 { }
            string inner = payloadObjectJson.Substring(1, payloadObjectJson.Length - 2);
            if (inner.Trim().Length == 0) return "{\"" + VersionField + "\":" + version + "}";
            return "{\"" + VersionField + "\":" + version + "," + inner + "}";
        }

        /// <summary>把 `{"_v":N,...}` 剥成 `{...}`。只认「`_v` 是**第一个键**」（写的时候就是这样）。
        /// 返回 null ⇒ 结构不认识（`problem` 给原因）。</summary>
        static string StripEnvelope(string text, out string problem)
        {
            problem = null;
            int i = 0, n = text.Length;
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n || text[i] != '{') { problem = "开头不是 `{`"; return null; }
            i++;
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n || text[i] != '"') { problem = "第一个键不是字符串"; return null; }
            int keyStart = i;
            i = SkipJsonString(text, i);
            if (i < 0) { problem = "键字符串没闭合"; return null; }
            string key = text.Substring(keyStart + 1, i - keyStart - 2);
            if (key != VersionField) { problem = "第一个键是 `" + key + "`、不是 `" + VersionField + "`"; return null; }
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n || text[i] != ':') { problem = "版本键后面没有 `:`"; return null; }
            i++;
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            int vStart = i;
            while (i < n && (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '+')) i++;
            if (i == vStart) { problem = "`_v` 的值不是整数"; return null; }
            while (i < n && char.IsWhiteSpace(text[i])) i++;
            if (i >= n) { problem = "对象没闭合"; return null; }
            if (text[i] == ',') { i++; return "{" + text.Substring(i); }
            if (text[i] == '}') return "{}";
            problem = "`_v` 的值后面既不是 `,` 也不是 `}`";
            return null;
        }

        static int SkipJsonString(string s, int quoteIndex)
        {
            int i = quoteIndex + 1;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\') { i += 2; continue; }
                if (c == '"') return i + 1;
                i++;
            }
            return -1;
        }

        static string ApplyMigrations(string key, string payload, int version)
        {
            int target = CurrentVersion(key);
            if (version >= target) return payload;
            Dictionary<int, Func<string, string>> table;
            if (!_migrations.TryGetValue(key, out table))
            {
                SpeakOnce("nomig|" + key + "|" + version, false,
                    "[SaveCenter] key '" + key + "' 盘上是 v" + version + "、本代码是 v" + target
                  + "，但**没注册迁移** ⇒ 原样返回 payload（缺的字段会按默认值读出来 —— 这是 G2 的已知残留）。");
                return payload;
            }
            int guard = 0;
            while (version < target)
            {
                Func<string, string> f;
                if (!table.TryGetValue(version, out f))
                {
                    SpeakOnce("miggap|" + key + "|" + version, true,
                        "[SaveCenter] key '" + key + "' 的迁移链**断了**：缺 v" + version + " → v" + (version + 1)
                      + " ⇒ 停在 v" + version + "，原样返回（文件没动）");
                    return payload;
                }
                try { payload = f(payload); }
                catch (Exception e)
                {
                    LogError("[SaveCenter] key '" + key + "' 的迁移函数 v" + version + " 抛了：" + e.Message
                           + " ⇒ 本次返回 null（文件**没动**）");
                    return null;
                }
                version++;
                if (++guard > 64) { LogError("[SaveCenter] key '" + key + "' 的迁移超过 64 步 ⇒ 判定成环，停止"); break; }
            }
            return payload;
        }

        static bool AtomicWrite(string path, string json, out string error)
        {
            error = null;
            string tmp = path + ".tmp";
            lock (_gate)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter w = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        w.Write(json);
                        w.Flush();
                        try { fs.Flush(true); } catch (Exception) { /* 非 NTFS/某些平台不支持落盘 fsync ⇒ 不影响原子换名 */ }
                    }
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Replace(tmp, path, null, true);     // NTFS：原子换名
                        }
                        catch (Exception eReplace)
                        {
                            // 兜底：非 NTFS / 跨卷 / 被占用 ⇒ 旧的两步法（**不是原子**，出声）
                            Debug.LogWarning("[SaveCenter] `File.Replace` 不可用（" + eReplace.Message
                                           + "）⇒ 退回「删旧 + 换名」（小窗口内不是原子，已知）");
                            File.Delete(path);
                            File.Move(tmp, path);
                        }
                    }
                    else
                    {
                        File.Move(tmp, path);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    error = e.Message;
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch (Exception) { }
                    return false;
                }
            }
        }

        /// <summary>把坏档**改名**成 `<key>.json.bak`（已有就 `.bak1`/`.bak2`…）。⛔ 不删除、不覆盖。
        /// <para>原版没有这一档（`OnlineData.cs:145-193` 读的时候**没有 try/catch**）—— 这是**我们比原版强**的地方。</para></summary>
        static void Quarantine(string key, string why)
        {
            string path = PathOf(key);
            string bak = path + ".bak";
            int n = 1;
            while (File.Exists(bak)) { bak = path + ".bak" + n; n++; }
            try
            {
                File.Move(path, bak);
                Debug.LogError("[SaveCenter] key '" + key + "' 的档**坏了**（" + why + "）⇒ 已隔离到 `" + bak
                             + "`，本次返回 null。要恢复：修好那份 `.bak` 再改名回 `" + Path.GetFileName(path) + "`。"
                             + "⚠️ 原版这一档**没有**（坏 JSON 直接冒泡，`OnlineData.cs:145-193`）。");
            }
            catch (Exception e)
            {
                LogError("[SaveCenter] key '" + key + "' 的档坏了（" + why + "），**而且隔离也失败**：" + e.Message
                       + "（`" + path + "`）⇒ 本次返回 null，文件原地没动");
            }
        }

        static void SpeakOnce(string id, bool isError, string message)
        {
            lock (_gate)
            {
                if (!_said.Add(id)) return;
            }
            if (isError) Debug.LogError(message); else Debug.LogWarning(message);
        }

        static void LogError(string message) { Debug.LogError(message); }

        static string Head(string s, int n)
        {
            if (s == null) return "<null>";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static int Utf8Bytes(string s)
        {
            return s == null ? 0 : new UTF8Encoding(false).GetByteCount(s);
        }
    }

    /// <summary>`{"_v":N}` 的解析探针 —— **只认这一个字段**，别的字段 `JsonUtility` 会自动忽略。
    /// <para>⚠️ 放在**文件级**（不嵌进 `SaveCenter`）：`JsonUtility` 对嵌套类型的支持在各 Unity 版本上不一致
    /// （同 `CardProgress` 那条既有注释）。它不是 `MonoBehaviour` ⇒ 不会被场景烘成 `!u!115` 存根。</para></summary>
    [Serializable]
    public class SaveVersionProbe
    {
        /// <summary>schema 版本（0 = 没有这个键）。</summary>
        public int _v;
    }
}
