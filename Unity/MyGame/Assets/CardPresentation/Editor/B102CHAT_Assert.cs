// B102CHAT_Assert.cs — 执行代理 `B102_CHAT`（2026-10-30 · 第一百零二会话）：
//   **聊天窗「输入框 / 发送键」点了只打日志（= 静默失败）** ⇒ 现在输入框接真输入、发送键走**本地回显**。
//
// ============================ 它盯的是什么 ============================
//  `资料/完全复刻/判据/AUD04_interactions.tsv:78` 的缺口行：
//    「⚠️ 聊天（ChatPanel）：输入框（`:281`）/发送键（`:292`）有命中件但点了只打日志、不输入不发送」
//  🟡 口径（用户 2026-09-26）：「**网络聊天暂不做，界面照建**」⇒ 界面早就照建了；
//     但「点了只打日志」属**静默失败**（铁律：不许静默）⇒ 两档：A 档接本地回显（本批做的）、B 档只出声。
//  ⇒ 本批选 **A 档**：本仓**已有**本地聊天链 —— `SocialData.ChatMessages`（数据）→
//     `ChatTab.Rebuild`（按 `m.Channel` 过滤、逐行 `ChatMessageRow.Build`）⇒ 往表里 `Add` 一条再
//     `ChatPanel.RefreshMessages()`，自己那一页**真的多一行**。⛔ 不假造第二条链。
//
// ============================ 判据（⛔ 不是我们的常量） ============================
//  · 原版发送入口 = `ChatPanel.SendMessage(string)`（`ChatPanel.cs:687`）—— 方法体内经
//    `ChatGlobalManager.SendMessage*` 走 Photon（**已关服 ⇒ 判 D**，`账/社交_聊天.tsv` 里那条 D 行）。
//  · 原版输入框那颗 `EverguildInputField`（`assets_full/bundle_menus_assets_all` 的
//    `ChatPanel/Holder/Chat/Enter Text/InputField (TMP)`）：`m_CharacterLimit = 200` · `m_ContentType = 0`
//    （Standard）· `m_LineType = 0`（单行）。普查原文 → `资料/普查产出_0927/聊天窗与挑战弹窗.md:73`。
//  · 原版 `Text Area/Text`（正文那颗 TMP）：字号 28 · Left/Top · 折行 3 · 白 · 文案零宽空格
//    （同文件 `:76`）—— 我们**原来没有这一颗**（只有 `Placeholder`），本批补上。
//  · 原版**回车发送** = `ChatPanel.Update()`（`decomp_full/ChatPanel__Update.c:7-16`）：
//    `GetKeyDown(KeypadEnter 0x10F) || GetKeyDown(Return 0x0D)` 且 **Shift（`0x130`/`0x12F`）没按** ⇒ 走 `TrySendMessage` 同一段。
//    ⚠️ 原版是 **`Update` 里全局收键**（输入框没焦点也发）；我们只在**编辑态**收（见 `ChatSend.SubmitByEnter` 的 doc）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红） ============================
//  · **把发送键改回「只 `Debug.Log`」**（`Shell/ChatPanel.cs` 那句 `MenuDraw.Hit(send, …)` 的 lambda
//    改回日志）⇒ **A1-1 / A1-2 / A1-3 三条同时红**（返回 false、行数不涨、表里也没有这条）。
//  · **把输入框改回「只打日志」**（不 `TextInputField.Bind`）⇒ **A0-2 红**（那颗组件不在）⇒
//    A1-0（注入的字进不去）也红。
//  · **只 `RefreshMessages()` 不 `Add`**（= 假回显）⇒ A1-3（表里末条）红，而 A1-1 若同时被改成
//    「返回 true」也只是自欺 —— ⇒ 所以 A1 同时断**三处**：返回值 · 画出来的行 · 数据表末条，
//    **结构上不可能同时满足又没真回显**。
//  · **把 `ChatSend.Send` 的禁言闸删掉** ⇒ A4-1 / A4-2 红。
//  · **把 `Clear()` 删掉** ⇒ A3-1 / A3-2 红。
//  · **把 `Bind` 的 `onSubmit` 实参删掉**（回车不再发送）⇒ A7-1 / A7-2 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主） ============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs` 的 `ShellScene.Run`（与 `B102AUD04_Assert` 同一个 `_sink`）。
//  在**合计行之前**、**同一行追加**（⇒ 行数零变化，照既有代接线的做法）：
//      B102CHAT_Assert.Run(); _sink.Pass += B102CHAT_Assert.Pass; _sink.Fail += B102CHAT_Assert.Fail;
//  ⚠️ 本件**不需要宿主已开着聊天窗**：自己建探针根 + 调生产建树口 `ChatPanel.Build()`；
//     也**不碰宿主的聊天数据**（进来先快照 `SocialData.ChatMessages`，收尾原样还原）。
//     单跑：`-executeMethod B102CHAT_Assert.Run`。
//  ⚠️ 为什么**不走** `ChatPanel.Create(WindowsManager)`：那条会 `WindowsManager.Get()`（没有现成实例时
//     `EnsureHost` 会顺带认领/建主菜单）⇒ 在别的宿主里是副作用（同 `B102AUD04_Assert` 的取舍）。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_CHAT`：聊天窗输入框接真输入 + 发送键走本地回显。</summary>
public static class B102CHAT_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102CHAT] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 常量（判据侧，⛔ 不读实现）
    /// <summary>原版 `EverguildInputField.m_CharacterLimit`（普查 `:73`）。</summary>
    const int CharLimit = 200;
    /// <summary>注入串（带空格与大小写，防「大小写/空白被吃掉」这类静默差异）。</summary>
    const string Inject = "B102 chat: hello world";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102CHAT 聊天窗：输入框真输入 · 发送键本地回显（原版 `ChatPanel.SendMessage` ⇒ Photon，本地等价）");

        // ---- 夹具独立：进来先快照聊天表，收尾原样还原（⛔ 不污染别的谱系的断言）
        var savedChat = new List<SocialData.ChatMessage>(SocialData.ChatMessages);
        var savedBan = ChatPanel.ChatBanProvider;
        SocialData.ChatMessages.Clear();

        ChatPanel chat = null;
        try
        {
            var go = new GameObject("B102CHAT_ChatProbe", typeof(RectTransform));
            chat = go.AddComponent<ChatPanel>();
            chat.Build();                                   // 生产建树口（⛔ 不复制一份建法）
            if (chat.tabButtons != null) chat.tabButtons.Click(0);   // 第 0 页活着（`RefreshMessages` 只重画 active 那页）

            if (SectionA0_Premise(chat))
            {
                var input = ChatSend.InputOf(chat);
                var content = ContentOf(chat);
                SectionA1_Send(chat, input, content);
                SectionA2_EmptySend(chat, input, content);
                SectionA3_ClearedAfterSend(chat, input);
                SectionA4_Banned(chat, input, content);
                SectionA5_NoSendNoRow(chat, input, content);
                SectionA6_CharLimit(chat, input);
                SectionA7_SubmitByEnter(chat, input, content);
            }
        }
        catch (Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            ChatPanel.ChatBanProvider = savedBan;
            SocialData.ChatMessages.Clear();
            SocialData.ChatMessages.AddRange(savedChat);
            if (chat != null) UnityEngine.Object.DestroyImmediate(chat.gameObject);
        }
        Debug.Log(_sink.P + "B102CHAT 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static Transform Child(Transform p, string path) { return p == null ? null : p.Find(path); }

    /// <summary>当前页那个 `ChatTab`（第 0 页 = `Global`）。</summary>
    static ChatTab TabOf(ChatPanel win)
    {
        return win != null && win.CurrentTabView is ChatTab ? (ChatTab)win.CurrentTabView : null;
    }

    /// <summary>消息行挂的那颗 `Chat Tab/Viewport/Content`。</summary>
    static Transform ContentOf(ChatPanel win)
    {
        var tab = TabOf(win);
        return tab != null ? tab.transform.Find("Viewport/Content") : null;
    }

    /// <summary>把画出来的每一行正文读出来（`ChatMessageRow/<Message>` 那颗 `Label` 的 `Text`）。
    /// ⚠️ 读的是**渲染那棵树**，⛔ 不读 `SocialData`（读数据表就是自证：数据进没进表 ≠ 玩家看不看得见）。</summary>
    static List<string> RowTexts(Transform content)
    {
        var list = new List<string>();
        if (content == null) return list;
        for (int i = 0; i < content.childCount; i++)
        {
            var row = content.GetChild(i);
            if (row.name != "ChatMessageRow") continue;
            var msg = row.Find("Message");
            var lb = msg != null ? msg.GetComponent<Label>() : null;
            list.Add(lb != null ? (lb.Text ?? "") : "<行里没有 Message 标签>");
        }
        return list;
    }

    // ============================================================ A0 前提（硬断言，⛔ 不写成 `if (x != null) Check(...)`）
    static bool SectionA0_Premise(ChatPanel chat)
    {
        MenuCheck.Section(_sink, "A0 前提：探针窗建出来了 · 输入框那颗是真输入件 · 正文/发送键/消息页都在");

        Tr(chat != null, "A0-0 **前提**：`ChatPanel` 组件建出来了（`AddComponent` + `Build()`）");
        if (chat == null) return false;

        var inputNode = Child(chat.transform, ChatSend.InputPath);
        Tr(inputNode != null, "A0-1 **前提**：`Holder/Chat/Enter Text/InputField (TMP)` 在（原版同一路径）");
        var field = ChatSend.InputOf(chat);
        Tr(field != null, "A0-2 ★ **输入框那颗是 `TextInputField`**（= 本仓的可输入件；"
                        + "｜🧨 把 `TextInputField.Bind(...)` 那行删掉（退回「只打日志」）⇒ 本条红）");
        if (field == null) return false;

        var textLb = ChatSend.TextLabelOf(chat);
        Tr(textLb != null, "A0-3 **前提**：正文那颗 `Text` 在（原版 `Text Area/Text`，2026-10-30 新建；"
                         + "没有它 ⇒ 打进字去**没地方显示**）");

        var sendNode = Child(chat.transform, ChatSend.SendPath);
        var sendWb = sendNode != null ? sendNode.GetComponentInChildren<WindowButton>(true) : null;
        Tr(sendWb != null, "A0-4 **前提**：发送键 `Enter Text/Button` 带命中件（`WindowButton`）");

        var tab = TabOf(chat);
        Tr(tab != null, "A0-5 **前提**：第 0 页 = `ChatTab`（`tabButtons.Click(0)` 之后 `CurrentTabView` 是它）");
        var content = ContentOf(chat);
        Tr(content != null, "A0-6 **前提**：消息行挂点 `Chat Tab/Viewport/Content` 在");
        if (tab == null || content == null) return false;

        C(SocialData.ChatMessages.Count, 0, "A0-7 **前提**：夹具把聊天表清空了（本节起点的行数 = 0）");
        return true;
    }

    // ============================================================ A1 ★ 主链：注入 → 发送 → 真的多一行
    static void SectionA1_Send(ChatPanel chat, TextInputField input, Transform content)
    {
        MenuCheck.Section(_sink, "A1 ★ 输入 → 发送 ⇒ 本机那一页**真的多一行**，正文就是注入的那段");

        if (content == null) { Tr(false, "A1 **前提不成立**：`Content` 不在（本节没跑，⛔ 不是绿）"); return; }

        input.BeginEdit();
        input.FeedText(Inject);
        input.Tick();                                        // = `Update` 里那条泵（自检里手调）
        Tr(input.Value == Inject,
           "A1-0 **前提**：注入的字真的进了输入框（Value = 「" + input.Value + "」；"
         + "｜🧨 键盘泵 / `_maxLen` 写坏 ⇒ 红）");

        int before = RowTexts(content).Count;
        bool ok = ChatSend.Send(chat);

        Tr(ok, "A1-1 ★ **发送键返回「发了」**（`ChatSend.Send` = 原版 `ChatPanel.SendMessage` 的本地等价）"
             + "｜🧨 把发送键改回「只 `Debug.Log`」⇒ 红");
        var after = RowTexts(content);
        C(after.Count, before + 1,
          "A1-2 ★ **画出来的行数 +1**（前 " + before + " → 后 " + after.Count + "）"
        + "｜🧨 只出声、不落数据 ⇒ 红");
        var tab1 = TabOf(chat);
        C(tab1 != null ? tab1.BuiltRows : -1, after.Count,
          "A1-2b **前提**：`ChatTab.BuiltRows` 与画出来的行数一致（= " + after.Count + "）—— "
        + "为 0 说明**这一页根本没建行**（视口粗筛没过 / 页没激活），那 A1-2/A1-3 的读数**不可信**（⛔ 不是绿）");
        if (after.Count > 0)
            Tr(after[after.Count - 1] == Inject,
               "A1-3 ★ **最后一行正文 = 注入的那段**（实测「" + after[after.Count - 1] + "」）"
             + "｜🧨 行是从别处冒出来的 / 正文被截断 / 换了条 ⇒ 红");

        // 第三处读数：**数据表末条**（画出来的行 + 数据表 + 返回值，三处一起钉住「真回显」）
        var list = SocialData.ChatMessages;
        Tr(list.Count > 0 && list[list.Count - 1].Text == Inject,
           "A1-4 ★ **数据表末条 = 注入的那段**（`SocialData.ChatMessages` 是 `ChatTab.Rebuild` 的唯一数据源）");
        Tr(list.Count > 0
           && list[list.Count - 1].Mine
           && list[list.Count - 1].Channel == ChatPanel.Channels[0]
           && list[list.Count - 1].Type == SocialData.ChatMessage.TypeMessage,
           "A1-5 末条的三格语义：`Mine = true`（自己发 ⇒ 走 `Player Header`）· `Channel = Global`（当前页）· "
         + "`Type = 50 Message`（原版 `ChatMessageType.Message`，⛔ 别用默认 0 = Practice ⇒ 正文会是空串）");
    }

    // ============================================================ A2 空输入 ⇒ 不发、不长行
    static void SectionA2_EmptySend(ChatPanel chat, TextInputField input, Transform content)
    {
        MenuCheck.Section(_sink, "A2 空输入 ⇒ 发送键不发（原版也不会把空串发出去）");

        int before = content != null ? RowTexts(content).Count : -1;
        input.Clear();
        bool ok = ChatSend.Send(chat);
        Tr(!ok, "A2-1 空输入 ⇒ `Send` 返回 false｜🧨 把空串判据删掉 ⇒ 红");
        if (content != null)
            C(RowTexts(content).Count, before, "A2-2 空输入 ⇒ **行数不变**（空行不该冒出来）");
        else
            Tr(false, "A2-2 **前提不成立**：`Content` 不在（这一条没验到，⛔ 不是绿）");
    }

    // ============================================================ A3 发完清空 + 占位字回来
    static void SectionA3_ClearedAfterSend(ChatPanel chat, TextInputField input)
    {
        MenuCheck.Section(_sink, "A3 发完清空输入框 · 占位字自己亮回来（原版发完清空）");

        input.BeginEdit();
        input.FeedText("one more");
        input.Tick();
        ChatSend.Send(chat);
        C(input.Value, "", "A3-1 发完输入框清空（`Value` = 空串）｜🧨 删掉 `Clear()` ⇒ 红");
        Tr(input.PlaceholderLabel != null && input.PlaceholderLabel.gameObject.activeSelf,
           "A3-2 占位字亮回来了（空值 ⇒ `Refresh` 里那一支把 `Placeholder` 重新 `SetActive(true)`）");
    }

    // ============================================================ A4 禁言 ⇒ 不发（原版 `CheckChatBan` 置灰）
    static void SectionA4_Banned(ChatPanel chat, TextInputField input, Transform content)
    {
        MenuCheck.Section(_sink, "A4 禁言 ⇒ 发送键不发 · 点输入框不进编辑（原版 `CheckChatBan` 两处置灰）");

        ChatPanel.ChatBanProvider = delegate (int channel) { return true; };
        chat.CheckChatBan();

        int before = content != null ? RowTexts(content).Count : -1;
        input.EndEdit(true);
        input.SetValue("banned text");                       // 绕过 `BeginEdit` 的闸，直接测 `Send` 的闸
        bool ok = ChatSend.Send(chat);
        Tr(!ok, "A4-1 禁言 ⇒ `Send` 返回 false｜🧨 删掉 `IsBanned` 那道闸 ⇒ 红");
        if (content != null)
            C(RowTexts(content).Count, before, "A4-2 禁言 ⇒ 行数不变");
        else
            Tr(false, "A4-2 **前提不成立**：`Content` 不在（这一条没验到，⛔ 不是绿）");

        input.Clear();
        ChatSend.Begin(chat);
        Tr(!input.Editing, "A4-3 禁言 ⇒ `ChatSend.Begin` 不让输入框进编辑态｜🧨 删掉 `Begin` 里的禁言闸 ⇒ 红");

        ChatPanel.ChatBanProvider = null;
        chat.CheckChatBan();
        input.Clear();
    }

    // ============================================================ A5 灭自证：光重画不发送 ⇒ 不长行
    static void SectionA5_NoSendNoRow(ChatPanel chat, TextInputField input, Transform content)
    {
        MenuCheck.Section(_sink, "A5 **灭自证**：只 `RefreshMessages()` 不发送 ⇒ 行数不变（行不是重画自己冒出来的）");

        if (content == null) { Tr(false, "A5 **前提不成立**：`Content` 不在（本节没跑，⛔ 不是绿）"); return; }

        int before = RowTexts(content).Count;
        input.SetValue("typed but not sent");
        chat.RefreshMessages();                              // 原版 `ChatTab.CheckMessages` 那一跳
        C(RowTexts(content).Count, before,
          "A5-1 只重画 ⇒ 行数不变（" + before + "）｜🧨 若把「打字/重画」当成发送（例如在 `RefreshMessages` 里补数据）⇒ 红");
        C(input.Value, "typed but not sent",
          "A5-2 没人偷走输入框里的字（⇒ 上面那条不是「因为框被清了才不变」）");
        input.Clear();
    }

    // ============================================================ A6 字数上限 = 原版 200
    static void SectionA6_CharLimit(ChatPanel chat, TextInputField input)
    {
        MenuCheck.Section(_sink, "A6 输入框上限 = 200（原版 `EverguildInputField.m_CharacterLimit`）");

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < CharLimit + 40; i++) sb.Append('x');
        input.BeginEdit();
        input.FeedText(sb.ToString());
        input.Tick();
        C(input.Value.Length, CharLimit,
          "A6-1 喂 " + (CharLimit + 40) + " 个字符 ⇒ 输入框只收 **200**（原版那份 prefab 就是 200）"
        + "｜🧨 把 `ChatSend.CharLimit` 传错（例如 0 = 不限长）⇒ 红");
        input.Clear();
        input.EndEdit(false);
    }

    // ============================================================ A7 回车发送（原版 `ChatPanel.Update` 那条）
    static void SectionA7_SubmitByEnter(ChatPanel chat, TextInputField input, Transform content)
    {
        MenuCheck.Section(_sink, "A7 回车发送（原版 `ChatPanel__Update.c:7-16`：Return / KeypadEnter 且 Shift 没按）");

        if (content == null) { Tr(false, "A7 **前提不成立**：`Content` 不在（本节没跑，⛔ 不是绿）"); return; }

        const string viaEnter = "sent by Enter";
        int before = RowTexts(content).Count;
        input.BeginEdit();
        input.FeedText(viaEnter);
        input.Tick();
        ChatSend.SubmitByEnter(chat);              // = `TextInputField.Tick` 在 Enter 上调的那个回调
        var after = RowTexts(content);
        C(after.Count, before + 1, "A7-1 回车 ⇒ 也真的多一行（原版 `Update` 与发送键是同一段逻辑的两份拷贝）");
        if (after.Count > 0)
            Tr(after[after.Count - 1] == viaEnter,
               "A7-2 回车发出去的就是框里那段（实测「" + after[after.Count - 1] + "」）"
             + "｜🧨 把 `Bind` 的 `onSubmit` 实参删掉 ⇒ 回车只退出编辑、不发送 ⇒ A7-1/A7-2 一起红");
        C(input.Value, "", "A7-3 回车发完同样清空（与原版 `TMPro_TMP_InputField__set_text(…, \"\")` 一致）");
    }
}
