# B121-FIX · 收口小修（2026-11-03）

执行写手：`E121-FIX`。白名单内 8 个文件，**未跑 Unity、未动 git、未改账主表/活状态表/正本**。

## 一、逐条

| ID | 改了什么 | 判据 | 落点 | 验证 | 还差什么 |
|---|---|---|---|---|---|
| 1 | 删掉「下一波给 13 张变身卡落 `notDeckable: 1`」那条指令，改成「**允许加入卡组**（现读置位 = 0 张）」，保留订正痕迹 | 用户 2026-11-03 裁定；现读 `cards_engine.json`：`notDeckable` 非 0 = **0 张**，13 张 `TRANSFORM_<阵营>` 全为缺键 | `RuleEngine/Core/DeckRules.cs:1271-1274`（**零行数变化**，`:1280 IsNotDeckable` 未漂） | 账闸 A 5899 / 漂移 0 / 失败 0 | 无 |
| 2a | `TacticPlayable` 加例外：变身卡不再被判「打不出」⇒ 自动组牌不再整批跳过 | `RuleCore.HasWarlordTransformAbility`（`RuleCorePartial_B96CB.cs:624`，唯一判据）；`RuleCore.TacticOpsOf` 对变身卡给一条 `warlordtransform` op | `RuleEngine/Data/DeckBuilder.cs:454`（同行内改，行数不变） | typecheck 0/0 | 需要一条断言（本文件不属断言宿主，见 §三） |
| 2b | 卡面不再错打「效果本版结算不了\*」，**同时**不再落「效果本版没作用\*」 | 同上；`warlordtransform` 有真实执行支（`EffectResolver.cs:1096` → `DoWarlordTransform`） | `CardPresentation/Battle/BattleDriver.cs:28576` + `:28578`（两行都同行内改） | typecheck 0/0；`ruleprobe card "Warlord Ascension"` 复现「`parse` 只吐一条整句原文」= 原来必落 `*` | 无 |
| 3 | 13 张变身卡的**立绘别名**：`TRANSFORM_<阵营>` → 同阵营一张有画的原版卡 id（纯查表，⛔ 不造贴图） | 现读：13 张卡的 `art_<id>.png` **0 张**；13 个源图 id 的 `art_*.png` **13/13 在位**、阵营逐条相符（见下） | `CardPresentation/Core/CardArt.cs:438`（`Portrait()` 内接别名）+ 类尾新增 `_portraitAlias` / `PortraitKeyAlias`（`:1423-1469`，在全部账落点 `:1405` 之后） | typecheck 0/0；表 ↔ 数据 13/13 双向零差（脚本核）；`chosenAbility==900` 的卡 = 同 13 张 | 需真机看图（未跑 Unity） |
| 4 | `WFSoundBank` 的产出方说明过期（原写「生成器只产出 collection/card 两组」）⇒ 改成「**三表 + `HAND_WRITTEN` 手写组**」并记订正 | `工具/import_battle_sfx.py:313-358`（C 组 `HAND_WRITTEN`）· 现读三表条数 `513 / 34 / 6` | `WarpforgeVFX/Runtime/WFSoundBank.cs:55`（同行内改，`:168` 未漂） | 账闸绿 | 无 |
| 5 | 开门音效 `PlayCue` 提到两处早退（`ClipName` 未命中 / `Resources.Load<VideoClip>` 为 null）**之前** ⇒ 早退只挡视频、不挡音效 | 原版 `EndBattleDoors__SetupDoor.c:113` = `SoundManager.Play2D(cue, Jingles)`，**无条件** | `CardPresentation/Battle/BattleDoors.cs:355`（调用挪到这里）+ `:435`（原位留注释） | 账闸：`:352` / `:434` / `:472` 三个落点**行号全部未动** | 需真机听（未跑 Unity） |
| 6 | `B121_D5_Assert` G 组标题/说明过期（原写「`unitsToSummon` 卡 id 列缺」）⇒ 改成「列**已由 `B121-D4` 就位**，本组是『夹具没带这一列』的反证」 | `CardDef.cs:9163`（`UnitsToSummon` 三态）· `EnchantmentExecutor_B115.cs:1696-1703`（`null` = 未知 ⇒ 出声 + 不结算） | `CardPresentation/Editor/B121_D5_Assert.cs:30-32` + G 组 section 标题 | typecheck 0/0（断言语义一字未改，只改文案） | 无 |
| 7 | `B121_D2_Assert` 的 `AddedCues` 缺 `Draw` ⇒ 补齐成 7 条，`AddedClip`/`AddedParams` 同补，与 `B117_G10_12_Assert` C1b 逐名对齐 | 现读 `battle_sounds.json`：`Draw` = `group=scene` · `clips=["MatchDraw"]` · pitch 1/1 · vol 0.7/0.7 · ttp 0.1；`Resources/Art/audio/sfx/MatchDraw.ogg` 在位 | `CardPresentation/Editor/B121_D2_Assert.cs:14,53,70-101,164,203` | typecheck 0/0 | 需跑一次宿主（未跑 Unity） |

