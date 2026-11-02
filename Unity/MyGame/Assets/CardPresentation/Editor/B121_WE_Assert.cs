// B121_WE_Assert.cs — 执行写手 `B121-WE`（2026-11-03）落在 `Battle/BattleDriver.cs` 上的断言
//   · A 段：**注释**（用户 2026-11-03「做好战斗部分代码注释」）—— 功能句是否真的落在主链入口上、
//           段标题是否真的改成「功能名在前」（判据 → `资料/完全复刻/报告/B121_CMT_战斗代码注释与卡牌索引.md` §三 / §五）
//   · B 段：**驱动侧三条**（`活状态表` B120-B02 / B03 / B11）—— 组件侧由 `B120-W3` 改，这里钉**驱动那一半**；
//           三条在 2026-11-03 现读时**已经落地**（`B120-W2b`），本段的作用是**防回退**
//   · C 段：**死代码 `C08`** —— `_attackTweens` 与 `ApplyReplaySkipToLiveAttackTweens` 必须已经删除
//
// ============================ 宿主 / 接线 ============================
//   全部是**源级**断言（只读 `BattleDriver.cs` 的文本），不需要 driver、不需要 `LayoutSpace` / `HudCanvas`
//   ⇒ 接到哪个宿主都行。⚠️ **接线由调度台统一做**（⛔ 本文件不改任何 `Editor/*Scene.cs`）。
//   建议接法（与同族一致）：`B121_WE_Assert.Run(); pass += B121_WE_Assert.Pass; fail += B121_WE_Assert.Fail;`
//
// ============================ 判据（一手）============================
//   A · 用户要求 + 普查结论：`B121_CMT_战斗代码注释与卡牌索引.md` §三 #9–#19（P0/P1 清单）· §五（插入位置规则 A/B）
//   B02 · 原版 `BattleManager__CanUseActiveAbility.c`（`param_2 +0x40` 与 `param_3` 比对、再看 `+0x244/+0x246` 的可用位）
//         ⇒ 我们的等价物 = `RuleCore.CanStartActiveAbility`（`RuleCore.cs:15488`）
//   B03 · 原版 `AiScripted__GetTipPosition.c` 末段三条 relation 偏移（`LeftOf / RightOf / Above`）
//         ⇒ 高亮与提示文案过**同一个** `GetTipPosition(..., positionRelation, …)`
//   B11 · 原版 `BattleSettingsWindow__SkipTutorialButtonOnClick.c` 全文两句（`CloseWindow` + `ClickSkip`），
//         ⛔ 不查 `minTimeBeforeSkip`（那是 `TutorialTipScript` 的字段）
//   C08 · 删的是**零调用点**：`SetReplayPaused` 的唯一调用已停用（`BattleDriver.cs` 那行注释），
//         `NoteAttackTween` 三处调用点仍在（源级断言见 `Editor/B117_WBD_Assert.cs` 第 ⑤ 组）
using System;
using System.IO;
using UnityEngine;

public static class B121_WE_Assert
{
    const string P = "[B121_WE] ";

