// B102ALLY_Assert.cs — 执行代理 `B102_ALLY`（2026-10-30）：
//   **联盟窗里三处「有命中件、点了只出声、不真输入」** —— 现在都是真输入框（`Shell/TextInputField.cs`），
//   而且**输入内容被原本的消费方读到**（搜索串 ⇒ 过滤 `Open Alliances`；Name/Desc ⇒ `Continue` 那一跳的请求参数）。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · 普查 = `资料/完全复刻/判据/AUD04_interactions.tsv` 第 **49 / 50** 行：
//       49 联盟 · 搜索联盟框 `Search Field`（`Shell/AllianceSearchTab.cs`）——「当前只出声」；
//       50 联盟 · 建盟输入框（Name/Desc）——「当前只出声」。
//   · 原版真值（本轮现读 `decomp_full/`）：
//       ① 搜索框 = `TMP_InputField`（`JoinAllianceMenu__FillOpenAlliances.c:29-32` 读 `searchInput+0x210` 的
//          `m_Text` → `String_Trim`）；过滤链 = `…b__23_1.c`（`MemberCount > 0`）→
//          `…__DisplayClass23_0___FillOpenAlliances_b__0.c`（`if (0 < searchText.Length)`）→
//          `…b__2.c`（`String_Contains(group.name, searchText, 5)`，**5 = `OrdinalIgnoreCase`**）→ `Take(200)`。
//       ② 建盟两格 = `CreateAllianceMenu` 的 `TMP_InputField`（Name / Desc），`Continue` 时把值组进请求发服务器。
//   本文件**只读上面这些行为**（对外可见的语义），⛔ 不读实现里的私有常量。
//
// ============================ 怎么用（宿主）============================
//   最省事（宿主里已经有一扇开着的 `SocialWindow`，`Editor/MainMenuScene.cs` 的 `sw` 就是）：
//
//       B102ALLY_Assert.Run(sw);
//       pass += B102ALLY_Assert.Pass; fail += B102ALLY_Assert.Fail;
//
//   也可以不传（它自己 `SocialWindow.Create(null)` + `Open()` 建一扇，跑完 `DestroySafe` 拆掉）：
//
//       B102ALLY_Assert.Run();
//       pass += B102ALLY_Assert.Pass; fail += B102ALLY_Assert.Fail;
//
//   ⚠️ 它会**临时改写** `SocialData.OpenAlliances`（喂 3 条夹具）—— `finally` 里**逐条还原**并重画，
//      ⛔ 不会把夹具留给宿主后面的断言。宿主若在它之后断「OpenAlliances 恒空」，照旧成立。
//
// ============================ 🔴 灭自证（🧨 改坏法写在各条文案里）============================
//   · A1：把搜索框换回旧的 `Hit(...) ⇒ Say("打不了字")`（不建 `TextInputField`）⇒ A0 红 + A1 恒 3 行 ⇒ 红；
//         只把 `OpenAllianceRows()` 里新增的那道 `Name.IndexOf` 删掉 ⇒ 注入 "alpha" 后仍是 3 行 ⇒ 红。
//   · A2：把 `Continue` 的 onClick 改回原来那句 `Say(...)`（不读 `_nameInput/_descInput`）⇒
//         `LastCreateName/Desc` 停在 null ⇒ 红。
//   · A3：把搜索框命中区的 `onClick` 从 `BeginEdit` 改回 `Say` ⇒ 点完 `Editing` 仍为 false ⇒ 红。
//
// ============================ 已知边界（如实写，不假装）============================
//   · **建盟请求本身要服务器**（原版 `CreateAllianceMenu` → 服务端建 `Group`；P3「玩家账户/云后端」那一类）
//     ⇒ 本地**只到「收到参数 + 出声」**为止，⛔ 不造本地假链。所以 A2 断的是「消费方**收到了**那段文本」，
//     不是「盟真的建出来了」。
//   · 搜索同理：原版先用同一个串问服务器（`GetGroupList`），本机把「服务器侧筛」与「回调里的 `Name.Contains`」
//     合成**本地一道过滤**（可观测结果一致）。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_ALLY`：联盟窗三处输入框（搜索 / 建盟 Name / 建盟 Desc）——「真输入 + 真消费」。</summary>
public static class B102ALLY_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102ALLY] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口

    /// <summary>跑全部四段。<paramref name="sw"/> 可传宿主**已经开着**的那扇社交窗；传 null 就自己建一扇。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B102_ALLY · 联盟窗三处输入框（AUD04 第 49/50 行）");

        SocialWindow own = null;
        AllianceSearchTab view = null;
        var backup = new List<SocialData.AllianceListing>(SocialData.OpenAlliances);
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

            view = sw.PageAlliances != null ? sw.PageAlliances.Search : null;
            Tr(view != null, "（前提）`Alliances` 页的 `AllianceSearchTab` 在（`sw.PageAlliances.Search`）");
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
            // 夹具还原（⛔ 不把 3 条假联盟留给宿主）
            SocialData.OpenAlliances.Clear();
            SocialData.OpenAlliances.AddRange(backup);
            try { if (view != null) view.RebuildForTest(); } catch (Exception e) { Debug.LogWarning("[B102ALLY] 还原后重画失败：" + e.Message); }
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B102ALLY 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A0 前提（硬断言，⛔ 不用 `if (x != null) Check`）

    static void SectionA0(AllianceSearchTab v)
    {
        MenuCheck.Section(_sink, "A0 前提：三处输入件**都建出来了**、**都是 `TextInputField`**");

        // 三条**各自独立**地断（写成一个 `&&` 就分不清是谁缺了）
        bool findOk = v.SearchInput is TextInputField;
        Tr(findOk, "A0① `List View/Search Field` 那颗是 `TextInputField`（不是「底图 + Say」那套）"
                 + "｜🧨 换回旧写法 ⇒ 红");
        bool nameOk = v.NameInput is TextInputField;
        Tr(nameOk, "A0② 建盟 `Name Input` 那颗是 `TextInputField`｜🧨 换回旧写法 ⇒ 红");
        bool descOk = v.DescInput is TextInputField;
        Tr(descOk, "A0③ 建盟 `Desc Input` 那颗是 `TextInputField`｜🧨 换回旧写法 ⇒ 红");

        // 光「有组件」不够 —— 打字要看得见、点得动。⚠️ 两处的**接法不同**，所以分口径断：
        //   · 搜索框 = `TextInputField.Bind`（版面/占位字/命中区是 `BuildListView` 原来那几句画的）
        //     ⇒ 看它的两颗字 + 宿主那颗 `SearchHit`；
        //   · 建盟两格 = `TextInputField.Create`（它自己把三件建出来）⇒ 看组件自己的 `TextLabel/PlaceholderLabel/Hit`。

        var sfNode = FindByName(FindByName(v.transform, "List View"), "Search Field");
        var sfHit = sfNode != null ? FindByName(sfNode, "SearchHit") : null;
        var sfWb = sfHit != null ? sfHit.GetComponent<WindowButton>() : null;
        Tr(v.SearchInput != null && v.SearchInput.TextLabel != null && v.SearchInput.PlaceholderLabel != null,
           "A0④ `Search Field`：占位 / 正文两颗字都绑上了（`Bind` 的口）"
         + "｜🧨 绑不上 ⇒ `Value` 永远读不到 ⇒ A1 红");
        Tr(sfWb != null && sfWb.onClick != null,
           "A0⑤ `Search Field`：命中区 `SearchHit` 在、且接了点击（`WindowButton.onClick` 非空）");

        for (int i = 1; i <= 2; i++)
        {
            var f = i == 1 ? v.NameInput : v.DescInput;
            string who = i == 1 ? "Name Input" : "Desc Input";
            if (f == null) { Tr(false, "A0⑥ " + who + "：输入件为 null ⇒ 后面两条无从谈起"); continue; }
            Tr(f.TextLabel != null && f.PlaceholderLabel != null && f.Hit != null,
               "A0⑥ " + who + "：正文 / 占位 / 命中区三件都建出来了"
               + "（" + (f.TextLabel != null) + "/" + (f.PlaceholderLabel != null) + "/" + (f.Hit != null) + "）");
            var wb = f.Hit != null ? f.Hit.GetComponent<WindowButton>() : null;
            Tr(wb != null && wb.onClick != null, "A0⑦ " + who + "：命中区接了点击（`WindowButton.onClick` 非空）");
        }

        // 建盟那两格必须挂在 `Create Alliance View` 底下（⛔ 别挂到列表支去）
        var cv = v.CreateAllianceView;
        Tr(cv != null && v.NameInput != null && v.NameInput.transform.IsChildOf(cv.transform),
           "A0⑧ `Name Input` 真挂在 `Create Alliance View` 底下（同 `Desc Input` 那一支）");
        Tr(cv != null && v.DescInput != null && v.DescInput.transform.IsChildOf(cv.transform),
           "A0⑨ `Desc Input` 真挂在 `Create Alliance View` 底下");
    }

    // ============================================================ A1 ★ 搜索串真的过滤列表

    static void SectionA1(AllianceSearchTab v)
    {
        MenuCheck.Section(_sink, "A1 ★ 搜索框注入文本 ⇒ `Open Alliances` 真的按它过滤");

        v.ShowJoin();       // 保住前提：`List View` 那一支是亮的（宿主可能停在 Create 支）
        // 夹具：3 条，名字里都带 'a'（用来验子串），只有一条带 "alpha"（用来验命中）
        SocialData.OpenAlliances.Clear();
        SocialData.OpenAlliances.Add(new SocialData.AllianceListing { Name = "Alpha Knights", Region = "Global", Members = 10, MemberMax = 20, Rating = "" });
        SocialData.OpenAlliances.Add(new SocialData.AllianceListing { Name = "Beta Legion",   Region = "Global", Members = 8,  MemberMax = 20, Rating = "" });
        SocialData.OpenAlliances.Add(new SocialData.AllianceListing { Name = "Gamma Wolves",  Region = "Global", Members = 6,  MemberMax = 20, Rating = "" });

        var list = v.OpenListForTest;
        Tr(list != null, "A1 前提：`Open Alliances > Viewport > List` 行容器在（数它里面的 `Entry`）");
        if (list == null) return;

        // ---- 0 空串 ⇒ 不过滤（原版 `if (0 < searchText.Length)` 那一支）
        Tr(TypeInto(v.SearchInput, ""), "A1a 搜索框能回到空值（`EndEdit(false)` + 重新编辑）");
        v.RebuildForTest();
        C(CountRows(list), 3, "A1a 搜索串 **空** ⇒ 三条全在（原版空串那一支**不过滤**）");

        // ---- 1 子串 + 大小写不敏感（原版 `String_Contains(..., 5)` = OrdinalIgnoreCase）
        Tr(TypeInto(v.SearchInput, "alpha"), "A1b ★ 往搜索框真的打进了 \"alpha\"（`BeginEdit`+`FeedText`+`Tick` 那条真路）"
                                          + "｜🧨 只建底图不打字 ⇒ `Value` 为空 ⇒ 红");
        v.RebuildForTest();
        C(CountRows(list), 1, "A1b ★ 只有 `Alpha Knights` 留下（**列表真的被过滤了**，不是只出声）"
                            + "｜🧨 删掉 `OpenAllianceRows` 里那道 `Name.IndexOf` ⇒ 仍是 3 ⇒ 红");

        Tr(TypeInto(v.SearchInput, "ALPHA"), "A1c 大写串也能注进去");
        v.RebuildForTest();
        C(CountRows(list), 1, "A1c ★ **大小写不敏感**（原版 `String.Contains(…, 5)` = `OrdinalIgnoreCase`）："
                            + "\"ALPHA\" 命中 `Alpha Knights`");

        // ---- 2 `Trim()`（原版取数前先 `String_Trim`）
        Tr(TypeInto(v.SearchInput, "  Beta  "), "A1d 带首尾空格的串注进去了");
        v.RebuildForTest();
        C(CountRows(list), 1, "A1d ★ 串先 `Trim` 再比（原版 `FillOpenAlliances.c:29-32`）："
                            + "\"  Beta  \" 命中 `Beta Legion` 一条");

        // ---- 3 子串命中多条（'a' 三条都有）
        Tr(TypeInto(v.SearchInput, "a"), "A1e 单字母串注进去了");
        v.RebuildForTest();
        C(CountRows(list), 3, "A1e 子串 \"a\" ⇒ 三条都在（验证的是**子串**匹配，不是前缀）");

        // ---- 4 没有命中 ⇒ 0 行
        Tr(TypeInto(v.SearchInput, "zzz"), "A1f 打一个不存在的串");
        v.RebuildForTest();
        C(CountRows(list), 0, "A1f ★ 无命中 ⇒ **0 行**（不是「过滤没生效所以全留」）");

        // ---- 5 走**搜索圆钮**那条真链：`GoHit` → `WithLoading(RebuildOpenAlliances)`
        Tr(TypeInto(v.SearchInput, "gamma"), "A1g 打 \"gamma\" 准备走那颗圆钮");
        // 🔴 2026-10-30：`FindByName` 只找**直接子件**（见本文件末尾那个 helper）⇒ 必须逐层下钻：
        //   `Root > List View > Generic Round Button Variant > GoHit`
        //   （`Shell/AllianceSearchTab.cs`：`_listView = Node(Root, "List View", …)` → `go = Node(_listView, "Generic Round Button Variant", …)`
        //    → `Hit(go, "GoHit", …)`）。改回 `FindByName(v.transform, "GoHit")` ⇒ 恒 null（那条红就是它）。
        var goHit = FindByName(FindByName(FindByName(v.transform, "List View"), "Generic Round Button Variant"), "GoHit");
        var goWb = goHit != null ? goHit.GetComponent<WindowButton>() : null;
        Tr(goWb != null && goWb.onClick != null, "A1g 前提：搜索圆钮 `GoHit` 在、且接了点击");
        if (goWb != null)
        {
            goWb.Click();       // = 原版 `FillOpenAlliances`（清空重填）那条路
            C(CountRows(list), 1, "A1g ★ 点搜索圆钮 ⇒ 列表按当前搜索串重填（`Gamma Wolves` 一条）"
                                + "｜🧨 把 `GoHit` 的 onClick 改回原来那句只 `Say` 的写法 ⇒ 列表不重填 ⇒ 红");
        }

        // 收尾：搜索串清空（别把串留给 A2/A3）
        TypeInto(v.SearchInput, "");
        v.RebuildForTest();
    }

    // ============================================================ A2 ★ Name / Desc 被消费方读到

    static void SectionA2(AllianceSearchTab v)
    {
        MenuCheck.Section(_sink, "A2 ★ 建盟 Name / Desc 注入 ⇒ `Continue` 那一跳**收到了**那段文本");

        v.ShowCreate();     // 把 `Create Alliance View` 点亮（那颗 `Continue` 在它底下）
        var cv = v.CreateAllianceView;
        Tr(cv != null && cv.activeSelf, "A2 前提：`Create Alliance View` 已亮（`ShowCreate()`）");
        if (cv == null) return;

        Tr(TypeInto(v.NameInput, "My Test Guild"),
           "A2a ★ 往 `Name Input` 真打进了 \"My Test Guild\"｜🧨 换回旧写法 ⇒ `Value` 为空 ⇒ 红");
        Tr(TypeInto(v.DescInput, "we fight at dawn"),
           "A2b ★ 往 `Desc Input` 真打进了 \"we fight at dawn\"");
        C(v.NameInput != null ? v.NameInput.Value : null, "My Test Guild", "A2b 两格的值都在（Name）");
        C(v.DescInput != null ? v.DescInput.Value : null, "we fight at dawn", "A2b 两格的值都在（Desc）");

        var pb = FindByName(cv.transform, "Price Display Button");
        var hit = pb != null ? FindByName(pb, "Hit") : null;
        var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
        Tr(wb != null && wb.onClick != null, "A2 前提：`Continue`（`Price Display Button`）的命中区在、且接了点击");
        if (wb == null) return;

        v.ResetLastCreateForTest();     // 先清掉可能存在的旧值（本类只给自检用）
        wb.Click();

        C(v.LastCreateName, "My Test Guild",
          "A2c ★★ `Continue` 那一跳**读到了** Name 那段文本（= 原版要发出去的第一格请求参数）"
        + "｜🧨 把 onClick 改回原来那句只 `Say(\"要服务器\")` 的写法 ⇒ 这里恒 null ⇒ 红");
        C(v.LastCreateDesc, "we fight at dawn",
          "A2d ★★ ……以及 Desc 那段文本（两格都真的接上了消费方）");
        C(v.LastCreateName, v.NameInput != null ? v.NameInput.Value : null,
          "A2e 收到的那一格 = 输入框**当下**的值（不是我们预置的默认值、也不是上一次的残值）");
    }

    // ============================================================ A3 灭自证：命中区真的在「开始编辑」

    static void SectionA3(AllianceSearchTab v)
    {
        MenuCheck.Section(_sink, "A3 灭自证：点命中区 ⇒ 真的进入编辑态（换回「只 Say」写法必红）");

        v.ShowJoin();       // A2 把视图切到了 Create 支 ⇒ 先切回来（搜索框在 `List View` 底下）
        var f = v.SearchInput;
        Tr(f != null, "A3 前提：搜索框在");
        if (f == null) return;

        f.EndEdit(false);       // 先退出编辑态（幂等）
        Tr(!f.Editing, "A3a 前提：点之前**不在**编辑态（否则下一条等于没验）");

        // ⚠️ 搜索框走 `Bind`（组件自己不建命中区）⇒ 命中区是宿主那颗 `SearchHit`，⛔ 别读 `f.Hit`
        var sfNode = FindByName(FindByName(v.transform, "List View"), "Search Field");
        var hitN = sfNode != null ? FindByName(sfNode, "SearchHit") : null;
        var wb = hitN != null ? hitN.GetComponent<WindowButton>() : null;
        Tr(wb != null && wb.onClick != null, "A3b 前提：搜索框命中区 `SearchHit` 接了点击");
        if (wb == null) return;

        wb.Click();
        Tr(f.Editing, "A3b ★ 点一下 ⇒ **进入编辑态**（`TextInputField.Editing` = true），键盘那一路挂上了"
                    + "｜🧨 把命中区的 onClick 从 `BeginEdit` 改回 `Say(\"打不了字\")` ⇒ 仍是 false ⇒ 红");

        f.EndEdit(false);
        Tr(!f.Editing, "A3c 退出编辑态（`EndEdit(false)`）⇒ 值回到默认空串、正文不再带光标");
        C(f.Value, "", "A3c 丢弃后值是空串（`EndEdit(keep:false)` 的语义）");
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

    /// <summary>数 `List` 底下有几个 `Entry`（= 过滤之后剩几条；原版行 prefab 的节点名就是 `Entry`）。</summary>
    static int CountRows(Transform list)
    {
        if (list == null) return -1;
        int n = 0;
        for (int i = 0; i < list.childCount; i++)
            if (list.GetChild(i).name == "Entry") n++;
        return n;
    }

    /// <summary>按名找**直接子件**（本文件只找三层以内的固定节点，⛔ 不用递归全树）。</summary>
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
