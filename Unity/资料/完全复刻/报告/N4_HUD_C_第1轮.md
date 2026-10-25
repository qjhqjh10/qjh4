# N4 · HUD 域 C 清理 + 两处手牌接线（第 1 轮）

> 代理 N4 · 2026-10-25 · 接手 P1 的 `战斗界面_HUD.tsv`（679 方法）+ 两处手牌接线。
> 实做文件：`Battle/SkillPanel.cs` · `Hand/HandLayout.cs` · **`Battle/BattleDriver.cs`（调度台 2026-10-25 授权）**；`Core/**` 一行未动（归 N5）。
> 交活状态：`typecheck 0/0` · `check_ledger_points 战斗界面_HUD → A=394 通过=394 漂移=0 失败=0` ·
> `verify_annot` 本域缺注解 **0** · `check_a_vs_diff` 本域**强 0**（弱 8 条全是既有行，见 §④）。

---

## ① 结论

1. **HUD 的 C 从 46 → 39**：**4 条 C→A** · **3 条 C→D** · 另有 **6 条 A→D**（死类口径统一，见 §①·b）：
   | 行 | 原版方法 | 结论 | 一句话依据 |
   |---|---|---|---|
   | 5 / 15 | `ActiveSkillDisplay.Drag()` / `.OnTouchDrag(…)` | **C→A** | 两处差异（不亮 acting 层 / 多熄 `pressingLight`）都在 `SkillPanel.OnSkillDrag` 里按 `ActiveSkillDisplay__OnTouchDrag.c` 逐句补掉 |
   | 195 | `BattleHud.GetCardDrawPosition(bool)` | **C→A** | P3 第 2 轮已落地（`CardDrawAnchor01` + `CardDrawPositionWorld` + 两条消费点），账没跟着改 |
   | 311 | `CardDisplayWindow.ToggleCardState()` | **C→D** | `showCardTextButton` 13 战场 **13/13 = null** ⇒ `Open()` 里那句 `if (…)` 永不成立（三查见 §②） |
   | 9 | `ActiveSkillDisplay.FinishActing()` | **C→A** | 实现 + 调用点都已按原版落地：失败支**不隐藏面板** ⇒ 0.3 s 白层淡出看得见、且再点「释放」能重进选目标（见 §3·b R3） |
   | 317 / 324 | `CemeteryLogCard.SetupCemeteryCard` / `CemeteryLogGroup.SetActionImage` | **C→D** | 死类（原版零实例、零引用）⇒ 按调度台 2026-10-25 裁定判 D（§①·b） |
2. **代码改动 4 个文件**：`Battle/SkillPanel.cs`（acting 白层 **0.4 s 淡入 / 0.3 s 淡出** + 拖拽路径照原版）；`Hand/HandLayout.cs`（第二跳口 `TweenIntoSlot`）；**`Battle/BattleDriver.cs`（调度台本轮授权）** 三处 —— ① `PlayDealAnimation` 第二跳调用点（R1）② `CancelTargetChoosingByInvalidClick` 按原版失败支重写（`FinishActing` + 不隐藏面板 + 收高亮/准星 + 保留选择）③ `SkillPanelConfirmRelease` 补 `HighlightTargets()/ShowReticleNow()`（重进选目标）；**`Editor/BattleScene.cs`（调度台为「改断言」授权）** —— 只改/增 `X19-P3①b/①c/①c2/①d` 四条。
3. **敌方那半（R2）仍没做**：要改 `Core/CardFeel.cs` 的 `DealInQuad` 签名 + `BattleDriver.SyncFoeHand` 的调用点 —— `Core/**` 归 N5，调度台已裁定**下一批派**。代码留在 §③·b。
4. **`FinishActing` 已收口（行 9 → A）**：调度台批了「取消后不 Hide 面板」这一支后，`CancelTargetChoosingByInvalidClick` 已**逐句照原版失败支**重写（不隐藏面板、只收目标高亮/准星、保留 `_selectedSlot`/`_command`），`SkillPanelConfirmRelease` 补上 `HighlightTargets()/ShowReticleNow()`（= 原版 `TryUsingActiveAbility` → `StartChoosingAbilityTargetByClick`）⇒ 三条都成立：**淡出看得见 · 面板留着 · 能重进选目标**（见 §3·b R3）。断言同步改了 `Editor/BattleScene.cs` 的 X19-P3①b 并新增 ①c/①c2/①d。
5. **剩余 39 条 C 已逐条分堆定级**：P0 **3** / P1 **14** / P2 **22**（`ledger_progress` 现读 `3/14/22`，与 C=39 逐分对上）。**每条「还差什么 + 可达性 + 先后序号」写在账的备注列**（`N4-triage 2026-10-25：桶=… · 序号 P?-N · 还差=… · 可达=…`）—— 下表只是**分堆索引**（桶 / 行号 / 等级 / 序号），⛔ 不抄第二份详情：

