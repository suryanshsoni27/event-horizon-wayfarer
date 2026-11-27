#import <Cocoa/Cocoa.h>
#import <AVFoundation/AVFoundation.h>
#import <Vision/Vision.h>
#import <CoreVideo/CoreVideo.h>
#import <ImageIO/ImageIO.h>
#include "facepilot.h"
#include <atomic>
#include <chrono>
#include <cmath>
#include <cstring>
#include <mutex>
#include <vector>

namespace {
std::mutex stateMutex;
FPFrame latest={};
std::vector<uint8_t> preview;
double capturedAt=0;
std::atomic<uint64_t> generation{0};
std::atomic<bool> wanted{false};
std::atomic<uint32_t> sequence{0};
double Clock(){return std::chrono::duration<double>(std::chrono::steady_clock::now().time_since_epoch()).count();}
void Clear(int status){std::lock_guard<std::mutex> lock(stateMutex);latest={};latest.status=status;capturedAt=0;std::fill(preview.begin(),preview.end(),0);preview.clear();}

NSArray<AVCaptureDevice*>* Cameras(){
 NSMutableArray<AVCaptureDeviceType>* types=[NSMutableArray arrayWithObject:AVCaptureDeviceTypeBuiltInWideAngleCamera];
 if(@available(macOS 14.0,*)){[types addObject:AVCaptureDeviceTypeExternal];[types addObject:AVCaptureDeviceTypeContinuityCamera];}
 else {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
  [types addObject:AVCaptureDeviceTypeExternalUnknown];
#pragma clang diagnostic pop
 }
 return [AVCaptureDeviceDiscoverySession discoverySessionWithDeviceTypes:types mediaType:AVMediaTypeVideo position:AVCaptureDevicePositionUnspecified].devices;
}
void Mouth(VNFaceObservation* face,FPFrame& frame){
 auto inner=face.landmarks.innerLips,outer=face.landmarks.outerLips,left=face.landmarks.leftEye,right=face.landmarks.rightEye;
 if(!inner||!outer||!left||!right)return;
 std::vector<float> points;
 for(VNFaceLandmarkRegion2D* region in @[inner,outer,left,right])for(NSUInteger i=0;i<region.pointCount;i++){CGPoint p=region.normalizedPoints[i];points.push_back(p.x*frame.w*frame.width);points.push_back(p.y*frame.h*frame.height);}
 fp_mouth_metrics(points.data(),(int)points.size(),(int)inner.pointCount,(int)outer.pointCount,(int)left.pointCount,(int)right.pointCount,frame.roll,&frame.mouthOpen,&frame.mouthWidth);
}
int Detect(CVPixelBufferRef image,FPFrame& frame){
 @autoreleasepool {
  VNImageRequestHandler* handler=[[VNImageRequestHandler alloc] initWithCVPixelBuffer:image orientation:kCGImagePropertyOrientationUp options:@{}];
  VNDetectFaceRectanglesRequest* pose=[VNDetectFaceRectanglesRequest new];pose.revision=VNDetectFaceRectanglesRequestRevision3;
  NSError* error=nil;if(![handler performRequests:@[pose] error:&error])return 0;
  VNFaceObservation* selected=nil;for(VNFaceObservation* face in pose.results){if(face.confidence<.55f||face.boundingBox.size.height<.075f)continue;frame.faces++;selected=face;}
  if(frame.faces!=1)return 1;
  frame.confidence=selected.confidence;frame.x=selected.boundingBox.origin.x;frame.y=selected.boundingBox.origin.y;frame.w=selected.boundingBox.size.width;frame.h=selected.boundingBox.size.height;
  if(!selected.yaw||!selected.pitch||!selected.roll)return 1;
  constexpr float degrees=180.f/3.14159265358979323846f;
  frame.yaw=selected.yaw.floatValue*degrees;frame.pitch=selected.pitch.floatValue*degrees;frame.roll=selected.roll.floatValue*degrees;
  VNDetectFaceLandmarksRequest* landmarks=[VNDetectFaceLandmarksRequest new];landmarks.revision=VNDetectFaceLandmarksRequestRevision3;landmarks.inputFaceObservations=@[selected];
  if(![handler performRequests:@[landmarks] error:&error]||landmarks.results.count!=1)return 1;
  Mouth(landmarks.results.firstObject,frame);
  frame.valid=std::isfinite(frame.yaw)&&std::isfinite(frame.pitch)&&std::isfinite(frame.roll);
  return 1;
 }
}
}

