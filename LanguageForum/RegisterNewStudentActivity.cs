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
using Android.Content.PM;
using LanguageForum.Classes;
using LanguageForum.Model;
using static Android.Widget.TextView;
using static Android.App.ActivityManager;
using Android.Media;
using Android.Provider;
using Java.IO;
using Android.Graphics;
using Environment = Android.OS.Environment;
using Android.Graphics.Drawables;
using Android.Database;
using System.IO;
using System.Threading.Tasks;

namespace LanguageForum
{

    [Activity(Label = "RegisterNewStudentActivity", Icon = "@drawable/icon", Theme = "@android:style/Theme.Material.Light.NoActionBar")]
    public class RegisterNewStudentActivity : Activity, DatePickerDialog.IOnDateSetListener
    {
        private const Int32 REQUEST_CAMERA = 0;
        private const Int32 SELECT_FILE = 1;

        private SQLDatabase database;

        private Bitmap bitmap;
        private ImageView studentPhoto;

        private EditText txtFirstName;
        private EditText txtLastName;
        private EditText txtStreet;
        private EditText txtCity;
        private EditText txtZipCode;
        private EditText txtEmail;
        private EditText txtPhone;
        private EditText txtTIN;
        private EditText txtBirthDate;
        private Spinner nativelanguage;
        private Spinner learningLanguage;

        private TextView lblFirstName;
        private TextView lblLastName;
        private TextView lblStreet;
        private TextView lblCity;
        private TextView lblZipCode;
        private TextView lblEmail;
        private TextView lblPhone;
        private TextView lblTIN;
        private TextView lblBirthDate;
        private TextView lblLanguage;

        private Button btnRegisterStudent;

        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);

            InitDatabase();

            SetContentView(Resource.Layout.RegisterNewStudent);
            // Create your application here

            FindViewById<EditText>(Resource.Id.txtBirthDate).Click += (sender, args) =>
            {
                var dialog = new DatePickerDialogFragment(this, new DateTime(1990, DateTime.Now.Month, DateTime.Now.Day), this);
                dialog.Show(FragmentManager, null);
            };

            InitPhoto();

            txtFirstName = FindViewById<EditText>(Resource.Id.txtFirstName);
            txtLastName = FindViewById<EditText>(Resource.Id.txtLastName);
            txtStreet = FindViewById<EditText>(Resource.Id.txtStreet);
            txtCity = FindViewById<EditText>(Resource.Id.txtCity);
            txtZipCode = FindViewById<EditText>(Resource.Id.txtZipCode);
            txtPhone = FindViewById<EditText>(Resource.Id.txtPhone);
            txtEmail = FindViewById<EditText>(Resource.Id.txtEmail);
            txtTIN = FindViewById<EditText>(Resource.Id.txtICO);
            txtBirthDate = FindViewById<EditText>(Resource.Id.txtBirthDate);

            lblFirstName = FindViewById<TextView>(Resource.Id.lblFirstName);
            lblLastName = FindViewById<TextView>(Resource.Id.lblLastName);
            lblStreet = FindViewById<TextView>(Resource.Id.lblStreet);
            lblCity = FindViewById<TextView>(Resource.Id.lblCity);
            lblZipCode = FindViewById<TextView>(Resource.Id.lblZipCode);
            lblPhone = FindViewById<TextView>(Resource.Id.lblPhone);
            lblEmail = FindViewById<TextView>(Resource.Id.lblEmail);
            lblTIN = FindViewById<TextView>(Resource.Id.lblTIN);
            lblBirthDate = FindViewById<TextView>(Resource.Id.lblBirthDate);
            lblLanguage = FindViewById<TextView>(Resource.Id.lblLanguage);
            
