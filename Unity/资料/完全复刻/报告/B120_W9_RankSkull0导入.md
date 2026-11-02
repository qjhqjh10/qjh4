# B120-W9 · 导入原版 `Rank Skull_0`（EndBattleDoors.sealIconSprite）

日期：2026-11-02 · 写手：B120-W9（资源）· 只读 `d:/2`、未动 `.cs`、未跑 Unity

---

## 一、结论一句话

`sprite` 已按原版 `m_Rect` 切出并落到 `Resources/Art/ui_menu/Rank_Skull_0.png`（128×128，非透明 9,665 px）。
**⚠️ 但与工程里已有的 `Rank_Skull.png` 逐像素完全相同（0/16384 差异、PNG 字节 md5 也相同）** ——
两张是「不同 guid、同一张图」。详见 §四，需要调度台裁定这条记录怎么写。

---

## 二、图集与裁剪矩形

| 项 | 值 | 出处 |
|---|---|---|
| sprite 资产 | `Assets/Sprite/Rank Skull_0.asset`（guid `9bcbe305c836c17428c28e38cb8f1c1d`，`m_Name: Rank Skull`） | `D:/2/tools/assetripper_out/ExportedProject/Assets/Sprite/Rank Skull_0.asset:4,9` |
| `m_Rect` | `x:1720, y:1916, width:128, height:128`（Unity 原点**左下**） | 同上 `:10-15`；`m_RD.textureRect` `:107-112` 逐字一致 |
| 引用图集 guid | `ef391b8d96fe26342952aab2f78d6d22` → `sactx-0-4096x2048-BC7-0_GeneralUI Atlas-7a9721b1_0.png` | `:29`（现核 meta：该 guid 属于 `..._0.png`；不带 `_0` 的那份是 `52b87a90…`） |
| **图集实测尺寸** | **4096 × 2048**，mode `RGBA`（`Image.open().size`） | 脚本读数，与文件名一致 ⇒ **未触发「停手写反证」** |
| 裁剪（PIL 左上原点） | `left=1720, top = 2048 - (1916+128) = 4, right=1848, bottom=132` | `_tmp_view/B120_W9/slice_rank_skull_0.py` |

> 补充：两份图集 PNG **字节完全相同**（md5 `a87e8980564607af5ea96d45530ac12a`，均 4,700,337 B）。
> 这正是 §四「两张 sprite 图片一样」的根因 —— 同一个图集被 AssetRipper 用两个 guid 各导了一份，
> 而 `Rank Skull.asset`(guid `d90d07c0…`) 指不带 `_0` 那份、`Rank Skull_0.asset`(`9bcbe305…`) 指 `_0` 那份，**两者的 `m_Rect` 完全相同**。

---

## 三、切出结果

| 读数 | 值 |
|---|---|
| 尺寸 | 128 × 128 |
| 非透明像素（alpha > 0） | **9,665 / 16,384（59.0%）** |
| alpha == 255 | 5,052 |
| alpha == 0 | 6,719 |
| 不透明包围盒（PIL 坐标） | `(0,0)-(126,119)`，127 × 120 ⇒ 图形**基本铺满**该 rect，没有取偏 |
| 主要颜色（alpha>200，量化到 32 级） | 近黑 741 · 墨绿 735 · 灰白 422 · 深绿 368 |
| PNG 字节 | 27,700 B |
| md5 | `90f304f1bd7da19b7c62281e779164ca` |

**内容确认（看图）**：骷髅头 + 双侧月桂叶环 —— 与「rank / 印记」语义相符，不是切错区域。
4× 预览（棋盘格衬底）：`D:/4/_tmp_view/B120_W9/preview_Rank_Skull_0_x4.png`

---

## 四、与 `Rank_Skull.png` 的逐像素差 —— **判据结果是「同一张」**

| 对比 | 结果 |
|---|---|
| 尺寸 | 都是 128×128 |
| **逐像素差异** | **0 / 16,384（0.0%）**，最大通道差 **0**，平均通道差 **0.000** |
| 文件字节 | 两文件均 **27,700 B**，md5 **都是 `90f304f1bd7da19b7c62281e779164ca`**（**逐字节相同**） |
| 反证（`Rank_Skull_Empty`） | 与 `Rank_Skull` 差 **16,169 / 16,384（98.7%）** ⇒ 那确实是另一张图，说明本方法**能**区分不同图，不是比较器失灵 |

### ⚠️ 这一条与任务简报的前提冲突，请调度台裁定

简报写「不是 `Rank_Skull` / `Rank_Skull_Empty`（那两个是**不同资产**）」。
现核的**准确说法**是：

- **资产（guid）确实不同**：`Rank Skull_0` = `9bcbe305…`，`Rank Skull` = `d90d07c0…`；
- **但像素内容完全相同**（0 差异、字节 md5 相同）。

⇒ 也就是说：`EndPanel.SealIcon()` 现在**能取到图了**，但取到的画面与原来的 `Rank_Skull` **一模一样**。
这不影响「接上」这件事（我们导的是原版 `sealIconSprite` 真正指向的那一个 guid，见 §五），
但会直接影响账里「本条补的是一张缺失的图」这个描述 —— **建议写成「补的是缺失的资产槽位，画面与 `Rank_Skull` 相同」**。

> 我没有改 `EndPanel.cs:1413-1415` 那段注释（其中「本地没有这张图」「不同资产 ⛔ 不许拿它顶」两句现已半过期）。
> 按红线未动任何 `.cs`，留一条待办给有写权限的代理：
> `CardPresentation/Battle/EndPanel.cs:1413-1415` —— 注释应改为「已于 2026-11-02 导入 `Rank_Skull_0.png`；
> 其像素与 `Rank_Skull` 相同（原版图集被导出两份、两个 guid）」。
> 注意**行号零位移**要求（`资料/完全复刻` 的账按行号引用），改写时别新增行。

