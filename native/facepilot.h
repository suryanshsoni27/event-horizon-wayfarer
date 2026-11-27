#pragma once
#include <stdint.h>

// No image, device identifier, or pose is written to disk by this interface.
struct FPFrame {
 int32_t status,faces,width,height,sequence,valid;
 float yaw,pitch,roll,confidence,mouthOpen,mouthWidth,x,y,w,h,age;
};
extern "C" {
 int fp_frame_size();
 int fp_camera_count();
 int fp_camera_name(int index,char* destination,int capacity);
 int fp_start(int camera);
 void fp_stop();
 int fp_poll(FPFrame* frame);
 int fp_copy_preview(uint8_t* destination,int capacity,int* width,int* height,int* sequence);
 // Offline analysis shares the exact detector used by live capture. It never opens a camera.
 int fp_analyse_bgra(const uint8_t* pixels,int length,int width,int height,int stride,FPFrame* frame);
 int fp_mouth_metrics(const float* points,int floatCount,int innerCount,int outerCount,int leftCount,int rightCount,float rollDegrees,float* openness,float* width);
}
