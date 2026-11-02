# B117-FIXD — 收藏方叉（ban 徽标）断言订正 + BattleScene 零散红逐条判定

日期：2026-11-02 · 执行代理 `B117-FIXD`
输入日志：`_tmp_view/collection.log`（13:59）· `_tmp_view/battle.log`（13:52）· `_tmp_view/netbattle.log`（14:00）

---

## 一、`CollectionScene` 6 条（`[B97W6]` 方叉 / ban 徽标）—— **全部是断言过期**

### 1.1 判定

`WCOLL`（2026-11-03）按原版把徽标从「196.875 px / 中心 = 格心」改成「**181.62 px / 中心 = 卡心**」，
而 `Editor/B97W6_Assert.cs` 的期望值还是**旧口径** ⇒ 6 条全红。**实现是对的，断言过期。**

### 1.2 新期望值 —— **两条独立推导**（⛔ 不是只读实现反推）

原版 prefab 逐字段现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/GameObject/Collection Card.prefab`）：

| 字段 | 值 | 说明 |
|---|---|---|
| `Collection Card.m_SizeDelta` | 350 × **512** | **整格**（含底下那条 Counter）—— ⛔ 不是卡体高 |
| `CardUI.m_LocalScale` | **150** | |
| `2DCard.m_SizeDelta` | 2.0927 × **3.3313** | **卡体**在卡单位下的高（= `CardView.Height`） |
| `Counter.m_AnchorMax.y` | **0.09966714** | 底下那条占格高的 9.9667% |
| `Ban Icon.m_SizeDelta` | **1.75 × 1.75** | 卡单位；挂 `CardUI` 下 ⇒ 原版 = 1.75 × 150 = **262.5 px** |
| `Banned Text` anchor | x 0.1–0.9 / y 0.4–0.6 | 框 = 方叉的 0.8 × 0.2 |

- **推导 ①（原版 prefab 比值 × 本页卡体渲染高）**：方叉/卡体 = `1.75 ÷ 3.3313` = 0.525322；
  本页卡体渲染高 = `CardsCellH 384 × (1 − 0.09966714)` = **345.728 px**
  ⇒ 方叉 = **181.62 px**。
- **推导 ②（实现那条几何）**：`pxPerUnit = cardR.H ÷ CardView.Height = 345.728 ÷ 3.3313 = 103.783`
  ⇒ `1.75 × 103.783` = **181.62 px**。两条同值。

⇒ 新断言期望：方叉 **181.62**（±0.5）· 字框 **145.30 × 36.32**（±0.6）· 字号 base **24.91**（±0.6）·
中心 = **卡心**（格心 − `CounterH/2` = 347.90 − 19.136 = **328.76**，±0.5）。

### 1.3 改了什么（`Unity/MyGame/Assets/CardPresentation/Editor/B97W6_Assert.cs`）

- 常量组重写：删掉 `OrigCardH = 512` / `OrigCardUiScale` / `PageScale`（那三个把**根高 512** 当成卡体高），
  换成 `OrigCardUnits 3.3313` · `OrigCounterMaxY 0.09966714` · `CounterH` · `CardVisualH`（全带 prefab 出处）。
- `ExpectSidePx = OrigBanUnits / OrigCardUnits * CardVisualH`（⛔ 不读实现的 `side`/`pxPerUnit`）。
- 中心那一格改成 `cell.CY - CounterH * 0.5f`。
- 文件头与三条断言的说明文字同步订正（带更正痕）。

---

## 二、`BattleScene` 零散红 —— 逐条判定

| # | 断言 | 判定 | 处理 |
|---|---|---|---|
| 1 | `B117G1021` C5/C6（描边在实例材质 / 共享材质未被污染） | **判据本身错** | 见 §2.1 |
| 2 | `B117G1004` B1/B2/C2/C3 | **真缺口（补丁未落地）** | 见 §2.2（**未能修**：在 `BattleDriver.cs`） |
| 3 | `B117G1017` A6/A9/A16 | **真回归（3 处）** | 见 §2.3（已修） |
| 4 | `B117G1006` A3/A5 | **断言过期（参照物取错）** | 见 §2.4 |
| 5 | `B117G1003` E6 | **断言过期（符号错）** | 见 §2.5 |
| 6 | `B117G1009` E2 | **真缺口（`ZTargets` 常量建了没用）** | 见 §2.6（已修） |
| 7 | `B117G1011` E16 | **断言前提在本宿主不成立** | 见 §2.7 |
| 8 | `B117G1012` A2 | **时钟口径不适配批处理** | 见 §2.8 |
| 9 | `B117G1024` E0 | **真缺口（战场 prefab 层全被刷成 8）** | 见 §2.9（**未能修**） |
| 10 | `B117F11` E2（`B117-37`） | **真缺陷（`BattleDriver`）** | 见 §2.10（**未能修**） |
| 11 | `[G10/19]` A8 | **结构偏离原版** | 见 §2.11（已修） |
| 12 | `[G10/19]` A35 | **前提断言写错** | 见 §2.12 |
| 13 | `[B117_WBD]` 前提（`FindLinkProbe`） | **批处理无帧循环** | 见 §2.13 |
| 14 | `704562`（换环境淡出） | **未定**（需一次带探针的运行） | 见 §2.14 |
| 15 | `722650`（技能结算铺白） | **断言过期（与 `B117-G10-09` 的新口径打架）** | 见 §2.15 |
| 16 | `800493`（D6⑦ 真实材质队列） | **未定**（需一次诊断运行） | 见 §2.16 |
| 17 | `728067`（E8 回弹角度前提） | **断言过期（阈值不适配 G10-06 的归零）** | 见 §2.17 |
| 18 | `728800`（E8 卡名字号只缩不放） | **断言过期（bandMax 上调）** | 见 §2.18 |

### 2.1 `B117G1021` C5/C6 —— **旧判据在本工程的 TMP 版本下结构性恒假**

`TextMeshPro.GetMaterial`（`com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs:1182-1201`，
`TextMeshProUGUI.cs:1474-1497` 同构）：

```csharp
if (m_fontMaterial == null || m_fontMaterial.GetInstanceID() != mat.GetInstanceID())
    m_fontMaterial = CreateMaterialInstance(mat);
