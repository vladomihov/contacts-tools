Directory.SetCurrentDirectory(@"C:\Data\Misc\FacebookContacts");

var export = new FacebookContacts();
await export.ExportAsync();