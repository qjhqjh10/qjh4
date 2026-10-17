# R3 · S01 逐按钮 `m_onClickSound` —— 原版 cue 映射表（只读研究）

> 会话：2026-10-17 · 角色：界面线第二批【只读研究代理 R3】
> 范围：音效线 `S01`。**只统计壳侧（菜单/窗口/按钮）**；战斗 HUD 的按钮（`scenes_battlearena*`，每场 30 Button + 3 Toggle）归战斗线 `T06`，本报告不展开。
> 红线：**未改任何代码 / 任务表 / 正本，未跑 Unity，未起 player**。唯一写出的文件 = 本报告。
> 数据源：`d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/` 的 `menus_assets_all.bundle` · `Waprforge_monoscripts.bundle` · `soundcollection_assets_all.bundle` · `scenes_scenes_mainmenuwarpforge.bundle` · `generalgamewindows_assets_all.bundle`（**UnityPy 1.25.3 直接读 PPtr**）+ 我们自己的 `Shell/*.cs`。

---

## 结论（3–6 行）

1. **原版确实按颗带 cue，而且绝大多数不是默认音**：`menus` 包 **1006** 颗 `EverguildButton|EverguildToggle`，其中 **424 颗 `m_onClickSound` 非空**（**291 Button + 133 Toggle**）；扣掉 15 颗 cue 恰好 = 默认 `ButtonClick` 的，**409 颗的实际音与我们现在播的不同**。另有 `generalgamewindows` **14 颗**、主菜单场景 **4 颗**（共 **442 颗** 有显式 cue）。
2. **全部 cue 只有 10 个名字，而且 10 条都能在本地完整解出**（cue 包装 → `clipList` → `AudioClip`，见 §二·B）。其中 **8 条我们工程里还没有**（`animfx_sounds.json` 现只有 `ButtonClick` / `ButtonClick Back` 两条）⇒ 要靠 `工具/import_original_sfx.py` 的 `EXTRA_CUES` 补 8 条再跑一次导入器；**不是「查不到」**。
3. **映射可分三档**：① **能逐颗对上**（**73 条映射行**，对应我们 60+ 处源码站点，集中在设置、收藏、卡组信息、聊天、个人资料、奖励、排位/小冲突、排行榜、商店/报价、任务等窗）；② **原版 prefab 我们整体没建**（`Give Feedback Popup menu` 42 颗、Draft 一族 51 颗、`Sunset Popup` 4、`Rate Popup` 2、`WhereToGetCardsPopup` 2、`Expansion Pass Tab` 2 等）——这些**不是映射不到，是那扇窗还没做**；③ **建了窗但没建那颗钮/语义不同**（任务容器的点击、商店商品的点击、`Practice Mode Menu` 的 Back 等），逐条写在 §四·C。
4. **默认按钮音 `ButtonClick` 本身就是 15 颗 Toggle 的显式 cue**（`clip = UI Select Toggle`）⇒ 这 15 颗不用改；但注意 **cue 名 `Select Toggle` 是另一条音**（`clip = UI Click 2_no delay`）—— 名字像、声音不同，⛔ 别合并。
5. **最小改法只有一处漏斗**：给 `MenuDraw.Hit(...)` 末尾加 `string clickCue = null` → `WindowButton.clickCue`；`GameWindow.AddHit` 透传；`WindowButton.PlayClickSound()` 里 `cue = clickCue ?? DefaultButtonCue`。**不改任何现有调用点也不会变**（可选参数）。
6. ⚠️ **W_G 报告里的「438 颗 Button + 133 颗 Toggle」中 438 这个数现读不成立**：`bundle_menus_assets_all` 里 `EverguildButton` 非空 cue = **291**（总 855 颗），`EverguildToggle` 非空 = **133**（总 151 颗）⇒ **424**。438 的来源未查到（可能混入了别的包/场景实例）。按铁律 5 在此记下。

---

## 一、读法与口径（可复现）

```
# 读 PPtr（关键：m_FileID 1-based 指 externals；m_onClickSound 指【cue 包装】不是 clip）
# UnityPy 把 menus + monoscripts + soundcollection 一起 load，m_Script 才能 deref 到类名
import UnityPy
env = UnityPy.load(base+"/menus_assets_all.bundle", base+"/Waprforge_monoscripts.bundle", base+"/soundcollection_assets_all.bundle")
# 遍历 MonoBehaviour：d.m_Script.read().m_ClassName in ("EverguildButton","EverguildToggle")
# d.m_onClickSound → (m_FileID, m_PathID)；用 PathID 去 soundcollection 的 MonoBehaviour 里查 m_Name
```

口径：

- **cue 名 = 包装 MonoBehaviour 的 `m_Name`**；`clipList` 才是真 `AudioClip`。本报告两条都给。
- **`useDefaultAudioClipIfNull`：424/424 颗 = 1**（`menus`）。⇒ 只要 `m_onClickSound` 有内容就播它自己的，否则播 `SoundManager.defaultButtonSound`（`ButtonClick`）。
- 原版 `EverguildButton__OnPointerClick.c` / `EverguildToggle__OnPointerClick.c` 逐句：`cue!=null && cue.HasAudios()` ⇒ 播自己的；否则 `useDefaultAudioClipIfNull` ⇒ 播 `defaultButtonSound`；**两者都没有 ⇒ 静默**。混音组 = `SoundManager.Play2D(cue, 0)`（FX）。
- 路径 = prefab 根到该组件 GameObject 的父子链（`Transform.m_Father` 逐级上溯）；根 = 父为 null 的那一节。
- **同一颗按钮在别的 prefab 实例里 cue 可能不同**：本报告逐实例读，不按名字去重。

---

## 二、原版侧

### A. 全量统计（按包）

| 包 | 组件总数 | 非空 cue | cue 分布 |
|---|---|---|---|
| `menus_assets_all` | 1006（855 Button + 151 Toggle） | **424** | `ButtonClick Positive` 140 · `Select Toggle` 115 · `Purchase Click` 67 · `ButtonClick Back` 64 · `ButtonClick` 15 · `SelectAvatar` 11 · `MenuStartGame` 6 · `ButtonClick Negative` 4 · `UI Send Message` 1 · `UI Purchase 2` 1 |
| `generalgamewindows_assets_all` | 16 | **14** | Positive 8 · Negative 4 · Back 2 |
| `scenes_scenes_mainmenuwarpforge`（主菜单场景实例） | 26 | **4** | Back 2 · Positive 2 |
| `mainmenualwaysloaded_assets_all` / `menusharedresources_assets_all` | 2 / 0 | 0 | — |
| `scenes_scenes_battlearena*`（13 场，战斗侧，⛔ 本线不碰） | 每场 33 | 每场未逐个统计（同上口径可跑） | 归战斗线 `T06` |

### B. 10 个 cue 的包装 / clip / 参数（全部本地可解析）

| cue | 包装 pid | clip(s) | pitch | volume | timeToPlayAgain | 我们表里有没有 |
|---|---|---|---|---|---|---|
| `ButtonClick` | 6176553109044381692 | `UI Select Toggle` | 1–1 | 1–1 | 0.1 | ✅（默认按钮音） |
| `ButtonClick Positive` | 8549838806721213354 | `UIButtonPress` | **0.8–1.2** | 1–1 | 0.1 | ❌ 待补 |
| `ButtonClick Negative` | 1387228150816026206 | `UIButtonPress Negative` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `ButtonClick Back` | 8084683923307806969 | `UIButton Back` | 1–1 | 1–1 | 0.1 | ✅（默认关窗音） |
| `Select Toggle` | 8881786964274599484 | `UI Click 2_no delay` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `SelectAvatar` | -7839165639853058975 | `SelectAvatar` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `Purchase Click` | 8111810622317061932 | `UI PurchaseClick` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `MenuStartGame` | -1545367626748338049 | `StartMatch` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `UI Send Message` | -4151575586990132199 | `UI Send Message` | 1–1 | 1–1 | 0.1 | ❌ 待补 |
| `UI Purchase 2` | 6164823724645931967 | `UI Purchase 2` | 1–1 | 1–1 | 0.1 | ❌ 待补 |

> 补法：`工具/import_original_sfx.py` 的 `EXTRA_CUES` 追加这 8 个名字 → `PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py`（会拷 wav/ogg 到 `Resources/Art/audio/sfx/` 并写 `Resources/animfx_sounds.json`）。**这 8 条全在 `soundcollection_assets_all` 里，clip 都在**，无远端依赖。
> ⚠️ `ButtonClick Positive` 的 pitch 是 **0.8–1.2 随机** —— `WFSoundCue.minPitch/maxPitch` 已支持，导入器会照写。

### C. `menus_assets_all` 逐 prefab（只列 `m_onClickSound` 非空）

> 排序 = 非空 cue 颗数降序。**「我们建了」= 该 prefab 有对应窗/件（文件名见括号）**；「未建」= 我们这边没有那扇窗，逐颗列出仅供以后建窗时照接。
>
> **「是否与默认按钮音不同」**：cue 写 `ButtonClick` 的行 = **与默认音相同**（全表只有 **15 颗**，全是 Toggle）；其余 **409 颗**都不同。

#### C-1. 我们有对应窗/件的 prefab（121 个根 · 315 颗）

##### Collection Menu Variant — **29 颗** · 对应：Shell/CollectionWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Owned Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tab Buttons/CardBacks` | `ButtonClick` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Cost Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tab Buttons/Cards` | `ButtonClick` |
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Cost Filter/Content/Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Shared/Close Button` | `ButtonClick Positive` |
| Button | `Content Area/Tabs/Alternate Art Tab/Header Filters/Filters/Clear Filter Button` | `ButtonClick Positive` |
| Button | `Content Area/Tabs/CardsTab/Header Filters/Filters/Clear Filter Button` | `ButtonClick Positive` |
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Type Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Upgradable Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Upgradable Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Cardback Tab/Header/Filters/Clear Filter Button` | `ButtonClick Positive` |
| Toggle | `Content Area/Tabs/Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Owned Toggle (1)` | `Select Toggle` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Army Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Owned Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tab Buttons/Alternate Art` | `ButtonClick` |
| Toggle | `Content Area/Tabs/Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Army Filter/Content/Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Alternate Art Tab/Header/Select Art Button Right` | `ButtonClick Back` |
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Rarity FIlter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/Select Deck Tab/Header/Filter Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Select Deck Tab/Header/Filters/Clear Filter Button` | `ButtonClick Positive` |
| Toggle | `Content Area/Tab Buttons/Deck` | `ButtonClick` |
| Toggle | `Content Area/Tabs/Select Deck Tab/Decks Tab/Collection Display/Deck Filters/Filters/Army Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Type Filter/Content/Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Alternate Art Tab/Header/Select Art Button Left` | `ButtonClick Back` |
| Button | `Content Area/Tabs/Select Deck Tab/Header/Control Buttons/Create` | `ButtonClick Positive` |
| Toggle | `Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Army Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Rarity FIlter/Content/Toggle` | `Select Toggle` |
| Button | `Content Area/Tabs/Select Deck Tab/Header/Control Buttons/Import` | `ButtonClick Positive` |

