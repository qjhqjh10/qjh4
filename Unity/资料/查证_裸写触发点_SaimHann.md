# 查证：SaimHann 三张「裸写效果句」单位卡的触发点

> **2026-10-10 合并（`项目任务.md` §三 A262② 组 7）**：本文件**并入**了 `资料/灵魂石卡_逐张核.md`（28 张灵魂石卡的逐张名单）
> —— 全文在**文末「附录 · 灵魂石卡逐张核」**，**一字未删**。⇒ 凡是引 `资料/灵魂石卡_逐张核.md` 的地方，一律改看**本文件末尾那一节**。
> ⚠️ **旧行号已平移**：本文件顶部加了这段说明 ⇒ **下面的行号 +7**（例：旧 `:20,35` ⇒ 现 `:27,42`）。
> ⚠️ 同族另两份 `资料/查证_裸写触发点_EC.md` / `_Leviathan.md` **本批没并**（它们被调度台专属文件引用，按派活规矩「那一组就别做」留给调度台）。
> 理由与逐条清单：`资料/普查产出_1010/庚2_A262②_8组合并.md` §④。


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
|---|
| `Autarch` ‖ `Give +1 melee, +1 ranged and +1 Health to all your troops` ‖ **花 1 颗灵魂石激活该句**（绿圈 1）。**不是**部署、**不是**「收集灵魂石」、**不是** `Waystone` 关键词本身 ‖ ①`IMG/Zrzut ekranu 2026-04-16 o 18.59.12.png`（`Waystone.` 后的绿六边形 `1`，图案 = `ATLAS` 的 `Atlas_SpiritStone_1`）；②`EXT/Aeldari__2.md:25` 逐图标提取 = `【图标:菱形】Waystone. 【图标:绿圈1】Give +1 …`；③`AT:71 UseSpiritStone = 600`（与 `:3 ThisCardPlayed=0`、`:73 OnCollectSpiritStone=610` 并列）；④`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:2712 CanUseSpiritStone()` → `DC/decomp_out/CardScript__CanUseSpiritStone.c:18,47,52`：先 `HasAbilityType(raw,600)`，再按 `CardAbility.triggerValue`（`CardAbility.cs:16`，反编译侧 `+0x20`）比 `GetCurrentSpiritStone()`；⑤`DC/decomp_out/CardScript__TriggerSpiritStone.c:18,21`（`TriggerAbilities(600)`）‖ **高** ‖ `desc` 改成 `1 [spirit]: Give +1 melee, …` —— `EffectText.CostKindOf`（`ENG/Core/EffectText.cs:3651`）出 `spirit` ⇒ `ENG/Core/EffectResolver.cs:249` 扣 `ps.SpiritStones`、`:262-263` 广播 `WhenEventKind.SpiritAbility`（`ENG/Core/WhenEvent.cs:167`）。**机制早已铺好、只差这条前缀**（`WhenEvent.cs:146-165` 已独立记过同一结论） |
| `Wraithblade` ‖ `Gain Armour 2` ‖ **花 1 颗灵魂石激活该句**（绿圈 1）‖ ①`IMG/Warpforge_18_Wraithblade.png`（`Vanguard.` 下一行 = 绿六边形 `1` + `Gain [盾] Armour 2`）；②`EXT/Aeldari__1.md:37` = `【图标:?】Vanguard. **1** Gain 【图标:盾牌】Armour 2`；③`ENG/Core/EffectText.cs:3640`（原版 `ManaType` 只有 `Normal` / `SpiritStone` 两种**货币**）；④同族旁证 `EXT/Aeldari__1.md:46`（`Vengeful Wraithblade` = `Vanguard. **2** Gain Armour 2 and Flank`）⇒ **数字独立于关键词**（`Vanguard` 在 `RB:223` 是「其他单位不能被选为攻击目标」的常驻被动，不带参数）‖ **高** ‖ 同上（`desc` 补 `1 [spirit]: Gain Armour 2`） |
| `Wraithlord` ‖ `Gain +4 Attack, +4 Armor, and +2 Health` ⚠️ 卡面第二个图标是**紫枪 = 远程**，`desc` 里的 `Armor` 是 OCR 误记 ‖ **花 3 颗灵魂石激活该句**（绿圈 3）‖ ①`IMG/Warpforge_41_Wraithlord.png`（卡名下面**没有关键词行、没有阵营行**，直接是绿六边形 `3` + 效果 —— 见 `EXT/Aeldari__2.md:58` 第 1 条）；②`EXT/Aeldari__2.md:13` = `【图标:绿圈3】Gain +4 【图标:拳头】, +4 【图标:枪】 and +2 Health`；③`DC/decomp_out2/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:726` 调 `CanUseSpiritStone` ⇒ 付费窗口开在**从手牌打出结算时** ‖ **高** ‖ 同上（`desc` 补 `3 [spirit]: Gain +4 melee, +4 ranged and +2 Health`；顺手修 `Armor`→`ranged`） |

