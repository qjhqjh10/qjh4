# S11 · 联盟面板「四件缺口」＋ `AbilityTrigger.PlayedThirdCardInTurn`（只读侦察报告）

> ⚠️ **落盘说明**：该代理是 `Explore` 型、**没有写文件工具** ⇒ 本文件由**主对话摘录代录**（结论与出处逐条保留，口语化过渡句有删减，**数值/行号/资产路径一字未改**）。
> **日期**：2026-10-17（第四十二会话）· ⛔ 未跑 Unity、未动 git、未改任何文件。
> 🔑 **本报告推翻/改写了**：`AlliancePanelWindow.cs` 文件头那「四件缺口」的**三条理由** · `战斗线总表` 里 `T01` 那条「**两条都落铁律 11 例外②（判据在远端）**」的**前提**。

---

## 🔴 一条贯穿全篇的第一权威

`DeckAndWarlordData`（`dump.cs:77945-77964`）字段表：
`0x10 deckName` · **`0x38 eloRating`** · **`0x40 playerAvatar`** · `0x48 playerAvatarBorder` · **`0x50 playerTitle`** · **`0x68 allianceName`** · **`0x7C allianceRating`** · **`0x80 allianceBadge : GroupBadge`**。
`MatchData.enemyRawBattleData @0x50` / `playerRawBattleData @0x58`（`dump.cs:78144-78145`）。
⇒ **`BattleHud__AlliancePanelOpenButton.c:25-46`** 就是从这里**逐格取出来**喂 `BattleAlliancePanel__Open` 那 8 个实参的。

---

# 第一节 · 四件缺口 —— 资源在不在 / 缺什么

