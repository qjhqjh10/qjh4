# `普查产出_0917` —— 索引与**未结线索**

> 这一批是 **2026-09-17** 一次会话里「**4 路并行子代理 + 主线**」一起产出的。
> **为什么单开一份 README**：每份产出本身只讲自己的结论，**没人告诉你「哪条还没挖完、入口在哪」** ——
> 下一个会话要么重挖一遍，要么不知道那儿还有东西。
> ⚠️ **本文件只做索引与线索汇总，不复述结论**（复述 = 第二份，迟早不一致）。

---

## 一、这批文件是什么（速查）

| 文件 | 一句话 | 复跑入口 | 状态 |
|---|---|---|---|
| `shader属性表_块1~4.md` + `_汇总.md` | 原版 **135 个 shader**（不是 77）逐个的「属性表 / pass 状态 / 采样纹理 / 材质用量」 | `Unity/工具/_dump_shaders_batch.py` | ✅ 完整 |
| `菜单盘点_块1~4.md` + `_汇总.md` | 原版 **313 个菜单/功能界面** vs 我们有什么；**阶段二的地图** | 无脚本（读文档） | ✅ 完整 |
| `效果_shader对账.tsv`（959 行 5 列）+ `_小结.md` | 958 个效果 → 用了哪些原版 shader；哪些没进映射表 | `Unity/工具/_effect_shader_audit.py` | ✅ 完整 |
| `补充shader引用清单.md` | 「33 个补充 shader」的引用与归置情况 | `Unity/工具/_missing_shaders_audit.py`（**只跑 `--check`**） | ✅ 完整 |
| 🆕 `替代shader丢失的源属性_0917.md` | **172 个材质 / 460 条**「源上有值、我们的替代 shader 认不出」的属性 | `Unity/工具/_extract_dropped_props.py` | ✅ 完整 |

---

## 二、🔴 还没解决 / 值得再挖的（按价值排序）

| # | 线索 | 为什么值得挖 | 入口 / 已有证据 | 卡在哪 |
|---|---|---|---|---|
| 1 | **原版 shader 本体就在我们随包的补充包里** | 「近似替代」有 20 个、掉兜底 30 个 —— 既然原件在手，**可以改走原版 shader**，比挑最接近的自建 shader 准得多 | `wf_shaders_extra.bundle` 的 `m_Container` 42 条（**必须按容器路径读，裸字节 grep 会被 LZ4 骗**）· `补充shader引用清单.md` | **没拍板**（是设计决定，不是技术问题） |
| 2 | 🆕 **「黑方块那批」到底清没清** | 拖尾那条成因已修（207 个材质），但 E/D 组的台账数**没重跑** | `资料/特效还原台账.tsv`（由 sweep 生成）· 做法见 `特效还原_进度与交接.md` §七 | 要跑两趟全量 sweep（各 ~25 分、**不能并发**） |
| 3 | ✅ **2026-09-17 已修好**（原记「3 个材质导贴图抛 `ArgumentNullException`」） | **留着当判据用**：① **`EncodeToPNG` 对压缩格式是「返回 `null` 而不抛」** ⇒ `File.WriteAllBytes(path, null)` 抛 `ArgumentNullException: bytes`，**栈指向 `File.WriteAllBytes`、看着跟贴图毫无关系** ② **crunch 压缩的纹理「标记成可读也没用」**（`GetPixels` 直接拒绝），只有走 GPU 才读得出 | 修在 `EffectExporter.ImportTexture`（编不出就摊成 RGBA32 再编）与 `ReadableCopy`（crunch 格式强制走 `Graphics.Blit` → `ReadPixels`） | 已收口，**无需再挖** |
| 4 | 🔴 **经济/进度数值读不出来** | **阶段二的「商店照常能买 / 锻造厂 / 日常奖励」直接卡在这上面** | `_stats.json`：**905 个导出失败全在核心文件**（`resources.assets` 616 · `sharedassets0` 182 · `level0` 73 · globalgamemanagers 29），失败的 MonoBehaviour JSON 只剩 4 个头字段 | **和待办表第 14 行「没有 type tree 的 .assets」是同一个根** —— 那条的做法（签名桩给字段顺序 + 原始字节给值 + 锚点自检）**大概率能救这里** |
| 5 | **`Explosion_Ground` 的根因要重查** | 原来记的「shader 不在包里 ⇒ `Shader.Find` null ⇒ 整块渲不出来」**前提已被推翻** | 更正痕迹在 `WarpforgeShaderMap.cs:50-56` | 更像踩的是「导出那趟加载源包会把补充包顶掉」（`特效还原_进度与交接.md` 那条） |
| 6 | **纹理绑定槽位拿不到确切结果** | 想精确知道「某个原版 shader 采样哪张图」 | `shader属性表_汇总.md`「没解决的」 | DXBC **没有 RDEF 段**（出包时反射表被剥了）⇒ 只能靠 `X_ST`/`X_TexelSize` 兄弟 + 引擎白名单启发式 |
| 7 | **我们自建的 5 个替代 shader 没进属性表** | 第 1 条那个决定要它当输入；`替代shader丢失的源属性_0917.md` 也要它 | `Assets/WarpforgeVFX/Shaders/`（5 个） | 当时主对话正在跑全量重导，**读了怕读到半成品** ⇒ 现在随时可补 |
| 8 | **约 10 项界面待用户裁决** | 决定阶段二要建多少 | `菜单盘点_汇总.md` 末尾 | Draft 整套 · Expansion Pass 套（5 件）· `Card Shop VIP Tab` · 主菜单 Offer 轮播族 · `Two Side Event Select Team` · `Rate Popup` 按钮去向 |
| 9 | **两张映射表的同步缺口 4 个** | 导出报告不会把它们标成「近似替代」 | `效果_shader对账_小结.md`：`Shader Graphs/Fx_ParticleDissolve_apb`×11 · `Fx_RockDissolve`×4 · `Eclipse Tau`×3 · `TextMeshPro/Distance Field Offset`×1 | 未做（**改一张要同步另一张**，见 `EffectExporter.ShaderMap` 头注释） |
| 10 | **`extract_missing_shaders.py:116` 依赖报告第 3 格** | 它按 `p[2]` 取 shader ⇒ 报告一旦两格就**静默读空** | 工具本体 | `LoadReport` 那条**已修**，但这个工具的脆弱依赖还在（**改成读 `效果_shader对账.tsv` 更稳**） |

