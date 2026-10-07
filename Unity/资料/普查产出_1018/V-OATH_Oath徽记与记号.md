# V-OATH · Oath 徽记：记号 or 装饰？（只读现核）

> 只读现核代理 `V-OATH`。**只读 + 只写本文件**，一行代码/一张正本都没动，没跑 Unity，没动 git。
> 判据来源逐条标 `文件:行号` / 资产路径。**裁定由调度台下，本报告只把判据查齐**（含两个候选，不选）。

---

## 1. 一句话结论

**① 那枚徽记是「记号」，不是装饰** —— 它是原版 **`Oath` 这个 `DefinedTrait`／关键词的图标**
（原版有专门的 `[SerializeField] private Sprite oathSprite`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTraitCollection.cs:27`）。
卡面写法一律是「**〔徽记〕+ 紧跟印着的那个词 `Oath`**」（= 图标 + 词），**不是「只画图标、词不印」**。

**② 但这 24 张的 `Oath N:` 不需要「补记号」——它现在就已经画出来了。**
S11 的框架（「25 张全写裸 `Oath N:` ⇒ 记号没带走 ⇒ 全族缺陷」）**不成立**：
`card_icon_plan.json` 恰恰把这 24 张记成**裸写 token `Oath N:` → `sprite: oath`**，
而 `CardIcons.Rewrite` 对「裸关键词」那一支的换法是 **「图标插在词前面、词留着」**（`CardIcons.cs:242-255`）
⇒ 我们渲出来就是 `〔徽记〕Oath 3:`，**与原版逐字一致**。**裸写正是对的写法。**

**③ 真缺陷只有 3 张，而且与 `Oath N:` 无关** —— 是那句 **`Oath abilities …`**（原版在词 `Oath` 前也印徽记）：

| 卡 | 现状 | 差在哪 |
|---|---|---|
| `UM84 Chaplain Cassius` | desc = `Friendly **[Oath]** abilities …` ⇒ 徽记**画出来了**，但**词 `Oath` 被吃掉**（同卡 `[Talent]:` 也一样吃掉词） | 少了「Oath」和「Talent」两个词 |
| `UM89 Ferren Areios` | 首句 `Oath abilities of friendly…` **整枚徽记没画**（计划表只记了 `Oath 1:`） | 少一枚徽记 |
| `UM_Vico_Therbeus` | 同上（计划表只记了 `Oath 1:`） | 少一枚徽记 |

**④ 「补记号」这条路有先例，但方向是反的**：25 张里**唯一带记号的那一张（`UM84`）恰恰是唯一画错的**。
`[Oath]` 这个 token 的来路是**上游 OCR 源**（`card_stats.json`），不是手写覆盖。

**⑤ 影响面不小、而且是行为级的**：`Oath N:` 这段文字**就是引擎取誓约正文的唯一来源**
（`CardDef.CollectOathOps` 从 `Desc` 解析）。改成 `[Oath] N: …` 会让 `ReOathPaid` 失配
⇒ `OathOps` 空 / `OathCost = 0` ⇒ **誓约能力静默失效**（卡面也不打 `*`）。
⚠️ 改 `desc` 还要**重跑两个生成器**（`gen_cards_engine.py` + `gen_icon_plan.py --write`）。

---

## 2. 原版卡面实据（**25 张逐张亲读 · 不是抽看**）

### 2.1 那枚徽记长什么样

- **形状/颜色**：**蓝圆盘（饱和度高的钴蓝）+ 白色「跪姿/垂首的袍服人形」**（头罩 + 面甲弧线 + 披风下摆两道折）。
  **不是**任何已知的属性图标。
- **位置**：效果文字**行内**，**紧贴词 `Oath` 的左侧**（行首或句中都是「徽记 + `Oath`」这一对）。
- **大小**（量法可复查）：满宽 900 px 的卡图上直径约 **33 px**；`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/traits/oath.png`（80×80）与它**同形同色**（见 §2.3）。

### 2.2 与其它 token 并排对比（同一批像素算法、同一台机器）

| 徽记 | 圆底 | 内盘 r=13 均值 RGB | 判 |
|---|---|---|---|
| 粉圈白拳 | 深红 | R 最大、B≈G | **近战** |
| 紫圈白枪 | 紫 | B 最大 | **远程** |
| 银盾 | 银灰 | 三通道接近 | **护甲** |
| **蓝圈白袍（本件）** | **钴蓝** | B 最大且**远离 G** | **Oath** |
| 纸卷 | 米黄 | R 最大、G 高 | Talent |

（「蓝底 + 白袍人形」是这一族里**唯一**的一枚 —— 上表五行里没有第二行是蓝底。）

（粉拳/紫枪那两行读数直接取自 `资料/普查产出_1018/S11_UM85与探针脚本.md` §1 那张表，**我复用了它、没有重跑**；
本件读数与「紫枪」同属 B 最大，但**底色与人形**肉眼可区分，见下面的并排图。）

### 2.3 并排图（本批产物，供复核）

`d:/tmp/wf_oath/grid_oath.png` —— 五格：`UM82 徽记` · `UM73 徽记` · `UM85 徽记` · `UM84 徽记` · **我们的 `traits/oath.png`**（第 5 格）。
判读：**前四格与第五格是同一枚**（蓝圆盘 + 白袍人形 + 披风下摆）。
另外三张拼图供整卡复核：`d:/tmp/wf_oath/sheet_g1.png`（8 张）· `sheet_g2.png`（8 张）· `sheet_g3.png`（7 张）· `sheet_text.png` · `um83.png`。

### 2.4 **25/25 全部亲读的读数**（每张都：徽记**在**，且紧贴词 `Oath`）

| 卡图（`d:/2/Warpforge部队卡片/Ultramarines/…`） | 卡面原文（我读到的） | 徽记处数 |
|---|---|---|
| `3部队/Warpforge_19_1st-Company-Terminator.png`（`UM83` 用这张） | `〔盾〕Armour 1. 〔爆裂〕Blast 3. **〔蓝徽记〕Oath** 2: Deal 3 damage` | 1 |
| `3部队/Warpforge_23_Bladeguard-Ancient.png` | `Your other units have +2〔拳〕. **〔蓝徽记〕Oath** 1: Give〔盾〕Armour 1 to a friendly troop` | 1 |
| `3部队/Warpforge_20_Chaplain-Cassius.png` | `Friendly **〔蓝徽记〕Oath** abilities apply an additional time. 〔纸卷〕Talent: Catechism of Death` | **1（句中！）** |
| `3部队/Warpforge_14_Chapter-Champion.png` | `〔盾〕Armour 1. 〔codex〕Codex: Gain +2〔拳〕. **〔蓝徽记〕Oath** 2: Gain〔盾〕Armour 1 and Vanguard` | 1 |
| `3部队/Warpforge_25_Ferren-Areios.png` | `**〔蓝徽记〕Oath** abilities of friendly troops can be activated up to 3 times each turn. **〔蓝徽记〕Oath** 1: Deal 1 damage` | **2** |
| `3部队/Warpforge_07_Incursor.png` | `〔闪电〕Rally: Deal 1 damage. **〔蓝徽记〕Oath** 2: 〔眩晕〕Stun an enemy` | 1 |
| `3部队/Warpforge_08_Phobos-Lieutenant.png` | `〔远射〕Long Range **〔蓝徽记〕Oath** 1: Gain〔眼〕Stealth. 〔斩杀〕Slay: Create a random Ultramarines card in hand` | 1 |
| `3部队/UPDATED_Warpforge_15_Suppressor.png` | `〔飞行〕Flying. 〔哨戒〕Sentry 1. **〔蓝徽记〕Oath** 1: Choose a non-Legendary Stratagem …` | 1 |
| `3部队/Warpforge_09_Tactical-Marine.png` | `**〔蓝徽记〕Oath** 3: Gain +3〔拳〕, +3〔枪〕and +3 Health` | 1 |
| `3部队/Warpforge_24_Terminator-Captain.png` | `〔盾〕Armour 2. **〔蓝徽记〕Oath** 4: Gain〔无敌〕Invulnerable until your next turn` | 1 |
| `3部队/Warpforge_18_Terminator.png` | `**〔蓝徽记〕Oath** 1: Gain〔盾〕Armour 1` | 1 |
| `3部队/Warpforge_12_Vico-Therbeus.png` | `〔潜行〕Stealth. **〔蓝徽记〕Oath** abilities of friendly troops may be activated on later turns. **〔蓝徽记〕Oath** 1: Gain〔迷彩〕Camouflage` | **2** |
| `4计策/Warpforge_28_Adaptive-Strategy.png` | `Give +1〔拳〕to all friendly troops. **〔蓝徽记〕Oath** 1: Give +1 Health to all friendly troops` | 1 |
| `2天赋/Warpforge_06B_Angels-Wrath.png` | `〔临时〕Ephemeral. Give +1〔拳〕to a friendly unit until your next turn. **〔蓝徽记〕Oath** 2: Give it〔盾〕Armour 2 this turn` | 1 |
| `2天赋/Warpforge_21_Catechism-of-Death.png` | `〔临时〕Ephemeral. Give +2〔拳〕… this turn. **〔蓝徽记〕Oath** 3: Give it an additional +2〔拳〕` | 1 |
| `4计策/Warpforge_30_Champions-of-Humanity.png` | `Give +2 Health to all friendly units. **〔蓝徽记〕Oath** 4: Give them an additional +3 Health` | 1 |
| `4计策/Warpforge_29_Fall-Back.png` | `Return a friendly troop to your hand. **〔蓝徽记〕Oath** 2: Lower its cost by 2 and give it〔侧翼〕Flank` | 1 |
| `4计策/Warpforge_37_Indomitus-Crusade.png` | `Deploy an Ultramarines troop that costs 6 or more. **〔蓝徽记〕Oath** 6: Deal damage to all enemy troops equal to its〔拳〕` | 1 |
| `4计策/Warpforge_35_No-Mercy.png` | `Deal 4 damage to an enemy troop. **〔蓝徽记〕Oath** 4: Also destroy all damaged enemy troops` | 1 |
| `4计策/Warpforge_36_No-Respite.png` | `Give〔哨戒〕Sentry 1 to all friendly units. **〔蓝徽记〕Oath** 3: Create No Respite in your hand` | 1 |
| `4计策/Warpforge_32_Shall-Know-No-Fear.png` | `Give〔无敌〕Invulnerable to a friendly troop this turn. **〔蓝徽记〕Oath** 8: Give it +8〔拳〕and +8〔枪〕` | 1 |
| `4计策/Warpforge_34_Stormhawk-Interception.png` | `Deal 2 damage to an enemy unit and adjacent units. **〔蓝徽记〕Oath** 3: Repeat this effect` | 1 |
| `3部队/Warpforge_13_Attack-Bike.png` | `**〔蓝徽记〕Oath** 2: Gain +1〔枪〕and〔侧翼〕Flank` | 1 |
| `3部队/Warpforge_27_Brutalis-Dreadnought.png` | `**〔蓝徽记〕Oath** 2: Deal 3 damage to all enemies` | 1 |
| `3部队/Warpforge_22_Dreadnought.png` | `**〔蓝徽记〕Oath** 2: Deal 2 damage to an enemy and adjacent units` | 1 |

🔴 **关键读数（两处，决定整条裁定的形状）**：

1. **原版在「每一个 `Oath` 词」前面都印徽记**，包括**句中**的 `Friendly 〔徽记〕Oath abilities …`（`UM84`）——
   **不是**只在 `Oath N:` 那里印。
2. **徽记后面那个词是印着的**（`〔徽记〕Oath 2:`，不是「徽记 = Oath 这个词的替身」）。
   独立旁证：`资料/卡表核对_卡图提取/_还原效果文字.md:1049`（29 个子代理逐张看卡图抄的表、2026-09-13）
   写的是 `Friendly OathOath abilities apply an additional time. TalentTalent: Catechism of Death`
   —— 该表 `WordWord` 这种**重复写法**就是它的「**图标 + 那个词**」约定（同文件里 `Long RangeLong Range` / `StealthStealth` 同族）。

### 2.5 徽记的图我们**已经有了**

`MyGame/Assets/CardPresentation/Resources/Art/traits/oath.png`（80×80，来自原版图集
`bundle_atlasindividual_assets_40ktraiticonatlas/Texture2D/40k Trait icon atlas.png`，
建法与出处见 `CardPresentation/Editor/IconSetup.cs` 文件头）。
⚠️ 原版的 `oathSprite` **不在本地解包资源里**（全库 grep `oathSprite` **0 命中**；
`CardTraitCollection` 只命中一个 `MonoScript_3970.json`）—— 但**不影响**：图集里那一枚与卡面同形同色（§2.3）。
⇒ **「我们图集里有没有这张图」不是缺口的来源**（`CardIcons.cs` 的换图就是走这张）。

---

## 3. 原版文本侧的字面量（我查过哪些文件/关键词 · 读到什么）

### 3.1 `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条字面量）

