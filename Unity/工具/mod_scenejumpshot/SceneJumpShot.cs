using System;
using System.Collections;
using System.IO;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(SceneJumpShot.SceneJumpMod), "SceneJumpShot", "3.6.0", "CC")]
[assembly: MelonGame("Everguild", "Warpforge")]

namespace SceneJumpShot
{
    public class SceneJumpMod : MelonMod
    {
        private const string BundlePathSuffix = "aa/StandaloneWindows64";
        private const string CfgPath = "d:/2/unity_run_ref/UserData/sjs_dump_cfg.txt";
        private readonly System.Collections.Generic.List<AssetBundle> _bundles = new System.Collections.Generic.List<AssetBundle>();

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("[SJS] v3.6 BattleDrive + Il2Cpp.PlayerHand extractor loaded");
            try { Application.runInBackground = true; } catch { }   // v4.0: 失焦也要出帧, 否则 WaitForEndOfFrame 永远不返回
            MelonCoroutines.Start(Boot());
        }

        private string Esc(string s)
        {
            return (s ?? "").Replace("\t", " ").Replace("\n", "\\n").Replace("\r", "");
        }

        private void DumpGo(Transform t, string path, StreamWriter w)
        {
            var go = t.gameObject;
            var sb = new StringBuilder();
            sb.Append(path).Append('\t').Append(Esc(go.name))
              .Append('\t').Append(go.activeSelf).Append('\t').Append(go.activeInHierarchy);
            try
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var ap = rt.anchoredPosition; var sd = rt.sizeDelta;
                    var amin = rt.anchorMin; var amax = rt.anchorMax; var pv = rt.pivot;
                    sb.Append("\tRT\t").Append(ap.x.ToString("F1")).Append(',').Append(ap.y.ToString("F1"))
                      .Append('\t').Append(sd.x.ToString("F1")).Append(',').Append(sd.y.ToString("F1"))
                      .Append('\t').Append(amin.x.ToString("F3")).Append(',').Append(amin.y.ToString("F3"))
                      .Append('\t').Append(amax.x.ToString("F3")).Append(',').Append(amax.y.ToString("F3"))
                      .Append('\t').Append(pv.x.ToString("F3")).Append(',').Append(pv.y.ToString("F3"));
                }
                else
                {
                    sb.Append("\tTF\t\t\t\t\t");
                }
            }
            catch { sb.Append("\tRT?\t\t\t\t\t"); }

            try
            {
                // TMP 程序集在 cpp2il 下编译引用不可见 → 运行时反射 (2026-08-25)
                object comp = null;
                try
                {
                    var tmpT = Il2CppSystem.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    comp = tmpT != null ? go.GetComponent(tmpT) : null;
                }
                catch { comp = null; }
                if (comp != null)
                {
                    var st = comp.GetType();
                    var txtF = st.GetField("m_text");
                    var fsF = st.GetField("m_fontSize");
                    string txt = txtF != null ? (txtF.GetValue(comp) ?? "").ToString() : "";
                    object fso = fsF != null ? fsF.GetValue(comp) : null;
                    float fsv = 0f; float.TryParse((fso ?? "0").ToString(), out fsv);
                    sb.Append("	TXT	").Append(Esc(txt)).Append('	').Append(fsv.ToString("F1"));
                }
                else
                {
                    sb.Append("			");
                }
            }
            catch { sb.Append("	TXT?		"); }

            try
            {
                var img = go.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    string sn = "";
                    try { sn = img.sprite != null ? img.sprite.name : ""; } catch { }
                    sb.Append("\tIMG\t").Append(Esc(sn));
                }
                else
                {
                    sb.Append("\t\t");
                }
            }
            catch { sb.Append("\tIMG?\t"); }

            w.WriteLine(sb.ToString());
            for (int i = 0; i < t.childCount; i++)
            {
                DumpGo(t.GetChild(i), path + "/" + go.name, w);
            }
        }

        private void DumpTree(string sceneName, string outPath)
        {
            try
            {
                using (var w = new StreamWriter(outPath, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("path\tname\tactiveSelf\tactiveInHierarchy\trect\tpos\tpivot\ttext\tfontSize\tcolor\timage");
                    var scene = SceneManager.GetActiveScene();
                    GameObject[] roots = scene.GetRootGameObjects();
                    foreach (var go in roots)
                    {
                        DumpGo(go.transform, go.name, w);
                    }
                }
                LoggerInstance.Msg("[SJS] DUMP " + sceneName + " -> " + outPath + " (" + (new FileInfo(outPath)).Length + " bytes)");
            }
            catch (Exception e)
            {
                LoggerInstance.Msg("[SJS] dump fail: " + e);
            }
        }

        // RT+ReadPixels 截图（ScreenCapture 在 interop 崩，2026-08-25 已验证此路）
        private void CaptureShot(string path, int w = 1920, int h = 1080)
        {
            try
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    var cams = Camera.allCameras;
                    if (cams != null && cams.Length > 0) cam = cams[0];
                }
                if (cam == null) { LoggerInstance.Msg("[SJS] shot fail: no camera"); return; }
                var rt = new RenderTexture(w, h, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                cam.targetTexture = prev;
                RenderTexture.active = prev;
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] shot fail: " + e); }
        }

        // ---------- Il2Cpp.PlayerHand 提取器 v3.6 (2026-08-25): playet/敌方手牌实战数据 TSV ----------
        private string EnumName(object v)
        {
            if (v == null) return "";
            try { return v.ToString(); } catch { return ""; }
        }

        private string CardTypeName(int v)
        {
            switch (v)
            {
                case 0: return "Minion";
                case 10: return "Hero";
                case 20: return "Tactic";
                case 40: return "Whispers";
                default: return "?" + v;
            }
        }

        private string KeywordName(int v)
        {
            switch (v)
            {
                case 0: return "none";
                case 5: return "Infantry";
                case 10: return "Vehicle";
                case 15: return "Monster";
                case 20: return "Building";
                case 25: return "Battlesuit";
                case 30: return "Drone";
                case 35: return "Beast";
                case 40: return "Daemon";
                default: return "?" + v;
            }
        }

        private string Tr(Transform t, int maxDepth = 4)
        {
            string s = "";
            int d = 0;
            while (t != null && d < maxDepth) { s = "/" + t.name + s; t = t.parent; d++; }
            return s;
        }

        private string ParentName(Il2Cpp.CardScript c)
        {
            try
            {
                var t = c.transform;
                if (t == null) return "";
                var p = t.parent;
                return p != null ? p.name : "(root)";
            }
            catch { return "?"; }
        }

        // 返回提取到的总卡数; -1=Il2Cpp.PlayerHand 类型没找到
        private int ExtractPlayerHand(string outDir)
        {
            int total = 0, nPlayerHands = 0;
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outPath = Path.Combine(outDir, "battle_hand_" + ts + ".tsv");
            try
            {
                Il2Cpp.PlayerHand[] hands = UnityEngine.Object.FindObjectsOfType<Il2Cpp.PlayerHand>(true);
                LoggerInstance.Msg("[SJS] hand: FindObjectsOfType<Il2Cpp.PlayerHand> -> " + (hands == null ? 0 : hands.Length));
                if (hands == null || hands.Length == 0)
                {
                    // 兜底: 场景里按 GO 名扫 + Il2Cpp 反射 GetComponent
                    try
                    {
                        var t = Il2CppSystem.Type.GetType("PlayerHand, Assembly-CSharp");
                        LoggerInstance.Msg("[SJS] hand fallback: Il2Cpp type resolved = " + (t != null));
                        int found = 0;
                        foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
                        {
                            var phGo = FindGo(ro.transform, "PlayerHand");
                            while (phGo != null && found < 8)
                            {
                                found++;
                                var comp = t != null ? phGo.GetComponent(t) : null;
                                LoggerInstance.Msg("[SJS] hand fallback: '" + phGo.name + "' comp=" + (comp != null));
                                break;
                            }
                        }
                        LoggerInstance.Msg("[SJS] hand fallback GOs found: " + found);
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] hand fallback err: " + e.Message); }
                    return -1;
                }
                using (var w = new StreamWriter(outPath, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("timestamp\thandName\thandParent\thandIsPlayer\thandActiveInHierarchy\thandCount\t" +
                        "cardIdx\tuniqueInGameID\tcardIsPlayer\tcardParent\tgoName\tactiveSelf\tactiveInHierarchy\t" +
                        "posX\tposY\tposZ\tcardShortId\tcardName\tmanaCost\tcardTypeInt\tcardTypeName\t" +
                        "keywordInt\tkeywordName\tmaxHealth\tarmour\tmeleeAttack\trangeAttack");
                    foreach (var hand in hands)
                    {
                        nPlayerHands++;
                        bool hIsP = false;
                        int hCount = -1;
                        try { hIsP = hand.isPlayer; } catch (Exception e) { LoggerInstance.Msg("[SJS] hand isPlayer err: " + e.Message); }
                        string handName = "", handParent = "", handActive = "";
                        try { handName = hand.gameObject.name; } catch { }
                        try { handParent = hand.transform.parent != null ? hand.transform.parent.name : "(root)"; } catch { }
                        try { handActive = hand.gameObject.activeInHierarchy.ToString(); } catch { }
                        try { hCount = hand.currentHand != null ? hand.currentHand.Count : -1; } catch (Exception e) { LoggerInstance.Msg("[SJS] hand currentHand err: " + e.Message); }
                        LoggerInstance.Msg("[SJS] hand: '" + handName + "' parent=" + handParent + " isPlayer=" + hIsP + " active=" + handActive + " currentHand.Count=" + hCount);
                        if (hand.currentHand == null) continue;
                        for (int i = 0; i < hand.currentHand.Count; i++)
                        {
                            Il2Cpp.CardScript card = null;
                            try { card = hand.currentHand[i]; }
                            catch (Exception e) { LoggerInstance.Msg("[SJS] hand card[" + i + "] access err: " + e.Message); continue; }
                            if (card == null) { w.WriteLine(ts + "\t" + Esc(handName) + "\t" + Esc(handParent) + "\t" + hIsP + "\t" + handActive + "\t" + hCount + "\t" + i + "\t(NULL)"); continue; }
                            total++;
                            string uid = "?", cardIsP = "?", px = "?", py = "?", pz = "?", sid = "?", nm = "?", mana = "?", ct = "?", ctn = "?", kw = "?", kwn = "?", mh = "?", arm = "?", ma = "?", ra = "?", goName = "?", aSelf = "?", aHier = "?", cardParent = "?";
                            Il2Cpp.CardScript c = card;
                            try { uid = c.uniqueInGameID.ToString(); } catch (Exception e) { LoggerInstance.Msg("[SJS] card uniqueInGameID err: " + e.Message); }
                            try { cardIsP = c.isPlayer.ToString(); } catch { }
                            try { goName = c.gameObject.name; aSelf = c.gameObject.activeSelf.ToString(); aHier = c.gameObject.activeInHierarchy.ToString(); cardParent = Esc(ParentName(c)); } catch { }
                            try { var v = c.transform.position; px = v.x.ToString("F2"); py = v.y.ToString("F2"); pz = v.z.ToString("F2"); } catch { }
                            try
                            {
                                var rc = c.rawCardData;
                                if (rc == null) { sid = "NULL"; }
                                else
                                {
                                    sid = rc.cardShortId.ToString();
                                    nm = Esc(rc.cardName);
                                    mana = rc.manaCost.ToString();
                                    ct = ((int)rc.cardType).ToString(); ctn = CardTypeName((int)rc.cardType);
                                    kw = ((int)rc.keyword).ToString(); kwn = KeywordName((int)rc.keyword);
                                    mh = rc.maxHealth.ToString(); arm = rc.armour.ToString();
                                    ma = rc.meleeAttack.ToString(); ra = rc.rangeAttack.ToString();
                                }
                            }
                            catch (Exception e) { LoggerInstance.Msg("[SJS] card rawCardData err: " + e.Message); }
                            w.WriteLine(ts + "\t" + Esc(handName) + "\t" + Esc(handParent) + "\t" + hIsP + "\t" + handActive + "\t" + hCount + "\t" + i + "\t" +
                                uid + "\t" + cardIsP + "\t" + cardParent + "\t" + goName + "\t" + aSelf + "\t" + aHier + "\t" +
                                px + "\t" + py + "\t" + pz + "\t" + sid + "\t" + nm + "\t" + mana + "\t" + ct + "\t" + ctn + "\t" +
                                kw + "\t" + kwn + "\t" + mh + "\t" + arm + "\t" + ma + "\t" + ra);
                        }
                    }
                }
                LoggerInstance.Msg("[SJS] hand TSV -> " + outPath + " (" + (new FileInfo(outPath)).Length + " bytes, hands=" + nPlayerHands + ", cards=" + total + ")");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] hand extract fail: " + e); }
            return total;
        }

        // ---------- v3.8 探针 (2026-09-14): 强制展开 ChooseCardMenu, dump 运行时真实矩形 ----------
        //   用途: 原版「选牌 / 选效果」面板 (同一个组件) 的静态 RectTransform 数字**实测核对**。
        //   面板在有手牌的对局里才自然出现, 而手牌注入做不到 ⇒ 用探针 SetActive(true) 把它摆出来,
        //   量 GetWorldCorners + lossyScale + rect (这些是 Unity 运行时算出来的, 不是序列化值)。
        //   ⚠️ 面板里的**卡是运行时生成的**, 探针造不出来 ⇒ 拍照只有面板框/标题/按钮, 没有卡。
        private Transform FindInactiveByName(string name)
        {
            try
            {
                Transform[] all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && all[i].name == name) return all[i];
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] find '" + name + "' fail: " + e.Message); }
            return null;
        }

        private void DumpRectChain(Transform t, string path, StreamWriter w, int depth)
        {
            if (t == null || depth > 6) return;
            var go = t.gameObject;
            var sb = new StringBuilder();
            sb.Append(path).Append('\t').Append(Esc(go.name))
              .Append('\t').Append(go.activeSelf).Append('\t').Append(go.activeInHierarchy);
            var rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                var c = new Vector3[4];
                rt.GetWorldCorners(c);
                var ls = t.lossyScale;
                sb.Append("\tRT")
                  .Append('\t').Append(rt.anchoredPosition.x.ToString("F2")).Append(',').Append(rt.anchoredPosition.y.ToString("F2"))
                  .Append('\t').Append(rt.sizeDelta.x.ToString("F2")).Append(',').Append(rt.sizeDelta.y.ToString("F2"))
                  .Append('\t').Append(rt.rect.width.ToString("F2")).Append(',').Append(rt.rect.height.ToString("F2"))
                  .Append("\twc0=").Append(c[0].x.ToString("F2")).Append(',').Append(c[0].y.ToString("F2"))
                  .Append("\twc2=").Append(c[2].x.ToString("F2")).Append(',').Append(c[2].y.ToString("F2"))
                  .Append("\tlossyScale=").Append(ls.x.ToString("F4")).Append(',').Append(ls.y.ToString("F4"));
            }
            else sb.Append("\tTF\t\t\t\t\t");
            w.WriteLine(sb.ToString());
            for (int i = 0; i < t.childCount; i++)
                DumpRectChain(t.GetChild(i), path + "/" + t.GetChild(i).name, w, depth + 1);
        }

        private void OpenChoosePanelProbe(string outDir)
        {
            try
            {
                var cm = FindInactiveByName("ChooseCardMenu");
                if (cm == null) { LoggerInstance.Msg("[SJS] probe: ChooseCardMenu NOT FOUND"); return; }
                LoggerInstance.Msg("[SJS] probe: ChooseCardMenu path=" + Tr(cm, 8) + " activeSelf=" + cm.gameObject.activeSelf);
                var mull = FindInactiveByName("Mulligan");
                if (mull != null) { mull.gameObject.SetActive(false); LoggerInstance.Msg("[SJS] probe: Mulligan -> inactive"); }
                cm.gameObject.SetActive(true);
                var anchor = FindInactiveByName("ChooseCardMenuAnchor");
                LoggerInstance.Msg("[SJS] probe: ChooseCardMenuAnchor childCount=" + (anchor != null ? anchor.childCount : -1)
                                   + " (运行时生成的卡; 0 = 没有对局数据)");
                string path = Path.Combine(outDir, "runtime_rect_choose_panel.tsv");
                using (var w = new StreamWriter(path, false, Encoding.UTF8))
                    DumpRectChain(cm, "", w, 0);
                LoggerInstance.Msg("[SJS] probe: wrote " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] probe fail: " + e.Message); }
        }

        // ---------- 战斗 UI 截图准备 v3.6: overlay canvas → ScreenSpaceCamera + worldCamera ----------
        private Camera PrepareBattleUiForShot()
        {
            Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            Camera uiCam = null, best = null;
            float maxDepth = -999f;
            foreach (var c in cams)
            {
                try
                {
                    LoggerInstance.Msg("[SJS] cam: '" + c.name + "' en=" + c.enabled + " active=" + c.gameObject.activeInHierarchy + " depth=" + c.depth + " clear=" + c.clearFlags + " tag=" + c.tag);
                }
                catch { }
                if (c == null || !c.enabled) continue;
                if (uiCam == null && c.name.ToLowerInvariant().Contains("ui")) uiCam = c;
                if (c.depth > maxDepth) { maxDepth = c.depth; best = c; }
            }
            if (uiCam == null) uiCam = best;
            if (uiCam == null) uiCam = Camera.main;
            LoggerInstance.Msg("[SJS] ui cam chosen: " + (uiCam != null ? uiCam.name : "NONE"));
            Canvas[] cvs = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
            int nSw = 0;
            foreach (var cv in cvs)
            {
                try
                {
                    string wcName = "";
                    if (cv.renderMode == UnityEngine.RenderMode.ScreenSpaceCamera)
                    {
                        try { wcName = cv.worldCamera != null ? cv.worldCamera.name : "(null)"; } catch { wcName = "(?)"; }
                        LoggerInstance.Msg("[SJS] canvas: '" + cv.name + "' active=" + cv.gameObject.activeInHierarchy + " mode=SSCam wc=" + wcName);
                        // v3.8: 原来只在 worldCamera 为 null 时才改 —— 实测那样 UI **一张都拍不到**
                        //       (shot_battle_hand.png 全空)。改成**无条件**指到 uiCam, 让 uiCam.Render() 一定画得出 UI。
                        if (uiCam != null && cv.gameObject.activeInHierarchy) { cv.worldCamera = uiCam; nSw++; }
                    }
                    else if (cv.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay && cv.gameObject.activeInHierarchy)
                    {
                        cv.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
                        cv.worldCamera = uiCam;
                        nSw++;
                        LoggerInstance.Msg("[SJS] canvas '" + cv.name + "' -> ScreenSpaceCamera wc=" + (uiCam != null ? uiCam.name : "null"));
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] canvas switch err '" + cv.name + "': " + e.Message); }
            }
            LoggerInstance.Msg("[SJS] ui canvas switched: " + nSw);
            return uiCam;
        }

        private void CaptureShotCam(Camera cam, string path, int w = 1920, int h = 1080)
        {
            try
            {
                if (cam == null) { LoggerInstance.Msg("[SJS] shot fail: cam null"); return; }
                var rt = new RenderTexture(w, h, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                cam.targetTexture = prev;
                RenderTexture.active = prev;
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] shot fail: " + e); }
        }

        // v3.7: 双相机同 RT 全帧截图: 3D 主相机(clear=SolidColor)先渲 → UI 相机(clear=Nothing)叠 UI
        private void CaptureShotFull(Camera worldCam, Camera uiCam, string path, int w = 1920, int h = 1080)
        {
            try
            {
                if (uiCam == null) { LoggerInstance.Msg("[SJS] shot fail: uiCam null"); return; }
                var rt = new RenderTexture(w, h, 24);
                var pw = worldCam != null ? worldCam.targetTexture : null;
                var pu = uiCam.targetTexture;
                if (worldCam != null && worldCam != uiCam)
                {
                    worldCam.targetTexture = rt;
                    worldCam.Render();
                }
                uiCam.targetTexture = rt;
                uiCam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                if (worldCam != null && worldCam != uiCam) worldCam.targetTexture = pw;
                uiCam.targetTexture = pu;
                RenderTexture.active = pu;
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] shot fail: " + e); }
        }

        // ---------- BattleDrive v3.2 (2026-08-25): 用战斗场景自带 Debug 按钮给双方发牌/加能量, 抓实战手牌 ----------
        private Transform FindGo(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindGo(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private bool Click(Transform root, string goName, int reps = 1)
        {
            var go = FindGo(root, goName);
            if (go == null) { LoggerInstance.Msg("[SJS] drive: not found " + goName); return false; }
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) { LoggerInstance.Msg("[SJS] drive: no Button on " + goName); return false; }
            for (int i = 0; i < reps; i++) btn.onClick.Invoke();
            LoggerInstance.Msg("[SJS] drive: clicked " + goName + " x" + reps);
            return true;
        }

        // v3.6: 把每个 Debug 按钮的 onClick 持久化调用目标方法名打出来(查明 AddPlayerCardButton 到底调什么)
        private void DumpDebugButtons(Transform root)
        {
            try
            {
                UnityEngine.UI.Button[] btns = root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                LoggerInstance.Msg("[SJS] dbg buttons total: " + btns.Length);
                foreach (var b in btns)
                {
                    try
                    {
                        if (b == null || b.onClick == null) continue;
                        string calls = "";
                        int n = b.onClick.GetPersistentEventCount();
                        for (int i = 0; i < n; i++)
                        {
                            string m = b.onClick.GetPersistentMethodName(i);
                            object tg = null;
                            try { tg = b.onClick.GetPersistentTarget(i); } catch { }
                            string tn = "";
                            try { tn = tg != null ? tg.GetType().Name : ""; } catch { }
                            if (calls.Length > 0) calls += ";";
                            calls += tn + "." + m;
                        }
                        LoggerInstance.Msg("[SJS] dbg btn: '" + b.gameObject.name + "' active=" + b.gameObject.activeSelf + " calls=" + calls);
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] dbg btn err: " + e.Message); }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] dbg dump fail: " + e); }
        }

        // v3.5: 编译期引用游戏程序集 (MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll)
        private void InitBattleManager()
        {
            try
            {
                var bm = FindGo(SceneManager.GetActiveScene().GetRootGameObjects()[0].transform, "BattleManager");
                foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    bm = FindGo(ro.transform, "BattleManager");
                    if (bm != null) break;
                }
                if (bm == null) { LoggerInstance.Msg("[SJS] init: no Il2Cpp.BattleManager GO"); return; }
                var comp = bm.GetComponent<Il2Cpp.BattleManager>();
                if (comp == null) { LoggerInstance.Msg("[SJS] init: no Il2Cpp.BattleManager component"); return; }
                LoggerInstance.Msg("[SJS] init: Il2Cpp.BattleManager component OK");
                try { comp.Initialize(); LoggerInstance.Msg("[SJS] init: Initialize() ok"); }
                catch (Exception e) { LoggerInstance.Msg("[SJS] init Initialize err: " + e.Message); }
                // 私有方法走反射 (编译期类型下方法列表真实可见)
                foreach (var mname in new[] { "SetupTestButtons", "SetupInitialState", "StartBattleManager" })
                {
                    try
                    {
                        var mi = comp.GetType().GetMethod(mname,
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);
                        if (mi != null) { mi.Invoke(comp, null); LoggerInstance.Msg("[SJS] init: invoked " + mname); }
                        else LoggerInstance.Msg("[SJS] init: no method " + mname);
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] init " + mname + " err: " + e.Message); }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] init fail: " + e); }
        }

        private IEnumerator BattleDrive()
        {
            Transform bp = null;
            foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                bp = FindGo(ro.transform, "DebugButtons");
                if (bp != null) break;
            }
            if (bp == null)
            {
                LoggerInstance.Msg("[SJS] drive FAIL: no DebugButtons in any root");
                yield break;
            }
            InitBattleManager();
            yield return new WaitForSecondsRealtime(1f);
            bp.gameObject.SetActive(true);
            LoggerInstance.Msg("[SJS] drive: DebugButtons activated");
            yield return new WaitForSecondsRealtime(1f);
            DumpDebugButtons(bp);

            // v3.8: 调试按钮在正式版=死调用(场景 JSON 引用 BattleDebugController.ShowPlayerCardPicker,
            // 但元数据里该方法不存在(UNITY_EDITOR 编译裁掉), onClick.Invoke 无任何效果)
            // → 直达真实 API: grid.Show(空回调) 填充 Data → 造 CardScript 设 rawCardData → PlayerHand.AddCardNotDrawnToHand
            Click(bp, "AddMana", 3);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return InjectCardsIntoHand(bp);
            yield return new WaitForSecondsRealtime(2f);   // 等布局/飞行动画
            LoggerInstance.Msg("[SJS] drive sequence done");
        }

        private IEnumerator InjectCardsIntoHand(Transform bp)
        {
            // --- 1. 拿 DebugCardPickerGrid (在 Card Picker/OSA 节点) ---
            Il2Cpp.DebugCardPickerGrid grid = null;
            var gridGo = FindGo(bp, "Card Picker");
            if (gridGo != null)
            {
                var osaGo = FindGo(gridGo, "OSA");
                if (osaGo != null) { try { grid = osaGo.GetComponent<Il2Cpp.DebugCardPickerGrid>(); } catch (Exception e) { LoggerInstance.Msg("[SJS] inj grid osa err: " + e.Message); } }
                if (grid == null) { try { grid = gridGo.GetComponent<Il2Cpp.DebugCardPickerGrid>(); } catch (Exception e) { LoggerInstance.Msg("[SJS] inj grid picker err: " + e.Message); } }
            }
            if (grid == null)
            {
                try
                {
                    var gs = UnityEngine.Object.FindObjectsOfType<Il2Cpp.DebugCardPickerGrid>(true);
                    if (gs != null && gs.Length > 0) grid = gs[0];
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] inj grid find err: " + e.Message); }
            }
            if (grid == null) { LoggerInstance.Msg("[SJS] inj FAIL: no DebugCardPickerGrid"); yield break; }
            string gn = "?"; try { gn = grid.gameObject != null ? grid.gameObject.name : "?"; } catch { }
            LoggerInstance.Msg("[SJS] inj grid on GO: " + gn);
            // --- 2. 激活面板让 Start 跑, 然后 Show(空回调) 填充 Data ---
            bool needActivate = false;
            try { if (gridGo != null && !gridGo.gameObject.activeSelf) needActivate = true; } catch { }
            if (needActivate)
            {
                try { gridGo.gameObject.SetActive(true); } catch (Exception e) { LoggerInstance.Msg("[SJS] inj activate err: " + e.Message); }
                LoggerInstance.Msg("[SJS] inj panel activated");
                yield return new WaitForSecondsRealtime(0.5f);
            }
            try { grid.Show(null, null, null); LoggerInstance.Msg("[SJS] inj grid.Show() ok"); }
            catch (Exception e) { LoggerInstance.Msg("[SJS] inj grid.Show err: " + e.Message); }
            // v3.9: 优先走 CardsCollectionAddressables(场景内 AssetReference 列表)同步轮询加载 RawCardScript
            var raws = new System.Collections.Generic.List<Il2Cpp.RawCardScript>();
            Il2Cpp.CardsCollectionAddressables cca = null;
            try
            {
                var ccas = UnityEngine.Object.FindObjectsOfType<Il2Cpp.CardsCollectionAddressables>(true);
                LoggerInstance.Msg("[SJS] inj CardsCollectionAddressables found: " + (ccas == null ? 0 : ccas.Length));
                if (ccas != null && ccas.Length > 0)
                {
                    cca = ccas[0];
                    string ccaName = "?"; try { ccaName = cca.gameObject.name; } catch { }
                    LoggerInstance.Msg("[SJS] inj cca on GO '" + ccaName + "'");
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] inj cca scan err: " + e.Message); }
            if (cca != null) yield return LoadCcaCards(cca, raws);
            // grid.Data 长轮询兜底
            yield return new WaitForSecondsRealtime(1f);
            int n = -1;
            try { n = grid.Data != null ? grid.Data.Count : -1; } catch (Exception e) { LoggerInstance.Msg("[SJS] inj Data err: " + e.Message); }
            LoggerInstance.Msg("[SJS] inj grid Data.Count(early)=" + n);
            for (int t = 0; t < 6 && raws.Count < 1 && (n <= 0 || n < 0); t++)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                int nn = -1;
                try { nn = grid.Data != null ? grid.Data.Count : -1; } catch { }
                LoggerInstance.Msg("[SJS] inj grid Data.Count(t" + t + ")=" + nn);
                n = nn;
            }
            if (raws.Count < 1 && n > 0)
            {
                int use = Math.Min(n, 8);
                for (int i = 0; i < use; i++)
                {
                    try { var r = grid.Data[i]; if (r != null) raws.Add(r); }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] inj grid Data[" + i + "] err: " + e.Message); }
                }
                LoggerInstance.Msg("[SJS] inj took " + (raws.Count) + " raws from grid.Data");
            }
            LoggerInstance.Msg("[SJS] inj raws total: " + raws.Count);
            if (raws.Count == 0)
            {
                yield return ProbeAddr(raws);
            }
            if (raws.Count == 0)
            {
                // v3.10: bundle 枚举兜底: 已加载的全 bundle → GetAllAssetNames → 名含 card 的 .asset → LoadAsset<Object> → cast RawCardScript
                try
                {
                    int totalNames = 0;
                    foreach (var b in _bundles)
                    {
                        if (b == null) continue;
                        string[] names = null;
                        try { names = b.GetAllAssetNames(); } catch { continue; }
                        if (names == null) continue;
                        foreach (var nm in names)
                        {
                            totalNames++;
                            string ln = nm.ToLowerInvariant();
                            if (raws.Count >= 8) break;
                            if (!(ln.Contains("rawcard") || (ln.Contains("card") && ln.EndsWith(".asset")))) continue;
                            try
                            {
                                var obj = b.LoadAsset<UnityEngine.Object>(nm);
                                if (obj != null)
                                {
                                    var raw = obj as Il2Cpp.RawCardScript;
                                    if (raw != null)
                                    {
                                        raws.Add(raw);
                                        string rn = "?"; try { rn = raw.cardName; } catch { }
                                        LoggerInstance.Msg("[SJS] inj bundle raw '" + nm + "'='" + rn + "'");
                                    }
                                    else LoggerInstance.Msg("[SJS] inj bundle obj '" + nm + "' type=" + obj.GetType().Name + " (not RawCardScript)");
                                }
                            }
                            catch (Exception e) { LoggerInstance.Msg("[SJS] inj bundle load '" + nm + "' err: " + e.Message); }
                        }
                        if (raws.Count >= 8) break;
                    }
                    LoggerInstance.Msg("[SJS] inj bundle scan: totalNames=" + totalNames + " raws=" + raws.Count);
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] inj bundle scan err: " + e.Message); }
            }
            if (raws.Count == 0) { LoggerInstance.Msg("[SJS] inj FAIL: empty Data"); yield break; }
            // --- 3. PlayerHand(玩家/敌方) + BattleManager ---
            Il2Cpp.PlayerHand ph = null, eh = null;
            try
            {
                var hs = UnityEngine.Object.FindObjectsOfType<Il2Cpp.PlayerHand>(true);
                foreach (var h in hs) { if (h.isPlayer) ph = h; else eh = h; }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] inj handscan err: " + e.Message); }
            int phc = -1;
            try { phc = ph != null && ph.currentHand != null ? ph.currentHand.Count : -1; } catch { }
            LoggerInstance.Msg("[SJS] inj hands: player=" + (ph != null) + "(count=" + phc + ") enemy=" + (eh != null));
            Il2Cpp.BattleManager bm = null;
            foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                try
                {
                    var t = FindGo(ro.transform, "BattleManager");
                    if (t != null) bm = t.GetComponent<Il2Cpp.BattleManager>();
                }
                catch { }
                if (bm != null) break;
            }
            LoggerInstance.Msg("[SJS] inj BattleManager=" + (bm != null));
            // --- 4. 造卡注入: 玩家 4 张 / 敌方 2 张 ---
            int done = 0;
            for (int i = 0; i < 4; i++) TryInjectCard(raws, ph, bm, i, true, ref done);
            yield return new WaitForSecondsRealtime(1f);
            for (int i = 0; i < 2; i++) TryInjectCard(raws, eh, bm, i + 40, false, ref done);
            LoggerInstance.Msg("[SJS] inj done, cards injected=" + done);
        }

        private IEnumerator LoadCcaCards(Il2Cpp.CardsCollectionAddressables cca, System.Collections.Generic.List<Il2Cpp.RawCardScript> raws)
        {
            int libCount = -1;
            try { libCount = cca.cardLibrary != null ? cca.cardLibrary.Count : -1; } catch (Exception e) { LoggerInstance.Msg("[SJS] inj cca cardLibrary err: " + e.Message); }
            LoggerInstance.Msg("[SJS] inj cca cardLibrary.Count=" + libCount);
            for (int i = 0; i < libCount && raws.Count < 6; i++)
            {
                var aref = cca.cardLibrary[i];
                string err = null;
                try { aref.LoadAssetAsync<UnityEngine.Object>(); }
                catch (Exception e) { err = e.Message; }
                if (err != null) { LoggerInstance.Msg("[SJS] inj cca raw[" + i + "] load err: " + err); break; }
                int w = 0;
                bool stepDone = false;
                while (w++ < 60)
                {
                    try { stepDone = aref.IsDone; }
                    catch (Exception e) { err = e.Message; break; }
                    if (stepDone) break;
                    yield return new WaitForSecondsRealtime(0.1f);
                }
                if (err != null) { LoggerInstance.Msg("[SJS] inj cca raw[" + i + "] poll err: " + err); break; }
                if (!stepDone) { LoggerInstance.Msg("[SJS] inj cca raw[" + i + "] timeout"); continue; }
                try
                {
                    object resObj = aref.Asset;
                    var raw = (Il2Cpp.RawCardScript)(object)resObj;
                    if (raw != null) { raws.Add(raw); }
                    string rn = "?"; try { rn = raw.cardName; } catch { }
                    LoggerInstance.Msg("[SJS] inj cca raw[" + i + "]='" + rn + "'");
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] inj cca raw[" + i + "] result err: " + e.Message); }
            }
        }

        private IEnumerator ProbeAddr(System.Collections.Generic.List<Il2Cpp.RawCardScript> raws)
        {
            string[] probeKeys = new string[] { "cards", "RawCardScript", "rawcards", "000219bc3c835a74cb0d0672e03bf1a0", "0003a1819f6828d41b902266e47a287e", "000a2a390a9c40c4989a4c88c802c1fe" };
            foreach (var k in probeKeys)
            {
                if (raws.Count >= 2) break;
                string err = null;
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle opL = default;
                try { opL = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(k); }
                catch (Exception e) { err = e.Message; }
                if (err != null) { LoggerInstance.Msg("[SJS] inj addr key='" + k + "' err: " + err); continue; }
                int w = 0; bool ldone = false;
                while (w++ < 40)
                {
                    try { ldone = opL.IsDone; } catch (Exception e) { err = e.Message; break; }
                    if (ldone) break;
                    yield return new WaitForSecondsRealtime(0.1f);
                }
                if (err != null) { LoggerInstance.Msg("[SJS] inj addr key='" + k + "' poll err: " + err); continue; }
                if (!ldone) { LoggerInstance.Msg("[SJS] inj addr key='" + k + "' loc timeout"); continue; }
                int cnt = 0;
                try
                {
                    object locsObj = null;
                    try { locsObj = opL.Result; } catch { }
                    var lst = locsObj as System.Collections.IList;
                    cnt = lst != null ? lst.Count : -1;
                }
                catch { }
                LoggerInstance.Msg("[SJS] inj addr key='" + k + "' locations=" + cnt);
                if (cnt > 0)
                {
                    UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle opA;
                    try { opA = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(k); }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] inj addr load err: " + e.Message); continue; }
                    int wa = 0; bool adone = false;
                    while (wa++ < 50)
                    {
                        try { adone = opA.IsDone; } catch (Exception e) { err = e.Message; break; }
                        if (adone) break;
                        yield return new WaitForSecondsRealtime(0.1f);
                    }
                    if (err != null) { LoggerInstance.Msg("[SJS] inj addr load poll err: " + err); continue; }
                    if (!adone) { LoggerInstance.Msg("[SJS] inj addr load timeout"); continue; }
                    object resObj = null;
                    try { resObj = opA.Result; } catch { }
                    var raw = resObj as Il2Cpp.RawCardScript;
                    if (raw != null) raws.Add(raw);
                    string tn = "?"; try { tn = resObj != null ? resObj.GetType().Name : "null"; } catch { }
                    LoggerInstance.Msg("[SJS] inj addr key='" + k + "' resultType=" + tn + " isRaw=" + (raw != null));
                }
            }
        }

        private void TryInjectCard(System.Collections.Generic.List<Il2Cpp.RawCardScript> raws, Il2Cpp.PlayerHand hand, Il2Cpp.BattleManager bm, int uid, bool isPlayer, ref int done)
        {
            if (hand == null) { LoggerInstance.Msg("[SJS] inj skip: no hand (uid=" + uid + ")"); return; }
            if (raws == null || raws.Count == 0) { LoggerInstance.Msg("[SJS] inj skip: no raws"); return; }
            Il2Cpp.RawCardScript raw = raws[uid % raws.Count];
            string rawName = "?"; try { rawName = raw.cardName; } catch { }
            bool ok = false;
            // 方案A: CardSetup 完整初始化
            try
            {
                var go = new GameObject("SJS_Card_" + uid);
                var card = go.AddComponent<Il2Cpp.CardScript>();
                card.uniqueInGameID = uid;
                card.rawCardData = raw;
                card.isPlayer = isPlayer;
                try { card.CardSetup(bm, isPlayer, Il2Cpp.CardArmy.Neutral, false); }
                catch (Exception e) { LoggerInstance.Msg("[SJS] inj CardSetup err: " + e.Message); }
                var en = hand.AddCardNotDrawnToHand(card);
                if (en != null)
                {
                    int g = 0;
                    while (g++ < 100)
                    {
                        bool mv; try { mv = en.MoveNext(); } catch { break; }
                        if (!mv) break;
                    }
                }
                ok = true;
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] inj A fail: " + e.Message); }
            // 方案B: 最小设置兜底
            if (!ok)
            {
                try
                {
                    var go = new GameObject("SJS_CardB_" + uid);
                    var card = go.AddComponent<Il2Cpp.CardScript>();
                    card.uniqueInGameID = uid;
                    card.rawCardData = raw;
                    card.isPlayer = isPlayer;
                    var en = hand.AddCardNotDrawnToHand(card);
                    if (en != null)
                    {
                        int g = 0;
                        while (g++ < 100)
                        {
                            bool mv; try { mv = en.MoveNext(); } catch { break; }
                            if (!mv) break;
                        }
                    }
                    ok = true;
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] inj B fail: " + e.Message); }
            }
            if (ok)
            {
                done++;
                int hc = -1;
                try { hc = hand.currentHand != null ? hand.currentHand.Count : -1; } catch { }
                LoggerInstance.Msg("[SJS] inj card uid=" + uid + " name='" + rawName + "' isPlayer=" + isPlayer + " handCount=" + hc);
            }
        }

        // ================= v4.0 (2026-09-14 下午): 「选牌 / 选效果」面板展开实况 + URP 截屏 =================
        // 背景 (上一轮实测 + README:48): 原版所有 Canvas 都是 ScreenSpaceCamera, worldCamera = UI Camera;
        //   UI Camera 是 URP 相机栈里的 overlay 相机 ⇒ ① 单渲 uiCam 到 RT 出黑 ② 单渲主相机也不带相机栈 ⇒ 拍不到 UI。
        // 本版三条互不依赖的抓屏路, 各存一个文件, 谁成谁败看日志:
        //   A = WaitForEndOfFrame 读 backbuffer (最终合成画面, 含 UI)
        //   B = 临时把 canvas.worldCamera 指到主相机, 再 cam.Render() 到 RT
        //   C = RenderPipelineManager.endContextRendering 回调里 Graphics.Blit 抓最终画面
        // 另: 用 Addressables 载入 EnviromentalEffectCardsSO (catalog.bin 里查到这个地址键) 拿真实候选卡,
        //   调 ChooseCardMenu.Setup(...) 把「选效果」面板填起来 —— ⚠️ 这是**催出来**的, 不是原版自然流程。
        private static bool _eofDone;
        private RenderTexture _blitRt;
        private bool _blitArmed;
        private string _blitPath;

        private IEnumerator EOFWorker(string path)
        {
            _eofDone = false;
            yield return new WaitForEndOfFrame();
            try
            {
                int w = Screen.width, h = Screen.height;
                if (w <= 0 || h <= 0) { LoggerInstance.Msg("[SJS] EOF: bad screen " + w + "x" + h); _eofDone = true; yield break; }
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                LoggerInstance.Msg("[SJS] SHOT-EOF ok " + path + " " + w + "x" + h);
                // 同一时刻的另一条路: 把当前激活 RT(backbuffer) Blit 进 RT 再读 (若上面 ReadPixels 出黑而这条有内容 ⇒ 是读 backbuffer 的问题)
                try
                {
                    var rt = new RenderTexture(w, h, 24);
                    Graphics.Blit((Texture)null, rt);
                    RenderTexture.active = rt;
                    var t2 = new Texture2D(w, h, TextureFormat.RGB24, false);
                    t2.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    t2.Apply();
                    string p2 = path.Substring(0, path.Length - 4) + "_blit.png";
                    File.WriteAllBytes(p2, t2.EncodeToPNG());
                    UnityEngine.Object.Destroy(t2);
                    UnityEngine.Object.Destroy(rt);
                    RenderTexture.active = null;
                    LoggerInstance.Msg("[SJS] SHOT-EOF-BLIT ok " + p2);
                }
                catch (Exception e2) { LoggerInstance.Msg("[SJS] SHOT-EOF-BLIT fail: " + e2.Message); }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-EOF fail: " + e); }
            _eofDone = true;
        }

        // 起 worker 协程拍照, 主序列最多等 timeout 秒 (防 WaitForEndOfFrame 卡死整条链)
        private IEnumerator ShotEOF(string path, float timeout = 8f)
        {
            _eofDone = false;
            MelonCoroutines.Start(EOFWorker(path));
            float t = 0f;
            while (!_eofDone && t < timeout) { yield return new WaitForSecondsRealtime(0.25f); t += 0.25f; }
            if (!_eofDone) LoggerInstance.Msg("[SJS] SHOT-EOF TIMEOUT (" + timeout + "s, 没有帧循环?): " + path);
        }

        private void ShotCanvasRepoint(Camera cam, string path, int w = 1920, int h = 1080)
        {
            var cvs = new System.Collections.Generic.List<Canvas>();
            var oldMode = new System.Collections.Generic.List<UnityEngine.RenderMode>();
            var oldCam = new System.Collections.Generic.List<Camera>();
            try
            {
                foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                {
                    if (cv == null || !cv.gameObject.activeInHierarchy) continue;
                    cvs.Add(cv); oldMode.Add(cv.renderMode); oldCam.Add(cv.worldCamera);
                    cv.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
                    cv.worldCamera = cam;
                }
                LoggerInstance.Msg("[SJS] SHOT-REPOINT: canvases=" + cvs.Count + " -> cam '" + (cam != null ? cam.name : "NULL") + "'");
                if (cam == null) return;
                var rt = new RenderTexture(w, h, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                cam.targetTexture = prev;
                RenderTexture.active = prev;
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT-REPOINT ok " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-REPOINT fail: " + e); }
            finally
            {
                for (int i = 0; i < cvs.Count; i++)
                {
                    try { cvs[i].renderMode = oldMode[i]; cvs[i].worldCamera = oldCam[i]; } catch { }
                }
            }
        }

        // ⚠️ 2026-09-14 实测: RenderPipelineManager 的 endFrameRendering/endContextRendering 在这套 Cpp2IL interop 里
        //    是 Il2Cpp 的委托类, C# 的 `+=` 编不过 (CS0019)。所以最终合成画面走 EOFWorker 里的两条路
        //    (ReadPixels 直接读 backbuffer + Graphics.Blit(null, rt)), 不再订阅渲染事件。

        // ---------- 环境事实 (判「为什么之前的截图拍不到 UI」用) ----------
        /// <summary>v4.1（2026-09-22）：量 `Barrels Light FX` / `Generator 2 FX` 这两块「烘焙光斑」的**运行时实况**。
        /// 起因：复刻里这两块比原版亮 **+30**（消融过：关掉它们，那块 56.1 vs 原版 56.9），
        /// 而**静态数据逐项都对得上**（材质属性 / shader 是同一份 / 贴图 BC7 / 几何与绕向与 UV 逐位相同 /
        /// `_Surface=1 src=5 dst=10 _ZWrite=0 q=3002`）⇒ 只剩「**原版运行时到底画不画、用多大 alpha 画**」这一层。
        /// 取两轮（间隔 1.5 s）：那两块上挂着 `MaterialFlickerEffect`（`m_amplitude=0.834`），alpha 是**随时间变**的。</summary>
        private void ProbeArenaFx(string outDir, string tag)
        {
            try
            {
                string[] want = { "Barrels Light FX", "Generator 2 FX" };
                var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                using (var w = new StreamWriter(Path.Combine(outDir, "probe_fx_runtime_" + tag + ".tsv"), false, Encoding.UTF8))
                {
                    w.WriteLine("obj\tgoActiveSelf\tgoActiveInHier\tactiveParent\tmrEnabled\tisVisible\tshader\tmatName\tmatIsShared\trq"
                              + "\tcolor\tBaseColor\tSurface\tCull\tZWrite\tAlphaClip\tworldPos\tlossyScale\tboundsC\tboundsS\tcomps");
                    foreach (var t in all)
                    {
                        if (t == null) continue;
                        bool hit = false;
                        for (int i = 0; i < want.Length; i++) if (t.name == want[i]) hit = true;
                        if (!hit) continue;

                        string goSelf = "?", goHier = "?", activeParent = "-", mrEn = "-", vis = "-";
                        string shader = "-", matName = "-", matIsShared = "-", rq = "-";
                        string color = "-", baseCol = "-", surface = "-", cull = "-", zw = "-", clip = "-";
                        string pos = "-", scale = "-", bC = "-", bS = "-", comps = "";
                        try { goSelf = t.gameObject.activeSelf.ToString(); } catch { }
                        try { goHier = t.gameObject.activeInHierarchy.ToString(); } catch { }
                        try { if (t.parent != null) activeParent = t.parent.name + ":" + t.parent.gameObject.activeInHierarchy; } catch { }
                        try { pos = Fmt3(t.position); scale = Fmt3(t.lossyScale); } catch { }

                        try
                        {
                            var mr = t.GetComponent<MeshRenderer>();
                            if (mr != null)
                            {
                                mrEn = mr.enabled.ToString();
                                try { vis = mr.isVisible.ToString(); } catch { }
                                bC = Fmt3(mr.bounds.center); bS = Fmt3(mr.bounds.size);
                                var sm = mr.sharedMaterial;
                                // ⚠️ 读 `.material`（不是 `sharedMaterial`）—— 闪烁脚本写的是**实例**那一份，
                                //    而它已经在 `Awake` 里实例化过了，所以这一步不会再产生新副本。
                                var mat = mr.material;
                                var useMat = mat != null ? mat : sm;
                                if (sm != null && mat != null) matIsShared = (sm.GetInstanceID() == mat.GetInstanceID()).ToString();
                                if (useMat != null)
                                {
                                    matName = useMat.name;
                                    try { shader = useMat.shader != null ? useMat.shader.name : "(null shader)"; } catch { }
                                    rq = useMat.renderQueue.ToString();
                                    try { color = Fmt(useMat.color); } catch { }
                                    try { if (useMat.HasProperty("_BaseColor")) baseCol = Fmt(useMat.GetColor("_BaseColor")); } catch { }
                                    try { if (useMat.HasProperty("_Surface")) surface = useMat.GetFloat("_Surface").ToString(); } catch { }
                                    try { if (useMat.HasProperty("_Cull")) cull = useMat.GetFloat("_Cull").ToString(); } catch { }
                                    try { if (useMat.HasProperty("_ZWrite")) zw = useMat.GetFloat("_ZWrite").ToString(); } catch { }
                                    try { if (useMat.HasProperty("_AlphaClip")) clip = useMat.GetFloat("_AlphaClip").ToString(); } catch { }
                                }
                            }
                        }
                        catch (Exception e) { comps += "mrErr:" + e.GetType().Name + " "; }

                        try
                        {
                            var cs = t.GetComponents<Component>();
                            for (int i = 0; i < cs.Length; i++)
                            {
                                var c = cs[i];
                                if (c == null) { comps += "<null> "; continue; }
                                string tn = c.GetType().Name;
                                if (tn.IndexOf("Flicker", StringComparison.OrdinalIgnoreCase) >= 0
                                 || tn.IndexOf("Flick", StringComparison.OrdinalIgnoreCase) >= 0)
                                    comps += tn + "{" + FieldDump(c) + "} ";
                            }
                        }
                        catch { }

                        w.WriteLine(string.Join("\t", new string[] {
                            Esc(t.name), goSelf, goHier, Esc(activeParent), mrEn, vis,
                            Esc(shader), Esc(matName), matIsShared, rq,
                            color, baseCol, surface, cull, zw, clip,
                            pos, scale, bC, bS, Esc(comps) }));
                        w.Flush();
                        LoggerInstance.Msg("[SJS] FX " + tag + " '" + t.name + "' active=" + goHier + " mrEn=" + mrEn
                            + " vis=" + vis + " shader=" + shader + " color=" + color + " flicker=" + (comps.Length > 0 ? "有" : "无"));
                    }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fx probe err: " + e); }
        }

        private void LogEnvFacts()
        {
            try
            {
                LoggerInstance.Msg("[SJS] env: screen=" + Screen.width + "x" + Screen.height
                    + " fullscreen=" + Screen.fullScreenMode + " focused=" + Application.isFocused
                    + " runInBackground=" + Application.runInBackground + " targetFrameRate=" + Application.targetFrameRate);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env screen err: " + e.Message); }
            try
            {
                var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                LoggerInstance.Msg("[SJS] env: SRP=" + (rp != null ? rp.GetType().FullName + " '" + rp.name + "'" : "(builtin)"));
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env SRP err: " + e.Message); }

            Il2CppSystem.Type camDataType = null;
            try { camDataType = Il2CppSystem.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime"); }
            catch { }
            LoggerInstance.Msg("[SJS] env: UniversalAdditionalCameraData type resolved = " + (camDataType != null));
            try
            {
                foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
                {
                    if (c == null) continue;
                    string s = "'" + c.name + "' en=" + c.enabled + " active=" + c.gameObject.activeInHierarchy
                        + " depth=" + c.depth + " clear=" + c.clearFlags + " ortho=" + c.orthographic
                        + " near=" + c.nearClipPlane + " far=" + c.farClipPlane
                        + " orthoSize=" + c.orthographicSize
                        + " cullingMask=" + LayerMaskStr(c.cullingMask)
                        + " targetTex=" + (c.targetTexture != null ? c.targetTexture.name : "null")
                        + " pxRect=" + c.pixelRect.width + "x" + c.pixelRect.height
                        + " tag=" + c.tag + " stack=" + (c.cameraType.ToString());
                    if (camDataType != null)
                    {
                        try
                        {
                            var ad = c.GetComponent(camDataType);
                            if (ad != null)
                            {
                                var st = ad.GetType();
                                string rt = "?";
                                try { var f = st.GetField("renderType"); if (f != null) { var v = f.GetValue(ad); rt = v == null ? "null" : v.ToString(); } } catch { }
                                string csN = "?";
                                try
                                {
                                    var f = st.GetField("cameraStack");
                                    if (f != null)
                                    {
                                        var v = f.GetValue(ad);
                                        var lst = v as Il2CppSystem.Collections.Generic.List<Camera>;
                                        csN = lst != null ? lst.Count.ToString() : "notList";
                                        if (lst != null)
                                        {
                                            string names = "";
                                            for (int i = 0; i < lst.Count && i < 6; i++)
                                            { try { names += (i > 0 ? "," : "") + lst[i].name; } catch { } }
                                            csN += "[" + names + "]";
                                        }
                                    }
                                }
                                catch { }
                                s += " URP.renderType=" + rt + " URP.cameraStack=" + csN;
                            }
                            else s += " URP.additionalData=null";
                        }
                        catch (Exception e) { s += " URP.err=" + e.Message; }
                    }
                    LoggerInstance.Msg("[SJS] env cam: " + s);
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env cam err: " + e.Message); }

            try
            {
                foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                {
                    if (cv == null) continue;
                    string wc = "";
                    try { wc = cv.worldCamera != null ? cv.worldCamera.name : "null"; } catch { wc = "?"; }
                    string sc = "";
                    try { var sca = cv.GetComponent<UnityEngine.UI.CanvasScaler>(); if (sca != null) sc = " scaler=" + sca.uiScaleMode + "/" + sca.referenceResolution.x + "x" + sca.referenceResolution.y + "/match" + sca.matchWidthOrHeight; } catch { }
                    string cg = "";
                    try
                    {
                        var g = cv.GetComponent<CanvasGroup>();
                        cg = g != null ? (" canvasGroup(alpha=" + g.alpha + ",interactable=" + g.interactable + ",blocks=" + g.blocksRaycasts + ")")
                                       : " canvasGroup=none";
                    }
                    catch { }
                    LoggerInstance.Msg("[SJS] env canvas: '" + cv.name + "' active=" + cv.gameObject.activeInHierarchy
                        + " mode=" + cv.renderMode + " wc=" + wc + " plane=" + cv.planeDistance
                        + " sortOrder=" + cv.sortingOrder + " override=" + cv.overrideSorting + " scaleFactor=" + cv.scaleFactor
                        + " targetDisplay=" + cv.targetDisplay + " layer=" + cv.gameObject.layer + cg + sc);
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env canvas err: " + e.Message); }
        }

        // ---------- 2026-09-20 新增：运行时 RenderSettings + 灯光 + 环境条件 实况 dump ----------
        // 用途：核对「战场环境光」到底用哪个值。静态证据（反编译）显示
        // `ScenarioEnvironmentConditionsManager.Awake()` 会用 `defaultEnvironment` 的
        // `ambientColor` 覆盖 `RenderSettings.ambientLight`，但场景里的 `m_AmbientSkyColor`
        // 与它并不相等 ⇒ 只有实况能定。结果仅供本地核对，不影响 mod 原有行为。
        private void LogRenderEnvFacts()
        {
            try
            {
                var sc = SceneManager.GetActiveScene();
                LoggerInstance.Msg("[SJS] RSSCN scene='" + sc.name + "' path=" + sc.path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN err: " + e.Message); }

            try
            {
                LoggerInstance.Msg("[SJS] RSSCN ambientMode=" + RenderSettings.ambientMode
                    + " ambientIntensity=" + RenderSettings.ambientIntensity
                    + " ambientLight=" + Fmt(RenderSettings.ambientLight)
                    + " sky=" + Fmt(RenderSettings.ambientSkyColor)
                    + " equator=" + Fmt(RenderSettings.ambientEquatorColor)
                    + " ground=" + Fmt(RenderSettings.ambientGroundColor));
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN ambient err: " + e.Message); }

            try
            {
                var sh = RenderSettings.ambientProbe;
                LoggerInstance.Msg("[SJS] RSSCN probe DC=(" + sh[0, 0] + "," + sh[1, 0] + "," + sh[2, 0]
                    + ") L1=(" + sh[0, 1] + "," + sh[0, 2] + "," + sh[0, 3] + ")");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN probe err: " + e.Message); }

            try
            {
                LoggerInstance.Msg("[SJS] RSSCN fog=" + RenderSettings.fog
                    + " fogMode=" + RenderSettings.fogMode
                    + " fogColor=" + Fmt(RenderSettings.fogColor)
                    + " fogDensity=" + RenderSettings.fogDensity
                    + " reflectionIntensity=" + RenderSettings.reflectionIntensity
                    + " defaultReflectionMode=" + RenderSettings.defaultReflectionMode);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN fog err: " + e.Message); }

            try
            {
                foreach (var l in UnityEngine.Object.FindObjectsOfType<Light>(true))
                {
                    if (l == null) continue;
                    string path = l.name;
                    try
                    {
                        var t = l.transform; string p = t.name;
                        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
                        path = p;
                    }
                    catch { }
                    LoggerInstance.Msg("[SJS] RSSCN light '" + path
                        + "' type=" + l.type + " en=" + l.enabled + " active=" + l.gameObject.activeInHierarchy
                        + " color=" + Fmt(l.color) + " intensity=" + l.intensity
                        + " shadows=" + l.shadows + " shadowStrength=" + l.shadowStrength
                        + " pos=" + Fmt3(l.transform.position) + " rot=" + Fmt4(l.transform.rotation)
                        + " range=" + l.range);
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN light err: " + e.Message); }

            // 环境条件管理器（原版 `ScenarioEnvironmentConditionsManager`）
            try
            {
                Il2CppSystem.Type mt = null;
                foreach (var cand in new string[] {
                    "ScenarioEnvironmentConditionsManager, Assembly-CSharp",
                    "ScenarioEnvironmentConditionsManager" })
                {
                    try { mt = Il2CppSystem.Type.GetType(cand); } catch { mt = null; }
                    if (mt != null) break;
                }
                LoggerInstance.Msg("[SJS] RSSCN envmgr type resolved = " + (mt != null));
                if (mt != null)
                {
                    var arr = UnityEngine.Object.FindObjectsOfType(mt, true);
                    LoggerInstance.Msg("[SJS] RSSCN envmgr count = " + (arr == null ? -1 : arr.Length));
                    if (arr != null)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var o = arr[i];
                            if (o == null) continue;
                            var st = o.GetType();
                            string cur = "?", def = "?", act = "?";
                            try
                            {
                                var pgo = st.GetProperty("gameObject");
                                var gv = pgo == null ? null : pgo.GetValue(o);
                                if (gv != null)
                                {
                                    var gt = gv.GetType();
                                    var pa = gt.GetProperty("activeInHierarchy");
                                    act = (pa == null ? "noprop" : pa.GetValue(gv).ToString());
                                }
                            }
                            catch (Exception e) { act = "err:" + e.Message; }
                            try { var p = st.GetProperty("CurrentEnvironment"); var v = p == null ? null : p.GetValue(o); cur = SoStr(v); } catch (Exception e) { cur = "err:" + e.Message; }
                            try { var f = st.GetField("defaultEnvironment"); var v = f == null ? null : f.GetValue(o); def = SoStr(v); } catch (Exception e) { def = "err:" + e.Message; }
                            LoggerInstance.Msg("[SJS] RSSCN envmgr[" + i + "] go='" + o.name + "' active=" + act
                                + " current=" + cur + " default=" + def);
                        }
                    }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] RSSCN envmgr err: " + e.Message); }
        }

        private static string SoStr(object so)
        {
            if (so == null) return "null";
            try
            {
                var uo = so as UnityEngine.Object;
                if (uo == null) return "notUnityObject:" + so.GetType().Name;
                var st = so.GetType();
                string s = "'" + uo.name + "'";
                foreach (var fn in new string[] { "blendTime", "ambientColor", "ambientBlend", "fogColor", "fogDensity", "defaultScenarioObjectsState" })
                {
                    try
                    {
                        var f = st.GetField(fn);
                        if (f == null) { s += " " + fn + "=<nofield>"; continue; }
                        var v = f.GetValue(so);
                        s += " " + fn + "=" + (v == null ? "null" : v.ToString());
                    }
                    catch (Exception e) { s += " " + fn + "=err(" + e.Message + ")"; }
                }
                return s;
            }
            catch (Exception e) { return "soErr:" + e.Message; }
        }

        private static string Fmt(UnityEngine.Color c)
        { return "(" + c.r.ToString("F5") + "," + c.g.ToString("F5") + "," + c.b.ToString("F5") + "," + c.a.ToString("F5") + ")"; }
        private static string Fmt3(UnityEngine.Vector3 v)
        { return "(" + v.x.ToString("F5") + "," + v.y.ToString("F5") + "," + v.z.ToString("F5") + ")"; }
        private static string Fmt4(UnityEngine.Quaternion q)
        { return "(" + q.x.ToString("F5") + "," + q.y.ToString("F5") + "," + q.z.ToString("F5") + "," + q.w.ToString("F5") + ")"; }

        // ---------- 面板节点树 dump (带 sprite / 文字 / 颜色 / activeSelf) ----------
        private void DumpPanelNode(Transform t, string path, StreamWriter w, int depth)
        {
            if (t == null) return;
            var go = t.gameObject;
            var sb = new StringBuilder();
            sb.Append(path).Append('\t').Append(Esc(go.name))
              .Append('\t').Append(go.activeSelf).Append('\t').Append(go.activeInHierarchy);
            try
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var c = new Vector3[4];
                    rt.GetWorldCorners(c);
                    sb.Append("\tRT")
                      .Append('\t').Append(rt.sizeDelta.x.ToString("F2")).Append(',').Append(rt.sizeDelta.y.ToString("F2"))
                      .Append('\t').Append(rt.rect.width.ToString("F2")).Append(',').Append(rt.rect.height.ToString("F2"))
                      .Append('\t').Append(rt.anchoredPosition.x.ToString("F2")).Append(',').Append(rt.anchoredPosition.y.ToString("F2"))
                      .Append('\t').Append(rt.anchorMin.x.ToString("F3")).Append(',').Append(rt.anchorMin.y.ToString("F3"))
                      .Append('\t').Append(rt.anchorMax.x.ToString("F3")).Append(',').Append(rt.anchorMax.y.ToString("F3"))
                      .Append('\t').Append(c[0].x.ToString("F1")).Append(',').Append(c[0].y.ToString("F1"))
                      .Append("..").Append(c[2].x.ToString("F1")).Append(',').Append(c[2].y.ToString("F1"))
                      .Append("\tls=").Append(t.lossyScale.x.ToString("F4"));
                }
                else sb.Append("\tTF\t\t\t\t\t\t\t\t");
            }
            catch { sb.Append("\tRT?\t\t\t\t\t\t\t\t"); }

            // 文字
            string txt = ""; string fs = ""; string tcol = "";
            try
            {
                var tmp = go.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    try { txt = tmp.text; } catch { }
                    try { fs = tmp.fontSize.ToString("F1"); } catch { }
                    try { var cc = tmp.color; tcol = cc.r.ToString("F2") + "," + cc.g.ToString("F2") + "," + cc.b.ToString("F2") + "," + cc.a.ToString("F2"); } catch { }
                }
            }
            catch (Exception e) { txt = "<TMPerr " + e.GetType().Name + ">"; }
            sb.Append("\tTXT\t").Append(Esc(txt)).Append('\t').Append(fs).Append('\t').Append(tcol);

            // 图
            string sn = ""; string icol = ""; string ien = "";
            try
            {
                var img = go.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    try { sn = img.sprite != null ? img.sprite.name : "(null)"; } catch { }
                    try { var cc = img.color; icol = cc.r.ToString("F2") + "," + cc.g.ToString("F2") + "," + cc.b.ToString("F2") + "," + cc.a.ToString("F2"); } catch { }
                    try { ien = img.enabled.ToString(); } catch { }
                    try { if (img.sprite != null && img.sprite.texture != null) sn += "(tex:" + img.sprite.texture.name + ")"; } catch { }
                }
            }
            catch { }
            sb.Append("\tIMG\t").Append(Esc(sn)).Append('\t').Append(icol).Append('\t').Append(ien);

            // 组件名清单 (帮判这块是什么)
            string comps = "";
            try
            {
                var cs = go.GetComponents<UnityEngine.Component>();
                if (cs != null)
                    for (int i = 0; i < cs.Length; i++)
                    {
                        if (cs[i] == null) continue;
                        string nm = ""; try { nm = cs[i].GetType().Name; } catch { }
                        if (nm == "Transform" || nm == "RectTransform") continue;
                        comps += (comps.Length > 0 ? "," : "") + nm;
                    }
            }
            catch { }
            sb.Append('\t').Append(Esc(comps));

            w.WriteLine(sb.ToString());
            if (depth >= 8) return;
            for (int i = 0; i < t.childCount; i++)
                DumpPanelNode(t.GetChild(i), path + "/" + t.GetChild(i).name, w, depth + 1);
        }

        private void DumpPanelTree(Transform t, string outPath, string tag)
        {
            try
            {
                using (var w = new StreamWriter(outPath, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("# " + tag + "  (activeSelf/activeInHierarchy 是运行时真值; sd=sizeDelta rect=实际矩形 ap=anchoredPosition wc=世界角)");
                    w.WriteLine("path\tname\tactiveSelf\tactiveInHierarchy\tsd\trect\tap\tanchorMin\tanchorMax\tworldCorners\tlossyScale\ttext\tfontSize\ttextColor\timage\tspriteColor\timgEnabled\tcomponents");
                    DumpPanelNode(t, "", w, 0);
                }
                LoggerInstance.Msg("[SJS] panel tree -> " + outPath + " (" + (new FileInfo(outPath)).Length + " bytes)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] panel tree fail: " + e); }
        }

        // 逐组件把字段值也 dump 出来 (ChooseCardUI / SelectCardButtonFrame 这些静态查不到字段含义)
        private string SimpleVal(object o)
        {
            if (o == null) return "null";
            string n = "?"; try { n = o.GetType().Name; } catch { }
            try
            {
                if (n == "String") return "\"" + Esc(o.ToString()) + "\"";
                if (n == "Boolean" || n == "Int32" || n == "Single" || n == "Double" || n == "Int64" || n == "Byte" || n == "Int16" || n == "UInt32")
                    return o.ToString();
                if (n == "Color" || n == "Color32" || n == "Vector2" || n == "Vector3" || n == "Vector4" || n == "Quaternion" || n == "Rect")
                    return o.ToString();
            }
            catch { }
            try
            {
                var uo = o as UnityEngine.Object;
                if (uo != null) return n + "('" + uo.name + "')";
            }
            catch { }
            return n;
        }

        private string FieldDump(object comp)
        {
            var sb = new StringBuilder();
            try
            {
                var st = comp.GetType();
                var fs = st.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fs == null) return "";
                for (int i = 0; i < fs.Length; i++)
                {
                    var f = fs[i];
                    if (f == null) continue;
                    string v;
                    try { v = SimpleVal(f.GetValue(comp)); }
                    catch (Exception e) { v = "<err " + e.GetType().Name + ">"; }
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(f.Name).Append('=').Append(v);
                }
            }
            catch (Exception e) { return "<fielddump err " + e.Message + ">"; }
            return sb.ToString();
        }

        private void DumpComponentsNode(Transform t, string path, StreamWriter w, int depth, ref int lines)
        {
            if (t == null || lines > 3000) return;
            var go = t.gameObject;
            try
            {
                var cs = go.GetComponents<UnityEngine.Component>();
                if (cs != null)
                    for (int i = 0; i < cs.Length; i++)
                    {
                        if (cs[i] == null) continue;
                        string nm = ""; try { nm = cs[i].GetType().Name; } catch { }
                        if (nm == "Transform" || nm == "RectTransform") continue;
                        w.WriteLine(path + "\t" + nm + "\t" + Esc(FieldDump(cs[i])));
                        lines++;
                    }
            }
            catch { }
            if (depth >= 4) return;
            for (int i = 0; i < t.childCount; i++)
                DumpComponentsNode(t.GetChild(i), path + "/" + t.GetChild(i).name, w, depth + 1, ref lines);
        }

        private void DumpComponentsTree(Transform t, string outPath, string tag)
        {
            try
            {
                int lines = 0;
                using (var w = new StreamWriter(outPath, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("# " + tag + " — 每个组件一行: path / 组件类型 / 全部实例字段");
                    w.WriteLine("path\tcomponent\tfields");
                    DumpComponentsNode(t, "", w, 0, ref lines);
                }
                LoggerInstance.Msg("[SJS] components -> " + outPath + " (" + (new FileInfo(outPath)).Length + " bytes)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] components fail: " + e); }
        }

        // ---------- 用本地 Addressables 资源把「选效果」面板填起来 ----------
        private Transform _fillMenuT;
        private Il2Cpp.ChooseCardMenu _fillMenu;
        private int _fillN = -1;

        private void FillTryArmies(UnityEngine.Object so, string outDir)
        {
            var typed = so.TryCast<Il2Cpp.EnviromentalEffectCardsSO>();
            if (typed == null) { LoggerInstance.Msg("[SJS] fill: TryCast<EnviromentalEffectCardsSO> = null"); return; }
            Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> best = null;
            string bestTag = "";
            Il2Cpp.CardArmy[] armies = new Il2Cpp.CardArmy[] {
                Il2Cpp.CardArmy.Neutral, Il2Cpp.CardArmy.Ultramarines, Il2Cpp.CardArmy.Goff, Il2Cpp.CardArmy.SaimHann,
                Il2Cpp.CardArmy.Sautekh, Il2Cpp.CardArmy.BlackLegion, Il2Cpp.CardArmy.Leviathan, Il2Cpp.CardArmy.TauEmpire,
                Il2Cpp.CardArmy.Sororitas, Il2Cpp.CardArmy.Genestealers, Il2Cpp.CardArmy.AstraMilitarum,
                Il2Cpp.CardArmy.DarkAngels, Il2Cpp.CardArmy.EmperorsChildren, Il2Cpp.CardArmy.SpaceWolves };
            foreach (var a in armies)
            {
                foreach (bool off in new bool[] { true, false })
                {
                    try
                    {
                        var l = typed.GetEnvCardList(a, off, false, 0);
                        int c = l != null ? l.Count : -1;
                        if (c > 0)
                        {
                            LoggerInstance.Msg("[SJS] fill: GetEnvCardList(" + a + "," + off + ",false,0) = " + c + " cards");
                            if (best == null || c > best.Count) { best = l; bestTag = a + "/" + (off ? "offensive" : "defensive"); }
                        }
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] fill: GetEnvCardList(" + a + "," + off + ") err: " + e.Message); }
                }
                if (best != null) break;
            }
            if (best == null) { LoggerInstance.Msg("[SJS] fill: GetEnvCardList 全空 (SO 在, 但列表是空的)"); return; }
            _fillN = best.Count;
            _fillList = best;
            LoggerInstance.Msg("[SJS] fill: 用 " + bestTag + " 的 " + best.Count + " 张卡填面板");
            // 先把卡名列出来 (这就是「选效果」的候选项 —— 原版同一入口)
            for (int i = 0; i < best.Count && i < 20; i++)
            {
                string cn = "?", sid = "?";
                try { cn = best[i] != null ? best[i].cardName : "(null)"; } catch { }
                try { sid = best[i] != null ? best[i].cardShortId.ToString() : "?"; } catch { }
                LoggerInstance.Msg("[SJS] fill:   card[" + i + "] '" + cn + "' shortId=" + sid);
            }
            if (_fillMenu == null) { LoggerInstance.Msg("[SJS] fill: menu 组件不在, 只记录卡表"); return; }
            try
            {
                _fillMenu.Setup(best, "", 30, 0);
                LoggerInstance.Msg("[SJS] fill: ChooseCardMenu.Setup(list," + "\"\"" + ",30,0) ok");
            }
            catch (Exception e)
            {
                LoggerInstance.Msg("[SJS] fill: Setup() 抛了: " + e.Message + " —— 兜底直跑私有协程 SetupChooseCardsUi");
                try
                {
                    var mi = _fillMenu.GetType().GetMethod("SetupChooseCardsUi",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (mi != null)
                    {
                        var en = mi.Invoke(_fillMenu, new object[] { best, false });
                        if (en != null) MelonCoroutines.Start(WrapIl2CppEnum(en));
                        LoggerInstance.Msg("[SJS] fill: SetupChooseCardsUi invoked");
                    }
                    else LoggerInstance.Msg("[SJS] fill: 找不到 SetupChooseCardsUi");
                }
                catch (Exception e2) { LoggerInstance.Msg("[SJS] fill: SetupChooseCardsUi err: " + e2.Message); }
            }
        }

        private IEnumerator WrapIl2CppEnum(object en)
        {
            var it = en as Il2CppSystem.Collections.IEnumerator;
            if (it == null) { LoggerInstance.Msg("[SJS] fill: 返回的不是 IEnumerator"); yield break; }
            int guard = 0;
            while (guard++ < 600)
            {
                bool mv;
                try { mv = it.MoveNext(); } catch (Exception e) { LoggerInstance.Msg("[SJS] fill: MoveNext err " + e.Message); yield break; }
                if (!mv) break;
                yield return null;
            }
            LoggerInstance.Msg("[SJS] fill: 内部协程跑完 (steps=" + guard + ")");
        }

        // Addressables 调用包成非迭代器方法 (yield 不能待在带 catch 的 try 里)
        private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.Object> _loadOp;
        private bool _loadStarted;

        private void StartAssetLoad(string key)
        {
            _loadStarted = false;
            try
            {
                _loadOp = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(key);
                _loadStarted = true;
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: LoadAssetAsync('" + key + "') err: " + e.Message); }
        }

        private bool AssetLoadDone()
        {
            if (!_loadStarted) return true;
            try { return _loadOp.IsDone; } catch { return true; }
        }

        private UnityEngine.Object AssetLoadResult()
        {
            try
            {
                string stt = "?"; try { stt = _loadOp.Status.ToString(); } catch { }
                LoggerInstance.Msg("[SJS] fill: load status=" + stt);
                return _loadOp.Result;
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: .Result 抛: " + e.Message); return null; }
        }

        private Il2Cpp.BattleManager FindBattleManager()
        {
            try
            {
                foreach (var ro in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var t = FindGo(ro.transform, "BattleManager");
                    if (t != null)
                    {
                        var bm = t.GetComponent<Il2Cpp.BattleManager>();
                        if (bm != null) return bm;
                    }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: FindBattleManager err: " + e.Message); }
            return null;
        }

        private IEnumerator TryFillChoosePanel(string outDir)
        {
            var cm = FindInactiveByName("ChooseCardMenu");
            if (cm == null) { LoggerInstance.Msg("[SJS] fill: ChooseCardMenu NOT FOUND"); yield break; }
            _fillMenuT = cm;
            try { _fillMenu = cm.GetComponent<Il2Cpp.ChooseCardMenu>(); } catch (Exception e) { LoggerInstance.Msg("[SJS] fill: GetComponent<ChooseCardMenu> err " + e.Message); }
            LoggerInstance.Msg("[SJS] fill: menu 组件=" + (_fillMenu != null));
            try { if (!cm.gameObject.activeSelf) { cm.gameObject.SetActive(true); LoggerInstance.Msg("[SJS] fill: ChooseCardMenu 强制 SetActive(true) [探针动作, 非原版自然流程]"); } }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: SetActive err " + e.Message); }

            // 路 1: 反编译说 BattleManager 上有 Inspector 直连字段 (BattleManager.cs:4464 enviromentalEffectCards / :4452 chooseCardManager)
            UnityEngine.Object so = null;
            Il2Cpp.BattleManager bm = FindBattleManager();
            LoggerInstance.Msg("[SJS] fill: BattleManager = " + (bm != null));
            if (bm != null)
            {
                try { so = bm.enviromentalEffectCards; } catch (Exception e) { LoggerInstance.Msg("[SJS] fill: bm.enviromentalEffectCards 读失败: " + e.Message); }
                LoggerInstance.Msg("[SJS] fill: BM.enviromentalEffectCards = " + (so != null ? so.name + " (" + so.GetType().Name + ")" : "null"));
                try
                {
                    var cm2 = bm.chooseCardManager;
                    LoggerInstance.Msg("[SJS] fill: BM.chooseCardManager = " + (cm2 != null ? cm2.gameObject.name : "null"));
                    if (cm2 != null && _fillMenu == null) _fillMenu = cm2;
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] fill: bm.chooseCardManager 读失败: " + e.Message); }
            }

            // 路 2: Addressables 按地址键载 (catalog.bin 里查到 EnviromentalEffectCardsSO 这个键)
            if (so == null)
            {
                string key = "EnviromentalEffectCardsSO";
                StartAssetLoad(key);
                int w = 0;
                while (w++ < 200 && !AssetLoadDone()) yield return new WaitForSecondsRealtime(0.1f);
                LoggerInstance.Msg("[SJS] fill: LoadAssetAsync('" + key + "') 等了 " + (w / 10f).ToString("F1") + "s, started=" + _loadStarted);
                so = AssetLoadResult();
                LoggerInstance.Msg("[SJS] fill: Addressables SO = " + (so != null ? so.GetType().Name + " '" + so.name + "'" : "null"));
            }

            if (so == null) so = FindSoInBundles();
            if (so == null) { LoggerInstance.Msg("[SJS] fill: 没拿到 EnviromentalEffectCardsSO ⇒ 面板只能是空的 (候选卡原版走服务器/远程 bundle allcards)"); yield break; }
            FillTryArmies(so, outDir);
        }

        private UnityEngine.Object FindSoInBundles()
        {
            try
            {
                var soType = Il2CppSystem.Type.GetType("EnviromentalEffectCardsSO, Assembly-CSharp");
                LoggerInstance.Msg("[SJS] fill: SO type resolved = " + (soType != null));
                if (soType == null) return null;
                foreach (var b in _bundles)
                {
                    if (b == null) continue;
                    try
                    {
                        var arr = b.LoadAllAssets(soType);
                        if (arr != null && arr.Length > 0)
                        {
                            LoggerInstance.Msg("[SJS] fill: bundle '" + b.name + "' LoadAllAssets 命中 " + arr.Length + " 个");
                            return arr[0] as UnityEngine.Object;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: bundle SO 兜底 err: " + e.Message); }
            return null;
        }

        private void DumpBundleAssetNames(string outDir)
        {
            string path = Path.Combine(outDir, "bundle_asset_names.tsv");
            try
            {
                int n = 0;
                using (var w = new StreamWriter(path, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("bundle\tassetName");
                    foreach (var b in _bundles)
                    {
                        if (b == null) continue;
                        string bn = "?"; try { bn = b.name; } catch { }
                        string[] names = null;
                        try { names = b.GetAllAssetNames(); } catch { continue; }
                        if (names == null) continue;
                        foreach (var nm in names) { w.WriteLine(bn + "\t" + nm); n++; }
                    }
                }
                LoggerInstance.Msg("[SJS] bundle asset names -> " + path + " (" + n + " names)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] bundle names fail: " + e); }
        }

        private void LogPanelStates(string tag)
        {
            foreach (var nm in new string[] { "Mulligan", "ChooseCardMenu" })
            {
                var t = FindInactiveByName(nm);
                if (t == null) { LoggerInstance.Msg("[SJS] panel[" + tag + "]: " + nm + " NOT FOUND"); continue; }
                string anc = "";
                try
                {
                    var a = FindInactiveByName(nm == "Mulligan" ? "MulliganAnchor" : "ChooseCardMenuAnchor");
                    anc = a != null ? (" anchor.childCount=" + a.childCount) : " anchor=?";
                }
                catch { }
                LoggerInstance.Msg("[SJS] panel[" + tag + "]: " + nm + " activeSelf=" + t.gameObject.activeSelf
                    + " activeInHierarchy=" + t.gameObject.activeInHierarchy + anc);
            }
        }

        private IEnumerator PanelMode(string outDir)
        {
            Application.runInBackground = true;
            LogEnvFacts();
            LogPanelStates("t0_natural");
            yield return ShotEOF(Path.Combine(outDir, "cap1_endframe_t0_natural.png"));
            var mull = FindInactiveByName("Mulligan");
            if (mull != null) DumpPanelTree(mull, Path.Combine(outDir, "mulligan_tree_t0.tsv"), "Mulligan @t0 (原版自己弹的)");

            yield return BattleDrive();
            LogPanelStates("t1_after_drive");
            yield return ShotEOF(Path.Combine(outDir, "cap2_endframe_after_drive.png"));

            yield return TryFillChoosePanel(outDir);
            yield return new WaitForSecondsRealtime(2f);
            LogPanelStates("t2_forced");

            yield return ShotEOF(Path.Combine(outDir, "cap3_endframe_choose_forced.png"));
            Camera board = Camera.main;
            ShotCanvasRepoint(board, Path.Combine(outDir, "cap4_camrt_choose_forced.png"));

            if (_fillMenuT != null)
            {
                DumpPanelTree(_fillMenuT, Path.Combine(outDir, "choose_tree_forced.tsv"), "ChooseCardMenu 展开 (探针 SetActive + Setup 填卡)");
                DumpComponentsTree(_fillMenuT, Path.Combine(outDir, "choose_components_forced.tsv"), "ChooseCardMenu 组件字段");
            }
            DumpBundleAssetNames(outDir);
            LoggerInstance.Msg("[SJS] panel mode done (fillN=" + _fillN + ")");
            yield break;
        }

        // ================= v4.1 (2026-09-14 晚): panel2 —— 逐条试「UI 到底怎么才能拍下来」 =================
        // v4.0 实测结论: ① WaitForEndOfFrame + ReadPixels(backbuffer) 全黑 (直接读 & Blit 两条都黑, 文件字节数相同
        //   ⇒ 两条读的其实是同一个 RenderTexture.active, URP 留下的中间 RT, 不是 backbuffer)
        //   ② 把 canvas.worldCamera 指到主相机后立刻 cam.Render() —— UI 没出来 (怀疑 canvas 要等一帧重建 + 层被剔除)
        // 本版把「重指 → 等 3 帧 → 再渲」拆开, 并试自建正交 UI 相机 / UI 相机换 clear 色 / ScreenCaptureIntoRenderTexture。
        private System.Collections.Generic.List<Canvas> _savedCvs = new System.Collections.Generic.List<Canvas>();
        private System.Collections.Generic.List<UnityEngine.RenderMode> _savedMode = new System.Collections.Generic.List<UnityEngine.RenderMode>();
        private System.Collections.Generic.List<Camera> _savedCam = new System.Collections.Generic.List<Camera>();

        private int LayerNames(int mask)
        {
            return 0;
        }

        private string LayerMaskStr(int mask)
        {
            string s = "";
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0)
                {
                    string n = "";
                    try { n = UnityEngine.LayerMask.LayerToName(i); } catch { }
                    s += (s.Length > 0 ? "|" : "") + i + (n.Length > 0 ? ":" + n : "");
                }
            return s.Length > 0 ? s : "(none)";
        }

        private void RepointCanvases(Camera cam)
        {
            _savedCvs.Clear(); _savedMode.Clear(); _savedCam.Clear();
            try
            {
                foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                {
                    if (cv == null || !cv.gameObject.activeInHierarchy) continue;
                    _savedCvs.Add(cv); _savedMode.Add(cv.renderMode); _savedCam.Add(cv.worldCamera);
                    cv.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
                    cv.worldCamera = cam;
                }
                LoggerInstance.Msg("[SJS] repoint: " + _savedCvs.Count + " canvases -> '" + (cam != null ? cam.name : "NULL") + "' (等 3 帧再渲)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] repoint err: " + e.Message); }
        }

        private void RestoreCanvases()
        {
            for (int i = 0; i < _savedCvs.Count; i++)
            {
                try { _savedCvs[i].renderMode = _savedMode[i]; _savedCvs[i].worldCamera = _savedCam[i]; } catch { }
            }
            _savedCvs.Clear();
        }

        private void RenderCamToRT(Camera cam, string path, int w = 1920, int h = 1080)
        {
            try
            {
                if (cam == null) { LoggerInstance.Msg("[SJS] RT shot: cam null " + path); return; }
                var rt = new RenderTexture(w, h, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                cam.targetTexture = prev;
                RenderTexture.active = prev;
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT-RT ok " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-RT fail " + path + ": " + e); }
        }

        private IEnumerator FrameSettle(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        // B: 重指到主相机 → 等 n 帧 → 渲
        private IEnumerator ShotRepointSettled(Camera cam, string path)
        {
            RepointCanvases(cam);
            yield return FrameSettle(3);
            RenderCamToRT(cam, path);
            RestoreCanvases();
            yield return FrameSettle(2);
        }

        // D: 原 UI 相机 (换一个显眼的 clear 色, 便于判「它到底有没有渲东西」)
        private IEnumerator ShotUiCamSettled(string path)
        {
            Camera ui = null;
            try
            {
                foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
                    if (c != null && c.name.ToLowerInvariant().Contains("ui")) { ui = c; break; }
            }
            catch { }
            if (ui == null) { LoggerInstance.Msg("[SJS] uicam: NOT FOUND"); yield break; }
            var oldClear = ui.clearFlags; var oldCol = ui.backgroundColor;
            try { ui.clearFlags = CameraClearFlags.SolidColor; ui.backgroundColor = new Color(0.1f, 0.6f, 0.1f, 1f); }
            catch (Exception e) { LoggerInstance.Msg("[SJS] uicam clear err: " + e.Message); }
            yield return FrameSettle(3);
            RenderCamToRT(ui, path);
            try { ui.clearFlags = oldClear; ui.backgroundColor = oldCol; } catch { }
            yield return FrameSettle(2);
        }

        // E: 自建正交 UI 相机 (1 单位 = 1 像素), cullingMask 抄 UI 相机, 重指 canvas 到它
        private IEnumerator ShotOwnOrthoCamSettled(string path)
        {
            Camera ui = null;
            try
            {
                foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
                    if (c != null && c.name.ToLowerInvariant().Contains("ui")) { ui = c; break; }
            }
            catch { }
            GameObject go = null;
            Camera cam = null;
            try
            {
                go = new GameObject("SJS_OrthoUICam");
                if (ui != null) { go.transform.position = ui.transform.position; go.transform.rotation = ui.transform.rotation; }
                cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = Screen.height / 2f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 300f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.5f, 1f);
                cam.depth = -50f;
                if (ui != null) cam.cullingMask = ui.cullingMask;
                cam.enabled = true;   // 要它进正常渲染循环 (canvas 布局按绑定的相机算)
                LoggerInstance.Msg("[SJS] ortho cam: size=" + cam.orthographicSize + " cullingMask=" + LayerMaskStr(cam.cullingMask));
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] ortho cam create err: " + e.Message); }
            if (cam == null) yield break;
            RepointCanvases(cam);
            yield return FrameSettle(3);
            RenderCamToRT(cam, path);
            RestoreCanvases();
            try { UnityEngine.Object.Destroy(go); } catch { }
            yield return FrameSettle(2);
        }

        // A2: backbuffer 读法修正 (先把 RenderTexture.active 清成 null 再读)
        private IEnumerator ShotEOF2(string path, float timeout = 8f)
        {
            _eofDone = false;
            MelonCoroutines.Start(EOFWorker2(path));
            float t = 0f;
            while (!_eofDone && t < timeout) { yield return new WaitForSecondsRealtime(0.25f); t += 0.25f; }
            if (!_eofDone) LoggerInstance.Msg("[SJS] SHOT-EOF2 TIMEOUT: " + path);
        }

        private IEnumerator EOFWorker2(string path)
        {
            _eofDone = false;
            yield return new WaitForEndOfFrame();
            try
            {
                int w = Screen.width, h = Screen.height;
                LoggerInstance.Msg("[SJS] EOF2: RenderTexture.active(before)=" + (RenderTexture.active != null ? RenderTexture.active.name : "null"));
                RenderTexture.active = null;   // ← v4.1 的关键改动: 读 backbuffer, 不是 URP 留下的中间 RT
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                LoggerInstance.Msg("[SJS] SHOT-EOF2 ok " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-EOF2 fail: " + e); }
            _eofDone = true;
        }

        // F: ScreenCapture (README 记「会崩」, 放最后一步试)
        private IEnumerator ShotScreenCaptureRT(string path)
        {
            RenderTexture rt = null;
            bool started = false;
            try
            {
                rt = new RenderTexture(Screen.width, Screen.height, 24);
                UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
                started = true;
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-SCRCAP start fail: " + e.Message); }
            if (!started) yield break;
            yield return new WaitForEndOfFrame();
            try
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                UnityEngine.Object.Destroy(rt);
                LoggerInstance.Msg("[SJS] SHOT-SCRCAP ok " + path);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] SHOT-SCRCAP read fail: " + e); }
        }

        private void DumpTypeMethods(object comp, string tag)
        {
            try
            {
                var st = comp.GetType();
                var ms = st.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string s = "";
                if (ms != null)
                    for (int i = 0; i < ms.Length; i++) { try { s += ms[i].Name + " "; } catch { } }
                LoggerInstance.Msg("[SJS] methods[" + tag + "] n=" + (ms == null ? -1 : ms.Length) + ": " + s);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] methods[" + tag + "] err: " + e.Message); }
        }

        private void TryInvokeSetupChooseCardsUi(Il2Cpp.ChooseCardMenu menu, Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> list)
        {
            try
            {
                var st = menu.GetType();
                foreach (var name in new string[] { "SetupChooseCardsUi", "CreateDisplayCard", "CreateCardUI", "ShowChooseCards" })
                {
                    System.Reflection.MethodInfo mi = null;
                    try { mi = st.GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); } catch { }
                    LoggerInstance.Msg("[SJS] fill: GetMethod('" + name + "') = " + (mi != null));
                    if (mi == null || name != "SetupChooseCardsUi") continue;
                    object en = null;
                    try { en = mi.Invoke(menu, new object[] { list, false }); }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] fill: Invoke SetupChooseCardsUi 抛: " + e.Message); }
                    if (en != null) { MelonCoroutines.Start(WrapIl2CppEnum(en)); LoggerInstance.Msg("[SJS] fill: SetupChooseCardsUi 协程已起"); }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill: TryInvoke err: " + e.Message); }
        }

        private void SetNamedCanvasActive(string name, bool on)
        {
            try
            {
                foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                {
                    if (cv == null || cv.name != name) continue;
                    if (cv.gameObject.activeSelf == on) continue;
                    cv.gameObject.SetActive(on);
                    LoggerInstance.Msg("[SJS] canvas '" + name + "' -> activeSelf=" + on + " [探针动作, 不是原版自然状态]");
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] canvas '" + name + "' set err: " + e.Message); }
        }

        private IEnumerator PanelMode2(string outDir)
        {
            Application.runInBackground = true;
            LogEnvFacts();
            LogPanelStates("p2_t0");

            yield return ShotEOF2(Path.Combine(outDir, "capA1_eof2_nullrt_t0.png"));

            // 自然状态: 换牌面板 (Mulligan activeSelf=True) —— 先拍它, 再动 ChooseCardMenu
            var mull = FindInactiveByName("Mulligan");
            if (mull != null && mull.gameObject.activeInHierarchy)
            {
                DumpPanelTree(mull, Path.Combine(outDir, "mulligan_tree_p2.tsv"), "Mulligan @t0 (原版自己弹的)");
                LogSubtreeLayers(mull, "Mulligan");
            }
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "capB1_repoint_mulligan_natural.png"));
            yield return ShotUiCamSettled(Path.Combine(outDir, "capD1_uicam_mulligan_natural.png"));
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "capE1_ortho_mulligan_natural.png"));

            // 催出「选效果」面板
            yield return TryFillChoosePanel(outDir);
            yield return FrameSettle(3);
            LogPanelStates("p2_t1_forced");
            if (_fillMenuT != null)
            {
                DumpPanelTree(_fillMenuT, Path.Combine(outDir, "choose_tree_forced_p2.tsv"), "ChooseCardMenu (探针 SetActive + Setup/兜底填卡)");
                DumpComponentsTree(_fillMenuT, Path.Combine(outDir, "choose_components_p2.tsv"), "ChooseCardMenu 组件字段");
                LogSubtreeLayers(_fillMenuT, "ChooseCardMenu");
                if (_fillMenu != null)
                {
                    DumpTypeMethods(_fillMenu, "ChooseCardMenu");
                    Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> l = null;
                    try { l = _fillList; } catch { }
                    if (l != null && l.Count > 0) TryInvokeSetupChooseCardsUi(_fillMenu, l);
                    yield return FrameSettle(4);
                    DumpPanelTree(_fillMenuT, Path.Combine(outDir, "choose_tree_after_ui_call.tsv"), "ChooseCardMenu (兜底调用之后)");
                }
            }
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "capB2_repoint_choose_forced.png"));
            yield return ShotUiCamSettled(Path.Combine(outDir, "capD2_uicam_choose_forced.png"));
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "capE2_ortho_choose_forced.png"));

            // 第三轮: 关掉两个盖在最上面的整屏 canvas (LoadingMenu sortOrder=1000 / Tutorial 1500) —— 探针动作
            SetNamedCanvasActive("LoadingMenu", false);
            SetNamedCanvasActive("Tutorial", false);
            yield return FrameSettle(2);
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "capB3_repoint_no_loading.png"));
            yield return ShotUiCamSettled(Path.Combine(outDir, "capD3_uicam_no_loading.png"));
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "capE3_ortho_no_loading.png"));
            SetNamedCanvasActive("LoadingMenu", true);
            SetNamedCanvasActive("Tutorial", true);

            yield return ShotScreenCaptureRT(Path.Combine(outDir, "capF_screencapture.png"));
            LoggerInstance.Msg("[SJS] panel2 done");
            yield break;
        }

        private void LogSubtreeLayers(Transform t, string tag)
        {
            try
            {
                var all = t.GetComponentsInChildren<Transform>(true);
                string s = "";
                for (int i = 0; i < all.Length && i < 40; i++)
                {
                    if (all[i] == null) continue;
                    s += (s.Length > 0 ? " " : "") + all[i].name + "@" + all[i].gameObject.layer;
                }
                LoggerInstance.Msg("[SJS] layers[" + tag + "]: " + s);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] layers[" + tag + "] err: " + e.Message); }
        }

        private Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> _fillList;

        // ================= v4.2 (2026-09-14 夜): panel3 —— 面板填真实卡 + 干净地拍 =================
        // v4.1 结论: ① UI 相机在离线态**一张都不渲** (给它换了显眼的绿 clear 色, 渲到 RT 仍是**纯黑** ⇒ URP 根本没跑这台相机)
        //   ② 把 canvas 重指到主相机 + 等 3 帧, UI 就出来了 (capB3) ③ 自建正交 UI 相机也可以 (capE3)
        //   ④ LoadingMenu/Tutorial 这两个整屏 canvas 盖在最上面 (sortOrder 1000/1500), 关掉才看得到面板
        //   ⑤ 这个 interop 的 Il2CppSystem.Type.GetMethod(name,flags) 永远返回 null ⇒ 要遍历 GetMethods() 按名字找
        // ⚠️ 注意: 这里必须用 **Mono 侧** 的 System.Type (即 typeof(Il2Cpp.ChooseCardMenu) / comp.GetType()),
        //    不能用 Il2CppSystem.Type —— 后者的 GetMethod(name,flags) 在这个 interop 里永远返回 null (v4.1 实测),
        //    而且它返回的 MethodInfo 是 Il2CppSystem.Reflection 的, 跟 System.Reflection 不是一个体系。
        private System.Reflection.MethodInfo FindMethodByName(System.Type st, string name)
        {
            try
            {
                var ms = st.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ms == null) return null;
                for (int i = 0; i < ms.Length; i++)
                {
                    try { if (ms[i] != null && ms[i].Name == name) return ms[i]; } catch { }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] FindMethodByName err: " + e.Message); }
            return null;
        }

        private void InvokeSetupChooseCardsUi(Il2Cpp.ChooseCardMenu menu, Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> list)
        {
            try
            {
                var st = menu.GetType();
                var mi = FindMethodByName(st, "SetupChooseCardsUi");
                LoggerInstance.Msg("[SJS] fill2: FindMethodByName('SetupChooseCardsUi') = " + (mi != null));
                if (mi == null)
                {
                    var all = st.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    string s = "";
                    if (all != null) for (int i = 0; i < all.Length; i++) { try { s += all[i].Name + " "; } catch { } }
                    LoggerInstance.Msg("[SJS] fill2: Mono 侧方法表 n=" + (all == null ? -1 : all.Length) + ": " + s);
                    return;
                }
                object en = null;
                try { en = mi.Invoke(menu, new object[] { list, false }); }
                catch (Exception e) { LoggerInstance.Msg("[SJS] fill2: Invoke SetupChooseCardsUi 抛: " + e.Message); }
                if (en != null) { MelonCoroutines.Start(WrapIl2CppEnum(en)); LoggerInstance.Msg("[SJS] fill2: SetupChooseCardsUi 协程已起"); }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill2: err: " + e.Message); }
        }

        // 把所有 army × offensive/defensive 都问一遍, 挑「卡名非空」的那张表
        private void FillTryArmies2(UnityEngine.Object so, string outDir)
        {
            var typed = so.TryCast<Il2Cpp.EnviromentalEffectCardsSO>();
            if (typed == null) { LoggerInstance.Msg("[SJS] fill2: TryCast 失败"); return; }
            Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> best = null;
            string bestTag = "";
            var armies = (Il2Cpp.CardArmy[])Enum.GetValues(typeof(Il2Cpp.CardArmy));
            foreach (var a in armies)
            {
                foreach (bool off in new bool[] { true, false })
                {
                    Il2CppSystem.Collections.Generic.List<Il2Cpp.RawCardScript> l = null;
                    try { l = typed.GetEnvCardList(a, off, false, 0); } catch (Exception e) { LoggerInstance.Msg("[SJS] fill2: GetEnvCardList(" + a + "," + off + ") err " + e.Message); continue; }
                    int c = l != null ? l.Count : -1;
                    if (c <= 0) continue;
                    string n0 = "(?)"; int named = 0;
                    try { for (int i = 0; i < c; i++) { var nm = l[i] != null ? l[i].cardName : null; if (!string.IsNullOrEmpty(nm)) named++; if (i == 0) n0 = nm == null ? "(null)" : nm; } } catch { }
                    LoggerInstance.Msg("[SJS] fill2: GetEnvCardList(" + a + "," + off + ",false,0) = " + c + " 张, cardName 非空 " + named + " 张, [0]='" + n0 + "'");
                    if (best == null || (named > 0 && best.Count == 0)) { if (best == null || named > 0) { best = l; bestTag = a + "/" + (off ? "offensive" : "defensive"); } }
                }
            }
            if (best == null) { LoggerInstance.Msg("[SJS] fill2: 所有 army 都空"); return; }
            _fillList = best; _fillN = best.Count;
            LoggerInstance.Msg("[SJS] fill2: 选 " + bestTag + " 的 " + best.Count + " 张");
            for (int i = 0; i < best.Count && i < 12; i++)
            {
                string cn = "?", sid = "?", ty = "?", man = "?", hp = "?";
                try { cn = best[i] != null ? (best[i].cardName ?? "(null)") : "(obj null)"; } catch { }
                try { sid = best[i].cardShortId.ToString(); } catch { }
                try { ty = best[i].cardType.ToString(); } catch { }
                try { man = best[i].manaCost.ToString(); } catch { }
                try { hp = best[i].maxHealth.ToString(); } catch { }
                LoggerInstance.Msg("[SJS] fill2:   card[" + i + "] name='" + cn + "' shortId=" + sid + " type=" + ty + " mana=" + man + " hp=" + hp);
            }
            if (_fillMenu == null) { LoggerInstance.Msg("[SJS] fill2: menu 组件不在"); return; }
            try { _fillMenu.Setup(best, "", 30, 0); LoggerInstance.Msg("[SJS] fill2: Setup() ok"); }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fill2: Setup() 抛: " + e.Message + " ⇒ 改走私有 SetupChooseCardsUi"); InvokeSetupChooseCardsUi(_fillMenu, best); }
        }

        private IEnumerator PanelMode3(string outDir)
        {
            Application.runInBackground = true;
            LogEnvFacts();
            LogPanelStates("p3_t0");

            // 探针动作 1: 关掉盖在最上面的两个整屏 canvas
            SetNamedCanvasActive("LoadingMenu", false);
            SetNamedCanvasActive("Tutorial", false);
            yield return FrameSettle(2);

            // ---- A. 换牌面板: 原版自己弹出来的那个 (只要不点它它就一直开着) ----
            var mull = FindInactiveByName("Mulligan");
            LoggerInstance.Msg("[SJS] p3: Mulligan activeInHierarchy=" + (mull != null ? mull.gameObject.activeInHierarchy.ToString() : "NOTFOUND"));
            if (mull != null && mull.gameObject.activeInHierarchy)
            {
                DumpPanelTree(mull, Path.Combine(outDir, "p3_mulligan_tree.tsv"), "Mulligan 原版自然展开 (只把 canvas 重指到主相机/正交相机, 面板本身没动)");
                yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "p3_mulligan_repoint.png"));
                yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "p3_mulligan_ortho.png"));
            }

            // ---- B. 选牌/选效果面板: 探针展开 + 填真实候选卡 ----
            var cm = FindInactiveByName("ChooseCardMenu");
            Il2Cpp.BattleManager bm = FindBattleManager();
            UnityEngine.Object so = null;
            if (bm != null)
            {
                try { so = bm.enviromentalEffectCards; } catch (Exception e) { LoggerInstance.Msg("[SJS] p3: SO 读失败 " + e.Message); }
                try { if (bm.chooseCardManager != null) _fillMenu = bm.chooseCardManager; } catch { }
            }
            if (cm != null && _fillMenu == null) { try { _fillMenu = cm.GetComponent<Il2Cpp.ChooseCardMenu>(); } catch { } }
            LoggerInstance.Msg("[SJS] p3: SO=" + (so != null ? so.name : "null") + " menu=" + (_fillMenu != null));
            // 探针动作 2: 把换牌面板关掉, 让选牌面板不被压住
            if (mull != null) { mull.gameObject.SetActive(false); LoggerInstance.Msg("[SJS] p3: Mulligan -> inactive [探针动作]"); }
            if (cm != null) { cm.gameObject.SetActive(true); LoggerInstance.Msg("[SJS] p3: ChooseCardMenu -> active [探针动作]"); }
            yield return FrameSettle(2);
            if (so != null) FillTryArmies2(so, outDir);
            yield return FrameSettle(6);
            LogPanelStates("p3_t1_forced");
            if (cm != null)
            {
                DumpPanelTree(cm, Path.Combine(outDir, "p3_choose_tree.tsv"), "ChooseCardMenu (探针展开 + SetupChooseCardsUi 填卡)");
                DumpComponentsTree(cm, Path.Combine(outDir, "p3_choose_components.tsv"), "ChooseCardMenu 组件字段");
            }
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "p3_choose_repoint.png"));
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "p3_choose_ortho.png"));
            yield return FrameSettle(3);
            // 填完卡后再 dump 一次树 (卡是异步协程建的)
            if (cm != null) DumpPanelTree(cm, Path.Combine(outDir, "p3_choose_tree_after.png"), "ChooseCardMenu (填卡协程跑完之后)");
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "p3_choose_ortho2.png"));
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "p3_choose_repoint2.png"));

            LoggerInstance.Msg("[SJS] panel3 done (fillN=" + _fillN + ")");
            yield break;
        }

        // ================= v4.3 (2026-09-14 夜): panel4 —— 问原版自己的布局算法要数字 =================
        // 面板的卡是运行时用 Addressables 实例化的 (离线拿不到), 但 CardsHorizontalLayout 的
        // GetPosition / GetRotation / Scale 是**原版自己的公开方法** —— 直接问它, 比我们照静态字段推算准。
        private void DumpLayoutMath(Il2Cpp.CardsHorizontalLayout lay)
        {
            try { LoggerInstance.Msg("[SJS] p4: layout.Scale=" + lay.Scale); } catch (Exception e) { LoggerInstance.Msg("[SJS] p4: Scale err " + e.Message); }
            try
            {
                LoggerInstance.Msg("[SJS] p4: m_scale=" + lay.m_scale + " spacing=" + lay.m_betweenElementsSpacing
                    + " maxHeight=" + lay.m_maxHeight + " maxLayoutSize=" + lay.m_maxLayoutSize
                    + " nForMaxHeight=" + lay.m_numberOfCardsForMaxHeight + " useRotation=" + lay.m_useRotation
                    + " invertRotation=" + lay.m_invertRotation + " useViewportSize=" + lay.m_useViewportSize);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] p4: 字段读 err " + e.Message); }
            for (int n = 1; n <= 7; n++)
            {
                for (int i = 0; i < n; i++)
                {
                    string pos = "?", rot = "?";
                    try { var p = lay.GetPosition(i, n, -1); pos = p.x.ToString("F2") + "," + p.y.ToString("F2") + "," + p.z.ToString("F2"); }
                    catch (Exception e) { pos = "<err " + e.Message + ">"; }
                    try { var p = lay.GetPosition(i, n, -1); rot = lay.GetRotation(p, n).ToString(); }
                    catch (Exception e) { rot = "<err " + e.Message + ">"; }
                    LoggerInstance.Msg("[SJS] p4: GetPosition(i=" + i + ",n=" + n + ")=" + pos + "  rot=" + rot);
                }
            }
        }

        private IEnumerator PanelMode4(string outDir)
        {
            Application.runInBackground = true;
            LogEnvFacts();
            SetNamedCanvasActive("LoadingMenu", false);
            SetNamedCanvasActive("Tutorial", false);
            var mull = FindInactiveByName("Mulligan");
            if (mull != null && mull.gameObject.activeInHierarchy) { mull.gameObject.SetActive(false); LoggerInstance.Msg("[SJS] p4: Mulligan off [探针]"); }
            var cm = FindInactiveByName("ChooseCardMenu");
            if (cm != null) { cm.gameObject.SetActive(true); LoggerInstance.Msg("[SJS] p4: ChooseCardMenu on [探针]"); }
            yield return FrameSettle(3);

            // (a) 本地 bundle 里到底有没有 RawCardScript 资产?
            try
            {
                var t = Il2CppSystem.Type.GetType("RawCardScript, Assembly-CSharp");
                LoggerInstance.Msg("[SJS] p4: RawCardScript type = " + (t != null));
                int total = 0, hitBundles = 0; string sample = "";
                if (t != null)
                {
                    foreach (var b in _bundles)
                    {
                        if (b == null) continue;
                        try
                        {
                            var arr = b.LoadAllAssets(t);
                            if (arr == null || arr.Length == 0) continue;
                            hitBundles++; total += arr.Length;
                            if (sample.Length < 200)
                            {
                                for (int i = 0; i < arr.Length && i < 3; i++)
                                {
                                    try { var r = arr[i].TryCast<Il2Cpp.RawCardScript>(); string nm = r != null ? r.cardName : null; sample += " [" + b.name + ":" + (nm ?? "(null)") + "]"; } catch { }
                                }
                            }
                        }
                        catch { }
                    }
                }
                LoggerInstance.Msg("[SJS] p4: 本地 bundle 里 RawCardScript = " + total + " 个 (命中 " + hitBundles + " 个 bundle)" + sample);
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] p4: RawCardScript 扫描 err " + e.Message); }

            // (b) 面板组件 + 布局算法
            Il2Cpp.ChooseCardMenu menu = null;
            if (cm != null) { try { menu = cm.GetComponent<Il2Cpp.ChooseCardMenu>(); } catch { } }
            LoggerInstance.Msg("[SJS] p4: menu=" + (menu != null));
            if (menu != null)
            {
                try { LoggerInstance.Msg("[SJS] p4: chooseButtonText='" + (menu.chooseButtonText != null ? menu.chooseButtonText.text : "null") + "'"); } catch (Exception e) { LoggerInstance.Msg("[SJS] p4: chooseButtonText err " + e.Message); }
                try { LoggerInstance.Msg("[SJS] p4: titleTextField=" + (menu.titleTextField != null ? menu.titleTextField.name + " activeSelf=" + menu.titleTextField.activeSelf : "null")); } catch { }
                try { LoggerInstance.Msg("[SJS] p4: buttonsGroup=" + (menu.buttonsGroup != null ? menu.buttonsGroup.name + " activeSelf=" + menu.buttonsGroup.activeSelf : "null")); } catch { }
                try
                {
                    var sbf = menu.selectCardButtonFrame;
                    LoggerInstance.Msg("[SJS] p4: selectCardButtonFrame=" + (sbf != null ? sbf.gameObject.name : "null"));
                    if (sbf != null)
                    {
                        var go = UnityEngine.Object.Instantiate(sbf.gameObject);
                        try { go.transform.SetParent(cm, false); } catch { }
                        var rt = go.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            var c = new Vector3[4]; rt.GetWorldCorners(c);
                            LoggerInstance.Msg("[SJS] p4: SelectCardButtonFrame 实例 sd=" + rt.sizeDelta.x.ToString("F2") + "," + rt.sizeDelta.y.ToString("F2")
                                + " rect=" + rt.rect.width.ToString("F2") + "," + rt.rect.height.ToString("F2")
                                + " lossyScale=" + go.transform.lossyScale.x.ToString("F4")
                                + " wc=" + c[0].x.ToString("F1") + "," + c[0].y.ToString("F1") + ".." + c[2].x.ToString("F1") + "," + c[2].y.ToString("F1"));
                        }
                        UnityEngine.Object.Destroy(go);
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] p4: selectCardButtonFrame err " + e.Message); }
            }
            Il2Cpp.CardsHorizontalLayout lay = null;
            if (menu != null) { try { lay = menu.chooseMenuAnchor; } catch (Exception e) { LoggerInstance.Msg("[SJS] p4: chooseMenuAnchor 读 err " + e.Message); } }
            if (lay == null)
            {
                try { var a = FindInactiveByName("ChooseCardMenuAnchor"); if (a != null) lay = a.GetComponent<Il2Cpp.CardsHorizontalLayout>(); } catch { }
            }
            LoggerInstance.Msg("[SJS] p4: layout=" + (lay != null));
            if (lay != null) DumpLayoutMath(lay);

            yield return FrameSettle(2);
            DumpPanelTree(cm, Path.Combine(outDir, "p4_choose_tree.tsv"), "ChooseCardMenu (探针展开, 卡区仍是空的)");
            yield return ShotOwnOrthoCamSettled(Path.Combine(outDir, "p4_choose_ortho.png"));
            yield return ShotRepointSettled(Camera.main, Path.Combine(outDir, "p4_choose_repoint.png"));
            LoggerInstance.Msg("[SJS] panel4 done");
            yield break;
        }

        private IEnumerator Boot()
        {
            yield return new WaitForSecondsRealtime(10f);
            string bundleName = "";
            float wait = 12f;
            string outDir = "D:/2/Unity参照管线_0825/data";
            string multi = ""; // 检查点列表,逗号分隔秒数, v3.1 多点头
            try
            {
                if (File.Exists(CfgPath))
                {
                    string[] lines = File.ReadAllLines(CfgPath);
                    if (lines.Length > 0) bundleName = lines[0].Trim();
                    if (lines.Length > 1) wait = float.Parse(lines[1].Trim());
                    if (lines.Length > 2) outDir = lines[2].Trim();
                    if (lines.Length > 3) multi = lines[3].Trim();
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] cfg fail: " + e); }
            LoggerInstance.Msg("[SJS] cfg: bundle='" + bundleName + "' wait=" + wait + " out=" + outDir + " multi=" + multi);

            if (bundleName != "")
            {
                string aaDir = Application.streamingAssetsPath + "/" + BundlePathSuffix;
                AssetBundle ab = null;
                try
                {
                    foreach (var f in Directory.GetFiles(aaDir, "*.bundle"))
                    {
                        var b = AssetBundle.LoadFromFile(f);
                        if (b != null) _bundles.Add(b);
                        if (b != null && System.IO.Path.GetFileName(f) == bundleName) ab = b;
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] bundle fail: " + e); }
                if (ab != null)
                {
                    string[] paths = ab.GetAllScenePaths();
                    if (paths.Length > 0)
                    {
                        var loadOp = SceneManager.LoadSceneAsync(paths[0], LoadSceneMode.Single);
                        while (!loadOp.isDone) yield return new WaitForSecondsRealtime(0.5f);
                        LoggerInstance.Msg("[SJS] scene: " + SceneManager.GetActiveScene().name);
                    }
                }
            }

            yield return new WaitForSecondsRealtime(wait);
            LoggerInstance.Msg("[SJS] dump start, active scene: " + SceneManager.GetActiveScene().name);

            // 2026-09-20：先量运行时 RenderSettings / 灯光 / 环境条件（与原有流程无关，纯追加）
            LogRenderEnvFacts();

            // 2026-09-22：两块「烘焙光斑」的运行时实况（取两轮 —— 它们上的 MaterialFlickerEffect 让 alpha 随时间变）
            ProbeArenaFx(outDir, "t0");
            yield return new WaitForSecondsRealtime(1.5f);
            ProbeArenaFx(outDir, "t1");
            // 2026-09-22 追加：把闪烁的 **alpha 时序列**量满（0.15 s × 20 = 3 s）——
            // 「那一块到底该多亮」是**随时间变的**，单帧比对会被相位淹没（与粒子的 `WF_PSFIXSEED` 同一类问题）
            for (int i = 0; i < 20; i++)
            {
                ProbeArenaFx(outDir, "s" + i.ToString("00"));
                yield return new WaitForSecondsRealtime(0.15f);
            }

            if (multi == "drive")
            {
                yield return BattleDrive();
                int nHand = ExtractPlayerHand(outDir);
                LoggerInstance.Msg("[SJS] hand extract done, cards=" + nHand);
                var uiCam = PrepareBattleUiForShot();
                yield return new WaitForSecondsRealtime(1f);
                CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_battle_hand.png"));
                // v3.8: 换牌 (Mulligan) 面板开局会自己弹 ⇒ 先拍它一张 + 量它的运行时矩形
                var mullT = FindInactiveByName("Mulligan");
                if (mullT != null && mullT.gameObject.activeInHierarchy)
                {
                    using (var w = new StreamWriter(Path.Combine(outDir, "runtime_rect_mulligan.tsv"), false, Encoding.UTF8))
                        DumpRectChain(mullT, "", w, 0);
                    CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_mulligan_panel.png"));
                }
                // v3.8: 把 ChooseCardMenu 强制摆出来 (= 原版「选牌 / 选效果」面板), 再拍一张
                OpenChoosePanelProbe(outDir);
                yield return new WaitForSecondsRealtime(1f);
                CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_choose_panel.png"));
                DumpTree(SceneManager.GetActiveScene().name, Path.Combine(outDir, "runtime_ui_dump_drive.tsv"));
                yield return new WaitForSecondsRealtime(1f);
                CaptureShot(Path.Combine(outDir, "shot_drive.png"));
                LoggerInstance.Msg("[SJS] drive done");
                yield break;
            }

            if (multi == "panel")
            {
                yield return PanelMode(outDir);
                yield break;
            }

            if (multi == "panel2")
            {
                yield return PanelMode2(outDir);
                yield break;
            }

            if (multi == "panel3")
            {
                yield return PanelMode3(outDir);
                yield break;
            }

            if (multi == "panel4")
            {
                yield return PanelMode4(outDir);
                yield break;
            }

            if (multi == "menu_open")
            {
                yield return MenuOpenMode(outDir);
                yield break;
            }

            // 2026-10-11 M1: `env:<SO 名>` —— 把战场停在指定的【环境条件 SO】上再拍一张真渲图。
            //   ⛔ 不是 `panel*` / `drive` / `menu_open` / 空动作 那一批，**独立新分支**，原有行为一字未改。
            //   判据 / 自证办法见 EnvMode() 头部注释。
            if (multi != null && multi.StartsWith("env:", StringComparison.Ordinal))
            {
                yield return EnvMode(multi.Substring(4).Trim(), outDir);
                yield break;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            string baseOut = Path.Combine(outDir, "runtime_ui_dump_" + sceneName.Replace(' ', '_'));

            if (multi == "")
            {
                DumpTree(sceneName, baseOut + ".tsv");
                CaptureShot(Path.Combine(outDir, "shot_" + sceneName.Replace(' ', '_') + ".png"));
            }
            else
            {
                float total = 0f;
                foreach (var tok in multi.Split(','))
                {
                    float sec;
                    if (!float.TryParse(tok.Trim(), out sec)) continue;
                    if (sec > total) yield return new WaitForSecondsRealtime(sec - total);
                    total = sec;
                    sceneName = SceneManager.GetActiveScene().name;
                    string tag = "t" + ((int)sec).ToString();
                    DumpTree(sceneName, baseOut + "_" + tag + ".tsv");
                    CaptureShot(Path.Combine(outDir, "shot_" + sceneName.Replace(' ', '_') + "_" + tag + ".png"));
                }
                LoggerInstance.Msg("[SJS] multi done");
            }
        }

        // ================= v4.4 (2026-10-11 · M1): `env:<SO 名>` —— 停在指定【环境条件】上并拍真渲图 =================
        //  为什么需要：原版「1 场 × 4 环境」里那 3 个**非默认**环境只有打了对应进攻卡才出现，
        //    而正式版已关服（手牌注入 `inj FAIL: empty Data`、卡数据在远端 CCD）⇒ 拿不到。
        //    所以只能在 mod 里**直接调那条底层方法**（只读代理已把 `ApplyEnvironment` 的全部调用点反查完：
        //    `Awake` / `ApplyEnvEffect` / `DisableEnvironment` / `TestToggleOn` / `BackToDefault`）。
        //  判据（**签名桩逐字核过**，目录 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）：
        //    · `ScenarioEnvironmentConditionsManager.cs:4`   public class ScenarioEnvironmentConditionsManager : MonoBehaviour
        //    · `ScenarioEnvironmentConditionsManager.cs:11`  public ScenarioEnvironmentConditionSO CurrentEnvironment { get; private set; }
        //    · `ScenarioEnvironmentConditionsManager.cs:17`  public void ApplyEnvironment(ScenarioEnvironmentConditionSO environmentConditions, bool instant)
        //    · `ScenarioEnvironmentConditionsManager.cs:21`  public void ApplyDefaultEnvironment(bool instant)
        //    · `ScenarioEnvironmentConditionSO.cs:6/26/65`   class / private float blendTime / public ScenarioBlendOptions EnableEnvironment(bool instant)
        //       （`blendTime` 是**私有**字段，没有公开 getter ⇒ 只能反射读，名字见上）
        //  取 SO 的办法（三条路里只有第三条通）：
        //    · 按名字 `LoadAsset(name)` ✗ —— 本 build 的 `GetAllAssetNames()` 返回的是**容器键(GUID)**
        //    · Addressables 按名字 ✗ —— `aa/catalog.bin` 里搜 `'EnvironmentalCondition'` = **0 命中**
        //      （环境 SO 只在 `bundle_*cardanims*_assets_all` 里、不是 addressable 条目）
        //    · ✅ **按类型枚举**：`Resources.FindObjectsOfTypeAll(type)` → 逐 bundle `LoadAllAssets(type)`
        //       再按 `Object.name` 匹配。SO 住 cardanims 包（离线判据 `工具/gen_environment_conditions.py:52`）。
        //  ⛔ 找不到 SO / 找不到管理器 / 调用抛异常 ⇒ **出声并退出，⛔ 不拍图**
        //     （拍一张默认态冒充「环境生效」= 静默失败，本仓红线）。
        //  ✅ 自证（不靠"看着像"）：本动作**同一进程内**拍两张 —— 调用前 `<tag>_BEFORE.png`、
        //     等够 `blendTime` 后 `<tag>.png`；并把调用前后 `RenderSettings` 的
        //     `ambientLight` / `fog` / `fogColor` / `fogDensity` 逐项打出来（配合两张图的逐像素差）。
        //  已知限制：`instant=false`（原版打进攻卡走的就是 false）⇒ 雾是**补间**的，
        //     必须等 `blendTime`；`EnableEnvironment` 还会 `Instantiate(scenarioObjects, Camera.main)`
        //     ⇒ `Camera.main` 必须是那张 arena 相机（arena 场景里就是）。

        private static string EnvSafeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "noname";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
                    sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }

        private static string ObjName(UnityEngine.Object o)
        {
            try { return o == null ? "null" : (o.name ?? ""); } catch { return "?"; }
        }

        // 反射读 SO 上的私有 float 字段（`blendTime` 是 private，无 getter）
        private static float EnvSoFloat(object so, string field, float dflt)
        {
            try
            {
                if (so == null) return dflt;
                var t = so.GetType();
                var f = t.GetField(field);
                if (f == null) return dflt;
                var v = f.GetValue(so);
                if (v == null) return dflt;
                float r;
                if (float.TryParse(v.ToString(), System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out r)) return r;
                return dflt;
            }
            catch { return dflt; }
        }

        // cfg 第 1 行空 ⇒ Boot 不加载任何 bundle ⇒ 这里自己补一次（只在本动作里调，不动既有流程）
        private void EnsureBundlesLoaded()
        {
            if (_bundles.Count > 0) return;
            try
            {
                string aaDir = Application.streamingAssetsPath + "/" + BundlePathSuffix;
                LoggerInstance.Msg("[SJS] env: _bundles 空（cfg 第 1 行没写 bundle）⇒ 主动加载 " + aaDir);
                foreach (var f in Directory.GetFiles(aaDir, "*.bundle"))
                {
                    try { var b = AssetBundle.LoadFromFile(f); if (b != null) _bundles.Add(b); } catch { }
                }
                LoggerInstance.Msg("[SJS] env: 主动加载到 " + _bundles.Count + " 个 bundle");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env: bundle 加载失败 " + e.Message); }
        }

        // 扫所有 ScenarioEnvironmentConditionSO（只扫指定范围的包），把名字一并带回来
        private void ScanEnvSos(bool onlyCardanims, System.Collections.Generic.List<UnityEngine.Object> pool,
                                System.Collections.Generic.List<string> names)
        {
            var soType = Il2CppSystem.Type.GetType("ScenarioEnvironmentConditionSO, Assembly-CSharp");
            LoggerInstance.Msg("[SJS] env: 类型解析 'ScenarioEnvironmentConditionSO, Assembly-CSharp' = " + (soType != null));
            if (soType == null) return;
            int nBundle = 0;
            foreach (var b in _bundles)
            {
                if (b == null) continue;
                string bn = "?"; try { bn = b.name; } catch { }
                if (onlyCardanims && bn.IndexOf("cardanims", StringComparison.OrdinalIgnoreCase) < 0) continue;
                nBundle++;
                try
                {
                    var arr = b.LoadAllAssets(soType);
                    if (arr == null) continue;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var o = arr[i] as UnityEngine.Object;
                        if (o == null) continue;
                        string nm = ""; try { nm = o.name; } catch { }
                        pool.Add(o); names.Add(nm);
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] env: 包 '" + bn + "' LoadAllAssets 抛: " + e.Message); }
            }
            LoggerInstance.Msg("[SJS] env: 扫了 " + nBundle + " 个包（onlyCardanims=" + onlyCardanims
                               + "）⇒ 共 " + pool.Count + " 个 ScenarioEnvironmentConditionSO");
        }

        // 从候选里按名字挑（精确 → 忽略大小写 → 唯一子串）；挑不到/有歧义都出声
        private Il2Cpp.ScenarioEnvironmentConditionSO PickEnvSo(
            System.Collections.Generic.List<UnityEngine.Object> pool,
            System.Collections.Generic.List<string> names, string want, out string how)
        {
            how = "";
            if (pool == null || pool.Count == 0) return null;
            for (int i = 0; i < pool.Count; i++)
                if (string.Equals(names[i], want, StringComparison.Ordinal)) { how = "精确"; return AsEnvSo(pool[i]); }
            for (int i = 0; i < pool.Count; i++)
                if (string.Equals(names[i], want, StringComparison.OrdinalIgnoreCase)) { how = "忽略大小写"; return AsEnvSo(pool[i]); }
            int hit = -1, nHit = 0;
            for (int i = 0; i < pool.Count; i++)
                if (!string.IsNullOrEmpty(names[i]) && names[i].IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                { nHit++; if (hit < 0) hit = i; }
            if (nHit == 1) { how = "唯一子串"; return AsEnvSo(pool[hit]); }
            if (nHit > 1)
            {
                LoggerInstance.Msg("[SJS] env: ⚠ 子串 '" + want + "' 命中 " + nHit + " 个 ⇒ 有歧义，拒绝猜（按精确名重写 cfg）");
                for (int i = 0; i < pool.Count; i++)
                    if (!string.IsNullOrEmpty(names[i]) && names[i].IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                        LoggerInstance.Msg("[SJS] env:   候选 '" + names[i] + "'");
            }
            return null;
        }

        private Il2Cpp.ScenarioEnvironmentConditionSO AsEnvSo(UnityEngine.Object o)
        {
            if (o == null) return null;
            try { return o.TryCast<Il2Cpp.ScenarioEnvironmentConditionSO>(); }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env: TryCast<ScenarioEnvironmentConditionSO> 抛: " + e.Message); return null; }
        }

        private IEnumerator EnvMode(string soName, string outDir)
        {
            LoggerInstance.Msg("[SJS] env: 起 · 目标 SO='" + soName + "' scene=" + SceneManager.GetActiveScene().name);
            if (string.IsNullOrEmpty(soName))
            {
                LoggerInstance.Msg("[SJS] env: ❌ cfg 第 4 行是 'env:' 但**没给 SO 名** ⇒ 停手，不拍图");
                yield break;
            }

            // ---- ① 找环境条件管理器（arena 场景自带且 active）----
            Il2Cpp.ScenarioEnvironmentConditionsManager mgr = null;
            try
            {
                var arr = UnityEngine.Object.FindObjectsOfType<Il2Cpp.ScenarioEnvironmentConditionsManager>(true);
                LoggerInstance.Msg("[SJS] env: FindObjectsOfType<ScenarioEnvironmentConditionsManager> -> "
                                   + (arr == null ? -1 : arr.Length));
                if (arr != null && arr.Length > 0) mgr = arr[0];
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] env: 取管理器抛: " + e.Message); }
            if (mgr == null)
            {
                var bm = FindBattleManager();
                LoggerInstance.Msg("[SJS] env: 场景里没有 ⇒ 兜底 BattleManager = " + (bm != null));
                if (bm != null)
                {
                    try { mgr = bm.EnvironmentConditionsManager; } catch (Exception e) { LoggerInstance.Msg("[SJS] env: bm.EnvironmentConditionsManager 读失败: " + e.Message); }
                }
            }
            if (mgr == null)
            {
                LoggerInstance.Msg("[SJS] env: ❌ 找不到 ScenarioEnvironmentConditionsManager ⇒ **停手，不拍图**"
                                   + "（拍默认态会让人以为环境生效了）");
                yield break;
            }
            string mgrGo = "?"; try { mgrGo = ObjName(mgr.gameObject) + " activeInHierarchy=" + mgr.gameObject.activeInHierarchy; } catch { }
            LoggerInstance.Msg("[SJS] env: 管理器 go=" + mgrGo);

            Il2Cpp.ScenarioEnvironmentConditionSO curBefore = null;
            try { curBefore = mgr.CurrentEnvironment; } catch (Exception e) { LoggerInstance.Msg("[SJS] env: 读 CurrentEnvironment 抛: " + e.Message); }
            LoggerInstance.Msg("[SJS] env: 调用前 CurrentEnvironment=" + SoStr(curBefore));
            LoggerInstance.Msg("[SJS] env: 调用前 RenderSettings ambient=" + Fmt(RenderSettings.ambientLight)
                               + " fog=" + RenderSettings.fog + " fogMode=" + RenderSettings.fogMode
                               + " fogColor=" + Fmt(RenderSettings.fogColor) + " fogDensity=" + RenderSettings.fogDensity);

            // ---- ② 取目标 SO ----
            Il2Cpp.ScenarioEnvironmentConditionSO so = null;
            string how = "";
            if (curBefore != null) { try { if (curBefore.name == soName) { so = curBefore; how = "已经是它(CurrentEnvironment)"; } } catch { } }

            if (so == null && !string.IsNullOrEmpty(soName))
            {
                // 2a 内存里已加载的资产（含场景直连引用的 defaultEnvironment）
                try
                {
                    var soType = Il2CppSystem.Type.GetType("ScenarioEnvironmentConditionSO, Assembly-CSharp");
                    if (soType != null)
                    {
                        var all = UnityEngine.Resources.FindObjectsOfTypeAll(soType);
                        LoggerInstance.Msg("[SJS] env: Resources.FindObjectsOfTypeAll -> " + (all == null ? -1 : all.Length));
                        if (all != null)
                        {
                            var p0 = new System.Collections.Generic.List<UnityEngine.Object>();
                            var n0 = new System.Collections.Generic.List<string>();
                            for (int i = 0; i < all.Length; i++)
                            {
                                var o = all[i] as UnityEngine.Object;
                                if (o == null) continue;
                                p0.Add(o); try { n0.Add(o.name); } catch { n0.Add(""); }
                            }
                            so = PickEnvSo(p0, n0, soName, out how);
                            LoggerInstance.Msg("[SJS] env: 2a 已加载资产命中 = " + (so != null) + (so != null ? (" (" + how + ")") : ""));
                        }
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] env: 2a FindObjectsOfTypeAll 抛: " + e.Message); }
            }

            if (so == null && !string.IsNullOrEmpty(soName))
            {
                // 2b bundle 里按类型枚举（第一遍只扫 cardanims 包；没中再全量）
                EnsureBundlesLoaded();
                var pool = new System.Collections.Generic.List<UnityEngine.Object>();
                var nms = new System.Collections.Generic.List<string>();
                ScanEnvSos(true, pool, nms);
                so = PickEnvSo(pool, nms, soName, out how);
                if (so == null && _bundles.Count > 0)
                {
                    LoggerInstance.Msg("[SJS] env: 2b 第一遍（只扫 cardanims 包）没命中 ⇒ 全量扫 " + _bundles.Count + " 个包");
                    var pool2 = new System.Collections.Generic.List<UnityEngine.Object>();
                    var nms2 = new System.Collections.Generic.List<string>();
                    ScanEnvSos(false, pool2, nms2);
                    so = PickEnvSo(pool2, nms2, soName, out how);
                    if (so == null && pool2.Count > 0)
                    {
                        int lim = pool2.Count < 60 ? pool2.Count : 60;
                        LoggerInstance.Msg("[SJS] env: 看过的候选名（前 " + lim + "/" + pool2.Count + "）—— 拿来核 cfg 里的名字写没写错：");
                        for (int i = 0; i < lim; i++) LoggerInstance.Msg("[SJS] env:   [" + i + "] " + nms2[i]);
                    }
                }
                LoggerInstance.Msg("[SJS] env: 2b bundle 枚举命中 = " + (so != null) + (so != null ? (" (" + how + ")") : ""));
            }

            if (so == null && !string.IsNullOrEmpty(soName))
            {
                // 2c Addressables 按名字（离线判据：catalog.bin 里没有这个键 ⇒ 多半失败，但便宜，留个实证）
                StartAssetLoad(soName);
                int w = 0; while (w++ < 100 && !AssetLoadDone()) yield return new WaitForSecondsRealtime(0.1f);
                var o = AssetLoadResult();
                LoggerInstance.Msg("[SJS] env: 2c Addressables('" + soName + "') 等了 " + (w / 10f).ToString("F1") + "s -> " + ObjName(o));
                if (o != null) { so = AsEnvSo(o); if (so != null) how = "Addressables"; }
            }

            if (so == null)
            {
                LoggerInstance.Msg("[SJS] env: ❌ 找不到 ScenarioEnvironmentConditionSO '" + soName + "' ⇒ **停手，不拍图**"
                                   + "（上面已打出扫过哪些包 / 看过哪些候选名）");
                yield break;
            }
            LoggerInstance.Msg("[SJS] env: ✅ 目标 SO = " + SoStr(so) + " (取法: " + how + ")");

            // ---- ③ 调用前先拍一张「BEFORE」（同进程、同相机 ⇒ 后面那张的对照是干净的）----
            string tag = EnvSafeName(soName);
            string shotBefore = Path.Combine(outDir, "shot_env_" + tag + "_BEFORE.png");
            CaptureShot(shotBefore);

            // ---- ④ 调底层方法（instant=false = 原版打进攻卡走的那条）----
            float bt = EnvSoFloat(so, "blendTime", 3f);
            bool ok = false; string err = "";
            try { mgr.ApplyEnvironment(so, false); ok = true; }
            catch (Exception e) { err = e.Message; }
            LoggerInstance.Msg("[SJS] env: ApplyEnvironment(so, instant:false) " + (ok ? "已调" : ("抛异常: " + err)));
            if (!ok)
            {
                LoggerInstance.Msg("[SJS] env: ❌ 调用失败 ⇒ **停手，不拍 AFTER 图**");
                yield break;
            }
            string curAfter = "?";
            try { curAfter = ObjName(mgr.CurrentEnvironment); } catch (Exception e) { curAfter = "err:" + e.Message; }
            LoggerInstance.Msg("[SJS] env: 调用后 CurrentEnvironment='" + curAfter + "' （应 == '" + soName + "'）");

            // ---- ⑤ 等补间（blendTime；雾/环境光是补间 ⇒ 不等会拍到过渡中）----
            float waitSec = (bt > 0.5f ? bt : 0.5f) + 1.0f;
            LoggerInstance.Msg("[SJS] env: blendTime=" + bt.ToString("F2") + "s ⇒ 等 " + waitSec.ToString("F2") + "s");
            float total = 0f;
            for (int i = 0; i < 4; i++)
            {
                float step = waitSec / 4f;
                yield return new WaitForSecondsRealtime(step);
                total += step;
                LoggerInstance.Msg("[SJS] env:   t=" + total.ToString("F2") + "s fogDensity=" + RenderSettings.fogDensity
                                   + " ambient=" + Fmt(RenderSettings.ambientLight));
            }

            LoggerInstance.Msg("[SJS] env: 调用后 RenderSettings ambient=" + Fmt(RenderSettings.ambientLight)
                               + " fog=" + RenderSettings.fog + " fogMode=" + RenderSettings.fogMode
                               + " fogColor=" + Fmt(RenderSettings.fogColor) + " fogDensity=" + RenderSettings.fogDensity);
            LoggerInstance.Msg("[SJS] env: 对照 SO 目标值 ambientColor/ambientBlend/fogColor/fogDensity 见上面 '目标 SO =' 那一行");

            string shotAfter = Path.Combine(outDir, "shot_env_" + tag + ".png");
            CaptureShot(shotAfter);
            LoggerInstance.Msg("[SJS] env: done · BEFORE=" + shotBefore + " · AFTER=" + shotAfter
                               + " · 自证 = 两张逐像素差（同一进程/同一相机）+ 上面 ambient/fog 调用前≠调用后");
        }

        // ================= v4.3 (2026-10-16 · A817): menu_open —— 主菜单 →（奖励窗 / 锻造页）运行时 dump =================
        //  目的：把**锻造页那枚青色八边形徽 + 金框狼徽**在运行时认出来（节点名 / 父链 / rect / sprite / 显示条件）。
        //  钥匙（离线静态查到的硬证据）：`Main Menu Navigation Button - Rewards` 上挂 `OpenWindowButton`，
        //    它的 `windowToOpenPrefab.m_AssetGUID = 32024b169f2f0844288f6f547d8c459b` 在本地 `aa/catalog.bin` 命中、
        //    `bundle_menus_assets_all` 的容器键就是它（→ `Rewards Base Submenu Variant`）⇒ 反射调 `OpenWindow()`。
        //  ⚠️ 已知限制：主菜单 **97% 靠运行时实例化**（0927 那份 dump 里 `Tab Buttons`/`Submenu` 各 0 命中）；
        //    窗内容是 **Addressables 异步**装的、且 live-ops 那部分在远端 ⇒ 开完**必须等**几个实时秒再 dump。
        //  ⚠️ TMP 未激活时 `textBounds` 是垃圾 ⇒ 本探针只读 `m_text` / `Image.sprite.name`（`DumpGo` 已经在读 sprite 名）。
        private IEnumerator MenuOpenMode(string outDir)
        {
            LoggerInstance.Msg("[SJS] menu_open: 起 · scene=" + SceneManager.GetActiveScene().name);

            // ---- ① 找那颗「Rewards」入口钮 ----
            var btn = FindInactiveByName("Main Menu Navigation Button - Rewards");
            if (btn == null)
            {
                LoggerInstance.Msg("[SJS] menu_open: 精确名没找到 ⇒ 全扫名字含 'Reward' 的 Transform");
                try
                {
                    var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] == null) continue;
                        string n = all[i].name;
                        if (n != null && n.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) >= 0)
                            LoggerInstance.Msg("[SJS] menu_open: 候选 '" + n + "' path=" + Tr(all[i], 8)
                                               + " active=" + all[i].gameObject.activeInHierarchy);
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: 全扫失败 " + e.Message); }
            }
            else
            {
                LoggerInstance.Msg("[SJS] menu_open: 找到钮 path=" + Tr(btn, 8) + " activeSelf=" + btn.gameObject.activeSelf);
                // 组件 + 全部实例字段（含 OpenWindowButton.windowToOpenPrefab）—— 这就是 A817 要的第一手判据
                DumpComponentsTree(btn, Path.Combine(outDir, "menu_open_btn_components.tsv"),
                                   "Main Menu Navigation Button - Rewards");
                // ---- ② 反射调 OpenWindow() ----
                //  ⚠️ v4.3 实测：走 `GetComponents<UnityEngine.Component>()` 拿到的包装器 `GetType()` 只报基类
                //    `Component` ⇒ 按名字永远匹配不上。必须**按具体类型取**：`GetComponent<Il2Cpp.OpenWindowButton>()`
                //    （该类型在 `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll` 里确实有）。
                try
                {
                    var owb = btn.GetComponent<Il2Cpp.OpenWindowButton>();
                    LoggerInstance.Msg("[SJS] menu_open: GetComponent<Il2Cpp.OpenWindowButton>() = " + (owb != null));
                    if (owb != null)
                    {
                        DumpPrivateFields(owb, "OpenWindowButton", outDir);
                        var mi = FindMethodByName(owb.GetType(), "OpenWindow");
                        LoggerInstance.Msg("[SJS] menu_open: FindMethodByName('OpenWindow') = " + (mi != null));
                        if (mi != null)
                        {
                            try { mi.Invoke(owb, null); LoggerInstance.Msg("[SJS] menu_open: OpenWindow() 已调"); }
                            catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: OpenWindow Invoke 抛 " + e.Message); }
                        }
                        else
                        {
                            var mi2 = FindMethodByName(owb.GetType(), "ButtonClick");
                            LoggerInstance.Msg("[SJS] menu_open: 退而求其次 FindMethodByName('ButtonClick') = " + (mi2 != null));
                            if (mi2 != null) { try { mi2.Invoke(owb, null); LoggerInstance.Msg("[SJS] menu_open: ButtonClick() 已调"); } catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: ButtonClick 抛 " + e.Message); } }
                        }
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: 取 OpenWindowButton 失败 " + e.Message); }
            }

            var uiCam = PrepareBattleUiForShot();

            // ---- ③ 等 Addressables 把窗装出来（异步）----
            yield return new WaitForSecondsRealtime(4f);
            DumpTree(SceneManager.GetActiveScene().name, Path.Combine(outDir, "runtime_ui_dump_menu_open.tsv"));
            CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_menu_open.png"));
            LoggerInstance.Msg("[SJS] menu_open: A 路（反射 OpenWindow）跑完，接着走 B 路");

            // ---- ③b B 路：直接用 Addressables 按【容器键】把窗 prefab 取出来自己 Instantiate ----
            //   键 = `OpenWindowButton.windowToOpenPrefab` 那条 GUID（R5 已查实它在本地 `aa/catalog.bin` 里命中，
            //   且 `bundle_menus_assets_all` 的容器键就是它 → `Rewards Base Submenu Variant`）。
            //   ⚠️ 这条**拿到的就是静态 prefab 树**（R5 已 dump 过同一份）⇒ 对 A817 只是**对照**，不是新证据；
            //      真正的新证据只能来自「运行时把窗开出来之后的样子」。
            const string WindowKey = "32024b169f2f0844288f6f547d8c459b";
            StartAssetLoad(WindowKey);
            int guard = 0;
            while (!AssetLoadDone() && guard < 120) { guard++; yield return new WaitForSecondsRealtime(0.2f); }
            LoggerInstance.Msg("[SJS] menu_open: B 路 Addressables 等待 " + (guard * 0.2f).ToString("F1") + "s, started=" + _loadStarted);
            UnityEngine.GameObject winPrefab = null;
            try { var o = AssetLoadResult(); if (o != null) winPrefab = o.TryCast<UnityEngine.GameObject>(); } catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: TryCast 抛 " + e.Message); }
            LoggerInstance.Msg("[SJS] menu_open: B 路取到 prefab = " + (winPrefab != null));
            if (winPrefab == null)
            {
                // 兜底：从已加载的 bundle 里按容器键取
                try
                {
                    foreach (var b in _bundles)
                    {
                        try
                        {
                            var has = b.GetAllAssetNames();
                            if (has == null) continue;
                            bool hit = false;
                            for (int i = 0; i < has.Length; i++) if (has[i] != null && has[i].IndexOf(WindowKey, StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; break; }
                            if (!hit) continue;
                            var o2 = b.LoadAsset<UnityEngine.GameObject>(WindowKey);
                            LoggerInstance.Msg("[SJS] menu_open: bundle '" + b.name + "' 命中容器键 ⇒ LoadAsset = " + (o2 != null));
                            if (o2 != null) { winPrefab = o2; break; }
                        }
                        catch { }
                    }
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: bundle 兜底抛 " + e.Message); }
            }
            if (winPrefab != null)
            {
                try
                {
                    var holder = FindInactiveByName("3 - PopUp Holder");
                    if (holder == null) holder = FindInactiveByName("PopUp Holder");
                    var parent = holder != null ? holder : null;
                    var inst = UnityEngine.Object.Instantiate(winPrefab, parent);
                    inst.name = "A817_RewardsWindow_Probe";
                    inst.SetActive(true);
                    LoggerInstance.Msg("[SJS] menu_open: B 路已 Instantiate 到 '" + (parent != null ? parent.name : "(场景根)") + "'");
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: Instantiate 抛 " + e.Message); }
                yield return new WaitForSecondsRealtime(3f);
                DumpTree(SceneManager.GetActiveScene().name, Path.Combine(outDir, "runtime_ui_dump_menu_open_b.tsv"));
                CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_menu_open_b.png"));
            }

            // ---- ④ 把「名字含 Forge/Tab 的候选」全列出来（供下一步找锻造页那颗 tab）----
            LogNameCandidates("Forge", 60);
            LogNameCandidates("Tab", 60);
            LogNameCandidates("Octagon", 40);
            LogNameCandidates("wolf", 40);

            // ---- ⑤ 点锻造页那颗 tab（**精确名 `Forge Button`**；Button.onClick 优先，其次 Toggle）----
            bool clicked = ClickNodeByNameExact("Forge Button");
            if (!clicked) clicked = ClickNodeByNameContains("Forge Button");
            if (!clicked) LoggerInstance.Msg("[SJS] menu_open: 没点到 Forge Button（见上面的候选清单）");
            yield return new WaitForSecondsRealtime(4f);
            DumpTree(SceneManager.GetActiveScene().name, Path.Combine(outDir, "runtime_ui_dump_menu_forge.tsv"));
            CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_menu_forge.png"));
            // ⑤b 把「锻造页」那一支**单独**dump 一份（带 rect 数字 + sprite 名）—— A817 的正判据在这一支里
            var forgeTab = FindInactiveByName("Forge Tab");
            LoggerInstance.Msg("[SJS] menu_open: FindInactiveByName('Forge Tab') = " + (forgeTab != null)
                               + (forgeTab != null ? (" activeInHierarchy=" + forgeTab.gameObject.activeInHierarchy) : ""));
            // ⑤c **强制激活**锻造页那一支（原版的切换器在我们手搓的实例上没接上 ⇒ 手工 SetActive）
            //     —— 目的只是「把版面渲出来跟用户实拍并排比」，⛔ 不代表原版的显示条件。
            if (forgeTab != null && !forgeTab.gameObject.activeInHierarchy)
            {
                try
                {
                    var tabsRoot = forgeTab.parent;
                    if (tabsRoot != null)
                        for (int i = 0; i < tabsRoot.childCount; i++)
                        {
                            var ch = tabsRoot.GetChild(i);
                            if (ch == null) continue;
                            bool want = (ch == forgeTab);
                            if (ch.gameObject.activeSelf != want) ch.gameObject.SetActive(want);
                        }
                    else forgeTab.gameObject.SetActive(true);
                    LoggerInstance.Msg("[SJS] menu_open: 已强制激活 Forge Tab（其余兄弟关掉）activeInHierarchy=" + forgeTab.gameObject.activeInHierarchy);
                }
                catch (Exception e) { LoggerInstance.Msg("[SJS] menu_open: 强制激活抛 " + e.Message); }
                yield return new WaitForSecondsRealtime(2f);
                CaptureShotFull(Camera.main, uiCam, Path.Combine(outDir, "shot_menu_forge_forced.png"));
                DumpTree(SceneManager.GetActiveScene().name, Path.Combine(outDir, "runtime_ui_dump_menu_forge_forced.tsv"));
            }
            if (forgeTab != null) DumpSubtree(forgeTab, Path.Combine(outDir, "menu_forge_tab_subtree.tsv"));
            var tabButtons = FindInactiveByName("Tab Buttons");
            if (tabButtons != null) DumpSubtree(tabButtons, Path.Combine(outDir, "menu_tab_buttons_subtree.tsv"));

            // ---- ⑥ 把带 sprite 的 Image 全列一遍（A817 的正判据：找 rect ≈ 59×61 或直径 ≈ 90 的那一枚）----
            DumpImagesWithSprite(outDir);
            // ---- ⑦ A817 定向搜捕：octagon 贴图 / 小号数字文字，连父链一起打 ----
            HuntBadge(outDir);
            LoggerInstance.Msg("[SJS] menu_open: done");
        }

        /// <summary>反射读一个组件的**私有实例字段** —— 必须先把组件按**具体类型**取出来
        /// （`GetComponents&lt;UnityEngine.Component&gt;()` 拿到的包装器 `GetType()` 只报基类 `Component`，
        ///  `GetFields` 于是只给 `isWrapped`/`pooledPtr` 那两条内部字段 —— v4.3 实测）。</summary>
        private void DumpPrivateFields(UnityEngine.Component c, string tag, string outDir)
        {
            try
            {
                var st = c.GetType();
                LoggerInstance.Msg("[SJS] fields<" + tag + ">: 类型 = " + st.FullName);
                var fs = st.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                LoggerInstance.Msg("[SJS] fields<" + tag + ">: 字段数 " + (fs == null ? -1 : fs.Length));
                if (fs == null) return;
                for (int i = 0; i < fs.Length; i++)
                {
                    string nm = "?"; string v = "";
                    try { nm = fs[i].Name; } catch { }
                    try { var o = fs[i].GetValue(c); v = o == null ? "(null)" : o.ToString(); }
                    catch (Exception e) { v = "(读值抛 " + e.Message + ")"; }
                    LoggerInstance.Msg("[SJS] field<" + tag + ">: " + nm + " = " + v);
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] fields<" + tag + "> fail: " + e.Message); }
        }

        /// <summary>A817 定向搜捕：① 贴图名含 octagon 的 Image ② 短数字 TMP（rect 宽 &lt; 160）③ rect≈59×61 的 Image。
        /// 每条都打**完整父链**（`Tr(t, 12)`），方便认它归谁。</summary>
        private void HuntBadge(string outDir)
        {
            string path = Path.Combine(outDir, "menu_hunt_badge.tsv");
            try
            {
                using (var w = new StreamWriter(path, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("path\tname\trect\tpos\tsprite\ttext\tcolor");
                    var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        var t = all[i];
                        if (t == null) continue;
                        UnityEngine.RectTransform rt = null;
                        try { rt = t.TryCast<UnityEngine.RectTransform>(); } catch { }
                        if (rt == null) { try { rt = t.GetComponent<UnityEngine.RectTransform>(); } catch { } }
                        float ww = rt != null ? rt.rect.width : 0f, hh = rt != null ? rt.rect.height : 0f;
                        string sprite = "", col = "";
                        UnityEngine.UI.Image img = null;
                        try { img = t.GetComponent<UnityEngine.UI.Image>(); } catch { }
                        if (img != null)
                        {
                            try { sprite = img.sprite != null ? img.sprite.name : ""; } catch { }
                            try { col = Fmt(img.color); } catch { }
                        }
                        string txt = "";
                        try { var tmp = t.GetComponent<Il2CppTMPro.TextMeshProUGUI>(); if (tmp != null) txt = tmp.m_text; } catch { }
                        bool hitOct = sprite.IndexOf("octagon", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool hitNum = txt != null && txt.Length > 0 && txt.Length <= 4 && txt.Trim().Length > 0
                                      && txt.Trim().TrimStart('-').Length > 0
                                      && long.TryParse(txt.Trim(), out long _) && ww > 0f && ww < 160f;
                        bool hitSize = ww > 45f && ww < 75f && hh > 45f && hh < 75f && sprite != "";
                        if (!(hitOct || hitNum || hitSize)) continue;
                        w.WriteLine(Tr(t, 12) + "\t" + Esc(t.name)
                                    + "\t" + ww.ToString("F2") + "x" + hh.ToString("F2")
                                    + "\t" + (rt != null ? (rt.position.x.ToString("F1") + "," + rt.position.y.ToString("F1")) : "?")
                                    + "\t" + sprite + "\t" + Esc(txt) + "\t" + col);
                    }
                }
                LoggerInstance.Msg("[SJS] hunt -> " + path + " (" + (new FileInfo(path)).Length + " bytes)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] hunt fail: " + e.Message); }
        }

        /// <summary>**精确名**点一颗节点（先 `Button.onClick`、再 `Toggle.isOn`）。</summary>
        private bool ClickNodeByNameExact(string name)
        {
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    if (all[i].name != name) continue;
                    var go = all[i].gameObject;
                    LoggerInstance.Msg("[SJS] clickExact: 命中 '" + name + "' path=" + Tr(all[i], 10) + " active=" + go.activeInHierarchy);
                    try
                    {
                        var b = go.GetComponent<UnityEngine.UI.Button>();
                        if (b != null && b.onClick != null) { b.onClick.Invoke(); LoggerInstance.Msg("[SJS] clickExact: Button.onClick.Invoke()"); return true; }
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] clickExact: Button 抛 " + e.Message); }
                    try
                    {
                        var tg = go.GetComponent<UnityEngine.UI.Toggle>();
                        if (tg != null) { tg.isOn = true; LoggerInstance.Msg("[SJS] clickExact: Toggle.isOn=true"); return true; }
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] clickExact: Toggle 抛 " + e.Message); }
                    try
                    {
                        var ev = go.GetComponent<Il2Cpp.EverguildToggle>();
                        if (ev != null)
                        {
                            var mi = FindMethodByName(ev.GetType(), "ToggleValueChange");
                            LoggerInstance.Msg("[SJS] clickExact: EverguildToggle 找到, ToggleValueChange = " + (mi != null));
                            if (mi != null) { mi.Invoke(ev, new object[] { true }); LoggerInstance.Msg("[SJS] clickExact: EverguildToggle.ToggleValueChange(true) 已调"); return true; }
                        }
                    }
                    catch (Exception e) { LoggerInstance.Msg("[SJS] clickExact: EverguildToggle 抛 " + e.Message); }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] clickExact<" + name + "> fail: " + e.Message); }
            return false;
        }

        /// <summary>把一个子树 dump 成表：path / name / activeSelf / activeInHierarchy / rect(数字) / pos / pivot / sprite / color / text。</summary>
        private void DumpSubtree(Transform root, string outPath)
        {
            try
            {
                int n = 0;
                using (var w = new StreamWriter(outPath, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("path\tname\tactiveSelf\tactiveInHierarchy\trect\tpos\tpivot\tsprite\tcolor\ttext");
                    DumpSubtreeNode(root, "", w, ref n);
                }
                LoggerInstance.Msg("[SJS] subtree -> " + outPath + " (" + (new FileInfo(outPath)).Length + " bytes, " + n + " 行)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] subtree fail: " + e.Message); }
        }

        private void DumpSubtreeNode(Transform t, string path, StreamWriter w, ref int n)
        {
            if (t == null || n > 4000) return;
            var go = t.gameObject;
            string here = path + "/" + go.name;
            string rect = "", pos = "", piv = "";
            UnityEngine.RectTransform rt = null;
            try { rt = t.TryCast<UnityEngine.RectTransform>(); } catch { }
            if (rt == null) { try { rt = t.GetComponent<UnityEngine.RectTransform>(); } catch { } }
            if (rt != null)
            {
                rect = rt.rect.width.ToString("F2") + "x" + rt.rect.height.ToString("F2");
                pos = rt.position.x.ToString("F1") + "," + rt.position.y.ToString("F1");
                piv = rt.pivot.x.ToString("F2") + "," + rt.pivot.y.ToString("F2");
            }
            string sprite = "", col = "", txt = "";
            UnityEngine.UI.Image img = null;
            try { img = t.GetComponent<UnityEngine.UI.Image>(); } catch { }
            if (img != null)
            {
                try { sprite = img.sprite != null ? img.sprite.name : "(null)"; } catch { }
                try { col = Fmt(img.color); } catch { }
            }
            try
            {
                var tmp = t.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                if (tmp != null) txt = tmp.m_text;
            }
            catch { }
            w.WriteLine(here + "\t" + Esc(go.name) + "\t" + go.activeSelf + "\t" + go.activeInHierarchy
                        + "\t" + rect + "\t" + pos + "\t" + piv + "\t" + sprite + "\t" + col + "\t" + Esc(txt));
            n++;
            for (int i = 0; i < t.childCount; i++) DumpSubtreeNode(t.GetChild(i), here, w, ref n);
        }

        /// <summary>把名字里含 <paramref name="needle"/> 的 Transform 全列出来（带路径 / 激活态 / 组件名）。</summary>
        private void LogNameCandidates(string needle, int limit)
        {
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                int n = 0;
                for (int i = 0; i < all.Length && n < limit; i++)
                {
                    if (all[i] == null) continue;
                    string nm = all[i].name;
                    if (nm == null || nm.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string comps = "";
                    try
                    {
                        var cs = all[i].gameObject.GetComponents<UnityEngine.Component>();
                        if (cs != null) for (int k = 0; k < cs.Length; k++)
                        { if (cs[k] == null) continue; string cn = ""; try { cn = cs[k].GetType().Name; } catch { } if (cn != "Transform" && cn != "RectTransform") comps += cn + " "; }
                    }
                    catch { }
                    LoggerInstance.Msg("[SJS] cand<" + needle + ">: '" + nm + "' path=" + Tr(all[i], 10)
                                       + " active=" + all[i].gameObject.activeInHierarchy + " comps=" + comps);
                    n++;
                }
                LoggerInstance.Msg("[SJS] cand<" + needle + ">: 命中 " + n + " 条");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] cand<" + needle + "> fail: " + e.Message); }
        }

        /// <summary>点一颗名字含 <paramref name="needle"/> 的节点：先试 <c>UnityEngine.UI.Button.onClick</c>，再试反射 <c>Toggle</c>。</summary>
        private bool ClickNodeByNameContains(string needle)
        {
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    string nm = all[i].name;
                    if (nm == null || nm.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var go = all[i].gameObject;
                    try
                    {
                        var b = go.GetComponent<UnityEngine.UI.Button>();
                        if (b != null && b.onClick != null) { b.onClick.Invoke(); LoggerInstance.Msg("[SJS] click: Button.onClick.Invoke() @ '" + nm + "'"); return true; }
                    }
                    catch { }
                    try
                    {
                        var tg = go.GetComponent<UnityEngine.UI.Toggle>();
                        if (tg != null) { tg.isOn = true; LoggerInstance.Msg("[SJS] click: Toggle.isOn=true @ '" + nm + "'"); return true; }
                    }
                    catch { }
                }
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] click<" + needle + "> fail: " + e.Message); }
            return false;
        }

        /// <summary>把场景里所有「带 sprite 的 Image」列成表（名字 + rect + sprite 名 + 激活态）—— A817 的正判据靠它。</summary>
        private void DumpImagesWithSprite(string outDir)
        {
            string path = Path.Combine(outDir, "menu_open_images.tsv");
            try
            {
                using (var w = new StreamWriter(path, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("path\tname\tactiveInHierarchy\trect\tpos\tsprite\tcolor");
                    var all = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] == null) continue;
                        UnityEngine.UI.Image img = null;
                        try { img = all[i].GetComponent<UnityEngine.UI.Image>(); } catch { }
                        if (img == null) continue;
                        string sp = ""; try { sp = img.sprite != null ? img.sprite.name : "(null)"; } catch { }
                        if (sp == "(null)" || sp == "") continue;          // A817 只关心有图的那批
                        var rt = all[i] as RectTransform;
                        if (rt == null) { try { rt = all[i].TryCast<RectTransform>(); } catch { } }
                        if (rt == null) { try { rt = all[i].GetComponent<RectTransform>(); } catch { } }
                        string rect = rt != null ? (rt.rect.width.ToString("F2") + "x" + rt.rect.height.ToString("F2")) : "?";
                        string pos = rt != null ? (rt.position.x.ToString("F1") + "," + rt.position.y.ToString("F1")) : "?";
                        string col = ""; try { col = Fmt(img.color); } catch { }
                        w.WriteLine(Tr(all[i], 10) + "\t" + Esc(all[i].name) + "\t" + all[i].gameObject.activeInHierarchy
                                    + "\t" + rect + "\t" + pos + "\t" + sp + "\t" + col);
                    }
                }
                LoggerInstance.Msg("[SJS] images -> " + path + " (" + (new FileInfo(path)).Length + " bytes)");
            }
            catch (Exception e) { LoggerInstance.Msg("[SJS] images fail: " + e.Message); }
        }
    }
}
