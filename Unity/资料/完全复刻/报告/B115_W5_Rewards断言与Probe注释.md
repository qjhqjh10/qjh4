# B115_W5 · Rewards 倒计时断言（CHK-06）+ TutorialProbe 过期注释（TOOL-02 尾巴）

> 执行写手 `B115_W5` · 2026-11-01。
> 判据来源：`资料/完全复刻/判据/B115_旧任务对账_E_断言工具.md`（`CHK-06` 行 / `TOOL-02` 行 / 顺手发现 4）。
> 只动了两个文件：`Editor/RewardsScene.cs`（+80 行）· `工具/ruleprobe/TutorialProbe.cs`（2 行就地改写，行数不变）。
> 未跑 Unity · 未动 git · 未动账主表与正本。

---

## 一、活 1 · `CHK-06`：补两条有鉴别力的倒计时断言

### 改哪一处（`Editor/RewardsScene.cs`）

| 位置 | 内容 |
|---|---|
| `:2586-2589`（原样保留） | 原断言 `timerText.StartsWith(prefix) && timerText.Length > prefix.Length` —— **一个字没动**（它挡「换成死串 / 空串」） |
| `:2590-2669`（新增 80 行） | 追加两条断言 + 三条前提 + 一个局部读数函数 `DigitsOf` |

新增 6 条 `CheckTrue`（绿路径下全过）：

| 行 | 断言 | 作用 |
|---|---|---|
| `:2615` | （前提）`Timer` 节点 + 它身上的 `TimerDisplay` 都取得到 | 硬前提，不静默 |
| `:2632` | **剩余少 1 秒 ⇒ 渲染文本必须变** | 挡「印定值」——死串在这里必红 |
| `:2640` | （前提）两次采样都能从 `prefix` 之后切出「时间那一格」 | 硬前提 |
| `:2647` | （前提）时间那一格恰好两段数字（本档 = `MS` 分+秒） | 硬前提 |
| `:2653` | **数字段解析得成整数、且在本档上限内**（分钟 ≤ 2 · 秒 ≤ 59） | 把原「长度 > 前缀长度」的功能用**数字判据**承担 |
| `:2663` | 两次采样的秒数差 = 1（允许 1–2，第二次采样前真实时钟也在走） | 挡「文本被别的东西改了、剩余其实没动」 |

前提失败时另有两条 `CheckTrue(false, …没验成)` 显式报红（同文件 `:10690` 的既有写法），**没有 `if (取得到) Check(…)` 这种静默跳过**。

### 关键口径：怎么「把时钟往前推 1 秒」

`Shell/TimerDisplay.cs:390` 的剩余式是 `remain = target − DateTime.Now`，而 `DateTime.Now` **本机不可拨**。
⇒ 等价做法 = **把目标时刻减 1 秒**（`DailyData.ForceNextResetForTest(lgTgt.AddSeconds(-1.0))`），只动我们自己的数据口，
不碰实现、不伪造时钟。数据口用现成的 `ForceNextResetForTest`（同文件 `:10674-10677` 已有先例），**没有自创时间轴**。

档位选择：目标 = 现在 + 125 秒 ⇒ 剩余落在 `MS`（分+秒）档，1 秒差在文本上一定看得见；
**不能**定成几小时——小时档粒度是分钟，推 1 秒文本本来就该不变（那会变成假红）。

跑完后 `DailyData.ForceNextResetForTest(lgResetWas)` + `lgTimerTd.Refresh()` **当场还原**，不把造态留给后面的节。

### 判据

- `B115_旧任务对账_E_断言工具.md` `CHK-06` 行：「只要印出一段**更长的死字符串**（如 `准备在 99 天 23 小时`）依然绿 —— 正是条目要修的那一处」；
  写法规格「推 1 秒后文本必变 / 数字段可解析且 ≤ 上限」「先硬 Check 前提」。
