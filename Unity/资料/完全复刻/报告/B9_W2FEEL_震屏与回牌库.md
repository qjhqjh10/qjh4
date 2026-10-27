# B9_W2FEEL —— 震屏与回牌库（施工单 #14 / #15 / #16）

> 执行代理 `B9_W2FEEL` · 2026-10-27 · 写文件白名单内一切改动只落在
> `Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs`（+ 本报告 + 账补丁 `账/_parts/B9_W2FEEL.tsv`）。
> typecheck：`TMPDIR=/tmp/wf_w2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ⚠️ **复核说明**：上面那次 0/0 是本件改完**当场**跑的。之后同一工作树里**并发代理**在写
> `Editor/CardFaceAssertE5.cs`（W4）与 `CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs`（W5），
> 再跑类型检查时两边的错会互相串（实测一次 198 条、一次 76 条，**全部落在那两个文件**）。
> 判据（CLAUDE.md §13·3）：`grep -c CardFeel /tmp/w2_tc.txt` = **0** —— 本件没有编译错误。
> ⛔ 未跑任何 Unity（无 `-executeMethod` / 无 `_run_8_checks.sh`）· 未动 git。

---

## 一、结论

| # | 施工单条目 | 原版值 → 我们值 → 改后值 | 本轮结论 |
|---|---|---|---|
| 14 | `BattleManager.ResolveUnitShake` | 原版：`delay → IsInPlay → (击退 \| 抖动)` 四步；我们：只有 `ShakeUnit`（一个裸 `DOShakePosition` 叶子），**前面三步整段没有**，且档位映射 `>=3→8` 与原版 `==3→8`（其余落回 2）不符 → 改后：`CardFeel.ResolveUnitShake` 四步全落地；`ShakeScaleOfUnitSize` 的 `>=` 改 `==` | **已落地（B 档：缺调用方/数据）** |
| 15 | `CardScript.ReturnToDeckAnim` | 原版：`状态位 → (敌方换卡背 \| 我方翻面) → 三件补间(OutQuart) → 等 → 关层`；我们：**整条没有**（只有 `ReturnToHand`）→ 改后：`CardFeel.ReturnToDeck` 逐句实现 | **补间本体已落地（B 档：缺牌库参考件 + 调用方）** |
| 16 | `CardScript.ResolveAttackAnimationEffects` | 原版 `SetAmplitude = 2.0 / 4.0 / 8.0`（0/1 档同值、其余落回 2.0）；我们 `ShakeAmpUnitSize1/2/3 = 2.0 / 4.0 / 8.0` → 改后：**数值不变**（逐格已对上） | **数值已对齐；另查出 `SetDirection` 那一格的真偏离（见 §三·1）** |

一句话：**14 与 15 是「原版有、我们整段没有」，本轮把本体补进 `CardFeel`（判据逐句带行号）；
16 是施工单判错了 —— 三个幅度常量本来就是 2/4/8，本轮只做实读复核 + 修掉一个镜像件的分支形状，
真正对不上的是同一方法的 `SetDirection`（原版喂的是目标的棋盘坐标，不是方向向量）。**

---

## 二、证据（文件:行号）

### #16 三档幅度的实读（先把「值到底是多少」钉死）

`decomp_full/CardScript__ResolveAttackAnimationEffects.c:30-52`：

```
iVar2 = SupportMethods__GetUnitSize(param_1,0);
if ((iVar2 == 0) || (iVar2 == 1)) { uVar8 = DAT_1834b2bbc; }        // LAB_1805f8b18
else if (iVar2 == 2)              { uVar8 = DAT_1834b2df0; }
else { if (iVar2 != 3) goto LAB_1805f8b18; uVar8 = DAT_1834b2e00; } // 非 3 ⇒ 落回 0/1 那一支
...
OverwriteCameraShakePreset__SetAmplitude(preset, uVar8, 0);
```

`工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2bbc 0x1834b2df0 0x1834b2e00`：

| 地址 | 4B float | 8B double | 取哪个 |
|---|---|---|---|
| `0x1834b2bbc` | **2.0** | 1.39612e+17（不像） | 4B |
| `0x1834b2df0` | **4.0** | 2048（**两档都像**） | 4B —— `SetAmplitude(longlong, undefined4, …)` 的实参是 **4 字节**（`OverwriteCameraShakePreset__SetAmplitude.c:5` 存 `*(undefined4 *)(param_1 + 0x28)`） |
| `0x1834b2e00` | **8.0** | 2.19902e+13（不像） | 4B |

⇒ **2 / 4 / 8，逐格与 `CardFeel.ShakeAmpUnitSize1/2/3`（`CardFeel.cs:240-242`）一致**；
`ShakeAmplitudeForUnitSize`（`:309`）的分支形状也已经是 `== 3`（同日 `B9_E2UDW` 改的）。

### #16 真正对不上的那一格：`SetDirection` 收到的是「坐标」不是「方向」

- `decomp_full/CardScript__ResolveAttackAnimationEffects.c:51-58`：`SetDirection(preset, *param_3)` → `SetAmplitude` → `Play()`。
- 唯一调用方 `decomp_full/CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:14-16`：
  `ResolveAttackAnimationEffects(card, card.CardUI(+0x290).+0x1a0, &local_28 /* 闭包捕获的那个 Vector3 */, 0)`。
- 那个 Vector3 的来历 = `decomp_full/BattleManager._ResolveAttack_d__438__MoveNext.c:898-901`：
  `GetBoardPosOfCard(bm, targetCard)` → `AttackMeleeAnim(attacker, &pos, targetCard)`（`CardScript__AttackMeleeAnim.c:18-22` 把它存进闭包 `+0x30`）。
- `decomp_full/BattleManager__GetBoardPosOfCard.c`：返回 **`(card.transform.position.x, 0, card.transform.position.z)`**
  （ILSpy `BattleManager.cs:47701-47708` 明确只写 x/z，y 留默认 0；卡为空时取 `Vector3.zeroVector`）。
- 消费端 `decomp_full/CameraShakerManager._DoShake_d__33__MoveNext.c:44-56`：
  `fVar11 = direction.magnitude` → `velocity = Normalize(direction)` → `m_AmplitudeGain = fVar11 × amplitude`。

⇒ 原版攻击震屏的输入是**目标所在的棋盘坐标**，两处后果：
`m_Velocity` 的 y 分量恒 0（⇒ 按本工程的 2D 模型 `ShakeScreenUpOf` 算出来**竖直分量 = 0**），
`m_AmplitudeGain` 被「目标离棋盘原点多远」缩放。

### #14 `ResolveUnitShake` 的四步（`decomp_full/BattleManager._ResolveUnitShake_d__472__MoveNext.c`）

| 步 | 行 | 原版做了什么 |
|---|---|---|
| ① | `:39-44` | `fVar12 = *(float*)(data + 0x28); if (0 < fVar12) yield return new WaitForSeconds(fVar12)` |
| ② | `:48-70` | 协程自身那一格非空 + `data.target(+0x10)` 非空 |
| ③ | `:72-75` | `CardScript__IsInPlay(target)` —— `CardScript__IsInPlay.c` = `cardState ∈ {2 inPlay, 3 inPlayAttacking, 15 inPlayTransforming, 17 inPlayAminingAttack}` |
| ④a | `:77-160` | `data.awayFrom(+0x18) != null && != target` ⇒ `dir = normalize(target.pos − away.pos)`；`|dir| ≤ DAT_1834b2f44` 取静态常量向量；`DoPushBack(target, GetPushBackFactor(data.shakeSize), dir, …)` |
| ④b | `:161-181` | 否则 `amp = (0/1→2 · 2→4 · 3→8 · 其余→2)`，`DoUnitShake(target, amp, data.shakeDuration)` |

- `DAT_1834b2f44` 4B float 实读 = **1e-5**（`0x1834b2f48` = 0.0001，别混）。
- 那个静态常量向量：`DAT_1842da2a8 + 0xb8` 是 `UnityEngine.Vector3` 的 Il2CppClass 静态块首格；
  ILSpy 在同一格上给出名字（`BattleManager.GetBoardPosOfCard` 的 null 支：`x = (float)Vector3.zeroVector;`）
  ⇒ **`Vector3.zero`**。⚠️ 本文件 `PushBackDir` 现在的退路是 `Vector3.up`（见 §五·1）。
- `data` 五个字段的来历：`decomp_full/UnitShake__.ctor.c` —— `+0x10 = GetTargetUnit(info+0x10)` ·
  `+0x18 = GetTargetUnit(info+0x14)` · `+0x20 = info+0x18` · `+0x24 = info+0x1c` · `+0x28 = info+0x20`，
  与本地 schema `unitsShake[{ targetUnit, awayFromUnit, shakeSize, shakeDuration, shakeDelay }]` 逐字段同形。

### #15 `ReturnToDeckAnim`（真体在状态机里，`.c` 那份只是工厂）

- 工厂：`decomp_full/CardScript__ReturnToDeckAnim.c`（22 行，只 `new d__248(0)` + 写 `timeToMove`）——
  **上一轮记「`.c` 体薄，疑体丢」是误判**：真体在
  `decomp_full/CardScript._ReturnToDeckAnim_d__248__MoveNext.c:1-142`（与 CLAUDE.md 那条「ILSpy 丢掉 async 状态机体」同族）。
- 顺序（行号）：`:62-72` 状态位（`== 0 inDeck` ⇒ `LogError("Trying to return to deck, but it's already there ")` 并退出；
  否则置 0）· `:73-78` `!isPlayer ⇒ SetObjectVisibility(inDeck)` / `isPlayer ⇒ TurnCardAround(timeToMove)` ·
  `:80-104` 取参考件（`DeckManager.GetReferencePosition()` = **world position**、
  `GetReferenceScale()` = **(localScale, localRotation)**，见 `DeckManager__GetReferencePosition.c` / `__GetReferenceScale.c`）·
  `:105-111` 三条 `Ease.OutQuart`（枚举 0xC）补间 + 移动那条 `OnComplete(SetInDeckPosition)` ·
  `:113-118` `WaitForSeconds(timeToMove)` · `:38-52` `if (CanContinueAnim()) { SetSortingLayer("Background"); SetActive(false); }`。
- 收尾 `SetInDeckPosition()`（`decomp_full/CardScript__SetInDeckPosition.c:14-40`）= 把
  `localPosition / localRotation / localScale` **照抄参考件的局部三项**；
  `CanContinueAnim()`（`CardScript__CanContinueAnim.c`）= `this != null && gameObject != null && activeSelf`。
- **唯一调用点** `decomp_full/PlayerHand._FinishMulligan_d__52__MoveNext.c:118-124`：`cardState == 0x0e(14)`
  （`inMulliganDiscarded`）逐张播，`timeToMove = *(float*)(globalVars + 0xAC)`；
  `+0xAC` = `VarsGlobal.timeToDrawEnemyCard` = **0.15**（`il2cpp_out/dump.cs:120113`；值 → `资料/VarsGlobal_原版数值.md`）
  ⇒ 就是本文件的 `DealDurationFoe`（`CardFeel.cs:3197`）。
- 我们这侧的现实：`BattleDriver.OnMulliganDone`（`Battle/BattleDriver.cs:8984-9035`）只做
  `RuleCore.Mulligan` + `EndMulligan` + `_mulligan.Close()`；`MulliganPanel.cs` 里也**没有任何回牌库补间**
  （`grep DOScale|DOMove|DOLocalRotate|TweenPose` 只命中牌位换算的注释）⇒ 换掉的牌是「面板一关就没了」。

---

## 三、没查清的部分（逐条写「还差什么」）

1. **`SetDirection` 的 2D 等价物（#16 的真缺口）** —— 判据已经查清（§二），**缺的是通道**：
   原版那一下在屏幕上主要是**水平 + 景深**（`velocity` 的 y = 0），而我们的 `ShakeCamera`
   （`CardFeel.cs:1382`）只建模「屏幕竖直」一个通道（`ShakeScreenUpOf`）。照抄输入 ⇒ `screenUp = 0` ⇒ 画面**完全不震**。
   要做完，得先给 `ShakeCamera` 加**屏幕水平**通道（`pos = R·(0,s,0)` 的 X 分量）并把 `worldAmp` / 旋转 gain
   改成按 `|direction|` 缩放 —— 那是「震屏模型」的一次改动，**本轮刻意只做一半的相反**：不动，写清。
   ⇒ 建议立一条独立账（`CardFeel.ShakeCamera` 的水平通道 + `ShakeCameraForHit` 接收攻击坐标）。
2. **`SetSortingLayer("Background")` 的等价物** —— 我们的排序模型是「一张卡一段 renderQueue」
   （`CardView.QCard` 那段号段约束：3000 是上限、往 2980 排），**没有 `Background` 这个层**。
   本方法的可观察那一半（`SetActive(false)`）已照做。要不要为「牌库里的卡」留一个号段 —— **没查**：
   需要先确认原版 `Background` 的 sorting order 数值（`TagManager.asset` 里的排序层次序）以及它在我们这套里对应哪一段。
3. **`ResolveUnitShake` 协程里那个「协程自身非空」格子（`param_1 + 0x28`）到底是什么** ——
   `_d__472` 的字段只有 `state / current / unitShake(+0x20) / <>4__this`，`+0x28` 落在 `UnitShake` **之后**
   的另一个捕获位上；Ghidra 的 `op_Equality(uVar6, 0)` 说明它是个 Unity 对象引用。**没查清它是哪个参数**
   ⇒ 我们的实现用 `target == null` 覆盖了这一档（语义上等价于「目标没了就不播」），但**没有逐字对上**。
4. **`PushBackDir` 的两点重合退路（`Vector3.up`）与原版（`Vector3.zero`）不一致** —— 见 §五·1。
   改它要同步改 `Editor/BattleScene.cs:13439`（`Check(CardFeel.PushBackDir(x,x) == Vector3.up, …)`），
   **那个文件不在本件白名单** ⇒ 只记账、未改。
5. **`unitsShake` 数据本地不存在** —— `Resources/card_anim_order.json` **没有这个字段**
   （`BattleDriver.cs:34681-34692` 已记）。⇒ `ResolveUnitShake` 现在是**0 调用点**的口。
   「原版那份数据能不能从 bundle 里挖出来」**本轮没碰**（不在施工单范围）。
6. **`TurnCardAround` 与三条补间并发写旋转** —— 原版自己就是这样（我方支：`TurnCardAround(timeToMove)`
   与 `DOLocalRotateQuaternion(toRot, timeToMove)` 同时跑）。我们照抄，但**哪个最后赢**取决于 DOTween 的
   求值顺序，**没实测**（要真机看）。如实记：这是原版的自有冲突，不是我们引入的。

---

## 四、改动清单

### 4·1 `CardPresentation/Core/CardFeel.cs`（唯一源码改动）

| 类型 | 位置 | 内容 | 行号影响 |
|---|---|---|---|
| 改 | `:89` | `PushBackMagnitude` 的 1.0/1.5/0.5 收口到 `PushBackFactorOfSize(size)` | 1→1（无漂移） |
| 改 | `:1281-1284` | `PunchBack` 的 `DOPunchPosition` 形状收口到 `PushBackByFactor` | 3→3（无漂移） |
| 改 | `:3785` | `ShakeScaleOfUnitSize`：`unitSize >= 3 ? 8` → `unitSize == 3 ? 8` | 1→1（无漂移） |
| 新 | `:3841` | `internal static float PushBackFactorOfSize(int unitSize)` | 追加在类尾 |
| 新 | `:3855` | `internal static Tween PushBackByFactor(Transform, Vector3, float, float)` | 追加在类尾 |
| 新 | `:3906` | `public static Tween ResolveUnitShake(CardView, CardView, int, float, float, Func<bool>)` | 追加在类尾 |
| 新 | `:3921` | `static Tween ResolveUnitShakeNow(…)`（②③④） | 追加在类尾 |
| 新 | `:3990` | `public static Tween ReturnToDeck(CardView, Transform, float, bool, Texture2D, Action)` | 追加在类尾 |
| 注 | `:4049` | 类尾 #16 注记（三个 `_DAT_` 实读 + `SetDirection` 发现） | 追加在类尾 |

行号守恒证据：`git diff -U0 -- Assets/CardPresentation/Core/CardFeel.cs` 的四个 hunk =
`@@ -89 +89 @@` · `@@ -1282,3 +1282,3 @@` · `@@ -3785 +3785 @@` · `@@ -3822,0 +3823,247 @@`（**纯尾部追加**）。
新增常量一律 `internal`（不是 `public const`）—— 理由是 `CardFeel.Unclassified()`（`:825`）会把
**未登记的 `public const float/int`** 判红，而往 `Catalog` 里插行会让下游账落点漂移（先例：
`DissolveCardTime` 那条注释 `:3536-3539` 就是同一个理由）。

### 4·2 账补丁 `Unity/资料/完全复刻/账/_parts/B9_W2FEEL.tsv`（4 行，11 列，LF）
`ResolveUnitShake`（B）· `ResolveAttackAnimationEffects`（B，数值改为「已对齐 + 记 `SetDirection` 偏离）·
`ReturnToDeckAnim`（B，落点改 `CardFeel.cs:3990`）· `DoUnitShake`（**C → A**，落点从 `UnitTweenTable.cs` 改回 `CardFeel.cs`）。
`check_patch_conflicts.py`：**没有跨补丁重复键（2 份补丁）**。
⚠️ 合并时请**整份 `--only B9_W2FEEL`**（4 行都是**替换**同键行，不是新增）。

### 4·3 给 W1（`BattleDriver.cs`）的接口变化

**没有任何既有签名被改**（`ShakeCameraForHit` / `ShakeUnit` / `ShakeAmplitudeForUnitSize` / `ShakeCameraForLand`
的形参表逐字未变）。可用的两个**新**入口：

```csharp
// ① 一拍里的「抖 / 弹」二选一（原版 _d__472）。数据 schema = unitsShake 的五个字段。
CardFeel.ResolveUnitShake(CardView target, CardView awayFrom, int shakeSize,
                          float shakeDuration, float shakeDelay = 0f,
                          System.Func<bool> stillInPlay = null);   // null ⇒ 回退 activeSelf