---

## 五、原版真的用的是这一张（交叉核）

全 13 个战场场景的 `sealIconSprite` **一律**指向 guid `9bcbe305c836c17428c28e38cb8f1c1d`：

```
Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity:262538
  sealIconSprite: {fileID: 21300000, guid: 9bcbe305c836c17428c28e38cb8f1c1d, type: 2}
…（Arena 2 / 3 / Aeldari / Astra Militarum / Black Legion / Dark Angels /
   Emperors Children / Genestealers / Leviathan / Sororitas / Space Wolves / Tau Viorla 同值，共 13 处）
```

⇒ **调度的判据（用 `Rank Skull_0`）成立**，不是拿 `Rank Skull`（`d90d07c0…`）顶。

---

## 六、落地文件

| 文件 | 说明 |
|---|---|
| `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/Rank_Skull_0.png` | 128×128 RGBA，27,700 B |
| `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/Rank_Skull_0.png.meta` | 见 §七 |

消费链现读确认（**未改动**）：
`Battle/EndPanel.cs:1417-1419` `SealIcon()` → `CardArt.MenuUi("Rank_Skull_0")`
→ `Core/CardArt.cs:890` `Get("Art/" + "ui_menu/" + name)` → `Core/CardArt.cs:1064` `Resources.Load<Texture2D>("Art/ui_menu/Rank_Skull_0")`
⇒ 落盘路径与加载路径逐段对齐。

**只新增了上面两个文件**，`ui_menu/` 里其它 471 张 PNG 与 `Rank_Skull.png` / `Rank_Skull_Empty.png` 均未触碰
（`Rank_Skull.png` mtime 仍为 10-31 18:56）。

---

## 七、`.meta` 的判断与做法

**现读事实**：`/Unity/MyGame/Assets/CardPresentation/Resources/Art/` 整棵目录**被 git 忽略**
（`.gitignore:99`），`git check-ignore` 对 `Rank_Skull.png.meta` 返回命中 ⇒ 目录里的 `.meta` **不是入库的**，
是本地 Unity 生成物（473 个 `.meta`，其中 **454 个除 `guid` 外逐字相同**）。

**做法**：按简报「不要手写」的口径，**没有从零手写**；但为了让 `TextureImporter` 的设置与同目录 PNG
**逐字一致**（Unity 首次自动导入的默认值不一定等于现状，而这台机器上无法用 Unity 验证），
采用简报给的兜底路：**整份复制 `Rank_Skull.png.meta`、只换 `guid`**。

| 校验 | 结果 |
|---|---|
| 新 guid | `2dcc96383b57495b935436d27bf0a820`（`uuid4().hex`） |
| 全工程 guid 冲突 | **0**（扫 `Unity/MyGame/Assets/**/*.meta`） |
| 除 guid 外与原 meta 是否逐字节相同 | **是**（`BYTES_EQUAL_EXCEPT_GUID=True`） |
| 字节数 / 行尾 / BOM | 2,652 B · LF 117 · 无 BOM（与源一致） |
| 关键字段 | `spriteMode: 0`（Texture，非 Sprite）· `textureType: 0` · `alphaIsTransparency: 0` · `enableMipMap: 0` |

> 若调度台坚持「交给 Unity 生成」：**删掉 `Rank_Skull_0.png.meta` 即可**，Unity 下次导入会重建；
> 代价是设置可能与本目录现有那 454 份不完全一致（无法离线验证）。
> 因为目录在 `.gitignore` 内，这一步**不会产生任何 git 变更**（`git status` 对该目录为空）。

---

## 八、要不要真机/Unity 再看

- **不需要为「切图对不对」开 Unity**：图集尺寸、矩形、非透明像素、包围盒、看图确认，都在本地核过了。
- **需要一次（可攒批）的**：`Resources.Load` 能否取到 —— 依赖 Unity 首次导入生成本地资产库。
  判据很轻：起任一宿主后 `CardArt` 不再对 `Art/ui_menu/Rank_Skull_0` 报 `NoteMissing`（`CardArt.cs:1065` 出声），
  且战斗结算面板 `EarnedSeals != 0` 时印记图标不再是奖杯图（`EndPanel.cs:1426` `ApplyRatingIcon`）。
  ⚠️ 视觉上会与本来的 `Rank_Skull` 相同（§四），**所以「看画面」判不出新图有没有生效**，
  要靠上面那条 `NoteMissing` 不出现来判 —— 这点别弄反。

---

## 九、没做的 / 留给别人的

1. **没改任何 `.cs`**（`EndPanel.cs:1413-1415` 的注释已半过期，待办见 §四末）。
2. **没跑 Unity / typecheck / 没动 git**（按红线）。
3. **没在活状态表里核销**（`资料/完全复刻/活状态表.md` 只有调度台能改）。
4. **没动 `d:/2`**（全程只读；脚本与预览都写在 `D:/4/_tmp_view/B120_W9/`）。

---

## 十、可复跑

```bash
python -I d:/4/_tmp_view/B120_W9/slice_rank_skull_0.py    # 切图 + 与 Rank_Skull 逐像素比
python -I d:/4/_tmp_view/B120_W9/verify_preview.py        # 非透明像素 / 包围盒 / 主色 / 4x 预览
python -I d:/4/_tmp_view/B120_W9/make_meta.py             # 复制 meta + 换 guid（重跑会覆盖成同一 guid）
```
