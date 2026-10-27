// AnimateUIMaterialProperty.cs — 原版 `AnimateUIMaterialProperty`（UI 材质浮点动画）的对应物
//
// ════════════════════════════════════════════════════════════════════════════════════
//  判据（两份都现读，2026-10-26）
//    🥈 有真体的 ILSpy：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AnimateUIMaterialProperty.cs`（全 8 个方法）
//    🥇 逐句 `d:/2/tools/decomp_full/AnimateUIMaterialProperty__{OnEnable,Update,OnDestroy,OnDisable,
//        CreateInstanceMaterial,SetMaterial,ResetToOriginalMaterial}.c` + `…AnimatedMaterialPropertyFloat__Update.c`
//
//  原版语义（逐方法，照抄口径）：
//   · `OnEnable()`    : `if (!initializeOnEnable) return;` → `CreateInstanceMaterial();`
//                       → `if (instancedMaterial) image.material = instancedMaterial;`
//                       🔴 这次赋值和 `CreateInstanceMaterial()` 里那次**重复**，但 **re-enable 时全靠它**
//                          （`OnDisable` 把 image 的材质还原成 original 了）⇒ **照抄，别当冗余删掉**。
//   · `Update()`      : `if (!instancedMaterial) return;` → 两个槽逐槽
//                       **先用 `struct.Equals(default)` 判空**（空槽整槽跳过）→ 非空再
//                       `useVector4 ? material.SetVector(name, vector4Value) : material.SetFloat(name, value)`。
//                       （`il2cpp_value_box + ValueType.Equals` 那半句是从 `.c` 读出来的，见下面「🔶 自己设计①」）
//   · `OnDisable()` / `ResetToOriginalMaterial()` : `image.material = originalMaterial;`
//                       ⚠️ **原版不还原 `_Reveal` 的值**（也不还原 α）—— 只换材质引用。
//   · `OnDestroy()`   : `Object.Destroy(instancedMaterial);`
//   · `CreateInstanceMaterial()` : `if (instancedMaterial) return;` → `originalMaterial = image.material;`
//                       → `source = changeMaterialOnAnimation ? changeMaterialOnAnimation : originalMaterial;`
//                       → `instancedMaterial = new Material(source); instancedMaterial.name += "(Instanced)";`
//                       → `image.material = instancedMaterial;`
//   · `SetMaterial(Material)` : `CreateInstanceMaterial(); instancedMaterial = <形参>;`
//                       🔴 **原版不再回写 `image.material`**（`.c` 全文只有那两句 + 一次写屏障）⇒ 照抄。
//                          ⇒ 调用它之后 `Update` 写的是**传进来那份**，屏幕上的还是上一次贴上去的那份。
//                          这是原版行为，⛔ 别「修好」（铁律：原版自己的怪行为照抄，注释里写清）。
//
//  🔴 我们这边的唯一替换（原版 `Image` → 我们的 `Renderer`）
//     本工程 UI 没有 uGUI Canvas，图块一律是 `CardPresentation.ImageQuad`
//     （`MeshRenderer` + 每件一份 `Sprites/Default` 材质，见 `ImageQuad.cs` 头）。
//     ⇒ 原版的 `image.material` 读/写 换成 `target.sharedMaterial` 读 / `target.sharedMaterial=` 写；
//       **字段名、方法名、顺序全部不动**（序列化名与 legacy clip 的 `floatProperty1.value` 绑定点要一致）。
//     ⚠️ ⛔ **别用 `Renderer.material`**：那个 getter 会**自己再克隆一份** ⇒ 和 `instancedMaterial` 不是同一个
//        对象，`Update` 写的就不是画的那份（典型的「改了但看不出来」）。
//
//  ── 🔶 自己设计（非原版）—— 只有两处 ─────────────────────────────────────────────────
//   ① **空槽判定**：原版是 `floatPropertyX.Equals(default)`（逐字节比整个结构体）。我们改成
//      「`materialProperty` 为空 且 `value==0` 且 `useVector4==false` 且 `vector4Value==zero` ⇒ 当空槽」。
//      为什么必须自己定：逐字节等价**没证**（G12 记：`il2cpp_value_box + ValueType.Equals` 只读出调用形状）。
//      实际差别只在「`materialProperty` 是 `""` 而不是 null」这一档：原版会走到 `SetFloat("", 0)`（Unity 的 no-op），
//      我们直接跳过 ⇒ **可见结果相同**，只是少一次无意义调用。6 个出厂实例的空槽都是 `""` 这一档（现读）。
//      定的人/时间：调度台 2026-10-26（B8_AS）。
//   ② **`OnDestroy` 只销毁我们自己克隆的那份**：原版无脑 `Destroy(instancedMaterial)` —— 若中途调过
//      `SetMaterial(m)`，那它销毁的是**别人传进来的材质**。为什么必须自己定：本工程的材质是**静态共享缓存**
//      （`WarpforgeEffectBinder.Cache` / `ImageQuad` 的 `sharedMaterial` 都可能被别人同时用），销毁会把
//      同场别的实例一起打成粉红、且**零报错**。⇒ 只销毁 `_ownInstancedMaterial == true` 的那份；
//      `SetMaterial` 换进来时置 false 并**出声**（`Debug.LogWarning`，本工程红线：不静默失败）。
//      定的人/时间：调度台 2026-10-26（B8_AS）。
//   ③ （接线便利）`Attach(...)` / `Slot(...)` / `SlotV4(...)` 三个静态/实例帮手 —— 原版没有，只为 Shell 侧
//      运行时接线不用手填 Inspector。**不改变任何语义。**
//  ────────────────────────────────────────────────────────────────────────────────
//
//  ── ⚠️ 接线方须知：我们的「换材质」口有三条（原版 uGUI 也有同族问题）────────────────
//   ① `ImageQuad.SetMaterial(m)`（`Shell` 换图/换队列时会调）② `ImageQuad.EnsureGreyCapable()`（去饱和会**整份换材质**）
//   ③ 视图重建（`CardView.Create` 一类）⇒ 换了 GameObject / 换了 `sharedMaterial`。
//   任何一条发生后，`_instancedMaterial` 就**不是正在渲染的那份**了 ⇒ `Update` 写到空气里（静默）。
//   原版同样躲不开（uGUI 的 `IMaterialModifier` 也会顶掉 `image.material`）⇒ **我们没有发明修法**；
//   接线时**别把本组件挂到「会被去饱和/会被 SetMaterial 复用」的那张图上**（例：卡组编辑卡池的
//   `Counter` 条会走 `SetGrey`）。⛔ 也别为此在 `Update` 里每帧比对 `sharedMaterial`（那是猜出来的修法）。
//  ────────────────────────────────────────────────────────────────────────────────
//
//  ── 出厂数据（`UnityPy` 现读 6 个实例 / 4 个用例，2026-10-26 B8_AB `_tmp_view/b8ab/details.json`）──
//   #  GameObject                所在                    floatProperty1                                  floatProperty2                              changeMaterialOnAnimation
//   1  CardFrame                 battleprefabs_vfxandmisc  _DissolveAmount 0                               （空）                                        **非空**（m_Enabled **0**）
//   2  Booster pack Background   menus                     _Distort_Scale 0                                _Distort_Strength 0                           null
//   3  Rank Icon                 menus                     _Blend 0                                        （空）                                        null
//   4  Division Image            menus                     _Blend 0                                        （空）                                        null
//   5  Level Up Effect           scenes_mainmenuwarpforge  _Reveal **−0.5**                                （空）                                        **非空**
//   6  Level Up Effect Down      scenes_mainmenuwarpforge  _Reveal **−0.5**                                `_Noise_Strength_X_Speed_Y_Scale_Z`         **非空**
//                                                                                                             **vector4 (0.02, −0.1, 8.17, 0)**
//  驱动它们的 legacy clip（5 条，`m_FloatCurves` 的 `attribute` 就是 `floatProperty1.value`）：
//   `Card Hand To Board`（已导入）· `Booster Window - Background Shake On Open`（已导入）
//   · `Rank Up` / `Division Up` / `Level Up Card`（**已在** `WarpforgeVFX/Animations/`，且有运行时副本 `Resources/Animations/` —— 见 `报告/B9_W37CLIP_升级clip时长与路径.md`）。
//   ⚠️ **`CardFrame` 那个实例原版是 `m_Enabled: 0`** ⇒ 它的 `_DissolveAmount` 走的是别处
//      （我们这边 `CardView`/`CardFeel` 硬编码驱动）⇒ **⛔ 别再给它挂本组件**（否则一路写两遍）。
//
//  ── Shell 侧接线（不在本代理白名单，写在这里给接线方）────────────────────────────────
//   `BoosterPackOpenWindow`（`Booster pack Background`）：
//       AnimateUIMaterialProperty.Attach(quad, AnimateUIMaterialProperty.Slot("_Distort_Scale", 0f),
//                                              AnimateUIMaterialProperty.Slot("_Distort_Strength", 0f));
//   段位窗（`RankedDivisionChangeWindow`，`Division Image` + `Division Image/Rank Icon` 两件，各一个组件）：
//       AnimateUIMaterialProperty.Attach(quad, AnimateUIMaterialProperty.Slot("_Blend", 0f));
//   ⚠️ `_Distort_Scale` / `_Distort_Strength` / `_Blend` **我们现有的材质没有这三个属性**
//      （`Sprites/Default`）⇒ 挂上去不会报错、也不会有观感 —— 要真生效得先让那几块图用**原版那个材质**
//      （原版是 `Everguild/UI/…` 系列，运行时能用 `WarpforgeShaderMap.TryResolve` 取到；取不到要**出声**）。
//      本组件对「材质没有这个属性」**不报警**（原版也不报，`Material.SetFloat` 对不存在的属性是静默 no-op）
//      —— 这条**已知**，判据/影响面见报告 §四。
// ════════════════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `AnimateUIMaterialProperty`（`Assembly-CSharp`，无命名空间）。
    /// 把两个「材质浮点/向量属性」由 legacy `Animation` 曲线驱动（曲线的 `attribute` 就是
    /// `floatProperty1.value` / `floatProperty2.vector4Value.x` 这种路径）。</summary>
    [DisallowMultipleComponent]
    public class AnimateUIMaterialProperty : MonoBehaviour
    {
        /// <summary>原版嵌套结构体，**逐字段照抄**（序列化名 = legacy clip 的绑定点，改一个名字曲线就断）。</summary>
        [Serializable]
        public struct AnimatedMaterialPropertyFloat
        {
            /// <summary>材质属性名（原版 `materialProperty`）。</summary>
            public string materialProperty;
            /// <summary>标量值（原版 `value`；曲线绑的就是 `<槽名>.value`）。</summary>
            public float value;
            /// <summary>true ⇒ 走 `SetVector(vector4Value)`，否则 `SetFloat(value)`（原版 `useVector4`）。</summary>
            public bool useVector4;
            /// <summary>向量值（原版 `vector4Value`；曲线可绑 `.x/.y/.z/.w`）。</summary>
            public Vector4 vector4Value;

            /// <summary>原版 `floatPropertyX.Equals(default)` 的等价物（见文件头「🔶 自己设计①」）。</summary>
            public bool IsDefault
            {
                get
                {
                    return string.IsNullOrEmpty(materialProperty)
                        && value == 0f
                        && !useVector4
                        && vector4Value == Vector4.zero;
                }
            }

            /// <summary>原版 `AnimatedMaterialPropertyFloat.Update(Material)` —— 逐句照抄（`useVector4` 分两路）。</summary>
            public void Update(Material material)
            {
                if (material == null) return;
                if (!useVector4) material.SetFloat(materialProperty, value);
                else material.SetVector(materialProperty, vector4Value);
            }
        }

        // ── 原版字段（顺序与名字都照抄）────────────────────────────────────────────
        /// <summary>原版 `initializeOnEnable`（ctor 默认 **true**）。</summary>
        public bool initializeOnEnable = true;
        /// <summary>槽 1（原版 `floatProperty1`）。</summary>
        public AnimatedMaterialPropertyFloat floatProperty1;
        /// <summary>槽 2（原版 `floatProperty2`）。</summary>
        public AnimatedMaterialPropertyFloat floatProperty2;
        /// <summary>克隆材质时的**源**材质（原版 `changeMaterialOnAnimation`；为 null 就用当前那份）。</summary>
        public Material changeMaterialOnAnimation;

        // ── 我们的目标件（原版是 `Image image`）────────────────────────────────────
        /// <summary>要动画的图块。**留空 ⇒ `OnEnable` 时取同一个 GameObject 上的 Renderer**。
        /// （原版 `image` 是序列化引用；本工程 UI 是 `ImageQuad`，它的 `MeshRenderer` 就是那个「image」。）</summary>
        public Renderer target;

        // ── 运行时（原版 `instancedMaterial` / `originalMaterial`）────────────────
        Material _instancedMaterial;
        Material _originalMaterial;
        /// <summary>🔶 自己设计②：`_instancedMaterial` 是不是**我们自己 new 出来的**（决定 OnDestroy 销不销毁）。</summary>
        bool _ownInstancedMaterial;
        bool _warnedNoTarget;

        /// <summary>当前那份克隆材质（原版 `instancedMaterial`）。只读 —— 给接线/自检看。</summary>
        public Material InstancedMaterial { get { return _instancedMaterial; } }
        /// <summary>克隆之前的原材质（原版 `originalMaterial`）。只读。</summary>
        public Material OriginalMaterial { get { return _originalMaterial; } }

        // ==================================================================
        //  接线帮手（原版没有；只为了让 Shell 侧运行时挂载不用填 Inspector）
        // ==================================================================

        /// <summary>造一个标量槽。</summary>
        public static AnimatedMaterialPropertyFloat Slot(string property, float value)
        {
            var s = default(AnimatedMaterialPropertyFloat);
            s.materialProperty = property;
            s.value = value;
            return s;
        }

        /// <summary>造一个 vector4 槽（`useVector4 = true`）。</summary>
        public static AnimatedMaterialPropertyFloat SlotV4(string property, Vector4 value)
        {
            var s = default(AnimatedMaterialPropertyFloat);
            s.materialProperty = property;
            s.useVector4 = true;
            s.vector4Value = value;
            return s;
        }

        /// <summary>运行时接线：给 <paramref name="renderer"/> 所在的 GameObject 挂一个本组件并填好两个槽。
        /// ⚠️ `AddComponent` 会**当场**触发 `OnEnable`（那时槽还是空的）—— 无害：`CreateInstanceMaterial()`
        /// 只是克隆一份同样的材质，随后我们才写槽；`Update` 下一帧就用上了。
        /// 已经挂过（`DisallowMultipleComponent`）⇒ 直接复用并**出声**。</summary>
        public static AnimateUIMaterialProperty Attach(
            Renderer renderer,
            AnimatedMaterialPropertyFloat slot1,
            AnimatedMaterialPropertyFloat slot2 = default(AnimatedMaterialPropertyFloat),
            Material changeMaterial = null)
        {
            if (renderer == null) return null;
            var c = renderer.GetComponent<AnimateUIMaterialProperty>();
            if (c == null) c = renderer.gameObject.AddComponent<AnimateUIMaterialProperty>();
            else Debug.LogWarning($"[AnimateUIMaterialProperty] 《{renderer.gameObject.name}》上已经有一个了 ⇒ 复用并覆盖两个槽（⛔ 别挂第二个）");
            c.target = renderer;
            c.floatProperty1 = slot1;
            c.floatProperty2 = slot2;
            c.changeMaterialOnAnimation = changeMaterial;
            // 🔶 自己设计（非原版）：`AddComponent` 会**当场**触发 `OnEnable → CreateInstanceMaterial()`，
            //   那一刻 `changeMaterialOnAnimation` 还是 null ⇒ 克隆源退化成 `originalMaterial`。
            //   原版这一格是 **Inspector 序列化**的（`OnEnable` 之前就有值）⇒ 原版不存在这个时序问题。
            //   ⇒ 传了 `changeMaterial` 就必须按新源**重建**一次（否则 `Level Up Effect` 那两处 ——
            //   原版 `changeMaterialOnAnimation` 非空 —— 克隆的是错的底材，画面上「改了但不对」）。
            //   谁定的 + 日期：调度台 2026-10-27（B9_W14FX）。
            if (changeMaterial != null) c.RebuildOwnInstanceMaterial();
            return c;
        }

        // ==================================================================
        //  原版七跳（逐句照抄，见文件头）
        // ==================================================================

        void OnEnable()
        {
            if (!initializeOnEnable) return;
            CreateInstanceMaterial();
            if (_instancedMaterial != null) SetTargetMaterial(_instancedMaterial);
        }

        void Update()
        {
            if (_instancedMaterial == null) return;
            Apply(floatProperty1);
            Apply(floatProperty2);
        }

        void OnDisable()
        {
            // 原版 `image.material = originalMaterial;`
            // 🔶 自己设计（非原版）：只在 `_originalMaterial != null` 时回写 —— 原版 uGUI 的
            //    `Graphic.material = null` 会**退回 defaultMaterial（照样可见）**；我们的
            //    `Renderer.sharedMaterial = null` 是**整块不渲染**（画面上直接没了、且零报错）。
            //    为什么必须自己定：判据是 uGUI `Graphic.material` 的 getter 有 defaultMaterial 兜底，我们没有。
            //    谁定的 + 日期：调度台 2026-10-26。
            if (_originalMaterial != null) SetTargetMaterial(_originalMaterial);
        }

        void OnDestroy()
        {
            // 🔶 自己设计②：只销毁我们自己克隆的那份（原因见文件头）
            if (_instancedMaterial != null && _ownInstancedMaterial)
            {
                if (Application.isPlaying) Destroy(_instancedMaterial);
                else DestroyImmediate(_instancedMaterial);
            }
            _instancedMaterial = null;
        }

        /// <summary>原版 `ResetToOriginalMaterial()`。</summary>
        public void ResetToOriginalMaterial()
        {
            if (_originalMaterial != null) SetTargetMaterial(_originalMaterial);
        }

        /// <summary>原版 `SetMaterial(Material)`：`CreateInstanceMaterial(); instancedMaterial = 形参;`
        /// 🔴 **原版不回头改 `image.material`**（见文件头）⇒ 这里也不改，只换 `_instancedMaterial`。</summary>
        public void SetMaterial(Material material)
        {
            CreateInstanceMaterial();
            if (_instancedMaterial != null && _ownInstancedMaterial && _instancedMaterial != material)
            {
                // 我们自己那份被换掉了 ⇒ 交给 OnDestroy 时别再销毁它（它已经不是「我们创造的」了）
                if (Application.isPlaying) Destroy(_instancedMaterial);
                else DestroyImmediate(_instancedMaterial);
            }
            _instancedMaterial = material;
            _ownInstancedMaterial = false;
            if (material != null)
                Debug.LogWarning($"[AnimateUIMaterialProperty] 《{name}》`SetMaterial` 换进来一份**不是我们克隆的**材质" +
                                 $"《{(material != null ? material.name : "null")}》⇒ 按 🔶 自己设计②，`OnDestroy` **不会**销毁它" +
                                 "（原版会销毁别人传进来的材质 —— 我们工程材质是共享缓存，销毁会把同场别的实例打成粉红）。");
        }

        /// <summary>原版 `CreateInstanceMaterial()`（逐句照抄）。</summary>
        void CreateInstanceMaterial()
        {
            if (_instancedMaterial != null) return;                     // 原版：`if (!instancedMaterial)` 才建

            var src = TargetMaterial();
            if (src == null)
            {
                if (!_warnedNoTarget)
                {
                    _warnedNoTarget = true;
                    Debug.LogWarning($"[AnimateUIMaterialProperty] 《{name}》**没有可克隆的材质**" +
                                     "（`target` 为空或那块 Renderer 上没有 `sharedMaterial`）⇒ 本组件什么都不做。" +
                                     "接法：把本组件挂在 `ImageQuad` 同一个 GameObject 上，或先赋 `target`。");
                }
                return;
            }

            _originalMaterial = src;
            var source = changeMaterialOnAnimation != null ? changeMaterialOnAnimation : _originalMaterial;
            var inst = new Material(source);
            inst.name = inst.name + "(Instanced)";
            _instancedMaterial = inst;
            _ownInstancedMaterial = true;
            SetTargetMaterial(inst);
        }

        // ==================================================================
        //  适配层：原版 `image.material` ↔ 我们的 `Renderer.sharedMaterial`
        // ==================================================================

        Renderer TargetRenderer()
        {
            if (target == null) target = GetComponent<Renderer>();     // ImageQuad 就在同一个 GameObject 上
            return target;
        }

        Material TargetMaterial()
        {
            var r = TargetRenderer();
            return r != null ? r.sharedMaterial : null;
        }

        void SetTargetMaterial(Material m)
        {
            var r = TargetRenderer();
            if (r == null || m == null) return;
            // ⚠️ 用 `sharedMaterial`（不是 `material`）—— `material` 会再克隆一份，见文件头。
            // ⚠️ 不碰 `ImageQuad.SetMaterial`：它会顺手改 `mainTexture`/`renderQueue`，而这里的 `m`
            //    是从当前那份克隆出来的（贴图/队列本来就一样），走 SetMaterial 只会多两次无谓写。
            r.sharedMaterial = m;
        }

        /// <summary>原版 `Update()` 里的单槽一跳：**先判空、再按 `useVector4` 分两路**。
        /// （判空口径 = 文件头「🔶 自己设计①」）</summary>
        void Apply(AnimatedMaterialPropertyFloat slot)
        {
            if (slot.IsDefault) return;                 // 原版 `floatPropertyX.Equals(default)` ⇒ 整槽跳过
            if (string.IsNullOrEmpty(slot.materialProperty)) return;   // 🔶 自己设计①：`SetFloat("")` 是 no-op
            slot.Update(_instancedMaterial);
        }

        /// <summary>🔶 自己设计（非原版）：按当前的 <see cref="changeMaterialOnAnimation"/> **重克隆**一份实例材质。
        /// <para>只服务 <see cref="Attach"/> —— 那是「运行时挂载」才有的时序问题（`AddComponent` 先触发
        /// `OnEnable`、槽与 `changeMaterialOnAnimation` 后赋值）。原版靠 Inspector 序列化，不存在这一步。</para>
        /// <para>⚠️ 重建前必须先把**真原件**贴回去：`CreateInstanceMaterial()` 是从当前
        /// `sharedMaterial` 读「原材质」的，若此刻贴的还是上一份克隆，`_originalMaterial` 就会
        /// 记成那份克隆（`OnDisable` 之后还原到克隆 ⇒ 材质泄漏 + 名字多一层 `(Instanced)`）。</para>
        /// <para>谁定的 + 日期：调度台 2026-10-27（B9_W14FX）。</para></summary>
        void RebuildOwnInstanceMaterial()
        {
            if (_instancedMaterial == null || !_ownInstancedMaterial) return;
            var old = _instancedMaterial;
            var trueOriginal = _originalMaterial;
            _instancedMaterial = null;
            _ownInstancedMaterial = false;
            if (trueOriginal != null) SetTargetMaterial(trueOriginal);   // 让 `TargetMaterial()` 读到真原件
            if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            CreateInstanceMaterial();                                    // 这次 source = changeMaterialOnAnimation
        }
    }
}
