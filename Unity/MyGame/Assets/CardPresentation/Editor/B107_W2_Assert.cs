// B107_W2_Assert.cs — 执行代理 `B107_W2`（第一百零七会话）
//
// ============================ 它盯的是什么 ============================
//  A 段 **聊天行高没有 60 下限**（`Shell/ChatTab.cs` 的 `RowHeightFor`）
//      原版根 `ChatMessageRow` 挂 `ContentSizeFitter(m_VerticalFit = 1 = MinSize)` + `VerticalLayoutGroup`
//      （`m_Padding L25/R60/T10/B10` · `m_Spacing 12` · `m_ChildControlHeight 0`），参与布局的活子件 = **一个头（25 高）+ Message**：
//      `h = 10 + 10 + 25 + 12 + 正文高 = 57 + 正文高`。**没有下半截** ——
//        · 根上**没有** `LayoutElement`（`ChatMessageRow.prefab` 根 GO 只有 RT + VLG + CSF + `ChatMessageUI`；全 prefab 三个
//          `LayoutElement` 都在子件上，`RowBackground` 那颗还是 `m_IgnoreLayout: 1`）；
//        · `_DefaultItemSize = 60` 只是 OSA 的**占位估计**（`_PrefabControlsDefaultItemSize: 1` 运行期还会被 prefab 覆盖）；
//        · TMP 空正文的 preferredHeight = **0**（`TMP_Text.cs:3901-3905`：`m_TextProcessingArray[0].unicode == 0 ⇒ return Vector2.zero`）。
//      而正文**真的会空**：`SocialData.Text` 的 `default:` 支对「没有模板」的类型（`Practice` / `Rally` / `RewardShare` / `ReplayShare` …）
//      一律 `return ""`（`Shell/SocialData.cs:324`，= 原版 `ChatMessageData.GetMessage` 的同形分支）。
//      ⇒ 我们原来那层 `Mathf.Max(DefaultRowH, …)` 会把空正文行撑成 60（原版 57，每行差 3px）。
//  B 段 **空态轨道**（`Shell/CampaignTab.cs` 的 `BuildTrack()` 这条入口）
//      `B106_W73` 只修了 `RefreshNodes()` 那条入口；`BuildTrack()`（`Build()` / `BuildTrackForTest()`）在空态下**照建 47 个节点**，
//      于是「切到没有战役的阵营」仍能横向滚进一大片空白。
//      原版判据（`d:/2/tools/decomp_full/`）：
//        · `CampaignTrack__Initialize.c`：先 `ClearTrack`，`if (campaign != null)` 才逐个 `Instantiate` 节点 ⇒ 传 null **只清不建**；
//        · `CampaignWindowTab__ChangeArmy.c`：`FirstOrDefault(campaigns, c => c.Data.Army == army)` 查不到 ⇒ **整条 no-op**；
//        · `CampaignWindowTab__OnOpen.c` + `__c___OnOpen_b__13_1.c`：阵营选择器的条目 = **把 campaign 列表 map 成 army id**
//          ⇒ **原版根本选不到「没有战役」的阵营**，「空态」这一态在原版不存在。
//      ⇒ 空态长什么样 = `🔶 自己设计`（本地 13 阵营全列、只有 Ultramarines 一套战役表，其余在服务端 / 已关服；调度台 2026-10-31 裁定）：
//        **0 节点 + 0 连线 + 滚动区零宽**，并且「切回有内容阵营 ⇒ 轨道与滚动跨度都回来」。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · A 段 `B107W2_Assert.RunChatRowHeight(ctab)` —— 宿主 **`Editor/CollectionScene.cs`**（聊天行高/滚动那一节就在那儿，
//    局部变量 `ctab` 就是 `ChatTab` 实体）。接在「收尾：清数据 + 关窗」那三行**之前**（现读 `:~11690`，`SocialData.ChatMessages.Clear();` 之上）。
//    不建窗、不建节点：空正文那一支 `MeasureMessagePx` 直接返回 0；单行那一支建一颗临时量尺、量完自己销毁。
//  · B 段 `B107W2_Assert.RunCampaignTrack(cTab, cContent)` —— 宿主 **`Editor/RewardsScene.cs`**（局部变量 `cTab` = `CampaignTab`、
//    `cContent` = `Campaign Track/Viewport/Content`）。接在 `FP-CAMP-01` 那段「收工还原」的 `nodesBack` 块**之后**、
//    `// ---- FP-CAMP-06 ②：战役红点跟着「可领节点」变 ----` **之前**（现读 `:~6205`）。
//    它自己 `CampaignData.Select(...)` 两态来回切、`finally` 还原成进来时的阵营 ⇒ 放在「还原」之后不会打乱宿主状态。
//  · 两段都**只调生产口**（`RowHeightFor` / `RefreshNodes` / `BuildTrackForTest` / `MenuScroll`），不建持久资产。
//
// ============================ 🔴 灭自证 ============================
//  · A①：把 `Mathf.Max(DefaultRowH, …)` 加回 `ChatTab.RowHeightFor` ⇒ 空正文那一条 **57 → 60** ⇒ 红（差 3px，与 B 段无关）。
//  · A②：单行那条的期望 = `57 + 90/95×22`（字体资产的行盒，**独立来源**）⇒ 谁能把 `RowPadTop/Bottom/RowHeadH/RowGap`
//    或量高口改错，它跟 A① 不会一起变绿。
//  · B②/③：空态那两条同时盯**节点数**与**滚动跨度** —— 只删节点不收跨度（= 原来的 `RefreshNodes` 半成品）⇒ 跨度那条红。
//  · B④：只「早退」不「恢复」⇒ `builtBack == 0` / `MaxOffset == 0` ⇒ 红（这就是 `B106_W73` 踩过的坑）。
//
// ============================ 判据 / 报告 ============================
//  报告：`资料/完全复刻/报告/B107_W2.md`（⛔ 别在这里抄第二份）
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B107_W2`：聊天行高的 60 下限（原版没有）＋ 战役轨道空态的两条入口。</summary>
public static class B107W2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107W2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 独立来源的两个常量
    // ① 行盒：`Resources/Fonts/Pragati-Regular SDF.asset` 的 `m_LineHeight 90` / `m_PointSize 95`，正文 `m_fontSize = 22`
    //    （判据同 `B106_D4` §0；⛔ 这里**不回读** `ChatTab.MeasureMessagePx`）
    const float FontLineH = 90f / 95f * 22f;          // = 20.842
    // ② 行基线：`ChatMessageRow.prefab` 的 VLG `m_Padding.Top 10` + `.Bottom 10` + 一个头 `25` + `m_Spacing 12`
    const float RowBase = 10f + 10f + 25f + 12f;      // = 57

    // ==================================================================
    //  A 段：聊天行高（⛔ 没有 60 下限）
    // ==================================================================
    /// <summary>A 段入口。`tab` = 宿主手里那颗 `ChatTab`（`RowHeightFor` 不读任何实例字段，但它是实例方法）。</summary>
    public static void RunChatRowHeight(ChatTab tab)
    {
        MenuCheck.Section(_sink, "🆕 B107_W2 · A：聊天行高 = 57 + 正文高（原版 CSF 只撑不夹 ⇒ 空正文 57，不是 60）");
        T(tab != null, "（前提）拿到 `ChatTab`（行高算式要它；这一支**不建窗**）");
        if (tab == null) return;

        const float RowW = 1150f;   // 折行宽（1630 视口 − padL/padR 两侧 25+60 的余量，随便取一个够宽的；短串恒单行）
        var empty = new SocialData.ChatMessage { Channel = "Global", Sender = "S", Text = "" };
        var one = new SocialData.ChatMessage { Channel = "Global", Sender = "S", Text = "msg-01" };

        N(tab.RowHeightFor(empty, RowW), RowBase, 0.01f,
          "★ 空正文行高 = **57**（padT10 + padB10 + 头25 + spacing12 + 正文 0）—— 原版 prefab 的 CSF `m_VerticalFit=1` 只把根撑到 preferred、"
          + "根上**没有** `LayoutElement`，`_DefaultItemSize 60` 只是 OSA 的占位估计 ⇒ ⛔ 不许再夹出 60（夹回去这里 57→60 ⇒ 红）");
        N(tab.RowHeightFor(one, RowW), RowBase + FontLineH, 0.6f,
          "★ 单行正文行高 = 57 + 行盒 " + FontLineH.ToString("F3") + " = 77.842"
          + "（行盒 = `Pragati-Regular SDF.asset` 的 m_LineHeight 90 / m_PointSize 95 × 字号 22 —— **独立来源**，不回读量高口）");
        T(tab.RowHeightFor(one, RowW) > tab.RowHeightFor(empty, RowW),
          "（前提）有正文比空正文高 —— 两条期望不是同一个数，上面那两条才不会一起空转");
    }

    // ==================================================================
    //  B 段：战役轨道的空态（两条入口）＋ 切回能建
    // ==================================================================
    /// <summary>B 段入口。`tab` / `trackContent` 由宿主给（`cTab` / `Campaign Track/Viewport/Content`）。</summary>
    public static void RunCampaignTrack(CampaignTab tab, Transform trackContent)
    {
        MenuCheck.Section(_sink, "🆕 B107_W2 · B：空态轨道（`RefreshNodes` + `BuildTrack` 两条入口都不建、跨度也归零）＋ 切回能建");
        T(tab != null, "（前提）拿到 `CampaignTab` 实体（`BuildTrackForTest` 要它）");
        T(trackContent != null, "（前提）拿到 `Campaign Track/Viewport/Content` 这颗节点（数节点要它）");
        if (tab == null || trackContent == null) return;
        var ts = tab.TrackScroll;
        T(ts != null, "（前提）轨道横向滚动区在（`MenuScroll`）");
        if (ts == null) return;

        string other = null;
        for (int i = 0; i < CampaignData.Armies.Length; i++)
            if (!CampaignData.HasContent(CampaignData.Armies[i])) { other = CampaignData.Armies[i]; break; }
        T(!string.IsNullOrEmpty(other),
          "（前提）能找到「本地没有战役内容」的阵营（本地只有 " + CampaignData.ArmiesWithContent + " 有内容）");
        if (string.IsNullOrEmpty(other)) return;

        string home = CampaignData.Selected;   // 进来时的阵营（`finally` 原样还原）

        try
        {
            // ---- ① 有内容那一态：先立基准（下面「空态」才有对照）----
            CampaignData.Select(CampaignData.ArmiesWithContent);
            tab.RefreshNodes();
            int builtHome = CountNodes(trackContent);
            T(builtHome > 0, "（前提）有内容时轨道**建出了节点**（" + builtHome + " 个；全车 `CampaignData.NodeCount` = "
                             + CampaignData.NodeCount + "，我们只建视口内的那几个）");
            T(ts.MaxOffset > 100f, "（前提）有内容时右边**滚得过去**（MaxOffset " + ts.MaxOffset.ToString("F1") + " > 100）");

            // ---- ② 空态 · `RefreshNodes` 那条入口（`B106_W73` 修的）----
            CampaignData.Select(other);
            tab.RefreshNodes();
            C(CountNodes(trackContent), 0, "★ 空态（`RefreshNodes` 这条路口）⇒ **0 个 `CampaignNode_*`**");
            N(ts.MaxOffset, 0f, 0.01f,
              "★ …而且**滚动区零宽**（`ContentX2` 收成 `ContentX1`）—— 否则「空态还能横向滚进一大片空白」");

            // ---- ③ 空态 · `BuildTrack()` 那条入口（本轮修的；`Build()` / `BuildTrackForTest()` 都走它）----
            tab.BuildTrackForTest();
            C(CountNodes(trackContent), 0,
              "★★ 空态 ⇒ `BuildTrack()` 这条入口也**一个节点都不建**"
              + "（原来是照建 47 个 ⇒ 存档里选中一个没有战役的阵营时，整条空轨道照铺、能滚进空白）");
            N(ts.MaxOffset, 0f, 0.01f, "★★ …`BuildTrack()` 这条路也只给**零跨度**");

            // ---- ④ 空态 → 切回有内容阵营：节点与跨度都要回来（W73 那个 `_nodeTf == null` 的坑）----
            CampaignData.Select(home);
            if (!CampaignData.HasContent(CampaignData.Selected)) CampaignData.Select(CampaignData.ArmiesWithContent);
            tab.RefreshNodes();
            int builtBack = CountNodes(trackContent);
            T(builtBack > 0, "★★ 空态 → 切回有内容阵营 ⇒ 轨道**重建得回来**（" + builtBack + " 个节点）"
                             + " —— `ClearTrackContent()` 自己把 `_nodeTf` 置 null，早退之后必须还能再建");
            T(ts.MaxOffset > 100f, "★★ …滚动跨度也跟着回来（MaxOffset " + ts.MaxOffset.ToString("F1") + "）"
                                   + " —— ⛔ 不然重建出来的节点滚不动");

            // ---- ⑤ 有内容时节点数 = 「视口内那几个」（原版全车 = `CampaignData.NodeCount`；我们是 A435 的裁剪口径）----
            int want = 0;
            for (int i = 0; i < CampaignData.NodeCount; i++) if (ts.Intersects(tab.NodeRectForTest(i))) want++;
            C(builtBack, want, "★ 有内容时建出的节点数 = **视口内那几个**（" + want + " / 全车 " + CampaignData.NodeCount
                               + "；空态闸不许误伤有内容的那一趟）");
            T(want > 0 && want <= CampaignData.NodeCount,
              "（前提）视口内节点数落在 1..NodeCount 之间（" + want + "）");
        }
        finally
        {
            CampaignData.Select(home);
            tab.RefreshNodes();
        }
    }

    /// <summary>数 `Content` 底下建出来的战役节点（`CampaignNode_*`；连线是 `NodeLine_*`，不算）。</summary>
    static int CountNodes(Transform content)
    {
        int n = 0;
        var all = content.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name.StartsWith("CampaignNode_", System.StringComparison.Ordinal)) n++;
        return n;
    }
}
