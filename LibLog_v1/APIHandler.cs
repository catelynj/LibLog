using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LibLog_v1
{
    public static class APIHandler
    {
        /**
         JSON Return Example: 

         {'items':
            [{'match': 'exact',
                'status': 'full access'}],
                'itemURL': 'http://www.archive.org/stream/TheArtOfCommunity',
                'cover': {'large': 'http://covers.openlibrary.org/b/id/6223071-L.jpg',
                            'medium': 'http://covers.openlibrary.org/b/id/6223071-M.jpg',
                            'small': 'http://covers.openlibrary.org/b/id/6223071-S.jpg'},
                'fromRecord': '/books/OL23747519M',
                'ol-edition-id': 'OL23747519M',
                'ol-work-id': 'OL15328717W'}],
         'records':
             {'/books/OL23747519M':
                {'data': { ... }
                    'isbns': ['0596156715',
                                '9780596156718'],
                    'publishDates': ['August 2009'],
                    'recordURL': 'http://openlibrary.org/books/OL23747519M'}}}
         
        items -> list of matching or similar books with same ISBN, may be empty
        items[0] -> what i want, 'match' should be either 'exact' or 'similar' (similar means its a different edition)
        status - doesnt really matter
        itemURL -> link to online scan or borrow page on openlibrary
        cover -> links, most likely will use the Medium
        fromRecord -> not needed
        ol-edition-id -> not needed
        ol-work-id -> not needed
         
        records.data -> returns url, title, authors, identifiers, genre/subjects, covers, etc.
         */
        private static async Task<byte[]> getCover(string coverUri)
        {
            var httpClient = (App.Current as App)?.HttpClient;
            if (httpClient == null || string.IsNullOrEmpty(coverUri))
                return Array.Empty<byte>();

            try
            {
                return await httpClient.GetByteArrayAsync(coverUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cover download error: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        public static async Task<(string Title, string Author, byte[] CoverImage)> RetrieveData(string isbn)
        {
            var httpClient = (App.Current as App)?.HttpClient;

            if (httpClient != null)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "LibLog/1.0 (cxj15@pct.edu)");

                    var response = await httpClient.GetAsync($"http://openlibrary.org/api/volumes/brief/isbn/{isbn}.json");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var jsonObj = JObject.Parse(jsonString);

                        var records = jsonObj["records"] as JObject;
                        JToken? firstRecord = null;
                        
                        if (records != null && records.Properties().Any())
                        {
                            var firstProperty = records.Properties().First();
                            firstRecord = firstProperty.Value?["data"];
                        }

                        if(firstRecord != null)
                        {
                            string title = firstRecord["title"]?.ToString() ?? "Unknown";
                            var authors = firstRecord["authors"]?.Select(a => a["name"]?.ToString()).Where(name => !string.IsNullOrEmpty(name));
                            string author = authors != null && authors.Any() ? string.Join(", ", authors) : "Unknown Author";

                            string coverUrl = jsonObj["items"]?.FirstOrDefault()?["cover"]?["medium"]?.ToString() ?? "";
                            var coverImage = await getCover(coverUrl);

                            return (title, author, coverImage);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API Exception: {ex.Message}");
                }
            }

            return ("Title Not Available", "Author Not Available", Array.Empty<byte>());
        }
    }
}
