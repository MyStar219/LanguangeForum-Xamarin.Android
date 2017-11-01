using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;

namespace LanguageForum.Model
{
    public class QRCodeItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public Guid Lesson { get; set; }

        public string Code { get; set; }
        public QRCodeUserType Type { get; set; }
        public DateTime Created { get; set; }

        public string LocationDescription { get; set; }
        public string Provider { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public bool? IsFromMockProvider { get; }
        public float? Accuracy { get; set; }
        public bool? HasBearing { get; }
        public bool? HasAltitude { get; }
        public bool? HasAccuracy { get; }
        public long? ElapsedRealtimeNanos { get; set; }
        public float? Bearing { get; set; }
        public double? Altitude { get; set; }
        public float? Speed { get; set; }
        public bool? HasSpeed { get; }
        public long? Time { get; set; }

        public string PersonInfo { get; set; }
        public bool PersonInfoUpdated { get; set; }
    }

}