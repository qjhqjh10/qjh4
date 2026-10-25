// UIImageMaterialColorChanger.cs — 通用件：**材质属性色的补间**（原版同名类，666 处实例）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它在什么时候改什么（一句话判据）】
//   `DoColorChange(target, propertyId, time, instant)` 先把目标件的材质**实例化一份**，然后：
//   **instant=false ⇒ 起补间**（记住 `originalColor = 当前材质色`、`currentTime=0`、`timeToChange=time`、`enabled=true`），
//   每帧 `Update` 里 `t = clamp01(currentTime / timeToChange)`、`材质.SetColor(prop, Lerp(original, target, t))`；
//   `currentTime >= timeToChange` 那一帧**先写终值再 `enabled=false`**（注意：原版是**先算 t、后累加**，见下方逐句）；
//   **instant=true ⇒ 直接 SetColor、不补间**。`SetColor` = 直接写；`Stop` = 停（`enabled=false`）；`OnDestroy` 销毁实例材质。
//
// 【判据（第一权威 = 反编译）】`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIImageMaterialColorChanger.cs`
//   · `Awake`（:21）＝ `enabled = false`（组件出厂不跑）
//   · `DoColorChange`（:53-81）＝ `if (!instancedMaterial) { instancedMaterial = new Material(image.material); image.material = instancedMaterial; }`
//     → `if (!instant) { this.prop = prop; targetColor = target; originalColor = 实例材质当前色; currentTime = 0; timeToChange = time; enabled = true; }`
//     → `else { 实例材质.SetColor(prop, target); }`
//   · `Update`（:31-51）＝ `t = currentTime / timeToChange`（原版**不夹**，Lerp 自己夹）→ `SetColor(prop, Lerp(original + (target−original)×t))`
//     → `if (!(currentTime < timeToChange)) enabled = false;` → **最后**才 `currentTime += Time.deltaTime`
//   · `SetColor`（:83-88）/ `Stop`（:90-93）/ `Initialize`（:95-101，同第一句实例化）/ `OnValidate`（:103-116，找 Image，找不到 LogError）
//   · `OnDestroy`（:26-29）＝ `Destroy(instancedMaterial)`
//
// 【与我们的世界的**结构性适配**（如实标，不是复刻）】
//   · 原版挂在 `Image` 上、读写 `Image.material`；我们**没有 uGUI** ⇒ 本件改挂 **`Renderer`**
//     （`ImageQuad` 的 `MeshRenderer` 或任何带材质的 `Renderer`），语义一一对应（`image.material` ↔ `renderer.sharedMaterial`）。
//   · ⚠️ **`_Color` 不一定是我们全部 shader 的有效属性**：`ImageQuad.SetTint` 写的是**材质 `color`**（等价 `_Color`），
//     但特效/自建 shader 的颜色属性名各不相同 ⇒ 调用方**显式传属性名/ID**；本件不猜。**接实例时必须逐个确认该材质有这个属性**
//     （否则 `SetColor` 静默无效 —— 本件会在 `DoColorChange` 里对「材质没有该属性」**出声一次**）。
//   · `Update` 用真实 `Time.deltaTime`；批处理没有帧循环 ⇒ 自检/编辑器用 `Step(dt)` 手推（与 `UiBadgeNotification.Step` 同口径）。
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>材质属性色补间（原版 `UIImageMaterialColorChanger`）。判据见文件头。</summary>
    public sealed class UIImageMaterialColorChanger : MonoBehaviour
    {
        /// <summary>最常用的颜色属性名（`ImageQuad` 的材质色就是 `_Color`）。</summary>
        public const string DefaultProperty = "_Color";

        [SerializeField] Renderer targetRenderer;

        Material _instanced;
        Color _target, _original;
        float _time, _duration;
        int _prop = -1;
        bool _running;
        bool _counted;                          // `_instanced` 已记过一次引用计数（每个组件只记一次）

        /// <summary>🔴 **2026-10-25（接实例前置）**：同一颗 `Renderer` 上可能挂**多颗**本组件。
        /// <para>原版是「一颗 `Image` 一份材质、一个组件」，而我们的卡面**同一份 SDF 材质上要同时补间
        /// `_Outline` 与 `_ShadowColor` 两个属性**（两个属性各有各的节奏）⇒ 必须让它们**共用同一份实例材质**，
        /// 否则第二颗组件会再 `new Material` 一次并把 `renderer.sharedMaterial` 换掉（第一颗的写入**静默丢失**）。</para>
        /// <para>实现 = 「同一 Renderer 上已有的实例材质则复用」+ 引用计数（先销毁的那颗不销毁共享材质）。</para></summary>
        static readonly Dictionary<Material, int> _refCounts = new Dictionary<Material, int>();

        static readonly HashSet<string> _missingPropNoted = new HashSet<string>();   // 同一个「材质+属性」只出声一次

        /// <summary>在补间中吗（自检读口）。</summary>
        public bool Running { get { return _running; } }
        /// <summary>目标色 / 起始色（自检读口）。</summary>
        public Color TargetColor { get { return _target; } }
        public Color OriginalColor { get { return _original; } }
        /// <summary>实例材质（原版 `instancedMaterial`；没绑/没材质时 null）。</summary>
        public Material InstancedMaterial { get { return _instanced; } }
        /// <summary>当前进度 0..1（自检读口；`duration<=0` 时恒 1）。</summary>
        public float Progress { get { return _duration > 0f ? Mathf.Clamp01(_time / _duration) : 1f; } }

        /// <summary>绑到一颗渲染器（= 原版 `OnValidate` 里取 `Image` 那一半的等价物）。</summary>
        public void Bind(Renderer r)
        {
            if (_instanced != null) Release();      // 换目标前先把旧共享材质的引用计数还掉
            targetRenderer = r;
            _instanced = null;
        }

        /// <summary>建一颗并绑上（原版靠 prefab 挂载，我们代码建 ⇒ 给程序化入口；同 `UiBadgeNotification` 的 Attach）。</summary>
        public static UIImageMaterialColorChanger Attach(Renderer r)
        {
            if (r == null) return null;
            var c = r.gameObject.GetComponent<UIImageMaterialColorChanger>();
            if (c == null) c = r.gameObject.AddComponent<UIImageMaterialColorChanger>();
            c.Bind(r);
            c.enabled = false;                 // 原版 Awake：出厂不跑
            return c;
        }

        void Awake() { enabled = false; }      // 原版 :23

        Material EnsureMaterial()
        {
            if (_instanced != null) return _instanced;
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                Debug.LogWarning("[UIImageMaterialColorChanger] `" + name + "`：没有 Renderer/材质 ⇒ 本次改色**不生效**"
                               + "（出声，不静默；接线口 = `Attach(renderer)`）");
                return null;
            }
            // 🔴 先看同一 Renderer 上有没有别的本组件已经建好实例材质 ⇒ **复用**（见 `_refCounts` 的注释）
            var peers = targetRenderer.GetComponents<UIImageMaterialColorChanger>();
            for (int i = 0; i < peers.Length; i++)
            {
                var p = peers[i];
                if (p == null || p == this || p._instanced == null) continue;
                _instanced = p._instanced;
                Retain();
                return _instanced;
            }
            // 原版：`new Material(image.material)` + `image.material = instancedMaterial`
            _instanced = new Material(targetRenderer.sharedMaterial);
            targetRenderer.sharedMaterial = _instanced;
            Retain();
            return _instanced;
        }

        void Retain()
        {
            if (_instanced == null || _counted) return;
            _counted = true;
            int n;
            _refCounts.TryGetValue(_instanced, out n);
            _refCounts[_instanced] = n + 1;
        }

        void Release()
        {
            if (_instanced == null || !_counted) return;
            _counted = false;
            int n;
            _refCounts.TryGetValue(_instanced, out n);
            if (n <= 1) _refCounts.Remove(_instanced); else _refCounts[_instanced] = n - 1;
            if (n > 1) return;                     // 还有别颗组件在用 ⇒ **不销毁**
            // ⚠️ 批处理/编辑模式下 `Destroy` 不生效且会报错 ⇒ 按 `Application.isPlaying` 分岔
            if (Application.isPlaying) Destroy(_instanced);
            else DestroyImmediate(_instanced);
        }

        /// <summary>原版 `DoColorChange`（:53）。`instant == true` ⇒ 直接写；否则起补间。</summary>
        public void DoColorChange(Color target, string propertyName, float time, bool instant)
        {
            if (string.IsNullOrEmpty(propertyName)) propertyName = DefaultProperty;
            DoColorChange(target, Shader.PropertyToID(propertyName), time, instant, propertyName);
        }

        /// <summary>重载（原版收的就是 `propertyId`）。</summary>
        public void DoColorChange(Color target, int propertyId, float time, bool instant)
        {
            DoColorChange(target, propertyId, time, instant, null);
        }

        void DoColorChange(Color target, int propertyId, float time, bool instant, string propertyNameForLog)
        {
            var m = EnsureMaterial();
            if (m == null) return;
            if (propertyNameForLog == null) propertyNameForLog = "#" + propertyId;
            if (!m.HasProperty(propertyId))
            {
                string k = m.shader != null ? m.shader.name : "?" + "|" + propertyNameForLog;
                if (_missingPropNoted.Add(k))
                    Debug.LogWarning("[UIImageMaterialColorChanger] 材质 `" + k + "` 没有颜色属性 `" + propertyNameForLog
                                   + "` ⇒ `SetColor` 不生效（出声一次；⛔ 别猜一个近似的属性名）");
                return;
            }
            if (instant)
            {
                m.SetColor(propertyId, target);
                _running = false; enabled = false;
                return;
            }
            _prop = propertyId;
            _target = target;
            _original = m.GetColor(propertyId);
            _time = 0f;
            _duration = Mathf.Max(0f, time);
            if (_duration <= 0f)
            {
                // `time == 0` 的补间在原版里会「下一帧 t = 0/0」——这里直接落终值（同一外显结果，且不产生 NaN）
                m.SetColor(propertyId, target);
                _running = false; enabled = false;
                return;
            }
            _running = true;
            enabled = true;
        }

        /// <summary>原版 `SetColor`（:83）：直接写（要求实例材质已建；未建时先建）。</summary>
        public void SetColor(Color c, string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) propertyName = DefaultProperty;
            var m = EnsureMaterial();
            if (m == null) return;
            int id = Shader.PropertyToID(propertyName);
            if (!m.HasProperty(id)) return;
            m.SetColor(id, c);
        }

        /// <summary>原版 `Stop`（:90）：停补间。</summary>
        public void Stop() { _running = false; enabled = false; }

        void Update() { Step(Time.deltaTime); }

        /// <summary>推进一帧（**逐句照原版 Update 的顺序**：先按 `_time` 算 t 写色，再判是否到点，最后才累加）。
        /// 返回是否仍在补间。批处理里没有帧循环 ⇒ 自检/编辑器用它手推。</summary>
        public bool Step(float dt)
        {
            if (!_running || _instanced == null) return false;
            float t = _duration > 0f ? Mathf.Clamp01(_time / _duration) : 1f;
            _instanced.SetColor(_prop, Color.Lerp(_original, _target, t));
            if (!(_time < _duration)) { _running = false; enabled = false; }
            _time += dt;
            return _running;
        }

        void OnDestroy()
        {
            // 原版 :26-29（`Object.Destroy(instancedMaterial)`）；我们多一层引用计数（共享材质，见 `_refCounts`）
            Release();
            _instanced = null;
        }

        /// <summary>自检用：把 `_time` 直接拨到某处（验「中间值 = Lerp」而不受 dt 累加影响）。</summary>
        public void SetTimeForTest(float t) { _time = Mathf.Max(0f, t); }
    }
}