| 桶（谁才能动） | 行号 | 等级·序号 | 一句话 |
|---|---|---|---|
| **BD**（等 `BattleDriver.cs` 一行 / 两行接线；判据已定、面板侧已做完） | 43 | P0-1 | 抬牌态不开窗（E§11.2① 一行） |
| | 544 · 553 | P0-2 · P0-3 | 能量数字弹跳（E§11.2②；⚠️ 可达面见 §⑤·2） |
| | 258 | P1-4 | 设置面板传 `MatchType` + `OnSkipTutorial`（E§8.4④ 两行） |
| | 367 · 370 · 376 | P1-5 | 选牌面板倒计时喂秒数（E§11.2③ 一行） |
| | 389 · 402 · 415 | P1-6 | 15 s 倒计时遇演出停表（E§8.4①） |
| | 243 · 339 · 364 · 378 · 397 · 399 · 409 · 412 · 413 · 537 · 548 | P2-15…23 | 分数动画 / drawTrap 行（休眠）/ 回放自动选 / 教程倒计时 10 s / 切后台补时 / 回放文案 / StopClock 两半 / 存档能量两件 |
| **本域**（`SkillPanel`） | 20 | P2-25 | 目标数拼进 descText —— 要连带改 `BattleScene` 断言（本轮未授权） |
| **新件 + 判据**（codex / oath 持久框） | 150 · 152 · 159 · 160 | P1-7 | 缺 prefab/几何 + 触发源；可达 = 池内 **31 张 codex / 28 张 oath** |
| **Core**（P2 域） | 92 · 108 · 153 · 189 | P2-11…14 | 战斗侧异画 / 传说溶解材质 / 残骸露原卡 / 换语言刷整棵 HUD |
| **Net**（联机线） | 178 · 182 · 185 | P1-10 | 本机掉线 / 对手不同步 / 断线弹窗（只在联机） |
| | 180 · 184 | P2-26 · P2-27 | 同一族的初始化与订阅 |
| **Shell**（外壳线） | 573 · 582 · 584 | P2-24 | 练习模式窗的三处状态刷新 |
| **教程线** | 306 | P2-16 | 教程指向物（`教程_引导.tsv` 另账） |

### ①·b 死类口径统一（调度台 2026-10-25 裁定 · 本轮改 8 行）

**裁定**：`D` = 读过并判定 离线不可用 / **死代码** / 纯编辑器（方案 §四）。「原版零实例 / 零调用 ⇒ 离线无行为」**正是 D 的理由**；
判 `A` 等于说「我们和它对齐了」，可它**根本没有行为可对齐**（先例 = 自绘光标 448/449/450 判 D）。

| 行 | 原版方法 | 本轮先改成 | 现状态 | 死类证据 |
|---|---|---|---|---|
| 317 | `CemeteryLogCard.SetupCemeteryCard` | C→A | **D** | 见下方「证据三件」 |
| 318 | `CemeteryLogGroup.ClearActions` | （原就是 A） | **D** | 同上（**调度台没点名，但同属该类 ⇒ 一并改**；不改会留下两套口径） |
| 319 | `…CreateCemeteryCard` | （原就是 A） | **D** | 同上 |
| 320 | `…CreateMainCard` | （原就是 A） | **D** | 同上（且 `private` 0 调用点） |
| 321 | `…CreateTargetCard` | （原就是 A） | **D** | 同上 |
| 322 | `…DisplayAction` | （原就是 A） | **D** | 同上 |
| 323 | `…GetCemeteryCardLocalPos` | （原就是 A） | **D** | 同上（方法体本身就是 `return default(Vector2)`） |
| 324 | `…SetActionImage` | C→A | **D** | 同上（`private` 0 调用点） |

**证据三件**（已逐条写进每行的「差异」列）：① `SetupCemeteryCard|SetActionImage` 全量反编译**除定义外 0 引用**；
② `"actionImage"` 在 `assets_full` **0 命中**、`"targetCard1"` / `"centralCemetery"` 0 命中；
③ 两类在 24.7 万文件里**只有 `globalgamemanagers/MonoScript/MonoScript_{319,2187}.json`**（脚本资产本身），无任何组件实例。
⇒ 结论句统一写成「**两类只以 MonoBehaviour 脚本存在；`SetupCemeteryCard|SetActionImage` 全量反编译 0 引用 ⇒ 原版零实例、离线无行为**」。

