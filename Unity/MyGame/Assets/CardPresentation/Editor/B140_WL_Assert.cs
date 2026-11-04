// B140_WL_Assert.cs — 执行代理 `B140-WL`（2026-11-04）· EventSystem 两件参数级复刻
//
// ============================ 它盯的是什么 ============================
//  ① `PointerLayer.DragThreshold` = **5**（原版 `EventSystem.m_DragThreshold` 的**序列化值**）。
//     改前 = 10 —— 那是 **uGUI 字段初值**，不是原版值（`B139-Y4` 同批查出）。
//  ② 运行期那只 `EventSystem.sendNavigationEvents` = **false**（原版 `m_sendNavigationEvents: 0`）。
//     `BattleEventSystem.Ensure()` 的**三条路**（① 收养 `current` · ② 收养场景那只 · ③ 现建）都要设 ——
//     只补 ③ 时，Play 模式（`current` 早就存在）走的是 ①，永远设不上。
//
// ============================ 判据（两份都在同一块资产里）============================
//  · `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:5428` `m_sendNavigationEvents: 0`
//  · 同文件 `:5429` `m_DragThreshold: 5`
//  · uGUI 默认（= 改错时会掉回的那一档）：`Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/EventSystem/EventSystem.cs:56`
//    `private bool m_sendNavigationEvents = true;` · `:68` `private int m_DragThreshold = 10;`
//
// ============================ 灭自证（🧨 一条改动不能两边都绿）============================
//  🧨 A：`DragThreshold == 5f`；A1：`< 10f` —— 改回 10 ⇒ A 红、A1 也红。
//  🧨 B0：**先证明观测面存在** —— 外部现建一只的 `sendNavigationEvents` 此刻是 **true**（uGUI 默认）；
//     不成立 ⇒ B 绿得没有意义（同族先例：`if (前提) Check(结论)` 会静默跳过，本文件一律拆两条 `Check`）。
//  🧨 B：把 `Ensure()` ①②③ 的 `sendNavigationEvents = false` 删掉 ⇒ 收养/现建那两只都停在 true ⇒ B 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自带临时 `EventSystem`，收尾 `DestroyImmediate` + `ResetForTest()`）。
//  挂法（由调度台合并，⛔ 本文件不碰 `Editor/*Scene.cs`）：同一物理行追加
//      B140_WL_Assert.Run(); pass += B140_WL_Assert.Pass; fail += B140_WL_Assert.Fail;
//  判绿红：逐条按**行首** `B140WL ✓` / `B140WL ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using UnityEngine;
using UnityEngine.EventSystems;
using CardPresentation;

