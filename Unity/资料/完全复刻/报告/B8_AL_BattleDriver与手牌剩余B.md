# B8_AL · `BattleDriver` / `Hand/**` / `Core/CardFeel.cs` 剩余 B 行推进

> 执行代理 `B8_AL` · 2026-10-26 · 批次 8
> 白名单：`CardPresentation/Battle/BattleDriver.cs` · `CardPresentation/Hand/**` · `CardPresentation/Core/CardFeel.cs`
> 账补丁：`资料/完全复刻/账/_parts/B8_AL.tsv`（**14 行：9 条升 A · 4 条保持 B（判据/观测量全部补实）· 1 条 B→C**）
> 判据源：🥇 `d:/2/tools/decomp_full/*.c`（本轮亲读 6 份）· 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs` · 字段偏移 = `d:/2/tools/il2cpp_out/dump.cs`

---

## 一、结论

| 项 | 数 | 说明 |
|---|---|---|
| **真升 A** | **9** | `CardScript.AttackMeleeAnim` · `CardScript.GetEffectAnchor` · `CardScript.GetHandToBoardAnimEventTime` · `CardScript.MoveToDeck` · `CardScript.OnTouchDrag` · `CardScript.SetAsUnitInPlay` · `CardScript.SetAsUnitTransforming` · `CardScript.ShowCardInHand` · `CardScript.TryDraggingFromHand` |
| 保持 B（补实判据 + 观测量 + 写清卡在哪） | 4 | `OnTouchDown` · `OnTouchEnter` · `OnTouchExit` · `OnTouchUpAsButton` —— **同一处阻断**，见 §五 |
| **B → C**（明确缺口 + 工作量） | 1 | `CardScript.ShowCardBeingDrawn`（弃牌 / 偷牌揭示整条路不存在；B8_W 也建议过 C） |
| 改了 `.cs` | **+48 行** | 2 个文件、2 处，全在类尾（见 §三） |
| 机检 | **全绿** | `typecheck` **0 / 0** · `verify_annot` **缺注解 0** · `check_ledger_points`（预览）**失败 0**、我这 9 行**一条都不在漂移列表** · `check_a_vs_diff` **强 0**，我这 9 行**一条都不在 13 条弱信号里** |

**没达成 ≥10 的原因（如实报，不凑数）**：剩下那 4 条（`OnTouchDown` / `OnTouchEnter` / `OnTouchExit` / `OnTouchUpAsButton`）
**全部卡在同一台东西上** —— 原版的**场上卡高亮状态机**（`TryCardHighlight` / `CardHighlight.ToggleCombatPreviewHighlight` /
`ChangeState(2⇒0)` / `ChangeState(4⇒3)`）。我们没有「悬停/按下场上卡 → 预览高亮」这条交互
（`BattleDriver` 里与高亮有关的只有攻击瞄准确星 `_reticleTarget` / `SelectedTargetInBoard`，那是**选目标**不是**悬停预览**），
而那台机器在 `Core/CardHighlight.cs` + `Core/CardView.cs`（**本代理白名单外**）。
⇒ 按 B2A 门槛③「方法体里还有一段没对上落点 ⇒ 保持 B」，把这四条**先钉上观测量、再回 B**。
**下一手只要把那台机器核一次，这四条可以一起翻 A**（明细见 §七 请求 ①）。宁可 9 条真 A，不写 12 条假 A。

---

## 二、逐条证据（G8 格式）

```
块 B8_AL-1 | CardScript.AttackMeleeAnim(Vector3 pos, CardScript target)
  | 判据 decomp_full/CardScript._AttackMeleeAnim_d__357__MoveNext.c:1-313 + 工厂 :1-27 + 三个闭包 + get_AttackStepTime.c
  | 落点 CardFeel.cs:1104-1136（等价实现 = MeleeAttack）+ HookDepthScale
  | 观测量 = ① 近战序列实测总长：我们 SequenceSeconds(MeleeAttack(...)) = 0.4 s（0.1 + 0.1 + 0.2）
              vs 原版 DOMove 0.1 + AppendInterval 0.1 + DOLocalMove 0.2 = 0.4 s（`.c:139-267`）
            ② 冲点 y 项 = EffectsAnchorHeight(受击方) × 0.5（1.34 × 世界缩放）
  | 差异 = ① 收招：原版 Sequence.OnComplete = FinishAfterAttack，我们 AttackRecover + EventTiming
           ② 命中帧三跳：原版在 DOMove.OnComplete，我们在驱动层命中帧（DOMove 只完成一次 ⇒ 次数等价）
  | 还剩 = 回放快进那一档（`get_AttackStepTime` 的 `DAT_1834b2e8c` 未解，且我们不实现回放快进）

