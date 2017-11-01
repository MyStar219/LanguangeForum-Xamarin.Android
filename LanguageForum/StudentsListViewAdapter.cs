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
using LanguageForum.Model;
using System.Collections.ObjectModel;

namespace LanguageForum
{
    class StudentsListViewAdapter : BaseAdapter<QRCodeItem>
    {
        public ObservableCollection<QRCodeItem> Items;
        private Context context;

        public override QRCodeItem this[int position] => Items[position];

        public override int Count => Items.Count;

        public StudentsListViewAdapter(Context context, ObservableCollection<QRCodeItem> items)
        {
            this.Items = items;
            this.context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View row = convertView;
            if (row == null)
            {
                row = LayoutInflater.From(context).Inflate(Resource.Layout.StudentListView, null, false);
            }

            TextView txtName = row.FindViewById<TextView>(Resource.Id.txtCode);
            txtName.Text = Items[position].PersonInfo;

            return row;
        }
    }
}