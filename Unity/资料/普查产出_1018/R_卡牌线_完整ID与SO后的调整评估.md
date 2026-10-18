# R · 卡牌线：拿到完整卡牌 ID 与 SO 之后的调整评估（只读 · 2026-10-18）

> 只读代理产出（无写文件工具 ⇒ 由主对话原样落盘）。

## 1. 一句话结论

**ID 部分已经彻底解决**（池 1,127/1,127 的 `id` 都已是当前 build 的 `uniqueId`，自造 0；
C# 里 192 处 id 字面量 0 处陈旧）；**任务侧 T01 的「A2075 / A2072 / A1869 等远端」整族可以关账**，
T02/T07 的一批「等新包」判据也齐了。**代码不需要大改** —— 真正的调整只有两处：
把 id/描述键的「按名猜」降为守卫（改走 uniqueId / `cardDescriptionTextReferenceId`），
以及三个仍读旧 2025 快照的派生生成器。**唯一的实质数据分歧（60 项数值，49 项是督军 −10 血）仍未裁定，⛔ 现在别切 SO。**

## 2. SO 字段清单（`RawCardScript`，1,351 实例，31 个序列化字段 + `full_raw` 116 字段）

| 字段 | 类型 | 样例 | 真值性 / 覆盖 |
|---|---|---|---|
| `uniqueId` | str | `EC29` | **真值 id**，1351/1351 非空、0 重复；短号或 32-hex |
| `cardName` / `m_Name` | str | `Heldrake` / `33 EmperorsChildren-Heldrake` | SO 内部名，**不是显示名**；108 组重名 ⇒ 不能当键 |
| `cardArmy` | int | `120` | 阵营码，13 值，1:1 前缀 |
| `manaCost` / `meleeAttack` / `rangeAttack` / `maxHealth` / `armour` | int | `8/7/5/6/0` | 数值真值；`manaCost=-1` = 不可直接打出（105 张） |
| `cardType` / `spellType` | int | `0/0`（Minion）、`20/210`（Defence） | 类型真值，1009/1009 无例外 |
| `cardRarity` | int | `2` | 1..5 = common/rare/epic/legendary/special；0=无值（4 张） |
| `keyword` | int | `10` | **兵种枚举**（5 Infantry/10 Vehicle/15 Monster/25 Battlesuit/30 Drone/35 Beast/40 Daemon），613 张非 0 |
| `cardShortId` | int | `0` | ⛔ 不是卡 id，别拿来换 |
| `CardTraits[]` | list | `{name:"rally",_trait:10,_traitValue:0,_hide:0}` | **关键词/trait 真值**，750 张非空 |
| `cardAbilities[]` | list | 11 键结构，`abilityLogic[].chosenAbility` = 效果枚举 | **效果"列表"真值**：池内 1001/1127 有非 0 效果码、48 种效果码、57 种 trigger |
| 同上 `filterSpecificCards` | PPtr[] | 指向具体卡 | 18 张卡有结构化"指定某卡"引用 |
| 同上 `maxTargets` | int | `1/2/3/4/5` | **317 张 >0**（分布 291/10/13/1/2） |
| `activeAbility` / `alternativeAbility` | dict | 含 `manaCost`/`targetCriteria` | 88 张有主动技 |
| `cardImageReference` | GUID | `0d720e0b…` | **立绘真值**；全 1351 中 23 张空；池内 1127/1127 已解进 `card_vfx_by_card.json` |
| `initiative` | int | `20` | 先后手真值（1190/1351 = 20） |
| `inventoryOptions` / `showInCollectionOptions` | int | `0/5` | 卡组资格真值 |
| `expansionPack` | int | `0` | 扩展包 |
| `fullHeroSpriteReference` | ref | 65 张非空，44 张带资产名 `UI_Deck_Warlord_…` | 督军整幅立绘，部分可解名 |
| `relatedCard1..4` | PPtr | 212/36/12/6 张 | 相关卡真值（结构化） |
| `goSecondCardInHand` | PPtr | 64 张 | 督军后手防御卡 |
| `cardDescriptionTextReferenceId` | str | `TL66_new` / GUID | **44 张指定描述键**，44/44 可解；15 张与基础键 `Card_Description/<id>` 不同 |
| `itemDescriptionKey` / `activeAbilityTextReferenceId` / `targetingTextReferenceId` | str | `Item_Description/OffenceCardExplanation` | 90 / 3 / 51 张 |
| `refNameId` / `nameModifier` / `altArtFromId` | str | 100 / 0 / 0 张 | 名覆盖/别名 |

