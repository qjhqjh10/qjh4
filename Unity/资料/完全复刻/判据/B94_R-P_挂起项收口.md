# B94 波 1.5 · R-P 挂起项收口（只读取证代理 · 第九十四会话）

> **输入** = 两份出处补全报告里「需再读一个文件才能定论」的挂起表：
> · `判据/B94_R-A2_出处补全_非战斗.md:524-541`（14 条）
> · `判据/B94_R-A3_出处补全_其余.md:155-169`（11 条）
>
> **本报告只做一件事**：按那两张表逐条去读，能定的定死、定不了的写清还差什么。
> **不改源码 / 账 / 正本**（铁律 13·3），⛔ 未跑 Unity、未动 git、未动 `d:/2`。
>
> 结果：**25 条里 21 条定死**（含 3 条「其实早就定死了、只是被记成挂起」）· **4 条仍挂**（各写了下一步读哪一份/跑哪个工具）。
> 顺带查出 **6 条账记错**（§三），其中 2 条会影响 A/B 定档。

---

## 〇 · 本批用到的新能力（下一批可直接复用，别再撞）

| 能力 | 怎么用 | 本批用它解掉了 |
|---|---|---|
| **`out_b94` 泛型类真方法体** | `D:/2/tools/decomp_generic_trial/out_b94/INDEX.tsv`（7 列，**按 INDEX 找文件**，别按首行函数名） | R-A3 **H1**（`DraftEventBase` 6 个方法）、R-A2 **#10** |
| **`out_il2csharp` 逐方法真体** | `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/<类>.cs`（按 `// RVA:` 注释定位） | **#1**（`get_ActiveEvent` 的谓词名）、**#5**（匿名类型与回写段） |
| **AssetRipper 桩文件的【字段序】** | `D:/2/tools/assetripper_out/ExportedProject/Assets/Scripts/Assembly-CSharp/<类>.cs` —— 桩只有声明，但 **`[SerializeField] private` 的声明顺序 = 序列化顺序 = 偏移顺序**（MonoBehaviour 首字段在 `0x28`） | **#2**（两个 sprite 谁是谁）、**#3**（`+0x28` 是序列化字段）、**#8**（4 个 GameObject 的名字）、**#7** |
| **`m_Script` pathID → monoscripts → prefab 实例反查** | 先 `grep -rl "<类名>" bundle_*monoscripts/**` 拿 pathID，再 `grep -rl "\"m_PathID\": <pid>" bundle_<包>_assets_all/MonoBehaviour/` | **H10**（`LiveopMenuContainer` 4 份实例在本地） |
| **`read_il2cpp_string.py`**（`.data` 里的 I2/字面量槽） | `python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py DAT_1842xxxxx` | **#9**（三条词条键）、**H7**（格式串） |
| **`read_literal.py`**（`.rdata` 裸数值） | 同 `/工具/read_literal.py <dll> 0x1834xxxxx` | **#6**（100.0）、**#11**（7 个浮点常量 + 第 8 个 `0.2`） |
| **`dump.cs` 的 `GenericInstMethod` 段** | `grep -n -B4 "RVA: 0x<rva> " dump.cs` 能把一个 `FUN_xxxx` 反解成真正的 BCL 方法名 | 全批（`OrderBy`/`Select`/`OrderByDescending`/`Aggregate`/`Where`/`DefaultIfEmpty`/`FirstOrDefault` 都是这么反解出来的） |

---

## 一 · R-A2 §二·二（14 条）

### 1｜`GameModes.GetActiveEvent(Type)` 的三个筛选谓词

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:528` |
| 账行 | `其他_基础设施.tsv:218`（**B**） |
| 原版方法 | `GameModes.GetActiveEvent(Type gameModeType)` |
| 读了 | `decomp_full/GameModes__GetActiveEvent.c` · `GameModes.__c__DisplayClass39_0___GetActiveEvent_b__0.c` · `GameModes.__c___GetActiveEvent_b__39_1.c` · `GameModes.__c___GetActiveEvent_b__39_2.c` · `cpp2il/out_il2csharp/Assembly-CSharp/GameModes.cs:359-520` · `dump.cs`（`LiveOpState` / `ILiveOps`） |

**读到的东西（逐句）**

```
public IPlayEvent GetActiveEvent(Type gameModeType)
{
    var cfg = ConfigManager.GetConfig<GameModesConfig>();

    if (gameModeType != typeof(IRankedEvent))
        return GetEvents(false)
                 .Where(g => ReflectionHelper.Is(g, gameModeType))          // ① DisplayClass39_0::b__0
                 .Where(g => g.EventState == LiveOpState.Active)            // ② b__39_1：访问器 == 2
                 .OrderByDescending(g => g.Priority)                        // ③ b__39_2：ILiveOps.Priority（int）
                 .FirstOrDefault();

    // IRankedEvent 支
    var t = cfg.mainRankedType.Type;
    if (t == null)                        → 上面那同一条查询（只是 gameModeType 就是 IRankedEvent）
    else if (t != typeof(IRankedEvent))   → 同上
    else { var r = GetActiveEvent(t); return r ?? 同上那一条查询; }
}
```

- ② 的判据：`b__39_1` = `FUN_1800021f0(0x17, …) == 2`；`out_il2csharp` 直接写成 `g.EventState == 2`；`dump.cs` 的 `LiveOpState { NotAvailable=-1, Planned=0, OnPreview=1, **Active=2**, Finished=3, Expired=4 }` ⇒ **`== LiveOpState.Active`**。
- ③ 的判据：`b__39_2` 在 `out_il2csharp:352` 是 `(int)sub_1800021f0(21, typeof(ILiveOps), g)`，`ILiveOps` 的 `Priority` 是 int 属性（`dump.cs:126037`）⇒ **`OrderByDescending(g => g.Priority)`**。
- `GetEvents(false)`：`(**(code**)(*param_1 + 0x2e8))(param_1, 0, …)`。

**结论**：✅ **定死**。三个「谓词」实为 **两个 Where + 一个 OrderByDescending 键**：
① `ReflectionHelper.Is(g, gameModeType)` ② `g.EventState == LiveOpState.Active(2)` ③ 排序键 `g.Priority`（降序）。

**对账的影响**：`其他_基础设施:218` 的「判据要点」把这三句写死；「还差什么」一栏清空。

---

### 2｜`CampaignPremiumPointsPanel.ChangeState` 的 state→块 映射

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:529` |
| 账行 | `商店.tsv:36`（**B**） |
| 原版方法 | `CampaignPremiumPointsPanel.ChangeState(State state)` |
| 读了 | `decomp_full/CampaignPremiumPointsPanel__ChangeState.c`（全文 114 行）· `assetripper_out/.../CampaignPremiumPointsPanel.cs:1-70`（字段序 + `State` 枚举） |

**读到的东西（逐句）**

字段序（桩的声明序 ⇒ 偏移）；`MonoBehaviour` 首字段在 `0x28`：

| 偏移 | 字段 | 依据 |
|---|---|---|
| `+0x30` | `pointsObject` (GameObject) | `SetActive` 直调（第三字段） |
| `+0x38` | `timerDisplay` | `.gameObject.SetActive` |
| `+0x48` | `collectButton` (EverguildButton) | `.gameObject.SetActive` + 标签宿主 |
| `+0x50` | `collectText` (LocalizedString) | `case 0` 的文案 |
| `+0x68` | `becomePremiumText` | `case 1` 的文案 |
| `+0x80` | `panelBackground` (Image) | `.sprite =` |
| `+0x88` | **`collectedSprite`** | `case 2` 用 |
| `+0x90` | **`notCollectedSprite`** | `case 0/1` 用 |
| `+0x98` | `backgroundShinyEffect` (UIEffect) | `Behaviour.set_enabled` |
| `+0xA8` | `currentState` | 函数第一句 `*(int*)(this+0xA8) = state` |

`State { PremiumPurchased = 0, PremiumNotPurchased = 1, AlreadyCollected = 2 }`（桩里明写）。

```
ChangeState(State s):
  currentState = s;
  switch (s) {
    case 0 (PremiumPurchased):
      collectButton.gameObject.SetActive(true);
      pointsObject.SetActive(true);
      timerDisplay.gameObject.SetActive(false);
      panelBackground.sprite = notCollectedSprite;
      collectButton 上的 TMP(+0x1a0).text = collectText;
      backgroundShinyEffect.enabled = true;
    case 1 (PremiumNotPurchased):
      同 case 0，唯一差别：text = becomePremiumText;      // sprite 仍是 notCollectedSprite
    case 2 (AlreadyCollected):
      collectButton.gameObject.SetActive(false);
      pointsObject.SetActive(false);
      timerDisplay.gameObject.SetActive(true);
      panelBackground.sprite = collectedSprite;
      backgroundShinyEffect.enabled = false;
      **不写标签**（提前 return）
    default: return;   // 其它值什么都不做
  }
```

