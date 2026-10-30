// B103W4_Assert.cs — 执行代理 `B103_W4`（2026-10-30 · 第一百零三会话）：
//   账 `菜单_外壳.tsv` 的 `CardDetailPopup.BuyOriginalCard()`（**状态 B**）。
//
// ============================ 本件盯的是【落点 / 判据】，不是行为 ============================
//  行为级（点一下 ⇒ 出声、不扣、不改、无模态窗）**已经有人做了**：
//    `CardPresentation/Editor/B102BUY_Assert.cs`（19 条，已接进 `Editor/ShellScene.cs:11023` 的聚合行）。
//  ⛔ **本件不重复那 19 条**（重复断言 = 维护两份、迟早打架）。
//  本件只锁三件 **B102BUY 覆盖不到、而且已经出过事** 的东西：
//    ① **落点锁** —— 账/简报里那条落点写成了 `CardPresentation/Battle/CardDisplayWindow.cs:2527`，
//       而**那棵树里根本没有这颗钮**（AUD04 原文的落点其实是 `Shell/CardDetailPopup.cs`）。
//       真落点见 `CardPresentation/Shell/CardDetailPopup.cs` 的 `OnBuyOriginalCardClicked()`。
//    ② **无本地价格锁** —— 原版价格来自**远端 LiveOps** `ItemsConfig.GetOriginalArtPrice(rarity)`
//       （`ConfigProvider<T>.Initialize(Dictionary<string,string> titleData)` 吃的是 **PlayFab TitleData**，
//       见本会话报告 §①）；本地一个价格都没有 ⇒ ⛔ 不许在方法体里自己造一个价。
//    ③ **自设计标记锁** —— 这段是 `CLAUDE.md` §18·0 允许的「服务器相关」自设计，必须在场。
//
// ============================ 灭自证（哪一处改动会让哪一条红）============================
//  · 把实现搬到 `Battle/CardDisplayWindow.cs`（照账上那条错落点去做）⇒ A1 **红**（同时 A0 红）。
//  · 在方法体里写死一个价格 / 自己算花费 ⇒ A2 **红**。
//  · 删掉 `🔶 自己设计` 那一行 ⇒ A3 **红**。
//  · 在本地白送一张卡 / 扣一笔钱（假买）⇒ A4 **红**（B102BUY 的 A1-5/A1-6 也会红）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs` 的 `ShellScene.Run`（与 `B102BUY_Assert` 同一个 `_sink`）。
//  在**合计行之前**、**同一行追加**（⇒ 行数零变化；⛔ 追加前先看该行第一个 `//`，落在它**之前**）：
//      B103W4_Assert.Run(); _sink.Pass += B103W4_Assert.Pass; _sink.Fail += B103W4_Assert.Fail;
//  🆕 本件**纯源码级**：不建窗、不点、不依赖运行期 ⇒ 任何宿主都能跑，也可以单独跑：
//      `-executeMethod B103W4_Assert.Run`
using System;
using System.IO;
using UnityEngine;

