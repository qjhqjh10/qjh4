# 查证：SaimHann 三张「裸写效果句」单位卡的触发点

> 2026-09-14。**只读查证**，未改任何已有文件 / 引擎代码。三张卡 `keywords` 均为 `[]`、`desc` 均无 `X:` 前缀
> ⇒ 引擎不知道何时触发，效果**从不发生**（且覆盖率报表看不见 —— 见 `资料/单位卡desc与光环_批次划分.md:97-127` 的【B】栏）。
> 🔴 **结论：三张的触发点都是「花 N 颗灵魂石激活」**（原版 `AbilityTrigger.UseSpiritStone = 600`，N 就是卡面上那个**绿色六边形数字**）。
> 「裸写句 = 打出时 `ThisCardPlayed`」的猜想**不成立** —— 触发词被印成了**图 + 数字**，`desc` 把它整个丢了。

证据路径缩写：`AT`=`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` ·
`DC`=`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/`（`decomp_out/` `decomp_out2/`）·
`RB`=`d:/4/Unity/资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md` ·
`EXT`=`d:/4/Unity/资料/卡表核对_卡图提取/` · `ATLAS`=`d:/2/新解包资源/assets_full/bundle_atlasindividual_assets_40ktraiticonatlas/`
（`Sprite/Atlas_SpiritStone_1..5.json` + `Texture2D/40k Trait icon atlas.png`）·
`ENG`=`d:/4/Unity/MyGame/Assets/RuleEngine/` · `IMG`=`d:/2/Warpforge部队卡片/**Aeldari**/3部队/`（⚠️ 目录名是 `Aeldari` 不是 `SaimHann`）
⚠️ 引擎行号按 2026-09-14 07:46 最后一次核对；查证期间 `Core/*.cs` 有**并发改动**，行号漂了就以**符号名**为准（`EffectText.CostKindOf` / `EffectResolver` 付费段 / `WhenEvent.SpiritAbility`）。

| 卡名 ‖ 句子 ‖ 触发点是什么 ‖ 证据 ‖ 置信度 ‖ 建议挂在引擎哪一处 |
|---|---|
| `Autarch` ‖ `Give +1 melee, +1 ranged and +1 Health to all your troops` ‖ **花 1 颗灵魂石激活该句**（绿圈 1）。**不是**部署、**不是**「收集灵魂石」、**不是** `Waystone` 关键词本身 ‖ ①`IMG/Zrzut ekranu 2026-04-16 o 18.59.12.png`（`Waystone.` 后的绿六边形 `1`，图案 = `ATLAS` 的 `Atlas_SpiritStone_1`）；②`EXT/Aeldari__2.md:25` 逐图标提取 = `【图标:菱形】Waystone. 【图标:绿圈1】Give +1 …`；③`AT:71 UseSpiritStone = 600`（与 `:3 ThisCardPlayed=0`、`:73 OnCollectSpiritStone=610` 并列）；④`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:2712 CanUseSpiritStone()` → `DC/decomp_out/CardScript__CanUseSpiritStone.c:18,47,52`：先 `HasAbilityType(raw,600)`，再按 `CardAbility.triggerValue`（`CardAbility.cs:16`，反编译侧 `+0x20`）比 `GetCurrentSpiritStone()`；⑤`DC/decomp_out/CardScript__TriggerSpiritStone.c:18,21`（`TriggerAbilities(600)`）‖ **高** ‖ `desc` 改成 `1 [spirit]: Give +1 melee, …` —— `EffectText.CostKindOf`（`ENG/Core/EffectText.cs:3651`）出 `spirit` ⇒ `ENG/Core/EffectResolver.cs:249` 扣 `ps.SpiritStones`、`:262-263` 广播 `WhenEventKind.SpiritAbility`（`ENG/Core/WhenEvent.cs:167`）。**机制早已铺好、只差这条前缀**（`WhenEvent.cs:146-165` 已独立记过同一结论） |
| `Wraithblade` ‖ `Gain Armour 2` ‖ **花 1 颗灵魂石激活该句**（绿圈 1）‖ ①`IMG/Warpforge_18_Wraithblade.png`（`Vanguard.` 下一行 = 绿六边形 `1` + `Gain [盾] Armour 2`）；②`EXT/Aeldari__1.md:37` = `【图标:?】Vanguard. **1** Gain 【图标:盾牌】Armour 2`；③`ENG/Core/EffectText.cs:3640`（原版 `ManaType` 只有 `Normal` / `SpiritStone` 两种**货币**）；④同族旁证 `EXT/Aeldari__1.md:46`（`Vengeful Wraithblade` = `Vanguard. **2** Gain Armour 2 and Flank`）⇒ **数字独立于关键词**（`Vanguard` 在 `RB:223` 是「其他单位不能被选为攻击目标」的常驻被动，不带参数）‖ **高** ‖ 同上（`desc` 补 `1 [spirit]: Gain Armour 2`） |
| `Wraithlord` ‖ `Gain +4 Attack, +4 Armor, and +2 Health` ⚠️ 卡面第二个图标是**紫枪 = 远程**，`desc` 里的 `Armor` 是 OCR 误记 ‖ **花 3 颗灵魂石激活该句**（绿圈 3）‖ ①`IMG/Warpforge_41_Wraithlord.png`（卡名下面**没有关键词行、没有阵营行**，直接是绿六边形 `3` + 效果 —— 见 `EXT/Aeldari__2.md:58` 第 1 条）；②`EXT/Aeldari__2.md:13` = `【图标:绿圈3】Gain +4 【图标:拳头】, +4 【图标:枪】 and +2 Health`；③`DC/decomp_out2/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:726` 调 `CanUseSpiritStone` ⇒ 付费窗口开在**从手牌打出结算时** ‖ **高** ‖ 同上（`desc` 补 `3 [spirit]: Gain +4 melee, +4 ranged and +2 Health`；顺手修 `Armor`→`ranged`） |

