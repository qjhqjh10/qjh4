// B102BUY_Assert.cs — 执行代理 `B102_BUY`（2026-10-30 · 第一百零二会话）：
//   卡牌详情窗的 **「Buy Original Card」钮有命中件、但点了只打 Log、没有行为**（`AUD04` 普查第 95 行）。
//
// ============================ 它盯的是什么 ============================
//  落点（AUD04 原文）＝ `Shell/CardDetailPopup.cs` 的 `OnBuyOriginalCardClicked()`（调度台简报里把文件名
//  写成 `Battle/CardDisplayWindow.cs` 是笔误 —— 那棵树里**根本没有**这颗钮，见报告 §1）。
//
// ============================ 判档：B 档（依赖服务器，原版判据实读）============================
//  `d:/2/tools/decomp_full/AlternateArtPanel__BuyOriginalCard.c` 逐段读过：
//    ① `WindowsManager.ShowPopUp(<确认弹窗>)`；
//    ② `PlayfabWrapper__GenericCloudScriptHandler<object>(…)` = **PlayFab CloudScript（服务端）**；
//    ③ 成功回调 `AlternateArtPanel___BuyOriginalCard_b__24_0.c`：`InventoryManager.Add(…)` +
//       `RewardService.Collect(…)`（扣费 / 入库**都在服务端**）；
//    ④ 失败回调 `AlternateArtPanel_<>c__<BuyOriginalCard>b__24_1.c`：`HidePopUp` + `CustomDebug.LogError`。
//  价格那一跳 = `ItemsConfig.GetOriginalArtPrice(rarity)`（**远端 LiveOps 配置**，本地 0 命中）；
//  `AlternateArtPanel__TogglePurchaseBasicCard.c` 还有第二道闸 `price != 0`。
//  ⇒ 关服后**没有任何一条本地等价链**能落这一笔（没有价格、没有库存、没有扣费口）。
//  ⇒ 按 `CLAUDE.md` §18·0 的**唯一例外**（服务器相关）：**本地只出声、不假装买成功**。
//  ⇒ 🔴 **⛔ 绝对不要**在这里造一个「花 2000 买卡」的假经济 —— 玩家会看到原版没有的行为。
//
// ============================ 灭自证（哪一处改动会让哪一条红）============================
//  · `OnBuyOriginalCardClicked` 改回「只 `Debug.Log`」（旧版就是这么写的）⇒
//      A1-1（`MessageToast.ShowCount` +1）**红** · A1-2（`LastRawText` 非空）= 红 · A2 红。
//  · 把它接到 `WindowsManager.ShowPopUp`（模态 + OK 钮）⇒ A1-4（`popUpWindow == null`）**红**
//      —— 原版这一档是「连不上服务器」的**瞬时**提示，⛔ 不是一扇要玩家点 OK 的模态窗。
//  · 真去扣资源 / 改存档（假买）⇒ A1-5（钱包一格没变）· A1-6（拥有数一格没变）**红**。
//  · 把词条换成自造明文（不查官方表）⇒ A1-3（`LastRawText` **不是键名本身**且含「服务器 / server」）仍绿，
//      但 A0-5（官方表里真有这条键）会红 —— 那条钉的是「不许印键名」。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs` 的 `ShellScene.Run`（与 `B102AUD04_Assert` 同一个 `_sink`）。
//  在**合计行之前**、**同一行追加**（⇒ 行数零变化，照既有代接线的做法）：
//      B102BUY_Assert.Run(); _sink.Pass += B102BUY_Assert.Pass; _sink.Fail += B102BUY_Assert.Fail;
//  🆕 本件**不需要宿主已开着卡片详情窗** —— 自己建探针窗 + 走生产口 `CardDetailPopup.ShowCard(def)`，
//     收尾 `DestroyImmediate`；因此**任何宿主**（ShellScene / CollectionScene）都能跑；
//     也可以单独跑：`-executeMethod B102BUY_Assert.Run`。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;
using WarpforgeVFX;