| 件 | 图在不在（路径） | 缺的到底是 **id** 还是**资源** | 我们单机拿得到吗 | 能不能补 |
|---|---|---|---|---|
| **① 头像立绘** | ✅ **在**：`bundle_cosmeticavatarsimages_assets_all/Sprite|Texture2D/Avatar_<阵营>_<名>`（`Avatar_AM_Astropath` · `Avatar_AM_Attilan Rough Rider` …）。⚠️ **本仓已导入**：`MyGame/Assets/CardPresentation/Resources/Art/avatars/`（**944 文件**）；取图口 = `Core/CardArt.cs:601-604 Cosmetics(name)` → `Resources/Art/avatars/<名>` | **只缺 id 的【取值链】** —— `m_Sprite=0` **只是出厂态**：`AvatarDisplay__ChangeAvatar.c:53-70` 里 `avatarImage.sprite = CosmeticItem.get_Image(item)` 然后 `set_enabled(img, sprite != null)`（**有图就开**）；实参来自 `PlayerAvatarDataManager.GetItemWithId(DeckAndWarlordData.playerAvatar@0x40)`（`BattleHud__AlliancePanelOpenButton.c:41-43`），**null 还回落 `get_DefaultAvatarItem()`** ⇒ **原版永远有图** | ✅ **拿得到，而且我们已经在画**：`BattleDriver.cs` 的 `BuildHudExtras` 建 `AvatarImage_Me/Foe`（`:15167/:15170`）—— **我方** = `ProfileData.AvatarArt` · **对手** = `BotAvatarIndex()` 按本局种子从 `ProfileData.Avatars` 确定性挑 | ✅ **能做**。唯一真缺 = 原版 bot 的**允许清单** `AIBotsConfig.GetRandomAvatar`（**LiveOps 远端**）⇒ 我们「从本地清单挑」是**已如实标注的近似**。搬 HUD 那一套进面板即可 |
| **② 两处评级** | 不需要图（**图标来自 LiveOps**） | **缺的是数据 id**：`AllianceRatingDisplay__Initialize(playerScore, param_4 = eloRating@0x38, liveOpsAsset)` 与 `(allianceRatingDisplay, param_8 = allianceRating@0x7C, 0, …)`（`BattleAlliancePanel__Open.c:76,113`）。图标那半 = `AllianceRatingDisplay__Initialize.c:42-56`：`GameModes.GetActiveEvent → LiveOpsAssetUtility.HasAsset(ev,6)/LoadAsset(ev,6)` = **每事件远端资产** | ⛔ **拿不到**：我们的引擎 `grep eloRating|allianceRating|allianceName|allianceBadge` 于 `Assets/RuleEngine/` + `Battle/BattleDriver.cs` = **0 命中**；壳侧 `Shell/SocialData.cs:27-30` **明写** `AllianceName`/`AllianceTrophies` **本地恒空** | ⚠️ **要拆成两句**：**(a)「我们单机没有对手评级数据」= 成立**（数据源在服务端 profile + LiveOps）· **(b)「静态取不到」只是【尺寸层面】** —— 两节点挂 HLG、`childControlWidth/Height = 1` ⇒ 尺寸运行期算 ⇒ ⛔ **别把这句当「做不了」**（一次运行期读数或按首选宽重算即可）。⇒ 缺口②归**「等远端」** |
| **③ 徽章 `Frame` / `Badge`** | ✅ **在，两层都有**：`bundle_alliancesbadgesimages_assets_all/Sprite|Texture2D/`（**22 张**）= `Alliance_Trophies_<阵营>`（AM / BlackLegion / Dark Angels / Emperors Children / Goff / GSC / Leviathan / Ork / SaimHann / Sautekh / Sororitas / Space Wolves / Tau / Ultramarines(+event) / Space Marine 2 event / Black Legion event 1 / Default_v2）＋ 🔑 **Frame 四档 = `Alliance_Trophies_TIER1..TIER4`**。SO 那半 = `bundle_cosmeticsso_assets_all/MonoBehaviour/` 的 **16 个 `*_Badge.json`**（`Default_Badge` · `AM_Forge_Badge` … `UM_Event_Badge`；字段 = `uniqueId` + `cardArmy` + `badgeImage : AssetReferenceGUID`） | **缺的是 id**。两层的确切归属（第一权威）：`AllianceBadgeDrawer : ItemDrawer<AllianceTrophyData>`（`dump.cs:96857`；字段 `tierFrame:Image@0x58` · `armyText@0x60` · `currentTier:int?@0x68`）。**`Badge`** = 基类主图 `param_1[7](0x38)` ← `AllianceBadgeDrawer__Draw.c:24-30`：`GroupBadge.featuredTrophy@0x10` → `AllianceTrophyData.reward@0x58` → `AllianceBadge.badgeImage@0x38`（`dump.cs:96262-96266`）→ 那张 `Alliance_Trophies_<阵营>`；**null 回落 `AlliancesTrophyController.DefaultTrophy`**。**`Frame`** = `AllianceBadgeDrawer__SetTierFrame.c` → `AlliancesTrophyController.LoadTierFrame`，索引 = `Max(0, tier-1)` 取那 **4 张 TIER**。id 来源 = `*(DeckAndWarlordData+0x80) : GroupBadge`（`GroupBadge` = `featuredTrophy@0x10` + `tierLevel@0x18`，`dump.cs:96521-96527`） | ⛔ **拿不到**（同样是我们单机没有联盟对象）。⚠️ **而且我们连自己那份 `*_Badge` 都没接**：`grep Alliance_Trophies` 在 `d:/4` **只命中两份盘点文档**（`普查产出_0917/菜单盘点_块3.md:98` · `块4.md:84` · `索引与盘点/按阵营资源索引.md`），**零代码 / 零生成器**引用 | 🔴 **理由要改**：**不是**「原版出厂态没有」，而是 **`Open` 里 `bVar9 = !IsNullOrEmpty(allianceName)` ⇒ 联盟名空 ⇒ `allianceGO` 关、`Draw` 压根不执行**（`BattleAlliancePanel__Open.c:93-108`）。我们传 `Open(name,"","")` ⇒ **`Draw` 永远走不到** ⇒ 缺口③ = **走不到那一支**，⛔ 不是「原版没有」 |
| **④ 出场动画 `DOFade`+`DOScale`** | 不需要图 | **缺的三个常量 —— 已全部读出** | — | ✅ **能做，而且算「欠做」**（见下） |

## ④ 的判据（能不能读常量）—— **已读出**

