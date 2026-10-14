# M · 卡牌线 P1 相：id 置换批（`A1782` + `A1778` + `A1783` + `A1800`）—— 施工报告

> 2026-10-14 第三十一会话 · 执行代理 M。施工图 = `资料/普查产出_第三十会话/卡表块_施工设计_1014.md`（整篇读过，判据照它的 §一/§二/§三/§五/§七）。
> ⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未改任何正本 · ✅ 秒级 `typecheck` 0 错。

---

## 一、结论（一句话）

**全池 1126 张卡里 171 张换了 id（从「自造名样式」/「PnP 旧表残留」换成原版真值）**：
**58 张拿到真值短 id（`前缀+数字`）+ 113 张拿到真值 32 位 GUID**；**23 张（全是 Ultramarines）真值快照里没有 ⇒ 保持自造**。
卡池 **1126 张 / 1126 个唯一 id / 0 重 id / 卡名与换 id 前逐张一致**（只有 id 变）。四笔账**同批一次做完**（⛔ 没分次）。
落地 = 改 `card_ids.json`（+259 条 GUID 行）**＋**新开一层**真值索引**（判据只有真值侧有，见 §三）**＋**重跑 **10 个以 id 为键的生成器**（⛔ 是一条链、不是一个命令）。

---

## 二、四笔账各算一笔

| 账 | 改成多少张 | 依据 | 落在哪 |
|---|---|---|---|
| **`A1782`** | 自造 id 共 **187** 张，其中 **164** 张换到真值（**58 短 + 106 GUID**）；**23** 张不动 | 级联 join（见 §三） | 卡池 `cards_engine.json` 的 `id` 列 |
| **`A1778`** | **10 个缺口 + 8 督军，10/10 全部落位**（都在上面那 58 张短 id 里） | 同上；`A1783` 那条「⛔ 不能当新增做」的裁定没被推翻 | 同上 |
| **`A1783`** | 6 张里 **5 张是我们既有的卡**（4 张靠 `ocrName` 别名 + `EC30` 就是我们的 `EC_Maulerfiend`）⇒ 拿到真值 id；**只有 `TL40` 我方确实没有**（见 §六·1，本轮**未加**） | 同上 | 同上；**卡池 `count` 前后差值 = 0** |
| **`A1800`** | **106 张**自造 id → 真值 **GUID**；另 **7 张**持「与真值撞名的旧表残留 id」的卡也换成真值 GUID（合计 **113 张 GUID**）；`card_ids.json` **+259 条 GUID 行** | 施工设计 §七·② 已裁「GUID 就是合法卡 id」 | `card_ids.json` 的 `mapping` + 卡池 `id` 列 |

**`A1778` 逐条现核（都在换 id 名单里）**：`BL_Hellfire_Torch→BL57` · `BL_Hellfire_Pit→BL58` · `DA_Grim_Effigy→DA59` ·
`DA_Azrael→DA1` · `DA_Belial→DA2` · `DA_Asmodai→DA3` · `BL_Sylar_Hexcorn→BL36` · `AM_Commissar_Elan→AM3` ·
`AM_Hektor_Thenmann→AM66` · `GSC_Acolyte_Iconward→GSC3` · `GSC_Lhaska_Szenari→GSC66`。
🔴 **已裁 ①（`Helfire`/`Hellfire` 拼写）被遵守**：**卡名一字未改**（`Hellfire Torch`/`Hellfire Pit` 保持），只换了 `id`。

**`A1783` 逐条现核**：`GOF_Veteran_Flyboy→GOF20`（`ocrName = Veteran Stormboy`）· `SAU_Gauss_Reaper_Warrior→SAU4` ·
`SAU_Tesla_Carbine_Immortal→SAU6` · `SAU_Gauss_Blaster_Immortal→SAU8` · `EC_Maulerfiend→EC30`。
⚠️ 施工设计 §二·2 说「只有 `EC30`/`TL40` 我方真没有」——**`EC30` 那半句不成立**：我们有它，只是自造 id `EC_Maulerfiend`（现读卡池确认），
所以 §二·2 与 `新发现_待立账.md:79`（那张表本来就把 `EC30` 与 `EC_Maulerfiend` 配了对）**两处口径冲突，按数据判 §二·2 错**。

