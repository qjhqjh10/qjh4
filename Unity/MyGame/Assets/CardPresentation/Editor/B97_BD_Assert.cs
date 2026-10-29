// B97_BD_Assert.cs — 第九十七会话 · 执行代理 `B97_BD`（2026-10-29）
//
// ============================ 它盯的是什么 ============================
// 本代理在 `CardPresentation/Battle/BattleDriver.cs` 落地的两件（施工单 →
// `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·5 的 `:255` ① 与 `:292`）：
//
//  · **账 `战斗逻辑驱动:255` ①**（`BattleManager.ChoiceOfCardPlayer` ⇒ `BattleManager.GetChoiceOptions`）：
//    选牌面板「最多摆几张」的**判据从手抄表换成卡数据** ——
//    `BattleDriver.ChooseShowLimit` 现在读 `CardDef.ChoicesMaxTargets`（原版 `choicesDefinition.maxTargets`），
//    手抄的 21 张 `ChooseShowByCardId` **降级为兜底快照**（只在数据回 `-1` 时才查）。
//    判据全文 = `decomp_full/BattleManager__GetChoiceOptions.c:40-45`
//      （`iVar1 = min(pool.Count, *(int*)(choicesDefinition + 0x40))`；`maxTargets == 0` ⇒ 回空表）。
//    🔴 **本件为什么不断「面板摆几张」**：那一半**已经有主** —— `Editor/BattleScene.cs` 的 §20-b
//      （`A1964`：`GSC65` ⇒ 4、`GSC50` ⇒ 3，两条互锁的灭自证）断的正是它，而它「只断行为、不读那张表」。
//      在这里再断一遍 = 同一件事两份，⛔ 不重复；本件补的是它**断不到的那一半**：
//      「数据这一侧真的一列不缺」（否则兜底表/众数 3 会**静默复活**）。
//
//  · **账 `战斗逻辑驱动:292`**（`BattleManager.DeviceVariables` ⇒ `CardScript.HeroLandIntoField` 的缩放）：
//    督军入场那一刻整卡 **× `VarsDevice.warlordStartSizeFactor`（桌面 1.3）**，落地时再补间回正在位缩放。
//    判据（第一权威，`decomp_full/CardScript._HeroLandIntoField_d__318__MoveNext.c` 逐句）：
//      · state 0 `:42` `DefaultScale = GetUnitSizeInPlay(...)`；`:52` 读 `VarsDevice+0x30`；`:59` 三分量 `mulss` → `set_localScale`
//      · state 1 `:342-400` `waitMult != 1` ⇒ `y += 0.2`；`DOScale(GetUnitSizeInPlay, AttackStepTime = 0.1)` + `SetEase(0x18)`（OutElastic）
//    `+0x30` 的**偏移独立坐实**（不靠符号名）：`VarsDevice__GetCardInHandYOffsetTouchInput.c` 自己写
//    `this+0x1c` / `this+0x20` = `cardInHandShownYOffset` / `…Min`（两个相邻 float）⇒ 往前一格 `+0x18` 是
//    `cardInHandShownScale`、`extraSpaceOnSelectedCard` 指针在 `+0x28` ⇒ `+0x30` 正好是 `warlordStartSizeFactor`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B97_BD_Assert.CheckB97BD(driver);`
//   · **建议插入点 = `CardPresentation/Editor/BattleScene.cs` 的 `driver.ApplyTutorialVisibilityForTest();`
//     （现读 :36520）那一行【之后】、`driver.BeginTutorial(0);`（现读 :36523，「⑤ 督军两拍」的第一句）之【前】**：
//     本件自己会 `BeginTutorial(0)` 并由第二拍驱动 `HeroLandIntoField`，紧接着宿主那一句又把同一条
//     开场重置一遍（`BattleDriver.cs:4738` 会 `_tutWarlordBeat=0/_tutBeatDone=false`）⇒ 对它后面那三条断言（一藏一显）**逐字无害**。
//   · **不跑 Unity 批处理**、不建持久资产；只推时间线 `0.31 s`（= 段0 的 `WaitForSeconds(1 × 0.3)`）与
//     补间 `0.3 s` —— 两者都是表现泵，`animateFeel` 用完还原。
//   · ⛔ 不传 `driver` 也行（会 `FindFirstObjectByType`），但推荐传，免得拿到别的场景里的那个。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · `ChooseShowLimit` 退回手抄表（不读 `CardDef.ChoicesMaxTargets`）⇒ 今天**不会红**（两者逐 id 一致，
//     判据见 A2）；能红的是数据侧：`cards_engine.json` 少一列 / 出现 `-1` ⇒ **A1** 红；
//     出现「正文带 `Choose ` 但 `maxTargets ≤ 0`」的卡 ⇒ **A5** 红（那种卡会让面板摆 0 张，原版回空表）。
//   · `HeroLandScaleIn` 那一跳删掉（= 2026-10-26 那个「缩放不写」的旧口径）⇒ **C8** 红（实得 = 在位缩放）。
//   · 只做放大、不接 `HeroLandScaleOut` ⇒ **C11** 红（督军永久大 30%）。
//   · 把 `warlordStartSizeFactor` 写死成 1.3 而不同时改数据件 ⇒ **C2** 红（移动那份是 1.5）。
//   · `waitMult == 1` 时也去动 y（多做一次 ∓0.2）⇒ **C9** 红。
using System.Collections.Generic;
using CardPresentation;
using DG.Tweening;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_BD`：账 `战斗逻辑驱动:255` ①（选牌上限改读卡数据）与 `:292`（督军入场缩放 ×1.3）的读数口。
/// <para>⛔ 期望值全是**字面量 / 数据件真值 / 入场前现读的对照量**，不从被测实现里反算。</para></summary>
public static class B97_BD_Assert
{
    static readonly CheckSink _sink = new CheckSink("[BD] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static bool Near(float a, float b, float eps = 1e-3f) { return Mathf.Abs(a - b) <= eps; }

    // ============================================================ 入口
    /// <summary>本件唯一入口（宿主 `Run()` 里用一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，理由同 `B97_W1_Assert`）。</summary>
    public static void CheckB97BD(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "BD：账 255 ①（选牌上限 = 卡数据 `ChoicesMaxTargets`）· 账 292（督军入场 ×`warlordStartSizeFactor`）");
        Check255ChooseLimitFromData();
        Check292HeroLandScale(drv);
        Debug.Log(_sink.P + "BD 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  1 · 账 255 ①：`ChoicesMaxTargets` 这一列**全池可用**（⇒ 运行期只走数据那一支）
    //  判据 = `BattleManager__GetChoiceOptions.c:40-45`（`min(pool, choicesDefinition.maxTargets)`；
    //         `maxTargets == 0` ⇒ 空表）+ 数据 `cards_engine.json` 的 `choicesMaxTargets`（v10 起）
    // ==================================================================
    static void Check255ChooseLimitFromData()
    {
        var pool = CardDatabase.Load();
        Tr(pool != null && pool.Count > 1000, "A0（前提）卡池载得出来（`CardDatabase.Load()`，现读 " + (pool != null ? pool.Count : 0) + " 张）");
        if (pool == null) return;

        int nulls = 0, neg = 0, zero = 0, two = 0, three = 0, four = 0, other = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) { nulls++; continue; }
            int v = c.ChoicesMaxTargets;
            if (v < 0) neg++;
            else if (v == 0) zero++;
            else if (v == 2) two++;
            else if (v == 3) three++;
            else if (v == 4) four++;
            else other++;
        }
        C(nulls, 0, "A0a（前提）池里没有 null 卡（否则下面的分布没意义）");
        C(neg, 0, "A1 ★ 全池**没有一张** `ChoicesMaxTargets == -1` ⇒ `BattleDriver.ChooseShowLimit` 运行期**只走数据那一支**，"
                + "兜底快照表 `ChooseShowByCardId` 与众数 3 **恒不可达**｜🧨 数据管线漏一列（回 `-1`）⇒ 本条先红（那时才是兜底表接管）");
        C(two + four, 21, "A2 ★ 数据侧 `maxTargets ∈ {2,4}` 的正好 **21 张** —— 与手抄表那 21 个 id **同规模**"
                        + "（逐 id 一致这一半由 `BattleScene` §20-b 的行为断言兜住）");
        C(two, 12, "A2a …其中 `= 2` 的 **12** 张");
        C(four, 9, "A2b …其中 `= 4` 的 **9** 张");
        C(other, 0, "A2c 池内 `maxTargets` 只出现 {0,2,3,4} —— 多出别的值说明换了 build，先回源再谈口径");
        C(zero + two + three + four, pool.Count - nulls, "A3 四档之和 = 池内可判定的张数（无漏计 / 无重叠）");
        Tr(zero > 900, "A3a（前提）`= 0` 的是绝大多数（" + zero + " 张）：原版这一档**不开选牌这条路**（`GetChoiceOptions` 回空表）");

        // 「正文要求选一张」的那一族：原版 `maxTargets` 必须 > 0，否则面板摆 0 张（= 原版的空表）
        int chooseCards = 0, chooseZero = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Desc)) continue;
            if (c.Desc.IndexOf("Choose ", System.StringComparison.Ordinal) < 0) continue;
            chooseCards++;
            if (c.ChoicesMaxTargets <= 0) chooseZero++;
        }
        Tr(chooseCards > 40, "A4（前提）池内正文带 `Choose ` 子句的卡有 " + chooseCards + " 张（与 `ChooseShowDefault` 那段「50 张」同一族）");
        C(chooseZero, 0, "A5 ★ **没有一张**「正文有 `Choose ` 子句、`maxTargets` 却 ≤ 0」的卡 ⇒ 面板**不会**出现「摆 0 张」那种死局"
                       + "（原版这一步回空表、玩家无牌可选）｜🧨 数据哪天出现这种卡 ⇒ 本条先红，那时要在 `ShowAsk` 补一道出声");

        // 两张锚点卡：**同一句 desc 子句、候选域相同、张数不同** —— 「一处常数管全部」在这里必错
        var g65 = CardDatabase.FindById(pool, "GSC65");
        var g50 = CardDatabase.FindById(pool, "GSC50");
        Tr(g65 != null && g50 != null, "B0（前提）两张锚点卡都在池里（`GSC65 Pilfered Supplies` / `GSC50 Clandestine Operation`）");
        if (g65 != null) C(g65.ChoicesMaxTargets, 4, "B1 ★ `GSC65` 的 `ChoicesMaxTargets = 4`（原版 `maxTargets`，面板摆 4 张）");
        if (g50 != null) C(g50.ChoicesMaxTargets, 3, "B2 ★ `GSC50` = **3** —— 候选域与 `GSC65` 完全相同、desc 前半句相同，只有张数不同");
    }

    // ==================================================================
    //  2 · 账 292：督军入场 `state 0 ②`（× 1.3）与 `state 1`（补间回正在位缩放）
    //  判据 = `CardScript._HeroLandIntoField_d__318__MoveNext.c:42/52/59`（state 0）· `:342-400`（state 1）
    // ==================================================================
    static void Check292HeroLandScale(BattleDriver drv)
    {
        // 数据真值先断（两档各一份 ⇒ 断得出「这不是一条写死的常数」）
        Tr(Near(VarsDevice.Current.warlordStartSizeFactor, 1.3f),
           "C1 ★ `VarsDevice.Current.warlordStartSizeFactor = 1.3`（桌面 `VarsDesktop.json` 真值；原版读 `VarsDevice+0x30`）");
        Tr(Near(VarsDevice.Mobile.warlordStartSizeFactor, 1.5f),
           "C2 ★ …移动那份是 **1.5**（同一字段两档不同）｜🧨 把 1.3 写死在调用点而不同时改数据件 ⇒ 本条红");

        if (drv == null) drv = Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "C3（前提）拿到一个活着的 `BattleDriver`");
        if (drv == null) return;

        bool feelWas = drv.animateFeel;
        var modeWas = CardTween.Mode;
        try
        {
            // 教程开场：敌方督军第一拍藏着，第二拍 `SetActive(true)` + `HeroLandIntoField(…, 1.0)`
            //   （判据 = `BattleManager._TutorialStartSequence_d__592__MoveNext.c:80`；我们的等价入口 = `BeginTutorial`）
            drv.BeginTutorial(0);
            drv.animateFeel = true;                        // 原版这一串只在真机演出档跑（批处理默认关）
            CardTween.Mode = UpdateType.Manual;            // 批处理没有帧循环：补间要手动泵

            var fw = drv.BoardViewAt(BoardSpec.WarlordSlot, false);
            Tr(fw != null, "C4（前提）敌方督军的视图在场上（槽 " + BoardSpec.WarlordSlot + "）");
            if (fw == null) return;
            bool activeWas = fw.gameObject.activeSelf;
            float dflt = fw.BaseScale;                     // 原版 `DefaultScale`（`GetUnitSizeInPlay` 的等价物）
            float yWas = fw.transform.localPosition.y;
            Tr(dflt > 0.01f, "C5（前提）入场前它的在位缩放大得可判（" + dflt.ToString("F4") + "）");

            drv.TickTutorialViewForTest(0.1f);
            Tr(!fw.gameObject.activeSelf, "C6（前提）还没到第二拍 ⇒ 敌方督军仍藏着（`HeroLandInto` 因此还没跑）");
            Tr(Near(fw.BaseScale, dflt), "C7（判别式的一半）第一拍**没有**放大：缩放仍是入场前的值");

            drv.TickTutorialViewForTest(1.0f);
            Tr(fw.gameObject.activeSelf, "C8（前提）第二拍：敌方督军显出来");
            float want = dflt * VarsDevice.Current.warlordStartSizeFactor;
            Tr(Near(fw.BaseScale, want),
               "C9 ★ 段0 ②：入场那一刻整卡 = 在位缩放 × `warlordStartSizeFactor`（" + dflt.ToString("F4") + " × 1.3 = "
             + want.ToString("F4") + "，实得 " + fw.BaseScale.ToString("F4") + "）｜🧨 把 `HeroLandScaleIn` 那一跳删掉"
             + "（= 2026-10-26 那个「缩放不写」的旧口径）⇒ 本条红");
            Tr(Near(fw.transform.localPosition.y, yWas),
               "C10 ★ `waitMult == 1.0` ⇒ y **不动**（原版 `waitMult != 1` 才 ∓0.2；本 build 两个调用点都传 1.0）"
             + "｜🧨 无条件动 y ⇒ 本条红");

            drv.AdvanceTimeline(0.31f);                    // = 段0 的 `WaitForSeconds(waitMult × 0.3)`
            Tr(fw.BaseScale > want - 1e-2f, "C11 ★ 段1 的起点仍是放大态（`DOScale` 是补间，不是跳变）");
            CardTween.Advance(0.3f);                       // `AttackStepTime`(0.1) 之上，OutElastic 用平面推
            Tr(Near(fw.BaseScale, dflt),
               "C12 ★ 段1 收尾：补间把缩放**回正到在位缩放**（" + dflt.ToString("F4") + "，实得 "
             + fw.BaseScale.ToString("F4") + "）｜🧨 只做放大、不接 `HeroLandScaleOut` ⇒ 督军永久大 30% ⇒ 本条红");
            Tr(Near(fw.transform.localPosition.y, yWas), "C13 收尾 y 也没漂（同 C10 的另一半）");

            fw.gameObject.SetActive(activeWas || true);    // 留给宿主：它紧接着会自己再 `BeginTutorial(0)`
        }
        finally
        {
            if (drv != null) drv.animateFeel = feelWas;
            CardTween.Mode = modeWas;
        }
    }
}
