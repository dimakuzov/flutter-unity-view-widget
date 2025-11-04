//
//  NSObject+LocationProxy.h
//  Unity-iPhone
//
//  Created by Voltaire Villafuerte on 7/7/20.
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>


@interface LocationPlugin : NSObject<CLLocationManagerDelegate>
// Adopt CLLocationManagerDelegate protocol
{
    CLLocationManager *locationManager;
    NSString *callbackTarget;
    bool isReverseGeocoding;
}

+ (LocationPlugin *)sharedInstance;

@end

// LocationPlugin class definition
@implementation LocationPlugin

// singleton
static LocationPlugin *sharedInstance = nil;
+ (id)sharedInstance
{
    @synchronized(self)
    {
        if(sharedInstance == nil)
        {
            sharedInstance = [[self alloc] init];
        }
    }
    return sharedInstance;
}

// Start getting location information
- (BOOL)startUpdatingLocation:(NSString *)newCallbackTarget
{
    // cache callback
    callbackTarget = newCallbackTarget;
    
    if(locationManager == nil)
    {
        locationManager = [[CLLocationManager alloc] init];
    }

    // Confirm that the location information service is valid and licensed
    BOOL isEnabledAndAuthorized = NO;
    if([CLLocationManager locationServicesEnabled])
    {
        CLAuthorizationStatus status = [CLLocationManager authorizationStatus];
        if(status == kCLAuthorizationStatusAuthorizedAlways ||
           status == kCLAuthorizationStatusAuthorizedWhenInUse)
        {
            isEnabledAndAuthorized = YES;
        }
    }
    if(!isEnabledAndAuthorized)
    {
        // Request user authorization when the location information service is invalid and not licensed
        [locationManager requestWhenInUseAuthorization];
        return NO;
    }
    
    // Start getting location information
    locationManager.delegate = self;
    locationManager.desiredAccuracy = kCLLocationAccuracyBest;
    [locationManager startUpdatingLocation];

    return YES;
}

// stop getting location information
- (void)stopUpdatingLocation
{
    if(locationManager != nil)
    {
        [locationManager stopUpdatingLocation];
    }
}

#pragma  Mark - method implementation of CLLocationManagerDelegate protocol

// Called when updating location information
- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray *)locations
{
    if(isReverseGeocoding)
    {
        return;
    }
    
    // Reverse geocoding based on the acquired location information to get the address info.
    isReverseGeocoding = YES;
    
    CLLocation *location = [locations lastObject];
    CLGeocoder *geocoder = [[CLGeocoder alloc] init];
    [geocoder reverseGeocodeLocation:location
                   completionHandler:^(NSArray *placemarks, NSError *error) {
        isReverseGeocoding = NO;

        NSString *name = @"";
        NSString *street = @"";
        NSString *city = @"";
        NSString *state = @"";
        NSString *neighborhood = @"";
        NSString *postalCode = @"";
        NSString *country = @"";
        if(placemarks.count >= 1)
        {
            CLPlacemark *placemark = [placemarks firstObject];
   
            name = placemark.name;
            street = [NSString stringWithFormat:@"%@ %@", placemark.subThoroughfare, placemark.thoroughfare];
            neighborhood = placemark.subLocality;
            if ([neighborhood length] == 0) {
                neighborhood = placemark.locality;
            }
            if ([neighborhood length] == 0) {
                neighborhood = placemark.administrativeArea;
            }
            city = placemark.locality;
            if ([city length] == 0) {
                city = placemark.subAdministrativeArea;
            }
            state = placemark.administrativeArea;
            postalCode = placemark.postalCode;
            country = placemark.country;
        }
        
        // Create a string passed as a parameter
        NSString *parameter = [NSString stringWithFormat:@"%f\t%f\t%f\t%f\t%@\t%@\t%@\t%@\t%@\t%@\t%@",
                               location.coordinate.latitude,
                               location.coordinate.longitude,
                               location.speed, location.horizontalAccuracy,
                               name, street,
                               neighborhood, city, state, postalCode,
                               country];

        // Call Unity's OnUpdateLocation method with the UnitySendMessage method
        UnitySendMessage([callbackTarget cStringUsingEncoding:NSUTF8StringEncoding],
                         "OnUpdateLocation",
                         [parameter cStringUsingEncoding:NSUTF8StringEncoding]);
    }];
}

// other necessary methods
- (void)locationManager:(CLLocationManager *)manager
      didDetermineState:(CLRegionState)state forRegion:(CLRegion *)region {}

- (void)locationManagerDidPauseLocationUpdates:(CLLocationManager *)manager {}

- (void)locationManagerDidResumeLocationUpdates:(CLLocationManager *)manager {}

@end

#pragma  Mark - interface

// To avoid C++ naming smash, declare with C linkage
extern "C" {
    // Interface used to call the method to get the location information
    BOOL _startUpdatingLocation(const char *callbackTarget)
    {
        LocationPlugin *instance = [LocationPlugin sharedInstance];
        @synchronized(instance)
        {
            return [instance startUpdatingLocation:
                [NSString stringWithUTF8String:callbackTarget]];
        }
    }

    // Interface for calling the method of stopping the acquisition of location information
    void _stopUpdatingLocation()
    {
        LocationPlugin *instance = [LocationPlugin sharedInstance];
        @synchronized(instance)
        {
            [instance stopUpdatingLocation];
        }
    }
}
