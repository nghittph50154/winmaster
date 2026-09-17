import requests
import os

TARGET_DIR = r"d:\Profile\Visual Code File\Tool_download_for_Nghi\WinMaster\src\WinMaster\Assets\Icons"

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
}

# 1. Edge official logo
edge_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Microsoft_Edge_logo_%282019%29.svg/256px-Microsoft_Edge_logo_%282019%29.svg.png"
r = requests.get(edge_url, headers=headers)
if r.status_code == 200:
    with open(os.path.join(TARGET_DIR, "microsoft-edge.png"), "wb") as f:
        f.write(r.content)
    print("Edge icon updated.")

# 2. Microsoft logo for Office 365
ms_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/44/Microsoft_logo.svg/256px-Microsoft_logo.svg.png"
r = requests.get(ms_url, headers=headers)
if r.status_code == 200:
    with open(os.path.join(TARGET_DIR, "office365.png"), "wb") as f:
        f.write(r.content)
    print("Office 365 Microsoft logo updated.")

# 3. FxSound icon
fx_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/21/Speaker_Icon.svg/256px-Speaker_Icon.svg.png"
r = requests.get(fx_url, headers=headers)
if r.status_code == 200:
    with open(os.path.join(TARGET_DIR, "fxsound.png"), "wb") as f:
        f.write(r.content)
    print("FxSound icon updated.")

# 4. Mem Reduct icon
mem_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6c/Microprocessador.svg/256px-Microprocessador.svg.png"
r = requests.get(mem_url, headers=headers)
if r.status_code == 200:
    with open(os.path.join(TARGET_DIR, "memreduct.png"), "wb") as f:
        f.write(r.content)
    print("Mem Reduct icon updated.")

# 5. Drivers icons (Mouse / Keyboard)
mouse_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/22/Computer_mouse_icon.svg/256px-Computer_mouse_icon.svg.png"
kb_url = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Keyboard-icon_wikipedians.png/256px-Keyboard-icon_wikipedians.png"

for app_id, url in [
    ("yindiao-g17-drive", kb_url),
    ("inphic-drive", mouse_url),
    ("aula-f87-drive", kb_url)
]:
    r = requests.get(url, headers=headers)
    if r.status_code == 200:
        with open(os.path.join(TARGET_DIR, f"{app_id}.png"), "wb") as f:
            f.write(r.content)
        print(f"Driver icon updated: {app_id}")
