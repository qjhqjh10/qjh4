# B9 · D1BG · `BattleScene.Run` 16 红定性

- 跑次：`d:/4/_tmp_view/battle.log`（2026-10-27 07:43）· `BT === 合计：5528 通过 / 16 失败 ===`
- **计数口径**：按**行首标记**数 = **BT 12 条 + E7 4 条 = 16**（`E7 合计：20 通过 / 4 失败`，其余都是通过行/汇总行里的 `✗` 字样）。
- 另有 **1 条不计入 16 的红**（`RE ✗ 诊断入口过期`，日志第 262 行）→ 见 §四·1。
- ⛔ 本报告只读诊断，没有改任何代码/账/正本。

---

## ① 逐条表

| # | 断言原文（截断） | 文件:行 | 分类 | 证据 | 该怎么改 / 哪笔改动 / 缺什么 |
|---|---|---|---|---|---|
| 1 | `原版卡拖上场成功： → 槽 5` | `Editor/BattleScene.cs:15906`（日志 150040） | **δ** | 日志 `[CardPresentation] 落位：Hand_Scout → 槽 5`（150026）→ 引擎格位**永远没填**；`落位登记被新一局接管了（Placed 1 个）` 绿 ⇒ 松手这一侧确实走了 ok 支。`Hand_Scout` = `UM1 "Scout"`，`cards_engine.json` 里 **`type=unit` / `cost=1` / `targeted=1`**（技能 = `Rally: Deal 1 damage to an enemy`）。`BattleDriver.TryBeginPlayTargetPick`（`BattleDriver.cs:7967`）2026-10-26 起对**所有 `targeted==1`** 的卡开闸（`BattleDriver.cs:8018`），合法目标池非空（敌方督军恒在）⇒ **落地即进「等目标」态 ⇒ 引擎 `PlayCard` 根本没被调**。松手到断言之间**一条日志都没有**（既无 `引擎拒绝了这次落位`、也无 `回放局里…被拒`、也无 `落位回调找不到这张牌`）⇒ 与「等目标」这条静默支逐条吻合 | **本块夹具要挑「非 targeted 的单位卡」**：`... && hand[i].Card.IsUnit && hand[i].Card.Targeted != 1`；**并且**用硬前提断言（`Check(dragIdx >= 0, "（前提）…")`），手牌凑不出就**注入一张夹具卡**（`ctx.NewInstance(def)`），⛔ 别落回「`if (dragIdx>=0)` 静默跳过」那个反模式（`CLAUDE.md` §三 那条）。<br>想保留「拖 `targeted` 牌」这一档的话：拖完补 `driver.SimulatePlayTargetPickForTest(敌督军侧, 槽)` 再断引擎格位 |
| 2 | `★ 拖空松手 ⇒ 提示条上是原版那句 Battle/Tips/DragToTarget（期望「拖动到有效目标」，实得「」）` | `Editor/BattleScene.cs:15950`（日志 150079） | **δ（第 1 条的级联）** | 150026 之后到 150079 之间**没有任何第二条 `[CardPresentation] Release：…` 日志**（我逐行查过：那一段里唯一的 `Release` 栈是 150026 那一次的栈），也**没有**「落点不合法…回弹」⇒ 第二次拖拽**根本没抓到牌**。原因同 #1：第一张松手时 `CardInteraction.Release` 已 `_cards.Remove(card)`，而「等目标」支**刻意不调 `SyncHand()`**（`BattleDriver.cs:7998-8002` 注释写明），`_handViews[0]` 仍指向那张已离手的卡 ⇒ `missIdx=0` 拿到的视图抓不起来 | 同 #1：只要第一张不再进等目标态（或拖完立刻取消/点目标 + `RefreshAll()`），本条自然回绿。**另建议**：`missIdx` 的挑选加一条「这张视图确实还在 `_cards` 里」的判据，别只判 `HandViewAt(i) != null` |
| 3 | `★ ② 的前提：刚拖上场那一格有单位` | `Editor/BattleScene.cs:15967`（日志 150105） | **δ（#1 级联）** | 该格本来就没单位（#1） | 同 #1 |
| 4 | `刚打出去的那张卡在场上找得到（按槽位）` | `Editor/BattleScene.cs:15992`（日志 150118） | **δ（#1 级联）** | `driver.BoardViewAt(freeSlot)` = null（引擎格位空 ⇒ 没有场卡视图） | 同 #1 |
| 5 | `★ 换上场的这张展示场景是 Board（实为 ）` | `Editor/BattleScene.cs:15994`（日志 150131） | **δ（#1 级联）** | `dv == null` ⇒ 打印空 | 同 #1 |
| 6 | `★ 手牌打出去的卡也有 3D 卡体` | `Editor/BattleScene.cs:15996`（日志 150144） | **δ（#1 级联）** | 同上 | 同 #1 |
| 7 | `★ 换上场的这张不再露 2D 立绘层 / 卡框` | `Editor/BattleScene.cs:15998`（日志 150157） | **δ（#1 级联）** | 同上 | 同 #1 |
| 8 | `★ R18③（行为级判别式）：整条位移里 X 反拧到 ≈ −43°（实得最低 -37.5°）` | `Editor/BattleScene.cs:2009`（日志 115467） | **α** | 本会话 `B9 E4BDR 账 505` 把 `MinionAxisRot` 的算子从 `SlerpUnclamped` 改成 **`LerpUnclamped`**（`BattleDriver.cs:23673`）。判据 = **原版 `.c` 明写 `UnityEngine_Quaternion__LerpUnclamped`**（`d:/2/tools/decomp_full/CardScript__ApplyMovementDisappearAnimRotations.c:54`）⇒ **实现是对的**。旧阈值 `xMin < -38f` 是按旧 Slerp 值（`-42.8°`）调的：10-25 日志同一行 = `最低 -42.8°`（`_tmp_view/20261025/q_battlescene20.log:109177`）。我按 Unity 的四元数语义 + 那条 hermite 曲线（键 `(0,0)/(0.2373,-0.952)/(1,1)`，`Keyframe(t,v)` 两端切线 0）**复算：Lerp 最低 -37.47° · Slerp 最低 -42.77°**，与两侧实测（-37.5 / -42.8）逐位吻合 ⇒ 差值 5.3° 全部来自算子 | 阈值改 **`xMin < -35f`**（真值 −37.5），文案 `≈ −43°` → `≈ −37.5°`，并把「🧨 改回 `Slerp`」那条改坏法的描述改成**「`SlerpUnclamped` 与原版 `.c` 不符」**（现在写反了：照它说的改，反而会离开原版） |
| 9 | `★ 弹的是**这个词**那一条（实得「进行或接受近战攻击时所造成的伤害。」）` | `Editor/BattleScene.cs:25550`（日志 194037） | **α** | 扫到的 link 键是 **`melee`**（日志 193972）；生产路径 `TickTraitTip` 用的是 **`TipText.ByLink(key)`**（`BattleDriver.cs:32315`），而 `ByLink` 对三个键**走常量、不过 `Trait`**：`case "melee": return Melee`（`Tooltip.cs:1166`，`Melee = Loc.T("Tips/MeleeAttackTip")` = 那句 17 字正文）。断言却拿 **`TipText.Trait(foundKey)`** 比 —— `Trait` 返回的是 `Compose(...)` 拼出来的 `<b>标题</b>\n正文`（`Tooltip.cs:1002`），两者**结构上不可能相等**。10-25 那次绿是因为扫到的是 `codex`（走 `default: return Trait(id)`，`Tooltip.cs:1169`） | 期望值改 **`TipText.ByLink(foundKey)`**；为保住「灭自证」，再加一条判别式：`ShownBody != TipText.ByLink(<另一个不同的关键词>)`（否则 `ByLink` 退化成「认不出给第一条」照样绿） |
| 10 | `没选称号（ProfileData.TitleIndex = −1）⇒ 我方整块关（底条 + 文字）` | `Editor/BattleScene.cs:26232`（日志 195991） | **γ** | 本会话 `B1UI`（`U2`V1）**改的是 `ProfileData.TitleIndex` 的读口**（`Shell/ProfileData.cs:363-366`，`git diff` 确认是唯一一笔）：<br>`get { EnsureSave(); if (_titleIndex < 0 && Titles.Count > 0) _titleIndex = 0; return _titleIndex; }`<br>⇒ **`-1`（没选）在外部读不到了**，`TitleName` 随即返回第 1 条 ⇒ `RefreshHudTitles` 把称号贴上去 ⇒ 「整块关」这一档**不可达**。同一节的 `drv.SetTitle(null,null)`（26259）仍然绿，证明**关闭态本身的测量是好的**，红的就是数据口 | **判据冲突，要调度台裁**：原版「已装备称号 = 服务器下发」这一档在**单机离线**下确实没有数据；但读口**就地改写字段**会让「玩家主动取消称号」永远失败（`SaveDto.titleIndex = -1` 的语义也废了）。建议改成：**在装载/首次读盘那一刻兜底一次**（例如 `EnsureSave()` 里判「存档里没有这一格时才置 0」），读口保持纯读；或者保留读口兜底、但**另给档案窗一个显式的「取消称号」通道**，并把 26232/26249 两条断言的**前提**改成「玩家还没选过」而不是「`TitleIndex == -1`」 |
| 11 | `★ 取消选择 ⇒ 又整块关回去（判据跟着数据走，不是一次性开关）` | `Editor/BattleScene.cs:26249`（日志 196030） | **γ（#10 同根）** | 断言里先 `ProfileData.TitleIndex = -1` 再 `RefreshHudTitles()` —— setter 存进去了 `-1`，但 `RefreshHudTitles` 读的是 `ProfileData.TitleName` ⇒ 又走 #10 那个会被改成 0 的读口 | 同 #10 |
| 12 | `★ **判别式**：ActiveAbility（6 关数据里 0 条）仍然是桩 ⇒ 返回 false + 进 Unhandled + 出声` | `Editor/BattleScene.cs:35219-35223`（日志 239490） | **α**（同时也是**本批两个代理的口径打架**，见 §二） | 本会话 `TutorialScript.cs` 把桩换成了真实现（`git diff`：`Stub(...)` → `return RunActiveAbility(ctx, a, ref done);`，新方法在 `TutorialScript.cs:1072`）。日志里那条 `[Tutorial] …「ActiveAbility」**没有执行**：认不出要放技能的那张卡…` 来自 `Fail()`（自检合成的动作没写 `unitType`），**不是** `Stub()`。<br>**原版确实有这一支**：`decomp_full/AiScripted__ExecuteAction.c:388-420` —— `if (iVar8 == 0x28) { … CardScript__SetupActiveAbility(lStack_340,0); … }` → `:608 BattleManager__AddAutoActionToQueue(…, 0x1e /* playActiveAbility */)` ⇒ 实现方向对（`铁律 11`：原版有就得做） | 断言改写成「**已实现**」那一档：`Check(aaScript.ExecuteAction(cT1, aaAct) == false && aaScript.Unresolved.ContainsKey(ActiveAbility) …)`（用当前的「认不出 acting ⇒ `Fail` 出声」当判别式），或者给夹具补 `unitType` 断「真的走到 `RuleCore.UseAbility`」。⛔ 别为了让这条绿把 `RunActiveAbility` 退回去（那会离开原版）。**另**：`TutorialScript.cs:1138/1177/659/877` 那几句「只有 `ActiveAbility` 走得进来」的 doc 已成过期描述，要一起改（铁律 5） |
| 13 | `[E7] B0（前提）AI 发言闸开着（IntroStage=-1）` | `Editor/BattleChatAssertE7.cs:209`（日志 287173） | **γ** | `CanAiChat()` 本会话新增了一条闸：`if (ChatPopUpBlocksLocal) return false;`（`BattleDriver.cs:7083`；`ChatPopUpBlocksLocal = _chatPopUpBlocked || _chatPopup.Visible`，`:34772`）。日志**明写 `IntroStage=-1`**（独白早演完了）⇒ 卡住的是 `ChatPopUpBlocksLocal`，不是独白那一道。而 **`_chatPopUpBlocked` 没有任何「每局复位」**（全类只有 `DisableChatPopUp/EnableChatPopUp` 两个写点，`:34790/34793`；`Begin` 的复位块 `:5095-5135` **没有它**）。原版的 `+0x78` 属**每局新建的 `VoiceLinesController`** ⇒ 语义上就是「每局默认 1（未禁）」；我们复用同一台 driver ⇒ **上一局没播完的独白留下的 `true` 会一直留到下一局** | 在 `Begin` 的「本局的账不跨局」块里补 **`EnableChatPopUp();`**（= 每局从「未禁」起）。<br>⚠️ **究竟是哪个标志我查不清**（见 §三）：若实测是 `_chatPopup.Visible` 残留，则改成在 `Begin` 里 `_chatPopup.HideImmediate()`。两个都补也无害 |
| 14 | `[E7] C1（前提）凑够 4 条敌方督军聊天（试了 24 次）` | `Editor/BattleChatAssertE7.cs:313`（日志 287202） | **γ（#13 级联）** | `AiEmoteForTest()` → `SpeakAiChat` 第一句就是 `if (!CanAiChat()) return false;`（`BattleDriver.cs:7101-7103`）⇒ 同一条闸 | 同 #13 |
| 15 | `[E7] D0（前提）排上一条自发发言…` | `Editor/BattleChatAssertE7.cs:397`（日志 287231） | **γ（#13 级联）** | `MaybeAiEmote` 里 `if (!CanAiChat()) return;`（`BattleDriver.cs:7338`） | 同 #13 |
| 16 | `[E7] D0（前提）回话也排上一条、同样不推进` | `Editor/BattleChatAssertE7.cs:400`（日志 287246） | **γ（#13 级联）** | `ScheduleAiReplyToPlayerChat` 里 `if (_aiReplyPending \|\| !CanAiChat()) return;`（`BattleDriver.cs:7299`） | 同 #13 |

