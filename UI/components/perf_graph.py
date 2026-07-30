"""
TaskManagerMiniGraph — 任務管理器風格即時效能 Mini 折線圖元件
繪製 CPU、RAM、GPU、VRAM 的 Task Manager 即時波動圖表
"""

from collections import deque
from PySide6.QtWidgets import QWidget
from PySide6.QtCore import Qt
from PySide6.QtGui import QPainter, QColor, QPen, QBrush, QPainterPath, QLinearGradient, QFont


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
        font = QFont("Segoe UI", 9)
        font.setBold(True)
        painter.setFont(font)
        painter.drawText(6, 14, self.title_str)

        font.setPointSize(8)
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
