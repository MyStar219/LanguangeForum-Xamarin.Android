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
using System.Timers;
using Android.Media;
using Android.Content;
using Android.Provider;
using Java.IO;
using Android.Graphics;
using Environment = Android.OS.Environment;

namespace LanguageForum
{
    [Activity(Label = "LessonActivity", Icon = "@drawable/icon", Theme = "@android:style/Theme.Material.Light.NoActionBar")]
    public class LessonActivity : Activity, GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener, Android.Gms.Location.ILocationListener
    {
        MediaPlayer player;
        ProgressBar progressBarBottom;
        Timer timer;
        Timer sender;
        object _lock = new object();

        const int LessonLenght60Min = 3600;

        private MobileBarcodeScanner scanner;
        private ObservableCollection<QRCodeItem> students = new ObservableCollection<QRCodeItem>();
        private ListView studentsList;

        private SQLDatabase database;

        GoogleApiClient apiClient;
        LocationRequest locRequest;

        EditText txtDescription;
        TextView lblDescription;

        TextView txtLessonType;
        TextView txtTeacherName;
        TextView txtStudentsCaption;

        Button btnScanCode;
        Button btnCancelLesson;

        bool _isGooglePlayServicesInstalled;
        private Lesson actualLesson;
        private LessonType actualLessonType;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            timer.Dispose();

            if (sender != null)
                sender.Dispose();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Lesson);

            MobileBarcodeScanner.Initialize(Application);
            var lessonType = Intent.Extras.GetInt("LessonType");
            player = MediaPlayer.Create(this, Resource.Raw.sound);

