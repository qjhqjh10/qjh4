// C7_Assert.cs — 执行代理 `C7`（第一百零九会话 · 2026-10-31）
//
// 判据 = `Unity/资料/完全复刻/判据/B109_R19_poisoned_effectText.md` §2·2 A / B。
//
// ============================ 它盯的是什么（两件）============================
//  A **真缺陷：`Card_Abilities/*` 右列的字体闸漏了。**
//    `CardDisplayWindow.TryAddEffectTextRow` 原来自己写了一遍 `Loc.T(key)` + `RewriteMarks(…, Loc.Current == Chinese)`，
//    **没有** `SkillPanel.AbilityRaw` 里那道 `CardText.Zh` 字体闸 ⇒ 「语档 = 中文 + 中文字体资产缺失」这一档
//    会把中文列打成**方块**（另一条路不会）。⇒ §A / §B 用 `Loc.RestoreForTest(Chinese)` +
//    `AbilityText.SetZhGateForTest(false)` 把那一档**造出来**，直接断输出。
//  B **抽共用件**：取词 + 记号渲染收成 `Core/AbilityText.cs` 一份，`SkillPanel` 与 `CardDisplayWindow` 都转调它。
//    ⇒ §C 用反射调 `SkillPanel` 的私有静态口，断它与共用件**逐字一致**（⛔ 防止有人再写回一份）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · 在 `Editor/BattleScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：{pass} 通过 …`）**同一物理行**
//    的合计之前追加：`C7_Assert.Run(); pass += C7_Assert.Pass; fail += C7_Assert.Fail;`
//    —— **同一行追加 ⇒ 行数零变化**（铁律 18·b）。🔴 追加的文本必须落在该行**第一个 `//` 之前**
//    （2026-10-30 的坑：追加到行末 `//` 之后 = 代码被注释吞掉，typecheck 全绿但一条断言没跑）。
//  · 也可以单独跑：`-executeMethod C7_Assert.Run`。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · `AbilityText.Raw` 去掉字体闸 ⇒ §A②③ 红（gate=false 时会返回中文列）。
//  · `TryAddEffectTextRow` 改回裸 `Loc.T` + `RewriteMarks(…, Loc.Current == Chinese)` ⇒ §B① 红
//    —— 那就是**改前**的行为，而期望值来自 `Loc.OfficialEnglish` + `CardIcons.RewriteMarks(en,false)`
//    （**两个来源都与共用件无关**）⇒ 把两边一起改回旧写法也照样红。
//  · `SkillPanel` 私自留一份实现（不转调共用件）⇒ §C 红（gate=false 时反射拿到的是中文列）。

