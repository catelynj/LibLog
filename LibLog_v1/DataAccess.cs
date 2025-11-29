using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Management.Core;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace LibLog_v1
{
    public static class DataAccess
    {
        private static StorageFolder LocalFolder => ApplicationData.Current.LocalFolder;

        public async static Task InitDatabase()
        {
            //TODO: ADD CONFIG FILE TO REMOVE HARDCODED DB FILE PATH

            // C:\Users\coryc\AppData\Local\Packages\88d3411d-d18f-46f6-9a6e-834b0156028c_r3twpzxgkaxrw\LocalState

            StorageFile storageFile = await LocalFolder.CreateFileAsync("LibLog_v1.db", CreationCollisionOption.OpenIfExists);
            StorageFile dbFile = storageFile;
            string dbpath = Path.Combine(LocalFolder.Path, "LibLog_v1.db");

            using var db = new SqliteConnection($"Filename={dbpath}");
            db.Open();
            string tableCommand = @"CREATE TABLE IF NOT EXISTS Book (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ISBN INTEGER,
                    Author TEXT,
                    Title INTEGER,
                    CoverImage BLOB,
                    Tags TEXT
                    )";

            var createTable = new SqliteCommand(tableCommand, db);
            createTable.ExecuteNonQuery();
        }

        public static async Task AddData(string isbn)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            
            // Get the book data from API
            var (title, author, coverImage) = await APIHandler.RetrieveData(isbn);
            
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                var insertCommand = new SqliteCommand
                {
                    Connection = db,

                    // tags here will be empty so it doesn't need to be included in the insert

                    CommandText = "INSERT INTO Book (ISBN, Author, Title, CoverImage, Tags) VALUES (@ISBN, @Author, @Title, @CoverImage, @Tags);"
                };

                insertCommand.Parameters.AddWithValue("@ISBN", isbn);
                insertCommand.Parameters.AddWithValue("@Author", author);
                insertCommand.Parameters.AddWithValue("@Title", title);
                insertCommand.Parameters.AddWithValue("@CoverImage", coverImage);
                insertCommand.Parameters.AddWithValue("@Tags", "");

                insertCommand.ExecuteNonQuery();
            }
        }

        public static void RemoveData(string isbn)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                var removeCommand = new SqliteCommand();
                removeCommand.Connection = db;

                removeCommand.CommandText = "DELETE FROM Book WHERE ISBN = @ISBN";
                removeCommand.Parameters.AddWithValue("@ISBN", isbn);

                removeCommand.ExecuteNonQuery();
            }
        }

        public static Task<byte[]> GetCoverImage(string isbn)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand(
                    "SELECT CoverImage FROM Book WHERE ISBN = @ISBN", db);
                selectCommand.Parameters.AddWithValue("@ISBN", isbn);

                var result = selectCommand.ExecuteScalar();
                return Task.FromResult<byte[]>(result == DBNull.Value ? Array.Empty<byte>() : result as byte[]);
            }
        }

        public static async Task<List<Book>> GetAllBooks()
        {
            var books = new List<Book>();
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand(
                    "SELECT Id, ISBN, Author, Title, CoverImage, Tags FROM Book", db);

                using (SqliteDataReader reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        BitmapImage coverImage = null;
                        if (!reader.IsDBNull(4))
                        {
                            byte[] coverData = (byte[])reader[4];
                            if (coverData.Length > 0)
                            {
                                BitmapImage? bitmapImage = await MainWindow.BytesToBitmapImage(coverData);
                                coverImage = bitmapImage;
                            }
                        }
                        var book = new Book
                        {
                            Id = reader.GetInt32(0),
                            ISBN = reader.GetString(1),
                            Author = reader.GetString(2),
                            Title = reader.GetString(3),
                            CoverImage = coverImage ?? new BitmapImage(),
                            Tags = reader.GetBoolean(5) ? reader.GetString(5).Split(',') : Array.Empty<string>()
                        }; 

                        books.Add(book);
                    }
                }
            }

            return books;
        }
    }
}
