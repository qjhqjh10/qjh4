# A26 · `Hand/**` 两处 Sequence 缓动收尾 + DOTween 两套默认值落账

> 执行代理 **A26** · 2026-10-24
> 白名单：`CardPresentation/Hand/**` · `资料/已知的坑.md` · 本目录。
> ⛔ 未跑 Unity。改完 `.cs` 跑过 **`TMPDIR=/tmp/wf_a26 bash d:/4/Unity/工具/typecheck.sh`** ⇒ **运行时 0 / 编辑器 0**（见 §五）。
> ⛔ 没动 git。`git diff --numstat`：`Hand/HandLayout.cs +15/−1` · `Hand/CardInteraction.cs +14/−1` · `资料/已知的坑.md +4/−0`。
> 判据口径沿用 **`A22_Sequence缓动审计.md` §一**（已读，未重复查 DOTween 语义）。

---

## 〇、结论速览

| # | 位置 | 原版对应方法 | 原版形态 | 处置 |
| --- | --- | --- | --- | --- |
| 1 | `Hand/HandLayout.cs:844`（手牌重排） | `PlayerHand.PositionCardInHand` → `CardScript.UpdateMinionInHandPosition` | **3 条独立单补间**，**无 `SetEase`** ⇒ 各自默认 `OutQuad` | ✅ **改 `OutQuad` → `Linear`** |
| 2 | `Hand/CardInteraction.cs:493`（拿起手牌） | `CardScript.StartMovingFromHand` | **1 条单补间**（只有 `DOScale`），**无 `SetEase`** ⇒ 默认 `OutQuad` | ✅ **改 `OutQuad` → `Linear`** |
| 3 | `Hand/CardInteraction.cs:880`（松手回弹，**A22 普查范围外、本轮新发现**） | **查不到** | —— | ⏸ **不改**（判据缺，见 §四·1） |
| 4 | `资料/已知的坑.md` | —— | —— | ✅ **追加第 63 条**（两套默认值 + IL 判据 + 原版实测） |

---

## 一、判 1：手牌重排（`HandLayout.Place`）

### 1.1 结论

**我们的 `OutQuad` 是错的，改成 `Ease.Linear`。** 理由不是「线性手感」，而是：
`CardTween.ToPose` 是**一个 Sequence + 3 条 Join**、子补间**都没写 `SetEase`**（⇒ 各自吃默认 `OutQuad`）；
在序列层再传 `OutQuad` = **二次弯曲**（半程 `0.9375`，原版是 `0.75`）。传 `Linear` 才等于原版那三条独立单补间。

### 1.2 证据（原版方法体，逐层）

1. `d:/2/tools/decomp_full/PlayerHand__PositionCardInHand.c`
   - `:14` 卡状态 `+0x228 == 8` 时先 `CardScript__StopShowingCardInHand`（先退出「展示」态）；
   - `:29-33` 取 `CardsHorizontalLayout__GetPositionAndRotation(...)`（位置+旋转）；
   - `:41` 交给 **`CardScript__UpdateMinionInHandPosition`**，第 5 个实参是 `param_5`（重排时长）。
2. `d:/2/tools/decomp_full/PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c`
   - `:76-80` 取 `BattleManager.get_globalVars()` 再读 `*(float*)(globalVars + 0xA4)` 作为**第 4 实参**喂给
     `PositionCardInHand` ⇒ 这个时长 = **`timeToPositionCard` = 0.2s**
     （`资料/VarsGlobal_原版数值.md:53`；= 我们的 `CardTween.RelayoutDuration`）。
3. `d:/2/tools/decomp_full/CardScript__UpdateMinionInHandPosition.c`（**关键**）
   - 只在该卡 `+0x228 == 1`（在手牌常态）时执行；
   - 先 `TweenExtensions.Kill` 掉**三条各自记住的旧补间**（`+0x340` / `+0x348` / `+0x2d0`）；
   - 然后开**三条独立单补间**，**同一个时长 `param_4`**：
     - `DOLocalRotate(transform, euler, param_4, 0, 0)`
     - `DOScale(transform, GetCardInHandScale(), param_4, 0)`
     - `DOMove(transform, pos, param_4, 0, 0)`
   - **全文一个 `SetEase` 都没有** ⇒ 三条各自吃 `DOTween.defaultEaseType`。
4. 原版 `defaultEaseType` 已从**原版资产**现读 = **`6`（OutQuad）**（见 §三，`resources.assets` pathID 1430）。

