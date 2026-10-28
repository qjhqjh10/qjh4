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
//   ③ 🆕 **2026-10-28（B93_D4）已解一格**：`FUN_180dc57c0(tween,0,1,0,…)` =
//      **`TweenSettingsExtensions.From<float,float,FloatOptions>(t, 0f, true, false)`**
//      （判据 = `d:/2/tools/il2cpp_out/dump.cs:1010694` 的 `|-RVA: 0xDC57C0` 那一行）
//      ⇒ 那条就是 `DOFillAmount(1, 0.4).From(0)`，= 我们已在做的「fill 从 0 补到 1」。⛔ 别再当成未解。
//      **仍缺**：punch 的**基向量**来自一个**运行期才解的静态结构**（`.c`：`*(float*)(*(klass +0xb8 /*static_fields*/) + 0xc / +0x14)`）
//      ⇒ 本机读不到它的值 ⇒ 做成**可覆写字段** `PunchBase`，默认 `Vector3.one`（DOTween 语义下 punch 是偏移，恒 0 = 什么都不动 ⇒ `Vector3.one` 是唯一非退化候选，**但这仍是我们挑的**）。
//   ④ `Image.fillAmount` 在本仓**没有等价物**（`Core/Battle/ImageQuad` 无 fill）⇒ `SetFill(f)` 用 **uv 左→右显影**（`SetUvRect(0,0,f,1)`）当等价物；原版 `m_FillMethod` 判据为空（EventBadge prefab 不在离线包里）⇒ 见 `SetFill` 的注释。
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

            // ③ `Data is RewardInfo` ⇒ `drawerHolder.SetActive(true)` + `drawer = ItemDrawer.Draw(drawerHolder, item, 1, 10)`
            //    🆕 **2026-10-28（B93_D4）已接**：上游缺口（`ToastNotifications.Context` 没有 `Data` 槽）已补（同批加 `object Data`）。
            //    判据 = `decomp_full/EventBadge__SetNotificationData.c:44-70`（`ctx+0x38` 的 runtime type 比对 `RewardInfo`；第 4 实参 `10` = `DrawerOverride.Icon`）
            if (context.Data is RewardInfo) ApplyRewardDrawer((RewardInfo)context.Data);

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
            // 8 拍的真时间轴（起点 = 0）。
            // 🔴 **2026-10-28（B93_D4）就地订正（铁律 5）**：旧版把 punch/fill/fade/rotate 全盖在 0.10 起（并行），
            //   **那是错的** —— 原版是 DOTween `Sequence`：`Append` 把元素接在**游标之后**、`Join` 接在**当前游标**上
            //   ⇒ 前四拍是**串行**。判据 = `decomp_full/EventBadge__ShowNotification.c` 逐句 +
            //   `cpp2il/out_cpp2il_dev_ilspy/EventBadge.cs` 的 `AppendInterval/Append/Join` 次序：
            //     0.00–0.10  AppendInterval(0.1)                                   DAT_1834b2dc4
            //     0.10–0.35  Append(icon.DOPunchScale(base×0.2, 0.25, vibrato 1))
            //     0.35–0.75  Append(holder.DOFillAmount(1, 0.4).From(0))           ← `From` 那一跳 2026-10-28 已解（见文件头 ③）
            //     0.75–0.95  Join (holder.DOFade(1, 0.2).From(0))                  游标 = fill 之后
            //     0.75–1.15  Join (icon.DORotate((0,0,+360), 0.4, WorldAxisAdd))
            //     1.15–3.15  AppendInterval(2.0)                                   游标 = 两个 Join 的较晚者
            //     3.15–3.55  Append(holder.DOFillAmount(0, 0.4))
            //     3.15–3.55  Join (icon.DORotate((0,0,−360), 0.4, WorldAxisAdd))
            const float tPunch   = T1;                                     // 0.35
            const float tFill1   = T2;                                     // 0.75  ← 旧版写 0.50
            const float tRot1End = T3;                                     // 1.15（fade 0.95 只是它的一小段，不用单列）
            const float tHoldEnd = T4;                                     // 3.15
            const float tEnd     = T5;                                     // 3.55

            // punch：`DOPunchScale(base × 0.2, 0.25, vibrato 1)` ⇒ 只在 0.10..0.35 内按 sin 弹一次
            if (icon != null && _t >= IntervalA && _t <= tPunch)
            {
                float p = (_t - IntervalA) / PunchDur;
                float amp = Mathf.Sin(p * Mathf.PI);                    // vibrato = 1 ⇒ 单峰
                Vector3 a = PunchBase * PunchScale * amp;
                icon.localScale = new Vector3(_iconScale0.x * (1f + a.x), _iconScale0.y * (1f + a.y), _iconScale0.z * (1f + a.z));
            }
            else if (icon != null && _t > tPunch) icon.localScale = _iconScale0;

            // fill：0.35→0.75 补到 1；0.75→3.15 停在 1；3.15→3.55 补回 0
            if (_t > tPunch && _t < tFill1) SetFill(Mathf.InverseLerp(tPunch, tFill1, _t));
            else if (_t >= tFill1 && _t < tHoldEnd) SetFill(1f);
            else if (_t >= tHoldEnd && _t <= tEnd) SetFill(1f - Mathf.InverseLerp(tHoldEnd, tEnd, _t));

            // fade：0.75→0.95 alpha 0→1（`DOFade(1, 0.2).From(0)`）；之后恒 1
            if (notificationHolder != null)
            {
                float fa = _t <= tFill1 ? 0f : Mathf.Clamp01((_t - tFill1) / FadeDur);
                var c = notificationHolder.Tint; c.a = fa; notificationHolder.SetTint(c);
            }

            // rotate：0.75→1.15 z 0→+360；1.15→3.15 停在 +360；3.15→3.55 +360→0（RotateMode.WorldAxisAdd = 累加）
            if (icon != null)
            {
                float rz = 0f;
                if (_t >= tFill1 && _t <= tRot1End) rz = RotateZ * Mathf.InverseLerp(tFill1, tRot1End, _t);
                else if (_t > tRot1End && _t < tHoldEnd) rz = RotateZ;
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

        // ============================================================ 8 拍时间轴的断点（单一来源）
        //   判据 = `decomp_full/EventBadge__ShowNotification.c` 的 `Append/Join` 次序（见 `Tick` 的注释）。
        //   ⚠️ 放在类尾而不是常量区：常量区在第 80/99 行之前，那里插行会让账落点位移（铁律 18·b 的同一条纪律）。
        const float T1 = IntervalA + PunchDur;      // 0.35  punch 结束
        const float T2 = T1 + FillDur;              // 0.75  fill(0→1) 结束（fade / rotate 从这里开始）
        const float T3 = T2 + FillDur;              // 1.15  rotate(+360) 结束
        const float T4 = T3 + HoldDur;              // 3.15  hold 结束
        const float T5 = T4 + FillDur;              // 3.55  全段结束

        /// <summary>自检口：8 拍断点（秒 · 相对起播）。**同一来源** = 上面那 5 个常量（`Tick` 用的也是它们）。
        /// 🔴 **灭自证**：旧的「并行」实现给出的会是 `0.10 / 0.35 / 0.50 / 2.50 / 2.90`；
        /// 现在是 `0.10 / 0.35 / 0.75 / 1.15 / 3.15 / 3.55` ⇒ 谁把时间轴改回并行，断言立刻红。</summary>
        public static float[] TimelineBreakpoints() { return new[] { 0f, IntervalA, T1, T2, T3, T4, T5 }; }

        // ============================================================ 追加区 · 2026-10-28（B93_D4 · 账 `日常/奖励` 158 第 ③ 段）
        /// <summary>原版 `SetNotificationData` 第 ③ 段（`EventBadge__SetNotificationData.c:44-70`）：
        /// `context.Data is RewardInfo` ⇒ `drawerHolder.gameObject.SetActive(true)` +
        /// `drawer = ItemDrawer.Draw(drawerHolder, item, 1, 10)`，并把那个抽屉记进 `+0x50`（下一轮 `Destroy` 它）。
        /// <para>那个 `item` = `RewardInfo` 的虚槽 `+0x208` getter（同一份 `.c` 里
        /// `(**(code **)(*plVar1 + 0x208))(plVar1, *(plVar1 + 0x210))`）⇒ 本工程等价物 = 按 `RewardInfo.Id` 建 `ItemSpec`。</para>
        /// <para>⚠️ 第 4 实参 `10` = <see cref="DrawerOverride.Icon"/>（`.c` 里那个裸整数 `10`）。</para></summary>
        public void ApplyRewardDrawer(RewardInfo reward)
        {
            if (reward == null) return;
            if (drawerHolder == null)
            {
                WarnOnce("[EventBadge] SetNotificationData 第 ③ 段：`drawerHolder` 没绑（prefab 几何不在离线包里）⇒ 不建抽屉");
                return;
            }
            drawerHolder.gameObject.SetActive(true);                       // 原版这一句在 Draw 之前
            var spec = ItemDrawer.Spec(reward.Id, null, null);
            var res = ItemDrawer.Draw(drawerHolder, DrawerBox, spec, 1,
                                      DrawerOverride.Icon,
                                      ItemDrawerStyle.Default(ToastNotifications.QHit + 1, ToastNotifications.QHit + 2, ToastNotifications.QHit + 3));
            drawer = res.Node != null ? (object)res.Node.gameObject : null;  // 原版存 `ItemDrawer`；我们存它建出来的那个根
            if (res.Node == null)
                WarnOnce("[EventBadge] 第 ③ 段：`ItemDrawer.Draw` 没画出抽屉（id=`" + reward.Id + "`）—— 见它自己的告警");
        }

        /// <summary>抽屉框（原版在 `EventBadge` prefab 里，那份 prefab 不在离线包里 ⇒ 尺寸由宿主给）。
        /// 🔶 自己设计（非原版）：判据为空（prefab 缺席）· 76×76 是「小徽记档」的通行尺寸 ·
        /// 调度台 2026-10-28 授权（B93_D4 报告）。</summary>
        public PxRect DrawerBox = new PxRect(0f, 0f, 76f, 76f);
    }
}
