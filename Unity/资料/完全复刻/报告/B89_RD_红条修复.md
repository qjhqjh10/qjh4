# B89 收尾 · 红条修复（执行代理 `RD` 转岗执行）

> 输入 = `报告/B89_RD_红条诊断.md`（34 条红 = 11 个根因）。本文件 = **逐根因的修复记录 + 自检读数**。
> 纪律：`账/*.tsv` **主表一个字节没动** · `git` 没动 · `d:/2` 没动 · Unity **全程串行**（一次一个宿主）。

---

## §0 结果总表（改前 → 改后）

| 宿主 | 改前 | 改后 | 日志 |
|---|---|---|---|
| `RuleEngineTest.Run` | 9490 / **18 失败** | **9508 / 9508 全过 ✅** | `_tmp_view/B89RD_ruleengine2.log` |
| `ShopScene.Run` | 2502 / **8 失败** | **2510 / 0 ✅** | `_tmp_view/B89RD_shop.log` |
| `BattleScene.Run` | 5662 / **2 失败** | **5664 / 0 ✅** | `_tmp_view/B89RD_battle.log` |
| `MainMenuScene.Run` | （旧日志**无合计行** —— 见 §4·1）**2 失败** | **5089 / 0 ✅** | `_tmp_view/B89RD_menu2.log` |
| `AnimFXCheck.Run` | 240 / **1 失败** | **241 / 0 ✅** | `_tmp_view/B89RD_animfx.log` |
| `LoadableRefCheck.Run` | 35 / **1 失败** | **36 / 0 ✅** | `_tmp_view/B89RD_loadref.log` |
| `EnchantmentTemplateCheck.Run` | 66 / **1 失败** | **68 / 0 ✅**（+2 = E6 拆成一对断言） | `_tmp_view/B89RD_enchant.log` |
| `typecheck.sh` | 0/0 | **0/0**（改完共跑 3 遍） | —— |
| `check_ledger_points.py` | —— | **失败 0 · 漂移 41（全是存量，⛔ 没有一条落在我改过的文件上）** | —— |
| 场景文件 | —— | **6 份 `.unity` 逐字节未变**（跑前快照 → 跑后 `cmp` 全 same） | `_tmp_view/scene_bak_B89RD/` |

---

## §1 逐根因（11 条 + 1 条越界红）

### 1. `β×8 商店` — `ItemDrawer.Spec` 用入参判 `Kind`（生产也中招）

- **改了什么**：`Shell/ItemDrawer.cs:827` `!string.IsNullOrEmpty(art)` → **`!string.IsNullOrEmpty(s.Art)`**（同一行只换内容）。
  理由写在该行注释里：上一行 `Art = art ?? CurrencyNames.Icon(targetId)` 已经把货币 code 的主图解出来了，
  而 `Kind` 决定 `PickDrawer` 走 `Icon`（进而转交 `DrawCurrency`）还是 `DrawerPlaceholder`。
- **判据**：`ItemDrawer.cs` 自身的四步（`Draw` 的 ④ ⇒ `Dispatch(DrawerIcon…)` ⇒ `Icon` 里 `:1297` 的货币转交）。
- **转绿**：`ShopScene.Run` 那 8 条（主图 / 名条底 / 小图标 / 数量 / 名条行矩形 / 小图标 30×30 两条 / 不溢出）。
- **顺手影响**：战役奖励窗 / 战役页 / 奖励窗里 `C2` 的格子（`CampaignRewardWindow.cs:934` · `CampaignTab.cs:683/720` · `RewardWindow.cs:1406`）
  本来画的是占位板，现在走真正的 `CurrencyDrawer`（有人工复核价值，见 §4·3）。

### 2. `γ×2 残骸落点` — `pos.y = 0f` 的判据是错的

- **改了什么**：`Core/CardFeel.cs:2921` / `:2927` **删掉两处 `pos.y = 0f;`**（各 1→1 行，文件 4188 行不变）。
- **判据**：`decomp_full/RemnantNecrons__GetDeathParticlePosition.c` / `RemnantAeldari__…c` 都是
  `Transform.get_position` 之后 `*param_1 = *puVar2; *(undefined4 *)(param_1+1) = uVar1;` = **12 字节（x/y/z）整份拷回**；
  cpp2il `RemnantBody.DoDestroyByAttack` 把整个 `Vector3` 交给 `Instantiate`。**「只填 x/z」不成立**。
- **转绿**：`BattleScene.Run`「落点 = `Idle/Spirit Stone Idle` 的世界坐标（差 0.3836）」（**1 处**）·
  `AnimFXCheck.Run`「残骸粒子的落点 = 残骸体根（偏差 8.34E-001）」（**2 处**）。
