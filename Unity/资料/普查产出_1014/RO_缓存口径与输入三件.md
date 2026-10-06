# RO · 缓存口径与输入三件（A796′ · A622 · A650）

> ⚠️ **本文件由主对话代录**：跑这一趟的代理**没有写文件工具** ⇒ 正文由主对话落盘（**结论/数字/站点/命令一字未改**，只压掉过程叙述）。
> **读到的是 2026-10-06 14:05 时刻的工作区**（`git status` 有大量 `M`）。⚠️ 行号会漂：13:56 → 14:05 之间 `RewardsScene.cs` +11 行、`CollectionScene.cs` +12 行 ⇒ **下面全部配了锚点，别只抄行号**。
> ⛔ 原报告自陈：**零改动**（只读命令 + 两个 `python -c` 只读脚本；不写文件、不落盘、不碰 git、不碰 `d:/2/`）。

---

# 一 · A796′ ·「缓存口径」同族**更外层**还在的口子

## 0 · 机制（一手判据，亲读 `Battle/Label.cs`）
`WorldW`/`WorldH` getter（`:53`/`:55`）→ `EnsureMeasured()` → **字段缓存 `_tmpW/_tmpH`**（`:37`）。写缓存的**只有** `RefreshBounds()`（`:896-906`）：`_tmpW = |b.size.x|`（TMP `textBounds`）并按 `-anchor.x*_tmpW - b.min.x` 摆好 TMP 子节点。
`SetFontSize`（`:503-514`）= `fontSize = w; ForceMeshUpdate();` —— **不碰缓存**；`SetCharSpacing`（`:524-533`）= `characterSpacing = v; ForceMeshUpdate();` —— **不碰缓存**。
⇒ 「末次 `RefreshBounds` 之后再重排」时**缓存陈旧、旧口照报旧值** —— 这正是 A750 换口的理由，**对上表每一个口同样成立**。

## 1 · 逐条判「同形同义 vs 不同义」
| 口 | 在哪（14:05 现读 + 锚点） | 调用点 | 判 | 该不该收口 |
|---|---|---|---|---|
| `RewardsScene.RectOf` **label 支** | `RewardsScene.cs:555` 定义；label 支 `:562`（锚：`var lb = t.GetComponentInChildren<Label>();` 的下一行 `if (lb != null) { node = lb.transform; w = lb.WorldW * 108f; … }`） | **42 处**（41 行；另 `RectOfUnion:612` 内部转发 1 次） | **同形同义** | 该收，但**一次动一片** |
| `ShopScene.RectOf` label 支 | `ShopScene.cs:1034` / `:1041` | **22 处** | **同形同义** | 同上 |
| `CollectionScene.RectOf` label 支 | `CollectionScene.cs:482` / `:489` | **4 处** | **同形同义** | 同上 |
| （三份逐字同源核对） | 三份 label 支**逐字符相同**，只差缩进（Collection 多 4 空格） | — | — | — |
| `TextLeftPx` / `TextRightPx` | `RewardsScene.cs:463` / `:469`（体 `:466`/`:472`） | **17 处** | **同形同义**（与 `RectOf` label 支**逐字同式**：`PxOf(lb.transform.position.x) ∓ lb.WorldW*108f*0.5f`） | **该收**（本文件 `:6573` 自己已写「只当快速回归用」；替代品 `TmpEdgePx`/`TmpRenderedRect` 已在本文件里） |
| `CollectionScene.TitleLeftPx` | `CollectionScene.cs:509`，体 `:514`（带 `w>20 && w<2000` 守卫，量不到回 **−9999**） | 1（helper 自身） | **同形同义**（多一道「量不到就红」的守卫 —— 方向是对的） | 该收（守卫保留） |
| CollectionScene 三处直读 | `:3381 lb0` · `:3455 olb` · `:4365 flb` · `:4415 alb` · `:4574 sgLb`（五个变量逐个查过声明，**全是 `Label`**） | 5 | **同形同义** | 该收（**`:3381/:3455/:4365` 就是 §5·2 点名的那三处**） |
| CollectionScene `WorldW * 54f`（半边宽） | `:1198 lcf` · `:1199 lil` · `:1892 dnl` · `:1897 wnl` · `:4027 lt` · `:4028 lc2` | **6** | **同形同义**（`×54` 只是 `×108/2`） | 该收。⚠️ **§5·2 把这一族写成「`RewardsScene.cs` 的 `* 54f`」= 归属错**：`RewardsScene.cs` 全文件 **0 处 `54f`** |
| 🆕 **`MainMenuScene.RenderedRect`（§5·2 没列到）** | `MainMenuScene.cs:290`，label 支 `:296`（`if (lb != null) { w = lb.WorldW * 108f; h = lb.WorldH * 108f; }`） | **≈12 处**：`:2106 Deck Name` · `:2107 Warlord Name` · `:2112 row` · `:3880` · `:3893` · `:4887` · `:4918` · `:5790` · `:5814` · `:7102` · `:8110`/`:8111 Event Title` | **同形同义** | **该收** —— 这才叫「同族更外层」。⚠️ **A617 那份「16 处调用点」清单只换掉清单里的断言、helper 本体留着** |
| ⛔ **非本族（同形不同义，别一起换）** | `RewardsScene` 的 `a92hq :3447` · `flat :4264` · `band :4269` · `left :4315` · `right :4316` · `bgS :8815` —— **声明全查过 = `ImageQuad`** | — | **不同义**（quad 支不是缓存口） | ⛔ **别动** |