using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class C7_Assert
{
    const string P = "C7 ";

    const string TestKey = "Card_Abilities/EffectPledge";   // 现读：en = `[[slay]]: Heal 2 and gain a [[markOfSlaanesh]]` · zh = `[[slay]]：恢复 2 点生命值并获得一个[[markOfSlaanesh]]`（两列都带记号、且不同）

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "===== C7（`Card_Abilities/*` 字体闸 + 取词/渲染共用件）开始 =====");

        var origLang = Loc.Current;
        try
        {
            Loc.RestoreForTest(AvailableLanguages.Chinese);

            string zh, en;
            CheckColumns(out zh, out en);
            CheckRawGate(zh, en);
            CheckMarkedGate(zh, en);
            CheckRowsOfEffects(zh, en);
            CheckSkillPanelForwards(zh, en);
            CheckMissingKey();
        }
        finally
        {
            // ⛔ 一条都不许留：语言 / 闸的覆盖都要还原（`Loc.RestoreForTest` 不动 PlayerPrefs）
            AbilityText.SetZhGateForTest(null);
            Loc.RestoreForTest(origLang);
        }

        Debug.Log(P + $"===== 合计：{_ok} 通过 / {_bad} 失败 =====");
    }

    // ==================================================================
    //  §A① 前置：这个键的中英两列**都非空且不同**（否则后面的闸测不出差别）
    //  ⚠️ 先写前提断言、再写被测断言（本仓坑表 #18：`if (前提) Check(结论)` = 前提不成立时静默跳过）
    // ==================================================================
    static void CheckColumns(out string zh, out string en)
    {
        zh = Loc.T(TestKey);
        en = Loc.OfficialEnglish(TestKey);
        Check(!string.IsNullOrEmpty(zh) && zh != TestKey
              && !string.IsNullOrEmpty(en) && en != TestKey
              && zh != en,
              "§A① 前置：`" + TestKey + "` 官方表里中英两列都非空且不同（现读 zh=`"
              + zh + "` · en=`" + en + "`）");
    }

    // ==================================================================
    //  §A②③④ `AbilityText.Raw` 的字体闸
    // ==================================================================
    static void CheckRawGate(string zh, string en)
    {
        AbilityText.SetZhGateForTest(false);     // = 中文档 + 中文字体资产缺失（`CardText.Zh == false`）
        Check(AbilityText.Raw(TestKey) == en,
              "§A② 字体闸（缺中文字体）⇒ 取官方**英文**列（= `Loc.OfficialEnglish`）");
        Check(AbilityText.Raw(TestKey) != zh,
              "§A③ 字体闸（缺中文字体）⇒ **不许**返回中文列 —— 那就是改前 `TryAddEffectTextRow` 的缺陷");

        AbilityText.SetZhGateForTest(true);      // = 中文档 + 字体可用
        Check(AbilityText.Raw(TestKey) == zh,
              "§A④ 中文可用 ⇒ 取当前语档列（= `Loc.T`）");
    }

    // ==================================================================
    //  §A⑤⑥ `AbilityText.Marked` 的 `zh` 位跟着同一道闸走
    // ==================================================================
    static void CheckMarkedGate(string zh, string en)
    {
        AbilityText.SetZhGateForTest(false);
        Check(AbilityText.Marked(zh) == CardIcons.RewriteMarks(zh, false),
              "§A⑤ 记号渲染的 `zh` 位（缺字体档）= `false` ⇒ 记号里的子词条取英文列（= 原版 `ModifyLocalization` 的 `zh=false` 那一支）");

        AbilityText.SetZhGateForTest(true);
        Check(AbilityText.Marked(zh) == CardIcons.RewriteMarks(zh, true),
              "§A⑥ 记号渲染的 `zh` 位（中文可用档）= `true`");

        Check(AbilityText.Marked(null) == null && AbilityText.Marked("") == "",
              "§A⑦ `Marked` 空输入原样返回（⛔ 不是抛/不是空串变 null）");
    }

    // ==================================================================
    //  §B **产品路径**回归锁：`CardDisplayWindow.RowsOfEffects` 的右列第一跳
    //     （改前它自己写了一遍、漏了字体闸；这一节断的是**真实调用链**，不是共用件本身）
    // ==================================================================
    static void CheckRowsOfEffects(string zh, string en)
    {
        var def = MakeCard("EffectPledge");
        var e = new ShownEffect
        {
            Family = ShownFamily.Stat,
            EffectTextCard = def,
            EffectAb = 0,
            EffectLg = 0,
            EffectIsHand = false,
            Source = "C7Source",
        };

        AbilityText.SetZhGateForTest(false);
        var rows = CardDisplayWindow.RowsOfEffects(new[] { e }, true);
        Check(rows.Count == 1,
              "§B① 前置：右列第一跳命中 ⇒ 恰好一行（实得 " + rows.Count + "）");
        if (rows.Count == 1)
        {
            Check(rows[0].What == CardIcons.RewriteMarks(en, false),
                  "§B② 【真缺陷回归锁】中文档 + 缺中文字体 ⇒ 右列 = 官方**英文**列"
                + "（改前这里是 `RewriteMarks(Loc.T(key), zh=true)` = 中文方块）");
            Check(rows[0].Who == "C7Source",
                  "§B③ 左列 = `ShownEffect.Source`（原版 `CardEffect.enchantingCard`）");
        }

        AbilityText.SetZhGateForTest(true);
        var rowsZh = CardDisplayWindow.RowsOfEffects(new[] { e }, true);
        Check(rowsZh.Count == 1 && rowsZh[0].What == CardIcons.RewriteMarks(zh, true),
              "§B④ 中文可用 ⇒ 右列 = 中文列（与改前的**正确**那一档逐字相同 = 零回归）");

        // 缺键 ⇒ 印 `effectText` 原文（原版 `GameStaticData.cs:1778-1782`；⛔ 不印键名）
        var def2 = MakeCard("C7NoSuchEffectText");
        var e2 = e; e2.EffectTextCard = def2;
        var rows2 = CardDisplayWindow.RowsOfEffects(new[] { e2 }, true);
        Check(rows2.Count == 1 && rows2[0].What == "C7NoSuchEffectText",
              "§B⑤ 官方表里没有这条键 ⇒ 右列印 `effectText` **原文**（⛔ 不印键名、⛔ 不留空）");
    }

    static CardDef MakeCard(string src)
    {
        var def = new CardDef("C7TEST", "C7 Test", "troop", "", "common", "BlackLegion",
                              0, 0, 1, 0);
        def.AttachAbilityEffectTexts(new[]
        {
            new CardDef.AbilityEffectTextSpec { Ab = 0, Lg = 0, IsHand = false, At = "buffEffect", Src = src },
        });
        return def;
    }

    // ==================================================================
    //  §C **抽共用件**的灭自证：`SkillPanel` 的三个私有静态口必须**转调**共用件
    //     反射调它们，与 `AbilityText.*` 逐字比 —— 谁把实现再写回一份，这里就红。
    // ==================================================================
    static void CheckSkillPanelForwards(string zh, string en)
    {
        var t = typeof(SkillPanel);
        var mRaw = t.GetMethod("AbilityRaw", BindingFlags.NonPublic | BindingFlags.Static);
        var mMarked = t.GetMethod("AbilityMarked", BindingFlags.NonPublic | BindingFlags.Static);
        var mTerm = t.GetMethod("AbilityTerm", BindingFlags.NonPublic | BindingFlags.Static);
        Check(mRaw != null && mMarked != null && mTerm != null,
              "§C① 前置：`SkillPanel` 的 `AbilityRaw/AbilityMarked/AbilityTerm` 都在（反射找得到）");
        if (mRaw == null || mMarked == null || mTerm == null) return;

        AbilityText.SetZhGateForTest(false);
        string rawRefl = (string)mRaw.Invoke(null, new object[] { TestKey });
        Check(rawRefl == AbilityText.Raw(TestKey),
              "§C② `SkillPanel.AbilityRaw` 已转调共用件（缺字体档下两者逐字相同 = 英文列）；"
            + "若它私自留了一份实现，这里会拿到中文列");

        string markedRefl = (string)mMarked.Invoke(null, new object[] { zh });
        Check(markedRefl == AbilityText.Marked(zh),
              "§C③ `SkillPanel.AbilityMarked` 已转调共用件（`zh` 位同一条闸）");

        string termRefl = (string)mTerm.Invoke(null, new object[] { TestKey });
        Check(termRefl == AbilityText.Term(TestKey),
              "§C④ `SkillPanel.AbilityTerm` 已转调共用件");
        Check(termRefl != null && termRefl == CardIcons.RewriteMarks(en, false),
              "§C⑤ `AbilityTerm` 的**内容**也是英文列 + `zh=false` 渲染（⛔ 不是「两边一起返回 null」也算过）");
    }

    // ==================================================================
    //  §D 缺键的三种输入都 ⇒ `null`（⛔ 不返回键名、⛔ 不返回空串）
    // ==================================================================
    static void CheckMissingKey()
    {
        Check(AbilityText.Raw("Card_Abilities/__C7_NoSuchKey__") == null,
              "§D① 官方表里没有的键 ⇒ `null`（⛔ 不印键名，判据 `Loc.T(key) == key`）");
        Check(AbilityText.Raw(null) == null && AbilityText.Raw("") == null,
              "§D② `null` / 空键 ⇒ `null`");
    }
}