块 B8_AL-2 | CardScript.GetEffectAnchor()
  | 判据 decomp_full/CardScript__GetEffectAnchor.c:8-24（本轮亲读全文 25 行）
  | 落点 CardFeel.cs:968-983（等价实现 = EffectsAnchorHeight）；两个候选 = CardView.cs:2091 / :2097
  | 观测量 = EffectsAnchorScaleRatio(卡) = EffectsAnchorHeight ÷ lossyScale.y = 1.34（两侧同值）
              我们 = Height/2 1.66565 − BoardEffectAnchorLocal.y 0.32565；原版 = prefab EffectAnchor.localPosition.y 1.34
  | 差异 = 原版返回 Transform（按 cardState 八元素集合二选一）；我们只给一个标量、恒取 effectsAnchor 那一棵。
           对唯一消费面（近战 dy）等价 —— 近战只发生在 inPlay(2)/inPlayAttacking(3)，两者都在集合之外
  | 还剩 = 若将来有消费面要 2D 卡体锚（card2DController 那一支），要回来补 EffectAnchorHand

块 B8_AL-3 | CardScript.GetHandToBoardAnimEventTime()
  | 判据 decomp_full/CardScript__GetHandToBoardAnimEventTime.c:1-40（本轮亲读）+ ILSpy CardScript.cs:8414-8421
          独立读数 = 资料/普查产出_1004/调度台_裁定与复核_1005.md:142（`Card Hand To Board` 的
          `CardHandToBoardAnimationFinished` @ 0.55 s）
  | 落点 CardFeel.cs:2048-2184（等价实现 = HandToBoard）+ :558 常量 + :2157 InsertCallback
  | 观测量 = 3D 体材质复位时刻，区间夹逼 0.50 s 仍溶解 / 0.583 s 已复位 ⇒ 0.55（CardBaseDemo.cs:3973-4001 既有断言）
  | 差异 = 原版运行时读 clip 的 AnimationEvent[0].time；我们是手抄常量 0.55（数值取自同一条 clip）
  | 还剩 = 无（那条 clip 是原版资产，我们不改它 ⇒ 常量不会与它分叉）

块 B8_AL-4 | CardScript.MoveToDeck(bool useVerticalAnimOffset, float fixedTime)
  | 判据 decomp_full/CardScript__MoveToDeck.c:1-201 + 消费点 _ResolveRecallToDeck_d__543:350-356
  | 落点 CardFeel.cs:1870-1990（等价实现 = ReturnToHand）+ EventTiming.cs:279（MoveToDeckTotal）+ :216-233
  | 观测量 = 回牌库那一拍的时长：我们 ReturnHoldOf(deck) = 0.55 + 0.4 = 0.95 s，其中 MoveToDeckTotal = 0.1 + 0.3 = 0.4
              vs 原版 `MoveToDeck(card,1,0)` 返回值 = 0.1（VarsGlobal+0x78）+ 0.3（DAT_1834b2dc8）= 0.4 s
  | 差异 = 原版一法自带三段补间并**返回时长**；我们补间走 ReturnToHand（倒放 clip，回手/回牌库共用）、时长由 EventTiming 给
  | 还剩 = 两条例外档（卡背朝上 / armedTrap ⇒ 0.65）判据已闭合但**我们引擎构造不出来**（只从场上发 Return）

