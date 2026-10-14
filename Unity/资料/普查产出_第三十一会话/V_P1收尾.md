# V · 卡牌线 P1 相之后的收尾（① `.cs` 旧 id · ② `TL40` 进池 · ③ 孤儿卡图 · ④ 两处死数据）

> 2026-10-15 · 执行代理 V。判据 = `资料/普查产出_第三十一会话/M_卡牌P1_id置换.md`（§六/§七 就是本笔清单）
> + `M_卡牌P1_逐张对拍.tsv`（171 行）+ `_tmp_view/p1_1014/{idmap.json,stale_cs_py.tsv}`（P1 生成，**都还在**）。
> ⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未改任何正本。✅ 秒级 `typecheck` 0/0。
> 🔵 跑过 `bash 工具/ruleprobe.sh check`（读数见 §验收）—— **未重钉指纹**。

---

## 一、① `.cs` 里的旧 id 字面量（37 处 / 9 文件）

### 1·A 30 处（8 文件）—— **照单换成新 id**（`_oneoff_1015_p1fix/remap_stale_cs.py`，逐条断言计数）

| 文件 | 行 | 旧 id → 新 id |
|---|---|---|
| `RuleEngine/Editor/RuleEngineTest.cs` | 8501 | `EC_Terminator_Champion` → `EC26` |
| 同上 | 8502 | `EC_Maulerfiend` → `EC30` |
| 同上 | 19113 | `BL_Dark_Pact_of_Fate` → `4a48457c24877f345be2e96f867c1e94` · `BL_Dark_Pact_of_Blood` → `9a2cadc64c80a6b42912441105bb1cee` · `BL_Dark_Pact_of_Excess` → `b8e81b2fe85cda747a68c4d6326b669c` · `BL_Dark_Pact_of_Resilience` → `1dfc9bc738bfc4f4d93d48f165aee510` |
| 同上 | 19137 | `BL_Dark_Pact_of_Fate` → `4a48457c24877f345be2e96f867c1e94` |
| 同上 | 19316 | `ASH_Path_of_the_Seer` → `09bcb1b3b8794c4429d82d4b2e68fa1f` |
| 同上 | 19320 | `EC_Duelist_s_Hubris_Lucius_Talent` → `6ed51d59b9f8ea24ab0dc7ceaef7956c` |
| 同上 | 19674 | `SAU_Master_Chronomancer` → `cbabeb234380634479feab1a1b4c733a` |
| 同上 | 19689 | `AM_Cadian_Honour` → `3a52026b99bb56f40aa9dc130fc66f34` |
| 同上 | 19845 | `BL_Diabolic_Strength` → `7bf8ce67fb6be4744ae44345a5b465fd` · `BL_Infernal_Gaze` → `453b0e8f88e094a17866a6979f389ef5` · `BL_Unhallowed_Gifts` → `141c5ed661a9e42bd9a793f0d2a1c682` |
| `CardPresentation/Editor/DeckScene.cs` | 1227（**一行两处**） | `UM_Smite` → `cf0b346f70e748c4bbfc4dc03ab39ead` |
| 同上 | 1231（**一行两处**） | `EC72` → `5a8a11b2a89796f48b34fbec8db6f452` |
| 同上 | 1237 · 1238 | `DA_Convoke_the_Circle` → `3e99603a7b7b2c54c8f1ad40250ee2ae` |
| 同上 | 4289 · 4297 | `DA_Plasma_Generator` → `DA61` |
| `CardPresentation/Editor/BattleScene.cs` | 14697（**一行两处**） | `GSC_Cult_Propaganda` → `7c666a04c1b9e8b4e9a96247e9fe115a` · `GSC_Improvised_Barricade` → `334a32d196d46ed4387bf3740cce069b` |
| 同上 | 14698（**一行两处**） | `GSC_Jammed_Communications` → `68909cf02f472734ea391665e0d28e15` · `GSC_Poisoned_Supplies` → `a7bb23355a30d344e9d2decf976d91e1` |
| `CardPresentation/Editor/NetSelfTest.cs` | 667 · 2009 | `UM_Master_Tactician` → `dcbd28525888ce54aaa93a47ba4de1e6` |
| `CardPresentation/Shell/CollectionWindow.cs` | 1516 | `DA_Azrael` → `DA1` |
| `CardPresentation/Shell/TutorialModePopup.cs` | 268 | `BL_Sylar_Hexcorn` → `BL36` |
| `RuleEngine/Editor/RuleEngineTest_S14.cs` | 1555 | `SOR_Righteous_Repugnance` → `f104d4c21abe84546abc9365d9eda8dd` |
| `RuleEngine/Editor/RuleEngineTest_S23_Parse.cs` | 217 | `GSC_Jammed_Communications` → `68909cf02f472734ea391665e0d28e15` |

