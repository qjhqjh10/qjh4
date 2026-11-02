# B117-WREPIN · ruleprobe 两处指纹重钉报告（收口代理，2026-11-02）

> 任务 = 重钉 `ruleprobe` 的**解析层**（`check`）与**结算层**（`play check`）指纹基线。
> 归因正本 = `判据/B117-WGB_三条落地.md` §一 / §三（`B117-10` = `UM64` 目标格）· 引擎落点 = `RuleEngine/Core/EffectText.cs:10382`。
> ⛔ 未改引擎代码 · ⛔ 未动 git / `d:/2` / 账主表。

---

## 一、重钉前的读数（串行 ≥2 次一致）

### 解析层 `bash 工具/ruleprobe.sh check`（rc=1）

```
=== 全池 1121 张 · 解析差异 1 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
⚠️ 解析差异（前 1 张）：UM64|Angels of Death
✗ [输出指纹] 现算=f937e54dc6a7b79dedc5d6a656e271da · 钉住=ebdac77915a81673bf44114062061ee9
✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）
  卡池 md5=64309acdab6d0d8003c9ae26a31bc694 · 基线 md5=b6b412f52f5e76cde6c005b6a118b0d4
```

- 两次串行读数：`f937e54d…` ×2 · 差异面 `UM64` ×2 · 哨兵失败 1 条 ×2。

### 结算层 `bash 工具/ruleprobe.sh play check`（rc=1）

```
结算指纹：当次现算 = a8b7eac3884b94b0df3e8e6ba6d53577 · 基线文件 = d6c68354baf04bca2f725aa0111817b5（不同）

--- ①-a 逐卡差异（主档 · 打出去那一拍）---
  🔴 1 张卡变了 · 新增 0 · 消失 0
  UM64 （21 列对拍）
    playRc 18→0 · played 0→1 · mech 0→3 · other 0→3 ·
    energyDelta 0→-6 · handCostDelta 0→-6 · changes 0→7
    changesFull →  ⚙ 能量 P0 20 → 14（-6）;✚ 落场/生成 P0[1|2|7] . → UM6 2/2/0/3 {flank1}; …

--- ①-b 逐卡差异（事件电池 · 打出去之后那一段）---
  🔴 1 张卡变了 · 新增 0 · 消失 0
  UM64 （16 列对拍）
    playRc 18→0 · played 0→1 · ctrlPlayed 0→1 ·
    steps play=18 → play=0,selfChip=-,softAtk=-,… · raw 0→43 · ctrl 0→42 · net 0→33
    note 「电池没起步（主档同样打不出去 rc=ErrNoTargetAvailable）⇒ 不是电池的缺口」 → 空

✓ ② 哨兵：基线文件的内容与钉住的一致（d6c68354…）
```

- 两次串行读数：`a8b7eac3…` ×2 · 差异面只有 `UM64` ×2。

---

## 二、差异面逐卡清单

| 层 | 卡 | 旧 → 新 | 归因 |
|---|---|---|---|
| 解析 | `UM64` Angels of Death | op2 目标块 `Count=1 → 0`（**唯一一个字段**） | `B117-10`：`EffectText.cs:10382` 追加 `(!NameFilter 为空 && Side == "own")` ⇒ `spec.Count = 0` |
| 结算 ①-a | `UM64` | `playRc 18→0` · `played 0→1` · `energyDelta 0→-6` … | 同一笔：`PickTarget` 只放行 `Count == 1` ⇒ 不再要求点格；结算走 `Count == 0` 支 = 全部己方原铸仲裁者 |
| 结算 ①-b | `E\|UM64` | `playRc 18→0` · `note` 空 | 同上（电池随主档一起从 `ErrNoTargetAvailable` 转成功） |

- 脚本逐行核旧 / 新基线：解析基线 **恰好 1 张**（`UM64`，1121 行不变）；结算基线 **恰好 2 行**（`UM64` / `E|UM64`，2202 行不变）；**新增 0 / 消失 0**。
- `SAU52` 本来 `Count = 0`，不在差异面 —— 与 `B117-WGB` 的预测一致。

---

## 三、钉了哪几处

