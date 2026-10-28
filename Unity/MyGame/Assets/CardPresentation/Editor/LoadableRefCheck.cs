// LoadableRefCheck.cs — `LoadableRef<T>` 那 12 条的**批处理自检**（账 `其他/基础设施:288·289·290·294·295·296·297·298·299·301·303·304`）。
//
// ============================ 为什么单开一份 ============================
//  · 那 12 条（`LoadableReference<T>` 的 2 个 ctor · 2 个 setter · `Equals`×2 · `GetHashCode` ·
//    `WriteJson` / `OnBeforeRead` / `OnBeforeWrite` · `implicit operator T` / `implicit operator string`）
//    的落点全在 `Core/LoadableRef.cs`，而**现有宿主没有一个碰过它**（`grep -rn LoadableRef Editor/` = 0）
//    ⇒ 这些成员此前**一条断言都没有**。本文件只放断言，不改实现。
//  · ⚠️ 本批（B89 第 1 波）红线**禁止跑 Unity** ⇒ 本文件写好后**没人跑过**，
//    「断言对不对」要等调度台在同步点把它接进 `_run_8_checks.sh` 才见分晓（见 §5 交接）。
//
// ============================ 判据 ============================
//  · 主判据 = 泛型方法体真产物 `D:/2/tools/decomp_generic_trial/out/LoadableReference_object___*.c`
//    （G6/G7 导出的 Ghidra 伪码，逐句读过；文件名后的 `__101xxxx` 就是 RVA）。
//  · 交叉 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs`（`:203-806`，噪声大但结构对得上）。
//  · ⚠️ 泛型类字段偏移：`genericObjectReference` = +0x10 · `targetId` = +0x18 · `reference` = +0x20
//    （`…___Equals__101a8c0.c` 里 `*(param_1 + 0x20)` 读的是本体、`+0x18` 读的是 id）。
//
// ============================ 三条「照抄原版、⛔ 别修」 ============================
//  ① `Equals(LoadableRef<T>)` **传 null ⇒ 抛 NullReferenceException** —— 原版没有 null 判
//     （Ghidra `…__Equals__101a8c0.c` 的 `FUN_1803f47a0()` = NRE 帮助函数；ILSpy `:638-642` 直接 `new NullReferenceException()`）。
//     施工单 `B88_R10` §1.2 第 5 行写的「if(other==null) return false」**是推的、不是读的**。
//  ② `OnBeforeRead(null)` ⇒ NRE（原版首句 `rawJson.Type` 就解引用）。
//  ③ `SetReferenceByObject(null)` ⇒ **`targetId` 被清成 null**（原版 `if (param_2 == 0) uVar1 = 0;`，
//     不是「不动」—— 我们 2026-10-28 之前那一版是「不动」，B89_E9 就地订正）。
//
// ============================ 用法 ============================
//   bash d:/4/Unity/工具/_run_8_checks.sh 里加一条（串行）：
//     unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//       -executeMethod LoadableRefCheck.Run -logFile -
//   ⚠️ 判绿红看「LOADREF 合计」，不看退出码（同仓其它宿主的约定）。

using System;
using System.Reflection;
using UnityEngine;
using CardPresentation;