**结论**：✅ **定死**。原挂起的两点都答了：
- 「枚举序 ≠ 块序」→ 枚举序恰好等于块序（0/1/2 三块），但 **0 与 1 共用同一块**（只差文案）；
- 「两个 sprite 哪个是 collected」→ **`+0x88` = `collectedSprite`（只有 `AlreadyCollected` 用）；`+0x90` = `notCollectedSprite`（0/1 都用）**。

**对账的影响**：`商店:36` 判据要点按上表写死；顺带补一条**「case 2 不更新标签」**（这条原账没写，是做实现时最容易漏的）。

---

### 3｜`CarouselOfferPopupDrawer.DrawRewards` 的两个谓词 + 阈值

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:530` |
| 账行 | `商店.tsv:44`（**B**） |
| 原版方法 | `CarouselOfferPopupDrawer.DrawRewards(IEnumerable<RewardInfo> rewards)` |
| 读了 | `decomp_full/CarouselOfferPopupDrawer__DrawRewards.c` · `CarouselOfferPopupDrawer.__c___DrawRewards_b__4_0.c` · `..._b__4_1.c` · `cpp2il/out_cpp2il_dev_ilspy/CarouselOfferPopupDrawer.cs:26-90` · `assetripper_out/.../CarouselOfferPopupDrawer.cs`（字段序） |

**读到的东西（逐句）**

```
DrawRewards(rewards):
  var listA = rewards.Where(谓词①).ToList();
  var listB = listA.SpliceWhere(谓词②).ToList();      // ListExtensions.SpliceWhere：原地摘出、返回被摘的
  foreach (r in listB) DrawItemReward(r.Item, r.Quantity);    // r.<0x208>() = Item，r.<0x218>() = Quantity
  foreach (r in listA) DrawItemReward(r.Item, r.Quantity);
  if (listA.Count + listB.Count < this.thresholdForEnableAutoScroll)      //  ← +0x28
      this.scroll.<某 bool @+0x22> = false;                                //  ← +0x30 = scroll
```

- **谓词②** = **定死**：`x => x.Item is Currency`（ILSpy `CarouselOfferPopupDrawer.cs:68` 明写 `typeof(Currency)`；`.c` 里是 isinst 对 `DAT_184246188`）。语义 = **货币奖励单独挑出来，先画**。
- **阈值 `+0x28`** = **定死 = 序列化字段**：桩的**第一个**字段是 `[SerializeField] private int thresholdForEnableAutoScroll;` ⇒ `0x28`；`+0x30` = `scroll`（`UI_InfiniteScroll`）、`+0x38` = `contentAnchor`（`Transform`），与 `.c` 里 `+0x30` 被当对象写 `+0x22` 那格吻合。
- **谓词①** = ⛔ **仍差**：`x => x.Item.<虚方法>()`（bool）。两份产物都只到「`Il2CppClass<ObtainableItem>+1C8` 上取方法指针 + `+1D0` 取 methodInfo，然后 indirect jump」——形状是**泛型虚方法**（`(**(code**)(*obj+slot))(obj, *(*obj+slot+8))`），具体是哪一支**没解出来**。

**结论**：**2/3 定死、1 仍差**。阈值是序列化 int（不是常量），谓词②是 `Item is Currency`，谓词① 只差「哪个 0 参 bool 泛型虚方法」。

**对账的影响**：`商店:44` 判据要点写死「Where(?) → SpliceWhere(is Currency) → 先画货币再画其余 → 条目数 < thresholdForAutoScroll 关滚动」；把「还差：谓词① 的虚方法身份（要按 VA `0x180811xxx` 反汇编那一跳）」留在备注里。

---

### 4｜`ExpansionPassPurchasePremiumPanel.<PurchasePremiumClick>b__12_1` 的委托签名

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:531` |
| 账行 | `商店.tsv:92`（**B**） |
| 原版方法 | `<>c__DisplayClass12_0.<PurchasePremiumClick>b__12_1()` |
| 读了 | `decomp_full/ExpansionPassPurchasePremiumPanel___PurchasePremiumClick_b__12_1.c` · `cpp2il/out_cpp2il_dev_ilspy/ExpansionPassPurchasePremiumPanel.cs:441-448` · `assetripper_out/.../ExpansionPassPurchasePremiumPanel.cs` |

**读到的东西**

```csharp
private void _003CPurchasePremiumClick_003Eb__12_1()
{
    Action action = onClosePurchasePopup;
    if (onClosePurchasePopup != null) { action(); }
}
```

`.c` 同一个结论：`lVar1 = this.<+0x60>`（= `onClosePurchasePopup`），`(**(code**)(lVar1+0x18))(*(lVar1+0x40), *(lVar1+0x28))` —— 委托 invoke。

**结论**：✅ **定死**：委托类型 = **`System.Action`（无参无返回）**，语义 = `onClosePurchasePopup?.Invoke()`。
顺带：`b__12_0()` = `Initialize(this.<+0x58> /*ExpansionPassEvent*/, this.<+0x60> /*Action*/, false)`（重初始化那一跳）。

**对账的影响**：`商店:92` 判据要点写「`() => onClosePurchasePopup?.Invoke()`」，不再写「jump table 未复原」。

---

### 5｜`PlayerDataManager.FixSkirmishDeck` 的 payload 字段名 + 回写段

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:532` |
| 账行 | `其他_基础设施.tsv:381`（**C**） |
| 原版方法 | `PlayerDataManager.FixSkirmishDeck(DeckInventory)`（async 状态机 `_d__466`） |
| 读了 | `decomp_full/PlayerDataManager._FixSkirmishDeck_d__466__MoveNext.c:230` · `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:3254-3500` · `cpp2il/out_cpp2il_dev_ilspy/--f__AnonymousType64.cs` · `dump.cs`（`AzureDeckInfo`） |

**读到的东西（逐句）**

```
var decksData = deckInventory.ProcessSkirmishDecksToUpload();
if (decksData == null || decksData.Count == 0) { CustomDebug.LogError("No decks to create for skirmish"); return; }

var arr = new AzureDeckInfo[decksData.Count];
foreach (var deck in decksData) {
    var info = new AzureDeckInfo(                     // 11 参 ctor（无 cards）
        deck.deckId, deck.deckName, deck.deckHero.uniqueId,
        cardback: "", warcry: "", deckTag: 0, characterId: "",
        lastEditTime: deck.<DateTime @+0x68>.Ticks,
        prebuiltDeckId: ?, gameMode: deck.GameMode, defenseCard: "");
    foreach (var id in deck.cardLibrary.Select(c => c.uniqueId))     // b__466_0 = card => card.uniqueId
        if (!info.cards.TryAdd(id, 1)) info.cards[id] = info.cards[id] + 1;
    arr[i] = info;
}

var payload = new <>f__AnonymousType64<string>(JsonWrapper.SerializeObject(arr));   // 属性名 = **Decks**
var res = await PlayfabWrapper.GenericCloudScriptHandlerAsync<SetTutorialResult>(
              CloudHandle.updatePlayerDecksArray, payload);

foreach (var deck in decksData) {            // ← 回写段
    deck.syncedToServer = true;
    deckInventory.Add(deck, 1);
}
```

- 匿名类型字段名 = **`Decks`**（`--f__AnonymousType64.cs:5` `public <T> Decks => ...`；ctor 形参也写 `Decks`）⇒ **JSON 里那一格叫 `Decks`**。
- `AzureDeckInfo` 12 参 ctor 的形参名（`dump.cs` 原版签名）：`(id, name, hero, cardback, warcry, deckTag, characterId, cards, lastEditTime, prebuiltDeckId, gameMode, defenseCard)`；本处走的是 **11 参（无 `cards`）** 那一支，`cards` 事后逐张填。

**结论**：✅ **定死**（payload 名 + 云函数句柄 + 回写两格）。唯一没逐字坐实的是 `prebuiltDeckId` 那个实参（Ghidra 丢值、ILSpy 复用了别的局部）；它不影响「回写哪一格」这个原问题。

**对账的影响**：`其他_基础设施:381`（C）—— 「判据要点」写全 payload 与回写；这一条现在**有足够判据去做**，C 的理由若只是「判据缺」，可改判 B/A。

---

### 6｜`PlayerDataManager.LoadPlayerData` / `SavePlayerData` 的两个 `DAT_` 真值

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:533` |
| 账行 | `其他_基础设施.tsv:427` / `:481`（均 **B**） |
| 读了 | `decomp_full/PlayerDataManager__LoadPlayerData.c:76,85,94` · `PlayerDataManager__SavePlayerData.c:77,86,95` · `工具/read_literal.py` |