| 文件:行 | 常量 | 旧值 | 新值 |
|---|---|---|---|
| `工具/ruleprobe/Program.cs:251` | `PinnedPoolMd5` | `64309acd…` | **不动**（卡池 md5 未变 = `64309acd…`） |
| `工具/ruleprobe/Program.cs:285` | `PinnedBaselineMd5` | `b6b412f52f5e76cde6c005b6a118b0d4` | `3a072df1897e96238b02de11e22acd03` |
| `工具/ruleprobe/Program.cs:350` | `PinnedParseMd5` | `ebdac77915a81673bf44114062061ee9` | `f937e54dc6a7b79dedc5d6a656e271da` |
| `工具/ruleprobe/PlayProbe.cs:4405` | `PinnedPlayMd5` | `d6c68354baf04bca2f725aa0111817b5` | `a8b7eac3884b94b0df3e8e6ba6d53577` |

- 基线文件同步重生成：`基线/out_baseline.txt`（raw md5 `b6b412f5…` → `3a072df1…`）· `基线/out_play_baseline.txt`（raw md5 `c3dfd0ff…` → `b4437d75…`；结算指纹 md5 = `a8b7eac3…`）。
- **三颗一起看（账 `B-267`）**：卡池 md5 未变 ⇒ `[基线指纹]` 这一支要 `PinnedBaselineMd5` 与 `PinnedParseMd5` **同刻**更新，只钉一颗会把第 ⑤ 段打红。
- 改动全部落在**常量行内的文本**上，`Program.cs` / `PlayProbe.cs` **行数不变**（2818 / 4912）。

---

## 四、备份路径

| 备份 | 内容 | raw md5 |
|---|---|---|
| `Unity/工具/ruleprobe/基线/_bak_20261102_pre_repin.txt` | 旧 `out_play_baseline.txt` | `c3dfd0ff482fac3884d672329fd1bf14` |
| `Unity/工具/ruleprobe/基线/_bak_20261102_pre_repin_parse.txt` | 旧 `out_baseline.txt` | `b6b412f52f5e76cde6c005b6a118b0d4` |

---

## 五、重钉后的验证（串行两次，读数一致）

### 解析层

```
✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）
✓ [输出指纹] 当次现算的解析输出与被确认的那一版一致（f937e54d…）
=== 哨兵合计：失败 0 条 ===
=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
rc=0
```

两次：rc=0 ×2 · `f937e54d…` ×2 · 哨兵失败 0 ×2 · 解析差异 0 行 ×2。

### 结算层

```
结算指纹：当次现算 = a8b7eac3884b94b0df3e8e6ba6d53577 · 基线文件 = a8b7eac3884b94b0df3e8e6ba6d53577（一致）
①-a 逐卡差异 = 0 张 · ①-b 逐卡差异 = 0 张
✓ 基线文件的内容与钉住的一致（a8b7eac3…） —— 重生成基线抹不掉这一条
rc=0
```

两次：rc=0 ×2 · `a8b7eac3…` ×2 · 哨兵 ✓ ×2。

---

## 六、纪律核对

- **无写手在飞**：`RuleEngine/**` ＋ `工具/ruleprobe/**` 近 30 分钟 **0 个 `.cs` 变更**；唯一 `dotnet.exe` = Unity 的 Roslyn `VBCSCompiler` 常驻进程（非构建）。
- **串行**：所有 `check` / `play check` / `scan` / `play all pf` 一条一条跑，无并行 `dotnet build`（同刻两个会互相覆盖 `bin`）。
- **逐卡差异与归因清单对得上**：只允许且实际只有 `UM64`（解析 1 张；结算主档 1 张 + 电池 1 张）。
- **未越界**：未改 `RuleEngine/**` · 未动 git · 未动 `d:/2` · 未动账主表。
- **行尾**：`Program.cs` / `PlayProbe.cs` / 两个基线均纯 LF、裸 CR = 0。

---

## 七、顺手发现

- `PlayProbe.cs:4405` 上一条重钉注记写的是 **`2026-11-03`**，而今天为 **2026-11-02**；该笔实际发生在本次之前（钉的值 `d6c68354…` 与我读到的旧值一致）。属**历史注记的日期写法**，本轮未改动它。