- 搜 **`oath`（不分大小写）⇒ 0 命中**。
- ⇒ **「Oath」这个词在原版里不是硬编码字面量** —— 它走**枚举名**（`DefinedTrait.oath` / `AbilityTrigger.Oath`）
  经 `Enum.ToString()` 再查本地化表。⇒ 本地字面量这条线**没有 `[Oath]` / `Oath` 可查**（不是「没搜到」，是**本来就不在那儿**）。

### 3.2 原版拼串的方法体（`d:/2/tools/decomp_full/`）

`GameStaticData__TraitNameToString.c`（`param_1` = trait 枚举、`param_2` = 要不要带图）：

- `DAT_1842ce340` → `"Card_Trait/"`（拼 term 名）
- `DAT_184237e80` → `"<nobr>"`；`DAT_1842cf8e8` → `"</nobr>"`
- `DAT_184237f80` → `"<nobr><b>"`；`DAT_1842cf1e8` → `"</b></nobr>"`
- `DAT_184237880` → `"<link="`；`DAT_18423b778` → `">"`；`DAT_1842cf7e8` → `"</link>"`
- 流程：`GetTermTranslation("Card_Trait/" + Enum.ToString(trait))` 取词；命中就
  `CardTraitCollection.GetTraitTextSprite(trait)` 取**图标标签**，再
  `Concat("<nobr>", 图标标签, 词, "</nobr>")` ⇒ **产出「图标 + 词」**（`param_2` 那支多套 `<link>` / `<b>`）。
