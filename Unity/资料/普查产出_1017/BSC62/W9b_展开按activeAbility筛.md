# W9b · 展开按 `activeAbility` 筛（主对话裁决后的改）

> 执行写手 W9 · 2026-10-17 · 起因 = 主对话裁决「**`EvtKind.Ability` ↔ 该卡的 `activeAbility`，⛔ 不是全部 `cardAbilities`**」。
> ⛔ 本轮**没跑 Unity 自检**；只跑了**秒级类型检查**。⛔ 未动 git / 未改正本。本报告**取代** `W9_T05第三步.md` 里
> 「按全表展开 / 850 卡 · 1371 拍」那几处（那份已就地打横幅指向本件）。

## 一、判别据是什么（带出处 · 可逐卡复核）

**判别据 = 全量件 `Unity/数据/游戏数据/card_anim_order.json` 里 `cards[].abilities[].path` 这个字段**
（值只有两种：`"activeAbility"` / `"cardAbilities"`）。

**它为什么可信（先证后改，两步）**：

1. **它本身就是真值源的字段名**。真值源 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
   （1,195 张卡的 SO 逐字段 dump）。现读每张卡两个**并列且不同**的字段（Heldrake 那张，`SO[0]`）：
   · `activeAbility` —— 一个**对象**，里面是 `activeAbilityLogic[]`（list）× 每项的 `cardAnimations[]`；
   · `cardAbilities` —— 一个**列表**，每项带 `trigger`（`0 = ThisCardPlayed` …）。
   全量件的 `abilities[].path` 就是照这两个字段名命名的，⛔ 不是拍的。
2. ✅🔴 **逐卡复核：897/897 精确相等**（两个方向各跑一遍，见下 §三 的脚本）：

| 比法 | 结果 |
|---|---|
| `path == "activeAbility"` 的 `steps[].ca` ⟷ SO `activeAbility.activeAbilityLogic[].cardAnimations[].m_AssetGUID`（按序） | **897 张卡逐张逐 GUID 相同**（0 失配） |
| `path == "cardAbilities"` 的 `steps[].ca` ⟷ SO `cardAbilities[].abilityLogic[].cardAnimations[].m_AssetGUID`（多重集） | **897 张卡相同**（0 失配） |

⇒ 排除了「`path` 标反了 / 是生成器拍的」这两种可能。**判据可逐卡复核**（任意一张卡的 `path` 与 SO 的两个字段名对一眼即可）。

**附一条自洽性**：全量件里 `path == "activeAbility"` **⟺ `trigger == null`**、`path == "cardAbilities"` **⟺ `trigger != null`**，
**0 行不自洽**（现读）—— 两条独立字段互为旁证。

**旁证（本仓自己的记录，主对话裁决时引的同一处）**：`Core/EventTiming.cs` 的 `SourceOf(EvtKind.Ability)`
写着「该卡 **`activeAbility`** 各 CardAnim 的 `GetAnimDuration()` 之和」；`Core/AbilityAnimDurations.cs` 文件头写明
「`cardAbilities[*]`（`ThisCardPlayed` / `Slay` / …）是**别的时机**播的 ⇒ 归 `EvtKind.Trigger` / `Play`」。

## 二、改了什么

| 搜哪一串 / 文件 | 改前 → 改后 |
|---|---|
| `工具/gen_card_anim_order.py` 的 `build()` 里 `for ab in c.get("abilities") or []:` | **插一行门**：`if (ab.get("path") or "") != "activeAbility": stats["rows_not_active"] += 1; stats["steps_not_active"] += len(...); continue` |
| 同文件 `stats` / 输出 `note` | 加 `rows_not_active` / `steps_not_active` 两个计数；输出加 `"filter": "abilities[].path == 'activeAbility'"` |
| 同文件文件头 | 加「**为什么只取 `activeAbility`**」一节（判据 + 897/897 复核，见上 §一） |
| `Resources/card_anim_order.json` | **重新生成**（产物，⛔ 没手改） |
| `Battle/CardAnimOrder.cs` 文件头 + `TryGetBeats` 的出声文案 | 「按行序拼全表所有行」→「**只取 `activeAbility`**」；命中面数字全部更新；出声的「原因三种」重写（① 只有 `cardAbilities` —— **绝大多数卡，不是缺陷** ② 那一拍 `ca` 空串 ③ 不在快照/卡池） |
| `Battle/BattleDriver.cs` | **没动**（展开/消费那一族与表内容无关 —— 张数骤降**不需要**改代码：`TryGetBeats` 回 0 ⇒ 走原样一拍，已有出声 + 计数口） |

## 三、复核脚本（可复现）

```python
# 读 SO（只读）与全量件，逐卡比 GUID
byuid = {c['uniqueId']: c for c in json.load(open(SO))}
# SO 侧：activeAbility.activeAbilityLogic[].cardAnimations[].m_AssetGUID
# 全量件侧：abilities[] 里 path=='activeAbility' 的那几行，steps 按 i 升序
# ⇒ 897 张卡（全量件 cards[] 的每一张）逐张相等；cardAbilities 侧同样 897/897。
```

