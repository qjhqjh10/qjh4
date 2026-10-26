# B8_BL —— `BattleScene.Run` 剩下 3 条红（定性 / 改动 / 重跑单）

> 执行代理 `B8_BL` · 2026-10-26。入口日志 = `_tmp_view/wf_battle_0826.log`（5,529 通过 / 5 失败；其中 2 条「敌方镜像」旧断言由调度台就地改成「两侧同号」，**本报告只处理剩下 3 条**）。
> ⛔ 本代理**没有跑 Unity**（会覆盖 `Battle.unity`）；只跑了 `TMPDIR=/tmp/wf_b8bl bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

## 〇、结论表

| # | 断言 | 定性 | 落点 | 本次改动 |
|---|---|---|---|---|
| 1 | `R10④ CardHoldPeek duringBattleFlag 等价物` | **(α) 断言过期**（实现已按原版改准，断言的判别式还是旧近似） | `Editor/BattleScene.cs:1897,1906,1909,1913,1912,1878` | 改断言（4 处标识符 + 2 处文案/注释），**零行数变化** |
| 2 | `翻开之后 3D 卡体 _CardImage 换回自己的立绘` | **(δ) 夹具前提不成立** —— 夹具用 `CardData.Simple(...)`（`faction = null`）⇒ `CardArt.Frame` = null ⇒ **根本不建 3D 卡体**，`Body3DMaterial` 恒 null | `Editor/BattleScene.cs:2922`（夹具）· `:2939`（顺带把静默跳过的 `if` 改硬检查） | 改夹具 + 把「静默跳过」改成硬检查，**零行数变化** |
| 3 | `面板那行「」= 原版拼法「3 可用」` | **(α) 断言过期**（它断的是 `targetsAny` 闸接线【之前】的「恒拼」行为）**＋ 一条新发现的 (β) 记录**：面板那条闸的**字段接错了层级**（详见 §3） | `Editor/BattleScene.cs:12005,12008-12010` | 断言拆成「先断闸、再显式断拼法」，**零行数变化** |

**行数**：`Editor/BattleScene.cs` 改前/改后都是 **47,974 行**（`\r\n` 47,974 / `\n` 47,974 / 裸 CR 0）—— 三处改动全部**就地改写**，**没有新增行**（账按 `文件:行号` 引用，插行必漂移）。**所以下面所有 行号 在改动前后相同。**

---

## 一、#1 `R10④` —— `CardHoldPeek` 的末环判别式

### 定性：(α) 断言过期（**不是**实现改错）

**实得值**（日志 `:114764`）：`（行序 16425 < 16437 < 16438 < -1）` —— 前三环都对，**第 4 环 `pressIdx = -1`**（找不到）。

**为什么找不到**：本会话 `B8_BB` 按原版把 `CardHoldPeek` 的触发条件改准了，原来那版近似的「按棋盘格 / 按自己手牌」被删，**`_pressSlot >= 0` 在 `CardHoldPeek` 体里已经不存在**。现读 `Battle/BattleDriver.cs:16437-16440`：

```
16437  if (Ctx == null || Ctx.IsOver || InMulligan || Ctx.Turn <= 0) return false;     ← 闸
16438  if (!PointerHeldRaw()) return false;                                            ← 按住
16440  return HitMyDeckPile(WorldPointer());              ← 末环（旧近似已删，见 :16439 注释）
```

**原版判据（我逐行亲读，不是转抄）**：`d:/2/tools/decomp_full/CardScript__OnTouchDown.c`
- `case 0`（`inDeck`，牌库里那张卡）⇒ `BattleManager__ShowDeckSize(pm, isPlayer)` —— **两侧都可能**；
- `case 1`（`inHand`）且 `+0x40 == 0`（**非我方**）⇒ `ShowDeckSize(0)`；**自己手牌按下去直接 return**（`:50-52`）；
- `case 2`（棋盘格）⇒ **只 `CardHighlight.ChangeState`，不显牌数**；
- 头一道闸 `UIstate == 0xe`（`choosingResolutionCard`）先返回；`duringBattleFlag` 那道闸在 `BattleManager__ShowDeckSize.c:22-25`。

⇒ `B8_BB` 那版（**按住 + 指针压在我方牌堆** = `case 0`）与原版一致；**断言钉住的末环是旧近似的 `_pressSlot`** ⇒ 断言过期。

### 改了什么（`Editor/BattleScene.cs`，全部就地、零新增行）

| 行 | 改动 |
|---|---|
| `:1878` | 注释就地补：末环判别式**跟着实现换**的理由（原版 `OnTouchDown.c:34-59` 的 `case 0/1/2`）+ 明确「⛔ 不是放宽」 |
| `:1897` | `int … pressIdx = -1;` → `… deckIdx = -1;`（改名，语义变成「牌堆」） |
| `:1906` | 判别串 `"_pressSlot >= 0"` → `"return HitMyDeckPile(WorldPointer());"` |
| `:1909` | `pressIdx > heldIdx` → `deckIdx > heldIdx` |
| `:1913` | 行序插值 `{pressIdx}` → `{deckIdx}` |
| `:1912` | 断言文案「按着棋盘格 / 手牌拖拽」→ `return HitMyDeckPile(WorldPointer());`（= 原版 `case 0`「按着牌库里的那张卡」；并写明 `case 1 ∧ !isPlayer` 本仓接不上、`case 2` 原版不显牌数） |

**灭自证没丢**：`🧨 删掉 Ctx.Turn <= 0 或 Ctx.IsOver 那一项 ⇒ 红` 原样保留；次序判断 `forced → gate → held → deck` 四环仍在（实得 16425 < 16437 < 16438 < 16440）。

---

## 二、#2 伏击翻开后 3D 卡体立绘 —— **夹具根本没有 3D 卡体**

### 定性：(δ) 夹具前提不成立（同时暴露一条「静默跳过」）

**证据链（四条独立，互证）**：
1. 夹具 `Editor/BattleScene.cs:2922`（改前）：`var d = CardData.Simple("AmbushProbe", 3, 3, 4); d.artId = "UM78";`
   `CardData.Simple` 的初始化器里 **`faction = null`**（`Core/CardView.cs:109-117`）。
2. `Core/CardArt.cs:73` `Frame(...)` 第一句就是 **`if (string.IsNullOrEmpty(faction)) return null;`** ⇒ `frameTex == null`。
3. `Core/CardView.cs:4263` 是 `if (frameTex != null) { … }`，而**建 3D 卡体的那句 `if (board) _body3D = BuildBody3D(artTex);` 在 `:4361`（这个 `if` 的里面）** ⇒ **`_body3D` 恒 null** ⇒ `Body3DMaterial`（`:7270`）恒 null。
4. 日志里 **`★ 3D 卡体 _CardImage = 伏击图` 这条根本没有打印**（`grep "3D 卡体"` 只有那条红的）—— 因为它在改前是 `if (bm != null && bm.HasProperty(...)) Check(...)`（`:2939`），**`bm == null` 就静默跳过**。⇒ 这一族**整整一季一次都没跑过**；`B8_AA` 新加的那条（`:2958`）是**第一条不带 `if` 的**，所以它一上来就红了。
   > 旁证（另一个会话早就写下同一诊断）：`BattleScene.cs:152285` 的断言文案里写着「🧨 把夹具阵营改回 `"Test"`（`CardArt.Frame` 回 null）⇒ 本行红」—— 同一个机理。

**不是 `SetCardImageTo3DBase` 的闸被绕开**：`Core/CardView.cs:7086` 那道闸是 `if (_faceDown) { var amb = FaceDownArtTexture(); if (amb != null) tex = amb; }`，`SetAmbush(false, false)` 会把 `_faceDown` 置 false 再调它（`:7200`）⇒ 闸本身没问题，**是压根没有 3D 卡体可换**。

### 改了什么（`Editor/BattleScene.cs`，零新增行）

| 行 | 改动 |
|---|---|
| `:2922` | 追加 `d.faction = "Ultramarines";`（`UM78` = `Heavy Intercessor`/Ultramarines，见 `Editor/CardFaceProbe.cs:1810`；`Resources/Art/cards/frame_ultramarines.png` 在）⇒ `frameTex != null` ⇒ 走真卡框支路 ⇒ **建出 3D 卡体**。就地加注释写明为什么必须给真阵营 |
| `:2939-2940` | `if (bm != null && bm.HasProperty(...)) Check(...)` → **`Check(bm != null && bm.HasProperty(...) && …)`**（硬检查）—— 不许再「没有 3D 卡体就静默跳过」。这条从此**必跑**（断言总数 +1） |

**⚠️ 副作用面（如实说）**：给 `faction` 之后，这一块**整张卡**改走「真卡框支路」（原来的占位卡面支路不再走）⇒ 同一块里那几条**原本就绿**的断言（`面朝下 + 数值被藏` / 四个数值层关着 / `?` 印出来 / `isPlayer != 0` 照旧画数值 / 翻开 `?` 收掉）会第一次在**有框**那一路下评估。我读代码确认它们**不依赖 `frameTex`**：`BuildStatLayers` 有无框都建（`Core/CardView.cs:13077`，且首句 `if (_stats[1] != null) return;` 幂等）、`StatShown`（`:13476`）/`SetStatsHidden`（`:14251`）/`AmbushMarkText`（`:14362`）都只读 `_stats`/`_ambushMark`。**但这只是静态推断** —— 重跑若在这一块冒出**新的**红，第一嫌疑就是这条（不是 #2 那两条）。

**为什么给阵营、而不是删断言**：这两条断的是原版 `BattleCardUI__SetCardImageTo3DBase.c` 那道闸（面朝下 ⇒ 伏击图；翻回来 ⇒ 自己的立绘），是**真判据**，只是夹具没造出让它能被观测的前提。给阵营之后 `art_um78.png` 在（`Resources/Art/cards/art_um78.png`），翻开时 `_CardImage` = 它 ≠ `GenestealerCults_special_Ambush` ⇒ 断言可满足。

---

## 三、#3 技能面板那行空串

### 定性：**(α) 断言过期**（断言 = 闸接线【之前】的「恒拼」行为）**＋ 一条必须记录的 (β)**：接线用的**字段接错了层级**

**实得值**（日志 `:137448`）：`面板那行「」` —— 空串。⇒ 面板走了「整段不拼」那一支。

**为什么走那一支（逐环读通）**：
1. `Battle/BattleDriver.cs:15070`（本会话 `B8_X` 接的）：
   `skillPanel.Show(…, !CardFaceRules.ShouldShowTargetsAvailable(u.Card, false))`，
   而 `SkillPanel.SetTargetsSuffix` 的 `targetsAny` **语义是反的**（`true` = 不拼）⇒ 极性**对**。
2. `Core/CardFaceRules.cs:103`：`ShouldShowTargetsAvailable(card,false)` = `inPlay == ShowTargetsAvailableInPlay`（`false == false` ⇒ 通过）`&& HasTargetsAvailableCounter(card)`。
3. 用例那张卡是 **`StarterCards.Ember()` 的 `Ironclad`**（`Editor/BattleScene.cs:11933`）—— 🔶 **我们自己设计的 26 张测试卡**（`RuleEngine/Data/StarterCards.cs:64`），`CardDef` 构造**不喂 `Conditions`** ⇒ `HasTargetsAvailableCounter` = **false**。
4. ⇒ `targetsAny = true` ⇒ `_targetsSuffix = ""` ⇒ `sp.TargetsText == ""`。**链路自洽，没有 bug。**

**原版判据（亲读）**：`cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:157-196`（`SetupActiveSkill`）

```
if (activeAbility.targetsAvailableCounter != null && targetsAvailableCounter.targetsAffected != TargetsAffected.any)
    text7 = desc + " " + targetAvailableText;          // :188