### 立绘别名表（13/13，阵营逐条核过）

`AM76 / BL37 / DA39 / EC42 / GSC56 / GOF100 / TL33 / ASH46 / SAU75 / SOR56 / SW43 / TAU57 / UM109`
—— 逐个与 `cards_engine.json` 的 `faction` 对齐（AstraMilitarum … Ultramarines，13/13 相符）。

## 二、为什么别名只接在 `Portrait()`、不改 `artId`

`CardData.artId` 另外兼两个活：`CardProgress.Level(artId)`（卡框升级档）与
`CardButtons.VoiceKey`（语音键）。把别名写进 `artId` ⇒ 变身卡会拿到**别人那张卡**的升级档与语音
（例如 `UM109` 的档位、`UM109` 的语音）。`HasCutout` 同样不映射（13 个源图本身无角色抠图，
硬映射会让卡面多一层前景把卡框盖住）。⇒ 只在「取插图」这一跳换键，三条消费链互不污染。

## 三、还差什么（都已记账，不在本笔白名单）

1. **断言**：本笔 7 条没有新增自检断言 —— 好的落点（`BattleScene` / `RuleEngineTest` 聚合行）
   属 `E121-WIRE`，本笔不许碰。建议补两条：
   ① `PortraitKeyAlias` 表 ↔ 数据 13/13（纯查表，不需要 Unity 渲染）；
   ② `DeckBuilder.TacticPlayable(13 张) == true` 且卡面 `UnimplementedNotes` 不含任何 `*`。
2. **`EffectText.WillRunOps` 不认识变身卡**（`EffectText.cs:3148`，只对 `tactic/defence` 走 `Parse`）⇒
   任何**将来新增**的 `WillRunOps` 消费点都会把变身卡判成「没机制」。本笔只在 `BattleDriver`
   卡面这一处挡（`MechanismFails` 那条）。根治 = 在 `WillRunOps` 里加一条
   `HasWarlordTransformAbility` 分支返回等价的 op（`EffectText.cs` 不在白名单）。
3. **真机验收**：立绘（第 3 条）与开门音效（第 5 条）都只能进游戏看/听。

## 四、顺手发现

- **全池无立绘的卡只有 15 张**：13 张变身卡 + 2 张附魔
  （`e8cf1eb5…` = `Reconnaissance Mission` · `9218576f…` = `Pray Enchantment`）。
  ⚠️ 后两张 `showInCollectionOptions = 0` / `inventoryOptions = 10`（**收藏里不显示**）⇒
  **玩家看不到占位图**，**不是缺陷**，本笔不动。原任务描述里的「39 张进攻卡」按现读**已收敛到 13 张**
  （其余卡在 `art_*` 里都有图）。
- `ruleprobe card <名字>` 按**名字**查，按 id 查会报「no card named」——排查 13 张卡时别被这句误导。
- `BattleDoors.SetupDoor` 的 `ClipName` 未命中分支（`Result` 枚举外的值）现在也会放音效；
  原版 `SetupDoor` 同样是无条件播，**不算新增行为**。
