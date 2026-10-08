# W_BattleScene 补挂（`A985⑨` 槽数两条 + 8 行窗口那条核查）

- 宿主：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（只动这一个文件）
- 落点：§5b·2「棋盘轻点」那段（①′ 之后、② 之前），新开一节 `⑤`，**净 +116 行**
- 类型检查：`TMPDIR=/tmp/wf_bs2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
- 行尾：改前 `crlf 18677 / lone_lf 0`，改后 `crlf 18793 / lone_lf 0` ⇒ **纯 CRLF 未被翻**（未用 `sed`）

---

### ① 槽数两条断言：期望值出处 · 两态 · 🧨 改坏法 · 灭自证

**期望值出处（原版读数，⛔ 不是我们的常量）** —— 已按简报现核一遍，**一致**：
- `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json`
  （`CardDisplayWindow`）序列化字段 **`cardEffectSlots` 长度 = 5**；
- `CardDisplayWindow__DisplayCardEffects.c` **没有 `Instantiate` / `AddChild`**，只遍历该数组
  ⇒ **槽数是常量、不随 buff 条数长**。
（简报里「14 个包各恰好 5」那条**我没有复算**——它在本笔白名单外，且与上面两条同向；记为未复核。）

**落地的两条**：
1. **常量**：`CardWinBox.EffSlots == 5 && CardWinBox.EffRowCy.Length == 5` —— 两边都是**字面量 5**
   （⛔ 不是 `EffSlots == EffRowCy.Length`，那才是同义反复）。
2. **运行期（非自证）**：数**树上真实的节点** —— 在 `driver.CardDisplay.transform` 下
   `GetComponentsInChildren<Transform>(true)` 里 `EnchanterText` / `EffectText` / `EffectBg`
   **各 5 个**。这一条读的是**建出来的对象本身**，与 `EffSlots` 常量无关。

**两态**（都真开窗）：
- **态 A = 5 条（正好占满）**：`EffectRowCount == 5` · 节点数 3×5 · 后 3 槽 = 探针 1/2/3（逐槽对位）·
  前 2 槽仍是既有夹具（**本节只加不改**）。
- **态 B = 6 条（超一格）**：仍然 `EffectRowCount == 5`，且**前 5 槽逐槽与态 A 完全相同**、
  第 6 条**一个槽都没进** ⇒ 截断是「丢尾巴」不是「顶掉头」。

**夹具前提（关键，差点写错）**：那段上面 `①′` 已经给同一个单位挂了 **2 条展示用 buff**
（`attack +2` / 先锋，`UntilMyNextTurn = true`）。所以本节**从 2 条起算**，只加 3 条探针凑满 5；
⛔ **不能**用 `RevertBuffs(false, 0)` 把那两条撤掉 —— 它们**只 `AddTempBuff`、从没真加过属性**
（`UnitState.AddTempBuff` 只 `_buffs.Add`），一撤 `RevertBuffs` 就会 `Attack -= 2` ⇒ 反向改坏夹具。

**🧨 改坏法**：
- 常量那条：`EffSlots` 改 4 / `EffRowCy` 少一项 ⇒ 红；
- 节点数那条：`BuildEffectList` 循环上界写死 4 ⇒ 三个计数一起变 4 ⇒ 红；
- 逐槽对位那条：5 个槽都写 `rows[0]` ⇒ 红；
- 态 B 那条：把 `SetEffectRows` 的 `Mathf.Min(rows.Count, CardWinBox.EffSlots)` 去掉
  ⇒ 第 6 条去写 `_fxWho[5]`（数组只有 5 个）⇒ 越界 ⇒ 红（`CardDisplayWindow.cs:433` 的告警就是这一句发的）。

**灭自证（三条独立口，同一个改动不可能一起满足）**：
(a) **节点数**（树上的对象）；(b) **`EffectWho(5) == null`** —— 它问的是**数组真实长度**
（6 个的数组会答**空串**、非 null）；(c) **5 个 `What` 两两不同** + **后 3 槽按序** ——
挡「把第 0 行抄 5 遍」那种假实现。
外加收尾 `RevertBuffs(true, 0) == 4`：挡「探针根本没挂上去」那种**假绿**（节点数那条对它无感）。
⚠️ 没断言到的一处：`SetEffectRows` 那句 `Debug.LogWarning`（超限出声）**没有**被断言盖住。

---

### ② 8 行窗口那条：**不会红**（判定 = 不影响，附证据）

那条断言 = `BattleScene.cs:2825-2831`（`for (int i = 0; i < 8 && !sawPlay; i++)` 找「打出」/「进入格位」）。

**证据链（三条，指向同一个结论）**：
1. 🔑 **窗口是「最新 8 条」，不是「最早 8 条」**：`BattleDriver.RefreshBattleLog`
   （`BattleDriver.cs:5640-5684`）从 `log.Count - 1` **倒着**填、`_logEntries.Count < 8` 就停；
   `BattleEvent` 收留档走 `Emit → AppendLog`（`BattleContext.cs:1330-1362`）。
   ⇒ **更早**多出来的 `Draw` 行只会把**更早**的那些挤出窗口，**动不了「打出」那一步的下标**
   （新事件没有改变「它之后还有几条」）。
2. 🔑 **从「拖拽出牌」到那句日志断言之间没有回合边界**：中间只有
   `for (…180…) Step(1f/60f)` + `Step(0.2f)` + `Step(0.3f)`，而 `Step`（`BattleScene.cs:18612-18621`）
   只推 `AdvanceTimeline` / `CardTween` / 粒子 —— `AdvanceTimeline` 只从队列里播表现事件
   （`BattleDriver.cs:9779-9840`），**不推回合、不跑 AI**。本节的最近一次 `SimulateEndTurn` 在 `:3118`，
   远在那句断言**之后**（该节是 2b，`SimulateEndTurn` 属于后面的段）。
3. `EvtKind.Draw` 的**唯一发出点** = `RuleCore.Draw`（`RuleCore.cs:1211`，`!ctx.MulliganOpen` 才发），
   而它的调用点是：`BeginTurn` 的抽牌（`:837`）· 换牌补抽（`:333`）· **卡效果抽牌**（`:5009` /
   `EffectResolver.cs:1707`）· 教程脚本。前两类都在「出牌之前」，后一类即便被那张牌触发也只有 1~2 条
   ⇒ 出牌那一步顶多落到第 2~3 行，**离 8 行还差得远**。

**⇒ 判定：既不是「实现错」也不是「前提不成立」，是【不影响】。⛔ 我没有改任何期望值、
也⛔ 没有动 `RefreshBattleLog`（那不是本笔的文件）。**
- 顺带核过、**同样不受影响**：`RowLinkKey` / `RowAt` / `RowCenterWorld` 那几条（`:2903-2960`）
  全是**按内容/几何动态取行号**（`linkRow` = 第一个有链接的行、`other = linkRow==0?1:0`），
  不写死下标；行底图那三条断的是**三张变体的映射**，不是「某一行是什么图」。
- ⚠️ 一个**可能但无害**的连带：若某张牌的「抽牌」发生在 `Emit(Play)` 与 `Emit(Deploy)` 之间，
  `AppendLog` 的合并规则（Deploy 并进前一条 Play，`BattleContext.cs:1355-1359`）会**不再合并**
  ⇒ 日志多一行「进入格位」。它**照样满足** `sawPlay`（那条断的就是 `打出 || 进入格位`），
  本节的其它断言也不数行数。⇒ 不改正、不报缺陷，只在这里记账。

---

### 类型检查 · 行尾 · 没查清

- 类型检查：**运行时 0 / 编辑器 0**（改完立刻跑，独立 `TMPDIR=/tmp/wf_bs2`）。
- 行尾：**纯 CRLF 保持**（`crlf == lf`、`lone_lf == 0`）；`git diff --numstat` = `917 / 17`
  （那 17 行删除与本笔无关 —— 该文件**进来时就已经被本会话别的写手改过**，diff 里含他们的量）。
- 🔴 **没跑 Unity**（本笔宿主 = `BattleScene.cs`）⇒ 本节这几条断言**一次都没真跑过**。
  按铁律 12，落点属「只动一个自检宿主」⇒ **`BattleScene.Run` 一条即覆盖**，请在同步点跑。
- **没查清 / 挂在外面**：
  1. 简报里「**14 个包每个恰好 5**」这一条**本笔没复算**（白名单外、与另两条同向）；
  2. **`EffectBg` 数不到 5 时**：那是 `40K_display` 不在 `Resources/Art/ui/` 的另一条失败
     （该图**现读存在**，`Resources/Art/ui/40K_display.png`）—— 断言里已写进提示；
  3. `SetEffectRows` 超限那句 `Debug.LogWarning` **没有断言覆盖**（见 ① 末）。
