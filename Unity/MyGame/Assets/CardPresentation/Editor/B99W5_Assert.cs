// B99W5_Assert.cs — 执行代理 `B99_W5`（第九十九会话 · 波 2 补位 · `DeckRuntime` 残留）的断言集
//
// ============================ 它盯的是什么（两件）============================
//   ① **开窗回第一页**：原版 `DeckEditingWindow__TryOpen.c:123` 把 tab0 那颗 `Toggle` 置 1
//      （`UnityEngine_UI_Toggle__set_isOn(*(lVar5 + 0x28), 1, 0)`）⇒ **每次开窗都回到 Cards 页**。
//      我们的 `Shell/DeckEditingWindow.Open()` **复用同一个 `DeckRuntime` 实例**（`_runtime` 缓存在组件上），
//      而原版**每次开窗都是新实例**（判据见下）⇒ 不显式重置 `_tab` 就会「关窗时在哪页、再开还在哪页」。
//      落点 = `Deck/DeckRuntime.cs:1397` 那句 `_tab = 0; RefreshAll(); WildcardDisplayEnable();`。
//   ② **开窗那一拍永不播扫光**：B99_W1 曾判成「第二次起才播」—— **不成立**。
//      ① `WildcardDisplay__OnEnable.c`：`Signal.Register` **无条件** + `if (*(int*)(this+0x50) != 0) { Play; … }`
//         （`+0x50` = `currentArmy`）；② `TryOpen` 里 tab0 的 `isOn=1` 在 **:123**、`ToggleDisplay(…,1)` 在 **:126**，
//         而写 `+0x50` 的 `FilterToDeck` 在 **:134** ⇒ 开窗那一刻 `+0x50` 恒为出厂 0；
//      ③ **决定性**：`Intro.unity` 里 `WindowsManager.temporaryWindowDictionary[0]` 就是 `DeckEditingWindow`，
//         其 `windowReference.m_AssetGUID = 02993d13b9580364aa733e56e13252ef` = `bundle_menus_assets_all`
//         的 **m_Container 键**（addressable）⇒ 走 `OpenWindow(ComponentReference)` 的 `Load()+Instantiate()`、
//         并 `automaticallyLoadedWindows.Add(…)`；而 `WindowsManager__CloseWindowCO.c:36-47` 对「在这张表里」的窗
//         做 `Object.Destroy(gameObject)` + `Release` + `Remove` ⇒ **关一次就销毁** ⇒ 每次开窗都是新实例
//         ⇒ `+0x50` 不跨开窗保留 ⇒ 开窗那一拍的闸**永远**关着。
//
//   判据全文（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B99_W5.md` §二 ／ `报告/B99_W1.md` §六·1
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · ① A2/a（`ActiveTab`）：删掉 `Build()` 里那句 `_tab = 0;` ⇒ 第 2 次开窗后 `ActiveTab == 2` ⇒ 红。
//   · ① A2/b（**视图**）：只写 `_tab = 0` 而**不**让 `RefreshAll()` 跑（或把刷新挪到赋值之前）
//         ⇒ 页签高亮还停在 Cosmetics ⇒ 红。**两条结构上不可能被同一个错法一起满足**。
//   · ② B2（首次不播）：改回 `WildcardDisplayEnable()` 里「无条件 `Play`」 ⇒ 首次 `Rate==0 && Enabled` ⇒ 红。
//   · ② B4（第 2 次也不播）：把 `WildcardShineAfterTab(2)` 加回 `WildcardDisplayEnable()`（= W1 的写法）
//         ⇒ 第 2 次 `Rate==0 && Enabled` ⇒ 红。
//   · ② B5（Register 无条件）：把 `HookWildcardUpdate()` 从 `WildcardDisplayEnable()` 删掉 ⇒ 红。
//   · ② C1（窗内换页还要播）：为了修 B4 把 `WildcardShineAfterTab` 的 `Play` 整段删掉（= 永远不播）
//         ⇒ `UiSetTab(2); UiSetTab(0)` 之后 `Rate == -1` ⇒ 红。**B2/B4 ∧ C1 结构上不可能同时满足**。
//   · ② C2（Hook 真挂在事件上）：退订掉（`HookWildcardUpdate` 不调 / `UnhookWildcardUpdate` 多调一次）
//         ⇒ 库存更新后 `Rate` 停在 0.4 ⇒ 红。
//
// ============================ 怎么用（宿主）============================
//   `Assets/CardPresentation/Editor/DeckScene.cs` 的 `RunCore()` 里，「版面」那一节
//   `state = Build(DeckLibrary.Load(), out _root);`（现读 `:825`）**之后**插一行：
//
//       B99W5_Assert.Run((tag, ok, detail) => CheckTrue(ok, tag + " —— " + detail));   // B99_W5：开窗回第一页 + 开窗永不播
//
//   ⚠️ **本件白名单不含宿主**（`Editor/DeckScene.cs` 归调度台）⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//   ⚠️ **与 `Editor/B99W1_Assert.cs` 冲突**：那份的 A2（「第二次开窗播」）与 A4（直接假设层已挂上）**已被本件推翻**
//      ⇒ 接线时**只接本文件**、把 `B99W1_Assert.cs` 的 A2 / A4 删掉（A1 / A3 / A5 仍然成立，可保留）。
//      详见 `报告/B99_W5.md` §五。
//
//   ⚠️ 本文件**不落盘**：进门前先 `CardProgress.ResetForTest()`（关掉卡牌进度落盘，⛔ 绝不改玩家存档 ——
//      与 `CardFaceAssertE5.cs` / `B99W1_Assert.cs` 同一口径），卡组库用**内存里的空库**（`new DeckLibrary()`）。
//   ⚠️ 它自己建一棵 `DeckRuntime` 树，跑完 `DestroyImmediate` 拆掉 ⇒ **不碰宿主的 `_root` / `_rt`**。
using System;
using UnityEngine;
using RuleEngine;
using CardPresentation;

