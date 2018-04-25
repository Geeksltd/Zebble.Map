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