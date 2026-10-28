# B89 收尾 · 红条诊断（只读诊断代理 `RD`）

> 输入 = 2026-10-28 那一轮 20 条自检里**红了的 8 个宿主**（日志在 `d:/4/_tmp_view/`）。
> 纪律：**逐条判成因、一格都不许「先修再解释」** —— 本文件零代码改动、零账改动、零 Unity 运行。

---

## §0 总表

| 宿主 | 日志 | 读数 | 红条（计入合计） | α 断言过期 | β 实现缺陷 | γ 本批回归 | δ 夹具前提 |
|---|---|---|---|---|---|---|---|
| `RuleEngineTest.Run` | `ruleengine.log` | 9490/9508 | **18** | 6 | 0 | 0 | 12 |
| `BattleScene.Run` | `battle.log` | 5662 通过 | **2** | 1 | 0 | 1 | 0 |
| `MainMenuScene.Run` | `menu.log` | （无合计行） | **2** | 0 | 0 | 2 | 0 |
| `RewardsScene.Run` | `rewards.log` | 2392/1 | **1** | 1 | 0 | 0 | 0 |
| `ShopScene.Run` | `shop.log` | 2502/8 | **8** | 0 | 8 | 0 | 0 |
| `AnimFXCheck.Run` | `animfx.log` | 240/1 | **1** | 0 | 0 | 1 | 0 |
| `LoadableRefCheck.Run` | `loadable_ref.log` | 35/1 | **1** | 0 | 0 | 0 | 1 |
| `EnchantmentTemplateCheck.Run` | `enchant_tpl.log` | 66/1 | **1** | 1 | 0 | 0 | 0 |
| **合计** | | | **34** | **9** | **8** | **4** | **13** |

**另有 1 条未计入任何合计**（`[诊断入口过期]`）：它由 `RuleEngineTest_Diag.DiagDriftAlarm()` 以 `LogError` 打出，
**8 份日志里各印一次**（ruleengine / battle / menu / rewards / shop / animfx / loadable_ref / enchant_tpl），
但**不进** `合计 通过/失败`。判档 = **α（生成物过期）**。合计口径下它是 34+1 = 35 条红。

---

## §1 逐条

### 1·1 `RuleEngineTest.Run`（18 条）

> 前 12 条全部是**同一个根因**（见 §2·1），先给逐条定位、再在 §2 统一判。

