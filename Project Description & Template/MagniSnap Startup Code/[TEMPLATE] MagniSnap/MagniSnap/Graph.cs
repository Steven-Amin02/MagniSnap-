using MagniSnap;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;


public class Graph
{
    private RGBPixel[,] imageMatrix;
    private int width;
    private int height;

    private double[,] rightWeights;
    private double[,] bottomWeights;

    public double[,] dist;
    public Point[,] parent;

    public Graph(RGBPixel[,] image)
    {
        imageMatrix = image;
        height = ImageToolkit.GetHeight(image);
        width = ImageToolkit.GetWidth(image);

        rightWeights = new double[height, width];
        bottomWeights = new double[height, width];
    }

    // ================= GRAPH CONSTRUCTION =================
    public void ConstructGraph()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2D energy = ImageToolkit.CalculatePixelEnergies(x, y, imageMatrix);

                if (x < width - 1)
                {
                    // If energy is 0 (perfectly flat), weight becomes Infinity.
                    rightWeights[y, x] = (energy.X == 0) ? 1e9 : 1.0 / energy.X;
                }

                if (y < height - 1)
                {
                    bottomWeights[y, x] = (energy.Y == 0) ? 1e9 : 1.0 / energy.Y;

                }
            }
        }
    }

    // ================= DIJKSTRA =================
    public void DijkstraShortestPath(Point anchor)
    {
        // Validate anchor point
        if (anchor.X < 0 || anchor.X >= width || anchor.Y < 0 || anchor.Y >= height)
            throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor must be inside image bounds.");


        dist = new double[height, width];
        parent = new Point[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                dist[y, x] = double.MaxValue;
                parent[y, x] = new Point(-1, -1);
            }
        }
        dist[anchor.Y, anchor.X] = 0.0;

        var pq = new SimplePriorityQueue<Point, double>();
        pq.Enqueue(anchor, 0.0);

        int[] dx = { -1, 0, 1, 0 };
        int[] dy = { 0, -1, 0, 1 };

        while (pq.Count > 0)
        {
            Point current = pq.Dequeue();
            double currentDist = dist[current.Y, current.X];

            for (int dir = 0; dir < 4; dir++)
            {
                int newX = current.X + dx[dir];
                int newY = current.Y + dy[dir];

                if (newX < 0 || newX >= width || newY < 0 || newY >= height)
                    continue;

                double weight;
                switch (dir)
                {
                    case 0: // Left
                            // edge between (newX,newY) and (current.X,current.Y) — newX = current.X-1
                        weight = rightWeights[current.Y, newX];
                        break;
                    case 1: // Up (newY = current.Y - 1)
                        weight = bottomWeights[newY, current.X];
                        break;
                    case 2: // Right
                        weight = rightWeights[current.Y, current.X];
                        break;
                    case 3: // case 3 Down
                        weight = bottomWeights[current.Y, current.X];
                        break;

                    default: 
                        weight = 1e9;
                        break;
                }

                double newDist = currentDist + weight;
                if (newDist < dist[newY, newX])
                {
                    dist[newY, newX] = newDist;
                    parent[newY, newX] = current;
                    Point neighbor = new Point(newX, newY);

                    if (pq.Contains(neighbor))
                        pq.UpdatePriority(neighbor, newDist);
                    else
                        pq.Enqueue(neighbor, newDist);
                }
            }
        }
    }

    public List<Point> backthrough(Point target)
    {
        List<Point> path = new List<Point>();

        // Check if target is within bounds
        if (target.X < 0 || target.X >= width || target.Y < 0 || target.Y >= height)
            return path;

        // Check if the target is reachable
        if (dist[target.Y, target.X] == double.MaxValue)
            return path;

        Point curr = target;
        // Backtrack until we find the start node (parent is -1, -1)
        while (curr.X != -1 && curr.Y != -1)
        {
            path.Add(curr);
            curr = parent[curr.Y, curr.X];
        }

        // The path is currently from Target -> Anchor.
        // It's often useful to have it Anchor -> Target.
        path.Reverse();

        return path;
    }

    /// <summary>
    /// Draws a path on the PictureBox
    /// </summary>
    public void DrawPath(Graphics g, List<Point> path, PictureBox picBox, Color color, int penWidth)
    {
        if (path == null || path.Count < 2)
            return;
        
        Pen pen = new Pen(color, penWidth);
        
        // Convert image coordinates to screen coordinates
        Point[] screenPoints = new Point[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            screenPoints[i] = GetScreenCoordinates(path[i], picBox);
        }
        
        // Draw lines connecting the path points
        for (int i = 0; i < screenPoints.Length - 1; i++)
        {
            g.DrawLine(pen, screenPoints[i], screenPoints[i + 1]);
        }
        
        pen.Dispose();
    }
    
    /// <summary>
    /// Draws a point (circle) on the PictureBox
    /// </summary>
    public void DrawPoint(Graphics g, Point imagePoint, PictureBox picBox, Color color, int radius)
    {
        Point screenPoint = GetScreenCoordinates(imagePoint, picBox);
        Brush brush = new SolidBrush(color);
        
        int x = screenPoint.X - radius / 2;
        int y = screenPoint.Y - radius / 2;
        
        g.FillEllipse(brush, x, y, radius, radius);
        
        brush.Dispose();
    }
    
    /// <summary>
    /// Converts image coordinates to screen coordinates for drawing
    /// Returns coordinates relative to PictureBox
    /// </summary>
    public Point GetScreenCoordinates(Point imagePoint, PictureBox picBox)
    {
        if (picBox.Image == null)
            return new Point(-1, -1);
        
        if (picBox.SizeMode == PictureBoxSizeMode.AutoSize)
        {
            return new Point(imagePoint.X, imagePoint.Y);
        }
        else
        {
            // Calculate scaling factors
            float scaleX = (float)picBox.Width / picBox.Image.Width;
            float scaleY = (float)picBox.Height / picBox.Image.Height;
            
            int screenX = (int)(imagePoint.X * scaleX);
            int screenY = (int)(imagePoint.Y * scaleY);
            
            return new Point(screenX, screenY);
        }
    }


    public List<Point> GenerateConnectedPaths(List<Point> anchors)
    {
        List<Point> fullPath = new List<Point>();

        if (anchors == null || anchors.Count < 2)
            return fullPath;

        for (int i = 0; i < anchors.Count - 1; i++)
        {
            DijkstraShortestPath(anchors[i]);
            List<Point> segment = backthrough(anchors[i + 1]);

            if (segment.Count == 0)
                continue;

            if (i > 0)
                segment.RemoveAt(0);

            fullPath.AddRange(segment);
        }

        return fullPath;
    
    }

    


}