**现核**：24 个不重复的新 id **逐个都在池里**（`cards_engine.json` 按 id 查，卡名逐一对上 —— 例 `cf0b34…`=Smite · `5a8a11…`=Antrak Silk · `DA61`=Plasma Generator）。行尾保持原样（`RuleEngineTest.cs`/`DeckScene.cs`/`BattleScene.cs` 是 CRLF，其余 LF），`git diff --numstat` 只动了那 30 行。

### 1·B 🔴 7 处（`CampaignData.cs`）—— **前提是错的，所以我没写卡 id；那 35 行是【被 601005b 盲替换改坏的原版 SO 原文】，已还原**

**这不是「换成新 id」，是「还原成原版原文」。** 判据三重：

1. **文件自己的表头**（`CampaignData.cs:4-8`）写着：「**照原版（本地可读）**：Ultramarines 那一套 47 个节点的
   `NodeId` / 坐标 `(x,y)` / `Cost` / `IsRepeatable` / 后继图 —— 出处 = `bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json`
   ……**下表是脚本从 SO 直接抄出来的，不是手抄**」；
   `Rewards` 表同源（`:340-342`，「源 = … 的 `Rewards[]`」）。
2. **现读 47 个 SO 逐行核**：`Nodes[i].Id` 该是**原版 `NodeId`**（`UM0`–`UM47`，缺 `UM14`），
   `Rewards[i][k].Id` 该是**原版 `Rewards[].item.targetId`**（= 原版 `allcards` 的 `uniqueId`，
   短 id 与 32 位 GUID 两种形态都有，例 `643ee10a8aeb99a4990e3bb9a2a0faf8`）。
   `git show 601005b^:…/CampaignData.cs` 与 47 个 SO **逐行吻合（0 处不符）**；而 `601005b`（第二十六会话，
   「整仓扫『写死的旧 id』：14 个 .cs / 93 处」）**盲替换**了这张表 —— 原版 `NodeId "UM1"` 被当成「我们的旧卡 id `UM1`」
   换成了 `"UM46"`，**26 行 NodeId + 9 行 reward targetId 共 35 行一起被改坏**（`git show 601005b --numstat` = **35/35**）。
3. **第二个独立来源**：原版 `allcards`（`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`）证明
   SO 的 `targetId`（`UM2`/`UM34`/`UM13`…）**就是原版卡的 `uniqueId`**，也正是我们卡池现在的 id
   （`UM2`=Octavio Infiltrator · `UM34`=Point-Blank Shot · `UM13`=Primaris Chaplain …）。
   ⇒ 还原文案后 reward 解析出来的卡**与原来那张对得上**，而照 ① 字面改成 GUID 会把 `NodeId` 列变成 GUID、制造**新**缺陷。

**改法**：`_oneoff_1015_p1fix/restore_campaign_types.py`，按行号 + 内容双断言，**35 行**贴回 SO 原文
（`git diff --numstat` = 35/35；行尾 LF 未动）。**现核：47 行 NodeSpec + 47 行 Rewards 与 47 个 SO 逐字段比对 0 处不符。**

