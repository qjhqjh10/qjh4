# -*- coding: utf-8 -*-
import io

MAIN = 'd:/4/Unity/资料/完全复刻/账/未分类_其他.tsv'
PATCH = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1.tsv'
OUT = 'd:/4/Unity/资料/完全复刻/报告/B93_U1.md'

main = [l.split('\t') for l in io.open(MAIN, 'rb').read().decode('utf-8').split('\n') if l.strip()]
hdr, mrows = main[0], { (r[1], r[2]): r for r in main[1:] }
patch = [l.split('\t') for l in io.open(PATCH, 'rb').read().decode('utf-8').split('\n') if l.strip()]
prows = { (r[1], r[2]): r for r in patch[1:] }

# 我的 61 条 = B93_U1.tsv 里的键。
# ⚠️ 别用「主表当前还是 B/C」筛 —— 主表已被合并（我那批里已有 14 条是 A），按状态筛会漏掉它们。
MYKEYS = [(r[1], r[2]) for r in patch[1:]]
mine = [mrows[k] for k in MYKEYS if k in mrows]
assert len(mine) == 61, len(mine)

BP = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1b.tsv'
brows = {}
try:
    for l in io.open(BP, 'rb').read().decode('utf-8').split('\n')[1:]:
        if l.strip():
            f = l.split('\t')
            brows[(f[1], f[2])] = f
except FileNotFoundError:
    pass

rows = []
rows.append('| # | 原版类.方法 | 状态（B93_U1 之后 → 裁定 ①② 之后） | 本轮落点（`文件:行（声明名）`） |')
rows.append('|---|---|---|---|')
for i, k in enumerate(MYKEYS, 1):
    r = mrows[k]
    u1 = prows.get(k)
    af = brows.get(k)
    st = (u1[5] if u1 else r[5]) + (' → **' + af[5] + '**' if af else '')
    land = (af[6] if af else r[6]).replace('|', '\\|')
    if len(land) > 150: land = land[:150] + '…'
    meth = (r[1] + '.' + r[2]).replace('|', '\\|')
    if len(meth) > 78: meth = meth[:78] + '…'
    rows.append('| %d | `%s` | %s | %s |' % (i, meth, st, land))

table = '\n'.join(rows)

from collections import Counter
_fin = Counter((brows[k][5] if k in brows else (prows[k][5] if k in prows else mrows[k][5]))
               for k in MYKEYS)
print('final status of my 61:', dict(_fin))