| # | 断言原文（节选） | 判档 | 证据（`文件:行号`） | 该怎么改 |
|---|---|---|---|---|
| 1 | ★ Mob **触发了两次**（2 → 4） | **δ** | 断言 `RuleEngine/Editor/RuleEngineTest.cs:17345`；夹具 `:17333-17337`（`mobber` 就是**攻击者**） | 见 §2·1：`mobber` 改成**旁观者**，另加一个不带关键词的普通攻击者 |
| 2 | ★ 判别式：那份**已经行动过**的单位**没被顺手放活**（重算只碰刚翻回来的那一个） | **δ** | 断言 `RuleEngineTest.cs:16473`；`acted` 只被写了 `Place(..., exhausted: true)`（`:16456`），**没有** `AttacksThisTurn++` | 夹具补 `acted.AttacksThisTurn = 1;`（本工程「已行动」的**真身**就是 `AttacksThisTurn >= ActionQuota`，见 `RuleCore.cs:3523`；所有 `Exhausted = true` 写点都同时 `AttacksThisTurn++`：`:11568` · `:11999` · `:15403`） |
| 3 | ★ 判别式：刚才那份**已经行动过**的单位**没被顺手放活**（重算只碰刚部署的那一个） | **δ** | 断言 `RuleEngineTest.cs:13152`；`fd.Exhausted = true;` 在 `:13145`，同样没动 `AttacksThisTurn` | 同上：`:13145` 改成 `fd.Exhausted = true; fd.AttacksThisTurn = 1;` |
| 4 | ★ **近战攻击 → `Mob:` 触发**（2 → 3 攻） | **δ** | 断言 `RuleEngineTest.cs:20674`；夹具 `:20666-20669`（`m = mobber` 且**它自己出手**） | 见 §2·1 |
| 5 | ★ **远程攻击 → `Regiment:` 触发**（2 → 3 攻） | **δ** | 断言 `RuleEngineTest.cs:20708`；夹具 `:20701-20705`（`t = trooper` 自己出手） | 见 §2·1 |
| 6 | 触发者 3 = 2 + 1（自己那条 `Mob:`） | **δ** | 断言 `RuleEngineTest.cs:20845`；夹具 `:20833-20836`（`m = mobber` 自己出手） | 见 §2·1 |
| 7 | ★ **`When a friendly unit triggers Mob` 真的响了**（2 + 2 = 4） | **δ** | 断言 `RuleEngineTest.cs:20848` | 同 #6（同一个夹具） |
| 8 | ★ **判别式**：手里那份 `watcher` 身上**真的挂了一条**（期望 1，实得 0） | **δ** | 断言 `RuleEngineTest.cs:20851` | 同 #6（监听器根本没被叫醒，所以手里那份也没挂） |
| 9 | ★ **敌方触发 Mob → 我方监听器不动**（期望 3，实得 2） | **δ** | 断言 `RuleEngineTest.cs:20867`；夹具 `:20860-20863`（`em = mobber` 是**敌方攻击者**） | 见 §2·1（敌方也要「旁观者带 Mob + 另一个兵出手」，极性判别式要重算） |
| 10 | ★ **判别式**：`ourId` 空 ⇒ **false**（期望 False，实得 True） | **α** | 断言 `RuleEngineTest.cs:30184`；实现 `RuleEngine/Core/TutorialScript.cs:1757-1761`（`ourId` 空 ⇒ `PickFirstMinion`） | 断言改成「退回列表第 0 个」，并写判据 = `decomp_full/AiScripted__GetChosenCard.c:36-40`（`iVar4 = 0;` → `get_Item(list, 0)`）。⚠️ 这条是 **2026-10-28 调度台裁定的「照抄原版」**（`TutorialScript.cs:1734` 起那个方法的 doc），实现是对的 |
| 11 | ★ 场上**没有**那一张 ⇒ 也 false（期望 False，实得 True） | **α** | 断言 `RuleEngineTest.cs:30193`；实现 `TutorialScript.cs:1767-1772` | 同上：改成「退回列表第 0 个」 |
| 12 | ★ 手牌里没有那一张 ⇒ **-1**（期望 -1，实得 0） | **α** | 断言 `RuleEngineTest.cs:30203`；实现 `TutorialScript.cs:1795-1800` | 同上：改成「退回下标 0」 |
| 13 | ★ A1562-①-bis【正例·同族】`mob` 这一族的广播照旧送到「别人」那里（期望 1，实得 0） | **δ** | 断言 `RuleEngineTest.cs:34925`；夹具 `:34911-34916`（`mobSelf` 带 `Mob:` **且自己出手**） | 见 §2·1 |
| 14 | ★ A1562-④ 带 `Mob:`＋`SelfOnly` 的监听器：自己触发时自己【要】响（期望 1，实得 0） | **δ** | 断言 `RuleEngineTest.cs:34987`；夹具 `:34979-34982`（`d = mobSelfOnly` 自己出手） | 见 §2·1 |
| 15 | ★ A1562-⑤【端到端】手里那条降费监听器照样被叫醒（期望 1，实得 0） | **δ** | 断言 `RuleEngineTest.cs:35043`；夹具 `:35036-35039`（`m = mobber` 自己出手） | 见 §2·1 |
| 16 | ★ T26 `EffectResolver.cs` 的 `using (BeginAction(ctx))` = 3（期望 3，实得 4） | **α** | 实得 4 处：`EffectResolver.cs:2519 / 9870 / 12324 / 18139`；第 4 处 = `BroadcastCreatedCardWithWarlordPreCall`（新件，本批 `E6`/账 L638·L639） | 期望改 **4**，并把出处写 `:18139`（预调 + 广播**必须同作用域**，`EffectResolver.cs:18136-18138` 有理由） |
| 17 | ★ T26 **泵点总数 = 22**（期望 22，实得 23） | **α** | 同上（19 + 4 = 23）；`RuleEngineTest_Priority.cs:327` | 期望改 **23**（该条 doc 自己写着「新增顶层入口 ⇒ 本条会红，确认后 +1」，`RuleEngineTest_Priority.cs:295`） |
| 18 | ★ A1 卡表 `version` = **36**（期望 36，实得 37） | **α** | 断言 `RuleEngineTest_EngineD4.cs:102`；真值 `工具/gen_cards_engine.py:6028`（`"version": 37`，E6 于本批 bump）+ `Resources/cards_engine.json` 首行 `"version":37` | 期望改 **37**，doc 补一行 v37 归因（三族「数据侧」列：账 L234/L867/L1044/L847） |
| — | `[诊断入口过期]`：反射 360 个用例方法、产物只认 357（缺 `TestB88PerTargetRoll` / `TestB88PlayerHandMultiplier` / `TestTacticOathChoice`） | **α** | 三个方法**确实存在**：`RuleEngineTest.cs:7659 / 7826 / 7889`；`RuleEngineTest_Diag.cs` 里 grep 这三个名字 = **0**；生成器在 `工具/gen_diag_runner.py` | 跑 `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py` 重生成（**⛔ 别手改那个文件**） |

