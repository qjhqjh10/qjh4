# AUD9 审查发现落账（2026-10-25 · 台账落账执行代理）

> 范围 = 用户 2026-10-25「确认审查出的问题已写进任务清单了」。只改 5 份 `任务总表.md` 的 §一；⛔ 未改 `.cs` / 判据文件 / `CLAUDE.md` / `项目任务.md`；⛔ 未跑 Unity / typecheck / git 写操作。备份 → `_tmp_view/table_bak_1025c/<线>/任务总表.md`；全程 Python 二进制读写；改后 5 份 CRLF 全 0；备份 vs 现读 = added 53 / deleted 0 / modified 0（`git diff --numstat` 里多出的行是本会话前就在工作树里的旧改动）。

## 逐线新增几条
| 文件 | 新增行 | 跳过（已存在） | 行尾 | 竖线 |
|---|---|---|---|---|
| 战斗线/任务总表.md | **19** | 3（AUD6 #11 准星跟手 · #13 阈值 · §四③ ClearSelection 均已修） | LF / CRLF 0 | 每行 6 根 ×19 |
| 引擎线/任务总表.md | **19** | 2（AUD7 `pray` 判定不改代码 · `huntMark` 名已修） | LF / CRLF 0 | ×19 |
| 界面线/任务总表.md | **8** | 2（X4 顺手① `m_PixelsPerUnitMultiplier` · 「当前战役」裁决，已在 §一 笔记块） | LF / CRLF 0 | ×8 |
| 卡牌线/任务总表.md | **3** | 1（X1 顺手② `CardFactionProbe` 已修） | LF / CRLF 0 | ×3 |
| 自检断言线/任务总表.md | **4** | 0 | LF / CRLF 0 | ×4 |

