# B117-WLEDGER — 账闸残值修复（9 漂移 + 29 失败）

> 代理：`B117-WLEDGER`（收口）· 日期：2026-11-02 · 工具：`Unity/工具/check_ledger_points.py`
> 授权范围：只改 `资料/完全复刻/账/*.tsv`（一次性）＋本报告。**未动 `.cs` / 断言 / 活状态表 / git / `d:/2`。**

## 一、读数（改前 → 改后）

| 时点 | A 总数 | 通过 | 漂移 | 失败 | 告警（通过的子集） |
|---|---|---|---|---|---|
| 改前（--fix 三遍后的残值） | 5831 | 5793 | **9** | **29** | 869 |
| 改后（本次） | 5831 | **5831** | **0** | **0** | 863 |

复跑命令（两次都跑过）：

```
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A
```

改后末行原样：`合计：A 5831 条 · 通过 5831（其中告警 863 条）· 漂移 0 · 失败 0`。

**改前状态是实打实重建出来核过的**，不是照抄任务书：把 7 份账复制到临时目录、把本次 38+2 处替换
**反向**应用，再跑同一个闸 ⇒ `漂移 9 · 失败 29` 逐条复现，且落点/原因与任务书一致
（7 份合计：A 3439 · 通过 3401 · 漂移 9 · 失败 29）。所以「改后归零」= 这 38 条真的被修掉了。

告警 869 → 863 的去向（**告警只是候选，不影响判档/退出码**）：`MEMBER` 64→70（+6，6 条
`ParentConstraint` 现在落在属性上，走方案 §4.1 例外）、`GETTER` 19→20（+1，`IAmDisconnectedEnd`）、
`PROSE` 246→234（−12，原先把「取不出声明名」当散文提示的那几行，名字现在对上了）、
`SECOND` 314→313（−1，`GetCardTier` 那条陈旧证据指针被换成真调用点）。

## 二、9 条漂移逐条处置（全部为「名字还在、行号是旧快照」）

判据：每一条都**现读目标行**确认「那一行确实声明了括号里写的名字」，才改行号。

| 账 | 原版类.方法 | 账里旧行号 → 现读 | 依据（现读那一行） |
|---|---|---|---|
| 战斗界面_HUD | `BattleErrorUIManager.ShowDisconnectionPopUp` | `NetBattle.cs:917` → **961**；散文 `:949` → **1010** | `public bool IAmDisconnectedEnd { get { return _s != null && _s.Role == NetRole.Client; } }`；`public static string ReconnectPopup(bool, int)`（`:525` HandlePeerLost 与 `NetRuntime.cs:136` UpdateReconnectPopup 本来就对，未动） |
| 战斗逻辑驱动 | `BattleManager.ResendMulligan` | `NetBattle.cs:1749` → **1810**；散文 `:1757` → **1818** | `public (MsgStart start, List<MsgAction> actions) ResumeData()`；`void OnResumed(MsgStart, List<MsgAction>)` |
| 未分类_其他 | `UIMessageController.Awake` | `ErrorMessageBanner.cs:472` → **487**，并补 `:1161` | `Create` 在文件里有**两处声明**（477 单参转发 / 487 真建池）⇒ 取真身 487；`:1161 void Awake() { EnsureSingleton(); }` 就是原版 Awake 前半截，一并写进落点 |
| 菜单_外壳 | `ParentConstraint.Awake` | `CardView.cs:2144` → **2157**；散文 `:2150`/`:2151` → `:2163`/`:2164` | `public Transform EffectAnchorBoard`（懒建锚点属性）；`new GameObject("EffectAnchor")` 在 2163、`SetParent(transform, false)` 在 2164 |
| 菜单_外壳 | `ParentConstraint.Init(Transform target)` | 同上 2144 → **2157** | 同上 |
| 菜单_外壳 | `ParentConstraint.LateUpdate` | 同上 2144 → **2157** | 同上 |
| 菜单_外壳 | `ParentConstraint.Sync` | 同上 2144 → **2157**（另 3 处证据指针 144 / 3009 / 53 现读仍然对，未动） | `ApplyInherit` / `RewireLevelUpTargets` / `BindLevelUpInheritTargets` 逐一现读确认 |
| 菜单_外壳 | `ParentConstraint.ToggleParentingOptions(bool, bool, bool)` | 同上 2144 → **2157** | 同上 |
| 菜单_外壳 | `ParentConstraint.Update` | 同上 2144 → **2157** | 同上 |

