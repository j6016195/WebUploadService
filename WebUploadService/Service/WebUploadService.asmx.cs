using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;
using WebUploadService.Common;
using WebUploadService.Core;

namespace WebUploadService.Service
{
    /// <summary>
    /// WebUploadService 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class WebUploadService : System.Web.Services.WebService
    {



        /// <summary>
        /// 使用base64字符串上传
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="base64Str"></param>
        [WebMethod]
        public string UploadByBase64Str(string attachName, string base64Str)
        {
            byte[] content = Convert.FromBase64String(base64Str);
            return UploadByByteArray(attachName, content);
        }
        /// <summary>
        /// 使用字节数组上传
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="attachName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        [WebMethod]
        public string UploadByByteArray(string attachName, byte[] content)
        {
            ResponseMessage result = new ResponseMessage();
            try
            {

                result = Uploader.CreateUploader(attachName, content.Length);
                if (result.errorFlag == "00")
                {
                    using (MemoryStream stream = new MemoryStream(content))
                    {
                        int bufferLength = 1024 * 1024 * 3;
                        byte[] buffer = new byte[bufferLength];
                        int unSendBufferLength = (int)stream.Length;
                        int sortNo = 0;
                        while (unSendBufferLength > 0)
                        {
                            if (unSendBufferLength < bufferLength)
                            {
                                bufferLength = unSendBufferLength;
                                buffer = new byte[bufferLength];
                            }
                            stream.Read(buffer, 0, buffer.Length);
                            var upResult = result.uploader.Upload(buffer, ++sortNo);
                            if (upResult.errorFlag != "00") { throw new Exception(upResult.errorMsg); }
                            unSendBufferLength = unSendBufferLength - bufferLength;
                        }
                        result = result.uploader.FinishUpload();
                        if (result.errorFlag != "00") { throw new Exception(result.errorMsg); }
                    }
                }

            }
            catch (Exception ex)
            {
                result.errorFlag = "01";
                result.errorMsg = ex.Message;
                Logger.SaveLog(ex.ToString());
            }
            return result.ToString();
        }
        [WebMethod]
        public string UploadByUrl(string attachName, string url)
        {
            ResponseMessage result = new ResponseMessage();
            try
            {

                using (WebClient client = new WebClient())
                {
                    using (Stream reader = client.OpenRead(url))
                    {
                        result = Uploader.CreateUploader(attachName, (int)reader.Length);
                        if (result.errorFlag == "00")
                        {
                            int bufferLength = 1024 * 1024 * 3;
                            byte[] buffer = new byte[bufferLength];
                            int sortNo = 0;
                            int unSendBufferLength = (int)reader.Length;
                            while (unSendBufferLength > 0)
                            {
                                if (unSendBufferLength < bufferLength)
                                {
                                    bufferLength = unSendBufferLength;
                                    buffer = new byte[bufferLength];
                                }
                                int size = reader.Read(buffer, 0, buffer.Length);

                                var upResult = result.uploader.Upload(buffer, ++sortNo);
                                if (upResult.errorFlag != "00") { throw new Exception(upResult.errorMsg); }
                                unSendBufferLength = unSendBufferLength - bufferLength;
                            }
                            result = result.uploader.FinishUpload();
                            if (result.errorFlag != "00") { throw new Exception(result.errorMsg); }

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                result.errorFlag = "01";
                result.errorMsg = ex.Message;
                Logger.SaveLog(ex.ToString());
            }
            return result.ToString();


        }
        /// <summary>
        /// 设置接口返回数据内容的类型（xml，json）
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="responseContentType"></param>
        /// <returns></returns>
        [WebMethod]
        public string SetResponseContentType(string transCode, string responseContentType)
        {
            return "";

        }

        /// <summary>
        /// 分段上传第一步操作（创建上传）
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="attachName"></param>
        /// <param name="contentSize"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        [WebMethod]
        public string BeginSegmentUpload(string attachName, int contentSize)
        {
            ResponseMessage result = new ResponseMessage();
            try
            {
                //通过授信码获取应用信息
                result = Uploader.CreateUploader(attachName, contentSize);
                
            }
            catch (Exception ex)
            {
                result.errorFlag = "01";
                result.errorMsg = ex.Message;
                result.attachId = string.Empty;
                Logger.SaveLog(ex.ToString());
            }
            return result.ToString();

        }
        /// <summary>
        /// 分段上传第二步操作（多次执行）
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="attachId"></param>
        /// <param name="segmentContent"></param>
        /// <param name="sortNo"></param>
        /// <returns></returns>
        [WebMethod]
        public string SegmentUpload(string attachId, byte[] segmentContent, int sortNo)
        {
            var uploadResult = new ResponseMessage();
            try
            {

                Uploader uploader = Uploader.GetUploader(attachId);
                uploadResult = uploader.Upload(segmentContent, sortNo);


            }
            catch (Exception ex)
            {
                uploadResult.errorFlag = "01";
                uploadResult.errorMsg = ex.Message;
                uploadResult.attachId = attachId;
                Logger.SaveLog(ex.ToString());
            }

            return uploadResult.ToString();

        }
        /// <summary>
        /// 分段上传第二步操作（多次执行）
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="attachId"></param>
        /// <param name="segmentContent"></param>
        /// <param name="sortNo"></param>
        /// <returns></returns>
        [WebMethod]
        public string SegmentUploadByBase64Str(string attachId, string base64Str, int sortNo)
        {
            byte[] content = Convert.FromBase64String(base64Str);
            return SegmentUpload(attachId, content, sortNo);
        }
        /// <summary>
        /// 分段上传第三步操作（结束上传）
        /// </summary>
        /// <param name="transCode"></param>
        /// <param name="attachId"></param>
        /// <returns></returns>
        [WebMethod]
        public string EndSegmentUpload(string attachId)
        {
            var uploadResult = new ResponseMessage();
            try
            {
                Uploader uploader = Uploader.GetUploader(attachId);
                uploadResult = uploader.FinishUpload();
            }
            catch (Exception ex)
            {
                uploadResult.errorFlag = "01";
                uploadResult.errorMsg = ex.Message;
                uploadResult.attachId = attachId;
                Logger.SaveLog(ex.ToString());
            }
            return uploadResult.ToString();
        }
    }
}