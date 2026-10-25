// MarkerlightController.cs — 原版 `MarkerlightController`（108 行 · `markerlight` / DefinedTrait 1170 的计数器 + 两态）
//
// 判据正本 → `Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md` §1.1 / §3.2 / §6.1③ / §6.6
// 原版一手出处（**逐字读过全文**）：
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MarkerlightController.cs`
//     :7-23  9 个序列化字段（counter / normalStateObject / materialFaderNormalState /
//            expiringStateObject / materialFaderExpiringState / fadeTime / updateMarkerLightVFX /
//            floors[] / alreadyAttacked）
//     :25-48 Start()          ← **必需**：关 `Expiring State`、给两个 `Grid floor` 设 `_RippleScale`
//     :50-52 Toggle(bool)     ← 空体
//     :54-79 UpdateValue(int) ← counter + punch（与 HuntMark 前 3 行完全相同）+ 一段**死支**
//     :81-86 AimingAttack(bool) ← `MaterialFader.Toggle(option ^ 1, fadeTime)`  ⇒ **P2，本轮未实现**
//     :88-93 MarkerLightUsed()  ← `alreadyAttacked = true` + Normal off / Expiring on（**直接三句**）
//     :95-101 ChangeState(bool) · :103-106 FadeFixedElements(...) ← **本 build 类内零调用 = 死代码**
//
// 反编译方法体 `D:/2/tools/decomp_full/MarkerlightController__*.c`：
//   `__Start.c` 逐字 = `alreadyAttacked = false; normal.SetActive(true); expiring.SetActive(false);
//                    foreach (r in floors) if (r) r.GetMaterial().SetFloat("_RippleScale", transform.lossyScale.x);`
//   `__UpdateValue.c:54-79` = `counter.text = newValue.ToString();`
//                             + `DOPunchScale(counter.transform, …, 0.5f, 10, 1.0f)`（同 HuntMark）
//                             + `if (updateMarkerLightVFX) { Instantiate(...); 缩放 = 自身 lossyScale }`
//
// 🔴 三条**必须照做的真值**（判据 §1.3 / §七 F5·F7）：
//   ① `fadeTime`：ILSpy 的字段默认值是 **1f**，但 **`MarkerlightIdle` prefab 覆写 = 0.5**。
//      我们是运行时 `AddComponent`（没有 prefab 序列化值可用）⇒ **字段初值写 0.5**，
//      ⛔ 照 ILSpy 的 `1f` 写会慢一倍（铁律 5·c「一个值 ≠ 全部情况」）。
//   ② `updateMarkerLightVFX`：本 build 的 prefab 里是**空引用**（`MonoBehaviour_-3235397510437391791.json:33-36`）
//      ⇒ `UpdateValue` 里那段 `Instantiate` 是**死支**（铁律 11 例外①：原版数据本身为空）。
//      ⛔ 别猜一个 VFX 塞进去。
//   ③ `MaterialFader`：**2026-10-25（第八十会话 · R5）已建并接上** —— `Core/MaterialFader.cs` 是共用件，
//      本类的两个 `materialFader*` 字段**已改回真类型 `MaterialFader`**，`AimingAttack` 走原版那三句。
//      原版这两个 fader 是 `MarkerlightIdle` prefab 上**序列化好的组件**（挂在
//      `Normal State/Fixed` 与 `Expiring State/Fixed`），我们是**运行时 AddComponent + 显式绑**（见 `EnsureFaders`）。
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// `markerlight`（1170）的计数器 + 「Normal / Expiring」两态。
    /// 全库只有 `MarkerlightIdle` 这一个 prefab 带它（判据 §1.3）。
    /// 本机 prefab 实测：`Counter` / `Normal State` / `Expiring State` 都是**根的直接子件**；
    /// 两个 `Grid floor` **不是**（见 `BindIfMissing` 里那条就地订正）。
    /// </summary>
    public class MarkerlightController : TraitEffectController
    {
        // ── 原版数值 ────────────────────────────────────────────────────────────
        /// <summary>punch 三个常量与 HuntMark **完全相同**（判据 §3.2：`MarkerlightController__UpdateValue.c`
        /// 前 3 行与 HuntMark 一模一样）。出处同上：`DAT_1834b2bb4` / `mov 0xa` / `DAT_1834b2bb8`。
        /// `0.1` 同样是「`Vector3.one` × 常量」的**高置信推断**（不是实读到乘法那一句）。</summary>
        public const float PunchScale = 0.1f;
        public const float PunchDuration = 0.5f;
        public const int PunchVibrato = 10;
        public const float PunchElasticity = 1.0f;
        /// <summary>shader 属性名（原版 `MarkerlightController__Start.c` 里的字面量 `"_RippleScale"`）。</summary>
        public const string RippleScaleProperty = "_RippleScale";
        /// <summary>prefab 里两个「Grid floor」节点的 GameObject 名（原版 `floors[]` 指向它们的 Renderer）。</summary>
        public const string FloorNodeName = "Grid floor";
        /// <summary>prefab 覆写值（判据 §1.3 / §七 F7）；⛔ 别改成 ILSpy 的默认值 1f。</summary>
        public const float PrefabFadeTime = 0.5f;

        // ── 原版 9 个序列化字段（名字逐一对应 `MarkerlightController.cs:7-23`）───────────────
        [SerializeField] TextMeshPro counter;
        [SerializeField] GameObject normalStateObject;
        /// <summary>原版类型 = `MaterialFader`（`MarkerlightController.cs:10`）。原版是 prefab 序列化引用；
        /// 我们是运行时建（见 <see cref="EnsureFaders"/>，节点 = `Normal State/Fixed`）。</summary>
        [SerializeField] MaterialFader materialFaderNormalState;
        [SerializeField] GameObject expiringStateObject;
        /// <summary>原版类型 = `MaterialFader`（同上）。节点 = `Expiring State/Fixed`。</summary>
        [SerializeField] MaterialFader materialFaderExpiringState;
        /// <summary>见本文件头 ①：**prefab 覆写 = 0.5**（`MonoBehaviour_-3235397510437391791.json:32`）。</summary>
        [SerializeField] float fadeTime = PrefabFadeTime;
        /// <summary>本 build = **空引用** ⇒ `UpdateValue` 里那段是死支（见本文件头 ②）。</summary>
        [SerializeField] GameObject updateMarkerLightVFX;
        /// <summary>原版 = 两个 `Grid floor` 节点的 Renderer（`ParticleSystemRenderer`，各 1 个）。</summary>
        [SerializeField] Renderer[] floors;
        bool alreadyAttacked;

        // ── 观测量（判据 §6.5；**我们的**，⛔ 不是原版字段；只读，给自检用）────────────────
        public int PunchCount { get; private set; }
        public int UpdateCalls { get; private set; }
        public int LastValue { get; private set; }
        public string TextForTest { get { return counter != null ? counter.text : null; } }
        public bool Bound { get { return counter != null; } }
        /// <summary>`alreadyAttacked` 的只读镜像（原版是 private 字段；自检要能看它）。</summary>
        public bool AlreadyAttackedForTest { get { return alreadyAttacked; } }
        /// <summary>绑上的 `floors` 条数（原版应为 **2**；判据 §6.4 A-ML）。</summary>
        public int FloorsBound { get { return floors != null ? floors.Length : 0; } }
        /// <summary>`floors[]` 的只读镜像（断言要读它们材质的 `_RippleScale`）。</summary>
        public Renderer[] FloorsForTest { get { return floors; } }
        /// <summary>`fadeTime` 的只读镜像（断言应断 **0.5**）。</summary>
        public float FadeTimeForTest { get { return fadeTime; } }
        /// <summary>两个 `MaterialFader` 的只读镜像（W10 自检要读它们的 `ToggleCalls` / `TargetFadeForTest`）。</summary>
        public MaterialFader NormalFaderForTest { get { return materialFaderNormalState; } }
        public MaterialFader ExpiringFaderForTest { get { return materialFaderExpiringState; } }

        // ── 生命周期 ────────────────────────────────────────────────────────────
        /// <summary>⚠️ **我们的**（原版没有 `Awake`）：原版靠 prefab 序列化拿引用，我们靠名字绑。
        /// 必须在 `Awake` 里就绑好 —— 原版 `UpdateValue` 在 `Instantiate` 那一拍就会被调，`Start` 是下一拍。</summary>
        void Awake() { BindIfMissing(); }

        /// <summary>原版 `:25-48` 逐字（**这一段是必需的**：`MarkerlightIdle` prefab 里
        /// `Normal State` 与 `Expiring State` 两个节点序列化时**都是 active=1**，没有这一步两层会叠着画）。
        /// ⚠️ 原版对 `normalStateObject` / `expiringStateObject` 没做空判（拿得到序列化引用）；
        /// 我们按名字绑 ⇒ 加空判 + `BindIfMissing` 已出声（⛔ 不静默、也不 NRE）。</summary>
        protected override void Start()
        {
            BindIfMissing();

            alreadyAttacked = false;
            if (normalStateObject != null) normalStateObject.SetActive(true);
            if (expiringStateObject != null) expiringStateObject.SetActive(false);

            if (floors != null)
            {
                float ripple = transform.lossyScale.x;
                for (int i = 0; i < floors.Length; i++)
                {
                    Renderer r = floors[i];
                    if (r == null) continue;
                    // ⚠️ 原版是 `r.GetMaterial()`。**那个方法的定义体在反编译产物里没找到**
                    //    （`grep 'GetMaterial(this' / 'Material GetMaterial'` 全库 0 命中；调用点很多）。
                    //    我们用 Unity 标准的 `Renderer.material`（为该 renderer 实例化一份独立材质）——
                    //    ⛔ 用 `sharedMaterial` 会在运行时改到共用资产（编辑模式下还会把资产弄脏）。
                    //    「原版那个 helper 到底是 material 还是 sharedMaterial」= **未查清**（报告 §⑤）。
                    Material mat = r.material;
                    if (mat != null) mat.SetFloat(RippleScaleProperty, ripple);
                }
            }
        }

        // ── 三个抽象方法 ────────────────────────────────────────────────────────
        /// <summary>原版 `:50-52` —— **空体**（本 build 0 调用点，判据 §八·A）。⛔ 别赋予语义。</summary>
        public override void Toggle(bool option) { }

        /// <summary>原版 `:54-79`：与 HuntMark 的前 3 行**逐字相同**（写文本 + punch），
        /// 之后多一段 `if (updateMarkerLightVFX) { Instantiate(...) }` —— 本 build 该字段是空引用
        /// ⇒ **死支，⛔ 不实现**（判据 §七 F5）。</summary>
        public override void UpdateValue(int newValue)
        {
            UpdateCalls++;
            LastValue = newValue;
            BindIfMissing();
            if (counter == null) return;   // 绑不上已经 `LogError` 过（⛔ 不静默），这里别 NRE

            counter.text = newValue.ToString();

            PunchCount++;
            // 同 HuntMark：参数一字不改，只多一层 `CardTween.Use`（Manual 推进 + `SetLink` 生命周期）。
            CardTween.Use(
                counter.transform.DOPunchScale(
                    Vector3.one * PunchScale, PunchDuration, PunchVibrato, PunchElasticity),
                Ease.Linear, counter.transform);
        }

        /// <summary>原版 `:81-86` 逐字：
        /// <code>
        /// MaterialFader materialFader = (!alreadyAttacked) ? materialFaderNormalState : materialFaderExpiringState;
        /// bool option2 = (option ? 1u : 0u) ^ 1u;
        /// materialFader.Toggle(option2, fadeTime);
        /// </code>
        /// ⇒ `option == true`（攻击**起手**）传 `0` = **淡出到全透明**；`option == false`（攻击**结束**）传 `1` = 淡回。
        /// <para>⚠️ 两个 fader 在**我们这一版**是运行时建的（原版是 prefab 序列化组件）⇒ 建不出来时**出声一次**
        /// （⛔ 不静默、也⛔ 不 NRE）。原版对 `materialFader` 没有空判（它拿得到序列化引用）。</para>
        /// <para>📌 **谁调它**（2026-10-25 · R5 · W10 ③ 已接）：`Battle/BattleDriver.cs` 的 `AimAttackingStatusPair`
        /// → `TraitFrames.AimingAttack("markerlight", bool)` → 本方法。起手 = `PlaySignal` 的 `EvtKind.Attack` 那一拍，
        /// 收手 = 同一拍排的「攻方动作序列跑完」回调（判据两拍 = `CardScript.cs:3649` / `:3637`）。</para></summary>
        public override void AimingAttack(bool option)
        {
            EnsureFaders();
            MaterialFader fader = !alreadyAttacked ? materialFaderNormalState : materialFaderExpiringState;
            if (fader == null)
            {
                if (!_warnedNoFader)
                {
                    _warnedNoFader = true;
                    Debug.LogError("[MarkerlightController] `AimingAttack` 取不到 "
                                 + (alreadyAttacked ? "`materialFaderExpiringState`（`Expiring State/Fixed`）"
                                                    : "`materialFaderNormalState`（`Normal State/Fixed`）")
                                 + " ⇒ 这一次攻击前后**不会**淡入/淡出（原版在这里直接 NRE）。"
                                 + "判据 → 资料/完全复刻/判据/TraitEffectController_钩子层.md §6.6 / §⑧·2 F-E");
                }
                return;
            }
            // 原版 `(byte)((option ? 1u : 0u) ^ 1u) != 0` = `!option`（`Toggle` 收的是 **bool**，
            // ⛔ 照抄字面量 `option ^ 1` 会 CS0019：`^` 不能混用 `bool` 与 `int`）。
            fader.Toggle(!option, fadeTime);
        }

        /// <summary>原版 `:88-93` 逐字（🔴 **直接三句，不走 `ChangeState`** ——
        /// 判据 §6.1 写的「上面两个都走它」与源码不符，就地记在报告 §⑤）：
        /// `alreadyAttacked = true; normal.SetActive(false); expiring.SetActive(true);`
        /// 调用方（下一批 P2 才接）= `CardScript.SetRemoveMarkerlight`（`CardScript.cs:11275/11293`）。</summary>
        public void MarkerLightUsed()
        {
            alreadyAttacked = true;
            if (normalStateObject != null) normalStateObject.SetActive(false);
            if (expiringStateObject != null) expiringStateObject.SetActive(true);
        }

        /// <summary>原版 `:95-101` 逐字。
        /// ⚠️ **本 build 类内零调用 = 死代码**（`grep ChangeState MarkerlightController.cs` 只有定义这一行；
        /// `MarkerLightUsed` 是自己写三句、`AimingAttack` 走 fader）—— 按形状保留，⛔ 别把它当接线点。</summary>
        void ChangeState(bool alreadyAttacked)
        {
            this.alreadyAttacked = alreadyAttacked;
            if (normalStateObject != null) normalStateObject.SetActive(!alreadyAttacked);
            if (expiringStateObject != null) expiringStateObject.SetActive(alreadyAttacked);
        }

        // 原版 `:103-106 FadeFixedElements(bool, MaterialFader)` = `whoToFade.Toggle(option, fadeTime);`
        // ⇒ 参数类型 `MaterialFader` 现在**已经有了**（2026-10-25 · R5），但本 build 类内**零调用 = 死代码**
        //   ⇒ 仍然**不复刻**（铁律 11 例外①：原版自己没有调用点）。⛔ 别把它当接线点。

        // ── 绑定 ────────────────────────────────────────────────────────────────
        /// <summary>
        /// 按名字绑 prefab 上的 5 个引用（原版全是序列化引用）。幂等；绑不上**逐个出声**。
        ///
        /// 本机 prefab 实测（判据 §1.3 / §6.2，2026-10-25 按 `m_Father` 链逐级核过）：
        ///   · `Counter`        → `['Counter','MarkerlightIdle']`              直接子件 ✅
        ///   · `Normal State`   → `['Normal State','MarkerlightIdle']`         直接子件 ✅
        ///   · `Expiring State` → `['Expiring State','MarkerlightIdle']`       直接子件 ✅
        ///   · `Grid floor` ×2  → **不是直接子件**（见下面那条就地订正）
        ///
        /// 🔴 **就地订正（判据 §6.2 那句不成立）**：判据写「两个 `Grid floor` 都是根的直接子件」，
        /// 实测是
        ///   `['Grid floor','Base Vfx','Fixed','Expiring State','MarkerlightIdle']` 与
        ///   `['Grid floor','Base VFX','Fixed','Normal State','MarkerlightIdle']`
        /// ⇒ ⛔ **别用 `transform.Find("Grid floor")`**（那样恒 null）；这里改成
        /// 「取全部 `ParticleSystemRenderer`（**含未激活**，因为 `Expiring State` 在 `Start` 里要被关掉）
        ///   + 按 GameObject 名 == `Grid floor` 过滤」—— 实测正好 2 个。
        /// </summary>
        void BindIfMissing()
        {
            if (counter == null)
                counter = ChildComponent<TextMeshPro>("Counter");
            if (normalStateObject == null)
                normalStateObject = ChildGo("Normal State");
            if (expiringStateObject == null)
                expiringStateObject = ChildGo("Expiring State");

            if (floors == null || floors.Length == 0)
            {
                ParticleSystemRenderer[] all = GetComponentsInChildren<ParticleSystemRenderer>(true);
                List<Renderer> hit = new List<Renderer>();
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && all[i].gameObject.name == FloorNodeName) hit.Add(all[i]);
                if (hit.Count > 0) floors = hit.ToArray();
            }

            // 🆕 W10（2026-10-25 · R5）：两个 `MaterialFader` —— 原版是 prefab 序列化组件，我们运行时建。
            //   放在 `BindIfMissing` 里 = `Awake` / `Start` / `UpdateValue` 三个入口都会建（幂等，见 `EnsureFaders`）。
            EnsureFaders();

            if (counter == null)
                Debug.LogError("[MarkerlightController] 绑不上 `Counter`（TMPro.TextMeshPro）—— "
                             + "实测路径应为 ['Counter','MarkerlightIdle']。计数器不会更新、也不会弹。"
                             + "判据 → 资料/完全复刻/判据/TraitEffectController_钩子层.md §1.3 / §6.2");
            if (normalStateObject == null || expiringStateObject == null)
                Debug.LogError("[MarkerlightController] 绑不上 `Normal State` / `Expiring State` —— "
                             + "实测路径应是根的直接子件。两态切换（`MarkerLightUsed`）会失效。");
            if (floors == null || floors.Length == 0)
                Debug.LogError("[MarkerlightController] 绑不上 `Grid floor`（`ParticleSystemRenderer`）—— "
                             + "`Start` 里不会设 `_RippleScale`。⚠️ 它们是**叶子**（`…/Fixed/Grid floor`），"
                             + "⛔ 不是根的直接子件（判据 §6.2 那句已就地订正）。");
            else if (floors.Length != 2)
                Debug.LogWarning("[MarkerlightController] `Grid floor` 绑到 " + floors.Length
                               + " 个（原版 `floors[]` = 2，判据 §1.3）—— 重导过 prefab？");
        }

        GameObject ChildGo(string childName)
        {
            Transform t = transform.Find(childName);
            return t != null ? t.gameObject : null;
        }

        T ChildComponent<T>(string childName) where T : Component
        {
            Transform t = transform.Find(childName);
            return t != null ? t.GetComponent<T>() : null;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-25（第八十会话 · R5 · W10）：两个 `MaterialFader` 的**建 + 绑**
        //
        //  原版这两个 fader 是 `MarkerlightIdle` prefab 上**序列化好的组件**（我们运行时 `AddComponent`
        //  的控制器里拿不到那两个引用）⇒ 照 `TraitEffectControllers.Attach` 的同一套做法：**按节点名 + 幂等**。
        //
        //  判据（逐条实读 `bundle_battleprefabs_vfxandmisc_assets_all` 的 `MarkerlightIdle`）：
        //    · 节点树：`MarkerlightIdle/{Normal State,Expiring State}/Fixed`，两个 `Fixed` 各挂**一个** `MaterialFader`；
        //      ⚠️ **⛔ 不是都在 `Expiring State`**（账 244 行那么写过，判据 §⑧·2 `F-E` 已订正）。
        //    · `Normal State/Fixed` 的 `renderers[]`  = `Base VFX` / `Ground Effect` / `Ground glow pulse` / `Grid floor`
        //    · `Expiring State/Fixed` 的 `renderers[]` = `Base Vfx` / `Ground Effect` / **null（PathID 0）** / `Grid floor`
        //      （大小写不同是**原版如此**：`Base VFX` vs `Base Vfx`，⛔ 别「统一」掉）
        //    · 两个实例的三个开关都是 `0 / 0 / 1`（`restoreOriginalMaterialsOnFadeOut = 0`、
        //      `restoreOriginalMaterialsOnFadeIN = 0`、`disableOnFadeOut = 1`）⇒ 必须 `ApplyPrefabFlags(false,false,true)`，
        //      而 C# 字段默认是 `true / false / true`（`MaterialFader` 类头那条）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版两个 `MaterialFader` 所在的节点路径（相对本组件所在的根）。</summary>
        public const string NormalFaderPath = "Normal State/Fixed";
        public const string ExpiringFaderPath = "Expiring State/Fixed";
        /// <summary>`Normal State/Fixed` 那个 fader 的 `renderers[]`（**原版实读，顺序不动**）。</summary>
        static readonly string[] NormalFaderRendererNames = { "Base VFX", "Ground Effect", "Ground glow pulse", "Grid floor" };
        /// <summary>`Expiring State/Fixed` 那个 fader 的 `renderers[]`。⚠️ 下标 2 原版就是 **null**（PathID 0），⛔ 别填。 </summary>
        static readonly string[] ExpiringFaderRendererNames = { "Base Vfx", "Ground Effect", null, "Grid floor" };

        /// <summary>`AimingAttack` 的空判出声只一次（`renderers` 那条由 `MaterialFader` 自己出声）。</summary>
        bool _warnedNoFader;

        /// <summary>建 + 绑两个 fader（幂等）。⚠️ 每次只重绑 `renderers[]` 为空的那些 ——
        /// `BindRenderers` 会清掉「原材质」缓存 ⇒ 淡变中途重绑会把还原用的材质丢掉。</summary>
        void EnsureFaders()
        {
            if (materialFaderNormalState == null)
                materialFaderNormalState = AttachFader(NormalFaderPath, NormalFaderRendererNames);
            if (materialFaderExpiringState == null)
                materialFaderExpiringState = AttachFader(ExpiringFaderPath, ExpiringFaderRendererNames);
        }

        MaterialFader AttachFader(string nodePath, string[] rendererNames)
        {
            Transform node = transform.Find(nodePath);
            if (node == null)
            {
                if (!_warnedNoFader)
                {
                    _warnedNoFader = true;
                    Debug.LogError("[MarkerlightController] 找不到节点 `" + nodePath + "` —— 两个 `MaterialFader` 建不出来。"
                                 + "实测路径应为 `MarkerlightIdle/[Normal|Expiring] State/Fixed`（判据 §⑧·2 F-E）。"
                                 + "`AimingAttack` 的前后淡入淡出会失效（⛔ 不静默）。");
                }
                return null;
            }

            MaterialFader f = node.GetComponent<MaterialFader>();
            if (f == null) f = node.gameObject.AddComponent<MaterialFader>();

            // ⚠️ 只在「还没绑过」时绑：`BindRenderers` 会清 `originalMaterials` 缓存（见上面的 doc）。
            if (f.BoundCount == 0)
            {
                f.BindRenderers(ResolveRenderers(node, rendererNames));
                // 原版两个实例的序列化开关都是 0 / 0 / 1（本段开头那张表）。
                f.ApplyPrefabFlags(false, false, true);
            }
            return f;
        }

        /// <summary>按**名字**在这个 `Fixed` 子树里取 `Renderer`（顺序照原版数组；`null` 名保持 `null`）。
        /// ⚠️ 名字是**大小写敏感**的（原版 `Base VFX` / `Base Vfx` 两个节点名本来就不一样）。</summary>
        static Renderer[] ResolveRenderers(Transform node, string[] names)
        {
            Renderer[] all = node.GetComponentsInChildren<Renderer>(true);
            var res = new Renderer[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;      // 原版这一格本来就是 null
                for (int k = 0; k < all.Length; k++)
                {
                    if (all[k] == null || all[k].gameObject.name != names[i]) continue;
                    res[i] = all[k];
                    break;
                }
                if (res[i] == null)
                    Debug.LogError("[MarkerlightController] `" + node.name + "` 下找不到 renderer `" + names[i]
                                 + "`（原版 `renderers[]` 的实读值）—— 重导过 prefab？这一格不参与淡变。");
            }
            return res;
        }
    }
}
