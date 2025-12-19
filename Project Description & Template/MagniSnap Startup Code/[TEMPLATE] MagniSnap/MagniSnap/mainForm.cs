using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MagniSnap
{
    #region
    /// 4d17639adfad0a300acd78759e07a4f2
    #endregion
    public partial class MainForm : Form
    {
        RGBPixel[,] ImageMatrix;
        bool isLassoEnabled = false;
        
        // Graph and path drawing variables
        Graph imageGraph;
        bool isGraphConstructed = false;
        Point currentAnchor = new Point(-1, -1);
        Point currentFreePoint = new Point(-1, -1);
        List<Point> currentPath;
        List<Point> confirmedPath;
        List<Point> allAnchorsList;


        public MainForm()
        {
            InitializeComponent();
            indicator_pnl.Hide();
            
            // Initialize path drawing variables
            currentPath = new List<Point>();
            confirmedPath = new List<Point>();
            allAnchorsList = new List<Point>();
            // Add Paint event handler to PictureBox for drawing paths
            mainPictureBox.Paint += MainPictureBox_Paint;
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            #region Do Change Remove Template Code
            /// 4d17639adfad0a300acd78759e07a4f2
            #endregion

            indicator_pnl.Top = ((Control)sender).Top;
            indicator_pnl.Height = ((Control)sender).Height;
            indicator_pnl.Left = ((Control)sender).Left;
            ((Control)sender).BackColor = Color.FromArgb(37, 46, 59);
            indicator_pnl.Show();
        }

        private void menuButton_Leave(object sender, EventArgs e)
        {
            ((Control)sender).BackColor = Color.FromArgb(26, 32, 40);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {         
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            #region Do Change Remove Template Code
            /// 4d17639adfad0a300acd78759e07a4f2
            #endregion

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageToolkit.OpenImage(OpenedFilePath);
                ImageToolkit.ViewImage(ImageMatrix, mainPictureBox);

                int width = ImageToolkit.GetWidth(ImageMatrix);
                txtWidth.Text = width.ToString();
                int height = ImageToolkit.GetHeight(ImageMatrix);
                txtHeight.Text = height.ToString();
                
                // Reset graph when new image is loaded
                isGraphConstructed = false;
                imageGraph = null;
                currentPath.Clear();
                confirmedPath.Clear();
                allAnchorsList.Clear();
                currentAnchor = new Point(-1, -1);
                currentFreePoint = new Point(-1, -1);
                mainPictureBox.Refresh();
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear all paths and reset state
            currentPath.Clear();
            confirmedPath.Clear();
            allAnchorsList.Clear();
            currentAnchor = new Point(-1, -1);
            currentFreePoint = new Point(-1, -1);
            mainPictureBox.Refresh();
        }

        private void btnLivewire_Click(object sender, EventArgs e)
        {
            menuButton_Leave(btnArrow, EventArgs.Empty);
            menuButton_Click(sender, e);
            mainPictureBox.Cursor = Cursors.Cross;
            isLassoEnabled = true;
        }

        private void btnLivewire_Leave(object sender, EventArgs e)
        {
            menuButton_Leave(sender, e);

            mainPictureBox.Cursor = Cursors.Default;
            isLassoEnabled = false;
        }

        private void mainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ImageMatrix != null && isLassoEnabled)
            {
                // Get image coordinates from mouse position
                Point imagePoint = GetImageCoordinates(e.Location);

                // Validate point is within image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);

                if (imagePoint.X >= 0 && imagePoint.X < width &&
                    imagePoint.Y >= 0 && imagePoint.Y < height)
                {
                    // Construct graph if not already done
                    if (!isGraphConstructed)
                    {
                        imageGraph = new Graph(ImageMatrix);
                        imageGraph.ConstructGraph();
                        isGraphConstructed = true;
                    }

                    // Add the point to the anchors list to draw the square marker later
                    if (currentPath != null && currentPath.Count > 0)
                    {
                        if (confirmedPath.Count > 0) currentPath.RemoveAt(0);
                        confirmedPath.AddRange(currentPath);
                    }

                    allAnchorsList.Add(imagePoint);

                    // Set anchor point
                    currentAnchor = imagePoint;

                    // Run Dijkstra from anchor point to all pixels
                    imageGraph.DijkstraShortestPath(currentAnchor);

                    // Clear current path (will be recalculated on mouse move)
                    currentPath.Clear();

                    // Refresh to redraw
                    mainPictureBox.Refresh();
                }
            }
        }
        private void mainPictureBox_MouseMove(object sender, MouseEventArgs e)
        {

            txtMousePosX.Text = e.X.ToString();
            txtMousePosY.Text = e.Y.ToString();

            if (ImageMatrix != null && isLassoEnabled && isGraphConstructed)
            {
                // Get image coordinates from mouse position
                Point imagePoint = GetImageCoordinates(e.Location);
                
                // Validate point is within image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                
                if (imagePoint.X >= 0 && imagePoint.X < width && 
                    imagePoint.Y >= 0 && imagePoint.Y < height)
                {
                    currentFreePoint = imagePoint;

                    // Backtrack path from free point to anchor point
                    currentPath = imageGraph.backthrough(currentFreePoint);
                    
                    // Refresh to redraw path
                    mainPictureBox.Refresh();
                }
            }
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        // Helper function to draw a small square at anchor points
        private void DrawAnchorSquare(Graphics g, Point imgPt)
        {
            // You can now call this method because it is public
            Point screenPt = imageGraph.GetScreenCoordinates(imgPt, mainPictureBox);
            int size = 6;
            Rectangle rect = new Rectangle(screenPt.X - size / 2, screenPt.Y - size / 2, size, size);

            g.FillRectangle(Brushes.White, rect); // White fill
            g.DrawRectangle(Pens.Green, rect);   // Green border
        }

        private void MainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (ImageMatrix == null || !isLassoEnabled || imageGraph == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 1. Draw the fully confirmed path
            if (confirmedPath != null && confirmedPath.Count > 1)
            {
                imageGraph.DrawPath(g, confirmedPath, mainPictureBox, Color.Yellow, 2);
            }

            // 2. Draw the current live path (between the last anchor and the mouse)
            if (currentPath != null && currentPath.Count > 1)
            {
                imageGraph.DrawPath(g, currentPath, mainPictureBox, Color.Red, 2);
            }

            // 3. Draw small squares at each anchor point (professional appearance)
            foreach (Point anchor in allAnchorsList)
            {
                DrawAnchorSquare(g, anchor);
            }
        }     


        private Point GetImageCoordinates(Point screenPoint)
        {
            if (mainPictureBox.Image == null)
                return new Point(-1, -1);
            
            // Mouse coordinates are already relative to PictureBox
            int relativeX = screenPoint.X;
            int relativeY = screenPoint.Y;
            
            // Clamp to PictureBox bounds
            if (relativeX < 0) relativeX = 0;
            if (relativeY < 0) relativeY = 0;
            if (relativeX >= mainPictureBox.Width) relativeX = mainPictureBox.Width - 1;
            if (relativeY >= mainPictureBox.Height) relativeY = mainPictureBox.Height - 1;
            
            // If PictureBox is using AutoSize, coordinates map directly
            // Otherwise, we need to account for scaling
            if (mainPictureBox.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                // Clamp to image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                if (relativeX >= width) relativeX = width - 1;
                if (relativeY >= height) relativeY = height - 1;
                return new Point(relativeX, relativeY);
            }
            else
            {
                // Calculate scaling factors
                float scaleX = (float)mainPictureBox.Image.Width / mainPictureBox.Width;
                float scaleY = (float)mainPictureBox.Image.Height / mainPictureBox.Height;
                
                int imageX = (int)(relativeX * scaleX);
                int imageY = (int)(relativeY * scaleY);
                
                // Clamp to image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                if (imageX >= width) imageX = width - 1;
                if (imageY >= height) imageY = height - 1;
                
                return new Point(imageX, imageY);
            }
        }

        private void mainPictureBox_Click(object sender, EventArgs e)
        {

        }

        private void appLogo_Click(object sender, EventArgs e)
        {
            if (ImageMatrix == null)
            {
                MessageBox.Show(
                    "Please open an image first.",
                    "No Image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (confirmedPath.Count < 3)
            {
                MessageBox.Show(
                    "Please draw a closed path using the Livewire tool before cropping.",
                    "Invalid Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (confirmedPath[0] != confirmedPath[confirmedPath.Count - 1])
                confirmedPath.Add(confirmedPath[0]);

            RGBPixel[,] cropped = CropPhoto(ImageMatrix, confirmedPath);

            if (cropped == null)
            {
                MessageBox.Show(
                    "Cropping failed.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            ShowCroppedImage(cropped);

            ImageToolkit.ViewImage(cropped, mainPictureBox);
            ImageMatrix = cropped;

            confirmedPath.Clear();
            currentPath.Clear();
            allAnchorsList.Clear();
            isGraphConstructed = false;
            imageGraph = null;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private RGBPixel[,] CropPhoto(RGBPixel[,] image, List<Point> path)
        {
            if (path == null || path.Count < 3)
                return null;

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (Point p in path)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            int newW = maxX - minX + 1;
            int newH = maxY - minY + 1;

            RGBPixel[,] result = new RGBPixel[newH, newW];

            bool Inside(int x, int y)
            {
                bool inside = false;
                for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
                {
                    if (((path[i].Y > y) != (path[j].Y > y)) &&
                        (x < (path[j].X - path[i].X) * (y - path[i].Y) / (double)(path[j].Y - path[i].Y) + path[i].X))
                        inside = !inside;
                }
                return inside;
            }

            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    if (Inside(x, y))
                        result[y - minY, x - minX] = image[y, x];
                    else
                        result[y - minY, x - minX] = new RGBPixel { red = 0, green = 0, blue = 0 };
                }

            return result;
        }

        private void ShowCroppedImage(RGBPixel[,] croppedImage)
        {
            Form cropForm = new Form();
            cropForm.Text = "Cropped Image";
            cropForm.Width = 800;
            cropForm.Height = 600;

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;

            ImageToolkit.ViewImage(croppedImage, pb);

            cropForm.Controls.Add(pb);
            cropForm.StartPosition = FormStartPosition.CenterScreen;
            cropForm.Show();
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (mainPictureBox.Image == null)
            {
                MessageBox.Show(
                    "There is no image to save.",
                    "Save Image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Save Image";
                sfd.Filter =
                    "PNG Image (*.png)|*.png|" +
                    "JPEG Image (*.jpg)|*.jpg|" +
                    "Bitmap Image (*.bmp)|*.bmp";
                sfd.DefaultExt = "png";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (Bitmap bmp = new Bitmap(mainPictureBox.Image))
                        {
                            switch (System.IO.Path.GetExtension(sfd.FileName).ToLower())
                            {
                                case ".jpg":
                                    bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    break;

                                case ".bmp":
                                    bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                                    break;

                                default:
                                    bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                                    break;
                            }
                        }

                        MessageBox.Show(
                            "Image saved successfully.",
                            "Save Image",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Failed to save image.\n" + ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        private void mainPictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || ImageMatrix == null)
                return;

            menuButton_Click(btnArrow, EventArgs.Empty);

            btnLivewire.BackColor = Color.FromArgb(26, 32, 40);

            bool prevLasso = isLassoEnabled;
            isLassoEnabled = true;

            mainPictureBox_MouseClick(
                sender,
                new MouseEventArgs(
                    MouseButtons.Left,
                    1,
                    e.X,
                    e.Y,
                    0
                )
            );

            isLassoEnabled = false;
            currentPath.Clear();
            currentFreePoint = new Point(-1, -1);
            mainPictureBox.Cursor = Cursors.Default;
        }
    }

}
