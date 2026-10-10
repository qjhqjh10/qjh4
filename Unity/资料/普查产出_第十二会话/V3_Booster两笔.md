# V3 — Booster 两笔（`A1117` 24 条 `m_Script` guid 0 · `A1118①` 两份 TMP 字体资产）

执行代理：`V3`。**按红线：⛔ 一次 Unity 都没跑**（没有任何 `-executeMethod`）· ⛔ 没动 git ·
⛔ 没碰两张正本 · ⛔ **一个字都没改 `.cs`**（`BoosterPackExporter.cs` 在**白名单外**，只读）。

---

## ① 结论

| 账 | 做没做到 | 结论 |
|---|---|---|
| **`A1117`**（24 条 `m_Script` guid 0） | ✅ **24/24 全部修好** | 17 × `350208831926335389`→`fe87c0e1cc204ed48ad3b37840f39efc`（`Image.cs`）· 7 × `7477354737935883349`→`f4688fdb7df04437aeb418b961361dc5`（`TextMeshProUGUI.cs`）。两扇窗现存 `m_Script` guid 0 = **0**。⚠️ **但改的只是 prefab 产物本身**，见 §⑤·1 的持久性警告 |
| **`A1118①`**（两份 `TMP_FontAsset` 导进工程 + `m_fontAsset` 接上） | ✅ **两份都导进来了 · 134 条全接上** | 新增 `Assets/CardPresentation/Resources/Fonts/{Pragati,Asar}-Regular SDF.asset`（**Static**，与原版一致）· 两扇窗 `m_fontAsset` guid 0 现读 = **0**（`Open Window` 92+35 = 127 · `Info Popup` 6+1 = 7，合计 **134**） |
| 秒级类型检查 | ✅ 过 | `TMPDIR=/tmp/wf_v3 bash 工具/typecheck.sh` → 运行时 **0** 错 · 编辑器 **0** 错 |

---

## ② 24 条 guid 0 逐条表（全部在 `Booster Info Popup.prefab`；`Booster Pack Open Window.prefab` 是 0 条）

判据链 = 原版 pathID →（`assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json`）`m_ClassName` →
（工程 `Library/PackageCache/com.unity.ugui@*/Runtime/…/*.cs.meta`）同名类的 guid。**两条都独立核过**。

| # | 组件锚点 | 所在 GameObject | 原版 pathID | 类名 | → 工程脚本（guid） | 结果 |
|---|---|---|---|---|---|---|
| 1 | `&1313532062274174109` | `Category` | `7477354737935883349` | `TextMeshProUGUI` | `TMP/TextMeshProUGUI.cs` `f4688fdb7df04437aeb418b961361dc5` | ✅ 已接 |
| 2 | `&7390364307758399587` | `text` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 3 | `&5533923383401363503` | `CrateCounter` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 4 | `&4334891491716175128` | `Title` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 5 | `&1790402624820143008` | `Button Text` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 6 | `&9054314131324378050` | `Descripton` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 7 | `&4367575332599192420` | `Button Text` | `7477354737935883349` | `TextMeshProUGUI` | 同上 | ✅ 已接 |
| 8 | `&1844823363915411352` | `Background` | `350208831926335389` | `Image` | `UGUI/UI/Core/Image.cs` `fe87c0e1cc204ed48ad3b37840f39efc` | ✅ 已接 |
| 9 | `&6642834023688078590` | `Icon` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 10 | `&2255017229387309722` | `Tooltip` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 11 | `&3584018649561345357` | `icon` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 12 | `&4921033286217770261` | `Generic UI Button` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 13 | `&9126645122773892354` | `Outline` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 14 | `&5332173404019703811` | `Menu Dark Background` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 15 | `&882447059093349456` | `Button Image` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 16 | `&7482067914071574444` | `end` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 17 | `&6327346895357113438` | `Generic Window Red Background Big` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 18 | `&3144591578283313058` | `Fill` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 19 | `&5381578603297372647` | `Highlight` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 20 | `&8921731404974434903` | `background` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 21 | `&6497821313512136897` | `foreground` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 22 | `&8133219047271483617` | `Generic Close Button Orange` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 23 | `&7813617992064092541` | `Background` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |
| 24 | `&1274698906385194828` | `Icon` | `350208831926335389` | `Image` | 同上 | ✅ 已接 |

**没有一条是「修不了」** —— 两个类的工程 guid 都是现成的（同一批导出的 `Open Window` 里 71 个 `Image` /
122 个 `TMP` **本来就是这么解析的**），只是这一扇窗的组件没被解析。

**`A1118①` 那 134 条的映射**（三路互证，见 §④）：`3485036404935369831` = `Pragati-Regular SDF`（92+6）·
`-8244042478085975641` = `Asar-Regular SDF`（35+1）。

---

## ③ 改动清单

