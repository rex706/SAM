using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SAM
{
    class Utils
    {
        public static void Serialize(List<Account> input)
        {
            var serializer = new XmlSerializer(input.GetType());
            var sw = new StreamWriter("info.dat");
            serializer.Serialize(sw, input);
            sw.Close();
        }

        public static List<Account> Deserialize(string file)
        {
            var stream = new StreamReader(file);
            var ser = new XmlSerializer(typeof(List<Account>));
            object obj = ser.Deserialize(stream);
            stream.Close();
            return (List<Account>)obj;
        }
    }
}
