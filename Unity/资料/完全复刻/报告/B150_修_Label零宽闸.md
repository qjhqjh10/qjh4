# B150 · 修 `Label` 的「零尺寸被当成量到」生产空洞（执行写手 `B150-LABELZERO`）

> 2026-11-05 · 执行代理产出。改动**只有两个文件**：`Battle/Label.cs`（生产闸）＋ `Editor/B140_WC_Assert.cs`（断言）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `账/` · `活状态表.md` · `CLAUDE.md` / `项目任务.md` · ⛔ 未改任何 `Editor/*Scene.cs`。
> **所有「改前」都是动手前现读**；行号若已漂移，以本文件「改前」列的内容为准。

---

## 〇 · 结论一句话

`Label.RefreshBounds` 的守卫**只挡哨兵 / NaN / 超量纲，不挡 `(0,0)`**；而 TMP 未 `Awake` 时 `textBounds` 返回的是
`new Bounds()`（全零，**不是**天文哨兵）⇒ 全零被当成「量到了」，`_tmpW` 又被 `Mathf.Max(|0|, 1e-4)` 钳成 `1e-4`、
`_measured = true` ⇒ `HasMeasuredWidth` 放行 ⇒ 两个 `Align*On` 拿 `1e-4` 去摆位、**且不记 `_pendAlignX` 待办**（静默错位）。
本件在同一行补一条「**两轴都≈0 ⇒ 不算量到**」，零行数变化；并加一条断言（C4）咬住它。

---

## 一 · 逐条改动

| 文件:行 | 改前 | 改后 | 判据出处 | 是否增删行 |
|---|---|---|---|---|
| `Battle/Label.cs:1663`（守卫本体） | 只有两条：`UsableBounds(b.size)` 与 `UsableBounds(b.min)`（有限 + ≤ 量纲上限；**全零能过**） | 同一行追加第三条：`(Mathf.Abs(b.size.x) < 1e-3f && Mathf.Abs(b.size.y) < 1e-3f)` ⇒ 早退（不写 `_tmpW/_tmpH`、不置 `_measured`、不挪节点） | `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:1354-1363`（`textBounds` 在 `m_textInfo == null` 时 `return new Bounds()`）＋ `Runtime/TMP/TextMeshPro.cs:592`（`m_textInfo` 只在 `Awake()` 里 `new`）＋ 本文件 `:1665-1667`（钳 `1e-4` 并置 `_measured`）· `:1989`（`HasMeasuredWidth` 读 `_measured`） | 否（1 行改 1 行，净 0） |
| `Battle/Label.cs:1662`（判据注释） | 「等真测量到了……再摆精确位。」 | 同一行尾追加本档的判据 / 后果 / 边界（⛔ 不写进 `UsableBounds` 的理由、运行时零影响） | 同上 | 否（就地改写，净 0） |
| `Editor/B140_WC_Assert.cs:72`（`Run()` 里） | `LabelConvergence();` | `LabelConvergence(); LabelZeroBoundsSize();` | 同族断言文件（B 组本来就在测 `Label` 的「量到 / 没量到」） | 否（同一物理行追加，净 0） |
| `Editor/B140_WC_Assert.cs:280-329`（类尾新增 C 组） | — | `LabelZeroBoundsSize()`：C0/C1/C2 前提 → C3 断 `textBounds` 两轴都≈0 → **C4 断 `AlignLeftOn(300)` 不摆位** | 同第一条 | **+50 行（全部加在类尾，下面没有别的东西）** |

改动后的守卫（`Battle/Label.cs:1663`，单行）：

```csharp
if (!UsableBounds(b.size) || !UsableBounds(b.min) || (Mathf.Abs(b.size.x) < 1e-3f && Mathf.Abs(b.size.y) < 1e-3f)) return;
```

**净行数核对**：`git diff --numstat` = `Label.cs 2 / 2`（两行改写，净 0）· `B140_WC_Assert.cs 51 / 1`（1 行改写 + 50 行类尾新增）。
两文件**本来就是 LF**（`HEAD` 版 `B140_WC_Assert.cs` 也是 LF 279 行），改动后仍是 LF、**裸 CR = 0**（二进制核过）。

---

## 二 · 「已有闸」核查（结论：真有洞，不是重复加）

三处都查了，**上游没有挡住它**：

1. **`UsableBounds`（`:1634-1641`）本身没有零值闸** —— 它只判「有限 + ≤ `MeasureBoundLimit`」，`0` 天然过。
2. **`PushLayoutInactive`（`:734`）与 `ApplyPendingWrapConverged`（`:2852`）里确实已有 `Mathf.Abs(b.size.x) > 1e-3f`**，
   但那是**收敛循环自己的「排出来没有」判据**，不是 `RefreshBounds` 的入口闸；而且：
   - `EnsureMeasured`（`:687-696`）**把 `PushLayoutInactive()` 的返回值丢掉了**（`:695` 单独一句），`:696` 照调 `RefreshBounds()` ⇒ 全零照样进；
   - `SetTextTmp`（`:1600-1608`）那条主路**压根不经过**那两个循环，直接 `ForceMeshUpdate()` + `RefreshBounds()`。
   ⇒ 洞在 `RefreshBounds` 上，**只有在这里补才封住**。
3. 另外两条「现成的零判据」是 `> 1e-3f`（**只挡 x**），本件按任务口径改成「**两轴都** < 1e-3」—— 比原来那两条**更保守**
   （|x| 很小但 y 正常的那一档，原来会被那两个循环拒掉，本件不拒）⇒ **不影响它们各自的语义**（它们本来就只服务自适应收敛）。

