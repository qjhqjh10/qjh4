# W_B26 · 卡 id 回填（`card_ids.json` 的「值」≠ 卡池卡名）

写手代理 B26 · 2026-10-17。**没跑 Unity**。动过的只有 `card_ids.json`（定点改 22 个**值**，**键一个没动**）+ **重跑**两个生成器（产物只由重跑产出）。
普查脚本 / 补丁脚本 / **改动前的两份基线副本** / 逐副 diff 全留在 `资料/普查产出_1017/_b26_scratch/`（只读分析 + 定点替换，未碰任何别的文件）。

## ① 根因（一条，可复算）

`card_ids.json` 的**值** = **PnP 扫描 OCR 名**（= `card_stats.json` 的 `ocrName`）；卡池的 `name` = **原版卡图资产名**。
`gen_cards_engine.py:763` 的 `pick_id` 按 **`_norm_name` 归一化卡名**查表（`:473` 只去大小写/标点）⇒ 两者不等 = **查不到 ⇒ 掉成自造 id `前缀_卡名`**（`make_id`，**静默**）。

**三条判据**（本次逐条核过）：① 22 条**旧值**逐字等于 `card_stats.json` 的 `ocrName`；② **新值**逐字等于 `card_stats.json` 的 `name`；③ **新值**逐字等于解包资源 `d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/{Sprite,Texture2D}/*.json` 的**资产名尾段** —— **22/22 命中新值、22/22 都查不到旧值**（旧名只是 OCR/印刷写法）。
⚠️ `zh_CN.csv` 的卡名键**不是独立来源**（它来自 `card_stats.name`，B20 §①.4 已证）⇒ 我没拿它当判据。

## ② 56 条分类表（口径 = **本次改动前**，即 B20 收工态；`值` 列写**旧值**）

**A 真掉 id —— 22 条 · 全部已回填**（`原版 id` 是关键，值改成卡池名后 `pick_id` 即命中）

| 原版 id | 旧值（PnP/OCR 名） | 卡池名（= 新值） | 原自造 id |
|---|---|---|---|
| `DA11` | `Sergeant Taaman` | `Sergeant Naaman` | `DA_Sergeant_Naaman` |
| `DA83` | `Ballistus Dreadnought` | `Ravenwing Ballistus Dreadnought` | `DA_Ravenwing_Ballistus_Dreadnought` |
| `EC2` | `Euphoric Strike` | `Euphoric Strike (Lord Exultant's Talent)` | `EC_Euphoric_Strike_Lord_Exultant_s_Talent` |
| `EC4` | `Duelists Hubris` | `Duelist's Hubris (Lucius' Talent)` | `EC_Duelist_s_Hubris_Lucius_Talent` |
| `EC6` | `Excessive Vigour` | `Excessive Vigour (Daemon Prince's Talent)` | `EC_Excessive_Vigour_Daemon_Prince_s_Talent` |
| `GOF31` | `Veteran Stormboy` | `Veteran Flyboy` | `GOF_Veteran_Flyboy` |
| `GSC1` | `Iconward Malak Vorenth` | `Acolyte Iconward` | `GSC_Acolyte_Iconward` |
| `GSC64` | `Telepathic Domination` | `Telephatic Domination` | `GSC_Telephatic_Domination` |
| `SOR19` | `Sister Repentia` | `Sisters Repentia` | `SOR_Sisters_Repentia` |
| `SOR30` | `Simulacrum Celestian` | `Simulacrum Imperialis` | `SOR_Simulacrum_Imperialis` |
| `TAU19` | `Fire Warrior Breacher` | `Breacher Fire Warrior` | `TAU_Breacher_Fire_Warrior` |
| `TAU34` | `AunShi` | `Aunshi Ethereal` | `TAU_Aunshi_Ethereal` |
| `TAU39` | `Razorshark Fighter` | `Razorshark` | `TAU_Razorshark` |
| `TAU45` | `Stormsurge` | `Stormsurge Battlesuit` | `TAU_Stormsurge_Battlesuit` |
| `TAU63` | `Saviour Protocols` | `Saviour Protocolst` | `TAU_Saviour_Protocolst` |
| `TAU64` | `Experimental Drones` | `Experimental Drone` | `TAU_Experimental_Drone` |
| `UM2` | `Master Tactician 2` | `Master Tactician` | `UM_Master_Tactician` |
| `UM45` | `Greatest Deeds 2` | `Greatest Deeds` | `UM_Greatest_Deeds` |
| `UM72` | `Storm Speeder Hailstrike 2` | `Storm Speeder Hailstrike` | `UM_Storm_Speeder_Hailstrike` |
| `UM75` | `Rapid Deployment 2` | `Rapid Deployment` | `UM_Rapid_Deployment` |
| `UM76` | `Armoured Offensive 2` | `Armoured Offensive` | `UM_Armoured_Offensive` |
| `UM79` | `Eliminator Sergeant 2` | `Eliminator Sergeant` | `UM_Eliminator_Sergeant` |

