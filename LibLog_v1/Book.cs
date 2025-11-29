using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace LibLog_v1
{
    public class Book
    {
        public int Id { get; set; }
        public required string ISBN { get; set; }
        public required string Author { get; set; }
        public required string Title { get; set; }
        public required BitmapImage CoverImage { get; set; }
        public required string[] Tags { get; set; }
    }
}
