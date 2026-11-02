# B121-C3 · 卡组格 `Ban Icon`（+ 名字变红 + 四张图灰 0.25）

> 执行写手 `B121-C3` · 2026-11-03 · 账 `B121-COLL-09`（`报告/B121_COLL_收藏界面审计.md` §卡组专章 / 分报告 B 的 C12）
> 未跑 Unity · 未动 git · 未改账主表 / 活状态表 / 正本 · 未派子代理。

## ① 改了什么

1. **`Shell/MenuDraw.cs` 的 `DeckCell` 补上原版 `CollectionDeck.Config` 末尾那一档**（改前**整层没有**）：
   - **`Ban Icon`**（300×377.61 作者 px，sprite `40k_Cross_icon_cross_big Banned card`，`preserveAspect = 1`）
     ＋ 子件 **`Banned Text`**（210×75.52，TMP 38.25 / 基准 36 / auto 0.25–72 / `Center`-**Midline** / 折行 1，
     文案 `Loc.T("MenuDeck/Banned")`）。
   - **卡名色** `(1, 0.3, 0.3, 1)`（未命中 = 原样白）。
   - **四张图灰度 0.25**：`Deck Image`（退回档是 `CardBack`）/ `Frame` / `Faction Icon` / `Game Mode Icon`
     —— `Ban Icon` 自己与 `Deck Name` **不灰**（与原版 `imageMaterialPairs` 恰好 4 条一致）。
2. **判定入口收口成一个类**：`MenuDraw.cs` 类尾新增 `internal static class DeckBan`
   （`IsBanned` / `NameInk` / `Apply` / `Grey`），数据为空时 `IsBanned` 第一句就 `return false`。
3. **`DeckCell` 两个重载各加一个尾参 `int deckIndex = -1`**（缺省 ⇒ 按卡组名回查 `CollectionData.IndexOf`）。
   既有调用点一个都不用改（可选参数）。
4. 新断言 `Editor/B121_C3_Assert.cs`（`Run()` 自建临时根，不需要 `CollectionWindow`）。

## ② 判据（文件:行）

| 是什么 | 判据 |
|---|---|
| 整档行为 | `d:/2/tools/decomp_full/CollectionDeck__Config.c` 末尾：`Any(GetLibraryInFull(), IsCardBanned)` → `SetActive(0x90)` → `ToggleGreyScale(cVar4, DAT_1834b2e7c)` → `deckName.set_color` |
| 谓词 | `decomp_full/CollectionDeck.__c__DisplayClass16_0___Config>b__0.c`（`PlayEventData.IsCardBanned`） |
| 扫哪些卡 | `decomp_full/CardDeck__GetLibraryInFull.c` = `cardIds` 拷贝 + 督军 `Insert(0)` + 防御卡 `Insert(0)` |
| 几何 / 字体 | `python -I 工具/menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 3`（`Ban Icon −25,−25.11→275,352.5` · `Banned Text 20,125.93→230,201.45`） |
| 颜色 / 灰度常量 | `工具/read_literal.py`：`0x1834b3210…` = (1, 0.3, 0.3, 1) · `0x1834b2e50…` = (1,1,1,1) · `0x1834b2e7c` = **0.25** |

## ③ 落点（文件:行）

- `CardPresentation/Shell/MenuDraw.cs:3310` —— `DeckCell(...)` 声明（签名尾参 `deckIndex`）
- `CardPresentation/Shell/MenuDraw.cs:3463` —— 卡名的 `Text(...)` 颜色改成 `DeckBan.NameInk(info, deckIndex)`
- `CardPresentation/Shell/MenuDraw.cs:3593` —— `return DeckBan.Apply(cell, info, deckIndex, r, sx, sy, clipR, qOverlay + 1);`
- `CardPresentation/Shell/MenuDraw.cs:3774` —— `internal static class DeckBan`（`:3803` `IsBanned` · `:3822` `Apply`）
- 断言：`CardPresentation/Editor/B121_C3_Assert.cs`

⚠️ **行号零漂移**：4 处改动都是**就地替换**（行数不变），新类追加在**文件最末**（所有账落点 `:3662` 之后）。

## ④ 休眠原因（为什么生产上看不到）

