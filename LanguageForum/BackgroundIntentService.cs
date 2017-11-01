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
using static LanguageForum.RegisterNewStudentActivity;
using static LanguageForum.MainActivity;
using static Android.App.ActivityManager;
using System.Threading;
using LanguageForum.Classes;
using LanguageForum.Model;
using System.IO;
using Android.Graphics;

namespace LanguageForum
{
    [Service]
    public class BackgroundIntentService : IntentService
    {
        private SQLDatabase database;

        public const String PARAM_IN_MSG = "imsg";
        public const String PARAM_OUT_MSG = "omsg";

        public BackgroundIntentService() : base("SimpleIntentService")
        {
            database = new SQLDatabase();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return base.OnStartCommand(intent, flags, startId);
        }


        protected override void OnHandleIntent(Intent intent)
        {
            String msg = intent.GetStringExtra(PARAM_IN_MSG);
            String resultTxt = msg + " " + "ServiceIntent started at " + DateTime.Now.ToString("h:mm:ss");

            RegisterCompletedLessons();
            RegisterNewStudent();
       
            // Odeslani informace, ze jsem dopracoval :)
            Intent broadcastIntent = new Intent();
            broadcastIntent.SetAction(ResponseReceiver.ACTION_RESP);
            broadcastIntent.AddCategory(Intent.CategoryDefault);
            broadcastIntent.PutExtra(PARAM_OUT_MSG, resultTxt);
            SendBroadcast(broadcastIntent);
        }

       
        private void RegisterNewStudent()
        {
            var list = database.GetReadyToSendStudents();
            foreach (var student in list)
            {
                var sendResult = RegisterStudent(student);
                if (sendResult)
                {
                    student.Sended = true;
                    database.Update(student);
                }
            }

            // Neni potreba promazavat, pokud je seznam ke zpracovani prazdny
            if (list.Count() > 0)
                database.DeleteSendedStudents();
        }


        private void RegisterCompletedLessons()
        {
            var list = database.GetReadyToSendLessons();
            foreach (var lesson in list)
            {
                var lessonScanned = database.GetLessonScanInfo(lesson.Id);
                var lessonTeacher = database.GetTeacher(lesson.Id);
                var lessonStudents = database.GetStudents(lesson.Id);

                // Kontrola, zda ma lekce narok na registraci (ucitel musi byt zadan)
                if (lessonTeacher != null)
                {
                    var sendResult = RegisterLesson(lessonTeacher?.Latitude,
                                                    lessonTeacher?.Longitude,
                                                    lessonTeacher?.Accuracy,
                                                    lessonTeacher?.ElapsedRealtimeNanos,
                                                    lessonTeacher?.LocationDescription,
                                                    lessonTeacher?.Code,
                                                    lessonStudents?.Select(t => t.Code).ToArray(),
                                                    lesson.Created,
                                                    Enum.GetName(typeof(LessonType), lesson.LessonType),
                                                    (int)lesson.LessonType,
                                                    lesson.Description,
                                                    lesson.Canceled
                                                    );

                    // Odeslana ma byt, ale odpoved serveru neni OK
                    if (!sendResult) continue;

                }

                lesson.Sended = true;
                database.Update(lesson);
            }

            // Neni potreba promazavat, pokud je seznam ke zpracovani prazdny
            if (list.Count() > 0)
                database.DeleteSendedLessons();
        }

        private bool RegisterStudent(Student student)
        {
            var rtn = false;

            try
            {
                DateTime birthdate = DateTime.MaxValue;
                try
                {
                    birthdate = DateTime.ParseExact(student.BirthDate, "yyyy-MM-dd", null);
                }
                catch { }
                string photo = student.Photo;
                var result = ManagementService.AndroidService.RegisterNewStudent(student.FirstName, student.LastName, birthdate, student.Email, student.Telephone, student.City, student.Language, student.LearningLanguage, student.Street, student.ZipCode, student.TIN, photo);
                if (result == true)
                    rtn = true;
            }
            catch (Exception) { }

            return rtn;
        }

        public bool RegisterLesson(double? latitude, double? longitude, float? locationAccuracy, long? locationAge, string locationDescription, string teacher, string[] students, DateTime? lessonStart, string lessonDescription, decimal? lessonDuration, string description = "", bool canceled = false)
        {
            var rtn = false;

            try
            {
                var result = ManagementService.AndroidService.RegisterLesson(teacher, latitude, longitude, students, locationDescription ?? "", lessonStart, description ?? "", lessonDuration, canceled);
                if (result == "OK")
                    rtn = true;
            }
            catch (Exception) { }

            return rtn;
        }
    }
}