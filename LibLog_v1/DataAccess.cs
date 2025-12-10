using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Management.Core;
using Windows.Storage;

namespace LibLog_v1
{
    public static class DataAccess
    {
        private static StorageFolder LocalFolder => ApplicationData.Current.LocalFolder;

        public async static Task InitDatabase()
        {
            StorageFile storageFile = await LocalFolder.CreateFileAsync("LibLog_v1.db", CreationCollisionOption.OpenIfExists);
            StorageFile dbFile = storageFile;
            string dbpath = Path.Combine(LocalFolder.Path, "LibLog_v1.db");

            using var db = new SqliteConnection($"Filename={dbpath}");
            db.Open();
            string tableCommand = @"CREATE TABLE IF NOT EXISTS Book (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ISBN TEXT,
                    Author TEXT,
                    Title TEXT,
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


        // Copilot Generated Tag Methods
        public static void AddTag(string isbn, string tag)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var updateCommand = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "UPDATE Book SET Tags = CASE WHEN Tags IS NULL OR Tags = '' THEN @Tag ELSE Tags || ',' || @Tag END WHERE ISBN = @ISBN"
                };
                updateCommand.Parameters.AddWithValue("@Tag", tag);
                updateCommand.Parameters.AddWithValue("@ISBN", isbn);
                updateCommand.ExecuteNonQuery();
            }
        }

        public static void RemoveTag(string isbn, string tag)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var updateCommand = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "UPDATE Book SET Tags = TRIM(REPLACE(',' || Tags || ',', ',' || @Tag || ',', ',')) WHERE ISBN = @ISBN"
                };
                updateCommand.Parameters.AddWithValue("@Tag", tag);
                updateCommand.Parameters.AddWithValue("@ISBN", isbn);
                updateCommand.ExecuteNonQuery();
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

        public static async Task<byte[]> GetCoverImage(string isbn)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "LibLog_v1.db");
            
            using (var db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand(
                    "SELECT CoverImage FROM Book WHERE ISBN = @ISBN", db);
                selectCommand.Parameters.AddWithValue("@ISBN", isbn);

                var result = selectCommand.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return Array.Empty<byte>();

                return result as byte[] ?? Array.Empty<byte>();
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
                            if (coverData != null && coverData.Length >0)
                            {
                                BitmapImage? bitmapImage = await MainWindow.BytesToBitmapImage(coverData);
                                coverImage = bitmapImage;
                            }
                        }
                        string tagsRaw = string.Empty;
                        if (!reader.IsDBNull(5))
                        {
                            tagsRaw = reader.GetString(5) ?? string.Empty;
                        }
                        
                        ObservableCollection<string> tagsCollection;
                        if (string.IsNullOrWhiteSpace(tagsRaw))
                        {
                            tagsCollection = new ObservableCollection<string>();
                        }
                        else
                        {
                            var parsed = tagsRaw.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => t.Length > 0)
                                .ToList();
                            tagsCollection = new ObservableCollection<string>(parsed);
                        }

                        var book = new Book
                        {
                            Id = reader.GetInt32(0),
                            ISBN = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Author = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Title = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            CoverImage = coverImage ?? new BitmapImage(),
                            Tags = tagsCollection
                        }; 

                        books.Add(book);
                    }
                }
            }

            return books;
        }
    }
}