**值得注意的发现**

1. 🔴 **绿圈不是「能量」**：卡图上的绿六边形 = 图集里的 `ATLAS/Sprite/Atlas_SpiritStone_1..5.json`（绿六边形 + 白数字，实物见图集 `40k Trait icon atlas.png`）；且全卡池 `【绿圈N】` **只出现在灵族 17 行**（`EXT/_合并总表.md`；另 3 行 DarkAngels 是 `【图标:绿圈人形】`=Teleport，**不同图**）。阵营专属货币 ⇒ **推翻** `EffectText.cs:3649` 那句「别的阵营的绿色圆框就是能量」在灵族上的适用（能量是 `UI_Energy_*`，灵魂石另计 —— 实况 HUD 里 `PlayerMana/SpiritStoneHolder/SpiritStone` 与能量并排，见 `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`）。
2. ⚠️ **不是这三张的特例**：`【绿圈N】`（值只有 1/2/3/5）在灵族共 **17 张**上都有 —— `Warlock`(1) `Spiritseer`(2) `Swooping Hawk`(1) `Farseer`(1) `Wraithknight`(3) `Cosmic Serpent`(2) `Reclaim the Stars`(5) …（`EXT/Aeldari__1.md` / `Aeldari__2.md`）⇒ 按**付费前缀**一次修一族，别只补这三张。
   > ⚠️ **2026-09-14 晚更正：「17 张」偏小 —— 实测是 28 张。** 那个数只数了 `【绿圈N】` 这个 token，
   > 而它**只出现在 `Aeldari__2.md`**（刚好 17 行）；`Aeldari__1.md` 里同一件事记成**裸数字**
   > （全文件 0 处「绿圈」）⇒ 漏掉 6 张，另有**天赋卡**那一类（`Storm of Silence` / `Witchfire` /
   > `Wrath of Khaine`）和 `Hornet`（现值是被截断的 `3 Gain`）也全漏了。
   > **逐张开图核出来的完整名单见本文末尾「附录 · 灵魂石卡逐张核」。**（已收工，见 `资料/阵营推进_清单与交接.md` §一之三）
3. **三处旁证把「灵魂石计价能力」坐实**：`EXT/Aeldari__2.md:37` `Cosmic Serpent` = `Trigger the abilities **requiring Spirit Stones** of all your troops`；`资料/卡牌数据表/卡牌完整信息库_0824.md:1081` `Bright Lance Vyper` = `When you trigger a **Spirit Stone ability**, gain +1 Attack`；`RB:210`（灵魂石「收集数量按**触发所需**减少」）。
4. 🔴 **补成 `(1)` 会错**：`CostKindOf`（`EffectText.cs:3651-3661`）对裸 `(N)` 返回 `""` ⇒ `EffectResolver.cs:238-250` 按**能量**扣钱，且 `:262` 的 `if (kind == "spirit")` **不广播** ⇒ `Bright Lance Vyper` 那类监听器永远不响。必须是 `1 [spirit]: …`。
   > ⚠️ **2026-09-14 补记（当时这句话没验过）**：`1 [spirit]: …` **当时其实解析不了** ——
   > 探针实测：`[` `]` 在 `ParseSegment` 开头先被剥掉，而 `RePaid` 的货币词表里只有 `spirit stones?`，
   > **光秃的 `spirit` 不在表里** ⇒ 轻则整句不认，重则被吃成载荷（`2 [spirit]: Repeat this effect`
   > → `载荷「2 spirit:」`，还报「认了」）。**照这句话照抄会踩坑。**
   > 已就地修掉引擎（两条正则改成 `spirit(?:\s+stones?)?`，改前全池 0 张匹配 ⇒ 无回归），
   > 但**数据里请写 `1 [Spirit Stone]: …`** —— 它在改与不改两种状态下都成立。
   > 详见 `资料/阵营推进_清单与交接.md` §一之三「写法已用逐句解析探针验过」。