##### Player Profile Window — **19 颗** · 对应：Shell/PlayerProfileWindow.cs + AvatarTab / TitleTab / ProfileTab / AchievementsMenu

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Menu Area/Tab  Area/Tab Content/Avatar Tab/Selected Item Panel/Toggle borde` | `SelectAvatar` |
| Toggle | `Menu Area/Tab Buttons/Ranked` | `Select Toggle` |
| Toggle | `Menu Area/Tab Buttons/Avatar Button` | `Select Toggle` |
| Button | `Menu Area/Tab  Area/Tab Content/Title Tab/Selected Item Panel/Toggle borde` | `SelectAvatar` |
| Toggle | `Menu Area/Tab Buttons/Profile Button` | `Select Toggle` |
| Toggle | `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle` | `Select Toggle` |
| Button | `Menu Area/Tab  Area/Tab Content/Profile Tab/Ranking/Highest Rank/LeaderboardButton` | `ButtonClick Positive` |
| Button | `Menu Area/Tab  Area/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Menu Area/Tab  Area/Tab Content/Avatar Tab/Selected Item Panel/Select Avatar Button` | `SelectAvatar` |
| Button | `Menu Area/Tab  Area/Tab Content/Title Tab/Selected Item Panel/Select Avatar Button` | `SelectAvatar` |
| Toggle | `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle (3)` | `Select Toggle` |
| Toggle | `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle (2)` | `Select Toggle` |
| Button | `Menu Area/Tab  Area/Tab Content/Profile Tab/ChooseNameWindow/Generic Close Button Green` | `ButtonClick Back` |
| Toggle | `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle (1)` | `Select Toggle` |
| Toggle | `Menu Area/Tab Buttons/Title Button` | `Select Toggle` |
| Button | `Menu Area/Tab  Area/Tab Content/Profile Tab/Ranking/Current Rank/LeaderboardButton` | `ButtonClick Positive` |
| Toggle | `Menu Area/Tab Buttons/Battle Log Button` | `Select Toggle` |
| Toggle | `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle (4)` | `Select Toggle` |
| Toggle | `Menu Area/Tab Buttons/Trophies` | `Select Toggle` |

##### Main Menu Settings Window — **16 颗** · 对应：Shell/SettingsWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Menu Area/Mask Tabs buttons/Tab Buttons/Graphics` | `Select Toggle` |
| Toggle | `Menu Area/Mask Tabs buttons/Tab Buttons/Support` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/General Tab/Checkboxes/Disable Bots` | `Select Toggle` |
| Button | `Menu Area/Generic Close Button` | `ButtonClick Back` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Android extra compatibility` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Hi FPS toggl` | `Select Toggle` |
| Toggle | `Menu Area/Mask Tabs buttons/Tab Buttons/Media` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/General Tab/Checkboxes/Touch Input` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Small Screen Size Toggle` | `Select Toggle` |
| Toggle | `Menu Area/Mask Tabs buttons/Tab Buttons/Account` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Auto Zoom Toggle` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Vsync` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/General Tab/Checkboxes/Disable Notifications` | `Select Toggle` |
| Toggle | `Menu Area/Mask Tabs buttons/Tab Buttons/General` | `Select Toggle` |
| Toggle | `Menu Area/Tab Content/Graphics Tab/Content/Scroll View/Viewport/Content/Use super sampling` | `Select Toggle` |
| Button | `Menu Area/Tab Content/Account Tab/Login Window/Generic Close Button Green` | `ButtonClick Back` |

##### Deck Editing Menu — **12 颗** · 对应：Shell/DeckEditingWindow.cs + Deck/DeckRuntime.cs（**另一条输入路径**，不走 MenuDraw.Hit）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Owned Toggle` | `Select Toggle` |
| Button | `Content Area/Header/Close` | `ButtonClick Positive` |
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Upgradable Toggle` | `Select Toggle` |
| Button | `Content Area/Header/Filters/Generic Simplified UI Button_updated` | `ButtonClick Positive` |
| Toggle | `Content Area/Header/Filters` | `Select Toggle` |
| Toggle | `Content Area/Cosmetic Display/Cosmetic FIlter/Filters/Owned Toggle` | `Select Toggle` |
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Type Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Cost Filter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Rarity FIlter/Content/Toggle` | `Select Toggle` |
| Toggle | `Content Area/Card Display/Card Filters/Scroll View/Viewport/Filters/Army Filter/Content/Toggle` | `Select Toggle` |
| Button | `Content Area/Sidebar/Footer/Done` | `ButtonClick Positive` |
| Toggle | `Content Area/Cosmetic Display/Cosmetic FIlter/Filters/Army Filter/Content/Toggle` | `Select Toggle` |

##### Rewards Base Submenu Variant — **10 颗** · 对应：Shell/RewardsWindow.cs + MissionsTab / CampaignTab / ForgeTab

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content Area/Tabs/Campaign Tab/Premium Panel/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Content Area/Tabs/Missions Tab/Normal Missions/Special Missions/Daily Login Container/raycast target` | `Purchase Click` |
| Toggle | `Content Area/Tab Buttons/MissionsRewardsButton` | `ButtonClick` |
| Toggle | `Content Area/Tab Buttons/Forge Button` | `ButtonClick` |
| Toggle | `Content Area/Tab Buttons/CampaignRewardsButton` | `ButtonClick` |
| Button | `Content Area/Tabs/Campaign Tab/Campaign Header/Premium Button Container/Premium Button` | `ButtonClick Positive` |
| Button | `Content Area/Tabs/Missions Tab/Weekly Mission Holder/Weekly Mission/raycast target` | `Purchase Click` |
| Toggle | `Content Area/Tab Buttons/Menu Navigation Panel Button` | `ButtonClick` |
| Button | `Content Area/Tabs/Campaign Tab/Campaign Header/Debug Point Button` | `ButtonClick Positive` |
| Button | `Content Area/Tabs/Missions Tab/Normal Missions/Special Missions/Daily Skulls Mission Container/raycast target` | `Purchase Click` |

##### RankedEventWindowV2 — **9 颗** · 对应：Shell/RankedEventWindow.cs（+ LiveOpsEventWindow 基类）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `DEBUG/DebugBattle/Continue Button` | `MenuStartGame` |
| Button | `Ranked Division Info/ChangeRankedToggle/ChangeRankedButton` | `Select Toggle` |
| Button | `Ranked Deck Selection/Deck Buttons/Next Deck Button` | `ButtonClick Back` |
| Button | `Ranked Deck Selection/Deck Buttons/Change Deck Button` | `ButtonClick Back` |
| Button | `Ranked Deck Selection/Deck Buttons/View Deck Button` | `ButtonClick Back` |
| Button | `Ranked Division Info/LeaderboardButton` | `ButtonClick Positive` |
| Button | `Ranked Deck Selection/No Deck Text/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Ranked Deck Selection/Deck Buttons/Previous Deck Button` | `ButtonClick Back` |
| Button | `To Battle Button` | `ButtonClick Positive` |

##### ChatPanel — **8 颗** · 对应：Shell/ChatPanel.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Holder/Chat/Enter Text/Button` | `UI Send Message` |
| Button | `Holder/Chat/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Holder/Chat/Player Options Panel/Buttons/Block` | `ButtonClick Positive` |
| Button | `Holder/Chat/Player Options Panel/Buttons/Profile` | `ButtonClick Positive` |
| Button | `Holder/Chat/Player Options Panel/Buttons/Challenge` | `ButtonClick Positive` |
| Toggle | `Holder/Chat/Tab Buttons/Orange Tab Toggle` | `Select Toggle` |
| Button | `Holder/Chat/Player Options Panel/Buttons/Add as a friend` | `ButtonClick Positive` |
| Button | `Holder/Chat/Player Options Panel/Buttons/Report` | `ButtonClick Positive` |

##### Deck info Popup — **8 颗** · 对应：Shell/DeckInfoPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Deck Options/Share Button` | `ButtonClick Back` |
| Button | `Buttons/Edit Deck` | `ButtonClick Positive` |
| Button | `Deck Options/Delete Button` | `ButtonClick Back` |
| Button | `Deck Options/Share On Chat` | `ButtonClick Back` |
| Button | `Deck Options/Duplicate Button` | `ButtonClick Back` |
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Buttons/Practice Deck` | `ButtonClick Positive` |
| Button | `Buttons/Select Deck` | `ButtonClick Positive` |

##### Member Options Panel — **8 颗** · 对应：Shell/AllianceMemberOptionsPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Buttons/Promote` | `ButtonClick Positive` |
| Button | `Buttons/Profile` | `ButtonClick Positive` |
| Button | `Buttons/Quit` | `ButtonClick Positive` |
| Button | `Buttons/Add as a friend` | `ButtonClick Positive` |
| Button | `Buttons/Challenge` | `ButtonClick Positive` |
| Button | `Buttons/Kick` | `ButtonClick Positive` |
| Button | `Buttons/Debug Add Skulls` | `ButtonClick Positive` |
| Button | `Buttons/Demote` | `ButtonClick Positive` |

##### SkirmishModeEventWindow — **8 颗** · 对应：Shell/SkirmishEventWindow.cs（LiveOpsEventWindow）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Ranked Deck Selection/No Deck Text/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Ranked Deck Selection/Deck Buttons/Previous Deck Button` | `ButtonClick Back` |
| Button | `To Battle Button` | `ButtonClick Positive` |
| Button | `Ranked Deck Selection/Deck Buttons/Change Deck Button` | `ButtonClick Back` |
| Button | `Ranked Deck Selection/Deck Buttons/Next Deck Button` | `ButtonClick Back` |
| Button | `DEBUG/DebugBattle/Continue Button` | `MenuStartGame` |
| Button | `Ranked Deck Selection/Deck Buttons/View Deck Button` | `ButtonClick Back` |

##### RankedEventWindow — **6 颗** · 对应：Shell/RankedEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `DeckSelectionContinueButton/Continue Button` | `MenuStartGame` |
| Button | `DEBUG/DebugBattle/Continue Button` | `MenuStartGame` |
| Button | `Deck info/ViewDeckButton` | `ButtonClick Positive` |
| Button | `Deck info/ChangeDeckButton` | `ButtonClick Positive` |
| Button | `Ranked Division Info/LeaderboardButton` | `ButtonClick Positive` |
| Button | `ChangeRankedToggle/ChangeRankedButton` | `Select Toggle` |

##### Ranked Deck Selection — **5 颗** · 对应：Shell/LiveOpsEventWindow.cs（Deck Buttons）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `No Deck Text/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Deck Buttons/Change Deck Button` | `ButtonClick Back` |
| Button | `Deck Buttons/View Deck Button` | `ButtonClick Back` |
| Button | `Deck Buttons/Previous Deck Button` | `ButtonClick Back` |
| Button | `Deck Buttons/Next Deck Button` | `ButtonClick Back` |

##### RankedSkirmishLeaderboardPopup — **5 颗** · 对应：Shell/LeaderboardWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Tab Buttons/Alliances` | `Select Toggle` |
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Toggle | `Tab Buttons/Armies` | `Select Toggle` |
| Toggle | `Tab Buttons/Player` | `Select Toggle` |
| Button | `Ranking Display/Generic Simplified UI Button_updated` | `ButtonClick Positive` |

##### RankedClassicLeaderboardPopup Variant — **4 颗** · 对应：Shell/LeaderboardWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Tab Buttons/Player` | `Select Toggle` |
| Button | `Ranking Display/Generic Simplified UI Button_updated` | `ButtonClick Positive` |
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Toggle | `Tab Buttons/Armies` | `Select Toggle` |

##### Social Submenu Variant — **4 颗** · 对应：Shell/SocialWindow.cs + FriendsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content Area/Tabs/Friends Tab/Header/Find players panel/Add Friend Button` | `ButtonClick Positive` |
| Toggle | `Content Area/Tab Buttons/Friends Tab Button` | `ButtonClick` |
| Button | `Content Area/Tabs/Friends Tab/Header/Find players panel/Instant duel Button` | `ButtonClick Positive` |
| Toggle | `Content Area/Tab Buttons/Alliances Tab Button` | `ButtonClick` |

##### Alliance Event Score Panel — **3 颗** · 对应：Shell/AllianceEventScorePanel.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `No Alliance/Leaderboard Button` | `ButtonClick Positive` |
| Button | `No Alliance/Join Alliances Button` | `ButtonClick Positive` |
| Button | `In Alliance/View Leaderboard Button` | `ButtonClick Positive` |

