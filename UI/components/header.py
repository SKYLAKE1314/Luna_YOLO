"""
GoogleHeaderWidget — 頂部導覽與系統設定列
包含品牌標題、主題模式切換 (系統/淺色/深色)、說明對話框與算力裝置切換選單
"""

import os
from PySide6.QtWidgets import QWidget, QHBoxLayout, QLabel, QPushButton, QMenu
from PySide6.QtCore import QSize, Signal
from PySide6.QtGui import QIcon, QAction
from components.env_dialog import show_environment_dialog


class GoogleHeaderWidget(QWidget):
    theme_changed = Signal(str)         # 發送主題切換請求
    compute_mode_changed = Signal(str, str, str) # title, device_val, icon_name
    switch_page_requested = Signal(int) # 頁面切換請求

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setObjectName("GoogleHeader")
        self.setFixedHeight(60)
        self.init_ui()

    def init_ui(self):
        layout = QHBoxLayout(self)
        layout.setContentsMargins(24, 0, 24, 0)

        # Logo Text
        logo_box = QHBoxLayout()
        logo_box.setSpacing(6)
        logo_title = QLabel("Nya")
        logo_title.setObjectName("GoogleLogoText")
        logo_sub = QLabel("YOLO Studio")
        logo_sub.setObjectName("GoogleLogoSubtext")
        logo_box.addWidget(logo_title)
        logo_box.addWidget(logo_sub)

        # 主題按鈕 (預設跟隨系統)
        self.btn_theme = QPushButton("⚙ 跟隨系統 (預設)")
        self.btn_theme.setObjectName("GoogleHeaderBtn")
        self.btn_theme.clicked.connect(self._on_theme_clicked)

        btn_help = QPushButton("❓ 說明")
        btn_help.setObjectName("GoogleHeaderBtn")
        btn_help.clicked.connect(self._show_help_dialog)

        btn_grid = QPushButton("⣿ 服務")
        btn_grid.setObjectName("GoogleHeaderBtn")
        btn_grid.clicked.connect(self._show_services_menu)

        # Compute Mode Button
        ICON_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "icons")
        def _icon(name):
            p = os.path.join(ICON_DIR, f"{name}.png")
            return QIcon(p) if os.path.exists(p) else QIcon()

        self.avatar_btn = QPushButton(" CPU")
        self.avatar_btn.setObjectName("GoogleHeaderBtn")
        self.avatar_btn.setIcon(_icon("cpu"))
        self.avatar_btn.setIconSize(QSize(18, 18))

        mode_menu = QMenu(self)
        mode_menu.setStyleSheet("""
            QMenu {
                background-color: #1E1F20;
                color: #E3E3E3;
                border: 1px solid #333537;
                border-radius: 8px;
                padding: 4px;
            }
            QMenu::item {
                padding: 8px 28px 8px 8px;
                border-radius: 4px;
            }
            QMenu::item:selected {
                background-color: #282A2C;
                color: #A8C7FA;
            }
        """)

        modes = [
            ("CUDA  (NVIDIA GPU)", "0",   "nvidia"),
            ("CPU   Mode",         "cpu", "cpu"),
            ("OpenVINO  (Intel)",  "cpu", "openvino"),
            ("TensorFlow",         "cpu", "tf"),
            ("MPS   (Apple)",      "mps", "apple"),
        ]

        for label, dev_val, icon_name in modes:
            action = QAction(_icon(icon_name), label, self)
            action.triggered.connect(lambda checked, t=label, d=dev_val, ic=icon_name: self.compute_mode_changed.emit(t, d, ic))
            mode_menu.addAction(action)

        mode_menu.addSeparator()
        diag_action = QAction("🔍 硬體診斷面板", self)
        diag_action.triggered.connect(lambda: self.switch_page_requested.emit(5))
        mode_menu.addAction(diag_action)

        env_diag_action = QAction("🛡️ 環境與依賴檢測", self)
        env_diag_action.triggered.connect(lambda: show_environment_dialog(self.window(), auto_on_startup=False))
        mode_menu.addAction(env_diag_action)

        self.avatar_btn.setMenu(mode_menu)
        self._icon_dir = ICON_DIR

        layout.addLayout(logo_box)
        layout.addStretch()
        layout.addWidget(self.btn_theme)
        layout.addWidget(btn_help)
        layout.addWidget(btn_grid)
        layout.addWidget(self.avatar_btn)

    def _on_theme_clicked(self):
        self.theme_changed.emit("cycle")

    def _show_help_dialog(self):
        from PySide6.QtWidgets import QMessageBox
        QMessageBox.information(
            self, "使用說明",
            "✨ 歡迎使用 Nya YOLO Studio！\n\n"
            "本軟體支援 YOLOv8, YOLOv9, YOLOv10, YOLO11, YOLO12 等全系列架構訓練、開放詞彙 (World Detection) 零樣本檢測、專用文字區域標籤檢測與 Model Export 工具。\n"
            "如需協助請參閱系統環境診斷或說明文件。"
        )

    def _show_services_menu(self):
        from PySide6.QtWidgets import QMessageBox
        QMessageBox.information(self, "服務功能", "🌐 相關服務：DataPrep 格式轉換、Model Trainer、World Zero-Shot 測試、Export Deploy。")
