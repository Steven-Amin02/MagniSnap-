using MagniSnap;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MagniSnap
{
    public class Graph
    {
        private RGBPixel[,] imageMatrix;
        private int width;
        private int height;
        private int size;

        private double[] rightWeights;
        private double[] bottomWeights;

        public double[] dist;
        public int[] parent;

        // NEW: Active Window Tracking for Dynamic Expansion
        public int WindowRadius { get; set; } = 200;
        private Point lastAnchor = new Point(-1, -1);
        private int currentMinX = -1, currentMaxX = -1, currentMinY = -1, currentMaxY = -1;

        public Graph(RGBPixel[,] image)
        {
            imageMatrix = image;
            height = ImageToolkit.GetHeight(image);
            width = ImageToolkit.GetWidth(image);
            size = width * height;

            rightWeights = new double[size];
            bottomWeights = new double[size];
            dist = new double[size];
            parent = new int[size];
        }

        // ================= GRAPH CONSTRUCTION =================
        public void ConstructGraph()
        {
            int idx = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2D energy = ImageToolkit.CalculatePixelEnergies(x, y, imageMatrix);

                    // Right Edge
                    if (x < width - 1)
                    {
                        rightWeights[idx] = (energy.X == 0) ? 1e9 : 1.0 / energy.X;
                    }
                    else
                    {
                        rightWeights[idx] = 1e9; // Border
                    }

                    // Bottom Edge
                    if (y < height - 1)
                    {
                        bottomWeights[idx] = (energy.Y == 0) ? 1e9 : 1.0 / energy.Y;
                    }
                    else
                    {
                        bottomWeights[idx] = 1e9; // Border
                    }

                    idx++;
                }
            }
        }

        // ================= DIJKSTRA =================
        // UPDATED: Added overrideRadius to allow dynamic expansion triggered by backthrough
        public void DijkstraShortestPath(Point anchor, Point? stopPoint = null, int? overrideRadius = null)
        {
            if (anchor.X < 0 || anchor.X >= width || anchor.Y < 0 || anchor.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor must be inside image bounds.");

            lastAnchor = anchor;
            int radius = overrideRadius ?? WindowRadius;

            int startIndex = anchor.Y * width + anchor.X;
            int targetIndex = stopPoint.HasValue ? (stopPoint.Value.Y * width + stopPoint.Value.X) : -1;

            int minX = Math.Max(0, anchor.X - radius);
            int maxX = Math.Min(width - 1, anchor.X + radius);
            int minY = Math.Max(0, anchor.Y - radius);
            int maxY = Math.Min(height - 1, anchor.Y + radius);

            // 1. Clear the OLD bounding box to prevent stale paths bleeding over
            if (currentMinX != -1)
            {
                for (int y = currentMinY; y <= currentMaxY; y++)
                {
                    for (int x = currentMinX; x <= currentMaxX; x++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            int idx = y * width + x;
                            dist[idx] = double.MaxValue;
                            parent[idx] = -1;
                        }
                    }
                }
            }

            // 2. Clear the NEW bounding box
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = y * width + x;
                    dist[idx] = double.MaxValue;
                    parent[idx] = -1;
                }
            }

            // Update tracked bounds
            currentMinX = minX;
            currentMaxX = maxX;
            currentMinY = minY;
            currentMaxY = maxY;

            dist[startIndex] = 0;

            SimplePriorityQueue<int, double> pq = new SimplePriorityQueue<int, double>();
            pq.Enqueue(startIndex, 0);

            while (pq.Count > 0)
            {
                int u = pq.Dequeue();
                double uDist = dist[u];

                if (u == targetIndex) break;

                int ux = u % width;
                int uy = u / width;

                int v;
                double weight;

                if (ux > minX)
                {
                    v = u - 1;
                    weight = rightWeights[v];
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v)) pq.UpdatePriority(v, dist[v]);
                        else pq.Enqueue(v, dist[v]);
                    }
                }

                if (ux < maxX)
                {
                    v = u + 1;
                    weight = rightWeights[u];
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v)) pq.UpdatePriority(v, dist[v]);
                        else pq.Enqueue(v, dist[v]);
                    }
                }

                if (uy > minY)
                {
                    v = u - width;
                    weight = bottomWeights[v];
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v)) pq.UpdatePriority(v, dist[v]);
                        else pq.Enqueue(v, dist[v]);
                    }
                }

                if (uy < maxY)
                {
                    v = u + width;
                    weight = bottomWeights[u];
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v)) pq.UpdatePriority(v, dist[v]);
                        else pq.Enqueue(v, dist[v]);
                    }
                }
            }
        }

        public List<Point> backthrough(Point target)
        {
            List<Point> path = new List<Point>();

            if (target.X < 0 || target.X >= width || target.Y < 0 || target.Y >= height)
                return path;

            // ===== DYNAMIC EXPANSION LOGIC =====
            // If the user's mouse leaves the calculated window, expand it automatically!
            if (target.X < currentMinX || target.X > currentMaxX ||
                target.Y < currentMinY || target.Y > currentMaxY)
            {
                // Calculate the distance to the target and add a buffer (50 pixels)
                int targetDistX = Math.Abs(target.X - lastAnchor.X);
                int targetDistY = Math.Abs(target.Y - lastAnchor.Y);
                int neededRadius = Math.Max(targetDistX, targetDistY) + 50;

                // Dynamically re-run Dijkstra with the expanded radius to catch up to the mouse
                DijkstraShortestPath(lastAnchor, null, neededRadius);
            }
            // ===================================

            int currIndex = target.Y * width + target.X;

            if (dist[currIndex] == double.MaxValue) return path;

            while (currIndex != -1)
            {
                path.Add(new Point(currIndex % width, currIndex / width));
                currIndex = parent[currIndex];
            }

            path.Reverse();
            return path;
        }

        public void DrawPath(Graphics g, List<Point> path, PictureBox picBox, Color color, int penWidth)
        {
            if (path == null || path.Count < 2)
                return;

            Pen pen = new Pen(color, penWidth);

            Point[] screenPoints = new Point[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                screenPoints[i] = GetScreenCoordinates(path[i], picBox);
            }

            for (int i = 0; i < screenPoints.Length - 1; i++)
            {
                g.DrawLine(pen, screenPoints[i], screenPoints[i + 1]);
            }

            pen.Dispose();
        }

        public void DrawPoint(Graphics g, Point imagePoint, PictureBox picBox, Color color, int radius)
        {
            Point screenPoint = GetScreenCoordinates(imagePoint, picBox);
            Brush brush = new SolidBrush(color);

            int x = screenPoint.X - radius / 2;
            int y = screenPoint.Y - radius / 2;

            g.FillEllipse(brush, x, y, radius, radius);

            brush.Dispose();
        }

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
                Point start = anchors[i];
                Point end = anchors[i + 1];

                // Ensure we don't limit the window for the full bonus sequence generation
                int originalRadius = WindowRadius;
                WindowRadius = Math.Max(width, height);

                DijkstraShortestPath(start, end);

                WindowRadius = originalRadius;

                List<Point> segment = backthrough(end);

                if (segment.Count > 0)
                {
                    if (i > 0) segment.RemoveAt(0);
                    fullPath.AddRange(segment);
                }
            }
            return fullPath;
        }
    }
}