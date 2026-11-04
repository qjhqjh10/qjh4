// B140_WD_Assert.cs — 执行代理 `B140_WD` 的断言（BattleDriver 域 2 件：`B139-E2` / `B139-G1`）
//
// ============================ 它盯的是什么 ============================
//   · `B139-E2`：受击飘字**没传 `key`** ⇒ `PopNumber` 每条都新建一颗（原版每卡只有一颗 `DamageCounter`，
//     再中一次是**重播同一颗**）⇒「每次新建不重播」在生产里等于没做。
//   · `B139-G1`：特效**不该吃父/卡旋转** —— 原版 `BattleManager.CreateAnim` 是
//     `Instantiate(prefab, 锚点.position, **prefab.localRotation 当世界旋转**, 锚点)`。
//     我们的三参 `CardEffects.Fire(name, pos, parent)` 只能继承父旋转 ⇒ 按卡锚点播的那几条偏了。
//
// ============================ 怎么用（无宿主，直接跑）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WD_Assert.Run -logFile -
//   判绿红：按**行首** `B140WD ✓` / `B140WD ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · 飘字复用 = `decomp_full/CardDamageCounterController__DamageReceived.c:10-18`
//     （`SetActive(true)` + `SetText` + `Animation.Play()` = 重启同一节点）；机制本身的断言归 `B139_E_Assert:E2`
//     （⛔ 本件不重复它，铁律 6：一条规则只写一处）—— 本件只钉**调用点传了 `key`、且传的是受击那张卡**。
//   · 特效旋转 = `decomp_full/BattleManager__CreateAnim.c:56-72`（`Instantiate(prefab, pos, prefab.localRotation, parent)`）。
//     对照路（**要**继承锚点旋转）= `decomp_full/CardScript__PlayTriggerAnim.c:29-33`（两参 `Instantiate(prefab, anchor)`）
//     ⇒ 默认档 ⛔ 不许翻成 `true`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1-2 ← 把 `key: v` 删掉（改回 `PopNumber.PopDamage(transform, popAt, e.Amount, enemy: popFoe)`）⇒ 红。
//   · A2-1/A2-2 ← 把两条 `FireCardAnchored(...)` 改回三参 `CardEffects.Fire(...)` ⇒ 红。
//   · A3-2 ← 把 `FireAtAttackAnchor` 那条也改成 `FireCardAnchored`（= 传 `true`）⇒ 红
//            （载体朝向 = 攻击特效的朝向，翻了攻击特效全失去朝向）。
//   · A4-2 ← 把 `Spawn` 的 `worldRotationStays` 默认值改成 `true` ⇒ 红（会打断 `PlayTriggerAnim` 那条原版路）。
//   · B1-3 ← 让 `Spawn` 丢掉 `worldRotationStays`（回到 `WarpforgeEffectPlayer.Play(..., assignScale)`）⇒ 红。
//   · B2-1 ← 删掉 `FireCardAnchored` 里的 `_cardAnchorDepth++` ⇒ 红（作用域捎不过去 ⇒ 又吃父旋转）。
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

