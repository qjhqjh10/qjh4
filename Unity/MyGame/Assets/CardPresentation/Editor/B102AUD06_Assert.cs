// B102AUD06_Assert.cs — 执行代理 `B102_AUD06`（第一百零二会话 · `AUD02` 普查**最后一项**）：
//                      **聊天预览两行文案缺 `m_overflowMode = Ellipsis`**
//
// ============================ 它盯的是什么 ============================
//  · 原版（真包 `assets_full/bundle_menus_assets_all` 的 prefab 字段实读，UnityPy 直读）：
//      `ChatPreview ▸ Container ▸ Message Preview ▸ text`        —— `m_overflowMode = 1`（= Ellipsis）
//      `ChatPreview ▸ Container ▸ Message Preview (1) ▸ text`    —— `m_overflowMode = 1`（同值）
//    并且这两颗同时是 `m_enableWordWrapping = 0`（我们**早已**用 `SetWrapping(false)` 做掉，见 A3）。
//    普查账 → `资料/完全复刻/判据/AUD02_serialized_params.tsv`。
//  · 我们过去**没有这一口**：`Battle/Label.cs` 根本没有 overflow 的写入口 ⇒ 两颗标签停在
//    `TMP_Text.m_overflowMode` 的**出厂值** `TextOverflowModes.Overflow`(0)（`TMP_Text.cs:808`）。
//    修法 = ① `Battle/Label.cs` **类尾**追加 `SetOverflow(TextOverflowModes)` + 只读口 `OverflowNow`
//           （点阵后端出声，同 `SetCharSpacing` / `SetFontStyle`；收尾走 `ForceRelayout`）；
//           ② `Shell/MainMenuRuntime.BuildChatPreview` 那两行建好之后各调一次
//              `SetOverflow(TMPro.TextOverflowModes.Ellipsis)`（⚠️ 写全名是为了**不新增 `using`** —— 该文件被账按
//              `文件:行号` 引用，插行会让下游落点整体漂移）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · **A1 ★** 读的是**生产那两颗标签的真实状态**（`Label.OverflowNow`）—— ⛔ 不读任何我们自己定义的常量。
//  · **A2（灭自证）**：用**同一个工厂** `Label.Create(...)` 建一颗**同款、但不调 `SetOverflow`** 的标签，
//    它的 `OverflowNow` 必须是**出厂档 `Overflow`(0)**。
//    ⇒ A1 那个 `Ellipsis` **只能**来自 `BuildChatPreview` 里那两句 `SetOverflow`：
//       把调用删掉（或改成 `Overflow`）⇒ A1 红、A2 仍绿；
//       反过来若谁把 `TMP_Text` 的出厂档改成 `Ellipsis`（让 A1 无因也绿）⇒ A2 红。
//    —— 两条**结构上不可能同时满足**。
//  · **A3（别把上一笔弄坏）**：两颗仍是 `NoWrap`（`SetWrapping(false)` 那一笔）——
//    证明 `SetOverflow` 的 `ForceRelayout()` 没有把 `textWrappingMode` 带回去（`SetWrapWidth` 会无条件开 `Normal`）。
//  · **A0b（前提）**：两颗必须是 **TMP 后端**（`WrappingMode >= 0`；点阵后端规范值 = `-1`）。
//    ⛔ 没有它的话，字体资产缺失时会「点阵后端如实报 Overflow」⇒ A2 假绿、A1 红得莫名其妙。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 接在 `CardPresentation/Editor/MainMenuScene.cs` 的 `Run()` 里、**主菜单树建好之后**（在聚合行**同一物理行**追加，行数零变化）：
//      B102AUD06_Assert.Run(); pass += B102AUD06_Assert.Pass; fail += B102AUD06_Assert.Fail;
//  ⚠️ **前提（宿主必须满足，不满足 = 红、⛔ 不是跳过）**：主菜单树已经建出来（`MainMenuRuntime.InstanceOrFind() != null`
//     且 `ChatPreview` 节点在）。`ShellScene` 里主菜单树也建着，同样可跑；`DeckScene` / `BattleScene` 不行。
//  ⚠️ 本件**不产生任何持久副作用**：A2 那颗探针节点当场 `DestroyImmediate`。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`assets_full/bundle_menus_assets_all` 的 `ChatPreview ▸ Container ▸ Message Preview{, (1)} ▸ text`
//          的 `m_overflowMode`（= 1 · Ellipsis）与 `m_enableWordWrapping`（= 0）
//  · 账：`资料/完全复刻/账/_parts/B102_AUD06.tsv`
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B102_AUD06`：聊天预览两行文案必须是 `overflowMode = Ellipsis`（原版 `m_overflowMode = 1`）。</summary>
public static class B102AUD06_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD06] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void E<Tv>(Tv got, Tv want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>唯一入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        S("🆕 B102_AUD06：聊天预览两行文案 `overflowMode = Ellipsis`（原版 `m_overflowMode = 1`）");
        A_Production();
        B_FactoryDefault();
    }

    // ==================================================================
    //  A 段：生产那两颗（`MainMenuRuntime.BuildChatPreview` 建出来的 `ChatPreview/Message Preview{, (1)}`）
    // ==================================================================
    static void A_Production()
    {
        var menu = MainMenuRuntime.InstanceOrFind();   // 🔴 编辑模式下 `AddComponent` 不触发 `Awake` ⇒ `Instance` 恒 null；走统一读口
        T(menu != null, "A0（前提）宿主里找得到 `MainMenuRuntime`（`InstanceOrFind() != null`）");
        if (menu == null) return;

        var cp = menu.Find("ChatPreview");
        T(cp != null, "A0（前提）主菜单里建出了 `ChatPreview` 节点");
        if (cp == null) return;

        var r1t = MenuCheck.FindChild(cp, "Message Preview");
        var r2t = MenuCheck.FindChild(cp, "Message Preview (1)");
        var r1 = r1t != null ? r1t.GetComponent<Label>() : null;
        var r2 = r2t != null ? r2t.GetComponent<Label>() : null;
        T(r1 != null, "A0（前提）`ChatPreview/Message Preview` 建出来了、且挂着 `Label`");
        T(r2 != null, "A0（前提）`ChatPreview/Message Preview (1)` 建出来了、且挂着 `Label`");

        // A0b 前提：必须是 TMP 后端（点阵后端 `WrappingMode` 规范值是 −1，overflow 那一档它根本没有）
        T(r1 != null && r1.WrappingMode >= 0, "A0b（前提）第 1 行是 **TMP 后端**（`WrappingMode >= 0`；−1 = 点阵后端、没有 overflow 这回事）");
        T(r2 != null && r2.WrappingMode >= 0, "A0b（前提）第 2 行是 **TMP 后端**（`WrappingMode >= 0`）");

        // ---- A1 ★ 档位（判据 = 原版 prefab 的 `m_overflowMode = 1`）----
        if (r1 != null && r1.WrappingMode >= 0)
            E(r1.OverflowNow, TextOverflowModes.Ellipsis,
              "A1 ★ 第 1 行 `overflowMode` = **Ellipsis**（原版 `Message Preview/text` 的 `m_overflowMode = 1`）");
        else
            T(false, "A1 ★ 第 1 行 `overflowMode` = Ellipsis —— **读不到**（前提 A0/A0b 不成立）");
        if (r2 != null && r2.WrappingMode >= 0)
            E(r2.OverflowNow, TextOverflowModes.Ellipsis,
              "A1 ★ 第 2 行 `overflowMode` = **Ellipsis**（原版 `Message Preview (1)/text` 的 `m_overflowMode = 1`）");
        else
            T(false, "A1 ★ 第 2 行 `overflowMode` = Ellipsis —— **读不到**（前提 A0/A0b 不成立）");

        // ---- A3 别把上一笔（`SetWrapping(false)`）弄坏 ----
        E(r1 != null ? r1.WrappingMode : -999, 0,
          "A3 第 1 行仍是 **NoWrap**（`textWrappingMode = 0`；原版 `m_enableWordWrapping = 0`）—— `SetOverflow` 的 `ForceRelayout()` 不许把它带回去");
        E(r2 != null ? r2.WrappingMode : -999, 0,
          "A3 第 2 行仍是 **NoWrap**（`textWrappingMode = 0`）");
    }

    // ==================================================================
    //  B 段：同款但不调 `SetOverflow` 的标签（灭自证 —— 出厂档必须是 Overflow）
    // ==================================================================
    static void B_FactoryDefault()
    {
        var root = new GameObject("B102AUD06_Probe");
        try
        {
            // 同一个工厂 `Label.Create`（`MainMenuRuntime.Text` 内部走的就是它）—— 只有 `SetOverflow` 那一句不同。
            var probe = Label.Create(root.transform, "Player Name: Message", Vector3.zero, 5,
                                     Color.white, new Vector2(0.5f, 0.5f), "B102AUD06_ProbeLabel");
            T(probe != null, "A2（前提）`Label.Create` 建出了探针标签");
            if (probe == null) return;
            T(probe.WrappingMode >= 0, "A2（前提）探针是 **TMP 后端**（`WrappingMode >= 0`；−1 时下面那条会假绿）");
            //  🧨 谁把出厂档改成 Ellipsis（让 A1 无因也绿）⇒ 本条红。
            E(probe.OverflowNow, TextOverflowModes.Overflow,
              "A2 ★ **灭自证**：同款、但**没调** `SetOverflow` 的标签 = 出厂档 **Overflow**(0)"
            + " ⇒ A1 那个 `Ellipsis` 只能来自 `BuildChatPreview` 里那两句调用（两条结构上不可能同时满足）");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
