using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FileReceiver
{
    class Program
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        static List<string> InstLevelName = new List<string>()
        {
            "省国资委", "省属国有企业", "市县国资监管机构", "其他",
        };

        public static Dictionary<string, string> fileTypes = new Dictionary<string, string>()
        {
            {"文档", @"doc|docx|txt"},
            {"图片", @"jpg|png|gif"},
            {"视频", @"mp4|flv|avi|rmvb|wmv|mkv"},
        };

        public static List<List<string>> instName = new List<List<string>>()
        {
            new List<string>()
            {
                "委领导班子成员",
                "委机关各处室",
                "研究中心",
                "浙江产权交易所",
            },

            new List<string>()
            {
                "浙江省国有资本运营有限公司",
                "物产中大集团股份有限公司",
                "浙江省建设投资集团股份有限公司",
                "浙江省机电集团有限公司",
                "浙江省国际贸易集团有限公司",
                "浙江省旅游集团有限责任公司",
                "杭州钢铁集团有限公司",
                "巨化集团有限公司",
                "浙江省能源集团有限公司",
                "浙江省交通投资集团有限公司",
                "浙江省农村发展集团有限公司",
                "浙江省机场集团有限公司",
                "浙江省海港投资运营集团有限公司",
                "浙江省二轻集团有限公司",
                "浙江安邦护卫集团有限公司",
                "浙商银行股份有限公司",
                "浙江省农村信用社联合社",
                "浙江财通证券股份有限公司",
                "浙江出版联合集团有限公司",
                "浙江省文化产业投资集团有限公司",
                "浙江省金融控股有限公司",
                "浙江省担保集团有限公司",
                "浙江浙勤集团有限公司",
                "浙江省财务开发有限责任公司",
            },

            new List<string>()
            {
                "杭州市国资委",
                "宁波市国资委",
                "温州市国资委",
                "湖州市国资委",
                "嘉兴市国资委",
                "绍兴市国资委",
                "金华市国资委",
                "衢州市国资委",
                "舟山市财政局",
                "台州市国资委",
                "丽水市国资委",
            },

            new List<string>()
            {
                "",
            },
        };

        public static Dictionary<string, int> fileTotalCount = new Dictionary<string, int>();

        const string ORI_DIR = @"e:\SharedFile\宣传资料\时间顺序\";
        const string TYPE_DIR = @"e:\SharedFile\宣传资料\文件分类\";
        const string INST_DIR = @"e:\SharedFile\宣传资料\单位分类\";

        static void Main(string[] args)
        {
            byte[] response = new byte[4];
            TcpListener listener = new TcpListener(IPAddress.Any, 2309);
            Console.WriteLine("TcpListener start");
            listener.Start();
            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Accept client {0}", client.Client.LocalEndPoint.ToString());
                    fileTotalCount.Clear();
                    NetworkStream ns = client.GetStream();
                    StreamReader reader = new StreamReader(ns);
                    int fileCount = Convert.ToInt32(reader.ReadLine());
                    string dateTime = reader.ReadLine();
                    for (int i = 0; i < fileCount; i++)
                    {
                        int fileSize = Convert.ToInt32(reader.ReadLine());
                        Console.WriteLine(fileSize);

                        string filePath = reader.ReadLine();
                        Console.WriteLine(filePath);

                        int level = Convert.ToInt32(reader.ReadLine());
                        Console.WriteLine(level);

                        int instID = Convert.ToInt32(reader.ReadLine());
                        Console.WriteLine(instID);

                        string keyword = reader.ReadLine();
                        Console.WriteLine(keyword);

                        ns.Write(response);

                        byte[] buffer = new byte[fileSize];
                        int receivedSize = 0;
                        int readSize = 1024;

                        while (receivedSize < fileSize)
                        {
                            readSize = fileSize - receivedSize < readSize ? fileSize - receivedSize : readSize;
                            int read = ns.Read(buffer, receivedSize, readSize);
                            receivedSize += read;
                        }

                        Console.WriteLine("FileStream write {0}", filePath);

                        string typeName = GetFileType(filePath);
                        Console.WriteLine(typeName);

                        string finalPath = ORI_DIR + dateTime + @"\" + filePath;
                        CreatePath(finalPath);
                        using (FileStream fs = new FileStream(finalPath, FileMode.Create))
                        {
                            fs.Write(buffer);
                            fs.Flush();
                            fs.Close();

                            string extension = Path.GetExtension(finalPath);

                            string typeLink = GetLinkByType(dateTime, typeName, level, instID, keyword, extension);
                            CreatePath(typeLink);
                            File.Delete(typeLink);
                            bool result = CreateHardLink(typeLink, finalPath, IntPtr.Zero);

                            string instLink = GetLinkByInst(dateTime, typeName, level, instID, keyword, extension);
                            CreatePath(instLink);
                            File.Delete(instLink);
                            result = CreateHardLink(instLink, finalPath, IntPtr.Zero);
                        }

                        ns.Write(response);
                    }
                    Console.WriteLine("Client close");
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        static void CreatePath(string path)
        {
            int index = path.LastIndexOf(@"\");
            if (index > 0)
            {
                path = path.Substring(0, index);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        static string GetFileType(string path)
        {
            foreach (var item in fileTypes)
            {
                if (Regex.IsMatch(path, item.Value))
                {
                    return item.Key;
                }
            }

            return "其他";
        }

        static string GetLinkByType(string dateTime, string typeName, int instLevel, int instID, string keyword, string extension)
        {
            string path = TYPE_DIR + typeName
                + @"\" + InstLevelName[instLevel]
                + @"\" + instName[instLevel][instID];

            if (keyword != "" && keyword != null)
            {
                keyword = keyword + @"_";
            }
            else
            {
                keyword = "";
            }

            if (!fileTotalCount.ContainsKey(path + typeName))
            {
                fileTotalCount.Add(path + typeName, 1);
                return path + @"\" + dateTime + @"_" + keyword + "1" + extension;
            }

            fileTotalCount[path + typeName] = fileTotalCount[path + typeName] + 1;

            return path + @"\" + dateTime + @"_" + keyword + fileTotalCount[path + typeName].ToString() + extension;
        }

        static string GetLinkByInst(string dateTime, string typeName, int instLevel, int instID, string keyword, string extension)
        {
            string path = INST_DIR
                + @"\" + InstLevelName[instLevel]
                + @"\" + instName[instLevel][instID]
                + @"\" + dateTime;

            if (keyword != "" && keyword != null)
            {
                path = path + @"\" + keyword;
            }

            if(!fileTotalCount.ContainsKey(path))
            {
                fileTotalCount.Add(path, 1);
                return path + @"\" + "1" + extension;
            }

            fileTotalCount[path] = fileTotalCount[path] + 1;

            return path + @"\" + fileTotalCount[path].ToString() + extension;
        }
    }
}
