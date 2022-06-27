using JetBrains.Annotations;
using System;
using System.IO;
using System.Xml.Serialization;

namespace UnturnedBlackout.FileReaders
{
    [UsedImplicitly]
    public class XmlFileReader<T> : IFileReader<T> where T : class, new()
    {
        public T FileData { get; protected set; }

        public string FilePath { get; }

        protected XmlSerializer Serializer { get; }

        public XmlFileReader(string filePath, params Type[] extraTypes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath),
                    "Parameter 'fileName' either null or empty. This is not allowed.");

            FileData = new T();
            FilePath = Path.ChangeExtension(filePath, "xml");
            Serializer = new XmlSerializer(typeof(T), extraTypes);
            Load();
        }

        [UsedImplicitly]
        public virtual void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    Save();

                using var reader = File.OpenRead(FilePath);
                var deserializedData = Serializer.Deserialize(reader);
                if (deserializedData is T t)
                    FileData = t;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deserialize XML File: {FilePath}", ex);
            }
        }

        [UsedImplicitly]
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
