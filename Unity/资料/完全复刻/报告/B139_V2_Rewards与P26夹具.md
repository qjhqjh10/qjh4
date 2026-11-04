# B139_V2 · RewardsScene `C3`(10) + `B110M2F`(1) · ShellScene `P26`(2) 夹具订正

执行代理 `V2` · 2026-11-04 · 根因诊断 = `报告/B139_DIAG_Shell与Rewards.md`

## §0 一句话

**13 条红全部是「夹具前提过期」，产品零改动**（铁律 18·0）。三组的具体形状：

| 组 | 条数 | 过期前提 | 改法 |
|---|---:|---|---|
| `C3` D 段 | 10 | 「卡包那一格在视口内、命中件一定建得出来」 | 夹具**自己把连登轨滚到那一格**，再断「整格在视口内」 |
| `B110M2F` C 段 | 1 | 「第 1 格与第 3 格数量不同」 | **现找一格数量确实不同**的，并把「不同」写成前提断言 |
| `P26` P5-2 | 2 | 「`Select(-1)` 必然越界」 | 夹具**自己造两套卡组**（临时存档）再测越界 |

**⛔ 没有跑 Unity**（本代理无权限）⇒ 「13 条全绿」这一档由调度台跑 `RewardsScene.Run` + `ShellScene.Run` 验收。

---

## §1 `C3`（`Editor/C3_Assert.cs`，10 条）

### `C3` 的根因（一图）

`81ea4fbb` 把卡包格从旧「第 1/4 格」挪到 **第 7 格（index 6）**：

```text
内容坐标： ContentPadL(2) + Σ_{k<6}(EntryAdvance(k) − 64)
        = 2 + 5×455.7792 + 546.93504 − 6×64 = **2443.831**   ← 第 7 格左沿
视口：     S_Scroll = (0,159.33)–(1920,964.94)  ⇒ x ∈ [0,1920]
可滚范围： MaxOffset = TrackContentW(2899.61) − 1920 = **979.61**
```

⇒ 出厂偏移（0）下第 7 格**整块在视口外** ⇒ `MenuDraw.Hit` 的 `ClipRect` 判不建（`Shell/MenuDraw.cs:2235`）
⇒ `PreviewRewardsHitForTest(6)` 回 `null` ⇒ **D2 红，D3–D11 全部级联**（含两条灭自证）。

`DailyStreakPopup.FocusCurrentDay()` 在 `StreakCollected = 5` 时**早退不滚**（`if (day <= 5) return;`）
—— 那是**原版 `if (5 < 当前天)` 照做**的产品行为，⛔ 没动它。

### 改法

新增三个工具（**追加在 `FindLockedClickableDay` 之后**，⛔ 没插进既有成员之间）：

| 成员 | 干什么 |
|---|---|
| `DayLeftInContent(int day)` | 按 **`DailyStreakPopup` 的公开布局常量**（`ContentPadL` / `EntryAdvance` / `EntrySpacing`）算那一格的内容左沿 —— 与 `BuildTrack` 同一套步进，⛔ 不发明第二套布局 |
| `ScrollDayIntoView(ds, day)` | `ds.TrackScroll.SetOffset(内容左沿 − 视口左沿)`（`SetOffset` 自己夹到 `[0, 979.61]`）。**`SetOffset` 会触发 `OnChanged` ⇒ `BuildTrack` 重建 `entries`** ⇒ 调用方必须重新取命中件 |
| `DayFullyInViewport(ds, day, out x1, out x2)` | 把那一格换算到屏幕坐标、判**整格**在视口内（±0.5px 容差），坏掉时能说清是「没滚」还是「滚过头」 |

落点（D 段）：

* `D1` 之后 → `ScrollDayIntoView(ds, pack)`；再取 `hitPack`（**重新取**，不是滚动前那一份）
* `D9` 之前 → 新建的 `ds2` 偏移又从 0 起，所以**同样滚一次**再取 `hitOpen`

### 新增 / 改写的**前提硬断言**（⛔ 不再是 `if (前提) Check(结论)` 那种静默跳过）

