# K1 — 卡牌 ID 逆推线 · 现状（只读调查 · 2026-10-17）

> 只读。未改任何工程文件（本报告除外）、未跑 Unity、未动 git。
> 判据全部现核：UnityPy 1.24.2（`d:/2/Warpforge_tools/py312/python.exe`）直读本地 bundle + 全树 grep + 计数脚本。
> 同目录另有一份 K2 报告（`K2_卡牌ID数据实证.md`）；两者结论方向一致，**范围与两处细节不同，见 §6**。

---

## 一、一句话结论（回答三问）

1. **「快照」里是【ID + 卡 SO】，但要分两份看**：
   - **2025-07-30 旧 profile 快照**（`d:/2/远端缓存_旧profile_1013/`）**就是 `allcards` 的一个旧版本**，里面有 **1,192 个 `RawCardScript` 卡 SO 本体**（`uniqueId` / `cardName` / 数值 / `cardAbilities` / 立绘 GUID 全在同一对象上）—— **是 ID + SO，不是只有 ID**。
   - **当前 build（Unity 6000.2.6f2 / catalog 2026-08-15）在本机只有 ID 字符串**（`cardIds` / `cardLibraryIds` / `targetId`），**没有一个当前版本的卡 SO**；卡 SO 的 PPtr 全部指向本地不存在的 `CAB-fb8f0144…`。
2. **是，缺的就是「卡 SO 实例」这一类资产**：`RawCardScript : PlayerItem` 的 **MonoBehaviour 实例**。它们装在远端 Addressables 包 **`allcards_assets_all`**（catalog 条目 `allcards_assets_all_ea8bca4f5c6909d3124fd4975dc96173.bundle`，Addressables 内层名 `54bdb7b1114a2de41519cac560a4bce4`，内层 CAB **`CAB-fb8f01449289635703d83d52c3f18245`**）。本机 84 个 bundle 全扫 = **0 个卡 SO**；catalog 里本地缺的 3 个 bundle 中，`alternateartstyles` / `localization` 已从旧快照补回，**只剩 `allcards`**。
3. **本地能直读 uniqueId↔卡 = 1,192 张**（旧快照，`1,192/1,192` 非空、0 重复）；其中 **1,009 张**在现池。现池 1,127 张里 **118 张无 SO**。另有 **63 个 heroId → 督军名**可解（53 个短 id 由 SK 卡组唯一确定）。普通新卡只能拿到 **ID 字符串痕迹**：**74/95** 个短号在本地原版列表里出现过，**44 张连字符串都没有**；**0/118 能拿到卡名**。

---

## 二、资产清单表（逐包：带 ID 列表 还是 卡 SO 本体）

