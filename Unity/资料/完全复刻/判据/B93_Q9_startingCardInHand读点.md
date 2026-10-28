# B93_Q9 · `startingCardInHand` / `goSecondCardInHand` 的原版读点

> 只读代理产出。未改任何源码 / 数据 / 账 / 正本，未跑 Unity，未动 git。

## 判定（找到了 / 没找到 + 一句话）

**找到了。**

- **`startingCardInHand`（`RawCardScript +0x148`）唯一读点 = `BattleManager.<StartBattlePhase>d__334.MoveNext`**（开局那条 `StartBattlePhase` 协程的**状态机体**）：在「发完后手防御卡（`AddGoesSecondCardToDeck`）＋ 跑完开局附魔」之后、`NextTurn` 之前，**对两位督军各读一次**，非空就 `AddNewCardToHand(...)` **新建一张卡进手牌**（**全程不碰牌库**）。
- **`goSecondCardInHand`（`+0x150`）唯一读点 = `BattleManager.AddGoesSecondCardToDeck`**（我们已完整接线，见 §三，它是现成的范本）。

⇒ 上一轮担心的「加列 = 静默窗口」**不成立**：两列都有真实读点。
⇒ 但更要紧的是：**这两列我们其实都已经有等价实现** —— `startingCardInHand` 走的是**卡面文字解析**（`CardDef.StartWithInHand`）而不是这个 PPtr，所以本轮真正该做的是「**把自设计的那条换成原版这条**」，不是「凭空加一列」。

**上一轮（`B93_Q7`）为什么没定位到（已复现，非猜测）**：
`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` 是四人里最常翻的那份，而它**把整条 `_StartBattlePhase_d__334.MoveNext` 的方法体丢了** —— 全目录 `grep -rl startingCardInHand` **只命中 `RawCardScript.cs`（字段声明）**。所以「按字段名搜 ILSpy」必然 0 命中。命中在 `out_il2csharp` 与 `decomp_full` 两份。

---

## 证据（原版 文件:行 + 关键语句；字段偏移怎么定的）

### 0. 字段偏移是怎么定的（不靠猜）

`d:/2/tools/il2cpp_out/dump.cs`（IL2CPP 的类型/字段表，`// 0xNN` 就是字段偏移）：

| 行 | 原文 |
|---|---|
| `dump.cs:23291` | `public RawCardScript startingCardInHand; // 0x148` |
| `dump.cs:23292` | `public RawCardScript goSecondCardInHand; // 0x150` |
| `dump.cs:23293` | `public List<RawCardScript> startingTroopsInPlay; // 0x158` |
| `dump.cs:23294` | `public bool starterHero; // 0x160` |

这四格挂在 `RawCardScript`（`dump.cs:23244` `public class RawCardScript : PlayerItem`）。
而**持有它的实例是 `CardScript`**：`RawCardScript` 在 `EntityScript` 上是自动属性
`private RawCardScript <rawCardData>k__BackingField; // 0x28`（`dump.cs:21968`）。
⇒ 运行时读法 = **`*(RawCardScript**)(cardScript + 0x28)` 再 `+ 0x148`**。
这条桥在 `.c` 里能直接对上：`*(undefined8 *)(*(longlong *)(lVar8 + 0x28) + 0x148)`，其中 `lVar8 = BattleManager.GetHero(...)` 返回的正是 `CardScript`（`BattleManager__GetHero.c` 全文：`*(longlong*)(manager + 0xC0/+0xC8) → +0x28`）。

### 1. 读点 A —— `startingCardInHand`

**第一权威（`.c`）**：`d:/2/tools/decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c`

- `:210` / `:218` / `:235` / `:243` —— 四处 `uVar11 = *(undefined8 *)(*(longlong *)(lVar8 + 0x28) + 0x148);`
  （两位督军各 2 次：先判空、再取值传参）
