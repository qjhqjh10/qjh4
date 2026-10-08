# `Booster两跳` —— A1109（`BoosterPackExporter.Export()` 缺 uGUI 与 TMP 两跳，**已补**）

> 本件 = **一件**。给 `BoosterPackExporter.Export()` 补上 **uGUI `Image`** 与 **TMP** 两族引用
> 那一跳（`Image.m_Sprite` · `Image.m_Material` · TMP `m_fontAsset` · TMP `m_sharedMaterial`）。
> **本次没跑 Unity**（本轮红线）· **没动 git** · **没改正本** · **没动 `d:/2`**（只读引用）· 行尾二进制数过。
> 🔴 **简报有一处前提与现读不符**（「照 `EffectExporter` 里对应的那两支抄」—— **那两支不存在**），
> 见 §①·3 与 §④。

---

## ① 结论

1. **✅ 已做**：`BoosterPackExporter.cs` 的 `Export()` 里新增 **三支**（新 `:490-649`，**全在 binder 之前**）+
   **三个助手**（`CountUnresolvedScripts` `:709` · `SerializedFieldRef` `:735` · `FindProjectAsset` `:767`）：
   **① uGUI `Image.m_Sprite`（`:512-530`）· ② uGUI `Image.m_Material`（`:532-561`）·
   ③ TMP `m_fontAsset` + `m_sharedMaterial`（`:563-616`）**，外加一条 **「脚本没解析」守卫（`:634-649`）**。
   三支**一律「接得上就接 / 接不上就点名出声 + 留空」**，⛔ **没造任何空 sprite / 空材质 / 空字体**。
2. **实测规模（现读两份 prefab，均纯 LF）**：
   | 文件 | guid 全 0 合计 | 动画 | `m_Sprite` | `m_Material` | TMP `m_fontAsset` | TMP `m_sharedMaterial` | `m_Script` |
   |---|---|---|---|---|---|---|---|
   | `Booster Pack Open Window.prefab`（962823 B） | **350** | 30 | **45** | **21** | **127** | **127** | 0 |
   | `Booster Info Popup.prefab`（90215 B） | **53** | 0 | 13 | 2 | 7 | 7 | **24** |
   | 4 个粒子 prefab（`CardPresentation/Effects/Boosterpack Open Card Rarity 1..4`） | **0** | 0 | 0 | 0 | 0 | 0 | 0 |
   ⇒ 🔴 **简报写「299 条」—— 现读是 `320` 条**（`127+127+45+21`），**其自身枚举的四个数相加也是 320**。
      `350 − 30 = 320` ✔。**以现读为准**。
3. 🔴🔴 **简报的前提不成立（现读推翻）**：简报说「照 `EffectExporter` 里对应的那两支（先现读找出来）」
   —— **`EffectExporter` 里根本没有 uGUI `Image` 与 TMP 这两支**：
   · `grep "GetComponentsInChildren<Image>" | "UnityEngine.UI" | "TextMeshProUGUI" | "fontSharedMaterial" | "TMP_Text"`
     扫**整个 `Assets/`**（`*.cs`）—— 除本件新写的代码外 **命中 0**；
   · `EffectExporter` 的公开助手只有 **5 个**：`ImportSprite` `:2039` · `ImportMaterial` `:1680` ·
     `ImportTexture` `:1930` · `ImportMesh` `:2144` · `ImportClip` `:2364`（`grep "public static" `）——
     **没有 `ImportFont` / `ImportGraphic` 之类**；
   · `EffectExporter.Export` 的「引用接回」只有四跳：**渲染器材质（`:1070-1103`）· 网格（`:1134-1140`）·
     `SpriteRenderer`/`SpriteMask`（`:1148-1151`）· 粒子 TSA（`:1168-1181`）** —— **全是给粒子 prefab 的**
     （另加 `Animator` `:1194-1201` 与 A1095 的 `Animation` `:1203-1268`）。
   ⇒ **本件改按「本函数里已有的那一支」（`SpriteRenderer` / `SpriteMask` 精灵跳，`Export()` 原 `:385-388`）
     同形写**，母版逐行对应见 **§④**（那节里同时写明「母版不存在」）。
4. ✅ **四族里三族今天就能接上**（判据在 §②）：
   · `Image.m_Sprite` **45/45 可接**（7 张精灵原版全定位到了，`ImportSprite` 落成工程 sprite）；
   · `Image.m_Material` **21/21 可接**，但 **5 份原 shader 全不在 `ShaderMap`** ⇒ 会落 `URP/Unlit*`
     **近似替代**（`ImportMaterial` 自己出声，与上面渲染器材质那一跳**同一条既有口径**）；
   · TMP `m_sharedMaterial` **127/127 可接** —— shader 是 `TextMeshPro/Distance Field`，
     `Shader.Find` **找得到**（TMP 随 `com.unity.ugui` 进工程）⇒ **不落近似**。
     🔴 **这条判据不是猜的**：本 prefab **上一次导出的 `导出报告.tsv`** 里
     `window:Booster Pack Open Window` 那行的 `原 shader` 列**已经写着 `TextMeshPro/Distance Field`**，
     且 `Assets/WarpforgeVFX/Materials/` 里**已经有 6 份**同族材质
     （`Asar-Regular White w outline.mat` · `Pragati-Regular Atlas Material.mat` ·
     `Pragati-Regular Atlas Material Thick Outline.mat` · `Pragati-Regular Atlas Material Extra Thick Outline.mat` ·
     `Asar-Regular Atlas Material.mat` · `Pragati-Regular White_thick Offset for 3D.mat`）——
     那是上面「渲染器材质」那一跳为 **5 个 3D `TextMeshPro`**（带 `MeshRenderer`）落下的。
   · 🔴 **TMP `m_fontAsset` 127/127 接不上**：原版那两份字体（`Pragati-Regular SDF` ×92 ·
     `Asar-Regular SDF` ×35）**工程里没有**（全工程 `TMP_FontAsset` 只有 `NotoSerifCJK-Regular SDF` =
     我们自己生成的 CJK 字体、`LiberationSans SDF` = TMP 自带）⇒ **点名出声 + 留空**（记待办，见 §⑦）。
5. ⚠️ **最大的未验证面**：**本轮不跑 Unity ⇒ 这三支一次都没被执行过**（只在编译层面 0 错）。
   逐条列在 §⑥·1。

---

## ② 320 条逐类清单（现读 `Booster Pack Open Window.prefab`）

**怎么扫的**：python 二进制读 → 按 `--- !u!114 &<pid>` 切块 → `m_GameObject` 回查 `--- !u!1` 的 `m_Name`
→ 逐字段抓 `{fileID: …, guid: 00000000000000000000000000000000}`。
**原版资产怎么认出来的**：`fileID` **就是原版包里的 pathID**
（判据来源 = A1101 已经对同族做过一次并逐条命中：`W_Booster动画地雷_A1101.md` §②「判据 A」）——
对 `Material` 走 `assets_full/**/Material/Material_<pid>.json` 的**文件名即 pid**；
对 `Sprite` / `MonoBehaviour`（按 `m_Name` 命名、文件名不含 pid）走 **UnityPy 直接扫原包**
（`env.objects` 的 `path_id`），两条路**互相独立**。

