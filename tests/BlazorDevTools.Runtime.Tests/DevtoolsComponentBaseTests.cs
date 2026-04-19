using System.Reflection;
using BlazorDevTools.Protocol;
using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime.Tests;

public class DevtoolsComponentBaseTests
{
    [Test]
    public async Task SetParametersAsync_helpers_capture_parameters_before_async_boundary()
    {
        var parameterView = ParameterView.FromDictionary(
            new Dictionary<string, object?>
            {
                [nameof(TestComponent.Title)] = "Weather",
                [nameof(TestComponent.Count)] = 5
            });

        var capturedParameters = InvokeCaptureParameterValues(parameterView);

        await Task.Yield();

        var formattedParameters = InvokeFormatCapturedParameters(capturedParameters);

        Assert.That(formattedParameters.Select(parameter => parameter.Name), Is.EqualTo(new[] { nameof(TestComponent.Title), nameof(TestComponent.Count) }));
        Assert.That(formattedParameters.Select(parameter => parameter.Value), Is.EqualTo(new[] { "Weather", "5" }));
    }

    private static object InvokeCaptureParameterValues(ParameterView parameterView)
    {
        var method = typeof(DevtoolsComponentBase).GetMethod("CaptureParameterValues", BindingFlags.Static | BindingFlags.NonPublic);
        return method!.Invoke(null, [parameterView])!;
    }

    private static IReadOnlyList<ComponentParameterSnapshot> InvokeFormatCapturedParameters(object capturedParameters)
    {
        var method = typeof(DevtoolsComponentBase).GetMethod("FormatCapturedParameters", BindingFlags.Static | BindingFlags.NonPublic);
        return (IReadOnlyList<ComponentParameterSnapshot>)method!.Invoke(null, [capturedParameters])!;
    }

    private sealed class TestComponent
    {
        public string Title { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