- `:205-212` **第一位督军**：
  ```
  lVar8 = BattleManager__GetHero(lVar3, *(undefined1 *)(lVar3 + 0x247), 0);      // 0x247 = playerGoesFirst
  uVar11 = *(undefined8 *)(*(longlong *)(lVar8 + 0x28) + 0x148);                 // startingCardInHand
  if (UnityEngine_Object__op_Inequality(uVar11, 0, 0))
      BattleManager__AddNewCardToHand(lVar3, uVar11, GetHero(...), isPlayerByte,
                                      zeroVector, zeroVector, /*priority*/1, 0, 0, 0);
  ```
- `:232-250` **第二位督军**：`GetHero(*(char *)(lVar3 + 0x247) == '\0')`，同一套。
- **时机**：`:200-203`（`AddGoesSecondCardToDeck`）**之后**、`:287`（`NextTurn`）**之前**。

**可读版（il2csharp）**：`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs`

- class `_StartBattlePhase_d__334`（`:40282`）· `MoveNext`（`:40314`）
- `:40396` `RawCardScript rawCardScript2 = rawCardScript1.startingCardInHand;`
- `:40404` `AddNewCardToHand(battleManager1, rawCardScript3.startingCardInHand, GetHero(playerGoesFirst), playerGoesFirst, zeroVector, …, 1, 0, 0)`
- `:40408` / `:40417` 第二位督军同一套（`GetHero(!playerGoesFirst)`）

🔴 **一个容易读错的地方（两份可读反编译的缩进都会骗人）**：
`il2csharp:40349` 起看着像「`if (!ShouldActivatInitialEnchantments()) { <startingCardInHand 那一段> } else { <附魔> }`」，
但 `.c:180-196` 的附魔循环里有一句 **`if (count <= iVar12) goto LAB_1809c41f9;`**，而 `LAB_1809c41f9:` 就是 startingCardInHand 那一段
（`il2csharp:40450-40478` 是同一件事的 `goto L_1809c41f9`）。
⇒ **两条路都会加 startingCardInHand**（附魔局是「先跑附魔，再发起始卡」），⛔ 别读成「只有非附魔局才发」。

**它怎么用**：`d:/2/tools/decomp_full/BattleManager__AddNewCardToHand.c:54` +
`out_cpp2il_dev_ilspy/BattleManager.cs:39101-39148`：
`battleCards.CreateCard(createdCard, handParent, isPlayer, …)` → `new BattleAction(BattleActionType.createHandCard, …)`。
- `BattleActionType.createHandCard = 23`（`dump.cs:45333`）；
- **全程没有** `Deck` 的读写 ⇒ **凭空建一张新卡**，⛔ 不是「从牌库挪一张」。
- `RawCardScript.CheckInitialValues`（`out_cpp2il_dev_ilspy/RawCardScript.cs:593-605`）**不碰**这两列。

### 2. 读点 B —— `goSecondCardInHand`（已接线，作范本）

- `.c`：`decomp_full/BattleManager__AddGoesSecondCardToDeck.c:87` —— `uVar9 = *(undefined8 *)(lVar4 + 0x150);`
  （`lVar4` 的梯子见 `:80-86` = `playerManager.heroCard.rawCardData`）
- `out_cpp2il_dev_ilspy/BattleManager.cs:48465` —— `obj = rawCardScript.goSecondCardInHand;`
- `out_il2csharp/Assembly-CSharp/BattleManager.cs:26906` —— `rawCardScript1 = rawCardScript2.goSecondCardInHand;`
- 调用点：`BattleManager._StartBattlePhase_d__334__MoveNext.c:200-203`：
  ```
  cVar5 = IsTutorialOrCampaignMatch();
  if ((cVar5 == '\0') && (cVar5 = SpriteAtlasReference__ValidateAsset(lVar3,0), cVar5 == '\0'))
      BattleManager__AddGoesSecondCardToDeck(lVar3,0);
  ```