**读到的东西**

| 常量 | 真值 | 用在哪 |
|---|---|---|
| `DAT_1834b2e08` | **4 字节 float `100.0`** | 读：`float = (float)intField / 100`；写：`int = (int)(floatField * 100)` —— **百分比换算**，三个字段各一次（`+0xc0`/`+0xd4`/... 三个 float 与存档三个 int 对转） |
| `DAT_1834b2e60` | ⛔ **不在这两个方法里**（`grep -l` 全 `*PlayerDataManager*` **0 命中**）；它是全局 `int.MaxValue`（4 字节 int = `2147483647`），出现在 `Fake3DSpriteMovement.Update` 等无关处 | —— |

**结论**：✅ **定死**。`100.0f` 是百分比换算因子；`DAT_1834b2e60` 那条是**记错位置**（对这两条账无影响）。

**对账的影响**：`:427`/`:481` 备注里把「浮点/整数真值未坐实」删掉，改写成「÷100 / ×100」；另外在**全量反编译复核**那一类清单里记一句「`DAT_1834b2e60` = `int.MaxValue`，勿再当成 PlayerDataManager 的常量」。

---

### 7｜`OpenEventButton.SetButtonInfo` 的 label 顺序 + `LoadAsset` 槽号

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:534` |
| 账行 | `日常_奖励.tsv:549`（**B**） |
| 读了 | `decomp_full/OpenEventButton__SetButtonInfo.c`（全文）· `dump.cs`（`EventLabelReferenceType` / `EventAssetReferenceType` / `LiveOpsAssetUtility` 签名） |

**读到的东西（逐句）**

```
SetButtonInfo():
  if (icon(+0x30)) icon.sprite = LiveOpsAssetUtility.LoadAsset(ev, EventAssetReferenceType.Icon /*0*/);
  if (overIcon(+0x38)) {
      overIcon.enabled = false;
      if (LiveOpsAssetUtility.HasAsset(ev, EventAssetReferenceType.OverIcon /*9*/)) {
          var s = LoadAsset(ev, 9);
          if (s) { overIcon.sprite = s; overIcon.enabled = true; }
      }
  }
  if (TryGetLabel(ev, EventLabelReferenceType.NavigationBarTitle /*4*/, out label))   // ← 先试 4
  { labelObject(+0x50).SetActive(true); text(+0x48).text = label; }
  else if (TryGetLabel(ev, EventLabelReferenceType.Title /*0*/, out label))          // ← 再试 0
  { labelObject(+0x50).SetActive(true); text(+0x48).text = label; }
  else labelObject(+0x50).SetActive(false);                                          // ← 都没有 ⇒ 藏

  if (LiveOpsUtility.IsTimedEvent(ev, out var end)) { timerObject(+0x58).SetActive(true); timerDisplay(+0x40).Initialize(end); }
  else timerObject(+0x58).SetActive(false);
```

**结论**：✅ **定死**：
- **label 顺序 = 4 → 0**（即 `TryGetLabel(ev, NavigationBarTitle) ?? TryGetLabel(ev, Title)`，**不是** `0 ?? 4`）；
- **图标槽 = `EventAssetReferenceType.Icon(0)`**；**角标槽 = `OverIcon(9)` 且先 `HasAsset(ev,9)` 门控**（无资产 ⇒ 保持 disabled）。

**对账的影响**：`日常_奖励:549` 判据要点写死这四条（原账的 `4 ?? 0 还是 0 ?? 4` 一栏清空）。

---

### 8｜`TwoSidesEvent_OnGoingTab.OnOpen` 的 4 个 `SetActive` 判据

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:535` |
| 账行 | `日常_奖励.tsv:774`（**B**） |
| 读了 | `decomp_full/TwoSidesEvent_OnGoingTab__OnOpen.c:137-176` · `assetripper_out/.../TwoSidesEvent_OnGoingTab.cs`（字段序） |

**读到的东西**

字段序 ⇒ 偏移：`+0x38` = `teamDrawers[]`（与 `.c` 里当 `List`/数组用一致）· `+0x48` `yourTeamTeamATextObject` · `+0x50` `yourTeamTeamBTextObject` · `+0x58` `scoringSliders` · `+0x68` `backgroundImage` · `+0x70` `teamABonusObject` · `+0x78` `teamBBonusObject`。

```
var teams = <TwoSidesTeam[]>;            // teamDrawers 同源的两支数据
var selected = <当前所选队 id, string>;

yourTeamTeamATextObject.SetActive(teams[0].<+0x10 string> == selected);     // ① 比队 id
yourTeamTeamBTextObject.SetActive(teams[1].<+0x10 string> == selected);     // ② 比队 id
teamABonusObject.SetActive(teams[0].<+0x58 float> != 1.0f);                 // ③ 比 float ≠ 1.0（DAT_1834b2bb8）
teamBBonusObject.SetActive(teams[1].<+0x58 float> != 1.0f);                 // ④ 比 float ≠ 1.0
…
backgroundImage.sprite = LiveOpsAssetUtility.LoadAsset(ev, EventAssetReferenceType.BackgroundImage /*1*/);
```

**结论**：✅ **定死**（4 处）：
- 前两处 = **队 id 字符串相等**（`==`「你选的那支队」）⇒ 显示「Your Team」角标；
- 后两处 = **某 float ≠ `1.0f`** ⇒ 显示加成角标（`1.0` = 「无加成」）。
- 阈值 `1.0` 的判据 = `DAT_1834b2bb8`（`read_literal` = `1.0f`）。

**对账的影响**：`日常_奖励:774` 判据要点写死这 4 句 + 背景图槽 1。

---

### 9｜`ChallengeManager` 的三条词条键

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:536` |
| 账行 | `日常_奖励.tsv:45`（`ChallengeManager.CancelDueling(bool fromClick)`，**B**）⚠️ R-A2 写的「账 `:41`」是**记错行号**，真行是 **`:45`**（`:41` 是 `ChallengeLogMgr.LogTriggerAbility`） |
| 读了 | `decomp_full/ChallengeManager__CancelDueling.c` · `ChallengeManager__.cctor.c` · `工具/read_il2cpp_string.py` |

**读到的东西**

| 槽 | 真值 | 用法 |
|---|---|---|
| `DAT_18425d6a8` | **`CustomErrors/PracticeUserNotFound`** | 搜索状态 `== 1` 时的弹窗文案 |
| `DAT_18425d7a8` | **`CustomErrors/PracticeUserNotOnline`** | 搜索状态 `== 2` 时的弹窗文案 |
| `DAT_184277658` | **`Demo/MainMenu/OK`** | `ChallengeManager..cctor` 把它写进一个 `LocalizedString` 静态字段；`CancelDueling` 末尾 `ShowPopUp(..., 该 LocalizedString, …)` ⇒ **弹窗的确定按钮文案** |
| `DAT_1842b80e0` | **空串 `''`**（不是词条） | 账里把它当成「第三条词条」是误判；它只是 default 值 |

状态 → 文案：`1 ⇒ NotFound` · `2 ⇒ NotOnline` · `3/4 ⇒ 先发一条私信（`PrivateMessageData(2)`）再走 `NotOnline` 那支的默认` · 其它 ⇒ 空串（不弹）。

**结论**：✅ **定死**（三条 + 一条误判订正）。

**对账的影响**：`日常_奖励:45` 的「三条提示词条未解 ⇒ 不编提示文案」这一句**作废**——三条都解出来了，可以照抄。同时把 `DAT_1842b80e0` 从「第三条词条」改成「空串默认」。

---

### 10｜`SignalEventTrigger<T>.Register` / `Unregister`

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:537` |
| 账行 | `日常_奖励.tsv:702` / `:704`（均 **B**） |
| 读了 | `out_b94/INDEX.tsv`（`SignalEventTrigger` 8 条）· `SignalEventTrigger_object_.Register__12549a0.c` · `...Unregister__1254b90.c` · `dump.cs`（`Scaffold.Core.Events.Signal`） |

**读到的东西（逐句）**

```
Register(Action callback):
    this.callback(+0x18) = callback;
    Signal.Register<T>(new Action(this.<0 参转发方法，rgctx +0x18/+0x28>));
    //   目标 = Signal.Register<T>(Action)，dump.cs RVA 0xDA0930

Unregister():
    Signal.Unregister<T>(new Action(this.<同一个 0 参转发方法>));
    //   dump.cs 里 Unregister<T>(Action) 是另一条 RVA 0xDA0AA0
    //   ⚠️ Ghidra 把这一处也标成了 Signal__Register<object> —— 符号串味，别照抄
```