            var spinner = FindViewById<Spinner>(Resource.Id.spLanguage);
            nativelanguage = spinner;
            spinner.Prompt = "Choose language";
            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
            var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.languages, Android.Resource.Layout.SimpleSpinnerItem);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;

            var spinner2 = FindViewById<Spinner>(Resource.Id.spDesiredLanguage);
            spinner2.Prompt = "Choose language";
            spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
            var adapter2 = ArrayAdapter.CreateFromResource(this, Resource.Array.languages, Android.Resource.Layout.SimpleSpinnerItem);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner2.Adapter = adapter;
            learningLanguage = spinner2;
            btnRegisterStudent = FindViewById<Button>(Resource.Id.btnRegisterNewStudent);

            btnRegisterStudent.Click += BtnRegisterStudent_Click;
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var spinner = sender as Spinner;
            //txtLanguage.Text = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void BtnRegisterStudent_Click(object sender, EventArgs e)
        {
            var errorMesage = CheckInputs();

#if DEBUG
            errorMesage = String.Empty;
#endif

            if (String.IsNullOrEmpty(errorMesage))
            {
                var student = new Student()
                {
                    BirthDate = txtBirthDate.Text,
                    City = txtCity.Text,
                    Email = txtEmail.Text,
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    Street = txtStreet.Text,
                    Telephone = txtPhone.Text,
                    TIN = txtTIN.Text,
                    ZipCode = txtZipCode.Text,
                    Created = DateTime.Now,
                    LearningLanguage = learningLanguage.SelectedItem.ToString(),
                    Language = nativelanguage.SelectedItem.ToString(),
                    Photo = Convert.ToBase64String(ImageToArray(((BitmapDrawable)studentPhoto?.Drawable)?.Bitmap))
                };

#if DEBUG
#else
                database.Insert(student);
#endif

                var alert = new AlertDialog.Builder(this);
                alert.SetTitle("Registration completed");
                alert.SetMessage("Welcome to the language forum. Your registration was completed. Are you ready to learn?");
                alert.SetNeutralButton("Yes", delegate
                {
                    alert.Dispose();
                    alert = null;
                    Finish();
                });
                alert.Show();

                HideAlert(alert, 5000);

                

            }
            else
            {

                new AlertDialog.Builder(this).SetTitle("Validation error")
                                     .SetMessage(errorMesage)
                                     .SetCancelable(false)
                                     .SetPositiveButton("OK", delegate { })
                                     .Show();

            }
        }

        private async void HideAlert(AlertDialog.Builder alert, int delay)
        {
            await Task.Delay(delay);
            if (alert != null)
            {
                alert.Dispose();
            }
            Finish();
        }

        private byte[] ImageToArray(Bitmap bitmap)
        {
            byte[] bitmapData;
            using (var stream = new MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, stream);
                bitmapData = stream.ToArray();
            }
            return bitmapData;
        }

        private string CheckInputs()
        {
            var rtn = String.Empty;

            rtn += CheckInput(txtFirstName, lblFirstName);
            rtn += CheckInput(txtLastName, lblLastName);
            rtn += CheckInput(txtStreet, lblStreet);
            rtn += CheckInput(txtCity, lblCity);
            rtn += CheckInput(txtZipCode, lblZipCode);
            rtn += CheckInput(txtPhone, lblPhone);
            rtn += CheckInput(txtEmail, lblEmail);
            rtn += CheckEmail(txtEmail, lblEmail);

            return rtn;
        }

        private string CheckEmail(EditText txt, TextView lbl)
        {
            var rtn = String.Empty;

            if (!Android.Util.Patterns.EmailAddress.Matcher(txt.Text).Matches())
            {
                rtn = lbl.Text + " is not in correct format.\n";
            }

            return rtn;
        }

        private string CheckInput(EditText txt, TextView lbl)
        {
            var rtn = String.Empty;

            if (String.IsNullOrEmpty(txt.Text))
            {
                rtn = lbl.Text + " is required.\n";
            }

            return rtn;
        }

        public void OnDateSet(DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            var date = new DateTime(year, monthOfYear + 1, dayOfMonth);
            FindViewById<TextView>(Resource.Id.txtBirthDate).Text = date.ToString("yyyy-MM-dd");
        }

        private void InitDatabase()
        {
            database = new SQLDatabase();
        }