public static class B140_WD_Assert
{
    const string P = "B140WD ";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + what); }
        else { _fail++; Debug.LogError(P + "✗ " + what); }
    }

    static void Section(string t) { Debug.Log("--- " + P + "· " + t + " ---"); }

    /// <summary>被测源码（相对 `Assets/`）。</summary>
    const string DriverRel = "CardPresentation/Battle/BattleDriver.cs";
    const string HookRel = "WarpforgeVFX/Runtime/WarpforgeEffectHook.cs";

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(P + "=== B140-WD 断言开始（B139-E2 飘字 key / B139-G1 特效世界旋转）===");
        SourceLocks();
        HookPassThrough();
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ==================================================================
    //  A · 调用点锁（**不看行号**：按方法签名截方法体，天然抗行号漂移）
    // ==================================================================
    static void SourceLocks()
    {
        Section("A · 调用点锁（源码：方法体里「走哪条通道」）");
        string driverPath = Path.Combine(Application.dataPath, DriverRel);
        string hookPath = Path.Combine(Application.dataPath, HookRel);
        Check(File.Exists(driverPath), $"A0a **前提**：读得到 `{DriverRel}`（{driverPath}）—— 读不到时本节等于没验 ⇒ 当场红");
        Check(File.Exists(hookPath), $"A0b **前提**：读得到 `{HookRel}`（{hookPath}）");
        if (!File.Exists(driverPath) || !File.Exists(hookPath)) return;

        string drv = File.ReadAllText(driverPath);
        string hook = File.ReadAllText(hookPath);

        // ---- A1 · B139-E2：受击路径的 `PopDamage` 必须带 `key:` ----
        string hitCode = StripComments(BodyOf(drv, "void PlayHitFeel("));
        Check(hitCode.Length > 2, "A1-0 **前提**：`PlayHitFeel` 的方法体截得到（受击那条路 = 伤害飘字的落点）");
        Check(hitCode.IndexOf("var v = ViewAt(e.Player, e.Slot);", StringComparison.Ordinal) >= 0,
              "A1-1 **前提**：`v` 就是受击那张卡的视图（`ViewAt(e.Player, e.Slot)`）");
        int at = hitCode.IndexOf("PopNumber.PopDamage(", StringComparison.Ordinal);
        Check(at >= 0, "A1-1b **前提**：受击路径里调了 `PopNumber.PopDamage(`");
        if (at >= 0)
        {
            int close = hitCode.IndexOf(')', at);
            string call = close > at ? hitCode.Substring(at, close - at + 1) : "";
            Check(call.Contains("key: v"),
                  "A1-2 ★ `B139-E2`：受击飘字**传了 `key`**（= 受击那张卡 ⇒ 同卡再中一次走「重播同一颗」）"
                + "（实得：`" + call + "`）"
                + "｜判据 `CardDamageCounterController__DamageReceived.c:10-18`｜🧨 删掉 `key: v` ⇒ 红");
            Check(call.IndexOf("enemy: popFoe", StringComparison.Ordinal) >= 0,
                  "A1-3 两行档（`enemy:`）仍在 —— `key` 是**追加**的形参，没有把原参数换掉");
        }

        // ---- A2 · B139-G1：`PlayAbilityBeat` 的两条「按卡锚点播」走 `FireCardAnchored` ----
        string beat = StripComments(BodyOf(drv, "void PlayAbilityBeat("));
        Check(beat.Length > 2, "A2-0 **前提**：`PlayAbilityBeat` 的方法体截得到");
        Check(beat.IndexOf("WarpforgeEffectHook.FireCardAnchored(vfx, Vector3.zero, anchor)", StringComparison.Ordinal) >= 0,
              "A2-1 ★ `B139-G1`：**无载体那一支**（直接挂在锚点上）走 `FireCardAnchored` ⇒ 世界旋转 = prefab 自身"
            + "｜🧨 改回三参 `CardEffects.Fire(vfx, Vector3.zero, anchor)` ⇒ 红");
        Check(beat.IndexOf("WarpforgeEffectHook.FireCardAnchored(vfx, Vector3.zero, carrier)", StringComparison.Ordinal) >= 0,
              "A2-2 ★ `B139-G1`：**载体那一支**（位移/朝向拍）同样走 `FireCardAnchored`"
            + "（`orient` 档随后由 `FlattenNewEffectRotation` 归零 ⇒ 世界旋转 = 载体瞄准朝向，与原版 `RotateAnim` 同结论）");
        Check(beat.IndexOf("CardEffects.Fire(vfx, Vector3.zero, anchor)", StringComparison.Ordinal) < 0
           && beat.IndexOf("CardEffects.Fire(vfx, Vector3.zero, carrier)", StringComparison.Ordinal) < 0,
              "A2-3（灭自证·反例）`PlayAbilityBeat` 里**不再有**三参 `CardEffects.Fire(vfx, Vector3.zero, anchor|carrier)`");

        // ---- A3 · 攻击锚点 ⛔ 不许翻（载体朝向 = 攻击特效的朝向）----
        string atk = StripComments(BodyOf(drv, "void FireAtAttackAnchor("));
        Check(atk.Length > 2, "A3-0 **前提**：`FireAtAttackAnchor` 的方法体截得到");
        Check(atk.IndexOf("CardEffects.Fire(name, Vector3.zero, carrier)", StringComparison.Ordinal) >= 0,
              "A3-1 ★ `FireAtAttackAnchor` 仍走**三参** `CardEffects.Fire`（特效继承载体朝向 = 攻击朝向）");
        Check(atk.IndexOf("FireCardAnchored", StringComparison.Ordinal) < 0
           && atk.IndexOf("worldRotationStays", StringComparison.Ordinal) < 0,
              "A3-2 ★（灭自证）`FireAtAttackAnchor` 里**没有** `FireCardAnchored` / `worldRotationStays`"
            + "｜🧨 把它改成 `true` ⇒ 攻击特效全失去朝向 ⇒ 本条红");

        // ---- A4 · 钩子侧：形参真的存在、默认 false、作用域真的接上了 ----
        string spawn = StripComments(BodyOf(hook, "public static GameObject Spawn("));
        Check(spawn.Length > 2, "A4-0 **前提**：`WarpforgeEffectHook.Spawn` 的方法体截得到（它现在是类级方法，不是 `Install` 里的局部函数）");
        Check(spawn.IndexOf("worldRotationStays || _cardAnchorDepth > 0", StringComparison.Ordinal) >= 0,
              "A4-1 ★ `Spawn` 把这一位**透传**给 `WarpforgeEffectPlayer.Play(..., worldRotationStays)`（显式形参 + 同步作用域两路合并）"
            + "｜🧨 丢掉这一位 ⇒ B1-3 红");
        string sigLine = LineOf(hook, "public static GameObject Spawn(");
        Check(sigLine.IndexOf("bool worldRotationStays = false", StringComparison.Ordinal) >= 0,
              "A4-2 ★ 形参默认值 = **false**（默认档 = 另一条原版路 `PlayTriggerAnim`，那条要继承锚点旋转）"
            + "｜🧨 默认值改成 true ⇒ 红");
        Check(hook.IndexOf("_cardAnchorDepth++;", StringComparison.Ordinal) >= 0
           && hook.IndexOf("_cardAnchorDepth--;", StringComparison.Ordinal) >= 0,
              "A4-3 ★ `FireCardAnchored` 是 `try/finally` 括起来的**同步作用域**（生产那条 `CardEffects.Fire` 只有三参）"
            + "｜🧨 删掉 `++` ⇒ B2-1 红");
    }

    // ==================================================================
    //  B · 行为：`worldRotationStays` 真的到得了 `WarpforgeEffectPlayer`
    // ==================================================================
    static void HookPassThrough()
    {
        Section("B · 行为：世界旋转 = prefab 自身（不吃宿主旋转）");
        var lib = WarpforgeEffectLibrary.Instance;
        Check(lib != null, "B0 **前提**：效果库读得到（读不到 ⇒ 下面每条都是空跑）");
        if (lib == null) return;
        var e = lib.entries.FirstOrDefault(x => x != null && x.prefab != null);
        Check(e != null, "B0b **前提**：库里至少有一件带 prefab 的效果");
        if (e == null) return;

        var host = new GameObject("b140wd_rot_host");
        host.transform.rotation = Quaternion.Euler(0f, 47f, 0f);
        var prevPlay = CardEffects.Play;
        var prevInst = CardEffects.PlayInstance;
        try
        {
            Quaternion prefabRot = e.prefab.transform.rotation;
            Quaternion inherited = host.transform.rotation * e.prefab.transform.localRotation;
            Check(Quaternion.Angle(prefabRot, inherited) > 10f,
                  $"B1-0 **前提**：宿主转 47° ⇒ 「prefab 自身」与「父旋转 × prefab.local」差 {Quaternion.Angle(prefabRot, inherited):F1}°"
                + " —— 否则下面两条没有判别力");

            WarpforgeEffectPlayer.ResetB139Diagnostics();
            var a = WarpforgeEffectHook.Spawn(e.name, Vector3.zero, host.transform, worldRotationStays: true);
            Check(a != null, $"B1-1 **前提**：`Spawn(..., worldRotationStays: true)` 给《{e.name}》建出了实例");
            if (a != null)
            {
                Check(Quaternion.Angle(a.transform.rotation, prefabRot) < 0.5f,
                      $"B1-2 ★ 世界旋转 = **prefab 自身**（差 {Quaternion.Angle(a.transform.rotation, prefabRot):F3}°）"
                    + "｜判据 `BattleManager__CreateAnim.c:56-72`（`Instantiate(prefab, pos, prefab.localRotation, parent)`）");
                Check(Quaternion.Angle(a.transform.rotation, inherited) > 10f,
                      $"B1-3 ★ ……而且**不是**「父旋转 × prefab.local」（差 {Quaternion.Angle(a.transform.rotation, inherited):F1}°）"
                    + "｜🧨 让 `Spawn` 丢掉 `worldRotationStays` ⇒ 落到这一档 ⇒ 红");
                Check(WarpforgeEffectPlayer.WorldRotationStaysApplied == 1,
                      $"B1-4 ★ `WorldRotationStaysApplied` = 1（现读 {WarpforgeEffectPlayer.WorldRotationStaysApplied}）；🧨 删掉 `Play` 里那一行 ⇒ 0 ⇒ 红");
                UnityEngine.Object.DestroyImmediate(a);
            }

            WarpforgeEffectPlayer.ResetB139Diagnostics();
            var b = WarpforgeEffectHook.Spawn(e.name, Vector3.zero, host.transform);
            Check(b != null, "B1-5 **前提**：默认档也建得出实例");
            if (b != null)
            {
                Check(Quaternion.Angle(b.transform.rotation, inherited) < 0.5f,
                      $"B1-6 ★ 默认档仍**吃父旋转**（差 {Quaternion.Angle(b.transform.rotation, inherited):F3}°）"
                    + " —— 那是 `PlayTriggerAnim`（两参 `Instantiate`）那条原版路，⛔ 别为了修 12-01 把默认翻过来");
                Check(WarpforgeEffectPlayer.WorldRotationStaysApplied == 0,
                      "B1-7 默认档不涨计数（现读 " + WarpforgeEffectPlayer.WorldRotationStaysApplied + "）");
                UnityEngine.Object.DestroyImmediate(b);
            }

            // ---- B2 · 生产那条口：`FireCardAnchored` → `CardEffects.Fire` → 钩子 ----
            CardEffects.Play = null;
            CardEffects.PlayInstance = (n, p, par) => WarpforgeEffectHook.Spawn(n, p, par);
            int before = host.transform.childCount;
            WarpforgeEffectHook.FireCardAnchored(e.name, Vector3.zero, host.transform);
            Check(host.transform.childCount == before + 1,
                  "B2-0 **前提**：`FireCardAnchored` 确实经 `CardEffects` 建出了一件（少了 ⇒ 这条口断了）");
            if (host.transform.childCount > before)
            {
                var c = host.transform.GetChild(before);
                Check(Quaternion.Angle(c.rotation, prefabRot) < 0.5f,
                      $"B2-1 ★ `FireCardAnchored`（生产口）也得到「世界旋转 = prefab 自身」（差 {Quaternion.Angle(c.rotation, prefabRot):F3}°）"
                    + "｜🧨 删掉 `_cardAnchorDepth++` ⇒ 变「父旋转 × prefab.local」⇒ 红");
            }
        }
        finally
        {
            CardEffects.Play = prevPlay;
            CardEffects.PlayInstance = prevInst;
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    // ==================================================================
    //  源码小工具（**按方法签名截方法体** —— 账再漂也不影响）
    // ==================================================================
    static string BodyOf(string text, string sig)
    {
        int at = text.IndexOf(sig, StringComparison.Ordinal);
        if (at < 0) return "";
        int brace = text.IndexOf('{', at);
        if (brace < 0) return "";
        int depth = 0;
        for (int i = brace; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '{') depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0) return text.Substring(brace, i - brace + 1);
            }
        }
        return "";
    }

    static string LineOf(string text, string sig)
    {
        int at = text.IndexOf(sig, StringComparison.Ordinal);
        if (at < 0) return "";
        int s = at;
        while (s > 0 && text[s - 1] != '\n') s--;
        int e = text.IndexOf('\n', at);
        if (e < 0) e = text.Length;
        return text.Substring(s, e - s).TrimEnd('\r');
    }

    /// <summary>剥掉 `//` 行注释与 `/* */` 块注释（**字符串里的不收**）—— 只留代码。</summary>
    static string StripComments(string code)
    {
        var sb = new System.Text.StringBuilder(code.Length);
        bool inStr = false; char quote = '\0';
        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (inStr)
            {
                sb.Append(c);
                if (c == '\\' && i + 1 < code.Length) { sb.Append(code[++i]); continue; }
                if (c == quote) inStr = false;
                continue;
            }
            if (c == '"' || c == '\'') { inStr = true; quote = c; sb.Append(c); continue; }
            if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
            {
                while (i < code.Length && code[i] != '\n') i++;
                if (i < code.Length) sb.Append('\n');
                continue;
            }
            if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < code.Length && !(code[i] == '*' && code[i + 1] == '/')) i++;
                i++;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
