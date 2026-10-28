# B89 第 1 波 · `R6` —— `战斗界面_HUD` 里「锁在 BattleDriver 里」的那批，逐条施工单

> 代理：`B89_R6`（**查判据**，只读；唯一写盘 = 本文件）· 2026-10-28
> 消费者：**下一个拿到 `CardPresentation/Battle/BattleDriver.cs` 写权的执行代理**。
> ⛔ 本轮：没动任何 `.cs` · 没动账主表 · 没跑 Unity / ruleprobe · 没动 git · 没动 `d:/2`。

---

## §0 现读数字（⛔ 别抄简报里的 20）

```
python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --domain 战斗界面_HUD --status B   ⇒ 5 行
python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --domain 战斗界面_HUD --status C   ⇒ 19 行
python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --domain 战斗界面_HUD --status B,C --file BattleDriver  ⇒ 16 行
```

| 项 | 数 |
|---|---:|
| `战斗界面_HUD` 待办总数 | **24**（B **5** / C **19**） |
| 落点**含** `BattleDriver.cs` | **16**（其中 **755** 的主落点其实是 `Battle/SkillPanel.cs`，BattleDriver 只是调用点） |
| ⇒ 落点在 BattleDriver 的「真账」 | **15** |
| 这 16 条里**代码已到位、只差账升级**的 | **7**（693 悬停档 · 695 · 713 · 750 · 755 · 756 · 757） |
| 这 16 条里**BattleDriver 还有真残差**的 | **6**（689 · 704 · 712 · 741 · 759 · 378+364） |
| 这 16 条里**被数据/资产卡住、写不了**的 | **3**（684 · 20 · 709） |
| 不在 BattleDriver 的另外 8 条 | 537 · 802 · 306 · 684* · 687 · 761 · 805 · 806（*684 已计入上面，见 §1·C） |

> 🔴 **简报里「20 条全部落在 BattleDriver.cs」这句与现读不符** —— 现读 16 条（真落点 15 条），
> 且其中 **7 条已经被 E5c / E5e / R4b / W44HUD / W57BDR4 / W67CENTER / K2 / J3 做完了**（见 §1·A）。
> 简报点名「已有人做过」的那串物理行里，落在我这 16 条内的只有 **695 / 713** 两条；
> `761 / 802 / 805 / 806` 的落点根本不在 `BattleDriver.cs`（分别 `CardHighlight.cs` / `CardFeel.cs` / `NetRuntime.cs`）。
> `782 / 930 / 103 / 298 / 392 / 584 / 585 / 682` 现读**已不在 B/C 集合里**（E5b 已升 A/B）。

**行号全部按现读**（`BattleDriver.cs` 现读 **36,700 行**、**纯 CRLF**（`crlf 36700 / lines 36700 / bareCR 0`））。
> ⚠️ **2026-10-28 更正（执行写手 `B91_W3` · 铁律 5）**：本文件里所有「`36,700` / `:36699` / `:36700`」都是 **2026-10-27 的快照**，**已过期**。**现读 = `37,165` 行**（`crlf 37165 / LF 37165 / bareCR 0`），**类闭合 `}` 在 `:37164`、namespace 的 `}` 在 `:37165`**。
> **错因**：`B90_*` / `B91_*` 两批在类尾追加过成员，行号整体下移 —— 这不是文档写错，是**行号本身在漂**。
> ⇒ 🔑 **别照抄任何固定行号定位类尾**：读文件末尾，**倒数第 2 个 `    }` 就是类闭合**（倒数第 1 个顶格 `}` 是 namespace），追加在类闭合之前。
⚠️ 账里很多落点行号**已经漂了**（例：账写 `SetSavedMana:33908`，现读 **34028**；`ShowEndPanelAndFreezeInput:32107`，现读 **32227**）——
下一任写手**按符号名找，别按账里的行号找**。

---

## §1 逐条施工单

### 1·A —— 代码已到位，**写手零改动**，只差调度台升级账（7 条）

这一组不是「没人做」，是「做完了但账还挂在 C/B 上」。**写手别重复做**（重复做＝再写一份判据，铁律：两处写同一条规则迟早不一致）。

| 账行 | 原版方法 | 现读落点（符号名） | 判据 | 为什么可以升 A |
|---:|---|---|---|---|
| **695** | `BattleManager.CanMoveHandCard()`<br>`longlong BattleManager__CanMoveHandCard(longlong)` | `Hand/CardInteraction.cs:346-349` `DragBlocked()` / `:434-439` `IsDoingAnim` / `:722` `UpdateHover` 首句 | `BattleManager__CanMoveHandCard.c` 全文 8 行 | 原版两句都已在：`playerHand.animFlag` ⇒ `IsDoingAnim`（`_animGate`，`J3` 已建，判据 = `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:33-35/104-106/134`）；`UIstate == 1` ⇒ `DragOnlyBlocked()`（`K2`）。`PickUp` / `UpdateHover` / `BeginPressCandidate` 三个入口都过它 |
| **713** | `BattleManager.GetUnitSizeRatio(bool isPlayer)` | `Board/ArenaSlots.cs` `InPlayScale(bool)`（`E5c` 落地）→ `BattleDriver` `ArenaCardScale`（现读 `:22051` 一带，按符号找） | `BattleManager.cs:45540`（`!isPlayer ⇒ globalVars.enemyUnitsScaleRatio`，`isPlayer ⇒ 1f`） | `E5c` 已按 `minionInPlayScale(0.75) × (isPlayer ? 1 : 0.86)` 补齐第三支；且 **`GetUnitSizeRatio` 三源 0 调用者 = 死方法**（`E5c` §1·①），不按它另建 |
| **757** | `CardScript.GetEnemyCardPlayedDisplayScale()` | `BattleDriver.cs:26050` `const EnemyCardPlayedDisplayScale = 1.4f` · `:26071` `PlayedCardCenterAbsoluteScaleForTest` · `:26151` 消费 · `:26065` `EnemyCardPlayedMobileMultiplier = 1.3f` | `CardScript__GetEnemyCardPlayedDisplayScale.c:26-31`（基准 = `Vector3.one × globalVars+0x48`）/ `:53-61`（小屏再 × `+0x4c`） | **账里那句「常量已备、零消费者」已过期**：`PlayedCardCenterAbsoluteScaleForTest` 就是唯一消费者，且带 `SmallScreenUI.Enabled` 两档（1.4 / 1.82）。断言已存在：`Editor/BattleScene.cs:6166`（1.4）、`:6176`（1.82） |
| **750** | `CardScript.DisplayOffensiveCardInCenter(float, float)` | `BattleDriver` reveal 链：`BeginOffensiveRevealOrApply`（现读 `:10003`）· `TickOffensiveReveal` · `ApplyRevealInnerPose` · **`RevealStartPx/RevealEndPx`（现读 `:9860` 一带 = `(589f,495f)`）** | `CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c` 全文 | `B9_W44HUD` 已把落位按 `enemyHand(960,50) + 进攻档 (−25,+30)×14.835` 落地；**唯一残差 = `RevealStartPx` 是已标 `🔶 自己设计`**（原版起点是世界零点、z=0 不在 UI 平面，正交布局不可表示）。⇒ 建议调度台复判 A/B（`E11e` 同判） |
| **756** | `CardScript.GetEnemyCardPlayedDisplayPosition()` | `BattleDriver.cs:26105` `DisplayEnemyCardInCenter` → `:35814` `EnemyCardPlayedDisplayPointWorld()` → `RuleEngine/Core/GameplayVariables.cs` `VarsGlobal.PlayedCardDisplayOffsetPx` | `CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75` | `W57BDR4` 已把 `DOMove` 终点从屏幕正中改成 `(574,406)`/`(1331,406)`；`W67CENTER` 补了**两条结构断言**（`Editor/BattleScene.cs:15410` + 文件尾 `W67CenterAssert`）。**两条断言还没跑过**（本波禁 Unity）⇒ 跑绿即可升 A |
| **755** | `CardScript.GetCurrentLocalizedAbilityDescription()` | `Battle/SkillPanel.cs:2133` `AbilityDescription` · `:2187` `AbilityBareText` · `:2202` `AbilityRemovingSiege`（**主落点不是 BattleDriver**，BattleDriver 只是调用点 `ShowSkillPanel`） | `CardScript__GetCurrentLocalizedAbilityDescription.c:1-75` + `RawCardScript.cs:2374-2446` | `B9_W21SKILL` 已把前缀改成 `Card_Abilities/`、剥 `[[reload]]` 四支、裸键先查官方表；残差两件都**不在 BattleDriver**（`activeAbilityTextReferenceId` 数据列 · `id.Contains("((")` 那一支） |
| **693** | `BattleManager.CanDisplayHandCard(CardScript)` | **悬停预览那一路已做**：`Hand/CardInteraction.cs:722` `UpdateHover` 的 `if (DragOnlyBlocked()) return;`（`K2`）<br>**开大卡窗那一路是另一条判据**，见 §1·B·B6 | `BattleManager__CanDisplayHandCard.c` 全文 9 行；调用点 `CardScript__OnTouchEnter.c:20-33` | 原版的 `CanDisplayHandCard` 只喂**悬停放大**（`ShowCardInHand`），**不是**开大卡窗（开大卡窗走 `DisplayCard`，闸是 `{0,7,8,11,12}`，见 B6）。⇒ 账的落点（`OnCardTapped`）**指错了对象**，应改指 `CardInteraction.UpdateHover` |

