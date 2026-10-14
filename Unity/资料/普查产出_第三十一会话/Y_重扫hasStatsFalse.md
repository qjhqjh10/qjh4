# Y · 重扫源表 `hasStats=false` 的 81 条（照 `TL40` 那次的五条判据）

> 只读查证，2026-10-15。⛔ 没改任何文件。判据源：`V_P1收尾.md` §二·B。
> 源表 = `Unity/数据/游戏数据/card_stats.json`（`cards[].hasStats == false`，共 **81** 条）。

---

## 结论一句话

**没有被误跳的真卡。** 81 条一条都**不是** `TL40` 那种「真卡躺在源表里」——
决定性全局判据：**`allcards`（`cards_1195_2022时代.json`，1194 行）里「不在卡池」且带
「可收藏卡签名」（`showInCollectionOptions != 0` **且** `inventoryOptions == 0`）的行 = `0` 条**
（`TL40` 本来就是最后一条，已于 2026-10-15 收进 `STATS_EXCEPTIONS`）。

⇒ 这一族的账**可以结**：不是「还没查完」，是**已经查空**。

---

## 一、关键发现：真正的鉴别判据不是那五条，是 `allcards` 行的**签名**

`TL40` 那条的真值行长这样：
`showInCollectionOptions = 5` · `inventoryOptions = 0` · `m_Name = "T7 Leviathan-Infestation Node"`（裸 id）。

81 条里其余任何一条的 `allcards` 行都**不满足这个签名**，而 `m_Name` 前缀把 81 条分得干干净净：

| `m_Name` 形状 | `showInColl` / `invOpt` | 是什么 | 本批条数 |
|---|---|---|---|
| `Z-<faction>-<名字>` · `cost=-1` · `rarity=5` | 5 / 5 | **环境效果卡（= 进攻卡）** | 39 |
| `Z-NC <faction>-<名字>` | 0 / 10 | **非收藏挂件（附魔/Dark Pact/防御卡旧名）** | 11 |
| `17 Leviathan-Carnifex_Enchantment_*` | 0 / 10 | 单位挂件（角虫附魔） | 3（含在上面 11 里） |
| `REMOVED_T11 Sautekh-…` | 0 / 10 | **开发者显式删除**（全 allcards **仅此 1 条** `REMOVED_`） | 1 |
| 裸 id（无前缀） | 5 / **0** | **可收藏卡** | 1（= `TL40`，已收） |

---

## 二、81 条的去处（可加总到 81）

