// CardDisplayLevelUp.cs — 原版「卡牌升级庆祝动画」那两件组件的**合并等价物**
// （`CardDisplayLevelUpAnimationController` ＋ `CardDisplayLevelUpAnimationNotifier`；账 `特效动画` 域 177/178/179/180/181/182/184/185）。
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么合并成一件】施工单（`资料/完全复刻/判据/B9_特效C_施工单.tsv` 行 184）写死的口径：
//   「等价物 = 补间 OnComplete 直接转 `CardLevelUpAnim.AnimationFinished()`（我们直接把通知者与控制器合成一件，
//   ⛔ 不必造第二件 MonoBehaviour）」。⇒ **结构性等价，不是自设计**。两个原版入口都保留成方法（见下）。
//
// 【判据（第一权威 = `d:/2/tools/decomp_full/`）】
//   `CardDisplayLevelUpAnimationController__OnEnable.c:1-45`   ⇒ 三件图（+0x30 / +0x38 / +0x50）α 归零（保留 RGB）
//   `CardDisplayLevelUpAnimationController__FadeOutImages.c`    ⇒ 与 OnEnable **同码折叠（ICF）**（首行是 OnEnable、体逐字相同）
//   `CardDisplayLevelUpAnimationController__FadeOutImage.c:1-19`⇒ 读 `image.color` → `.w = 0` → 写回（`local_18._12_4_ = 0`、
//                                                                  `local_18._0_12_ = 原 12 字节` ⇒ **只清 α、RGB 原样**）
//   `CardDisplayLevelUpAnimationController__DoAnim.c:1-39`      ⇒ ① `gameObject.SetActive(true)` ② `myAnimation.Play()`
//                                                                  ③ `impulseSource.GenerateImpulse()` ④ `fadeBackground.gameObject.SetActive(true)`
//                                                                  ⑤ `DOFade(fadeBackground, DAT_1834b3188, DAT_1834b2dc8, 0)`
//                                                                  ⑥ `SoundManager.Play2D(sound(+0x58), MixerType 3)`
//   `CardDisplayLevelUpAnimationController__ChangeFrame.c:1-29`  ⇒ ① `cameraShakePreset(+0x40).Play()` ② `upgradePanel(+0x48).ChangeFrameOnLevelUp()`
//                                                                  ③ `DOFade(fadeBackground, 0, DAT_1834b3188)` ④ `.OnComplete(⇒ fadeBackground.gameObject.SetActive(false))`
//   `CardDisplayLevelUpAnimationController__AnimationFinished.c` ⇒ `upgradePanel(+0x48).AnimationFinished()`（原义 = 重新开输入）
//   `CardDisplayLevelUpAnimationNotifier__ChangeFrame.c:1-33`    ⇒ 与控制器那一份**逐字同形**，只多一跳 `levelUpController(+0x20)`
//   `CardDisplayLevelUpAnimationNotifier__AnimationFinished.c`   ⇒ `levelUpController(+0x20).upgradePanel(+0x48).AnimationFinished()`（两跳）
//   `CardDisplayLevelUpAnimationController__<ChangeFrame>b__12_0` = ④ 那个闭包（`b__12_0.c` 空 ⇒ 见 ChangeFrame 里对它的处理）
//
// 【两个 DOTween 时长常量（现读，`工具/read_literal.py` 表 + 施工单 179/178 双源一致）】
//   `DAT_1834b3188` = **0.7**（DoAnim 的**目标 α**；ChangeFrame 的**时长**）
//   `DAT_1834b2dc8` = **0.3**（DoAnim 的**时长**）
//   ⚠️ 同一个地址在两个方法里当「目标值」和「时长」用 —— 那是原版的写法，**照抄，⛔ 别「理顺」**。
//
// 【序列化字段真值（原版 `Upgrade Panel`@`scenes_scenes_mainmenuwarpforge`；解包实读见
//   `资料/完全复刻/报告/B8_AB_特效动画C档.md` §五·10）】
//   `myAnimation` · `impulseSource` · `levelUpEffect1`=Level Up Effect · `levelUpEffect2`=Level Up Effect Down ·
//   `upgradePanel` · `fadeBackground` · `sound`(AudioCue) · `cameraShakePreset` =
//   `{ presetSO, delay 0, overwriteAmplitude 1 → **20**, overwriteFrequency 1 → **0.15**, overwriteDecayTime 1 → **1** }`
//   实例数：控制器 **1**（Upgrade Panel）· 通知者 **1**（CardUI）—— 账上旧写的「8 处 / 3 处」**是错的**
//   （现数判据 `报告/B8_AB_特效动画C档.md` §七·1）。
//
// 【这条动画的**驱动源**：一支 legacy Animation clip，靠动画事件回调本组件】
//   原版 clip = `Level Up Card`（`bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`，本代理现读）：
//     · `m_Name = Level Up Card` · `m_Legacy = 1` · `m_SampleRate = 60` · `m_WrapMode = 0`
//     · **`m_Events` 2 条**：`{t=1.4500000476837158, functionName="ChangeFrame"}` · `{t=3.4833333492279053, functionName="AnimationFinished"}`
//       ⇒ **这就是「谁调 ChangeFrame / AnimationFinished」的答案**：动画事件。我们这边由调用方在对应时刻调
//       `NotifierChangeFrame()` / `NotifierAnimationFinished()`（或自己 `Tick` 时钟到点转），⛔ 不要另造 MonoBehaviour。
//     · 曲线（10 条 float + 2 条 scale，逐条现读）：`2DCard/Level Up Effect` 与 `…/Level Up Effect Down` 各有
//       `floatProperty1.value`（`AnimateUIMaterialProperty`）、`m_Color.a`、`m_IsActive`；另有 `2DCard/Front` 的
//       `inheritPosition/inheritRotation` · `2DCard/Glow Explosion` / `Lighting Rays` 的 `m_IsActive`。
//   ⇒ **`myAnimation.Play()` 那一跳不需要我们重做**：把 3 条 clip 导进工程后挂 `Animation` 即可（导 clip 清单见
//     `报告/B8_AB_特效动画C档.md` §五·8；本波禁 Unity ⇒ 本代理**没有**导入）。
//
// 【我们这边的等价物（每一处都标了为什么）】
//   ① 「三张 Image 的 α」—— 原版是 uGUI `Image.color`；我们的 UI 是 `ImageQuad`（`Battle/ImageQuad.cs`，`_mr.sharedMaterial.color`）
//      ⇒ `FadeImage` 这个 struct 包住「`ImageQuad` 优先 / 任意 `Renderer` 兜底」，对外只暴露 `Color`（= 原版 `image.color` 那一对读写）。
//   ② 「相机抖」—— 菜单层**没有相机**（施工单 178 的风险栏写死）⇒ 等价物 = **窗容器抖**，先例 = `Shell/BoosterPackOpenWindow.cs:984 StepBursts`
//      （同一段「amplitude × 衰减包络 × sin(2πft)」），⛔ 别新造一套。
//   ③ 「换框」—— `upgradePanel.ChangeFrameOnLevelUp()` 我们**不实现**，转 `onChangeFrame` 回调
//      （落点 = `Shell/CardDetailPopup.RebuildKeepingState()`；⛔ 别在这里另写一份 tier 计算——判据见该方法自己的注释）。
//   ④ 「输入锁」—— 原版锁点在 `UpgradePanel.TryUpgrade`（本代理**没读到**，判据缺）⇒ 我们把「上锁」放在 `DoAnim` 入口、
//      「解锁」放在 `AnimationFinished`（施工单 177 的验收栏就是这么断言的）。
//   ⑤ 「2D cue」—— 原版 `SoundManager.Play2D(cue, MixerType 3)` ⇒ `WarpforgeVFX.WFSoundPlayer.Play(clip, is2d: true)`（先例 `CardDetailPopup.PlayVoice`）。
//
// 🔶 自己设计（非原版）：`ShakeParams.amplitude` 的**量纲 = px**（原版那格喂给 Cinemachine 冲量、是世界量）
//   · 为什么必须自己定：菜单层没有相机 ⇒ 只能换算到「窗容器位移」，而 px→世界的桥在本机没有第二个判据
//   · 谁定的 + 日期：调度台 2026-10-27（施工单 178 风险栏已裁定走「窗容器抖」等价物）
//   ⇒ 只影响抖动的**绝对量级**，不影响包络/频率/时长（那三个数照抄 20 / 0.15 / 1）。
//
// 【没做的事（如实记）】
//   · 挂点：`Shell/CardDetailPopup.DoUpgrade()` 末尾那一跳**不在本代理白名单**（Shell 归 F7）⇒ 本件只造组件，
//     接线由调度台另派。未接线时它**不会被实例化**，因此不会静默跑错。
//   · ~~3 条 clip 没有导入~~ ⇒ **订正 2026-10-27（B9_W37CLIP）**：三条已在 `Resources/Animations/`，`Level Up Card` 由 `CardDetailPopup` 接线播放。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CardDisplayLevelUpAnimationController` ＋ `…Notifier` 的合并等价物。
    /// <para>**两个原版入口都在**：控制器的 `ChangeFrame` / `AnimationFinished`（`OnEnable` / `DoAnim` / `FadeOutImage(s)` 也在），
    /// 通知者的两个同名转发 = <see cref="NotifierChangeFrame"/> / <see cref="NotifierAnimationFinished"/>。</para>
    /// <para>⚠️ 编辑模式下 `AddComponent` **不触发** `Awake/OnEnable/Start/Update`（本工程踩过）⇒ 自检要读「初始化后」的状态时，
    /// 要么 `SendMessage("OnEnable")`，要么显式调；推时钟用 <see cref="Tick"/>（批处理没有帧循环）。</para></summary>
    public class CardDisplayLevelUp : MonoBehaviour
    {
        // ================================================================== 判据常量

        /// <summary>DoAnim 的背景淡入**目标 α** = `DAT_1834b3188` = 0.7（现读；同一个地址在 `ChangeFrame` 里当时长用）。</summary>
        public const float DoAnimFadeTo = 0.7f;
        /// <summary>DoAnim 的背景淡入**时长（秒）** = `DAT_1834b2dc8` = 0.3。</summary>
        public const float DoAnimFadeTime = 0.3f;
        /// <summary>ChangeFrame 的背景淡出**时长（秒）** = `DAT_1834b3188` = 0.7（同一个 0.7，见上）。</summary>
        public const float ChangeFrameFadeTime = 0.7f;

        /// <summary>原版 `Level Up Card` clip 里两条动画事件的时刻（秒）—— 现读
        /// `bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json` 的 `m_Events`
        /// （`:1.4500000476837158` = `ChangeFrame` · `:3.4833333492279053` = `AnimationFinished`）。
        /// 只在「调用方没自己接动画事件」时当参（见 <see cref="Tick"/> 那条可选时钟）。</summary>
        public const float ClipChangeFrameTime = 1.4500000476837158f;
        public const float ClipAnimationFinishedTime = 3.4833333492279053f;

        // ================================================================== 序列化形状（照原版字段）

        /// <summary>一个「可淡的图」—— 原版这里是 uGUI `Image`（`FadeOutImage(Image)` 只读写 `.color`）。
        /// `ImageQuad` 优先（它会连软边子块一起刷，见 `ImageQuad.SetTint`）；否则退回任意 `Renderer` 的材质 `color`。</summary>
        [Serializable]
        public struct FadeImage
        {
            public ImageQuad quad;
            public Renderer renderer;
            public bool Valid { get { return quad != null || renderer != null; } }
            public Color Color
            {
                get
                {
                    if (quad != null) return quad.Tint;
                    if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                        return renderer.sharedMaterial.color;
                    return Color.white;
                }
                set
                {
                    if (quad != null) { quad.SetTint(value); return; }
                    if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                        renderer.sharedMaterial.color = value;
                }
            }
            public GameObject gameObject
            {
                get
                {
                    if (quad != null) return quad.gameObject;
                    if (renderer != null) return renderer.gameObject;
                    return null;
                }
            }
            public void SetActive(bool on)
            {
                var go = gameObject;
                if (go != null && go.activeSelf != on) go.SetActive(on);
            }
        }

        /// <summary>原版 `cameraShakePreset`（`OverwriteCameraShakePreset`）里**被覆盖过的**四个值。
        /// 真值 = `{delay 0, overwriteAmplitude 1 → 20, overwriteFrequency 1 → 0.15, overwriteDecayTime 1 → 1}`
        /// （判据 → `报告/B8_AB_特效动画C档.md` §五·10）。
        /// ⚠️ `frequency` 的语义 = **每秒完整震荡次数**（`sin(2π·f·t)`）；`decayTime` = 衰减到 0 的秒数。
        /// 🔶 `amplitude` 的量纲是 **px**（为什么 → 文件头「自己设计」那条）。</summary>
        [Serializable]
        public struct ShakeParams
        {
            public bool enabled;
            public float amplitude;
            public float frequency;
            public float decayTime;
            public float delay;
            public static ShakeParams Original
            {
                get
                {
                    return new ShakeParams
                    {
                        enabled = true,
                        amplitude = 20f,       // overwriteAmplitude 1 → 20
                        frequency = 0.15f,     // overwriteFrequency 1 → 0.15
                        decayTime = 1f,        // overwriteDecayTime 1 → 1
                        delay = 0f,            // delay 0（原版无条件覆盖的那格）
                    };
                }
            }
        }

        [Header("原版 CardDisplayLevelUpAnimationController 的序列化字段（+offset 见注释）")]
        /// <summary>原版 `myAnimation`（`+0x20`）—— `DoAnim` 第 ② 跳就是 `myAnimation.Play()`（无参 = 播默认 clip）。</summary>
        public Animation myAnimation;
        /// <summary>原版 `impulseSource`（`+0x28`）＋`cameraShakePreset`（`+0x40`）的等价物落点：**抖这一颗 transform**。
        /// 菜单层没有相机 ⇒ 传**窗容器**（先例 `Shell/BoosterPackOpenWindow` 抖 `Booster Animation Parent`）。</summary>
        public Transform shakeTarget;
        /// <summary>原版 `levelUpEffect1`（`+0x30`）= 场景里的 `Level Up Effect`。</summary>
        public FadeImage levelUpEffect1;
        /// <summary>原版 `levelUpEffect2`（`+0x38`）= `Level Up Effect Down`。</summary>
        public FadeImage levelUpEffect2;
        /// <summary>原版 `upgradePanel`（`+0x48`）—— 只当**标识/日志**用；换框与解输入走下面的两个回调
        /// （⛔ 别在这里碰 `UpgradePanel` 的内部状态）。</summary>
        public Transform upgradePanel;
        /// <summary>原版 `fadeBackground`（`+0x50`）—— 升级时压在卡下面的那层暗背景。</summary>
        public FadeImage fadeBackground;
        /// <summary>原版 `sound`（`+0x58`，AudioCue）—— 我们取已解出的 `AudioClip`（`WFSoundBank` / `WFSoundPlayer` 那套）。</summary>
        public AudioClip sound;
        /// <summary>原版 `cameraShakePreset` 的四个覆盖值（见 <see cref="ShakeParams"/>）。</summary>
        public ShakeParams cameraShake = ShakeParams.Original;

        [Header("接线（原版这两跳在 UpgradePanel 里，我们转回调）")]
        /// <summary>原版 `upgradePanel.ChangeFrameOnLevelUp()` —— 落点 = `Shell/CardDetailPopup.RebuildKeepingState()`
        /// （⛔ 别另写一份 tier 计算）。null ⇒ **出声一次**，不静默。</summary>
        public Action onChangeFrame;
        /// <summary>原版 `upgradePanel.AnimationFinished()` 的其它副作用（除了「解输入锁」之外我们没读到更多 ⇒ 留口）。
        /// null 是**正常**的（输入锁由本组件自己管）。</summary>
        public Action onAnimationFinished;

        // ================================================================== 读数（自检用；不参与行为）

        /// <summary>`FadeOutImage` 被调用的次数（`FadeOutImages` 一次 +3 —— 三个图各一次）。</summary>
        public int FadeOutCount { get; private set; }
        public int DoAnimCount { get; private set; }
        public int ChangeFrameCount { get; private set; }
        /// <summary>`AnimationFinished` 被调用的次数（控制器那一路）。</summary>
        public int FinishedCount { get; private set; }
        /// <summary>通知者那一路的 `AnimationFinished`（与上面分开计 —— 防「只有一条转发路」被删）。</summary>
        public int NotifierFinishedCount { get; private set; }
        public int ShakeCount { get; private set; }
        /// <summary>2D cue 真正播放的次数（`sound == null` 时**不计**）。</summary>
        public int SoundCount { get; private set; }

        /// <summary>「升级动画期间输入被锁」——`DoAnim` 上锁、`AnimationFinished` 解锁（见文件头 ④）。</summary>
        public bool InputBlocked { get; private set; }
        public float BackgroundAlpha { get { return fadeBackground.Color.a; } }
        /// <summary>剩余抖动量（秒）；`0` = 不抖。</summary>
        public float ShakeLeft { get { return _shakeLeft; } }

        // ================================================================== 内部状态

        // 背景淡变（DOFade 的等价物）：同一时刻只有一条在跑（原版两跳在时间上不重叠：
        // DoAnim 的 0.3 s 早于 clip 在 1.45 s 发的 ChangeFrame）。
        float _fadeFrom, _fadeTo, _fadeDur, _fadeLeft;
        bool _fadeBusy, _fadeDeactivateAtEnd;

        // 抖动：`(基准 localPosition) + (包络 × sin)`，与 `BoosterPackOpenWindow.StepBursts` 同形
        Transform _shakeBaseOf;
        Vector3 _shakeBase;
        float _shakeLeft, _shakeT, _shakeAmp, _shakeFreq, _shakeDecay;

        bool _warnedNoAnimation, _warnedNoFadeTarget, _warnedNoChangeFrameTarget, _warnedShakeDelay;

        // ================================================================== 原版七跳

        /// <summary>原版 `CardDisplayLevelUpAnimationController.OnEnable`（`.c:1-45`）/ `FadeOutImages`（同码折叠）：
        /// **每帧首次启用**就把三张图（`levelUpEffect1` / `levelUpEffect2` / `fadeBackground`）的 α 归零 —— 保留 RGB、无闸无条件。</summary>
        public void OnEnable()
        {
            FadeOutImages();
        }

        /// <summary>原版 `FadeOutImage(Image image)`（`.c:1-19`）：读 `.color` → **只把 α 置 0** → 写回。
        /// ⚠️ ILSpy 把它输出成 `image.color = (Color)(&obj)`（看着像写了个 default）—— **那是「实参常量读成 default」的假象**，
        /// 真值在 `.c`：`local_18._0_12_ = 原 12 字节` + `local_18._12_4_ = 0` ⇒ **保留 RGB、只清 α**（施工单 180 已订正过同一处）。</summary>
        public void FadeOutImage(FadeImage image)
        {
            if (!image.Valid)
            {
                if (!_warnedNoFadeTarget)
                {
                    _warnedNoFadeTarget = true;
                    Debug.LogWarning("[CardDisplayLevelUp] 有一格「可淡图」没接线（原版这里有 `Image`，空引用会抛）"
                                   + " ⇒ 这一格**不淡**（⛔ 不是静默：这条只出声一次）");
                }
                return;
            }
            var c = image.Color;
            c.a = 0f;                 // 原版 `local_18._12_4_ = 0`
            image.Color = c;          // RGB 原样写回
            FadeOutCount++;
        }

        /// <summary>原版 `FadeOutImages()`（`.c` 与 `OnEnable` 同码折叠）：三张图各淡一次，**无补间、无时长**。</summary>
        public void FadeOutImages()
        {
            FadeOutImage(levelUpEffect1);
            FadeOutImage(levelUpEffect2);
            FadeOutImage(fadeBackground);
        }

        /// <summary>原版 `DoAnim()`（`.c:1-39`）六跳：
        /// ① 开物体 ② `myAnimation.Play()` ③ 抖（`impulseSource.GenerateImpulse()`）④ 背景开 ⑤ 背景淡到 0.7 / 0.3 s ⑥ 播 2D cue。</summary>
        public void DoAnim()
        {
            DoAnimCount++;
            // ①
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            // ②  —— 原版无参 `Animation.Play()` = 播默认 clip（判据：`UnityEngine_Animation__Play(field, 0)`）
            if (myAnimation != null) myAnimation.Play();
            else if (!_warnedNoAnimation)
            {
                _warnedNoAnimation = true;
                Debug.LogWarning("[CardDisplayLevelUp] `myAnimation` 没接线 ⇒ 原版 `DoAnim` 第 ② 跳（`Animation.Play()`）"
                               + "**不播**。clip = `Level Up Card`（`AnimationClip_10.json`，events @1.45 / 3.4833 s），"
                               + "导入清单 → `报告/B8_AB_特效动画C档.md` §五·8");
            }
            // ③
            PlayShake(); BeginPulse();     // `impulseSource.GenerateImpulse()` ＋ 原版 clip 的「根脉冲」（见类尾 `BeginPulse`）
            // ④ + ⑤
            fadeBackground.SetActive(true);
            StartFade(fadeBackground.Color.a, DoAnimFadeTo, DoAnimFadeTime, deactivateAtEnd: false);
            // ⑥
            if (sound != null)
            {
                WarpforgeVFX.WFSoundPlayer.Play(sound, true, 1f, 1f);
                SoundCount++;
            }
            InputBlocked = true;      // 见文件头 ④（原版锁点在 UpgradePanel.TryUpgrade，本代理没读到）
        }

        /// <summary>原版 `ChangeFrame()`（`.c:1-29`）四跳：
        /// ① 抖（`cameraShakePreset.Play()`）② `upgradePanel.ChangeFrameOnLevelUp()` ③ 背景淡到 0 / 0.7 s
        /// ④ `.OnComplete(⇒ fadeBackground.gameObject.SetActive(false))`。
        /// <para>⚠️ 原版 ③④ 是一对 DOTween 链；我们这条链的「完成」= <see cref="Tick"/> 把 `_fadeLeft` 减到 0 那一刻
        /// （就是 `b__12_0` 那个闭包）。</para></summary>
        public void ChangeFrame()
        {
            ChangeFrameCount++;
            PlayShake();                                   // ①
            if (onChangeFrame != null) onChangeFrame();     // ②
            else if (!_warnedNoChangeFrameTarget)
            {
                _warnedNoChangeFrameTarget = true;
                Debug.LogWarning("[CardDisplayLevelUp] `onChangeFrame` 没接线 ⇒ 原版 `ChangeFrame` 第 ② 跳"
                               + "（`upgradePanel.ChangeFrameOnLevelUp()` = 换框）**不发生**（落点应是 "
                               + "`Shell/CardDetailPopup.RebuildKeepingState()`）");
            }
            StartFade(fadeBackground.Color.a, 0f, ChangeFrameFadeTime, deactivateAtEnd: true);   // ③ + ④
        }

        /// <summary>原版 `AnimationFinished()`（`.c:1-12`）：`upgradePanel.AnimationFinished()` —— **原义 = 重新开输入**。</summary>
        public void AnimationFinished()
        {
            FinishedCount++;
            InputBlocked = false; ResetPulse();   // 收尾把根脉冲比值归 1（判据 `判据/升级动画scale曲线_节点映射.md` §五·A「收尾」）
            if (onAnimationFinished != null) onAnimationFinished();
        }

        // ---- 通知者那两跳（原版 `CardDisplayLevelUpAnimationNotifier`；同一份实现，只是入口不同）

        /// <summary>原版 `Notifier.ChangeFrame()`（`.c:1-33`）：**与控制器逐字同形**（只多一跳 `levelUpController(+0x20)`）。</summary>
        public void NotifierChangeFrame() { ChangeFrame(); }

        /// <summary>原版 `Notifier.AnimationFinished()`（`.c:1-15`）：`levelUpController(+0x20).upgradePanel(+0x48).AnimationFinished()`。</summary>
        public void NotifierAnimationFinished()
        {
            NotifierFinishedCount++;
            AnimationFinished();
        }

        // ================================================================== 时钟（唯一的推进口）

        /// <summary>推一帧（`Update` 调它）。**批处理/自检唯一的路** —— 与 `BoosterPackOpenWindow.Advance` 同一条口径：
        /// ⛔ 别在别处再推一次（两处推 = 走两倍速）。</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            StepFade(dt);
            StepShake(dt); StepPulse(dt);   // 根脉冲（原版 clip 第 ① 条 scale 曲线，判据见类尾）
        }

        void Update() { Tick(Time.deltaTime); }

        void StepFade(float dt)
        {
            if (!_fadeBusy) return;
            _fadeLeft -= dt;
            float elapsed = _fadeDur - Mathf.Max(0f, _fadeLeft);
            // 原版 `Update` 是 `MoveTowards(a, targetFade, dt/blendTime)`（**恒定速率** = 1.0 α/秒·blendTime⁻¹）
            // ⇒ 用 MoveTowards(from, to, elapsed/dur) 逐字等价；|to−from| == 1 时与 Lerp 同值（今天的调用都是 0↔1）。
            SetAlpha(Mathf.MoveTowards(_fadeFrom, _fadeTo, _fadeDur <= 0f ? 1f : Mathf.Clamp01(elapsed / _fadeDur)));
            if (_fadeLeft > 0f) return;
            _fadeBusy = false;
            SetAlpha(_fadeTo);
            if (_fadeDeactivateAtEnd) fadeBackground.SetActive(false);   // 原版 `b__12_0` 闭包
        }

        void StepShake(float dt)
        {
            if (_shakeLeft <= 0f) return;
            _shakeLeft -= dt;
            _shakeT += dt;
            if (_shakeLeft <= 0f || shakeTarget == null)
            {
                _shakeLeft = 0f;
                if (shakeTarget != null && _shakeBaseOf == shakeTarget) shakeTarget.localPosition = _shakeBase;
                return;
            }
            float u = _shakeDecay > 0f ? Mathf.Clamp01(_shakeLeft / _shakeDecay) : 1f;   // 线性衰减包络
            float w = Mathf.Sin(_shakeT * Mathf.PI * 2f * _shakeFreq);
            // 🔶 量纲 = px（`LayoutSpace.Px` 那套 108 px/世界单位）—— 见文件头「自己设计」
            shakeTarget.localPosition = _shakeBase + new Vector3(LayoutSpace.Px(_shakeAmp * u * w), 0f, 0f);
        }

        // ================================================================== 小工具

        void StartFade(float from, float to, float duration, bool deactivateAtEnd)
        {
            _fadeFrom = from;
            _fadeTo = to;
            _fadeDur = duration;
            _fadeLeft = duration;
            _fadeDeactivateAtEnd = deactivateAtEnd;
            _fadeBusy = true;
            if (duration <= 0f) { _fadeBusy = false; SetAlpha(to); if (deactivateAtEnd) fadeBackground.SetActive(false); }
        }

        void SetAlpha(float a)
        {
            if (!fadeBackground.Valid) return;
            var c = fadeBackground.Color;
            c.a = a;                 // RGB 原样（原版 `set_color` 也是保留 RGB）
            fadeBackground.Color = c;
        }

        void PlayShake()
        {
            ShakeCount++;
            if (!cameraShake.enabled || cameraShake.amplitude <= 0f || shakeTarget == null) return;
            // 原版那格 `delay` 现读 = 0（`{presetSO, delay 0, …}`）⇒ 立即开抖。非 0 时**出声**（本件没实现延迟排队，
            // ⛔ 不静默按 0 处理）。
            if (cameraShake.delay > 0f && !_warnedShakeDelay)
            {
                _warnedShakeDelay = true;
                Debug.LogWarning("[CardDisplayLevelUp] `cameraShake.delay = " + cameraShake.delay.ToString("F3")
                               + " s` —— 本件**不支持延迟排队**（原版序列化值是 0）⇒ 这次仍立即开抖");
            }
            if (_shakeBaseOf != shakeTarget) { _shakeBaseOf = shakeTarget; _shakeBase = shakeTarget.localPosition; }
            _shakeLeft = cameraShake.decayTime > 0f ? cameraShake.decayTime : 0.001f;
            _shakeT = 0f;
            _shakeAmp = cameraShake.amplitude;
            _shakeFreq = cameraShake.frequency > 0f ? cameraShake.frequency : 0.05f;
            _shakeDecay = cameraShake.decayTime;
        }

        /// <summary>「这一次 `DoAnim` 真的起播了一支 clip」—— `CardDetailPopup.UpgradeAnimLocked` 的「真在播」那一半。
        /// <para>判据 = 组件上有 `Animation` **且它挂着真 clip**；真 Play 时还要求 `isPlaying`。
        /// ⚠️ 编辑器/批处理**没有帧循环** ⇒ `Animation.isPlaying` 恒 false（本工程反复踩到「编辑模式读到的运行时值不可信」），
        /// 所以非 Play 档只按「挂了真 clip」算 —— 那一档的断言在 `CardDetailPopup.SelfCheckUpgradeAnimationWiring` 里。</para>
        /// <para>🔶 自己设计（非原版）：**「真在播」的定义** · 为什么必须自己定：原版 `UpgradePanel.TryUpgrade` 没有方法体产物，
        /// 读不到它按什么判「动画期间」；clip 侧只有 `length` / `events`，没有「正在播」这个字段 ·
        /// 谁定的 + 日期：调度台 2026-10-27（取代原来的 `myAnimation != null`，见 `报告/B9_W37CLIP_升级clip时长与路径.md`）。</para></summary>
        public bool AnimationWillRun
        {
            get
            {
                var a = myAnimation;
                if (a == null || a.clip == null) return false;
                return a.isPlaying || !Application.isPlaying;
            }
        }

        // ================================================================== 根脉冲（原版 clip 第 ① 条 `m_ScaleCurves`）
        //
        //  🥇 判据 = `Level Up Card` clip 的 `m_ScaleCurves[0]`（`path: ''` = **动画根自己**）：
        //    `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`
        //    （= `assetripper_out/ExportedProject/Assets/AnimationClip/Level Up Card.anim:18-95`）：
        //
        //      t=0            (250,     250,     250     )  si/so = 0
        //      t=1.4333333    (250,     250,     250     )  si/so = 0
        //      t=1.45         (288.97699, 288.98001, 254.30411)  si/so = 0
        //      t=1.5833334    (250,     250,     250     )  si/so = **72.92988 / 68.22313 / 68.66892**
        //      t=1.8333334    (250,     250,     250     )  si/so = 0
        //
        //  · `path: ''` 的根是谁 = 场景里的 **`CardUI`**（卡视图根，`m_LocalScale = 250`），
        //    ⛔ **不是** `Upgrade Panel`（那个只是控制器组件的宿主）。证据链 → `判据/升级动画scale曲线_节点映射.md` §二·1。
        //  · 250 是「1 卡单位 = 250 画布 px」的量纲 ⇒ 有意义的只有**相对值**：288.977/250 = **1.155907959** …
        //  · ⛔ **不能写进 clip**：我们的 `Animation` 挂在**窗根**（`Shell/CardDetailPopup.cs:3298`）⇒ `path: ''`
        //    会把**整扇窗**放大 250 倍；写成 `Card Display/CardUI` 又会在 **1.45 s（正好是峰值）**
        //    被 `ChangeFrame → RebuildKeepingState()` 撞上。⇒ 走**代码补间**（判据 §五·A 那三条理由）。
        //  · 写入口 = `CardView.SetExtraScale`（⛔ **不直接写 `transform.localScale`** —— 会被下一次
        //    `SetPose`/`SetHighlightScale` 的 `ApplyScale()` 静默冲掉）。
        //  · 我们的基准缩放 = `CardView.BaseScale`（槽 0 是 250/108 = 2.3148148，**别写死**）⇒
        //    这里只算**比值**，乘上去由 `CardView` 做。
        //  · 末键之后曲线恒 1 ⇒ 用 `PulseEndTime` 自己收尾；`AnimationFinished` 再兜一次（⛔ 别只归 x/y 忘了 z）。
        //  ⚠️ 目标节点被 `RebuildKeepingState()` 重建后要**重新取**（接线方负责；本件检测到 null 就收尾，
        //    不会把「卡永久大 15.6%」那种最坏情况留在场上）。

        /// <summary>原版那条 scale 曲线的目标（**卡视图根** = 槽 0 的 `CardView`；原版是场景里的 `CardUI`）。
        /// <para>⚠️ 没接线时**出声一次**、脉冲不作用（⛔ 不静默）；重建卡之后由接线方重新赋值。</para></summary>
        public CardView pulseTarget;

        /// <summary>曲线关键帧时刻（秒，与原版 clip 逐位相同）。</summary>
        public const float PulseRiseTime = 1.4333332777f;
        public const float PulsePeakTime = 1.4500000477f;
        public const float PulseFallTime = 1.5833333731f;
        public const float PulseEndTime = 1.8333333731f;

        /// <summary>峰值比值（原版 288.97699 / 250 等三轴，逐位）—— 断言直接对它比。</summary>
        public const float PulsePeakX = 1.155907958984375f;    // 288.97698974609375 / 250
        public const float PulsePeakY = 1.1559200439453125f;   // 288.9800109863281  / 250
        public const float PulsePeakZ = 1.0172164306640625f;   // 254.30410766601562 / 250

        /// <summary>末键的斜率（原版 72.92987823486328 / 68.22312927246094 / 68.6689224243164，÷250 换算成**比值**量纲）。</summary>
        const float PulseSlopeX = 0.29171951293945312f;
        const float PulseSlopeY = 0.27289251708984376f;
        const float PulseSlopeZ = 0.27467568969726563f;

        /// <summary>当前比值（缺省 = 1）。断言/诊断读口。</summary>
        public Vector3 PulseRatio { get { return _pulseRatio; } }
        /// <summary>脉冲是否在跑。</summary>
        public bool PulseActive { get { return _pulseRunning; } }
        /// <summary>`BeginPulse` 被调的次数（= `DoAnim` 的次数，除非手工调）。</summary>
        public int PulseStarts { get; private set; }

        Vector3 _pulseRatio = Vector3.one;
        float _pulseT;
        bool _pulseRunning;
        bool _warnedNoPulseTarget;

        /// <summary>起一次根脉冲（`DoAnim` 里调；手工调用于自检）。**先归 1 再计时**，重复调用不会叠加。</summary>
        public void BeginPulse()
        {
            PulseStarts++;
            _pulseT = 0f;
            _pulseRunning = true;
            _pulseRatio = Vector3.one;
            if (pulseTarget == null && !_warnedNoPulseTarget)
            {
                _warnedNoPulseTarget = true;
                Debug.LogWarning("[CardDisplayLevelUp] `pulseTarget` 没接线 ⇒ 原版 `Level Up Card` clip 第 ① 条"
                               + " scale 曲线（`CardUI` 250→288.98→250，即 ×1.1559 的根脉冲）**不作用**"
                               + "（应传槽 0 的 `CardView`；⛔ 不是静默：这条只出声一次）");
            }
            ApplyPulse();
        }

        /// <summary>收尾：比值归 1（`AnimationFinished` 与「末键之后」都调它）。重复调用无害。</summary>
        public void ResetPulse()
        {
            _pulseRunning = false;
            _pulseT = 0f;
            _pulseRatio = Vector3.one;
            if (pulseTarget != null) pulseTarget.SetExtraScale(Vector3.one);
        }

        void StepPulse(float dt)
        {
            if (!_pulseRunning) return;
            _pulseT += dt;
            ApplyPulse();
        }

        void ApplyPulse()
        {
            if (!_pulseRunning) return;
            if (_pulseT >= PulseEndTime) { ResetPulse(); return; }     // 末键之后曲线恒 1
            if (pulseTarget == null) return;                           // 目标被重建/销毁 ⇒ 不写，等收尾
            var cs = PulseCurves();
            _pulseRatio = new Vector3(cs[0].Evaluate(_pulseT), cs[1].Evaluate(_pulseT), cs[2].Evaluate(_pulseT));
            pulseTarget.SetExtraScale(_pulseRatio);
        }

        static AnimationCurve[] _pulseCurves;

        /// <summary>三轴各一条（原版就是三条独立曲线，x 与 y 在峰值差 0.000012、z 是另一档）。
        /// ⚠️ **逐键必须给 `inTangent/outTangent`**：`new Keyframe(t, v)` 是 0 斜率、与原版一致，
        /// 但末键那一对非 0（72.93/68.22/68.67 ÷250）—— 少了它整段就变成「冲到峰值再硬落」，
        /// 原版那一下 **0.99929 的欠冲**也会消失（端点断言抓不到，只有中段能抓）。</summary>
        static AnimationCurve[] PulseCurves()
        {
            if (_pulseCurves != null) return _pulseCurves;
            _pulseCurves = new[]
            {
                PulseCurve(PulsePeakX, PulseSlopeX),
                PulseCurve(PulsePeakY, PulseSlopeY),
                PulseCurve(PulsePeakZ, PulseSlopeZ),
            };
            return _pulseCurves;
        }

        static AnimationCurve PulseCurve(float peak, float slope)
        {
            var c = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(PulseRiseTime, 1f),
                new Keyframe(PulsePeakTime, peak),
                new Keyframe(PulseFallTime, 1f, slope, slope),
                new Keyframe(PulseEndTime, 1f));
            return c;
        }
    }
}