    static int _ok, _bad;
    /// <summary>宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception ex) { Debug.LogWarning(P + "读源码失败 " + rel + "：" + ex.Message); return null; }
    }

    /// <summary>`sig` 那一行的**下一行**（跳过紧邻的空行 / 参数续行后的第一行）必须以 `{` 开头且带 `【功能】`。
    /// ⚠️ 找不到签名 ⇒ 返回 null（调用点据此报红，⛔ 不静默跳过）。</summary>
    static bool BraceLineHasFunc(string src, string sig, out string line)
    {
        line = null;
        int i = src.IndexOf(sig, StringComparison.Ordinal);
        if (i < 0) return false;
        int j = src.IndexOf('\n', i);
        if (j < 0) return false;
        // 签名可能是多行（`Begin` 那种）：从签名行往下找第一行 strip 后是 `{`
        int from = j + 1;
        for (int k = 0; k < 40; k++)
        {
            int e = src.IndexOf('\n', from);
            if (e < 0) return false;
            string t = src.Substring(from, e - from).Trim();
            if (t.StartsWith("{"))
            {
                line = src.Substring(from, e - from);
                return line.Contains("【功能】");
            }
            from = e + 1;
            if (t.Length == 0) continue;
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            if (t.EndsWith(",")) continue;                 // 参数续行
            return false;                                  // 遇到别的语句 ⇒ 形状不对
        }
        return false;
    }

    /// <summary>按「整行内容」找段标题行，返回那一行（找不到返回 null）。</summary>
    static string FindLineStartingWith(string src, string prefix)
    {
        foreach (var raw in src.Split('\n'))
        {
            string t = raw.TrimEnd('\r');
            if (t.TrimStart().StartsWith(prefix)) return t;
        }
        return null;
    }

    public static void Run()
    {
        Debug.Log(P + "--- B121-WE：BattleDriver 注释 / 驱动侧三条 / 死代码 C08 ---");
        string src = ReadSrc("CardPresentation/Battle/BattleDriver.cs");
        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— B121-WE 整段没跑"); return; }

        // ==================== A · 注释 ====================
        Debug.Log(P + "--- A 段：功能级注释（P0 / P1 / 段标题功能名前置）---");

        string l;
        Check(BraceLineHasFunc(src, "void SetFactionResourceVisible(bool myFaith, bool foeFaith, bool myStone, bool foeStone)", out l)
           && l.Contains("信仰") && l.Contains("灵魂石") && l.Contains("ManaTypeHolder__Toggle.c:17"),
              "A1（P0）`SetFactionResourceVisible` 的 `{` 行有功能句，且写明**两组**（信仰 / 灵魂石）+ 机器码级判据"
            + "｜🧨 退回「11 行全裸」⇒ 本条红");

        Check(BraceLineHasFunc(src, "void SyncBoard(int owner, Dictionary<int, CardView> views, bool mine)", out l)
           && l.Contains("引擎棋盘"), "A2a `SyncBoard` = 引擎棋盘 → 场上卡视图同步（349 行原本 0 段标）");
        Check(BraceLineHasFunc(src, "void BuildHudExtras(Transform root)", out l)
           && l.Contains("非主路径"), "A2b `BuildHudExtras` = HUD 非主路径那批件（603 行原本无方法头）");
        Check(BraceLineHasFunc(src, "void UpdateHud()", out l) && l.Contains("引擎真值"),
              "A2c `UpdateHud` = 每帧把 HUD 刷成引擎真值");
        Check(BraceLineHasFunc(src, "void DrivePlayerTurn()", out l) && l.Contains("玩家回合主循环"),
              "A2d `DrivePlayerTurn` = 玩家回合主循环");
        Check(BraceLineHasFunc(src, "void DriveAiTurn()", out l) && l.Contains("AI 回合驱动"),
              "A2e `DriveAiTurn` = AI 回合驱动");
        Check(BraceLineHasFunc(src, "void PlaySignals()", out l) && l.Contains("演出时间线"),
              "A2f `PlaySignals` = 引擎事件流 → 演出时间线");
        Check(BraceLineHasFunc(src, "void ShowAsk()", out l) && l.Contains("选牌"),
              "A2g `ShowAsk` = 效果要玩家选牌 / 选选项时弹面板");
        Check(BraceLineHasFunc(src, "void BuildHud()", out l) && l.Contains("战斗 HUD 主链"),
              "A2h `BuildHud` = 战斗 HUD 主链（该段标题原只有「HUD」两个字）");
        Check(src.IndexOf("/// 开局。 【功能】**一局的开局主链**", StringComparison.Ordinal) >= 0,
              "A2i `Begin` 的 XML doc 首行补了功能句（零行数：就地改写 `/// 开局。`）");

        // 段标题「功能名前置」：抽查主链各域各一条 —— 必须**以功能名开头**（⛔ 不以 🆕 / 🔴 开头）
        var heads = new[]
        {
            "//  HUD · Canvas 级三件",
            "//  出牌 · 出牌飞行的【中段交接】",
            "//  攻击 · 反击演出",
            "//  教程 · 教程表现层",
            "//  联机 · 「等对手最后几条动作」那个窗口",
            "//  回放 · 回放局的出口",
            "//  技能面板 · uGUI 事件 → 驱动",
            "//  演出 · 阻塞闸",
            "//  战斗 HUD 主链：场上卡 / 手牌 / 牌库 / 坟场 / 能量 / 计时 / 按钮",
        };
        int miss = 0; string missList = "";
        foreach (var h in heads)
            if (FindLineStartingWith(src, h) == null) { miss++; missList += "\n    · 找不到：" + h; }
        Check(miss == 0, $"A3 段标题改成「功能名在前」（抽查 {heads.Length} 条主链段，缺 {miss} 条）" + missList);

        // 段标题「功能名前置」的量化：以「功能域 + ` · `」开头的段标题条数（改前 = **0**，改后 = 55）
        var prefixes = new[]
        {
            "HUD ·", "出牌 ·", "教程 ·", "联机 ·", "回放 ·", "攻击 ·", "技能 ·", "技能面板 ·",
            "演出 ·", "表现 ·", "结算 ·", "对局 ·", "聊天 ·", "语音 ·", "特效 ·", "相机 ·",
            "战斗日志 ·", "场景过渡 ·", "回合时钟 ·", "选牌面板 ·", "手牌 ·", "战斗 HUD 主链",
        };
        var all = src.Split('\n');
        int domainFirst = 0;
        for (int i = 1; i < all.Length; i++)
        {
            if (!all[i - 1].TrimStart().StartsWith("// ====")) continue;
            string body = all[i].TrimStart();
            if (!body.StartsWith("//  ")) continue;
            body = body.Substring(4);
            foreach (var pre in prefixes)
                if (body.StartsWith(pre, StringComparison.Ordinal)) { domainFirst++; break; }
        }
        Check(domainFirst >= 50,
              $"A4 段标题改成「功能名在前」：现读 **{domainFirst}** 条以「功能域 · 」开头"
            + "（改前 0 条；🆕 日期开头的从 132 降到 81）｜🧨 把新段标题写回「日期在前」⇒ 本数会掉");

        // ==================== B · 驱动侧三条 ====================
        Debug.Log(P + "--- B 段：驱动侧三条（B120-B02 / B03 / B11）---");

        Check(src.IndexOf("canUse: _canUse", StringComparison.Ordinal) >= 0
           && src.IndexOf("RuleCore.CanStartActiveAbility(Ctx, _me, _uSlot)", StringComparison.Ordinal) >= 0,
              "B02 技能面板可用层：驱动按**原版 `CanUseActiveAbility`**（= `RuleCore.CanStartActiveAbility`）判「可用」再传给面板"
            + "｜🧨 退回「`ShownTargets > 0` 即铺」⇒ 本条红");

        Check(src.IndexOf("ov.SetHighlight(true, c, sz, (TutRelation)rel, dx);", StringComparison.Ordinal) >= 0
           && src.IndexOf("int rel = tp != null ? tp.positionRelation : 0; float dx = TutorialTipOffset(pref);",
                          StringComparison.Ordinal) >= 0,
              "B03 教程高亮带 `positionRelation` 与偏移：驱动把 `rel` / `dx` 传给 `SetHighlight`，且与 `SetTip` **同源**"
            + "（⛔ 别另算一套）｜🧨 退回 `SetHighlight(true, c, sz)` 三参 ⇒ 本条红");

        Check(src.IndexOf("if (ov != null) ov.TrySkipFromSettings();", StringComparison.Ordinal) >= 0,
              "B11 设置窗「Skip tutorial」走**没有时间门**的那条入口（`TrySkipFromSettings`）");
        Check(src.IndexOf("if (ov != null) ov.TrySkip();", StringComparison.Ordinal) < 0,
              "B11 **灭自证**：设置窗那条链**不再**调 `TrySkip()`（= 不再被 `minTimeBeforeSkip = 1 s` 挡住）");

        // ==================== C · 死代码 C08 ====================
        Debug.Log(P + "--- C 段：死代码 C08（`_attackTweens` / `ApplyReplaySkipToLiveAttackTweens`）---");

        Check(src.IndexOf("void ApplyReplaySkipToLiveAttackTweens", StringComparison.Ordinal) < 0,
              "C1 `ApplyReplaySkipToLiveAttackTweens` **已删**（零调用点；⛔ 别加回来）");
        Check(src.IndexOf("readonly List<Tween> _attackTweens", StringComparison.Ordinal) < 0,
              "C2 字段 `_attackTweens` **已删**");
        Check(src.IndexOf("_attackTweens.Add(", StringComparison.Ordinal) < 0
           && src.IndexOf("_attackTweens.RemoveAll(", StringComparison.Ordinal) < 0,
              "C3 登记表的读写（`Add` / `RemoveAll`）一并删净");
        Check(src.IndexOf("Tween NoteAttackTween(Tween t)", StringComparison.Ordinal) >= 0,
              "C4 `NoteAttackTween` **保留**成纯转发（三处调用点 + `B117_WBD_Assert` 的源级断言还指着它 —— ⛔ 别删它）");
        Check(src.IndexOf("方法**已删**：2026-11-03 `B121-WE` 死代码 `C08`", StringComparison.Ordinal) >= 0,
              "C5 `SetReplayPaused` 旁边那句注释**就地订正**过（原来写「方法本身保留」，现已删 ⇒ 铁律 5）");
    }
}