### 1·2 `BattleScene.Run`（2 条）

| # | 断言原文（节选） | 判档 | 证据（`文件:行号`） | 该怎么改 |
|---|---|---|---|---|
| 19 | ★ 落点 = **`Idle/Spirit Stone Idle` 的世界坐标**（差 0.3836） | **γ** | 断言 `CardPresentation/Editor/BattleScene.cs:17240-17244`；改坏处 = `CardPresentation/Core/CardFeel.cs:2921`（`Vector3 pos = root.transform.position; pos.y = 0f;`）与 `:2927`（`pos = node.position; pos.y = 0f;`），本批 `R4b`（账 HUD:802）加的 | **删掉那两处 `pos.y = 0f;`**（还原整份 `position`）。判据 = `decomp_full/RemnantAeldari__GetDeathParticlePosition.c` 与 `RemnantNecrons__GetDeathParticlePosition.c`：两条都是 `Transform.get_position` **原样 12 字节拷回**（`*param_1 = *puVar2; *(undefined4 *)(param_1+1) = uVar1;` ⇒ x/y/z 全带），cpp2il `RemnantBody.cs` 的 `DoDestroyByAttack` 也是 `Vector3 deathParticlePosition = GetDeathParticlePosition();` → `Instantiate(prefab, position, rotation)`。**「只填 x/z、y 恒 0」这条判据是错的**（见 §4·3） |
| 20 | ★ A462·抬起沿：`ReleasedThisFrame()` 的代码调用点只有白名单那 7 处（`行8916` 白名单外 / `HandleMulligan` 白名单没命中） | **α** | 白名单条目 `BattleScene.cs:34278-34279`（期望行文 `if (ReleasedThisFrame()) _mulligan.HandleClick(WorldPointer());`）；实际行 `Battle/BattleDriver.cs:8916`（本批加了 `!_replaySession &&`，**同一方法里键盘那支同批也加了**，见 `:8909`） | 白名单那一格的文本改成 `"if (!_replaySession && ReleasedThisFrame()) _mulligan.HandleClick(WorldPointer());"`（或退一步用稳定子串 `"_mulligan.HandleClick(WorldPointer());"`）。⚠️ 行数 7/7 仍然对得上，**只差文本**；`!_replaySession` 的闸是调度台 2026-10-28 的裁定，别删 |

### 1·3 `MainMenuScene.Run`（2 条）

