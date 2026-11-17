# Corregge le note delle versioni gia' pubblicate: la tabella dei download riportava i file della 1.0.10 (richiesta di IC8TEM del 17/11/2026 22:41).
import json, os, re, sys, urllib.request
repo = os.environ["GITHUB_REPOSITORY"]; tok = os.environ["GH_TOKEN"]
def req(url, data=None, method="GET"):
    r = urllib.request.Request("https://api.github.com/repos/" + repo + url, data=data, method=method,
        headers={"Authorization": "Bearer " + tok, "Accept": "application/vnd.github+json", "Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(r))
for r in req("/releases?per_page=100"):
    if r["draft"]: continue
    v = r["tag_name"][1:]; b = r["body"] or ""
    if "1.0.10-win-x64-setup" not in b or v == "1.0.10": continue
    nb = b
    for pat in ["YA-NEC-1.0.10-", "ya-nec_1.0.10_", "ya-nec-1.0.10-1", "download/v1.0.10/"]:
        nb = nb.replace(pat, pat.replace("1.0.10", v))
    nb = re.sub(r"^\*\*YA-NEC 1\.0\.1\d\*\*", "**YA-NEC %s**" % v, nb, count=1)
    if v == "1.1.0":
        nb = nb.replace("**Novita' 1.0.13:**", "**Novita' 1.1.0:** vedi [Novità della 1.1](https://github.com/ic8tem/YA-NEC/wiki/Novita-1.1).\n\n**Dalla 1.0.13:**", 1)
    if nb != b:
        req("/releases/%d" % r["id"], json.dumps({"body": nb}).encode(), "PATCH")
        print("corretta", r["tag_name"])
