using System.IO;
using System.Text.Json;
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
                FileStream stream = File.Create(Path);
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
            PositionsData deserialized = JsonSerializer.Deserialize<PositionsData>(File.ReadAllText(Path));
            Data = deserialized;
        }

        public string ToJson(PositionsData data)
        {
            string serialized = JsonSerializer.Serialize(data);
            return serialized;
        }

        public string ConvertLoadoutToJson(LoadoutData data)
        {
            string serialized = JsonSerializer.Serialize(data);
            return serialized;
        }

        public LoadoutData ConvertLoadoutFromJson(string serialized)
        {
            LoadoutData deserialized = JsonSerializer.Deserialize<LoadoutData>(serialized);
            return deserialized;
        }
    }
}
