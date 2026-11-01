// B116W23_Assert.cs — 执行代理 `B116_W23`（2026-11-02）落地账 `ENG-07` 的断言：`massive` 的**水平镜像**。
//
// ============================ 它盯的是什么 ============================
//  `ENG-07` = 原版对**带 `massive` 的敌方卡**做 `localScale = (-1, 1, 1)`（**水平镜像，不是放大**），我们没做。
//  本文件三段：A = `Prefixes` 认得出 `massive`（否则机制从卡数据永远不可达）；B = 镜像本身（我方 +1 / 敌方 −1，**并排断**）；
//  C = 灭自证（不带 `massive` 的卡两侧都是 +1，且镜像**可还原**、不是「粘住」）。
//
// ============================ 判据（原版，一手）============================
//  `d:/2/tools/decomp_full/CardScript__CardSetup.c:198-210`（ILSpy 版 `CardScript.cs:6536`）：
//      cVar8 = RawCardScript__ShowDefaultTrait(*(param_1 + 0x28), 0x24e, 0);
//      if ((cVar8 != '\0') && (param_3 == '\0')) {          // ← `param_3` = isPlayer
//          lVar13 = Component__get_transform(*(param_1 + 0x20));   // ← `cardImage`
//          ... Transform__set_localScale(lVar13, &local_48, 0);   // local_48 = (-1, 1, 1)
//      }
//    · 倍率字面量 `_DAT_1834b2bc8` 用 `工具/read_literal.py` 现读 = **`-1`**（float）⇒ 三元组 `(-1, 1, 1)`。
//    · `param_1 + 0x20` = `EntityScript.cardImage`（`dump.cs:21966`，类型 `UnityEngine.UI.Image`）——
//      在 `CardPrefab.prefab` 里指向 `CardPrefab/2DCard/Front/CardImage`。
//    · 我们的等价层 = `CardView._art`（底层完整插图）。
//  ⚠️ 同值 `0x24e` 另有 `AbilityTrigger.Combo = 590` —— `CheckForCombo` 等 4 处的
//     `OnTrigger(raw, 0x24e, …)` 是 **Combo 触发器、与 massive 无关**（⛔ 别混）。
//  ⚠️ `massive` **不进 `Implemented`**：它在 `trait_collection.json` 里没有 SO，
//     进 `Implemented` 会让 `BattleDriver` 的外观过滤多显示一枚原版没有的徽标 ⇒ A 段**反向断**它不在里面。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B：把 `ApplyArtTransform` 里的 `* _artMirrorX` 删掉 ⇒ B 段「敌方 == −1」「我方 ≠ 敌方」当场红。
//  🧨 B：把 `ApplyMassiveMirror` 的 `!mine` 写成 `mine`（或去掉）⇒ 我方那条红（它是 +1 的硬期望）；
//     反过来写成「敌方恒 −1、我方恒 +1、不看 massive」⇒ C 段（不带 massive 的敌方要 +1）红。
//  🧨 C：把 `SetArtMirrorX(false)` 做成空操作（只写字段不重算 transform）⇒ C 段「还原成 +1」红。
//  🧨 A：把 `new[] { "massive", "massive" }` 从 `Prefixes` 删掉 ⇒ A 段 `Normalize` 三条 + 卡模板那条一起红
//     （期望值全是**字面量**，⛔ 不从 `Prefixes` 回读）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主其它部分）============================
//  `Editor/BattleScene.cs` 的聚合行（`=== 合计 ===` 那一行）**之前**、同一物理行追加：
//      B116W23_Assert.Run(); pass += B116W23_Assert.Pass; fail += B116W23_Assert.Fail;
//  ⚠️ 本文件**自带探针根节点**并在收尾 `DestroyImmediate`（同 `B101R4_Assert` 那一族先例）——
//     ⛔ 不依赖宿主手里任何现场（本件不读 `driver`、不 `Begin` 一局，零污染）。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W23`：账 `ENG-07`（`massive` 敌方卡的水平镜像）。</summary>
public static class B116W23_Assert
{
    const string Prefix = "[B116W23] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 探针数据

    /// <summary>带 `massive` 的单位卡（照 `cards_engine.json` 的 `keywords` 写法，大写开头 ——
    /// 原版卡面就印大写；`Normalize` 自己会 `ToLowerInvariant`）。</summary>
    static CardDef MassiveCard()
    {
        return new CardDef("B116W23_Massive", "B116W23 Massive Probe", "unit", "", "common", "Test",
                           3, 3, 4, 0, new[] { "Massive" });
    }

    /// <summary>同一张卡但**不带** `massive` —— C 段的对照物（除这一个词以外逐字相同）。</summary>
    static CardDef PlainCard()
    {
        return new CardDef("B116W23_Plain", "B116W23 Massive Probe", "unit", "", "common", "Test",
                           3, 3, 4, 0, null);
    }

