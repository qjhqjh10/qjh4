# B117-FIXE — 两条 `BattleDriver.cs` 补丁落地记录

日期：2026-11-02 · 执行代理 `B117-FIXE`
输入判据：`B117-FIXD_收藏方叉与零散.md` §2.2（`B117G1004` ×4）· §2.10（`B117F11` E2）
施工判据（更详）：`B117-G10-04_HUD状态信息层.md` §二（补丁 A / 补丁 B 的可粘贴原文）

**只动了一个文件**：`CardPresentation/Battle/BattleDriver.cs`（+79 行、**0 行被删**；行尾全 CRLF、无裸 CR）。
⛔ 没碰 `Editor/*Scene.cs` · `Core/CardView.cs` · 账主表 · git · `d:/2`。

---

## 一、`B117G1004` ×4 —— 补丁 A（里程碑可见性）+ 补丁 B（三枚已出牌数灯）

### A. 里程碑按 `playMode` 显隐 —— `:32172`（改已有行，**行数不变**）

**旧**（`:32172`）：
```csharp
            if (_skullScore != null) _skullScore.SetText("x" + _foeSkullCount);
```
**新**（同一物理行）：
```csharp
            bool showSkull = MilestonesExist(Ctx); SetVis(_skullIcon, showSkull); SetVis(_skullScore, showSkull); if (_skullScore != null) _skullScore.SetText("x" + _foeSkullCount);   // …判据注释…
```
- `SetVis` **两个重载都已存在**（`HudPiece` = `:17346`、`Component`（`Label` 走这支）= `:17343`），没新写。
- 文案**照写**（原版空表也写 `x0`，只是组件关着）—— `SetText` 没被跳过。
- 原版关的是**组件**、我们关的是**对象**，这是判据里已登记的刻意近似（骷髅那颗 tooltip 我们从来没做，
  且 `HitTip` 自带 `!activeSelf` 闸 ⇒ 今天无差别）。

### B-1. 三枚灯 —— `:32173` 注释就地订正 + `:32174-32176` 三行重写（**3 行 → 3 行**）

**旧**：
```csharp
            // 本回合已出牌数：出几张亮几枚（原版只有三枚节点，第四张不显示）
            if (_playedPips != null)
                for (int i = 0; i < _playedPips.Length; i++)
                    if (_playedPips[i] != null) _playedPips[i].gameObject.SetActive(i < _cardsPlayedThisTurn);
```
**新**：
```csharp
            // …（原句 +）🔴 2026-11-02（`B117G1004`）：**先过 holder 那道闸** …… 张数改用**全局** `Ctx.CardsPlayedInTurn`，⛔ 不是本类那个 `_cardsPlayedThisTurn`
            if (_playedPips != null) { int pipsLit = PipsLit(Ctx);   // 原版 `SetTurnPlayedCards(cards)` × holder 那道闸（判据见类尾 `PipsLit`）
                for (int i = 0; i < _playedPips.Length; i++)
                    if (_playedPips[i] != null) _playedPips[i].gameObject.SetActive(i < pipsLit); }
```
`_cardsPlayedThisTurn` **没删**（`CardsPlayedThisTurn` 读数口 `:31114` 与旧断言仍在用，且这个量本身仍只数我方 —— 那不是缺陷，是另一个量）。

### B-2. 三个静态方法 —— 追加在 **`BattleDriver` 类尾**（`:40798-40876`，新增 79 行）

| 方法 | 行 | 作用 |
|---|---|---|
| `PipsLit(BattleContext)` | `:40829` | `holder 闸` × `min(3, ctx.CardsPlayedInTurn)` |
| `OffensiveCardHasThirdCardTrigger(BattleContext)` | `:40843` | `OffensiveSlotIdx >= 0` 且该 `envSO` 的进攻卡有 `trigger == 15` |
| `MilestonesExist(BattleContext)` | `:40866` | 6 档（Duel/PracticeLodge/Tutorial/CutScene/Campaign/TutorialReplay）返回 false |

**位置确认（`B116_W23` 那个坑）**：本文件有**两个顶层类** —— `BattleDriver` 收在 `:40877`、namespace 收在 `:40878`，
`:40881` 起是第二个顶层类 `ChoicePanelPool`。新成员全部落在 `MinionKey` 的 `}`（`:40797`）之后、`BattleDriver` 的 `}`（`:40877`）之前 ⇒ **类对了**。
账里对 `BattleDriver.cs` 的**最大落点是 `:40001`**（793 处全扫过）⇒ 本段之后**一条 A 档落点都没有**，
`check_ledger_points.py` 复跑**输出的问题表里 `BattleDriver.cs` 0 行**。

---

## 二、`B117F11` E2 —— AoE 并排的**起锚**（`:19617`，改已有行，**行数不变**）

**旧**：
```csharp
                t += beatGap; if (HitBurstParallel(_signalBuf, i)) { if (float.IsNaN(hitBurstAt)) hitBurstAt = t; else t = hitBurstAt; }
```
**新**（同一物理行）：
```csharp
                t += beatGap; bool burst = HitBurstParallel(_signalBuf, i) || (e.Kind == EvtKind.Hit && i + 1 < _signalBuf.Count && HitBurstParallel(_signalBuf, i + 1)); if (!burst) hitBurstAt = float.NaN; else { if (float.IsNaN(hitBurstAt)) hitBurstAt = t; else t = hitBurstAt; }
```
**为什么**：一条 `Hit` 自己 `HitBurstParallel` **恒 false**（它前面没有 `Hit`）⇒ 旧写法把锚取在
「组首**已经** `+= DurationOf` 之后」⇒ 并排组自己又多算一整条。实测 `span1 = 1.00 s` / `span3 = 2.00 s`（= 2 × `HitWaitSeconds`）。
改成「`i+1` 也是并排 ⇒ `i` 就是组首、由它起锚」后：3 条 `Hit` 全落在 `t0`，尾巴 = `t0 + DurationOf(一条)` ⇒ `span3 == span1`。