原版方法体 = `d:/2/tools/decomp_full/BattleAlliancePanel__Open.c:99-125`，四句：
`CanvasGroup__set_alpha(cg, 0)` → `DG_Tweening_DOTweenModuleUI__DOFade(cg, **DAT_1834b2bb8**, **DAT_1834b31d4**, 0)` → `localScale *= **DAT_1834b2e84**` 再 `set_localScale` → `DG_Tweening_ShortcutExtensions__DOScale(transform, 原 localScale, DAT_1834b31d4, 0)`。

**实读**（`D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" …`，⚠️ **要 `PYTHONIOENCODING=utf-8`**，否则 GBK 报 `UnicodeEncodeError`）：

| 常量 | 值 | 用处 |
|---|---|---|
| `0x1834b2bb8` | **1.0** | `DOFade` 的**目标 alpha**（4B float；8B 档读成 double 2 也「像」⇒ **选 4B 的判据 = 形参类型是 `float`**，且本仓多处同址已定 1.0f） |
| `0x1834b31d4` | **0.35** | `DOFade` 与 `DOScale` **共用的时长**（秒） |
| `0x1834b2e84` | **0.75** | **起始缩放系数** |

🔑 **一句话**：三个常量**都是 4 字节 float、值整齐（1.0 / 0.35 / 0.75）、实参位置在 `Open.c` 里逐句可读**
⇒ **④ 算「做得了」**；`AlliancePanelWindow.cs` 文件头那句「要在 `GameAssembly.dll` 里取值」**应改成「值已取到」**。

## 另外两条（顺手）

1. 🔵 **`资料/全量反编译复核_靠推断的清单.md`（202 行）对这四件 + `PlayedThirdCardInTurn`【零覆盖】** —— `grep Alliance|评级|头像|徽章|DOFade|DOScale|ThirdCard` 该文件 **0 命中**（它「最该先复核的 10 条」里没有一条是它们）⇒ ⛔ **别去那里找**。
2. ⚠️ **一处可能的订正**：`BattleDriver.cs:4021-4022` 记「`BattleHud__AlliancePanelOpenButton.c` 取 `+0x10` = **对手的名字**」—— 但字段表里 `+0x10` = **`deckName`**，**称号**读的是 `+0x50 playerTitle`（→ `LocalizableLabel` 解析）⇒ 当「名字」用是**我们自己的口径**，值得留意。

---

# 第二节 · `AbilityTrigger.PlayedThirdCardInTurn`（= **15**）

## 1 · `HasPlayedThirdCardInTurn` 的方法体（`RawCardScript__HasPlayedThirdCardInTurn.c:3-56`）

读 **`this + 0x290`** = `RawCardScript.cardAbilities : List<CardAbility>`（`dump.cs:23339`），逐元素取 **`item + 0x10`**（= `CardAbility.trigger`，`dump.cs:20343`）比 **`== 0xf`** ⇒ 命中返回 1、扫完返回 0。
🔴 **它不读任何「本回合出过几张」的计数器**（**名字骗人**；`任务/T01_HUD缺件.md:179-180` 已就地订正过）。
不命中时那段还会看 `item + 0x50`（= `CardAbility.abilityLogic : List<AbilityLogic>`，`dump.cs:20351`）第 0 颗的 `+0x88` 是不是非 null 的 Unity Object —— 那是 `playIcon` 图标链，**判据仍在那一句 `== 0xf`**。

**调用者只有 2 处**：
1. **`BattleHud__SetOffensiveButtonVisible.c:31`** —— HUD：先判 `+0x70`（那颗钮「选定卡 ≠ 空卡」，`GetEmptyOffensiveCard` 比 id），再
   **`SetActive(BattleHud + 0x78 = offensiveCardTurnCardsHolder, HasPlayedThirdCardInTurn(<选中的那张进攻卡>))`** ⇒ **那三颗 `CardsPlayedInTurn1/2/3` 的闸**。
2. **`BattleManager._ApplyOffensiveAndDefensiveEffects_d__337__MoveNext.c:169`**（state 1）—— 结算那一遍**同一判据** ⇒ `SetActive(+0x78, true)`，紧接 `DestroyCardOffensive(卡, DAT_1834b2bb8 = 1.0)`。
   ⇒ ⛔ **不是「结算时按打过 3 张」**。

## 2 · 这个 trigger 长在什么卡上（**第一权威**）

