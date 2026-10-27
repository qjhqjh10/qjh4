# B行逐条核_R1（第三段：尾部 74 条）—— R1T3A2 报告

> 任务：继续核 B 行**剩下的尾部**。产出 8 列结论表 + 11 列补丁。
> ⚠️ **口径差异（重要）**：开工时主表 `状态=B` 的只有 **405 行**（上一批开工时是 422）—— 期间有并发写手把 17 行改了状态。按「剩下的尾部」取，本批覆盖 **当前 B-ordinal 332→405**（= 331 之后到末尾全部），共 **74 行**（不是 91）。边界方法已写死供合并核对：**起点 `MinionManager.AreMinionsWithAmbush()`，终点 `EntityScript.get_displaySummonSickness()`**。
> ⚠️ `判据/B行逐条核_R2_后半.tsv` **不存在**（R2T3B 未产出）⇒ 无「已核过就跳过」可做；若 R2T3B 之后覆盖到本段，请在合并时去重。

## 产出
- `判据/B行逐条核_R1_第三段.tsv`（8 列，74 行）
- `账/_parts/B9_R1T3A2.tsv`（11 列补丁，74 行）
- 本报告

## ① 核了多少 / 建议分布
- **74 条**（B-ordinal 332–405）。
- **建议：B 73 · 判据缺 1（R350 `GetWhispersCardsCount`） · A 0 · 落点失效 0**。
- 按本轮新裁定：**落点是「字段 / 无落点 / 注释行」的一律记 B**（不再建议升 A），已在备注写「字段级/结构等价」。本段绝大多数正是这一类（MinionManager 几何、PlayerHand、RawCardScript 本地化族、Photon TurnExtensions 等）。

## ② 最有价值的发现
1. **`GetWhispersCardsCount`（R350）= 判据缺**：`decomp_full/PlayerHand__GetWhispersCardsCount.c` 内容其实是 `<>f__AnonymousType31__GetHashCode(){return 0;}` —— **文件名与内容不符**（已知 16.5% 撞名），不可据此判。补判据走 VA 反汇编或 cpp2il 同名方法。
2. **一批「原记不适用、实为 B」的 J12DK 订正**（本段大多是它们）：`GetCardInPos`/`PositionWillBeAtLeft`/`RefreshOccupationSlots`/`GetCardInPos(PlayerHand)`/`IsRawCardInHand`/`NumCardsInHand`/`GetCardNameTerm`/`GetLocalized*` 全族/`PreloadAssets`/`GetFinishedTurn`/`SetFinishedTurn` —— 原判 D 的理由是「移交 / 不适用」，**不是《方案》§4.1 的任何一种 D 理由**，且有**活调用点**。本批确认维持 B（字段级/结构等价，未逐方法核）。
3. **`GetOriginalId`（R374）旧落点是错误等价**：`LegacyCardIds` 解决「旧卡号→新卡号」的存档迁移，原版 `GetOriginalId` 解决「异画取本体 id」；我们异画不建档 ⇒ 本地无对象 ⇒ 无落点（⛔ 别再用 LegacyCardIds 顶）。
4. **`ArenaSlots`/`BoardSlots` 几何链已逐字对上**（R333/R335/R346）：`Awake`/`FillMinionPositions` 公式 18/18 格 Δ≤2.5e-7；**敌方镜像已去掉**（`ArenaSlots.Position` 敌槽 0 x 由 +6.25 改回 −6.25，R335），`ExtraYOnBoard` 已并入 `Position` 的 y（R346，13 场常数 0 ⇒ 恒等）。这些**几何本身等价**，保持 B 只因「SO 真值取不到 / cardShadow 无对应 / §4.1 / 无非自证读数」。
5. **洗牌机制真差异（R391/392/393）**：原版是「按名次挑选 + `MatchData` 预烧 100 整数按下标取」，我们是 **Fisher-Yates + ctx.Rng**；同种子排列不同。钉位（`stratagemList`）池内全 0 不可达。与「预烧 100 + 下标」那条口径同源，做 `RandomizedOnResolution(90)` 时要一起换。
6. **`GetCardListFromIds`（R390）null 洞形状差**：原版结果表**保留 null 占位**（下标对齐输入），我们单值 + 调用方跳过 ⇒ 列表长度可能不同；接 bot 牌库装配时要先定「对齐 vs 跳过」。
7. **`TriggerAbilities`（R387）仍是本族最大一块**：272 行只读了签名；605 那族已按组配对，其它 trigger 是否也有多条未核。`TriggerAbility`（R388）116 行同理未逐句。
8. **`GetActiveAbility`/`ResolveActiveAbility`（R364/R386）**：选支那句逐句等价（17 行）；唯一差异是原版 `MeetsCriteria` 两态、我们三态 `null`（判不了就出声）；`ResolveActiveAbility` 的「动画先于结算」次序可能不同（引擎不可观测）。
9. **联机回合同步机制不同（R397/398/399）**：原版把 Turn/TStart/FToA 写进 **Photon Room customProperties**（且只被示例 `PunTurnManager` 用、本作 0 消费），我们走**确定性重放 + 动作流 seq** ⇒ 等价物存在、机制不同，按 §4.1 记 B。
10. **`get_displaySummonSickness`（R405）**：光环重算周期内延到末尾、只在实际丢豁免时重算（避免 `Recompose` 中途停在中间态被误判疲劳）；原版是逐跳即时改写 —— 已修 `RE1–RE4` 回归，`play check` 指纹逐字不变。