**SO 里没有的**：① **没有任何效果描述文本字段**（只有 I2 键引用；EN/ZH 正文在**另一个 localization bundle**，
K07 已并入，`Card_Description/*` 1,258 键、池内 1125/1127 有 EN、1122 有 ZH）；② 没有 `subtype` 字符串（只有 `keyword` 枚举）；
③ 没有阵营名字符串（只有码）；④ 没有卡面中文/图标/几何。

## 3. 任务调整表

| 任务/账 | 现状 | 有了 ID+SO 后该怎么改 | 优先级 |
|---|---|---|---|
| **T01 `A2075`**（普通卡 id 本机拿不到·等远端） | ⛔ 等远端 | **关账**：当前 build `uniqueId` 1351/1351 已解 | P0 |
| **T01 `A2072`**（118 张 id 我们自己排·等远端） | ⛔ 等远端 | **关账**：K06 已落 114 改 id + 4 确认，池 0 自造 | P0 |
| **T01 `A1869`**（171→118 接不上真值） | ◐ 等远端 | **关账**：现池 1127/1127 全在当前包 | P0 |
| **T01 `A1880`**（256 张 GUID uniqueId） | ✅ 规则已现成 | 改写成"SO 事实"；索引已按当前包重出（`card_id_truth_index.json` 06:57） | P2 |
| **T01 `A2068`**（立绘真值链） | ✅ 已做 | 补一条覆盖率断言：`card_vfx_by_card.byCard._assetRefs` 现 1127/1127，按名兜底已成死路 | P2 |
| **T01 `A2053`**（Veteran Flyboy/Stormboy） | 需拍板 | 判据已齐（SO `cardName`+`Card_Name/GOF20` = `Veteran Stormboy`）⇒ 只剩用户拍板 | P2 |
| **T01「20 个自造残留短 id 键」** | 留给用户拍板 | **关账**：新 `mapping` 为 `{uniqueId: 卡名}`，0 个含 `_` 的残留键 | P0 |
| **T02 `A1963`**（maxTargets 4 张+13 新卡未知） | ◐ 转战斗线 | **数据侧全齐**：SO 317 张有 maxTargets；只剩 `BattleDriver.cs:6079-6084` 接线（战斗线） | P1 |
| **T02 `A2073`**（关键词系统遗漏） | ◐ 只清了点名族 | 可做**全池 1127**：用 `CardTraits` 逐卡对账（我们 `keywords` 把 trigger+effect 拼成一串，需按前缀比） | P1 |
| **T02 `A2012`**（真值陈旧值、预组数） | ✗ | ①SO `cardName` 已换新；②预组 `complete:false` 已 0，旧「84/86/87」读数作废 | P2 |
| **T02 `A1938①`**（92 条 ocrSrc 空） | 待裁 | 与 SO 无关（OCR 源表账），SO 不能填 `ocrSrc` | P3 |
| **T03 `A1957`**（`$$id$$` 展开） | ✗ | **可做**：182 条描述含 `$$…$$`、141 个唯一 id、**0 个解不出** | P1 |
| **T03 `A2117`**（官方中文换没换） | ✗ | 用当前表重测：EN 1125/1127、ZH 1122/1127；`descZh` 与官方差 429 = 271 条人工层 + 158 条官方为纯宏/未译 | P1 |
| **T03 `A1827`/`A1842`**（旧卡名 89 个/16 处） | ✗ | 仍须逐条人判，但现在有官方 ZH 可对照 | P3 |
| **T06 `A2056`**（4 副 complete:false 无理由） | ？ | **可关**：`prebuilt_decks.json` 现读 103 副、incomplete **0** | P1 |
| **T06 `A2057`**（页上副数 87 对不上） | ？ | 数据侧已闭；页面数**只有真机可验** ⇒ 挂到验收期 | P3 |
| **T07 `A2032`**（28 张同名多候选） | ✗ | **关账**：按 `uniqueId` join | P1 |
| **T07 `A2050`**（按英文名找键的假阳） | ✗ | **关账**：键必须 = `Card_Description/<uniqueId>` | P1 |
| **T07 `A2037③`**（117 张不在真值快照） | ？ | **关账**（当前包覆盖） | P1 |
| **T07 `A2037①`**（EC22 两张差 2px） | ？ | 仍查不到判据（SO 两对象 `cardImageReference` 可再比） | P3 |
| **T07 `A1849`**（两条近名模糊匹配） | ？ | **可裁**：id 已唯一，`SAU38` / `GOF12` 直接定 | P2 |
| **T07 `A2033`**（fullHeroSprite GUID 解不出名） | ✗ | 部分可解：65 条非空、**44 条带 `m_SubObjectName`**，剩 21 条仍只有 GUID | P3 |
| **新增 N1** | — | 把 SO 结构化效果（`cardAbilities`）/`relatedCard1..4`/`filterSpecificCards` 落成卡表列（只落值） | P2 |
| **新增 N2** | — | 用 `cardDescriptionTextReferenceId` 作描述真值键 | P2 |
| **新增 N3** | — | **32 张卡名 ≠ 当前官方显示名** ⇒ 需拍板，⛔ 别机械改 | 待拍板 |
| **新增 N4** | — | **60 项数值分歧**（49 health 多为督军 −10、10 cost、1 melee）；前置 = 查 `SetupWarlordsLife` 常量，**未裁前不切** | P1（只查不改） |

