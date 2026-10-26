# B8_BR — `MainMenuScene.Run` 剩下的 14 条红（2026-10-26）

> 口径：⛔ 不为变绿放宽断言；⛔ 不把「我们做错了」写成「断言过期」。每条都给了**原版判据（`文件:行号`）**。
> 本代理**没有跑 Unity**（只跑了 `TMPDIR=/tmp/wf_b8br bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**，改动前后各一次）。

---

## 〇、共同根因（先说结论）

**14 条里 8 条（#3 #4 #5 #6 #7 #8 #9 #11）+ 2 条夹具（#1 #2）来自同一个根因**：

`Shell/PracticeModePopup.cs` 的 **`PlayerDeckChanged(string faction)`（`:3498-3515`）把玩家刚选中的那一副换掉了**。

- 它做的是 `ArmyIndex = ni;` 然后 **`if (!SelectDefaultDeck()) …`** ——
  而 `SelectDefaultDeck()`（`Shell/PracticeModePopup.cs:1609-1617`）会
  **① 选「你的卡组」那一格（= 卡组库里那套 `YourDeckIndex`），② 否退「该阵营第一对」**。
- 一手判据（原版，`cpp2il/out_cpp2il_dev_ilspy/PracticeModePopup.cs:854-960` 的 `SetArmyButtons`）：
  ```csharp
  PlayerDeckDemoDeckInfoWrapper w = playerDeckInfoWrapper;
  CardDeck cardDeck = w.cardDeck;                       // ← PlayerDeckChanged 上一句刚写成「新选中的那一副」
  if (cardDeck != null && cardDeck.deckHero.cardArmy == cardArmy) {
      item0.InitializeWithPracticeDeck(this, playerDeckInfoWrapper, false, playMode);  // ← 用【wrapper】建
      item0.gameObject.SetActive(true);  item0.highlightObject.SetActive(true);
      item0.deckSelectionMenu.DeckSelected(item0.demoDeck, item0);   // ← 重选 item0
      num = 1; num2 = 1;
  }
  … 只有 `num == 0 && !num2` 才 `SelectItem(item0)`（= 该阵营第一对）
  ```
  `PlayerDeckChanged`（`decomp_full/PracticeModePopup__PlayerDeckChanged.c:5-10`）先把
  `wrapper.cardDeck(+0x58) = newDeck` **再** `ChangeSelectedArmy(newDeck.deckHero.cardArmy)`
  ⇒ `SetArmyButtons` 建出来的 item0 **装的就是刚选的中那一副** ⇒ `DeckSelected(item0)` 是**重选同一副**，
  **不是**「退回卡组库那套」。
- 实测佐证（`_tmp_view/wf_menu_b2.log`）：
  - `:122981` `换了自己的卡组 ⇒ 阵营筛选跟到新卡组 …「<空>」→ 第 0 格` ← 紧接着 `#4/#6` 红（`CardRows` 实得 **31** = 卡组库那套 30+1，不是空卡组的 0）；
  - `:127207` `…「Ultramarines」→ 第 0 格` ← 紧接着 `#3 #5 #7 #8 #9 #11` 红（曲线和 30、逐档 `0 2 3 3 4 4 8 3 3` = 夹具那套 30 张，不是预组的 12）。
- **#1 #2（夹具归一）**是它的**下游**：`PlayerDeckChanged` 顺带把**阵营筛选**切到了新那副的阵营，
  而 `RebuildDeckRows` 按 `ArmyIndex` 过滤（`Shell/PracticeModePopup.cs:2020`）
  ⇒ 左列只剩 Ultramarines 那一对 ⇒ 旧夹具「视口里找一对**阵营 ≠ Ultramarines**」必然 `wantPi = −1`。
  （**这条筛选本身是原版的**：`ChangeSelectedArmy` → `SetArmyButtons`，⛔ 不是缺陷 ⇒ 要改的是**夹具**。）

⇒ **#1 #2 已在本代理白名单内改掉；#3–#9 #11 需要 `PracticeModePopup.cs` 的改动请求（见 §三）**。

---

## 一、逐条定性

