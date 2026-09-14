# 查证：`Venomthrope`（Leviathan）裸写效果句的触发点

> 2026-09-14 · **只读查证**（没改任何已有文件 / 引擎代码）。四层过了一遍：① 成品卡图（铁律 7）② 规则书 ③ 反编译（有方法体）④ 参考实现 `d:/warpforge/scripts/rule_core.gd`。
> ⚠️ **行号按 2026-09-14 查证当时**（`CardDef.cs` / `RuleCore.cs` / `WhenEvent.cs` 那一刻正在被改，对不上就按**符号名**找）。
> **这一族为什么瞎**：`CardDef.TryParseWhenSentence`（`Core/CardDef.cs:712`）只收 `When …` 开头的句子，另加 `After receiving a Dark Pact`（`CardDef.cs:789`）。
> `Venomthrope` 是**陈述句** —— 没有 `When`、没有 `X:` 前缀、**连关键词都没有** ⇒ 一条监听器都注册不上，**静默失效**（报表也看不见）。

## 一、结论

| 卡名 | 句子 | 触发点是什么（一句话） | 证据（file:行号 / 卡图路径） | 置信度 | 建议挂在我们引擎的哪一处 |
|---|---|---|---|---|---|
| `Venomthrope` | `Destroy any troop attacked by this unit` | **本单位发动攻击时**（攻击宣言之后、被攻击者**受伤之前**）摧毁**这次攻击的宾语**＝被攻击的那个部队。即原版 `AbilityTrigger.UnitAttack = 50` 上的一条 `destroy` | ① 卡图 `D:/2/Warpforge部队卡片/Tyranid/3部队/Warpforge_23_Venomthrope.png` —— 正文区**既无关键词图标、也无关键词名**（对照同目录 `Warpforge_16_Genestealer.png`：带关键词的卡把**图标印在关键词名前面**，那张印着「⚙Swarm．👁Stealth．」）② `decomp_out/CardScript__ResolveUnitAttacked.c:30` ＝ 全反编译集里**唯一**发触发点 `0x32=50` 的地方，投递时 `actingCard`＝**攻击者**、`targetCard`＝**被攻击者**；同函数 `:100`(`580 Strike`) / `:133`(`300 Mob`) / `:154`(`301 Regiment`) 三条**全部被关键词门控** ③ `decomp_out2/BattleManager._ResolveAttack_d__438__MoveNext.c:991` ＝ 全反编译集里 `BroadcastUnitAttacked` 的**唯一**调用点，**紧接着 `:1002` 才** `CardScript__ReceiveDamage(target,…)` ④ `AbilityTrigger.cs:14` | **中**（机制＝高；枚举值 `50` 是排除法定的，见 §三） | `Core/RuleCore.cs:1064` 那句 `BroadcastWhen(ctx, WhenEventKind.Attack, …)` —— 攻击宣言、**伤害之前**，`actor`＝攻击者、`target`＝被攻击者。那句注释里本来就写着「原版依据：`BattleManagerSupport.BroadcastUnitAttacked(manager, actingCard, targetCard, …)`」，**位置已经对上了**，只差一句文法 |

### 排除掉的候选（坑，别退回去）

- **不是 `Strike`（580）**：`Strike` 是**要在卡面印图标**的关键词，这张卡一个图标都没有；而且 `ResolveUnitAttacked.c:97` 那条 580 额外要求「攻击者不会被打死」。
- **不是 `Concussion`（930）**：它是**特质**、在目标**受伤之后**才施加（`decomp_out/CardScript__ResolveDamageDealt.c:216` 判 `0x3a2=930`），正文长相也不同（见 §二 C 族）。
- **不是 `WhileInPlay`（20）**：它只在**召唤**时广播（`资料/战斗规格/战斗重建_0827/子代理读报_csharp行为_0827.md:391` 写着 `WhileInPlay(unitJustSummoned, excl×2)`），攻击时根本不响。
- **⚠️「attacked」这个词本身不是触发点标记** —— 见 §二 C 族：那 3 张的 `attacked` 只是 `Concussion` 的展开正文，触发点归关键词自己（我们已实现：`Core/RuleCore.cs:1227`）。**别写「见 attacked 就装攻击触发」的通用规则。**

## 二、同类句型在原版 1130 张卡池里的分布（`D:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`，`desc` 含 `attacked`，大小写不敏感）

| 句型变体 | 卡名 | 原文 |
|---|---|---|
| **A. `Destroy any [<限定语>] attacked by this unit`** | `Venomthrope`（Leviathan） | `Destroy any troop attacked by this unit` |
| | `Blastmaster Noise Marine`（EmperorsChildren） | `Destroy any enemy troop with Armour attacked by this unit` |
| **B. `Destroy(s) any <限定语> attacked`**（省略 `by this unit`） | `Arjac Rockfist`（SpaceWolves） | `Destroys any enemy troop with Hunt Mark attacked. Rally: …` |
| **C. `Stun … attacked`**（＝ `Concussion` 的展开正文） | `Sonic Blaster Noise Marine`（EC） | `Stun enemy troops attacked and give them -1 [armor] and -1 [attack]` |
| | `Snakebite Grot`（Goff） | `Tide 1. Stun troops attacked.` |
| | `Stikkbomb Boy`（Goff） | `Stun enemies attacked` |

**总数 6 张**；`attacked by this unit` 精确命中 **2 张**；`destroy`＋`attacked` 共 **3 张**（A＋B）。**句式变体 3 种**。
⇒ **修法是一条通用规则，不是逐卡硬写**：A/B 同形，可归成 `Destroy[s] any <限定语> attacked [by this unit]`，限定语只有两种（裸兵种 `troop` / `enemy troop with <关键词>`）。
⚠️ **C 族 3 张不要并进这条规则** —— 它们该走 `concussion` 关键词（别名表在 `Core/CardDef.cs:1403` 的 `new[] { "concussive", "concussion" }`），否则会把「被攻击者眩晕」重复实现成两份。

## 三、如实写「查不到」的一条

**原版这张卡的 `CardAbility.trigger` 字面值查不到。** 不是「没找到」，是**本地就没有**：`AbilityTrigger trigger` 挂在 `CardAbility`（`CardAbility.cs:8`）上，而 `CardAbility` 属于**卡数据资产**，按已知情况在远程 CCD（原版已关服）。
三条都试过，全部 0 命中：① `d:/2/新解包资源/assets_full/` 全部 **91 个 bundle** 的 `MonoBehaviour/*.json`（**66 543 个文件**）搜 `cardAbilities` / `Venomthrope` —— 0（含 13 个战场场景 bundle）；**再对整个 `assets_full` 目录本身（4.4 GB / 24.7 万文件，含原始 bundle 二进制）搜 `cardAbilities`，同样 0**。
② `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/` 里**没有** card data 包（只有美术/场景/monoscripts）。
③ `resources.assets` / `globalgamemanagers.assets` 导出的 **4512 个对象全是 `MonoScript`**，没有卡数据。
⇒ 所以 `UnitAttack = 50` **是排除法**（攻击路径上只有它是「攻击者自己」的、且不被关键词门控的触发点），**不是读到的字面值**。`AbilityTrigger.AboutToAttack = 280` 定义了但全反编译集里**一处都不发**，不构成候选。
**要找字面值只能等**：能拿到 CCD 的卡数据包，或在原版里给 `RawCardScript.cardAbilities[].trigger` 加探针（`d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs`）—— 但手牌注入已知失败，实况路走不通。
