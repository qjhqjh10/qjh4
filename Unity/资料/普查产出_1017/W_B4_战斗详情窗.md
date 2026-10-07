# W_B4 · 战斗侧卡片详情窗（A860）—— 两条都核完了，**第 1 条的前提是记反的**

写手代理 B4 · 2026-10-17 · 白名单：`Battle/CardDisplayWindow.cs` + `Editor/BattleScene.cs`（只补断言）。⛔ 未跑 Unity。

---

## 一、结论（逐条：原版 / 我们 / 改成什么 / 判据）

### 条目 1 · `CardDisplayOptions` 面板 —— ❌ **原版战斗版没有这一块**（前提记反了）

| | 内容 |
|---|---|
| **原版（战斗）** | `CardDisplayWindow.options` = `{m_FileID:0, m_PathID:0}` = **null**，**13/13 竞技场全是 0**。`informationPanel` 同样 13/13 为 0，`tryShowResources` 13/13 = 0。 |
| **原版（菜单）** | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2033.json`：`options = 2331` · `informationPanel = 2206` · `tryShowResources = 1` ⇒ 这一块**只有菜单版有**。 |
| **代码链** | `CardDisplayWindow__ShowCard.c:153-167`（`param_4 != 0` 那一支）**那句守卫**（`:159`）就是 `if (options == null \|\| card.field_0x4c != 0) goto LAB_1807fbe68` ⇒ 战斗侧那块**永远不会出现**。`ShowBattleCard.c:28,41` 把 `showOptions = DisplayCardEffects(...)` 传进来在战斗侧是**空转**（我读了那个方法体逐句：它算出的是「≥1 条 effect 文字非空 **且** 不是环境卡」，用来切 `cardEffectsGroup` 那块）。 |
| **节点面（自走 RectTransform）** | 战斗 arena1 根 = `RectTransform_3030`（go=419）**2 个孩子**：`Menu Dark Background`(go=631) + `Card Display`(go=243, 752×868, **4 个孩子** = `LowerSection`/`Cards`/`TutorialObjs`/`EffectList`)。菜单版根挂在 go=479（`Card Displayer Menu For Menu`）**4 个孩子**：mask(go=57) · **options 宿主 go=106**（父为空、3 孩子：`Panel` 450×756.98@(559,15.14)、go=71 0×85@(0,−71)、go=94 269.86×79.37@(0,−329.5)）· `Card Display`(go=434) · go=512 392.93×500@(500,−8.35) = `Item Information Panel`。**`CardDisplayOptions(2331).m_GameObject == {0,106}`** ⇒ 宿主就是那个节点。 |
| **侧证** | 13 个竞技场 `GameObject/` 目录全表 grep `Panel\|Crafting\|Upgrade\|Alternate\|Card Counter\|Wildcard\|Item Information` = **零命中**；`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:333-345` 同此。 |
| **我们** | `Battle/` 下一条 options 面板都没有（与上一条一致）。 |
| **改成什么** | **什么都不加**（保持现状）。⚠️ **`资料/普查产出_1017/W_选择窗与详情窗.md:44,93` 那句「原版战斗侧**可能**出这块，是缺口、不是『原版没有』」是错的** —— 提请注意（该文件不在我的白名单，未改）。`Shell/CardDetailPopup.cs:42-45` 同一句话，**也不在**我的白名单。 |

### 条目 2 · 眼睛钮「显示卡面文字」—— ✅ **原版战斗版没有，我们多建了一颗**（已删）

| | 内容 |
|---|---|
| **原版（战斗）** | `showCardTextButton` = `{m_PathID:0}` = **null，13/13 全是 0**。三处**都拿 `!= null` 把着**：`ShowCard.c:85-96`（`SetActive(go, cardType==0\|\|10)` 整段）、`CardSwapFinished.c:20-45`、`Open.c:72-83`（`AddListener(→ ToggleCardState)`）⇒ **画不出、也点不到**。`ToggleCardState` 在 `decomp_full` 里**只有定义、零调用点**（UnityEvent 直连，没有代码调用）。 |
| **节点面** | 全库 `Show Card Text` GameObject **只有 1 份**：`bundle_scenes_scenes_mainmenuwarpforge/GameObject/Show Card Text.json`（`资料/说明书/04_界面UI/主菜单全树.md:201`：`[-779,1486 89x89]`）。13 个竞技场 `*Text*` 名字全表**零命中**。战斗 `LowerSection`(go=827, 1320×137@(0,−518.34)) **只有 2 个孩子**（`FlavourTextBG` + `Voice Over Button` 88.655²@x734.33）；**菜单版同层是 3 个**。 |
| **原版（菜单）那颗的位置** | `181.34,945.51 → 270,1034.17`（88.655²，框中 `225.668, 989.838`）—— 断言用它当负向探针。 |
| ⚠️ **别拿图标认钮** | 眼睛那张图 `-7255197835733746773` 在竞技场里**是有的**，但挂在 `ChooseCardMenu` 的开关钮上（`MonoBehaviour_4813` 的 `m_TargetGraphic` = `_4921` 那张 `Image`；`m_OnClick → ChooseCardMenu.ToogleChooseMenuVisibility`）⇒ 同 `40K_melee_glow` 那次的教训。 |
| **我们** | `Battle/CardDisplayWindow.cs` 建了一颗 `Show Card Text`（`MenuDraw.Rect(..., "Show Card Text", ...)`）+ `_eyeBtn` + `HitEye` + `ToggleLore`；`BattleDriver.cs:5101` 接线。 |
| **改成什么** | **删掉那颗钮**（`_eyeBtn` / 建节点 / `SetChrome` 那行 / `RefreshButtons` 里那一支）。`HitEye` 改成**恒 false 的桩**、`ToggleLore` 改成**空桩** —— ⚠️ **只因为 `Battle/BattleDriver.cs:5101`（白名单外）还调它们**，删了就编不过；那两个桩各带一段「删掉那两行后本桩也能删」的注释。 |

### 条目 2 附带查出的一条偏离（我改了）

- **原版** `CardDisplayWindow__SetCardLore.c:24-27`：`SetActive(loreObjectBG, !IsNullOrEmpty(flavourText))` ⇒ **没有风味文字时整块 `FlavourTextBG` 不亮**。
- **我们**：写的是 `SetActive(LoreVisible)`，而眼睛钮删掉后 `LoreVisible` **恒 true** ⇒ **没字也亮一块空的风味底板**（以前还能拿那颗钮手动切掉 ⇒ 是我的删除动作让它变成「永远露着」的，所以在本件里一并收口）。
- **改成**：没字 ⇒ `SetActive(false)`；有字 ⇒ `SetActive(true)`。

---

## 二、改动清单

### A. `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/CardDisplayWindow.cs`（+63 / −32）
1. 删 `_eyeBtn` 字段 + `Build()` 里那颗 `Show Card Text` 节点 + `SetChrome` 里那一行 + `RefreshButtons` 里那一支。
2. `HitEye(Vector3)` → `return false;`（带完整判据注释 + 「这是给白名单外留的桩」）。
3. `ToggleLore()` → 空桩 + 出声（原版那颗的真语义是 `ToggleCardState`：翻 `showCardInfo` + 逐格 `CardTextsController.ChangeState`，**不是**切 `FlavourTextBG` —— 如实标注在注释里）。
4. 文件头新增 A860 判据块（13/13 字段表 + 代码行号 + 节点面 + 「别拿图标认钮」）。
5. `RefreshLore` 的「没字也亮」→ 照 `SetCardLore` 收口（上面那条）。

### B. `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（+135 / −0，**纯追加**）
- **§A860 块**（插在「风味底图」之后、`Tap()` 之前，约 57 行）· 6 条断言：
  ① 子树里**没有** `Show Card Text` 节点；② …而 `Voice Over Button` **留着**（防「删过头」）；
  ③ `HitEye(225.668, 989.838)` = **false**（**判别式**：把那颗钮建回来就红）；
  ④ `CardDisplayOptions` 那一族 7 个节点名**一个都不命中**（补面板就红）；
  ⑤ **没字 ⇒ `FlavourTextBG` 不亮**（改成恒亮就红）；⑥ 换成有字那张 ⇒ **亮回来**（两态真翻得动，防「一直关着」）。
