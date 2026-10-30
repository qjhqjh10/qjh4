// B104_A2_Assert.cs — 第一百零四会话 · 执行代理 `B104_A2`（2026-10-31 · 特效线 A2 九条待接线）
//
// ============================ 它盯的是什么 ============================
// A2 的「9 条待接线」= 8 件 `Wildcard_Use_*` + `Swarm Card Merge VFX`。
//
// 🔴 回源结论（2026-10-31 现读，**推翻了台账 `W65·A2` 那半句「→ CollectionWindow 万能卡使用」**）：
//   1. **8 件 `Wildcard_Use_*` 的原版引用者 = `CraftingPanel.craftVFXs[]`**（不是 `WildcardDisplay` /
//      不是 `CollectionWindow`）。判据：`d:/2/tools/decomp_full/CraftingPanel__DoVFX.c` 逐句 +
//      `CraftingPanel__TryCraftingInternal.c:51`（发出合成请求后**同一拍**调 `DoVFX()`）+
//      场景 `MainMenu Warpforge.unity:81172-81190` 的 `craftVFXs[]`（含第 5 行 rarity 5 的原值）。
//      我们的等价物 = `CollectionWindow.OpenCardDetail` → `CardDetailPopup`（菜单版 `CardDisplayWindow`）
//      → `CraftHit` → `OnCraftClicked` → `TryCraft`（`DoCraft` 真扣通配符 + owned+1）→ `PlayCraftVfx`
//      → `WarpforgeUiEffect.PlayWildcardCraft` 两件（按钮 + 前台卡）。
//      ✅ **这条链在 HEAD 上已经接好**（`CardDetailPopup.cs` 的 `CraftNow` / `PlayCraftVfx`；
//         行为级断言早已在 `Editor/CollectionScene.cs` 的「四档逐档走」里 —— 真按钮 `chb.Click()`、
//         真扣减、真两件、反向「没通配符一件都不许播」）。
//   2. **`Swarm Card Merge VFX` 的原版引用者 = `CardTrait "Swarm"(650).customObject`**，在
//      `CardScript__ExecuteSwarm.c:39-79` 实例化；我们的消费点 = `BattleDriver.PlaySwarmMergeFeel`
//      （`EvtKind.Trigger` + `keyword == swarm`），含 3D 音 cue 与材质交接。
//      ✅ **这条在 HEAD 上也已经接好**（`BattleDriver.cs` 的 `case EvtKind.Trigger: … PlaySwarmMergeFeel(e)`；
//         行为级断言在 `Editor/BattleScene.cs` 的「虫群合并①~⑤」）。
//   3. 🔴 **仍有一处没接，且现有实现时机是错的**（详见报告 `资料/完全复刻/报告/B104_A2.md` §三）：
//      原版的 `CardView.SetSwarm(true)` 对应 `CardScript.DisplaySwarm` → `SwarmIconController.Toggle(on)`，
//      它的**唯一生产调用点**是 `BattleManager__Update.c:608` —— **拖一张带 `swarm` 的牌、指针悬停在
//      「同名卡」那一格时的拖拽预览高亮**（`case 2`；配套 `MinionManager__RemoveSwarm` 清掉其余 `:612`）。
//      松开 / 取消时一律清掉：`case 3` 成功那一支 `:418` `RemoveSwarm(mm, 0)`、取消支 `:208/:303`、
//      离开落区 `:620` ⇒ **图标只在拖动中亮、松手即灭**。
//      而 `CardScript__ExecuteSwarm.c` 全文**没有** `DisplaySwarm`：合并那一拍只做 VFX + 材质 + 3D 音
//      + `ActivateTraitParticlesFromCode(650)`（`…ExecuteSwarm_b__0.c:47-55`）。⇒ 我们目前在
//      `PlaySwarmMergeFeel` 里点亮图标的**时机**与原版不同（原版是拖拽预览、松手即清；我们等合并结算
//      之后点亮并**一直留着** —— 只有换回手牌形态才关），且**拖拽预览那一支完全没做**。
//      ✅ **2026-10-31（本代理第二批，调度台授权）已落地 P1/P2**：`BattleDriver.OnDropPreview` →
//      `RefreshSwarmPreview()`（拖拽预览亮 / 收工全灭）+ `PlaySwarmMergeFeel` 的 `v.SetSwarm(true)` 已删。
//      自检口 `SimulateSwarmPreviewForTest(dragged, mine, slot)` 走**产品同一个函数**。
//
// ============================ 怎么跑 ============================
//   · 静态组：`B104_A2_Assert.Run();` —— 不需要开局/UI/prefab，纯常量 + 源码静态接线普查。
//   · 行为组：`B104_A2_Assert.RunBehavior(driver);` —— 需要一台活着的 `BattleDriver`（`BattleScene` 宿主），
//     且本方右半场有带视图单位；它**不重置** `Passed/Failed`（可与 `Run()` 累加），收尾自己清干净。
//   判绿 = 末尾 `合计` 那行 `失败 0`。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · `WarpforgeUiEffect` 的 `craftVFXs` 表被「顺手改回去」（例如 rarity 5 的 Card 改成 `5 Legendary`）
//     ⇒ `W1/W2` 红（期望是字面量，⛔ 不从被测表反算）。
//   · `CardDetailPopup` 把 `PlayCraftVfx(rarity);` 从 `CraftNow` 删掉 ⇒ `W7` 红。
//   · `PlayCraftVfx` 少播一件（只留按钮那件）⇒ `W8` 红。
//   · `CollectionWindow.OpenCardDetail` 不再 `CardDetailPopup.Create` / 不 `ShowCard` ⇒ `W5` 红。
//   · `BattleDriver` 的 `case EvtKind.Trigger:` 删掉 `PlaySwarmMergeFeel(e)` ⇒ `S3` 红。
//   · `PlaySwarmMergeFeel` 删掉 `WarpforgeEffectPlayer.Play(SwarmMergeVfxName, …)` ⇒ `S4` 红。
//   · `RuleCore.SwarmMergeNow` 删掉 `FireTriggerAlways(… KeywordTable.Swarm …)` ⇒ `S5` 红
//     （那条是「事件到底会不会发」的另一跳，直调产品的行为断言覆盖不到）。
//   · `CardView.SetSwarm(bool)` 被改名/删掉，或 prefab/音 cue 常量被改 ⇒ `S1/S2` 红。
//   · 把 `v.SetSwarm(true)` 加回 `PlaySwarmMergeFeel` ⇒ `S6/B5` 红（合并拍不许亮图标）。
//   · `OnDropPreview` 删掉 `RefreshSwarmPreview()`（或收工支不清）⇒ `B1/B3/B4` 红。
//   · 预览判据删掉「同名」那半句 ⇒ `B2` 红。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;      // CardView / BattleDriver
using RuleEngine;            // KeywordTable
using UnityEngine;