| 包 / 资产 | 路径 | 带什么 | 数量（现核） | 能解 uniqueId↔卡？ |
| --- | --- | --- | --- | --- |
| **旧 profile 的 allcards 缓存** | `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/dca75913…/__data`（467,359 B） | **卡 SO 本体**：1,192 `RawCardScript` + 2 `AlternateArtCard` + 1 `AddressablesBuildNumber` + 3 `MonoScript` | 1,192 张卡；`uniqueId` 1,192 非空/0 重复；`cardName` 1,192 非空 | ✅ **1,192/1,192（直读，不是推）** |
| **当前 build 的 allcards** | **本地不存在**（只在 catalog 里） | 本该是当前卡池的 `RawCardScript` | — | ❌ 拿到即 118/118 |
| `draftpacks_assets_all` | `…/StreamingAssets/aa/StandaloneWindows64/draftpacks_assets_all.bundle`；导出 `assets_full/bundle_draftpacks_assets_all/MonoBehaviour/` | `PrebuiltPack.cardIds: string[]` | 882 个包；**2,646 条 / 904 distinct**（全短号） | ❌ **只有 ID 列表** |
| `prebuiltdecks_assets_all` | 同上；`bundle_prebuiltdecks_assets_all/MonoBehaviour/` | `cardLibraryIds: string[]` + `deckHero.targetId` + `deckAvatar` PPtr | 236 副卡组；**5,157 条 / 876 distinct**（859 短 + 17 hex）；63 个 heroId | ❌ 卡只有 ID 列表；`deckAvatar` → 本地 `cosmeticsso`（督军名），`deckHero.reference` → CAB-fb8f（缺） |
| `cosmeticsso_assets_all` | 同上；`bundle_cosmeticsso_assets_all/MonoBehaviour/` | 470 `PlayerAvatar` · 32 `DropTableItem` · 1,238 个自带 `uniqueId` 的商品/头像；`obtainableItems[].targetId` | `uniqueId` **1,238 distinct（对象自己的 id）**；`obtainableItems` **348 条 / 300 distinct**（22 个文件）；`dropTableItems` 另 86 条/33 distinct | ⚠️ **只有 ID 列表 + 督军头像名**；DT 的 PPtr（`m_FileID=2`）→ CAB-fb8f，落不到本地 |
| `duplicateassetisolationso_assets_all` | 同上；`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/` | 2 个 `DropTableItem` + `Enchantments`（`markOfChaos`）+ `EnvironmentalEffectCardsSO`（13 个环境 / 39 条 `offensiveCards[].offensiveCard` PPtr，**无 targetId**）等 | `targetId` **45 distinct（41 短 + 4 hex）**；Enchantments 8 个卡引用；offensiveCard 39 + emptyOffensive 13 个 PPtr | ❌ 只有 ID 列表 / 纯 PPtr；全部 `m_FileID=4` → CAB-fb8f |
| `menus_assets_all` | 同上；`bundle_menus_assets_all/MonoBehaviour/` | 2 个 `Booster Pack` + 2 个 `DT Ultramarines …` 的自带 `uniqueId`；战役节点 `Rewards[].item.targetId` | **15 个卡 id**（UM2/3/8/13/17/18/22/24/27/28/34/37/39/44/55）；另有 46 个 `NodeId` 只是**同名号段**、不是卡引用 | ❌ 只有 ID 列表（少量奖励引用）；PPtr `m_FileID=22` → CAB-fb8f |
| `staticgeneralassets_assets_all` | `d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/ACH24 Archaeovist.json`（第 239 行） | 只有一处 `"_typeNameAndAssembly": "RawCardScript, Assembly-CSharp"` 的**空引用占位** | 0 个卡 SO | ❌ |
| `*cardassets_assets_all` / `*cardanims_assets_all` | 13 族各两份 | `Sprite` / `Texture2D` / `AudioClip` / `CardAnim` | 例：SW 91 Sprite + 109 AudioClip；SW 38 个 CardAnim | ❌（没有卡 SO；卡面图/语音/演出在，规则数据不在） |
| `localization`（旧快照解码件） | `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` | `Card_Name/<id>` / `Card_Description/<id>` | `Card_Name` **1,065 个 id**（覆盖快照 1,050/1,194；**118 张缺卡 0 命中**） | ⚠️ 只覆盖快照时代，且同样没有那 118 张 |
| `card_ids.json` | `d:/4/Unity/数据/游戏数据/card_ids.json` | **扁平 dict：`uniqueId → cardName`（英文名）** | **1,310 键 / 1,308 非空**：1,194 真值（1,192 精确 + 2 个 `#HB_` 空值）+ 116 旧表残留 | ✅ 真值部分 1,194/1,194 与快照逐字一致 |

---

## 三、覆盖数字（分档，每档给「可解 / 不可解」与判据）

### A 档 · 直读（SO 本体在本地）

| 项 | 数字 | 判据 |
| --- | --- | --- |
| 旧快照卡 SO | **1,192** | UnityPy：`MonoBehaviour` 1,195 → `m_Script.PathID=8864902669260471755` 的 **1,192**；`uniqueId`/`cardName` 各 1,192 非空、id 0 重复 |
| 其中短号 / 32-hex | **933 短 + 259 hex** | 导出 JSON 1,194 个 `uniqueId`（含 2 个 `#HB_`）；`RawCardScript` 1,192 = 933 + 259 |
| 旧快照覆盖现池 | **1,009 / 1,127** | 896 短 + 113 hex 的 id 命中 |
| 旧快照有、现池没有 | **183** | 37 短 + 146 hex（多为变体/附魔/退役卡） |
| **现池缺 SO** | **118 / 1,127** | SW 68 · UM 短号 15（UM87–101）· DA 12（DA75–86）· 23 个自造 `UM_*` 下划线 id |