关于 6 条 `ParentConstraint`：它们的落点是**属性**（本工程没有 `ParentConstraint` 组件，跟随全由
父子关系承担 —— 见各行的「差异」列）。闸现在报 `MEMBER` 告警（方案 §4.1 例外：纯取值器/结构等价的
落点可以是字段/属性，⛔ 不算失败）。**没有为了过闸去造一个零行为转发方法。**

## 三、29 条失败逐条判档与处置

判四档：`档1` 名字还在、只是行号漂 · `档2` 名字已消失 · `档3` 账文本身错 · `档4` 判据不足。
**结果：档1 = 26 条 · 档3 = 3 条 · 档2 = 0 · 档4 = 0**（**没有任何一条要降到 B 档**）。

### 3·1 其他_基础设施.tsv（3 条，全档1）

| 方法 | 落点 旧 → 新 | 依据 |
|---|---|---|
| `PlayerDataManager.SellSpareCards(Dictionary<string,int>, bool)` | `CardDetailPopup.cs:3489` → **3496** | 旧行是 `ShouldSellCardsFromCrate` 里的 `return CurrencyExtensions...`；`public static void SellSpareCards(IDictionary<string, int>, bool)` 在 3496 |
| `PlayerDataManager.SellSpareCardsRecursive()` | `:3443` → **3525** | 3443 是 `//   · 为什么必须自己定…` 注释；`public static void SellSpareCardsRecursive(IDictionary<string, int>)` 在 3525 |
| `PlayerDataManager._003CSellSpareCardsRecursive_003Eb__602_0()` | `:3470` → **3552** | 3470 是 `/// 这里只是同一格的转发…` 注释；`public static void OnSellSpareCardsRecursiveCompleted()` 在 3552 |

### 3·2 卡牌面_渲染.tsv（10 条）

| 方法 | 落点 旧 → 新 | 档 | 依据 |
|---|---|---|---|
| `BoardTraitIcon.Initialize(Sprite, int, bool)` | `CardView.cs:3877` → **3890**；散文 `:3691` → `:3704` | 1 | 3877 是注释；`void BuildBadgeSlot(int i, Badge b)` 在 3890；`public int SetBadges(List<Badge>)` 在 3704 |
| `Card2DController.SetCardVisibility(CardVisibility)` | `:7561` → **7574** | 1 | `public void SetBackVisible(bool on, Texture2D back)` 在 7574（旧行声明的是 `_backMr`） |
| `CardServices.GetCardTier(RawCardScript)` | 主落点 `CardDetailPopup.cs:293（Level）` 本来就对；**次要证据指针** `` Core/CardView.cs:2860 `` / `` :11931 `` / `` Core/CardFeel.cs:2851 `` → **2920 / 12141 / 2864** | **3** | 2860 是空行、11931 是空行、2851 是注释 ⇒ 三处「取档口」全陈旧。现读真调用点：`CardView.cs:2920`（`int tierLevel = ... CardProgress.Level(Data.artId)`）、`:12141`、`CardFeel.cs:2864` |
| `CardTextsController.ChangeState(CardNameAndDescriptionTypes)` | `CardView.cs:5225` → **5238** | 1 | 5225 是 `return true;`；`CardTextState ComputeTextState(CardData d)` 在 5238 |
| `CardTextsController.SetDescription(string)` | `Core/CardView.cs:15568（DescUnitBandMaxForUse）· :5069` → **4936（BuildTextLayers）· :15581（属性）· :5082（调用点）** | **3** | 见 §五·1（闸的工具边界）：`DescUnitBandMaxForUse` 真身在 15581 没错，但它是**一行式属性且 getter 体内含括号**，闸结构性不接受它当落点（永远判失败）⇒ 主落点改到真正的方法 `BuildTextLayers`（4936），属性 15581 与真调用点 5082 留作证据 |
| `CardTextsController.SetTexts(string, string, CardTypeOptions, SpellType)` | `CardView.cs:3326` → **3339** | 1 | 3326 是注释；`public void SetData(CardData d)` 在 3339 |
| `CardTextsController.SetTexts(string, string, string, string, CardTypeOptions, SpellType)` | `Core/CardView.cs:15564（NameUnitBandMaxForUse）· :15729 · :4987` → **4936（BuildTextLayers）· :15577（属性）· :5000** | **3** | 同上一格：15564 是注释、属性真身在 15577（含括号、不能当落点）、`noDescName` 那一支现读在 5000（`bandMax: unit ? (noDescName ? NameUnitBandMax : NameUnitBandMaxForUse) : …`） |
| `CardTextsController.ToggleState()` | `CardView.cs:4923` → **4936** | 1 | 4923 是注释；`void BuildTextLayers(CardData d)` 在 4936 |
| `TMP_Text.CalculatePreferredValues(ref float, Vector2, bool, TextWrappingModes)` | `CardView.cs:5909` → **5922** | 1 | 5909 是 `}`；`static bool FitToBox(TextMeshPro t, float maxWidth, …)` 在 5922 |
| `TextMeshPro.OnPreRenderObject()` | `CardView.cs:5909` → **5922** | 1 | 同上 |