- ⚠️ 出处：`DAT_` 常量的解 = **`RVA = 地址 − 0x180000000`**，再拿 RVA 去 `stringliteral.json` 的 `address` 直接对
  （实测 `0x4233780` / `0x4237e80` 这类就是 RVA，不必再换文件偏移）。

`SupportMethods__ModifyLocalization.c`：扫正文里的 `((...))`（`DAT_1842b3a48` = `\(\((.*?)\)\)`）、
按 `,` split、拼 `<link=Modular_Description/…>`。

⇒ **原版的机制就是「图标 + 词」**；这与我 §2.4 的卡面读数、以及 `CardIcons.cs` 里那句
「原版正文里 `[[枚举名]]` 展开成 `<link=…><nobr>图 + 词</nobr></link>`」完全同向。

### 3.3 枚举与类（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）

| 处 | 读数 |
|---|---|
| `DefinedTrait.cs:132-135` | `oath = 1275` · `oathInAllTurns = 1276` · `oathDouble = 1277` · `oathTripleActivation = 1278` |
| `AbilityTrigger.cs:99` | `Oath = 730`（枚举**最后一项**） |
| `CardTraitCollection.cs:27` | `[SerializeField] private Sprite **oathSprite**;` + `public Sprite OathSprite => null;` ⇒ **原版专门给 Oath 一枚图**（同段 `:24 ferocitySprite` / `:30 questPointsSprite`） |
| `CardTraitCollection.cs` | `public static string GetTraitTextSprite(DefinedTrait traitName)` ⇒ 取出可塞进 TMP 的图标标签 |
| `CardTraitOath.cs` | `public override bool IsActive(CardScript card) => false;`（与 `CardTraitDuty.cs` 逐字同形） |

### 3.4 原版的**卡面效果文字本身不在本地**（下否定结论，附搜过的路径与词）

在 `d:/2/新解包资源/assets_full/`（24.7 万文件 / 4.4 GB）里全文搜：

| 关键词 | 命中 |
|---|---|
| `Gain Armour 1` | **0** |
| `Oath 3` | **0** |
| `Card_Trait/` | **0** |
| `Catechism of Death` | 1 个 —— `bundle_spacemarinesultramarinescardassets_assets_all/Sprite/SM_UM_strat_Catechism of Death.json`（只是**贴图资产的文件名**） |

⇒ **原版卡牌 `desc` 与 I2 词条表都在远端**（与 `资料/待办判据_1018.md:114` B24 那句「原版那张卡的数据在远端 CCD」一致）。
**所以「原版文本侧怎么写的 `Oath`」在本机没有直接判据**，判据只能取**成品卡图**（铁律 7）。
⛔ 不要把 §3.2 的机制推成「原版 desc 里写的就是 `[Oath]`」——**那是推断，不是读到的**（本报告按「疑似」处理）。

---

## 4. 我们解析层今天认什么（逐处 `文件:行号`）

### 4.1 引擎侧（`RuleEngine/Core/`）—— **两种写法都认，但认的位置不同**

| 处 | 认什么 | 判据（原文） |
|---|---|---|
| `EffectText.cs:938-943` `NormalizeIconPrefix` | **`[Oath] …` → `Oath: …`**（句首限定，替换成 `$1: `） | `@"^\[\s*(codex\|mob\|oath\|strike\|slay\|rally\|backlash\|penitence)\s*\]\s*"` |
| `EffectText.cs:2819` `ReOathPaid` | **`Oath N: …`**（**必须紧挨着数字**） | `@"^oath\s+(\d+)\s*:\s*(.+)$"` |
| `EffectText.cs:1624` → `:1625` | 先 `NormalizeIconPrefix`，**再** `s.Replace("[","").Replace("]","")` | `ParseSegment` |
| `EffectText.cs:1686-1696` | 命中 `ReOathPaid` ⇒ `paidCost = N` / `paidKind = "oath"` | 「和 `12 [Energy]: …` 复用**同一条**付费路径」 |
| `EffectText.cs:5976-5987` `CostKindOf` | 规范货币名之一 = **`oath`**（`energy`/`faith`/`spirit`/`oath`/`""`） | **全仓唯一**的货币名判据 |
| `CardDef.cs:959-976` `CollectOathOps` | **从 `Desc`** 取 op，`op.Cost > 0 && CostKindOf(op.CostKind)=="oath"` ⇒ 进 `OathOps`，`OathCost = max(N)` | 「判据**转** `EffectText.CostKindOf`」 |
| `CardDef.cs:757-777` `MatchStaticBattleRule` | `[Oath]` 三条修饰句：**比较前先 `Replace("[","").Replace("]","")`**（`:772`）⇒ **带不带方括号都认** | 注释原话：「卡面把 `Oath` 印成一个图标，OCR 出来是 `[Oath]` —— 比较前**一律去掉方括号**」 |
| `CardDef.cs:1177-1207` `StartsAnotherThing` | `head.StartsWith("**oath **")` ⇒ 认定「这是另一件事」 | ⚠️ `head` 来自 `t.IndexOf(':')`，`t = NormalizeIconPrefix(low)` |
| `CardDef.cs:392` | 加载时调 `CollectOathOps()` | —— |

