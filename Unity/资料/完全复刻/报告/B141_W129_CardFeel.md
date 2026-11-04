# B141-W47 报告：11-10 `CardFeel.Dissolve` 三处形状偏离（W129）

> 执行写手 `B141-W47` · 会话第一百二十八 · 2026-11-04
> 判据包：`判据/B141_W129_对账_UI动画.md` **W21** → 行 **11-10** · 审计源：`报告/B138_审计汇总_问题清单.md` §11 `11-10`
> 白名单：`Core/CardFeel.cs` · 新建 `Editor/B141_W47_Assert.cs` · 本报告
> （⛔ 未动账主表 / 活状态表 / CLAUDE.md / 所有权表 / 任何宿主）

---

## 一、结论

**选「改成原版语义」，不选「删死路」。** 理由两条：

1. `CardFeel.Dissolve` **不是零消费者**：`Editor/BattleScene.cs:23080 / 23083 / 23116` 三处在用（`Dur(...)` 时长断言 ×2 + F1 兜底支断言 ×1）⇒ 删掉会直接编译不过（白名单外，改不了）。
2. 它在 `CardFeel.cs:3106-3118`（F1 前置那段头注）里被显式定为「卡体改不透明之后**最后的安全网**」—— 删掉 = 把「取不到溶解材质」变成「卡体啪地留在场上」。

三处偏离已逐条按原版 `CardScript.DissolveWholeCard` 改掉。

---

## 二、判据（🥇 一手，本批亲读）

`d:/2/tools/decomp_full/CardScript__DissolveWholeCard.c:1-54` 全文：

| 原版语句（`.c` 行号） | 语义 |
|---|---|
| `:18` `uVar4 = Component.get_transform(param_1)` | 目标是**卡根 `transform`**（= 我们的 `card.transform`） |
| `:23-24` `local_38/local_30` = `Vector3.one` 静态块（`DAT_1842da2a8` + `0xb8`，`+0xC` = `one`） | 终点向量取自 **`Vector3.one`** |
| `:28-31` `× *(float*)(lVar5 + 0x40)` = `VarsGlobal.spellPlayedScale` | 值 = **1.0**（`资料/VarsGlobal_原版数值.md` §一；`工具/read_varsglobal.py` 解出） |
| `:32` `DG_Tweening_ShortcutExtensions__DOScale(uVar4, &local_38, param_2, 0)` | **唯一的运动补间**：缩放到 `one × 1.0`；**整个方法没有 `DOMove`**；**没有 `SetEase`** ⇒ DOTween 默认 `OutQuad`（`DOTweenSettings.asset:31 defaultEaseType = 6`） |
| `:41` `BattleCardUI__FadeCardItems(ui, 0, param_2, 0)` | 淡的是 **`fadeGroup`**（`BasicCardUI.FadeCard` = `DOTween.To(fadeGroup.alpha → 0)`，账 `战斗逻辑驱动:730` + `B9_E2UDW` §②）⇒ 我们的等价量 = **`CardView.Alpha2D`**，`SetAlpha` 会连 3D 卡体一起淡 |
| `:34-39` `StopShowingCodex` / `StopReflected` / `ToggleHighlight(false)` | 与本包无关（账 `战斗界面_HUD:508` 已另记），本批未动 |

> 交叉印证（同一结论的既有落地点）：`Battle/BattleDriver.cs:35467-35511` 的 `DissolveWholeCardInField` 就是按同一份 `.c` 写的（`DOScale(Vector3.one, t)` + `SetAlpha2D` + `Ease.OutQuad`）—— 本批让 `CardFeel.Dissolve` 与它**同形**。

---

## 三、原版值 vs 改前 / 改后