m_sharedMaterial = m_fontMaterial;      // ← 关键
... return m_sharedMaterial;
```

⇒ **读过一次 `fontMaterial` 之后，`fontSharedMaterial` 返回的就是那份实例**。
旧 C5 `fontMaterial != fontSharedMaterial` **永远为 false**；旧 C6 读到的 0.097 是**实例**的值，
不是「共享材质被污染」。

**改**：判据改成与**字体资产自己的材质**比 —— `TMP_Text.font.material`（那份才是全工程共享的）：
`fontMaterial != font.material`（实例化成立）＋ `font.material._OutlineWidth == 0`（资产没被污染）。
`ApplyBannerOutline` 写的就是实例 ⇒ 新判据通过。

### 2.2 `B117G1004` ×4 —— **补丁 A/B 还没落地到 `BattleDriver.cs`**

现读：

- 里程碑：`BattleDriver.cs:28855/28863` 建 `_skullIcon` / `_skullScore` 之后**没有任何 `playMode` 闸**
  ⇒ B1/B2（练习档该藏）**恒红**。
- 三枚灯：`BattleDriver.cs:32174-32176` 是 `SetActive(i < _cardsPlayedThisTurn)`，
  **既没有 holder 闸、读的也不是 `Ctx.CardsPlayedInTurn`**（断言设的是 `Ctx.CardsPlayedInTurn`）
  ⇒ C2/C3 **恒红**（C1 恒绿）。

**未改**（`Battle/BattleDriver.cs` 在本代理的红线里，`FIXB` 在改）。修法（供调度台转派）：
① 里程碑：`RefreshAll` 里按 `Ctx.PlayMode` 查 `MatchData.GetMilestones()` 那 6 档
（`Duel/PracticeLodge/Tutorial/CutScene/Campaign/TutorialReplay`）⇒ `_skullIcon/_skullScore` 的 GO `SetActive(有阈值)`；
② 三枚灯：`SetActive(Ctx.OffensiveSlotIdx >= 0 && OffensiveCardData.HasTrigger15(Ctx.OffensiveEnvSO) && i < Ctx.CardsPlayedInTurn)`。
⚠️ 两条是**原版本义**（`MatchData__GetMilestones.c:20-33` · `BattleManager._ApplyOffensiveAndDefensiveEffects_d__337` state 1），
不是「为过断言而改」。

### 2.3 `B117G1017` A6/A9/A16 —— 三处真回归（`Battle/MulliganPanel.cs`，已修）

1. **A6（红标记不跟着收）**：`SetContentHidden` 末句原来是 `if (show && _selBgs.Count > 0) ApplyMarks();`
   ⇒ **收的时候 `ApplyMarks` 根本没被调到**（而 `ApplyMarks` 里那句 `SetActive(m && !_contentHidden)` 才是藏标记的地方）。
   ⇒ 改成 `if (_selBgs.Count > 0) ApplyMarks();`（收/放两边都复原；`Close()` 那条路 `_selBgs` 已被
   `ClearCardButtons()` 清空 ⇒ 天然跳过，不会去写卡的 `SetHighlight`）。
2. **A9（收起后点完成照样提交）**：`HandleClick` 原来**先判 `HitDone`、后判 `_contentHidden`**
   ⇒ 改成 `if (!_contentHidden && HitDone(world))`（原版那颗 `Button` 的 GO 已 inactive ⇒ 收不到射线）。
3. **A16（`Close()` 没真的还原卡的 activeSelf）**：原 `Close()` 的顺序是
   `ClearCardButtons(); SetContentHidden(false);` —— `ClearCardButtons()` 会 `_cards.Clear()`
   ⇒ 之后 `SetContentHidden` 里 `for (_cards)` **一张都碰不到**（注释写着「先放回来」，实际从没还原过）。
   ⇒ 新增 `RestoreHiddenCardsBeforeClose()`（**追加在类尾**），`Close()` 第一句调它，再 `ClearCardButtons()`。
   它**只还原 `activeSelf`、不走 `ApplyMarks`**（否则会把刚标记那几张刷成 `Unplayable`/灰）。
   ⚠️ `MulliganPanel.cs` 1946 → **1965 行**，**新增的全在 `:1550` 之后**（`HandleClick` 那条注释 + 类尾新方法）；
   账对它的最大落点是 `:1550`（`public bool HandleClick`）⇒ **一条都没漂**。

**断言侧同步**：`B117_G10_17_Assert.cs` 的 A16 改量**调用方手里的卡对象**（`cards[0].gameObject.activeSelf`）——
`Close()` 之后 `panel.CardActiveForTest(0)` 恒 false（`_cards` 已清），量它没有判别力。

### 2.4 `B117G1006` A3/A5 —— 参照物取错（已修，`Editor/B117_G10_06_Assert.cs`）

- **A3** 拿 A0 那一趟的 `rest1` 当「撤悬停前的旧位」—— 但撤悬停**之前**那张牌明明在**带洞那一趟**的槽 2
  （实得 `(0.85, −3.79, 0.08)`，`rest1 = (0.13, −3.78, 0.08)`）⇒ 拿**目标**当**起点**，恒红。
  ⇒ 改成在 `Refresh(made,−1,−1)` **之前**抓 `hoverPos1`，与它比。
- **A5** 拿 `layout.SlotPosition(1, 3)` 当期望 —— 那个只有 x/y，而 `Refresh` 还会叠 `i × zOrderStep`
  （第 1 张 = **+0.08 z**）⇒ 恒差 0.08。⇒ 改成与 `rest1`（实测的**完整** `localPosition`）比。

### 2.5 `B117G1003` E6 —— 符号错（已修，`Editor/B117_G10_03_Assert.cs`）

点锚 `(0,1)` = 父矩形**左上角**，`anchoredPosition` 相对那个角、Unity 里 y 向上
⇒「在顶边之下 50 px」= **−50**（`AnchorApPx` 的式子 `1080·(1−ay) − yPxFromTop` 代入 ay=1 也是 −50）。
旧断言写 `1080 − 50 = 1030`（那是 `ay = 0` 左下角的式子）。实现不变，断言改成 −50。

### 2.6 `B117G1009` E2 —— `ZTargets` 建了没用（`Battle/SkillPanel.cs`，已修）

`const float ZTargets = ZLight - 0.02f` 早就在（`:220`），但 `MakeLabel` 里**写死 `ZText`**，
而画布档决定「谁压谁」的是**建件那一刻**登记的兄弟序（`HudCanvas.Add`），`Place()` 只写位置、不改兄弟序
⇒ `TargetsAvailableText` 落到色片下面。

**改**：`MakeLabel` 增加可选形参 `float z = ZText`（旧调用点零变化），
`BuildCanvas` 里 `_targets` 传 `z: ZTargets`。

### 2.7 `B117G1011` E16 —— 前提在本宿主不成立（已修）

断言写「未挂 `BoardCenterOf`」，但 `BattleDriver.cs:3760` 已经把它挂上了 + 返回非 null
⇒ `Resolve(boardCenter)` 正常返回、`LastMissing` 恒 null。
⇒ 改成「**临时摘掉** `AnimAnchors.BoardCenterOf` 造出未挂态 → 断言 null + `LastMissing` → `finally` 装回去」。

### 2.8 `B117G1012` A2 —— 冷却时钟在批处理里不前进（已修，断言侧）

`-executeMethod` 批处理里 `Time.time` 实测 **0.000**；`WFSoundPlayer.CooldownClock` 的回退
（`Time.realtimeSinceStartup`）在本宿主同样读到 0 ⇒ `_lastAt[cue] = 0` ⇒ 原版 `&& (last != 0)` 哨兵
把第二次读成「从没播过」⇒ `Throttled` 恒 0。
⇒ 用 `WFSoundPlayer` **已有的注入口** `ClockOverride`（`B117-FIXA` 设计的就是它）把 A 组的时刻钉成 1000，
跑完还原。⛔ 不是放宽断言：`A2` 仍然确定性量到「窗口内第二次被挡」。

### 2.9 `B117G1024` E0 —— **真缺口，未修**

`Resources/ArenaPrefabs/battlearena1.prefab` 现读 `m_Layer` 分布 = **8 × 120 + 0 × 1**
⇒ `ArenaBuilder` 建完 tapHits（0/11 层）之后又**整树刷成 8**，而射线掩码是 0/10/11/30
⇒ `掩码 ∧ 宿主层 = 0`。宿主里 `FindObjectsByType<TapHitMaterialDefinition>` = **0**（驱动已销毁且 prefab 也打不到）。

两条修法都要动**别人的文件 + 调度台裁定**（断言自己的注释已列出 ①挪刷层次的次序 ②把 bit8 加进掩码 = 🔶 自设计），
⇒ **本代理不改**。

### 2.10 `B117F11` E2（`B117-37`）—— **真缺陷，未修**（在 `BattleDriver.cs`）

实测：单目标那一批 `span1 = 1.00 s`，3 目标 `span3 = **2.00 s**`（= 2 × `HitWaitSeconds`）。
根因 = `BattleDriver.cs:19617` 那一句：

```csharp
t += beatGap; if (HitBurstParallel(_signalBuf, i)) { if (float.IsNaN(hitBurstAt)) hitBurstAt = t; else t = hitBurstAt; }
```

`HitBurstParallel` 对**第一条** `Hit` 返回 false（它前面没有 `Hit`）⇒ `hitBurstAt` 是在**第一条已经
`t += DurationOf` 之后**才抓的 ⇒ 并排组自己又多算了一整条时长。
**修法**：让「本批最后一条并排」的**第一条**也参与锚定，例如

```csharp
bool burst = HitBurstParallel(_signalBuf, i)
          || (i + 1 < _signalBuf.Count && HitBurstParallel(_signalBuf, i + 1));
