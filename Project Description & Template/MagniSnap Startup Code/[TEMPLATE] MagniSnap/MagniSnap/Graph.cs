using MagniSnap;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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
                        // Weight = 1 / Energy. Handle 0 energy (flat color) by giving it a high cost.
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
        public void DijkstraShortestPath(Point anchor, Point? stopPoint = null)
        {
            // Validate anchor point
            if (anchor.X < 0 || anchor.X >= width || anchor.Y < 0 || anchor.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor must be inside image bounds.");

            int startIndex = anchor.Y * width + anchor.X;
            int targetIndex = stopPoint.HasValue ? (stopPoint.Value.Y * width + stopPoint.Value.X) : -1;

            for (int i = 0; i < size; i++)
            {
                dist[i] = double.MaxValue;
                parent[i] = -1;
            }
            dist[startIndex] = 0;

            SimplePriorityQueue<int, double> pq = new SimplePriorityQueue<int, double>();
            pq.Enqueue(startIndex, 0);

            while (pq.Count > 0)
            {
                int u = pq.Dequeue();
                double uDist = dist[u];

                // Stop early if we reached the target
                if (u == targetIndex) break;

                int ux = u % width; // x-coordinate
                int uy = u / width; // y-coordinate

                // CHECK NEIGHBORS
                int v;
                double weight;
                // LEFT (u - 1)
                if (ux > 0)
                {
                    v = u - 1;
                    weight = rightWeights[v]; // Coming from left, weight is stored in v's rightWeights
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if(pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // RIGHT (u + 1)
                if (ux < width - 1)
                {
                    v = u + 1;
                    weight = rightWeights[u]; // Moving right, weight is stored in current u
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if(pq.Contains(v)) 
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // UP (u - width)
                if (uy > 0)
                {
                    v = u - width;
                    weight = bottomWeights[v]; // Coming from top, weight is stored in v's bottomWeights
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // DOWN (u + width)
                if (uy < height - 1)
                {
                    v = u + width;
                    weight = bottomWeights[u]; // Moving down, weight is stored in current u
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
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

            int currIndex = target.Y * width + target.X;

            // If unreachable, return empty
            if (dist[currIndex] == double.MaxValue) return path;

            // Reconstruct path
            while (currIndex != -1)
            {
                path.Add(new Point(currIndex % width, currIndex / width));
                currIndex = parent[currIndex];
            }

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
                Point start = anchors[i];
                Point end = anchors[i + 1];

                DijkstraShortestPath(start, end);
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