/// <summary>执行代理 `B104_A2`：A2 九条待接线的**链路断言**（8 件 `Wildcard_Use_*` + 虫群合并）。
/// <para>⛔ 期望值全是**原版判据里的字面量**（`A31_特效接线.md` §2·1 / `CardScript__ExecuteSwarm.c`），
/// 不从被测实现里反算 —— 否则「两边一起改回去」也会绿。</para>
/// <para>⚠️ 本件只覆盖「常量对不对 + 生产调用链在不在」。拖拽预览那一支**没接**，报告 §三/§四 如实记着，
/// 这里**不假装盖住了它**。</para></summary>
public static class B104_A2_Assert
{
    const string P = "BT B104 A2 ";

    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    static void Check(bool cond, string label)
    {
        if (cond) Passed++; else Failed++;
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        Passed = 0; Failed = 0;
        Debug.Log(P + "=== 特效线 A2：8 件 Wildcard_Use_* + Swarm Card Merge VFX 的链路断言 ===");
        Wildcard();
        Swarm();
        Debug.Log(P + $"=== 合计：通过 {Passed} / 失败 {Failed} ===");
    }

    // ================================================================== 8 件 `Wildcard_Use_*`
    //
    // 原版表（`craftVFXs[]`，场景 `MainMenu Warpforge.unity:81172-81190`，A31 §2·1 第 4 条）：
    //   rarity 1..4 → Button/Card 各一件；**rarity 5 = Button 复用 4 + Card 回落 1**（原版原值，⛔ 别当笔误）。