---

### 1·B —— BattleDriver 有**真残差**、本波写手可以做（**6 条账** + 1 条**新账**）

> **动 `BattleDriver.cs` 的三条铁律**（`CLAUDE.md`）：
> ① **新成员一律追加到类尾**（~~现读类闭合 `}` 在 **`:36699`**，即追加在 **`:36698` 之后**~~ —— ⚠️ **2026-10-28 `B91_W3` 更正**：现读类闭合在 **`:37164`**，见文件头那条更正；**⛔ 别照抄行号，按「文件末尾倒数第 2 个 `    }`」定位**）；
> ② **⛔ 别插进已有成员之间**（账按 `文件:行号` 引用本文件，插一行整条下游漂移）；
> ③ 改完立刻 `bash d:/4/Unity/工具/typecheck.sh`（几秒），**别在 Unity 批处理跑着时改**。

---

#### B1 · 账 **689** `BattleManager.BasicBattleEndSequence()`

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :689 | BattleManager | BasicBattleEndSequence() | 状态 B
longlong BattleManager__BasicBattleEndSequence(undefined8 param_1)          // 工厂：建 <>d__391 状态机
undefined8 BattleManager_<BasicBattleEndSequence>d__391__MoveNext(longlong) // 真体
```

**② 原版判据**：`decomp_full/BattleManager._BasicBattleEndSequence_d__391__MoveNext.c:20-200`（**逐句全文已读**）。
关键 5 句（伪码，字段名已按 `il2cpp_out/dump.cs:30709-30990` 的 `BattleManager` 字段表还原）：

```
state0: yield WaitForSeconds(DAT_1834b2bb8 /* = 1.0f */);
state1: (this+0x110).gameObject.SetActive(false);   // +0x110 = cemeteryManager
        (this+0x140).gameObject.SetActive(false);   // +0x140 = chatButton
        local_28 = (matchResult+0x28 != 10);        // +0x28 = BattleWinner；Player=10 / Enemy=20 / Draw=30
        local_29 = (matchResult+0x24 == 2);         // +0x24 = BattleResult；2 = Forfeit
        if (matchResult+0x28 == 0x14) goto state2;  // 敌方胜 ⇒ 跳过「摧敌方」这一整侧
        StartCoroutine(DestroyUnitsAfterBattleEnd(this, isPlayer: 0 /*敌方*/, local_29));
        yield WaitForSeconds((n+1) * globalVars[0x8c] + globalVars[0x34]);   // (n+1)×0.2 + 0.5
state2: if (local_28) { StartCoroutine(DestroyUnitsAfterBattleEnd(this, isPlayer: 1 /*我方*/, local_29));
        yield WaitForSeconds((m+1) * globalVars[0x8c] + globalVars[0x34]); }
state3: yield WaitForSeconds(DAT_1834b2bb4 /* = 0.5f */);
state4: StartCoroutine(CompleteBattleFinished());
```
字段来源：`il2cpp_out/dump.cs:30750`（`cemeteryManager // 0x110`）· `:30759`（`chatButton // 0x140`）·
`:78887-78900`（`RankedMatchResult`：`battleResult // 0x24` / `battleWinner // 0x28`）·
`:45405-45430`（`BattleResult.Forfeit = 2` / `BattleWinner.Player = 10, Enemy = 20, Draw = 30`）。

**③ 改哪个符号、加在哪**
- `BuildEndBattleDestroySequence()`（现读 **`:36124`**）—— **三处改**：
  1. **侧序反了**：现读 `int side = i == 0 ? _me : 1 - _me;` ⇒ 原版是 **`isPlayer:0`（敌方）先**、`isPlayer:1`（我方）后
     ⇒ 改成 `int side = i == 0 ? 1 - _me : _me;`。
  2. **胜方不摧**：原版是「**输的那一侧才倒**」（`battleWinner == Enemy(20)` ⇒ 跳过敌方那一侧；`== Player(10)` ⇒ 跳过我方那一侧；平局(30) ⇒ 两侧都摧）。
     我们**本机完全判得出来**（账里那句「本机拿不到同一语义」**不成立**）：
     `Ctx.Winner`（`RuleEngine/Core/BattleContext.cs:371`，0 进行中 / 1-2 = 该方胜 / 3 平）+ `Ctx.ForfeitedBy`（`:374`，`-1` 无人投降）。
     ```
     int bw = Ctx.Winner == 3 ? 30 : (Ctx.Winner == _me + 1 ? 10 : (Ctx.Winner == (1 - _me) + 1 ? 20 : 0));
     bool curSideIsPlayer = side == _me;
     bool skipThisSide = (bw == 20 && !curSideIsPlayer) || (bw == 10 && curSideIsPlayer);
     ```
     ⚠️ `Winner == 0`（还在打）理论上进不到这里（`EndBattleDestroySequencePending` 首句已要 `Ctx.IsOver`）⇒ 那一支保底「两侧都摧」。
  3. **逐单位间隔 0.2 是错的**（原版 **0.3**），见 B2；本函数里 `t += EndSeqMinionWait` 那三处**同批改**。