**结论**：✅ **定死（`<object>` 实例）**。两条的差别只有「调 Register 还是 Unregister」+「Register 额外把回调存进 `+0x18`」。
⚠️ `SignalEventTrigger<__Il2CppFullySharedGenericType>` 那两份 INDEX 标「部分（jumptable 未展开）」，但 `<object>` 实例已完全可读 ⇒ 判据够用。

**对账的影响**：`日常_奖励:702/704` 判据要点写死两行；备注留一句「`Unregister` 的产物符号被 Ghidra 标成 `Register`，按 `dump.cs` 的 RVA 区分」。

---

### 11｜`EventBadge.ShowNotification` 的 7 个浮点常量

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:538` |
| 账行 | `日常_奖励.tsv:159`（**B**） |
| 读了 | `decomp_full/EventBadge__ShowNotification.c` · `工具/read_literal.py` |

**读到的东西（真值表 + 8 拍序列）**

| 槽 | 真值 |
|---|---|
| `DAT_1834b2dc4` | **0.1** |
| `DAT_1834b2e7c` | **0.25** |
| `DAT_1834b2bb8` | **1.0** |
| `DAT_1834b3158` | **0.4** |
| `DAT_1834b2bc4` | **360** |
| `DAT_1834b2bbc` | **2.0** |
| `DAT_1834b2bcc` | **-360** |
| `DAT_1834b2bb0` | **0.2** ← 🔴 **第 8 个，原清单漏列** |

```
ShowNotification(ctx):
  SetNotificationData(ctx);
  seq = DOTween.Sequence()
      .AppendInterval(0.1)                                                   // 2dc4
      .Append(transform.DOPunchScale(baseScale * 0.2, 0.25f, vibrato: 1, elasticity: ?))   // bb0 / 2e7c
      .Append(icon(+0x20).DOFillAmount(1.0f, 0.4f) + <FUN_180dc57c0 三实参>)  // 2bb8 / 3158
      .Join(icon.DOFade(1.0f, 0.2f).From())                                  // 2bb8 / bb0
      .Join(transform.DORotate((0,0,360), 0.4f))                             // 2bc4 / 3158
      .AppendInterval(2.0f)                                                  // 2bbc
      .Append(icon.DOFillAmount(0f, 0.4f))
      .Join(transform.DORotate((0,0,-360), 0.4f));                           // 2bcc / 3158
```

**结论**：✅ **定死 8 个常量 + 序列顺序**。唯一残留：`DOPunchScale` 的 `elasticity` 实参在 `.c` 里是从未初始化栈取的（`(ulonglong)uVar6 << 0x20`）⇒ **需按 VA 反汇编确认**（DOTween 默认 1）。

**对账的影响**：`日常_奖励:159` 的「8 拍序列的时长/幅度/角度真值」一栏按上表填死；备注写「elasticity 待 VA」。

---

### 12｜`EventBadgeController.Awake` 的筛选谓词

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:539` |
| 账行 | `日常_奖励.tsv:160`（**B**） |
| 读了 | `decomp_full/EventBadgeController.__c___Awake_b__0_0.c` · `dump.cs`（`EventComponentReferenceType`） |

**读到的东西**

```csharp
ev => LiveOpsAssetUtility.GetComponentReference(ev, (EventComponentReferenceType)0x19) != null
```

**结论**：✅ **定死**：`0x19 = 25` = **`EventComponentReferenceType.EventBadge`**（枚举里明写）。
⇒ **只有「该活动在 LiveOps 资产里配了 `EventBadge` 组件引用」的活动，才建角标。**

**对账的影响**：`日常_奖励:160` 判据要点从「哪一类活动才建角标（未定）」改成上面这一句。

---

### 13｜`MissionsConfig.CleanUp` 的释放范围

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:540` |
| 账行 | `日常_奖励.tsv:524`（**B**） |
| 读了 | `decomp_full/MissionsConfig__CleanUp.c` · `cpp2il/out_cpp2il_dev_ilspy/MissionsConfig.cs:75-101` · `dump.cs`（`MissionsConfig` 字段） |

**读到的东西**

```
CleanUp():
  foreach (var c in this.challenges)          // +0x28  List<MissionChallenge>
      if (c.<+0x50 bool /*已加载/已订阅*/>) {
          c.<virtual +0x1b8>();               // 释放那一跳
          c.<+0x48> = MissionChallenge.<>c.<>9__30_0;   // 换成缓存的空 Action（清回调）
          c.<+0x50> = false;
      }
```

**结论**：✅ **定死**：**释放范围 = `challenges`（`List<MissionChallenge>`）里「已加载」的那些**；**`challengeLists(+0x30)` 完全不动**；`tutorialMissionRef(+0x10)` / `dailyLoginRef(+0x18)` / `configs(+0x20)` 也都不动。
⇒ 原句「释放哪几种资产」问法不准——它**不按资产种类**，按「列表里的已加载实例」。

**对账的影响**：`日常_奖励:524` 判据要点写死；特别记一句「`challengeLists` 不在范围内」（实现时最容易顺手多放）。

---

### 14｜`ChallengeManager.Start` 的两个订阅目标

| | |
|---|---|
| 来源 | `B94_R-A2_出处补全_非战斗.md:541` |
| 账行 | `日常_奖励.tsv:53`（**B**） |
| 读了 | `decomp_full/ChallengeManager__Start.c`（全文 27 行）· `cpp2il/out_cpp2il_dev_ilspy/ChallengeManager.cs:46-60` |

**读到的东西**

```csharp
private void Start()
{
    ChatGlobalManager.Instance.<+0xa0> = this.OnRemoveFriendDuel;              // Action<string>
    ChatGlobalManager.Instance.<+0xa8> = this.AcceptedFriendDuelNotification;  // Action<string>
}
```

**结论**：✅ **定死**：两个订阅 = **`OnRemoveFriendDuel`** 与 **`AcceptedFriendDuelNotification`**，都塞进 **`ChatGlobalManager`** 的 `+0xa0` / `+0xa8`（`Action<string>` 字段），**不是** C# `event +=`。

**对账的影响**：`日常_奖励:53` 判据要点写死。

---

## 二 · R-A3 §二·一（11 条）

### H1｜`DraftEventBase` 六个方法体（轮抽 `:6/:9/:12/:14/:24/:33`）

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:159` |
| 账行 | `轮抽.tsv:6` `Expire` · `:9` `GetBaseData` · `:12` `GetEventDeck` · `:14` `GetPossibleWarlords` · `:24` `PickWarlord` · `:33` `ValidateEventDeck`（均 B） |
| 读了 | `out_b94/DraftEventBase_object__object_*.c`（INDEX 里 `DraftEventBase` 共 26 个方法体 + lambda）· `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/DraftEventBase.cs` · `dump.cs`（`DraftEventData` / `CardInventoryOptions` / `ObtainableItem.cardArmy` @0x2C） |

**读到的东西（逐方法）**

**① `GetBaseData()`（:9）** —— ✅ **定死，且原判据要订正**
```c
DraftEventBase.GetBaseData()  =  令 plVar1 = PlayEvent<..>.GetBaseData(this);   // 基类取值器
                                 return (plVar1 is TData) ? plVar1 : <抛 InvalidCast / 断言>;
```
⇒ 账上写的「纯取值器 `return (TData)this.<字段@0x220>`」**不成立**：它是 **`(TData)base.GetBaseData()` 的强制转换包装**（带 `isinst` 校验），真取值在其基类 `PlayEvent.GetBaseData`（`out_b94/PlayEvent_object__object_.GetBaseData__1126cc0.c`，那一个同样是「取字段 + isinst」）。

**② `ValidateEventDeck(deck, validateOwnership)`（:33）** —— ✅ **定死**
```c
if (param_1 == 0) throw;
uVar3 = FUN_1811283b0(this, deck, 0, <rgctx+0x230>);      // 第一步：调 deck 的校验（返回 bool）
if ((char)uVar3 == 0) return false;
if (this.Data(+0x18) == null || <某 Collection @ data+0x90> == null) throw;
return (deck.<+0x98>.Count - 1) <= <Collection @ +0x90>.Count;    // 卡数上限比较
```
⇒ 语义 = **「先让 deck 自己校验；再要求 `deck 卡数 - 1 ≤ 事件允许集合的容量`」**。
账上写的「只校验卡都在本活动内」只覆盖了第一跳，**漏了第二跳（`deck.Count - 1 <= 允许集合.Count`）**。