public static class LoadableRefCheck
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("LOADREF ✓ " + what); }
        else { _fail++; Debug.LogError("LOADREF ✗ " + what); }
    }

    /// <summary>夹具：`LoadableRef&lt;T&gt;` 要的 `T : class` + 一个 id。
    /// 原版靠 `T : IIdentifiable.GetID()`，我们靠构造时注入 `Func&lt;T,string&gt;`（`LoadableRef.cs` 文件头「🔶 自己设计」）。</summary>
    public sealed class Item
    {
        public string Id;
        public Item(string id) { Id = id; }
    }

    static readonly Func<Item, string> IdOf = x => (x == null ? null : x.Id);

    /// <summary>`protected bool Equals(LoadableRef<T>)` —— 原版是 protected（ILSpy `:622`），外部只能用反射调。</summary>
    static readonly MethodInfo EqualsOf = typeof(LoadableRef<Item>).GetMethod(
        "Equals",
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(LoadableRef<Item>) },
        null);

    static bool EqT(LoadableRef<Item> x, LoadableRef<Item> y)
    {
        return (bool)EqualsOf.Invoke(x, new object[] { y });
    }

    /// <summary>批处理入口。</summary>
    public static void Run()
    {
        _pass = _fail = 0;

        // ── 前提（硬检查在前）：protected 那一支真的在、签名是泛型那一支 —— 没有它，下面所有 Equals 断言都是空转 ──
        Check(EqualsOf != null,
              "（前提）`protected bool Equals(LoadableRef<T>)` 存在且签名是「泛型那一支」（不是 object 重载）");
        if (EqualsOf == null)
        {
            Debug.LogError("LOADREF === 合计：0 通过 / 1 失败（前提不成立，后续断言无法执行）===");
            if (Application.isBatchMode) Application.Quit(1);
            return;
        }

        LoadableRef<Item>.ResetNotesForTest();

        // ════════════════════════ ① 构造 `LoadableRef<T>(T)` ════════════════════════
        // 原版 `…___.ctor__101c040.c`：`reference = param; targetId = (param == null ? null : param.GetID());`
        var itemA = new Item("card_a");
        var a = new LoadableRef<Item>(itemA, IdOf);
        Check(a.TargetId == "card_a",
              $"构造(T)：TargetId 由对象回填（实得 `{a.TargetId ?? "null"}`）");
        Check(ReferenceEquals(a.Reference, itemA),
              "构造(T)：Reference 就是传进来的那一份本体（不是副本）");

        var aNull = new LoadableRef<Item>((Item)null, IdOf);
        Check(aNull.TargetId == null,
              $"构造(T = null)：TargetId 是 null（原版 `if (param_2 == 0) uVar1 = 0;`，实得 `{aNull.TargetId ?? "null"}`）");

        // ════════════════════════ ② 构造 `LoadableRef<T>(string)` ════════════════════════
        // 原版 `…___.ctor__101c110.c`：`targetId = id; reference = AssetLocator.GetAsset<T>(id);`
        LoadableRef<Item>.ResetNotesForTest();
        var b = new LoadableRef<Item>("card_b");
        Check(b.TargetId == "card_b", "构造(string)：只记 id");
        Check(b.Reference == null,
              "构造(string)：本体不解 —— 原版这一跳是 `AssetLocator.GetAsset<T>(id)`，我们没有寻址层 ⇒ 出声 + null"
            + "（⚠️ 上面这句会打一条 `[LoadableRef] 只有 id、没有本体` 的警告，**是预期的**）");

        // ════════════════════════ ③ `SetReferenceById(string)` ════════════════════════
        // 原版 `…__SetReferenceById__101b3d0.c`：`targetId = id; reference = AssetLocator.GetAsset<T>(id);`
        var c = new LoadableRef<Item>(new Item("card_c"), IdOf);
        c.SetReferenceById("card_d");
        Check(c.TargetId == "card_d", $"SetReferenceById：id 改成新的（实得 `{c.TargetId ?? "null"}`）");
        Check(c.Reference == null,
              "SetReferenceById：旧本体被清（原版此处会去 GetAsset 覆盖；我们 = null，等价于原版「解不出」那一档）");

        // ════════════════════════ ④ `SetReferenceByObject(T)` ════════════════════════
        // 原版 `…__SetReferenceByObject__101b650.c`：`reference = obj; targetId = (obj == null ? null : obj.GetID());`
        var d = new LoadableRef<Item>(new Item("stale"), IdOf);   // 🔴 2026-10-28（B89_RD）夹具订正：④ 这条语义（`targetId = obj.GetID()`）在原版靠 `T : IIdentifiable`，我们的等价物是**构造时注入的取 id 口**（`LoadableRef.cs:63-70`，`_idOf` 是 readonly、只有这一个入口能设，且**不序列化**）—— 原来用 `LoadableRef<Item>("stale")`（string ctor）没有那个口 ⇒ 回填那段整段不执行，断言必红（实现是对的）
        d.SetReferenceByObject(new Item("card_e"));
        Check(d.TargetId == "card_e", $"SetReferenceByObject(obj)：id 被回填（实得 `{d.TargetId ?? "null"}`）");
        d.SetReferenceByObject(null);
        Check(d.TargetId == null,
              $"SetReferenceByObject(null)：**TargetId 被清成 null**（原版 `if (param_2 == 0) uVar1 = 0;`，"
            + $"⛔ 不是「不动」；实得 `{d.TargetId ?? "null"}`）");
        Check(d.Reference == null, "SetReferenceByObject(null)：reference 也是 null");

        // ════════════════════════ ⑤ `Equals(LoadableRef<T>)` ════════════════════════
        // 原版 `…___Equals__101a8c0.c`：两边先各自回填 targetId，再比 targetId。
        var e1 = new LoadableRef<Item>(new Item("same"), IdOf);
        var e2 = new LoadableRef<Item>(new Item("same"), IdOf);
        var e3 = new LoadableRef<Item>(new Item("other"), IdOf);
        Check(EqT(e1, e2),
              "Equals(T)：两份**不同实例**、同 id ⇒ true —— 这一条同时挡「改成比引用」那种退步（灭自证）");
        Check(!EqT(e1, e3), "Equals(T)：id 不同 ⇒ false");

        var e4 = new LoadableRef<Item>("same");     // 只有 id、没有本体
        Check(EqT(e4, e1),
              "Equals(T)：一方只带 id、一方只带本体 ⇒ true（原版首句/第二句的「先从本体回填 id」真的生效）");

        bool nreOnNull = false;
        try { EqT(e1, null); }
        catch (TargetInvocationException tie) { nreOnNull = tie.InnerException is NullReferenceException; }
        catch (NullReferenceException) { nreOnNull = true; }
        Check(nreOnNull,
              "Equals(T) 传 null ⇒ 抛 NullReferenceException（原版**没有** null 判；⛔ 别改成 `return false`）");

        // ════════════════════════ ⑥ `Equals(object)` ════════════════════════
        // 原版 `…___Equals__101a5a0.c`：null ⇒ false · 同实例 ⇒ true · 类型不同 ⇒ false · 否则转 Equals(T)。
        Check(!e1.Equals((object)null), "Equals(object)：null ⇒ false（原版首句）");
        Check(e1.Equals((object)e1), "Equals(object)：同一实例 ⇒ true（`ReferenceEquals` 那一支）");
        Check(e1.Equals((object)e2), "Equals(object)：同 id 不同实例 ⇒ true（落进 Equals(T)）");
        Check(!e1.Equals((object)e3), "Equals(object)：id 不同 ⇒ false");
        Check(!e1.Equals((object)"same"),
              "Equals(object)：类型不同 ⇒ false（原版 `obj.GetType() != GetType()` 那一支）");
        Check(!e1.Equals((object)new LoadableRef<object>("same")),
              "Equals(object)：泛型实参不同 ⇒ false（`LoadableRef<Item>` vs `LoadableRef<object>`）");

        // ════════════════════════ ⑦ `GetHashCode()` ════════════════════════
        // 原版 `…___GetHashCode__101aa10.c`：`[回填] return targetId == null ? 0 : targetId.GetHashCode();`
        Check(e1.GetHashCode() == e2.GetHashCode(),
              "GetHashCode：同 id ⇒ 同哈希（改了 `Equals` 就必须同改它，否则同 id 落不同桶）");
        Check(new LoadableRef<Item>((string)null).GetHashCode() == 0,
              "GetHashCode：targetId 为 null ⇒ 0（原版 `return 0`）");
        var gh = new LoadableRef<Item>(new Item("h"), IdOf);
        Check(gh.GetHashCode() == "h".GetHashCode(),
              "GetHashCode：就是 `targetId.GetHashCode()`（原版末句直接转调 string 的那一个）");

        // ════════════════════════ ⑧ `implicit operator string` ════════════════════════
        // 原版 `…___op_Implicit__101cb70.c`：`r == null ? null : (r.TargetId == null ? null : r.targetId)`
        Check(((string)(LoadableRef<Item>)null) == null, "implicit string：引用为 null ⇒ null");
        Check(((string)new LoadableRef<Item>((string)null)) == null,
              "implicit string：TargetId 为 null ⇒ null（原版第二句）");
        Check(((string)e1) == "same", $"implicit string：有 id ⇒ id（实得 `{(string)e1 ?? "null"}`）");

        // ════════════════════════ ⑨ `implicit operator T` ════════════════════════
        // 原版 `…___op_Implicit__101c840.c`：`r == null ? null : (r.TargetId == null ? null : r.Reference)`
        Item t0 = (LoadableRef<Item>)null;
        Check(t0 == null, "implicit T：引用为 null ⇒ null");
        Item t1 = new LoadableRef<Item>((string)null);
        Check(t1 == null, "implicit T：TargetId 为 null ⇒ null（原版第二句）");
        Item expectedT = e1.Reference;
        Item t2 = e1;
        Check(expectedT != null && ReferenceEquals(t2, expectedT),
              "implicit T：有本体 ⇒ 就是那一份（原版 `return instanceRef.Reference;`）");

        // ════════════════════════ ⑩ 序列化三钩子 ════════════════════════
        // 原版 `…___WriteJson__101bec0.c`：`[回填] return new JValue(targetId);` ⇒ 写出去的是**裸 id 字符串**
        var w = new LoadableRef<Item>(new Item("w1"), IdOf);
        object wj = w.WriteJson();
        Check(wj is string && (string)wj == "w1",
              $"WriteJson：写出裸 id 字符串（原版 `new JValue(targetId)`；实得 `{wj ?? "null"}`）");
        var w2 = new LoadableRef<Item>("w2");       // 只有 id 的引用也要写得出来
        object wj2 = w2.WriteJson();
        Check(wj2 is string && (string)wj2 == "w2",
              $"WriteJson：只有 id、没有本体的引用同样写出 id（实得 `{wj2 ?? "null"}`）");

        // 原版 `…___OnBeforeRead__101b150.c`：`if (rawJson.Type == JTokenType.String) targetId = rawJson.Value<string>();`
        var rd = new LoadableRef<Item>("before");
        rd.OnBeforeRead("after");
        Check(rd.TargetId == "after",
              $"OnBeforeRead(字符串 token)：targetId 取该字符串（实得 `{rd.TargetId ?? "null"}`）");
        rd.OnBeforeRead(42L);
        Check(rd.TargetId == "after",
              "OnBeforeRead(非字符串 token)：**原样不动**（原版只有 `JTokenType.String` 那一档才写）");
        bool readNre = false;
        try { rd.OnBeforeRead(null); }
        catch (NullReferenceException) { readNre = true; }
        Check(readNre, "OnBeforeRead(null) ⇒ NRE（原版首句 `rawJson.Type` 就解引用；照抄）");

        // 原版 `…___OnBeforeWrite__4b33b0.c`：空体（RVA 0x4B33B0 = 全局空桩 `ret 0`）
        var obw = new LoadableRef<Item>(new Item("nowrite"), IdOf);
        obw.OnBeforeWrite();
        Check(obw.TargetId == "nowrite" && obw.Reference != null,
              "OnBeforeWrite：无副作用（原版空体 ⇒ id 与本体都不许变）");

        LoadableRef<Item>.ResetNotesForTest();

        Debug.Log($"LOADREF === 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) Application.Quit(_fail == 0 ? 0 : 1);
    }
}
