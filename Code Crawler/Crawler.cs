using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Code_Crawler
{
    [TestClass]
    public class Crawler
    {
        Type[] _types = Assembly.GetExecutingAssembly().GetTypes();

        [TestMethod]
        public void Crawl()
        {
            foreach (var type in _types)
            {
                type.GetMethods()
            }
        }
    }
}