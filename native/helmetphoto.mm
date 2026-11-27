#import <Cocoa/Cocoa.h>
#import <UniformTypeIdentifiers/UniformTypeIdentifiers.h>
#import <ImageIO/ImageIO.h>
#include <string>
// Unity invokes and polls these on its main thread. No network, cache, or persistence.
static int pickerStatus=0;
static std::string selectedPath;
static NSOpenPanel* activePanel=nil;
extern "C" {
void hp_activate_player(){if(![NSThread isMainThread])return;[NSApp activateIgnoringOtherApps:YES];for(NSWindow* window in NSApp.windows)if([window.title isEqualToString:@"Event Horizon"]){[window makeKeyAndOrderFront:nil];break;}}

int hp_begin_pick(){
 if(![NSThread isMainThread] || pickerStatus==1)return 0;
 hp_activate_player();selectedPath.clear();pickerStatus=1;
 activePanel=[NSOpenPanel openPanel];
 activePanel.title=@"Choose a local helmet photo";
 activePanel.message=@"PNG or JPEG. Used in memory only; never uploaded or saved by this app.";
 activePanel.canChooseFiles=YES;activePanel.canChooseDirectories=NO;activePanel.allowsMultipleSelection=NO;
 activePanel.allowedContentTypes=@[UTTypePNG,UTTypeJPEG];activePanel.allowsOtherFileTypes=NO;
 [activePanel beginWithCompletionHandler:^(NSModalResponse response){
  if(response==NSModalResponseOK && activePanel.URL){selectedPath=activePanel.URL.fileSystemRepresentation;pickerStatus=2;}
  else pickerStatus=3;
  activePanel=nil;hp_activate_player();
 }];
 return 1;
}
int hp_pick_status(){return pickerStatus;}
const char* hp_pick_path(){return selectedPath.c_str();}
void hp_clear_result(){if(pickerStatus!=1){selectedPath.clear();pickerStatus=0;}}
// Validate metadata before Unity decompresses. No image data is copied into the plugin.
int hp_image_info(const char* path,int* width,int* height){
 @autoreleasepool {
  if(!path || !width || !height)return 0;
  NSURL* url=[NSURL fileURLWithPath:[NSString stringWithUTF8String:path]];
  CGImageSourceRef source=CGImageSourceCreateWithURL((__bridge CFURLRef)url,nullptr);
  if(!source)return 0;
  CFStringRef type=CGImageSourceGetType(source);
  bool supported=type&&(CFEqual(type,(__bridge CFStringRef)UTTypePNG.identifier)||CFEqual(type,(__bridge CFStringRef)UTTypeJPEG.identifier));
  NSDictionary* info=(__bridge_transfer NSDictionary*)CGImageSourceCopyPropertiesAtIndex(source,0,nullptr);
  *width=[info[(__bridge NSString*)kCGImagePropertyPixelWidth] intValue];*height=[info[(__bridge NSString*)kCGImagePropertyPixelHeight] intValue];
  CFRelease(source);
  if(!supported)return -1;
  if(*width<1 || *height<1)return 0;
  if(*width>8192 || *height>8192 || (long long)*width**height>24000000)return -2;
  return 1;
 }
}
}
