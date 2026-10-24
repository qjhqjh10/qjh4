# A12 · 生成器 `--check` + `AM34/Valkyrie_Hover_Dust` 证据 + `guid_map` 坑落盘

> 执行代理 **A12**（特效数据线）。日期 **2026-10-24**。仓库根 `d:/4`。
> 承接 **`A9_四个unbacked桶.md`** §四末（「`--check` 不存在」）与 **`A10_余下两个unbacked.md`** §六·1 / §六·2。
> ⛔ 未跑 Unity / player / `-executeMethod`；⛔ 未动 git；⛔ 未改正本（`CLAUDE.md` / `项目任务.md`）；
> ⛔ 未碰任何 `.cs` / `RuleEngine/**` / 其它 `工具/*.py`。

---

## 〇、一句话结论

**三件全部做完**：

| # | 活 | 结果 |
|---|---|---|
| 1 | 给 `gen_card_vfx_by_card.py` 加 `--check`（+ 仓里的 `-h/--help` 早退块） | ✅ 红/绿都实跑过；**默认路径逐字节不变** |
| 2 | 补 `AM34 / Valkyrie_Hover_Dust` 的证据 | ✅ 只换 `why` 文字；判定 / 条目数 / `notPlayed` 全部不变 |
| 3 | 「`guid_map_bundles.tsv` 不能当无引用判据」落进 `资料/已知的坑.md` | ✅ 归到**第五十会话（guid_map 重建）**那节第 10 条 |

---

## 一、改动清单（全部在白名单内）

