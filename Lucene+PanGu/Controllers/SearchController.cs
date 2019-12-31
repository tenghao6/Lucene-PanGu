using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene_PanGu.Common;
using Lucene_PanGu.Models;
using Microsoft.AspNetCore.Mvc;
using PanGu;
using Directory = System.IO.Directory;

namespace Lucene_PanGu.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : Controller
    {
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("createIndex")]
        public string CreateIndex()
        {
            //索引保存位置
            var indexPath = Directory.GetCurrentDirectory() + "/Index";
            if (!Directory.Exists(indexPath)) Directory.CreateDirectory(indexPath);
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());
            if (IndexWriter.IsLocked(directory))
            {
                //  如果索引目录被锁定（比如索引过程中程序异常退出），则首先解锁
                //  Lucene.Net在写索引库之前会自动加锁，在close的时候会自动解锁
                IndexWriter.Unlock(directory);
            }
            //Lucene的index模块主要负责索引的创建
            //  创建向索引库写操作对象  IndexWriter(索引目录,指定使用盘古分词进行切词,最大写入长度限制)
            //  补充:使用IndexWriter打开directory时会自动对索引库文件上锁
            //IndexWriter构造函数中第一个参数指定索引文件存储位置；
            //第二个参数指定分词Analyzer，Analyzer有多个子类，
            //然而其分词效果并不好，这里使用的是第三方开源分词工具盘古分词；
            //第三个参数表示是否重新创建索引，true表示重新创建（删除之前的索引文件），
            //最后一个参数指定Field的最大数目。
            IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), true,
                IndexWriter.MaxFieldLength.UNLIMITED);
            var txtPath = Directory.GetCurrentDirectory() + "/Upload/Articles";
            for (int i = 1; i <= 1000; i++)
            {
                //  一条Document相当于一条记录
                Document document = new Document();
                var title = "天骄战纪_" + i + ".txt";
                var content = System.IO.File.ReadAllText(txtPath + "/" + title, Encoding.Default);
                //  每个Document可以有自己的属性（字段），所有字段名都是自定义的，值都是string类型
                //  Field.Store.YES不仅要对文章进行分词记录，也要保存原文，就不用去数据库里查一次了
                document.Add(new Field("Title", "天骄战纪_" + i, Field.Store.YES, Field.Index.NOT_ANALYZED));
                //  需要进行全文检索的字段加 Field.Index. ANALYZED
                //  Field.Index.ANALYZED:指定文章内容按照分词后结果保存，否则无法实现后续的模糊查询 
                //  WITH_POSITIONS_OFFSETS:指示不仅保存分割后的词，还保存词之间的距离
                document.Add(new Field("Content", content, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                writer.AddDocument(document);
            }
            writer.Close(); // Close后自动对索引库文件解锁
            directory.Close(); //  不要忘了Close，否则索引结果搜不到
            return "索引创建完毕";
        }
        /// <summary>
        /// 盘古分词
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("standard")]
        public object Standard(string words)
        {
            var str = Participle.PanGu(words);
            return str;
        }
        /// <summary>
        /// 搜索
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("search")]
        public object Search(string keyWord, int pageIndex, int pageSize)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string indexPath = Directory.GetCurrentDirectory() + "/Index";
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NoLockFactory());
            IndexReader reader = IndexReader.Open(directory, true);
            //创建IndexSearcher准备进行搜索。
            IndexSearcher searcher = new IndexSearcher(reader);
            // 查询条件
            keyWord = GetKeyWordsSplitBySpace(keyWord, new PanGuTokenizer());
            //创建QueryParser查询解析器。用来对查询语句进行语法分析。
            //QueryParser调用parser进行语法分析，形成查询语法树，放到Query中。
            QueryParser msgQueryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Content", new PanGuAnalyzer(true));
            Query msgQuery = msgQueryParser.Parse(keyWord);
            //TopScoreDocCollector:盛放查询结果的容器
            //numHits 获取条数
            TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
            //IndexSearcher调用search对查询语法树Query进行搜索，得到结果TopScoreDocCollector。
            // 使用query这个查询条件进行搜索，搜索结果放入collector
            searcher.Search(msgQuery, null, collector);
            // 从查询结果中取出第n条到第m条的数据
            ScoreDoc[] docs = collector.TopDocs(0, 1000).scoreDocs;
            stopwatch.Stop();
            // 遍历查询结果
            List<ReturnModel> resultList = new List<ReturnModel>();
            var pm = new Page<ReturnModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRows = docs.Length
            };
            pm.TotalPages = pm.TotalRows / pageSize;
            if (pm.TotalRows % pageSize != 0) pm.TotalPages++;
            for (int i = (pageIndex - 1) * pageSize; i < pageIndex * pageSize && i < docs.Length; i++)
            {
                var doc = searcher.Doc(docs[i].doc);
                var content = HighlightHelper.HighLight(keyWord, doc.Get("Content"));
                var result = new ReturnModel
                {
                    Title = doc.Get("Title"),
                    Content = content,
                    Count = Regex.Matches(content, "<font").Count
                };
                resultList.Add(result);
            }

            pm.LsList = resultList;
            var elapsedTime = stopwatch.ElapsedMilliseconds + "ms";
            var list = new { list = pm, ms = elapsedTime };
            return list;
        }
        /// <summary>
        /// 对关键字进行盘古分词处理
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="ktTokenizer"></param>
        /// <returns></returns>
        private static string GetKeyWordsSplitBySpace(string keywords, PanGuTokenizer ktTokenizer)
        {
            StringBuilder result = new StringBuilder();
            ICollection<WordInfo> words = ktTokenizer.SegmentToWordInfos(keywords);

            foreach (WordInfo word in words)
            {
                if (word == null)
                {
                    continue;
                }
                result.AppendFormat("{0}^{1}.0 ", word.Word, (int)Math.Pow(3, word.Rank));
            }
            return result.ToString().Trim();
        }
    }
}