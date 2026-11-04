// B140_WP_Assert.cs — 执行写手 `B140_WP` 的断言（`B139-D3` 可选收紧：伤害飘字去掉 `Mathf.Abs`）
//
// ============================ 它盯的是什么 ============================
//   `B139-D3`：原版 `CardDamageCounterController.DamageReceived` 把 `System.Int32.ToString(amount)`
//   的结果**直接**塞进 TMP —— **没有任何符号处理**（负数就印 `-N`），治疗档才是 `"+"+量`。
//   我们原来伤害档是 `Mathf.Abs(amount).ToString()` ⇒ 正数域等价、**负数域印成 `N`**（原版印 `-N`）。
//   本包把那一个 `Abs` 去掉（只动伤害分支），这里钉住三件事：
//     A `-3` ⇒ 「-3」 · B `5` ⇒ 「5」 · C 治疗档仍是「+5」 · D 源码里伤害分支**没有 `Abs`**。
//
// ============================ 怎么用（无宿主，直接跑）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WP_Assert.Run -logFile -
//   判绿红：按**行首** `B140WP ✓` / `B140WP ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//   ⚠️ 本件不建持久资产：临时根节点在 `finally` 里 `DestroyImmediate`，`CardTween.Mode` 原样还原
//      （否则会把后面所有宿主的补间一起冻住）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `decomp_full/CardDamageCounterController__DamageReceived.c:10-11`
//     —— `uVar2 = System_Int32__ToString(local_res10, 0);` 随后直接当 `SetText` 的实参。
//   · 治疗档 `"+"+量` → `decomp_full/CardScript._Heal_d__468__MoveNext.c:155-204`（同 `PopNumber.Pop` 的注释）。
//   · 完整判据表 → `资料/完全复刻/判据/B140_参数复核_G1P1_战斗引擎.md` §三·3 / `B139-E` 的 `1-01` 账行。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A  ← 把 `Mathf.Abs(amount)` 加回伤害分支 ⇒ `-3` 变「3」⇒ 红（B/C 仍绿 ⇒ 「两档一起改」过不了）。
//   · B  ← 给伤害档加任何前缀（`+` / `-`）⇒ 红。
//   · C  ← 把治疗的 `"+"` 一起删掉、或把治疗也改成 `amount.ToString()`（负治疗印 `-5`）⇒ C/C2 红。
//   · D3 ← 源码层结构性判据：只要伤害分支里出现 `Abs` 就红 —— 挡「运行时再 Abs 一次」那类漏网。
using System;
using System.IO;
using DG.Tweening;
using UnityEngine;
using CardPresentation;