##### Base Offer Popup — **3 颗** · 对应：Shell/BaseOfferPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### Campaign Tab — **3 颗** · 对应：Shell/CampaignTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Campaign Header/Premium Button Container/Premium Button` | `ButtonClick Positive` |
| Button | `Premium Panel/Generic Simplified UI Button` | `ButtonClick Positive` |
| Button | `Campaign Header/Debug Point Button` | `ButtonClick Positive` |

##### General Basic Offer Popup Booster_CardOrAltArt — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |

##### General Basic Offer Popup Booster_CardOrAltArt_AvatarORTitle — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Booster_CardOrAltArt_Cardback_Avatar_Title — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Just Foreground — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant 2 Currencies — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Booster + 2 Currencies — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Booster_avatar_cardback_title — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Booster_avatar_resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Variant Booster_cardback_resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Booster_title_resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Variant Deck_cardback_avatar — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Expansion pass — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |

##### General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |

##### General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title_resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |

##### General Basic Offer Popup Variant Premium_Premium_cardback_avatar — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Premium_Resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Variant Premium_booster_title_avatarOrResource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant Single Item Type — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `Preview` | `ButtonClick Positive` |

##### General Basic Offer Popup Variant avatarOrTitle_resource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### General Basic Offer Popup Variant cardback_premiumOrAvatarOrResource_titleOrResource — **3 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Preview` | `ButtonClick Positive` |

##### Inbox Menu — **3 颗** · 对应：Shell/InboxWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Content/Reset Button` | `ButtonClick Positive` |
| Button | `Content/Message Display/Content/Scroll View/Viewport/Content/Claim Button/Claim Button` | `ButtonClick Positive` |

##### Missions Tab — **3 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Weekly Mission Holder/Weekly Mission/raycast target` | `Purchase Click` |
| Button | `Normal Missions/Special Missions/Daily Login Container/raycast target` | `Purchase Click` |
| Button | `Normal Missions/Special Missions/Daily Skulls Mission Container/raycast target` | `Purchase Click` |

##### Alliance Trophy Info Popup — **2 颗** · 对应：Shell/TrophyInfoPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Toggle | `window/RightSide/Controls/selectButton/Checkbox` | `Select Toggle` |

##### Booster Info Popup — **2 颗** · 对应：Shell/BoosterInfoPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |
| Button | `window/Text/Purchase buttons/Price Display/Generic UI Button` | `Purchase Click` |

##### Campaign Reward Window — **2 颗** · 对应：Shell/CampaignRewardWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Scroll View/Viewport/Content/Premium Rewards/Rewards/Unlock Button` | `ButtonClick Positive` |
| Button | `Content/Scroll View/Viewport/Content/Base Rewards/Rewards/Unlock Button` | `ButtonClick Positive` |

##### Deck Selection Popup with Tabs — **2 颗** · 对应：Shell/DeckSelectionPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Button | `Deck Display/Header/Practice buttons/Generic Simplified UI Button` | `ButtonClick Positive` |

##### MessagePopupWindowDuel — **2 颗** · 对应：Shell/DuelPopupWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Rounded Button Green` | `ButtonClick Back` |
| Button | `Window/Buttons/Button Classic` | `ButtonClick Positive` |

##### Practice Mode Menu — **2 颗** · 对应：Shell/PracticeModePopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `DeckSelectionContinueButton/Continue Button` | `MenuStartGame` |
| Button | `Deck info/General container/Generic Simplified UI Button` | `ButtonClick Positive` |

##### Profile Cosmetic Tab — **2 颗** · 对应：Shell/ProfileTab.cs（装饰页）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Selected Item Panel/Select Avatar Button` | `SelectAvatar` |
| Button | `Selected Item Panel/Toggle borde` | `SelectAvatar` |

##### Shop Menu Variant — **2 颗** · 对应：Shell/ShopWindow.cs + ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content Area/Tabs/Shop Tab/Packs Scroll View/Viewport/Content/Comsetic Item Shop Container/raycast target` | `Purchase Click` |
| Toggle | `Content Area/Tab Buttons/Shop Icon` | `ButtonClick` |

##### Achievement Type Toggle — **1 颗** · 对应：Shell/AchievementsMenu.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `Select Toggle` |

##### Army Item Button — **1 颗** · 对应：Shell/ForgeTab.cs / CampaignTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `SelectAvatar` |

##### Army Item Button No Arrow Variant — **1 颗** · 对应：Shell/CampaignTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `SelectAvatar` |

##### Base Submenu — **1 颗** · 对应：Shell/RewardsWindow.cs（基类）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Content Area/Tab Buttons/Menu Navigation Panel Button` | `ButtonClick` |

##### Battle Log Popup — **1 颗** · 对应：Shell/BattleLogPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Close Button` | `ButtonClick Back` |

##### Booster Offer Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Campaign Army Item Button — **1 颗** · 对应：Shell/CampaignTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `SelectAvatar` |

##### Card Shop Tab — **1 颗** · 对应：Shell/ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Packs Scroll View/Viewport/Content/Catalog Item Shop Container/raycast target` | `Purchase Click` |

##### Card Shop VIP Tab Variant — **1 颗** · 对应：Shell/ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Packs Scroll View/Viewport/Content/Catalog Item Shop Container/raycast target` | `Purchase Click` |

##### Catalog Item Shop Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Checkbox — **1 颗** · 对应：Shell/SettingsWindow.cs / CollectionWindow.cs / TrophyInfoPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `Select Toggle` |

##### ChooseNameWindow — **1 颗** · 对应：Shell/ProfileTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Login Window/Generic Close Button Green` | `ButtonClick Back` |

##### Cosmetic Item Shop Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Daily Bonus Shop Container — **1 颗** · 对应：Shell/OfferContainer.cs（同类容器）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `ButtonClick Positive` |

##### Daily Login Bonus Container — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Daily Shop Tab — **1 颗** · 对应：Shell/ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Packs Scroll View/Viewport/Content/Catalog Item Shop Container/raycast target` | `Purchase Click` |

##### Daily Skulls Mission Container — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Daily Skulls Mission Container Small — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Daily Skulls Mission Container Variant  Expansion Pass — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Daily Streak Popup — **1 颗** · 对应：Shell/DailyStreakPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Streak Failed/Generic Simplified UI Button` | `ButtonClick Positive` |

##### Deck Selection Popup — **1 颗** · 对应：Shell/DeckSelectionPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Close Button Orange` | `ButtonClick Back` |

##### Deck Selector Menu Item — **1 颗** · 对应：Shell/DeckSelectionPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Button border` | `ButtonClick Back` |

##### DeckSelectionContinueButton — **1 颗** · 对应：Shell/LiveOpsEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Continue Button` | `MenuStartGame` |

##### EnergySinglePlayerOnlyEventWindow — **1 颗** · 对应：Shell/EnergySinglePlayerOnlyEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Reward Progress Panel/Scoring Bar Event Score Info/Generic Simplified UI Button` | `ButtonClick Positive` |

##### Forge Army Item Button — **1 颗** · 对应：Shell/ForgeTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `SelectAvatar` |

##### Forge Menu Reward Button — **1 颗** · 对应：Shell/ForgeTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic UI Button` | `ButtonClick Positive` |

##### Friends Menu Demo — **1 颗** · 对应：Shell/FriendsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Window/Generic Close Button` | `ButtonClick Back` |

##### Gacha Tab — **1 颗** · 对应：（未建）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Chest panel/Generic Simplified UI Button (1)` | `ButtonClick Positive` |

##### General Basic Offer Container Booster_CardOrAltArt — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant 2 Currencies — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Booster + 2 Currencies — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Booster_avatar_cardback_title — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Booster_avatar_resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Booster_cardback_resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Booster_title_resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Deck_cardback_avatar — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Premium_Booster_avatar_cardback_title — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Premium_Premium_cardback_avatar — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Premium_Resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Premium_booster_title_avatarOrResource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant Single Item Type — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant avatarOrTitle_resource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource — **1 颗** · 对应：Shell/BaseOfferPopup.cs / OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Generic Options Panel — **1 颗** · 对应：Shell/GenericOptionsPanel.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Template` | `ButtonClick Positive` |

##### Import Deck Popup — **1 颗** · 对应：Shell/ImportDeckPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Window/Generic Close Button Green` | `ButtonClick Back` |

##### InApp Shop Container — **1 颗** · 对应：（未建）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `ButtonClick Positive` |

##### InApp Shop Container Disabled — **1 颗** · 对应：（未建）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `ButtonClick Positive` |

##### Item Shop Tab — **1 颗** · 对应：Shell/ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Packs Scroll View/Viewport/Content/Catalog Item Shop Container/raycast target` | `Purchase Click` |

##### Item Shop Tab No Automatic Ordering — **1 颗** · 对应：Shell/ShopTabPage.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Packs Scroll View/Viewport/Content/Catalog Item Shop Container/raycast target` | `Purchase Click` |

##### Menu Navigation Panel Button — **1 颗** · 对应：Shell/MainMenuSubmenuWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `ButtonClick` |

##### Mission Container Vertical — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Offer Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Orange Tab Toggle — **1 颗** · 对应：Shell/ChatPanel.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `Select Toggle` |

##### Parent Event Window — **1 颗** · 对应：Shell/BaseEventPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Content Area/Tab Buttons/Menu Navigation Panel Button` | `ButtonClick` |

##### Prebuilt Pack UI — **1 颗** · 对应：（未建）

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Cards container/Select Button` | `ButtonClick Positive` |

##### Purchase Premium Window — **1 颗** · 对应：Shell/PurchasePremiumWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Close Button Orange` | `ButtonClick Back` |

##### Ranked Army Selector Container V2 — **1 颗** · 对应：Shell/LiveOpsEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `SelectAvatar` |

##### Ranked Boost Reward Event Window — **1 颗** · 对应：Shell/RankedRewardEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `window/Generic Close Button Orange` | `ButtonClick Back` |

##### Ranked Division Info — **1 颗** · 对应：Shell/RankedDivisionInfo.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `LeaderboardButton` | `ButtonClick Positive` |

##### Ranked Leaderboard Display — **1 颗** · 对应：Shell/LeaderboardWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Simplified UI Button_updated` | `ButtonClick Positive` |

##### ReRollPopup Variant — **1 颗** · 对应：Shell/MissionRerollPopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Window/Buttons/ButtonLeft` | `ButtonClick Negative` |

##### Referral Container — **1 颗** · 对应：Shell/ReferralPopupWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Reward Window — **1 颗** · 对应：Shell/RewardWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Collect Button` | `ButtonClick Positive` |

##### Settings Menu Tab Toggle — **1 颗** · 对应：Shell/SettingsWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `(prefab 根)` | `Select Toggle` |

##### Shop Item Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Small General Basic Offer Container Variant Single Item Type — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Tutorial Mode Menu — **1 颗** · 对应：Shell/TutorialModePopup.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `PlayTutorialButton` | `ButtonClick Positive` |

##### Two Sides Event Window — **1 颗** · 对应：Shell/EnergySinglePlayerOnlyEventWindow.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Event On Going Tab/Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button` | `ButtonClick Positive` |

##### Webshop Offer Container — **1 颗** · 对应：Shell/OfferContainer.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

##### Weekly Mission Container — **1 颗** · 对应：Shell/MissionsTab.cs

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `raycast target` | `Purchase Click` |

#### C-2. 我们还没建的 prefab（14 个根 · 109 颗）

> 这些**不是查不到**：prefab 与 cue 都在本地，只是我们这边还没有对应窗。等建那扇窗时直接照这张表接。

