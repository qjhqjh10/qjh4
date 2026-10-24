# E10 · `AnimFXCheck` ⑤e 重建（注入驱动）

> 只改了白名单里的一个文件：`Unity/MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs`。
> ⛔ 未跑 Unity · 未动 git · 未改正本 · 未动 `voice_lines.json`（只读扫了一遍）。
> ✅ 已跑秒级类型检查：`TMPDIR=/tmp/wf_e10 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 〇、一句话结论

⑤e 原来的自然夹具（`BL35` 的 `deploy` 4 行 `vs_*`）被 **`D7` 合法清空**（改记 `ev=intro-vs` + `src=orphan`），
所以旧断言「前半恒真、后半恒假」。本轮**规则一字未改**，只把夹具换成 `VoiceLines.SetRowsForTest` 注入的合成行：
① 2 条无 SO ⇒ 24 个种子必须 ≥2 种取值；② 1 条 SO + 1 条猜 ⇒ 24 个种子都取 SO 那条；③ 还原后取不到合成行。
**仍是 1 条 `Check`**（不增删条数），预期宿主 **151/1 → 152/0**。

---

## 一、为什么自然夹具没了（现读证据）

`voice_lines.json` 现读（`python -I`，只读）：

| 事实 | 现读值 |
|---|---|
| 卡片数 / 台词表 | 642 张 |
| **全表「同卡同 ev 多行」组** | **4 组**，**全部是 `intro-vs`**：`ASH3` 3 行 · `BL35` 4 行 · `BL36` 7 行 · `SAU36` 4 行 |
| 上述 4 组的 SO 行数 | **均 0** |
| **`BL35` 的 `deploy` 行数** | **0**（全表**已无任何 `deploy` 多行组**） |

原因（判据 → `资料/普查产出_1024/D7_语音脏数据订正.md` §一/§二）：
`D7` 把那 19 条 `_vs_` 对手台词从 `ev=deploy` 改记成 **`ev=intro-vs`** 并标 **`src=orphan`**
（`deploy` 606 → 587）。这是**合法的脏数据订正**，不是退化。

⇒ 旧 ⑤e 断言 `SoRowCount("BL35","deploy")==0 && DistinctPicks("BL35","deploy")>=2`：
- 前半 **恒真**（0 行）；
- 后半 **恒假** —— 0 行时 `TryPick` 24 个种子全返回 false ⇒ `DistinctPicks` 只会得到 `{(miss)}` = **1 种**。

⛔ **不是「为了绿而改」**：⑤e 守的规则完全没变 —— *同 ev 多行、且无一行 SO 指名 ⇒ 仍须保留随机*。
变的只是夹具来源（与 ⑤d 同款注入，成对 / 幂等 / 可还原）。

> 为什么不用现存的 4 组 `intro-vs` 当自然夹具：那是**对手台词归档桶**，按 `D7` §二 走 `vs~` 虚拟 ev 通路
> （现结构下永不命中），既不适合当 `deploy` 的随机池夹具，也随时可能再被数据订正动到 ⇒ 用注入更稳。

---

## 二、注入夹具怎么搭

用 `Core/VoiceLines.cs` 里 E3 已经为 ⑤d 加好的注入口（**未改动** `VoiceLines.cs`）：

```csharp
public static RowsInjectionScopeForTest SetRowsForTest(
    string cardId, string ev, string soFile, params string[] guessedFiles)
```

它造出的行：`soFile` 非空 ⇒ 1 条 `src="cardSO"`；`guessedFiles` 每条 ⇒ 1 条 `src="orphan"`。
`Dispose` 时**成对还原**（原来没有 ⇒ 删掉），`WasInjected` 可断言。

| 夹具 | 调用 | 造出的行 |
|---|---|---|
| ① `ZZ_E10_NO_SO_RANDOM` | `SetRowsForTest(id, "deploy", null, guessA, guessB)` | **2 行 / 0 SO**（两条都是 `orphan`） |
| ② `ZZ_E10_SO_PRIORITY_DEPLOY` | `SetRowsForTest(id, "deploy", soClip, guessC)` | **2 行 / 1 SO**（`cardSO` + `orphan`） |

两个合成 id 都用 `ZZ_E10_` 前缀（与 ⑤d 的 `ZZ_E3_SO_PRIORITY` 不撞）。
两次注入各自独立 `using` 作用域，出作用域即还原；再在作用域外断「两个 id 都取不到、残留 0 行」。

`EnsureLoaded()` 是 `_tried` 一次性（`VoiceLines.cs:113-116`）⇒ 注入期间不会被重新载入冲掉。

---

## 三、断言 + 灭自证

本轮把 ①②③ **合并进同一条 `Check`**（保持条数不变，失败时消息把每个读数都打出来）：

```
Check(randInj && soInj
      && randRows == 2 && randSo == 0 && distinct >= 2        // ① 2 条无 SO ⇒ 24 种子 ≥2 种
      && soRows == 2 && soSo == 1 && allSo                    // ② 1 SO + 1 猜 ⇒ 24 种子都取 SO
      && !back1 && !back2 && backRows == 0,                   // ③ 还原后取不到合成行
      "⑤e …① … ② … 还原后取不到[…] 残留行数 …");