## ③ 没查清的部分（照实说）
- **未逐句的大方法**（沿用主表判断）：`TriggerAbilities`(R387,272行)、`TriggerAbility`(R388,116行)、`OnCardPlayedWithTarget`(R380,120行)、`GetTransformedKnightId`(R376,50行只读前段)、`Shuffle*`(R391-393 主体已读但细节未逐支)。
- **判据不可读**：R350（文件名与内容不符）；R400 `UpdateName` 方法体在场但拼串段失真（`num6=default`）。R339 满盘回退「回督军锚点」我们无等价物。
- **本地化全族（R365/367-373/384）**：主表判据只写「本地化层直接落文本」，我用 grep 确认我们确有 `Loc.T`/官方词条表，但**没有逐方法核**「term 拼接规则 / 带换行 / 去 Siege / 语言覆盖」各档是否逐一复刻 —— 这是本地化线的一批账，本批只标 B 不改。
- **§4.1 策略**：R346/R397/R399 等因「同落点覆盖多方法 / 无落点」按策略记 B，语义其实等价，请调度台决定是否保留。

## ④ 顺手发现
- **`Shuffle` 落点行漂移**：主表写 `RuleCore.cs:1184`，现读实为 **`RuleCore.cs:1192`**（`static void Shuffle<T>`）。同类漂移在 `EndMulligan`（grep 只命中注释，方法体需现读定位）—— 建议这几个落点行由调度台在同步点统一重钉。
- **主表 B 行数 422→405** 的变化发生在两批之间：有 17 行被并发改状态。建议同步点核对「谁改的、改成了什么」，避免本批/上一批的 ordinal 与旧派活单对不上（本报告已用**方法名**而非 ordinal 固定边界）。
- **`PlayerHand` 全族（R347-361）** 与 **`RawCardScript` 本地化全族（R365-373）** 大多是「我们有本地化/换牌实现、但形态与落点不是方法级」，属**同一类结构差异**，可考虑合并成 2 条总账，而不是逐方法挂 B（减少噪声）。

## 覆盖边界与下一批
- 本段 = 当前 B-ordinal 332→405（**已到主表末尾**），边界方法：`AreMinionsWithAmbush` … `get_displaySummonSickness`。
- 当前主表已无 B-ordinal >405 的行 ⇒ **B 表的「尾部」到此核完**。中间 166–331 由 R2T3B 负责（其文件尚未出现）。