- 生产侧现状（现读）：`Shell/MissionsTab.cs:1111` `TimerDisplay.Attach(tl, () => DailyData.NextResetAt, MissionTimerDisplay.Description(false, DailyData.LoginClaimed()), …)`（`lastMinutesPrefix` = null ⇒ 没有最后 5 分钟档干扰）。
- 现网读数（`_tmp_view/rewards.log` 2026-11-01 14:56）：该条实得「在 9小时 3分钟 可用」，前缀 = `在 `（`Missions/CompleteCounter` = `在 {0} 可用`）⇒ 切出的时间格 = `9小时 3分钟`，无干扰数字。

### 灭自证 / 鉴别力自检（未跑 Unity，逻辑推演）

- 🧨 把 `TimerDisplay.UpdateTimer` 那格写成定值串（如「在 99 天 23 小时 可用」）⇒ 原断言仍绿（前缀对、长度够）、**`:2632` 与 `:2653` 双红**。
- 🧨 让 `UpdateTimer` 每次都印同一个剩余 ⇒ `:2663` 红。
- 反例（真活值）==> 全绿（见下「预期读数」）。

---

## 二、活 2 · `TOOL-02` 尾巴：`TutorialProbe.cs` 两句过期注释

`工具/ruleprobe/TutorialProbe.cs` **行数不变（1135 行）**，两行就地改写：

| 行 | 原文（过期） | 现文 |
|---|---|---|
| `:1048` | 「我们侧 ★缺读口 —— `BattleDriver.cs:16260` `TipAnchor` + `:17032-17092`」 | 「我们侧 已接 —— `BattleDriver.cs:17120` `TutorialAnchor` + `:36873` `TutorialAnchorExtra`（`:17170` 的 default 跳过去）；Unity 断言 `Editor/BattleScene.cs:37734/37738/37743/37746/37749`（`b114_unity_battlescene3.log:200939/200952/200978` 实跑 ✓）｜⚠️ 2026-11-01 更正：…」 |
| `:1061` | 「差异 三处已在账里（… · 50/70/100 未做（数据 0 条） · …）」 | 「差异 已收口（… · 50/70/100 已做：数据 0 条也按铁律 11 建了 `TutorialAnchorExtra`，配 3 条断言 + 2 条前提 · …）｜⚠️ 2026-11-01 更正：…」 |

两行都保留了「⚠️ 日期 更正：原来写 X，实际是 Y」的痕迹。

### 判据（本轮现读，不是照抄判据件）

| 事实 | 现读证据 |
|---|---|
| 产品侧锚点已接 | `Battle/BattleDriver.cs:36873` `bool TutorialAnchorExtra(int positionReference, out Vector3 center, out Vector2 size)`（`EnemyMana(70)` / `ActiveAbility(100)` / `EnemyCardInHand(50)`） |
| 分发跳转 | `BattleDriver.cs:17170` `default: if (TutorialAnchorExtra(positionReference, out center, out size)) return true;` |
| 主 switch | `BattleDriver.cs:17120` `bool TutorialAnchor(int positionReference, out Vector3 center, out Vector2 size)` |
| `TipAnchor` 已不存在 | `grep -n "TipAnchor" BattleDriver.cs` ⇒ 0 命中（唯一相关名是 `TutorialAnchorForTest`，`:37694`） |
| 原写的行号已漂 | `BattleDriver.cs:16260` 现在是 `RefreshReticle` 一带的伤害结算，与锚点无关 |
| Unity 断言 | `Editor/BattleScene.cs:37734`（前提·能量底板）· `:37738`（70/80）· `:37743`（100）· `:37746`（前提·敌方手牌）· `:37749`（50） |
| 真跑过 | `_tmp_view/b114_unity_battlescene3.log:200939 / :200952 / :200978` 三条都是 `BT ✓ ★ **账 103**…` |

### 验证方式（不止编译）