### ②·1 `Image.m_Sprite`（**45 条 / 7 个不同 Sprite**）

| # | `fileID`（= 原版 pathID） | 原版 Sprite（`m_Name`） | 所在原包 | 条数 | 挂在哪些 GO |
|---|---|---|---|---|---|
| 1 | `-3526939114648998153` | `Card Frame Cost Icon` | `duplicateassetisolation_assets_all` | **10** | `Cost Background` ×10 |
| 2 | `-6907984259834462938` | `Card Text smooth background` | `duplicateassetisolation_assets_all` | **10** | `TextBackground Big UI` ×5 · `TextBackground Small UI` ×5 |
| 3 | `-3980338515175932349` | `1_40k_cardframe_rarity_common` | `duplicateassetisolation_assets_all` | **5** | `Card Rarity Sprite` ×5 |
| 4 | `-4138590900053668541` | `40k_Cross_icon_cross_big Banned card` | `duplicateassetisolation_assets_all` | **5** | `Ban Icon` ×5 |
| 5 | `-4840078238721171920` | `pedestal_icon_armor` | `duplicateassetisolation_assets_all` | **5** | `Image` ×5 |
| 6 | `-6381131329509798649` | `WF_Special offer_Value` | **`liveopsmenuimages_assets_all`** ⚠️ | **5** | `New Card Badge` ×5 |
| 7 | `7506635356624546798` | `Card Ready For Level Up` | `duplicateassetisolation_assets_all` | **5** | `Card Ready for level up` ×5 |

**能不能找到？** ✅ **7/7 都找到了**（`#6` 是**唯一的跨包**：`liveopsmenuimages_assets_all`，
**用 UnityPy 扫了 84 个原包才定位到**——前 7 个「像样」的包里都没有它）。
**该指向工程的哪一份资产？** 现在**还没有**；`EffectExporter.ImportSprite` 会把它们落成
`Assets/WarpforgeVFX/Textures/<名字>_sprite.png`（`ImportSprite` `EffectExporter.cs:2064`，
`TexDir` = `Assets/WarpforgeVFX/Textures`）。**7 个目标路径现读全不存在** ⇒ 首次重导是**新建**
（guid 由 Unity 定），之后再导走**原地覆盖**（guid 不变）。
⚠️ **另注（不是冲突，是另一条路）**：其中 **4 张在工程里【另有】一份**
（`CardPresentation/Resources/Art/ui_deck/Card_Frame_Cost_Icon.png` ·
`…/ui_deck/1_40k_cardframe_rarity_common.png` · `…/ui_menu/40k_Cross_icon_cross_big_Banned_card.png` ·
`…/ui_menu/Card_Ready_For_Level_Up.png`）—— 那是 `工具/import_original_art.py` 的产物，**本件不碰它**
（本 `Export()` 明写「不碰 `Resources/`」，见文件头 `:43-44`）。

### ②·2 `Image.m_Material`（**21 条 / 5 个不同 Material**）

| # | `fileID` | 原版 Material | 原 shader | 原包 | 条数 | 挂在哪些 GO |
|---|---|---|---|---|---|---|
| 1 | `-8301844802436097929` | `Card Image Overflow` | `Everguild/Card ImageUI` | `duplicateassetisolation` | **5** | `CardImage` ×5 |
| 2 | `-3316280387615011577` | `Card Frame SDF` | `Everguild/FX/Card Highlight And Shadow` | `duplicateassetisolation` | **5** | `Card Highlight And Shadow` ×5 |
| 3 | `6996605638394099752` | `Card Backs SDF` | `Everguild/FX/Card Highlight And Shadow` | `duplicateassetisolation` | **5** | `Cardback Shadow SDF` ×5 |
| 4 | `7594506706299407200` | `UI Card Ready For Level Up` | `Everguild/UI/Card Ready for level up` | `duplicateassetisolation` | **5** | `Card Ready for level up` ×5 |
| 5 | `-1341631206307795618` | `Booster Pack Open Menu Background` | `Shader Graphs/Nebula` | `menus_assets_all` | **1** | `Booster pack Background` ×1 |

**能不能找到？** ✅ **5/5 都找到了**（shader 名是顺着 `m_Shader.m_PathID` 反查到
`bundle_shaders_assets_all/Shader/Shader_<pid>.json` 的 `m_ParsedForm.m_Name`）。
**该指向工程的哪一份资产？** **4 份 shader 不在 `ShaderMap`**（`EffectExporter.cs:63-137`，**38 条**里
既没有 `Everguild/Card ImageUI`，也没有 `Everguild/FX/Card Highlight And Shadow` /
`Everguild/UI/Card Ready for level up` / `Shader Graphs/Nebula`），`Shader.Find` 也拿不到
（它们是原版私有 shader，不在工程）⇒ 会落到 `URP/Unlit*` 的**近似替代**（`approx = true`，出声）。
目标路径 `Assets/WarpforgeVFX/Materials/<名字>.mat` **现读 5 个全不存在**。

### ②·3 TMP `m_fontAsset`（**127 条 / 2 个不同 `TMP_FontAsset`**）

| # | `fileID` | 原版 `TMP_FontAsset` | 原包 | 条数 | 挂在哪些 GO |
|---|---|---|---|---|---|
| 1 | `3485036404935369831` | `Pragati-Regular SDF` | `fonts_assets_all` | **92** | `RaceText` ×10 · `CostText` ×10 · `DescTacticSmall` ×5 · `Armour Text` ×5 · `ArmyTextTactc` ×5 · `DescTextTactic Big` ×5 · `RaceText Big` ×5 · `Melee Attack Text` ×5 · `Created By Text` ×5 · `DescTextTactic` ×5 · `HealthText` ×5 · `DescTextUnit Big` ×5 · `Army No Desciption` ×5 · `Range Attack Text` ×5 · `ArmyTextUnit` ×5 · `DescTextUnit` ×5 · `Tap to discover` ×1 · `Tap to close` ×1 |
| 2 | `-8244042478085975641` | `Asar-Regular SDF` | `fonts_assets_all` | **35** | `CreatedByText` ×5 · `NameTextTactic` ×5 · `NameTextUnit` ×5 · `NameTextTacticBig` ×5 · `NameTextUnit No description` ×5 · `NameTextUnit Big` ×5 · `NameTextUnit No Description Big` ×5 |

**能不能找到？** ✅ **原版那两份定位到了**（`bundle_fonts_assets_all/MonoBehaviour/`，
`m_ClassName` = `TMP_FontAsset` 的 `m_Script`）。
🔴 **工程里没有** —— 全工程 `*.asset` 里带 `TMP_FontAsset` 的只有
`CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset`），`grep -ril "pragati|asar"` 扫全
`Assets/` 只命中 `.cs` 与 **1 张 png** ⇒ **这两份字体从没被导入过**（卡面数字走
`Core/PragatiDigits` 的**位图表**，源 = `工具/gen_pragati_digits.py`；菜单/HUD 走我们那份 CJK 字体）。
⇒ **本件留空 + 出声**，写成待办（§⑦）。