- **§14b4 语言那一行**（插在 AutoZoom 块之后、`14c` 之前，约 72 行）· 8 条断言：
  ① `LanguageRowBuilt` + 框**实画**尺寸 = **250.00 × 59.40 px**（原版 `1007.3,222.6→1257.3,282.0`）；①b 箭头**内接**进 20×20；
  ② 落 `Chinese` ⇒ 语言名「中文」（词条 `MainMenu/Settings/LanguageName/<枚举名>`）；
  ③ **判别式**：只喂按下那一帧 ⇒ **不换**；抬起那一帧 ⇒ 被接住 + `Chinese → English` + 语言名跟着变，**两态都写死**；
  ④ 标签 = 原版 TMP 的 `m_text` `'Select Language'` + 词条键 = 原版 `mTerm`；
  ⑤ **判别式**：按下在框内、抬起跑到框外 ⇒ **不触发**（命中区写成「整个面板」就红）；
  ⑥ 收尾把语言放回去（`Loc.PersistOverride = true`，**一个字节都不写 `PlayerPrefs`**，`finally` 里还原）。

---

## 三、没查清的部分 / 要主对话接手的

1. 🔴 **`Battle/BattleDriver.cs:5101`**（白名单外）：`if (_cardDisplay.HitEye(wp)) { _cardDisplay.ToggleLore(); return true; }` —— 这一句删掉后，`CardDisplayWindow` 里 `HitEye` / `ToggleLore` / `LoreVisible` **三个桩可以一并删**。✅ **2026-10-17 当天已由 B8 批删干净**（含 `ContainsPointer` 的 `|| HitEye` 与 `Build`/`Show` 各一处 `LoreVisible = true`；`grep _cardDisplay.HitEye` 全仓 **0 命中**）。⚠️ **别误删另外三个类上的同名成员**（`MulliganPanel.cs:332` / `CardChoicePanel.cs:338` / `Shell/CardDetailPopup.cs:870` —— 那几扇窗**原版本来就有**眼睛钮）。
2. 🔴 **两处文档把 A860 第 1 条记反了**：`资料/普查产出_1017/W_选择窗与详情窗.md:44,93` 与 `Shell/CardDetailPopup.cs:42-45` —— 都写着「原版战斗侧**可能**出 options 那块，是缺口」。**实际战斗侧 `options` 13/13 = null**。（两处都在白名单外，未动；`CardDetailPopup.cs` 里关于**眼睛钮**那半句是对的，菜单版确实有。）
3. `Battle/CardButtons.cs` 的 `HasTextButton` 现在**只剩 `Shell/CardDetailPopup.cs:889` 在用**（战斗侧已不用）—— 文件头把两扇窗并在一起说，可顺手分开（非必须，不在白名单未动）。
4. ⚠️ **`Resources/Art/` 此刻在工作树里不存在**（`.gitignore` + 靠 `工具/import_original_art.py` 生成）⇒ 断言里凡依赖美术的那几条（`Voice Over Button` 节点、语言那一行的框/箭头）与本节既有的 `CardArt.FlavorBg(...) != null` 是**同一条依赖**：素材腿没跑时它们会红。
5. **我没跑 Unity**（红线）：断言只写不跑，红绿由主对话在收口时定。
6. **查不到的**：原版那颗眼睛钮（**菜单版**）的**运行期**行为我只读了反编译、**没跑到实况**；战斗版那颗根本不存在，无从跑。