/// <summary>执行代理 `B103_W4`：账 `菜单_外壳.BuyOriginalCard` 的**落点 / 判据锁**（源码级，不建窗、不点）。</summary>
public static class B103W4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>真落点：`CardDetailPopup` 在 **`Shell/`** 下（⛔ 不是 `Battle/CardDisplayWindow.cs`）。</summary>
    const string RealSrcRel = "CardPresentation/Shell/CardDetailPopup.cs";
    /// <summary>账/简报里那条**写错的**落点文件（实读：那棵树里没有 `BuyOriginalCard` 的任何形态）。</summary>
    const string WrongSrcRel = "CardPresentation/Battle/CardDisplayWindow.cs";
    /// <summary>真方法的签名（逐字，用于在源码里定位方法体）。</summary>
    const string MethodSig = "public void OnBuyOriginalCardClicked()";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B103_W4 账 `菜单_外壳.BuyOriginalCard`：落点锁 + 无本地价格锁 + 自设计标记锁");
        RunSource();
        Debug.Log(_sink.P + "B103W4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunSource()
    {
        try
        {
            string realPath = Path.Combine(Application.dataPath, RealSrcRel);
            string wrongPath = Path.Combine(Application.dataPath, WrongSrcRel);

            // ---- A0 前提（硬断言；⛔ 不写成 `if (x != null) Check(...)`）----------------------
            MenuCheck.Section(_sink, "A0 前提：真落点文件读得到、里面真有那个方法");
            bool realOk = File.Exists(realPath);
            Tr(realOk, "A0-0 **前提**：读得到 `" + RealSrcRel + "`（路径 " + realPath + "）"
                     + "—— 读不到时本节**等于没验** ⇒ 当场红");
            if (!realOk) return;

            string realText = File.ReadAllText(realPath);
            int at = realText.IndexOf(MethodSig, StringComparison.Ordinal);
            Tr(at >= 0, "A0-1 **前提**：`" + RealSrcRel + "` 里有 `" + MethodSig + "`"
                      + "（= 原版 `AlternateArtPanel.BuyOriginalCard` 在我们这边的落点入口）");
            if (at < 0) return;

            int braceAt = realText.IndexOf('{', at);
            string body = braceAt < 0 ? "" : BodyOf(realText, braceAt);
            Tr(body.Length > 2, "A0-2 **前提**：截到了方法体（实得 " + body.Length + " 字符）");

            string bodyNoComment = StripLineComments(body);

            // ---- A1 ★ 落点锁（灭自证：照账上那条错落点去做 ⇒ 红）------------------------------
            MenuCheck.Section(_sink, "A1 ★ 落点锁：`Battle/CardDisplayWindow.cs` **不是**这颗钮的家（账上/简报里那条错的）");
            Tr(File.Exists(wrongPath), "A1-0 **前提**：`" + WrongSrcRel + "` 这个文件存在（终局要验的是「它里面没有」）");
            if (File.Exists(wrongPath))
            {
                string wrongText = File.ReadAllText(wrongPath);
                bool wrongHasIt = wrongText.IndexOf("OnBuyOriginalCardClicked", StringComparison.Ordinal) >= 0
                               || wrongText.IndexOf("BuyOriginalCard", StringComparison.Ordinal) >= 0;
                Tr(!wrongHasIt,
                   "A1-1 ★ `" + WrongSrcRel + "` 里**没有** `BuyOriginalCard` / `OnBuyOriginalCardClicked`"
                 + "（那颗钮长在 `Shell/CardDetailPopup.cs` 的异画面板里；账/简报里那条落点是笔误）"
                 + "｜🧨 把实现搬到那棵树 ⇒ 本條红、A0-1 同时红（两向都挡）");
            }

            // ---- A2 ★ 无本地价格锁（灭自证：写死一个价 ⇒ 红）--------------------------------
            MenuCheck.Section(_sink, "A2 ★ 无本地价格锁：方法体里⛔ 不出现价格（原版价格在远端 PlayFab TitleData 的 `ItemsConfig`）");
            // 只看**代码**（先剥 `//` 行注释）—— 注释里允许提到 `ItemsConfig.GetOriginalArtPrice` 作判据说明。
            // ⚠️ 前提：本方法体里没有含 `//` 的字符串字面量（当前满足；改方法体时先看一眼）。
            bool hasPriceWord = bodyNoComment.IndexOf("Price", StringComparison.Ordinal) >= 0
                             || bodyNoComment.IndexOf("price", StringComparison.Ordinal) >= 0;
            Tr(!hasPriceWord,
               "A2-1 ★ 方法体**代码里没有价格**（`Price` / `price` 都不出现）"
             + "（原版 `ItemsConfig.GetOriginalArtPrice(rarity)` 取的是 **PlayFab TitleData** 下发的 "
             + "`unlockOriginalArtPrices`，本地 0 命中 ⇒ 我们自己算一个价 = 造原版没有的经济）"
             + "｜🧨 写死价格 / 自己算花费 ⇒ 红");

            // ---- A3 ★ 自设计标记锁（`CLAUDE.md` §18·0 唯一例外：服务器相关）--------------------
            MenuCheck.Section(_sink, "A3 ★ 自设计标记在场：这段是「服务器相关」的自设计，必须有 `🔶 自己设计`");
            Tr(body.IndexOf("🔶 自己设计", StringComparison.Ordinal) >= 0,
               "A3-1 ★ 方法体（含注释）里有 `🔶 自己设计` 标记"
             + "（`CLAUDE.md` §18 / §18·0：关服后唯一允许自设计的就是服务器相关这一类，必须在代码里标出来）"
             + "｜🧨 删掉标记 ⇒ 红（下一个会话会把它当成原版判据）");

            // ---- A4 ★ 无本地记账（灭自证：假买 ⇒ 红）-----------------------------------------
            MenuCheck.Section(_sink, "A4 ★ 无本地记账：⛔ 不白送卡、⛔ 不扣钱（原版扣费/入库都在服务端回包里）");
            bool hasLocalBook = bodyNoComment.IndexOf("AddOwned(", StringComparison.Ordinal) >= 0
                             || bodyNoComment.IndexOf("Wallet.Grant(", StringComparison.Ordinal) >= 0
                             || bodyNoComment.IndexOf("Wildcards.Take(", StringComparison.Ordinal) >= 0;
            Tr(!hasLocalBook,
               "A4-1 ★ 方法体**没有本地记账**（`AddOwned(` / `Wallet.Grant(` / `Wildcards.Take(` 都不出现）"
             + "（原版 `InventoryManager.Add(cost…)` 的 `cost` 与 `RewardService.Collect(rewards)` 的 `rewards` "
             + "都是**服务器回包**给的 ⇒ 关服后在本地落下任何一笔 = 假经济）"
             + "｜🧨 在本地 +1 一张卡 / −1 一笔钱 ⇒ 红（B102BUY 的 A1-5/A1-6 会同时红）");
        }
        catch (Exception ex)
        {
            Tr(false, "（不静默）源码级检查抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
    }

    // ============================================================ 工具
    /// <summary>从 `{` 开始按花括号配平截出这一层（本方法体只有一层嵌套；⛔ 不做通用词法分析）。
    /// ⚠️ 前提：这一段里没有带花括号的字符串字面量（本方法满足，改方法体时**先看一眼**这条前提）。</summary>
    static string BodyOf(string text, int braceAt)
    {
        int depth = 0;
        for (int i = braceAt; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return text.Substring(braceAt, i - braceAt + 1); }
        }
        return text.Substring(braceAt);        // 没配平（源码被改坏）⇒ 把剩下的全给它：宁可多扫、由调用方红
    }

    /// <summary>剥掉**行注释**（`//` 到行尾）。⚠️ 前提：这段源码的字符串字面量里不出现 `//`
    /// （本方法满足 —— 只有 `CustomErrors/…` 这种单斜杠路径）。</summary>
    static string StripLineComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue;
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }
}
