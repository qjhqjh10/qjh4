# B9_W24ASSERT —— K-04「长按 / 轻点 / 按住弹卡面」五条口径的**断言组**

> 执行代理：`B9_W24ASSERT` · 2026-10-27 · **未派子代理**（本件全部自己做完）
> 白名单：`Assets/CardPresentation/Editor/BattleScene.cs`（本波**只动了这一个代码文件**）
> 账补丁：`账/_parts/B9_W24ASSERT.tsv`（5 行）· 报告：本文件
> 红线：**未跑任何 Unity** · **未动 git** · **未改正本/主表/别人的文件**

---

## 一、结论

1. K-04 五条此前在 `Editor/BattleScene.cs` 里 **0 条断言**（上一路 `B9_W3ANIM` §三·4 亲证）。本波补 **17 条 `Check`**
   （**14 条功能断言 + 3 条前提硬检查**），**一条都没跑过**（本波禁 Unity）⇒ 由调度台在同步点跑 `BattleScene.Run` 验。
2. **行号零漂移**：走 `B9_W11SHAKE` 那套「委托外传 + 文件尾类」—— 调用点 **在 `:3596` 那一行内追加**（1:1 替换），
   断言体整块挂在**文件尾**（`static class K4HoldAssert`，`:48637-48881`）⇒ 文件从 48610 行变 48882 行，
   **但新增的 272 行全在原最后一行之后** ⇒ `1…48610` 的既有行号**一个没动**（账里几十条指本文件的 `文件:行号` 不受影响）。
3. 覆盖到的五条口径：**682** 按下记时刻+置标志 · **683** 离开清标志 · **684** 轻点填面板/试放（含 `isPlayer` 闸与
   「真/假两条支」）· **686** 按住 > 0.3 s 弹卡面（**严格大于**）+ 状态迁移 · **687** PressedBtn 0.4 s 熄灭 +
   「钩子没接**出声**、不假装」。
4. **6 条灭自证**（结构上不可能同时满足的对偶），另有 1 条边界双档（0.3 正好不弹 / 0.3001 弹）。
5. ⚠️ **687 本组只钉「状态机 + 0.4 s 时序在」与「未接钩子会出声」**，**不钉建层** —— `PressedBtn` 的节点名/rect/sprite
   本地仍拿不到（沿用 `B9_W3ANIM` §三·1 的四路搜尽结论），⛔ 没有为它编判据。
6. ⚠️ **棋盘 inPlay 档不量时长**这条断言是**照判据写的**（`CardScript__OnTouchUpAsButton.c:63-88` 全文没有时间量），
   ⛔ 没有给棋盘加 0.2 s 门，也没有写「两者应相等」那种错断言。

### 1.1 断言清单（17 条 · 逐条「断什么 / 改坏哪处会红 / 灭自证?」）

