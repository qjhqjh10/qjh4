# B123_C3 · `CardFeel.HandToBoard` 的 `OnKill` 复位链（对账条目 `B121-Q4B-02`）

> 执行写手 `B123-C3` · 2026-11-03 · 批次 `B123`
> 判据 → `资料/完全复刻/判据/B123_对账_C_表现外壳.md` §14（＋ 总表那一行）
> ⛔ 本代理**没跑 Unity**、**没动 git**、**没改账主表 / 正本 / 任何 `*Scene.cs`**

---

## 一、结论

`B121-Q4B-02` 已修完：`CardFeel.HandToBoard` 的 `OnKill` 原来只复位三档 α，现在**带「是否正常跑完」闸**，
并且**只在没跑完时**把这条 clip 改过的 **9 项**逐项复位到「起播前那一档」。
写法与 `Hand/CardInteraction.cs:2742-2751`（撤销出牌那条路）**逐项等价**，⛔ 没有新造状态。

今天能看到的路径本来就没错（撤销那一侧自己补了一遍）；本条堵的是「**别处复用会静默串味**」。

---

## 二、改了什么（文件:行号，现读）

### 1. `Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs:2190-2194`（**零行数变化**）

```csharp
2190  var t = CardTween.Use(seq, Ease.Linear, tr);
2191  // 🔴 `B121-Q4B-02`（`B123-C3`）：**「这条 clip 正常跑完了没有」必须自己记** —— …
2192  bool finished = false;
2193  t.OnKill(() => { if (card == null) return; card.SetAlpha2D(1f); card.SetCounterAlpha(1f); card.SetCountersForcedVisible(false); if (!finished) ResetHandToBoardResidue(card, body, bodyBase, boardEl, boardElBase); });
2194  t.OnComplete(() => { finished = true; if (onDone != null) onDone(); });
```

- 原来那 4 行（两行注释 + `OnKill` 一行 + `if (onDone != null) t.OnComplete(...)` 一行）**就地改写**成同样 4 行
  ⇒ `CardFeel.cs` 在 `:2190` 之后有 **235 条**账按行号引（现读实测），**行号一个没动**。
- `OnComplete` 在 DOTween 里是**单槽**（再设一次会覆盖前一个）⇒ 标志与 `onDone` 必须写在同一个 lambda 里。
- α 三档照旧**无条件**复位（正常跑完时它们本来就是 1 / 不强制；复位是安全的一侧，同 `DealIn` 那条注释）。

### 2. `Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs:4505`（**新方法追加在类尾**）

```csharp
static void ResetHandToBoardResidue(CardView card, Transform body, Vector3 bodyBase,
                                    Transform boardEl, Vector3 boardElBase)
{
    if (card == null) return;
    card.EndFrameDissolve();                                          // ① 卡框溶解材质实例 → 还原原材质
    card.RestoreOriginalMaterial(); card.SetDissolveAmount(0f);       // ② 3D 卡体材质 + `_DissolveAmount`
    card.SetBody3DVisible(false);                                     // ③ 3D 卡体可见性
    card.SetSpawnParticlesActive(false); card.SetTrailActive(false);  // ④ `ToBoardFX` 两颗粒子（两项）
    card.SetArtLocalScale(1f); card.SetFaceRootLocalZ(0f);            // ⑤ 立绘缩放 / `2DCard` 的 z
    if (body != null) { … p.y = bodyBase.y; … }                       // ⑥ `body3D` 的 y
    if (boardEl != null) { … p.y = boardElBase.y; … }                 // ⑥ `Board Elements` 的 y
}
```

- **为什么追加在类尾**：`CardFeel.cs` 中间插一行 = 下游 235 条账整体漂移（铁律 18·b）。
- **两级 y 用起播时捕获的 `bodyBase` / `boardElBase`**（`:2079` 捕获，clip 的位移曲线本身就是「基准 + 偏移」）
  ⇒ 这才是「起播前那一档」；撤销那条路用的是常数 `-CardView.Height*0.5f` / `0`，对**刚建出来的卡**两者同值，
  但常数只在那种情况下成立（本方法不另填常数）。

---

## 三、判据：为什么必须带「是否正常跑完」闸

**DOTween 的 `OnKill` 在正常跑完时也会触发**，而且是**先 `onComplete` 再 `onKill`**。这不是传闻，是反编译现读：

- `Assets/Plugins/Demigiant/DOTween/DOTween.dll`（`ilspycmd` 现读）：
  - `TweenManager.DoUpdate` 末尾：`if (t.isComplete && !flag2 && updateMode != IgnoreOnComplete && t.onComplete != null) OnTweenCallback(t.onComplete, t);` → `if (t.autoKill) return t.isComplete;`
  - 完成 ⇒ 调用方 `MarkForKilling` → `TweenManager.Despawn(t)` → **`Despawn` 第一句就是 `Tween.OnTweenCallback(t.onKill, t)`**。