else
    text7 = parentCard.GetCurrentLocalizedAbilityDescription();   // :193 —— 描述原样
```

⇒ 对「`targetsAvailableCounter` 为 null」的卡，原版**就是 else 支 = 整段不拼**。`Ironclad` 是自设计卡、这张表**没有任何 counter 数据** ⇒ 等价于 null ⇒ 原版这一支也不拼。
**⇒ 结论：原版对该卡「不拼」，我们的输出正确；断言写的 `3 可用` 是错的期望。**

### 🔴 但顺手查出一条真的缺口（必须记账，⛔ 不是「查不到」）

原版面板那道闸读的是 **`ActiveAbility.targetsAvailableCounter`**（`d:/2/tools/il2cpp_out/dump.cs:23764` = `ActiveAbility +0x40`），
而 `B8_X` 接的 `ShouldShowTargetsAvailable` 读的是 **卡级 `RawCardScript.targetsAvailableCounter`（`+0x298`）** ——
**两个不同字段、两个不同消费者**：
- `+0x298` 的消费者 = `BattleCardUI.ShowAvailableTargets`（**手牌**抬起时那行「N 个可用」，账 `账/战斗界面_HUD.tsv:148`，状态 C）；
- `+0x40` 的消费者 = 本处（**技能面板**）。

而且 **`+0x40` 整列在我们的卡表里根本不存在**：`Unity/工具/gen_cards_engine.py` 的 `scan_ability`（`:2862-2878`）只 emit `targetCriteria(+0x38) → activeAbilityPickCriteria`、`actingCardCriteria`、`multiCountCriteria`、`boardCriteria.countCriteria` 与 logic 级那几格，**没有 `targetsAvailableCounter(+0x40)`**。

**影响面（现读数据，`Resources/cards_engine.json`，1,128 张）**：
- 卡级 `avail` 行且 `targetsAffected != 0` 的卡 = **17 张** ⇒ 面板**会**拼后缀；
- 其余 **1,111 张** ⇒ 面板**不拼**（`B8_X` 接线前是**全拼**）；
- 作为「真闸大概多大」的近似上限：`activeAbilityTargets` 里 `criteria.targetsAffected ∉ {0,-1}` 的卡 = **122 张**（⚠️ 这是 logic 级、**不是** `+0x40` 的真值，只能当量级参考）。

⇒ **记录**：`+0x40` 未落表 = 铁律 17 那类「已知未覆盖面」的一格；在它进表之前，面板这条口径是 **🔶 近似（接的是卡级同形字段）**，应在 `Core/CardFaceRules.cs` 的 doc 里标出来（**该文件在本代理白名单内，但改动点在 `BattleDriver` 的调用处，故本次只记录不改**，见 §五 改动请求）。

### 改了什么（`Editor/BattleScene.cs`，零新增行）

| 行 | 改动 |
|---|---|
| `:12005` | 注释就地补：**先断闸、再断拼法**的理由（`Ironclad` 是 🔶 自设计卡、无 counter 数据 ⇒ 原版 else 支） |
| `:12008` | 原「面板那行 = 3 可用」→ **闸断言**：`Check(!CardFaceRules.HasTargetsAvailableCounter(ctx.Players[0].Board[casterSlot].Card) && sp.TargetsText == "", …)`（`🧨 删掉 targetsAny 那一支（退回恒拼）⇒ 红`） |
| `:12009` | 新增一句 **`sp.SetTargetsAvailable(wantT);`**（显式喂「该拼」那一档 —— 状态与原来 `finally` 里那次完全一样，⛔ 不改下游语义） |
| `:12010` | 拼法断言**原样保留**：`Check(sp.TargetsText == (zhT ? "3 可用" : "3 available"), …)`（判据 `Replace("{0}", n)` + 词条） |

**为什么这不是「放宽断言」**：
- 覆盖**变多不变少** —— 「拼法」那条一个字没改，只是从「面板默认态」改成「显式喂一次后」；面板默认态改由**新加的那条闸断言**钉（原来没有这条）。
- 两条合起来仍是「一开一关」的灭自证；下游 G16 那组（`:12033-12065`）读的 `sp.TargetsText` 在这一点上仍是 `3 可用`/`3 available`，**一行没动**。
- 期望值仍用**字面量**（⛔ 没改成拿 `CardText.TargetsAvailable` 当期望 —— 那是自证）。

---

## 四、重跑单（只重跑一个宿主）

```
bash d:/4/Unity/工具/_run_8_checks.sh            # ⛔ 不要全量
```
**只跑 `BattleScene.Run`**（改动只在这一个宿主里：`Editor/BattleScene.cs`；`BattleDriver/CardView/CardFaceRules/SkillPanel` **一行没动**）：

```
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run -logFile -
```

**期望**：
- **失败 0 条**（原来 5 条：2 条镜像断言由调度台已改 + 本报告这 3 条）。
- **断言合计 = 5,536**（原 5,534 + 2）—— 因为 `#2` 把一条「`if` 里静默跳过」改成**必跑**（+1），`#3` 把一条拆成两条（+1）。
- ⚠️ 判绿红看「**断言合计**」、按**行首标记**数 `✗`（⛔ 别 `grep -c ✗`，`✗` 会出现在文案里）。
- ⚠️ `BattleScene.Run` 跑前照例留 `Battle.unity` 副本，跑完 `git diff --numstat` 瞄一眼（宿主会重写场景）。