| # | 断言原文（节选） | 判档 | 证据（`文件:行号`） | 该怎么改 |
|---|---|---|---|---|
| 21 | ★ 0.5 s 到点 ⇒ 终值（期望 `30/2000`，实得 `20/2000`） | **γ** | 断言 `CardPresentation/Editor/MainMenuScene.cs:1136-1138`；改坏处 = `CardPresentation/Shell/TopBar.cs:612`（本批 `E30`/账 L706 把 `return moved;` 改成 `if (moved) FixCounterLayoutIfNeeded(parts); return moved;`） | 见 §2·6 |
| 22 | ★ 到点**自己停**（下一帧不再滚）（期望 True，实得 False） | **γ** | 同上（`MainMenuScene.cs:1139`） | 见 §2·6 |

**为什么是 γ（机制级，不是「测试太脆」）**：`FixCounterLayoutIfNeeded` 的末段会
`DestroyImmediate(Resources Bar)` → `parts.Counters.Clear()` → `BuildResourcesBar(...)` **整条重建**
（`TopBar.cs:1082-1086`）⇒ 每次调用都**换掉** `Parts.Counters[i]`（新 `CounterRef`，`Animating=false`）。
而 `StepCounters`（`TopBar.cs:601-613`）**每一帧只要 `moved` 就调它** —— 于是**滚动动画到一半被打断**。
`TopBar.cs:1031` 的注释自己写死了调用时机：「**只放在「一段滚动刚刚结束」那一拍**，⛔ 别在动画逐帧里调」，
**代码与该注释相反**。日志侧实证：`menu.log` 里 `[TopBar] 资源药丸宽变了 ⇒ 整条资源条重建`（`:2541`）
**正好落在两次 `StepCounters` 之间**，之后 `cr` 指向已销毁的旧 Label（所以「实得」还是 `20/2000`）、
新那批计数件全 `Animating=false`（所以第二条断言红）。

### 1·4 `RewardsScene.Run`（1 条）

| # | 断言原文 | 判档 | 证据 | 该怎么改 |
|---|---|---|---|---|
| 23 | 里程碑格间距(px)（(40+20) × 1.15）（81.94 ≈ 69.00±2.00） | **α** | 断言 `CardPresentation/Editor/RewardsScene.cs:1642`；实现 `CardPresentation/Shell/MissionsTab.cs:1196`（本批 `R3c`/账 215：`stepSp = (325 − n×40)/(n − 1)` = **31.25**，n=5） | 期望改 **81.94 =（40 + 31.25）× 1.15**，并写判据 = `decomp_full/EverguildLayoutGroup__CalculateSpacing.c`（`+0x74`=evenlySpaceinBetween 为真 ⇒ `set_spacing((容器同向尺寸 − n×子件尺寸)/(n−1))`，见 `:98-100`）；序列化的 `m_Spacing 20` 是**被运行期覆盖**的旧值 ⇒ 「(40+20)」那半句作废（⛔ 别改回实现） |

### 1·5 `ShopScene.Run`（8 条 · 同一根因）

| # | 断言原文（节选） | 判档 | 证据 | 该怎么改 |
|---|---|---|---|---|
| 24 | ① 主图画了（原版 `image.sprite = item.GetIcon(Large)`） | **β** | 断言 `Editor/ShopScene.cs:1943`；根因见 §2·2 | 见 §2·2 |
| 25 | （前提）名条底 `40k_main_bt_nametag` 取到并画了 | **β** | `ShopScene.cs:1945` | 同上 |
| 26 | ② 小图标画了 | **β** | `ShopScene.cs:1947` | 同上 |
| 27 | ③ 数量画了 | **β** | `ShopScene.cs:1949` | 同上 |
| 28 | 名条行矩形 = 逐项几何的映射结果（`Content/Label` **节点不在**） | **β** | `ShopScene.cs:1951-1954` | 同上 |
| 29 | 小图标边长 = 108 × 0.277778 = 30（实得 0） | **β** | `ShopScene.cs:1955` | 同上 |
| 30 | …高同样 30（实得 0） | **β** | `ShopScene.cs:1956` | 同上 |
| 31 | 两件都在名条行内部（⛔ 没溢出到主图上） | **β** | `ShopScene.cs:1958-1960` | 同上 |

