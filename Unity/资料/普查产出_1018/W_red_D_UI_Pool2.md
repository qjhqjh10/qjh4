# W_red_D_UI_Pool2 —— 批次 D 写手回执（8 条红）

> 范围 = `DIAG_red_D_UI_Pool2.md` 的 8 条。⛔ 没跑 Unity / `*.Run` / player；只跑了秒级类型检查。
> **类型检查**：`TMPDIR=/tmp/wf_redD bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（2026-10-19，全部改动落盘之后跑的）。

## §一 逐条表

| # | 宿主:行（修后） | 判定 | 改了什么 | 依据 | typecheck |
|---|---|---|---|---|---|
| 1 | `Editor/DeckScene.cs:7278-7318` | δ 夹具前提失效 | 费用 tooltip 夹具从固定 `pool_0` 改成**循环找第一张「非战将且有费用」的可见卡**（`UiPoolCardAt(i)` + `PoolCellGo(i)` + `GetComponent<CardView>()`，跳过 `!activeSelf`）。三条断言原意保留（显示 / 费用那一条 / 面板可量），护甲/空白两条也保留。找不到时**显式红**（并打印可见槽数），⛔ 不退回 `pool_0` | 本批 `SortForDisplay` ⇒ 每阵营第 0 格恒为战将；`CardView.ShowsCost` = `cost >= 0 && type != "hero"`（原版 `BasicCardUI__SetRawCardData.c:84`）⇒ 战将没有费用 tooltip 是正确行为 | ✅ 0/0 |
| 2 | `Editor/DeckScene.cs:7306-7308` | δ（同 #1 级联） | 同 #1（断言本身未改，/ 只是现在喂给了真能触发的那一格） | 同 #1 | ✅ |
| 3 | `Editor/DeckScene.cs:7309-7312` | δ（同 #1 级联） | 同 #1 | 同 #1 | ✅ |
| 4 | `Shell/PracticeModePopup.cs:1779-1792` | β 实现缺陷 | `PickArmy` 开头加 `if (ArmyIndex == i) return;`（同阵营再点 = 直接返回，不改筛选、不重建）；其余逻辑不动。⛔ 没用 `allowClickWhenAlreadySelected`（练习窗是 `EverguildButton`，不是 Toggle；用了会把原版第二次点击仍会响的按钮音一起吞掉） | 原版 `PracticeModePopup__ChangeSelectedArmy.c:30-33`：`if (param_2 == *(int *)(param_1 + 0xe8)) return;` | ✅ |
| 5 | `Editor/MainMenuScene.cs:2909-2910`（断言未改） | β（同 #4 级联） | 由 #4 一处修复转绿（期望 0 / 657 保持不变） | 同 #4 | ✅ |
| 6 | `Editor/RewardsScene.cs:9946-9953` | α 断言过期 | 「**不建** `Reset Button`」→「**建了但关着**」：`resetBtnRewards != null && !resetBtnRewards.gameObject.activeSelf`（`FindChild` 含 inactive）。顺手订正 `Shell/InboxWindow.cs:23-34` 文件头、`:511`、`:627`/`:660` 三处「不建」的过期注释（含同批也建壳了的 `Claim Button`） | 原版 prefab 有该节点、运行时 `InboxWindow__Open.c:28-29` **首句**就 `SetActive(false)` ⇒ 我们的「建好再藏」= 原版运行态；`MenuCheck.FindChild` 走 `GetComponentsInChildren<Transform>(true)` ⇒ 关着的也找得到 | ✅ |
| 7 | `Editor/CollectionScene.cs:7346-7355`（另 `:7167` 注释、`:7466-7468` 删死局部） | δ 夹具/期望值 | `sgCap` 改成 `CardProgress.DeckCap(cd.Card.Rarity, cd.Card.Type)`（战将 ⇒ **1**，期望 `x1` 与原版一致）；`sgDelta` 随之自洽（`AddOwned` 落到 1 ⇒ `Spares` 的 `min(o, cap)` 仍 = 0，前提断言仍成立）；删掉 `:7459` 那句**未使用且不带卡型**的 `int cap`；`:7167` 的「文案是 x2」订正为 x1 | 原版 `GetMaxCopiesInDeck` 对督军返回 1（`decomp_full/…GetMaxCopiesInDeck.c:7-9`）；详情窗按 `DeckCap(rarity, type)` 印 x1（`CardDetailPopup.cs:1608-1610`） | ✅ |
| 8 | `Editor/CollectionScene.cs:8876-8894, 8949-8976` | α 新断言期望值算错 | 记 `hadChainWin = CollectionWindow.LastCardDetail != null`，主期望改 `cdWinsBefore + (hadChainWin ? 0 : 1)`；另加一条**更强**的「第 1 次开成后的扇数 == 5 次后的扇数」（不看起手；删掉 N116 的销毁段必红）。实现（`Shell/CollectionWindow.OpenCardDetail` 新建前 `Close()+DestroySafe`）**一字未动** | 进本段时已有一扇本文件开的详情窗 ⇒ 第一次 open 先销毁它再新建 ⇒ **净增 0**；日志原文「实得 2；开之前 2」（`collection.log:52664`） | ✅ |

## §二 关键 diff

**DeckScene.cs（#1–#3）**
```csharp
Tooltip.Hide(); Tooltip.FinishFade();
int tipSlot = -1;
CardView tipView = null;
CardDef tipDef = null;
for (int i = 0; i < _rt.UiPoolSlotCount; i++)
{
    var d = _rt.UiPoolCardAt(i);
    if (d == null || string.Equals(d.Type, "hero", StringComparison.Ordinal) || d.Cost < 0) continue;
    var g = PoolCellGo(i);
    if (g == null || !g.activeSelf) continue;          // `TickTooltipAt` 只问活着的格
    var cv = g.GetComponent<CardView>();
    if (cv == null) continue;
    tipSlot = i; tipView = cv; tipDef = d; break;
}
CheckTrue(tipView != null, "卡池里找得到一格**非战将且有费用**的卡来做费用 tooltip 夹具…");
```

**PracticeModePopup.cs（#4/#5）**
```csharp
void PickArmy(int i)
{
    // 原版 ChangeSelectedArmy.c:30-33：同阵营再点 = 直接返回
    if (ArmyIndex == i) return;
    ArmyIndex = i;
    ...
}
```

**RewardsScene.cs（#6）**
```csharp
var resetBtnRewards = FindChild(inbox.transform, "Reset Button");
CheckTrue(resetBtnRewards != null && !resetBtnRewards.gameObject.activeSelf,
          "`Reset Button` **按原版建好、且由 Open() 那一档关着**（死钮…）");