| # | 行 | 断什么 | 改坏哪处会红 | 灭自证 |
|---|---|---|---|---|
| ① | `:48647` | **前提·三条常量 = 原版字面量**：`cardShowTime` 0.3（`__ctor` 的 `0x3e99999a`）· `PressedBtnHideDelay` 0.4（`DAT_1834b3158`）· `LongPressNoDisplaySeconds` 0.2（`DAT_1834b2bb0`） | 改任何一个常量，或把三条并成一条 | — （硬前提） |
| ② | `:48666` | **686/682：0.3 s 门是严格大于** —— 按下置 flag；elapsed **正好 0.3 不弹**、**0.3001 弹**；`ForceDisplayCard` 恰好 1 次 | 写成 `>=` ⇒ 正好 0.3 就弹 ⇒ 第二格真；写成 `> 0.5` 之类粗门 ⇒ 第三格假 | 边界双档（`>=` / 粗门 二者必红其一） |
| ③ | `:48682` | **683：`OnTouchExit` 真清 flag** —— 松手后等到 100 s 也不弹（同一台 `Tick`、同样「远大于 0.3」，只差一次 exit） | `OnTouchExit` 写成空方法 / `Tick` 不看 flag ⇒ 这里弹 | ✅ **灭自证**（「一松手还弹」与「一次都不弹」不可能同时满足 —— 后者会让 ② 红） |
| ④a | `:48693` | **686 命中后对 `+0x38` 写两字节 `0x100`**：`inPlayClickFlag` 清 0、`displayingCardFlag` 置 1、`holdDisplayed` 置 1，且**不重复弹** | 漏 `_inPlayClickFlag = false`（每帧都弹，计数变 3）/ 漏 `_displayingCardFlag = true` / 漏 `_holdDisplayedThisPress = true` | — |
| ④b | `:48701` | **682：下一按重新武装** —— `OnTouchDown` 后 flag=1、`holdDisplayed` 归 0 | 漏清 `_holdDisplayedThisPress` ⇒ 第二次长按弹不出窗（静默） | — |
| ⑤a | `:48723` | **684+687 顺序与点亮**：`setup>use>btn:on>btn:off`（先填面板、再试放、真才点灯） | 两行先后互换 / 删掉 `PressedBtn(true)` | — |
| ⑤b | `:48728` | **0.4 s 与 0.3 是两个量**：0.3 那一刻 PressedBtn **还亮**、正好 0.4 **熄灭** | 0.4 合并成 0.3（第一格假）/ `>=` 写成 `>`（正好 0.4 不熄）/ 不挂收尾（第二格假） | ✅ **灭自证** |
| ⑥a | `:48745` | **684 的 `isPlayer` 闸**：敌方卡整条早退（面板没填、没试放、没点灯） | 删掉那道闸 | — |
| ⑥b | `:48759` | **684 真/假两条支**：`TryUsingActiveAbilityOfCard` 返回 0（打不了 / 进选目标）⇒ **不点灯但面板照填** | 删 `if (!used) return` / 把「进选目标」也算真 ⇒ 这里点灯；把 Setup 挪到 used 判断之后 ⇒ 面板 0 次 | ✅ **灭自证**（与 ⑤a「返回真 ⇒ 亮」成对） |
| ⑦`P` | `:48799` | **前提·日志捕获器有效**（自打一条 `K4-SENTINEL` 警告并抓到 1 次）—— 否则下面「听到 0 条」是假绿 | `Application.logMessageReceived` 在该环境不派发 | — （硬前提） |
| ⑦a | `:48801` | **687 钩子没接（原版 `PressedBtn == null`）⇒ 出声一次、且不假装**（警告里含 `PressedBtnSetActive`，按钮不亮、不挂熄灭） | `WarnOnce` 删成静默（0 条）/ 这一支改成也点灯 | — |
| ⑦b | `:48807` | **钩子接上之后一条警告都不该有**，且按钮亮了 | 「一律出声」/「一律不出声」 | ✅ **灭自证**（与 ⑦a 成对） |
| ⑧ | `:48820` | **手牌那道 0.2 s 门**：0.19 算轻点、**正好 0.20 已算长按**、9 s 长按（原版 `0.2 <= elapsed ⇒ 不开窗`） | 改成 0.3（与棋盘合并）⇒ 0.20 那格真；`<` 写成 `<=` ⇒ 0.20 那格也红 | — |
| ⑨`P` | `:48832` | **前提**：K-04 探针卡视图建出来了（否则下面两条空转） | `CardView.Create` 失败 | — （硬前提） |
| ⑨a | `:48842` | **棋盘那个类不接手牌档**：`CardState == inHand` ⇒ 返回假、且**没开窗**（0 次 `DisplayCard`） | 把 0.2 s 门搬进棋盘类（返回真 / 又开一次窗 ⇒ 一次点开两次窗） | — |
| ⑨b | `:48853` | **棋盘 inPlay 档不量时长**：喂 `PressSeconds = 9 s` ⇒ 照样 `DisplayCard(true)`、返回真 | 把 0.2 s 门挪到 inPlay 支 ⇒ 9 s 那一档不开窗 | ✅ **灭自证**（与 ⑨a 成对） |
| ⑩ | `:48873` | **同一时刻 0.2 s，两条门结论相反**：手牌**已算长按**、棋盘「按住弹卡面」**还没到**；且 `cardShowTime > handGate` | 两条合成 0.2 ⇒ 这里弹出；合成 0.3 ⇒ 0.2 s 时手牌不算长按 | ✅ **灭自证** |