### 1.3 为什么我们这边必须传 `Linear`（而不是「照着 OutQuad 抄」）

`Core/CardTween.cs:86-94` 的 `ToPose`：

```csharp
var seq = DOTween.Sequence();
seq.Join(tr.DOMove(pos, duration));      // 子补间无 SetEase ⇒ defaultEaseType
seq.Join(tr.DORotate(..., duration));
seq.Join(tr.DOScale(..., duration));
return Use(seq, ease, tr);               // 你传什么就挂到【序列】上
```

- 原版：`OutQuad(t)`（三条各自一次）。
- 我们传 `OutQuad`：`OutQuad(OutQuad(t))` ⇒ 半程 `OutQuad(0.75) = 0.9375`（改了之后是 `0.75`）。
- 我们传 `Linear`：序列层不弯（`Sequence::DoApplyTween` 在 `easeType == Linear` 时整段跳过），
  三条子补间各自 `OutQuad` ⇒ **与原版同形**。

### 1.4 改动

`Hand/HandLayout.cs:841-858`：`Ease.OutQuad` → `Ease.Linear`，并在上一行加 14 行判据注释
（原版方法体路径、三条单补间、`defaultEaseType = 6` 的原版实测、二次弯曲的半程数值、⛔ 别改回去）。

---

## 二、判 2：拿起手牌（`CardInteraction.PickUp`）

### 2.1 结论

**同上：`OutQuad` → `Ease.Linear`**，理由同一套（我们是 Sequence，原版是单补间）。

### 2.2 证据

`d:/2/tools/decomp_full/CardScript__StartMovingFromHand.c`（亲读）：
- `:28` 先置卡状态 `+0x228 = 9`；
- `:40` 播 `SoundAssetCollection.MoveCardIntoBattle` 的 cue；
- `:42` 调 `BattleManager.StartMovingCardInHand(...)` —— 读 `BattleManager__StartMovingCardInHand.c`：**只记账**
  （`SetCardTracked` / 记录 card、把 `+0x328 = 0`、`+0x290 = 2`），**没有任何补间**；
- `:52-57` **唯一一条补间**：
  `DOScale(transform, GetCardInHandScale() * globalVars+0x44, globalVars+0x78, 0)`
  - `+0x44` = `cardInHandMovingScale` = **1.1**（`VarsGlobal_原版数值.md:34`；= 我们的 `dragScale`）
  - `+0x78` = `timeToScaleIntoMoving` = **0.1**（`VarsGlobal_原版数值.md:43`；= 我们的 `PickUpDuration`）
  - **无 `SetEase`** ⇒ 默认 `OutQuad`（原版 `DOTweenSettings` = 6，见 §三）
- ⚠️ 原版**拿起时没有位置补间**：位置由拖拽每帧直接跟手（我们的 `UpdateDrag` 也是直接
  `Vector3.Lerp`，同一形态）；我们这边额外多一条「抬起 `-dragLiftZ`」的 `DOMove`，
  那是我们自己的 `dragLiftZ`（`CardInteraction.cs:40`），**不在本件范围**。

### 2.3 改动

`Hand/CardInteraction.cs:492-510`：`Ease.OutQuad` → `Ease.Linear` + 13 行判据注释（同上口径）。

---

## 三、`资料/已知的坑.md` 第 63 条（新增）

写入内容（4 行，LF，未翻行尾）：**`Sequence` 默认 ease = `Linear`；单 tween 默认 = `DOTween.defaultEaseType`
（本工程 `Assets/Resources/DOTweenSettings.asset:31` = `6` = `OutQuad`）**，并给：
- IL 判据三条（`Sequence::Setup` `IL_003a: ldc.i4.1 → stfld easeType`；`Tweener::Setup` `IL_006a: ldsfld defaultEaseType`；
  `Sequence::DoApplyTween` `IL_0018-1f`：`Linear` ⇒ `beq.s` 跳过）；
- 用法（照原版复刻的 Sequence 一律 `Linear`；`CardTween.ToPose` 同理）；
- 出处 → `A22_Sequence缓动审计.md §一·1.1`。

### 3.1 🆕 顺手坐实：**原版自己的 `defaultEaseType` = 6**

`W58_界面线_Toast黑场次序判据.md:255-256` 把「原版有没有覆盖默认 ease」列为**待坐实**
（当时只能证明游戏代码里 `grep defaultEaseType` 0 命中）。本轮现读坐实了：

