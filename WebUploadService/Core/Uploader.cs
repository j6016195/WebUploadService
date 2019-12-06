using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using WebUploadService.Common;
using WebUploadService.Config;

namespace WebUploadService.Core
{
    public class Uploader
    {
     
        private static object lockObj = new object();

        //上传文件类型限制
        private static List<string> _uploadTypeLimit;
        /// <summary>
        /// 片段存放根目录
        /// </summary>
        private string _segmentSavePath;
        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// 附件存放目录
        /// </summary>
        private string _attachSavePath = string.Empty;
        /// <summary>
        /// 缓存上传器列表字典
        /// </summary>
        private static Dictionary<string, Uploader> uploaderDictionary = new Dictionary<string, Uploader>();
        /// <summary>
        /// 上传器实例关联的附件
        /// </summary>
        private Attach _attach;
        public static int _defaultUploadSizeLimit = 1024 * 1024 * 100;//默认允许最大上传100M
        public static int _uploadSizeLimit;
        /// <summary>
        /// 上传片段内容大小限制
        /// </summary>
        private static int _segmentMaxSizeLimit = 1024 * 1024 * 10;//片段大小限制为最大10M
        /// <summary>
        /// 上传片段内容最小限制（最后一个片段）
        /// </summary>
        private static int _segmentMinSizeLimit = 1024 * 1024;
        /// <summary>
        /// 允许同时进行上传的上传器数量最大值
        /// </summary>
        private static readonly int _cachedUploaderMaxLimit = 300;
        /// <summary>
        /// 最近一次访问时间
        /// </summary>
        private DateTime _lastAccessDate;
        /// <summary>
        /// 上传器超期时限2分钟（当前时间-最近一次访问时间超过2分钟则认为上传器超时）
        /// 超时的上传器会在当上传器数量超过_cachedUploaderMaxLimit的时候被移除掉
        /// </summary>
        private static int _expiredTime = 2;
        private static readonly string _uploading = "Uploading";
        private static readonly string _finish = "Finish";

