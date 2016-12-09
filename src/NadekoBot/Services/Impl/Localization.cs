namespace NadekoBot.Services
{
    public class Localization
    {
        public string this[string key] => LoadCommandString(key);

        public static string LoadCommandString(string key)
        {
            string toReturn = Resources.CommandStrings.ResourceManager.GetString(key);
            return string.IsNullOrWhiteSpace(toReturn) ? key : toReturn;
        }

        //private static string GetCommandString(string key)
        //{
        //    return key;
            //var resx = new List<DictionaryEntry>();
            //var fs = new StreamReader(File.OpenRead("./Strings.resx"));
            //Console.WriteLine(fs.ReadToEnd());
            //using (var reader = new ResourceReader(fs.BaseStream))
            //{
            //    List<DictionaryEntry> existing = new List<DictionaryEntry>();
            //    foreach (DictionaryEntry item in reader)
            //    {
            //        existing.Add(item);
            //    }
            //    var existingResource = resx.Where(r => r.Key.ToString() == key).FirstOrDefault();
            //    if (existingResource.Key == null)
            //    {
            //        resx.Add(new DictionaryEntry() { Key = key, Value = key });
            //    }
            //    else
            //        return existingResource.Value.ToString();
            //}
            //using (var writer = new ResourceWriter(new FileStream("./Strings.resx", FileMode.OpenOrCreate)))
            //{
            //    resx.ForEach(r =>
            //    {
            //        writer.AddResource(r.Key.ToString(), r.Value.ToString());
            //    });
            //    writer.Generate();
            //}
            //return key;
        //}
    }
}
