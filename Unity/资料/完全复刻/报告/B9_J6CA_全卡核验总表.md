# B9 · 全卡端到端核验 —— S1「建总表」（代号 J6CA · 2026-10-26）

> **规格** = `判据/B9_全卡端到端核验_规格.md` §三 S1。本报告只覆盖 **S1（只 join、不判定）**：
> 一行 = 一张卡，列出 ① 技能树 ② 声明的 VFX ③ 声明的 anim + 时序 ④ 语音 cue ⑤ **这些引用在工程里是否都存在** ⑥ 账档位分布。
> ⛔ 本阶段**不给** ✅/⚠️/❌/？ 四维判定（那是 S2/S3）；本报告里所有「✓/✗」只表示**引用在工程里找得到 / 找不到**，**不代表效果生效**。
> 产物 = `判据/全卡核验总表.tsv`（22 列 × **1,128 行**，983 KB）＋ 本报告。

---

## ① 结论

### 1.1 分母口径（四个数字都不冲突，⛔ 别互相校验）

| 口径 | 现读 | 说明 |
|---|---|---|
| **本表分母 = 池内卡** | **1,128** | `MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 `count`（本表 1,128 行逐一对应） |
| `cards_truth` 全量 SO | **1,351** | 其中 **1,128** 在池内（交集 = 1,128，**无一条池内卡缺 truth**）；其余 **223** 是池外 SO（代币/战役专用等） |
| `card_ids.json` 的 `poolTrueIds` | **1,127** | 🔴 这是**历史迁移表**（键 = **旧池 id**、值 = 真 id；其中 **114 条**已重编号 ⇒ `counts.poolSameId` 1,013 / `poolIdFix` 114）。**它的【值域】覆盖 1,127/1,128 张现池卡**，缺的恰好是后补的 `Pray Enchantment`（§⑤-4）⇒ **不是「缺一张卡」**，⛔ 别拿 `len(poolTrueIds)` 当现池分母 |
| `ruleprobe check` 基线 | 1,121 | 引擎对拍口径，本表未使用（S3 才用） |

### 1.2 一句话结论

**池内 1,128 张卡的四维声明引用共 6,120 条（VFX 2,755 · anim 1,468 · 语音 1,897），
除 24 格「Wait_* 计时拍」（原版就是无 prefab 的纯延迟）外，其余 6,096 条 100% 在工程里找得到。**
「工程里找不到」的**卡片级缺口 = 0**；发现的**唯一一处数据源缺口**是 `cards_engine.animTimes`
少了 **1 条** `alternativeAbility` 的动画引用（SW21，见 §⑤-1）。

### 1.3 覆盖率（有/无声明）

| 维度 | 声明总条数 | 有声明的卡 | **声明为 0 的卡** | 判读 |
|---|---|---|---|---|
| ① 引擎技能树（ability） | **1,406** 条 | 1,043 | **85**（7.5%） | 全部是 `unit`（兵种卡里 85 张确实没有技能）；**不是解析缺口** |
| ② VFX（逐卡） | **2,755** 条 | 1,089 | **39**（3.5%） | 39 张**原版 `cardAnimations` 就是 0 且自定义粒子/残骸也全空引用**（已回 SO 真值逐张核）⇒ **原版就没有**，不是缺 |
| ③ anim（逐卡） | **1,468** 条 | 874 | **254**（22.5%） | 254 张**原版 `cardAnimations` = 0**（回 `cards_raw.json` 的 `cardAbilities/activeAbility/alternativeAbility` 逐张核过，254/254 一致）⇒ **原版就没有** |
| ④ 语音 cue | **1,897** 条（642 卡） | 642 | **486**（43.1%） | 486 张 = **tactic 446 + defence 39 + enchantment 1**（零 unit / 零 hero）⇒ **按卡种就是原版设计** |
| 四维**都有**声明 | —— | **419** 张（37.1%） | —— | 其余 709 张至少一维为空，绝大多数是「计策/防御卡没有语音」（454 张）与「没有动画」（214 张） |

### 1.4 需要 S2 接手的「事实」（不是判定，只是本阶段查实的形状）

- **45 条 VFX 落在 `trigger_unmapped`**（26 张卡）：原版这些动画挂在我们的引擎**没有发射口**的触发上
  （`Unstable` ×36 条 / `UnitLoseStealth`、`UnitStunned`、`OnCollectQuestPoints`、`OnSecretCreated`、`OnSabotageCreated`、`TriggeredCodex` 各若干）
  ⇒ **S2 的第一批候选**（VFX 表已逐条带 `abilityEventWhy` 出处，本表把它们放在 `声明的VFX` 列的 `trigger_unmapped:` 事件名下）。
- **631 张近战卡里只有 11 张有 `attack_melee` 粒子** —— 不是缺口：原版近战特效走卡的 **3D Animator**（`card3DAnimationController` + `AlternateArtCard.attackAnim`），
  `VfxMap.cs:356` 的注释已写明，且**原版 1,195 张卡里只有 2 张绑了 `attackAnim` 且都是空引用**。
- **37 张 `ranged>0` 的卡既没有逐卡远程粒子、所在阵营也没有阵营档粒子** —— 已回 `full_raw` 核：其 `customRangedAttackParticles` **就是空 PPtr** ⇒ 原版没有，⛔ 别补。

---

## ② 逐维统计表

### 2.1 列清单（22 列）与每一列的判据

| # | 列名 | 取值来自 | 判据 / 口径 |
|---|---|---|---|
| 1–5 | `卡id` `卡名` `卡名Zh` `阵营` `卡种` | `cards_engine.json` | id 已是原版真 id（`poolIdsAreTrue: true`，⛔ 别反转 `poolTrueIds`） |
| 6–8 | `ability数` `ability明细` `trigger集合` | **`cards_truth.json`**（一卡一份，🥇 唯一正确统计源） | `ability数 = len(abilities) + active + alt`；`trigger集合` 里 `active(trigger=-1)` = `activeAbility`、`alt` = `alternativeAbility`。触发名 = 枚举桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs`（97 项，**57 个被池内用到，全部有名字**） |
| 9 | `chosenAbility集合` | `cards_truth` 的 `abilities[].logic[].chosenAbility` **＋ `activeAbility` ＋ `alternativeAbility`** | 名字 = `AbilityEffect.cs` 枚举（83 项；池内用到 52 个，**全部解析出名字**）。🔴 只扫 `abilities` 会漏（143 张卡的效果在 `activeAbility` 里） |
| 10 | `buffType集合(整卡)` | 同上，**整卡**（含 `handBuff.cardEffect`） | 名字 = `BuffType.cs`（28 项）。池内共 **1,015 处**、覆盖 **657 张卡**。🔴 只扫 `abilities` 会少 73 条（本表按规格扫全卡） |
| 11–12 | `VFX条数` `声明的VFX` | `card_vfx_by_card.json` 的 `byCard[卡id]`（每事件一条） | 一条 = `事件:prefab(存在?)`；`inLibrary/assetName/cardanim/abilityTriggerName` 都在源表里 |
| 13 | `共用链VFX(非逐卡)` | 同上文件 `generic.byEvent` / `generic.attackRangedByArmy` | `unit/hero` 才列（部署/命中/死亡共用链 + 阵营远程档）。阵营远程档原版**只有 4 个**：10 UM / 20 Goff / 40 Sautekh / 50 BlackLegion（`RangedAttackParticlesByArmy`） |
| 14–15 | `anim条数` `原版cardAnimations数` | `cards_engine.animTimes` + **`cards_raw.json` 的 `cardAbilities/activeAbility/alternativeAbility` 单份树** | 两列是**互不依赖的两条链**（前者 = 生成器产物，后者 = SO 真值直读）⇒ 用来做 §⑤-1 的交叉核对。⚠️ 递归时**跳过 `full_raw`**（否则翻倍） |
| 16 | `声明的anim` | `cards_engine.animTimes` | 一条 = `CardAnim名[tg触发:触发名](d,sd,s,m,e,u,t)(表✓/prefab✓)`。**时序口径**（`工具/gen_cards_engine.py:3069-3101`）= `d=vfxDelayTime · sd=startDelay · s=timeAtStartPos · m=timeMoving · e=timeAtEndPos · u=时长(m+s+e+d) · t=同(卡,trigger)组内起点偏移(前面各拍 u 累加)`，`startDelay` **不计入** u（照原版 `AnimInfo.GetAnimDuration`） |
| 17–19 | `语音条数` `语音cue` `开场白cue` | `Resources/voice_lines.json`（v2）＋ `数据/游戏数据/card_chat_intro.json` | `语音cue` = `ev:文件名(存在?)`，另标 `[orphan]`（归属靠文件名猜）/`[卡索引兜底]`/`(死数据:原版不播)`；`开场白cue` = 原版 `RawCardScript` 的 12 个 chat 字段 + 逐对手开场白 |
| 20 | `工程缺失项` | 本表自算 | 只放**事实缺口**（找不到的引用 / 两源条数不一致）。**空白 = 无缺口** |
| 21–22 | `账档位分布(A/B/C/D)` `账命中方法` | `资料/完全复刻/账/*.tsv`（21 份） | 见 §2.6；`直接` = 账行文本里**点名这张卡 id**的行；`机制` = 按该卡机制词命中的行 |