**③ `GetPossibleWarlords()`（:14）** —— ✅ **定死（这是本批最大的一条）**
```csharp
List<RawCardScript> GetPossibleWarlords()
{
    var data = this.Data;                                        // +0x18
    List<RawCardScript> list;
    if (data.specificWarlords(+0xB8) == null ||
        data.specificWarlords.Count < data.warlordsPerDraft(+0xA0))
    {
        IEnumerable<RawCardScript> pool = <Singleton 实例>.<+0x20>;      // 全部卡
        if (data.specificArmies(+0xC0) == null || data.specificArmies.Length < 1)
            list = pool.OrderBy(<某个 string 键，虚方法>).ToList();       // 按名字排
        else
            list = pool.Where(w => data.specificArmies.Contains(w.cardArmy))   // ○ 见下 b__20_1
                       .OrderBy(<同一个 string 键>).ToList();
    }
    else
    {
        list = new List<RawCardScript>(data.specificWarlords.Select(x => x.Reference));   // b__20_0
    }

    for (int i = list.Count - 1; i >= 0; i--)          // 倒序删
    {
        var c = list[i];
        if (c.inventoryOptions(+0x4C) == CardInventoryOptions.NotInInventory /*10*/) { list.RemoveAt(i); continue; }
        if (<某 0 参 bool 泛型虚方法>(c))                                              { list.RemoveAt(i); continue; }
        if (c.IsAlternateArt())                                                         { list.RemoveAt(i); continue; }
    }
    if (data.blacklistedWarlords(+0xD0) != null)
        foreach (var br in data.blacklistedWarlords) list.Remove(br.Reference);

    return list;
}
```
- `b__20_1` 体（`out_b94/..._GetPossibleWarlords_b__20_1__1813f80.c`）= `w => data.specificArmies.Contains((CardArmy)w.<+0x2C>)`，`+0x2C` = `ObtainableItem.cardArmy`（`dump.cs`）✔
- `b__20_0` = `x => x.Reference`（`LoadableReference<RawCardScript>` → `RawCardScript`）✔
- `b__20_2` / `b__20_3` = 同一个 ICF 折叠加地址的 `c => c.<虚 string getter>` —— **两个分支各自一个 OrderBy 键选择器**（同一表达式两份 lambda），具体是 `DisplayName`(Slot 25) 还是 `LocalizedName`(Slot 7) **未定**。

⇒ 账 :14 的「**按活动数据过滤全部可选战将；本机不可达**」**作废**：方法体现在读得出来，「全部可选集」= `specificWarlords`（够 `warlordsPerDraft`）优先，否则从全卡池按 `specificArmies` 过滤。原账说的「我们只产 count(3) 张 ≠ 原版全部可选集」这个**差异仍然成立**，但**理由要换**（不是「读不出」，是「我们没照这个三步做」）。

**④ `Expire(onComplete)`（:6）** —— ✅ **定死，且有一处要订正**
```csharp
Expire(Action onComplete)
{
    WindowsManager.Instance.CloseWindow<DraftModeMenuDemo>();
    if (this.DraftState(+0x28) < DraftModeState.SelectWarlord /*5*/)
        if (!AlliancesManager.Instance.HasJoinedEvent(this.GetID()))
            { base.Expire(onComplete); return; }        // 🔴 传的是 onComplete，不是 null

    // 否则开 ExpirePopup（DraftModeExpirePopup）
    if (this.DraftState == DraftModeState.OngoingRun /*15*/)
        PlayfabWrapper.GenericCloudScriptHandler(0x36d /* CloudHandle 之一 */,
                                                 new { id = this.GetID() }, callback /* = FinishRun 那条 */);
    else { CustomDebug.Log("..."); onComplete?.Invoke(); }
}
```
**订正**：账上写 `base.Expire(null)` ⇒ 实际是 **`base.Expire(onComplete)`**（`.c` 里 `LiveOpsEvent__Expire(param_1, *(lVar2+0x18) /*= param_2 = onComplete*/, 0)`）。

**⑤ `GetEventDeck()`（:12）** —— ✅ **体已导出、可读**（两支）
```csharp
GetEventDeck()
{
    if (this.Deck(+0x38) == null) { /* 建 CardDeck：督军 + data.<+0xa8> 给的卡列表，见下 */ }
    else { /* 已存在：只刷新 +0x40 = 督军引用，+0x58 = Select(卡).ToList() */ }
    // 末尾：cardback 为空 ⇒ 取 ArmyUtilities.GetDefaultCardback(<当前督军阵营>)
}
```
具体：`new CardDeck(督军引用, <data.+0x98 逐张 Select(lp => lp.Reference)> + ToList)`，随后 `deck.+0x28 = data.<+0xa8>`（string）、`deck.+0x70 = new Nullable<int>(7) /* PlayModes.ClosedDeck */`。⇒ 与账「把已选卡拼成出战卡组」一致，可写死到这一粒度。

**⑥ `PickWarlord(warlord, onSuccess, onFail)`（:24）** —— 账已被 `B93_N3` 就地订正过（云脚本那一半在服务端）；本批**复核一致**，不重复。

**结论**：
- **4 条完全定死**（`GetBaseData` / `ValidateEventDeck` / `GetPossibleWarlords` / `Expire`）—— 其中 **3 条判据要订正**；
- **`GetEventDeck` 定死到可施工粒度**；
- **`PickWarlord` 维持 `B93_N3` 的结论**（服务器那一半 → P3）。
- 残差：`GetPossibleWarlords` 的 `OrderBy` 键（`DisplayName` vs `LocalizedName`）、以及那个 0 参 bool 泛型虚谓词。

**对账的影响**：`轮抽.tsv` 的 `:6`（订正 `base.Expire(onComplete)`）· `:9`（订正成「转换包装」）· `:14`（**整段重写**：判据已齐，B 的理由从「读不出」改成「我们没照做」）· `:33`（补第二跳卡数上限）。

---

### H2｜`GenericArmyItem.GetItem(CardArmy)` 的带参版体

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:160` |
| 账行 | `未分类_其他.tsv:322`（**B**） |
| 读了 | `decomp_full/GenericArmyItem__GetItem.c`（**那是无参版**）· `cpp2il/out_cpp2il_dev_ilspy/GenericArmyItem.cs:221-274` · `dump.cs`（`GenericArmyItem` + `Entry`）· `out_b94/INDEX.tsv`（**不含** `GenericArmyItem`） |

**读到的东西**

`.c` 侧确实**没有带参版**（同名两重载在导出时撞名，只留下无参版；`grep` 全库没有 `FUN_1806efa20`）。`dump.cs` 说带参版是**普通方法**、RVA `0x6EFA20`（不是泛型 RVA −1）。

ILSpy（第二权威）：
```csharp
public ObtainableItem GetItem(CardArmy army)
{
    if (entries != null) {
        var e = entries.GetValueOrDefault(army);      // Dictionary<CardArmy, Entry>
        if (e != null) return e.reward.Item;          // ItemInfo<ObtainableItem>.Item
    }
    var f = values.FirstOrDefault(x => x.army == army);   // List<Entry>，即 b__0
    if (f != null) return f.reward.Item;
    throw new NullReferenceException();               // 找不到 ⇒ 原版直接 NRE
}
```

**结论**：**定死到「够写代码」**（走 ILSpy 第二权威）。第一权威 `.c` **本机确实没有**，要补只能按 RVA `0x6EFA20` 单独做 **Ghidra 单函数导出**（不覆盖 `decomp_full/`，也不动 `d:/2` 的工程库）。

**对账的影响**：`未分类_其他:322` 判据要点写上面四句；备注写「第一权威缺：同名重载撞名，需按 RVA 0x6EFA20 单函数补导」。

---

### H3｜`ToggleCollectionFilter` 两个 lambda 的被调方法身份

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:161` |
| 账行 | `泛型与启动链_补读.tsv:137`（`b__5_0(bool b)`）· `:138`（`b__7_2(T rc)`），均 **B** |
| 读了 | `decomp_generic_trial/out/..._SetupFilter_b__5_0__1366660.c` · `..._Filter_b__7_2__1366530.c` · `cpp2il/out_cpp2il_dev_ilspy/ToggleCollectionFilter.cs:120-128,803-826` · `dump.cs`（`CollectionFilter<T>`） |

**读到的东西**