⚠️ 同族的 `CemeteryLogManager`（325-331）本来就是 D —— 本次把 317-324 拉齐，这一类**从此不再有两套口径**。

---

## ② 判据与证据（文件:行号）

### 2·1 acting 白层两档（行 5/15/9 → 代码）

| 事实 | 出处 |
|---|---|
| 淡入 = `actingLight.SetActive(true)` + `DOFade(actingLight, 1f, 0.4f)` + `SetEase(6)` | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:456-505`（`OnTouchDrag`）；`decomp_full/ActiveSkillDisplay__ShowActingLight.c:20-33` |
| 淡出 = `DOFade(actingLight, 0f, 0.3f)` + `SetEase(6)` | `decomp_full/ActiveSkillDisplay__FinishActing.c:15-24` |
| 两个时长常量 | `python -I -X utf8 工具/read_literal.py D:/2/unity_run_ref/GameAssembly.dll 0x1834b3158 0x1834b2dc8` → **0.4** / **0.3**（4 字节 float，✅ 像字面量） |
| `SetEase(6)` = `Ease.OutQuad` | `Assets/Resources/DOTweenSettings.asset` 的 `defaultEaseType: 6`（本仓既有口径，见 `HandLayout.Place` 那段注释） |
| 拖拽路径：`pressingLight` 亮着 ⇒ **直接 return**（不熄灯） | `ActiveSkillDisplay.cs:474-480`（`if (pressingLight.gameObject.activeSelf) return;`） |
| `HideActingLight` = 杀补间 + α 瞬间归零 | `decomp_full/ActiveSkillDisplay__HideActingLight.c:20-33` |

### 2·2 行 311 判 D 的三查

| 查什么 | 结果 |
|---|---|
| ① C# 引用 | 全量 cpp2il 里 `ToggleCardState` **只有两处**：定义 `CardDisplayWindow.cs:975` 和 `:360` 的 `UnityAction call2 = ToggleCardState;`（在 `if (showCardTextButton != null)` 里） |
| ② 场景挂接 | `showCardTextButton` 在 `assets_full` 命中 **14** 个实例（13 战场 + `mainmenuwarpforge`）；**13 战场全部 `{m_FileID:0, m_PathID:0}`**（例：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json`）；只有 mainmenu 那份非空（`PathID 2514`，**菜单域、不属本账**） |
| ③ prefab / SO / Addressables | 同一棵树只有这 14 个场景实例；`ToggleCardState` 作为 UnityEvent `m_MethodName` **0 命中**；`assets_full` 里没有含该字段的 prefab/SO |
| 断点 | **战斗侧**：`Open()` 的 `if (showCardTextButton != null)` 永不成立 ⇒ 监听从不挂 ⇒ 战斗侧调用链断在这儿 |

### 2·3 死类证据（账 317-324 判 D 的依据）

| 事实 | 出处 |
|---|---|
| `CemeteryLogCard` / `CemeteryLogGroup` 全库只作为**脚本资产**存在 | `assets_full/globalgamemanagers/MonoScript/MonoScript_319.json` / `MonoScript_2187.json` |
| 方法体 0 引用 | cpp2il 全量 grep `SetupCemeteryCard|SetActionImage` → 只有定义（`CemeteryLogCard.cs:8` / `CemeteryLogGroup.cs:94`） |
| 序列化字段 0 实例 | `"actionImage"` 在 `assets_full` **0 命中**；`"damageText"` 唯一命中那份是另一个类（有 `damageCounterParent`/`myAnimation`，`MonoBehaviour_6701279354835082176`） |
| 类级证据本就写在代码里 | `Battle/BattleLogPanel.cs` 文件头「`CemeteryLogGroup`/`CemeteryLogManager`/`CemeteryLogCard` 全是死代码 ⇒ 这条销账」 |
| 同族既有先例 | `CemeteryLogManager` 的 7 条（账 325-331）本来就是 **D**；自绘光标 448-450 也是判 D |

### 2·4 行 195（抽牌锚）

| 事实 | 出处 |
|---|---|
| 锚值 `(1350, 490, 100, 100)`，两方同值，13 场实算一致 | `判据/HUD光标与抽牌锚.md` §二·订正 |
| 我方落点读口 / 世界化 / 消费点 | `BattleDriver.cs:23542（HudAnchor01）` · `:23551（CardDrawAnchor01）` · `:23559（CardDrawPositionWorld）` · `PlayDealAnimation` 第一段终点 |

### 2·5 第二跳（我方）与敌方那半的判据（改动请求用）