| 行 | 列 | 改前（601005b 的坏值） | 改后（= SO 原文） |
|---|---|---|---|
| 70 | NodeSpec | `UM46` | `UM1` |
| 71 | NodeSpec | `UM_Master_Tactician` | `UM2` |
| 72 | NodeSpec | `UM47` | `UM3` |
| 73 | NodeSpec | `UM_Master_of_the_Fleet` | `UM4` |
| 74 | NodeSpec | `UM48` | `UM5` |
| 76 | NodeSpec | `UM_Might_of_Heroes` | `UM7` |
| 78 | NodeSpec | `UM_Smite` | `UM9` |
| 79 | NodeSpec | `UM2` | `UM10` |
| 80 | NodeSpec | `UM1` | `UM11` |
| 81 | NodeSpec | `UM4` | `UM12` |
| 82 | NodeSpec | `UM3` | `UM13` |
| 84 | NodeSpec | `UM11` | `UM16` |
| 85 | NodeSpec | `UM8` | `UM17` |
| 86 | NodeSpec | `UM56` | `UM18` |
| 91 | NodeSpec | `UM_Spear_of_Macragge` | `UM23` |
| 96 | NodeSpec | `UM16` | `UM28` |
| 98 | NodeSpec | `UM15` | `UM30` |
| 100 | NodeSpec | `UM21` | `UM32` |
| 102 | NodeSpec | `UM23` | `UM34` |
| 103 | NodeSpec | `UM18` | `UM35` |
| 107 | NodeSpec | `UM25` | `UM39` |
| 108 | NodeSpec | `UM26` | `UM40` |
| 109 | NodeSpec | `UM59` | `UM41` |
| 112 | NodeSpec | `UM29` | `UM44` |
| 113 | NodeSpec | `UM_Greatest_Deeds` | `UM45` |
| 115 | NodeSpec | `UM32` | `UM47` |
| 352 | RewardSpec | `UM23` | `UM34` |
| 353 | RewardSpec | `UM_Master_Tactician` | `UM2` |
| 354 | RewardSpec | `UM47` | `UM3` |
| 359 | RewardSpec | `UM25` | `UM39` |
| 364 | RewardSpec | `UM3` | `UM13` |
| 365 | RewardSpec | `UM8` | `UM17` |
| 370 | RewardSpec | `UM29` | `UM44` |
| 376 | RewardSpec | `UM16` | `UM28` |
| 381 | RewardSpec | `UM56` | `UM18` |

**影响面（现核，确认无回归）**：`NodeSpec.Id` 全仓只有 `CampaignData.NodeName(i)`（`CampaignData.cs:272`）读，
而它只被 `CampaignTab.cs:823/908/912` 的 `Debug.Log` 用；`CampaignData.IndexOf(nodeId)`（`:120`）**全仓 0 调用**；
`RewardsScene` 的节点断言走的是**下标** `CampaignNode_<i>`（`RewardsScene.cs:5130/10876`），**不读 `Id`**。
⇒ 两张表的值既不进引擎也不进断言，改回原版不会动任何自检结论（**但这正是它该长成的样子**）。

### 1·C ⚠️ 没做的一件（**要主控裁**）：文案里还写着旧 id

**25 处**旧 id 出现在**注释 / 断言消息**里（**去引号**才数得到，本轮只换「字面量」故未动）：
`RuleEngineTest.cs` 11 · `DeckScene.cs` 6 · `BattleScene.cs` 6 · `RuleEngineTest_S14.cs` 1 · `RuleEngineTest_S23_Parse.cs` 1。
最典型的一条：`DeckScene.cs:1228-1232` 代码已用 GUID、而紧邻的注释与断言文案仍写「`` `EC72` Antrak Silk ``」。
⇒ 属 `A1777` 那一族（**断言文案点名错卡、照样绿**），**改不改请裁**（改了也不影响绿红）。

---

## 二、② `TL40 Infestation Node` 进池

### 2·A 🔴 关键现核：**源表里早就有这一行**，它是被「无数值」滤掉的，不是「没有」

`card_stats.json` 里那条 `Infestation Node`（Leviathan / `factionId 60` / `type tactic` /
`art …/Tyranid_Leviathan_strat_Infestation Node.png` / `decks ["LEV_InfestationNode","LEV_InfestationNode"]`），
`cost/desc/rarity/subtype` **全 null** + **`hasStats: false`** ⇒ 在 `gen_cards_engine.py:1016`
（`if not c.get("hasStats") and name not in STATS_EXCEPTIONS: continue`）被**整条跳过**。

⇒ 正确落点是本表**既定的那个白名单** `STATS_EXCEPTIONS`（`gen_cards_engine.py:401` 起那段注释 · 字典在 `:414`（本笔加在 `:447`），
注释写着「`hasStats=false`（OCR 没读到数值）里**确证是真卡**的少数几张 —— 补上费用后照常收」），
**不是**往源表插一行（P1 报告 §六·1 写的是「插一行」，那样会多出一条重复行）。
本笔按白名单做 ⇒ **源表 `card_stats.json` 一个字没动**。

### 2·B 收录依据（五条独立证据，逐条给出处）

