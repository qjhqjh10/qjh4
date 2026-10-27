# D3 · 三个宿主各 1 条红的定性（只读诊断，2026-10-27）

> 口径：全部按【行首标记】数过了 —— `DeckScene.Run` 1931/1932（**1 条真失败**，日志里那 2 行 ✗ 里第 2 行是 `失败重列`）· `CollectionScene.Run` 2474/2475（1 条）· `RewardsScene.Run` 2392 通过 / 1 失败。
> **三条全部是 (α) 断言写错（测试侧），生产实现都没错。** 三条都是本次同步点第一次真跑，无一属于 (γ) 本批回归。

---

## ① Deck · `PlayInspectCue` 的 cue 断言 —— (α) 断言读得太晚

**原文**（`Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs:10302-10305`）

```csharp
CheckTrue(WindowSounds.LastCue == WindowSounds.ButtonClick1Cue,
          "★ 点一次 ⇒ 壳侧播放口收到的 cue = `" + WindowSounds.ButtonClick1Cue + "`"
        + "（原版 `inspectSound.Play2D()`）｜🧨 把 `PlayInspectCue` 改回只出声 ⇒ 本条红；实测 = "
        + (WindowSounds.LastCue ?? "<null>"));
```

**日志**（`d:/4/_tmp_view/deck.log:153242`）：`实测 = ButtonClick Back`。

**定性：α**（断言侧错：在一个**已被后续动作覆盖**的字段上取值） —— 不是实现没接，也不是读错 sink。

**证据链（逐跳）**
1. 点一下确实播了 `ButtonClick 1`：`Shell/ItemDrawer.cs:1601` `HandlePointerClick(); PlayInspectSound();` → `Shell/ItemDrawer.cs:2018-2024` `PlayInspectCue()` → `WindowSounds.PlayFx(WindowSounds.ButtonClick1Cue, …)`。cue 在表里（同一次运行 `deck.log:152298` 那条 ✓ 已证）。
2. `LastCue` 是**每次 `Play` 都无条件首写**的一个槽：`Shell/WindowSounds.cs:186` `LastCue = cueName;`（在「表里查不到 / 被 `timeToPlayAgain` 节流」这两个分支**之前**）⇒ 只要有任何一声经过这个口，它就被覆盖。
3. 断言之前**刚好**有一声：`Editor/DeckScene.cs:10300` `dip.Close();` → `Shell/GameWindow.cs:757` `PlayCloseSound();` → `:817-826` `WindowSounds.PlayFx(WindowSounds.DefaultCloseCue, …)`，而 `DefaultCloseCue = "ButtonClick Back"`（`Shell/WindowSounds.cs:49`）。
4. ⇒ 断言读到的是**关窗音**，不是点击音。关窗音是原版行为（`GameWindow__Close.c` 第 ③ 句），⛔ 不是缺陷。

**该怎样**：把「点击音」在关窗**之前**取下来再断 —— 例如在 `:10274` `view.OnPointerClick();` 之后立刻 `var cueAfterClick = WindowSounds.LastCue;`，`:10302` 改断 `cueAfterClick`（断言文案里的「实测」也改印它）。⚠️ **别把 `dip.Close()` 挪到断言之后就当修好** —— 那样断的仍是同一个槽，只是赌目前没有第二个写点；取快照才有鉴别力。另：`🧨 把 PlayInspectCue 改回只出声` 这条灭自证**在当前写法下依然成立**（那时 `LastCue` 会是 `ButtonClick Back` ⇒ 仍红），所以这条修的是**指向性**，不是放过一条假红。

---

## ② Collection · 「徽记阵营置成了 Neutral(0)」—— (α) 断言读得太晚（同一类）

**原文**（`Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs:12758-12759`）

```csharp
win.RebuildCardsPageWithArmyForTest(ArmyIcons.Neutral);
CheckTrue(win.WildcardArmyForTest == ArmyIcons.Neutral, "（前提）徽记阵营真的置成了 Neutral(0)");
```

