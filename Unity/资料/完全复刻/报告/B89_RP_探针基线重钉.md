# B89 收尾 · 探针基线重钉（`RP` · 第二轮：修回归 → 重钉）

- 代理：`RP` · 2026-10-28
- 红线遵守：⛔ 未改 `账/*.tsv` 主表 · ⛔ 未动 git · ⛔ 未跑 Unity（只跑 `ruleprobe`）· ⛔ 未动 `d:/2`
- 改动面（`git diff --stat` 现读）：`EffectResolver.cs` **2 行**（本笔唯一引擎改动，行数不变）·
  `Program.cs` **1 行**（`PinnedPoolMd5`）· `PlayProbe.cs` **1 行**（`PinnedPlayMd5`）· `基线/out_play_baseline.txt` **26 行**

---

## §0 一句话结论

**9 张 UM 的 Codex 回归已修好并逐字节回到旧基线；四个指纹按「实需」重钉了两颗（池 + 结算），`check` / `play check` 均 rc=0（连跑两次同值）。
唯一未达成：`play all` rc=**1** —— 它只来自 ⑧ 自检的 **Mob** 那一格，而那一格**在我接手前就是红的**。

> 🔴 **2026-10-28 就地订正（铁律 5，第二轮跑完才发现）**：本报告初版把「13 张 Mob 卡电池净差塌成 0」判成**引擎缺陷**（`atkType` 按 `actor.CurrentAttackType` 现推）。
> **那条判据不成立**：`RuleCore.cs:6984` **本波（`B89-E26`）已经显式传** `attackType: ranged ? 2 : 1`（现读，`git diff` 里是 `+` 行），
> `EffectResolver.cs:9896` 的现推只剩 `attackType == 0` 的兜底 ⇒ **近战/远程的 kw 是对的**。
> 13 张 Mob 卡净差变 0 是**正确读数**：它们自己不再跑自己的 Mob（本波照原版删掉的那一支），而夹具里**没有第二个同方 Mob 单位**可扫。
> ⇒ **不需要再改 `RuleCore`**，也⛔ 不加第二处显式传参（两处写同一条规则 = 迟早不一致）。详见 §5·1。

---

## §1 修回归（①）：只改 `EffectResolver` 一侧

**回源核判据（不是信转述）**

- 原版 `AbilityLogic__GetTargets.c:198-207`（现读原文）：
  `if ((iVar6 == 0x1e) || (iVar6 == 0xdc)) { if (op_Inequality(param_5,0)) { list.Add(param_5); } goto 出口; }`
  ⇒ **只有 `ta == 30 (0x1e)` 与 `220 (0xdc)` 两支会读 `param_5`（= 调用方传的 targetCard）**，其余每一档忽略它。
- 我们 `EffectResolver.cs:1644-1647` 早就有这道闸（`op == null || ta < 0 || ta == 30 || ta == 220`），
  但**同一件事的第二份写法** —— 池解析末尾 `EffectResolver.cs:1930-1934`「点定目标 ⇒ 清表只留 `chosen`」**没有闸**。
- 本波 `RuleCore.TriggerPlayerCodex`（`RuleCore.cs:13258`）按原版 `TriggerCodex.c:18-30` 新传 `chosen: u`
  （= `targetCard ?? this`）⇒ 撞上 `:1930` ⇒ 任何 `Count == 1` 的目标被换成**施放者本人**。

**裁定：`chosen: u` 是对的（照原版），错在 `EffectResolver` 缺闸 ⇒ 只改 `EffectResolver`，`RuleCore` 一个字节没动。**

改动（代码行数不变，`:1929` 注释 + `:1930` 条件各就地改写）：

```csharp
if (chosen != null && (op == null || op.SubjectTargetsAffected < 0
    || op.SubjectTargetsAffected == 30 || op.SubjectTargetsAffected == 220)
    && spec.Count == 1 && list.Count > 0 && !list.Contains(chosen))
```

**验证（逐格，不是「看着绿」）**

