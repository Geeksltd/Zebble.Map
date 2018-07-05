[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Map/master/Shared/Icon.png "Zebble.Map"


## Zebble.Map

![logo]

A Zebble plugin which make you able to use the Zebble.Plugin.Map component to render a Map. It supports adding annotations too.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Map.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Map/)

> To add a map to your page, you can use the built-in map object in Zebble. You can set the Center and Zoom level of the map to define the starting position. After that the user can drag the round or zoom in and out. You can also add an annotations to the map. Each annotation should specify the location, icon, text and some other details. Internally it is rendered as a native map for each operating system and each annotation will be displayed as a marker on the map

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Map/](https://www.nuget.org/packages/Zebble.Map/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

To show the map in a page you can use this code:
```xml
<Map Id="MyMap" Center="51.5074, 0.1278" ZoomLevel="13" />
```
Also, you can below code to add annotation to your map.
```xml
<Map Id="MyMap" Center="51.5074, 0.1278" ZoomLevel="13">
   <Map.Annotation
          Location="51.5074, 0.1278"
          IconPath="Images/Icons/MyPin.png"  IconWidth="25"  IconHeight="50"
          Title="Marker 1" Subtitle="Some description..." />
    <Map.Annotation … />
</Map>
```
You can add annotations using C# too.
```csharp
foreach(var item in ... /*usually API or database call*/)
{
     MyMap.Add(new Map.Annotation
       { 
            Location = new Zebble.Services.GeoLocation(item.Latitude, item.Longitude),
            Title = item.Name
       });
}
```

#### Map interactions

You can specify which interactions are enabled on the map by setting the following boolean properties on the map:

**Pannable:** Whether the user can drag the map around. This is true by default. <br>
**Zoomable:** Whether the user can pinch to zoom in and out. This is true by default. <br>
**Rotatable:** Whether the user can use rotation gestures. This is true by default. <br>
**ShowZoomControls:** Whether the UI controls should be displaed to change the Zoom. This is false by default (as gesture zooming is preferred). <br>

#### Get a Google Maps API key

To use the Google Maps Android API, you must register your app project on the Google API Console and get a Google API key which you can add to your app. For details, see the guide to getting an API key.

### Platform Specific Notes


#### Android:

##### Manifest Permissions

In order for the map and location services to work you should add the necessary permissions to the manifest files. The following lists the relevant permissions you might need:

**AndroidManifest.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:versionName="4.5" package="com.xamarin.docs.android.mapsandlocationdemo2" android:versionCode="6">
    <uses-sdk android:minSdkVersion="14" android:targetSdkVersion="17" />

    <!-- Google Maps for Android v2 requires OpenGL ES v2 -->
    <uses-feature android:glEsVersion="0x00020000" android:required="true" />

    <!-- We need to be able to download map tiles and access Google Play Services-->
    <uses-permission android:name="android.permission.INTERNET" />

    <!-- Allow the application to access Google web-based services. -->
    <uses-permission android:name="com.google.android.providers.gsf.permission.READ_GSERVICES" />

    <!-- Google Maps for Android v2 will cache map tiles on external storage -->
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

    <!-- Google Maps for Android v2 needs this permission so that it may check the connection state as it must download data -->
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <!-- Permission to receive remote notifications from Google Play Services -->
    <!-- Notice here that we have the package name of our application as a prefix on the permissions. -->
    <uses-permission android:name="<PACKAGE NAME>.permission.MAPS_RECEIVE" />
    <permission android:name="<PACKAGE NAME>.permission.MAPS_RECEIVE" android:protectionLevel="signature" />

    <!-- These are optional, but recommended. They will allow Maps to use the My Location provider. -->
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />


    <application android:label="@string/app_name">
        <!-- Put your Google Maps V2 API Key here. -->
        <meta-data android:name="com.google.android.maps.v2.API_KEY" android:value="YOUR_API_KEY" />
        <meta-data android:name="com.google.android.gms.version" android:value="@integer/google_play_services_version" />
    </application>
</manifest>
```

### Properties
| Property     | Type         | Android | iOS | Windows |
| :----------- | :----------- | :------ | :-- | :------ |
| ZoomLevel           | int          | x       | x   | x       |
| Zoomable  | bool | x | x | x |
| ShowZoomControls   | bool | x | x | x |
| Rotatable   | bool | x | x | x |
| Pannable   | bool | x | x | x |
| VisibleRegion | Span | x | x | x |
| Center | GeoLocation | x | x | x |
| Annotations | IEnumarable<Annotations&gt; | x | x | x |

### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| UserChangedRegion             | AsyncEvent    | x       | x   | x       |

### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| Add         | Task| annotations -> Annotation[] | x       | x   | x       |
| Remove         | Task| annotations -> Annotation[]| x       | x   | x       |
| ClearAnnotations         | Task| -| x       | x   | x       |