// ② 回牌库补间（原版 ReturnToDeckAnim）。deckRef = 牌库参考件（带位姿的那张卡，不是牌堆图）。
CardFeel.ReturnToDeck(CardView card, Transform deckRef, float timeToMove,   // 原版那唯一一处传 0.15
                      bool isPlayer = true, Texture2D cardBack = null,      // 敌方支要卡背贴图
                      System.Action onDone = null);
```

⚠️ **行为上唯一变了一处**：`ShakeScaleOfUnitSize(unitSize)` 在 `unitSize >= 4` 时从 `8` 变 `2`
（原版非 0/1/2/3 一律落回 2）。W1 现在传的是 `ShakeUnitSizeOf(melee)`（只可能 0/2/3）⇒ **线上行为不变**。
⚠️ `ApplyScale` / `_baseScale` 相关：`ReturnToDeck` 走原版形状（`tr.DOScale` 直写 `localScale`，**不过** `CardView.TweenPose`）。
理由是这条补间跑完那张卡**当场 `SetActive(false)`**、之后按「牌库里的卡」重建视图；若将来有人在补间途中对同一张卡 `SetPose`，
会与 `CardTween.ToPose` 被删那次的坑同形（`CardTween.cs:62-76`）。

---

## 五、顺手发现（都能复核；⛔ 都不在本件白名单里改完的）

1. 🔴 **`PushBackDir` 的两点重合退路写的是 `Vector3.up`，原版是 `Vector3.zero`** ——
   `CardFeel.cs:1221-1228` 的实现在 `len ≤ 1e-5` 时 `return Vector3.up`，而它的注释自己写着
   「原版的静态常量**读出不来**（沿用 `Flat` 的 `Vector3.up`）」。
   **那句「读不出来」已经不成立**：同一个静态格 `DAT_1842da2a8 + 0xb8` 在本文件 **`:3234`** 的注释里
   早已被认定是 `UnityEngine.Vector3` 静态块首格 = `zero`（`字段表 zero 0x0 / one 0xC / up 0x18`），
   而 ILSpy 在 `BattleManager.GetBoardPosOfCard` 的 null 支直接给出名字 `Vector3.zeroVector`。
   ⇒ **同一条判据在本文件里有两个说法**（`:3234` 说 zero、`:1214` 说读不出来）—— 按铁律 5 该就地统一，
   但改值会让 `Editor/BattleScene.cs:13439` 那条断言变红（它钉着 `== Vector3.up`），那个文件不在白名单
   ⇒ **记账**（已写进 §三·4 与该账行的差异列）。
2. 🔴 **账里 `CardScript.DoUnitShake` 那一行是错的**：`状态 = C`、落点指到
   `Core/UnitTweenTable.cs:495-505`（`shakeType==3 ⇒ DOShakeScale`）。那是 **AnimFX 的数据驱动表**，
   与原版 `CardScript.DoUnitShake`（`transform.DOShakePosition` 的直接调用）是两套数据源。
   `UnitTweenTable.cs:503` 的注释已就地订正过，但账没跟着改 ⇒ 本补丁把它改成 **A**、落点改 `CardFeel.cs:3814`。
3. 🟡 **`DoPushBack` 的 `duration` 形参是死值**：C# 签名 `DoPushBack(float, Vector3, float duration, float delay = 0)`，
   而方法体里 `DOPunchPosition` 的第 3 参**写死 0.4**（`DAT_1834b3158`），`param_4` 全篇未用
   （`decomp_full/CardScript__DoPushBack.c:20-31`）。`ResolveUnitShake` 的击退支还专门把 `data+0x24`
   当 duration 传进去（`:132-135`）—— 传了也白传。**照抄，⛔ 别「修」**（已写进 `PushBackByFactor` 的 doc）。
4. 🟡 **`IsInPlay` 与 `MoveBackToHand` 用的那组 cardState 不是一回事**：`IsInPlay` = `{2,3,15,17}`；
   `MoveBackToHand` 判的是 `{10 inHandPlaying, 4 inPlay}`（`CardFeel.cs:3677`）。两条**别互相套用** ——
   本轮特意把它们分开查过（`CardScript__IsInPlay.c` 全文 12 行）。
5. 🟡 **`ReturnToDeckAnim` 的 `.c` 是工厂、真体在 `_d__248__MoveNext.c`** —— 与 CLAUDE.md 里
   「ILSpy/Cpp2IL 丢 async 状态机体」是同族坑的反面（这次是**找对了文件名**就全在）。
   建议后续看到「`.c` 只有一句 return / 一个构造」时，**先找 `<Method>_d__NNN__MoveNext.c`**。
6. 🟡 **`CardScript.MoveToDeck` 与 `ReturnToDeckAnim` 是两条不同的事**（账里两行都在）：
   `MoveToDeck` 服务于 `ResolveRecallToDeck`（场上卡被送回牌库，带 delay、返回值 0.4 s），
   `ReturnToDeckAnim` 只服务于**换牌**（`PlayerHand._FinishMulligan`，0.15 s）。
   ⛔ 别拿一条的实现去顶另一条 —— 本轮只做后者。