- 9 张 UM（`UM16 UM21 UM24 UM26 UM55 UM56 UM59 UM68 UM72`）的电池行 **与旧基线逐字节相同**（脚本逐行比，`UM restored exactly: True`）。
- 修前 → 修后（同一台机、同一份卡池）：
  · `UM16` 自己吃 `blind1`（远程 2→0） → 恢复「敌 `FIX_E2` 吃 blind」
  · `UM26` 盾挂自己 → 恢复「`WARLORD {shield1}`」
  · `UM55` 落场血 3→1（自伤 2） → 恢复「敌 `FIX_E2` 6→4」
  · `UM68` 自己拿 `armour1` → 恢复「`FIX_B`（载具）3/0/0/3 → 3/0/0/5」
  · `UM24/UM56/UM59/UM72`：落场血量不再预先扣掉自己那份伤害（6→3 / 4→3 / 7→4 / 8→5 那几笔消失）
- **顺带修好第 10 处同类**：`AM49 Summary Execution`（`targeted=1`，`ta=[30,10]`）第二句
  「destroy a **random enemy** troop」(ta=10) 原来被玩家点定的**友军**劫持 ⇒ 现在真的打死一个敌人
  （`☠ 离场 P1[3] FIX_E1` 出现）。这是**同一个缺闸**，不是新口径。

---

## §2 四个指纹（已重钉，逐条给依据）

| 常量 | 位置 | 旧值 | 新值 | 依据 |
|---|---|---|---|---|
| `PinnedPoolMd5` | `Program.cs:251` | `7a79ef8d097573532b6716e7a6efedf4` | **`519c262985964ab3ff570588992ec336`** | 卡表 v36→**v37**（+6 列），`md5sum` 现读 |
| `PinnedBaselineMd5` | `Program.cs:285` | `ed2edeabc340c5899940bcda9eda7e66` | **不变** | `基线/out_baseline.txt` 现读 md5 与常量逐字符相同；解析差异 0 行 |
| `PinnedParseMd5` | `Program.cs:350` | `136cbb6731e1d6a6ed171462fdb3de00` | **不变** | `check` ⑦ ✓（本波解析器零改动） |
| `PinnedPlayMd5` | `PlayProbe.cs:4057` | `a577ab809f9d0c793d31ed9812f88019` | **`5f945c49463d8e818a445811447836b1`** | `play all pf` 连跑两次逐字节相同（主档 `a7221094…` · 电池 `08dfbc48…` · 1,715 行） |

> 简报原写「解析侧要改两颗」——**实测只改了一颗（池）＋结算那一颗**；那两颗早已是第八十八会话 `F5` 钉过的值。
> `基线/_bak_1028b/` 留着重钉前的两份基线（`out_baseline.txt` / `out_play_baseline.txt`）。

**结算基线的差异面 = 26 行**（行数 1,715 不变、id 集合不变）：

| 行 | 张数 | 成因 | 判定 |
|---|---|---|---|
| 主档 `EC48` | 1 | `UnitState.cs:1548` `RecomputeActAfterAuraKeyword`（B89 `E4` · `L1376`，判据 `CardScript__AddEffect.c:570-572` → `UpdateSummonSicknessAnims` → `ActivateMinion`）：Daemonette 被 `Alluress` 光环挂上 **Flank** ⇒ 重算 `canAct` ⇒ 不再算召唤病 | ✅ 正确 |
| 主档 `AM49` | 1 | 本文 §1 的闸（第二句不再被点定目标劫持） | ✅ 正确（顺带修好） |
| 电池 Mob 13 张 | 13 | `RuleCore.cs:6886-6918` 删掉「攻击者自己跑 Mob」＋ `EffectResolver.cs:9967/:18174` 同方其余那一趟 | ✅（净差大多转 0，见 §5·2） |
| 电池 Regiment 10 张 | 10 | 同上（Regiment 那一半） | ⚠️ 见 §5·2（**可疑**） |
| 电池 `AM70` | 1 | 其 Rally 部署的 `AM8`（Regiment）现在会跑自己的 Regiment | ✅ 正确 |

---

## §3 rc 读数（重钉后，全部现跑）

| 命令 | rc | 读数 |
|---|---|---|
| `check` | **0**（连跑 2 次） | 哨兵 **0 条失败**；⑤ `[基线指纹]` ✓（卡池与基线都与钉住一致）；⑦ `[输出指纹]` ✓ `136cbb67…`；全池 1,121 张 · 解析差异 **0 行** |
| `play check` | **0**（连跑 2 次） | 现算 `5f945c49…` = 基线文件 `5f945c49…`；①-a / ①-b 无差异行；② 哨兵 ✓ |
| `play all` | **1** | 指纹一致；**⑧ 自检红 1 条 = `Mob（攻击那一发）` 净差 0 vs 0**（见 §5·1，**接手前就是红的**） |
| `bash 工具/typecheck.sh` | **0 错误** | 运行时程序集 0 · 编辑器程序集 0（改完 `.cs` 的秒级检查） |