> 6 条 `… 2` 是**同名现象**：旧值那个 `" 2"` 后缀正来自 PnP 文件名 `…-2.png`（`card_stats.face` 逐条对得上，例 `Ultramarines/2天赋/Warpforge_2_Master-Tactician-2.png`）。全表**只有这 6 条**值带尾随数字。

**B 命名差异（纯标点/大小写/连字符差）—— 33 条 · 不动**（归一化后 `pick_id` **本来就命中**，右边括号里是它们**已经拿到**的原版 id）：
`AM28` `Bullgryn Boneead`→`Bullgryn Bone’ead`(AM28) · `GOF103` `Power of the Waaagh`→`Power of the Waaagh!`(GOF103) · `GOF42` `Ard as Nails`→`Ard As Nails`(GOF42) · `GOF43` `Cloud of Smoke`→`Cloud Of Smoke`(GOF43) · `GOF46` `Scrag Em`→`Scrag ’Em`(GOF46) · `GOF53` `Getem ladz`→`Get'em ladz!`(GOF53) · `GOF85` `Unleashed Tramplasquig`→`Unleashed TramplaSquig`(GOF85) · `GOF98` `Skrag Every Stash`→`Skrag Every Stash!`(GOF98) · `GSC16` `Experimental Bio Horrors`→`Experimental Bio-Horrors`(GSC16) · `GSC29` `Vox Hacker`→`Vox-Hacker`(GSC29) · `SAU60` `Self Destruction`→`Self-Destruction`(SAU60) · `SOR26` `Aestred Thurga`→`Aestred-Thurga`(SOR26) · `SOR76` `Emperors Judgement`→`Emperor's Judgement`(SOR76) · `SOR8` `Arco flagellant`→`Arco-Flagellant`(SOR8) · `SW42` `Bjorn the Fell Handed`→`Bjorn the Fell-Handed`(SW42) · `SW46` `Bjorns Shrine`→`Bjorn's Shrine`(SW46) · `SW57` `Hunters Guile`→`Hunter's Guile`(SW57) · `SW6` `Tempests Wrath`→`Tempest's Wrath`(SW6) · `TAU1` `AunVa`→`Aun'Va`(TAU1) · `TAU29` `Breacher Shasui`→`Breacher ShasUi`(TAU29) · `TAU33` `Stealth Shasui`→`Stealth Shas’ui`(TAU33) · `TAU4` `Commander OMaisos`→`Commander O'Maisos`(TAU4) · `TAU49` `Zephyrs Grace`→`Zephyr's Grace`(TAU49) · `TAU50` `For The Greater Good`→`For the Greater Good`(TAU50) · `TAU73` `Lone spear`→`Lone-spear`(TAU73) · `TL3` `Swarmlord`→`SwarmLord`(TL3) · `TL33` `Screamer Killer`→`Screamer-Killer`(TL33) · `TL51` `Hyper adaptation`→`Hyper-adaptation`(TL51) · `TL65` `Protective Bio structure`→`Protective Bio-structure`(TL65) · `UM51` `Heros Respite`→`Hero's Respite`(UM51) · `UM52` `Humanitys Shield`→`Humanity's Shield`(UM52) · `UM54` `Point Blank Shot`→`Point-Blank Shot`(UM54) · `UM95` `Dutys End`→`Duty's End`(UM95)
> **不动**的理由：值改了**功能上零变化**（`_norm_name` 相等），却会动 `gen_prebuilt_decks.py` 的 t1/t3 匹配档 ⇒ 无收益、有风险。（B20 那 5 条 `SOR32/SW36/SW45/UM34/UM83` 属 A 类、已在上一批修掉，**不在这 56 条里**；HEAD 态实测 60 条，B20 收工态 56 条。）