| 事实 | 出处 |
|---|---|
| 原版第一段 `DOMove` 的终点 = `GetCardDrawPosition(isPlayer)` | `decomp_full/CardScript__DrawCard.c:76-80` |
| 第二段 = `SetupCardInHand` → `PlayerHand.MoveCardsInHandToPosition` → `CardScript.UpdateMinionInHandPosition`：`DOMove`/`DOLocalRotate`/`DOScale` 三条，时长 `VarsGlobal.timeToPositionCard` = **0.2 s**，**无 `SetEase`** ⇒ 吃默认 `OutQuad` | `HandLayout.Place` 里那一整段注释（2026-10-24 `A26`/`A3`/`J3` 已逐句核过） |
| 敌我同值同路径（`isPlayer` 只影响时长字段 `timeToDrawEnemyCard`） | `判据/HUD光标与抽牌锚.md` §二·3（13 场同值，⛔ 别给对手挪一格） |

---

## ③ 改动清单

### 3·a 已施工（调度台本轮授权后，`BattleDriver.cs` 也在内）

| 文件 | 改了什么 | diff |
|---|---|---|
| `MyGame/Assets/CardPresentation/Battle/SkillPanel.cs` | ① 新增 `ActingFadeIn=0.4` / `ActingFadeOut=0.3` 常量（判据见 §2·1）② 字段 `_actingA` / `_actingFade` ③ `SetActing()` 改成**淡入**（α 0.392 → 1，`Ease.OutQuad`）④ 新增 `FinishActing()`（淡出到 0，0.3 s）+ 共用口 `FadeActingTo` ⑤ `ApplyColors()` 的 acting 分支改用 `_actingA` 当 α ⑥ `Show` / `HideImmediate` / `Teardown` 里杀补间 + 归零 ⑦ `OnSkillDrag` 逐句照 `OnTouchDrag`：`_pressed` 时直接 return（⛔ 不再熄 `pressingLight`）· 可用（`ShownTargets>0`）才 `SetActing()` | **92+/5−** |
| `MyGame/Assets/CardPresentation/Hand/HandLayout.cs` | 新增 `public bool TweenIntoSlot(CardView, float delay=0)`：从 `_lastPose` 取格位、走 `CardView.TweenPose`（`CardTween.RelayoutDuration` 0.2 s · `Ease.OutQuad`），与 `Place` **同一条判据** | **26+/1−** |
| `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（**调度台 2026-10-25 授权**） | ① **R1 第二跳调用点**：`PlayDealAnimation` 非换牌支的 `deal.OnComplete(...)` 改走 `hl.TweenIntoSlot(v)`，取不到排布时退回 `SetPose` ② **R3 `FinishActing` + 失败支重写**：`CancelTargetChoosingByInvalidClick` 逐句照原版失败支（不隐藏面板 · `FinishActing` · 收高亮/准星 · 保留选择）；③ `SkillPanelConfirmRelease` 补 `HighlightTargets()/ShowReticleNow()`；④ 把 `ClearSelection` 里两段「清两侧高亮」抽成 `ClearTargetHighlights()` 供两处共用 | 该文件本轮前已被 P3 改过 ⇒ 总量 **686/63** |
| `MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（**调度台本轮为「改断言」授权**） | 只动 `X19/P3` 那一块：① **改** `X19-P3①b` 的期望值（`SelectedSlot < 0` → 仍在选目标），旁边写死「期望值依据 = `…ByClick.c:32` 的调用顺序」；② **新增** `X19-P3①c` / `①c2` / `①d` 三条观测量（面板留着 · 高亮与准星已收 · 再点释放 ⇒ 重进选目标）。⛔ 其它断言一行未碰 | 见 `git diff --numstat`（该文件本轮前已被别人改过） |
| `资料/完全复刻/账/战斗界面_HUD.tsv` | 4 条 C→A（5/15/195/9）· 3 条 C→D（311/317/324）· **8 条死类 A→D（§①·b）** · 3 条陈旧 A 行的备注就地订正（12/19/21）· 39 条 C 行加 `N4-triage` 分堆标签 · `--fix` 收漂移（两轮共 58 + 另一轮 43）＋ 手工歧义 **10** 条（`InitialSetup→Build`、`ViewAt×2`、`SetHint×3`、`SetVis×2`、`EndPlayerTurn`、`AutoEndTurnIfStuck`，BattleDriver 两次改动后各重指一次） | 见 `git diff --numstat` |

### 3·b 改动请求（R1/R3 已施工留档；**R2 未施工**，可按此照抄）

