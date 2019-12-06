using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace WebUploadService.Config
{
    public static class CommonConfig
    {
        public static readonly string SegmentSavePath = ConfigurationManager.AppSettings["SegmentSavePath"];
        public static readonly string UploadTypeLimit = ConfigurationManager.AppSettings["UploadTypeLimit"];
        public static readonly string UploadSizeLimit = ConfigurationManager.AppSettings["UploadSizeLimit"];
        public static readonly string AttachSavePath = ConfigurationManager.AppSettings["AttachSavePath"];
        // public static readonly string AttachSavePath = ConfigurationManager.AppSettings["SegmentSavePath"];
    }
}