```

**CollectionScene.cs（#7）**
```csharp
int sgCap = CardProgress.DeckCap(cd.Card.Rarity, cd.Card.Type);   // 战将 ⇒ 1
```

**CollectionScene.cs（#8）**
```csharp
bool hadChainWin = CollectionWindow.LastCardDetail != null;
int cdWinsAfterFirst = -1;
...
int cdWinsExpect = cdWinsBefore + (hadChainWin ? 0 : 1);
Check(cdWinsAfter, cdWinsExpect, ...);
if (cdWinsAfterFirst >= 0)
    Check(cdWinsAfter, cdWinsAfterFirst, "★ N116（更强）：第 1 次开成之后 … 后面 4 次不再涨");
```

## §三 需要主对话复跑的宿主

| 宿主 | 为什么 | 备注 |
|---|---|---|
| **`DeckScene.Run`** | #1–#3 的夹具改了（`Editor/DeckScene.cs`） | 重点看三条费用 tooltip 与其后两条（护甲 / 空白） |
| **`RewardsScene.Run`** | #6 断言改了（`Editor/RewardsScene.cs`） | `Reset Button` 应「存在且 `activeSelf == false`」 |
| **`CollectionScene.Run`** | #7/#8（`Editor/CollectionScene.cs`） | #8 现在有两条断言：主条与「更强」条 |
| **`MainMenuScene.Run`** | 🔴 **#4/#5 的宿主在这里**（`MainMenuScene.cs:2909-2910`），修复落在 `Shell/PracticeModePopup.cs` | 期望 0 / 657 |

参考（**不必单独跑**）：
- `ShellScene.Run` / `BattleScene.Run` 只 `Create`/挂 `PracticeModePopup`（视口、开战链），**没有点阵营格** ⇒ `PickArmy` 语义改动不影响它们。
- `CollectionScene.Run` 里也有 `PracticeModePopup.Create`（Warlord Name 那一段），同样不点阵营格。

## §四 没查清 / 顺手发现

1. **#8 的日志与简报数字不一致**：简报写「实测多 2 扇」，日志原文是「实得 2 扇；本段开之前 2 扇」= **净增 0**。我按日志原文改（诊断报告的判断）。
2. **#7 没有换夹具卡**，走的是诊断里的备选「带卡型」。原因：整套详情窗断言（计数条 + 三块面板）都挂在同一个 `cd`（`CardHit_0` 开出的那扇）上，换卡会牵动几十条与本条无关的断言。带卡型之后 `sgDelta` 仍把 `spares` 压到 0（`Spares` 用 `min(o, DecCap(rarity))`，owned=1 时 min=1），所以 `:7357` 的前提断言仍成立。
3. **`Editor/MainMenuScene.cs:2904-2906` 的断言注释仍写着「原版 `allowClickWhenAlreadySelected = 0`（`Ranked Army Selector Container V2`）」** —— 那是**排名窗**的判据，练习窗那颗是 `EverguildButton`、不是 Toggle；注释把两件事混在一起（断言值 0/657 本身是对的）。该文件不在本写手白名单，**未改**，建议主对话顺手订正。
4. **`Shell/InboxWindow.cs` 另有两处「`Claim Button` 没建」的过期说法**（文件头、`BuildRow` 的点击日志）—— 本批已建壳；我一起订正了（无断言咬它们，属铁律 5 的文档订正）。
5. **没跑 Unity**：#1–#3 的「卡池里找得到非战将」是**静态推断**（本批排序 + `ShowsCost` + 改前拿 `Autarch` 的证据链）。复跑时若这条**红**，断言会打印可见槽数；届时可用 `_rt.UiPoolCardAt(i).Type` 现读（本写手没有加额外日志字段）。
6. 行尾检查：`DeckScene.cs` 全 CRLF（9190/9190），其余四个文件全 LF；本次编辑没有翻行尾。
