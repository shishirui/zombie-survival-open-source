#import <Foundation/Foundation.h>
#include <string>
extern "C" const char* DeadDistrictLaunchArguments() {
    static std::string arguments;
    arguments = [[[[NSProcessInfo processInfo] arguments] componentsJoinedByString:@"\x1f"] UTF8String];
    return arguments.c_str();
}
