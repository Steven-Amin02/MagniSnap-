using MagniSnap;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                    if (energy.X == 0)
                        rightWeights[y, x] = 1e9; // Very high cost
                    else
                        rightWeights[y, x] = 1.0 / energy.X;
                }

                if (y < height - 1)
                {
                    if (energy.Y == 0)
                        bottomWeights[y, x] = 1e9;
                    else
                        bottomWeights[y, x] = 1.0 / energy.Y;
                }
            }
        }
    }

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
                    default: // case 3 Down
                        weight = bottomWeights[current.Y, current.X];
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

    public List<Point> GetPath(Point target)
    {
        List<Point> path = new List<Point>();
        // ...
        return path;
    }

}