### 2.2 ① 引擎技能树

| 指标 | 现读 |
|---|---|
| ability 条数（池内） | **1,406**（`cards_truth` 全量 1,351 张是 1,496，差 = 池外 223 张的 90 条） |
| 用到 distinct trigger id | **57**（枚举 97；无名 trigger = **0**） |
| trigger 排行（卡数） | `0 ThisCardPlayed` 658 · `46 Talent` 96 · `145 Codex` 34 · `240 WhileInPlayVariable` 33 · `410 FollowUpResolution` 31 · `580 Strike` 30 · `600 UseSpiritStone` 29 · `605 UseFaith` 29 · `90 OtherUnitDeath` 28 · `655` 23 … |
| 用到 distinct chosenAbility | **52**（枚举 83）：`30 buffOthers` 420 · `10 doDamage` 272 · `100 createCardInHand` 164 · `40 buffSelf` 147 · `0 drawCard` 76 · `70 summonUnit` 70 · `20 doHeal` 63 · `60 buffMana` 52 · `462 triggerPray` 31 · `130 stun` 28 … |
| buffType（整卡扫） | **1,015 处 / 657 卡**：`1 addStats` 433 · `3 addTrait` 341 · `2 changeCost` 122 · `15 singleUseEnchantment` 37 · `8 markOfChaos` 24 · `12 enchantment` 23 · `4 removeTrait` 14 · `16 choiceEnchantment` 13 · `95 setCost` 3 · 其余各 1 |