---

## ② 按根因归并（16 条 → **5 个根因**）

| 根因 | 条数 | 归属 | 一句话 |
|---|---|---|---|
| **A. 拖拽块以「`targeted` 单位卡」开局 ⇒ 进等目标态 ⇒ 引擎没出牌** | **7**（#1–#7） | **δ 夹具** | 手牌序变了（见下），第一张可负担的牌从 `Primaris Intercessor` 变成 `Scout`（UM1, `targeted=1`）。夹具 + 级联 |
| **B. R18③ 阈值按旧 `Slerp` 值调，实现已按原版改 `LerpUnclamped`** | **1**（#8） | **α** | 复算 Lerp = −37.47 / Slerp = −42.77，与两侧日志逐位吻合 |
| **C. tooltip 断言拿 `Trait()` 比 `ByLink()`** | **1**（#9） | **α** | 三个特例键（melee/ranged/questpoints）走常量，结构上不可能等于 `Trait` |
| **D. `ProfileData.TitleIndex` 读口兜底把 −1 改成 0** | **2**（#10–#11） | **γ**（本会话 `B1UI`） | 「没选称号」在外部不可达 ⇒ 整块关不成立 |
| **E. `CanAiChat` 新闸 + `_chatPopUpBlocked` 无每局复位** | **4**（#13–#16） | **γ**（本会话 `A-05`） | E7 三条支全部经 `CanAiChat()` |
| （F. `ActiveAbility` 桩断言 vs 已实现） | **1**（#12） | **α**（+ 本批代理口径打架） | 实现方向与原版 `.c` 一致 |

