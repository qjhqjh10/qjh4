// B99A3_Assert.cs — 第九十九会话 · 波 3 · 组 E · 执行代理 `B99_A3`
//   施工单 = `资料/完全复刻/判据/B99_R3a_施工单_战斗按文件.md` §二「组 E」（7 条）。
//
// ============================ 它盯的是什么 ============================
//  ① **AUD6 · `Spell = 3`**（施工单 §五 #6：原版有这一档枚举、**零行为差异**）
//     · 我们的 `AttackKind` 值域**逐值等于**原版 `AttackTypes`
//       （`dump.cs:24798-24807` = `None0 / Melee1 / Ranged2 / Spell3 / Active4`）；
//     · `AttackSelector.KindOf(3)` **必须是 `None`**（⛔ 别加 `case 3` —— 原版从不产 3，
//       加了会让「引擎给出 3」的那一帧凭空多出一颗可点/可亮的钮）；
//     · 引擎那一格 `BattleDriver.OwnAttackTypeOf`（private static）**3 → `AttackTypes.None`**；
//     · 卡面：喂 3 ⇒ **两颗底光都不亮、两个数值都不 × `AttackScaleModifier`（1.35）**
//       （原版 `HighlightAttackType__HighlightIfNeeded.c:53` = **逐实例比 `attackType`**，
//        而卡面只有 Melee / Ranged 两个实例 ⇒ spell / active **没有实例** ⇒ 都不亮）。
//  ② **#6 `sneakAttack` 的顺手项**：`CardText.TraitTerm` 拼的是 `Card_Trait/<canonical>`，
//     我们的 canonical = `sneakattack`（`CardDef.cs:7317` 的 `KeywordTable.SneakAttack`），
//     **官方那条键叫 `Card_Trait/sneak`** ⇒ 别名键已补进 `Loc.cs` 的 trait 表
//     （两列值**逐字照抄官方**，不是我们编的）。
//     同时钉死「**⛔ 别把 `sneakattack` 加进徽标白名单**」—— 原版既没有这个 `CardTrait` SO、
//     也没有那张图（`Badges` 的 47 / 59 两条表都是从原版数据现算的）。
//  ③ **#31 `CanApply` 防回归**：原版**没有**统一的可施用谓词（ILSpy 全树 0 文件）⇒
//     全仓**不许**出现一个叫 `CanApply` 的方法（真建 = 同一条规则写两遍，见 CLAUDE.md §三）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B99A3_Assert.Run -logFile "d:/4/_tmp_view/b99a3.log"
//   🔴 **接线由调度台做**：本文件**没有**挂到任何自检宿主（`Editor/*.cs` 是别人的文件）。
//   判绿 = 小结那一行 `✗ 0`；日志里出现 `… 未验到：` 时**别读成通过**（那是夹具缺件，不是实现错）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `AttackKind.Spell` 的值改成 3 以外的数（或漏写 `Spell`）⇒ §A 两条红。
//   · 在 `AttackSelector.KindOf` 里加 `case 3: return AttackKind.Spell;` ⇒ §B 那条红。
//   · 把 `BattleDriver.OwnAttackTypeOf` 的 `default:` 改成任何一个具体打法 / 加 `case 3` ⇒ §C 红。
//   · 把 `CardView.HighlightAttackTypes` 改成「`OwnAttackType != None` ⇒ 两颗一起亮」⇒
//     §D 的「Spell 档两颗底光都灭」与「两个数值都不 ×倍率」一起红（**这是本段最该防的那一改**）。
//   · 把 `Loc.cs` 那条 `Card_Trait/sneakattack` 删掉（或把中文/英文列改成自译词）⇒ §E 红。
//   · 往 `Badges.InPlayIconKeys` / `CardIconKeys` 加 `sneakattack` ⇒ §F 红
//     （加了会去画一张原版不存在的图 —— `SpriteOf` 返 null 就 `continue`，等于白加）。
//   · 真新建一个 `CanApply(...)` ⇒ §G 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static class B99A3_Assert
{
    const string P = "B99A3 ";

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    /// <summary>出声的「**未验到**」—— ⛔ 别把它读成通过（本仓坑表 #18：断言缺前提时静默跳过）。</summary>
    static void NotVerified(string label) { Debug.LogWarning(P + "… 未验到：" + label); }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "===== B99_A3（施工单组 E：Spell=3 / #6 别名键 / #31 防回归）开始 =====");

        CheckEnumValues();
        CheckSelectorKindOf();
        CheckEngineAttackTypeMapping();
        CheckCardFaceSpellNoHighlight();
        CheckSneakAttackLoc();
        CheckBadgeWhitelistHasNoSneak();
        CheckNoUnifiedCanApply();

        Debug.Log(P + $"===== 合计：{_ok} 通过 / {_bad} 失败 =====");
    }

    // ==================================================================
    //  §A 枚举值域 = 原版 `AttackTypes`（逐值）
    // ==================================================================
    static void CheckEnumValues()
    {
        Check((int)AttackKind.None == 0
              && (int)AttackKind.Melee == 1
              && (int)AttackKind.Ranged == 2
              && (int)AttackKind.Spell == 3
              && (int)AttackKind.Ability == 4,
              "§A① `AttackKind` 值域 = `0/1/2/3/4`（原版 `AttackTypes` 逐值："
            + "`dump.cs:24798-24807`；实测 "
            + $"{(int)AttackKind.None}/{(int)AttackKind.Melee}/{(int)AttackKind.Ranged}/"
            + $"{(int)AttackKind.Spell}/{(int)AttackKind.Ability}）"
            + "｜🧨 漏写 `Spell = 3` ⇒ 红");

        Check((int)AttackKind.Spell == (int)AttackTypes.Spell
              && (int)AttackKind.Ability == (int)AttackTypes.Active,
              "§A② 两份枚举**同值**：`AttackKind.Spell == AttackTypes.Spell`(3) ∧ "
            + "`AttackKind.Ability == AttackTypes.Active`(4)（名字不同是历史包袱，**值**必须一致 —— "
            + "不然「引擎那一格」与「选择器那一格」各说各话）");
    }

    // ==================================================================
    //  §B `AttackSelector.KindOf(3)` 必须落 `None`（⛔ 别加 `case 3`）
    // ==================================================================
    static void CheckSelectorKindOf()
    {
        var mi = typeof(AttackSelector).GetMethod("KindOf", BindingFlags.NonPublic | BindingFlags.Static);
        Check(mi != null, "§B（前提）反射找得到 `AttackSelector.KindOf(int)`（private static）");
        if (mi == null) { NotVerified("§B 整段（找不到 `KindOf` —— 改签名了？）"); return; }

        Check(KindOf(mi, 1) == AttackKind.Melee
              && KindOf(mi, 2) == AttackKind.Ranged
              && KindOf(mi, 4) == AttackKind.Ability,
              "§B① 正控：`KindOf(1)=Melee` / `(2)=Ranged` / `(4)=Ability`（4 = 原版的 `Active`）");

        Check(KindOf(mi, 0) == AttackKind.None,
              "§B② `KindOf(0) = None`（原版「还没选过」那一档，⛔ 不许给默认打法）");

        Check(KindOf(mi, 3) == AttackKind.None,
              "§B③ `KindOf(3) = None`（原版 `Spell(3)` 引擎从不产、卡面也没有它的高亮实例；"
            + $"实测 {KindOf(mi, 3)}）｜🧨 加 `case 3: return AttackKind.Spell;` ⇒ 红");
    }

    static AttackKind KindOf(MethodInfo mi, int attackType)
    {
        return (AttackKind)mi.Invoke(null, new object[] { attackType });
    }

    // ==================================================================
    //  §C 引擎那一格：`BattleDriver.OwnAttackTypeOf` 把 3 显式落 `None`
    // ==================================================================
    static MethodInfo OwnAttackTypeOfMi;

    static AttackTypes OwnAttackTypeOf(UnitState u)
    {
        if (OwnAttackTypeOfMi == null)
            OwnAttackTypeOfMi = typeof(BattleDriver).GetMethod("OwnAttackTypeOf",
                                                             BindingFlags.NonPublic | BindingFlags.Static);
        if (OwnAttackTypeOfMi == null) return AttackTypes.None;
        return (AttackTypes)OwnAttackTypeOfMi.Invoke(null, new object[] { u });
    }

    static void CheckEngineAttackTypeMapping()
    {
        OwnAttackTypeOfMi = typeof(BattleDriver).GetMethod("OwnAttackTypeOf",
                                                         BindingFlags.NonPublic | BindingFlags.Static);
        Check(OwnAttackTypeOfMi != null,
              "§C（前提）反射找得到 `BattleDriver.OwnAttackTypeOf(UnitState)`（private static）");
        if (OwnAttackTypeOfMi == null) { NotVerified("§C 整段（找不到 `OwnAttackTypeOf` —— 改签名了？）"); return; }

        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, $"§C（前提）卡表读出来了（{(pool == null ? 0 : pool.Count)} 张）");
        if (pool == null || pool.Count == 0) { NotVerified("§C 整段（卡表空）"); return; }
        var def = pool[0];

        var u = new UnitState(def, false);

        u.LastAttackType = UnitState.AttackTypeMelee;
        Check(OwnAttackTypeOf(u) == AttackTypes.Melee, "§C① 正控：`LastAttackType = 1` ⇒ `AttackTypes.Melee`");
        u.LastAttackType = UnitState.AttackTypeRanged;
        Check(OwnAttackTypeOf(u) == AttackTypes.Ranged, "§C② 正控：`LastAttackType = 2` ⇒ `AttackTypes.Ranged`");
        u.LastAttackType = UnitState.AttackTypeActive;
        Check(OwnAttackTypeOf(u) == AttackTypes.Active,
              "§C③ `LastAttackType = 4` ⇒ `AttackTypes.Active`（原版这档也没有底光实例 ⇒ 实际两颗都不亮，"
            + "由 §D 的卡面段兜住）");

        u.LastAttackType = 3;   // 原版 `Spell` —— ⛔ `UnitState` 故意没有 `AttackTypeSpell` 常量（引擎从不产）
        Check(OwnAttackTypeOf(u) == AttackTypes.None,
              $"§C④ `LastAttackType = 3`（原版 `Spell`）⇒ `AttackTypes.None`（实测 {OwnAttackTypeOf(u)}）"
            + "｜🧨 把 `default:` 改成具体打法、或加 `case 3` ⇒ 红");

        u.LastAttackType = 0;
        Check(OwnAttackTypeOf(u) == AttackTypes.None, "§C⑤ `LastAttackType = 0`（没打过）⇒ `AttackTypes.None`");
    }

    // ==================================================================
    //  §D 卡面：Spell 档 ⇒ 两颗底光都不亮、两个数值都不 ×1.35
    //     判据 = `HighlightAttackType__HighlightIfNeeded.c:53`（逐实例比 attackType）
    // ==================================================================
    static void CheckCardFaceSpellNoHighlight()
    {
        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "§D（前提）卡表读出来了");
        if (pool == null || pool.Count == 0) { NotVerified("§D 整段（卡表空）"); return; }

        // 挑一张近战 / 远程都 > 0 的单位卡（卡面那两格数值才建得出来）
        CardDef def = null;
        CardData data = default(CardData);
        for (int i = 0; i < pool.Count; i++)
        {
            var d = BattleDriver.ToCardData(pool[i], pool[i].Faction);
            if (d.melee > 0 && d.ranged > 0) { def = pool[i]; data = d; break; }
        }
        Check(def != null, "§D（前提）卡池里找得到一张「近战 > 0 ∧ 远程 > 0」的卡（卡面两格数值要用）");
        if (def == null) { NotVerified("§D 整段（夹具挑不到卡）"); return; }

        var root = new GameObject("chk_b99a3_spell");
        var prevMode = CardTween.Mode;
        try
        {
            // ⚠️ `CardTween.Mode` 必须在**建补间之前**切 `Manual`（`CardTween.Use` 建的时候读它）
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            var v = CardView.Create(root.transform, data, "chk_b99a3_spell_card", CardFace.Board);
            // 预备：先把两格数值层 / 两颗底光建出来并捕获基准（原版 `originalSize` 惰性捕获）
            v.SetOwnAttackType(AttackTypes.None);
            CardTween.Advance(0.2f);

            bool statsOk = v.StatShown(CardView.StatMelee) >= 0 && v.StatShown(CardView.StatRanged) >= 0;
            Check(statsOk, $"§D（前提）两格数值层建出来了（`{def.Name}`；"
                         + $"melee={v.StatShown(CardView.StatMelee)} / ranged={v.StatShown(CardView.StatRanged)}）"
                         + "—— 不在 ⇒ 下面几条**没验到**，别读成通过（多半是 `PragatiDigits.Available` 为假）");
            if (!statsOk) NotVerified("§D 的数值段（两格数值层没建出来）");

            bool glowOk = v.AttackGlowBuilt;
            Check(glowOk, "§D（前提）两颗底光（`40K_melee_glow` / `40K_ranged_glow`）建出来了"
                        + "—— 不在 ⇒ 下面两条**没验到**，别读成通过（缺图那档只出声不抛）");
            if (!glowOk) NotVerified("§D 的底光段（图没建出来）");

            float mult = v.AttackScaleModifier;

            // ── D1 经**引擎那一格**喂 3（`LastAttackType = 3` ⇒ `OwnAttackTypeOf` ⇒ 卡面）──────
            var u = new UnitState(def, false) { LastAttackType = 3 };
            var atkOf3 = OwnAttackTypeOf(u);
            Check(atkOf3 == AttackTypes.None,
                  $"§D① 喂 `LastAttackType=3` ⇒ 引擎映射落 `{atkOf3}`（= `None`）");
            v.SetOwnAttackType(atkOf3);
            CardTween.Advance(0.2f);
            AssertBothUnlit(v, "§D② 喂 3 之后（经引擎链）", statsOk);

            // ── D2 直接把 `Spell(3)` 喂进卡面（防「将来某个调用点直接传 Spell」）──────────────
            v.SetOwnAttackType(AttackTypes.Spell);
            CardTween.Advance(0.2f);
            Check(v.OwnAttackType == AttackTypes.Spell,
                  $"§D③ `SetOwnAttackType(Spell)` 之后读回 = Spell（实测 {v.OwnAttackType}）");
            AssertBothUnlit(v, "§D④ 直接喂 `Spell(3)`", statsOk);

            // ── D3 正控：喂 1 ⇒ 近战那格亮 + 数值 ×倍率、远程那格回基准 ──────────────────────
            u.LastAttackType = UnitState.AttackTypeMelee;
            var atkOf1 = OwnAttackTypeOf(u);
            v.SetOwnAttackType(atkOf1);
            CardTween.Advance(0.2f);
            if (statsOk)
            {
                float baseM = v.StatBaseScale(CardView.StatMelee).x;
                float nowM = v.StatScaleNow(CardView.StatMelee).x;
                Check(baseM > 1e-4f && Mathf.Abs(nowM / baseM - mult) < 1e-3f,
                      $"§D⑤ 正控：喂 1 ⇒ 近战数值 = 基准 × {mult:F2}（基准 {baseM:F4} → 现在 {nowM:F4}）"
                    + "｜🧨 这条红 = 上面那几条「都不亮」的结论**没意义**（读口看不见「亮」）");
                float baseR = v.StatBaseScale(CardView.StatRanged).x;
                float nowR = v.StatScaleNow(CardView.StatRanged).x;
                Check(baseR > 1e-4f && Mathf.Abs(nowR / baseR - 1f) < 1e-3f,
                      $"§D⑦ 正控：喂 1 ⇒ 远程那格回基准（基准 {baseR:F4} → 现在 {nowR:F4}）");
            }
            if (glowOk)
                Check(v.AttackGlowOn(false) && !v.AttackGlowOn(true),
                      "§D⑥ 正控：喂 1 ⇒ **只亮近战那颗**（实测 glow=(近距离="
                    + $"{v.AttackGlowOn(false)}, 远程={v.AttackGlowOn(true)})）");
        }
        finally
        {
            CardTween.Mode = prevMode;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>「两颗底光都不亮 + 两个数值都**不是**基准 × 倍率」—— Spell 档的唯一可观测判据。</summary>
    static void AssertBothUnlit(CardView v, string tag, bool numericOk)
    {
        float mult = v.AttackScaleModifier;
        if (numericOk)
        {
            float bM = v.StatBaseScale(CardView.StatMelee).x, nM = v.StatScaleNow(CardView.StatMelee).x;
            float bR = v.StatBaseScale(CardView.StatRanged).x, nR = v.StatScaleNow(CardView.StatRanged).x;
            float rM = bM > 1e-4f ? nM / bM : -1f, rR = bR > 1e-4f ? nR / bR : -1f;

            Check(rM > 0f && Mathf.Abs(rM - 1f) < 1e-3f && Mathf.Abs(rM - mult) > 0.05f,
                  $"{tag}：近战数值 **不 ×{mult:F2}**（基准 {bM:F4} → 现在 {nM:F4}，比值 {rM:F4}）"
                + "｜🧨 `HighlightAttackTypes` 改成「!= None 就两颗一起亮」⇒ 红");
            Check(rR > 0f && Mathf.Abs(rR - 1f) < 1e-3f && Mathf.Abs(rR - mult) > 0.05f,
                  $"{tag}：远程数值 **不 ×{mult:F2}**（基准 {bR:F4} → 现在 {nR:F4}，比值 {rR:F4}）");
        }
        else NotVerified($"{tag} 的数值段（两格数值层没建出来 —— 别读成通过）");

        if (v.AttackGlowBuilt)
            Check(!v.AttackGlowOn(false) && !v.AttackGlowOn(true),
                  $"{tag}：**两颗底光都不亮**（实测 glow=(近距离={v.AttackGlowOn(false)}, "
                + $"远程={v.AttackGlowOn(true)})）｜🧨 原版那两个 `HighlightAttackType` 实例只有 Melee/Ranged "
                + "⇒ Spell/Active 一颗都不该亮");
        else
            NotVerified($"{tag} 的底光段（两颗底光没建出来）");
    }

    // ==================================================================
    //  §E `sneakattack` 这个 canonical 键**认得出官方名**（#6 顺手项）
    // ==================================================================
    static void CheckSneakAttackLoc()
    {
        Check(KeywordTable.SneakAttack == "sneakattack",
              "§E①（前提）我们的 canonical 键 = `sneakattack`（实测 "
            + $"`{KeywordTable.SneakAttack}`）—— 它决定 `CardText.TraitTerm` 拼出 `Card_Trait/sneakattack`");

        string key = "Card_Trait/" + KeywordTable.SneakAttack;
        Check(Loc.HasEntry(key), $"§E② `Loc` 里登记了 `{key}`（别名键；官方那条叫 `Card_Trait/sneak`）");
        string zh = CardText.KeywordZh(KeywordTable.SneakAttack);
        Check(zh == "潜行攻击",
              "§E③ `KeywordZh(sneakattack)` = 「潜行攻击」（实测「" + (zh == null ? "<null>" : zh) + "」）"
            + "｜🧨 删掉那条 Loc 条目 ⇒ 红");
        Check(CardText.KeywordEn(KeywordTable.SneakAttack) == "Sneak Attack",
              "§E④ `KeywordEn = Sneak Attack`（官方英文列）");

        // 独立交叉核对：两列值必须 = 官方 13 语言表的 zh / en（不是我们编的）
        Check(Loc.HasBulk("Card_Trait/sneak"),
              "§E⑤（前提）官方表里有 `Card_Trait/sneak` —— 我们这条别名的**出处**");
        string offZh = OfficialLang("Card_Trait/sneak", "Chinese");
        string offEn = OfficialLang("Card_Trait/sneak", "English");
        if (offZh == null || offEn == null)
            NotVerified("§E 的交叉核对（取不到官方 13 语言表；多半是 `Resources/official_loc_13lang.json` 缺）");
        else
            Check(offZh == "潜行攻击" && offEn == "Sneak Attack"
                  && CardText.KeywordZh(KeywordTable.SneakAttack) == offZh
                  && CardText.KeywordEn(KeywordTable.SneakAttack) == offEn,
                  $"§E⑥ 两列值**逐字等于官方**（官方 zh=「{offZh}」/ en=「{offEn}」）"
                + "｜🧨 手打一个自译词 ⇒ 红");
    }

    /// <summary>官方 13 语言表取一条（`Loc.Lang13Text` 是 private static ⇒ 反射；⛔ 不改 `Loc` 的可见性）。</summary>
    static string OfficialLang(string key, string langName)
    {
        var mi = typeof(Loc).GetMethod("Lang13Text", BindingFlags.NonPublic | BindingFlags.Static);
        if (mi == null) return null;
        var langType = Loc.Current.GetType();
        object lang;
        try { lang = Enum.Parse(langType, langName); } catch { return null; }
        try { return mi.Invoke(null, new object[] { key, lang }) as string; } catch { return null; }
    }

    // ==================================================================
    //  §F 徽标白名单**不含** `sneakattack`（#6：原版既没这个 SO、也没那张图）
    // ==================================================================
    static void CheckBadgeWhitelistHasNoSneak()
    {
        var inPlay = Badges.InPlayIconKeysForTest;
        var card = Badges.CardIconKeysForTest;
        Check(inPlay != null && inPlay.Length == 47,
              $"§F① 场上白名单仍 47 键（实测 {(inPlay == null ? -1 : inPlay.Length)}；"
            + "原版 `GetTraitInPlayListForIcons` 现算的 `hasIcon ∧ isInPlayIcon ∧ traitIcon` 那一族）");
        Check(card != null && card.Length == 59,
              $"§F② 卡面白名单仍 59 键（实测 {(card == null ? -1 : card.Length)}）");
        bool hasInPlay = inPlay != null && Array.IndexOf(inPlay, "sneakattack") >= 0;
        bool hasCard = card != null && Array.IndexOf(card, "sneakattack") >= 0;
        Check(!hasInPlay && !hasCard,
              "§F③ 两张白名单都**不含** `sneakattack`（原版 73 个 `CardTrait` SO / 78 张图集里都没有它）"
            + "｜🧨 加进去 ⇒ 红（`SpriteOf` 返 null ⇒ 白加，还让「与原版对齐」这句话变成假的）");
        Check(!Badges.PlaysInPlayIcon("sneakattack") && !Badges.PlaysCardIcon("sneakattack"),
              "§F④ 两个筛选器对 `sneakattack` 都返 false（不是只改了表、筛选器还漏一条路）");
    }

    // ==================================================================
    //  §G 全仓**没有**统一可施用谓词 `CanApply`（#31 防回归）
    // ==================================================================
    static void CheckNoUnifiedCanApply()
    {
        var hits = new List<string>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            catch { continue; }
            if (types == null) continue;
            foreach (var t in types)
            {
                if (t == null) continue;
                MethodInfo[] ms;
                try
                {
                    ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                                    | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch { continue; }
                foreach (var m in ms)
                    if (m != null && m.Name == "CanApply") hits.Add(t.FullName + "." + m.Name + "()");
            }
        }
        Check(hits.Count == 0,
              "§G 全仓**没有** `CanApply` 方法（原版 ILspy 全树 0 文件；判定按机制各写一份 = 47 个 `Can*`/`IsValid*`）"
            + $"｜实测 {hits.Count} 处" + (hits.Count > 0 ? "：" + string.Join(", ", hits) : "")
            + "｜🧨 真建一个 ⇒ 与 15 个原版同名判据「同一条规则写两遍」");
    }
}
