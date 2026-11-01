// RuleEngineTest_B115W13.cs — 执行写手 `B115_W13`（2026-11-01 · 第一百一十五会话）
//
// ============================ 这件活是什么 ============================
// 旧任务 `B-308`：`工具/gen_prebuilt_decks.py --check`（**只比不写**、一致 rc=0 / 不一致 rc=1）已经建好了闸，
// 但**没有任何宿主调用它** ⇒ 只能人手跑 ⇒ 「下次又静默回旧」没被兜住。
// 本文件补的是**宿主侧的哨兵**：读产物 `Assets/RuleEngine/Resources/tutorial_decks.json`，
// 断「教程 12 副全部 `complete == true`」，红时**点名是哪一副、缺几张**。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · 产物 = `Assets/RuleEngine/Resources/tutorial_decks.json`，由 `工具/gen_prebuilt_decks.py` 生成（**别手改**）。
//   · `complete` 的定义 = `gen_prebuilt_decks.py` 的 `make_entry()`（`kind == "tutorial"` 那一支）：
//       `complete = not missing and hero_id is not None`  —— 即「卡全解析出 + 督军解析出」。
//     教程那 12 副**不卡张数、也不卡同名上限**（关卡牌是手工编的，原版自己就 20 / 25 / 30 张不等，
//     见 `资料/预组卡组_原版规格.md:286`）；`problems` 只当情报印出来、**不进** `complete`。
//   · 现读（2026-11-01 本代理实跑 `python -I -X utf8 工具/gen_prebuilt_decks.py --check`，rc=0）：
//       `tutorial stages=6 decks=12 complete=12` · `not complete = 0` ·
//       `CHECK 结果：两份产物与盘上逐字一致。`
//   · `missing` 条目的形状 = `"%s x%d (%s)"`（原卡号 / 张数 / 档位），见同一处的 `make_entry()` 返回值。
//
// ============================ 宿主接线 ============================
//   在 `RuleEngineTest.cs` 的 `Run()` 里（**同一物理行追加 ⇒ 行数零变化**，见 `CLAUDE.md` 那条
//   「聚合行行尾 `//` 会吞掉代码」）：
//       Guard("CheckB115W13TutorialDecksComplete", CheckB115W13TutorialDecksComplete);
//   ⚠️ 方法名**不以 `Test` / `Report` 打头** —— `VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例。
//   ⚠️ 本断言**不建棋局、不打牌、不写盘**，只读一个 `TextAsset` ⇒ 放在 `Run()` 里任何位置都无副作用。
//
// ============================ 🔴 灭自证 ============================
//   判据函数 `JudgeTutorialDecksB115` 被**两份数据**各判一次：
//     ① 真产物 —— 必须**过**（A5）；② 手工把第 3 关玩家那副的 `missing` 塞回一条 + `complete=false` 的**最小反例**
//     —— 必须**不过**、且点名并把那条 `missing` 原样印出来（B1 / B2 / B2b）。
//   ⇒ 一个「恒 true」的判据过不了 ②；一个「恒 false」的过不了 ① —— **两条结构上不可能同时绿**。
//   另有 A6 / A6b / A7 三条**绕开 `complete` 这一列**的独立口径（`missing` 非空数、`missing` 条目合计、`heroId` 空数）
//   —— 防「生成器把 `complete` 算成恒 true」这种一侧错、另一侧跟着错的自证。
//
//   🧨 反例怎么造（给人复核用）：把 `tutorial_decks.json` 任一 `stages[i].player.missing` 改回 `["X x2 (unknown)"]`
//      并把同侧 `complete` 改 `false` ⇒ A5 红（点名该关该侧），而 A2/A3/A4 仍绿（结构与张数没变）。
using System.Collections.Generic;
using UnityEngine;

public static partial class RuleEngineTest
{
    // ---- `tutorial_decks.json` 的最小镜像（`JsonUtility` 只认 public 字段 + 数组，⛔ 不能有字典）----
    [System.Serializable]
    public class TutDeckSideB115
    {
        public string deckId;
        public string name;
        public string heroId;
        public string[] cardIds;
        public string[] missing;
        public bool complete;
    }

