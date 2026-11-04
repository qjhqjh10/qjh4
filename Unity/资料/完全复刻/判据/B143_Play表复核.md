# `B143-Play表复核` —— 独立核 `Resources/card_anim_order_play.json`

> 只读复核 · 2026-11-05 · 执行代理 `B143-W7` · **未改任何文件、未跑 Unity**。
> 被核对象 = `Unity/MyGame/Assets/CardPresentation/Resources/card_anim_order_play.json`（`B143-W1` 新建）。
> 数据源 = `Unity/数据/游戏数据/card_anim_order.json`（全量件，928 卡 / 551 个 `cardAnims`）＋ `RuleEngine/Resources/cards_engine.json`（池 1142）。

## 〇、结论

**✅ 表正确** —— 总数、行集合、逐拍字段**全部对上**；只有 **1 拍**（`ASH85`）的 `ease` 编码有差异，而那一拍 `shouldMoveVFX = 0` ⇒ **该曲线本来就不参与演出**，且**触发表对同一拍的编码完全相同** ⇒ **不是缺陷**。

---

## ① 总数重算（从全量件自己算，再与 `stats` 逐项对）

口径 = `cards[].abilities[]` 里 `path == 'cardAbilities'` 且 `trigger == 0`，且**卡在池内**（`cards_engine.json` 的 1142 张）。

| `stats` 项 | 表里的值 | 我重算 | 对不对 |
|---|---:|---:|---|
| `cards` | 475 | **475**（池内含 trigger0 行的卡） | ✅ |
| `rows` | 579 | **579**（全量件里 pool 内的 `cardAbilities + trigger 0` 行） | ✅ |
| `steps_raw` | 703 | **703**（含空 `ca` 拍） | ✅ |
| `steps` | 697 | **697**（剔除空 `ca` 后） | ✅ |
| `blank_guid` | 6 | **6**（703 − 697） | ✅ |
| `unresolved_guid` | 0 | **0**（`cardAnims` 551 条、guid **0 重复** ⇒ 一拍都能解） | ✅ |
| `pure_wait` | 9 | **9**（解析后 `vfx` 为空串的拍） | ✅ |
| `movers` | 138 | **138**（`shouldMoveVFX == 1`） | ✅ |
| `offset_beats` | 12 | **12**（`applyMovementOffsets == 1`，且表里这 12 拍**正好**带 `ao/ont/ofx/ofy/ofz/mnx/mxx/mny/mxy/mnz/mxz`） | ✅ |
| `cards_with_trigger0` | 475 | **475** | ✅ |
| `cards_no_rows` | 359 | **359**（池∩全量件 = 834；834 − 475 = 359） | ✅ |
| `not_in_pool` | 94 | **94**（全量件 928 卡 − 交集 834） | ✅ |

**行集合**：PLAY 的 `(卡id, ai, li)` 与「全量件里 pool 内的 trigger0 行」**双向差集都是 0**。
**逐行步数**：579 行里**没有一行**的步数与「剔除空 `ca` 后」不一致；**4 行的 steps 为空表**（全部是「整行只有空 `ca`」那 4 行，符合 `filter` 里写的口径）。

## ② 抽样逐拍（比要求的 5 张更狠：**697 拍全比 13 个字段**）

字段映射（表用短名）：`ca`=step.ca · `vfx`=`cardAnims[ca].vfx` · `sp/ep/pt/vd/ts/tm/te/w/mv`=`params.{startPosOption,endPosOption,pointTowardsOption,vfxDelayTime,timeAtStartPos,timeMoving,timeAtEndPos,waitAnimToApplyEffect,shouldMoveVFX}` · `otv`=`animInfo.orientToVelocity` · `ao`=`animInfo.applyMovementOffsets` · `ont`=`animInfo.offsetMovementAxis.useNonModifiedAnimTime`。

| 抽样卡 | 行数 / 拍数 | 结果 |
|---|---|---|
| `EC29` Heldrake | 1 / 1 | ✅ 逐拍一致（`vfx='EC Heldrake Rally'` · `sp=ep=4` · `vd=0.4` · `tm=2.75`） |
| `SAU46` Spyder Nest | 3 / 3 | ✅ 逐拍一致（`GreenSummonCircle` ×2 + 1 个纯延时拍） |
| `AM76` Armoured Lightning | 4 / 6 | ✅ 逐拍一致（`GuardSummon` ×3 + ×3） |
| `SAU57` Tomb World（多拍） | 5 / 5 | ✅ |
| `SOR73` Sacred Rose（多拍） | 4 / 5 | ✅ |

