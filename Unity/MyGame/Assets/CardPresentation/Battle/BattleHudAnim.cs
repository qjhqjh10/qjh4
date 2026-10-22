// BattleHudAnim.cs — 换牌期那 8 条原版 `BattleHud*` legacy clip 的**运行期播放器**。
//
// ─────────────────────────────────────────────────────────────────────────────
// 【原版语义 · 判据链】（全文 → `资料/普查产出_1022/E-C_BattleHud8条.md`）
//   换牌期整条战斗 HUD 是**滑出屏幕**、不是压暗：
//     · 进换牌期：`BattleHudHiddenNoHand`（Instant，0 s）
//     · 换牌收尾：`BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:113`
//       = `BattleHud.ToggleWithAnimation(hud, 1, 1, 0)` ⇒ `BattleHudShow`（1 s 补间）
//     · 战斗结束：`_CloseBattleDoors_d__393__MoveNext.c:29` = `(0,1,1)` ⇒ `BattleHudHideNoHand`（1 s）
//   8 条全是 legacy 曲线（本地只有一份，13 场共引用）⇒ 运行期用 `AnimationClip.SetCurve` 重建
//   （同 `Battle/CameraIntro.cs:387-444`），数据来自 `Resources/battlehud_clips.json`
//   （由 `工具/gen_battlehud_clips.py` 生成；全量件另存 `Unity/数据/游戏数据/battlehud_clips.json`）。
//
// 【🔴 为什么落盘量是「增量」而不是 clip 里的绝对值】（E-C §3·0，2026-10-22）
//   clip 里存的是**原版那棵树里组节点的绝对 `m_AnchoredPosition`**；我们的 HUD 是**摊平**的
//   （`_hudLayer` 直子 + 归一化锚点，件的原点是**画布**），而每个件对应的只是原版组节点里的**某一个子件**。
//   直接把绝对值写上去 = 把整块 HUD 搬到原版那棵树的位置（整体错位）。
//   ⇒ 本文件施加的是 `delta(t) = key(t) − reference.shown`
//     （`reference.shown` = `BattleHudShow` 该曲线的**末键**，由生成器写进 JSON `refs[]`）。
//     于是 `t=1`（显示态）**全部 delta = 0** ⇒ 回到我们自己的显示基准；`t=0` = 原版的隐藏位移。
//   ⚠️ 原版 `m_PlayAutomatically = True` + 默认 clip `BattleHudHiddenNoHand` ⇒ 原版**一加载就被 clip 覆盖**，
//      所以「显示态 = clip 末键」而不是 prefab 的序列化 rest。场景 rest 与 clip 末键**不同**的两处
//      （`PlayerInfo.x` 31.7002 vs 50 · `SettingsBtn.x` −48.1001 vs −54）是记账项（E-C §2·2），
//      由**显示基准**那一侧去修；⛔ 别在这里拿 rest 当 reference（那会让 t=0 的位移量错 18.3 / 5.9 px）。
//
// 【rig 节点 · 「搬家零位移」】
//   原版 9 个落点各建一颗**全屏 stretch `RectTransform`**（`HudCanvas.FillParent`：与 `_hudLayer` 逐位同尺），
//   把件搬进去 ⇒ 搬家本身**一个像素都不动**（全屏 + 单位变换 ⇒ 局部/世界坐标不变）。
//   之后给 rig 的 `anchoredPosition` 写 delta，就等价于原版那个**组节点**的位移。
//   ✅ 搬家前后逐件比 `transform.position`（`MoveMaxDeltaWorld`，自检断言 ≤ 1e-5）。
//   ⚠️ 搬完要**按原来的 z 重新登记**（`HudCanvas.Add`）—— 否则 `HudCanvas.OrderZOf` 从旧的父件组里读不到，
//      返回 `NaN`，`Editor/BattleScene.cs` 里那几条 z 序断言（SDF vs 卡背 / 骷髅 / 头衔底条）
//      会**静默变成 NaN 比较**。
//
// 【两个「不是 uGUI 件」的落点】
//   `BottomAnchor/PlayerArea/HandArea` 与 `UpperAnchor/EnemyArea` 在原版是**纯 Transform 组**，
//   我们这一侧手牌是**世界空间**摆的（`HandLayout.SlotPosition`）⇒ 这两条只暴露一个**像素位移**，
//   由 `BattleDriver` 写进 `HandLayout.areaOffsetY`（归一化 = px/1080）后重摆那两排。
//
// 【瞬时 vs 补间 · 批处理】
//   原版只有 4 条 `useAnimation=1` 带 1 s 补间；`Hidden` / `HiddenNoHand` / `ShowInstant*` 都是 0 s。
//   🔴 本仓的批处理/编辑器非播放态**没有帧循环**（`Time.deltaTime = 0`）⇒ `Play()` 在那一档**直接落到终态**
//      （同 `BattleDriver.BuildLoadingCover` 的先例）。自检要逐帧验补间就显式 `SnapTo(name, t)`。
//
// 【⛔ 别做的事】
//   · 别对重建出来的 clip 调 `SampleAnimation` 去驱动 rig —— 它写的是**绝对值**（见上面那条）。
//     `SampleForTest` 只留给诊断（校验曲线确实能绑上）。
//   · 别把 delta 写成绝对值 / 别把 `shown` 换成场景 rest（两处原因各一条，见上）。
//   · 别按名字 `Transform.Find("LeftArea/PlayerInfo")` 找 rig —— 名字里的 `/` 会被当路径。
//     一律走 `Rig(node)` 字典。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ---- JSON schema（与 `工具/gen_battlehud_clips.py` 的「运行期副本」逐字对应）----
    [Serializable]
    public class BattleHudKey { public float t; public float[] v; public float[] si; public float[] so; }

    [Serializable]
    public class BattleHudCurve
    {
        public string node;
        public string path;
        public string type;          // "RectTransform" | "Transform"
        public string property;      // "m_AnchoredPosition.x" / ".y" / "m_LocalPosition"
        public BattleHudKey[] keys;
    }

    [Serializable]
    public class BattleHudClip
    {
        public string name;
        public bool legacy;
        public int muscleClipSize;
        public float sampleRate;
        public string wrapMode;
        public float length;
        public BattleHudCurve[] curves;
    }

    [Serializable]
    public class BattleHudRef
    {
        public string node;
        public string property;
        public string type;
        public float[] hidden;
        public float[] shown;
    }

    [Serializable]
    public class BattleHudFile
    {
        public int schema;
        public string note;
        public string generated;
        public string pathPrefix;
        public string clipsHash;
        public BattleHudClip[] clips;
        public BattleHudRef[] refs;
    }

    /// <summary>换牌期 8 条 `BattleHud*` 的运行期播放器。**判据 / 坑 → 文件头**；测试口见类末。</summary>
    public class BattleHudAnim : MonoBehaviour
    {
        /// <summary>`Resources.Load&lt;TextAsset&gt;` 的路径（与 `CameraIntro` 同惯例）。</summary>
        public const string ResourcePath = "battlehud_clips";

        // ---- 8 条的名字（原版 `Animation.m_Animations` 的固定顺序；13 场逐场现核一致）----
        /// <summary>进换牌期（Instant，0 s；**不含** HandArea / EnemyArea）。</summary>
        public const string HiddenNoHandClip = "BattleHudHiddenNoHand";
        /// <summary>换牌收尾（1 s 补间；9 个落点全含）。</summary>
        public const string ShowClip = "BattleHudShow";
        /// <summary>战斗结束（1 s 补间；不含手牌区两处）。</summary>
        public const string HideNoHandClip = "BattleHudHideNoHand";
        /// <summary>教程开局（1 s；`_TutorialStartSequence...c:283` = `(1,1,1)`）。⚠️ 我们**还没接**调用点。</summary>
        public const string ShowNoHandClip = "BattleHudShowNoHand";

        // ---- 两个「不是 uGUI 件」的落点（JSON 的 node）----
        /// <summary>原版 `BottomAnchor/PlayerArea/HandArea`（我方手牌区，纯 `Transform`，`m_LocalPosition.y`）。</summary>
        public const string HandAreaNode = "BottomAnchor/PlayerArea/HandArea";
        /// <summary>原版 `UpperAnchor/EnemyArea`（敌方手牌区，`RectTransform.m_AnchoredPosition.y`）。</summary>
        public const string EnemyAreaNode = "UpperAnchor/EnemyArea";

        // ---- 表缓存（静态：全局只有一份）----
        static BattleHudFile _file;
        static Dictionary<string, BattleHudClip> _byName;
        static Dictionary<string, BattleHudRef> _byRef;
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>表载进来了没有（没载 = 资源不在；<see cref="LastError"/> 有话说）。</summary>
        public static bool Loaded { get { EnsureLoaded(); return _file != null && _file.clips != null && _file.clips.Length > 0; } }
        /// <summary>表里几条（原版 = 8）。</summary>
        public static int ClipCount { get { EnsureLoaded(); return _file != null && _file.clips != null ? _file.clips.Length : 0; } }
        /// <summary>表里第 i 条的名字（越界 ⇒ null）。</summary>
        public static string ClipNameAt(int i)
        {
            EnsureLoaded();
            if (_file == null || _file.clips == null || i < 0 || i >= _file.clips.Length) return null;
            return _file.clips[i].name;
        }
        /// <summary>载表/解析失败的原因（无错 = null）。</summary>
        public static string LastError { get; private set; }

        static void EnsureLoaded()
        {
            if (_file != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                          + "`python -I -X utf8 工具/gen_battlehud_clips.py`";
                _file = new BattleHudFile { clips = new BattleHudClip[0], refs = new BattleHudRef[0] };
                _byName = new Dictionary<string, BattleHudClip>();
                _byRef = new Dictionary<string, BattleHudRef>();
                return;
            }
            BattleHudFile f = null;
            try { f = JsonUtility.FromJson<BattleHudFile>(ta.text); }
            catch (Exception e) { LastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message; }
            var clips = f != null && f.clips != null ? f.clips : new BattleHudClip[0];
            var refs = f != null && f.refs != null ? f.refs : new BattleHudRef[0];
            _file = new BattleHudFile
            {
                schema = f != null ? f.schema : 0,
                note = f != null ? f.note : null,
                generated = f != null ? f.generated : null,
                pathPrefix = f != null ? f.pathPrefix : null,
                clipsHash = f != null ? f.clipsHash : null,
                clips = clips,
                refs = refs,
            };
            _byName = new Dictionary<string, BattleHudClip>(clips.Length);
            foreach (var c in clips)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                if (!_byName.ContainsKey(c.name)) _byName[c.name] = c;
            }
            _byRef = new Dictionary<string, BattleHudRef>(refs.Length);
            foreach (var r in refs)
            {
                if (r == null || string.IsNullOrEmpty(r.node)) continue;
                var k = RefKey(r.node, r.property);
                if (!_byRef.ContainsKey(k)) _byRef[k] = r;
            }
            if (LastError == null && clips.Length == 0)
                LastError = "`" + ResourcePath + ".json` 里一条 clip 都没有";
        }

        static string RefKey(string node, string property) { return node + "|" + property; }

        /// <summary>表里某条 clip 的**原版时长**（秒）；没有 = −1。
        /// ⚠️ 读的是**重建出来的 `AnimationClip.length`**（不是 JSON 字段）——`SetCurve` 若绑不上，
        /// 曲线就进不去、`length` 会变 0 ⇒ 这条口本身就是「曲线真的进得去」的判据。</summary>
        public static float LengthOf(string name)
        {
            var lc = LoadClip(name);
            return lc != null && lc.clip != null ? lc.clip.length : -1f;
        }

        /// <summary>表里某条 clip 的**原版** `m_AnchoredPosition` 末键（诊断/自检用，**绝对值**）。
        /// `property` = `"m_AnchoredPosition.x"` 之类；没有这条 ⇒ `NaN`。</summary>
        public static float RawLastKey(string clipName, string node, string property)
        {
            var c = FindClip(clipName);
            if (c == null || c.curves == null) return float.NaN;
            for (int i = 0; i < c.curves.Length; i++)
            {
                var cv = c.curves[i];
                if (cv == null || cv.node != node || cv.property != property) continue;
                if (cv.keys == null || cv.keys.Length == 0) return float.NaN;
                var last = cv.keys[cv.keys.Length - 1];
                int comp = CompOf(cv.property, cv.keys[0]);
                return last.v != null && comp < last.v.Length ? last.v[comp] : float.NaN;
            }
            return float.NaN;
        }

        /// <summary>某条 (node, property) 的**显示态参考值**（`BattleHudShow` 的末键）。没有 ⇒ `NaN`。</summary>
        public static float RefShown(string node, string property)
        {
            EnsureLoaded();
            BattleHudRef r;
            if (_byRef == null || !_byRef.TryGetValue(RefKey(node, property), out r) || r == null) return float.NaN;
            int comp = property == "m_LocalPosition" ? 1 : 0;
            return r.shown != null && comp < r.shown.Length ? r.shown[comp] : float.NaN;
        }

        static BattleHudClip FindClip(string name)
        {
            EnsureLoaded();
            if (_byName == null || string.IsNullOrEmpty(name)) return null;
            BattleHudClip c;
            return _byName.TryGetValue(name, out c) ? c : null;
        }

        // ---- 重建出来的 clip 缓存（每条一份：`AnimationClip` + 逐曲线的运行期求值器）----
        sealed class LoadedClip
        {
            public BattleHudClip d;
            public AnimationClip clip;
            public AnimationCurve[] eval;   // 与 d.curves 一一对应；求的是**该曲线要施加的那个分量**
            public float[] shown;           // 同上：显示态参考值（已取分量）
            public bool[] isHand, isEnemy;
        }
        static readonly Dictionary<string, LoadedClip> _loaded = new Dictionary<string, LoadedClip>();

        /// <summary>该曲线在 `v`/`shown` 里取**哪一个分量**：
        /// `m_LocalPosition` 是**三分量**（取 y = 1，另两轴原版不动）；float 曲线只有一个分量（0）。</summary>
        static int CompOf(string property, BattleHudKey k)
        {
            if (property == "m_LocalPosition" && k != null && k.v != null && k.v.Length >= 3) return 1;
            return 0;
        }

        /// <summary>按表里一条数据建 `AnimationClip`（照 `CameraIntro.BuildClip` 那套）。
        /// ⚠️ 回放路径**不**用它采样（它写绝对值），只用它的 `length` 当「曲线真进得去」的判据。</summary>
        static AnimationClip BuildClip(BattleHudClip d)
        {
            if (d == null) return null;
            var clip = new AnimationClip
            {
                name = d.name,
                legacy = true,
                frameRate = d.sampleRate > 0f ? d.sampleRate : 60f,
                wrapMode = WrapMode.Once,          // 原版 `m_WrapMode = 1`
            };
            if (d.curves == null) return clip;
            for (int i = 0; i < d.curves.Length; i++)
            {
                var c = d.curves[i];
                if (c == null || c.keys == null || c.keys.Length == 0) continue;
                // 三个分量各写一条（`m_LocalPosition`）；float 曲线只写被点名的那一轴。
                for (int comp = 0; comp < 3; comp++)
                {
                    if (c.property != "m_LocalPosition" && comp > 0) break;
                    var curve = BuildCurve(c.keys, comp);
                    string prop;
                    Type ty;
                    if (c.property == "m_LocalPosition")
                    {
                        ty = typeof(Transform);
                        prop = "m_LocalPosition." + "xyz"[comp];
                    }
                    else if (c.property == "m_AnchoredPosition.x")
                    {
                        ty = typeof(RectTransform); prop = "m_AnchoredPosition.x";
                    }
                    else if (c.property == "m_AnchoredPosition.y")
                    {
                        ty = typeof(RectTransform); prop = "m_AnchoredPosition.y";
                    }
                    else continue;                       // 未知属性：不猜（下面出声）
                    clip.SetCurve(c.path != null ? c.path : "", ty, prop, curve);
                }
                if (c.property != "m_LocalPosition"
                    && c.property != "m_AnchoredPosition.x" && c.property != "m_AnchoredPosition.y")
                {
                    if (_warned.Add("prop:" + c.property))
                        Debug.LogError("[BattleHudAnim] 表里出现没见过的属性 `" + c.property
                                     + "`（" + d.name + "）⇒ 这一条**没写进 clip**（⛔ 不按名字猜绑定）");
                }
            }
            return clip;
        }

        /// <summary>逐键 `new Keyframe(t, v, inTangent, outTangent)`（斜率原样写回；本表全 0 ⇒ smoothstep）。</summary>
        static AnimationCurve BuildCurve(BattleHudKey[] keys, int comp)
        {
            var ks = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                float v = k.v != null && comp < k.v.Length ? k.v[comp] : 0f;
                float it = k.si != null && comp < k.si.Length ? k.si[comp] : 0f;
                float ot = k.so != null && comp < k.so.Length ? k.so[comp] : 0f;
                ks[i] = new Keyframe(k.t, v, it, ot);
            }
            return new AnimationCurve(ks);
        }

        static LoadedClip LoadClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            LoadedClip lc;
            if (_loaded.TryGetValue(name, out lc)) return lc;
            var d = FindClip(name);
            if (d == null) return null;
            lc = new LoadedClip { d = d, clip = BuildClip(d) };
            int n = d.curves != null ? d.curves.Length : 0;
            lc.eval = new AnimationCurve[n];
            lc.shown = new float[n];
            lc.isHand = new bool[n];
            lc.isEnemy = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var c = d.curves[i];
                int comp = c != null && c.keys != null && c.keys.Length > 0 ? CompOf(c.property, c.keys[0]) : 0;
                lc.eval[i] = c != null && c.keys != null && c.keys.Length > 0 ? BuildCurve(c.keys, comp) : null;
                lc.isHand[i] = c != null && c.node == HandAreaNode;
                lc.isEnemy[i] = c != null && c.node == EnemyAreaNode;
                float shown = c != null ? RefShown(c.node, c.property) : float.NaN;
                if (float.IsNaN(shown))
                {
                    shown = 0f;
                    if (c != null && _warned.Add("ref:" + RefKey(c.node, c.property)))
                        Debug.LogError("[BattleHudAnim] 表里缺显示态参考值 `" + RefKey(c.node, c.property)
                                     + "`（" + name + "）⇒ 该条按 0 处理（**位移量会错**）");
                }
                lc.shown[i] = shown;
            }
            _loaded[name] = lc;
            return lc;
        }

        /// <summary>建（幂等）：在 `host` 上挂组件、在 HUD 那一层下建 9 颗 rig。失败 ⇒ null（并出声）。</summary>
        public static BattleHudAnim Attach(GameObject host, RectTransform hudLayer)
        {
            if (host == null || hudLayer == null)
            {
                Debug.LogError("[BattleHudAnim] `host` / `hudLayer` 为空 ⇒ **动画播放器建不出来**"
                             + "（HUD 退回 `CanvasGroup.alpha` 硬切）");
                return null;
            }
            EnsureLoaded();
            if (LastError != null)
                Debug.LogError("[BattleHudAnim] " + LastError);

            var anim = host.GetComponent<BattleHudAnim>();
            if (anim == null) anim = host.AddComponent<BattleHudAnim>();
            anim._layer = hudLayer;
            anim.BuildRigs(hudLayer);
            return anim;
        }

        // ---- 实例状态（全部 `[NonSerialized]`：本组件是**运行时**加的，⛔ 不进场景存档）----
        [NonSerialized] RectTransform _layer;
        [NonSerialized] readonly Dictionary<string, RectTransform> _rigs = new Dictionary<string, RectTransform>();
        [NonSerialized] readonly Dictionary<string, Vector2> _off = new Dictionary<string, Vector2>();
        [NonSerialized] readonly List<string> _rigOrder = new List<string>();
        [NonSerialized] LoadedClip _cur;
        [NonSerialized] float _t;
        [NonSerialized] float _len;
        [NonSerialized] bool _playing;
        [NonSerialized] float _handPx, _enemyPx;
        [NonSerialized] bool _built;

        /// <summary>搬家前后世界坐标的最大偏差（世界单位）—— 「全屏 + 单位变换 ⇒ 零位移」的**实测见证**。</summary>
        [NonSerialized] float _moveMaxDelta;
        public float MoveMaxDeltaWorld { get { return _moveMaxDelta; } }

        /// <summary>当前播的 clip 名（没播过 = null）。</summary>
        public string CurrentClipName { get { return _cur != null ? _cur.d.name : null; } }
        /// <summary>当前 clip 的归一化时间（0~1；没播过 = 0）。</summary>
        public float Time01 { get { return _cur == null || _cur.d.length <= 1e-6f ? 0f : Mathf.Clamp01(_t / _cur.d.length); } }
        /// <summary>当前 clip 的秒数（0~len）。</summary>
        public float TimeSeconds { get { return _t; } }
        /// <summary>还在播（补间没跑完）。</summary>
        public bool IsPlaying { get { return _playing; } }
        /// <summary>我方手牌区（`HandArea`）当前该施加的 **y 位移（设计 px，+ = 向上）**。</summary>
        public float HandAreaOffsetPx { get { return _handPx; } }
        /// <summary>敌方手牌区（`EnemyArea`）当前该施加的 **y 位移（设计 px，+ = 向上）**。</summary>
        public float EnemyAreaOffsetPx { get { return _enemyPx; } }
        /// <summary>rig 颗数（原版 9 个落点）。</summary>
        public int RigCount { get { return _rigOrder.Count; } }
        /// <summary>第 i 颗 rig 的 node（越界 ⇒ null）。</summary>
        public string RigNodeAt(int i) { return i >= 0 && i < _rigOrder.Count ? _rigOrder[i] : null; }
        /// <summary>某颗 rig 当前的 `anchoredPosition`（没有 ⇒ `NaN`）。</summary>
        public Vector2 RigAnchoredPosition(string node)
        {
            RectTransform rt;
            if (node == null || !_rigs.TryGetValue(node, out rt) || rt == null) return new Vector2(float.NaN, float.NaN);
            return rt.anchoredPosition;
        }
        /// <summary>第 i 颗 rig 当前的 `anchoredPosition`（越界 ⇒ `NaN`）。</summary>
        public Vector2 RigAnchoredPositionAt(int i) { return RigAnchoredPosition(RigNodeAt(i)); }
        /// <summary>某颗 rig 的节点（自检诊断用；没有 ⇒ null）。</summary>
        public RectTransform Rig(string node)
        {
            RectTransform rt;
            return node != null && _rigs.TryGetValue(node, out rt) ? rt : null;
        }
        /// <summary>rig 建出来了没有（前提探针）。</summary>
        public bool Built { get { return _built && _rigs.Count > 0; } }

        /// <summary>9 颗全屏 rig（`HudCanvas.FillParent` ⇒ 与 `_hudLayer` 逐位同尺）。**幂等**。</summary>
        void BuildRigs(RectTransform layer)
        {
            if (_built && _layer == layer) return;
            _built = true;
            _layer = layer;
            _rigs.Clear();
            _rigOrder.Clear();
            _off.Clear();
            EnsureLoaded();
            var seen = new HashSet<string>();
            if (_file != null && _file.clips != null)
            {
                for (int ci = 0; ci < _file.clips.Length; ci++)
                {
                    var c = _file.clips[ci];
                    if (c == null || c.curves == null) continue;
                    for (int i = 0; i < c.curves.Length; i++)
                    {
                        var cv = c.curves[i];
                        if (cv == null || string.IsNullOrEmpty(cv.node)) continue;
                        if (!seen.Add(cv.node)) continue;
                        var go = new GameObject(cv.node, typeof(RectTransform));
                        go.transform.SetParent(layer, false);
                        var rt = (RectTransform)go.transform;
                        HudCanvas.FillParent(rt);          // 全屏 stretch + 单位变换（见文件头「搬家家位移」）
                        rt.anchoredPosition = Vector2.zero;
                        _rigs[cv.node] = rt;
                        _rigOrder.Add(cv.node);
                        _off[cv.node] = Vector2.zero;
                    }
                }
            }
            if (_rigs.Count == 0)
                Debug.LogError("[BattleHudAnim] 一颗 rig 都没建出来（表里没有曲线？）⇒ 换牌动画不会生效");
        }

        /// <summary>把件搬进某颗 rig。**按原兄弟序搬 + 按原 z 重新登记**（见文件头）。
        /// 返回搬进去的件数；`node` 没有 rig ⇒ 0（并出声）。</summary>
        public int MoveInto(string node, params Transform[] pieces)
        {
            RectTransform rig = Rig(node);
            if (rig == null)
            {
                Debug.LogError("[BattleHudAnim] 没有 rig `" + node + "` ⇒ 这一批件**没搬**（会留在动画外面）");
                return 0;
            }
            var list = new List<Transform>();
            if (pieces != null)
                for (int i = 0; i < pieces.Length; i++) if (pieces[i] != null) list.Add(pieces[i]);
            // 按**当前兄弟序**升序搬（= 原来在 `_hudLayer` 里的 z 降序）⇒ 相对次序逐件保真
            list.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t.parent == rig) { n++; continue; }
                float z = HudCanvas.OrderZOf(t as RectTransform);
                Vector3 before = t.position;
                t.SetParent(rig, false);                 // 保留局部变换 ⇒ rig 是全屏单位件 ⇒ 世界坐标不变
                Vector3 after = t.position;
                float d = Vector3.Distance(before, after);
                if (d > _moveMaxDelta) _moveMaxDelta = d;
                if (float.IsNaN(z))
                {
                    if (_warned.Add("z:" + t.name))
                        Debug.LogError("[BattleHudAnim] `" + t.name + "` 搬家前**没有 z 档**（`HudCanvas.OrderZOf` = NaN）"
                                     + " ⇒ 没重新登记兄弟序（z 序断言会读到 NaN）");
                }
                else HudCanvas.Add(t as RectTransform, z);
                n++;
            }
            return n;
        }

        // ============================================================ 播放
        /// <summary>起播一条 clip。**0 s 的那 4 条会当场落到终态**；批处理/非播放态**一律直接落到 `t = length`**
        /// （没有帧循环，见文件头）。`name` 不在表里 ⇒ 出声、不动。</summary>
        public void Play(string name)
        {
            var lc = LoadClip(name);
            if (lc == null)
            {
                if (_warned.Add("play:" + name))
                    Debug.LogError("[BattleHudAnim] 表里没有 clip `" + name + "` ⇒ 这一档**没动**");
                return;
            }
            _cur = lc;
            _len = lc.d.length;
            if (!Application.isPlaying || _len <= 1e-6f)
            {
                // 批处理无帧循环 ⇒ 直接落终态（同 `BuildLoadingCover` 那条先例）
                _t = _len; _playing = false; ApplyAt(_len); return;
            }
            _t = 0f; _playing = true;
            ApplyAt(0f);
        }

        /// <summary>推时间（只有 `Update` 泵调；⛔ 别在 `UpdateHud` 里推 —— 那里一帧可能调两次）。</summary>
        public void Advance(float dt)
        {
            if (!_playing || _cur == null) return;
            _t += dt > 0f ? dt : 0f;
            if (_t >= _len) { _t = _len; _playing = false; }
            ApplyAt(_t);
        }

        /// <summary>**自检口**：把某条 clip 钉到 `t`（不播、不推）。⛔ 产品路径别调。</summary>
        public void SnapTo(string name, float t)
        {
            var lc = LoadClip(name);
            if (lc == null) { Debug.LogError("[BattleHudAnim] `SnapTo` 找不到 clip `" + name + "`"); return; }
            _cur = lc; _len = lc.d.length; _t = Mathf.Clamp(t, 0f, _len); _playing = false;
            ApplyAt(_t);
        }

        /// <summary>**诊断口**：把重建出来的 clip 采到 `root` 上（⛔ 产品路径不用 —— 它写绝对值）。</summary>
        public static bool SampleForTest(GameObject root, string name, float t)
        {
            var lc = LoadClip(name);
            if (lc == null || lc.clip == null || root == null) return false;
            lc.clip.SampleAnimation(root, t);
            return true;
        }

        /// <summary>把当前 clip 在 `t` 处的**位移**写进 rig / 手牌区。
        /// <para>⚠️ **clip 里没有的曲线 ⇒ 那个轴保持上一档的值**（Unity 的 legacy 采样语义：
        /// 没有这条曲线就没人写它）。例：`ShowInstant` **没有** `Left Anchor.y` ⇒ 它不把那一轴归零。</para></summary>
        void ApplyAt(float t)
        {
            if (_cur == null) return;
            var d = _cur.d;
            for (int i = 0; i < d.curves.Length; i++)
            {
                var c = d.curves[i];
                var curve = _cur.eval[i];
                if (c == null || curve == null) continue;
                float val = curve.Evaluate(t);
                float delta = val - _cur.shown[i];
                if (_cur.isHand[i]) { _handPx = delta; continue; }
                if (_cur.isEnemy[i]) { _enemyPx = delta; continue; }
                Vector2 p;
                if (!_off.TryGetValue(c.node, out p)) continue;      // 没有 rig 的节点（手牌两处已在上面接走）
                if (c.property == "m_AnchoredPosition.x") p.x = delta;
                else if (c.property == "m_AnchoredPosition.y") p.y = delta;
                else continue;
                _off[c.node] = p;
            }
            for (int i = 0; i < _rigOrder.Count; i++)
            {
                var node = _rigOrder[i];
                var rt = _rigs[node];
                if (rt != null) rt.anchoredPosition = _off[node];
            }
        }

        /// <summary>把 rig 全部归零（重开一局/建完先用一次）。</summary>
        public void ResetAll()
        {
            _cur = null; _t = 0f; _len = 0f; _playing = false;
            _handPx = 0f; _enemyPx = 0f;
            foreach (var k in new List<string>(_off.Keys)) _off[k] = Vector2.zero;
            foreach (var kv in _rigs) if (kv.Value != null) kv.Value.anchoredPosition = Vector2.zero;
        }

        // ============================================================ 自检/诊断
        /// <summary>表里每条 clip 的名字（自检对账用）。</summary>
        public static IEnumerable<string> Names
        {
            get
            {
                EnsureLoaded();
                if (_file == null || _file.clips == null) yield break;
                for (int i = 0; i < _file.clips.Length; i++)
                    if (_file.clips[i] != null) yield return _file.clips[i].name;
            }
        }

        /// <summary>表里某条 clip 的曲线条数（原版：Hidden/Hide/Show 各 16、NoHand 各 14、
        /// ShowInstant 14、ShowInstantNoHand 15）。</summary>
        public static int CurveCountOf(string name)
        {
            var c = FindClip(name);
            return c != null && c.curves != null ? c.curves.Length : -1;
        }

        /// <summary>建不建得出来（诊断用：载荷有没有进 `Resources`）。</summary>
        public static string Describe()
        {
            EnsureLoaded();
            return LastError != null
                ? "BattleHudAnim 未就绪：" + LastError
                : "BattleHudAnim " + ClipCount + " 条 · hash " + (_file != null ? _file.clipsHash : "?");
        }
    }
}
