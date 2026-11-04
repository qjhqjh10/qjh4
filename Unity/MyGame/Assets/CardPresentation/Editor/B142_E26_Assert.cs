// B142_E26_Assert.cs — 批次 **B142** · 执行写手 `E26`（2 件：CARDTRACTS-BADGE / POSEFOR-THIRD）
//
// ============================ 它盯的是什么 ============================
//   A 组（新账 `B142-CARDTRACTS-BADGE`）：场上那 7 格徽标的**基础源**必须是 SO 的 `CardTraits`
//       （`BattleCardUI__UpdateTraitIcons.c:59` → `CardTraitCollection__GetTraitInPlayListForIcons.c:47-113`
//        遍历 `card+0x128`；`+0x128` = `CardScript__SetBaseCardTraits.c:36-70` 从 `rawCard+0x230` 整表拷来），
//       **不是** `keywords` 列（我们原来唯一吃的那一列）。
//       活例 `AM72 Vitus Gryf`：`keywords = Flying/Strike` · `cardTraits = Flying/Slay`
//       ⇒ 改前多画一枚 `strike`、少画一枚 `slay`。
//
//   B 组（新账 `B142-POSEFOR-THIRD`）：`BattleDriver.PoseFor` 的 3D 支要照原版
//       `BattleManager.GetUnitSizeInPlay` 的**三条**分支走 —— 第三支（`cardType ∉ {0,10}` = defence/enchantment
//       在场卡）= `minionInPlayScale 0.75 × (enemy ? enemyUnitsScaleRatio 0.86 : 1)` = **0.75 / 0.645**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2：不传 `traitSource`（= 改前那条路）⇒ `AM72` 的徽标**会**出现 `strike` ⇒ A2 红、A1 红。
//   · A3/A4：把 `RetargetBadgeSourceToCardTraits` 里「基础那一半改用 `CardTraits`」删掉 ⇒ A3 红；
//       把「运行期授予那一半保留」删掉（改成整表重建、不留 `runtime`）⇒ A4 红。
//   · A6：同上 ⇒ 全池 `strike` 徽标「改前 − 改后」= 0（不是 1）⇒ 红。
//   · B1/B2：把第三支删掉（`!isUnit` 那一行拿掉，退化到 `CardScale`）⇒ 0.75/0.645 变成 0.36/0.69 ⇒ 红。
//   · B4：同上 ⇒ 「第三支 ≠ 部队支」这条红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/CardPresentation/Editor/BattleScene.cs` 聚合行**同一物理行**追加（接线由调度台统一做）：
//     `B142_E26_Assert.Run(); pass += B142_E26_Assert.Pass; fail += B142_E26_Assert.Fail;`
//   （⚠️ 追加文本必须落在该行第一个 `//` **之前**；⚠️ `BattleScene` 宿主里**没有 `Guard`**，照抄 `Guard` 会 CS0103）
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B142-E26 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E26_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B142-E26：徽标源 = SO CardTraits / PoseFor 第三支 0.75·0.645");
        GroupA_CardTraitsBadgeSource();
        GroupB_PoseForThirdBranch();
        Summary();
    }

    // ==================================================================
    //  A 组 · 徽标源 = SO 的 `CardTraits`
    // ==================================================================
    static void GroupA_CardTraitsBadgeSource()
    {
        Section("A · `B142-CARDTRACTS-BADGE`：基础源 = `CardDef.CardTraits`（⛔ 不是 `keywords` 列）");
        Badges.ResetUnknownForTest();

        // ---- A0 前提（先断数据，别让后面几条空转绿）----
        var pool = RuleEngine.CardDatabase.Load();
        CardDef am72 = null;
        if (pool != null)
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && pool[i].Id == "AM72") { am72 = pool[i]; break; }

        if (am72 == null)
        {
            CheckTrue(false, "A0 **前提不成立**：卡池里找不到 `AM72`（`CardDatabase.Load()`）—— A1/A2/A6 **没有执行**，不是通过");
        }
        else
        {
            bool dataOk = am72.CardTraitsKnown && HasTraitNamed(am72, "slay") && HasKeywordNamed(am72, "strike") && !HasKeywordNamed(am72, "slay");
            CheckTrue(dataOk,
                      "A0 前提：`AM72` 的 `cardTraits` 含 `slay`、`keywords` 含 `strike` 而**不含** `slay`"
                    + $"（现读：CardTraitsKnown={am72.CardTraitsKnown}；keywords={KeysOf(am72.Keywords)}；cardTraits={TraitNamesOf(am72)}）");

            // ---- A1 ★ 完整接线（derivedCard + traitSource）⇒ 含 slay、不含 strike ----
            var full = Badges.For(am72.Keywords, null, am72.NumericKeywords, null, true, false, am72, am72);
            var fullSprites = Sprites(full);
            CheckTrue(fullSprites.Contains("slay") && !fullSprites.Contains("strike"),
                      "A1 ★ `AM72` 走完整接线 ⇒ 徽标**含 `slay`、不含 `strike`**"
                    + $"（实际：[{string.Join(",", fullSprites)}]）—— 判据 = SO `CardTraits`，`Strike` 只是它效果正文里的触发词");

            // ---- A2 灭自证：不传 `traitSource`（= 改前那条路）⇒ strike 回来 ----
            var legacy = Badges.For(am72.Keywords, null, am72.NumericKeywords, null, true, false, am72);
            var legacySprites = Sprites(legacy);
            CheckTrue(legacySprites.Contains("strike"),
                      "A2 ★ 灭自证：**不传 `traitSource`** 时 `strike` 仍在"
                    + $"（实际：[{string.Join(",", legacySprites)}]）⇒ A1 的「不含」是本条修出来的，不是它本来就没有");
        }

        // ---- A3 合成卡把两条路彻底分开（无 trigger ⇒ 派生 trait 不干扰）----
        var synth = MakeCard("T_CT", "Test CardTraits", new[] { "Flying", "Strike" });
        synth.AttachCardTraits(new[] { "Flying", "Slay" });
        var synthNew = Sprites(Badges.For(synth.Keywords, null, synth.NumericKeywords, null, true, false, synth, synth));
        var synthOld = Sprites(Badges.For(synth.Keywords, null, synth.NumericKeywords, null, true, false, synth));
        CheckTrue(synthNew.Contains("slay") && !synthNew.Contains("strike"),
                  $"A3 ★ 合成卡（keywords=Flying/Strike · cardTraits=Flying/Slay · 无 trigger）：传 `traitSource` ⇒ 含 `slay`、不含 `strike`（实际：[{string.Join(",", synthNew)}]）");
        CheckTrue(synthOld.Contains("strike") && !synthOld.Contains("slay"),
                  $"A3b 同一张卡**不传** `traitSource` ⇒ 含 `strike`、不含 `slay`（实际：[{string.Join(",", synthOld)}]）—— 两列彻底分开，派生 trait 不参与");

        // ---- A4 运行期授予那一半**必须保留**（原版 `AddTraitSilently` 也写 `+0x128`）----
        var granted = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("flying", 1),        // 卡面基准（在 `Card.Keywords` 里）
            new KeyValuePair<string, int>("regeneration", 1),  // 运行期授予（**不在** `Card.Keywords` 里）
        };
        var grantedSprites = Sprites(Badges.For(granted, null, null, null, true, false, null, synth));
        CheckTrue(grantedSprites.Contains("regeneration"),
                  $"A4 ★ 运行期授予的 `regeneration` **仍然画**（实际：[{string.Join(",", grantedSprites)}]）"
                + " —— 原版 `CardScript.AddTraitSilently` 直接追加进 `+0x128`；整表重建时漏掉它就会静默少画");

        // ---- A5 零回归：`traitSource == null` 与旧调用逐项相同 ----
        var r1 = Badges.For(synth.Keywords, null, synth.NumericKeywords, null, true, false, synth);
        var r2 = Badges.For(synth.Keywords, null, synth.NumericKeywords, null, true, false, synth, null);
        CheckTrue(r1.Count == r2.Count && string.Join(",", Sprites(r1)) == string.Join(",", Sprites(r2)),
                  "A5 零回归：`traitSource: null` 与改前的调用**逐项相同**（夹具 / 合成卡 / 旧表都走这一档）");

        // ---- A6 全池：`strike` 徽标「改前 − 改后」= 1（就是 AM72 那一枚）----
        if (pool == null || pool.Count == 0)
        {
            CheckTrue(false, "A6 **前提不成立**：卡池加载为空 —— 这条**没有执行**，不是通过");
        }
        else
        {
            var before = new List<string>();
            var after = new List<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (Sprites(Badges.For(c.Keywords, null, c.NumericKeywords, null, true, false, c)).Contains("strike")) before.Add(c.Id);
                if (Sprites(Badges.For(c.Keywords, null, c.NumericKeywords, null, true, false, c, c)).Contains("strike")) after.Add(c.Id);
            }
            CheckTrue(before.Count - after.Count == 1 && after.IndexOf("AM72") < 0 && before.IndexOf("AM72") >= 0,
                      $"A6 ★ 全池带 `strike` 徽标的卡：改前 {before.Count} → 改后 {after.Count}（差 **1**，就是 `AM72`）"
                    + $"；改后名单 = [{string.Join(",", after)}]");
        }
    }

    // ==================================================================
    //  B 组 · `PoseFor` 的第三支（0.75 / 0.645）
    // ==================================================================
    static void GroupB_PoseForThirdBranch()
    {
        Section("B · `B142-POSEFOR-THIRD`：`PoseScale3DBranch(warlord, isUnit, foe)`（判据 `BattleManager__GetUnitSizeInPlay.c:16-73`）");

        float inMy = BattleDriver.PoseScale3DBranch(false, false, false);
        float inFoe = BattleDriver.PoseScale3DBranch(false, false, true);
        CheckTrue(Mathf.Approximately(inMy, 0.75f) && Mathf.Approximately(inFoe, ArenaSlots.InPlayScale(true)),
                  $"B1 ★ **第三支**（非部队 / 非督军的在场卡 —— defence 39 / enchantment 2）：我方 {inMy:F4}（原版 0.75）· "
                + $"敌方 {inFoe:F4}（原版 0.75 × 0.86 = 0.645）");

        CheckTrue(Mathf.Approximately(BattleDriver.PoseScale3DBranch(false, false, false), ArenaSlots.MinionInPlayScale)
                  && Mathf.Approximately(BattleDriver.PoseScale3DBranch(false, false, true), ArenaSlots.MinionInPlayScale * ArenaSlots.EnemyUnitsScaleRatio),
                  "B2 逐值复核：第三支 = `MinionInPlayScale(0.75)` × `(enemy ? EnemyUnitsScaleRatio(0.86) : 1)`");

        float troopMy = BattleDriver.PoseScale3DBranch(false, true, false);
        float troopFoe = BattleDriver.PoseScale3DBranch(false, true, true);
        CheckTrue(Mathf.Approximately(troopMy, 0.36f) && Mathf.Approximately(troopFoe, 0.69f),
                  $"B3 **部队支不变**（原版 `MinionArea.desiredScale`，MB 4372/4373 实读 0.36 / 0.69）：我方 {troopMy:F4} · 敌方 {troopFoe:F4}");

        float heroMy = BattleDriver.PoseScale3DBranch(true, true, false);
        float heroFoe = BattleDriver.PoseScale3DBranch(true, true, true);
        CheckTrue(Mathf.Approximately(heroMy, 0.40f) && Mathf.Approximately(heroFoe, 0.77f),
                  $"B4 **督军支不变**（原版 `heroScale`）：我方 {heroMy:F4} · 敌方 {heroFoe:F4}");

        CheckTrue(!Mathf.Approximately(inMy, ArenaSlots.CardScale(false)) && !Mathf.Approximately(inFoe, ArenaSlots.CardScale(true)),
                  "B5 ★ 灭自证：第三支 **≠ 部队支**（0.75≠0.36 · 0.645≠0.69）"
                + " —— 把 `if (!isUnit)` 那一行删掉、退化到 `CardScale`，B1/B2/B5 全红");

        CheckTrue(Mathf.Approximately(BattleDriver.PoseScale3DBranch(true, false, false), ArenaSlots.HeroScale(false)),
                  "B6 分支次序照原版：`param_3 == 10`（督军）在第三支**之前**判 ⇒ 督军旗标优先（`:70` 早于 `:31-52`）");
    }

    // ---------------------------------------------------------------- 夹具 / 小工具
    /// <summary>合成卡（同 `B142_E3_Assert.MakeCard` 的形状）：只填本件要用的那几列。</summary>
    static CardDef MakeCard(string id, string name, IEnumerable<string> keywords)
    {
        var c = new CardDef(id, name, "unit", "", null, "Ultramarines", 2, 1, 5, 0, keywords,
                            subtype: "Infantry", hasActiveAbility: 0);
        return c;
    }

    /// <summary>徽标的**图名**集（`Badges.Norm` 归一 —— 与 `InPlayIconKeys` 同一把尺子）。</summary>
    static HashSet<string> Sprites(List<Badge> list)
    {
        var s = new HashSet<string>();
        if (list == null) return s;
        for (int i = 0; i < list.Count; i++)
        {
            string n = Badges.Norm(list[i].sprite);
            if (!string.IsNullOrEmpty(n)) s.Add(n);
        }
        return s;
    }

    static bool HasTraitNamed(CardDef c, string name)
    {
        var ts = c.CardTraits;
        if (ts == null) return false;
        for (int i = 0; i < ts.Count; i++)
            if (ts[i] != null && ts[i].Name == name) return true;
        return false;
    }

    static bool HasKeywordNamed(CardDef c, string name)
    {
        var ks = c.Keywords;
        return ks != null && ks.ContainsKey(name);
    }

    static string KeysOf(IReadOnlyDictionary<string, int> ks)
    {
        if (ks == null) return "null";
        var l = new List<string>();
        foreach (var kv in ks) l.Add(kv.Key);
        l.Sort();
        return string.Join(",", l);
    }

    static string TraitNamesOf(CardDef c)
    {
        var ts = c.CardTraits;
        if (ts == null) return "null";
        var l = new List<string>();
        for (int i = 0; i < ts.Count; i++) if (ts[i] != null) l.Add(ts[i].Name);
        return string.Join(",", l);
    }

    // ---------------------------------------------------------------- 输出
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B142_E26 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B142_E26 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
