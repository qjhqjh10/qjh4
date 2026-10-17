# W3 · BattleDriver 读回口修复（TitlePosPx / EnemyPlatePos01）

> 执行写手 W3。白名单只有 `Battle/BattleDriver.cs` 一个文件。⛔ 未跑 Unity / 未动 git / 未改正本 / 未碰别的文件。
> 判据 = `BSC62/R2_量纲与字框.md`（#11）· `BSC62/R3_位置层序命中卡体.md`（簇 A②/#4 `18161`）。行号全部**现 grep 现核**（本文件行号漂得快）。

## 一、改了什么（2 处）

### ① `TitlePosPx`（搜 `public Vector2 TitlePosPx`，现 `:6783-6795`）
- **改前**（`:6788`）：`var n = LayoutSpace.ToNormalized(l.transform.localPosition);`
- **改后**（`:6793`）：`var n = ((RectTransform)l.transform).anchorMin;`
- **为什么**：`T09`（2026-10-16 字族整族迁 uGUI）后 `_titleMe/_titleFoe` 建在**画布**上（`Hud(root,…)`，`:15073/:15076`），`localPosition` 是**画布 px / 原点在画布中心**；喂世界尺 `ToNormalized` ⇒ 量纲错，报出 `(-79216.3, 54340.8)`。
- **判据**：同族 `PlateTextPosPx`（现 `:16070-16079`）**当日已按 `anchorMin` 改对**、本处漏改。我**先读了 `PlateTextPosPx` 的最终写法再照抄**。
- **正确性复核（逐位吻合）**：`Hud()`（`:16352` 的建法；那两句在 `:16374`）明写 `rt.anchorMin = rt.anchorMax = new Vector2(x01, y01); rt.anchoredPosition = Vector2.zero;`，而 `_titleMe` 传入 `x01 = 217.62721/1920`、`y01 = 1 - 1038.15523/1080`。
  ⇒ 新式 `(anchorMin.x*1920, (1-anchorMin.y)*1080)` = **(217.62721, 1038.15523)**，正是断言期望值（`Editor/BattleScene.cs:14818`）。
- 顺带把 doc 补成「口径同 `PlateTextPosPx`」。

### ② `EnemyPlatePos01`（搜 `public Vector2 EnemyPlatePos01`，现 `:16055-16058`）
- **改前**（原 `:16047`）：`get { return _enemyPlate != null ? LayoutSpace.ToNormalized(_enemyPlate.transform.localPosition) : Vector2.zero; }`
- **改后**（`:16057`）：`get { return _enemyPlate != null ? _enemyPlate.Anchor01() : Vector2.zero; }`
- **为什么**：孪生 getter **不同源** —— 我方 `MyPlatePos01`（`:16046`）已走 `_myPlate.Anchor01()`，敌方漏改。`T09` 后名牌也在画布上，`localPosition` 是画布 px ⇒ ÷VisibleWidth×108 错（实得 −51.978 = (−933.4)/17.78+0.5）。改成 `Anchor01()` 即**两者同源**。
- **正确性复核**：`_enemyPlate = HudImage(root,"UI_Player_Frame", 27.0559f/1920f, 0.927083f, …)`（`:14054`）⇒ `Anchor01()` 的 uGUI 支回 `anchorMin` = (27.0559/1920, 0.927083)，正是断言期望（`Editor/BattleScene.cs:3551`）。
- ⚠️ **本处依赖 W2 的 `HudPiece.Anchor01()` 读回口修复** —— 该支当前 `HudPiece.cs:256` 只回 `anchorMin`、丢了 `NudgeWorld` 写进 `anchoredPosition` 的偏移（`R3` 簇 A①/`18289`）。对我这一处：两块名牌是不是被 `NudgeWorld` 挪过，我没查（见 §五）；只要 W2 把 `Anchor01()` 修全，本处自动跟着对。

## 二、`localPosition` + `ToNormalized` 全仓普查结果
搜法（两轮）：`rg -n "ToNormalized\(.*localPosition"` 全 `Assets/`；再在 `BattleDriver.cs` 单搜 `ToNormalized\(` 兜底（防「先存进变量」那种）。
**全仓仅 4 个命中**，其中 2 个是我改的两处：