**日志**（`d:/4/_tmp_view/collection.log:196502`）：期望 `[True]`、实得 `[False]`；**紧随其后的三条（`Neutral ⇒ 不播` / `rate 停在 1` / `推进 0.2 也不动`）全 ✓**。

**定性：α** —— 前提断言去读一个**生产代码故意会重算**的字段。

**证据链**
- helper 本身没错：`Shell/CollectionWindow.cs:6471-6475` `_wcArmy = army; RebuildCardsPage();`。
- 但重建链的**尾部**会把这个字段算回去：`BuildCardsPage`（`:1002`）里 `:1043` 先 `BuildWildcardShine(wcBg)`（**闸用的就是此刻的 `_wcArmy` = Neutral** ⇒ 正确地不播），随后 `:1158` `RebuildCardsCells(holder)`，后者**尾句** `:1728` `CheckFocusedArmy();` → `:5121-5133` 拿固定参考矩形 `WcRef`（402.3,236.4→596.1,281.4）命中的**第一格** → `SetWildcardArmy(ArmyIcons.Id(list[i].Faction), …)` → `:5076` `_wcArmy = army;`。
- 这个重算**是原版行为**（`CardCollectionDisplay.RefreshCollection` 末尾那次 `CheckFocusedArmy`），不是我们加的；本窗 setup 已实读为 Ultramarines（`collection.log:149791` 那条 ✓，图 = `40k_DeckSelection_icon_FactionUM`）。
- `_wcArmy` 全类只有 3 个写点（`:1258` 初始化 / `:5076` `SetWildcardArmy` / `:6473` 本 helper），且 `SetWildcardArmy` 对 0 会**早退**（`IconName(0)` 为空，`:5067-5075`）⇒ 重建之后**不可能**再是 0。
- 顺序无关：把 helper 里的 `_wcArmy = army` 挪到 `RebuildCardsPage()` 之后也没用（会把「闸读到什么」变成假的）。

**该怎样**：把前提断言从「重建**之后**的 `_wcArmy`」改成「`BuildWildcardShine` **那一刻**读到的 `_wcArmy`」—— 在 `Shell/CollectionWindow.cs:6449` 那道闸旁边加一个只读快照（如 `WildcardArmyAtShineBuildForTest`），断言 `== ArmyIcons.Neutral`。⛔ **不要只是删掉这条前提**：它是「闸真的被跑到了」的唯一直接证据（虽然 `rate == 1` 那三条事实上已经不空转，但一旦将来 `_wcShine` 换成不重建的实现，那三条会退化成空转绿 —— 5·b/铁律 3 的先例）。
（可选顺手把「CheckFocusedArmy 会重算」也钉一条：`WildcardArmyForTest != Neutral`，这一条是**生产行为**的断言，与闸那条并排。）

---

## ③ Rewards · `实例 alpha 推到 0.75` —— (α) 断言把「恒定速率」当成了「Lerp」

**原文**（`Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:6845-6850` 造局、`:6862-6864` 断言）

```csharp
fader.DoScenarioBlend(new ScenarioBlendOptions { duration = 1f, direction = true, targetValue = 0.5f, … });
fader.Advance(0.5f);
…
CheckTrue(instFader != null && Mathf.Abs(instFader.color.a - 0.75f) < 0.01f,
          "★ A1967 残尾·ScenarioMaterialFader：实例 alpha 推到 0.75（实得 " …);
```

**日志**（`d:/4/_tmp_view/rewards.log:26811`）：`实得 0.5000`；`源材质 alpha 没被写脏（实得 1.0000）` 那条 ✓。

**定性：α** —— 期望 `0.75` 只有在 **Lerp** 语义下成立；原版是 **`MoveTowards`（恒定速率、步长 = `dt / duration` 的绝对 α 量）**，本题恰好一步跨完整段差距。

