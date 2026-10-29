// B94B11_Assert.cs — 执行代理 B94_B11（第九十四会话 · 波 3）三处接线的断言
//
// ============================ 它盯的是什么 ============================
//   A 账 `战斗逻辑驱动:393` —— `BattleManager.GetWarcry` 的接线（`BattleDriver.MetaOf` 那一句）
//   B 账 `战斗逻辑驱动:1280` —— `RawCardScript.ReleaseFullHeroSprite` 的三个宿主
//       （`Shell/DeckInfoPopup.cs` 新增 `Close()` · `Shell/DraftModeWindow.Close()` · `Shell/PracticeModePopup` 的
//        `RefreshCharacterImage()` + `Close()`）
//   C 账 `战斗逻辑驱动:424` —— `IsScriptedStageAvailable` 收口到 `TutorialScriptPredicatesB94B8`
//
// ============================ 怎么用（宿主）============================
// `B94B11_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；不新建任何 `BattleDriver` 实例（那会触发 `OnDestroy` → `DetachStaticHooks()`，
//     把同一批别的断言挂的静态钩子一起摘掉）。
//   · 接线面全在**私有方法 / 私有闭包**里，没有纯函数入口 ⇒ 这里用「**契约断言（跑得到）** + **结构断言（源码文本里有那一句）**」
//     两档配合；**结构断言只证明「那一句还在」，不证明行为对** —— 行为那半写明交给哪个宿主（见每条文案）。
//
// ============================ 🔴 灭自证 ============================
//   · A3：把 `Loc.Warcry` 改成恒返回 `""` ⇒ A3 红（空 id 档必须非空）。
//   · A4：把 `m.Warcry = Loc.Warcry(deck.WarcryId);` 删掉 ⇒ A4 红（结构断言）。
//   · B2：`ReleaseFullHeroSprite` 改成「放完不清登记」⇒ B2 红（第二次放应为 false）。
//   · B3：改成「放不存在的 id 时抛异常」⇒ B3 红（原版这一档**静默**返回）。
//   · B4/B5/B6：宿主里把那一句删掉 ⇒ 对应结构断言红。
//   · C2：`IsScriptedStageAvailable` 改成只看 `CampaignStage` ⇒ C2 红。
//   · C3：改成恒 `true` ⇒ C1 红（`ctx == null` 必须 false）。
using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B11：B94 波 3 的三处接线（warcry / 督军整身立绘释放 / 脚本关判据收口）。
/// <para>⛔ **期望值全是字面量**（`"Chat_Messages/warcry-"` / `true` / `false` …），**不从被测实现里读**。</para></summary>
public static class B94B11_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94B11] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_B11：warcry 接线 · `ReleaseFullHeroSprite` 三处宿主 · `IsScriptedStageAvailable` 收口");

        SectionA_Warcry();
        SectionB_ReleaseFullHeroSprite();
        SectionC_ScriptedStage();

        Debug.Log(_sink.P + "B94B11 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ 共用：读源码（结构断言用）
    /// <summary>读 `Assets/` 下的源码文本；读不到 ⇒ `null`（断言会红，但**不抛**）。
    /// <para>⚠️ 结构断言的必要性：接线落在**私有方法 / 私有闭包**里，没有可从编辑器直接调的入口；
    /// 把 `BattleDriver` 实例化来跑 `MetaOf` 会触发 `OnDestroy → DetachStaticHooks()`，**摘掉同批别的断言挂的静态钩子**
    /// ⇒ 宁可「源码里有那一句」+「两半契约各自跑一遍」，也不制造跨断言的副作用。</para></summary>
    static string ReadSource(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(p)) { Debug.LogWarning("[B94B11] 找不到源码：" + p); return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B94B11] 读源码失败：" + rel + " · " + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>源码里出现过 <paramref name="needle"/>（逐字）。</summary>
    static bool SrcHas(string src, string needle)
    {
        return src != null && src.IndexOf(needle, StringComparison.Ordinal) >= 0;
    }

    // ------------------------------------------------------------ A 账 393
    static void SectionA_Warcry()
    {
        MenuCheck.Section(_sink, "A 账 `战斗逻辑驱动:393` `BattleManager.GetWarcry` 接线（`BattleDriver.MetaOf`）");

        // 前提：`PlayerBattleMeta` 真有 `Warcry` 这一格，且是 string（原版 `DeckAndWarlordData` 的 `[1] warcry`）。
        var meta = typeof(BattleDriver).GetNestedType("PlayerBattleMeta", BindingFlags.Public);
        Tr(meta != null, "A0 前提：`BattleDriver.PlayerBattleMeta` 是**公开嵌套类型**");
        var wf = meta != null ? meta.GetField("Warcry") : null;
        Tr(wf != null && wf.FieldType == typeof(string), "A0b 前提：`PlayerBattleMeta.Warcry` 是 string（= 原版 `DeckAndWarlordData` `[1] warcry`）");

        // 前提：两半都在 —— `Loc.Warcry`（取文）与 `PlayerDeck.WarcryId`（数据）。
        var widF = typeof(PlayerDeck).GetField("WarcryId");
        Tr(widF != null && widF.FieldType == typeof(string),
           "A1 前提：`PlayerDeck.WarcryId` 是 string 字段（`Core/DeckRules.cs:1057`，原版 `CardDeck.warcryId` @0x30）");
        var deck = new PlayerDeck("B94B11 Deck", "AM1", "", new string[0], (int)GameMode.Classic);
        deck.WarcryId = "cw_probe";
        C(deck.Clone().WarcryId, "cw_probe", "A1b 前提：`PlayerDeck.Clone()` 会拷 `WarcryId`（漏拷 = 静默丢装饰）");
        C(Loc.WarcryKey(""), "Chat_Messages/warcry-default", "A2 前提：`WarcryKey(\"\")` = 默认 term（原版 `GetWarcry.c:69` 那条 `GetTranslation(默认)`）");
        C(Loc.WarcryKey("cw_probe"), "Chat_Messages/warcry-cw_probe", "A2b 前提：非空 id ⇒ 前缀 + id（原版 `GetWarcry.c:73` 的 `String.Concat(前缀, 装饰.uniqueId)`）");

        // ★ 契约：空 id 档**恒不是空串**（⛔ 这一档以后不许再退化成「空白」）。
        string empty = Loc.Warcry("");
        Tr(!string.IsNullOrEmpty(empty), "A3 ★ `Loc.Warcry(\"\")` **非空**（= 原版走默认 term；⛔ 空串 = 一块空白 = 静默失败）实得：" + (empty ?? "null"));
        string withId = Loc.Warcry("cw_probe");
        Tr(!string.IsNullOrEmpty(withId) && withId != empty,
           "A3b ★ 带 id 与空 id **取到的不是同一个串**（证明 id 真的进了键里）实得：" + (withId ?? "null"));

        // ★ 结构：`MetaOf` 里那一句真的在（`m.Warcry` 从此不再走 `missing.Add("warcry")`）。
        string src = ReadSource("CardPresentation/Battle/BattleDriver.cs");
        Tr(src != null, "A4 前提：读得到 `BattleDriver.cs` 源码");
        Tr(SrcHas(src, "m.Warcry = Loc.Warcry(deck.WarcryId)"),
           "A4b ★ `BattleDriver.MetaOf` 里有 `m.Warcry = Loc.Warcry(deck.WarcryId)`（账 393 的接线那一句）");

        Debug.Log(_sink.P + "A 段：`MetaOf` 是**私有实例方法**（要 `BattleDriver` 实例才跑得到）⇒ 端到端建议宿主 = `Editor/BattleScene.cs` 开一局后读"
                  + "「玩家元数据那一条 Debug.Log 不再列 warcry」；本段不实例化（理由见 `ReadSource` 的注）。");
    }

    // ------------------------------------------------------------ B 账 1280
    static void SectionB_ReleaseFullHeroSprite()
    {
        MenuCheck.Section(_sink, "B 账 `战斗逻辑驱动:1280` `RawCardScript.ReleaseFullHeroSprite` 的释放契约 + 三个宿主");

        // 前提：`AM1` 的整身立绘在工程里（`CardArt._deckArtById` 表里有它、且 `Resources/Art/ui_menu/` 有这张图）。
        Texture2D tex;
        bool got = CardArt.TryDeckHero("AM1", out tex);
        Tr(got && tex != null, "B0 前提：`CardArt.TryDeckHero(\"AM1\")` 取到整身立绘（表 + 图都在）");

        // ★ 契约：放得掉（返回 true）+ 放完就真没了（第二次 false）—— 两态缺一不可。
        bool first = CardArt.ReleaseFullHeroSprite("AM1");
        Tr(first, "B1 ★ `ReleaseFullHeroSprite(\"AM1\")` 放掉了登记（返回 true）");
        bool second = CardArt.ReleaseFullHeroSprite("AM1");
        Tr(!second, "B2 ★ 第二次放同一张 ⇒ **false**（登记已摘；⛔ 若恒 true 说明它根本没清缓存）");

        // ★ 契约：原版那两条**静默**空操作（`RawCardScript__ReleaseFullHeroSprite.c`：句柄无效 ⇒ 直接返回、不出声）。
        Tr(!CardArt.ReleaseFullHeroSprite(null), "B3 ★ `warlordId == null` ⇒ false（⛔ 不抛、不刷告警）");
        Tr(!CardArt.ReleaseFullHeroSprite("__b94b11_no_such_warlord__"), "B3b ★ 表里没有的 id ⇒ false");

        // ★ 结构：三个宿主真的调了它。
        string deckInfo = ReadSource("CardPresentation/Shell/DeckInfoPopup.cs");
        Tr(deckInfo != null, "B4 前提：读得到 `Shell/DeckInfoPopup.cs` 源码");
        Tr(SrcHas(deckInfo, "CardArt.ReleaseFullHeroSprite("),
           "B4b ★ `DeckInfoPopup` 调了 `CardArt.ReleaseFullHeroSprite`（原版 `DeckInfoPopup__Close.c:19-22`）");
        // 更强的一条：`Close()` 必须是**本类**的覆写（改前本类**没有** `Close()`，这条在改前必红）。
        var closeM = typeof(DeckInfoPopup).GetMethod("Close", BindingFlags.Public | BindingFlags.Instance);
        Tr(closeM != null && closeM.DeclaringType == typeof(DeckInfoPopup),
           "B4c ★ `DeckInfoPopup.Close()` 是本类**覆写**（改前它继承基类那份 ⇒ 这一档关窗一秒图都放不掉），DeclaringType = "
           + (closeM == null ? "<无>" : closeM.DeclaringType.Name));

        string draft = ReadSource("CardPresentation/Shell/DraftModeWindow.cs");
        Tr(draft != null, "B5 前提：读得到 `Shell/DraftModeWindow.cs` 源码");
        Tr(SrcHas(draft, "CardArt.ReleaseFullHeroSprite(_warlordId)"),
           "B5b ★ `DraftModeWindow.Close()` 调了它、实参 = `_warlordId`（原版 `DraftModeMenuDemo__ReleaseMenuWarlordImage` + `DraftRunDisplay__OnDestroy`）");

        string practice = ReadSource("CardPresentation/Shell/PracticeModePopup.cs");
        Tr(practice != null, "B6 前提：读得到 `Shell/PracticeModePopup.cs` 源码");
        Tr(SrcHas(practice, "CardArt.ReleaseFullHeroSprite(_charImgWarlordId)"),
           "B6b ★ `PracticeModePopup` 调了它、实参 = `_charImgWarlordId`（原版 `PlayerDeckDemoDeckInfoWrapper__UnloadCharacterImage`）");
        Tr(SrcHas(practice, "RefreshCharacterImage"), "B6c 前提：`RefreshCharacterImage()` 仍在（换图那条路是本窗的第 2 个调用点）");
        Tr(SrcHas(practice, "string _charImgWarlordId;"),
           "B6d ★ 「上一张是谁」那格在位（原版靠持有者自己的句柄知道上一张；我们只有按路径一条登记 ⇒ 必须自己记）");

        Tr(!CardArt.IsCached(CardArt.Root + "ui_menu/UI_Deck_Warlord_Leontus"),
           "B7 收尾：`AM1` 那张已经不在缓存里（B1/B2 的副作用；随时可重新 `DeckHero` 取回）");
    }

    // ------------------------------------------------------------ C 账 424
    static void SectionC_ScriptedStage()
    {
        MenuCheck.Section(_sink, "C 账 `战斗逻辑驱动:424` `IsScriptedStageAvailable` 收口（原版 `BattleManager__IsScriptedStageAvailable.c:1-15`）");

        var m = typeof(TutorialScriptPredicatesB94B8).GetMethod("IsScriptedStageAvailable",
                    BindingFlags.Public | BindingFlags.Static);
        Tr(m != null && m.ReturnType == typeof(bool) && m.GetParameters().Length == 1,
           "C0 前提：`TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(BattleContext)` 在位且返回 bool");

        // ★ 三态真值表（`ctx == null` / 教程半 / 战役半）。
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(null),
           "C1 ★ `ctx == null` ⇒ **false**（⛔ 不许恒 true，否则所有局都按「脚本关」走）");

        var ctx = new BattleContext(20261029);
        Tr(ctx.Tutorial == null && ctx.CampaignStage == null, "C1b 前提：新建 `BattleContext` 两半都空");
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C2 ★ 两半都空 ⇒ **false**（普通对局不该进脚本关那一支）");

        // 🔴 2026-10-29（`B97_TUT`）就地订正（铁律 5）：这三态原来是按「两条 null 判断」写的，
        //    而原版两个操作数**各四道闸**（① match ② stage 非空 ③ `turn>0` ④ `turn-1 < stage[+0x48].Count`，
        //    判据 = `decomp_full/BattleManager__IsTutorialStageAvailable.c` / `__IsCampaignStageAvailable.c`
        //    + `B97_可推A七条_审查.md` §一·3）⇒ 空 stage（`turns == null`）原版返回 **false**。
        ctx.Turn = 1;
        ctx.Tutorial = new TutorialScript(new TutorialStageData());
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C3 ★ 教程半非空但 `Stage.turns == null` ⇒ **false**（原版 ④：`+0x48 == 0` 直接返 false，`.c` 逐句）");

        ctx.Tutorial = new TutorialScript(new TutorialStageData { turns = new[] { new TutorialTurn() } });
        Tr(TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C3b ★ 教程半非空 ∧ `turns.Length = 1` ∧ `Turn = 1` ⇒ **true**（四道闸全过；原版 `IsTutorialStageAvailable()` 那一半）");

        ctx.Turn = 0;
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C3c ★ 同上但 `Turn = 0` ⇒ **false**（原版 ③ `turnCounter > 0`）");
        ctx.Turn = 2;
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C3d ★ `Turn = 2` 而 `turns.Length = 1` ⇒ **false**（原版 ④ `turn-1 < Count` 不成立）");
        ctx.Turn = 1;

        ctx.Tutorial = null;
        ctx.CampaignStage = new TutorialStageData();
        Tr(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C4 ★ 教程空、战役半非空但 `turns == null` ⇒ **false**（战役那一半是同一套四道闸）");
        ctx.CampaignStage = new TutorialStageData { turns = new[] { new TutorialTurn() } };
        Tr(TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
           "C5 ★ 战役半 `turns.Length = 1` ∧ `Turn = 1` ⇒ **true**（原版 `IsCampaignStageAvailable()` 那一半；⛔ 只判教程会漏这一档）");

        // ★ 结构：`TutorialWaitingUseAbility` 的 ⑤ 真的改走这个谓词了。
        string src = ReadSource("CardPresentation/Battle/BattleDriver.cs");
        Tr(src != null, "C5 前提：读得到 `BattleDriver.cs` 源码");
        Tr(SrcHas(src, "TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(Ctx)"),
           "C5b ★ `BattleDriver.TutorialWaitingUseAbility()` 的 ⑤ 走引擎侧唯一判据（账 424 的接线那一句）");
    }
}
