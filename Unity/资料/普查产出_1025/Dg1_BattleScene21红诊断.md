# Dg1 · `BattleScene` 本轮 21 红诊断（只读代理产出，主对话落盘）

> 代理 Dg1 为只读（Explore，无写工具）⇒ 报告由主对话代存。
> 日志：`d:/4/_tmp_view/20261025/battlescene2.log` · 日期 2026-10-25 · 第七十五会话

## 总表

| 编号 | 现象 | 根因（文件:行号） | 判类 | 一句话修法 | 真缺陷? |
|---|---|---|---|---|---|
| 1 | （前提）R10② 卡池里有 `Empyric Ambush SaimHann` → 找不到 | `Editor/BattleScene.cs:1704-1705` 用旧名查；卡池真名 = **`Empyric Ambush`**（faction SaimHann，id `dc7e575c0a328f446be1521c23ec850b`，`RuleEngine/Resources/cards_engine.json` 现读；HEAD 里也已是这个名字） | **α** | 改 `CardDatabase.FindById(poolR10, "dc7e575c0a328f446be1521c23ec850b")` | 否（断言过期） |
| 2 | ★ C6·Q7 立绘中心比 Full 低 0.021，实得 **-0.0105** | 量具错：`Editor/BattleScene.cs:5206` 拿 `Mesh.bounds.center`（= **被卡框画窗裁过**的四边形中心）当立绘中心；`Core/CardView.cs:9822-9827` 把上沿夹到画窗顶（`yT = min(yT0, win.yMax)`）⇒ 上沿两族同值、只有下沿动 0.021 ⇒ 中心只走 **一半** = 0.0105。族本身是对的（前置那条 `Face == Hand` 已绿）。**与今天 `Front(+0.08)` 无关**（`Front` 是父变换，`Mesh.bounds` 是网格局部空间） | **α** | 改比**未裁**的量：断 `ArtFamilyDy`（或 `_art` 网格**底边**差值）/换一张不被画窗上沿裁到的卡 | 否（量具/断言过期） |
| 3-10 | 主动技能 8 条全红（选择器里有该项 / 进选目标 / 定打法 / 弹面板 / 点敌方 / 面板铺白 / 6→6 / 花掉行动） | **同一根因**：`Battle/AttackSelector.cs:1289` 今天把门写成 `o.Army != 0`，而 `Battle/BattleDriver.cs:13818` 的 `Army = ArmyIcons.Id(u.Card.Faction)` 对自研两阵营 `Ember`/`Tide` 回 **-1**（`Core/ArmyIcons.cs:120-124`）⇒ 进 `1289` → `1298` `o.Army > 0 ? … : null` → tex=null → `Hidden=true; Enabled=false` ⇒ `HasCommand(Ability)` false ⇒ `SimulateUseAbility` 在 `BattleDriver.cs:30330` 直接回 `ErrNoAbility`。**HEAD 的门是 `o.Army > 0`，且 `BattleDriver.cs:13815` 注释明写「-1 ⇒ 落回旧口径」——实现与自己的规格相反** | **γ** | `AttackSelector.cs:1289` 改回 **`o.Army > 0`**（一句） | **是（真缺陷）** |
| 11 | `Begin` 后提示条「你的回合」为空串 | `Editor/BattleScene.cs:15653` 把 `Step(0.3f)` 改成 `Step(TurnHeadsUpHold + 0.3f)` = 1.8 s > `TipDuration` 1.7 s（`BattleDriver.cs:1880`）⇒ 横幅已自动关闭；`HoldForTurnHeadsUp` 只推时间线队尾+开阻塞窗，**不延长横幅** | **γ** | 退回 `Step(0.3f)` | 否 |
| 12 | ★ T12：0.625 s 黑场 α≈0.5、运镜仍在播（α=-1.000 · t=2.125） | `Editor/BattleScene.cs:24009` `Step(TurnHeadsUpHold + 0.625f)` = 2.125 s ≫ `LoadingFadeSeconds` 1.25 s（`BattleDriver.cs:15224`） | **γ** | 退回 `Step(0.625f)` | 否 |
| 13 | ★ T12 重入前提：推到中途（离本位 > 1）失败 | `Editor/BattleScene.cs:24029` 同一句；t=2.125 已接近 clip 末段（本位定义在 2.5 s） | **γ** | 退回 `Step(0.625f)` | 否 |
| 14 | ★★ A1 判别式（灭自证）：只把 `clientFaction` 拨空 ⇒ 对手牌塌掉（31 vs 31） | 判别式**已失效**：今天新增「按录像头记的双方实际牌表重建」`BattleDriver.cs:1022-1028` + `TryExpandRecordedDecks`（`905-914`）+ `ReplayStore.cs:264-266` 新字段 ⇒ 对手牌由 `clientCardIds` 原样展开、不再按阵营过卡池 | **α** | 负控换成 `rec.clientCardIds = null`（或塞不存在的 id） | 否（生产行为是有意改的） |
| 15 | ★ A1（对照）：空阵营那趟确实报了「一张卡都没有」 | 同 14：空阵营报错来自老路 `StarterDeck/ResolveDeck`，新路根本不进 | **α** | 与 14 一起改 | 否 |
| 16 | ★ A964/E2⑥ 技能面板 Lights 中心/0.45 半宽点不在命中框里 | **3-10 的级联**：`Editor/BattleScene.cs:36060` `SimulateUseAbility` 被 `HasCommand(Ability)==false` 卡住 ⇒ 面板从未 `Show/RefreshLayout` | **γ** | 修 3-10 后自愈 | 是（同根因） |
| 17 | ★ B2·残余①：0.3 s 时仍在飞 | `Editor/BattleScene.cs:37822` `Step(TurnHeadsUpHold + 0.3f)` = 1.8 s ≫ 四段飞行 0.65 s | **γ** | 退回 `Step(0.3f)` | 否 |
| 18 | ★ B2·偏差8：落地段还在走（0.2→0.5 位移 0.000） | `Editor/BattleScene.cs:38024` 同一句 | **γ** | 退回 `Step(0.3f)` | 否 |
| 19 | ★ B2·偏差8：竞技场空间 0.5→0.6 位移 0.000 | 18 的续（采样时补间早已结束） | **γ** | 随 18 修 | 否 |
| 20 | ★ C1-B01：落地那拍绿光按真值重推（实得可见 True） | 18 的级联（`38049` 把请求拨回 true，`38053` 的 while 因 cue 已清而不推） | **γ** | 随 18 修 | 否 |
| 21 | ★ C5（夹具）：池里有跨阵营**部队**卡 + 我方空格 | `Editor/BattleScene.cs:39051` 用 `c.Type == "troop"`；`CardDef.Type` 只有 **`unit`/`tactic`/`hero`/`defence`**（`"troop"` 0 条） | **δ** | 改 `c.Type == "unit"` 且排除督军 | 否（夹具场景造不出） |

