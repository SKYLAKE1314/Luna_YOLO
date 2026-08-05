"""
Nya YOLO Studio — PyInstaller 打包指令生成與執行腳本
用於將 Nya YOLO Studio 打包為獨立的 Windows .exe 可執行檔
"""

import os
import sys
import subprocess

def main():
    root_dir = os.path.dirname(os.path.abspath(__file__))
    ui_dir = os.path.join(root_dir, "UI")
    main_script = os.path.join(ui_dir, "NyaYOLOStudio.py")
    icon_path = os.path.join(ui_dir, "icon.ico")
    wallpaper_path = os.path.join(ui_dir, "file_0000000031e8720681bd49398eace5bf.png")

    cmd = [
        "pyinstaller",
        "--name=NyaYOLOStudio",
        "--onedir",             # 打包為資料夾模式（解壓更快且運行穩定）
        "--windowed",           # 不顯示 CMD 主黑框
        "--noconfirm",
        "--clean",
        f"--icon={icon_path}" if os.path.exists(icon_path) else "",
        f"--add-data={icon_path};UI" if os.path.exists(icon_path) else "",
        f"--add-data={wallpaper_path};UI" if os.path.exists(wallpaper_path) else "",
        "--hidden-import=PySide6.QtCore",
        "--hidden-import=PySide6.QtGui",
        "--hidden-import=PySide6.QtWidgets",
        "--hidden-import=ultralytics",
        "--hidden-import=cv2",
        "--hidden-import=pyqtgraph",
        "--hidden-import=torch",
        main_script
    ]

    # 過濾空字串
    cmd = [c for c in cmd if c]

    print("🚀 開始執行 PyInstaller 打包程序...")
    print("執行的指令:", " ".join(cmd))

    try:
        subprocess.run(cmd, check=True, cwd=root_dir)
        print("\n✅ 打包完成！生成的可執行檔部位於 dist/NyaYOLOStudio/NyaYOLOStudio.exe")
    except Exception as e:
        print(f"\n❌ 打包過程發生錯誤: {e}")
        print("提示: 請先確保已安裝 PyInstaller: pip install pyinstaller")

if __name__ == "__main__":
    main()
