using PuppeteerSharp;

namespace LlmTornado.Internal.Press.Utils;

/// <summary>
/// Implements the WindMouse algorithm for realistic, human-like mouse movement.
/// </summary>
public static class WindMouse
{
    private static readonly Random Random = new Random();
    
    private static double _lastMouseX = 0;
    private static double _lastMouseY = 0;
    
    private const double MOUSE_SPEED = 10.0;
    
    /// <summary>
    /// Moves the mouse from current position to target position using human-like curved motion
    /// </summary>
    /// <param name="page">The Puppeteer page</param>
    /// <param name="endX">Target X coordinate</param>
    /// <param name="endY">Target Y coordinate</param>
    /// <param name="gravity">Strength of the gravitational pull towards the target (recommended: 5-15)</param>
    /// <param name="wind">Strength of the wind drift (randomness) (recommended: 3-10)</param>
    /// <param name="minWait">Minimum wait time between mouse movements in ms (recommended: 1-3)</param>
    /// <param name="maxWait">Maximum wait time between mouse movements in ms (recommended: 3-10)</param>
    /// <param name="timeoutMs">Maximum time in ms to spend on the movement before giving up (default: 500ms)</param>
    public static async Task MoveAsync(
        IPage page, 
        double endX, 
        double endY,
        double gravity = 9.0,
        double wind = 3.0,
        int minWait = 5,
        int maxWait = 10,
        int timeoutMs = 500)
    {
        try
        {
            // Use last known position as starting point
            await MoveFromToAsync(page, _lastMouseX, _lastMouseY, endX, endY, gravity, wind, minWait, maxWait, timeoutMs);
        }
        catch (TimeoutException)
        {
            // If we timeout, just jump to the target
            await page.Mouse.MoveAsync((decimal)endX, (decimal)endY);
        }
        
        // Update last position
        _lastMouseX = endX;
        _lastMouseY = endY;
    }
    
    /// <summary>
    /// Moves the mouse to a random position within the viewport for idle-like behavior
    /// </summary>
    public static async Task MoveToRandomPositionAsync(IPage page)
    {
        var viewport = page.Viewport;
        if (viewport == null) return;
        
        // Move to a random position, avoiding edges
        int margin = 100;
        double targetX = Random.Next(margin, viewport.Width - margin);
        double targetY = Random.Next(margin, viewport.Height - margin);
        
        await MoveAsync(page, targetX, targetY, gravity: 7, wind: 4);
    }
    
    /// <summary>
    /// Moves the mouse to an element's center with realistic motion
    /// </summary>
    public static async Task MoveToElementAsync(IPage page, IElementHandle element, int offsetX = 0, int offsetY = 0)
    {
        var box = await element.BoundingBoxAsync();
        if (box == null) return;
        
            // Add small random offset to avoid always clicking exact center
            double targetX = (double)box.X + ((double)box.Width / 2) + offsetX + (Random.NextDouble() * 10 - 5);
            double targetY = (double)box.Y + ((double)box.Height / 2) + offsetY + (Random.NextDouble() * 10 - 5);
        
        await MoveAsync(page, targetX, targetY);
    }
    
    /// <summary>
    /// Core WindMouse algorithm implementation based on the correct reference
    /// </summary>
    private static async Task MoveFromToAsync(
        IPage page,
        double startX,
        double startY,
        double endX,
        double endY,
        double gravity,
        double wind,
        int minWait,
        int maxWait,
        int timeoutMs)
    {
        var startTime = DateTime.UtcNow;
        
        // Algorithm parameters
        double maxStep = 20.0;
        double targetArea = 5.0;
        
        if (gravity < 1) gravity = 1;
        if (maxStep == 0) maxStep = 0.01;
        
        double windX = Random.Next(10);
        double windY = Random.Next(10);
        double velocityX = 0;
        double velocityY = 0;
        
        double sqrt2 = Math.Sqrt(2.0);
        double sqrt3 = Math.Sqrt(3.0);
        double sqrt5 = Math.Sqrt(5.0);
        
        double currentX = startX;
        double currentY = startY;
        double dist = Hypot(endX - startX, endY - startY);
        
        int moveCount = 0;
        
        while (dist > 1.0)
        {
            // Check timeout
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Mouse movement timeout");
            }
            
            wind = Math.Min(wind, dist);
            
            if (dist >= targetArea)
            {
                int w = Random.Next((int)Math.Round(wind) * 2 + 1);
                windX = windX / sqrt3 + (w - wind) / sqrt5;
                windY = windY / sqrt3 + (w - wind) / sqrt5;
            }
            else
            {
                windX = windX / sqrt2;
                windY = windY / sqrt2;
                if (maxStep < 3)
                    maxStep = Random.Next(3) + 3.0;
                else
                    maxStep = maxStep / sqrt5;
            }
            
            velocityX += windX;
            velocityY += windY;
            velocityX = velocityX + (gravity * (endX - currentX)) / dist;
            velocityY = velocityY + (gravity * (endY - currentY)) / dist;
            
            double veloMag = Hypot(velocityX, velocityY);
            if (veloMag > maxStep)
            {
                double randomDist = maxStep / 2.0 + Random.Next((int)Math.Round(maxStep) / 2);
                velocityX = (velocityX / veloMag) * randomDist;
                velocityY = (velocityY / veloMag) * randomDist;
            }
            
            double oldX = Math.Round(currentX);
            double oldY = Math.Round(currentY);
            
            currentX += velocityX;
            currentY += velocityY;
            
            dist = Hypot(endX - currentX, endY - currentY);
            
            double newX = Math.Round(currentX);
            double newY = Math.Round(currentY);
            
            // Only move if position actually changed
            if (oldX != newX || oldY != newY)
            {
                await page.Mouse.MoveAsync((decimal)newX, (decimal)newY);
                
                double step = Hypot(currentX - oldX, currentY - oldY);
                double waitDiff = maxWait - minWait;
                int wait = (int)Math.Round(waitDiff * (step / maxStep) + minWait);
                
                if (wait > 0)
                    await Task.Delay(wait);
                
                moveCount++;
            }
        }
        
        // Final movement to exact target
        double finalX = Math.Round(endX);
        double finalY = Math.Round(endY);
        await page.Mouse.MoveAsync((decimal)finalX, (decimal)finalY);
    }
    
    private static double Hypot(double dx, double dy)
    {
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