**A 的「为什么今天才红」**：本块代码本会话**没动过**（`git diff` 的 BattleScene.cs 里 15888–16010 无改动）。手牌内容整体换了人：10-25 那一跑的同一位置是 `Hand_Primaris Intercessor` / 更早那条是 `Hand_Ember Archer`，今天是 `Hand_Scout` / `Hand_Falcon`（对照 `_tmp_view/20261025/q_battlescene20.log:118471,142283`）。**改手牌序那一笔**（最可能是 2026-10-26 `G13` 的「牌库装载倒序」修复或 10-26/27 的卡 id 重编号那批）**不在本机**（上一会话日志没留下来）⇒ 只能记成「手牌序变了」，⛔ 不指名具体提交。

---

## ③ 查不清的（逐条 + 差什么）

1. **#1–#7 的「等目标」那一跳没有直接证据**：`TryBeginPlayTargetPick` 的等目标支**一行日志都不打**，我是靠「无 `[Replay]`/无引擎拒绝/无 `落位回调找不到`/无第二次 `Release`」+ `Scout.targeted=1` + 代码路径反推的。
   - **差什么**：在该块 `Check(c2.Players[0].Board[freeSlot] != null, …)` 前加一行探针 `Debug.Log($"[DBG] awaiting={driver.AwaitingPlayTarget} targetDef={(driver.AwaitingPlayTarget?…) }")`（或直接反射 `_awaitPlayTargetDef`），一次复跑即可钉子钉死。
   - 若实测 `awaiting=False`，则改判「`DeploySequence` 的 `onDone` 3 s 内没触发」⇒ 那是**另一条根因**（β，飞行补间没跑完），优先级更高。