## 4. 代码调整表

| 文件:行号 | 现状 | 建议 | 影响面 |
|---|---|---|---|
| `工具/gen_cards_engine.py:767-870`（`IDS_SRC`/`load_ids`/`pick_id`） | id 按**归一化卡名**查 `card_ids.json`；`card_stats.json` 无 id 列 | 给源表加 `uniqueId`（1125/1127 可按 (阵营,名) 唯一落位，另 2 张 `SAU35/SAU37` 有已知别名），id 层改直查；名字只作守卫 | 仅 id 分配层；今日输出不变 |
| `工具/gen_cards_engine.py:885-922`（`pick_truth_id`） | 名级联 `by_official_en→by_tail→by_name` | 先按 uniqueId 直取，级联降为交叉校验/报警 | 同上 |
| `工具/gen_cards_engine.py:1793-1807` | 三条路都可能落 `make_id`，只记账不出声 | 加断言：池内 `id_made == 0`，且每个 id ∈ SO 集合 | 防静默退回自造 |
| `工具/gen_cards_engine.py:1490-1581`（`load_official_desc`） | 用 `<id>` 键 + `_new` 后缀**启发式**取代 | 改读 SO `cardDescriptionTextReferenceId`（44 张），缺省再退 `<id>` | 防变体/教程副本取错 |
| `工具/gen_cards_engine.py:1161-1215`（A1799 十列） | 缺值写 `-1`/`""`，注释仍写"118 张缺" | 覆盖率改成 1127/1127 | 文档+守卫 |
| `工具/gen_cards_engine.py:1870-1884`（`keywords`） | 真值 `CardTraits` **只补空** | **不建议改**（PnP 是内容尺子，铁律 7） | — |
| `工具/gen_cards_engine.py:1848-1868`（督军 cost） | 只有督军取 SO `manaCost` | **暂不扩**：等 60 项数值裁定 | — |
| `工具/gen_talent_cards.py:40` | `TRUTH` 仍指 **2025-07-30 旧快照** | 换指 `allcards_1350/cards_raw.json`；现表 89 条，当前包 trigger 46 有 105 张（池内 96） | 天赋映射 +7~16 条 |
| `工具/gen_related_cards.py:76` | 读**旧 profile bundle** | 换指 `cards_raw.json`；当前包 `relatedCard1` 212 张 vs 现表 196 | 相关卡 +16 张 |
| `工具/gen_ability_anim_durations.py:206` | `cardSoTruth` 指旧快照 | 换指当前包，补齐新卡动画时长 | 表现层时长表 |
| `工具/gen_card_id_truth_index.py:44` | **已指当前包** | 无需改 | — |
| `工具/import_original_art.py:289-349` | 真值链 + 按名兜底 | 兜底已成死路 ⇒ 加"兜底命中数应为 0"断言 | 立绘导入 |
| `RuleEngine/Data/CardDatabase.cs:32-33` | 注释仍说 id 可能是 `AM_Some_Card` 自造 | 注释订正（0 自造） | 仅注释 |
| `RuleEngine/Core/CardDef.cs:55-57` | 注释写死"0 / 1126"等计数 | 计数改指针/现读 | 仅注释 |
| `CardPresentation/Battle/BattleDriver.cs:6079-6084` | `ChooseShowByCardId` 手写 5 条 | 可由 SO `maxTargets`（317 张）生成；**属战斗线文件**，按交界规则交接 | 战斗表现 |
| `Battle/CardAnimOrder.cs:117-211`、`Core/AbilityAnimDurations.cs:79-174` | `_byName` 兜底 | 保留，但 id 命中率可加日志 | 低 |

