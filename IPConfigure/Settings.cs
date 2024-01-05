using Newtonsoft.Json;
using System.IO;
using System.Windows;

namespace NetworkToolBox {

    public class Settings {
        private static Settings instance = null;
        private static readonly object padlock = new object();

        Settings() {
        }

        public static Settings Instance {
            get {
                if (instance == null) {
                    lock (padlock) {
                        if (instance == null) {
                            instance = new Settings();
                        }
                    }
                }
                return instance;
            }
        }

        public void Save(string path) {
            JsonSerializer jsons = new JsonSerializer();
            jsons.Formatting = Formatting.Indented;
            using (StreamWriter sw = new StreamWriter(path)) {
                using (JsonWriter writer = new JsonTextWriter(sw)) {
                    jsons.Serialize(writer, this);
                }
            }
        }

        public void Load(string path) {
            JsonSerializer jsons = new JsonSerializer();
            using (StreamReader sr = new StreamReader(path)) {
                using (JsonReader reader = new JsonTextReader(sr)) {
                    instance = jsons.Deserialize<Settings>(reader);
                }
            }
        }

        public bool Topmost { get; set; }

        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public WindowState WindowsState { get; set; }

        public string StartupAdapter { get; set; }
    }
}