/// <summary>B99_W5 的全部断言（宿主：`Editor/DeckScene.cs` 的 `RunCore()` 里一行 `B99W5_Assert.Run(...)`）。</summary>
public static class B99W5_Assert
{
    /// <summary>跑一遍；返回失败条数（0 = 全绿）。</summary>
    public static int Run(Action<string, bool, string> check)
    {
        int bad = 0;
        if (check == null) return 0;
        Action<string, bool, string> C = (tag, ok, detail) => { check(tag, ok, detail); if (!ok) bad++; };

        // 前提 0：关掉卡牌进度落盘 —— 本文件只读库存、只摆内存值，⛔ 一个字节都不写盘。
        CardProgress.ResetForTest();

        var go = new GameObject("B99W5_DeckRuntime");
        try
        {
            var rt = go.AddComponent<DeckRuntime>();

            // ==========================================================================================
            //  第 1 次开窗（= 一次 `Build()`：生产路 `Shell/DeckEditingWindow.Open()` 每开一次就调一次）
            // ==========================================================================================
            rt.Build(new DeckLibrary());

            // 前提 1：计数条建出来了（否则扫光那几条全空转 —— 先红这一条，别让它们假绿）
            C("界面2·前提：第 1 次 Build 之后计数条在（`UiWcText(0) != null`）",
              rt.UiWcText(0) != null, "UiWcText(0)=" + (rt.UiWcText(0) ?? "null"));

            // A1：第 1 次开窗落在 Cards 页（出厂 `_tab == 0`；这条只挡「开窗顺手切到别的页」）
            C("界面1·★ 第 1 次开窗落在 **Cards 页**（`ActiveTab == 0`）",
              rt.ActiveTab == 0, "ActiveTab=" + rt.ActiveTab);

            // A2：`Signal.Register` 那半**无条件**（原版 `OnEnable` 第一段没有闸）
            C("界面2·★ 第 1 次开窗也注册了 `UpdateCounters`（原版 `Signal.Register` 无条件）",
              rt.WcSignalHookedForTest, "WcSignalHookedForTest=" + rt.WcSignalHookedForTest);

            // A3：第 1 次开窗**不播**（`currentArmy == 0` 那道闸）
            C("界面2·★ 第 1 次开窗**不播**扫光（`+0x50 == 0` ⇒ 层还没挂，`Rate == -1`）",
              !rt.WcShineEnabled && rt.WcShineRate < 0f,
              "WcShineEnabled=" + rt.WcShineEnabled + " · WcShineRate=" + rt.WcShineRate
              + "（应为 false / -1；true / 0 ⇒ 就是「无条件播」那一下多播）");

            // ---- 关窗之前先切到 Cosmetics 页（模拟「关窗时停在第 3 页」）----
            rt.UiSetTab(2);
            C("界面1·前提：`UiSetTab(2)` 之后 `ActiveTab == 2`（否则下面那条重置断言测不到东西）",
              rt.ActiveTab == 2, "ActiveTab=" + rt.ActiveTab);

            // ==========================================================================================
            //  第 2 次开窗 —— 界面的第 1 条主线
            // ==========================================================================================
            rt.Build(new DeckLibrary());

            C("界面2·前提：第 2 次 Build 之后计数条仍在（重建没把它丢了）",
              rt.UiWcText(0) != null, "UiWcText(0)=" + (rt.UiWcText(0) ?? "null"));

            // A2/a：字段回 0（原版 `TryOpen:123` 把那颗 `Toggle.isOn` 置 1）
            C("界面1·★ 第 2 次开窗**回到 Cards 页**（原版 `TryOpen:123` 置 tab0 `Toggle.isOn=1`）",
              rt.ActiveTab == 0, "ActiveTab=" + rt.ActiveTab + "（应为 0；2 ⇒ `Build()` 没重置 `_tab`）");

            // A2/b：**视图**也跟着回 0 —— 灭自证「只写了字段、没让刷新跑」
            C("界面1·★ ……而且页签高亮真的跟着换（tab0=1 / tab2=0；只写 `_tab` 不刷新 ⇒ 红）",
              Mathf.Abs(rt.UiTabHighlightAlpha(0) - 1f) < 1e-3f && Mathf.Abs(rt.UiTabHighlightAlpha(2) - 0f) < 1e-3f,
              "alpha0=" + rt.UiTabHighlightAlpha(0) + " · alpha2=" + rt.UiTabHighlightAlpha(2));

            // A2/c：第 2 次开窗**仍然不播**（判据③：窗每次新实例 ⇒ `+0x50` 不跨窗保留）
            C("界面2·★ 第 2 次开窗**仍然不播**（原版关窗即销毁 ⇒ 每次都是新实例，`+0x50` 恒 0）",
              !rt.WcShineEnabled && rt.WcShineRate < 0f,
              "WcShineEnabled=" + rt.WcShineEnabled + " · WcShineRate=" + rt.WcShineRate
              + "（应为 false / -1；true / 0 ⇒ 就是 W1 那条「第二次起播」）");

            // A2/d：Register 半照旧挂上（每窗都挂、幂等不重复挂）
            C("界面2·★ 第 2 次开窗仍然注册着 `UpdateCounters`（`HookWildcardUpdate` 幂等）",
              rt.WcSignalHookedForTest, "WcSignalHookedForTest=" + rt.WcSignalHookedForTest);

            // ==========================================================================================
            //  窗内换页那一拍 —— **还要播**（⛔ 别为了修上面那条把整条扫光砍掉）
            // ==========================================================================================
            rt.UiSetTab(2);      // Cosmetics：原版 `ToggleDisplay(0)` ⇒ 计数条 SetActive(false)
            rt.UiSetTab(0);      // 回 Cards：SetActive(true) ⇒ 这一次 `OnEnable` 时 `currentArmy != 0` ⇒ 播

            C("界面2·★ 窗内「Cosmetics → Cards」那一拍**要播**（`Play(resetTime:true)` ⇒ Rate 归 0、Enabled 真）",
              rt.WcShineEnabled && rt.WcShineRate >= 0f && rt.WcShineRate <= 1e-3f,
              "WcShineEnabled=" + rt.WcShineEnabled + " · WcShineRate=" + rt.WcShineRate
              + "（应为 true / 0；-1 ⇒ 扫光整条被砍了）");

            // ---- Hook 真挂在 `WildcardInventory.Updated` 上 ----
            WildcardShineLayer.StepAll(0.3f);                // 0.3 / 0.75 = 0.4
            float before = rt.WcShineRate;
            C("界面2·前提：推进 0.3 s 后 Rate 真的离开了 0（否则下一条分辨不出「重播」）",
              before > 0.2f, "before=" + before);

            Inventories.Wildcards.UpdateItemCollection();    // = 原版 `Signal.Raise(WildcardUpdateSignal)`
            float after = rt.WcShineRate;
            C("界面2·★ 库存更新 ⇒ 计数条**重播**一次（原版 `WildcardDisplay.UpdateCounters` ⇒ `Play`）",
              after >= 0f && after <= 1e-3f, "before=" + before + " · after=" + after);
        }
        finally
        {
            // ⚠️ 拆掉自己那棵树（`OnDestroy` 会 `UnhookWildcardUpdate()`，不把静态事件留给下一个用例）
            UnityEngine.Object.DestroyImmediate(go);
        }

        return bad;
    }
}
