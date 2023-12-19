using System;
using System.Collections.Generic;
using System.IO;


namespace Kings_Field_Additional.IO
{
    public class TXT
    {
        
        public static List<string> LoadTxt(string txtName)
        {
            List<string> listString = new List<string>();
            if (!File.Exists(txtName))
            {
                return listString;
            }
            FileStream fileStream = new FileStream(txtName, FileMode.Open, FileAccess.Read);
            StreamReader txtReader = new StreamReader(fileStream);
            int count = 0;
            try
            {
                count = int.Parse(txtReader.ReadLine().Substring(1, 4));
               
            }
            catch (FormatException)
            {
                Console.WriteLine("Broken txt count");

            }
            txtReader.ReadLine();
            int txtIndex;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    txtIndex = int.Parse(txtReader.ReadLine().Substring(1, 4));
                    if (txtIndex != i)
                    {
                        Console.WriteLine("Mismatch txt index");
                       
                       
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Broken txt index");
                   
                }
                string text = "";
                while (true)
                {
                    string line = txtReader.ReadLine();

                    if (line.Length > 10)
                    {
                        if (line.Substring(0, 10) == "----------")
                        {
                            text = text.Substring(0, text.Length - 2);

                            break;
                        }
                    }
                    text += line + "\r\n";
                }

                listString.Add(text);
               
            }
            fileStream.Close();
            return listString;
        }
        public static void Toxtxt(string txtName,List<string> strings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(txtName));
            StreamWriter writer = new StreamWriter(txtName);
            int i = 0;
            writer.WriteLine("[{0, 0:d4}]\n********************", strings.Count);
            foreach (var  s in strings)
            {
                

                writer.WriteLine("[{0, 0:d4}]", i++);
                writer.WriteLine(s);
                writer.WriteLine("--------------------------------------------");

            }
            writer.Flush();
            writer.Close();


        }


    }
}