## 2 · 对账：§5·2 那三句话，两句要订正
- ✅ 成立：「`RectOf` 被**几十个调用点**共用 ⇒ 换它一次就动一大片断言，**不是「顺手」的量级**」—— 数字对上（42/22/4）。
- ❌ **不成立**：「被 `CheckRectPx`/`CheckW`/`CheckH`/`CheckRectPxUnion` 共用」—— 那三条在三个文件里**都不走 `RectOf`**：`RewardsScene.cs:79/88/96` · `ShopScene.cs:960` · `CollectionScene.cs:252` **一律 `GetComponentInChildren<ImageQuad>()` 直读**。⇒ 共用的主体是 **67 处直接调用点**。
- ❌ **归属错**：`* 54f` 那族在 `CollectionScene.cs`，不在 `RewardsScene.cs`。
- 🆕 **漏一处**：`MainMenuScene.RenderedRect`。

## 3 · 落地口径（风险与改法）
1. **换口当天逐位同值**（`CollectionScene.cs` 的 `TmpRenderedRect` doc `:324` 自己写着）⇒ 期望值**一位都不用动**，动的只是「以后重排能看见」。**这不是「修红」，是「补牙」。**
2. ⛔ **不许换成「TMP 网格顶点」那条口**：`RewardsScene.cs:6580-6584` 实测墨迹起点还多一个**首字左边距**（`Current Streak` 43.00 vs 45.22 = **2.22px**；1 字时 1.35px）⇒ 换错口会让一批期望值**集体偏 1–2px**。**要换就换 `textBounds` 那条**。
3. **改动面必须串行**：`RectOf` 是**共用量尺**，返回值同时被拿去做**落位**判据 ⇒ 三文件 ≈68 处一起换尺。落地时**四个宿主各跑一次**（`RewardsScene.Run` / `ShopScene.Run` / `CollectionScene.Run` / `MainMenuScene.Run`），**一块一个写手**。
4. **本件查不到的一格（如实）**：**「哪些调用点真的落到 label 支」静态判不出来**（`RectOf` 是 Label 优先；本仓已有陷阱记录：`RewardsScene.cs:5108` 附近「⛔ 别用 `RectOf(FindChild(ph,"Unlock Button"))`」）。**可复算的做法**（将来的人做）：在 `RectOf` 两支各加 `static int` 计数器 + 四个 `Run()` 末尾打印一次。**现在能给的是界**：上界 = 68。

---

# 二 · A622 · D3「类名优先 vs 字段指纹」逐包核