/// <summary>执行代理 `B140-WL`：`PointerLayer.DragThreshold = 5` + `EventSystem.sendNavigationEvents = false`（原版 `Intro.unity:5428-5429`）。</summary>
public static class B140_WL_Assert
{
    const string Tag = "B140WL";
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
    /// <para>为什么需要：`Ensure()` 的 ① 支先看 `EventSystem.current` —— 场里残留别人的一只，
    /// 「现建 / 收养」两条路的观测面就不对（判出来的绿/红与代码无关）。销毁时**出声**，⛔ 不静默。</para></summary>
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
        GameObject adoptedGo = null, rawGo = null;
        try
        {
            // ── A：常量（`PointerLayer.DragThreshold`）────────────────────────────────
            float thr = PointerLayer.DragThreshold;
            Check(thr == 5f,
                  "A ★ `PointerLayer.DragThreshold == 5`（原版 `Intro.unity:5429` `m_DragThreshold: 5`；uGUI 字段初值 10）"
                + "｜实得 " + thr + "｜🧨 改回 10f ⇒ 本条红");
            Check(thr < 10f,
                  "A1 🧨 灭自证：`DragThreshold < 10`（与 A 结构上不可能同时被写反满足；改回 uGUI 默认 10 ⇒ A/A1 双红）"
                + "｜实得 " + thr);

            // ── C：RewardsScene 那两条样本（4px 不算拖 / 6px 算拖）的判别力 ──────────────
            // 判据样本 = `Editor/RewardsScene.cs` 的 `dragP1`（设计 x 1129 = 按下 +4px）与 `dragP2`（1131 = +6px）。
            Check(thr > 4f,
                  "C1 样本判别力：4px **仍在阈值内**（RewardsScene `dragP1` = 按下 +4px ⇒ 不该判成拖拽）｜实得阈值 " + thr);
            Check(thr < 6f,
                  "C2 样本判别力：6px **已越阈值**（RewardsScene `dragP2` = 按下 +6px ⇒ 必须判成拖拽）｜实得阈值 " + thr);

            // ── B0：观测面 —— 外部现建一只此刻是 uGUI 默认 true ────────────────────────
            BattleEventSystem.ResetForTest();
            int stray = DestroyStrays(null);
            if (stray > 0)
                Debug.Log(Tag + " 清场：销毁了 " + stray + " 只残留 `EventSystem`（否则 `Ensure()` 会先收养它）");

            rawGo = new GameObject("B140WL_RawES");
            rawGo.SetActive(false);                       // 编辑模式：`AddComponent` 不派 `OnEnable`
            var raw = rawGo.AddComponent<EventSystem>();
            Check(raw != null && raw.sendNavigationEvents,
                  "B0（硬前提）裸 `EventSystem` 出厂 `sendNavigationEvents == true`（uGUI `EventSystem.cs:56`）"
                + " —— 不成立 ⇒ B 证明不了「是我们把它关掉的」｜实得 " + (raw != null ? raw.sendNavigationEvents.ToString() : "null"));
            Object.DestroyImmediate(rawGo); rawGo = null;

            // ── B：③ 现建路 ──────────────────────────────────────────────────────────
            var es = BattleEventSystem.Ensure();
            Check(es != null, "B0b（硬前提）`Ensure()` 现建出那只 `EventSystem`（拿不到 ⇒ B/B1 没有观测面）");
            Check(es != null && !es.sendNavigationEvents,
                  "B ★ `Ensure()` 现建路：`sendNavigationEvents == false`（原版 `Intro.unity:5428` `m_sendNavigationEvents: 0`）"
                + "｜实得 " + (es != null ? es.sendNavigationEvents.ToString() : "null")
                + "｜🧨 删掉 `Ensure()` ③ 那句赋值 ⇒ 停在 uGUI 默认 true ⇒ 红");
            Check(es != null && es.pixelDragThreshold == 5,
                  "B1 同一只的 `pixelDragThreshold` 仍是 5（与 `B124_W10_Assert` 同源，这里只是顺带守一眼）"
                + "｜实得 " + (es != null ? es.pixelDragThreshold : -1));

            // ── D：①/② 收养路（外部现建一只停在默认值的，`Ensure()` 必须把它关掉）────────
            BattleEventSystem.ResetForTest();
            DestroyStrays(null);
            adoptedGo = new GameObject("B140WL_AdoptedES");
            adoptedGo.SetActive(false);
            var adopted = adoptedGo.AddComponent<EventSystem>();
            adoptedGo.SetActive(true);
            // 编辑模式下 `SetActive(true)` 也不补派 `OnEnable` ⇒ 手动登记成 `current`（同 `B124_W10_Assert` 的注释）
            if (EventSystem.current != adopted) adopted.SendMessage("OnEnable");
            if (EventSystem.current != adopted) EventSystem.current = adopted;

            Check(adopted != null && adopted.sendNavigationEvents,
                  "D0（硬前提）外部那只此刻停在 **uGUI 默认 true** —— 不成立 ⇒ D 证明不了「收养路把它关掉」｜实得 "
                + (adopted != null ? adopted.sendNavigationEvents.ToString() : "null"));
            Check(adopted != null && EventSystem.current == adopted,
                  "D0b（硬前提）它此刻就是 `EventSystem.current`（否则 `Ensure()` 走 ②/③ 而不是 ① 收养）｜实得 "
                + (EventSystem.current == adopted));
            var got = BattleEventSystem.Ensure();
            Check(ReferenceEquals(got, adopted) && !got.sendNavigationEvents,
                  "D ★ 收养路：`Ensure()` 收养了外部那只、并把 `sendNavigationEvents` 从 true **改成 false**（判据同 B）"
                + "｜实得 instanceIsAdopted=" + ReferenceEquals(got, adopted)
                + " · sendNavigationEvents=" + (got != null ? got.sendNavigationEvents.ToString() : "null")
                + "｜🧨 删掉 `Ensure()` ①/② 那两句赋值 ⇒ 仍旧 true ⇒ 红（Play 模式走的就是这条路）");
        }
        finally
        {
            BattleEventSystem.ResetForTest();
            DestroyStrays(null);
            if (rawGo != null) Object.DestroyImmediate(rawGo);
            if (adoptedGo != null) Object.DestroyImmediate(adoptedGo);
        }
    }
}
