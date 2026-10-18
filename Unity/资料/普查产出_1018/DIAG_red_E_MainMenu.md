# 自检红诊断 · 批次 E · MainMenuScene 14 条红

> 只读诊断（不跑 Unity、不改 `.cs`/数据/正本）。
> 输入 = `d:/4/_tmp_view/menu.log`（行号均为该文件的行号）+ 源码静态核对。
> 判定档位：**(α) 断言过期 / (β) 实现缺陷 / (γ) 本批回归 / (δ) 夹具前提 / 级联**。

**结论一句话**：14 条红**全部是同一根因的级联**，无一条是实现缺陷。
根因 = 本批把 `PracticeModePopup.PickArmy` 改成原版语义「同阵营再点 ⇒ 早返回」（正确），
但测试在 ㉒④ 那一段之后**仍按「再点一次会取消筛选」往下写**（第二次点击已不会清筛选）⇒
筛选停在 `Factions()[0]`（Ultramarines），后面所有「不过滤 / 4 套 / 9 套」的前提全塌。
`#10–#13` 是这条链上的**二级放大器**（`#9` 真的开了挑对手窗 ⇒ 窗口栈里多出一扇没关的
`Deck Selection Popup with Tabs`）。

---

## §一 逐条定性

| # | 日志行 | 断言原话（短） | 判定 | 证据 | 建议修法（精确到行） |
|---|---|---|---|---|---|
| 1 | `:11133` | （前提）卡组列表下滚 140px（本夹具滚得到，见上）—— 实得 **116.210** | **δ 夹具前提**（γ 本批改动的直接后果） | `Editor/MainMenuScene.cs:2918` 调 `SetOffset(140f)`；而同一段 `:2901-2903` 自己写着「筛成 8 套 ⇒ 内容高 657 ⇒ **可滚范围 116.21**」（Log `:11090` 该条 ✓）。`SetOffset` 夹到 `MaxOffset = 116.21` ⇒ `Offset` 读回 116.210。改筛选的源头 = `Shell/PracticeModePopup.cs:1788` `if (ArmyIndex == i) return;`（本批新增），而测试 `:2908` 的第二次点击**不再清筛选** | 在 `Editor/MainMenuScene.cs:2912` 之后（㉒④ 那个 `{}` 收口之后、`:2914` 的注释之前）插夹具复位：<br>`pw.ArmyIndex = -1;`<br>`pw.RebuildDeckListForTest();`<br>`CheckNear(pw.DeckScrollContentH, 816f, 0.6f, "（夹具复位）清回不限 ⇒ 9 套 ⇒ 5 行 ⇒ 816");`<br>（可选再补 `pw.ArmyScroll.OnChanged();` 让 `Highlight` 回到「一格都不亮」，写法同本文件 `:2478`）<br>复位后 `MaxOffset = 816 − 540.79 = 275.21` ≥ 140 |
| 2 | `:11162` | 名字条纵向中心滚到 = **276.54** —— 实得 **300.330** | **δ 级联**（#1 的同一次夹取） | 数值可逐位复算：`262.64 + (138.60+169.20)/2 − 116.21 = 300.330` —— 与实测**逐位相等**，证明就是「140 被夹成 116.21」。`:2923-2926` 的期望式本身没错 | 同 #1；本行断言**一个字不用改** |
| 3 | `:11177` | ★ …确认它在带内（不在带内 ⇒ 下面那条「削 alpha」等于没查） | **δ 级联** | 300.330 > 带下沿 285.64 ⇒ 不在带内。`:2927-2929` | 同 #1 |
| 4 | `:11191` | ★ 卡组名也吃软边：…有 **0** 个顶点 alpha < 250 ｜诊断：带内 0 个 | **δ 级联** | 日志自带诊断「带内（y > 285.64）184 个顶点、最小 alpha 255（带内 0 个 ⇒ 这段字不在带里 —— 那不是裁切的问题，是文字摆位）」⇒ 软边实现没被咬到，是文字整体落在带外。`:2930-2936` | 同 #1 |
| 5 | `:11262` | …内容高也回到 **339**（4 套 ⇒ 2 行）—— 实得 **180** | **δ 级联** | `180 = 159×1 + 21` = **1 行**。此时 `ArmyIndex` 仍是 0（Ultramarines）；夹具 4 套里只有第 4 套「菜单测试卡组 合法30」是 Ultramarines（`Editor/MainMenuScene.cs:2120` `const string LegalFaction = "Ultramarines"`）⇒ 筛完只剩 1 套（过滤点 = `Shell/PracticeModePopup.cs:1350`）。`CollectionData.DeckCount()==4` 那条 `:2979` 是 ✓（库确实 4 套，只是被筛掉了） | 同 #1（复位发生在夹具恢复**之前**即可；`:2976-2980` 恢复 4 套后 `RebuildDeckListForTest()` 自然是 2 行 339） |
| 6 | `:11277` | 第 1 套卡组那一行有点击区 | **δ 级联** | 行节点按**卡组下标**命名（`Shell/PracticeModePopup.cs:1373` `"DeckRow_" + i`）；筛成 Ultramarines 后唯一建出来的是 **`DeckRow_3`**，`FindChild(pw.transform, "DeckRow_0")`（`:2987`，递归查找）⇒ null | 同 #1 |
| 7 | `:11291` | 点第 1 套 ⇒ 选中它 —— 期望 [0] 实得 [3] | **δ 级联** | `:2990-2991` 有 `if (w0a != null) w0a.Click();` 守卫 ⇒ #6 为 null 时**根本没点**；`DeckIndex` 停在开窗值 3（`Shell/PracticeModePopup.cs:820` `DeckIndex = CollectionData.CurrentIndex()`） | 同 #1 |
| 8 | `:13171` | ★ 第 0 套只有 4 张卡 ⇒ `Practice Deck` 的 `interactable = false` | **δ 级联** | `di` 由 `ShowDeckContent` 以**当时的 `DeckIndex`** 建（`Shell/PracticeModePopup.cs:720` `DeckInfoPopup.Create(Manager, DeckIndex, …)`）；`DeckIndex==3` ⇒ 打开的是「合法30」（30 张、合法）⇒ interactable=true 是**正确行为**。日志 `:13421` 亲证 context 是「菜单测试卡组 合法30」。实现侧判据（`DeckRules.Validate`）无缺陷 | 同 #1（`DeckIndex=0` 后 `di` 打开的是 4 张那套 ⇒ 这条自然绿） |
| 9 | `:13437` | ★ 点它 ⇒ **什么都没发生**（窗没关、挑对手那扇也没开） | **δ 级联**（并且是 #10–#13 的**直接种子**） | 因为 #8 前提不成立，`interactable=true` ⇒ 点击真的走了生产路径：`DeckInfoPopup.SelectPracticeOpponentDeck()`（`Shell/DeckInfoPopup.cs:2253-2266`）**开一扇选卡组窗 + 关自己**。这扇窗测试**从头到尾没有关它**（全文件只在 `:3355/:3361` 读 `LastOpponentSelection`，无 `Close()`） | 根修同 #1。可选硬化：`:3362` 之后补 `if (DeckInfoPopup.LastOpponentSelection != null) DeckInfoPopup.LastOpponentSelection.Close();`（⛔ 但 `:3361` 那条断言**不许放宽**，否则等于把缺陷洗绿） |
| 10 | `:17892` | 练习窗（选卡组那一列）：（前提）这一刻窗是开着的 | **δ 级联**（二级） | `pw.CurrentState` 此刻是 **`Background`**，不是 `Open`。铁证 = 日志 `:17956` `[Win] Practice Mode Menu：TryOpen 命中 Background 支`（= 只有 Background 才走这一支）；而 `:17659` `[Win] Deck Selection Popup with Tabs：TryOpen 命中 Background 支`（栈 = `MainMenuScene.cs:3976` 的 `ClickAt` 关窗 → `ShowPreviousWindow`）证明**关掉 ds4 之后被还原到上面的是另一扇选卡组窗** —— 即 #9 留下的那扇。`Shell/MenuDraw.cs:2773` 这条前提的写法本身没问题 | 根修 #1/#8/#9（那扇窗根本不该被开出来）⇒ 栈干净后 `pw` 会由 `ShowPreviousWindow` 从 Background 还原成 Open |
| 11 | `:17924` | 练习窗：**点面板 ⇒ 窗不关** | **δ 级联**（二级） | `Shell/MenuDraw.cs:2776-2777` 判的是 `state() == Open`；窗是 Background ⇒ 恒红。**不是**吸收层坏了（`:17812/:17828` 档位那两条都 ✓） | 同 #10 |
| 12 | `:17940` | 练习窗：**(5,5) 命中的就是这扇窗自己的压暗层那一颗**（实得 `BackgroundHit` = **别家的窗**） | **δ 级联**（二级） | `Shell/MenuDraw.cs:2799-2800`；命中的是 #9 留下的 `Deck Selection Popup with Tabs` 的压暗层（节点名就叫 `BackgroundHit`，`Shell/DeckSelectionPopup.cs:341/496`）。⚠️ **顺带**：`:17876`「矩形内找得到一个点、它的命中是吸收层」当时也是**为错窗过的**——两扇窗的吸收层节点同名 `AbsorbHit`（调用点 `MainMenuScene.cs:3977`），旧写法分不出是谁的 | 同 #10（栈干净后 (5,5) 命中的就是练习窗自己那颗） |
| 13 | `:17993` | 练习窗：**点面板外 ⇒ 关窗** | **δ 级联**（二级） | 点 (5,5) 关掉的是**那扇遗留窗**，随后 `ShowPreviousWindow` 把练习窗从 Background 拉回 Open（日志 `:17956`）⇒ `state()==Closed` 判假。`Shell/MenuDraw.cs:2808` | 同 #10 |
| 14 | `:29636` | 卡组名画的是**当前选中那套**：实测「菜单测试卡组 合法30」，期望「菜单测试卡组 1」 | **δ 级联**（同源，跨到很后面） | 链条：`DeckIndex` 停在 3（#7）→ `Editor/MainMenuScene.cs:3216` 的 `if (pw.DeckRows.Count > 1)` **整块被跳过**（本 run 日志里「点第 2 套卡组 ⇒ 选中的换成它」「没有督军的卡组 ⇒ Battle! 如实拒绝」「点回第 1 套 ⇒ 选中回来」**一条都没有**）→ `Battle!`（`:3250`）以 `DeckIndex=3` 开战（日志 `:12642` 亲证「菜单测试卡组 合法30」）→`StartBotBattle` 调 `CollectionData.Select(3)`（`Shell/PracticeModePopup.cs:2086`）**并落盘** → `DeckLibrary.CurrentIndex=3` → 遭遇战窗开窗时 `DeckIndex = FirstDeckInMode(CollectionData.CurrentIndex())`（`Shell/LiveOpsEventWindow.cs:367`）→ `BuildDeckTexts` 用 `CollectionData.DeckAt(DeckIndex)` 画名（`:521-535`）⇒「合法30」。**是同一状态残留，不是独立缺陷** | 同 #1（复位后 `:3216` 那块会重新跑，`DeckIndex` 归 0 ⇒ `Select(0)` ⇒ 遭遇战窗画「菜单测试卡组 1」） |