**⇒ 结论：`[Oath]` 与 `Oath N:` 都被认，但认的是两件不同的事**：
`[Oath]` 只在**三条「改规则」修饰句**里被认（靠去括号）；
`Oath N:` 是**付费前缀**（`ReOathPaid`），**数字必须紧跟 `Oath`**。

### 4.2 表现侧（`CardPresentation/`）—— **`card_icon_plan.json` 的 token 由 `CardIcons.Rewrite` 消费**

| 处 | 干什么 |
|---|---|
| `Core/CardIcons.cs:31` `PlanPath = "card_icon_plan"` + `:66-88 LoadPlan()` | 运行时 `Resources.Load` 计划表，按 **卡 id + 字段名（`desc`/`descZh`）** 查 token |
| `Core/CardIcons.cs:219-220` | `symbol = !(字母或数字开头)` |
| `Core/CardIcons.cs:227` | `numInArt` = 「图名里有数字」 |
| `Core/CardIcons.cs:242-255` | **`else if (!symbol && !token.StartsWith("["))` ⇒ 裸关键词：插在词前、词留着** |
| `Core/CardIcons.cs:257` | 其余（方括号 / 符号）⇒ **整串换掉、词不留** |
| `Battle/BattleDriver.cs:10584` | `CardIcons.Rewrite(c.Id, zh?"descZh":"desc", …)` —— **卡面正文只有这一条路**（`FaceTextFull`） |

**逐张推演（按 `Rewrite` 的代码路径手推，**未执行** —— 我没跑 Unity）**：

| 卡 | 计划表 token → sprite | 走哪一支 | 卡面渲出来 |
|---|---|---|---|
| 24 张（`UM83/87/78/89/71/Um_Phobos…/73/88/82/Vico/92/Angel/85/94/93/101/99/100/96/98/77/91/86`） | `"Oath N:" → "oath"` | **裸关键词**（首字符 `O` 是字母、不以 `[` 开头、`oath` 无数字）⇒ `:242` 插在词前 | `〔徽记〕Oath N: …` ✅ **与原版一致** |
| `UM84` | `"[Oath]" → "oath"` | **方括号**（首字符 `[`）⇒ `:257` 整串换掉 | `Friendly 〔徽记〕 abilities …` ⛔ **词 `Oath` 没了**（同卡 `[Talent]` 一样） |
| `UM89` / `UM_Vico_Therbeus` 的**首句** | **计划表里没有这一条** | —— | 首句 `Oath abilities …` **完全没有徽记** ⛔ |

（`gen_icon_plan.py` 的 `KEYWORD_SCAN` 正则 `(?:^|[.;]\s+)([A-Z]…)(?:\s+(\d+))?\s*([:.])` **要求 `Oath` 后紧跟数字+冒号**，
所以 `Oath abilities` 这种「没有数字没有冒号」的句中/句首形式**扫不到** —— 这是它只覆盖 24 处的机制原因。）

---

## 5. 25 张逐张表

列：`卡 id` · `卡名` · `desc`（我们池里的原文）· 计划表 token→sprite · **卡面徽记在不在** · 与 `UM84` 的 `[Oath]` 写法差异。

| id | 卡名 | `desc` | 计划表 token → sprite | 卡面徽记（原版） | 我们渲出来 | 差异 |
|---|---|---|---|---|---|---|
| `UM83` | 2nd Company Terminator | `Armour 1. Blast 3. Oath 2: Deal 3 damage.` | `Oath 2:`→`oath` | **在** ×1 | `… 〔徽记〕Oath 2: …` ✅ | —— |
| `UM87` | Bladeguard Ancient | `Your other units have +2 Attack. Oath 1: Give Armour 1 to a friendly troop.` | `Oath 1:`→`oath` | **在** ×1 | ✅ | —— |
| `UM84` | Chaplain Cassius | `Friendly [Oath] abilities apply an additional time.\n[Talent]: Catechism of Death` | **`[Oath]`→`oath`**（+`[Talent]`→`talent`） | **在** ×1（**句中**） | `Friendly 〔徽记〕 abilities …` ⛔ | 🔴 **唯一带方括号**；**词被吃掉** |
| `UM78` | Chapter Champion | `Codex: Gain +2 Attack. Oath 2: Gain Armour 1 and Vanguard` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM89` | Ferren Areios | `Oath abilities of friendly troops can be activated up to 3 times each turn. Oath 1: Deal 1 damage` | 只有 `Oath 1:`→`oath` | **在 ×2** | 首句**没徽记**、第二处 ✅ | 🔴 **少一枚（首句）** |
| `UM71` | Incursor | `Rally: Deal 1 damage. Oath 2: Stun an enemy` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM_Phobos_Lieutenant` | Phobos Lieutenant | `Long Range. Oath 1: Gain [eye icon] Stealth. Slay: …` | `Oath 1:`→`oath` | **在** ×1 | ✅ | —— |
| `UM_Suppressor` | Suppressor | `Flying. Sentry 1. Oath 1: Choose a non-Legendary Stratagem …` | `Oath 1:`→`oath` | **在** ×1 | ✅ | —— |
| `UM73` | Tactical Marine | `Oath 3: Gain +3 Attack, +3 Ranged and +3 Health` | `Oath 3:`→`oath` | **在** ×1 | ✅ | —— |
| `UM88` | Terminator Captain | `Armour 2. Oath 4: Gain Invulnerable until your next turn` | `Oath 4:`→`oath` | **在** ×1 | ✅ | —— |
| `UM82` | Terminator | `Oath 1: Gain Armour 1` | `Oath 1:`→`oath` | **在** ×1 | ✅ | —— |
| `UM_Vico_Therbeus` | Vico Therbeus | `Oath abilities of friendly troops may be activated on later turns. Oath 1: Gain Camouflage` | 只有 `Oath 1:`→`oath` | **在 ×2** | 首句**没徽记**、第二处 ✅ | 🔴 **少一枚（首句）** |
| `UM92` | Adaptive Strategy | `Give +1 [Attack] to all friendly troops. Oath 1: Give +1 Health to all friendly troops` | `Oath 1:`→`oath` | **在** ×1 | ✅ | —— |
| `UM_Angel_s_Wrath` | Angel's Wrath | `Ephemeral. Give +1 to a friendly unit until your next turn. Oath 2: Give it Armour 2 this turn` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM85` | Catechism of Death | `Ephemeral. Give +2 Attack to a friendly troop, … Oath 3: Give it an additional +2 Attack` | `Oath 3:`→`oath` | **在** ×1 | ✅ | —— |
| `UM94` | Champions of Humanity | `Give +2 Health to all friendly units. Oath 4: Give them an additional +3 Health` | `Oath 4:`→`oath` | **在** ×1 | ✅ | —— |
| `UM93` | Fall Back | `Return a friendly troop to your hand. Oath 2: Lower its cost by 2 and give it Flank.` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM101` | Indomitus Crusade | `Deploy an Ultramarines troop that costs 6 or more. Oath 6: Deal damage to all enemy troops equal to its` | `Oath 6:`→`oath` | **在** ×1 | ✅ | —— |
| `UM99` | No Mercy | `Deal 4 damage to an enemy troop. Oath 4: Also destroy all damaged enemy troops` | `Oath 4:`→`oath` | **在** ×1 | ✅ | —— |
| `UM100` | No Respite | `Give Sentry 1 to all friendly units. Oath 3: Create No Respite in your hand` | `Oath 3:`→`oath` | **在** ×1 | ✅ | —— |
| `UM96` | Shall Know No Fear | `Give Invulnerable to a friendly troop this turn. Oath 8: Give it +8 Attack and +8 Ranged.` | `Oath 8:`→`oath` | **在** ×1 | ✅ | —— |
| `UM98` | Stormhawk Interception | `Deal 2 damage to an enemy unit and adjacent units. Oath 3: Repeat this effect` | `Oath 3:`→`oath` | **在** ×1 | ✅ | —— |
| `UM77` | Attack Bike | `Oath 2: Gain +1 Ranged and Flank` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM91` | Brutalis Dreadnought | `Oath 2: Deal 3 damage to all enemies` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |
| `UM86` | Dreadnought | `Oath 2: Deal 2 damage to an enemy and adjacent units` | `Oath 2:`→`oath` | **在** ×1 | ✅ | —— |

