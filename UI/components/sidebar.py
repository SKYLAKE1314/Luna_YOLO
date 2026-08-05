"""
GoogleSidebarWidget — 左側 Google 導覽欄與效能監控儀表板
包含導覽頁面按鈕及 Task Manager 風格的 CPU、RAM、GPU、VRAM 即時折線圖
"""

from PySide6.QtWidgets import QWidget, QVBoxLayout, QLabel, QPushButton, QFrame, QGridLayout
from PySide6.QtCore import Signal
from components.perf_graph import TaskManagerMiniGraph


class GoogleSidebarWidget(QWidget):
    page_selected = Signal(int)

    def __init__(self, dark_mode=True, parent=None):
        super().__init__(parent)
        self.setObjectName("GoogleSidebar")
        self.setFixedWidth(240)
        self.dark_mode = dark_mode
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(12, 16, 12, 16)
        layout.setSpacing(6)

        self.nav_buttons = []
        # 導航圖標文件位置 (供替換): UI/icons/nav_home.png, nav_data.png, nav_train.png, nav_live.png, nav_infer.png, nav_export.png
        nav_items = [
            ("首頁", 0),
            ("資料與標註轉換", 1),
            ("模型與訓練設定", 2),
            ("即時訓練動態", 3),
            ("推理與目標追蹤", 4),
            ("模型導出與診斷", 5)
        ]

        for text, idx in nav_items:
            btn = QPushButton(text)
            btn.setObjectName("GoogleNavItem")
            btn.setCheckable(True)
            btn.clicked.connect(lambda _, i=idx: self._on_nav_btn_click(i))
            layout.addWidget(btn)
            self.nav_buttons.append(btn)

        self.nav_buttons[0].setChecked(True)
        layout.addStretch()

        # Task Manager Mini Graphs
        perf_card = QFrame()
        perf_card.setObjectName("GoogleCard")
        perf_card.setStyleSheet("QFrame#GoogleCard { padding: 6px; border-radius: 12px; }")
        
        p_layout = QVBoxLayout(perf_card)
        p_layout.setContentsMargins(6, 6, 6, 6)
        p_layout.setSpacing(4)

        p_title = QLabel("即時效能")
        p_title.setStyleSheet("font-size: 11px; font-weight: bold; margin-bottom: 2px;")
        p_layout.addWidget(p_title)

        grid = QGridLayout()
        grid.setContentsMargins(0, 0, 0, 0)
        grid.setSpacing(6)

        self.graph_cpu  = TaskManagerMiniGraph("CPU",  "#00BCD4", dark_mode=self.dark_mode)
        self.graph_ram  = TaskManagerMiniGraph("RAM",  "#AB47BC", dark_mode=self.dark_mode)
        self.graph_gpu  = TaskManagerMiniGraph("GPU",  "#76B900", dark_mode=self.dark_mode)
        self.graph_vram = TaskManagerMiniGraph("VRAM", "#FFB300", dark_mode=self.dark_mode)

        grid.addWidget(self.graph_cpu,  0, 0)
        grid.addWidget(self.graph_ram,  0, 1)
        grid.addWidget(self.graph_gpu,  1, 0)
        grid.addWidget(self.graph_vram, 1, 1)

        p_layout.addLayout(grid)
        layout.addWidget(perf_card)

    def _on_nav_btn_click(self, idx):
        self.set_active_page(idx)
        self.page_selected.emit(idx)

    def set_active_page(self, idx):
        for i, btn in enumerate(self.nav_buttons):
            btn.setChecked(i == idx)

    def switch_page(self, idx):
        self.set_active_page(idx)

    def set_dark_mode(self, dark_mode):
        self.dark_mode = dark_mode
        for g in [self.graph_cpu, self.graph_ram, self.graph_gpu, self.graph_vram]:
            g.set_dark_mode(dark_mode)
