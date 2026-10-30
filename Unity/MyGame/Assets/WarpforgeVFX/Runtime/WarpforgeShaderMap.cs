// WarpforgeShaderMap.cs — 原版 shader 名 → 本工程 shader 的映射
//
// 原版自定义 shader 的源码被剥离、无法落成工程资产，所以自建功能等价版顶上。
// 属性名与原版保持一致，原版材质的数值可以直接灌进来，不需要转换。
//
// 解析优先级：
//   1. 自建替代 shader（工程资产，可改、跨平台）  ← 推荐
//   2. 工程自带的同名 shader（URP / Sprites / UI 等标准 shader）
//   3. 原版 shader bundle（工程里没有的 Everguild 自定义 shader 才走这条；
//      bundle 里那份在**编辑器下渲染会出故障**，只适合运行时用）
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeShaderMap
    {
        /// <summary>优先用自建 shader 而不是原版 bundle</summary>
        public static bool PreferBuiltIn = true;

        /// <summary>🔴 **这些原版名改走「原件」** —— 即跳过 <see cref="Replacements"/>，
        /// 直接落到解析链的最后一步（`WarpforgeShaderLoader` 从随包 bundle 取原版 shader 本体）。
        ///
        /// **为什么单开一张白名单，而不是把 <see cref="Replacements"/> 里的条目删掉**：
        /// `Replacements` 与 `EffectExporter.ShaderMap` 按文件头注释**必须同步**，而后者管的是
        /// **导出期占位材质用什么** —— 占位材质**不能**是 bundle 里的 shader（`Shader.Find` 拿不到
        /// 非工程资产）。所以「改走原件」这件事只该影响**运行时最终解析**，不该动导出期那张表。
        /// 单开白名单还让这件事**一句话可回滚**（把名字从这张表里删掉即可）。
        ///
        /// **怎么定的这些名字**：`资料/普查产出_0918/shader原件可用性_表.md`
        /// （由 `工具/survey_shader_originals.py` 实读两个 bundle 生成）。
        /// 入选条件 = ① 是 **Everguild / Shader Graphs 的自定义 shader**（不是 Unity 内建）②
        /// **原件确实在两个随包 bundle 之一里**。
        /// ⚠️ **Built-in 管线的老 shader（`Mobile/Particles/*` · `Legacy Shaders/Particles/*` ·
        /// `Particles/*` · `Sprites/*` · `UI/Default`）不在名单里，也永远不该进** ——
        /// 它们在 URP 工程里本来就渲染不了，自建替代才是对的。
        ///
        /// ✅ **2026-09-18 用户取消版权红线**（个人学习用途）⇒ 走原件 = 把原版编译字节码打包带走，**不再要求发布前处理**。
        ///    （原写「与 `Resources/Art/` 同一条红线」—— 那条已取消，见 `项目任务.md` §二「版权」。）</summary>
        /// <summary>🔬 **诊断开关：`WFBIND_FORCE_BUILTIN` → 白名单失效，里面的名字也走自建替代。**
        ///
        /// **为什么要它**：白名单（下面那张 `UseOriginal`）把那些原版名都导去**原件**了 ——
        /// 好处是保真，代价是**我们自建的那一份平时根本渲不到** ⇒ 「我们那份算式对不对」**没有尺子可量**
        /// （改了只能靠读指令自证，而那正是 2026-10-02 想摆脱的东西）。
        /// 打开它，**同一个材质就能分别用【原件】和【我们那份】各渲一遍**，逐像素比才是判据。
        /// 这也是坐实「`cb1[i]` ↔ 属性名」的唯一手段 —— DXBC 段没有 `RDEF`，名字读不出来，**只能试了再比**。
        ///
        /// **取值**（2026-10-02 第二轮加过滤，否则一次会同时翻掉好几族、没法归因）：
        ///   · **不设** → 关（默认）
        ///   · `1`    → **全开**（白名单里每个名字都走自建）
        ///   · 其它   → **逗号分隔的子串清单**，只对这些原版名生效。
        ///     例：`WFBIND_FORCE_BUILTIN="Matcap"` 只翻 matcap 那几条 —— **单变量 A/B 靠它**。
        /// ⚠️ **默认关**，只给探针用（`EffectIso` / `EffectSweepBatch` 那条路）；**别在正常跑里开**。</summary>
        public static readonly string ForceBuiltIn =
            System.Environment.GetEnvironmentVariable("WFBIND_FORCE_BUILTIN") ?? "";

        /// <summary>这个原版名要不要绕过白名单、强制走自建替代。取值语义见 <see cref="ForceBuiltIn"/>。</summary>
        public static bool ForceBuiltInFor(string originalName)
        {
            if (string.IsNullOrEmpty(ForceBuiltIn)) return false;
            if (ForceBuiltIn == "1") return true;
            foreach (var t in ForceBuiltIn.Split(','))
            {
                var s = t.Trim();
                if (s.Length > 0 && originalName != null &&
                    originalName.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>🆕 2026-10-31（B104 · **287 近似 shader 的 A/B 实验开关，默认关**）。
        /// 取值语义与 <see cref="ForceBuiltIn"/> 对称：不设 = 关 · `1` = 全开 · 其它 = 逗号分隔子串清单。
        /// 打开后：`TryResolve` 对命中的名字**先问原版 bundle**，取到就用原件（取不到则照旧回落）。
        /// ⚠️ 只给 `EffectSweepBatch` / 探针做「原件 vs 我们那份」的同一把尺子 A/B；⛔ 别在正常跑里开
        /// （bundle shader 在编辑器里对部分效果会渲成品红，那是已知现象）。</summary>
        public static readonly string PreferOriginal =
            System.Environment.GetEnvironmentVariable("WF_PREFER_ORIGINAL") ?? "";

        /// <summary>这个原版名要不要优先走原件（A/B 实验）。语义见 <see cref="PreferOriginal"/>。</summary>
        public static bool PreferOriginalFor(string originalName)
        {
            if (string.IsNullOrEmpty(PreferOriginal)) return false;
            if (PreferOriginal == "1") return true;
            foreach (var t in PreferOriginal.Split(','))
            {
                var s = t.Trim();
                if (s.Length > 0 && originalName != null &&
                    originalName.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        public static readonly HashSet<string> UseOriginal = new HashSet<string>
        {
            // ---- 2026-09-18 第一批（原件在随包 bundle 里、且我们原本用的是「按名字挑的近似」）----
            "Everguild/Matcap/Matcap Full Options",                    // 120 条效果
            "Everguild/Matcap/Matcap With Texture",                    //  70
            "Everguild/FX/Unlit UV scroll",                            //  39
            "Everguild/FX/Multi Ray",                                  //  33
            "Everguild/FX/Alpha Mask One Layer",                       //  29
            "Everguild/FX/Particle Dissolve Mask",                     //  29
            "Everguild/FX/Particle Shine Custom Vertex Streams",       //  26
            "Everguild/FX/Particle Premultiply Greyscale Coloring",    //  25
            "Everguild/FX/Particle Premultiply",                       //  12
            "Shader Graphs/Fx_ParticleDissolve_apb",                   //  11
            "Everguild/FX/Alpha Masks Two Layer",                      //   9
            "Everguild/FX/TrailShader_1",                              //   8
            "Everguild/UnlitAmbient",                                  //   8
            "UI/Additive",                                             //   7
            "Shader Graphs/Fx_RockDissolve",                           //   4
            "Shader Graphs/Doomweaver effect",                         //   3
            "Shader Graphs/Eclipse Tau",                               //   3
            "Everguild/FX/TrailShader_Fading",                         //   2
            "Everguild/Sprites/Sprite Additive",                       //   1
            "Everguild/UnlitAmbient Emissive Flickker",                //   1
            "Everguild/Unlit Wind",                                    //   0
            // ---- 🆕 2026-10-02：**`_missing_shaders_audit` 里「掉兜底」的那三件** ----
            // 🔴 来由：`Buff_DA_Forest_Self` 的 `Smoke Trails` 槽在 `EffectIso` 里差 **2.99×/2.70×**，
            //    一路查到**拖尾材质**（`ParticleSystemRenderer.trailMaterial`，渲染器 `m_RenderMode=5(None)`、
            //    只画拖尾）：原版 `Spiral Trail FX Smoke` 的 shader = **`Everguild/FX/Spiral Trail FX`**，
            //    `WarpforgeShaderMap` 里**一条都没有它**（`工具/_missing_shaders_audit.py` 把这三件
            //    都标成「未映射 → **掉兜底：原版 bundle**」，合计 **24 条效果**）。
            //    三件**都在 `wf_shaders_extra.bundle` 里**（42 件逐名实读）⇒ 进白名单 = 走「用原件」这条既定机制。
            // ⚠️ **诚实标注**：解析链最后一步本来也会去 bundle 取（见下面 Cards 那段注释），
            //    所以这一条**可能只是把「要用原件」写成数据 + 让 `ShaderResolveProbe` 的白名单断言盯住它**；
            //    **渲染是否因此改变，以 2026-10-02 的 iso 实测为准**（结果 → `资料/特效还原_进度与交接.md` §P1-a0·附二）。
            "Everguild/FX/Spiral Trail FX",                            //   9（Buff_DA_Forest_Self / BulletImpact_Sororitas_MissileVolley …）
            "Everguild/FX/ShieldVfx",                                  //  10（ArmourEffect_Bladeguard_Shield / Axe_Slash_User_SW …）
            "Everguild/FX/Glow Shader",                                //   5（Goff_ProperKilly / Sword_Slash_DA …）
            // ---- 🆕 2026-10-01 晚：**卡牌那一族**（`Everguild/Cards/*`）----
            // 🔴 **来由**：`RemnantBody3D Aeldari` 在台账里 **16.39×（E 组）** —— 一路查下去，
            //    **真因在【网格】那边**（`EffectExporter.ImportMesh` 对流式顶点数据的网格拷不出数据，
            //    见 `资料/特效还原_进度与交接.md` §〇之五 与 `资料/已知的坑.md`），**不是 shader 解析**。
            // ⚠️ **这两条其实是「把已有行为写明」**：它们**没有** `Replacements` 条目
            //    （那是「改走自建替代」才会进这张白名单的理由），所以就算不写在这里，
            //    解析链本来也会走到最后一步从 bundle 取原件 —— 实测加不加它，渲染结果**逐位相同**。
            //    ⇒ 留着是为了① 把「这两件要用原件」的意图写成数据、② 让 `ShaderResolveProbe`
            //    的白名单断言盯住它。**别把它当成那条 16× 的修法**。
            "Everguild/Cards/Gem Crystal Glitter Explosion",           //   1
            "Everguild/Cards/Gem Crystal Glitter",                     //   4
            // ---- 🆕 2026-09-19：Unity **内置管线**那批（原件在 `wf_builtin.bundle`，包里 15 个里的这 8 个）----
            // 这批原来按「Built-in 老 shader 在 URP 工程里本来就渲染不了」自建替代 ——
            // 而**那条论断从没实测过**（`项目任务.md` ⛔ 行 ④ 原文：「可验的一条路 = 直接挂一次看渲不渲得出」）。实测三条：
            //   ① 原版把内置 shader 的**编译产物**打进了自己的包（构建版本 `6000.2.6f2`；本工程 6000.3.23f1，同一大版本）
            //   ② 这批**全是 unlit 粒子 shader**、pass 上没有 URP 需要的光照 tag ⇒ 在 URP 下照常渲染
            //   ③ 材质上的 `_TintColor`/`_InvFade` 这类属性**只有原件才认**，自建替代一律当死值丢掉
            // 影响面（`普查产出_0917/效果_shader对账.tsv` 的引用效果数）：
            //   Additive 423 · Alpha Blended 159 · Standard Unlit 17 · Legacy Additive 19
            //   Legacy Premultiply 9 · Legacy Anim 8 · Legacy Alpha Blended 4 · Multiply 3 = **642 处**
            // ⚠️ `Particles/Additive` **不在**这里 —— 它在原版包里根本不存在（死条目，引用数也是 0），
            //    加进来只会让 `ShaderResolveProbe` 的白名单断言变红。
            // ⚠️ 这 8 条**必须靠下面的解析顺序改动才生效** —— 它们的名字在编辑器里 `Shader.Find` 也找得到，
            //    不换序就会一直拿到引擎自带的那一份（= URP 下渲不出来的那一份），白名单形同虚设。
            "Mobile/Particles/Additive",                               // 423
            "Mobile/Particles/Alpha Blended",                          // 159
            "Mobile/Particles/Multiply",                               //   3
            "Particles/Standard Unlit",                                //  17
            "Legacy Shaders/Particles/Additive",                       //  19
            "Legacy Shaders/Particles/Alpha Blended",                  //   4
            "Legacy Shaders/Particles/Alpha Blended Premultiply",      //   9
            "Legacy Shaders/Particles/Anim Alpha Blended",             //   8
            // ---- 🆕 2026-09-26：**平面反射的两台 Blit shader**（原件在 `wf_arena_mirror.bundle`）----
            // 为什么会漏到现在：它们**只在 `globalgamemanagers.assets` 里**（裸 SerializedFile，
            // 运行时加载不了），既不在主包里也不在任何一个战场包里 ⇒ 逐包实读都没命中，
            // 于是 `ArenaMirror` 只能「双线性降采样+升采样当模糊」，**一级 Blit 都没有**
            // ⇒ 反射比原版亮（`aeldari` 1.016→1.041 · `tauviorla` 1.028→1.032）。
            // 抽取工具 = `工具/extract_mirror_shaders.py`（源是裸文件，做法与两个坑写在那个脚本文件头）；
            // 判据全文 → `资料/战场13场_逐场对账_0920.md` §一 ①-j **结论八**。
            "Hidden/Everguild/PlanarReflectionsBlit",
            "Hidden/Everguild/GrainyBlurForPlanarReflections",
            // ---- 🆕 2026-10-02：「掉兜底」那 28 件整批收口 —— **剩下的 23 件** ----
            // 🔴 **来由与就地订正**：`项目任务.md` §三 第 4 条原来记着
            //    「28 个未映射；**只在主包里的**那几件要 `extract_missing_shaders.py` **抽进补充包**，
            //      否则**运行时解析不到 = 真缺口**」—— **2026-10-02 逐条核实：后半句是错的**（铁律 5）。
            //    四条实据：
            //      ① `工具/_missing_shaders_audit.py` 今天实读：33 件里 **0 件**落进「未映射：占位材质保留」；
            //      ② `ShaderFallbackProbe.Run`（2026-09-18 实跑，日志 `d:/4/_tmp_view/shaderfallback.log`）：
            //         **28/28 解析到原版 bundle · `isSupported` 全 true · 洋红 0 个**，收尾 `=== 通过 ===`；
            //      ③ `WarpforgeShaderLoader.EnsureLoaded` 加载的是**四组**包 —— 主包 `wf_shaders.bundle`
            //         （= `shaders_assets_all.bundle` 的**逐字节副本**）**和**补充包**都在内**
            //         ⇒ 「只在主包里的」那 5 件（`Burning` / `Necrons Rays` / `Card Highlight And Shadow` /
            //         `Halo UV scroll` / `Vortex`）本来就在 `_byName` 里；
            //      ④ `TryResolve` 的**最后一步**本来就是「去 bundle 取」。
            //    ⇒ **这 23 件一直是「在用原件」，从没掉到占位材质。** 写进白名单**对渲染是零改动**
            //      （与 10-01 那两条卡牌、10-02 那三条 `Spiral Trail FX`/`ShieldVfx`/`Glow Shader` 同理），
            //      它买到的是两件事：
            //        ① 把「这几件要用原件」写成**数据**，而不是靠解析链最后一步**静默**兜着；
            //        ② 让 `ShaderResolveProbe` 的白名单断言**自动盯住它们**（那个探针逐条遍历本表）。
            //    ⚠️ **仍未验到的**（照抄 `资料/普查产出_0917/补充shader引用清单.md` 的如实标注）：
            //      `ShaderFallbackProbe` 只证明了「**解析得到 + 不是故障色**」；「**画出来的东西对不对**」
            //      要靠 sweep / `EffectIso` 的台账，而且那支探针的像素分类器本身写错了
            //      （拿线性 0.25 比 sRGB 读回的 0.54）。
            //    ⚠️ **为什么不是「抽进补充包」**：白名单那条路走的是
            //      `WarpforgeShaderLoader.TryGetShader`，它在**所有已加载的包**里找
            //      ⇒ 不关心 shader 落在主包还是补充包，抽包是多余的。
            //    ⚠️ **为什么这 23 件与上面那些不一样**：它们**没有** `Replacements` 条目（本来就走原件），
            //      所以进白名单**不改变解析结果**，只是把它**声明出来**。
            //    影响效果行数（09-13 口径 → `资料/普查产出_0917/补充shader引用清单.md`，合计 **103 处**）：
            "Everguild/FX/Rays For Trail",                    //  42
            "Spine/Special/HiddenPass",                       //  23
            "Everguild/FX/Burning",                           //   4（原件在主包）
            "Everguild/FX/FX Shine For Animation",            //   4
            "Everguild/FX/Burning Dissolve",                  //   3
            "Everguild/FX/Specific/Necrons Rays",             //   3（原件在主包）
            "Everguild/Matcap/Matcap Full Options VAT",       //   3
            "Everguild/Cards/3D Card Explosion",              //   2
            "Everguild/Cards/BlobShadow",                     //   2
            "Everguild/FX/MarkerLight",                       //   2
            "Everguild/Wind Matcap",                          //   2
            "GlassRefraction",                                //   2
            "Custom/EditorIcon",                              //   1
            "Everguild/Cards/3D Card",                        //   1
            "Everguild/Cards/Card Swarm Effect",              //   1
            "Everguild/Cards/Necrons Base Death",             //   1
            "Everguild/Cards/Shatter Inner Pieces",           //   1
            "Everguild/FX/Card Highlight And Shadow",         //   1（原件在主包）
            "Everguild/FX/Card Remnant Death Icon",           //   1
            "Everguild/FX/Halo UV scroll",                    //   1（原件在主包）
            "Everguild/FX/Particle Dissolve Premultiply",     //   1
            "Everguild/FX/Specific/Pray Glow",                //   1
            "Everguild/FX/Vortex",                            //   1（原件在主包）
            // ---- 🆕 2026-10-19：**`Everguild/FX/Extra Color`（741 处引用）整条改走原件** ----
            // 🔴 **为什么改**：它原来落在 `Replacements` → 我们那份 `WarpforgeVFX/Particles/Extra Color`，
            //    而那一份**同时顶了 8 个原版 shader**（内置族 + EC 族），只能按内置族的算式写（`tex × vcol`）；
            //    而原版 EC 这一族比它**多一跳** —— 32/32 个 VS 变体都写
            //    `o2.xyz = LinearToSRGB(_Color.rgb) × vcol.rgb`，12 个 Universal Forward PS 变体全都把 `v2`
            //    （= o2）乘进最终色 ⇒ **原版最终色 = `tex × LinearToSRGB(_Color) × vcol`**，我们少乘 `_Color` 那一项
            //    ⇒ 这一族**系统性偏暗**。判据全文 → `资料/普查产出_第二十三会话/V23_特效ExtraColor的PS读数.md`
            //    §一/§二（槽位铁证：`cb2[1]` = `_Color`，Unity 序列化反射 `off=16` ⇒ slot 1）。
            // 🔴 **为什么走原件而不是再单开一份自建**：原件**就在随包 `wf_shaders.bundle` 里、带编译字节码**
            //    （V23 §二 实读 `shaders_assets_all.bundle`，与 `wf_shaders.bundle` 逐字相同）
            //    ⇒ 绑原件 = **零猜测、精确复刻那一跳**，741 处一次到位，且与下面 23 件「本来就走原件」同形。
            // ⚠️ **`Replacements` 里那一条【保留不动】** —— 理由见那条自己的注释（导出期占位 + 兜底路）。
            // ⚠️ **生效范围**：这一族几乎全在 `UseOriginal` 里 ⇒ **只在正常态生效**；原件包不在场时
            //    （`TryResolve` 落到 `Replacements`）仍走我们那份 —— 那半边本来就按内置族写，**不是这次能修的**。
            // ⚠️ **仍未跑的验证**（本轮不起 Unity）：`ShaderFallbackProbe` 的「不品红」与 `EffectCompare` 的 A/B
            //    —— 命令写在 `资料/普查产出_1018/W_特效线_ExtraColor与UnlitAmbient.md` §三。
            "Everguild/FX/Extra Color",                                // 741
            // ---- 🆕 2026-10-22（W64 · 兜底审计 **B11** 第 5 条）：**TMP 的 `Distance Field Offset`** ----
            // 🔴 **来由**：它原来只有一条 `Replacements` 近似（→ 标准 `TextMeshPro/Distance Field`），
            //    注释自承「拿不到原版那张 shader 的属性表，按名字挑最接近的」。
            //    现核：原版**编译好了的那一份就在字体包** `fonts_assets_all.bundle` 里
            //    （4 个 `TextMeshPro/*` 之一，`m_ParsedForm.m_Name` 逐名实读），只是**不在任何我们有加载的包里**
            //    ⇒ 走 `extract_missing_shaders.py --tmp` 重打成 `wf_tmp_shaders.bundle`，由
            //    `WarpforgeShaderLoader.TmpBundleRelPath` 加载。**走原件 = 零猜测**。
            //    ⚠️ 与上面那些 Everguild 名字不同：这一件是 **Unity 包 shader**，走原件的唯一顾虑是
            //      「URP 下能不能渲」—— 原版自己就是 URP 6000.2.6f2 的工程，用的就是这一份，**不需要我们再判断**。
            "TextMeshPro/Distance Field Offset",                       //   1（`CardPrefab.prefab` 的 4 条描边材质）
            // ---- 🆕 2026-10-22（W64 · 兜底审计 **M6**）：**`Particle Distortion Affect Transparents` 改走原件** ----
            // 🔴 **来由**：它是白名单里**最后一条「⏸ 暂缓的大头」**（原文：「改它会一次性动 233 条效果，
            //    要单独一批 + 一次全量 sweep 量过再动」）。现在动它的依据：
            //    ① 原件**就在随包主包** `wf_shaders.bundle` 里（UnityPy 实读 45 个 Shader 对象含这一条，
            //       与 2026-10-19 `Extra Color` 741 处**同形**）；
            //    ② `Replacements` 里那条指向的是**自建重建** `WarpforgeVFX/FX/Distortion`
            //       （`WFDistortion.shader` 文件头自承：原版 ShaderGraph 的 HLSL 与字节码**都被剥了**，
            //        只按属性表 + pass 状态重建）。
            //    ⇒ 走原件 = **零猜测**。判据全文 → `资料/普查产出_1022/W64_素材导出缺口.md` §一 M6。
            // ⚠️ **`Replacements` 里那一条【保留不动】** —— 与上面 `Extra Color` 同款理由：
            //    ① **导出期占位材质要用**（占位材质不能是 bundle 里的 shader，`Shader.Find` 拿不到非工程资产）；
            //    ② **原件包不在场时的兜底路**要用（新克隆没有 `StreamingAssets/WarpforgeVFX/`）。
            //    ⚠️ `TryResolve` 里白名单排在 `Replacements` **前面**、且**取件失败才回落** ⇒ 进白名单**不改变兜底路**。
            // 🔴 **影响面 = 233 个 prefab 引用**（判据：`grep -rl "Particle Distortion Affect Transparents"
            //    d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/ | wc -l`，2026-10-22 现读 = **233**）。
            // 🔴 **⚠️ A/B 尚未跑**（本轮不起 Unity）：这 233 条效果的**画面可能整体变化** ——
            //    原件这一族是真·抓屏扭曲（`_DistortTex` 偏移重采样场景色），我们的重建版是另一套同名近似算式。
            //    ⏭ 待办：跑一次 `WFSWEEP_SIDE=both` 的 sweep（`EffectCompare` / `EffectSweepBatch`）量差异。
            "Everguild/FX/Particle Distortion Affect Transparents",    // 233
            // ---- 🆕 2026-10-22（W64 · 兜底审计 **M7**）：**两条 `Everguild/Cards/*` 把已有行为写成数据** ----
            // 🔴 **来由**：这两条原来**两张表里都没有**（既不在 `Replacements`、也不在 `UseOriginal`）
            //    ⇒ 只靠 `TryResolve` **最后一步**「去已加载的包里取原件」**静默**兜着（各 1 条效果）。
            //    ⚠️ **写进白名单不改变解析结果**：工程里没有任何 `.shader` 资产用这两个名字
            //    （2026-10-22 全仓 `grep 'Shader "…"'` 现读为空），`Shader.Find` 对这类私有名本来也返回 null
            //    ⇒ 加不加它，**渲染逐位相同**（与 2026-10-01 那两条 `Gem Crystal Glitter*` 同款判定）。
            //    买到的是：① 把「这两件要用原件」写成**数据**；② 让 `ShaderResolveProbe` 的白名单断言**盯住它们**。
            //    判据全文 → `资料/普查产出_1022/W64_素材导出缺口.md` §一 M7。
            "Everguild/Cards/3D Card Blend Image",                     //   1（`AmbushEffect`）
            "Everguild/Cards/3D Card Stealth",                         //   1（`StealthEffect`）
            // ---- 🆕 2026-10-29（B97_W23 查实 / B97_W29 落地）：**卡包外观** ----
            // 🔴 **来由**：16 个 `Card Booster Pack*` 材质的 shader = 这一条，而它**没有 `.shader` 源**
            //    （ShaderGraph 生成体：`m_Script = null`，只有 `compressedBlob` 里的编译字节码）
            //    ⇒ 抄不成工程资产；构件**不属 addressable**，`LoadAsset<Shader>` 对它取不到。
            //    ⇒ 走 `extract_missing_shaders.py --booster-shaders` 重打成 `wf_booster_shaders.bundle`，
            //    由 `WarpforgeShaderLoader.BoosterShaderBundleRelPath` 加载。**走原件 = 零猜测**。
            // ⚠️ 与上面那两条 `Everguild/Cards/*` 不同：这一件**真的有材质在用它**（16 份），不是「把已有行为写成数据」。
            // ⚠️ 本工程另一条取件路 `CardPresentation.BoosterPackAssets`（经 prefab 传递）**先于**本表存在，
            //    两条并存不是重复：那条要的是 prefab / 整块材质模板，这一条要的是**全局可按名取的 Shader**。
            //    `B97W23_Assert` 第 2 段 / `B97W29_Assert` 分别钉住两边的实读数。
            // ⚠️ **能不能在这台 6000.3.23f1 上 `SetPass` 还没验**（源包构建版 6000.2.6f2，与其它 64 个已在用的
            //    原版 shader 同一批）⇒ 判据只能跑一次 Unity 的 `B97W23Assert.Run`（本轮不起 Unity）。
            "Everguild/Cards/Shader Graphs/Booster Pack",              // 16（`Card Booster Pack*`）
        };

        /// <summary>原版 shader 名 → 本工程 shader 名</summary>
        public static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            // 自定义 ShaderGraph shader —— 源码被剥离，自建替代
            // 🔴 **2026-10-19 晚（T02 的持久修法）：EC 单开一份自建变体。**
            //    上面 `UseOriginal` 让正常态走原件；原件包不在场时（兜底路）落到**本表**。
            //    而 EC 那一族比内置族**多一跳** `LinearToSRGB(_Color)`（判据 → `WFParticlesExtraColorEC.shader`
            //    文件头 + `资料/普查产出_第二十三会话/V23_特效ExtraColor的PS读数.md`）——
            //    原来这一条和 7 个内置管线名**共用** `WarpforgeVFX/Particles/Extra Color`（`tex × vcol`），
            //    兜底路上 EC 族因此系统性偏暗（`_Color=4` → 只有原版 0.548；`766.996` → 0.060）。
            //    ⇒ 拆成两份：EC 走 `… Extra Color EC`（含 `LinearToSRGB(_Color)`），
            //    内置族继续走 `… Extra Color`（`_Color` 对它们是**死值**，乘了会弄坏 —— 2026-10-02 那次 A/B 就是这一手）。
            //    ⚠️ 本表与 `EffectExporter.ShaderMap` 必须同步（那边管导出期占位材质）。
            //    ⚠️ 新 shader 要重跑一次 `WarpforgeSetup.RegisterShaders` 才进 Always Included（它遍历本表的值）。
            { "Everguild/FX/Extra Color", "WarpforgeVFX/Particles/Extra Color EC" },
            // 本质是 Unity 内置 Sprites/Default 的加法版（7 个属性、Blend One One 写死）。
            // 不映射的话会掉到默认的 URP 粒子 shader —— 属性名对不上，_MainTex/_Color 全丢，
            // 粒子直接渲染成空（ArdAsNailsEffect 的 Back Glow 就是这么没的）
            { "Everguild/Sprites/Sprite Additive", "WarpforgeVFX/Sprites/Additive" },
            // 抓屏扭曲，用 _DistortTex 偏移重采样场景颜色。影响 233 个效果，是第二大缺口
            { "Everguild/FX/Particle Distortion Affect Transparents", "WarpforgeVFX/FX/Distortion" },
            // matcap：用视图空间法线采样「从正面看一个球」的贴图，不需要实时光照。
            // 两个原版 shader 属性表一样，一个 shader 顶两个
            { "Everguild/Matcap/Matcap Full Options", "WarpforgeVFX/Matcap/Matcap" },
            { "Everguild/Matcap/Matcap With Texture", "WarpforgeVFX/Matcap/Matcap" },

            // 🆕 2026-10-01：`Hidden/LUTBlender` —— **全屏 LUT 混色**（`WFModulePostProcess`，52 实例）。
            //   它和上面那些**形状不一样**：**没有任何材质用它**（不是烘在 prefab 上的），
            //   是**下游在运行时 `new Material`** 出来的（原版 `LUTBlender.instanceMaterial`）
            //   ⇒ 所以它**不会**出现在 `EffectExporter.ShaderMap` 里（那边只管「导出期占位材质」，
            //     而这条链没有占位材质这一步）—— 两份表**这一条是特例**，别照抄过去。
            //   🔴 **为什么自建而不是用原版那份编译字节码**：bundle 里的 shader 在**编辑器 / 批处理下
            //     渲染会出故障**（整片品红，见 `TryResolve` 下面那段注释）—— 而**自检就跑在批处理里**。
            //     ⚠️ **2026-10-23（`W65`·`R1`）：「原版在批处理下渲品红」这条理由本身【没核过】**
            //     —— 它要真渲一次才知道（`资料/普查产出_1023/E4_R29分因与R1判据.md` §③-4 / §2·3）。
            //     而且反证已经出现：`UseOriginal` 里 64 条（含 `Mobile/Particles/*`、`Hidden/Everguild/*`）
            //     全都在走 bundle 原件。⇒ **A/B 之前不改这一条**（没量过就改 = 有可能让
            //     `BattlePostFx` 整条后期失效）；A/B 方案（`LutBlendProbe.Run` + 7 条断言）
            //     在同报告 §④-1，⛔ 要起 Unity，留给主对话。
            //   算式是**反汇编读出来的**（`o = lerp(_LUT1, _LUT2, _Blend)`），不是猜的：
            //   `WarpforgeVFX/Shaders/WFLUTBlender.shader` 文件头 + `资料/普查产出_1001/资产导入路三件_侦察.md` §3·5·a。
            //   🆕 **2026-10-23**：自建那份的 **VS 少了 UV 的 `ST` 一跳**（原版 `mad o0.xy, v1.xyxx, cb0[2].xyxx,
            //     cb0[2].zwzz`）已补 —— 单位 ST 下渲染逐像素不变，只影响非单位 ST 的情形。
            //     判据 → `WFLUTBlender.shader` 文件头 2026-10-23 那段订正。
            { "Hidden/LUTBlender", "WarpforgeVFX/LUTBlender" },

            // ---- 🆕 2026-09-13 第三十三轮补的 10 个 ------------------------------------
            // ⚠️ 这张表与 `EffectExporter.ShaderMap` **是两份，改一份必须同步另一份**
            //    （那边管「占位材质用什么 + 报告标不标近似」，这边管「运行时重建材质解析到谁」）。
            // 这 10 个原来两边都没有 ⇒ 运行时 `Shader.Find` 也找不到 ⇒
            // `WarpforgeEffectBinder` 返回 null ⇒ 那个材质槽**保留占位材质**（16 条效果）。
            // ⚠️ **全部是近似**：我们拿不到这些 shader 的属性表，只按名字挑最接近的自建 shader
            //    （真实的溶解 / UV 滚动 / 顶点流**没做**）。
            // 🆕 2026-10-02（兜底路第 2/3/4 族）：这三个原版 shader 已按 DXBC **逐条重写**成专用自建 shader
            //    （原来都指向只乘的通用 `Extra Color` ⇒ 兜底路上差 2~9 倍）。判据 → 各自 shader 文件头。
            { "Everguild/FX/Particle Dissolve Mask",                  "WarpforgeVFX/FX/ParticleDissolveMask" },
            { "Everguild/FX/Alpha Mask One Layer",                    "WarpforgeVFX/FX/AlphaMaskOneLayer" },
            { "Everguild/FX/Alpha Masks Two Layer",                   "WarpforgeVFX/FX/AlphaMasksTwoLayer" },
            { "Everguild/FX/TrailShader_1",                           "WarpforgeVFX/FX/TrailShader1" },
            { "Everguild/FX/Multi Ray",                               "WarpforgeVFX/FX/MultiRay" },
            { "Everguild/FX/Particle Shine Custom Vertex Streams",    "WarpforgeVFX/FX/ParticleShineCVS" },
            { "Everguild/FX/Particle Premultiply Greyscale Coloring", "WarpforgeVFX/FX/ParticlePremultiplyGreyscale" },
            // 🆕 2026-09-15：C 组「导出整个丢了」的 `Explosion_Ground` 用的就是这一个。
            // 🔴 **2026-09-17 更正：这里原来写「它不在任何我们随包走的 shader 包里」—— 那条是错的。**
            //    实读 `wf_shaders_extra.bundle` 的 `m_Container`（共 42 条）：`Everguild/FX/Particle
            //    Premultiply` **就在里面**（与 `…/Particle Dissolve Premultiply` 是两个不同的
            //    Shader 对象，前者带编译字节码、不是桩）。
            //    **错因**：当时是拿**裸字节 grep** 这个包的 —— 而它的数据块是 **LZ4 压缩**的，
            //    42 条容器名里**只有 6 条**能在裸字节里搜到（这一条恰好搜不到，**已实测复现**）
            //    ⇒ 「搜出 0 命中」被当成了「不存在」。同铁律 2：**「没找到」≠「不存在」**。
            //    ⇒ 「`Shader.Find` 返回 null ⇒ 材质槽保留占位材质 ⇒ 整块渲不出来」这个**根因要重查**；
            //    目前更像踩的是 `资料/特效还原_进度与交接.md` 里那条
            //    「导出那趟加载源包会把补充包顶掉」。
            //    ⚠️ 顺带一条**还没做的改进**：既然原版 shader 就在我们随包的补充包里，
            //    这一条（以及同类「近似替代」）其实可以试着改走**原版 shader 本体**。
            // 🔴 **2026-10-22（W64 · 兜底审计 B11）：这三条「近似」已销账** —— 原版编译字节码已被
            //    Ruri 反编译出来（`资料/普查产出_1020/W58_工具线_Ruri实测.md`），按字节码**逐条重写**成：
            //      `Particle Premultiply` → `WarpforgeVFX/FX/ParticlePremultiply`
            //      `TrailShader_Fading`   → `WarpforgeVFX/FX/TrailShaderFading`
            //      `Doomweaver effect`    → `WarpforgeVFX/FX/DoomweaverEffect`
            //    判据与逐条指令 → 各自 `.shader` 的文件头（`Assets/WarpforgeVFX/Shaders/WF*.shader`）。
            //    ⇒ 这一族**不再有「按名字挑最接近的」那种近似**。
            { "Everguild/FX/Particle Premultiply",                    "WarpforgeVFX/FX/ParticlePremultiply" },
            { "Everguild/FX/Unlit UV scroll",                         "WarpforgeVFX/FX/UnlitUVScroll" },
            { "Everguild/FX/TrailShader_Fading",                      "WarpforgeVFX/FX/TrailShaderFading" },
            { "Shader Graphs/Doomweaver effect",                      "WarpforgeVFX/FX/DoomweaverEffect" },
            // 🆕 2026-09-15：**同一条根因的另外三个**（`Explosion_Ground` 那次只补了上面一行）。
            //    这三张 ShaderGraph 原版是**自定义 shader 图**，既不在我们随包的 shader 包里、
            //    也没有映射表条目 ⇒ `Shader.Find` 返回 null ⇒ 材质槽保留占位材质 ⇒ **整块渲不出来**。
            //    影响面（`grep Prefabs/*.prefab` 实测）：
            //      · `Fx_ParticleDissolve_apb` —— **13 个 prefab**，含 C 组剩下的 `BlastEffect`
            //      · `Fx_RockDissolve`         —— **4 个 prefab**，含 C 组剩下的 `BlastEffect`
            //      · `Eclipse Tau`             —— 3 个 Tau 环境效果
            //    ✅ **2026-10-22 起这一族没有「近似」了**（铁律 5：原来这里写着「⚠️ 近似：拿不到 ShaderGraph
            //    属性表，按名字挑最接近的自建 shader」—— 三件都已按 Ruri 反编译的字节码逐条重写：
            //    `Fx_ParticleDissolve_apb` → `…/ParticleDissolveAPB`、`Fx_RockDissolve` → `…/RockDissolve`、
            //    `Eclipse Tau` → `…/EclipseTau`）。
            //    补完要**重跑全量扫描重出台账**看 C 组是不是清零（别只看这三个效果）。
            // 🆕 2026-10-02（兜底路第 2 族）：`Fx_ParticleDissolve_apb` 原来也指到 `Extra Color`（只乘的通用版），
            //    而原版是**深度的软粒子淡出 + 双次采样遮罩**（逐条指令 → 新 shader 的文件头）
            //    ⇒ 兜底路上 `BlastEffect` ×7.65 · `Invoke Minion *` ×4.4~4.5。判据 → `资料/比对基线/兜底路审计_1002.tsv`。
            { "Shader Graphs/Fx_ParticleDissolve_apb",                "WarpforgeVFX/FX/ParticleDissolveAPB" },
            { "Shader Graphs/Fx_RockDissolve",                        "WarpforgeVFX/FX/RockDissolve" },
            // 🔴 2026-10-22（W64 B11）：这条也**不再是近似** —— 原版字节码（Ruri 反编译）逐条重写成
            //    `WarpforgeVFX/FX/EclipseTau`（判据 → 那个 shader 的文件头）。
            { "Shader Graphs/Eclipse Tau",                            "WarpforgeVFX/FX/EclipseTau" },
            // 非粒子的 Everguild shader —— 必须指到 URP/Unlit。
            // 让它们掉进默认的 URP **Particles**/Unlit 会连粒子专用逻辑一起套上（实测过曝 4 倍）
            { "Everguild/UnlitAmbient",                   "WarpforgeVFX/UnlitAmbient" },
            { "Everguild/UnlitAmbient Emissive Flickker", "WarpforgeVFX/UnlitAmbient" },
            { "Everguild/Unlit Wind",                     "WarpforgeVFX/UnlitAmbient" },

            // 内置管线（Built-in RP）的老粒子 shader —— 在 URP 工程里会渲染成粉色。
            // 混合是写死在 shader 里的，材质上根本没有 _SrcBlend，所以还必须靠
            // InferBlend() 补上正确的混合模式。
            { "Mobile/Particles/Additive",            "WarpforgeVFX/Particles/Extra Color" },
            { "Mobile/Particles/Alpha Blended",       "WarpforgeVFX/Particles/Extra Color" },
            // 🔴 **2026-10-02 更正：`Multiply` 不再指到 `Extra Color`** —— 它的算式**根本不同**：
            //    原版是 `o0 = lerp(1, tex × 顶点色, tex.a × 顶点色.a)`（11 条指令，实读），
            //    而 `Extra Color` 是「只乘」。审计实测 `Goff_Rok_Invasion` **×7.96** 就是它。
            //    现在单开一份 `WFParticlesMultiply.shader`（判据在那个文件头）。
            { "Mobile/Particles/Multiply",            "WarpforgeVFX/Particles/Multiply" },
            { "Particles/Standard Unlit",             "WarpforgeVFX/Particles/Extra Color" },
            // 🔴 **2026-10-23 删除 `Particles/Additive` 这一行**（A1 R2 · 铁律 5 记下来免得又被加回来）：
            //    原版 84 个 bundle 的 `Shader/` 转储里**没有**这个名字（现读 0 命中），引用数也是 0
            //    ⇒ 它是一条**死条目**，只会让每轮审计多数一条「疑似替代」。
            //    ⚠️ 别和 `Mobile/Particles/Additive`（上面那行，仍在用）或
            //    `Legacy Shaders/Particles/Additive`（下面那行，单独 shader）搞混。判据 → `普查产出_1022/A1_自造与兜底_特效动画.md` R2。
            // 🆕 2026-10-02（第三批）：`Legacy Shaders/Particles/{Additive,Alpha Blended}` 按**内置管线的编译字节码**
            //    单开 —— 它们比通用那份多一个 **`×2`**（Additive 那支还多乘 `_TintColor`）。判据 → 两个新 shader 的文件头。
            //    ⚠️ **同族的 `Mobile/Particles/*` 实测就是 `tex × 顶点色`**（只有 2 条指令、连 cbuffer 都没有）
            //      ⇒ 那两条**继续用通用那份**，别一起改。
            { "Legacy Shaders/Particles/Additive",    "WarpforgeVFX/Particles/LegacyAdditive" },
            { "Legacy Shaders/Particles/Alpha Blended", "WarpforgeVFX/Particles/LegacyAlphaBlended" },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", "WarpforgeVFX/Particles/Extra Color" },
            { "Legacy Shaders/Particles/Anim Alpha Blended", "WarpforgeVFX/Particles/Extra Color" },
            { "UI/Additive",                          "WarpforgeVFX/Particles/Extra Color" },

            // ---- 🆕 2026-09-16（构建后 player 验证抓出来的）--------------------------------
            // **原版的 TMP 变体名**：`TextMeshPro/Distance Field Offset` 在我们的 TMP Essentials 里
            // **不存在**（本地 13 张 TMP shader 里没有这个名字，只有原版 `CardPrefab.prefab` 引用它）。
            // 不映射的话运行时报「找不到 shader」⇒ 那个材质槽保留占位材质 ⇒ **整块渲成洋红**
            //（实测：白板 `CardPrefab` 那一格是一大块洋红）。
            // 🔴 **2026-10-22（W64 · 兜底审计 B11 第 5 条）：这条近似已销账 —— 改走原件。**
            //    原版那份编译 shader 在**字体包** `fonts_assets_all.bundle` 里（`m_ParsedForm.m_Name`
            //    逐条实读 = `TextMeshPro/Distance Field Offset`，9 KB blob），
            //    已由 `工具/extract_missing_shaders.py --tmp` 重打进 `wf_tmp_shaders.bundle`
            //    （工厂幂等：两次产物 sha256 相同）⇒ 列进 `UseOriginal` 后**正常态与兜底路都解析到原件**。
            //    ⇒ 这里**故意不再留 Replacements 条目**：留着就等于「原件不在场时用标准 Distance Field
            //      顶」，而那正是 B11 要销掉的那条近似（⛔ 不许拿近似当兜底成品）。
            //    ⚠️ 导出期占位材质仍然用 `TextMeshPro/Distance Field`（`EffectExporter.ShaderMap`，标 `*`）
            //       —— 那条只管编辑器里占位，运行时会被 binder 换成原件。
        };

        /// <summary>有些原版 shader 把混合写死在 shader 里，材质上没有 _SrcBlend。
        /// 这时必须按 shader 名把混合补回去，否则加法发光会变成不透明。
        /// 返回 null 表示该 shader 能自己表达混合，不用补。⚠️ **参数必须是【原版名】** —— `WFMatDef.shader`
        /// 可能是我们替身的名字（2026-10-26 实测 417 块），那种先过 `OriginalNameFor`。</summary>
        public static float[] InferBlend(string originalName)
        {
            if (string.IsNullOrEmpty(originalName)) return null;

            // 🆕 2026-10-19（T05 的配套，**必须先查表**）：`OriginalShaderBlendTable.Map` 是
            //   **原版 pass 状态实读**（只收「混合烘死在 pass 里」的那些，`gen_shader_blend.py` 生成）。
            //   为什么现在必须查：T05 的「孤儿值收口」会把**原版 shader 没声明的** `_SrcBlend/_DstBlend/_ZWrite`
            //   从材质里丢掉（那正是内置 Standard 的残留死值）；丢掉之后这两格就落到**我们 shader 的默认值**
            //   （`WFParticlesExtraColor` 默认是 **1/0 = 不透明**）—— 对「原版烘死 5/10」的那一族就是错的。
            //   ⇒ 查得到表值就用表值（这是原版真值），查不到才退回下面按名字猜。
            //   ⚠️ 实测会因此改变的一族（原来按名字猜不出来）：`Everguild/FX/Particle Distortion Affect Transparents`
            //     （252 个材质块）· `Everguild/FX/Particle Shine Custom Vertex Streams`（29）—— 名字里没有任何
            //     混合关键词，旧逻辑返回 null ⇒ 残留死值/我们默认值 1-0 直接顶上。
            int ts, td; float tz;
            if (OriginalShaderBlendTable.TryGet(originalName, out ts, out td, out tz))
                return new[] { (float)ts, (float)td, tz };

            string n = originalName.ToLowerInvariant();
            // Sprite Additive 是 Blend One One（纯加法），不是 SrcAlpha/One
            if (n.Contains("sprites/sprite additive") || n.Contains("sprites/additive"))
                return new[] { 1f, 1f, 0f };                       // One / One
            if (n.Contains("additive") || n.EndsWith("/add"))
                return new[] { 5f, 1f, 0f };                       // SrcAlpha / One，加法
            if (n.Contains("premultiply"))
                return new[] { 1f, 10f, 0f };                      // One / OneMinusSrcAlpha
            if (n.Contains("multiply"))
                return new[] { 2f, 0f, 0f };                       // DstColor / Zero
            if (n.Contains("alpha blended") || n.Contains("anim alpha"))
                return new[] { 5f, 10f, 0f };                      // SrcAlpha / OneMinusSrcAlpha
            return null;
        }

        /// <summary>有些原版 shader 把 **`Cull` 也写死在 pass 状态里**（材质上的 `_Cull` 是**死值**）。
        /// 返回要写进 `_Cull` 的值；**返回 null = 该 shader 的 cull 由材质属性驱动，别覆盖**。
        ///
        /// 🔴 **为什么单开这一条（2026-10-02 晚，铁律 5·b）**：`InferBlend()` 只补
        ///   `_SrcBlend/_DstBlend/_ZWrite/_Surface`，**从来没补过 `_Cull`**。而我们那份
        ///   `WarpforgeVFX/Particles/Extra Color` 的 `_Cull` **默认是 2（Back）** ⇒
        ///   凡材质里没有 `_Cull` 的（= cull 写死在原版 shader 里的那一族）**全被背面剔除**。
        ///   **实测**：`Mobile/Particles/Additive` 那一族里 `renderMode = Mesh` 的粒子
        ///   **整整少一半** —— `Screen_Gold` / `StunEffect_proc` / `StunEffect` 三条的渲染器
        ///   `iso2` 差异区 `orig 92 vs 我们 51`、全图能量比 0.95，形状上「上下两排整片没了」
        ///   （并排图 `_tmp_view/cmp_crop3/Screen_Gold_iso2.png`）。
        ///
        /// **判据 = 从原版包实读 pass 状态**（`Warpforge_unitybuiltinassets.bundle`，2026-10-02 晚）：
        /// <code>
        ///   Mobile/Particles/Additive              cull=0 · zwrite=0 · ztest=4 · colMask=15 · blend 5/1
        ///   Mobile/Particles/Alpha Blended         cull=0 · zwrite=0 · ztest=4 · colMask=15 · blend 5/10
        ///   Legacy Shaders/Particles/Additive      cull=0 · zwrite=0 · ztest=4 · colMask=14 · blend 5/1
        ///   Legacy Shaders/Particles/Alpha Blended cull=0 · zwrite=0 · ztest=4 · colMask=14 · blend 5/10
        ///   Particles/Standard Unlit               cull=0 · zwrite=0 · ztest=0 · colMask=15 · blend 1/0
        /// </code>
        /// ⚠️ 这几条的 `cull` 里 `name` 都是 **`&lt;noninit>`**（= 不是属性引用）⇒ **值是被烘死的**，
        ///   材质上那份 `_Cull` 是死值 ⇒ 覆盖它是对的。**反过来**：`Everguild/*` 那批 dump 出来是
        ///   `name='_Cull'`（**属性驱动**）⇒ 那种一律**不许覆盖**。
        /// 🆕 **2026-10-12：这一格改成【查实读表】** —— `OriginalShaderBlendTable.CullMap`（现读 79 条，
        ///   由 `工具/gen_shader_blend.py` 从原版 pass 状态读、**只收「烘死」那一档**）⇒ 不再按名字猜。
        ///   ⚠️ 包清单原来缺 `Warpforge_unitybuiltinassets.bundle` ⇒ `Mobile/Particles/*` 那一族**表里查不到**
        ///   （与 `工具/disasm_dxbc.py` 文件头那条「第一次只读了一个包 ⇒ 误报没有」**同形**）；补包后 cull 表 79 条、
        ///   生成器末尾自检「5 个必有名必须命中」**5/5 通过**。
        ///   ⚠️ 同批读进表的另外两格（`zTest` / `colMask`）见下面那条。
        /// ✅ **`colMask=14`（= RGB，不写 A）那两条 2026-10-12 已建模** —— 实读表确认
        ///   `Legacy Shaders/Particles/{Additive,Alpha Blended}` 的 colMask=14（其余全是 15），已在 `WFLegacyParticlesAdditive.shader` / `WFLegacyParticlesAlphaBlended.shader` 里各加一行 `ColorMask RGB`。
        /// ⚠️ ✅ **2026-10-19：`zTest` 那一格有消费者了** —— `InferZTest`（下面那条），
        ///   判据与 cull 同族、同样「只收烘死值、表里没有就不覆盖」。原来这句写「代码里没人查」，已不成立。</summary>
        public static float? InferCull(string originalName)
        {
            if (string.IsNullOrEmpty(originalName)) return null;

            // 🆕 **2026-10-12（特效线 §十四 第 4 条「表驱动收口」）：改成【查实读表】优先。**
            //   表 = `OriginalShaderBlendTable.CullMap`，由 `工具/gen_shader_blend.py` 从**原版 pass 状态**读出来
            //   （`cull` 那一格 `name` 不以 `_` 开头 = **烘死**，才收进表）。**表里没有 = 属性驱动 ⇒ 不覆盖。**
            int cull;
            if (OriginalShaderBlendTable.TryGetCull(originalName, out cull))
                return cull;                                        // 实读值（0=Off · 1=Front · 2=Back）

            // 兜底 = 原来那条**按名字猜**的白名单。**它今天正常走不到** ——
            //   `gen_shader_blend.py` 末尾有一条自检：这 5 个名字必须在 cull 表里，缺一个就**非 0 退出**。
            //   留着它是为了「表被重生成坏 / 表文件丢了」时**不静默退回「全都不覆盖」**（那正是 2026-10-02 D 那条缺陷）。
            //   ⚠️ 新覆盖到的两条（原来这条名字规则**漏了**）：`Shader Graphs/Eclipse Tau` · `UI/Additive`
            //     —— 它们的原版 cull 也是烘死的 Off，而我们的默认是 2(Back)。
            //   🔴 **注意生效范围：这两条（以及这一族几乎全部）都在 `UseOriginal` 里** ⇒ **正常态走原件、
            //      `InferCull` 根本轮不到**；它只在【兜底路】生效（原件包不在场 ⇒ `TryResolve` 落到 `Replacements`）。
            //      `Mobile/Particles/Additive` 那一族的 `_Cull` 修（2026-10-02 D）**也是兜底路的修** —— 别当成正常态缺陷。
            //   ⚠️ `Particles/Standard Unlit` 的 cull **不是烘死的**（现核：它的 `pass0` 是 `gpuProgramID = -1` 的
            //      **空 pass**，真正的渲染 pass 是 `pass2`、`cull` 读的是 `_Cull`）⇒ 表里**故意没有它**，
            //      走下面兜底那支（仍返回 0，**行为与改前一致**）。
            string n = originalName.ToLowerInvariant();
            // 🔴 2026-10-26（E8SH · B9 shader 硬锁）：**删掉原来这里的 `|| n == "particles/standard unlit"`**。
            //   判据：这一族的 `cull` 是**属性驱动**的（`_Cull` 在 pass2 里读，见本函数上面 2026-10-12 那段），
            //   而原版 6 个该族材质里 `_Cull`=0 的 5 个、**=2（Back）的 1 个**
            //   （`IceTrail` → `bundle_battleprefabs_vfxandmisc_assets_all/Material_3773910547246591993.json`）
            //   ⇒ 原来那一支会把这个**活值**强改成 Off。删掉后：那 5 个行为不变（材质本来就是 0），
            //   第 6 个恢复原版的 Back。⚠️ 只影响「原版名 = Standard Unlit 且走我们替身」的材质 ——
            //   本轮之前是 **0 个**（28 块都解析到原件），本轮起 `IceTrail` 那 1 块会走到（硬锁修复带来的）。
            //   判据全文 → `资料/完全复刻/报告/B9_E8SH_shader硬锁修复.md` §二·3。
            if (n.Contains("mobile/particles") || n.Contains("legacy shaders/particles")
                || n == "particles/additive")
                return 0f;                                          // 0 = Off
            return null;
        }

        /// <summary>🆕 2026-10-19（`T05`）：**`zTest` 那一格终于有消费者了** —— 判据与 `InferCull` 完全同族。
        ///
        /// **为什么要补**：`OriginalShaderBlendTable` 从 2026-10-12 起就读了 `zTest` / `zTestBaked` 两列，
        ///   但**代码里没人查** ⇒ 表里那 84 条烘死值一直没生效。我们自建的替代 shader 大多声明了
        ///   `_ZTest`（默认 **4 = LEqual**），而原版这一格有 **11 条不是 4**：
        ///     · `UI/Additive` · `UI/Default` · 7 个 `Everguild/*UI*` ⇒ **0 = Disabled**
        ///     · `Hidden/VideoDecode` ⇒ **8 = Always**
        ///   这些名字里有 3 个（`UI/Additive` 等）本来就在 `UseOriginal` 白名单**之外**（走 `Replacements`）
        ///   ⇒ **兜底路上以前一律按 LEqual 渲**，与原版「不做深度测试」不符。
        ///
        /// **判据 = 原版 pass 状态里 `name` 不以 `_` 开头的那一格**（= 烘死）；
        ///   属性驱动（`name='_ZTest'`，例如 `Particles/Standard Unlit` 的 pass2）**故意不在表里**
        ///   ⇒ 这里返回 null、调用方**不许覆盖**（材质上那份才是活值）。**表里没有就不猜**（⛔ 没有名字兜底）。
        ///
        /// ⚠️ 与 cull 一样：这一族几乎全在正常态走原件 ⇒ 本函数**只在兜底路真正改变画面**。
        /// ⚠️ 生效点只在 `WarpforgeEffectBinder.Build`，且要求目标 shader **自己声明了 `_ZTest`**
        ///   （没声明的 `SetFloat` 是 no-op —— 状态写在 pass 里）。</summary>
        public static float? InferZTest(string originalName)
        {
            if (string.IsNullOrEmpty(originalName)) return null;
            int zt;
            if (OriginalShaderBlendTable.TryGetZTest(originalName, out zt)) return zt;
            return null;
        }
        

        /// <summary>已被自建 shader 覆盖的原版 shader 名单</summary>
        public static IEnumerable<string> Covered { get { return Replacements.Keys; } }

        /// <summary>诊断用：打印解析链各环节是否可用</summary>
        public static string Describe()
        {
            int bundleShaders = 0;
            foreach (var _ in WarpforgeShaderLoader.ShaderNames) bundleShaders++;
            return $"PreferBuiltIn={PreferBuiltIn} 自建替换={Replacements.Count} 条 " +
                   $"改走原件={UseOriginal.Count} 条 " +
                   $"原版bundle={(WarpforgeShaderLoader.Ready ? $"{bundleShaders} 个 shader" : "未加载")}";
        }

        public static bool TryResolve(string originalName, out Shader shader, out string source)
        {
            shader = null; source = null;
            if (string.IsNullOrEmpty(originalName)) return false;

            // 🆕 2026-10-31（B104 · 287 近似 shader A/B）：实验开关命中的名字**先问原版 bundle**；
            // 取不到就照旧往下走（与开关关闭时逐位相同）。默认关。
            if (PreferOriginalFor(originalName) && WarpforgeShaderLoader.TryGetShader(originalName, out shader))
            {
                source = "原版bundle（A/B 实验 WF_PREFER_ORIGINAL）";
                return true;
            }

            // 🔴 「改走原件」白名单**排在最前面**：这几个名字跳过自建替代，直接去 bundle 取原版本体。
            //    放在 `PreferBuiltIn` 之前是**故意**的 —— 它比「优先自建」这个总开关优先级更高，
            //    否则开关一开就把白名单也一起关掉了，那种「关了但没完全关」最难查。
            //    🆕 `ForceBuiltIn`（`WFBIND_FORCE_BUILTIN`）是**凌驾于白名单之上**的诊断开关 ——
            //    它让「原件 vs 我们那份」可以在同一个探针里都渲出来（见 `ForceBuiltIn` 的注释）。
            bool wantOriginal = UseOriginal.Contains(originalName) && !ForceBuiltInFor(originalName);

            // 🔴 「改走原件」的名字**先问 bundle**，再回落到工程自带同名 —— 这个顺序是 2026-09-19 换的。
            //    为什么必须换：那天加进白名单的 8 个内置管线名（`Mobile/Particles/Alpha Blended` …）
            //    在编辑器里 `Shader.Find` **找得到**（Unity 自带同一批），而找到的恰恰是
            //    「在 URP 下渲染不了」的那一份 ⇒ 不换序的话白名单**永远走不到 bundle**，
            //    等于没生效、而且是**静默**的（画面还是自建近似，没人看得出来）。
            //    对原有 21 条零影响（**这是 2026-09-19 当时的条数**；现读以 `UseOriginal.Count` 为准）：它们都是 Everguild / ShaderGraph 的私有名，
            //    `Shader.Find` 本来就返回 null（`ShaderResolveProbe` 的白名单断言要求 21/21 走「原版bundle」，
            //    换序前后都过 —— 这条正是用来钉住这次改动的）。
            if (wantOriginal && WarpforgeShaderLoader.TryGetShader(originalName, out shader))
            {
                source = "原版bundle（白名单）";
                return true;
            }

            // 🔴 **2026-10-02 修：这里原来还带一个 `!wantOriginal &&` 守卫，那是个真缺口（铁律 5 就地改）。**
            //    **现象**：白名单名字**取不到原件**时（= 新克隆没有 `StreamingAssets/WarpforgeVFX/` 那 8 个随包）
            //      会因为那个守卫**跳过** `Replacements`，接着往后走：
            //        · Everguild / ShaderGraph 那批私有名 → `Shader.Find` 返回 null → 最终 `return false`
            //          ⇒ 材质槽**保留占位材质**（占位恰好也是我们那份 shader ⇒ 看着「对」是**凑巧**）；
            //        · **8 个内置管线名**（`Mobile/Particles/*` …）→ `Shader.Find` **命中引擎自带那一份**
            //          ⇒ 走的是「**在 URP 下渲染不了**」的那个 shader ✗✗。
            //    **与项目记录不符**：`项目任务.md` / 本文档都写着「**新克隆一个包都没有 ⇒ 兜底路必然被走到**」，
            //      而按上面的路径，白名单里的名字**根本走不到自建替代**
            //      （条数现读 = `UseOriginal.Count`，2026-10-22 为 **64** —— ⛔ 别在此写死，加了新条目就会变）。
            //    **改法**：白名单那次取件已经**失败**才会执行到这里 ⇒ 允许它落 `Replacements`（= 我们那份），
            //      这比「引擎那个渲不出来的」或「干脆没有」都更接近原版。
            //      ⚠️ **正常态不受影响**：包在 → 上面早就 `return true` 了。
            //    **自检**：`ShaderResolveProbe.Run` 现在应报 `64 个走原件 / 0 个没走成`
            //      （2026-10-22 加完三条后的现数；⚠️ **本轮没跑 Unity**，条数以 `UseOriginal.Count` 为准）。
            //      它逐条遍历本表并断言 `source.StartsWith("原版bundle")` ⇒ 白名单**只增不减**时它只会更严。
            if (PreferBuiltIn && Replacements.TryGetValue(originalName, out var mine))
            {
                shader = Shader.Find(mine);
                if (shader != null) { source = "自建"; return true; }
            }
            // 工程自带的同名 shader 排在 bundle 前面。
            //
            // 实测（Psychic_Lightning_down，同一个原版材质，只换 shader）：
            //   bundle 里的 URP Particles/Unlit   → 整片品红（编辑器下渲不出来）
            //   工程自带的 URP Particles/Unlit    → 干净正常
            // 两者是**不同的 Shader 对象**（GetInstanceID 不同）。
            //
            // bundle 里那份只在【运行时才能真正工作】，在编辑器里渲染会出故障 ——
            // 这会让「原版 vs 导出」的比对基准整个失真（原版那一侧全是品红）。
            // 所以只要工程里有同名 shader 就用工程的；bundle 只留给工程里没有的
            // Everguild 自定义 shader 兜底。
            shader = Shader.Find(originalName);
            if (shader != null) { source = "工程自带"; return true; }
            if (WarpforgeShaderLoader.TryGetShader(originalName, out shader))
            {
                source = wantOriginal ? "原版bundle（白名单）" : "原版bundle";
                return true;
            }
            return false;
        }

        // ================================================================================================
        // 🆕 2026-10-26（E8SH · B9「shader 硬锁」修复）：**替身名 → 原版名的反查表**
        //
        // 🔴 **为什么需要它**：`WarpforgeEffectBinder` 把 `WFMatDef.shader` 当【原版 shader 名】用
        //    （解析、三格状态修正、孤儿值闸全都按它查表）。但有 **417 个材质块 / 343 个 prefab**
        //    （2026-10-26 现数）里这个字段被写成了**我们替身的名字**（`WarpforgeVFX/…`）——
        //    逐条判据见 `资料/完全复刻/判据/B9_shader近似映射_对账.tsv` 的「硬锁」小节，
        //    取证与逐族明细见 `资料/完全复刻/报告/B9_E8SH_shader硬锁修复.md` §二。
        //    后果三条（本表修掉 ②③，①仍未修）：
        //      ① 解析永远走不到原件（`UseOriginal` 白名单按原名查）—— ⚠️ **本条【仍未修】**，见下；
        //      ② `InferBlend / InferCull / InferZTest` 按原名查表 ⇒ 替身名查不到 ⇒ **三格都不覆盖**；
        //      ③ `OriginalShaderBlendTable.OriginalProperties(替身名)` 返回 null ⇒ **孤儿值闸不过滤**
        //         ⇒ 材质上那批内置 Standard 的**残留死值**（`_SrcBlend/_DstBlend/_ZWrite/_Surface/
        //         `_Glossiness/_Metallic/_Mode/_Parallax/_UVSec` …）会灌进替身**声明了的**同名属性 ——
        //         与 2026-09-12 那次「加法发光变不透明 + 写深度」**同形状**。
        //    ⇒ 修法：调用方先用 `OriginalNameFor()` 换成原版名，再拿去做那四处查表。
        //
        // ⚠️ **为什么这次不动解析路径（①）**：换解析 = 这 417 块**整体换渲染源**（替身 → 原件），
        //    那是 343 个 prefab 的画面变化，必须 A/B（`WFBIND_FORCE_BUILTIN` 的全量 sweep 要起 Unity，
        //    由主对话做）。根治仍在**数据侧**（让 `WFMatDef.shader` 回到原名，要重跑导出）——
        //    待办与验收口径写在上面的报告 §三。
        //
        // 🔑 **这张表怎么定的（⛔ 别按「看起来像」改）** —— 两路硬证，缺一不填：
        //    ① **prefab 内部的兄弟块**：同名材质在同一次导出里还留着原名的那一份
        //       （例：`Laser_Stream_MultiRay 1` 有 2 块 `Everguild/FX/Multi Ray` + 2 块 `WarpforgeVFX/FX/MultiRay`）；
        //    ② **`assets_full` 的 PPtr**：该材质的 `m_Shader` → `Shader.m_ParsedForm.m_Name`
        //       （J3SH 给的 8 组 PPtr 本代理逐条自校验通过，见报告 §二·1）。
        //    ⚠️ **一个替身顶多个原版族**时（`WarpforgeVFX/Particles/Extra Color` 顶 **3** 个族：
        //      `Mobile/Particles/Additive` 6 个基材质 · `Particles/Standard Unlit` 1 个 · `Mobile/Particles/Alpha Blended` 1 个）
        //      ⇒ 光靠名字反查**不够**，多出来的那两族按【材质基名】走 `SurrogateMaterialOverrides`。
        //    ⚠️ 与 `Replacements` 方向相反：这张表是「**值 → 键**」的反查，**不许**拿它去替代
        //      `Replacements`（那边是「原版名 → 替身」的正查，改动会影响导出期占位材质）。
        // ================================================================================================
        public static readonly Dictionary<string, string> SurrogateToOriginal =
            new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            { "WarpforgeVFX/Particles/Extra Color EC", "Everguild/FX/Extra Color" },                            // 304 块 / 14 个基材质，全部同族
            { "WarpforgeVFX/FX/MultiRay",              "Everguild/FX/Multi Ray" },                              //  37 / 12
            { "WarpforgeVFX/FX/TrailShader1",          "Everguild/FX/TrailShader_1" },                          //   6 /  2
            { "WarpforgeVFX/FX/Distortion",            "Everguild/FX/Particle Distortion Affect Transparents" },//   6 /  2
            { "WarpforgeVFX/FX/UnlitUVScroll",         "Everguild/FX/Unlit UV scroll" },                         //   5 /  3
            { "WarpforgeVFX/FX/TrailShaderFading",     "Everguild/FX/TrailShader_Fading" },                      //   3 /  1
            { "WarpforgeVFX/Matcap/Matcap",            "Everguild/Matcap/Matcap Full Options" },                 //   2 /  1（⚠️ `Matcap With Texture` 也映射到同一个替身，但它 0 块硬锁 ⇒ 名字反查对它不成立，见报告 §三）
            // ⚠️ 下面这条是**默认档**：8 个基材质里 6 个是它；`IceTrail` / `smokesoft_dark` 两个走例外表。
            { "WarpforgeVFX/Particles/Extra Color",    "Mobile/Particles/Additive" },                           //  54 /  8（6 默认 + 2 例外）
        };

        /// <summary>替身名 + **材质基名** → 原版名。**只用于「一个替身顶多个原版族」的那几条**，
        /// 键 = `替身名|材质基名`（基名 = 去掉导出器加的 `__&lt;hex&gt;` 后缀，见 <see cref="MaterialBaseName"/>）。
        ///
        /// ⚠️ 这不是「猜」：两条都同时满足上面那两路硬证（prefab 兄弟块 + `assets_full` PPtr）——
        ///   逐条出处写在 `资料/完全复刻/报告/B9_E8SH_shader硬锁修复.md` §二·2。</summary>
        static readonly Dictionary<string, string> SurrogateMaterialOverrides =
            new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            // 判据：**同一个 prefab**（`PathOfTheWarrior_Projectile.prefab`）里 `IceTrail` 与
            //   `IceTrail__605375ed` 并存 —— 前者写的正是 `Particles/Standard Unlit`；
            //   `assets_full` 里同名的 `IceTrail` 也有一份 `m_Shader` 指向 `Particles/Standard Unlit`
            //   （另一份指向 `Universal Render Pipeline/Particles/Unlit`，那是另一个包里的另一个对象）。
            { "WarpforgeVFX/Particles/Extra Color|IceTrail",       "Particles/Standard Unlit" },
            // 判据：55 块原名块 = `Mobile/Particles/Alpha Blended`，3 块被写成替身名（2026-10-26 现数）；
            //   材质记的那批 `_Glossiness/_Metallic/_Mode/_Parallax/_UVSec/_Cutoff` 是 **Standard 残留死值**
            //   （两个候选原版族的属性表都只有 `_MainTex`）⇒ 归属只能靠 PPtr/兄弟块，不靠属性。
            { "WarpforgeVFX/Particles/Extra Color|smokesoft_dark", "Mobile/Particles/Alpha Blended" },
        };

        /// <summary>把导出器加了 hash 后缀的材质名还原成基名（`LightningTrail__9322aa06` → `LightningTrail`）。
        ///
        /// 后缀形状 = `__` + **≥6 位纯十六进制**（`EffectExporter` 给「同名不同源材质」加的）。
        /// ⚠️ 不满足这个形状就**原样返回** —— ⛔ 别把正常名字里出现的 `__` 截掉
        ///   （例如 `Chain_Rugged_Trail_blend_SCROLLING` 里根本没有 `__`）。</summary>
        public static string MaterialBaseName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return materialName;
            int i = materialName.LastIndexOf("__", System.StringComparison.Ordinal);
            if (i <= 0 || materialName.Length - i - 2 < 6) return materialName;
            for (int k = i + 2; k < materialName.Length; k++)
            {
                char c = materialName[k];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return materialName;            // 不是 hash 后缀
            }
            return materialName.Substring(0, i);
        }

        /// <summary>**这个 `WFMatDef.shader` 值是不是我们替身的名字**（`WarpforgeVFX/` 那一批自建 shader）。
        /// 只用来**出声**（「没查到反查条目」时不许静默），⛔ 不用它做解析判断。</summary>
        public static bool IsSurrogateName(string shaderName)
        {
            return !string.IsNullOrEmpty(shaderName)
                && shaderName.StartsWith("WarpforgeVFX/", System.StringComparison.Ordinal);
        }

        /// <summary>🆕 2026-10-26（E8SH）：把 `WFMatDef.shader` 这个值换成**原版 shader 名**。
        ///
        /// · 不是替身名 ⇒ **原样返回**（99% 的情况走这条，行为与改前逐字相同）；
        /// · 是替身名且表里有 ⇒ 返回原名（多族替身再看 `materialName` 走例外表）；
        /// · 是替身名但表里**没有** ⇒ 原样返回 —— 调用方（`WarpforgeEffectBinder`）会**报一次警告**
        ///   （用 <see cref="IsSurrogateName"/> 判）⇒ 新出现的硬锁**不会静默**。
        ///
        /// ⚠️ **只给「按原版名查表」的四处用**：`OriginalProperties` / `InferBlend` / `InferCull` /
        ///   `InferZTest`（+ `ApplyDerivedParticleDefaults` 的原版默认值查询）。
        ///   ⛔ **别拿它去喂 `TryResolve`** —— 那会把这 417 块的渲染源从替身换成原件，是另一件事（见类内注释 ⚠️）。</summary>
        public static string OriginalNameFor(string shaderName, string materialName = null)
        {
            if (string.IsNullOrEmpty(shaderName)) return shaderName;
            string orig;
            if (!SurrogateToOriginal.TryGetValue(shaderName, out orig)) return shaderName;
            string b = MaterialBaseName(materialName);
            string ov;
            if (!string.IsNullOrEmpty(b)
                && SurrogateMaterialOverrides.TryGetValue(shaderName + "|" + b, out ov)) return ov;
            return orig;
        }
    }
}