**分布**（别把 25 张当一种情况 —— 铁律 5·c）：
- `Oath N:` **裸写**：**24 张**（每张一处；`UM89`/`UM_Vico_Therbeus` 另有**一句不带数字**的 `Oath abilities`）。
- **方括号记号**：**1 张**（`UM84`），且**只此一处**。
- 12 位/字段组合里 `descZh` 侧：24 张是 **`誓言 N：`（裸写、中文）**、`UM84` 是 **没有对应项**（中文写「誓言能力」，计划表只记了 `天赋：`）。
- **类型分叉**：14 张单位 + 10 张战术 + `UM84`（单位）= 25。（`CardDef.cs:945` 与 `RuleCore.cs:3929` 的注释写「**22 张单位卡 + 3 张改规则**」，
  与实测 25 张的构成**对不上** —— 见 §10 第 4 条，如实标着，没改。）

---

## 6. `UM84` 那处 `[Oath]` 的来路

**结论：来自【上游 OCR 源】，不是手写、也不是图集侧手加的。**

| 环节 | 读数 |
|---|---|
| ① 源数据 | `d:/4/Unity/数据/游戏数据/card_stats.json` 里 `Chaplain Cassius` 那条 **原样**就是<br>`"desc": "Friendly [Oath] abilities apply an additional time.\n[Talent]: Catechism of Death"`（同时有 `ocrName` / `ocrSrc` / `face` 字段 ⇒ 这张表是**对着 PnP 成品卡图做的 OCR**） |
| ② `cardface_fixes.json`（人工覆盖） | **没有 `Chaplain Cassius` 的 `desc` 条目** —— 该表里含 "Oath" 的键只有 `Chapter Champion`(`:90`) / `Ultramarines/Terminator`(`:137`) / `Tactical Marine`(`:140`) / `Shall Know No Fear`(`:142`) / `Terminator Captain`(`:143`) / `Attack Bike`(`:195`) / `Catechism of Death`(`:201`) 等，**都不是本处**。⇒ **不是手写覆盖** |
| ③ 生成器 | `工具/gen_icon_plan.py:130` 有 `("UM84", "[Oath]"): ("oath", "卡图 Chaplain Cassius + NCC 0.924")` —— 但这条只决定**「这枚 token 画哪张图」**，token 文本本身**来自 desc**（`TOKEN_BY_CARD` 的键必须与 desc 里的 token 逐字相同，否则是死条目） |
| ④ 落进池 | `gen_cards_engine.py`（`SRC = card_stats.json`）→ `cards_engine.json` 的 `UM84.desc` |
| ⑤ 卡面 | 见 §4.2 ⇒ 徽记画出来、**词 `Oath` 与 `Talent` 都掉** |

**先例有没有？** 「desc 里写方括号记号」这件事**在池内有大量先例**，但 UM84 与它们**都不一样**：

| 写法 | 例（都是我实读的 `desc`） | 卡面 | 我们的换法 |
|---|---|---|---|
| `[图标] 词`（**最常见**） | `[Markerlight] Markerlight 2`（`TAU47`）· `[Vanguard] Vanguard`（`TAU65`）· `[Shield] Shield`（`TAU49`）· `[Invulnerable] Invulnerable`（`SOR52`） | 图标 + 词 | 换掉方括号部分、**词留着**（词在 desc 里） |
| **`[Oath]` + 无词**（**仅此一处**） | `UM84` `Friendly [Oath] abilities` / `[Talent]: Catechism of Death` | 图标 + 词 | 换掉整串 ⇒ **词没了** ⛔ |
| 裸关键词（`Oath N:` 这一族） | 24 张 | 图标 + 词 | **插在词前、词留着** ✅ |

⇒ **「补记号」这条路是有先例的**（第 1 类遍布全池、且工作正常），
**但 UM84 是「先例写错了词」的那一例** —— 它把「图标 + 词」压成了一个 token。
**这正是「记号化」的风险所在**：只要 OCR 把「图标 + 词」压成一个 token，我们的换法就会**静默吃掉那个词**。

---

## 7. 补了会不会改行为

### 7.1 会 —— 而且 `Oath` 这一段**文字就是引擎的唯一输入**

| 链 | 出处 |
|---|---|
| `OathCost`/`OathOps` 的来源 | `CardDef.cs:959-976 CollectOathOps`：`EffectText.Parse(**Desc**)` → 逐 op 判 `CostKindOf(op.CostKind) == "oath"` |
| op 的 `Cost`/`CostKind` 从哪来 | `EffectText.cs:1686-1696`（`ReOathPaid` 命中后写 `paidCost`/`paidKind="oath"`） |
| 消费点 | `RuleCore.CanUseOathAbility` / `RuleCore.UseOathAbility` / `EffectResolver.ResolveOathAbility`（`RuleCore.cs:3936-4000`、`EffectResolver.cs:4326-4390`） |