- `Tween.Kill(bool complete)`：`if (!DOTween.initialized || t == null || !t.active) return;`（⇒ 对已不活跃的补间再 `Kill` 是安全空操作，幂等）
  → `TweenManager.Despawn(t)` → `onKill`。

⇒ 若**无条件**做那 9 项，正常跑完那一刻会把**已经换成场上形态**的卡：3D 体关掉（新缺陷）、立绘缩放打回 1、
`2DCard.z` 打回 0、两颗粒子关掉。这就是「修一个残留、造一个更显眼的缺陷」。

⚠️ **自证不了的那半边（如实记）**：`OnComplete` → `OnKill` 的先后关系是**反编译读代码**得出的，
本批**没有 Unity 可跑** ⇒ 由断言 `N2/N3/N4/N5`（正常跑完那一组）在宿主上首次跑的时候兜底验证。

---

## 四、断言（新建 `CardPresentation/Editor/B123_C3_Assert.cs`）

入口 = `B123C3_Assert.Run()`；前缀 `[B123C3] ✓ / ✗`；`Pass` / `Fail` 静态计数。

| 组 | 覆盖 | 关键几条 |
|---|---|---|
| ① `N*` 正常跑完 | 跑满 1.2 s（> 0.9167，autoKill 走完 `onComplete`→`onKill`） | `N2` **3D 体仍可见** · `N3` 立绘缩放停在 `HandToBoardShrink`(0.905) · `N4` `2DCard.z` 停在 `-0.043` · `N5` 两颗粒子仍开着 · `N6` 卡框已还原 · `N7/N8/N9` 三档 α |
| ② `K5` @0.50 s 打断 | **3D 溶解材质 + `_DissolveAmount`** 那一对（0.55 clip 自己会还原 ⇒ 必须 0.55 前打） | 逐项 `AssertClean`：9 项 + 三档 α，共 14 条 |
| ② `K6` @0.56 s 打断 | **`Trail`**（0.55 才打开 ⇒ 必须 0.55 后打） | 同上，14 条 |
| ③ `D1–D5` 幂等 | 同一补间 `Kill` **两次**；再起一条 clip 再打断 | `D2` 两次 Kill 后快照逐字段不变 · `D4` 第二次打断仍回到**同一个**基准（不累积漂移） |

- **前提断言是硬检查**（⛔ 没写 `if (x != null) Check(...)`）：`N0` / `K0` / `P0-P2` / `K1`（起播前必须是干净手牌态）/
  `K3…`（打断处必须**真的脏**）都直接 `Check`，不成立就红。
- **为什么要打两刀**：这条 clip 自己在 **0.55** 既还原 3D 溶解材质、又才打开 `Trail`
  ⇒ 只打一刀必然有 2 项「空转绿」（绿了但什么也没验到）。这一点写在测试方法头，别后人合成一刀。
- **两条溶解的前提**（`K4` / `K4'`）是硬断言 —— 取不到原版 shader 就**红**（如实说「这半边没验到」），不静默跳过。
  这与 `Editor/CardBaseDemo.cs` 的 B05 那组（`Check(can, …)`）同一口径。

**🧨 灭自证（怎么才能红）**

- 把 `CardFeel.cs:2193` 的 `if (!finished)` 拆掉（改成无条件调 `ResetHandToBoardResidue`）
  ⇒ `N2` / `N3` / `N4` / `N5` **一起红**（那是「正常跑完把场上卡打回手牌残留态」）。
- 删 `ResetHandToBoardResidue` 里任意一行 ⇒ **对应那一格**红（9 项逐项一格，谁漏谁红）。
- 把 `K3…` 那批前提改松（例如删掉「必须脏」的反向钳制）⇒ 该组断言会**空转绿** —— 这正是要防的。

**夹具**：`CardView.Create(root, CardData.Placeholder(4), name, CardFace.Hand)` ＋
`SetFace(Board); SetFace(Hand)`（借场上形态把 3D 体建出来，同 `CardBaseDemo` B05 / `CardFaceAssertE5` 的做法）；
粒子自己造一对 `Spawn Particles` / `Trail` 用 `CardView.MountSpawnParticles` 挂上（真件由驱动层挂）；
`CardTween.Mode = UpdateType.Manual` + 逐帧 `CardTween.Advance(1/60)` 推进，收尾复原。

---

## 五、接线补丁（**给调度台**，本代理不动宿主）

建议挂在 `Unity/MyGame/Assets/CardPresentation/Editor/CardFaceAssertE5.cs` 的 `Run()` 里
（现读 `:88` 是 `RunSoundCueAssertT();`，在同一 `try` 内、`finally` 恢复 `CardTween.Mode` 之前）。
**零行数变化**的做法 = 把它追加到 `:88` 那一行**物理行尾**（该行没有 `//`）：

```csharp
            RunSoundCueAssertT(); B123C3_Assert.Run(); _pass += B123C3_Assert.Pass; _fail += B123C3_Assert.Fail;
```

