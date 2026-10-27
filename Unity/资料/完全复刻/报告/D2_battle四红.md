# D2 · `BattleScene.Run` 4 条红定性（只读诊断 · 2026-10-27）

日志 `d:/4/_tmp_view/battle.log`（5620 ✓ / 4 ✗）。⚠️ `grep -c "✗"` = **6**，
其中 **2 条是断言文案里的 ✗**（`battle.log:145266` 的「E8 合计」行、`294661` 的 ColorLookup 文案）
⇒ **真红 = 4 条**（`:135020` `:135051` `:191610` `:192800`）。

| # | 位置（`Editor/BattleScene.cs`） | 定性 |
|---|---|---|
| ① | `:10857` bloodthirst 第 1 次触发 | **(γ) 本批回归**（新闸是对的，**夹具过期**） |
| ② | `:10866` bloodthirst 第 2 次触发 | 同 ①（同一条夹具、同一根因） |
| ③ | `:23414` 飘字没落在挨打那张卡上 | **(γ) 本批回归**（新命中帧震屏把「锚点走实况相机投影」这条老隐患放大了 357 倍） |
| ④ | `:48589` W11 HUD 根与相机同向等量 | **(α) 断言错**（本条本会话新增，公式漏了 `hudRoot` 自己的 home） |

---

## ① ② bloodthirst 两条

**原文（`:10857` / `:10866`）**
```
Check(tf3 != null && tf3.TriggerCount == 1 && tf3.TriggerPlayCount == btPlays0 + 2,
      "★ 「bloodthirst」第 1 次触发 ⇒ 演**两条**（3 → `_Gain`、6 → `_Idle`）、并写进去重表"
    + $"（{(tf3 != null ? tf3.TriggerCount : -1)} 张 / 演过 {(tf3 != null ? tf3.TriggerPlayCount : -1)} 次）"
    + "｜🧨 只绑/只播一条 ⇒ 红（原版 `DisplayTriggerAnim.c:47-111` 逐条遍历）");
...
Check(tf3 != null && tf3.TriggerCount == 1 && tf3.TriggerPlayCount == btPlays0 + 2,
      "★ 「bloodthirst」第 2 次触发 ⇒ **不再演**（原版守卫：去重表里已有这个 trait；"
    + $"累计仍演过 {(tf3 != null ? tf3.TriggerPlayCount : -1)} 次）");
```

**定性：(γ) 本批回归 —— 但错的是夹具，不是实现。**
新闸本身是原版判据，⛔ 不要把它去掉。

**证据**
1. 两条红前面各有一条日志：`[Battle] 「血渴」的触发点不在行动侧（player=1 / Active=0） ⇒ 原版
   `ActivateBloodThirst` 为假、这一下**不演**`（`BattleDriver.cs:24659`，两处 `battle.log:135002` / `135033`）。
2. 读数差正好是「闸拦在 `DisplayTrigger` 之前」的形状：现在 `(0 张 / 演过 3 次)`，
   上一版 `(1 张 / 演过 5 次)`（`_tmp_view/20261024_sync6_battle.log:123822` · `20261024_sync3_battle3.log:123620`）
   ⇒ **去重表没写、播放数没加**，说明 `return` 发生在 `tf.DisplayTrigger(...)`（`BattleDriver.cs:24682`）之前。
3. 闸 = 本会话新增（`git diff` 的 `@@ -24596,2 +24644,20 @@` 那一块，`BattleDriver.cs:24645-24662`），
   判据现读吻合：`CardScript__ActivateBloodThirst.c` 全文 =
   `HasCurrentTrait(0xdc) && *(int*)(this+0x48)==1 && (char)(this+0x40) == IsPlayerTurn() && state(+0x228)!=5`；
   `BattleManager__ResolveBloodThirstAnim.c:21-24` 用它**门住整条动画**。
4. 夹具制造了一个真机不可能出现的状态：事件写死 `Player = 1`（`:10852` / `:10863`），
   而本局 `Ctx.Active = 0`（`PinFirstSeat(driver, 0)`，`BattleScene.cs:46961`）。
   原版里血渴只由**正在行动那一方**的单位触发 ⇒ `isPlayer == IsPlayerTurn()` 在真机上恒真。