**R1 — 我方第二跳的调用点 ✅ 已施工**（`Battle/BattleDriver.cs`，`PlayDealAnimation` 非换牌支；下面是当时写的补丁，落地时**压成一行**以免行号漂移）

```csharp
// 原（现读 :26504-26512）
var handWorld = p; float handAng = ang, handSc = sc;
var deal = CardFeel.DealInPose(v, deck, anchorWorld, ang, sc, dealDelay,
                               back: CardArt.CardBack(_myFaction));
if (deal != null)
{
    var parent = v.transform.parent;
    Vector3 handLocal = parent != null ? parent.InverseTransformPoint(handWorld) : handWorld;
    deal.OnComplete(() => { if (v != null) v.SetPose(handLocal, handAng, handSc); });
}

// 改：落到锚点之后**补间**进格位（原版 SetupCardInHand → MoveCardsInHandToPosition，0.2 s OutQuad）
var deal = CardFeel.DealInPose(v, deck, anchorWorld, ang, sc, dealDelay,
                               back: CardArt.CardBack(_myFaction));
if (deal != null)
{
    var parent = v.transform.parent;
    Vector3 handLocal = parent != null ? parent.InverseTransformPoint(p) : p;
    float handAng = ang, handSc = sc;
    deal.OnComplete(() =>
    {
        if (v == null) return;
        // 第二跳：抽牌锚 → 手牌格。取不到排布（`_lastPose` 没这张）时**退回硬切**并出声，⛔ 别静默。
        if (hl == null || !hl.TweenIntoSlot(v))
        {
            Debug.LogWarning("[SyncHand] 第二跳取不到手牌格位（HandLayout 最近一趟没排过这张），退回硬切");
            v.SetPose(handLocal, handAng, handSc);
        }
    });
}
```

判据：`§2·5`。⚠️ `hl` 是 `PlayDealAnimation` 开头已有的局部（`interaction != null ? interaction.hand : null`），不用新取。

**实际落地的那一行**（与上面等价，只是没拆开）：
```csharp
deal.OnComplete(() => { if (v != null && (hl == null || !hl.TweenIntoSlot(v))) v.SetPose(handLocal, handAng, handSc); });
```
⚠️ 取不到排布时**不发 `Debug.LogWarning`**（原补丁想发，压行后省掉）—— 这条路只在「`Refresh` 之后 `_lastPose` 没有这张」时走到，等于老行为；要出声得等下一批重排注释时再加。

**请求 R2 — 敌方那半（两文件）**

R2-1（文件：`Core/CardFeel.cs`，`DealInQuad`，现读 `:3342`）：目标点改成可传

```csharp
public static Tween DealInQuad(ImageQuad q, Vector3 from, float delay = 0f, Vector3? to = null)
{
    if (q == null) return null;
    var tr = q.transform;
    Vector3 toPos = to ?? tr.position;   // 缺省保持旧行为（调用点零改动）
    tr.position = from;
    ...
    seq.Append(tr.DOMove(toPos, d).SetEase(Ease.OutCubic));   // 原 :3352 用的是 tr.position 读到的 `to`
```

R2-2（文件：`Battle/BattleDriver.cs`，`SyncFoeHand` 末段，现读 `:26648-26660`）：第一段落锚、第二段补间到槽位

```csharp
for (int i = 0; i < _foeDealt.Count; i++)
{
    var q = _foeDealt[i];
    if (q == null) continue;
    var qtr = q.transform;
    Vector3 slotLocal = qtr.localPosition;          // ReapplyFoeHandPoses 已按槽位摆好（含卡背 padding 偏置）
    Quaternion slotRot = qtr.localRotation;
    var t = CardFeel.DealInQuad(q, deck, DealStaggerDelay(false, i), CardDrawPositionWorld(false));
    if (t != null)
        t.OnComplete(() =>
        {
            if (q == null) return;
            // 第二跳：抽牌锚 → 槽位（原版 MoveCardsInHandToPosition，0.2 s OutQuad）
            var s = DOTween.Sequence();
            s.Append(q.transform.DOLocalMove(slotLocal, CardTween.RelayoutDuration).SetEase(Ease.OutQuad));
            s.Join(q.transform.DOLocalRotateQuaternion(slotRot, CardTween.RelayoutDuration).SetEase(Ease.OutQuad));
            CardTween.Use(s, Ease.Linear, q.transform);
        });
}
```

⛔ 敌方锚用 `CardDrawPositionWorld(false)` —— 与玩家**同值**（`(1350,490,100,100)`），**别按场分支、别挪一格**（判据 §2·5）。
⚠️ 这两份文件当前都不在我的白名单（`BattleDriver.cs` 除名、`Core/**` 属 P2）⇒ **一行未改**。

