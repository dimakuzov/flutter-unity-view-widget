// [!] important set UnityFramework in Target Membership for this file
// [!]           and set Public header visibility

#import <Foundation/Foundation.h>

// NativeCallsProtocol defines protocol with methods you want to be called from managed
@protocol NativeCallsProtocol
@required
- (void) showHostMainWindow;
- (void) unloadPlayer;
- (void) quitPlayer;
// other methods

- (void) debugMessage:(NSString*)message;
- (void) openGallery:(NSString*)activityId;
- (void) showNativePage:(NSString*)page;
- (void) giveActivityData:(NSString*)mapID act:(NSString*)activity;
- (void) getCurrentUserAuthToken:(NSString*)identifier cb:(NSString*)callback;
- (void) getApiUrl:(NSString*)identifier;
- (void) onWillGetImageKeywords:(NSString*)contentPath ga:(NSString*)getAll;
- (void) onWillGetLocationInfo:(NSString*)latitude lon:(NSString*)longitude;
- (void) onSceneLoaded:(NSString*)reference;
- (void) saveContentPath:(NSString*)contentPath;
- (void) endAR;
- (void) onActivitySubmitted:(NSString*)json type:(NSString*)activityType;
- (void) onActivityCompleted:(NSString*)json type:(NSString*)activityType;
- (void) onActivityDeleted:(NSString*)activityId;
- (void) onAnchorDeleted:(NSString*)anchorId;
- (void) onWillUpdateActivitiesMapId:(NSString*)mapId ids:(NSString*)activityIds;
- (void) onWillUpdatePost:(NSString*)activityId chk:(NSString*)isCheckIn;
- (void) onClearMap:(NSString*)mapId;
- (void) onDeleteMultiple:(NSString*)ids;
- (void) onLoadActivityFeed:(NSString*)json;
@end

__attribute__ ((visibility("default")))
@interface FrameworkLibAPI : NSObject
// call it any time after UnityFrameworkLoad to set object implementing NativeCallsProtocol methods
+(void) registerAPIforNativeCalls:(id<NativeCallsProtocol>) aApi;

@end

    extern "C" {
    // Плагин
    void _showHostMainWindow();
    void _unloadPlayer();
    void _quitPlayer();

    // Ваши 19
    void onClearMap(const char* mapId);
    void onDeleteMultiple(const char* activityIds);
    void onWillUpdatePost(const char* activityId, const char* isCheckIn);
    void onWillUpdateActivitiesMapId(const char* mapId, const char* activityIds);
    void onActivitySubmitted(const char* json, const char* activityType);
    void onActivityCompleted(const char* json, const char* activityType);
    void onActivityDeleted(const char* activityId);
    void onAnchorDeleted(const char* anchorId);
    void debugMessage(const char* message);
    void openGallery(const char* activityId);
    void showNativePage(const char* page);
    void giveActivityData(const char* mapID, const char* activity);
    void getCurrentUserAuthToken(const char* identifier, const char* callback);
    void getApiUrl(const char* identifier);
    void onWillGetImageKeywords(const char* contentPath, const char* getAll);
    void onWillGetLocationInfo(const char* latitude, const char* longitude);
    void onSceneLoaded(const char* reference);
    void saveContentPath(const char* contentPath);
    void onLoadActivityFeed(const char* json);
    void endAR();
    const char* GetSettingsURL();
}