- **两个 `SetActive(false)`**：我们**没有**整条 HUD 收——`BattleHudHideNoHand`（1 s clip）只搬 **7 颗 rig 节点**
  （`LeftArea/EnemyInfo` · `LeftArea/Left Anchor` · `LeftArea/PlayerInfo` · `RightArea/EnemyDeck` · `RightArea/PlayerDeck` ·
  `RightArea/Right Anchor` · `RightArea/SettingsBtn`；判据 = `Resources/battlehud_clips.json` 该 clip 的 `curves[]` 现读）
  ⇒ **`_chatBtn`（`:2267`）与 `_cemeteryBtn`（`:1726`）不在里面，必须另做**。
  ⇒ 在 `BuildEndBattleDestroySequence()` 里第一个时间点（`EndSeqInitialWait` = 1.0 s）加两条；
  或更好：给 `EndSeqStep` 加一个 `kind`（`0=death / 1=hideChat / 2=hideCemetery`），由 `EndBattleDestroySequencePending()` 的泵统一派发
  （**⛔ 别在 `ShowEndPanelAndFreezeInput` 当场写死** —— 那会让「等 1 s」变成「立刻」）。
  两个件的隐藏方式按本文件既有写法：`_chatBtn.gameObject.SetActive(false)` / `_cemeteryBtn.gameObject.SetActive(false)`
  （`HudPiece` 的引用已在，见 `:6016-6017` 的命中判定）。
- **4. 那条 `CustomDebug.Log`**（`state1` 里拼 `battleWinner.ToString() + " / " + battleResult.ToString()`）：原版**只打日志**、无观察量 ⇒ 我们可选补一条 `Debug.Log`，**不补不算差异**（写明即可）。

**④ 观测口**
- **批处理可验（推荐先做这一层）**：给 `EndSeqStep` 加只读自检口
  `public EndSeqStep[] EndSeqStepsForTest { get; }`（返回 `_endSeqSteps.ToArray()`）+ `public float EndSeqEndAtForTest`，
  然后在 `Editor/BattleScene.cs` 收局那一节断四条：
  ① 我方赢（`Ctx.Winner == _me+1`）⇒ `steps` 里**没有我方**的 `UnitState`；
  ② 敌方赢 ⇒ **没有敌方**；
  ③ 平局 ⇒ 两侧都有；
  ④ 相邻两个「逐单位」步骤的 `at` 差 **== 0.3f（1e-3 容差）**，首步 `at == 1.0f`，末尾 `_endSeqEndAt - 最后一步.at == 0.5f`。
- 真 Play 口（现读**零断言**，本函数此前一条断言都没有）：
  打完一局 ⇒ 1 s 后**输的那一侧**的单位从末格往回逐个倒下、**赢的那一侧站着**；约 1 s 后**聊天钮与墓地钮消失**。
- ⚠️ `EndBattleDestroySequencePending()` 首句 `if (!Application.isPlaying) return false;` ⇒ **批处理不跑真演出**，只有上面的「时间表断言」可跑。

**⑤ 风险**
- **改指纹**：否（不碰引擎，只碰表现层时间表）。
- **动手感**：**是**。三个可见变化：① **我方输了之后我军单位会全部倒下**（此前两侧都倒）；
  ② **敌方赢了之后我军单位不再倒**；③ 聊天钮/墓地钮在收局 1 s 后消失。⇒ 真机必须复看一次。
- **行数**：只改已有行 ⇒ **零漂移**；新增自检口**追加类尾**。

---

#### B2 · 账 **704** `BattleManager.DestroyUnitsAfterBattleEnd(bool isPlayer, bool skipDeathSound)`

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :704 | BattleManager | DestroyUnitsAfterBattleEnd(bool isPlayer, bool skipDeathSound) | 状态 B
longlong BattleManager__DestroyUnitsAfterBattleEnd(undefined8 this, undefined1 isPlayer, undefined1 skipDeathSound)
undefined8 BattleManager_<DestroyUnitsAfterBattleEnd>d__392__MoveNext(longlong)
```

**② 原版判据**：`decomp_full/BattleManager._DestroyUnitsAfterBattleEnd_d__392__MoveNext.c:20-190`（**逐句全文已读**）。

```
state0: hero = GetHero(isPlayer);
        if (hero == null || hero.gameObject == null || !hero.gameObject.activeSelf || GetMinionManager(isPlayer) == null)
            { CustomDebug.LogError(...); return; }
        hero.SetAsUnitDestroyed();
        StartCoroutine(hero.UnitDeath(skipDeathSound, /*isHero*/1, /*deathType*/0xF, 0));
        yield WaitForSeconds(globalVars[0x8c] + globalVars[0x34]);       // 0.2 + 0.5
state1: list = GetMinionManager(isPlayer).GetMinionList();  i = list.Count - 1;
state2: for (; i >= 0; i--) {
          var c = list[i];
          if (c != null && c.gameObject != null && c.gameObject.activeSelf) {
              c.SetAsUnitDestroyed();
              StartCoroutine(c.UnitDeath(0, 0, 0xF, 0));
              yield return this.sec3FractionDelay;      // ← 🔴 this+0x4A8
          } }
