import os
import shutil

USER_ICON_DIR = r"C:\Users\phong\Downloads\icon"
PROJECT_ICON_DIR = r"d:\Profile\Visual Code File\Tool_download_for_Nghi\WinMaster\src\WinMaster\Assets\Icons"

# Full mapping for all files in C:\Users\phong\Downloads\icon
FILE_MAPPING = {
    "AULA.png": "aula-f87-drive.png",
    "Autoruns.png": "autoruns.png",
    "chatgpt-icon.png": "chatgpt-desktop.png",
    "codex-icon.png": "codex.png",
    "egde.png": "microsoft-edge.png",
    "FxSound.png": "fxsound.png",
    "Genshin.png": "genshin-impact.png",
    "GitHub.png": "github-desktop.png",
    "google-antigravity-icon.png": "antigravity.png",
    "IDM.png": "idm.png",
    "inphic drive.png": "inphic-drive.png",
    "Mem Reduct.webp": "memreduct.webp",
    "Minitool partition wizard.png": "minitool-partition.png",
    "NodeJS.png": "nodejs.png",
    "office-365-icon.png": "office365.png",
    "RevoltG.png": "revoltg.png",
    "Riot.png": "riot-client.png",
    "telegram.png": "telegram.png",
    "Tor_Browser.png": "tor-browser.png",
    "VMware.png": "vmware.png",
    "WinRAR.png": "winrar.png",
    "XMCL.png": "xmcl.png",
    "YINDIAO-G17.ico": "yindiao-g17-drive.ico"
}

os.makedirs(PROJECT_ICON_DIR, exist_ok=True)

print(f"Syncing all icons from {USER_ICON_DIR} to {PROJECT_ICON_DIR}...")

for src_name, target_name in FILE_MAPPING.items():
    src_path = os.path.join(USER_ICON_DIR, src_name)
    target_path = os.path.join(PROJECT_ICON_DIR, target_name)
    
    if os.path.exists(src_path):
        base_id = target_name.split('.')[0]
        # Clean existing files with different extensions for this app ID
        for old_ext in ['.png', '.ico', '.jpg', '.svg', '.webp']:
            old_file = os.path.join(PROJECT_ICON_DIR, f"{base_id}{old_ext}")
            if os.path.exists(old_file) and old_file != target_path:
                os.remove(old_file)
                
        shutil.copy(src_path, target_path)
        print(f"[SYNCED] {src_name} -> {target_name}")
    else:
        print(f"[NOT FOUND] {src_name}")

print("Completed icon synchronization.")