- **铁律 5 就地订正**：`报告/B89_R4b_HUD与特效执行.md` §1·802 四行已整段改写成「原写……不成立 + 该行已删 + R4b 那条 `y == 0` 观测口作废」。

### 3. `γ×2 主菜单滚动` — 滚动动画中途被整条重建打断

- **改了什么**：`Shell/TopBar.cs:604` 加 `justFinished` · `:609` 在 `StepCounter` 之后判「本帧是否有件刚滚完」·
  `:612` `if (moved)` → **`if (justFinished)`**（三处都是同一行只换内容，文件行数不变）。
  这正是 `TopBar.cs:1031` 那句注释本来就写死的时机：「只放在**一段滚动刚刚结束**那一拍」。
- **夹具**：`Editor/MainMenuScene.cs:1129-1138` 把滚动目标从写死的 `30` 改成**钱包真值 `old`**（7→7 行）。
  理由：`FixCounterLayoutIfNeeded` 在**真重建**时会按钱包重画文本（`TopBar.cs:1082-1086`），
  夹具的 `Target` 若 ≠ 钱包，重建后必然对不上 —— 那是夹具与生产语义不自洽，不是实现错。
  改成 `Target = old` 之后：滚到终值时文本 == 建条时那一串 ⇒ 量宽无变化 ⇒ **不重建**、`cr` 不失效。
- **转绿**：`MainMenuScene.Run`「0.5 s 到点 ⇒ 终值」+「到点自己停」。

### 4. `δ×12 Mob/Regiment 夹具` — 载体挂在攻击者身上

- **改了什么**（全部只改夹具，`RuleEngineTest.cs` 文件 39128 行**不变**）：
  | 断言 | 落点 | 改法 |
  |---|---|---|
  | GOF68 再触发一次 | `:17338-17339` | `mobber` = 旁观者（2 号格）+ 监听者 3 号格 + **不带词的攻击者 1 号格** |
  | ① 近战 → Mob | `:20670` | 载体 2 + 攻击者 3（`Unit("FixtureMobAtk1",1,2,9)`） |
  | ② 远程 → Mob 不触发 | `:20684` | 同上，攻击者用 **`Ranged(...,1,2,9,2)`**（`Unit` 的 ranged=0 ⇒ `ErrCannotRanged`） |
  | ③-a 远程 → Regiment | `:20703` | 同上（攻击者 `Ranged("FixtureRegAtk1",1,2,9,2)`） |
  | ③-b 近战 → Regiment 不触发 | `:20718` | 载体 2 + 攻击者 3 |
  | ①-bis / ④-a / ⑤ | `:34919` / `:20825` / `:35035` | 载体改 `exhausted:true` + 另加旁观者位置上的攻击者 |
  | ④ SelfOnly | `:34981` | 载体 3 + 攻击者 2（S2 连续性 3,2 ✓） |
  | ④-b 敌方那只 | `:20863` | 敌方旁观者 2 + 敌方攻击者 3 ⇒ 敌方那只**照样 2→3**、我方监听器不动（极性判别式保住） |
  | 「已行动过」两条判别式 | `:13147` / `:16454` | 补 `AttacksThisTurn = RuleCore.ActionQuota(...)`（「已行动」的真身是 `RuleCore.cs:3523` 的 `actedOut`；三个 `Exhausted = true` 写点都同时 `AttacksThisTurn++`） |
- **判据**：`decomp_full/CardScript__ResolveUnitAttacked.c:126-146`（接收者 `!=` 攻击者且同 `isPlayer` 才跑 300/301；
  攻击者本人走 `:31-112` 那一支 `return`）。
- **⚠️ 顺手修掉两处「空转绿」**：② 与 ③-b（「不该触发」那两半）原本**必然绿** —— 攻击者本来就不跑自己的 Mob。
  现在它们真的能分辨「`!ranged` / `ranged` 写反」了。
- **转绿**：那 12 条（含 9 条 Mob/Regiment + 2 条已行动判别式 + 1 条「我方监听器不动」）。

### 5. `α×3 GetChosenCard 退回档`

- `RuleEngineTest.cs:30187/30193/30203` 三条改成新口径：`ourId` 空 / 场上没有 / 手牌没有 ⇒ **退回列表第 0 个**（并断 `side/slot` 落在 3 号格），
  判据写进断言 = `decomp_full/AiScripted__GetChosenCard.c:36-40` 的 `iVar4 = 0;`，并注明这是 **2026-10-28 调度台裁定「照抄原版」**。

### 6. `α×2 T26 泵点` + `α×1 卡表 version`

