// B103W2_Assert.cs — 执行代理 `B103_W2`（2026-10-30 · 第一百零三会话）：
//   账 `个人资料/成就/排行:346`（`AllianceMemberTab.SearchInput(string)`）那一行 ——
//   联盟窗里那几处「有命中件、但点了只 `Say("打不了字")`、不真输入」的输入框。
//
// ============================ 🔴 账的落点写错了文件（铁律 5）============================
//   · 账行落点写的是 `Shell/AllianceMemberTab.cs:425 / :924`；`资料/完全复刻/判据/AUD04_interactions.tsv`
//     第 **49 / 50** 行写的是 **`Shell/AllianceSearchTab.cs:425 / :924`** —— **正确的是后者**
//     （`AllianceMemberTab.cs` 今天 `:425` 是 `_onTrophyTab` 的 doc、`:924` 是 `NineSoft` 的 doc，**都不是输入框**）。
//   · 原版 `AllianceMemberTab` 类**根本没有输入框**：字段表只有
//     `{generalButton, trophiesButton, generalView, trophiesView, onTrophyTab}`
//     （`out_cpp2il_dev_ilspy/AllianceMemberTab.cs:8-18`）。三处真输入框在**未入盟支**那两个菜单上：
//     搜索 = `JoinAllianceMenu.searchInput`（`…JoinAllianceMenu.cs:233`）、
//     建盟两格 = `CreateAllianceMenu.{nameInput, descriptionInput}`（`…CreateAllianceMenu.cs:63/65`）。
//   · 那三处 **2026-10-30 已由 `B102_ALLY` 接成真输入**（`Shell/AllianceSearchTab.cs:443` 的 `Bind`、
//     `:997` 的 `Create`、`:950` 的消费点），并已有全量断言 `Editor/B102ALLY_Assert.cs`
//     （**已接线**：`Editor/MainMenuScene.cs:18447`）⇒ ⛔ **本文件不重复那一批**。
//   · 本文件补的是**它没覆盖的两块**：
//       ① `AllianceMemberTab`（已入盟支）那**唯一一格输入** = 改盟描述框
//          （原版 `AllianceView.descriptionText`，`out_cpp2il_dev_ilspy/AllianceView.cs:76`；
//          我们 = `TextInputField`，见 `Shell/AllianceMemberTab.cs:1509`）—— 全仓此前**零断言**；
//       ② 那三处今天**仍然是真输入**（防回退）的结构断。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · `AllianceView.OnToggleEditClicked`（`out_cpp2il_dev_ilspy/AllianceView.cs:483-519`）⇒ 进编辑态；
//     `OnEditCancel`（`:521-538`）⇒ 退出；`OnEditSave`（`:540-568`）⇒ `descriptionText.m_Text.Trim()` 后发请求。
//   · 原版描述框那一格是 `EverguildInputField`（`TMP_InputField` 子类，`AllianceView.cs:76`）；
//     本仓没有 uGUI `TMP_InputField` ⇒ 等价物 = `Shell/TextInputField.cs`（见那个文件头）。
//   · `descriptionText.interactable = !blockDescriptionChange && !me.blockDescriptionChange`（服务器字段，本地无值）
//     ⇒ 我们按「**编辑态才出现**」等价实现（`EditChrome.Apply`）。
//
// ============================ 怎么用（宿主）============================
//   宿主里已经有一扇开着的社交窗（`Editor/MainMenuScene.cs` 的 `sw`）：
//
//       B103W2_Assert.Run(sw);
//       pass += B103W2_Assert.Pass; fail += B103W2_Assert.Fail;
//
//   也可以不传（它自己 `SocialWindow.Create(null)` + `Open()` 建一扇，跑完 `DestroySafe` 拆掉）：
//
//       B103W2_Assert.Run();
//       pass += B103W2_Assert.Pass; fail += B103W2_Assert.Fail;
//
//   ⚠️ 它会**临时点亮** `AllianceMemberVariant`（本地恒走不到那一支，同 `Editor/MainMenuScene.cs` 的做法），
//      `finally` 里把 `SetActive` 还原成进函数时那一档；⛔ 不动 `SocialData` 任何表（无夹具）。
//   ⚠️ A 段会把改盟态「进编辑 → 退编辑」走一遍，**结束时停在静止态**（`Edit` 亮、`Confirm`/`Cancel` 灭）。
//
// ============================ 🔴 灭自证（🧨 改坏法写在各条文案里）============================
//   · A1/A3 vs A2b 是一对**结构上互斥**的断言：
//       - 退回 `B102_ALLY` 之前那套「底图 + 只 `Say("打不了字")`」⇒ 没有 `TextInputField` ⇒ **A1 红**；
//       - 把描述框改成**常显**（不进编辑态也 `SetActive(true)`）⇒ **A2b 红**；
//         两者不可能同时绿 ⇒ 「两边一起改回去」也骗不过。
//   · A3：把 `Edit button` 的 `onClick` 里那句 `chrome.Apply(true, true)` 删掉 ⇒ 描述框不亮 ⇒ 红。
//   · A4：把 `TextInputField.Tick` 里吃 `_typed` 那段删掉（= 只收字不回写）⇒ `Value` 恒空 ⇒ 红。
//   · A5b：`EditChrome.Apply` 的 else 支不调 `EndEdit(false)` ⇒ 值留着 ⇒ 红（原版 `OnEditCancel` 是丢弃）。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_W2`：联盟窗输入框 —— 已入盟支描述框（真输入 + 编辑态门）+ 未入盟支三格防回退。</summary>
public static class B103W2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W2] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口

    /// <summary>跑 A / B 两段。<paramref name="sw"/> 可传宿主**已经开着**的那扇社交窗；传 null 就自己建一扇。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B103_W2 · 联盟窗的输入框（账 个人资料/成就/排行:346）");

        SocialWindow own = null;
        AllianceMemberTab member = null;
        bool variantWasActive = false;
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

            var page = sw.PageAlliances;
            Tr(page != null, "（前提）`Alliances` 页在（`sw.PageAlliances`）");
            if (page == null) return;

            member = page.Member;
            var search = page.Search;
            Tr(member != null && search != null,
               "（前提）两支子视图都在：`Member`（已入盟支）/ `Search`（未入盟支）");
            if (member == null || search == null) return;

            variantWasActive = member.gameObject.activeSelf;
            // = `Editor/MainMenuScene.cs` 那段「本地恒走不到、自检里手动点亮」（原版按 `AlliancesManager` 二选一）
            member.gameObject.SetActive(true);

            SectionA(member);
            SectionB(search);
        }
        catch (Exception e)
        {
            Tr(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            try { if (member != null) member.gameObject.SetActive(variantWasActive); }
            catch (Exception e) { Debug.LogWarning("[B103W2] 还原 `AllianceMemberVariant` 的 active 失败：" + e.Message); }
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B103W2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A 已入盟支那一格输入（改盟描述框）

    static void SectionA(AllianceMemberTab m)
    {
        MenuCheck.Section(_sink, "A · 已入盟支那一格真输入（改盟描述框 = 原版 `AllianceView.descriptionText`）");

        var gd = FindChild(m.transform, "GeneralDetails");
        Tr(gd != null, "A0 前提：`GeneralDetails` 在（这一支那一棵，act T 那份几何）");
        if (gd == null) return;

        // ---- A1：那一格是不是**真输入件**
        //   ⚠️ 这里只认「组件在不在」，不认「底图在不在」—— 旧的写法**底图和命中区都有**，缺的正是这一件。
        var fields = m.GetComponentsInChildren<TextInputField>(true);
        int n = fields != null ? fields.Length : 0;
        Tr(n >= 1, "A1 ★ `AllianceMemberTab` 底下有真输入件 `TextInputField`（找到 " + n + " 颗）"
                 + "｜🧨 退回「底图 + 只 `Say(\"打不了字\")`」那套 ⇒ 0 颗 ⇒ 红");
        if (n == 0) return;

        var f = fields[0];
        C(n, 1, "A1b 这一支**恰好一格**输入（描述框；⛔ 建盟那两格在另一支 `AllianceSearchTab` 上）");
        Tr(f.TextLabel != null && f.PlaceholderLabel != null,
           "A1c 正文 / 占位两颗字都在（" + (f.TextLabel != null) + "/" + (f.PlaceholderLabel != null) + "）"
         + "⇒ 打进去的字**看得见**（旧缺口的一半就是「打进去没地方显示」）");
        Tr(f.Hit != null && f.Hit.onClick != null,
           "A1d 命中区在、且接了点击（`WindowButton.onClick` 非空）⇒ 点它 = `BeginEdit`"
         + "｜🧨 命中区改回只 `Say` ⇒ `onClick` 仍非空、但 A3 会红 ⇒ 两条一起才挡得住");

        // ---- A2：编辑态的开门/关门
        //   🔴 **2026-10-30（`B103_F2`）就地订正（铁律 5）**：原写法 `FindChild(gd, "Edit button")` 走的是
        //      **直接子件**查找，而这两颗钮挂在 `GeneralDetails/Content/Config fields/` 下
        //      （`Shell/AllianceMemberTab.cs` 里 `var cf = Node(content, "Config fields", g.Cfg);` +
        //      同文件 `YellowBtn(v, cf, "Edit button", …)` / `YellowBtn(v, cf, "Cancel button", …)`）⇒
        //      原写法恒 null、A2 前提恒假（实测 `_tmp_view/menu.log`「A2 前提 … 实得 [False]」）。
        //      **错因**：把「钮建在 `Config fields` 那一行里」当成了「建在 `GeneralDetails` 的直接子件」。
        //   ⇒ 改走**显式路径**（仍然只走固定几层，⛔ 不用递归全树 —— 递归会撞同名件，见 `FindChild` 的 doc）。
        //   ⚠️ 出厂态是 `Edit` **亮** / `Cancel` **灭**（`EditChrome.Apply(false, idleEdit)` 那一梯子），
        //      但 `FindChild` 用 `GetChild` ⇒ **不活动子件照样找得到**，所以这里不受显隐影响。
        var cfNode = FindChild(FindChild(gd, "Content"), "Config fields");
        Tr(cfNode != null, "A2 前提①：`GeneralDetails/Content/Config fields` 在（两颗钮的父节点）");
        var editHit = HitOf(FindChild(cfNode, "Edit button"));
        var cancelHit = HitOf(FindChild(cfNode, "Cancel button"));
        Tr(editHit != null && cancelHit != null, "A2 前提：`Edit` / `Cancel` 两颗钮的命中区都在（改盟态的开关）"
                                                + "｜🧨 删掉 `Hit` 子件 / 断掉 `onClick` ⇒ 红");
        if (editHit == null || cancelHit == null) return;

        // 宿主前一批断言可能把这一支留在编辑态 ⇒ 先回到静止态（⛔ 别在别人的状态上断言）
        if (!editHit.gameObject.activeInHierarchy) cancelHit.Click();
        Tr(!f.gameObject.activeSelf,
           "A2b 静止态：描述框**收起**（原版只切 `interactable`；我们这一格是另一个控件 ⇒ 按「编辑态才出现」等价实现）"
         + "｜🧨 改成常显 ⇒ 红");

        editHit.Click();
        Tr(f.gameObject.activeSelf,
           "A3 ★ 点 `Edit button` ⇒ 描述框**亮起来**（= 原版 `OnToggleEditClicked` 进编辑态那一档）"
         + "｜🧨 `onClick` 里不调 `chrome.Apply(true, …)` ⇒ 红");

        // ---- A4：真键盘那条路（`onTextInput` 收字 + 每帧泵回写）
        bool typed = TypeInto(f, "we fight at dawn");
        Tr(typed, "A4 ★ 走真输入那条路（`BeginEdit` + `FeedText` + `Tick`）⇒ `Value` == \"we fight at dawn\""
                + "｜🧨 `Tick` 里不吃 `_typed` ⇒ `Value` 恒空 ⇒ 红");
        C(f.Value, "we fight at dawn", "A4b 读回的值就是打进去那一串");

        // ---- A5：退出编辑 ⇒ 收起 + 丢弃未提交的输入
        cancelHit.Click();
        Tr(!f.gameObject.activeSelf, "A5 ★ 点 `Cancel button` ⇒ 描述框**收起**（= 原版 `OnEditCancel`）");
        C(f.Value, "", "A5b ★★ 收起时**丢弃**未提交的输入（`EndEdit(keep:false)`，原版没有本地暂存这条路）"
                     + "｜🧨 `Apply` 的 else 支不调 `EndEdit(false)` ⇒ 值还留着 ⇒ 红");
    }

    // ============================================================ B 账上真正的落点（防回退，⛔ 不重复 B102ALLY）

    static void SectionB(AllianceSearchTab s)
    {
        MenuCheck.Section(_sink, "B · 未入盟支那三格（账 `AUD04:49/50` 的真落点；`B102_ALLY` 已接，这里只防回退）");

        Tr(s.SearchInput is TextInputField,
           "B1 搜索框 `List View/Search Field` = 真输入件（原版 `JoinAllianceMenu.searchInput`）");
        Tr(s.NameInput is TextInputField,
           "B2 建盟 `Name Input` = 真输入件（原版 `CreateAllianceMenu.nameInput`）");
        Tr(s.DescInput is TextInputField,
           "B3 建盟 `Desc Input` = 真输入件（原版 `CreateAllianceMenu.descriptionInput`）");

        var sf = FindChild(FindChild(s.transform, "List View"), "Search Field");
        var sfHit = FindChild(sf, "SearchHit");
        var wb = sfHit != null ? sfHit.GetComponent<WindowButton>() : null;
        Tr(wb != null && wb.onClick != null,
           "B4 搜索框命中区 `SearchHit` 在、且接了点击（原版 `TMP_InputField` 自己吃 `IPointerClickHandler`）");
        // ⚠️ 本文件**刻意不点**这三格：「点一下 ⇒ 进编辑态」与「搜索串真的过滤列表」那两条在
        //    `Editor/B102ALLY_Assert.cs` 的 A1/A3（已接线 `MainMenuScene.cs:18447`）—— 这里再点一次只会抢宿主的页签态。
        //    ⛔ 别把上面四条当成「行为已验证」：它们断的是**形状**（组件在、命中区接了），行为看 B102ALLY。
    }

    // ============================================================ 小工具

    /// <summary>把 <paramref name="s"/> 通过**真输入那条路**打进输入框，并回读核对。
    /// 走 `EndEdit(false)` → `BeginEdit()` → `FeedText()` → `Tick()`
    /// （= 键盘 `onTextInput` 收字 + 每帧泵的等价，同 `B102ALLY_Assert.TypeInto`）。</summary>
    static bool TypeInto(TextInputField f, string s)
    {
        if (f == null) return false;
        f.EndEdit(false);
        f.BeginEdit();
        f.FeedText(s);
        f.Tick();
        return f.Value == s;
    }

    /// <summary>按名找**直接子件**（本文件只走固定几层，⛔ 不用递归全树 —— 递归会撞同名件）。</summary>
    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    /// <summary>取一颗钮下面那颗 `Hit` 的 `WindowButton`（三颗黄钮都是 `v.Hit(n, "Hit", …)` 建的）。</summary>
    static WindowButton HitOf(Transform btnNode)
    {
        var h = FindChild(btnNode, "Hit");
        return h != null ? h.GetComponent<WindowButton>() : null;
    }
}