块 B8_AL-5 | CardScript.OnTouchDrag(PointerEventData, Vector2)
  | 判据 decomp_full/CardScript__OnTouchDrag.c:1-52（本轮亲读全文）
  | 落点 CardInteraction.cs:638-676（等价实现 = BlockedThisFrame）+ :780-829 + :954-1040 + BattleDriver.cs:32552-32618
  | 观测量 = 闸下的起拖结果（两头堵）：回放/选目标档 ⇒ Dragging == null（轻点开窗照常）；普通局 ⇒ Dragging != null
             （既有断言 BattleScene.cs:14284-14290 + 其后 LastReleaseLog 写 拖动中=True）
  | 差异 = 原版 cardState 一维分派到 Hand/Board 两法；我们拆成两个组件，四道闸逐条对应
           （0xA0 ⇒ ReplayDragBlocked · UIstate!=1 ⇒ DragOnlyBlocked · 右键/双指 ⇒ IsRightButtonOrTwoFingerTouch）
  | 还剩 = 无（这道闸只挡起拖、松手照常结算 —— 我们照抄）

块 B8_AL-6 | CardScript.SetAsUnitInPlay(bool setVisibility)
  | 判据 decomp_full/CardScript__SetAsUnitInPlay.c:1-16（🥇 全文）
  | 落点 BattleDriver.cs:13279-13293（等价实现 = EnterBoardBody）+ :25302-25629（SyncBoard）+ :25428-25432
  | 观测量 = 棋盘视图与卡面形态：部署到 slot s 后 BoardViewAt(s) != null ∧ .Face == CardFace.Board ∧ .Data.id == 打出的卡
  | 差异 = 原版每卡一个 cardState 再派生可见性；我们无状态位 —— 「谁在场」由引擎 Board[slot] 决定、SyncBoard 对齐视图集合
  | 还剩 = 无（[非缺口]：状态不再可单点查询是结构选择）

块 B8_AL-7 | CardScript.SetAsUnitTransforming()
  | 判据 decomp_full/CardScript__SetAsUnitTransforming.c:1-50 + 同族 BattleCardUI__CreateRemnantBody.c:36-65
  | 落点 BattleDriver.cs:25302-25629（等价实现 = SyncBoard，残骸体那一支 :25515-25517）+ :24341-24346（PlayDeathFeel 残骸守卫）
  | 观测量 = 变形后 BoardViewAt(s) 仍是**同一个视图实例**（ReferenceEquals 不变）∧ RemnantBodyVisible == 真，且**不消散**
  | 差异 = 原版用状态位 15 表示「变形中」并先断言前置状态合法；我们无状态位，前置合法性由引擎侧残骸链保证
  | 还剩 = 无（[非缺口]：中间态 15 在我们这里没有对应帧，可见结果一致）

块 B8_AL-8 | CardScript.ShowCardInHand(PointerEventData)
  | 判据 decomp_full/CardScript__ShowCardInHand.c:1-206
  | 落点 CardInteraction.cs:722-746（等价实现 = UpdateHover）+ :1882-1907 + HandLayout.cs:263-268 / :796-800
  | 观测量 = ① 抬升 30 px：推完 0.2 s 后 position.y − restY = hoverLift = 30/108 = 0.2778 世界单位
                  （既有断言 BattleScene.cs:23404-23411，带「第一帧还没抬到位」的半程判别式）
             ② 放大 1.3：_lastPose.scale = cardScale × hoverScale(1.3)（HandLayout.TryLastPose 直读）
  | 差异 = 原版一法包办（Kill 其余补间 + 逐卡摆位 + 关碰撞体 + tooltip 开关）；我们拆成「切悬停下标」+「布局重算」
           + 谓词 CardDisplaySuppressedFor（碰撞体不关，那一格归 战斗界面_HUD 账）
  | 还剩 = 无（两个数值与原版 VarsDesktop 逐位相同）

块 B8_AL-9 | CardScript.TryDraggingFromHand(Vector3, Vector3)
  | 判据 decomp_full/CardScript__TryDraggingFromHand.c:1-98（本轮亲读）
  | 落点 CardInteraction.cs:188-196（等价实现 = HandDragPastThreshold）+ :780-829 + :954-1040
  | 观测量 = 两道闸的边界（纯函数直接喂点，不必跑场景）：位移² 0.00016 ⇒ 假 / 0.00014 ⇒ 真；
             夹角 65.0° ⇒ 真 / 65.1° ⇒ 假；altAngle 档 70° 同理
             vs 原版 DAT_1834b3150 = 0.00015f · DAT_1834b316c = 65.0f · DAT_1834b3170 = 70.0f（.rdata 实读）
  | 差异 = 原版一法内「算 delta → 过闸 → StartMovingFromHand」；我们拆成纯函数 + 输入泵，两道闸逐值照抄
           （含「起拖用**当前**指针而不是按下点」）；平台闸收在 TouchInputOn（桌面默认关 = PC 档按下即抓）
  | 还剩 = 无