### B 档 · 督军（ID ↔ 名字，PPtr 落到本地对象）

| 项 | 数字 | 判据 |
| --- | --- | --- |
| 可解 heroId | **63** | 236 副预组卡组的 `deckAvatar`（`m_FileID=2`）→ `cosmeticsso` 的 `PlayerAvatar`；**223/236 副**能落到对象（13 副 Starter 的 `deckAvatar=null`，其 heroId 均被别的卡组覆盖） |
| 短 id / hex | **53 短 + 10 hex** | 53 个短号全部由 `*_SK_*`（天梯预组）**唯一**给出；10 个 hex 来自 FTUE / 教程卡组（`*_BOT_FTUE_DeckEasy`、`*_Deck0_Tutorial*`），均各有唯一头像名 |
| 质量提醒 | — | **非 SK 的主题卡组（`SpaceWolves_Deck1..6` 等）的 `deckAvatar` 是「头像皮肤」不是督军**：同一 heroId 可对应 2–3 个头像（如 SW1 出现 Logan/Arjac/Hrolf）⇒ 把全部卡组混在一起算会让 **20 个 heroId 变歧义**；只有 `*_SK_*` 那一档干净 |
| 缺卡里的督军 | **4 / 6** | DA75 Master Zacharial · SW1 Logan Grimnar · SW2 Njal Stormcaller · SW3 Ragnar Blackmane（SW3 须只取 SK 卡组）；UM_Roboute_Guilliman / UM_Valius_Paxor 无链接 |

### C 档 · 只有 ID 字符串（有 id、无名字、无 SO）

| 项 | 数字 | 判据 |
| --- | --- | --- |
| 本地「卡相关字段」的 id 并集 | **1,073 distinct（964 短 + 109 hex）** | 字段限定 `cardIds` / `cardLibraryIds` / `targetId`；其中 938 在现池、135 不在现池、890 在旧快照 |
| 现池缺卡里「id 出现过」 | **74 / 95 短号**（+ 23 个自造 id 全部 0） | 来源分解（95 口径）：DT+deck+draft 55 · DT+draft 8 · DT+draft+dup 4 · DT+deck 3 · DT+dup 3 · 仅 deck 1（其中 4 个只从 `deckHero.targetId` 拿到：SW1/2/3/DA75；SW1–3 同时也在 DT 里） |
| 现池缺卡里「一个字符串都没有」 | **21 短号**：DA76–DA86（11）+ SW59–SW68（10）；再加 23 个自造 id = **44 张全无** | 全树 24 万 JSON + 5 个 ID 包逐字段扫描 |
| 卡名 | **0 / 118** | 旧快照 0 命中、官方本地化 `Card_Name` 0 命中、draft pack 名对 SW 47 个包 0 命中（`R_SO…md:291`） |

### D 档 · 旧表残留（不是那 118 张，但会影响「id 真值」）

`card_ids.json` 里 **21 个键不在现池**，逐条现核后分三类（**与 R_SO §4·1 的「19 条旧表偏移」有一处需要订正**）：

1. **真 id、但我们表里配错名字：`DA65` / `DA66`** —— 它们**出现在原版 draft + deck 的卡表里**（本地 2 个源都有），所以不是假 id；错的是我们表把 `Grim Effigy` / `Plasma Generator` 配到了这两个格子，而现池这两张卡的 id 是 **DA59 / DA61**（快照真值）。
2. **旧表偏移候选（名字在快照里另有真 id）：17 条** —— `AM20→AM59` · `AM77→AM76` · `BL76→BL75` · `BL77→BL74` · `GSC48→GSC40` · `GSC78→GSC75` · `SAU77→SAU76` · `TL76→TL65` · `TL77→TL66` · `TL78→TL67` · `TL79→TL75` · `TL80→TL68` · `TL82→TL69` · `TL83→TL70` · `TL84→TL71` · `TL85→TL72` · `TL86→TL73`（BL76/BL77 各还有一个 GUID 形态同名对象 ⇒ 严格说这 2 条是「2 候选」）。
3. **两个都没有：`SOR76`（Emperors Judgement）/ `TAU76`（Hunters Instincts）** —— 快照无同名、本地无字符串。

