// B117_G10_13_Assert.cs — 执行代理 `B117-G10-13`（战斗域第 13 部分：**卡牌详情窗 · Tooltip**）的断言
//
// ============================ 判据（一手，全部现读）============================
//  🥇 反编译（`d:/2/tools/decomp_full/`）：
//    · `CardDisplayWindow__SetCardVoiceOver.c:60-66` —— 语音钮可见性的**唯一写者**：
//        `GameObject.SetActive(voiceOverButton, 0 < availableSounds.Count)`
//      （另两支：卡种 / 远开关两个 remote-config 位；本地无远端配置 ⇒ 判据退化成「这张卡有没有语音」）
//    · `CardDisplayWindow__Open.c` / `__Close.c` —— **没有任何一处按「窗开没开」动那颗钮**
//      （整扇窗靠 `GameWindow` 的 `activeSelf` 开关，见 `GameWindow__TryOpen.c`）
//    · `CardDisplayWindow__DisplayCardEffects.c:88-105` —— `RawCardScript.IsEnviromentaleffect`
//      （= `spellType ∈ {210, 200}`）⇒ 「受到以下影响」**整组强制隐藏**
//    · `CardDisplayWindow__ChangeCardPosition.c:203-221` —— 换位只补 `localPosition/localRotation/localScale`
//      + 一条染色 tween，全部同一个时长（窗上 `relatedCardSwapTime`）
//    · `CardDisplayWindow__InitializeRelatedCards.c:26-68` —— 逐张 `GetRelatedCards()`（= `relatedCard1..4`）
//  🥈 原版场景（`assets_full/bundle_scenes_scenes_battlearena1/`，本批现读）：
//    · `RectTransform/RectTransform_3533.json`（`LowerSection`）：`m_SizeDelta = (1320, 137)`
//    · `RectTransform/RectTransform_2753.json`（`FlavourTextBG`）：`m_SizeDelta = (1320, 178)`
//    · `RectTransform/RectTransform_3221.json`（`Voice Over Button`）：
//        `m_SizeDelta = (88.655, 88.655)` · `m_AnchoredPosition = (734.33, 0.0001831)`（相对 `LowerSection` 中心）
//    · `GameObject/EffectList.json`：`m_IsActive = False`；`Elements` 下 **5** 个 `EffectElement`
//    · `MonoBehaviour_4750.json`（本窗那一颗）：`relatedCardSwapTime = 0.25` ·
//        `cardInBackGroundColorTint = (0.65, 0.65, 0.65, 1)`
//
// ============================ 本批修了什么 ============================
//  唯一一处：`SetChrome(true)` 原来无条件 `_voiceBtn.SetActive(true)` ⇒ **覆盖**了排在它前面的
//  `RefreshButtons()`（按卡算「有没有语音」）⇒ 没有语音的卡（战术卡 4/484 有语音）开窗后
//  多出一颗按了没反应的语音钮。改成**只在关窗时收**（`!on`）。
//  ⇒ B 组就是钉这条；C/D/E 组是同一部分的回归锁。
//
// ============================ 灭自证（结构上不可能同时满足）============================
//  ① **B3 ⇄ B5 成对**：只断「没语音 ⇒ 钮不亮」的话，「把所有卡的钮都关掉」也能满足；
//     反过来只断「有语音 ⇒ 亮」，那「一律点亮」（改前那版）也能满足。两条一起 ⇒ 只有
//     「按卡决定」这一种实现能同时过。
//  ② **D3 ⇄ D4 成对**：只断 `Find(TAU3, 4).Count == 4` 的话，「忽略 `max` 参数」也能满足；
//     再断 `Find(TAU3, 3).Count == 3` 才钉住「`max` 真被尊重」。
//  ③ **C5 结构锁**：语音钮中心偏移 **734.33 > 父件半宽 660** ⇒ 原版那颗钮**本来就在
//     `LowerSection` 矩形之外**。谁觉得「这样不合理」把它挪进去，C4/C5 一起红。
//  ④ **E2 两侧**：环境效果卡档（210/200 ⇒ 真）与普通档（0/1 ⇒ 假）同时断 ——
//     只断一侧的话「恒真」或「恒假」都能过。
//
// 用法（⚠️ 要主对话跑 —— 子代理不许 `-executeMethod`）。本文件**不自带宿主**（本批红线：
// ⛔ 不改 `Editor/*Scene.cs`）；接线 = 在任意宿主（建议 `BattleScene.Run`）里加一行：
//     B117_G10_13_Assert.Run();
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117_G10_13_Assert
{
    const string P = "B117G1013 ";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5") + "；实得 " + got.ToString("F5") + "）");
    }

    // ---- 原版字面量（⛔ 一律写死原版 prefab / 反编译里读到的那几个数，不从我们的常量反推）----
    const float OrigLowerW      = 1320f;      // LowerSection.m_SizeDelta.x
    const float OrigLowerH      = 137f;       // LowerSection.m_SizeDelta.y
    const float OrigLoreBgH     = 178f;       // FlavourTextBG.m_SizeDelta.y
    const float OrigVoiceSize   = 88.655f;    // Voice Over Button.m_SizeDelta = (88.655, 88.655)
    const float OrigVoiceOffX   = 734.33f;    // Voice Over Button.m_AnchoredPosition.x（相对 LowerSection 中心）
    const float OrigSwapTime    = 0.25f;      // 本窗 MB.relatedCardSwapTime
    const float OrigBackTint    = 0.65f;      // 本窗 MB.cardInBackGroundColorTint
    const int   OrigEffectSlots = 5;          // Elements 下 EffectElement 的个数（现数 5）
    const int   OrigFanSlots    = 5;          // cardUIs[5]（RawCardScript.relatedCard1..4 真值最多 4 + 主卡 1）

    const string SrcRel = "CardPresentation/Battle/CardDisplayWindow.cs";

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(P + "--- B117-G10-13：卡牌详情窗 · Tooltip（语音钮可见性 + 下缘几何 + 相关卡 + 效果清单）---");
        RunSource();
        RunGeometry();
        RunRelatedAndSwap();
        RunEffectList();
        RunBehaviour();
        Debug.Log(P + "B117G1013 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // ============================================================ §A 源锁
    static void RunSource()
    {
        string path = Path.Combine(Application.dataPath, SrcRel);
        Check(File.Exists(path), "A0 **前提**：读得到 `" + SrcRel + "`（" + path + "）—— 读不到时 §A 等于没验 ⇒ 当场红");
        if (!File.Exists(path)) return;

        string src;
        try { src = File.ReadAllText(path); }
        catch (Exception e) { Check(false, "A0' 读源码抛异常：" + e.Message); return; }

        // ---- B1：`SetChrome` 里不许再出现「一律按 on 点亮语音钮」那一句 -------------------
        int sc = src.IndexOf("void SetChrome(bool on)", StringComparison.Ordinal);
        Check(sc >= 0, "B1 **前提**：`SetChrome(bool on)` 还在（没被改名/搬走）");
        if (sc >= 0)
        {
            int end = src.IndexOf("\n        }", sc + 10, StringComparison.Ordinal);
            string body = end > sc ? src.Substring(sc, end - sc) : src.Substring(sc, Math.Min(600, src.Length - sc));
            Check(body.IndexOf("_voiceBtn.gameObject.SetActive(on)", StringComparison.Ordinal) < 0,
                  "B1 `SetChrome` **不再**无条件 `_voiceBtn.gameObject.SetActive(on)`"
                + "（原版那颗钮的唯一写者是 `SetCardVoiceOver`；这一句会把 `RefreshButtons` 的结果盖掉）");
            Check(body.IndexOf("_voiceBtn != null && !on", StringComparison.Ordinal) >= 0,
                  "B1' `SetChrome` 只在**关窗**（`!on`）时收语音钮");
        }

        // ---- B2：`Show` 里 `RefreshButtons()` 必须排在 `SetChrome(true)` **之前** -----------
        int show = src.IndexOf("public void Show(CardData d, CardDef def, IReadOnlyList<EffectRow> effects, bool faceDown)", StringComparison.Ordinal);
        Check(show >= 0, "B2 **前提**：`Show(CardData, CardDef, IReadOnlyList<EffectRow>, bool)` 还在");
        if (show >= 0)
        {
            int rb = src.IndexOf("RefreshButtons();", show, StringComparison.Ordinal);
            int ch = src.IndexOf("SetChrome(true);", show, StringComparison.Ordinal);
            Check(rb >= 0 && ch >= 0 && rb < ch,
                  "B2 `Show` 里 `RefreshButtons()` 排在 `SetChrome(true)` **之前**"
                + "（顺序反了的话，`SetChrome(true)` 修好之后又会把结果盖掉 —— 两句是一对，⛔ 别对调）");
        }
    }

    // ============================================================ §C 下缘几何（原版字面量）
    static void RunGeometry()
    {
        CheckNear(CardWinBox.LowerR - CardWinBox.LowerL, OrigLowerW, 0.05f, "C1 `LowerSection` 宽 = 原版 1320");
        CheckNear(CardWinBox.LowerB - CardWinBox.LowerT, OrigLowerH, 0.05f, "C2 `LowerSection` 高 = 原版 137");
        CheckNear(CardWinBox.LoreBgB - CardWinBox.LoreBgT, OrigLoreBgH, 0.05f, "C3 `FlavourTextBG` 高 = 原版 178");

        float lowerCx = (CardWinBox.LowerL + CardWinBox.LowerR) * 0.5f;
        float lowerCy = (CardWinBox.LowerT + CardWinBox.LowerB) * 0.5f;
        CheckNear(CardWinBox.BtnS, OrigVoiceSize, 0.01f, "C4 语音钮边长 = 原版 88.655");
        CheckNear(CardWinBox.VoiceCx - lowerCx, OrigVoiceOffX, 0.05f,
                  "C5 语音钮中心相对 `LowerSection` 中心的 x 偏移 = 原版 734.33");
        CheckNear(CardWinBox.BtnCy - lowerCy, 0f, 0.05f,
                  "C5' 语音钮中心相对 `LowerSection` 中心的 y 偏移 = 原版 0");

        // 灭自证 ③：原版那颗钮**在父矩形之外**（734.33 > 660）。谁把它「挪进去」就红。
        Check(OrigVoiceOffX > OrigLowerW * 0.5f,
              "C6 **灭自证**：语音钮偏移 " + OrigVoiceOffX.ToString("F2") + " > `LowerSection` 半宽 "
            + (OrigLowerW * 0.5f).ToString("F2") + " ⇒ 原版那颗钮**本来就在父矩形外**（⛔ 别「顺手挪进去」）"
            + "；本断的实得值同样成立 = " + ((CardWinBox.VoiceCx - lowerCx) > (CardWinBox.LowerR - CardWinBox.LowerL) * 0.5f));
        Check((CardWinBox.VoiceCx - lowerCx) > (CardWinBox.LowerR - CardWinBox.LowerL) * 0.5f,
              "C6' 我们的语音钮同样落在父矩形之外（与 C5 合成一条「照原版摆」的锁）");
    }

    // ============================================================ §D 相关卡 / 换位
    static void RunRelatedAndSwap()
    {
        Check(CardFan.OriginalSlots == OrigFanSlots,
              "D1 `CardFan.OriginalSlots` = 原版 `cardUIs[5]`（实得 " + CardFan.OriginalSlots + "）");
        CheckNear(CardFan.SwapTime, OrigSwapTime, 1e-6f, "D2 换位时长 = 原版 `relatedCardSwapTime` 0.25");
        CheckNear(CardFan.BackTint.r, OrigBackTint, 1e-4f, "D3 相关卡压暗 r = 原版 `cardInBackGroundColorTint` 0.65");
        CheckNear(CardFan.BackTint.g, OrigBackTint, 1e-4f, "D3' 相关卡压暗 g = 0.65");
        CheckNear(CardFan.BackTint.b, OrigBackTint, 1e-4f, "D3'' 相关卡压暗 b = 0.65");
        Check(CardFan.FrontTint.r > 0.999f && CardFan.FrontTint.g > 0.999f && CardFan.FrontTint.b > 0.999f,
              "D3''' 前台那张是纯白（原版 `_DAT_1834b2e50` = Color.white）");

        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "D4 **前提**：卡池载得出来");
        if (pool == null || pool.Count == 0) return;
        var tau3 = CardDatabase.FindById(pool, "TAU3");
        Check(tau3 != null, "D4' **前提**：`TAU3` 在池内（真值表里它有 **4** 条相关卡，是唯一一个「满 4」且 id 全可解析的样本）");
        if (tau3 == null) return;

        var four = RelatedCards.Find(tau3, 4);
        Check(four.Count == 4, "D5 `RelatedCards.Find(TAU3, max=4)` 得 4 张（实得 " + four.Count + "；= 原版 `relatedCard1..4` 装满）");
        var three = RelatedCards.Find(tau3, 3);
        // 灭自证 ②：只断 D5 的话「忽略 max」也能过 ⇒ 再断一次「max 真被尊重」。
        Check(three.Count == 3,
              "D6 **灭自证**：`Find(TAU3, max=3)` 得 3 张（实得 " + three.Count
            + "）—— 只断 max=4 那一档的话「把 max 参数丢掉」也能满足");
        Check(three.Count == 0 || four.Count == 0 || three[0].Id == four[0].Id,
              "D7 截断是**从尾部切**（`max=3` 与 `max=4` 的第一张相同 —— 原版 `while(truth.Count > max) RemoveAt(Count-1)`）");

        var again = RelatedCards.Find(tau3, 4);
        bool same = again.Count == four.Count;
        for (int i = 0; same && i < four.Count; i++) same = again[i].Id == four[i].Id;
        Check(same, "D8 同一张卡两次 `Find` 顺序稳定（真值表是**顺序敏感**的：槽 1→4）");
    }

    // ============================================================ §E 效果清单
    static void RunEffectList()
    {
        Check(CardWinBox.EffSlots == OrigEffectSlots,
              "E1 效果清单槽数 = 原版 `Elements` 下的 `EffectElement` 个数 5（实得 " + CardWinBox.EffSlots + "）");

        // 灭自证 ④：环境效果卡那一道闸 —— 真档与假档一起断。
        Check(EffectsToShow.IsEnvEffectCard(210), "E2 环境效果卡闸：`spellType 210` ⇒ 真（原版 `IsEnviromentaleffect`）");
        Check(EffectsToShow.IsEnvEffectCard(200), "E2' 环境效果卡闸：`spellType 200` ⇒ 真");
        Check(!EffectsToShow.IsEnvEffectCard(0),  "E2'' 环境效果卡闸：`spellType 0`（Normal）⇒ 假（⛔ 别写成恒真）");
        Check(!EffectsToShow.IsEnvEffectCard(1),  "E2''' 环境效果卡闸：`spellType 1` ⇒ 假");
    }

    // ============================================================ §B/§E 行为（真要开一扇窗）
    static void RunBehaviour()
    {
        GameObject root = null;
        CardDisplayWindow win = null;
        try
        {
            root = new GameObject("B117G1013_Probe", typeof(RectTransform));
            win = CardDisplayWindow.Create(root.transform);
        }
        catch (Exception e)
        {
            Check(false, "F0 **前提**：`CardDisplayWindow.Create` 抛异常 ⇒ §B/§E 的行为断言全没验（" + e.Message + "）");
        }
        Check(win != null, "F0' **前提**：展示窗建出来了");
        if (win == null) { if (root != null) UnityEngine.Object.DestroyImmediate(root); return; }

        // E3：出厂关着（原版 `EffectList.m_IsActive = false`）
        Check(!win.EffectsVisible, "E3 新窗的 `EffectList` 出厂关着（原版 `m_IsActive = False`）");

        var voicePiece = win.VoicePieceForTest;
        Check(voicePiece != null, "F1 **前提**：语音钮建出来了（`40k_UI_bt_voicelines` 在 `Resources/Art/ui/`）");
        if (voicePiece != null)
        {
            const string NoVoiceId = "__b117_g1013_no_voice__";   // 一定不在语音表里
            var noVoice = new CardData
            {
                id = NoVoiceId, artId = NoVoiceId, title = "no-voice probe",
                cost = 1, melee = 1, ranged = 0, health = 1, armor = 0,
                keywords = "", isUnit = true, faction = "AstraMilitarum", rarity = "common", type = "tactic",
            };
            Check(!CardButtons.HasVoice(noVoice), "F2 **前提**：探针卡确实没有语音（`CardButtons.HasVoice` 为假）");

            win.Show(noVoice, null, null);
            Check(win.Visible, "F3 **前提**：窗真的开了（`Visible`）");
            Check(!voicePiece.gameObject.activeSelf,
                  "B3 ★ **本条钉的是本批那个修复**：没有语音的卡 ⇒ 开窗后语音钮**不亮**"
                + "（改前 `SetChrome(true)` 会把它点亮 ⇒ 这一条红）");

            win.Hide();
            Check(!voicePiece.gameObject.activeSelf, "B4 关窗后语音钮一定不亮（`!on` 那一半没被改坏）");

            // 灭自证 ①：另一侧 —— 有语音的卡必须**亮**。只断 B3 的话「一律不建」也能过。
            var pool = CardDatabase.Load();
            var am1 = pool != null ? CardDatabase.FindById(pool, "AM1") : null;
            Check(am1 != null, "F4 **前提**：`AM1`（Lord Solar Leontus）在池内");
            if (am1 != null)
            {
                var d = BattleDriver.ToCardData(am1, am1.Faction);
                if (string.IsNullOrEmpty(d.artId)) d.artId = am1.Id;
                Check(CardButtons.HasVoice(d),
                      "F5 **前提**：`AM1` 在语音表里（表一变，本条会红 —— 那是数据的事，不是这儿静默跳过）");
                win.Show(d, null, null);
                Check(voicePiece.gameObject.activeSelf,
                      "B5 **灭自证**：**有语音**的卡 ⇒ 开窗后语音钮**亮**"
                    + "（与 B3 成对：只断 B3 的话「把所有钮都关掉」也能满足）");
                win.Hide();
            }
        }

        try { UnityEngine.Object.DestroyImmediate(root); }
        catch (Exception e) { Debug.LogWarning(P + "探针根没销毁（" + e.Message + "）—— 不影响断言计数"); }
    }
}
