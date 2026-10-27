# B9_W72TIP —— 选目标时的卡片提示（tooltip）· 表现侧接线

> 上游判据 = `资料/完全复刻/报告/B9_W61TARG_卡牌目标文本.md` §4·3「改动请求 ②（表现侧）」。
> 本件产出 = ① `Battle/BattleDriver.cs` 的接/收线（+ 一条让它在**真机活下来**的必要改动）② 账补丁 ③ 本报告。
> ⛔ 未碰 `RuleEngine/**` · `Editor/**` · `Core/Loc.cs` · 任何 `*.unity` / 数据 JSON。

---

## 一、结论

1. **接上了，且只接在单位卡那一条路上**（原版唯一调用点 `BattleManager._ChooseUnitTarget_d__780__MoveNext.c:43,60-61`）。
   进入「等点目标」态那一刻弹 `Tooltip.Show(body, card.transform.position)`，正文 =
   `CardIcons.RewriteMarks(Loc.TargetedText(def.Id, ref), CardText.Zh)`；三处真出口 `Tooltip.Hide()`。
   **战术 / Whispers 卡那一支一个字都不弹**（原版 `Update.c:626-793` 全篇 0 处 `TooltipManager.Show`）。
2. 🔴 **不做一件事它就不会生效**：`TickTooltipAt` 的**末尾无条件 `Tooltip.Hide()`**（每帧 hover 扫描的收尾）。
   只按 §4·3 加三行 `Show` ⇒ 下一帧就被扫掉，**真机上等于没做**（而且不报错 = 静默失败）。
   ⇒ 改法：等目标期间**不收**（`if (!AwaitingPlayTarget) { Tooltip.Hide(); return false; }`），
   依据 = 原版那一条是 `StartTrackingFrom(卡位, 3)` **钉住**的、不是每帧重算的。
   ⚠️ 鼠标按下仍会 `Hide`（`TickTooltip` 顶部那一句，原版 `Manager.Update` 按下即收）—— **照抄**。
3. **数据侧还没到，按「字段一到就能工作」写**：`CardDef.TargetingTextReferenceId` 现读**不存在**
   （`grep -rn TargetingTextReferenceId --include=*.cs` 只命中 `Loc.cs` 的注释与本件）
   ⇒ 取值走 `TargetingTextReferenceIdOf`（反射 + 只探一次）：属性在就读属性、只有字段就读字段、
   **都没有就出声一次且不弹**（⛔ 不静默弹空串、⛔ 不硬编码文案）。
   缺词条时也拦：`Loc.T` 的缺键档会**返回键名**，把 `Card_Targeted/…` 印上屏比不弹更糟。
4. 闸 = `def.Targeted == 1`：调用点就在 `TryBeginPlayTargetPick` 的 `return true` 那一行，
   而它前面已经有 `if (def.Targeted != 1) return false;` ⇒ **隐式就是 targeted 闸**，
   `SAU24 Command Barge`（ref 有、`targeted=0`）**结构上到不了这里**（W61 报告 §2·4 第 2 条那条坑已躲开）。
5. 账：本行状态**保持 `B`**（不升 A）—— 表现侧虽然接上了，但 ① 数据列还没进 `CardDef`（走反射桥）、
   ② 原版是协程 + `StartTrackingFrom` 钉住，我们是每帧状态机 + 不 Hide ⇒ 机制不同、语义等价，
   按「等价实现」继续记 B 更诚实。

---

## 二、证据（文件:行号）

### 2·1 改动点（`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`，现读行号）

| 行 | 内容 |
|---|---|
| `:8137` | **进**：`if (unitCard) ShowPlayTargetPrompt(def, card); return true;`（`TryBeginPlayTargetPick` 的「进等待态」那一行，前面已过 `targeted` 闸 / 空池闸 / `n == 0` 闸） |
| `:8231` | **出 1**：`_awaitPlayTargetCard = null; _awaitPlayTargetDef = null; Tooltip.Hide();`（回合易主 / 对局结束） |
| `:8254` | **出 2**：`_chosenTargetForNextPlay = hit; Tooltip.Hide();`（点定目标 ⇒ 照常开打） |
| `:8282` | **出 3**：`ClearPlayTargetHighlight(); Tooltip.Hide();`（取消，牌留手里） |
| `:32370` | **每帧那一道**：`if (!AwaitingPlayTarget) { Tooltip.Hide(); return false; }` + `return Tooltip.Visible;`（`TickTooltipAt` 末尾） |
| `:35601-35630` | 类尾新增的说明块（原版链逐段 + 为什么只单位卡 + 为什么走反射桥） |
| `:35632` | `static string TargetingTextReferenceIdOf(CardDef def)`（反射过渡桥 + 只探一次 + 缺字段出声一次） |
| `:35660` | `void ShowPlayTargetPrompt(CardDef def, CardView card)`（取键 → 缺键拦 → `RewriteMarks` → `Tooltip.Show`） |