5. 🚧 **真正缺的是「玩家动作」**：「付灵魂石激活」这个入口引擎没有（`ENG/Core/CardDef.cs:978-979` 记原版 `useWaystone = 76` / `TryUsingWaystone:6967` **没做**；全仓唯一扣石处 `EffectResolver.cs:249` 是**被动**扣）⇒ 补完前缀也只能「够就一定付」。另：付费窗口「打出时 vs 回合内随时」在原版只查到 `ResolvePlayCardFromHand` 这一条，**未跑实况坐实**（铁律 4，本轮未跑）。
   > 🔴 **2026-09-14 晚更正（这句话把两件事混成了一件）**：`useWaystone`（76 号动作）**不是**「花石激活」，
   > 而是「**收集**」—— 对象是场上**已翻面成「灵族残骸／灵魂石」的自己人**，而 `CanUseWaystone`
   > **既不查余额、也不查这卡有没有 600 能力**。**付石那一步在「从手牌打出」的流程里**
   > （`CanUseSpiritStone` 在 dump 里唯一的调用点）。两条链**互不调用**。
   > **已收工**：28 张卡补上前缀，触发点接在 `RuleCore.PlayCard`（部署时，排在 `Rally` 之前），
   > `Bright Lance Vyper` 的监听器**真的响**。语义与出处：`资料/查证_useWaystone_语义.md`；
   > 逐卡名单：本文末尾「附录 · 灵魂石卡逐张核」；完整记录：`资料/阵营推进_清单与交接.md` §一之三。
   > ✅ **2026-10-07 更正：这一句已作废，两个说法都不成立** ——
   > ① 「收集」**2026-09-25 就做完了**（`RuleCore.CanCollectWaystone` `:3081` / `CollectWaystone` `:3103`：
   > 带 `Waystone.` 的单位死亡先留一具残骸体，**玩家点它才 +1**）；
   > ② 「**翻面**」这个词**是错的** —— **不是翻面、更不是卡背**（原版卡牌族里没有 flip/faceDown 字段、
   > `ShowCardBack` 只给手牌用），实际是「**原卡变残骸体**」，见 `资料/查证_useWaystone_语义.md:19-29`。
   > ⚠️ 上面那句「付费窗口…**未跑实况坐实**（铁律 4）」**照旧成立**（原版已关服 + 手牌注入失败），
   > 但**判据那一档已升级为「反编译有据」**（扣石 = `RawCardScript__TriggerAbility.c:42-53`）
   > → `资料/普查产出_1007/波7判据核查.md` §A120 / §A121。

---

<!-- ══════ 附录 开始 · 原 `资料/灵魂石卡_逐张核.md` 全文 ══════ -->

# 灵魂石卡 · 逐张核（Aeldari / Saim-Hann）