| 桶 | 条数 | 是什么 · 判据 |
|---|---|---|
| **A 环境效果卡（= 进攻卡）** | **39** | 12 阵营 × 3 = 36 条有 `Z-<faction>-…` 真值行（`ASH58-60` `AM63-65` `BL59-61` `DA62-64` `EC62-64` `GOF50/51/54` `GSC60-62` `SAU62-64` `SOR62-64` `TAU59-61` `TL57-59` `UM49-51`）+ SpaceWolves 3 条（`First Light`/`Full moon`/`Everstorm`，只有贴图）。**另有一套数据表在管**：`数据/游戏数据/offensive_cards.json`（13×3=39）+ `Resources/OffensiveCards.json` + `Art/offensive/`（50 张）。**不是被误跳，是本来就不进卡池。** |
| **B 空进攻占位** | **13** | `Normal Conditions` ×9 · `Normal` ×3 · `Default Conditions` ×1 = 13 阵营的 `emptyOffensiveCard`（`offensive_cards.json` 里每阵营那条 `face_texture = …_offensive_empty`）。官方本地化只有 `Card_Name/NormalOffensiveConditions`、**没有 `Card_Description`** ⇒ 占位。**全部带 `noise:true`。** |
| **C `noise:true` 的其它** | **2** | `v2`（`DarkAngels_AA_warlord_Azrael_v2` = 阿兹瑞尔异画）· `Threnodic Choir Flawless`（与另一张同图，源表自己标了重复）。 |
| **D 旧名，真卡仍在池** | **2** | `Hive Fleet Arrival` → 本地化键是 `OLD_GSC59`；抽包 `GSC_HiveFleetArrival.json` 的 `cardIds` = `[GSC15,GSC35,**GSC59**]` ⇒ **池里 `GSC59` =「Day of Ascension 飞升之日」**。`Annihilation Command` → 抽包 `SAU_AnnihilationCommand.json` 的 `cardIds` = `[SAU5,SAU16,**SAU53**]` ⇒ **池里 `SAU53` =「Undying Legions」**；那张贴图 `Necron_Sautekh_strat_Annihilation Command` 的 `cardImageReference` **就挂在 `SAU53` 上**。 |
| **E 挂件行 / 防御卡旧名** | **11** | `Khorne`/`Nurgle`/`Slaanesh`/`Tzeench`（= `Mark of X (enchantment)` → 池里 `Dark Pact of Blood/Resilience/Excess/Fate`）· `Warmaster`（`97e95944…` → 池里该 id 是「Chosen of the Four」）· `Exemplar of Hate`（`4cdc1b5f…` → 池「Master of Repentance」）· `Dark Prince´s Throne`（`EC61` → 池「Decadent Throne」）· `Tools of Torture`（`EC60` → 池「Armoury of Excess」）· `Crushing Claws`/`Armoured Exoskeleton`/`Devourer Cannon`（`17 Leviathan-Carnifex_Enchantment_*`，`cost=-1` `rarity=1`）。 |
| **F 已经在池（白名单）** | **3** | `Dark Pact of Fate` · `Fire Warrior Marksman` · `Infestation Node`(= `TL40`) —— 全部在 `gen_cards_engine.py` 的 `STATS_EXCEPTIONS`。 |
| **G 开发者删除** | **1** | `Disentegration Capacitors` → `allcards` **`SAU48`**，`m_Name = "REMOVED_T11 Sautekh-Disintegration Capacitors"`（`showInColl=0` `invOpt=10`）。`card_ids.json` 里 `SAU48 → Disintegration Capacitors` 也有。 **全库唯一一条 `REMOVED_`。** |
| **H 孤儿贴图 · 无任何游戏侧判据** | **10** | `Righteous Gaze` · `Method to the Madness` · `Threnodic Choir Flawless Blade` · `Ambush` · `Xeno-Mutations` · `Slay the Heretic` · `Grav Inhibitor Drone` · `Lieutenant with Combi-Weapon` · `Purgator Mirabilis` · `Predator Destructor`。贴图在 `bundle_*cardassets*_Texture2D/` 里有，但**不被任何 allcards 行的 `cardImageReference` 引用**（= 解包残图），无 `allcards` 行、不在抽包、不进池。见下 §三。 |

**加总核对**：`noise` 过滤掉 15（B+C）；`hasStats=false` 过滤掉 66（A39 + D2 + E11 + F3 + G1 + H10）= **81**。

---

## 三、H 桶里最像真卡的一条：`Method to the Madness`（**倾向：仍是死数据，但留一句没查清**）

| 判据 | 真值 |
|---|---|
| ① allcards | **没有**（按名、按 `m_Name`、按 `edafa88b75667466da893bea3f6167a7` 全无） |
| ② 官方本地化 | ✅ **`Card_Name/edafa88b75667466da893bea3f6167a7` = ("Method to the Madness", "疯狂之道")** —— 但 **`Card_Description/<同 id>` 不存在**（`TL40` 是 Name+Description 都有） |
| ③ 抽包 `cardIds` | **没有**（882 个 draftpack 全扫过） |
| ④ 立绘 | ✅ 本地有：`bundle_chaosspacemarinesblacklegioncardassets_assets_all/Texture2D/CSM_BlackLegion_strat_Method to the Madness.png` |
| ⑤ zh_cards | ✅「疯狂自有章法」——⚠️ 见 §四，**这条不算独立证据** |

**为什么仍判「不是被误跳的真卡」**：同一张本地化表里成串躺着**同类占位/旧键** ——
`BL_OldMarkOfKhorne/Nurgle/Slaanesh/Tzeentch` · `OLD_GSC59` · `GSC55_OLD` ·
`OLD_d01a5ba5865ded4418194ea50e333b21` · `NormalOffensiveConditions`（共 15 个 `Card_Name` 键不在 allcards 里）。
`Method to the Madness` 的 `Card_Name` **无 `Card_Description`**，与那批旧键同形。
⇒ 倾向：**旧词条/被撤卡**，与 `SAU48` 同类，只是没打 `REMOVED_` 标记。

⚠️ **要坐实只有一条路**：远端 `allcards`（另一个写手在跑的 `P2` 线）或实机。**本地判据到此为止。**

---

## 四、🔴 两条判据的鉴别力更正（照它做会拿到噪声）

