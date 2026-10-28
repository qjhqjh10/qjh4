// UiBadgeNotification.cs — 红点徽记组件（原版 `UiBadgeNotification` 的复刻）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `Shell/BadgeNotifications.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    🔴 实证：`DeckScene.Run` 重生成的 `Scenes/DeckEditor.unity` 里出现了 1 个 `!u!115` 存根，
//       `m_EditorClassIdentifier` 正是 `CardPresentation.UiBadgeNotification`。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里、把「判据见文件头 ④」指回 `BadgeNotifications.cs`）。
//    **通知类型 / 聚合器 / provider / 接线**仍在 `BadgeNotifications.cs`（同一 `CardPresentation` 命名空间）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `UiBadgeNotification`（挂在 35×35 红点节点上）。**判据见 `BadgeNotifications.cs` 文件头 ④**。
    /// <para>它**不自己画**：alpha 与计数文本通过 <see cref="SetAlpha"/> / <see cref="SetCountText"/>
    /// 两个钩子交给宿主（红点 quad / 计数 `Label`）⇒ 同一份逻辑能挂世界档（`ImageQuad`）与
    /// 将来的 uGUI 档，不用两套。</para>
    /// </summary>
    public sealed class UiBadgeNotification : MonoBehaviour
    {
        public List<BadgeNotificationDefinition> notificationDefinitions = new List<BadgeNotificationDefinition>();
        public bool showCounter = true;

        /// <summary>alpha 输出（0..1）——宿主接到红点图 + 计数文字的色上。</summary>
        public Action<float> SetAlpha;
        /// <summary>计数文本输出（`""` = 空，原版 `Hide()` 就是写空串）。</summary>
        public Action<string> SetCountText;

        /// <summary>当前计数（原版 `CurrentCount`）。</summary>
        public int CurrentCount { get; private set; }
        /// <summary>目标 alpha（自检读口）。</summary>
        public float TargetFade { get { return _target; } }
        /// <summary>还在补间吗（原版 `enabled`）。</summary>
        public bool Fading { get { return _fading; } }

        float _alpha;
        float _target;
        bool _fading;

        /// <summary>原版 `Update` 的步进速率：**3 / 秒**，差 ≤ **0.01** 贴目标并停（`UiBadgeNotification.cs:161-193`）。</summary>
        public const float FadePerSecond = 3f;
        public const float SnapEpsilon = 0.01f;

        void OnEnable() { UiBadgeNotificationManager.OnNotify += OnSignalRaise; }
        void OnDisable() { UiBadgeNotificationManager.OnNotify -= OnSignalRaise; }

        /// <summary>原版 `Awake`：alpha 0 + 订阅 + `UpdateCount`（我们由宿主在 `Attach` 里显式 `Recount`）。</summary>
        public void Recount()
        {
            // 🔴 读时刷新（文件头「有意不同」那一节）：先让 provider 按**当前数据**重算一遍。
            //    ⚠️ `notify:false` —— 广播会回调到本组件的 `OnSignalRaise → Recount`（无限递归）。
            UiBadgeNotificationManager.Refresh(false);
            CurrentCount = UiBadgeNotificationManager.GetCount(notificationDefinitions);
            if (CurrentCount > 0) Show();
            else Hide();
        }

        /// <summary>原版 `Show()`：文本 = 计数（`showCounter` 为假则空），目标 alpha = 1。</summary>
        public void Show()
        {
            if (SetCountText != null) SetCountText(showCounter ? CurrentCount.ToString() : "");
            _target = 1f; _fading = true;
        }

        /// <summary>原版 `Hide()`：文本清空，目标 alpha = 0。</summary>
        public void Hide()
        {
            if (SetCountText != null) SetCountText("");
            _target = 0f; _fading = true;
        }

        /// <summary>原版 `OnSignalRaise(Type)`：信号与**任意一条定义**相关 ⇒ 重数。</summary>
        void OnSignalRaise(Type signal)
        {
            if (signal == null) return;
            for (int i = 0; i < notificationDefinitions.Count; i++)
            {
                var d = notificationDefinitions[i];
                if (d != null && UiBadgeNotificationManager.IsRelated(d.Type, signal)) { Recount(); return; }
            }
        }

        /// <summary>原版 `Update`（🆕 2026-10-29 B94_T1 末尾加 `ReassertAlpha()`：显隐唯一写手，见类尾注记）。批处理没有帧循环 ⇒ 自检/建场时用 <see cref="Step"/> 手推。</summary>
        void Update() { Step(Time.deltaTime); ReassertAlpha(); }

        /// <summary>alpha 逼近目标（3/s，`0.01` 贴值）。返回是否还在补间。</summary>
        public bool Step(float dt)
        {
            if (!_fading) return false;
            if (dt < 0f) dt = 0f;
            float delta = _target - _alpha;
            if (Mathf.Abs(delta) <= FadePerSecond * dt)
            {
                _alpha = _target;
                _fading = false;
            }
            else
            {
                _alpha += Mathf.Sign(delta) * FadePerSecond * dt;
            }
            if (SetAlpha != null) SetAlpha(_alpha);
            return _fading;
        }

        /// <summary>自检用：直接写 alpha（绕开补间；验「终值」用）。</summary>
        public void SnapToTargetForTest()
        {
            _alpha = _target; _fading = false;
            if (SetAlpha != null) SetAlpha(_alpha);
        }

        /// <summary>自检读口：当前 alpha。</summary>
        public float CurrentAlpha { get { return _alpha; } }

        // ============================================================ 显隐唯一写手（B94_T1 · 2026-10-29 追加，类尾）
        // 🔶 自己设计（非原版）：**红点 quad 的 alpha 由本组件独占**，别的代码直写材质色会被下一帧纠回。
        //   · 为什么必须自己定：原版 alpha 住在 `CanvasGroup` 上（`UiBadgeNotification.Awake` 的 `canvasGroup.alpha = 0`），
        //     那份东西**只有本组件写**；我们的红点 quad 是**平铺 `ImageQuad` + 共享材质色**。历史上仓里曾有两条
        //     遗留的直写通道（`RewardsWindow.RefreshBadges` · `SocialWindow.RefreshBadges`）——**已由 B94_Z1（2026-10-29）删除**；
        //     本护栏保留下来，是为了挡住**将来任何**绕开组件直接 `SetTint` 到同一颗 quad 的写手
        //     （谁后写谁赢；且本组件补间一停（`_fading == false`）就不再写 ⇒ 那个值会**永久留屏**）。
        //     ⚠️ 它不是「原版行为」：原版 alpha 在 CanvasGroup 上由组件独占，这里是**载体不同**（材质色）的补偿。
        //     本组件每帧自持，保证「屏幕上画出来的 = 计数算出来的」。
        //   · 谁定的：调度台 2026-10-29（B94_T1 派活「修显隐链」）。
        //   · 代价：每帧一次比较（只在**真的不一致**时才写一次材质色，不是无条件写）。
        /// <summary>宿主给的**回读口**（= 红点 quad 现在的颜色 alpha）。设了才会做「自持」校正；不设 = 原版行为。</summary>
        public Func<float> GetAlpha;

        /// <summary>把外面偷改掉的 alpha 纠回 <see cref="_alpha"/>。见上面那段注记。</summary>
        void ReassertAlpha()
        {
            if (GetAlpha == null || SetAlpha == null) return;
            float cur = GetAlpha();
            if (float.IsNaN(cur)) return;
            if (Mathf.Abs(cur - _alpha) > 0.001f) SetAlpha(_alpha);
        }

        /// <summary>自检口：手动跑一次「自持校正」（批处理没有帧循环 ⇒ `Update` 不跑；语义与 `Update` 末尾那一跳同一份）。</summary>
        public void ReassertForTest() { ReassertAlpha(); }
    }
}