---

## 四、缺口的准确定性（用证据说，不猜）

**不是「有 SO 但没 ID 字段」，也不是「有 ID 但没 SO 指向」——是【当前 build 的 SO 本体不在本地】。**

| 命题 | 判据 |
| --- | --- |
| 卡 SO 的 id/名字是**自身字段** | `RawCardScript : PlayerItem`；`uniqueId`（`PlayerItem.cs:14`）· `cardName`（`RawCardScript.cs:21`）；旧包 1,192/1,192 两者非空、0 重复 |
| 当前 build 本地 **0 个卡 SO** | ① 84 个 bundle UnityPy 全扫：`MonoBehaviour` 66,459 个，含 `cardAbilities` 或 `uniqueId+cardName` 的 **0**；② `assets_full` 66,543 个 MonoBehaviour JSON，`"cardAbilities"` **0**、`"cardShortId"`/`"manaCost"` **0**；③ 全盘 `d:/2` 递归 grep `--include=*.json '"cardAbilities"'`，**唯一命中 = 旧 profile 导出 `cards_1195_2022时代.json`**（其余 0）；④ `m_Script.PathID=8864902669260471755` 的引用 **0** |
| **ID → 卡 SO 的 PPtr 是存在的，只是目标缺** | `prebuiltdecks.deckHero.reference`（`m_FileID=3`）、`cosmeticsso` DT 的 `obtainableItems[].reference`（`m_FileID=2`）、`duplicateassetisolationso` DT（`m_FileID=4`）、`menus` 奖励（`m_FileID=22`）**全部指向 `CAB-fb8f0144…`**；该 CAB 在本机 84 个 bundle 的 CAB 名里**不存在** |
| **CAB-fb8f = allcards（直接实证，不再是推断）** | 旧快照那份 `__data` 的**内层 CAB 名就是 `CAB-fb8f01449289635703d83d52c3f18245`**，且里面正是 1,192 个 `RawCardScript`；同时 catalog 在 `allcards_assets_all_ea8bca4f…bundle` 旁边列出内层名 `54bdb7b1114a2de41519cac560a4bce4`，而旧文件的父目录名正是 `54bdb7b1…` ⇒ **旧快照 = allcards 的旧版本** |
| 本地缺的 bundle 只有 allcards 一类卡包 | catalog 里 67 个带哈希的 bundle 名与本地 84 个文件做差：缺 **3 个** —— `allcards` / `alternateartstyles` / `localization`；后两个已从旧快照拿回（alternateartstyles = 2 张画风 logo；localization = 9,441 条词条） |
| **旧快照不是当前版** | Player.log：旧 profile = **2022.3.38f1**、现客户端 = **6000.2.6f2**；旧包 `cardArmy` 最大 **120**（无 130 SpaceWolves）、`expansionPack` 最大 **70**、SW id **0 个**；旧包 1,192 张 vs 现池 1,127 张（且现池有 SW） |
| 不能靠 CCD 现拉 | `AddressablesManager__SetupCCDManager` 的 `{EnvironmentName}/{BucketId}/{Badge}` 运行时从后端拷，后端已关；两边 `settings.json` 的 `m_CcdManagedData` 四字段全空（`R9…md:88`） |

**所以「本地 0/118」的准确含义**：当前 build 的 118 张卡**没有 SO 本体**；但它们中的 74 张能拿到**原版 id 字符串**（用于证明「原版确有这个号」），只有 4 张能拿到督军名，**没有一张能拿到普通卡的名字/数值/效果**。

---