| # | 证据 | 出处 |
|---|---|---|
| ① | `uniqueId: TL40` · `cardName "Infestation Node"` · `m_Name "T7 Leviathan-Infestation Node"` · `manaCost 2` · **`cardType 20`**（=`CardTypeOptions.Tactic`）· **`cardRarity 3`**（=`CardRarity.Epic`）· `cardImageReference.m_SubObjectName "Tyranid_Leviathan_strat_Infestation Node"` | `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（原版 `allcards`）|
| ② | `Card_Name/TL40` = (`Infestation Node`, `感染节点`) · `Card_Description/TL40` = (`Heal 2 to a friendly troop and create a copy of it in your hand`, 中文齐) | `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` |
| ③ | 原版抽包 SO 的 `cardIds` 里有 `TL40`，且 `packId = LEV_InfestationNode`（与源表那条 `decks` 值自洽） | `bundle_draftpacks_assets_all/MonoBehaviour/Infestation Node.json` |
| ④ | 立绘本地有 | `bundle_tyranidsleviathancardassets_assets_all/Texture2D/Tyranid_Leviathan_strat_Infestation Node.png` |
| ⑤ | 我们有它的中文名（`zh_cards.json` → 「侵染节点」），真噪音那 74 张只有「正常条件」这种通用词 | `Unity/数据/卡牌翻译/zh_cards.json` |

**id 怎么拿到 `TL40`**：真值索引那一层已命中（`card_id_truth_index.json` 的
`by_official_en/by_tail/by_name['infestationnode'] = ['TL40']`，`army['TL40'] = 60`）⇒ `pick_truth_id` 直取；
`card_ids.json` 本来也有 `TL40 → Infestation Node`（**无需动它**）。

### 2·C ⚠️ **`subtype` 没给（判据不在本地）—— 卡面那行橙字暂时不画**

`Infestation Node` **不在 PnP 成品卡图那一套里**（`d:/2/Warpforge部队卡片/` 全树无 `Infestation`，
只有 GSC 的 `Rampant-Infestation`）⇒ 铁律 7 那条路走不通。其余三处也都查过、**都没有**：
真值 `CardTraits`/`ShowTraits` 都空 · `cardRaceTextReferenceId` 全库 1194 行**全是空串** ·
`_stratagem` / `spellType` 与 subtype 无对应（实测同一 `spellType=0` 下 subtype 有 `Spell`/`Tactic`/`Ability`/…）；
**反例排除**：「立绘名含 `_strat_`」也推不出 subtype（`Spell` 245 / `Tactic` 57 / `Ability` 22 / … 混在一起）。
⇒ 按铁律 2 **宁可留空 + 出声，不猜**（池里另有 10 张 subtype 为空，是既有状态）。
**这一格要补只有一条路：拿到该卡的卡面（远端/实机）**。

### 2·D 落地 = `gen_cards_engine.py` 的 `STATS_EXCEPTIONS` 加一条（+带出处的长注释）

```python
"Infestation Node": {"cost": 2, "rarity": "epic",
                     "desc": "Heal 2 to a friendly troop and create a copy of it in your hand",
                     "hasStats": True},