### ②·4 TMP `m_sharedMaterial`（**127 条 / 7 个不同 Material**）

**7 份全在 `bundle_fonts_assets_all`，shader 全是 `TextMeshPro/Distance Field`**（现读 `m_Shader.m_PathID` →
`Shader_683148379888368288.json` 的 `m_ParsedForm.m_Name`）。

| # | `fileID` | 原版 Material | 条数 | 挂在哪些 GO |
|---|---|---|---|---|
| 1 | `-6250168224648873853` | `Pragati-Regular Card Content` | **40** | `RaceText` ×10 · `DescTacticSmall` ×5 · `DescTextTactic Big` ×5 · `RaceText Big` ×5 · `DescTextTactic` ×5 · `DescTextUnit Big` ×5 · `DescTextUnit` ×5 |
| 2 | `-6876116930658831071` | `Asar-Regular White-Card name` | **30** | `NameTextTactic` ×5 · `NameTextUnit` ×5 · `NameTextTacticBig` ×5 · `NameTextUnit No description` ×5 · `NameTextUnit Big` ×5 · `NameTextUnit No Description Big` ×5 |
| 3 | `-8115038251467144366` | `Pragati-Regular Atlas Material Thin Outline` | **20** | `Melee Attack Text` ×5 · `Created By Text` ×5 · `HealthText` ×5 · `Range Attack Text` ×5 |
| 4 | `-455814643003338116` | `Pragati-Regular Atlas Material Thick Outline` | **15** | `CostText` ×10 · `Armour Text` ×5 |
| 5 | `3931739351530146963` | `Pragati-Regular Card Unit type` | **15** | `ArmyTextTactc` ×5 · `Army No Desciption` ×5 · `ArmyTextUnit` ×5 |
| 6 | `6389513473928706012` | `Asar-Regular White w outline` | **5** | `CreatedByText` ×5 |
| 7 | `-5050103597772954521` | `Pragati-Regular Atlas Material` | **2** | `Tap to discover` ×1 · `Tap to close` ×1 |

**能不能找到？** ✅ **7/7 都找到了**，**而且导得出来**（shader 工程里有，见 §①·4）。
**该指向工程的哪一份资产？** `Assets/WarpforgeVFX/Materials/<名字>.mat`：
其中 **`Asar-Regular White w outline.mat` 与 `Pragati-Regular Atlas Material.mat` 现读【已存在】**
（上面「渲染器材质」那一跳为 3D `TextMeshPro` 落的）⇒ 走 `ImportMaterial` 的**原地覆盖**（guid 不变）；
**其余 5 份现读不存在** ⇒ 首次重导新建。
⚠️ `Assets/WarpforgeVFX/Materials/` 里另有 `Asar-Regular White w outline 1.mat` / `2.mat`
（**2026-10-16 改成「确定路径」之前**那套 `GenerateUniqueAssetPath` 留下的老副本）——
本件不动它们；新导走确定路径会覆盖 `Asar-Regular White w outline.mat`，**撞名由 `GuardMatName` 出声**。

### ②·5 汇总：重导后**各族的期望余数**（收口判据的期望值）

| 族 | `Booster Pack Open Window` 改前 | 期望改后 | `Booster Info Popup` 改前 | 期望改后 |
|---|---|---|---|---|
| 动画（A1101） | 30 | **0** | 0 | 0 |
| `Image.m_Sprite` | 45 | **0** | 13 | **0** ⚠️ |
| `Image.m_Material` | 21 | **0** | 2 | **0** ⚠️ |
| TMP `m_fontAsset` | 127 | **127**（工程无该资产 ⇒ 按设计留空） | 7 | **7** |
| TMP `m_sharedMaterial` | 127 | **0** | 7 | **0** ⚠️ |
| `m_Script`（本件**不修**） | 0 | 0 | 24 | **24** |
| **guid 全 0 合计** | **350** | **127** ⚠️ | **53** | **31** ⚠️ |

⚠️ **Info Popup 那三行带 ⚠️ 的「期望 0」有一个前提**：那 17 个 `Image` + 7 个 TMP 组件
**在内存里得是真正的 `Image` / `TMP_Text` 类型**。它们保存出来的 `m_Script` 是 guid 0（§⑦·2），
**类型可能压根没解析**（那它们在内存里就是普通 `MonoBehaviour`，本件按类型遍历的三支
**一条都找不到**）。⇒ 本件加了**守卫**把这种情况**数出来并点名**（`:634-649`），
⛔ 不让它静默。**这一点本轮无法验证**（不跑 Unity），见 §⑥·1 第 4 条。

---

## ③ 改动清单

**只改了 1 个文件：`Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs`**

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:56-68` | （无） | 文件头新增一节「🆕 2026-10-09（A1109）uGUI 与 TMP 那两跳」（**13 行**）：漏在哪 / 规模 / 与 A1101 的关系 / **简报那条前提不成立** / 指针到本报告 |
| `:490-511` | （无） | 三支的**头注**（22 行）：根因 · 两份 prefab 的逐族数 · **「母版不存在」的现读证据** · 零幻觉兜底 · 指针 |
| `:512-530` | （无） | **支 ① uGUI `Image.m_Sprite`**（19 行） |
| `:532-561` | （无） | **支 ② uGUI `Image.m_Material`**（30 行） |
| `:563-616` | （无） | **支 ③ TMP `m_fontAsset` + `m_sharedMaterial`**（54 行） |
| `:618-632` | （无） | 三支的**汇总日志**（15 行） |
| `:634-649` | （无） | **「脚本没解析」守卫**（16 行，**只报不改**） |
| `:658-659` | `return $"材质定义…`+1 行 | **追加 1 行**记账（`uGUI 图{}/截{} · uGUI 材{}/截{} · TMP 字{}/截{} · TMP 材{}/截{} · 脚本未解析{}`） |
| `:655-666` | `binder.animatorController = "";` | **加 12 行注释**说明「没有 `Animator` 那一支」这条形状差 + 今天无影响的实测 + 为什么**没顺手补**（**行为一字未改**，见 §⑦·3） |
| `:709-723` | （无） | 新助手 `CountUnresolvedScripts`（15 行） |
| `:735-753` | （无） | 新助手 `SerializedFieldRef`（19 行） |
| `:764-787` | （无） | 新助手 `FindProjectAsset<T>` + 它的缓存（24 行） |
| （原 `:476` 起） | `var binder = inst.GetComponent<WarpforgeEffectBinder>();` | 被推下去到 **`:651`**（**只新增、没改写内容** —— 全文件唯一的删改是上面那条 `return`） |

**净改动**：**`+365 / -1` / 1 个文件**，其中 **A1101 那件占 `+83/0`**（已提交，`git diff f7ef415` 实测），
⇒ **本件 A1109 = `+282 / -1`**。
⚠️ **另注（时间线 —— 2026-10-09 收尾时现核，与本节初稿写的不一样，就地订正）**：
本件动手期间**主对话提交了两次**，本件的代码**已经全部入库**：
· `d6c4111`（第六会话收尾）**首次落进本件的代码**（当时 = 前半，**764 行**）；
· `8d6302d`（`项目任务.md` 搬节）顺带**收进了本件的后半**（该提交对本文件 `98` 行改动）
  ⇒ **`HEAD` 那一版 = 本件的最终版（826 行）**。