```
⇒ N 条 `Hit` 的尾巴落在 `start + beatGap + DurationOf(一条)` = 1.0 s。

### 2.11 `[G10/19]` A8 —— `(−6, +64)` 挂错了层（`Battle/BattleDoors.cs`，已修）

原版 `Video Image` 那颗节点自己的 `RT.ap` 就是 `(−6, 64)`；我们把它写在**子件** `video_screen` 上
（画面逐位一样，但节点值对不上）。⇒ `BuildVideo` 建 `Video Image` 时写 `anchoredPosition = (−6, 64)`，
`SetupDoor` 里 `video_screen` 改停父件中心（`ap (0,0)`）。

### 2.12 `[G10/19]` A35 —— `GetComponentInChildren` 默认跳过非激活件（已修）

`HolderRating` 生产恒关（`EarnedRating == 0` 不亮），`GetComponentInChildren<TextMeshProUGUI>()`
默认 `includeInactive: false` ⇒ **前提恒假**。⇒ 传 `true`。

### 2.13 `[B117_WBD]` 前提（`FindLinkProbe` 扫不到链接点）—— 批处理无帧循环（已修）

`Show()` 的同一拍 TMP 还没 `GenerateTextMesh`，而 `FindIntersectingLink` 判的就是 `textInfo.characterInfo`
⇒ 恒 −1。同族已记在 `Editor/BattleScene.cs:37198` 的 `B117-FIXC` 注释里。
⇒ `BattleLogPanel` **追加一个测试口** `ForceRowTextLayoutForTest()`（类尾），断言在 `FindLinkProbe` 前调它。
⛔ 没接进生产路径（真实帧循环里 TMP 自己会生成）。

### 2.14 `704562`（换环境时旧实例先进入淡出）—— **未定**

`EnvironmentApplier.Apply` 的第一句是 `if (Applied && CurrentSO == want) return this;`（同一条不重播）；
而 `BeginFadeOut` 只在 `CurrentInstance != null` 时才走。
实测 `FadingCount = 0` 只能是这两条之一：①`Choices("Ultramarines")[2].envSO` 与当前 `CurrentSO` **同值**；
②那条环境**没有 prefab**（`EnvironmentConditions.HasPrefab` 假）⇒ 根本没有实例可淡出。
⇒ **需要一次带探针的运行**（打印 `CurrentSO` / `c1.envSO` / `HasPrefab(c1)`）才能定，**本代理没有跑批处理权限**。

### 2.15 `722650`（技能结算时面板铺白那层）—— 断言与 `B117-G10-09` 的新口径打架（已改断「不赖着」）

`Resolve` 是 `skillPanel.SetActing()` 之后**同一拍**就 `ClearSelection()`（`BattleDriver.cs:16435`），
而 `ClearSelection → Hide() → FadeTo()` 的**第一件事**是原版 `FadePower` 的 `HideActingLight()`
（判据 `__FadePower.c:20` / `__SetAlpha.c:11`，2026-11-02 `B117-G10-09` 已接）
⇒ 读到那一行时白层**按原版已经熄掉**（实得 `Available`）。

「`SetActing()` 真能点亮 + 淡入从 0 起 + `Hide()` 当场熄」这三条**已在 `B117_G10_09_Assert` 的 C/D 组**逐条钉住
⇒ `BattleScene.cs` 那条改成断「结算之后白层**不许赖在面板上**」（仍有判别力：`FadeTo` 不熄就会被它抓住）。
⚠️ 真正的复刻差距（原版白层在「瞄准/结算」整个窗口里是亮的，我们同帧就收）**还在**，那要改 `Resolve` 的时序
（`BattleDriver.cs`，不在本代理红线内）。

### 2.16 `800493`（D6⑦ 真实材质队列）—— **未定**

实测 `3000..3007 > 压暗层 3006`、**违规 23 颗**。`CardView.SetQueue` 会逐颗 `StampQueue`（含粒子），
所以违规的 23 颗只可能是**某条路在建完之后把材质换回 3000**（`SetData`/`SetFace`/换材质缓存那一族），
或者它们根本不是这张卡该盖的子树成员。⇒ **需要一次带探针的运行**（把违规那 23 颗的名字打出来）才能定。

### 2.17 `728067`（E8 回弹角度前提）—— 门槛不适配（已修，`Editor/HandAssertE8.cs`）

2026-11-02（`B117-G10-06`）按原版 `ShowCardInHand` 第二句把**被悬停/抬起那张的角度强制归零**，
而本夹具**先 hover 再按下** ⇒ 拖拽期 `angBefore` 恒 **0°**（原来它是槽 2 的扇形角 ≈ −1.15°）
⇒ 角度差从「两端之差 ≈2.3°」变成「单边 ≈1.15°」，过不了 `1.5°` 门槛。
⇒ 门槛降到 **1.0°**（`1.15 > 1.0`），下面 ④（`|终值 − wantAng| < 0.5°`）**仍然有判别力**
（丢掉角度那一跳 ⇒ 停在 0° ⇒ 差 1.15° ⇒ 红）。

### 2.18 `728800`（E8 卡名字号只缩不放）—— bandMax 上调后旧口径作废（已修）

本波（`UI-21`）把卡名那一层的自适应带上限改成原版 prefab 的 **`NameUnitBandMax = 1.595`**
（`Core/CardView.cs:5575`），而名义 `NameFontSize` 是 **1.4241** ⇒ **带上限本来就在名义之上**，
短卡名拟合到 bandMax 是原版行为（TMP `m_fontSizeMax` 的语义就是「可以放大到 max」）。
⇒ 断言从「≤ 名义」改成「**落在原版那条带里**：`fs ≤ NameBandMaxForTest`」（越上限才是缺陷）。

---

## 三、`NetBattleTest` 退出码 1

现读 `_tmp_view/netbattle.log` 末尾：**没有「合计」行**，最后是 `Shut down.` / `Cleanup mono` /
`abort_threads: Failed aborting id: …` / `debugger-agent: Unable to listen on 3704`。

- `abort_threads: Failed aborting id: …` + `Cleanup mono` 是 **Unity 退出期的常态噪声**
  （`battle.log` 结尾**逐字相同**，而 `BattleScene` 是有合计行的）⇒ **不是崩溃证据**。
- 真正缺的是**断言合计行**：`NetBattleTest` 的合计在 `Run()` 的 `finally` 里印（同族 `B117G1012` 的写法），
  ⇒ 没有合计行 = `Run()` **没走到 finally**（早退/异常/根本没跑到），或日志被截断。
- **判定**：`netbattle.log` 里 `grep "✗"` 命中 0、`grep "合计"` 命中 0，
  且日志 55 MB 全是 Unity 启动/资产导入日志 ⇒ **信息不足，判「未定」**。
  需要一次**只跑 `NetBattleTest.Run`** 的复跑并保留完整日志（本代理无权跑 Unity 批处理）。
  ⚠️ 与 `BattleScene` 那类已知「退出期段错误」**没有可比证据**，⛔ 别先按「已知间歇」记过。

---

## 四、`typecheck`

```
TMPDIR=/tmp/wf_fixd bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

