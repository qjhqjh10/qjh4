// WarpforgeEffectHook.cs — 把卡牌表现接到特效库上（**运行时会自动装**）
//
// 为什么需要它：`CardEffects.Play` 是个**静态委托**，而静态字段**不进场景、也不进 Play 模式**——
// 在编辑器建场景时接了也没用，按 Play 时它是 null，特效就只剩一行日志（踩过）。
// 所以用 `[RuntimeInitializeOnLoadMethod]` 在进 Play 时自己装上，不用场景里挂任何东西。
//
// 依赖方向：**特效库认识卡牌，卡牌不认识特效库**（`CardPresentation` 里没有一根
// `using WarpforgeVFX`，特效库不在时它照样跑）—— 所以这个文件放在特效这边。
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeEffectHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            // 🆕 2026-10-31（`B107_W24` · `FX-04`；接线 `B107_W29`）：实体抽成局部函数 —— 两个钩子共用同一份实现。
            //   `PlayInstance` 与 `Play` 的唯一区别 = 多返回一个实例根（根特效 `parent == null` 时
            //   `CardEffects` 靠 childCount 差认不出实例）。⛔ 不要让它俩各实例化一次（`FireOne` 里是 if/else）。
            GameObject Spawn(string name, Vector3 pos, Transform parent)
            {
                if (string.IsNullOrEmpty(name)) return null;
                if (WarpforgeEffectLibrary.Instance == null)
                {
                    Debug.LogWarning($"[WarpforgeVFX] 没有特效库，'{name}' 播不了");
                    return null;
                }
                // 🔴 **2026-10-22（W64 · `B17`）**：**落地扬尘**那一族在原版是
                //   `localScale = Vector3.one × desiredScale`（**赋值**，`CardScript__LandOnGround.c:87-106`）——
                //   不是「乘在 prefab 自带根缩放之上」。我们挂在卡视图上（父节点已经带 desiredScale）
                //   ⇒ 根缩放必须是 **1**；照「乘」写会让自带 0.9913 的那件大 0.9%。
                //   名字从 `BattleDriver` 那两枚常量取（判据只留一处，⛔ 别在这儿抄字面量）。
                bool assignScale = name == CardPresentation.BattleDriver.LandDustMinion
                                || name == CardPresentation.BattleDriver.LandDustLegendary;
                var fx = WarpforgeEffectPlayer.Play(name, parent, pos, 1f, -1f, assignScale);
                if (fx == null) { Debug.Log($"[WarpforgeVFX] 特效没播出来：{name} @ {pos}"); return null; }
                // 🔴 2026-11-02（执行代理 `B118_W22` · 账 `D118-75`）：**真机 trace 落盘口**。
                //   为什么必须在这儿：`S3VfxTrace`（`Editor/BattleScene.cs:51091`）挂在 `CardEffects.Play` 上，
                //   而**真机 player** 装的是 `PlayInstance`（下面那句）⇒ `FireOne` 走那条路、一条都记不到。
                //   本文件在**运行时程序集**（player 里有），所以补这一口。列序与 `S3VfxTrace.Columns`
                //   **逐字一致**（`cardId / evt / fx / t / x / y / z / parentLayer`），下游 join 才对得上。
                //   ⚠️ `pos` 在 `parent != null` 时是**局部坐标**（`CardEffects.FireGet` 的约定）⇒ 这里换回世界点。
                //   ✅ **记在 `fx != null` 之后**（= 真的建出了实例）—— 这一条与 Editor 那份**不同**（那份记的是
                //      「钩子被调」）：验收要的是「播没播」，把「库里查不到这件」也记成一行会把缺口误判成已覆盖。
                //      播不出来时那句 `[WarpforgeVFX] 特效没播出来` 仍然照打（⛔ 不静默）。
                //   默认关：环境变量 `WF_S3_TRACE=1` 才开；关着时 `Capture` 第一句就 return、一个字节都不写。
                S3Trace.Capture(name,
                                parent != null ? parent.TransformPoint(pos) : pos,
                                parent != null ? parent.gameObject.layer : -1);

                // 🆕 2026-10-16：**实例跟随父节点的层**（`parent != null` 时才管）。
                //   为什么需要：原版那些「挂在卡 / 棋盘上的特效」（落地扬尘、逐卡演出…）是由**透视战场相机**画的
                //   —— 它们跟卡活在同一层（`ArenaLayer`）。而 **Unity 的层不继承**：
                //   `Instantiate(prefab, parent)` 保的是 **prefab 自己的层**（我们那批是 0）⇒
                //   不补这一步，挂在卡上的特效会停在层 0、被 HUD 的正交相机画 ⇒ **位置和大小的量纲全错**。
                //   ⚠️ `parent = null` 的**根特效**不走这一步 —— 它们由谁画是另一笔账（`A367`），
                //     那一路的位置判据是「投到 HUD 平面」（见 `BoardLayout.DropTargetWorld`）。
                // 🆕 2026-10-25（`B14` · `X10`）：这一句**会盖掉 `Play` 里刚设的层** ⇒ 必须同样过
                //   `GrabScreen501.FinalLayer`（否则失真效果被搬去 501 层之后又被这里搬回父层，
                //   501 那一步就什么都画不到；开关关着时它原样返回父层，与改前同值）。
                if (parent != null)
                    SetLayerRec(fx.gameObject,
                                GrabScreen501.FinalLayer(fx.gameObject, parent.gameObject.layer));
                return fx.gameObject;
            }

            CardPresentation.CardEffects.Play = (name, pos, parent) => { Spawn(name, pos, parent); };
            CardPresentation.CardEffects.PlayInstance = Spawn;
        }

        /// <summary>把这个实例整棵子树设到某一层（层**不会**从父节点继承，必须自己走一遍）。</summary>
        static void SetLayerRec(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRec(t.GetChild(i).gameObject, layer);
        }

        // ====================================================================================
        //  🆕 2026-11-02（执行代理 `B118_W22` · 账 `D118-75`）：**运行时逐卡特效 trace（默认关）**
        // ====================================================================================
        //  **为什么有第二份**（与 `CardPresentation/Editor/BattleScene.cs:51091` 的 `S3VfxTrace` 成对）：
        //    那份在 **Editor 程序集**（`Editor/` 下），**进不了 player**；而真机走的是本文件装的
        //    `CardEffects.PlayInstance` ⇒ `FireOne` 一落地就绕开了 `Play`。两处各写一份是**程序集边界**逼的，
        //    ⛔ 不是「两处写同一条规则」—— 落盘格式（文件名 / 列序 / 开关名）**必须逐字相同**，
        //    改一处就要改另一处（`Editor/B116W42_Assert.cs` 的 G1 段钉的是 Editor 那份的列序）。
        //  **判据**：`判据/B108_ID01_S3_表现维.md` §5.2 的 `S3-G1`（原话：「把 `(cardId/evt, name, world,
        //    parentLayer, clock)` 追加到 `_tmp_view/s3_vfx_trace.tsv`」）。
        //  **默认零影响**：`Enabled` 假 ⇒ `Capture` 第一句就 `return` —— 不建目录、不建文件、不写一个字节
        //    （✅ 自检钉了这条：关着时文件长度 0）。
        //  **卡 / 事件两列从哪来**：`BattleDriver.CurrentVfxEvent`（**最近一条**卡级事件）+ `PresentationClock`，
        //    与 Editor 那份**同一个口径**（它同样是「最近一条」，非卡级特效会带残留值 —— 如实记，⛔ 别当因果）。
        //    ⚠️ 运行时拿不到那个实例的静态引用 ⇒ 用 `FindFirstObjectByType` **按需缓存一次**
        //    （先例 `TapParticleController.cs:130`）；拿不到（不在战斗场景里）⇒ 两列空、`t = 0`，且**只出声一次**。
        // ====================================================================================
        public static class S3Trace
        {
            /// <summary>开关的环境变量名（与 `S3VfxTrace.EnvVar` 必须相同）。</summary>
            public const string EnvVar = "WF_S3_TRACE";
            /// <summary>落盘文件名（在 `<工程根>/_tmp_view/` 下；与 `S3VfxTrace.FileName` 必须相同）。</summary>
            public const string FileName = "s3_vfx_trace.tsv";
            /// <summary>列名（顺序即 TSV 的列序）—— 🔴 **逐字照抄** `S3VfxTrace.Columns`，⛔ 别改顺序。</summary>
            public static readonly string[] Columns =
            {
                "cardId", "evt", "fx", "t", "x", "y", "z", "parentLayer",
            };
            /// <summary>表头行（不含换行）。</summary>
            public static string HeaderLine { get { return string.Join("\t", Columns); } }

            static int _envCached = -1;         // -1 = 还没问过；0 = 关；1 = 开
            static bool _testOverride;          // 自检强开/强关（⛔ 生产不设）
            static bool _testValue;
            static string _path;                // 本会话正在写的文件（null = 还没写过）
            static int _rows;                   // 已写的数据行（不含表头）
            static bool _headerChecked;         // 首行写过没有（文件已存在且非空 ⇒ 只追加、不重写表头）
            static bool _ioErrorReported;
            static bool _noDriverReported;
            static CardPresentation.BattleDriver _drv;

            /// <summary>环境变量是不是要求开 trace（现读，不缓存 —— 便于自检改环境变量）。</summary>
            public static bool EnvWantsTrace()
            {
                try
                {
                    string v = System.Environment.GetEnvironmentVariable(EnvVar);
                    if (string.IsNullOrEmpty(v)) return false;
                    v = v.Trim();
                    return v == "1" || string.Equals(v, "true", System.StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }

            /// <summary>现在开不开（自检强开关优先；否则读环境变量，结果缓存）。</summary>
            public static bool Enabled
            {
                get
                {
                    if (_testOverride) return _testValue;
                    if (_envCached < 0) _envCached = EnvWantsTrace() ? 1 : 0;
                    return _envCached == 1;
                }
            }

            /// <summary>本会话正在写的文件（`null` = 关着 / 还没写第一行）。</summary>
            public static string FilePath { get { return _path; } }
            /// <summary>本会话已写的数据行数（不含表头）。</summary>
            public static int Rows { get { return _rows; } }

            /// <summary>`_tmp_view` 目录（= `<工程根>/_tmp_view`）。⚠️ **只是算路径** —— 关着时不会建它。</summary>
            public static string DirPath
            {
                get
                {
                    try
                    {
                        string data = Application.dataPath;
                        if (string.IsNullOrEmpty(data)) return null;
                        return System.IO.Path.GetFullPath(
                            System.IO.Path.Combine(data, "..", "..", "..", "_tmp_view"));
                    }
                    catch { return null; }
                }
            }

            /// <summary>默认落盘路径（`DirPath` + `FileName`）；算不出来 = `null`。</summary>
            public static string DefaultPath
            {
                get
                {
                    string d = DirPath;
                    return string.IsNullOrEmpty(d) ? null : System.IO.Path.Combine(d, FileName);
                }
            }

            /// <summary>记一行（**关着 = 立即 return、零副作用**）。只给 `Spawn` 用 —— 卡 / 事件 / 时钟自己解。</summary>
            public static void Capture(string fx, Vector3 world, int parentLayer)
            {
                if (!Enabled) return;
                string cardId = null, evt = null;
                float t = 0f;
                var d = Driver;
                if (d != null)
                {
                    var ev = d.CurrentVfxEvent;
                    if (ev != null) { cardId = ev.CardId; evt = ev.Kind.ToString(); }
                    t = d.PresentationClock;
                }
                else if (!_noDriverReported)
                {
                    _noDriverReported = true;
                    Debug.Log($"[S3Trace] 这一帧没找到 `BattleDriver` ⇒ `cardId`/`evt` 两列留空、`t` 记 0"
                            + "（特效照样播；只是 trace 少了「这一拍属于哪张卡」那两列）。");
                }
                Capture(fx, world, parentLayer, cardId, evt, t);
            }

            /// <summary>记一行（参数顺序 = `S3VfxTrace.Capture` 的**形参**顺序，⛔ 别按列序传）。</summary>
            public static void Capture(string fx, Vector3 world, int parentLayer, string cardId, string evt, float t)
            {
                if (!Enabled) return;      // 🔴 关着时**第一句**就 return：不建目录、不建文件、不写一个字节
                try
                {
                    string path = EnsurePath();
                    if (path == null) return;
                    if (!_headerChecked)
                    {
                        // 文件已经有人写过（Editor 那份 / 上一局）⇒ **只追加、不重写表头**（否则会多一行列名）
                        _headerChecked = true;
                        bool hasContent = System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length > 0;
                        if (!hasContent) FileAppend(path, HeaderLine + "\n");
                    }
                    _rows++;
                    FileAppend(path,
                        Clean(cardId) + "\t" + Clean(evt) + "\t" + Clean(fx) + "\t"
                        + t.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\t"
                        + world.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\t"
                        + world.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\t"
                        + world.z.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\t"
                        + parentLayer.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
                }
                catch (System.Exception e)
                {
                    if (_ioErrorReported) return;
                    _ioErrorReported = true;
                    Debug.LogError($"[S3Trace] 写 trace 失败（本会话不再重复出声）：{e.Message}｜文件 = `{_path}`");
                }
            }

            static string EnsurePath()
            {
                if (!string.IsNullOrEmpty(_path)) return _path;
                string p = DefaultPath;
                if (string.IsNullOrEmpty(p)) return null;
                string d = System.IO.Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(d)) System.IO.Directory.CreateDirectory(d);
                _path = p;
                return _path;
            }

            static void FileAppend(string path, string line)
            {
                if (string.IsNullOrEmpty(path)) return;
                System.IO.File.AppendAllText(path, line, new System.Text.UTF8Encoding(false));
            }

            /// <summary>字段清洗：tab / CR / LF 一律换成空格（否则一列会被撑成两列）—— 与 Editor 那份同一个口径。</summary>
            static string Clean(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                if (s.IndexOf('\t') < 0 && s.IndexOf('\r') < 0 && s.IndexOf('\n') < 0) return s;
                return s.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            }

            static CardPresentation.BattleDriver Driver
            {
                get
                {
                    if (_drv != null) return _drv;
                    try { _drv = Object.FindFirstObjectByType<CardPresentation.BattleDriver>(FindObjectsInactive.Include); }
                    catch { _drv = null; }
                    return _drv;
                }
            }

            /// <summary>关掉当前文件（换文件 / 自检读回之前用）。之后再 `Capture` ⇒ 同一路径**续写**（不重写表头）。</summary>
            public static void Close()
            {
                _path = null;
                _headerChecked = (_rows > 0);
            }

            /// <summary>自检专用：强开 / 强关。<paramref name="path"/> 非空 = 写到那个文件。</summary>
            public static void SetEnabledForTest(bool on, string path = null)
            {
                _testOverride = true;
                _testValue = on;
                _path = on ? path : null;
                _rows = 0;
                _headerChecked = false;
                _ioErrorReported = false;
                _noDriverReported = false;
            }

            /// <summary>自检专用：撤掉强开 / 强关，回到「看环境变量」那一档（同时让环境变量缓存失效）。</summary>
            public static void ClearTestOverride()
            {
                _testOverride = false;
                _testValue = false;
                _envCached = -1;
                _path = null;
                _rows = 0;
                _headerChecked = false;
                _ioErrorReported = false;
                _noDriverReported = false;
            }
        }
    }
}