| # | 断言（简） | 定性 | 根因 / 判据 | 处理 |
|---|---|---|---|---|
| 1 | `（夹具归一）选预组之前 Selected Army Title ≠ 「极限战士」` | **(δ) 夹具前提**（根因 = 产品侧 β，见 §三） | 见 §〇（筛选被切到 UM ⇒ 下面那格没点成 ⇒ 标题本来就没换） | ✅ 本文件改夹具 |
| 2 | `（夹具归一·前提）视口里找得到一对阵营 ≠ Ultramarines` | **(δ) 夹具前提** | 同上；`PracticeModePopup.cs:2020` 按 `ArmyIndex` 过滤 | ✅ 本文件改夹具 |
| 3 | `选中预组 ⇒ 写进 PendingSource` | **(β) 实现缺陷**（今天批次 6 `G15` 新接的一条路走歪） | §〇 | ⛔ 需改 `PracticeModePopup.cs`（§三·请求 1） |
| 4 | `选中「我的卡组」第 2 套 ⇒ DeckIndex = 1、预组/那一对作废` | **(β)** | 同上（`SelectDefaultDeck()` 把 `DeckIndex` 换成 `YourDeckIndex`） | 同上 |
| 5 | `点一副预组 ⇒ PickedPrebuilt = UM_SK_1` | **(β)** | 同上（`PickDeck` 把 `PickedPrebuilt` 清成 null） | 同上 |
| 6 | `换到空卡组 ⇒ 卡列表 0 行` | **(β)** | 同上（实得 31 = 卡组库那套 30+防御卡） | 同上 |
| 7 | `★ 费用曲线 9 行之和 = 12` | **(β)** | 同上（实得 30） | 同上 |
| 8 | `★ 费用曲线逐档 = 0 0 1 1 5 1 0 2 2` | **(β)** | 同上（实得 `0 2 3 3 4 4 8 3 3`） | 同上 |
| 9 | `★ 卡表 12 + 防御卡 1 = 13 行` | **(β)** | 同上（实得 30） | 同上 |
| 10 | `★ 按格号错开：第 2 格每一件都压第 1 格每一件` | **(α) 断言过期**（守的是**机制**，2026-10-25 `E4` 有意改成「允许一对同号」） | `Shell/AllianceMemberTab.cs:94-116`（`QCellBase 3210` · `QCellStride 6` · `QCellMax 15` · `LC_Ring = -1`） | ✅ 本文件改写（**没有放宽**，见 §二） |
| 11 | `` `TakePendingBattleDeck()` 拿得到 `` | **(β)** | 同 §〇（`PickPrebuilt` 里才 `SetPendingBattleDeck`） | 同上（§三·请求 1） |
| 12 | `Player Level Text` = `-` | **(α) 断言过期** | 原版 `PlayerProfileUIController.SetPlayerLevel` ⇒ `level.ToString()`；`-` 只是 prefab **出厂占位**。2026-10-26 用户裁定 (b) 后已接本地等级（`Shell/CardTierConfig.cs:193-213`） | ✅ 本文件改期望值（实得「1」= 新号等级） |
| 13 | `Banned card in deck` 建了但关着 | **(α) 断言过期** | 原版 `OnDeckSelected`：`HasValidDeckWithValidationMessage()` 不通过才写这一行；2026-10-25 `E` 已接成动态（`Shell/SkirmishEventWindow.cs:760-796`）。此刻当前卡组 = **空卡组** ⇒ `TooFewCards` ⇒ **开、文案「无效卡组」**（log `:149886`） | ✅ 本文件改成动态不变式 |
| 14 | `SelectHit 悬停没换图`（×2） | **(γ) 本会话回归**（`N5` 的闸 + 审计没跟上） | `WindowButton.Enter()` 2026-10-25（`N5`）加了原版的 `if (m_Interactable)` 闸（`Shell/WindowButton.cs:498-509`）；而 `AuditHoverSwap`（`:382-402`）**不看 `Interactable`** ⇒ 把「本来就该不亮的禁用钮」判成缺陷。禁用源 = `Shell/AvatarTab.cs:637` / `Shell/TitleTab.cs:502`（`Interactable = has && PreviewIndex != 已装备` ⇒ 预览停在已装备那件 = 关） | ⛔ 需改 `Shell/WindowButton.cs`（§三·请求 2） |