**为什么没写进 `UsableBounds`（任务书原话是「在 UsableBounds 增加一条」）—— 反证**：
`UsableBounds` 是 `size` 与 `b.min` **共用**的（三处调用全是 `UsableBounds(b.size) && UsableBounds(b.min)`）。
把零闸写进去，`UsableBounds(b.min)` 也会在 **`min` 落在原点附近**时返回 false ——
而合法文本的 `bounds.min` 落在原点附近**是常态**（左对齐 ⇒ `min.x = 0`；底对齐 ⇒ `min.y ≈ 0`）⇒
会**误杀正常测量**（后果：`RefreshBounds` 静默不再摆位、`PushLayoutInactive` 白跑 32 遍并记「不收敛」）。
⇒ 零闸只对 **size** 生效，写在 `RefreshBounds` 那条 `if` 里（`min` 那半边一个字没动）。

---

## 三 · 断言（加了，加在 `Editor/B140_WC_Assert.cs`）

**为什么选这个文件**：`B140_WC_Assert.cs` 的 B 组本来就在测 `Label` 的「量到 / 没量到」（B2/B4/B5/B8/B9），是同族；
且它的 mtime 是 **11-04 13:36**（不是本波写手在写的文件）。**没有**加进 `Editor/B148_W3_Assert.cs`（D 组虽然是同族，
但 mtime **11-05 22:40**、刚被 `B150-W-E` 改过，归属拿不准）。它已接线（`Editor/BattleScene.cs:47042` 聚合行，非注释片段）。

新组 `C 标签 · TMP 未 awake（textBounds = 全零）不算「量到」`：

| 条 | 断的是什么 |
|---|---|
| C0/C1/C2 | 前提：TMP 字体可用 · 夹具建得出来 · TMP 子件 `text` 在（前提**写成断言**，⛔ 不用 `if (前提) Check` 吃掉） |
| C3 ★ | 前提：未给 TMP `Awake` ⇒ `textBounds.size` **两轴都≈0**（证明这一档真的是「全零」而不是哨兵） |
| C4 ★ | **被测**：`lb.AlignLeftOn(300f)` 之后 `localPosition.x` **不动**（全零不算量到 ⇒ 不摆位、只欠账） |

**🧨 灭自证**：把 `Label.cs:1663` 那条「两轴都≈0」删掉 ⇒ `_measured = true` + `_tmpW = 1e-4` ⇒ `HasMeasuredWidth` 放行 ⇒
`PlaceLeftNow(300)` ⇒ `localPosition.x ≈ 300.00005` ⇒ **C4 红**（改动前的那条 `300.0001` 实测就是这个来源）。
**会不会过宽（误杀正常测量）**：由同文件的 **B5**（收敛化之后 `WorldW × WorldH` 是真尺寸）兜着 —— 闸过宽 ⇒ B5 一起红。

⚠️ **一条没覆盖**：C4 断的是「不摆位」，**没有**再断「`_pendAlignX` 记的是 300」（那是私有字段，要反射）——
`RefreshBounds` 成功路径的兑现由同文件 B 组 + `B148_W3_Assert` 的 D 组覆盖，本件不重复。

---

## 四 · typecheck

```text
TMPDIR=/tmp/wf_b150label bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

一次过，**没有**出现「集中在别人在飞的文件」上的假错。

---

## 五 · 这个闸会不会影响正常运行期（任务书点名要回答）

**不会，应当是零影响。** 三条理由：

1. **运行时 TMP 一定已 `Awake`** ⇒ `m_textInfo != null` ⇒ `textBounds` 走 `GetTextBounds()`（真顶点）⇒ 只要**有可见字符**，
   `size.x` 至少是一个字宽、`size.y` 至少是一行高，**不可能两轴都 < 1e-3**。
   （世界档 `1e-3` 世界单位 = **0.108 px**；uGUI 档 `1e-3` 设计 px。两轴都低于这个数 = 这一颗本来就没有可画的东西。）
2. **空文本 / 全是不可见字符**这一档**不是**本闸管的：它给的是 `characterCount == 0` ⇒ `GetTextBounds` 走空集哨兵（±2.1e9），
   早就被原来的 `MeasureBoundLimit` 挡掉；`HasMeasuredWidth` 另有 `string.IsNullOrEmpty(_text)` 那一闸。
3. **本闸只影响 `_measured` 与「要不要走这次摆位」**：`_tmpW/_tmpH` 的 `1e-4` 钳位、`_vOffset`、margin 位移、
   `WorldW/WorldH` 的公式、`Contains` 命中区**一个字都没改**。正常测量那一档走的是**原来的成功路径**，逐位相同。

**唯一会命中它的运行期场景**：TMP 组件在运行期被销毁/未初始化（`m_textInfo` 恒 null）。
那种情况下旧行为是「拿 1e-4 摆位且标记为已测量」（错，而且会让 `Align*On` 静默错位），新行为是「不动、等真测量」——
与 `A1751` 那条「量不出来时一个字都不动」的既有口径**一致**。

---

## 六 · 顺手发现 / 没做

- **`Label.cs:734` / `:2852` 那两条 `Mathf.Abs(b.size.x) > 1e-3f` 是「只挡 x」的旧口径**：竖排 / 极窄的标签理论上会被它们判成「没量到」。
  本件**没动它们**（改了会变它们各自的收敛语义，超出本任务白名单）。⇒ 若要统一成「两轴都≈0」，另开一条。
- **`UsableBounds` 的 doc（`:1646-1647`）说「两个消费者必须同值」** —— 本件没有新增第三个消费者，那句话仍然成立。
- 本件**没有**改 `_measured` 的 doc；它写的「只在 `RefreshBounds` 接受一次真测量之后置 true」在加了本闸之后**更准确了**（不动）。