| # | 项 | 原版值 | 我们改前 | **我们改后** |
|---|---|---|---|---|
| 1 | 位移 | **无** | `DOMove(tr.position + ToOurs(0.3), dur)` · `Ease.InSine`（注释自陈「上浮量：我们挑的」） | **整条删掉** |
| 2 | 缩放终点 | `Vector3.one × spellPlayedScale(1.0)` = **`Vector3.one`** | `tr.localScale * 0.85f` · `Ease.InQuad` | **`DOScale(Vector3.one, dur)`**（无 `SetEase`） |
| 3 | 淡的层 | `fadeGroup`（= **`Alpha2D`**） | `card.Alpha` → `SetAlpha`（整卡） | **`card.Alpha2D` → `SetAlpha2D`** |
| 4 | 缓动 | DOTween 默认 **OutQuad** | `InSine`（位移） / `InQuad`（缩放） | **OutQuad**（默认） |
| 5 | 时长 | `deathTimeMinionDuration 0.2` / `deathTimeWarlordDuration 0.5` | 同（已对） | **未动** |

### 改动清单（`Core/CardFeel.cs`）

| 物理行 | 改了什么 |
|---|---|
| `:3086` | 警告文案「淡出 + 上浮 + 缩」→「淡出（2D 卡面 Alpha2D）+ 缩到 Vector3.one」 |
| `:3100-3102` | 方法头注：删掉「我们没有溶解 shader，退而用『透明度 + 轻微上浮/缩小』—— 表现形式是我们的」（**已过期 + 自设计**），换成原版形状 + 就地订正痕迹 |
| `:3112` | F1 那段头注：「退回旧的整卡淡出」→「退回 2D 卡面淡出」 |
| `:3125` | `FallbackPath.WholeCardAlpha` 的 doc：注明名字是历史名、**实际动作已改成淡 `Alpha2D`** |
| `:3142-3143` | 出声文案：「退回整卡淡出」→「退回 2D 卡面淡出（`Alpha2D` = 原版 `fadeGroup`）」 |
| `:3156-3159` | **主改动**：删 `up`/`DOMove`/`×0.85`，改成 `seq.Append(tr.DOScale(Vector3.one, dur));` + `// ✅ 已复刻（2026-11-04 · B141-W47 · 判据 decomp_full/CardScript__DissolveWholeCard.c:18-32 …）` |
| `:3160` | 「拿不到溶解材质才退回整卡淡出」→「退回 2D 卡面淡出（`Alpha2D`）」 |
| `:3171` | `card.Alpha/SetAlpha` → **`card.Alpha2D/SetAlpha2D`** |

**行数变化：0**（`4510 → 4510`；`git diff --numstat` = `12 12`）。
理由：该文件下游有账按行号引用的落点（本批现核：`CardFeel.cs:3266 / 3282 / 3306 / 3347`）⇒ 一律**同物理行改**。
现读复核：这四行改后内容仍是 `TurnCardAround` / `enemyUnitsScaleRatio` 读数表注 / `Reassemble` / 注释行 —— **无漂移**。
（同批已把 `:3080` 附近行尾翻车风险排掉：`io.open(p,'rb')` 读、断言 `b'\r' not in raw` 后才写；现读 `newlines 4510 / crlf 0 / lone_cr 0`。）

**未动的部分（有意）**：材质溶解支（`canDissolve` → `_DissolveAmount` 0→1 · `Ease.Linear`）是 F1 那一轮的设计落点，本包的判据（`DissolveWholeCard`）不管它，故保留。

---

## 四、验证

```
TMPDIR=/tmp/wf_b141_w47 bash 工具/typecheck.sh
  运行时错误数: 0
  编辑器错误数: 0
```
（本批两次跑：改完 `CardFeel.cs` 一次 · 加完断言文件再一次，均 **0/0**。）

新增断言 **`Editor/B141_W47_Assert.cs`**（静态类 · 全局命名空间 · 类名 == 文件名 · `B141_W47_Assert.Run()`）：
**22 条**，分两组 ——

| 组 | 条数 | 断什么 |
|---|---|---|
| 前提 | 2 | `P0` 探针卡建得出 · `P1` 探针起点缩放 = **0.36**（真生效） |
| A（兜底支 `WholeCardAlpha`） | 13 | `A0` 补间非 null · `A1` 走的是兜底支 · `A1b` 时长仍 0.2 · `A5a★`/`A5b★` 缓动 = OutQuad（两相位）· `A2★` ×3 无位移 · `A3★` ×3 缩到 1 · `A4★` Alpha2D→0 + **Alpha 不动** |
| B（正常支 `MaterialDissolve`） | 7 | `B0` 探针建得出 · `B1` 支持溶解（前提）· `B2` 补间非 null · `B3` 走材质支 · `B4` `_DissolveAmount`→1 · `B5` 无位移 · `B6★` 同样缩到 1 |

