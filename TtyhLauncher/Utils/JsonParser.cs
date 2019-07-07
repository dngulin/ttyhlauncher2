using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TtyhLauncher.Utils {
    public class JsonParser {
        private readonly UTF8Encoding _utf8;
        
        public JsonSerializer Serializer { get; }

        public JsonParser(JsonSerializer serializer) {
            Serializer = serializer;
            _utf8 = new UTF8Encoding(false);
        }
        
        public T ReadFile<T>(string path) {
            using (var textReader = new StreamReader(File.OpenRead(path), _utf8))
            using (var jsonReader = new JsonTextReader(textReader)) {
                return Serializer.Deserialize<T>(jsonReader);
            }
        }

        public void WriteFile(object content, string path) {
            using (var textWriter = new StreamWriter(File.Create(path), _utf8))
            using (var jsonWriter = new JsonTextWriter(textWriter)) {
                Serializer.Serialize(jsonWriter, content);
            }
        }
    }
}