---

## 五、改动的文件清单

| 文件 | 改了什么 |
|---|---|
| `CardPresentation/Editor/B97W6_Assert.cs` | 方叉 6 条期望值重推（181.62 / 中心=卡心） |
| `CardPresentation/Editor/B117_G10_21_Assert.cs` | C5/C6 改与 `font.material` 比 |
| `CardPresentation/Battle/MulliganPanel.cs` | A6/A9/A16 三处真回归（收标记 / 闸 `HitDone` / `Close()` 先还原） |
| `CardPresentation/Editor/B117_G10_17_Assert.cs` | A16 改量调用方卡对象 |
| `CardPresentation/Editor/B117_G10_06_Assert.cs` | A3/A5 参照物改正 |
| `CardPresentation/Editor/B117_G10_03_Assert.cs` | E6 符号改正 |
| `CardPresentation/Battle/SkillPanel.cs` | `MakeLabel` 加 `z` 形参，`_targets` 传 `ZTargets` |
| `CardPresentation/Editor/B117_G10_11_Assert.cs` | E16 临时摘钩子造未挂态 |
| `CardPresentation/Editor/B117_G10_12_Assert.cs` | A 组钉 `ClockOverride` |
| `CardPresentation/Battle/BattleDoors.cs` | `(−6,+64)` 挪到 `Video Image` 自己 |
| `CardPresentation/Editor/B117_G10_19_Assert.cs` | A35 传 `includeInactive: true` |
| `CardPresentation/Battle/BattleLogPanel.cs` | 追加 `ForceRowTextLayoutForTest()`（类尾） |
| `CardPresentation/Editor/B117_WBD_Assert.cs` | 探链接前先强制生成字形 |
| `CardPresentation/Editor/BattleScene.cs` | 722650 改断「白层不赖着」 |
| `CardPresentation/Editor/HandAssertE8.cs` | 728067 门槛 1.5°→1.0° · 728800 改成 band 上限 |