---

## 三、判据：为什么在 `card_ids.json` 之外**必须**多一层真值索引

只改 `card_ids.json` **做不成这三笔**（三条都实测过，不是推断）：

1. **我们池里 32 张卡的卡名是「美术/VO 资产名」**（Godot 期 `d:/warpforge/data/cards.json` 带过来的）——
   `Commissar Elan` ↔ 官方 `Commissar Denkler`、`Veteran Flyboy` ↔ `Veteran Stormboy`、`Sister Dogmata` ↔ `Dogmata`…
   ⇒ 「按卡名逐字查表」**一定落空**（`A1779` 查实的根因）。
2. **259 张真值卡的 `uniqueId` 是 32 位 GUID**，而 `pick_id()` 只认「前缀 + 纯数字」⇒ 表里收了也挑不出（`A1800` 第一根因）。
3. **同名组里「基准卡」与「附魔/教程副本」同名**（`Uge Choppa` = 本体 + `_enchantment`；`Storm of Silence` = 本体 + 2 副本）
   ⇒ 按名取会把副本当本体。

⇒ 落地成 **`工具/gen_card_id_truth_index.py` → `Unity/数据/游戏数据/card_id_truth_index.json`**（180 KB，新件）：
把设计 §`A1800` 那张**级联 join 表**（①官方 `Card_Name/*` 英文值 → 键 ②真值 `m_Name` 尾巴 ③真值 `cardName`）**算成索引**，
外加两个只有真值侧能给的量：**`base`**（基准卡 = `m_Name` 归一化以 `cardName` 归一化**结尾**）与 **`army`**（真值 `cardArmy`，与源表 `factionId` 同口径）。
`gen_cards_engine.py` 分 id 时**先查它、查不到才退回**「按卡名查 `card_ids.json`」那条老路（`pick_truth_id`，口径写在函数 docstring + `TRUTH_IDX_SRC` 注释里，标了判据出处）。

**验收（可复算）**：把同一套判据套到**全池 1126 张**上逐张比 ⇒ **只有 171 张变、其余一字不动**；两种优先序（先基准卡 / 先短 id）**结果逐张相同**；新 id 之间、以及新 id 与保留 id 之间**0 冲突**（`_tmp_view/p1_1014/sim_apply.py`）。
**逐张核过**：171 张里 **171 张**至少有**一个**名字侧旁证（我方 `name` 或 `ocrName` ⊗ 真值 `cardName`/`m_Name` 尾巴/官方英文名）；
另有**独立的第二把钥匙**（真值 `cardImageReference` → sprite 名 ↔ 我们 `card_stats.art` 的立绘名）：**155 OK / 13 n-a / 3 DIFF** ——
13 个 n-a = **我们源表 `art` 列为空**（正是那些靠 `ART_ID_ALIAS` 点名的卡，没得比）；3 处 DIFF 逐条查明**都不是错配**：
`EC25 Terminator` 是**我们源表 `art` 列写错了**（指向 UM 的立绘，见 §六·2）· `Righteous Repugnance` 是**改名卡**（立绘资产沿用旧名 `Purgator Mirabilis`）·
`Skorflense` 是**真值那一侧指错**（它的 `cardImageReference` 指到卡框 `40k_Cardframes_stratagem_Emperor Children_tier4`，见 §六·10）。

---

## 四、以 id 为键的下游生成器 —— 逐条（都跑了，rc 全 0）

🔴 **链有先后、而且有【不动点】**：`gen_card_inventory_options` / `gen_card_initiative` 的键是**我们卡池的 id**，
   ⇒ 先重出卡池 → 再重出这两张表 → **再跑一次卡池**（第一趟有 118 张会掉成 `-1`「未知」，第二趟才收回来）。
   实测：`inv`/`initiative` 覆盖 **837 → 1008 / 1126**（`+171` 的 53 张本来就查不到、118 张是这一趟收回来的）。

