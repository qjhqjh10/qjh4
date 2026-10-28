// SaveCenter.cs — 中央存档（完全复刻 · Step 1「建中央层」）
//
// ⛔ **原定「纯新增、不接入任何现有子系统」（Step 4 才切，要双写期 + 全字段 dump 相等）**——
//    ⚠️ **2026-10-28 更正（`B90-N5`，铁律 5）**：**首个消费者已出现 = `Shell/ChatCache.cs`**（`SaveKeys.BlockedPlayers`）。
//    那是「新功能直接用中央口」、不是「把旧档切过来」⇒ Step 4 的双写期纪律不变；但「没有任何子系统调用它」这句已不成立。
//    ✅ 仍然成立：**现有 10 份旧档一个字节都不会因为它而改变**。
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
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>中央存档的统一读写口（原版 `PlayerDataManager` 的本地等价物）。
    /// <para>接口：<see cref="Get{T}"/> / <see cref="GetRaw"/> / <see cref="Set{T}"/> / <see cref="SetRaw"/> /
    /// <see cref="Exists"/> / <see cref="Delete"/> / <see cref="Flush"/> / <see cref="RegisterMigration"/>。
    /// ⚠️ 2026-10-28（`B90-N5`）：**已有首个消费者 = `Shell/ChatCache.cs`**（原写「Step 1 阶段没有任何子系统调用它」，已不成立）。</para></summary>
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
            string payload;
            if (problem != null)
            {
                // Step 3 兜底：坏档 ⇒ **先试 `.bak` 回退**（救回来就照常返回；救不回来才只隔离）。
                // ⚠️ 原版**没有这一档**（`OnlineData.cs:145-193` 读的时候没有 try/catch，坏 JSON 直接冒泡）
                //    —— 这是方案 §七 Step 3 明写的「我们比原版强」的那一处。
                string recoveredFrom;
                int recoveredVersion;
                if (TryRecoverFromBackup(key, problem, out payload, out recoveredVersion, out recoveredFrom))
                {
                    return ApplyMigrations(key, payload, recoveredVersion);
                }
                QuarantineIfAllowed(key, problem);
                return null;
            }

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

            // payload 自己带了**顶层 `_v`** ⇒ 套信封会产出**重复键**（谁赢取决于解析器，且两种解析器可能不同）
            // ⇒ 直接拒绝。只在文本里真的出现 `"_v"` 时才付解析代价（绝大多数写盘走不到这一支）。
            if (payload.IndexOf("\"" + VersionField + "\"", StringComparison.Ordinal) >= 0)
            {
                string probeError;
                SortedDictionary<string, string> flat = SaveJsonDiff.Flatten(payload, out probeError);
                if (probeError == null && flat.ContainsKey(SaveJsonDiff.Quote(VersionField)))
                {
                    LogError("[SaveCenter] SetRaw('" + key + "')：payload **顶层自己带了 `" + VersionField + "`** ⇒ 拒绝"
                           + "（套上信封会出现重复键，读回来是哪个值取决于解析器）。把它改成别的名字再写。");
                    LastWriteOk = false; LastWriteError = "payload 顶层有 " + VersionField;
                    return false;
                }
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
                // Step 4 双写期：登记过旧档的 key，把**同一份 payload（不带 `_v`）**再写一份到旧档。
                // ⚠️ 双写失败**必须出声**：此刻中央档是新值、旧档还是旧值 —— 用户回滚旧客户端会读到过期数据。
                if (LegacyMirrorOn)
                {
                    string mirrorPath = LegacyMirrorPathOf(key);
                    if (mirrorPath != null)
                    {
                        string mirrorError;
                        LastMirrorOk = MirrorToLegacy(key, payload, out mirrorError);
                        LastMirrorError = LastMirrorOk ? null : mirrorError;
                        if (LastMirrorOk)
                            Debug.Log("[SaveCenter] 双写：'" + key + "' → 旧档 `" + mirrorPath + "`（" + Utf8Bytes(payload) + " B，不含 `_v`）");
                        else
                            Debug.LogError("[SaveCenter] 双写**失败**：'" + key + "' 的旧档 `" + mirrorPath + "` 没写成（" + mirrorError
                                         + "）⇒ 中央档是新值、旧档还是旧值（回滚旧客户端会读到过期数据）。`Flush()` 会返回 false。");
                    }
                }
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
        /// 返回 `false` 时会再出声一次 —— 调用方（将来 Step 4 的切换点）可以用它决定要不要重试/提示玩家。
        /// <para>⚠️ 双写期（Step 4）还要求**旧档也写成了**：中央档成功、旧档失败 ⇒ 这里返回 false 并出声
        /// （两边不一致 = 回滚旧客户端会读到过期数据，不能当成功）。</para></summary>
        public static bool Flush()
        {
            if (!LastWriteOk)
            {
                Debug.LogError("[SaveCenter] Flush()：上一次写盘**没成功**（" + LastWriteError + "）⇒ 那几笔数据只在内存里。");
            }
            if (!LastMirrorOk)
            {
                Debug.LogError("[SaveCenter] Flush()：上一次**双写**没成功（" + LastMirrorError
                             + "）⇒ 中央档与旧档此刻**不一致**（Step 4 双写期内不许当成功）。");
            }
            return LastWriteOk && LastMirrorOk;
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

        /// <summary>自检专用：把「只说一次」的记账 / 未来版本锁 / 版本表 / 迁移表 / 写盘结果 / **双写登记表** 全清掉。
        /// ⚠️ **不碰盘**（要清盘自己删目录）。</summary>
        public static void ResetForTest()
        {
            lock (_gate)
            {
                _said.Clear();
                _future.Clear();
                _current.Clear();
                _migrations.Clear();
                _mirror.Clear();
                LastWriteOk = true;
                LastWriteError = null;
                LastMirrorOk = true;
                LastMirrorError = null;
                RecoverCount = 0;
                LastRecoveredFrom = null;
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

        /// <summary>隔离的门（照 <see cref="StoreOn"/> 的既有纪律）：**落盘关着时一律不碰玩家的文件**。
        /// <para>为什么需要这层：`Quarantine` 会 `File.Move` 玩家的真档 —— 批处理自检里读到一个坏档就把它改名，
        /// 等于自检篡改了玩家数据。⇒ 批处理下只出声、不动盘。</para></summary>
        static void QuarantineIfAllowed(string key, string why)
        {
            if (!StoreOn)
            {
                SpeakOnce("quarantine|" + key, true,
                    "[SaveCenter] key '" + key + "' 的档坏了（" + why + "）⇒ **落盘关着**（批处理 / 没设 OverrideRoot），"
                  + "⇒ **不动盘上任何文件**（不隔离、不回退）。要修它请在真 Play 里打开一次。");
                return;
            }
            Quarantine(key, why);
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

        // ════════════════════════════════════════════════════════════════════════════════════
        //  Step 3 —— 兜底：坏档从 `.bak` 回退
        //  ⚠️ 本节全部成员**追加在类尾**（`CLAUDE.md`：新成员别插进已有成员之间，免得下游行号漂）。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>本进程**从 `.bak` 救回来**的次数（Step 3 兜底真的生效过几次）。</summary>
        public static int RecoverCount { get; private set; }

        /// <summary>最近一次救回来的来源文件（`null` = 本进程没救过）。</summary>
        public static string LastRecoveredFrom { get; private set; }

        /// <summary>坏档 ⇒ 找一个能解析的备份（`.bak` / `.bak1` / …，**编号大的先试**）、把坏档隔离掉、
        /// 再把救回来的 payload 写回主档。返回 false = 没有可用备份（调用方走原来的「隔离 + null」）。
        /// <para>顺序（三步都不可省）：① 试备份 → ② **先把坏档改名隔离**（⛔ 不销毁）→ ③ **把好的写回主档**
        /// （只在 <see cref="StoreOn"/> 时；否则只在内存里，出声说明）。</para>
        /// <para>⚠️ 备份本身是**未来版本**时**不采用**（同一条红线：降级运行不能拿旧代码去改写新档）。</para></summary>
        static bool TryRecoverFromBackup(string key, string why, out string payload, out int version, out string fromPath)
        {
            payload = null; version = 0; fromPath = null;
            string path = PathOf(key);
            string[] cands = BackupCandidates(path);
            if (cands.Length == 0) return false;                      // 连备份都没有 ⇒ 交回调用方

            string chosen = null, chosenPayload = null;
            int chosenVersion = 0;
            for (int i = 0; i < cands.Length; i++)
            {
                string t;
                try { t = File.ReadAllText(cands[i], Encoding.UTF8); }
                catch (Exception) { continue; }
                string p;
                int v = ProbeVersion(t, out p);
                if (p != null) continue;                              // 备份本身也坏 ⇒ 试更早那份
                if (v > CurrentVersion(key)) continue;                 // 未来版本 ⇒ 坚决不采用
                string inner;
                if (v <= 0)
                {
                    if (!IsWellFormedJsonObject(t, out p)) continue;   // 没有 `_v` 的：必须是合法 JSON 对象
                    inner = t.Trim();
                    v = DefaultVersion;
                }
                else
                {
                    inner = StripEnvelope(t, out p);
                    if (inner == null) continue;
                }
                chosen = cands[i]; chosenPayload = inner; chosenVersion = v;
                break;
            }
            if (chosen == null) return false;

            if (StoreOn) Quarantine(key, why);                        // ① 坏档先挪走（不销毁，且不会覆盖已有的 .bakN）

            bool restored = false;
            string restoreError = null;
            if (StoreOn) restored = AtomicWrite(path, WrapEnvelope(chosenVersion, chosenPayload), out restoreError);

            RecoverCount++;
            LastRecoveredFrom = chosen;
            payload = chosenPayload;
            version = chosenVersion;
            fromPath = chosen;
            Debug.LogError("[SaveCenter] key '" + key + "' 的档坏了（" + why + "）⇒ **已从备份 `" + chosen + "` 救回**（v"
                         + chosenVersion + "）。" + (restored
                             ? "并已写回主档 `" + path + "`。"
                             : StoreOn ? "⚠️ **写回主档失败**：" + restoreError + "（数据仍在内存里；下一次写会覆盖主档）"
                                       : "落盘关着 ⇒ **只在内存里**：既没写回主档、也没隔离坏档（自检/批处理不碰玩家真档）。")
                         + " ⚠️ 原版没有这一档（坏 JSON 直接冒泡，`OnlineData.cs:145-193`）。");
            return true;
        }

        /// <summary>列出某份存档的备份候选，**编号大的（新的）在前**。只认 `<file>.bak` 与 `<file>.bak<数字>`。
        /// <para>⚠️ 编号方向：`Quarantine` 是从 `.bak` 起找**第一个空位**，所以编号**越大越新**。</para></summary>
        static string[] BackupCandidates(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return new string[0];
            string name = Path.GetFileName(path);
            string[] all;
            try { all = Directory.GetFiles(dir, name + ".bak*"); }
            catch (Exception) { return new string[0]; }

            List<string> keep = new List<string>();
            List<int> nums = new List<int>();
            for (int i = 0; i < all.Length; i++)
            {
                string f = Path.GetFileName(all[i]);
                if (!f.StartsWith(name + ".bak", StringComparison.Ordinal)) continue;
                string tail = f.Substring((name + ".bak").Length);
                int n = 0;
                if (tail.Length > 0)
                {
                    bool digits = true;
                    for (int k = 0; k < tail.Length; k++)
                        if (tail[k] < '0' || tail[k] > '9') { digits = false; break; }
                    if (!digits) continue;
                    if (!int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) continue;
                }
                // 插入排序：编号降序（同号按名字定序，保证可复现）
                int at = keep.Count;
                while (at > 0)
                {
                    int prev = nums[at - 1];
                    bool prevBefore = prev > n || (prev == n && string.CompareOrdinal(keep[at - 1], all[i]) <= 0);
                    if (prevBefore) break;
                    at--;
                }
                keep.Insert(at, all[i]);
                nums.Insert(at, n);
            }
            return keep.ToArray();
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  Step 4 —— 双写期（中央档 + 旧档同时写），一个开关关掉
        //  判据 = 方案 §七 Step 4：「每切一个：双写期（同时写旧文件 + 新 key）跑一轮自检，再删旧写入」。
        //  ⚠️ 本节全部成员**追加在类尾**。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>**双写期总开关**（Step 4 默认开 · Step 5 关掉）。
        /// <para>`true`：**登记过**（<see cref="RegisterLegacyMirror"/>）的 key 在 `Set*` 写成功后，把同一份 payload
        /// 再写一份到旧档 —— **不带 `_v` 信封**（旧客户端照旧能读，也不改它认识的文件形状）。</para>
        /// <para>`false`（Step 5）：只写中央档。⛔ 关之前必须确认每个登记过的 key 都已有中央档（见 <see cref="DumpMirrorTable"/>）。</para></summary>
        public static bool LegacyMirrorOn = true;

        static readonly Dictionary<string, string> _mirror = new Dictionary<string, string>();   // key → 旧档绝对路径

        /// <summary>最近一次**双写**成功没有（没登记过旧档的 key 不受影响，保持上一次的值）。</summary>
        public static bool LastMirrorOk { get; private set; } = true;

        /// <summary>最近一次双写失败的原文（成功时 `null`）。</summary>
        public static string LastMirrorError { get; private set; }

        /// <summary>登记「这个 key 的旧档在哪」。登记之后每次写这个 key 就会**双写**到那份旧档。
        /// <para>⛔ 两个 key 不许登记到同一个文件（会互相覆盖）；同一 key 重复登记同一路径 = 幂等返回 true。</para>
        /// <para>⚠️ 只登记、**不检查**旧档在不在 —— 旧档是「另一个客户端的文件」，这一步不该因为文件缺失而失败。</para></summary>
        public static bool RegisterLegacyMirror(string key, string legacyFullPath)
        {
            string why;
            if (!ValidateKey(key, "RegisterLegacyMirror", out why)) return false;
            if (string.IsNullOrEmpty(legacyFullPath))
            {
                LogError("[SaveCenter] RegisterLegacyMirror('" + key + "')：旧档路径是空的 ⇒ 拒绝");
                return false;
            }
            string full;
            try { full = Path.GetFullPath(legacyFullPath); }
            catch (Exception e)
            {
                LogError("[SaveCenter] RegisterLegacyMirror('" + key + "')：路径 `" + legacyFullPath + "` 不合法（" + e.Message + "）⇒ 拒绝");
                return false;
            }
            lock (_gate)
            {
                string old;
                if (_mirror.TryGetValue(key, out old))
                {
                    if (string.Equals(old, full, StringComparison.OrdinalIgnoreCase)) return true;     // 幂等
                    LogError("[SaveCenter] RegisterLegacyMirror('" + key + "')：**已经登记过**另一条路径（`" + old
                           + "`）⇒ 拒绝改（要改先 UnregisterLegacyMirror）");
                    return false;
                }
                foreach (KeyValuePair<string, string> kv in _mirror)
                {
                    if (!string.Equals(kv.Value, full, StringComparison.OrdinalIgnoreCase)) continue;
                    LogError("[SaveCenter] RegisterLegacyMirror('" + key + "')：`" + full + "` **已被 key '" + kv.Key
                           + "' 占用** ⇒ 拒绝（两个 key 写同一份旧档 = 后写的静默盖掉先写的）");
                    return false;
                }
                _mirror[key] = full;
            }
            Debug.Log("[SaveCenter] 已登记双写：'" + key + "' → 旧档 `" + full + "`（Step 5 关 `LegacyMirrorOn` 即停）");
            return true;
        }

        /// <summary>撤销登记（Step 5 逐个 key 收尾时用）。返回是否原本就登记着。</summary>
        public static bool UnregisterLegacyMirror(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            lock (_gate) { return _mirror.Remove(key); }
        }

        /// <summary>这个 key 的旧档绝对路径（没登记 ⇒ `null`）。</summary>
        public static string LegacyMirrorPathOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            lock (_gate)
            {
                string p;
                return _mirror.TryGetValue(key, out p) ? p : null;
            }
        }

        /// <summary>手动双写一次（Step 4 的切换点若不方便走 `Set` 可调它）。走登记的旧档路径。</summary>
        public static bool MirrorNow(string key, string payloadJson)
        {
            string why;
            if (!ValidateKey(key, "MirrorNow", out why)) return false;
            if (!StoreOn) { SpeakOnce("storeoff|mirror", false, "[SaveCenter] 落盘关着 ⇒ MirrorNow('" + key + "') 不动盘上任何东西。"); return false; }
            string err;
            LastMirrorOk = MirrorToLegacy(key, payloadJson, out err);
            LastMirrorError = LastMirrorOk ? null : err;
            return LastMirrorOk;
        }

        /// <summary>登记表里的全部 key（序数序）。给「双写期诊断」用 —— ⛔ 别拿它去遍历写盘。</summary>
        public static string[] MirrorKeys()
        {
            lock (_gate)
            {
                string[] keys = new string[_mirror.Count];
                _mirror.Keys.CopyTo(keys, 0);
                Array.Sort(keys, StringComparer.Ordinal);
                return keys;
            }
        }

        /// <summary>把一个 key 的**当前中央档 payload** 逐字段和它登记的旧档比一遍，返回报告（"" = 相等）。
        /// 这是方案 §七 Step 4 的**验收判据**：切前切后 dump 必须逐字段相等。</summary>
        public static string DiffWithLegacy(string key)
        {
            string legacyPath = LegacyMirrorPathOf(key);
            if (legacyPath == null) return "key '" + key + "' 没有登记旧档 ⇒ 无从比较（先 RegisterLegacyMirror）";
            if (!File.Exists(PathOf(key))) return "中央档不存在：`" + PathOf(key) + "`";
            if (!File.Exists(legacyPath)) return "旧档不存在：`" + legacyPath + "`";
            return DiffPayload(GetRaw(key), SafeRead(legacyPath));
        }

        /// <summary>两份 **payload**（已剥 `_v` 的）的逐字段对比。返回 "" = **全字段相等**；否则是人看的差异清单。
        /// <para>⚠️ 只比字段与值（对象键序不敏感、数字 `1.0` 与 `1` 视为相等、**数组顺序敏感**）——
        /// 不比空白与书写格式（那是实现细节，不是「字段」）。</para></summary>
        public static string DiffPayload(string expectedPayload, string actualPayload)
        {
            return SaveJsonDiff.Diff(expectedPayload, actualPayload);
        }

        /// <summary>双写表 + 每一条「中央档 vs 旧档」的逐字段对比结论（Step 4 收尾 / Step 5 前的必查项）。</summary>
        public static string DumpMirrorTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[SaveCenter] 双写表（LegacyMirrorOn = ").Append(LegacyMirrorOn ? "true" : "false").AppendLine("）：");
            string[] keys;
            lock (_gate) { keys = new string[_mirror.Count]; _mirror.Keys.CopyTo(keys, 0); }
            Array.Sort(keys, StringComparer.Ordinal);
            if (keys.Length == 0)
            {
                sb.AppendLine("  （**没有登记任何 key** ⇒ 现在只写中央档 —— 还没进双写期）");
                return sb.ToString();
            }
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                string p = LegacyMirrorPathOf(k);
                bool hasC = File.Exists(PathOf(k));
                bool hasL = File.Exists(p);
                string state;
                if (!hasC || !hasL) state = "⚠️ 缺 " + (hasC ? "" : "中央档 ") + (hasL ? "" : "旧档 ");
                else
                {
                    string d = DiffPayload(GetRaw(k), SafeRead(p));
                    state = d.Length == 0 ? "✅ 逐字段相等" : "🔴 不一致（见 DiffWithLegacy）";
                }
                sb.Append("  ").Append(k).Append(" → `").Append(p).Append("` · ").AppendLine(state);
            }
            return sb.ToString();
        }

        static bool MirrorToLegacy(string key, string payload, out string error)
        {
            error = null;
            string path = LegacyMirrorPathOf(key);
            if (path == null) { error = "没有登记旧档路径"; return false; }
            if (payload == null) { error = "payload 是 null"; return false; }
            return AtomicWrite(path, payload, out error);      // 旧档写**不带 `_v` 信封**（旧客户端照旧能读）
        }

        static string SafeRead(string path)
        {
            try { return File.ReadAllText(path, Encoding.UTF8); }
            catch (Exception e) { Debug.LogWarning("[SaveCenter] 读 `" + path + "` 失败：" + e.Message); return null; }
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  Step 4 —— 子系统「切换口」（2026-10-25 · 执行代理 R4）
        //  逐子系统只改两处，⛔ 别在子系统里再自己 `File.WriteAllText` 一次（那就是三份写手）：
        //     写：string json = JsonUtility.ToJson(dto);
        //         SaveCenter.SaveThroughLegacy(<key>, <旧档绝对路径>, json, out err);
        //     读：string text = SaveCenter.ReadThroughLegacy(<key>, <旧档绝对路径>);
        //  判据 = 方案 §七 Step 4（双写期 + 「切前 dump 与切后中央档逐字段相等」）。
        //  ⚠️ 本节成员**追加在类尾**（`CLAUDE.md`：新成员别插进已有成员之间，免得下游行号漂）。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Step 4 切换口（写）：把一份**已经序列化好的 payload** 写到「中央档 + 旧档」。
        /// <para>① 落盘开着（<see cref="StoreOn"/>）⇒ 登记双写（幂等）后走 <see cref="SetRaw"/>：
        /// 中央档带 `_v` 信封、旧档**不带**（旧客户端照旧能读），两边是**同一份 payload**。</para>
        /// <para>② 落盘关着 ⇒ **只写旧档**（原子写，形状与切换前逐字节同）。这一支是
        /// 「**子系统自己那道门开着、中央档这道门关着**」的情形（自检只注入了子系统自己的
        /// `OverrideDir`，没注入 `SaveCenter.OverrideRoot`）。必须保住切换前的老行为 ——
        /// 否则一批既有自检会突然**一条都不落盘**（而且还都「绿」，因为没人写盘就没人报错）。</para>
        /// <para>返回 `false` = **真的没写成**（中央档失败 / 双写失败都算）；`error` 是给人看的原因。
        /// 🔴 调用方**必须出声**（红线：不许静默失败）。</para></summary>
        public static bool SaveThroughLegacy(string key, string legacyFullPath, string payloadJson, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(key)) { error = "key 是空"; return false; }
            if (string.IsNullOrEmpty(legacyFullPath)) { error = "旧档路径是空"; return false; }
            if (payloadJson == null) { error = "payload 是 null"; return false; }

            // 两个分支收一样的形状（否则「落盘关着」那一支会把非对象也写进旧档 —— 而开着那一支会拒绝）。
            string payload = payloadJson.Trim();
            if (payload.Length < 2 || payload[0] != '{' || payload[payload.Length - 1] != '}')
            {
                error = "payload 不是 JSON 对象（开头/结尾不是 { }）";
                return false;
            }

            string full;
            try { full = Path.GetFullPath(legacyFullPath); }
            catch (Exception e) { error = "旧档路径不合法：" + e.Message; return false; }

            if (!StoreOn)
            {
                // 中央档这一趟不许碰（批处理 / 自检）⇒ 保持老行为：把这**一份 payload** 原样写进旧档。
                return AtomicWrite(full, payload, out error);
            }

            // 🔴 登记表里若已有**另一条**路径（例：`LegacySaveReader.RegisterMirrors` 先按玩家真目录登记过，
            //    而自检给子系统注入了临时目录）⇒ 以**调用方这次实际用的路径**为准（撤掉旧登记再登）。
            //    不这么做的话，双写会写到玩家真档上去 —— 自检污染真存档，比少一次双写糟得多。
            string registered = LegacyMirrorPathOf(key);
            if (registered == null)
            {
                if (!RegisterLegacyMirror(key, full))
                {
                    error = "双写登记失败（见上一条 [SaveCenter] 日志）";
                    return false;
                }
            }
            else if (!string.Equals(registered, full, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[SaveCenter] SaveThroughLegacy('" + key + "')：登记表里的旧档是 `" + registered
                               + "`，本次调用用的是 `" + full + "` ⇒ **改登记到后者**（以实际写入点为准）");
                UnregisterLegacyMirror(key);
                if (!RegisterLegacyMirror(key, full))
                {
                    error = "双写登记（改路径）失败（见上一条 [SaveCenter] 日志）";
                    return false;
                }
            }

            if (!SetRaw(key, payload))
            {
                error = LastWriteError ?? "SetRaw 失败";
                return false;
            }

            // 双写真的被尝试过（开关开 + 有登记 ⇒ SetRaw 里那一支一定走过）才看 LastMirrorOk。
            bool mirrored = LegacyMirrorOn && LegacyMirrorPathOf(key) != null;
            if (mirrored && !LastMirrorOk)
            {
                error = LastMirrorError ?? "双写失败";
                return false;
            }
            return true;
        }

        /// <summary>Step 4 切换口（读）：**先中央档、再旧档**。
        /// <para>中央档在（且读得出来）⇒ 用它；不在 / 坏档 / 未来版本（`GetRaw` 返回 null）⇒
        /// 退回读**旧档原文**（旧档没有 `_v` 信封，整份就是 payload）。两边都没有 ⇒ `null`。</para>
        /// <para>⚠️ 落盘关着时**根本不看中央档**（那个目录是玩家的真档）⇒ 只读调用方给的旧档路径，
        /// 与切换前完全一致（批处理 / 自检不会读到玩家真档）。</para>
        /// <para>⚠️ 这里**不自动倒灌**：旧档比中央档新也不覆盖（要人拍板；诊断走
        /// `LegacySaveReader.NewerLegacyReport`）。</para></summary>
        public static string ReadThroughLegacy(string key, string legacyFullPath)
        {
            if (StoreOn && Exists(key))
            {
                string central = GetRaw(key);
                if (central != null) return central;
                // 中央档在但读不出来（坏档 / 未来版本）：GetRaw 已经出声并处理过盘上那一份，
                // 这里**退回旧档**——旧档是另一份文件，读它是安全的，也是双写期该有的兜底。
            }
            return string.IsNullOrEmpty(legacyFullPath) ? null : SafeRead(legacyFullPath);
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

    /// <summary>JSON 的**规范化 + 逐字段对比**（Step 4 的验收判据：切前切后 dump 必须逐字段相等）。
    /// <para>⚠️ 为什么要自己写一个最小解析器：Unity 自带的 `JsonUtility` **只能反序列化到固定类型**，没有通用
    /// JSON 树；本工程也**没有** Newtonsoft（`Packages/manifest.json` 里没有 `com.unity.nuget.newtonsoft-json`）
    /// ⇒ 想比「两份任意 schema 的 JSON」只能自带一个。</para>
    /// <para>⚠️ 非 `MonoBehaviour`、无序列化字段 ⇒ 不会被场景烘成 `!u!115` 存根（同 `SaveVersionProbe`）。</para>
    /// <para>**规范化规则**（两边同一套，所以「相等」= 逐字段相等，而不是逐字节相等）：
    /// ① 对象键按**序数**排序；② 数字统一成最简十进制（`1.0` == `1`、`1e3` == `1000`）；
    /// ③ 字符串先反转义、再按**最小转义**重写（`A` == `A`）；④ **数组顺序敏感**（下标进路径）；
    /// ⑤ 空对象/空数组保留成 `{}` / `[]`（否则「没有」和「空」会被当成一样）。
    /// ⑥ 重复键 = 后写的赢（与 `JsonUtility` 的行为一致），并**不**另报。</para></summary>
    public static class SaveJsonDiff
    {
        /// <summary>差异清单最多列几条（再多只报总数）。</summary>
        public const int MaxDiffLines = 40;

        /// <summary>两份 payload 的逐字段对比。返回 `""` = **全字段相等**；否则是给人看的差异清单。</summary>
        public static string Diff(string expectedJson, string actualJson)
        {
            string e1, e2;
            SortedDictionary<string, string> a = Flatten(expectedJson, out e1);
            SortedDictionary<string, string> b = Flatten(actualJson, out e2);
            if (e1 != null) return "左边不是合法 JSON：" + e1;
            if (e2 != null) return "右边不是合法 JSON：" + e2;

            StringBuilder sb = new StringBuilder();
            int diffs = 0;
            foreach (KeyValuePair<string, string> kv in a)
            {
                string vb;
                if (!b.TryGetValue(kv.Key, out vb))
                {
                    diffs++;
                    if (diffs <= MaxDiffLines) sb.Append("  − 只有**左**边有：").Append(kv.Key).Append(" = ").AppendLine(kv.Value);
                }
                else if (!string.Equals(vb, kv.Value, StringComparison.Ordinal))
                {
                    diffs++;
                    if (diffs <= MaxDiffLines) sb.Append("  ≠ ").Append(kv.Key).Append("：左 = ").Append(kv.Value).Append(" · 右 = ").AppendLine(vb);
                }
            }
            foreach (KeyValuePair<string, string> kv in b)
            {
                if (a.ContainsKey(kv.Key)) continue;
                diffs++;
                if (diffs <= MaxDiffLines) sb.Append("  + 只有**右**边有：").Append(kv.Key).Append(" = ").AppendLine(kv.Value);
            }
            if (diffs == 0) return "";
            sb.Insert(0, "共 " + diffs + " 处不同（最多列 " + MaxDiffLines + " 条）：\n");
            return sb.ToString();
        }

        /// <summary>把一份 JSON 摊平成 `路径 → 值的规范写法`（路径段用 JSON 引号包住，所以 `"a.b"` 与 `a.b`
        /// 这两种形状**不会**撞在一起）。排序 = 序数序，可逐字比。</summary>
        public static SortedDictionary<string, string> Flatten(string json, out string error)
        {
            error = null;
            SortedDictionary<string, string> into = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (json == null) { error = "内容是 null"; return into; }
            int i = 0;
            SkipWs(json, ref i);
            if (i >= json.Length) { error = "内容是空的"; return into; }
            if (!FlattenValue(json, ref i, "", into))
            {
                error = "解析失败（位置 " + i + " 附近：" + Head(json.Substring(Math.Min(i, json.Length)), 24) + "）";
                return into;
            }
            SkipWs(json, ref i);
            if (i < json.Length) { error = "结尾还有多余内容（位置 " + i + "）"; return into; }
            return into;
        }

        /// <summary>规范化后的整份文本（逐行 `路径 = 值`）。**两份 JSON 的这个字符串相等 ⟺ 逐字段相等。**</summary>
        public static string Canonical(string json, out string error)
        {
            SortedDictionary<string, string> map = Flatten(json, out error);
            if (error != null) return null;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in map)
                sb.Append(kv.Key).Append(" = ").AppendLine(kv.Value);
            return sb.ToString();
        }

        // ── 内部：递归下降 ─────────────────────────────────────────────────────────────────

        static bool FlattenValue(string s, ref int i, string path, SortedDictionary<string, string> into)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) return false;
            char c = s[i];
            if (c == '{') return FlattenObject(s, ref i, path, into);
            if (c == '[') return FlattenArray(s, ref i, path, into);
            string scalar;
            if (!ReadScalar(s, ref i, out scalar)) return false;
            into[path] = scalar;
            return true;
        }

        static bool FlattenObject(string s, ref int i, string path, SortedDictionary<string, string> into)
        {
            i++;                                                    // '{'
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; into[path] = "{}"; return true; }
            while (true)
            {
                SkipWs(s, ref i);
                string key;
                if (!ReadString(s, ref i, out key)) return false;
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') return false;
                i++;
                string child = path.Length == 0 ? Quote(key) : path + "." + Quote(key);
                if (!FlattenValue(s, ref i, child, into)) return false;
                SkipWs(s, ref i);
                if (i >= s.Length) return false;
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return true; }
                return false;
            }
        }

        static bool FlattenArray(string s, ref int i, string path, SortedDictionary<string, string> into)
        {
            i++;                                                    // '['
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; into[path] = "[]"; return true; }
            int idx = 0;
            while (true)
            {
                string child = path + "[" + idx.ToString(CultureInfo.InvariantCulture) + "]";
                if (!FlattenValue(s, ref i, child, into)) return false;
                idx++;
                SkipWs(s, ref i);
                if (i >= s.Length) return false;
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return true; }
                return false;
            }
        }

        static bool ReadScalar(string s, ref int i, out string canon)
        {
            canon = null;
            SkipWs(s, ref i);
            if (i >= s.Length) return false;
            char c = s[i];
            if (c == '"')
            {
                string v;
                if (!ReadString(s, ref i, out v)) return false;
                canon = Quote(v);
                return true;
            }
            if (c == 't') { if (!Match(s, ref i, "true")) return false; canon = "true"; return true; }
            if (c == 'f') { if (!Match(s, ref i, "false")) return false; canon = "false"; return true; }
            if (c == 'n') { if (!Match(s, ref i, "null")) return false; canon = "null"; return true; }
            if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber(s, ref i, out canon);
            return false;
        }

        static bool ReadNumber(string s, ref int i, out string canon)
        {
            canon = null;
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                bool ok = (c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E';
                if (!ok) break;
                i++;
            }
            string raw = s.Substring(start, i - start);
            if (raw.Length == 0) return false;
            double d;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return false;
            if (double.IsNaN(d) || double.IsInfinity(d)) return false;
            double r = Math.Round(d);
            if (Math.Abs(d - r) < 1e-9 && Math.Abs(d) < 1e15)
                canon = ((long)r).ToString(CultureInfo.InvariantCulture);
            else
                canon = d.ToString("R", CultureInfo.InvariantCulture);
            return true;
        }

        static bool ReadString(string s, ref int i, out string value)
        {
            value = null;
            if (i >= s.Length || s[i] != '"') return false;
            i++;
            StringBuilder sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') { value = sb.ToString(); return true; }
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) return false;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) return false;
                        int code;
                        if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) return false;
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default: return false;
                }
            }
            return false;
        }

        /// <summary>把字符串写成 JSON 字面量（只转义**必须**转义的：`"` `\` 与 `&lt;0x20` 的控制符）。
        /// ⚠️ 中文/emoji **原样保留** —— 转成 `\uXXXX` 会让差异清单没法读，而且两边同一套规则不影响相等判断。</summary>
        public static string Quote(string s)
        {
            if (s == null) return "null";
            StringBuilder sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\uFEFF') i++;
                else break;
            }
        }

        static bool Match(string s, ref int i, string word)
        {
            if (i + word.Length > s.Length) return false;
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
            i += word.Length;
            return true;
        }

        static string Head(string s, int n)
        {
            if (s == null) return "<null>";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }
}