**C 别的 —— 1 条**：`AM4` `Righteous Gaze` —— **卡池里没有这张卡**，不是 id 问题 ⇒ **没回填**（它是真卡：卡图资产 `AstraMilitarum_strat_Righteous Gaze` 在解包资源里、`zh_cards.json` 有「正义凝视」、原版预组引用它 8 次；但 `hasStats=false` 被生成器跳过，本地**没有它的数值/效果文字来源**，见 §⑤-1）。

## ③ 改了什么

| 文件 | 改动 | 行尾 |
|---|---|---|
| `Unity/数据/游戏数据/card_ids.json` | **定点替换 22 个「值」**（脚本按字节改，⛔ 没整篇重写）+ note 追加一段「2026-10-17【B26 回填】」更正痕迹。`git diff --numstat` = **28/28**（22 我的 + 5 B20 的 + 1 note），键集合 996 不变、CRLF 1000/1000 原样 | CRLF ✓ |
| `RuleEngine/Resources/cards_engine.json` | **重跑** `gen_cards_engine.py` 产出（我没手编） | 单行 |
| `RuleEngine/Resources/{prebuilt_decks,tutorial_decks}.json` | **重跑** `gen_prebuilt_decks.py` 产出 | LF ✓ |

- **先做的对照实验**：改任何东西**之前**先重跑一遍两个生成器 ⇒ `prebuilt_decks.json` md5 **逐字节不变**（`5097f3e8…`）⇒ **流水线是确定性的、现有产物没过期**，后面所有变化都能归因到本次改动。
- `pick_id` 只认「**同前缀 + 纯数字 + 归一化名唯一命中**」⇒ 22 条逐条预检过「同前缀候选恰好 1 个」；生成器的 `_seen_ids` 撞车断言没炸（= 没有一张卡因此丢 id）：**原版 id 973 → 995（+22）· 自造 153 → 131（−22）**，±22 对得上、无丢失。

## ④ `prebuilt_decks.json` 的变化（逐条）—— **md5 `5097f3e89ce7288485961d2a70a6de9d` → `21d01b551b365190c73e35601ce4c421`（破了，且【是对的】）**

**103 副里 25 副有变化；`complete` 66 → 70；cardIds 净 +7 张（5 副）；其余是「同一张卡换 id」，不是换卡**（其中 **1 副 `SororitasDeck1` 既换 id 又补牌**）。（逐字段 diff 脚本输出在 `_b26_scratch/`）

- **(a) 补回了牌（5 副 / +7 张）** —— 之前是 `miss-known`（id 在表里、名字对不上池子）⇒ 现已解出：
  `OrksDeck4` +2 `GOF31` · `OrksDeck5` +2 `GOF31` · `SOR_SK_1` +1 `SOR30` · `SororitasDeck1` +1 `SOR30`（另 2 张 `SOR19` 是换 id）· `SororitasDeck5` +1 `SOR30`。
  **`complete` False→True 的 4 副**：`OrksDeck4` `OrksDeck5` `SOR_SK_1` `SororitasDeck1`（`problems` 由 `size:28/30` 等变空）。`SororitasDeck5` 补了牌但**仍不 complete**（它督军解不出，`heroId` 是空 —— 与本次无关的老账）。