```

| 子断言 | 期望（独立于被测实现） | 🧨 灭自证（怎么弄红） |
|---|---|---|
| 注① 形状 | `行/SO = 2/0`（`SetRowsForTest` 的契约） | 注入失败 / 只进 1 行 ⇒ 红 |
| ① 随机池 | 24 个种子（`System.Random(0..23)`）出现 **≥2 种**取值 | **把 `TryPick` 改成「永远取第一条」** ⇒ 只有 1 种 ⇒ 红 |
| 注② 形状 | `行/SO = 2/1` | 同上 |
| ② SO 优先 | 24 个种子**都取** `VO_E10_so_deploy.ogg` | **删掉 `TryPick` 的 `soOnly`（SO 优先）、回纯随机** ⇒ 24 个种子里几乎必然抽到猜的那条 ⇒ 红 |
| ③ 还原 | 两个合成 id `TryPick == false` 且 `LineRowCount == 0` | 把注入做成「永久写进表」/ `Dispose` 不还原 ⇒ 红 |

**随机池确实出现过两种取值**（独立验过，不是靠猜）：PowerShell/.NET 的 seeded `Random(0..23).Next(0,2)`
⇒ `distinct=2`，`keys=1,0`。25 个种子以上的偏差概率是 2⁻²³，正常不可能。

> ② 这条闸与 ⑤d 同源（⑤d 用 `ev=gen1` 验同一规则）；这里在 `deploy` 上再钉一次，**⑤d 一个字没动**。

---

## 四、预期条数

- 宿主：`AnimFXCheck.Run` —— **本件不增删断言条数**（旧的 1 条 `Check` 换成新的 1 条）。
- 预期：**151 通过 / 1 失败 → 152 通过 / 0 失败**（`ANIMFX === 合计：152 通过 / 0 失败 ===`）。
- ⚠️ 本轮**没有跑 Unity**（红线），所以这是**预期**、不是实测。

---

## 五、改动清单

| 项 | 内容 |
|---|---|
| ⑤e 主体 | 8 行（自然夹具 1 条 `Check`）→ 58 行（注入驱动 1 条 `Check` + 注释） |
| ⑤d 注释就地订正 | 原写「全表只剩 4 组 (卡, ev) 多行（`ASH3/BL35/BL36/SAU36` 的 `deploy`）」→ 改成 **`intro-vs`；`D7` 之前记作 `deploy`**（铁律 5：记录与事实不符，就地改掉；**未动 ⑤d 的断言**） |
| `VoiceLines.cs` | **未改**（只调用已有的 `SetRowsForTest`） |
| 行尾 | `AnimFXCheck.cs` **纯 CRLF 保持**：改前 1638/1638，改后 **1692/1692**（`\r\n` 数 == `\n` 数，无裸 LF） |
| 备份 | `C:/Users/qjh36/AppData/Local/Temp/e10_AnimFXCheck.cs.bak` |

`git diff --numstat`（该文件，含本轮之前就在工作区的 B4/E3 改动）：`261 / 7`。
**只属于本轮**的增量 = 与备份逐行 diff 的 **84 行 delta**（1 行注释订正 + ⑤e 替换），已单独核过。

---

## 六、没查清 / 没做

1. **没跑 `AnimFXCheck.Run`**（红线）。`152/0` 是静态推算 + 类型检查通过，同步点请实跑确认。
2. **`⑤e` 的随机池期望依赖 .NET `System.Random` 的种子序列** —— 已用 PowerShell（.NET Framework 的 legacy 实现）
   独立验出 `Random(0..23).Next(0,2)` 有 2 种取值；.NET 8 的 **seeded** `Random` 走 `Net5CompatSeedImpl`
   （与旧实现序列兼容）。若万一宿主跑出 1 种，先看这个前提，再看 `TryPick` 是否被动过。
3. **合并成 1 条 `Check`** 是按「本件不增删条数」做的取舍：失败时要读消息里的 `①/②/还原` 三段读数定位。
   如果调度台更想要 3 条独立断言（总数变 154），可拆 —— 但那样与「152/0」的预期对不上，故本轮没拆。
4. **只改了 ⑤e**；`⑤c`（`ASH2/gen1`、`UM88/gen3` 各 1 行）等其它语音断言未动、也复核了仍与现数据一致。