    static CardData ProbeData(string title)
    {
        var d = CardData.Simple(title, 3, 3, 4);
        d.isUnit = true;
        return d;
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B116_W23 · ENG-07：`massive` 的敌方卡水平镜像（原版 `CardScript.CardSetup` 那一句）");
        RunKeywordRegistered();
        RunMirror();
        Debug.Log(Prefix + "B116W23 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：`Prefixes` 认得出 `massive`

    static void RunKeywordRegistered()
    {
        MenuCheck.Section(_sink, "B116_W23 · A：`CardDef.Prefixes` 认得 `massive`（不认得 ⇒ 机制从卡数据不可达）");

        // ---- ① 规范化：三种大小写都要落到同一个字面量 ----
        C(KeywordTable.Normalize("Massive"), "massive",
          "A1 `Normalize(\"Massive\")` = `\"massive\"`（卡面印的是大写开头）"
        + "｜🧨 把 `new[] { \"massive\", \"massive\" }` 从 `Prefixes` 删掉 ⇒ 返回 null ⇒ 本条红（期望是字面量，⛔ 不从表回读）");
        C(KeywordTable.Normalize("massive"), "massive", "A1b 全小写同样认得出");
        C(KeywordTable.Normalize("MASSIVE"), "massive", "A1c 全大写同样认得出（`Normalize` 先 `ToLowerInvariant`）");

        // ---- ② 进了 Prefixes 但**不进** `Implemented`（进了会多显示一枚原版没有的徽标）----
        Tr(!KeywordTable.Implemented.Contains(BattleDriver.MassiveKeyword),
           "A2 `massive` **不在** `KeywordTable.Implemented` 里 —— 它在 `trait_collection.json` 没有 SO，"
         + "进 `Implemented` 会让 `BattleDriver` 的外观过滤给它多显示一枚原版没有的徽标"
         + "｜🧨 为了「看起来做完了」把它塞进 `Implemented` ⇒ 本条红");

        // ---- ③ 端到端：`CardDef` 构造函数不再把它 `NoteDropped` 丢掉 ----
        var card = MassiveCard();
        Tr(card.Keywords != null && card.Keywords.ContainsKey(BattleDriver.MassiveKeyword),
           "A3 ★ 带 `Massive` 的 `CardDef` 里**真的有**这个关键词（`Keywords[\"massive\"]`）——"
         + " 这条是 B/C 两段的前提：不登记时构造函数的 `NoteDropped` + `continue` 会把它**静默丢掉**"
         + "｜🧨 删掉 `Prefixes` 那条 ⇒ 本条红");
        Tr(!KeywordTable.Dropped.Contains("Massive"),
           "A3b `KeywordTable.Dropped` 里**没有** `\"Massive\"`（= 它没有被当成「认不出的词」静默扔掉）");

        var plain = PlainCard();
        Tr(plain.Keywords == null || !plain.Keywords.ContainsKey(BattleDriver.MassiveKeyword),
           "A4 ★ 对照卡（`keywords = null`）**没有** `massive` —— C 段的对照物必须是真对照"
         + "（否则 C 段那两条 +1 会变成同义反复）");
    }

    // ============================================================ B/C 段：镜像本身 + 灭自证

    static void RunMirror()
    {
        MenuCheck.Section(_sink, "B116_W23 · B/C：我方 +1 · 敌方 −1 · 不带 massive 的两侧都 +1");

        var card = MassiveCard();
        var plain = PlainCard();
        Tr(card.Keywords != null && card.Keywords.ContainsKey(BattleDriver.MassiveKeyword),
           "（前提 · B/C）带 `massive` 的卡确实带上了这个词（A3 若红，本段全部无意义，本条先红）");

        var root = new GameObject("B116W23_MassiveMirrorProbe");
        try
        {
            // ---- 建三张**同一张卡**的场上视图：我方 / 敌方 / 不带 massive 的敌方 ----
            var mv = CardView.Create(root.transform, ProbeData("B116W23 Massive Probe"), "B116W23_mine", CardFace.Board);
            var fv = CardView.Create(root.transform, ProbeData("B116W23 Massive Probe"), "B116W23_foe", CardFace.Board);
            var nv = CardView.Create(root.transform, ProbeData("B116W23 Massive Probe"), "B116W23_foe_plain", CardFace.Board);

            // 硬前提（⛔ 不写 `if (v != null) Check(...)`：那会在建不出来时**空转绿**）
            Tr(mv != null, "（前提 · B）我方探针视图建出来了（`CardFace.Board`）");
            Tr(fv != null, "（前提 · B）敌方探针视图建出来了（`CardFace.Board`）");
            Tr(nv != null, "（前提 · C）不带 `massive` 的对照视图建出来了（`CardFace.Board`）");
            Tr(mv != null && !float.IsNaN(mv.ArtLayerScaleX),
               "（前提 · B）我方视图的**立绘层真的建出来了**（`ArtLayerScaleX` 不是 NaN）——"
             + " 层建不出来时下面每条 `== 1` 都会变成空转");
            Tr(fv != null && !float.IsNaN(fv.ArtLayerScaleX), "（前提 · B）敌方视图的立绘层真的建出来了");
            Tr(nv != null && !float.IsNaN(nv.ArtLayerScaleX), "（前提 · C）对照视图的立绘层真的建出来了");
            if (mv == null || fv == null || nv == null
                || float.IsNaN(mv.ArtLayerScaleX) || float.IsNaN(fv.ArtLayerScaleX) || float.IsNaN(nv.ArtLayerScaleX))
                return;

            // ---- 出厂值：还没跑判据之前，三张都该是 +1（否则下面的 +1 就不是「判据给的」）----
            C(mv.ArtLayerScaleX, 1f, "B0 出厂（未跑判据）：我方立绘 `scale.x == +1`");
            C(fv.ArtLayerScaleX, 1f, "B0b 出厂：敌方立绘 `scale.x == +1`（镜像**只**由 `massive` 触发，⛔ 不是「敌方一律翻」）");

            // ---- B 段：同一份带 `massive` 的卡，**一边一半**（并排断）----
            bool rMine = BattleDriver.ApplyMassiveMirror(mv, card, mine: true);
            bool rFoe = BattleDriver.ApplyMassiveMirror(fv, card, mine: false);

            Tr(!rMine, "B1 我方带 `massive` ⇒ **不镜像**（`ApplyMassiveMirror` 返回 false）"
                     + "｜🧨 把判据里的 `!mine` 去掉 ⇒ 本条红（原版 `param_3 == '\\0'` 那道 `isPlayer` 闸）");
            Tr(rFoe, "B2 敌方带 `massive` ⇒ **镜像**（返回 true）｜🧨 把 `!mine` 写成 `mine` ⇒ 本条红");
            C(mv.ArtLayerScaleX, 1f, "B3 ★ 我方立绘真实 `localScale.x == +1`（原版那道 `isPlayer` 闸挡住的一半）");
            C(fv.ArtLayerScaleX, -1f, "B4 ★ 敌方立绘真实 `localScale.x == -1`（原版 `set_localScale(-1, 1, 1)`）"
                                   + "｜🧨 把 `ApplyArtTransform` 里的 `* _artMirrorX` 删掉 ⇒ 本条红");
            Tr(mv.ArtLayerScaleX != fv.ArtLayerScaleX,
               "B5 ★ 并排：我方与敌方**必须不同号**（`" + mv.ArtLayerScaleX + "` vs `" + fv.ArtLayerScaleX + "`）"
             + "—— 单独断一边的写法抓不到「两边一起翻」｜🧨 判据写成「敌方一律 −1」且顺手把我方也翻了 ⇒ 本条红");
            C(fv.ArtLocalScale, 1f, "B6 镜像**只翻 x 这一维**：立绘层的均匀缩放 `ArtLocalScale` 仍是 +1"
                                  + "（⛔ 不是「把整层负缩放」那种写法的副作用）");
            Tr(Mathf.Approximately(mv.ArtLayerScaleX, 1f) && Mathf.Approximately(fv.ArtLayerScaleX, -1f),
               "B7 ★ 并排终判：我方 `+1` ∧ 敌方 `−1` **同时**成立（两条都在这一句里再走一遍）");

            // ---- C 段：灭自证 —— 不带 `massive` 的敌方**两侧都 +1**，且镜像可还原 ----
            bool rPlainFoe = BattleDriver.ApplyMassiveMirror(nv, plain, mine: false);
            Tr(!rPlainFoe, "C1 ★ 敌方但**不带** `massive` ⇒ 不镜像（返回 false）"
                         + "｜🧨 把判据写成「敌方一律镜像」（不看 `massive`）⇒ 本条红");
            C(nv.ArtLayerScaleX, 1f, "C2 ★ 敌方 · 不带 `massive` ⇒ 立绘真实 `scale.x == +1`"
                                   + "｜🧨 同上，本条是「敌方一律翻」那种假实现的第二道闸");

            bool rMinePlain = BattleDriver.ApplyMassiveMirror(nv, plain, mine: true);
            Tr(!rMinePlain, "C3 我方 · 不带 `massive` ⇒ 也不镜像");
            C(nv.ArtLayerScaleX, 1f, "C4 我方 · 不带 `massive` ⇒ 立绘真实 `scale.x == +1`");

            // 还原：同一张敌方视图改喂**不带** massive 的卡 ⇒ 必须回到 +1（证明判据每次都真写 transform，不是「翻过就粘住」）
            BattleDriver.ApplyMassiveMirror(fv, plain, mine: false);
            C(fv.ArtLayerScaleX, 1f, "C5 ★ 同一张敌方视图改喂「不带 `massive`」⇒ 立绘回到 `+1`"
                                   + "（`SetArtMirrorX(false)` 真的重算了 transform）"
                                   + "｜🧨 把 `SetArtMirrorX` 写成只改字段、不调 `ApplyArtTransform` ⇒ 本条红");
            // 再翻回去：证明可逆（B4 那次不是「一次性的巧合」）
            BattleDriver.ApplyMassiveMirror(fv, card, mine: false);
            C(fv.ArtLayerScaleX, -1f, "C6 再喂回带 `massive` ⇒ 又变 `−1`（镜像**可逆**，C5 不是把状态弄坏了）");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
