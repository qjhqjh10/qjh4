# T06b · 剩下两条 cue 的触发点（2026-10-15 · 只读普查）

## 结论（直答两问）

- **① `unitDamage` 谁调**：不是「多目标专属」。原版 `DealMultiDamage` **一次调用播一条**，且**只在该批的【最后一个目标】**那一下播
  （`_ResolveMultiDamage_d__456__MoveNext.c:144-149` · `_ResolveMultiDamageAndDestroySelf_d__457__MoveNext.c:158-163`，
  判据同为 `list.Count-1 <= index`）。调用方 = **6 条**（下面 §一·一）。我们这边**能落地**：
  `EvtKind.Hit` 且 `Amount>0` 且**不是攻击命中** ⇒ 播 `unitDamage`（判据见 §一·三）。
- **② `sneakAttack` 什么算**：**攻击者当前身上带 `DefinedTrait.sneakAttack`(20) 的一次攻击**。
  与你看到的「远程」无关 —— 那是 `else` 的**上一级**判据，见 §二。
  🔴 但**两边卡池都没有这张牌**（我们 0 / 原版 1195 张快照 0）⇒ **本 build 的卡池上没有触发物**。

---

## 一、unitDamage

### 调用链（逐跳 `文件:行号`）

**为什么 `decomp_full` 里 0 调用点（根因，不是「查不到」）**：
`ResolveMultiDamage(BattleAction)` 被 MSVC **内联进 `BattleManager.ResolveAction` 的 `case` 体**了。
- `ResolveAction` = **一张 91 项跳转表**的开关：`BattleManager__ResolveAction.c` 的入口 VA `0x1809747B0`，
  表基址 **`0x18097ADFC`**（`0x180974F3A` 的 `mov ecx,[rdx+rax*4+0x97adfc]` 定出），**索引 = `BattleActionType - 1`**
  （`:180974F26` `dec eax` / `:180974F28` `cmp eax,0x5a`）。
- `BattleActionType.doMultiDamage = 9`（`dump.cs:45319`）⇒ 索引 8 ⇒ case 体 **`0x180975242`**；
  该段是**逐指令同形的 `ResolveMultiDamage` 工厂**（分配 → `[obj+0x120]=this` → 拷 0x100 字节 `BattleAction` 到 `+0x20`），
  与 `BattleManager__ResolveMultiDamage.c:27-77` 一致 ⇒ **工厂被内联 ⇒ 没有 `call`**。
- 旁证：`BattleManager` 的 63 个「0 调用点」`Resolve*` 里，**`ResolveDrawCard` / `ResolveHeal` / `ResolvePlayCardFromHand` 也在内**
  —— 那几个显然在用 ⇒ **「0 调用点」在这条线上不是判据说**。脚本留在 `d:/4/_tmp_view/t06b_scan/`（`dllscan_callers.py` · `read_switch_table.py`）。

**调用方（DLL 字节扫描 `call rel32`，`d:/2/unity_run_ref/GameAssembly.dll`）**：

| 入队方（方法名） | 调用点 VA | 情形 |
|---|---|---|
| `AbilityLogic.PlayAbility` | `0x180821399` | **`AbilityEffect.doDamage(10)` / `doDamageFast(13)`** ⇒ 卡牌技能伤害（`AbilityLogic__PlayAbility.c:1504`） |
| 同上（另一处） | `0x18082120c` | **`doDamageMultiRandom(12)`** → `DealMultiDamageVariable`（`.c:1623`） |
| 同上（另一处） | `0x180821502` | **`doDamageAndDestroySelf(15)`** → `DealMultiDamageAndDestroySelf`（`.c:1671`） |
| `CardScript.TriggerOnMinionDeath` | `0x180600075` | **带猎杀标记（Hunt Mark）的单位阵亡** → 对敌方督军造成 `CurrentHuntMark` 点伤害 + 给自己回同样多（`CardScript__TriggerOnMinionDeath.c:39-93`） |
| `CardScript.<ReceiveDamage>d__381.MoveNext` | `0x1806147f1` | **受击者自己带 `Vulnerable`**（且不带 dodge/shield/invulnerable、本次不是 Vulnerable 类伤害）⇒ 自我结算一次 = `Vulnerable` 点的伤害（`.c:188-254`） |
| `BattleManager.<ResolveFatigue>d__587.MoveNext` | `0x1809b3b35` | **牌库抽空 → 疲劳伤害**（打督军，`DamageType.Undersupplied=2`，`.c:130-141`） |