### 2.3 ② VFX（逐卡，`card_vfx_by_card.byCard`）

| 事件桶 | 条数 | 有该桶的卡数 | 说明 |
|---|---|---|---|
| `trigger` | **935** | 572 | 触发器动画（按 trigger 名字路由） |
| `play` | **795** | 533 | 打出时 |
| `attack_ranged` | **518** | 518 | 逐卡 `customRangedAttackParticles`（**与 SO 真值 518 张一一对上，双向 0 差错**） |
| `ability` | **160** | 93 | `activeAbility` 主动技 |
| `death` | **145** | 101 | `customDeathParticles` **66** 条 + `remnantBodyPrefab` **71** 条（重卡 42 ⇒ 并集 95 卡）+ **8** 条从 `cardAbilities[].abilityLogic[].cardAnimations` 归口来的阵亡动画 ⇒ 95 卡 + 6 卡（后者） = 101 卡 |
| `trait_onplay` | **142** | 137 | 关键词 trait（来源 `cardtrait-so`，非逐卡 SO 字段） |
| `trigger_unmapped` | **45** | 26 | 声明了但**引擎无发射口**（S2 候选，见 §1.4） |
| `attack_melee` | **13** | 11 | 近战粒子（原版绝大多数走 3D Animator） |
| `deploy` / `hit` | 1 / 1 | 1 / 1 | 逐卡的「召唤延迟特效 / 命中特效」；**通用档在 `generic.byEvent`**（部署 4 条、命中 2 条、死亡 1 条、治疗 1 条） |
| **合计** | **2,755** | 1,089 | 其中 **2,743 条有 prefab 名**、12 条无（全是 `Wait_*` 计时拍） |