| 断言 | 内容 | 为什么要它 |
|---|---|---|
| **D1b**（新） | 滚过去之后**那一格整格在视口内**（按布局常量算：x **1464.22 → 1920.00**，视口 [0,1920]） | 直接量几何，⌈命中件非 null⌋ 只是它的**后果**；这条红了能一眼分出「没滚」 |
| **D2**（改写） | 命中件建出来了 = `ClipRect` 判它在视口内、`MenuDraw.Hit` 真建了 | 原文案「（视口内）」没写清为什么，现在把因果写死在断言里 |
| **D9b**（新） | 灭自证那一份**也**滚到了那一格（整格在视口内） | `ds2` 是另建的一扇窗，偏移独立 —— ⛔ 不能靠 `ds` 滚过就算 |

**判别力没被削弱**：D4–D8 仍然验「闸关 ⇒ 不挂回调 / 关按钮音 / 无 cue / 不开窗 / 无声」，
D9–D11 仍然是灭自证（闸摆开 ⇒ 三样都回来）。滚动的只是夹具的取样位置。

---

## §2 `B110M2F`（`Editor/B110_M2F_Assert.cs`，1 条）

### `B110M2F` 的根因

`81ea4fbb` 把 `DailyData._streakAmount` 由 `{1,150,20,…}` 改成 **`{150,75,150,2,150,300,1}`**
⇒ 第 1 格与第 3 格**都是 150** ⇒ 「灭自证：两格数量不同」前提失效（日志实测 `150 vs 150`）。

### 改法（⛔ 产品数据没改）

C 段重排：

| 断言 | 内容 |
|---|---|
| `C1` | 第 1 格 `Amount` = `StreakRewardSpecsOf(0)[0].Quantity`（不变） |
| `C2` | 第 1 格 `ShowAmount = true` —— ⚠️ **文案就地订正**：原文写「第 1 格（**卡包** ⇒ `Container Drawer`）」已过期，第 1 格现在是**水晶** ⇒ `Currency Drawer` |
| **`C3`**（改写 = 前提） | **现找**一格数量**确实与第 1 格不同**的（本轮 = 第 2 格 75）。表若退化成一串相同值 ⇒ **本条红**，⛔ 不让 C5 静默退化成恒真 |
| `C4` | 那一格 `Amount` = 它自己的 `StreakRewardSpecsOf(other)[0].Quantity` |
| `C5` | 两格数量**确实不同**（`150 vs 75`）⇒ 值真的来自那一格，不是恒写某个固定值 |
| `C6` | 那一格的 `ArtName` = `StreakRewardIcon(other)` |

文件头「灭自证」那一节同步订正（原文写「`E`：连登那边同样的形状（1 / 20）」，段号早已是 C、
数值也早变了）——按铁律 5 留了 `⚠️ 2026-11-04 更正（B139_V2）` 痕迹。

---

## §3 `P26`（`Editor/B104_P26_Assert.cs`，2 条）

### `P26` 的根因

`81ea4fbb` 把存档根迁到 `C:\warpforgesave` ⇒ 新根下卡组库是**空表**（`current:-1, decks:[]`）
⇒ `CollectionData.Select(-1)` 里 `Lib.CurrentIndex(-1) == i(-1)` ⇒ **被读成「选中成功」**
（P52a 红），还往下走 `Lib.Save()` 之后关窗（P52b 红）。

即：**「环境里恰好没有卡组」被当成了越界样本**。

### 改法（⛔ 产品没改）

`CheckP52SelectFailureKeepsWindow` 现在：

1. 临时 `DeckStore.OverridePath = Temp/_p26_select.json` + `DeckStore.DeleteFile()`
2. `DeckLibrary.Load()`（空库）→ `Insert(NewUnsaved(...))` **造两套**（⚠️ 用 `Insert` 而不是已 `[Obsolete]`
   的 `Create`：`Insert` 是「入库的唯一入口」，语义一样、零警告）
3. **从盘上重读**一份当证据（⛔ 不用内存那份自证）
4. `CollectionData.InvalidateLibrary()` ⇒ 让 `CollectionData.Lib` 重读这份临时库
5. 之后才建 `DeckInfoPopup` → 点 `Select Deck` → `CollectionData.Select(-1)` = **真越界**

`finally`：拆窗 + `MessageToast.DisposeForTest()`（失败支会弹红横幅，⛔ 不留给后面的宿主）
+ 删临时档 + 还原 `DeckStore.OverridePath` + **再 `InvalidateLibrary()` 一次**
（`CollectionData._lib` 是 static，不作废会把这两套夹具卡组带进后面每一节）。
外层 `CheckB104P26` 的 `finally` 也补了一次 `InvalidateLibrary()`（双保险）。