/// <summary>执行代理 `B102_BUY`：「Buy Original Card」钮点了要有行为 —— B 档 = 明确出声 + 不扣资源 + 不改存档。</summary>
public static class B102BUY_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102BUY] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>钱包那一格（= `Shell/DailyData.cs` 的 `Wallet`；原版 `PlayerDataManager.gold` 的本地替身）。
    /// ⚠️ 取**这个** key 只因为它是仓里现成的金币图名（`RewardsScene` 也在用它记账），⛔ 不是我们新造的键。</summary>
    const string GoldArt = "40k_topmarquee_currency_gold";

    /// <summary>`Shell/CardDetailPopup.cs` 的仓内相对路径（`Application.dataPath` 之下）。</summary>
    const string SrcRel = "CardPresentation/Shell/CardDetailPopup.cs";

    /// <summary>那颗价签的离线提示词条 = **官方** `CustomErrors/InternetUnreachable`
    /// （`Shell/ErrorPopupWindowController` 的 `CustomError.InternetUnreachable` 走的就是它；官方 13 语言全表命中）。
    /// <para>⚠️ 这里**独立写一份字面量**（不从 `CardDetailPopup` 取常量）—— 拿被测实现证明被测实现就是自证。</para></summary>
    const string KeyBuyOriginalOffline = "CustomErrors/InternetUnreachable";

    /// <summary>`MessageToast` 这颗是不是**本件建的** —— 只有本件建的才在收尾拆掉
    /// （宿主里已经有别人那颗时不碰，免得把同一批别的断言依赖的横幅一起毁掉）。</summary>
    static bool _toastCreatedHere;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_BUY 卡牌详情窗「Buy Original Card」：B 档（依赖服务器）⇒ 明确出声、不扣资源、不改存档");
        SectionA2_Source();                                // 源码级，**先跑**：它不依赖运行期，别让夹具早退把它一起跳过
        RunBehavior();                                     // 行为级（自己管夹具与清理）
        Debug.Log(_sink.P + "B102BUY 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunBehavior()
    {
        var made = new List<GameObject>();
        bool keepSound = WFSoundPlayer.Enabled;
        try
        {
            WFSoundPlayer.Enabled = false;                 // 自检：不建 AudioSource、不出声（同 `CardFaceAssertE5`）

            var pool = CardDatabase.Load();
            Tr(pool != null && pool.Count > 0, "（夹具前提）卡池 `CardDatabase.Load()` 载入了卡（实得 "
                                             + (pool != null ? pool.Count : 0) + " 张）");
            if (pool == null || pool.Count == 0) return;

            var sample = PickSample(pool);
            Tr(sample != null, "（夹具前提）挑到一张样本卡（优先**带异画**的 —— 原版这颗价签只长在异画面板里）");
            if (sample == null) return;

            var go = new GameObject("B102BUY_Popup");
            made.Add(go);
            var pop = go.AddComponent<CardDetailPopup>();
            Tr(pop != null, "（夹具前提）`CardDetailPopup` 组件加上了");
            if (pop == null) return;

            pop.ShowCard(sample);                          // 生产口（= 原版 `ShowCard` 那条链）

            SectionA0(pop, sample);
            SectionA1(pop, sample);
        }
        catch (Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            WFSoundPlayer.Enabled = keepSound;
            for (int i = 0; i < made.Count; i++) if (made[i] != null) UnityEngine.Object.DestroyImmediate(made[i]);
            if (_toastCreatedHere) MessageToast.DisposeForTest();   // 壳场景收尾有 `SaveScene` ⇒ 漏销会被烘进 `Shell.unity`；只拆**本件建的**那颗
        }
    }

    // ============================================================ A0 前提（硬断言，⛔ 不写成 `if (x != null) Check(...)`）
    static void SectionA0(CardDetailPopup pop, CardDef sample)
    {
        MenuCheck.Section(_sink, "A0 前提：那颗钮**建出来了**、有命中件、挂着回调（⛔ 不是哑钮）");

        Tr(pop.HasCard && pop.Card != null, "A0-0 **前提**：窗上真有一张卡（`HasCard` + `Card != null`）");

        var hit = pop.BuyHit;
        Tr(hit != null, "A0-1 **前提**：`Buy Original Card Button` 底下那颗命中件 `BuyHit` **建出来了**"
                      + "（原版 `AlternateArtPanel` 的 `priceDisplayButton` 长在 `Buy Original Card Button` 这个 GO 上）");
        if (hit == null) return;

        var wb = hit.GetComponent<WindowButton>();
        Tr(wb != null && wb.onClick != null,
           "A0-2 **前提**：命中件上挂着 `WindowButton`，且 `onClick` **不是空的**"
         + "（原版 `AlternateArtPanel.Start.c:39-42` 把 `PriceDisplayButton.OnClick` 接到 `BuyOriginalCard`）");

        var quad = hit.GetComponentInChildren<ImageQuad>(true);
        Tr(quad != null,
           "A0-3 **前提**：命中区里有**可射线件**（`ImageQuad`）—— 只有裸节点的话真鼠标点不到（本仓踩过）");

        // 原版显隐 = `buyRoot.gameObject.SetActive(canBuyOriginal)`；本工程 `CardProgress.Owned` 是「给足」口径
        // （用户 2026-09-22 裁决：全解锁）⇒ `canBuyOriginal` 恒 false ⇒ **整棵关**（节点照建，原版同样是建好再关）。
        // 🔴 **量 `activeSelf` 而不是 `activeInHierarchy`**：后者会被「窗自己也关着」这种无关原因满足
        //    （= 空转绿）；`Hit(buyRoot, "BuyHit", …)` ⇒ 命中件的父就是那颗钮的根。
        var buyRoot = hit.parent;
        Tr(buyRoot != null && buyRoot.name == "Buy Original Card Button",
           "A0-4a **前提**：命中件的父节点就是 `Buy Original Card Button`（实得 `"
         + (buyRoot != null ? buyRoot.name : "<null>") + "`）—— 下面那条才量得到正确的那一格");
        Tr(buyRoot != null && !buyRoot.gameObject.activeSelf,
           "A0-4 **原版显隐照抄**：`Buy Original Card Button` **自己 `activeSelf == false`**（整棵关）"
         + "（原版 `SetStyle` 的 `(style==0) && !拥有那张 && 拥有一张它的异画` —— 本工程「全解锁 ⇒ 恒拥有」"
         + "⇒ 第二项恒假 ⇒ 价签恒关；🧨 把它改成恒开 ⇒ 红）");

        Tr(Loc.HasAnyTerm(KeyBuyOriginalOffline),
           "A0-5 **前提**：离线提示词条 `" + KeyBuyOriginalOffline + "` 在本地化表里**取得到**"
         + "（官方 13 语言全表命中 ⇒ `Loc.HasAnyTerm` 为真、⛔ 不会把键名印到玩家屏幕上）");

        // 样本侧：有异画的那一档说明「原版真会长这颗价签」，我们只是把它关着 —— 记一条可见的旁证。
        Tr(CardDetailPopup.AltStylesOf(sample.Id).Count > 0,
           "A0-6 **样本口径**：「" + sample.Name + "」**带异画**（`AltStylesOf` 实得 "
         + CardDetailPopup.AltStylesOf(sample.Id).Count + " 种）—— 原版这颗价签只长在异画面板里，"
         + "拿一张没异画的卡测就等于测一棵不会长的树");
    }

    // ============================================================ A1 ★ 真点一下 ⇒ 行为发生（出声 · 不扣 · 不改）
    static void SectionA1(CardDetailPopup pop, CardDef sample)
    {
        MenuCheck.Section(_sink, "A1 ★ 点一下（走真实入口 `BuyHit` 的 `WindowButton.Click()`）⇒ 官方「连不上服务器」横幅 + 资源/存档一格没变");

        var hit = pop.BuyHit;
        if (hit == null) { Tr(false, "A1 **前提不成立**：A0-1 的命中件不在（本节没跑，⛔ 不是绿）"); return; }
        var wb = hit.GetComponent<WindowButton>();
        if (wb == null) { Tr(false, "A1 **前提不成立**：命中件上没有 `WindowButton`（本节没跑，⛔ 不是绿）"); return; }

        _toastCreatedHere = MessageToast.Instance == null;   // 只拆**我们自己建的**那一颗（宿主里有别人的就不动）
        var mt = MessageToast.Ensure();
        Tr(mt != null && mt.Built, "A1-0 **前提**：横幅宿主建出来了（`MessageToast.Ensure` ⇒ 池子 5 条）");
        if (mt == null || !mt.Built) return;

        // 🔴 快照必须在**点之前**（同 `CardFaceAssertE5` C3 那条：点完可能重建树 / 旧引用变假 null）
        int gold0 = Wallet.Of(GoldArt);
        int owned0 = CardProgress.Owned(sample.Id, sample.Rarity);
        int show0 = mt.ShowCount;

        bool hadBtn = wb != null && wb.Interactable;
        if (wb != null) wb.Click();                        // = `PointerLayer` 唯一的派发口

        int show1 = mt.ShowCount;
        string raw = mt.LastRawText;
        int gold1 = Wallet.Of(GoldArt);
        int owned1 = CardProgress.Owned(sample.Id, sample.Rarity);

        Tr(hadBtn, "A1-0b **前提**：那颗钮点之前是可交互的（`WindowButton.Interactable`）");

        C(show1, show0 + 1,
          "A1-1 ★ **点了真的出声**：`MessageToast.ShowCount` " + show0 + " → " + show1
        + "（屏幕上一面红横幅 = 原版 `UIMessageController` 那一档）"
        + "｜🧨 把 `OnBuyOriginalCardClicked` 改回「只 `Debug.Log`」⇒ 这个数**不动** ⇒ 红（旧版就是这么静默的）");

        Tr(!string.IsNullOrEmpty(raw),
           "A1-2 横幅**真有文案**（`LastRawText` 非空；实得 `" + raw + "`）｜🧨 弹空串 ⇒ 红");

        Tr(!string.IsNullOrEmpty(raw)
           && raw != KeyBuyOriginalOffline
           && (raw.IndexOf("服务器", StringComparison.Ordinal) >= 0
               || raw.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0
               || raw.IndexOf("Internet", StringComparison.OrdinalIgnoreCase) >= 0),
           "A1-3 文案是**人话、不是键名**，且说的是「连不上服务器」这一件事（实得 `" + raw + "`）"
         + "｜🧨 直接把键名 `" + KeyBuyOriginalOffline + "` 印出去 ⇒ 红（本仓红线）");

        Tr(WindowsManager.Instance == null || WindowsManager.Instance.popUpWindow == null,
           "A1-4 **不是模态窗**：点完之后 `WindowsManager.popUpWindow == null`"
         + "（原版这一档是「连不上服务器」的**瞬时**提示；🧨 接到 `ShowPopUp` 上 ⇒ 玩家得点一下 OK ⇒ 红）");

        C(gold1, gold0,
          "A1-5 **一格资源都没扣**：金币 `" + GoldArt + "` " + gold0 + " → " + gold1
        + "（原版的扣费在 PlayFab CloudScript 里；关服 ⇒ ⛔ 不许本地记账、⛔ 也不许假装买了）");

        C(owned1, owned0,
          "A1-6 **存档一格没变**：「" + sample.Name + "」拥有数 " + owned0 + " → " + owned1
        + "（原版入库走 `InventoryManager.Add` / `RewardService.Collect` = 服务端；"
        + "🧨 在本地 `AddOwned` 一下 = 造原版没有的假经济 ⇒ 红）");

        // 连点两下也只出声、不加任何东西（幂等；挡「每点一次扣一点」这类隐形副作用）
        if (wb != null) wb.Click();
        C(mt.ShowCount, show1 + 1,
          "A1-7 再点一下 ⇒ 横幅再弹一条（" + show1 + " → " + mt.ShowCount + "）—— 每次点都出声，⛔ 不静默");
        C(CardProgress.Owned(sample.Id, sample.Rarity), owned0,
          "A1-8 连点两下**拥有数仍然不变**（" + owned0 + "）—— 挡「点一次 +1」的假买");
    }

    // ============================================================ A2 灭自证（源码级：改回「只 Log」就红）
    static void SectionA2_Source()
    {
        MenuCheck.Section(_sink, "A2 灭自证：`OnBuyOriginalCardClicked` 的方法体**真的走横幅**（把 onClick 改回「只 Log」⇒ 红）");

        string path = Path.Combine(Application.dataPath, SrcRel);
        if (!File.Exists(path))
        {
            Tr(false, "A2-0 **前提**：读得到 `" + SrcRel + "`（路径 " + path + "）—— 读不到时本节**等于没验** ⇒ 当场红");
            return;
        }

        var text = File.ReadAllText(path);
        int at = text.IndexOf("public void OnBuyOriginalCardClicked()", StringComparison.Ordinal);
        Tr(at >= 0, "A2-0 **前提**：源码里有 `public void OnBuyOriginalCardClicked()`");
        if (at < 0) return;

        // 🔴 窗口必须是**这个方法体本身**（按花括号配平截），⛔ 别用「签名之后 N 个字符」——
        //   那样会把后面的 `DoDowngrade` / `DoUpgrade` / `DoCraft` 一起圈进来，而它们**真的有** `AddOwned(`，
        //   A2-3 就会假红（本件第一版就是这么写错的）。
        int braceAt = text.IndexOf('{', at);
        string body = braceAt < 0 ? "" : BodyOf(text, braceAt);

        Tr(body.IndexOf("MessageToast.ShowError(", StringComparison.Ordinal) >= 0,
           "A2-1 ★ **B 档的实现形态 = 走横幅**：方法体里调了 `MessageToast.ShowError(`"
         + "｜🧨 把 `onClick` 改回「只 `Debug.Log`」（旧版那句 `**本版未实现：购买原卡面**`）"
         + "⇒ 本节红，同时 A1-1 的 `ShowCount` 不动 ⇒ 也红（两向都挡）");

        Tr(body.IndexOf("**本版未实现：购买原卡面**", StringComparison.Ordinal) < 0,
           "A2-2 旧那句**只出声、没有行为**的日志（`**本版未实现：购买原卡面**`）**已经不在了**"
         + "｜🧨 改回旧版 ⇒ 本节与 A2-1 同时红");

        Tr(body.IndexOf("AddOwned(", StringComparison.Ordinal) < 0
           && body.IndexOf("Wallet.Grant(", StringComparison.Ordinal) < 0,
           "A2-3 **⛔ 没有本地记账**：方法体里不出现 `AddOwned(` / `Wallet.Grant(`"
         + "（原版的扣费与入库都在服务端；🧨 在本地送一张卡 / 扣一笔钱 = 造假经济 ⇒ 红）");

        Tr(body.IndexOf("🔶 自己设计", StringComparison.Ordinal) >= 0,
           "A2-4 **自设计标记在场**：方法体里有 `🔶 自己设计`（`CLAUDE.md` §18 要求「我们自己设计的地方」"
         + "在代码里打标，方便以后复查）｜🧨 删掉标记 ⇒ 红");
    }

    /// <summary>从 `{` 开始按花括号配平截出这一层（本方法体只有一层嵌套；⛔ 不做通用词法分析，
    /// 只要求「这一段里没有带花括号的字符串字面量」—— 本方法满足，改方法体时**先看一眼**这条前提）。</summary>
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

    // ============================================================ 夹具
    /// <summary>优先挑**带异画**的卡（原版这颗价签只长在异画面板里）；没有就退到第一张有效卡。</summary>
    static CardDef PickSample(List<CardDef> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var d = pool[i];
            if (d == null || string.IsNullOrEmpty(d.Id)) continue;
            if (CardDetailPopup.AltStylesOf(d.Id).Count > 0) return d;
        }
        for (int i = 0; i < pool.Count; i++)
        {
            var d = pool[i];
            if (d != null && !string.IsNullOrEmpty(d.Id)) return d;
        }
        return null;
    }
}
