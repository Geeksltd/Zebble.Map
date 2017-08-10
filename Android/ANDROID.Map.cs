namespace Zebble.Plugin
{
    using System;

    partial class Map
    {
        partial class Annotation
        {
            internal string Id;

            public bool Flat { get; set; }

            public Annotation()
            {
                Id = Guid.NewGuid().ToString();
            }
        }
    }
}