**普通攻击不在此列**：`_ResolveAttack` 走 `BattleManager.ResolveAttackDamage`（`BattleManager__ResolveAttack_d__438__MoveNext.c:865`），
它的两个调用点**都在 `_ResolveAttack` 里**（`0x1809ae78f` / `0x1809af293`），**不调 `DealMultiDamage`**
⇒ 攻击命中是**另一条音**：`BattleCardSounds.meleeAttackHitSmall/Medium/Big`（`CardScript._AttackMeleeAnim_d__357__MoveNext.c:83`）。

### 什么游戏情形（哪几条卡 / 哪种效果）

1. **任何 `AbilityEffect.doDamage / doDamageFast / doDamageMultiRandom / doDamageAndDestroySelf` 的技能结算** ——
   卡面 `Damage N …` 那一族（含 1 个目标）。`doDamageAndDestroySelf` = 打完自己没（卡面「…then destroy this unit」）。
2. **猎杀标记单位阵亡**（督军卡上的 `huntMark` 那套）。
3. **Vulnerable 易伤自结算**。
4. **疲劳**（牌库空时抽牌）。

### 我们这边怎么判（可落地）

- **播点判据**：`EvtKind.Hit` 且 `Amount > 0`（`Amount<0` = 治疗，见 `BattleEvent.cs:255-258`）**且不是攻击命中**。
- **「是不是攻击命中」的判据**：这次 `Hit` 的**同源上一条事件是 `EvtKind.Attack`**
  —— `RuleCore.DeclareAttack` 在 `Core/RuleCore.cs:4385` 发 `Attack`（注释原话「在伤害之前发」），
  紧随其后由 `ApplyDamage`（`Core/RuleCore.cs:6163-6179`）发 `Hit`。其余 `Hit` 走效果/疲劳/易伤，前面没有 `Attack`。
- 🔴 **但直接挂会「多播」，必须收口**：
  · 原版**一批只播 1 条**（最后一个目标）；我们 `ApplyDamage` **逐目标**发 `Hit`。
  · `EventTiming.DurationOf(Hit)` = `CardFeel.HitRotDuration 0.5 + ResetDuration 0.25` = **0.75 s**
    （`Core/CardFeel.cs:136,141`）⇒ 时间线上两条 `Hit` 相隔 0.75 s，**cue 的 `timeToPlayAgain = 0.1 s` 节流挡不住**。
  · **收口建议（留给调度台裁）**：在 `BattleDriver.PlaySignals` 排时间线时，让**同一批里的最后一条 `Hit`** 才播
    —— 与 `drawFromEnd`（`BattleDriver.cs:11278-11291` 已在算「这一段里倒数第几张」）**同形**。
  · ⚠️ **没查清**：「同一批」在我们引擎侧怎么判 —— `_signalBuf` 是一次 `DrainSignals`（可能跨整个回合），
    「连续 `Hit`」还缺一条判据（例如两条 `Hit` 之间不得夹别的**源**事件）。**这一条我没做，也没改任何代码。**

---

## 二、sneakAttack

### `_ResolveAttack`（`d__438`）状态机 → 语义表

`state = *(param_1+0x10)`；`case` 标签在 `BattleManager._ResolveAttack_d__438__MoveNext.c:83/493/524/1373/1376/1394/1397/1433/1449/1489/1508/1519/1537/1550/1559/1572/1581/1594/1603/1623/1626/1630`。