✅ **现核 `git diff`（工作区 vs `HEAD`）= 空** ⇒ 盘上的 `BoosterPackExporter.cs` 已全部提交，
**下次看到 `git status` 里没有它，不能推出「本件没做」**。
（🔴 本节初稿写的是「HEAD 不是本件最终版」—— **写的时候成立、收尾时不成立了**，已按现核改掉。）

**没动**：`EffectExporter.cs`（**全程只读**，一行未改 —— 也就**没有**触及「复用它的公开口」那条例外）·
`WarpforgeVFX/**` · `WarpforgeBooster/**`（**本轮一个产物都没落**，两次导出的都是源码）·
`工具/scripts快照/gen_unity_arena_manifest.py` · `RuleEngine/**` · `Shell/` · `Net/` · `Battle/` · `Core/` ·
`.gitignore` · `项目任务.md` / `CLAUDE.md` · 别的任何文件 · `d:/2/`。

---

## ④ 证据：**与 `EffectExporter` 的逐行对应**（母版不存在，按本文件已有的支同形）

### ④·0 🔴 先说清楚：**简报指的那两支母版不存在**（现读，三条独立证据）

| 找什么 | 怎么找的 | 结果 |
|---|---|---|
| uGUI `Image` 那一支 | `grep -rn "GetComponentsInChildren<Image>" --include=*.cs Assets/` | **命中 0** |
| TMP 那一支 | `grep -rn "TextMeshProUGUI\|TMP_Text\|fontSharedMaterial" --include=*.cs Assets/` | **命中 0**（只有 `TMP_FontAsset` 出现在 `TmpSetup.cs` / `TmpFont.cs` 等**建字体/取字体**的地方，不是导出器） |
| `EffectExporter` 的公开导入助手 | `grep -n "public static .*Import" EffectExporter.cs` | 只有 **5 个**：`ImportSprite:2039` · `ImportMaterial:1680` · `ImportTexture:1930` · `ImportMesh:2144` · `ImportClip:2364` —— **没有字体/Graphic 那两个** |
| `EffectExporter.Export` 的「引用接回」跳数 | 读 `:1080-1201` | **4 跳**：渲染器材质 · 网格 · `SpriteRenderer`/`SpriteMask` · 粒子 TSA（+ `Animator`、+ A1095 的 `Animation`）—— **全是给粒子 prefab 的** |

⇒ 简报那句「照 `EffectExporter` 里对应的那两支抄」**在现读里没有对应物**；
本件按**本文件 `Export()` 里已有的那两支**（渲染器材质跳 + 精灵跳）**同形**写。

### ④·1 支 ①（uGUI `m_Sprite`）↔ 本文件已有的**精灵跳**（原 `:385-388`）

| 母版（本文件原 `:385-388`） | 本件（`:512-530`） | 说明 |
|---|---|---|
| `foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))` | `foreach (var img in inst.GetComponentsInChildren<UnityEngine.UI.Image>(true))` | 同一个「遍历 + `true`（含未激活）」形状；只换类型 |
| `if (sr.sprite != null) sr.sprite = EffectExporter.ImportSprite(sr.sprite);` | `if (img.sprite == null) continue; var imported = EffectExporter.ImportSprite(img.sprite); … img.sprite = imported;` | **同一个 `ImportSprite`**；本件**多了一层空值分支**（原版本来就空的那种不算丢） |
| （母版没有：`null` 时**静默不动**） | `if (imported == null) { uiSpriteMiss++; Debug.LogWarning(…) ; continue; }` | 🔴 **本件比母版严**：母版那句 `sr.sprite = null` 是**静默**的（赋 null 与不改动没有任何区别、也不出声）—— 本件照 A1101 那条纪律补上**出声** |
| 另两支同族：`SpriteMask`（`:387-388`）· 粒子 TSA（`:390-401`） | —— | 本件不需要：普查里 `m_Sprite` 的持有者**全是 `Image`**，`RawImage.m_Texture` 的 guid-0 **实测 0 处** |

### ④·2 支 ②（uGUI `m_Material`）↔ 本文件已有的**渲染器材质跳**（原 `:348-378`）

| 母版（原 `:348-378`） | 本件（`:532-561`） | 说明 |
|---|---|---|
| `var mats = r.sharedMaterials; … var nm = EffectExporter.ImportMaterial(om, out bool wasApprox, out string origShader);` | `var nm = EffectExporter.ImportMaterial(om, out bool uiApprox, out string uiShader);` | **同一个 `ImportMaterial`**，同样的 `out approx / out origShader` 两个出参 |
| `if (nm != null) { mats[i] = nm; … usedShaders.Add(origShader); if (wasApprox) approx++; }` | `if (nm == null) { uiMatMiss++; 出声; continue; } img.material = nm; uiMatOk++; if (uiApprox) 出声;` | 同一条「导得出就写、导不出就**出声**」；近似替代的记账方式一致（母版汇总进报告、本件即时出声） |
| `var om = mats[i]; if (om == null) { slots.Add(-1); continue; }` | `var om = SerializedFieldRef(img, "m_Material") as Material; if (om == null) continue;` | **唯一一处形状不同、而且是有意的**：见 ④·4 |

### ④·3 支 ③（TMP）↔ A1101 那支 + 渲染器材质跳

| 母版（本文件 `:437-474` = A1101 的 `Animation` 支） | 本件（`:563-616`） | 说明 |
|---|---|---|
| `foreach (var an in inst.GetComponentsInChildren<Animation>(true))` | `foreach (var t in inst.GetComponentsInChildren<TMPro.TMP_Text>(true))` | 同形（`TMP_Text` 覆盖 `TextMeshProUGUI` 122 个 + `TextMeshPro` 5 个） |
| `var a = EffectExporter.ImportClip(clips[i]); if (a == null) { clipMiss++; 出声; continue; } imported[i] = a; clipOk++;` | `var pm = EffectExporter.ImportMaterial(srcMat, out bool tmpApprox, out string tmpShader); if (pm == null) { tmpMatMiss++; 出声; continue; } t.fontSharedMaterial = pm; tmpMatOk++;` | 同一条「导不出就**留空 + 出声**」；`ImportClip` ↔ `ImportMaterial` 是同一族助手 |
| `if (def != null) { var d = ImportClip(def); if (d != null) an.clip = d; }`（**两条出口都写**） | `if (srcFont != null) { … t.font = f; }` 与材质的写口**各自独立**（**两条出口都写**） | 同一原则：`m_Animations[]` + `m_Animation` ↔ `m_fontAsset` + `m_sharedMaterial`，**缺一不可** |
| `⛔ 零幻觉兜底：接不上就留空 + 出声，绝不 CreateAsset 空 clip` | `⛔ 同上：TMP 字体接不上就留空 + 出声，绝不 CreateFontAsset 现造一份` | **逐字同一条纪律** |
| `if (clipOk > 0 \|\| clipMiss > 0) Debug.Log(…)` | `if (tmpFontOk > 0 \|\| tmpFontMiss > 0 \|\| tmpMatOk > 0 \|\| tmpMatMiss > 0) Debug.Log(…)` | 同一汇总格式（**缺字体的还按名字去重列出条数**，见下） |

