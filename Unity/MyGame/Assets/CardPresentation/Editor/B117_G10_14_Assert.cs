// B117_G10_14_Assert.cs — 执行代理 `B117-G10-14`（战斗域第 14 部分「选牌 · 多卡展示」）的断言宿主
//
// 覆盖本批**当场修掉**的两处（判据全部来自第一权威反编译，逐条写在每组头里）：
//   A. `ChooseCardMenu.ToogleChooseMenuVisibility` / `ShowChooseCardElements` ——
//      **眼睛一次收五样**（标题 / `Continue Button` / 每张候选卡 / 压暗层），**眼睛自己不动**。
//      （改前我们只收了「卡下那颗钮 + 它的字」两样。）
//   B. `ChooseCardMenu.SelectCard` —— 选中一张要播 `selectCardSound`（cue = **`CardExpand`**）。
//      （改前一声不响。）
//   C. **灭自证**：A 的两条断在**同一个坐标**上、且「收起来」那几件与「眼睛还开着」同时成立 ——
//      一个「把 `HitDone` 写死成 false」的实现会在「未收时必须判中」那条红；
//      一个「连眼睛一起收」的实现会在「眼睛仍 active」那条红。
//
// 用法（⚠️ 要**主对话/调度台**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_14_Assert.Run -logFile -
//
// 判绿红：`grep "^B117G1014"` 的**合计行**；逐条按**行首** `B117G1014 ✓` / `B117G1014 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求：**零前提** —— 自己建相机 + `LayoutSpace.Apply` + 一棵临时根，收尾自己 `DestroyImmediate`。
//   ⚠️ 它**不销毁** `HudCanvas` 根画布（那是静态件、别处还要复用；同 `Editor/BattleScene.cs` 的做法）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B117_G10_14_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1014 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1014 ✗ " + msg); }
    }

    /// <summary>一组跑挂了**不许**把后面几组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。</summary>
    static void Group(string name, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, "组【" + name + "】抛异常（这一组后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B117G1014_ProbeRoot");
        GameObject camGo = null;
        ChoosePanel panel = null;
        try
        {
            camGo = new GameObject("Main Camera (B117G1014)");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);          // ⚠️ 必须在 `HudCanvas.Ensure` 之前（`HudCanvas.cs:88`）

            panel = ChoosePanel.Create(root.transform);
            Check(panel != null, "（前提）选牌面板建得出来（`OverHand` 子画布立起来了）");
            if (panel == null) { Finish(); return; }

            Group("A·眼睛收内容", () => A_EyeHidesContent(panel, root.transform));
            Group("B·选中音", () => B_SelectSound(panel, root.transform));
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
            if (camGo != null) Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(root);
        }
        Finish();
    }

    static void Finish()
    {
        Debug.Log("B117G1014 ==== 合计：✓ " + _pass + " · ✗ " + _fail + " ====");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  A. 眼睛 = 收/放「面板内容整组」
    //
    //  判据（`decomp_full`，逐句现读）：
    //   · `ChooseCardMenu__ToogleChooseMenuVisibility.c:8-13` —— 读 `titleTextLocalize` 那个 GO 的
    //     `activeInHierarchy` **取反** → `ShowChooseCardElements(!active)` ⇒ **开关**；
    //   · `ChooseCardMenu__ShowChooseCardElements.c:10/14/18/19/22` —— `SetActive(param_2)` 打在
    //     ① `titleTextLocalize.gameObject` ② `titleTextField` ③ `chooseButton.gameObject`（= `Continue Button`），
    //     然后 `ShowChooseCards(param_2)`（逐张 `cardUI.gameObject.SetActive`，
    //     `ChooseCardMenu__ShowChooseCards.c:50-52`）、最后 `Shade.SwitchShade(shade, param_2, 0, 0)`；
    //   · **不动** `buttonsGroup`（`+0x48`）—— 眼睛 `HideChooseButton` 就挂在它下面
    //     （RT_3051 的 `m_Father = RT_3448 = ButtonsGroup`）⇒ 收完还能点回来。
    //
    //  🧨 改坏法：`ToggleEye` 退回「只收卡钮 + 卡钮的字」⇒ 标题/压暗/`HitDone` 三条红；
    //     把眼睛也 `SetActive(false)` ⇒ 「眼睛仍 active」那条红。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void A_EyeHidesContent(ChoosePanel panel, Transform root)
    {
        var cards = new List<CardView>();
        try
        {
            cards.Add(CardView.Create(root, UnitData("B117G1014 A0"), "g1014_a0"));

            panel.Open(cards, "B117G1014 眼睛", "offensive");
            Check(panel.Visible, "（前提）面板开着");
            Check(panel.CardCount == 1, "（前提）候选卡摊开了 1 张");
            // 前提（硬检查）：这几件得真建出来，否则下面的「灭了」分不清是「本来就没有」还是「收对了」
            bool hasTitle = panel.TitleActiveForTest;
            bool hasShade = panel.ShadeActiveForTest;
            bool hasEye = panel.EyeActiveForTest;
            Check(hasTitle, "（前提）标题那一行建出来了（`ChoiceTitle`）");
            Check(hasShade, "（前提）整屏压暗建出来了（`ChoiceShade`）");
            Check(hasEye, "（前提）眼睛那颗钮建出来了（`ChoiceEye`）");
            if (!(hasTitle && hasShade && hasEye)) return;

            // 同一枚坐标：确认（`DoneWorldPos` = 确认字/底条那个实件的世界位置）
            var done = panel.DoneWorldPos;
            Check(panel.HitDone(done), "（前提）**未收**时确认点判中（后面「收起来不判中」要有对照）");
            Check(!panel.ContentHiddenForTest, "（前提）开面板时内容是全显的（原版 `Setup` 自己 SetActive(true)）");

            // ---- 第一下：收 ----
            panel.ToggleEye();
            Check(panel.ContentHiddenForTest, "★ A1 眼睛第一下 = **收**（原版读 `activeInHierarchy` 取反）");
            Check(!panel.TitleActiveForTest, "★ A2 收起来 ⇒ **标题** inactive（`ShowChooseCardElements` ①②）");
            Check(!panel.ShadeActiveForTest, "★ A3 收起来 ⇒ **压暗层** inactive（`Shade.SwitchShade(false)`）"
                                           + "｜🧨 改前只收卡钮 ⇒ 压暗还盖着 ⇒ 本行红");
            Check(!panel.HitDone(done),
                  "★ A4 收起来 ⇒ **同一枚确认点不再判中**（原版那颗 `Button` 的 GO 已 inactive）"
                + "｜🧨 只切渲染、不闸命中 ⇒ 本行红");
            Check(panel.EyeActiveForTest,
                  "★ A5 收起来 ⇒ **眼睛自己仍然 active**（原版 `buttonsGroup` 不在收的名单里）"
                + "｜🧨 连眼睛一起收 ⇒ 这一下点不回来 ⇒ 本行红");
            Check(panel.Visible,
                  "★ A6 收的是**内容**、不是窗：`Visible` 仍为真（与 `SetVisible(false)` 是两个状态）");

            // ---- 第二下：放 ----
            panel.ToggleEye();
            Check(!panel.ContentHiddenForTest, "★ A7 眼睛第二下 = **放**");
            Check(panel.TitleActiveForTest && panel.ShadeActiveForTest && panel.EyeActiveForTest,
                  "★ A8 放回来 ⇒ 标题 / 压暗 / 眼睛三件同时复活");
            Check(panel.HitDone(done), "★ A9 放回来 ⇒ 同一枚确认点重新判中（A4 的逆向，成对）");
        }
        finally
        {
            panel.Close();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) Object.DestroyImmediate(cards[i].gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  B. 选中一张牌要响 `selectCardSound`
    //
    //  判据：
    //   · `ChooseCardMenu__SelectCard.c:24-40` —— `selectedCard = card` → `set_interactable(chooseButton, 1)`
    //     → `SoundManager.Play2D(selectCardSound)` → 其余卡 `ToggleSelected(false)`；
    //   · cue 名 = **`CardExpand`**：`selectCardSound` 是 PPtr（`battlearena1/MonoBehaviour_4353.json`
    //     `m_PathID 4006604330728631164`）→ 导出工程 `Battle Arena 1.unity:236733` 记的是
    //     `guid 8d495f12d0537b14e859f3c0141e1e34` → 该 guid 的 `.meta` 就是 `CardExpand.asset.meta`
    //     （同一颗 cue 也是放大窗的开窗音：`Shell/WindowSounds.cs:80`）。
    //
    //  🧨 改坏法：删掉 `TogglePick` 里那句 `WindowSounds.PlayFx(...)` ⇒ B1 红；
    //     把它挪到「不管选没选中都播」（比如 `TogglePick` 开头）⇒ B2 红（再点同一张不该响）。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void B_SelectSound(ChoosePanel panel, Transform root)
    {
        var cards = new List<CardView>();
        try
        {
            cards.Add(CardView.Create(root, UnitData("B117G1014 B0"), "g1014_b0"));
            cards.Add(CardView.Create(root, UnitData("B117G1014 B1"), "g1014_b1"));

            panel.Open(cards, "B117G1014 选中音", "offensive");
            Check(panel.CardCount == 2, "（前提）候选卡摊开了 2 张（单选要能换选）");

            WindowSounds.ResetCounters();
            panel.TogglePick(0);
            Check(panel.IsPicked(0), "（前提）第 0 张被选中了");
            Check(WindowSounds.LastCue == WindowSounds.CardExpandCue,
                  "★ B1 选中一张 ⇒ 播原版 `selectCardSound`（cue `" + WindowSounds.CardExpandCue + "`）"
                + "（实得 `" + (WindowSounds.LastCue ?? "<没播>") + "`）"
                + "｜🧨 不接线 ⇒ 本行红");

            // 单向：再点同一张什么都不做（原版那颗钮 `set_interactable(false)`）⇒ **不许重复响**
            WindowSounds.ResetCounters();
            panel.TogglePick(0);
            Check(panel.IsPicked(0) && WindowSounds.LastCue == null,
                  "★ B2 再点**同一张** ⇒ 不取消、也不重复响（原版 `Single` 单向）"
                + "（实得 cue `" + (WindowSounds.LastCue ?? "<没播>") + "`）");

            // 换选另一张 ⇒ 仍然响（`SelectCard` 对每张新选中的卡都播）
            WindowSounds.ResetCounters();
            panel.TogglePick(1);
            Check(panel.IsPicked(1) && !panel.IsPicked(0) && WindowSounds.LastCue == WindowSounds.CardExpandCue,
                  "★ B3 **换选另一张** ⇒ 上一张让位、这一张仍然响 `selectCardSound`"
                + "（实得 cue `" + (WindowSounds.LastCue ?? "<没播>") + "`）");
        }
        finally
        {
            panel.Close();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) Object.DestroyImmediate(cards[i].gameObject);
        }
    }

    /// <summary>最小卡夹具 —— ⚠️ `CardData.Simple` 的 `faction` 恒 null（本仓已知坑），这里填满，
    /// 免得卡面那几行不建、把「卡建出来了」这条前提弄成假绿。</summary>
    static CardData UnitData(string title)
    {
        var d = CardData.Simple(title, 3, 3, 4);
        d.type = "unit";
        d.isUnit = true;
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }
}