存的字段 = **`RawCardScript.cardAbilities[].trigger`**（SO JSON 里的 `cardAbilities[].trigger`）。
🔑 **本地有【全字段】的原版卡 SO 快照** = **`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`**（**1195 张 `RawCardScript`**；`cardAbilities` 出现 **1192** 次、`"trigger"` **1310** 次）。

**全表扫 `trigger == 15` ⇒ 只有 2 张卡**：
- **`Z-DA-Asteroid Zone_enchantment`**（`uniqueId db34f054e134c094aaf92e7a6ae124c0`）· `spellType **200 = OffensiveCard**` · `cardType 20 = Tactic` · `cardArmy 110 DarkAngels` · `manaCost -1`
- **`Z-EmperorsChildren-Empyric Rift_enchantment`**（`430c733f7e6de72489e2f0586ccc8d3d`）同上，`cardArmy 120`；**带 2 条** trigger-15 能力

⇒ 🔴 **「它长在进攻卡上」成立，但成立的依据是【上面那张第一权威表】**，⛔ **不是** `offensive_cards.json` 的 0 命中：
那张表的 `_fields` 只有 `forgeLevel/cardRef/cardName/envSO_GUID/envSO_name/prefab_name/face_texture`，**`trigger`/`ability` 两个字面量全文出现 0 次**（39 张 = 13 阵营 × 3）⇒「我们表 0 命中」是**必然**、**零证据力**。
🔴 **两条纠正**：① 它是**逐卡的**（⛔ 不是「进攻卡这一类通配」）② 这两张是 **`_enchantment`（环境效果）版** SO。

**与我们本地的对应**：`MyGame/Assets/CardPresentation/Resources/OffensiveCards.json`
= `Asteroid Zone`（`DarkAngels`，`idx 0 / forgeLevel 4`，`envSO: "Environmental Condition Dark Angels Asteroid Zone"`）· `:138-142` = `Empyric Rift`（`EmperorsChildren`，`idx 2 / forgeLevel 45`）
⇒ **两张都在我们的清单里，只是我们的表没记 `trigger`**。

## 3 · 效果正文在不在本地 ⇒ **在（但只有「逻辑」，没有「文字」）**

- ✅ **`abilityLogic` 是全的**：`cards_1195_2022时代.json` 里那两张卡 —— Asteroid Zone **1 条**、Empyric Rift **2 条**，每条 **`chosenAbility = 10` = `AbilityEffect.doDamage`**
  （词典出处：`d:/2/远端缓存_旧profile_1013/导出/AbilityEffect_枚举词典.tsv` · `普查产出_第二十七会话/abilityLogic_枚举词典.tsv`；映射见 `普查产出_第二十七会话/abilityLogic_词典_报告.md:24,38`），
  各带自己的 **`targetCriteria`**（Asteroid Zone：`friendlyUnits 1` / `enemyUnits 1` / `includeHero,Minions,Remnants 1` / `maxTargets 1`）与 **`cardAnimations`**（`40e7fa2a…` / `4f379f7a…`）。
- ⛔ **卡面文案字符串不在本地**：这两条 SO 的 `cardDescriptionTextReferenceId` / `activeAbilityTextReferenceId` **都是空**，**本地化表在远端**（`全量反编译复核_靠推断的清单.md` 第 5 条已记「词条正文在远端本地化表，本地没有」）。

## 4 · 两个问题的答案

- **什么时候用**：
  ① **HUD** 用它决定 `CardsPlayedInTurnHolder` 那三颗 20×20 **点亮不亮**（`BattleHud__SetOffensiveButtonVisible.c:31`；结算那一遍同判据 `d__337:169`）
     ⇒ **闸门 =「你选的这张进攻卡【自己】带 trigger 15 吗」**；
  ② **结算**：本回合第 3 张牌打出时，在**刚打出的那张牌**上触发它的能力（`RawCardScript__OnCardPlayedWithTarget.c:105-112`）。
- **是不是进攻卡的效果**：**是** —— 第一权威里它**只出现在 2 张 `spellType 200 = OffensiveCard` 的卡上**；
  但要按「**逐卡、且是 `_enchantment` 版**」理解，⛔ **不是「所有进攻卡都带」**。

---

# 三、查不到的（逐条 + 搜过什么）

