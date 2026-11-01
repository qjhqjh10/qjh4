// B116W29_Assert.cs — 执行代理 `B116_W29`（2026-11-02）落地账 `B-331` 的断言：
// 「表现层关键词」白名单（今天只有 `massive`）—— 打星时排除、上徽标时不加。
//
// ============================ 它盯的是什么 ============================
//  `massive` 已经复刻（敌方立绘 `localScale.x = -1`，见 `BattleDriver.ApplyMassiveMirror`
//  与 `B116W23_Assert`），但它是**表现层**，不能进 `KeywordTable.Implemented`
//  （那栏语义 = 引擎会结算，而且进了会让徽标那一路多显示一枚原版没有的徽标）。
//  于是 `BattleDriver.UnimplementedNotes` 会给它白打一个 `massive*`（「未实现」标记），
//  而它其实已经做了。池内今天 0 张带 `massive` ⇒ 零可见，将来一有卡就会静默错标。
//
// ============================ 判据 ============================
//  🔶 本白名单是**我们自己的装置**（原版没有「星」这个概念，见 `UnimplementedNotes` 上方那段）。
//  · `Implemented` 仍是唯一的「引擎会结算」判据（本文件 ⛔ 不改它、⛔ 不往它里面加词）；
//  · 白名单只让**表现层**词在打星处被排除，且**不给**它开徽标口。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 C2/C4：把 `UnimplementedNotes` 的打星闸整个关掉（恒不打星）⇒ `firststrike*` 期望落空 ⇒ 红。
//  🧨 B2/B3：把 `IsPresentationOnlyKeyword` 改成恒 false ⇒ 带 `massive` 的卡会打 `massive*` ⇒ 红。
//  🧨 A7：往白名单多塞一个词 ⇒ 「今天只含 massive」那条红。
//
// ============================ 怎么接 ============================
//  `Editor/BattleScene.cs` 聚合行（`=== 合计 ===` 之前）**同一物理行**追加：
//      B116W29_Assert.Run(); pass += B116W29_Assert.Pass; fail += B116W29_Assert.Fail;
using CardPresentation;
using RuleEngine;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>执行代理 `B116_W29`：账 `B-331`（表现层关键词 `massive` 不打星、也不上徽标）。</summary>
public static class B116W29_Assert
{
    const string Prefix = "[B116W29] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- 反射取私有的两个同名重载（T09Probe 同族先例；⛔ 不为了自检把它们改成 public）----
    static readonly MethodInfo _notesCard = typeof(BattleDriver).GetMethod(
        "UnimplementedNotes", BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] { typeof(CardDef) }, null);
    static readonly MethodInfo _notesUnit = typeof(BattleDriver).GetMethod(
        "UnimplementedNotes", BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] { typeof(UnitState) }, null);

    static List<string> NotesCard(CardDef c)
    {
        if (_notesCard == null) return null;
        return _notesCard.Invoke(null, new object[] { c }) as List<string>;
    }
    static List<string> NotesUnit(UnitState u)
    {
        if (_notesUnit == null) return null;
        return _notesUnit.Invoke(null, new object[] { u }) as List<string>;
    }

    /// <summary>`notes` 里有没有**那一个**词被打星（`CardText.Keyword(kw) + "*"`）。
    /// ⚠️ 期望串由 `CardText.Keyword` **现算**，与生产代码同源（中文档 / 英文档都对）。</summary>
    static bool Starred(List<string> notes, string kw)
    {
        if (notes == null) return false;
        string want = CardText.Keyword(kw) + "*";
        foreach (var s in notes) if (s == want) return true;
        return false;
    }

    // ---- 探针卡：统一 `unit` + 空 desc（`MechanismFails` 对空 desc 恒 false ⇒ 不会掺进「效果」星） ----
    static CardDef Probe(string id, params string[] kws)
    {
        return new CardDef(id, "B116W29 " + id, "unit", "", "common", "Test",
                           3, 3, 4, 0, kws);
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B116_W29 · `B-331`：表现层关键词 `massive` 不打星、也不上徽标");
        RunWhitelistRead();
        RunStar();
        RunStarStillWorks();
        RunBadge();
        Debug.Log(Prefix + "B116W29 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：白名单读口
    static void RunWhitelistRead()
    {
        MenuCheck.Section(_sink, "B116_W29 · A：白名单读口 `IsPresentationOnlyKeyword` / `PresentationOnlyKeywordsForTest`");

        Tr(BattleDriver.IsPresentationOnlyKeyword("massive"),
           "A1 ★ `IsPresentationOnlyKeyword(\"massive\")` = 真（它是这张表唯一的成员）"
         + "｜🧨 把 `IsPresentationOnlyKeyword` 改成恒 false ⇒ 本条与 B2/B3 一起红");
        Tr(BattleDriver.IsPresentationOnlyKeyword("Massive"),
           "A2 归一化：`\"Massive\"`（卡面印的大写开头）也认得 —— 与 `KeywordTable.Implemented` 同一口径");
        Tr(!BattleDriver.IsPresentationOnlyKeyword("flying"),
           "A3 真·已实现词 `flying` **不在**白名单（白名单不是「已实现表」的第二份）");
        Tr(!BattleDriver.IsPresentationOnlyKeyword("firststrike"),
           "A4 真·未实现词 `firststrike` **不在**白名单（否则它会跟着「免打星」= 静默错标）");
        Tr(!BattleDriver.IsPresentationOnlyKeyword(null), "A5 `null` ⇒ false（不抛）");
        Tr(!BattleDriver.IsPresentationOnlyKeyword(""), "A6 空串 ⇒ false（不把空串当成一个词）");

        var all = BattleDriver.PresentationOnlyKeywordsForTest;
        Tr(all != null && all.Length == 1 && all[0] == BattleDriver.MassiveKeyword,
           "A7 ★ 白名单**今天只含 `massive`**（现读 " + (all == null ? "null" : all.Length + " 条："
                                                              + string.Join(",", all)) + "）"
         + "｜🧨 往里多塞一个未核对判据的词 ⇒ 本条红（这张表每加一个词都要先证明「它是表现层」）");
    }

    // ============================================================ B 段：带 `massive` 的卡面不打星（两个重载）
    static void RunStar()
    {
        MenuCheck.Section(_sink, "B116_W29 · B：带 `massive` 的卡面**不打 `massive*`**（CardDef / UnitState 两个重载）");

        // 前提（硬检查，⛔ 不写 `if (x != null) Check(...)`：那会在夹具坏掉时空转绿）
        Tr(_notesCard != null, "（前提 · B）反射找到了 `BattleDriver.UnimplementedNotes(CardDef)` 那个重载");
        Tr(_notesUnit != null, "（前提 · B）反射找到了 `BattleDriver.UnimplementedNotes(UnitState)` 那个重载");
        if (_notesCard == null || _notesUnit == null) return;

        var card = Probe("B116W29_Massive", "Massive");
        Tr(card.Keywords != null && card.Keywords.ContainsKey(BattleDriver.MassiveKeyword),
           "（前提 · B）夹具卡的 `Keywords` 里真的有 `massive`（`Prefixes` 认得它）—— 否则下面两条是空转");
        if (card.Keywords == null || !card.Keywords.ContainsKey(BattleDriver.MassiveKeyword)) return;

        // ★ 判据本身不在 `Implemented`（这正是需要白名单的原因）——
        Tr(!KeywordTable.Implemented.Contains(BattleDriver.MassiveKeyword),
           "B1 前提：`massive` **不在** `KeywordTable.Implemented`（它是表现层，进去会多一枚原版没有的徽标）");

        var nc = NotesCard(card);
        Tr(nc != null, "（前提 · B）`UnimplementedNotes(CardDef)` 返回了列表（不是 null）");
        if (nc == null) return;
        Tr(!Starred(nc, BattleDriver.MassiveKeyword),
           "B2 ★ CardDef 重载：带 `massive` 的卡**不再打 `massive*`**（白名单把它排除了）"
         + "｜🧨 把 `IsPresentationOnlyKeyword` 改成恒 false（或去掉那半个 `&&`）⇒ 本条红");

        var u = new UnitState(card, false);
        var nu = NotesUnit(u);
        Tr(nu != null, "（前提 · B）`UnimplementedNotes(UnitState)` 返回了列表（不是 null）");
        if (nu == null) return;
        Tr(!Starred(nu, BattleDriver.MassiveKeyword),
           "B3 ★ UnitState 重载：同一张卡走场上那一路也**不打 `massive*`**"
         + "｜🧨 只改了一个重载、漏了另一个 ⇒ 本条红");
    }

    // ============================================================ C 段：灭自证 —— 真未实现的词仍然打星
    static void RunStarStillWorks()
    {
        MenuCheck.Section(_sink, "B116_W29 · C：灭自证 —— 真未实现的词**仍然**打星（白名单没把打星整个关掉）");

        if (_notesCard == null) { Tr(false, "C0 前提：`UnimplementedNotes(CardDef)` 反射拿不到 ⇒ 本段无法验（报出来而不是静默跳过）"); return; }

        // `firststrike`：`Prefixes` 有它、**故意不进** `Implemented`（`B98N3_Assert` D2 的同一只夹具）。
        var mixed = Probe("B116W29_Mixed", "Massive", "firststrike");
        Tr(mixed.Keywords != null && mixed.Keywords.ContainsKey("firststrike"),
           "（前提 · C）夹具卡真的带上了 `firststrike`");
        if (mixed.Keywords == null || !mixed.Keywords.ContainsKey("firststrike")) return;

        var notes = NotesCard(mixed);
        Tr(notes != null, "（前提 · C）`UnimplementedNotes(CardDef)` 返回了列表（不是 null）");
        if (notes == null) return;

        Tr(Starred(notes, "firststrike"),
           "C2 ★ 同一张卡上的**真未实现词 `firststrike` 仍然打星**（`firststrike*`）"
         + "｜🧨 把打星闸整个关掉（`UnimplementedNotes` 恒返回空）⇒ 本条红 —— 这条是「灭自证」的核心");
        Tr(!Starred(notes, BattleDriver.MassiveKeyword),
           "C3 ★ 同一张卡上 `massive` **不打星**、`firststrike` **打星** —— 两词并排、逐词分流（不是一刀切）"
         + "｜🧨 白名单写成「有任意白名单词就整张卡都不打星」⇒ 本条红");
        Tr(Starred(notes, "firststrike") && !Starred(notes, BattleDriver.MassiveKeyword),
           "C4 ★ 终判：`firststrike*` 在 ∧ `massive*` 不在，**同时**成立（B/C 两段合起来只有一个解释）");
    }

    // ============================================================ D 段：不给它开徽标口
    static void RunBadge()
    {
        MenuCheck.Section(_sink, "B116_W29 · D：`massive` **不上场上徽标**（白名单只免打星，不开口）");

        // ⚠️ 三条都走**纯读口**（`SpriteOf` / `*KeysForTest` / `For` 的返回）——
        //    🧨 不直接调 `PlaysInPlayIcon`：那会往 `Badges` 的「未收录告警」计数器里记一笔
        //    （`BadgeFilterAssert` 那一族读它），本断言不该给别的宿主留痕。
        Tr(Badges.SpriteOf(BattleDriver.MassiveKeyword) == null,
           "D1 ★ `Badges.SpriteOf(\"massive\")` = null —— `trait_collection.json` 认不出这张图"
         + "（没有 SO ⇒ `Badges.For` 里那句 `if (spr == null) continue;` 直接跳过）"
         + "｜🧨 给 massive 配一张 trait 图 / 塞进图集 ⇒ 本条红");

        Tr(System.Array.IndexOf(Badges.InPlayIconKeysForTest, BattleDriver.MassiveKeyword) < 0
           && System.Array.IndexOf(Badges.CardIconKeysForTest, BattleDriver.MassiveKeyword) < 0,
           "D2 ★ `massive` **不在**那两个图标键表里（「场上 47 条」与「卡面 59 条」都没有它）"
         + "｜🧨 把 massive 塞进任一张键表 ⇒ 本条红（那会画出一枚原版没有的徽标）");

        var card = Probe("B116W29_Badge", "Massive");
        var badges = Badges.For(card.Keywords);
        bool has = false;
        if (badges != null)
            foreach (var b in badges) if (b.key == BattleDriver.MassiveKeyword) has = true;
        Tr(!has,
           "D3 ★ 带 `massive` 的卡走 `Badges.For` **一枚 massive 徽标都没有**（白名单不参与徽标路径）"
         + "｜🧨 为了「看起来做完了」把 massive 塞进徽标表 ⇒ 本条红");
    }
}