### 1·6 `AnimFXCheck.Run`（1 条）

| # | 断言原文 | 判档 | 证据 | 该怎么改 |
|---|---|---|---|---|
| 32 | ② 残骸粒子的落点 = **残骸体根**（死灵那一支 = 残骸体自身；偏差 8.34E-001） | **γ** | 断言 `CardPresentation/Editor/RootVfxLayerAssert.cs:151-155`；同一个改坏处 `Core/CardFeel.cs:2921`（`pos.y = 0f`） | **与 #19 同一根因、同一修法**（删 `pos.y = 0f`） |

### 1·7 `LoadableRefCheck.Run`（1 条）

| # | 断言原文 | 判档 | 证据 | 该怎么改 |
|---|---|---|---|---|
| 33 | `SetReferenceByObject(obj)`：id 被回填（实得 `stale`） | **δ** | 断言 `Editor/LoadableRefCheck.cs:121`；夹具 `:120` 用 **`new LoadableRef<Item>("stale")`**（没有取 id 的口）；实现 `Core/LoadableRef.cs:111-121`（`_idOf == null` ⇒ 回填那段**整段不执行**），`_idOf` 是 `readonly` 且**只有** `LoadableRef(T, Func<T,string>)`（`:66-70`）能设 | 夹具改成 `var d = new LoadableRef<Item>(new Item("stale"), IdOf);` —— ④ 这条语义（`targetId = obj.GetID()`）在原版靠 `T : IIdentifiable`，我们的等价物就是注入 `idOf`，**测试漏了那个口** |

### 1·8 `EnchantmentTemplateCheck.Run`（1 条）

| # | 断言原文 | 判档 | 证据 | 该怎么改 |
|---|---|---|---|---|
| 34 | E6 模板 id 落进可玩池的有 **6** 张（期望 0；池现读 1129） | **α**（⚠️ 见下） | 断言 `RuleEngine/Editor/EnchantmentTemplateCheck.cs:283-295`；实读那 6 张 = `Mark of Nurgle/Tzeentch/Khorne/Slaanesh (enchantment)`（池里是 `Dark Pact of *`，`cost = -1`）· `Pray Enchantment` · `Reconnaissance Mission`。**HEAD 版 `cards_engine.json`（v36 / count 1129）里这 6 个 id 也全在**（只读比对过） | 期望从 0 改成一个**钉住这 6 条**的集合（`forbidden = new[]{...6 个 id...}`，并写明「这 6 张是**池内本来就有**的附魔卡/哨兵费用卡；本表收录的是同一批 SO 的另一视图」）；或改成「池的 id 集与 `count` 与模板表构建**无关**」的可判形状。⚠️ **这条要请调度台看一眼**：`v30` 的账写着「池内新增隐藏附魔模板 `Pray Enchantment`」，而 E6 的前提是「模板不该进池」——**两条口径本身互斥**，别只改期望值把矛盾盖住 |

---

## §2 同根因合并

**34 条红 = 11 个根因**（另有 1 条未计入合计的「诊断入口过期」= 第 12 个）。

### 2·1 `Mob:` / `Regiment:` 那 12 条 = **1 个根因：夹具把关键词挂在攻击者身上**（δ · 12 条）

- **本批改了什么（且是对的）**：`RuleCore.cs:6893-6908`（`B89-E14`）删掉了「在**攻击者自己**身上发 Mob/Regiment」那两发，
  改由 `EffectResolver.FireMobOrRegimentOnSameSide`（`EffectResolver.cs:18174-18211`，由 `BroadcastWhen(Attack)` 在
  `:9967` 那一跳调）对**同方其余卡**各发一次。