> 判据 = 卡面**效果文字区**里的「绿六边形 + 白数字」（原版图集 `d:/2/新解包资源/assets_full/bundle_atlasindividual_assets_40ktraiticonatlas/Sprite/Atlas_SpiritStone_1..5.json`，实物 `Texture2D/40k Trait icon atlas.png`）。
> 2026-09-14 逐张开图：`d:/2/Warpforge部队卡片/Aeldari/` 下 **92 张卡图全开**（80 张 PnP + 12 张 `Zrzut ekranu` 截图），另用放大裁图逐点核过图标底色/图案。
> 方括号 = 卡面此处有图标，例 `Gain [旋刃]Shuriken 2`。**N 的取值只出现 1 / 2 / 3 / 5。**
> desc 列 = 在我们卡表现值上**补前缀**（其余缺口见附注 ⑤）；前缀语法 `N [spirit]: `（判据 `Core/EffectText.cs:4197 CostKindOf` + `:4182` 那条实测记录）。

| 卡名 | 卡图文件名 | N | 绿圈管住的那一句（照抄卡面英文原文）| 补完前缀后整条 desc 应该长什么样 | 置信度 |
|---|---|---|---|---|---|
| Storm of Silence | 2天赋/Warpforge_02_Storm-of-Silence.png | 1 | `[螺旋]Stun a random enemy troop` | `Your Warlord gains +2 [Health] this turn. 1 [spirit]: Stun a random enemy troop` | 中 |
| Witchfire | 2天赋/Warpforge_24_Witchfire.png | 1 | `Repeat this effect` | `Deal 1-3 damage.\n1 [spirit]: Repeat this effect` | 高 |
| Wrath of Khaine | 2天赋/Warpforge_43_Wrath-of-Khaine.png | 2 | `Repeat this effect` | `Give +2 Melee and +2 Ranged Attack to your units. 2 [spirit]: Repeat this effect` | 高 |
| Shining Spear | 3部队/Warpforge_03_Shining-Spear.png | 1 | `Gain [旋刃]Shuriken 2 this turn` | `1 [spirit]: Gain Shuriken 2 this turn` | 高 |
| Hornet | 3部队/Warpforge_05_Hornet.png | 3 | `Gain [齿轮]Fast` | `3 [spirit]: Gain Fast` | 高 |
| Warlock | 3部队/Warpforge_11_Warlock.png | 1 | `Give +1 Ranged Attack and +1 Health to all your troops` | `1 [spirit]: Give +1 Ranged Attack and +1 Health to all your troops` | 高 |
| Swooping Hawk | 3部队/Warpforge_16_Swooping-Hawk.png | 1 | `Gain +1 [紫枪] and [箭头]Flank` | `1 [spirit]: Gain +1 Ranged Attack and Flank` | 高 |
| Wraithblade | 3部队/Warpforge_18_Wraithblade.png | 1 | `Gain [盾牌]Armour 2` | `1 [spirit]: Gain Armour 2` | 高 |
| Wraithguard | 3部队/Warpforge_25_Wraithguard.png | 1 | `Gain +2 Ranged Attack and +2 Health` | `1 [spirit]: Gain +2 Ranged Attack and +2 Health` | 高 |
| Spiritseer | 3部队/Warpforge_26_Spiritseer.png | 2 | `Deploy a Wraithguard` | `2 [spirit]: Deploy a Wraithguard` | 高 |
| Vengeful Wraithblade | 3部队/Warpforge_28_Vengeful-Wraithblade.png | 2 | `Gain [盾牌]Armour 2 and [箭头]Flank` | `2 [spirit]: Gain Armour 2 and Flank` | 高 |
| Hemlock Wraithfighter | 3部队/Warpforge_40_Hemlock-Wraithfighter.png | 3 | `Gain [星爆]Blast 5` | `3 [spirit]: Gain Blast 5` | 高 |
| Wraithlord | 3部队/Warpforge_41_Wraithlord.png | 3 | `Gain +4 [粉拳], +4 [紫枪] and +2 Health` | `3 [spirit]: Gain +4 melee, +4 ranged and +2 Health` | 高 |
| Avatar of Khaine | 3部队/Warpforge_42_Avatar-of-Khaine.png | 2 | `Gain [星爆]Blast 6 and [兜帽]Camouflage.` | `Armour 2. 2 [spirit]: Gain Blast 6 and Camouflage. Talent: Wrath of Khaine.` | 高 |
| Wraithknight | 3部队/Warpforge_44_Wraithknight.png | 3 | `[绿圈3] . Gains [无敌]Invulnerable until your next turn`（绿圈后**紧跟一个孤立句点**）| `3 [spirit]: Gains [Invulnerable] until your next turn` | 中 |
| Death Spinner Warp Spider | 3部队/Zrzut ekranu 2026-04-16 o 18.41.16.png | 1 | `Gain [箭头]Flank` | `Waystone. Stealth. 1 [spirit]: Gain Flank.` | 中 |
| Farseer | 3部队/Zrzut ekranu 2026-04-16 o 18.48.58.png | 1 | `Choose a card from your deck and draw it` | `When you collect a Spirit Stone, gain Shield. 1 [spirit]: Choose a card from your deck and draw it` | 高 |
| Farseer Skyrunner | 3部队/Zrzut ekranu 2026-04-16 o 18.57.33.png | 2 | `Choose a card in the enemy hand and shuffle it into their deck` | `2 [spirit]: Choose a card in the enemy hand and shuffle it into their deck` | 高 |
| Autarch | 3部队/Zrzut ekranu 2026-04-16 o 18.59.12.png | 1 | `Give +1 [粉拳], +1 [紫枪] and +1 Health to all your troops` | `1 [spirit]: Give +1 melee, +1 ranged and +1 Health to all your troops` | 高 |
| Ahnakh-Yth Shrine | 4计策/Ahnakh-Yth Shrine.png | 1 | `Create a copy of Ahnakh-Yth Shrine in your hand` | `Give Shuriken 1 to a friendly troop. 1 [spirit]: Create a copy of Ahnakh-Yth Shrine in your hand` | 高 |
| Will of Asuryan | 4计策/Warpforge_47_Will-of-Asuryan.png | 1 | `Draw a card` | `Give Vanguard to a friendly troop.\n1 [spirit]: Draw a card` | 高 |
| Webway Gate | 4计策/Warpforge_50_Webway-Gate.png | 1 | `It costs 2 less` | `Choose a troop from your deck and draw it. 1 [spirit]: It costs 2 less` | 高 |
| Cosmic Serpent | 4计策/Warpforge_52_Cosmic-Serpent.png | 2 | `Repeat this effect` | `Trigger the abilities requiring Spirit Stones of all your troops. 2 [spirit]: Repeat this effect` | 高 |
| Devoted of Khaine | 4计策/Warpforge_53_Devoted-of-Khaine.png | 1 | `Repeat this effect` | `Give +1 Melee Attack to your troops. 1 [spirit]: Repeat this effect` | 高 |
| Eldritch Storm | 4计策/Warpforge_59_Eldritch-Storm.png | 2 | `Repeat this effect` | `Deal 1-3 damage to all enemies.\n2 [spirit]: Repeat this effect` | 高 |
| Reclaim the Stars | 4计策/Warpforge_63_Reclaim-the-Stars.png | 5 | `Reduce their cost by 5` | `Draw 5 cards. 5 [spirit]: Reduce their cost by 5` | 高 |
| Wailing Doom | 4计策/Warpforge_64_Wailing-Doom.png | 2 | `Deal 4 damage to units adjacent to the target` | `Deal 8 damage to an enemy. 2 [spirit]: Deal 4 damage to units adjacent to the target` | 高 |
| Forewarned | 4计策/Zrzut ekranu 2026-04-16 o 18.49.42.png | 1 | `Also give it [眼睛]Shield` | `Give +3 melee and +3 ranged to a friendly unit this turn. 1 [spirit]: Also give it Shield` | 高 |