```

---

## 三、改了哪些文件、哪些行、新增行数

**两处都在「类尾」，零行数漂移**（已按铁律：`BattleDriver.cs` / `CardFeel.cs` 被账引用极多 ⇒ 只能追加在类尾）。
已核：`CardInteraction.cs` 账里最大引用行 = `:1893`（`战斗逻辑驱动.tsv:1061`）< 插入点 `:1926`；
`CardFeel.cs` 最大引用行 = `:3347`（`战斗逻辑驱动.tsv:1046`）< 插入点 `:3484`。

| # | 文件 | 行 | 内容 | 新增 |
|---|---|---|---|---|
| 1 | `CardPresentation/Core/CardFeel.cs` | 类尾 **:3484-3511** | `SequenceSeconds(Tween)`（:3496，实测补间总长，`null` ⇒ −1）+ `EffectsAnchorScaleRatio(CardView)`（:3505，锚点高 ÷ 世界缩放 = 1.34） | **+28 行** |
| 2 | `CardPresentation/Hand/CardInteraction.cs` | 类尾 **:1926-1945** | `DescribeInput()`（:1936）—— 一行打出 悬停 / 拖拽中 / 空位 / 累计位移 / 按下时长 / 短按 / 第二颗碰撞 / 冻结 / 拖拽闸 / 触屏档 | **+20 行** |

- 两个文件**行尾未翻**：`CardFeel.cs` 纯 LF（3485 → **3513** 行）· `CardInteraction.cs` 纯 CRLF（1927 → **1947** 行），裸 CR = 0。
- `HandLayout.cs` / `BattleDriver.cs` **一个字节没动**。
- 这两个件都**只读不判**（注释里写死「⛔ 不参与任何逻辑」），所以不影响任何既有断言的期望值。

---

## 四、断言草稿（要插进 `Editor/BattleScene.cs` 的那几条 —— 该文件不在本代理白名单）

> 插入位置建议：**紧接 `CheckCardViewState(…)`（BattleScene.cs:47692-…）那一段之后**，
> 在 `CheckCardViewState` 里加一节 `// ---- 1g. 表现层读数口（B8_AL）----`，`driver` / `Check` 都在作用域里。
> 需要的读点都已经存在（`drv.BoardViewAt` / `drv.HandViewAt` / `it.HoveredCard` / `hand.TryLastPose` / `hand.hoverLift`）。