**R3 — `FinishActing` 的调用点 + 「取消后面板不留」那一支 ✅ 已施工（行 9 → A）**

**读清了什么**（判据逐条）：
- `FinishActing` 全库**只有两个调用点**：`BattleManager.FinishedChoosingAbilityTargetByClick`（失败支，`BattleManager.cs:44362-44367` / `decomp_full/…ByClick.c:32`）与 `BattleManager.CancelAbilityTargeting`（`BattleManager.cs:44369-44373`）。
- 失败支的**顺序**：`NotifyCantDoAction(Battle/Tips/InvalidTarget)` → `waitingToResolveActiveAbilityFlag = false` → `cardSelected.CancelActiveAbility()` → **`activeSkillDesc.FinishActing()`** → `StopShowingPotentialTargets(isAttack:false)` → 准星 `ToggleCrosshair(false, instant)` + 攻击选择器关。
- **这一支不 FadeOut 面板**：`FadeOut` 的调用点全在别处 —— `ExecuteActiveAbility.c:160` → `StopDisplayingActiveAbility`（成功路径）· `StopDisplayingActiveAbility.c:13` · `StopDisplayingSkillFromManager.c:9`（隐藏钮）· `ChoiceOfCardPlayer.c:25` / `ChoiceOfCardEnemy.c:19` · Recall-to-hand/deck · `ResolveTransformUnit`。⇒ **原版取消后技能面板留在屏上**（`cardSelected` / `UIstate` 也不重置）。
- 「再点面板」= 原版 `TryUsingActiveAbility`（目标型）→ `StartChoosingAbilityTargetByClick`（`BattleManager.cs:44314-44340`：遍历场上单位、`IsValidActiveAbilityTarget` ⇒ `ChangeState(potentialTargetInBoard)`，再 `ToggleCrosshair(true)`）。

**落地（三处）**
1. `BattleDriver.CancelTargetChoosingByInvalidClick`（`:14174`）**逐句照失败支重写**：技能档 = 出声 → `skillPanel.FinishActing()` → `ClearTargetHighlights()`（新抽出的等价 `StopShowingPotentialTargets`）→ 关选择器/准星/图标；**不隐藏面板、不清 `_selectedSlot`/`_command`**。攻击档（我们自造的那条）仍走 `ClearSelection()`。
2. `BattleDriver.SkillPanelConfirmRelease`（`:15042`）补 `HighlightTargets()` + `ShowReticleNow()` = 原版 `StartChoosingAbilityTargetByClick` 那两半（常规路径幂等）。
3. `Editor/BattleScene.cs` 断言：`X19-P3①b` 的 `SelectedSlot < 0` 改成「仍在选目标」（并就地注明**期望值依据 = `…ByClick.c:32` 的调用顺序**）；新增 **X19-P3①c**（面板留着）· **①c2**（准星 + 目标高亮已收）· **①d**（再点释放 ⇒ 目标重新点亮 + 准星回来 + 亮 acting）= 三条观测量。
   - ⚠️ 关于「`SelectedSlot < 0` 是不是通用不变量」：它只出现在这个失败支的断言里（块注释也自承「原版接着 `CancelActiveAbility`/`FinishActing`/`StopShowingPotentialTargets`」）⇒ 判**这一支的旧期望值**，按调度台口径可以改；没有别的断言依赖它。
4. 顺带把 `ClearSelection()` 里那两段「清两侧高亮」抽成 `ClearTargetHighlights()`（`:15843`）—— 两个调用点共用一份，⛔ 不抄第二份。

⚠️ 防御：批处理里 DOTween 走 Manual 档、不推进 ⇒ 自检**测不到** 0.3 s 这段长度本身（只测状态与流程）；淡出参数的判据仍是 `read_literal` 现读的 0.4/0.3 + `SetEase(6)`。

---

