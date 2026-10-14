# 卡牌 id 换真值 —— 落地记录（2026-10-13 · 第二十六会话）

> **一句话**：全池 1126 张卡的 `id` 从「2026-08-17 按 PnP 编号自建的**猜测值**」换成**原版卡 SO 的 `uniqueId` 真值**。
> **账号** `A1758`（`资料/任务_卡组卡牌战斗.md` §四）· **真值来源** `d:/2/远端缓存_旧profile_1013/`

---

## 一、为什么换（判据）

| 项 | 内容 |
|---|---|
| **旧表** | `数据/游戏数据/card_ids.json` 的 `note` **自陈**：「2026-08-17 **自建**：**PnP 卡表编号 = 游戏卡 ID 偏移 0**」——**前提是猜的** |
| **实测差异** | 与真值逐条比：两边都有的 873 条里 **851 条不符**（id 整体是**置换**，不是个别错） |
| **端到端反证 ①** | 原版牌组 `ASH_SK_2`「**Hosts of the Dead**」（幽灵军）：**真值**下解出 Wraithblade / Wraithguard / Vengeful Wraithblade / Spiritseer（**全是幽灵**、与牌组名自洽）；**旧表**下解出 Shroud Runner / Dire Avenger / Howling Banshee（**一个都不是**） |
| **端到端反证 ②** | 原版牌组 `DA_SK_1`「**Encircle the Foe**」：真值 → Black Knight / Ravenwing Champion / Ravenwing Ancient（**鸦翼**）；旧表 → Reiver / Company Veteran（无关） |
| **判据强度** | 真值直接来自**原版卡 SO 资产**（`RawCardScript.uniqueId`），**不是二手转录** |

## 二、真值是什么、有什么限制

- **内容**：原版 `allcards` 包里的 **1,195 个 `RawCardScript`**（卡 SO），**全字段**已导出 → `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（38.6 MB）。
- **年代**：Unity **2022.3.38f1** / 缓存时间 **2025-07-30** ⇒ 与当前客户端（Unity 6 / `6000.2.6f2`）**不是一个时代**。
- ⚠️ **限制 ①**：**1,195 张 vs 当前卡池 1,126 张** —— 真值里**没有** 266 张后来新增的卡（SpaceWolves 68 / Ultramarines 51 / DarkAngels 26 居多）⇒ 这些卡**仍走旧 id 或自造 id**。
- ⚠️ **限制 ②**：真值里约 **260 条 `uniqueId` 是 32 位 hex**（GUID 形态，全是**效果/附魔类** SO）—— **未收进 `card_ids.json`**（`gen_cards_engine.pick_id` 只认「前缀 + 纯数字」，收了也用不上）。

## 三、落地链（每一步都实跑过）

| # | 做什么 | 读数 / 结果 |
|---|---|---|
| 0 | **备份** | `_tmp_view/cardid_bak_1013/`（`card_ids.json` / `cards_engine.json` / `card_stats.json` / `card_stats_raw.json`） |
| 1 | **重建 `card_ids.json` 的 `mapping`** | 真值 **935** 条 + 旧表里真值没有的 **123** 条原样保留（标注未核）⇒ **mapping 1058 条**；被推翻的 **851 条全部替换**。`note` 重写：写清来源、年代限制、为什么换 |
| 2 | **重跑 `工具/gen_cards_engine.py`** | `cards_engine.json` = **1126 张 / 1126 个唯一 id / 0 重 id**；**真值 id 939 · 自造 187**（旧状态 995/131，**但旧那 995 里 851 张是错的**） |
| 3 | **重出卡图**（文件名 = 卡 id） | 重跑 `工具/import_original_art.py`（干跑：插图 **1126/1126 配得上 · 0 配不上 · 0 缺 rect**）⇒ 落盘后现核 **缺图 0** |
| 4 | **换算 `ART_ID_ALIAS`**（按 id 点名的 24 条贴图表） | **22 个键**按「旧 id → 同名同阵营新 id」换算，**值一字未动** |
| 5 | **重跑派生生成器** | `gen_card_vfx_by_card.py` ✅（465 条，id 已新）· `gen_icon_plan.py --write` ✅（873 条，id 已新；**位置锁全过**）· `gen_prebuilt_decks.py` ✅（103 副）· `import_original_audio.py` ✅（`voice_lines.json` 606 张，**样例 `AM1` 已从 "Commissar Elan" 变成真值 "Lord Solar Leontus"**） |
| 6 | **换算工具里写死的旧 id** | `gen_icon_plan.py` **302 处**（整文件一次改）· `gen_tutorial_stages.py` 2 处 · `gen_prebuilt_decks.py` 的 `TUT_HERO_BY_SO`（`ASH5 → ASH3`） |
| 7 | **换算代码里写死的旧 id** | **14 个 `.cs` / 93 处**（`CampaignData.cs` 26 · `RuleEngineTest.cs` 18 · `RuleEngineTest_S14.cs` 11 · `IconSetup.cs` 8 · `CollectionWindow.cs` 6 · `DeckScene.cs` 6 · `TutorialModePopup.cs` 6 · `BattleScene.cs` 3 · `DeckLibrary.cs` 3 · `NetSelfTest.cs` 2 · `ChoosePanel.cs` / `CardIcons.cs` / `CardBaseDemo.cs` / `MainMenuScene.cs` 各 1） |

**卡池核对脚本**（要复核时照这个跑）：`re.findall(r'"([A-Za-z][A-Za-z0-9_]{1,40})"', 源码) ∩ 旧 id 集`；
**替换时左右引号都要回吐**（我第一版只回吐左引号 ⇒ 302 处全坏成语法错，靠 `git checkout` 还原重来）。

## 四、连带影响（都已处理）

- ✅ **预组牌 / 教程牌**：`decklists.json` 用原版 id 命名空间，靠 `card_ids.json` 当桥 ⇒ **桥一换，预组牌就对了**（`A897` 的核心问题解决）。
- ✅ **卡图 / 语音 / 图标计划 / 卡→特效表**：四件都按新 id 重出（见 §三·3/5）。
- ✅ **断言**：`RuleEngineTest.Run` 第一趟红 **14 条**，**全部**是「断言里写死旧 id」⇒ 改完重跑见 §六。

## 五、遗留（新会话要接）

1. ⚠️ **266 张卡没有真值**（2025-07 快照之后新增）—— 若将来能拿到**更晚的**远端内容（`allcards` 的新版本），这批还能再修一轮。
2. ⚠️ **该快照里 260 条 GUID 型 `uniqueId`**（效果/附魔类）**没进表** —— 要不要按 GUID 收，需要先判「游戏内部是否拿 GUID 当卡 id」（**没查到判据**）。
3. 🔴 **`localization`（I2 词条表）没解开** —— 它是这批远端包里唯一的另一件，**9.4 MB 疑似加密**；解开能清掉一整族账（见交接 §二·2）。
4. 🔴 **还没消费的真值字段**（`cards_1195_2022时代.json` 里现成）：`cardImageReference` 1,171 · `summonAudio` 537 / `attackAudio` 59 / `deathAudio` 61 · `customRangedAttackParticles` 484 / `customDeathParticles` 72 / `remnantBodyPrefab` 73 / `whileInPlayAnim` 28 · `cardAbilities` 1,066 · `CardTraits` 650 · `goSecondCardInHand` 55。

## 六、复核方式（新会话照着做）

```bash
# ① 卡池自身：1126 张 / 唯一 id / 无重
python -I -X utf8 -c "import json,collections;d=json.load(open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'));i=[c['id'] for c in d['cards']];print(len(i),len(set(i)),[k for k,v in collections.Counter(i).items() if v>1])"

