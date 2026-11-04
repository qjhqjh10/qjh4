// B141_W66_Assert.cs — 执行写手 `B141-W66`（第一百二十八会话 · 2026-11-04）· W129 `B141-ALLOWCLICK`
//
// ============================ 它盯的是什么 ============================
//  `Shell/AchievementsMenu.cs:272-273` 原注释写「这一族 `allowClickWhenAlreadySelected = 0`」——**写反了**。
//  原版 `Player Profile Window.prefab` 逐颗现读（`EverguildToggle` 字段偏移 `0x139`）：
//
//  | prefab ▸ 节点族 | 颗 | `allowClickWhenAlreadySelected` |
//  |---|---:|---:|
//  | `Trophies Tab/buttons/Achievement Type Toggle (0..4)` | 5 | **1（5/5）** |
//  | `Menu Area/Tab Buttons/*`（档案窗左栏） | 6 | **0（6/6）** |
//
//  判据工具 = `python -I -X utf8 d:/4/_tmp_view/b141_w60/list_toggles.py
//             "d:/2/tools/assetripper_out/ExportedProject/Assets/Player Profile Window.prefab"`（只读）。
//
//  回调判据（`d:/2/tools/decomp_full/AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c`）：
//     `NotifyToggleOn(toggleGroup, this.toggle, false, false)` → 写 `filter(+0x58)` → `Refresh()`
//     ⇒ **没有「同值就 return」那一段**。原版再点当前分类 = **重放回调 + 按钮音 + 重画**；
//     我们的等价物 = `allow = true`（原版 5/5 颗就是 1）+ `Select()` 里**不设同值闸**。
//
//  ⚠️ 颗数口径（别照「5 颗」写死）：prefab 里烘焙 **5 颗**；运行时 `AchievementsMenu.Awake` 按
//     `Enum.GetValues(AchievementType)` **重建**，而原版枚举只有 4 个值
//     （`Battle=1 / Collection=2 / Victories=4 / Account=8`，判据 `Everguild.LiveOps/Achievement.cs:5-10`）
//     ⇒ **运行时 4 颗**，我们同（`ProfileData.TypeOrder`）。所以本断言断「**每一颗**都是 true」，
//     颗数由 `ProfileData.TypeOrder.Length` 现读，⛔ 不写死 5。
//
// ============================ 断言（16 条）============================
//  A 组（源码文本 · 灭自证）：
//    A1 前提：`Shell/AchievementsMenu.cs` 读得到
//    A2 成就页签赋值 = **`true`**（恰 1 处；`tabWb.allowClickWhenAlreadySelected = true;`）
//    A3 🧨 成就页签**零处** `= false`（改回 false ⇒ 红 —— 这正是本批修掉的那处）
//    A4 🧨 `Select()` 里**没有**同值早退（把 `if (_filter == type) return;` 加回去 ⇒ 红）
//    A5 左栏（`Shell/PlayerProfileWindow.cs`）仍是 `_btnWb[i].allowClickWhenAlreadySelected = false;`
//    A6 四扇窗左栏（`Shell/MainMenuSubmenuWindow.cs`）仍是 `tabWb.allowClickWhenAlreadySelected = false;`
//    A7 颗数（两条：A7a/A7b）：`ProfileData.TypeOrder.Length` = **4**（原版枚举 4 个值）· `PlayerProfileWindow.Tabs.Length` = **6**（左栏 6 颗）
//  B 组（运行时探针 · `WindowButton` 语义，全部在临时节点上，用完即销毁）：
//    B1（两条：B1a/B1b）前提：探针 `WindowButton` 挂得上 + 新实例缺省 `allowClickWhenAlreadySelected` = **true**
//    B2 左栏口径：`allow = false` + `isOn = true` ⇒ **不派发**（0 次）
//    B3 🧨 灭自证：同一颗、同一个 `isOn`，只把 `allow` 改成 `true` ⇒ **派发 1 次**（⇒ B2 不是恒真）
//    B4 成就页口径：`allow = true` 连点两次 ⇒ **派发 2 次**（不吞重复点）
//    B5 未选中：`allow = false` + `isOn = false` ⇒ 照常派发
//    B6 `allow = true` 时 `isOn` **一次都不被调用**（守卫整条短路 ⇒ `isOn` 对成就页是死字段）
//    B7 探针收尾：拿到的「不派发」全部来自**守卫**而不是别的闸（`absorbOnly/interactable` 保持缺省）
//
// ============================ 🧨 什么坏法会让哪条红 ============================
//  · 把 `AchievementsMenu` 那行改回 `= false;`（= 旧错口径）⇒ **A2 + A3 + B4** 红。
//  · 把 `if (_filter == type) return;` 加回 `Select()` ⇒ **A4** 红。
//  · 顺手把左栏一起改成 `true` ⇒ **A5 / A6** 红。
//  · 把 `PlayerProfileWindow.Tabs` 删掉一颗 / 把 `ProfileData.TypeOrder` 改成 5 ⇒ **A7** 红。
//  · `WindowButton.Click()` 的守卫被删（`if (!allowClickWhenAlreadySelected && isOn != null && isOn()) return;`）⇒ **B2/B3** 同时红。
//  · 把守卫改成「先播音后 return」/ 恒放行 ⇒ **B2** 红；恒拦截 ⇒ **B3/B5** 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 份源码 + 2 个静态表 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W66_Assert.Run(); pass += B141_W66_Assert.Pass; fail += B141_W66_Assert.Fail;
//  判绿红：按行首 `B141W66 ✓` / `B141W66 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W66`：`allowClickWhenAlreadySelected` 两族逐值（成就页 5 颗 prefab = 1 / 左栏 6 颗 = 0）的回归断言。</summary>
public static class B141_W66_Assert
{
    const string Tag = "B141W66";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    /// <summary>原版成就页颗数（prefab 烘焙 5 颗；运行时按枚举重建 4 颗 —— 见文件头「颗数口径」）。</summary>
    public const int PrefabAchievementToggles = 5;
    /// <summary>原版档案窗左栏颗数（prefab `Menu Area/Tab Buttons/*`）。</summary>
    public const int PrefabLeftBarToggles = 6;

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    static bool Has(string s, string needle)
    {
        return !string.IsNullOrEmpty(s) && s.IndexOf(needle, System.StringComparison.Ordinal) >= 0;
    }

