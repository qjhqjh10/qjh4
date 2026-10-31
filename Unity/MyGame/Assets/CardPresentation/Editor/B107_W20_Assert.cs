// B107_W20_Assert.cs — 执行代理 `B107_W30`（断言接线 · 第一百零七会话 · 2026-10-31）
//
// 盯的是 `报告/B107_W20.md` 交下来的两段：
//   · A 段（**N131**）`Collected Badge` 是 `Image → Text (TMP)` 两层（不是空节点）。
//       判据 = `bundle_menus_assets_all` 的 104 份 `GameObject/Collected Badge*.json` 现读（见 `Shell/ItemDrawer.cs`
//       文件尾那个 partial 的注释块）—— 根 RT 有 1 个 `Image` 子件；`Image` 组件 `m_Enabled = 0`；
//       `Text (TMP)` 的 `m_text = "Claimed"` · `m_fontColor` 黑 · `m_fontSize 72` · autosize **18/72/36** ·
//       `H=2 center / V=512 middle` · `m_TextWrappingMode 0` · **无 `Localize`**。
//   · B 段（**N130**）换语言后四窗（`RewardWindow` / `DailyRewardPopup` / `DailyStreakPopup` / `InboxWindow`）
//       文本都跟着变；`RewardsWindow` 是 **0 绑定 + 整窗重建**那一档（判据 → `报告/B107_W20.md` §五）。
//
// ============================ 怎么接（⚠️ 本代理只动一条接线）============================
//   `Editor/RewardsScene.cs` 的 `RunCore()` 尾部（`RunCampaignRewardLocalization(wm2);` 那一行，**同一物理行**）：
//       B107_W20_Assert.RunAll(root, wm2); _sink.Pass += B107_W20_Assert.Pass; _sink.Fail += B107_W20_Assert.Fail;
//   ⚠️ **各窗都在本件里自建自毁**（不借宿主那几具）—— 宿主的两扇窗在 `wm2.CloseAllWindows()` / 后续
//      `OpenWindow(...)` 的 `ReapClosedWindows` 之后**已被销毁**（`RewardsScene.cs:9077` 那条注释记的就是这个坑，
//      样板 = `B102_D9`）。自建一具的代价只有一次建树，换掉的是「拿到一具尸体就抛 `MissingReferenceException`」。
//
// ============================ 🔴 灭自证（每条都写进了断言文案）============================
//   A：① 删掉 `ToggleClaimedWarning` 里那句 `BuildClaimedLayers(...)` ⇒ 三条 `!= null` 红；
//      ② 把 `ItemDrawerView.ClaimedLiteral` 改一个字 ⇒ 文本那条红；
//      ③ 前置「出厂三层都不存在」挡「一开始就静态建好 ⇒ 断言永远真」。
//   B：① 退绑一颗（`BindText(null)`）后换语言它**必须停在旧语言**，同窗另一颗必须跟着变
//          ⇒ 防「`RelocalizeAll` 被改成什么也不做」仍绿；
//      ② `RewardsWindow` 那条断的是「官方中文列计数 → 0 / 官方英文列计数 ↑」
//          ⇒ 把覆写改成 `{ }`（既不重取也不重建）过不了。
//   ⚠️ 期望值全部来自**官方 13 语言表**（`Resources/official_loc_13lang.json`，走 `Loc.Lang13Of`），
//      ⛔ 不读被测实现里的常量（那是自证）。
//
// ============================ 判据 / 报告 ============================
//   报告：`资料/完全复刻/报告/B107_W30.md`（⛔ 别在这里抄第二份）
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B107_W30`：N131（`Collected Badge` 两层）＋ N130（换语言后四窗跟着变 / `RewardsWindow` 整窗重建）。</summary>
public static class B107_W20_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107W20] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口（宿主只调这一个）

    /// <summary>宿主入口：A 段 + B 段依次跑，任一段抛异常都不掐掉另一段（红线：不许静默失败）。</summary>
    public static void RunAll(Transform root, WindowsManager wm)
    {
        try { RunBadge(root); }
        catch (Exception e) { T(false, "「N131 徽章」抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { RunLanguage(wm); }
        catch (Exception e) { T(false, "「N130 换语言」抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B107_W20 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · N131（Collected Badge 两层）

    /// <summary>N131：`Collected Badge` 的根 + `Image` + `Text (TMP)` 三层（懒建、文本 `Claimed`、折行 0、字号 18–72）。</summary>
    public static void RunBadge(Transform root)
    {
        MenuCheck.Section(_sink, "B107_W20 · N131：`Collected Badge` 三层（根 → Image → Text (TMP)）");
        T(root != null, "（前提）宿主给了可挂的根节点（它底下**没有** `ViewportClip` ⇒ `MenuDraw.Text` 才会真建节点）");
        if (root == null) return;

        Transform holder = null;
        try
        {
            holder = MenuDraw.Node(root, "B107W20_badge_holder", new PxRect(0f, 0f, 200f, 260f));
            T(holder != null, "（前提）夹具容器建出来了");
            if (holder == null) return;

            var all = PrebuiltDecks.All;
            T(all != null && all.Count > 0, "（前提）`PrebuiltDecks.All` 里有预组卡组（夹具用真抽屉，⛔ 不拿空数据建 `CardView`）");
            if (all == null || all.Count == 0) return;
            PrebuiltDecks.Deck deck = null;
            for (int i = 0; i < all.Count; i++) if (all[i] != null) { deck = all[i]; break; }
            T(deck != null, "（前提）取到一副非空预组");
            if (deck == null) return;

            var box = new PxRect(0f, 0f, 200f, 260f);
            var spec = new ItemSpec
            {
                Id = deck.deckId, Kind = ItemKind.Generic, Type = "PrebuiltDeck",
                Deck = deck, WithCardback = false, Label = deck.DisplayName,
            };
            var style = ItemDrawerStyle.Default(10, 11, 12);
            var v = ItemDrawer.DrawView(holder, box, spec, 1, DrawerOverride.Default, style);
            T(v != null, "（前提）抽屉画出来了（画不出来 ⇒ 下面的徽章无从谈起）");
            if (v == null) return;

            // ③ 前置：出厂徽章三层都不存在（懒建 ⇒ 证明下面读到的是**真建出来的**，不是静态默认）
            T(v.ClaimedNode == null && v.ClaimedImageLayer == null && v.ClaimedTextLayer == null,
              "（前提）出厂徽章三层都不存在（`ItemDrawerView.ToggleClaimedWarning(true)` 才懒建）"
            + " —— 否则下面几条可能断的是「一开始就静态建好」的树，断言永远真");

            v.ToggleClaimedWarning(true);
            T(v.ClaimedNode != null && v.ClaimedNode.name == "Collected Badge",
              "N131 徽章根节点 = `Collected Badge`（原版那 104 份 prefab 的根名）");
            T(v.ClaimedImageLayer != null && v.ClaimedImageLayer.name == "Image",
              "N131 `Image` 层（= 原版 `Collected Badge/Image`；原版组件 `m_Enabled = 0` ⇒ 我们只建节点、不建 quad）"
            + "｜🧨 删掉 `ToggleClaimedWarning` 里那句 `BuildClaimedLayers(...)` ⇒ 本条红");
            T(v.ClaimedTextLayer != null && v.ClaimedTextLayer.name == "Text (TMP)",
              "N131 `Text (TMP)` 层（= 原版 `Collected Badge/Image/Text (TMP)`）");

            if (v.ClaimedTextLayer != null)
            {
                C(v.ClaimedTextLayer.Text, ItemDrawerView.ClaimedLiteral,
                  "N131 文本 = `" + ItemDrawerView.ClaimedLiteral + "`（原版该颗**无 `Localize`** ⇒ 任何语言都印英文）"
                + "｜🧨 把 `ClaimedLiteral` 改一个字 ⇒ 红");
                C(v.ClaimedTextLayer.WrappingMode, 0, "N131 折行 = 0（原版 `m_TextWrappingMode = 0`）");
                T(v.ClaimedTextLayer.FontPxNow >= 17.5f && v.ClaimedTextLayer.FontPxNow <= 72.5f,
                  "N131 自适应字号收敛在 18–72px（原版 `m_fontSizeMin 18 / Max 72 / Base 36`；实得 "
                + v.ClaimedTextLayer.FontPxNow.ToString("F2") + "）");
            }

            // 阳性对照：不靠句柄、按名字查树（防止「句柄字段对了、树里却没有」）
            var byName = v.ClaimedNode != null ? v.ClaimedNode.Find("Image") : null;
            T(byName != null && byName.Find("Text (TMP)") != null,
              "N131 不靠句柄、**按名字**也查得到两层（`Collected Badge/Image/Text (TMP)`）");
            T(v.ClaimedTextLayer != null && v.ClaimedImageLayer != null
              && v.ClaimedTextLayer.transform.parent == v.ClaimedImageLayer
              && v.ClaimedImageLayer.parent == v.ClaimedNode,
              "N131 父链 = `Collected Badge/Image/Text (TMP)`（原版那三层的父子关系）");

            v.ToggleClaimedWarning(false);
            T(!v.ClaimedVisible, "N131 关掉之后整颗隐藏（原版 `ToggleClaimedWarning(false)` = `SetActive(false)`）");
        }
        finally
        {
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder.gameObject);
        }
    }

    // ============================================================ B · N130（换语言）

    /// <summary>N130：四窗（每窗自建自毁）换语言文本跟着变 + `RewardsWindow` 整窗重建。</summary>
    public static void RunLanguage(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B107_W20 · N130：切语言后四窗文本都变（先断 en≠zh 前提）＋ `RewardsWindow` 整窗重建");
        T(wm != null, "（前提）拿到 `WindowsManager`（四窗都要它建）");
        if (wm == null) return;

        var prevLang = Loc.Current;
        bool prevPersist = Loc.PersistOverride;
        Loc.PersistOverride = true;              // 自检不许写 `PlayerPrefs`（同 `B103A03_Assert`）
        try
        {
            BoundWindowRoundTrip("RewardWindow",
                delegate { var w = RewardWindow.Create(wm); if (w != null) w.Open(); return w; },
                new[]
                {
                    new ProbeDef("Text Get Reward", "MainMenu/RewardWidnow/GetRewardsTitle"),
                    new ProbeDef("Disclaimer Text", "MainMenu/RewardWindow/PremiumItems"),
                });

            BoundWindowRoundTrip("DailyRewardPopup",
                delegate { var w = DailyRewardPopup.Create(wm); if (w != null) w.Open(); return w; },
                new[]
                {
                    new ProbeDef("Claimed Tex", "Rewards Menu/Claimed"),
                    new ProbeDef("EverguildTextMeshPro", "Missions/MoreRewardsIn"),
                });

            BoundWindowRoundTrip("DailyStreakPopup",
                delegate { var w = DailyStreakPopup.Create(wm); if (w != null) w.Open(); return w; },
                new[]
                {
                    new ProbeDef("Current Streak", "Rewards Menu/DailyStreak/CurrentStreak"),
                    new ProbeDef("Info", "Rewards Menu/DailyStreak/Help"),
                });

            BoundWindowRoundTrip("InboxWindow",
                delegate { var w = InboxWindow.Create(wm); if (w != null) w.Open(); return w; },
                new[]
                {
                    new ProbeDef("Content/Title", "MainMenu/Settings/Inbox/InboxTab"),
                    new ProbeDef("No News Warning", "MainMenu/Inbox/NoNews"),
                });

            RewardsWindowRebuild(wm);
        }
        finally
        {
            Loc.SetLanguage(prevLang);           // 还原玩家语档（会再广播一次，无副作用）
            Loc.PersistOverride = prevPersist;
        }
    }

    // ============================================================ 逐窗骨架

    /// <summary>一条「窗内某颗文字 = 某条官方词条」的探针。</summary>
    sealed class ProbeDef
    {
        /// <summary>相对窗根的节点路径；单段时等同「唯一节点名」（先按路径走，走不到再按名字递归找）。</summary>
        public readonly string Path;
        /// <summary>官方 13 语言表的词条键。</summary>
        public readonly string Term;
        public string En, Zh;
        public Label Lb;
        public ProbeDef(string path, string term) { Path = path; Term = term; }

        public Label Locate(Transform root)
        {
            var t = Walk(root, Path);
            if (t == null) t = MenuCheck.FindChild(root, Path);
            return LabelOf(t);
        }
    }

    /// <summary>「绑过的标签跟着语言走」那一档（`Label.BindText` = 原版 I2 `Localize` 的等价物）。</summary>
    static void BoundWindowRoundTrip(string winName, Func<Component> make, ProbeDef[] defs)
    {
        Component win = null;
        try
        {
            Loc.SetLanguage(AvailableLanguages.Chinese);     // 显式选中文档（⛔ 不是「出厂默认」—— `Loc.Default` 是按系统语言解析的）⇒ 窗按中文建
            win = make();
            T(win != null, "（前提）" + winName + " 建出来了（为 null ⇒ 本窗整段等于没验）");
            if (win == null) return;

            // ---- 前提：每条词条的 en / zh 都非空且不同（两档同字 ⇒ 「跟着变」分不出换没换）----
            int ok = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                string en = Loc.Lang13Of(defs[i].Term, "en"), zh = Loc.Lang13Of(defs[i].Term, "zh");
                bool good = !string.IsNullOrEmpty(en) && !string.IsNullOrEmpty(zh) && en != zh;
                defs[i].En = en; defs[i].Zh = zh;
                if (good) ok++;
                else T(false, "（前提）`" + defs[i].Term + "` 官方 13 语言表 en/zh 都非空且不同（en=[" + en + "] zh=[" + zh + "]）");
            }
            T(ok == defs.Length, "（前提）" + winName + " 的 " + defs.Length + " 条词条 en/zh 全在且两档不同");
            if (ok != defs.Length) return;

            // ---- 取节点 + 中文档值 ----
            for (int i = 0; i < defs.Length; i++)
            {
                defs[i].Lb = defs[i].Locate(win.transform);
                T(defs[i].Lb != null, "（前提）" + winName + " 的 `" + defs[i].Path + "` 上找得到 `Label`");
                if (defs[i].Lb == null) return;
                C(defs[i].Lb.Text, defs[i].Zh,
                  "N130 " + winName + " 中文档 `" + defs[i].Path + "` = 官方 zh 列「" + defs[i].Zh + "」（`" + defs[i].Term + "`）");
            }

            // ---- 中 → 英 ----
            T(Loc.SetLanguage(AvailableLanguages.English),
              "（前提）`SetLanguage(English)` 真改了值（早退 ⇒ 下面「跟着变」是空断）");
            for (int i = 0; i < defs.Length; i++)
                C(defs[i].Lb.Text, defs[i].En,
                  "N130 " + winName + " 换语言（中→英）：`" + defs[i].Path + "` 跟着变成官方 en 列「" + defs[i].En + "」"
                + "｜🧨 删掉 `Shell/" + winName + ".cs` 里那条 `BindText(...)` ⇒ 停在中文 ⇒ 红");

            // ---- 灭自证：退绑第一颗 ⇒ 它停在英文；同窗第二颗照样跟着变回中文 ----
            if (defs.Length >= 2)
            {
                defs[0].Lb.BindText(null);
                T(!defs[0].Lb.HasTextProvider,
                  "（灭自证·前提）`" + defs[0].Path + "` 退绑后不再自称跟着语言走（`HasTextProvider == false`）");
                T(Loc.SetLanguage(AvailableLanguages.Chinese), "（前提）`SetLanguage(Chinese)` 真改了值");
                C(defs[0].Lb.Text, defs[0].En,
                  "N130 " + winName + " **灭自证**：退绑之后换语言，`" + defs[0].Path + "` **停在英文**「" + defs[0].En + "」"
                + " —— 这正是「删掉那条 `BindText`」的后果 ⇒ 上面那几条「跟着变」靠的是绑定，不是别的东西在改字");
                C(defs[1].Lb.Text, defs[1].Zh,
                  "N130 …对照：同一跳里**绑着**的第二颗 `" + defs[1].Path + "` 仍跟着变回官方中文「" + defs[1].Zh + "」");
            }
            else
            {
                Loc.SetLanguage(AvailableLanguages.Chinese);
            }
        }
        catch (Exception e)
        {
            T(false, "「" + winName + " 换语言」抛异常（本窗后续条目没跑完）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null) UnityEngine.Object.DestroyImmediate(win.gameObject);
            Loc.SetLanguage(AvailableLanguages.Chinese);     // 下一窗按中文建
        }
    }

    /// <summary>`RewardsWindow` 那一档：**0 绑定 + 整窗重建**（判据 → `报告/B107_W20.md` §五）。</summary>
    static void RewardsWindowRebuild(WindowsManager wm)
    {
        RewardsWindow win = null;
        try
        {
            Loc.SetLanguage(AvailableLanguages.Chinese);
            win = RewardsWindow.Create(wm);
            T(win != null, "（前提）`RewardsWindow` 建出来了");
            if (win == null) return;
            win.Open();

            C(CountBound(win.transform), 0,
              "N130 `RewardsWindow` **0 绑定**（有意：正文由 `MissionsTab`/`CampaignTab`/`ForgeTab`/`DailyStreakTab` 四页各自建、四页都没绑 "
            + "⇒ 只绑左栏 3 颗会「页签变、正文停旧语言」= 半绑比不绑更糟；判据 → `报告/B107_W20.md` §五）");

            // 观察点：正文三页里有好几颗 `Loc.T(...)` 建出来的字；**按候选顺序取第一棵真有字的**
            //   （`MissionsTab` 的 `Collect` 钮 / 周常标题 / 连登页签 —— 宿主在跑到这里之前会临时改 `DailyData`，
            //    所以**不写死一条**；取不到就当场红，⛔ 不静默跳过）。
            string[] candidates = { "MainMenu/General/Collect", "Missions/Title/WeeklyChallenge", "Rewards Menu/DailyStreak/Title" };
            string en = null, zh = null;
            int zhBefore = 0, enBefore = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                string e = Loc.Lang13Of(candidates[i], "en"), z = Loc.Lang13Of(candidates[i], "zh");
                if (string.IsNullOrEmpty(e) || string.IsNullOrEmpty(z) || e == z) continue;
                int zc = CountTexts(win.transform, z);
                if (zc <= 0) continue;
                en = e; zh = z; zhBefore = zc; enBefore = CountTexts(win.transform, e);
                T(true, "（前提）观察点 = `" + candidates[i] + "`（官方 zh「" + z + "」在中文档窗内有 " + zc + " 颗）");
                break;
            }
            T(en != null, "（前提）三个候选词条里至少有一个在中文档窗内**真的建出了字**"
                       + "（`MainMenu/General/Collect` / `Missions/Title/WeeklyChallenge` / `Rewards Menu/DailyStreak/Title`）"
                       + " —— 都没有 ⇒ 下面「重建后换字」失去观察点 ⇒ 本条只能判红（⛔ 不静默跳过）");
            if (en == null) return;

            T(Loc.SetLanguage(AvailableLanguages.English), "（前提）`SetLanguage(English)` 真改了值");
            C(CountTexts(win.transform, zh), zhBefore,
              "N130 `RewardsWindow` **0 绑定 ⇒ 换语言那一跳一个字都不改**（还没重建；提前变字说明有人偷偷绑了半棵树）");

            // 生产同一条路：`SettingsWindow.ChooseLanguage` 对每扇开着的窗调 `OnLanguageChanged()`
            //   ⇒ 覆写里 `RelocalizeUnder == 0` ⇒ `Open()` 整窗重建。
            win.OnLanguageChanged();
            int enAfter = CountTexts(win.transform, en);
            T(enAfter > enBefore,
              "N130 `RewardsWindow.OnLanguageChanged()` ⇒ **整窗重建**后出现官方英文「" + en + "」（" + enBefore + " → " + enAfter + " 颗）"
            + "｜🧨 把覆写改成 `{ }`（既不重取也不重建）⇒ 仍是中文 ⇒ 红");
            C(CountTexts(win.transform, zh), 0,
              "N130 …重建后**一颗官方中文都不剩**（中文列「" + zh + "」计数 = 0）—— 就地改字/什么都没做都过不了这一条");
        }
        catch (Exception e)
        {
            T(false, "「RewardsWindow 换语言重建」抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null) UnityEngine.Object.DestroyImmediate(win.gameObject);
        }
    }

    // ============================================================ 小工具

    /// <summary>按 `A/B/C` 逐段走直接子件（⛔ 不是递归）。</summary>
    static Transform Walk(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;
        var t = root;
        var parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (t == null) return null;
            t = t.Find(parts[i]);
        }
        return t;
    }

    /// <summary>取节点上（或其子树里）第一颗 `Label`。</summary>
    static Label LabelOf(Transform t)
    {
        if (t == null) return null;
        var self = t.GetComponent<Label>();
        if (self != null) return self;
        var all = t.GetComponentsInChildren<Label>(true);
        return all.Length > 0 ? all[0] : null;
    }

    /// <summary>子树里登记了本地化来源的 `Label` 颗数（= 原版「挂了几颗 `Localize`」）。</summary>
    static int CountBound(Transform root)
    {
        if (root == null) return 0;
        int n = 0;
        var all = root.GetComponentsInChildren<Label>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].HasTextProvider) n++;
        return n;
    }

    /// <summary>子树里文字**逐字等于** <paramref name="text"/> 的 `Label` 颗数。</summary>
    static int CountTexts(Transform root, string text)
    {
        if (root == null || string.IsNullOrEmpty(text)) return 0;
        int n = 0;
        var all = root.GetComponentsInChildren<Label>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && string.Equals(all[i].Text, text, StringComparison.Ordinal)) n++;
        return n;
    }
}
