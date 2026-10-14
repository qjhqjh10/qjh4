# `A1949` —— `ChooseShowMax = 3` 那条判据换成原版第一权威（2026-10-14）

> **本笔只动一个文件**：`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（`ChooseShowMax` 相关）。
> **判据来源**：原版反编译方法体 `d:/2/tools/decomp_full/`（第一权威）+ 原版卡 SO 实读
> `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（第二，但本笔是**数据**不是语义）。
> ⚠️ 那份《Warpforge Offline Rulebook》**不作出处**（它自己第 5 行 `Not official. Fan project.`）。

---

## 一、原版那条路是什么（`文件:行号` · 它怎么算 · 取几 · 有几档）

**入口链**（原版 `BattleManager`）：

| 步 | 位置 | 干什么 |
|---|---|---|
| ① | `BattleManager__ChoiceOfCardPlayer.c:27` | `lVar2 = BattleManager__GetChoiceOptions(this, actingCard, targetCard, 0)` |
| ② | `BattleManager__ChoiceOfCardPlayer.c:50-53` | 把 `lVar2` **原样**塞进 `ChooseCardMenu__Setup(...)` —— 这就是面板的 `chooseCards`（`ChooseCardMenu.cs:131`） |
| ③ | `BattleManager._ChooseCardMethod_d__449__MoveNext.c:16-24` | 协程入口：`GetChoiceFullPool` 空 ⇒ 直接回调 0；否则玩家走 `ChoiceOfCardPlayer` / AI 走 `ChoiceOfCardEnemy` |

**它怎么算**（`BattleManager__GetChoiceOptions.c`，签名见桩 `BattleManager.cs:5744`
`GetChoiceOptions(CardScript actingCard, CardScript targetCard, bool isBotAndNotReplay = false)`）：

```c
:40  if (actingCard != null && actingCard.rawCardData != null
:41      && (lVar8 = actingCard.rawCardData.choicesDefinition) != null
:42      && (iVar1 = *(int *)(lVar8 + 0x40), pool != null)) {      // ← iVar1 = maxTargets
:43      if (*(int *)(lVar5 + 0x18) <= iVar1) iVar1 = *(int *)(lVar5 + 0x18);   // ← 夹到「候选池张数」
:46-70   for (k = 0; k < iVar1; k++) {
:53          iVar3 = BattleManager__GetRandomInt(this, 0, pool.Count);
             options.Add(pool[iVar3]);
:66          pool.RemoveAll(x => x == 刚抽中的那张);              // ← 不放回
         }
         抽不满 ⇒ 从池子的副本（`ToList()`）里按原序补齐
:110-113 if (options.Count <= iVar1 && !isBotAndNotReplay) options.Sort(CompareChoiceOptions);
         return options;
```

**偏移链**（`dump.cs` 逐字段给偏移，逐条现核）：

| 字段 | 偏移 | 出处 |
|---|---|---|
| `EntityScript.rawCardData`（`CardScript` 的基类） | **0x28** | `d:/2/tools/il2cpp_out/dump.cs:21968` |
| `RawCardScript.choicesDefinition`（`TargetCriteria`） | **0x2A0** | `dump.cs:23341` |
| `TargetCriteria.maxTargets`（`int32_t`） | **0x40** | `il2cpp.h:16727`（`TargetCriteria_Fields` 内） |

> 偏移 0x40 与字段表**独立吻合**：`TargetCriteria` 从 0x10 起 `targetsAffected`(4) + 15 个 bool + `chosenArmy`(0x24) +
> `armyFilter`(0x28) + `keywordFilter`(0x2C) + `excludeRace`(0x30) + `traitsFilter`(0x34) + `conditionsFilter`(0x38) +
> 两个 bool(0x3C/0x3D) ⇒ `maxTargets` 正好落在 **0x40**；再往后 `filterSpecificCards` 落在 **0x70** ——
> 与 `GetChoiceFullPool.c` 里另一处 `*(longlong *)(criteria + 0x70)` 也对得上。**两处互相印证。**

**取几 / 有几档**（`iVar1` 就是档位）：

* **不是常数** —— 每张卡自己一份数据（`RawCardScript.choicesDefinition.maxTargets`）。
* **夹到候选池张数**：`min(maxTargets, pool.Count)`。
* **对【每一个】候选域都这么做**：方法体里**没有**「只对大池子抽」这种分支。
  域由 `TargetCriteria.targetsAffected` 定（`dump.cs` 的 `TargetsAffected` 枚举）：
  `inHandFiltered=50` / `inCemeteryFiltered=60` / `inDeckFiltered=65` / `choiceResolution=180` … **四种都照样夹**。
