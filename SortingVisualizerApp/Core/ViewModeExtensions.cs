namespace SortingVisualizerApp.Core;

public static class ViewModeExtensions
{
    public static SupportedViews ToFlag(this VisualizationMode mode)
    {
        return mode switch
        {
            VisualizationMode.Bars => SupportedViews.Bars,
            VisualizationMode.Network => SupportedViews.Network,
            VisualizationMode.External => SupportedViews.External,
            VisualizationMode.Graph => SupportedViews.Graph,
            VisualizationMode.String => SupportedViews.String,
            VisualizationMode.Spatial => SupportedViews.Spatial,
            _ => SupportedViews.Bars
        };
    }

    public static bool IsSupportedBy(this VisualizationMode mode, SupportedViews supportedViews)
    {
        return (supportedViews & mode.ToFlag()) != 0;
    }

    public static VisualizationMode FirstSupportedMode(this SupportedViews supportedViews)
    {
        if ((supportedViews & SupportedViews.Bars) != 0)
        {
            return VisualizationMode.Bars;
        }

        if ((supportedViews & SupportedViews.Network) != 0)
        {
            return VisualizationMode.Network;
        }

        if ((supportedViews & SupportedViews.External) != 0)
        {
            return VisualizationMode.External;
        }

        if ((supportedViews & SupportedViews.Graph) != 0)
        {
            return VisualizationMode.Graph;
        }

        if ((supportedViews & SupportedViews.String) != 0)
        {
            return VisualizationMode.String;
        }

        if ((supportedViews & SupportedViews.Spatial) != 0)
        {
            return VisualizationMode.Spatial;
        }

        return VisualizationMode.Bars;
    }

    public static string ToDisplayString(this SupportedViews views)
    {
        if (views == SupportedViews.None)
        {
            return "-";
        }

        var parts = new List<string>(6);
        if ((views & SupportedViews.Bars) != 0)
        {
            parts.Add("Bars");
        }

        if ((views & SupportedViews.Network) != 0)
        {
            parts.Add("Network");
        }

        if ((views & SupportedViews.External) != 0)
        {
            parts.Add("External");
        }

        if ((views & SupportedViews.Graph) != 0)
        {
            parts.Add("Graph");
        }

        if ((views & SupportedViews.String) != 0)
        {
            parts.Add("String");
        }

        if ((views & SupportedViews.Spatial) != 0)
        {
            parts.Add("Spatial");
        }

        return string.Join(", ", parts);
    }
}