@interface FPCaptureDelegate : NSObject<AVCaptureVideoDataOutputSampleBufferDelegate>
@property(nonatomic) uint64_t token;
@end
@implementation FPCaptureDelegate
- (void)captureOutput:(AVCaptureOutput*)output didOutputSampleBuffer:(CMSampleBufferRef)buffer fromConnection:(AVCaptureConnection*)connection {
 if(!wanted||self.token!=generation.load())return;
 @autoreleasepool {
  double start=Clock();CVPixelBufferRef image=CMSampleBufferGetImageBuffer(buffer);if(!image)return;
  int width=(int)CVPixelBufferGetWidth(image),height=(int)CVPixelBufferGetHeight(image);
  if(width<1||height<1||width>1920||height>1080||CVPixelBufferGetPixelFormatType(image)!=kCVPixelFormatType_32BGRA)return;
  FPFrame frame={};frame.status=2;frame.width=width;frame.height=height;frame.sequence=(int32_t)++sequence;
  Detect(image,frame);
  CVPixelBufferLockBaseAddress(image,kCVPixelBufferLock_ReadOnly);
  {
   std::lock_guard<std::mutex> lock(stateMutex);
   if(wanted&&self.token==generation.load()){
    preview.resize((size_t)width*height*4);const uint8_t* source=(const uint8_t*)CVPixelBufferGetBaseAddress(image);size_t stride=CVPixelBufferGetBytesPerRow(image);
    for(int y=0;y<height;y++)memcpy(preview.data()+(size_t)y*width*4,source+(size_t)y*stride,(size_t)width*4);
    latest=frame;capturedAt=start;
   }
  }
  CVPixelBufferUnlockBaseAddress(image,kCVPixelBufferLock_ReadOnly);
 }
}
@end

@interface FPCamera : NSObject
@property(nonatomic,strong) AVCaptureSession* session;
@property(nonatomic,strong) FPCaptureDelegate* delegate;
@property(nonatomic,strong) id faultObserver;
@property(nonatomic,strong) id disconnectObserver;
@property(nonatomic,strong) dispatch_queue_t sessionQueue;
@property(nonatomic,strong) dispatch_queue_t frameQueue;
- (void)cleanup;
- (void)start:(int)index token:(uint64_t)token;
@end
@implementation FPCamera
- (instancetype)init {if((self=[super init])){_sessionQueue=dispatch_queue_create("wayfarer.face.session",DISPATCH_QUEUE_SERIAL);_frameQueue=dispatch_queue_create("wayfarer.face.frames",DISPATCH_QUEUE_SERIAL);}return self;}
- (void)cleanup {
 if(self.faultObserver)[NSNotificationCenter.defaultCenter removeObserver:self.faultObserver];
 if(self.disconnectObserver)[NSNotificationCenter.defaultCenter removeObserver:self.disconnectObserver];
 self.faultObserver=nil;self.disconnectObserver=nil;
 [self.session stopRunning];for(AVCaptureOutput* output in self.session.outputs)if([output isKindOfClass:AVCaptureVideoDataOutput.class])[(AVCaptureVideoDataOutput*)output setSampleBufferDelegate:nil queue:nullptr];
 self.session=nil;self.delegate=nil;
}
- (void)start:(int)index token:(uint64_t)token {
 dispatch_async(self.sessionQueue,^{@autoreleasepool {
  if(!wanted||generation.load()!=token)return;
  [self cleanup];NSArray<AVCaptureDevice*>* cameras=Cameras();
  if(index<0||index>=(int)cameras.count){Clear(-2);wanted=false;return;}
  NSError* error=nil;AVCaptureDeviceInput* input=[AVCaptureDeviceInput deviceInputWithDevice:cameras[index] error:&error];
  if(!input){Clear(-3);wanted=false;return;}
  AVCaptureSession* session=[AVCaptureSession new];[session beginConfiguration];
  if([session canSetSessionPreset:AVCaptureSessionPreset640x480])session.sessionPreset=AVCaptureSessionPreset640x480;
  else if([session canSetSessionPreset:AVCaptureSessionPresetMedium])session.sessionPreset=AVCaptureSessionPresetMedium;
  AVCaptureVideoDataOutput* output=[AVCaptureVideoDataOutput new];output.alwaysDiscardsLateVideoFrames=YES;output.videoSettings=@{(NSString*)kCVPixelBufferPixelFormatTypeKey:@(kCVPixelFormatType_32BGRA)};
  if(![session canAddInput:input]||![session canAddOutput:output]){[session commitConfiguration];Clear(-3);wanted=false;return;}
  [session addInput:input];[session addOutput:output];[session commitConfiguration];
  self.delegate=[FPCaptureDelegate new];self.delegate.token=token;[output setSampleBufferDelegate:self.delegate queue:self.frameQueue];self.session=session;
  // No audio device/input, movie output, network operation or image persistence.
  auto fail=^(NSNotification* notification){if(generation.load()==token&&wanted){wanted=false;Clear(-3);dispatch_async(self.sessionQueue,^{if(generation.load()==token)[self cleanup];});}};
  self.faultObserver=[NSNotificationCenter.defaultCenter addObserverForName:AVCaptureSessionRuntimeErrorNotification object:session queue:nil usingBlock:fail];
  self.disconnectObserver=[NSNotificationCenter.defaultCenter addObserverForName:AVCaptureDeviceWasDisconnectedNotification object:cameras[index] queue:nil usingBlock:fail];
  if(!wanted||generation.load()!=token){[self cleanup];return;}
  [session startRunning];
  if(generation.load()==token&&wanted){std::lock_guard<std::mutex> lock(stateMutex);if(latest.status==1)latest.status=2;}
 }});
}
@end

