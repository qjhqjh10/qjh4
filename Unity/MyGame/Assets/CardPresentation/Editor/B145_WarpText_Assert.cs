// B145_WarpText_Assert.cs — 执行代理 `B145-E1`（2026-11-05）
//
// ============================ 它盯的是什么 ============================
//  `Core/TMProWarpText.cs` 原来：`OnEnable` 只 `WarpText()`（**不订阅**）· `OnDisable` 只有 `ForceMeshUpdate()`（**不退订**）·
//  `ReactToTextChanged` **全仓 0 调用点** ⇒ 运行时文本一变，弯字就停在旧网格上（静默）。
//  原版判据（2026-11-05 现读）：
//    · `d:/2/tools/decomp_full/TMProWarpText__OnEnable.c:15-24`
//        `TMProWarpText__WarpText(this, 0);` → 取 `TMPro_EventManager` 的那个 `FastAction`
//        （`*(DAT_1842b08a0 + 0xb8)` 的静态字段 `+0x58`）→ `System_Action<object>___ctor(…, this, <ReactToTextChanged>)`
//        → `TMPro_FastAction<bool>__Add(fastAction, action)`。**先弯一次、再订阅**。
//    · `d:/2/tools/decomp_full/TMProWarpText__OnDisable.c:19-23` —— 同一条链 `TMPro_FastAction<bool>__Remove(...)`，
//        **之后**才 `(**(code **)(*plVar2 + 0x7d8))(plVar2, …)`（= TMP 的 `ForceMeshUpdate`）。
//    · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TMProWarpText.cs:66-79`（ILSpy 版同义、名字是明文）：
//        `OnEnable`  = `WarpText(); TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged);`
//        `OnDisable` = `TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged); text.ForceMeshUpdate();`
//  ⚠️ 本工程 TMP 版本里该事件 = `public static readonly FastAction<Object> TEXT_CHANGED_EVENT`
//     （`Library/PackageCache/com.unity.ugui@*/Runtime/TMP/TMPro_EventManager.cs:30`），元素类型 `UnityEngine.Object`
//     与原版 `System_Action<object>` 对齐。
//
// ============================ 断什么 ============================
//  A 硬前提：三个方法都在 · `TEXT_CHANGED_EVENT` 是 `FastAction<UnityEngine.Object>` · 能反射读到订阅表（**5 条**）
//  B 行为 —— **唯一可信的「订阅 +1 / −1」证据 = `FastAction<Object>` 的私有 `delegates` 链表**：
//    B1 `OnEnable` ⇒ 订阅数 **+1**，且新委托**指向本组件的 `ReactToTextChanged`**（不是别人的）
//    B2 再 `OnEnable` 一次 ⇒ 不变（`FastAction.Add` 按委托相等去重，原版同一份实现）
//    B3 `OnDisable` ⇒ 回到改前，且**不再有**指向本组件的委托
//    B4/B5 `text` 为空（没挂 TMP）时：`OnEnable` **照样订阅** · `OnDisable` **不抛异常且照样退订**
//    B6 带真 TMP（`Resources/Fonts/RobotoCondensed-Regular SDF`）时 `OnEnable`/`OnDisable` 不抛异常
//  C `ReactToTextChanged` 的三道闸（原版 `ReactToTextChanged.c:22-30`：`obj` 空 / 不是 `TMP_Text` / 不是自己的 `text`）：
//    三种入参都不抛异常，且**不改订阅数**
//  E 原版 bug 照抄（调度台 2026-11-05 裁定 `isForceUpdatingMesh` 按原版走）：空文本跑过一次 `WarpText()` ⇒ 标记**仍为 1**、
//    随后 `ReactToTextChanged` **不会**重弯（`.c:120/123/126` 早退不复位、唯一复位点 `.c:352`）；E3 = 有字形时确实复位（灭自证）
//  D 源码结构守卫（「先弯再订」「先退再重算」两处**顺序**行为测不出来，只能读源码）：
//    D1 `OnEnable` 里 `WarpText()` 出现在 `.Add(ReactToTextChanged)` **之前**
//    D2 `OnDisable` 里 `.Remove(ReactToTextChanged)` 出现在 `ForceMeshUpdate()` **之前**
//    D3 灭自证：`OnEnable` 里**不许**出现 `if (text == null) return;`（那会让空 text 时静默不订阅 = 本 bug 复发）
//    D4 文件里**没有** `void Update()` / `void Tick()`（原文件头那句假自陈指的），⛔ 别让那个说法回来
//
// ============================ 灭自证（怎么才能红）============================
//  🧨 删掉 `OnEnable` 里那句 `.Add(...)` ⇒ B1 / B1' / D1 红（订阅数 +0）
//  🧨 删掉 `OnDisable` 里那句 `.Remove(...)` ⇒ B3 / B3' 红（订阅数不回落、委托还在）
//  🧨 把 `OnEnable` 改回 `if (text == null) return;` ⇒ B4 红 + D3 红
//  🧨 把 `.Add(...)` 挪到 `WarpText()` 之前 ⇒ D1 红（行为测不出来，只能靠结构守卫 —— 所以 D1 必须留）
//  🧨 把 `if (text != null) text.ForceMeshUpdate();` 改回原版的无条件写法 ⇒ B5 红（空 text 抛 NRE）
//  🧨 把 `WarpText()` 的早退分支改回 `_warping = false`（= 不复刻原版 bug）⇒ E1 / E2 红
//  ⚠️ B1 的期望值 = 「读数差 **+1**」，⛔ 不拿产品代码里的任何常量反推。
//
// ============================ 怎么接（⚠️ 本代理只做接线，宿主别的地方不动）============================
//  接 `Editor/ShellScene.cs` 的聚合行（`:11301`，**合计 `Debug.Log` 之前**、**同一物理行追加** ⇒ 行数零变化）：
//    B145_WarpText_Assert.Run(); _sink.Pass += B145_WarpText_Assert.Pass; _sink.Fail += B145_WarpText_Assert.Fail;
//  **零前提**：只在临时 GameObject 上建件，收尾 `DestroyImmediate`；不碰场景 / 存档 / 别家订阅者（只比**差值**）。
//
// ============================ 覆盖边界（如实写）============================
//  · 本件**不验**「弯出来的字形对不对」（那是 `B142_E55_Assert` 的曲线逐键；视觉面另有人看）。
//  · 本件**没有**跑 Unity（执行代理只跑秒级 typecheck）⇒ 跑绿之前不得据此收账。
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B145-E1`：`TMProWarpText` 在 `OnEnable` 订阅 `TMPro_EventManager.TEXT_CHANGED_EVENT`、
/// `OnDisable` 成对退订（判据见文件头）。</summary>
public static class B145_WarpText_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B145E1] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    const string SrcWarp = "CardPresentation/Core/TMProWarpText.cs";
    const string FontPath = "Fonts/RobotoCondensed-Regular SDF";

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B145-E1：`TMProWarpText` 的 `TEXT_CHANGED_EVENT` 订阅 / 退订（先弯再订 · 先退再重算）");

        // ── A 硬前提（⛔ 不写 `if (前提) Check(结论)` —— 前提不成立时要**红**，不许静默跳过）──
        var onEnable = typeof(TMProWarpText).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        var onDisable = typeof(TMProWarpText).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        var react = typeof(TMProWarpText).GetMethod("ReactToTextChanged", BindingFlags.Instance | BindingFlags.Public,
                                                    null, new[] { typeof(UnityEngine.Object) }, null);
        T(onEnable != null, "（A1 前提）`TMProWarpText.OnEnable()` 存在（private，引擎按名调）");
        T(onDisable != null, "（A2 前提）`TMProWarpText.OnDisable()` 存在（private，引擎按名调）");
        T(react != null, "（A3 前提）`TMProWarpText.ReactToTextChanged(UnityEngine.Object)` 存在（委托签名要能吃 `Action<Object>`）");

        var ev = TMPro_EventManager.TEXT_CHANGED_EVENT;
        var evType = ev == null ? null : ev.GetType();
        var ga = (evType != null && evType.IsGenericType) ? evType.GetGenericArguments() : null;
        T(ga != null && ga.Length == 1 && ga[0] == typeof(UnityEngine.Object),
          "（A4 前提）`TEXT_CHANGED_EVENT` = `FastAction<UnityEngine.Object>`（原版那个 FastAction 的元素类型 = `object`）");

        int probe = SubCount();
        T(probe >= 0, "（A5 前提）能反射读到 `TEXT_CHANGED_EVENT` 的私有订阅表 `FastAction<Object>.delegates`"
          + "（读不到 ⇒ 本件后面全不可信；现读 = " + probe + "）");
        if (onEnable == null || onDisable == null || react == null || probe < 0)
        {
            S("A 前提不成立 ⇒ B/C/D 无法执行（已在上面的前提条里红）");
            return;
        }

        var fixtures = new System.Collections.Generic.List<GameObject>();
        try
        {
            // ------------------------------------------------------------ B 行为
            // 无 TMP 的实例：`text` 为空也要订阅（订阅**不在** null 早退之后）
            var goNull = new GameObject("B145E1 Warp(null text)");
            fixtures.Add(goNull);
            var wNull = goNull.AddComponent<TMProWarpText>();
            onDisable.Invoke(wNull, null);            // 先清干净（AddComponent 若已触发过 OnEnable 也不影响读数）
            int before = SubCount();
            onEnable.Invoke(wNull, null);
            int after = SubCount();
            T(after == before + 1, "★ B1 `OnEnable` ⇒ `TEXT_CHANGED_EVENT` 订阅数 **+1**（改前 " + before + " → 改后 " + after + "）");
            T(HasHandlerFor(wNull), "★ B1' 新注册的那个委托**指向本组件的 `ReactToTextChanged`**（不是别人的）");

            onEnable.Invoke(wNull, null);             // 重复 OnEnable
            int again = SubCount();
            T(again == after, "B2 再调一次 `OnEnable` 订阅数不变（" + after + " → " + again
              + "；`FastAction.Add` 按委托相等去重）");

            onDisable.Invoke(wNull, null);
            int off = SubCount();
            T(off == before, "★ B3 `OnDisable` ⇒ 订阅数**回到改前**（" + after + " → " + off + "，期望 " + before + "）");
            T(!HasHandlerFor(wNull), "★ B3' 退订后**不再有**指向本组件的委托");

            // text 为空时：OnEnable 照样订阅、OnDisable 不抛异常且照样退订
            int b4 = SubCount();
            onEnable.Invoke(wNull, null);
            T(SubCount() == b4 + 1, "★ B4 `text` 为空（没挂 TMP）时 `OnEnable` **照样订阅**（现读 " + SubCount() + "，期望 " + (b4 + 1) + "）");
            bool threw = false;
            try { onDisable.Invoke(wNull, null); } catch (Exception) { threw = true; }
            T(!threw && SubCount() == b4, "★ B5 `text` 为空时 `OnDisable` **不抛异常且照样退订**（抛=" + threw
              + " · 现读订阅数 " + SubCount() + "，期望 " + b4 + "）");

            // 带真 TMP：走 WarpText 的整条路径（含 ForceMeshUpdate），不许抛异常
            var canvasGo = new GameObject("B145E1 Canvas", typeof(RectTransform), typeof(Canvas));
            fixtures.Add(canvasGo);
            var goTmp = new GameObject("B145E1 Warp(+TMP)", typeof(RectTransform));
            goTmp.transform.SetParent(canvasGo.transform, false);
            var tmp = goTmp.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>(FontPath);
            T(font != null, "（B6 前提）取得到字体 `Resources/" + FontPath + "`（弯字要真字形才会走满 `WarpText`）");
            if (font != null) tmp.font = font;
            tmp.text = "Warp Text";
            var wTmp = goTmp.AddComponent<TMProWarpText>();
            onDisable.Invoke(wTmp, null);
            int b6 = SubCount();
            bool threw6 = false;
            try { onEnable.Invoke(wTmp, null); } catch (Exception) { threw6 = true; }
            T(!threw6 && SubCount() == b6 + 1 && HasHandlerFor(wTmp),
              "★ B6 带真 TMP 的 `OnEnable`：不抛异常 + 订阅 +1 + 委托指向本组件（抛=" + threw6
              + " · 订阅 " + b6 + " → " + SubCount() + "）");
            bool threw6b = false;
            try { onDisable.Invoke(wTmp, null); } catch (Exception) { threw6b = true; }
            T(!threw6b && SubCount() == b6, "★ B6' 带真 TMP 的 `OnDisable`：不抛异常 + 订阅数回落（抛=" + threw6b
              + " · 现读 " + SubCount() + "，期望 " + b6 + "）");

            // ------------------------------------------------------------ C `ReactToTextChanged` 的三道闸
            var otherGo = new GameObject("B145E1 Other TMP", typeof(RectTransform));
            otherGo.transform.SetParent(canvasGo.transform, false);
            fixtures.Add(otherGo);
            var otherTmp = otherGo.AddComponent<TextMeshProUGUI>();
            int c0 = SubCount();
            T(NoThrow(delegate { wTmp.ReactToTextChanged(null); }),
              "C1 `ReactToTextChanged(null)` 直接返回、不抛异常（原版 `ReactToTextChanged.c:22-30` 第一道闸）");
            T(NoThrow(delegate { wTmp.ReactToTextChanged(otherGo); }),
              "C2 传入**不是** `TMP_Text` 的对象 ⇒ 直接返回、不抛异常");
            T(NoThrow(delegate { wTmp.ReactToTextChanged(otherTmp); }),
              "C3 传入**别家的** `TMP_Text`（≠ `text`）⇒ 直接返回、不抛异常");
            T(SubCount() == c0, "C4 上面三次调用都**没有改订阅数**（" + c0 + " → " + SubCount() + "）");

            // ------------------------------------------------------------ E 原版 bug 照抄：早退**不复位** `isForceUpdatingMesh`
            //  判据：`decomp_full/TMProWarpText__WarpText.c:110` 置 1；`:120` / `:123` / `:126` 三条早退**都不复位**；
            //        **唯一**复位点 = `:352`（走完循环）。⇒ 空文本上跑过一次后，`ReactToTextChanged.c:29` 的闸恒假、再也不重弯。
            //  调度台 2026-11-05 裁定：**照抄原版（含这个 bug）**，⛔ 别修（先例 = `BroadcastCardDrawn` 原版漏 `!`）。
            var goEmpty = new GameObject("B145E1 Warp(empty text)", typeof(RectTransform));
            goEmpty.transform.SetParent(canvasGo.transform, false);
            fixtures.Add(goEmpty);
            var emptyTmp = goEmpty.AddComponent<TextMeshProUGUI>();
            emptyTmp.text = "";                       // 空文本 ⇒ `WarpText` 走 `characterCount == 0` 那条早退
            var wEmpty = goEmpty.AddComponent<TMProWarpText>();
            wEmpty.Configure(emptyTmp, TMProWarpText.Axis.Z, TMProWarpText.DefaultCurve(), -0.09f);
            T(wEmpty.IsForceUpdatingMesh,
              "★ E1 空文本跑过一次 `WarpText()` ⇒ `isForceUpdatingMesh` **仍为 1**（原版早退不复位：`.c:120/123/126` vs 唯一复位点 `.c:352`；照抄原版 bug，⛔ 别修）");
            wEmpty.ReactToTextChanged(emptyTmp);
            T(wEmpty.IsForceUpdatingMesh,
              "★ E2 紧接着 `ReactToTextChanged(自己)` ⇒ 仍为 1（原版 `__ReactToTextChanged.c:29` 的闸恒假 ⇒ **不会**重弯）");

            tmp.ForceMeshUpdate();
            int chars = tmp.textInfo == null ? -1 : tmp.textInfo.characterCount;
            T(chars > 0, "（E3 前提）带字形的夹具 `characterCount = " + chars + "`（>0 才会走完 `WarpText` 的循环）");
            wTmp.WarpText();
            T(!wTmp.IsForceUpdatingMesh,
              "★ E3 灭自证：**有字形**的文本走完循环 ⇒ 复位为 0（证明 E1 不是「恒 true」；现读 " + wTmp.IsForceUpdatingMesh + "）");
        }
        finally
        {
            for (int i = fixtures.Count - 1; i >= 0; i--)
                if (fixtures[i] != null) UnityEngine.Object.DestroyImmediate(fixtures[i]);
        }

        // ------------------------------------------------------------ D 源码结构守卫
        string src = ReadAsset(SrcWarp);
        T(src != null, "（D0 前提）读得到源码 `Assets/" + SrcWarp + "`");
        string onEn = MethodBody(src, "void OnEnable()");
        string onDis = MethodBody(src, "void OnDisable()");
        T(onEn != null, "（D0 前提）在源码里找到 `OnEnable` 方法体");
        T(onDis != null, "（D0 前提）在源码里找到 `OnDisable` 方法体");
        if (onEn != null && onDis != null)
        {
            int iWarp = onEn.IndexOf("WarpText()", StringComparison.Ordinal);
            int iAdd = onEn.IndexOf(".Add(ReactToTextChanged)", StringComparison.Ordinal);
            T(iWarp >= 0 && iAdd >= 0 && iWarp < iAdd,
              "★ D1 `OnEnable`：`WarpText()` 在 `.Add(ReactToTextChanged)` **之前**（原版 `OnEnable.c:15` → `:22` 同序）"
              + "（现读 idx WarpText=" + iWarp + " · Add=" + iAdd + "）");

            int iRem = onDis.IndexOf(".Remove(ReactToTextChanged)", StringComparison.Ordinal);
            int iFmu = onDis.IndexOf("ForceMeshUpdate()", StringComparison.Ordinal);
            T(iRem >= 0 && iFmu >= 0 && iRem < iFmu,
              "★ D2 `OnDisable`：`.Remove(ReactToTextChanged)` 在 `ForceMeshUpdate()` **之前**（原版 `OnDisable.c:23` → `:28` 同序）"
              + "（现读 idx Remove=" + iRem + " · ForceMeshUpdate=" + iFmu + "）");

            T(onEn.IndexOf("if (text == null) return;", StringComparison.Ordinal) < 0,
              "★ D3 灭自证：`OnEnable` 里**没有** `if (text == null) return;`（有它 ⇒ 空 text 时静默不订阅 = 本 bug 复发）");
        }
        T(src != null && src.IndexOf("void Update()", StringComparison.Ordinal) < 0
                    && src.IndexOf("void Tick()", StringComparison.Ordinal) < 0,
          "★ D4 源码里**没有** `void Update()` / `void Tick()`（原文件头那句「`Update` 里文本变了才重弯（见 `Tick()`）」是假的，已订正）");

        S("小结：✓ " + _sink.Pass + " · ✗ " + _sink.Fail);
    }

    // ============================================================ 反射读 `FastAction<Object>` 的订阅表
    static IEnumerable Subscribers()
    {
        var ev = TMPro_EventManager.TEXT_CHANGED_EVENT;
        if (ev == null) return null;
        var f = ev.GetType().GetField("delegates", BindingFlags.Instance | BindingFlags.NonPublic);
        return f == null ? null : f.GetValue(ev) as IEnumerable;
    }

    static int SubCount()
    {
        var e = Subscribers();
        if (e == null) return -1;
        int n = 0;
        foreach (var _ in e) n++;
        return n;
    }

    /// <summary>订阅表里有没有「目标 = 这颗组件 · 方法名 = `ReactToTextChanged`」那一条。</summary>
    static bool HasHandlerFor(TMProWarpText w)
    {
        var e = Subscribers();
        if (e == null || w == null) return false;
        foreach (var d in e)
        {
            var del = d as Delegate;
            if (del == null) continue;
            if (ReferenceEquals(del.Target, w) && del.Method != null && del.Method.Name == "ReactToTextChanged") return true;
        }
        return false;
    }

    static bool NoThrow(Action a)
    {
        try { a(); return true; } catch (Exception) { return false; }
    }

    // ============================================================ 源码读口
    static string ReadAsset(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    /// <summary>取一条 8 空格缩进的方法体（从签名到它自己那行的 `}` 为止；找不到 ⇒ null）。</summary>
    static string MethodBody(string src, string sig)
    {
        if (src == null || sig == null) return null;
        int i = src.IndexOf(sig, StringComparison.Ordinal);
        if (i < 0) return null;
        int end = src.IndexOf("\n        }", i, StringComparison.Ordinal);
        return end < 0 ? null : src.Substring(i, end - i);
    }
}
