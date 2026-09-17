# 工具/ —— Python 工具链入口

> **运行工具请去 `d:/2/Warpforge_tools/scripts/`**，不要在这里跑。
> 本目录只是「快照 + 同步器 + 日志」，因为脚本里硬编码了 `d:/2/解包整理/...` 绝对路径，
> 还依赖同级 `../data/`（如 `cab_bundle_map.json`），搬走会改变行为。

## 怎么跑

```bash
PY=D:/2/Warpforge_tools/py312/python.exe      # 系统 Python 3.14 缺纹理转换模块，不要用
$PY d:/2/Warpforge_tools/scripts/<脚本名>.py
```

## 目录内容

| 文件 | 说明 |
|---|---|
| `sync_from_d2.py` | ★ 从档案库 `d:/2` 复制资源的**可重跑脚本**（幂等；目标已存在同名文件则跳过并记日志，绝不覆盖）。`--list` 看计划 / `--batch <名>` 跑单批 / `--batch all` 全跑 |
| **`read_literal.py`** | ★ **读反编译 `.c` 里的 `_DAT_xxxxxxxxx` 常量**（Ghidra 没跟到的只读字面量）：PE 节表把 RVA 映射成文件偏移读 4 字节。**读出来要「整齐」才算对**（240.0 / 0.4 这种），乱数=映射错了。2026-09-13 加，用它读出了 `DoPushBack` 的 0.4/0.3 |
| **`verify_feel_params.py`** | ★ **把手感参数回到四个原始来源里读一遍**（99 条 AnimationClip / 74 个 UnitTweenSO / 卡预制体字段 / DLL 常量），和 `Core/CardFeel.cs` 的常量表对照。⚠️ 它是「哪些参数不是原版的」那次追问的产物，见 `资料/规则引擎_进度与交接.md` 第二十五轮 |
| `scripts快照/` | 13 个关键脚本的**只读快照**，供阅读与检索。改造请直接改 `d:/2/Warpforge_tools/scripts/` 下的原件 |
| `_同步日志_*.json` | 每批的 `label / src / dst / files / bytes` —— 查"这东西从哪来的" |
| `_已清理临时目录清单.json` | 2026-09-10 清理掉的 3 个临时目录的内容清单（留档） |
| `Warpforge_tools原始README.md` | `d:/2/Warpforge_tools/README.md` 的副本。⚠️ 内有已知陈旧引用（`D:\2\Warpforge_assets_full` 已不存在），以 `资料/资源使用手册.md` 为准 |

## 最常用的几个（按用途）

| 用途 | 脚本 |
|---|---|
| ★ 战场 → Unity（读场景 JSON，写 manifest + 拷模型贴图） | `gen_unity_arena_manifest.py`（依赖 `unity_scene_to_godot.py` + `gltf_export.py`） |
| ★ RectTransform 链式换算 → 屏幕绝对坐标（权威） | `chain_rect.py` |
| 场景全树 / 按 GO 名 dump 子树 | `dump_scene_tree.py` / `dump_go_tree.py` |
| 全局贴图索引 / 补齐场景贴图 | `build_texture_index.py` |
| 从 Font JSON 还原 TTF | `restore_fonts.py` |
| UI 图集切片 | `slice_ui_atlas.py` |
| 单场景 bundle 提取 | `extract_scene_bundle.py` |
| 粒子 / 动画曲线 → 归一化 JSON | `convert_unity_particles.py` / `convert_unity_anim.py` |
| 规则书提取 | `extract_rulebook_md.py` |

完整 124 个脚本 + 用法见 `d:/2/Warpforge_tools/scripts/` 与 `资料/资源使用手册.md` §3。

---

## 🆕 2026-09-17 新增的四个（**跑在 `d:/4`，不依赖 `d:/2`**）

> 上面那句「运行工具请去 `d:/2/Warpforge_tools/scripts/`」说的是**从档案库同步过来的那批**
> （它们硬编码了 `d:/2` 路径、还依赖同级 `../data/`）。下面这四个是**本工程自己长出来的**，
> 路径全写死 `d:/4`，**就在这里跑**。Python 仍用 `D:/2/Warpforge_tools/py312/python.exe`。

| 脚本 | 干什么 | 产出落在哪 | 出处 |
|---|---|---|---|
| `_dump_shaders_batch.py` | 把原版 **135 个 shader** 逐个 dump 成「属性表 / pass 状态 / 采样纹理 / 材质用量」 | `资料/普查产出_0917/shader属性表_块1~4.md` + `_汇总.md` | 2026-09-17 并行活 ④ |
| `_effect_shader_audit.py` | 958 个效果 → 用了哪些原版 shader；哪些没进映射表 | `资料/普查产出_0917/效果_shader对账.tsv` + `_小结.md` | 并行活 ⑮（`--check` 只打汇总、不写文件） |
| `_missing_shaders_audit.py` | 「补充 shader」的引用清单 | `资料/普查产出_0917/补充shader引用清单.md` | 并行活 ⑯（**只读**，绝不重打 bundle） |
| `_extract_dropped_props.py` | 把导出日志里「替代 shader 认不出源属性」那批警告提炼成清单 | `资料/普查产出_0917/替代shader丢失的源属性_0917.md` | 2026-09-17 主线（**原始日志在 `_tmp_view/`，会被清空 ⇒ 必须提炼**） |

⚠️ 四个都是**只读**：不启动 Unity、不写 `MyGame/Assets/`。
📌 这批的**索引与未结线索**看 `资料/普查产出_0917/README.md`。
