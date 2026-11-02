// B117_G10_17_Assert.cs — 执行代理 `B117-G10-17`（战斗域第 17 部分「换牌 Mulligan」）的断言宿主
//
// 覆盖本批**当场修掉 / 核过**的东西（判据全部来自第一权威反编译，逐条写在每组头里）：
//   A. `MulliganManager.ShowMulliganElements(bool)` —— 眼睛那颗钮**一次收四组**
//      （提示行 / 完成钮 / 每张换牌卡 + 每张卡的钮·字·红标记 / 压暗层），**眼睛自己不动**。
//      （改前我们只收了「卡钮 + 钮上的字 + 压暗层」三样。）
//   B. **标记 = 原地**（`CardScript.ClickMulliganSelected` 只翻 `cardState 0xD⇄0xE` + `SetAsMulliganSelected`，
//      **不搬牌**；牌早在 `PlayerHand.AddCardsToMulligan` / `MoveCardToMulliganPos` 里就整批搬进换牌区那一行了）。
//   C. 倒计时文案（`MulliganManager.SetMulliganTimer` = `"0:0" + 秒数`，**只在前十秒写**）。
//
// 灭自证（⛔ 不是「新写法对」那种同义反复）：
//   · A5 —— 「连眼睛一起收」的实现在这条红（收完就再也点不回来）；
//   · A6/A7 —— 「只切 `activeSelf`、不闸命中」的实现会在这里红：`ButtonIndexAt` 走的是**矩形**
//     （`HudPiece.Contains` 不看 `activeSelf`）、`HitDone` 走的是**常量算出来的框**
//     ⇒ 不显式闸住的话，收起来之后点同一枚坐标**照样会标记 / 照样会提交**。
//
// 用法（⚠️ 要**主对话/调度台**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_17_Assert.Run -logFile -
//
// 判绿红：`grep "^B117G1017"` 的**合计行**；逐条按**行首** `B117G1017 ✓` / `B117G1017 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求：**零前提** —— 自己建相机 + `LayoutSpace.Apply` + 一棵临时根，收尾自己 `DestroyImmediate`。
//   ⚠️ 它**不销毁** `HudCanvas` 根画布（那是静态件、别处还要复用；同 `Editor/BattleScene.cs` 的做法）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B117_G10_17_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1017 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1017 ✗ " + msg); }
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
        var root = new GameObject("B117G1017_ProbeRoot");
        GameObject camGo = null;
        MulliganPanel panel = null;
        try
        {
            camGo = new GameObject("Main Camera (B117G1017)");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);          // ⚠️ 必须在 `HudCanvas.Ensure` 之前（`MulliganPanel.Create` 里会调它）

            panel = MulliganPanel.Create(root.transform);
            Check(panel != null, "（前提）换牌面板建得出来（`OverHand` 子画布立起来了）");
            if (panel == null) { Finish(); return; }

            Group("A·眼睛收整组", () => A_EyeHidesWholeContent(root.transform));
            Group("B·标记是原地", () => B_MarkInPlace(root.transform));
            Group("C·倒计时文案", () => C_CountdownText(root.transform));
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
        Debug.Log("B117G1017 ==== 合计：✓ " + _pass + " · ✗ " + _fail + " ====");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  A. 眼睛 = 收/放「换牌面板内容整组」
    //
    //  判据（🥇 `cpp2il/out_cpp2il_dev_ilspy/MulliganManager.cs`，`ShowMulliganElements(bool showY)` 全文四句）：
    //    ① `mulliganTextObj.SetActive(showY)`   —— 提示行（`MulliganText`，含其中的 `TurnText`）
    //    ② `mulliganButton.SetActive(showY)`    —— 「完成换牌」那颗钮（底条 + 圆钮 + 字）
    //    ③ `ShowMulliganCards(showY)`           —— 逐张 `currentMulligan[i].gameObject.SetActive` **+
    //         `mulliganFrameButtons` 循环**（卡下那颗钮 / 钮上的字 / 那枚红标记一体）
    //    ④ `manager.shade.SwitchShade(showY, useAnim: false)` —— 压暗层（**硬切**）
    //  · 开关判据 = `ToggleMulliganVisibility()`（同一文件 `:183`）读 `mulliganTextObj.activeInHierarchy` **取反**；
    //  · **不动** `buttonsGroup`（`+0x40`）—— 眼睛自己就在它下面（dump：`ButtonsGroup/HideMulliganButton`）。
    //
    //  🧨 改坏法：退回「只收卡钮 + 钮上的字 + 压暗」⇒ A2 / A3 / A5 / A6 四条红；
    //     把眼睛也 `SetActive(false)` ⇒ A7 红（玩家再也点不回来）；
    //     只切 `activeSelf` 不闸命中 ⇒ A8 / A9 红（收起来之后点同一枚坐标照样生效）。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void A_EyeHidesWholeContent(Transform root)
    {
        var panel = MulliganPanel.Create(root);
        var cards = new List<CardView>();
        try
        {
            Check(panel != null, "（前提）A 组自己的面板建出来了");
            if (panel == null) return;
            for (int i = 0; i < 3; i++) cards.Add(CardView.Create(root, UnitData("B117G1017 A" + i), "g1017_a" + i));

            panel.Open(cards);
            Check(panel.Visible, "（前提）面板开着");
            Check(panel.CardButtonCount == 3, "（前提）三张起手牌各建了一颗「换」钮（实得 " + panel.CardButtonCount + "）");
            // 前提（硬检查）：这四组得真建出来 —— 否则下面的「灭了」分不清是「本来就没有」还是「收对了」
            Check(panel.PromptRowActiveForTest, "（前提）提示行建出来了（`MulliganText` + `TurnText`）");
            Check(panel.DoneActiveForTest, "（前提）完成钮三件（底条 / 圆钮 / 字）都在");
            Check(panel.ShadeActive, "（前提）压暗层建出来了");
            Check(panel.EyeActiveForTest, "（前提）眼睛那颗钮建出来了");
            Check(panel.CardActiveForTest(0), "（前提）换牌卡本体在画");

            panel.ToggleMark(1);
            Check(panel.IsMarked(1) && panel.MarkerActiveForTest(1),
                  "（前提）第 2 张标上了、红标记画出来了（后面要验它跟着眼睛一起走）");

            // ---- 第一下：收 ----
            bool okEye = panel.HandleClick(panel.EyeWorldPos);
            Check(okEye && panel.ContentHiddenForTest, "★ A1 点眼睛第一下 = **收**（原版读 `activeInHierarchy` 取反）");
            Check(!panel.PromptRowActiveForTest,
                  "★ A2 收起来 ⇒ **提示行** inactive（原版 `mulliganTextObj.SetActive(false)`，`ShowMulliganElements` ①）"
                + "｜🧨 改前只收卡钮 ⇒ 提示行还挂在屏幕上 ⇒ 本行红");
            Check(!panel.DoneActiveForTest,
                  "★ A3 收起来 ⇒ **完成钮**三件 inactive（原版 `mulliganButton.SetActive(false)`，②）"
                + "｜🧨 改前只收卡钮 ⇒ 完成钮还看得见 ⇒ 本行红");
            Check(!panel.CardButtonsShown && !panel.ShadeActive,
                  "★ A4 收起来 ⇒ 卡钮与**压暗层**一起收（原版 ③④；压暗是 `SwitchShade(false, useAnim:false)` 硬切）");
            Check(!panel.CardActiveForTest(0),
                  "★ A5 收起来 ⇒ **换牌卡本体**也 inactive（原版 `ShowMulliganCards(false)` 收的是 `cardScript.gameObject`）"
                + "｜🧨 只收钮、不收卡 ⇒ 眼睛按下去战场还是被三张卡挡着 ⇒ 本行红");
            Check(!panel.MarkerActiveForTest(1),
                  "★ A6 收起来 ⇒ **红标记**也一并藏（原版它是 `MulliganFrame` 的子件）");
            Check(panel.EyeActiveForTest,
                  "★ A7（**灭自证**）收起来 ⇒ **眼睛自己仍然 active**（原版 `buttonsGroup` 不在收的名单里）"
                + "｜🧨 连眼睛一起收 ⇒ 这一下点不回来、整局卡死 ⇒ 本行红");
            Check(panel.Visible,
                  "★ A8 收的是**内容**、不是窗：`Visible` 仍为真（与 `SetVisible(false)` 是两个状态）");

            // ---- 灭自证②：收起来之后，那几条**用框算出来的**命中口必须失效 ----
            int doneFired = 0;
            panel.OnDone = m => { doneFired++; };
            panel.HandleClick(panel.DoneWorldPos);
            Check(doneFired == 0,
                  "★ A9（**灭自证**）收起来之后点**完成钮那一枚坐标** ⇒ **不提交**（原版那颗 `Button` 的 GO 已 inactive）"
                + "｜🧨 只切 `activeSelf`、不闸命中 ⇒ `HitDone` 走的是常量框、照样判中 ⇒ 本行红");
            panel.HandleClick(panel.CardBtnWorldPos(0));
            Check(!panel.IsMarked(0),
                  "★ A10（**灭自证**）收起来之后点**第 1 张卡钮那一枚坐标** ⇒ **不标记**"
                + "（`HudPiece.Contains` 是矩形测试、**不看 `activeSelf`** ⇒ 必须由 `HandleClick` 显式闸）"
                + "｜🧨 不闸 ⇒ 玩家看不见钮却照样能点 ⇒ 本行红");

            // ---- 第二下：放 ----
            Check(panel.HandleClick(panel.EyeWorldPos) && !panel.ContentHiddenForTest, "★ A11 点眼睛第二下 = **放**");
            Check(panel.PromptRowActiveForTest && panel.DoneActiveForTest && panel.CardActiveForTest(0)
                  && panel.CardButtonsShown && panel.ShadeActive,
                  "★ A12 放回来 ⇒ 提示行 / 完成钮 / 卡体 / 卡钮 / 压暗 **五组同时复活**");
            Check(panel.MarkerActiveForTest(1),
                  "★ A13 放回来 ⇒ **红标记按 `_marked` 复原**（第 2 张仍是标记态）");
            Check(!panel.MarkerActiveForTest(0),
                  "★ A14（反向·灭自证）放回来**不许**把没标记的那张也画上红条（`SetActive(true)` 一把梭会在本行红）");

            // ---- 提交仍然通（放回来之后走同一条路）----
            int fired = -1; int nfired = 0;
            panel.OnDone = m => { nfired++; fired = (m != null && m.Count > 0) ? m[0] : -1; };
            panel.HandleClick(panel.DoneWorldPos);
            Check(nfired == 1 && fired == 1,
                  "★ A15 放回来之后点完成 ⇒ 回调一次、带上被标记的下标（实得 " + nfired + " 次 / 首项 " + fired + "）");

            // ---- Close 必须先放回内容（那几张是**手牌本体**，不是面板自带件）----
            panel.HandleClick(panel.EyeWorldPos);
            Check(panel.ContentHiddenForTest, "（前提）再收一次（下面验 `Close()` 的还原义务）");
            panel.Close();
            Check(!panel.Visible, "（前提）`Close()` 之后面板关着");
            // 🔴 **2026-11-02（`B117-FIXD`）就地订正**：这一格必须量**调用方手里那几张卡对象**（`cards[0]`），
            //   不能量 `panel.CardActiveForTest(0)` —— `Close()` 里 `ClearCardButtons()` 会 `_cards.Clear()`，
            //   那个读口在关完之后**恒为 false**（与「有没有还原」无关 ⇒ 量了也没判别力）。
            Check(cards[0] != null && cards[0].gameObject.activeSelf,
                  "★ A16 **`Close()` 不许把收起态带出去**：那几张起手牌是**手牌本体**（`_cards` 由驱动层持有）"
                + "，原版 `ProcessMulliganDone` 第一句就是 `ShowMulliganElements(true)`"
                + "｜🧨 不还原 ⇒ 换牌结束后手牌里三张是 `inactive`（看不见、点不到）⇒ 本行红");
        }
        finally
        {
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) Object.DestroyImmediate(cards[i].gameObject);
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  B. 标记 = **原地**（不搬牌）
    //
    //  判据：
    //   · 「整批起手牌搬进换牌区那一行」发生在**开面板那一刻** —— `PlayerHand.AddCardsToMulligan`
    //     → 逐张 `MoveCardToMulliganPos(card, pos, total)`（`PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:119`）；
    //   · 「标记一张」只做三件：`cardState = 0xE`（`ClickMulliganSelected`）、`BattleCardUI.SetAsMulliganSelected(true)`、
    //     钮上的词条换 `Battle/Mulligan/Undo` —— **一个字都不搬**（`CardScript__ClickMulliganSelected.c` 全文）。
    //     ⇒ 界面上「标记」= **原地加一层红条**，不是把牌拖进另一个区。
    //
    //  🧨 改坏法：`ToggleMark` 里顺手改卡的位姿/父件 ⇒ B2 红。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void B_MarkInPlace(Transform root)
    {
        var panel = MulliganPanel.Create(root);
        var cards = new List<CardView>();
        try
        {
            Check(panel != null, "（前提）B 组自己的面板建出来了");
            if (panel == null) return;
            for (int i = 0; i < 3; i++) cards.Add(CardView.Create(root, UnitData("B117G1017 B" + i), "g1017_b" + i));

            panel.Open(cards);
            // ① 三张都在换牌区那一行上（判据只此一处 = `RowWorldPosAt`）
            for (int i = 0; i < cards.Count; i++)
            {
                var want = panel.RowWorldPos(cards[i]);
                var got = cards[i].transform.position;
                float d = Vector3.Distance(want, got);
                Check(d < 0.02f,
                      $"★ B1.{i} 起手牌都在**换牌区那一行**（第 {i + 1} 张与 `RowWorldPos` 差 {d:F4} 世界单位）"
                    + "｜🧨 只贴钮不搬牌（牌留在屏幕底部那排手牌上）⇒ 本行红");
            }

            // ② 标记前后**位置逐位不变**
            var before = new Vector3[cards.Count];
            for (int i = 0; i < cards.Count; i++) before[i] = cards[i].transform.position;

            panel.ToggleMark(1);
            Check(panel.IsMarked(1), "（前提）第 2 张标上了");
            float moved = Vector3.Distance(before[1], cards[1].transform.position);
            Check(moved < 1e-3f,
                  $"★ B2 **标记不搬牌**（第 2 张标记前后位移 {moved:F5} 世界单位；原版 `ClickMulliganSelected` 只翻 cardState）"
                + "｜🧨 标记时顺手挪牌/换父件 ⇒ 本行红");
            Check(panel.IsMarked(1) && !panel.IsMarked(0) && !panel.IsMarked(2),
                  "★ B3 只标记点中那一张（其余两张不动）");

            // ③ 取消标记也不搬
            panel.ToggleMark(1);
            float back = Vector3.Distance(before[1], cards[1].transform.position);
            Check(!panel.IsMarked(1) && back < 1e-3f,
                  $"★ B4 **取消标记**同样不动牌（位移 {back:F5}）");
        }
        finally
        {
            if (panel != null) panel.Close();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) Object.DestroyImmediate(cards[i].gameObject);
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  C. 倒计时文案（面板侧那一半）
    //
    //  判据（同一份 ILSpy）：`MulliganManager.SetMulliganTimer(int secondsLeftCounter)` 全文 =
    //    `mulliganButtonText.text = "0:0" + secondsLeftCounter.ToString();`
    //  调用点 = `BattleManager.<MulliganCountdown>` 的 `if (iVar1 < 10) SetMulliganTimer(iVar1)`
    //    ⇒ **只在前十秒**把剩余秒数写到「完成换牌」那颗钮上（`0:09` … `0:01`），
    //      之后由 `ProcessMulliganDone` 收掉整颗钮。
    //  ⚠️ 驱动侧那一半（25 s / 逐秒 −1 / `<1` 自动完成）已有 `Editor/BattleScene.cs` 的断言兜着，
    //     本组只钉**面板这一侧**的格式与「字能被换回来」。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void C_CountdownText(Transform root)
    {
        var panel = MulliganPanel.Create(root);
        var cards = new List<CardView>();
        try
        {
            Check(panel != null, "（前提）C 组自己的面板建出来了");
            if (panel == null) return;
            cards.Add(CardView.Create(root, UnitData("B117G1017 C0"), "g1017_c0"));
            panel.Open(cards);

            string label = MulliganPanel.DoneLabel;
            Check(!string.IsNullOrEmpty(label), "（前提）默认按钮文案取得到（`Battle/Mulligan/ButtonDone`）");
            Check(panel.DoneText == label, $"（前提）开面板时钮上写的是默认文案（实得「{panel.DoneText}」）");

            panel.SetDoneText("0:0" + 9);
            Check(panel.DoneText == "0:09",
                  $"★ C1 `SetMulliganTimer` 的格式 = **`\"0:0\" + 秒数`**（`SetDoneText(\"0:09\")` 写进去读回「{panel.DoneText}」）");
            panel.SetDoneText("0:0" + 1);
            Check(panel.DoneText == "0:01", $"★ C2 一秒档同样是两位（实得「{panel.DoneText}」）");
            panel.SetDoneText(label);
            Check(panel.DoneText == label, "★ C3 收尾能把默认文案写回来（倒计时结束后不再挂着秒数）");
        }
        finally
        {
            if (panel != null) panel.Close();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) Object.DestroyImmediate(cards[i].gameObject);
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
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
