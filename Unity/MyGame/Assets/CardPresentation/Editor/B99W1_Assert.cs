// B99W1_Assert.cs — 执行代理 `B99_W1`（第九十九会话 · 波 2 · 界面 A）的断言集
//
// ============================ 它盯的是什么（两件）============================
//   ① 界面 #2 · **Deck 开窗扫光**：原版 `WildcardDisplay__OnEnable.c` 是「`Signal.Register` **无条件** +
//      `if (*(int*)(this+0x50) != 0) { Play; SetCountersText; }`」，而 `DeckEditingWindow__TryOpen.c`
//      把 `ToggleDisplay(1)`（⇒ OnEnable，**:125**）排在 `FilterToDeck`（⇒ `WildcardDisplay__Initialize`，**:133**）
      // **之前** ⇒ **第一次开窗 `currentArmy` 还是出厂 0 ⇒ 不播**，第二次及以后才播。
//      旧任务「开窗就播」只对了一半 —— 照它做会多出**原版没有的第一次那一下**。
//   ② 界面 #5 · **四格通配符**：原版 `WildcardDisplay__SetCountersText.c` 那 4 个数字是**该阵营 4 档稀有度的库存**
//      （谓词 `WildcardDisplay__<SetCountersText>b__11_0.c`：`wildcard.army(+0x2c) == this.currentArmy(+0x50)`），
//      我们原来写死 `"99"`。等价读口 = `CardProgress.Wildcards(army, rarity)`。
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B99_R3b_施工单_其余按文件.md` §2·2 / §2·5
//                            ＋ 本代理 `资料/完全复刻/报告/B99_W1.md`
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · 界面 2 · A1（首次不播）：把 `WildcardDisplayEnable()` 改回「无条件播」（= 旧任务的写法）
//         ⇒ 首次开窗那一刻就 `Play` ⇒ `WcShineRate == 0 && WcShineEnabled == true` ⇒ A1 红。
//   · 界面 2 · A2（第二次播）：把它改成「永不播」（= 改之前的状态，`Build()` 里根本没调它）
//         ⇒ 第二次开窗仍是 `Rate == -1 / Enabled == false` ⇒ A2 红。
//         **A1 ∧ A2 结构上不可能被同一个固定实现同时满足** ⇒ 「恒不播」「恒播」两种错法各被一条抓住。
//   · 界面 2 · A3（Register 无条件）：旧代码首次开窗根本不挂 Signal ⇒ `WcSignalHookedForTest == false` ⇒ 红。
//   · 界面 2 · A4（Hook 真起作用）：退订掉 Signal（把 `HookWildcardUpdate()` 从首次那支删掉）⇒ 注入变动后
//         `Rate` **不回到 0**（停在 0.4）⇒ 红。
//   · 界面 5 · A5：把这 4 格改回常量 `"99"` ⇒ 注入 7 / 0 之后仍印 `"99"` ⇒ 红。
//
// ============================ 怎么用（宿主）============================
//   `Assets/CardPresentation/Editor/DeckScene.cs` 的 `RunCore()` 里，「版面」那一节
//   `state = Build(DeckLibrary.Load(), out _root);` **之后**（⚠️ 必须在那之后：那时
//   `RuleEngine.DeckStore.OverridePath` 已指向临时存档、场景里也已经有相机）插一行：
//
//       B99W1Assert.Run((tag, ok, detail) => CheckTrue(ok, tag + " —— " + detail));   // B99_W1：界面 2 + 界面 5
//
//   ⚠️ **本批白名单不含宿主**（`Editor/DeckScene.cs` 归调度台）⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//
//   ⚠️ 本文件**不落盘**：进门前先 `CardProgress.ResetForTest()`（关掉卡牌进度落盘，⛔ 绝不改玩家存档 ——
//      与 `CardFaceAssertE5.cs` / `BattleScene.cs` 的既有做法同一口径），卡组库用**内存里的空库**（`new DeckLibrary()`）。
//   ⚠️ 它自己建一棵 `DeckRuntime` 树（与 `DeckScene.Build` 逐句同形的三行），跑完 `DestroyImmediate` 拆掉
//      ⇒ **不碰宿主的 `_root` / `_rt`**。
using System;
using UnityEngine;
using RuleEngine;
using CardPresentation;

/// <summary>B99_W1 的全部断言（宿主：`Editor/DeckScene.cs` 的 `RunCore()` 里一行 `B99W1Assert.Run(...)`）。</summary>
public static class B99W1Assert
{
    /// <summary>跑一遍；返回失败条数（0 = 全绿）。</summary>
    public static int Run(Action<string, bool, string> check)
    {
        int bad = 0;
        if (check == null) return 0;
        Action<string, bool, string> C = (tag, ok, detail) => { check(tag, ok, detail); if (!ok) bad++; };

        // 前提 0：关掉卡牌进度落盘（`_persist = false`）—— 本文件只读库存、只摆内存值，⛔ 一个字节都不写盘。
        CardProgress.ResetForTest();

        var go = new GameObject("B99W1_DeckRuntime");
        try
        {
            var rt = go.AddComponent<DeckRuntime>();

            // ==========================================================================================
            //  界面 #2 · 开窗扫光（`DeckRuntime.WildcardDisplayEnable`）
            //  ⚠️ 「一次开窗」= 一次 `Build()`：生产路 `Shell/DeckEditingWindow.Open()` **每开一次就调一次 `Build()`**
            //     （那句 doc 自己写着「同窗再开会重建、不叠」）⇒ 在这里 `Build` 两趟就是「开两次窗」。
            // ==========================================================================================

            // ---------- 第 1 次开窗 ----------
            rt.Build(new DeckLibrary());
            C("界面2·前提：第 1 次 Build 真的把计数条建出来了（否则下面三条全空转）",
              rt.UiWcText(0) != null,
              "UiWcText(0)=" + (rt.UiWcText(0) ?? "null"));

            // A3：`Signal.Register` 那半是**无条件**的（原版 `OnEnable` 第一段没有闸）
            C("界面2·★ 首次开窗也注册了 UpdateCounters（原版 `Signal.Register` 无条件）",
              rt.WcSignalHookedForTest,
              "WcSignalHookedForTest=" + rt.WcSignalHookedForTest);

            // A1：首次开窗**不播**（原版 `*(int*)(this+0x50) == 0` ⇒ `Play` 那一段整块跳过）
            C("界面2·★ 首次开窗**不播**扫光（原版 `currentArmy == 0` 那道闸）",
              !rt.WcShineEnabled && rt.WcShineRate < 0f,
              "WcShineEnabled=" + rt.WcShineEnabled + " · WcShineRate=" + rt.WcShineRate
              + "（应为 false / -1；若为 true / 0 ⇒ 就是旧任务那一下**多播**）");

            // ---------- 第 2 次开窗 ----------
            rt.Build(new DeckLibrary());
            C("界面2·前提：第 2 次 Build 之后计数条仍在（重建没把它丢了）",
              rt.UiWcText(0) != null, "UiWcText(0)=" + (rt.UiWcText(0) ?? "null"));

            // 🔴 2026-10-30（`B99_W5` 现核推翻，本两条已删）：原版关窗是 **addressable 销毁**
            //   （`WindowsManager__CloseWindowCO.c:36-47` 命中 `automaticallyLoadedWindows` ⇒ `Object.Destroy` + `Release` + `Remove`）
            //   ⇒ `WildcardDisplay.currentArmy(+0x50)` **不跨开窗保留** ⇒ 开窗那一拍闸**恒关**。
            //   ⇒ 原来的 A2「第二次开窗播」/ A4「库存更新重播」**不成立，删掉**；改由 `B99_W5_Assert` 的
            //   「两次开窗都不播」+「窗内切页签要播」两条对钉。

            // ==========================================================================================
            //  界面 #5 · 四格通配符（`DeckRuntime.RefreshHeader` 里那 4 句 `SetText`）
            // ==========================================================================================

            string army = DeckRuntime.DeckArmyName(rt.State);
            C("界面5·前提：拿得到这副卡组的阵营名（空 ⇒ 下面几条全空转）",
              !string.IsNullOrEmpty(army), "army=" + (army ?? "null"));

            // ⚠️ 这里**另写一份**四档名（生产侧用的是 `CardDetailPopup.WildcardRarities`，那是 `internal`、
            //   编辑器程序集看不见）。**这不是「两处写同一条规则」**：下面的 A5① 逐格拿「本表」去和生产侧
            //   真印出来的字比 ⇒ **两边档序一旦不一致就红**（等于用断言把两处钉在一起）。
            string[] Rarities = { "common", "rare", "epic", "legendary" };

            // A5①：四格 == 真库存读口（出厂 = `WildcardsPerRarity` = 99）
            bool eq = true; string why = "";
            for (int i = 0; i < 4; i++)
            {
                int want = CardProgress.Wildcards(army, Rarities[i]);
                string got = rt.UiWcText(i);
                if (got != want.ToString()) { eq = false; why += " 格" + i + "(" + Rarities[i] + ") 期望 " + want + " 实得 " + got + "；"; }
            }
            C("界面5·★ 四格数字 == `CardProgress.Wildcards(army, rarity)`（出厂 99/档；档序不对这条就会红）", eq, why);

            // A5②：注入 (army, rare) = 7 ⇒ 第 2 格印 "7"（改回常量 "99" 这条就红）
            CardProgress.SetWildcardsForTest(CardProgress.WildKey(army, "rare"), 7);
            rt.UiSetTab(0);                              // 走 `SetTab` ⇒ `RefreshHeader`（生产路同一个刷新口）
            C("界面5·★ 注入 (army,rare)=7 ⇒ 第 2 格印 \"7\"",
              rt.UiWcText(1) == "7", "UiWcText(1)=" + (rt.UiWcText(1) ?? "null"));

            // A5③：扣到 0 ⇒ 印 "0"（**0 也要显示**，同「阵营资源 0 值也显示」那条口径）
            CardProgress.SetWildcardsForTest(CardProgress.WildKey(army, "rare"), 0);
            rt.UiSetTab(0);
            C("界面5·★ 扣到 0 ⇒ 第 2 格印 \"0\"（不是空、不是 99）",
              rt.UiWcText(1) == "0", "UiWcText(1)=" + (rt.UiWcText(1) ?? "null"));

            // A5④：同一拍其余三格仍是 99（证明改动是**按档**生效，不是整排一起被换掉）
            C("界面5·其余三格不受影响（仍是 99）",
              rt.UiWcText(0) == "99" && rt.UiWcText(2) == "99" && rt.UiWcText(3) == "99",
              "格0=" + rt.UiWcText(0) + " · 格2=" + rt.UiWcText(2) + " · 格3=" + rt.UiWcText(3));
        }
        finally
        {
            // ⚠️ 拆掉自己那棵树（`OnDestroy` 会 `UnhookWildcardUpdate()`，不把静态事件留给下一个用例）
            UnityEngine.Object.DestroyImmediate(go);
        }

        return bad;
    }
}
