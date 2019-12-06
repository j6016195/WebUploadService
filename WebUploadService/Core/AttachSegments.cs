using System;
using System.Data.SqlClient;
using WebUploadService.Common;
using WebUploadService.Config;

namespace WebUploadService.Core
{
    public class AttachSegments
    {
        public string AttachId { get; set; }
        public int SegmentSize { get; set; }
        public int SerialNumber { get; set; }
        public string SegmentContent { get; set; }
        public DateTime UploadDate { get; set; }        
        public string IpAddr { get; set; }
        public static AttachSegments Insert(string attachId, int segmentContentSize, int sortNo, string pathName)
        {
            AttachSegments result = null;

            try
            {
                string cmdTxt = @"insert into TBL_AttachSegments(AttachId,SegmentSize,SerialNumber,SegmentContent,UploadDate,IpAddr)
                                  values (@AttachId,@SegmentSize,@SerialNumber,@SegmentContent,@UploadDate,@IpAddr) ";
                SqlParameter[] parameters = new SqlParameter[]{
                    new SqlParameter("@AttachId",attachId),                     
                    new SqlParameter("@SegmentSize",segmentContentSize),
                    new SqlParameter("@SerialNumber",sortNo),
                    new SqlParameter("@SegmentContent",pathName),
                    new SqlParameter("@UploadDate",DateTime.Now.ToString()),                 
                    new SqlParameter("@IpAddr",""),
                 };
                SqlHelper.ExecuteNonQuery(DBConnectConfig.DBAttach, System.Data.CommandType.Text, cmdTxt, parameters);
                result = new AttachSegments
                         {
                           
                             AttachId = attachId,
                             IpAddr = "",
                             SegmentSize = segmentContentSize,
                             SegmentContent = pathName,
                             SerialNumber = sortNo
                         };
            }
            catch (Exception ex)
            {
                result = null;
                Logger.SaveLog(ex.ToString());
            }
            return result;
        }
    }
}