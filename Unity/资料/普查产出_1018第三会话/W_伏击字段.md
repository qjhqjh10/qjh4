# W_伏击字段 — `A985⑥①` 收口最后一块（「伏击」那一维）· 2026-10-18 第三会话

> 执行写手代理。**只动**这四个：`RuleEngine/Core/BattleEvent.cs` · `Core/RuleCore.cs` ·
> `Core/EffectResolver.cs` · `RuleEngine/Editor/RuleEngineTest.cs`。
> ⛔ 没跑 Unity · 没动 git · 没改正本 · 没碰白名单外的任何文件。
> ✅ 秒级类型检查（`TMPDIR=/tmp/wf_pa`，共 4 次）末次 **运行时 0 / 编辑器 0**。

### 加了什么字段（形状 + 判据）· 两个 emit 点各改了哪（按符号）· 判别式自己复核的结论

**字段**（`BattleEvent.cs`，加在 `TargetCardId` **之后** —— `Play` 的两维挨着）：`public bool PlayAmbush;`
doc 里落的判据：
- 原版 `CemeteryManager.GetActionText` 的 **case `10`**（= `Play`）= **6 档** = `2(靶向)+2(普通)+2(伏击)`；
- 「伏击」= **它自己的一个位**：**字节 `0x1C`**（`local_18` 取自 `param_2 + 0x18`，读 `local_18._4_1_`）；
  **不靶向**的那两支才看它，每档再按 `isPlayer`（`(char)*(param_2 + 6)` = 字节 `0x18` 首字节）各分两个；
- ⇒ **不是 `isPlayer` 的派生**、也不是「有没有目标」的派生 ⇒ 专用 `bool`；⛔ 没复用 `Effect`（字符串诊断位）。

**判别式复核结论：算式对**，而且它**本来就是**「面朝下下场」那一支的判别式（`RuleCore.PlayCard` 里置
`unit.FaceDown` 那句）。⇒ 没抄三遍，**提成函数** `static bool PlaysFaceDown(CardDef card)`
（定义在 `RuleCore.cs`、紧挨 `PlayCard` 上方），三处共用 —— 原来那一处也改成调它（行为零变化：
只多一个恒真的 `card != null` 守卫）。要回退只回退那一行即可（不影响任何断言）。

| emit 点（按符号） | 改了什么 |
|---|---|
| `RuleCore.PlayCard` 的 `ctx.Emit(EvtKind.Play, p, slot, card.Name)` | → `ctx.Emit(new BattleEvent { Kind/Player/Slot/CardId 照旧, PlayAmbush = PlaysFaceDown(card) })` |
| `EffectResolver.PlayTactic` 的 `ctx.Emit(EvtKind.Play, p, targetSlot, card.Name, targetPlayer:…, …)` | → 同上；**原来那三个靶向实参一字未动**，只多 `PlayAmbush` 一项 |

🔴 **为什么换成 `Emit(BattleEvent)` 重载**：按字段那个重载的**签名在 `BattleContext.cs`**，**它没有这个形参**，
而那个文件**不在本笔白名单** ⇒ 只能在发出点用对象重载。⛔ **也没用**「先 `Emit` 再回写 `Signals[^1]`」
那种写法（靠「`Emit` 不复制对象」的隐含前提）。**请你裁**：要不要把 `bool playAmbush = false` 补进那个签名
（对既有调用点零影响），那是 `BattleContext.cs` 的一行，**本笔没做**。

### 断言逐条（宿主 `RuleEngineTest.cs`；新用例 `TestA985TargetedAndAmbushFields` + 新 `Section`/`Step`）

**①–⑦ 是你给的那七条形状**（上一批只给形状、没写代码），**⑧ 是伏击那两条**。每条 `Check` 文案里都带 🧨。

