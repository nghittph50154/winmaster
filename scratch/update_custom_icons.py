import os
import shutil
import requests

USER_UPLOADED_DIR = r"C:\Users\phong\.gemini\antigravity-ide\brain\0877b9c6-6029-4868-87cb-38b687873a7a\.user_uploaded"
TARGET_DIR = r"d:\Profile\Visual Code File\Tool_download_for_Nghi\WinMaster\src\WinMaster\Assets\Icons"

# 1. Copy user uploaded images directly
USER_MAPPINGS = {
    "xdm.png": "media_1789637260118.png",
    "autoruns.png": "media_1789637285121.png",
    "genshin-impact.png": "media_1789637353129.png",
    "revoltg.png": "media_1789637460083.png"
}

for icon_name, media_name in USER_MAPPINGS.items():
    src = os.path.join(USER_UPLOADED_DIR, media_name)
    dst = os.path.join(TARGET_DIR, icon_name)
    if os.path.exists(src):
        shutil.copy(src, dst)
        print(f"[COPIED USER IMAGE] {icon_name}")

# 2. Download missing & requested corrected icons
URL_UPDATES = {
    "microsoft-edge": "https://upload.wikimedia.org/wikipedia/commons/9/98/Microsoft_Edge_logo_%282019%29.png",
    "office365": "https://upload.wikimedia.org/wikipedia/commons/4/44/Microsoft_logo.svg",
    "fxsound": "https://www.fxsound.com/assets/images/logo.png",
    "memreduct": "https://raw.githubusercontent.com/henrypp/memreduct/master/res/main.ico",
    "yindiao-g17-drive": "https://www.google.com/s2/favicons?domain=yindiao.com&sz=128",
    "inphic-drive": "https://www.google.com/s2/favicons?domain=inphic.cn&sz=128",
    "aula-f87-drive": "https://www.google.com/s2/favicons?domain=aulacn.com&sz=128"
}

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
}

# Edge official logo png fallback
edge_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Microsoft_Edge_logo_%282019%29.svg/256px-Microsoft_Edge_logo_%282019%29.svg.png"
ms_logo_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/44/Microsoft_logo.svg/256px-Microsoft_logo.svg.png"
fxsound_url = "https://www.fxsound.com/assets/images/fxsound-logo.png"

for app_id, url in [
    ("microsoft-edge", edge_url),
    ("office365", ms_logo_url),
    ("fxsound", fxsound_url)
]:
    try:
        r = requests.get(url, headers=headers, timeout=10)
        if r.status_code == 200:
            with open(os.path.join(TARGET_DIR, f"{app_id}.png"), "wb") as f:
                f.write(r.content)
            print(f"[DOWNLOADED OFFICIAL] {app_id}.png")
    except Exception as e:
        print(f"[ERR] {app_id}: {e}")

# Generate simple clean icon placeholder PNGs for mouse driver & memreduct if missing
print("Finished updating icons.")