##### Give Feedback Popup menu — **42 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (3)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (7)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox (4)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 1/S1Q1/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (8)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q1/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (5)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox (3)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox (4)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 3/S3Q10/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (3)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (4)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 1/S1Q1/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (9)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 3/S3Q10/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (5)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox (5)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (10)` | `Select Toggle` |
| Button | `Window/Generic Close Button` | `ButtonClick Back` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (6)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 1/S1Q1/Questions List/Checkbox (4)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox (4)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 3/S3Q10/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox (5)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox (3)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (6)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q4/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 3/S3Q10/Questions List/Checkbox (3)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q3/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q1/Questions List/Checkbox (1)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 4/S4Q2/Questions List/Checkbox (2)` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 1/S1Q1/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 2/S2Q3/Questions List/Checkbox` | `Select Toggle` |
| Toggle | `Window/Scroll Rect/Questions container/Section 1/S1Q1/Questions List/Checkbox (3)` | `Select Toggle` |

##### Draft Mode Timed Mode Window — **20 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Draft Mode Intro State/Start Button` | `ButtonClick Positive` |
| Button | `Draft Mode Select Warlord State/Generic Simplified UI Button_updated` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/Reward Info Panel/Reward Info/Collect Button` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Card List Toggle` | `Select Toggle` |
| Button | `Draft Mode Ongoing State/MainContent/Scaled Content/Reset Event Button` | `ButtonClick Negative` |
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Premium Ten Button` | `ButtonClick Positive` |
| Button | `Draft Mode Select Warlord State/DEBUG_REROLL_BUTTON` | `ButtonClick Positive` |
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Premium Button` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/Debug Win` | `ButtonClick Positive` |
| Button | `Draft Mode Pay State/Alliance Scoring Panel/In Alliance/View Leaderboard Button` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Alliance Info Toggle` | `Select Toggle` |
| Button | `Draft Mode Pay State/Alliance Scoring Panel/No Alliance/Join Alliances Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Join Alliances Button` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/To Battle Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Leaderboard Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/In Alliance/View Leaderboard Button` | `ButtonClick Positive` |
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Free Button` | `ButtonClick Positive` |
| Button | `Draft Mode Pay State/Alliance Scoring Panel/No Alliance/Leaderboard Button` | `ButtonClick Positive` |
| Button | `Draft Mode Select Warlord State/Generic Card Selector Menu/Viewport/Content/Generic Select Card Container/Content/Select Button` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Deck Info Toggle` | `Select Toggle` |

##### Draft Mode Menu Demo — **17 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Premium Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/In Alliance/View Leaderboard Button` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Deck Info Toggle` | `Select Toggle` |
| Button | `Draft Mode Select Warlord State/Generic Simplified UI Button_updated` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/Reward Info Panel/Reward Info/Collect Button` | `ButtonClick Positive` |
| Button | `Draft Mode Select Warlord State/DEBUG_REROLL_BUTTON` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Card List Toggle` | `Select Toggle` |
| Button | `Draft Mode Ongoing State/To Battle Button` | `ButtonClick Positive` |
| Toggle | `Draft Mode Deck Info Panel/Buttons/Alliance Info Toggle` | `Select Toggle` |
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Premium Ten Button` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/MainContent/Scaled Content/Reset Event Button` | `ButtonClick Negative` |
| Button | `Draft Mode Intro State/Start Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Leaderboard Button` | `ButtonClick Positive` |
| Button | `Draft Mode Select Warlord State/Generic Card Selector Menu/Viewport/Content/Generic Select Card Container/Content/Select Button` | `ButtonClick Positive` |
| Button | `Draft Mode Pay State/Scale menu/ButtonsLayout/Free Button` | `ButtonClick Positive` |
| Button | `Draft Mode Deck Info Panel/content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Join Alliances Button` | `ButtonClick Positive` |
| Button | `Draft Mode Ongoing State/Debug Win` | `ButtonClick Positive` |

##### Draft Mode Deck Info Panel — **6 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Toggle | `Buttons/Card List Toggle` | `Select Toggle` |
| Button | `content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Leaderboard Button` | `ButtonClick Positive` |
| Toggle | `Buttons/Alliance Info Toggle` | `Select Toggle` |
| Toggle | `Buttons/Deck Info Toggle` | `Select Toggle` |
| Button | `content/Alliance Detail View/Alliance Event Score Panel/In Alliance/View Leaderboard Button` | `ButtonClick Positive` |
| Button | `content/Alliance Detail View/Alliance Event Score Panel/No Alliance/Join Alliances Button` | `ButtonClick Positive` |

##### Debug Reward Window — **4 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Collect` | `ButtonClick Positive` |
| Button | `Content/Preview` | `ButtonClick Positive` |
| Button | `Content/Scroll View/Viewport/Content/Reward/Generic Simplified UI Button_updated (1)` | `ButtonClick Positive` |
| Button | `Content/Add` | `ButtonClick Positive` |

##### DraftLeaderboardPopup — **4 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Ranking Display/Generic Simplified UI Button_updated` | `ButtonClick Positive` |
| Toggle | `Tab Buttons/Armies` | `Select Toggle` |
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Toggle | `Tab Buttons/Alliances` | `Select Toggle` |

##### Sunset Popup — **4 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Convert To Legions enable/Play Legions/Download Legions Content/Play Legions Button` | `ButtonClick Positive` |
| Button | `Convert To Legions enable/Convert to Legions/Conversion not initialized/Convert coins Button` | `ButtonClick Positive` |
| Button | `Convert To Legions Disabled/Play Legions/Download Legions Content/Play Legions Button` | `ButtonClick Positive` |
| Button | `Close Button` | `ButtonClick Back` |

##### Draft Mode Ongoing State — **3 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Reward Info Panel/Reward Info/Collect Button` | `ButtonClick Positive` |
| Button | `Reset Event Button` | `ButtonClick Negative` |
| Button | `Debug Win` | `ButtonClick Positive` |

##### Expansion Pass Tab — **2 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Simplified UI Button (1)` | `ButtonClick Positive` |
| Button | `Purchase premium Panel/IsNotPremium/Generic Simplified UI Button` | `ButtonClick Positive` |

##### Rate Popup — **2 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Buttons/Generic Simplified UI Button_updated (1)` | `ButtonClick Positive` |
| Button | `Buttons/Generic Simplified UI Button_updated` | `ButtonClick Positive` |

##### WhereToGetCardsPopup — **2 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Generic Close Button Orange` | `ButtonClick Back` |
| Toggle | `Scroll View/Viewport/Content/Section For Reference/Header/Toggle` | `ButtonClick` |

##### Draft Mode Expiring Popup — **1 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Content/Generic Simplified UI Button_updated` | `ButtonClick Positive` |

##### Expansion Pass Missions Tab — **1 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Daily Skulls Mission Container/raycast target` | `Purchase Click` |

##### Expansion Pass Reward Container — **1 颗** · 未建

| 组件 | 节点（相对 prefab 根） | cue |
|---|---|---|
| Button | `Level up/Price Display Button (1)/Generic UI Button` | `UI Purchase 2` |

### D. 另外两个包的非空 cue（逐颗）

#### 主菜单场景实例（`Game UI/…`） — 4 颗

| 组件 | 节点 | cue | 对应我们 |
|---|---|---|---|
| Button | `Game UI/Main  Canvas/MainMenu/Safe area Only Horizontal/3 - PopUp Holder/Card Displayer Menu For Menu/Card Options Panel/Panel/Alternate Art Panel/Select Art Button Right` | `ButtonClick Back` | CardDetailPopup.cs:1279 |
| Button | `Game UI/Main  Canvas/MainMenu/Safe area Only Horizontal/3 - PopUp Holder/Card Displayer Menu For Menu/Card Options Panel/Panel/Alternate Art Panel/Select Art Button Left` | `ButtonClick Back` | CardDetailPopup.cs:1279 |
| Button | `Game UI/Main  Canvas/MainMenu/Safe area Only Horizontal/3 - PopUp Holder/Card Displayer Menu For Menu/Card Options Panel/Panel/Upgrade Panel/Content/Upgrade` | `ButtonClick Positive` | CardDetailPopup.cs:975 |
| Button | `Game UI/Main  Canvas/MainMenu/Safe area Only Horizontal/3 - PopUp Holder/Card Displayer Menu For Menu/Card Options Panel/Panel/Crafting Panel/Content/Craft` | `ButtonClick Positive` | CardDetailPopup.cs:881/886 |

#### 通用窗（`generalgamewindows`） — 14 颗

| 组件 | 节点 | cue | 对应我们 |
|---|---|---|---|
| Button | `MessagePopupWindow/Window/Buttons/Generic UI Button` | `ButtonClick Positive` | GenericOptionsPanel.cs:493 / DuelPopupWindow（待核） |
| Button | `Debug Reactivate Event Window/Window/Buttons/Clear Events + Shared Data` | `ButtonClick Negative` | （调试窗，未建） |
| Button | `Referral Popup/window/content/Input View/Generic Simplified UI Button` | `ButtonClick Positive` | ReferralPopupWindow.cs:534 |
| Button | `MessagePopupWindow2Buttons/Window/Buttons/ButtonLeft` | `ButtonClick Negative` | GenericOptionsPanel.cs:493 / DuelPopupWindow（待核） |
| Button | `Referral Popup/window/Generic Close Button Orange` | `ButtonClick Back` | ReferralPopupWindow.cs:404 |
| Button | `Debug Reactivate Event Window/Window/Buttons/Clear Shared Data` | `ButtonClick Negative` | （调试窗，未建） |
| Button | `Debug Reactivate Event Window/Window/Buttons/Activate Button` | `ButtonClick Positive` | （调试窗，未建） |
| Button | `Debug Reactivate Event Window/Window/Scroll View/Viewport/Content/Debug Event Entry/Activate Event` | `ButtonClick Positive` | （调试窗，未建） |
| Button | `Debug Play Window/Content/Play` | `ButtonClick Positive` | （调试窗，未建） |
| Button | `MessagePopupWindow2Buttons/Window/Buttons/ButtonRight` | `ButtonClick Positive` | GenericOptionsPanel.cs:493 / DuelPopupWindow（待核） |
| Button | `Debug Reactivate Event Window/Window/Buttons/Clear Persistence Button` | `ButtonClick Negative` | （调试窗，未建） |
| Button | `Debug Reactivate Event Window/Window/Scroll View/Viewport/Content/Debug Event Entry (1)/Activate Event` | `ButtonClick Positive` | （调试窗，未建） |
| Button | `Debug Reactivate Event Window/Window/Generic Close Button Green` | `ButtonClick Back` | （调试窗，未建） |
| Button | `Debug Event Entry/Activate Event` | `ButtonClick Positive` | （调试窗，未建） |

---

## 三、我们侧（`Shell/` 全部命中区）

> 现值一栏默认 = **`ButtonClick`（= 现在 `WindowButton.Click()` 播的默认音）**。
> 解析口径：`MenuDraw.Hit(...)` + `MenuDraw.AddHit(...)` + 各窗包装的 `Hit(...)` / `AddHit(...)`（第 1 个字符串实参 = 节点名）。**共 175 处源码站点**。
> ⚠️ 同一个辅助方法（`GenToggleRow` / `GenButton` / `RowButton` / `Btn` / `Dropdown` 等）会在循环里建多颗钮 —— 表里是**源码站点**，不是运行期颗数。