### 3·3 战斗逻辑驱动.tsv（7 条，全档1）

| 方法 | 落点 旧 → 新 | 依据（现读） |
|---|---|---|
| `BattleManager.CommunicateBattleAction(BattleAction)` | `NetBattle.cs:1161` → **1222**；`:1191` → **1252** | `public void OnLocalAction(AiAction a)`；`public void OnLocalRawAction(MsgAction m)`；`NetProtocol.cs:510 ToWire` 本来就对 |
| `BattleManager.ForfeitDisconnectedEnemy()` | `:813` → **857**；`:958` → **1019** | `void TickReconnectCountdown()`；`void ReconnectCountdownExpired()`；`:748 ReconnectSecondsFor` 本来就对 |
| `BattleManager.SendMulliganCards()` | `:1212` → **1273** | `public void OnLocalMulligan(int[] marks)` |
| `BattleManager.ShowDisconnectionPopup(bool)` | `:949-955` → **1010**；`:932-937` → **976**；`:786-809` → **808**；`:525-551` → **525** | `ReconnectPopup` 1010 · `ReconnectHint` 976 · `StartReconnectCountdown` 808 · `HandlePeerLost` 525（原来 4 处起点全落在注释/区间外） |
| `BattleManager.StopWaitingOpponentReconnection()` | `:1127` → **1188** | `void CancelReconnectCountdown(bool hideWindow)` |
| `CardScript.StopAnimsInPlay()` | `CardView.cs:14702` → **14715** | `public void StopInPlayAnims()`（旧行是注释） |
| `TurnExtensions.SetTurn(Room, int, bool)` | `NetBattle.cs:1370` → **1438** | `public void SendFingerprint()`（旧行是 `+ " ⇒ 这一条丢掉");` 语句中段） |

### 3·4 日常_奖励.tsv（5 条，全档1）

| 方法 | 落点 旧 → 新 | 依据（现读） |
|---|---|---|
| `RankedEventWindow.ChangeDeckButtonClick()` | `LiveOpsEventWindow.cs:2359` → **2363** | `public virtual void ChangeDeckButtonClick() { OpenDeckSelection(); }`（旧行是 doc 注释） |
| `RankedEventWindow.PlayButtonClicked()` | `:2343` → **2347** | `public virtual void PlayButtonClicked() { StartMatch(); }` |
| `RankedEventWindow.ViewDeckButtonClick()` | `:2364` → **2368** | `public virtual void ViewDeckButtonClick() { OpenDeckInfo(); }`（旧行是空行） |
| `SkirmishEventWindow.BattleButtonClick()` | `:2343` → **2347** | 与 `PlayButtonClicked` **同一个基类方法**（两扇窗共用，⛔ 不各写一份）；接线 `:1430 BattleHit` 现读仍然对 |
| `SkirmishEventWindow.HelpButtonClick()` | `:2352` → **2356** | `public virtual void HelpButtonClick() { OpenHelp(); }`（旧行是空行）；接线 `:1488 HelpHit` 现读仍然对 |

### 3·5 未分类_其他.tsv（4 条，全档1）