| 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|
| `D:/4/Unity/工具/gen_card_vfx_by_card.py` | ① 仓里通用的 **`-h`/`--help` 早退块**（`_wf_help_sys`，imports 之后、任何真活之前）② 新增 **`--check`** 开关 ③ 两份产物改成「**先算文本、后写盘**」（`json.dumps` 与 `json.dump` 输出逐字节相同）④ `--check` 时**全程不写盘**、末尾 `_wf_run_check(...)` 比对 ⑤ 新增差异摘要帮手 `_wf_short` / `_wf_json_diff_summary` / `_wf_run_check` ⑥ `UNBACKED_EVIDENCE` 加 1 条 + 表头补注 | `231/12`（含 A9/A10 未提交的 `76/0`） |
| `D:/4/Unity/数据/游戏数据/card_vfx_by_card.json` | 同一次运行产出：**只有** `stats.inferenceGone.unbacked[9].why` 一条变（其余逐字段相同，实测结构差异 **1 处**）；`generatedAt` 仍固定为 `2026-10-24 10:20:00` | `14/14`（A10 那轮是 `13/13`，+1 就是这条 `why` 换行） |
| `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | **字节未变**（`Valkyrie_Hover_Dust` 属 `_unclassified` ⇒ 本来就不进运行时 `notPlayed[]`）；`cmp` 对改前副本 `RT_BEFORE_IDENTICAL` | `1/1`（仍是 A10 那轮的改动，与本轮无关） |
| `D:/4/Unity/资料/已知的坑.md` | 第五十会话（`T14` guid_map 重建）节追加**第 10 条**：`guid_map_bundles.tsv` 只能当「GUID → 束/容器键/资产原名」的翻译表，**不能当零引用判据**（附 `Valkyrie_Hover_Dust` 反例与判据出处） | `11/0` |
| `D:/4/Unity/资料/普查产出_1024/A12_生成器check与坑落盘.md` | 本报告 | 新文件 |

**没有改**：`.cs` · `RuleEngine/**` · 其它工具脚本 · 两张正本 · git · A9/A10 的报告（见 §五·1）。

---

## 二、验收实跑读数

### 1) `--check` **红**（天然差异 = 本轮新证据；证明差异摘要在真差异上指得准）

```bash
cd d:/4/Unity
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 工具/gen_card_vfx_by_card.py --quiet --check \
  --generated-at "2026-10-24 10:20:00"
```

实跑（改生成器后、重出产物前）：

```
[--check] d:/4/Unity/数据/游戏数据/card_vfx_by_card.json **不同** ❌
          （盘上 3166963 字节 / 新算 3167797 字节；第一处不同 @3145300）
           结构差异 **1 处**：
            · $.stats.inferenceGone.unbacked[9].why: 盘上="按名字猜出来的动画；…" / 新算="⛔ **无权威依据、不播**（2026-10-24 `A10` 发现 · `A12` 补录）…"
[--check] d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json 逐字节一致 ✅（401289 字节）
rc=1
```

⇒ **差异摘要准确指到唯一那条**；同时顺带证明 `_unclassified` 那条**不进运行时**（运行时那份逐字节一致）。

### 2) `--check` **绿**（重出产物后）

```bash
... gen_card_vfx_by_card.py --quiet --check --generated-at "2026-10-24 10:20:00"
```

```
[--check] d:/4/Unity/数据/游戏数据/card_vfx_by_card.json 逐字节一致 ✅（3167797 字节）
[--check] d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json 逐字节一致 ✅（401289 字节）
rc=0
```

**`--check` 确实不写盘**：跑前/跑后 `stat -c '%n %s %Y'` 两份产物**逐字段相同**（`MTIME_UNCHANGED(不写盘)`）。

### 3) 故意改一个字节 ⇒ 必须红（字节级判据，结构级看不出来的那种）

在运行时那份**尾部加一个空格**（`401289 → 401290` 字节，JSON 语义不变）：

```
[--check] …/Resources/card_vfx_by_card.json **不同** ❌（盘上 401290 字节 / 新算 401289 字节；第一处不同 @401289）
           结构差异 **0 处**：
rc=1
```

还原（`cp` 回改前副本）后 `cmp` → `RESTORED_OK`，再跑 `--check` → **rc=0**。
⇒ 判据是**逐字节**，不是 JSON 结构比对；尾部多一个字节也抓得到。

### 4) 默认路径（不带 `--check`）逐字节不变 + 确定性

```bash
# 固定时间戳连跑两次，两份产物各自对拍
cmp run1_full 数据/游戏数据/card_vfx_by_card.json          → FULL_IDENTICAL
cmp run1_rt   MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json → RT_IDENTICAL
# 与本轮改前（A10 产出的）运行时那份对拍
cmp 改前_rt   改后_rt                                       → RT_BEFORE_IDENTICAL
```

⇒ 「先算文本、后写盘」这个重构**没有改一个字节**（`json.dumps(obj)` 与 `json.dump(obj, fh)` 输出相同）；
全量件唯一的差异就是本轮要加的那条 `why`（改前/改后**逐字段 walk，差异 1 处**）。

### 5) 计数闸 / 行尾 / `numstat`

| 项 | 读数 |
|---|---|
| 计数闸 | `22 2672 0 0`（`notPlayedCount` / `entryCount` / `problems` / `leftInferred`）—— **不变** |
| 全量件与运行时件一致 | `stats.notPlayed == rt.notPlayed` → **True** |
| `notPlayed` 构成 | `16 duplicate + 6 unbacked` —— **不变** |
| `inferenceGone.unbacked` 条数 | **25 → 25**（本轮只换 `why`，**没有增删条目**；`markedUnbacked` 也未变） |
| 行尾 | 全量件 `CRLF=0 / LF=86104` · 运行时件 `CRLF=0 / LF=0`（单行）· 生成器 `CRLF=0 / LF=2450` · 坑表 `CRLF=0 / LF=6976` |
| `git diff --numstat` | 生成器 `231/12` · 全量件 `14/14` · 运行时件 `1/1` · 坑表 `11/0`（**没有翻行尾**） |
| `-h` 早退 | `real 0m0.127s`，只打印 3 行用法，`rc=0`（**没跑真活**） |

---

## 三、`--check` 怎么用（给下个会话 / 给断言）

```bash
# 绿/红判据：rc=0 = 盘上两份产物 == 生成器现在算出来的；rc=1 = 有人手改过 / 输入变了没重跑
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_card_vfx_by_card.py \
  --quiet --check --generated-at "2026-10-24 10:20:00"
```

- 🔴 **必须固定 `--generated-at`**：不固定时 `generatedAt` 取当前时间，**必然红**（`--check` 末尾会打一句提示）。
- ⛔ **别拿 `--no-truth --check` 对拍**：那条路出来的表结构完全不同（所有推断变「无依据」），**必然红**，不是缺陷。
- 差异摘要 = **结构路径 + 盘上值 / 新算值**，最多列 8 条（总数照报）；另有「第一处不同的**字节偏移**」——
  像本次「尾部多一个空格」那种**结构上看不出来的差异**，只有字节偏移能指出来。
- 它只比对 `OUT_JSON`（全量）与 `OUT_RUNTIME`（运行时）**两份**；不写任何文件、不建目录。

---

## 四、`AM34 / Valkyrie_Hover_Dust` 的状态核对（简报要求「不一致就写进报告、⛔ 别擅自改判定」）

| 产物 | 位置 | 现状（本轮现读） |
|---|---|---|
| **全量件** | `stats.inferenceGone.unbacked[9]` | `event = "unclassified"`（原事件桶 = `_unclassified`）· `cardanim = Valkyrie_Hover_Dust` · `confBefore = inferred-name` · `why` = **本轮新证据** |
| **运行时件** | `notPlayed[]` | **没有这一条**；AM34 在 `notPlayed` 里只有 `ev = attack_ranged`（`Valkyrie_MultiLaser_Hover_Target`）这一行 |

⇒ **两边「不一致」是【既有设计】**：`strip_unbacked_inference` 里
`in_runtime = not ev.startswith("_")` —— `_unclassified` 本来就不进运行时表（运行期按事件查，查不到）。
**判定一行未动**（条目数前后都是 25，`notPlayed` 22 条不变），只把 `why` 写具体。
⛔ **不要**为了「让两边一致」把它塞进运行时 —— 那会改判定、也不是原版口径。

---

## 五、没查清 / 没做

1. 🔴 **A9 报告 §四末那段「`--check` 不存在 / 属发明口径留调度台裁」现在过期了** —— 调度台已裁定要加、本轮已加完。
   那段在 `资料/普查产出_1024/A9_四个unbacked桶.md`（**别的代理的报告**），我**没有就地改**（避免与可能的写手撞车），
   请调度台核销或订正。
2. **没有原版实况**（原版已关服、后端不可达）—— 同 A9/A10，本报告的结论全部来自当前产物 + 生成器的确定性，不是实拍。
3. **差异摘要只到「结构路径」级**（最多 8 条）：运行时那份是**单行 400 KB**，逐行 diff 没法看；
   长 `why`（如本次 AM34 那条）会被截到 110 字符 —— **定位够用，看全文要回产物**。
4. **没加「只比其中一份产物」的开关**（`--check` 一律两份都比）—— 简报没要求，加了反而是新口径。
5. **没在 `--no-truth` 下跑 `--check`**（必然红、无诊断价值）。

---

## 六、顺手发现（交给调度台分流）

1. 🟡 **两份产物里 `unbacked` 这个词指【两个不同集合】，台账写数字时要带口径。**
   现读：全量件 `stats.inferenceGone.unbacked[]` = **25** 条（**19 条 `_unclassified`** + 6 条真实事件桶），
   而运行时 `notPlayed` 里 `outcome == "unbacked"` = **6** 条（A9/A10 报告说的「6 个 unbacked」是**这个**口径）。
   ⇒ 以后写「unbacked 有 N 条无证据」必须说清是哪一份；否则会出现「25 条都没证据」这种误读。
2. 🟢 **`--check` 现在可以当「产物有没有被手改 / 输入变了忘了重跑」的判据**，
   典型场景 = `K06` 那种 id 迁移、`T14` 那种换真值快照之后 —— 改完跑一次 `--check`，rc=0 才算产物跟上。
3. 🟢 **`-h/--help` 早退块放在 `import` 之后、常量之前是安全的**：本脚本模块级**没有任何可执行语句**
   （实测：`import` 之后到 `def main()` 之前，除字面量闭括号外没有顶层语句）⇒ `--help` 不会触发任何读盘。
4. 🟡 **本轮的差异定位读数本身可复用**：`$.stats.inferenceGone.unbacked[9].why` 这个下标说明
   **`unbacked[]` 的顺序是稳定的**（同一份输入，两次跑同序）—— 但 ⛔ 别把下标写进断言，
   下一条证据进来就会挪位（用 `(cardId, cardanim)` 定位，和 `UNBACKED_EVIDENCE` 的键一致）。