#region PHOTO
        private void InitPhoto()
        {
            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                studentPhoto = FindViewById<ImageView>(Resource.Id.imageView1);

                Button btnTakePhoto = FindViewById<Button>(Resource.Id.btnTakePhoto);
                btnTakePhoto.Click += TakeAPicture;

                Button btnSelectPhoto = FindViewById<Button>(Resource.Id.btnSelectPhoto);
                btnSelectPhoto.Click += SelectPicture;
            }
        }

        private void SelectPicture(object sender, EventArgs e)
        {
            var imageIntent = new Intent();
            imageIntent.SetType("image/*");
            imageIntent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(
                Intent.CreateChooser(imageIntent, "Select photo"), SELECT_FILE);
        }

        private void CreateDirectoryForPictures()
        {
            App._dir = new Java.IO.File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "Language Forum");
            if (!App._dir.Exists())
            {
                App._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            App._file = new Java.IO.File(App._dir, String.Format("Photo_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(App._file));
            StartActivityForResult(intent, REQUEST_CAMERA);
        }

#region Get the Path of Selected Image
        private string GetPathToImage(Android.Net.Uri uri)
        {
            ICursor cursor = ContentResolver.Query(uri, null, null, null, null);
            cursor.MoveToFirst();
            string documentId = cursor.GetString(0);
            documentId = documentId.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new[] { documentId }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
        }
#endregion

        protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            // Make it available in the gallery

            if (resultCode == Result.Ok)
            {
                string path = "";

                switch (requestCode)
                {
                    case SELECT_FILE:
                        studentPhoto.SetImageURI(data.Data);
                        bitmap = ((BitmapDrawable)studentPhoto?.Drawable)?.Bitmap;
                        path = GetPathToImage(data.Data);
                        break;

                    case REQUEST_CAMERA:
                        path = App._file.AbsolutePath;
                        Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                        Android.Net.Uri contentUri = Android.Net.Uri.FromFile(App._file);
                        mediaScanIntent.SetData(contentUri);
                        SendBroadcast(mediaScanIntent);

                        //bitmap = BitmapFactory.DecodeFile(path);


                        // Display in ImageView. We will resize the bitmap to fit the display
                        // Loading the full sized image will consume to much memory 
                        // and cause the application to crash.

                        // Dispose of the Java side bitmap.

                        break;

                }


                int width = 500;
                int height = 500;
                bitmap = path.LoadAndResizeBitmap(width, height);


                if (bitmap != null)
                {
                    ExifInterface exif = new ExifInterface(path);
                    int exifOrientation = exif.GetAttributeInt(
                    ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

                    int rotate = 0;

                    switch (exifOrientation)
                    {
                        case (int)Android.Media.Orientation.Rotate90:
                            rotate = 90;
                            break;

                        case (int)Android.Media.Orientation.Rotate180:
                            rotate = 180;
                            break;

                        case (int)Android.Media.Orientation.Rotate270:
                            rotate = 270;
                            break;
                    }

                    if (rotate != 0)
                    {
                        int w = bitmap.Width;
                        int h = bitmap.Height;

                        // Setting pre rotate
                        Matrix mtx = new Matrix();
                        mtx.PreRotate(rotate);

                        // Rotating Bitmap & convert to ARGB_8888, required by tess
                        using (var photo = Bitmap.CreateBitmap(bitmap, 0, 0, w, h, mtx, false))
                        {
                            studentPhoto.SetImageBitmap(photo.Copy(Bitmap.Config.Argb8888, true));
                        }
                    }
                    else
                    {
                        studentPhoto.SetImageBitmap(bitmap);
                    }

                    if (bitmap != null)
                        bitmap.Dispose();

                    GC.Collect();
                }

            }
        }

#endregion
    }

    public static class App
    {
        public static Java.IO.File _file;
        public static Java.IO.File _dir;
        public static Bitmap bitmap;
    }
}