using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Google;

/// <summary>
/// Normalized [y, x] coordinates in 0-1000 range returned by Gemini Robotics-ER spatial reasoning.
/// </summary>
public readonly struct GoogleGeminiRoboticsNormalizedPoint
{
    /// <summary>
    /// Vertical coordinate normalized to 0-1000.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Horizontal coordinate normalized to 0-1000.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Creates a normalized point from a [y, x] coordinate pair.
    /// </summary>
    public GoogleGeminiRoboticsNormalizedPoint(int y, int x)
    {
        Y = y;
        X = x;
    }
}

/// <summary>
/// Object location with a normalized point and label from Gemini Robotics-ER object detection.
/// JSON format: <c>{"point": [y, x], "label": "..."}</c>.
/// </summary>
public class GoogleGeminiRoboticsPointDetection
{
    /// <summary>
    /// Normalized [y, x] coordinates in 0-1000 range.
    /// </summary>
    [JsonProperty("point")]
    public List<int>? Point { get; set; }

    /// <summary>
    /// Identifying name for the detected object.
    /// </summary>
    [JsonProperty("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Parses the normalized point from <see cref="Point"/>.
    /// </summary>
    public GoogleGeminiRoboticsNormalizedPoint? GetNormalizedPoint()
    {
        if (Point is null || Point.Count < 2)
        {
            return null;
        }

        return new GoogleGeminiRoboticsNormalizedPoint(Point[0], Point[1]);
    }
}

/// <summary>
/// 2D bounding box with normalized coordinates from Gemini Robotics-ER object detection.
/// JSON format: <c>{"box_2d": [ymin, xmin, ymax, xmax], "label": "..."}</c>.
/// </summary>
public class GoogleGeminiRoboticsBoundingBoxDetection
{
    /// <summary>
    /// Normalized [ymin, xmin, ymax, xmax] coordinates in 0-1000 range.
    /// </summary>
    [JsonProperty("box_2d")]
    public List<int>? Box2D { get; set; }

    /// <summary>
    /// Identifying name for the detected object.
    /// </summary>
    [JsonProperty("label")]
    public string? Label { get; set; }
}

/// <summary>
/// Trajectory step with a normalized point and order label from Gemini Robotics-ER trajectory planning.
/// JSON format: <c>{"point": [y, x], "label": "0"}</c> where label is the step index.
/// </summary>
public class GoogleGeminiRoboticsTrajectoryPoint : GoogleGeminiRoboticsPointDetection;

/// <summary>
/// Robot function call step from Gemini Robotics-ER task orchestration.
/// JSON format: <c>{"function": "move", "args": [163, 427, true]}</c>.
/// </summary>
public class GoogleGeminiRoboticsFunctionCall
{
    /// <summary>
    /// Name of the robot function to invoke.
    /// </summary>
    [JsonProperty("function")]
    public string? Function { get; set; }

    /// <summary>
    /// Arguments passed to the robot function.
    /// </summary>
    [JsonProperty("args")]
    public List<object>? Args { get; set; }
}