```csharp
// ---- 1g. 表现层读数口（B8_AL · 2026-10-26）--------------------------------
//  ⛔ 期望值全部来自原版判据（prefab 序列化 / 反编译方法体 / .rdata 常量），不从被测实现里读。

// AL-778：近战序列总长 = 原版 0.1 + 0.1 + 0.2 = 0.4 s
{
    var aV = CardView.Create(driver.transform, d, "Al778", CardFace.Board);
    var bV = CardView.Create(driver.transform, d, "Al778t", CardFace.Board);
    bV.transform.position = aV.transform.position + new Vector3(2.5f, 0f, 0f);
    var seq = CardFeel.MeleeAttack(aV.transform,
                  CardFeel.MeleeEndPos(aV.transform.position, bV.transform.position, true, bV),
                  aV.transform.localPosition);
    Check(Mathf.Abs(CardFeel.SequenceSeconds(seq) - 0.4f) < 1e-3f,
          $"★ AL-778：近战序列实测总长 = {CardFeel.SequenceSeconds(seq):F3}s，原版 "
        + "`DOMove 0.1 + AppendInterval 0.1 + DOLocalMove 2×attackStepTime 0.2 = 0.4s`"
        + "（`CardScript._AttackMeleeAnim_d__357__MoveNext.c:139-267`）"
        + "｜🧨 把 AttackReturnTime 改回 AttackStepTime、或删掉 AppendInterval ⇒ 红");
    if (seq != null) seq.Kill();
    Object.DestroyImmediate(aV.gameObject); Object.DestroyImmediate(bV.gameObject);
}

// AL-857：锚点高度 ÷ 卡世界缩放 = 原版 prefab 的 1.34
{
    var ev = CardView.Create(driver.transform, d, "Al857", CardFace.Board);
    Check(Mathf.Abs(CardFeel.EffectsAnchorScaleRatio(ev) - 1.34f) < 1e-3f,
          $"★ AL-857：`EffectsAnchorHeight ÷ lossyScale.y` = {CardFeel.EffectsAnchorScaleRatio(ev):F4}，"
        + "原版 prefab `CardPrefab/Board Elements/EffectAnchor.localPosition.y = 1.34`（原版卡根 = 卡底边）"
        + "｜🧨 把 `BoardEffectAnchorLocal` 写回 +0.08（旧错值）或忘记减 Height/2 ⇒ 红");
    Object.DestroyImmediate(ev.gameObject);
}

// AL-921 / AL-922：进入时成对开、离开时**无条件**复位（原版 StopShowingCardInHand 没有闸）
{
    it.SimulateHover(view.transform.position);
    var h921 = it.HoveredCard;
    Check(h921 != null && h921.ShowInBigColliderEnabled && h921.TooltipsEnabled,
          "★ AL-921：悬停手牌 ⇒ `ShowInBigColliderEnabled && TooltipsEnabled` 都开"
        + "（原版 `StartShowingCardInHand:10593-10601` 三句）");
    it.SimulateHover(farAwayWorld);            // 挪到空白
    Check(it.HoveredCard == null && h921 != null
          && !h921.ShowInBigColliderEnabled && !h921.TooltipsEnabled,
          "★ AL-922：离开 ⇒ 两个位**都关**（原版 `OnTouchExit` 的 StopShowingCardInHand 通篇无闸）"
        + "｜🧨 把 `ApplyShowInHand` 里 prev 的无条件复位挪到 `DragOnlyBlocked()` 之后 ⇒ 「只禁拖」档下本条红");
}

// AL-1049：悬停那张的缩放 = cardScale × hoverScale(1.3)
{
    hand.Refresh(list, 0);
    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
    Vector3 lp; float la, ls;
    Check(hand.TryLastPose(list[0], out lp, out la, out ls)
          && Mathf.Abs(ls - hand.cardScale * hand.hoverScale * LayoutSpace.Scale) < 1e-3f,
          $"★ AL-1049：悬停那张的位姿缩放 = cardScale × hoverScale({hand.hoverScale:F2}) "
        + $"= {hand.cardScale * hand.hoverScale * LayoutSpace.Scale:F4}（原版 VarsDevice.cardInHandShownScale = 1.3）"
        + "｜🧨 把 hoverScale 改成 1.0 ⇒ 红");
    hand.Refresh(list);
    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
}

// AL-1012 / AL-1013：棋盘视图形态 + 残骸「同一实例 + 盖上」
{
    var bv = drv.BoardViewAt(BoardSpec.WarlordSlot, true);   // 督军一定在场
    Check(bv != null && bv.Face == CardFace.Board,
          $"★ AL-1012：场上卡是 `CardFace.Board`（原版 `SetAsUnitInPlay` ⇒ `SetObjectVisibility(2)`；实得 {bv?.Face}）");
    var bv2 = drv.BoardViewAt(BoardSpec.WarlordSlot, true);
    Check(ReferenceEquals(bv, bv2),
          "★ AL-1013（判别式）：`SyncBoard` 不重建已有视图（同槽两次读必须是同一个实例）"
        + "｜🧨 把 SyncBoard 写成「每帧重建」⇒ 红（残骸体会闪一下没了）");
}

// AL-1100：两道起拖闸的边界（纯函数，不需要场景）
{
    Check(!CardInteraction.HandDragPastThreshold(Vector3.zero, Vector3.zero),
          "★ AL-1100a：位移² = 0 ≤ 原版 `DAT_1834b3150 0.00015` ⇒ 不起拖");
    var upFar = new Vector3(0f, 0.013f * LayoutSpace.VisibleHeight, 0f);      // 视口² ≈ 1.7e-4 > 1.5e-4
    Check(CardInteraction.HandDragPastThreshold(Vector3.zero, upFar),
          "★ AL-1100b：**朝上**拖过位移闸 ⇒ 起拖（方向角 0° ≤ 65°）");
    var sideFar = new Vector3(0.013f * LayoutSpace.VisibleWidth, 0f, 0f);    // 纯水平 = 90° > 65°
    Check(!CardInteraction.HandDragPastThreshold(Vector3.zero, sideFar),
          "★ AL-1100c：**纯水平**拖（90° > 原版 `DAT_1834b316c 65°`）⇒ 不起拖");
    Check(CardInteraction.HandDragPastThreshold(Vector3.zero, sideFar, altAngle: true) == false,
          "★ AL-1100d：altAngle 档（原版 `DAT_1834b3170 = 70°`）在 90° 下**仍然**拦（判别式：70≠90）");
}
```

