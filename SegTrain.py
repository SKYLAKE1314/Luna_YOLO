from ultralytics import YOLO

# Load a model
model = YOLO("yolo11n-seg.pt")  # load a pretrained model (recommended for training)

# Train the model
results = model.train(data=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\config.yaml", 
                      epochs=100, 
                      imgsz=640,
                      workers=0)