- **(b) 督军被纠正（2 副）**：`GSC_SK_3` / `GenestealersDeck5` —— `heroId` 由 `GSC5`（`Primus Saffa Rhiannor`）改成 **`GSC1`（`Acolyte Iconward`）**，`heroNote` `疑似(子串)` → **`id命中`**。判据：这两副的 `heroId` 本来就是 **`GSC1`**，而 PnP 目录 `Genestealer Cult/1督军/` 里是 `Warpforge_01_Iconward-Malak-Vorenth.png` 与 `Warpforge_05_Primus-Saffa-Rhiannor.png` ⇒ **1 号就是 Acolyte Iconward**、5 号才是 Primus。⚠️ 这条同时证明 `warlord_ids.json` 的 `GSC1 = GSC_Warlord_Primus Saffa` **是错的**（见 §⑤-2，**建议复核**）。
- **(c) 只是同一张卡换 id（18 副 · 另有 `SororitasDeck1` 见上）**：`DA_SK_2` `DarkAngelsDeck3`(DA11×3) · `Deck_UM_Titus` `UMDeck5`(UM2) · `EC_SK_1` `EmperorsChildrenDeck1` `EmperorsChildrenDeck3`(EC6/EC4/EC2) · `TAU_SK_1` `TAU_SK_2` `TAU_SK_3` `TauEmpireDeck2` `TauEmpireDeck3`(TAU34/TAU19/TAU39) · `TauEmpireDeck6`(TAU45×2) · `UMDeck4`(UM45) · `UMDeck6` `UM_SK_1` `UM_SK_4`(UM72/UM75) · `UM_SK_5`(UM79)。**张数、顺序、`complete` 全不变**。
- **`tutorial_decks.json`**：md5 `8b8805327ecdc955ee817ad21824c650` → `490c7bfed57379fd0bc6f35d80b21f2a`。变化只有 3 处：`stage2/ai` `stage4/ai` `stage6/ai` 各把 `UM_Master_Tactician` 换成 `UM2`。**没多牌、`complete` 仍 2/12、`heroMismatch` 仍 0。**

## ⑤ 复验（命令与输出原样贴）

```
$ "D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/gen_cards_engine.py --check
收录        1126    defence=39  hero=56  tactic=445  unit=586
稀有度       common=330  epic=229  legendary=197  rare=320  special=50     ← 空稀有度 0
卡面修正表：所有键都匹配上了 ✅
中文名       1126/1126   中文效果 1120/1126   （缺 0 张、卡面回英文、不静默）
卡 id        用原版 995 张 · 自造 131 张（共 1126）                         ← 改前 973 / 153
  自造 id 按阵营：Ultramarines=60  Goff=26  SaimHann=13  BlackLegion=7  Genestealers=6  TauEmpire=6  DarkAngels=5  Sautekh=3  Sororitas=3  AstraMilitarum=2
--check：现有 …/cards_engine.json 一致 ✅                     # 幂等：连跑 3 次 md5 都是 b725996807f58597a0baacaeebbe0583
$ md5sum MyGame/Assets/RuleEngine/Resources/{cards_engine,prebuilt_decks,tutorial_decks}.json
b725996807f58597a0baacaeebbe0583 *cards_engine.json          # 改前 90376eb2a3e51629ea353b6f4c05d38f
21d01b551b365190c73e35601ce4c421 *prebuilt_decks.json        # 改前 5097f3e89ce7288485961d2a70a6de9d ← **硬约束破了，逐条理由见 §④**
490c7bfed57379fd0bc6f35d80b21f2a *tutorial_decks.json        # 改前 8b8805327ecdc955ee817ad21824c650
$ "D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/gen_prebuilt_decks.py   # 连跑两次
practice=103 complete=70 classic=49 classic_complete=32 no_cardback=0 hero_missing=7   # 改前 complete=66（其余逐项相同）
tutorial stages=6 decks=12 complete=2
$ git diff --numstat d:/4/Unity/数据/游戏数据/card_ids.json
28	28	Unity/数据/游戏数据/card_ids.json      # 无整篇翻转（CRLF 1000/1000）
```
「值 ≠ 卡池卡名」的条数：HEAD 态 **60** → 本次改动前 **56** → 改动后 **34**（= 33 B + 1 C）。

## ⑥ 没查清 / 判不了