* `maxTargets <= 0` ⇒ `iVar1 = 0` ⇒ 循环一次不跑、返回**空表**（共 977 张卡是这一档 = 这些卡不走这条路）。

### 状态 → 参数（铁律 5·c：一个值 ≠ 全部情况）

| 情况 | 原版 `maxTargets` | 卡数（2022 版卡表实读） | 我们的旧值 | 我们的新值 |
|---|---|---|---|---|
| 该卡不走 choose | `0` | **977** | —— | —— |
| **众数**（走 choose 的绝大多数） | `3` | **204** | 3 ✅ | 3 ✅ |
| 黑团「给黑暗契约」等 | `4` | **10** | 3 ❌ | 表里逐条记 |
| `Storm of Silence` | `2` | **1** | 3 ❌ | 见 §五（对不上号） |
| 2022 卡表里没有的**新卡**（13 张） | **未知** | —— | 3 | 3（**如实标缺口**） |

---

## 二、「3」对不对（原版值 vs 我们值 · 判据出处）

**结论：3 是【众数】，不是【规则】。** 原版逐卡读 `choicesDefinition.maxTargets`（见 §一的偏移链与分布表）。

**分布**（`cards_1195_2022时代.json`，1195 张卡里 **1192 张**有非 null 的 `choicesDefinition`）：

```
maxTargets = 0  → 977 张
maxTargets = 3  → 204 张   ← 众数（= 我们那个常数）
maxTargets = 4  →  10 张
maxTargets = 2  →   1 张
```

**那 11 张非 3 的卡**（逐条现读）：

| 原版 `uniqueId` | 卡名 | `maxTargets` | 在我们池里的 `CardDef.Id` |
|---|---|---|---|
| `GSC65` | Pilfered Supplies | **4** | `GSC65` ✅ |
| `BL4` | Chaos Sergeant | 4 | `BL4` ✅ |
| `BL13` | Chosen | 4 | `BL13` ✅ |
| `BL29` | Terminator Champion | 4 | `BL29` ✅ |
| `BL37` | Abaddon's Chosen | 4 | `BL37` ✅ |
| `BL38` | Dark Oratory | 4 | `BL38` ✅ |
| `BL57` | Helfire Torch | 4 | `BL_Hellfire_Torch`（拼写差一个 `l`，**没敢当同一张**） |
| `EC60` | Tools of Torture | 4 | 不在我们池里 |
| `7ae7…ad6d` | Abaddon's Chosen (tutortial) | 4 | 不在我们池里 |
| `851f…91db` | Storm of Silence | **2** | `ASH_Storm_of_Silence`（原版表里**同名三条 `0/2/0`** ⇒ 不猜） |
| `141c…d682` | Unhallowed Gifts | 4 | `BL_Unhallowed_Gifts`（原版表里**同名两条 `0/4`** ⇒ 不猜） |

🔴 **「数不能从文本推」的硬证据**（同一句、两个值）：

* `GSC65 Pilfered Supplies` —— `Choose a Sabotage card and add it to your opponent's hand` ⇒ 原版 **4**
* `GSC50 Clandestine Operation` —— `Choose a Sabotage card and add it to your opponent's hand. Draw a card` ⇒ 原版 **3**

而且 `Sabotage` 在我们池子里**正好 4 张**（`GSC_Cult_Propaganda` / `GSC_Improvised_Barricade` /
`GSC_Jammed_Communications` / `GSC_Poisoned_Supplies`）⇒ 原版 `maxTargets=4` 夹到池子大小 = **四张全摆**，
我们旧代码 `4 > 3` ⇒ **随机抽 3、藏掉 1 张**。**这是我们唯一能当场证出来的可见偏离。**

**我们这一层实际走得到这个面板的 choose 卡**（判据 = `EffectText.TryChooseCard`，要求句子以 `choose ` 开头）：
卡面含 `choose` 的 61 张 → 与 2022 卡表按名字对得上 48 张：**44 张 = 3** · **1 张 = 4**（GSC65）·
**3 张 = 0**（那 3 张是 `choose one:` / `Choose an effect`，**走的不是本路**，见 §三末）；
另 **13 张新卡**在 2022 卡表里查不到。

**另一条与原版不符的（同一条规则的另一半）**：旧代码 `bool bigPool = op.ChooseSrc == "pool" || "deck"`
⇒ 只对全卡池 / 牌库夹上限，**手牌 / 对手手牌 / 阵亡堆不夹**。原版**分域不分**（§一）。
反例就在原版数据里：`Reconnaissance Mission`（DA42，`Choose a card in your opponent's hand`）是
`maxTargets = 3` + `targetsAffected = inHandFiltered(50)`；`Eternal Servitude`（EC39）是 `3` + `inCemeteryFiltered(60)`。
⇒ 这两条**都改了**（见 §三）。