| # | 生成器 | 跑了吗 | rc | 产物 / 读数 |
|---|---|---|---|---|
| 1 | `工具/gen_cards_engine.py` | ✅ **两次**（跑完 id 键表再跑一次收口） | 0 | `cards_engine.json`（1126 张 · 唯一 id 1126 · 自造 23）· **`id_truth` 那一层 1008 张** |
| 2 | `工具/import_original_art.py` | ✅ | 0 | **插图 1126/1126 配上 · 配不上 0**（`ART_ID_ALIAS` 15 个键同步换算了，见 §六·5） |
| 3 | `工具/import_original_audio.py` | ✅ | 0 | `voice_lines.json` 重出（**已拷 0 条 / 已存在跳过 1848 条**，音频本体没动） |
| 4 | `工具/gen_icon_plan.py --write` | ✅（`rc=0` 才落盘） | 0 | 两份 `card_icon_plan.json`（数据 + 运行时）· 灭自证 / 位置锁全过 |
| 5 | `工具/gen_card_vfx_by_card.py` | ✅ | 0 | `card_vfx_by_card.json` ×2 |
| 6 | `工具/gen_prebuilt_decks.py` | ✅ | 0 | `prebuilt_decks.json` + `tutorial_decks.json`（**103 副**；`complete` 84 → 86，见 §六·4） |
| 7 | `工具/gen_card_inventory_options.py` | ✅ | 0 | `map 1008 · unknown 118`（`-1` 那批 = SW/UM/DA 快照后新卡） |
| 8 | `工具/gen_card_initiative.py` | ✅ | 0 | `map 1126 · unknown 118` |
| 9 | `工具/gen_related_cards.py` → `工具/gen_talent_cards.py` | ✅ | 0 | 两份 `related_cards.json` / 两份 `talent_cards.json`（**不在施工图那 6 个里，但同样以 id 为键 ⇒ 一并重出**） |
| 10 | `工具/gen_ability_anim_durations.py` · `工具/gen_tutorial_stages.py` | ✅ | 0 | 两份 `ability_anim_durations.json` / `tutorial_stages.json`（同上，一并重出） |

**顺手改了 3 个工具里写死的旧 id**（落地链第 6 步，判据 = `资料/卡牌id_换真值_落地记录.md` §三·6）：
`import_original_art.py` 15 处（整表按字节替换）· `gen_icon_plan.py` **60 个键 / 107 处** · `gen_tutorial_stages.py` 2 处 —— 都留了带日期的注释，**值/答案一字未动**。

---

## 五、🔴 逐张对拍（`资料/普查产出_第三十一会话/M_卡牌P1_逐张对拍.tsv`，171 行）

| 项 | 结果 |
|---|---|
| **卡图** | 卡池每张都有图 **1126/1126**；171 个新 id **全部有图（171/171）** |
| **旧 id 的图** | 🔴 **171 个 `art_<旧id>.png`（+.meta）仍留在盘上 = 孤儿** —— 导入器**从不删旧文件**（读过源码：没有 `os.remove`/`unlink`）。逐条列在 TSV 第 7 列。⚠️ **上一批（2026-10-13）也留了一批**（拿 `_tmp_view/cardid_bak_1013/` 比：还有 40+ 个不属于本批的旧 id 图）⇒ **这是既有口径，本轮照旧不删**（删文件是破坏性操作，交给主对话裁） |
| **按 id 建键的产物：旧 id 残余** | **0 / 0 / 0 / 0 / 0**（`voice_lines` · `card_icon_plan`×2 · `card_vfx_by_card` · `card_inventory_options` · `card_initiative` · `related_cards`×2 · `talent_cards`×2 · `ability_anim_durations`×2 · `tutorial_stages` · `prebuilt_decks`）—— 全库 `grep` 级复扫，**只有下面三处例外** |
| **新 id 命中率** | `card_vfx_by_card` **171/171** · `card_inventory_options` **171/171** · `card_initiative` **171/171** · `card_icon_plan` **123/171** · `voice_lines` **33/171** |
| **两处「不满」是不是缺口** | ✅ 都不是：`card_icon_plan` 只收**卡面有记号**的卡 ⇒ 缺的 **48 张 `desc` 里 0 个记号**（逐张核过）；`voice_lines` 只收**有语音**的卡 |
| **三处例外（旧 id 还在）** | ① `card_ids.json` **7 行**（`EC72..EC77` = 旧表残留，**本轮故意保留**，见 §六·6）② `warlord_ids.json` **4 个值**（见 §六·6）③ **`.cs`/one-off `.py` 里的字面量**（见 §六·3，⛔ 白名单外） |