## ① 可复算口径（纯只读）
类名路 = `menu_dump.mono_index()`（`:179`），索引 `assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json`（文件名 = PathID）+ `globalgamemanagers/MonoScript/*.json`，读 `m_ClassName`。
指纹路 = `menu_dump.fingerprint()`（`:196`），布局组那一支 = `{'m_Padding'} ⊆ keys && ({'m_Spacing'} | {'m_ChildAlignment'} ∩ keys) → 'LayoutGroup(?)'`；`menu_rect` 的 `kinds` 用**同一条**（`menu_rect.py:728` 注释自称逐字相同）。
实跑命令（可整段粘；`d:/4/Unity/工具` 下执行，**只读**）：见原报告的 `python -c "…"`（遍历 45 个带 `MonoBehaviour/` 的包，打印 `MB / resolved / LGname / miss / falsepos`）。
判 D3 成立与否就两列：`miss` = 类名说是布局组、指纹没认出；`falsepos` = 指纹说是布局组、类名不是那三个内置类。

## ② 实跑结果（**全部 45 个包**，不是抽 8 个）
- **分母收窄（这条要改账）**：`assets_full` 下 = **84 个 `bundle_*`** + 若干内置文件；其中**有 `MonoBehaviour/` 的只有 45 个** ⇒ 「其余 89 包」要收成「其余 **44** 个带组件表的包」。
- **MB 66459 · 类名解出 66458**。唯一没解出的 1 个 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_1274605648866122881.json`（`m_Script.m_PathID = 0`，**无脚本的坏件**）⇒ **类名那条路全覆盖**。
- **`miss` = 0（45/45 包逐包 0）**；类名判为布局组 **1820** 个。
- **`falsepos` = 9**：menus 7 · `battlearenaleviathan` 1 · `mainmenuwarpforge` 1。**逐条看：9/9 都是 `LayoutGroup` 的真子类**（`EverguildLayoutGroup` ×4 · `FlexibleGridLayout` ×4 · `EverguildGridLayoutGroup` ×1）⇒ **真实误报 0，指纹反而比「只认三个内置类」更全**。
- 抽样 8 包（`MB/类名LG/miss/falsepos`）：`menus 35014/1463/0/7` · `mainmenuwarpforge 1134/29/0/1` · `generalgamewindows 356/9/0/0` · `battlearena1 1715/24/0/0` · `cosmeticsso 1261/0/0/0` · `draftpacks 882/0/0/0` · `battleprefabs_vfxandmisc 2847/0/0/0` · `battlearenaleviathan 1714/24/0/1` ⇒ **结论在每个包上都成立**。
- ⚠️ **覆盖面如实标**：本判据只覆盖 **「布局组」这一族**（D3 原话就是它）。`kinds` 其余标签（`Button`/`Image`/`Text`/`Mask`…）**没逐包比**；`menu_rect` 的 Mask 多出来的第二条 clause 属另账（A625）。
- ⚠️ 口径提醒：本件是 **MB 级**；账上「1470 个布局组」是 `menu_dump` **节点级** ⇒ 两者差 7，**口径不同、都成立**。

---

# 三 · A650 · 原版唯一的 `TouchPressed` 消费点（`EnemyInfoTouch`）

## ① 原版那个消费点是什么行为（反编译实读）
类 `EnemyInfoTouch : MonoBehaviour`（`dump.cs:38080`，TypeDefIndex 740），**唯一字段** `public GameObject enemyInfoObj; // 0x20`。
| 方法 | 行为 |
|---|---|
| `Start()` | `enemyInfoObj.SetActive(false)` ⇒ **开局先藏** |
| `OnTouchDown(Vector2)` | `manager != null` 且 **`!IsTutorialMatch`** 且 **`!IsCampaignLike`** ⇒ `enemyInfoObj.SetActive(true)`（入参在体里**没被用到**） |
| `Update()` | 读 `TouchInputManager` 静态块（= `TouchPressed`）；`if (!TouchPressed) enemyInfoObj.SetActive(false)` ⇒ **没按住/松手就收** |
⇒ **净语义 = 「按住显示、松手收起」的敌情浮窗**；`TouchPressed` = legacy `Input.GetMouseButton(0)`（**按住**，不是「点了一下」，与 A655 一致）。
⚠️ `OnTouchDown` 全反编译里**零调用点** ⇒ 入口在 **prefab 的 UnityEvent/EventTrigger** ⇒ **必须读原版 prefab 才能定**。

