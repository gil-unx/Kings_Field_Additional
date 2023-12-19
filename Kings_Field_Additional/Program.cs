using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using Kings_Field_Additional.IO;
using System.IO.Pipes;
using System.Data.SqlTypes;

namespace Kings_Field_Additional
{
    internal class Program
    {
        public static bool txtOverWrite = false;
        static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
        {
            byte[] encryptedBytes = null;

            // Set up the encryption objects
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                // Encrypt the input plaintext using the AES algorithm
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }

            return encryptedBytes;
        }
        static byte[] Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
        {
            byte[] decryptedBytes = null;

            // Set up the encryption objects
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                // Decrypt the input ciphertext using the AES algorithm
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                }
            }
             
            return decryptedBytes;
        }
        static void DecryptDat(string fileName)
        {
            byte[] key = new byte[] { 0x8D, 0x5A, 0xB5, 0x6A, 0xE7, 0xCC, 0xDA, 0xE9, 0x4D, 0x01, 0x4C, 0x43, 0xBE, 0x36, 0xEA, 0x65 };
            byte[] ivec = new byte[] { 0x36, 0xC9, 0x2E, 0x63, 0xA8, 0x7C, 0x05, 0x4B, 0x8C, 0x2A, 0xAB, 0x1A, 0xA8, 0x4F, 0x15, 0xEA };
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            FileStream fileStream1 = new FileStream(Path.ChangeExtension(fileName, "dec"), FileMode.Create, FileAccess.Write);
            BinaryReader reader = new BinaryReader(fileStream);
            BinaryWriter writer = new BinaryWriter(fileStream1);
            writer.Write(Decrypt(reader.ReadBytes(0x10), key, ivec));
            writer.Write(Decrypt(reader.ReadBytes(0x120), key, ivec));
            int ptPos = 0x6AFF0;
            byte[] hdr = Decrypt(reader.ReadBytes(0x6b6d0), key, ivec);
            MemoryStream memory = new MemoryStream(hdr);
            BinaryReader hdrReader = new BinaryReader(memory);
            hdrReader.BaseStream.Seek(ptPos, SeekOrigin.Begin);
            writer.Write(hdr);
            writer.Write(Decrypt(reader.ReadBytes(0x193010), key, ivec));
            for (int i = 0; i < 110; i++)
            {
                int size = hdrReader.ReadInt32();
                int id = hdrReader.ReadInt32();
                int offset = hdrReader.ReadInt32();
                hdrReader.ReadBytes(4);
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(Decrypt(reader.ReadBytes(size), key, ivec));
                writer.Flush();
            }
            writer.Write(reader.ReadBytes((int)(reader.BaseStream.Length - writer.BaseStream.Position)));
            reader.Close();
            writer.Flush();
            writer.Close();

        }
        static void EncryptDat(string fileName)
        {
            byte[] key = new byte[] { 0x8D, 0x5A, 0xB5, 0x6A, 0xE7, 0xCC, 0xDA, 0xE9, 0x4D, 0x01, 0x4C, 0x43, 0xBE, 0x36, 0xEA, 0x65 };
            byte[] ivec = new byte[] { 0x36, 0xC9, 0x2E, 0x63, 0xA8, 0x7C, 0x05, 0x4B, 0x8C, 0x2A, 0xAB, 0x1A, 0xA8, 0x4F, 0x15, 0xEA };
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            FileStream fileStream1 = new FileStream(Path.ChangeExtension(fileName, "dat.new"), FileMode.Create, FileAccess.Write);
            BinaryReader reader = new BinaryReader(fileStream);
            BinaryWriter writer = new BinaryWriter(fileStream1);
            writer.Write(Encrypt(reader.ReadBytes(0x10), key, ivec));
            writer.Write(Encrypt(reader.ReadBytes(0x120), key, ivec));
            int ptPos = 0x6AFF0;
            byte[] hdrDec = reader.ReadBytes(0x6b6d0);
            byte[] hdr = Encrypt(hdrDec, key, ivec);
            MemoryStream memory = new MemoryStream(hdrDec);
            BinaryReader hdrReader = new BinaryReader(memory);
            hdrReader.BaseStream.Seek(ptPos, SeekOrigin.Begin);
            writer.Write(hdr);
            writer.Write(Encrypt(reader.ReadBytes(0x193010), key, ivec));
            for (int i = 0; i < 110; i++)
            {
                int size = hdrReader.ReadInt32();
                int id = hdrReader.ReadInt32();
                int offset = hdrReader.ReadInt32();
                hdrReader.ReadBytes(4);
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(Encrypt(reader.ReadBytes(size), key, ivec));
                writer.Flush();
            }
            writer.Write(reader.ReadBytes((int)(reader.BaseStream.Length - writer.BaseStream.Position)));
            reader.Close();
            writer.Flush();
            writer.Close();

        }
        static void ImgUnpack(string fileName)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(outF);
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                BR reader = new BR(fileStream);

                fileStream.Seek(8, SeekOrigin.Begin);
                int count = reader.ReadInt32() / 0x10;
                fileStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    int type = reader.ReadInt32();
                    int size = reader.ReadInt32();
                    int offset = reader.ReadInt32();
                    short width = reader.ReadInt16();
                    short height = reader.ReadInt16();
                    byte[] buffer = reader.GetBytes(offset, size);
                    string extension = "bin";
                    switch (type)
                    {
                        case 0:
                            extension = "jpg";
                            break;
                        case 1:
                            Console.WriteLine( fileName, "Type1 ");
                            Console.ReadKey();
                            extension = "gim";
                            break;
                        case 2:
                            extension = "gim";
                            break;
                        case 3:
                            extension = "bmp";
                            break;
                        default:
                            break;
                    }
                    File.WriteAllBytes(string.Format("{0}\\{1,0:d4}.{2}", outF, i, extension), buffer);
                }
            }


        }
        static void ImgRepack(string fileName)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            byte[] data = File.ReadAllBytes(fileName);
            using (MemoryStream memory = new MemoryStream(data))
            {
                BR reader = new BR(memory);
                BW writer = new BW(new FileStream(fileName,FileMode.Create,FileAccess.Write));
                MemoryStream memory1 = new MemoryStream();
                BW wr = new BW(memory1);
                memory.Seek(8, SeekOrigin.Begin);
                int ptRef = reader.ReadInt32();
                int count =  ptRef/ 0x10;
                memory.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    int type = reader.ReadInt32();
                    int size = reader.ReadInt32();
                    int offset = reader.ReadInt32();
                    short width = reader.ReadInt16();
                    short height = reader.ReadInt16();
                    string extension = "bin";
                    switch (type)
                    {
                        case 0:
                            extension = "jpg";
                            break;
                        case 1:
                            Console.WriteLine(fileName, "Type1 ");
                            Console.ReadKey();
                            extension = "gim";
                            break;
                        case 2:
                            extension = "gim";
                            break;
                        case 3:
                            extension = "bmp";
                            break;
                        default:
                            break;
                    }
                    byte[] buffer = File.ReadAllBytes(string.Format("{0}\\{1,0:d4}.{2}", outF, i, extension));
                    offset = (int)memory1.Position+ptRef;
                    size = (int)buffer.Length;
                    wr.Write(buffer);
                    wr.WritePadding(0x10, 0);
                    writer.Write(type);
                    writer.Write(size);   
                    writer.Write(offset);
                    writer.Write(width);
                    writer.Write(height);   

                }
                writer.Write(memory1.ToArray());
                writer.Flush();
                writer.Close(); 
            }


        }
        static void UnpackDat(string fileName, int startIndex = 0, int endIndex = 110)
        {
            fileName =Path.GetFullPath(fileName);   
            string outFHDR = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName)+"\\_0000";

            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(outFHDR);
            
            using (BR reader = new BR(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                //SYSTEM/
                {
                    int count;
                    Console.WriteLine("Unpack >>{0}\\_0000", outF);
                    //STRING block
                    {
                        List<string> strings = new List<string>();
                        reader.BaseStream.Seek(0X204, SeekOrigin.Begin);
                        count = reader.ReadInt32();
                        int pos = reader.ReadInt32();
                        reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                        for (int i = 0; i < count; i++)
                        {
                            int offset = reader.ReadInt32();
                            string str = reader.GetJis(offset);
                            strings.Add(str);
                        }
                        TXT.Toxtxt(outFHDR + "\\0000.txt", strings);

                        //01-----------------------------------
                        strings = new List<string>();
                        reader.BaseStream.Seek(0X134, SeekOrigin.Begin);
                        count = reader.ReadInt32();
                        pos = reader.ReadInt32();
                        reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                        for (int i = 0; i < count; i++)
                        {
                            strings.Add(reader.GetJis());
                            reader.ReadPadding(0x20, 0x10);
                            long v = reader.BaseStream.Position;
                        }
                        TXT.Toxtxt(outFHDR + "\\0001.txt", strings);
                        //02-----------------------------------



                    }
                    //extract GIm0
                    {

                        reader.BaseStream.Seek(0x29EB0, SeekOrigin.Begin);
                        int gimSize = reader.ReadInt32();
                        int gimOffset = reader.ReadInt32();
                        File.WriteAllBytes(outFHDR + "\\0000.gim", reader.GetBytes(gimOffset, gimSize));
                    }
                    //extract messBlock & imgBlock
                    reader.BaseStream.Seek(0x6B800, SeekOrigin.Begin);
                    int size_6B800 = reader.ReadInt32();


                    reader.BaseStream.Seek(0x6B814, SeekOrigin.Begin);
                    int ptPosMessBlock = reader.ReadInt32();
                    int ptPosImgBlock = reader.ReadInt32();
                    int imgBlockSize = size_6B800 - ptPosImgBlock + 0x6B800;
                    byte[] buffer;
                    //messBlock
                    {
                        int messBlockCount = 49;
                        string outFBlockMess = outFHDR + string.Format("\\{0,0:X8}", ptPosMessBlock);
                        Directory.CreateDirectory(outFBlockMess);
                        reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                        List<int> offsets = new List<int>();
                        for (int i = 0; i < messBlockCount; i++)
                        {
                            offsets.Add(reader.ReadInt32());

                        }

                        for (int i = 0; i < messBlockCount; i++)
                        {
                            int offset = offsets[i];
                            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                            int size = reader.ReadInt32();
                            reader.BaseStream.Seek(-4, SeekOrigin.Current);
                            buffer = reader.ReadBytes(size);
                            File.WriteAllBytes(outFBlockMess + string.Format("\\{0,0:d4}.bin", i), buffer);
                            MessBlockUnpack(outFBlockMess + string.Format("\\{0,0:d4}.bin", i), offset);
                        }

                    }

                    //imgblock
                    {
                        reader.BaseStream.Seek(ptPosImgBlock, SeekOrigin.Begin);
                        buffer = reader.ReadBytes(imgBlockSize);
                        using (MemoryStream memory = new MemoryStream(buffer))
                        {
                            BinaryWriter wr = new BinaryWriter(memory);
                            BinaryReader rdr = new BinaryReader(memory);
                            rdr.ReadInt32();

                            int size = rdr.ReadInt32();
                            count = (rdr.ReadInt32() - ptPosImgBlock) / 0x10;
                            memory.Seek(0, SeekOrigin.Begin);
                            for (int i = 0; i < count; i++)
                            {
                                memory.Seek(8, SeekOrigin.Current);
                                int offset = rdr.ReadInt32() - ptPosImgBlock;
                                memory.Seek(-4, SeekOrigin.Current);
                                wr.Write(offset);
                                memory.Seek(4, SeekOrigin.Current);
                            }

                            File.WriteAllBytes(outFHDR + "\\IMG.bin", buffer);
                            ImgUnpack(outFHDR + "\\IMG.bin");
                        }
                    }

                }
                ////Indexed Block start
                {
                    reader.BaseStream.Seek(0x06B120, SeekOrigin.Begin);
                    for (int i = 0; i < 110; i++)
                    {

                      
                        int size = reader.ReadInt32();
                        int id = reader.ReadInt32();
                        int offset = reader.ReadInt32();
                        reader.ReadBytes(4);
                        if ((i >= startIndex) && (i <= endIndex))
                        {
                            Console.WriteLine("Unpack >>{0}\\{1,0:d4}.bin", outF, i);
                            byte[] buffer = reader.GetBytes(offset, size);
                            File.WriteAllBytes(outF + string.Format("\\{0,0:d4}.bin", i), buffer);
                            BinBlockUnpack(outF + string.Format("\\{0,0:d4}.bin", i), offset);

                        }
                            
                    }

                }
                ////Indexed Block end
                reader.Close();
            }
        }
        static void RepackDat(string fileName,int startIndex = 0, int endIndex = 110)
        {
            fileName = Path.GetFullPath(fileName);
            string outFHDR = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "\\_0000";
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(outFHDR);
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                BR reader = new BR(fileStream);
                BW writer = new BW(fileStream);
                Console.WriteLine("Repack >>{0}\\_0000", outF);
                // insert STRINGS
                {
                    int strings1Size = 0xC5F0;
                    byte[] stringsBuffer = new byte[strings1Size];
                    MemoryStream memory = new MemoryStream(stringsBuffer);
                    reader.BaseStream.Seek(0X204, SeekOrigin.Begin);
                    int count = reader.ReadInt32();
                    int pos = reader.ReadInt32();
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    int ptRef = reader.ReadInt32();
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    List<string> strings = TXT.LoadTxt(outFHDR + "\\0000.txt");
                    Dictionary<string, int> ptSame = new Dictionary<string, int>();
                    for (int i = 0; i < count; i++)
                    {
                        byte[] str = Encoding.GetEncoding("shift-jis").GetBytes(strings[i]);
                        int offset;
                        if(ptSame.TryGetValue(strings[i], out offset))
                        {
                        }
                        else
                        {
                            offset = (int)memory.Position + ptRef;
                            ptSame.Add(strings[i], offset);
                            memory.Write(str, 0, str.Length);
                            memory.Seek(1, SeekOrigin.Current);
                        }
                        writer.Write(offset);

                    }
                    reader.BaseStream.Seek(ptRef, SeekOrigin.Begin);
                    writer.Write(memory.ToArray());

                    //01-----------------------------------

                    reader.BaseStream.Seek(0X134, SeekOrigin.Begin);
                    count = reader.ReadInt32();
                    pos = reader.ReadInt32();
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    strings = TXT.LoadTxt(outFHDR + "\\0001.txt");
                    for (int i = 0; i < count; i++)
                    {
                        byte[] str = Encoding.GetEncoding("shift-jis").GetBytes(strings[i]);
                        if (str.Length > 0x1c)
                        {
                            Console.WriteLine("FIXED STRING EXPANDED!");
                            Environment.Exit(1);
                        }
                        writer.Write(str);

                        reader.ReadPadding(0x20, 0x10);



                    }


                }

               
                //insert GIm0
                {

                    reader.BaseStream.Seek(0x29EB0, SeekOrigin.Begin);
                    int gimSize = reader.ReadInt32();
                    int gimOffset = reader.ReadInt32();
                    reader.BaseStream.Seek(gimOffset, SeekOrigin.Begin);
                    byte[] gimBuffer = File.ReadAllBytes(outFHDR + "\\0000.gim");
                    if (gimBuffer.Length>gimSize)
                    {
                        Console.WriteLine("GIM 0000.gim size increased!");
                        Environment.Exit(1);
                    }
                    writer.Write(gimBuffer);
                }
                reader.BaseStream.Seek(0x6B800, SeekOrigin.Begin);
                int size_6B800 = reader.ReadInt32();
                reader.BaseStream.Seek(0x6B814, SeekOrigin.Begin);
                int ptPosMessBlock = reader.ReadInt32();
                int ptPosImgBlock = reader.ReadInt32();
                int imgBlockSize = size_6B800 - ptPosImgBlock + 0x6B800;
                byte[] buffer;
                //messBlock
                {
                    int messBlockCount = 49;
                    string outFBlockMess = outFHDR + string.Format("\\{0,0:X8}", ptPosMessBlock);
                    reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                    List<int> offsets = new List<int>();
                    int posMessBlock = reader.ReadInt32();
                    reader.BaseStream.Seek(posMessBlock, SeekOrigin.Begin);
                    for (int i = 0; i < messBlockCount; i++)
                    {
                        int offset = (int)fileStream.Position;
                        offsets.Add(offset);
                        MessBlockRepack(outFBlockMess + string.Format("\\{0,0:d4}.bin", i), offset);
                        buffer = File.ReadAllBytes(outFBlockMess + string.Format("\\{0,0:d4}.bin", i));
                        writer.Write(buffer);
                        
                    }
                    ptPosImgBlock = (int)writer.BaseStream.Position;
                    reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                    for (int i = 0; i < messBlockCount; i++)
                    {
                        writer.Write(offsets[i]);
                    }
                }
                reader.BaseStream.Seek(ptPosImgBlock, SeekOrigin.Begin);
                //IMG BLOCK
                {
                   
                    ImgRepack(outFHDR + "\\IMG.bin");
                    buffer = File.ReadAllBytes(outFHDR + "\\IMG.bin");
                    if (reader.BaseStream.Position + buffer.Length > 0x1FF000)
                    {
                        Console.WriteLine("Maximum size reached!! _0000");
                        Environment.Exit(1);
                    }
                    using (MemoryStream memory = new MemoryStream(buffer))
                    {
                        BinaryWriter wr = new BinaryWriter(memory);
                        BinaryReader rdr = new BinaryReader(memory);
                        memory.Seek(8, SeekOrigin.Begin);
                        int count = (rdr.ReadInt32()) / 0x10;
                        memory.Seek(0, SeekOrigin.Begin);
                        for (int i = 0; i < count; i++)
                        {
                            memory.Seek(8, SeekOrigin.Current);
                            int offset = rdr.ReadInt32() + ptPosImgBlock;
                            memory.Seek(-4, SeekOrigin.Current);
                            wr.Write(offset);
                            memory.Seek(4, SeekOrigin.Current);
                        }
                        
                    }
                    writer.Write(buffer);
                    writer.WritePadding(0x1FF000, 0);
                }
                {
                    size_6B800 = (int)writer.BaseStream.Position - 0x6B800;
                    reader.BaseStream.Seek(0x6B800, SeekOrigin.Begin);
                    writer.Write(size_6B800);
                    reader.BaseStream.Seek(0x6B814, SeekOrigin.Begin);
                    ptPosMessBlock = reader.ReadInt32();
                    writer.Write(ptPosImgBlock);
                    
                    

                }
                ////Indexed Block start
                {
                    reader.BaseStream.Seek(0x06B120, SeekOrigin.Begin);
                    for (int i = 0; i < 110; i++)
                    {
                       
                        long tmp = reader.BaseStream.Position;
                        int size = reader.ReadInt32();
                        int id = reader.ReadInt32();
                        int offset = reader.ReadInt32();
                        reader.ReadBytes(4);
                        int maxSize = size + (size & 0x800);
                        if ((i >=startIndex)&&(i<=endIndex))
                        {
                            Console.WriteLine("Repack >>{0}\\{1,0:d4}.bin", outF, i);
                            BinBlockRepack(outF + string.Format("\\{0,0:d4}.bin", i), offset);
                            buffer = File.ReadAllBytes(outF + string.Format("\\{0,0:d4}.bin", i));
                            if (buffer.Length>maxSize)
                            {
                                Console.WriteLine("Repack >>{0}\\{1,0:d4}.bin FAILED!!!!!!!!!!" +
                                    "Maximum size reached.." +
                                    "", outF, i);
                                Environment.Exit(1);
                            }
                            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                            writer.Write(buffer);
                            reader.BaseStream.Seek(tmp, SeekOrigin.Begin);
                            int newSize = BitConverter.ToInt32(buffer, 0);
                            writer.Write(newSize);
                            writer.BaseStream.Seek(0xc, SeekOrigin.Current);
                        }
                       


                    }
                    
                }
                ////Indexed Block end
                fileStream.Flush();
                fileStream.Close();

            }
        }
        static void MESS2TXT(string fileName)
        {
            using (BR reader = new BR(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                List<string> strings = new List<string>();
                int count = reader.ReadInt32();
                if (count == 0) return;
                
                int ptpos = reader.ReadInt32();
                reader.BaseStream.Seek(ptpos, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    strings.Add(reader.GetJis(reader.ReadInt32()));
                }
                TXT.Toxtxt(Path.ChangeExtension(fileName, "txt"), strings);
            }

        }
        static void TXT2MESS(string fileName)
        {
            using (BW writer = new BW(new FileStream(Path.ChangeExtension(fileName,"bin"), FileMode.Open, FileAccess.ReadWrite)))
            {
                List<string> strings = TXT.LoadTxt(fileName);
                if (strings.Count == 0)
                {
                    return;
                }
                writer.Write((int)strings.Count);
                writer.Write((int)0x10);
                writer.WritePadding(0x10, 0);
                int pad = (4 - (strings.Count % 4));
                if (pad==4)
                {
                    pad = 0;
                }

                int ptRef = ((strings.Count + pad) * 4)+0x10;
                MemoryStream memory = new MemoryStream();
                BW wr = new BW(memory);
                for (int i = 0; i < strings.Count; i++)
                {
                    writer.Write((int)wr.BaseStream.Position+ptRef);
                    wr.Write(Encoding.GetEncoding("shift-jis").GetBytes(strings[i]));
                    wr.Write((byte)0);
                }
                writer.WritePadding(0x10, 0);
                wr.WritePadding(0x10,0);
                writer.Write(memory.ToArray());
                writer.Flush();
                writer.Close();

            }

        }
        static void MessBlockUnpack(string fileName, int ptRef)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(outF);
            using (BR reader = new BR(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                List<int> offsets = new List<int>();
                int fileSize = reader.ReadInt32();
                reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
                int offset;
                int size;
                byte[] buffer;

                for (int i = 0; i < 4; i++)
                {
                    offsets.Add(reader.ReadInt32() - ptRef);
                }
                //0-----------------
                offset = offsets[0];
                size = offsets[1] - offset;
                if (offset > 0)
                {
                    buffer = reader.GetBytes(offset, size);
                    File.WriteAllBytes(outF + string.Format("\\0000.bin", ptRef), buffer);

                }
                //1-----------------
                offset = offsets[1];
                size = offsets[3] - offset;
                if (offset > 0)
                {
                    buffer = reader.GetBytes(offset, size);
                    File.WriteAllBytes(outF + "\\0001.bin", buffer);

                }
                //2-----------------
                offset = offsets[2];
                size = offsets[0] - offset;
                if (offsets[0] <0)
                {
                    size = offsets[1] - offset;
                }

                if (offset > 0)
                {
                    buffer = reader.GetBytes(offset, size);
                    using (MemoryStream memory = new MemoryStream(buffer))
                    {
                        BinaryWriter wr = new BinaryWriter(memory);
                        BinaryReader rdr = new BinaryReader(memory);
                        int count = rdr.ReadInt32();
                        if (count > 0)
                        {
                            int ptpos = rdr.ReadInt32() - ptRef - offset;
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(ptpos);

                            rdr.BaseStream.Seek(ptpos, SeekOrigin.Begin);
                            for (int i = 0; i < count; i++)
                            {
                                int newOff = rdr.ReadInt32() - ptRef - offset;
                                rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                                wr.Write(newOff);

                            }
                        }
                        
                        File.WriteAllBytes(outF + "\\0002.bin", memory.ToArray());
                        MESS2TXT(outF + "\\0002.bin");
                    }


                }

                //3-----------------
                offset = offsets[3];
                size = fileSize - offset;
                if (offset > 0)
                {
                    buffer = reader.GetBytes(offset, size);
                    using (MemoryStream memory = new MemoryStream(buffer))
                    {
                        BinaryWriter wr = new BinaryWriter(memory);
                        BinaryReader rdr = new BinaryReader(memory);
                        int count = rdr.ReadInt32();
                        int ptpos = rdr.ReadInt32() - ptRef - offset;
                        rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                        wr.Write(ptpos);
                        rdr.BaseStream.Seek(ptpos, SeekOrigin.Begin);
                        List<int> newOffs = new List<int>();
                        int newOff;
                        for (int i = 0; i < count; i++)
                        {
                            newOff = rdr.ReadInt32() - ptRef - offset;
                            newOffs.Add(newOff);
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(newOff);
                        }
                        for (int i = 0; i < count; i++)
                        {
                            rdr.BaseStream.Seek(newOffs[i] + 0x3c, SeekOrigin.Begin);
                            newOff = rdr.ReadInt32() - ptRef - offset;
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(newOff);

                        }
                        File.WriteAllBytes(outF + "\\0003.bin", buffer);
                    }

                }

            }


        }
        static void MessBlockRepack(string fileName,int ptRef)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            byte[] data = File.ReadAllBytes(fileName);
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(data, 0, 0x80);
                BR reader = new BR(ms);
                BW writer = new BW(ms);
                List<int> offsets = new List<int>();

                int fileSize;
                byte[] buffer;
                ms.Seek(0x10, SeekOrigin.Begin);
                for (int i = 0; i < 4; i++)
                {
                    offsets.Add(reader.ReadInt32());
                }
                ms.Seek(0x80, SeekOrigin.Begin);
                //2-----------------
                if (offsets[2] != -1)
                {
                    offsets[2] = (int)ms.Position+ptRef;
                    TXT2MESS(outF + "\\0002.txt");
                    buffer = File.ReadAllBytes(outF + "\\0002.bin");
                    using (MemoryStream memory = new MemoryStream(buffer))
                    {
                        BinaryWriter wr = new BinaryWriter(memory);
                        BinaryReader rdr = new BinaryReader(memory);
                        int count = rdr.ReadInt32();
                        if (count!=0)
                        {
                            int ptpos = rdr.ReadInt32() + offsets[2];
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(ptpos);
                            rdr.BaseStream.Seek(ptpos - offsets[2], SeekOrigin.Begin);
                            for (int i = 0; i < count; i++)
                            {
                                int newOff = rdr.ReadInt32() + offsets[2];
                                rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                                wr.Write(newOff);

                            }

                        }
                       
                        writer.Write(memory.ToArray());
                    }

                }
                //0-----------------
                if (offsets[0] != -1)
                {
                    offsets[0] = (int)ms.Position + ptRef;
                    buffer = File.ReadAllBytes(outF + "\\0000.bin");
                    writer.Write(buffer);

                }
                //1-----------------
                if (offsets[1] != -1)
                {
                    offsets[1] = (int)ms.Position + ptRef;
                    buffer = File.ReadAllBytes(outF + "\\0001.bin");

                    writer.Write(buffer);

                }
               

                //3-----------------
               
                if (offsets[3] != -1)
                {
                    offsets[3] = (int)ms.Position+ptRef;
                    buffer = File.ReadAllBytes(outF + "\\0003.bin");
                    using (MemoryStream memory = new MemoryStream(buffer))
                    {
                        BinaryWriter wr = new BinaryWriter(memory);
                        BinaryReader rdr = new BinaryReader(memory);
                        int count = rdr.ReadInt32();
                        int ptpos = rdr.ReadInt32() + offsets[3];
                        rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                        wr.Write(ptpos);
                        rdr.BaseStream.Seek(ptpos  - offsets[3], SeekOrigin.Begin);
                        List<int> newOffs = new List<int>();
                        int newOff;
                        for (int i = 0; i < count; i++)
                        {
                            newOff = rdr.ReadInt32()  + offsets[3];
                            newOffs.Add(newOff);
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(newOff);
                        }
                        for (int i = 0; i < count; i++)
                        {
                            rdr.BaseStream.Seek(newOffs[i] + 0x3c  - offsets[3], SeekOrigin.Begin);
                            newOff = rdr.ReadInt32()  + offsets[3];
                            rdr.BaseStream.Seek(-4, SeekOrigin.Current);
                            wr.Write(newOff);

                        }
                        writer.Write(memory.ToArray());
                    }

                }

                fileSize = (int)ms.Position;
                ms.Seek(0, SeekOrigin.Begin);
                writer.Write(fileSize);
                ms.Seek(0x10, SeekOrigin.Begin);
                foreach (var item in offsets)
                {
                    writer.Write(item);
                }
                File.WriteAllBytes(fileName, ms.ToArray());
            }


        }
        static void BinBlockUnpack(string fileName, int ptRef)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(outF);
            using (BR reader = new BR(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                byte[] buffer;
                int fileSize = reader.ReadInt32();
                reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                int ptPosMessBlock = reader.ReadInt32()-ptRef;
                int ptPosImgBlock = reader.ReadInt32() - ptRef;
                int imgBlockSize = fileSize - ptPosImgBlock;
                reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                List<int> offsets = new List<int>();
                int count = (reader.ReadInt32()- ptPosMessBlock-ptRef)/4;
                reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    int offset = reader.ReadInt32();
                    if (offset>0)
                    {
                        offsets.Add(offset-ptRef);
                    }
                    

                }
                for (int i = 0; i < offsets.Count; i++)
                {
                    int offset = offsets[i];
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    int size = reader.ReadInt32();
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    buffer = reader.ReadBytes(size);
                    File.WriteAllBytes(outF + string.Format("\\{0,0:d4}.bin", i), buffer);
                    MessBlockUnpack(outF + string.Format("\\{0,0:d4}.bin", i), offset+ptRef);
                
                }
                reader.BaseStream.Seek(ptPosImgBlock, SeekOrigin.Begin);
                buffer = reader.ReadBytes(imgBlockSize);
                using (MemoryStream memory = new MemoryStream(buffer))
                {
                    BinaryWriter wr = new BinaryWriter(memory);
                    BinaryReader rdr = new BinaryReader(memory);
                    rdr.ReadInt32();

                    int size = rdr.ReadInt32();
                    int x = rdr.ReadInt32()-ptRef;
                    count = (x - ptPosImgBlock) / 0x10;
                    memory.Seek(0, SeekOrigin.Begin);
                    for (int i = 0; i < count; i++)
                    {
                        memory.Seek(8, SeekOrigin.Current);
                        int offset = rdr.ReadInt32() - ptPosImgBlock-ptRef;
                        memory.Seek(-4, SeekOrigin.Current);
                        wr.Write(offset);
                        memory.Seek(4, SeekOrigin.Current);
                    }

                    File.WriteAllBytes(outF + "\\IMG.bin", buffer);
                    ImgUnpack(outF + "\\IMG.bin");
                }

            }


        }
        static void BinBlockRepack(string fileName, int ptRef)
        {
            string outF = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
            byte[] data = File.ReadAllBytes(fileName);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BR reader = new BR(memoryStream);
                BW writer = new BW(memoryStream);
                writer.Write(data);
                byte[] buffer;
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                int fileSize = reader.ReadInt32();
                reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                int ptPosMessBlock = reader.ReadInt32() - ptRef;
                int ptPosImgBlock = reader.ReadInt32() - ptRef;
                int imgBlockSize = fileSize - ptPosImgBlock;
                reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                List<int> offsets = new List<int>();
                int posMesBlock = reader.ReadInt32()-ptRef;
                int count = (posMesBlock - ptPosMessBlock ) / 4;
               
                reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    int offset = reader.ReadInt32();
                    if (offset > 0)
                    {
                        offsets.Add(offset + ptRef);
                    }


                }
                reader.BaseStream.Seek(posMesBlock, SeekOrigin.Begin);
                for (int i = 0; i < offsets.Count; i++)
                {
                    int offset = (int)reader.BaseStream.Position;
                    offsets[i] =offset;
                    MessBlockRepack(outF + string.Format("\\{0,0:d4}.bin", i), offset+ptRef);
                    writer.Write(File.ReadAllBytes(outF + string.Format("\\{0,0:d4}.bin", i)));   
                }
                ptPosImgBlock = (int)reader.BaseStream.Position;
                reader.BaseStream.Seek(ptPosMessBlock, SeekOrigin.Begin);
                foreach (var off in offsets)
                {
                    writer.Write(off+ptRef);
                }
                writer.BaseStream.Seek(ptPosImgBlock, SeekOrigin.Begin);
                ImgRepack(outF + "\\IMG.bin");
                buffer = File.ReadAllBytes(outF + "\\IMG.bin");
                using (MemoryStream memory = new MemoryStream(buffer))
                {
                    BinaryWriter wr = new BinaryWriter(memory);
                    BinaryReader rdr = new BinaryReader(memory);
                    memory.Seek(8, SeekOrigin.Begin);
                    count = (rdr.ReadInt32()) / 0x10;
                    memory.Seek(0, SeekOrigin.Begin);
                    for (int i = 0; i < count; i++)
                    {
                        memory.Seek(8, SeekOrigin.Current);
                        int offset = rdr.ReadInt32() + ptPosImgBlock + ptRef;
                        memory.Seek(-4, SeekOrigin.Current);
                        wr.Write(offset);
                        memory.Seek(4, SeekOrigin.Current);
                    }

                    writer.Write(memory.ToArray());
                   
                }
                File.WriteAllBytes(fileName, memoryStream.ToArray());
            }


        }
        static void Usage()
        {
            Console.WriteLine("Kings_Field_Additional main.dat unpacker by Gil_Unx");
            Console.WriteLine("------------------------------------");
            Console.WriteLine
                (
                "ERROR: Argument tidak spesifik\n\n" +
                "Decrypt main.dat to main.dec: Kings_Field_Additional.exe -d main.dat\n\n" +
                "Encrypt main.dec to main.dat: Kings_Field_Additional.exe -e main.dec\n\n" +
                "Unpack main.dec: Kings_Field_Additional.exe -u main.dec\n\n" +
                "Repack main.dec: Kings_Field_Additional.exe -r main.dec\n\n" +
                "Repack main.dec spesifik index: Kings_Field_Additional.exe -r [index start] [index end] main.dec\n\n"
               );

        }
        static void Main(string[] args)
        {
            string mode = "";
            string input = "";
            if (args.Length < 2) { Usage(); return; }
            mode = args[0].ToUpper();
            input = args[1];
             ////
            {

                if (mode == "-D")
                {
                    DecryptDat(input);
                    Console.WriteLine("Decrypt {0} done...", Path.GetFileName(input));
                }
                if (mode == "-E")
                {
                    EncryptDat(input);
                    Console.WriteLine("Encrypt {0} done...", Path.GetFileName(input));
                }
                if (mode == "-U")
                {
                    if (args.Length == 4)
                    {
                        int start = int.Parse(args[2]);
                        int end = int.Parse(args[3]);
                        UnpackDat(input, end, start);
                    }
                    else
                    {
                        UnpackDat(input);
                    }
                    Console.WriteLine("Unpack {0} done...", Path.GetFileName(input));

                }
                if (mode == "-R")
                {
                    if (args.Length == 4)
                    {
                        int start = int.Parse(args[2]);
                        int end = int.Parse(args[3]);
                        RepackDat(input, end, start);
                    }
                    else
                    {
                        RepackDat(input);
                    }

                    Console.WriteLine("Repack {0} done...", Path.GetFileName(input));

                }
            }
            
            ////

        }
    }
}