---

## §二 根因归并

```
R1（唯一根因，γ 本批回归 + α 测试后续段落未跟着改）
  Shell/PracticeModePopup.cs:1788-1792  PickArmy 删掉「再点=取消筛选」（按原版，正确）
  Editor/MainMenuScene.cs:2908-2910      只改了紧随其后的两条期望（0 / 657）
  ⇒ 后续段落仍按「已回到不过滤」写：
     ├─ #1–#4   滚 140 / 名字条进带 / 削 alpha（同一夹具前提）
     ├─ #5–#7   恢复 4 套后仍按 Ultramarines 筛 ⇒ 180 / 无 DeckRow_0 / 没点成
     ├─ #8–#9   DeckInfoPopup 以 DeckIndex=3 开 ⇒「合法30」可点 ⇒ 点击真开了挑对手窗
     │            └─ R2（二级放大器，δ）
     │               #9 开的 DeckSelectionPopup 从没关 ⇒ WindowsManager.openWindows 里多一扇
     │               ds4 关掉时 ShowPreviousWindow 还原的是它（日志 :17659）⇒ 练习窗停在 Background
     │                 ├─ #10 前提「窗是开着的」红
     │                 ├─ #11 点面板「窗不关」红（判的是 Open）
     │                 ├─ #12 (5,5) 命中「别家的窗」红
     │                 └─ #13 点外面「关窗」红（拉回来的是练习窗，不是关掉）
     └─ #14      DeckIndex=3 → :3216 整块被跳过 → Battle 以第 4 套开战 → Select(3) 落盘
                 → 遭遇战窗开在第 4 套
```