---

## 三、⚠️ 判读这批产出前必须知道的「尺子」限制

- 🔴 **除主菜单外没有画面参照尺子**：`原版参照图/.../shots_ui/ui_prefabs/` **218 张里 217 张是同字节纯黑**（27,260 B），
  `menu_roots/` 5 张同字节。⇒ 铁律 3 的「拿截图验收」**在这批界面上不成立**，只能靠节点树 + 切图。
  （主菜单能看的只有 `menu_full_0825.png`（**仅顶栏**）与那一张整屏。）
- 🔴 **要点块里的英文名是 GameObject 名、不是 sprite 名** —— 按名找图会扑空，要走 **PathID 反查**。
- ⚠️ **`d:/warpforge/` 是我们自己的 Godot 原型**（70 个 `.gd` / 38,952 行，UI 全写在代码里；
  `.scn`/`.tscn` 只是 265–326 B 的空壳）⇒ **判「Godot 有没有」必须看 `scripts/*.gd`**，
  看 `.tscn` 会把整条线判反（0913 那次就是这么错的）。
- ⚠️ **shader 属性表里带 `[_SrcBlend]` 这种方括号的，是「材料属性驱动」** —— **照状态硬编码 = 错**，必须同时看材质属性值。
- ⚠️ **`导出报告.tsv` 的第 3 格只在「那一行真被导出过」时才有**，而且 `Resume=true` 续跑**曾经**会把它抹掉
  （`LoadReport` 那条，**2026-09-17 已修**）。要 shader 名单**别读那份报告**，用 `效果_shader对账.tsv`。

---

## 四、这批产出的原始数据在哪（别去 `_tmp_view/` 找）

`_tmp_view/` **是 gitignore 的临时目录，会被清空**。本批已经把要留的都落到 `资料/` 下了：

| 原始数据 | 落到哪 |
|---|---|
| 全量重导日志（958 个效果、172 条丢属性警告、3 条异常） | `替代shader丢失的源属性_0917.md`（已提炼）· 日志本体在 `_tmp_view/export_0917.log`（**会丢**） |
| 并排渲染图 13 组 | `_tmp_view/cmp/*.png`（**会丢**，要留就得另存） |
| 自检重写的 `_tmp_view/*.txt`/`*.md` | 跑一次自检就全重生（**别以为丢了**） |