- `RuleEngineTest_Priority.cs:324` 3→**4** · `:327` 22→**23**（出处补 `EffectResolver.cs:18139`，并订正旧行号 `:2457/:9757/:12183` → `:2519/:9870/:12324`）。
- `RuleEngineTest_EngineD4.cs:101-102` 36→**37**（归因补「第八十九会话 · `E6` = 三族数据侧列（账 L234/L867/L1044/L847）」）。

### 7. `α×1 A462 白名单行文`

- `Editor/BattleScene.cs:34279` 白名单文本补 `!_replaySession && `（实现本批加的闸，`BattleDriver.cs:8916`；扫到 7/7 本来就没多）。

### 8. `α×1 里程碑格间距`

- `Editor/RewardsScene.cs:1642` 期望 69 → **81.94**，判据写进断言 = `decomp_full/EverguildLayoutGroup__CalculateSpacing.c:98-100`
  （`evenlySpaceinBetween` ⇒ 运行期 `set_spacing((325 − 5×40)/4 = 31.25)`，序列化的 `m_Spacing 20` 是被覆盖的旧值）。

### 9. `δ×1 LoadableRef 夹具`

- `Editor/LoadableRefCheck.cs:120` `new LoadableRef<Item>("stale")` → **`new LoadableRef<Item>(new Item("stale"), IdOf)`**
  （④ 的语义靠「取 id 的口」，string ctor 没有它 ⇒ 回填整段不执行）。

### 10. `α×1 诊断入口过期`

- 重生成：`PYTHONIOENCODING=utf-8 python -I -X utf8 工具/gen_diag_runner.py`
  → 写 360 条；`--check` 复跑 = **逐字节相同（48298 字节）**。八份日志里的 `[诊断入口过期]` 全消失。

### 11. `α×1 E6「模板不许进池」`（调度台裁定：改断言、不改实现）

- `Editor/EnchantmentTemplateCheck.cs` 的 E6 从「模板 id 一个都不许出现在池里（期望 0）」改成**一对不变量断言**：
  ① **超出既有重叠的有 0 张**（= 模板表没往池里塞新东西）；② **既有重叠正好 6 张**（挡「少掉」那半边）。
  6 条 id 逐条列出并注明身份（`Dark Pact of *` ×4 含 `cost=-1` 哨兵 · `Pray Enchantment`（v30 决策加进池）· `Reconnaissance Mission`（可玩的附魔卡））。
  注释里写清**为什么换**：那 6 个 id 在**模板表出现之前**就在池里（HEAD 版 `cards_engine.json` 现读同样 6 个全在）⇒ 那是**池的历史内容**，不是本表的副作用。
- 本文件的 3 条账落点（`:141/:171/:217`）都在 E6 之前 ⇒ 加行不影响落点。

### 12.（**越界红**，不在原 11 条里）`MainMenuScene` 的 `[E13] D3 飘窗节点名`

- **它是怎么冒出来的**：诊断时给我的 `menu.log` 只有 **201 KB / 2607 行**（这次全量跑是 17.9 MB），
  `[E13]` 段在里面**一条都没有** ⇒ 那一轮 MainMenuScene **跑漏了 E13 段**，D3 这条红**从来没在读数里出现过**。
  全量跑之后它是 **第 1 条也是唯一 1 条**红。
- **根因**：`Editor/ProfileDataAssertE13.cs:354` 断 `live.name == "Toast"`，而 `ToastNotifications.ShowInternal`
  是 `Instantiate(_templateByType[ti], _holder, false)`（`ToastNotifications.cs:852`）⇒ **克隆体名带 `(Clone)`**。
  本仓既有旁证：`MainMenuRuntime.cs:3394`「运行时是 `Instantiate` 出来的克隆（名字带 `(Clone)`）」·
  `ErrorMessageBanner.cs:70/:547` 直接把节点建成 `Error Mensage_Ref(Clone)`。
- **改了什么**：断言改成 `live.name == "Toast" || live.name == "Toast(Clone)"`（仍钉「⛔ 别改名」的意图），注释写清判据。
- **转绿**：`MainMenuScene.Run` 5089/0（这一条是最后一块拼图）。

---

## §2 改动文件清单（全部零行数变化，除标注）

