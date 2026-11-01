# B116_W33 · `B-324`：`ruleprobe.LoadPool` 不挂 `cardTraits`

执行代理 `B116_W33` · 2026-11-02 · 只改 `工具/ruleprobe/Program.cs`

## 一、改动（2 处代码 + 1 条新哨兵）

| 位置 | 改了什么 |
|---|---|
| `Program.cs:1317`（**同一物理行追加**，行号不变） | `AttachTriggerAbilityAndOpsByNumber(__def, c, poolVersion); **__def.AttachCardTraits(ProbeCardTraits(c));**` —— 与引擎 `RuleEngine/Data/CardDatabase.cs:1734` 同一句、同一个方法 |
| `Program.cs` 类尾 `ProbeCardTraits(JsonElement)` | 卡池 `cardTraits` 列 → `string[]`。**三态与引擎逐字相同**：缺键 / 非数组 ⇒ `null`（不挂 ⇒ 判不了）· 键在且是数组 ⇒ 逐项收（**空数组 = 明确没有**）· 非字符串元素**出声并跳过** |
| `Sentinel.Run` ⑨（`Program.cs:2619`） | 新哨兵：① 全池 `CardTraitsKnown` 必须 = 池张数；② 拿池里第一张带 `Ephemeral` 的卡走**产品代码** `TargetCriteriaEval.RawCardMeetsTraitCriteria`，`traitsFilter=5,exclude=0` 必须 `true`、`exclude=1` 必须 `false`、两次 `gap` 全空 |

- 行数：2,716 → 2,818（+102，**全部在类尾 / 哨兵段**；调用点零行数变化）。行尾 **LF · 裸 CR 0**。
- **判别力已实测**：把 `:1317` 那一句去掉再跑 `check` ⇒ `✗ [cardTraits] 池内 CardTraitsKnown 只有 **0/1129** 张` · 哨兵失败 1 · `rc=1`；恢复后 `✓` · `rc=0`。

## 二、前后读数

| 闸 | 改前 | 改后 |
|---|---|---|
| `ruleprobe check`（解析层） | `rc=0` · 全池 1121 · **解析差异 0 行** · 哨兵失败 0 · 输出指纹 `ebdac779…`（与钉住一致） | `rc=0` · **解析差异 0 行** · 哨兵失败 0（含新 ⑨ ✓）· 输出指纹**仍是 `ebdac779…`** |
| `ruleprobe play check`（结算层） | `rc=0` · 主档 **0** 张变 · 电池 **0** 张变 · 结算指纹 `a8a2a218…`（= 基线） | `rc=1` · 主档 **4** 张变 · 电池 **4** 张变 · 结算指纹 `3decaacb…` |
| `play all dump` 逐卡对拍（26 列） | — | **粗列变了 3 张**（`UM110`/`UM111`/`UM118`）；第 4 张 `8482077f…` 粗列全同、只有 `changesFull`（目标不同）变 ⇒ 以 `play check` 的 21 列对拍为准 = **4 张** |

**解析层为什么 0 变化**：`RawCardMeetsTraitCriteria` 的消费方只有 `CardPoolCriteriaFilter`（出牌/检索时的候选卡筛选）与 `CountEval`，**都不进 `scan` 的 dump** ⇒ 这条修复只动结算层。

## 三、影响面逐卡清单（4 张，全部是「平 / 誓二选一」战术卡）

四张的卡池形状**完全一致**：`choicesTargetsAffected=180` + `choicesSpecificCardIds` 正好 2 个 + `cardTraits` 里有 `oath N`。
改前 `CardTraitsKnown == false` ⇒ `EffectResolver.OathChoiceNeeded` 恒假 ⇒ `DecideOathChoice` 直接 `return true` = **无条件付誓约**（原版不是这样）；改后走真掷骰（`RollOathPay`，消耗**恰好 1 格预烧随机数**，原版 `BattleManager__GetChoiceOptions`）。

| 卡 | 名字 | 变了哪一格 | 为什么 |
|---|---|---|---|
| `UM110` | Fall Back（1 费 · Oath 2） | 能量 **-3 → -1** · `mech 3→2` · `other 2→3` · `handCostDelta -1→0` · `changes 6→5` | 掷到**平版** ⇒ 不付 2 点誓约费、不结算 `Oath 2: Lower its cost by 2 and give it Flank`（降费/Flank 未发） |
| `UM111` | Shall Know No Fear（2 费 · Oath 8） | 能量 **-10 → -2** · `other 1→2` | 掷到平版 ⇒ 不付 8 点、`Oath 8: +8/+8` 未发（只剩 Invulnerable） |
| `UM118` | Indomitus Crusade（6 费 · Oath 6） | 能量 **-12 → -6** · 部署出来的兵 `UM101 5/6/1/6` → `UM30 6/10/0/12` · 疲劳位多 `P0[2]=1` | 掷到平版（少付 6）＋ **预烧随机游标后移一格** ⇒ 后续随机取兵换了另一张 |
| `8482077f309b08943b190f18b5874871` | Catechism of Death（0 费 · Oath 3） | 目标 `FIX_A 2→6` → **`WARLORD 2→6`**（同样 +4 攻）· 能量仍 -3 | 这次掷到**付**（能量不变）；但那 1 格消耗让 `or to your Warlord this turn` 那条 `or` 的随机游标后移一格 ⇒ 改给督军 |