### 2·2 判据（沿用 W61，未重查）

- 原版调用链：`BattleManager._ChooseUnitTarget_d__780__MoveNext.c:43`（正文 = `GetLocalizedTargetted()`）
  · `:60-61`（`Show(battleTooltip, 标题=空串, 正文, pos, 0, 0)` ⇒ anchor = 0）· `:62` 点亮 · `:84-91` `Hide`。
- 进入闸：`BattleManager__NeedToChooseMinionTarget.c:12-18` = `isPlayer ∧ cardType==0 ∧ targeted ∧ matchType != 0xA0`。
- 战术卡那一支**没有** tooltip：`BattleManager__Update.c` 全文 `grep EverguildTooltipManager__Show` = 0。
- 键 / 取值 / 记号展开：`Core/Loc.cs:3776`（`TargetedKey`）· `:3788`（`TargetedText`）· `Core/CardIcons.cs:647`（`RewriteMarks`）·
  zh 列闸 = `Core/CardText.cs:38`（`CardText.Zh`）。
- `Tooltip.Show(body, worldPos, anchor=0, offset=default)`：`Core/Tooltip.cs:307`（`Show` 自带「文案没变不重排版」+ 抑制期不弹）·
  `:392`（`Hide`）· `:66`（`Visible`）。

### 2·3 必跑（实测）

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w72 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh check` | **rc=0** · `[输出指纹] … 与确认的一致（c9a64798…）` **不变** |
| `bash 工具/ruleprobe.sh play check` | **rc=1，只有 `BL64` + `DA42`**（`=== 2 张卡变了 · 新增 0 · 消失 0 ===`）· **没有第三张** |
| 行尾 | 全文 **CRLF 35,682 / LF 35,682 · 裸 CR 0**（改前 35,600） |
| `python 工具/check_ledger_points.py` | `BattleDriver.cs` 那批数**没变**（`战斗界面_HUD` 467/386/79/8/2 · `卡牌面_渲染` 116/84/30/2/2 与改前逐格相同）；既有 +67 行偏移是别的批次留下的、与本次无关 |

---

## 三、没查清的部分

1. **tooltip 的锚点偏移**（原版 `pos = Get2DPosOfCard(卡) + 小偏移`，`DAT_1834b3400` 是 float 常量、可读但没读）——
   我们直接用 `card.transform.position`（anchor 0 居中、offset 0）⇒ **位置是近似**，如实标在代码注释里。
2. **`EverguildTooltipManager.Show` 第 6/7 个实参（`0, 0`）** 语义仍未查（W61 §三·3 同一条）—— 不影响「有没有」。
3. **等目标期间「悬停别的卡」的优先级**：我们保留 hover 扫描（悬停到某张卡 ⇒ 换成那张卡的卡面提示；
   移开 ⇒ 提示**不会**自己回来，要等下一次进状态）。原版 `Show` 也只发一次、`OnPointerExit` 后就没了 ⇒
   两边形状一致，但**没有实证到原版在 UIstate 9 里 hover 的行为**（未上真机跑这一档）。
4. **按下但没结算**（例如压在 HUD 按钮上）：`TickTooltip` 顶部那一句会把提示收掉，而等待态还在 ⇒
   提示不再回来。原版 `Manager.Update` 同样「按下即 Hide」、协程不再 Show ⇒ **照抄**，但属未实证的边界。

---

## 四、改动清单

| 文件 | 改了什么 |
|---|---|
| `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | **body 5 处原地等行数替换**（0 新增行）：`:7959-7961` 注释改准（字段名 + 「数据其实有，只是不在 `cards_engine.json`」）· `:8137` 进态调用 · `:8231`/`:8254`/`:8282` 三处出口 `Tooltip.Hide()` · `:32370` 每帧扫描在等目标期间不收。**类尾追加 80 行**（说明块 + `_tgtRefProp/_tgtRefFld/_tgtRefProbed` + `_playTargetPromptMissingWarned` + `TargetingTextReferenceIdOf` + `ShowPlayTargetPrompt`）。 |
| `账/_parts/B9_W72TIP.tsv` | 新建。1 行（`战斗逻辑驱动.tsv:1241` / `RawCardScript.GetLocalizedTargetted()`），**状态仍 `B`**；`我们的落点` 补表现侧四处、`差异` 补「表现侧已接线 + 仍欠数据列」、`轮次` = `B9_W72TIP`。11 列、带表头、`方法` 列带 `()` ⇒ 过 `check_patch_conflicts.py`（现读「1 份补丁、无跨补丁重复键」）。 |
| 本报告 | 新建。 |