---

## 五、改动请求（本代理白名单外，⛔ 没动）

1. **`Battle/BattleDriver.cs:15070`** —— 面板那条闸接的是**卡级** `RawCardScript.targetsAvailableCounter(+0x298)`，原版面板读的是 **`ActiveAbility.targetsAvailableCounter`（`+0x40`）**。
   **建议**：等 `+0x40` 落表后改成
   `targetsAny: !CardFaceRules.ShouldShowTargetsAvailableOnPanel(u.Card)`（新判据放 `Core/CardFaceRules.cs`，读 `+0x40` 那一行），
   ⛔ 别继续借 `CardScript.ShouldShowTargetsAvailable`（它的消费者是 `BattleCardUI.ShowAvailableTargets`）。
2. **`Unity/工具/gen_cards_engine.py`（`scan_ability`，`:2862-2878`）** —— 补 emit `ActiveAbility.targetsAvailableCounter`（`+0x40`，`ab = -1/-2`，对象级、**没有 `lg`**，形状同 `activeAbilityPickCriteria` 那一列）。这是本条的**上游**，做完 1 才有意义。
3. **`Unity/资料/完全复刻/账/战斗界面_HUD.tsv:20`**（`ActiveSkillDisplay / SetupActiveSkill`，状态 **C**）—— 「差异」列现在**过时**：原写「唯一生产调用方 `BattleDriver.ShowSkillPanel` 未传」；`B8_X` **已经传了**（`:15070`）。建议把那一句替换成：
   > 「③ 生产调用方 `B8_X` 已接（`BattleDriver.cs:15070`）—— ⚠️ 但接的是**卡级** `+0x298`，原版面板读的是 **`ActiveAbility.targetsAvailableCounter(+0x40)`**（`dump.cs:23764`）；`+0x40` **整列未落表**（`gen_cards_engine.py` 的 `scan_ability` 不 emit）⇒ 面板那条闸是 🔶 近似（现读池内 1,128 张里卡级 `avail≠0` = **17 张**会拼、其余不拼）。」

   ⛔ **本代理没有写 `账/_parts/B8_BL.tsv`** —— 这条的**状态没变**（仍 C），按调度台「只在真的改了某条账的状态时才写」的口径，改为把替换文本交给你合并（这一行 `B8_U` 刚改过，补丁直接落盘有撞键风险）。

