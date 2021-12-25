using Newtonsoft.Json;
using System.IO;
using UnturnedLegends.Models;

namespace UnturnedLegends.Managers
{
    public class DataManager
    {
        public string Path { get; set; }
        public PositionsData Data { get; set; }

        public DataManager()
        {
            Path = Plugin.Instance.Directory + "/Positions.json";
            Data = new PositionsData();

            Utility.Debug("Initializing data manager");
            InitiateData();
        }

        public void InitiateData()
        {
            Utility.Debug("Checking if positions file exists");
            if (!File.Exists(Path))
            {
                Utility.Debug("File doesn't exist, create a new file");
                var stream = File.Create(Path);
                stream.Close();
                Utility.Debug("Created a new file, save default data");
                SaveJson();
            }
            else
            {
                LoadJson();
            }
        }

        public void SaveJson()
        {
            File.WriteAllText(Path, ToJson(Data));
        }

        public void LoadJson()
        {
            Utility.Debug("Loading json file");
            var deserialized = JsonConvert.DeserializeObject<PositionsData>(File.ReadAllText(Path));
            Data = deserialized;
        }

        public string ToJson(PositionsData data)
        {
            Utility.Debug("Converting data to serialized json object for saving");
            var serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
            Utility.Debug($"Serialized data: {serialized}");
            return serialized;
        }
    }
}
