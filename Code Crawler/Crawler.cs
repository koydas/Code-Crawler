using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Code_Crawler;

[TestClass]
public class Crawler
{
    Type[] _types = Assembly.GetExecutingAssembly().GetTypes();

    [TestMethod]
    public void Crawl()
    {
        foreach (var type in _types.Where(x => x != GetType()))
            Crawl(type);
    }

    private static void Crawl(Type type)
    {
        var obj = Activator.CreateInstance(type);

        foreach(var methodInfo in type.GetMethods())
        {
            methodInfo.Invoke(obj, Array.Empty<object>());
        }
    }
    

}