| # | 断什么（两态） | 🧨 改坏法 | 灭自证 |
|---|---|---|---|
| ① | 带目标技能（`Duel`+`UseAbility(0,3,3)`）：`TargetPlayer==1`；`TargetSlot` 回查棋盘 = **真的挨打那一份**（`ReferenceEquals` 比 `UnitState`）；`TargetCardId` = 那一格卡名 | 三个实参删掉 / 写成 `targetSlot: slot` | **不写字面量**：格位与卡名都**现读棋盘**交叉 ⇒「随便填个合法格位」过不了 |
| ② | 不带目标的技能（`Heal 3 OwnWarlord`，不传 `targetSlot`）⇒ `-1/-1/null` | 条件写成无条件填 | 与 ① 成对 ⇒「永远填/永远不填」都过不了 |
| ③ | 誓约 `UseOathAbility` 成功那条 ⇒ 仍是 `-1/-1/null` | 顺手给 `:4351` 那三个实参也填上 | 「别顺手填宽」：誓约链本来不带 `+0x28` |
| ④-a | 靶向战术卡（`Side=enemy`）⇒ 填上且 `TargetPlayer==1` | 删 `:1116-1118` | `TargetSlot` 回查**对面**棋盘 = 玩家点的那一份 |
| ④-b | **换侧**：`Side=own` 的靶向卡 ⇒ `TargetPlayer==0` | 写死 `1-p` | 「换侧」这一态直接把它判红 |
| ⑤ | **非靶向**战术卡（`Draw a card`）**拖到己方有人的格** ⇒ 仍是 `-1/-1/null` | 去掉 `PickTarget(ops) != null &&` 半句 | 🔴 **必须「拖到有人的格」**：空格时 `chosen` 本就 null ⇒ 去掉半句也绿（**假绿**） |
| ⑥ | **单位卡**那条 `Play` ⇒ `-1/-1/null`（且 `Slot==3` = 落点） | 在 `PlayCard` 的 `Emit` 上也填 | 与 ④ 成对 ⇒「凡 `Play` 都把落点抄进 `TargetSlot`」过不了 |
| ⑦ | **台账**：`RuleCore+EffectResolver` 里 `EvtKind.Play`=**2** · `EvtKind.Ability`=**2**；整个 `Core/` 合计 **3 / 3**（各含 1 处**消费**：`AppendLog` 的合并 · `BattleEvent.ToString()` 的 `case`）。先反制计数器：合成样本真值 **3**（≠2）＋空样本 **0** | 新增第 3 个 `Play` 发出点 | 「恒返回 2 / 认不出返 2」都被反制条判红。⚠️ 边界如实标着：只扫 `Core/`，表现层自己 `Emit` 扫不到 |
| ⑧-a | **伏击**单位卡 `Play` ⇒ `PlayAmbush == true`；交叉 `FaceDown == true` | 删 `PlayAmbush = PlaysFaceDown(card)` | 事件位与棋盘状态由**同一个函数**推 ⇒ 不会各写一份 |
| ⑧-b | **不带**伏击的单位卡 ⇒ `false`（两态） | 判别式写成 `card != null` | 与 ⑧-a 成对 |
| ⑧-c | **光有 `ambush` 关键词、卡面没正文** ⇒ `false`；交叉：也没面朝下 | 删 `&& TriggerOps(ambush) != null` 半句 | 钉住判别式的**第二个半句** |
| ⑧-d | 🔴 **「伏击与靶向分开」**：伏击那条 `Play` = `PlayAmbush==true` **而**三个目标字段全空（从留档 `ActionLog` 取） | `PlayAmbush := TargetPlayer != -1` ⇒ ⑧-a 红；「靶向看伏击位」⇒ ④-a/⑤ 红 | **方向相反**的两组断言 ⇒ 一个 bool 顶不了这两个语义（正是加专用字段的理由） |

⚠️ 夹具按**卡名/事件值**取对象（新增小助手 `OnBoardByName`，**走既有 `SlotOf`**），⛔ 不写格位字面量
—— 落点由 `BoardSlots.Insert`（连续无洞模型）算，写死会**假红**。