    static void Wildcard()
    {
        // ---- W1 · 1..4 逐档：按钮列 + 卡列（字面量，来自原版场景序列化）----
        string[] expBtn =
        {
            "Wildcard_Use_Button 1 Common", "Wildcard_Use_Button 2 Rare",
            "Wildcard_Use_Button 3 Epic",   "Wildcard_Use_Button 4 Legendary",
        };
        string[] expCard =
        {
            "Wildcard_Use_Card 1 Common", "Wildcard_Use_Card 2 Rare",
            "Wildcard_Use_Card 3 Epic",   "Wildcard_Use_Card 4 Legendary",
        };
        int okBtn = 0, okCard = 0;
        for (int i = 0; i < 4; i++)
        {
            if (WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(i + 1, true) == expBtn[i]) okBtn++;
            if (WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(i + 1, false) == expCard[i]) okCard++;
        }
        Check(okBtn == 4,
              "W1 8 件表·按钮列：rarity 1..4 = `Wildcard_Use_Button 1 Common … 4 Legendary`"
            + $"（逐档命中 {okBtn}/4）｜🧨 改表里的名字 / 把 rarity 映射串档 ⇒ 红");
        Check(okCard == 4,
              "W1 8 件表·卡列：rarity 1..4 = `Wildcard_Use_Card 1 Common … 4 Legendary`"
            + $"（逐档命中 {okCard}/4）｜🧨 同上 ⇒ 红");

        // ---- W2 · rarity 5：**原版原值**（Button 复用 4 / Card 回落 1）----
        Check(WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(5, true) == expBtn[3],
              "W2 rarity 5·按钮 = **复用 4 Legendary**（原版 `craftVFXs` 第 5 行原值，⛔ 不是笔误）"
            + "｜🧨 「修成」5 Legendary ⇒ 红");
        Check(WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(5, false) == expCard[0],
              "W2 rarity 5·卡 = **回落 1 Common**（原版 `craftVFXs` 第 5 行原值）"
            + "｜🧨 改成别的档 ⇒ 红");

        // ---- W3 · 越界 = null（不许回落成某一档：那会把「没这档素材」静默成「播了别的」）----
        Check(WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(0, true) == null
              && WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(6, false) == null,
              "W3 rarity 0 / 6 ⇒ **null**（不回落；与 `WildcardCraftPrefab` 的 doc 一致）");

        // ---- W4 · 前提：两份源码读得到（读不到时下面的接线普查等于没验 ⇒ 当场红，⛔ 不静默跳过）----
        string[] colSrc = ReadSource("CardPresentation/Shell/CollectionWindow.cs", out string colPath);
        string[] cdpSrc = ReadSource("CardPresentation/Shell/CardDetailPopup.cs", out string cdpPath);
        Check(colSrc != null && colSrc.Length > 0,
              "W4 （前提）读得到 `CollectionWindow.cs`（" + colPath + "）—— 读不到 = 下面 W5 没验");
        Check(cdpSrc != null && cdpSrc.Length > 0,
              "W4 （前提）读得到 `CardDetailPopup.cs`（" + cdpPath + "）—— 读不到 = 下面 W6/W7/W8 没验");

        // ---- W5 · 真入口：`CollectionWindow.OpenCardDetail` 建菜单版详情窗并把卡交给它 ----
        int openAt = IndexOf(colSrc, "public CardDetailPopup OpenCardDetail(CardDef card)");
        Check(openAt >= 0,
              "W5 （前提）`CollectionWindow.OpenCardDetail(CardDef)` 还在（卡池格 / 异画格的点击入口）");
        int w5From = Math.Max(0, openAt);
        int createAt = IndexOf(colSrc, "CardDetailPopup.Create(", w5From, w5From + 60);
        int showAt = IndexOf(colSrc, "w.ShowCard(card)", w5From, w5From + 60);
        Check(openAt >= 0 && createAt >= 0 && showAt >= 0,
              "W5 真入口链：`OpenCardDetail` ⇒ `CardDetailPopup.Create(Manager)` + `w.ShowCard(card)`"
            + "（= 原版 `CardDisplayWindow.Instance.ShowCard(card,…)`）"
            + "｜🧨 换成别的窗 / 不把卡交进去 ⇒ 红");

        // ---- W6 · 真按钮：`CraftHit` 的命回调就是 `OnCraftClicked()`（不是死路）----
        // ⚠️ 第一处 `"CraftHit"` 是**属性取口**（`public Transform CraftHit { get { return Find(transform, "CraftHit"); } }`）
        //    —— 它**没有**回调。⇒ 必须先找到**真正的 `Hit(…)` 调用行**（同一行里既有 `"CraftHit"` 又有 `Hit(`），
        //    再**只在那条语句（到第一个分号为止）**里找回调。⛔ 不接受「文件里别处出现过 `OnCraftClicked()`」。
        int hitCallAt = -1;
        if (cdpSrc != null)
        {
            for (int i = 0; i < cdpSrc.Length; i++)
            {
                string t = cdpSrc[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (t.IndexOf("\"CraftHit\"", StringComparison.Ordinal) < 0) continue;
                if (t.IndexOf("Hit(", StringComparison.Ordinal) < 0) continue;   // 排除 `Find(transform, "CraftHit")` 那个属性口
                hitCallAt = i; break;
            }
        }
        Check(hitCallAt >= 0,
              "W6 （前提）`CardDetailPopup` 里有 **`Hit(…, \"CraftHit\", …)` 调用行**（不是只有那个同名属性取口）");
        int onCraftInCall = -1;
        if (hitCallAt >= 0)
        {
            // 只取这次调用的语句：从调用行起、到**第一个分号**为止（最多 8 行，够容下换行参数）。
            var callText = new System.Text.StringBuilder();
            for (int i = hitCallAt; i < cdpSrc.Length && i < hitCallAt + 8; i++)
            {
                string t = cdpSrc[i];
                int semi = t.IndexOf(';');
                bool ends = semi >= 0;
                callText.Append(ends ? t.Substring(0, semi + 1) : t).Append('\n');
                if (ends) break;
            }
            string call = callText.ToString();
            // 认「回调里真的调 `OnCraftClicked()`」——两种等价写法都认，但**必须在这条语句内**。
            if (call.IndexOf("() => { OnCraftClicked(); }", StringComparison.Ordinal) >= 0
                || call.IndexOf("OnCraftClicked();", StringComparison.Ordinal) >= 0)
                onCraftInCall = hitCallAt;
        }
        Check(hitCallAt >= 0 && onCraftInCall >= 0,
              "W6 真按钮链：`Hit(…, \"CraftHit\", …, () => { OnCraftClicked(); }, …)`"
            + " —— 点「创建副本」真的走到合成入口（**只认这次 `Hit(...)` 调用语句内**的回调）"
            + "｜🧨 换回空实现 / 换成别的回调 / 只把 `OnCraftClicked()` 留在别处 ⇒ 红");

        // ---- W7 · 请求 → 特效：`CraftNow` 里「先请求、成功后同一拍播」----
        int tryAt = IndexOf(cdpSrc, "if (!TryCraft(out rarity)) return;");
        Check(tryAt >= 0,
              "W7 请求链：`CraftNow` 先 `TryCraft(out rarity)`，失败直接 return（原版失败不调 `DoVFX`）");
        int w7From = Math.Max(0, tryAt);
        int playAt = IndexOf(cdpSrc, "PlayCraftVfx(rarity);", w7From, w7From + 8);
        Check(tryAt >= 0 && playAt >= 0,
              "W7 请求链：成功**同一拍**调 `PlayCraftVfx(rarity)`（= 原版 `TryCraftingInternal.c:51`）"
            + "｜🧨 删掉这一行 ⇒ 请求成了但永不出特效 ⇒ 红");

        // ---- W8 · 两件都播：`PlayCraftVfx` 里 Button + Card 各一次 ----
        int playDefAt = IndexOf(cdpSrc, "void PlayCraftVfx(int rarity)");
        Check(playDefAt >= 0,
              "W8 （前提）`CardDetailPopup.PlayCraftVfx(int)` 还在");
        int w8From = Math.Max(0, playDefAt);
        int bodyEnd = Math.Min(cdpSrc != null ? cdpSrc.Length : 0, w8From + 80);
        int one = IndexOf(cdpSrc, "PlayWildcardCraft(rarity, true", w8From, bodyEnd);
        int two = IndexOf(cdpSrc, "PlayWildcardCraft(rarity, false", w8From, bodyEnd);
        Check(playDefAt >= 0 && one >= 0 && two >= 0,
              "W8 两件：`PlayCraftVfx` 里 `PlayWildcardCraft(rarity, true…)`（按钮）+ `(…, false…)`（前台卡）"
            + "各一次（= 原版 `DoVFX` 两次 `Instantiate`）"
            + "｜🧨 只留一件 / 两件都叫同一个 onButton ⇒ 红");
    }

    // ================================================================== `Swarm Card Merge VFX` + 虫群图标
    //
    // 判据（2026-10-31 现读）：
    //   · 合并件：`CardScript__ExecuteSwarm.c:39-79`（`CardTrait 650.customObject`）—— 合并那一拍播；
    //   · 3D 音：cue `Trait Swarm Tyranid`（`SwarmVFXController.Initialize` 的 `Play3D`）；
    //   · 图标：`CardScript__DisplaySwarm` → `SwarmIconController.Toggle(on)` —— 唯一生产调用点是
    //     `BattleManager__Update.c:608` 的**拖拽预览**（见文件头 §3；那条**没接**，本件不断言它）。

    static void Swarm()
    {
        // ---- S1 · 常量逐值对原版 ----
        Check(CardView.SwarmPrefabName == "Swarm Battle Card Icon",
              "S1 图标 prefab 名 = `Swarm Battle Card Icon`（原版 `CardScript.swarmObject`）"
            + "｜🧨 改名 ⇒ 效果库取不到 ⇒ 红");
        Check(CardView.SwarmClipName == "SwarmIconOn",
              "S1 图标 clip 名 = `SwarmIconOn`（原版同名 legacy clip）");
        Check(CardView.SwarmOnCue == "Swarm Trait UI Enabled",
              "S1 图标开音 cue = `Swarm Trait UI Enabled`（原版 `toggleOnSound`；原版**没有关音**）");
        Check(Math.Abs(CardView.SwarmOffFadeTime - 0.3f) < 1e-6f,
              "S1 图标收起时长 = 0.3s（原版 `SwarmIconController.offAnimationTime`）");
        Check(Math.Abs(CardView.SwarmFadeInTime - 0.35f) < 1e-6f,
              "S1 图标淡入时长 = 0.35s（clip `SwarmIconOn` 里 `m_Color.a` 那两条关键帧 t=0/0.35）");
        Check(BattleDriver.SwarmMergeVfxName == "Swarm Card Merge VFX",
              "S1 合并件名 = `Swarm Card Merge VFX`（原版 `CardTrait \"Swarm\"(650).customObject`）");
        Check(BattleDriver.SwarmMergeCue == "Trait Swarm Tyranid",
              "S1 合并 3D 音 cue = `Trait Swarm Tyranid`（原版 `Trait Swarm Tyranid.asset`）");
        Check(KeywordTable.Swarm == "swarm",
              "S1 关键词常量 `KeywordTable.Swarm` = `swarm`（引擎 `EvtKind.Trigger` 的过滤键）");

        // ---- S2 · `CardView.SetSwarm(bool)` 的方法本体还在（图标三支的落点）----
        var setSwarm = typeof(CardView).GetMethod("SetSwarm", new[] { typeof(bool) });
        Check(setSwarm != null,
              "S2 `CardView.SetSwarm(bool)` 还在（原版 `SwarmIconController.Toggle` 三支的等价口）"
            + "｜🧨 改名/删掉 ⇒ 红");

        // ---- S3~S5 · 静态接线普查（源码现读；⚠️ 只认非注释行）----
        string[] drvSrc = ReadSource("CardPresentation/Battle/BattleDriver.cs", out string drvPath);
        string[] coreSrc = ReadSource("RuleEngine/Core/RuleCore.cs", out string corePath);
        Check(drvSrc != null && drvSrc.Length > 0,
              "S3 （前提）读得到 `BattleDriver.cs`（" + drvPath + "）");
        Check(coreSrc != null && coreSrc.Length > 0,
              "S3 （前提）读得到 `RuleCore.cs`（" + corePath + "）");

        // S3：事件分发那一行必须真的调了 PlaySwarmMergeFeel（「事件来不来」那一跳，直调覆盖不到）
        //      ⚠️ `BattleDriver` 里 `case EvtKind.Trigger:` 有 4 处（别的 switch 也有）⇒ **逐行找
        //      「同一行里既含 case 又含 PlaySwarmMergeFeel(」**，⛔ 不能用「第一个 case」。
        int caseAt = IndexOf(drvSrc, "case EvtKind.Trigger:");
        Check(caseAt >= 0,
              "S3 （前提）`BattleDriver` 里有 `case EvtKind.Trigger:` 那一支");
        bool wired = false;
        if (drvSrc != null)
        {
            for (int i = 0; i < drvSrc.Length; i++)
            {
                string t = drvSrc[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal)) continue;
                if (t.Contains("case EvtKind.Trigger:") && t.Contains("PlaySwarmMergeFeel(")) { wired = true; break; }
            }
        }
        Check(wired,
              "S3 事件链：`case EvtKind.Trigger:` 那一行**真的**调了 `PlaySwarmMergeFeel(e)`"
            + " ⇒ 引擎那条 `Trigger(keyword=swarm)` 才到得了表现层"
            + "｜🧨 删掉那句（或挪到别的事件分支）⇒ 红");

        // S4：合并那一拍真的播了件名 + 3D 音（函数体内，非注释行）
        int feelAt = IndexOf(drvSrc, "void PlaySwarmMergeFeel(BattleEvent e)");
        Check(feelAt >= 0,
              "S4 （前提）`BattleDriver.PlaySwarmMergeFeel(BattleEvent)` 还在");
        int s4From = Math.Max(0, feelAt);
        int end = Math.Min(drvSrc != null ? drvSrc.Length : 0, s4From + 90);
        int vfxAt = IndexOf(drvSrc, "WarpforgeEffectPlayer.Play(SwarmMergeVfxName", s4From, end);
        int cueAt = IndexOf(drvSrc, "WFSoundBank.TryGetCue(SwarmMergeCue", s4From, end);
        Check(feelAt >= 0 && vfxAt >= 0,
              "S4 合并件：函数体里**真的** `WarpforgeEffectPlayer.Play(SwarmMergeVfxName, v.transform, …)`"
            + "（原版在被并掉那张的世界位姿实例化；⚠️ 我们挂存活那格是 A31 §5·3 已记的近似）"
            + "｜🧨 删掉 ⇒ 红");
        Check(feelAt >= 0 && cueAt >= 0,
              "S4 3D 音：函数体里**真的**走 `WFSoundBank.TryGetCue(SwarmMergeCue, …)` + `WFSoundPlayer.Play(…, is2d:false)`"
            + "（该 prefab 没有 `AnimFXController`，效果库不会替它播）"
            + "｜🧨 删掉 ⇒ 合并静默无声 ⇒ 红");
        // ⚠️ 这一条是**反向闸**：合并那一拍**不许**点亮图标。原版 `DisplaySwarm` 只在拖拽预览
        //    （`BattleManager__Update.c:583-612`），且松手那一拍 `:418` 就 `RemoveSwarm(mm, 0)` 清掉；
        //    `CardScript__ExecuteSwarm.c` 全文没有它 ⇒ 合并后不存在常驻图标（`B104_A2` 报告 §2·3）。
        //    🧨 把 `v.SetSwarm(true)` 加回 `PlaySwarmMergeFeel` ⇒ 红（这是「自检绿 ≠ 口径对」那条先例）。
        int iconAtMerge = IndexOf(drvSrc, "SetSwarm(true)", s4From, end);
        Check(feelAt >= 0 && iconAtMerge < 0,
              "S6 合并拍**不亮**虫群图标：`PlaySwarmMergeFeel` 函数体里**没有** `SetSwarm(true)`"
            + "（原版 `ExecuteSwarm` 全文无 `DisplaySwarm`；图标只服务拖拽预览）"
            + "｜🧨 把那句加回去 ⇒ 红");

        // S5：引擎那一跳真的发了 `EvtKind.Trigger(keyword=swarm)`（= `FireTriggerAlways`）
        int fireAt = IndexOf(coreSrc, "FireTriggerAlways(ctx, host, KeywordTable.Swarm");
        Check(fireAt >= 0,
              "S5 引擎事件：`RuleCore.SwarmMergeNow` 尾部 `FireTriggerAlways(ctx, host, KeywordTable.Swarm, …)`"
            + "（原版 `BroadcastUnitSwarm` 那一跳；⛔ 别改成只广播不 Event）"
            + "｜🧨 删掉 ⇒ 表现层永远收不到 ⇒ 红");
    }

