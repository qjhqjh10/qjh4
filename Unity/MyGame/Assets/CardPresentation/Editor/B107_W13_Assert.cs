// B107_W13_Assert.cs — 执行代理 `B107_W13`（第一百零七会话）
//
// ============================ 它盯的是什么 ============================
//  A 段 · N91 聊天输入去富文本标签 =
//      原版 `ChatPanel.Start` 给 `TMP_InputField.m_OnValueChanged` 挂一条
//      `TextMessageValidationTools.RemoveRichTextTags(m_Text) → text = 结果`（`out_cpp2il_dev_ilspy/ChatPanel.cs:154-161`；
//      另一份等价 `ValidateInput` 在 `:651-655`）。黑名单 = **18 条**（`TextMessageValidationTools.cs` 静态构造的
//      `new string[18]` 逐槽赋值；`decomp_full/TextMessageValidationTools__RemoveRichTextTags.c` =
//      `CacheString.Clear() → Append(target) → 逐条 Replace(tag, "")`）。
//      🔴 **`<b>` / `<i>` / `<color=…>` 都不在黑名单里**（原版原样保留、交 TMP 渲染）⇒ ⛔ 别把「所有 `<…>`」剥掉。
//      另：`<size=40>` 剥完是 `40>`（逐条精确子串 Replace 的必然结果，原版这样）；⛔ 不「修好」它。
//  B 段 · N93 成员行 `Ranked Rating` 的赛季闸 =
//      原版 `AllianceMemberEntry.Initialize`：`if (member.season == group.currentSeason) value = member.Rating; else value = 0;`
//      （`out_cpp2il_dev_ilspy/AllianceMemberEntry.cs:196-207` · `decomp_full/AllianceMemberEntry__Initialize.c:118-125`），
//      再交 `AllianceRatingDisplay.Initialize(…, disableIfZero: true, …)` ⇒ **0 = 整块关闭**。
//      哪一格吃它：prefab 里 `playerRating` 字段落在 **`Ranked Rating`** 那颗 GO 上（`Alliance Member Entry.prefab`）。
//  C 段 · N88 新消息滑入 =
//      原版 `ChatContentView.Update`：`newMessageReceived` 时对最后一条已建项
//      `DOMoveY(当前y, 0.3f)` + `From(y − 10f)`（`out_cpp2il_dev_ilspy/ChatContentView.cs:96-107`；默认缓动 `Ease.OutQuad`），
//      上一条还在播 ⇒ 先 `Kill(complete:true)`（`:437`）。`ChatContentView.AddMessage` 只在**非缓存新消息**时置位（`:409-454`）。
//  D 段 · N89 15 秒计时刷新 =
//      原版 `updateDelay = 15`（`ChatContentView__.ctor.c:17` 写 `0xf`；`ChatContentView.cs:518`），
//      `Update` 里 `(DateTime.Now − lastUpdate).TotalSeconds > updateDelay ⇒ UpdateTimers()`，
//      逐个可见项 `ChatMessageUI.UpdateTimer()` 按 `TimestampMs` 与 `UtcNow` 的差重算那颗 `Time`
//      （`ChatMessageUI__UpdateTimer.c` · `SupportMethods.GetTimeStringWithSingleDigit(ts, eventTimer:true)`）。
//  E 段 · N92 按频道开窗 =
//      原版 `ChatPanel.Open()`：`FirstOrDefault(tabs, t => t.channel.ChannelType == startingChannel)` → `ChangeTab`
//      （`out_cpp2il_dev_ilspy/ChatPanel.cs:224-462` · 上下文类 `ChatPanel.ChatPanelContext` 在 `:16-24`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · `B107W13_Assert.RunAll(win)` —— 任何手里有 `ChatPanel` 的宿主（现读候选：`Editor/ShellScene.cs`
//    的聊天窗那一节 / `Editor/CollectionScene.cs` 的 `ChatTab` 那一节，局部变量 `chat` / `ctab`）。
//    它**不重建窗、不关窗**，只读 `win.CurrentTabView` 拿到 `ChatTab` 再跑 C/D 段。
//  · 只想单跑：`RunRichText()` / `RunSeason()` / `RunStartingChannelIndex()` 是**纯函数**、不需要窗；
//    `RunSlide(tab)` / `RunTimers(tab)` 要一个已经 `Setup()` 过、至少建出 1 行的 `ChatTab`。
//  · C/D 段会**临时改** `SocialData.ChatMessages` 里一条消息的 `TimestampMs`，`finally` 原样还原；不建持久资产。
//
// ============================ 🔴 灭自证 ============================
//  · A 段的期望串是**照原版行为手写的字面量**（不是回读 `StripRichTextTags`）；另有一条「`<b>` 必须**不**被剥掉」——
//    把黑名单换成「所有 `<…>`」或加进 `<b>` ⇒ 那条红（和「该剥的剥了」不会一起变绿）。
//  · B 段同赛季 / 不同赛季**两条对照**：只把季节闸写成「恒显示」⇒ 第二条红；写成「恒隐藏」⇒ 第一条红。
//  · C 段除了终态，还断**中点**（OutQuad(0.5) = 0.75 ⇒ 偏移 −2.5）与**节点真的动了**（读 `localPosition`，
//    不回读 `SlideOffsetPxNow`）⇒ 只改常量不改算式 / 只改算式不改常量，都至少有一条红。
//  · D 段用**两个不同档**（61s ⇒ `M` 档 1、5s ⇒ `FewSeconds` 档）+ 一次「刷新前后文本真的变了」，
//    ⛔ 不回读 `SocialData.TimeText` 当期望（只借它的词条键，分支与值是本文件自己判的）。
//
// ============================ 判据 / 报告 ============================
//  报告：`资料/完全复刻/报告/B107_W13.md`（⛔ 别在这里抄第二份）
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B107_W13`：N88 滑入 · N89 15s 计时 · N91 去富文本 · N92 起始频道 · N93 赛季闸 · N94 注释 · N127 RowPad 收口。</summary>
public static class B107W13_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107W13] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>一次跑完 A–E 段（需要窗的段在 `win == null` 时只断前提、不假装通过）。</summary>
    public static void RunAll(ChatPanel win)
    {
        RunRichText();
        RunSeason();
        RunStartingChannelIndex();
        var tab = win != null ? win.CurrentTabView as ChatTab : null;
        RunSlide(tab);
        RunTimers(tab);
    }

    // ==================================================================
    //  A 段：N91 去富文本（原版 18 条黑名单，⛔ 不是「所有 <…>」）
    // ==================================================================
    /// <summary>原版黑名单 —— **从反编译静态构造手抄**（独立于实现对它保持零引用）。</summary>
    static readonly string[] OriginalTags =
    {
        "<align=", "<alpha=", "<cspace=", "<font=", "<indent=", "<line-height=",
        "<line-indent=", "<link=", "<margin=", "<mspace=", "<nobr>", "<page>",
        "<pos=", "<size=", "<sprite=", "<style=", "<voffset=", "<width=",
    };

    public static void RunRichText()
    {
        MenuCheck.Section(_sink, "🆕 B107_W13 · A（N91）：聊天输入去富文本 = 原版 18 条标签黑名单");
        T(ChatPanel.RichTextTagCount == OriginalTags.Length,
          "（前提）实现声明的标签条数 = 反编译静态构造的 18 条（现 " + ChatPanel.RichTextTagCount + "）");

        int missing = 0;
        for (int i = 0; i < OriginalTags.Length; i++)
            if (ChatPanel.StripRichTextTags(OriginalTags[i] + "Z") == OriginalTags[i] + "Z") missing++;
        C(missing, 0, "★ 原版那 18 条**逐条**都被剔除（一条都没漏；漏一条这里 +1）");

        C(ChatPanel.StripRichTextTags("<size=40>Hi"), "40>Hi",
          "★ 逐条精确子串 Replace 的原版结果：`<size=40>Hi` → `40>Hi`（⛔ 别「修好」成 `Hi`，原版就是这么剥的）");
        C(ChatPanel.StripRichTextTags("a<nobr>b<page>c"), "abc", "两条整对标签（`<nobr>` / `<page>`）整段消失");
        C(ChatPanel.StripRichTextTags("<b>bold</b>"), "<b>bold</b>",
          "★★ `<b>` **不在**黑名单里 ⇒ 原样保留（把黑名单改成「所有 `<…>`」或加进 `<b>` ⇒ 这条红）");
        C(ChatPanel.StripRichTextTags("<color=#FF0000>x</color>"), "<color=#FF0000>x</color>",
          "★★ `<color=…>` 同样**不**在黑名单里（原版只有 `<alpha=`，没有 `<color=`）");
        C(ChatPanel.StripRichTextTags(null), "", "null ⇒ 空串（原版这里会 NRE；输入框不会给 null ⇒ 我们回空、不崩）");
    }

    // ==================================================================
    //  B 段：N93 成员行 Ranked Rating 的赛季闸
    // ==================================================================
    public static void RunSeason()
    {
        MenuCheck.Section(_sink, "🆕 B107_W13 · B（N93）：`Ranked Rating` 只在 `member.season == group.currentSeason` 时显示");
        var m = new SocialData.Member { RankedRating = "32", Season = 7 };
        int saved = SocialData.CurrentSeason;
        try
        {
            SocialData.CurrentSeason = 7;
            C(AllianceMemberRow.RankedRatingText(m), "32",
              "★ 同赛季 ⇒ 显示 `member.Rating`（我们的显示串 `RankedRating`）");
            SocialData.CurrentSeason = 8;
            T(string.IsNullOrEmpty(AllianceMemberRow.RankedRatingText(m)),
              "★★ 不同赛季 ⇒ 0 ⇒ `AllianceRatingDisplay(disableIfZero:true)` 整块关闭（这里返回 null/空）");
            C<string>(AllianceMemberRow.RankedRatingText(null), null, "（前提）空成员 ⇒ null（不 NRE、也不假装有值）");
            C<string>(AllianceMemberRow.RankedRatingText(new SocialData.Member { Season = SocialData.CurrentSeason }), null,
              "（前提）同赛季但没填值 ⇒ 仍为 null（= 0 ⇒ Rating() 同判 0、整块关闭；可见态与空串相同）");
        }
        finally
        {
            SocialData.CurrentSeason = saved;
        }
    }

    // ==================================================================
    //  C 段：N92 起始频道的「频道名 → 页签下标」一跳
    // ==================================================================
    public static void RunStartingChannelIndex()
    {
        MenuCheck.Section(_sink, "🆕 B107_W13 · C（N92）：`startingChannel` → 页签下标（取不到 ⇒ 0 = Global）");
        C(ChatPanel.TabIndexForChannel("Global"), 0, "Global ⇒ 第 0 页");
        C(ChatPanel.TabIndexForChannel("Alliance"), 1, "Alliance ⇒ 第 1 页（`ChatRoom.Global = 0 / Alliance = 1`）");
        C(ChatPanel.TabIndexForChannel("Nope"), 0, "未知频道 ⇒ 0（原版 `FirstOrDefault` 取不到时不切页）");
        C(ChatPanel.TabIndexForChannel(null), 0, "null（没有 context）⇒ 0 = Global（既有行为）");
    }

    // ==================================================================
    //  D 段：N88 新消息滑入（0.3s / 10px / OutQuad）
    // ==================================================================
    public static void RunSlide(ChatTab tab)
    {
        MenuCheck.Section(_sink, "🆕 B107_W13 · D（N88）：新消息滑入 0.3s · 从 y−10 起 · OutQuad");
        N(ChatTab.SlideDuration, 0.3f, 0.0001f, "（前提）时长 = 原版 `DOMoveY(…, 0.3f)` 的 0.3s");
        N(ChatTab.SlideOffsetPx, 10f, 0.0001f, "（前提）起始下移 = 原版 `From(y − 10f)` 的 10px");
        T(tab != null, "（前提）拿到 `ChatTab` 实体（宿主传 `win.CurrentTabView as ChatTab`）");
        if (tab == null) return;
        var row = tab.LastBuiltRow;
        T(row != null, "（前提）这一页**至少建出过一行**（滑入的目标 = 最后一条已建项）");
        if (row == null) return;

        Vector3 basePos = row.localPosition;
        float pxPerUnit = LayoutSpace.Px(1f);
        tab.BeginSlideForTest(row);
        N(tab.SlideOffsetPxNow, -ChatTab.SlideOffsetPx, 0.01f, "★ 起手：相对基准位 **−10px**");
        N((row.localPosition.y - basePos.y) / pxPerUnit, -ChatTab.SlideOffsetPx, 0.05f,
          "★ …而且**节点真的被挪了** 10 设计 px（读 `localPosition`，不回读 `SlideOffsetPxNow`）");

        tab.TickSlide(ChatTab.SlideDuration * 0.5f);
        N(tab.SlideOffsetPxNow, -2.5f, 0.05f,
          "★ 中点：`Ease.OutQuad(0.5) = 0.75` ⇒ 偏移 −10 × 0.25 = **−2.5px**（只改常量不改缓动 ⇒ 这条红）");

        tab.TickSlide(ChatTab.SlideDuration * 0.5f);
        N(tab.SlideOffsetPxNow, 0f, 0.001f, "★ 到点 ⇒ 偏移归零");
        T(!tab.IsSliding, "★ …状态收尾（`IsSliding == false`）");
        N(row.localPosition.y - basePos.y, 0f, 0.01f, "★ …节点落回基准位（不留残差）");
    }

    // ==================================================================
    //  E 段：N89 15 秒计时刷新（重算那颗 `Time`）
    // ==================================================================
    public static void RunTimers(ChatTab tab)
    {
        MenuCheck.Section(_sink, "🆕 B107_W13 · E（N89）：每 15s 重算相对时间（两个档位各验一次）");
        C(ChatTab.TimerRefreshSeconds, 15, "（前提）周期 = 原版 `updateDelay = 15`");
        T(tab != null, "（前提）拿到 `ChatTab` 实体");
        if (tab == null) return;
        T(tab.BuiltTimeLabels > 0, "（前提）本页记下了至少一颗 `Time` 标签（现 " + tab.BuiltTimeLabels + "）");
        if (tab.BuiltTimeLabels <= 0) return;

        SocialData.ChatMessage m = null;
        Label lb = null;
        int at = -1;
        for (int i = 0; i < tab.BuiltTimeLabels; i++)
            if (tab.BuiltTimeLabelAt(i) != null && tab.BuiltMessageAt(i) != null) { at = i; m = tab.BuiltMessageAt(i); lb = tab.BuiltTimeLabelAt(i); break; }
        T(m != null && lb != null, "（前提）找到一对「有标签、有消息」的行（找不到就无法验刷新）");
        if (m == null || lb == null) return;

        long saved = m.TimestampMs;
        try
        {
            long nowMs = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;

            m.TimestampMs = nowMs - 61000;             // 61 秒前 ⇒ `TimeSpan.Minutes == 1`
            string before = lb.Text;
            int n = tab.RefreshTimeTexts();
            T(n > 0, "★ `RefreshTimeTexts()` 刷到了行（现 " + n + "）");
            C(lb.Text, string.Format(Loc.T("MainMenu/Time/M"), 1),
              "★ 61s ⇒ **分钟档**（`MainMenu/Time/M` 填 1；判据 `SupportMethods.GetTimeStringWithSingleDigit(ts, eventTimer:true)`）");
            T(before != lb.Text || string.IsNullOrEmpty(before), "★ …刷新前后文本真的变了（不是空转）");

            m.TimestampMs = nowMs - 5000;              // 5 秒前 ⇒ `TimeSpan.Minutes < 1`
            tab.RefreshTimeTexts();
            C(lb.Text, Loc.T("MainMenu/Time/FewSeconds"),
              "★ 5s ⇒ **FewSeconds 档**（无参）—— 两个档各自成立 ⇒ 不是「只有一个分支会写」");
        }
        finally
        {
            m.TimestampMs = saved;
            tab.RefreshTimeTexts();                     // 还原成原值（不让夹具残留）
        }
    }
}