**灭自证合计 6 条**（③ ⑤b ⑥b ⑦b ⑨b ⑩）+ 1 条边界双档（②）+ 3 条硬前提（① ⑦`P` ⑨`P`）。
**前提一律写成 `check(前提, …)`（红）**，本组**没有一处** `if (前提) Check(结论)` 那种静默跳过。

---

## 二、证据（文件:行号）

### 2.1 原版判据（全量反编译 `d:/2/tools/decomp_full/`，逐份亲读）
| 事实 | 出处 |
|---|---|
| `cardShowTime = 0x3e99999a` = **0.3f**（构造里就写死） | `ActiveWarlordAbility__.ctor.c:1-8` |
| `OnTouchDown` 全文两句：`+0x3c = Time.time`、`+0x38 = 1` | `ActiveWarlordAbility__OnTouchDown.c:1-14` |
| `OnTouchExit` 全文一句：`+0x38 = 0` | `ActiveWarlordAbility__OnTouchExit.c:1-7` |
| `Update`：`cardShowTime <= elapsed && elapsed != cardShowTime`（浮点上 = **严格大于**）；命中后对 `+0x38` 写**两字节** `0x100` | `ActiveWarlordAbility__Update.c:1-26` |
| `OnTouchUpAsButton`：`isPlayer` 闸 → `SetupActiveSkill` → `TryUsingActiveAbilityOfCard(forceSelectCard:1)` → 真且 `PressedBtn != null` ⇒ `SetActive(true)` + `DelayedCall(0.4f)`；`PressedBtn == null` ⇒ 什么都不做 | `ActiveWarlordAbility__OnTouchUpAsButton.c:1-72` |
| 0.4 s 后 `PressedBtn.SetActive(false)` | `ActiveWarlordAbility___OnTouchUpAsButton_b__12_0.c:1-9` |
| **手牌**那档 0.2 s 门：`0.2 <= elapsed ⇒ 不开窗`（读数 `:37`、比较 `:38`） | `CardScript__OnTouchUpAsButton.c:30-44`（`DAT_1834b2bb0` = 0.2f） |
| **棋盘** inPlay/2·3·17 档：`:70` `IsAeldariRemnant` ⇒ 收集；否则 `displayingCardFlag=1` + `DisplayCard(1, !MulliganElementsVisible)` —— **全文没有时间量** | `CardScript__OnTouchUpAsButton.c:63-88` |
| 常量实读（4 字节） | `0x1834b3158` = 0.4f · `0x1834b2bb0` = 0.2f · `ctor +0x40` = 0x3e99999a = 0.3f（沿用 `B9_W3ANIM` 的 `工具/read_literal.py` 输出） |

### 2.2 我们的实现（被测件，**只读**；本波一行没改它们）
| 事实 | 出处 |
|---|---|
| 状态机本体（`OnTouchDown/TouchExit/Tick/OnTouchUpAsButton` + `Clock` 注入口 + 三个读口） | `CardPresentation/Core/CardHighlight.cs:946-1124` |
| 0.3 门写成 `elapsed > CardShowTime`（= 严格大于） | 同上 `:1034` |
| `_holdDisplayedThisPress` / `_pressedBtnHidePending` 两处 `🔶 自己设计` 标记 | 同上 `:994` / `:996` |
| 手牌 0.2 s 门（判据本体 + 批处理注入口） | `CardPresentation/Hand/CardInteraction.cs:140`（常量）· `:513`（`PressShortEnough`）· `:1791`（`SetPressElapsedForTest`）· `:1798`（`PressShortEnoughForTest`） |
| 棋盘高亮类的 `OnTouchUpAsButton`（inHand 返假 / inPlay 不量时长） | `Core/CardHighlight.cs:796-827` |
| 宿主接线（四处调用点，本波**只读**，文件归 W19） | `Battle/BattleDriver.cs:33985`（`BoardCardTouchDown`）· `:14929-14998`（`BoardPress`，现读已**两条链都跑**）· `:34768-34806`（三个钩子 + 试放返回语义）· `:34814-34834`（`TickBoardHold`） |