            InitDatabase();
            InitGUI();
            InitLesson((LessonType)lessonType);
            InitProgressBar();
            InitGooglePlayServices();
        }

        private void InitProgressBar()
        {
            progressBarBottom.Max = actualLessonType == LessonType.Lesson60minutes ? LessonLenght60Min : actualLessonType == LessonType.Lesson90minutes ? 5400 : 0;
            progressBarBottom.Progress = (int)(DateTime.Now - actualLesson.Created).TotalSeconds;

            timer = new Timer()
            {
                Enabled = true,
                Interval = 1000
            };

            timer.Elapsed += OnTimeEvent;
        }

        private void InitDatabase()
        {
            database = new SQLDatabase();
        }

        private void InitLesson(LessonType lessonType)
        {
            actualLesson = database.GetLastLesson();
            if (actualLesson == null)
            {
                // zalozime novou
                var lesson = new Lesson()
                {
                    LessonType = lessonType,
                    Created = DateTime.Now
                };

                database.Insert(lesson);

                actualLesson = lesson;
            }
            else if (actualLesson.LessonType != lessonType)
            {
                EndLesson(actualLesson.Id);

                // zalozime novou
                var lesson = new Lesson()
                {
                    LessonType = lessonType,
                    Created = DateTime.Now
                };

                database.Insert(lesson);

                actualLesson = lesson;
            }

            actualLessonType = lessonType;

            txtLessonType.Text = actualLessonType == LessonType.Lesson60minutes ? "60 minutes lesson" : actualLessonType == LessonType.Lesson90minutes ? "90 minutes lesson" : "";

            LoadQRWithoutPersonInfo();
            LoadDataFromActualLesson();
        }

        private void LoadDataFromActualLesson()
        {
            var teacher = database.GetTeacher(actualLesson.Id);
            if (teacher != null)
                txtTeacherName.Text = "Teacher: " + teacher.PersonInfo;

            students.Clear();
            foreach (var student in database.GetStudents(actualLesson.Id).ToList())
            {
                students.Add(student);
            }

            txtDescription.Text = actualLesson.Description;

            UpdateStudentsCount();
        }

        private void InitGooglePlayServices()
        {
            _isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();

            if (_isGooglePlayServicesInstalled)
            {
                // pass in the Context, ConnectionListener and ConnectionFailedListener
                apiClient = new GoogleApiClient.Builder(this, this, this)
                    .AddApi(LocationServices.API).Build();

                // generate a location request that we will pass into a call for location updates
                locRequest = new LocationRequest();

            }
            else
            {
                Log.Error("OnCreate", "Google Play Services is not installed");
                Toast.MakeText(this, "Google Play Services is not installed", ToastLength.Long).Show();
                Finish();
            }
        }



        private void InitGUI()
        {
            txtTeacherName = FindViewById<TextView>(Resource.Id.txtTeacherName);
            txtStudentsCaption = FindViewById<TextView>(Resource.Id.txtStudentsCaption);
            btnScanCode = FindViewById<Button>(Resource.Id.btnScanCode);

            studentsList = FindViewById<ListView>(Resource.Id.studentsList);
            studentsList.Adapter = new StudentsListViewAdapter(this, students);

            txtLessonType = FindViewById<TextView>(Resource.Id.txtLessonType);
            progressBarBottom = FindViewById<ProgressBar>(Resource.Id.progressBarBottom);

            btnCancelLesson = FindViewById<Button>(Resource.Id.btnCancelLesson);
            lblDescription = FindViewById<TextView>(Resource.Id.lblDescription);
            txtDescription = FindViewById<EditText>(Resource.Id.txtDescription);
        }

        private void OnTimeEvent(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                progressBarBottom.IncrementProgressBy(1);
                CheckProgress(progressBarBottom.Progress);
            });
        }

        public void CheckProgress(int progress)
        {
            lock (_lock)
            {
                if (progress >= progressBarBottom.Max)
                {
                    Toast.MakeText(this, "Lesson has ended.", ToastLength.Long).Show();
                    timer.Dispose();
                    EndLesson(actualLesson.Id);
                    Finish();
                }
            }
        }

        private void EndLesson(Guid lessonId, bool canceled = false)
        {
            var lesson = database.GetLesson(lessonId);
            lesson.Canceled = canceled;
            lesson.Closed = DateTime.Now;
            database.Update(lesson);


        }

        private void SenderTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var l in database.GetReadyToSendLessons())
            {
                EndLesson(l.Id);
            }
        }

        private void mListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Toast.MakeText(this, students[e.Position].Code, ToastLength.Long);
        }

        bool IsGooglePlayServicesInstalled()
        {
            int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info("MainActivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error("ManActivity", "There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString);

                // Show error dialog to let user debug google play services
            }
            return false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            Log.Debug("OnResume", "OnResume called, connecting to client...");

            apiClient.Connect();

            btnScanCode.Click += btnScanCode_Click;
            btnCancelLesson.Click += btnCancelLesson_Click;

            txtDescription.TextChanged += txtDescription_TextChanged;

            #region LOCATION BUTTONS
            // Clicking the first button will make a one-time call to get the user's last location
            /*
            button.Click += delegate
            {
                if (apiClient.IsConnected)
                {
                    button.Text = "Getting Last Location";

                    Location location = LocationServices.FusedLocationApi.GetLastLocation(apiClient);
                    if (location != null)
                    {
             
                        Log.Debug("LocationClient", "Last location printed");
                    }
                }
                else
                {
                    Log.Info("LocationClient", "Please wait for client to connect");
                }
            };

            // Clicking the second button will send a request for continuous updates
            button2.Click += async delegate
            {
                if (apiClient.IsConnected)
                {
                    button2.Text = "Requesting Location Updates";

                    // Setting location priority to PRIORITY_HIGH_ACCURACY (100)
                    locRequest.SetPriority(100);

                    // Setting interval between updates, in milliseconds
                    // NOTE: the default FastestInterval is 1 minute. If you want to receive location updates more than 
                    // once a minute, you _must_ also change the FastestInterval to be less than or equal to your Interval
                    locRequest.SetFastestInterval(500);
                    locRequest.SetInterval(1000);

                    Log.Debug("LocationRequest", "Request priority set to status code {0}, interval set to {1} ms",
                        locRequest.Priority.ToString(), locRequest.Interval.ToString());

                    // pass in a location request and LocationListener
                    await LocationServices.FusedLocationApi.RequestLocationUpdates(apiClient, locRequest, this);
                    // In OnLocationChanged (below), we will make calls to update the UI
                    // with the new location data
                }
                else
                {
                    Log.Info("LocationClient", "Please wait for Client to connect");
                }
            };
            */
            #endregion
        }

        private void txtDescription_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            actualLesson.Description = txtDescription.Text;
            database.Update(actualLesson);
        }

        private void btnCancelLesson_Click(object sender, EventArgs e)
        {
            btnCancelLesson.Click -= btnCancelLesson_Click;
            EndLesson(actualLesson.Id, true);
            timer.Dispose();
            Finish();
        }

        private void btnScanCode_Click(object sender, EventArgs e)
        {
            btnScanCode.Click -= btnScanCode_Click;

            if (scanner == null)
                scanner = new MobileBarcodeScanner();

            scanner.AutoFocus();
            scanner.TopText = "You can scan teacher or student QR code.";
            scanner.BottomText = "Scan QRCode.";
            scanner.ScanContinuously(new MobileBarcodeScanningOptions()
            {
                AutoRotate = true,
                PureBarcode = true,
                UseNativeScanning = true,
                DelayBetweenContinuousScans = 3000,
                PossibleFormats = (new[] { BarcodeFormat.QR_CODE }).ToList()

            }, HandleScanResult);
        }

        private async void HandleScanResult(ZXing.Result code)
        {
            if (code != null)
            {
                var identification = code.Text.Substring(0, 1).ToUpper();
                var type = identification == "T" ? QRCodeUserType.Teacher : identification == "S" ? QRCodeUserType.Student : QRCodeUserType.Unnknown;
                var location = GetLastKnownLocation();
                Address address = null;
                string locationDesc = "";
                try
                {
                    address = await ReverseGeocodeCurrentLocation(location);
                    locationDesc= FormatAddress(address);
                }
                catch { }

                switch (type)
                {
                    case QRCodeUserType.Student:
                        RunOnUiThread(() =>
                        {

                            if (students.FirstOrDefault(sq => sq.Code.ToLower() == code.Text.Substring(1).ToLower()) == null)
                            {
                                var student = new QRCodeItem()
                                {
                                    Type = QRCodeUserType.Student,
                                    Code = code.Text.Substring(1),
                                    Lesson = actualLesson.Id,
                                    PersonInfo = code.Text
                                };

                                if (location != null)
                                {
                                    // TODO
                                    //student.LocationDescription = await ReverseGeocodeCurrentLocation(location), 
                                    student.LocationDescription = locationDesc;
                                    student.Accuracy = location.Accuracy;
                                    student.Altitude = location.Altitude;
                                    student.Bearing = location.Bearing;
                                    student.ElapsedRealtimeNanos = location.ElapsedRealtimeNanos;
                                    student.Latitude = location.Latitude;
                                    student.Longitude = location.Longitude;
                                    student.Provider = location.Provider;
                                    student.Speed = location.Speed;
                                    student.Time = location.Time;
                                }

                                database.Insert(student);

                                if (!students.Any(s => s.Code == student.Code))
                                {
                                    students.Add(student);
                                }

                                RefreshStudentList();

                                player.Start();
                            }
                        });
                        break;

                    case QRCodeUserType.Teacher:
                        var teacher = new QRCodeItem()
                        {
                            Type = QRCodeUserType.Teacher,
                            Code = code.Text.Substring(1),
                            Lesson = actualLesson.Id,
                            PersonInfo = code.Text
                        };

                        if (location != null)
                        {
                            if (address != null)
                            {
                                teacher.LocationDescription = locationDesc;
                            }
                            teacher.Accuracy = location.Accuracy;
                            teacher.Altitude = location.Altitude;
                            teacher.Bearing = location.Bearing;
                            teacher.ElapsedRealtimeNanos = location.ElapsedRealtimeNanos;
                            teacher.Latitude = location.Latitude;
                            teacher.Longitude = location.Longitude;
                            teacher.Provider = location.Provider;
                            teacher.Speed = location.Speed;
                            teacher.Time = location.Time;
                        }

                        database.Insert(teacher);
                        player.Start();

                        RunOnUiThread(() =>
                        {
                            txtTeacherName.Text = "Teacher: " + teacher.PersonInfo;
                        });
                        break;
                }

                LoadQRWithoutPersonInfo();

                RunOnUiThread(() => UpdateStudentsCount());
            }
        }

        private async void LoadQRWithoutPersonInfo()
        {
            await UpdateStudentAndTeacherPersonInfo();
            //await UpdateTeacherPersonInfo();
            //await UpdateStudentsPersonInfo();

            RunOnUiThread(() =>
            {
                LoadDataFromActualLesson();
                RefreshStudentList();
            });
        }

        private async Task UpdateStudentAndTeacherPersonInfo()
        {
            var list = database.GetQRWithoutPersonInfo(actualLesson.Id);
            foreach (var qr in list)
            {
                UpdatePersonInfo(qr);
                database.Update(qr);
            }

        }

        private async Task UpdateTeacherPersonInfo()
        {
            var teacher = database.GetTeacherWithoutPersonInfo(actualLesson.Id);
            if (teacher != null)
            {
                UpdatePersonInfo(teacher);
                RunOnUiThread(() =>
                {
                    txtTeacherName.Text = "Teacher: " + teacher.PersonInfo;
                });
            }
        }

        private async Task UpdateStudentsPersonInfo()
        {
            var list = students.Where(p => !p.PersonInfoUpdated && p.Type == QRCodeUserType.Student).ToList();
            foreach (var qr in list)
            {
                UpdatePersonInfo(qr);
            }

            RunOnUiThread(() => RefreshStudentList());
        }

        private void UpdatePersonInfo(QRCodeItem qr)
        {
            try
            {
                qr.PersonInfo = ManagementService.AndroidService.GetPersonDetail(qr.PersonInfo ?? "");
                qr.PersonInfoUpdated = true;

                //database.Update(qr);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => Toast.MakeText(this, ex.ToString(), ToastLength.Long));
            }
        }

        private void RefreshStudentList()
        {
            var adapter = (StudentsListViewAdapter)studentsList.Adapter;
            studentsList.Adapter = adapter;
            adapter.NotifyDataSetChanged();
        }

        private string FormatAddress(Address address)
        {
            try
            {
                return address.GetAddressLine(0);
            }
            catch { }
            int m = address.MaxAddressLineIndex;
            List<string> lines = new List<string>();
            for (int i = 0; i < m; i++)
            {
                lines.Add(address.GetAddressLine(i));
            }
            return string.Join(", ", lines.ToArray());
        }

        private void UpdateStudentsCount()
        {
            txtStudentsCaption.Text = "Student" + (students.Count > 1 ? "s" : "") + " (" + students.Count + ")";
        }

        Location GetLastKnownLocation()
        {
            var _locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Coarse
            };
            IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
            {
                var _locationProvider = acceptableLocationProviders.First();
                var loc = _locationManager.GetLastKnownLocation(_locationProvider);
                return loc;
            }
            return null;
        }

        async Task<Address> ReverseGeocodeCurrentLocation(Location loc)
        {
            Geocoder geocoder = new Geocoder(this);
            IList<Address> addressList =
                await geocoder.GetFromLocationAsync(loc.Latitude, loc.Longitude, 10);
            Address address = addressList.FirstOrDefault();
            return address;
        }


        protected override async void OnPause()
        {
            base.OnPause();
            Log.Debug("OnPause", "OnPause called, stopping location updates");

            txtDescription.TextChanged -= txtDescription_TextChanged;
            btnCancelLesson.Click -= btnCancelLesson_Click;

            if (apiClient.IsConnected)
            {
                // stop location updates, passing in the LocationListener
                await LocationServices.FusedLocationApi.RemoveLocationUpdates(apiClient, this);

                apiClient.Disconnect();
            }
        }


        ////Interface methods

        public void OnConnected(Bundle bundle)
        {
            // This method is called when we connect to the LocationClient. We can start location updated directly form
            // here if desired, or we can do it in a lifecycle method, as shown above 

            // You must implement this to implement the IGooglePlayServicesClientConnectionCallbacks Interface
            Log.Info("LocationClient", "Now connected to client");
        }

        public void OnDisconnected()
        {
            // This method is called when we disconnect from the LocationClient.

            // You must implement this to implement the IGooglePlayServicesClientConnectionCallbacks Interface
            Log.Info("LocationClient", "Now disconnected from client");
        }

        public void OnConnectionFailed(ConnectionResult bundle)
        {
            // This method is used to handle connection issues with the Google Play Services Client (LocationClient). 
            // You can check if the connection has a resolution (bundle.HasResolution) and attempt to resolve it

            // You must implement this to implement the IGooglePlayServicesClientOnConnectionFailedListener Interface
            Log.Info("LocationClient", "Connection failed, attempting to reach google play services");
        }

        public void OnLocationChanged(Location location)
        {
            // This method returns changes in the user's location if they've been requested

            // You must implement this to implement the Android.Gms.Locations.ILocationListener Interface
            Log.Debug("LocationClient", "Location updated");

        }

        public void OnConnectionSuspended(int i)
        {

        }

    }
}