static FPCamera* Camera(){static FPCamera* camera=nil;static dispatch_once_t once;dispatch_once(&once,^{camera=[FPCamera new];});return camera;}
extern "C" {
int fp_frame_size(){return sizeof(FPFrame);}
int fp_mouth_metrics(const float* points,int floatCount,int innerCount,int outerCount,int leftCount,int rightCount,float rollDegrees,float* openness,float* width){
 if(!openness||!width)return 0;*openness=*width=0;
 if(!points||innerCount<4||outerCount<4||leftCount<2||rightCount<2||innerCount>128||outerCount>128||leftCount>128||rightCount>128||floatCount!=(innerCount+outerCount+leftCount+rightCount)*2||!std::isfinite(rollDegrees))return 0;
 for(int i=0;i<floatCount;i++)if(!std::isfinite(points[i]))return 0;
 float angle=rollDegrees*3.14159265358979323846f/180.f,c=cosf(angle),s=sinf(angle);
 auto extent=[&](int first,int count,bool vertical){float low=1e9f,high=-1e9f;for(int i=first;i<first+count;i++){float x=points[i*2],y=points[i*2+1],v=vertical?-s*x+c*y:c*x+s*y;low=fminf(low,v);high=fmaxf(high,v);}return high-low;};
 auto center=[&](int first,int count,int axis){float sum=0;for(int i=first;i<first+count;i++)sum+=points[i*2+axis];return sum/count;};
 int l=innerCount+outerCount,r=l+leftCount;float eyeDistance=hypotf(center(l,leftCount,0)-center(r,rightCount,0),center(l,leftCount,1)-center(r,rightCount,1));
 float span=extent(0,innerCount,false);if(eyeDistance<3||span<1)return 0;
 *openness=extent(0,innerCount,true)/span;*width=extent(innerCount,outerCount,false)/eyeDistance;return 1;
}
int fp_camera_count(){@autoreleasepool{return (int)Cameras().count;}}
int fp_camera_name(int index,char* destination,int capacity){@autoreleasepool {if(!destination||capacity<1)return 0;destination[0]=0;auto cameras=Cameras();if(index<0||index>=(int)cameras.count)return 0;const char* name=cameras[index].localizedName.UTF8String;snprintf(destination,capacity,"%s",name);return 1;}}
int fp_start(int index){
 @autoreleasepool {
  if(![NSBundle.mainBundle objectForInfoDictionaryKey:@"NSCameraUsageDescription"]){Clear(-5);return 0;}
  uint64_t token=++generation;wanted=true;Clear(1);AVAuthorizationStatus status=[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
  if(status==AVAuthorizationStatusAuthorized){[Camera() start:index token:token];return 1;}
  if(status==AVAuthorizationStatusNotDetermined){[AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted){if(generation.load()!=token||!wanted)return;if(granted)[Camera() start:index token:token];else{wanted=false;Clear(-1);}}];return 1;}
  wanted=false;Clear(-1);return 0;
 }
}
void fp_stop(){wanted=false;++generation;Clear(0);FPCamera* camera=Camera();dispatch_async(camera.sessionQueue,^{[camera cleanup];});}
int fp_poll(FPFrame* frame){if(!frame)return 0;std::lock_guard<std::mutex> lock(stateMutex);*frame=latest;frame->age=capturedAt>0?(float)(Clock()-capturedAt):9999;return 1;}
int fp_copy_preview(uint8_t* destination,int capacity,int* width,int* height,int* seq){if(!destination||!width||!height||!seq)return 0;std::lock_guard<std::mutex> lock(stateMutex);*width=latest.width;*height=latest.height;*seq=latest.sequence;if(preview.empty()||capacity<(int)preview.size())return 0;memcpy(destination,preview.data(),preview.size());return (int)preview.size();}
int fp_analyse_bgra(const uint8_t* pixels,int length,int width,int height,int stride,FPFrame* frame){
 if(!frame)return 0;*frame={};if(!pixels||width<1||height<1||width>1920||height>1080||stride<width*4||(int64_t)stride*height>length)return 0;
 CVPixelBufferRef image=nullptr;CVReturn result=CVPixelBufferCreateWithBytes(kCFAllocatorDefault,width,height,kCVPixelFormatType_32BGRA,(void*)pixels,stride,nullptr,nullptr,nullptr,&image);if(result!=kCVReturnSuccess)return 0;
 frame->width=width;frame->height=height;frame->status=2;int ok=Detect(image,*frame);CVPixelBufferRelease(image);return ok;
}
}