    // ================================================================== 源码读取辅助
    //
    // ⚠️ 与 `BattleScene.cs` 里那条「静态接线普查」同一套做法（`Application.dataPath` 下现读源码）。
    //    只跳过**整行 `//` 注释**（本仓红线提醒过：注释里的示例键会造出假阳性）；
    //    块注释 `/* */` 在目标文件里不被用来包真代码，这里不额外处理。

    static string[] ReadSource(string relUnderAssets, out string abs)
    {
        abs = Path.Combine(Application.dataPath, relUnderAssets);
        try
        {
            if (!File.Exists(abs)) return null;
            return File.ReadAllLines(abs);
        }
        catch (Exception e)
        {
            Debug.LogWarning(P + "读源码抛异常：" + abs + " —— " + e.Message);
            return null;
        }
    }

    static int IndexOf(string[] lines, string needle, int from = 0, int toExclusive = -1)
    {
        if (lines == null || string.IsNullOrEmpty(needle)) return -1;
        if (toExclusive < 0 || toExclusive > lines.Length) toExclusive = lines.Length;
        for (int i = Math.Max(0, from); i < toExclusive; i++)
        {
            string t = lines[i].TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            if (t.IndexOf(needle, StringComparison.Ordinal) >= 0) return i;
        }
        return -1;
    }