**全量复核**（不限抽样）：697 拍 × 12 个数值/字符串字段（`ca/vfx/sp/ep/pt/vd/ts/tm/te/w/mv/otv`）**0 差异**；
唯二的两类「差」都是**编码约定**，不是数据错：

1. **`vfx` 的 `null → ""`**：全量件里那 9 拍 `cardAnim.vfx` 是 `null`，表里写 `""`（= 纯延时拍）。这正是 `stats.pure_wait = 9` 的来源 ⇒ ☑ 预期。
2. **`ease` 的三种编码**：源曲线在 697 拍里**全部是 2 点**；表里按 `[]`（559 拍）· `[0,0,1,1]`（135 拍）· 9 数字形 `[-1, t0,v0,in0,out0, t1,v1,in1,out1]`（3 拍，`GOF103` / `GOF95`×2，它们的斜率是 2.0/0.0 不是默认值）三种写法。
   **把三种都解码回 `(t, v)` 点对后逐拍比：697 拍里只有 1 拍不一致** —— 见 ③。

## ③ `blank = 6`：那 6 个空 `ca` 在全量件里真的存在

6 条（括号内 = 全量件的 `ai, li, i`）：

```
30007162b68992e41a422f2061705b65 (0,1,0) · TL64 (0,0,0) · TL64 (0,0,1)
SOR39 (1,0,0) · GSC74 (0,1,0) · ASH41 (0,0,1)
```

- 它们在**全量件里确实存在**（`ca == ""`），表里按 `PlayAnim:62-64` **从 `steps` 剔除**（`steps_raw 703 → steps 697`）✅；
- 其中 4 行是「整行只有空 `ca`」⇒ 表里保留该行、`steps: []`（符合 `filter` 的自述）✅；
- 另 2 行（`TL64` 的第 2 拍、`ASH41` 的第 2 拍）混在正常拍里，只剔掉那一拍 ✅。

## ④ 与触发表的「同源」—— **是真的同一批，不是两个恰好相等的数**

| 比较 | 结果 |
|---|---|
| `TRIG.stats.this_card_played_rows` | **579** |
| `TRIG` 里 `trigger == 0` 的 `(id, ai, li)` 集合 | **579 条** |
| `PLAY` 的 `(id, ai, li)` 集合 | **579 条** |
| 交集 / 只在 TRIG / 只在 PLAY | **579 / 0 / 0** |

🔴 **不只是数字相等：两边的 `(卡id, abilityIndex, logicIndex)` 三元组集合逐条相同、双向差集为 0** ⇒ 同源成立。
另：同一拍在两张表里的 `ease` 编码也一致（抽查 `ASH85` 那一拍，两表都是 `[]`）。

## ⑤ 唯一的一处差异（**判为不构成缺陷**）

| 位置 | 全量件原值 | 表里的值 | 判定 |
|---|---|---|---|
| `ASH85`（`Unholy Smite`）`ai=1, li=0, beat 0`（`ca=159573a0c979cf34d803c5a640a32014`，anim `Atk_UnholySmite_warlord`） | `easeCurve` = `(0, 1.0) → (1, 1.0)`（**常数 1**） | `ease: []`（= 被当成恒等 `(0,0)→(1,1)`） | **不影响**：该拍 `shouldMoveVFX = 0`（`mv: 0`）⇒ 这条曲线**本来就不参与位移**；且触发表对同一拍也是 `[]`（两表一致）。只剩「理论上曲线值不同」这一点，**记在这里备查**。 |

## ⑥ 检查方法（可复跑）

纯 Python（`json` 读三份 JSON + `cards_engine.json`），不跑 Unity。要点：
1. 从全量件按 `path/trigger` 建 `(id, ai, li) → steps` 索引，`ca` 用 `cardAnims[guid]` 解析出 12 个字段；
2. `ease` 三种编码统一解码成 `(t, v)` 点对再比；
3. `ao/ont` 缺省 = `0`（表只在为 1 时写）；`vfx` 的 `null` 与 `""` 视同。

## ⑦ 局限

- 只核了**数据一致性**，**没核**「这些字段在运行时怎么用」（`CardAnimOrder.TryGetPlay` 那一层）—— 不属本任务。
- `ease` 的 9 数字形解码是按 3 个样本归纳的（`[-1, t,v,in,out]×N`），**样本只有 3 拍**；若将来出现别的斜率组合，值得再抽一次验证。
