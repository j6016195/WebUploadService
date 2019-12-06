using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using WebUploadService.Common;
using WebUploadService.Config;
namespace WebUploadService.Core
{
    public class Attach
    {
        public string AttachID { get; set; }
        public string AttachName { get; set; }
        public string UploadMode { get; set; }
        public string UploadStatus { get; set; }
        public int ContentSize { get; set; }
        public int SegmentNumber { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string IpAddr { get; set; }
        public string SavePath { get; set; }
        public string Extension { get; set; }
        //剩余大小
        public int RemainingCapacity
        {
            get
            {
                return this.ContentSize - this.Segments.Select(segment => segment.SegmentSize).Sum();
            }
        }
        //片段列表
        public List<AttachSegments> Segments { get; set; }
        public static Attach Insert(string attachName, int contentSize, string extension)
        {
            Attach attach = null;
            try
            {
                string attachId = Guid.NewGuid().ToString();
                DateTime beginDate = DateTime.Now;
                string uploadMode = "Segment";
                string uploadStatus = "Uploading";
                string cmdTxt = @"insert into TBL_Attachs(AttachId,AttachName,UploadMode,UploadStatus,ContentSize,BeginDate,IpAddr,Extension)
                                  values(@AttachId,@AttachName,@UploadMode,@UploadStatus,@ContentSize,@BeginDate,@IpAddr,@Extension) ";
                SqlParameter[] parameters = new SqlParameter[]{
                    new SqlParameter("@AttachId",attachId),
                    new SqlParameter("@AttachName",attachName),
                    new SqlParameter("@UploadMode",uploadMode),
                    new SqlParameter("@UploadStatus",uploadStatus),
                    new SqlParameter("@ContentSize",contentSize),
                    new SqlParameter("@BeginDate",beginDate.ToString()),               
                    new SqlParameter("@IpAddr",""),
                    new SqlParameter("@Extension",extension),
                 };
                SqlHelper.ExecuteNonQuery(DBConnectConfig.DBAttach, System.Data.CommandType.Text, cmdTxt, parameters);
                attach = new Attach
                {
                    AttachID = attachId,
                    AttachName = attachName,
                    BeginDate = beginDate,
                    ContentSize = contentSize,
                    UploadMode = uploadMode,
                    UploadStatus = uploadStatus,
                    Segments = new List<AttachSegments>(),
                    Extension = extension

                };
            }
            catch (Exception ex)
            {
                attach = null;
                Logger.SaveLog(ex.ToString());
            }
            return attach;
        }
        public static Attach GetAttach(string attachId)
        {
            Attach attach = null;
            try
            {
                string cmdTxt = "select * from TBL_Attachs where AttachId=@AttachId ";
                SqlParameter[] parameters = new SqlParameter[]{
                           new SqlParameter("@AttachId",attachId)
                    };
                DataSet ds = SqlHelper.ExecuteDataSet(DBConnectConfig.DBAttach, cmdTxt, parameters);
                if (ds.Tables[0].Rows.Count == 1)
                {
                    DataRow r = ds.Tables[0].Rows[0];
                    attach = new Attach();
                    attach.AttachID = r["AttachID"].ToString();
                    attach.AttachName = r["AttachName"].ToString();
                    attach.UploadMode = r["UploadMode"].ToString();
                    attach.UploadStatus = r["UploadStatus"].ToString();
                    attach.ContentSize = Convert.ToInt32(r["ContentSize"]);
                    attach.BeginDate = Convert.ToDateTime(r["BeginDate"]);
                    attach.EndDate = (DateTime?)r["EndDate"];
                    attach.Segments = new List<AttachSegments>();
                    attach.Extension = r["ExtenSion"].ToString();
                    attach.SavePath = r["SavePath"].ToString();
                    cmdTxt = "select * from TBL_AttachSegments where attachId=@AttachId order by SerialNumber asc";
                    parameters = new SqlParameter[]{            
                        new SqlParameter("@AttachId",attach.AttachID)
                        };
                    ds = SqlHelper.ExecuteDataSet(DBConnectConfig.DBAttach, cmdTxt, parameters);
                    foreach (DataRow rSegment in ds.Tables[0].Rows)
                    {
                        attach.Segments.Add(new AttachSegments
                        {

                            AttachId = rSegment["AttachId"].ToString(),
                            IpAddr = "",
                            SegmentSize = (int)rSegment["SegmentSize"],
                            SegmentContent = rSegment["SegmentContent"].ToString(),
                            SerialNumber = (int)rSegment["SerialNumber"]
                        });

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.SaveLog(ex.ToString());
                attach = null;
            }
            return attach;
        }
        public int InsertSegment(byte[] segmentContent, int sortNo, string pathName)
        {
            int i = 0;
            if (this.RemainingCapacity >= segmentContent.Length)
            {
                AttachSegments segment = AttachSegments.Insert(this.AttachID, segmentContent.Length, sortNo, pathName);
                if (segment != null)
                {
                    this.Segments.Add(segment);
                }
                i = 1;
            }
            else
            {
                throw new Exception("上传的文件片段内容大小超出文件总大小");
            }
            return i;
        }
        public int GetRemainingCapacity()
        {
            int remainingCapacity = 0;
            try
            {
                string cmdTxt = "select sum(SegmentSize) from TBL_AttachSegments where AttachId=@AttachId";
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@AttachId", this.AttachID) };
                object o = SqlHelper.ExecuteScalar(DBConnectConfig.DBAttach, CommandType.Text, cmdTxt, parameters);
                int segmentSizes = Convert.ToInt32(o);
                if (this.ContentSize > segmentSizes)
                {
                    remainingCapacity = this.ContentSize - segmentSizes;
                }
            }
            catch (Exception ex)
            {
                Logger.SaveLog(ex.ToString());
                remainingCapacity = 0;
            }
            return remainingCapacity;

        }
        public int Update()
        {
            string cmdTxt = "update  TBL_Attachs  set UploadStatus=@UploadStatus , EndDate=@EndDate,SavePath=@SavePath  where attachId=@AttachId";
            SqlParameter[] parameters = new SqlParameter[]{            
                          new SqlParameter("@AttachId",this.AttachID),
                          new SqlParameter("@UploadStatus",this.UploadStatus),
                           new SqlParameter("@EndDate",this.EndDate),
                             new SqlParameter("@SavePath",this.SavePath)
                        };
            return SqlHelper.ExecuteNonQuery(DBConnectConfig.DBAttach, CommandType.Text, cmdTxt, parameters);
        }
    }
}