### 7.2 逐种候选写法的判读（**代码追踪，不是跑出来的**）

| 写法 | `ParseSegment` 走到哪 | 结果 | 行为 |
|---|---|---|---|
| **`Oath N: …`（现状）** | `NormalizeIconPrefix` 不动 → `ReOathPaid` **命中** | `Cost=N` / `CostKind="oath"` | ✅ `OathCost=N`，能力可用；卡面 `〔徽记〕Oath N:` |
| `[Oath] N: …` | `:1624` 先归一成 **`Oath: N: …`** → `ReOathPaid` 要求 `^oath\s+(\d+)` ⇒ **失配** | `Cost=0` | 🔴 **誓约能力静默死掉**（`OathOps` 空、卡面也不打 `*`）；而且 `StartsAnotherThing`（`:1189`）`head="[oath n]"` 不 `StartsWith("oath ")` ⇒ **那半句会被前一句 Codex/Rally 正文吞掉**（双重触发/乱扣费） |
| `[Oath N]: …` | `NormalizeIconPrefix` **不匹配**（`]` 前多了数字）→ 但 `:1625` 的 `Replace("[","").Replace("]","")` 会补上 ⇒ 变成 `Oath N: …` | `Cost=N` ✅ | ✅ **行为不变**（但这不是自然写法，且 `StartsAnotherThing` 里 `head="oath n"` 会 `StartsWith("oath ")` ✅ 也过） |
| **`[Oath] abilities` → 裸 `Oath abilities`（`UM84` 反方向）** | `MatchStaticBattleRule`（`:772`）**本来就先去方括号** ⇒ 两版同值 | `OathDouble = true` 两版**都是** | ✅ **行为不变**（只影响显示） |

### 7.3 「改哪两处会一起变绿」—— 现有断言已经钉住了一条（灭自证）

- `RuleEngineTest.cs:10543-10545`：`Chapter Champion` 的 `Codex:` 正文 **不许含 "oath"**
  （`ct.ToLowerInvariant().IndexOf("oath") < 0`，「不停就会在触发 Codex 时**乱扣 2 费**」）。
  ⇒ 把 `UM78` 的 `Oath 2:` 改成 `[Oath 2]:` 时这条**会变红**（`[oath 2]` 仍含 `oath`）⇒ **能挡住「吞句」那种改法**。
- `RuleEngineTest.cs:6280-6281`：真卡 `Suppressor` 的 `OathOps.Count == 1 && OathCost == 1`
  ⇒ **能挡住「`OathCost` 归 0」那种改法**（至少对这一张）。
- `RuleEngineTest.cs:12558-12562`：`TestOathAbility` **夹具**显式用带方括号的字符串
  `"Friendly [Oath] abilities apply an additional time."` ⇒ 证明「引擎必须继续认方括号那一版」，
  **所以任何「把方括号从池里删掉」的改法都不会让这条变红**（它不读池）—— ⚠️ **它挡不住 UM84 那类改动**。

### 7.4 判不了的（如实写）

- **没有跑 Unity**（红线）⇒ §4.2 的「渲出来是什么」是**按 `Rewrite` 代码路径手推**，不是执行结果。
- **全池「当前有多少张真的 `OathCost > 0`」我没统计**（离线探针 `ruleprobe` 要跑 dotnet，属「跑探针」；引擎侧只有 `Suppressor` 一条真卡断言）。
- `descZh` 是否有引擎侧副作用：我读了 `CollectOathOps`/`MatchStaticBattleRule`/`EffectText.Parse` 的入口，**只吃 `Desc`（英文）**
  ⇒ **中文改动看起来只影响显示**；但**没有**全仓穷举「谁读了 `DescZh`」（只确认 `BattleDriver.FaceTextFull:10566` 那一处是显示用）——**标「疑似、置信度中」**。

---

## 8. 影响面 + 我据什么这么判

### 8.1 数据链（改 `desc` 必须按顺序重跑）

```
card_stats.json（上游 OCR 源，含 ocrSrc/face）
        │  + cardface_fixes.json（人工覆盖） + 卡牌翻译/zh_cards.json
        ▼  工具/gen_cards_engine.py
cards_engine.json（Assets/RuleEngine/Resources/）
        ▼  工具/gen_icon_plan.py --write
card_icon_plan.json  ×2 份：数据/游戏数据/  +  MyGame/Assets/CardPresentation/Resources/（运行时那份）
        ▼  工具/gen_icon_doc.py
资料/卡面图标_对照与缺口.md
```
> 依据：`gen_cards_engine.py:28,34,427`（三个输入）· `gen_icon_plan.py:43-46`（**两份产物一起写**）· `gen_icon_doc.py:16`。

### 8.2 受影响的自检（**按覆盖面**，依据 = 调用点 grep）

| 自检宿主 | 为什么受影响 | 依据（我 grep/读到的原话） |
|---|---|---|
| **`RuleEngineTest.Run`** | 解析与覆盖率吃全池 `desc`；真卡誓约走 `Suppressor`；`Chapter Champion` 的 Codex 边界 | `RuleEngineTest.cs:6280`（真卡 `OathOps`）· `:10543`（`Codex:` 正文停在 `Oath` 前）· `:5186-5199`（全池数 `Oath N:` 条数并断言 `oath > 0`） |
| **`BattleScene.Run`** | 卡面正文那条路 + **全池扫 `desc`** 的 icon 断言 | `BattleScene.cs:10767-10778`（对全池跑 `CardIcons.Rewrite`、数带 `<link>` 的卡）· 真渲那条路是 `BattleDriver.cs:10584`（`FaceTextFull`） |
| **`CardBaseDemo.Run`** | 四档分辨率渲卡面 ⇒ **文字长度一变、折行/字号跟着变** | `_run_8_checks.sh` 末段注释：「四档分辨率渲染 + 交互闭环」；`CLAUDE.md` §二「改版面之后 `CardBaseDemo.Run` **必须复跑**」 |
| `DeckScene.Run` / `CollectionScene.Run` | 卡池/收藏窗里逐张渲卡面（`CardView` → `FaceTextFull`） | `CardIcons` 是卡面正文唯一入口（`BattleDriver.cs:10584`），这两个宿主显示卡池 |
| ⚠️ **`IconSetup.Run`**（**不在** 13 条里） | 它有 4 组 icon 断言（含「②e 裸关键词：图标插在前、**词留着**」「②f 方括号：换掉、词不留」） | `IconSetup.cs:251-310`；`_run_8_checks.sh` **全文没有 `IconSetup`**（我 grep 过 `工具/*.sh`）⇒ **要手跑** |
| 离线探针基线 | `ruleprobe check` 会多出解析差异行 | `工具/ruleprobe/基线/out_baseline.txt` 存在；S11 §3 已记「基线本笔没有重建」 |

