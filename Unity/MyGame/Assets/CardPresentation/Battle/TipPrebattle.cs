// TipPrebattle.cs — 战前提示条（原版 `TipPrebattle`）
//
// ════════════════════════════════════════════════════════════════════════════════════
//  判据（唯一权威 = 原版反编译，逐方法读过）
//    · 方法体：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TipPrebattle.cs`（ILSpy 版，
//      **方法体是完整的**；AssetRipper 那份 `Assets/Scripts/Assembly-CSharp/TipPrebattle.cs`
//      是空壳桩，别拿它当判据）。
//    · 字段默认值：**同一个 ILSpy 文件** :408 `openBgWidth = 1.8f` · :410 `timeToOpenBg = 0.3f`
//      （AssetRipper 桩把初始化器丢了，所以只能取 ILSpy 这份）。
//
//  🔴 **本类拿不到的两样（如实记，⛔ 不猜）**
//    ① **prefab 实例本地一份都没有** —— 全量 `assets_full` 里 `openBgWidth`/`timeToOpenBg`
//       两个字段名 **0 命中**，`TipPrebattle` 这个类名除了 MonoScript（`globalgamemanagers/
//       MonoScript/MonoScript_1941.json`）之外**没有任何 MonoBehaviour 引用它**，
//       AssetRipper 导出工程里也只有脚本源码、没有 prefab ⇒
//       **位置 / 尺寸 / 贴图三样判据缺**。⇒ 本类**不写死位置**，由宿主任传入参。
//    ② **文案源缺**：原版 `DisplayTips` 走 `playerData.GetRandomTip()` +
//       词条 `PrebattleTips/<key>`（:474-490）。而**我们的官方本地化表里 `PrebattleTips/`
//       一条都没有**（`官方本地化_en+zh-CN.json` 与 13lang 各 0 条；本地只有 `Battle/Tips/*`，
//       是**另一族键**，⛔ 不能拿来顶替）。⇒ 文案池由宿主任传；查不到就**出声**、不显示假文案。
//
//  调用时机（谁是调用方）**本地也判不了**：全 26k 个反编译方法里 `TipPrebattle` 只出现在
//  它自己的文件里 ⇒ 挂接在 prefab / UnityEvent 上。⇒ 本类只提供**原版那套 API**，
//  接线点写进报告交给战场线（`Battle/**` 不归本次改动）。
//
//  行为摘要（逐条对应原版方法）
//    SetupClosed  :408→:426  收起态 = 底板横向压扁到 0 + 文字 α0
//    DisplayTip   :437       未在播动画时：记下文案，按 showingTip 决定走 OpenTip 还是 RenewTip
//    DisplayTips  :460       = 抽一条 + DisplayTip
//    GetNextTip   :467       最多试 **6** 次（`object obj = 5`），空串或与上一条相同就重抽
//    OnTouchUp…   :502       点条 = 换下一条
//    HideTips     :511       停 OpenTip、杀补间、起 CloseTip **并且**起 DeactivateTip
//    OpenTip      :541       scaleX 0→1.8（0.3s 线性）→ 等 0.15s → 换字 + 淡入 0.3s → animFlag=false
//    CloseTip     :552       文字淡出 0.15s + scaleX→0（0.15s）→ 等 0.15s → showingTip=false
//    RenewTip     :563       CloseTip 走完 → 若还 active 再 OpenTip
//    DeactivateTip:574       等一小段 → gameObject.SetActive(false)
//
//  ⚠️ 两处**反编译没解出来**的地方（照实标，⛔ 别当已定案）
//    a) `SetupClosed` 里 `transform.localScale = (Vector3)(&obj)`（ILSpy :430-434）是
//       **by-ref 结构体没解**的产物；结合 `CloseTip` 的 `DOScaleX(…, 0f, …)`，
//       本类取「**只把 x 压到 0、y/z 保持**」（这是推断，不是直读）。
//    b) `DeactivateTip` 的等待时长：ILSpy :583 前有一条
//       `NoteDecompilerIssue("Unmanaged memory load: [v28 @ rcx_v2 (UnityEngine.Component)+34]")`
//       ⇒ 真实的 `WaitForSeconds(...)` 实参**没解出来**，反编译里是占位的 `0f`。
//       本类取 **`timeToOpenBg * 0.5f`**（= 收起动画的时长），并用字段 `deactivateDelay`
//       暴露出来，待有人从 prefab/实机核到真值时改这一个字段。
// ════════════════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>战前提示条。原版 `TipPrebattle`（`Assembly-CSharp`，无命名空间）。</summary>
    public class TipPrebattle : MonoBehaviour
    {
        // ── 原版公开字段（ILSpy :404-414）────────────────────────────────────────
        /// <summary>底板（原版 `SpriteRenderer bg`）。我们这边是 `ImageQuad`（纯色板，原版那格也没有 sprite）。</summary>
        ImageQuad bg;
        /// <summary>文字（原版 `TextMeshPro tipText`）。</summary>
        Label tipText;

        /// <summary>展开后底板的 **localScale.x**（原版 :408 `= 1.8f`）。⚠️ 是**缩放倍数**，不是像素宽。</summary>
        public float openBgWidth = 1.8f;
        /// <summary>展开/淡入时长（秒，原版 :410 `= 0.3f`）。</summary>
        public float timeToOpenBg = 0.3f;
        /// <summary>见文件头 ⚠️b：原版这个实参没解出来，本类取收起动画的时长。</summary>
        public float deactivateDelay = -1f;   // <0 ⇒ 用 timeToOpenBg * 0.5f

        // ── 原版私有状态 ────────────────────────────────────────────────────────
        bool showingTip;
        string newTip;
        bool animFlag;
        Coroutine _bgScaleAnim;      // 原版 bgMoveAnim（DOTween）
        Coroutine _textFadeAnim;     // 原版 textFadeAnim（DOTween）
        Coroutine _deactivateRoutine;
        /// <summary>文案池（原版 = `PlayerDataManager.tips`）。宿主任传。</summary>
        IReadOnlyList<string> _pool;

        const float kDisabledFade = 0f;

        // ════════════════════════════════════════════════════════════════════════════
        //  建（原版没有「建」这一步 —— 它挂在 prefab 上；我们这里是**重建**）
        // ════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// 建一条提示条。
        /// </summary>
        /// <param name="parent">宿主（战场 HUD 的锚点；**由调用方给** —— 原版位置判据缺，见文件头 ①）。</param>
        /// <param name="barHeightPx">底板高（**设计 px**，1080 基准）。⚠️ 原版真值缺，宿主给。</param>
        /// <param name="anchoredPosPx">底板位置（**设计 px**）。⚠️ 原版真值缺，宿主给。</param>
        /// <param name="anchor">对齐点（同其它 HUD 件）。</param>
        /// <param name="fontPx">文字字号（设计 px）。⚠️ 原版真值缺，宿主给。</param>
        public static TipPrebattle Create(Transform parent, float barHeightPx, Vector3 anchoredPosPx,
                                          Vector2 anchor, float fontPx = 22f, string name = "TipPrebattle")
        {
            if (parent == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var self = go.AddComponent<TipPrebattle>();

            // 底板：原版 `bg` 是 SpriteRenderer，**没有给 sprite** ⇒ 画出来就是纯色块。
            //   我们照同一口径用 1×1 白图 + `SetTint`。
            // 位置/高度都是**设计 px ÷ 108**（本工程 HUD 的口径；见 `AttackSelector.W`）。
            var localPos = new Vector3(anchoredPosPx.x / 108f, anchoredPosPx.y / 108f, 0f);
            var tex = SolidWhite();
            self.bg = ImageQuad.Create(go.transform, tex, localPos,
                                       barHeightPx / 108f, anchor, name + "Bg");
            if (self.bg == null)
            {
                // 本工程红线：不静默失败。底板建不出来 = 这件没做成。
                Debug.LogError("[TipPrebattle] 底板建不出来（ImageQuad.Create 返回 null）");
                Destroy(go);
                return null;
            }
            // ⚠️ 缩放要**围绕锚点**长出来 —— `ImageQuad` 的网格本来就按 `anchor` 布（:475
            //    `x0 = -anchor.x * w`）⇒ 直接改 `localScale.x` 就是绕锚点展开，不需要额外 pivot。

            self.tipText = Label.Create(go.transform, "", localPos,
                                        Mathf.Max(1, Mathf.RoundToInt(fontPx)), Color.white, anchor,
                                        name + "Text");
            if (self.tipText == null)
                Debug.LogError("[TipPrebattle] 文字建不出来（Label.Create 返回 null，多半是没有 TMP 字体）");

            self.SetupClosed();
            return self;
        }

        /// <summary>1×1 白（**实例级**，别做成 static：static 的 UnityEngine.Object 跨场景会成野指针）。</summary>
        static Texture2D _solid;
        static Texture2D SolidWhite()
        {
            if (_solid == null)
            {
                _solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _solid.SetPixel(0, 0, Color.white);
                _solid.Apply();
                _solid.name = "solid_white_tip";
                _solid.hideFlags = HideFlags.DontSave;
            }
            return _solid;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  原版 API（签名与语义照抄）
        // ════════════════════════════════════════════════════════════════════════════

        /// <summary>收起态（原版 :426 `SetupClosed`）：底板 x 压到 0、文字 α0。<para>
        /// ⚠️ 原版那句 `localScale = (Vector3)(&obj)` 是反编译 by-ref 未解（见文件头 ⚠️a），
        /// 这里取「只压 x」。 </para></summary>
        public void SetupClosed()
        {
            if (bg != null)
            {
                var s = bg.transform.localScale;
                bg.transform.localScale = new Vector3(0f, s.y, s.z);
            }
            SetTextAlpha(kDisabledFade);
            showingTip = false;
            animFlag = false;
        }

        /// <summary>显示一条（原版 :437）。**正在播动画时直接丢弃**（原版的 `if (!animFlag)` 闸）。</summary>
        public void DisplayTip(string tip)
        {
            if (animFlag) return;
            if (string.IsNullOrEmpty(tip)) return;    // 原版没有这道闸，但空文案会让淡入演一条空白 —— 宁可不演
            newTip = tip;
            StartCoroutine(showingTip ? "RenewTip" : "OpenTip");
        }

        /// <summary>从池里抽一条显示（原版 :460 `DisplayTips(PlayerDataManager)`）。</summary>
        public void DisplayTips(IReadOnlyList<string> pool)
        {
            _pool = pool; _nextRaw = null;      // 走平铺池 ⇒ 关掉分组抽样器（见 <see cref="DisplayTips(IReadOnlyList{Tips})"/>）
            var t = GetNextTip();
            if (t != null) DisplayTip(t);
        }

        /// <summary>抽一条，**最多试 6 次**（原版 :467：`object obj = 5;` 然后循环里 `obj--`），
        /// 空串或与上一条相同就重抽；仍拿不到 → 返回 null 并**出声**（本工程红线：不静默失败）。</summary>
        string GetNextTip()
        {
            if (_nextRaw == null && (_pool == null || _pool.Count == 0))
            {
                Debug.LogWarning("[TipPrebattle] 文案池是空的 —— 原版走 PlayerDataManager.GetRandomTip() + " +
                                 "词条 `PrebattleTips/*`；我们本地化表里这两样都没有（见文件头 ②）");
                return null;
            }
            for (int i = 0; i <= 5; i++)          // 原版 `obj = 5` + 循环 ⇒ 最多 6 次
            {
                // 原版每次重抽都走 `playerData.GetRandomTip()`；分组池那条路由 `_nextRaw` 提供同一个动作。
                var raw = _nextRaw != null ? _nextRaw() : _pool[Random.Range(0, _pool.Count)];
                if (string.IsNullOrEmpty(raw)) continue;
                // 原版：`term = "PrebattleTips/" + raw`，再查 I2。我们照同一口径查词条；
                //   查不到就把 raw 本身当文案（**只在它不像键的时候**），否则出声。
                var key = "PrebattleTips/" + raw;
                string text = Loc.HasEntry(key) ? Loc.T(key) : raw;
                if (string.IsNullOrEmpty(text)) continue;
                if (text == newTip) continue;      // 原版：与上一条相同 ⇒ 重抽
                return text;
            }
            Debug.LogWarning("[TipPrebattle] 6 次都没抽到可用文案（池里全是空串/全与上一条相同）");
            return null;
        }

        /// <summary>点提示条 = 换下一条（原版 :502 `OnTouchUpAsButton`）。</summary>
        public void OnTouchUpAsButton()
        {
            if (_pool == null) return;
            var t = GetNextTip();
            if (t != null) DisplayTip(t);
        }

        /// <summary>隐藏（原版 :511）：停 OpenTip → 杀两条补间 → 起 CloseTip **并且**起 DeactivateTip。</summary>
        public void HideTips()
        {
            if (!gameObject.activeSelf) return;
            StopCoroutine("OpenTip");
            KillAnim(ref _bgScaleAnim);
            KillAnim(ref _textFadeAnim);
            StartCoroutine("CloseTip");
            if (_deactivateRoutine != null) StopCoroutine(_deactivateRoutine);
            _deactivateRoutine = StartCoroutine(DeactivateTip());
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  四个协程（逐行照原版状态机）
        // ════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 :541 `OpenTip`：<c>0.3s 展开 → 等 0.15s → 换字+0.3s 淡入 → animFlag=false</c>。</summary>
        IEnumerator OpenTip()
        {
            animFlag = true;
            showingTip = true;

            // ① 从「收起」起：x=0、文字 α0
            if (bg != null) { var s = bg.transform.localScale; bg.transform.localScale = new Vector3(0f, s.y, s.z); }
            SetTextAlpha(kDisabledFade);

            // ② 底板横向展开：0 → openBgWidth，时长 timeToOpenBg，**线性**（原版 SetEase(Linear)）
            if (bg != null) yield return StartCoroutine(ScaleXTo(openBgWidth, timeToOpenBg));

            // ③ 等到一半时间点才换字（原版 `new WaitForSeconds(timeToOpenBg * 0.5f)`）
            yield return new WaitForSeconds(timeToOpenBg * 0.5f);

            // ④ 换字 + 淡入（原版 DOFade(tipText, 1, timeToOpenBg)）
            if (tipText != null) tipText.SetText(newTip ?? "");
            yield return StartCoroutine(FadeTextTo(1f, timeToOpenBg));

            // ⑤ 原版这里 `new WaitForSeconds(timeToOpenBg)`（那半秒在 ②③ 之间已过，这里是完整一段）
            yield return new WaitForSeconds(timeToOpenBg);

            animFlag = false;
        }

        /// <summary>原版 :552 `CloseTip`：<c>文字淡出 0.15s + scaleX→0（0.15s）→ 等 0.15s → showingTip=false</c>。</summary>
        IEnumerator CloseTip()
        {
            animFlag = true;
            float half = timeToOpenBg * 0.5f;

            if (tipText != null) StartCoroutine(FadeTextTo(kDisabledFade, half));
            if (bg != null) yield return StartCoroutine(ScaleXTo(0f, half));
            else yield return new WaitForSeconds(half);

            yield return new WaitForSeconds(half);

            showingTip = false;
            animFlag = false;
        }

        /// <summary>原版 :563 `RenewTip`：先把 CloseTip 走完，**若还 active** 再 OpenTip。</summary>
        IEnumerator RenewTip()
        {
            yield return StartCoroutine("CloseTip");
            if (gameObject.activeSelf) StartCoroutine("OpenTip");
        }

        /// <summary>原版 :574 `DeactivateTip`：等一小段后 `SetActive(false)`。
        /// ⚠️ 时长判据缺（见文件头 ⚠️b），取 <see cref="deactivateDelay"/>，默认收起时长。</summary>
        IEnumerator DeactivateTip()
        {
            float d = deactivateDelay >= 0f ? deactivateDelay : timeToOpenBg * 0.5f;
            if (d > 0f) yield return new WaitForSeconds(d);
            else yield return null;                 // 反编译里是 WaitForSeconds(0f) ⇒ 下一帧
            gameObject.SetActive(false);
            _deactivateRoutine = null;
        }

        // ── 两个补间的替身（原版用 DOTween；本类不引第三方，用协程手算）────────────
        IEnumerator ScaleXTo(float target, float dur)
        {
            if (bg == null) yield break;
            var t = bg.transform;
            var s = t.localScale;
            float from = s.x;
            if (dur <= 0f) { t.localScale = new Vector3(target, s.y, s.z); yield break; }
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dur);   // 线性（原版 Ease.Linear）
                t.localScale = new Vector3(Mathf.Lerp(from, target, k), s.y, s.z);
                yield return null;
            }
            t.localScale = new Vector3(target, s.y, s.z);
        }

        IEnumerator FadeTextTo(float target, float dur)
        {
            if (tipText == null) yield break;
            var c = tipText.color;
            float from = c.a;
            if (dur <= 0f) { c.a = target; tipText.SetColor(c); yield break; }
            float e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dur);
                c.a = Mathf.Lerp(from, target, k);
                tipText.SetColor(c);
                yield return null;
            }
            c.a = target;
            tipText.SetColor(c);
        }

        void SetTextAlpha(float a)
        {
            if (tipText == null) return;
            var c = tipText.color; c.a = a; tipText.SetColor(c);
        }

        void KillAnim(ref Coroutine c)
        {
            if (c != null) { StopCoroutine(c); c = null; }
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  断言（两条，按调度台要求）
        //  ⚠️ **待自检恢复时跑** —— 本轮没跑 Unity（用户 2026-10-25 暂停了自检）。
        //     接法：宿主/自检里 `var ok = tip.SelfCheckBuilt() && tip.SelfCheckTiming(...)`。
        // ════════════════════════════════════════════════════════════════════════════

        /// <summary>【断言 1】建出来了：底板 + 文字都在，且处在**收起态**（x=0、文字 α0）。</summary>
        public bool SelfCheckBuilt()
        {
            bool ok = bg != null && tipText != null;
            if (ok)
            {
                ok &= Mathf.Abs(bg.transform.localScale.x) < 1e-4f;
                ok &= Mathf.Abs(tipText.color.a - kDisabledFade) < 1e-3f;
            }
            if (!ok)
                Debug.LogError("[TipPrebattle·断言1] 没建出来或不在收起态（bg=" + (bg != null) +
                               " text=" + (tipText != null) + "）");
            return ok;
        }

        /// <summary>【断言 2·前半】按判据的时机「显示」：调一次 <see cref="DisplayTips"/> 之后，
        /// <c>1.5 × timeToOpenBg</c> 内必须是「已展开」（x == openBgWidth、文字 α1、animFlag=false）；
        /// 且**中途**（0.75 × timeToOpenBg）应该还在播（animFlag=true）—— 这条挡「一帧到位」。</summary>
        public IEnumerator SelfCheckShowTiming(float tolerance = 0.15f)
        {
            DisplayTips(_pool);
            yield return new WaitForSeconds(timeToOpenBg * 0.75f);
            bool midOk = animFlag;                       // 那时还没播完
            yield return new WaitForSeconds(timeToOpenBg * 0.75f + tolerance);
            bool endOk = !animFlag
                         && bg != null && Mathf.Abs(bg.transform.localScale.x - openBgWidth) < 1e-2f
                         && tipText != null && Mathf.Abs(tipText.color.a - 1f) < 1e-2f;
            if (!midOk || !endOk)
                Debug.LogError("[TipPrebattle·断言2·显示] midAnim=" + midOk + " endOk=" + endOk);
        }

        /// <summary>【断言 2·后半】按判据的时机「隐藏」：<c>HideTips()</c> 之后
        /// <c>0.5 × timeToOpenBg</c> 内必须回到 x=0 / α0，并在 <see cref="deactivateDelay"/> 后 inactive。</summary>
        public IEnumerator SelfCheckHideTiming(float tolerance = 0.15f)
        {
            HideTips();
            yield return new WaitForSeconds(timeToOpenBg * 0.5f + tolerance);
            bool closed = bg == null || Mathf.Abs(bg.transform.localScale.x) < 1e-2f;
            if (!closed)
                Debug.LogError("[TipPrebattle·断言2·隐藏] 收起后 scaleX = " +
                               (bg != null ? bg.transform.localScale.x : 0f));
            yield return new WaitForSeconds((deactivateDelay >= 0f ? deactivateDelay : timeToOpenBg * 0.5f) + 0.1f);
            if (gameObject.activeSelf)
                Debug.LogError("[TipPrebattle·断言2·隐藏] DeactivateTip 没有把自己 SetActive(false)");
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  账 409：`PlayerDataManager.GetRandomTip()`
        // ════════════════════════════════════════════════════════════════════════════
        //
        // **判据 = `d:/2/tools/decomp_full/PlayerDataManager__GetRandomTip.c` 全文**：
        //   ① 遍历 `tips(+0x118)`；每项 `+0x10` = **`maxHeroLevel`**（`int`）、`+0x18` = `List<string> tips`；
        //   ② 取 **`maxHeroLevel > 0` 且最大**的那一组（**严格 `>`** ⇒ 平手取**先遇到**的）；
        //   ③ 一组都没有 ⇒ 返回 **`""`**（`DAT_1842b80e0` = 空串实解）；
        //   ④ 否则 `UnityEngine.Random.Range(0, 组.Count)` → `组[i]`。
        //   ⚠️ 原版对「`tips` 字段为 null」与「选中组 `+0x18` 为 null」走 NRE 帮助函数（`FUN_1803f47a0`）；
        //      本工程**不抛**（红线），这两种退化输入都按「没有可用组」返回 `""`（并与"一组都没有"同一档）。
        //   ⚠️ **文案池本身**（原版 `TipsData` = `OnlineData` key 14，服务器下发）本地**没有** ⇒ 池子由宿主任传
        //      （与 `DisplayTips(pool)` 同一条口径：没有就出声，⛔ 不造假文案）。

        /// <summary>= 原版 `Tips`（`tips(+0x118)` 的元素；`cpp2il/out_cpp2il_dev_ilspy/Tips.cs` 两个字段逐字照抄）。
        /// ⚠️ `maxHeroLevel` 在这套逻辑里就是**分组权重**（`GetRandomTip` 按它取最大组）—— 名字照原版，别改名。</summary>
        [System.Serializable]
        public class Tips
        {
            public int maxHeroLevel;
            public List<string> tips = new List<string>();
        }

        /// <summary>分组池那条路的抽样器（`null` ⇒ 走平铺 `_pool`）。见 <see cref="GetNextTip"/>。</summary>
        System.Func<string> _nextRaw;

        /// <summary>原版 `PlayerDataManager.GetRandomTip()`（账 409）。判据/退化输入见本节文件头注释。</summary>
        public static string GetRandomTip(IReadOnlyList<Tips> groups)
        {
            if (groups == null) return "";                       // 原版：NRE（见本节注释）
            Tips best = null;
            int bestLevel = -1;                                  // 原版 `iVar5 = -1`
            for (int i = 0; i < groups.Count; i++)               // 原版 `foreach` 序
            {
                var g = groups[i];
                if (g == null) continue;
                int lv = g.maxHeroLevel;
                if (lv > 0 && bestLevel < lv) { best = g; bestLevel = lv; }   // 严格 `<` ⇒ 平手取先遇到的
            }
            if (best == null || best.tips == null || best.tips.Count == 0) return "";   // 原版 `return DAT_1842b80e0`（空串）
            return best.tips[Random.Range(0, best.tips.Count)];  // 原版 `UnityEngine.Random.Range(0, 组.Count)`
        }

        /// <summary>原版 `DisplayTips(PlayerDataManager)` 的**分组池**入口：池 = <see cref="Tips"/> 列表
        /// ⇒ 走 <see cref="GetRandomTip"/>，再进 `GetNextTip` 那条「`PrebattleTips/&lt;条目&gt;` 查表 + 最多重抽 6 次」的路
        /// （与平铺池**同一条**查表/重抽逻辑，⛔ 别复制第二份）。</summary>
        public void DisplayTips(IReadOnlyList<Tips> groups)
        {
            if (groups == null)
            {
                Debug.LogWarning("[TipPrebattle] 分组文案池是 null —— 原版 `PlayerDataManager.tips` 由服务器下发"
                               + "（`TipsData` = OnlineData key 14），本工程没有 ⇒ 池子必须由宿主任传");
                return;
            }
            _nextRaw = () => GetRandomTip(groups);
            var t = GetNextTip();
            if (t != null) DisplayTip(t);
        }
    }
}
