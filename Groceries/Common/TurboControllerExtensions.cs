namespace Groceries.Common;

using Microsoft.AspNetCore.Mvc;

public static class TurboControllerExtensions
{
    public static TurboStreamResult TurboStream(
        this Controller controller,
        TurboStreamAction action,
        string target,
        object? model)
    {
        return controller.TurboStream(action, target, null, model);
    }

    public static TurboStreamResult TurboStream(
        this Controller controller,
        TurboStreamAction action,
        string target,
        string? viewName,
        object? model)
    {
        controller.ViewData.Model = model;

        return new TurboStreamResult(action, target)
        {
            ViewName = viewName,
            ViewData = controller.ViewData,
            TempData = controller.TempData,
        };
    }
}
