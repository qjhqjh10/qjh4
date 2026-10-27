// BoosterPackAnim.cs — 开包窗动画族（**翻牌 / 抖动 / 快速开包**）的纯函数与采样表（界面线 T19）
//
// ============================ 判据（全部来自原版，逐条标出处） ============================
// 🔴 **第一权威 = 原版 clip 本身**。7 条 `AnimationClip` 由 `BoosterPackExporter.Run()` 落成
//    `Assets/WarpforgeVFX/Animations/*.anim`（**不是**运行时格式的空壳 —— `EffectExporter.ImportClip`
//    是按曲线重建的，`m_Legacy: 1`、曲线条数与关键帧逐条在）。本文件里的表就是**从这几份 .anim 里采出来的**：
//    · `Booster Window Open.anim`（0.6166667 s）—— 5 张卡的收场缩放 157.58151 → **151**（t=0.2166667 → 0.6166667）
//    · `Booster Opening - Card Open Normal.anim`（0.6666667 s）—— 稀有度 **0/1/2** 用（MB `contentByRarities` 实读）
//    · `Booster Opening - Card Open Rare.anim`（1.1666667 s）—— 稀有度 **3/5** 用
//    · `Booster Opening - Card Open Legendary.anim`（1.5 s）—— 稀有度 **4** 用
//    · `Booster Opening - Card Idle.anim`（8 s，`m_WrapMode = 2` = Loop）—— 翻开后回到它
//    · `OpenCardbacks.anim`（0.25 s · `m_WrapMode = 1` = Once）—— **已查清（2026-10-27 `B9_W23DRAFT` 订正：原来这里写「用途未查清，不接」）** = `DeckGeneralInfoDemo.cardbacksAnimation`：卡背**两层入场**（内层 `anchoredPosition (0.3,0.3) → (11.2012,4.9016)`、两层 z 旋转到 **+1.60°** / 相对 **−3.43°**）；消费者 = `Shell/PracticeModePopup.BuildCardbacks`（⛔ 与开包窗无关，本文件不接它是**对的**）
//    · `Booster Window - Background Shake On Open.anim`（0.9166667 s）—— 只动 `floatProperty1/2`
//      （`AnimateUIMaterialProperty` 的材质属性，原版 shader 的逻辑本地没有）⇒ **没做**，见 §缺
//
// 🔴 **两个事件（`m_Events`，从 `bundle_menus_assets_all/AnimationClip/AnimationClip_<pid>.json` 实读）**：
//    · `EnableCardBackAnimEvent` = **翻到 90° 那一刻**（Normal 0.45 · Rare 0.71666664 · Legendary 1.0833334）
//      ⇒ `CardInBoosterPack.EnableCardBackAnimEvent` 调 `BasicCardUI.ToggleCardBack(false)`（关卡背、露卡面）
//      —— 与 clip 里 `Content.localScale.x` 翻成 **-1**（镜像纠正）是**同一个时刻**
//    · `FinishedOpeningAnimEvent` = clip 末帧（0.6666667 / 1.1666667 / 1.5）
//      ⇒ `CardInBoosterPack._WaitForSpinAnimationEnd` 等这么久，然后重开交互、补 New/Upgrade 角标、
//        `ResetContentAnimation`（旋转/缩放/位移归零）+ `CrossFade` 回 `Card Idle`
//
// 🔴 **行为（反编译方法体，`d:/2/tools/decomp_full/`）**：
//    · `BoosterPackOpenWindow__BoosterPackOpen.c`：`Animation.Play(0)` 播开包 clip + 背景 clip
//    · `BoosterPackOpenWindow__AnimationEndEvent.c`：5 张卡 `CardInBoosterPack__ChangeState(2)`
//    · `CardInBoosterPack__UiColliderOnClick.c`：`state != 2` 直接 return；先播 `clickSound`；
//      `+0x98`（= `CalculateReOpenProtectionDelay()` 的返回值）> 0 ⇒ `ChangeState(4)`，否则 `ChangeState(3)`
//    · `CardInBoosterPack__ChangeState.c`：
//        1 = 卡背态（`ToggleCardBack(true)`，交互关）
//        2 = 可交互（交互开；把当前 localScale 记进 +0x80/+0x88）
//        3 = 翻牌：`window.CardOpened(this)` → `DOScale(记下的 scale, 0.2 s)` → 按稀有度取 clip →
//            `Animation.CrossFade(clip, 0.2f)` → 播 `audioCue`（`MixerType.Jingles`）→
//            `Instantiate(particles, card.transform)` → 每条 `shakes` 调 `OverwriteCameraShakePreset.Play()`
//        4 = **防连开抖动**：`DOShakePosition(card, duration = +0x98, strength = localScale × (1 + 4×clamp01(delay)),
//            vibrato 15, randomness 90, ...)`，`OnComplete → ChangeState(3)`
//    · `BoosterPackOpenWindow__CalculateReOpenProtectionDelay.c`：60 s 窗口 · 连开超过 4 次后每次 +0.15 s ·
//      上限 2.0 s · `delay <= 0.15 ⇒ 返回 0`
//
// ============================ ⚠️ 等价物（我们挑的，不是原版值） ============================
//   · **曲线采样**：rotation 用 **30 Hz**、idle 用 **0.25 s** 采样后线性插值（原 clip 是 60 Hz 关键帧）。
//     误差 < 0.5°；**关键帧表本身是原版值**，只有「关键帧之间的插值方式」从 Unity 的 Hermite 退化成线性。
//   · **防连开抖动的幅度量纲查不到**：原式 `strength = localScale × (1 + 4×clamp01(delay))`，
//     原版卡节点 localScale = **151**（动画驱动），而 DOTween 的 `strength` 是世界单位 ⇒ 静态读不出
//     151 到底折成多少 px（108 px/单位 vs 151 px 两种读法差 108 倍，无法二选一）。
//     ⇒ 本实现按 **我们的卡节点 localScale = 1** 代进同一个式子 ⇒ 幅度 **1~5 px**。
//     **不是原版幅度**，只是同一条公式在另一套量纲下的值；如实标。
//   · **相机抖动（`contentByRarities[i].shakes`）**：原版是 `OverwriteCameraShakePreset` ——
//     **preset SO 本地包里没有**（`bundle_menus_assets_all/MonoBehaviour/` 里搜 `"amplitude"` 只命中 5 份
//     `CardInBoosterPack`，没有 preset 本体）⇒ `sustain/attack/decay/amplitude` 的**缺省值拿不到**。
//     本文件只保留了 MB 里**明确覆盖过**的那几个数（延迟/幅度/频率/衰减），包络与时长是**我们挑的**
//     （`CameraShakeDuration = 0.5 s`、8 Hz、线性衰减），在窗口里表现为**卡容器整体抖**（菜单层没有相机）。
//     ⚠️ 传奇第一条（delay 0）`overwriteAmplitude = 0` ⇒ 幅度取 preset 缺省值，**查不到** ⇒ **不抖**（不猜）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>翻牌 clip 的三档（判据 = `contentByRarities[]` 的 `animationClip` 实读）。</summary>
    public enum BoosterFlipKind
    {
        /// <summary>`Booster Opening - Card Open Normal`（0.6666667 s）—— 稀有度 0/1/2。</summary>
        Normal = 0,
        /// <summary>`Booster Opening - Card Open Rare`（1.1666667 s）—— 稀有度 3/5。</summary>
        Rare = 1,
        /// <summary>`Booster Opening - Card Open Legendary`（1.5 s）—— 稀有度 4。</summary>
        Legendary = 2,
    }

    /// <summary>开包窗动画的**纯函数 + 采样表**（不持有状态；状态机在 `BoosterPackOpenWindow` 里）。</summary>
    public static class BoosterPackAnim
    {
        // ============================================================ ① 开包（`Booster Window Open`）

        /// <summary>`Booster Window Open` 的时长（clip 末帧关键帧 t）。</summary>
        public const float WindowOpenDuration = 0.6166667f;
        /// <summary>缩放曲线的第一帧时刻（0.2166667）之前是常数 157.58151。</summary>
        public const float WindowOpenScaleKeyT = 0.2166667f;
        /// <summary>clip 末帧的卡节点 `localScale`（= 我们摆放时用的静止档）。</summary>
        public const float CardScaleRest = 151f;
        /// <summary>clip 第一帧的卡节点 `localScale`（157.58151 —— 原版就是 6 位小数）。</summary>
        public const float CardScalePeak = 157.58151f;

        /// <summary>开包瞬间卡**相对静止档的缩放倍率**（1.043586 → 1）。
        /// 判据：`Booster Window Open.anim` 的 `m_ScaleCurves`（2 个关键帧，t=0.2166667/0.6166667，
        /// 两端 `outSlope = 0` ⇒ 平滑进出，这里用 smoothstep 等价）。</summary>
        public static float WindowOpenScale(float t)
        {
            if (t <= WindowOpenScaleKeyT) return CardScalePeak / CardScaleRest;
            if (t >= WindowOpenDuration) return 1f;
            float u = (t - WindowOpenScaleKeyT) / (WindowOpenDuration - WindowOpenScaleKeyT);
            u = u * u * (3f - 2f * u);                       // smoothstep（两端切线 0）
            return Mathf.Lerp(CardScalePeak / CardScaleRest, 1f, u);
        }

        // ============================================================ ② 翻牌（`Booster Opening - Card Open *`）

        /// 🔶 自己设计（非原版）：翻牌动画采样步长 `FlipSampleStep = 1/30` · 原 clip 是 60 Hz 关键帧，我们按代码补间（原版判据拿不到） · TODO(谁定的)｜原注：<summary>采样步长（**我们的**采样，不是原版 —— 原 clip 是 60 Hz 关键帧；见文件头「等价物」）。</summary>
        public const float FlipSampleStep = 1f / 30f;

        /// <summary>稀有度（`BoosterPackOpenWindow.RarityInt` 的那套 0..4）→ 用哪条 clip。
        /// 🔴 判据 = MB `contentByRarities[]` 的 `animationClip` **逐条实读**：
        /// rarity 0/1/2 → `8301228418192668656` = Normal · 3/5 → `-9033435933554767437` = Rare ·
        /// 4 → `-1691631367813043119` = Legendary。⚠️ **粒子那一套分组不一样**（0/1 · 2 · 3 · 4/5），别混用。</summary>
        public static BoosterFlipKind KindForRarity(int rarity)
        {
            if (rarity == 4) return BoosterFlipKind.Legendary;
            if (rarity == 3 || rarity == 5) return BoosterFlipKind.Rare;
            return BoosterFlipKind.Normal;                   // 0 / 1 / 2
        }

        /// <summary>翻牌 clip 时长（= clip 末帧关键帧 t，即 `FinishedOpeningAnimEvent` 的时刻）。</summary>
        public static float FlipDuration(BoosterFlipKind k)
        {
            switch (k)
            {
                case BoosterFlipKind.Rare: return 1.1666667f;
                case BoosterFlipKind.Legendary: return 1.5f;
                default: return 0.6666667f;
            }
        }

        /// <summary>`EnableCardBackAnimEvent` 的时刻（= 翻到 90°、关卡背/露卡面、`localScale.x → -1` 那一拍）。</summary>
        public static float FlipRevealTime(BoosterFlipKind k)
        {
            switch (k)
            {
                case BoosterFlipKind.Rare: return 0.71666664f;
                case BoosterFlipKind.Legendary: return 1.0833334f;
                default: return 0.45f;
            }
        }

        // ---- 三张 yaw 表（单位：度；`2·atan2(y, w)`，四元数先翻到 w ≥ 0 那一支）----
        // 判据 = 每份 `.anim` 的 `m_RotationCurves`（path = `Content`），30 Hz 采样。
        static readonly float[] YawNormal =
        {
            0f, 1.46f, 5.26f, 10.56f, 16.5f, 22.22f, 26.88f, 29.62f, 27.19f, 10.31f,
            -10.59f, -34.17f, -58.51f, -80.66f, -98.82f, -117.38f, -135.93f, -152.96f, -166.98f, -176.5f,
            180f,
        };
        static readonly float[] YawRare =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1.03f,
            7.78f, 17.49f, 26.21f, 30f, 18.53f, 0f, -14.67f, -30.02f, -45.39f, -60.13f,
            -73.6f, -85.15f, -94.53f, -103.75f, -113.05f, -122.3f, -131.33f, -140.03f, -148.23f, -155.8f,
            -162.6f, -168.48f, -173.31f, -176.93f, -179.21f, 180f,
        };
        static readonly float[] YawLegendary =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.6f, 4.94f, 12.37f,
            21.56f, 31.21f, 40f, 35.9f, 17.63f, 0f, -12.63f, -25.58f, -38.57f, -51.32f,
            -63.54f, -74.97f, -85.32f, -94.57f, -104.03f, -113.76f, -123.55f, -133.18f, -142.46f, -151.18f,
            -159.12f, -166.08f, -171.86f, -176.24f, -179.03f, 180f,
        };

        static float[] YawTable(BoosterFlipKind k)
        {
            switch (k)
            {
                case BoosterFlipKind.Rare: return YawRare;
                case BoosterFlipKind.Legendary: return YawLegendary;
                default: return YawNormal;
            }
        }

        /// <summary>`Content` 绕 Y 的偏航角（度）。原版先**反向蓄力**（Normal +27° / Rare +30° / Legendary +40°）
        /// 再翻到 **180°**（卡面朝前）。</summary>
        public static float FlipYaw(BoosterFlipKind k, float t)
        {
            var a = YawTable(k);
            if (t <= 0f) return a[0];
            float x = t / FlipSampleStep;
            int i = (int)x;
            if (i >= a.Length - 1) return a[a.Length - 1];
            return Mathf.Lerp(a[i], a[i + 1], x - i);
        }

        /// <summary>翻牌过程中的「压扁」系数（`Content.localScale.xy`，x 另乘镜像）。
        /// 判据 = `m_ScaleCurves`：Rare `0.95 @ 0.5 s` · Legendary `0.95 @ 0.8333333 s`；Normal **没有压扁**。
        /// （末帧那个 `x = -1` 是**镜像**，由 <see cref="FlipMirrorX"/> 单独表达。）</summary>
        public static float FlipSquash(BoosterFlipKind k, float t)
        {
            if (k == BoosterFlipKind.Rare)
            {
                if (t <= 0f) return 1f;
                if (t <= 0.5f) return Mathf.Lerp(1f, 0.95f, t / 0.5f);
                if (t <= 0.7f) return Mathf.Lerp(0.95f, 1f, (t - 0.5f) / 0.2f);
                return 1f;
            }
            if (k == BoosterFlipKind.Legendary)
            {
                if (t <= 0f) return 1f;
                if (t <= 0.8333333f) return Mathf.Lerp(1f, 0.95f, t / 0.8333333f);
                if (t <= 1.0666667f) return Mathf.Lerp(0.95f, 1f, (t - 0.8333333f) / 0.2333334f);
                return 1f;
            }
            return 1f;                                       // Normal：m_ScaleCurves 里三帧都是 1
        }

        /// <summary>`Content.localScale.x` 的符号：翻过 `EnableCardBackAnimEvent` 之后变 **-1**（镜像纠正）。
        /// 判据 = `m_ScaleCurves` 的末帧（Normal 0.45 / Rare 0.71666664 / Legendary 1.0833334 都是 `x = -1`）。</summary>
        public static float FlipMirrorX(BoosterFlipKind k, float t)
        {
            return t >= FlipRevealTime(k) ? -1f : 1f;
        }

        // ---- `Content.m_AnchoredPosition` 的**起手抖**（单位 = 卡单位；× `CardK` = px）----
        // 判据 = 每份 `.anim` 的 `m_FloatCurves`（path = `Content`，m_AnchoredPosition.x/y）逐帧实读：
        //   Rare      x: 0 → .025 → -.01 → -.01 → .0125 → 0（0.28333 之后恒 0）
        //             y: 0 → -.025 → .025 → -.025 → .02  → 0
        //   Legendary 同样的形状，节点时刻稍晚（0.13333 / 0.18333 / 0.26667 / 0.31667）
        //   Normal    恒 0（两帧都是 0）
        static readonly float[] JitterT = { 0f, 0.05f, 0.11667f, 0.16667f, 0.23333f, 0.28333f, 0.43333f, 0.5f };
        static readonly float[] JitterX = { 0f, 0.025f, -0.01f, -0.01f, 0.0125f, 0f, 0f, 0f };
        static readonly float[] JitterY = { 0f, -0.025f, 0.025f, -0.025f, 0.02f, 0f, 0f, 0f };
        static readonly float[] JitterTLeg = { 0f, 0.05f, 0.13333f, 0.18333f, 0.26667f, 0.31667f, 0.83333f, 0.98333f };
        static readonly float[] JitterXLeg = { 0f, 0.025f, -0.01f, -0.01f, 0.0125f, 0f, 0f, 0f };
        static readonly float[] JitterYLeg = { 0f, -0.025f, 0.025f, -0.025f, 0.02f, 0f, 0f, 0f };

        /// <summary>翻牌起手的位移抖动（**卡单位**）。Normal 恒 0。</summary>
        public static void FlipJitter(BoosterFlipKind k, float t, out float x, out float y)
        {
            x = 0f; y = 0f;
            if (k == BoosterFlipKind.Normal) return;
            bool leg = k == BoosterFlipKind.Legendary;
            var ts = leg ? JitterTLeg : JitterT;
            var xs = leg ? JitterXLeg : JitterX;
            var ys = leg ? JitterYLeg : JitterY;
            if (t <= ts[0] || t >= ts[ts.Length - 1]) return;
            for (int i = 0; i < ts.Length - 1; i++)
            {
                if (t < ts[i] || t > ts[i + 1]) continue;
                float u = (t - ts[i]) / Mathf.Max(1e-6f, ts[i + 1] - ts[i]);
                x = Mathf.Lerp(xs[i], xs[i + 1], u);
                y = Mathf.Lerp(ys[i], ys[i + 1], u);
                return;
            }
        }

        // ============================================================ ③ 翻开之后的待机（`Card Idle`，8 s 循环）

        static readonly float[] IdleYawTable =
        {
            0f, -0.25f, -0.92f, -1.85f, -2.89f, -3.91f, -4.75f, -5.27f, -5.26f, -4.27f,
            -2.7f, -1.09f, 0f, 0.63f, 1.23f, 1.8f, 2.34f, 2.82f, 3.24f, 3.6f,
            3.88f, 4.08f, 4.19f, 4.19f, 3.96f, 3.53f, 2.96f, 2.31f, 1.64f, 1.01f,
            0.49f, 0.13f, 0f,
        };
        /// <summary>`Card Idle` 的循环长度（clip 8 s）。</summary>
        public const float IdleDuration = 8f;
        /// <summary>Idle 采样步长（0.25 s —— 我们的采样，见文件头「等价物」）。</summary>
        public const float IdleSampleStep = 0.25f;

        /// <summary>Idle 的偏航角（度）。`t` 会自动对 8 s 取模（原版 `m_WrapMode = 2` = Loop）。</summary>
        public static float IdleYaw(float t)
        {
            if (t < 0f) t = -t;
            t = Mathf.Repeat(t, IdleDuration);
            float x = t / IdleSampleStep;
            int i = (int)x;
            if (i >= IdleYawTable.Length - 1) return IdleYawTable[IdleYawTable.Length - 1];
            return Mathf.Lerp(IdleYawTable[i], IdleYawTable[i + 1], x - i);
        }

        // ============================================================ ④ 防连开（`useQuickOpenProtection`）

        /// <summary>`CalculateReOpenProtectionDelay` 的 60 s 窗口（`_DAT_1834b30c0` = 60）。</summary>
        public const float ProtectionWindow = 60f;
        /// <summary>连开超过 4 次后每次加的秒数（`_DAT_1834b3178` = 0.15）。</summary>
        public const float ProtectionStep = 0.15f;
        /// <summary>上限（`_DAT_1834b2bbc` = 2.0）。</summary>
        public const float ProtectionCap = 2f;
        /// <summary>`delay <= 0.15` 时按 0 处理（`CardInBoosterPack.Initialize` 的 `if (fVar4 <= fVar3) fVar4 = 0`）。</summary>
        public const float ProtectionMin = 0.15f;
        /// <summary>前 4 次免费（`if (4 < count)`）。</summary>
        public const int ProtectionFreeOpens = 4;

        /// <summary>`CalculateReOpenProtectionDelay` 逐字复刻（状态由调用方持有）：
        /// `sinceLastOpen > 60` ⇒ 清零；否则 `count++`，`count > 4` 时 `delay = min(delay + 0.15, 2)`；
        /// 返回 `delay <= 0.15 ? 0 : delay`。**注意 lastOpenTime 的更新也在原方法里** —— 调用方负责记。</summary>
        public static float StepProtection(float sinceLastOpen, ref float delay, ref int count)
        {
            if (sinceLastOpen > ProtectionWindow)
            {
                delay = 0f;
                count = 0;
            }
            else
            {
                count++;
                if (count > ProtectionFreeOpens)
                    delay = Mathf.Min(delay + ProtectionStep, ProtectionCap);
            }
            return delay <= ProtectionMin ? 0f : delay;
        }

        /// <summary>`ChangeState(4)` 的 DOShakePosition 参数：`vibrato` 立即数 = **15**。</summary>
        public const float ProtectionVibrato = 15f;
        /// <summary>防连开抖动的幅度（**我们这一套量纲下的 px**；原式 `localScale × (1 + 4×clamp01(delay))`，
        /// 我们的卡节点 `localScale = 1` ⇒ 1~5 px。⚠️ 原版那里 `localScale = 151` —— 量纲见文件头，**不是原版幅度**）。</summary>
        public static float ProtectionShakePx(float delay)
        {
            return 1f + 4f * Mathf.Clamp01(delay);
        }

        // ============================================================ ⑤ 稀有度相机抖（`contentByRarities[].shakes`）

        /// <summary>一条 `OverwriteCameraShakePreset`（MB 里**覆盖过**的字段只有这几个）。</summary>
        public struct CameraShake
        {
            /// <summary>触发延迟（秒）。</summary>
            public float Delay;
            /// <summary>幅度（px）。⚠️ `<= 0` = 原版取 preset 缺省值、**本地拿不到** ⇒ 我们不抖（不猜）。</summary>
            public float Amplitude;
            /// <summary>衰减时长（秒）；0 = 原版没覆盖（本实现按 0.5 s 处理，见文件头）。</summary>
            public float DecayTime;
        }

        // 判据 = MB `contentByRarities[i].shakes` 逐条实读（rarity 2 / 3 / 4 有；0 / 1 / 5 是空数组）：
        //   rarity 2: 1 条 —— delay 0.65 · amplitude 5 · frequency 0.1
        //   rarity 3: 1 条 —— delay 1.0  · amplitude 40
        //   rarity 4: 2 条 —— ① delay 0 · frequency 0.25 · **amplitude 用的是 preset 缺省值（读不到）**
        //                     ② delay 1.15 · amplitude 20 · direction z 10 · decay 0.5
        static readonly CameraShake[] ShakesNone = new CameraShake[0];
        static readonly CameraShake[] ShakesRare =
            { new CameraShake { Delay = 0.6499999761581421f, Amplitude = 5f, DecayTime = 0f } };
        static readonly CameraShake[] ShakesEpic =
            { new CameraShake { Delay = 1f, Amplitude = 40f, DecayTime = 0f } };
        static readonly CameraShake[] ShakesLegendary =
        {
            new CameraShake { Delay = 0f, Amplitude = 0f, DecayTime = 0f },          // 幅度是 preset 缺省值 ⇒ 不抖
            new CameraShake { Delay = 1.149999976158142f, Amplitude = 20f, DecayTime = 0.5f },
        };

        /// <summary>稀有度（0..4，`BoosterPackOpenWindow.RarityInt` 那套）→ 相机抖预设。</summary>
        public static CameraShake[] ShakesFor(int rarity)
        {
            switch (rarity)
            {
                case 2: return ShakesRare;
                case 3: return ShakesEpic;
                case 4: return ShakesLegendary;
                default: return ShakesNone;
            }
        }

        /// 🔶 自己设计（非原版）：相机抖持续时长 `CameraShakeDuration = 0.5f` · preset 的 sustain/attack/decay 本地拿不到 · TODO(谁定的)｜原注：<summary>一条相机抖的持续时长（**我们挑的** —— preset 的 sustain/attack/decay 本地拿不到）。</summary>
        public const float CameraShakeDuration = 0.5f;
        /// <summary>相机抖的振荡频率（**我们挑的** —— MB 里的 `frequency` 是 preset 的内部量，语义查不到）。</summary>
        public const float CameraShakeHz = 8f;
    }
}