| 文件:行 | 节点名 | 所属窗 | 现在播什么 |
|---|---|---|---|
| `Shell/AchievementsMenu.cs:247` | `Hit` | 个人资料 · 成就页 | `ButtonClick`（默认） |
| `Shell/AllianceEventScorePanel.cs:134` | `ButtonHit` | 联盟活动分数面板 | `ButtonClick`（默认） |
| `Shell/AllianceEventScorePanel.cs:168` | `ButtonHit` | 联盟活动分数面板 | `ButtonClick`（默认） |
| `Shell/AllianceEventScorePanel.cs:178` | `ButtonHit` | 联盟活动分数面板 | `ButtonClick`（默认） |
| `Shell/AllianceMemberOptionsPopup.cs:404` | `Hit` | 成员选项面板（Member Options Panel） | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:303` | `InfoHit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:304` | `TrophiesHit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:719` | `Hit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:838` | `Hit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:1234` | `Hit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:1252` | `Hit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceMemberTab.cs:1487` | `Hit` | 联盟成员页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:135` | `JoinHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:138` | `CreateHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:321` | `SearchHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:330` | `GoHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:469` | `InfoHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:492` | `InfoHit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:584` | `Hit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:652` | `Hit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:686` | `Hit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AllianceSearchTab.cs:703` | `Hit` | 联盟搜索页 | `ButtonClick`（默认） |
| `Shell/AvatarTab.cs:229` | `SelectHit` | 个人资料 · 头像页 | `ButtonClick`（默认） |
| `Shell/AvatarTab.cs:241` | `BordeHit` | 个人资料 · 头像页 | `ButtonClick`（默认） |
| `Shell/AvatarTab.cs:287` | `Hit` | 个人资料 · 头像页 | `ButtonClick`（默认） |
| `Shell/BaseEventPopup.cs:292` | `Hit` | 活动弹窗基类 | `ButtonClick`（默认） |
| `Shell/BaseOfferPopup.cs:768` | `Hit` | 通用报价弹窗（General Basic Offer Popup） | `ButtonClick`（默认） |
| `Shell/BaseOfferPopup.cs:979` | `Hit` | 通用报价弹窗（General Basic Offer Popup） | `ButtonClick`（默认） |
| `Shell/BaseOfferPopup.cs:1012` | `Hit` | 通用报价弹窗（General Basic Offer Popup） | `ButtonClick`（默认） |
| `Shell/BaseOfferPopup.cs:1046` | `Hit` | 通用报价弹窗（General Basic Offer Popup） | `ButtonClick`（默认） |
| `Shell/BattleLogPopup.cs:166` | `Hit` | 战斗日志弹窗 | `ButtonClick`（默认） |
| `Shell/BattleLogTab.cs:162` | `Hit` | 个人资料 · 战斗日志页 | `ButtonClick`（默认） |
| `Shell/BoosterInfoPopup.cs:376` | `Hit` | 卡包信息弹窗 | `ButtonClick`（默认） |
| `Shell/BoosterInfoPopup.cs:564` | `Hit` | 卡包信息弹窗 | `ButtonClick`（默认） |
| `Shell/BoosterInfoPopup.cs:607` | `Hit` | 卡包信息弹窗 | `ButtonClick`（默认） |
| `Shell/BoosterInfoPopup.cs:630` | `Hit` | 卡包信息弹窗 | `ButtonClick`（默认） |
| `Shell/BoosterPackOpenWindow.cs:566` | `Hit` | 开包窗 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/CampaignTab.cs:525` | `Hit` | 奖励 · 战役页 | `ButtonClick`（默认） |
| `Shell/CampaignTab.cs:742` | `Hit` | 奖励 · 战役页 | `ButtonClick`（默认） |
| `Shell/CampaignTab.cs:1097` | `ContinueHit` | 奖励 · 战役页 | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:517` | `VoiceHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:527` | `ShowTextHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:881` | `CraftHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:886` | `CraftHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:975` | `UpgradeHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:1201` | `BuyHit` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/CardDetailPopup.cs:1279` | `40k_general_bt_yellow` | 卡片详情（主菜单/收藏） | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:52` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:277` | `InputHit` | 聊天面板 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/ChatPanel.cs:282` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:317` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:345` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:422` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/ChatPanel.cs:621` | `Hit` | 聊天面板 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:1310` | `Hit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:1773` | `40k_general_bt_yellow` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:2205` | `Hit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:2370` | `Hit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:2625` | `FiltersHit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:2657` | `ClearFiltersHit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:2935` | `CloseHit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:3072` | `ImportHit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:3074` | `CreateHit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:3250` | `Hit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/CollectionWindow.cs:3283` | `Hit` | 收藏窗 | `ButtonClick`（默认） |
| `Shell/DeckInfoPopup.cs:906` | `WarlordHit` | 卡组信息弹窗 | `ButtonClick`（默认） |
| `Shell/DeckInfoPopup.cs:981` | `UI_Button_Mulligan` | 卡组信息弹窗 | `ButtonClick`（默认） |
| `Shell/DeckInfoPopup.cs:1060` | `40k_general_bt_yellow` | 卡组信息弹窗 | `ButtonClick`（默认） |
| `Shell/DeckInfoPopup.cs:1119` | `CloseHit` | 卡组信息弹窗 | `ButtonClick`（默认） |
| `Shell/DeckSelectionPopup.cs:461` | `RandomHit` | 卡组选择弹窗 | `ButtonClick`（默认） |
| `Shell/DeckSelectionPopup.cs:550` | `CloseHit` | 卡组选择弹窗 | `ButtonClick`（默认） |
| `Shell/DuelPopupWindow.cs:214` | `Hit` | 决斗弹窗 | `ButtonClick`（默认） |
| `Shell/DuelPopupWindow.cs:241` | `Hit` | 决斗弹窗 | `ButtonClick`（默认） |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:456` | `CollectHit` | 单人活动窗（Two Sides） | `ButtonClick`（默认） |
| `Shell/ForgeTab.cs:575` | `Help Icon Hit` | 奖励 · 锻造页 | `ButtonClick`（默认） |
| `Shell/ForgeTab.cs:719` | `Hit` | 奖励 · 锻造页 | `ButtonClick`（默认） |
| `Shell/ForgeTab.cs:887` | `ClaimHit` | 奖励 · 锻造页 | `ButtonClick`（默认） |
| `Shell/FriendsTab.cs:192` | `SearchFieldHit` | 社交 · 好友页 | `ButtonClick`（默认） |
| `Shell/FriendsTab.cs:201` | `Hit` | 社交 · 好友页 | `ButtonClick`（默认） |
| `Shell/FriendsTab.cs:210` | `Hit` | 社交 · 好友页 | `ButtonClick`（默认） |
| `Shell/FriendsTab.cs:416` | `Hit` | 社交 · 好友页 | `ButtonClick`（默认） |
| `Shell/GenericOptionsPanel.cs:493` | `Hit` | 通用选项面板 | `ButtonClick`（默认） |
| `Shell/ImportDeckPopup.cs:269` | `CloseHit` | 导入卡组弹窗 | `ButtonClick`（默认） |
| `Shell/InboxWindow.cs:432` | `Hit` | 收件箱 | `ButtonClick`（默认） |
| `Shell/InboxWindow.cs:564` | `RowHit` | 收件箱 | `ButtonClick`（默认） |
| `Shell/LeaderboardRow.cs:208` | `Hit` | 排行榜行 | `ButtonClick`（默认） |
| `Shell/LeaderboardWindow.cs:575` | `Hit` | 排行榜窗 | `ButtonClick`（默认） |
| `Shell/LeaderboardWindow.cs:656` | `Hit` | 排行榜窗 | `ButtonClick`（默认） |
| `Shell/LeaderboardWindow.cs:712` | `Hit` | 排行榜窗 | `ButtonClick`（默认） |
| `Shell/LeaderboardWindow.cs:734` | `Hit` | 排行榜窗 | `ButtonClick`（默认） |
| `Shell/LiveOpsEventWindow.cs:514` | `Hit` | 活动窗（排位/小冲突） | `ButtonClick`（默认） |
| `Shell/LiveOpsEventWindow.cs:582` | `CreateDeckHit` | 活动窗（排位/小冲突） | `ButtonClick`（默认） |
| `Shell/LiveOpsEventWindow.cs:803` | `Hit` | 活动窗（排位/小冲突） | `ButtonClick`（默认） |
| `Shell/LiveOpsEventWindow.cs:943` | `BattleHit` | 活动窗（排位/小冲突） | `ButtonClick`（默认） |
| `Shell/LiveOpsEventWindow.cs:967` | `HelpHit` | 活动窗（排位/小冲突） | `ButtonClick`（默认） |
| `Shell/MainMenuRuntime.cs:1779` | `ChatHit` | 主菜单 | `ButtonClick`（默认） |
| `Shell/MainMenuSubmenuWindow.cs:648` | `Hit` | 子菜单窗基类 | `ButtonClick`（默认） |
| `Shell/MatchLogRow.cs:216` | `Hit` | 战斗日志行 | `ButtonClick`（默认） |
| `Shell/MatchLogRow.cs:223` | `Hit` | 战斗日志行 | `ButtonClick`（默认） |
| `Shell/MatchLogRow.cs:285` | `EnemyClickHit` | 战斗日志行 | `ButtonClick`（默认） |
| `Shell/MenuDraw.cs:2119` | `DarkBgHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MenuDraw.cs:2127` | `DarkBgHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MenuDraw.cs:2131` | `CloseHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MenuDraw.cs:2132` | `CloseHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MenuDraw.cs:2151` | `DarkBgHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MenuDraw.cs:2152` | `DarkBgHit` | 公共件（压暗层） | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/MissionRerollPopup.cs:369` | `Hit` | 任务重掷弹窗 | `ButtonClick`（默认） |
| `Shell/MissionsTab.cs:689` | `Hit` | 奖励 · 任务页 | `ButtonClick`（默认） |
| `Shell/OfferContainer.cs:1347` | `Hit` | 商店/报价容器 | `ButtonClick`（默认） |
| `Shell/PlayerProfileWindow.cs:349` | `Hit` | 个人资料窗 | `ButtonClick`（默认） |
| `Shell/PlayerProfileWindow.cs:401` | `Hit` | 个人资料窗 | `ButtonClick`（默认） |
| `Shell/PracticeModePopup.cs:852` | `BackHit` | 练习模式窗 | `ButtonClick`（默认） |
| `Shell/ProfileTab.cs:401` | `Hit` | 个人资料 · 资料页 | `ButtonClick`（默认） |
| `Shell/ProfileTab.cs:452` | `AvatarHit` | 个人资料 · 资料页 | `ButtonClick`（默认） |
| `Shell/ProfileTab.cs:536` | `EditNameHit` | 个人资料 · 资料页 | `ButtonClick`（默认） |
| `Shell/ProfileTab.cs:850` | `DarkBgHit` | 个人资料 · 资料页 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/ProfileTab.cs:893` | `InputHit` | 个人资料 · 资料页 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/ProfileTab.cs:927` | `ChangeNameHit` | 个人资料 · 资料页 | `ButtonClick`（默认） |
| `Shell/ProfileTab.cs:964` | `Hit` | 个人资料 · 资料页 | `ButtonClick`（默认） |
| `Shell/PurchasePremiumWindow.cs:597` | `Hit` | 高级购买窗 | `ButtonClick`（默认） |
| `Shell/PurchasePremiumWindow.cs:665` | `Hit` | 高级购买窗 | `ButtonClick`（默认） |
| `Shell/PurchasePremiumWindow.cs:717` | `Hit` | 高级购买窗 | `ButtonClick`（默认） |
| `Shell/RankedEventWindow.cs:119` | `Hit` | 排位活动窗 | `ButtonClick`（默认） |
| `Shell/RankedEventWindow.cs:146` | `Hit` | 排位活动窗 | `ButtonClick`（默认） |
| `Shell/RankedRewardEventWindow.cs:395` | `Hit` | 排位奖励窗 | `ButtonClick`（默认） |
| `Shell/RankedTab.cs:236` | `AvatarHit` | 个人资料 · 排位页 | `ButtonClick`（默认） |
| `Shell/RankedTab.cs:252` | `EditNameHit` | 个人资料 · 排位页 | `ButtonClick`（默认） |
| `Shell/ReferralPopupWindow.cs:404` | `Hit` | 推荐弹窗 | `ButtonClick`（默认） |
| `Shell/ReferralPopupWindow.cs:513` | `Hit` | 推荐弹窗 | `ButtonClick`（默认） |
| `Shell/ReferralPopupWindow.cs:534` | `Hit` | 推荐弹窗 | `ButtonClick`（默认） |
| `Shell/RewardWindow.cs:1165` | `Hit` | 奖励窗（基础子菜单） | `ButtonClick`（默认） |
| `Shell/SearchingMatchPopup.cs:258` | `CancelHit` | 匹配中弹窗 | `ButtonClick`（默认） |
| `Shell/SearchingOpponentWindow.cs:325` | `CancelHit` | 寻找对手窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:1712` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:1878` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2183` | `LanguageHit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2447` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2457` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2480` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2526` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2599` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2818` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:2918` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3002` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3033` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3047` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3111` | `HitBox` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3112` | `HitLabel` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3140` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3164` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:3455` | `Hit` | 设置窗 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/SettingsWindow.cs:3709` | `Hit` | 设置窗 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/SettingsWindow.cs:4082` | `QualityHit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:4121` | `TextInHandHit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:4329` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:4679` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:5091` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:5173` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:5246` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:5299` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:5389` | `Hit` | 设置窗 | `ButtonClick`（默认） |
| `Shell/SettingsWindow.cs:6036` | `Hit` | 设置窗 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/SettingsWindow.cs:6121` | `Hit` | 设置窗 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |
| `Shell/ShopTabPage.cs:527` | `InfoHit` | 商店页 | `ButtonClick`（默认） |
| `Shell/TitleTab.cs:198` | `SelectHit` | 个人资料 · 称号页 | `ButtonClick`（默认） |
| `Shell/TitleTab.cs:253` | `Hit` | 个人资料 · 称号页 | `ButtonClick`（默认） |
| `Shell/ToastNotifications.cs:442` | `Hit` | 提示条 | `ButtonClick`（默认） |
| `Shell/ToastNotifications.cs:498` | `Hit` | 提示条 | `ButtonClick`（默认） |
| `Shell/ToastNotifications.cs:535` | `Hit` | 提示条 | `ButtonClick`（默认） |
| `Shell/ToastNotifications.cs:570` | `Hit` | 提示条 | `ButtonClick`（默认） |
| `Shell/TrophyInfoPopup.cs:337` | `CloseHit` | 奖杯信息弹窗 | `ButtonClick`（默认） |
| `Shell/TrophyInfoPopup.cs:457` | `CheckHit` | 奖杯信息弹窗 | `ButtonClick`（默认） |
| `Shell/TutorialModePopup.cs:644` | `Hit` | 教程模式窗 | `ButtonClick`（默认） |
| `Shell/TutorialModePopup.cs:695` | `PlayHit` | 教程模式窗 | `ButtonClick`（默认） |
| `Shell/WindowButton.cs:680` | `Hit` | 按钮公共件 | **无**（压暗/吸收/输入框/点卡，原版非 EverguildButton） |

