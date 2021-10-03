using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using System.Web.Script.Serialization;

using System.Text.RegularExpressions;
using System.Collections;

namespace SubtitleTranslator
{
    
    class Program
    {
        static string appId = null, secretKey = null;
        static void Main(string[] args)
        {
            initConfig();
            initTranslate();
        }

        public static void initConfig()
        {
            Readjson();
            if (appId == "" && secretKey == "")
            {
                Console.Write("输入百度翻译appId\n");
                appId = Console.ReadLine();
                Console.Write("输入百度翻译密钥\n");
                secretKey = Console.ReadLine();
                Writejson(appId, secretKey);
            }

        }
        //准备翻译
        public static void initTranslate()
        {
            Console.Write("输入文件路径，按回车开始翻译\n");
            String pathroot = Console.ReadLine().Replace("\"", "");
            Console.Write("输入一次读取行数（20，30，……）\n");
            String speedNum = Console.ReadLine();
            List<String> pathList = new List<String>();
            if (pathroot != null)
            {
                if (Directory.Exists(pathroot))
                {
                    pathList = Directory.GetFiles(@pathroot, "*.srt", SearchOption.AllDirectories).where(s=>!s.Contains("chi_eng_")).ToList<string>();
                }
                else
                {
                    pathList.AddRange(Regex.Split(pathroot, ".srt").AsEnumerable<string>());
                }
                foreach (String path in pathList)
                {
                    Console.Write("------------------" + Path.GetFileNameWithoutExtension(path) + "------------------\n");
                    if (speedNum == null || speedNum == "")
                        speedNum = "100";
                    translateFile(path, pathList.IndexOf(path), Convert.ToInt32(speedNum));
                }
            }
            initTranslate();
        }
        // 计算MD5值
        public static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }
            // 返回加密的字符串
            return sb.ToString();
        }

        //翻译句子
        public static List<Trans_resultItem> translateLine(String str)
        {
            // 原文
            string q = str;
            string qstr = "";
            // 源语言
            string from = "auto";
            // 目标语言
            string to = "zh";
            Random rd = new Random();
            string salt = rd.Next(100000).ToString();
            string sign = EncryptString(appId + q + salt + secretKey);
            string url = "http://api.fanyi.baidu.com/api/trans/vip/translate";//?
            qstr += "q=" + HttpUtility.UrlEncode(q);
            qstr += "&from=" + from;
            qstr += "&to=" + to;
            qstr += "&appid=" + appId;
            qstr += "&salt=" + salt;
            qstr += "&sign=" + sign;
            qstr += "&action=" + 1;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            byte[] bs = Encoding.ASCII.GetBytes(qstr);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bs.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Close();
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            JsonSerializer serializer = new JsonSerializer();
            StringReader sr = new StringReader(retString);
            resp_str list = (resp_str)serializer.Deserialize(new JsonTextReader(sr), typeof(resp_str));
            List<Trans_resultItem> result = list.trans_result;
            return result;
        }

        //翻译单个文件
        public static void translateFile(String path, int fileIndex, int num)
        {
            try
            {
                int maxNumOnce = num;
                String name = Path.GetFileNameWithoutExtension(path);
                // using 语句也能关闭 StreamReader
                using (StreamReader sr = new StreamReader(path))
                {
                    int indexNum = 0;
                    List<string> timeInterval = new List<string>();
                    List<string> sentence = new List<string>();
                    string line;
                    // 从文件读取并显示行，直到文件的末尾 
                    Console.Write("-----开始读取文件！-----\n");
                    while ((line = sr.ReadLine()) != null)
                    {
                        //正则表达式判断是否为数字
                        if (Regex.IsMatch(line, @"^\d+$"))
                        {
                            indexNum++;
                        }
                        else if ((line.IndexOf("-->") > -1))
                        {
                            timeInterval.Add(line);
                        }
                        else if (line != "")
                        {
                            if (sentence.Count < indexNum)
                            {
                                sentence.Add(line);
                            }
                            else
                            {
                                sentence[indexNum - 1] += " " + line;
                            }
                        }
                    }
                    Console.Write("-----读取文件完毕！-----\n");
                    Console.Write("-----开始创建文件！-----\n");
                    using (StreamWriter sw = new StreamWriter(path.Replace(name, "chi_eng_" + name)))
                    {
                        //界面初始显示
                        {
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.Write(new string(' ', 50));
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.SetCursorPosition(52, Console.CursorTop);
                            Console.Write("0/" + sentence.Count);
                        }
                        //一次读取多行文本进行翻译
                        for (int count = 0; count <= sentence.Count / maxNumOnce; count++)
                        {
                            string sentStr = "";
                            for (int i = 0; i < maxNumOnce && count * maxNumOnce + i < sentence.Count; i++)
                            {
                                sentStr += sentence[count * maxNumOnce + i] + "\n";
                            }
                            if (sentStr == "")
                            {
                                continue;
                            }
                            List<Trans_resultItem> transResult = translateLine(sentStr);
                            Thread.Sleep(1000);

                            for (int i = 0; i < transResult.Count; i++)
                            {
                                indexNum = count * maxNumOnce + i + 1;
                                sw.WriteLine(indexNum);
                                sw.WriteLine(timeInterval[indexNum - 1]);
                                sw.WriteLine(transResult[i].dst);
                                sw.WriteLine(transResult[i].src);
                                sw.WriteLine("");
                                //界面变化显示
                                {
                                    Console.BackgroundColor = ConsoleColor.Yellow;
                                    Console.SetCursorPosition((int)Math.Round(((double)indexNum / sentence.Count) * 50.0), Console.CursorTop);
                                    Console.Write(" ");
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.SetCursorPosition(52, Console.CursorTop);
                                    Console.Write("{0}/" + sentence.Count, indexNum);
                                }
                                Thread.Sleep(1);
                            }
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\n-----写入文件完成！-----\n\n");
                }


            }
            catch (Exception e)
            {
                // 向用户显示出错消息
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\n翻译过程出现错误:");
                Console.WriteLine(e.Message+"\n");
            }
        }

        //读取json文件
        public static void Readjson()
        {
            if (!File.Exists("config.json"))
            {
                string a = "{\"appId\": \"\",\"secretKey\": \"\"}";
                StreamWriter sw = new StreamWriter("config.json");
                sw.WriteLine(a);//按行写
                sw.Close();
            }
            using (StreamReader file = File.OpenText("config.json"))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    appId = o["appId"].ToString();
                    secretKey = o["secretKey"].ToString();
                }
            }
        }
        public static void Writejson(string appId, string secretKey)
        {
            try
            {
                string json = File.ReadAllText("config.json");
                dynamic jsonObj = JsonConvert.DeserializeObject(json);
                jsonObj["appId"] = appId;
                jsonObj["secretKey"] = secretKey;
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText("config.json", output);
            }
            catch { }

        }
    }
}