**附注**

① **共 28 张**（N=1 十五张 · N=2 八张 · N=3 四张 · N=5 一张）。除你点名的 6 张 + 主对话后补的 2 张（`Storm of Silence` `Witchfire`）之外，**另外 20 张是这次才认出来的**：`Wrath of Khaine` `Shining Spear` `Hornet` `Hemlock Wraithfighter` `Wraithlord` `Wraithknight` `Avatar of Khaine` `Farseer` `Farseer Skyrunner` `Autarch` `Death Spinner Warp Spider` `Ahnakh-Yth Shrine` `Will of Asuryan` `Webway Gate` `Cosmic Serpent` `Devoted of Khaine` `Eldritch Storm` `Reclaim the Stars` `Wailing Doom` `Forewarned`。分布：1督军 0 · 2天赋 3 · 3部队（含 4 张截图）16 · 4计策 9 · 5防御卡 0。
② **排除掉的**：`Path of the Seer`（`2天赋/Warpforge_2_Path-of-the-Seer.png`）—— 同一个绿圈**长在句尾**（`… Draw a card. Gain [绿圈1]`），是「获得 1 颗灵魂石」的**名词**、不是费用前缀 ⇒ 不进表（顺带：我们卡表把它写成了 `Gain 1 Energy`，**很可能该是 Spirit Stone**，属另一处口径问题）。另有 11 张写 `Spirit Stone(s)` 的是**纯文字无绿圈**、也不是费用：`Path of the Warrior` `Path of Command` `Warp Spider Exarch` `Warlock Skyrunner` `Bright Lance Vyper` `Spiritseer Qelenaris` `Orian Laratharjos` `Infinity Circuit` `Aspect Shrine` `Craftworld Convergence` `Hosts of the Dead`。`Stealth (1)`（`Death Spinner Warp Spider`）与 `Gain (1)`（DarkAngels `Martial Superiority` 的**金齿轮**）都不是灵魂石 —— 前者拆开看是 `Stealth.` + 独立绿圈，后者底色/图案是齿轮。
③ **拿不准 2 类 / 3 张**：`Storm of Silence` —— 那个「1」的底色/图案 = **深绿椭圆 + 白字 + 亮绿描边**，与另外 27 处**是同一个图**，且与修女会真 `[Energy]` 的**金色太阳**（`Sororitas/4计策/Warpforge_55_Fiery-Conviction.png` `8 ☀ :`、`Sororitas/2天赋/Warpforge_02_Daemonbreaker.png` `4 ☀ :`）明显不同 ⇒ 图是灵魂石；**但我们卡表那行写的是 `(1) [Energy]:`**（token 与图冲突、且 `(N)` 带括号的写法全卡池仅此一例）⇒ 记「中」，请拍板。`Wraithknight` 与 `Death Spinner Warp Spider` 记「中」是因为要**动到句子本身**（吞孤立句点 / 删 `Stealth (1)` 的 `(1)`），不只是插前缀。
④ **`Wraithknight` 的畸形形状照实记下**：卡面印的是 `[绿圈3] . Gains …` —— 绿圈后面**紧跟一个孤立句点**，然后才是动词。引擎要「数字后紧跟动词」⇒ `3 [spirit]: . Gains …` 会**整条不认**；建议按上表吞掉那个句点。同族的坑：`Death Spinner Warp Spider` 卡面是 `Stealth.` + 独立绿圈 + `Gain Flank`，**`Stealth` 没有参数** ⇒ 我们卡表的 `(1)` 是绿圈被误记进括号，要删掉、换成本表的前缀（`Stealth (1)` 这个写法全卡池只此一处）。
⑤ **desc 列除前缀外顺手带上的（同一条 desc 上的其它缺口，与本条前缀无关）**：`Wraithlord`/`Forewarned` 的 `Armor` 卡面是**紫枪图标 = 远程**（照卡面改 `ranged`，同 `规范` 那条）；`Hornet` 的 `Gain` 后掉了 `Fast`、`Swooping Hawk` 的 `+1` 后掉了远程图标、`Ahnakh-Yth Shrine` 的 `Give 1` 实为 `Shuriken 1`、`Storm of Silence` 的 `+2 [Health]` 卡面是**粉拳 = 近战**（与 `Wraithlord` 的拳同图）。**未代补**的：多数部队卡的 desc 还丢了关键词行（`Ephemeral.` / `Waystone.` / `Flying.` / `Flank.` / `Vanguard.`）。