```
`desc` = **官方英文原样**（我方没有该卡 PnP 卡面可抄）；`rarity` 由真值 `cardRarity 3 → epic`
（映射实测：`(3,'epic')` 138 例为多数派）—— 宝石表（`卡牌宝石稀有度_0824.md`）**不收录这张**，
代码在「宝石表查不到时保留源表合法值」那一支（`gen_cards_engine.py:1093`）照常取 `epic`。
`descZh` 走**手维护**通道 `cardface_fixes.json` 的 `descZh` 列（`_note` 写明该列手工维护；
代码 `:1218-1220` 把它放在中文表**之后**应用）—— 值 = **官方中译原样**
「对一支友方军队治疗 2 点生命值，并在你的手中创建一个副本。」⛔ 没写 `zh_cards.json`（那份 `note` 写着「不要手改」）。

### 2·E 下游链 —— **全部重跑**（P1 那条**含不动点**的链，顺序照它报告 §四）

| # | 生成器 | rc | 读数 |
|---|---|---|---|
| 1 | `gen_cards_engine.py` | 0 | 卡池 **1126 → 1127** · 唯一 id 1127 · 0 重 id · 自造 23（全 UM）|
| 2 | `gen_card_inventory_options.py` | 0 | `map 1008 → **1009** · unknown 118`（TL40 进 map）|
| 3 | `gen_card_initiative.py` | 0 | `1127` 张 · 119 → **118** 张 `-1`（Leviathan 那 1 张收回）|
| 4 | `gen_cards_engine.py`（**不动点第二趟**）| 0 | inv **1009/1127** · initiative **1009/1127** · 未知 118（SpaceWolves 68 / Ultramarines 38 / DarkAngels 12）· 中文效果 **1120 → 1121**（TL40 的 descZh 进来）|
| 5 | `import_original_art.py` | 0 | 插图 **1127/1127 配上 · 配不上 0**（`art_tl40.png` 已落盘，2.1 MB）|
| 6 | `import_original_audio.py` | 0 | 「**已拷 1 条** / 已存在跳过 1848 条」⚠️ P1 那趟是 0 条 —— 见 §五·3 |
| 7 | `gen_icon_plan.py --write` | 0 | 两份 `card_icon_plan.json` 落盘 · 位置锁 12 不画/2 必画全过 · **灭自证五条控制臂全部变红** |
| 8 | `gen_card_vfx_by_card.py` · `gen_prebuilt_decks.py` | 0 | 两份 `card_vfx_by_card.json` · 预组 **103 副 · `complete` 86 → 87**（见 §二·F）|
| 9 | `gen_related_cards.py` → `gen_talent_cards.py` | 0 | 两份各 2 份（`related_cards.json`×2 · `talent_cards.json`×2）· talent `mapped 96/97 · dup 7`（与 P1 同）|
| 10 | `gen_ability_anim_durations.py` · `gen_tutorial_stages.py` | 0 | 两份各 2 份（读数与 P1 同）|

**不动的**：`card_stats.json`（源表，一个字没动）· 指纹三常量（**没重钉**）· `cardface_fixes.json` 除 `descZh` 那一行外未动。

### 2·F ⚠️ 副作用（**与 P1 §六·4 同类，要主控看一眼**）：预组「完整」数又 +1

`prebuilt_decks.json` 的 `complete` **86 → 87** —— 新完整的这一副是 **`LeviathanDeck4`**（它引用了 `TL40`；
P1 那批已让 `AM_SK_1` · `AstraMilitarumDeck1` 变得完整，84 → 86）⇒ 预组页会**多列 1 副**。这是 id/池对齐的**自然收益**，不是缺陷。

---

## 三、③ 171 个孤儿卡图 ⇒ **保留（不删）**

- **裁定（写进本报告正本）**：**保留，不删。** 它们是 `Resources/Art/cards/art_<旧id>.png`（+.meta），
  全部读取**按 id 键查**（`CardArt.Portrait(key)` = `Resources.Load("Art/cards/art_" + Slug(key))`，
  `CardArt.cs:359-362`）⇒ **孤儿不可能被读到**；目录本身在 `.gitignore` 里（不进仓）。
- **🔴 顺手核的那一件事（会不会有人 glob / 列目录读它）—— 结论：不推翻裁定，但确实有一处列目录读法**：

| 读者 | 读法 | 会不会读到死文件 |
|---|---|---|
| C# 运行时 | `Resources.Load`（按键）—— 全仓 `Resources.LoadAll<Texture2D>` 只有两处，落在 **`Art/traits`**（`Badges.cs:274`）与 **`Art/cardbacks`**（`CardArt.cs:676`），**没有一处扫 `Art/cards`** | ❌ 不会 |
| `工具/check_pnp_cards.py:275` | ✅ **真的 `os.listdir` 了 `Art/cards`**：`art_files = set(f for f in os.listdir(ART) if f.endswith(".png"))` | ❌ 不会 —— 它只做**成员判定**（`if want not in art_files`，`want = art_<池内 id>.png`）⇒ 集合里多出来的孤儿**只可能让判定更宽松不了**（池内 id 与孤儿 id 不相交）|
| `工具/_oneoff_1014_p1/check_orphans.py` | 故意列出来数 | —— |

  ⇒ **「保留」这个裁定成立**。（若哪天真要清，判据已现成：`check_orphans.py`。）
- **附一条观察（非正确性问题）**：`Resources/` 下的**每个文件都会进 player 构建**，不管有没有被 `Load` ⇒
  这 197 个孤儿是**白带的构建体积**。**要不要顺手清，请主控裁**（清理=破坏性操作）。
- **顺手订正一处数字**：盘上孤儿**不止 171 个，是 197 个** = P1 那 171 + **26 个**更早的
  （按**卡名**命名的自设计立绘：`art_abyss_titan.png` · `art_ember_archer.png` · `art_tide_warlord.png` ·
  `art_deep_hunter.png` · `art_war_drake.png` … —— 2026-09-15 改用按 id 命名之后被落下的；
  P1 §五 估的是「40+」，**实测 26**）。

---

## 四、④ 两处要核的数据

### 4·A `card_ids.json` 里已死的 `EC72`–`EC77` · `TL81`（7 行）⇒ **零引用，已清**

**判据（三条，全部现核）**：
1. **原版预组零引用**：`Unity/数据/游戏数据/decklists.json` 的 **236 副**逐 id 现核
   （`cardIds[]` ∪ `heroId` 的全集），**7 个一个都不在**；
2. **原版 SO 零引用**：`d:/2/解包整理/01_卡牌/卡组数据/MonoBehaviour/` 整目录 `grep` 这 7 个 id —— **一个文件都没有**；
3. **我们数据树零引用**：`Unity/数据/` 整树 `grep`，只有 `card_ids.json` **自己**提到它们。
   （⚠️ 唯一「引用」是 `CardPresentation/Editor/DeckScene.cs` 的**注释与断言文案**里那 2 处 `EC72` —— 那是散文、不是数据引用，已见 §一·C。）

**落地**（`_oneoff_1015_p1fix/clean_dead_cardids.py`，按行断言、纯 CRLF 保持）：
`mapping` **1317 → 1310 行**（933 真值短 id + 259 GUID + **123 → 116** 旧表残留 + 2 `#HB_`）；
`note` 里两处数字就地改并追加带日期的更正段（铁律 5）。
**桥没断**：那 7 张卡的**真值 GUID 行都还在**（`5a8a11b2…`=Antrak Silk · `70199b6f…`=Heliotrophos · `6e6d1400…`=Quail ·
`92157999…`=Shivversplint · `4eec3b13…`=Skorflense · `aa45079b…`=Xylocil · `468db99b…`=Feeding Frenzy）。
**回归核**：删完重跑 `gen_cards_engine.py` ⇒ 卡池 **1127 张 / id 集合与删除前逐张相同（差集为空）** ✅
（那 7 张仍走真值索引拿 GUID）。**留着反而有害**：`load_ids()` 会为这些卡名建出 `[旧 id, GUID]` 两条候选，
而 `pick_id()` 只认「前缀+纯数字」⇒ 真值索引一旦失效，它会**静默退回**这 7 个**已非池内 id** 的旧值。

