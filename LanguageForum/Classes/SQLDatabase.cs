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
using System.IO;
using LanguageForum.Model;
using System.Collections;

namespace LanguageForum.Classes
{
    public class SQLDatabase
    {
        public SQLiteConnection Database { get; set; }

        public SQLDatabase()
        {
            Database = GetConnection();
            Install();
        }

        private SQLiteConnection GetConnection()
        {
            var sqliteFilename = "LanguageForum5.db3";

            // Documents folder
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal).ToString();

            var path = Path.Combine(documentsPath, sqliteFilename);

            // Create the connection
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());

            var conn = new SQLite.SQLiteConnection(path);
            // Return the database connection
            return conn;
        }

        internal Lesson GetLastLesson()
        {
            return Database.Table<Lesson>().OrderByDescending(l => l.Created).ToList().FirstOrDefault(l => !l.Closed.HasValue);
        }

        public void Install()
        {
            Database.CreateTable<Lesson>();
            Database.CreateTable<QRCodeItem>();
            Database.CreateTable<Student>();
        }

        public void InsertOrder(Lesson order)
        {
            Database.Insert(order);
        }

        public void UpdateOrder(Lesson order)
        {
            Database.Update(order);
        }

        public void Delete(Lesson order)
        {
            Database.Delete(order);
        }

        public void Insert(object o)
        {
            Database.Insert(o);
        }

        internal IEnumerable<QRCodeItem> GetQRWithoutPersonInfo(Guid lessonID)
        {
            return Database.Table<QRCodeItem>().Where(p => !p.PersonInfoUpdated && p.Lesson == lessonID).ToList();
        }

        internal QRCodeItem GetTeacherWithoutPersonInfo(Guid lessonID)
        {
            return Database.Table<QRCodeItem>().FirstOrDefault(p => p.Type == QRCodeUserType.Teacher && !p.PersonInfoUpdated && p.Lesson == lessonID);
        }

        public void InsertAll(IEnumerable data)
        {
            Database.InsertAll(data);
        }

        public void Delete(object o)
        {
            Database.Delete(o);
        }

        public void Update(object o)
        {
            Database.Update(o);
        }

        public IEnumerable<Lesson> GetAllLessons()
        {
            return Database.Table<Lesson>().ToList();
        }

        internal Lesson GetLastLesson(LessonType lessonType)
        {
            return Database.Table<Lesson>().OrderByDescending(l => l.Created).ToList().FirstOrDefault(l => l.LessonType == lessonType && !l.Closed.HasValue);
        }

        internal IEnumerable<Lesson> GetReadyToCloseLessons()
        {
            var endDateTime60minutes = DateTime.Now.AddMinutes(-60);
            var endDateTime90minutes = DateTime.Now.AddMinutes(-90);

            return Database.Table<Lesson>().Where(l => l.Closed == null &&
                                                       ((l.LessonType == LessonType.Lesson60minutes && l.Created < endDateTime60minutes) ||
                                                        (l.LessonType == LessonType.Lesson90minutes && l.Created < endDateTime90minutes)));
        }

        internal QRCodeItem GetTeacher(Guid id)
        {
            return Database.Table<QRCodeItem>().OrderByDescending(q => q.Created).ToList().FirstOrDefault(t => t.Lesson == id && t.Type == QRCodeUserType.Teacher);
        }

        internal IEnumerable<QRCodeItem> GetStudents(Guid id)
        {
            return Database.Table<QRCodeItem>().Where(t => t.Lesson == id && t.Type == QRCodeUserType.Student);
        }

        internal Lesson GetLesson(Guid lessonId)
        {
            return Database.Table<Lesson>().FirstOrDefault(t => t.Id == lessonId);
        }

        internal QRCodeItem GetLessonScanInfo(Guid lessonId)
        {
            return Database.Table<QRCodeItem>().OrderBy(t => t.Created).ToList().FirstOrDefault(t => t.Lesson == lessonId && t.Longitude.HasValue);
        }

        internal void DeleteSendedLessons()
        {
            var lessons = Database.Table<Lesson>().Where(t => t.Sended);
            foreach (var lesson in lessons)
            {
                var items = Database.Table<QRCodeItem>().Where(i => i.Lesson == lesson.Id);
                foreach (var item in items)
                {
                    Database.Delete(item);
                }

                Database.Delete(lesson);
            }
        }

        internal void DeleteSendedStudents()
        {
            var students = Database.Table<Student>().Where(t => t.Sended);
            foreach (var student in students)
            {
                Database.Delete(student);
            }
        }

        internal IEnumerable<Lesson> GetReadyToSendLessons()
        {
            return Database.Table<Lesson>().ToList().Where(i => i.Closed.HasValue && !i.Sended);
        }

        internal IEnumerable<Student> GetReadyToSendStudents()
        {
            return Database.Table<Student>().ToList().Where(i => !i.Sended);
        }
    }

}