| 文件 | 动作 | 一行说明 |
|---|---|---|
| `Unity/工具/gen_tmp_font_assets.py` | **新增** | `A1118①`：把原版那两份 `TMP_FontAsset` 的 42 个正文字段**逐字段转写**成工程 `.asset`；自带 `_selftest()`（字段集/像素/guid 唯一三条断言）与 `--verify` |
| `Unity/工具/fix_booster_prefabs.py` | **新增** | `A1117` + `A1118①` 的 prefab 收尾：按 pathID→类名/字体名 改写两份 prefab 的 guid-0 引用；**幂等**、`--check` 只报不改、行尾不是纯 LF 就停手 |
| `Unity/MyGame/Assets/CardPresentation/Resources/Fonts/Pragati-Regular SDF.asset` (+`.meta`) | **新增** | 6457 行 · 250 字形 · 250 字符 · `popMode 0`(Static) · 图集 `1024×2048` · 引工程材质 `3592247d…` |
| `Unity/MyGame/Assets/CardPresentation/Resources/Fonts/Asar-Regular SDF.asset` (+`.meta`) | **新增** | 6489 行 · 250 字形 · 250 字符 · `popMode 0`(Static) · 图集 `1024×1024` · 引工程材质 `87856da7…` |
| `Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Info Popup.prefab` | **改**（产物原地） | 24 条 `m_Script` + 7 条 `m_fontAsset`；LF 未翻（3040 行不变）· 该目录是 `.gitignore:184`，git 看不见 |
| `Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab` | **改**（产物原地） | 127 条 `m_fontAsset`；LF 未翻（33412 行不变） |

**没碰**：`BoosterPackExporter.cs` · `EffectExporter.cs` · 任何 `.cs` · 任何别的 `.py` · `d:/2/**` · 两张正本。

---

## ④ 断言（跑过的 / 为什么不自证）

| 断言 | 结果 | 为什么它不是自证 |
|---|---|---|
| 模板字段集 == 头 10 ∪ 原版 JSON 键集（48 == 10 + 42） | ✅ | 拿**本工程 Unity 自己写的** `NotoSerifCJK-Regular SDF.asset` 当模板 —— 期望值**不是**从我的生成器推出来的 |
| 图集「工程那份 vs 原版那份」解码后 RGBA **逐位相同** | ✅（`Pragati 2048×1024` / `Asar 1024×1024`，最大通道差 `[0,0,0,0]`） | 期望值来自 `d:/2/新解包资源/…/Texture2D/*.png`（**原版**），不是我的产物 |
| 派生 guid 与工程既有 10012 个 guid 不冲突 | ✅ | 比的是**别人**的 guid |
| 独立复算：生成物 YAML 解析后与源 JSON 逐字段比（float32 精度 + 空串/引用归一） | ✅ **不等字段数 = 0** | 用 PyYAML **重新解析我写出来的文本**，再跟原版 JSON 比 —— 走的是「产物」不是「生成器内部状态」 |
| 生成器**幂等**（重跑两次产物 sha256 逐字节相同） | ✅ | —— |
| 改写**幂等**（`--check` 第二次跑报 0 条改写） | ✅ | —— |
| 交叉核对：`Image`/`TextMeshProUGUI` 的 guid 必须在**另一扇窗**的 `m_Script` 里出现过 | ✅（71 / 122 次） | 期望值来自**同一批导出的另一份 prefab**，不是这次改的对象 |
| 字体 pathID → 名字：原版 `m_CreationSettings.referencedFontAssetGUID` 当容器键，在 `AssetBundle_1.json` 的 `m_Container` 里 preload 的 pathID 必须含它 | ✅（Pragati `1df326…6c02` → 3 条 · Asar `61505a…4f18` → 3 条） | 用**原版包自己的容器表**定的名，不是我按计数猜的 |
| 行尾：改前是纯 LF 才动手、改后 CRLF 仍 = 0 | ✅ | —— |
| 秒级类型检查 | ✅ 0/0 | —— |

---

## ⑤ 没查清 / 没做到的

1. 🔴 **`A1117` 的修法【不持久】—— 请主对话裁「要不要落进导出器」。**
   `Booster Info Popup.prefab` 是 `BoosterPackExporter.Export()` 里 `PrefabUtility.SaveAsPrefabAsset`
   **每次重导整个重写**的产物 ⇒ **下一次 `BoosterPackExporter.Run` 会把那 24 条打回 guid 0**。
   本会话白名单**不含 `BoosterPackExporter.cs`** ⇒ 我只能在 prefab 产物上治症，并把它做成
   **可重跑的收尾件**：`python -I 工具/fix_booster_prefabs.py`（`--check` 只报不改）。
   ✅ **建议的持久修法**（一行位置，留给能改那个文件的人）：`Export()` 末尾 `SaveAsPrefabAsset`
   **之后**跟一次同样的改写；或在 `StripMissingScripts` 之后把 `m_Script` 的 `SerializedObject`
   显式写成 `AssetDatabase.LoadAssetAtPath<MonoScript>(…)`。
   ⚠️ `m_fontAsset` 那 134 条**不用**跟 —— 导出器走 `FindProjectAsset<TMP_FontAsset>(名字)`，
   `A1118①` 已把名字备好，重导会自己接上（届时「`Pragati-Regular SDF`×92 · `Asar-Regular SDF`×35」那行应从
   `接回 0 / 留空 127` 变成 `接回 127 / 留空 0`）。