| 状态 | 语义（读了哪一段） |
|---|---|
| `0` | 开局准备（清双方 pending damage / 记 `RecordAttackPendingDamage`）→ 落 2 或 8（`:83-492`） |
| `1` `2` | 主体：等到伤害前那一刻（`:493-1372`） |
| `3`–`6` | 中途分支的接头（`:1373-1393`，各自立即 return/继续） |
| **`7`** | 「**反击 / 受击路**」等一拍之后：`ChallengeLogMgr.LogDamage` + `RawCardScript.OnTrigger(0x82,…)`（`:1433-1448`；进入点 = `:1421` 的 `ReceiveDamage(attacker, 0x14, …)`） |
| `8` | 目标高亮 + `BattleManager.EnemyTargetingAnim` + `WaitForSeconds(GetTargettingTime)` → 9（`:1449-1488`） |
| `9` | → `LAB_1809ae6f3`（`:854`）= **结算攻击伤害 + 挑动画**（`:1489-1507`） |
| **`10`** | 🔴 **偷袭**：`CardScript.ReceiveSneakAttackAnim(target, attacker)` → 0xb（`:1508-1518`） |
| `0xb` `0xe` `0x11` `0x14` | 共同收尾（等 `battleManager+0x4b8`）→ `LAB_1809aed47` 广播 `BroadcastUnitAttacked` + 应用 `ResolveAttackDamage` 的结果（`:1519-1536` · `:987-999`） |
| `0xc` | **恐惧**（`ActivatesTerror`）之后：`ResolveRangedAttack` → 0xd（`:1537-1549`；进入点 `:948-962`，此处的 cue 是 `activateTerror`） |
| `0xd` | 等一拍 → 0xe（`:1550`） |
| `0xf` | **`ActivatesNoReturnLongRangeAttack`** 之后：`ResolveRangedAttack` → 0x10（`:1559`；进入点 `:920-931`，粒子 `0x3de`） |
| `0x10` | 等一拍 → 0x11（`:1572`） |
| `0x12` | **`ActivatesNoReturnSniperAttack`** 之后：`ResolveRangedAttack` → 0x13（`:1581`；进入点 `:934-944`，粒子 `0x488`） |
| `0x13` | 等一拍 → 0x14（`:1594`） |
| `0x15` `0x16` | **普通远程 / 近战攻击动画**演完 → 收尾（`:1603-1622`） |
| `0x17` `0x18` `0x19` `0x1a` | 另外四条动画路（`:1623-1642`；`0x1a` 收尾里有 `SetRemoveMarkerlight`） | 
| `default` | `return 0`（`:1645`） |

### 结论 + 证据

🔴 **那个「矛盾」是读错了上一级判据**：`state 10` 所在的 `else` **不是「远程」的 `else`**，而是
**`HasCurrentTrait(sneakAttack)` 的 `else`**：

```
:871  cVar11 = EntityScript__HasCurrentTrait(attacker /*param_1+0x28*/, 0x14);   // 0x14 = 20 = DefinedTrait.sneakAttack
:872  if (cVar11 == '\0') { ...同一条 if 里嵌了第二层 if → 近战/远程动画... }
:967  else { ...Col? —:970 ResolveRangedAttack(attacker, target) → StartCoroutine → state = 10 }
```
- 第二层（`:891`）：`if (*(int *)(param_1 + 0x58) != 2)` → `AttackMeleeAnim` → state `0x16`；
  `== 2`（`AttackTypes.Ranged = 2`，`dump.cs` `AttackTypes`）→ `ResolveAttackRangedAnim` → state `0x15`。
  ⇒ **「近战还是远程」在【无偷袭】那一支里判**，与偷袭无关。
- **偷袭那一支走的是 `ResolveRangedAttack`**（`:970`）—— 所以「看到 `ResolveRangedAttack`」**不是**偷袭判据：
  state `0xc` / `0xf` / `0x12`（恐惧 / 两种 NoReturn）**也**走它。**唯一**调 `ReceiveSneakAttackAnim` 的是 state 10。

**结论**：**「什么算一次 sneak attack」= 发起攻击的那张卡【当前】带 `sneakAttack` trait（`EntityScript.HasCurrentTrait(…, 20)`）**。
不需要别的字段/分支 —— 判据就在 `:871` 那一个 `if`。原版对它做的事 = **强制用「远程攻击动画」** + 在**被打者**身上
`BattleCardUI.DisplayTriggerAnim(20, 1)` + 播 `SoundAssetCollection.SneakAttack`（`CardScript__ReceiveSneakAttackAnim.c:14-37`，
**该方法体内无条件** —— 它只有 state 10 一个调用点）。
（`param_1+0x13a/0x139` 两个门 `:863` 与 `0x58==2` 那类都在这条判据**之前**，不改变结论。）

### 卡池复核（我们 / 原版 各有没有 sneakAttack trait）

**我们：0 张。** 独立复核（不只是 grep 一次）：
- `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（现池 **1127** 张）：`cardTraits` 60 个取值 · `keywords` 60 个取值，
  **都不含** `Sneak Attack` / `sneak`。最接近的是 `Stealth`（29 张）· `Long Range`（11）· `Sniper`（9）。
- `Unity/数据/游戏数据/cards.json`（Godot 时代旧表）：唯一命中是语音名 `…get all sneakylike.ogg`（`Kommando`）—— **与 trait 无关**，
  你的结论**成立**。
- `Unity/数据/` 全树 `sneak` 只剩三处：上述语音 · 预组卡组名 `Sneaky Gitz`（Ork Goff）· 音效 clip 名 `Sneak Attack`
  （`索引/enum_names.json`）。

**原版：也是 0 张。** 搜过的东西：
- **正本 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`**（38.6 MB · **1195 张原版卡 SO** · 2025-07-30 快照；
  我们 `cards_engine.json` 的 `cardTraits` 就是它来的）：
  · 全文 `sneak`（不分大小写）**0 次**；
  · `CardTraits` / `_trait` 值域里 **没有 20**；`_trait == 20` 的卡 **0 张**；
  · 判据/筛选用途的 `trait` / `traitsFilter` 值域里**也没有 20**（`_traitValue` 同样没有）。
  · 对照（证明这份文件**搜得动**）：`stealth` 40 次 · `ephemeral` 106 · `rally=10` 92 张 · `fast=40` 32 次 ⇒ 低编号 trait 是在用的。
