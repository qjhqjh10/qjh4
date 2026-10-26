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

        /// <summary>原版 `Update`。批处理没有帧循环 ⇒ 自检/建场时用 <see cref="Step"/> 手推。</summary>
        void Update() { Step(Time.deltaTime); }

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
    }
}