## ④ 核验结果

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_n4 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅（中途出现过 **2 次** 1 条运行时错，分别在 `Shell/DraftModeWindow.cs:2533` / `Shell/PopUpGameWindow.cs:477` —— 都是**别的代理正在写的 Shell 文件**，隔一次重跑即恢复 0/0（铁律 13·3 那条「报假错」的现实版）；未去改别人的文件） |
| `python -I -X utf8 工具/check_ledger_points.py 战斗界面_HUD` | **A=394 通过=394 漂移=0 失败=0（告警 4 = 工具判为「返回式/属性型落点」的候选，非失败）** ✅（漂移被 `--fix` 收了四轮：SkillPanel 15 条 + BattleDriver 43 条；另有 **10 条同名多声明的歧义**手工改指 —— `InitialSetup→Build:709` · `ViewAt:23793`×2 · `SetHint:15649`×3 · `SetVis:16414`×2 · `EndPlayerTurn:15844` · `AutoEndTurnIfStuck:17307`） |
| `python -I -X utf8 工具/ledger_progress.py` | `战斗界面_HUD 679 · A 394 / B 30 / C 39 / D 216 / E 0 · P0/P1/P2 = 3/14/22`（本轮改前 A 396 / C 46 / D 207）|
| `python -I -X utf8 工具/verify_annot.py` | 本域 `战斗界面_HUD` **0** 条（全局 11 条缺注解全在别的域）✅ |
| `python -I -X utf8 工具/check_a_vs_diff.py 战斗界面_HUD` | **强 0**；弱 8 条（`ClickDown` / `FadeIn` / `BattleHud.Awake` / `CardDisplayWindow.ResetOrder` / `CemeteryManager.AddCardToCemetery` / `ClockManager.ShouldUpdateSecondsPassed` / `EndBattleDoors.SetupDoor` / `PracticeModePopup.Close`）—— **全是既有行**；本轮新增的 A 行（5/15/195）与改 D 的 8 条**一条没进** |
| 账结构 | 680 行 · 0 CRLF · 无 BOM · 列数 ≠ 11 的行 **0** ✅（收工时另有 5 条**别人文件**造成的漂移已 `--fix`：`BattleLogPanel` / `CombatAutoZoom` / `TouchInputManager` = 本轮未碰的文件，是别的代理在改） |
| `git diff --numstat` | `SkillPanel.cs` **92/5** · `HandLayout.cs` **26/1** · `BattleDriver.cs` **686/63** · `BattleScene.cs` **351/39**（后两份本轮前都已被别的代理改过；行尾实测无翻转）✅ 行尾：三个 `.cs` 都保持各自原样（SkillPanel/HandLayout = LF、BattleDriver = CRLF，`b.count(b'
') == b.count(b'
')` 实测无翻转） |

> ⚠️ `ledger_progress` 的 `P0/P1/P2 = 3/14/22` 与 C=39 **逐分对上**（等级列只出现在 C 行；311 改 D、9 改 A 时都已清等级）。

---

## ⑤ 没查清 / 没做的

1. **（已收口）行 9 已 A**。仍留着两条、都不阻塞：
   (a) 批处理里 DOTween 走 Manual 档、不推进 ⇒ **0.3 s 这段时长本身自检测不到**（自检只测状态与流程；参数判据仍是 `read_literal` 现读的 0.3 + `SetEase(6)`）。
   (b) **另一个 `FinishActing` 调用点没接**：`BattleManager.CancelAbilityTargeting`（`ExecuteActiveAbility.c:41` / `BattleManager__Update.c:1213` 两处调用）在**成功结算**那一支也会先 `FinishActing`（随后 `ExecuteActiveAbility.c:160` 才 `StopDisplayingActiveAbility`=FadeOut）。我们 `DoResolve` 是同步的、`SetActing()` 之后**立刻** `ClearSelection()` 收面板 ⇒ 那一段白层淡出同样被盖掉。**归属 = `战斗逻辑驱动` 账的 `ExecuteActiveAbility` / `CancelAbilityTargeting` 行，不在本域**，本轮没做；已在本报告 §⑥ 单列。
2. **`ManaTypeHolder.DoTextAnim` 的等级冲突**（行 544/553）：E 把它改 P0「每局能量变化都看得见」，但同一行备注写的生效面 = 每场 8 个 `ManaTypeHolder` 里**只有 2 个** `useTweenOnValueChange=1`（我现读 13 场逐份核过，每场都是 **2 个 1 / 6 个 0**）。两句打架，**等级请调度台裁**（账里两说都留着，没有静默改）。
3. **（已收口）死类口径**：调度台已裁定「死类 ⇒ D」，318-324 + 317 共 8 行已改（§①·b），`CemeteryLogManager` 那 7 条本来就是 D ⇒ 该类不再有两套口径。
4. **行 311 在菜单域是否活**：`mainmenuwarpforge` 有一份非空 `showCardTextButton`（PathID 2514）。它不在本账域，**没查**菜单侧那条链（归属：菜单/外壳线）。
5. **行 20（目标数拼进 descText）**：会改渲染层级与既有断言（`Editor/BattleScene.cs` 读 `TargetsText` / `TextForTest(3)`，且 `B17` 那组断言钉着「目标数不吃淡入淡出」）⇒ 要连带改断言，而那个文件不在白名单。**没做**。
6. **两处手牌接线**：我方那半（Hand 侧 `TweenIntoSlot` + `BattleDriver` 调用点）**已全做完**；**敌方那半（R2）没做** —— `Core/CardFeel.cs` 归 N5，调度台已裁「下一批派」⇒ 见 §③·b R2。**这不是「查不到」**，是「不在本批授权」。