### 灭自证（每条怎么才会红）

| 把什么改回去 | 哪条红 |
|---|---|
| 加回 `seq.Append(tr.DOMove(...))` | `A2★` ×3（位置变了） |
| `DOScale(Vector3.one)` → `(tr.localScale * 0.85f)` | `A3★` ×3（探针起点 **0.36 ≠ 1**，终点 0.306） |
| `SetAlpha2D` → `SetAlpha` | `A4★` 两条（`Alpha` 变 0 / `Alpha2D` 停 1，各红一条） |
| 缩放加回 `SetEase(Ease.InQuad)` 或 `InSine` | `A5a★`/`A5b★`（现读 `Elapsed()` 后与三条解析曲线比：u=0.25 处 OutQuad 0.4375 / Linear 0.25 / InQuad 0.0625） |
| 只改兜底支、漏改材质支的缩放 | `B6★` |
| 恢复「整卡淡出」口径 | `A4★` 第二条 |

> ⚠️ `A3★` 的**区分度来源就是探针起点 0.36**：若探针从 1 起，「缩到 one」与「×0.85」都不动/都到 0.85，条就废了 —— 所以 `P1` 是硬前提断言，不是摆设。

> ✅ **已接线（2026-11-04 `B141-W47b`）**：`Editor/BattleScene.cs:47035` 的聚合行、合计 `Debug.Log` **之前** ——
> `B141_W47_Assert.Run(); pass += B141_W47_Assert.Pass; fail += B141_W47_Assert.Fail;`（同一物理行追加 ⇒ 行数零变化；插入点在**第一个 `//` 之前**、且不在任何 `/* */` 里 —— 脚本里三条断言把过关）。
> ⚠️ 仍未**真跑**：本波 ⛔ 不跑 Unity ⇒ 22 条的实际红绿要等下一次 `BattleScene.Run`。

---

## 五、必须跟进（⛔ 白名单外，本批不能改）

1. ✅ **已做（2026-11-04 `B141-W47b`，白名单扩到 `Editor/BattleScene.cs` 后一口气收掉）** ——
   `Editor/BattleScene.cs:23121` 的 `Check(f1.Alpha < 0.05f, …)` 钉的是**旧口径**（淡整卡 `Alpha`），
   本批改成 `SetAlpha2D` 之后 `f1.Alpha` 恒 1 ⇒ 会红。**现已改** `Check(f1.Alpha2D < 0.05f, …)`，
   `:23124` 的 `f1.SetAlpha(1f);` → `f1.SetAlpha2D(1f);`，`:23118` 文案「退回整卡淡出」→「退回 2D 卡面淡出」，`:23122` 的读数也改成 `α2D={f1.Alpha2D:F3}`。
   （`:23135` 那条 `Mathf.Abs(f1.Alpha - 1f) < 1e-4f` 验的是 `ReturnToHand` 不碰 `_alpha` —— **不受影响、未动**。
   ⚠️ 但它的**强度**变了：现在兜底支压根不碰 `Alpha`，所以这一条从「反证」变成了「恒真」——不红、只是变弱；要不要改成断 `Alpha2D` 由调度台定，本批**未动**。）
   ⛔ 这是「断言把错口径钉死」的又一例（CLAUDE.md §三 有同族记录）；**不要**为了让它绿而把 `SetAlpha2D` 改回去。

2. 🟠 **`FallbackPath.WholeCardAlpha` 这个名字已名不副实**（现在淡的是 2D 卡面，不碰 `_alpha`）。
   但 `Editor/BattleScene.cs` 两处按这个名字断言 ⇒ 本批**不改名**，只在 `:3125` 的 doc 里注明是历史名。
   要改名需与宿主同一批做（一个写手同时改两文件）。

---

## 六、❓ 顺手发现（未做 · 留给调度台定）