---

## 六、哨兵换了没 + 顺手发现（⛔ 只报未改）

**哨兵：不用换（已现核）**。`SentinelCard1="UM64"` = `Angels of Death`（`desc` 里 `Primaris Intercessor` 仍在）·
`SentinelCard2="SOR71"` = `Adelaide the Serene`（`desc` 以 `6 ☀` 开头）—— 两张卡的 **id 与依赖字段都没变**，
且**没有任何新 id 撞上这两个键**（`tools/ruleprobe.sh check` 实跑：两条哨兵**都绿**）。

1. 🔴 **`TL40 Infestation Node` —— 本地可做，但本轮【没做】（要主对话裁/另派）**：它的判据**齐到能直接建卡** ——
   官方名 `Infestation Node`/`感染节点` · 官方描述 en+zh（`Heal 2 to a friendly troop and create a copy of it in your hand`）·
   真值 `manaCost=2 / cardType=20(Tactic) / cardRarity=3` · 立绘**本地有**（`assets_full/bundle_tyranidsleviathancardassets_assets_all/Texture2D/Tyranid_Leviathan_strat_Infestation Node.png`）。
   **没做的理由（要写清，别当逃生的口子）**：加一张卡 = 往**不是 id 置换批**的源表（`card_stats.json`，PnP OCR 源）插一行，
   卡池 `count` 会变成 **1127**（施工图的验收写的是「预期 **2 或 0**」），**并且**会把 `RuleEngineTest.cs` 里那批
   与卡池规模/卡组相关的断言顶红 —— 那个文件**在本代理白名单外、且此刻有别的写手在动** ⇒ 这一趟加进去**没人能收红**。
   ⇒ 按「⛔ 卡住就停手写进报告」处理：**建议另立一笔**（判据已齐，见上）。
2. ⚠️ **`card_stats.json` 的 `art` 列有一处错的**：`EC_Terminator`（`Terminator`，帝皇之子）的 `art` 指向
   **极限战士**的立绘 `SM_UM_inf_Terminator.png`，而真值/官方两份都说它的立绘是 `CSM_EmperorsChildren_inf_Terminator`。
   ⇒ 独立第二把钥匙正是这么发现的（`149 OK / 2 DIFF` 里的一处）。**本轮不动**（源表不在白名单）。
3. 🔴 **`.cs` 里有 37 处旧 id 字面量（9 个文件）—— 本批留下的必修件**（`A1777` 早就预告过这一族）：
   `RuleEngineTest.cs` 14 · `CampaignData.cs` 7 · `CardPresentation/Editor/DeckScene.cs` 6 · `BattleScene.cs` 4 ·
   `NetSelfTest.cs` 2 · `CollectionWindow.cs` 1 · `TutorialModePopup.cs` 1 · `RuleEngineTest_S14.cs` 1 · `RuleEngineTest_S23_Parse.cs` 1。
   逐条（文件:行号 → 旧 id → 新 id）落在 `_tmp_view/p1_1014/stale_cs_py.tsv`。**⛔ 白名单外，一行没动**。
   ⚠️ **预期后果**：`RuleEngineTest.Run` 这一趟会**红**（2026-10-13 那次同样是 14 条红、全是「断言里写死旧 id」）——
   这是**已知必修**、不是本批的回归。另有 3 个**一次性历史脚本**也有旧 id（`_fix_a6_descZh.py` 10 · `_fix_a7_talent.py` 2 · `_verify_fixes.py` 2），同上未动。