- `cd 工具/ruleprobe && dotnet build -v:q --nologo` ⇒ **0 个错误**（1 个既有 warning：`TutorialPositioning.cs(34,27) CS0282`，与本改无关）。
- 真跑读数口：`dotnet bin/Debug/net8.0/wfprobe.dll tutorial g3 111` ⇒ 两行新文案**原样印出来了**（贴一段）：

```
──────── [行 111] AiScripted.GetTipPosition(manager, pos, relation, referenceUnit) ────────
  判据 🥈 cpp2il dev AiScripted.cs:3114-3466（Ghidra 274 行）—— 锚点逐档 + Relation 偏移
  我们侧 已接 —— `BattleDriver.cs:17120` `TutorialAnchor` + `:36873` `TutorialAnchorExtra`（50/70/100 三档，`:17170` 的 default 跳过去）；Unity 断言 `Editor/BattleScene.cs:37734/37738/37743/37746/37749`（b114_unity_battlescene3.log:200939/200952/200978 实跑 ✓）｜…
```

---

## 三、闸读数（本轮现跑）

| 闸 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_w5 bash 工具/typecheck.sh` | **运行时错误数 0 · 编辑器错误数 0** |
| 聚合行闸 | `python -I -X utf8 工具/check_aggregate_lines.py <RewardsScene> <BattleScene> <ShellScene>` | 文件 3 · 聚合行 2 · 调用 110 · **违规 0 ⇒ PASS**（`RewardsScene.cs` 本身无聚合行：`< 20 个匹配`；另两条 `BattleScene.cs:46896` 52/52、`ShellScene.cs:11093` 58/58 全在 `//` 之前） |
| 账闸（只读，未 `--fix`） | `python -I -X utf8 工具/check_ledger_points.py` | **A 5,841 条 · 通过 5,841 · 漂移 0 · 失败 0**（告警 859 条，与改前一致） |

### 行号漂移与旧→新映射（⛔ 我没改任何 `账/*.tsv`）

插入点在**旧 2590 行**，插入 80 行 ⇒ 旧 ≥ 2590 的一切行号 **+80**。

- **账的落点列**：现读只有 1 行 `我们的落点` 提到 `RewardsScene.cs`（`账/战役.tsv:34`），而它是散文（`—（原版不可达：…）`、无 `文件:行号`）⇒ 账闸**漂移 0 / 失败 0**，无需任何补丁。
- **账的散文列里（备注/差异）有 5 处 `文件:行号` 引用**，其中 3 处随 +80 漂了（这些列不进闸）：

| 账行 | 旧 | 新 |
|---|---|---|
| `账/战役.tsv:34`（差异） | `Editor/RewardsScene.cs:4933/4935` | `:5013/5015` |
| `账/收藏.tsv:265`（备注） | `Editor/RewardsScene.cs:9637` | `:9717` |
| `账/日常_奖励.tsv:82`（备注） | `Editor/RewardsScene.cs:13525` | `:13605` |
| `账/日常_奖励.tsv:510`（备注） | `Editor/RewardsScene.cs:2543` | 不变（在插入点之前） |
| `账/菜单_外壳.tsv:215`（备注） | `Editor/RewardsScene.cs:1061` | 不变（在插入点之前） |

映射已用 `git show HEAD:…` 与工作树逐行比对确认（这 5 个旧行号在新文件里 4 处逐字相同；`9637` 旧新都是空行 = 本来就指偏了，不是本次造成的）。
**若调度台要收口：只需把那 3 个散文行号 +80**（可选，不影响任何闸）。

---

## 四、要跑的宿主与预期读数（本波没跑 Unity）

**跑 `RewardsScene.Run`**（`Unity.exe -batchmode -quit -projectPath D:\4\Unity\MyGame -executeMethod RewardsScene.Run`）。
上一次同代码基线：`_tmp_view/rewards.log`（2026-11-01 14:56）= **3088 通过 / 0 失败**。

预期：**3094 通过 / 0 失败**（+6 = 本轮新增的 6 条绿路径断言）。要认的 6 行（前缀 `[Rewards]`）：