## 四、新张数 / 拍数（与旧值对比）

| | 旧（全表展开，已作废） | **新（只取 `activeAbility`）** | 降幅 |
|---|---|---|---|
| 卡数 | 850 | **63** | −787 |
| 拍数 | 1371 | **100** | −1271 |
| 表大小 | 367,511 B | **27,220 B** | −93% |
| 剔掉的行 / 拍 | —— | `rows_not_active 1003` / `steps_not_active 1284` | —— |
| 空 GUID（原版没绑） | 97 | **84**（全在 `activeAbility` 行里） | —— |
| 纯延时拍（`vfx` 空） | 14 | **0**（那 4 个 `Wait_*` 都挂在 `cardAbilities` 行上，已剔） | —— |

**✅ 交叉印证**：新表 **63 卡 / 100 拍**，与既有的 `Resources/ability_anim_durations.json`（**62 卡 / 99 条 CardAnim**，
同一个 `activeAbility` 口径、另一条生成链）**几乎逐一对上**（差 1 卡 1 拍 = 一张卡的卡名写法不同）。

**命中面（现读）**：真正会发 `EvtKind.Ability` 的那一族 = `Duty`/`Pray`/`Agenda`/`Ferocity`/`Oath` 共 **85 张**，
其中 **50 张**在新表里 ⇒ 那 50 张按表展开 N 拍，其余 35 张 + 全池别的一律**走原样（一拍）**（出声 + 计数）。

## 五、幂等

`python -I -X utf8 工具/gen_card_anim_order.py --check` ⇒ **[check] 幂等 = True | cards = 63 | steps = 100**（连跑两次逐字节相同）。
`python -I -X utf8 工具/gen_card_anim_order.py` ⇒ `stats = {'blank_guid': 84, 'unresolved_guid': 0, 'pure_wait': 0, 'cards': 63, 'steps': 100, 'cards_no_steps': 834, 'rows_not_active': 1003, 'steps_not_active': 1284}`。

## 六、类型检查

`TMPDIR=/tmp/wf_w9 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（生成前后各跑一遍）。

## 七、有没有被迫收窄 —— **没有**

拿到了**能逐卡复核的判别据**（`abilities[].path`，且与真值源 SO 的两个字段名 **897/897** 对上）⇒
**不需要动安全阀**：不是「拿不准的一律走原样」，而是**精确地**只取 `activeAbility`。

⛔ 仍然留着的那一处近似（**与本次裁决无关**，属原设计）：**同一张卡有多条 `activeAbility` 行**时（如
`Leman Russ Vanquisher` 有 2 行 3 拍）**按行序全部保留** —— 我们的事件分不出是哪一条 `AbilityLogic`；
这是**如实聚合**、不是猜（原版的遍历单位就是 `AbilityLogic` 实例）。

## 八、顺手发现（⛔ 没动手）

1. 🔴 **运行期命中面：63 张里按【卡名】命中 57 张**（`BattleEvent.CardId` = `CardDef.Name`，已核
   `UnitState.cs:579` 的 `Name = card != null ? card.Name : "?"` ⇒ 就是 `CardDef.Name`）**⇒ 有 6 张按名命不中**，
   会**走原样一拍**（出声；**宁可少播**）。按 id 能命中 62、按 id-或-名 **63/63**（`cards_engine.json` 1127 张现读）。
   那 6 张（现读逐张）：
   | 表里的 `name`（SO `cardName`） | 表里 `id` | 我们池里同 id 的 `name` |
   |---|---|---|
   | `Asmodai, Master Interrogator` | `DA3` | `Asmodai` |
   | `Azrael, Grand Master` | `DA1` | `Azrael` |
   | `Iconward Malak Vorenth` | `GSC3` | `Acolyte Iconward` |
   | `Shock Trooper Sgt.` | `AM11` | `Shock Trooper Sgt`（**只差一个句点**） |
   | `Tempestor Prime` | `AM66` | `Hektor Thenmann` ⚠️ |
   | `Shanus Daskovian` | `GSC66` | `Lhaska Szenari` ⚠️ |
   前 4 条看着是**同一张卡的长短名**；**后 2 条像 id↔名 对不上**（可能是我们的 `card_ids.json` 真值映射或 SO 的
   `cardUniqueId` 在那一格有问题）。🔴 **我【没有】加「按 id 补别名」** —— 因为在后 2 条那种情况下，
   按 id 补别名会把**另一张卡的动画**挂到这张卡上 = **错播**（静默的错误演示）。
   ⇒ **另开一条账**：核这几张的 id↔名（`Data/游戏数据/card_ids.json` + SO `uniqueId`）。在此之前，**按名不中就走原样**（安全方向）。
2. `84 条 activeAbility 的 `ca` 已空串`（原版没绑 CardAnim）= **本表覆盖率的真正上限**：
   有 `activeAbility` 的 **105 张**卡里，只有 **63 张**至少有一拍解得出参数 ⇒ 另 **42 张**原版就没绑（**不是我们缺**）。
