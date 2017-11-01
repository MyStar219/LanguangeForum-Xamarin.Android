using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LanguageForum
{
    public class RegisterStudentArgs : EventArgs
    {
        private string firstName;

        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }

        private string lastName;

        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }

        private string email;

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private string city;

        public string City
        {
            get { return city; }
            set { city = value; }
        }

        private string phone;

        public string Phone
        {
            get { return phone; }
            set { phone = value; }
        }

        private string tin;

        public string TIN
        {
            get { return tin; }
            set { tin = value; }
        }

        private string street;

        public string Street
        {
            get { return street; }
            set { street = value; }
        }

        private string zipCode;

        public string ZipCode
        {
            get { return zipCode; }
            set { zipCode = value; }
        }


        public RegisterStudentArgs(string firstName, string lastName, string street, string city, string zipcode, string phone, string email, string tin) : base()
        {
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            Street = street;
            City = city;
            ZipCode = zipcode;
            Email = email;
            TIN = tin;
        }
    }

    public class RegisterStudentDialog : DialogFragment
    {
        private EditText txtFirstName;
        private EditText txtLastName;
        private EditText txtStreet;
        private EditText txtCity;
        private EditText txtZipCode;
        private EditText txtEmail;
        private EditText txtPhone;
        private EditText txtTIN;

        private Button btnRegisterStudent;

        public event EventHandler<RegisterStudentArgs> RegisterNewStudentCompleted;

        public override void OnCreate(Bundle savedInstanceState)
        {
            Dialog?.Window.RequestFeature(WindowFeatures.NoTitle);

            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.RegisterStudent, container, false);

            txtFirstName = view.FindViewById<EditText>(Resource.Id.txtFirstName);
            txtLastName = view.FindViewById<EditText>(Resource.Id.txtLastName);
            txtStreet = view.FindViewById<EditText>(Resource.Id.txtStreet);
            txtCity = view.FindViewById<EditText>(Resource.Id.txtCity);
            txtZipCode = view.FindViewById<EditText>(Resource.Id.txtZipCode);
            txtPhone = view.FindViewById<EditText>(Resource.Id.txtPhone);
            txtEmail = view.FindViewById<EditText>(Resource.Id.txtEmail);
            txtTIN = view.FindViewById<EditText>(Resource.Id.txtICO);

            btnRegisterStudent = view.FindViewById<Button>(Resource.Id.btnRegisterNewStudent);

            btnRegisterStudent.Click += BtnRegisterStudent_Click;

            return view;
        }

        private void BtnRegisterStudent_Click(object sender, EventArgs e)
        {
            if (IsInputsValid())
            {
                RegisterNewStudentCompleted.Invoke(this, new RegisterStudentArgs(txtFirstName.Text, txtLastName.Text, txtStreet.Text, txtCity.Text, txtZipCode.Text, txtPhone.Text, txtEmail.Text, txtTIN.Text));
               
                this.Dismiss();
            }
            else
            {

                new AlertDialog.Builder(Activity).SetTitle("Input error")
                                     .SetMessage("Inputs are not filled corectly!")
                                     .Show();

            }
        }

        private bool IsInputsValid()
        {
            var rtn = true;

            return rtn;
        }
    }
}