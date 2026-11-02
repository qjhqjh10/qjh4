// B117_G10_04_Assert.cs — 执行代理 `B117-G10-04`（战斗域第 4 部分：HUD 状态信息层）的断言宿主（2026-11-03）
//
// 覆盖两条**判据已定**的偏差（判据全文 → `资料/完全复刻/判据/B117-G10-04_HUD状态信息层.md`）：
//   ① 里程碑（`MatchSkulls Icon` + `MatchSkulls Score`）的**可见性按 playMode** ——
//      原版 `MatchData.GetMilestones()`（`MatchData__GetMilestones.c:20-33`，开关量是 `playMode`）
//      在 `{1 Duel, 2 PracticeLodge, 4 Tutorial, 5 CutScene, 8 Campaign, 9 TutorialReplay}` 这 6 档返回
//      `Array.Empty`，而 `BattleScoreManager.Initialize`（`BattleScoreManager.cs:21-25`）第一句就是
//      `milestoneCountIcon.enabled = milestoneCountText.enabled = targetThresholds.Length > 0`
//      ⇒ **这 6 档里两颗都不画**（节点还在、组件关着），另外 9 档才画。
//   ② 三枚「本回合已出牌数」灯（`CardsPlayedInTurn1..3`）**先过 holder 那道闸** ——
//      它们 `CardsPlayedInTurnHolder` 的子件（序列化实证：`BattleHud.offensiveCardTurnCardsHolder`
//      → `Battle Arena 1.unity` fileID 177，objs → 1000/609/727），holder 只在
//      `SetActive(offensiveCard != null && offensiveCard.HasPlayedThirdCardInTurn())` 时才亮
//      （`BattleManager._ApplyOffensiveAndDefensiveEffects_d__337_MoveNext` state 1），场景默认 `m_IsActive = 0`
//      ⇒ **进攻卡不带 `trigger == 15` 的对局，这三枚一枚都不出现**；亮几枚 = `min(3, 本回合全局已出张数)`。
//
// ⚠️ **本文件与 `Battle/BattleDriver.cs` 的两条补丁成对**（补丁 A/B，见上面那份判据 §二）。
//    **补丁没落地时：B 组与 C 组会红 —— 那正是它们的意义（判别性）**，不是本文件写错。
// ⚠️ 也正因如此，补丁落地后 `Editor/BattleScene.cs` 的 **:4683 / :6914 两条旧断言**要同步改
//    （它们钉的是「没有闸」的旧口径）；那两处在别人的白名单里，本件没动。
//
// 用法（⚠️ **主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_04_Assert.Run -logFile -
//
// 判绿红：`grep "^B117G1004"` 的**合计行**；逐条按**行首** `B117G1004 ✓` / `B117G1004 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   · `Run()`：**零前提** —— 不读 `BattleDriver`、不读场景、不读存档。
//   · `RunWithDriver(drv)`：要一台**已开局**的 `BattleDriver`（`Begin(...)` 已跑完、`Ctx != null`）。
//     它会**临时改写** `Ctx.PlayMode / CardsPlayedInTurn / OffensiveSlotIdx / OffensiveEnvSO`，
//     并在 `finally` 里**原样还原 + 再刷一次 HUD** ⇒ 但请仍排在
//     **所有依赖棋盘/HUD 现状的断言之后**、卸 `driver` 之前（它会改 HUD 的激活态）。
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117_G10_04_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1004 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1004 ✗ " + msg); }
    }

    // =====================================================================================
    //  Run —— 零前提（纯函数级）
    // =====================================================================================
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("B117G1004 ── Run()（零前提）──────────────");
        try { A_Rules(); }
        catch (System.Exception e)
        {
            Check(false, "A 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        Debug.Log("B117G1004 合计 " + _pass + " / " + (_pass + _fail));
    }

    /// <summary>A 组：两条**纯函数级**的不变式 —— 它们错了，下面 B/C 组就算全绿也说明不了什么。</summary>
    static void A_Rules()
    {
        // A1/A2/A3：阈值表必须是 **{20,10,0} 且严格降序**。
        //   为什么「降序」是硬判据：原版 `UpdateMilestonesCount(index)` 印的是 `"x" + (index+1)`
        //   （`BattleScoreUiManager__UpdateMilestonesCount.c`），而 `CheckThresholds` 是**按下标升序**扫、
        //   每新达成一条就覆盖一次文案 ⇒ 只有**下标越大阈值越小**时，最终那个 `index+1` 才等于
        //   `GetSkullCount()`（= 已达成档数，结算面板用的也是它）。改成升序 ⇒ 名牌与结算面板会**对不上**。
        var t = DeckRules.SkullThresholds;
        Check(t != null && t.Length == 3, "A1 里程碑阈值恒 3 档（实得 " + (t == null ? "null" : t.Length.ToString()) + "）");
        if (t == null || t.Length != 3) return;
        Check(t[0] == 20 && t[1] == 10 && t[2] == 0,
              "A2 阈值逐值 = {20,10,0}（实得 {" + t[0] + "," + t[1] + "," + t[2] + "}）"
            + " —— 判据 `MatchData__GetMilestones.c` 的 0x14/10/0 三条");
        Check(t[0] > t[1] && t[1] > t[2],
              "A3 阈值**严格降序**（实得 " + t[0] + " > " + t[1] + " > " + t[2] + "）"
            + " —— 只有降序时 `\"x\"+(index+1)` 才 == 已达成档数");

        // A4：牌库张数的**文案形状** = `Loc.T("Battle/HUD/CardsLeft") + ": " + n`。
        //   判据逐字：`DeckManager__DisplayDeckSize.c:44-49` 拼的是
        //   `GetTranslation(词条)` + 字面量 `": "`（`stringliteral.json` 0x42c7df8） + `Int32.ToString(count)`。
        //   ⛔ 不是 `n/max`（那是**手牌**那一颗的写法，见 `PlayerHand.ShowHandSize` 的兄弟实现）；也不是纯数字。
        string want = Loc.T(BattleDriver.DeckCountTerm) + ": 7";
        Check(BattleDriver.DeckCountTerm == "Battle/HUD/CardsLeft",
              "A4a 牌库张数的本地化键 = `Battle/HUD/CardsLeft`（实得 `" + BattleDriver.DeckCountTerm + "`）");
        Check(BattleDriver.CountText(BattleDriver.DeckCountTerm, 7) == want,
              "A4b 牌库张数文案 = `词条 + \": \" + 张数`（实得「"
            + BattleDriver.CountText(BattleDriver.DeckCountTerm, 7) + "」，期望「" + want + "」）");
    }

    // =====================================================================================
    //  RunWithDriver —— 要一台已开局的 BattleDriver
    // =====================================================================================
    public static void RunWithDriver(BattleDriver drv)
    {
        _pass = 0; _fail = 0;
        Debug.Log("B117G1004 ── RunWithDriver()（要一台已开局的 BattleDriver）──────────────");
        if (drv == null || drv.Ctx == null || drv.Ctx.Players == null)
        {
            Check(false, "前提：`drv != null && drv.Ctx != null`（实得 "
                       + (drv == null ? "drv = null" : "Ctx = null") + "）—— 本组**一条都没验**，⛔ 别读成通过");
            Debug.Log("B117G1004 合计 " + _pass + " / " + (_pass + _fail));
            return;
        }
        try { B_Milestones(drv); }
        catch (System.Exception e)
        {
            Check(false, "B 组抛异常（后面的断言**没跑到**）：" + e.GetType().Name + ": " + e.Message);
        }
        try { C_Pips(drv); }
        catch (System.Exception e)
        {
            Check(false, "C 组抛异常（后面的断言**没跑到**）：" + e.GetType().Name + ": " + e.Message);
        }
        try { D_Counters(drv); }
        catch (System.Exception e)
        {
            Check(false, "D 组抛异常（后面的断言**没跑到**）：" + e.GetType().Name + ": " + e.Message);
        }
        Debug.Log("B117G1004 合计 " + _pass + " / " + (_pass + _fail));
    }

    // ---- B：里程碑可见性（灭自证：同一台 driver 上「该藏」与「该画」各验一次）-------------
    static void B_Milestones(BattleDriver drv)
    {
        var icon = FindOne("MatchSkullsIcon");
        var score = FindOne("MatchSkullsScore");
        Check(icon != null && score != null, "B0 前提：HUD 里 `MatchSkullsIcon` / `MatchSkullsScore` 各恰好一个");
        if (icon == null || score == null) return;

        var keep = drv.Ctx.PlayMode;
        try
        {
            // B1/B2：练习档（playMode 2）⇒ 原版 `GetMilestones()` 返回 `Array.Empty` ⇒ 两颗都**不画**。
            //   改坏法①「无条件常显」在这一步必红。
            drv.Ctx.PlayMode = GameMode.PracticeLodge;
            drv.RefreshAll();
            Check(!icon.gameObject.activeSelf,
                  "B1 练习档（playMode 2）骷髅**不画**（原版 `milestones.Length == 0 ⇒ milestoneCountIcon.enabled = false`；"
                + "现在 activeSelf = " + icon.gameObject.activeSelf + "）");
            Check(!score.gameObject.activeSelf,
                  "B2 练习档（playMode 2）`x N` **不画**（同上，`milestoneCountText.enabled = false`；"
                + "现在 activeSelf = " + score.gameObject.activeSelf + "）");

            // B3/B4：经典档（playMode 0）⇒ 3 条阈值 ⇒ 两颗都**画**。
            //   改坏法②「一律藏起来」在这一步必红 ⇒ 与 B1/B2 构成**灭自证对**。
            drv.Ctx.PlayMode = GameMode.Classic;
            drv.RefreshAll();
            Check(icon.gameObject.activeSelf,
                  "B3 经典档（playMode 0）骷髅**要画**（原版 3 条阈值 ⇒ enabled = true；"
                + "现在 activeSelf = " + icon.gameObject.activeSelf + "）");
            Check(score.gameObject.activeSelf,
                  "B4 经典档（playMode 0）`x N` **要画**（同上；现在 activeSelf = " + score.gameObject.activeSelf + "）");
            Check(score != null && !string.IsNullOrEmpty(drv.SkullScoreText) && drv.SkullScoreText[0] == 'x',
                  "B5 还画着的时候文案仍是 `x N`（原版 `UpdateMilestonesCount` 恒 `\"x\"+(index+1)`；"
                + "实得「" + drv.SkullScoreText + "」）");
        }
        finally
        {
            drv.Ctx.PlayMode = keep;
            drv.RefreshAll();
        }
    }

    // ---- C：三枚灯的 holder 闸 + 张数（灭自证：无卡 0 枚 · 有卡 2 枚 · 超量 3 枚）-----------
    static void C_Pips(BattleDriver drv)
    {
        // C0 前提：进攻卡能力表**载到了**。载不到 ⇒ 闸永远打不开（游戏里三枚永远不出现）= 真缺陷，
        //    必须先红在这里，否则下面的 C2/C3 会以一个「假绿的空表」为基准。
        Check(OffensiveCardData.TableCards > 0,
              "C0 前提：`offensive_card_abilities` 表载到了（TableCards = " + OffensiveCardData.TableCards + "；"
            + "0 = 没载到 ⇒ 补丁 B 的闸在真机上也永远打不开）");
        if (OffensiveCardData.TableCards <= 0) return;

        var keepPlayed = drv.Ctx.CardsPlayedInTurn;
        var keepSlot = drv.Ctx.OffensiveSlotIdx;
        var keepSo = drv.Ctx.OffensiveEnvSO;
        try
        {
            // C1：没有进攻卡（`OffensiveSlotIdx = -1` = 选了「不使用进攻卡」）⇒ **一枚都不亮**，
            //     哪怕本回合已经出了 2 张。改坏法「只要出了牌就亮」在这一步必红。
            drv.Ctx.CardsPlayedInTurn = 2;
            drv.Ctx.OffensiveSlotIdx = -1;
            drv.Ctx.OffensiveEnvSO = "";
            drv.RefreshAll();
            Check(drv.PlayedPipsOn == 0,
                  "C1 没有进攻卡时，本回合出了 2 张也**一枚都不亮**（原版 holder 整块关着；实得亮 "
                + drv.PlayedPipsOn + " 枚）");

            // C2：换成**带 `trigger 15`** 的进攻卡（DA `Asteroid Zone` —— 全池只有 2 张这样的卡）⇒ 亮 2 枚。
            //     改坏法「把闸焊死成一盏都不亮」在这一步必红 ⇒ 与 C1 构成灭自证对。
            drv.Ctx.OffensiveSlotIdx = 0;
            drv.Ctx.OffensiveEnvSO = "Environmental Condition Dark Angels Asteroid Zone";
            drv.RefreshAll();
            Check(drv.PlayedPipsOn == 2,
                  "C2 进攻卡带 `trigger 15` + 本回合 2 张 ⇒ 亮 **2** 枚（原版 `SetTurnPlayedCards(2)`；实得亮 "
                + drv.PlayedPipsOn + " 枚）");

            // C3：第 3 张以后恒 3 枚（原版那个 `GameObject[]` 只有 3 格，第 4 张不显示）。
            drv.Ctx.CardsPlayedInTurn = 5;
            drv.RefreshAll();
            Check(drv.PlayedPipsOn == 3,
                  "C3 本回合 5 张 ⇒ 只亮 **3** 枚（原版 `offensiveCardTurnCardsObjs.Length == 3`；实得亮 "
                + drv.PlayedPipsOn + " 枚）");

            // C4：回合清 0 ⇒ 全灭（原版 `_NextTurn` 写 0 后 `SetTurnPlayedCards(0)`）。
            drv.Ctx.CardsPlayedInTurn = 0;
            drv.RefreshAll();
            Check(drv.PlayedPipsOn == 0,
                  "C4 本回合张数清 0 ⇒ 三枚全灭（实得亮 " + drv.PlayedPipsOn + " 枚）");
        }
        finally
        {
            drv.Ctx.CardsPlayedInTurn = keepPlayed;
            drv.Ctx.OffensiveSlotIdx = keepSlot;
            drv.Ctx.OffensiveEnvSO = keepSo;
            drv.RefreshAll();
        }
    }

    // ---- D：牌库张数 / 能量数字两条读数（都是原版拼法的**形状**判据）----------------------
    static void D_Counters(BattleDriver drv)
    {
        int me = drv.MyIndex;
        if (me < 0 || me > 1 || drv.Ctx.Players.Length < 2) { Check(false, "D0 前提：座位号合法"); return; }
        int foe = 1 - me;

        string wantMe = BattleDriver.CountText(BattleDriver.DeckCountTerm, drv.Ctx.Players[me].Deck.Count);
        string wantFoe = BattleDriver.CountText(BattleDriver.DeckCountTerm, drv.Ctx.Players[foe].Deck.Count);
        Check(drv.MyPileText == wantMe,
              "D1 我方牌库张数 = `Cards left: <我方 Deck.Count>`（实得「" + drv.MyPileText + "」，期望「" + wantMe + "」）");
        Check(drv.FoePileText == wantFoe,
              "D2 敌方牌库张数 = `Cards left: <敌方 Deck.Count>`（实得「" + drv.FoePileText + "」，期望「" + wantFoe + "」）"
            + " —— 原版 `ShowDeckSize` **两侧一起亮**，别只刷我方");

        // D3/D4：能量数字 = `未用/总`（原版 `ManaManager.SetMana(total,available)` → `SetMana(available,total)`
        //    → `string.Format("{0}/{1}")`）。分母**必须**是本侧 `MaxEnergy`。
        CheckEnergy(drv.MyEnergyText, drv.Ctx.Players[me].MaxEnergy, "D3 我方");
        CheckEnergy(drv.FoeEnergyText, drv.Ctx.Players[foe].MaxEnergy, "D4 敌方");
    }

    static void CheckEnergy(string txt, int max, string who)
    {
        if (string.IsNullOrEmpty(txt) || txt.IndexOf('/') < 0)
        {
            Check(false, who + "能量数字必须是 `未用/总` 那种写法（实得「" + txt + "」）");
            return;
        }
        int slash = txt.IndexOf('/');
        string left = txt.Substring(0, slash), right = txt.Substring(slash + 1);
        int parsedMax, parsedCur;
        bool okMax = int.TryParse(right, out parsedMax);
        bool okCur = left == "?" || int.TryParse(left, out parsedCur);
        Check(okMax && parsedMax == max,
              who + "能量数字的分母 = 本侧 MaxEnergy " + max + "（实得「" + right + "」）");
        Check(okCur,
              who + "能量数字的分子 = 整数 或 `?`（伏击那一档 `ToggleCurrentMana(false)` 印 `?`；实得「" + left + "」）");
    }

    // ---- 工具：按名字找唯一节点（HUD 图/字挂在子画布上，不一定是 `hudRoot` 的子树 ⇒ 全景搜）----
    static Transform FindOne(string name)
    {
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform hit = null;
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name) { hit = all[i]; n++; }
        }
        if (n == 1) return hit;
        Check(false, "前提：场景里叫 `" + name + "` 的节点应当**恰好一个**（实得 " + n + " 个）");
        return null;
    }
}
