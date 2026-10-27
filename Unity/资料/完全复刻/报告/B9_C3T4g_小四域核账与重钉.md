# B9_C3T4g · 小四域（收藏 / 卡牌面·渲染 / 卡组编辑 / 本地化）—— B 行核账 + 落点重钉

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围（现读）：`收藏.tsv` 16 B · `卡牌面_渲染.tsv` 8 B · `卡组编辑.tsv` 8 B · `本地化.tsv` 5 B = **37 条**。
> 产出：`判据/B行逐条核_小四域.tsv`（8 列 × 37）· `判据/落点重钉_小四域.tsv`（6 列 × 37）· `账/_parts/B9_C3T4g.tsv`（11 列 × 37）· 本报告。

## 一、结论分布

| 建议等级 | 条数 | 说明 |
|---|---|---|
| **A（等价，已重钉）** | **1** | #33 `GetTranslationWithParameters` → `Loc.T` |
| **B（真偏离，含 2 条已重钉）** | **12** | 落点是方法但有缺口；#34/#37 已重钉 |
| **落点失效** | **22** | 无 file:line / 字段级 / 抽象声明 / `—`，且无可指方法 ⇒ 写「无落点：原因」 |
| **判据缺** | **2** | #9 / #12（ItemDrawer 的 vtable/NoteDecompilerIssue） |
| 合计 | 37 | |

## 二、3 条已重钉（每条实际打开目标行，原文见下）

| # | 域 | 原版方法 | 新落点 | 该行原文（前 80 字符） | 等价 |
|---|---|---|---|---|---|
| 33 | 本地化 | `EverguildLocalization.GetTranslationWithParameters(key,p1,p2,p3)` | `CardPresentation/Core/Loc.cs:3421（T）` | `public static string T(string key)` | 是 → **A**（参数替换走调用点 `string.Format(Loc.T(键), …)`） |
| 34 | 本地化 | `LanguageSelector.Initialize()` | `CardPresentation/Shell/SettingsWindow.cs:3478（BuildLangTemplate）` | `void BuildLangTemplate(Transform fld)` | 部分 → **B** |
| 37 | 本地化 | `LanguageSelector.ResetLanguagesDropdown()` | `CardPresentation/Shell/SettingsWindow.cs:3490（BuildLangListSubtree）` | `void BuildLangListSubtree(Transform listRoot, bool withRows)` | 部分 → **B** |

> ⚠️ `SettingsWindow.cs` 正在被别的写手改（行号已观察到 −1 漂移）；上面 :3478/:3490 是**生成时按声明串动态定位**得到的现读行号（不是抄死行号）。

## 三、12 条 B（真偏离）

- **收藏 5**：#1 `OnCollectionUpdate`（落点 `RebuildCardsPage`/`RefreshStylesAfterFilter`，整页重建覆盖逐格 `UpdateCardVisuals`，**缺信号层+逐格刷新**）、#2 `CardCollectionTab.ToFocus`（页 2/3 不重灌）、#6 `GameModesIconCollection.Release(PlayModes?)` / #7 `ReleaseAll`（落点 `CardArt.cs:1268 ReleaseModeIcon`，**差一跳接线**）、#13 `ItemDrawer.OnDestroy`（落点 `ItemDrawerLeash`；原版 Addressables 句柄、我们解除登记）。
- **卡牌面 1**：#17 `CardCollider2D.SetColliderNotInHand`（落点 `BattleDriver.cs:24811 PlayDeathFeel`；**进墓地已接，`removedFromGame(11)` 那一半无落点** = 引擎没这个状态）。
- **卡组编辑 3**：#25 `SetPlayerData`（`MetaOf`：能拿到的七格已灌，服务器几格离线无源、每次开局出声）、#26 `SetupSerializedData`（③④已接，①②⑤ DTO 反序列化在 `DeckRules`）、#30 `PrebuiltDeck.GetCardLibrary`（`ToPlayerDeck`，**逐方法未核**）。
- **本地化 3**：#34/#37（已重钉，见 §二）、#36 `OnLanguageChanged`（落点 `SettingsPanel.cs:1889 ChooseLanguage`，**第 4 跳不等价**：`BattleHudLanguage.Notify()` 今天订户 = 0，卡面/日志还没订）。