**TMP 字体那条**用了**新助手 `FindProjectAsset<TMP_FontAsset>(name)`**（`:767`）——
**工程里没有母版，所以形状由本件定**（**这是本件唯一一处「自己定口径」**，如实标出）：
按**资产名**找同名工程 `TMP_FontAsset`（`AssetDatabase.FindAssets("t:TMP_FontAsset")` 逐条比文件名），
**找不到就 null（留空）+ 出声**。理由：`m_fontAsset` 的引用在包里是 guid 0，运行时拿到的是
**包里的对象**（不是工程资产），**除了「工程里已经有一份同名的」这条，没有第二条接回来的路**。

### ④·4 🔴 三处「有意的不一样」（逐条给理由）

1. **`m_Material` / `m_fontAsset` / `m_sharedMaterial` 一律走 `SerializedFieldRef`（按序列化字段名读），
   不用组件的公开属性**：
   · `Image.material` 是个**带回落**的 getter（`m_Material == null` ⇒ 返回**内建默认材质**；
     带 alpha 分离贴图的精灵还返回 `defaultETC1GraphicMaterial`）⇒ 照它取值会把**本来没有材质**的
     那 50 个 `Image` 也算成「有」，然后给它们各写一份工程副本 —— **`m_Material` 由空变非空 = 静默改原始数据**。
   · TMP 的 `font` / `fontSharedMaterial` 本身不回落，但**也走同一处**，为的是
     **与普查用的是同一批字段名**（`m_fontAsset` / `m_sharedMaterial`）—— 一处口径，别两套。
   · ⚠️ **`Image.sprite` 那一支没用它**：`Image.sprite` 的 getter **是精确的**（只返回 `m_Sprite`），
     所以照母版直用，不必绕。
2. **只覆盖 `UnityEngine.UI.Image`，不遍历 `Graphic`**：普查里 `m_Sprite` / `m_Material` 的持有者
   **全是 `Image`**；遍历 `Graphic` 会把 `TMP_Text`（也是 `Graphic`）一起捞进来，而它的
   `material` 语义是 `m_sharedMaterial`（已由支 ③ 管）⇒ **会写重、而且写错口**。
3. **TMP 的字体与材质「各自独立地接」**：原打算「字体接不上就不接材质（成对）」——
   **现读推翻了它**：`ImportMaterial` 在这一族上**本来就接得上**（§①·4），
   而且安全性查过源码：`TMP_Text.fontSharedMaterial` setter → `SetSharedMaterial`
   （`TextMeshProUGUI.cs:1529` / `TextMeshPro.cs:1233`）只写 `m_sharedMaterial` +
   `GetPaddingForMaterial()`，后者对 `m_fontAsset == null` **没有依赖**（`TMP_Text.cs:1823-1834`）。
   ⇒ 按铁律 11「不因困难回避、能接就接」，材质那一跳**照接**。

### ④·5 新增的那一条**守卫**（`:634-649` + `CountUnresolvedScripts` `:709`）

这不是「照母版抄」，是**本件补的一条防静默**（理由在 §②·5 的 ⚠️ 与 §⑦·2）：
本 `Export()` 的几支**一律按 C# 类型遍历**，而**脚本没解析**的组件在内存里是普通 `MonoBehaviour`
⇒ 那几支**一条都找不到、计数器是 0、日志也不打**。守卫数出这种组件并点名。
⛔ **只报不改**（怎么把包里的 `MonoScript` 映射回工程脚本是另一件事）。

---

## ⑤ 验证

### 5·1 秒级类型检查（`TMPDIR=/tmp/wf_boost2 bash d:/4/Unity/工具/typecheck.sh`）

| 次 | 时机 | 结果 |
|---|---|---|
| 1 | 三支 + 三个助手写完 | **运行时 0 / 编辑器 0** |
| 2 | TMP 材质改走 `ImportMaterial` + `FindProjectAsset` 注释订正后 | **运行时 0 / 编辑器 0** |
| 3 | 守卫 + `CountUnresolvedScripts` 写完（最终） | **运行时 0 / 编辑器 0** |

✅ **另核「我的文件真被编进去了没有」**：`/tmp/wf_boost2/wf_csc_editor.rsp` 里**有**
`"D:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs"` ⇒ **不是「没编所以 0 错」**。
✅ 三次都**没有出现**「错误全在别人的文件上」那种情形（本次零错误，无需辨别归属）。

### 5·2 行尾（二进制读；⛔ 没用 `sed -i`、⛔ 没用 python 文本模式写）

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF | 改后字节 | md5 |
|---|---|---|---|---|
| `BoosterPackExporter.cs` | **0 / 545**（纯 LF，与 A1101 记的一致） | **0 / 826**（**LF 未翻**） | 34046 → **54727** | `327ddadc90632dae8450e1ad7afd3f50` |

**`git diff`**（收尾时现核）：`git diff f7ef415 HEAD --numstat` = **`365  1`**
（= A1101 那件 `83 0` + 本件 **`282 1`**）· `git diff HEAD`（工作区 vs HEAD）= **空**
（本件已全部提交 —— 时间线见 §③ 那条订正后的注）。
**唯一那条 `-` 是 `Export()` 末尾那行 `return`**（要加记账），**没有全文件重写**（+282 不是 545）。
**结构自检**：`{` = `}` = **189**；`GetComponentsInChildren<UnityEngine.UI.Image>` 出现 **2** 次、
`<TMPro.TMP_Text>` **1** 次、`var binder = …` **1** 次、`binder.animatorController = ""` **1** 次。

### 5·3 ⚠️ **另列被 ignore 的产物**（本件**一个都没落**，下表是**现有**状态 + 判据）

`git check-ignore -v` 逐条核过：

| 路径 | 命中哪条 ignore | 说明 |
|---|---|---|
| `Assets/WarpforgeBooster/`（含 `Prefabs/Booster Pack Open Window.prefab` · `Booster Info Popup.prefab` · `导出报告.tsv`） | **`.gitignore:184: /Unity/MyGame/Assets/WarpforgeBooster/`** | 🔴 **整棵目录**。⇒ **本件的验收对象（两份 prefab）在 `git status` / `git diff` 里根本看不见** —— 收口必须**直接看文件**（mtime / 字节 / md5 / 数 guid） |
| `Assets/WarpforgeVFX/*`（含 `Textures/<…>_sprite.png` · `Materials/<…>.mat` · `Animations/*.anim`） | **`.gitignore:26: /Unity/MyGame/Assets/WarpforgeVFX/*`** | 🔴 **本件两支的【产物】也全在这里** ⇒ 重导之后新增的 sprite / 材质**同样进不了 `git status`** |
| `Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs` | **不在任何 ignore 里** | ✅ 本件**唯一的入库改动**（**已提交进 HEAD**，见 §③ 的时间线注）；`git status` 里现在**没有它**（= 已提交，**不是**「没改」） |

