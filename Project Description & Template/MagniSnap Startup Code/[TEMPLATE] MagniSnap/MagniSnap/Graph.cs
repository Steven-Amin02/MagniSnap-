using MagniSnap;
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
        dist = new double[height, width];
        parent = new Point[height, width];
        // ...
    }

    public List<Point> GetPath(Point target)
    {
        List<Point> path = new List<Point>();
        // ...
        return path;
    }

}