1. **对手评级 / 联盟徽章的真值数据**：`grep eloRating|allianceRating|allianceName|allianceBadge` 于 `Assets/RuleEngine/` + `Battle/BattleDriver.cs` = **0 命中**；`Shell/SocialData.cs:27-30` **明写恒空** ⇒ 数据源在**服务端 profile + LiveOps**（不只是「没查」）。同理 bot 头像允许清单 `AIBotsConfig` 在 **LiveOps 远端**。
2. **那两张卡的卡面文案（本地化字符串）**：本地化表在**远端**；`cards_1195` 两条 SO 的文本引用字段为空。搜过：`数据/游戏数据/environment_conditions.json`（只有战场视觉参数 `ambientColor/blendTime/…` + `used_by_cards`，**无 trigger、无文案**）· `Assets/RuleEngine/Resources/cards_engine.json`（1127 张，**无 `trigger`/`ability` 字段且根本不含这两张卡**，两名字各 0 命中）· `offensive_cards.json`（39 张，无 trigger/ability 键）· `普查产出_第二十七会话/guid_map.tsv`（4,985 行，列 = `uniqueId/cardName/field/assetGuid/bundle/…`，**没有 trigger 列**）· `普查产出_第三十三会话/`（R9 只讲卡 id 真值 / CCD URL，无 trigger）。
3. **`全量反编译复核_靠推断的清单.md` 对这四件 + `PlayedThirdCardInTurn` 零覆盖** —— 不是「它说了别的」，是**它没提**。
4. **未解的死角**：`Alliance_Trophies_*` 里「哪张图对应哪个 `*_Badge` SO」**我没逐条把 GUID 对到图上**（`AM_Forge_Badge.badgeImage` = `12d7cba3769fd4b138c6553a346f86c7` · `Default_Badge` = `b321812b9e4727547be248926b2bca3a`，而 `guid_map.tsv` 里这两个 GUID **0 命中** —— 那份表只覆盖**卡 SO** 的引用、不含徽章 SO）⇒ **真要建 ③ 时需要另找一份「徽章 GUID → sprite」映射**。

---

# 四、🔴 主对话据此的裁定与就地订正（2026-10-17 · **本节由主对话写**）

1. 🔴 **`T01` 那条「两条都落铁律 11 例外②（判据在远端）」—— 只有【②两处评级】那一半成立，`A1547` 那一半【不成立】**：
   `trigger == 15` 的卡（**2 张**）与其 `abilityLogic`（`doDamage` + `targetCriteria`）**都在本地的 2022 快照里**（`cards_1195_2022时代.json`）
   ⇒ **缺口 = 卡面文案字符串**（在远端本地化表），**而逻辑/触发/目标都是现成的** ⇒ `A1547` 从「等远端」**降级为「可做（文案先用逻辑/出声顶）」**。
2. 🔴 **那「四件缺口」的三条理由要改**（`AlliancePanelWindow.cs` 文件头）：
   · **① 头像**：**资源全在**（还在本仓 `Resources/Art/avatars/` 有 944 文件）、**我们 HUD 已经在画同一套** ⇒ **能做**，⛔ 不是「判据拿不到」；唯一近似 = bot 的**允许清单**在远端。
   · **③ 徽章**：**图和 SO 两层都在本地**；**真原因是「走不到那一支」**（联盟名空 ⇒ `allianceGO` 关 ⇒ `Draw` 不执行）⇒ ⛔ 不是「原版没有」。
   · **④ 出场动画**：**三个常量已读出**（`1.0` / `0.35` / `0.75`）⇒ **能做**，属**欠做**。
   · **② 两处评级**：**这一件才真的归「等远端」**（`eloRating` / `allianceRating` / LiveOps 图标）；⚠️ 但「静态取不到」那句**只是尺寸层面**，⛔ 别当「做不了」。
3. ⚠️ **连带的订正**：`BattleDriver.cs:4021-4022` 把 `+0x10`（= **`deckName`**）当「对手的名字」用 —— **现核字段表如此**，值得留意（**未改**：那要动代码）。
4. 📌 **⛔ 别去 `全量反编译复核_靠推断的清单.md` 找这五件事**（它零覆盖）。
