using Rocket.Core.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace UnturnedBlackout.FileReaders
{
    public class XmlFileReader<T> : IFileReader<T> where T : class, new()
    {
        public T FileData { get; protected set; }

        public string FilePath { get; }
        public string URLPath { get; }

        protected XmlSerializer Serializer { get; }

        public XmlFileReader(string filePath, string urlPath, params Type[] extraTypes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath),
                    "Parameter 'fileName' either null or empty. This is not allowed.");

            FileData = new T();
            FilePath = Path.ChangeExtension(filePath, "xml");
            URLPath = urlPath;
            Serializer = new XmlSerializer(typeof(T), extraTypes);
            Load();
        }

        public virtual void Load()
        {
            try
            {
                var file = LoadURL();
                if (string.IsNullOrEmpty(file))
                {
                    if (!File.Exists(FilePath))
                        Save();

                    using var reader = File.OpenRead(FilePath);
                    var deserializedData = Serializer.Deserialize(reader);
                    if (deserializedData is T t)
                        FileData = t;
                }
                else
                {
                    using StringReader reader = new(file);
                    var deserializedData = Serializer.Deserialize(reader);
                    if (deserializedData is T t)
                        FileData = t;
                    Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deserialize XML File: {FilePath}", ex);
            }
        }

        public virtual string LoadURL()
        {
            try
            {
                using HttpClient wc = new();
                return Task.Run(async Task<string> () => await wc.GetStringAsync(URLPath)).Result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to load file from URL: {URLPath}");
                return "";
            }
        }

        public virtual void Save()
        {
            try
            {
                var directoryName = Path.GetDirectoryName(FilePath);

                if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                using var writer = File.Open(FilePath, FileMode.Create);
                Serializer.Serialize(writer, FileData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to serialize XML File: {FilePath}", ex);
            }
        }
    }
}