- 原版按**每副卡组自己的** `CustomGameModeEvent` 判；禁卡表来自 LiveOps 活动数据（远端 CCD / `BannedCardsSO`），
  本地 0 事件 ⇒ 本机判据空。
- 我们复用 `B96_MENU`（2026-10-29）为**卡池格**建的注入点 `DeckRuntime.CurrentEventData`（那里已标
  `🔶 自己设计（非原版）`）。`PlayerDeck` 没有 `CustomGameModeEvent` 那一格，mode→event 在 LiveOps 远端
  ⇒ 用「进程级一个活动」代替「每副卡组一个活动」。
- ⇒ **生产路今天恒 `null` ⇒ `IsBanned` 恒 false ⇒ 整档空转**（一格都不建、一笔灰都不刷、名字仍白）。
  活动线把禁卡表灌进 `PlayEventData.SetBannedCards(ids)` 再赋给 `CurrentEventData` 后，这一档**自动点亮**，
  本文件不用再改。⛔ 没有伪造任何禁卡表。

## ⑤ 验证

- `TMPDIR=/tmp/wf_c3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` ⇒ **A 5876 · 漂移 0 · 失败 0**
  （中途一度读到 26→37 漂移，逐条查过：**没有一条指向 `MenuDraw.cs`**，全是别的代理当时在改的
  `CollectionWindow.cs`；他们改完后复跑归零）。
- `python -I 工具/merge_ledger_parts.py --dry --only B121_C3` ⇒ **更新 1 · 新增 0**（同键命中
  `卡组编辑.tsv` 里 `CollectionDeck / Config(...)` 那一行，不新增重复行）。
- ⛔ **未跑 Unity**（子代理红线）· 断言**未接线** ⇒ 首次由调度台挂上后看 `B121C3` 前缀。

## ⑥ 还差什么

1. **接线**：宿主里加一行
   `B121_C3_Assert.Run(); pass += B121_C3_Assert.Pass; fail += B121_C3_Assert.Fail;`（`Editor/*Scene.cs` 不在本件白名单）。
2. **把下标传进去**：`Shell/CollectionWindow.cs:5303` 的 `BuildDeckCell` 手上就有 `i`
   ⇒ 建议加 `deckIndex: i`（现在靠卡组名回查，同名卡组会取第一个）。
3. 活动线接上 `CurrentEventData` 后，本档才在生产上可见（届时建议顺带复核 `DeckSelectionPopup` 的队列，见 §七·3）。

## ⑦ 顺手发现（⛔ 只报不改，都不是本件白名单）

1. **`Shell/DeckSelectionPopup.cs:937-942` 的注释已过期** —— 它还写着「Ban Icon：本 build 判据是空的 ⇒
   **永不触发** ⇒ **不建**」。本件把这一档建在了**共用的 `MenuDraw.DeckCell`** 上 ⇒ 那扇窗（选卡组）
   也会跟着有这一档；注释与它的「不建」结论需要 C2 就地订正。
2. **`Shell/CollectionWindow.cs:306-320` 的注释已过期** —— 它写「原版那颗是 `UIImageGreyscaleController`，
   我们的 `ImageQuad` 没有对应的判据灰值 ⇒ 只记不画」。实测 `ImageQuad.SetGrey` 早就在
   （`Battle/ImageQuad.cs:355`，与 `CardView.SetGrey` 同一算式 / 同一属性 `_GreyScale`）⇒
   卡池 counter「拥有数 < 1 置灰」那一档**其实做得了**，现在是可做未做（属 C1 范围）。
3. **队列撞号风险（休眠档不影响今天）**：本档用 `qOverlay + 1`。收藏窗 = 3035（该页免费；`QFlt` 3040 在它之上，
   正确）；**选卡组窗** `qOverlay` = `QDsText + 1` = 3129 ⇒ 本档 = **3130**，与 `DeckInfoPopup.QDIOver`(3130) /
   `ImportDeckPopup.QImp`(3130) **同号**，而那两扇窗是**开在选卡组窗之上**的 ⇒ 接线到那扇窗时要复核/换档。
4. `DeckRuntime.CurrentEventData` 是 **`public static` 的进程级字段**，`B97W6` 与本次断言都靠它注入；
   活动线接线时记得「离开该模式置 null」（那条 doc 已经写了，这里只是再提醒一次）。