### 同文件的同类问题（调度台转达 · 包 V4 顺手发现，本轮一并改掉）

`CheckP22CloseChecksDirty` 与 `CheckP56CardbackSpeaks` 的 `finally` 原来把 `DeckStore.OverridePath`
还原成**字面 `null`** —— 那只是「进来时恰好是 null」的巧合，**会把外层注入的值清掉**（= 自检污染宿主）。
两处都改成「先存 `keepPath = DeckStore.OverridePath`、`finally` 还原它」
（V4 已给写口加批处理沙箱，所以现状不会真写到玩家根，但那两行本身是错的）。
本文件现在三处临时改路径的段落（P2-2 / P5-2 / P5-6）**全是「存进来那份 → 还原那份」**，`grep` 已无字面 `null` 还原点。

### 新增的**前提硬断言**

| 断言 | 内容 |
|---|---|
| **P52 前提 ①**（新） | 临时库里有 ≥ 2 套（⛔ 不碰玩家真卡组） |
| **P52 前提 ②**（新） | `reread.CurrentIndex != -1` ⇒ `Select(-1)` 才是一次**真越界** |
| **P52 前提 ③**（新） | `CollectionData` 看到的就是这份临时库（`DeckCount() >= 2`）—— 挡「缓存还是老库」那条假失败 |
| **P52c**（新，灭自证） | 越界那一下**没有**改掉当前选中项（仍 = 进段时那个下标）｜🧨 若 `Select(-1)` 把 `_current` 写成 −1（= 修前那条「假成功」的形状）⇒ 红 |

`P52a` 的文案补了实测值（`LastSelectError` 原文上屏）。

---

## §4 验证（已做 / 待做）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_v2 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `python 工具/check_ledger_points.py --all-A` | **漂移 0 · 失败 0**（A 条数随别路代理并入而变：本轮两次读数 5967 → 5969，纯属并发写手，与本补丁无关） |
| `python 工具/merge_ledger_parts.py --dry --parts <临时目录>` | 补丁 3 行全部命中既有行：**更新 3 · 新增 0**（⛔ 没有把新行写歪） |
| 行尾 / 裸 CR | 三份 `.cs` 全部 **LF、0 个裸 CR、0 个 CRLF**（没翻行尾）；补丁 `.tsv` 同为 LF |
| `RewardsScene.Run` / `ShellScene.Run` | **未跑**（本代理无权跑 Unity）—— 13 条是否全绿由调度台验收 |

### 账补丁

`账/_parts/B139_V2.tsv`（11 列）：原样复制主表三行、只在 `备注` 末尾追加 2026-11-04 的订正说明
（`merge_ledger_parts.py` 的键是 `(原版类, 方法)` ⇒ **整行替换**，所以补丁里带的是完整行）：

| 域 | 原版类 | 方法 |
|---|---|---|
| 收藏 | `ItemDrawer` | `OnPointerClick(PointerEventData eventData)` |
| 收藏 | `ItemDrawer` | `ItemDrawer<T>.HandlePointerClick()` |
| 卡组编辑 | `DeckInfoControls` | `OnSelect()` |

---

## §5 顺手发现（⛔ 本代理没改 —— 不在白名单）

1. **`Shell/DailyStreakPopup.cs:1030`（`BuildPreviewHit` 的 doc）有过期描述**：
   「连登第 **0/3** 格是卡包 ⇒ false」—— 卡包现在在**第 7 格（index 6）**。只影响注释、不影响行为，
   但下一批谁读那段会被带偏（铁律 5：就地改掉）。
2. **`Editor/RewardsScene.cs` 的同族假设要顺带核一眼**：凡「连登第 N 格 = 卡包 / 命中件一定存在」的取样，
   都是同一个病（`81ea4fbb` 改表没同步改断言）。本轮它没报红说明暂时没踩到，但形状一样。
3. **诊断 §4·1 那条仍然成立且没修**：自检宿主会往玩家真存档根写盘（`DeckStore` 的写口没有 `isBatchMode` 闸）。
   本轮 P26 那一段**已经**改走临时 `OverridePath`（不再写真根），但**别的段/别的宿主**没有
   （诊断实测 `C:\warpforgesave\WarpforgeDecks.json` 被 ShellScene 写过一次）—— 建议单独立账。