### 那句过期 doc：改前/改后

`BattleEvent.cs` 的 `TargetPlayer`（原 `:204`）：
- **改前**：`/// <summary>只有 Attack 用：被打的那一方（攻击永远是跨半场的，这里显式写出来，别让表现层去猜）</summary>`
- **改后**：`/// <summary>**被指向**的那一方 0/1（没目标 = -1）。` + **就地订正（铁律 5）**：
  `Attack`（必填）· `Play`（卡面要求选目标的战术卡才填）· `Ability`（`NeedsPick` 的才填）三种都在用；
  判据 = `GetActionText` case `0xF`/`10` 的**字节 `0x28`**（⚠️ `param_2` 是 `int*`）是不是 null；
  「没目标保持 −1、⛔ 别用哨兵值」；「`BattleDriver.BuildCardContext` 拿它算 `targetIsPlayer`，不是纯诊断」。
- 顺带把紧跟其后的 `TargetCardId` doc 里「（`Attack` 用）」扩成「（`Attack` / 带目标的 `Play` / 带目标的 `Ability` 用）」。

### 类型检查 · 行尾 · 没查清

**类型检查**：4 次全 **运行时 0 / 编辑器 0**（无别人半成品造成的假错）。中途一次红是**我自己的重名**：
`CountOf` 已存在于 `RuleEngineTest.cs`（`:19617`）⇒ 删掉我那份、改用既有的。

**行尾**（`b.count(b'\r\n')` vs `b.count(b'\n')`；全程 Edit、⛔ 无 `sed -i`；**零翻转**）：

| 文件 | 改前 | 改后 | 判定 | 净增行 |
|---|---|---|---|---|
| `BattleEvent.cs` | LF 252 / CRLF 0 | LF 289 / CRLF 0 | **纯 LF（保持）** ✅ | +37 |
| `RuleCore.cs` | LF 5263 / CRLF 0 | LF 5307 / CRLF 0 | **纯 LF（保持）** ✅ | +44 |
| `EffectResolver.cs` | CRLF 7624 / LF 7624 | CRLF 7637 / LF 7637 | **纯 CRLF（保持）** ✅ | +13 |
| `RuleEngineTest.cs` | CRLF 21847 / LF 21847 | CRLF 22176 / LF 22176 | **纯 CRLF（保持）** ✅ | +329 |

（`git diff --numstat` 的 140/2 · 408/36 · 248/12 是**相对 HEAD** 的累计量 —— 这几个文件在我接手**之前**就已经是脏的。
翻转判据我改用「HEAD 的 CRLF/LF 比 vs 工作区」比过：四个文件两侧**同为纯 LF / 纯 CRLF** ⇒ 无翻转。）

**没跑 Unity（红线）⇒ 自检一条没跑。** 收口请跑 `RuleEngineTest.Run`（宿主就在本笔改的文件里）；
若还要验表现层那条 `targetIsPlayer`，另加 `BattleScene.Run`（**上一批** `W_Targeted填充.md` 已挂这条）。

**没查清 / 请你处置**
1. **`BattleContext.Emit` 的按字段重载没加 `playAmbush` 形参**（白名单外）—— 两个发出点现在用的是对象重载。补不补请你裁（见上）。
2. **⑦ 的扫描边界**：只覆盖 `Core/`；将来在 `Core/` 之外新增 `Play` 发出点，这条**扫不到**（已写在断言文案里）。
3. **伏击位在【战术卡】那条上恒 `false`**（伏击是单位关键词）—— 我按「同一个纯函数」照算、不按卡种分家（原版 case `10` 那一维也不分卡种）。**未跑实况**，如实标着。
4. **表现层一个字没动**（白名单外）⇒ `PlayAmbush` 今天**还没有消费者**（`BattleDriver.BuildCardContext` 没读它）；
   战斗日志那句伏击措辞怎么落，是下一件活的判据。
