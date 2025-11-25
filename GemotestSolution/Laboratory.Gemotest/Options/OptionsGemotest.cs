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
        public string Login { get; set; } = "10003-gem";    
        public string Password { get; set; } = "F(SP{2JPg";
        public string Contractor { get; set; } = "10003";
        public string Contractor_Code { get; set; } = "10003";
        public string Salt { get; set; } = "b4f6d7d2fe94123c03c86412a0b649494017463f";

        public override string Pack()
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                new XmlSerializer(typeof(OptionsGemotest)).Serialize(memStream, this);
                memStream.Position = 0;
                return Encoding.UTF8.GetString(memStream.GetBuffer());
            }
        }

        public override BaseOptions Unpack(string source)
        {
            try
            {
                using (StringReader sR = new StringReader(source ?? string.Empty))
                    return (OptionsGemotest)new XmlSerializer(typeof(OptionsGemotest)).Deserialize(sR);
            }
            catch (Exception e)
            {
                //LogEvent.SaveExceptionToLog(e, GetType().Name);
                return new OptionsGemotest();
            }
        }

        public void SaveToFile(string filePath)
        {
            string xml = Pack();
            File.WriteAllText(filePath, xml);
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