**没有任何一条是 (β) 实现缺陷**：`PickArmy` 的早返回、`DeckRules.Validate`、软边裁切、
吸收层档位、遭遇战窗取当前卡组，全部按判据工作。**⛔ 不要回滚 `PracticeModePopup.cs:1788`**
（原版 `ChangeSelectedArmy.c:30-33` 就是同阵营早返回）。

**推荐的最小修法（只动测试夹具，不动实现）** —— `Editor/MainMenuScene.cs:2912` 之后插入：

```csharp
// 🔴 **2026-10-19（批次 E 诊断）**：`PickArmy` 已按原版 `ChangeSelectedArmy.c:30-33` 删掉
//   「再点取消筛选」⇒ 上面那次点完，筛选**停在** `Factions()[0]`（Ultramarines）。
//   下面几节（滚 140px / 名字条进渐隐带 / 恢复 4 套夹具后的 339）都按**不过滤**算
//   ⇒ 这里显式清回「不限」。写法同本文件 `:2472/:2478`（直接改字段 + 走重建口），
//   ⛔ 不是把 toggle 改回来。
pw.ArmyIndex = -1;
pw.RebuildDeckListForTest();          // 9 套 ⇒ 5 行 ⇒ 内容高 816 / MaxOffset 275.21
CheckNear(pw.DeckScrollContentH, 816f, 0.6f,
          "（夹具复位）阵营筛选清回不限 ⇒ 9 套 ⇒ 5 行 ⇒ **816**");
// 可选（只为状态自洽）：pw.ArmyScroll.OnChanged();  // Highlight 回到「一格都不亮」
```

