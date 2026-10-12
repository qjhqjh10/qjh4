// ChatTab.cs —— 从 `Shell/ChatPanel.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ==================================================================
    //  `ChatTab` —— 一个频道页（原版 `ChatTab` + `ChatContentView`）
    // ==================================================================

    /// <summary>原版 `ChatTab`（`content` 指向 `ChatContentView`）。`ChatContentView` 是 **OSA 虚拟列表**
    /// （`OSA&lt;BaseParamsWithPrefab, ChatEntryView&gt;`）—— 我们没有 OSA ⇒ 用**逐行建 + MenuScroll** 那条路
    /// （同 `BattleLogTab` 的做法）。参数照抄原档：`_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` ·
    /// `_DefaultItemSize = 60`（判据 → 普查 §A·2 第 112–117 行）。</summary>
    public class ChatTab : WindowTabBase
    {
        public override WindowTabType Type
        { get { return _channel == "Global" ? WindowTabType.ChatGlobal : WindowTabType.ChatAlliance; } }

        ChatPanel _win;
        Transform _root, _content;
        PxRect _rect;
        string _channel;
        public int BuiltRows { get; private set; }
        public string Channel { get { return _channel; } }

        const float PadL = 25f, PadR = 25f, PadT = 5f, PadB = 5f, Spacing = 10f, DefaultRowH = 60f;

        /// <summary>🔴 **原版 `Chat Tab/Viewport` 的 `RectMask2D` 软边 = `(0, 22)`**（画布像素：
        /// `x` 管左右两条边、`y` 管上下两条边；`(0,0)` = 硬边 = 本页原来那套行为）。
        /// 判据 = **逐字实读原档**：`assets_full/bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/
        /// MonoBehaviour_-7904774033703794794.json` —— 那个 `RectMask2D` 挂的 GO 正是 `Chat Tab/Viewport`
        /// 的 RT（`m_Softness = {x:0, y:22}` · `m_Padding = (0,0,0,0)` · `m_Enabled = 1`；
        /// 整包 1 条，全量表 → `d:/4/_tmp_view/q1_rm2d.txt` 的 `bundle_mainmenualwaysloaded_assets_all` 那一节）。
        /// 纵向滚动区 ⇒ **上下各 22px 渐隐、左右硬边**。
        /// <para>🔴 **为什么是「逐件传下去」而不是像别的窗那样设一个 `ClipSoftness`**（A78①）：
        ///   本窗是 `ChatPanel : GameWindowWithTabs : GameWindow : MonoBehaviour`，**不是 `MenuWindowBase` 的子类**
        ///   ⇒ 那三兄弟（`Clip` / `ClipSoftness` / `ClipPad`）**当年一个都够不着**
        ///   （🔴 **2026-10-10 订正（A194）**：三兄弟**已上移到 `GameWindow`**（A78② 落地）⇒ **今天够得着了**；
        ///   ⚠️ **本窗仍走「逐件传」是 A78① 的裁定、不是回归** —— ⛔ 别看到「够得着」就去改成设 `ClipSoftness`。
        ///   原文：「它们只长在 `MenuWindowBase` 上」（按**名字**找那三个字段 —— ⛔ 别写行号，它会过期））。而 `MenuDraw.Rect/Nine/ClipText` 本来就收 `clipSoftness`
        ///   ⇒ 这里照本页既有的做法（`clip` 也是逐件传的，见 `Setup`/`Rebuild`）把软边一起传下去 ——
        ///   机制**只有 `MenuDraw` 那一份**，本页不新写第二份。
        ///   ⚠️ 全量表里 `GameWindow` 族（`bundle_generalgamewindows_assets_all` 那 5 个 `RectMask2D`）
        ///   **软边全是 `(0,0)`** ⇒ **今天只有本窗需要这条路**；将来真要给那一族加，
        ///   该做的是把「当前 clip/软边」挪到 `GameWindow` 那一层，⛔ 不是每扇窗各抄一段。</para></summary>
        static readonly Vector2 VpSoft = new Vector2(0f, 22f);

        /// <summary>🆕 **2026-10-05（A38 顺手发现①）**：这一页的**纵向滚动区**（全壳唯一一份滚动实现
        /// = `MenuScroll`）。补之前它**连滚动区都没有** ⇒ 消息行既不滚也不裁，**超一屏就画到框外**、
        /// 第一屏之外的行永远看不到也点不到（`grep MenuScroll Shell/ChatPanel.cs` 那时零命中）。
        /// 判据与对齐对象 → `Setup` 里那段注释。</summary>
        MenuScroll _scroll;

        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll.Wheel/SetOffset`（**和真滚同一条路**）。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        /// <summary>内容底边（画布 px · 屏幕坐标 · 偏移 0 时）—— 就是写进滚动区 `ContentX2` 的那个值。</summary>
        public float ContentBottom { get; private set; }

        public void SetHost(ChatPanel win, Transform root, PxRect rect, string channel)
        { _win = win; _root = root; _rect = rect; _channel = channel; }

        public override void Setup()
        {
            // `Viewport`（`RectMask2D`，softness (0,22)）+ `Content`
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Viewport` 就是**裁切状态的载体**
            //    （= 原版 `Chat Tab/Viewport` 身上那个 `RectMask2D`）—— 参数取原档逐字实读的
            //    `m_Padding = (0,0,0,0)` · `m_Softness = (0,22)`（见上面 `VpSoft` 那段）
            //    ⇒ 与迁移前「`SetClip`/逐件传 `vpR` + `VpSoft`」逐位同值。
            var vp = ViewportClip.Hang(_root, "Viewport", _rect, Vector4.zero,
                                       new Vector2Int((int)VpSoft.x, (int)VpSoft.y)).transform;

            // 🆕 **2026-10-05（A38 顺手发现①）：把滚动区补上** —— 做法**照 `Shell/BattleLogPopup.cs`**
            //   （同一族：全壳只有一份滚动实现 `MenuScroll`；`BattleLogPopup` / 排行榜 / 对局历史那批
            //   都是这么接的），逐项对齐：`Owner` / `OnChanged` / `ContentX2` / 逐行 `Shift` + `ClipRect`
            //   一个都不少。⛔ **别在这里另写一份滚动逻辑**（CLAUDE.md §三）。
            // 🔴 **原版这一格不是 uGUI `ScrollRect`** —— ⛔ 别照抄 `BattleLogPopup` 那组 `Clamped/1.0` 的数
            //   （铁律 5·c：一个值 ≠ 全部情况）。实读原档：`Chat Tab` 那个 GO 上**没有 `ScrollRect`**
            //   （`bundle_mainmenualwaysloaded_assets_all/GameObject/Chat Tab.json` 的 5 个组件 =
            //    RT + `Image`(α0) + **`ChatContentView`** + `ChatTab`，另两处 GraphicRaycaster；
            //    同一棵树里 `Viewport` 挂的是 `RectMask2D`）。它是一棵 **TheFallenGames OSA 虚拟列表**：
            //   `class ChatContentView : OSA<BaseParamsWithPrefab, ChatEntryView>`（`Warpforge_code` 签名桩
            //   `ChatContentView.cs:8`），滚动参数全在组件 `MonoBehaviour_-8786325734254409834.json` 的
            //   `_Params` 里，**逐字段实读**：
            //     `_Content` = `Viewport/Content`(RT `-7815640432147003498`) · `_Viewport` = `Viewport`(RT `870065029823367062`)
            //     · `_Orientation = 0`(竖) · `_ContentPadding = (L25, R25, T5, B5)` · `_ContentSpacing = 10`
            //     · `_DefaultItemSize = 60` · `_ScrollSensivity = 20` · `_ScrollSensivityOnXAxis = 100` ·
            //       `_DragEnabled = 1` · `_ScrollEnabled = 1` · `_UseUnscaledTime = 1` ·
            //       `_ItemTransversalSize = 0`（行宽取 viewport 宽）· `_Scrollbar = null` ·
            //       `_Effects._ElasticMovement = 1` · `_PullElasticity = 0.3` · `_ReleaseTime = 0.1` ·
            //       `_Inertia = 1` · `_InertiaDecelerationRate = 0.865` · `_CutMovementOnPointerDown = 1` ·
            //       `_MaxSpeed = 10000`（普查 §A·2 第 112–117 行逐值相同）
            //   ⇒ **能落到 `MenuScroll` 上的只有下面这几条**：纵向 · 上对齐 · pad(25,25,5,5) ·
            //     spacing 10 · itemSize 60。**OSA 那套弹性/惯性/灵敏度是另一个机制**（它自己的 `_Effects` +
            //     `_PullElasticity` + 自绘滚动），而全壳的滚动只有一份实现（CLAUDE.md §三）⇒ 手感仍走
            //     `MenuScroll` 的 Clamped + 全壳滚轮系数（`NotchK`）。
            //     ⚠️ **如实标**：这不是「照原版抄了全部参数」，是「机制不同，取能对上的那几项」。
            //   ⚠️ `_Gravity = 3` 对应哪一档**没查到**（OSA 的 `Gravity` 枚举本地没解出来，普查 §E 已记）——
            //     它不影响上面那几项（纵向 + 上对齐）。
            //   ✅ **2026-10-05（A78①）：软边（`m_Softness = (0,22)`）已接** —— 见 `VpSoft` 那段注释
            //     （机制在 `MenuDraw.ApplySoftEdges`，本页把 `VpSoft` 逐件传给 `MenuDraw`）。
            //     🔴 **本页原来那句「⛔ 别顺手在这儿接」已作废**，它当时的顾虑是「重复重切把 uv 缩掉」
            //     —— 那条**潜伏缺陷已经修掉**（`ApplySoftEdges` 的 `uv0` 改成显式入参，A52/F1，
            //     断言见 `Editor/ShellScene.cs` ⑤·c）。⛔ 别再照旧注释把这条接线推回去。
            PointerLayer.UnregisterOwnedBy(_root != null ? _root.gameObject : gameObject);   // 重开一次窗 ⇒ 旧的那份是死条目
            _scroll = MenuScroll.TopAligned(_rect, 0f);   // 内容高在 `Rebuild` 里按条数写（原版 OSA 那份 `_Content` 的高）
            _scroll.Owner = _root.gameObject;             // 这一页不显示时指针层跳过它（切页走 `SetActive`）
            _scroll.OnChanged = Rebuild;                  // 🔴 滚轮只改 `Offset`、**画是调用方的事**：不接 = 滚了什么都不动
            // ⚠️ 走 `SocialPage.RegisterScroll` 那一份（**会出声**）——`PointerLayer.RegisterScroll` 在
            //   指针层还不在场景里时是 `return`（登记表都没建）⇒ 滚轮永远落不上来，而画面看着完全正常；
            //   `SocialPage.RegisterScroll`（`SocialWindow.cs:319-329`）先判这一条、并顺带判 `Owner` 空不空。
            //   （同 `Shell/BattleLogPopup.cs` 里那句 `SocialPage.RegisterScroll(_scroll)` 那一处的选择；日志前缀是 `[Social]`，那是那个公共件的既有文案。）
            SocialPage.RegisterScroll(_scroll);

            // ⚠️ `Content` 摆在**视口左上、零高**（原版那个 RT 是全拉伸的；行位置由每行自己的绝对矩形给）
            //    —— 与 `BattleLogPopup` 逐字同形。⛔ 建完行之后**别再挪它**（子件是按「父节点当时的位置」
            //    换算 `localPosition` 的，挪一下整排都偏）。
            _content = MenuDraw.Node(vp, "Content", new PxRect(_rect.x1, _rect.y1, _rect.x2, _rect.y1));
            Rebuild();
        }

        public override void OnOpen() { Rebuild(); }

        /// <summary>按消息条数逐行建（原版 OSA 池化生成 `ChatEntryView`）。本地没服务器 ⇒ 恒 0 条。
        /// 🆕 2026-10-05：行按**滚动偏移之后**的位置摆（`MenuScroll.Shift`），整行滚出视口的**不建**。</summary>
        public void Rebuild()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(_content.GetChild(i).gameObject);
            BuiltRows = 0;
            var all = SocialData.ChatMessages;

            // 🔴 **内容高 = 原版 OSA 的算法**：`padTop + Σ(itemSize) + spacing×(n−1) + padBottom`
            //   （`_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` · 行高给了就用行高、
            //    否则 `_DefaultItemSize = 60`）。
            //   必须写进滚动区：`MenuScroll` 的 `ContentX1/X2` 就是内容两端，不写 ⇒ `ContentX1 == ContentX2`
            //   ⇒ `ClampHi == 0` ⇒ **这一页一格都滚不动**，而「整行滚出视口 ⇒ 不建」那道守卫会把第二屏起
            //   **彻底藏掉**（不是「画到框外至少看得见」）—— 同 `BattleLogPopup.BuildRows` 那条注释。
            //   ⚠️ **空表那一支也要写**（写成视口顶）—— 否则上一次的内容高留在区里，是个静默的脏值
            //   （同 `LeaderboardWindow` / `BattleLogPopup` 那两处）。
            float y = _rect.y1 + PadT;
            int n = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                y += (all[i].Height > 0f ? all[i].Height : DefaultRowH) + Spacing;
                n++;
            }
            ContentBottom = n == 0 ? _rect.y1 : y - Spacing + PadB;
            if (_scroll != null) _scroll.ContentX2 = ContentBottom;

            y = _rect.y1 + PadT;
            float w = _rect.W - PadL - PadR;
            // 🔴 **2026-10-14（A776 · δ 族第 5 处 = 最后一处）**：这一行原来在 `_rect` 与 `_scroll.Viewport`
            //    之间挑一个矩形存进局部 `vpR`，再把它喂给**下面那道粗筛**。那是**第二状态源**：
            //    矩形是**调用点自己手里的**，节点（`Viewport`）搬了它不跟 ⇒ 粗筛与渲染可能不同源。
            //    现在粗筛走 **`MenuDraw.VisibleAbove(_content, rr, null)`**（节点态：沿 `_content` 的父链
            //    解析 `ViewportClip`），**一个自持矩形都不留** ⇒ 粗筛与渲染同源。
            //    判据 / 残差 → 同族那四处（`AllianceMemberTab.cs:532`/`:1346` · `AlliancesTab.cs:531` ·
            //    `BattleLogPopup.cs:237` · `FriendsTab.cs:338`），它们都已经是这个形状。
            //    ⚠️ **留痕（铁律 5）**：本处原来有一句「照旧传 `vpR`」—— **那句话已过期**（见上一段的来龙去脉），
            //    连同局部 `vpR` 一起删掉了（留着它 = 让人以为还有一条自持矩形在用）。
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                float h = all[i].Height > 0f ? all[i].Height : DefaultRowH;
                var rr = new PxRect(_rect.x1 + PadL, y, _rect.x1 + PadL + w, y + h);
                if (_scroll != null) rr = _scroll.Shift(rr);   // 内容坐标 → 屏幕坐标（**只做偏移、不裁**）
                y += h + Spacing;                              // ⚠️ 累加**必须用内容坐标**，别用 Shift 之后的
                // 整行滚出视口 ⇒ **连节点一起不建**（与档案窗那一页 `BattleLogTab.cs` / 弹窗那份
                // `BattleLogPopup.BuildRows` 同形）。求交那一份 = `MenuDraw.Visible`（**全工程唯一一份**求交；
                // `ClipRect` 是它「顺带夹出可见矩形」的那版）。⚠️ 2026-10-07 更正（铁律 5 / A12①）：原文写
                // 「= `MenuDraw.ClipRect`（全工程唯一一份）」—— 收口后那两句是**同一份**。
                if (!MenuDraw.VisibleAbove(_content, rr, null)) continue;
                // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：`clip` 传 **`null`** —— 裁切状态已经迁到
                //    `Setup()` 建的那颗 `Viewport` 节点上（`ViewportClip`），`MenuDraw.*` 会沿父链解析到它。
                //    ⚠️ 上面那道粗筛（「整行滚出视口 ⇒ 连节点一起不建」）**也是**节点态了（A776，见上）。
                //    ⚠️ `VpSoft` 照旧传（它现在是**回落那一档**的软边；节点在时由节点说了算）。
                ChatMessageRow.Build(_win, _content, all[i], rr, null, VpSoft);
                BuiltRows++;                     // 现在 = **真建出来几行**（滚出视口的不算；断言用）
            }
        }
    }
}