### 2.3 编辑器侧（本波改动）
| 事实 | 出处 |
|---|---|
| 调用点（同一行内追加，1:1） | `Editor/BattleScene.cs:3596` |
| 断言组本体（文件尾） | `Editor/BattleScene.cs:48611-48881`（`static class K4HoldAssert` 声明在 `:48637`） |
| 同款先例（`Check` 是 `Run` 的局部函数 `:636`，所以必须委托外传） | `Editor/BattleScene.cs:48489-48609`（`W11ShakeAssert`） |

---

## 三、没查清的部分（如实记）

1. 🔴 **17 条断言一条都没跑过** —— 本波禁 Unity（红线 1），`BattleScene.Run` 由调度台在同步点跑。**下面这些都是「设计上应该绿」，不是「验过绿」**。
2. ⚠️ **⑦`P` 那条前提本身有风险**：`Application.logMessageReceived` 在批处理下**同步派发**这一点，本仓有先例
   （`CardPresentation/Core/CardFeel.cs:2997` 用它抓「Instantiating material …」并断言条数），但**没有**人验过
   「`LogType.Warning` + 我们这条中文文案」这个组合。⇒ 我把它拆成**哨兵前提 + 两条结论**：
   若捕获器在该环境不派发，⑦`P` 会**先红**，与「被测行为坏」区分得开（不会混成一个假红/假绿）。
3. ⚠️ **687 建层判据仍缺**（沿用 `B9_W3ANIM` §三·1：AR prefab 0 命中 / `d:/2/解包整理` 0 命中 / `assets_full` 只有 MonoScript /
   `CardPrefab` 10 个组件逐个解过无此组件）。⇒ 本组**只钉状态机与 0.4 s 时序**，**不钉**「那颗钮长什么样」。
4. ⚠️ **0.3 s / 0.4 s 的真机手感仍未验**（按住多久弹出、按压反馈亮多久）—— 断言用的是**假时钟**，
   证明的是「判据与时序在」，不是「手感对」。
5. ⚠️ **⑨ 用 `CardView.Create` 建一张探针卡**：它不依赖字体资产（`CheckCardViewState` 里也是无条件建的），
   但**没在批处理里跑过** ⇒ 若建卡那一步在主场景里出问题，⑨`P` 会先红并**明说**「下面两条没跑过」。
6. `CardInteraction` 探针只读 `PressShortEnough` **判据本体**；`Release` 整条链（开窗/不开窗）已有 `K2` 的断言，
   本组**不重复**（避免两份判据打架）。
7. **没做的**（不在本件范围）：W19 那份 `BattleDriver` 侧的 K-04 接线是否还有别的偏差（本件只读、不改它的文件）。

---

## 四、改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `Assets/CardPresentation/Editor/BattleScene.cs` | ① `:3596` **同一行内追加**调用 `K4HoldAssert.Check(driver.transform, Check);`（1:1 替换）② 文件尾**新增** `static class K4HoldAssert`（17 条 `Check` + 判据头注释） | `:3596` 行数不变；文件尾 **+272 行 / 0 删**（48610 → 48882） |
| `资料/完全复刻/账/_parts/B9_W24ASSERT.tsv` | K-04 五行（682 / 683 / 684 / 686 / 687）：`差异` 列**追加**本波记录、`轮次` = `B9_W24ASSERT 2026-10-27`、`备注` 追加一行指针；**状态 / 我们的落点 / 等级 一律照抄主表**（本波没改实现）+ 684 行**就地订正一句过期口径**（见 §五·1） | 6 行（表头 + 5 行）× 11 列，**LF、无裸 CR** |
| 生成器 | `d:/4/_tmp_view/gen_b9_w24assert.py`（**故意放临时目录**，不往 `账/_parts/` 里再堆 `.py`） | — |