⚠️ 三条理由与注意：
1. `B123C3_Assert.Run()` **自己**保存/恢复 `CardTween.Mode`（内部置 `Manual`）⇒ 不搅乱宿主那一档；
2. 本件**不调** `DOTween.KillAll`（会打死宿主上别人家的补间），只清它自己最后造的那一条；
3. 若不喜欢挂 E5（它已经有 `BT T` 前缀的混排先例），也可以挂 `BattleScene.Run` 的方法尾聚合行
   ——**无论挂哪儿，都必须落在「第一个 `//` 之前」**（铁律：追加到行尾注释之后 = 永不执行）。

---

## 六、验证（本批能做的都做了）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查（运行时 + 编辑器两个程序集） | `TMPDIR=/tmp/wf_c3 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 账落点闸（全仓） | `python -I -X utf8 工具/check_ledger_points.py` | **A 5899 条 · 通过 5899 · 漂移 0 · 失败 0** |
| 补丁行本身过闸 | 把补丁行替换进 `战斗界面_HUD.tsv` 的临时副本再跑（只读，副本已删） | 该表 **A 575 / 漂移 0 / 失败 0 / 告警 39**，与改前**逐字相同** |
| 账补丁格式 | 裸 `split("\t")`：表头 11 列、数据行 11 列 | ✅ |

⛔ **没跑 Unity**（子代理不许 `-executeMethod`）⇒ 断言**尚未执行过**，红绿待调度台在同步点接宿主后定。

---

## 七、还差什么 / 边界与顺手发现

### 边界（不是缺陷，写清免得后人当成回归）

1. **0.7 s 之后被打断**：clip 在 0.7 已 `SetFace(Board)`，此时打断会按「手牌残留态」收口（3D 体被关）。
   今天**唯一的打断口**是撤销出牌（`CardInteraction.CancelDeployFlight` → 驱动层 `BattleDriver.CancelDeployFlight`
   在 `:41726` kill 这条补间），那条路本来就要回手牌 ⇒ 一致。将来若出现「打断但仍想留在场上」的调用点，
   要在那一侧自己重设形态（⛔ 别在这里加状态）。
2. **撤销那一侧现在会复位两遍**：`CancelDeployFlight`（驱动层）kill 补间 ⇒ 我们的 `OnKill` 先复位一遍，
   随后 `CardInteraction.cs:2742-2751` 又复位一遍。两遍都是幂等的（`EndFrameDissolve` 自己就幂等），
   **今天无副作用**；但它是「两处写同一条规则」的形状（CLAUDE.md §三），见下面「顺手发现 1」。

### 顺手发现（不在本代理白名单，**未改**）

1. **`Hand/CardInteraction.cs:2742-2751` 那 9 行现在是冗余的第二份**。
   判据：那条路先 `tween.Kill(false)`（`:2732` → 驱动层 `:41726` kill 登场 clip ⇒ 我们的 `OnKill` 已经复位完），
   之后才走 `:2742-2751`。⇒ 建议后续**删掉那 9 行**并让注释指向 `CardFeel.ResetHandToBoardResidue`（判据只留一处）。
   ⚠️ 注意两点：① 该文件有账按行号引（删 9 行会让下游落点漂，要按 `check_ledger_points --fix` 的既定流程）；
   ② 删之前必须确认「驱动层 `CancelDeployFlight` 一定被调到」——`CardInteraction` 里是先
   `if (OnCancelDeployFlight != null) OnCancelDeployFlight(card);`，**该委托可为 null**（注释写着「不接也能跑」）
   ⇒ 若真有宿主不接，删掉那 9 行会让撤销留下残留。**这就是当初为什么两边都写**，建议**先记账、不动**。
2. **`ReturnToHand`（`CardFeel.cs:1887`）的 `OnKill` 也有同族形状**：它复位 `EndDissolve` / 三档 α / `SetFaceGroupVisible`，
   但**不复位**立绘缩放 / `2DCard.z` / 两级 y —— 那条 clip **本来也不动**这三项（它只动 α / 面组 / 溶解）⇒ **不是缺陷**，如实记一句。
3. `CardFeel.HandToBoard` 的 `onDone` 语义没变（仍是「0.9167 全条跑完」）；原版那个 **0.55** 的
   `CardHandToBoardAnimationFinished` 仍由 `HandToBoardDone` 那条 `InsertCallback`（`:2174`）承担 ⇒ 两件事别混。

---

## 八、账

补丁 = `资料/完全复刻/账/_parts/B123_C3.tsv`（11 列，**改的是既有行**
`BattleCardUI.CardHandToBoardAnimationFinished()`，靠 `(原版类, 方法)` 命中 `战斗界面_HUD.tsv:102`）：
判据要点 / 落点（加了 `ResetHandToBoardResidue` 的次要落点）/ 差异（写明「DOTween 正常跑完也触发 `OnKill`」）/ 备注 四处补新内容。