## 五、`card_ids.json` 的准确形状（用户点名的 1,310 行）

- **形状**：`{"mapping": {uniqueId: cardName(英文)}, "note": "..."}` —— **扁平字典，值是英文卡名**，不是「唯一键→内部 id」，也没有数值/效果。
- **1,310 行构成（现核）**：1,194 个键在旧快照里（1,192 个值 == 快照 `cardName`，逐字一致；2 个 `#HB_DA1`/`#HB_BL34` 值为空、对应 `AlternateArtCard` 的 `cardName=null`）+ **116 个旧表残留**。
- **116 的归宿**：**95 个是现池缺卡 id**（SW 68 + UM 87–101 + DA 75–86，其中 4 个只从 `deckHero` 拿到）；**21 个不在现池**（见 §3·D）。
- ⚠️ **note 里的「真值覆盖 935 条」是 2026-10-13 重建当时的说法，已过期**：现表现核 **1,194/1,194 与快照逐字一致**（note 自己最后一行也这么写）。935 这个数不建议再引用。
- 另有 `card_id_truth_index.json`（1,060 `base` / 1,086 `by_name` / 1,048 `by_official_en`），来源仍是同一份旧快照 + 官方本地化，**不含新卡**。

---

## 六、与同目录 K2 报告的口径差异（两处，供主对话合并）

1. **K2 的一句话「本地只有 ID 字符串，没有一个 RawCardScript 卡 SO 实例」——范围应限定为「当前 build / `assets_full`」**。**2025-07-30 旧 profile 快照里确实有 1,192 个**（K2 未把旧缓存算进「本地」）。这不是 K2 查错，是「本地」口径不同；**用户问的正是含 2025-07-30 快照的那一份**，所以本节按「两份快照」回答。
2. **K2 §3·3 把「CAB-fb8f = allcards」写成高置信推断；本报告已直接坐实**（旧快照内层 CAB 名 = fb8f + 内含 1,192 `RawCardScript`）。
3. 计数差异（不是事实冲突）：K2 的 `cosmeticsso` targetId 382 distinct 用的是更宽的字段/正则口径；本报告按 **`obtainableItems` 348 条 → 300 distinct（22 文件）**，加 `dropTableItems` 后 333。`menus` 的 15 个卡奖励 targetId 是本报告新增的（K2 §5·3 只写了 DropTable）。

---

## 七、没查清 / 下一步该补什么

1. **当前 build `allcards` 的真实内容**：卡池是否仍是 1,127 张、SW59–SW68 在官方是什么 id、UM102–118 属于哪些卡 —— **只有拿到包才能定**。
2. **23 个自造 `UM_*` id ↔ 原版 id**：本地原版列表里有 **UM102–118（17 个）** 是现池完全没有的号，数量上像是一批新 UM 卡；但 draft/DT 只有 id、没有卡名，**不能据此配对**。
3. **`BL76`/`BL77`** 各有短号 + GUID 两个同名对象，严格判据下只能算「2 候选」，没有定论。
4. **`menus` 的 15 个卡奖励 targetId**：PPtr 全落 CAB-fb8f，但本地无 SO，无法确认它们与卡池哪张同名。
5. **`EnvironmentalEffectCardsSO`**：`enviromentalEffects[].offensiveCards[].offensiveCard`（39 条）与 `emptyOffensive`（13 条）是**纯 PPtr、没有 targetId**（全部 `m_FileID=4` → CAB-fb8f），卡 id 在本地文本层不可恢复。
6. **`duplicateassetisolationso` 的 `Enchantments`** 8 个卡引用（含 4 个 hex）只证 id 存在，未逐条对到卡名。
7. 搜索面：`d:/2`（含 `新解包资源/assets_full`、两份 StreamingAssets、`10.13游戏`、旧 profile）、`d:/4`、`C:/Users/qjh36`、`C:/Windows.old`、当前用户 LocalLow 缓存。**当前用户 LocalLow 的 `54bdb7b1…` 目录是空的**（内容已搬到旧 profile 备份），没有第二份更新缓存。