2. **#13–#16 的具体是哪一格**：`_chatPopUpBlocked` 还是 `_chatPopup.Visible`，日志没有打印。
   - **差什么**：在 `BattleChatAssertE7.cs:209` 的 `Ok(...)` 消息里补 `driver.ChatPopUpDisabledForTest` / `driver.ChatPopUpBlockedForTest`（两个都是现成的只读口，`BattleDriver.cs:34777/34779`），一次复跑分辨。
   - 我的倾向：**`_chatPopUpBlocked`**（本会话新增的 19·c 段在 `driver.Begin(...)` 之后只 `Step` 了几秒，独白未必演完；而 `Begin` 里 `if (!Ctx.Vars.showMulligan) StartIntroMonologue()` 这条在自检档（`mulliganEnabled=false` 但 `Vars.showMulligan=true`）**根本不叫独白** ⇒ 留下的 `true` 没人清）。弹窗那一侧：`ChatPopup` 在各段收尾都 `HideImmediate()`，且本会话新闸把「点聊天钮开窗」也挡住了 ⇒ 更不可能是它。
3. **#10/#11 的修法要「原版判据」还是「产品可用性」**：`TitleIndex` 读口兜底是 `B1UI` 按「原版已装备称号由服务器下发」做的**替代物**。它是否算「🔶 自己设计」（该打标）也请调度台裁 —— 现在那行只有 `🔴` 前缀，没有 `🔶 自己设计` 标记（`CLAUDE.md` 铁律 18）。

