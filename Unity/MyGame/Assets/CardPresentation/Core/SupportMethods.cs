// SupportMethods.cs — 原版 `SupportMethods` 里那族**通用 UI 小工具**的等价物
//                    （透明 / 排序层 / 本地化赋值 / Rect 运算 / 子物体清理）
//
// ============================ 判据（唯一一份） ============================
// 🥇 `d:/2/tools/decomp_full/SupportMethods__<名字>.c`（Ghidra，**方法体完整** —— 每个成员都亲读过）
// 🥈 交叉 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SupportMethods.cs`（另一份**带真体**的 C#；
//    ⚠️ 同目录的 `out_cpp2il_ilspy` / `out_il2csharp` / `out_cpp2il_dif` 三份**只有签名壳**，
//    拿它们做交叉验证会得到「全空」的假结论 —— 见 `CLAUDE.md` §三 那条四份产物的规矩）。
//
// | 本文件成员 | 原版行号（ilspy dev） | 原版方法体一句话 |
// |---|---|---|
// | `SetTextSortingOrder` | `:144` | `textObj.sortingOrder = n`，再把每个 `TMP_SubMesh` 子件的 `MeshRenderer.sortingOrder` 也设成 `n` |
// | `GetChildGameObject`  | `:1281` | 在 `fromGameObject` 的**所有 SpriteRenderer 子件**里按 `gameObject.name == withName` 找**第一个**，返回那个 `SpriteRenderer`（名字骗人：不是 GameObject） |
// | `SetTerm`             | `:1314` | 本地化赋值：有 I2 `Localize` 组件就 `enabled = 有译文` + `Term = term`，没有就直接写 `GetTranslation(term)`；**没译文 ⇒ 文本清空**（`""`） |
// | `SetAllChildrenAlpha` | `:1987` | 遍历**所有子件**的 `SpriteRenderer` / `Image` / `TextMeshPro` / `TextMeshProUGUI`，逐个 `color.a = newAlpha` |
// | `ShowRacePlaque`      | `:2521` | `return card.keyword != Keyword.none;`（**查询**，不是「显示」；`Keyword` = 兵种：Infantry/Vehicle/Monster/Building/Battlesuit/Drone/Beast/Daemon） |
// | `SetSpriteAlpha(SpriteRenderer)` | `:2933` | `if (x != null) { var c = x.color; c.a = newAlpha; x.color = c; }` |
// | `SetImageAlpha`       | `:2944` | 同上（形参是 `Image`）—— Ghidra 里这两份体**逐字节同形** |
// | `SetSpriteAlpha(Image)` | `:2955` | 同上 |
// | `SetTextAlpha(TextMeshPro)` | `:2966` | 同上（形参是 `TextMeshPro`） |
// | `SetTextAlpha(TextMeshProUGUI)` | `:2977` | 同上 |
// | `With`                | `:3500` | `new Rect(x ?? r.xMin, y ?? r.yMin, width ?? r.width, height ?? r.height)` —— **逐属性覆写，null = 保持原值** |
// | `Offset`              | `:3523` | `new Rect(r.xMin + (x ?? 0), r.yMin + (y ?? 0), r.width + (width ?? 0), r.height + (height ?? 0))` |
// | `Multiply`            | `:3545` | `new Rect(r.xMin * (x ?? 1), r.yMin * (y ?? 1), r.width * (width ?? 1), r.height * (height ?? 1))` |
// | `DestroyAllChildren`  | `:3363` | `foreach (Transform t in transform) Object.Destroy(t.gameObject);` |
//
// ⚠️ **`Offset` / `Multiply` 作用在 `xMin` / `yMin` 上，不是 `x` / `y`**（Unity 的 `Rect` 存的就是
//    `m_XMin / m_YMin / m_Width / m_Height`）⇒ `Multiply` 的 x 会**同时改变位置与宽度**的一半语义
//    （锚点恒定在左下角）。照抄，⛔ 别「顺手修成居中缩放」。
// ⚠️ `With` / `Offset` / `Multiply` 的形参是**可空浮点**，默认值 分别是「保持 / 0 / 1」——
//    三者各自不同，`Offset` 的 `?? 0f` 与 `Multiply` 的 `?? 1f` 在反汇编里是**两个不同常量**
//    （`DAT_1834b2bb8 = 1f` 只在 `Multiply` 里被加载）。
//
// ============================ 谁在用（原版 = 全树 `SupportMethods.<名字>(` 计数）============================
//   · `SetSpriteAlpha` **27** · `SetTextAlpha` **13** · `SetImageAlpha` **2** · `SetTextSortingOrder` **1**
//     · `DestroyAllChildren` **40** —— 这些是真在跑的（淡入淡出 / 列表重建），我们也**有等价效果**，
//     只是**散在各处内联**（`c.a = v; x.SetColor(c)` / `SetRenderQueue` / 各处 `DestroyImmediate` 循环），
//     没有共用助手。清单见账 `战斗逻辑驱动.tsv` 的那几行。
//   · `SetTerm` / `GetChildGameObject` / `SetAllChildrenAlpha` / `Multiply` / `Offset` / `With`
//     · `ShowRacePlaque` —— **原版本 build 0 调用点**（全树 grep 只命中自身声明）。
//     ⇒ 它们是死工具，**没有生产行为可核**；本文件照原版逐句搬过来只为「类对类」完整，
//        ⛔ 别在账里把它们记成「已验证的观感」（备注要写明「原版 0 调用点」）。
//
// ============================ 我们这边的取舍（如实标）============================
//   · **`SetTerm` 的签名换了**：原版收 `TextMeshPro` + 依赖 I2 `Localize` 组件；本工程**没有 I2**
//     （本地化走 `Loc` / `CardText.TermOr`），文本层是 `Label`（`Battle/Label.cs`）⇒ 这里收 `Label`，
//     语义照原版三段：**有译文 ⇒ 写译文；没译文 ⇒ 写空串**（原版那句 `if (flag) textObj.text = "";`）。
//   · **`DestroyAllChildren` 加了批处理分支**：原版逐个子件 `Object.Destroy`（销毁延迟到帧末 ⇒
//     边遍历边销毁安全）。本工程大量路径在 **`Application.isPlaying == false`** 下跑，而那时
//     `Destroy` **不生效**（`CLAUDE.md` §三 已记）⇒ 这里取快照后：播放中走 `Destroy`、
//     非播放走 `DestroyImmediate`。见该方法上的 `🔶 自己设计` 标记。
//   · **`ShowRacePlaque` 收 `CardDef`**：原版读 `RawCardScript.keyword`（`Keyword` 枚举，`0x98`）；
//     我们的 `CardDef` 只暴露 `Subtype`（字符串，**多义**：既装兵种也装 `Spell`/`Tactic`/`Warlord`
//     这些卡种）⇒ 必须按 `Keyword` 那 8 个成员逐个比，⛔ **不能**写成「`Subtype` 非空」。
//     现读偏差：池内 1,128 张里 **3 张**（`subtype="Warlord"` 而 SO `keyword=5 Infantry`）会判假。
//     根治要引擎线把 `RawCardScript.keyword` 落进卡表（见报告 §⑥）。
//   · **`SetTextSortingOrder` 在生产上我们走 `Label.SetRenderQueue`**（`Battle/Label.cs:263`）——
//     UGUI 的 `sortingOrder` 与渲染队列是两套排序规则；我们全程世界空间 mesh ⇒ 只用队列。
//     本函数按原版留一份（UGUI 那一侧将来要用）。
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `SupportMethods` 的**工具族**（透明 / 排序 / 本地化 / Rect / 子物体）。
    /// 判据与「谁在用」见本文件头。</summary>
    public static class SupportMethods
    {
        // ─────────────────────────────────────────────────────────────────────────
        //  ① 透明（5 个重载，原版 5 份体**同形**：`c = x.color; c.a = newAlpha; x.color = c;`）
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.SetSpriteAlpha(SpriteRenderer, float)`（`SupportMethods.cs:2933`）。
        /// 空引用 ⇒ 什么都不做（原版那句 `op_Equality(x, null)` 早退）。</summary>
        public static void SetSpriteAlpha(SpriteRenderer image, float newAlpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = newAlpha;
            image.color = color;
        }

        /// <summary>原版 `SupportMethods.SetImageAlpha(Image, float)`（`SupportMethods.cs:2944`）。</summary>
        public static void SetImageAlpha(Image image, float newAlpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = newAlpha;
            image.color = color;
        }

        /// <summary>原版 `SupportMethods.SetSpriteAlpha(Image, float)`（`SupportMethods.cs:2955`）。
        /// ⚠️ 与 <see cref="SetImageAlpha"/> 在反汇编里**是同一份体**（Ghidra 只落了一份 `.c`）
        /// —— 两个名字都留着，因为原版有两个（旧调用点用的是这个）。</summary>
        public static void SetSpriteAlpha(Image image, float newAlpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = newAlpha;
            image.color = color;
        }

        /// <summary>原版 `SupportMethods.SetTextAlpha(TextMeshPro, float)`（`SupportMethods.cs:2966`）。</summary>
        public static void SetTextAlpha(TextMeshPro text, float newAlpha)
        {
            if (text == null) return;
            Color color = text.color;
            color.a = newAlpha;
            text.color = color;
        }

        /// <summary>原版 `SupportMethods.SetTextAlpha(TextMeshProUGUI, float)`（`SupportMethods.cs:2977`）。</summary>
        public static void SetTextAlpha(TextMeshProUGUI text, float newAlpha)
        {
            if (text == null) return;
            Color color = text.color;
            color.a = newAlpha;
            text.color = color;
        }

        /// <summary>原版 `SupportMethods.SetAllChildrenAlpha(GameObject, float)`（`SupportMethods.cs:1987`）：
        /// **整棵子树**（四类组件各自一批）全部设成同一个 alpha。
        /// <para>⚠️ `GetComponentsInChildren&lt;T&gt;()` **不含未激活的子件**（原版调的就是这个无参重载）
        /// —— 所以「隐藏的层不会被设 alpha」，照抄。</para>
        /// <para>⚠️ 顺序照原版：`SpriteRenderer` → `Image` → `TextMeshPro` → `TextMeshProUGUI`
        /// （同一个 GameObject 上既能挂 `Image` 又能挂 TMP 的情况会**各写一次**，后写的赢；照抄）。</para>
        /// <para>⚠️ **原版本 build 0 调用点**（死工具）。</para></summary>
        public static void SetAllChildrenAlpha(GameObject baseObject, float newAlpha)
        {
            if (baseObject == null) return;

            SpriteRenderer[] sprites = baseObject.GetComponentsInChildren<SpriteRenderer>();
            if (sprites != null)
                for (int i = 0; i < sprites.Length; i++) SetSpriteAlpha(sprites[i], newAlpha);

            Image[] images = baseObject.GetComponentsInChildren<Image>();
            if (images != null)
                for (int i = 0; i < images.Length; i++) SetImageAlpha(images[i], newAlpha);

            TextMeshPro[] tmps = baseObject.GetComponentsInChildren<TextMeshPro>();
            if (tmps != null)
                for (int i = 0; i < tmps.Length; i++) SetTextAlpha(tmps[i], newAlpha);

            TextMeshProUGUI[] tmpsUgui = baseObject.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpsUgui != null)
                for (int i = 0; i < tmpsUgui.Length; i++) SetTextAlpha(tmpsUgui[i], newAlpha);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ② 排序层
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.SetTextSortingOrder(int, ref TextMeshPro)`（`SupportMethods.cs:144`）：
        /// 设 TMP 自己的 `sortingOrder`，**并且**把每个 `TMP_SubMesh` 子件的 `MeshRenderer.sortingOrder`
        /// 也设成同一个数（否则子件（描边/阴影层）会留在旧层、被别的面板盖住）。
        /// <para>⚠️ 原版的参数是 `ref TextMeshPro`（历史签名）；`ref` 在体里**没被写回**
        /// —— 语义就是「传引用省一次拷贝」。这里收普通参数（同一语义，⛔ 不是简化）。
        /// 只有调用点需要 `ref` 写法时才要改回来。</para>
        /// <para>⚠️ 我们的**生产路径用渲染队列**（`Label.SetRenderQueue`，见 `Battle/Label.cs:263`）：
        /// 世界空间 mesh 的透明物件按「到相机的 3D 距离」排序，UGUI 的 `sortingOrder` 不参与
        /// （`CLAUDE.md` §三「分层要用渲染队列，不能用 z」）。本函数只在 UGUI 那一侧有意义。</para></summary>
        public static void SetTextSortingOrder(int sortOrder, TextMeshPro textObj)
        {
            if (textObj == null) return;
            textObj.sortingOrder = sortOrder;
            TMP_SubMesh[] subs = textObj.GetComponentsInChildren<TMP_SubMesh>();
            if (subs == null) return;
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i] == null) continue;
                MeshRenderer mr = subs[i].GetComponent<MeshRenderer>();
                if (mr == null) continue;
                mr.sortingOrder = sortOrder;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ③ 本地化赋值
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.SetTerm(TextMeshPro, string)`（`SupportMethods.cs:1314`）的等价物。
        /// <para>原版三段（逐句）：
        /// ① `flag = string.IsNullOrEmpty(term) || string.IsNullOrEmpty(LocalizationManager.GetTranslation(term))`
        ///    —— **「有没有译文」**；
        /// ② 有 I2 `Localize` 组件 ⇒ `loc.enabled = !flag; loc.Term = term;`（交给 I2 自己刷）；
        ///    没有 ⇒ `textObj.text = GetTranslation(term);`
        /// ③ **`if (flag) textObj.text = "";`** —— **没译文就把字清空**（不印键名）。</para>
        /// <para>我们这边：**没有 I2**（本地化走 `Loc` / `CardText.TermOr`），文本层是
        /// <see cref="Label"/>，所以 ②③ 合成一句「有译文写译文、没译文写空串」。
        /// ⚠️ 与 `CardText.Term`（缺键**印键名 + 出声**）**是两个用途**：那一条是「必须印点什么」，
        /// 这一条是「没有就别印」—— ⛔ 别互相替换。</para>
        /// <para>⚠️ **原版本 build 0 调用点**（死工具）；本工程同语义的生产路径是
        /// `CardText.TermOr(term, null)` 取到 null 就不写（各处内联）。</para></summary>
        public static void SetTerm(Label textObj, string term)
        {
            if (textObj == null) return;
            // 「有没有译文」——原版问的是 LocalizationManager.GetTranslation，我们问 TermOr：
            // 它三档都查过（手写表 → 官方全表 → 出声），查不到才回 null。
            string v = CardText.TermOr(term, null);
            textObj.SetText(string.IsNullOrEmpty(v) ? "" : v);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ④ Rect 运算（三个都是**纯函数**，作用在 xMin/yMin 上）
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.With(Rect, float?, float?, float?, float?)`（`SupportMethods.cs:3500`）：
        /// **逐属性覆写**，`null` = 保持原值。
        /// <para>⚠️ 形参虽叫 `x` / `y`，原版写的是 `m_XMin` / `m_YMin`（`Rect` 的存储名）——
        /// 语义等同 `rect.x` / `rect.y`，因为 `Rect.x == xMin`。照抄。</para></summary>
        public static Rect With(Rect rect, float? x = null, float? y = null, float? width = null, float? height = null)
        {
            return new Rect(x ?? rect.xMin, y ?? rect.yMin,
                            width ?? rect.width, height ?? rect.height);
        }

        /// <summary>原版 `SupportMethods.Offset(Rect, ...)`（`SupportMethods.cs:3523`）：**逐属性相加**，
        /// `null` = **加 0**（`?? 0f`，不是「保持」—— 与 <see cref="With"/> 的默认值不同！）。</summary>
        public static Rect Offset(Rect rect, float? x = null, float? y = null, float? width = null, float? height = null)
        {
            return new Rect(rect.xMin + (x ?? 0f), rect.yMin + (y ?? 0f),
                            rect.width + (width ?? 0f), rect.height + (height ?? 0f));
        }

        /// <summary>原版 `SupportMethods.Multiply(Rect, ...)`（`SupportMethods.cs:3545`）：**逐属性相乘**，
        /// `null` = **乘 1**（`?? 1f`）。
        /// <para>⚠️ 乘的是 `xMin` / `yMin` ⇒ **改 x 会同时把左边界推走、让宽度在视觉上缩水**
        /// （右上角恒定）。原版就是这个语义，⛔ 别「顺手修成以中心缩放」。</para></summary>
        public static Rect Multiply(Rect rect, float? x = null, float? y = null, float? width = null, float? height = null)
        {
            return new Rect(rect.xMin * (x ?? 1f), rect.yMin * (y ?? 1f),
                            rect.width * (width ?? 1f), rect.height * (height ?? 1f));
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ⑤ 子物体
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.GetChildGameObject(GameObject, string)`（`SupportMethods.cs:1281`）。
        /// <para>🔴 **名字骗人**：它在 `fromGameObject.transform.GetComponentsInChildren&lt;SpriteRenderer&gt;()`
        /// 里按 **`gameObject.name` 全等**找第一个，返回的是那个 **`SpriteRenderer`**（不是 GameObject）。
        /// 找不到 ⇒ `null`。⛔ 别「按名字改成返回 GameObject」（原版所有调用点都按 SpriteRenderer 用）。</para>
        /// <para>⚠️ `GetComponentsInChildren&lt;T&gt;()` 不含未激活子件（原版无参重载），
        /// 且**包含自身**（Unity 语义：`fromGameObject` 自己身上的 SpriteRenderer 也在结果里）。</para>
        /// <para>⚠️ **原版本 build 0 调用点**（死工具）。我们按名字找节点的生产路径走
        /// `transform.Find(name)`（只找**直接子件**，见 `CLAUDE.md` §「`transform.Find` 只找直接子件」）
        /// 或 `Shell/MenuDraw` 的节点表 —— 语义不同，⛔ 别拿本函数替换它们。</para></summary>
        public static SpriteRenderer GetChildGameObject(GameObject fromGameObject, string withName)
        {
            if (fromGameObject == null || fromGameObject.transform == null) return null;
            SpriteRenderer[] all = fromGameObject.transform.GetComponentsInChildren<SpriteRenderer>();
            if (all == null) return null;
            for (int i = 0; i < all.Length; i++)
            {
                SpriteRenderer sr = all[i];
                if (sr == null) continue;
                GameObject go = sr.gameObject;
                if (go == null) continue;
                if (go.name == withName) return sr;
            }
            return null;
        }

        /// <summary>原版 `SupportMethods.DestroyAllChildren(Transform)`（`SupportMethods.cs:3363`）：
        /// 销毁一个 Transform 的**所有直接子件**（`foreach (Transform t in transform)` 只枚举一级）。
        /// <para>⚠️ 原版逐个子件 `Object.Destroy(t.gameObject)` —— `Destroy` **延迟到帧末**，
        /// 所以「边遍历边销毁」是安全的。</para>
        /// <para>🔶 自己设计（非原版）：**先取快照、再按 `Application.isPlaying` 分派** · 原版只有
        /// `Destroy`，而本工程大量路径在**批处理**（`isPlaying == false`）下跑，那时 `Destroy` **不生效**
        /// （`CLAUDE.md` §三「批处理下没有帧循环」），照抄会让「清空列表」静默什么都不做 ·
        /// 调度台 2026-10-26 按本工程既有口径落地。</para>
        /// <para>⚠️ 原版本 build **40 个调用点**（列表重建那一族）—— 这是真在跑的语义；
        /// 我们各处用 `DestroyImmediate` 循环内联，尚未共用本助手。</para></summary>
        public static void DestroyAllChildren(Transform transform)
        {
            if (transform == null) return;
            // 快照：DestroyImmediate 会立刻改 childCount，不能边遍历边删。
            var kids = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++) kids.Add(transform.GetChild(i));
            for (int i = 0; i < kids.Count; i++)
            {
                Transform t = kids[i];
                if (t == null) continue;
                GameObject go = t.gameObject;
                if (go == null) continue;
                if (Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ⑥ 兵种铭牌
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.ShowRacePlaque(RawCardScript)`（`SupportMethods.cs:2521`）：
        /// **`return card.keyword != Keyword.none;`** —— 是个**查询**（返回 `bool`，名字里的 "Show" 骗人）。
        /// `Keyword` 枚举 8 个真值：`Infantry 5` · `Vehicle 10` · `Monster 15` · `Building 20` ·
        /// `Battlesuit 25` · `Drone 30` · `Beast 35` · `Daemon 40`（`dump.cs` 的 `Keyword` 枚举）。
        /// <para>⚠️ 我们的 `CardDef` **没有** `keyword` 字段，只有 `Subtype`（字符串，**多义**：
        /// `Infantry/Vehicle/…` 之外还装 `Spell` / `Tactic` / `Warlord` / `Defence` 这些**卡种**）
        /// ⇒ 判据**必须**按那 8 个名字逐个比，⛔ 不能写 `Subtype != ""`（那会把 700 多张法术/战术卡
        /// 都判成「有兵种」）。</para>
        /// <para>⚠️ **原版本 build 0 调用点**（死工具）；卡面上的兵种行是 `CardView` 自己按 `Subtype`
        /// 画的（`CardView.cs` 的 `SubtypeLine → RaceTerm`），**不是**从本函数来的。</para></summary>
        public static bool ShowRacePlaque(RuleEngine.CardDef card)
        {
            if (card == null) return false;
            return IsRaceKeyword(card.Subtype);
        }

        /// <summary>`Keyword` 枚举的 8 个成员名（原版 `RawCardScript.keyword` 的**非 none** 取值）。
        /// ⚠️ 只比名字：我们的 `Subtype` 里 `"Structure"` / `"Beast"` 这类**混着卡种与兵种**，
        /// 现读 `Structure`(3 张) 的 SO `keyword` 全是 **0** ⇒ **不该算兵种**，所以刻意**不在**本表里。</summary>
        static readonly string[] RaceKeywords =
            { "Infantry", "Vehicle", "Monster", "Building", "Battlesuit", "Drone", "Beast", "Daemon" };

        static bool IsRaceKeyword(string subtype)
        {
            if (string.IsNullOrEmpty(subtype)) return false;
            for (int i = 0; i < RaceKeywords.Length; i++)
                if (string.Equals(subtype, RaceKeywords[i], System.StringComparison.Ordinal)) return true;
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ⑦ 本地化记号渲染（原版 `ModifyLocalization`，账 745）
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>原版 `SupportMethods.ModifyLocalization(string)`（`decomp_full/SupportMethods__ModifyLocalization.c`，497 行）。
        /// 把官方词条里的四种记号按**固定顺序**展开成 TMP 能画的东西：
        /// <list type="number">
        /// <item>`((Key,a,b))` → `Modular_Description/&lt;Key&gt;` 的本地化，`{0}`/`{1}` 代参</item>
        /// <item>`$$ID$$`      → `Card_Name/&lt;ID&gt;` 的本地化</item>
        /// <item>`[[枚举名]]`  → `GameStaticData.TraitNameToString(值, true)`</item>
        /// <item>`(N)`（N=0..8）→ `" &lt;sprite name=ManaN&gt;"`（前导空格是真的）</item>
        /// </list>
        /// <para>🔴 **本方法【委托】既有等价物 <see cref="CardIcons.RewriteMarks"/>**，⛔ 不在这里再抄一份四支
        /// （铁律：两处写同一条规则 = 迟早不一致）。`CardIcons.RewriteMarks` 就是这条 497 行纯函数的逐支移植
        /// （判据、逐条缺口语义、四支顺序见它的 doc），本成员只是把「同名可达」这一层补上（账 745 的残差 = 方法不存在）。
        /// </para>
        /// <para>语档：原版 `ModifyLocalization` 用**当前语档**的 `LocalizationManager`；这里按
        /// `Loc.Current == AvailableLanguages.Chinese` 取中文列（其它语档由 `CardIcons.OfficialTerm` 的
        /// `Loc.Lang13Current` 走当前语言列，与本 flag 无关 —— 该口径见 `RewriteMarks` 的 doc）。</para>
        /// <para>空/`null` 入参：原版 `IsNullOrEmpty ⇒ return baseString ?? ""`（`.c:489-493`）—— 照抄。</para></summary>
        public static string ModifyLocalization(string baseString)
        {
            if (string.IsNullOrEmpty(baseString)) return baseString ?? "";
            return CardIcons.RewriteMarks(baseString, Loc.Current == AvailableLanguages.Chinese);
        }
    }
}
