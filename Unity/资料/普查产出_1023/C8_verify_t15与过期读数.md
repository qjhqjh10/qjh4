# C8 · `verify_t15` 过期断言收口 + 3 处过期读数（铁律 5）

> 2026-10-23。执行代理 C。未动 git、未跑 Unity、未改正本、`d:/2` 只读。
> 只碰了主对话指定的 4 个文件（1 个验证脚本 + 3 处注释），外加本报告。

---

## ① 结论

### 1·1 `verify_t15_anim_chains.py:189` 的过期断言 —— ✅ 已收口，**25/25 全过（退出码 0）**

**改法**（按 `C7 §③·1` 的建议，**有条件放宽、不删硬断言**）：
- 先读 `stats.catalogKind`：
  - **`bin`（二进制 catalog，与本地束同 build）** ⇒ 断言 `skipped_exact_false == 0`
    （短键 `internalId` 是真键，**不需要「整族跳过」**，计数 0 才是正确态）；
  - **其它（旧 JSON catalog 路径）** ⇒ 保留原断言 `> 0`。
- **`wrong == 0`（`exact=False` 的行不许被对到别的对象）原样保留**，仍是硬断言 ——
  它才是真正的安全判据；本次修改**没有**碰它（注释里也写死了「⛔ 别因为本条的放宽而删掉它」）。
- 文件头 `②b` 那段补了 2026-10-23 的口径说明（为什么 bin 下 0 是对的）。

**跑出来的结果**：
```
[2b] catalog 补跳的安全闸
  [OK]   旧表 exact=True 的 4997 行：guid_to_asset 名字逐字相同
  [OK]   旧表 exact=False 的行没有被 catalog 对到别的对象
  [OK]   stats：二进制 catalog（catalogKind=bin）下「exact=False 整族跳过」= 0（真键直取，不再需要跳过；安全由上面 exact=False 逐行核对守）
…
全部通过 ✅
```
→ **`[OK]` 25 条 / `[FAIL]` 0 条 / 退出码 0**（改前是 24 OK / 1 FAIL / 退出码 1）。

### 1·2 3 处过期读数 —— ✅ 已按铁律 5 订正（只改注释数字）

| 文件:行 | 改前 | 改后 |
|---|---|---|
| `工具/extract_missing_shaders.py:297` | `guid_to_asset（10,539 条）**8/8 都没有对应 guid**` | `guid_to_asset（**10,564** 条；⚠️ 2026-10-23 现读，原写 10,539）` + 下一行补「这 8 件是 `Wildcard_Use_*` 根；2026-10-23 用新表复核 `name` 仍 0 命中」 |
| `工具/gen_cardanim_clips.py:21` | `10,539 条 guid_to_asset / 1,099 条 cardanim_to_asset` | `**10,564** 条 guid_to_asset / 1,099 条 cardanim_to_asset；⚠️ 2026-10-23 现读（原写 10,539 —— T14 的二进制 catalog 补集 +25 条；本脚本只读 cardanim_to_asset，那一节未变）` |
| `MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:934` | `guid_to_asset（10,539 条）**8/8 都没有对应 guid**` | `guid_to_asset（**10,564** 条；⚠️ 2026-10-23 现读，原写 10,539）` + 下一行补 8/8 复核说明 |

**顺带复核了那句「8/8 都没有对应 guid」本身**（不只是改数字）：用**更新后的** `anim_address_map`
按 `name` 查那 8 个 `Wildcard_Use_{Button,Card} {1..4} {Common,Rare,Epic,Legendary}` 根 —— **8/8 仍 0 命中**
（本次 +25 条全是 EC 语音/卡框，与它们无关）⇒ 该结论**今天仍然成立**。

---

## ② 证据

**`verify_t15`**
- 改动：`工具/verify_t15_anim_chains.py:19-21`（文件头补口径）· `:189-209`（条件分支；`wrong==0` 那条在 `:186-188` 未动）。
- 运行：`d:/4/_tmp_view/wfC8/verify.log` —— `[OK]` **25** / `[FAIL]` **0** / `全部通过 ✅` / 退出码 **0**。
- `git diff --numstat`：**`19 2`**（仅这两处）。