## ② 🔴 prefab 侧：**查不到**（如实，附搜过哪儿）
- `grep -rl "enemyInfoObj"` **全 `d:/2`**：只命中**代码产物**（`il2cpp_out/{dump.cs,il2cpp.h,DummyDll}`、`unity_run_ref/MelonLoader/…/Assembly-CSharp.dll`）；**一个序列化 JSON 都没有** ⇒ 该组件实例**不在已解包资产里**。
- 反向印证：45 个包的 MB 逐个解类名**每个包解出率 100%**（`battlearena1` = 1715/1715）；若它在包里必然表现为**一个解不出类名的 MB** —— **不存在这样的 MB**。
- `assets_full/_stats.json`：`bundle_scenes_scenes_battlearena1` = 5331 json / **fail 0** ⇒ **不是导出失败**。真有导出失败的是 `resources.assets`（**616 fail / 1401 json**）· `sharedassets0`（182）· `level0`（73）—— ⚠️ **`enemyInfoObj` 最可能落在这些没导出的对象里**（**没验证，不能当结论**；要验证得重解包，本件只读不动 `d:/2/`）。
- ⇒ 「`enemyInfoObj` 指向哪颗 GO / `OnTouchDown` 由谁触发 / 长什么样」**= 查不到**。搜过：`assets_full/**`（字段名 + 类名，大小写不敏感）· `_stats.json` · `menu_dump.mono_index()` 全索引 · `battlearena1` 全 1715 个 MB · `资料/战斗规格/**`（原版战场全树里 `EnemyInfo` GO 280 的组件表**没有** `EnemyInfoTouch`）。

## ③ 我们有没有「敌情小窗」—— **没有**（但查法比 A650 原话深一层）
- **有「那条名牌」，没有「那扇窗」**：`Battle/BattleDriver.cs:7716-7739` 建的是**常显**的敌方督军名牌（`_enemyPlate` = `UI_Player_Frame` 435.7×126.3、`_enemyText`）；`:8109-8130` 建 `TitleBackground_Foe`。**没有任何一处**是「按下才出现、松手消失」的件。
- `grep -rn "EnemyInfo" --include=*.cs`：**全是注释**（`BattleDriver.cs:7717/7718/8029/8116` · `Battle/ChatPopupPanel.cs:10` · `Battle/TouchInputManager.cs:9` · `Editor/BattleScene.cs:2215`）；**没有一行在建节点/挂组件**。
- 与 W-B5 的结论一致（`普查产出_1013/WB5_两件组件.md:40` / `:271`「❌ 没有等价物」）—— A650 要的复核**已完成**。

## ④ 判「要不要做、要做什么」
- **该单开账**，但**必须标「判据不齐」**（缺**宿主**+**目标 GO**）⇒ 现在判不了「要做什么」，只能把**已齐的一半**锁死：
  1. 入口必须是 **`Input.GetMouseButton(0)` 的「按住」语义**（**不是**点击/抬起，同 A655）；
  2. `Start` 先藏；
  3. **教程局与战役局不显示**；
  4. **松手那一帧收**。
- **不影响 A664**：`TouchInputManager` 那 12 个消费点已裁「**0 条要接**」；本件只补一句：`EnemyInfoTouch` 不接的**根因是没有宿主**（没有那件浮窗），不是接线方式的问题。⇒ 「要做小窗」= 先建那件东西 = **新账**（判据要等 prefab 侧能读出来）。继续补判据只有两条路（都要点头）：**① 运行时取**（真机/真包 dump 那颗 GO）；**② 从没导出的 `resources.assets` 616 个失败对象里重解包找**。⛔ 两条都**超出只读范围**。