state3: yield WaitForSeconds(globalVars[0x8c]);                     // 0.2
```
🔴 **`this+0x4A8` = `sec3FractionDelay`**（`dump.cs:30891` 字段名现读）——
构造处 `BattleManager__.ctor.c:206-209`：`WaitForSeconds(DAT_1834b2dc8)`；
`工具/read_literal.py` 文件头的读数表：**`0x1834b2dc8 = 0.3`**。
⇒ **原版逐单位间隔 = 0.3 s，不是 0.2**。账里「各等 0.2」是**把外层 `689` 那笔 `(n+1)×minionDeathTime` 误当成内层间隔**。

**③ 改哪个符号、加在哪**
- `BattleDriver.cs:36120` `const float EndSeqMinionWait = 0.2f;` —— **拆成两条常量**：
  ```
  const float EndSeqMinionWait   = 0.2f;   // minionDeathTime（VarsGlobal +0x8c）—— 只给「hero 之后那笔」与「收尾那笔」
  const float EndSeqMinionStep   = 0.3f;   // sec3FractionDelay（BattleManager +0x4A8 = WaitForSeconds(DAT_1834b2dc8=0.3)）—— 逐单位
  ```
- `BuildEndBattleDestroySequence()`（`:36124`）：`t += EndSeqMinionWait;`（逐单位那一句，现读 **`:36149`**）→ **`t += EndSeqMinionStep;`**；
  hero 之后那一笔 `t += EndSeqWarlordWait + EndSeqMinionWait;`（现读 **`:36141`**）**保持 0.2**；
  收尾那一笔 `t += EndSeqMinionWait;`（现读 **`:36151`**）**保持 0.2**。
- **hero 的两个前置闸**（原版 `state0` 的四连判）：`GetHero(isPlayer) != null` ∧ `gameObject != null` ∧ `activeSelf` ∧ `GetMinionManager(isPlayer) != null`；
  我们只判了 `hero != null` ⇒ 补 `&& hero.Card != null` 不够（我们对`activeSelf`没概念：`UnitState` 是引擎对象）。
  ⇒ 如实折算成「引擎侧活着的单位才送」：`if (hero != null && hero.Card != null && !_endSeqDead.Contains(hero))`，并**在注释里写明**这是等价折算（原版看的是 `CardScript.gameObject.activeSelf`）。
- **`SetAsUnitDestroyed()`**：我们已有等价 `_endSeqDead`（`SyncBoard` 不再重建，见 `:26445`）⇒ **不另建**，注释里指回它。
- **`skipDeathSound`（`matchResult+0x24 == 2` Forfeit）**：我们**有判据**了 —— `Ctx.ForfeitedBy >= 0`（`BattleContext.cs:374`）就是 `BattleResult.Forfeit`。
  ⚠️ 但**今天无观察量**：`PlayDeathFeel`（`:25409`）本身**不播死亡音**，死亡音走事件那一拍的 `PlayBattleSfx` ⇒
  ⇒ 施工建议：**加一个开关字段** `bool _endSeqSkipDeathSfx`，在 `BuildEndBattleDestroySequence()` 里按 `Ctx.ForfeitedBy >= 0` 置位，
  `EndBattleDestroySequencePending()` 派发 `PlayDeathFeel` 时**顺手记一行日志**说明「原版这一档跳死亡音，我们这一层的音不在这个口 ⇒ 无差异」。
  ⛔ **别为此在 `PlayDeathFeel` 里加 `if (skip) return;`** —— 那个口本来就不播音，加了等于自造一条死支。

**④ 观测口**
- 批处理：由 B1 的 `EndSeqStepsForTest` 断言（④ 那条 `0.3f`）。
- 真 Play：打完一局 ⇒ **hero 先倒**、顿 0.7 s，再从**末格往回**逐单位倒（每个间隔约 0.3 s，肉眼可辨）；收尾 0.2 s 后才出面板。
- 现读：**零断言**（`Editor/` 全树 `grep EndSeq` = 0 命中）⇒ 本批是第一次给这条链建观测口。

**⑤ 风险**
- **改指纹**：否。**动手感**：**是**（逐单位间隔 0.2→0.3 ⇒ 整段收局长约 +0.3~1.2 s，取决于场上单位数）。
- 行数：只改已有行 + 新增 `const`（类尾或紧邻已有常量处；**紧邻处插入会漂移 ⇒ 追加类尾**）。

---

#### B3 · 账 **712** `BattleManager.GetUIState()`

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :712 | BattleManager | GetUIState() | 状态 B
undefined4 BattleManager__get_UIstate(longlong param_1) { return *(undefined4 *)(param_1 + 0x290); }
```
（`dump.cs:30834`：`private UIstateOptions <UIstate>k__BackingField; // 0x290`）

**② 原版判据**：`decomp_full/BattleManager__GetUIState.c` **全文 6 行**（上面的伪码就是全文）。
`UIstateOptions` 15 档原文 = `cpp2il/out_cpp2il_dev_ilspy/UIstateOptions.cs`。

**③ 改哪个符号、加在哪**
- **主问题**：账挂着的落点是 `bool BattleUiIsNormalBattle`（现读 **`:34198`**），而 `check_ledger_points` 判它「不像声明」
  （该行既无 `(` 也无 `{`；`工具/check_ledger_points.py:158-176` 的 `looks_like_decl`）。
  ⚠️ **`private` + `{` 拆到下一行 = 仍被判「不像声明」**（加 `private` 不改那三个字符判据）；
  而**把 `{` 挪上来会减 1 行 ⇒ 全下游账漂移**（`BattleDriver.cs` 是账引用最多的文件）。
- 🔑 **正解（推荐）：把落点换个符号，顺手消掉一份重复判据** ——
  本文件有**两份** `UIstate` 映射：
  · `BattleUiIsNormalBattle`（`:34198`，**只回答「是不是 normalBattle(1)」**）
  · `ChatHotkeyUiState()`（现读 **`:36016`**，**15 档全映射**，`A4` 为 ESC 热键建的）
  两者必须永远一致，否则就是 `CLAUDE.md` 那条「两处写同一条规则 = 迟早不一致」。
  ⇒ 施工：把 `BattleUiIsNormalBattle` 的 getter 改成 **`return ChatHotkeyUiState() == 1;`**（**同一行数，零漂移**），
  并把账的落点改指 **`int ChatHotkeyUiState()`（`:36016`，是真声明、机检得过）**。
  ⚠️ **改之前必须先核一处差异**：`BattleUiIsNormalBattle` **不判** `interaction.IsDragging`、
  而 `ChatHotkeyUiState` 把它判成 2（movingHandCard）⇒ 合并后「拖拽中 `BattleUiIsNormalBattle` 会变 false」。
  它今天的唯一消费点是**收灵魂石**那一路（`:33852` 附近，按符号 `BattleUiIsNormalBattle` 找）⇒ 拖拽中点灵魂石本来也不可能
  （`CardInteraction` 在拖拽时已经把棋盘点击吞掉）⇒ **行为等价**，但**必须在注释里写明这一档是有意合并的**。
- **若调度台不想动行为**：退而求其次 = 只把账的落点改指 `ChatHotkeyUiState()`（不动代码），并在备注里写
  「`BattleUiIsNormalBattle` 是它的子集，两份映射待合并（记为新账）」。

**④ 观测口**
- 批处理（`BattleScene.Run`）已有 ESC / 聊天那几条断言 ⇒ 覆盖 `ChatHotkeyUiState`。
- 新增一条**等价性断言**（`Editor/BattleScene.cs`）：把驱动摆成 6 个状态（normalBattle / 选目标 / 拖拽 / 换牌 / 面板开 / 已结束），
  逐态断 `BattleUiIsNormalBattle == (ChatHotkeyUiState() == 1)`（**结构上不可能两边一起改回去**）。

**⑤ 风险**
- **改指纹**：否。**动手感**：否（收灵魂石那一档的重叠态不可达）。
- 行数：**零漂移**（1 行换 1 行）。

---