- **`b__5_0(bool b)`** = ✅ **定死**：`this.OnFilterChange();`（ILSpy `:803-806` 直译）。`OnFilterChange` = `CollectionFilter<T>` 的 **Slot 5 虚方法**（`dump.cs:71452` 段）。⇒ 原句「对捕获对象调一个无参虚方法」应改成 **「对本组件调 `OnFilterChange()`（toggle 一被拨就重筛）」**。
- **`b__7_2(T rc)`** = ⛔ **仍差**：`rc == null ? false : this.<泛型虚方法>(rc)`。两份产物都只到「从 `klass+0x1E0` 取 `{target, invoke_impl, methodInfo}` 三件套 + indirect jump」，`.c` 里也写着 `Could not recover jumptable`。语义只能确定「对选项做一次成员测试」，具体方法名未解。
- 顺带定死两条：`b__7_0` = `b => !b`、`b__7_1` = `b => b`（`Filter` 里的两个 bool 投影）。

**结论**：**1 定死 / 1 仍差**。

**对账的影响**：`:137` 判据要点改成「`OnFilterChange()`」，`:138` 维持「lambda 谓词」并写明「被调方法 = `klass+0x1E0` 槽上的泛型虚方法，未解，要按 VA `0x1813665xx` 反汇编」。

---

### H4｜`ListExtensions.GetMax` / `GetMin` 的平手取谁

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:162` |
| 账行 | `未分类_其他.tsv:398` / `:399`（均 **B**） |
| 读了 | `decomp_full/ListExtensions__GetMax_KeyValuePair_object__object___int_.c` · `..._GetMax_b__0.c` · `..._GetMin_b__0.c` · `dump.cs`（RVA 反解） |

**读到的东西**

```
GetMax(data, f) = data.DefaultIfEmpty(default(T)).Aggregate((a, b) => {
                       int ka = f(a), kb = f(b);
                       return ka.CompareTo(kb) > 0 ? a : b;      // 平手 ⇒ 取 b
                  });

GetMin(data, f) = data.DefaultIfEmpty(default(T)).Aggregate((a, b) => {
                       long ka = f(a), kb = f(b);
                       if (ka.CompareTo(kb) < 0) b = a; else /* 平手 */
                                                 b = b;
                       return b;                                 // 平手 ⇒ 取 b
                  });
```
- RVA 反解：`FUN_180c91d90` = **`Enumerable.DefaultIfEmpty<TSource>`**、`FUN_180c8b810` = **`Enumerable.Aggregate<TSource>`**（`grep -B4 "RVA:" dump.cs`）。
- `Aggregate` 的第二个实参是「当前元素 / 后来者」，两个 lambda 在平手时都返回它。

**结论**：✅ **定死**：**`GetMax` 与 `GetMin` 平手都取「后出现的那一个」**；两者都先 `DefaultIfEmpty(default(T))`（空集合 ⇒ 返回 `default(T)`，不抛）。

**对账的影响**：`:398`/`:399` 判据要点写死这两句，原「查不到」一栏清空。

---

### H5｜`AllianceRatingDisplay.Initialize` 的签名

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:163` |
| 账行 | `社交_聊天.tsv:60`（**A**）—— ⚠️ **这条其实早就定了**，R-A3 记成挂起是误记 |
| 读了 | `decomp_full/AllianceRatingDisplay__Initialize.c`（全文）· `dump.cs`/账行 |

**读到的东西**

`.c` 的形参形状：`(this, int param_2, UnityEngine.Object param_3 /*Sprite*/, char param_4, byte param_5, byte param_6)`。
账行 `社交_聊天:60` 的原版签名栏已经写着：**`Initialize(int value, Sprite overrideIcon, bool disableIfZero, bool useSealSystem, bool zeroBasedRank)`** —— **与 `.c` 的 5 参形状逐一对应**（int / Sprite / bool / bool / bool）。

体（`disableIfZero && value == 0 ⇒ 整个 gameObject 关掉并 return`；`GetActiveEvent` 取当前活动；`param_6 & param_5` 同时为真且 `value < X` 时走「已排名」那一支；最后写标签、可选换 icon）。

**结论**：✅ **定死**（`社交_聊天:60` 已 A，**不需要改**）。

**对账的影响**：无（这条是**误挂起**，写进本报告只为不再被下一轮当成缺口）。

---

### H6｜`SwarmVFXController.ToggleBody3D` 的实参

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:164` |
| 账行 | `特效动画.tsv:403` —— ⚠️ **行号对不上**：`:403` 是 `SwarmVFXController.Initialize`；`ToggleBody3D` 的真行是 **`战斗界面_HUD.tsv:163`**（`BattleCardUI.ToggleBody3D(bool optionBody, bool? optionEffectAnchor)`，**A**） |
| 读了 | `decomp_full/BattleCardUI__ToggleBody3D.c` · `assetripper_out/.../BattleCardUI.cs:198-220`（`body3D` / `effectsAnchor`）· `dump.cs`（`DefinedTrait.ambush = 1200`） |

**读到的东西**

```
public void ToggleBody3D(bool active, bool? overrideAnchorActive)
{
    body3D(+0x158).SetActive(active);
    if (overrideAnchorActive.HasValue)      // 🔴 (char)local[0] != 0 就是 HasValue 位（Nullable<bool> 第 0 字节）
        active = overrideAnchorActive.Value;
    if (entity(+0x248).HasCurrentTrait(DefinedTrait.ambush /*1200*/))
        active = false;                     // 🔴 埋伏单位 ⇒ 特效锚点强制关
    effectsAnchor(+0x1A0).gameObject.SetActive(active);
}
```

- 原挂起问的「`Nullable<bool>_ctor(local, 0, mi)` 是 `value=false` 还是 `hasValue` 位」⇒ **是 `HasValue` 判定**（`(char)local[0] != 0` 走的是 Nullable 的 hasValue 字节）。
- `0x4B0 = 1200 = DefinedTrait.ambush`（`dump.cs:45841`）。

**结论**：✅ **定死**，并且 **H6 的方法名记错了**：`SwarmVFXController` 里**没有** `ToggleBody3D`，只有 `BattleCardUI.ToggleBody3D`。

**对账的影响**：`战斗界面_HUD:163`（A）的「差异」栏补一句 **「`ambush(1200)` ⇒ 锚点强制 false」**（原账只写「埋伏时特效锚点另算」，没写这一支的具体条件）。

---

### H7｜`Countdown.Activate` 的格式串常量

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:165` |
| 账行 | `未分类_其他.tsv:204`（**B**） |
| 读了 | `工具/read_il2cpp_string.py 0x184264910` · `decomp_full/Countdown__Activate.c:30-45` · `cpp2il/out_cpp2il_dev_ilspy/Countdown.cs:23-38` |

**读到的东西**

- 槽 `0x184264910` ⇒ **`'{0:0}:{1:00}'`**（从 `global-metadata.dat` 的 string literal 表直解，与 ILSpy 给的一致）。
- 体：`TimeSpan(0, 0, secondsDuration)` → `text = string.Format("{0:0}:{1:00}", ts.Minutes, ts.Seconds)`；`speed → +0x44`；`_startTime = DateTime.Now (+0x28)`；`+0x30`/`+0x38` 两个 DateTime 置默认；`+0x40 = true`；`+0x50 = false`。

**结论**：✅ **定死**。

**对账的影响**：`未分类_其他:204` 判据要点写死格式串 + 初值四格。

---

### H8｜`MilestoneScoreBar.SetProgressBar`

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:166` |
| 账行 | `未分类_其他.tsv:431`（**A**）—— ⚠️ R-A3 说「`:430` 的账只覆盖 `Initialize`，滑块布置没判据」**不成立**：`:431` 就是 `SetProgressBar`，且已是 A |
| 读了 | `decomp_full/MilestoneScoreBar__SetProgressBar.c`（全文）· `dump.cs`（签名） |

**读到的东西**

```csharp
SetProgressBar(GenericScoringMilestone[] milestones, int currentScore)
{
    int[] arr = (milestones == null || milestones.Length == 0)
                  ? Array.Empty<int>()
                  : milestones.Select(m => m.<阈值字段>).ToArray();
    slider(+0x20).wholeNumbers = false;
    slider.maxValue = arr.LastOrDefault();
    slider.normalizedValue = SupportMethods.InterpolateMilestones(
                                 arr.Prepend(0).ToList(), currentScore);   // ⚠️ currentScore 实参被 Ghidra 丢
}
```

**结论**：✅ **定死**（`lastOrDefault` 作 `maxValue`、`Prepend(0)` 后交给 `InterpolateMilestones` 求归一值）。
**影响**：这条**没有账要改**（`:431` 已 A）；本报告只是把它的判据写清楚，顺带订正 R-A3 的「没判据」说法。

**对账的影响**：无（判据补记在本报告即可）。

---

### H9｜`TutorialTipScript.StopTipPermanent`

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:167` |
| 账行 | `教程_引导.tsv:133`（`ClickContinue()`，**B**）—— 这正是 `ClickContinue` 的第 ② 句 |
| 读了 | `decomp_full/TutorialTipScript__StopTipPermanent.c` · `cpp2il/out_cpp2il_dev_ilspy/TutorialTipScript.cs:294-306` |