2. ⚠️ **`A1117` 的成因**仍**没查清**（第六会话已如实记、本会话也没能查明）：
   **同一源包、同一次导出，`Booster Pack Open Window` 解析了、`Booster Info Popup` 没解析**。
   本会话额外查到的、但仍不足以定论的两条线索：
   · 未解析的那 24 个组件的 YAML 是 `{fileID: <原版 pathID>, guid: 0, type: 0}`，已解析的是
     `{fileID: 11500000, guid: <工程 guid>, type: 3}` —— **形状**上是「原版 MonoScript 对象 vs 工程脚本」；
   · 导出日志 `_tmp_view/booster_export_收口.log` 里**那 7 个 TMP 组件是能被 C# 类型遍历到的**
     （报「TMP 材质 接回 7」）⇒ 内存里它们是真 `TMP_Text`、`m_fontAsset` 也读得出来 —— **只在落盘时丢了**。
   ⇒ 这现象与「脚本没解析」的通常解释**不完全自洽**，需要跑一次 Unity 才判得动（本会话红线不许跑）。
3. ⚠️ **源 `.ttf` 本地没有**：`Pragati` / `Asar` 的字体文件在 `d:/2/新解包资源/` 与 `d:/2/解包整理/`
   全库**零命中**（只有 `NotoSerifCJK-Regular.ttf`）⇒ `m_SourceFontFile` 只能 `{fileID: 0}`，
   `m_SourceFontFileGUID` / `m_CreationSettings.sourceFontFileGUID` **照抄原版那个字符串**
   （解析不到东西，但是原版的真值）。**Static 模式下 TMP 运行时不读源字体** ⇒ 不影响渲染。
4. ⚠️ **`RobotoCondensed-Regular SDF` 我没导** —— `A1118①` 点名的是两份，第三份（`512×512`、304 字形）
   不在本件范围。**同一套工具加一行就能导**（`工具/gen_tmp_font_assets.py` 的 `FONTS` 列表）。
   ⚠️ 它与 `A1097②③` 那份「三份」的口径差一份，**要不要一并导请主对话定**（⛔ 我没顺手做）。
5. ⚠️ **没能验证的**：Unity 是否真的认这两份 `.asset`（**红线不许跑 Unity**）。本会话只能**按文件层面核**
   （YAML 结构 + 字段值 + guid 引用 + 行尾），**这是本报告所有结论的已知上限**。
   最便宜的复核 = 主对话跑一次 `BoosterPackExporter.Run`，看那两行是否变成「接回 127 / 留空 0」。
6. ⚠️ `A1118②`（`BoosterPackExporter` 的 `Animator` 那一跳）**没做** —— 不在本次派单范围。

---

## ⑧ 顺手发现（只报，⛔ 没自己改）

1. 🔴 **`BoosterPackExporter` 那条「脚本未解析」守卫有洞 —— 它自己静默了。**
   实据：`导出报告.tsv` 与日志里 `Booster Info Popup` 都报 **`脚本未解析0`**，而**同一份 prefab 盘上就有 24 条 guid 0**。
   成因（现读 `BoosterPackExporter.cs:709-721`）：`SerializedFieldRef(c,"m_Script") as MonoScript` **返回 null 就 `continue`**
   ⇒ 读不出来的那一格**不计入 n**。**一笔按设计「把静默变成出声」的守卫，自己掉进了同一种静默。**
2. ⚠️ **两扇窗还有 52 条 `m_Sprite` 是 guid 0**（`Open Window` 40 · `Info Popup` 12，共 12 个不同 sprite，
   如 `40K_button` · `Card Frame Cost Icon` · `Card Text smooth background`）—— `A1109` 那支 `ImportSprite` 导不出图的账，**不在本件范围**。
3. ⚠️ **新字体资产引的图集贴图在 `.gitignore` 里**：`.asset` 是入库的，它指的
   `Assets/WarpforgeVFX/Textures/*.png` 因 `.gitignore:26` **不入库**（材质那份也一样）。
   若哪天 `EffectExporter.ClearGenerated()` 清了那棵树，这些引用会**静默变 null**。这是这条链的既有状态，不是本件引入的。
4. ⚠️ **图集贴图的导入设置可能把 SDF 压掉（没查清）**：`*.png.meta` 里 `textureFormat: 1`（Alpha8）但
   **`textureFormatSet: 0`** + `platformSettings[].textureFormat: -1`（= 自动）+ `textureCompression: 1`。
   实测 `WarpforgeVFX/Textures/` 下 **456/458** 张都是这个组合 ⇒ 看不出是「Unity 自动挑成 Alpha8」还是「被压了」。
   ⛔ 没查到判据，如实记（跑一次 Unity 看 `Texture2D.format` 即知）。
