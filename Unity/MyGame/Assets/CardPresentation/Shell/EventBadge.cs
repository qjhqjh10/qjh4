// EventBadge.cs — 原版 `EventBadge : MonoBehaviour, ToastNotificationHandler`（活动角标 / 红点）。
//
// ============================ 判据（第一权威 = 反编译，本轮现读）============================
//   · `out_cpp2il_dev_ilspy/EventBadge.cs`（类头 + 四方法全文）
//   · `decomp_full/EventBadge__{Awake,Initialize,SetNotificationData,ShowNotification}.c`（**常量全在 `.c` 里**）
//   · 常量用 `工具/read_literal.py` 从 `GameAssembly.dll` 的 `.rdata` 读出（4B float，全部「像字面量」✅）：
//       `DAT_1834b2dc4` = **0.1**（首段 AppendInterval）
//       `DAT_1834b2bb0` = **0.2**（punch 幅度倍数 · 也是 DOFade 的时长）
//       `DAT_1834b2e7c` = **0.25**（punch 时长，vibrato = 1）
//       `DAT_1834b2bb8` = **1**（holder 的 fill 目标 / fade 目标）
//       `DAT_1834b3158` = **0.4**（fill 时长；与 `CardScript.DoPushBack` 同一个 0.4 字面量）
//       `DAT_1834b2bc4` = **+360** · `DAT_1834b2bcc` = **−360**（两段 DORotate 的 z 分量）
//       `DAT_1834b2bbc` = **2.0**（中间那段 AppendInterval）
//   ⇒ 序列（`.c` 逐句）：`SetNotificationData` → `Sequence()` → `AppendInterval(0.1)` → `Append(icon.DOPunchScale(基向量×0.2, 0.25, 1))`
//      → `Append(holder.DOFillAmount(1, 0.4))` → `Join(holder.DOFade(1, 0.2).From(0))`
//      → `Join(icon.DORotate((0,0,+360), 0.4, WorldAxisAdd))` → `AppendInterval(2.0)`
//      → `Append(holder.DOFillAmount(0, 0.4))` → `Join(icon.DORotate((0,0,−360), 0.4, WorldAxisAdd))`
//
// ============================ 本文件**没有**做的三格（⛔ 别当成「已复刻」）============================
//   ① 🔴 **prefab 几何 / 节点树**：`assets_full` 全量按 `notificationHolder`/`notificationText` 字段名搜
//      = **0 命中**（本 build 的 `EventBadge` prefab 不在离线包里；它是活动组件 `EventComponents[25]` 的远端引用）
//      ⇒ **不建树**。本类改成「由宿主 `Bind(...)` 把 6 个件交进来」，没绑就出声（⛔ 不静默）。
//   ② 🔴 **`RegisterNotificationHandler` 的实参 id**：`EventBadge__Initialize.c` 里是
//      `uVar3 = FUN_1800021f0(0, DAT_1842a4970); RegisterNotificationHandler(this, uVar3)`
//      —— 那个 slot-0 接口调用 = **`IIdentifiable.GetID()`**（`ILiveOps : IIdentifiable, IContainerProvider`，
//      `IIdentifiable` 槽 0；旁证 = 我们自己的 `Shell/RewardEvent.HandleRewardDisplay` 把 `ctx.ControllerId = GetID()`）
//      ⇒ 取 `liveOps.GetID()`。**这是有判据的推断（两处旁证），不是默认值**；⛔ 仍待一次 VA 复核那一句的接口类。
//   ③ 🔴 **两处未解**：`DOFade` 之后那个 `FUN_180dc57c0(tween,0,1,0,…)`（ILSpy = `Method not found @180DC57C0`，
//      看着像 `SetEase/SetDelay` 一族）**没复刻**；punch 的**基向量**来自 `Vector3` 的一个静态字段
//      （`.c`：`*(float*)(*(Il2CppClass<Vector3> +0xb8) + 0xc / +0x14)`），本机读不到它的运行值 ⇒
//      做成**可覆写字段** `PunchBase`，默认 `Vector3.one`（DOTween 语义下 punch 是偏移，恒 0 = 什么都不动 ⇒
//      `Vector3.one` 是唯一非退化候选，**但这仍是我们挑的**，见下方 TODO）。
//   ④ `Image.fillAmount` 在本仓**没有等价物**（`Core/Battle/ImageQuad` 无 fill）⇒ `SetFill(f)` 用 **uv 左→右显影**
//      （`SetUvRect(0,0,f,1)`）当等价物；原版 `m_FillMethod` 未读 ⇒ TODO。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `EventBadge`（活动角标：`ToastNotificationHandler` 的一个实现）。
    /// <para>⚠️ 只复刻**逻辑那半边**（判据齐）；prefab 几何不在离线包里 ⇒ 视觉节点由宿主 `Bind(...)` 交进来。</para>
    /// <para>📌 常驻状态机：`ShowNotification` 起一条**内部时间线**（本仓没有运行时 DOTween，界面线一贯自带 `Tick`），
    /// `Update()` 只把 `Time.deltaTime` 转进 `TickForTest`（自检可直调）。</para></summary>
    public class EventBadge : MonoBehaviour, ToastNotifications.IHandler
    {
        // ---- 原版 7 个序列化字段（顺序照 ILSpy：+0x20 holder / +0x28 text / +0x30 image / +0x38 drawerHolder / +0x40 icon / +0x48 button / +0x50 drawer）----
        public ImageQuad notificationHolder;   // +0x20 `Image`（`fillAmount` 那颗；等价物见 SetFill）
        public Label notificationText;         // +0x28 `TextMeshProUGUI`
        public ImageQuad notificationImage;    // +0x30 `Image`
        public Transform drawerHolder;         // +0x38（`ItemDrawer` 的父）
        public Transform icon;                 // +0x40（punch/rotate 的目标）
        /// <summary>+0x48 `EverguildButton.m_OnClick` 那颗 UnityEvent（原版 `Initialize` 往里 `AddListener`）。</summary>
        public UnityEngine.Events.UnityEvent buttonOnClick;
        /// <summary>+0x50 `ItemDrawer`（运行时由 `ItemDrawer.Draw` 造出来，**不是**序列化字段）。</summary>
        object drawer;

        /// <summary>+0x58 `ILiveOps liveOps`。</summary>
        public ILiveOps LiveOps { get; private set; }

        /// <summary>punch 的基向量。🔶 **这是我们挑的**：原版那个分量是 `Vector3` 的一个静态字段（见文件头 ③），
        /// 本机读不到 ⇒ 默认 `Vector3.one`（幅度由 ×0.2 给）。将来 VA 解出来就改这一行。</summary>
        public Vector3 PunchBase = Vector3.one;

        // ---- 内部时间线（等价于原版那条 DOTween Sequence）----
        const float IntervalA = 0.1f;     // DAT_1834b2dc4
        const float PunchDur = 0.25f;     // DAT_1834b2e7c
        const float PunchScale = 0.2f;    // DAT_1834b2bb0（幅度倍数；也是 fade 时长）
        const float FillDur = 0.4f;       // DAT_1834b3158
        const float FadeDur = 0.2f;       // DAT_1834b2bb0
        const float HoldDur = 2.0f;       // DAT_1834b2bbc
        const float RotateZ = 360f;       // DAT_1834b2bc4 / 取负 = DAT_1834b2bcc
        const int PunchVibrato = 1;       // 调用第 4 实参

        float _t = -1f;                   // <0 = 没在播
        Vector3 _iconScale0 = Vector3.one;
        Vector3 _iconEuler0 = Vector3.zero;
        int _warned;

        void Awake()
        {
            // 原版 `Awake()` = 一句 `notificationHolder.fillAmount = 0f;`（`decomp_full/EventBadge__Awake.c` 全文 12 行）
            SetFill(0f);
        }

        // ============================================================ Bind（宿主交件；原版是 prefab 序列化，我们没几何）
        /// <summary>宿主把 6 个件交进来（原版由 prefab 序列化给）。没绑的件 ⇒ 对应那一段**跳过并出声一次**。</summary>
        public void Bind(ImageQuad holder, Label text, ImageQuad image, Transform drawerParent, Transform iconTf,
                         UnityEngine.Events.UnityEvent onClick)
        {
            notificationHolder = holder; notificationText = text; notificationImage = image;
            drawerHolder = drawerParent; icon = iconTf; buttonOnClick = onClick;
        }

        // ============================================================ Initialize（原版 :45-62）
        /// <summary>原版 `Initialize(ILiveOps liveOps)`：存 `liveOps`（+0x58）→
        /// `RegisterNotificationHandler(this, liveOps.GetID())` → `button.m_OnClick.AddListener(() =&gt; Debug.Log("Click"))`。
        /// <para>⚠️ 原版按钮回调**真的只有一句 `Debug.Log("Click")`**（`DisplayClass9_0.&lt;Initialize&gt;b__0`）——照抄，⛔ 别接别的动作。</para></summary>
        public void Initialize(ILiveOps liveOps) { Initialize(liveOps, liveOps == null ? null : liveOps.GetID()); }

        /// <summary>带显式 handler id 的版本（= 原版那一句的实参；见文件头 ② 的判据与旁证）。</summary>
        public void Initialize(ILiveOps liveOps, string handlerId)
        {
            LiveOps = liveOps;
            if (liveOps == null) { WarnOnce("[EventBadge] Initialize(null)：原版这一档 NRE ⇒ 不注册"); return; }
            if (string.IsNullOrEmpty(handlerId))
            {
                WarnOnce("[EventBadge] Initialize：handler id 为空（原版 = `liveOps.GetID()`）⇒ 不注册（⛔ 不拿空串当键）");
                return;
            }
            ToastNotifications.RegisterNotificationHandler(handlerId, this);
            if (buttonOnClick != null)
                buttonOnClick.AddListener(() => Debug.Log("Click"));
        }

        // ============================================================ SetNotificationData（原版 :96-190）
        /// <summary>原版 `SetNotificationData(ToastNotificationContext context)`，逐支：
        /// ① `context.Icon == null` ⇒ `notificationImage.sprite = null; notificationImage.gameObject.SetActive(false)`（**没有 else**，原版不会重新打开）；
        /// ② `drawer != null` ⇒ `Destroy(drawer); drawerHolder.gameObject.SetActive(false)`；
        /// ③ `context.Data is RewardInfo` ⇒ `drawerHolder.SetActive(true)` + `drawer = ItemDrawer.Draw(drawerHolder, item, 1, DrawerOverride.Icon)`；
        /// ④ `notificationText.text = context.Text`。</summary>
        public void SetNotificationData(ToastNotifications.Context context)
        {
            // ① 图标：本仓 `Context` 用**图名**（`IconArt`）而不是 Sprite 引用
            if (string.IsNullOrEmpty(context.IconArt))
            {
                if (notificationImage != null)
                {
                    notificationImage.SetTexture(null);
                    notificationImage.gameObject.SetActive(false);   // 原版这一支没有 else ⇒ 不重新打开
                }
            }

            // ② 清掉上一次的 drawer
            if (drawer != null)
            {
                Destroy(drawer as UnityEngine.Object);
                drawer = null;
                if (drawerHolder != null) drawerHolder.gameObject.SetActive(false);
            }

            // ③ `Data is RewardInfo` 那一段 —— 🔴 **做不了**：本仓 `ToastNotifications.Context` **没有 `Data` 槽**
            //    （`Shell/ToastNotifications.cs` 的 `Context` 只有 Text/IconArt/OnClick/ControllerId/Type + 任务四件套）
            //    ⇒ 这一支永远进不去。TODO(判据缺一片)：上游是 `ToastNotifications.Context` 加 `object Data`（**不在本批白名单**）。
            //    原版判据 = `decomp_full/EventBadge__SetNotificationData.c:44-70`（`ctx+0x38` 的 runtime type 比对 `RewardInfo`）。
            if (context.ShowProgress)
                WarnOnce("[EventBadge] SetNotificationData：原版第 ③ 段（`Data is RewardInfo` ⇒ `ItemDrawer.Draw`）**没接** —— "
                       + "本仓 `ToastNotifications.Context` 缺 `Data` 槽（账 `日常_奖励` 第 10 行仍为 B）");

            // ④ 文本（原版最后一句：`notificationText.text = ctx.Text`，`.c` 里是 slot 0x558 = `set_text`）
            if (notificationText != null) notificationText.SetText(context.Text ?? "");
        }

        // ============================================================ ShowNotification（原版 :64-93；常量见文件头）
        /// <summary>原版 `ShowNotification(ToastNotificationContext context)`：`SetNotificationData` 后起那条 8 拍序列。
        /// <para>⚠️ 时序已按 `.c` 逐拍复刻（见文件头常量）；⚠️ `DOFade` 之后那个 `FUN_180dc57c0`（未解）**没复刻**。</para></summary>
        public void ShowNotification(ToastNotifications.Context context)
        {
            SetNotificationData(context);
            if (notificationHolder == null)
            {
                WarnOnce("[EventBadge] ShowNotification：`notificationHolder` 没绑（prefab 几何不在离线包里）⇒ 整条序列不播");
                return;
            }
            if (icon != null) { _iconScale0 = icon.localScale; _iconEuler0 = icon.localEulerAngles; }
            _t = 0f;
        }

        /// <summary>推时间线（自检口；`Update()` 只是把 `Time.deltaTime` 转进来）。</summary>
        public void TickForTest(float dt) { Tick(dt); }

        void Update()
        {
            if (!Application.isPlaying) return;                 // 批处理/编辑器没有帧循环（同 `Shell/InboxWindow` 的口径）
            Tick(Time.deltaTime);
        }

        /// <summary>当前这条时间线是否还在播（自检口）。</summary>
        public bool IsPlayingForTest { get { return _t >= 0f; } }

        void Tick(float dt)
        {
            if (_t < 0f) return;
            _t += dt;
            // 8 拍的时间轴（起点 = 0）：
            //   0.00 起 interval 0.1
            //   0.10 → 0.35  punch（弹一下）
            //   0.10 → 0.50  fill 0→1（Append，与 punch 串在同一拍上：原版 punch 的 Append 之后紧跟 fill 的 Append）
            //   0.10 → 0.30  fade 0→1（Join）
            //   0.10 → 0.50  rotate z 0→+360（Join）
            //   0.50 → 2.50  hold
            //   2.50 → 2.90  fill 1→0
            //   2.50 → 2.90  rotate z 0→−360（Join）
            const float tPunch = IntervalA + PunchDur;                 // 0.35
            const float tFill1 = IntervalA + FillDur;                  // 0.50
            const float tFade = IntervalA + FadeDur;                   // 0.30
            const float tHoldEnd = tFill1 + HoldDur;                   // 2.50
            const float tEnd = tHoldEnd + FillDur;                     // 2.90

            // punch：`DOPunchScale(base × 0.2, 0.25, vibrato 1)` ⇒ 在 0.1..0.35 内按 sin 弹一次
            if (icon != null && _t >= IntervalA && _t <= tPunch)
            {
                float p = (_t - IntervalA) / PunchDur;
                float amp = Mathf.Sin(p * Mathf.PI);                    // vibrato = 1 ⇒ 单峰
                Vector3 a = PunchBase * PunchScale * amp;
                icon.localScale = new Vector3(_iconScale0.x * (1f + a.x), _iconScale0.y * (1f + a.y), _iconScale0.z * (1f + a.z));
            }
            else if (icon != null && _t > tPunch) icon.localScale = _iconScale0;

            // fill：0.1→0.5 补到 1；2.5→2.9 补到 0
            if (_t > IntervalA && _t < tFill1) SetFill(Mathf.InverseLerp(IntervalA, tFill1, _t));
            else if (_t >= tFill1 && _t < tHoldEnd) SetFill(1f);
            else if (_t >= tHoldEnd && _t <= tEnd) SetFill(1f - Mathf.InverseLerp(tHoldEnd, tEnd, _t));

            // fade：0.1→0.3 alpha 0→1（原版 `DOFade(1, 0.2).From(0)`；`.c` 里目标 = 1，时长 = 0.2）
            if (notificationHolder != null)
            {
                float fa = _t <= IntervalA ? 0f : Mathf.Clamp01((_t - IntervalA) / FadeDur);
                var c = notificationHolder.Tint; c.a = fa; notificationHolder.SetTint(c);
            }

            // rotate：0.1→0.5 z 0→+360；2.5→2.9 z 0→−360（RotateMode.WorldAxisAdd = 累加）
            if (icon != null)
            {
                float rz = 0f;
                if (_t >= IntervalA && _t <= tFill1) rz = RotateZ * Mathf.InverseLerp(IntervalA, tFill1, _t);
                else if (_t > tFill1 && _t < tHoldEnd) rz = RotateZ;
                else if (_t >= tHoldEnd) rz = RotateZ - RotateZ * Mathf.InverseLerp(tHoldEnd, tEnd, _t);
                icon.localEulerAngles = new Vector3(_iconEuler0.x, _iconEuler0.y, _iconEuler0.z + rz);
            }

            if (_t > tEnd)
            {
                // 收尾：回到起点（原版 tween 结束即落回目标值；fill 归 0、旋转回原角、缩放松手）
                SetFill(0f);
                if (icon != null) { icon.localScale = _iconScale0; icon.localEulerAngles = _iconEuler0; }
                _t = -1f;
            }
        }

        /// <summary>原版 `notificationHolder.fillAmount = f`（0..1）。
        /// ⚠️ 本仓 `ImageQuad` **没有 `fillAmount`** ⇒ 等价物 = **uv 左→右显影**（`SetUvRect(0,0,f,1)`）。
        /// TODO(判据缺一片)：原版 `Image.m_FillMethod` 未读（Horizontal / Radial360 会不一样）。</summary>
        public void SetFill(float f)
        {
            if (notificationHolder == null) return;
            f = Mathf.Clamp01(f);
            notificationHolder.SetUvRect(new Rect(0f, 0f, f, 1f));
        }

        void WarnOnce(string msg)
        {
            if (_warned >= 3) return;
            _warned++;
            Debug.LogWarning(msg);
        }

#if UNITY_EDITOR
        // ============================================================ 结构性自检钩子（B92_N6b）
        /// <summary>结构性自检（不依赖场景/资产）。返回**问题清单**，空 = 通过。</summary>
        public static List<string> SelfCheck()
        {
            var errs = new List<string>();
            var t = typeof(EventBadge);
            if (!typeof(ToastNotifications.IHandler).IsAssignableFrom(t)) errs.Add("没实现 `ToastNotifications.IHandler`（原版实现 `ToastNotificationHandler`）");
            foreach (var n in new[] { "Awake", "Initialize", "SetNotificationData", "ShowNotification" })
            {
                bool any = false;
                foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                    if (m.Name == n) { any = true; break; }
                if (!any) errs.Add("缺方法 `" + n + "`（原版有）");
            }
            foreach (var f in new[] { "notificationHolder", "notificationText", "notificationImage", "drawerHolder", "icon", "buttonOnClick" })
                if (t.GetField(f) == null) errs.Add("缺字段 `" + f + "`（原版 7 个序列化字段里的一个）");
            // 常量核对（判据 = 文件头那 8 个 .rdata 字面量）：deviation ⇒ 有人改了数没改账
            if (!Mathf.Approximately(IntervalA, 0.1f)) errs.Add("IntervalA ≠ 0.1（DAT_1834b2dc4）");
            if (!Mathf.Approximately(PunchScale, 0.2f)) errs.Add("PunchScale ≠ 0.2（DAT_1834b2bb0）");
            if (!Mathf.Approximately(PunchDur, 0.25f)) errs.Add("PunchDur ≠ 0.25（DAT_1834b2e7c）");
            if (!Mathf.Approximately(FillDur, 0.4f)) errs.Add("FillDur ≠ 0.4（DAT_1834b3158）");
            if (!Mathf.Approximately(FadeDur, 0.2f)) errs.Add("FadeDur ≠ 0.2（DAT_1834b2bb0）");
            if (!Mathf.Approximately(HoldDur, 2.0f)) errs.Add("HoldDur ≠ 2.0（DAT_1834b2bbc）");
            if (!Mathf.Approximately(RotateZ, 360f)) errs.Add("RotateZ ≠ 360（DAT_1834b2bc4）");
            if (PunchVibrato != 1) errs.Add("PunchVibrato ≠ 1（调用第 4 实参）");
            return errs;
        }
#endif
    }
}
