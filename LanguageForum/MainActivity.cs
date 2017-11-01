using System;
using Android.App;
using Android.OS;
using Android.Gms.Location;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Util;
using Android.Widget;
using Android.Locations;
using System.Collections.Generic;
using LanguageForum.Model;
using ZXing.Mobile;
using ZXing;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.PM;
using System.Collections.ObjectModel;
using LanguageForum.Classes;
using Android.Content;
using Android.Views.InputMethods;
using static Android.App.ActivityManager;
using System.Timers;
using Android.Net;

namespace LanguageForum
{
    [Activity(Label = "Language Forum", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Material.Light.NoActionBar")]
    public class MainActivity : Activity
    {
        private Timer timer;

        private SQLDatabase database;
        private LessonType actualLessonType = LessonType.NotSet;
        private Button btnStartLesson60Minutes;
        private Button btnStartLesson90Minutes;
        private Button btnRegisterNewStudent;
        private ResponseReceiver receiver;

        private ConnectivityManager connectivityManager;
        private ActivityManager activityManager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Globals.CurrentMainActivity = this;

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            InitDatabase();
            InitGUI();
            InitButtons();
            InitReciever();
            InitTimer();
           
            CloseEndedLessons();

            connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
            activityManager = (ActivityManager)GetSystemService(ActivityService);
        }

      
        private void InitTimer()
        {
            timer = new Timer()
            {
                Enabled = true,
                Interval = 10000
            };

            timer.Elapsed += OnTimeEvent;
        }

        private void OnTimeEvent(object sender, ElapsedEventArgs e)
        {
            CloseEndedLessons();

            RunOnUiThread(() =>
            {
                StartIntentService();
            });
        }

        private void CloseEndedLessons()
        {
            var list = database.GetReadyToCloseLessons();

            if (list.Count() > 0)
            {
                foreach (var lesson in list)
                {
                    lesson.Closed = DateTime.Now;
                    database.Update(lesson);
                }

                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Lesson has ended.", ToastLength.Long).Show();
                    InitGUI();
                });
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            timer.Dispose();
        }

        private void InitReciever()
        {
            IntentFilter filter = new IntentFilter(ResponseReceiver.ACTION_RESP);
            filter.AddCategory(Intent.CategoryDefault);
            receiver = new ResponseReceiver();
            RegisterReceiver(receiver, filter);
        }

        protected override void OnResume()
        {
            base.OnResume();

            InitGUI();
        }

        private void InitButtons()
        {
            btnStartLesson60Minutes.Click += (sender, e) =>
            {
                CheckLessonType(LessonType.Lesson60minutes);
            };

            btnStartLesson90Minutes.Click += (sender, e) =>
            {
                CheckLessonType(LessonType.Lesson90minutes);
            };

            btnRegisterNewStudent.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(RegisterNewStudentActivity));
                StartActivity(intent);

                //Classes.ManagementService.AndroidService.RegisterNewStudent(studentInfo.FirstName, studentInfo.LastName, DateTime.Now, studentInfo.Email, studentInfo.Phone, studentInfo.City, "CZ", studentInfo.Street, studentInfo.ZipCode, studentInfo.TIN);

            };
        }

        private void CheckLessonType(LessonType lessonType)
        {
            if (actualLessonType != LessonType.NotSet && actualLessonType != lessonType)
            {
                var alert = new AlertDialog.Builder(this)
                                                        .SetPositiveButton("Yes", (_sender, args) =>
                                                        {
                                                            var intent = new Intent(this, typeof(LessonActivity));
                                                            intent.PutExtra("LessonType", (int)lessonType);
                                                            StartActivity(intent);
                                                        })
                                                        .SetNegativeButton("No", (_sender, args) =>
                                                        {
                                                        })
                                                        .SetMessage("Another lesson is active.\nStart new one?")
                                                        .SetTitle("Start new lesson")
                                                        .Show();
            }
            else
            {
                var intent = new Intent(this, typeof(LessonActivity));
                intent.PutExtra("LessonType", (int)lessonType);
                StartActivity(intent);
            }

        }

        private void InitGUI()
        {
            btnStartLesson60Minutes = FindViewById<Button>(Resource.Id.btnStartLesson60Minutes);
            btnStartLesson90Minutes = FindViewById<Button>(Resource.Id.btnStartLesson90Minutes);
            btnRegisterNewStudent = FindViewById<Button>(Resource.Id.btnRegisterNewStudent);

            var lesson = database.GetLastLesson();
            if (lesson != null)
            {
                actualLessonType = lesson.LessonType;

                var estimatedSeconds = (DateTime.Now - lesson.Created).TotalSeconds;

                switch (actualLessonType)
                {
                    case LessonType.NotSet:
                        break;
                    case LessonType.Lesson60minutes:
                        if (estimatedSeconds > 3600)
                        {
                            lesson.Closed = DateTime.Now;
                        }
                        else
                        {
                            btnStartLesson60Minutes.Text = btnStartLesson60Minutes.Text.Replace("Start", "Continue");
                            btnStartLesson90Minutes.Text = btnStartLesson90Minutes.Text.Replace("Continue", "Start");
                        }
                        break;
                    case LessonType.Lesson90minutes:
                        if (estimatedSeconds > 5400)
                        {
                            lesson.Closed = DateTime.Now;
                        }
                        else
                        {
                            btnStartLesson90Minutes.Text = btnStartLesson90Minutes.Text.Replace("Start", "Continue");
                            btnStartLesson60Minutes.Text = btnStartLesson60Minutes.Text.Replace("Continue", "Start");
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                actualLessonType = LessonType.NotSet;
                btnStartLesson60Minutes.Text = btnStartLesson60Minutes.Text.Replace("Continue", "Start");
                btnStartLesson90Minutes.Text = btnStartLesson90Minutes.Text.Replace("Continue", "Start");
            }

        }

        private void InitDatabase()
        {
            database = new SQLDatabase();
        }

        #region IntentService
        public void StartIntentService()
        {
            // Must be connected a not already running
            if (!IsConnected() || IsMyServiceRunning()) return;

            String strInputMsg = "";
            Intent msgIntent = new Intent(this, typeof(BackgroundIntentService));
            msgIntent.PutExtra(BackgroundIntentService.PARAM_IN_MSG, strInputMsg);
            StartService(msgIntent);
        }

        private bool IsConnected()
        {
            return connectivityManager.ActiveNetworkInfo?.IsConnected ?? false;
        }

        private bool IsMyServiceRunning()
        {
            foreach (RunningServiceInfo service in activityManager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.EndsWith("BackgroundIntentService"))
                {
                    return true;
                }
            }
            return false;
        }

        public class ResponseReceiver : BroadcastReceiver
        {
            public const String ACTION_RESP = "com.mamlambo.intent.action.MESSAGE_PROCESSED";

            public override void OnReceive(Context context, Intent intent)
            {
                String text = intent.GetStringExtra(BackgroundIntentService.PARAM_OUT_MSG);
                // Toast.MakeText(Globals.CurrentMainActivity, text, ToastLength.Long).Show();
            }
        }
        #endregion
    }
}