- 位置理由：必须排在 `:2918` `SetOffset(140f)` **之前**、且要等 ㉒④ 自己的两条断言（657/116.21）**跑完之后**（那两条证明「筛选生效时内容高会重算」，是活的判据，⛔ 别删）。
- `ArmyIndex` 是 `public` 字段（`Shell/PracticeModePopup.cs:773`），本测试已有直写先例（`:2472`、`:2478`、`:2493`）⇒ 不需要新加实现口。
- 备选（更自解释）：在 `Shell/PracticeModePopup.cs:1519-1523`（`RebuildDeckListForTest` 旁）加一个
  `public void ClearArmyFilterForTest() { ArmyIndex = -1; if (_deckHolder != null) RebuildDeckRows(_deckHolder); }`，测试改调它。
- ⛔ **不要**改用「把 `SetOffset(140)` 改成 116.21 / 把期望 276.54 改成 300.33」来「修」#1–#4：
  滚到底只有 116.21，名字条中心 300.33 **落在渐隐带之外**（带 = 262.64..285.64），
  `#4` 的软边断言将**永远没有鉴别力**（正是它自己文案里说的「等于没查」）。

---

## §三 复跑建议

- 只动了 `Editor/MainMenuScene.cs` 一个宿主 ⇒ 修完**只跑 `MainMenuScene.Run`** 一条（铁律 12：不跑全量）。
- 复跑后应出现的新信号（可当验收点）：
  1. 复位处新增那条 `816` ✓，随后 `:11133` 的 `140` ✓、`:11162` 的 `276.54` ✓、`:11177` 在带内 ✓、`:11191` `faded > 0` ✓；
  2. `:11262` 339 ✓、`:11277` 有点击区 ✓、`:11291` DeckIndex=0 ✓；
  3. **`点第 2 套卡组 / 换到空卡组 / Battle! 如实拒绝 / 点回第 1 套` 这四条这次应该重新出现**（本 run 被整块跳过，见 §四）；
  4. `:13171` / `:13437` 两条绿，且日志里**不应再出现** `[DeckInfo] Practice Deck ⇒ 开选卡组窗挑对手`；
  5. 练习窗吸收层那四条全绿（`:17892/:17924/:17940/:17993`）；
  6. `:29636` 卡组名 = 「菜单测试卡组 1」。
