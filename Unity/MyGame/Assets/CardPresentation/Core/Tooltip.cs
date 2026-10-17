// Tooltip.cs — 悬停信息层（原版 `EverguildTooltipManager` + `EverguildTooltipItem`）
//
// 原版怎么做（**逐条来自全量反编译**，出处 `d:/2/tools/decomp_full/`）：
//   · 面板是**运行时 instantiate** 的（场景里 `TooltipManager` 只有一个空节点、下面没有子节点）
//   · 显示时机：触发器 `OnPointerEnter` **立刻**显示、`OnPointerExit` **立刻**隐藏（**没有延迟**）
//   · 位置 = **触发器自己的 `transform.position`**（**不跟随鼠标**）+ `defaultOffset` + `data.Offset`
//   · `rt.pivot = GetPivotPosition(TooltipAnchor)`（9 项表，未知值回落 (0.5,0.5) 并打 warning）
//   · 动画只有 **alpha 淡入 0.3s / 淡出 0.3s**（`ANIMATION_TIME = 0.3f`，DOTween 默认 OutQuad），
//     **没有缩放、没有位移**
//   · **没有屏幕边缘翻面/夹取**（全量反编译里零处用到 `Screen.width/height/safeArea`）
//
// 面板本体（**本地就有**，2026-09-20 查到）：
//   `assets_full/bundle_duplicateassetisolation_assets_all/GameObject/BasicToolTip.json`
//   （脚本类 `EverguildTooltipItem`，同名还有 4 个变体：`Battle Tooltip Variant` / `Trait Tooltip` /
//     `Updated Tooltip Variant` / `… - ranked`；子物体 `Background`/`Title`/`Text (TMP)`/`Content`/`Line`/`Mask`）
//   · 底图 = **`Smooth background square`**（32×32、**`m_Border=(12,2,12,12)`** ⇒ 九宫格）
//   · 正文 TMP 实测：**字号 28**（自动缩到 10）、**纯白**、HAlign=2(Center)；
//     **基础 tooltip 只有正文、没有标题**（带标题的是 `EverguildTooltipWithTitle` / Trait 版）
//   ⚠️ **`Tips/HealthTip` 那串不是预制体名、是 I2 本地化 key**（在触发器的 `text` 字段里）；
//      `tooltipPrefab` 字段**全部指向同一个** tooltip 预制体（`assets_full/…/MonoBehaviour_-4253307515847882232.json:17`）。
//
// 🔴 **我们查不到的（如实标，别当原版）**：
//   ① **文案**：原版走本地化 term（`Tips/MeleeAttackTip` 这类），而 **I2 的词条表在远端 CCD**，
//      本地只有 key、**一个 value 都没有**（全量扫过：`I2Languages` 零命中、`assets_full` 的
//      `*.csv/*.tsv` 零命中）。⇒ 见 `TipText`，**那是我们照规则书写的**，不是原版文案。
//      🔴 **2026-10-18（第十二轮 · W6）补判据（结论不变，把「查过哪几处」写全）**：
//      `Tips/*` 这一族的 key **有两种载体**，两边都扫过：
//        · **触发器字段里的普通字符串**（`EverguildTooltipItem` 的 `text` 字段，见上面那行）
//          —— ⛔ **扫 `Localize.mTerm` 对它是无效否定**（`资料/已知的坑.md` #20 的第四种载体）；
//        · **二进制里的字面量** —— `d:/2/tools/il2cpp_out/stringliteral.json`
//          （`RVA = 地址 − 0x180000000`）：`Battle/Tips/` 前缀实测 **24 条**
//          （`AttackBlock` / `AttackBlockFlying` / `AttackBlockStealth` / `AttackGiantKiller` /
//           `CantAttackCoward` / `CantAttack{Destroyer,Flank,Tainted}` / `CantAttackWithZero{Melee,Ranged}Attack` /
//           `DamageFatigue` / `DamageFatigueEnemy` / `DragToTarget` /
//           `GoFirst` / `HandFull` / `InvalidTarget` / `MuteEnemyChat` / `NoTargetAvailable` /
//           `NotEnoughMana` / `NotEnoughRoom` / `NotYourTurn` / `PendingBerzerk` / `PleaseWait` /
//           `UnitNotReady`），**一条 value 都没有**（同①）。
//      ⇒ **`TipText` 仍是我们写的**（判据：那些 key 本地只有名字）。
//   ② 面板的**圆角/内边距/九宫格拉伸目标尺寸**：prefab 的 RectTransform 是 UI 布局算的，
//      我们这套是世界空间 quad ⇒ **内边距与最小宽高是我们挑的**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>悬停信息层。全局一个，按需建。</summary>
    public class Tooltip : MonoBehaviour
    {
        static Tooltip _inst;
        /// <summary>当前显示的那条（自检用）。没显示时是 null。</summary>
        public static string ShownBody { get; private set; }
        /// <summary>正在显示吗（**淡出期间也算显示**，和原版一致）。</summary>
        public static bool Visible { get { return _inst != null && _inst._target > 0f; } }
        public static int ShowCount { get; private set; }      // 自检用：来过几条
        /// <summary>🆕 2026-10-15（账 `A2088`）：**真正重排版了几次**（= `Layout` 跑了几次 =
        /// 九宫格重建了几次）。与 <see cref="ShowCount"/> 分开正是为了能断「每帧重建」那个缺陷：
        /// 同一串文案连调 N 次 `Show` ⇒ `ShowCount` 涨 N、**本计数只涨 1**。</summary>
        public static int LayoutCount { get; private set; }
        /// <summary>面板中心的世界坐标（自检用来验 pivot 表）。没显示时是 zero。</summary>
        public static Vector3 PanelCenter
        {
            get { return (_inst != null && _inst._bgRoot != null) ? _inst._bgRoot.transform.localPosition : Vector3.zero; }
        }
        /// <summary>面板尺寸（px，1920 设计空间）—— 自检用。</summary>
        public static Vector2 PanelSizePx
        {
            get { return _inst != null ? new Vector2(_inst._w, _inst._h) : Vector2.zero; }
        }

        /// <summary>渲染队列：**全壳最高一档**（压在 HUD / 卡面 / 卡组编辑 / 每一扇窗与弹窗之上）。
        ///
        /// 🔴 **2026-10-04（A74①）搬过一次：原来是 3199 / 3200 / 3201**，与社交窗联盟页那一档
        ///    （`SocialWindow.QPageBase = 3200` ⇒ 页内容占 **3200–3209**）**正面重叠** ——
        ///    哪天社交窗里出现 tooltip（它的 `AllianceMemberTab` 里已经有 `&lt;link>` 类悬停件的位置），
        ///    提示面板会被**压在页面内容下面**（同档还更糟：`ImageQuad` 的世界 z 恒 0，谁盖谁退化成枚举顺序）。
        ///    ⇒ 本仓硬规矩「层带不许重叠」⇒ 必须搬开一处。**搬的是 tooltip 这一处**（不是 `SocialWindow`）：
        ///    ① 社交页那一档被**几十个 `Q` 常量 + 一百多条自检期望值**钉着（`SocialPage.Q` / 各 `SocialView.QOff`
        ///       / 奖杯格那个「借 3204」的特例），动它 = 大面积回归；tooltip 这一处是**三个私有常量**；
        ///    ② tooltip 的**契约**就是「必须在它解释的那个件之上」（它不属于任何一扇窗）⇒ 它本来就该在
        ///       最上面一档，而不是嵌在某个窗的层带中间。
        ///
        /// **选 3605 起的判据（为什么不是 3210 / 3309 / 3530）**：
        ///   · `3210–3299` 归**奖杯格那一带**（`AllianceMemberTab.QCellBase`，见那边的「按格号错开」）⇒ 不能用；
        ///   · `3300–3308` 聊天窗 · `3310–3326` `TrophyInfoPopup`（⚠️ 2026-10-07 更正：原写 3325；A95 给 `Next Tier` 让位后带子到 3326）· `3400–3405` 挑战弹窗 ·
        ///     `3450–3458` 战斗日志 · `3500–3520` 排行榜 —— 都是**窗**，tooltip 得压在它们之上；
        ///   · 🔴 **2026-10-11（A307 现读订正，铁律 5）**：这一条原写「`3600–3604` 是**主菜单顶栏**
        ///     （用户 2026-09-28 拍板『顶栏压住窗口』）⇒ tooltip 只能排在它**之上**」——
        ///     **号码与语义双过期**：同日的 A283 已把顶栏整条降到 **`2994–2998`**，且方向反过来
        ///     （用户 2026-10-11 裁定「照原版」⇒ **顶栏在每一扇窗之下**；判据 → `MainMenuRuntime.QBarPanel` 那段）。
        ///     🔴 **2026-10-18 订正（铁律 5）**：上面那个号段**又过期了一次** —— 2026-10-17 F5 把下沿从 `2994`
        ///     放宽到 `2986` ⇒ **现带子 = `2986–2998`**（**唯一出处 = `Shell/TopBar.cs` 那张表**，
        ///     `MainMenuRuntime.QBarPanel` 已是 `2986`）；⚠️ 本句的**方向**（顶栏在每一扇窗之下）没变、仍成立。
        ///     ⇒ 顶栏**不再是 tooltip 要避开的对象**（它连窗都比它低），取 3605 起的理由只剩上一行那条
        ///     「压在所有窗之上」。⚠️ **功能无影响**：`QTip = 3606` 仍是全壳最高（`3605–3607` 与 `2986–2998` 不重叠
        ///     —— ⚠️ **2026-10-18 订正**：这里原写 `2994–2998`，同 F5 放宽了下沿；区间更短、结论不变）。
        /// 🔴 **「tooltip 在全壳最高」不是我们挑的，是原版的兄弟序**（判据 = 原始 RT 的 `m_Children`，
        ///   与「**弹窗**压住顶栏」那条用的是**同一把尺子** —— 铁律 4：多实例先解父链/兄弟序）：
        ///    `bundle_scenes_scenes_mainmenuwarpforge` 场景里 `Safe area Only Horizontal` 的子件序
        ///    （`python d:/4/Unity/工具/menu_dump.py bundle_scenes_scenes_mainmenuwarpforge
        ///      "Safe area Only Horizontal" --md --depth 1` 实读）：
        ///    `Navigation Panel · ChatPreview · 1 - Below Upper Bar Holder · **Upper bar** · Borders(act F) ·
        ///     2 - Canvas Holder Above upper bar · **3 - PopUp Holder** · LoadingMenu(act F) ·
        ///     UI Error Message Controller · **TooltipManager** · Toast Notification Controller · Loading Controller`
        ///    ⇒ `TooltipManager` 排在 `Upper bar` 与 `3 - PopUp Holder` **之后** = **画在它们之上** ✓；
        ///    而 `TooltipManager` 自己的矩形是 `-1010.35,1055.00→-910.35,1155.00`（屏幕外那 100×100 的挂点，
        ///    面板是运行期 instantiate 的 —— 同本文件头那条）。
        ///    ⚠️ **2026-10-11（A307 现读订正，铁律 5）**：这一段原写「这与 `MainMenuRuntime` 那条
        ///      『**实拍里顶栏压住选卡组弹窗**』**不冲突**」—— **那句已过期**：A283 当天按用户裁定
        ///      把那条口径**整个反过来**（现在是**窗压住顶栏**）⇒ 兄弟序这条判据与它**方向一致**，
        ///      不但不冲突，两者现在用的是**同一把尺子**（判据 → `MainMenuRuntime.QBarPanel` 那段）。
        ///   ⇒ 取 **3605 / 3606 / 3607**：全壳最高，且与任何已有层带都不重叠。
        ///   ⚠️ 战斗场景那一侧用的是 4000（`BattleLogPanel.OverlayQ`），与本层**不同场景**、不会共存
        ///     （相对次序也没变：原来 3200 就已在战斗 HUD 3000–3010 之上、在 4000 之下）。</summary>
        const int QTipShadow = 3605, QTip = 3606, QTipText = 3607;
        // ⚠️ **`QTipShadow` 今天一个读者都没有**（面板**没有投影那一层** —— 原版 `EverguildTooltipItem`
        //    的那个 `Content`/`Line` 也不是投影）。留着只是**把号占住**（免得以后加投影时随手挑一个
        //    撞上 `QTip`）；⛔ **别把它当成「已经实现的层」**（同 `项目任务.md` §三 A58-R3 那条：
        //    一个「注释说自检会断它、实际全工程零读者」的常量 = 空口声称）。
        /// <summary>原版 `EverguildTooltipItem.ANIMATION_TIME = 0.3f`（桩 `.cs:10`，.rdata 实测 0.3f）</summary>
        const float FadeTime = 0.3f;
        /// <summary>1 世界单位 = 108 px（和 `LayoutSpace` 同一口径）</summary>
        const float PxPerUnit = 1080f / 10f;
        /// <summary>正文的字号：原版 TMP 是 **28 pt**，跨工程只能对**渲染高度**
        /// （28 pt ⇒ 大写高 0.72 em ≈ 20.2 px ≈ 0.1867 世界单位）。同 `OvertimeSplashText` 那条教训：
        /// **原版的 `m_fontSize` 不能照抄**，它按原版画布算。</summary>
        const float BodyCapWorld = 0.72f * 28f / PxPerUnit;

        // 面板内边距与下限 —— ⚠️ **我们挑的**（原版是 UI 布局算的，见文件头 ②）
        const float PadX = 20f, PadY = 14f, MinW = 120f, MaxW = 640f, LineH = 30f;

        // 底图（原版资产，九宫格）
        const string BgSprite = "Smooth_background_square";
        const float BgTexW = 32f, BgTexH = 32f;
        static readonly Vector4 BgBorder = new Vector4(12f, 2f, 12f, 12f);   // m_Border = (12,2,12,12)

        GameObject _bgRoot;
        /// <summary>底图的件。世界档 = 九块 quad；uGUI 档 = **一张 `Image (Sliced)`**
        /// （🔴 2026-10-16 · `T09` 批 1：类型从 `ImageQuad` 改成 `HudPiece` —— `ApplyAlpha` 只用 `SetTint`，两档都有）。</summary>
        readonly List<HudPiece> _bgParts = new List<HudPiece>();
        Label _body;
        float _alpha, _target;
        Vector3 _pos;                       // 世界坐标（已含 offset）
        float _w = MinW, _h = 60f;

        /// <summary>🔴 **2026-10-16（`T09` 批 1）：本场景 tooltip 的宿主画布**（由**建 UI 的那一方**登记一次）。
        /// <para>`null`（出厂档）= **世界空间**（旧行为）—— **卡组编辑那边就是这一档**（它的 UI 不在战斗画布上）。
        /// 非 null = **uGUI 档**：件建在该画布下、坐标 = **设计 px**（原版 `TooltipManager` 是 `Blocking` 层，
        /// 判据 → `普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一）。</para>
        /// <para>🔑 **跨场景安全**：场景卸载把那份 GameObject 销毁后，Unity 的 `==` 会把它当 `null`
        /// ⇒ **自动退回世界档**，⛔ 不需要谁去清（本件是**静态单例**、战斗与卡组编辑共用）。</para></summary>
        static RectTransform _host;
        /// <summary>登记本场景的 tooltip 宿主（**建 UI 时调一次**，必须早于任何 `Show`）。传 `null` = 明确要世界空间档。</summary>
        public static void UseHost(RectTransform host) { _host = host; }
        /// <summary>当前是不是 uGUI 档（诊断 / 断言 / 自检用）。</summary>
        public static bool UguiMode { get { return _host != null; } }
        /// <summary>面板中心的**世界坐标**（⛔ 不管哪一档都返回**世界单位**）—— 自检与别的场景读它。
        /// <para>⚠️ **与 `PanelCenter` 的区别**：那个返回的是**本档的局部坐标**
        /// （世界档 = 世界单位 · uGUI 档 = **设计 px**）⇒ 拿它直接跟一个世界点比会**差 108 倍**。
        /// 本属性就是为「跨档比较」准备的（🔴 2026-10-16 · `T09` 批 1 加）。</para></summary>
        public static Vector3 PanelCenterWorld
        {
            get
            {
                if (_inst == null || _inst._bgRoot == null) return Vector3.zero;
                Vector3 l = _inst._bgRoot.transform.localPosition;
                return UguiMode ? new Vector3(l.x / PxPerUnit, l.y / PxPerUnit, 0f) : l;
            }
        }
        /// <summary>本档里「1 个单位 = 多少 px」：世界档 = 108（`PxPerUnit`）· uGUI 档 = **1**（canvas 单位就是设计 px）。</summary>
        static float UnitPx { get { return UguiMode ? 1f : PxPerUnit; } }
        /// <summary>正文大写高（**本档的单位**）：世界档 = 世界单位 · uGUI 档 = 设计 px。</summary>
        static float BodyCap { get { return UguiMode ? BodyCapWorld * PxPerUnit : BodyCapWorld; } }
        /// <summary>🆕 2026-10-15（账 `A2088`）：**上一次真正排版用的是哪串文案**。
        ///
        /// 为什么要有它：原版 tooltip 是**事件驱动**的 —— `OnPointerEnter` **显示一次**、
        /// `OnPointerExit` **隐藏一次**（文件头第 5 行那条），面板几何一辈子只建一遍。
        /// 我们这套是**每帧轮询**（`BattleDriver.TickTooltipAt`）⇒ `Show` 会**连续每帧**被调，
        /// 而当时的 `Show` **无条件**走 `Layout → BuildBg` ⇒ **悬停期间每帧销毁重建整块九宫格**
        /// （9 个 `new GameObject` + 9 个 `new Material(Shader.Find(...))` + 一次全子树
        /// `GetComponentsInChildren`）。用户 2026-10-15 报的「对战卡顿」里，这一处是最大的一笔。
        ///
        /// ⛔ **判据不是「省点开销」，是「回到原版那件事」**：内容变了才重建
        /// （= 原版的 enter/exit 语义），内容没变只挪位置（`Pivot`，它本来就每次都跑）。
        /// ⚠️ **`null` 初值**：第一次 `Show` 一定会排版（`Layout` 里那个 `ForceMeshUpdate`
        /// 依赖「对象已激活」，见 `Show` 的注释）。</summary>
        string _laidBody;

        static Tooltip Ensure()
        {
            if (_inst != null) return _inst;
            // uGUI 档：根节点**自带 `RectTransform`**、挂在宿主画布下、锚在画布中心
            //   （⛔ 别让它的 `localPosition` 有值 —— 子件的 px 偏移全是「相对本根的中心」算的）
            var go = UguiMode ? new GameObject("TooltipLayer", typeof(RectTransform))
                              : new GameObject("TooltipLayer");
            _inst = go.AddComponent<Tooltip>();
            if (UguiMode)
            {
                var rt = (RectTransform)go.transform;
                rt.SetParent(_host, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            _inst.Build();
            return _inst;
        }

        void Build()
        {
            // 🔴 **两档各建各的**（⛔ 不许两边都建 —— 同 `HudPiece` / `Label` 那条铁律）：
            //   世界档 = `Label.Create`（世界单位）· uGUI 档 = `Label.CreateUgui`（**设计 px**）。
            //   ⚠️ 字号也要跟着换档：`BodyCap` 那条属性已经把 108 那一步算进去了。
            _body = UguiMode
                  ? Label.CreateUgui(transform, "", Vector3.zero, 2, new Color(1f, 1f, 1f, 0f),
                                     new Vector2(0.5f, 0.5f), "tip_body")
                  : Label.Create(transform, "", Vector3.zero, 2, new Color(1f, 1f, 1f, 0f),
                                 new Vector2(0.5f, 0.5f), "tip_body");
            _body?.SetRenderQueue(QTipText);   // uGUI 档是**有注释的空操作**（层级归画布管）
            _body?.SetCapHeight(BodyCap);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (_inst != this) return;
            if (Mathf.Approximately(_alpha, _target))
            {
                if (_target <= 0f && gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            // `DOTween` 的 `DOFade` 默认 OutQuad；批处理下没有补间驱动 ⇒ 自己按时间推
            _alpha = Mathf.MoveTowards(_alpha, _target, Time.unscaledDeltaTime / FadeTime);
            ApplyAlpha(_alpha);
        }

        void ApplyAlpha(float a)
        {
            foreach (var q in _bgParts) if (q != null) q.SetTint(new Color(0f, 0f, 0f, 0.9f * a));
            if (_body != null) _body.SetColor(new Color(1f, 1f, 1f, a));
        }

        /// <summary>显示一条。`worldPos` 是**触发器自己的位置**（不是鼠标），`anchor` = 原版 `AnchorPositions`。
        /// `anchor` 表：0/5 居中 · 10 左中 · 15 右中 · 17 上中 · 20 左上 · 25 右上 · 27 下中 · 30 左下 · 55 右下。
        ///
        /// 🔴 **2026-10-15（账 `A2088`）：文案没变就<b>不重排版</b>。**
        /// 本方法是**每帧**被调的（`BattleDriver.TickTooltipAt`），而原版那条链是
        /// **`OnPointerEnter` 显示一次 / `OnPointerExit` 隐藏一次**（文件头第 5 行）——
        /// 面板几何**一辈子只建一遍**。原来这里无条件走 `Layout()` → `BuildBg()`
        /// ⇒ **悬停期间每帧销毁重建整块九宫格**（`Destroy` + `MenuDraw.Nine` + 全子树
        /// `GetComponentsInChildren` + 9 个 `new Material(Shader.Find("Sprites/Default"))`）。
        /// ⇒ 这一道「同一串文案就跳过」正是把原版那个 enter/exit 语义**补回来**，
        /// ⛔ 不是「为省开销随手加缓存」。判据 → `Tooltip.Layout` / `BuildBg` 两处的行号。
        /// ⚠️ **`ShownBody` / `ShowCount` 照旧每次都更新**（它们的语义是「来过几条」，
        ///    `BattleScene` 的自检拿它数调用次数）—— ⛔ 别一起跳过。</summary>
        public static void Show(string body, Vector3 worldPos, int anchor = 0, Vector3 offset = default(Vector3))
        {
            if (string.IsNullOrEmpty(body)) { Hide(); return; }
            var t = Ensure();
            t._pos = worldPos + offset;
            ShownBody = body;
            ShowCount++;
            // 🔴 **顺序不能反**：`Layout()` 里要 `ForceMeshUpdate()` 量文字尺寸，
            //    而 TMP **在对象没激活时量不出尺寸**（实测：`tmp=True` 但 `textBounds` 是垃圾，
            //    `tmpW` 变成 4.29e9 ⇒ 面板 640px 宽、字一个都看不见）。
            //    所以：**先激活、再排版**。
            t.gameObject.SetActive(true);
            if (t._bgRoot == null                              // 底图没建起来（取不到图）⇒ 每次都重试，别把失败状态钉死
                || !string.Equals(t._laidBody, body, System.StringComparison.Ordinal))
            {
                t.Layout(body);
                t._laidBody = body;
            }
            t.Pivot(anchor);
            t._target = 1f;
        }

        public static void Hide()
        {
            if (_inst == null) return;
            _inst._target = 0f;
            ShownBody = null;
        }

        /// <summary>自检用：立刻结束淡入淡出（批处理里没有帧循环推 `Update`）。</summary>
        public static void FinishFade()
        {
            if (_inst == null) return;
            _inst._alpha = _inst._target;
            _inst.ApplyAlpha(_inst._alpha);
            if (_inst._target <= 0f) _inst.gameObject.SetActive(false);
        }

        /// <summary>自检用：uGUI 档底图在 `TooltipLayer` 里的**兄弟序**（世界档没有兄弟序语义 ⇒ `−1`）。
        /// 判据 = 它必须**小于** <see cref="BodySiblingIndexForTest"/>（先画 = 在字底下）；
        /// 见 `BuildBg` 里 2026-10-17 那条修复。</summary>
        public static int BgSiblingIndexForTest
        {
            get
            {
                return (UguiMode && _inst != null && _inst._bgRoot != null)
                     ? _inst._bgRoot.transform.GetSiblingIndex() : -1;
            }
        }

        /// <summary>自检用：uGUI 档文字的**兄弟序**（世界档 ⇒ `−1`）。</summary>
        public static int BodySiblingIndexForTest
        {
            get
            {
                return (UguiMode && _inst != null && _inst._body != null)
                     ? _inst._body.transform.GetSiblingIndex() : -1;
            }
        }

        /// <summary>自检用：把面板与文字的当前状态摊开（排「面板在、字不在」这种问题）。</summary>
        public static string DebugDump()
        {
            if (_inst == null) return "(没有实例)";
            var l = _inst._body;
            if (l == null) return "(没有 body)";
            int q = -1;
            if (l.GetComponent<TMPro.TMP_Text>() != null)
                q = l.GetComponent<TMPro.TMP_Text>().fontMaterial != null
                    ? l.GetComponent<TMPro.TMP_Text>().fontMaterial.renderQueue : -2;
            return $"text=\"{l.Text}\" len={l.Text?.Length ?? -1} W={l.WorldW:F3} H={l.WorldH:F3} "
                 + $"cap={l.CapHeightWorld:F4} size={l.transform.localPosition} parentActive={l.transform.parent.gameObject.activeSelf} "
                 + $"selfActive={l.gameObject.activeSelf} queue={q} alpha={_inst._alpha:F2} target={_inst._target:F2} "
                 + $"bgParts={_inst._bgParts.Count} bgPos={PanelCenter} panelPx={PanelSizePx} | {l.DumpSizes()}";
        }

        /// <summary>原版 `EverguildTooltipItem.GetPivotPosition`（9 项表 + 未知值回落）。</summary>
        void Pivot(int anchor)
        {
            Vector2 p;
            switch (anchor)
            {
                case 0: case 5:  p = new Vector2(0.5f, 0.5f); break;   // None / MiddleCenter
                case 10:         p = new Vector2(0f, 0.5f);   break;   // MiddleLeft
                case 15:         p = new Vector2(1f, 0.5f);   break;   // MiddleRight
                case 17:         p = new Vector2(0.5f, 1f);   break;   // TopCenter
                case 20:         p = new Vector2(0f, 1f);     break;   // TopLeft
                case 25:         p = new Vector2(1f, 1f);     break;   // TopRight
                case 27:         p = new Vector2(0.5f, 0f);   break;   // BottomCenter
                case 30:         p = new Vector2(0f, 0f);     break;   // BottomLeft
                case 55:         p = new Vector2(1f, 0f);     break;   // BottomRight
                default:
                    Debug.LogWarning($"[Tooltip] anchor {anchor} not implemented —— 回落 (0.5, 0.5)");
                    p = new Vector2(0.5f, 0.5f); break;
            }
            // 锚点 = pivot 那个点落在 `_pos` 上 ⇒ 面板中心 = pos + (0.5 − pivot) × 尺寸
            // 🔴 **2026-10-16（`T09`）：两档的基准不同** ——
            //   世界档：基准 = `_pos`（世界），尺寸换算 ÷108；uGUI 档：基准 = `_pos × 108`（设计 px），尺寸 ÷1。
            //   ⇒ 两档写出来的 `localPosition` 各自落在**自己那套坐标系**里（子件锚点都在中心，见 `Ensure`/`BuildBg`）。
            float u = UnitPx;
            float bx = UguiMode ? _pos.x * PxPerUnit : _pos.x;
            float by = UguiMode ? _pos.y * PxPerUnit : _pos.y;
            float cx = bx + (0.5f - p.x) * _w / u;
            float cy = by + (0.5f - p.y) * _h / u;
            if (_bgRoot != null) _bgRoot.transform.localPosition = new Vector3(cx, cy, 0f);
            if (_body != null) _body.transform.localPosition = new Vector3(cx, cy, 0f);
        }

        void Layout(string body)
        {
            if (_body == null) return;
            LayoutCount++;
            // ⚠️ 我们的 `Label` **不自动换行**（TMP 只是一个 text 组件，宽度是算出来的）
            //    ⇒ 太长会顶到 `MaxW` 上限、字跑到面板外面去。这里按**字符数**手动折行
            //    （CJK 都是等宽的，按字宽算比按像素稳）。**这是我们的做法**，原版靠 UI 布局。
            string wrapped = Wrap(body, MaxCharsPerLine);
            _body.SetText(wrapped);
            // ⚠️ `WorldW` 是「本档的单位」（世界档 = 世界单位 · uGUI 档 = 设计 px）⇒ 用 `UnitPx` 折
            float bw = _body.WorldW * UnitPx;
            _w = Mathf.Clamp(bw + PadX * 2f, MinW, MaxW);
            int lines = Mathf.Max(1, wrapped.Split('\n').Length);
            _h = lines * LineH + PadY * 2f;
            BuildBg();
        }

        /// <summary>按**显示宽度**折行：CJK/全角算 1 个宽单位，ASCII 算 0.5。</summary>
        const float MaxCharsPerLine = 22f;

        static string Wrap(string body, float maxUnits)
        {
            if (string.IsNullOrEmpty(body)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var raw in body.Split('\n'))
            {
                float line = 0f;
                int i = 0;
                while (i < raw.Length)
                {
                    // 🔴 **标签整段当「零宽原子」搬过去**（`<sprite name="…">` / `<nobr>` / `<link=…>` / `<b>` …）。
                    //    2026-09-21 修：原来是**逐字符**折行 ⇒ 行满时正好落在标签中间的话，
                    //    会在 `<sprite name="codex">` **里面**插一个 `\n` 把它**掐成两半**
                    //    （TMP 认不出半个标签 ⇒ 卡面上把标签原样印出来）。
                    //    标签本身**不计宽度** —— 它不占位，占位的是它渲染出来的那张图，那个由 TMP 自己量。
                    if (raw[i] == '<')
                    {
                        int gt = raw.IndexOf('>', i);
                        if (gt < 0) { sb.Append(raw, i, raw.Length - i); break; }   // 没有闭合 ⇒ 原样搬完
                        sb.Append(raw, i, gt - i + 1);
                        i = gt + 1;
                        continue;
                    }
                    int lt = raw.IndexOf('<', i);
                    int stop = lt < 0 ? raw.Length : lt;
                    for (; i < stop; i++)
                    {
                        char c = raw[i];
                        float u = c < 0x2E80 ? 0.5f : 1f;
                        if (line + u > maxUnits) { sb.Append('\n'); line = 0f; }
                        sb.Append(c); line += u;
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>自检用：走一遍 `Layout` 里那个折行（不建面板）。</summary>
        public static string WrapForTest(string body) { return Wrap(body, MaxCharsPerLine); }

        /// <summary>九宫格底**尺寸一变就得重建**（九块的位置依赖目标尺寸）。
        /// 原版是一张 `Image (Sliced)`，Unity 自己重排；我们这套是世界空间 quad。</summary>
        void BuildBg()
        {
            if (_bgRoot != null) DestroySafe(_bgRoot);
            _bgParts.Clear();
            var tex = CardArt.DeckUi(BgSprite);
            if (tex == null) { Debug.LogWarning($"[Tooltip] 面板底图 `{BgSprite}` 取不到，只显示文字"); return; }
            // 🔴🔴 **2026-10-16（`T09` 批 1）：uGUI 档 = 一张 `Image (Sliced)`** ——
            //   原版那颗面板本来就是 `Image (Sliced)`（九宫格靠 `m_Border` + `m_PixelsPerUnitMultiplier`），
            //   所以 uGUI 档**比九块 quad 更贴原版**，而且层级/缩放全归画布管。
            //   ⚠️ 锚点/枢轴全在中心（`MakeSlicedPiece` 里设的）⇒ 紧跟的 `Pivot()` 写 `localPosition` 才对得上。
            if (UguiMode)
            {
                var piece = HudCanvas.MakeSlicedPiece(transform, tex, "tip_bg", _w, _h, BgBorder,
                                                      new Color(0f, 0f, 0f, 0.9f));
                if (piece == null) { Debug.LogWarning("[Tooltip] uGUI 档的九宫格底图没建起来"); return; }
                // 🔴🔴 **2026-10-17 修（活缺陷）：底图必须登记兄弟序** —— `MakeSlicedPiece` **既
                //   不写 z、也不登记**（见 `ErrorMessageBanner` 同一处注释），而 `tip_body` 是
                //   `Build()` 里**先**建的（uGUI 树里排在 0）⇒ 不登记的话底图落在**文字之后**、
                //   把整块字盖死（`UguiMode = _host != null`，战斗侧这个档是活的）。
                // ⚠️ 而且换文案时本方法**重建**底图 ⇒ 没有这事先登记，新件永远排到末尾、**不会自愈**；
                //   重复重建还会让 `Group` 的 z 账里堆死件（已在 `HudCanvas.Group.Prune` 修掉）。
                HudCanvas.Add(piece.Rect, 0f);
                _bgRoot = piece.gameObject;
                _bgParts.Add(piece);
                ApplyAlpha(_alpha);
                return;
            }
            // 🔴 **2026-10-04（A50③）：收口到公共件 `MenuDraw.Nine`** —— 原来直调
            //    `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`）。三样与旧代码**逐项等价**：
            //    ① **尺寸** = `_w × _h`（px）—— 助手内部按 `LayoutSpace.Px` 折世界尺寸，
            //       与本件那个 `PxPerUnit = 108` 是**同一个换算**；
            //    ② **落位** = 紧接着那行 `localPosition = Vector3.zero`（旧代码传的 center 就是 `Vector3.zero`）；
            //       矩形中心因此只是**占位**（九块是按根节点的局部原点铺的）——
            //       ⚠️ 就算漏了那行也不由矩形决定：`Layout()` 建完之后 `Show()` 还会调 `Pivot()`，
            //       那里按 `_pos + (0.5−pivot)×尺寸` **重摆一次**（本层根 `TooltipLayer` 无父节点、在世界原点）；
            //    ③ **渲染队列 = QTip**（旧代码是在下面那个循环里逐块 `SetRenderQueue(QTip)` 的，
            //       现在助手一次设完 ⇒ 循环只留「收 `_bgParts`」这一件事）。
            _bgRoot = MenuDraw.Nine(transform, tex, CenteredRectPx(_w, _h), BgBorder, BgTexW, BgTexH,
                                    QTip, name: "tip_bg");
            if (_bgRoot == null) { Debug.LogWarning($"[Tooltip] 面板底图 `{BgSprite}` 的九宫格没建起来"); return; }
            _bgRoot.transform.localPosition = Vector3.zero;
            foreach (var q in _bgRoot.GetComponentsInChildren<ImageQuad>())
            {
                q.SetRenderQueue(QTip);        // 同一个数（助手已设过一遍，这里保留只为收集 `_bgParts`）
                _bgParts.Add(HudPiece.Of(q));  // ⚠️ 2026-10-16：`_bgParts` 现在是 `List<HudPiece>`
            }
            ApplyAlpha(_alpha);
        }

        /// <summary>`MenuDraw.Nine` 要的**画布 px 矩形**。🔴 **只用到它的宽高** ——
        /// 九宫格那九块按**根节点的局部原点**铺（尺寸从 `−尺寸/2` 起算）⇒ 根节点摆在哪由调用方随后那行
        /// `localPosition`（以及之后的 `Pivot()`）定，这里的**中心值是故意的占位**（取画布中心，好读而已）。
        /// ⛔ 别改成「`_pos` 反推的画布 px」：那要绕一次 `ToPixel/FromPixel` 往返，
        ///    而非 16:9 下这两个不是逆运算 ⇒ 平白引入一份位置误差。</summary>
        static PxRect CenteredRectPx(float wPx, float hPx)
        {
            float cx = LayoutSpace.DesignPxW * 0.5f, cy = LayoutSpace.DesignPxH * 0.5f;
            return new PxRect(cx - wPx * 0.5f, cy - hPx * 0.5f, cx + wPx * 0.5f, cy + hPx * 0.5f);
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }

    /// <summary>
    /// tooltip 的**文案**。
    ///
    /// 🔴 **这是我们写的，不是原版文案** —— 原版走 I2 本地化 key（`Tips/MeleeAttackTip` 那一族），
    ///    而**词条表在远端 CCD**：本地只有 key、一个 value 都没有（`assets_full` 全量扫过：
    ///    `I2Languages` 零命中、`*.csv/*.tsv` 零命中、`LanguageSourceData` 的序列化字段零命中）。
    /// ⇒ 这里的每一句都**照规则书**写（**判据行号**留在内部正本 `资料/关键词图标/_规则书关键词表.md` 的
    ///    「出处行号」列）—— 🔴 **那个行号【不上屏】**：我们一度把它拼进 tooltip（数值那五条 +
    ///    关键词那条），**2026-10-12（`A1660`）已整条删掉**（原版只凑**标题 + 描述**两段、零出处节点）。
    /// **拿到原版词条表之后，把这张表整个换掉即可**（接口不变）。
    ///
    /// 🆕 **2026-10-08（第六会话 · `Core双语-第一步`）：键已收进 `Core/Loc.cs`** ——
    ///   下面那 12 条**一律走 `Loc.T(键)`**（键名、原版出处、逐条计数 → `Loc.cs` 那一块的头）。
    ///   **值仍然是我们写的**（同上一段：显示串在远端 I2 表）⇒ 两列都是自拟；
    ///   **ZH 列 = 改前这几句逐字** ⇒ **中文档零变化**，英文档从「悬停出来还是中文」变成印英文。
    ///   ⛔ **别在这里再留一份写死的文案**（两处写同一条规则 = 迟早不一致）。
    /// </summary>
    public static class TipText
    {
        /// <summary>卡面四个数值 + 费用。原版挂点 = `Health/Range Attack/Melee Attack/Cost Container`
        /// （`assets_full/bundle_staticgeneralassets_assets_all/GameObject/` 那几个）。
        /// 🔴 **键名 = 原版那颗 `EverguildTooltipTrigger` 的 `text` 字段**（逐条计数与「两个字段逐字吻合」
        /// 那条硬证据 → `Core/Loc.cs` 里那一块的头）。</summary>
        public static string Melee  { get { return Loc.T("Tips/MeleeAttackTip"); } }
        public static string Ranged { get { return Loc.T("Tips/RangedAttackTip"); } }
        /// <summary>⚠️ **原版没有护甲 tooltip**（`Armour Container` 无触发器）+ 我们这边**零消费点**
        /// ⇒ 这条的键 `Tips/ArmourTip` 是**自拟**的（留着只为同一族形状一致、不静默）。
        /// 判据 → `Core/Loc.cs` 那一块。</summary>
        public static string Armour { get { return Loc.T("Tips/ArmourTip"); } }
        public static string Health { get { return Loc.T("Tips/HealthTip"); } }
        public static string Cost   { get { return Loc.T("Tips/CostTip"); } }

        /// <summary>HUD 计数 —— **我方那五条**。键 = 原版 HUD 那几个计数节点上的 `EverguildTooltipTrigger.text`
        /// （13 个 arena 各一颗；逐条计数 → `Core/Loc.cs` 那一块）。
        /// ✅ **2026-10-09（`A1086②`）已照原版把敌我拆开**：原版**敌我各一条键**
        /// （`Tips/Hud/{Player,Opponent}…Count`，各 13 颗）⇒ **我方**图标取下面这五条、
        /// **对面**图标取 `Foe*` 那四条（消费点 = `Battle/Tooltip` 的调用方 `BattleDriver.TickTooltipAt`）。
        /// ⚠️ **`Skulls` 只有一条** —— 原版**没有** `Tips/Hud/OpponentSkulls`（双方共用同一颗里程碑计数）
        /// ⇒ ⛔ 别硬造一条对面键。</summary>
        public static string Energy      { get { return Loc.T("Tips/Hud/PlayerEnergyCount"); } }
        public static string Skulls      { get { return Loc.T("Tips/Hud/Skulls"); } }
        public static string QuestPoints { get { return Loc.T("Tips/Hud/PlayerQPCount"); } }
        public static string Faith       { get { return Loc.T("Tips/Hud/PlayerFaithCount"); } }
        public static string SpiritStone { get { return Loc.T("Tips/Hud/PlayerSpiritStoneCount"); } }

        /// <summary>HUD 计数 —— **对面那四条**（`A1086②`，2026-10-09 补；键名 = 原版各 13 颗）。
        /// ⚠️ **两列文案全自拟**（原版那两个触发器 `localize = 1`，显示串在**远端 I2 表**；
        /// 逐条 → `Core/Loc.cs` 那一块）⇒ ⛔ 别当成「原版这么说」。
        /// ⚠️ 没有「对面的 `Skulls`」这一条 —— 原版那条键只有一颗（双方共用），理由见上一段。</summary>
        public static string FoeEnergy      { get { return Loc.T("Tips/Hud/OpponentEnergyCount"); } }
        public static string FoeQuestPoints { get { return Loc.T("Tips/Hud/OpponentQPCount"); } }
        public static string FoeFaith       { get { return Loc.T("Tips/Hud/OpponentFaithCount"); } }
        public static string FoeSpiritStone { get { return Loc.T("Tips/Hud/OpponentSpiritStoneCount"); } }

        // ==================================================================
        //  🆕 2026-09-21：关键词（trait）的 tooltip —— 悬停卡面关键词段里那一枚图标时弹的
        //  ==================================================================
        //
        //  **原版怎么做**（全量反编译）：关键词段那一小段整段套 `<link=<DefinedTrait枚举名>>…</link>`
        //  （`GameStaticData__TraitNameToString.c:84-109`），`TextTooltipController` 每帧
        //  `TMP_TextUtilities.FindIntersectingLink` 命中、把 link id `TryParse` 成 `DefinedTrait`
        //  （`TextTooltipController__GetTraitTooltip.c:30,35-36`），面板是 `EverguildTraitTooltipItem`
        //  （比基础版多 **图标 + 标题**）。
        //
        //  **文案从哪来**：`工具/gen_trait_tips.py` 生成 `Resources/trait_tips.json`。
        //  **不手抄进 C#**（手抄一份迟早和源头对不上，正是本工程反复强调的「判据只写一处」）。
        //
        //  🔴 **2026-10-14（`A1803`）就地更正（铁律 5）**：这里原来写「文案 = 规则书那 61 条」——
        //     **今天不对了**。正文本源 = **官方 zh-CN 词条表的 `CardTraitDescription/<规范键>`**
        //     （56 条，`src:official`）；官方**确实没有**的那 5 个词（`ferocity` / `huntmark` /
        //     `oath` / `pack` / `sentry`）才退回《Warpforge Offline Rulebook》
        //     （`src:rulebook` —— 那是**粉丝实体版**、`Not official. Fan project.`，⛔ 别当原版）；
        //     `ability` 两边都没有（`src:none`，正文空、如实说明）。
        //     ⇒ **「这一份是我们照规则书写」这句只对那 5 条成立**，⛔ 别按旧口径理解整张表。
        //     判据逐条 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。
        //
        //  ⚠️ 原版那套走 **I2 词条表**（`TextTooltipController__GetTraitTooltip` → `GetTraitDesc`），
        //     那份表 2026-10-14 已经解开了 ⇒ 现在**就是照它取的**（不是「在远端拿不到」了）。
        //
        //  **图标**：`Badges.SpriteOf(key)` —— 与卡面/徽标**同一份判据**，别另写一张表。

        // 🔴 **2026-10-12（`A1660`）**：原来还有一个 `public string line`（规则书出处行号）——
        //   生成物 `trait_tips.json` **已不再输出这个字段**（`工具/gen_trait_tips.py` 同步改了）
        //   ⇒ 字段一起删掉（`JsonUtility` 对多余的 json key 不报错，但留着就是个**死字段**：
        //   没人读、却让人以为正文里还有出处）。判据 → `资料/普查产出_第二十二会话/DIAG_tooltip出处行.md`。
        //
        // 🔴 **2026-10-14（`A1803`）形状换了（v2）**：正文的来源从「规则书」换成
        //   **官方 zh-CN 词条表的 `CardTraitDescription/*`**（官方没有的那几个词才退回规则书）
        //   ⇒ ① 查表的键从「中文显示名」改成**小写规范键**（`key`）；
        //     ② 多了 `bodyEn`（官方那一列英文 —— 原来只有中文，英文档的 tooltip 正文也是中文）；
        //     ③ `src` = `official` / `rulebook` / `none`，运行时**出声**用（不静默）。
        //   判据与逐条表 → `资料/普查产出_第二十八会话/W4_官方文案族.md`；生成器文件头也写了一遍。
        [System.Serializable] class TraitTipRow
        {
            public string key; public string zh; public string en;
            public string bodyZh; public string bodyEn; public string src;
        }
        [System.Serializable] class TraitTipTable { public TraitTipRow[] entries; }

        static Dictionary<string, TraitTipRow> _tips;
        static readonly HashSet<string> _warnedNoEntry = new HashSet<string>();
        /// <summary>已经为哪些键报过「这一条正文退回规则书 / 干脆没有」（**按键去重**，理由同 `Loc._warnedMissing`）。</summary>
        static readonly HashSet<string> _warnedSrc = new HashSet<string>();

        static void LoadTips()
        {
            if (_tips != null) return;
            _tips = new Dictionary<string, TraitTipRow>();
            var ta = Resources.Load<TextAsset>("trait_tips");
            if (ta == null)
            {
                // **不静默**：认不出就明说怎么补
                Debug.LogWarning("[TipText] 找不到 `Resources/trait_tips.json` —— 关键词 tooltip 只有名字、没有解释。"
                                 + "跑 `工具/gen_trait_tips.py --write` 生成。");
                return;
            }
            var d = JsonUtility.FromJson<TraitTipTable>(ta.text);
            if (d == null || d.entries == null) return;
            foreach (var e in d.entries)
                if (e != null && !string.IsNullOrEmpty(e.key)) _tips[e.key.Trim().ToLowerInvariant()] = e;
        }

        /// <summary>文案表里收了多少条（自检用）。🔴 v2 起 = **规范键的条数**（= `Card_Trait/*` 那一族），
        /// ⛔ 不是「规则书 61 条」—— 那 61 条里官方覆盖的 56 条已换成官方正文。</summary>
        public static int TraitCount { get { LoadTips(); return _tips.Count; } }

        /// <summary>这一条正文的来源（自检/诊断用）：`official` / `rulebook` / `none` / `<不认识>`。</summary>
        public static string TraitSource(string key)
        {
            LoadTips();
            string k = key == null ? null : key.Trim().ToLowerInvariant();
            string canon = string.IsNullOrEmpty(k) ? null : Badges.KeyOf(k);
            TraitTipRow t;
            if (string.IsNullOrEmpty(canon) || !_tips.TryGetValue(canon, out t)) return null;
            return t.src;
        }

        /// <summary>
        /// 🆕 2026-09-21：**`&lt;link>` 的统一入口** —— 先看那几个**资源/数值**键，其余一律当关键词。
        ///
        /// 为什么要有这一层：卡面上的 link 有**两个来源** ——
        ///  · **关键词段**（`CardText.KeywordSegment`，键 = 规范键）；
        ///  · **效果正文里的行内图标**（`CardIcons.Rewrite`，键 = `Badges.KeyOf(图名)`）——
        ///    那里除了关键词图，还有 `Melee` / `Ranged` / `questPointsN` 这几种**不是关键词**的图
        ///    （原版对它们用的是那 7 个**显式 key**，见 `资料/tooltip_原版规格与实现.md` §一）。
        /// ⚠️ `faith` / `spiritstone` / `sabotage` **不在这里列** —— 它们**本身就是关键词**
        ///    （规则书 :184 / :210 / :204 有条目）⇒ 走 `Trait` 拿到的「图标 + 标题 + 规则书原文」更全。
        ///    🔴 **2026-09-21 补注 `sabotage` 这一条**：它的**机制**与**卡面那行字**都走 `subtype`
        ///    （兵种行），**不是** `keywords`（那三个误抽词已清，见 `资料/关键词图标_现状与总表.md` §六 第 7 条）。
        ///    这里留着它**没有坏处**（`Trait` 里本来就有它那条规则书文案），
        ///    只是目前**没有卡面会为它生成 `&lt;link=sabotage>`** —— 兵种行那一行是纯文本、不带链接。
        ///    哪天要给它做链接，得先有原版出处（别自己发明）。
        /// </summary>
        public static string ByLink(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            switch (id)
            {
                case "melee":       return Melee;
                case "ranged":      return Ranged;
                case "questpoints": return QuestPoints;   // 图 `questPointsN`（数字烘在图里）→ 规范键是 `quest`
                default:            return Trait(id);
            }
        }

        /// <summary>
        /// 一个关键词的 tooltip 正文：**图标 + 标题（显示名 + 英文名）+ 正文**。
        /// <para>🔴 **2026-10-12（`A1660`）**：原来末尾还贴一句「（规则书 :NNN）」—— **已整条删除**
        /// （那是我们自加的，原版 `TextTooltipController__GetTraitTooltip` 只凑**标题 + 描述**两段）。
        /// 用户真机的原话是「**只写该词条的效果就行，不要多余的出处**」。</para>
        ///
        /// <para>🔴 **2026-10-14（`A1803`）正文换成官方那两列**（判据 → `工具/gen_trait_tips.py` 文件头）：
        /// `src:official`（56 条）取官方 zh/en 各取各的；`src:rulebook`（5 条）只有中文 ⇒ **英文档下会印中文**、
        /// 并 `Debug.Log` 出声；两边都没有（`ability`）⇒ 正文空。</para>
        ///
        /// <para>🔴 **正文凑不出来时只出「图标 + 名字」+ 如实说明**（`Tips/Trait/NoRulebookEntry`）
        /// —— **不编一句解释**（自造词 `ability` 就是这一种）。
        /// 这也是原版的行为：`EverguildTraitTooltipItem` 一样是「图标 + 标题 + 正文」，查不到描述就没有正文。
        /// 连名字都凑不出来（键是空的）才返回 null，调用方**不弹面板**。</para>
        /// </summary>
        public static string Trait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            LoadTips();

            // 标题：显示名优先，英文名兜底。**两个都拿不到就没得显示。**
            // 🔴 **2026-10-10（`A1084` 第二步 · 原 `A1086③`）就地修一处同族缺陷**：原来写的是
            //   `title = CardText.KeywordZh(key)`（**没有语言闸** ⇒ 恒中文）+ `title += "（" + en + "）"`
            //   （**裸全角括号字面量**）⇒ **英文档下关键词 tooltip 的标题仍是中文**，
            //   而且括注会印成 `护甲（Armour）` 这种中英混排。
            //   ⇒ ① 显示名改走 `CardText.KeywordDisplay`（**跟语档**：中文档中文名、英文档英文名）；
            //     ② 那对括号落成词条 `Tips/Trait/TitleWithEn`（形状 `{0}（{1}）` / `{0} ({1})`）。
            string disp  = CardText.KeywordDisplay(key);     // 显示名：**跟语档**
            string en    = CardText.KeywordEn(key);          // 英文名：恒英文（只用来补那条括注）
            string title = !string.IsNullOrEmpty(disp) ? disp : en;
            if (string.IsNullOrEmpty(title)) return null;
            // 两端名字**相同**（= 英文档，显示名本来就是英文名）⇒ **不套括注**（否则印成 `Armour (Armour)`）
            if (!string.IsNullOrEmpty(disp) && !string.IsNullOrEmpty(en) &&
                !string.Equals(disp, en, System.StringComparison.Ordinal))
                title = Loc.T("Tips/Trait/TitleWithEn").Replace("{0}", disp).Replace("{1}", en);

            // 图标：与卡面/徽标**同一份判据**（`Badges.SpriteOf`），认不出就**不画**、不猜
            string sprite = Badges.SpriteOf(key);
            string icon = string.IsNullOrEmpty(sprite) ? "" : "<sprite name=\"" + sprite + "\">";

            // 🔴 **2026-10-14（`A1803`）查表的键改口径**：原来是「中文显示名」
            //   （`CardText.KeywordZh(key)`），v2 起是**小写规范键**。
            //   ⚠️ **必须过 `Badges.KeyOf` 归一**（不是裸 `ToLowerInvariant`）：来的键可能是**图名**
            //      或**官方键**（`frenzied` / `rage` / `markOfChaos` / `concussive` / `markOfSlaanesh`
            //      —— 见 `CardIcons.Rewrite` 那条路），而表里存的是**我们 Loc 的规范键**
            //      （`destroyer` / `penitence` / `darkpact` / `concussion`）。
            //   判据表**只此一处**（`Badges.SpriteKey`）—— ⛔ 别在这里另写一张别名表。
            string canon = Badges.KeyOf(key);
            TraitTipRow t = null;
            if (!string.IsNullOrEmpty(canon)) _tips.TryGetValue(canon, out t);

            if (t == null)
            {
                // 🔴 下面那句是**我们自己加的**：原版查不到描述时**只有「图标 + 标题」、没有正文**
                //    （`EverguildTraitTooltipItem`，见 `资料/tooltip_原版规格与实现.md` §五）
                //    ⇒ 键 `Tips/Trait/NoRulebookEntry` **自拟**（两列都是我们写的）。
                // ⚠️ **不静默**：日志里说一次「文案表里没这条」，免得以后有人以为文案表漏生成
                if (_warnedNoEntry.Add(key))
                    Debug.Log("[TipText] `" + key + "` 在关键词文案表里没有条目 —— "
                              + "tooltip 只出名字、不出解释（`ability` 就是这一种：官方 `CardTraitDescription/*` "
                              + "与规则书 61 条里都没有它）。");
                return "<b>" + icon + title + "</b>\n"
                              + Loc.T("Tips/Trait/NoRulebookEntry");
            }

            // 🔴 **2026-10-12（`A1660`）：这里原来拼「（规则书 :NNN）」贴正文尾 —— 已【整条链路】删掉。**
            //   判据（原版**没有**这一行）：① 反编译 `TextTooltipController__GetTraitTooltip.c` 只凑
            //   **两段** —— `TraitNameToStringNoImage(...)`（标题）+ `GetTraitDesc(...)`（描述），没有第三段；
            //   ② 解包预制体 `Trait Tooltip`（`bundle_duplicateassetisolation_assets_all/GameObject/`）
            //   的子节点里 `Line` 与 `Background`/`TraitImage` **组件数相同** = 纯 `Image` 分隔线，
            //   **正文节点只有 `Description` 一处** ⇒ 零出处节点。
            //   ⇒ 三处一起清（⛔ 不是只在渲染处 `if` 掉）：键 `Tips/Trait/RulebookLine`（`Core/Loc.cs`）、
            //     生成物的 `line` 字段（`工具/gen_trait_tips.py` → `Resources/trait_tips.json`）、本句。
            //   ⇒ 正文 = **标题行 + 正文**，与原版同形。回归见 `Editor/BattleScene.cs` §15c。

            // 🔴 **2026-10-14（`A1803`）正文跟语档**：
            //   · `src == "official"` ⇒ 官方那两列（中/英）**都在**，各取各的；
            //   · `src == "rulebook"` ⇒ 官方**确实没有**这个词（`ferocity`/`huntmark`/`oath`/`pack`/
            //     `sentry`）⇒ 只有中文那一列，**英文档下会印中文** ⇒ **出声**说明（⛔ 不静默、⛔ 也不许
            //     自己译一句冒充官方）。判据 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。
            bool useZh = Loc.Effective == AvailableLanguages.Chinese;
            string body = useZh ? t.bodyZh : t.bodyEn;
            if (string.IsNullOrEmpty(body))
            {
                body = useZh ? t.bodyEn : t.bodyZh;          // 另一列兜底（别让正文整段没了）
                if (!string.IsNullOrEmpty(body) && _warnedSrc.Add(canon))
                    Debug.Log("[TipText] `" + canon + "` 的正文没有 "
                              + (useZh ? "中文" : "英文") + "那一列（`src=" + t.src + "`）⇒ 退回另一列。");
            }
            if (string.IsNullOrEmpty(body))
                return "<b>" + icon + title + "</b>\n" + Loc.T("Tips/Trait/NoRulebookEntry");
            return "<b>" + icon + title + "</b>\n" + body;
        }
    }
}