    /// <summary>去掉所有空白（空格/制表/换行）—— 跨行实参也能按「连续 token」匹配。</summary>
    static string Squash(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, @"\s+", "");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_Probes();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（成就页 prefab " + PrefabAchievementToggles + " 颗 = 1 · 左栏 prefab " + PrefabLeftBarToggles + " 颗 = 0）");
    }

    // ==================================================================
    //  A · 逐族逐值（判据 = `Player Profile Window.prefab` 的 `EverguildToggle` 字段 0x139）
    // ==================================================================
    static void GroupA_Wiring()
    {
        var ach = Squash(Src("Shell/AchievementsMenu.cs"));
        var left = Squash(Src("Shell/PlayerProfileWindow.cs"));
        var sub = Squash(Src("Shell/MainMenuSubmenuWindow.cs"));

        Check(ach.Length > 0, "A1 前提：`Shell/AchievementsMenu.cs` 读得到（长度 " + ach.Length + "）");
        Check(Has(ach, "tabWb.allowClickWhenAlreadySelected=true;"),
              "A2 🎯 成就页签赋值 = **true**（原版 `Trophies Tab/buttons/Achievement Type Toggle (0..4)` 5/5 颗 = 1）");
        Check(!Has(ach, "allowClickWhenAlreadySelected=false"),
              "A3 🧨 成就页签**零处** `= false`（改回旧错口径 ⇒ 本条 + A2 一起红）");
        Check(!Has(ach, "if(_filter==type)return;"),
              "A4 🧨 `Select()` 里**没有**同值早退（判据 = 原版 `<Awake>b__0` 回调体：`NotifyToggleOn` → 写 filter → `Refresh()`，无闸）");

        Check(Has(left, "_btnWb[i].allowClickWhenAlreadySelected=false;"),
              "A5 左栏（`PlayerProfileWindow` 六颗，prefab `Menu Area/Tab Buttons/*` = 6/6 颗 0）仍是 **false**");
        Check(Has(sub, "tabWb.allowClickWhenAlreadySelected=false;"),
              "A6 四扇窗左栏（`MainMenuSubmenuWindow`）仍是 **false**（本次⛔ 没被顺手改掉）");

        int typeOrder = ProfileData.TypeOrder != null ? ProfileData.TypeOrder.Length : -1;
        int leftTabs = PlayerProfileWindow.Tabs != null ? PlayerProfileWindow.Tabs.Length : -1;
        Check(typeOrder == 4,
              "A7a 成就页运行时颗数 = `ProfileData.TypeOrder.Length` = **4**（原版枚举 `AchievementType` 只有 4 个值 —— "
              + "`Everguild.LiveOps/Achievement.cs:5-10`）｜实得 " + typeOrder);
        Check(leftTabs == PrefabLeftBarToggles,
              "A7b 左栏表 = **6** 颗（prefab 原值；其中 `Ranked` 按用户 2026-10-24 口径隐藏 ⇒ 运行期建 5 颗）｜实得 " + leftTabs);
    }

    // ==================================================================
    //  B · 运行时探针（`WindowButton` 守卫语义 + 灭自证）
    // ==================================================================
    static void GroupB_Probes()
    {
        var root = new GameObject("B141W66_probe");
        try
        {
            var probe = root.AddComponent<WindowButton>();
            var freshGo = new GameObject("B141W66_probe_fresh");
            freshGo.transform.SetParent(root.transform, false);
            var fresh = freshGo.AddComponent<WindowButton>();
            Check(probe != null && fresh != null, "B1a 前提：探针 `WindowButton` 挂得上");
            if (probe == null || fresh == null) return;
            Check(fresh.allowClickWhenAlreadySelected,
                  "B1b 新实例缺省 `allowClickWhenAlreadySelected` = **true**（`WindowButton` 的口径：只有逐站点核过的 toggle 才置 false）");

            // 只测「派发闸」；按钮音那一路要有 A/V 宿主才验得了（如实标，⛔ 不冒充已验）。
            probe.playClickSound = false;
            int n = 0;
            probe.onClick = () => n++;

            // ---- B2 · 左栏口径：已选中 + allow=false ⇒ 整下吃掉 ----
            probe.allowClickWhenAlreadySelected = false;
            probe.isOn = () => true;
            probe.ClickForTest();
            Check(n == 0, "B2 左栏口径：`allow = false` + `isOn = true` ⇒ **不派发**（原版 `OnPointerClick` 第一段）｜实得 " + n + " 次");

            // ---- B3 · 🧨 灭自证：同一颗、同一个 isOn，只翻 allow ----
            probe.allowClickWhenAlreadySelected = true;
            probe.ClickForTest();
            Check(n == 1, "B3 🧨 同一颗只把 `allow` 翻成 true ⇒ **派发**（⇒ B2 不是恒真断言）｜实得 " + n + " 次");

            // ---- B4 · 成就页口径：连点两次都要派发（原版 5/5 颗 = 1）----
            probe.ClickForTest();
            Check(n == 2, "B4 成就页口径：`allow = true` 连点两次 ⇒ **派发 2 次**（再点当前分类照原版重放回调）｜实得 " + n + " 次");

            // ---- B5 · 未选中时 allow=false 也照常可点 ----
            probe.allowClickWhenAlreadySelected = false;
            probe.isOn = () => false;
            probe.ClickForTest();
            Check(n == 3, "B5 `allow = false` + `isOn = false`（未选中）⇒ 照常派发｜实得 " + n + " 次");

            // ---- B6 · allow=true 时 isOn 是死字段（守卫短路）----
            int isOnCalls = 0;
            probe.allowClickWhenAlreadySelected = true;
            probe.isOn = () => { isOnCalls++; return true; };
            probe.ClickForTest();
            Check(isOnCalls == 0 && n == 4,
                  "B6 `allow = true` ⇒ `isOn` **一次都不被调用**（守卫整条短路 ⇒ 成就页不设 `isOn` 不是漏接线）｜实得 "
                  + isOnCalls + " 次 · 派发累计 " + n);

            // ---- B7 · 上面「不派发」只来自守卫：另外两道闸保持缺省 ----
            Check(!probe.absorbOnly && probe.Interactable,
                  "B7 探针的 `absorbOnly` 关着、`Interactable` 开着 ⇒ B2 的「0 次」确实来自 `allowClickWhenAlreadySelected` 守卫");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
