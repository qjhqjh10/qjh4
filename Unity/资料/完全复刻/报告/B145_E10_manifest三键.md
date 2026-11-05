# B145-E10 · 5 场 manifest 补粒子三键

> 执行代理 `B145-E10` · 2026-11-05 · 判据 = `报告/B145_E8_ArenaBuilder.md` 顺手发现 §2
> 目标：`genestealers` · `leviathan` · `sororitas` · `spacewolves` · `tauviorla` 五场 manifest 的粒子条目补齐
> `renderingLayerMask` / `lightProbeUsage` / `sortingOrder`（合计 323 颗），其余字段一个字节都不变。
>
> 做法：逐场备份 → 逐场 `python gen_unity_arena_manifest.py --arena <场> --no-copy` → 跑完立刻比对 → 下一场。
> 生成器未改（三键输出早在 `gen_unity_arena_manifest.py:2211-2213`）。未跑 Unity · 未动 git · 未改账。

---

## 一、逐场结果

| 场 | 前：粒子数 / 三键覆盖 | 后：粒子数 / 三键覆盖 | 其它字段逐字节不变 | diff（相对 HEAD） | 判定 |
|---|---|---|---:|---:|---|
| battlearenagenestealers | 35 / 0·0·0 | 35 / 35·35·35 | ✅ | +105 / −0 | **PASS** |
| battlearenaleviathan | 45 / 0·0·0 | 45 / 45·45·45 | ✅ | +135 / −0 | **PASS** |
| battlearenasororitas | 49 / 0·0·0 | 49 / 49·49·49 | ✅ | +147 / −0 | **PASS** |
| battlearenaspacewolves | 31 / 0·0·0 | 31 / 31·31·31 | ✅ | +93 / −0 | **PASS** |
| battlearenatauviorla | 163 / 0·0·0 | 163 / 163·163·163 | ✅ | +489 / −0 | **PASS** |
| **合计** | **323 / 0** | **323 / 323** | — | **+969 / −0** | **5/5 PASS** |

比对口径（`_tmp_view/manifest_check_e10.py`，读备份 vs 现盘，`json` 逐条比）：
- `len(meshes)` / `len(particles)` 前后相同（无新增/删除条目）；
- 逐条剥掉三键后，`meshes` 与 `particles` **全部条目相等**；
- 除 `meshes` / `particles` 外的所有顶层成员（`ambient` `camera` `defaultEnv` `light` `postFx` `scene` `tapHits`）**逐值相等**。

`+969 / −0` 与 `323 × 3 = 969` 完全吻合：相对 HEAD **只有新增行、零删除/零改写**，即三键以外的文件文本原样保留。

---

## 二、三键取值分布（新增后）

| 场 | renderingLayerMask | lightProbeUsage | sortingOrder |
|---|---|---|---|
| genestealers | `1`×29 · `4294967295`(All)×6 | `0`×35 | `0`×35 |
| leviathan | `4294967295`(All)×35 · `1`×10 | `0`×35 · `1`×10 | `0`×39 · `1`×6 |
| sororitas | `1`×49 | `0`×49 | `0`×49 |
| spacewolves | `1`×31 | `0`×31 | `0`×31 |
| tauviorla | `1`×139 · `4294967295`(All)×24 | `0`×146 · `1`×17 | `0`×163 |

> 意义：这 5 场原先全部缺键 ⇒ `ArenaBuilder.ApplyParticleRendererParams` 只能吃兜底 `(1, 0, 0)`；
> 现在 **leviathan 的 6 颗 `sortingOrder=1`、tauviorla 的 17 颗 `lightProbeUsage=1`、合计 65 颗 `All`** 才是原版真值，
> 不再被兜底洗掉。

---

## 三、全 13 场复核（只读，确认 A1/A2 条件已成立）

`_tmp_view/scan13_e10.py` 现读：**13/13 场粒子侧三键齐全**，`total_entries = 578`（原 255 + 本次 323）。