- **原版判据（亲读，两份互证）**：
  · `decomp_full/CardScript__ResolveUnitAttacked.c:126-146`：接收者 `!=` 攻击者（`:31-112` 那一支已 `return`）且同 `isPlayer`
    ⇒ `param_4 == 1` 跑 `OnTrigger(300 = Mob)`、`== 2` 跑 `OnTrigger(0x12d = 301 = Regiment)`；
  · `cpp2il/out_cpp2il_dev_ilspy` 的 `BattleManagerSupport__BroadcastUnitAttacked` 4 跳里，攻击者那一步是**单独**调
    `ResolveUnitAttacked(attacker, attacker, …)` ⇒ 落进**被排除**的那一支。
  ⇒ **带 `Mob:` 的单位【自己出手】不会触发自己的 Mob；它靠「友军出手」被叫醒**（规则书中文版 `:193`「友方部队执行**近战**攻击后触发效果」同义）。
- **所以 12 条断言的「期望值」建立在一个错的模型上**：夹具里 `FixtureMobber` / `FixtureTrooper` / `FixtureKwMobber` /
  `A1562_MobSelf` / `A1562_MobSelfOnly` / `A1562_Mobber` / `em`（敌方那只）**全是攻击者**，于是「Mob 该响一次」从来不会发生。
- **统一修法**：每个夹具拆成「**旁观者带 `Mob:`/`Regiment:`** + **另一个不带该词的兵出手**」，观察量挪到旁观者身上。
  逐处落点：#1 `RuleEngineTest.cs:17333-17339`（夹具 `:17339` 那行注释自己写着「攻击者」）· #4 `:20666-20670` · #5 `:20701-20709` ·
  #6/#7/#8 `:20821-20852`（夹具 `:20825`）· #9 `:20855-20872`（夹具 `:20863`）（⚠️ 这条的**极性判别式**要重算：原写「敌方那张打出 2+1=3，极性反了会变 5」依赖「攻击者自己触发」，
  改成旁观者后应断「**敌方旁观者**的 Mob 响（→+1）、我方监听器不动」）· #13 `:34911-34927`（夹具 `:34919`）· #14 `:34972-34993`（夹具 `:34981`）· #15 `:35028-35045`（夹具 `:35035`）。
- ⚠️ **修完别只留 δ 断言的平移**：改完之后**必须有一条断言真正咬住「同方其余卡」那条新路**（今天 12 条**没有一条**覆盖它 ——
  这也是「实现改对了但整套 Mob 测试反而全红」的原因）。

### 2·2 商店 8 条 = **1 个根因：`ItemDrawer.Spec` 用【入参 `art`】判 `Kind`，没用解析出来的 `s.Art`**（β · 8 条）

- `Shell/ItemDrawer.cs:800`：`Art = art ?? CurrencyNames.Icon(targetId)`（货币 code 真的解出了主图）；
  但 `:827` 的 `if (s.Kind == ItemKind.Unknown && !string.IsNullOrEmpty(art)) s.Kind = ItemKind.Generic;` **读的是入参 `art`**。
- 生产调用点是 `Spec(id, CampaignData.ItemIcon(id), …)`，而 `CampaignData.ItemIcon("C2")` **返回 null**
  （`Shell/CampaignData.cs:529-545` 只认 BoosterPack / Wildcard）⇒ `C2` 这类货币的 `Kind` **停在 `Unknown`**。
- `PickDrawer`（`ItemDrawer.cs:526-531`）：`Unknown` ⇒ `DrawerPlaceholder` ⇒ **根本走不到** `Icon` 里那句货币转交
  （`:1297` 的 `if (CurrencyNames.Of(item.Id) != null) DrawCurrency(...)`）⇒ `LastCurrency` 全默认 `false`、`Content/Label` 不存在。
- **影响面（不只是自检）**：战役奖励窗 / 战役页 / 奖励窗里凡是 `C2` 的格子（`CampaignRewardWindow.cs:934` · `CampaignTab.cs:683/720` ·
  `RewardWindow.cs:1406`）**现在画的是占位板**，不是 `CurrencyDrawer` 四件套。