## 新增条目清单（任务名 — 索引）
战斗 | AUD6·手牌目标型卡「打出后点选目标」 — AUD6 §二#4
战斗 | AUD6·飞行目标的「先锋」近战豁免 — AUD6 §二#6
战斗 | AUD6·攻击合法性缺六道闸 — AUD6 §二#7
战斗 | AUD6·stealth/camo/ward 分侧口径收口 — AUD6 §二#8
战斗 | AUD6·打法枚举补 Spell=3 — AUD6 §二#15
战斗 | AUD6·友方候选档的「自己的打法」底光 — AUD6 §五 新发现1
战斗 | AUD6·抬手别被落场补间挡掉 — AUD6 §五 新发现3
战斗 | AUD6/X9·本轮新断言一次都没跑过（待 BattleScene.Run） — AUD6 §四⑥/§六 · X9 §6
战斗 | AUD8·友方／任意目标的主动能力点不到 — AUD8 §一#7 · §二Q1①
战斗 | AUD8·单位卡「落位后再点目标」整条缺 — AUD8 §一#5 · §二Q1②
战斗 | AUD8·非法出牌按因给提示（四档错文案） — AUD8 §一#17
战斗 | AUD8·选目标点到卡（含自己人）要出声 — AUD8 §一#16
战斗 | AUD8·战术卡拖空要出声 — AUD8 §一#3
战斗 | AUD8·手牌轻点开窗缺 UIstate 闸 — AUD8 §一#12
战斗 | AUD8·「点牌堆摊开牌库」是自造入口 — AUD8 §一#13
战斗 | AUD8·THIS UNIT CANNOT ACT 不是官方词条 — AUD8 §三 顺手2
战斗 | AUD8·被引擎拒后让位预览停在假位 — AUD8 §三 顺手3
战斗 | AUD8·模态窗清单收成一处 — AUD8 §三 顺手8
战斗 | AUD8·「ESC 取消选择」这句引用是错的 — AUD8 §三 顺手1
引擎 | AUD7·ward/wardWb/tainted/summonRift 漏登 Implemented — AUD7 §一#40~#42
引擎 | AUD7·sneakAttack 原版有徽标、我们没进白名单 — AUD7 §一#9
引擎 | AUD7·jam 语义只做了 3 处 — AUD7 §一#43
引擎 | AUD7·「有键无实现」词条欠账（约 22 + 44 条） — AUD7 只有引擎/两边都没有
引擎 | AUD7·RuleCore.TermResolver 那行表现层接线 — AUD7 §七·一
引擎 | AUD7·ferocity 中文「凶残 vs 狂暴」一次改齐 — AUD7 §七·一
引擎 | AUD7·swarm「并到左侧」可能是真缺陷 — AUD7 §八·六 B#4
引擎 | AUD7·companion 的「玩家选」要复核一次 — AUD7 §八·六 B#3
引擎 | AUD7·ephemeral 的去向缺反编译判据 — AUD7 §八·六 B#5
引擎 | AUD7·InPlayIconKeys 里 relentless/resolution 引擎零实现 — AUD7 顺手5
引擎 | AUD7·黑暗契约四变体与「关键词」不是同一层 — AUD7 §一#84
引擎 | AUD7·两处注释还写着旧口径 — AUD7 §八·七
引擎 | X3·ta=100 的「相邻里有人满足同一 criteria」那半 — X3 §四·1
引擎 | X3·「被闸拒 ⇒ 0 条 Trigger」还没有断言 — X3 §四·2
引擎 | X3·降费监听 BroadcastCostWhen 没有入队口 — X3 §四·3
引擎 | X3·E5 那 13 行判据齐的「接闸」活（FireTriggerAt/580/620） — X3 §四·4
引擎 | X6·Rally 与 tactic-body 那 8 行还是「判不了⇒放行」 — X6 §七
引擎 | X6·chooseone 折成单条 op ⇒ 族闸碰不到分支 — X6 §六·3
引擎 | X3/X6/AUD7·本轮新断言待跑 RuleEngineTest.Run — X3 §三·3
界面 | X2·成就剩余 4 个计数源（开包/战役通关/轮抽战绩/Ach_24） — X2 §六
界面 | X2·猎杀标记／黑暗契约的成就授予点 — X2 §六
界面 | X2·隐秘／破坏的成就计数 — X2 §六
界面 | X2·再造／暴群的成就计数 — X2 §六
界面 | X2·Ach_89／Ach_97 的过滤位判据不闭合 — X2 顺手1/2
界面 | X2·本批 8 组成就断言待跑 RuleEngineTest.Run — X2 §七·2
界面 | X4·设置窗版本号显示 v0.1.0 — X4 §④
界面 | X4·模式卡新断言待复跑 MainMenuScene.Run — X4 判据不足1
卡牌 | X1·效果文字溢出修复待验收（跑 CardBaseDemo.Run） — X1 §五
卡牌 | X1·「fit 没跑」的具体触发入口还没查清 — X1 §五
卡牌 | X1·CardFaceProbe 五档字号断言是算术自证 — X1 顺手①
自检 | X5·CollectionScene 3 红修复待验收 — X5 §6
自检 | X5/Dg5·「延后生成」链第 4 步为什么没落下网格 — Dg5 §5
自检 | X5/Dg5·469 格同帧激活可能是生成尖峰 — X5 §3 · Dg5 §3
自检 | X5/Dg5·若证实 469 格打断生成链，要修「激活时生成」 — Dg5 §4·2
## 我判「不该落」的（理由）
· **已修／非缺陷／已在 §一**：AUD6 #11/#13/§四③（已修）、AUD8 #4 跨侧落点与 #19 输入闸与 #18 回放拖拽（X9 已修，回放档含轻点正例）、AUD7 `pray +1 信仰`（判定不改代码）、`huntMark` 名（F1 已修）、X1 顺手②（已修）、X4 ②F6 左栏 5 键／③F7 标题／④半透明底板（原版本来如此或已修）、X4 顺手① 与「当前战役」裁决（已在 §一 笔记块）、X2 `Ach_21/22`（服务端事件、无本地判据 = 铁律 11 例外①）、X4 圆形金环徽章（本地确实没有那族图 = 例外①）。
· **属删账／订正而非新账**：AUD7 `penumbra` 查无此词 ⇒ 建议主对话从待办里**删掉**（本代理只加不删）；AUD6 §五 新发现2（`KindAtPoint` 的 lossyScale 兜底，报告自述「会红而不是静默错」）不构成待办；X3 §四·6 `ta=200/201` 已按口径不做且 `A2`/`E5-4` 判「加列也没人写」；X6 §六·1/§六·4 报告自述「没有消费者」。
