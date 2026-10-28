// GameStaticDataCheck.cs — 本批（B89 第 1 波 · `E12`）那 11 条基础设施的**批处理自检**
//
// ============================ 覆盖哪几条账 ============================
//   `其他/基础设施` 234 `CheckIfNewCards` · 237 `FoundNewCard` · 250 `GetTraitDesc` ·
//   400 `GetLegionRankedMatchesWon` · 409 `GetRandomTip` · 421 `IsBattleLogCreated` ·
//   460 `LoadSharedDefaultValues`（+ 250 的 `Alt` 数据那一支）。
//
// ============================ 为什么单开一份 ============================
//  · 这些落点分散在 `Shell/GameStaticData.cs`（本批新建）· `Core/Tooltip.cs` · `Shell/RankedData.cs` ·
//    `Battle/TipPrebattle.cs` · `Core/PlayerDataMath.cs`，而**现有宿主没有一个覆盖它们**
//    ⇒ 此前一条断言都没有。本文件**只放断言**，不改实现。
//  · ⚠️ 本批红线**禁止跑 Unity** ⇒ 本文件写好后**没人跑过**（同 `Editor/LoadableRefCheck.cs`），
//    「断言跑不跑得绿」要等调度台在同步点把它接进 `_run_8_checks.sh`（见报告 §5 交接）。
//
// ============================ 判据 ============================
//  · 逐条在对应实现文件的 doc 里（`Shell/GameStaticData.cs` 文件头有完整出处表）。
//  · 🔴 唯一一条**与施工单相反**的：`IsBattleLogCreated` 的白名单里 **110 `ClosedDeck` 在**、
//    `100 Tutorial` 不在 —— 判据 = 机器码 `mov ecx, 0x40100001` + `bt ecx, eax`（bit 20），
//    反证见报告 §1 / §3。本文件的断言就钉这一条。
//
// ============================ 用法 ============================
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod GameStaticDataCheck.Run -logFile -
//   ⚠️ 判绿红看「GSD 合计」，不看退出码（同仓其它宿主的约定）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class GameStaticDataCheck
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("GSD ✓ " + what); }
        else { _fail++; Debug.LogError("GSD ✗ " + what); }
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // ── 460 LoadSharedDefaultValues：13 格的默认值（判据 = `PlayerDataManager__LoadSharedDefaultValues.c`）──
        GameStaticData.LoadSharedDefaultValues();
        Check(Mathf.Approximately(GameStaticData.MusicVolume, 1f), "460 MusicVolume 默认 1f");
        Check(Mathf.Approximately(GameStaticData.SoundFxVolume, 1f), "460 SoundFxVolume 默认 1f");
        Check(Mathf.Approximately(GameStaticData.VoiceOversVolume, 1f), "460 VoiceOversVolume 默认 1f");
        Check(!GameStaticData._disableParticles, "460 _disableParticles 默认 false");
        Check(GameStaticData.lastRegistrationShowTime == 0L, "460 lastRegistrationShowTime 默认 0");
        Check(!GameStaticData.notificationsOn, "460 notificationsOn 默认 false");
        Check(GameStaticData.hiFPS, "460 hiFPS 默认 **true**（不是 false —— 判据 .c 写 1）");
        Check(GameStaticData.useCombatAutoZoom == GameStaticData.DefaultTouchInput,
              "460 useCombatAutoZoom 默认 = DefaultTouchInput");
        Check(GameStaticData.touchInput == Application.isMobilePlatform,
              "460 touchInput 默认 = Application.isMobilePlatform");
        Check(GameStaticData.graphicsQuality == GameStaticData.EverguildQualitySetting.Ultra,
              "460 graphicsQuality 默认 = Ultra(4)（判据 get_DefaultQualitySetting.c = return 4）");
        Check(GameStaticData.cardsTextInHandFormat == GameStaticData.DefaultCardTextsInHandOptions,
              "460 cardsTextInHandFormat 默认 = DefaultCardTextsInHandOptions");
        Check(GameStaticData.lastTimeAskedForNotifications == 0L, "460 lastTimeAskedForNotifications 默认 0");
        Check(GameStaticData.DefaultCardTextsInHandOptions == GameStaticData.CardTextsInHandOptions.Default,
              "460 DefaultCardTextsInHandOptions：smallScreenUI=false ⇒ Default(0)");

        // `smallScreenUI` 是**读穿** `CardPresentation.SmallScreenUI.Enabled`（唯一来源，别另存一份）。
        // ⚠️ `SmallScreenUI` **没有** `RestoreForTest(bool,bool)`（只有 `ResetForTest()`）⇒ 收尾走
        //    「Reset → 有必要再 Set 回原值」（⛔ 全程 `PersistOverride = true`，不碰玩家的 `PlayerPrefs`）。
        bool s0 = CardPresentation.SmallScreenUI.Enabled;
        bool c0 = CardPresentation.SmallScreenUI.ChosenManually;
        bool p0 = CardPresentation.SmallScreenUI.PersistOverride;
        CardPresentation.SmallScreenUI.PersistOverride = true;    // ⛔ 不动玩家 PlayerPrefs
        try
        {
            CardPresentation.SmallScreenUI.Set(true);
            Check(GameStaticData.SmallScreenUI, "460 smallScreenUI 读穿 SmallScreenUI.Enabled（置 true 后 = true）");
            Check(GameStaticData.DefaultCardTextsInHandOptions == GameStaticData.CardTextsInHandOptions.OnlyDescription,
                  "460 DefaultCardTextsInHandOptions：smallScreenUI=true ⇒ OnlyDescription(1)");
            CardPresentation.SmallScreenUI.Set(false);
            Check(!GameStaticData.SmallScreenUI, "460 smallScreenUI 读穿 SmallScreenUI.Enabled（置 false 后 = false）");
        }
        finally
        {
            CardPresentation.SmallScreenUI.ResetForTest();                 // 出厂：false / 未手动
            if (s0) CardPresentation.SmallScreenUI.Set(true);              // 原本开着 ⇒ 放回
            if (c0) CardPresentation.SmallScreenUI.Set(s0);                // 原本手动选过 ⇒ 把「已手动」这一位放回
            CardPresentation.SmallScreenUI.PersistOverride = p0;
        }

        // ── 234 CheckIfNewCards ──
        var newCards = new List<string> { "UM47", "DA1" };
        Check(GameStaticData.CheckIfNewCards("UM47", newCards), "234 列表里有 ⇒ true");
        Check(!GameStaticData.CheckIfNewCards("SW18", newCards), "234 列表里没有 ⇒ false");
        Check(!GameStaticData.CheckIfNewCards("UM47", null), "234 列表为 null ⇒ false（原版 NRE，本工程不抛）");

        // ── 237 FoundNewCard ──
        // 原版只 lower **头部**（`MenuDeck/HUD/NewCard` 的译文），`text` 必须是已 lower 的那一档。
        string hLower = Loc.T("MenuDeck/HUD/NewCard").ToLower();
        Check(!string.IsNullOrEmpty(hLower), "237 `MenuDeck/HUD/NewCard` 取到了译文（现读 en=`New!` / zh=`新的！`）");
        Check(GameStaticData.FoundNewCard("UM47", hLower, newCards), "237 头部含 text ∧ 列表含 id ⇒ true");
        Check(!GameStaticData.FoundNewCard("UM47", "__nope__", newCards), "237 头部不含 text ⇒ false（短路在 Contains）");
        Check(!GameStaticData.FoundNewCard("SW18", hLower, newCards), "237 头部含 text 但列表不含 id ⇒ false");
        Check(!GameStaticData.FoundNewCard("UM47", hLower, null), "237 列表为 null ⇒ false（原版 NRE，本工程不抛）");
        Check(!GameStaticData.FoundNewCard("UM47", null, newCards), "237 text 为 null ⇒ false（原版抛 ArgumentNull）");

        // ── 250 GetTraitDesc（Alt → 非 Alt → 枚举名 三跳）──
        string prayAlt = TipText.GetTraitDesc("pray", true);
        string prayBase = TipText.GetTraitDesc("pray", false);
        Check(!string.IsNullOrEmpty(prayAlt) && prayAlt != "pray", "250 `pray` + isAlt ⇒ 取到 `CardTraitDescription/prayAlt` 的译文");
        Check(!string.IsNullOrEmpty(prayBase) && prayBase != "pray", "250 `pray` 非 Alt ⇒ 取到 `CardTraitDescription/pray` 的译文");
        Check(prayAlt != prayBase, "250 Alt 与 非Alt 是**两条不同**的文案（Alt 键确实生效）");
        // `faiths` 本体**缺**、`faithsAlt` 在 ⇒ 正好走「Alt 有、base 空」那条支
        string faithsAlt = TipText.GetTraitDesc("faiths", true);
        Check(!string.IsNullOrEmpty(faithsAlt) && faithsAlt != "faiths",
              "250 `faiths` + isAlt ⇒ Alt 键有译文（base 缺也照拿 Alt）");
        Check(TipText.GetTraitDesc("faiths", false) == "faiths",
              "250 `faiths` 非 Alt ⇒ 两跳都空 ⇒ 回**枚举名字符串**（原版 trait.ToString()）");
        Check(TipText.GetTraitDesc("__noSuchTrait__", true) == "__noSuchTrait__",
              "250 两跳都空 ⇒ 回枚举名字符串");
        Check(TipText.GetTraitDescForCanon("pray", true) == prayAlt,
              "250 规范键入口：`pray` 直取（同名）⇒ 与枚举名入口同一条 Alt 文案");

        // ── 400 GetLegionRankedMatchesWon ──
        RankedData.SetArmyRecordForTest(3, 7, 2);
        Check(RankedData.GetLegionRankedMatchesWon(3) == 7, "400 该阵营的 wins 求和（注入 7 ⇒ 7）");
        Check(RankedData.GetLegionRankedMatchesWon(9) == 0, "400 没有该阵营的条目 ⇒ 0");
        RankedData.SetArmyRecordForTest(3, 0, 0);          // 清理（`ResetForTest` 不清 `_wins`，见报告 §3）

        // ── 409 GetRandomTip ──
        var g0 = new TipPrebattle.Tips { maxHeroLevel = 0, tips = new List<string> { "zero" } };
        var g5 = new TipPrebattle.Tips { maxHeroLevel = 5, tips = new List<string> { "five" } };
        var g3 = new TipPrebattle.Tips { maxHeroLevel = 3, tips = new List<string> { "three" } };
        Check(TipPrebattle.GetRandomTip(new List<TipPrebattle.Tips> { g0, g5, g3 }) == "five",
              "409 取 `maxHeroLevel` **最大**的那一组（0/5/3 ⇒ 5）");
        Check(TipPrebattle.GetRandomTip(new List<TipPrebattle.Tips> { g0 }) == "",
              "409 只有 `maxHeroLevel = 0` 的组 ⇒ 一组都没有 ⇒ **空串**（原版 return DAT_1842b80e0）");
        Check(TipPrebattle.GetRandomTip(new List<TipPrebattle.Tips>()) == "", "409 空表 ⇒ 空串");
        Check(TipPrebattle.GetRandomTip(null) == "", "409 null ⇒ 空串（原版 NRE，本工程不抛）");
        Check(TipPrebattle.GetRandomTip(new List<TipPrebattle.Tips> { g0, g3 }) == "three",
              "409 平手之外：只有 3 与 0 ⇒ 取 3（>0 才算候选）");

        // ── 421 IsBattleLogCreated（🔴 与施工单相反：110 在、100 不在）──
        var one = new List<string> { "x" };
        Check(CardPresentation.PlayerDataMath.IsBattleLogCreated(one, RuleEngine.MatchType.Ranked),
              "421 Ranked(10) 在白名单 ∧ 非空 ⇒ true");
        Check(CardPresentation.PlayerDataMath.IsBattleLogCreated(one, RuleEngine.MatchType.ClosedDeck),
              "421 **ClosedDeck(110) 在白名单**（判据 0x40100001 的 bit20；施工单说不在 —— 那是错的）");
        Check(!CardPresentation.PlayerDataMath.IsBattleLogCreated(one, RuleEngine.MatchType.Tutorial),
              "421 Tutorial(100) **不在**白名单 ⇒ false");
        Check(!CardPresentation.PlayerDataMath.IsBattleLogCreated(one, RuleEngine.MatchType.EventAI),
              "421 EventAI(80) 不在白名单 ⇒ false");
        Check(CardPresentation.PlayerDataMath.IsBattleLogCreated(one, RuleEngine.MatchType.FastModeRanked),
              "421 FastModeRanked(211) = `.c` 的特例 `p == 0xd3` ⇒ true");
        Check(!CardPresentation.PlayerDataMath.IsBattleLogCreated(new List<string>(), RuleEngine.MatchType.Ranked),
              "421 在白名单但列表空 ⇒ false");
        Check(!CardPresentation.PlayerDataMath.IsBattleLogCreated((List<string>)null, RuleEngine.MatchType.Ranked),
              "421 在白名单但列表 null ⇒ false");

        // ── 接线（B89-E12b）：设置页「手牌文本」⇒ 手牌卡面**渲出来的文本**真的变 ──
        CheckHandTextWiring();

        Debug.Log($"GSD === 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) Application.Quit(_fail == 0 ? 0 : 1);
    }

    /// <summary>接线断言（B89-E12b）：`SettingsWindow.TextInHand.Value` ⇒ **手牌卡面渲出来的文本**真的跟着变。
    /// <para>判据 = 原版 `GraphicsTab__CardsTextInHandDropdownChange.c`（只写 `cardsTextInHandFormat`(+0x140) +
    /// `…ChosenManually`(+0x144) 并 LogWarning）+ `BasicCardUI__SetCardTextInHand.c`（**消费者**每次进手牌时读那一格）；</para>
    /// <para>我们的等价物 = `CardView.HandTextFormat`（`Core/CardView.cs:5096` 起就是消费者，值域同为 0/1/2）。</para>
    /// <para>🔴 断言**盯渲染态**（TMP 的 `gameObject.activeSelf` + `.text`），⛔ 不盯我们自己的常量 ——
    /// 否则「两边都没接线」也能绿。</para></summary>
    static void CheckHandTextWiring()
    {
        if (!TmpFont.Available)
        {
            Check(false, "接线前提：TMP 字体可用（不在 ⇒ 本节**什么都没验到**，别读成通过）");
            return;
        }
        var pool = RuleEngine.CardDatabase.Load();
        if (pool == null || pool.Count == 0)
        {
            Check(false, "接线前提：卡池载得到（`CardDatabase.Load()` 空 ⇒ 本节没验到）");
            return;
        }

        CardData data = default(CardData);
        bool have = false;
        for (int i = 0; i < pool.Count && !have; i++)
        {
            var c = pool[i];
            if (c == null || !c.IsUnit) continue;
            var cd = BattleDriver.ToCardData(c, c.Faction);
            if (!cd.isUnit || string.IsNullOrEmpty(cd.keywords)) continue;   // 要一张**带 desc** 的单位卡（态 5 那一支才验得到）
            data = cd; have = true;
        }
        Check(have, "接线前提：卡池里找到一张**带 desc 的单位卡**（找不到 ⇒ 本节没验到）");
        if (!have) return;

        int v0 = SettingsWindow.TextInHand.Value;
        int f0 = CardView.HandTextFormat;
        bool p0 = SettingsWindow.TextInHand.PersistOverride;
        var root = new GameObject("chk_e12b_hand");
        try
        {
            var v = CardView.Create(root.transform, data, "chk_e12b_unit", CardFace.Hand);
            bool built = v != null && v.TitleText != null && v.KeywordsText != null;
            Check(built, "接线前提：手牌卡的**卡名 / 效果文字**两层都建出来了（不在 ⇒ 下面三条没验到）");
            if (!built) return;

            SettingsWindow.TextInHand.PersistOverride = true;      // ⛔ 不写玩家 PlayerPrefs

            SettingsWindow.TextInHand.Value = 0;                   // 出厂档（Default）
            Check(v.TitleText.gameObject.activeSelf && v.KeywordsText.gameObject.activeSelf
                  && !string.IsNullOrEmpty(v.KeywordsText.text),
                  "接线 ①：设置 = Default(0) ⇒ 卡名 + 效果文字**都在**（量的是渲染态）");
            string titleAt0 = v.TitleText.text;

            SettingsWindow.TextInHand.Value = 1;                   // OnlyDescription
            Check(!v.TitleText.gameObject.activeSelf && v.KeywordsText.gameObject.activeSelf
                  && !string.IsNullOrEmpty(v.KeywordsText.text),
                  "接线 ②：设置 = OnlyDescription(1) ⇒ **只剩效果文字**"
                + "｜🧨 卡名那层还亮 = 设置没接到卡面（本批修的就是这条）");

            SettingsWindow.TextInHand.Value = 2;                   // OnlyName
            Check(v.TitleText.gameObject.activeSelf && !v.KeywordsText.gameObject.activeSelf
                  && v.TitleText.text == titleAt0,
                  "接线 ③：设置 = OnlyName(2) ⇒ **只剩卡名**，且渲的就是这张卡的卡名（⛔ 不是占位字）");
        }
        finally
        {
            SettingsWindow.TextInHand.PersistOverride = p0;
            SettingsWindow.TextInHand.RestoreForTest(v0);          // 内存值放回（⛔ 不动 PlayerPrefs）
            CardView.SetHandTextFormat(f0);                        // 卡面档放回（别的宿主在同一个进程里也可能用）
            Object.DestroyImmediate(root);
        }
    }
}