1. ❓ **原版阵亡其实是「两段缩放」，我们只有后一段。**
   `CardScript._DestroyCard_d__269__MoveNext.c:28-42`（状态 0）在调 `DissolveWholeCard` **之前**先
   `DOScale(transform, Vector3.one × DAT_1834b2f58(0x1834b2f58 实读 = 1.2), param_1+0x2c)` 再 `WaitForSeconds` 同一时长；
   状态 1 才 `DissolveWholeCard(timeToDissolveCard 0.2)`。
   ⇒ 原版：**0.75/0.645（在场缩放）→ 1.2（放大一拍）→ 1.0（回落）并淡 2D**；我们 `CardFeel.Dissolve` 只有后一段。
   **本批没做**：判据包 11-10 只列了三处偏离，加「1.2 那一拍」属**新行为**，且需要先定它的时长参数
   （`param_1+0x2c` = 调用方传给 `DestroyCard` 的第 2 个实参；`BattleDriver.cs:39557` 记的是 `0.3f`，**未逐句复核**）。
   ⇒ 建议立成单独一条（`CardScript.DestroyCard` 状态 0 的缩放段）。

2. ❓ **缩放语义的**规模**对不上（不是值错，是坐标系不同）**：我们在场卡根缩放 = `ArenaSlots.PlayerCardScale 0.36`
   （= 原版 `MinionArea.desiredScale`），而原版 `MinionManager.SummonMinion` 把卡根设成
   `GetUnitSizeInPlay` = **0.75 / 0.645**（`MinionManager__SummonMinion.c:35,49`）。
   ⇒ 同样是「缩到 `Vector3.one`」，原版从 0.75 涨 1.33×，我们从 0.36 涨 2.78×，**观感幅度差一倍多**。
   **本批按判据照抄 `Vector3.one`（⛔ 没有按我们的缩放去「换算一个等价量」—— 那会是自设计）**；
   但这说明「在场卡的根缩放用 `desiredScale` 还是 `GetUnitSizeInPlay`」本身可能是一条待审计项，请调度台判。

3. ℹ️ `Batch` 下 `CardFeel.Dissolve` 走 `CardTween.Mode = Manual` 才能推（断言里已设）；
   真机（`Normal`）不受影响。

---

## 七、状态

- 11-10：**已按原版语义改完**（3 处偏离全消）· typecheck 0/0 · 断言已建、**已接线**（§八）。
- 活状态表 / 账主表：**本批未动**（白名单外）⇒ 由调度台核销。

---

## 八、续做记录 `B141-W47b`（2026-11-04 · 收宿主旧断言 + 接线）

白名单扩到 `Editor/BattleScene.cs`（只许 3 处 + 接线行）。四处改动：

| 物理行 | 改前 | 改后 |
|---|---|---|
| `:23118` | `⇒ **退回整卡淡出**（实得 {LastDeathFallback}）` | `⇒ **退回 2D 卡面淡出**（…）` |
| `:23121-23123` | `Check(f1.Alpha < 0.05f, $"…真的淡到 0（实得 α={f1.Alpha:F3}）" + "…保留旧的淡出是对的")` | `Check(f1.Alpha2D < 0.05f, $"…（实得 α2D={f1.Alpha2D:F3}）" + "…淡的是 **2D 卡面**（原版 `fadeGroup` = `Alpha2D`，⛔ 不是整卡 `Alpha`）")` |
| `:23124` | `f1.SetAlpha(1f);                 // 归位，好验「兜底没碰 α」` | `f1.SetAlpha2D(1f);               // 归位（2D 卡面 α）· ✅ 已复刻（2026-11-04 · B141 · 判据 \`CardScript__DissolveWholeCard.c:18-32\`：淡 \`fadeGroup\` = \`Alpha2D\`、无位移、缩到 \`Vector3.one\`）` |
| `:47035`（聚合行） | `…B141_W35_Assert.Fail; Debug.Log(P + "=== 合计…")` | `…B141_W35_Assert.Fail; **B141_W47_Assert.Run(); pass += B141_W47_Assert.Pass; fail += B141_W47_Assert.Fail;** Debug.Log(P + "=== 合计…")` |

### 验证