    // ================================================================== 行为断言（需要一台活着的 `BattleDriver`）
    //
    // 判据 = `BattleManager__Update.c:583-612`（拖拽预览亮）+ `:418/:303/:208/:620`（松手/取消清）
    //      + `CardScript__ExecuteSwarm.c`（合并拍无 `DisplaySwarm`）。
    // 产品同一个函数：`BattleDriver.SimulateSwarmPreviewForTest` → `OnDropPreview` → `RefreshSwarmPreview`。
    // ⚠️ **不重置** `Passed/Failed`（可与 `Run()` 累加）—— 宿主一次读完即可。
    public static void RunBehavior(BattleDriver driver)
    {
        Debug.Log(P + "=== A2 行为：虫群图标 = 拖拽预览亮 / 反例灭 / 松手灭 / 合并拍不亮 ===");
        Check(driver != null && driver.Ctx != null, "B0 （前提）有活着的 `BattleDriver` / `Ctx`");
        if (driver == null || driver.Ctx == null) return;

        var prevMode = CardTween.Mode;
        CardTween.Mode = DG.Tweening.UpdateType.Manual;   // 批处理没有帧循环：淡出必须手动推
        try
        {
            int owner = driver.MyIndex;
            var ps = (driver.Ctx.Players != null && owner >= 0 && owner < driver.Ctx.Players.Length)
                   ? driver.Ctx.Players[owner] : null;
            int hostSlot = -1; UnitState host = null; CardView hostView = null;
            if (ps != null && ps.Board != null)
            {
                for (int s = BoardSpec.WarlordSlot + 1; s < BoardSpec.Size; s++)
                {
                    if (ps.Board[s] == null) continue;
                    CardView v;
                    if (!driver.MyUnits.TryGetValue(s, out v) || v == null) continue;
                    hostSlot = s; host = ps.Board[s]; hostView = v; break;
                }
            }
            Check(hostSlot >= 0 && host != null && hostView != null,
                  "B0 （前提）本方右半场有一张**带视图**的单位（拿它当「落点后成为右邻」那张）"
                + " —— 不满足时如实计一条失败，⛔ 不静默跳过");
            Check(host != null && host.Card != null && !string.IsNullOrEmpty(host.Card.Name),
                  "B0 （前提）那张有卡定义与名字");
            if (hostSlot < 0 || host == null || host.Card == null) return;

            bool hadSwarm = host.Has(KeywordTable.Swarm);
            if (!hadSwarm) host.AddKeyword(KeywordTable.Swarm, 1);
            Check(host.Has(KeywordTable.Swarm),
                  "B0 （前提）宿主当前带 `swarm`（原本没有就临时挂上、收尾摘掉）");

            // 造「拖拽卡」：`CardData.id` 就是卡名（`ToCardData` 的 `id = u.Name`）。
            CardData dragData = hostView.Data;
            dragData.id = host.Card.Name;
            dragData.isUnit = true;
            var drag = CardView.Create(driver.transform, dragData, "B104A2SwarmDrag");
            Check(drag != null && drag.Data.id == host.Card.Name,
                  "B0 （前提）造出「拖拽卡」视图，且卡名与宿主一致");

            var fxBefore = new HashSet<WarpforgeVFX.WarpforgeEffectPlayer>(
                               WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers);

            // 起始：先清一遍，保证下面的「亮」不是残留
            driver.SimulateSwarmPreviewForTest(null, true, -1);
            CardTween.Advance(0.35f);
            Check(!hostView.SwarmVisible, "B0 起始：图标是灭的（否则 B1 的「亮」没有鉴别力）");

            // B1 预览亮
            driver.SimulateSwarmPreviewForTest(drag, true, hostSlot);
            Check(hostView.SwarmVisible,
                  "B1 预览：拖**同名带 swarm** 的牌、落在这张身上 ⇒ **图标亮**"
                + "｜🧨 删掉 `OnDropPreview` 里的 `RefreshSwarmPreview()`（或同名判据）⇒ 红");

            // B2 反例：卡名不同 ⇒ 不许亮（灭自证）
            var wrongData = dragData; wrongData.id = "B104A2·不是这张";
            var dragWrong = CardView.Create(driver.transform, wrongData, "B104A2SwarmDragWrong");
            driver.SimulateSwarmPreviewForTest(dragWrong, true, hostSlot);
            CardTween.Advance(0.35f);
            Check(!hostView.SwarmVisible,
                  "B2 反例：拖的卡**名字不同** ⇒ 图标**熄灭**（不是「拖着一张牌就亮」）"
                + "｜🧨 删掉 `host.Card.Name == drag.Data.id` 那半句 ⇒ 红");

            // B3 恢复（证明 B2 是关得掉、不是图标根本没建出来）
            driver.SimulateSwarmPreviewForTest(drag, true, hostSlot);
            Check(hostView.SwarmVisible, "B3 恢复：同名 ⇒ 再亮（B2 的熄灭不是「图标压根没建出来」）");

            // B4 松手/取消 ⇒ 灭
            driver.SimulateSwarmPreviewForTest(null, true, -1);
            CardTween.Advance(0.35f);
            Check(!hostView.SwarmVisible,
                  "B4 松手/取消 ⇒ 图标熄灭（原版 `case 3` `:418` / 取消 `:303/:208` / 离开落区 `:620`）"
                + "｜🧨 收工支不清 ⇒ 红");

            // B5 合并那一拍 ⇒ 不许亮
            driver.SimulateSwarmMergeTriggerForTest(owner, hostSlot, "swarm");
            CardTween.Advance(0.35f);
            Check(!hostView.SwarmVisible,
                  "B5 合并那一拍 ⇒ **图标不亮**（原版 `CardScript__ExecuteSwarm.c` 全文无 `DisplaySwarm`；"
                + "图标只服务拖拽预览）｜🧨 把 `v.SetSwarm(true)` 加回 `PlaySwarmMergeFeel` ⇒ 红");

            // 收尾：杀测试期间新建的特效、销毁两张探针视图、摘掉临时关键词
            var toKill = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
            foreach (var fp in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                if (fp != null && !fxBefore.Contains(fp)) toKill.Add(fp);
            for (int k = 0; k < toKill.Count; k++) toKill[k].Kill();   // ⚠️ Kill 会从 Active 里摘 ⇒ 先快照再杀
            if (drag != null) UnityEngine.Object.DestroyImmediate(drag.gameObject);
            if (dragWrong != null) UnityEngine.Object.DestroyImmediate(dragWrong.gameObject);
            if (!hadSwarm) host.RemoveAll(KeywordTable.Swarm, true);
        }
        finally
        {
            CardTween.Mode = prevMode;
        }
        Debug.Log(P + $"=== 行为合计：通过 {Passed} / 失败 {Failed} ===");
    }
}