**该怎样**
夹具让事件落在**行动侧**：在这两段前 `var act0 = ctx.Active; ctx.Active = 1;`、`:10868` 之后还原
（`BattleContext.Active` 是 `public int`，`RuleEngine/Core/BattleContext.cs:354`）；
并**补一条**反向断言「非行动侧 ⇒ **一次都不演**」，把新闸本身也钉住（铁律 11：新行为要配断言）。

---

## ③ 飘字位置

**原文（`:23414`，`battle.log:191610`）**
```
Check(dPop < 0.05f && drv.LastPop.transform.position.y > popBase.y,
      $"……而且飘在**挨打那张卡上**（基准 = 那一格的 3D 投影点，偏差 {dPop:F4} 世界单位、在上方）");
```
（`dPop = |LastPop(x,y) − [eBoard.DropTargetWorld(victim) + drv.PopOffsetWorldFor(1)](x,y)|`；本次实测 **0.1453**）

**定性：(γ) 本批回归** —— 触发点 = 本会话把**命中帧震屏**换成新的「原版棋盘坐标」那一路。
底层隐患（锚点取自**实况被震动的相机**投影）是老的，但被放大了 **357 倍**才越过 0.05 容差。

**证据**
1. 上一版同一条 = **0.0006**（`20261024_sync6_battle.log:164650` · `20261024_sync3_battle3.log:163589`）。
2. 实现与断言读的是**同一份常量与同一个口**（`BattleDriver.cs:25027` 的 `at = layout.DropTargetWorld(e.Slot)`
   对 `:23405` 的 `eBoard.DropTargetWorld(victim)`；`PopOffsetWorldFor` 也是同一个），
   ⇒ **纯改常量不可能让它动**。唯一会变的是那个口的**输入**：
   `BoardLayout.DropTargetWorld`（`Board/BoardLayout.cs:523-535`）是
   `boardCam.WorldToScreenPoint(ArenaSlots.CardCenter(slot, mirror, ProjectionScale))` —— **实况相机投影**。
3. 而这一刻 `boardCam` 正在动：本会话 `PlayHitFrameShake` 由 `ShakeCameraForHit` 换成了
   **`ShakeCameraForAttack`**（`BattleDriver.cs:22664-22667`，diff 逐字可核）
   —— 世界幅度 `|棋盘坐标|(≈101) × 2/4/8 × 0.149 × (73.1/108)` = **20.38 / 40.76 / 81.52**
   （`CardFeel.cs:4153-4158`），屏幕方向 **纯水平**（`CardFeel.cs:4100-4105` + `:4185` ⇒ `screenDir ≈ (−0.998, 0)`）。
   旧那一路 = `ShakeWorldAmplitude` **0.057**、纯竖直（`CardFeel.cs:512` / `:1521-1529`）。
4. 量化吻合：`20.38 / 0.057 ≈ 357`，`0.0006 × 357 ≈ 0.21` 与实测 **0.1453** 同量级
   （差的是那一拍噪声/包络，不是判据）；③ 的偏差**随震屏幅度线性缩放**。
5. **偏差在 x、不在 y**：独立那条「飘字落在卡身竖直中点」本批**是绿的**
   （`battle.log:191649`，离中点 0.0024），而新震屏的水平通道正是只改 x ⇒ 两处自洽。
6. 时间上也对得上：命中帧震屏总时长 = `0.1 + 0.3 = 0.4 s`（`CardFeel.cs:214-218`），
   起于 `EventTiming.HitFrameLag`（`BattleDriver.cs:22253`）；伤害 `Hit` 在 `Attack + MeleeDamageLag = 0.4 s`
   （`Core/EventTiming.cs:804`）⇒ **锚点是在这条震屏的最后一帧上取的**，把当时的残余位移（0.1453）烘进了飘字；
   再 `AdvanceTo(dmgAt + 0.03f)` 时震动已被 `Kill` 复位 ⇒ 两边投影不再一致。