- ⚠️ 那道 `SpriteAtlasReference__ValidateAsset` **不是真条件**：`decomp_full/SpriteAtlasReference__ValidateAsset.c` 全文就是 `return 0;`
  （ICF 折叠的共享桩；`// RVA: 0x6FB110` 在 `dump.cs` 里 **640 处**同址）⇒ **恒真**。
  ⛔ 但**真正被折叠的是哪个方法查不到**（Ghidra 名字不可信），只是「返 0」这件事对控制流是确定的。

### 3. 另两列（同一组，顺手核了）

- **`startingTroopsInPlay(+0x158)`**：读点在 `BattleManager.SetupStartingTroops`
  （`out_cpp2il_dev_ilspy/BattleManager.cs:15593-15604` 与 `:16172-16183`；`out_il2csharp/BattleManager.cs:1159-1167` / `:1572-1580`）
  ＋ 表现层 `CardScript.cs:15441-15449`（`GetOriginalWarlord(isPlayer).startingTroopsInPlay`）。
  现读 **1,351 张里 0 张非空**。
- **`starterHero(+0x160)`**：三源**只命中声明**（`RawCardScript.cs:156` / `out_il2csharp/RawCardScript.cs:63`）—— **没有任何读点**。现读 1,351 张全 0。

### 4. 数据真值（现读，不是抄旧账）

`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张 `RawCardScript` SO，bundle 内名 `54bdb7b1114a2de41519cac560a4bce4.bundle`）的 `full_raw`：

| 字段 | 非零行数 | 明细 |
|---|---|---|
| `startingCardInHand` | **3** | `SW1 Logan Grimnar` → `Z-NC SpaceWolves-Tyrnak and Fenrir`（`6e4f9040e8cc59641a32f29e33ea4cbc`）· `BL36 Sylar Hexscorn` → `T1 BlackLegion-Abaddons Chosen`（`BL37`）· `BlackLegion_war_Sylar Hexscorn_Tutorial`（`b5ee585c…`）→ `…_Tutorial_Nurgle`（`7ae784e5…`） |
| `goSecondCardInHand` | **64** | 13 个阵营各一张，见 `warlord_goes_second.json` |
| `startingTroopsInPlay` | **0** | —— |
| `starterHero` | **0** | —— |

三行的 `cardType` 全是 `10`(hero) —— 与「只有督军有」一致；卡片正文也逐字对上：
`BL36 desc = "Start the game with an Abaddon's Chosen in hand."` · `SW1 desc = "Start the game with Tyrnak and Fenrir in hand."`。

---

## 我们引擎对应哪条链（文件:行）

### `startingCardInHand` → 现在是**卡面文字解析**，不是这个 PPtr

```
cards_engine.json 的 desc
  → CardDef.StartWithInHand（CardDef.cs:4703；正则与抽取 = CardDef.cs:4705-4726 `^start the game with (a|an|the)?(.+?) in hand\.?$`）
  → RuleCore.SetupStartWith（RuleCore.cs:1118）
  → 调用点 RuleCore.cs:406（`for (int p = 0; p < 2; p++) SetupStartWith(ctx, p);`）
  → 现成断言 Editor/RuleEngineTest.cs:8005 / :8010 / :8020-8023
```

**两处与原文不符（都是我们自己挑的，⛔ 别当原版）**：

| # | 原版 | 我们 | 影响 |
|---|---|---|---|
| ① **时机** | 换牌**结束之后**才发（`StartBattlePhase`，`MoveNext.c:205`） | `RuleCore.cs:406`，换牌**之前** | 这张卡在换牌阶段**能被换掉**；原版不能 |
| ② **来源** | `CreateCard` **新建一张**（牌库不动） | `RuleCore.cs:1133-1137`「牌库里有就**从牌库挪**（`ps.Deck.RemoveAt(at)`）」 | 整副牌的构成变了（注释 `:1135` 里写的理由「凭空多一张会改变构成」是**自设计**） |

### `goSecondCardInHand` → **已完整接线**（可直接照抄这套三件套）

```
数据/游戏数据/warlord_goes_second.json（工具/gen_warlord_goes_second.py，从 cards_raw.json 的 full_raw 反查）
  → 工具/gen_cards_engine.py:5846 写 `goesSecond` 列
  → Data/CardDatabase.cs:82（DTO 字段）→ :1541（`goesSecondId: c.goesSecond`）
  → CardDef.GoesSecondCard（CardDef.cs:362）/ GoesSecondId（:366）
  → CreatePool.ResolveGoesSecond（CreatePool.cs:716）
  → RuleCore.GoesSecondCard（RuleCore.cs:796）→ GrantDefenceCard（:733）
  → 调用点 RuleCore.cs:238/:239（换牌不开）与 :517/:518（EndMulligan）
