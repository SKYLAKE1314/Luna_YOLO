"""
DataPrepPageWidget — 資料與標註格式轉換 & LabelImg 互動標註工具頁面模組
整合深度學習工具風格工作流：匯入圖像/標註集 -> 自動專案固定目錄 (NYA_Project/) -> 一鍵拆分與生成 config.yaml -> 一鍵直達模型訓練
"""

import os
from PySide6.QtWidgets import (
    QWidget, QHBoxLayout, QVBoxLayout, QLabel, QLineEdit, QPushButton,
    QFrame, QFormLayout, QComboBox, QCheckBox, QDoubleSpinBox, QTextEdit,
    QScrollArea, QGridLayout, QFileDialog, QTabWidget, QListWidget,
    QListWidgetItem, QInputDialog, QMessageBox, QSizePolicy
)
from PySide6.QtCore import Signal, Qt
from PySide6.QtGui import QKeySequence, QShortcut
from components.annotation_canvas import AnnotationCanvasWidget
from project_manager import HalconProjectManager

CURRENT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PARENT_DIR = os.path.abspath(os.path.join(CURRENT_DIR, ".."))

COCO8_CLASSES = [
    'person', 'bicycle', 'car', 'motorcycle', 'airplane', 'bus', 'train', 'truck',
    'boat', 'traffic light', 'fire hydrant', 'stop sign', 'parking meter', 'bench',
    'bird', 'cat', 'dog', 'horse', 'sheep', 'cow', 'elephant', 'bear', 'zebra',
    'giraffe', 'backpack', 'umbrella', 'handbag', 'tie', 'suitcase', 'frisbee',
    'skis', 'snowboard', 'sports ball', 'kite', 'baseball bat', 'baseball glove',
    'skateboard', 'surfboard', 'tennis racket', 'bottle', 'wine glass', 'cup',
    'fork', 'knife', 'spoon', 'bowl', 'banana', 'apple', 'sandwich', 'orange',
    'broccoli', 'carrot', 'hot dog', 'pizza', 'donut', 'cake', 'chair', 'couch',
    'potted plant', 'bed', 'dining table', 'toilet', 'tv', 'laptop', 'mouse',
    'remote', 'keyboard', 'cell phone', 'microwave', 'oven', 'toaster', 'sink',
    'refrigerator', 'book', 'clock', 'vase', 'scissors', 'teddy bear',
    'hair drier', 'toothbrush'
]