**读到的东西**

```csharp
public virtual void StopTipPermanent() { FinishTutorialTip(); }

public void ClickContinue()
{
    continueText.gameObject.SetActive(false);
    StopTipPermanent();                       // ← 第 ② 句
    battleManager.ContinuePlayerScriptedTurn();   // ← 第 ③ 句
}
```
（`TutorialTipScript__StopTipPermanent.c` 全文只有一条 tail-call：`(**(code**)(this+0x1b8))(this, …)` = 调虚方法 `FinishTutorialTip`。）

**结论**：✅ **定死**：`StopTipPermanent()` **= `FinishTutorialTip()`**（一行转发）。账上「我们无同形物」可以改成「等价物 = 我们的 FinishTutorialTip 那一跳」。

**对账的影响**：`教程_引导:133` 判据要点把三句写全（`continueText` 关 → `FinishTutorialTip()` → `ContinuePlayerScriptedTurn()`）。

---

### H10｜`MainMenuWindow` 的 `LiveopMenuContainer` 容器族（菜单 `:427/:434/:438/:439`）

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:168` |
| 账行 | `菜单_外壳.tsv:427` `CleanupContainers` · `:434` `InstantiateLiveOp` · `:438` `RefreshLiveOpContainers`（均 **B**）· `:439` `RefreshSignal`（**A**，已落地） |
| 读了 | `dump.cs`（`LiveopMenuContainer` / `UiContainer<T>`）· `decomp_full/LiveopMenuContainer__{Awake,OnInitialize,OnDestroy,OnClick,SetText,OnLocalize,Release,ReleaseLoadedContent}.c` · **`assets_full` 反查**：`bundle_Waprforge_monoscripts/MonoScript/MonoScript_3437379357541609161.json`（`m_ClassName = "LiveopMenuContainer"`）→ `grep` 四个 `MonoBehaviour_*.json` · `dump.cs`（`ContainerBuilder<T>`） |

**读到的东西**

1. **四个 `LiveopMenuContainer` 实例本地就有**（`bundle_menus_assets_all/MonoBehaviour/`）：
   `-4318495628687614811` · `-2251736854757368202` · `3260382881153191541` · `4473714526825562173` —— 每一个的 **`title` / `timer` / `backgroundImage` / `foregroundImage` 四支 PPtr 全在**（`backgroundButton` 来自基类）。其中一份 `timer` 是 `m_PathID: 0`（= 不挂计时器），**这正好对应 `IsTimedEvent` 的两个分支**。
2. **`LiveopMenuContainer` 的 8 个方法体全在 `decomp_full`**（`Awake` / `OnInitialize` / `OnDestroy` / `OnClick` / `SetText` / `OnLocalize` / `Release` / `ReleaseLoadedContent`）。
3. ⛔ **唯一真缺**：`ContainerBuilder<T>.MakeContainer(...)` 三份重载**在 `decomp_full` 与 `out_b94` 都是 0 产物**（泛型类 ⇒ `RVA -1`，且不在 B94 那 11 个类里）。

**结论**：**判据基本齐了**——`LiveopMenuContainer` 本身（类 + prefab + 4 份实例）**不是缺口**；缺的只是 `ContainerBuilder<T>` 的实现体。
⇒ 账上「③ 阻塞在**容器族**（类 C）」这个理由**要改**：应改成「**缺 `ContainerBuilder<T>` 的实现体**；若要逐句等价，需把它加进 `DecompG7.java` 名单再导一趟（约 15 秒，只读，别覆盖 `decomp_full/`）」。
⇒ 另外 `:434` 的判据要点里「`ComponentKey(=35)`」已可坐实：`EventComponentReferenceType.MainMenuContainer = 35`。

**对账的影响**：`菜单_外壳:427/434/438` 三条的「阻塞理由」一栏改写（从「容器族不在仓库」→「`ContainerBuilder<T>` 体缺 + 我们没做按事件建卡」）；`:439` 不动。

---

### H11｜`PlayerInfo` DTO 在我们工程里对应哪个类

| | |
|---|---|
| 来源 | `B94_R-A3_出处补全_其余.md:169` |
| 账行 | `未分类_其他.tsv:479`（`PlayerInfo.SetBotInfo(string botName, int botElo)`，**A**） |
| 读了 | `grep -rn "IsBot\|EloRating" Unity/MyGame/Assets/CardPresentation` · `日常/未分类` 账行 |

**读到的东西**

- 工程里**没有名为 `PlayerInfo` 的类**（原账「—（无）」字面没错）。
- 等价物在 **`CardPresentation/Shell/PlayerProfileWindow.cs`**：`ViewedPlayer`（`Name` / `BotName` / `BotELO` / `IsBot` 由 `BotName` 派生）+ `ApplyBotInfo`（`:613`）—— `B94_X1`（2026-10-29）已落地并把它升成 **A**。
- 另一处同源消费者：`Shell/StrangerProfilePage.cs:55-66`（`BotELO > 0` 才画 ELO 那行）。

**结论**：✅ **定死**：不是「缺」，是**换了宿主**（原版写进 `PlayerInfo` 对象，我们写在窗口的 `ViewedPlayer` 上，同一份数据）。

**对账的影响**：无（`:479` 已 A）。

---

## 三 · 顺手发现：账记错 / 需订正（逐条，给调度台）

1. **`轮抽.tsv:6`（`DraftEventBase.Expire`）** —— 「没加入过走基类过期」那句写的是 `base.Expire(null)`，实际是 **`base.Expire(onComplete)`**（`out_b94/DraftEventBase_object__object_.Expire__180f230.c` 里 `LiveOpsEvent__Expire(this, param_2, 0)`，`param_2` 就是入参 `onComplete`）。
2. **`轮抽.tsv:9`（`DraftEventBase.GetBaseData`）** —— 「纯取值器 `return (TData)this.<字段@0x220>`」**不成立**：它是 `(TData)base.GetBaseData()` 的 isinst 包装；真取值在基类。
3. **`轮抽.tsv:14`（`GetPossibleWarlords`）** —— 整段「方法体本机读不出 / 建议加名单再导」**已过期**：`out_b94` 里体完整可读。判据改成 §二·H1 那三步。
4. **`轮抽.tsv:33`（`ValidateEventDeck`）** —— 「只校验卡都在本活动内」漏了第二跳：`deck.Count - 1 <= 允许集合.Count`（卡数上限）。
5. **`日常_奖励.tsv:45`（`ChallengeManager.CancelDueling`）** —— 「三条提示词条键没解出来 ⇒ 不编提示文案」**作废**：三条 = `CustomErrors/PracticeUserNotFound` / `CustomErrors/PracticeUserNotOnline` / `Demo/MainMenu/OK`；且 `DAT_1842b80e0` 是**空串**，不是词条。
6. **`菜单_外壳.tsv:427/434/438`（`MainMenuWindow` 容器三件）** —— 「阻塞在容器族（类 C）」的理由要换：`LiveopMenuContainer` 的**类体 8 个方法全在、prefab 实例本地有 4 份**；真缺的只有 `ContainerBuilder<T>.MakeContainer` 的体。

**另外 4 条「误挂起」（不是账错，是 R-A3 的挂起表记错了，别再重查）**：
- H5 `AllianceRatingDisplay.Initialize` —— `社交_聊天:60` 早已是 **A** 且签名写全。
- H6「`SwarmVFXController.ToggleBody3D`」—— **没有这个方法**；真身是 `BattleCardUI.ToggleBody3D`（`战斗界面_HUD:163`，A）。
- H8 `MilestoneScoreBar.SetProgressBar` —— `未分类_其他:431` 就是它、已 **A**（不是「没判据」）。
- `B94_R-A2 §二·二 #9` 写的「账 `:41`」——真行是 `日常_奖励:45`（`:41` 是 `ChallengeLogMgr.LogTriggerAbility`）。

---

## 四 · §定死的（可执行改账）

> 「改哪一格」= 该行的 **判据要点 / 备注 / 状态**。⛔ 本代理不改账，只给可粘贴的结论。