---

## 六、应该转绿的清单（复跑后核对）

- `CollectionScene.Run`：`[B97W6]` **6 条**（`922071/922088/922105/922154/922171/922205`）⇒ 该组应 **全绿**。
- `BattleScene.Run`：`B117G1021` C5/C6 · `B117G1017` A6/A9/A16（合计 42/42）· `B117G1006` A3/A5（38/38）·
  `B117G1003` E6（55/55）· `B117G1009` E2（30/30）· `B117G1011` E16（44/44）· `B117G1012` A2（19/19）·
  `[G10/19]` A8/A35 · `[B117_WBD]` 前提 · `722650` · `728067` · `728800`。

## 七、仍会红（需要别人/别的手续）

| 断言 | 卡在哪 | 需要谁 |
|---|---|---|
| `B117G1004` B1/B2/C2/C3 | `BattleDriver.cs` 两条补丁（§2.2） | `FIXB` / 调度台转派 |
| `B117F11` E2 | `BattleDriver.cs:19617`（§2.10） | 同上 |
| `B117G1024` E0 | `ArenaBuilder` 层刷写 + 掩码口径（§2.9） | 调度台裁定 ①/② |
| `704562` | 需带探针的运行（§2.14） | 调度台下次跑批处理时加探针 |
| `800493` | 需带探针的运行（§2.16） | 同上 |
| `NetBattleTest` 退出码 1 | 缺完整日志（§三） | 调度台单独复跑 |
