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

namespace LanguageForum.Classes
{
    public class ManagementService
    {
        private static AndroidService.AndroidService InitAndroidService()
        {
            AndroidService.AndroidService ans = new AndroidService.AndroidService()
            {
                Url = "http://lf.medevid.cloud/AndroidService.asmx"
            };
            return ans;
        }

        private static AndroidService.AndroidService _service = null;

        public static AndroidService.AndroidService AndroidService
        {
            get
            {
                if(_service==null)
                {
                    _service = InitAndroidService();
                }
                return _service;
            }
        }

    }
}