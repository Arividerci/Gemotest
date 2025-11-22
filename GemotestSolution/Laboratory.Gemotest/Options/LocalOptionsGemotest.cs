using SiMed.Clinic.Logger;
using SiMed.Laboratory;
using System;
using System.IO;
using System.Text;
using System.Drawing.Printing;
using System.Xml.Serialization;

namespace Laboratory.Gemotest.Options
{
    public enum LabelEncoding
    {
        UTF8 = 1,
        Code866,
        Windows1251
    }

    public enum LabelType
    {
        ZPL = 1,
        EPL,
        Custom
    }

    [Serializable]
    public class LocalOptionsGemotest : BaseOptions
    {
        public bool PrintAtOnce { get; set; }
        public bool PrintStikersAtOnce { get; set; }
        public bool PrintBlankAtOnce { get; set; }
        public PaperSource PaperSource { get; set; }
        public PrinterSettings PdfPrinterSettings { get; set; }
        public PrinterSettings StickerPrinterSettings { get; set; }
        public LabelEncoding LabelEncoding { get; set; } = LabelEncoding.Code866;
        public LabelType LabelType { get; set; } = LabelType.EPL;
        public string CustomLabelTemplate { get; set; } = "";

        public static string GetDefaultLabelTemplate(LabelType _LabelType)
        {
            switch (_LabelType)
            {
                case LabelType.ZPL:
                    return
@"^XA^CWZ,E:TT0003M_.FNT
^PR2
~TA000
^LS0
^LT0
^LH0,0
^PW335
^LL200
^MD10
^FS^XZ
^XA
^CI28
^FO20,15^AZN,25,20^FD{0}^FS
^FO20,45^AZN,25,20^FD{1}^FS
^FO20,75^BY2^BCN,60,Y^FD>;{2}^FS
^FO20,160^AZN,25,20^FD{3}^FS
^XZ";
                case LabelType.EPL:
                    return
@"N
S2
D5
ZB
I8,10
q335
Q200,24
A20,0,0,3,1,1,N,""{0}""
A20,25,0,3,1,1,N,""{1}""
B20,50,0,1C,2,30,70,B,""{2}""
A20,150,0,3,1,1,N,""{3}""
P1,1
";
                default:
                    return "";
            }
        }

        public override string Pack()
        {
            System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(LocalOptionsGemotest));
            serializer.Serialize(memStream, this);
            memStream.Position = 0;
            string XmlStr = Encoding.UTF8.GetString(memStream.GetBuffer());
            return XmlStr;
        }

        public override BaseOptions Unpack(string _Source)
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(LocalOptionsGemotest));
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(_Source));
                LocalOptionsGemotest options = (LocalOptionsGemotest)deserializer.Deserialize(memStream);
                return options;
            }
            catch (Exception e)
            {
                return new LocalOptionsGemotest();
            }
        }
    }
}