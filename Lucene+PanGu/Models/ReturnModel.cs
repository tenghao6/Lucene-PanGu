using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lucene_PanGu.Models
{
    public class ReturnModel
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 出现次数
        /// </summary>
        public int Count { get; set; }
    }
}
