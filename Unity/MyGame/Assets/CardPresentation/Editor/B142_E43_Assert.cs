// B142_E43_Assert.cs — 执行写手 `B142-E43`（第一百二十九会话 · 2026-11-04）
//   条目 **`B142-TURNBTN-GRAY`** / **`B142-CHATBTN-GRAY`**：HUD 上两颗钮「不可点 ⇒ 换灰化材质」
//
// ============================ 它盯的是哪几条 ============================
//  原版判据（第一权威 = 反汇编，逐跳实读；场景 = `Battle Arena 1.unity` GO `TurnBtn` / `ChatButton`）：
//   · `TurnBtn`（End Turn）：
//     `ClockManager__StartTimer.c:11` → `SetStartUi`（`out_cpp2il_dev_ilspy/ClockManager.cs:358`：
//     `clockButton.interactable = true`）是**己方回合**那一支（调用点 `BattleManager._NextTurn_d__395__MoveNext.c:426`）；
//     `SetEnemyTurnUI.c:7`（敌回合）· `StopClock.c:35`（点完 / 终局）· `StopClockUi.c:27` · `Start` 的
//     `Replay` 档（ILSpy `:122`）全置 **false** ⇒ 等价物 = 「`Ctx.Active == _me` ∧ `!Ctx.IsOver` ∧ `!_replaySession`」。
//   · `ChatButton`（玩家名牌下那颗）：`SetChatButtonOnCooldown` 协程开场置 false、冷却 4 s
//     （`VoiceLinesController.cs:343 CHAT_INTERACTABLE_COOLDOWN = 4f`）后置 true（ILSpy `:128/143`），
//     `PopUpShowed` 置 false / `PopUpHid` 置 true（`:1160/1171`）⇒ 等价物 = 「冷却结束 ∧ 气泡关着」。
//   · 灰化的**机制那一半**（两颗共用，已在 `Battle/BattleButtonFx.cs` 类尾那一节实现）：
//     `EverguildButton__DoStateTransition.c` 末段 → `bVar4 = (state != Disabled) && (softDisabled == 0)` →
//     `SetToStateActiveOrDisabled` → `SwitchMaterial`（**前置闸 = `+0x17A colorTintGreyOnDisable`，
//     与 `m_Transition` 无关**）→ `DoMaterialRefresh` → `Everguild/UI/Greyscale`。
//     ⚠️ `TurnBtn` 是 `m_Transition = 2`（SpriteSwap）：**精灵**不换（`m_DisabledSprite = null`），
//     但**材质照换** —— 这正是 `B142` 前一轮注释「视觉不动」漏掉的那一半。
//
//  我们这一侧（本轮）：`BattleDriver.TickHudButtonFeedback` 每帧
//     `_fxEndTurn.SetDisabled(!EndTurnInteractable)` / `_fxChat.SetDisabled(!ChatButtonInteractable)`
//  （追加在**同一物理行**上 ⇒ 行数零变化）。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  🧨 A2：给 `Swap` 族加「禁用自己的图」⇒ 红（原版 `m_DisabledSprite = null` ⇒ 精灵保持普通）。
//  🧨 A3：把 `ApplyGray` 从 `Apply()` 里拿掉（禁用只变色不变灰）⇒ 红（且**不是**静默放行）。
//  🧨 A6：删掉 `WantGray` 里的 `ColorTintGreyOnDisable &&` ⇒ 红（`grey = 0` 的钮会被误灰）。
//  🧨 A8：`SetDisabled(false)` 不还材质 ⇒ 红。
//  🧨 B1/B2：删掉任一处 `SetDisabled` 接线（或只改注释不改代码）⇒ 红。
//  🧨 B5：把旧注释「保持普通精灵 / 底图恒为原样」写回去 ⇒ 红（那正是本轮订正掉的错记）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `public static void Run();` —— **零前提**（只在临时 GameObject 上建件，收尾 `DestroyImmediate`）。
//  挂法（由调度台决定）：`Editor/BattleScene.cs` 的方法尾聚合行**同一物理行**追加
//      B142_E43_Assert.Run(); pass += B142_E43_Assert.Pass; fail += B142_E43_Assert.Fail;
//  判绿红：按行首 `[B142E43] ✓` / `[B142E43] ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CardPresentation;

