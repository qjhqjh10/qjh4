# RV1 · 独立审查（三处高风险改动）—— 只读、只找错

> 审查代理 RV1 · 2026-10-09 · **⛔ 全程只读**：没跑 Unity、没动 git 写操作、没改任何文件（除本报告）。
> 审查对象 = 工作区里这一批的三件高风险改动 + 三份作者报告（E10 / E14 / E15）。
> 🔴 **立场**：结论一律**自己读代码 / 读反编译**得出；作者报告只当线索。
> ⚠️ **审查期间工作区在被别的写手改**（`Editor/DeckScene.cs` 我在本轮里读到至少 3 个不同版本；
> `Core/LayoutSpace.cs` 在我第一份 `git diff --stat` 之后才变成 `M`）⇒ 本报告的行号是**我读到那一刻**的，
> 凡涉及会漂的引用我一律同时给**符号名**。

---

## ① 逐条结论

| # | 归属（件 / `文件:行号`） | 结论 | 证据（逐条） | 该照哪个判据 |
|---|---|---|---|---|
| 1 | ① E10 · 「原版只有 trait 一份表示」 | **不是缺陷**（作者判断成立） | 全反编译扫描：`grep -rn "AddTraitSilently(param_1,100" decomp_full/*.c` **只命中 `CardScript__Stun.c:48`**；`DefinedTrait.stun = 100`（`il2cpp_out/dump.cs:45732`）· `blind = 975`（`:45816`）。读点全是 `HasCurrentTrait(100)`/`(0x3cf)`（`CardScript__CheckStun.c:7` · `CardScript__OnTurnStart.c:119/:123` · `CardScript__OnTurnEnd.c:125/:131` · `EntityScript__get_CurrentRangeAttack.c:25`） | `D:/2/tools/decomp_full/`（第一权威） |
| 2 | ① E10 · 「`+0x55`/`+0x56` 是**回合末摘不摘**的闸门，不是「当前是否眩晕」」 | **不是缺陷**（作者判断成立） | `CardScript__OnTurnStart.c:119-125`：`HasCurrentTrait(100)`**且**`param_2 == *(char*)(card+0x40)` ⇒ `+0x55 = 1`（blind 同形 ⇒ `+0x56 = 1`）；`CardScript__OnTurnEnd.c:124-134`：`+0x55 && HasCurrentTrait(100)` ⇒ 摘 trait + `+0x55 = 0`。`+0x40` = `EntityScript.isPlayer`（`dump.cs:21972`）⇒ 那是**侧别守卫**。`+0x55`= `stunnedAtStartOfTurn` / `+0x56` = `blindedAtStartOfTurn`（`dump.cs:21985/:21987`，属性 `:22046-22047`） | 同上 |
| 3 | ① E10 · 「闸门**全部**写点」表（报告 §2·2 那张 9 行表） | **不是缺陷**（我独立扫了一遍，与作者表逐行一致） | 写 `+0x55` 的文件只有 6 个：`EntityScript__set_stunnedAtStartOfTurn.c` · `CardScript__ResetToValuesInHand.c:152` · `CardScript__OnTurnEnd.c:128` · `CardScript__ReactToUnitJammed.c:125` · `CardScript__Stun.c:98` · `CardScript__OnTurnStart.c:121`；写 `+0x56` 的只有 4 个：setter · `OnTurnStart.c:125` · `AddEffect.c:487` · `AddEffect.c:692` | 同上 |
| 4 | ① E10 · 「Concussion 与原版 `Stun` 是同一个函数」 | **不是缺陷** | `BattleManager__ResolveStun.c:78` `cVar14 = CardScript__Stun(param_2[2], uVar13, local_208)` | 同上 |
| 5 | ① E10 · 「`Stun.c:98` 那句 `+0x55 = 0` 带 `+0x108 != 0` 守卫，我判断无条件执行」 | **不是缺陷，但作者的近似判断方向偏保守** | `CardScript__Stun.c:96-98`：`if (lVar3 != 0) { FUN_180002430(...); *(card + 0x55) = 0; }`，`lVar3 = *(longlong*)(param_1 + 0x108)` —— **守卫确实在**（作者如实标了）。⚠️ 但**施加 trait 本身**在 `:48`、**在守卫之外**；作者把它写成「无对应物 ⇒ 无条件执行」只对**清闸门**那一句成立 | 同上 |
| 6 | ① E10 · `IsStunned` 从字段改派生属性后「有没有第二份写点」 | **不是缺陷**（零残留；编译能过即无写点） | 全仓 `grep -rn "\.IsStunned\s*=" *.cs` 零命中；`UnitState.cs:159` / `:289` 两条都是 `{ get { return Has(...); } }` | `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」 |
| 7 | ① E10 · `A1121` 是真缺陷（改前眩晕**永不解除**） | **不是缺陷**（作者判断成立，我独立核了改前版本） | `git show HEAD:…/UnitState.cs` ⇒ 只有 `:618/:619` 两处置真、`:650` 只对 `blind` 置假；`HEAD:RuleCore.cs` 只有 `:1067` 那一处 `IsBlind=false`；`HEAD:EffectResolver.cs:2091/:2124` 两处置真 ⇒ **`IsStunned` 全程无置假点** | 改前代码（`git show`）+ 上面 #2 |
| 8 | ① E10 · 断言③「只废一个回合」 | **不是缺陷 —— 不是同义反复** | `BattleScene.cs:5740-5770`：夹具 `vic.StunnedAtStartOfTurn = true`（**人为掰**）、期望 `Check(!vic.StunnedAtStartOfTurn, …)` 是**字面写死的 false**，不是从被测实现读的；`Check(vic.IsStunned, …)` 期望同样写死。三条「🧨 删掉 X ⇒ 红」的构造（删 `StunnedAtStartOfTurn = false` / 删 `BeginTurn` 置位 / 删 `EndTurn` 那一趟）**结构上互斥** | `CLAUDE.md` §三「灭自证」「断言自证」 |
| 9 | ① E10 · 「失明解除点悄悄提前」对既有断言的影响面 | **不是缺陷**（作者说得对，我独立推演 + 逐步走了一遍既有断言） | 旧路径：`RuleCore.cs:1053-1080` 在 `BeginTurn` 里（**在** `ResolveAtTurn("turn_start")`(`:1110`) **之前**）按 `BlindOwner`+`BlindTurnEnd` 清；新路径：`RuleCore.cs:1341-1364` 在 `EndTurn` 里（**在** `ResolveAtTurn("turn_end")` 与 `DestroyRemnants` **之后**）摘。两刻之间能跑到的东西只有 `EndTurn` 的再生/`oath`/日志 与 `BeginTurn` 的能量/抽牌（`turn_setup` **无钩子**，见 `RuleCore.cs:1085-1088`）⇒ 正常局面下观测不到差异。既有断言 `RuleEngineTest.cs:4672-4698` 我逐步走过：**仍绿**（失明在 P2 回合末被闸门摘，断言只在 P1 下个回合开始处读 `IsBlind == false`） | 原版 `CardScript__OnTurnEnd.c:130-134`（**这才是原版的解除点**） |
| 10 | ① E10 · 「`cards_engine.json` 1126 张、0 张带 `stun`/`blind`」（= 拆掉安全垫的风险面） | **不是缺陷**（我独立跑了） | 自跑脚本（`python -I` 只读）：`RuleEngine/Resources/cards_engine.json` **1126** 张，`keywords` 里 `stun`/`blind`/`concussion` 命中 **0** | 铁律 5·c（一个值 ≠ 全部情况） |
| 11 | ① E10 · 新加断言的**条数** | 🔴 **报告数字错（不影响行为，影响台账）** | 作者 §①/§⑥ 写「共 **20** 条 `Check`」；我按块边界（`BattleScene.cs:5636-5773`，块头 = `:5635` 的 `Debug.Log("--- 眩晕/失明…")`）逐行数 ⇒ **25 条** | 铁律 6（数字只留一处、且要准） |
| 12 | ① E10 · **新段与原作者次序的偏离（作者没提）** | 🔴 **属实偏离（低危，应记账）** | 原版：再生（`HasCurrentTrait(0x406)` ⇒ `BattleManager__HealOneCharacter`）在 `CardScript__OnTurnEnd.c:84-108`，**排在**闸门摘除（`:124-134`）**之前**；我们：`RuleCore.cs:1341-1364` 的新段排在 `:1366+` 的「再生 X」段**之前** ⇒ 次序反了。今天没有任何卡的效果在再生那一刻读 stun/blind ⇒ 观测不到差异，但**这是一处与原文不同的次序** | `CardScript__OnTurnEnd.c:84-108` vs `:124-134` 的先后 |
| 13 | ① E10 · **`unstunnable`(250) 那道守卫没建模（作者没提）** | 🔴 **原版语义缺一块**（今天 0 张卡，按铁律 11 **先记后做**） | `CardScript__Stun.c:46`：`cVar13 = HasCurrentTrait(param_1, 0xfa)`，**非假 ⇒ 整段跳过**（`0xfa = 250 = DefinedTrait.unstunnable`，`dump.cs`）。我们的 `EffectResolver.DoStun`（`:2088-2108`）与 `RuleCore` 震荡（`:3266-3280`）**都不读它**（`SimpleAI.cs:631` 读了，只是 AI 打分用）。现池 `cards_engine.json` 带 `unstunnable` 的 **0 张**（我扫过） | `CardScript__Stun.c:46`；铁律 11 |
| 14 | ② E14 · 「20+ 个传**明文**的调用点零影响」 | **不是缺陷**（我独立验了，比作者强的一条：我做了全表交叉） | ① `Core/Loc.cs` 全表 **429** 条键，**每一条都含 `/`**（自跑脚本）；② 逐个 `.ShowPopUp(`/`.ShowMessagePopUp(` 的**字面首参**与全表求交：唯一两条「命中」里 `Shell/MainMenuRuntime.cs:670` 是 `/// <item>` **文档注释**（不是代码），`Editor/DeckScene.cs:4103` 是**故意传键**的自检站点 ⇒ **真正的明文站点一条都不撞键** | `Core/Loc.cs:1826`（`T` 缺键返回键名） |
| 15 | ② E14 · 「`Terms.ContainsKey` → `Loc.HasEntry` 那一跳，可见结果今天不变（数字键不在表里）」 | **不是缺陷**（作者判断成立，我独立 grep 了全表） | `grep -n "MenuDeck/Error" Core/Loc.cs` ⇒ 只有**具名族** + `SaveFailed` + `Import*` + 整句族，**零条数字键**；`DeckRuntime.cs:3846` `MenuDeckErrorKeyFmt = "MenuDeck/Error/{0}"`、`:3849` 兜底键 | `Core/Loc.cs:1009-1021`（具名族） |
| 16 | ② E14 · 「三条裸键都在 `Loc` 里」 | **不是缺陷** | `MenuDeck/HUD/DiscardChanges` = `Core/Loc.cs:1124` · `MainMenu/General/Discard` = `:1105` · `MainMenu/General/Cancel` = `:1104` · 兜底键 `MenuDeck/Error/InvalidDeck` = `:1133` | 同上 |
| 17 | ② E14 · 「`MenuDeckErrorKey` 生产调用点只此 1 处」 | **不是缺陷** | 全仓 `grep -rn MenuDeckErrorKey` ⇒ 生产侧只有 `Deck/DeckRuntime.cs:3823`（`ShowInvalidDeckPopUp`）；其余全在 `Editor/DeckScene.cs` 的自检里 | 铁律 10「先查全量」 |
| 18 | ② E14 · 「`Loc.T` 的缺键兜底 = 返回键名 + 出声；`HasEntry(null)` 返回 false」 | **不是缺陷** | `Core/Loc.cs:1826-1841`（`T`）· `:1885-1900`（`HasEntry`）；`T(null)`/`T("")` ⇒ `""` | 本项目已定案口径 |
| 19 | ② E14 · 「明文站点会多记 `MissingCount` / 多一条 warning，但**没有**绝对断言读它」 | **不是缺陷**（我独立 grep 过） | 全仓读 `Loc.MissingCount` 的断言全是**增量**：`BattleScene.cs:3036/3050` · `DeckScene.cs:5780/5794` · `SettingsScene.cs:716/724` —— **零处**比绝对值 | 同上 |
| 20 | ② E14 · ShellScene 那 4 个明文站点（`:958/:999/:1014/:1026`） | **不是缺陷**（仍绿） | `Editor/ShellScene.cs:987/991/1006/1007/1008` 断的是 `自检弹窗` / `确定` / `取消` / `A416·两钮·明文正文` —— 四条都**不含 `/`**，不可能是键（见 #14 ①） | 同上 |
| 21 | ③ E15 · 「中心与半宽**成对**改、**同一条斜率**」 | **不是缺陷**（我读了 `LayoutSpace` 自己验，没信作者的帧表） | `Core/LayoutSpace.cs:242-244` `ToDesignPixel` 的 x = `world.x * PxPerWorldX + 960`（`PxPerWorldX` 在 `:221-227` = `1920 / VisibleWidth`）；`CampaignTab.cs:582` 半宽用的**同一个** `PxPerWorldX` ⇒ 同斜率 ✓。`_vpR` 确是设计 px：`Core/UguiRect.cs:26`（`PxRect`「单位 = 设计像素 1920×1080」）+ `CampaignTab.cs:263`（`_vpR = UguiRect.Child(...)`） | `Core/LayoutSpace.cs:177-196` 的公允口径 + `Core/UguiRect.cs:26` |
| 22 | ③ E15 · 「`halfLen` 仍取 `ab.magnitude`（总长）而非 `|ab.x|`」 | **不是缺陷：方向确是「只多建、不误剔」**（我独立推导，✅ 与作者一致） | 真 x 半跨 = `|ab.x| * PxPerWorldX * 0.5`；`ab.magnitude >= |ab.x|` 且乘**同一个**斜率 ⇒ 算出的区间**恒覆盖**真区间。剔除条件（`CampaignTab.cs:583-584`）`midPx.x + halfLen < _vpR.x1 - 2 \|\| midPx.x - halfLen > _vpR.x2 + 2` 在区间**更大**时**更难**满足 ⇒ 只会少剔 ⇒ 只多建 ✓ | 同上 |
| 23 | ③ E15 · 「`UpdateFpsDrag` 只有 x 真的变、y 本来就同值」 | **不是缺陷**（y 那一半我逐字验了） | `Core/LayoutSpace.cs:244` `ToDesignPixel` 的 y **直接转调 `PxY`**；`:188` `PxY(v) = 540 - v.y*108` ⇒ 与旧式 `LayoutSpace.PxY(wp.y)` **逐位相同**。x 那一半 `PxX`(`:184`) vs `PxPerWorldX`(`:221`) 只差 `r` 倍 | 同上 |
| 24 | ③ E15 · 「`ToPixel/PxX` 的 x 斜率写死 108、只在 16:9 等于设计 px」 | **不是缺陷** | `Core/LayoutSpace.cs:179` `k = DesignPxH / DesignHeight = 108`；`:184` `PxX = worldX*108 + 960`。`108 == 1920/VisibleWidth` ⟺ `VisibleWidth == 17.7778`（=`DesignWidth`，16:9） | 同上 |
| 25 | ③ E15 · 「三处在 16:9 下只差 float 舍入（≤2.44e-4 px）」 | **不是缺陷**（我实测算出来更小、作者的数字是上界） | 自算（float32）：`PxPerWorldX` 在 16:9 实得 **107.99999237060547**，与 108 差 **7.63e-6 / 世界单位**；可见世界宽只有 17.78 ⇒ 单段连线误差 ≤ **7e-5 px** ≪ 2.44e-4 ✓ | 该断言自己的容差（本仓最紧 0.01 / 0.05 px） |
| 26 | ③ E15 · §5.1 #3「`LayoutSpace.cs` 的兄弟清单也要改」 | **不是缺陷（已被另一位写手改掉）** | `git status` 显示 `Core/LayoutSpace.cs` 现为 `M`；diff **只有注释**（`:204-210`）⇒ 现读两份清单（`LayoutSpace.cs:205-210` 与 `MenuDraw.cs:597-601`）**都列 6 处**、逐条一致 | 铁律 6（两份要一起改） |
| 27 | ③ E15 · **`FpsPressAtCanvas` 内部的 `hx` 仍是 108 帧（作者没提，且是本批新引入的不一致）** | 🔴 **缺陷（非 16:9 可观测）** | `Shell/SettingsWindow.cs:2490` `float hx = _fpsHandle.transform.position.x * 108f + 960f;` —— 就是内联的 `LayoutSpace.PxX`（`:184`）。本批把喂进来的 `px` 改成设计帧（`:2433-2434`）⇒ **同一函数内 `px`（设计帧）与 `hx`（108 帧）不同帧**。改前两者同为 108 帧。4:3（`r=0.75`）下 `\|px - hx\| = 0.25*\|px-960\|`，手柄离中心 480 px 时差 120 px ≫ `half`（≈8，`FpsHandleSquare*RootScale*0.5`）⇒ 「按在手柄正中间」那条判据恒假 ⇒ `_fpsGrabPx` 恒 0（拖手柄会跳一下） | `Core/LayoutSpace.cs:184` vs `:221-244`；16:9 下两者重合（所以单态断言抓不住） |
| 28 | ③ E15 · §5.1 #5「`MenuDraw.cs` 的 `CheckAbsorbLayer` 一族是同族双料，未修」 | **属实（潜伏，未修）** | `Shell/MenuDraw.cs` `float w = q.WorldW * 108f; float cx = LayoutSpace.PxX(q.transform.position.x); near(cx - w*0.5f, x1, 1.5f, …)`，`x1..y2` = 字面设计 px ⇒ ①帧错 ②父链没除一次。宿主全钉 16:9（断言辅助件）⇒ 今天不红 | 同 #21/#24 |

---

## ② 【作者的判断我复核后**不成立**的】

| # | 作者原话（哪份报告哪一节） | 我实测的 | 差在哪 |
|---|---|---|---|
| A | **E14 §④(c)**：「`Editor/DeckScene.cs:3799`（现 :3802 / 重写后 :3830）—— `Term` 转 `Loc`，`Loc` 表没有 `MenuDeck/Error/5` ⇒ 返回**键名本身** ⇒ **红**」；随后建议「随 `:3796-3798` 一起改/删」 | 🔴 **这一条不会红**。`Term` 的实现（`Shell/PopUpGameWindow.cs:188-194`）**保留了 `Terms` 先查那一跳**，而该行**前面**（原 `:3796`）刚把假词条塞进 `Terms` ⇒ `Term("MenuDeck/Error/5")` 取到的就是 `"（自检塞的假词条）"` ⇒ 期望值相同 ⇒ **绿** | 作者在同一节里自己写着「那一跳**我已经保留了**（`Terms` 先查）」—— 与「它会红」**自相矛盾**。按他的建议改/删会**删掉一条仍然有效的断言**（而它正是「两处表、两个口」那条灭自证的**另一半**）。✅ 现读该块已被另一位写手重写成一对「选键看 `Loc` / 取字先看 `Terms`」的灭自证 —— 与我这条结论同形 |
| B | **E14 §④ 标题**：「共 **5 条**确定会红 + ……」（正文 (a)(b)(c) 列了 **6 条**） | 🔴 我逐条过完全仓 `Editor/*.cs` 读 `MessageShown/PrimaryShown/SecondaryShown/MessageKey/PrimaryKey/SecondaryKey` 的**全部**断言（只有 `DeckScene.cs` 与 `ShellScene.cs` 两处有 `*Shown`）⇒ **真会红的是 5 条**：`:3836-3837`（现 :3879-3884 区）/`:3838-3839`/`:3840`/`:4118`/`:3796-3798`（现 :3800/:3827）。**没有第 7 条** | 多算的那条就是 A；作者**没漏**第 7 条（`Editor/DeckScene.cs` 里我核过 `:3788-3789`(现 :3814) `Terms.Count == 0` 仍绿 · `:3835`(现 :3870) 断 `MessageKey` 仍绿 · `:4119`(现 :4188) 断 `SecondaryShown == null\|\|""` 仍绿 —— 作者对这三条的「仍绿」判断**正确**） |
| C | **E15 §4·1「牙口」表 第 ② 行**：「只把 `halfLen` 换回 `108f`」⇒ 16:9 绿 / 4:3 **下界红** / **21:9 下界红** | 🔴 **21:9 那一格方向写反了，应为「上界红」**。21:9 下 `PxPerWorldX = 1920/23.3333 = 82.2857`（我实算），`108 > 82.2857` ⇒ 换回 `108f` 会把半宽**放大 1.3125 倍** ⇒ 区间**更大** ⇒ 「整段在视口外」**更难**判中 ⇒ **多建** ⇒ 触犯的是**上界（不许多建）**，而下界（「该建的必须建」）只会更满足 | 作者把「半宽变小 ⇒ 剔除变严」这条（只在 4:3 成立：108 < 144）误当成了**两边都成立**。21:9 的斜率比 108 **小**，方向相反 |
| D | **E10 §① / §⑥**：「共 20 条 `Check`」 | 🔴 实测 **25 条**（`Editor/BattleScene.cs:5636-5773`，含 `CheckTrue`/`CheckNear` 在内的块内全部检查） | 纯计数错（不影响行为），但台账数字要与实情一致（铁律 6） |
| E | **E10 §2·3 / §④·6** 把 `CardScript__Stun.c:98` 的 `+0x108 != 0` 守卫判成「场上有实体卡 ⇒ 无条件执行」 | ⚠️ **部分不成立**：那句 `= 0` 在 `if (lVar3 != 0)` **之内**，而**施加 trait**（`:48`）在**之外** ⇒ 「施加」无条件、「清闸门」有条件。作者把它整体写成「无条件执行」，把两件事合并了 | 结论方向不至于错（我们两件都无条件做），但**判据被写得比事实强**；建议在注释里分开写 |

---

## ③ 【我没查清 / 判不了的】

1. 🔴 **`CardScript__OnTurnEnd` 里闸门摘除块的可达性 —— 我判不了**。
   摘除块在 `:124-134`，但它外面套着一层 `poisoned(200) / resistant(600)` 守卫（`:110-113`）：
   `if (HasCurrentTrait(200) == 0 || param_2 != isPlayer || HasCurrentTrait(600) != 0) { …摘除… }`，
   而同一文件 `:260` 又有一句 `goto LAB_1805f2c8d`（标签 = `:113`，即摘除块的**入口**）从**后面**跳回来。
   ⇒ 反编译器把共享基本块摊在了「首次出现处」，**我无法从这份清单确定**「一张 `poisoned` 且非 `resistant` 的卡，在**自己**回合末会不会走到摘除」。
   **缺什么**：需要按 VA 反汇编（`资料/全量反编译_入口与用法.md` 那条路）看真实 CFG，或跑原版实测。
   **今天的影响**：我们 `EndTurn` 是无条件摘（不看这两道 trait）—— 若原版对毒单位跳闸门，则那一格我们与原版不同。**未验**。
2. 🔴 **新加断言一条都没跑过**（E10/E14/E15 三位作者都没跑，我也没跑 —— 红线）。
   本报告里所有「会红 / 仍绿 / 顺序对不对」都是**静态推演**。我逐步走过 `RuleEngineTest.cs` 那 7 处读 `IsStunned/IsBlind` 的既有断言（`:4459/:4461` · `:4684-4696` · `:7099` · `:10486` · `:13978` · `:15329-15332` · `:20969`），**判为全绿** —— 与作者结论一致，但**同为未跑**。
3. ⚠️ **`Editor/DeckScene.cs` 正在被另一位写手改**（我在本轮里读到至少 3 个版本：`MessageShown` 断言从「断键名」→ 已改成 `Loc.T` + 「不是键名」的灭自证对；§① 那块被整段重写；行号漂了 3 次）。
   ⇒ 我对该文件的结论**成立于我读到的那一刻**；**判红绿必须以收口那趟的实跑为准**。
4. ⚠️ **`SettingsWindow.UpdateFpsDrag` 那一层换算今天仍没有任何口能被批处理驱动**（作者 E15 §4·2 给的两条路都没落；E18 的报告说「甲」没做成）。
   ⇒ 我在 #27 报的那条**没有断言护着**，且**今天也跑不出来**。**缺什么**：一个能绕过 `Mouse.current == null` 的口（E18 §④·1 给的三条修法）。
5. ⚠️ **`CampaignTab` 连线剔除的「形状」严格化（`ab.magnitude` → `|ab.x|`）我判不了**：原版那一段（`CampaignNode.Connect` 的视口剔除）判据不在本地（作者 §5.1 #4 也这么写）。
   我**只**能判「当前形状是**保守**的、方向安全」（见 #22）—— **判不了原版是不是也这么保守**。
6. ⚠️ **E10 报的「`turn_start` 触发段与闸门置位的次序」那一格**（`RuleCore.cs:1164-1169` 自陈的已知近似）我**没法验**：今天卡池里没有「在自己回合开始时把自己这一侧的单位晕掉」的写法，构造不出来。原版是**逐卡**跑 `OnTurnStart`（`CardScript__OnTurnStart.c` 逐卡入口），我们是**整段** `ResolveAtTurn` 跑完才置闸门 ⇒ 那一格原版可能置 1、我们置 0。**未验**。
7. ℹ️ **`BattleManagerSupport__BroadcastTurnStart.c:29/:39` 那两处 `CardScript__OnTurnStart(lVar3)`（只传 1 个实参、丢 `param_2`）** 是反编译器的实参缺口还是一种真重载，我**没查清**。若在那些调用里 `param_2` 是垃圾值，则 `param_2 == +0x40` 那条侧别守卫对**在场卡**未必成立 —— 那会影响 #2 的等价性结论（我们按「只扫 `p.Board`」兑现它）。**缺什么**：签名桩核对或反汇编。**今天的影响**：即便有差，也只影响「非行动方单位在别人回合开始会不会置闸门」，而那正是守卫要排除的。

---

## 附：这一轮我实际做过的核查（可复核）

- 反编译实读：`CardScript__OnTurnEnd.c` · `CardScript__OnTurnStart.c` · `CardScript__Stun.c` · `CardScript__AddEffect.c`（:465-500 / :680-700）· `CardScript__ResetToValuesInHand.c:144-152` · `CardScript__ReactToUnitJammed.c:125` · `BattleManager__ResolveStun.c:78` · `BattleManagerSupport__BroadcastTurnEnd.c` · `BroadcastTurnStart.c` · `BattleManager._NextTurn_d__395__MoveNext.c:371` · `il2cpp_out/dump.cs:21960-22050`（EntityScript 字段）与 `:45725-45860`（DefinedTrait 全表）。
- 全反编译扫描：`grep -rn "AddTraitSilently(param_1,100"` · `grep -rln "param_1 + 0x55) = "` · `grep -rln "param_1 + 0x56) = "`。
- 工作区实读：`git show HEAD:` 改前版（`UnitState.cs` / `RuleCore.cs` / `EffectResolver.cs`）逐行比对。
- 只读脚本（`python -I`）：`cards_engine.json` 1126 张关键词扫描（stun/blind/concussion/unstunnable 全 0）· `Core/Loc.cs` 429 条键全表（全含 `/`）· 全仓弹窗调用点字面首参与全表求交 · float32 复算 `PxPerWorldX`（16:9 = 107.9999924 / 4:3 = 144 / 21:9 = 82.2857）。
- 逐条走读的断言：`Editor/BattleScene.cs:5636-5773`（25 条）· `Editor/DeckScene.cs` 全部读 `*Shown`/`*Key` 的断言 · `Editor/ShellScene.cs:980-1030` · `RuleEngineTest.cs` 7 处 `IsStunned/IsBlind`。