```
✓ （前提·不静默）登录卡 `Timer` 节点 + 它身上的 `TimerDisplay` 都取得到…
✓ ★ 倒计时**随剩余递减**：剩余少 1 秒 ⇒ 渲染文本**必须变**（实得「在 2分钟 4秒 可用」→「在 2分钟 3秒 可用」）…
✓ （前提）两次采样都能从前缀之后切出「时间那一格」…
✓ （前提）时间那一格**恰好两段数字**（本档 = MS 分+秒；实得 2 / 2 段）
✓ ★ 时间那一格**解析得成整数、且都在本档上限内**（… 实得分 2 · 秒 4）
✓ ★ …而且秒数差 = 1 秒（… 实得 1）
```

（秒数取 4/3 还是 5/4 取决于采样瞬间的余数截断，差必须恰为 1。）

⚠️ 若这两条红了，先看实得串：
- 若实得是「在 9小时 3分钟 可用」（= 还原后的正常值）⇒ 说明 `ForceNextResetForTest` 没影响到 `TimerDisplay` 的 `_target` delegate；
- 若 `lgDelta` = 0 且两串完全相同 ⇒ 生产侧真的印了定值（那就是本条要抓的缺陷）；
- 若 `lgDelta` = 3 以上 ⇒ 两次采样之间机器卡了 >1 秒（编辑器 GC / 控制台刷屏），属偶发，复跑一次即可。

其余宿主不受影响（只动了 `RewardsScene.cs` 的一个自检块 + 一个纯工具文件）⇒ **不需要**跑 `_run_8_checks.sh` 全套。

---

## 五、顺手发现（⛔ 未改，只报）

1. **`TutorialProbe.cs:1062` 的 `_bad++` 现在与新文案打架。**
   该块结尾仍 `_bad++`，于是 `tutorial g3 111` 的收尾行还是「⚠️ 有 1 块读不下去 / 缺读口」，而上面两行已经写着「已接 / 已收口」。
   `_bad` 的定义（`:73`）=「读数本身坏掉（读不到数据 / 反射找不到）」，本块现在**没有任何读不出来的东西** ⇒ 建议调度台删掉这一句（**一行，零风险**；⛔ 本波按简报「只改那两行注释、不增删行」没动它）。
   若保留的意图是「偏移量同上一行」那条差异还开着，那应把文案改回「差异：偏移量…」，二选一，别留两种说法。

2. **`BattleDriver.cs:36873` 的 doc 里那句「这三档在教程数据里各 0 条」现已可核**：
   `tutorial g3 111` 现读分布 = `0×350 10×8 13×2 21×3 22×1 23×4 30×5 40×27 60×9 80×3 90×18`
   ⇒ **50 / 70 / 100 确实一条都没有**，而 80 有 3 条（80 走的是主 switch 的 `PlayerMana`，不是 `TutorialAnchorExtra`）。与 doc 一致，无需改。

3. **`RewardsScene.cs` 里同类「只测格式不测递减」的地方只剩这一处**：
   `grep "RewardsScene.cs" 账/*.tsv` + 现读该文件，别的倒计时断言（`:10665-10690` Premium 弹窗那条）走的是 `Expired` + `ToFocus` 副作用面，**有鉴别力**，不需要同样处理。

4. **我写进 probe 注释的行号是「此刻现读」**：本轮同时有别的写手在改 `BattleDriver.cs` / `BattleScene.cs`（`git status` 现读可见），
   ⇒ `:17120` / `:36873` / `:37734…37749` 随时可能再漂。所以那两句注释**同时写了方法名**（`TutorialAnchor` / `TutorialAnchorExtra`）
   —— 下一个人按方法名找，别只认行号。

5. **`rewards.log` 里那条旧断言近三次实得分别是「11小时 3分钟」「16小时 35分钟」「3小时 21分钟」**（`b112w_z_rewards.log` / `rewards4.log` / `rewards.log`）⇒ 现网确实是活值，新断言按现网形态应当全绿。
