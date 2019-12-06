using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace WebUploadService.Config
{
    public static class DBConnectConfig
    {
        public static readonly string DBAttach;
        static DBConnectConfig()
        {
            DBAttach = ConfigurationManager.ConnectionStrings["DBAttach"].ConnectionString;

        }
    }
}