## 必须先改的（β/γ）排序
1. **P0 · `Battle/AttackSelector.cs:1289` `o.Army != 0` → `o.Army > 0`** —— 生产代码，一行，**一次修掉 9 条**（3-10 + 16）。
2. **P1 · 把「按墙钟采样的 `Step`」从 `TurnHeadsUpHold + x` 收回原值** —— 一次修掉 **7 条**，连带 2 条自愈：`:15653`（→0.3）· `:24009`/`:24029`（→0.625）· `:37822`/`:38024`（→0.3）。
   ⚠️ 系统性风险：本批共 **51 处** `+TurnHeadsUpHold`（`0.3`）+ 2 处 `+0.625` + 2 处 `+0.05`。凡「推完立刻读墙钟量」或「取飞行中途快照」的都必须回退；凡「等时间线 cue 收尾」的才该保留。
3. **P2 · 5 条测试侧过期（α/δ）**：`:1704`（卡名）· `:5206`（裁过的网格当立绘中心）· `:29355-29361`（A1 负控换杠杆）· `:39051`（`troop`→`unit`）。

## 摘要
- 21 条：**α 4**（#1、#2、#14、#15）· **β 0** · **γ 16**（#3-10 = 1 个根因 9 条含 #16；#11；#12-13；#17-19；#20）· **δ 1**（#21）。
- 真缺陷只有 **1 个根因**：`AttackSelector.cs:1289` 的 `Army != 0`。
- #2 与 `CardView.Front(+0.08)` 无关（网格局部量），`-0.0105` 是「上沿被画窗裁住、只有下沿动」的精确一半签名。