**存在性**：2,743 / 2,743 的 prefab **同时**在
① `Assets/WarpforgeVFX/Prefabs/<名>.prefab`（现读 **978** 个 prefab 文件）与
② `Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 的 `entries[].name`（现读 **982** 个库名）里。
⇒ **`inLibrary` 为 true 的 2,116 条 + trait 来源的 627 条全部落地**；`✗` = **0**。

### 2.4 ③ anim（`cards_engine.animTimes` ↔ 工程 CardAnim 表）

| 指标 | 现读 |
|---|---|
| anim 行数（池内） | **1,468**（`card_anim_order.json` 的 `_coverage.cardAnimationsRefs` **也是 1,468**，两源一致） |
| 其中 `Atk_*` 攻击动画 | 135 行（131 在 `atk_vfx_map.json` 的 297 条里；**4 条不在**） |
| `Wait_*` 计时拍 | **12 行**（`Wait_300ms / Wait_1s / Wait_1,5s / Wait_2s`：原版就是**没有 prefab 的纯延迟**，VFX 表里同样是这 12 条无 prefab） |
| 非 Wait 的 1,456 行 | **1,456 / 1,456 全部「CardAnim 在表里 ∧ prefab 在工程里」**（CardAnim 表 = `Assets/CardPresentation/Resources/cardanim_clips.json` 的 **1,067** 行） |
| 原版 `cardAnimations` 引用（真值直读） | **1,469** ⇒ 比衍生表的 1,468 **多 1 条**（SW21，见 §⑤-1） |
| 声明为 0 的卡 | 254（回真值核过，254/254 确实 0 条） |

> ⚠️ **动画「在」≠「能播」**：本列只判 **CardAnim 数据在表里 + 对应 prefab 在工程里**。
> CardAnim 是 `MonoBehaviour`（不是 clip），真正的 clip 播放由 `CardAnimClips` 的运行期投影 + 特效 prefab 承担；
> 「能不能播 / 时序对不对」**本阶段不下结论**（规格 §六：动画/网格类资产不能从 bundle 直接拷，运行期格式会成空壳）。

### 2.5 ④ 语音 cue

| 指标 | 现读 |
|---|---|
| `voice_lines.json` | **642 卡 / 1,897 行**（642 卡**全在池内**），涉及 **1,840 个不同音频文件** |
| 文件存在性 | **1,840 / 1,840 全部在** `Resources/Art/audio/vo/`（1,726 `.ogg` + 114 `.wav`）——按**文件名**判，不是按 guid |
| 行来源 | `cardSO` **1,370** · `card_index` 兜底 **281** · `orphan`（按文件名猜主人）**246** |
| **死数据行** | **289 行**（`gen4–gen7` + `backup*`：文件头写明「原版**没有**这些通道，保留作证据」）⇒ 原版会播的行 = **1,608** |
| 事件排行 | `deploy` 587 · 其余 13 族各 56（`attack/death/default/cantdo/concede/gen1-3/greet/hurry/intro/mirror/threat/wp`）+ `intro-vs` 19 + 各种 `vs*` |
| 开场白（`card_chat_intro`） | 原版 SO 覆盖 **67** 卡，其中**池内 56 卡**（本表 `开场白cue` 非空 = 56）；逐对手开场白 + `introChatSound/mirrorChatSound/defaultChatSound` 的 `soundAsset` **全部在工程里找得到** |

**两个数字要现读、别混**：语音线交接文件写「**1,844 条 / 606 卡**」，本表是「**1,897 行 / 642 卡 / 1,840 个文件**」——
1,844 ≈ 文件数（差 4），606 是**旧口径的「有战斗语音的卡」**。`vo/` 目录现读 **1,893 个文件** ⇒ 有 **53 个音频文件没有任何卡引用**（§⑤-5）。

### 2.6 ⑥ 账档位分布（**这是代理量，不是逐卡账**）

**为什么要代理**：`账/*.tsv` 的单位是「原版类 × 方法」（21 份 / **9,617** 数据行），**没有「卡」这个轴**。
所以本表给两列，各自的规则写死如下（可复算）：

1. `直接 A?B?C?D?` —— 账行文本（类/方法/判据要点/差异/备注）里**出现这张卡的 id** 的行 ⇒ **点名证据**。
   现读：**168 张卡**有直接命中（另有一些 id 只在池外卡上）。
2. `机制 A?B?C?D?` —— 按 `cards_truth` 的机制词去匹配 `账/战斗逻辑驱动.tsv` 的**方法名**（⚠️ 排除 `AI*` 三个类，那是决策层不是卡的结算路径）：

| 机制词 | 命中方法名正则 | 命中行数（A/B/C/D） |
|---|---|---|
| 所有卡（基础） | `PlayAbility` / `CanPlayAbility` / `MatchingTrigger` / `GetTargets` | 7（A2 B2 D3） |
| damage | `Damage` ∥ `Attack` ∥ `Hit` | 103（A71 B29 C2 D1） |
| heal | `Heal` | 17（A6 B9 C1 D1） |
| summon | `Summon` ∥ `Reanimate` | 15（A11 B3 C1） |
| buff | `Buff` ∥ `Trait` ∥ `Keyword` ∥ `Aura` ∥ `Enchant` | 78（A41 B19 C7 D11） |
| mana | `Mana` ∥ `Energy` | 28（A5 B11 D12） |
| hand | `Hand` ∥ `Draw` | 98（A61 B21 C5 D11） |
| death | `Death` ∥ `Remnant` ∥ `Cemetery` ∥ `Destroy` | 45（A24 B16 C1 D4） |

（表中的 `∥` 只是「正则里的或」，⛔ 不是字面字符；真实正则是 `Damage|Attack|Hit` 这种写法。）

现读：**44 种不同的机制分布**；**969 张卡**的机制代理里至少含 1 条 C。
全账现读合计 **A3,550 · B1,472 · C968 · D3,625 · E2**（E 两行分别是 `CollectionDisplay<T>.OnListDrag` 与 `LiveOpsHandler<T>.GetBaseEvents`，**都不是战斗机制**，未计入卡的分布）。

> 🔴 **口径说明（别拿它当「这张卡已复刻/没复刻」）**：`机制` 列是**机制级代理**——它回答「这张卡用到的那族机制，账上是什么档位」，
> ⛔ **不回答**「这张卡的这条效果逐句对上了没有」（那是 S3 的 `ruleprobe play <id>`）。
>
> ⚠️ **账是活的**：本列取的是 **2026-10-26 22:40 前后**的快照（当时 21 份 tsv / 9,617 数据行）；同日还有多条代理在往 `账/` 合并补丁
> ⇒ 账一涨，`机制` 列的计数就会变。**要复核请按上表的匹配规则重跑**（规则是确定性的，不依赖我的脚本）。

### 2.7 按阵营的四维覆盖（卡数 / 条数 / 为零的卡数）

| 阵营 | 卡数 | VFX条 | VFX=0 | anim条 | anim=0 | 语音条 | 语音=0 |
|---|---|---|---|---|---|---|---|
| AstraMilitarum | 78 | 219 | 0 | 146 | 5 | 143 | 32 |
| BlackLegion | 84 | 128 | 9 | 62 | 36 | 137 | 38 |
| DarkAngels | 83 | 224 | 2 | 135 | 7 | 130 | 37 |
| EmperorsChildren | 70 | 168 | 0 | 94 | 4 | 100 | 32 |
| Genestealers | 83 | 197 | 2 | 117 | 7 | 136 | 37 |
| Goff | 119 | 280 | 5 | 149 | 31 | 201 | 52 |
| Leviathan | 79 | 156 | 2 | 64 | 29 | 110 | 33 |
| SaimHann | 92 | 282 | 1 | 141 | 25 | 157 | 38 |
| Sautekh | 79 | 261 | 1 | 76 | 29 | 138 | 33 |
| Sororitas | 78 | 203 | 3 | 152 | 7 | 149 | 32 |
| SpaceWolves | 68 | 163 | 3 | 101 | 9 | 111 | 29 |
| TauEmpire | 81 | 174 | 5 | 66 | 33 | 138 | 35 |
| Ultramarines | 134 | 300 | 6 | 165 | 32 | 247 | 58 |
| **合计** | **1,128** | **2,755** | **39** | **1,468** | **254** | **1,897** | **486** |

**没有任何阵营在某维整列全空**（最差的 BlackLegion anim=36/84、TauEmpire anim=33/81，已逐张回真值核为「原版 0 条」）。

### 2.8 「原版 GUID → 工程/束」两跳 —— 我自己实现并抽样全验（规格要求写进报告）

规格说 `guid_map.tsv` 只给第一跳（GUID → 束），**第二跳（束的 `m_Container` → 资产名）任何文档都没写**。实做如下：

```text
hop1: guid_map.tsv（4,998 行，字段 uniqueId/field/assetGuid/bundle/containerKey/type/name）
      → 取 assetGuid == 目标 GUID 的那行，得到 bundle 名
hop2: UnityPy.load(<d:/2/unity_run_ref/.../StandaloneWindows64/<bundle>>)
      → 找 type == 'AssetBundle' 的对象，read_typetree()['m_Container']
      → 取 key == GUID 的条目 → ['asset']['m_PathID']
      → 在同一个 env.objects 里按 path_id 找到对象 → read_typetree()['m_Name']
```
（判据：`m_Container` 在真实 build 里是 `[(容器键, {preloadIndex, preloadSize, asset:{m_FileID, m_PathID}})]`；
容器键**并不总是 GUID** —— 见下面 33 条的短键族。）

**抽样规模 = 全部可就地解析的逐卡 VFX 引用**（不是随机抽几条）：

| 项 | 现读 |
|---|---|
| 逐卡 VFX 引用涉及的 GUID | **741**（分布在 15 个束：`cardanimsgeneral` **331** + **13** 个阵营 cardanims 共 **406** + `battleprefabs_vfxandmisc` **4**） |
| 两跳解出资产名 | **708 / 741 = 95.5%** |
| 解出的名字与 `card_vfx_by_card.assetName` 一致 | **708 / 708 = 100%**（⇒ 那张表的 `assetName` 可信，可以当 `a` 的 join 键） |
| 两跳解不出 | **33 / 741 = 4.5%**，**全部在 `chaosspacemarinesemperorschildrencardanims_assets_all.bundle`**：该束的 `m_Container` 键是**短键**（`0` / `0c` / `1` / `1b` …，共 52 条），**不是 GUID** ⇒ 缺第三跳 `catalog.bin`（GUID → internalId → 短键）。我已核：这些资产**确实在那个束里**（按 `m_Name` 找得到，33/33），且 `catalog.bin`（1,305,148 B）里**含这些 GUID 的 ASCII 串**（样本 `564af14f…` 在偏移 401,603） |

---

## ③ 没查清（还差什么）

| # | 没查清的东西 | 差什么 | 谁能补 |
|---|---|---|---|
| 1 | **「动画能不能播 / 时序对不对」** | 本列只判「CardAnim 在表 + prefab 在工程」。CardAnim → clip 的运行期投影（`CardAnimClips.RtRow`）与 `ability_anim_durations.json` 的时长约定**没有逐卡比对** | S2/S3（要 Unity） |
| 2 | **33 个短键 GUID 的第三跳** | `catalog.bin` 的二进制格式（GUID → internalId → 短键）未解码；目前只证明「GUID 在里面 + 资产在束里」 | 工具线（`gen_guid_map_from_bundles.py` 那条会读 catalog） |
| 3 | **`trigger_unmapped` 45 条到底该不该播** | 本阶段只照 VfxMap 的现状标「无发射口」；**原版这些触发是否真的会发这条动画**要回 `AbilityLogic__PlayAbility.c` / `CardScript.OnTrigger` 的 57 个 trigger id 逐条核（S2） | S2 |
| 4 | **`attack_melee` 只 13 条、而近战卡 631 张** | 那 13 条来自 `cardAbilities[].abilityLogic[].cardAnimations`（事件归口为 `attack_melee`，**不是**某个「近战粒子字段」），说明**原版逐卡的近战粒子本来就只有这 13 条**；其余近战走卡的 3D Animator（`AlternateArtCard.attackAnim`，原版几乎全空）。我们工程里 `Assets/WarpforgeVFX/Animations` 只有 **47** 个 `.anim`、`Animators` 只有 **1** 个 controller ⇒ **近战动画链没有逐卡数据源可 join** | 需另做一维普查（`alternateArts[].attackAnim` + `card3DAnimationController`） |
| 5 | **`death` 桶里两条语义不同的东西没分开** | `abilityEventMapping` 把 `remnantBodyPrefab`（原版触发时刻是「**转化为残骸**」，不是阵亡）与 `customDeathParticles`（真阵亡）**都归进 `death`**；本表两条都留着（66 + 71 + 8 条 cardAnimations = 145 条）。🔴 **没查到的是运行期**：`VfxMap` 的 `death` 档对同一张卡只取一条时**取哪一条**（源表 `_conventions` 原文写「42 张两者都有的卡谁进 `death` 是掷硬币」，我本轮**没有回代码核这个说法**） | S2（按 `CardScript.TransformIntoRemnant` / `UnitDeath` 两条调用点分开） |
| 6 | **语音「响不响」** | 本表只判「音频文件在」。cue 名 → `WindowSounds` 的映射、`hurry`/`vs*` 的播放时机都未在逐卡层验证。📌 **同日另一代理已产出 `判据/音效cue到WindowSounds_映射.tsv`（614 行，含「原版消费者 × 引用数 / 我们侧出处 / 状态」）—— 本表未并入、也未逐条核，S3 可直接拿它当起点** | S3 |
| 7 | **`voice_lines` 1,897 vs 文档 1,844 / 目录 1,893** | 三个数各有口径（行数 / 旧文件数 / 现目录文件数）；**差 4 与差 53 未逐条对出是哪几个文件** | 音效线一条小活 |
| 8 | **账的逐卡轴不存在** | 本表的「机制」列是代理；要「这张卡逐句对不对」只能等 S3 的 `ruleprobe play <id>`（全池对拍现读 1,144 对 / 1 错 / 783 未验到） | S3 |

---

## ④ 改动清单（只有这两个文件）

| 文件 | 动作 |
|---|---|
| `资料/完全复刻/判据/全卡核验总表.tsv` | **新建**（22 列 × 1,128 行 + 表头，UTF-8 / LF / 制表符分隔，983,210 B） |
| `资料/完全复刻/报告/B9_J6CA_全卡核验总表.md` | **新建**（本文件） |

⛔ 没有改任何 `.cs` / 资产 / 主账表 / 正本；没有跑 Unity；没有动 git。
（生成脚本写在仓外临时目录 `%TEMP%/j6ca/`，不落进仓库。）

---

## ⑤ 顺手发现

1. 🔴 **`cards_engine.animTimes` 与 `card_anim_order.json` 都漏收 `alternativeAbility` 的动画引用**
   —— 池内唯一一例 **SW21 `Wulfen Pack Leader`**：SO 真值 3 条
   （`activeAbility[0].abilityLogic[0].cardAnimations[0]` = `94501434…`、`[1]` = `c54f94d3…`、`alternativeAbility[0]…[0]` = `94501434…`），
   两张衍生表都只有 **2** 条（`Cut Wulfen` / `Cut Wulfen Reverse`）。
   ⇒ 全池 stats：真值 **1,469** vs `animTimes` **1,468** vs `card_anim_order` **1,468**。影响面 = **1 张卡**（`hasAlternativeAbility=1` 本来就只这一张），且缺的那条 GUID 与 active 第 1 条**相同** ⇒ 不影响「prefab 在不在」，但**S2 要判「用替代能力时这条动画会不会播」就缺数据**。
   ⚠️ `card_vfx_by_card.byCard['SW21']` 同样只有 `activeAbility` 两条（无 `alternativeAbility`）⇒ 三张表同源同病。

2. 🔴 **`资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv` 只覆盖 51/83 个 `chosenAbility`**
   （缺 71/72/80/85/90/120/135/160/162/165/170/175…；`BuffType` 只覆盖 15/28）。
   典型缺项：**`462 = triggerPray`**（池内 **31 张卡**在用），词典里查不到 ⇒ 若照那份词典做判据会出现「能力名解不出」。
   ✅ 本表改用**原版枚举桩** `AbilityEffect.cs`（83）/`BuffType.cs`（28）/`AbilityTrigger.cs`（97）⇒ **池内 57 个 trigger、52 个 chosenAbility 全部有名**。建议就地改那份词典（它不在我的白名单里，未动）。

3. 🔴 **`trigger_unmapped` 的构成高度集中**：45 条里 **36 条是 `Unstable`（81）**（`UnstableExplosion_self/_target`、`_WaaaghEnergy_self/_target`，18 张卡）。
   ⇒ 只要把 `Unstable` 这一条触发接上发射口，就能一次消掉 80% 的未归口条目。

4. **`Pray Enchantment`（`9218576f…`）是池内唯一不在 `card_vfx_by_card.byCard` 的卡**
   —— 它不是漏了：该卡 SO 的 `cardAnimations` = 0、自定义粒子/残骸/召唤延迟**全空引用** ⇒ 原版没有任何逐卡 VFX。
   `byCard` 只有 1,127 个键的原因 = 那张表按**当时的池快照（1,127）**生成，而 `cards_engine` 现在是 **1,128**。

5. **`vo/` 目录 1,893 个音频文件里，只有 1,840 个被 `voice_lines.json` 引用**（53 个没人用）——
   另外 `voice_lines` 有 **289 行是原版不播的死数据**（`gen4–gen7`/`backup*`，文件头写明「保留作证据」）。
   ⇒ 做「语音有没有响」的统计时，**分母别用 1,897**（会高估 18%）。

6. **`whileInPlayAnim` / `targetedSpellAnim` / `nonTargetedSpellAnim` 三个 SO 字段在池内 1,128 张里全是空引用**
   （逐张读 `full_raw` 的 PPtr 得到 0 个非空）⇒ 原版这三条通道在本 build 里是死的。
   ⛔ 别拿这三条当「我们还没做的特效」立账。

7. **VFX 维度的 518 条 `attack_ranged` 与 SO 真值 `customRangedAttackParticles` 的 518 张卡双向 0 差错**
   （「表里有、SO 没有」= 0；「SO 有、表里没有」= 0）——这是本表最干净的一维，可当 S2 的基准。

8. **`buffType` 的「全量口径」和文档里那个 1,198 对不上（现读 1,186）**。
   同一个扫法（`abilities` ＋ `activeAbility` ＋ `alternativeAbility`，递归扫 `buffType`、跳过 0）现读：
   **只扫 `abilities` = 1,125**（与 `CLAUDE.md` 那个 1,125 吻合）· **整卡 = 1,186**（`cards_truth`）/ **1,187**（`cards_raw` 的解析视图）。
   `CLAUDE.md` 写的「非 0 总数 **1,198**」比现读多 **12** —— **没查清是哪一处口径差**（本轮不猜）。
   池内（1,128 张）同法是 **1,015 处 / 657 卡**；池外 223 张是 171 处 ⇒ 1,015 + 171 = 1,186，**池内池外能对上**。
   ⛔ 引用时一律写「现读」，别把 1,198 当现状。

---

## ⑥ 有没有「某维系统性缺失」——是真缺，还是数据源没覆盖

**结论：四维都没有「系统性缺失」。逐个维度的判定过程如下（每条都回过真值源，不是靠「表里没有」推的）：**

| 维度 | 表面上像缺的现象 | 回源结论 | 判定 |
|---|---|---|---|
| VFX | 39 张卡 VFX=0；BlackLegion 9 张 | 逐张读 `cards_raw.json` 的 `full_raw`：`customRangedAttackParticles` / `customDeathParticles` / `remnantBodyPrefab` / `summonAnimDelayed` **全空引用**，`cardAnimations` 也是 0 ⇒ **原版就没有** | **真「原版没有」**，不是缺 |
| VFX | `attack_melee` 只有 13 条，而近战卡 631 张 | 原版近战特效走卡的 3D Animator（`AlternateArtCard.attackAnim`，原版几乎全空）；VFX 表 `generic.notes` 与 `VfxMap.cs:299,356` 都写明 | **数据源没覆盖这一档**（不是「缺」，但也**没查到**：近战链无逐卡数据源，见 §③-4） |
| VFX | 37 张 `ranged>0` 卡无任何远程粒子（阵营也无档） | `full_raw.customRangedAttackParticles` 就是空 PPtr | **原版没有** |
| anim | 254 张卡 anim=0（22.5%），BlackLegion 36 / TauEmpire 33 / UM 32 | `cards_raw` 单份树逐张核：**254/254 的 `cardAnimations` 都是 0** | **原版没有** |
| anim | 12 条 `Wait_*` 无 prefab | 原版就是纯延迟拍（`stepsWithoutVfx_pureWaitBeats: 14` 同族口径） | **设计如此** |
| 语音 | 486 张卡语音=0（43%） | 全部落在 tactic / defence / enchantment；**零 unit、零 hero** ⇒ 兵种/英雄全有 `summonAudio`，计策卡原版本来没有语音字段（对话语音走 `chat` 通道，见 `开场白cue`） | **按卡种就是原版设计** |
| 语音 | 开场白只有 56/1,128 张 | `card_chat_intro` 的原版 SO 覆盖本来只有 **67 张**（督军/带自定义 intro 的卡），池内 56 | **原版就没有给所有卡** |
| 账 | 969 张卡机制代理里含 C | 那是「卡的机制族里有 C 档方法」，**不是这张卡被标了 C** | 代理量，**不能当缺失** |

**唯一一条「像系统性缺、实际是数据源口径」的**：近战特效链（`attack_melee`）—— 见上表第 2 行，
它属于 §③-4 的「没查清」，⛔ 不能写成「我们已经做了」也不能写成「原版没有」。
