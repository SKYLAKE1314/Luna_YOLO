"""
DataPrepPageWidget — 資料與標註格式轉換頁面模组
提供 XML/JSON 轉 YOLO Detect/Seg 轉換與 DataCheck 畫框驗證預覽
"""

import os
from PySide6.QtWidgets import (
    QWidget, QHBoxLayout, QVBoxLayout, QLabel, QLineEdit, QPushButton,
    QFrame, QFormLayout, QComboBox, QCheckBox, QDoubleSpinBox, QTextEdit,
    QScrollArea, QGridLayout, QListView, QFileDialog
)
from PySide6.QtCore import Signal, Qt

CURRENT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


class DataPrepPageWidget(QWidget):
    start_convert_requested = Signal(dict)
    start_datacheck_requested = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.init_ui()

    def init_ui(self):
        layout = QHBoxLayout(self)
        layout.setContentsMargins(24, 24, 24, 24)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")
        left_layout = QVBoxLayout(left_card)

        header = QLabel("資料與標註格式轉換")
        header.setObjectName("GoogleCardTitle")
        left_layout.addWidget(header)

        form = QFormLayout()
        self.task_type_combo = QComboBox()
        self.task_type_combo.setView(QListView())
        self.task_type_combo.addItems(["detect (目標檢測)", "segment (實例分割)"])
        form.addRow("任務類型:", self.task_type_combo)

        self.anno_input = QLineEdit()
        btn_anno = QPushButton("選擇標註資料夾")
        btn_anno.clicked.connect(lambda: self._select_folder(self.anno_input))
        form.addRow("標註資料夾:", self.anno_input)
        form.addRow("", btn_anno)

        self.image_input = QLineEdit()
        btn_img = QPushButton("選擇影像資料夾")
        btn_img.clicked.connect(lambda: self._select_folder(self.image_input))
        form.addRow("影像資料夾:", self.image_input)
        form.addRow("", btn_img)

        self.dataset_input = QLineEdit(os.path.join(CURRENT_DIR, "NYA"))
        btn_dataset = QPushButton("選擇 Dataset 根目錄")
        btn_dataset.clicked.connect(lambda: self._select_folder(self.dataset_input))
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
        self.btn_start_convert.clicked.connect(self._on_convert_click)
        left_layout.addWidget(self.btn_start_convert)

        btn_datacheck = QPushButton("執行 DataCheck 數據集驗證")
        btn_datacheck.setObjectName("GoogleSecondaryButton")
        btn_datacheck.clicked.connect(lambda: self.start_datacheck_requested.emit())
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

    def _select_folder(self, line_edit):
        folder = QFileDialog.getExistingDirectory(self, "選擇資料夾")
        if folder:
            line_edit.setText(folder)

    def _on_convert_click(self):
        data = {
            "task_type": "segment" if "segment" in self.task_type_combo.currentText() else "detect",
            "anno_dir": self.anno_input.text().strip(),
            "image_dir": self.image_input.text().strip(),
            "output_root": self.dataset_input.text().strip(),
            "auto_class": self.auto_class_cb.isChecked(),
            "class_str": self.class_input.text().strip(),
            "val_ratio": self.split_ratio_spin.value()
        }
        self.start_convert_requested.emit(data)

    def append_log(self, text):
        self.convert_log.append(text)