**3 处注释**
- `git diff --numstat`：`EffectExporter.cs` **`2 1`**；`extract_missing_shaders.py` 的本次改动 = 那 1 行→2 行（`@@ -291,7 +294,8 @@` 那个 hunk），
  ⚠️ **该文件的 `192/7` 里另有约 190 行与本批无关的未提交改动**（`--arena-anims` 的 9 条 legacy clip；本批只改了那一个 hunk，这些内容**在我编辑之前就已在盘上**）；
  `gen_cardanim_clips.py` 是**未跟踪文件**（另一会话新建，尚未入 git）⇒ `git diff` 里没有它，但改动已在盘上。
- 行尾：`EffectExporter.cs` **仍是 CRLF 4965/4965**（改前 4964/4964，只多一行）；两个 `.py` 仍全 LF。
- 复核脚本：本报告 1·2 的「8/8 仍 0 命中」用 `anim_address_map.json` 的 `guid_to_asset[*].name` + `also[*].name` 直接查名（只读）。

**`.cs` 改动后的秒级类型检查**
```
TMPDIR=/tmp/wf_C8 bash d:/4/Unity/工具/typecheck.sh
→ 运行时错误数: 0 / 编辑器错误数: 0
```

---

## ③ 没查清 / 说明

1. **`extract_missing_shaders.py` 工作树里混着别人的改动**：`git diff` 显示该文件 `192/7`，其中约 190 行是「arena-anims 9 条 legacy clip」的改动（`@@ -106` / `@@ -376` 两个 hunk），**不是本批的**；本批只改 `@@ -291` 那个 hunk 的两行。⛔ 我没碰其它 hunk（也没法把它们从 diff 里分开——它们在盘上本来就存在）。
2. **`gen_cardanim_clips.py` 未入 git**（`git ls-files --error-unmatch` 返回未跟踪）：本批只改它的第 21 行注释；入不入库由主对话决定。
3. **`10,539` 在文档里还有多份历史读数**（本批所有权只到那 3 处代码注释，文档未动）：至少 `资料/任务线/特效线/任务总表.md` · `普查产出_1019/{W57_特效线_T15_V1_T02静态,_调度_文件所有权_第五十七会话}.md` · `普查产出_1022/{A2_素材未导出与未使用,J-A_战场素材与材质诊断,W64_素材导出缺口}.md` · `普查产出_第十八会话/B_三条判据挖掘.md` · `说明书/04_界面UI/菜单全树.md` —— 按铁律 5 迟早要订正，**本批没动**。

---

## ④ 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/工具/verify_t15_anim_chains.py` | **+19/−2**：`:189` 断言按 `stats.catalogKind` 分支（`bin` ⇒ `skipped==0`；旧 JSON ⇒ `>0`）；`wrong==0` 硬断言保留；文件头 `②b` 补口径 |
| `Unity/工具/extract_missing_shaders.py` | **只那一段注释**（`:297` 附近 1 行→2 行）：`10,539 → 10,564（⚠️ 2026-10-23 现读）` + 8/8 复核说明 |
| `Unity/工具/gen_cardanim_clips.py` | **只那一行注释**（`:21`）：同上（该文件未跟踪） |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs` | **只那一段注释**（`:934` 附近 1 行→2 行）：同上；CRLF 未翻 |
| `Unity/资料/普查产出_1023/C8_verify_t15与过期读数.md` | 本报告 |

**验过**：验证脚本 **25/25 / exit 0** · typecheck **0/0** · `EffectExporter.cs` CRLF 未翻 · 未动 git / 未跑 Unity / 未改正本 / 未动其它文件。

---

## ⑤ 顺手发现

1. **`verify_t15` 的「安全闸」现在分成两半**：名字逐字相同（exact=True）· 没被对错对象（exact=False）—— 这两条**与 catalog 形态无关**，永远该硬断言；只有「整族跳过计数」是路径相关的。这次改完，它以后无论 bin 还是 json 都能跑（bin 走 0、json 走 >0）。
2. **`10,539` 的「残留」比主对话点名的 3 处多**：`.md` 里还有 8 个文件提到它（见 ③·3）。它们多是**历史普查快照**（`普查产出_1019/1022`），按「历史文档不回写」的惯例可以不动；但 `任务线/特效线/任务总表.md` 与 `说明书/04_界面UI/菜单全树.md` 是**活文档**，建议下一批订正。
3. **`gen_cardanim_clips.py` 未入 git** 本身值得记一笔：它是 2026-10-22/23 那批新建的产物脚本之一，若一直不跟踪，下次交接可能「找不到生成器」。
4. **本批的 3 处注释都写了「原写 10,539」**（保留更正痕迹，符合铁律 5）—— 所以现在全仓搜 `10,539` 仍会命中这 3 个文件，那是**预期**，不是漏改。