#### B4 · 账 **741** `BattleManager.Update()` —— **只剩一条** C（教程「等玩家用主动技」别 FadeOut）

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :741 | BattleManager | Update() | 状态 C（18 段里 17 段已关，只剩 B08 教程那一支）
void BattleManager__Update(longlong param_1)      // decomp_full/BattleManager__Update.c:97-1413
```

**② 原版判据**
- 位置：`decomp_full/BattleManager__Update.c` **`case 1`（normalBattle）** 段，`CustomDebug__LogWarning(DAT_18427c2c8, 0)` 在 **`:183-185`**。
- 文案（字面量，逐字）：`"Avoid activeSkillDesc.FadeOut because we are waiting for that action in tutorial"`
  —— `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25696`（教程支）/ `:25711`（战役支，同一句）。
- 七条件（`out_il2csharp/Assembly-CSharp/BattleManager.cs:25680-25714` 逐句）：
  ① `case 1` ② `(targetCard == null || targetCard != cardSelected)` ③ `!waitingToResolveActiveAbilityFlag(+0x418)`
  ④ `IsPlayerTurn()` ⑤ `IsTutorialStageAvailable() || IsCampaignStageAvailable()` ⑥ `cardSelected != null`
  ⑦ `aiScriptedmanager.CheckIfWaitingToUseAbility(this, GetCurrentTurnScriptedData(), cardSelected)`
- 谓词：`decomp_full/AiScripted__CheckIfWaitingToUseAbility.c`；
  ⚠️ **`ScriptedActionType.ActiveAbility = 40`**（`dump.cs:42015` 现读）—— `判据/B89_R5b_APK挖PressedBtn.md` §二 写的「`ActiveAbility(50)`」是**笔误**，`R5c` 的 40 才对（`.c` 里是 `+0x10 == 0x28`）。

**③ 改哪个符号、加在哪**
- **前置已完成**：`RuleEngine/Core/TutorialScript.cs:1981` `TutorialRules.IsWaitingUseAbility(TutorialTurn, int actionCounter, BattleContext, string cardSelectedId)`
  已由 `R5c` 转写并 typecheck 过，**今天零调用**。⇒ 本笔**只需接消费者**（⛔ 别重写谓词）。
- 落点：`void TutorialFrameGuard()`（现读 **`:16731`**）——
  现读最后一句是 `if (!TutorialPermits(TutAttemptResolve(_command, _selectedSlot, -1))) ClearSelection();`
  ⇒ `ClearSelection()` 会一路走到 `skillPanel.Hide()`（`:31714`）/ `CancelAbilityTargeting()`（`:36458`），
  **那正是原版要拦下的那一下 FadeOut**。
  施工（**在 `ClearSelection()` 前**加一条早退）：
  ```
  // 七条件（原版 Update.c:183-185）；⑤ 战役支今天恒 false（Ctx.CampaignStage 无写点 = 账 709）
  if (_selectedSlot >= 0 && _command == AttackKind.Ability && Ctx.Active == _me && !Ctx.IsOver
      && Ctx.Tutorial != null && tut.CurrentTurn != null
      && TutorialRules.IsWaitingUseAbility(tut.CurrentTurn, tut.ActionCounter, Ctx,
             Ctx.Players[_me].Board[_selectedSlot]?.Card?.Id))
  {
      Debug.LogWarning("Avoid activeSkillDesc.FadeOut because we are waiting for that action in tutorial");
      return;                       // ⛔ 别 ClearSelection —— 原版这一档只跳 FadeOut
  }
  ```
  ⚠️ ② `targetCard != cardSelected`（悬停那张 != 选中那张）：我们悬停态在 `_hoverBoardCard`（现读 **`:34223`**，自检读口 `HoverBoardCardView` `:34227`）
  与 `_selectedSlot` 是**两个量** ⇒ 若要逐字照抄，条件② = `(_hoverBoardCard == null || BoardSlotOf(_hoverBoardCard) != _selectedSlot)`。
  ⚠️ ③ `waitingToResolveActiveAbilityFlag` 本仓**无同名 flag**（`R5c` §2 条件③ 已如实记「认领失败」）⇒ 建议用**默认放行**并在注释里写明（宁可按原版少拦一次，也别自造一个语义不明的闸）。
- ⛔ **别把日志塞进 `skillPanel.Hide()`**（`:31714` 是状态机切档，每档都会刷）。

**④ 观测口**
- 批处理（`RuleEngineTest.Run` 已有 5 条谓词用例矩阵，见 `R5c` §三；消费者接上后补宿主断言）：
  `Editor/BattleScene.cs` 摆一局教程 + 一条 `ActiveAbility(40)` 脚本 + 选中那张卡 ⇒ 断言 `skillPanel.Visible == true` 且**没有走 `ClearSelection`**（`_selectedSlot` 仍 ≥ 0、`_command` 仍是 `Ability`）。
- 反例（**灭自证**）：把脚本换成 `ChangeToMelee`（`IsWaitingUseAbility == false`）⇒ 断言**照常 `ClearSelection`** ——
  「两边一起改回去」不可能同时满足这两条。

**⑤ 风险**
- **改指纹**：否（`TutorialFrameGuard` 只在教程/战役局跑）。**动手感**：**是**（教程里主动技面板不再被收）。
- 行数：**插 6~8 行 ⇒ 会漂移**（`TutorialFrameGuard` 在 16731，**后面有大量账落点**）。
  ⇒ **做法**：不要在 `TutorialFrameGuard` 里插行，改成 **1 行调用**：
  `if (TutorialWaitingUseAbility()) return;`（新方法**追加类尾** `:36698` 之后），
  再在 `TutorialFrameGuard` 的 `ClearSelection()` 那一行**就地改写成** `if (!TutorialWaitingUseAbility()) ClearSelection();`
  （**1 行换 1 行，零漂移**）。

---

#### B5 · 账 **759** `CardScript.HighlightBloodThirst()`

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :759 | CardScript | HighlightBloodThirst() | 状态 C
void CardScript__HighlightBloodThirst(longlong param_1)
void BattleCardUI__HighlightBloodThirst(longlong param_1)
```

**② 原版判据**（**两份都读了全文，下面 5 句就是全部**）

`decomp_full/CardScript__HighlightBloodThirst.c`：
```
if (*(param_1 + 0x290) != 0) {                                  // +0x290 = BattleCardUI
    BattleCardUI__HighlightBloodThirst(*(param_1 + 0x290));
    BattleCardUI__DisplayTriggerAnim(*(param_1 + 0x290), 0xdc, 1, 0);   // ← 🔴 无条件、0xdc = 220
    return; }
throw;                                                          // 空 UI ⇒ 抛
```
`decomp_full/BattleCardUI__HighlightBloodThirst.c`：
```
if ((this + 0x268) == null) return;                            // bloodThirstFrame：**恒 null ⇒ 这一支无画面**
sr = (this + 0x268).GetComponentInChildren<SpriteRenderer>();
if (sr != null) DOFade(sr, DAT_1834b2dc8 /*0.3*/, DAT_1834b3178).SetLoops(6, Yoyo).SetEase(...);
```