4. ⚠️ **`prebuilt_decks.json` 有 2 副从「不完整」变成「完整」**（`AM_SK_1` · `AstraMilitarumDeck1`；`complete` 84 → 86）——
   是 id 对齐的**自然收益**（原版牌组引的就是这些真值 id）。**要不要保留这个变化请主对话看一眼**（预组页会多列 2 副）。
5. ✅ **`ART_ID_ALIAS` 是本批的隐藏依赖**：它按**我们的 id** 点名 24 条贴图表，卡池一换 id 就有 11 张卡**从「有图」掉成「配不上」**（`Exemplary Warrior`/`Chosen of the Four`/`Dark Pact of …`/`Armoury of Excess`/…）。
   已按落地记录那套「旧 id → 同名同阵营新 id」**换算 15 个键**，换算后 **1126/1126 全配上**。
6. ⚠️ **旧表残留（123 行）本轮全部保留**（⛔ 没删）：`DA65`/`DA66`/`DA75`/`SW1..SW58` 这些 id **`decklists.json` 原版数据自己还在引用**
   （现读 62 个），删了会把预组牌那座桥打断。⚠️ 副作用：`EC72..EC77` 那 **7 行**现在**池内已不用**（那 7 张卡这趟拿到了真值 GUID）——
   留着只为桥，**要不要清请主对话裁**。另：`warlord_ids.json` 有 **4 个值**（`DA_Azrael`/`DA_Belial`/`DA_Asmodai`/`TL_Neurogaunt`）是旧 id，
   **不在白名单、语义还不一致**（它自身 `TL74→TL_Neurogaunt` 与真值 `TL66=Neurogaunt` 也对不上）⇒ **只报未改**。
7. ⚠️ **真值 `cardName` 列本身有陈旧值**（实测一处）：`e59ac9e46f4ea804784e954c3b820e77` 的 `cardName` 写的是 `Path of the Warrior`，
   而它的 `m_Name`（`Z-NC SaimHann-Skyborne Deployment`）与官方 `Card_Name`（`Skyborne Deployment`）**都说它叫 Skyborne Deployment**。
   ⇒ 本批的消歧**最后一步**（候选只剩一个就取它）正是为它兜的底；**记一笔**：以后凡「拿真值 `cardName` 当权威」的地方要留这个心眼。
8. ⚠️ **官方中文表有一处串行**：`Card_Name/BL36` = (`Sylar Hexscorn`, **`疯狂之法`**) —— 英文是督军名、中文却是另一张卡的名字。
   本批让 `BL_Sylar_Hexcorn` 拿到 `BL36`，于是这条**第一次进入了我们卡池**（`nameZh` 变成「疯狂之法」）。
   ⛔ 未动官方表（生成物），**并入 `A1956` 那一族**（官方中文值错的清单）。
9. ⚠️ **卡池里 23 张 Ultramarines 仍是自造 id**（`UM_2nd_Company_Terminator` 等）—— 真值快照（2025-07-30）里**整个 UM 阵营**都没有
   （`A1800` 已查过：真值 `m_Name`/`cardName`/官方表/`guid_map`/`decklists` 全 0 命中）⇒ **属预期，不是缺口**。
10. ⚠️ **真值里有 2 张卡的 `cardImageReference` 指到【卡框】而不是立绘**：`EC62 Aural Hijack` 与 `4eec3b13… Skorflense`
   → 都指 `40k_Cardframes_stratagem_Emperor Children_tier{2,4}`（`guid_map.tsv` 里就它们俩，`cardImageReference` 共 2,342 行）
   ⇒ **凡拿 `cardImageReference` 当「这张画的立绘是哪张」的判据（`A1770` 那把钥匙）在这 2 张上会拿到错图**；
   我们源表 `art` 列给的才对（`CSM_EmperorsChildren_strat_Elixir_Skorflense`）。⛔ 未动任何东西，**只报**。

