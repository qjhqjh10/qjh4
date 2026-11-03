// B124_W10_Assert.cs — 执行代理 `B124-W10`（2026-11-03）· `EventSystem.pixelDragThreshold = 5`（`B120-A13`）
//
// ============================ 它盯的是什么 ============================
//  `Battle/BattleEventSystem.Ensure()` 的**三条路**（① 收养 `EventSystem.current` · ② 收养场景里那只 · ③ 现建）
//  都必须把 `pixelDragThreshold` 设成 **5**。`B124-W4` 已于 2026-11-03 把三处都补上，但**没有任何断言验过它** ——
//  本文件补上；不加，2/3 的修复（两条收养路）永远没被走过。
//
// ============================ 判据 ============================
//  · **原版值** = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:5429`
//    `m_DragThreshold: 5`（常驻 `EventSystem` 的序列化值；`decomp_full` 里 0 处读写 ⇒ 奔跑值就是 5）。
//  · **uGUI 默认** = 10：本机包源码
//    `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:68`
//    `private int m_DragThreshold = 10;`（`:73-76` 就是 `pixelDragThreshold` 的属性口）。
//  · **唯一出处** = `Battle/AttackSelector.PopDragThresholdPx`（`= 5f`，`AttackSelector.cs:252`）
//    ⇒ 本文件**读生产常量**、不另写 `5` 的字面量之外的判据（字面量只出现在断言文案与期望里）。
//
// ============================ 灭自证（🧨 一条改动不能两边都绿）============================
//  🧨 A2：`pixelDragThreshold < 10` —— 与 A（`== 5`）**结构上不可能同时被写反满足**：
//     把赋值改成 `= 10` ⇒ A 红；改成 `= 12` ⇒ A2 红；三处赋值全删（回到 uGUI 默认）⇒ A 红。
//  🧨 B0/B0b：**硬前提**先断「外部现建那只此刻就是 10、且就是 `current`」—— 前提不成立时 B 结论没有观测面
//     （同族先例：`if (前提) Check(结论)` 会让整条断言**静默跳过**，本文件一律拆成两条 `Check`）。
//  🧨 B：删掉 `Ensure()` ①② 那两句赋值 ⇒ `Instance` 仍是那只、但阈值停在 10 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自带临时 `EventSystem`，收尾 `DestroyImmediate` + `ResetForTest()`）。
//  挂法（由调度台合并，⛔ 本文件不碰 `Editor/*Scene.cs`）：同一物理行追加
//      B124_W10_Assert.Run(); pass += B124_W10_Assert.Pass; fail += B124_W10_Assert.Fail;
//  判绿红：`grep "^B124W10"` 的合计行；逐条按**行首** `B124W10 ✓` / `B124W10 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using UnityEngine;
using UnityEngine.EventSystems;
using CardPresentation;

/// <summary>执行代理 `B124-W10`：`BattleEventSystem.Ensure()` 三条路都把 `pixelDragThreshold` 设为 5（原版 `Intro.unity:5429`）。</summary>
public static class B124_W10_Assert
{
    const string Tag = "B124W10";
    static int _pass, _fail;