## 四、2 条判据缺

- **#9 `ItemDrawer.Draw(ObtainableItem, int amount, ItemDrawerOptions)`**：sealed override 体有 **20+ 处 `NoteDecompilerIssue`**（quantity/label/image 几跳读不到）。
- **#12 `ItemDrawer.GetItemName(T)`**：基类体 = vtable 间接跳转，真值在 23 个子类 override（已解 2 条）。

## 五、22 条落点失效（新落点=「无落点：原因」；明细见 `判据/落点重钉_小四域.tsv`）

- **收藏 9**：#3 `CollectionCard.OnDestroy`（无卡格组件/Sequence）· #4 `CollectionCard.Start`（无 card2DController 高亮层）· #5 lambda 体（并入 Toggle）· #8 `TryRelease`（无句柄层，GetIcon 内联）· #10 `Draw(ObtainableItem,int,ItemDrawerOptions)`（**基类抽象声明**，真体=:356）· #11 `Draw(T,int)`（**泛型抽象声明**，23 子类）· #14 `ToggleShowInfoOnClick`（**abstract**，行为由序列化字段决定）· #15 `WhereToGetCardsSection.OnDestroy` / #16 `Release`（该窗不建抽屉、无此方法）。
- **卡牌面 7**：#18–#24 `TextTooltipController` 的 OnDeselect/OnDisable/OnEnable/OnPointerEnter/OnPointerExit/OnSelect/Toggle —— **Tooltip 族无逐方法入口**（悬停/显隐/链接判定混在一处；`Tooltip.cs` 里只有注释提及这些名字）。
- **卡组编辑 5**：#27 `DeckListDrawer.ConfigPrefab`（行代码直建、无模板 prefab）· #28 `DeckSelectorMenuItemDemo.OnDestroy`（整棵销毁、无独立退订）· #29 `DemoDeckInfoSO.SetTutorialIndex`（**字段级**：`TutorialModePopup.StageRow.Idx` 常量表）· #31 `PrebuiltDeck.IsIncompleteLibrary`（**字段级**：`Deck.complete/missing` 生成期写）· #32 `PrebuiltDeck.SetHero`（**字段级**：`Deck.heroId` 生成期定死）。
- **本地化 1**：#35 `LanguageSelector.OnDestroy`（SettingsWindow 语言下拉无监听可退订）。

> 🔴 「落点失效 / 无落点」≠ 判 D / 不做：`状态` 仍保持原值 `B`。#29/#31/#32 是「运行期写 vs 生成期/字段」的数据模型差（不是漏做），要落地得在 `PrebuiltDecks`/`TutorialModePopup` 补运行时写口 —— 属「休眠也要做」的范畴。

## 六、验证与残余（如实）

- 3 条重钉**逐条打开目标行读过原文**（§二）；两个 `SettingsWindow` 行按声明串**动态定位**（文件在改，写死不稳）。
- 22 条「无落点」：按落点提示文件 + 全仓方法名检索后仍无可指方法（Tooltip 族、抽象声明、字段级、该窗不建的东西）。
- A/B 主要依据行内 `差异`（历轮逐句读原版体写下）+ 本轮方法存在性抽验；**未对 37 条全部重读原版全文**。
- 补丁 `状态` 全 B 不动、`轮次`=B9_C3T4g、`我们的落点` 只对 3 条已重钉行改写（其余保留）；11 列无坏行；`check_patch_conflicts.py` → 无跨补丁重复键（2 份补丁）。
- 生成器脚本：`d:/4/Unity/_tmp_view/gen_c3t4g.py`。