- **词条是存在的**：`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 有 `Card_Trait/sneak` = **"Sneak Attack" / "潜行攻击"**
  （77 条 `Card_Trait/*` 之一）—— 但**没有任何卡引用它**（孤立词条）。
- 原版 **TMP trait 图标字模** `Unity/数据/游戏数据/trait_textsprites.json`
  （源 = `assets_full/bundle_fonts_assets_all/MonoBehaviour/Warpforge Trait TextSprites.json`）：`sneak` **0 次**。
- **PnP 成品卡图那一套**（`d:/2/Warpforge部队卡片/`）：4 份卡表 md + 2 份规则书 + `_tmp_rulebook_terms.json`，`sneak` **全 0**
  （规则书里只有 `Stealth` = 「潜行」，是**另一个** trait=70）。

⇒ **两边都没有 ⇒ 这条 cue 在我们这个卡池上【没有触发物】，⛔ 别硬接**（照它接也永远不会响）。
⚠️ 但**触发点判据是有的**（不是「判不出来」）—— 将来加进带这个 trait 的卡就能直接接线：**攻击者带 `sneakAttack` ⇒ 在被打者身上播**。
⚠️ **还没查清**：远端 catalog / 关服后新版本里有没有这张牌 —— 本地判据到此为止。

---

## 三、没查清的 / 判不出

1. **`param_1 + 0x138`**（偷袭/恐惧/NoReturn 三条支里都被清 0）的字段名 —— `dump.cs` 没对出 `_ResolveAttack` 状态机的字段表，**没查到**。
2. **`FUN_1805e39d0(0)`**（传给 `SoundManager.Play2D` 的第 2 实参）是什么 —— 只确认它**在 6 处播 cue 时都出现**，
   没去 `script.json` 反查类名。**没查到**。
3. **引擎侧「同一批伤害」的判据** —— 见 §一·三最后的 ⚠️。**判不出来**（今天没有这个位）。
4. 搜过但**无果**的词/位置：`sneak` / `Sneak` / `sneaky` / `sneakylike` / `Sneak Attack` / `潜行攻击`；
   trait 数值 `20` 出现在 `_trait` / `trait` / `traitValue` / `traitsFilter` 里；
   目录 = 上面 §二 列的那 5 处。

## 四、顺手发现（⛔ 我一个字没改，报给调度台）

1. 🔴 **「0 调用点」在这条线上多半是【内联】，不是「代码被剥掉」** —— 根因与脚本见 §一。
   这条**给以后判据**用：`BattleEvent.cs:175-177`（`0x31 → ResolveRevealOrders` 是「从动作类型推的，不是读出来的」）
   现在能用同一张表**读出来**（表 idx 48 = type 49 = `revealOrders` → case 体 `0x1809776AC`，同形的内联工厂）。
   ⚠️ 我**没**去逐指令核那个 case 体是不是 `ResolveRevealOrders`（那属另一件活）。
2. **`ResolveAction` 的开关表是 91 项、索引 = `BattleActionType − 1`**（`0 = undefined` 落 `default`）。
   表基址 `0x18097ADFC` · case 体范围大致 `0x180974F46`–`0x18097AD3E`。
   ⇒ **以后查「谁处理 `BattleActionType.X`」不用再猜**：读 `_tmp_view/t06b_scan/read_switch_table.py`。
3. 🔴 **`unitDamage` 播在【最后一个目标】上**（`count-1 <= index`），**不是第一个** —— 两处 MoveNext 判据一致，
   与直觉相反，接线时别想当然。
4. **cue 的 0.1 s 节流挡不住我们的多目标多播**（`DurationOf(Hit)=0.75 s`），见 §一·三。
5. `_ResolveAttack` 里**另有 3 条**走 `ResolveRangedAttack` 的路（`0xc` 恐惧 / `0xf` NoReturnLongRange / `0x12` NoReturnSniper）
   —— 别拿「走了 `ResolveRangedAttack`」当偷袭判据。