```

---

## 施工单（若找到了：要加哪几列、DTO 改哪、读点接哪、断言怎么写）

> 目标不是「加一列」，是**把 `startingCardInHand` 的真相源从卡面文字换成 PPtr，并把时机挪回原版**。

**① 数据层（照 `goesSecond` 那套一模一样）**
1. 新脚本 `工具/gen_warlord_starting_card.py`（照 `gen_warlord_goes_second.py` 的形）：源 = `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw.startingCardInHand`，按 `m_PathID` 反查成 `uniqueId`，产出 `数据/游戏数据/warlord_starting_card.json`。
   断言（脚本内，不成立就抛）：**恰好 3 行** · 全部是 `cardType == hero` · 只有 13 阵营的督军卡出现。
2. `工具/gen_cards_engine.py`：加一列 `startingCardInHand`（字符串 id；不是督军就不写这个键 —— 与 `alternateArts` 的 `null ≠ []` 同口径，缺键 = 判不了）。
3. `Data/CardDatabase.cs`：DTO 加 `public string startingCardInHand;`，`Parse` 里 `CardDef(..., startingCardInHandId: c.startingCardInHand)`。

**② 引擎层**
4. `CardDef`：加 `StartingCardInHand`（`CardDef` 直接引用）+ `StartingCardInHandId`（原始串），doc 照 `GoesSecondCard` 那套三态写（反查失败 ⇒ null + `CardDatabase` 出声）。
5. `CreatePool`：与 `ResolveGoesSecond` **合并成一趟**（两个调用方 `CardDatabase.Parse` 与 `ruleprobe/Program.LoadPool` 都在调，⛔ 别只改一处）；反查失败要 `LogError` 出声。
6. **读点 = `RuleCore.StartBattlePhaseHops`（`RuleCore.cs:16399`）**：两侧督军各一次，`StartingCardInHand != null` 时 `ctx.NewInstance(...)` + `AddCardToHandWithinLimit(ctx, seat, inst)`。
   选这里的原因：原版就在「`AddGoesSecondCardToDeck` 之后、`NextTurn` 之前」，而 `StartBattlePhaseHops` 正是我们这两跳之间的那一格（`RuleCore.cs:422` / `:518`）。
   ⚠️ **先修下面 §顺手发现 ① 那个「hop 可能一次都不跑」的坑**，否则挂了也不生效。
7. **收掉旧路，否则会发两张**：`StartingCardInHand != null` 时让 `SetupStartWith` 跳过同名卡；
   并把 `SetupStartWith` 的调用点从 `:406` 挪到同一个 hop（**时机单一化**）。
8. **②那条偏离要不要改**：原版是「新建一张、牌库不动」⇒ 按铁律 18·0（默认复刻原版）应改成 `NewInstance`。
   若判定「挪牌库」要保留，必须在代码里按铁律 18 打 `🔶 自己设计（非原版）` 并写清「为什么拿不到判据」—— 但这条**不是服务器相关**，按 18·0 应收敛到原版。

**③ 断言（每条都给了「怎么把它弄红」，防自证）**
- 真卡 A：`BL36 Sylar Hexscorn` 开局手里有 `BL37 Abaddon's Chosen`。
- **灭自证**：同一局 `ps.Deck` 里 **仍然有** `BL37` —— 把实现改回「从牌库挪」这条就红（`RuleEngineTest.cs:8022-8023` 现在断的**恰好相反**，要同步改）。
- 真卡 B：`SW1 Logan Grimnar` → `Tyrnak and Fenrir`（`6e4f9040…`；见顺手发现 ②）。
- **时机**：`openMulligan:true` 时，`EndMulligan` **之前**手里没有它、**之后**有 —— 把调用点挪回 `:406` 就红。
- 反例：一张没有该字段的督军（例 `Goff` 任意一位），开局手牌张数与改前逐张相同。
- 教程档：`BlackLegion_war_Sylar Hexscorn_Tutorial`（`b5ee585c…`）与它的目标卡**都不在池** ⇒ 教程局必须**显式跳过并出声**，别让它落进 `FindByName` 失败的静默分支。