🔴 **账里那句「原版这一支零画面」只对了一半**：`bloodThirstFrame(+0x268)` 恒 null 只让**淡入淡出那一句**不生效，
**紧随其后的 `DisplayTriggerAnim(220, 1, 0)` 是无条件的** —— 它走 `CardTraitCollection.GetTrait(220)`，
只要该 trait 有 `+0x28 ∈ {3, 6}` 的 effect 就会 `ResolveTraitAnim`（`decomp_full/BattleCardUI__DisplayTriggerAnim.c:30-110`）。
⇒ **不能照账里那句「把多出来的脉冲去掉」直接删** —— 删错了就变成「原版有、我们没有」。

**③ 改哪个符号、加在哪** —— **本笔第一步不是改代码，是补一条判据**（见 §3·③）：
1. 先查 `CardTraitCollection.GetTrait(220)` 在我们数据里有没有 `+0x28 ∈ {3,6}` 的 effect（数据侧 = 卡表 SO / 我们 `cards_truth.json` 的 trait 表）。
2. 若**有** ⇒ 原版**会播状态框** ⇒ 我们 `PlayTraitPulse`（`:24845`）里的 `tf.DisplayTrigger(kw, …)` **保留**，
   只需把 `v.PulseBadgeByKeyword(kw)` 那一半按「原版 `HighlightTraitIcon` 只在 `ActivateTriggerTraitAnim` 那条路上」核一次（**另一笔**，不在本行）。
3. 若**没有** ⇒ 原版这一支真无画面 ⇒ 把 `PlayTraitPulse` 里 `bloodthirst` 那一格的 `PulseBadgeByKeyword` 跳过（**不是整段删**）。

**④ 观测口**：`CardView.PulseBadgeByKeyword`（`:4084`）返回 bool ⇒ 加一个自检计数
`public int TraitPulseCountForTest`（类尾追加），断言「`EvtKind` 血渴触发 ⇒ 计数 +1 / 不 +1」。
宿主 = `Editor/BattleScene.cs` 的 trait 触发那一节。

**⑤ 风险**：改指纹否；动手感**是**（少一下或多一下可见脉冲）。**行数**：只改已有行 ⇒ 零漂移。

---

#### B6 · 账 **693** 的**另一半**：开大卡窗的 `UIstate ∉ {0,7,8,11,12}` 闸（**新账**，可选）

**① 原版全签名**
```
bool BattleManager__CanDisplayHandCard(longlong, longlong)   // 只喂「悬停放大」，已做（§1·A）
// 开大卡窗那条路：
void BattleManager__DisplayCard(...)   // 闸 = decomp_full/BattleManager__DisplayCard.c:14-18
```

**② 原版判据**
```
// CardScript__OnTouchUpAsButton.c:14-17 —— 只挡一个：if (UIstate == 0xe /*choosingResolutionCard*/) return;
// BattleManager__DisplayCard.c:14-18 —— 真正的开窗闸：
if (UIstate != 0 && UIstate - 7 > 1 && UIstate - 11 > 1) { …开窗… }
   ⇒ 只挡 {0 inactive, 7 finishBattleSequence, 8 postBattle, 11 mulligan, 12 mulliganFinished}
```

**③ 改哪个符号、加在哪**
- `void OnCardTapped(CardView card)`（现读 **`:13248`**）。现读四道闸：`CardDisplaySuppressedFor` / `SameFrame` / `TutorialAllowsCardDisplay`，
  **没有** `UIstate ∈ {0,7,8,11,12} ⇒ 不开` 那一档。
- 施工（**就地加 1~2 行**，用现成的 `ChatHotkeyUiState()` 当 `UIstate` 读口 —— **⛔ 别另算一份**）：
  ```
  int st = ChatHotkeyUiState();
  if (st == 0 || st == 7 || st == 8 || st == 11 || st == 12) return;
  ```
  ⚠️ 与本文件 `BattleUiIsNormalBattle` 的 doc 里那句「⛔ 别拿它去挡轻点开大卡窗」**不冲突** —— 那句反对的是拿 **`==1`** 去挡，
  原版挡的是 **5 个具体档**（选目标 9/10/13 与打法选择器 6 **照样能开窗**）。
  ⚠️ 顺序：放在 `SameFrame` 之后、`_cardDisplay.Toggle` 之前；`SetHint("")` 与 `_net.SendCardSeen` 保持在原位（原版那两句在闸之前）。

**④ 观测口**：`Editor/BattleScene.cs` 一条「终局/换牌/面板开着时点手牌 ⇒ `_cardDisplay.Visible == false`，选目标态点 ⇒ `true`」。
**⑤ 风险**：改指纹否；手感**是**（终局/换牌期不再能开大卡窗）。行数：**加 2 行会漂移** ⇒ 改成**一行**：
`if (CardWinUiStateBlocked()) return;`（新方法类尾追加）。

---

#### B7 · 账 **378** + **364**（`ChooseCardMenu.Setup` / `AutoProcessDone`）—— 回放局摆候选 + 2 s 自动选

**① 账行 + 原版全签名**
```
战斗界面_HUD.tsv :378 | ChooseCardMenu | Setup(List<RawCardScript> chooseCards, string actingCardId, int timerCounter, int target)
战斗界面_HUD.tsv :364 | ChooseCardMenu | AutoProcessDone()
```

**② 原版判据**：`cpp2il/out_cpp2il_dev_ilspy/ChooseCardMenu.cs:559-648` + `:30-62`（状态机 `MoveNext`）
```
// Setup :628-648
if (matchType == MatchType.Replay) {                              // Replay = 160 = 0xA0
    SelectCard(_chooseCardUis[0]._cardUI);                        // 自动选第一张
    if (!battleManager.isReplayPlayActive) StartCoroutine(AutoProcessDone());
}
if (timerCounter <= 1) StartCoroutine(EndSelectionWithDelay(timerCounter));   // 0.5 s 延迟

// AutoProcessDone d__21.MoveNext
state0: yield new WaitForSeconds(2f);      // ← 2.0 s
state1: this.ProcessChooseDone();
```

**③ 改哪个符号、加在哪**
- 我们侧：`ShowAsk()`（现读 **`:11177`**）/ `BeginAsk()`（现读 **`:11050`**）/ `CardChoicePanel`。
  **残差 = 录像里没有候选集** ⇒ 回放局根本不弹面板（`BeginAsk` 只有 `BeginPlay` 与技能 `DoResolve` 两个调用点）。
- 三段活：
  1. `Battle/ReplayStore.cs` `class ReplayRecord`（`:104`）：加 `public string[][] chooseCandidates;`（逐次选择的候选 `CardDef.Id` 列表；
     **纯新增字段 ⇒ `version` 不 +1**，同 `startFullHash` / `traceFull` 的先例，`ReplayStore.cs:150-176` 有完整论证可照抄）。
  2. **写侧**：`BattleDriver.ShowAsk()`（`:11177`）在摆面板那一刻把「这一次的候选 id 列表」追加进 `_rec.chooseCandidates`。
  3. **读侧**：回放路径（`ApplyReplayActionAt` / `RankedReplay` 那条）遇到 choose 动作 ⇒ 用候选集 `_choosePanel.Open(...)` 摆出面板，
     并用**显式秒数 Tick**（批处理没有协程）在 **2.0 s** 后自动选第一张 ⇒ `ProcessChooseDone` 的等价物。