| # | 位置 | 是不是缺陷 |
|---|---|---|
| 1 | `BattleDriver.cs:6793` `TitlePosPx` | ✅ 已改（本单 ①） |
| 2 | `BattleDriver.cs:16057` `EnemyPlatePos01` | ✅ 已改（本单 ②） |
| 3 | **`BattleDriver.cs:16173` `static Vector2 PosOf(Component c)`** | **第三处** —— 见下，**在**我的白名单里，但**没动** |
| 4 | `HudPiece.cs:255` `Anchor01()` 的 **`_quad != null` 那一支** | **不是缺陷** —— 世界档（quad）本来就该读 `localPosition`，是**正确**的那一支 |

**关于第三处 `PosOf(Component) c`（`BattleDriver.cs:16173`，方法体 `:16175`）**：
- 它是同族的旧读口（`ToNormalized(c.transform.localPosition)`）。
- 🔴 **它是【死代码】**：本类**不是 partial**（`class BattleDriver`，`:22` 唯一一处），`PosOf` 是 `private static`；全部 3 个调用点 `:16162/:16163/:16171` 传的**都是 `HudPiece`** 实参（`_myEnergyPlate/_foeEnergyPlate/_myQuestIcon/_foeQuestIcon` 四个字段全声明为 `HudPiece`，`:1285/:1286`）⇒ **重载解析一律选中更具体的 `PosOf(HudPiece)`（`:16180`）**，`PosOf(Component)` **一个调用点都没有**。全 `Assets/` grep `PosOf(` 只回这 5 行（3 调用 + 2 定义）+ `Editor/BattleScene.cs` 的 `FiredPosOf`（无关同名）。
- **我【没动】它的理由**：① 它不在本单点名的两处之内；② 它**是死的**，改量纲无任何行为变化（不值当冒险）；③ 简报「⛔ 别顺手改别的」。→ **据简报要求写进报告，由调度台裁断**（要清就一句话：删掉这个 `Component` 重载，或给它补 `Anchor01` 口径）。

## 三、`*PlatePos01` 两个 getter 的全部使用点（grep 结果）
搜 `PlatePos01`（全 `Assets/`）：
- **定义**：`MyPlatePos01` `:16046` · `EnemyPlatePos01` `:16055` · （另 `HandPlatePos01` `:16045`，不同族，未动）
- **使用**（唯一一处消费者，**两个一起读**）：
  - `Editor/BattleScene.cs:3541` `var mp = drv.MyPlatePos01;`
  - `Editor/BattleScene.cs:3542` `var ep = drv.EnemyPlatePos01;`
  - 紧接着 `:3549` 断 `mp`、`:3551` 断 `ep`。**函数体内再无别的 `tpM/ep` 用法** ⇒ 改成同源**不会带坏别的调用点**（两者共用同一段检查，本来就该同源）。

## 四、类型检查结果
`TMPDIR=/tmp/wf_w3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**（干净）。
`git diff --numstat` = **13 / 3**（小改，非行尾翻）；二进制核行尾 **CRLF=17669 / LF=17669**（纯 CRLF 保住了）。

## 五、没能验的部分（⛔ 不猜）
1. **没跑 Unity 自检**（本轮现行口径 = 只做不验）⇒ `BattleScene.Run` 那两条（`143872` 称号 / `18161` 名牌）**没实测变绿**。我给的「逐位吻合」是**静态推算**（拿 `Hud()` 的建点参数 + 断言期望值反算），**不是跑出来的**。
2. **`Anchor01()` 的 uGUI 支此刻残缺**（W2 在修）⇒ ② 的最终读数**取决于 W2**；若 `_enemyPlate` 被 `NudgeWorld` 挪过而我不知，则**要等 W2 修完才对得全**。**我没查 `_enemyPlate` 是否被 `NudgeWorld` 调用过**（超出本单两条目标）。
3. **非 16:9 分辨率下**没验（`Hud()` doc 说锚点自带分辨率关系，理论上没问题；但这轮没跑）。

## 六、顺手发现（⛔ 没动手）
1. **`PosOf(Component)` 是死代码**（见 §二）—— `T09` 加了 `PosOf(HudPiece)` 重载却没删旧的 `Component` 重载；留着会让人误以为还存在世界尺读口。**建议由调度台一句话裁掉**（弃用/删除）。
2. **`PlateTextPosPx` 的 doc 老注释**（`:16067`）写「口径同 `TitlePosPx`」—— 我改完后两者口径确实一致了，这句**现在成立**（不必改）。
3. 其余 `ToNormalized` 命中（`BattleDriver.cs:12499` `hl.SlotPosition(...)`）是**棋盘槽位**、与世界/HUD 尺无关，**不是同族**。