- `TMPDIR=/tmp/wf_b141_w47 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完宿主 + 接线后跑）。
- **行数变化：0**（`BattleScene.cs` `newlines 51505 → 51505`，`crlf 51505 / lone_cr 0`）。
  接线那一段是**纯 ASCII 83 字节**插进同一物理行（与备份逐字节对比：`size delta = 83`、换行数不变）。
- **接线位置的三道自检**（脚本里写成断言，任一不成立就 abort）：① 锚点（`Debug.Log(P + "=== 合计`）存在；
  ② 锚点在**第一个 `//` 之前**（行注释不会把新代码吃掉 —— CLAUDE.md 记过 6 次这个坑）；
  ③ 锚点前那一段不在 `/* */` 里（判据 = 最后一个 `/*` 早于最后一个 `*/`）；
  ④ 锚点前紧挨着的是 `B141_W35_Assert.Fail;`（不打断别家写手的接线）。
- **灭自证**：把 `:23121` 改回 `f1.Alpha` ⇒ 恒 1 ⇒ 该条**必红**；把 `CardFeel` 的 `SetAlpha2D` 改回 `SetAlpha` ⇒ `Alpha2D` 停 1 ⇒ **必红**（与 `B141_W47_Assert` 的 A4★ 同口径，两边一起红）。

### 未动 / 留给调度台

- ✅ 原「`:23135` 那条 `f1.Alpha` 变弱」已在 **§九** 处理（选 (b) 删除）。
- `FallbackPath.WholeCardAlpha` 仍叫历史名（宿主两处按名断言），未改名。

---

## 九、续做记录 `B141-W47c`（2026-11-04 · 收掉 `:23135` 的恒真断言）

### 选了 **(b) 删除** —— 因为 (a) 也**同样是恒真**，理由三条（已写进代码原处）：

| 断言候选 | 终态 | 有没有判别力 |
|---|---|---|
| 原版 `Mathf.Abs(f1.Alpha - 1f) < 1e-4f` | 恒 1 | ❌ **恒真**：`CardFeel.ReturnToHand`（`CardFeel.cs:1887-1950`）**全文不调** `CardView.SetAlpha`（两支都只碰 `SetAlpha2D` / `SetArtAlpha` / `SetBody3DVisible` / `SetDissolveAmount`）⇒ `_alpha` 从进函数到出函数没被写过一个字节 |
| (a) 改断 `Alpha2D` | 恒 1 | ❌ **也恒真**：`ReturnToHand` 的 `OnKill`（`CardFeel.cs:1948`）**无条件** `SetAlpha2D(1f)`，而该注释自陈「`OnKill` 在**正常播完**时也会跟着触发一次」⇒ 序列一结束就复位，删掉淡回补间也看不出来 |
| 中途采样（`Step` 落在 verter 0.25~0.58 那段） | ≈0→1 | ✅ 唯一有判别力的形态 —— 但要改 `:23134` 的 `Step(1.2f)`（**不在本批「只许改这一处」的授权内**），而且是**插行**，会让本文件 3 万行之后所有账落点整体漂移 |

⇒ 按「⛔ 不许留恒真断言占位」，**删掉**，并把上面三条理由写在原处（`:23135-23137` 三行注释，**行数零变化**）。

### 改法

`:23135-23137` 三行 `Check(...)` → 三行注释：

```
// ✅ 已复刻（2026-11-04 · B141 · 删恒真断言，判据 `CardFeel.cs:1887-1950`）：此处原来那条 `Check(Mathf.Abs(f1.Alpha - 1f) < 1e-4f, …)` **已删** ——
//    兜底支不再碰 `_alpha`（`ReturnToHand` 全文**不调** `CardView.SetAlpha`）⇒ 它以**恒真**、没有判别力；⚠️ 改断 `Alpha2D` **同样恒真**：
//    `ReturnToHand` 的 `OnKill`（`CardFeel.cs:1948`，「正常播完也会触发」）**无条件** `SetAlpha2D(1f)` ⇒ 终态永远是 1。真验「2D 卡面淡回来」要**中途采样**（得改 `:23134` 的 `Step`）。本路径由上一句断「走哪条路」。
```

### 验证

- `TMPDIR=/tmp/wf_b141_w47 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **行数变化：0**（`BattleScene.cs` `newlines 51505 → 51505`，`crlf 51505 / lone_cr 0`）。
- 该路径仍有覆盖：`:23131` 断 `LastReturnFallback == HideBody`（走哪条路）。
- 灭自证：(b) 无「改回必红」可言 —— 正因为**改成哪个值都不会红**，它才必须删。

### ⚠️ 两条留给调度台（本批未动，超授权）

1. 🔴 **`:23133` 的文案有半句已悬空**：它的尾串是「｜🧨 改回 SetAlpha(0) ⇒ 本条红 + **下一半也红**」—— 「下一半」指的就是刚删掉的那条。
   现应改成「本条红」（或另配一条真断言，见下）。**这是另一个 `Check` 的字符串**，按「⛔ 别动其它断言」本批没碰。
2. 🟠 **想把这半边的覆盖补回来，最便宜的替换是断 `f1.Body3DVisible == false`**（`CardView.cs:3155` 有 getter）——
   它正是 `!canDissolve` 支的真实效果（`SetBody3DVisible(false)`，判据 `D3_F1判据与方案.md` §③ C4），且**不恒真**：
   删掉那句 `SetBody3DVisible(false)` 就红。本批没有它是因为它不在 (a)/(b) 两个候选里，等调度台一句话授权。

> ✅ 上面两条 **2026-11-04 `B141-W47d` 已获授权并做完**，见 §十。

---

## 十、续做记录 `B141-W47d`（2026-11-04 · 收悬空文案 + 补一条真断言）

白名单：`Editor/BattleScene.cs`（只这两处）+ 本报告。

| 物理行 | 改前 | 改后 |
|---|---|---|
| `:23133` | `+ "｜🧨 改回 \`SetAlpha(0)\` ⇒ 本条红 + 下一半也红"` | `+ "｜🧨 改回 \`SetAlpha(0)\` ⇒ 本条红（原「下一半也红」指的那条 α 恒真断言已删，见下）"` |
| `:23135-23137`（3 行，等行替换） | 3 行注释（W47c 写的「为什么删」） | `Check(f1.Body3DVisible == false, $"...走完一轮后 **3D 体是关的**…🧨 删掉 SetBody3DVisible(false) ⇒ 本条红");   // ✅ 已复刻（2026-11-04 · B141 · 判据 D3_F1判据与方案.md §③ C4）` + 1 行注释（保留 W47c 的「为什么原来的 α 断言被删」） |

### 这条新断言盯的是什么 / 为什么它不恒真

- 判据：`资料/普查产出_1023/D3_F1判据与方案.md` §③ **C4** —— `ReturnToHand` 的 `!canDissolve` 支必须
  **到点直接关 3D 体**（`SetBody3DVisible(false)`），⛔ 不再淡卡体（不透明档下 `_Color.a` 无效）。
  代码落点：`CardFeel.cs:1928` 的 `InsertCallback(artOn, … card.SetBody3DVisible(false) …)`。
- **灭自证**：删掉 `CardFeel.cs:1928` 里那句 `SetBody3DVisible(false)`（或把 `false` 改成 `true`）⇒ 走完一轮后
  `f1.Body3DVisible == true` ⇒ **本条红**。`StopInPlayAnims`（`:14730-14747`）不碰卡体可见性、`OnKill` 也不碰 ⇒ 没有别处替它兜住。
- ⚠️ **已写进断言的诚实边界**：`CardView.Body3DVisible`（`:3155`）的实现在 `_body3D` 为 null 时**恒 false**
  ⇒ 那种机器上本条无判别力。但本宿主 `:23100` 已硬要求 `dissolvable > 0`（= 有卡体且溶解材质建得出来），
  即「这条路真走得通」时卡体一定在 —— 这条边界写进了断言文案里，⛔ 没有假装它永远有判别力。

### 验证

- `TMPDIR=/tmp/wf_b141_w47 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **行数变化：0**（`BattleScene.cs` `newlines 51505 → 51505`，`crlf 51505 / lone_cr 0`；两处都是**等行替换**）。
