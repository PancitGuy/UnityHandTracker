import cv2
import mediapipe as mp
from mediapipe.tasks.python import vision
from mediapipe.tasks.python.vision import HandLandmarker, HandLandmarkerOptions, RunningMode

import socket


width, height = 1280, 720

cap = cv2.VideoCapture(0)
cap.set(3, width)
cap.set(4, height)

base_options = mp.tasks.BaseOptions(model_asset_path='hand_landmarker.task')

options = HandLandmarkerOptions(
    base_options=base_options,
    running_mode=RunningMode.VIDEO,
    num_hands=2
)

detector = HandLandmarker.create_from_options(options)

frame_timestamp_ms = 0

landmark_connections = [
    (1, 2), (2, 3), (3, 4), 
    (5, 6), (6, 7), (7, 8), 
    (9, 10), (10, 11), (11, 12), 
    (13, 14), (14, 15), (15, 16), 
    (17, 18), (18, 19), (19, 20), 
    (0, 5), (0, 1)
]

palm_connections = [ 
    (5, 9), 
    (9, 13), (13, 17), 
    (17, 0) ]

landmark_connections.extend(palm_connections)

#Communication with Unity
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sever_address = ("127.0.0.1", 5052)


while True:
    success, img = cap.read()
    if not success:
        break

    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=img_rgb)
    hand_landmarker_result = detector.detect_for_video(mp_image, 
                                                       timestamp_ms=(int(frame_timestamp_ms)))
    frame_timestamp_ms += 1000 / 30 

    data = []

    if hand_landmarker_result.hand_landmarks:
        for hand_landmarks in hand_landmarker_result.hand_landmarks:

            landmark_id = []

            for landmark in hand_landmarks:
                x, y = int(landmark.x * img.shape[1]), int(landmark.y * img.shape[0])
                z = landmark.z
                landmark_id.append((x, y, z))
                cv2.circle(img, (x, y), 5, (0, 0, 255), cv2.FILLED)
            
            for start_idx, end_idx in landmark_connections:
                point1 = landmark_id[start_idx][:2]
                point2= landmark_id[end_idx][:2]
                cv2.line(img, point1, point2, (0, 255, 0), 2)
            
            #print(landmark_id)

            for landmark in landmark_id:
                data.extend([landmark[0], height - landmark[1], landmark[2]])
            #print(data) 
            sock.sendto(str(data).encode(), sever_address)   

    #img = cv2.resize(img, (0,0), None, 0.5, 0.5)
    cv2.imshow("Image", img)
    cv2.waitKey(1)


    
