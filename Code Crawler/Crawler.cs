using System.Reflection;

namespace CodeCrawler;

public interface ICodeCrawler
{
    void CrawlAssembly(Assembly assembly, out List<Exception> errors, CancellationToken cancellationToken);

    void CrawlClass<T>(out List<Exception> errors, CancellationToken cancellationToken) where T : class;
    void CrawlClass(Type type, out List<Exception> errors, CancellationToken cancellationToken);

    void InvokeMethods<T>(MethodInfo[] methods, T obj, out List<Exception> errors,
                          CancellationToken cancellationToken) where T : class;
}

/// <summary>
/// Intended to be used in unit tests only.
/// </summary>
public sealed class CodeCrawler : ICodeCrawler
{
    public void CrawlAssembly(Assembly assembly, out List<Exception> errors, CancellationToken cancellationToken)
    {
        errors = new ();

        foreach(var type in assembly.GetTypes())
        {
            CrawlClass(type, out var classErrors, cancellationToken);
            errors.AddRange(classErrors);
        }
    }

    public void CrawlClass<T>(out List<Exception> errors, CancellationToken cancellationToken) where T : class => CrawlClass(typeof(T), out errors, cancellationToken);
    public void CrawlClass(Type type, out List<Exception> errors, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Testing {type.Name}");

        var obj = Activator.CreateInstance(type);
        Console.WriteLine($"Instance created");

        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        // TODO
        var properties   = type.GetProperties();
        var fields       = type.GetFields();
        var constructors = type.GetConstructors();

        InvokeMethods(methods,
                      obj,
                      out errors,
                      cancellationToken);
    }

    public void InvokeMethods<T>(  MethodInfo[] methods, T obj, out List<Exception> errors,
                                   CancellationToken cancellationToken) where T : class
    {
        errors = new ();

        foreach (var method in methods)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            InvokeMethod(method, obj, out var methodErrors);
            errors.AddRange(methodErrors);
        }
    }

    public void InvokeMethod<T>(MethodInfo method, T obj, out List<Exception> errors) where T : class
    {
        errors = new();

        try
        {
            var passedParams = BuildMethodParams(method, out var paramInfos);
            Console.WriteLine($"Testing {method.Name}({string.Join(',', passedParams)})");

            TestingReturnedValue(method.Invoke(obj,
                                               passedParams.ToArray()),
                                 method.ReturnType);

            TestNullableValues(obj,
                               passedParams,
                               paramInfos,
                               method);
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }
    }

    private void TestNullableValues<T>(T obj, List<object?> passedParams, ParameterInfo[] paramInfos,
                                       MethodInfo method) where T : class
    {
        for (var i = 0; i < passedParams.Count; i++)
        {
            var passedParamsModified = passedParams.ToArray();

            if (IsNullable(paramInfos[i].ParameterType, out var underlyingType))
            {
                var defaultValue = Activator.CreateInstance(underlyingType);

                passedParamsModified[i] = defaultValue;
            }

            TestingReturnedValue(method.Invoke(obj,
                                               passedParamsModified.ToArray()),
                                 method.ReturnType);
        }
    }
    private List<object?> BuildMethodParams(MethodInfo method, out ParameterInfo[] paramInfos)
    {
        List<object?> passedParams = new();
        paramInfos = method.GetParameters();

        foreach (var methodParam in paramInfos)
        {
            var type  = methodParam.ParameterType;
            var param = Activator.CreateInstance(type);

            if (IsNullable(type, out _) || param != null)
                passedParams.Add(param);
        }

        return passedParams;
    }
    private void TestingReturnedValue(object? result, Type? returnType = null)
    {
        bool voidMethod = returnType == null;

        if (voidMethod) return;

        if (IsNullable(returnType, out _))
        {
            returnType = Nullable.GetUnderlyingType(returnType);
        }

        if (result != null && result.GetType() != returnType)
            throw new("Method result is not valid");
    }

    private bool IsNullable<T>(out Type underlyingType) => IsNullable(typeof(T), out underlyingType);
    private bool IsNullable(Type type, out Type underlyingType)
    {
        underlyingType = Nullable.GetUnderlyingType(type);

        return underlyingType != null;
    }
}
