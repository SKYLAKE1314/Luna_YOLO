import os
import sys
import shutil
import cv2
import pyqtgraph as pg
from PySide6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
    QTextEdit, QLineEdit, QLabel, QProgressBar, QFileDialog, QFormLayout,
    QStackedWidget, QCheckBox, QComboBox, QSpinBox, QDoubleSpinBox, QGroupBox,
    QTabWidget, QFrame, QScrollArea, QSplitter, QGridLayout, QListView,
    QMessageBox, QDialog, QCompleter, QMenu
)
import psutil
from collections import deque
from PySide6.QtCore import Qt, QSize, QStringListModel, QTimer
from PySide6.QtGui import (
    QIcon, QImage, QPixmap, QFont, QAction,
    QPainter, QColor, QPen, QBrush, QPainterPath, QLinearGradient
)

# 讓 UI 目錄下執行時也能找到同級與上層模組
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
PARENT_DIR = os.path.abspath(os.path.join(CURRENT_DIR, ".."))
if CURRENT_DIR not in sys.path:
    sys.path.insert(0, CURRENT_DIR)
if PARENT_DIR not in sys.path:
    sys.path.insert(0, PARENT_DIR)

# 圖標路徑
ICON_PATH = os.path.join(CURRENT_DIR, "icon.ico")

from styles import GoogleAccountTheme
from workers import (
    ConvertWorker, DataCheckWorker, TrainWorker,
    InferenceWorker, ExportWorker, CudaCheckWorker
)


def detect_system_dark_mode():
    """檢測 Windows 系統主題 (AppsUseLightTheme)"""
    try:
        import winreg
        key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
        )
        value, _ = winreg.QueryValueEx(key, "AppsUseLightTheme")
        winreg.CloseKey(key)
        return value == 0  # 0 表示深色主題，1 表示淺色主題
    except Exception:
        return False