class DataPrepPageWidget(QWidget):
    start_convert_requested = Signal(dict)
    start_datacheck_requested = Signal()
    jump_to_train_requested = Signal(str)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.project_mgr = HalconProjectManager(PARENT_DIR)
        self.image_files = []
        self.current_img_idx = -1
        self.label_dir = self.project_mgr.labels_dir
        self.class_list = ["NG", "OK"]
        self.init_ui()

    def init_ui(self):
        root_layout = QVBoxLayout(self)
        root_layout.setContentsMargins(16, 16, 16, 16)

        self.main_tabs = QTabWidget()
        self.main_tabs.setObjectName("GoogleTabWidget")

        # ── Tab 1: XML/JSON 轉換與 DataCheck ────────────────────────
        tab_convert = QWidget()
        conv_layout = QHBoxLayout(tab_convert)
        conv_layout.setContentsMargins(12, 12, 12, 12)

        left_card = QFrame()
        left_card.setObjectName("GoogleCard")

        card_outer_layout1 = QVBoxLayout(left_card)
        card_outer_layout1.setContentsMargins(0, 0, 0, 0)
        card_outer_layout1.setSpacing(0)

        scroll_area1 = QScrollArea()
        scroll_area1.setWidgetResizable(True)
        scroll_area1.setFrameShape(QFrame.NoFrame)
        scroll_area1.setStyleSheet("QScrollArea { background: transparent; border: none; }")

        scroll_content1 = QWidget()
        scroll_content1.setObjectName("CardScrollContent")
        left_layout = QVBoxLayout(scroll_content1)
        left_layout.setContentsMargins(16, 16, 16, 16)
        left_layout.setSpacing(6)

        header = QLabel("資料與標註格式轉換")
        header.setObjectName("GoogleCardTitle")
        left_layout.addWidget(header)

        form = QFormLayout()
        form.setVerticalSpacing(8)
        self.task_type_combo = QComboBox()
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

        self.dataset_input = QLineEdit(self.project_mgr.dataset_dir)
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
        self.convert_log.setMaximumHeight(90)
        left_layout.addWidget(self.convert_log)

        left_layout.addStretch()
        
        scroll_area1.setWidget(scroll_content1)
        card_outer_layout1.addWidget(scroll_area1)

        conv_layout.addWidget(left_card, 1)

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
        conv_layout.addWidget(right_card, 1)

        self.main_tabs.addTab(tab_convert, "🔄 XML/JSON 轉 YOLO & DataCheck")

        # ── Tab 2:  工作流與 LabelImg 標註工具 ────────────
        tab_labeler = QWidget()
        labeler_layout = QHBoxLayout(tab_labeler)
        labeler_layout.setContentsMargins(12, 12, 12, 12)

        # 左側控制器面板
        lbl_ctrl_card = QFrame()
        lbl_ctrl_card.setObjectName("GoogleCard")
        lbl_ctrl_card.setFixedWidth(350)

        card_outer_layout = QVBoxLayout(lbl_ctrl_card)
        card_outer_layout.setContentsMargins(0, 0, 0, 0)
        card_outer_layout.setSpacing(0)

        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setFrameShape(QFrame.NoFrame)
        scroll_area.setStyleSheet("QScrollArea { background: transparent; border: none; }")

        scroll_content = QWidget()
        scroll_content.setObjectName("CardScrollContent")
        ctrl_layout = QVBoxLayout(scroll_content)
        ctrl_layout.setContentsMargins(12, 12, 12, 12)
        ctrl_layout.setSpacing(0)

        # 上方按鈕群組
        top_layout = QVBoxLayout()
        top_layout.setSpacing(6)

        c_title = QLabel("✨ NYA 一鍵工作流")
        c_title.setObjectName("GoogleCardTitle")
        ctrl_layout.addWidget(c_title)

        # NYA 工作流 3 步驟快捷按鈕列
        btn_step1_img = QPushButton("📁 步驟 1: 匯入圖像資料夾")
        btn_step1_img.clicked.connect(self._import_images_dir)

        btn_step1_lbl = QPushButton("🏷️ 匯入已有標註集 (可選)")
        btn_step1_lbl.setObjectName("GoogleSecondaryButton")
        btn_step1_lbl.clicked.connect(self._import_labels_dir)

        btn_step2_split = QPushButton("⚡ 步驟 2: 一鍵拆分並生成 config.yaml")
        btn_step2_split.setObjectName("GoogleSecondaryButton")
        btn_step2_split.clicked.connect(self._halcon_split_dataset)

        btn_step3_train = QPushButton("🚀 步驟 3: 立即開啟模型訓練 ➔")
        btn_step3_train.setObjectName("GoogleAmberButton")
        btn_step3_train.clicked.connect(self._halcon_jump_to_train)

        top_layout.addWidget(btn_step1_img)
        top_layout.addWidget(btn_step1_lbl)
        top_layout.addWidget(btn_step2_split)
        top_layout.addWidget(btn_step3_train)

        top_layout.addSpacing(10)
        top_layout.addWidget(QLabel("專案圖像清單 (NYA_Project/):"))
        ctrl_layout.addLayout(top_layout)

        self.img_list_widget = QListWidget()
        self.img_list_widget.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        self.img_list_widget.setMinimumHeight(80)
        self.img_list_widget.setMaximumHeight(150)
        self.img_list_widget.currentRowChanged.connect(self._on_image_selected)
        ctrl_layout.addWidget(self.img_list_widget, 0)

        # 下方控制群組
        bot_layout = QVBoxLayout()
        bot_layout.setSpacing(6)
        
        # 類別管理與 COCO8
        bot_layout.addSpacing(6)
        bot_layout.addWidget(QLabel("當前標註類別:"))

        cls_box = QHBoxLayout()
        self.class_combo = QComboBox()
        self.class_combo.addItems(self.class_list)
        self.class_combo.currentIndexChanged.connect(self._on_class_changed)

        btn_add_cls = QPushButton("➕")
        btn_add_cls.setFixedWidth(36)
        btn_add_cls.setToolTip("新增自訂類別")
        btn_add_cls.clicked.connect(self._add_custom_class)

        btn_coco8 = QPushButton("📦 COCO8 預設 (80類)")
        btn_coco8.setToolTip("載入標準 COCO 80 類別名單")
        btn_coco8.clicked.connect(self._load_coco8_classes)

        cls_box.addWidget(self.class_combo, 1)
        cls_box.addWidget(btn_add_cls)
        bot_layout.addLayout(cls_box)
        bot_layout.addWidget(btn_coco8)

        # Val 比例
        bot_layout.addSpacing(6)
        val_row = QHBoxLayout()
        val_row.addWidget(QLabel("Val 比例:"))
        self.labeler_val_spin = QDoubleSpinBox()
        self.labeler_val_spin.setRange(0.05, 0.5)
        self.labeler_val_spin.setValue(0.2)
        self.labeler_val_spin.setSingleStep(0.05)
        val_row.addWidget(self.labeler_val_spin)
        bot_layout.addLayout(val_row)

        # 操作按鈕
        bot_layout.addSpacing(8)
        nav_row = QHBoxLayout()
        self.btn_prev = QPushButton("⬅ 上一張 (A)")
        self.btn_prev.clicked.connect(self._prev_image)
        self.btn_next = QPushButton("➡ 下一張 (D)")
        self.btn_next.clicked.connect(self._next_image)
        nav_row.addWidget(self.btn_prev)
        nav_row.addWidget(self.btn_next)
        bot_layout.addLayout(nav_row)

        self.btn_save_anno = QPushButton("💾 儲存標註 (Ctrl+S)")
        self.btn_save_anno.clicked.connect(self._save_current_annotation)
        bot_layout.addWidget(self.btn_save_anno)

        btn_delete_selected = QPushButton("❌ 刪除選取框 (Delete)")
        btn_delete_selected.setObjectName("GoogleSecondaryButton")
        btn_delete_selected.clicked.connect(lambda: self.canvas.delete_selected_box())
        bot_layout.addWidget(btn_delete_selected)

        btn_clear = QPushButton("🗑 清除此圖所有畫框")
        btn_clear.setObjectName("GoogleSecondaryButton")
        btn_clear.clicked.connect(self._clear_canvas_boxes)
        bot_layout.addWidget(btn_clear)
        
        ctrl_layout.addSpacing(6)
        ctrl_layout.addLayout(bot_layout)
        ctrl_layout.addStretch()

        scroll_area.setWidget(scroll_content)
        card_outer_layout.addWidget(scroll_area)

        labeler_layout.addWidget(lbl_ctrl_card, 0)

        # 右側繪圖畫布
        canvas_card = QFrame()
        canvas_card.setObjectName("GoogleCard")
        canvas_layout = QVBoxLayout(canvas_card)

        self.lbl_status = QLabel("待命：點擊「步驟 1」匯入圖像資料夾開啟標註器...")
        self.lbl_status.setObjectName("GoogleCardTitle")
        canvas_layout.addWidget(self.lbl_status)

        self.canvas = AnnotationCanvasWidget()
        self.canvas.annotation_changed.connect(self._on_annotation_changed)
        canvas_layout.addWidget(self.canvas, 1)

        hint_bar = QLabel("💡 操作提示：【點擊框體】拖曳移動 | 【8個控制點】拖曳縮放 | 【Delete / Backspace】刪除選取框 | 【Esc】取消選取")
        hint_bar.setStyleSheet("font-size: 11px; opacity: 0.8; margin-top: 4px;")
        canvas_layout.addWidget(hint_bar)

        labeler_layout.addWidget(canvas_card, 1)

        self.main_tabs.addTab(tab_labeler, "🏷️ LabelImg 互動標註 & NYA 工作流")
        root_layout.addWidget(self.main_tabs)

        # 鍵盤快捷鍵 (A: 上一張, D: 下一張, Ctrl+S: 儲存)
        self.shortcut_prev = QShortcut(QKeySequence("A"), self)
        self.shortcut_prev.activated.connect(self._prev_image)

        self.shortcut_next = QShortcut(QKeySequence("D"), self)
        self.shortcut_next.activated.connect(self._next_image)

        self.shortcut_save = QShortcut(QKeySequence("Ctrl+S"), self)
        self.shortcut_save.activated.connect(self._save_current_annotation)

        # 初始嘗試載入既有 NYA_Project
        self._refresh_project_file_list()

    # --- NYA 工作流 3 步驟邏輯 ---
    def _import_images_dir(self):
        folder = QFileDialog.getExistingDirectory(self, "選擇圖像資料夾 (Import Images)")
        if not folder:
            return
        raw_dir, _ = self.project_mgr.setup_project_from_folders(folder, copy_files=True)
        self._refresh_project_file_list()
        QMessageBox.information(self, "匯入成功", f"已成功匯入圖像並建立專案目錄！\n專案位置: {raw_dir}")

    def _import_labels_dir(self):
        folder = QFileDialog.getExistingDirectory(self, "選擇已有標註集資料夾 (Import Labels)")
        if not folder:
            return
        _, lbl_dir = self.project_mgr.setup_project_from_folders(self.project_mgr.raw_images_dir, folder, copy_files=True)
        self._refresh_project_file_list()
        QMessageBox.information(self, "標註集匯入成功", f"已將已有標註檔匯入至專案！\n標註位置: {lbl_dir}")

    def _refresh_project_file_list(self):
        raw_dir = self.project_mgr.raw_images_dir
        self.label_dir = self.project_mgr.labels_dir
        exts = ('.jpg', '.jpeg', '.png', '.bmp', '.webp')

        if os.path.exists(raw_dir):
            self.image_files = [
                os.path.join(raw_dir, f) for f in os.listdir(raw_dir)
                if f.lower().endswith(exts)
            ]
            self.image_files.sort()

        self.img_list_widget.clear()
        for p in self.image_files:
            fname = os.path.basename(p)
            txt_name = os.path.splitext(fname)[0] + ".txt"
            txt_path = os.path.join(self.label_dir, txt_name)
            is_labeled = os.path.exists(txt_path) and os.path.getsize(txt_path) > 0

            item_text = f"✅ {fname}" if is_labeled else f"📄 {fname}"
            item = QListWidgetItem(item_text)
            self.img_list_widget.addItem(item)

        if self.image_files:
            self.img_list_widget.setCurrentRow(0)
            self.lbl_status.setText(f"專案共有 {len(self.image_files)} 張圖片 | 標註目錄: NYA_Project/labels/")

        # Auto-load classes.txt if it exists
        classes_txt_path = os.path.join(self.label_dir, "classes.txt")
        if os.path.exists(classes_txt_path):
            try:
                with open(classes_txt_path, "r", encoding="utf-8") as f:
                    loaded_classes = [line.strip() for line in f if line.strip()]
                if loaded_classes:
                    self.class_list = loaded_classes
                    self.class_combo.clear()
                    self.class_combo.addItems(self.class_list)
                    self.canvas.set_class_names(self.class_list)
            except Exception:
                pass

    def _halcon_split_dataset(self):
        self._save_current_annotation()
        try:
            res = self.project_mgr.split_and_build_dataset(
                val_ratio=self.labeler_val_spin.value(),
                class_names=self.class_list
            )
            cfg_path = res["config_path"]
            QMessageBox.information(
                self, "拆分與 config.yaml 生成成功",
                f"✨ [NYA 工作流] 資料集拆分成功！\n\n"
                f"• 訓練集 (Train): {res['train_count']} 張\n"
                f"• 驗證集 (Val): {res['val_count']} 張\n"
                f"• 標註類別數: {len(res['class_names'])} 個\n"
                f"• Config 檔案: {cfg_path}"
            )
            return cfg_path
        except Exception as e:
            QMessageBox.warning(self, "拆分錯誤", f"無法完成拆分: {e}")
            return None

    def _halcon_jump_to_train(self):
        cfg_path = self._halcon_split_dataset()
        if cfg_path:
            self.jump_to_train_requested.emit(cfg_path)

    # --- LabelImg 畫布互動事件 ---
    def _on_image_selected(self, row):
        if row < 0 or row >= len(self.image_files):
            return
        self.current_img_idx = row
        img_path = self.image_files[row]
        fname = os.path.basename(img_path)
        base_name = os.path.splitext(fname)[0]
        txt_path = os.path.join(self.label_dir, f"{base_name}.txt")

        self.canvas.load_image(img_path, txt_path, self.class_list)
        box_cnt = len(self.canvas.boxes)
        self.lbl_status.setText(f"[{row+1}/{len(self.image_files)}] {fname} — 已有 {box_cnt} 個標註框")

    def _on_class_changed(self, idx):
        if idx >= 0:
            self.canvas.set_current_class(idx)

    def _add_custom_class(self):
        text, ok = QInputDialog.getText(self, "新增標註類別", "請輸入類別名稱:")
        if ok and text.strip():
            cname = text.strip()
            if cname not in self.class_list:
                self.class_list.append(cname)
                self.class_combo.addItem(cname)
                self.class_combo.setCurrentIndex(len(self.class_list) - 1)
                self.canvas.set_class_names(self.class_list)

    def _load_coco8_classes(self):
        self.class_list = list(COCO8_CLASSES)
        self.class_combo.clear()
        self.class_combo.addItems(self.class_list)
        self.canvas.set_class_names(self.class_list)
        QMessageBox.information(self, "COCO8 載入成功", "已為標註器一鍵載入標準 COCO 80 類別名稱庫！")

    def _save_current_annotation(self):
        if self.current_img_idx < 0 or self.current_img_idx >= len(self.image_files):
            return
        img_path = self.image_files[self.current_img_idx]
        fname = os.path.basename(img_path)
        base_name = os.path.splitext(fname)[0]
        txt_path = os.path.join(self.label_dir, f"{base_name}.txt")

        self.canvas.save_yolo_labels(txt_path)

        classes_txt = os.path.join(self.label_dir, "classes.txt")
        with open(classes_txt, "w", encoding="utf-8") as f:
            for cname in self.class_list:
                f.write(f"{cname}\n")

        item = self.img_list_widget.item(self.current_img_idx)
        if item:
            item.setText(f"✅ {fname}")

        self.lbl_status.setText(f"✅ 已儲存標註至: NYA_Project/labels/{base_name}.txt ({len(self.canvas.boxes)} 個框)")

    def _prev_image(self):
        if self.current_img_idx > 0:
            self._save_current_annotation()
            self.img_list_widget.setCurrentRow(self.current_img_idx - 1)

    def _next_image(self):
        if self.current_img_idx < len(self.image_files) - 1:
            self._save_current_annotation()
            self.img_list_widget.setCurrentRow(self.current_img_idx + 1)

    def _clear_canvas_boxes(self):
        self.canvas.clear_boxes()
        self._save_current_annotation()

    def _on_annotation_changed(self):
        if self.current_img_idx >= 0 and self.current_img_idx < len(self.image_files):
            fname = os.path.basename(self.image_files[self.current_img_idx])
            box_cnt = len(self.canvas.boxes)
            self.lbl_status.setText(f"[{self.current_img_idx+1}/{len(self.image_files)}] {fname} — 已編輯 ({box_cnt} 個標註框, 按 Ctrl+S 儲存)")

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