---

## 六、没查清的部分（如实列出，⛔ 没拿猜测填空）

1. **#2 我没能运行期验证**（⛔ 本代理不许跑 Unity）—— 给阵营后 `_body3D` 一定非 null 是**读代码推出来的**（`CardView.cs:4263/4361`），重跑才知道。**若重跑仍红**，下一步按此顺序查：① `foe.Body3DMaterial` 是否被换成**溶解材质**（`CardView.SetCardMaterial`）② `ArtTexture(d)` 是否返回 `null`（`CardArt.Portrait("UM78")` 命中 `art_um78.png`，已核文件在）③ `SetAmbush(false,…)` 是否真的跑到（`_faceDown` 的其它写点）。
2. **#1 运行期验不了**（批处理无真指针，`PointerHeldRaw()` 恒 false；自检钉死口 `_cardHoldForTest` **在闸之前短路**，设计如此）⇒ 只能源级钉。原版 `case 0` 那支还有两个**结构上够不到**的档：**敌方牌库那张卡**（本仓只有 `HitMyDeckPile`）与**敌方手牌**（`CardInteraction` 只接管我方手牌）—— 这两条是 `CardHoldPeek` doc 里既有的「如实记」，本次没改变现状。
3. **#3 的 `+0x40` 真值拿不到**（不是「查不到方法体」—— 字段偏移与消费者都读到了；**是这一列从没被解包进卡表**）。⇒ 面板对**真实卡池**的正确行为今天**无法验证**；本报告给的 122 张只是 logic 级近似。
4. `Ironclad` 是 🔶 自设计卡，原版**没有它的对应物** ⇒ 「原版会怎么显示 Ironclad 的技能面板」这件事**没有原版判据**（只有「算法怎么处理 null 字段」这一层判据）。
