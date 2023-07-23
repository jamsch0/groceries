namespace Groceries.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

public enum TurboStreamAction
{
    Append,
    Prepend,
    Replace,
    Update,
    Remove,
    Before,
    After,
}

public class TurboStreamResult : ActionResult, IStatusCodeActionResult
{
    public TurboStreamResult(TurboStreamAction action, string target)
    {
        Action = action;
        Target = target;
    }

    public TurboStreamAction Action { get; set; }

    public string Target { get; set; }

    public string ContentType => "text/vnd.turbo-stream.html";

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the name or path of the partial view that is rendered to the response.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, defaults to <see cref="ControllerActionDescriptor.ActionName"/>.
    /// </remarks>
    public string? ViewName { get; set; }

    /// <summary>
    /// Gets the view data model.
    /// </summary>
    public object? Model => ViewData.Model;

    /// <summary>
    /// Gets or sets the <see cref="ViewDataDictionary"/> for this result.
    /// </summary>
    public ViewDataDictionary ViewData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="ITempDataDictionary"/> for this result.
    /// </summary>
    public ITempDataDictionary TempData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="IViewEngine"/> used to locate views.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, an instance of <see cref="ICompositeViewEngine"/> from
    /// <c>ActionContext.HttpContext.RequestServices</c> is used.
    /// </remarks>
    public IViewEngine? ViewEngine { get; set; }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
    {
        var services = context.HttpContext.RequestServices;
        var executor = services.GetRequiredService<IActionResultExecutor<TurboStreamResult>>();
        return executor.ExecuteAsync(context, this);
    }
}

public class TurboStreamResultExecutor : PartialViewResultExecutor, IActionResultExecutor<TurboStreamResult>
{
    public TurboStreamResultExecutor(
        IOptions<MvcViewOptions> viewOptions,
        IHttpResponseStreamWriterFactory writerFactory,
        ICompositeViewEngine viewEngine,
        ITempDataDictionaryFactory tempDataFactory,
        DiagnosticListener diagnosticListener,
        ILoggerFactory loggerFactory,
        IModelMetadataProvider modelMetadataProvider)
        : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, loggerFactory, modelMetadataProvider)
    {
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(ActionContext context, TurboStreamResult result)
    {
        var viewEngine = result.ViewEngine ?? ViewEngine;
        var viewName = result.ViewName ?? GetActionName(context)!;

        var viewEngineResult = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
        var originalViewEngineResult = viewEngineResult;
        if (!viewEngineResult.Success)
        {
            viewEngineResult = viewEngine.FindView(context, viewName, isMainPage: false);
        }

        viewEngineResult.EnsureSuccessful(originalViewEngineResult.SearchedLocations);

        var action = result.Action.ToString().ToLowerInvariant();
        var preContent = result.Action switch
        {
            TurboStreamAction.Remove => $"<turbo-stream action=\"{action}\">\n",
            _ => $"<turbo-stream action=\"{action}\" target=\"{result.Target}\">\n<template>\n",
        };
        var postContent = result.Action switch
        {
            TurboStreamAction.Remove => "</turbo-stream>",
            _ => "</template>\n</turbo-stream>",
        };

        result.ViewData["RenderingToTurboStream"] = true;

        using var view = new WrapperView(viewEngineResult.View, preContent, postContent);
        return ExecuteAsync(context, view, result.ViewData, result.TempData, result.ContentType, result.StatusCode);
    }

    private static string? GetActionName(ActionContext context)
    {
        const string actionNameKey = "action";
        if (!context.RouteData.Values.TryGetValue(actionNameKey, out var routeValue))
        {
            return null;
        }

        string? normalizedValue = null;
        if (context.ActionDescriptor.RouteValues.TryGetValue(actionNameKey, out var value) && !string.IsNullOrEmpty(value))
        {
            normalizedValue = value;
        }

        var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedValue;
        }

        return stringRouteValue;
    }
}

public sealed class WrapperView : IDisposable, IView
{
    private readonly IView innerView;
    private readonly string preContent;
    private readonly string postContent;

    public WrapperView(IView innerView, string preContent, string postContent)
    {
        this.innerView = innerView;
        this.preContent = preContent;
        this.postContent = postContent;
    }

    public string Path => string.Empty;

    public void Dispose()
    {
        (innerView as IDisposable)?.Dispose();
    }

    public async Task RenderAsync(ViewContext context)
    {
        context.Writer.Write(preContent);
        await innerView.RenderAsync(context);
        context.Writer.Write(postContent);
    }
}