- 文件：`d:/2/unity_run_ref/Warpforge_Data/resources.assets`
- 对象：`MonoBehaviour` **pathID 1430**、`m_Name = "DOTweenSettings"`（`m_Script.pathID = 0x11cb`）
- 原始字节（`len = 200`）尾部：`… 01000000 00000000 03000000 00000000 00000000 06000000 60cdd93f 00000000`
- 按字段序（与我们的 `.asset` 一致）解：`drawGizmos=1 · defaultRecyclable=0 · defaultAutoPlay=3 ·
  defaultUpdateType=0 · defaultTimeScaleIndependent=0 · **defaultEaseType=6（OutQuad）** ·
  defaultEaseOvershootOrAmplitude=1.70158f · defaultEasePeriod=0`
- ⇒ **前面这些字段与我们 `Assets/Resources/DOTweenSettings.asset` 逐项一致**
  （`useSafeMode=1 · safeModeOptions.logBehaviour=2 · timeScale=1 · unscaledTimeScale=1 ·
  useSmoothDeltaTime=0 · maxSmoothUnscaledTime=0.15 · rewindCallbackMode=0 · showUnityEditorReport=0 ·
  logBehaviour=0 · drawGizmos=1 · defaultRecyclable=0 · defaultAutoPlay=3 · defaultUpdateType=0 ·
  defaultTimeScaleIndependent=0 · defaultEaseType=6 · defaultEaseOvershootOrAmplitude=1.70158 · defaultEasePeriod=0`）。
- 复现方式：UnityPy 载入该 `.assets` → 遍历 `MonoBehaviour` → `get_raw_data()` → `offset 28` 取
  `m_Name` 长度、`offset 32` 取名字 → 找 `DOTweenSettings` → 在原始字节里找 `struct.pack('<f', 1.70158)`，
  **它前 4 字节的 int 就是 `defaultEaseType`**。（本轮探针脚本在 `/tmp`，没进仓库。）

---

## 四、没改的 / 没查清的部分

### 4.1 ⏸ `Hand/CardInteraction.cs:880` 松手回弹 —— **查不到原版出处，按任务分支三不改**

```csharp
var seq = DOTween.Sequence();
seq.Join(card.transform.DOMove(home, CardTween.SnapBackDuration));
seq.Join(card.transform.DOScale(..., CardTween.SnapBackDuration));
CardTween.Use(seq, Ease.OutQuad, card.transform);   // ← 同族「序列层二次弯曲」
```

- 这是 `Hand/**` 里**第三处**「Sequence + 非 Linear」，`A22` 的普查范围（`Shell/Core/Editor`）**没覆盖到它**。
- **原版出处查不到**：`CardTween.cs:21-32` 已记（`R16` 查过）——
  `CardScript__OnTouchDrag.c` 全文 52 行，只有一句卡状态闸，**没有回弹 / `DOMove` / 时长**；
  `SnapBackDuration = 0.22f` 本身也标着「**我们挑的，原版判据没定位到**」。
- **为什么没顺手改**：两端（时长、目标位）都是我们自己的设计，没有原版判据判「该不该弯」。
  硬改 `Linear` = 把「二次弯曲」换成「单次 OutQuad」——**观感会变**（回弹会慢半拍），
  但那是**用 A22 的通用口径去推断一个没有原版的场合**，不是读出来的。
- **建议（交主对话裁）**：若按「A22 口径统一」处理，改成 `Ease.Linear` 即可（**同一批文件、判据已懂、typecheck 秒过**）；
  若坚持「判据只认原版」，则保持现状，等找到原版回弹那一支（**还差**：原版「松手落点不合法 → 回手牌」
  的整条链 —— 建议从 `BattleManager__Update.c` 的 `case` 分支或 `CardScript__MoveBackToHand.c` 入手核）。

### 4.2 只覆盖到的事

- **没有补断言**：能覆盖这两处的宿主是 `Editor/BattleScene.cs`（**黑名单**）。
  建议主对话补一条**非自证**断言（判据已定，直接抄）：
  ```text
  CardTween.Mode = Manual;
  记 a = 某张牌当前位置、b = 目标槽位（Refresh 前后各取一次）；
  hand.Refresh(cards, -1, -1);              // animateRelayout = true
  CardTween.Advance(CardTween.RelayoutDuration * 0.5f);
  // Linear 序列 + 子补间 OutQuad ⇒ 半程进度 = 0.75（改前是 0.9375）
  Check(Distance(pos, a) / Distance(b, a) ≈ 0.75, 0.02);
  ```
  期望值 `0.75` 来自「OutQuad 在半程的值」，改回 `OutQuad` 会读成 `0.9375` ⇒ **结构上不可能两边一起绿**。