**值得注意的发现**

1. 🔴 **绿圈不是「能量」**：卡图上的绿六边形 = 图集里的 `ATLAS/Sprite/Atlas_SpiritStone_1..5.json`（绿六边形 + 白数字，实物见图集 `40k Trait icon atlas.png`）；且全卡池 `【绿圈N】` **只出现在灵族 17 行**（`EXT/_合并总表.md`；另 3 行 DarkAngels 是 `【图标:绿圈人形】`=Teleport，**不同图**）。阵营专属货币 ⇒ **推翻** `EffectText.cs:3649` 那句「别的阵营的绿色圆框就是能量」在灵族上的适用（能量是 `UI_Energy_*`，灵魂石另计 —— 实况 HUD 里 `PlayerMana/SpiritStoneHolder/SpiritStone` 与能量并排，见 `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`）。
2. ⚠️ **不是这三张的特例**：`【绿圈N】`（值只有 1/2/3/5）在灵族共 **17 张**上都有 —— `Warlock`(1) `Spiritseer`(2) `Swooping Hawk`(1) `Farseer`(1) `Wraithknight`(3) `Cosmic Serpent`(2) `Reclaim the Stars`(5) …（`EXT/Aeldari__1.md` / `Aeldari__2.md`）⇒ 按**付费前缀**一次修一族，别只补这三张。
3. **三处旁证把「灵魂石计价能力」坐实**：`EXT/Aeldari__2.md:37` `Cosmic Serpent` = `Trigger the abilities **requiring Spirit Stones** of all your troops`；`资料/卡牌数据表/卡牌完整信息库_0824.md:1081` `Bright Lance Vyper` = `When you trigger a **Spirit Stone ability**, gain +1 Attack`；`RB:210`（灵魂石「收集数量按**触发所需**减少」）。
4. 🔴 **补成 `(1)` 会错**：`CostKindOf`（`EffectText.cs:3651-3661`）对裸 `(N)` 返回 `""` ⇒ `EffectResolver.cs:238-250` 按**能量**扣钱，且 `:262` 的 `if (kind == "spirit")` **不广播** ⇒ `Bright Lance Vyper` 那类监听器永远不响。必须是 `1 [spirit]: …`。
5. 🚧 **真正缺的是「玩家动作」**：「付灵魂石激活」这个入口引擎没有（`ENG/Core/CardDef.cs:978-979` 记原版 `useWaystone = 76` / `TryUsingWaystone:6967` **没做**；全仓唯一扣石处 `EffectResolver.cs:249` 是**被动**扣）⇒ 补完前缀也只能「够就一定付」。另：付费窗口「打出时 vs 回合内随时」在原版只查到 `ResolvePlayCardFromHand` 这一条，**未跑实况坐实**（铁律 4，本轮未跑）。