**该怎样**
让飘字锚点**不吃相机的瞬态位姿**：在 `BattleDriver.cs:25027` 取 `at` 之前先
`CardFeel.SettleShake()`（此刻命中帧震屏本来就只剩最后一帧，视觉上等于无损），
或让飘字跟着同一条震屏位移走（原版那颗 `DamageCounter` 是 **3D 卡的子件**，真机上是跟着卡晃的 —— 这才是长期正解）。
⚠️ **顺带**：`CardFeel.cs:4146-4152` 自己就写着「这个数**只由机器码与场景数据算出，没有真机对照过**；
若发现镜头飞出去，判据缺口在『原版运行期棋盘是不是真在 x≈100』」—— 20～81 世界单位 = **2～8 个屏高**，
`rotationGain` 还一起乘到 `|dir|×2/4/8 ≈ 202/404/808`（`CardFeel.cs:4185`）。
**这条幅度律在真机验收前别当定稿**；若它本身判错，③ 会自己变绿，但「锚点走实况相机」这个隐患仍然要按上面那样收口。

---

## ④ W11 HUD 根

**原文（`:48589`，`battle.log:192800`）**
```
check(hudRoot == null || (hudRoot.position - (home + dA)).magnitude < 1e-4f,
      "★ W11/端到端：HUD 根与相机**同向等量**平移（屏幕上纹丝不动那一条对水平通道同样成立）"
    + "｜🧨 HUD 只跟竖直分量 ⇒ 横着抖时 HUD 跟着飞 ⇒ 红");
```

**定性：(α) 断言错**（`W11ShakeAssert` 是本会话新增的类：`git diff` 里 `+ static class W11ShakeAssert`）。

**证据**
1. 实现（`CardFeel.cs:1473`）是**绕相机位置的刚体变换**：
   `hudRoot.position = camHome + d + M * (hudHome − camHome)`，`M = camHomeRot · q · camHomeRot⁻¹`。
   断言把它当成 `camHome + d` —— **漏了 `M*(hudHome − camHome)` 整项**，也从未记录过 `hudHome`。
2. 这在本场景下必然红：`hudRoot` = `new GameObject("HudRoot")` + `SetParent(transform,false)`
   挂在原点那棵 `Battle` 下（`BattleDriver.cs:28244-28246`），全项目只有 `CardFeel` 动过它的位置
   （`CardFeel.cs:1473/1495/1496`）⇒ `hudHome = (0,0,0)`；而 `cam` 在 `(0,0,−20)`（`BattleScene.cs:46862`）。
   ⇒ 误差 = `|M*(0,0,20)| ≈ 20 世界单位`，与 0.0001 的阈值差 5 个数量级。
3. 反证「接线是好的」：**同一意图的老断言**（`BattleScene.cs:24094-24113`，它就是
   `hudExpect = camHome + hd + M0*(hudHomeW − camHome)`）本批**绿**：
   `battle.log:192597 ……实得位移 2.6E-006，旋转残差 0.0000°`。
   ⇒ 坏的只有新加的这条写法。

**该怎样**
照抄 `:24099-24113` 那一份参考算式（含 `M0*(hudHomeW − camHome)`，`hudHomeW` 在测前取一次）；
更好的写法是断**屏空间不变式**：用 `cam.WorldToScreenPoint` 取 HUD 上一个钉子点在震前/震后的屏幕坐标、要求相同
—— 那才是「屏幕上纹丝不动」本身，也不必再手抄内部那个 `M`。

---

## 顺带发现（⛔ 未改，供调度台分流）

- `grep -c "✗"` 在本日志上是 **6**，但真红只有 **4**：`battle.log:145266`（「E8 合计」行）与 `294661`
  （ColorLookup 断言**文案里**的 ✗）都不算 —— 与 CLAUDE.md「按行首标记数」那条一致，这里记一笔以免下轮重复踩。
- ③ 与 ④ **不同源**：③ 是震屏幅度/锚点问题，④ 是断言公式问题（既有方法可照抄）。改 ④ 不会顺带修 ③。
- ①② 与 ③④ 也互不相关；四条的修复可以完全并行（各自文件不冲突：①②④ 在 `Editor/BattleScene.cs`，
  ③ 在 `Battle/BattleDriver.cs` 的 `PlayHitFeel`，**①② 与 ④ 同在 BattleScene.cs ⇒ 仍须串行或分块**）。