---

## 二、本代理改了什么（**只动 `Editor/MainMenuScene.cs`**）

### ① 夹具归一（#1 #2）— `:5595-5659`，净 **+43** 行
原来靠「左列恰好跨阵营列出」这个**偶然**前提。改成走本窗自己的正规入口：
先点一颗「**阵营 ≠ `first.faction`** 且本模式下有卡」的**阵营格**（`ArmyCells[i]` 的 `Hit` ⇒ `PickArmy(i)`，
= 原版 `ChangeSelectedArmy` 那条入口），**再**点该阵营的第一对。
- 新增三条**硬前提**（不是 `if` 包裹的静默跳过）：找得到那颗阵营格 / 命中区在 / 该档在视口里有格可点；
- 新增一条 `Check(pw.ArmyIndex, wantArmyN)`（筛选**真的**切过去了 —— 否则下面的「标题变了」没有意义）；
- 内层循环 `break` → `continue`（被筛掉/视口外的格**不建**，`break` 会因跳号提前收工）。
- **鉴别力**：比改前更强 —— 改前抽到同阵营就「恒真」，改后显式摆到另一档。

### ② `Player Level Text`（#12）— `:7294-7311`，净 **+16** 行
期望值 `"-"` → `CardTierConfig.PlayerLevel.ToString(InvariantCulture)`；
**⛔ 不读 `CardTierConfig.PlayerLevelText` 那个串口**（那是被测实现的下游，读了就成恒等式），
改成在断言里自己把 **int** 格式化 ⇒ 仍能咬住「印成 `-` / 印成 `Lv.1` / 区域设置改了数形」。
另补一条 `gotLv != "-"`（占位串回来立刻红）。

### ③ `Banned card in deck`（#13）— `:9451-9487`，净 **+36** 行
「建了但恒关」→ **动态不变式**：`activeSelf == (当前这副牌校不通过)`。
- 当前这副牌从**本窗自己的公开状态**取（`sk.PickedPrebuilt` / `sk.DeckIndex` / `sk.HasEffectiveDeck`）；
- 校验用 `RuleEngine.DeckRules.Validate(...)`（引擎是规则权威，本文件别处也这么用）；
- 文案**不读 `DescribeShort`**（被测那一侧），按原版唯一的两条短档键判：
  `MenuDeck/Error/InvalidDeck/Short` 或 `MenuDeck/Error/InvalidDeckBannedCards/Short`
  （`DeckUtility__ToShortRawLocalizationString.c:21-24` 常量兜底 ⇒ 其余 13 档一律落前者）。

### ④ 奖杯格队列带（#10）— `:13676-13727`，净 **+46** 行
`bMin > aMax`（**每一件**都严压）→ 改成守**真正的不变式**，而且**没有放宽**：
1. `bMin >= aMax`（⛔ 不许反序）——「同类同号」那版会得 `3209 < 3215` ⇒ **本条真红**（改坏法不变）；
2. **只在 `bMin == aMax` 时**再查「同号的两颗必须恰好是 第 2 格 `Collectable Highlight`（`LC_Ring`）+ 第 1 格的文字（`LC_Text`）」
   （写成条件，将来谁加宽带子回到严格版**不会假红**）；
3. 第 2 格**除最底那颗 Ring 之外**的每一件都 `> aMax`（次低号 vs 第 1 格 max）。

> **为什么这不是「把我们的错写成断言过期」**：`7 层 × 15 格 = 105 > 90 个号`，
> 带子 `3210–3299` 里**装不下第 8 个号**（推导 → `Shell/AllianceMemberTab.cs:69-116`）；
> 被牺牲的那一对（`LC_Text` ↔ 下一格 `LC_Ring`）**几何上不重叠** ⇒ 视觉等价。
> ⚠️ 但它属于铁律 18 的「机制不同、视觉等价」⇒ **建议 `AllianceMemberTab.cs` 那条注释加 `🔶 自己设计` 标记**
> （现在只写了「代价（如实记）」）。

---

## 三、白名单外的**改动请求**（两条，缺了这 9 条红下不去）