### 4·B `warlord_ids.json` 的 4 个值（`DA_Azrael` / `DA_Belial` / `DA_Asmodai` / `TL_Neurogaunt`）⇒ **不是「旧 id」，不换**

- 这个文件的语义是**「督军 id → 督军名」的自建名表**（`gen_prebuilt_decks.py:29` 头注；
  57 行；**全仓 C# 0 引用**，只有 `gen_prebuilt_decks.py` / `check_prebuilt_decks.py` 两个 python 读它）。
  值统一是 `<阵营前缀>_<角色/名>` 的**名字**，与我们的旧卡 id **同形但不同物**：
  `SW_Logan Grimnar` / `BL_Warlord_Haarken` / `UM_Warlord_Varro Tigurius` / `AM_Hektor Thenmann` / `GSC_Warlord_Iconward` …
  （带空格的**根本不可能是**我们的卡 id）；那 4 个只是**恰好没有空格**而已。
- **消费者实际怎么用它（现读代码）**：`gen_prebuilt_decks.py:258-283` 的 `hero_match()` ——
  **先按 id 直查池**（`by_id.get(hid)`），查不到才回落名字表，且回落时先 `re.sub(r"^[A-Za-z]{2,3}_(Warlord_|Presale_)?", "", v)`
  **剥掉前缀**再按名字匹配 ⇒ `DA_Azrael → "Azrael"`、`DA_Belial → "Belial"`、`DA_Asmodai → "Asmodai"` **都能对上**（现核池里 `DA1`/`DA2`/`DA3` 正是这三张督军）。