---

## 三、改了没有（改了：`文件:行号` 改前 / 改后 + 跑什么能验）

**文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（⚠️ 行号 = **改后**现读）

### 改点 1 —— 判据注释 + 常数（`BattleDriver.cs:5569-5651`）

* **改前**：`const int ChooseShowMax = 3;`，上面那段 `<summary>` 只引
  「规则书英文版 `:475`」+「用户 2026-09-14 口头口径」，并自述「**只对「大池子」抽** … **这一条是我们挑的**」。
* **改后**：
  * `const int ChooseShowDefault = 3;` —— 名字改成「**默认值**」（它不再冒充规则）。
  * 新增 `static readonly Dictionary<string,int> ChooseShowByCardId`（**原版 6 张非 3 的卡**，键 = `CardDef.Id`）
    + `static int ChooseShowLimit(CardDef c)`。
  * `<summary>` 换成**原版反编译方法体**：完整的 `GetChoiceOptions.c:40-43` 源码片段、
    0x28→0x2A0→0x40 的偏移链（三个出处 `dump.cs:21968` / `dump.cs:23341` / `il2cpp.h:16727`）、
    「夹到池子大小」「循环抽不放回 + 补齐」「最后 Sort」、**「不分候选域」**、
    以及那份 977/204/10/1 的分布与「3 只是众数」。
  * 明确写出**我们还没搬的那一半**（卡数据里没有 `choicesDefinition` 字段 ⇒ 默认 3；13 张新卡原值未知）
    —— **不许当「原版就是 3」**。
  * 把那份粉丝规则书**降级成第二来源**并说明它为什么不够用（同一句话两个值）。

### 改点 2 —— 那一处逻辑（`BattleDriver.cs:6128-6149`）

* **改前**：
  ```csharp
  bool bigPool = op.ChooseSrc == "pool" || op.ChooseSrc == "deck";
  if (bigPool && cands.Count > ChooseShowMax) { … DrawForDisplay(cands, ChooseShowMax) … }
  ```
* **改后**：
  ```csharp
  int showMax = ChooseShowLimit(PendingCard);
  if (cands.Count > showMax) { … DrawForDisplay(cands, showMax) … }
  ```
  ⇒ ① 上限**逐卡**（GSC65 = 4）；② 上限**对所有候选域生效**（删掉 `bigPool`）。

### 跑什么能验

| 层次 | 怎么验 | 状态 |
|---|---|---|
| **编得过** | `TMPDIR=/tmp/wf_a1949 bash d:/4/Unity/工具/typecheck.sh` | ✅ **跑过，运行时 0 错 · 编辑器 0 错** |
| **GSC65 摆 4 张** | ⚠️ **还没有断言** —— 要加一条「打出 `Pilfered Supplies` ⇒ `ChooseOptionCount == 4`」，宿主是 `CardPresentation/Editor/BattleScene.cs`（`:14596` 那一节），**不在本笔白名单** ⇒ 报给主对话 | ⛔ 未做 |
| **不回归** | 现成那条相关断言 = `BattleScene.cs:14627` `Check(driver.ChooseOptionCount >= 2, …「Rapid Deployment」手牌里的部队)` —— 改后仍成立（`Rapid Deployment` 原版上限也是 3）；`14943` / `15020` 是 `>= 1` ⇒ 不受影响 | ⛔ **没跑 Unity**（见 §四） |
| **引擎侧不动** | 本笔只动**表现层**（面板摆哪几张）；`RuleCore.ChooseCardCandidates` 的候选表**一个字节没改** ⇒ 引擎那批断言口径不变 | —— |

---

## 四、验了什么（类型检查 · 有没有跑断言 · 没跑就写没跑）

| 验的东西 | 命令 / 做法 | 结果 |
|---|---|---|
| **秒级类型检查**（改完 `.cs` 立刻跑） | `TMPDIR=/tmp/wf_a1949 bash d:/4/Unity/工具/typecheck.sh` | ✅ **运行时错误数 0 · 编辑器错误数 0**（独立 `TMPDIR`，不与别的写手抢输出） |
| **行尾没被翻**（铁律） | 逐字节数 `\r\n` / `\n` | ✅ `CRLF 15802 / LF 15802`（**bare LF = 0**，整篇仍是 CRLF） |
| **改动规模** | `git diff --numstat` | `123 / 18`（远小于 15802 行 ⇒ 没有整篇重写；其中还含**别的写手**在 `:2769` 之后的 `A1892` 在飞改动，⛔ 没碰） |
| **旧符号清干净** | `grep -rn "ChooseShowMax" --include=*.cs Unity/MyGame/Assets` | ✅ **0 命中** |
| **Unity 自检** | —— | ⛔ **没跑**（本笔是纯注释 + 一处表现层逻辑；按铁律 12 的覆盖面判据与第十七会话口径「别全量自检」，且宿主 `BattleScene.cs` 不在白名单 ⇒ 没法为本笔加断言） |