**我据什么这么判**：① §8.1 的数据链（生成器彼此喂）；② 上面逐条 grep 到的调用点；
③ `_run_8_checks.sh` 的宿主清单逐条对；④ **不据**任何「我觉得会」——凡我没读到调用点的（例如 `ShopScene`/`RewardsScene`），**没列**。

🔴 **没跑的**：Unity 13 条**一条都没跑**（红线）。上面全是**读码 + grep** 推的，**不是跑出来的读数**。

---

## 9. 候选裁定 2 个（只报不选）

### 候选 A · **保持裸写 + 只修那 3 张的「`Oath abilities`」短语**（改动面最小、方向与原版一致）

| 项 | 内容 |
|---|---|
| 形状 | ① `UM84` 的 `desc`：`Friendly [Oath] abilities …` → **`Friendly Oath abilities …`**（去方括号，让引擎那条无关紧要、让表现能插图标）；② `UM89` / `UM_Vico_Therbeus` 的首句徽记：在 `gen_icon_plan.py` 的 `TOKEN_BY_CARD` 里补 **两条带证据的**（`("UM89","Oath")` / `("UM_Vico_Therbeus","Oath")` → `oath`），并让 `desc` 那一处仍是裸 `Oath` |
| 要改哪些文件 | `数据/游戏数据/card_stats.json`（源）**或** `数据/游戏数据/cardface_fixes.json` 的 `desc` 列（走覆盖那条正规通道，**先例照 `S9`/`S11`**）；`工具/gen_icon_plan.py` 的 `TOKEN_BY_CARD`；然后重跑 `gen_cards_engine.py` + `gen_icon_plan.py --write` + `gen_icon_doc.py` |
| 改动面 | **1 张卡的 `desc`（UM84，去掉 2 处方括号，可能连带 `_manual_desc_note` 批注）+ 计划表 2 条** |
| 风险 | 中低。UM84 去方括号后 `MatchStaticBattleRule` **两版同值**（`:772` 先去括号）⇒ 引擎行为**不变**；`OathDouble` 那三条断言（`RuleEngineTest.cs:12556-12562`）是**夹具**、不读池 ⇒ 不受影响。计划表补两条要让 `gen_icon_plan.py` 的「锚点核对」过（`[Oath]` 那条已在表里，是同族先例） |
| 能不能被断言钉住 | **能**：① `CardIcons.Rewrite("UM84","desc", …)` 结果必须含 `<sprite name="oath">Oath`（图标**与词都在**）；② 反例：`UM84` 的 `desc` 不得再出现 `[`；③ `UM89`/`UM_Vico` 的 `Rewrite` 结果里 `<sprite name="oath">` **必须出现 2 次**（判别式：现在是 1 次）；④ 引擎侧 `OathDouble` 仍 true（`MatchStaticBattleRule` 不依赖方括号的**灭自证**判别式：把 `:772` 的 `Replace` 去掉，这条要能变红） |
| ⚠️ 仍留的洞 | 这只修了「`Oath abilities`」这**一族**；**同族的 `[Talent]:`（UM84 同卡）也吃掉词**——查不查、修不修由调度台定（它**超出 `Oath` 这个题面**，但属同一根因） |

### 候选 B · **全族改成统一记号形态**（`[Oath N]:` 或给 24 张都补 `[Oath]`）

| 项 | 内容 |
|---|---|
| 形状 | 把 24 张的 `Oath N:` 改写成某种带记号的形式，让 `desc` 里「看得见记号」 |
| 要改哪些文件 | `cardface_fixes.json` 的 `desc` **24 条** + `gen_icon_plan.py`（token 键要跟着换）+ 两个生成器 |
| 改动面 | **大**：24 张 `desc` 全动 ⇒ `card_engine.json` 24 处变 ⇒ 解析文本变（`ruleprobe check` 多 24 行差异）· 计划表 24 条 token 要重写 |
| 风险 | 🔴 **高**。<br>· 用 `[Oath] N:` ⇒ **誓约能力静默死掉**（§7.2 第 2 行）+ **前一句正文吞掉那半句**；<br>· 用 `[Oath N]:` ⇒ 行为**不变**，但它**不解决任何问题**（徽记现在就已画出来）、只是把文本弄得更容易出错；<br>· 且它**与卡面不符**：原版印的是**词**，`[Oath]` 这种「只有图标」的写法在原版卡面上**不存在**（§2.4） |
| 能不能被断言钉住 | **部分能**：`RuleEngineTest.cs:10543`（Codex 正文不含 oath）能挡住「吞句」那一支；`RuleEngineTest.cs:6280`（`Suppressor` 真卡 `OathCost==1`）能挡住「归 0」那一支；但**「徽记画对了没有」这一层没有断言**（`IconSetup`/`BattleScene` 里都没有 Oath 的卡面断言）⇒ 要新补 |
| 我的看法（**仅供参考，不构成裁定**） | 这一条我**没找到任何原版依据**：原版卡面 = 「徽记 + 词」，我们的裸写**已经**产出「徽记 + 词」。**改成记号会让 `desc` 离原版更远，不是更近。** |

---

## 10. 没查清的部分

1. **原版文本侧的 `desc` 到底怎么写的 `Oath`** —— **本机没有判据**（§3.4：远端 CCD）。
   我在 §3.2 读到的是**拼串机制**（「图标 + 词」），**不是**任何一处 `Oath` 的 desc 原文。
   ⛔ 谁要说「原版写的就是 `[Oath]`」，那是**推断**。
2. **`descZh` 有没有引擎侧副作用** —— 只确认了入口吃英文 `Desc`（§7.4），**没穷举**。
3. **全池当前 `OathCost > 0` 的真张数** —— 没跑探针、没跑 Unity；`CardDef.cs:945` 的注释写「22 张单位卡」，
   与实测**25 张**（14 单位 + 10 战术 + `UM84`）**对不上**，**如实标着、没改**。
4. **`card_icon_plan.json` 的两份是否逐字节相同** —— 我核了两份**都存在**、都由同一脚本写（`gen_icon_plan.py:43-46`），
   但**没有**逐字节 diff（本次不需要）。
