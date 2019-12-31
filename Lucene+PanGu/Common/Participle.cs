using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Analysis.Standard;

namespace Lucene_PanGu.Common
{
    public static class Participle
    {
        /// <summary>
        /// 盘古分词
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static object PanGu(string words)
        {
            Analyzer analyzer = new PanGuAnalyzer();
            TokenStream tokenStream = analyzer.TokenStream("", new StringReader(words));
            Lucene.Net.Analysis.Token token = null;
            var str = "";
            while ((token = tokenStream.Next()) != null)
            {
                string word = token.TermText(); // token.TermText() 取得当前分词
                str += word + "   |  ";
            }
            return str;
        }
    }
}
