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
    public class GemotestPriceList
    {
        public string ContractorCode { get; set; } 
        public string Name { get; set; }      
        public string Num { get; set; }

        public GemotestPriceList()
        {
            ContractorCode = "";
            Name = "";
            Num = "1";
        }
    }

    [Serializable]
    public class SystemOptions : BaseOptions
    {
        public string UrlAdress { get; set; } = "https://api.gemotest.ru/odoctor/odoctor/index/ws/1";
        public string Login { get; set; } = "10003-gem";
        public string Password { get; set; } = "F(SP{2JPg";
        public string Contractor { get; set; } 
        public string Contractor_Code { get; set; }
        public string Numerator { get; set; } = "1";
        public string Salt { get; set; } = "b4f6d7d2fe94123c03c86412a0b649494017463f";

        public System.Collections.Generic.List<GemotestPriceList> PriceLists { get; set; }
    = new System.Collections.Generic.List<GemotestPriceList>();

        public override string Pack()
        {
            using (var memStream = new MemoryStream())
            {
                new XmlSerializer(typeof(SystemOptions)).Serialize(memStream, this);
                return Encoding.UTF8.GetString(memStream.ToArray()); 
            }
        }

        public override BaseOptions Unpack(string source)
        {
            try
            {
                source = (source ?? string.Empty).TrimEnd('\0');
                using (var sR = new StringReader(source))
                    return (SystemOptions)new XmlSerializer(typeof(SystemOptions)).Deserialize(sR);
            }
            catch
            {
                return new SystemOptions();
            }
        }

        public void SaveToFile(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllText(filePath, Pack(), Encoding.UTF8);
        }


        public static SystemOptions LoadFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string xml = File.ReadAllText(filePath);
                return (SystemOptions)new SystemOptions().Unpack(xml);
            }
            return new SystemOptions();
        }
    }
}