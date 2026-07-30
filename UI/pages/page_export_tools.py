"""
ExportToolsPageWidget — 模型導出與系統診斷頁面模組
提供 ONNX, TensorRT, OpenVINO, TorchScript 模型轉檔與 PyTorch/CUDA 硬體診斷
"""

from PySide6.QtWidgets import (
    QWidget, QHBoxLayout, QVBoxLayout, QLabel, QLineEdit, QPushButton,
    QFrame, QFormLayout, QComboBox, QCheckBox, QListView, QFileDialog
)
from PySide6.QtCore import Signal


class ExportToolsPageWidget(QWidget):
    start_export_requested = Signal(dict)
    refresh_cuda_requested = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.init_ui()

    def init_ui(self):
        layout = QHBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")
        left_layout = QVBoxLayout(left_card)
        left_layout.addWidget(QLabel("模型導出與部署"))

        form = QFormLayout()
        self.export_model_input = QLineEdit("yolo12n.pt")
        btn_exp_model = QPushButton("選擇模型檔")
        btn_exp_model.clicked.connect(lambda: self._select_file(self.export_model_input, "選擇模型"))
        form.addRow("模型路徑:", self.export_model_input)
        form.addRow("", btn_exp_model)

        self.export_fmt_combo = QComboBox()
        self.export_fmt_combo.setView(QListView())
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
        btn_export.clicked.connect(self._on_export_click)
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
        btn_refresh_cuda.clicked.connect(lambda: self.refresh_cuda_requested.emit())
        right_layout.addWidget(btn_refresh_cuda)

        right_layout.addStretch()
        layout.addWidget(right_card, 1)

    def _select_file(self, line_edit, title):
        file_path, _ = QFileDialog.getOpenFileName(self, title)
        if file_path:
            line_edit.setText(file_path)

    def _on_export_click(self):
        fmt = self.export_fmt_combo.currentText().split()[0]
        data = {
            "model_path": self.export_model_input.text().strip(),
            "format": fmt,
            "half": self.export_half_cb.isChecked(),
            "dynamic": self.export_dynamic_cb.isChecked(),
            "simplify": self.export_simplify_cb.isChecked()
        }
        self.start_export_requested.emit(data)

    def update_cuda_info(self, info):
        avail = info.get("cuda_available", False)
        status_str = f"<b>CUDA 狀態:</b> {'已啟用加速' if avail else '未啟用 (CPU 模式)'}<br>"
        status_str += f"<b>PyTorch 版本:</b> {info.get('torch_version', 'N/A')}<br>"
        status_str += f"<b>CUDA 版本:</b> {info.get('cuda_version', 'N/A')}<br>"
        status_str += f"<b>GPU 裝置:</b> {info.get('device_name', 'CPU')}"
        self.cuda_info_label.setText(status_str)