### 我们侧的另一条建钮路径（手工 `AddComponent<WindowButton>()`）

> 这些**不经过** `MenuDraw.Hit`，加 per-button cue 时必须**逐处**补（否则那一颗接不上）。

| 文件:行 |
|---|
| `Shell/CampaignRewardWindow.cs:799` |
| `Shell/CardDetailPopup.cs:1630` |
| `Shell/DailyRewardPopup.cs:368` |
| `Shell/DailyRewardPopup.cs:421` |
| `Shell/DailyRewardPopup.cs:435` |
| `Shell/DailyStreakPopup.cs:525` |
| `Shell/DailyStreakPopup.cs:675` |
| `Shell/DeckInfoPopup.cs:2061` |
| `Shell/DeckSelectionPopup.cs:828` |
| `Shell/ImportDeckPopup.cs:536` |
| `Shell/MainMenuRuntime.cs:899` |
| `Shell/MainMenuRuntime.cs:2435` |
| `Shell/MenuDraw.cs:2053` |
| `Shell/MenuDraw.cs:3158` |
| `Shell/MenuWindowBase.cs:271` |
| `Shell/MissionsTab.cs:1808` |
| `Shell/PopUpGameWindow.cs:480` |
| `Shell/PracticeModePopup.cs:2374` |
| `Shell/PromptPopup.cs:238` |

---

## 四、映射表

### A. 能逐颗对上（我们的节点 → 原版 prefab 节点 → cue）