---

## 查过哪些源 / 哪些词（⛔ 别只写「查不到」）

| 源 | 搜了什么 | 结果 |
|---|---|---|
| `d:/2/tools/il2cpp_out/dump.cs` | `startingCard` / `startCard` / `initialCard` / `extraCard` / `cardInHand` / `startWith`（大小写不敏感） | 命中上述 4 个字段；另命中 `GetHeroExtraCardsCount`（**读的是 `+0x118 initialHandModifier`，不是本字段**，见 `decomp_full/PlayerHand__GetHeroExtraCardsCount.c`）与 `AddInitialCardToDeck(RawCardScript startingCard, …)` |
| 同上 | `RVA: 0x6FB110` | **640 处**（共享桩，用于判 `SpriteAtlasReference__ValidateAsset` 那道闸） |
| 同上 | `createHandCard` | `BattleActionType.createHandCard = 23`（`dump.cs:45333`） |
| `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（整目录递归） | `startingCardInHand` | **只 1 处**：`RawCardScript.cs:150`（声明）—— 方法体被 ILSpy 丢了 |
| 同上 | `goSecondCardInHand` | `BattleManager.cs:48465` ＋ `RawCardScript.cs:152` |
| 同上 | `startingTroopsInPlay` / `starterHero` | `BattleManager.cs:15593/15604/16172/16183` · `CardScript.cs:15441-15449` / 仅声明 |
| `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/`（递归） | `startingCardInHand` | `BattleManager.cs:40396/40404/40408/40417` ＋ `RawCardScript.cs:60` |
| 同上 | `goSecondCardInHand` | `BattleManager.cs:26906` ＋ `RawCardScript.cs:61` |
| 同上 | `starterHero` | **仅声明**（`RawCardScript.cs:63`）—— 0 读点 |
| `d:/2/tools/decomp_full/`（25,096 个 `.c`） | 字段**名** | **0 命中**（C 伪码不含字段名 —— 这一行不能当否定证据） |
| 同上 | `0x28) + 0x148` | **唯一命中** `BattleManager._StartBattlePhase_d__334__MoveNext.c:210/218/235/243` |
| 同上 | `0x150`（在 `BattleManager__*.c` 内） | `BattleManager__AddGoesSecondCardToDeck.c:87` |
| 同上 | 文件名 `AddGoesSecond* / AddInitialCard* / StartingTroops / InitialHands` | 6 个文件全读（`AddGoesSecondCardToDeck` / `AddInitialCardToDeck` / `CreateStartingTroops` / `GetInitialHandsCards` / `SetupStartingTroops` / `__AddGoesSecondCardToDeck_b__870_0`）；`GetInitialHandsCards.c` 全文仅 14 行、与本题无关（`B92_Q7` 的排除结论仍成立） |
| `d:/2/新解包资源/assets_full/`（整库） | `grep -rl "startingCardInHand"` | **0 命中** —— ⛔ **不能当否定证据**：序列化 `MonoBehaviour` 的字节里不含字段名（判「本地有没有」必须按 `m_Script.m_PathID → m_ClassName` 反查）。真值改走 `cards_raw.json`（就是从这个 bundle 的 1,351 张 SO 直读的） |
| `d:/4/_tmp_view/decomp_b91/` | 目录清单 | 只含**泛型类**（`BaseInboxEvent<>` / `ConfigProvider<>` / `ReferenceList<>` 等）—— **不覆盖 `BattleManager`**，故 0 命中是「不覆盖」不是「没有」 |
| `d:/4/Unity/MyGame/Assets/`（全仓 `.cs`） | `startingCardInHand` | **0 命中**（我们走 `StartWithInHand`）· `goesSecond` 命中一串（见 §三） |

---

## 没查清的部分

1. **`BattleActionType.createHandCard(23)` 的结算体**：`decomp_full/BattleManager__ResolveAction.c:2627`（`case 0x17:`）Ghidra 产物读不出语义，ILSpy 那份 `ResolveAction` 里搜不到 23 这一支（`grep "createHandCard"` 只有 `:39148` 那个构造点）。
   ⇒ **「这张起始卡进场会不会发 `BroadcastCardCreated` / 触发『卡进手牌』类 trigger」尚未确定**。要定就得上 VA 级反汇编（`0x180952380` 那条 switch）。
2. **`SpriteAtlasReference__ValidateAsset` 那个共享桩真身是哪个方法**（ICF 折叠，`out_il2csharp` 标注 `shared body, 3540 candidates`）。只知道它**返 0**，不知道它「本来是干什么的」。
3. **`AddInitialCardToDeck`（ILSpy `BattleManager.cs:48383` · `.c` 全文在）三源 0 调用点**：名字像「把起始卡塞进牌库顶」的旧路。
   按铁律「0 调用点可能是内联假象」复核过：它带 `ref List<CardScript>` 形参，内联可能性低，但**仍不能据此判死代码**。
4. `AddNewCardToHand` 里 `priority=1` / `blockPlayerActions=0` 两个实参各自的下游用法没逐句核（ILSpy 那段被 `NoteDecompilerIssue` 打花了）。

---

## 顺手发现

1. 🔴 **`StartBattlePhaseHops` 可能一次都不跑**（未记在账上的真缺陷）：
   `RuleCore.cs:422` 只在 `!ctx.MulliganOpen` 时调；`RuleCore.cs:517-518` 只在 `PendingDefence` **有值**时调，
   而 `PendingDefence` 只在「至少一方拿到了防御卡」时才 `Add`（`:243-248`）。
   ⇒ **换牌阶段开着 + 双方都没有防御卡 ⇒ 这个 hop 永远不执行**。任何要挂在这里的东西（本单的 `startingCardInHand`、以后别的 `StartBattlePhase` 跳）都会**静默失效**。建议先单独记一条账。
2. **简报里的「`SW1 → Tyrnak and Fenrir`（❌不在池）」现读不成立**：
   `6e4f9040e8cc59641a32f29e33ea4cbc` 就在 `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（现读 **1,129 张**）里，
   而且 `RuleEngineTest.cs:8022` 的用例正在用它跑通。旧记录（多半是按 `poolTrueIds` 或旧的 1,127 口径判的）该按铁律 5 订正。
3. **教程那两行（`b5ee585c…` 督军 / `7ae784e5…` 目标卡）确认都不在池** —— 符合「教程卡走 `TutorialRules`」的设计，但施工时必须**显式排除并出声**，否则会静默落进 `FindByName == null` 那一支。
4. **ILSpy 丢的是整条 `_StartBattlePhase_d__334.MoveNext`，不是局限这一处**：
   以后凡「某字段全树 0 命中」，先按 `CLAUDE.md` §三那条去 `out_il2csharp` / `decomp_full` 复扫，⛔ 别直接判「原版没有读取点」。
   （同族第三次现形：`ILSpy 丢异步状态机体` / `ILSpy 丢 = true 写点` / `decomp_full 文件名与内容不符`。）
5. `PlayerHand.GetHeroExtraCardsCount` 读的是 **`+0x118 initialHandModifier`**（不是 `startingCardInHand`）——
   我们已有 `CardDef.InitialHandModifier`（`RuleCore.cs:283-291`）覆盖，**不是新缺口**。