    [System.Serializable]
    public class TutStageB115
    {
        public int stage;
        public TutDeckSideB115 player;
        public TutDeckSideB115 ai;
    }

    [System.Serializable]
    public class TutDocB115
    {
        public int stageCount;
        public TutStageB115[] stages;
    }

    /// <summary>判据函数（**唯一一份**）：12 副是否全部「卡全解析出 + 督军解析出」。
    /// <para>A 段（真产物）与 B 段（最小反例）**都走这一份** —— 否则「断言」和「灭自证的断言」会各写一套口径。</para>
    /// <para><paramref name="why"/> 恒有值：绿时写「12 副全 complete…」，红时逐副点名 + 缺的张数。</para></summary>
    static bool JudgeTutorialDecksB115(TutDocB115 doc, out string why)
    {
        var bad = new List<string>();
        if (doc == null) { why = "`tutorial_decks.json` 解析出来是 null"; return false; }
        if (doc.stages == null || doc.stages.Length == 0) { why = "`stages` 为空"; return false; }

        for (int i = 0; i < doc.stages.Length; i++)
        {
            var s = doc.stages[i];
            int no = (s != null && s.stage > 0) ? s.stage : i + 1;
            if (s == null) { bad.Add("第 " + no + " 关：整关是 null"); continue; }
            JudgeOneTutorialSideB115(no, "玩家", s.player, bad);
            JudgeOneTutorialSideB115(no, "敌方", s.ai, bad);
        }

        if (bad.Count == 0)
        {
            why = "12 副的 `missing` 全空、`heroId` 全非空、`complete` 全 true";
            return true;
        }
        why = string.Join("；", bad.ToArray());
        return false;
    }

    static void JudgeOneTutorialSideB115(int no, string role, TutDeckSideB115 side, List<string> bad)
    {
        string who = "第 " + no + " 关" + role
                   + "（`" + (side != null && !string.IsNullOrEmpty(side.name) ? side.name : "?") + "`）";
        if (side == null) { bad.Add(who + "：整副是 null"); return; }
        if (string.IsNullOrEmpty(side.deckId)) bad.Add(who + "：`deckId` 空");
        if (string.IsNullOrEmpty(side.heroId)) bad.Add(who + "：`heroId` 空（督军没解析出 ⇒ `complete` 必然 false）");
        if (side.cardIds == null || side.cardIds.Length == 0) bad.Add(who + "：`cardIds` 为空（一张牌都没有）");
        if (side.missing != null && side.missing.Length > 0)
            bad.Add(who + "：缺 " + side.missing.Length + " 条 [" + string.Join("; ", side.missing) + "]");
        if (!side.complete) bad.Add(who + "：`complete = false`");
    }