## 5. 不建议动的（理由）

- **`desc`（英文效果原文）**：SO 里**根本没有文本字段**；官方 EN 是 `[[…]]/{{…}}` 模板层，
  且 `desc` 是引擎唯一解析输入（动一个字符即动 `PinnedPlayMd5`）⇒ 不换。
- **三围/费用按 SO 覆盖**：60 项分歧里 49 项是"督军 −10 血"，成因未查清
  （`BattleManager__SetupWarlordsLifeAndEnchantment` 常量未取值），且 PnP 是铁律 7 的尺子 ⇒ 先查不切。
- **`keywords` 让 `CardTraits` 升为高优先**：会抹掉 PnP 逐张核过的值。
- **卡名**：32 张与官方显示名不同，但用户已拍「只修机制、卡名一个字不动」，且改卡名回滚过 19 条 ⇒ 只列不改。
- **`card_stats.json` 的 `art`/`armor` 源表**：用户拍板路 B（加守卫不改源表）。
- **短 id 与 GUID 不合并**：SO 证明是两个对象（变体/教程副本）。

## 6. 没查清

1. 督军 −10 血的运行期成因（`GameplayVariables` 常量值未取）。
2. `A2057` 页上副数（需跑 Unity）。
3. `A2033` 剩 21 条只有 GUID 的 `fullHeroSpriteReference` 资产名。
4. `A2037①` EC22 两张 2px 差异哪张现行。
5. 官方表 27 个 `Card_Name` 键不在 SO（`counts.official_keys_not_in_truth = 27`），未逐条定性。
6. 224 张"包内多出卡"（计策 195/单位 19/英雄 10）是否应进池 —— **不属本次两问**，未裁。

（附：已核事实 —— `cards_engine.json` count=1127、0 自造 id、1127/1127 ∈ SO；`card_ids.json` mapping=1351 且
`poolIdsAreTrue=true`；`.cs` 内 125 个不同 id 字面量/192 处，除 `CampaignData.cs:69` 的 `"UM0"`（战役节点 spec）
外全部在池内；`prebuilt_decks.json` 103 副、complete:false=0；`card_vfx_by_card.byCard` 1127 条 art refs。）