- **没跑 Unity**、没看画面；本次只做「曲线形状」的判据对齐，**手感是否与原版一致仍需真机/并排比**。

### 4.3 其它口径差（**本轮没动**，如实记）

1. **旋转是 `DOLocalRotate`（原版）vs `DORotate`（我们 `ToPose`）**。位置两侧都是 `DOMove`（世界坐标），
   旋转这一跳不同。手牌父链 `HandArea` 的 `lossyScale` 不是 1（`损失缩放 7.28`，见记忆
   `warpforge-original-3d-size-bridge`），**父级若带旋转两者会分叉**。本轮**没量**手牌父链的旋转是否为 0。
2. **原版重排有「动画闸」**：`_MoveCardsInHandToPosition` 协程在开补间后把 `+0x61`（`PlayerHand.isDoingAnim`
   —— `PlayerHand__IsDoingAnim.c` 存在）置 1，`WaitForSeconds(timeToPositionCard)` 之后才置 0 并
   `SetColliderInHand` 重新启用点击。我们的 `HandLayout.Place` **没有等价闸**（重排期间手牌仍可点）。
   这是**手牌线**的潜在偏差，**不是缓动问题**，本轮没改；⛔ 别顺手在 `HandLayout` 里加（该闸属于交互层 `CardInteraction`）。
3. `DOKill()`（我们）杀掉该 Transform 上**全部**补间；原版是**按字段各杀各的**（`+0x340/+0x348/+0x2d0`）。
   在手牌这个场合结果一样（同一张牌的三条就是那三条），但语义更宽。

---

## 五、验证与改动清单

```text
 Unity/MyGame/Assets/CardPresentation/Hand/HandLayout.cs        +15 / −1   (LF 869→883 行，CRLF=0)
 Unity/MyGame/Assets/CardPresentation/Hand/CardInteraction.cs   +14 / −1   (CRLF == LF == 1185，没翻行尾)
 Unity/资料/已知的坑.md                                          +4  / −0   (LF，尾换行保留)
```

- **`TMPDIR=/tmp/wf_a26 bash d:/4/Unity/工具/typecheck.sh`** ⇒ **运行时错误数: 0 · 编辑器错误数: 0**。
  - ⚠️ 如实记：**前两次没跑绿，但错全在别人的文件上**（并发代理正在写 `Battle/BattleDoors.cs`
    → 9 条语法错；随后 `Battle/EndPanel.cs` → 4 条 `_rewardsHolder` 未声明），
    **一个都不在我白名单内、一个字节没碰**；等它写完（`EndPanel.cs` mtime 更新）后**第三次 0/0**。
- **行尾**：`HandLayout.cs` 改前 `CRLF=0 / LF=869`、改后 `CRLF=0 / LF=883`；
  `CardInteraction.cs` 改前 `CRLF=1172 / LF=1172`、改后 `CRLF=1185 / LF=1185`；`已知的坑.md` 全程 `CRLF=0`。

---

## 六、顺手发现（都在本件范围外，供调度台分流）

1. 🔴 **`Hand/CardInteraction.cs:880` 是第三处「Sequence + OutQuad」**（§4.1）—— A22 只覆盖 `Shell/Core/Editor`，
   `Hand/**` 只点了两处 `ToPose`。**建议把「全仓 `DOTween.Sequence()` + `CardTween.Use(seq, !Linear, …)`」
   补进 A22 的普查口径**（机械 `grep` 即可，零判据成本）。
2. ✅ **原版 `DOTweenSettings.defaultEaseType = 6` 已坐实**（§3.1）—— 可销 `W58` 那条「原版有没有覆盖默认缓动」的待坐实项。
3. ⚠️ **原版重排的「动画闸」我们没做**（§4.3·2）—— 若要做，落点在 `CardInteraction`，
   判据 = `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c` 的 `+0x61` 置 1/0 + `WaitForSeconds`。
4. 📌 `PlayerHand.PositionCardInHand` 的**「选中卡索引」**参数（`+0x28` 为假时传 `0xffffffff`）在我们这边
   对应 `HandLayout.Refresh` 的 `selSlot`（`HandLayout.cs:757-759`）—— 口径已对，**本件只是复核到**。