    /// <summary>通过条数（宿主聚合行累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主聚合行累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    /// <summary>把场景里除 <paramref name="keep"/> 之外的 `EventSystem` 全销毁（返回销毁只数）。
    /// <para>为什么需要：`Ensure()` 的 ① 支先看 `EventSystem.current` —— 场里要是残留着别人的一只，
    /// 「现建」「收养」这两条路的观测面就都不对（判出来的绿/红与代码无关）。销毁时**出声**，⛔ 不静默。</para></summary>
    static int DestroyStrays(EventSystem keep)
    {
        var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var e = all[i];
            if (e == null || ReferenceEquals(e, keep)) continue;
            Object.DestroyImmediate(e.gameObject);
            n++;
        }
        return n;
    }

    public static void Run()
    {
        GameObject adoptedGo = null;
        try
        {
            // 清场：上一次跑 / 别的宿主留下的那只（`ResetForTest()` 幂等；`Instance` 为 null 时什么都不做）
            BattleEventSystem.ResetForTest();
            int stray = DestroyStrays(null);
            if (stray > 0)
                Debug.Log(Tag + " 清场：销毁了 " + stray + " 只残留 `EventSystem`"
                        + "（留着的话 `Ensure()` 会先收养它 ⇒ 下面「现建 / 收养」两条路的观测面不成立）");

            // ── A / A2：③ 现建路 ────────────────────────────────────────────────────
            var es = BattleEventSystem.Ensure();
            int thr = es != null ? es.pixelDragThreshold : -1;
            Check(es != null, "A0（硬前提）`Ensure()` 现建出那只 `EventSystem`（拿不到 ⇒ A/A2/B 都没有观测面）");
            Check(es != null && thr == 5,
                  "A ★ `Ensure()` 之后 `pixelDragThreshold == 5`（原版 `Intro.unity:5429` `m_DragThreshold = 5`；uGUI 默认 10）"
                + "｜实得 " + thr
                + "｜🧨 删掉 `BattleEventSystem.Ensure()` ③ 那一句赋值 ⇒ 得 uGUI 默认 10 ⇒ 红（`B120-A13` 埋着的那条）");
            Check(es != null && thr < 10,
                  "A2 🧨 灭自证：`pixelDragThreshold < 10`（与 A 结构上不可能同时被写反满足）"
                + "｜把赋值改成 `= 10` ⇒ A 红；改成 `= 12` ⇒ 本条红；实得 " + thr);

            // ── B：①② 收养路（外部现建一只停在 10 的，`Ensure()` 必须把它改成 5）────────
            BattleEventSystem.ResetForTest();
            DestroyStrays(null);                                  // 连刚才现建那只也清掉 ⇒ 下一句必然是「外部那只」
            adoptedGo = new GameObject("B124W10_AdoptedES");
            adoptedGo.SetActive(false);                            // 编辑模式：`AddComponent` 不派 `OnEnable`
            var adopted = adoptedGo.AddComponent<EventSystem>();
            adoptedGo.SetActive(true);                             // 激活那一刻派 `OnEnable` ⇒ 登记成 `EventSystem.current`
            // 🔴 2026-11-03（`B124_W19`）：**编辑模式下 `SetActive(true)` 也不补派 `OnEnable`**（CLAUDE.md 已记的同族坑）
            //    ⇒ 它没进 uGUI 的 `m_EventSystems`（`UGUI/EventSystem.cs:409-411` 的 `OnEnable` 才 `Add(this)`）
            //    ⇒ `EventSystem.current` 仍是 null ⇒ B0b 硬前提恒 False、B 那条「收养路」实际是借 ② 支蒙过去的。
            //    ① 先手动派一次 `OnEnable`（登记进 `m_EventSystems`）；
            //    ② 若 `[0]` 位上还留着上一步 `DestroyImmediate` 的死件（编辑模式不派 `OnDisable` ⇒ 不会自己摘），
            //       用 `current` 的 setter 把本只挪到 `[0]`（setter 只重排已有条目，`UGUI/EventSystem.cs:32-50`）。
            if (EventSystem.current != adopted) adopted.SendMessage("OnEnable");
            if (EventSystem.current != adopted) EventSystem.current = adopted;
            Check(adopted != null && adopted.pixelDragThreshold == 10,
                  "B0（硬前提）外部现建的那只此刻停在 **uGUI 默认 10** —— 不成立 ⇒ B 证明不了「收养路把 10 改成 5」"
                + "｜实得 " + (adopted != null ? adopted.pixelDragThreshold : -1));
            Check(adopted != null && EventSystem.current == adopted,
                  "B0b（硬前提）它此刻就是 `EventSystem.current` —— 否则 `Ensure()` 走 ②/③ 而不是 ① 收养"
                + "｜实得 " + (EventSystem.current == adopted));
            var got = BattleEventSystem.Ensure();
            int thr2 = got != null ? got.pixelDragThreshold : -1;
            Check(ReferenceEquals(got, adopted) && thr2 == 5,
                  "B ★ 收养路：`Ensure()` 收养了外部那只、并把它从 10 **改成 5**（判据同 A）"
                + "｜实得 instanceIsAdopted=" + ReferenceEquals(got, adopted) + " · threshold=" + thr2
                + "｜🧨 删掉 `Ensure()` ①/② 那两句 `pixelDragThreshold = …` ⇒ 仍旧 10 ⇒ 红（这正是 `B120-A13` 埋着的那条）");
        }
        finally
        {
            BattleEventSystem.ResetForTest();
            DestroyStrays(null);
            if (adoptedGo != null) Object.DestroyImmediate(adoptedGo);
        }
    }
}
