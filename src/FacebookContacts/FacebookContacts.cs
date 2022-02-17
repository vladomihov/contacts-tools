using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

public class FacebookContacts
{
    private const string FACEBOOK_LINK = "https://www.facebook.com/";
    private const string ID_CACHE = "facebook_id_cache.csv";
    private const string ID_EXPORT = "fb_cosy_contacts.csv";

    private IDictionary<string, string> _idCache;
    private HttpClient _idClient;

    private record class CacheItem([Index(0)]string Link, [Index(1)]string FacebookId);
    private record class ContactItem(string Link, string Id, string Name);

    public FacebookContacts()
    {
        _idCache = ReadIdCache();

        _idClient = new HttpClient();
        _idClient.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.29.0");
    }

    private IDictionary<string, string> ReadIdCache()
    {
        using var reader = new StreamReader(ID_CACHE);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<CacheItem>();

        var idCache = records.ToDictionary(x => x.Link, x => x.FacebookId);
        return idCache;
    }

    private void AppendToCache(string link, string id)
    {
        using var writer = new StreamWriter(path: ID_CACHE, append: true);
        writer.WriteLine();

        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        var record = new CacheItem(Link: link, FacebookId: id);
        csv.WriteRecord(record);
        
    }

    public async Task ExportCosyAsync()
    {
        var contacts = await LoadContactsAsync();

        Console.WriteLine($"{contacts.Count()} records loaded.");

        using var writer = new StreamWriter(path: ID_EXPORT, append: false);
        foreach (var contact in contacts)
        {
            writer.WriteLine($"{contact.Id};{contact.Name}");
        }

        Console.WriteLine($"File '{ID_EXPORT}' is ready.");
    }

    private async Task<IEnumerable<ContactItem>> LoadContactsAsync()
    {
        var contacts = new List<ContactItem>();

        var fileContent = await File.ReadAllTextAsync("fb_contacts.html");
        var items = fileContent.Split("<div data-visualcompletion=\"ignore-dynamic\" style=\"padding-left: 8px; padding-right: 8px;\">", StringSplitOptions.RemoveEmptyEntries);
        foreach (var contactHtml in items)
        {
            var contactLink = ExtractLink(contactHtml);
            if (contactLink != null)
            {
                var contactId = await ExtractIdAsync(contactLink);
                var contactName = ExtractName(contactHtml);

                var contact = new ContactItem(contactLink, contactId, contactName);
                contacts.Add(contact);
            }
        }

        return contacts;
    }

    private string? ExtractLink(string contactHtml)
    {
        var idLinkIndex = contactHtml.IndexOf(FACEBOOK_LINK);
        if (idLinkIndex == -1)
        {
            return null;
        }

        var idEnd = contactHtml.IndexOf('"', idLinkIndex);

        var contactLink = contactHtml.Substring(idLinkIndex, idEnd - idLinkIndex);
        return contactLink;
    }

    private async Task<string> ExtractIdAsync(string contactLink)
    {
        if (_idCache.TryGetValue(contactLink, out var cachedId))
        {
            return cachedId;
        }

        string? contactId = null;

        const string ID_LINK = FACEBOOK_LINK + "profile.php?id=";
        if (contactLink.StartsWith(ID_LINK))
        {
            contactId = contactLink.Substring(ID_LINK.Length);
        }
        else
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://lookup-id.com/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "fburl", contactLink },
                { "check", "Lookup" },
            });

            var response = await _idClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            const string BEFORE_CODE = "<span id=\"code\">";
            const string AFTER_CODE = "</span>";
            int beforeIndex = html.IndexOf(BEFORE_CODE);
            if (beforeIndex == -1)
            {
                throw new Exception($"Cannot extract Facebook ID for '{contactLink}'");
            }

            var afterIndex = html.IndexOf(AFTER_CODE, beforeIndex);
            contactId = html.Substring(beforeIndex + BEFORE_CODE.Length, afterIndex - beforeIndex - BEFORE_CODE.Length);
        }

        AppendToCache(contactLink, contactId);

        return contactId;
    }

    private string ExtractName(string contactHtml)
    {
        const string NAME_PREFIX = "<svg aria-label=\"";
        var nameIndex = contactHtml.IndexOf(NAME_PREFIX);
        if (nameIndex == -1)
        {
            throw new Exception($"Cannot extract contact name.");
        }

        var nameStart = nameIndex + NAME_PREFIX.Length;
        var nameEnd = contactHtml.IndexOf('"', nameStart);

        var contactName = contactHtml.Substring(nameStart, nameEnd - nameStart);
        contactName = Transliterate(contactName);

        return contactName;
    }

    private string Transliterate(string text)
    {
        return text
            .Replace("а", "a")
            .Replace("б", "b")
            .Replace("в", "v")
            .Replace("г", "g")
            .Replace("д", "d")
            .Replace("е", "e")
            .Replace("ж", "zh")
            .Replace("з", "z")
            .Replace("и", "i")
            .Replace("й", "y")
            .Replace("к", "k")
            .Replace("л", "l")
            .Replace("м", "m")
            .Replace("н", "n")
            .Replace("о", "o")
            .Replace("п", "p")
            .Replace("р", "r")
            .Replace("с", "s")
            .Replace("т", "t")
            .Replace("у", "u")
            .Replace("ф", "f")
            .Replace("х", "h")
            .Replace("ц", "ts")
            .Replace("ч", "ch")
            .Replace("ш", "sh")
            .Replace("щ", "sht")
            .Replace("ъ", "a")
            .Replace("ь", "y")
            .Replace("ю", "yu")
            .Replace("я", "ya")
            .Replace("А", "A")
            .Replace("Б", "B")
            .Replace("В", "V")
            .Replace("Г", "G")
            .Replace("Д", "D")
            .Replace("Е", "E")
            .Replace("Ж", "Zh")
            .Replace("З", "Z")
            .Replace("И", "I")
            .Replace("Й", "Y")
            .Replace("К", "K")
            .Replace("Л", "L")
            .Replace("М", "M")
            .Replace("Н", "N")
            .Replace("О", "O")
            .Replace("П", "P")
            .Replace("Р", "R")
            .Replace("С", "S")
            .Replace("Т", "T")
            .Replace("У", "U")
            .Replace("Ф", "F")
            .Replace("Х", "H")
            .Replace("Ц", "Ts")
            .Replace("Ч", "Ch")
            .Replace("Ш", "Sh")
            .Replace("Щ", "Sht")
            .Replace("Ъ", "A")
            .Replace("Ь", "Y")
            .Replace("Ю", "Yu")
            .Replace("Я", "Ya");
    }
}