---

## §4 有没有归不了因的卡

**没有。** 34 行（重钉前的差异面）全部点名到具体改动；其中 9 张是回归、已修好并逐字节回到旧基线。
重钉后的差异面只剩 §2 那 26 行，每一行都有判据。

---

## §5 顺手发现（下个会话必须知道）

### 5·1 ⑧ 自检的 `Mob` 那一格**恒红** —— 是**夹具盲区**，⛔ 不是引擎缺陷（已就地订正）

- 现象：`Mob（攻击那一发）` 两条净差都是 0（**两条都是空的**）⇒ `play all` rc 恒 1。**我接手第一次跑就是这样**，与本次改动无关。
- 🔴 **订正（铁律 5）**：初版把「13 张 Mob 卡净差塌成 0」判成 `atkType` 现推的缺陷 —— **不成立**：
  · `RuleCore.cs:6984`（`B89-E26`，本波新增）**已经显式传** `attackType: ranged ? 2 : 1`；
  · `EffectResolver.cs:9896` 是 `attackType > 0 ? attackType : (…现推…)` ⇒ 现推只在 `attackType == 0`（非 Attack）时兜底 ⇒ **攻击这一族永远走显式值**。
  ⇒ **近战跑 Mob、远程跑 Regiment 是对的**，`RV` 残余 #4 已由 `E26` 闭合；⛔ 不要再加第二处显式传参。
- **13 张 Mob 净差 0 是正确读数**：本波按原版删掉「攻击者自己跑 Mob」（`CardScript__ResolveUnitAttacked.c:34` 那支 return），
  而电池夹具里**同方没有第二个 Mob 单位**可扫 ⇒ 这一族在夹具里**结构上无可观测**（不是「卡没效果」）。
  对照：`E|AM14` 仍留着 2 条净差 —— 那是**旧基线里就有**的那一笔（`P0[3] AM14 2/4 → 2/5` + 伤害累计），
  不是本波新引入的（本波只是把另外 8 条「自触发」去掉了）。
- 我试过三种探针侧修法（**全部已撤回**，`PlayProbe.cs` 现在只差 `PinnedPlayMd5` 一行）：
  1. 在 P0 再放一张「同方陪练」（`MateDesc`）—— 扫到了（`[FIRE] FX_MATE kw=mob rc=True`），但收益不进快照；
  2. 把陪练挪到 P0 回合部署（原来挂在 P1 回合被「不是你的回合」挡掉）—— 仍 0；
  3. 把被测卡做成 **0 攻**，逼 `kill` 那一步退到夹具部队出手 —— 仍 0。
  ⇒ 按调度台口径：**停手、不再试第四种**。下一步该由「引擎线的量具设计」决定（例如给电池加一个「同方陪练」档并查清为什么那笔收益不进快照），
  ⛔ 不要在 ⑧ 里为了变绿去改被测对象。

### 5·2 其他

1. 探针源码 `PlayProbe.cs` **只改了 `PinnedPlayMd5` 一行**（实验全部撤回；`git diff` 现读 = 1 insertion / 1 deletion）。
2. `EffectResolver.cs` 的文件级 diff 里有本波别人的大量改动（462 行）；**本笔只占 2 行**（`:1929` 注释 + `:1930` 条件），行数 18,206 不变。
3. 证据留档（`d:/4/_tmp_view/repin_before_1028/`）：`check_before/after_repin` · `playcheck_before/afterfix1/after_repin1-2` ·
   `out_play_baseline_new1/2`（修前候选，含 34 行）· `playall_after_repin.log` · `final_*`。

---

## §6 调度台点评（原文记档，供后续会话知道这两条判断被认可过）

> - 「你**拒绝重钉**那次救了 9 张卡的回归 —— 这是本轮最重要的一次判断。」
> - 「你**三次探针侧修法全撤回**也极对：**探针是量具，量具不该为了读数好看去改被测对象**。」

⇒ 以后遇到「基线红了但差异面上有回归」时，**默认动作仍是「先证明变化是对的，再钉」**（本报告 §1 就是那次拒绝的完整证据链）。