- ⚠️ 标题回落链（`Instructions-<id>` → `Instructions`）**已经在** `ChoosePanel.ResolveTitle` ⇒ 接线时**别挪走**。
- ⚠️ `isEnviromental`：原版 `actingCardId == "offensive"/"defensive"` 时 `chooseMenuAnchor.SetActive(false)` ——
  我们**没有** `ChooseCardMenuAnchor` 节点（卡片行直接挂面板下）⇒ 那一句**无对应物**，注释里写明。

**④ 观测口**：`ReplayStore` 自检（写一条含 choose 的录像 ⇒ 读回来 `chooseCandidates.Length == 动作数`）+ `BattleScene.Run` 里
「回放这条录像 ⇒ 面板出现过（计数 ≥1）且 2 s 后自动关」。
**⑤ 风险**：改指纹否（录像结构变，`version` 不动）；手感否（只在回放局）。
**行数**：`BattleDriver` 侧只加调用 + 类尾新方法；`ReplayStore` 早已有新增字段的先例。

---

### 1·C —— 判据不全 / 卡在别处（**别在本波硬做**）

| 账行 | 原版方法 | 卡在哪 | 还差什么（可照做） |
|---:|---|---|---|
| **684** | `ActiveWarlordAbility.OnTouchUpAsButton(Vector2)` | ①②③**已 A**（`WarlordTryUseActiveAbility` 现读 `:35077`）；**④ `PressedBtn` 建层** | 🔴 `判据/B89_R5b_APK挖PressedBtn.md:52` 已判 **D**（四级源全负：ILSpy/`.c` + `assets_full` + AssetRipper + 安卓 1.35.0 APK 按脚本/GUID/类名反查 **全 0**）⇒ **等调度台裁定 D**（铁律 11 例外①）。⛔ 别自己挑图建件；`CardHighlight.PressedBtnSetActive`（`:989`）钩子**保留不动** |
| **20** | `ActiveSkillDisplay.SetupActiveSkill(CardScript)` | 后缀闸 + 三档描述链**都已做**（`ShowSkillPanel:15723` 已喂 `maxTargets`/`firstTargetName`）；残差**全在数据侧** | ① `RuleEngine/Core/CardDef.cs` + `CardDatabase` 补 **ability 级** `targetsAvailableCounter`（现读 `cards_truth.json` 1,351 张 **0 条**）② `currentAbilityCost`（`SkillPanel:1564` 的 `costText` 恒 `"0"`）③ `activeAbilityTextReferenceId`（原版 `+0x248`）④ 官方表 `Card_Abilities/Ability_Name_<id>` 现读 **0 条** ⇒ 今天回落卡名**恰好等价**。⇒ **归引擎线/卡表线**，⛔ 别在 BattleDriver 里编 |
| **709** | `BattleManager.GetCurrentTutorialStage()` | 教程支已对（`Ctx.Tutorial`）；**战役支恒 null** | `Ctx.CampaignStage`（`BattleContext.cs:922`）**零写点**（`RuleCore.cs:1220/2799` 自承）。原版 `{100,140} ⇒ currentTutorialStage`、`{130,190} ⇒ currentCampaignStage`、其余 null。⇒ 要等**战役局入口**把 `TutorialStageData` 灌进去（战役线）|
| **537** | `ManaManager.SetManaSaved(int,int,int,bool)` | 桥已接（`SyncSavedManaToHud` 现读 `:36201`） | 残差 = `unUsedMana` 的「清零前余额」；`EndTurnCore` 写完 `SetSavedMana` 立刻 `p.Energy = 0` ⇒ 只能传 `max(carry, energy)` 并在 `EnergyShownOf` 夹 0（已标 `🔶 自己设计`）。**要么**在 `PlayerState` 加一格「上一手的 unused」（引擎线），**要么**接受现状并让调度台把账记成「半格 + 已标注自设计」 |
| **802** | `RemnantBody.DoDestroyByAttack()` | 落点 `Core/CardFeel.cs`（**不是 BattleDriver**），三件已对 | 残差只有一个量：原版 `GetDeathParticlePosition` **只填 x/z、y 恒 0**；我们取整只 `root.transform.position`。⇒ 断言 + 改成 `new Vector3(p.x, 0f, p.z)`，归 CardFeel 写手 |
| **306 / 364** | `CardDisplayWindow.SetupTutorialObj` / `ChooseCardMenu.AutoProcessDone` | 306：闸的数据源 = LiveOps `TutorialEvent`（随远端下发）⇒ 建出来**恒不可达**；**维持「不建」**（`B2AU`/`W44HUD`/`B87-A7` 三次一致裁定）。364 见 B7 | 306 的接口点已在 `Battle/TutorialOverlay.cs:1082` `SetAnnotations`；缺的两件（按卡类型/费用点亮哪几块 · 父链归位）**归教程线** |

---

## §2 建议的施工顺序（给写手的排程）

> **一个文件同一时刻只有一个写手** ⇒ 下面是**同一个写手**做完一条接着下一条的顺序，不是并行。

| 序 | 条目 | 为什么这个位置 | 依赖 |
|---:|---|---|---|
| 1 | **B2（704）的 0.3 s 常量拆分** | 只改 3 个字符 + 1 条新常量，**最省的一笔**，且 B1 会复用 | 无 |
| 2 | **B1（689）侧序 + 胜方 + 两个 SetActive** | 与 1 同一函数、同一批时间表；**一次改完、一次验** | 依赖 1 的 `EndSeqMinionStep` |
| 3 | **B1/B2 的自检口 + 四条断言**（`EndSeqStepsForTest`） | 上面两条改的是**批处理看不见**的真 Play 行为 ⇒ 必须先用**时间表断言**兜住 | 1、2 |
| 4 | **B4（741）** | 有现成施工指令（`R5c` §2）+ 谓词已就位；1 行换 1 行、零漂移 | 无 |
| 5 | **B3（712）** | 1 行换 1 行 + 账换落点符号；**顺手消掉两份 UIstate 映射** | 无（但**必须**先核 `interaction.IsDragging` 那一档） |
| 6 | **B6（693 大卡窗闸）** | 2 行 → 收敛成 1 行调用；与本批无冲突 | 与 B3 共用 `ChatHotkeyUiState()` ⇒ **排在 B3 之后** |
| 7 | **B5（759）** | **先查数据（`GetTrait(220)` 有没有 type 3/6 的 effect）再决定改不改** ⇒ 可能变成零改动 | 数据核查 |
| 8 | **B7（378+364）** | **最大的一件**（录像结构 + 回放路径），放最后 | `ReplayStore`（另一个文件，需单独占权） |

**能一次做完的**：1+2+3（同一函数族）、4+5+6（都是「读 `ChatHotkeyUiState` 做闸」的小件）。
**有依赖的**：3 依赖 1、2；6 依赖 5；其余互不依赖。

---

## §3 判据不全 / 查不到的（⛔ 别硬编）