- **修法（一处、1 行）**：`ItemDrawer.cs:827` 改成按**解析后**的图判：
  `if (s.Kind == ItemKind.Unknown && !string.IsNullOrEmpty(s.Art)) s.Kind = ItemKind.Generic;`
  （⚠️ 注意副作用面：`Spec(id, null, …)` 且 id 命中货币的三档从此变 `Generic` —— 正是想要的；非货币 id 的 `s.Art` 仍为 null，`Kind` 不变 ⇒ 其余调用点零变化。）

### 2·3 残骸落点 2 条（`battle` #19 + `animfx` #32）= **1 个根因：`pos.y = 0f`**（γ · 2 条）

`Core/CardFeel.cs:2921` / `:2927` 两处本批加的 `pos.y = 0f;`。原版两条 `GetDeathParticlePosition` 都是**整份 `Transform.position`**
（x/y/z，见 §1·2 的引文），cpp2il 的 `DoDestroyByAttack` 也把整个 `Vector3` 交给 `Instantiate`。
**修法 = 删掉那两处赋值**（恢复 `Vector3 pos = root.transform.position;` / `pos = node.position;`）。
⚠️ 删完 **`battle` 与 `animfx` 会同时转绿**，且 §4·3 那三份文档里的判据行要按铁律 5 就地订正。

### 2·4 T26 2 条 = **1 个根因：多了一个泵点**（α · 2 条）

新增的都是 `BroadcastCreatedCardWithWarlordPreCall`（`EffectResolver.cs:18122-18142`，账 L638/L639）；
它把「预调督军 + 棋盘广播」**成对**包进同一个 `BeginAction`（`:18139`，理由写在 `:18136-18138`）。
`ActionScope` 嵌套安全（`ActionPump.cs:88` `_outermost`），所以这条**不是缺陷**，是「期望值没跟上」。
修法 = 期望 `EffectResolver` 3→**4**、总数 22→**23**。

### 2·5 `GetChosenCard` 退化 3 条 = **1 个根因：实现按原版照抄了 `list[0]` 退回档**（α · 3 条）

`TutorialScript.cs:1734-1746` 的 doc 写着 **2026-10-28 调度台裁定「照抄原版」**（先例 = 原版 `BroadcastCardDrawn` 漏一个 `!`）；
判据 `decomp_full/AiScripted__GetChosenCard.c:36-40`。三条断言写的是**旧口径**（「⛔ 不退回 `list[0]`」）⇒ 改断言。

### 2·6 主菜单 2 条 = **1 个根因：滚动动画中途重建资源条**（γ · 2 条）

见 §1·3 的机制说明。**修法（与 `TopBar.cs:1031` 自己的口径一致）**：只在**「一段滚动刚刚结束」**那一拍调
`FixCounterLayoutIfNeeded`：

```csharp
// 形状（不是最终代码，交给写手）：只统计「本帧从 Animating 变 false」的件
bool justFinished = false;
for (...) { if (c == null || !c.Animating) continue; bool was = c.Animating;
            StepCounter(c, dt); if (was && !c.Animating) justFinished = true; moved = true; }
if (justFinished) FixCounterLayoutIfNeeded(parts);
return moved;
```

⚠️ **改完还得动测试夹具**：`FixCounterLayoutIfNeeded` 一旦真跑就会**换掉 `Parts.Counters`**（`TopBar.cs:1082-1086`），
所以 `MainMenuScene.cs:1117-1140` 那一段要么在「到点」之后**重新取一次** `parts.Counters`，
要么把 `cr.Target` 设成 `Wallet` 的**真值**（重建后的 Label 是按钱包重画的；今天夹具把它设成 30 而钱包不是 30 ⇒ 重建后必然对不上）。

### 2·7 其余 4 个各自独立

| 根因 | 条数 | 判档 | 落点 |
|---|---|---|---|
| A1 卡表 `version` 36→37（生成器本批 bump，含新列） | 1 | α | `RuleEngineTest_EngineD4.cs:102` |
| A462 抬起沿白名单行文过期（实现加了 `!_replaySession &&`） | 1 | α | `BattleScene.cs:34282-34285` |
| 里程碑格间距（`EverguildLayoutGroup` 运行期覆盖 spacing） | 1 | α | `RewardsScene.cs:1642` |
| `LoadableRef` 夹具没注入取 id 的口 | 1 | δ | `LoadableRefCheck.cs:120` |
| E6「模板不许进池」与 `v30` 的池内决策互斥 | 1 | α | `EnchantmentTemplateCheck.cs:283-295` |