    /// <summary>`B-308` 的宿主闸：教程 12 副必须全 `complete`（= `gen_prebuilt_decks.py --check` 那条口径的哨兵）。
    /// <para>只说「等于 true」是自证式断言 ⇒ 另用**同一个判据**判一次最小反例（B1/B2/B2b），
    /// 并用三条**绕开 `complete` 列**的独立读数（A6/A6b/A7）交叉钉死。</para></summary>
    public static void CheckB115W13TutorialDecksComplete()
    {
        Section("🆕 B115_W13 · B-308：教程 12 副卡组必须全 `complete`（`gen_prebuilt_decks.py --check` 的宿主闸）");

        var asset = Resources.Load<TextAsset>("tutorial_decks");
        CheckTrue(asset != null,
                  "A0 前提：`Assets/RuleEngine/Resources/tutorial_decks.json` 加载得到"
                + "（拿不到 ⇒ 本节余下全部等于没验；那一份是 `gen_prebuilt_decks.py` 的产物，⛔ 别手改）");
        if (asset == null) return;

        var doc = JsonUtility.FromJson<TutDocB115>(asset.text);
        CheckTrue(doc != null && doc.stages != null,
                  "A1 前提：JSON 解析得出 `stages`（`JsonUtility` 只认 public 字段 + 数组）");
        if (doc == null || doc.stages == null) return;

        Check(doc.stageCount, 6, "A2 `stageCount` = 6（原版教程 6 关：`*_Deck0_Tutorial{1..6}_*`）");
        Check(doc.stages.Length, 6, "A3 `stages` 数组 = 6 关");

        var sides = new List<TutDeckSideB115>();
        foreach (var s in doc.stages)
        {
            if (s == null) continue;
            if (s.player != null) sides.Add(s.player);
            if (s.ai != null) sides.Add(s.ai);
        }
        Check(sides.Count, 12, "A4 12 副（6 关 × 玩家/敌方）—— 少了就是产物缺副（`--check` 报 rc=1 的那一档）");

        // ---- A5：真产物必须过判据（红时 why 里逐副点名 + 缺几张）----
        string why;
        bool ok = JudgeTutorialDecksB115(doc, out why);
        CheckTrue(ok,
                  "★ A5 教程 12 副**全部** `complete`（口径 = `make_entry()` 的 `not missing and hero_id is not None`）"
                + "｜🧨 卡池 / id 表退回去重跑生成器、或有人手改产物 ⇒ 本条红并点名是哪几副、缺几张。现读：" + why);

        // ---- A6 / A6b / A7：**绕开 `complete` 这一列**的独立读数（防「生成器算错成恒 true」）----
        int missingDecks = 0, missingTotal = 0;
        foreach (var side in sides)
        {
            if (side == null || side.missing == null) continue;
            if (side.missing.Length > 0) missingDecks++;
            missingTotal += side.missing.Length;
        }
        Check(missingDecks, 0,
              "★ A6 独立口径：`missing` **非空**的副 = 0（不经 `complete` 这一列）"
            + "｜🧨 生成器把 `complete` 算成恒 true ⇒ A5 仍绿、本条红");
        Check(missingTotal, 0, "★ A6b `missing` 条目**合计** = 0（同上，数的是条目不是副）");

        int noHero = 0;
        foreach (var side in sides) if (side == null || string.IsNullOrEmpty(side.heroId)) noHero++;
        Check(noHero, 0,
              "★ A7 `heroId` 为空的副 = 0（督军没解析出 ⇒ 那一副在原版里**进不了战斗**；"
            + "这也是 `complete` 的第二半，独立于 `missing`）");

        // ---- B 段：同一个判据对**最小反例**必须判红（否则 A5 是恒真式）----
        var badDoc = JsonUtility.FromJson<TutDocB115>(asset.text);      // 再解析一份，⛔ 不碰 A 段那一具
        bool premiseOk = badDoc != null && badDoc.stages != null && badDoc.stages.Length >= 3
                         && badDoc.stages[2] != null && badDoc.stages[2].player != null;
        CheckTrue(premiseOk,
                  "（前提·不静默）能再解析一份产物、且**第 3 关玩家**那副在手（拿不到 ⇒ B1 / B2 / B2b 三条灭自证等于没验）");
        if (premiseOk)
        {
            badDoc.stages[2].player.missing = new[] { "B115W13-SENTINEL x2 (unknown)" };   // 最小反例：塞回一条 missing
            badDoc.stages[2].player.complete = false;

            string whyBad;
            bool okBad = JudgeTutorialDecksB115(badDoc, out whyBad);
            CheckTrue(!okBad,
                      "★ B1 灭自证：把第 3 关玩家的 `missing` 塞回一条、`complete=false` ⇒ **同一个判据必须判红**"
                    + "（判据写成恒 true ⇒ 本条红）");
            CheckTrue(whyBad != null && whyBad.IndexOf("第 3 关", System.StringComparison.Ordinal) >= 0,
                      "★ B2 灭自证：判红时**点名**是哪一副（第 3 关 · 玩家）—— " + whyBad);
            CheckTrue(whyBad != null && whyBad.IndexOf("SENTINEL", System.StringComparison.Ordinal) >= 0,
                      "★ B2b 判红时把那一条 `missing` **原样印出来**（「缺几张」要人可读，⛔ 不是只给个 bool）—— " + whyBad);
        }
    }
}