| 场 | 粒子数 | renderingLayerMask | lightProbeUsage | sortingOrder |
|---|---:|---|---|---|
| battlearena1 | 34 | All×28 · 1×6 | 0×20 · 1×14 | 0×32 · 1×2 |
| battlearena2 | 46 | All×31 · 1×15 | 0×22 · 1×24 | −1×1 · 0×35 · 1×8 · 5×2 |
| battlearena3 | 46 | All×1 · 1×45 | 0×36 · 1×10 | −1×2 · 0×38 · 1×6 |
| battlearenaaeldari | 23 | All×13 · 1×10 | 0×16 · 1×7 | 0×23 |
| battlearenaastramilitarum | 20 | All×4 · 1×16 | 0×20 | 0×20 |
| battlearenablacklegion | 39 | All×14 · 1×25 | 0×30 · 1×9 | 0×39 |
| battlearenadarkangels | 34 | 1×34 | 0×34 | 0×34 |
| battlearenaemperorschildren | 13 | 1×13 | 0×13 | 0×13 |
| battlearenagenestealers | 35 | 1×29 · All×6 | 0×35 | 0×35 |
| battlearenaleviathan | 45 | All×35 · 1×10 | 0×35 · 1×10 | 0×39 · 1×6 |
| battlearenasororitas | 49 | 1×49 | 0×49 | 0×49 |
| battlearenaspacewolves | 31 | 1×31 | 0×31 | 0×31 |
| battlearenatauviorla | 163 | 1×139 · All×24 | 0×146 · 1×17 | 0×163 |

**与判据参考分布吻合**：battlearena1 = All×28 / 1×6 · lightProbe 1×14 · sorting 1×2，逐项一致 ⇒ 生成器抽出来的确实是原版 `ParticleSystemRenderer` 真值。

对应断言（未跑 Unity，仅按条件推断）：`B144_R1_Manifest_Assert` 的 **A1（13 场每场三键齐）/ A2（13/13）** 条件已满足；
**A3（汇总有非默认值）** 也满足（All 总 156 · lightProbe1 总 91 · sortingOrder1 总 22，均 > 0）。

> 上述 A1/A2/A3 已用 `_tmp_view/emulate_a1a2_e10.py` **按断言的文本口径**（切片 `"particles": [` … `"\n  ],"` 再数字符串出现次数）现读复核：**13/13 OK**。

---

## 四、异常 / 已还原的场

**无。** 五场全部一次通过，没有出现「粒子条目数变化」「三键以外字段变化」「新增/删除条目」，
未触发任何还原。备份仍留在 `_tmp_view/manifest_bak_20261105/`（5 份原始文件）。

---

## 五、与备份的 diff 统计

| 文件 | + | − |
|---|---:|---:|
| battlearenagenestealers_manifest.json | 105 | 0 |
| battlearenaleviathan_manifest.json | 135 | 0 |
| battlearenasororitas_manifest.json | 147 | 0 |
| battlearenaspacewolves_manifest.json | 93 | 0 |
| battlearenatauviorla_manifest.json | 489 | 0 |
| **合计** | **969** | **0** |

`git status` 复核：`arenas/` 下本次只动了这 5 份 manifest；`battlearenaemperorschildren_manifest.json` 的 `M`
是**本轮之前就存在**的改动（会话开始时的 git status 已有），本代理未触碰。

---

## 六、顺手发现

1. **本次未跑 `typecheck`** —— 没有改任何 `.cs`（只重生成 5 份 JSON），按铁律 12「纯数据/纯文档零条」口径不需要。
2. **生成器单场耗时约 2–4 分钟**（genestealers ~3min、leviathan ~2min、sororitas ~5min、spacewolves ~4min、tauviorla ~4min），
   全部串行按 `--arena` 单场跑，未用 `--all`（铁律 15）。
3. **`--no-copy` 只写 manifest**，未拷贝任何模型/贴图；5 个目录下其余文件的 mtime 全部未变。
4. **生效边界**：manifest 补齐 ≠ 画面变化。真正写进 PSR 还要跑 `ArenaBuilder.BuildPrefabFromCLI`（Unity 窗口，B-04 的活）；
   本轮按红线未跑。
5. **8 场既有数据里 `sortingOrder` 有负值/大值**（arena2 的 `-1` 与 `5`、arena3 的 `-1`）——
   印证 E8 的判断「`sortingOrder` 不能夹 0..1」，本次未做任何夹取。