---

## 五、没查清 / 需要别人决定的

1. 🔴 **真正的解法是把 `choicesDefinition.maxTargets` 搬进我们的卡数据**（然后 `ChooseShowLimit` 直接读它，
   本文件这张表删掉）。要动 `数据/游戏数据/cards_engine.json` + `工具/gen_cards_engine.py` + 解析层
   （`EffectText.TryChooseCard` → `EffectOp`），**全在本笔白名单外** ⇒ **请主对话分流**。
   ⚠️ 已知那条路的前置缺口：当前卡池的「真值表」只采到 **4.3%**（账 `A970`，挂起等远端 `allcards`）。
2. **4 张对不上号的**（`BL57 Helfire Torch` / `EC60 Tools of Torture` / `Storm of Silence` / `Unhallowed Gifts`）
   —— 见 §二那张表：原版表里它们是 **GUID 型 `uniqueId`** 或**同名重复**（`0/2/0`、`0/4`）⇒ **没猜**。
   要补得先把 id 关系查出来。
3. **13 张新卡原值未知**（`Azrael` / `Watcher in the Dark` / `Master Zacharial` / `Reconstitution Protocols` /
   `Prophet of the Waaagh` / `High Rune Priest` / `The Fang` / `Saviour Protocolst` / `Suppressor` /
   `Author of the Codex` / `Lead From the Front` / `Decadent Throne` / `Duelist's Hubris`）——
   在 2022 版卡表里查不到，**一并按 3 走**（如实标缺口）。
4. **原版最后还 `Sort(CompareChoiceOptions)` 过一次**（`GetChoiceOptions.c:110-113`，
   条件 `!isBotAndNotReplay`）—— **比较函数没解出来**（`DAT_18423e6f8` 未解析，
   `decomp_full/` 里没有对应文件；桩里 lambda 被剥）。⇒ **我们没做这一步**（面板顺序 = 随机抽到的顺序）。
   要不要做、以及它是不是按费用/名字排，**得先把这个方法读出来**（可能要用 VA 反汇编，见
   `资料/全量反编译_入口与用法.md`）。
5. **`maxTargets = 0` 那 977 张**在 `GetChoiceOptions` 里会返回**空表** —— 本笔**没有**把这条落到我们代码里
   （我们的默认值是 3，不会出现「上限 0」）。⚠️ 若将来搬真数据，**这一档要单独决定**（原版返空 ⇒
   面板会摆 0 张卡，`ChooseCardMenu` 那边怎么收场**没读**）。

---

## 六、顺手发现（只报，⛔ 一处都没顺手改）

1. 🔴 **黑团「给黑暗契约」那一族在原版是【让玩家从 4 张契约里挑】，我们这一层没接。**
   `Chaos Sergeant` / `Chosen` / `Dark Oratory` / `Abaddons Chosen` / `Terminator Champion` 的原版
   `choicesDefinition = maxTargets 4 + targetsAffected 180(choiceResolution)`（§二那张表），
   而 `TargetsAffected.choiceResolution = 180` 正是「**选择结算**」那一档。
   **我们**：卡面文字是 `Gain a Dark Pact`（**不以 `choose` 开头**）⇒ `EffectText.TryChooseCard` 不发 `choosecard`
   ⇒ 那 4 张契约眼下**只**喂给卡片详情窗的「相关卡」（唯一调用点 = `CardPresentation/Core/RelatedCards.cs:163`
   → `CreatePool.DarkPactContracts`）。⇒ **「给黑暗契约」我们大概是随机/指定给的，原版是玩家挑** ——
   这是一条**独立的复刻缺口**（不是 `A1949`），⛔ 本笔没动（解析层在白名单外）。**请主对话立账。**
2. **`TargetsAffected` 枚举是查「候选域是什么」的钥匙**（`dump.cs` 那一段，值已在 §一列了常用的四个）——
   以后读 `choicesDefinition` 别只看 `maxTargets`，`targetsAffected` 才是域。
3. **`decomp_full/` 里有文件名与内容对不上的情况**：`BattleManager.__c__DisplayClass486_0___GetChoiceOptions_b__0.c`
   的**文件名**像是 `GetChoiceOptions` 的比较 lambda，**内容却是 `AiScripted` 的**
   （`AiScripted_<>c__DisplayClass33_0__<ExecuteAction>b__0`）⇒ **按文件名认内容会认错**，
   凡「按名字找方法体」都要现读内容确认（与 `CLAUDE.md` §四那条「按猜的名字去找、没找到」是同一类坑）。
