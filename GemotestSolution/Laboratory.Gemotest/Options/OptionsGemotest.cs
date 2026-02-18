using SiMed.Clinic.Logger;
using SiMed.Laboratory;
using StatisticsCollectionSystemClient;
using System;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Laboratory.Gemotest
{
    [Serializable]
    public class OptionsGemotest : BaseOptions
    {
        public string UrlAdress { get; set; } = "https://api.gemotest.ru/odoctor/odoctor/index/ws/1";
        public string Login { get; set; }    
        public string Password { get; set; } 
        public string Contractor { get; set; } 
        public string Contractor_Code { get; set; } 
        public string Salt { get; set; }

        public override string Pack()
        {
            using (var memStream = new MemoryStream())
            {
                new XmlSerializer(typeof(OptionsGemotest)).Serialize(memStream, this);
                return Encoding.UTF8.GetString(memStream.ToArray()); 
            }
        }

        public override BaseOptions Unpack(string source)
        {
            try
            {
                source = (source ?? string.Empty).TrimEnd('\0');
                using (var sR = new StringReader(source))
                    return (OptionsGemotest)new XmlSerializer(typeof(OptionsGemotest)).Deserialize(sR);
            }
            catch
            {
                return new OptionsGemotest();
            }
        }

        public void SaveToFile(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllText(filePath, Pack(), Encoding.UTF8);
        }


        public static OptionsGemotest LoadFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string xml = File.ReadAllText(filePath);
                return (OptionsGemotest)new OptionsGemotest().Unpack(xml);
            }
            return new OptionsGemotest();
        }
    }
}