| # | 我们的节点（文件:行） | 原版 prefab ▸ 节点 | cue | 依据 |
|---|---|---|---|---|
| 1 | `Shell/SettingsWindow.cs:1712` | Main Menu Settings Window ▸ Menu Area/Generic Close Button | `ButtonClick Back` | 同名节点 + 关窗语义 |
| 2 | `Shell/SettingsWindow.cs:1878` | Main Menu Settings Window ▸ Menu Area/Mask Tabs buttons/Tab Buttons/{General,Account,Media,Graphics,Support} | `Select Toggle` | 5 个页签逐颗实读（我们的 `Audio` 页 = 原版 `Media`，见 `SettingsWindow.cs:1831` 的注释） |
| 3 | `Shell/SettingsWindow.cs:3111` | Main Menu Settings Window ▸ General Tab/Checkboxes/{Disable Bots,Touch Input,Disable Notifications} | `Select Toggle` | 3 行复选框 |
| 4 | `Shell/SettingsWindow.cs:4329` | Main Menu Settings Window ▸ Graphics Tab/Content/Scroll View/Viewport/Content/{Android extra compatibility,Hi FPS toggl,Small Screen Size Toggle,Auto Zoom Toggle,Vsync,Use super sampling} | `Select Toggle` | 6 行开关 |
| 5 | `Shell/SettingsWindow.cs:3047` | Main Menu Settings Window ▸ Account Tab/Login Window/Generic Close Button Green | `ButtonClick Back` | 登录窗关钮 |
| 6 | `Shell/CollectionWindow.cs:2935` | Collection Menu Variant ▸ Content Area/Tabs/Shared/Close Button | `ButtonClick Positive` | 关窗钮 |
| 7 | `Shell/CollectionWindow.cs:2657` | Collection Menu Variant ▸ {CardsTab,Alternate Art Tab,Cardback Tab,Select Deck Tab}/Header*/Filters/Clear Filter Button | `ButtonClick Positive` | 三处 Clear Filter 同 cue |
| 8 | `Shell/CollectionWindow.cs:3072` | Collection Menu Variant ▸ Select Deck Tab/Header/Control Buttons/Import | `ButtonClick Positive` | 同名 |
| 9 | `Shell/CollectionWindow.cs:3074` | Collection Menu Variant ▸ Select Deck Tab/Header/Control Buttons/Create | `ButtonClick Positive` | 同名 |
| 10 | `Shell/CollectionWindow.cs:2205/2370/3283` | Collection Menu Variant ▸ …/Filters/*/Content/Toggle（Type/Cost/Rarity/Army/Owned/Upgradable） | `Select Toggle` | 整族筛选开关 |
| 11 | `Shell/CollectionWindow.cs:1773` | Collection Menu Variant ▸ Alternate Art Tab/Header/Select Art Button Left / Right | `ButtonClick Back` | 左右两颗 |
| 12 | `Shell/DeckInfoPopup.cs:981` | Deck info Popup ▸ Buttons/{Practice Deck,Edit Deck,Select Deck} | `ButtonClick Positive` | Btn_Practice/Edit/Select |
| 13 | `Shell/DeckInfoPopup.cs:1060` | Deck info Popup ▸ Deck Options/{Switch Deck Info Button,Duplicate Button,Share Button,Share On Chat,Delete Button} | `ButtonClick Back` | 5 颗选项 |
| 14 | `Shell/DeckInfoPopup.cs:1119` | Deck info Popup ▸ Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 15 | `Shell/ChatPanel.cs:282` | ChatPanel ▸ Holder/Chat/Enter Text/Button | `UI Send Message` | 发送钮 |
| 16 | `Shell/ChatPanel.cs:317` | ChatPanel ▸ Holder/Chat/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 17 | `Shell/ChatPanel.cs:345` | ChatPanel ▸ Holder/Chat/Player Options Panel/Buttons/{Add as a friend,Challenge,Report,Block,Profile} | `ButtonClick Positive` | 5 颗（节点名逐颗同） |
| 18 | `Shell/ChatPanel.cs:422` | ChatPanel ▸ Holder/Chat/Tab Buttons/Orange Tab Toggle | `Select Toggle` | 页签 |
| 19 | `Shell/PlayerProfileWindow.cs:349` | Player Profile Window ▸ Menu Area/Tab Buttons/{Profile,Avatar,Title,Trophies,Battle Log,Ranked} | `Select Toggle` | 6 个页签 |
| 20 | `Shell/PlayerProfileWindow.cs:401` | Player Profile Window ▸ Menu Area/Tab  Area/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 21 | `Shell/AvatarTab.cs:229` | Player Profile Window ▸ Avatar Tab/Selected Item Panel/Select Avatar Button | `SelectAvatar` | 同名 |
| 22 | `Shell/AvatarTab.cs:241` | Player Profile Window ▸ Avatar Tab/Selected Item Panel/Toggle borde | `SelectAvatar` | 同名 |
| 23 | `Shell/TitleTab.cs:198` | Player Profile Window ▸ Title Tab/Selected Item Panel/Select Avatar Button | `SelectAvatar` | 同名 |
| 24 | `Shell/TitleTab.cs:253` | Player Profile Window ▸ Title Tab/Selected Item Panel/Toggle borde | `SelectAvatar` | 同名 |
| 25 | `Shell/AchievementsMenu.cs:247` | Player Profile Window ▸ Trophies Tab/buttons/Achievement Type Toggle (1..4) | `Select Toggle` | 注释已点名原版节点 |
| 26 | `Shell/ProfileTab.cs:964` | Player Profile Window ▸ Profile Tab/ChooseNameWindow/Generic Close Button Green | `ButtonClick Back` | 改名窗关钮 |
| 27 | `Shell/LeaderboardWindow.cs:656` | Ranked{Skirmish,Classic}LeaderboardPopup ▸ Tab Buttons/{Player,Armies,Alliances} | `Select Toggle` | 页签 |
| 28 | `Shell/LeaderboardWindow.cs:712` | Ranked{Skirmish,Classic}LeaderboardPopup ▸ Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 29 | `Shell/LeaderboardWindow.cs:734` | RankedSkirmishLeaderboardPopup ▸ Ranking Display/Generic Simplified UI Button_updated | `ButtonClick Positive` | 出厂隐藏（我们同） |
| 30 | `Shell/LiveOpsEventWindow.cs:943` | RankedEventWindowV2 / SkirmishModeEventWindow ▸ To Battle Button | `ButtonClick Positive` | 同名 |
| 31 | `Shell/LiveOpsEventWindow.cs:514` | RankedEventWindowV2 / SkirmishModeEventWindow ▸ Ranked Deck Selection/Deck Buttons/{Previous,Next,Change,View} Deck Button | `ButtonClick Back` | 4 颗同 cue |
| 32 | `Shell/LiveOpsEventWindow.cs:803` | Ranked Army Selector Container V2 / RankedEventWindowV2 ▸ Ranked Deck Selection/Army 选择 | `SelectAvatar` | 阵营格 |
| 33 | `Shell/RankedEventWindow.cs:119` | RankedEventWindow(V2) ▸ Ranked Division Info/LeaderboardButton | `ButtonClick Positive` | 同名 |
| 34 | `Shell/RankedEventWindow.cs:146` | RankedEventWindow(V2) ▸ ChangeRankedToggle/ChangeRankedButton | `Select Toggle` | 同名 |
| 35 | `Shell/DeckSelectionPopup.cs:550` | Deck Selection Popup with Tabs ▸ Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 36 | `Shell/DeckSelectionPopup.cs:461` | Deck Selection Popup with Tabs ▸ Deck Display/Header/Practice buttons/Generic Simplified UI Button | `ButtonClick Positive` | 随机卡组钮 |
| 37 | `Shell/BoosterInfoPopup.cs:376` | Booster Info Popup ▸ window/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 38 | `Shell/BoosterInfoPopup.cs:607/630` | Booster Info Popup ▸ window/Text/Purchase buttons/Price Display/Generic UI Button | `Purchase Click` | 购买钮（**待核**：两颗里哪颗是价格钮） |
| 39 | `Shell/CampaignRewardWindow.cs:799` | Campaign Reward Window ▸ Content/…/{Premium Rewards,Base Rewards}/Rewards/Unlock Button | `ButtonClick Positive` | 解锁钮 |
| 40 | `Shell/InboxWindow.cs:432` | Inbox Menu ▸ Content/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 41 | `Shell/MissionRerollPopup.cs:369` | ReRollPopup Variant ▸ Window/Buttons/ButtonLeft | `ButtonClick Negative` | 重掷弹窗左钮 |
| 42 | `Shell/RankedRewardEventWindow.cs:395` | Ranked Boost Reward Event Window ▸ window/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 43 | `Shell/ImportDeckPopup.cs:269` | Import Deck Popup ▸ Window/Generic Close Button Green | `ButtonClick Back` | 关窗 |
| 44 | `Shell/TutorialModePopup.cs:695` | Tutorial Mode Menu ▸ PlayTutorialButton | `ButtonClick Positive` | 同名 |
| 45 | `Shell/AllianceEventScorePanel.cs:134` | Alliance Event Score Panel ▸ In Alliance/View Leaderboard Button | `ButtonClick Positive` | 同名 |
| 46 | `Shell/AllianceEventScorePanel.cs:168` | Alliance Event Score Panel ▸ No Alliance/Join Alliances Button | `ButtonClick Positive` | 同名 |
| 47 | `Shell/AllianceEventScorePanel.cs:178` | Alliance Event Score Panel ▸ No Alliance/Leaderboard Button | `ButtonClick Positive` | 同名 |
| 48 | `Shell/AllianceMemberOptionsPopup.cs:404` | Member Options Panel ▸ Buttons/{Promote,Demote,Kick,Profile,Quit,Add as a friend,Challenge,Debug Add Skulls} | `ButtonClick Positive` | 8 颗同 cue |
| 49 | `Shell/GenericOptionsPanel.cs:493` | Generic Options Panel ▸ Template | `ButtonClick Positive` | 模板钮 |
| 50 | `Shell/DuelPopupWindow.cs:214` | MessagePopupWindowDuel ▸ Generic Rounded Button Green | `ButtonClick Back` | 关钮 |
| 51 | `Shell/DuelPopupWindow.cs:241` | MessagePopupWindowDuel ▸ Window/Buttons/{Button Skirmish,Button Classic} | `ButtonClick Positive` | 两颗模式钮 |
| 52 | `Shell/EnergySinglePlayerOnlyEventWindow.cs:456` | Two Sides Event Window ▸ Event On Going Tab/Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button | `ButtonClick Positive` | 领取钮 |
| 53 | `Shell/CardDetailPopup.cs:881/886` | 主菜单场景 ▸ Card Displayer Menu For Menu/Card Options Panel/Panel/Crafting Panel/Content/Craft | `ButtonClick Positive` | 合成钮 |
| 54 | `Shell/CardDetailPopup.cs:975` | 主菜单场景 ▸ …/Upgrade Panel/Content/Upgrade | `ButtonClick Positive` | 升级钮 |
| 55 | `Shell/CardDetailPopup.cs:1279` | 主菜单场景 ▸ …/Alternate Art Panel/Select Art Button Left / Right | `ButtonClick Back` | 异画左右 |
| 56 | `Shell/ReferralPopupWindow.cs:404` | Referral Popup ▸ window/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 57 | `Shell/ReferralPopupWindow.cs:534` | Referral Popup ▸ window/content/Input View/Generic Simplified UI Button | `ButtonClick Positive` | 确认钮 |
| 58 | `Shell/BaseOfferPopup.cs:768` | General Basic Offer Popup* ▸ window/Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 59 | `Shell/BaseOfferPopup.cs:979` | General Basic Offer Popup* ▸ Preview | `ButtonClick Positive` | 预览钮 |
| 60 | `Shell/BaseOfferPopup.cs:1012/1046` | General Basic Offer Popup* ▸ window/Text/Purchase buttons/Price Display/Generic UI Button | `Purchase Click` | 购买钮 |
| 61 | `Shell/OfferContainer.cs:1347` | Webshop Offer Container / Offer Container ▸ raycast target | `Purchase Click` | 把整卡点击做成购买（**待核**：我们 OpenBoosterInfo 语义） |
| 62 | `Shell/RewardWindow.cs:1165` | Reward Window ▸ Content/Collect Button | `ButtonClick Positive` | 领取钮 |
| 63 | `Shell/ForgeTab.cs:887` | Forge Menu Reward Button ▸ Generic UI Button | `ButtonClick Positive` | 领取钮 |
| 64 | `Shell/ForgeTab.cs:719` | Forge Army Item Button ▸ (Toggle 根) | `SelectAvatar` | 阵营格 |
| 65 | `Shell/CampaignTab.cs:742` | Campaign Army Item Button / Army Item Button No Arrow Variant ▸ (Toggle 根) | `SelectAvatar` | 阵营格 |
| 66 | `Shell/CampaignTab.cs:525` | Campaign Tab ▸ Campaign Node Data 节点 | `ButtonClick Positive（该 prefab 3 颗全 Positive）` | 战役节点（**待核**） |
| 67 | `Shell/CampaignTab.cs:1097` | Campaign Tab ▸ Premium Panel/Generic Simplified UI Button | `ButtonClick Positive` | 高级继续钮 |
| 68 | `Shell/FriendsTab.cs:201` | Social Submenu Variant ▸ Friends Tab/Header/Find players panel/Add Friend Button | `ButtonClick Positive` | 同名 |
| 69 | `Shell/FriendsTab.cs:210` | Social Submenu Variant ▸ …/Instant duel Button | `ButtonClick Positive` | 同名 |
| 70 | `Shell/BattleLogPopup.cs:166` | Battle Log Popup ▸ Content/Close Button | `ButtonClick Back` | 关窗 |
| 71 | `Shell/DailyStreakPopup.cs:525` | Daily Streak Popup ▸ Streak Failed/Generic Simplified UI Button | `ButtonClick Positive` | 领取/继续 |
| 72 | `Shell/TrophyInfoPopup.cs:337` | Alliance Trophy Info Popup ▸ Generic Close Button Orange | `ButtonClick Back` | 关窗 |
| 73 | `Shell/TrophyInfoPopup.cs:457` | Alliance Trophy Info Popup ▸ Checkbox | `Select Toggle` | 整行切换 |

> **约 73 条映射行**（含一行代表多颗的族：设置页签/复选框、收藏筛选、卡组信息选项、聊天 5 钮、个人资料 6 页签、成员面板 8 钮、报价弹窗 3 钮等）。其中标 **待核** 的几条见 §六。

### B. 建了窗、但原版那颗钮我们没建 / 语义不同（⛔ 不硬配）

| 原版 prefab ▸ 节点 | cue | 我们这边的情况 |
|---|---|---|
| Inbox Menu ▸ Content/Reset Button | ButtonClick Positive | **没建**（收件箱只建行，正文/Reset 未建；`InboxWindow.cs` 的 `RowHit` 点了只出声） |
| Inbox Menu ▸ Content/Message Display/…/Claim Button/Claim Button | ButtonClick Positive | **没建**（正文区未建，见 `InboxWindow.cs` 注释） |
| Missions Tab ▸ {Weekly Mission,Daily Login Container,Daily Skulls Mission Container}/raycast target | Purchase Click | **语义不同**：我们 `MissionsTab.cs:689` 的命中是**垃圾桶（重掷）**，不是容器主体的购买/打开 |
| Shop Menu Variant / Card Shop Tab / Item Shop Tab ▸ …/Catalog Item Shop Container/raycast target | Purchase Click | **语义不同**：我们 `ShopTabPage.cs:527` 只给卡包接 `InfoHit`（开卡包信息），商品容器本身没有购买点击 |
| Practice Mode Menu ▸ Deck info/General container/Generic Simplified UI Button | ButtonClick Positive | **对不上**：我们 `PracticeModePopup.cs:852` 是 `BackHit`；原版那颗更像「卡组信息里的操作钮」，待核 |
| Profile Tab ▸ Ranking/{Current Rank,Highest Rank}/LeaderboardButton | ButtonClick Positive | **没找到对应命中区**（`ProfileTab.cs` 没有 leaderboard 点击；`RankedTab.cs` 只有头像/改名） |
| Profile Tab ▸ ChooseNameWindow/Change Name Button/Generic UI Button | （无 cue = 默认） | ✅ 我们有 `ProfileTab.cs:927`，默认音即可 |
| RankedEventWindowV2 ▸ Ranked Deck Selection/No Deck Text/Generic Simplified UI Button | ButtonClick Positive | 我们 `LiveOpsEventWindow.cs:582 CreateDeckHit` 可能对应；**待核**（原版是「没有卡组」提示上的钮） |
| RankedEventWindowV2 ▸ DEBUG/DebugBattle/Continue Button | MenuStartGame | 调试钮，**未建** |
| DeckSelectionContinueButton ▸ Continue Button | MenuStartGame | 我们 `LiveOpsEventWindow` 的继续钮未在 175 处里单独出现（可能在 `BaseEventPopup` 之外）——**待核**；若要接，cue = `MenuStartGame` |
| SkirmishModeEventWindow ▸ Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button | ButtonClick Positive | 我们 `EnergySinglePlayerOnlyEventWindow.cs:456` 是同类领取钮（但那属于 Two Sides prefab）——本窗那一颗**没建** |
| Collection Menu Variant ▸ Content Area/Tab Buttons/{Cards,Deck,CardBacks,Alternate Art} | **ButtonClick**（= 默认） | ✅ 我们页签走 `MainMenuSubmenuWindow` / `TabButtons`，**不用改** |
| Social Submenu Variant ▸ Content Area/Tab Buttons/{Friends Tab Button,Alliances Tab Button} | **ButtonClick**（= 默认） | ✅ 同上，不用改 |
| Rewards Base Submenu Variant ▸ Content Area/Tab Buttons/{MissionsRewards,CampaignRewards,Forge,Menu Navigation Panel} | **ButtonClick**（= 默认） | ✅ 不用改 |
| Shop Menu Variant ▸ Content Area/Tab Buttons/Shop Icon | **ButtonClick**（= 默认） | ✅ 不用改 |
| Menu Navigation Panel Button ▸ (Toggle 根) | **ButtonClick**（= 默认） | ✅ 不用改 |

### C. 原版 prefab 我们整体没建（所以没有「找不到对应节点」的问题）

| prefab 根 | 非空 cue 颗数 | 备注 |
|---|---|---|
| Give Feedback Popup menu | 42 | 反馈问卷窗（42 颗里 41 颗 Select Toggle） |
| Draft Mode Timed Mode Window | 20 | Draft 一族，未建 |
| Draft Mode Menu Demo | 17 | Draft 一族，未建 |
| Draft Mode Deck Info Panel | 6 | Draft 一族，未建 |
| Debug Reward Window | 4 | 调试窗 |
| DraftLeaderboardPopup | 4 | Draft 一族，未建 |
| Sunset Popup | 4 | 停服弹窗 |
| Draft Mode Ongoing State | 3 | Draft 一族，未建 |
| Expansion Pass Tab | 2 | 扩展通行证页签 |
| Rate Popup | 2 | 评分弹窗 |
| WhereToGetCardsPopup | 2 | 「卡从哪来」弹窗 |
| Draft Mode Expiring Popup | 1 | Draft 一族，未建 |
| Expansion Pass Missions Tab | 1 | 扩展通行证任务页 |
| Expansion Pass Reward Container | 1 | 扩展通行证奖励容器 |

---

## 五、最小改动方案

### 5.1 参数加在哪（只动一处漏斗）

1. **`Shell/WindowButton.cs`**
   - 在 `public bool playClickSound = true;`（现 `:866`）旁加：
     ```csharp
     /// <summary>这颗钮自己的点击 cue（原版 `m_onClickSound` 的 cue 名）。null = 走默认按钮音 `ButtonClick`。</summary>
     public string clickCue = null;
     ```
   - `PlayClickSound()`（现 `:868-875`）改成：
     ```csharp
     WindowSounds.PlayFx(clickCue ?? WindowSounds.DefaultButtonCue, $"按钮 `{name}`");
     ```
   - **保留** `playClickSound = false`（压暗层 / 开包卡 / 吸收层）；它优先于 `clickCue`。

2. **`Shell/MenuDraw.cs` 的 `Hit(...)`**（现 `:1994`）—— 在**最后一个参数之后**追加 `string clickCue = null`：
   ```csharp
   public static Transform Hit(..., Vector4 maskPad = default(Vector4), string clickCue = null)
   ```
   并在建 `wb` 之后（现 `:2053-2055`）：`wb.clickCue = clickCue;`
   ⚠️ **追加在末尾 = 所有现有调用点零改动**（可选参数）。⛔ 不要插在中间（会把现有位置实参整体错位）。

3. **`Shell/GameWindow.cs` 的 `AddHit(...)`**（现 `:293`）—— 同样在末尾加 `string clickCue = null`，转发给 `MenuDraw.Hit(..., Clip, ClipPad, clickCue)`。

4. **各窗自己的包装 `Hit`/`AddHit`**（要接 cue 的那几扇才改）：
   - `Shell/DeckInfoPopup.cs:2043` 的 `Hit(parent, basis, name, r, onClick, ...)`；
   - `Shell/ForgeTab.cs:1282` / `Shell/CampaignTab.cs:1282` 的 `AddHit(...)`（它们转调 `_win.AddHit`）；
   - `Shell/PracticeModePopup.cs` 的本地 `Hit(...)`；
   - `Shell/CollectionWindow.cs` 的本地 `AddHit` / `Hit` 包装（若有）。
   每个都只是把新形参继续往下传，不写任何 cue 名。

### 5.2 cue 名放哪（保持「表只此一份」）

在 `Shell/WindowSounds.cs` 里加 8 个常量（和现有 `DefaultButtonCue` 同一处）：

```csharp
public const string PositiveCue   = "ButtonClick Positive";   // clip: UIButtonPress（pitch 0.8–1.2）
public const string NegativeCue   = "ButtonClick Negative";   // clip: UIButtonPress Negative
public const string SelectToggleCue = "Select Toggle";        // clip: UI Click 2_no delay  ⚠️ 不是 ButtonClick
public const string SelectAvatarCue = "SelectAvatar";
public const string PurchaseCue   = "Purchase Click";         // clip: UI PurchaseClick
public const string StartGameCue  = "MenuStartGame";          // clip: StartMatch
public const string SendMessageCue = "UI Send Message";
public const string Purchase2Cue = "UI Purchase 2";
```

并把它们加进 `工具/import_original_sfx.py` 的 `EXTRA_CUES`，跑一次导入器。

### 5.3 调用点怎么逐颗接（按窗）

| 窗/文件 | 接法 |
|---|---|
| `SettingsWindow.cs` | `:1712` 传 `Back`；`:1878` 传 `SelectToggle`；`:3111` 传 `SelectToggle`；`:4329` 传 `SelectToggle`；`:3047` 传 `Back` |
| `CollectionWindow.cs` | `:2935`/`:2657`/`:3072`/`:3074` 传 `Positive`；筛选开关（`:2205/:2370/:3283`）传 `SelectToggle`；`:1773` 传 `Back` |
| `DeckInfoPopup.cs` | `:981` 传 `Positive`；`:1060`/`:1119` 传 `Back` |
| `ChatPanel.cs` | `:282` 传 `SendMessage`；`:317` 传 `Back`；`:345` 传 `Positive`；`:422` 传 `SelectToggle` |
| `PlayerProfileWindow.cs` + 三页 | `:349` 与 `AvatarTab:241`/`TitleTab:253`/`AchievementsMenu:247` 传 `SelectToggle`（头像/称号的 Select Avatar/Toggle borde 传 `SelectAvatar`）；`:401`/`ProfileTab:964` 传 `Back` |
| `LeaderboardWindow.cs` | `:656` 传 `SelectToggle`；`:712` 传 `Back`；`:734` 传 `Positive` |
| `LiveOpsEventWindow.cs` | `:943` 传 `Positive`；`:514` 传 `Back`；`:803` 传 `SelectAvatar` |
| `RankedEventWindow.cs` | `:119` 传 `Positive`；`:146` 传 `SelectToggle` |
| 各「关窗钮」 | 一律 `Back`：`DeckSelectionPopup:550` · `BoosterInfoPopup:376` · `InboxWindow:432` · `RankedRewardEventWindow:395` · `ImportDeckPopup:269` · `BaseOfferPopup:768` · `ReferralPopupWindow:404` · `BattleLogPopup:166` · `TrophyInfoPopup:337` · `DuelPopupWindow:214` |
| 各「确认/领取/购买」 | `Positive` 或 `Purchase Click`：`CampaignRewardWindow:799` · `RewardWindow:1165` · `ForgeTab:887` · `TutorialModePopup:695` · `AllianceMemberOptionsPopup:404` · `GenericOptionsPanel:493` · `DuelPopupWindow:241` · `EnergySinglePlayerOnlyEventWindow:456`；`BaseOfferPopup:1012/1046` 与 `OfferContainer:1347` 用 `Purchase Click` |
| `CardDetailPopup.cs` | `:881/:886`、`:975` 传 `Positive`；`:1279` 传 `Back` |
| `Deck/DeckRuntime.cs`（卡组编辑） | **不走 `MenuDraw.Hit`**：它的点击在 `HandlePoolClick`（`:4897`）/ `UiPoolHit` 这一路。要接 12 颗 cue，得先在**那一层的派发点**加同样的「这颗是谁 → cue」表（建议：给卡组编辑的按钮 id 加一张 `id → cue` 字典，在 `HandlePoolClick` 命中时播）。原版 12 颗的节点/cue 见 §二·C-1 `Deck Editing Menu` |

### 5.4 缺 cue / 特殊情形怎么办

- **表里没有**：`WindowSounds.PlayFx` 已经「每个名字只警告一次」并返回 false（不静默）。补 `EXTRA_CUES` + 跑导入器即可。
- **原版 cue = `ButtonClick`（默认）**：调用点**什么都不传**（保持 `null`）—— 这就是原版行为。
- **原版不是 `EverguildButton` 的命中面**（压暗层 `BackgroundCloseButton`、开包卡 `UIGenericEventCatcher`、吸收层、输入框）：继续 `playClickSound=false` / 不传 cue，⛔ 不要因为「有 Hit」就一律给 cue。
- **`EverguildToggle` 的两个特例**：`allowClickWhenAlreadySelected=0` 且已选中 ⇒ 原版**连 `OnPointerClick` 都提前 return**（声音也不响）。我们现在没有这个字段；如果后续要接，应在 `WindowButton.Click()` 最前面加同样的守卫（本轮不在 S01 范围，先记着）。
- **`ButtonClick Positive` 的 pitch 随机**由 `WFSoundCue.minPitch/maxPitch` 负责，调用点不用管。

---

## 六、没查到的 / 需要复核的（逐条）

1. **W_G 的「438 颗 Button」不复现**。本报告在 `menus_assets_all` 里实读 `EverguildButton` 非空 = **291**（总 855）、`EverguildToggle` 非空 = **133**（总 151）。搜过的范围：`menus` 包全量 MonoBehaviour（35014）+ `m_Script` 反查 `bundle_Waprforge_monoscripts` 的 `m_ClassName`。**没查到 438 的出处**；可能是把某个场景包的实例算进去了（`scenes_battlearena*` 每场 30 + 主菜单 21），但没有证据，⛔ 不猜。
2. **`Practice Mode Menu ▸ Deck info/General container/Generic Simplified UI Button`（Positive）到底是什么钮没查**：我们的 `BackHit` 在语义上最接近，但原版 `Practice Mode Menu` 只有 2 颗非空 cue（Continue = `MenuStartGame`、这颗 = `Positive`），没有一颗显式叫 Back。需要看反编译/实拍确认后再接。
3. **`BoosterInfoPopup.cs:607/630` 哪一颗是价格/购买钮没逐颗核**：原版只有 `window/Text/Purchase buttons/Price Display/Generic UI Button` 一颗带 `Purchase Click`；我们有两处 Hit（可能是购买 + 关闭/其它）。需要按代码里的节点名确认。
4. **`CollectionWindow.cs:1773` 的 `ArrowHit` 是异画左右箭头**：原版那两颗在 `Collection Menu Variant ▸ Alternate Art Tab/Header/Select Art Button Left / Right`，cue = `ButtonClick Back`；但同一颗在**主菜单场景**的 `Card Displayer Menu For Menu/…/Alternate Art Panel` 也是 Back（已如实计入）。两处都是 Back，结论一致。
5. **`DeckEditingWindow` 的 12 颗 cue 没有 `MenuDraw.Hit` 落点**：卡组编辑的点击走 `Deck/DeckRuntime.cs` 的 `HandlePoolClick`/`UiPoolHit`（另一条输入路径），不在本次 Shell 普查的 175 处里。12 颗原版节点/cue 已在 §二·C-1 列出，接线方案见 §5.3 最后一行。
6. **`MenuDraw.cs` 里那 6 处 `ShadeHit` 生成的 `DarkBgHit`/`CloseHit` 不是按钮**：它们是压暗层的命中区（原版 `BackgroundCloseButton`，88/88 颗无音）—— `WindowButton.PlayClickSound` 已按 `WasShadeHit` 跳过。⛔ 不要给它们 cue。
7. **`ToastNotifications.cs` 的 4 处 Hit**：原版没有对应的 `EverguildButton`（提示条不是按钮），但我们给了 `WindowButton`。默认音会让「点提示条」响一声 —— 这是**我们自己的行为**，原版没有。本轮只记录，不改（不在 S01 判据里）。
8. **主菜单场景的 4 颗 cue** 都是 `Card Displayer Menu For Menu`（卡片详情面板）里的：`Select Art Button Left/Right`（Back）、`Upgrade`/`Craft`（Positive）。我们的对应件 = `CardDetailPopup.cs`，已映射；但 **`CardDetailPopup` 同时服务收藏窗**，收藏窗打开卡片时是否应播同样的 cue 需要确认（原版收藏用的是同一个 `Card Displayer Menu For Menu` prefab，所以按同一套接是合理的 —— 但这是**推断**，标在这里）。
9. **`Give Feedback Popup menu` 42 颗** 没建窗，因此没有「我们的节点」可配；prefab/cue 已全量列出（§二·C-2）。
10. **`m_onClickSound` 的 `m_FileID` 版本相关**：本 build 里 `EverguildButton` 的字段偏移 `0x170/0x178`、`EverguildToggle` 的 `0x130/0x138`（反编译实读）。如果换 build，字段名不变但偏移会变 —— 读法应始终走**序列化字段名**，⛔ 不要按偏移硬编码。

