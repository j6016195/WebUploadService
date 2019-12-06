using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //递归测试目录下的所有文件进行多线程上传测试
            string filePath = ConfigurationManager.AppSettings["aa"];
            GetFiles(filePath);
            Console.Read();
        }
        public static void GetFiles(string filePath)
        {
            //获取测试目录下的所有文件
            string[] files = Directory.GetFiles(filePath);
            //遍历每一个文件创建一个上传线程上传文件
            foreach (string file in files)
            {
                Thread thread = new Thread(() =>
                {
                    Upload(file);
                });
                thread.Start();
                Console.WriteLine(file);
            }
            string[] directories = Directory.GetDirectories(filePath);
            foreach (string directory in directories)
            {
                GetFiles(directory);
            }
        }
        public static void Upload(string filePath)
        {
            string fileName = filePath.Split('\\')[filePath.Split('\\').Length - 1];
            string error = string.Empty;
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    int unSendBufferLength = (int)stream.Length;
                    int bufferLength = 1024 * 1024 * 1;
                    byte[] buffer = new byte[bufferLength];
                    WebUploadSvc.WebUploadService client = new WebUploadSvc.WebUploadService();
                    Console.WriteLine("开始上传" + fileName);
                    string result = client.BeginSegmentUpload(fileName, unSendBufferLength);
                    var resultObj = JObject.Parse(result);
                    string attachId = resultObj["attachId"].ToString();
                    if (!string.IsNullOrEmpty(attachId))
                    {
                        int sortNo = 0;
                        while (unSendBufferLength > 0)
                        {
                            if (unSendBufferLength < bufferLength)
                            {
                                bufferLength = unSendBufferLength;
                                buffer = new byte[bufferLength];
                            }
                            stream.Read(buffer, 0, buffer.Length);
                            // 使用base64或byte两种方式传输都可以
                            //string base64Str = Convert.ToBase64String(buffer);
                            //result = client.SegmentUploadByBase64Str(attachId, base64Str, ++sortNo);
                            result = client.SegmentUpload(attachId, buffer, ++sortNo);
                            resultObj = JObject.Parse(result);
                            error = resultObj["errorFlag"].ToString();
                            Console.WriteLine(result);
                            if (error == "01") { throw new Exception("上传失败" + fileName + "片段" + sortNo); }

                            unSendBufferLength = unSendBufferLength - bufferLength;
                        }
                        result = client.EndSegmentUpload(attachId);
                        resultObj = JObject.Parse(result);
                        error = resultObj["errorFlag"].ToString();
                        if (error == "01") { throw new Exception("上传失败" + fileName); }
                        Console.WriteLine(fileName + "上传完毕");
                    }
                    else
                    {
                        Console.WriteLine(resultObj["errorMsg"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