### 请求 1（8 条红：#3 #4 #5 #6 #7 #8 #9 #11）— `Shell/PracticeModePopup.cs`

`PlayerDeckChanged`（`:3498-3515`）里**删掉那次「默认选中」**，改成「只重列、不动选中」：

```csharp
            ArmyIndex = ni;
            // 原版 `SetArmyButtons(army)` = 按新阵营**重列**；而它末尾那句 `DeckSelected(item0)` 里的 item0
            // 是用**刚写进 wrapper 的那一副**建出来的（`out_cpp2il_dev_ilspy/PracticeModePopup.cs:854-960`）
            // ⇒ 它是**重选同一副**，⛔ 不是「退回卡组库那套」⇒ 这里**不许**调 `SelectDefaultDeck()`。
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
```
（替换原来的 `if (!SelectDefaultDeck()) ShowNoDeckSelected(...)` 三行。）

配套说明：
- `RebuildDeckRows` 末尾自己会刷 `_txtArmy`（`:2035`）⇒ 阵营标题跟着换，`⑥①` 那条仍然有意义；
- `PickedPrebuilt` / `DeckIndex` / `PairIndex` **一个字都不动** ⇒ #4 #5 #6 #7 #8 #9 #11 全部回绿；
- 原注释里那句「⚠️ 一条与原版一致的怪行为…这一圈会把它换回第一对」**判据有误**：
  原版换回的 item0 装的是**新选的那一副**（见上面那段 cpp2il），不是「该阵营第一对」
  —— 建议就地订正（铁律 5），否则下一个人会照着它再写一遍 `SelectDefaultDeck()`。
- **已知遗留（记一笔，本轮不必做）**：原版这一圈会把 item0 **高亮**起来（`highlightObject.SetActive(true)`，可见），
  我们的「你的卡组」格高亮条件是 `PickedPrebuilt == null && DeckIndex >= 0` ⇒ 选了**预组**时一格都不亮。
  ⇒ 属于「可见的小偏离」，按铁律 11 **记着**（不是不做）。

### 请求 2（1 条红：#14）— `Shell/WindowButton.cs`

`AuditHoverSwap`（`:382-402`）与镜像 `AuditPressedSwap`（`:437-…`）在挑钮时**跳过禁用钮**：

```csharp
if (wb == null || wb._hoverTex == null || wb.target == null) continue;
if (!wb.Interactable) continue;   // = 原版 `OnPointerEnter` 的 `if (m_Interactable)` 闸（N5，见 Enter() 的 doc）
```
理由：`Enter()`/`Press()` 已经在 `N5`（2026-10-25）按原版加了 `if (!_interactable) { ShowTip(); return; }` 闸
（`Shell/WindowButton.cs:498-509` / `:539-542`）⇒ 禁用钮**本来就不该换图**，审计再问它就是**假红**。
今天踩到的是 `AvatarTab.cs:637` / `TitleTab.cs:502` 那两颗 `SelectHit`（预览停在已装备那件 ⇒ `Interactable = false`）。

---

## 四、改了哪些行 / 账锚点位移

`Editor/MainMenuScene.cs`：**LF，无裸 CR**（`b.count(b'\r\n')=0`、`b.count(b'\r')-b.count(b'\r\n')=0`），
**19,942 → 20,083 行（净 +141）**。

| 编辑 | 位置（改后） | hunk（旧→新） | 净 |
|---|---|---|---|
| ① 夹具归一 | `:5595` 起 | `-5519,3 +5596,18` · `-5523,6 +5615,10` · `-5530,3 +5626,3` · `-5534,5 +5630,29` | **+43** |
| ② Player Level | `:7294-7311` | `-7174,2 +7294,18` | **+16** |
| ③ Banned card | `:9451-9487` | `-9314,2 +9450,38` | **+36** |
| ④ 奖杯队列带 | `:13676-13727` | `-13504,5 +13676,51` | **+46** |

**锚点位移（本代理这一份）**：

| 原行号区间 | 位移 |
|---|---|
| `< 5519` | 0 |
| `5519 – 7173` | **+43** |
| `7174 – 9313` | **+59** |
| `9314 – 13503` | **+95** |
| `≥ 13504` | **+141** |