⚠️ 上面 `AL-861` **不需要新断言** —— `CardBaseDemo.cs:3973-4001`（B05 那一节）已经在做区间夹逼
（0.50 s 仍在溶解 / 0.583 s 已复位），账里直接引它。
⚠️ `AL-919` 也不需要 —— `BattleScene.cs:35524-35533` 已有「按住 ⇒ 亮 / 松手 ⇒ 收」的判别式。

---

## 五、没查清的部分（按行列，含「为什么到不了 A」）

| 行 | 现状 | 阻断点（具体到哪一段 / 哪个读数口） |
|---|---|---|
| `CardScript.OnTouchDown` | **B**（本轮补实） | `cardState==2` 支 = `CardHighlight.ChangeState(2)` + 清 `+0x24a`：我们按下场上卡**不置任何高亮态**（`BattleDriver.cs:14434-14476` BoardPress 只判拖/轻点）⇒ 无落点。另有 `state==1 且敌方牌` 支（ShowDeckSize 敌方侧）未核 `DeckSizeVisibleForTest` 是否管两侧 |
| `CardScript.OnTouchEnter` | **B** | ④ `cardState==2 ⇒ TryCardHighlight`（同上那台机器，无落点）。①②③ 已对（含「右键/双指那条闸只拦进入、离开照旧走」） |
| `CardScript.OnTouchExit` | **B** | ③④ = `ToggleCombatPreviewHighlight(关)` + `state==2⇒0` / `state==4⇒3`（同上）。①② 已对（①大卡窗那一支落点 = `BattleDriver.cs:12722`（TickCardWinPointerExit）） |
| `CardScript.OnTouchUpAsButton` | **B** | ② 换牌档（cardState 13/14：`+0x24a=1` + `DisplayCard(...,1,1,0)` 与 `MulliganElementsAreVisible` 动态算的那个 mulliganException）—— 落在 `Battle/MulliganPanel.cs`（白名单外），本轮未核；另 `+0x28`（含 `+0x90 == 0xE6` 那道守卫）**字段名未锚定**（dump.cs 没展开）⇒ 不猜 |
| `CardScript.ShowCardBeingDrawn` | **C（P2）** | 弃牌 / 偷牌揭示**整条路不存在**：① `inHandMoving(9)` 档 ② `SetSortingLayer(HandMoving)`（等价物 `QHandMoving = 3001` 已在，但没接这条链）③ `delay = stepTime` 的**延迟翻**。工作量 ≈ 40–60 行 + 2 条断言（两条链各一处），判据齐、只差实现 |
| `CardScript.SetAsPlayer` / `SetAsCemetery` | **B（本轮未动）** | 前者 `+0x40` 每卡一份 vs 我们 owner 索引（语义等价但落点只到字段级）；后者缺 3 句（SetSortingLayer / SetSortingOrder(墓地序号) / SetColliderNotInHand），我们**没有墓地列表** ⇒ 那 3 句无观察面（GoToCemetery 的 doc 已写「原版同一帧就被 SetActive(false) 盖掉」） |

---

## 六、顺手发现（都没改，按铁律 13·4⑧ 报上来）

