using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace EternSynth
{
    // DB structures shared between WPF (Windows) and Avalonia (cross-platform) builds

    [DataContract]
    public class SavedSound
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ParamsString { get; set; }

        public SavedSound()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Sonido";
            ParamsString = "";
        }
    }

    [DataContract]
    public class SynthDatabase
    {
        [DataMember]
        public List<SavedSound> Sounds { get; set; }

        public SynthDatabase()
        {
            Sounds = new List<SavedSound>();
        }
    }

    public static class Storage
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EternSynth",
            "eternsynth_db.json"
        );

        public static SynthDatabase Load()
        {
            try
            {
                string dir = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(DbPath))
                    return new SynthDatabase();

                using (var fs = new FileStream(DbPath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SynthDatabase));
                    var db = (SynthDatabase)serializer.ReadObject(fs);
                    return db ?? new SynthDatabase();
                }
            }
            catch
            {
                return new SynthDatabase();
            }
        }

        public static void Save(SynthDatabase db)
        {
            try
            {
                string dir = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var fs = new FileStream(DbPath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SynthDatabase));
                    serializer.WriteObject(fs, db);
                }
            }
            catch { }
        }
    }
}