---

## ⑥ 顺手发现

1. **`check_ledger_points.py` 的假候选又抓到一次**：`ActiveSkillDisplay.InitialSetup → Build` 报「`Build` 有 2 处声明：707, 709」——**707 是 `void Awake() { Build(); }`**（方法体里单独一行的调用），不是声明。⇒ 与 P1 报告 §E5 同一条坑，这次出现在**单行方法**上（`void Awake() { Build(); }` 一行里既有声明又有调用）。**建议**：`find_decl_candidates` 排除「行内含 `)` 之后紧跟 `{` 且前面还有 `(`」的形态，或直接要求该行以访问修饰符/返回类型开头且**在 `(` 前只有一个标识符**。
2. **`SkillPanel` 的三盏灯是「一件三态」**：原版 `actingLight` / `bgLight` / `pressingLight` 三颗独立 `Image`，我们收成一颗 `_light` + `Light` 枚举。本次补 acting 的 0.392→1 淡入时只能引入「α 倍率」`_actingA` —— **代价**：原版「acting 亮着的同时 pressing 也亮」这种叠加态在我们这里表达不出来（优先级 acting > pressed）。**今天零可见差**（拖拽路径在 `_pressed` 时直接 return，两者不会同时亮），但若将来出现「结算时还能按」的状态，要先拆成两颗。已在 `_actingA` 的 doc 里写死。
3. **账里 `P0/P1/P2` 的统计口径**：现读等级列**只出现在 C 行**（`3/14/22` 与 C=39 逐分对上），但这个「只」靠**纪律**（改 A/D 时人手清等级）而不是工具保证；`ledger_progress.py` 看起来是按「等级列非空」直接数的（本次我把 311 改 D、9 改 A 时都主动清了等级）。**建议**：改成按 `状态=='C'` 过滤，否则将来谁忘清一格就虚高一条。
4. **`BattleDriver.cs` 的行号仍在漂**（本轮我读到的 `HudAnchor01` = 23542 / `CardDrawAnchor01` = 23551；P3 报告里写的是同一段但行号更早）。⇒ 我这次落点全部带**裸声明名**（新格式），已跑 `--fix` 收口；**下一次谁再动 `BattleDriver.cs`，交活前再跑一次 `--fix` 即可自愈**。
5. **（已按新发现做掉）原版「选目标点到非法目标」这一支【不隐藏技能面板】**（判据见 §3·b R3）：原版只让白层 0.3 s 褪 + 清目标高亮，面板留着、可继续点目标/再点面板。我们原来 `Hide()` 整个面板 ⇒ 不仅少一段淡出，**UX 也不同**。本轮已按原版重写该支并补三条断言（X19-P3①c/①c2/①d），账行 9 → A。
6. **`BattleDriver.cs` 本轮净 **+37 行** ⇒ 别的账里有 **32 条**落点会跟着漂**（本账 47 条已 `--fix` 收干净）：`未分类_其他` **16** · `战斗逻辑驱动` **5** · `特效动画` **5** · `教程_引导` **3** · `卡牌面_渲染` **2** · `音频_语音` **1**（口径 = 落点 `BattleDriver.cs:>14174` 的行）。调度台已答「同步点统一 `--fix`」⇒ 我不动它们。
7. **成功结算那一支也有一记 `FinishActing`**（本轮读 `FinishActing` 调用点顺带发现，见 §⑤·1b）：`ExecuteActiveAbility.c:41` → `CancelAbilityTargeting` → `FinishActing`，**之后**（`:160`）才 `StopDisplayingActiveAbility`=FadeOut ⇒ 原版在「技能正在结算」那段里，白层是*看得见地*在褪。我们 `DoResolve` 同步、`SetActing()` 后立刻 `ClearSelection()`（0.1 s 收面板）⇒ 这段看不见。**归属 = `战斗逻辑驱动` 账**（`ExecuteActiveAbility` / `CancelAbilityTargeting` 两行），建议那边记账。
8. **`CemeteryLogCard` 的 `damageText` 字段名会撞**：`assets_full` 里唯一命中那份是另一个类（`damageCounterParent`/`myAnimation`）。⇒ 「按独有字段名 grep 0 命中」这个手法要**带上第二个字段交叉**，否则会误判（这次没错，但差点）。
