// B141_W8b_Assert.cs — 执行写手 `B141-W8b`（2026-11-04）· 第一百二十八会话
//
// ============================ 它盯的是什么 ============================
//  A · `B138` §4 `4-05` 的**同族第二处**（`Battle/SettingsPanel.cs`）：战斗内设置面板的语言下拉
//      **按内容收缩**（原版 `TMP_Dropdown.Show():926-932`：`sizeDelta.y = itemSize.y * m_Items.Count + offsetMin.y − offsetMax.y`）。
//      公式：`n × 40.8707 + 0.8519` ⇒ 12 项 = **491.30** · 13 项 = **532.171**（现读 `Loc.Languages.Length = 13`）。
//      `Template` **原型**仍 = 序列化满高 **573.96**（原版 prefab 就是那么高，⛔ 不该跟着缩）。
//  B · `B138` §4 `4-10` 的**其余两处**（`BattleDriver.cs`）：END TURN 那颗 `TurnText` 与两颗 `ManaText`
//      的 TMP `m_VerticalAlignment` = **4096 (Midline)**（原版 prefab 逐颗现读）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//  · `d:/2/.../com.unity.ugui@.../Runtime/TMP/TMP_Dropdown.cs:926-932`（未改动的官方 ugui `Show()`）
//  · `判据/B141_W129_对账_UI动画.md` §4 `4-05` / `4-10` · `报告/B138_审计汇总_问题清单.md` §4
//  · 原版 prefab 现读（`bundle_scenes_scenes_battlearena1/MonoBehaviour/`）：
//      `MonoBehaviour_3971.json`（`go 77` · `m_text = "END TURN"`）→ `m_VerticalAlignment = **4096**`
//      `MonoBehaviour_4919.json`（`go 100` · 敌 `ManaText`）· `MonoBehaviour_5242.json`（`go 382` · 我 `ManaText`）→ 都是 **4096**
//    ⚠️ **同目录另有一颗同名 `TurnText_293`（`m_text = "You go second"`）的 V = 512 (Middle)** —— ⛔ 不是本件要的那颗。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//  · A4/A7 ← 把 `BuildLangListSubtree` 的 `bot` 改回恒 `top − LstHPx`（= 满高）⇒ 高变 573.96 ⇒ 红
//  · A2    ← 把 `Template` 原型也一起缩（`withRows` 传 true）⇒ 原型变 532.17 ⇒ 红
//  · A5/A6 ← 只改高、不改 `vpCy`/底边 ⇒ 顶/底至少一头漂 ⇒ 红
//  · B4    ← 删掉 `SetVAlign` 那一句 ⇒ 回到 `Label` 出厂档 `Middle` ⇒ 红
//  · B1/B2 ← 只给我方那颗接（漏敌方）⇒ B2 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//  · 批处理没有帧循环 —— 本件只读**建好那一刻**的矩形/档位，不验真机观感（那归 `资料/真Play待验清单.md`）。
//  · `A` 组会真的 `ShowLangList()` 再 `HideLangList()`（收尾在 `finally` 里复原，不给后面的断言留现场）。
using System;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B141-W8b`：语言下拉按内容收缩（战斗内设置面板）+ 三颗标签的 `Midline` 垂直档。</summary>
public static class B141_W8b_Assert
{
    const string Prefix = "[B141W8b] ";
    static int _ok, _bad;

    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>宿主入口（要一台已经 `Begin()` 过的 driver —— 接在 `BattleScene` 卸 driver **之前**那条聚合行）。</summary>
    public static void Run(BattleDriver d)
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B141-W8b：语言下拉按内容收缩 + END TURN/ManaText 的 Midline 开始 ===");
        var sp = d != null ? d.Settings : null;
        bool opened = false;
        try
        {
            A_LangList(sp, ref opened);
            B_VAlign(d);
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally
        {
            if (opened && sp != null) { try { sp.HideLangList(); } catch { } }   // 别把「列表开着」留给后面的断言
        }
        Debug.Log(Prefix + "B141_W8b 合计：✓ " + _ok + " / ✗ " + _bad + "（共 " + (_ok + _bad) + " 条）");
    }

    // ==================================================================
    //  A · 语言下拉面板高 = 内容高（4-05 同族第二处）
    // ==================================================================
    static void A_LangList(SettingsPanel sp, ref bool opened)
    {
        Check(sp != null, "A0（前提）战斗内设置面板建起来了（`driver.Settings`）");
        if (sp == null) return;

        // ---- A1 公式两档（原版 `Show():926-932`；两档都断 ⇒ 写死单值必有一条红）----
        float h12 = SettingsPanel.DropdownContentHForTest(12);
        float h13 = SettingsPanel.DropdownContentHForTest(13);
        Check(Mathf.Abs(h12 - 491.30f) < 0.02f,
              "A1 ★ 12 项的内容高 = **491.30**（实得 " + h12.ToString("F4") + "）"
            + "｜🧨 把 `LstItemGap` 0.8519 删掉 / 改行高 ⇒ 红");
        Check(Mathf.Abs(h13 - 532.171f) < 0.02f,
              "A1b ★ 13 项的内容高 = **532.171**（实得 " + h13.ToString("F4") + "）"
            + "｜🧨 同上；另：两档任一条红都说明算式不是原版那条");

        // ---- A2 `Template` 原型仍是序列化满高 ----
        float tpl = sp.LangTemplateBgDrawnHeight;
        Check(Mathf.Abs(tpl - 573.96f) < 0.05f,
              "A2 ★ `Template` **原型**底板仍是序列化满高 **573.96**（实得 " + tpl.ToString("F3") + "）"
            + "｜🧨 给原型也传 `withRows = true` ⇒ 它被缩到 532.17 ⇒ 红（原版 prefab 里原型就是 573.96）");

        // ---- A3/A4/A5/A6 真正显示的那份（克隆体）----
        sp.ShowLangList(); opened = true;
        Check(sp.LangListOpen, "A3（前提）语言下拉真的打开了（`LangListOpen`）");
        Check(sp.LangRowCount == Loc.Languages.Length,
              "A3b（前提）行数 = `Loc.Languages.Length`（" + sp.LangRowCount + " / " + Loc.Languages.Length + "）"
            + " —— ⛔ 两数不等时下面的高度期望值也要跟着换");

        float cx, cy, w, h;
        bool got = sp.LangListBgRectPx(out cx, out cy, out w, out h);
        Check(got, "A4a（前提）拿得到列表底板的矩形（`LangListBgRectPx`）");
        if (!got) return;

        Check(Mathf.Abs(h - h13) < 0.06f,
              "★★ A4 列表底板高 = **内容高 " + h13.ToString("F3") + "**（实得 " + h.ToString("F3") + "）"
            + "｜🧨 把 `bot` 改回恒 `top − LstHPx`（满高 573.96）⇒ 红");
        // 顶 = 框高的一半 29.7 − 22（原版 `ap.y = −22`）= 7.7；底 = 顶 − 面板高。
        const float kTop = 29.7f - 22f;
        float top = cy + h * 0.5f, bot = cy - h * 0.5f;
        Check(Mathf.Abs(top - kTop) < 0.06f,
              "A5 ★ 列表**顶**不变 = **" + kTop.ToString("F2") + "**（框心往下 22；实得 " + top.ToString("F3") + "）"
            + "｜🧨 只改高不改 `vpCy` ⇒ 顶或底至少一头漂 ⇒ 红");
        Check(Mathf.Abs(bot - (kTop - h13)) < 0.06f,
              "A6 ★ 列表**底** = **" + (kTop - h13).ToString("F3") + "**（实得 " + bot.ToString("F3") + "）"
            + "｜🧨 把 `bot` 写死成 `top − 573.96` ⇒ 红");
        Check(h < 573.96f && Mathf.Abs(h - 573.96f) > 1f,
              "A7 ★ 灭自证：底板高 **严格小于** 满高 573.96（实得 " + h.ToString("F3") + "）"
            + "｜🧨 改回「恒满高」⇒ h = 573.96 ⇒ 红（与 A4 不同源：这里是严格不等式）");
    }

    // ==================================================================
    //  B · 三颗标签的垂直档 = Midline（4-10 其余两处）
    // ==================================================================
    static void B_VAlign(BattleDriver d)
    {
        Check(d != null, "B0（前提）拿到活 driver");
        if (d == null) return;
        var me = d.EnergyLabelForTest;
        var foe = d.FoeEnergyLabelForTest;
        var et = d.EndTurnLabelForTest;
        Check(me != null && foe != null && et != null,
              "B0b（前提）三颗标签都建出来了（我 `" + (me != null) + "` · 敌 `" + (foe != null) + "` · END TURN `" + (et != null) + "`）"
            + " —— ⛔ 前提不成立=红（别用 `if (x != null) Check(…)` 静默跳过）");
        if (me == null || foe == null || et == null) return;

        Check(me.VAlignTier == Label.VAlign.Midline,
              "★ B1 我方 `ManaText` = **Midline**（原版 `MonoBehaviour_5242.json`：`go 382` · `m_VerticalAlignment = 4096`）"
            + "；实得 " + me.VAlignTier);
        Check(foe.VAlignTier == Label.VAlign.Midline,
              "★ B2 敌方 `ManaText` = **Midline**（原版 `MonoBehaviour_4919.json`：`go 100` · 4096）"
            + "；实得 " + foe.VAlignTier
            + "｜🧨 只给我方接（漏敌）⇒ 红（原版敌我各一颗）");
        Check(et.VAlignTier == Label.VAlign.Midline,
              "★ B3 END TURN `TurnBtn/TurnText` = **Midline**（原版 `MonoBehaviour_3971.json`：`go 77` · `m_text = \"END TURN\"` · 4096）"
            + "；实得 " + et.VAlignTier
            + "｜🧨 照抄同目录那颗 `TurnText_293`（`\"You go second\"` · 512/Middle）⇒ 红");
        Check(me.VAlignTier != Label.VAlign.Middle && et.VAlignTier != Label.VAlign.Middle,
              "★ B4 灭自证：两颗都**不是** `Label` 的出厂档 `Middle`（删掉 `SetVAlign` 那一句 ⇒ 回到 Middle ⇒ 红）");
    }
}
