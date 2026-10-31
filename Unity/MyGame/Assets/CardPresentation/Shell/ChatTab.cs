// ChatTab.cs —— 从 `Shell/ChatPanel.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
//
// ⚠️ **2026-10-19（战斗线 `T17`）**：`Chat_Messages/*`（单位/督军台词）**与本页无关** —— 那是战斗内
//    `Battle/UnitChatPanel` 的文本来源；本页的行 = `SocialData.ChatMessages`（玩家聊天，本地无服务器 ⇒ 恒空）。
//    本页**没有需要 `Loc` 的静态文案**：页签名取自 `ChatPanel.Channels`（原版也是 `ChatRoom` 枚举名，
//    官方表里**没有**频道名的词条 —— `SocialMenu/Alliances/Language/Global` 是语言筛选、不是频道）。
//    ⇒ 本文件**只读取数据、不取词条**；⛔ 别把 `Chat_Messages/*` 接到这里（那是另一套东西）。
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
            //     `MenuScroll` 的 Clamped + **本页自己的**滚轮灵敏度（见下面「原版判据查不到」那一段）。
            //     ⚠️ **如实标**：这不是「照原版抄了全部参数」，是「机制不同，取能对上的那几项」。
            //   ⚠️ `_Gravity = 3` 对应哪一档**没查到**（OSA 的 `Gravity` 枚举本地没解出来，普查 §E 已记）——
            //     它不影响上面那几项（纵向 + 上对齐）。
            //   ✅ **2026-10-05（A78①）：软边（`m_Softness = (0,22)`）已接** —— 见 `VpSoft` 那段注释
            //     （机制在 `MenuDraw.ApplySoftEdges`，本页把 `VpSoft` 逐件传给 `MenuDraw`）。
            //     🔴 **本页原来那句「⛔ 别顺手在这儿接」已作废**，它当时的顾虑是「重复重切把 uv 缩掉」
            //     —— 那条**潜伏缺陷已经修掉**（`ApplySoftEdges` 的 `uv0` 改成显式入参，A52/F1，
            //     断言见 `Editor/ShellScene.cs` ⑤·c）。⛔ 别再照旧注释把这条接线推回去。
            PointerLayer.UnregisterOwnedBy(_root != null ? _root.gameObject : gameObject);   // 重开一次窗 ⇒ 旧的那份是死条目
            // 🔴 **2026-10-17（第四十八会话 · `N46③`）就地订正（铁律 5）**：原注释写「判据查不到 ⇒ 保留 48」，
            //    **不成立** —— 判据就在**上面同一段实读**里：OSA `_Params._ScrollSensivity = 20`（`:92`）。
            //    它不是 UGUI `ScrollRect`（所以 142 条 `m_ScrollSensitivity` 里没有它），但 OSA 自己有这台参数。
            _scroll = MenuScroll.TopAligned(_rect, 0f, 20f);   // 原版 OSA `_ScrollSensivity = 20`（内容高在 `Rebuild` 里按条数写）
            _scroll.Owner = _root.gameObject;             // 这一页不显示时指针层跳过它（切页走 `SetActive`）
            _scroll.OnChanged = Rebuild;                  // 🔴 滚轮只改 `Offset`、**画是调用方的事**：不接 = 滚了什么都不动
            // ⚠️ 走 `SocialPage.RegisterScroll` 那一份（**会出声**）——`PointerLayer.RegisterScroll` 在
            //   指针层还不在场景里时是 `return`（登记表都没建）⇒ 滚轮永远落不上来，而画面看着完全正常；
            //   `SocialPage.RegisterScroll`（`SocialWindow.cs`）先判这一条、并顺带判 `Owner` 空不空。
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
            // 🆕 2026-10-31（B107_W13 · N89）：逐行缓存「这一行对应的消息 + 那颗 `Time` 标签」——
            //   原版 `ChatContentView.Update` 每 `updateDelay` 秒对**可见项**调 `ChatMessageUI.UpdateTimer()`
            //   重算相对时间（判据 `ChatContentView.cs:71-89` · `ChatContentView__.ctor.c:17`（`0xf` = 15）·
            //   `ChatMessageUI__UpdateTimer.c`）。我们的行是**全量重建**的、没有 OSA 的 view-holder ⇒
            //   在 `Rebuild` 里顺手把「能刷新的那几颗」记下来，`RefreshTimeTexts()` 逐颗 `SetText`。
            _rowMsgs.Clear(); _rowTfs.Clear(); _timeLabels.Clear(); _lastBuiltRow = null;
            SocialData.ChatMessage lastMsg = null;
            for (int i = 0; i < all.Count; i++) if (all[i].Channel == _channel) lastMsg = all[i];
            // N88：只有「本频道的末条换了」才播滑入（原版 `AddMessage` 置 `newMessageReceived`）；
            //   `Rebuild` 也会被滚轮/切页触发，那时末条没变 ⇒ 不播。
            bool freshMessage = _hasSeenOnce && lastMsg != null && !ReferenceEquals(lastMsg, _lastSeenMsg);
            _hasSeenOnce = true; _lastSeenMsg = lastMsg;
            Transform lastRowTf = null;
            float w = _rect.W - PadL - PadR;
            float y = _rect.y1 + PadT;
            int n = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                y += RowHeightFor(all[i], w) + Spacing;
                n++;
            }
            ContentBottom = n == 0 ? _rect.y1 : y - Spacing + PadB;
            if (_scroll != null) _scroll.ContentX2 = ContentBottom;

            y = _rect.y1 + PadT;
            // 🔴 2026-10-31（B106_W12）：`w` 已提到本轮开头算（行高要用它当折行宽） —— ⛔ 别在这里再声明一次
            // 🔴 **2026-10-14（A776 · δ 族第 5 处 = 最后一处）**：这一行原来在 `_rect` 与 `_scroll.Viewport`
            //    之间挑一个矩形存进局部 `vpR`，再把它喂给**下面那道粗筛**。那是**第二状态源**：
            //    矩形是**调用点自己手里的**，节点（`Viewport`）搬了它不跟 ⇒ 粗筛与渲染可能不同源。
            //    现在粗筛走 **`MenuDraw.VisibleAbove(_content, rr, null)`**（节点态：沿 `_content` 的父链
            //    解析 `ViewportClip`），**一个自持矩形都不留** ⇒ 粗筛与渲染同源。
            //    判据 / 残差 → 同族那四处（`AllianceMemberTab.cs`/`Shell/AllianceMemberTab.cs` 的 `PadL` · `AlliancesTab.cs` ·
            //    `BattleLogPopup.cs` · `FriendsTab.cs`），它们都已经是这个形状。
            //    ⚠️ **留痕（铁律 5）**：本处原来有一句「照旧传 `vpR`」—— **那句话已过期**（见上一段的来龙去脉），
            //    连同局部 `vpR` 一起删掉了（留着它 = 让人以为还有一条自持矩形在用）。
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                float h = RowHeightFor(all[i], w);
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
                // 🆕 2026-10-31（B107_W13 · N89）：`Build` 先建行根、再往行里挂件 ⇒ 最后一颗子件就是这一行
                var rowTf = _content.childCount > 0 ? _content.GetChild(_content.childCount - 1) : null;
                _rowMsgs.Add(all[i]); _rowTfs.Add(rowTf); _lastBuiltRow = rowTf;
                _timeLabels.Add(FindTimeLabel(rowTf, all[i].Mine));
                if (ReferenceEquals(all[i], lastMsg)) lastRowTf = rowTf;
            }
            // 🆕 2026-10-31（B107_W13 · N88）：**新消息**才滑入 —— 原版 `ChatContentView.AddMessage` 置
            //   `newMessageReceived`、`Update` 里对**最后一条已建项** `DOMoveY(当前 y, 0.3s).From(y − 10)`
            //   （判据 `ChatContentView.cs:96-107`）；上一条动画还在 ⇒ 先 `Kill(complete:true)`（`:437`）。
            //   ⚠️ 末条消息若**滚出视口没建**，就没有滑入对象 —— 原版 `GetItemViewsHolderIfVisible` 同样返回 null、整跳跳过。
            if (freshMessage && lastRowTf != null) StartSlide(lastRowTf);
        }

        // ============================================================ 🆕 2026-10-31（`B106_W12`）：行高
        //   🔴 **原版行高不是常量** —— 根 `ChatMessageRow` 上 `ContentSizeFitter(m_VerticalFit = 1 MinSize)`
        //   跟着 `VerticalLayoutGroup(m_Padding L25/R60/T10/B10 · m_Spacing 12 · m_ChildControlHeight 0 ·
        //   m_ChildForceExpandHeight 0)` 撑开：`h = padTop + padBottom + Σ子件高 + spacing×(子件数−1)`。
        //   判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/ChatMessageRow.prefab`
        //     · 根 `CSF m_HorizontalFit 0 / m_VerticalFit 1`；
        //     · `VerticalLayoutGroup` 见上（`m_ChildAlignment 1 = UpperLeft`）；
        //     · 参与布局的子件两个：两个头各 **25** 高（`Player Header` / `Friend Header`，同时只亮一个）+
        //       `Message`（RT `sizeDelta.y = 0`、自带 `CSF m_VerticalFit 2 = PreferredSize` ⇒ 高度由 TMP 撑）；
        //     · `RowBackground` 挂 `LayoutElement m_IgnoreLayout = 1` ⇒ **不参与**（所以不多数它）。
        //   ⇒ `h = 10 + 10 + 25 + 12 + 正文高 = 57 + 正文高`（空正文 = **57**）。原版**没有** 60 这个下限
        //     —— `_DefaultItemSize = 60` 只是 OSA 的**占位估计**（`_PrefabControlsDefaultItemSize: 1` 运行期还会被 prefab 覆盖），CSF 只撑不夹。
        //   ⚠️ 我们原来写死 `DefaultRowH = 60`（`ChatTab.cs` 类头的 `_DefaultItemSize`）⇒ 长消息换行之后
        //     行与行**叠在一起**（原版是往下长）。这一条就是补那半。
        const float RowPadTop = ChatMessageRow.RowPadTop, RowPadBottom = ChatMessageRow.RowPadBottom, RowHeadH = 25f, RowGap = 12f;   // #N127：上下内边距的唯一写点在 `ChatMessageRow`（原版 VLG `m_Padding.T10/B10`），这里只**引用**、⛔ 不再各写一遍
        const float MsgPx = 22f;                 // = `ChatMessageRow` 正文的 `m_fontSize`（判据同上 prefab）

        /// <summary>一行（设计 px）。数据侧显式给了 `ChatMessage.Height` 就用它（原版那一格就是 OSA 的
        /// 逐条尺寸口），否则按上面那条 CSF 算式现算。</summary>
        public float RowHeightFor(SocialData.ChatMessage m, float rowW)
        {
            if (m == null) return DefaultRowH;
            if (m.Height > 0f) return m.Height;
            float wrap = Mathf.Max(1f, rowW - ChatMessageRow.RowPadLeft(m.Mine) - ChatMessageRow.RowPadRight(m.Mine));
            float msgH = MeasureMessagePx(SocialData.Text(m), wrap);
            return RowPadTop + RowPadBottom + RowHeadH + RowGap + msgH;   // 2026-10-31（B107_W2）：**不夹 60** —— 原版 CSF 只给 `57 + 正文`，空正文 = 57（判据见上面那段）
        }

        /// <summary>量正文的高度（设计 px）。做法照全仓唯一先例 `Shell/ToastNotifications.MeasureTextPx`：
        /// 建一颗**屏幕外**的临时 `Label` → `ForceRelayout()` → 读 `WorldH` → **立刻销毁**。
        /// <para>🔴 **为什么挂在顶层临时节点上、而不是挂 `_content`**：本页的 `_content` 父链上有
        /// `ViewportClip`（`Setup` 那颗 `Viewport`），而 `MenuDraw.Text` 的「整块在框外 ⇒ 连节点一起不建」
        /// 那道闸会按那个裁切框判 —— 量尺摆在 −40000px 处就**必然被闸掉**（返回 null，静默量不到）。</para>
        /// <para>⚠️ 量不出来（返回 null / 天文数字）⇒ **出声**并退回「一行 22px」的估计值（⛔ 不静默）。</para></summary>
        float MeasureMessagePx(string text, float wrapPx)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            var tmp = new GameObject("__ChatMeasure");
            try
            {
                var r = new PxRect(-40000f, -40000f, -40000f + wrapPx, -40000f + 400f);
                var lb = MenuDraw.Text(tmp.transform, r, text, Color.white, "__chat_measure", MsgPx, ChatPanel.QText,
                                       wrapPx, 0f, 0f, 0f, face: TmpFont.Face.LatinCardBody);
                if (lb == null)
                {
                    Debug.LogWarning("[Chat] 量正文高度失败（`MenuDraw.Text` 返 null）⇒ 退回「一行 " + MsgPx + "px」的估计值。");
                    return MsgPx * 1.25f;
                }
                lb.ForceRelayout();
                float h = lb.WorldH * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
                if (!(h > 0f) || h > 4000f)
                {
                    Debug.LogWarning("[Chat] 量正文高度失败（h = " + h + "）⇒ 退回「一行 " + MsgPx + "px」的估计值。");
                    return MsgPx * 1.25f;
                }
                return h;
            }
            finally
            {
                SocialWindow.DestroySafe(tmp);
            }
        }

        // ============================================================================================
        // 🆕 2026-10-31（B107_W13）—— N88 新消息滑入 / N89 15 秒计时刷新（全部追加在类尾）
        // 🔴 纪律：本文件被 `账/社交_聊天.tsv` 按行号引用（现读最高 `:145`）⇒ 这些成员**只追加在类尾**，
        //    上面每一行的行号零变化。
        // ============================================================================================

        /// <summary>#N88 滑入时长（秒）—— 原版 `ChatContentView.Update` 的 `DOMoveY(…, 0.3f)`
        /// （判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChatContentView.cs:100`）。</summary>
        public const float SlideDuration = 0.3f;
        /// <summary>#N88 起始下移量（画布 px）—— 原版 `From(transform.position.y − 10f)`（同上 `:102-104`）。</summary>
        public const float SlideOffsetPx = 10f;
        /// <summary>#N89 计时刷新周期（秒）—— 原版 `updateDelay = 15`（`ChatContentView__.ctor.c:17` 写 `0xf`；
        /// `out_cpp2il_dev_ilspy/ChatContentView.cs:518`）。</summary>
        public const int TimerRefreshSeconds = 15;

        Transform _slideRow;
        Vector3 _slideBase;
        float _slideT;
        bool _sliding;

        readonly List<SocialData.ChatMessage> _rowMsgs = new List<SocialData.ChatMessage>();
        readonly List<Transform> _rowTfs = new List<Transform>();
        readonly List<Label> _timeLabels = new List<Label>();
        Transform _lastBuiltRow;                        // 最后一个**建出来**的行（自检读口；未必是末条消息那一行）
        SocialData.ChatMessage _lastSeenMsg;
        bool _hasSeenOnce;
        System.DateTime _lastTimerTick = System.DateTime.Now;
        /// <summary>已执行的 15s 刷新次数（自检读口）。</summary>
        public int TimeRefreshCount { get; private set; }

        /// <summary>最后一个建出来的行（滚出视口的不算）。</summary>
        public Transform LastBuiltRow { get { return _lastBuiltRow; } }
        /// <summary>当前这一页记下的 `Time` 标签数（= 本页建出来的行数；可能含 `null`，见 `BuiltTimeLabelAt`）。</summary>
        public int BuiltTimeLabels { get { return _timeLabels.Count; } }
        /// <summary>第 i 个建出来的行（`null` = 没建 / 已被重建清掉；自检读口）。</summary>
        public Transform BuiltRowAt(int i) { return i >= 0 && i < _rowTfs.Count ? _rowTfs[i] : null; }
        /// <summary>第 i 个建出来的行对应的消息（自检读口）。</summary>
        public SocialData.ChatMessage BuiltMessageAt(int i) { return i >= 0 && i < _rowMsgs.Count ? _rowMsgs[i] : null; }
        /// <summary>第 i 个建出来的行的 `Time` 标签（`null` = 这一颗被裁切闸挡掉、没建）。</summary>
        public Label BuiltTimeLabelAt(int i) { return i >= 0 && i < _timeLabels.Count ? _timeLabels[i] : null; }
        /// <summary>正在播滑入吗。</summary>
        public bool IsSliding { get { return _sliding; } }
        /// <summary>滑入进度 0..1（没在播 = 1）。</summary>
        public float SlideProgress { get { return _sliding ? Mathf.Clamp01(_slideT / SlideDuration) : 1f; } }
        /// <summary>当前行相对基准位的**纵向偏移**（设计 px；负 = 还在下面 10px 那一档）。自检读口。</summary>
        public float SlideOffsetPxNow { get { return -SlideOffsetPx * (1f - EaseOutQuad(SlideProgress)); } }

        /// <summary>DOTween 的默认缓动 `Ease.OutQuad` = `1 − (1 − k)²`（原版这两跳没显式设 Ease ⇒ 吃默认值）。</summary>
        static float EaseOutQuad(float k) { k = Mathf.Clamp01(k); return 1f - (1f - k) * (1f - k); }

        /// <summary>取行里那颗 `Time` 标签（原版 `ChatMessageUI.UpdateTimer` 刷的就是它）。
        /// ⚠️ `Time` 挂在两套头的下面（`Player Header` / `Friend Header`，同时只亮一套）⇒ 按发信侧取路径。</summary>
        static Label FindTimeLabel(Transform row, bool mine)
        {
            if (row == null) return null;
            var head = row.Find(mine ? "Player Header" : "Friend Header");
            if (head == null) return null;
            var lb = head.Find("Time");
            return lb != null ? lb.GetComponent<Label>() : null;
        }

        /// <summary>每帧泵（play 模式；批处理没有帧循环 ⇒ 自检直接调 `TickSlide` / `RefreshTimeTexts`）。</summary>
        void Update() { TickSlide(Time.deltaTime); TickTimers(); }

        /// <summary>#N88 播滑入（生产路：只在 play 模式起来；批处理/自检走 <see cref="BeginSlideForTest"/>）。
        /// 🔶 自己设计（非原版）：原版用 DOTween `DOMoveY(...).From(...)`，这里用**同一算式**按 `Time.deltaTime` 推进
        /// （批处理没有帧循环、DOTween 不会走）—— 行为等价（0.3s / 10px / OutQuad）· 执行代理 B107_W13 2026-10-31。</summary>
        void StartSlide(Transform row)
        {
            if (row == null) return;
            if (_sliding) FinishSlide();                   // 原版 AddMessage：上一条还在播 ⇒ `Kill(complete:true)`
            if (!Application.isPlaying) return;            // 没有帧就没有动画（同 `AlliancePanelWindow.StartAppear`）
            BeginSlideCore(row);
        }

        /// <summary>自检入口：**不受** `Application.isPlaying` 闸（宿主手动 `TickSlide` 推进）。</summary>
        public void BeginSlideForTest(Transform row)
        {
            if (row == null) return;
            if (_sliding) FinishSlide();
            BeginSlideCore(row);
        }

        void BeginSlideCore(Transform row)
        {
            _slideRow = row; _slideBase = row.localPosition; _slideT = 0f; _sliding = true; ApplySlide(0f);
        }

        void ApplySlide(float k)
        {
            if (_slideRow == null) return;
            float e = EaseOutQuad(k);
            var p = _slideBase; p.y -= LayoutSpace.Px(SlideOffsetPx) * (1f - e);
            _slideRow.localPosition = p;
            if (k >= 1f) { _slideRow.localPosition = _slideBase; _slideRow = null; _sliding = false; }
        }

        /// <summary>推进滑入（`Update` 每帧调；自检直接调）。</summary>
        public void TickSlide(float dt)
        {
            if (!_sliding) return;
            _slideT += dt;
            ApplySlide(Mathf.Clamp01(_slideT / SlideDuration));
        }

        /// <summary>立刻收尾（原版 `Kill(complete:true)` 的等价：落到终态）。</summary>
        public void FinishSlide()
        {
            if (_sliding) ApplySlide(1f);
            if (_slideRow != null) { _slideRow.localPosition = _slideBase; _slideRow = null; }
            _sliding = false;
        }

        /// <summary>#N89 到点才刷（原版 `ChatContentView.Update`：`(DateTime.Now − lastUpdate).TotalSeconds > updateDelay`）。
        /// 返回 true = 这一拍真的刷了。</summary>
        public bool TickTimers()
        {
            var now = System.DateTime.Now;
            if ((now - _lastTimerTick).TotalSeconds <= TimerRefreshSeconds) return false;
            _lastTimerTick = now;
            RefreshTimeTexts();
            return true;
        }

        /// <summary>= 原版 `ChatContentView.UpdateTimers()` → 每个可见项 `ChatMessageUI.UpdateTimer()`：
        /// 按 `TimestampMs` 与 `UtcNow` 的差重算那颗 `Time`（`SocialData.TimeText` 是同一份实现）。
        /// 返回刷新的行数。</summary>
        public int RefreshTimeTexts()
        {
            int n = 0;
            for (int i = 0; i < _timeLabels.Count; i++)
            {
                var lb = _timeLabels[i];
                if (lb == null) continue;
                lb.SetText(SocialData.TimeText(i < _rowMsgs.Count ? _rowMsgs[i] : null));
                n++;
            }
            TimeRefreshCount++;
            return n;
        }
    }
}