- **唯一的真错行 = `TL74 → TL_Neurogaunt`**，而且它**早已被记录在案、且代码已加闸**：
  `gen_prebuilt_decks.py:261-264` 的 docstring 原话「2026-09-25 实测：`warlord_ids.json` 那张自建名表**有错**
  （`TL74` 写成 `TL_Neurogaunt`，而卡池里 `TL74` 就是督军 `Terror of Vardenghast`），拿它当第一判据会把能用的牌误判成不能用」
  —— 现核池里 `TL74 = Terror of Vardenghast`、`TL66 = Neurogaunt` ✗ 与值名对不上。
  ⇒ **如实报**（属 `A876` 那一族「两张自建表互相矛盾」），**本轮不改**（本笔任务只核那 4 个「值」；
  改键 = 动一条已知有错的表、且要连带核 `card_ids.json` 那些冲突行，超出本笔）。

---

## 五、验收读数

| 验法 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_p1fix bash 工具/typecheck.sh` | ✅ **运行时错误数 0 · 编辑器错误数 0**（① 改完一次、②③④ 收口后一次）|
| 卡池 | **`count 1127` · 唯一 id 1127 · 0 重 id · 自造 23（全 `UM_`）** · 文件 463,192 B · **md5 `9d86c7ffaf1a78c32df0d4cb9035d6e6`** |
| `bash 工具/ruleprobe.sh check`（⛔ 未重钉指纹）| rc=1 · **`新增 172 · 消失 171 · 改名 0 行`**（P1 那趟 171/171 ⇒ **多出来的那 1 = `TL40`**）· 解析差异 **1 行 = `GSC29`**（`A1989` 那笔，**与本笔无关**，本笔没加出新的解析差异）· 卡池 md5 与基线指纹仍不同（**三个指纹待主控统一重钉，我没碰**）· 5 条哨兵里 4 条 ✓，1 条✗ 是 `[输出指纹]`（同一件「待确认」事）|
| 全池旧 id 字面量复扫 | `.cs` **0 处**；余 26 处见 §五·1 |
| `CampaignData` 与 47 个 SO 逐字段比 | **NodeSpec 47/47 · Rewards 47/47 · 0 处不符** |
| 24 个新 id 逐个查池 | **24/24 在池里、卡名对上** |
| 卡池 id 集合（删 7 行前后）| **逐张相同，差集为空** |

### 五·1 复扫后**故意留着**的 26 处（逐条给理由）

| 位置 | 计数 | 为什么留着 |
|---|---|---|
| `工具/_fix_a6_descZh.py` 10 · `_fix_a7_talent.py` 2 · `_verify_fixes.py` 2 | 14 | **一次性历史脚本**（P1 §六·3 已明确排除在本批白名单外）；它们记录的是「当时怎么改的」 |
| `数据/游戏数据/card_stats.json` · `cards.json` · `decks.json` · `数据/索引/card_index.json` 各 2 | 8 | **假阳性**：`GSC_Backstab` 在这 4 份里是**卡组 id**（`decks.json` 的 `deckId` / `card_stats.json` 卡行的 `decks` 列），不是卡 id |
| `数据/游戏数据/warlord_ids.json` 4 | 4 | 见 §四·B：**是督军名表条目，不是 id** |
| `数据/游戏数据/card_ids.json` 7（`EC72..EC77`/`TL81`）| 0 | ✅ **已清**（§四·A）|

### 五·2 本笔改了哪些文件

| 类别 | 文件（相对 `HEAD` 的 numstat）|
|---|---|
| ① 代码（30 处字面量）| `RuleEngine/Editor/RuleEngineTest.cs`（284/23）· `RuleEngineTest_S14.cs`（40/13）· `RuleEngineTest_S23_Parse.cs`（1/1）· `CardPresentation/Editor/DeckScene.cs`（158/6）· `BattleScene.cs`（2/2）· `NetSelfTest.cs`（2/2）· `CardPresentation/Shell/CollectionWindow.cs`（1/1）· `TutorialModePopup.cs`（1/1）⚠️ **这些文件在会话开始时就是 dirty（别的写手留下的未提交改动），numstat 是「相对 HEAD 的总量」，不是本笔的量；本笔只动了上表那 30 行** |
| ① `CampaignData.cs`（还原 35 行）| `CardPresentation/Shell/CampaignData.cs`（**35/35**）|
| ② 生成器 | `工具/gen_cards_engine.py`（`STATS_EXCEPTIONS` +1 条，带长注释）|
| ② 数据 | `数据/游戏数据/cardface_fixes.json`（`descZh` +1 条 + 1 段日期说明；22/1）· 重跑的产物见 §二·E |
| ④ 数据 | `数据/游戏数据/card_ids.json`（1317 → 1310 行；262/10）|
| 一次性脚本 | `工具/_oneoff_1015_p1fix/`：`remap_stale_cs.py` · `restore_campaign_types.py` · `verify_campaign_vs_so.py` · `scan_stale.py` · `add_tl40_descZh.py` · `fix_descZh_placement.py` · `clean_dead_cardids.py` |

### 五·3 没查清的部分（⛔ 没猜）

1. **`TL40` 的 `subtype`** —— 判据不在本地（§二·C）；卡面那行橙字**暂时不画**，**卡池里它是空串**。
   要补只有一条路：拿到该卡卡面（远端/实机）。
2. **`import_original_audio.py` 报「已拷 1 条」是哪一条 —— 没定位出来。**
   P1 那趟是 0 条；文件落在 gitignore 的 `Resources/Art/audio/` 下，而导入器用 `shutil.copy2`（**会保留源 mtime**）
   ⇒ 按 mtime 找不到。**没查到 ≠ 出事**：`vo/` 下 1848 条已存在、参考表格没多出条目（`voice_lines.json` 里**没有** `TL40`），
   疑似 = 某张卡（如 `AM3 Commissar Elan`，P1 那批改过名/换过 id）的 VO 文件名跟着变、这一次补拷。
   ⚠️ 只影响**不进仓**的 `Art/`，**没动任何 `Resources/*.json` 的语义**。
3. **`TL74 → TL_Neurogaunt` 那行**要不要连带订正（§四·B）—— 已记录在案、消费者已加闸，**本笔按任务边界没改**。
4. **§一·C 那 25 处文案**（注释/断言消息里的旧 id）—— 属 `A1777`，**改不改请裁**。
5. **197 个孤儿**要不要顺手清（§三）—— 破坏性操作，**请裁**。

---

## 六、顺手发现（⛔ 只报未改）

1. 🔴 **`CampaignData.cs` 那张表被 601005b 的盲替换改坏了 35 行，而且**——**它只被 `Debug.Log` 和
   一个 0 调用点的 `IndexOf` 读**，所以**四个月里没有任何自检会红**。这类「**照原版抄的表被批量替换打穿、
   而它恰好没有消费者**」是 `A1777` 的同族（**断言/数据写着错的照样绿**）。**建议**：给这张表配一条
   「与 SO 逐行比」的廉价断言（本笔的 `verify_campaign_vs_so.py` 已经是那个判据，**跑 2 秒**）。
2. ⚠️ **`card_stats.json` 里 `hasStats=false` 的白名单机制值得再扫一遍**：本笔证明
   **「卡池里没有某张卡」≠「源表里没有这一行」** —— `TL40` 的行带着立绘路径、抽包引用、双份 `decks` 值，
   **静静躺了四个月**。P1 报告写的是「要往源表插一行」，**方向反了**。建议按同一法子重扫那 81 条
   `hasStats=false`（真噪音 74 张里有几张其实是「有立绘 + 有 decks + 有真值」的真卡）。
3. ⚠️ **`gen_prebuilt_decks.py:635` 那批「缺 1 种」的告警**（`ORK_SK_2`/`OrksDeck2`/`SAU_SK_1`… 与本笔无关）
   + `SW_SK_1/SW_SK_2` 的「督军 id 指向非督军」—— **本笔没碰**，只顺手扫到。
4. ⚠️ **`cardface_fixes.json` 是 CRLF（CRLF==LF==1906）**，而 `card_ids.json` 也是 CRLF（1315==1315）；
   两份「新写/删除行」的操作都按 CRLF 保持（本笔第一次给 `cardface_fixes.json` 插说明段时**插错位置**
   —— 插到根对象**外面**去了，JSON 立刻 `Extra data` 报错；已用 `fix_descZh_placement.py` 挪进对象内并复验解析通过）。
   ⇒ **记一笔做法**：往 JSON 里插**顶层说明键**必须插在 `{` **之后**，且**写完要 `json.load` 复验**（光看 diff 看不出来）。
5. ⚠️ **`tools/ruleprobe.sh check` 的「新增 172 / 消失 171」里那 171 个「消失」是 P1 置换的名字面**，
   加一张卡就是 **+1 新增**（不会 +1 消失）——**将来看到「新增 N / 消失 M」不等是正常的**，
   别当成「加卡把别人挤掉了」。