---

## 四、类型检查结果（原样贴）

```text
$ TMPDIR=/tmp/wf_b4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾：两个文件**都还是 CRLF**（`file` 实核）；`git diff --numstat` = `CardDisplayWindow.cs 63/32` · `BattleScene.cs 135/0`（非整篇翻行尾）。

## 五、我顺手补了什么 / 为什么（协调台追加那条）

`Editor/BattleScene.cs` 原来对「对战设置窗那颗语言行」**零覆盖**。我在 `14b3 AutoZoom` 与 `14c 音量滑块` 之间加了 **§14b4**，把协调台点名的四种鉴别力各落到位（上表 B 那段），并多用了一条负向判别式（⑤ 抬起跑到框外）。
**为什么这么写**：① 期望值全部取**原版字面**（250×59.4 / 20×20 / `'Select Language'` / `mTerm` 键）—— 面板内 px 那几个常量我**没有**去读 `SettingsPanel` 的 `Lang*`，⛔ 避免拿我们自己的常量证明我们自己；② 「只喂按下那一帧不换」是**判别式**（把触发挪回按下就红），不是同义反复；③ 全程 `Loc.PersistOverride = true`，**不污染** `PlayerPrefs["Language"]`；④ `finally` 里还原注入点、块尾还原语言，后面的断言不受影响。