**账主表里只有 3 条**引用了 `Editor/MainMenuScene.cs` 的行号（都在这条线以上，全部受影响）：

| 账文件 | 原行号 | 本代理位移后 | 实测当前落点 | 备注 |
|---|---|---|---|---|
| `泛型与启动链_补读.tsv`（`ArmyItem<T>.TryGetItem` 读口） | `6829-6848` | `6872-6891` | `6952-6971`（语句在 `:6968`） | ⚠️ **另有 +80 是本会话别人改这个文件留下的**（hunk `-3941 +3942,51`、`-4321 +4372,29`）⇒ 合计 +123 |
| `社交_聊天.tsv`（`AlliancesEventScorePanel` 建造点） | `15834` | `15975` | `16101`（`AllianceEventScorePanel.Create`） | ⚠️ 另有 +126 是**入账时就已过期**的旧漂移 |
| `菜单_外壳.tsv`（`WhereToGetCards` 观测量） | `17410` | `17551` | `17629`（`Check(wc.SectionsShown, 1, …)`） | ⚠️ 另有 +78 旧漂移 |

⇒ **同步点 §13·6 跑 `check_ledger_points --all-A --fix` 时要按「实测当前落点」重钉**（⛔ 别按我给的中间值）。
本代理**没有**写 `账/_parts/B8_BR.tsv`（这些是行号漂移、不是状态变更，`--fix` 统一处理）。

---

## 五、没查清 / 未验的部分（如实）

1. **没跑 Unity**（红线）⇒ 我这 5 条改动的绿/红**没有实跑验证**，只有 `typecheck` 0/0。
   重跑单：`-executeMethod MainMenuScene.Run`（只这一条；主菜单宿主，~24s + 建场）。
2. **#14 的两个可能诱因里我只证到「禁用」这一条**：`Enter()` 还有 `if (Hovered) return;` 那一档
   （若指针恰好停在那颗上，审计也会读到「没换图」）。但两颗 `SelectHit` **同时**红、且它们都在
   `Interactable = false` 的那一态（`AvatarTab.Refresh` / `TitleTab.Refresh` 的表达式 + `TitleTab.OnOpen()`
   把预览复位到已装备那件），⇒ 判为禁用这一支。**§三·请求 2** 落地后若仍红，请按 `Hovered` 那一支再查一次。
3. **#12 的新期望值依赖 `CardTierConfig.PlayerLevel`**（新号 = 1）。若将来把 `XpAward` 数据填成非零，
   这条会跟着变（这正是它该有的行为），但**必须**在断言里现算（已经这么写了），⛔ 别回写成 `"1"`。
4. **#13 的「这是哪一副牌」那一步**与实现用的是同一份公开状态（`PickedPrebuilt`/`DeckIndex`/`HasEffectiveDeck`）
   —— 这一半是**不得已**（窗外没有第二个来源）。真正独立的判据在**文案那半**（两条官方短档键 + 常量兜底规则）。
5. **#10 的「视觉等价」是照注释与几何推的，没有实拍**：`Collectable Highlight` 只画 52px 描边、
   `title`/`counter` 在格内 `y+237` 起 —— 若将来有人把 `title` 挪到格子边缘，这一档就会**变成可见缺陷**
   （那时要么加宽带子、要么把 `title` 挪回格内）。
6. `pw.ArmyCells` 的 `Hit` 走 `HitOn(... ⇒ PickArmy(idx))`（`Shell/PracticeModePopup.cs:1887`）——
   我按这条接的；若将来 `PickArmy` 换了入口，夹具那一段要跟着改。

---

## 六、预计剩几条

| 场景 | 剩余红 |
|---|---|
| **只落本代理的 5 条改动**（`MainMenuScene.cs`） | **9**（#3 #4 #5 #6 #7 #8 #9 #11 = 请求 1；#14 = 请求 2） |
| **+ 请求 1（`PracticeModePopup.cs`）** | **1**（#14） |
| **+ 请求 1 + 请求 2** | **0** |

重跑单（同步点用）：`MainMenuScene.Run`（单条；本批只动了这一个宿主 + 两个白名单外的请求文件）。
