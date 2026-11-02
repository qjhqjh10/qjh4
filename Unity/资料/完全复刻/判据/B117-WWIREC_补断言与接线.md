# B117-WWIREC —— `R18①` 4 数格式订正 + 两个断言类接线

> 执行代理 `B117-WWIREC` · 2026-11-02。白名单 = `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> + 本报告。零 Unity 批处理 · 未动 git · 未动 `d:/2`。
> 输入判据 = `判据/B117-WMINIONCURVE_佣兵移动曲线.md` §六（现成补丁）。

## 一、改了什么（三处，全在 `Editor/BattleScene.cs`）

| # | 落点（改后行号） | 改动 |
|---|---|---|
| 1 | `:1973-2017` | `R18①` 按 **4 数格式** `[-1,t,v,in,out, …]` 重写（判据 §六 的补丁 + 一条新增的灭自证） |
| 2 | `:46931`（聚合行，**行内追加**） | 接 `B117WMinionCurveAssert.Run()` + `B117_WCOOL3_Assert.Run()`（含各自 `pass/fail` 累加） |
| 3 | `:40473` / `:40720` | 顺手修：两处「关键帧数」显示仍按旧 2 数格式写 `Length / 2` ⇒ 改成**格式感知**（见 §四） |

### 1️⃣ `R18①` 的四段

1. **前提条** `:1973-1981`：`HasSlopes(rotCurve) && rotCurve.Length == 13`（原来写死 `== 6`）。
   `13 = 1 + 4×3` ⇒ **仍钉着「3 个关键帧」这个语义**（不是放宽成 `>= 6`）；消息里印出「曲线格数 13（= 1 + 4×键数 ⇒ 3 键）」。
2. **键 + 切线** `:1982-1994`：键值取 `c[1],c[2] / c[5],c[6] / c[9],c[10]`，**另加**三处切线
   `c[3],c[4] / c[7],c[8] / c[11],c[12]` = **−4.0116 / −1.4681 / 2.9387**（1e-3 容差）——
   这就是「切线形状」那半，原补丁只打印不判，这里**判了**。
3. **`AnimationCurve` 构造** `:2002-2004`：`new Keyframe(t, v, inSlope, outSlope)` ×3（原来 `new Keyframe(t,v)`）。
   ②/③ 那两段（`kMid` / `xMid` / `xEnd`）**一字未动** —— 断言点恰好落在键上，带不带切线求值相同。
4. **灭自证** `:2005-2017`（新增，见 §二）。

### 2️⃣ 聚合行接线

同一物理行、**行内追加**：

```csharp
fail += B117_WSOUNDCOOL_Assert.Fail; B117WMinionCurveAssert.Run(); pass += B117WMinionCurveAssert.Pass; fail += B117WMinionCurveAssert.Fail; B117_WCOOL3_Assert.Run(); pass += B117_WCOOL3_Assert.Pass; fail += B117_WCOOL3_Assert.Fail; /* 🆕 2026-11-02（B117-WWIREA 接线）：…
```

⚠️ 追加前按铁律核过**三个边界**（本项目一天踩过 6 次「代码落进注释」）：
该行 `/*` 有**两对** —— `:2604-2754` 与 `:7799-8254`（字符位），`//` 在 **8311**；
插入点在字符 **7603 / 7707** ⇒ 落在 `2754` 与 `7799` 之间的**纯代码区**，既不在块注释里也不在 `//` 之后。
机检印证：`check_aggregate_lines.py` 报「74 个调用全在 `//` 之前」。
（该工具的默认模式 `\w+_Assert\.Run\(\)` 数不到 `B117WMinionCurveAssert`（少一个下划线），
所以 74 里含 `B117_WCOOL3_Assert` 但不含前者；前者按字符位 7603 < 7799 人工核过。）

## 二、灭自证怎么写的

**问题**：`R18①` 的键值断言**带不带切线都会过** —— 在键上求值必等于键值。若生成器把 `inSlope/outSlope`
抹成 0（或这里退回旧的 `new Keyframe(t, v)` 零切线），`HasSlopes` 仍真、`Length` 仍 13、键值仍对
⇒ 断言**全绿**，而「中段那一拧」已经没了（实测 `t=0.5` 处差 **0.34**）。

**做法**（`:2005-2017`）：同一组键再造一条**零切线**曲线（`new Keyframe(t,v)`，切线与旧实现同义），
在**非键点** `t = 0.5` 上同时钉两件事：

```csharp
Check(Mathf.Abs(kHalfR18 - (-0.7569f)) < 1e-3f
      && Mathf.Abs(kHalfR18 - kHalfZeroR18) > 0.3f, ...);
```

- 带切线 **−0.756863** / 零切线 **−0.416799** / 差 **0.3401**（按 `mmRxC` 逐值算，与
  `B117_WMINIONCURVE` §五 `M3b` 同一个判别式）。
- 🧨 丢斜率 / 退回零切线 ⇒ `t=0.5` 掉到 ≈ −0.4168 ⇒ **红**；反过来，若有人把这条改回硬编码常数，
  `|带 − 零| > 0.3` 那一半仍要求两条曲线**真的不同** ⇒ 改不动。
- 这是**结构上不可能「两边一起改回」**的判别：键值断言与它用的是同一条 `rotCurve`，但**取值点不同**
  （键上 vs 非键点），改任何一处都只会有一边变绿。

## 三、顺手发现（已当场修，铁律 14 ②③ 全中）

4 数格式落地后，**两处「关键帧数」的显示**会算错（`Length / 2` 的旧约定）：

| 行 | 原文 | 现读 |
|---|---|---|
| `:40473` | `mover.ease.Length / 2 个关键帧` | `(HasSlopes ? (len-1)/4 : len/2)` |
| `:40720` | `plG.Value.Ofy.Length / 2 个关键帧` | 同上 |

实测 `Atk_GrotGrenade.ofy` 现在是 **13** ⇒ 旧式会印「6 个关键帧」（真值 **3**）。
⚠️ 这两处**只是消息**，不影响断言结论（断言走 `CardAnimOrder.EaseAt/EaseAtRaw`，它们本来就格式感知）；
改成格式感知后，两种格式都印对。

## 四、验证读数

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wwirec bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `python -I -X utf8 工具/check_aggregate_lines.py <BattleScene.cs>` | **违规 0**（聚合行 1 · 调用 74 · PASS） |
| 行尾 | CRLF **51322** / LF **51322** / 裸 CR **0**（整篇仍全 CRLF，没被翻） |
| 行数变化 | **+21 行**（`git diff --numstat` 里含**别人的**未提交改动，净增按 `b.count(b'\r\n')` 现读：51301 → 51322） |

**账漂移提示**：本笔在 `:1974` 一带净增 **21 行** ⇒ `Editor/BattleScene.cs` 中 **行号 ≥ 1997** 的
其它引用整体 **+21**。现跑 `check_ledger_points.py`，该文件 **失败 0 / 漂移 0**
（它的 A 落点首引用都在 `BattleDriver.cs`，本文件是「第二处」引用；按 2026-10-31 起 `check_extra_refs`
那一类**只报 SECOND 告警**）。⇒ 调度台在同步点**跑一次 `--all-A --fix`** 即可把 SECOND 引用重钉，
⛔ 不必为此回退本笔。

## 五、边界与没做的

- **没跑 Unity**（红线）：`BattleScene.Run` 的实跑留给调度台；`R18①` 现读数据由
  `Resources/cardanim_clips.json` 逐值核过（`SporeMine_Explosion.mmRxC` 长度 **13**、首格 **−1**、
  三键值与切线如上），所以「不红」是按数据+代码逐句推的，**不是实跑结论**。
- 未改任何断言类文件本身 · 未动 `BattleDriver.cs` / `CardView.cs` · 未动 git / `d:/2`。
- `R18②/③`（`kMid → LerpUnclamped → −37.5°` 那两条）**一字未动** —— 它们本来就在键上求值。
