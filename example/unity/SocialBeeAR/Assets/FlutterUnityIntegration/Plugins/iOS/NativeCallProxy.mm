#import <Foundation/Foundation.h>
#import "NativeCallProxy.h"

id<NativeCallsProtocol> api = NULL;

char* MakeStringCopy (NSString* nsstring)
    {
        if (nsstring == NULL) {
            return NULL;
        }
        // convert from NSString to char with utf8 encoding
        const char* string = [nsstring cStringUsingEncoding:NSUTF8StringEncoding];
        if (string == NULL) {
            return NULL;
        }

        // create char copy with malloc and strcpy
        char* res = (char*)malloc(strlen(string) + 1);
        strcpy(res, string);
        return res;
    }

@implementation FrameworkLibAPI

+(void) registerAPIforNativeCalls:(id<NativeCallsProtocol>) aApi
{
    api = aApi;
}

@end

extern "C" {

void onClearMap(const char* mapId) {
        return [api onClearMap:[NSString stringWithUTF8String:mapId]];
    }

    void onDeleteMultiple(const char* activityIds) {
        return [api onDeleteMultiple:[NSString stringWithUTF8String:activityIds]];
    }

    void onWillUpdatePost(const char* activityId, const char* isCheckIn) {
        return [api onWillUpdatePost:[NSString stringWithUTF8String:activityId] chk:[NSString stringWithUTF8String:isCheckIn]];
    }

    void onWillUpdateActivitiesMapId(const char* mapId, const char* activityIds) {
        return [api onWillUpdateActivitiesMapId:[NSString stringWithUTF8String:mapId] ids:[NSString stringWithUTF8String:activityIds]];
    }

    void onActivitySubmitted(const char* json, const char* activityType) {
        return [api onActivitySubmitted:[NSString stringWithUTF8String:json] type:[NSString stringWithUTF8String:activityType]];
    }

    void onActivityCompleted(const char* json, const char* activityType) {
        return [api onActivityCompleted:[NSString stringWithUTF8String:json] type:[NSString stringWithUTF8String:activityType]];
    }

    void onActivityDeleted(const char* activityId) {
        return [api onActivityDeleted:[NSString stringWithUTF8String:activityId]];
    }

    void onAnchorDeleted(const char* anchorId) {
        return [api onAnchorDeleted:[NSString stringWithUTF8String:anchorId]];
    }

    void debugMessage(const char* message) {
        return [api debugMessage:[NSString stringWithUTF8String:message]];
    }

    void openGallery(const char* activityId) {
        return [api openGallery:[NSString stringWithUTF8String:activityId]];
    }

    void showNativePage(const char* page) {
        return [api showNativePage:[NSString stringWithUTF8String:page]];
    }

    void giveActivityData(const char* mapID, const char* activity){
        return [api giveActivityData:[NSString stringWithUTF8String:mapID] act:[NSString stringWithUTF8String:activity]];
    }

    void getCurrentUserAuthToken(const char* identifier, const char* callback){
        return [api getCurrentUserAuthToken:[NSString stringWithUTF8String:identifier] cb:[NSString stringWithUTF8String:callback]];
    }

    void getApiUrl(const char* identifier){
        return [api getApiUrl:[NSString stringWithUTF8String:identifier]];
    }

    void onWillGetImageKeywords(const char* contentPath, const char* getAll){
        return [api onWillGetImageKeywords:[NSString stringWithUTF8String:contentPath] ga:[NSString stringWithUTF8String:getAll]];
    }

    void onWillGetLocationInfo(const char* latitude, const char* longitude){
        return [api onWillGetLocationInfo:[NSString stringWithUTF8String:latitude] lon:[NSString stringWithUTF8String:longitude]];
    }

    void onSceneLoaded(const char* reference){
        return [api onSceneLoaded:[NSString stringWithUTF8String:reference]];
    }

    void saveContentPath(const char* contentPath){
        return [api saveContentPath:[NSString stringWithUTF8String:contentPath]];
    }

    void onLoadActivityFeed(const char* json) {
        return [api onLoadActivityFeed:[NSString stringWithUTF8String:json]];
    }

    void endAR() {
        return [api endAR];
    }

    const char* GetSettingsURL () {
        NSURL * url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
        return MakeStringCopy(url.absoluteString);
    }

}