> 🔴 **收口提醒**：`WarpforgeVFX/Textures/*_sprite.png` 与 `Materials/*.mat` 都是 `.gitignore:26` 挡着的
> **构建产物** —— 与 `Resources/Art/**` 同一族（见 `资料/已知的坑.md` 那类「素材腿一跑、git 一声不响」的教训）。
> **判据永远看盘上的文件，别看 git。**

---

## ⑥ 没查清 / 停手的

### 1. 🔴 **本件的新代码一次都没被执行过**（最大的未验证面）

本轮不跑 Unity ⇒ 三支 + 守卫只在**编译**层面验过（0 错）。四个**只有跑起来才知道**的点：

1. **`Image.sprite` / TMP 的 `m_fontAsset`·`m_sharedMaterial` 在实况里真取得到吗？**
   旁证（**不是猜**）：现读这两格写的是**原版真 pathID**（见 §②，320/320 全对得上原版资产），
   而**同一份文件**里「空引用」写的是 `{fileID: 0}`（`m_Animation: {fileID: 0}` 在 Open Window 里是 **0 条**）
   ⇒ 上一次导出那一刻这些引用是**活的**。**但我没在实况里量过。**
2. **7 张精灵能不能过 `ImportSprite`？** 它要 `s.texture` 可读或能 blit（`ReadableCopy` `:2098`），
   且 `m_SpriteAtlas` 打包的那些走 `textureRect` 裁切（`:2047-2057`）。**这 7 张我没逐张量过是不是图集精灵。**
   若某张落不下来 ⇒ 会**出声 + 留空**（本件已写），不会静默。
3. **5 份 uGUI 材质过 `ImportMaterial` 会落成什么样**：原 shader 全不在 `ShaderMap`
   ⇒ 落 `URP/Unlit*` 且 `approx = true`（出声）。**具体丢多少属性**要看 `ImportMaterial` 自己那条
   「认不出 N 个属性」的告警 —— **本轮跑不出这条日志**。
4. 🔴 **`Booster Info Popup` 那 17 个 `Image` + 7 个 TMP 在内存里到底是不是真类型？**（§②·5 的 ⚠️）
   **决定性判据 = 重导时看守卫那条告警**：
   · 若报 `有 24 个组件的 m_Script 指不到工程脚本` 且类型名是 **`MonoBehaviour`** ⇒ 它们是**普通 MB**，
     本件三支对那扇窗**空转**（守卫已出声，**不是静默**）；
   · 若类型名是 **`Image` / `TextMeshProUGUI`** ⇒ 三支照常命中（那 24 条 `m_Script` 是另一回事）。
   **本轮查不到**（`AssetDatabase` 之外的静态证据到此为止：源包里两扇窗的 `m_Script` 都是
   `{m_FileID: 1, m_PathID: <builtin 脚本 pid>}`（现读 `assets_full/…/menus_assets_all/MonoBehaviour/`：
   `Image` 6835 个 · `TMP` 4190 个，`m_FileID` **全是 1**）—— **同一个源包，保存出来却一个解析了、一个没解析，
   成因没查清**，如实记为「**没查清**」）。
5. **`FindProjectAsset` 这条新口径本身没被执行过**（今天必然命中 0）——
   它是「将来把原版那两份字体导进来就能自动接上」的那条路。**未验证。**

### 2. 没查的

- **`Booster Pack Open Window.prefab` 的 `m_Animation`（30 条）**：A1101 已修，**本件没碰、没复核**。
- **那 4 个粒子 prefab**：现读 `guid 0 = 0`（干净）⇒ 本件三支对它们**是空转**（0 次迭代），正确。
- **`Booster Info Popup.prefab` 的 24 条 `m_Script` guid 0**：**本件不修**（不在范围内，见 §⑦·2）。
- **`Booster Pack Open Window.prefab` 上次导出报告里那 2 个材质定义 / 6 个渲染器槽**：
  我只用它当「`ImportMaterial` 在 TMP 材质上通」的旁证，**没逐槽复核**。

### 3. 停手的地方

**没有停手**。三支都写完并过了编译。唯一「本可以补但选择不补」的是 **`Animator` 那一支**，
理由写进 §⑦·3（**不是「成本高」，是「`EffectExporter.ImportAnimatorController` 是私有、
复用要动别人的已交件，而自己再写一份违‘两处写同一条规则’；且今天判据是 0」**）。

---

## ⑦ 顺手发现的

### 1. 🔴 **`Assets/WarpforgeBooster/` 整棵目录也是 ignore 的**（`.gitignore:184`）—— 已核实

简报里那条已知（`git check-ignore -v` 实测）✔。**本件的验收对象（两份 prefab）在 `git status` 里看不见**。
**另加一条简报没提的**：**本件两支的产物**（`WarpforgeVFX/Textures/*_sprite.png` ·
`WarpforgeVFX/Materials/*.mat`）落在 **`.gitignore:26`** 里，**同样看不见** ⇒ 收口只能看盘（§5·3）。

### 2. 🔴🔴 **`Booster Info Popup.prefab` 有 24 个组件的 `m_Script` 是 guid 全 0**（**本件没修**）

现读分组：**17 × `350208831926335389`** + **7 × `7477354737935883349`**。
两个 pathID 我**先按仓内既有记录**认，再去 **`assets_full/bundle_Waprforge_monoscripts/MonoScript/` 直接反查**
（**独立复核，两条都对上**）：

| pathID | `MonoScript_<pid>.json` 的 `m_Name` | `m_ClassName` | `m_AssemblyName` | 仓内既有记录（旁证） |
|---|---|---|---|---|
| `350208831926335389` | **`Image`** | `Image` | `UnityEngine.UI` | `资料/已知的坑.md:1900`（原文「反查 … ⇒ `m_Name: "Image"`」）· `Battle/BattleLogPanel.cs:177` · `资料/普查产出_1005/调度台_裁定与复核_1005b.md:74` |
| `7477354737935883349` | **`TextMeshProUGUI`** | `TextMeshProUGUI` | `Unity.TextMeshPro` | `资料/普查产出_1018/REV_W1_词条化.md:252`（「`7477354737935883349`（TMP）」）· `资料/战斗规格/战斗重建_0827/子代理读报_back右区_0827.md:123` |

⇒ **那 24 个组件重导之后仍然是「Missing (Mono Script)」**：
**即便本件把 `m_Sprite` / `m_fontAsset` / `m_sharedMaterial` 都接上了，`Booster Info Popup` 这扇窗
仍然画不出来**（脚本没了，字段值再对也没人读）。
🔴 **这是一族本件没修的缺陷**，修法要另立：得把包里的 `MonoScript` **重新映射回工程脚本**
（`UnityEngine.UI.Image` / `Unity.TextMeshPro.TextMeshProUGUI` 工程里都有）。
**为什么两扇窗不一样**（同源包、同一次导出，一个解析了一个没解析）—— **没查清**，如实记（§⑥·1·4）。
⚠️ 本件为此加了**守卫**（`:634-649`）：重导时会**点名报出**这一族，⛔ 不会静默。

