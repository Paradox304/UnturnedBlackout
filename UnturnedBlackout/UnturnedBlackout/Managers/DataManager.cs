using Newtonsoft.Json;
using System.IO;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Managers
{
    public class DataManager
    {
        public string Path { get; set; }
        public PositionsData Data { get; set; }

        public DataManager()
        {
            Path = Plugin.Instance.Directory + "/Positions.json";
            Data = new PositionsData();

            InitiateData();
        }

        public void InitiateData()
        {
            if (!File.Exists(Path))
            {
                var stream = File.Create(Path);
                stream.Close();
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
            var deserialized = JsonConvert.DeserializeObject<PositionsData>(File.ReadAllText(Path));
            Data = deserialized;
        }

        public string ToJson(PositionsData data)
        {
            var serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
            return serialized;
        }

        public string ConvertLoadoutToJson(LoadoutData data)
        {
            var serialized = JsonConvert.SerializeObject(data, Formatting.None);
            return serialized;
        }

        public LoadoutData ConvertLoadoutFromJson(string serialized)
        {
            var deserialized = JsonConvert.DeserializeObject<LoadoutData>(serialized);
            return deserialized;
        }
    }
}
