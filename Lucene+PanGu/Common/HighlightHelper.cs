using PanGu;
using PanGu.HighLight;

namespace Lucene_PanGu.Common
{
    public class HighlightHelper
    {
        /// <summary>
        /// 搜索结果高亮显示
        /// </summary>
        /// <param name="keyword"> 关键字 </param>
        /// <param name="content"> 搜索结果 </param>
        /// <returns> 高亮后结果 </returns>
        public static string HighLight(string keyword, string content)
        {
            // SimpleHTMLFormatter：这个类是一个HTML的格式类，构造函数有两个，一个是开始标签，一个是结束标签。
            SimpleHTMLFormatter simpleHTMLFormatter =
                new SimpleHTMLFormatter("<font style=\"color:red;" +
                                        "font-family:'Cambria'\"><b>", "</b></font>");
            // 创建 Highlighter ，输入HTMLFormatter 和 盘古分词对象Semgent
            Highlighter highlighter =
                new Highlighter(simpleHTMLFormatter,
                    new Segment());
            // 设置每个摘要段的字符数
            highlighter.FragmentSize = int.MaxValue;
            // 获取最匹配的摘要段
            var str = highlighter.GetBestFragment(keyword, content);
            return str;
        }
    }
}