        static Uploader()
        {
            _initUploadLimitType();
            _initUploadLimitSize();
        }
        public Uploader(Attach attach)
        {
            this._attach = attach;
            this._lastAccessDate = Convert.ToDateTime(DateTime.Now.ToString());
            _segmentSavePath = string.Format(@"{0}\{1}\{2}\{3}\{4}",
                CommonConfig.SegmentSavePath,
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day, attach.AttachID);
            if (!Directory.Exists(_segmentSavePath))
            {
                Directory.CreateDirectory(_segmentSavePath);
            }
            _attachSavePath = string.Format(@"{0}\{1}\{2}\{3}",
                CommonConfig.AttachSavePath, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            if (!Directory.Exists(_attachSavePath))
            {
                Directory.CreateDirectory(_attachSavePath);
            }
            AddUploader(attach.AttachID, this);

        }
        /// <summary>
        /// 可上传类型初始化设置
        /// </summary>
        static void _initUploadLimitType()
        {
            try
            {
                _uploadTypeLimit = CommonConfig.UploadTypeLimit.Split(',').ToList();
            }
            catch (Exception ex)
            {
                _uploadTypeLimit = new List<string>();
                Logger.SaveLog(ex.ToString());
            }
        }
        /// <summary>
        /// 可上传大小初始化设置
        /// </summary>
        static void _initUploadLimitSize()
        {
            try
            {
                _uploadSizeLimit = Convert.ToInt32(CommonConfig.UploadSizeLimit);
            }
            catch (Exception ex)
            {
                _uploadSizeLimit = _defaultUploadSizeLimit;
                Logger.SaveLog(ex.ToString());
            }
        }

        /// <summary>
        /// 返回上传器关联的附件ID
        /// </summary>
        public string AttachId
        {
            get
            {
                return this._attach.AttachID;
            }
        }
        public DateTime DateCreated
        {
            get { return this._lastAccessDate; }
        }

        /// <summary>
        /// 从缓存字典中移除上传器
        /// </summary>
        /// <param name="attachId"></param>
        public static void RemoveUploader(string attachId)
        {
            lock (lockObj)
            {
                if (uploaderDictionary.ContainsKey(attachId))
                {
                    uploaderDictionary.Remove(attachId);

                }
            }

        }
        /// <summary>
        /// 从缓存字典中添加上传器
        /// </summary>
        /// <param name="attachId"></param>
        public static void AddUploader(string attachId, Uploader uploader)
        {
            lock (lockObj)
            {
                //判断上传器是否达到上限
                if (uploaderDictionary.Count < _cachedUploaderMaxLimit)
                {
                    if (!uploaderDictionary.ContainsKey(attachId))
                    {
                        uploaderDictionary.Add(attachId, uploader);
                        Logger.SaveLog("当前上传器数量：" + uploaderDictionary.Count);
                    }
                }
                else
                {
                    //如果上传器达到上限则检测上传器缓存中已经超时的上传器，并将其删除掉
                    var expiredUploaderList = uploaderDictionary.Where(kvp =>
                    {
                        return (DateTime.Now - kvp.Value._lastAccessDate).TotalMinutes > _expiredTime;
                    }).Select(kvp => kvp.Key).ToList();

                    //如果有超时的上传器则将其删除掉，如果没有则抛出达到上限异常
                    if (expiredUploaderList.Count > 0)
                    {
                        foreach (var key in expiredUploaderList)
                        {
                            //移除超时的上传器
                            uploaderDictionary.Remove(key);
                            // Logger.SaveLog("移除上传器：" + key);
                        }
                        uploaderDictionary.Add(attachId, uploader);
                    }
                    else
                    {
                        throw new Exception("当前上传数量达到上限");
                    }
                }
            }
        }
        /// <summary>
        /// 创建上传器开始上传文件
        /// </summary>       
        /// <returns></returns>
        public static ResponseMessage CreateUploader(string attachName, int contentSize)
        {
            ResponseMessage result = new ResponseMessage();
            using (TransactionScope scope = new TransactionScope())
            {
                try
                {
                    string[] attachNameSplitArray = attachName.Split('.');
                    //获取后缀
                    string extension = attachNameSplitArray.Length > 1 ? attachNameSplitArray[attachNameSplitArray.Length - 1] : "";
                    //验证文件类型
                    if (!_uploadTypeLimit.Contains(extension)) { throw new Exception(string.Format("不允许上传{0}类型的文件", extension)); }
                    //验证文件大小
                    if (contentSize > _uploadSizeLimit) { throw new Exception(string.Format("文件太大")); }
                    Attach attach = Attach.Insert(attachName, contentSize, extension);
                    //创建新的上传器
                    if (attach == null) { throw new Exception("无法获取附件信息"); }
                    Uploader uploader = new Uploader(attach);
                    result.errorFlag = "00";
                    result.errorMsg = "创建上传器成功";
                    result.attachId = attach.AttachID;
                    result.uploader = uploader;
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    result.errorFlag = "01";
                    result.errorMsg = ex.Message;
                    result.attachId = string.Empty;
                    result.uploader = null;
                    Logger.SaveLog(ex.ToString());
                }
            }
            return result;
        }
        /// <summary>
        /// 获取上传器
        /// </summary>
        /// <param name="attachId"></param>
        /// <returns></returns>
        public static Uploader GetUploader(string attachId)
        {
            Uploader uploader = null;
            try
            {
                //如果缓存上传器字典中有该附件的上传器则直接从缓存中获取，否则重新读表生成新的上传器
                if (uploaderDictionary.ContainsKey(attachId))
                {
                    uploader = uploaderDictionary[attachId];
                }
                else
                {
                    //重新获取上传器
                    Attach attach = Attach.GetAttach(attachId);
                    if (attach != null)
                    {
                        if (attach.UploadStatus == _uploading)
                        {
                            uploader = new Uploader(attach);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.SaveLog(ex.ToString());
            }
            return uploader;
        }
        /// <summary>
        /// 片段上传
        /// </summary>
        /// <param name="segmentContent"></param>
        /// <param name="sortNo"></param>
        /// <returns></returns>
        public ResponseMessage Upload(byte[] segmentContent, int sortNo)
        {
            ResponseMessage uploadResult = new ResponseMessage();
            uploadResult.attachId = this._attach.AttachID;
            try
            {
                //验证片段大小
                if (segmentContent.Length > _segmentMaxSizeLimit) { throw new Exception(string.Format("片段内容大小{0}字节，超过上传最大限制的{1}字节", segmentContent.Length, _segmentMaxSizeLimit)); }
                //当片段小于1M的时候,判断是否是最后一个片段（如果剩余容量大于当前片段则表明不是最后一个片段）
                if (segmentContent.Length < _segmentMinSizeLimit && this._attach.RemainingCapacity > segmentContent.Length)
                {
                    throw new Exception(string.Format("片段太小，如果不是文件的最后一个片段，请确保片段大于{0}字节", _segmentMinSizeLimit));
                }
                //锁定当前Uploader实例,同一个附件的多个片段之间采用同步方式上传
                lock (this)
                {
                    this._lastAccessDate = Convert.ToDateTime(DateTime.Now.ToString());
                    string serializeName = string.Format("{0}.segment", sortNo);
                    string pathName = Path.Combine(_segmentSavePath, serializeName);
                    File.WriteAllBytes(pathName, segmentContent);
                    int i = this._attach.InsertSegment(segmentContent, sortNo, pathName);
                    if (i == 0) { throw new Exception("上传片段失败"); }
                    uploadResult.errorMsg = "上传片段成功";
                    uploadResult.errorFlag = "00";
                    uploadResult.uploader = this;
                }
            }
            catch (Exception ex)
            {
                uploadResult.errorMsg = ex.Message;
                uploadResult.errorFlag = "01";
                uploadResult.ResultType = "xml";
            }
            return uploadResult;
        }
        /// <summary>
        /// 结束上传
        /// </summary>
        /// <returns></returns>
        public ResponseMessage FinishUpload()
        {
            ResponseMessage result = new ResponseMessage();
            result.attachId = this._attach.AttachID;
            try
            {
                lock (this)
                {
                    this._lastAccessDate = Convert.ToDateTime(DateTime.Now.ToString());
                    if (this._attach.UploadStatus == _uploading)
                    {
                        //将片段排序并合并生成附件；
                        var sortedSegments = this._attach.Segments.OrderBy(segment => segment.SerialNumber);
                        //string virtualPath = string.Format("~/Files/{0}/{1}/{2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                        //string virtualFileName = string.Format("{0}/{1}",
                        //       virtualPath,
                        //       string.IsNullOrEmpty(this._attach.Extension)
                        //       ? this._attach.AttachID
                        //       : this._attach.AttachID + "." + this._attach.Extension);
                        // string fileDirectory = System.Web.Hosting.HostingEnvironment.MapPath(virtualPath);                  
                        // if (!Directory.Exists(fileDirectory)) { Directory.CreateDirectory(fileDirectory); }
                        string fileSaveName = Path.Combine(_attachSavePath,
                            string.IsNullOrEmpty(this._attach.Extension)
                            ? this._attach.AttachID
                            : this._attach.AttachID + "." + this._attach.Extension);
                        if (File.Exists(fileSaveName)) { File.Delete(fileSaveName); }
                        using (FileStream fs = new FileStream(fileSaveName, FileMode.CreateNew, FileAccess.Write))
                        {
                            foreach (var segment in sortedSegments)
                            {
                                var segmentContent = File.ReadAllBytes(segment.SegmentContent);
                                fs.Write(segmentContent, 0, segmentContent.Length);
                            }
                        }
                        //更新状态
                        this._attach.UploadStatus = _finish;
                        this._attach.EndDate = DateTime.Now;
                        this._attach.SavePath = fileSaveName;
                        int i = this._attach.Update();
                        if (i != 0)
                        {
                            //移除上传器
                            RemoveUploader(this._attach.AttachID);
                            Logger.SaveLog("上传完毕移除上传器：" + this._attach.AttachID);
                            result.errorFlag = "00";
                            result.errorMsg = "上传完毕";
                        }
                        else
                        {
                            this._attach.UploadStatus = _uploading;
                            this._attach.EndDate = null;
                            this._attach.SavePath = null;
                            throw new Exception("更新上传状态失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.errorMsg = "01";
                result.errorMsg = ex.Message;
                Logger.SaveLog(ex.ToString());
            }
            return result;
        }


    }
}