---

## §3 判不了的

**没有「判不了」的条目** —— 34 + 1 条全部落进四档。两处**需要调度台拍板**（不是判不出，是口径二选一）：

1. **#34 / E6**：`v30` 的账（「池内新增隐藏附魔模板 `Pray Enchantment`」）与 E6 的前提（「模板 id 一个都不许出现在可玩池里」）
   **本身互斥**。要么把 E6 改成「钉住这 6 条既有重叠」，要么按 `v30` 重审「池内该不该放模板」。**只改期望值会把矛盾盖住。**
2. **#21/#22 / 主菜单滚动**：修 `StepCounters` 的调用时机之后，**测试夹具**还得跟着改（见 §2·6 末段）——
   如果要「零夹具改动」，就得把 `FixCounterLayoutIfNeeded` 的「重建」换成「原地重排」（大改，且 `TopBar.cs:1040-1050` 已论证过
   重建是唯一不产生第二份几何的写法）⇒ 建议走「改调用时机 + 改夹具」。

---

## §4 顺手发现（执行代理在现场才看得见的那些）

1. **本批的 Mob/Regiment 改动把整套 Mob 测试一起打红了，但新路一条断言都没覆盖** ——
   `FireMobOrRegimentOnSameSide`（`EffectResolver.cs:18174`）今天在 12 条断言里**没有一次被正向命中**。
   ⇒ 修夹具时**必须**留一条「旁观者带 `Mob:`、别人出手 ⇒ 它 +1」的正例，否则这条新路是**零覆盖**。
2. **`ItemDrawer.Spec` 的 `Kind` 判据是生产缺陷，不只是自检** —— 战役奖励里 `C2` 现在画占位板（§2·2）。
   建议同时按 `grep "ItemDrawer.Spec(" ` 逐个数一遍：凡 `art` 传 `null`/`CampaignData.ItemIcon(...)` 且 id 是 `C2/C3/C5` 的调用点都受影响。
3. **`报告/B89_R4b_HUD与特效执行.md` §1·802 的判据行是错的**（「`GetDeathParticlePosition` **只填 x/z、y 恒 0**」），
   同一句还被抄进 `判据/B89_R4_HUD与特效施工单.md:64` 与 `判据/B89_R6_BattleDriver里锁着的HUD施工单.md:435`。
   按铁律 5 要**就地订正**（保留更正痕迹），并写明「`.c` 里是 12 字节整份拷贝」。
4. **`LoadableRef.SetReferenceByObject` 在 `_idOf == null` 时静默不回填 id** —— `_idOf` 是 `readonly` 且**不序列化**，
   所以**任何经 `JsonUtility` 读回来的实例**（`LoadableRef.cs:54`）都没有它 ⇒ 那支回填**静默不发生**。
   这与「不许静默失败」冲突，建议补一次 `Note(...)`（或至少注释写明这条边界）。
5. **`[诊断入口过期]` 这条红在 8 份日志里各印一次、却一条合计都不进** —— 很容易被「合计全绿」掩盖。
   建议要么把它并进 `RuleEngineTest.Run` 的合计，要么在 `_run_8_checks.sh` 的收口里单独 grep 一次。
6. **`MainMenuScene.Run` / `BattleScene.Run` 的日志里没有 `合计 通过/失败` 行**（`menu.log` 全文无合计；
   `battle.log` 的合计在 `:295904`）。对「按行首标记数」的口径无影响，但**人工读日志时容易漏**。
7. `RewardsScene` 那条断言（#23）**同一节里还有一条 `CheckH(steps[0], 46f)` 仍然绿** ——
   说明「缩放 1.15」这半边没坏、坏的只有「间距模型」那半边。改期望时**别把 46 一起动**。
