from ultralytics import YOLO

# Load a model
model = YOLO("yolo11n.yaml")  # build a new model from YAML
model = YOLO("yolo11n.pt")  # load a pretrained model (recommended for training)
model = YOLO("yolo11n.yaml").load("yolo11n.pt")  # build from YAML and transfer weights

# # 中斷回復
# # Load a model
# model = YOLO("path/to/last.pt")  # load a partially trained model

# # Resume training
# results = model.train(resume=True)

# Train the model
results = model.train(data="coco8.yaml", epochs=100, imgsz=640)