| 方法 | 落点 旧 → 新 | 依据（现读） |
|---|---|---|
| `UIMessageController.OnDestroy()` | `ErrorMessageBanner.cs:1151` → **1166** | `void OnDestroy() { if (ReferenceEquals(Instance, this)) Instance = null; }`（旧行是 `Debug.LogWarning` 字符串拼接段）；另把 `EnsureSingleton`（1141）写进落点 |
| `UIMessageController.ShowError(string, bool)` | `:642 ShowError(...)` → **648** | `public void ShowError(string text, bool localize = false, string fallback = null)`（旧行是 doc 注释） |
| `UIMessageController.ShowMessage(string, bool)` | `:648 ShowMessage(...)` → **654** | `public void ShowMessage(string text, Color color, bool localize = false, string fallback = null)`（旧行声明的是 `ShowError`，名字对不上） |
| `UIMessageController.ShowMessage(string, bool, Color)` | `:648 起` → **654 起** | 同上 |

## 四、改法与本轮验证

- **改 `.tsv` 的方式**：**裸 `split("\t")` / `"\t".join`**（⛔ 没用 `csv` 模块，避免引号与反斜杠往返污染）；
  只替换目标行的第 7 列（`我们的落点`），另在 `其他_基础设施` 两行的第 8 列（`差异`）订正了陈旧指针。
  每一处替换都**先断言「旧串在这一行的第 7 列里出现恰好 1 次」**，任何一条不成立就整体中止、不写盘。
- **往返核对**：写回后重新按 `\t` 切分，7 份账**每一行都是 11 列**（0 条坏行）；
  行尾仍为**纯 LF**（`b.count(b"\r") == 0`）、**无 BOM**、**文件末尾有换行**；
  换行数与改前逐一相同：808 / 1389 / 757 / 901 / 782 / 172 / 789（split 后元素数）。
  因为写入是「同一份列表原样 join 回去」，除被替换的两个单元格外的字节不可能变化。
- **目标行现读核对**：每条新行号都用 `io.open(..., 'rb')` 按二进制读源码后**逐行打印确认**
  （`\r\n` 归一为 `\n`，避开裸 CR 让行号整体偏移的坑）。

## 五、仍降不下去的：**0 条**

没有一条需要「名字已消失且找不到等价物」的硬理由 —— 29 条失败里 26 条只是行号陈旧，
3 条是账文本身的落点选择问题（见下）。**没有任何一条降到 B 档。**

### 5·1 顺手发现（交给调度台分流，本次未改工具）

1. **`check_ledger_points.py` 的工具边界**：`member_decl_kind()` 里 `if "(" in s: return None`
   排在 `_PROP_BODY` 判定**之前** ⇒ **一行式属性只要 getter 体内含括号，就永远不能当落点**，
   会被判 `)` 后接的是语句「不像声明」，或干脆 `find_decl_candidates` 找不到。
   实测：`float DescUnitBandMaxForUse { get { return _use == CardUse.Booster ? … : (_use == CardUse.MenuDetail ? … ); } }`
   （`CardView.cs:15581`，`NameUnitBandMaxForUse` 在 15577 同形）。
   本次避开它（主落点改到方法 `BuildTextLayers`），⛔ **没有改工具**（红线：本代理只许动账）。
   若将来要让这类属性合法当落点，需要动 `member_decl_kind` 的判定顺序 —— 建议另开一条工具债。
2. **`差异` 列里也有陈旧行号**：`其他_基础设施` 两行写着「生产实现 `CardDetailPopup.cs:3400` / `:3429` 不变」，
   现读两处分别是 `Debug.LogWarning` 字符串段与注释。已就地改成 **3496 / 3525**（铁律 5：证据就地订正）。
3. **这 7 份账在本代理开工前就有别人未提交的改动**（`git diff --numstat` 里
   `卡牌面_渲染 68/68`、`战斗界面_HUD 105/105`，远大于本次的 10 / 1 行；diff 里 `@@ -2`、`@@ -17` 等
   非本次目标行的 hunk 都在）。本次是**读当前字节 → 只改目标单元格 → 原样写回**，别人的改动一个字节没动。

## 六、改动清单（7 份账，38 处落点 + 2 处证据指针）

| 账 | 改动条数 |
|---|---|
| `战斗界面_HUD.tsv` | 1 行（2 处行号） |
| `战斗逻辑驱动.tsv` | 9 行（含 8 条失败 + 1 条漂移；共 11 处行号） |
| `未分类_其他.tsv` | 5 行（4 条失败 + 1 条漂移歧义） |
| `菜单_外壳.tsv` | 6 行（6 条漂移；含 10 处散文行号） |
| `其他_基础设施.tsv` | 3 行落点 + 2 行「差异」列证据指针 |
| `卡牌面_渲染.tsv` | 10 行 |
| `日常_奖励.tsv` | 5 行 |