| 文件 | 行数 | 说明 |
|---|---|---|
| `CardPresentation/Shell/ItemDrawer.cs` | 2152 → 2152 | 1 行换内容（`:827`） |
| `CardPresentation/Core/CardFeel.cs` | 4188 → 4188 | 2 行换内容 |
| `CardPresentation/Shell/TopBar.cs` | +0（本笔） | 3 行换内容 |
| `CardPresentation/Editor/MainMenuScene.cs` | +0（本笔） | 7 行块内换内容 |
| `CardPresentation/Editor/BattleScene.cs` | 49217 → 49217 | 1 行换内容（白名单文本） |
| `CardPresentation/Editor/RewardsScene.cs` | 12864 → 12864 | 1 行换内容 |
| `CardPresentation/Editor/LoadableRefCheck.cs` | +0 | 1 行换内容 |
| `CardPresentation/Editor/ProfileDataAssertE13.cs` | +0 | 1 行换内容 |
| `RuleEngine/Editor/RuleEngineTest.cs` | **39128 → 39128** | 12 处夹具订正（全部原地替换） |
| `RuleEngine/Editor/RuleEngineTest_Priority.cs` | +0 | 2 行换内容 |
| `RuleEngine/Editor/RuleEngineTest_EngineD4.cs` | +0 | 2 行换内容 |
| `RuleEngine/Editor/EnchantmentTemplateCheck.cs` | 只动 E6 段（**账落点 `:141/:171/:217` 都在它之前** ⇒ 不受影响） | 一条断言 → 一对不变量断言 |
| `RuleEngine/Editor/RuleEngineTest_Diag.cs` | **整份重生成**（工具产物） | `gen_diag_runner.py`，`--check` 逐字节相同 |
| `资料/完全复刻/报告/B89_R4b_HUD与特效执行.md` | 表格 4 行 | 铁律 5 就地订正（§1·802） |

`check_ledger_points.py` 现读：**A 4790 · 通过 4749（告警 231）· 漂移 41 · 失败 0** —— 41 条漂移**没有一条**落在我改过的文件上
（脚本输出里 grep 我改过的 9 个文件 = 0 命中）。

---

## §3 账：**没有写补丁**，以及为什么

按 `CLAUDE.md` 铁律「同键补丁会**静默盖掉**主表既有内容」，我没有写 `账/_parts/B89_RD.tsv`。
本轮**没有新增待办账**（11 个根因全部当场做掉），只有两条**该并进既有行的备注**，请调度台按自己的节奏合并：

| 建议落点（既有行） | 要补的话 |
|---|---|
| `账/未分类_其他.tsv:708` `UIResourceCounter.SetQuantity(int newQuantity, bool animated)`（现状态 `C`） | 补一句：**药丸宽重排的落地形状 = 整条资源条重建**（`TopBar.cs:1082-1086`：`DestroyImmediate` + `BuildResourcesBar`）。代价如实记：① **每重建一次就换掉 `Parts.Counters` 的实例**（外部持有的 `CounterRef` 会失效）；② 重建那一刻会**换掉全部 Label**（真机上是「整条资源条闪一下」）；③ 因此**只在「一段滚动刚刚结束」那一拍调**（`TopBar.cs:612`，本笔已按此收窄）。**「原地重排」是更接近原版 CSF 的写法，但药丸是九宫格、没有 resize 口，要动 `MenuDraw.Nine`** ⇒ 留 `C`，写清性能/闪烁风险 |
| `账/其他_基础设施.tsv:299` `LoadableReference.SetReferenceByObject(T reference)`（现状态 `A`） | 补一句：我们的 `_idOf` 是 `readonly` 且**不序列化**（`Core/LoadableRef.cs:54-70`）⇒ **任何经 `JsonUtility` 读回来的实例都没有它**，于是「`targetId = obj.GetID()`」那一支**静默不发生**（原版靠 `T : IIdentifiable` 恒成立）。今天 0 可达（读档链没接 `LoadableRef`）⇒ 建议保持 `A` + 备注，或单开一条 `C` |

---

## §4 需要调度台知道的四件事

1. **给我的 `menu.log` 是残的**（201 KB / 2607 行；全量 17.9 MB / 22 万行），`[E13]` 段整段不在里面 ⇒
   「menu 只有 2 条红」这个读数**不完整**。建议以后收日志时核一下体积量级（`Battle` ≈ 22 MB、`Menu` ≈ 18 MB）。
2. **主菜单那两条夹具的期望值语义变了**：现在滚动的 `Target` = **钱包真值**，不是写死的 30。
   如果以后有人把 `FixCounterLayoutIfNeeded` 挪回「每帧都调」，这两条会**立刻红**（正是想要的守卫）。
3. **商店那条 β 有画面影响**：战役奖励窗里 `C2`（水晶）现在画的是**真正的货币抽屉**（主图 + 名条 + 小图标 + 数量），
   之前是占位板 —— 建议真机顺手看一眼（`Shell → Campaign → 任一给水晶的奖励`）。
4. **场景零变化**：6 份 `.unity` 跑前跑后逐字节相同（`_tmp_view/scene_bak_B89RD/` 留了快照，可直接删）。