5. **`Rewriter` 那条「裸关键词」分支在**真机上**的行为** —— 我按代码路径手推（`CardIcons.cs:242-255`），
   `gen_icon_doc.py` 生成的 `资料/卡面图标_对照与缺口.md` §三 的表述**与我的推演一致**，
   但**没有**真渲一张卡面并排比（铁律 10 第 6 条那一关**没过**）。
6. **`oath.png` 与原版 `oathSprite` 是不是同一枚** —— 原版那个 `oathSprite` **不在本地**（§2.5）；
   我比的是「卡面徽记 ↔ 我们图集切片」**同形同色**（§2.3 肉眼 + §2.2 色系），**没有**逐像素 NCC 的量化读数
   （本批的 NCC 脚本跑到超时被切到后台、没拿到读数 ⇒ 我没把没跑完的当结论）。
7. **顺手发现（不在本批范围，报给调度台分流）**：
   1. 🔴 `资料/关键词图标_现状与总表.md:122` 那一行写
      `| 30 | Oath X | 誓言 | **部署时**支付 X 能量触发 | ⚠️ 我们**只做效果句、没接「部署时」** |`
      —— **⚠️ 栏与 2026-09-16 之后的实现不符**：`RuleCore.CanUseOathAbility`（`RuleCore.cs`）已经**全链接了**
      （闸 `CanUseOathAbility` → 付费 `PayActiveAbilityCostOath` 对位 → 结算 `ResolveOathAbility`，
      真卡断言 `RuleEngineTest.cs:6280/6417`），而且「**本回合部署**」这条限制**就在闸里**
      （`if (!OathAllTurns(ctx,p) && u.DeployedTurn != ctx.Turn) return ErrExhausted;`）。
      ⇒ 该格**疑似过时**，**我没改**（只读）。⚠️ 另外那格「部署时支付」的口径来自**粉丝实体规则书**（非官方，铁律 2）。
   2. 🟡 `CardIcons.cs` 头部注释说计划表「**1369 处**（方括号 195 / 裸关键词 1095 / 符号 79）」，
      而 `资料/卡面图标_对照与缺口.md:27` 说「**167 处方括号 / 1348 处裸关键词 / 86 处符号**（共 1601 处）」
      —— **两个数不一致**（谁过期我没查，**没改**）。
   3. 🟡 `UM84` 同卡的 `[Talent]:` 也**吃掉词 `Talent`**（§6 表第 2 行），与 `[Oath]` 同根因、同形状。
   4. 🟡 S11 报告 §7.1 里那句「我们池里**含 `Oath` 的 desc 共 25 张，25 张全都写裸 `Oath N:`**」
      与它自己下一条（`UM84` 写 `[Oath]`）**互相打架** —— 实测是 **24 裸 + 1 方括号**（§5）。

---

## 11. 我搜过的目录与关键词（🔴 凡下否定结论，附这一节）

**读过的文件 / 目录**

- `d:/4/Unity/MyGame/Assets/RuleEngine/Core/{EffectText.cs, CardDef.cs, RuleCore.cs, EffectResolver.cs}`
- `d:/4/Unity/MyGame/Assets/RuleEngine/Editor/{RuleEngineTest.cs, CardProbe.cs}`
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/{CardIcons.cs, CardText.cs, Badges.cs}`
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（`:10540-10660` 卡面正文那一段）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/{BattleScene.cs, IconSetup.cs, IconSizeProbe.cs, CardFaceProbe.cs}`
- `d:/4/Unity/数据/游戏数据/{card_icon_plan.json, card_stats.json, card_stats_raw.json, cardface_fixes.json}`
- `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`
- `d:/4/Unity/工具/{gen_icon_plan.py（全文 775 行）, gen_cards_engine.py（相关段）, gen_icon_doc.py, _run_8_checks.sh}`
- `d:/4/Unity/资料/普查产出_1018/{S11_UM85与探针脚本.md, S9_GOF四张图标.md}`
- `d:/4/Unity/资料/{卡面图标_对照与缺口.md, 关键词图标_现状与总表.md, 待办判据_1018.md, 卡表核对_卡图提取/_还原效果文字.md}`

**搜过的关键词（含 0 命中的）**

| 关键词 | 搜到哪 | 结果 |
|---|---|---|
| `oath`（不分大小写） | `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条） | **0** |
| `Oath` | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs` | `:132-135` 四条 |
| `Oath` | 同上 `AbilityTrigger.cs` | `:99 Oath = 730` |
| `oathSprite` | `d:/2/新解包资源/assets_full`（全库、4.4 GB） | **0** |
| `CardTraitCollection` | 同上 | 1（`globalgamemanagers/MonoScript/MonoScript_3970.json`，只是脚本名） |
| `Card_Trait/` | 同上 | **0** |
| `Gain Armour 1` | 同上 | **0** |
| `Oath 3` | 同上 | **0** |
| `Catechism of Death` | 同上 | 1（一张 Sprite 的 `.json`，贴图资产名） |
| `Oath` | `d:/4/Unity/数据/游戏数据/*.json` 全目录 | 命中 14 个文件（最大：`card_icon_plan.json` 74 · `card_stats.json` 75 · `card_stats_raw.json` 50 · `cardface_fixes.json` 25） |
| `Oath` | `MyGame/Assets/CardPresentation/**/*.cs` | 命中 `AttackSelector.cs` / `BattleDriver.cs`（都是「主动技能那格按钮 / 誓约结算链」） |
| `Oath` | `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 153 处（真卡 `Suppressor` · `Chapter Champion` · `TestOathAbility` 三大簇） |

**卡图亲读（25/25，路径见 §2.4 表）**：`d:/2/Warpforge部队卡片/Ultramarines/{1督军,2天赋,3部队,4计策,5防御卡}/*.png`（900×1200）。
⚠️ `card_stats.json` 的 `ocrSrc` 字段里写的是**旧目录名**（`Ultramarines/The Avenging Son/…`），
**那个目录已经不在了**（现在按 `1督军…5防御卡` 分）⇒ 按 `ocrSrc` 直接取图会**全部取不到**，要按**文件名**在五个子目录里找。

**本批临时产物（不在仓库）**：`d:/tmp/wf_oath/`（`sheet_g1.png` · `sheet_g2.png` · `sheet_g3.png` · `sheet_text.png` · `um83.png` · `grid_oath.png` · `crop.py` · `sheets.py` · `grid.py` · `ncc2.py`）。