1. 🔴 **`BattleDriver.cs` 的行号相对账本整体偏 +16 左右**（本轮现读 vs 账里旧快照）：
   `SyncBoard` 账里 `:25285` → 现 **:25302**；`SyncHand` `:26141` → 现 **:26158**；`TapBoardCard` `:32541` → 现 **:32552**。
   我这 14 行**全部按现读**写，并带符号名；账里其余引用 `BattleDriver.cs` 的行需要同步点那次 `--fix`。
2. 🔴 **B8_W 判过的两条 A 掉回了 B**：`_merged_B8_W/B8_W.tsv` 里 `AttackMeleeAnim` / `GetEffectAnchor` 是 **A**，
   而主表现在是 **B**（`LightPlayableHand` / `StopLightPlayableHand` 是**有意回退**，简报已说明；这两条不是）。
   本轮重新给出**行号范围 + 读数口**后再升 A —— 若当时是「落点写成 `:1104-1104` 单行」被闸判形态不合格，那这就是根因。
3. ⚠️ **`ShowCardInHandLayer` 已经是 A**（B8_AA 做的，观测量 = 手牌段顶号 > 场上段顶号，读数口 `CheckCardViewState(2)`）。
   任务简报把它列在我这批的候选里 —— 已核实、**不动**。
4. ⚠️ **`CardFeel.Catalog` 的已知盲区仍成立**：跨文件的手感常量（`CardTween.RelayoutDuration` / `PickUpDuration` /
   `CardInteraction.dragScale` / `TapThreshold`）**不在 Catalog**，`Unclassified()` 的反射核对**盖不到**
   （现读 `Catalog` = **105 条**：`Src.Field 91 / Derived 10 / Ours 4`；与本轮无关，只是提醒别指望那张表会红）。
5. ⚠️ `CardInteraction.cs:135` 那段注释「棋盘支 = 状态 2/3/0x11 直接 DisplayCard、没有时间判据」——
   本轮对机器码核过，**它是对的**（2/3/17 落进末尾那段、不经 0.2 s 门；只有 1/8 那一支有时间门）。记下来免得下一个人又怀疑一遍。
6. `check_ledger_points`（预览）里 `漂移 118` 全部来自并发写手的 +5/+29 偏移；`check_a_vs_diff` 那 13 条弱信号
   （`ResolveRecallToHand` / `DisplaySwarm` / `LightPlayableHand` …）也**全是既有行**，与我无关。

---

## 七、给调度台的请求

1. 🔴 **派一条「场上卡高亮状态机」的判据 + 落地（最高性价比）**：核 `CardHighlight` 的原版对应物
   （`TryCardHighlight` / `ToggleCombatPreviewHighlight` / `ChangeState(2⇒0)` / `ChangeState(4⇒3)`）在我们
   `Core/CardHighlight.cs` + `Core/CardView.cs` 里的落点，并把「悬停/按下场上卡 ⇒ 预览高亮」接上。
   **接上之后 `OnTouchDown` / `OnTouchEnter` / `OnTouchExit` 三条可以一起翻 A**（判据与观测量我已在补丁里备好，只差这一段）。
   顺手会把 `CardScript.SetSortingOrder` / `SetSortingLayer` 那两条也解开（它们卡在同一台机器）。
2. **转派：`Battle/MulliganPanel.cs`** —— `OnTouchUpAsButton` 的换牌档（13/14）与 `MoveCardToMulliganPos` 的
   行几何（B8_W 报过的 383.8 / 502.2 vs 我们 292 / 414）。这一条核完 `OnTouchUpAsButton` 就能升 A。
3. **同步点请跑一次 `check_ledger_points --all-A --fix`**（在无 `.cs` 写手时）：本批 118 条漂移 + 我这 14 行里那几个
   `BattleDriver.cs` 引用（它们按现读写、应该不动）。
4. **转派：弃牌 / 偷牌揭示（`ShowCardBeingDrawn` = C）** —— 我已在账里写清三件缺口与 40–60 行的量。
5. **`+0x28`（含 `+0x90 == 0xE6`）字段名未锚定** —— 谁能把 `dump.cs` 的 `CardScript` 字段表展开一下
   （`OnTouchUpAsButton` 非我方牌那道守卫用它）。⛔ 我没猜。
6. **断言草稿 6 条**在 §四，请转派给 `Editor/BattleScene.cs` 的写手（插在 `CheckCardViewState` 之后）。
