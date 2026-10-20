#!/usr/bin/env python
# W58-K12 step 4: data-flow scan WITH class inheritance.
# Finds any read *(int*)(<expr>+0x40) where <expr> is known/derived to be a RawCardScript,
# where <expr> is (a) a RawCardScript-typed param, (b) `this` of a method typed RawCardScript,
# (c) local loaded from a RawCardScript-typed field (incl. inherited), (d) local from a
# RawCardScript-returning getter.
import io, os, re, collections

DUMP = r"d:/2/tools/il2cpp_out/dump.cs"
DEC  = r"d:/2/tools/decomp_full"
def read(p): return io.open(p, "r", encoding="utf-8", errors="replace").read().splitlines()

lines = read(DUMP)
cls_re = re.compile(r'^(?:public |private |internal |protected )?(?:static |sealed |abstract )*(?:class|struct|interface|enum)\s+([A-Za-z0-9_\.<>`]+)\s*(?::\s*([A-Za-z0-9_\.<>`]+))?')
field_re = re.compile(r'^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?RawCardScript\s+([A-Za-z0-9_<>]+)\s*;\s*//\s*0x([0-9A-Fa-f]+)')

class_base = {}     # class -> base
class_fields = collections.defaultdict(dict)  # class -> {offset: name}
methods = []        # (class, name, sig, line)
cur = None
for i, ln in enumerate(lines, 1):
    m = cls_re.match(ln)
    if m:
        cur = m.group(1)
        class_base[cur] = m.group(2) or ""
        continue
    if not cur: continue
    fm = field_re.search(ln)
    if fm:
        class_fields[cur][int(fm.group(2), 16)] = fm.group(1); continue
    s = ln.strip()
    if s.endswith("{ }") and "(" in s and not s.startswith("//"):
        nm = re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
        if nm: methods.append((cur, nm.group(1), s, i))

def resolve(c):
    seen=set()
    while c and c not in seen:
        seen.add(c)
        c = class_base.get(c, "")
    return c

# inherited raw fields
def raw_fields_of(c):
    out={}
    seen=set(); x=c
    while x and x not in seen:
        seen.add(x)
        out.update(class_fields.get(x, {}))
        x=class_base.get(x, "")
    return out

# methods with RawCardScript in signature OR RawCardScript instance methods
sig_methods=[m for m in methods if "RawCardScript" in m[2] or resolve(m[0])=="RawCardScript"]
# raw-returning getters
ret_raw=set()
for c,n,s,i in methods:
    nm=re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
    if nm:
        head=s[:nm.start()]
        if "RawCardScript" in head:
            ret_raw.add(n)

print("methods:",len(methods),"signature/self raw methods:",len(sig_methods),"raw-returning methods:",len(ret_raw))
print("classes deriving from RawCardScript:",[c for c in class_base if resolve(c)=="RawCardScript"])

files=set(os.listdir(DEC))
def mangle(x): return x.replace("<","_").replace(">","_").replace("+",".")
def cands(cls,name):
    c=mangle(cls); n=mangle(name)
    return [x for x in (c+"__"+n+".c", c.replace(".","__")+"__"+n+".c") if x in files]

pat40=re.compile(r'\+\s*0x40\b')
ld=re.compile(r'^\s*(\w+)\s*=\s*(.+)$')
hits=0; mapped=0
for cls,name,sig,ln in sig_methods:
    cf=cands(cls,name)
    if cf: mapped+=1
    offs=raw_fields_of(cls)
    for fn in cf:
        body=read(os.path.join(DEC,fn))
        rawloc=set()
        # self is RawCardScript?
        if resolve(cls)=="RawCardScript":
            rawloc.add("param_1")
        for j,bl in enumerate(body,1):
            if offs:
                for off in offs:
                    for form in ("param_1 + 0x%x"%off,"param_1 + 0x%X"%off,"param_1 + %d"%off):
                        if form in bl:
                            lm=ld.match(bl)
                            if lm: rawloc.add(lm.group(1))
                            break
            for g in ret_raw:
                if g+"(" in bl:
                    lm=ld.match(bl)
                    if lm: rawloc.add(lm.group(1))
            if pat40.search(bl):
                m2=re.search(r'\*\s*\(\s*(?:int|uint|longlong|undefined8|undefined4|char|undefined1)\s*\*\s*\)\s*\(\s*(\w+)\s*\+\s*0x40', bl)
                if m2 and m2.group(1) in rawloc:
                    hits+=1
                    print("HIT %s:%d  %s\n    sig(dump.cs:%d): %s"%(fn,j,bl.strip(),ln,sig))
print("mapped method files:",mapped,"/",len(sig_methods))
print("TOTAL genuine-looking RawCardScript+0x40 reads:",hits)