| 判据 | 实测命中 | 问题 |
|---|---|---|
| **⑤ `zh_cards.json` 中文名** | **79 / 81** | 🔴 **没有鉴别力，而且是自证** —— `zh_cards.json`（`cards` 1198 条）本身就是**从含这 81 条的源表**生成的，所以连 `Normal Conditions`→「正常条件」、`Normal`→「普通」都在。（缺的 2 条 = `v2`、`Fire Warrior Marksman`。）⇒ `V_P1收尾.md` §二·B 把判据⑤当**独立证据**引用，**口径不成立**（保留其结论，但别再拿它当第五条）。 |
| **④ 立绘本地** | **80 / 81** | 也几乎全中（只有 `Dark Pact of Fate` 的 `art` 是空串）。卡图整包解出来了 ⇒ **贴图在 = 不构成「它是真卡」**。⚠️ 但反过来：**这 81 张的贴图【一张都没进我们工程】**（`Resources/Art/` 里按原名 `find` 全部 0 命中）—— 因为没进池就没导。所以 `gen_cards_engine.py:401-407` 注释写的「**无立绘**」在**「我们工程里没有」**这个口径下**是对的**，只是当时没说清是哪一份。 |

---

## 五、没查清 / 边界（如实写）

- **搜过的文件**：`card_stats.json`(1213 行) · `cards_1195_2022时代.json`(1194 行，38.6 MB) ·
  `官方本地化_en+zh-CN.json`(9441 键) · `zh_cards.json`(1198) · `card_ids.json` ·
  `cards_engine.json`(1127) · `offensive_cards.json` · `environment_conditions.json` ·
  `defensive_cards_39.json` · `bundle_draftpacks_assets_all/MonoBehaviour/`(**882** 个 SO 全扫) ·
  `bundle_boosterpacks_assets_all/`(85 个 MB) · 13 个 `bundle_*cardassets_assets_all/Texture2D/`(1413 张) ·
  `Resources/Art/`(10866 文件) · `gen_cards_engine.py:390-470,980-1060`。
- **搜过的词**：81 条卡名（全名 + 归一化）· `uniqueId`（`ASH/AM/BL/DA/EC/GOF/GSC/SAU/SOR/TAU/TL/UM` 前缀全枚举）·
  `REMOVED` · `OLD_` / `_OLD` · `NormalOffensiveConditions` · `enchantment` · `Z-NC` ·
  `Disintegration` / `Disentegration` · `Method to the Madness`。
- **没查到 / 不在本批范围但相关**：
  ① `card_ids.json` 的 `mapping` 只有 2 键（`mapping` / `note`），`SAU48`、`EC62` 都在里面 ⇒ 「在 `card_ids` 里」**不是**「它是真卡」的证据；
  ② allcards 里另有 **10 张**「具名但不在池」的行（`SAU48` 属其一），其余 9 张里有 warlord/tutorial 族（`Uriel Ventris` / `Tervigon` / `Grukk Face-Rippa` / `Makari the Grot` / `Medreyal Ghaelyn` / `Tempestus Scion` / `Haarken Worldclaimer` / `Nemesor Zahndrekh` ×2 / 一堆 `(tutorial)` / `Unhallowed Gifts`）—— **都不在 81 条里**，本批**没判**（都 `showInColl=0` `invOpt=10`，签名上不是可收藏卡）；
  ③ `Card_Name` 表里 **15 个键不在 allcards**，其中 6 个是欧克族具名卡（`Kunnin' Grot` / `Ork Boy` / `Choppa Boy` / `Big Shoota Boy` / `Deffkilla Wartrike` / `Hulking Nob`）—— **不在 81 条里，本批没判**；它们可能是 2022 快照之后加的卡或衍生物，**要坐实需远端**。
- **⛔ 没跑的**：Unity · `ruleprobe` · 任何生成器 · git。**没改任何文件**（只新建本报告）。

---

## 六、倾向（⛔ 裁定由主对话下）

**结账。** 81 条已查空 —— 39 条是另一套系统（进攻卡）· 15 条源表自己标了 `noise` · 11 条是挂件/防御卡旧名（真卡在池）·
3 条早已在白名单 · 2 条是改名后仍在池 · 1 条被开发者删除 · 10 条只剩解包残图。
**`RL` 意义上的「真卡被误跳」在这一族里已归零。** 唯一值得挂着的不是这 81 条，而是 §五·③ 那 6 个欧克
`Card_Name` 键与 §五·② 那 10 张具名不齐 —— 两者都要**远端 `allcards`** 才能判。