---

## 七、没查清的部分（⛔ 没猜）

1. **`.cs` 那 37 处要不要按同套换算**（判据齐、只差谁敢改）—— **白名单外，没动**（§六·3）。
2. **`TL40` 该不该进池**（本地判据齐，见 §六·1）——**要主对话裁**；本轮按「不发明口径」停手。
3. **171 个孤儿 `art_<旧id>.png` 删不删**（上一批也没删，见 §五）——**要主对话裁**。
4. **`card_ids.json` 里 `EC72..EC77` 那 7 行死残留 + `warlord_ids.json` 4 个旧值**要不要清 —— **要主对话裁**。
5. **`Warpforge_code`/真值快照里 GUID 卡 259 张中 146 张仍不在我们池**（环境/附魔类 SO）—— 与 `A1799`/`A1800` 的后续账有关，**本轮没碰**。

## 八、本批动了哪些文件（**⛔ 一行没碰**引擎源码 / 表现层 / 指纹 / `cardface_fixes.json` / 两张正本）

| 类别 | 文件 |
|---|---|
| **数据（新）** | `Unity/数据/游戏数据/card_id_truth_index.json`（180 KB） |
| **数据（改）** | `Unity/数据/游戏数据/card_ids.json`（`mapping` 1058 → **1317**：+259 条 GUID 行；`note` 补了本批的来龙去脉） |
| **生成器（新）** | `Unity/工具/gen_card_id_truth_index.py` |
| **生成器（改）** | `Unity/工具/gen_cards_engine.py`（+真值索引那一层，97/14 行）· `import_original_art.py`（18/15）· `gen_icon_plan.py`（109/107）· `gen_tutorial_stages.py`（4/2） |
| **产物（重出）** | `cards_engine.json` · `Resources/Art/cards/*.png`（1126）· `voice_lines.json` · `card_icon_plan.json`×2 · `card_vfx_by_card.json`×2 · `prebuilt_decks.json` · `tutorial_decks.json` · `tutorial_stages.json` · `card_inventory_options.json` · `card_initiative.json` · `related_cards.json`×2 · `talent_cards.json`×2 · `ability_anim_durations.json`×2 |
| **一次性脚本（留证据）** | `Unity/工具/_oneoff_1014_p1/`：`build_picks.py`（算判据表 + 双钥匙核）· `apply_card_ids_guids.py` · `remap_hardcoded_ids.py` · `check_orphans.py` |
| **中间产物** | `_tmp_view/p1_1014/`：`picks.tsv`（171 行判据）· `idmap.json`（旧→新）· `sim_apply.py`（全池模拟）· `stale_cs_py.tsv` · `bak/`（卡池/卡表/各产物重出前的键快照） |

## 九、验了什么（读数）

| 验法 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_p1 bash 工具/typecheck.sh` | ✅ **运行时 0 错 · 编辑器 0 错** |
| `bash 工具/ruleprobe.sh check`（快内环，⛔ 未重钉指纹） | rc=1 · **`新增 171 · 消失 171 · 改名 0 行`**（= 干净置换）· 解析差异 **1 行 = `GSC29`**（另一笔 `A1989` 的预期结果，**不是本批的**）· 卡池 md5 现值 `6827156b331e2a7519d7c40f0bb279fa`，**三个指纹待主对话统一重钉** · 两条哨兵**都绿** |
| 卡池自检 | 1126 张 · 唯一 id 1126 · 0 重 id · 自造 id 23（全 `UM_`）· 新旧同名逐条一致 171/171 |
| 逐张对拍 | 见 §五（`M_卡牌P1_逐张对拍.tsv`） |
| ⛔ Unity 批处理 | **一次没跑**（同工程单实例，收口自检由主对话做） |