**算式（实得值的来源）**
- `Battle/ScenarioMaterialFader.cs:106-107`：`_fromA = CurrentAlpha()` = **1.0**、`_toA = o.targetValue` = **0.5**；
- `:115` `_left = 1f`；`Battle/ScenarioBlendable.cs:43-45` `Advance(0.5)` ⇒ `_left = 0.5`（未完成）；
- `ScenarioMaterialFader.cs:125` **t = 1 − 0.5/1 = 0.5** → `:130` `SetAlpha(Mathf.MoveTowards(1.0, 0.5, 0.5))` = **0.5**（差值恰好等于步长 ⇒ 直接贴到目标）。
- ⇒ 不是「推到了又被复位」，也不是「没推」——**一步就到位**。

**原版判据**（一手）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ScenarioMaterialFader.cs` 的 `Update()`：
`float num = deltaTime / blendTime;` → `float num3 = targetFade - color.a;` → `if (num < |num3|) num2 = sign(num3)*num + color.a;` → `material.color = …`，**之后**才 `currentBlendTime -= deltaTime`。
⇒ 步长是**绝对 α 量**、按 `dt/duration` 走，⛔ 不是 `Lerp`。我们 `:130` 那句与它逐字同义。

**该怎样**：把探针改成能区分「恒定速率 vs Lerp」的一档 —— 最小改动 = `:6847` `duration` 改 **2f**（`Advance(0.5)` ⇒ 步长 0.25 ⇒ α = **0.75**；若是 Lerp 会读到 0.875 ⇒ 仍然红）；或保持 `duration = 1f` 而 `Advance(0.25f)`。⚠️ 断言值 `0.75` 与文案都不用动，只动探针参数 —— 现有写法（`duration=1` + `target=0.5` + `Advance(0.5)`）在两种语义下**都**得到 0.5，**没有鉴别力**，这也是它为什么会写错：作者按 Lerp 心算，而算式刚好撞上同一个数的一半。

---

## ④ 顺手发现（未实跑验证影响面，⛔ 不计入上面三条）

`Battle/ScenarioMaterialFader.cs:118-126` 的次序与原版**相反**：原版是「**先写 α**，再看 `currentBlendTime <= 0` 做收尾」；我们是 `base.Advance(dt)`（到点直接 `Complete()`）→ `if (_busy) ApplyFade(...)` ⇒ **走完的那一帧不写 α**，停在「差最后一步」。两条后果都只在**不收尾还原**的档可见：`direction = true` 且 `restoreOriginalMaterialsOnFadeIN = false`（fadeOnEnable 的淡入就是这一档）⇒ 终值 ≈ `1 − dt/duration` 而不是 1.0；收尾会 `RestoreOriginalMaterials` 的档看不出来。
- 现状：本批没人断「终值 = 1.0」，所以它今天不红。
- 建议：另立一条账（改法 = 先 `ApplyFade`、后判完成，或把 `Complete()` 挪到 `ApplyFade` 之后），并在改的时候补一条「推进到刚好走完 ⇒ α == 目标」的断言。**我没有实跑验证它是否在原版里也看得见**，只按两份反编译的**语句次序**判，等级 = 疑点。

---

## 汇总

| # | 宿主 | 断言位置 | 定性 | 一句话修法 |
|---|---|---|---|---|
| ① | DeckScene | `Editor/DeckScene.cs:10302` | **α** | 在 `dip.Close()` **之前**取 `LastCue` 快照，断快照 |
| ② | CollectionScene | `Editor/CollectionScene.cs:12759` | **α** | 改断「`BuildWildcardShine` 那一刻的 `_wcArmy`」快照（⛔ 别只删） |
| ③ | RewardsScene | `Editor/RewardsScene.cs:6862` | **α** | 探针 `duration` 改 2f（或 `Advance(0.25f)`），期望 0.75 不变 |