### ⚠️ 两处**对判据原文的偏离**（都写进了代码注释）

1. 🔴 **判据补丁里写的 `OffensiveCardData.Get(…)` 原样编译不过** —— `BattleDriver` 类里**另有一个同名静态方法**
   `OffensiveCardData(OffensiveCards.Card, string)`（`:9575`），简单名先绑到那个方法组 ⇒ `CS0119`。
   ⇒ 代码里写的是全限定名 **`RuleEngine.OffensiveCardData.Get(…)` / `.ThirdCardAbilities(…)`**。
   （这是编译期错误、不会静默，但判据文本要跟着改，否则下一个人照抄还会撞。）
2. 🔴 **多了一句 `if (!burst) hitBurstAt = float.NaN;`**（判据的 3 行补丁里没有）。
   理由：同一批里若有**第二组**多目标伤害（一次结算里「先群伤、再点伤」就可能），上一组的 `hitBurstAt` 不清空
   会把第二组整组拽回**第一组**的起点 ⇒ `t` 倒退、其后的事件排到更早的时刻。`hitBurstAt` 只在本批内用 ⇒ 清空零副作用。
   ⚠️ 这条**不改变** `[Hit, Trigger, Hit]` 那种夹层情形（改前改后都是不压缩）。
3. 另外 `e.Kind == EvtKind.Hit` 那半句是刻意加的闸：只有「组首本身是 `Hit`」才许起锚，
   免得把锚拍到 `Trigger` 这类夹层事件上（不加的话 `Trigger` 也可能去吃并排窗口）。

---

## 三、应该转绿的清单（复跑 `BattleScene.Run` 时核对）

| 断言 | 现在应该 | 说明 |
|---|---|---|
| `B117G1004` **B1** | 绿 | `PlayMode = PracticeLodge` ⇒ 骷髅 `activeSelf = false` |
| `B117G1004` **B2** | 绿 | 同上，`x N` 也藏 |
| `B117G1004` **C2** | 绿 | `OffensiveSlotIdx=0` + `"Environmental Condition Dark Angels Asteroid Zone"`（该键现读**确实在** `Resources/offensive_card_abilities.json`，`abilities[0].trigger = 15`）+ 2 张 ⇒ 亮 **2** 枚 |
| `B117G1004` **C3** | 绿 | 5 张 ⇒ 亮 **3** 枚（上限 3） |
| `B117F11` **E2**（`★ B117-37` 那条） | 绿 | `span3 ≈ span1 ≈ HitWaitSeconds` |
| `B117G1004` B3/B4/B5 · C1 · C4 | **保持绿** | 它们与补丁方向一致（Classic 要画 / 无进攻卡 0 枚 / 清 0 全灭） |

---

## 四、⚠️ 会因此**转红**的两条旧断言（不在本代理红线内，需调度台派人改）

`Editor/BattleScene.cs` 的两条**旧口径**断言：

| 现位置 | 现在 | 打完补丁后 |
|---|---|---|
| `BattleScene.cs:4704` | `Check(drv.PlayedPipsOn == Mathf.Min(3, drv.CardsPlayedThisTurn), …)` | **大概率红**（该局的进攻卡多半不带 `trigger 15` ⇒ 灯恒 0） |
| `BattleScene.cs:6935` | `Check(driver.PlayedPipsOn == Mathf.Min(3, driver.CardsPlayedThisTurn), …)` | 同上 |

改法 = `B117-G10-04_HUD状态信息层.md` §三末那张表：进攻卡不带 `trigger 15` ⇒ 断 `== 0`；带 ⇒ 断 `Mathf.Min(3, Ctx.CardsPlayedInTurn)`。
⚠️ **判据里写的 `:4683` / `:6914` 是旧行号**（本文件这两个月一直在加行）—— 现读是 **`:4704` / `:6935`**。
`:6933` 的 `driver.CardsPlayedThisTurn >= 1` **保留**（本地计数这个量本身没变）。

---

## 五、`typecheck`

```
TMPDIR=/tmp/wf_fixe bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

## 六、冲突 / 并发情况（本波 `BattleDriver.cs` 已被多人改过）

- `git diff --numstat` 对 `BattleDriver.cs` 报 **547 / 94** —— ⛔ **不能当「我改了多少」用**（那是与 HEAD 的累积差异，
  本文件在我开工前就已被其他代理改过）。**替代判据**：`\n` 计数的**前后差 = +79 行**（40881 → 40960），
  与我这次「0 行就地改 ×3 + 类尾新增 79 行」逐值吻合；`\r\n` 计数 == `\n` 计数 == **40960**、裸 CR = **0**。
- 我改的三处**都在本代理独占的写入窗口内**，未发现与其他写手的同文件冲突（`B117-FIXD` 已明确记「没改 `BattleDriver.cs`」）。
- `check_ledger_points.py` 复跑：问题表里 **`BattleDriver.cs` 0 行**（现存 264 漂移 / 29 失败全在别人的文件上，与本补丁无关）。

## 七、没做的事（如实记）

- 没跑 Unity 批处理（红线）；上面「应转绿」是**按代码路径推的**，以调度台复跑 `BattleScene.Run` 的合计行为准。
- 没改 `Editor/*Scene.cs`（两条旧断言见 §四）。
- `B117G1024` E0 / `704562` / `800493` / `NetBattleTest` 退出码 1 —— **不在本代理任务内**，仍按 `B117-FIXD` §七 卡在原处。