doc = '''# B93_U1 报告 —— `账/未分类_其他.tsv` 的 61 条 B/C（文件所有者代理）

会话：2026-10-28（第九十三会话 · 批次 B93）· 我独占的源码：`Core/UguiRect.cs`（= 简报里的 `Shell/UguiRect.cs`，实际在 `Core/`）·
`Core/Tooltip.cs` · `Shell/WindowsManager.cs` · `Shell/ItemDrawer.cs` · 以及这 61 条里出现的其它 `Shell/*` / `CardPresentation/*`（黑名单外）。

## 结论（61 条）

| 口径 | 条数 | 说明 |
|---|---|---|
| **落地（A）** | **14** | 其中 **6 条是本轮新写的方法本体**（`UIExtension` ×5 + `RectExtensions.GetWorldRect`），8 条是逐条核过既有等价实现后升 A |
| 判 `D` | **0** | 本轮没有一条够 D 的条件（uGUI 类按调度台 2026-10-28 裁定③(a) 不进 D） |
| **判据未闭合（B）** | **23** | 有等价物/方法体但缺「同形入口」或两侧只到类级 ⇒ 维持 B（每条都写清了「具体缺口」） |
| **该做没做（C）** | **24** | 其中 **10 条是 B→C 的订正**（原写「机制不同」站不住：通用容器体系 7 条 · 四角渐变 1 条 · RectSizeLimiter 2 条） |
| **假无落点（原账写「无落点」、本轮给了 `文件:行`）** | **29** | 全在这 61 条里（清单见附表） |

> 状态迁移现读：`B→A 11 · C→A 3 · B→C 10 · B→B 23 · C→C 14` = 61。
>
> 🆕 **调度台裁定 ①② 落地之后（`账/_parts/B93_U1b.tsv`，6 行）**：**A 20 · B 18 · C 23** = 61（详见 §八）。

## 一、本轮真正写下去的代码（唯一一处源码改动）

**`d:/4/Unity/MyGame/Assets/CardPresentation/Core/UguiRect.cs`：299 行 → 353 行（+54）**

| 改动 | 落点 | 判据 |
|---|---|---|
| 新增 `UIExtension` 类（`GetCorners` / `MaxX` / `MaxY` / `MinX` / `MinY`） | `:315`（类）· `:320` `:328` `:330` `:332` `:334` | `decomp_full/UIExtension__*.c` 全文 |
| 新增 `RectExtensions.GetWorldRect(RectTransform)` | `:345`（`RectExtensions` 改为 `partial`，新块在 `:337`） | `decomp_full/RectExtensions__GetWorldRect.c:20-29` |

🔴 **两条「照原版、别优化」的关键判据（本轮现读 `.c` 得出，写进了注释）**：

1. **`MaxX/MaxY/MinX/MinY` 不是「扫四角取最大/最小」**，是**按固定下标取某一颗角**（`.c` 里是数组字节偏移）：
   `MaxX = corners[2].x`（`0x38`）· `MaxY = corners[1].y`（`0x30`）· `MinX = corners[0].x`（`0x20`）· `MinY = corners[0].y`（`0x24`）；
   长度校验（`2<count` / `1<count` / `count!=0`）逐条对上。⇒ **轴对齐矩形上两者同值、旋转过的矩形上不同**（铁律 5·c）。
   ⛔ 谁要「优化」成 `Mathf.Max` 扫描，就是换了一个函数。
2. **四角数组是静态复用的**（原版 `DAT_18429f330`）⇒ 后一次调用会覆盖上一次返回的数组，语义照抄（⛔ 别改成每次 `new`）。
   `null` 入参原版走 throw（`FUN_1803f47a0`）⇒ 我们抛 `ArgumentNullException`，不静默返回空数组。

**行数位移**：`RectExtensions` 那一行是**就地改写**（`public static class` → `public static partial class`，`+0` 行）；新代码全部**追加在文件 EOF**（`:315` 起）⇒ **既有 1~314 行的行号一个没动**（账里 `Core/UguiRect.cs:51` / `:277` / `:292` 三处 A 落点都不受影晌）。

## 二、验证读数

| 闸 | 读数 | 结论 |
|---|---|---|
| `TMPDIR=/tmp/wf_b93u1 bash 工具/typecheck.sh`（本轮共跑 2 次） | 第 1 次运行时 **2 个错**：`Shell/MainMenuRuntime.cs(4520)`（D3）· `Shell/Inventory.cs(2092)`（D4）；第 2 次（收工时）**只剩 1 个**：`Shell/Inventory.cs(2092)` `CS1729`（D4 正在写的文件）。编辑器程序集因运行时未过而 `CS0006`（级联，不是独立错） | **我的文件 0 错**（`Core/UguiRect.cs` 两次都不在错误列表里）；按 13·3-2，剩下的错全在别人的文件 ⇒ 不是本代理的 |
| `bash 工具/ruleprobe.sh play check` | **rc=0** · 哨兵 `✓ 基线文件的内容与钉住的一致（ef8a0249…）` | 期望值一致，**未重钉** |
| `工具/check_patch_conflicts.py` | `✅ 没有跨补丁重复键` | 补丁无跨补丁冲突 |

> ⚠️ **不跑 Unity**（简报 ⑦）：本轮新增的 `UIExtension` / `GetWorldRect` **没有任何断言被执行过**（见 §三 的「需调度台粘到 `Editor/ShellScene.cs`」）。

## 三、断言清单

### 3.1 需调度台粘到 `Editor/ShellScene.cs`（E27 独占，我没有写权限）

**应插位置**：`Editor/ShellScene.cs:10226` 那一节起头处（`Section("🆕 复刻第 3 轮：三个原版通用件 + 页签生命周期 —— 对着方法体断行为（未接实例）");`）**之后**，作为同族的一节。
用到的助手（该文件已有）：`Section(string)` · `Check<T>(got, want, msg)` · `CheckTrue(bool, msg)` · `CheckNear(got, want, tol, msg)`。

```csharp
        // ============================================================ B93_U1：原版 uGUI 矩形工具族
        //   判据：decomp_full/UIExtension__{GetCorners,MaxX,MaxY,MinX,MinY}.c ·
        //         decomp_full/RectExtensions__GetWorldRect.c:20-29
        //   🔴 两条【灭自证】：
        //      ① 把件**转 30°** 之后断 MaxX == corners[2].x —— 轴对齐时「取下标」与「取最大值」同值，
        //         只有旋转过的矩形能把「写成 Mathf.Max 扫描」抓出来；
        //      ② 断「两次调用的返回值是同一个数组实例」—— 抓「改成每次 new」。
        Section("B93_U1：UIExtension / RectExtensions.GetWorldRect（原版 uGUI 矩形工具族）");
        {
            var probe = new GameObject("b93u1_rectProbe", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)probe.transform;
                rt.SetParent(null, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(200f, 100f);
                rt.anchoredPosition = new Vector2(123f, 45f);
                rt.localRotation = Quaternion.identity;
                Canvas.ForceUpdateCanvases();

                // ① 前提（硬检查）：四角真的拿到了、且角序是 Unity 的「左下→左上→右上→右下」
                var c1 = UIExtension.GetCorners(rt);
                Check(c1 != null, true, "① 前提：GetCorners 返回非 null");
                Check(c1 != null ? c1.Length : -1, 4, "① 前提：四角数组长度 = 4");
                CheckTrue(c1[2].x > c1[0].x && c1[1].y > c1[0].y,
                          "① 前提：角序 = 左下→左上→右上→右下（c2.x > c0.x 且 c1.y > c0.y）");
                // ② 灭自证：静态复用数组（两次调用是同一个实例）
                CheckTrue(ReferenceEquals(c1, UIExtension.GetCorners(rt)),
                          "② 灭自证：GetCorners 复用同一个静态数组（原版 DAT_18429f330）—— 改成每次 new 会红");
                // ③ 轴对齐档：Max/Min 与 PxRect 四边同值
                CheckNear(UIExtension.MaxX(rt), rt.rect.xMax, 0.01f, "③ MaxX = rect.xMax（轴对齐）");
                CheckNear(UIExtension.MinX(rt), rt.rect.xMin, 0.01f, "③ MinX = rect.xMin（轴对齐）");
                CheckNear(UIExtension.MaxY(rt), rt.rect.yMax, 0.01f, "③ MaxY = rect.yMax（轴对齐）");
                CheckNear(UIExtension.MinY(rt), rt.rect.yMin, 0.01f, "③ MinY = rect.yMin（轴对齐）");
                // ④ 灭自证（旋转档）：MaxX 必须等于 corners[2].x，而不是四角里最大的 x
                rt.localRotation = Quaternion.Euler(0f, 0f, 30f);
                Canvas.ForceUpdateCanvases();
                var cr = UIExtension.GetCorners(rt);
                float maxOfAllX = Mathf.Max(Mathf.Max(cr[0].x, cr[1].x), Mathf.Max(cr[2].x, cr[3].x));
                CheckNear(UIExtension.MaxX(rt), cr[2].x, 0.01f,
                          "④ MaxX = corners[2].x（**转 30° 后**；写成 Max 扫描 ⇒ 这里会差出来）");
                CheckTrue(Mathf.Abs(cr[2].x - maxOfAllX) > 0.5f,
                          "④ 灭自证前提：转 30° 后「右上角的 x」确实不是四角最大 x（差 > 0.5）");
                // ⑤ GetWorldRect = 以 c0 基点、c2 对角 → 中心 + 对角线尺寸
                Rect wr = RectExtensions.GetWorldRect(rt);
                CheckNear(wr.center.x, (cr[0].x + cr[2].x) * 0.5f, 0.01f, "⑤ GetWorldRect 中心 x = (c0+c2)/2");
                CheckNear(wr.center.y, (cr[0].y + cr[2].y) * 0.5f, 0.01f, "⑤ GetWorldRect 中心 y = (c0+c2)/2");
                CheckNear(wr.width, cr[2].x - cr[0].x, 0.01f, "⑤ GetWorldRect 宽 = c2.x − c0.x（轴对齐包围盒）");
                CheckNear(wr.height, cr[2].y - cr[0].y, 0.01f, "⑤ GetWorldRect 高 = c2.y − c0.y");
                // ⑥ 灭自证：null 入参必须抛（原版 null-check throw），⛔ 不许静默返回空数组
                bool threw = false;
                try { UIExtension.GetCorners(null); } catch (System.ArgumentNullException) { threw = true; }
                CheckTrue(threw, "⑥ 灭自证：GetCorners(null) 抛 ArgumentNullException —— 改成静默返回会红");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
```

**另一条（同族、可选）—— 命中区「不画但能点」= 原版 `EmptyGraphic.OnPopulateMesh`（`VertexHelper.Clear`）**：

```csharp
        // 判据：decomp_full/EmptyGraphic__OnPopulateMesh.c（全文 = VertexHelper.Clear）⇒ 图形在、画面无。
        // 我们的等价 = MenuDraw.Hit 建的命中节点把 quad 设成全透明（Shell/MenuDraw.cs:2292）。
        // 灭自证：断的是 **tint 的 alpha == 0**（画面不可见），⛔ 不是断「建了个 ImageQuad」——后者改成有色也会绿。
        Section("B93_U1：EmptyGraphic 等价物 —— 命中区全透明");
        {
            var host = new GameObject("b93u1_hitProbe", typeof(RectTransform));
            try
            {
                var hit = MenuDraw.Hit(host.transform, "HitProbe",
                                       new PxRect(0f, 0f, 100f, 50f), 4000, null);
                CheckTrue(hit != null, "① 前提：MenuDraw.Hit 建出了节点");
                var iq = hit != null ? hit.GetComponentInChildren<ImageQuad>(true) : null;
                CheckTrue(iq != null, "① 前提：命中节点下**必须有 ImageQuad**（PointerLayer 只认它）");
                CheckNear(iq != null ? iq.Tint.a : 1f, 0f, 0.001f,
                          "② 命中 quad 全透明（= 原版 EmptyGraphic 的『不画但有 Graphic』）");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
```

> ⚠️ 第二段用到的 `PxRect` 在 `CardPresentation` 命名空间、`ImageQuad` 在 `CardPresentation`（`Battle/` 下但同命名空间—— 若不是，`ShellScene` 里已有多处直接用 `ImageQuad`，照它那里的写法调）。

### 3.2 不需要新断言的行（理由）

* 8 条升 A 的行（`OverlapsAny` / `GenericArmyItem.GetID` / `ReferralContainer.OnClick|OpenPopup|TryPurchase` / `EmptyGraphic` / `MessageList.Refresh` / `UnitOnBoardAttackTypeSelector.Toggle`）：**断言已存在于别处**（`OverlapsAny` 见 `CollectionWindow`/`DeckRuntime` 的命中断言；`OpenReferralPopup` 见 `Editor/ShopScene.cs:5381-5601` 四条；`AttackSelector.Show` 见 `Editor/BattleScene.cs`）。本轮**没有新增行为**，不重复造断言。

## 四、逐条结果（61 条）

{表}

## 五、没查清 / 留给调度台裁的（不许我自己发明口径）

1. 🔴 **「`Signal.Register` 订阅族」的口径还没定**（`AddSabotageCard.OnInitialize` · `CollectSpiritStone.OnReset` · `CreateSecret.OnInitialize/OnReset` 等 4 条）。
   本轮**现读推翻了一条旧结论**：**`OnReset` 也是 `Signal.Register`，不是「反注册」** ——
   `CollectSpiritStone__OnReset.c` / `CreateSecret__OnReset.c` 与各自 `OnInitialize.c` **逐字同形**，只是**事件 token 不同**
   （`DAT_184273010` vs `DAT_1842707b0` 那一类）。⇒ 这几行的「反注册那一跳」说法作废，真实形态 = 在**两个不同事件**上各订阅一次。
   **建议的统一口径（请调度台裁）**：这一族判 **A** 是成立的 —— 判据侧给 `OnInitialize/OnReset` 的体（订阅），
   我们侧给**被订阅的那个处理器**（信号总线在本仓不存在，处理器由 `EffectResolver` 直接调）：
   `AddSabotageCard` → `EffectResolver.cs:4221 ctx.Tally.AddSabotage(owner)` → `AchievementTally.cs:231`；
   `CreateSecret` → `EffectResolver.cs:5051 / :13311 ctx.Tally.AddSecret(owner)` → `AchievementTally.cs:225`。
   ⛔ 但这一族一旦判 A，其它线同形态的行要**一起**改（否则口径分裂）⇒ 我没有单方面改状态。

2. **`check_ledger_points` 的「同一落点覆盖两个原版方法 ⇒ 留 B」**（`MilestoneScoreBar.Initialize`、`ProgressBar.SetProgress`）：
   我按这条既有口径**维持 B**，但 §4.1 的「共享落点」例外本身是允许 A 的（条件是「给行号范围 + 说清各自如何被满足」）。
   ⇒ 这两行要升 A，得先请调度台把口径统一（我两条都写清了「怎么分别满足」，可直接照 §4.1 判）。

3. **`ContentSizeFitterMinMax`（3 行）/ `UIPreferedSizeFixer`（2 行）**：按 B92_N5b 裁定③(a) 维持 B。
   本轮补了**方法体逐句**（`ClampSize` = 按两条 per-axis 开关夹 `sizeDelta`；`SetLayoutHorizontal/Vertical` = 基类 + `ClampSize`；
   `ApplyConfig` = 把源件四组布局值搬给目标件 + 按源 rect 定高）。
   ⚠️ 顺手发现两处**判据侧缺陷**：
   * `decomp_full/UIPreferedSizeFixer__CalculateLayoutInputHorizontal.c` 的**内容就是 `ApplyConfig`**（首行逐字 = `UIPreferedSizeFixer__ApplyConfig`）⇒ **文件名与内容不符**（铁律 17 的第 ④ 类），该方法的体在本机**读不到**。
   * `decomp_full/GenericArmyItem__GetID.c` 首行是 `RoomOptions__get_plugins`（**ICF 折叠**，体同形「返回 +0x30 字段」）—— 不是解错。
   * `decomp_full/ListExtensions__GetMax*.c` 与 `__GetMin*.c` **同址、首行都是 `FUN_180d10c50`**（泛型共享体 + ICF）⇒ 判据侧只能按「泛型比较器」读。

4. **`Countdown` 的实例面查不下去**（B92_N5 要求「先按 `m_Script` pathID 反查实例」）：
   `Countdown` 的 MonoScript **不在** `assets_full/bundle_Waprforge_monoscripts`（722 个 addressable 类）里，
   也**不在** `_tmp_view/apk135/monoscripts.json` ⇒ 这条路要换包才走得通，**本轮没做成** ⇒ 维持 B（判据已补全）。

## 六、顺手发现（不属我这 61 条，但有据可查）

1. 🔴 **`PracticeModePopup.cs:515` 那条 🔶 标记的「理由」是错的**：写着「原版 `Warlord Name` 宽 0、靠 `ContentSizeFitter` + `RectSizeLimiter` 运行时算（**原版判据拿不到**）」——
   `RectSizeLimiter__SetLayoutHorizontal/Vertical.c` **全文可读**（夹 `minSize/maxSize` + 发一次 SizeChanged）。
   ⇒ 该 🔶 标记不是「服务器相关」那一类，按 §18·0 应改成「该复刻」；**我没动代码**（避免与 D 线撞车），请调度台派给 `Shell/PracticeModePopup.cs` 的写手。
2. `ReferralContainer__OnClick.c` 的**首行是 `ReferralContainer__TryPurchase`**（两个方法体逐字相同）—— ICF 折叠，**不是**「文件名与内容不符」那一类。
3. `Shell/ReferralPopupWindow.cs` **全文件 0 处 `Price|Setup`** ⇒ 推荐人窗的价签那一格确实没有（`ReferralContainer.Refresh` 判 C 的实证）。
4. `Shell/ItemDrawer.cs` 里 `public string Id;` 有**两处**（`:126` = `ItemSpec`、`:240` = 另一个 struct）—— 引落点时别按名字唯一化（我用 `:126`）。

## 七、交接给下一个碰这批的代理

* **补丁**：`账/_parts/B93_U1.tsv`（61 行、11 列、首行表头、裸 `split("\t")`、回环校验过；`轮次` 列是**追加** `· B93_U1` 不是覆盖）。
* **生成脚本**：`账/_parts/_gen_B93_U1.py`（`python -I -X utf8` 可重跑；再跑一次只会重写同内容）。
* **我改的唯一源码**：`CardPresentation/Core/UguiRect.cs`（+54 行，全在 EOF；既有行号零位移）。
* **还没做的事**：① 把 §3.1 的断言粘进 `Editor/ShellScene.cs`（E27）；② §五 三条口径裁定；
  ③ `RectSizeLimiter` 两条 C 要读 prefab 的 `minSize/maxSize` 再实现夹宽（判据已齐，只差 prefab 字段值）。

## 八、调度台裁定回执（2026-10-28 · `账/_parts/B93_U1b.tsv`，6 行 · 全部判 A）

### ① 「`Signal.Register` 订阅族」→ 4 行判 A：**已落地，但裁定里的事实前提被我现核推翻，已就地更正**

* 裁定前提「`OnReset` 也是 `Signal.Register`」**不成立**。真相 = **`OnReset` 走的是 `Signal.Unregister`**：
  两源 ILSpy 一致（`out_cpp2il_dev_ilspy/{CreateSecret,CollectSpiritStone,AddSabotageCard}.cs:9-18` ·
  `out_il2csharp/Assembly-CSharp/同名文件`），而 `dump.cs:1143072-1143130` 显示
  **`Signal.Register<T>(Action<T>)` 与 `Signal.Unregister<T>(Action<T>)` 同址（RVA `0xDA0A00`）**
  ⇒ `decomp_full/*.c` 把两个调用印成同一个符号名（`Signal__Register`），**那个名字分不出 Register/Unregister**。
  （= CLAUDE.md 已记的「同址多方法 ⇒ 符号名是别人」那一族的**泛型版**；我上一轮就是踩了这个。）
  🥇 **判据排序**：ILSpy 的**调用目标**（MemberRef）> Ghidra 的**符号名**。
* **状态仍按裁定判 A（4 行）**，只把**理由换成账里已有的先例口径**：
  `AddSabotageCard.OnReset` 早就是 A（B92_N6 2026-10-28），理由 = 「无总线 + **每局一份 `Tally`**
  （`RuleEngine/Core/BattleContext.cs:348`）承载订阅/退订语义」。
* `B93_U1b.tsv` 里已把 B93_U1 写错的两处改掉：备注里的「不是反注册」→ 更正为 **`Unregister`**；
  `CollectSpiritStone.OnReset` 的判据要点恢复为「反注册（`Signal.Unregister`）」；
  `CreateSecret.OnReset` 的判据要点**回退**（它原来写的 `Signal.Unregister` 本来是对的）。

### ② `MilestoneScoreBar.Initialize` / `ProgressBar.SetProgress` → **A，已落地**

按 §4.1 **共享落点例外**（同一落点覆盖两个原版方法**本来就允许 A**），两行「差异」列都写清了「各自怎么被满足」，
并注明 **2026-10-25 那条「同一落点覆盖两个原版方法 ⇒ 留 B」作废**。

### ③ `Shell/PracticeModePopup.cs:515` 的 🔶 → **知悉，未动该文件**（按裁定已转 N3）

### ④ 🔴 现核发现并已修：B93_U1 的补丁把「判据要点」写进了【判据出处】列（off-by-one）

* 生成器 `_gen_B93_U1.py` 里写的是 `r[3] = upd['judge']`，而 11 列里 **`r[3]` = 判据出处、`r[4]` = 判据要点** ⇒
  **49 行的判据出处被正文覆盖、判据要点留着旧值**（其余 12 行没传 `judge`，未受损）。
* 修复方式（**不重写已合并的那份**，另出补丁）：
  * `B93_U1b.tsv`（6 行）在判 A 的同时，把这 6 行的【判据出处】**恢复成 `HEAD` 原值**、正文写进【判据要点】；
  * `B93_U1c.tsv`（**44 行，纯列修复**）覆盖其余受损行：出处恢复 `HEAD` 原值、正文并回判据要点；
    **状态 / 落点 / 差异 / 等级一个字没动**；键与 `B93_U1b` **完全不重叠**（无跨补丁冲突）。
  * 出处基准 = `git show HEAD:Unity/资料/完全复刻/账/未分类_其他.tsv`（已与 `账_bak_同步点_1028` 比对：
    只有 2 行不同，且 `HEAD` 那份更全 ⇒ 用 `HEAD`）。
* **合并顺序**：`B93_U1`（**已并入主表**）→ `B93_U1b` → `B93_U1c`（文件名序天然就是这个顺序；`U1b`/`U1c` 各自字典序在后 ⇒ 赢，结果正确）。
* 📌 **教训（值得进 CLAUDE.md 的坑表）**：账补丁的回环校验不能只数「11 列」——
  本轮就是这么过的（列数对、键也对，**内容错列**却全绿）。**加一条硬校验：`判据出处` 必须含 `.cs:` / `decomp_full/` 之类出处特征，且不得等于 `判据要点`。**

### 合并提示

* `B93_U1.tsv`（61 行）**已经并入主表**（备注里带 `· B93_U1`）；本次**只需再合 `B93_U1b.tsv`**。
* `check_patch_conflicts.py` 现读 8 条跨补丁重复键，其中 6 条正是这两份的同一批行（`B93_U1b` 字典序在后 ⇒ **赢**，结果正确）；
  另 2 条是 `B93_D3` / `B93_N4`（不是我的）。
* 按规矩建议合并后把 `B93_U1.tsv` 归档到 `账/_parts/_merged_B93_U1/`（否则它是「旧内容定时炸弹」）。

'''

doc = doc.replace('{表}', table)
io.open(OUT, 'wb').write(doc.encode('utf-8'))
print('report rows', len(mine), 'bytes', len(doc.encode('utf-8')))
