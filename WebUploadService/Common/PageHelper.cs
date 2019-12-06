using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebUploadService.Common
{
    public class PageHelper
    {

        /// <summary>
        /// 获取底部分页链接
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNow"></param>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        public static PageLink GetFootPagerLink(int pageSize, int pageNow, int totalCount, int linkCount)
        {
            PageLink link = new PageLink();
            //总页数
            int pageCount = (int)Math.Ceiling(Convert.ToDouble(totalCount) / Convert.ToDouble(pageSize));
            //首页页码
            int startIdx = 1;
            //尾页页码
            int endIdx = pageCount;
            if (pageCount < linkCount)
            {
                linkCount = pageCount;
            }
            //当前页前面最多页码个数
            int maxFront = (int)Math.Ceiling((double)(linkCount / 2));

            if (pageNow - maxFront > 1)
                startIdx = pageNow - maxFront;
            else
                maxFront = pageNow - 1;

            int maxBack = linkCount - maxFront - 1;
            if (pageNow + maxBack <= pageCount)
                endIdx = pageNow + maxBack;
            else
            {
                endIdx = pageCount;
                startIdx = pageCount - linkCount + 1;
            }
            link.Total = totalCount;
            link.StartPage = startIdx;
            link.EndPage = endIdx;
            link.CurrentPage = pageNow;
            link.PageSet = pageSize;
            link.PageNow = pageNow;
            link.TotalPage = pageCount;
            link.DataCount = totalCount;
            return link;

        }
    }


    public class PageLink
    {
        public int Total { get; set; }
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPage { get; set; }
        public int PageSet { get; set; }
        public int PageNow { get; set; }
        public int DataCount { get; set; }
    }
}
