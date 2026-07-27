from calendar import EPOCH

from torch import device
from ultralytics import YOLO
from ultralytics.models.rtdetr import train

# Load a COCO-pretrained YOLO12n model
#model = YOLO(r"yolo12x.yaml").load("yolo12x.pt")
model = YOLO("yolo26n.pt")

# Train the model on the COCO8 example dataset for 100 epochs
results = model.train(data=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\config.yaml", 
                      epochs=10, 
                      imgsz=640, 
                      rect=True, # default False 是否使用矩形訓練以適應不同寬高比的圖像，從而提高訓練效率和準確性，但可能會影響精度
                      batch=2,
                      device=0,
                      #device='cpu',
                      workers=0,
                      pretrained=False, # default False 是否基於預訓練權重繼續訓練
                      optimizer='auto', # 训练优化器的选择。选项包括 SGD, Adam, AdamW(適合小標注), NAdam, RAdam, RMSProp 等等，或者 auto 用于基于模型配置自动选择。影响收敛速度和稳定性。
                      seed=0, # 隨機種子設置以確保結果可重現
                      deterministic=True, # 是否啟用確定性算法以獲得可重現的結果
                      single_cls=False, # 是否將所有目標視為單一類別。适用于二元分类任务或侧重于对象是否存在而非分类时。
                      classes=[0], # 寫下標 指定要側重訓練的類別索引列表，包含這些類別的標註將被更側重用於訓練
                      multi_scale=False, # 是否啟用多尺度訓練以增強模型對不同圖像尺寸的適應能力
                      cos_lr=True, # default false 是否使用餘弦退火學習率調度器來調整學習率
                      amp=False, # default true 是否啟用自動混合精度以加速訓練並減少顯存使用
                      cls=0.5, # default 0.5 類別損失的權重，影響模型對分類準確性的重視程度
                      dfl=1.5, # 	分布焦点损失的权重，在某些 YOLO 版本中用于细粒度分类。
                      box=7.5, # default 7.5 邊界框損失的權重，影響模型對邊界框定位的重視程度
                      iou=0.2,
                      profile=False, # 在训练期间启用 ONNX 和 TensorRT 速度的分析，有助于优化模型部署。
                      resume=False, # 是否從上次中斷的訓練過程繼續訓練
                      save=True, # 模型儲存以便恢復訓練
                      save_period=-1,# 模型儲存epoch，-1為禁用
                      # 超參數
                      hsv_h=0.015, # default 0.015 通过色轮的一小部分调整图像的色调，从而引入颜色变化。帮助模型在不同的光照条件下进行泛化。
                      hsv_s=0.7, # 通过一小部分改变图像的饱和度，从而影响颜色的强度。可用于模拟不同的环境条件。
                      hsv_v=0.5, # default 0.4 通过一小部分调整图像的亮度，从而影响图像的明暗程度。帮助模型适应不同的光照情况。
                      degrees=30, # default 0.0 在指定的角度范围内随机旋转图像，提高模型识别各种方向物体的能力。
                      translate=0.2, # default 0.1 将图像横向和纵向平移一小部分，帮助学习detect 部分可见的物体。
                      scale=0.5, # default 0.5 通过增益因子缩放图像，模拟物体与相机的不同距离。
                      shear=0.0, # 	按指定的角度错切图像，模仿从不同角度观察物体的效果。
                      flipud=0.2, # default 0.0 以一定概率垂直翻转图像，增强模型对不同视角的适应能力。
                      fliplr=0.5, # default 0.5 以一定概率水平翻转图像，增强模型对不同视角的适应能力。
                      bgr=0.05, # default 0.0 以指定的概率将图像通道从 RGB 翻转到 BGR，���拟不同的光照条件。
                      mosaic=1.0, # default 1.0 将四个训练图像组合成一个，模拟不同的场景组成和物体交互。对于复杂的场景理解非常有效。
                      mixup=0.0, # default 0.0 将两张图像及其标签按一定比例混合，增强模型对不同物体组合的识别能力。
                      cutmix=0.1, # default 0.0 将一张图像的一部分替换为另一张图像的部分，帮助模型学习在遮挡情况下识别物体。
                      copy_paste=0.0, # default 0.0 从其他图像中复制物体并粘贴到当前图像中，增强模型对复杂场景的理解能力。
                      auto_augment='autoaugment', # 	应用预定义的增强策略（'randaugment', 'autoaugment'或 'augmix'）通过视觉多样性来增强模型性能。
                      erasing=0.4 # default 0.4	在训练期间随机擦除图像区域，以鼓励模型关注不太明显的特征。
) 

# Run inference with the YOLO12n model on the 'bus.jpg' image
#results = model("path/to/bus.jpg")