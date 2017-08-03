namespace Zebble.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble;
    using Zebble.Services;

    partial class Map
    {
        public class Annotation
        {
            public string Title, SubTitle, Id, Content;
            public bool Draggable, Flat, Visible = true;

            public GeoLocation Location = new GeoLocation();
            public Icon Pin = new Icon();

            public AsyncEvent<Annotation> Tapped = new AsyncEvent<Annotation>();

            public object Native { get; internal set; }

            public class Icon
            {
                public string IconPath;
                public float Width = 48, Height = 92;

                internal async Task<ImageService.ImageProvider> GetProvider()
                {
                    var result = ImageService.GetImageProvider(IconPath, new Size(Width, Height), Stretch.Fit);
                    await result.Result();

                    return result;
                }
            }
        }
    }
}