public static class B140_WP_Assert
{
    const string P = "B140WP ";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + what); }
        else { _fail++; Debug.LogError(P + "✗ " + what); }
    }

    /// <summary>被测源码（相对 `Assets/`）。</summary>
    const string SourceRel = "CardPresentation/Battle/PopNumber.cs";

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(P + "=== B140-WP 断言开始（B139-D3：伤害飘字去掉 Mathf.Abs）===");

        // P0 = 前提硬检查（⛔ 不静默跳过 —— 字体不可用时飘字根本不建，后面几条全等于没跑）
        Check(TmpFont.Available, "P0 **前提**：TMP 字体资产可用（`TmpFont.Available`）");
        if (!TmpFont.Available)
        {
            Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败（P0 不成立，提前结束）===");
            return;
        }

        var mode0 = CardTween.Mode;
        var root = new GameObject("B140WP_ROOT");
        CardTween.Mode = UpdateType.Manual;
        PopNumber.ResetLive();
        try
        {
            A_Negative(root);
            B_Positive(root);
            C_Heal(root);
        }
        finally
        {
            PopNumber.ResetLive();
            CardTween.Mode = mode0;
            UnityEngine.Object.DestroyImmediate(root);
        }

        D_Source();

        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ---------------------------------------------------------------- A
    static void A_Negative(GameObject root)
    {
        var d = PopNumber.PopDamage(root.transform, Vector3.zero, -3);
        Check(d != null, "A0 **前提**：`amount = -3` 的伤害飘字建得出来");
        if (d == null) return;

        Check(d.Text == "-3",
              "A ★ 伤害档 `-3` ⇒ 文本「-3」"
            + "｜判据 `CardDamageCounterController__DamageReceived.c:10-11`"
            + "（`System_Int32__ToString(amount)` 直接塞 TMP、无任何符号处理）"
            + "｜实得「" + d.Text + "」"
            + "｜🧨 把 `Mathf.Abs` 加回伤害分支 ⇒ 变「3」⇒ 本条红");
        Check(d.Text != Mathf.Abs(-3).ToString(),
              "A2（灭自证·反例）伤害文本 **不等于** `Mathf.Abs(-3).ToString()`（=「" + Mathf.Abs(-3) + "」）"
            + "｜实得「" + d.Text + "」");
    }

    // ---------------------------------------------------------------- B
    static void B_Positive(GameObject root)
    {
        var d = PopNumber.PopDamage(root.transform, new Vector3(0f, 1f, 0f), 5);
        Check(d != null, "B0 **前提**：`amount = 5` 的伤害飘字建得出来");
        if (d == null) return;

        Check(d.Text == "5",
              "B ★ 伤害档 `5` ⇒ 文本「5」（无 `+`、无 `-`）｜实得「" + d.Text + "」"
            + "｜🧨 给伤害档加任何符号前缀 ⇒ 红");
    }

    // ---------------------------------------------------------------- C
    static void C_Heal(GameObject root)
    {
        var h = PopNumber.PopHeal(root.transform, new Vector3(0f, 2f, 0f), 5);
        Check(h != null, "C0 **前提**：`amount = 5` 的治疗飘字建得出来");
        if (h == null) return;

        Check(h.Text == "+5",
              "C ★ 治疗档仍是「+5」（本包**不动**治疗档）"
            + "｜判据 `CardScript._Heal_d__468__MoveNext.c:155-204` 的 `\"+\"+量`"
            + "｜实得「" + h.Text + "」｜🧨 把 `\"+\"` 一起删掉 ⇒ 红");

        // C2 = 灭自证：治疗档**保留** `Abs`（原版 `"+"+量` 恒正）。改成 `amount.ToString()` ⇒ 负治疗印 `-5` ⇒ 红。
        var hNeg = PopNumber.PopHeal(root.transform, new Vector3(0f, 3f, 0f), -5);
        Check(hNeg != null, "C2a **前提**：`amount = -5` 的治疗飘字建得出来");
        if (hNeg == null) return;
        Check(hNeg.Text == "+5",
              "C2（灭自证）治疗档 `-5` ⇒ 仍「+5」（原版 `\"+\"+Mathf.Abs(量)` 恒正号）"
            + "｜实得「" + hNeg.Text + "」"
            + "｜🧨 顺手把治疗档也改成 `amount.ToString()`（印 `-5`）⇒ 红");
    }

    // ---------------------------------------------------------------- D
    static void D_Source()
    {
        string path = Path.Combine(Application.dataPath, SourceRel);
        Check(File.Exists(path), "D0 **前提**：读得到 `" + SourceRel + "`（" + path + "）—— 读不到时本节等于没验 ⇒ 当场红");
        if (!File.Exists(path)) return;

        string[] lines = File.ReadAllLines(path);
        int at = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("string txt = heal", StringComparison.Ordinal) >= 0) { at = i; break; }
        }
        Check(at >= 0, "D1 **前提**：找到 `string txt = heal` 那一行（文本拼装只有这一处）");
        if (at < 0) return;
        Debug.Log(P + $"   （`string txt = heal` 现读 `{SourceRel}:{at + 1}`）");

        string line = lines[at];
        int q = line.IndexOf('?');
        int c = q >= 0 ? line.IndexOf(':', q + 1) : -1;
        Check(q > 0 && c > q, "D2 **前提**：三元表达式切得开（`?` 与 `:` 都在）");
        if (!(q > 0 && c > q)) return;

        string healBranch = line.Substring(q + 1, c - q - 1).Trim();
        string dmgBranch = line.Substring(c + 1).Trim();

        Check(dmgBranch.IndexOf("Abs", StringComparison.Ordinal) < 0,
              "D3 ★ **伤害分支里没有 `Abs`**（源码结构性判据 —— 挡「运行时在别处再 Abs 一次」那类漏网）"
            + "｜现读伤害分支 = `" + dmgBranch + "`"
            + "｜🧨 改回 `Mathf.Abs(amount).ToString()` ⇒ 红");
        Check(healBranch.IndexOf("Abs", StringComparison.Ordinal) >= 0
           && healBranch.IndexOf("\"+\"", StringComparison.Ordinal) >= 0,
              "D4 治疗分支**保持** `\"+\" + Mathf.Abs(amount)`（本包不收紧它 —— 改它要另开账）"
            + "｜现读治疗分支 = `" + healBranch + "`");
        Check(dmgBranch.IndexOf("ToString", StringComparison.Ordinal) >= 0,
              "D5 伤害分支走 `ToString()`（原版 `Int32.ToString()` 的逐字对应）｜现读 = `" + dmgBranch + "`");
    }
}