**为什么两次变化混在一起（如实记）**：`RollOathPay` 是原版就有的**每次消耗 1 格**（`EffectResolver.cs:18094` 判据块），改前因为「判不了」整条没跑 ⇒ 既少一次语义判定，也**少烧一格随机数**。所以改后同时出现 ①「誓约付/不付」变真 ②「同一张卡后续随机挑选」位移 —— 两者都是**回到原版**，不是新偏差。

## 四、影响面的边界（潜在 vs 实测）

- 池 v47 / 1,129 张：**每张都有 `cardTraits` 键**（**696 非空 / 433 空**）⇒ 改后 `CardTraitsKnown` = **1129/1129**。433 张的效果 = 「判不了」→「**明确没有 trait**」（这本身就是行为变化，只是今天没有消费点因此可见）。
- **「平/誓二选一」形状的卡 = 10 张**（`UM109` `UM110` `UM111` `UM112` `UM115` `UM116` `UM117` `UM118` `4de4fcfb…` `8482077f…`）；**实测只有 4 张读数变化**，另 6 张掷到「付」⇒ 与改前逐列相同。
- **卡面级 `traitsFilter > 0` 的卡 = 6 张**（`ASH70` `BL50` `SAU56` `SW39` `TAU41` `UM39`，全部 `excludeTrait=1`）：它们的 `MatchesRawCard` 从「判不了 ⇒ 整条不筛」变成**真筛**；**今天的 `play` fixture 读数未变**（这几张的候选走 `UnitState` 那条 `TraitPasses`，不吃 `CardDef.cardTraits`）⇒ 属「机制恢复、暂无可见差」。
- `abilityTargetCriteria` / `activeAbilityTargets` / `choicesDefinition` 里 `traitsFilter > 0` 的行 **66 条** —— 走棋盘 `UnitState.Has`，与本列无关。

## 五、重钉建议（本代理**没有**动基线 / `Pinned*Md5`）

1. **`play` 结算层：建议重钉**。4 张差异逐条归因到「原版就该这样」（改前是 `OathChoiceNeeded` 恒假的静默错），没有一条指向新缺陷。重钉要动的两样：`基线/out_play_baseline.txt` + `PinnedPlayMd5`（新值 `3decaacbc96b524d04e4b41b9e8fd9e8`）。⚠️ 重钉窗口必须**没有 `.cs`/账写手**。
2. **`check` 解析层：⛔ 不用重钉**（解析差异 0 · 输出指纹 `ebdac779…` 一字未变）。
3. 建议顺序：先合入本件 → 再重钉 `play` → 之后任何 `.cs` 改动都要重跑 `play check` 对比新基线。

## 六、三道闸

| 闸 | 读数 |
|---|---|
| `bash 工具/typecheck.sh` | **运行时错误数 1 · 编辑器错误数 9**，**全部落在别人的在飞文件**：`Assets/RuleEngine/Data/CardDatabasePartial_B116W32.cs:59`（写手 `W32`）· `Assets/RuleEngine/Editor/B116_W31_Assert.cs`（写手 `W31`）。本件改的 `工具/ruleprobe/Program.cs` **不进 Unity 程序集**（`Assets/` 之外）⇒ 它由 `dotnet build` 把关，**build 无一条 error**；本轮曾 2 次 `ruleprobe.sh` 构建失败，都是**别人占着 `bin/Debug/net8.0/wfprobe.dll`**（`MSB3021/3027`，并发跑探针）或**别人在飞文件编不过**，重跑即过 |
| `python -I -X utf8 工具/check_ledger_points.py` | **漂移 0 · 失败 0**（A 5,831 全通过；B 档 4 条只报告） |
| 行尾 / 行数 | `Program.cs` LF · 裸 CR 0 · 2,716 → 2,818 行（新增全在类尾/哨兵段；`:1317` 行号不变） |

## 七、留着的话

- 探针跑并发会撞 `bin/Debug/net8.0/wfprobe.dll` 文件锁（`MSB3021/3027`）⇒ **同一时刻只跑一个 `ruleprobe.sh`**（本轮踩到一次，重跑即过）；这也是为什么前后读数是一前一后串行取的。
- 测量产物留在 `_tmp_view/b116w33/`（`scan_before.txt` · `play_before/after.tsv` · `cards_before/after2.txt` · `Program_after.cs`），可复核后清理。
