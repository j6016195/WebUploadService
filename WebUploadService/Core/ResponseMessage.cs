
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using WebUploadService.Common;
using WebUploadService.Core;

namespace WebUploadService.Core
{
    public class ResponseMessage
    {
        [ResultAttribute]
        public string errorFlag { get; set; }
        [ResultAttribute]
        public string errorMsg { get; set; }
        public string ResultType { get; set; }
        [ResultAttribute]
        public string attachId { get; set; }
        public Uploader uploader { get; set; }
        public override string ToString()
        {
            string result = string.Empty;
            try
            {
                switch (this.ResultType)
                {
                    case "xml":
                        XmlDocument doc = new XmlDocument();
                        XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                        doc.AppendChild(dec);
                        //创建根节点
                        XmlElement root = doc.CreateElement("receiveData");
                        doc.AppendChild(root);
                        foreach (System.Reflection.PropertyInfo p in this.GetType().GetProperties())
                        {
                            if (p.GetCustomAttributes(typeof(ResultAttribute), true).Length > 0)
                            {
                                var value = p.GetValue(this, null);
                                if (value != null)
                                {
                                    XmlNode nodeFlag = doc.CreateElement(p.Name);
                                    nodeFlag.InnerText = value.ToString();
                                    root.AppendChild(nodeFlag);
                                }
                            }
                        }
                        result = doc.InnerXml;
                        break;
                    case "json":
                    default:
                        result = JsonConvert.SerializeObject(this);
                        break;

                }
            }
            catch (Exception ex)
            {
                Logger.SaveLog(ex.ToString());
                throw ex;
            }
            return result;
        }
    }
    public class ResultAttribute : Attribute
    {

    }
}