| # | 账行 | 现状态 | 定死的判据（一句话） | 该改哪一格 |
|---|---|---|---|---|
| 1 | `其他_基础设施:218` | B | `GetEvents(false).Where(ReflectionHelper.Is(g,t)).Where(g => g.EventState==LiveOpState.Active).OrderByDescending(g => g.Priority).FirstOrDefault()` + 四支 IRankedEvent 分支 | 判据要点 |
| 2 | `商店:36` | B | 0/1/2 三态；0/1 用 `notCollectedSprite(+0x90)`（0=`collectText`、1=`becomePremiumText`），2 用 `collectedSprite(+0x88)` 且**不写标签**；shiny 0/1 开、2 关 | 判据要点 + 新增一句「case 2 不更新标签」 |
| 3 | `日常_奖励:549` | B | label 顺序 **4(NavigationBarTitle) → 0(Title)**；icon 槽 `Icon(0)`；overIcon 槽 `OverIcon(9)` 且先 `HasAsset` | 判据要点 |
| 4 | `日常_奖励:774` | B | 4 处：`+0x48/+0x50` 比 `team.<+0x10>` 队 id 相等；`+0x70/+0x78` 比 `team.<+0x58>` float `≠ 1.0f` | 判据要点 |
| 5 | `日常_奖励:45` | B | 三条词条 = `CustomErrors/PracticeUserNotFound` / `CustomErrors/PracticeUserNotOnline` / `Demo/MainMenu/OK`；`DAT_1842b80e0` 是空串 | 差异列（删「不编提示文案」） |
| 6 | `日常_奖励:159` | B | 8 个常量 `0.1/0.25/1.0/0.4/360/2.0/−360/0.2` + 序列顺序（见 §一·11） | 判据要点 |
| 7 | `日常_奖励:160` | B | `GetComponentReference(ev, EventComponentReferenceType.EventBadge /*25*/) != null` | 判据要点 |
| 8 | `日常_奖励:524` | B | 只清 `challenges(+0x28)` 里「已加载」的；`challengeLists(+0x30)` **不动** | 判据要点 |
| 9 | `日常_奖励:53` | B | `ChatGlobalManager.Instance.+0xa0 = OnRemoveFriendDuel; .+0xa8 = AcceptedFriendDuelNotification` | 判据要点 |
| 10 | `日常_奖励:702/704` | B | `Register(cb){this.callback=cb; Signal.Register<T>(new Action(...))}` / `Unregister(){Signal.Unregister<T>(…)}` | 判据要点 + 备注「Ghidra 把 Unregister 标成 Register」 |
| 11 | `其他_基础设施:427/481` | B | `DAT_1834b2e08 = float 100.0`（读 `int/100`、写 `Round(float*100)`）；`DAT_1834b2e60` 与本方法无关 | 备注（删「未坐实」） |
| 12 | `商店:92` | B | `() => onClosePurchasePopup?.Invoke()`，委托类型 `System.Action` | 判据要点 |
| 13 | `其他_基础设施:381` | C | payload = `<>f__AnonymousType64<string>{ Decks = Json(arr) }` → `CloudHandle.updatePlayerDecksArray`；回写 `deck.syncedToServer=true; deckInventory.Add(deck,1)` | 判据要点（C 若只因「判据缺」→ 可升档） |
| 14 | `商店:44` | B | `Where(谓词①?) → SpliceWhere(x => x.Item is Currency) → 先画货币再画其余 → 条目数 < thresholdForEnableAutoScroll ⇒ scroll.<bool@+0x22>=false` | 部分（谓词① 留「仍差」） |
| 15 | `轮抽:6` | B | `base.Expire(onComplete)`（**不是 null**） | 判据要点 |
| 16 | `轮抽:9` | B | `(TData)base.GetBaseData()` 的 isinst 包装（非纯取值器） | 判据要点 |
| 17 | `轮抽:14` | B | 三步：`specificWarlords.Count ≥ warlordsPerDraft` ⇒ 用它；否则全卡池按 `specificArmies` 过滤 → `OrderBy` → 倒序删 `NotInInventory(10)` / 虚谓词 / `IsAlternateArt` → 再删 `blacklistedWarlords` | 判据要点**整段重写** + 理由改「我们没照做」 |
| 18 | `轮抽:33` | B | 第一跳 deck 自校验 + 第二跳 `deck.Count - 1 ≤ 允许集合.Count` | 判据要点补第二跳 |
| 19 | `未分类_其他:204` | B | 格式串 `'{0:0}:{1:00}'`；`TimeSpan(0,0,sec)`；`+0x44=speed`、`+0x28=Now`、`+0x40=true`、`+0x50=false` | 判据要点 |
| 20 | `未分类_其他:398/399` | B | `DefaultIfEmpty(default).Aggregate((a,b)=> … )`；**平手取 b（后者）** | 判据要点 |
| 21 | `未分类_其他:322` | B | `entries.GetValueOrDefault(army)?.reward.Item ?? values.FirstOrDefault(x=>x.army==army)?.reward.Item ?? throw NRE` | 判据要点 + 备注「第一权威缺（同名重载撞名）」 |
| 22 | `战斗界面_HUD:163` | A | 补：`ambush(DefinedTrait 1200)` ⇒ `effectsAnchor` **强制 false**；`overrideAnchorActive.HasValue` 才覆盖 | 差异列补一句 |
| 23 | `教程_引导:133` | B | 三句：`continueText` 关 → `FinishTutorialTip()` → `ContinuePlayerScriptedTurn()` | 判据要点 |
| 24 | `菜单_外壳:427/434/438` | B | 阻塞理由改写：容器类/prefab 都在，真缺 `ContainerBuilder<T>.MakeContainer` 体 + 我们按事件建卡未做；`:434` 的 `ComponentKey` 已坐实 = **`EventComponentReferenceType.MainMenuContainer = 35`** | 差异/备注 |

---

## 五 · §仍挂起的（差什么 · 下一步）

| # | 条目 | 差什么 | 下一步（具体到文件/工具/命令） |
|---|---|---|---|
| A | **`CarouselOfferPopupDrawer` 谓词①**（`商店:44`） | `x => x.Item.<0x1C8 槽上的 0 参 bool 泛型虚方法>` 的**方法身份** | 反汇编 `CarouselOfferPopupDrawer.__c___DrawRewards_b__4_0` 的 VA（`0x180811xxx` 段）看那一跳的 slot→方法映射；或全库搜同一 slot `+1C8` 在别的**有符号**方法里的调用点交叉命名 |
| B | **`ToggleCollectionFilter.b__7_2`**（`泛型与启动链_补读:138`） | `this.<klass+0x1E0 槽上的泛型虚方法>(rc)` 的方法名 | 按 `..._Filter_b__7_2__1366530.c` 里 `uVar2 = *(lVar1 + 8)` 那一跳做 VA 反汇编（`0x1813665xx`）；`CollectionFilter<T>` 的虚表只有 `SetupFilter/OnFilterChange/Filter` 三个，所以多半是**继承自 `MonoBehaviour` 之外的另一个基类**或 `ICollection<T>` 接口方法 |
| C | **`GenericArmyItem.GetItem(CardArmy)` 第一权威** | `decomp_full` 无带参版（同名重载撞名） | 按 **RVA `0x6EFA20`** 做 Ghidra **单函数**导出到**新目录**（⛔ 别覆盖 `decomp_full/`；动 `d:/2` 需用户确认）。在此之前可先用 ILSpy 版施工 |
| D | **`EventBadge.ShowNotification` 的 `DOPunchScale` elasticity 实参** | `.c` 里那一位是从未初始化栈取的 | 反汇编 `EventBadge__ShowNotification` 的 VA，读 `DOPunchScale(endValue, dur, vibrato, elasticity)` 第 4 实参（DOTween 默认 = 1） |
| E | **`ContainerBuilder<T>.MakeContainer`（3 个重载）** | 泛型类，`decomp_full` 与 `out_b94` 双 0 产物 | 把 `ContainerBuilder` 加进 `DecompG7.java` 名单再导一趟（约 15 秒，只读、输出到新目录）。**不阻塞** 427/434/438 的施工（`LiveopMenuContainer` 与 4 份 prefab 都已具备），只阻塞「逐句等价」那一档 |
| F | `DraftEventBase.GetPossibleWarlords` 的 `OrderBy` 键 | `c => c.<虚 string getter>`：`DisplayName`(Slot 25) 还是 `LocalizedName`(Slot 7) | 反汇编 `..._GetPossibleWarlords_b__20_2__eaa100.c` 对应的 `klass+0x188` 那一跳；或用 `--search` 找该 vtable slot 在别处带符号的调用点 |

---

## 六 · 红线自查

- ⛔ **未改任何源码 / 账 / 正本**：本会话只写了这一个文件。
- ⛔ **未跑 Unity**、⛔ **未动 git**。
- ⛔ **未动 `d:/2`**：全部是读；`read_literal.py` / `read_il2cpp_string.py` 都是纯只读工具（工具自带「不带参只打用法」的保护）。
- ✅ **查不到的写「查不到 + 还差什么」**：见 §五 六条，每条都给了可执行下一步，没有一条用猜测顶替。