- 注意：修好后 `:3216` 那个被跳过的块会**重新执行**（点空卡组 ⇒ 弹模态窗 ⇒ `CloseModalPopups` ⇒ 点回第 1 套）。
  本 run 它被跳过、没被验证过；复跑时若那边冒新红，那是**太久没被覆盖**，不是本修法引入的。

---

## §四 没查清 / 顺带发现

1. **（顺带发现，非红）`Editor/MainMenuScene.cs:3216` 的 `if (pw.DeckRows.Count > 1)` 整块被静默跳过**：
   本 run 里「点第 2 套卡组 ⇒ 选中的换成它」「换到空卡组 ⇒ 卡列表 0 行」「没有督军的卡组 ⇒ `Battle!` 如实拒绝」
   「点回第 1 套 ⇒ 选中回来」**一条都没出现**（`grep` 全日志 0 命中）。这是**覆盖静默丢失**（不是断言红），
   正是 R1 的直接后果（只剩 1 行），也是 #14 的中间环节。修 #1 时它会一起回来。
2. **（顺带发现，假绿）`MainMenuScene.cs:3991` 的「面板矩形内找得到一个点、它的命中是吸收层」（`Shell/MenuDraw.cs:2770`）**
   在 #10–#13 全红的情况下**仍然是 ✓**：因为遗留的选卡组窗也有一个同名 `AbsorbHit` 且同样 `absorbOnly`。
   也就是说这一条**单独**证明不了「命中的是练习窗自己的吸收层」——这是既有弱断言（不属本批改动），
   建议把判定收成 `oHit.transform.IsChildOf(winRoot) && WasAbsorb(...)` 的口径（同 `:2799` 那条的写法）。
   ⚠️ 本条**不是**本批 14 条红的成因，只是被这次级联暴露出来。
3. **未逐实例证明**「遗留的那扇 = #9 开的那一扇」：我是从「ds2/ds3/ds3b/ds4 各自都有显式 `Close` 且日志逐条 ✓」
   反推出剩下的只有 #9 那扇，并用日志 `:17659`（关 ds4 后还原了一扇 DeckSelectionPopup）与
   `:17956`（练习窗当时是 Background）交叉印证。**没有**在运行期 dump `WindowsManager.openWindows`
   （只读、不跑 Unity）——若复跑后 #10–#13 仍红，第一步就 dump 那个列表。
4. `FirstDeckInMode(3)` 是经 `DeckFitsMode` 接受还是走兜底 `return prefer`（`Shell/LiveOpsEventWindow.cs:266-276`）
   没有区分；两种情形结果都是 3，与本诊断无关。
5. 原版 `ChangeSelectedArmy.c:30-33` 的早返回语义**没有**再回反编译复核（本批已有引用与注释，
   且与 `allowClickWhenAlreadySelected` 的排除理由自洽）；本诊断按「本批实现是正确的」为前提展开。