### 3. ⚠️ **`binder.animatorController = ""` 硬写死 + `Export()` 没有 `Animator` 那一支**

**现读实测「今天无影响」**：两扇窗 + 那 4 个粒子 prefab 的 `--- !u!95`（`Animator`）**全 = 0**（§①·2 的表）。
🔴 **但将来把带 `Animator` 的根加进 `WindowRoots` / `CardFxRoots`**，那条控制器会**静默落成 guid 全 0**
（`EffectExporter` 那边是 `:1194-1201` 接控制器 + `:1280` 记 `origController` 给 binder；本函数没有）。
**选择：如实标注、不补代码**（**不是「影响小不做」**，理由是可操作性）：
`EffectExporter.ImportAnimatorController` 是 **`static` 私有**（`EffectExporter.cs:2414`）——
复用就得**动别人的已交件**（白名单里 `EffectExporter` 是「**只读为主**」），自己再写一份则违
`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」。
⇒ **已把那 12 行说明写进代码**（`:655-666`），**行为一字未改**；待办写法见 §⑧ 末。

### 4. **4 个粒子 prefab 的 guid-0 干净**（各 0 条）—— 与 A1101 的结论一致，**本件复核过**。

### 5. `Booster Pack Open Window.prefab` 里 **`!u!198`/`!u!199`（粒子）各 1 个**，
而 `导出报告.tsv` 记着它有 **2 个材质定义 / 6 个渲染器槽**（含 `TextMeshPro/Distance Field` +
`URP/Particles/Unlit`）—— 那是 **5 个 3D `TextMeshPro`（`!u!23 MeshRenderer` = 5）**贡献的
（不是粒子）。**顺带说明**：3D `TextMeshPro` 的材质走**渲染器材质跳**已经接过了，
**只有 UGUI 那 127 个 TMP 的 `m_sharedMaterial` 一直是漏的** —— 这正是本件支 ③ 要补的那一半。

---

## ⑧ 收口判据

**重导命令**（与文件头 `:31-41` 那三步同；**本次没跑**）：
```bash
python d:/4/Unity/工具/extract_missing_shaders.py --prefabs      # ① 重打小包（若小包被清过才需要）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BoosterPackExporter.Run -logFile "d:/4/_tmp_view/booster_export.log"
grep "^BP " d:/4/_tmp_view/booster_export.log
```

**日志里新增的判据行**（本件加的；期望值见 §②·5）：
```
BP A1109 `Booster Pack Open Window` uGUI `Image.m_Sprite`：接回 **45** 条
BP A1109 `Booster Pack Open Window` uGUI `Image.m_Material`：接回 **21** 条
BP A1109 `Booster Pack Open Window` TMP：字体 接回 **0** / 留空 **127** · 材质 接回 **127** / 留空 **0**（工程里没有这些 TMP_FontAsset：`Pragati-Regular SDF`×92 · `Asar-Regular SDF`×35）
BP A1109 `Booster Info Popup` uGUI `Image.m_Sprite`：接回 **13** 条      ← ⚠️ 见 §⑥·1·4（也可能是 0 条 + 守卫告警）
BP A1109 `Booster Info Popup` uGUI `Image.m_Material`：接回 **2** 条
BP A1109 `Booster Info Popup` TMP：字体 接回 **0** / 留空 **7** · 材质 接回 **7** / 留空 **0**（…）
BP A1109 `Booster Info Popup` 有 **24** 个组件的 `m_Script` 指不到工程脚本（保存后会是 Missing Script）…
```

### 判据 1（**主判据**：按字段名分组数 guid 全 0）

```bash
python -I -c "
import io,re,collections
for p in [r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab',
          r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Info Popup.prefab']:
    s=io.open(p,'rb').read().decode('utf-8')
    c=collections.Counter()
    for m in re.finditer(r'\{fileID: -?\d+, guid: 00000000000000000000000000000000, type: -?\d+\}',s):
        st=s.rfind(chr(10),0,m.start()); g=re.match(r'\s*([A-Za-z_][A-Za-z0-9_]*)',s[st+1:])
        c[g.group(1) if g else '(array item)']+=1
    print(p.split('/')[-1],'总',sum(c.values()),dict(c))
"
```
**改前实测基线**（现读，供对比）：

| 文件 | 总 | `m_Animation` | 数组项 | `m_Sprite` | `m_Material` | `m_fontAsset` | `m_sharedMaterial` | `m_Script` |
|---|---|---|---|---|---|---|---|---|
| `Booster Pack Open Window.prefab` | **350** | 7 | 23 | 45 | 21 | 127 | 127 | 0 |
| `Booster Info Popup.prefab` | **53** | 0 | 0 | 13 | 2 | 7 | 7 | 24 |

**改后目标**：Open Window `总 350 → 127`（`m_Sprite` 0 · `m_Material` 0 · `m_sharedMaterial` 0 ·
**`m_fontAsset` 仍 127** · `m_Animation`+数组 0）；
Info Popup `总 53 → 31`（`m_Sprite` 0 · `m_Material` 0 · `m_sharedMaterial` 0 · **`m_fontAsset` 仍 7** ·
**`m_Script` 仍 24**）。⚠️ 若 Info Popup 那两行 `m_Sprite`/`m_Material` **没降到 0**，
**先去看守卫那条告警**（§⑥·1·4），别先怀疑值。

> ⛔ **别再用「全文件 `grep -c "guid: 00000000000000000000000000000000"` → 0」当判据** ——
> 那份 prefab 里 **`m_fontAsset` 的 127 条按设计今天到不了 0**（工程里没有原版字体），
> 拿它当判据只会得出「修了没用」的假结论。（A1101 报 §⑧ 已经点过这条；本件**再加一条**：
> even 把字体导进来之后，**`m_Script` 那 24 条仍然不是本件管的**。）

### 判据 2（**灭自证**的结构判据：反向必须【出现】的那些 guid 及其**条数**）

⛔ **不许把这 12 个 guid 写死进脚本** —— 它们的值**不是我们的实现说了算**，
是 `EffectExporter.ImportSprite` / `ImportMaterial` 落盘时**Unity 定的**（且首次是新建、之后原地覆盖）。
⇒ 脚本**从产物自己的 `.meta` 现读 guid**，再去 prefab 里数条数：

```bash
python -I -c "
import io,os,re
PR=r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab'
s=io.open(PR,'rb').read().decode('utf-8')
def guid(p):
    if not os.path.exists(p): return None
    t=io.open(p,encoding='utf-8',errors='replace').read()
    m=re.search(r'^guid: ([0-9a-f]{32})',t,re.M); return m.group(1) if m else None
T='d:/4/Unity/MyGame/Assets/WarpforgeVFX/Textures/%s_sprite.png.meta'
M='d:/4/Unity/MyGame/Assets/WarpforgeVFX/Materials/%s.mat.meta'
sp={'Card Frame Cost Icon':10,'Card Text smooth background':10,'pedestal_icon_armor':5,
    '1_40k_cardframe_rarity_common':5,'WF_Special offer_Value':5,
    '40k_Cross_icon_cross_big Banned card':5,'Card Ready For Level Up':5}
mt={'Card Image Overflow':5,'Card Frame SDF':5,'Card Backs SDF':5,
    'UI Card Ready For Level Up':5,'Booster Pack Open Menu Background':1}
bad=0
for n,c in sp.items():
    g=guid(T%n); got=s.count(g) if g else -1
    print('sprite  ',n,'期望',c,'实得',got,'' if got==c else '<-- ✗'); bad+= got!=c
for n,c in mt.items():
    g=guid(M%n); got=s.count(g) if g else -1
    print('material',n,'期望',c,'实得',got,'' if got==c else '<-- ✗'); bad+= got!=c
print('不符项 =',bad,'(目标 0)')
"
```
**期望 = 精灵 `10/10/5/5/5/5/5`（合计 45）+ 材质 `5/5/5/5/1`（合计 21）= 66 条引用**，
**不符项 = 0**。

> 🔴 **这一对就是「灭自证」的那一对**：判据 1 的期望值（`0`）来自**我们的实现**（它写了就 0），
> 判据 2 的期望值（**那些 guid 与条数**）来自**产物自己的 `.meta`** + **原版包里的 pathID 反查表**（§②）
> ⇒ **把这三支整段删掉，两条会同时变红**（判据 1 的 `m_Sprite` 回到 45、`m_Material` 回到 21；
> 判据 2 的 12 行全部拿到 `-1`）。
> **只把「写 sprite」或「写 material」其中一半删掉**，判据 2 会**立刻指出是哪 7 行或 5 行**。
> 换个说法：**没有一种「两边一起改回去」能让这两条同时绿。**

### 判据 3（**TMP 字体**：工程里有那两份之后才成立）

```bash
python -I -c "
import io,os,re
# ① 工程里有没有同名 TMP_FontAsset（没有 = 今天的期望）
hits=[]
for r,d,fs in os.walk(r'd:/4/Unity/MyGame/Assets'):
    for f in fs:
        if f in ('Pragati-Regular SDF.asset','Asar-Regular SDF.asset'): hits.append(os.path.join(r,f))
print('工程里的原版 TMP 字体 =', hits if hits else '**一份都没有**（= 今天的期望）')
# ② 若有，那两份 guid 在 prefab 里的条数应为 92 / 35
if hits:
    s=io.open(r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab','rb').read().decode('utf-8')
    for p,exp in zip(hits,[92,35]):
        t=io.open(p+'.meta',encoding='utf-8',errors='replace').read()
        g=re.search(r'^guid: ([0-9a-f]{32})',t,re.M).group(1)
        print(p,'期望',exp,'实得',s.count(g),'OK' if s.count(g)==exp else '<-- ✗')
"
```
**今天的期望 = `工程里的原版 TMP 字体 = 一份都没有`**（⇒ 那 127 + 7 条**按设计仍留空**）。
**若这份脚本报出「找到了」** ⇒ 说明有人把字体导进来了，那时**判据 1 的 `m_fontAsset` 应当降到 0**、
且本脚本第二段应报 `92 / 35` OK。

### 判据 4（**守卫必须响**：本件**新增的一条反静默判据**）

重导日志里**必须出现**（`Booster Info Popup` 那一次）：
```
BP A1109 `Booster Info Popup` 有 **24** 个组件的 `m_Script` 指不到工程脚本（保存后会是 Missing Script）…
```
且后面点名单里**必须包含 `Image` 或 `TextMeshProUGUI` 类型名**。若这条**没出现** ⇒ 说明那 24 个组件
在内存里是真类型（那 §②·5 的 ⚠️ 走另一支：`m_Sprite`/`m_Material` 期望降到 0）。
**两条分支必居其一、而且都有日志为凭** —— 这正是「不许静默」那条规矩的落地。

### 判据 5（被 ignore 的产物：**看盘，不看 git**）

```bash
git check-ignore -v "Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab" \
                     "Unity/MyGame/Assets/WarpforgeVFX/Textures/Card Frame Cost Icon_sprite.png"
# 期望：分别命中 .gitignore:184 与 .gitignore:26
git status --porcelain -- Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs
# 期望：**空**（本件已提交进 HEAD；若报 ` M` = 又有人改了它 —— 那时先看 diff 再说）
```

---

## ⑨ 处置（交回主对话，落到正本）

1. **正本待办 · 要做**：**`BoosterPackExporter` 的 `Animator` 那一跳**（§⑦·3）——
   判据 = 两扇窗 + 4 个粒子 prefab 的 `!u!95` 全 0（今天无影响），**触发条件** = 把带 `Animator` 的根
   加进 `WindowRoots`/`CardFxRoots`。**前置**：把 `EffectExporter.ImportAnimatorController`（`:2414`）**放开可见性**。
2. **正本待办 · 要做**：**把原版那两份 `TMP_FontAsset` 导进工程**
   （`Pragati-Regular SDF` / `Asar-Regular SDF`，`bundle_fonts_assets_all`）——
   判据 = §②·3 的 127 条引用 + `FindProjectAsset` 那条现成的接回路径（导进来**不用改代码**就接上）。
   ⚠️ 这是一件**独立的活**（TMP 字体资产带图集纹理与字形表，怎么落盘要另查），**不是本件的尾巴**。
3. **正本待办 · 要做**：**`Booster Info Popup` 的 24 条 `m_Script` guid 0**（§⑦·2）——
   判据 = `350208831926335389` = `UnityEngine.UI.Image` · `7477354737935883349` = `TextMeshProUGUI`
   （反查 `bundle_Waprforge_monoscripts/MonoScript/`）。**两扇窗为什么不一样，成因没查清**。
4. **给下一个会话的坑**：
   · **`Assets/WarpforgeVFX/*` 与 `Assets/WarpforgeBooster/` 两条 ignore**（`.gitignore:26` / `:184`）
     ⇒ 这两棵树的产物**在 `git status` 里一个字都看不见**，收口**只能看盘**。
   · **uGUI 的 `material` 是带回落的 getter** —— 想精确读 `m_Material` 必须走序列化字段名
     （本件 `SerializedFieldRef` `:735`），照 getter 取值会**把 50 个「本来没材质」的 Image 写上一份默认材质**。
   · **`fileID` = 原版 pathID** 这条判据在 Booster 这一族**连用两次都成立**（A1101 的动画、本件的 320 条）。

---

## ⑩ 摘要（300 字以内）

**A1109 已做**：`Export()` 补 uGUI 与 TMP 两跳（新 `:490-649`）——`Image` 的 sprite/材质
（复用 `ImportSprite`/`ImportMaterial`）、TMP 字体（**工程无此资产 ⇒ 留空 + 出声**）与材质（接得上），
另加「脚本没解析」守卫。320 条逐族（45/21/127/127）按原版 pathID 全核。
🔴 简报两处不符：**320 不是 299**；**「照 `EffectExporter` 那两支抄」不成立——那两支不存在**。
**未跑 Unity ⇒ 三支未执行**；类型检查 0/0、`+282/-1`、LF 未翻。
另报：`WarpforgeBooster/` 被 `.gitignore:184` 挡住；`Booster Info Popup` 24 条 `m_Script` guid 0，本件没修。