# =========================================================
# 任務管理器風格 (Task Manager) 即時效能 Mini 折線圖元件
# =========================================================
class TaskManagerMiniGraph(QWidget):
    def __init__(self, title, line_color_hex, dark_mode=True, parent=None):
        super().__init__(parent)
        self.title_str = title
        self.line_color = QColor(line_color_hex)
        self.dark_mode = dark_mode
        self.history = deque([0.0] * 30, maxlen=30)
        self.current_val_str = "0%"
        self.setMinimumHeight(62)
        
    def add_data(self, val_pct, text_val=None):
        self.history.append(float(val_pct))
        self.current_val_str = text_val if text_val is not None else f"{int(val_pct)}%"
        self.update()

    def set_dark_mode(self, dark_mode):
        self.dark_mode = dark_mode
        self.update()

    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing)

        w, h = self.width(), self.height()
        
        # 1. 繪製 Task Manager 背景邊框
        bg_col = QColor(20, 15, 38, 150) if self.dark_mode else QColor(255, 255, 255, 190)
        border_col = QColor(120, 90, 200, 70) if self.dark_mode else QColor(180, 150, 240, 90)
        painter.setBrush(QBrush(bg_col))
        painter.setPen(QPen(border_col, 1))
        painter.drawRoundedRect(0, 0, w - 1, h - 1, 8, 8)

        # 2. 文字 (標題 + 數值)
        txt_col = QColor("#EDE8FF") if self.dark_mode else QColor("#1C1B1F")
        sub_col = QColor("#C4A8FA") if self.dark_mode else QColor("#6750A4")
        
        painter.setPen(txt_col)
        font = painter.font()
        font.setPixelSize(11)
        font.setBold(True)
        painter.setFont(font)
        painter.drawText(6, 14, self.title_str)

        font.setPixelSize(10)
        font.setBold(False)
        painter.setFont(font)
        painter.setPen(sub_col)
        painter.drawText(w - 75, 2, 70, 15, Qt.AlignRight | Qt.AlignVCenter, self.current_val_str)

        # 3. 任務管理器網格
        graph_x = 6
        graph_y = 18
        graph_w = w - 12
        graph_h = h - 22

        grid_pen = QPen(QColor(255, 255, 255, 25) if self.dark_mode else QColor(0, 0, 0, 20), 1, Qt.DashLine)
        painter.setPen(grid_pen)
        painter.drawLine(graph_x, graph_y + graph_h // 2, graph_x + graph_w, graph_y + graph_h // 2)
        for i in range(1, 4):
            gx = graph_x + (graph_w * i // 4)
            painter.drawLine(gx, graph_y, gx, graph_y + graph_h)

        # 4. 折線與漸層滿色
        if not self.history:
            return

        points = []
        n = len(self.history)
        step_x = graph_w / max(1, n - 1)

        for i, val in enumerate(self.history):
            px = graph_x + i * step_x
            val_norm = max(0.0, min(100.0, val))
            py = graph_y + graph_h - (val_norm / 100.0 * graph_h)
            points.append((px, py))

        path = QPainterPath()
        path.moveTo(points[0][0], points[0][1])
        for px, py in points[1:]:
            path.lineTo(px, py)

        fill_path = QPainterPath(path)
        fill_path.lineTo(points[-1][0], graph_y + graph_h)
        fill_path.lineTo(points[0][0], graph_y + graph_h)
        fill_path.closeSubpath()

        grad = QLinearGradient(0, graph_y, 0, graph_y + graph_h)
        c_top = QColor(self.line_color)
        c_top.setAlpha(70)
        c_bot = QColor(self.line_color)
        c_bot.setAlpha(5)
        grad.setColorAt(0.0, c_top)
        grad.setColorAt(1.0, c_bot)

        painter.setPen(Qt.NoPen)
        painter.setBrush(QBrush(grad))
        painter.drawPath(fill_path)

        line_pen = QPen(self.line_color, 1.8)
        painter.setPen(line_pen)
        painter.setBrush(Qt.NoBrush)
        painter.drawPath(path)



class NyaUI(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Nya YOLO Studio")
        self.resize(1340, 850)

        # 壁紙: 啟用透明背景
        self.setAttribute(Qt.WA_TranslucentBackground, True)

        # 載入壁紙圖片
        _wp_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "file_0000000031e8720681bd49398eace5bf.png")
        self._wallpaper = QPixmap(_wp_path) if os.path.exists(_wp_path) else QPixmap()

        # 🔴 設置視窗與應用程式圖標
        if os.path.exists(ICON_PATH):
            self.setWindowIcon(QIcon(ICON_PATH))

        # 🔴 預設主題模式: "system" -> "light" -> "dark"
        self.theme_mode = "system"
        self.dark_mode = detect_system_dark_mode()

        # 線程控制
        self.train_worker = None
        self.infer_worker = None
        self.convert_worker = None
        self.datacheck_worker = None
        self.export_worker = None

        # pyqtgraph 主題
        pg.setConfigOption('background', 'transparent')
        pg.setConfigOption('foreground', '#E3E3E3')

        self.init_ui()
        self.apply_theme()
        self.check_cuda_status()
        self.init_perf_monitor()
        self.showMaximized()

    def init_perf_monitor(self):
        """初始化 NVML 與 psutil 即時效能監控"""
        self.nvml_handle = None
        try:
            import pynvml
            pynvml.nvmlInit()
            if pynvml.nvmlDeviceGetCount() > 0:
                self.nvml_handle = pynvml.nvmlDeviceGetHandleByIndex(0)
        except Exception:
            self.nvml_handle = None

        self.perf_timer = QTimer(self)
        self.perf_timer.timeout.connect(self.update_perf_stats)
        self.perf_timer.start(1000)

    def update_perf_stats(self):
        """抓取 CPU, RAM, GPU, VRAM 數據並更新 Mini 折線圖"""
        if not hasattr(self, 'graph_cpu'):
            return

        # 1. CPU
        try:
            cpu_pct = psutil.cpu_percent(interval=None)
        except Exception:
            cpu_pct = 0.0

        # 2. RAM
        try:
            mem = psutil.virtual_memory()
            ram_pct = mem.percent
            ram_used_g = mem.used / (1024 ** 3)
            ram_total_g = mem.total / (1024 ** 3)
            ram_text = f"{ram_used_g:.1f}/{ram_total_g:.0f}G"
        except Exception:
            ram_pct = 0.0
            ram_text = "N/A"

        # 3. GPU & VRAM
        gpu_pct = 0.0
        vram_pct = 0.0
        vram_text = "N/A"

        if self.nvml_handle is not None:
            try:
                import pynvml
                util = pynvml.nvmlDeviceGetUtilizationRates(self.nvml_handle)
                mem_info = pynvml.nvmlDeviceGetMemoryInfo(self.nvml_handle)
                gpu_pct = float(util.gpu)
                vram_pct = float((mem_info.used / mem_info.total) * 100)
                v_used_g = mem_info.used / (1024 ** 3)
                v_total_g = mem_info.total / (1024 ** 3)
                vram_text = f"{v_used_g:.1f}/{v_total_g:.0f}G"
            except Exception:
                pass
        elif torch.cuda.is_available():
            try:
                allocated = torch.cuda.memory_allocated(0)
                total = torch.cuda.get_device_properties(0).total_memory
                vram_pct = (allocated / total) * 100
                vram_text = f"{allocated / (1024**3):.1f}/{total / (1024**3):.0f}G"
            except Exception:
                pass

        self.graph_cpu.add_data(cpu_pct, f"{int(cpu_pct)}%")
        self.graph_ram.add_data(ram_pct, ram_text)
        self.graph_gpu.add_data(gpu_pct, f"{int(gpu_pct)}%")
        self.graph_vram.add_data(vram_pct, vram_text)

    def paintEvent(self, event):
        """繪製壁紙背景 + 半透明遮罩 (依主題切換深/淺背景，確保最高對比度)"""
        from PySide6.QtGui import QPainter, QColor
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.SmoothPixmapTransform)

        if self.dark_mode:
            # 深色模式：深黑紫底色 + 28% 壁紙
            painter.fillRect(self.rect(), QColor("#0D0D10"))
            if not self._wallpaper.isNull():
                scaled = self._wallpaper.scaled(
                    self.size(),
                    Qt.AspectRatioMode.IgnoreAspectRatio,
                    Qt.TransformationMode.SmoothTransformation
                )
                painter.setOpacity(0.28)
                painter.drawPixmap(0, 0, scaled)
                painter.setOpacity(1.0)
        else:
            # 淺色模式：明亮極簡奶白/淺紫底色 (#F4F0FA) + 8% 質感水印壁紙
            painter.fillRect(self.rect(), QColor("#F4F0FA"))
            if not self._wallpaper.isNull():
                scaled = self._wallpaper.scaled(
                    self.size(),
                    Qt.AspectRatioMode.IgnoreAspectRatio,
                    Qt.TransformationMode.SmoothTransformation
                )
                painter.setOpacity(0.25)
                painter.drawPixmap(0, 0, scaled)
                painter.setOpacity(1.0)
            # 再疊加柔和白光遮罩，確保全介面文字高對比度
            painter.fillRect(self.rect(), QColor(244, 240, 250, 160))

        painter.end()
        super().paintEvent(event)

    def _setup_combo_view(self, combo):
        combo.setView(QListView())

    # =========================================================
    # UI 主架構
    # =========================================================
    def init_ui(self):
        main_widget = QWidget()
        main_widget.setObjectName("MainContainer")
        # 讓中心 widget 透明，壁紙從 QMainWindow.paintEvent 顯示
        main_widget.setAttribute(Qt.WA_TranslucentBackground, True)
        self.setCentralWidget(main_widget)

        root_layout = QVBoxLayout(main_widget)
        root_layout.setContentsMargins(0, 0, 0, 0)
        root_layout.setSpacing(0)

        # 1. 頂部 Google Header 欄
        root_layout.addWidget(self.create_google_header())

        # 2. 中間 (左側 Google 導覽欄 + 右側主內容頁面)
        body_layout = QHBoxLayout()
        body_layout.setContentsMargins(0, 0, 0, 0)
        body_layout.setSpacing(0)

        body_layout.addWidget(self.create_google_sidebar(), 0)

        self.stack = QStackedWidget()
        self.stack.addWidget(self.create_page_home())       # 首頁
        self.stack.addWidget(self.create_page_dataprep())   # 資料轉換與驗證
        self.stack.addWidget(self.create_page_train_config())# 模型訓練與超參數
        self.stack.addWidget(self.create_page_live_train())  # 即時訓練動態
        self.stack.addWidget(self.create_page_inference())   # 推理與目標追蹤
        self.stack.addWidget(self.create_page_export_tools())# 模型導出與診斷

        body_layout.addWidget(self.stack, 1)
        root_layout.addLayout(body_layout)

    # =========================================================
    # Google Top Header (跟隨系統預設)
    # =========================================================
    def create_google_header(self):
        header = QWidget()
        header.setObjectName("GoogleHeader")
        header.setFixedHeight(60)

        layout = QHBoxLayout(header)
        layout.setContentsMargins(24, 0, 24, 0)

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
        self.btn_theme.clicked.connect(self.cycle_theme_mode)

        btn_help = QPushButton("❓ 說明")
        btn_help.setObjectName("GoogleHeaderBtn")
        btn_help.clicked.connect(self.show_help_dialog)

        btn_grid = QPushButton("⣿ 服務")
        btn_grid.setObjectName("GoogleHeaderBtn")
        btn_grid.clicked.connect(self.show_services_menu)

        # --- Compute Mode Button ---
        ICON_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "icons")
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
            action.triggered.connect(lambda checked, t=label, d=dev_val, ic=icon_name: self.set_compute_mode(t, d, ic))
            mode_menu.addAction(action)

        mode_menu.addSeparator()
        diag_action = QAction("🔍 硬體診斷面板", self)
        diag_action.triggered.connect(lambda: self.switch_page(5))
        mode_menu.addAction(diag_action)

        self.avatar_btn.setMenu(mode_menu)
        self._icon_dir = ICON_DIR  # 儲存供 set_compute_mode 使用

        layout.addLayout(logo_box)
        layout.addStretch()
        layout.addWidget(self.btn_theme)
        layout.addWidget(btn_help)
        layout.addWidget(btn_grid)
        layout.addWidget(self.avatar_btn)

        return header

    def set_compute_mode(self, text_label, device_val, icon_name="cpu"):
        short = text_label.split()[0]
        self.avatar_btn.setText(f" {short}")
        p = os.path.join(getattr(self, '_icon_dir', ''), f"{icon_name}.png")
        if os.path.exists(p):
            self.avatar_btn.setIcon(QIcon(p))
            self.avatar_btn.setIconSize(QSize(18, 18))
        # 同步更新訓練和推理的 Device 欄位
        if hasattr(self, 'device_input'):
            self.device_input.setText(device_val)
        if hasattr(self, 'append_log'):
            self.append_log(f"⚙️ 系統運行模式已切換為: {text_label}")

    def cycle_theme_mode(self):
        """三態切換：跟隨系統 (system) -> 暖光護眼 (light) -> 暖暗護眼 (dark)"""
        if self.theme_mode == "system":
            self.theme_mode = "light"
        elif self.theme_mode == "light":
            self.theme_mode = "dark"
        else:
            self.theme_mode = "system"

        self.apply_theme()

    def toggle_theme(self):
        """別名相容方法"""
        self.cycle_theme_mode()

    def apply_theme(self):
        if self.theme_mode == "system":
            self.dark_mode = detect_system_dark_mode()
            sys_str = " (深色)" if self.dark_mode else " (淺色)"
            self.btn_theme.setText(f"⚙ 跟隨系統{sys_str}")
        elif self.theme_mode == "light":
            self.dark_mode = False
            self.btn_theme.setText("☀ 暖光護眼模式")
        else: # dark
            self.dark_mode = True
            self.btn_theme.setText("🌙 暖暗護眼模式")

        qss = GoogleAccountTheme.get_style(self.dark_mode)
        self.setStyleSheet(qss)

        # pyqtgraph 使用透明背景
        pg.setConfigOption('background', 'transparent')
        fg_color = GoogleAccountTheme.DARK_TEXT_PRIMARY if self.dark_mode else GoogleAccountTheme.LIGHT_TEXT_PRIMARY
        pg.setConfigOption('foreground', fg_color)
        
        # 更新圖表文字、座標軸與圖例對比度
        self.update_plot_styles()

        # 同步更新左側導覽欄即時效能圖表主題模式
        if hasattr(self, 'graph_cpu'):
            for g in [self.graph_cpu, self.graph_ram, self.graph_gpu, self.graph_vram]:
                g.set_dark_mode(self.dark_mode)

        # 重繪壁紙遮罩
        self.update()

    def update_plot_styles(self):
        """根據深/淺模式動態更新 pyqtgraph 圖表的標題、座標軸、圖例文字與筆劃對比度"""
        if not hasattr(self, 'plot_loss') or not hasattr(self, 'plot_map'):
            return

        txt_col = "#EDE8FF" if self.dark_mode else "#1C1B1F"
        sub_col = "#B8AEDD" if self.dark_mode else "#49454F"
        leg_bg = (20, 15, 38, 180) if self.dark_mode else (255, 255, 255, 220)

        if self.dark_mode:
            pen_box = pg.mkPen('#FFA726', width=2.5)
            pen_cls = pg.mkPen('#42A5F5', width=2.5)
            pen_dfl = pg.mkPen('#66BB6A', width=2.5)
            pen_m50 = pg.mkPen('#FFCA28', width=2.5)
            pen_m95 = pg.mkPen('#AB47BC', width=2.5)
        else:
            pen_box = pg.mkPen('#D97706', width=2.5)
            pen_cls = pg.mkPen('#1565C0', width=2.5)
            pen_dfl = pg.mkPen('#2E7D32', width=2.5)
            pen_m50 = pg.mkPen('#B45309', width=2.5)
            pen_m95 = pg.mkPen('#6B21A8', width=2.5)

        self.curve_box.setPen(pen_box)
        self.curve_cls.setPen(pen_cls)
        self.curve_dfl.setPen(pen_dfl)
        self.curve_map50.setPen(pen_m50)
        self.curve_map95.setPen(pen_m95)

        plots = [(self.plot_loss, "Loss 訓練損失動態"), (self.plot_map, "mAP 驗證精度動態")]
        for p, title_str in plots:
            p.setBackground('transparent')
            p.setTitle(f"<span style='color: {txt_col}; font-size: 14px; font-weight: bold;'>{title_str}</span>")
            
            for ax_name in ['left', 'bottom']:
                ax = p.getPlotItem().getAxis(ax_name)
                ax.setPen(pg.mkPen(sub_col, width=1))
                ax.setTextPen(pg.mkPen(txt_col))
                ax.setLabel(color=txt_col)
            
            legend = p.getPlotItem().legend
            if legend:
                legend.setPen(pg.mkPen(sub_col, width=1))
                legend.setBrush(pg.mkBrush(*leg_bg))
                for sample, label in legend.items:
                    label.setText(label.text, color=txt_col)

    def show_help_dialog(self):
        dlg = QDialog(self)
        dlg.setWindowTitle("YOLO Studio 使用說明與快捷指南")
        dlg.resize(560, 420)
        if os.path.exists(ICON_PATH):
            dlg.setWindowIcon(QIcon(ICON_PATH))
        
        layout = QVBoxLayout(dlg)
        title = QLabel("📖 Nys Studio AI平台系統指南")
        title.setStyleSheet("font-size: 18px; font-weight: bold; color: #D97706; margin-bottom: 10px;")
        layout.addWidget(title)

        content = QTextEdit()
        content.setReadOnly(True)
        help_text = """
### 🚀 主要功能模組說明：

1. **⚙️跟隨系統主題與護眼模式**:
   - 預設模式為【跟隨系統】，自動偵測 Windows 主題設定。
   - 亦可點擊頂部按鈕手動切換【暖光護眼】與【暖暗護眼】。

2. **🔍 實質性主頁搜尋引擎**:
   - 在首頁搜尋欄中可直接輸入任何指令或關鍵字，例如:
     - `yolo12` 或 `yolo26` ➔ 自動載入模型權重並跳轉至訓練配置頁。
     - `bytetrack` 或 `botsort` ➔ 自動設置追蹤器並跳轉至追蹤頁。
     - `onnx` 或 `tensorrt` ➔ 自動設置導出格式並跳轉至導出頁。
     - `datacheck` 或 `xml` ➔ 跳轉至標註轉換與畫框驗證頁。
     - `cuda` 或 `gpu` ➔ 自動診斷 GPU 硬體狀態。

3. **📁 資料與標註轉換**:
   - 支援 XML/JSON 轉 YOLO Detect 與 LabelMe JSON 轉 YOLO Seg。
        """
        content.setMarkdown(help_text)
        layout.addWidget(content)

        btn_close = QPushButton("關閉指南")
        btn_close.setObjectName("GoogleAmberButton")
        btn_close.clicked.connect(dlg.accept)
        layout.addWidget(btn_close, 0, Qt.AlignRight)

        dlg.exec()

    def show_services_menu(self):
        dlg = QDialog(self)
        dlg.setWindowTitle("Google 服務快速 Jump 選單")
        dlg.resize(480, 320)
        if os.path.exists(ICON_PATH):
            dlg.setWindowIcon(QIcon(ICON_PATH))

        layout = QVBoxLayout(dlg)
        title = QLabel("⣿ YOLO Studio 服務地圖")
        title.setStyleSheet("font-size: 16px; font-weight: bold; color: #1A73E8; margin-bottom: 12px;")
        layout.addWidget(title)

        grid = QGridLayout()
        grid.setSpacing(12)

        services = [
            ("🏠 首頁工作台", 0),
            ("📁 資料標註轉換", 1),
            ("⚙️ 模型訓練超參", 2),
            ("📊 即時訓練動態", 3),
            ("🎥 推態目標追蹤", 4),
            ("🚀 模型導出診斷", 5)
        ]

        for i, (stitle, idx) in enumerate(services):
            btn = QPushButton(stitle)
            btn.setObjectName("GoogleChip")
            btn.setMinimumHeight(44)
            btn.clicked.connect(lambda _, page_idx=idx: (self.switch_page(page_idx), dlg.accept()))
            row, col = i // 2, i % 2
            grid.addWidget(btn, row, col)

        layout.addLayout(grid)
        layout.addSpacing(16)

        btn_close = QPushButton("關閉")
        btn_close.setObjectName("GoogleSecondaryButton")
        btn_close.clicked.connect(dlg.accept)
        layout.addWidget(btn_close, 0, Qt.AlignRight)

        dlg.exec()

    # =========================================================
    # Google Sidebar
    # =========================================================
    def create_google_sidebar(self):
        sidebar = QWidget()
        sidebar.setObjectName("GoogleSidebar")
        sidebar.setFixedWidth(240)

        layout = QVBoxLayout(sidebar)
        layout.setContentsMargins(12, 16, 12, 16)
        layout.setSpacing(6)

        self.nav_buttons = []
        nav_items = [
            ("🏠  首頁", 0),
            ("📁  資料與標註轉換", 1),
            ("⚙️  模型與訓練設定", 2),
            ("📊  即時訓練動態", 3),
            ("🎥  推理與目標追蹤", 4),
            ("🚀  模型導出與診斷", 5)
        ]

        for text, idx in nav_items:
            btn = QPushButton(text)
            btn.setObjectName("GoogleNavItem")
            btn.setCheckable(True)
            btn.clicked.connect(lambda _, i=idx: self.switch_page(i))
            layout.addWidget(btn)
            self.nav_buttons.append(btn)

        self.nav_buttons[0].setChecked(True)
        layout.addStretch()

        # --- 左側導覽欄下方：任務管理器風格效能監控區域 ---
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

        # 四個 Task Manager 風格 Mini 折線圖: CPU (Teal), RAM (Purple), GPU (Green), VRAM (Amber)
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

        return sidebar

    def switch_page(self, idx):
        for i, btn in enumerate(self.nav_buttons):
            btn.setChecked(i == idx)
        self.stack.setCurrentIndex(idx)

    # =========================================================
    # Page 0: 首頁
    # =========================================================
    def create_page_home(self):
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setStyleSheet("border: none; background-color: transparent;")

        page = QWidget()
        layout = QVBoxLayout(page)
        layout.setContentsMargins(40, 30, 40, 40)
        layout.setSpacing(20)
        layout.setAlignment(Qt.AlignHCenter)

        # 1. 中央頭像
        avatar_box = QVBoxLayout()
        avatar_box.setAlignment(Qt.AlignCenter)

        avatar_frame = QLabel()
        avatar_frame.setFixedSize(100, 100)
        avatar_frame.setAlignment(Qt.AlignCenter)
        avatar_frame.setStyleSheet("""
            background-color: #FCE7CA;
            border: 4px solid #D97706;
            border-radius: 50px;
            font-size: 42px;
        """)
        avatar_frame.setText("✨")

        user_title = QLabel("YOLO Studio 智慧視覺訓練平台")
        user_title.setStyleSheet("font-size: 22px; font-weight: bold; margin-top: 10px;")
        user_title.setAlignment(Qt.AlignCenter)

        user_subtitle = QLabel("yolo12 & yolo26 相容 • 檢測 / 分割 / 分類 / 實時追蹤全功能")
        user_subtitle.setStyleSheet("font-size: 13px;")
        user_subtitle.setAlignment(Qt.AlignCenter)

        avatar_box.addWidget(avatar_frame, 0, Qt.AlignCenter)
        avatar_box.addWidget(user_title)
        avatar_box.addWidget(user_subtitle)
        layout.addLayout(avatar_box)

        # 2. 大型 Google 搜尋列
        search_box = QHBoxLayout()
        search_box.setAlignment(Qt.AlignCenter)

        self.home_search_bar = QLineEdit()
        self.home_search_bar.setObjectName("GoogleSearchBar")
        self.home_search_bar.setPlaceholderText("🔍 搜尋模型 (yolo12/26)、追蹤器 (bytetrack)、格式 (onnx)...")
        self.home_search_bar.setFixedWidth(640)

        # 自動補全清單 (QCompleter)
        search_keywords = [
            "yolo12n.pt - 一鍵載入並開啟 YOLO12 訓練配置",
            "yolo26n.pt - 一鍵載入並開啟 YOLO26 訓練配置",
            "yolo26n-seg.pt - 一鍵載入並開啟 YOLO26 Segmentation 實例分割",
            "bytetrack.yaml - 自動設置 ByteTrack 實態追蹤器",
            "botsort.yaml - 自動設置 BoT-SORT 多目標追蹤器",
            "onnx - 開啟 ONNX 模型導出面板",
            "tensorrt - 開啟 TensorRT Engine 導出面板",
            "datacheck - 開啟 XML/JSON 轉 YOLO 並進行畫框驗證",
            "cuda - 執行 CUDA GPU 硬體系統健康診斷",
            "train - 直接前往模型訓練與超參數配置"
        ]
        completer = QCompleter(search_keywords, self)
        completer.setCaseSensitivity(Qt.CaseInsensitive)
        completer.setFilterMode(Qt.MatchContains)
        completer.setPopup(QListView())
        self.home_search_bar.setCompleter(completer)

        self.home_search_bar.returnPressed.connect(self.execute_home_search)

        btn_search_exec = QPushButton("搜尋執行 ➔")
        btn_search_exec.setObjectName("GoogleAmberButton")
        btn_search_exec.clicked.connect(self.execute_home_search)

        search_box.addWidget(self.home_search_bar)
        search_box.addWidget(btn_search_exec)
        layout.addLayout(search_box)

        # 3. Quick Chips
        chip_box = QHBoxLayout()
        chip_box.setAlignment(Qt.AlignCenter)
        chip_box.setSpacing(10)

        chips = [
            ("📦 yolo12n.pt", lambda: self.quick_search_action("yolo12n.pt")),
            ("📦 yolo26n.pt", lambda: self.quick_search_action("yolo26n.pt")),
            ("⚡ CUDA GPU 診斷", lambda: self.quick_search_action("cuda")),
            ("🎥 ByteTrack 追蹤器", lambda: self.quick_search_action("bytetrack")),
            ("📄 config.yaml 配置", lambda: self.quick_search_action("config.yaml")),
            ("🚀 ONNX 導出", lambda: self.quick_search_action("onnx"))
        ]

        for text, cb in chips:
            btn = QPushButton(text)
            btn.setObjectName("GoogleChip")
            btn.clicked.connect(cb)
            chip_box.addWidget(btn)

        layout.addLayout(chip_box)
        layout.addSpacing(10)

        # 4. Windows 10/11 磁貼風格建議提示區
        tiles_header = QLabel("💡 智慧工作流建議與操作提示")
        tiles_header.setStyleSheet("font-size: 16px; font-weight: bold; margin-top: 10px;")
        tiles_header.setAlignment(Qt.AlignLeft)
        layout.addWidget(tiles_header, 0, Qt.AlignHCenter)

        metro_grid = QGridLayout()
        metro_grid.setSpacing(20)

        recommendations = [
            ("🚀 建議：一鍵訓練 YOLO12 / YOLO26", "檢測到已具備 PyTorch & GPU 加速，建議優先配置 100 Epochs 與 AMP 混合精度開啟模型訓練。", lambda: self.quick_search_action("yolo12n.pt"), "配置訓練 ➔"),
            ("🔍 建議：DataCheck 標註畫框驗證", "在開啟正式訓練前，建議使用 DataCheck 預覽畫框與 YOLO label polygon 數據是否完美對齊。", lambda: self.quick_search_action("datacheck"), "畫框驗證 ➔"),
            ("🎥 建議：ByteTrack 實體串流追蹤", "支援 Webcam 0 號即時串流與 MP4 影片物件追蹤，適合即時物體辨識與軌跡繪製。", lambda: self.quick_search_action("bytetrack"), "啟動追蹤 ➔"),
            ("⚡ 建議：ONNX / TensorRT 部署導出", "訓練完成的權重可一鍵轉為 ONNX 或 TensorRT Engine，以利嵌入式設備極速推斷部署。", lambda: self.quick_search_action("onnx"), "模型導出 ➔")
        ]

        for i, (rtitle, rdesc, rcb, rbtn_text) in enumerate(recommendations):
            tile = QFrame()
            tile.setObjectName("MetroTileCard")
            tile.setMinimumWidth(360)

            t_layout = QVBoxLayout(tile)
            t_layout.setContentsMargins(18, 18, 18, 18)

            t_title = QLabel(rtitle)
            t_title.setObjectName("MetroTileTitle")
            t_title.setWordWrap(True)

            t_desc = QLabel(rdesc)
            t_desc.setObjectName("MetroTileDesc")
            t_desc.setWordWrap(True)

            t_btn = QPushButton(rbtn_text)
            t_btn.setObjectName("MetroTileBtn")
            t_btn.clicked.connect(rcb)

            t_layout.addWidget(t_title)
            t_layout.addWidget(t_desc)
            t_layout.addSpacing(12)
            t_layout.addWidget(t_btn, 0, Qt.AlignRight)

            row, col = i // 2, i % 2
            metro_grid.addWidget(tile, row, col)

        layout.addLayout(metro_grid)

        # 底部提示小字 (單行顯示)
        footer_lbl = QLabel("本程式碼基於 Ultralytics YOLO，使用 MIT License。任何散佈和再發行務必遵循相關條款。")
        footer_lbl.setStyleSheet("font-size: 11px; text-align: center; margin-top: 15px;")
        footer_lbl.setWordWrap(False) 
        footer_lbl.setAlignment(Qt.AlignCenter)
        layout.addWidget(footer_lbl, 0, Qt.AlignCenter) 

        scroll.setWidget(page) 
        return scroll

    # =========================================================
    # 搜尋執行邏輯
    # =========================================================
    def execute_home_search(self):
        query = self.home_search_bar.text().strip().lower()
        if not query:
            return

        self.quick_search_action(query)

    def quick_search_action(self, query):
        q = query.lower()

        if "yolo12" in q or "yolo26" in q or "yolo11" in q or ".pt" in q or ".yaml" in q:
            model_name = "yolo12n.pt"
            if "yolo12s" in q: model_name = "yolo12s.pt"
            elif "yolo12m" in q: model_name = "yolo12m.pt"
            elif "yolo12l" in q: model_name = "yolo12l.pt"
            elif "yolo26n-seg" in q or "seg" in q: model_name = "yolo26n-seg.pt"
            elif "yolo26" in q: model_name = "yolo26n.pt"
            elif "yolo11" in q: model_name = "yolo11n.pt"

            self.model_input.setEditText(model_name)
            self.switch_page(2)
            self.append_log(f"🔍 [搜尋引擎] 已自動配置模型權重 [{model_name}] 並跳轉至訓練配置頁面。")
            return

        if "track" in q or "bytetrack" in q or "botsort" in q or "camera" in q:
            if "botsort" in q:
                self.tracker_combo.setCurrentText("botsort.yaml")
            else:
                self.tracker_combo.setCurrentText("bytetrack.yaml")
            self.infer_mode_combo.setCurrentIndex(1)
            self.switch_page(4)
            self.append_log(f"🔍 [搜尋引擎] 已自動設置目標追蹤器模式並跳轉至推理追蹤頁面。")
            return

        if "onnx" in q or "tensorrt" in q or "engine" in q or "export" in q or "openvino" in q:
            if "tensorrt" in q or "engine" in q:
                self.export_fmt_combo.setCurrentText("engine (TensorRT)")
            else:
                self.export_fmt_combo.setCurrentText("onnx")
            self.switch_page(5)
            self.append_log(f"🔍 [搜尋引擎] 已自動選擇導出格式並跳轉至模型導出頁面。")
            return

        if "data" in q or "check" in q or "xml" in q or "json" in q or "convert" in q:
            self.switch_page(1)
            self.append_log(f"🔍 [搜尋引擎] 已跳轉至標註資料轉換與 DataCheck 畫框驗證頁面。")
            return

        if "cuda" in q or "gpu" in q or "diag" in q or "torch" in q:
            self.switch_page(5)
            self.check_cuda_status()
            self.append_log(f"🔍 [搜尋引擎] 已啟動 CUDA GPU 狀態診斷。")
            return

        self.switch_page(2)
        self.append_log(f"🔍 [搜尋引擎] 已根據關鍵字 [{query}] 跳轉至相應模組。")

    # =========================================================
    # Page 1: 資料與標註轉換
    # =========================================================
    def create_page_dataprep(self):
        page = QWidget()
        layout = QHBoxLayout(page)
        layout.setContentsMargins(24, 24, 24, 24)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")
        left_layout = QVBoxLayout(left_card)

        header = QLabel("資料與標註格式轉換")
        header.setObjectName("GoogleCardTitle")
        left_layout.addWidget(header)

        form = QFormLayout()
        self.task_type_combo = QComboBox()
        self._setup_combo_view(self.task_type_combo)
        self.task_type_combo.addItems(["detect (目標檢測)", "segment (實例分割)"])
        form.addRow("任務類型:", self.task_type_combo)

        self.anno_input = QLineEdit()
        btn_anno = QPushButton("選擇標註資料夾")
        btn_anno.clicked.connect(lambda: self.select_folder(self.anno_input))
        form.addRow("標註資料夾:", self.anno_input)
        form.addRow("", btn_anno)

        self.image_input = QLineEdit()
        btn_img = QPushButton("選擇影像資料夾")
        btn_img.clicked.connect(lambda: self.select_folder(self.image_input))
        form.addRow("影像資料夾:", self.image_input)
        form.addRow("", btn_img)

        self.dataset_input = QLineEdit(os.path.join(CURRENT_DIR, "NYA"))
        btn_dataset = QPushButton("選擇 Dataset 根目錄")
        btn_dataset.clicked.connect(lambda: self.select_folder(self.dataset_input))
        form.addRow("Dataset 根目錄:", self.dataset_input)
        form.addRow("", btn_dataset)

        self.auto_class_cb = QCheckBox("Auto Classes (自動提取標註檔類別名單)")
        self.auto_class_cb.setChecked(True)
        form.addRow(self.auto_class_cb)

        self.class_input = QLineEdit("NG")
        form.addRow("手動指定類別 (逗號分隔):", self.class_input)

        self.split_ratio_spin = QDoubleSpinBox()
        self.split_ratio_spin.setRange(0.05, 0.5)
        self.split_ratio_spin.setValue(0.2)
        form.addRow("Val 驗證集比例:", self.split_ratio_spin)

        left_layout.addLayout(form)
        left_layout.addSpacing(10)

        self.btn_start_convert = QPushButton("開始標註轉換與生成 Config.yaml")
        self.btn_start_convert.setObjectName("GoogleAmberButton")
        self.btn_start_convert.clicked.connect(self.start_convert)
        left_layout.addWidget(self.btn_start_convert)

        btn_datacheck = QPushButton("執行 DataCheck 數據集驗證")
        btn_datacheck.setObjectName("GoogleSecondaryButton")
        btn_datacheck.clicked.connect(self.start_datacheck)
        left_layout.addWidget(btn_datacheck)

        left_layout.addSpacing(10)
        left_layout.addWidget(QLabel("轉換日誌:"))
        self.convert_log = QTextEdit()
        self.convert_log.setObjectName("GoogleLogViewer")
        self.convert_log.setReadOnly(True)
        self.convert_log.setMaximumHeight(150)
        left_layout.addWidget(self.convert_log)

        left_layout.addStretch()
        layout.addWidget(left_card, 1)

        right_card = QFrame()
        right_card.setObjectName("GoogleCard")
        right_layout = QVBoxLayout(right_card)

        v_header = QLabel("DataCheck 畫框預覽網格")
        v_header.setObjectName("GoogleCardTitle")
        right_layout.addWidget(v_header)

        self.scroll_area = QScrollArea()
        self.scroll_area.setWidgetResizable(True)
        self.grid_widget = QWidget()
        self.grid_layout = QGridLayout(self.grid_widget)
        self.scroll_area.setWidget(self.grid_widget)

        right_layout.addWidget(self.scroll_area)
        layout.addWidget(right_card, 1)

        return page

    # =========================================================
    # Page 2: 模型與訓練超設定
    # =========================================================
    def create_page_train_config(self):
        page = QWidget()
        layout = QVBoxLayout(page)
        layout.setContentsMargins(24, 24, 24, 24)

        card = QFrame()
        card.setObjectName("GoogleCard")
        card_layout = QVBoxLayout(card)

        header = QLabel("YOLO12 / YOLO26 訓練超參數配置")
        header.setObjectName("GoogleCardTitle")
        card_layout.addWidget(header)

        top_form = QFormLayout()
        self.model_input = QComboBox()
        self._setup_combo_view(self.model_input)
        self.model_input.setEditable(True)
        preset_models = [
            "yolo12n.pt", "yolo12s.pt", "yolo12m.pt", "yolo12l.pt", "yolo12x.pt", "yolo12.yaml",
            "yolo26n.pt", "yolo26n-seg.pt", "yolo11n.pt", "yolo11n-seg.pt"
        ]
        self.model_input.addItems(preset_models)
        btn_browse_model = QPushButton("選擇權重 / 配置文件")
        btn_browse_model.clicked.connect(lambda: self.select_file_to_combo(self.model_input, "選擇模型權重"))

        top_form.addRow("YOLO 模型權重:", self.model_input)
        top_form.addRow("", btn_browse_model)

        self.data_input = QLineEdit(os.path.join(CURRENT_DIR, "NYA", "config.yaml"))
        btn_data_select = QPushButton("選擇 Dataset config.yaml")
        btn_data_select.clicked.connect(lambda: self.select_file(self.data_input, "選擇 config.yaml"))
        top_form.addRow("Dataset config:", self.data_input)
        top_form.addRow("", btn_data_select)

        card_layout.addLayout(top_form)

        tabs = QTabWidget()

        # Tab 1
        tab_basic = QWidget()
        f_basic = QFormLayout(tab_basic)
        self.epochs_spin = QSpinBox()
        self.epochs_spin.setRange(1, 10000)
        self.epochs_spin.setValue(100)

        self.batch_spin = QSpinBox()
        self.batch_spin.setRange(-1, 512)
        self.batch_spin.setValue(16)

        self.imgsz_spin = QSpinBox()
        self.imgsz_spin.setRange(32, 4096)
        self.imgsz_spin.setValue(640)

        self.device_input = QLineEdit("0")
        self.workers_spin = QSpinBox()
        self.workers_spin.setRange(0, 64)
        self.workers_spin.setValue(4)

        self.patience_spin = QSpinBox()
        self.patience_spin.setRange(0, 1000)
        self.patience_spin.setValue(50)

        self.pretrained_cb = QCheckBox("Pretrained (預訓練權重)")
        self.pretrained_cb.setChecked(True)
        self.amp_cb = QCheckBox("AMP (自動混合精度)")
        self.amp_cb.setChecked(True)

        f_basic.addRow("Epochs (訓練輪次):", self.epochs_spin)
        f_basic.addRow("Batch Size (-1 為自動):", self.batch_spin)
        f_basic.addRow("Image Size (圖像尺寸):", self.imgsz_spin)
        f_basic.addRow("Device (0, 1 或 cpu):", self.device_input)
        f_basic.addRow("Workers (線程數):", self.workers_spin)
        f_basic.addRow("Patience (早停輪次):", self.patience_spin)
        f_basic.addRow(self.pretrained_cb)
        f_basic.addRow(self.amp_cb)
        tabs.addTab(tab_basic, "1. 基礎與硬體")

        # Tab 2
        tab_opt = QWidget()
        f_opt = QFormLayout(tab_opt)
        self.opt_combo = QComboBox()
        self._setup_combo_view(self.opt_combo)
        self.opt_combo.addItems(["auto", "SGD", "Adam", "AdamW", "NAdam", "RAdam", "RMSProp"])

        self.lr0_spin = QDoubleSpinBox()
        self.lr0_spin.setValue(0.01)
        self.lrf_spin = QDoubleSpinBox()
        self.lrf_spin.setValue(0.01)

        self.cos_lr_cb = QCheckBox("cos_lr (餘弦退火學習率)")
        self.cos_lr_cb.setChecked(True)

        f_opt.addRow("Optimizer (優化器):", self.opt_combo)
        f_opt.addRow("lr0 (初始學習率):", self.lr0_spin)
        f_opt.addRow("lrf (最終學習率比率):", self.lrf_spin)
        f_opt.addRow(self.cos_lr_cb)
        tabs.addTab(tab_opt, "2. 優化器與學習率")

        # Tab 3
        tab_loss = QWidget()
        f_loss = QFormLayout(tab_loss)
        self.box_loss_spin = QDoubleSpinBox()
        self.box_loss_spin.setValue(7.5)
        self.cls_loss_spin = QDoubleSpinBox()
        self.cls_loss_spin.setValue(0.5)
        self.dfl_loss_spin = QDoubleSpinBox()
        self.dfl_loss_spin.setValue(1.5)

        f_loss.addRow("box Loss 權重:", self.box_loss_spin)
        f_loss.addRow("cls Loss 權重:", self.cls_loss_spin)
        f_loss.addRow("dfl Loss 權重:", self.dfl_loss_spin)
        tabs.addTab(tab_loss, "3. 損失權重")

        # Tab 4
        tab_aug = QWidget()
        f_aug = QFormLayout(tab_aug)
        self.hsv_h_spin = QDoubleSpinBox()
        self.hsv_h_spin.setValue(0.015)
        self.hsv_s_spin = QDoubleSpinBox()
        self.hsv_s_spin.setValue(0.7)
        self.hsv_v_spin = QDoubleSpinBox()
        self.hsv_v_spin.setValue(0.4)

        self.mosaic_spin = QDoubleSpinBox()
        self.mosaic_spin.setValue(1.0)
        self.erasing_spin = QDoubleSpinBox()
        self.erasing_spin.setValue(0.4)

        f_aug.addRow("hsv_h (色調):", self.hsv_h_spin)
        f_aug.addRow("hsv_s (飽和度):", self.hsv_s_spin)
        f_aug.addRow("hsv_v (亮度):", self.hsv_v_spin)
        f_aug.addRow("mosaic (馬賽克增強):", self.mosaic_spin)
        f_aug.addRow("erasing (隨機擦除):", self.erasing_spin)
        tabs.addTab(tab_aug, "4. 資料增強")

        card_layout.addWidget(tabs)

        btn_layout = QHBoxLayout()
        self.btn_start_train = QPushButton("▶ 開始訓練")
        self.btn_start_train.setObjectName("GoogleAmberButton")
        self.btn_start_train.clicked.connect(self.start_train)
        
        self.btn_pause_train = QPushButton("⏸ 暫停訓練")
        self.btn_pause_train.setObjectName("GoogleSecondaryButton")
        self.btn_pause_train.setEnabled(False)
        self.btn_pause_train.clicked.connect(self.toggle_pause_train)
        
        self.btn_stop_train = QPushButton("⏹ 取消訓練")
        self.btn_stop_train.setObjectName("GoogleSecondaryButton")
        self.btn_stop_train.setEnabled(False)
        self.btn_stop_train.clicked.connect(self.stop_train)

        btn_layout.addWidget(self.btn_start_train)
        btn_layout.addWidget(self.btn_pause_train)
        btn_layout.addWidget(self.btn_stop_train)
        card_layout.addLayout(btn_layout)

        layout.addWidget(card)
        return page

    # =========================================================
    # Page 3: 即時訓練動態
    # =========================================================
    def create_page_live_train(self):
        page = QWidget()
        layout = QVBoxLayout(page)
        layout.setContentsMargins(24, 24, 24, 24)

        prog_card = QFrame()
        prog_card.setObjectName("GoogleCard")
        prog_layout = QVBoxLayout(prog_card)
        self.lbl_train_status = QLabel("訓練狀態: 待命 (Ready)")
        self.lbl_train_status.setObjectName("GoogleCardTitle")
        self.progress_bar = QProgressBar()
        self.progress_bar.setValue(0)

        prog_layout.addWidget(self.lbl_train_status)
        prog_layout.addWidget(self.progress_bar)
        layout.addWidget(prog_card)

        chart_splitter = QSplitter(Qt.Horizontal)

        self.plot_loss = pg.PlotWidget(title="<b>Loss 訓練損失動態</b>")
        self.plot_loss.showGrid(x=True, y=True)
        self.plot_loss.addLegend()
        self.curve_box = self.plot_loss.plot(pen=pg.mkPen('#D97706', width=2), name="box_loss")
        self.curve_cls = self.plot_loss.plot(pen=pg.mkPen('#1A73E8', width=2), name="cls_loss")
        self.curve_dfl = self.plot_loss.plot(pen=pg.mkPen('#34A853', width=2), name="dfl_loss")
        chart_splitter.addWidget(self.plot_loss)

        self.plot_map = pg.PlotWidget(title="<b>mAP 驗證精度動態</b>")
        self.plot_map.showGrid(x=True, y=True)
        self.plot_map.addLegend()
        self.curve_map50 = self.plot_map.plot(pen=pg.mkPen('#F9AB00', width=2), name="mAP50")
        self.curve_map95 = self.plot_map.plot(pen=pg.mkPen('#A142F4', width=2), name="mAP50-95")
        chart_splitter.addWidget(self.plot_map)

        layout.addWidget(chart_splitter, 2)

        log_card = QFrame()
        log_card.setObjectName("GoogleCard")
        log_layout = QVBoxLayout(log_card)
        log_layout.addWidget(QLabel("即時 Console 訓練日誌"))
        
        self.log_viewer = QTextEdit()
        self.log_viewer.setObjectName("GoogleLogViewer")
        self.log_viewer.setReadOnly(True)
        log_layout.addWidget(self.log_viewer)

        layout.addWidget(log_card, 1)

        self.epochs_data = []
        self.box_data, self.cls_data, self.dfl_data = [], [], []
        self.map50_data, self.map95_data = [], []

        self.update_plot_styles()

        return page

    # =========================================================
    # Page 4: 推理與目標追蹤
    # =========================================================
    def create_page_inference(self):
        page = QWidget()
        layout = QHBoxLayout(page)
        layout.setContentsMargins(24, 24, 24, 24)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")
        left_layout = QVBoxLayout(left_card)
        left_layout.addWidget(QLabel("推理與追蹤控制器"))

        form = QFormLayout()

        self.infer_model_input = QLineEdit(r"runs\detect\train\weights\best.pt")
        btn_infer_model = QPushButton("選擇模型 (.pt)")
        btn_infer_model.clicked.connect(lambda: self.select_file(self.infer_model_input, "選擇模型"))
        form.addRow("模型路徑:", self.infer_model_input)
        form.addRow("", btn_infer_model)

        self.infer_source_input = QLineEdit("0")
        btn_infer_src = QPushButton("選擇測試媒體")
        btn_infer_src.clicked.connect(lambda: self.select_file(self.infer_source_input, "選擇來源"))
        form.addRow("來源 (0為相機):", self.infer_source_input)
        form.addRow("", btn_infer_src)

        self.infer_mode_combo = QComboBox()
        self._setup_combo_view(self.infer_mode_combo)
        self.infer_mode_combo.addItems(["predict (普通推理)", "track (目標追蹤)"])
        form.addRow("模式:", self.infer_mode_combo)

        self.tracker_combo = QComboBox()
        self._setup_combo_view(self.tracker_combo)
        self.tracker_combo.addItems(["bytetrack.yaml", "botsort.yaml"])
        form.addRow("追蹤器:", self.tracker_combo)

        self.conf_spin = QDoubleSpinBox()
        self.conf_spin.setValue(0.25)
        self.iou_spin = QDoubleSpinBox()
        self.iou_spin.setValue(0.45)
        form.addRow("Conf (置信度):", self.conf_spin)
        form.addRow("IoU (重疊閾值):", self.iou_spin)

        left_layout.addLayout(form)
        left_layout.addSpacing(10)

        self.btn_start_infer = QPushButton("▶ 啟動推斷 / 追蹤")
        self.btn_start_infer.setObjectName("GoogleAmberButton")
        self.btn_start_infer.clicked.connect(self.start_inference)
        left_layout.addWidget(self.btn_start_infer)

        self.btn_stop_infer = QPushButton("⏹ 停止推斷")
        self.btn_stop_infer.setObjectName("GoogleSecondaryButton")
        self.btn_stop_infer.clicked.connect(self.stop_inference)
        left_layout.addWidget(self.btn_stop_infer)

        left_layout.addStretch()
        layout.addWidget(left_card, 1)

        right_card = QFrame()
        right_card.setObjectName("GoogleCard")
        right_layout = QVBoxLayout(right_card)

        self.infer_status_lbl = QLabel("畫面待命...")
        self.infer_status_lbl.setObjectName("GoogleCardTitle")
        right_layout.addWidget(self.infer_status_lbl)

        self.canvas_label = QLabel()
        self.canvas_label.setAlignment(Qt.AlignCenter)
        self.canvas_label.setStyleSheet("background-color: #000000; border-radius: 16px;")
        self.canvas_label.setMinimumSize(480, 360)
        right_layout.addWidget(self.canvas_label)

        layout.addWidget(right_card, 2)

        return page

    # =========================================================
    # Page 5: 模型導出與診斷
    # =========================================================
    def create_page_export_tools(self):
        page = QWidget()
        layout = QHBoxLayout(page)
        layout.setContentsMargins(24, 24, 24, 24)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")
        left_layout = QVBoxLayout(left_card)
        left_layout.addWidget(QLabel("模型導出與部署"))

        form = QFormLayout()
        self.export_model_input = QLineEdit("yolo12n.pt")
        btn_exp_model = QPushButton("選擇模型檔")
        btn_exp_model.clicked.connect(lambda: self.select_file(self.export_model_input, "選擇模型"))
        form.addRow("模型路徑:", self.export_model_input)
        form.addRow("", btn_exp_model)

        self.export_fmt_combo = QComboBox()
        self._setup_combo_view(self.export_fmt_combo)
        self.export_fmt_combo.addItems(["onnx", "engine (TensorRT)", "openvino", "torchscript", "tflite", "coreml"])
        form.addRow("目標格式:", self.export_fmt_combo)

        self.export_half_cb = QCheckBox("FP16 Half 精度")
        self.export_dynamic_cb = QCheckBox("Dynamic 動態尺寸")
        self.export_simplify_cb = QCheckBox("Simplify 結構簡化")
        self.export_simplify_cb.setChecked(True)

        form.addRow(self.export_half_cb)
        form.addRow(self.export_dynamic_cb)
        form.addRow(self.export_simplify_cb)

        left_layout.addLayout(form)
        left_layout.addSpacing(10)

        btn_export = QPushButton("開始導出模型")
        btn_export.setObjectName("GoogleAmberButton")
        btn_export.clicked.connect(self.start_export)
        left_layout.addWidget(btn_export)

        left_layout.addStretch()
        layout.addWidget(left_card, 1)

        right_card = QFrame()
        right_card.setObjectName("GoogleCard")
        right_layout = QVBoxLayout(right_card)
        right_layout.addWidget(QLabel("PyTorch & GPU 系統診斷"))

        self.cuda_info_label = QLabel("正在檢測系統硬體...")
        self.cuda_info_label.setStyleSheet("font-size: 14px; line-height: 1.8;")
        right_layout.addWidget(self.cuda_info_label)

        btn_refresh_cuda = QPushButton("🔍 重新檢測 CUDA 狀態")
        btn_refresh_cuda.setObjectName("GoogleSecondaryButton")
        btn_refresh_cuda.clicked.connect(self.check_cuda_status)
        right_layout.addWidget(btn_refresh_cuda)

        right_layout.addStretch()
        layout.addWidget(right_card, 1)

        return page

    # =========================================================
    # 槽函數與事件
    # =========================================================
    def select_folder(self, line_edit):
        folder = QFileDialog.getExistingDirectory(self, "選擇資料夾")
        if folder:
            line_edit.setText(folder)

    def select_file(self, line_edit, title):
        file_path, _ = QFileDialog.getOpenFileName(self, title)
        if file_path:
            line_edit.setText(file_path)

    def select_file_to_combo(self, combo_box, title):
        file_path, _ = QFileDialog.getOpenFileName(self, title)
        if file_path:
            combo_box.setEditText(file_path)

    def append_log(self, msg):
        self.log_viewer.append(msg)

    def start_convert(self):
        anno = self.anno_input.text().strip()
        img = self.image_input.text().strip()
        out = self.dataset_input.text().strip()
        
        if not anno or not img or not out:
            QMessageBox.warning(self, "輸入錯誤", "請確認「標註資料夾」、「影像資料夾」及「Dataset 根目錄」皆已填寫！")
            return
            
        task = "segment" if "segment" in self.task_type_combo.currentText() else "detect"
        use_auto = self.auto_class_cb.isChecked()
        manual_classes = [c.strip() for c in self.class_input.text().split(",") if c.strip()]

        if not use_auto and not manual_classes:
            QMessageBox.warning(self, "輸入錯誤", "未勾選自動提取類別，請手動指定類別名單！")
            return

        if hasattr(self, 'convert_log'):
            self.convert_log.clear()
            
        self.btn_start_convert.setEnabled(False)
        self.btn_start_convert.setText("⏳ 轉換進行中...")

        self.convert_worker = ConvertWorker(task, anno, img, out, use_auto, manual_classes, self.split_ratio_spin.value())
        self.convert_worker.log_signal.connect(self.append_convert_log)
        self.convert_worker.finished_signal.connect(self.on_convert_finished)
        self.convert_worker.start()

    def append_convert_log(self, msg):
        if hasattr(self, 'convert_log'):
            self.convert_log.append(msg)
        if hasattr(self, 'log_viewer'):
            self.log_viewer.append(msg)

    def on_convert_finished(self, success, path_or_err):
        self.btn_start_convert.setEnabled(True)
        self.btn_start_convert.setText("開始標註轉換與生成 Config.yaml")
        
        if success:
            self.data_input.setText(path_or_err)
            self.append_convert_log(f"✨ 轉換完成！已自動將 Config 指向: {path_or_err}")
            QMessageBox.information(self, "轉換成功", f"標註轉換已完成！\n配置文件已生成至:\n{path_or_err}")
        else:
            self.append_convert_log(f"❌ 發生錯誤: {path_or_err}")
            QMessageBox.critical(self, "轉換失敗", f"發生錯誤:\n{path_or_err}")

    def start_datacheck(self):
        cfg_p = self.data_input.text().strip()
        if not os.path.exists(cfg_p):
            self.append_log(f"❌ 請先選擇或生成有效的 config.yaml 檔案！")
            return

        for i in reversed(range(self.grid_layout.count())):
            self.grid_layout.itemAt(i).widget().setParent(None)

        self.datacheck_worker = DataCheckWorker(cfg_p)
        self.datacheck_worker.log_signal.connect(self.append_log)
        self.datacheck_worker.image_rendered_signal.connect(self.add_verify_thumbnail)
        self.datacheck_worker.start()

    def add_verify_thumbnail(self, orig_p, out_p):
        pixmap = QPixmap(out_p).scaled(180, 180, Qt.KeepAspectRatio, Qt.SmoothTransformation)
        lbl = QLabel()
        lbl.setPixmap(pixmap)
        lbl.setToolTip(out_p)
        lbl.setStyleSheet("border: 2px solid #D97706; border-radius: 12px; padding: 2px;")
        count = self.grid_layout.count()
        row, col = count // 3, count % 3
        self.grid_layout.addWidget(lbl, row, col)

    def start_train(self):
        kwargs = {
            "model_path": self.model_input.currentText().strip(),
            "data": self.data_input.text().strip(),
            "epochs": self.epochs_spin.value(),
            "batch": self.batch_spin.value(),
            "imgsz": self.imgsz_spin.value(),
            "device": self.device_input.text().strip(),
            "workers": self.workers_spin.value(),
            "patience": self.patience_spin.value(),
            "optimizer": self.opt_combo.currentText(),
            "lr0": self.lr0_spin.value(),
            "lrf": self.lrf_spin.value(),
            "cos_lr": self.cos_lr_cb.isChecked(),
            "amp": self.amp_cb.isChecked(),
            "pretrained": self.pretrained_cb.isChecked(),
            "box": self.box_loss_spin.value(),
            "cls": self.cls_loss_spin.value(),
            "dfl": self.dfl_loss_spin.value(),
            "hsv_h": self.hsv_h_spin.value(),
            "hsv_s": self.hsv_s_spin.value(),
            "hsv_v": self.hsv_v_spin.value(),
            "mosaic": self.mosaic_spin.value(),
            "erasing": self.erasing_spin.value(),
        }

        self.epochs_data.clear()
        self.box_data.clear()
        self.cls_data.clear()
        self.dfl_data.clear()
        self.map50_data.clear()
        self.map95_data.clear()

        self.btn_start_train.setEnabled(False)
        self.btn_pause_train.setEnabled(True)
        self.btn_pause_train.setText("⏸ 暫停訓練")
        self.btn_stop_train.setEnabled(True)
        self.lbl_train_status.setText("訓練進行中...")
        self.switch_page(3)

        self.train_worker = TrainWorker(kwargs)
        self.train_worker.log_signal.connect(self.append_log)
        self.train_worker.progress_signal.connect(self.progress_bar.setValue)
        self.train_worker.epoch_metrics_signal.connect(self.update_charts)
        self.train_worker.finished_signal.connect(self.on_train_finished)
        self.train_worker.start()

    def update_charts(self, m):
        ep = m.get("epoch", 0)
        self.epochs_data.append(ep)
        if "box_loss" in m: self.box_data.append(m["box_loss"])
        if "cls_loss" in m: self.cls_data.append(m["cls_loss"])
        if "dfl_loss" in m: self.dfl_data.append(m["dfl_loss"])
        if "map50" in m: self.map50_data.append(m["map50"])
        if "map50_95" in m: self.map95_data.append(m["map50_95"])

        self.curve_box.setData(self.epochs_data[:len(self.box_data)], self.box_data)
        self.curve_cls.setData(self.epochs_data[:len(self.cls_data)], self.cls_data)
        self.curve_dfl.setData(self.epochs_data[:len(self.dfl_data)], self.dfl_data)
        self.curve_map50.setData(self.epochs_data[:len(self.map50_data)], self.map50_data)
        self.curve_map95.setData(self.epochs_data[:len(self.map95_data)], self.map95_data)

    def toggle_pause_train(self):
        if not self.train_worker:
            return
            
        if self.train_worker._is_paused:
            self.train_worker.resume()
            self.btn_pause_train.setText("⏸ 暫停訓練")
            self.lbl_train_status.setText("訓練進行中...")
            self.append_log("▶ 繼續訓練...")
        else:
            self.train_worker.pause()
            self.btn_pause_train.setText("▶ 繼續訓練")
            self.lbl_train_status.setText("訓練已暫停 (Paused)")
            self.append_log("⏸ 訓練已暫停 (將在當前 Batch 結束後停下)...")

    def stop_train(self):
        if self.train_worker:
            self.train_worker.stop()
            self.btn_pause_train.setEnabled(False)
            self.append_log("🛑 已發送訓練取消請求...")

    def on_train_finished(self, success, msg):
        self.btn_start_train.setEnabled(True)
        self.btn_pause_train.setEnabled(False)
        self.btn_pause_train.setText("⏸ 暫停訓練")
        self.btn_stop_train.setEnabled(False)
        self.lbl_train_status.setText("訓練已完成" if success else "訓練被取消/出錯")

    def start_inference(self):
        m_path = self.infer_model_input.text().strip()
        src = self.infer_source_input.text().strip()
        mode = "track" if "track" in self.infer_mode_combo.currentText() else "predict"
        tracker = self.tracker_combo.currentText()

        self.infer_worker = InferenceWorker(m_path, src, mode, tracker, self.conf_spin.value(), self.iou_spin.value(), self.device_input.text().strip())
        self.infer_worker.log_signal.connect(self.append_log)
        self.infer_worker.frame_signal.connect(self.update_canvas)
        self.infer_worker.start()

    def update_canvas(self, qimg, info_text):
        pixmap = QPixmap.fromImage(qimg).scaled(self.canvas_label.size(), Qt.KeepAspectRatio, Qt.SmoothTransformation)
        self.canvas_label.setPixmap(pixmap)
        self.infer_status_lbl.setText(info_text)

    def stop_inference(self):
        if self.infer_worker:
            self.infer_worker.stop()

    def start_export(self):
        m_path = self.export_model_input.text().strip()
        fmt = self.export_fmt_combo.currentText().split()[0]
        self.export_worker = ExportWorker(m_path, fmt, self.imgsz_spin.value(), self.export_half_cb.isChecked(), self.export_dynamic_cb.isChecked(), self.export_simplify_cb.isChecked())
        self.export_worker.log_signal.connect(self.append_log)
        self.export_worker.start()

    def check_cuda_status(self):
        self.cuda_worker = CudaCheckWorker()
        self.cuda_worker.info_signal.connect(self.on_cuda_info)
        self.cuda_worker.start()

    def on_cuda_info(self, info):
        avail = info["cuda_available"]
        status_str = f"<b>CUDA 狀態:</b> {'已啟用加速' if avail else '未啟用 (CPU 模式)'}<br>"
        status_str += f"<b>PyTorch 版本:</b> {info['torch_version']}<br>"
        status_str += f"<b>CUDA 版本:</b> {info['cuda_version']}<br>"
        status_str += f"<b>GPU 裝置:</b> {info['device_name']}"
        self.cuda_info_label.setText(status_str)
        # 如果用戶有設定過，就不強制覆蓋；或者只在初始時更新。
        # 這裡保留預設更新為偵測到的 GPU，但前面我們已經做成選單了。
        detected_icon = "nvidia" if avail else "cpu"
        detected_short = info['device_name'].split()[0] if avail else "CPU"
        self.avatar_btn.setText(f" {detected_short}")
        _idir = getattr(self, '_icon_dir', '')
        _ipath = os.path.join(_idir, f"{detected_icon}.png")
        if os.path.exists(_ipath):
            self.avatar_btn.setIcon(QIcon(_ipath))
            self.avatar_btn.setIconSize(QSize(18, 18))
        if hasattr(self, 'device_input'):
            self.device_input.setText("0" if avail else "cpu")


__all__ = ["NyaUI", "detect_system_dark_mode"]


if __name__ == "__main__":
    app = QApplication(sys.argv)
    if os.path.exists(ICON_PATH):
        app.setWindowIcon(QIcon(ICON_PATH))
    win = NyaUI()
    win.show()
    sys.exit(app.exec())