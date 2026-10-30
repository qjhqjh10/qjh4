// B102FRIEND_Assert.cs — 执行代理 `B102_FRIEND`（2026-10-30 · 第一百零二会话）：
//   **好友页的搜索框**（`Friends Tab > Header > Find players panel > Search Field`）原来是「底图 + 占位字 +
//   一个只 `Say("打不了字")` 的命中区」—— 现在它是**真输入框**（`Shell/TextInputField.cs` 的 `Bind` 口），
//   而且**输入内容被原版那两个消费方读到了**（`Add Friend` / `Instant duel`）。
//
// ============================ 判据（⛔ 不是我挑的，本轮现读 `d:/2/tools/`）============================
//   · 原版那一格 = **`TMP_InputField inputFieldUserName`**（`out_cpp2il_dev_ilspy/FriendsTab.cs:58`）。
//   · 消费方①`ClickAddFriend()`：`m_Text.Trim()` → `SupportMethods.IsValidFriendName` →
//       `PlayfabWrapper.GetPlayerIdByName`（服务器）→ `PlayerDataManager.AddFriendToList`（后端）
//       （`decomp_full/FriendsTab__ClickAddFriend.c:29-56`）。
//   · 消费方②`ClickDuelPractice()`：`StartChallengeWithUser(m_Text)` —— **不 Trim、不校验** →
//       `ChallengeManager.OpenStartChallengeWindow(PlayerDuelPopUpData(userName, ""))`
//       （`decomp_full/FriendsTab__ClickDuelPractice.c` ＋ `FriendsTab__StartChallengeWithUser.c`）。
//   · 🔴 **好友列表本身没有搜索过滤**：`FillInFriendsContainer` 全文只做「清空 → SortByName → SortByConnected
//       → 逐条 CreateEntry」，一次都没读那个输入框（`decomp_full/FriendsTab__FillInFriendsContainer.c`）。
//   ⇒ 本文件断的是 **「值进了那两个消费方」**，**不是「列表被过滤了」** —— 后者是原版没有的行为。
//
// ============================ 怎么用（宿主）============================
//   宿主里已经有一扇开着的社交窗（`Editor/MainMenuScene.cs` 的 `sw`）：
//
//       B102FRIEND_Assert.Run(sw);
//       pass += B102FRIEND_Assert.Pass; fail += B102FRIEND_Assert.Fail;
//
//   也可以不传（它自己 `SocialWindow.Create(null)` + `Open()` 建一扇，跑完 `DestroySafe` 拆掉）：
//
//       B102FRIEND_Assert.Run();
//       pass += B102FRIEND_Assert.Pass; fail += B102FRIEND_Assert.Fail;
//
//   ⚠️ 它会**临时改写** `SocialData.Friends`（喂 3 条夹具）—— `finally` 里逐条还原并重画，
//      ⛔ 不会把夹具留给宿主后面的断言（宿主随后那条「`SocialData.Friends.Count == 0`」照旧成立）。
//   ⚠️ 它**不建** `WindowsManager`（只在**已经存在**时才开/关窗）—— 批处理里没有宿主就不会凭空长出一个。
//
// ============================ 🔴 灭自证（🧨 改坏法写在各条文案里）============================
//   · A1c：把 `OnAddFriendClicked` 换回旧的 `Say("加好友要服务器")`（不读搜索框）⇒ `LastAddFriendName` 恒 null ⇒ 红。
//   · A1a：给好友列表**自造一条**客户端过滤（原版没有）⇒ 注入 "Aragorn" 后行数 ≠ 3 ⇒ 红。
//          ⛔ 这一条与 A1a 是**结构上互斥**的：既要「值进了消费方」又要「列表一行都没少」——
//          「只出声」和「加了过滤」这两种改坏法各撞一条，改回去不可能同时绿。
//   · A1d：删掉 `OnAddFriendClicked` 里的 `.Trim()` ⇒ `LastAddFriendName` 带空格 ⇒ 红。
//   · A2b：把命中区的 `onClick` 从 `BeginEdit` 改回 `Say(...)` ⇒ 点完 `Editing` 仍是 false ⇒ 红。
//   · A3b：把 `ClickDuelPractice` 那一路改成先 `Trim`（「顺手加个闸」）⇒ `LastDuelName` ≠ "  Boromir  " ⇒ 红。
//
// ============================ 已知边界（如实写，不假装）============================
//   · **加好友本身要服务器**（`GetPlayerIdByName` + `AddFriendToList`，P3）⇒ 本地只到「名字过了校验 + 出声」为止，
//     ⛔ 不往 `SocialData.Friends` 里塞一条假的（那等于假装好友请求发成功了）。
//   · **立即决斗**那一跳是**真的**（本地有 `ChallengeManager.OpenStartChallengeWindow` → `DuelPopupWindow`），
//     窗里那两颗钮（Skirmish / Classic）各自要服务器，由那扇窗自己出声。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_FRIEND`：好友页搜索框 ——「真输入 + 真消费（Add Friend / Instant duel）」。</summary>
public static class B102FRIEND_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102FRIEND] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口

    /// <summary>跑全部四段。<paramref name="sw"/> 可传宿主**已经开着**的那扇社交窗；传 null 就自己建一扇。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B102_FRIEND · 好友页搜索框（真输入 + 真消费）");

        SocialWindow own = null;
        FriendsTab view = null;
        var backup = new List<SocialData.Friend>(SocialData.Friends);
        try
        {
            if (sw == null)
            {
                own = SocialWindow.Create(null);
                Tr(own != null, "（前提）SocialWindow 建得出来（`Create(null)`，自检同款）");
                if (own != null) own.Open();        // = 生产那条路（`Build` + 页 `Setup`）
                sw = own;
            }
            Tr(sw != null, "（前提）社交窗在");
            if (sw == null) return;

            view = sw.PageFriends;
            Tr(view != null, "（前提）`Friends Tab` 那一页在（`sw.PageFriends`）");
            if (view == null) return;

            SectionA0(view);
            SectionA1(view);
            SectionA2(view);
            SectionA3(view);
        }
        catch (Exception e)
        {
            Tr(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            // 夹具还原（⛔ 不把 3 条假好友留给宿主）
            SocialData.Friends.Clear();
            SocialData.Friends.AddRange(backup);
            try { if (view != null) view.RebuildForTest(); } catch (Exception e) { Debug.LogWarning("[B102FRIEND] 还原后重画失败：" + e.Message); }
            // 万一还挂着一扇消息窗（无效名那一支）就先收掉
            try { var wm = WindowsManager.Instance; if (wm != null) { wm.CloseWindow<DuelPopupWindow>(); wm.HidePopUp(true); } }
            catch (Exception e) { Debug.LogWarning("[B102FRIEND] 收尾关窗失败：" + e.Message); }
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B102FRIEND 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A0 前提（硬断言，⛔ 不用 `if (x != null) Check`）

    static void SectionA0(FriendsTab v)
    {
        MenuCheck.Section(_sink, "A0 前提：搜索框的**命中件**与**输入件**都建出来了，而且版面没被改动");

        // ⚠️ `SocialPage.Root` 就是 `transform`（`Shell/SocialPage.cs:22`）⇒ 页根 = `v.transform`，它**自己**名叫 `Friends Tab`。
        var sf = FindByName(FindByName(v.transform, "Header"), "Find players panel");
        sf = sf != null ? FindByName(sf, "Search Field") : null;
        Tr(sf != null, "A0① `Friends Tab/Header/Find players panel/Search Field` 那颗节点在");
        if (sf == null) return;

        var hitN = FindByName(sf, "SearchFieldHit");
        var wb = hitN != null ? hitN.GetComponent<WindowButton>() : null;
        Tr(wb != null && wb.onClick != null,
           "A0② 命中件 `SearchFieldHit` 在、且接了点击（`WindowButton.onClick` 非空）"
         + "｜🧨 删掉那句 `Hit(...)` ⇒ 红");
        Tr(v.SearchInput is TextInputField,
           "A0③ 输入件 `FriendsTab.SearchInput` 是 `TextInputField`（不是「底图 + Say」那套）"
         + "｜🧨 换回旧写法 ⇒ 红");
        Tr(v.SearchInput != null && v.SearchInput.TextLabel != null && v.SearchInput.PlaceholderLabel != null,
           "A0④ 占位 / 正文两颗字都绑上了（`Bind` 的口：值有地方显示）"
         + "｜🧨 绑不上 ⇒ `Value` 永远读不到 ⇒ A1 红");

        // 🔴 **版面不变的硬前提**（走 `Bind` 而不是 `Create` 的全部理由，破了下游宿主断言会白红）：
        var ph = FindByName(sf, "Placeholder");
        var tx = FindByName(sf, "Text");
        Tr(ph != null && ph.parent == sf,
           "A0⑤ `Placeholder` 仍然是 `Search Field` 的**直接子件**（`Bind` 不 re-parent 节点）"
         + "｜🧨 改用 `Create` ⇒ 提示窗那套 `Text Area` 会把占位字挪成孙节点 ⇒ 这里红、"
         + "`Editor/MainMenuScene.cs` 那三条按直接子件找它的断言也会红");
        Tr(tx != null && tx.parent == sf, "A0⑥ 新加的正文节点 `Text` 也是直接子件（同层，不套壳）");
        Tr(ph != null && tx != null && ph.GetSiblingIndex() < tx.GetSiblingIndex(),
           "A0⑦ `Placeholder` 排在 `Text` **前面** —— 宿主那条 `TextOf(Search Field)`（= `GetComponentInChildren<Label>`"
         + " 取**第一个**）读到的仍是占位字｜🧨 把正文插到占位字前面 ⇒ 宿主 `CheckText` 变成读正文（空串）⇒ 红");
    }

    // ============================================================ A1 ★ 搜索串真的进了消费方，而且列表**没有**被过滤

    static void SectionA1(FriendsTab v)
    {
        MenuCheck.Section(_sink, "A1 ★ 注入文本 ⇒ `Add Friend` 那一跳**收到了**它（且列表**没有**被过滤 —— 原版就没有这条路）");

        // 🔴 **先把这一页点亮**（出厂 act F —— `Friends Tab` 的 `m_IsActive = 0`，默认停在 Alliances 页）：
        //    行是**按视口可见性**建的（`BuildRows` 里那道 `MenuDraw.VisibleAbove` 守卫），页关着时行数没有意义。
        bool wasActive = v.gameObject.activeSelf;
        if (!wasActive) v.gameObject.SetActive(true);
        try { SectionA1Body(v); }
        finally { if (!wasActive) { v.gameObject.SetActive(false); v.RebuildForTest(); } }   // 状态还原（含早退那两支）
    }

    static void SectionA1Body(FriendsTab v)
    {
        // 夹具：3 条好友（名字里没有 "Aragorn" ⇒ 它能过 `IsValidFriendName`）
        SocialData.Friends.Clear();
        SocialData.Friends.Add(new SocialData.Friend { Name = "Legolas", Online = true, Status = 3 });
        SocialData.Friends.Add(new SocialData.Friend { Name = "Gimli", Online = false });
        SocialData.Friends.Add(new SocialData.Friend { Name = "Boromir", Online = false });
        v.RebuildForTest();

        var list = FindByName(FindByName(v.transform, "Friends List"), "Friends Container");
        list = list != null ? FindByName(FindByName(list, "Viewport"), "Content") : null;
        Tr(list != null, "A1 前提：`Friends Container>Viewport>Content` 行容器在（数它里面的 `Friend Info Item`）");
        if (list == null) return;
        C(CountRows(list), 3, "A1a 前提：3 条夹具 ⇒ 建出 3 行");

        Tr(TypeInto(v.SearchInput, "Aragorn"), "A1b ★ 往搜索框真的打进了 \"Aragorn\"（`BeginEdit`+`FeedText`+`Tick` 那条真路）"
                                            + "｜🧨 只建底图不打字 ⇒ `Value` 为空 ⇒ 下面那条红");

        // 🔴 **这一条是「原版没有客户端过滤」的钉子**（⛔ 不是「过滤没生效」）：
        //    `FillInFriendsContainer` 全文一次没读那个输入框 ⇒ 打字后列表**一行都不该少**。
        C(CountRows(list), 3, "A1a ★ 打了搜索串之后好友列表**一行都没少**（= 原版 `FillInFriendsContainer` 从不读那个输入框）"
                            + "｜🧨 自造一条客户端过滤 ⇒ 这里变 1 行 ⇒ 红（那是原版没有的行为）");

        var addHit = FindByName(FindByName(FindByName(v.transform, "Header"),
                                           "Find players panel"), "Add Friend Button");
        addHit = addHit != null ? FindByName(addHit, "Hit") : null;
        var addWb = addHit != null ? addHit.GetComponent<WindowButton>() : null;
        Tr(addWb != null && addWb.onClick != null, "A1c 前提：`Add Friend Button` 的命中区在、且接了点击");
        if (addWb == null) return;

        addWb.Click();
        C(v.LastAddFriendName, "Aragorn",
          "A1c ★★ `Add Friend` 那一跳**读到了**搜索框那段文本（= 原版 `ClickAddFriend` 的 `targetUser`）"
        + "｜🧨 换回旧的 `Say(\"加好友要服务器\")`（不读输入框）⇒ 这里恒 null ⇒ 红");
        Tr(!string.IsNullOrEmpty(v.LastAddFriendNotice) && v.LastAddFriendNotice.Contains("服务器"),
           "A1d ★ 并且给出了**明确的离线提示**（不是静默）：\"" + (v.LastAddFriendNotice ?? "<null>") + "\""
         + "｜判据 = 原版这一跳 `PlayfabWrapper.GetPlayerIdByName` 要服务器（已关服）");

        // `Trim()`（原版 `m_Text.Trim()` 那句）
        Tr(TypeInto(v.SearchInput, "  Aragorn  "), "A1e 带首尾空格的串注进去了");
        addWb.Click();
        C(v.LastAddFriendName, "Aragorn",
          "A1e ★ 串先 `Trim`（原版 `FriendsTab__ClickAddFriend.c:29-32` 的 `System_String__Trim` 那一句）"
        + "｜🧨 删掉 `OnAddFriendClicked` 里的 `.Trim()` ⇒ 这里带空格 ⇒ 红");

        // 自己在搜索框里打了自己 / 打了已经是好友的名字 ⇒ 原版 `IsValidFriendName` 判假
        Tr(TypeInto(v.SearchInput, "Gimli"), "A1f 把**已有好友**的名字打进去");
        addWb.Click();
        C(v.LastAddFriendNotice, Loc.T("Demo/FriendsMenu/InvalidName"),
          "A1f ★ 好友表里已有同名 ⇒ 走原版 `IsValidFriendName` 的第三支 + 那句 "
        + "`ShowPopUp(\"Demo/FriendsMenu/InvalidName\")`｜🧨 把 `IsValidFriendName` 的第三条删掉 ⇒ 变成「要服务器」那一支 ⇒ 红");
        ClosePopUpsForTest();

        // 空串 ⇒ 同一支（原版 `string.IsNullOrEmpty` 那一句）
        TypeInto(v.SearchInput, "");
        TypeInto(v.SearchInput, "   ");
        addWb.Click();
        C(v.LastAddFriendNotice, Loc.T("Demo/FriendsMenu/InvalidName"),
          "A1g ★ 只有空白的串 `Trim` 后为空 ⇒ 同样走「名字无效」那一支（原版 `IsNullOrEmpty` 那句）");
        ClosePopUpsForTest();

        // 收尾：串清空，别留给后面的段
        TypeInto(v.SearchInput, "");
    }

    // ============================================================ A2 灭自证：命中区真的在「开始编辑」

    static void SectionA2(FriendsTab v)
    {
        MenuCheck.Section(_sink, "A2 灭自证：点搜索框 ⇒ 真的进入编辑态（换回「只 Say」写法必红）");

        var f = v.SearchInput;
        Tr(f != null, "A2 前提：搜索框在");
        if (f == null) return;

        f.EndEdit(false);
        Tr(!f.Editing, "A2a 前提：点之前**不在**编辑态（否则下一条等于没验）");

        var sf = FindByName(FindByName(v.transform, "Header"), "Find players panel");
        sf = sf != null ? FindByName(sf, "Search Field") : null;
        var hitN = sf != null ? FindByName(sf, "SearchFieldHit") : null;
        var wb = hitN != null ? hitN.GetComponent<WindowButton>() : null;
        Tr(wb != null && wb.onClick != null, "A2b 前提：搜索框命中件接了点击");
        if (wb == null) return;

        wb.Click();
        Tr(f.Editing, "A2b ★ 点一下 ⇒ **进入编辑态**（`TextInputField.Editing` = true），键盘那一路挂上了"
                    + "｜🧨 把命中区的 onClick 从 `BeginEdit` 改回 `Say(\"打不了字\")` ⇒ 仍是 false ⇒ 红");

        f.EndEdit(false);
        Tr(!f.Editing, "A2c 退出编辑态（`EndEdit(false)`）⇒ 正文不再带光标");
        C(f.Value, "", "A2c 丢弃后值是空串（`EndEdit(keep:false)` 的语义）");

        // 纯规则那一半也直接咬一口（原版 `SupportMethods.IsValidFriendName` 的三个否定支）
        Tr(!FriendsTab.IsValidFriendName(null) && !FriendsTab.IsValidFriendName(""),
           "A2d `IsValidFriendName`：null / 空 ⇒ false（原版第一条）");
        Tr(!FriendsTab.IsValidFriendName(ProfileData.PlayerName),
           "A2e `IsValidFriendName`：「我自己」的名字 ⇒ false（原版第二条；比较 = `OrdinalIgnoreCase`）");
        Tr(!FriendsTab.IsValidFriendName("gimli"),
           "A2f `IsValidFriendName`：好友表里已有 `Gimli` ⇒ \"gimli\" 也 false（第三支**忽略大小写**）");
        Tr(FriendsTab.IsValidFriendName("Aragorn"), "A2g `IsValidFriendName`：陌生名 ⇒ true");
    }

    // ============================================================ A3 立即决斗那一颗：原样（不 Trim）交给挑战窗

    static void SectionA3(FriendsTab v)
    {
        MenuCheck.Section(_sink, "A3 ★ `Instant duel` 读同一格（原版 `ClickDuelPractice`：**不 Trim**）→ 挑战窗");

        var f = v.SearchInput;
        var duel = FindByName(FindByName(v.transform, "Header"), "Find players panel");
        duel = duel != null ? FindByName(duel, "Instant duel Button") : null;
        var hitN = duel != null ? FindByName(duel, "Hit") : null;
        var wb = hitN != null ? hitN.GetComponent<WindowButton>() : null;
        Tr(f != null && wb != null && wb.onClick != null, "A3 前提：搜索框 + `Instant duel Button` 命中区都在、且接了点击");
        if (f == null || wb == null) return;

        var wmBefore = WindowsManager.Instance;      // ⚠️ 只用 `Instance`（不 `Get()`）—— 批处理里**不凭空造宿主**
        TypeInto(f, "  Boromir  ");
        wb.Click();
        C(v.LastDuelName, "  Boromir  ",
          "A3a ★★ 传给挑战窗的名字**原样**（首尾空格都在）—— 原版 `ClickDuelPractice` 就是 `StartChallengeWithUser(m_Text)`、"
        + "**不 Trim** ⛔ 别「顺手」加一道闸"
        + "｜🧨 在那一路加 `.Trim()` ⇒ 这里变 \"Boromir\" ⇒ 红");

        var popup = wmBefore != null ? wmBefore.GetOpenWindow<DuelPopupWindow>() : null;
        Tr(wmBefore == null || popup != null,
           "A3b ★ 挑战窗真开出来了（`ChallengeManager.OpenStartChallengeWindow` → `WindowsManager.OpenWindow`）"
         + "｜环境里有 `WindowsManager` 时；没有宿主的批处理环境这一条按「不出声地走过」判过 —— 但 A3a 仍成立");
        if (popup != null)
        {
            wmBefore.CloseWindow<DuelPopupWindow>();
            Tr(wmBefore.GetOpenWindow<DuelPopupWindow>() == null, "A3c 收尾：那扇挑战窗关掉了（不把窗留给宿主）");
        }

        TypeInto(f, "");
    }

    // ============================================================ 小工具

    /// <summary>把 <paramref name="s"/> 通过**真输入那条路**打进输入框，并回读核对。
    /// 走 `EndEdit(false)` → `BeginEdit()` → `FeedText()` → `Tick()`（= 键盘 `onTextInput` 收字 + 每帧泵的等价）。</summary>
    static bool TypeInto(TextInputField f, string s)
    {
        if (f == null) return false;
        f.EndEdit(false);
        f.BeginEdit();
        f.FeedText(s);
        f.Tick();
        return f.Value == s;
    }

    /// <summary>数 `Content` 底下有几个 `Friend Info Item`（= 建出几行）。</summary>
    static int CountRows(Transform content)
    {
        if (content == null) return -1;
        int n = 0;
        for (int i = 0; i < content.childCount; i++)
            if (content.GetChild(i).name == "Friend Info Item") n++;
        return n;
    }

    /// <summary>无效名那一支会弹一扇消息窗 ⇒ 收掉它（没有宿主就什么都不做）。</summary>
    static void ClosePopUpsForTest()
    {
        var wm = WindowsManager.Instance;
        if (wm == null) return;
        wm.HidePopUp(true);
    }

    /// <summary>按名找**直接子件**（本文件只找固定层数的节点，⛔ 不用递归全树）。</summary>
    static Transform FindByName(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }
}