**typecheck**（`TMPDIR=/tmp/wf_w24 bash 工具/typecheck.sh`）：
- ✅ 断言行插入后第一次 = **运行时 0 / 编辑器 0**。
- ⚠️ 收工前复跑 = **编辑器 0 / 运行时 3**，三条**全在** `Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs`
  （CS0128「已在该作用域中定义名为 `mx` 的局部变量」×1、CS0019 `Vector3` 与 `float` 运算 ×2）——
  那是 **W16 正在写的文件**（本波文件所有权表里明确归 W16）⇒ 按 `CLAUDE.md` 13·3「错误全部集中在不是你负责的文件上」处理，
  **没有去动它**；等它收工后复跑即为 0/0。

**未做**：跑 Unity · 动 git · 改 `CLAUDE.md` / `项目任务.md` / 任务总表 / 账主表 / 别人的报告 / 基线文件 —— 一条都没碰。

---

## 五、顺手发现

1. 🔴 **账 `战斗界面_HUD:684` 的「差异」列有一句已过期**（我已在补丁里就地订正，铁律 5）：
   主表原写「🔶 **分派口径是我们定的**：……我们按『这张卡有没有主动技』分派（有 ⇒ 走本链；没有 ⇒ 照旧开大卡窗）」——
   **W19BDR 2026-10-27 已撤掉那条 `tapHasAbility` 二选一互斥**，现读 `BattleDriver.cs:14966-14974` 是
   **两条链都跑**（`wa.OnTouchUpAsButton()` + `TapBoardCard(...)`），与原版「两颗组件各收一次 touch 事件」一致。
   ⇒ 该行剩的真缺口只有 **④ PressedBtn 建层**（判据本地拿不到）。
   ⚠️ **若 W19 自己也写了 684 行的补丁会撞**：现读 `工具/check_patch_conflicts.py` = **✅ 没有跨补丁重复键（4 份补丁）**，
   但补丁合并**必须**按 `--only B9_W24ASSERT` 串行做（`CLAUDE.md` 那条「合并顺序按字典序是巧合不是语义」）。
2. **`CardHighlight.ActiveWarlordAbilityTouch.OnTouchDown()` 比原版多写一格**（`_holdDisplayedThisPress = false`，`:1011`）——
   原版 `OnTouchDown` 全文只有两句。这是我们为了「第二次长按还能弹窗」加的（字段声明处已有 `🔶 自己设计` 标记 `:994`）。
   断言 ④b 把这条行为**钉住**了：⛔ 将来别为了「逐字照抄原版两句」把它删掉（删了会静默少一次开窗）。
3. **`CardHighlight.TouchInput.PressSeconds` 在棋盘那一侧没人读**（`:413-415` 的 doc 自己也说「只用在与 0.2 s 比的那两档」）。
   我把它当「棋盘档不量时长」的**探针输入**用（⑨b 喂 9 s）—— 这是这条断言能成立的关键：只要有人往 inPlay 支塞时长门，
   9 s 那一档立刻不开窗。
4. **文件尾断言类现在是第 2 个**（`W11ShakeAssert` → `K4HoldAssert`）。建议后续新断言继续走这一口：
   `Run` 里那个 `Check` 是局部函数，往中间塞必然让本文件几十条账的 `文件:行号` 整体漂；
   ⛔ 别再发明第三种做法（比如在 `Run` 里直接写一大段）。
5. **本组顺带验证了一条「判据读深一层」的实例**：`CardScript__OnTouchUpAsButton.c` 的 `if (iVar3 < 4)` 分支里，
   `cardState 2/3` 是**穿过** if/else 落到 `:63` 之后那段（**不是** `goto` 到 0.2 s 门）—— 光看 `iVar3 < 4` 很容易读成
   「2/3 也走 0.2 s 门」。⇒ 判「某档量不量时长」必须**顺着控制流读到那一档自己的代码**，⛔ 别按 `switch` 的直觉猜。