/// <summary>执行写手 `B142_E43`：`TurnBtn` / `ChatButton` 不可点 ⇒ 灰化材质（两处接线 + 机制回归）。</summary>
public static class B142_E43_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    const string Prefix = "[B142E43] ";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Prefix + "✓ " + msg); }
        else { _fail++; Debug.LogError(Prefix + "✗ " + msg); }
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B142_E43_Root");
        try
        {
            A_SwapDisabledSemantics(root.transform);
            B_Wiring();
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
        Debug.Log(Prefix + "合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ============================================================ A · 机制（`TurnBtn` 的 Swap 族禁用语义）
    static void A_SwapDisabledSemantics(Transform parent)
    {
        var p = MakePiece(parent, "TurnBtn-like", Color.white);
        var normal = MakeTex("UI_Button_End_Turn_Normal_wide");
        var hover = MakeTex("UI_Button_End_Turn_Hover_wide");
        var pressed = MakeTex("UI_Button_End_Turn_Pressed_wide");

        var fx = BattleButtonFx.Swap(p, "TurnBtn(e43)", normal, hover, pressed);
        Check(fx != null, "A0 `TurnBtn` 形状的反馈对象建出来了");
        if (fx == null) return;

        Check(fx.TextureNameForTest == normal.name, "A1 可点 ⇒ 普通图（`UI_Button_End_Turn_Normal_wide`）");

        fx.SetDisabled(true);
        Check(fx.TextureNameForTest == normal.name,
              "🧨 A2 不可点 ⇒ **精灵仍是普通那一张**（原版 `m_SpriteState.m_DisabledSprite = null`；⛔ 别给 Swap 加禁用图）");

        Check(fx.GrayedForTest != fx.GrayShaderMissingForTest,
              "🧨 A3 不可点 ⇒「要么真换了灰化材质、要么明确记了没换成」—— 恰好一条为真（⛔ 不许静默不变灰）");
        if (!fx.GrayedForTest)
            Debug.Log(Prefix + "ℹ 本机取不到 `" + WindowButton.GrayShaderName + "` ⇒ 材质那几条按「已明确记录」放行"
                             + "（`GrayShaderMissingForTest = true`；⛔ 这不是通过，是如实记录）");

        fx.SetHover(true);
        Check(fx.TextureNameForTest == normal.name,
              "A4 悬停不盖掉禁用态：不可点时**仍是普通图**（原版 `DoStateTransition(Disabled)` 后不再走 Normal/Highlighted）");

        fx.SetDisabled(false);
        Check(!fx.GrayedForTest && !fx.IsGrayedForTest(p),
              "🧨 A5 恢复可点 ⇒ 灰化材质**还回去**（`ApplyGray(false)` 的 `SetMaterial(_savedMat)`）");
        Check(fx.TextureNameForTest == normal.name, "A5b 恢复后仍是普通图（没有残留换图）");

        // A6：`colorTintGreyOnDisable = 0` 那一族（原版 6 颗）⇒ **不许灰**
        var p2 = MakePiece(parent, "GreyGateOff", Color.white);
        var fx2 = BattleButtonFx.Swap(p2, "grey0(e43)", normal, hover, pressed);
        if (fx2 != null)
        {
            fx2.ColorTintGreyOnDisable = false;
            fx2.SetDisabled(true);
            Check(!fx2.GrayedForTest && !fx2.GrayShaderMissingForTest,
                  "🧨 A6 `colorTintGreyOnDisable = 0` ⇒ 禁用**不灰**（原版 `SwitchMaterial` 第一句就 return；⛔ 别把所有钮都灰）");
            fx2.SetDisabled(false);
        }
        else Check(false, "A6 夹具没建出来");

        // A7：`ColorTint` 族（`m_Colors` 那一半）不受本轮影响
        var p3 = MakePiece(parent, "TintFamily", Color.white);
        var fx3 = BattleButtonFx.Tint(p3, "tint(e43)");
        if (fx3 != null)
        {
            fx3.ApplyInstant();
            fx3.SetDisabled(true);
            Check(Near(fx3.KeyForTest, BattleButtonFx.DisabledOpaque),
                  "A7 `ColorTint` 族禁用色仍是 `DisabledOpaque`（0.7843 / α1）—— 本轮没动它");
            fx3.SetDisabled(false);
        }
        else Check(false, "A7 夹具没建出来");
    }

    // ============================================================ B · 接线（源码守卫）
    static void B_Wiring()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        string src = null;
        try { if (File.Exists(path)) src = File.ReadAllText(path); }
        catch (Exception e) { Check(false, "B0 读不到 `" + path + "`：" + e.Message); return; }
        if (src == null) { Check(false, "B0 读不到 `" + path + "`（文件不存在）"); return; }

        Check(Has(src, "FeedHud(_fxEndTurn, !blocked && HitEndTurn(world), held); if (_fxEndTurn != null) _fxEndTurn.SetDisabled(!EndTurnInteractable);"),
              "🧨 B1 `TurnBtn` 的禁用态接在每帧 tick 上（**同一物理行** ⇒ 行数零变化）");
        Check(Has(src, "FeedHud(_fxChat, !blocked && ChatBtnHit(world), held); if (_fxChat != null) _fxChat.SetDisabled(!ChatButtonInteractable);"),
              "🧨 B2 `ChatButton` 的禁用态接在每帧 tick 上（同上）");

        Check(Has(src, "Ctx != null && Ctx.Active == _me && !Ctx.IsOver && !_replaySession"),
              "B3 `EndTurnInteractable` = 己方回合 ∧ 未终局 ∧ 非回放（原版 `SetStartUi` / `SetEnemyTurnUI` / `Replay` 三档）");
        Check(Has(src, "_chatCooldown <= 0f && !(_chatPopup != null && _chatPopup.Visible)"),
              "B4 `ChatButtonInteractable` = 冷却结束 ∧ 气泡关着（原版 `SetChatButtonOnCooldown` + `PopUpShowed/Hid`）");

        Check(Has(src, "**换灰化材质**") && Has(src, "与 `m_Transition` 无关"),
              "🧨 B5 注释已就地订正：不可点时**换灰化材质**，且灰化闸与 `m_Transition` 无关（铁律 5）");
        Check(!Has(src, "保持普通精灵"),
              "🧨 B5b 旧错记「不可点时保持普通精灵」**已消失**（它只说了精灵、漏了材质那一半）");
        Check(!Has(src, "底图**恒为原样**"),
              "🧨 B5c 旧错记「底图恒为原样」**已消失**");
    }

    static bool Has(string src, string needle)
    {
        return src != null && src.IndexOf(needle, StringComparison.Ordinal) >= 0;
    }

    static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 1e-3f && Mathf.Abs(a.g - b.g) < 1e-3f
            && Mathf.Abs(a.b - b.b) < 1e-3f && Mathf.Abs(a.a - b.a) < 1e-3f;
    }

    // ============================================================ 夹具
    static HudPiece MakePiece(Transform parent, string name, Color baseColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var img = go.AddComponent<Image>();
        img.color = baseColor;
        img.raycastTarget = false;
        return HudPiece.Of(rt, img);
    }

    static Texture2D MakeTex(string name)
    {
        var t = new Texture2D(4, 4);
        t.name = name;
        t.Apply();
        return t;
    }
}