① **`BattleManager +0x4A8` 的构造实参** —— 字段名 `sec3FractionDelay`（`dump.cs:30891`）+
   `BattleManager__.ctor.c:206-209` 用 `DAT_1834b2dc8` 构造 +
   `工具/read_literal.py` 文件头读数表 **`0x1834b2dc8 = 0.3`**。三条互证 ⇒ **判 0.3**。
   ⚠️ 但那张表对 `0x1834b2dc8` 的旧注是「`CardScript.DoPushBack` 的 elasticity」—— **同一个地址被两处用是正常的**（float 常量池），
   **不是「读错了」**。若写手要用更硬的证据：`工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2dc8`，读 4 字节 float，应得 `0.3`。

② **`CardTraitCollection.GetTrait(220)`（血渴）在我们数据里有没有 `+0x28 ∈ {3,6}` 的 effect** —— **查不到**（本轮没查数据侧 trait 表）。
   ⇒ 这是 B5 的**前置**：`trait 220` 的 effect 表若为空，`DisplayTriggerAnim(220)` 就是空转（我们的脉冲才是多余）；
   若非空，**原版也播状态框**（账里「原版零画面」那句就是错的，得就地订正）。
   差什么：`d:/2/新解包资源/assets_full/` 里的 trait 表资产（`CardTraitCollection` 的 SO）或我们 `cards_truth.json` 的 trait 段。

③ **`BattleHudHideNoHand` 之外还有没有别的「收 HUD」动作会带走 `_chatBtn` / `_cemeteryBtn`** ——
   现读证据只有 `Resources/battlehud_clips.json` 的 7 条 `curves[]`（不含这两件）。
   ⚠️ 但 `BattleHud` 原版的 `ToggleWithAnimation` 是不是**另外**还 `SetActive(false)` 整棵组节点，本轮**没查**
   （`decomp_full/BattleHud__ToggleWithAnimation.c` 未读）。
   ⇒ 写手做 B1 的那两条 `SetActive` 之前，**先读那份 `.c` 一眼**：若原版 `ToggleWithAnimation` 已经把这两件带走，就不用重复设。

④ **`EnemyHandCardClickable` / `761`** —— 非本域（`Core/CardHighlight.cs`），本轮**没重查**，见 `报告/B89_E5e_敌手牌可点链路.md`。

---

## §4 顺手发现（不属本批，但该记）

1. 🔴 **账 `689` 里那句「那两个值来自服务端 `RankedMatchResult`，本机拿不到同一语义 ⇒ 两侧都摧」已过期**（`BattleDriver.cs:36106-36110` 的注释同句也过期）：
   `Ctx.Winner`（`BattleContext.cs:371`：1/2 = 该方胜、3 = 平）与 `Ctx.ForfeitedBy`（`:374`）**完全够用**
   ⇒ `battleWinner` / `battleResult` 两档**都能精确映射**。⛔ 别再拿「服务端枚举」当不做胜方判据的理由（铁律 5：就地订正那两处注释）。
2. 🔴 **`689`/`704` 的「我方先摧」与「逐单位 0.2」两处都是误读**（本文件 §1·B·B1/B2 已给出逐句反证）：
   - 原版第一次 `DestroyUnitsAfterBattleEnd(this, isPlayer:0)` = **敌方**（`GetHero(false)` = 敌方英雄），我们写成 `_me`；
   - 原版逐单位之间是 `sec3FractionDelay = 0.3`，不是外层那个 `(n+1)×minionDeathTime(0.2)`。
   ⇒ 这两条也说明**「账的差异列写的东西也要回源核」**：那条备注写得很有依据（有常量、有算式），但**侧序与间隔都是反的**。
3. **`BattleDriver.cs` 里有两份 `UIstate` 映射**（`BattleUiIsNormalBattle` `:34198` / `ChatHotkeyUiState` `:36016`）——
   违反「两处写同一条规则 = 迟早不一致」。建议合并（见 B3），并在合并后把 `BattleUiIsNormalBattle` 归零成一行转发。
4. **`BattleDriver.cs` 的类闭合在 `:36699`（不是 `:36700`）** —— `:36700` 是 **namespace** 的 `}`。
   简报与账里都写「追加在 `:36700` 之后」⇒ 照做会把新成员加到 **namespace 外面**（`CS1519`）。
   > ⚠️ **2026-10-28 更正（执行写手 `B91_W3` · 铁律 5）**：这对数字是 **2026-10-27 的快照，已过期**。
   > **现读 = 类闭合 `:37164` / namespace `:37165` / 全文 `37,165` 行**（`crlf 37165 / LF 37165 / bareCR 0`）。
   > **错因**：`B90_*` / `B91_*` 批次在类尾追加过成员 ⇒ 行号整体下移；**这类「绝对行号」的判据天生会漂**。
   > 🔑 **改成不会漂的写法**：**读文件末尾，倒数第 2 个 `    }`（缩进 4 空格）是类闭合，倒数第 1 个顶格 `}` 是 namespace**，新成员追加在类闭合之前。
   > ✅ 这条纪律本身（别加到 namespace 外面）**仍然成立**，只是不许再靠固定行号去定位。
   ⇒ 写手一律**按符号找类尾**，⛔ 别按行号。
5. **`R5b` 与 `R5c` 对 `ScriptedActionType.ActiveAbility` 的值有分歧**：`R5b` 写 50、`R5c` 写 40。
   `dump.cs:42015` 现读 = **40**；`AiScripted__CheckIfWaitingToUseAbility.c` 里的判据是 `+0x10 == 0x28`（= 40）⇒ **`R5c` 对**。
   `判据/B89_R5b_APK挖PressedBtn.md` §二 那一处「`ActiveAbility(50)`」应就地订正。
6. **账 `693` 的落点指错了对象**：原版 `CanDisplayHandCard` 只喂**悬停放大**（`CardScript__OnTouchEnter.c:28-31` → `ShowCardInHand`），
   **不是**开大卡窗（那是 `BattleManager__DisplayCard`，闸是 `{0,7,8,11,12}`）。
   ⇒ 该行的「差异」那句「OnCardTapped 只过三道闸」是**把两条判据混成一条**；建议拆成 693（悬停，已 A）+ 一条**新账**（大卡窗闸，见 B6）。
7. **`BattleDriver.cs` 里若干账落点行号已经漂了 100+ 行**（`SetSavedMana` 账 33908 / 现读 34028；`ShowEndPanelAndFreezeInput` 账 32107 / 现读 32227）。
   ⇒ 下一任写手**按符号名重定位**；调度台在同步点跑一次 `check_ledger_points --all-A --fix`（**只能在「没有 `.cs` 写手 + 补丁已合并完」时跑**，见 `CLAUDE.md`）。
8. **`BattleScene.cs` 里 `W67CenterAssert` 的两条断言（账 756）从未跑过**（`W67CENTER` 那一波禁 Unity）⇒
   账 756 想升 A，得先在同步点把 `BattleScene.Run` 跑一遍；在那之前**别提前升**。