1. 🔴 **`AM4 Righteous Gaze` 是真卡但本地无数值来源 ⇒ 判不了**（属**卡池缺卡**，不是 id 账）。它是真卡的四条证据：卡图资产 `…cardassets_assets_all/Sprite/AstraMilitarum_strat_Righteous Gaze.json` 存在 · 原版 `decklists.json` 引用 `AM4` **8 次**（练习池 4 次 ⇒ `AstraMilitarumDeck1/Deck5` 各缺 2 张、**永远不可能 complete**）· `zh_cards.json` 有「正义凝视」· `cards.json` 也列着它。**但**：`card_stats.json` 与 `cards.json` 两处的 cost/attack/health/desc/rarity **全是 null**、`card_stats.face` 也是 null（**没有卡面可读**）、`d:/2/Warpforge部队卡片` 里**没有这张的 PnP 图** ⇒ 「费用/三围/效果文字」**一条判据都没有**，**不许猜**。要补得先另找来源（建议另开账）。
2. ⚠️ **`warlord_ids.json` 与 `card_ids.json` 对 `GSC1` 的说法矛盾**（两条自建表打架，= A876 那一族）。`warlord_ids.json: GSC1 = GSC_Warlord_Primus Saffa` vs `card_ids.json: GSC1 = Iconward Malak Vorenth`。本次按 PnP 编号裁定后者（§④-b 的三条证据），**但 PnP 编号本身也只在我核的这两张上一致**（见下条）⇒ **建议复核**：`gen_prebuilt_decks.py` 的 `hero_match` 是「先按 id 直查、查不到才回落 `warlord_ids`」，我这次让 `GSC1` 进了卡池 ⇒ 那两副走 `id命中`。若 `warlord_ids` 才对，这两副就改错了。
3. ⚠️ **`card_ids.json` 的「编号列」可信度只到 84%，逐条没核过**（生成器注释也写着「基本可信、不是逐条验过」）。实测：`card_stats.face` 里的 PnP 编号 vs 池里 id 编号，可比 **985 条里 824 条一致（84%）**，161 条不一致（多是**扩展段**，例 `AM67`↔`Warpforge_02`、`ASH74`↔`Warpforge_01`）。6 张 UM 里 `UM2`/`UM45` 与 PnP 号对得上、`UM72/75/76/79` 对不上（`Warpforge_08/11/12/05`）—— 但**值这一侧是硬的**（`ocrName` / 卡图资产名 / 池名三处逐字一致，且旧值那个 `" 2"` 正是 PnP 文件名后缀）⇒ **id↔卡 这一对没问题，只是编号序号对不上**。本次**不动键**，仅记录。
4. 🔴 **下游要重跑（不在我白名单，我没动）**：
   - **卡立绘按 id 命名**（`Cards/art_<id>.png`，`CardArt.Portrait` = `CardArt.cs:360`，键取 `BattleDriver.ArtKey:8112`），这 22 张换了 id ⇒ **~~18 个~~ ⇒ 🔴 2026-10-17 订正（铁律 5 · B30 实测）：是【22/22 个】**现有立绘文件对不上了（`art_gsc_acolyte_iconward.png` 等，清单见 §② A 表右列小写）⇒ **要重跑 `工具/import_original_art.py`**；⚠️ 该脚本**不删旧文件**，那 **22** 个（+22 个 `.meta`，共 **29.4 MB**）会变孤儿（要不要删由调度台定）。
     > **错因**：写这份报告时**没有逐个 `stat`**，是按 A 表右列的小写名推的 —— 而右列只列了当时能对上的一部分。✅ **B30 已重跑并复验：22/22 新文件都在、且与旧文件【逐字节相同】**（只换名、没换画）。
   - `CardPresentation/Resources/card_icon_plan.json`（9 个旧 id）· `voice_lines.json`（9 个旧 id）按旧自造 id 建了键 ⇒ 要重跑各自生成器（`gen_icon_plan.py` / 语音那条链）。
   - `.cs` 里出现的旧 id **全是注释**（`EffectText.cs:7466` · `EffectResolver.cs:392,3927` · `BattleContext.cs:428` · `RuleCore.cs:764` · `RuleEngineTest.cs:6634,11418`）—— 逐个核过、**没有一处是字符串常量比较** ⇒ **不会编不过、不会行为变**。⚠️ `RuleEngineTest.cs:6634` 那处自己写着「`FindCard` 比的是 `Name`」，它按**卡名**查卡 ⇒ 不受 id 变化影响。
5. ⚪ `d:/4/_tmp_view/card_ids_made.txt` 被生成器**重写**（自造 id 全量清单，本就是它的 scratch 产物，不进 git）。
6. ⚪ **归属提醒**：`工具/gen_prebuilt_decks.py` 在工作区里相对 HEAD 有 **+322/−67** 的改动 —— **那是 B6/B16 加教程那一路留下的，不是本次**（我只**运行**它、没改它一个字节）。本次动过的文件**只有 4 个**：`card_ids.json` · `cards_engine.json` · `prebuilt_decks.json` · `tutorial_decks.json`（后两者是重跑产物）。