---

## ④ 顺手发现（不属于这 16 条，但需要记账/裁）

1. **`RE ✗ [诊断入口过期]`（日志第 262 行，不计入 16）**：反射看到 **357** 个用例方法、产物只认 **356**，缺 `TestB9EngForcedAttackAndShuffleGate`。
   - 修法（日志自己写了）：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py` —— **⛔ 别手改那个产物文件**。
   - 这条会随每加一条用例继续红，建议下一批顺手重生成一次。
2. **R18 的「数据侧」那条断言已经和行为侧脱钩**：`Editor/BattleScene.cs:1957-1968` 里**自己复算**用的还是 `Quaternion.SlerpUnclamped`（两处），而实现已是 `LerpUnclamped`。今天它靠区间 `(-48,-38)` **蒙绿**（−42.8），一旦有人真把实现改回 Slerp，行为侧（阈值 −35 之后）**不会红** ⇒ 鉴别力丢一半。建议：数据侧也改 `LerpUnclamped`，并与行为侧共用**同一份**期望值。
3. **`TutorialScript` 里四处过期 doc**（`CLAUDE.md` 铁律 5）：`TutorialScript.cs:1138/1177` 的「只有 `ActiveAbility` 走得进来」、`:659` 的「那是『这一档还没实现』（只有 `ActiveAbility`）」、`:877` 附近的同一句 —— 实现接通后都成了反向误导（下一个人会照它把 `RunActiveAbility` 删回去）。
4. **`_chatPopUpBlocked` 的生产风险**（同 #13 的根，但后果比自检严重）：真机上「再来一局 / Restart」时若上一局独白**没演完就被打断**，这一位会留 `true`，**整局聊天钮点不开**（`OnChatButtonClicked` 第一道就是它，`BattleDriver.cs:5869`）。这与 `Begin` 的「本局的账不跨局」纪律（`:5095` 那一整块注释）是同一条，建议一并补。
5. **E7 的 B 段没做 `ChatMutedSource` 的隔离**：C 段存/还原了它（`BattleChatAssertE7.cs:294/377`），B 段没有 ⇒ 一旦设置窗那条拉式接线真接上，B0 会**先**红在静音闸（顺序问题），而不是弹窗闸。
6. **`ProfileData.TitleIndex` 的读口在 getter 里写字段**：`get` 有副作用（改 `_titleIndex`）会让「读一次、状态变了」这类断言（本节就是）天然不稳；也违反 `CLAUDE.md` 那条「`SetDirty`/写口与读口分开」。建议挪到 `EnsureSave()` 的一次性兜底里。