⛔ **没碰**：`Core/Loc.cs`（W61 已交付，本波只读）· `RuleEngine/**` · `Editor/BattleScene.cs`（W67）·
`Core/CardDef.cs`（W65）· 任何 `*.unity` / `ProjectSettings` / 数据 JSON。

---

## 五、顺手发现

1. 🔴 **`TickTooltipAt` 末尾的 `Tooltip.Hide()` 会吃掉任何「程序自己弹的」tooltip** ——
   这次要不是顺手查了 `TickTooltip` 的调用链，接上去就是**下一帧消失、且零报错**。
   ⇒ **以后凡是「非 hover 驱动」的 `Tooltip.Show`（脚本弹的提示），都必须同时问一句「每帧扫描会不会收掉它」**
   （与 `CLAUDE.md` §三「加一层之前先 grep 谁会遍历 `_layers`」同族：要先查「谁在每帧改这个状态」）。
2. 🟡 **W61 报告 §4·3 的出口行号与现读略偏**（报告写 `:8231` / `:8260` / `:8280`；实为 `:8231` / **`:8254`** / **`:8282`**）——
   `:8260` 是 `Ctx.Log("点定目标…")`、清理动作其实在 `:8251-8254`。行号只当线索、按符号名定位这句纪律又对了一次。
3. 🟡 **`Loc.T` 缺键时返回键名**（`:3464` 一带的缺键档）⇒ 任何**直接吃返回值上屏**的新调用点都要自己拦一道，
   否则屏幕上会印 `Card_Targeted/xxx`。本件拦了，并留了「出声一次」的告警。
4. 🟡 **`CardDef` 上还没有 `TargetingTextReferenceId`**（另一路在出）—— 本件的反射桥是**过渡实现**，
   字段落地后应改成一句 `def.TargetingTextReferenceId` 并删掉 `TargetingTextReferenceIdOf` 与那一格缓存
   （注释里写死了这个删除条件）。
5. 🟡 **`AwaitingPlayTarget` 期间 `TickTooltipAt` 的返回值语义**从「恒 false（没命中）」变成「`Tooltip.Visible`」——
   现在**没有任何自检**在等待态里调它（`grep TickTooltipAt` 的调用点都在正常手牌态），
   但将来若有人写「等目标态 ⇒ `TickTooltipAt` 必须 false」的断言，会**红得莫名其妙**，先看这一行。
6. 🟢 **断言那一段没做**（W61 §4·4 的三行：`Loc.CheckTargetedText()` 接进 `BattleScene.Run`）：
   `Editor/BattleScene.cs` 归 `W67`（本波别人正占）⇒ 留给那一批。**本件没有新增任何自检**，
   也就是说「有没有弹」目前只能靠真机看 —— 建议下一波在 `BattleScene.Run` 里补：
   ① 前提硬检查 `Loc.Lang13Keys(Loc.TargetedTermPrefix).Length > 0`；② 进等待态后 `Tooltip.ShownBody` 非空
   且**以 `Card_Targeted/` 开头就是错**（= 键名漏上屏）；③ 三处出口后 `Tooltip.Visible == false`。

---

## 交接给下一个代理（本件已收尾）

- 代码 + 账补丁 + 报告都已落盘；`TMPDIR=/tmp/wf_w72 工具/typecheck.sh` = **0/0**。
- ⏭ 还欠两件（都在别人名下，⛔ 别在本波做）：
  ① `工具/gen_cards_engine.py` + `RuleEngine/Core/CardDef.cs` 出 `targetingTextReferenceId` 列（W65 / 调度台；
     重生成 `cards_engine.json` **会动 `play check` 指纹** ⇒ 只能在没有并发账写手时做）；
  ② `Editor/BattleScene.cs` 补 §五`顺手发现 6` 那三条断言（W67）。
- ⛔ **别改**：三处出口的 `Tooltip.Hide()` 与 `TickTooltipAt` 那一行 —— 少了任何一处，提示要么不收、要么当场被扫掉。