# ② 卡图逐张有图（应为 0）
python -I -X utf8 -c "import json,os;D=r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cards';d=json.load(open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'))['cards'];f=set(os.listdir(D));print([c['id'] for c in d if ('art_'+c['id'].lower()+'.png') not in f])"

# ③ 还有没有写死的旧 id（应为空）
#   判据：把 d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json 的 uniqueId 当【新】，
#   拿 _tmp_view/cardid_bak_1013/cards_engine.json 的 id 当【旧】，扫 14 个文件那套正则

# ④ 引擎侧自检（不跑全套）：RuleEngineTest.Run
```

## 七、同批还挖出了什么（同一批远端包里，**新会话优先做**）

> 详账 → `资料/历史/交接_第二十七会话.md` §二·甲；下面只记「挖到了什么 + 判据在哪」。

1. 🔴🔴 **原版卡牌效果数据库**（`A1759`）：`cardAbilities[].abilityLogic[]` = 效果步骤列表，
   每步 = **`chosenAbility` 枚举** + `targetCriteria` + `multiplier` + 参数块（`damageCriteria` / `healCriteria` /
   `buffEffect` / `summonCriteria` / `handBuff` / `manaBuff` / `chanceOfHappening`）。
   **规模** 1,622 步 / 51 种效果 / 1,066 张卡有。**枚举词典**（51/51 已译名）→
   `d:/2/远端缓存_旧profile_1013/导出/AbilityEffect_枚举词典.tsv`。
   🔑 **反证**：`ASH15 Howling Banshee` 卡面「Rally: **Stun** an enemy」= `chosenAbility 130` = `stun` ✓
2. 🔴 **督军后手防御卡**（`A1070③`）：55 督军 → 11 张阵营专属防御卡，**已解析成卡 id** →
   `导出/督军后手防御卡_goSecondCardInHand_已解析.json`（判据 = 同包内 PPtr 按 `m_PathID` 解，55/55 无失败）。
3. **trait 词典**：每张卡的 `CardTraits` 带 `name` + **`_trait` 数字** + `_traitValue`（650 张非空），
   可直接判「原版给这张卡挂了什么 trait」（`A1748` 那 20 张卡能用它补 `keywords`）。
4. **卡 → 立绘 / 语音 / 特效 的 GUID**：`cardImageReference` 1,171 · `summonAudio` 537 · `attackAudio` 59 ·
   `deathAudio` 61 · `customRangedAttackParticles` 484 · `customDeathParticles` 72 · `remnantBodyPrefab` 73 ·
   `whileInPlayAnim` 28。⚠️ **全是 AssetGUID ⇒ 要解析 Addressables 目录的 blob 才能落到资源上**（本会话没做）。
5. ⚠️ **没解开的那一只**：`localization`（I2 词条表，9.4 MB，疑似加密）—— 解开能一次清掉一整族账。
