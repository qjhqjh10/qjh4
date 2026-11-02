// BattleHudButton.cs — 一颗 HUD 件的 **uGUI 点击适配器**（`T09` 整层换 uGUI · 步骤 A1）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它解决什么】HUD 的件已经迁到 uGUI（`HudPiece` 的 uGUI 支），但点击此前一直走
//   `BattleDriver` 手搓的 `Contains` / `HitPaddedRect` + 两条手搓边沿。
//   `T09` 第 4 步要把它们逐个换成 uGUI：**置 `raycastTarget = true`、由 `EventSystem` 的射线命中、
//   由 `IPointerClickHandler` 收点击**。本类就是「一颗件 → 一个点击动作」的那一跳。
//
// 🔴 **为什么是 `IPointerClickHandler` 而不是自己判「按下+松开」**：
//   原版那几颗钮全是 uGUI `EverguildButton`（继承 `UnityEngine.UI.Button`，`T09` §1·4 有逐条反编译证据），
//   而 `Button.OnPointerClick` 由 `EventSystem` 在**松手那一帧**派发（本仓 `PollInputEdges` 的注释里
//   有完整链路：`StandaloneInputModule.ReleaseMouse()` → `ExecuteEvents.pointerClickHandler`）。
//   ⇒ 我们实现同一个接口，**语义与两条边沿逐字对齐**，但判据只剩这一份（不再手搓）。
//
// 🔴 **同一批必须退掉手搓那条路**（`T09` §四 R1）：一颗钮**同时**有 `Button.onClick` 和
//   `Contains` 判据 ⇒ 一次点击走两条路（开两次窗 / 结束两次回合），**画面完全正常**。
//   A1 的做法 = `BattleDriver.HandleOffensiveButton()` 保留成 `=> false`（只给断言读），
//   `BattleDriver.Update` 与 `TickHudButtonsForTest` 里那两处调用一并删掉。
//
// 【测试口】
//   · `ClickForTest()`          —— **只走 `OnPointerClick` 那一个动作**（= `Button.onClick` 的等价物），
//                                   ⛔ 不做命中/边沿判定（那两件事归 `EventSystem` 的射线 + 本接口）。
//   · `ClickCountForTest`       —— 这个动作被触发了几次（**灭自证**：uGUI 点一次 = 1；再喂不经过
//                                  `EventSystem` 的按下/松开 ⇒ 仍是 1）。
//   · `ResetClickCountForTest()` —— 清计数（自检按段用）。
//   · `LastClickCueForTest`     —— 🆕 2026-11-02（`B117-G10-03`）上一次真播的 cue（`null` = 没播）。
//                                  自检**不读音频系统**（批处理没有声卡），只认这个记账位。
//
// 🆕 **2026-11-02（`B117-G10-03`）：点击音**（原版 `EverguildButton.OnPointerClick` 的
//   `m_onClickSound` / `useDefaultAudioClipIfNull`）。字段 = `clickCue`（`null` = 静音，**默认**），
//   逐颗表与「为什么默认静音」写在 `clickCue` 的 doc 里（⛔ 别整族一刀切：原版有 2 颗是静的）。
//
// 【建不出来时**出声**】`Attach` 的三种失败（件是 null / 件还在世界空间 / 动作是 null）都 `LogError`
//   并返回 null；调用点必须把「这颗钮点不动」说出来（⛔ 不许静默半迁移）。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardPresentation
{
    /// <summary>把一颗 uGUI HUD 件变成可点件：`raycastTarget = true` + `IPointerClickHandler`。
    /// 一个 GameObject 只能有一个（`[DisallowMultipleComponent]`）。</summary>
    [DisallowMultipleComponent]
    public sealed class BattleHudButton : MonoBehaviour, IPointerClickHandler
    {
        Action _onClick;
        int _clickCount;
        string _lastCue;

        /// <summary>🆕 **2026-11-02（`B117-G10-03`）：点中时播哪条 cue**（原版 `EverguildButton.m_onClickSound`
        /// 的等价物）。`null` = **不播**（默认）。
        /// <para>🔴 **判据（`Battle Arena 1.unity` 的 39 颗 `EverguildButton` 逐颗现读）**：
        /// `useDefaultAudioClipIfNull = 1` 的 **37 颗**在 `OnPointerClick` 里播
        /// `SoundManager.defaultButtonSound`（= 我们的 `WindowSounds.DefaultButtonCue`，cue `ButtonClick`）；
        /// 4 颗带自己的 cue；**2 颗静音**（`Voice Over Button` / `Continue Button`，`useDefault… = 0` 且无 cue）。
        /// 反编译：`cpp2il/out_il2csharp/Assembly-CSharp/EverguildButton.cs:1008-1080`（`OnPointerClick` 后半段）。</para>
        /// <para>⚠️ **为什么默认是「不播」而不是「播默认音」**：本适配器不只服务 `EverguildButton`
        /// （42 处挂载里有纯命中面，例如详情窗那层铺满全屏的 `cdw_background_close` —— 原版 `BackgroundCloseButton`
        /// 是**纯 MonoBehaviour、没有按钮音**）⇒ 统一打开会**凭空多一声**。由调用点按上表逐颗给值。</para></summary>
        public string clickCue;

        /// <summary>上一次 `Fire()` 播的 cue（`null` = 没播）。自检用 —— ⛔ 不去读音频系统（批处理里没有声卡）。</summary>
        public string LastClickCueForTest { get { return _lastCue; } }

        /// <summary>这个动作被触发了几次（自检口；见文件头「测试口」）。</summary>
        public int ClickCountForTest { get { return _clickCount; } }

        /// <summary>挂上动作了吗（诊断用）。</summary>
        public bool HasAction { get { return _onClick != null; } }

        /// <summary>把 <paramref name="piece"/> 挂成一颗 uGUI 钮。**幂等**：同一件重复调只更新动作。
        /// <para>做的事：① `Image.raycastTarget = true`（`HudPiece.SetRaycastTarget`，判据只那一处）
        /// ② 保证本组件在它的 GameObject 上 ③ 记下动作。</para>
        /// <para>⚠️ 本组件**不需要** `Awake/OnEnable`（编辑器不派生命周期也不影响它）；
        /// 点击由 `EventSystem` 直接调 `OnPointerClick`。</para>
        /// <returns>成功 ⇒ 组件；失败 ⇒ null（**并且已经 `LogError` 说明原因**）。</returns></summary>
        public static BattleHudButton Attach(HudPiece piece, Action onClick)
        {
            if (piece == null)
            {
                Debug.LogError("[BattleHudButton] 件是 null ⇒ **挂不上**（调用点应先确认这一件建出来了）");
                return null;
            }
            if (!piece.IsUgui)
            {
                Debug.LogError("[BattleHudButton] `" + piece.name + "` 还在**世界空间**（`ImageQuad`）"
                             + " ⇒ 它没有 `raycastTarget`、uGUI 也打不到它；⛔ 本类不做世界空间命中");
                return null;
            }
            if (onClick == null)
            {
                Debug.LogError("[BattleHudButton] `" + piece.name + "` 的动作是 null ⇒ 挂上去也会「点得动但什么都不做」");
                return null;
            }
            var go = piece.gameObject;
            if (go == null)
            {
                Debug.LogError("[BattleHudButton] 件的 GameObject 已销毁 ⇒ 挂不上");
                return null;
            }
            if (!piece.SetRaycastTarget(true))
            {
                Debug.LogError("[BattleHudButton] `" + piece.name + "` 置 `raycastTarget` **失败**"
                             + "（`HudPiece.SetRaycastTarget` 返回 false —— 见它自己那条日志）");
                return null;
            }

            var btn = go.GetComponent<BattleHudButton>();
            if (btn == null) btn = go.AddComponent<BattleHudButton>();
            btn._onClick = onClick;
            return btn;
        }

        /// <summary>🆕 **2026-11-02（`B117-G10-03`）**：同上 + **指定点击音**。
        /// <para>`cue` = 原版那一颗的 `m_onClickSound` 名；要「默认按钮音」就传
        /// <see cref="WindowSounds.DefaultButtonCue"/>（cue `ButtonClick`）；`null` = 静音（与旧签名逐位相同）。</para>
        /// <para>逐颗判据 = 文件头那段 37/4/2 的表（⛔ 别整族一刀切）。</para></summary>
        public static BattleHudButton Attach(HudPiece piece, Action onClick, string cue)
        {
            var btn = Attach(piece, onClick);
            if (btn != null) btn.clickCue = cue;
            return btn;
        }

        /// <summary>uGUI 点击（**松手那一帧**由 `EventSystem` 派发；与 `Button.onClick` 同一条链）。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Fire();
        }

        /// <summary>自检口：**只走点击动作**（不做命中/边沿 —— 那两件事归 `EventSystem`）。</summary>
        public void ClickForTest()
        {
            Fire();
        }

        /// <summary>自检口：清计数。</summary>
        public void ResetClickCountForTest()
        {
            _clickCount = 0;
        }

        void Fire()
        {
            if (_onClick == null)
            {
                // ⛔ 不许静默：挂上了却没有动作，说明 `Attach` 之后有人把它清掉了。
                Debug.LogError("[BattleHudButton] `" + name + "` 被点了，但**没有动作**（`Attach` 时传了 null？）");
                return;
            }
            _clickCount++;
            // 🔴 先取名字：`_onClick` 里可能**把这一件拆了**（关窗 / 重建列表），拆完再读 `name`
            //   会抛 `MissingReferenceException`（同 `Shell/WindowButton.Click` 那条已记的坑）。
            string nm = name;
            // 原版次序（`EverguildButton.OnPointerClick`）：先 `base.OnPointerClick`（派 `m_OnClick` 那一路）
            // 再 `PlayButtonSound()` ⇒ 我们照它写成「先动作、后音」。⚠️ 只响一声（cue 为 null 就不响）。
            _onClick();
            if (this == null) return;           // 已销毁：连音也不再放（护栏，⛔ 不吞别的异常）
            if (!string.IsNullOrEmpty(clickCue))
            {
                _lastCue = clickCue;
                // 播放口与外壳同一条（`Shell/WindowSounds.PlayFx` → `WFSoundPlayer` 的 FX 组）——
                // ⛔ 别在这里另开一条音频路。取不到 cue 时 `PlayFx` 自己会出声（它有一条「表里查不到」的告警）。
                WindowSounds.PlayFx(clickCue, "战斗按钮 `" + nm + "`");
            }
        }
    }
}
